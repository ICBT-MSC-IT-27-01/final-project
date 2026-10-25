import { DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService, FrontendApiError } from '../../core/api.service';
import { LatestForecastResponse } from '../../core/api.models';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-public-dashboard',
  imports: [DecimalPipe, RouterLink],
  template: `
    @if (auth.isAuthenticated()) {
      <section class="workspace-welcome"><div><p class="eyebrow">ANURADHAPURA DISTRICT</p><h1>Weather overview</h1><p>Review the latest seven-day forecast and continue to crop suitability, seasonal planning, or your role workspace.</p></div><a routerLink="/recommendations" class="button primary">Open recommendations →</a></section>
    } @else {
    <section class="landing-hero" aria-labelledby="landing-title">
      <div class="landing-orbit landing-orbit-one" aria-hidden="true"></div>
      <div class="landing-orbit landing-orbit-two" aria-hidden="true"></div>
      <div class="landing-hero-inner">
        <div class="landing-copy">
          <span class="landing-pill"><span class="landing-pill-dot"></span> ANURADHAPURA DISTRICT · SRI LANKA</span>
          <h1 id="landing-title">Clearer weather insight.<br><em>More informed crop decisions.</em></h1>
          <p>Explore a seven-day AI weather forecast and transparent crop suitability results for six approved crops. Built to support decisions with evidence you can review.</p>
          <div class="landing-actions"><a routerLink="/recommendations" class="landing-primary">Explore recommendations <span aria-hidden="true">↗</span></a><a routerLink="/seasonal-planning" class="landing-secondary">Seasonal planning <span aria-hidden="true">→</span></a></div>
          <div class="landing-trust"><span><b>07</b> day forecast</span><span><b>06</b> approved crops</span><span><b>01</b> district focus</span></div>
        </div>
        <aside class="landing-preview" aria-label="Latest forecast preview">
          <div class="preview-topline"><span>FORECAST INTELLIGENCE</span><span class="preview-signal" [class.is-ready]="!!forecast()"><i></i>{{ forecast() ? 'Forecast ready' : loading() ? 'Checking forecast' : 'Forecast unavailable' }}</span></div>
          <div class="preview-heading"><div><small>Current outlook</small><h2>Seven-day forecast</h2></div><span class="preview-compass" aria-hidden="true">✳</span></div>
          @if (forecast(); as latest) {
            <p class="preview-period">{{ latest.forecastPeriodStart }} – {{ latest.forecastPeriodEnd }}</p>
            <div class="preview-metrics"><div><span>Rainfall total</span><strong>{{ totalRainfall() | number: '1.0-1' }} <small>mm</small></strong></div><div><span>Mean temperature</span><strong>{{ meanTemperature() | number: '1.0-1' }} <small>°C</small></strong></div></div>
            <div class="preview-days">@for (day of latest.dailyForecasts.slice(0, 4); track day.targetDate) { <div><span>{{ day.targetDate }}</span><strong>{{ day.temperatureC | number: '1.0-1' }}°</strong></div> }</div>
            <p class="preview-footnote">AI-predicted weather · {{ latest.modelVersion || 'Model version unavailable' }} · {{ latest.freshnessStatus }}</p>
          } @else {
            <div class="preview-empty"><span aria-hidden="true">◌</span><p>{{ loading() ? 'Loading the latest forecast…' : 'The latest seven-day forecast is not available yet.' }}</p></div>
          }
        </aside>
      </div>
    </section>

    <section class="landing-capabilities" aria-label="Platform capabilities">
      <article><span class="capability-icon" aria-hidden="true">↗</span><div><h2>Forecast with context</h2><p>Review predicted rainfall, temperature, and humidity for the coming seven days.</p></div></article>
      <article><span class="capability-icon" aria-hidden="true">◎</span><div><h2>Explainable suitability</h2><p>See the factors behind each crop result, with approved climate-risk indicators.</p></div></article>
      <article><span class="capability-icon" aria-hidden="true">▤</span><div><h2>Role-based workspaces</h2><p>Registered users, officers, and administrators get tools for their responsibilities.</p></div></article>
    </section>
    }

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

      <p class="dashboard-provenance">Forecast run {{ latest.forecastRunId }} · Generated {{ latest.createdAt }} · Status: {{ latest.freshnessStatus }} · Model: {{ latest.modelVersion || 'Not provided' }}</p>

      <section class="weather-metric-grid" aria-label="Seven-day forecast summary">
        <article class="weather-metric rain-metric"><span class="weather-symbol" aria-hidden="true">↘</span><strong>{{ totalRainfall() | number: '1.0-2' }} mm</strong><p>Total forecast rainfall</p></article>
        <article class="weather-metric"><span class="weather-symbol" aria-hidden="true">°</span><strong>{{ meanTemperature() | number: '1.0-2' }}°C</strong><p>Mean forecast temperature</p></article>
        <article class="weather-metric"><span class="weather-symbol" aria-hidden="true">%</span><strong>{{ meanHumidity() | number: '1.0-2' }}%</strong><p>Mean forecast humidity</p></article>
      </section>

      <section class="dashboard-forecast-panel" aria-label="Seven day AI weather forecast">
        <div class="dashboard-forecast-heading">
          <div><h2>Seven-day weather forecast</h2><p>Daily predicted conditions from the latest persisted run.</p></div>
          <span>Forecast information</span>
        </div>
        <div class="dashboard-forecast-days">
          @for (day of latest.dailyForecasts; track day.targetDate; let first = $first; let index = $index) {
            <article [class.today]="first">
              <span class="forecast-date">{{ day.targetDate }}</span>
              <span class="forecast-day-index">DAY {{ index + 1 }}</span>
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

  constructor(private readonly api: ApiService, public readonly auth: AuthService) {}

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
