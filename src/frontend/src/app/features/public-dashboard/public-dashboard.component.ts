import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService, FrontendApiError } from '../../core/api.service';
import { LatestForecastResponse, supportedCrops } from '../../core/api.models';
import { firstForecastDayAlreadyStarted } from '../../core/forecast-timing';

@Component({
  selector: 'app-public-dashboard',
  imports: [DatePipe, DecimalPipe, RouterLink],
  template: `
    <section class="dashboard-hero">
      <div class="hero-content">
        <p class="hero-kicker"><span class="hero-kicker-dot"></span> Anuradhapura District · Sri Lanka</p>
        <h1>Better decisions start with a clearer forecast.</h1>
        <p class="hero-description">Explore seven-day AI weather predictions and see how the evidence informs crop decisions for the six supported crops.</p>
        <div class="action-row">
          <a routerLink="/recommendations" class="button hero-button">Generate recommendation <span aria-hidden="true">↗</span></a>
          <a routerLink="/seasonal-planning" class="button hero-secondary">Seasonal crop planning</a>
        </div>
        <p class="hero-note">Forecasts support decisions; local agricultural advice still matters.</p>
      </div>
      <aside class="hero-weather" aria-label="Forecast at a glance">
        <div class="hero-weather-head">
          <span class="weather-icon" aria-hidden="true">
            <svg viewBox="0 0 64 64" fill="none" focusable="false">
              <path d="M8 43h48M9 35l10-9 10 6 12-16 13 6" stroke="currentColor" stroke-width="2.7" stroke-linecap="round" stroke-linejoin="round"/>
              <circle cx="9" cy="35" r="3" fill="currentColor"/><circle cx="29" cy="32" r="3" fill="currentColor"/><circle cx="41" cy="16" r="3" fill="currentColor"/><circle cx="54" cy="22" r="3" fill="currentColor"/>
            </svg>
          </span>
          <span>FORECAST AT A GLANCE</span>
        </div>
        @if (forecast(); as latest) {
          @if (featuredDay(); as selectedDay) {
            <div class="hero-weather-date">{{ selectedDay.targetDate }}</div>
            <div class="hero-temperature">{{ selectedDay.temperatureC | number: '1.0-1' }}<span>°C</span></div>
            <p class="hero-weather-caption">Mean temperature · next forecast day</p>
            <div class="hero-weather-metrics">
              <div><span>Rainfall</span><strong>{{ selectedDay.rainfallMm | number: '1.0-2' }} mm</strong></div>
              <div><span>Humidity</span><strong>{{ selectedDay.humidityPercent | number: '1.0-1' }}%</strong></div>
            </div>
          } @else {
            <div class="hero-weather-empty">No upcoming forecast day</div>
            <p class="hero-weather-caption">The saved forecast period has passed.</p>
          }
          <div class="hero-weather-foot"><span class="pulse-dot"></span> AI forecast · {{ latest.forecastPeriodStart }} to {{ latest.forecastPeriodEnd }}</div>
        } @else {
          <div class="hero-weather-empty">{{ loading() ? 'Loading forecast data…' : 'Forecast preview unavailable' }}</div>
          <p class="hero-weather-caption">The full forecast status appears below.</p>
        }
      </aside>
    </section>

    <section class="dashboard-section" aria-labelledby="forecast-heading">
      <div class="section-heading">
        <div><p class="eyebrow">WEATHER OUTLOOK</p><h2 id="forecast-heading">Latest AI Weather Forecast</h2><p>Seven daily predictions for Anuradhapura District. Check the dates before using them.</p></div>
        <span class="section-tag">7-day outlook</span>
      </div>
      @if (loading()) {
        <div class="panel state-panel" role="status">Loading the latest persisted forecast...</div>
      } @else if (error()) {
        <div class="panel state-panel notice error" role="alert">{{ error() }}</div>
      } @else if (forecast(); as latest) {
        <div class="forecast-summary" aria-label="Forecast provenance">
          <div><span>District</span><strong>{{ latest.district }}</strong></div>
          <div><span>Forecast period</span><strong>{{ latest.forecastPeriodStart }} to {{ latest.forecastPeriodEnd }}</strong></div>
          <div><span>Generated</span><strong>{{ latest.createdAt | date: 'd MMM y, h:mm a' : '+0530' }} (Sri Lanka)</strong></div>
          <div><span>Model version</span><strong>{{ latest.modelVersion || 'Not recorded' }}</strong></div>
        </div>
        @if (firstForecastDayAlreadyStarted(latest.createdAt, latest.forecastPeriodStart)) {
          <div class="notice warning" role="status">This forecast was generated after its first listed day had begun. It does not provide seven complete future days from its issue time. Check the dates before using it.</div>
        }
        @if (!featuredDay()) {
          <div class="notice warning" role="status">This forecast period has ended. Wait for a newer forecast before using the crop assessment.</div>
        }
        <div class="forecast-days" aria-label="Seven day AI weather forecast">
          @for (day of latest.dailyForecasts; track day.targetDate; let index = $index) {
            <article class="day-card" [class.first-day]="day.targetDate === featuredDay()?.targetDate">
              <div class="day-card-top"><span>DAY {{ index + 1 | number: '2.0-0' }}</span><span class="day-symbol" aria-hidden="true">·</span></div>
              <strong class="day-date">{{ day.targetDate }}</strong>
              <div class="day-temperature">{{ day.temperatureC | number: '1.0-1' }}<span>°C</span></div>
              <span class="day-temp-label">{{ day.temperatureC | number: '1.0-2' }} deg C mean temperature</span>
              <div class="day-divider"></div>
              <div class="day-metric"><span>Rainfall</span><strong>{{ day.rainfallMm | number: '1.0-2' }} mm rainfall</strong></div>
              <div class="day-metric"><span>Humidity</span><strong>{{ day.humidityPercent | number: '1.0-2' }}% mean humidity</strong></div>
            </article>
          }
        </div>
        <p class="forecast-caption">These are AI-predicted weather forecasts from persisted backend data, not measured observations or live provider readings.</p>
      } @else {
        <div class="panel state-panel notice warning">No complete persisted forecast is available yet.</div>
      }
    </section>

    <section class="dashboard-lower grid-two" aria-label="Crop and forecast information">
      <article class="panel crop-overview">
        <div class="panel-heading"><div><p class="eyebrow">IN SCOPE</p><h2>Supported Crops</h2></div><span class="count-badge">06</span></div>
        <p>Explore evidence for the six crops included in this research prototype.</p>
        <div class="crop-list">
          @for (crop of crops; track crop; let index = $index) {
            <div class="crop-list-item"><span class="crop-number">{{ index + 1 | number: '2.0-0' }}</span><strong>{{ crop }}</strong></div>
          }
        </div>
        <a routerLink="/recommendations" class="text-link">View crop recommendations <span aria-hidden="true">→</span></a>
      </article>
      <article class="panel evidence-overview">
        <div class="panel-heading"><div><p class="eyebrow">TRANSPARENCY</p><h2>How to read this forecast</h2></div><span class="info-symbol" aria-hidden="true">i</span></div>
        <p>The forecast is one input to the separate, explainable crop suitability assessment.</p>
        <div class="evidence-row"><span class="evidence-step">01</span><div><strong>Weather model</strong><p>Predicts rainfall, mean temperature and humidity for seven days.</p></div></div>
        <div class="evidence-row"><span class="evidence-step">02</span><div><strong>Crop assessment</strong><p>Compares forecast values with configured crop requirements and explains the factors.</p></div></div>
        @if (forecast(); as latest) {
          <div class="evidence-limits">
            <strong>Forecast limitations</strong>
            <ul class="evidence-list">
              <li>Forecast selected by the system: {{ latest.freshnessStatus }}</li>
              @for (limitation of latest.limitations; track limitation) { <li>{{ limitation }}</li> }
            </ul>
          </div>
        }
      </article>
    </section>
  `,
})
export class PublicDashboardComponent implements OnInit {
  readonly firstForecastDayAlreadyStarted = firstForecastDayAlreadyStarted;
  readonly crops = supportedCrops;
  readonly forecast = signal<LatestForecastResponse | null>(null);
  readonly featuredDay = computed(() => {
    const today = new Intl.DateTimeFormat('en-CA', {
      timeZone: 'Asia/Colombo', year: 'numeric', month: '2-digit', day: '2-digit',
    }).format(new Date());
    return this.forecast()?.dailyForecasts.find(day => day.targetDate >= today) ?? null;
  });
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
  if (!(error instanceof FrontendApiError)) return 'The latest forecast could not be loaded.';
  if (error.status === 404) return 'No seven-day AI weather forecast is available yet.';
  if (error.status === 503) return 'Forecast data is temporarily unavailable. Please try again later.';
  return 'The latest forecast could not be loaded.';
}
