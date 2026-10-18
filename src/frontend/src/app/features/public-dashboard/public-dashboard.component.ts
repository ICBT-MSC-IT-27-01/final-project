import { DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService, FrontendApiError } from '../../core/api.service';
import { LatestForecastResponse } from '../../core/api.models';

@Component({
  selector: 'app-public-dashboard',
  imports: [DecimalPipe, RouterLink],
  template: `
    <section class="dashboard-hero dashboard-hero-refined">
      <div>
        <p class="eyebrow">Anuradhapura District · seven-day outlook</p>
        <h1>Weather context, made clear for each farming decision.</h1>
        <p>
          Review the latest backend-generated forecast before exploring the six approved crop
          recommendations. Forecast information is shown as predicted weather, not measured observations.
        </p>
        <div class="action-row">
          <a routerLink="/recommendations" class="button primary">Explore crop recommendations</a>
          <a routerLink="/seasonal-planning" class="button secondary">View seasonal planning</a>
        </div>
      </div>
      <aside class="forecast-status-board" aria-label="Forecast availability">
        <span class="forecast-live"><span></span> Forecast available</span>
        <h2>Latest forecast run</h2>
        <p>A complete seven-day operational forecast has been selected by the backend and is ready for read-only evaluation.</p>
        @if (forecast(); as latest) {
          <div class="forecast-status-meta">
            <div><span>Model</span><strong>{{ latest.modelVersion || 'Not recorded' }}</strong></div>
            <div><span>Freshness</span><strong>{{ latest.freshnessStatus }}</strong></div>
            <div><span>Forecast run</span><strong>Operational</strong></div>
            <div><span>Period</span><strong>{{ latest.forecastPeriodStart }}</strong></div>
          </div>
        }
      </aside>
    </section>

    @if (loading()) {
      <p class="notice">Loading the latest persisted forecast...</p>
    } @else if (error()) {
      <p class="notice error">{{ error() }}</p>
    } @else if (forecast(); as latest) {
      <section class="dashboard-overview">
        <div>
          <p class="eyebrow">Forecast overview</p>
          <h2>At a glance</h2>
          <p>Seven-day mean and total values from the displayed forecast run.</p>
        </div>
        <span class="dashboard-period">{{ latest.forecastPeriodStart }} to {{ latest.forecastPeriodEnd }}</span>
      </section>

      <section class="weather-metric-grid" aria-label="Seven-day forecast summary">
        <article class="weather-metric rain-metric"><span class="weather-symbol">☔</span><strong>{{ totalRainfall() | number: '1.0-2' }} mm</strong><p>Total forecast rainfall</p></article>
        <article class="weather-metric"><span class="weather-symbol">☀</span><strong>{{ meanTemperature() | number: '1.0-2' }}°C</strong><p>Mean forecast temperature</p></article>
        <article class="weather-metric"><span class="weather-symbol">◌</span><strong>{{ meanHumidity() | number: '1.0-2' }}%</strong><p>Mean forecast humidity</p></article>
      </section>

      <section class="dashboard-forecast-panel" aria-label="Seven day AI weather forecast">
        <div class="dashboard-forecast-heading">
          <div><h2>Seven-day weather forecast</h2><p>Daily predicted conditions from the latest persisted run.</p></div>
          <span>Forecast information</span>
        </div>
        <div class="dashboard-forecast-days">
          @for (day of latest.dailyForecasts; track day.targetDate; let first = $first) {
            <article [class.today]="first">
              <span class="forecast-date">{{ day.targetDate }}</span>
              <span class="weather-symbol">{{ weatherSymbol(day.rainfallMm) }}</span>
              <strong>{{ day.temperatureC | number: '1.0-2' }}°C</strong>
              <span class="rainfall">{{ day.rainfallMm | number: '1.0-2' }} mm rain</span>
              <span class="humidity">{{ day.humidityPercent | number: '1.0-2' }}% humidity</span>
            </article>
          }
        </div>
      </section>

      <section class="dashboard-bottom-grid">
        <article class="panel dashboard-next-step"><p class="eyebrow">Next step</p><h2>Explore explainable crop recommendations</h2><p>The recommendation workflow uses this seven-day forecast with configurable crop requirements and optional soil information.</p><div class="dashboard-steps"><div><b>1</b><p><strong>Choose your soil type, if known</strong><span>Soil is optional and is excluded from the calculation when not supplied.</span></p></div><div><b>2</b><p><strong>Compare the six approved crops</strong><span>Review factor-level information, categories and climate-risk indicators.</span></p></div><div><b>3</b><p><strong>Save your result when signed in</strong><span>Registered Users can keep a recommendation in their history.</span></p></div></div></article>
        <aside class="panel dashboard-context"><p class="eyebrow">Important context</p><h2>Use as decision support</h2><p>Forecasts and crop results support review; they do not replace local agricultural expertise or measured field conditions.</p><div class="crop-cloud" aria-label="Approved crop scope"><span>Paddy</span><span>Maize</span><span>Green Gram</span><span>Cowpea</span><span>Groundnut</span><span>Chilli</span></div><small>District scope only · No planting-time or autonomous farming advice is shown.</small></aside>
      </section>

      @if (latest.limitations.length) {
        <details class="dashboard-limitations"><summary>Forecast limitations and provenance</summary><p>District: {{ latest.district }} · Generated: {{ latest.createdAt }}</p><ul>@for (limitation of latest.limitations; track limitation) { <li>{{ limitation }}</li> }</ul></details>
      }
    } @else {
      <p class="notice warning">No complete persisted forecast is available yet.</p>
    }
  `,
})
export class PublicDashboardComponent implements OnInit {
  readonly forecast = signal<LatestForecastResponse | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly totalRainfall = computed(() => this.forecast()?.dailyForecasts.reduce((total, day) => total + day.rainfallMm, 0) ?? 0);
  readonly meanTemperature = computed(() => average(this.forecast()?.dailyForecasts.map(day => day.temperatureC) ?? []));
  readonly meanHumidity = computed(() => average(this.forecast()?.dailyForecasts.map(day => day.humidityPercent) ?? []));

  constructor(private readonly api: ApiService) {}

  ngOnInit(): void {
    this.api.getLatestForecast().subscribe({
      next: forecast => {
        this.forecast.set(forecast.dailyForecasts.length === 7 ? forecast : null);
        this.error.set(forecast.dailyForecasts.length === 7 ? null : 'The latest forecast response is incomplete.');
        this.loading.set(false);
      },
      error: error => {
        this.forecast.set(null);
        this.error.set(toForecastErrorMessage(error));
        this.loading.set(false);
      },
    });
  }

  weatherSymbol(rainfallMm: number): string {
    if (rainfallMm >= 5) return '🌦';
    if (rainfallMm >= 1) return '⛅';
    return '☀';
  }
}

function average(values: number[]): number {
  return values.length ? values.reduce((total, value) => total + value, 0) / values.length : 0;
}

function toForecastErrorMessage(error: unknown): string {
  if (!(error instanceof FrontendApiError)) {
    return 'The latest forecast could not be loaded.';
  }

  if (error.status === 404) {
    return 'No seven-day AI weather forecast is available yet.';
  }

  if (error.status === 503) {
    return 'Forecast data is temporarily unavailable. Please try again later.';
  }

  return 'The latest forecast could not be loaded.';
}
