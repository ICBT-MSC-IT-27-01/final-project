import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService, FrontendApiError } from '../../core/api.service';
import { LatestForecastResponse } from '../../core/api.models';

@Component({
  selector: 'app-public-dashboard',
  imports: [RouterLink],
  template: `
    <section class="dashboard-hero">
      <div>
        <p class="eyebrow">Anuradhapura District only</p>
        <h1>Seven-day weather context for explainable crop recommendations.</h1>
        <p>
          The frontend presents backend-generated forecast provenance and crop evidence without
          inventing live weather inputs or agricultural scores.
        </p>
        <div class="action-row">
          <a routerLink="/recommendations" class="button primary">Generate recommendation</a>
          <a routerLink="/login" class="button secondary">Registered login</a>
        </div>
      </div>
      <aside class="status-board" aria-label="Backend readiness">
        <span class="status ready">Ready</span>
        <strong>Recommendation API</strong>
        <p>Uses the latest complete persisted seven-day forecast run selected by the backend.</p>
      </aside>
    </section>

    <section class="grid-two forecast-dashboard">
      <article class="panel">
        <h2>Latest AI Weather Forecast</h2>
        @if (loading()) {
          <p class="notice">Loading the latest persisted forecast...</p>
        } @else if (error()) {
          <p class="notice error">{{ error() }}</p>
        } @else if (forecast(); as latest) {
          <div class="forecast-strip" aria-label="Forecast provenance">
            <div>
              <span>District</span>
              <strong>{{ latest.district }}</strong>
            </div>
            <div>
              <span>Forecast period</span>
              <strong>{{ latest.forecastPeriodStart }} to {{ latest.forecastPeriodEnd }}</strong>
            </div>
            <div>
              <span>Generated</span>
              <strong>{{ latest.createdAt }}</strong>
            </div>
            <div>
              <span>Model</span>
              <strong>{{ latest.modelVersion || 'Not recorded' }}</strong>
            </div>
          </div>

          <p>
            These are AI-predicted weather forecasts from persisted backend data, not measured
            observations or live provider readings.
          </p>

          <div class="forecast-days" aria-label="Seven day AI weather forecast">
            @for (day of latest.dailyForecasts; track day.targetDate) {
              <article>
                <strong>{{ day.targetDate }}</strong>
                <span>{{ day.rainfallMm }} mm rainfall</span>
                <span>{{ day.temperatureC }} deg C mean temperature</span>
                <span>{{ day.humidityPercent }}% mean humidity</span>
              </article>
            }
          </div>

          <ul class="evidence-list">
            <li>Freshness status: {{ latest.freshnessStatus }}</li>
            @for (limitation of latest.limitations; track limitation) {
              <li>{{ limitation }}</li>
            }
          </ul>
        } @else {
          <p class="notice warning">No complete persisted forecast is available yet.</p>
        }
      </article>

      <article class="panel">
        <h2>Supported Crops</h2>
        <div class="crop-cloud" aria-label="Approved crop scope">
          <span>Paddy</span>
          <span>Maize</span>
          <span>Green Gram</span>
          <span>Cowpea</span>
          <span>Groundnut</span>
          <span>Chilli</span>
        </div>
      </article>
    </section>
  `,
})
export class PublicDashboardComponent implements OnInit {
  readonly forecast = signal<LatestForecastResponse | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

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
