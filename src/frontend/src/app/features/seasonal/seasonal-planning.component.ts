import { DecimalPipe } from '@angular/common';
import { Component, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { ApiService } from '../../core/api.service';
import { LatestForecastResponse, SeasonalCropResponse, SeasonalRangeResponse, supportedCrops } from '../../core/api.models';

@Component({
  selector: 'app-seasonal-planning',
  imports: [DecimalPipe, FormsModule, RouterLink],
  template: `
    <section class="page-title seasonal-title">
      <p class="eyebrow">Anuradhapura District</p>
      <h1>Seasonal Crop Planning</h1>
      <p class="seasonal-note">Calendar guidance only</p>
    </section>

    <section class="seasonal-planner" aria-label="Seasonal crop planner">
      <div class="seasonal-tabs" role="tablist" aria-label="Search type">
        <button class="seasonal-tab" type="button" role="tab" [attr.aria-selected]="mode() === 'dates'" (click)="setMode('dates')">Search by dates</button>
        <button class="seasonal-tab" type="button" role="tab" [attr.aria-selected]="mode() === 'crop'" (click)="setMode('crop')">Search by crop</button>
      </div>

      @if (mode() === 'dates') {
        <form class="seasonal-search" (ngSubmit)="findCrops()">
          <label>From <input type="date" name="rangeStart" [(ngModel)]="startDate" required></label>
          <label>To <input type="date" name="rangeEnd" [(ngModel)]="endDate" required></label>
          <button class="button primary" [disabled]="loading()">{{ loading() ? 'Checking…' : 'Check periods' }}</button>
        </form>
      } @else {
        <form class="seasonal-search crop-search" (ngSubmit)="findPeriods()">
          <label>Crop <select name="crop" [(ngModel)]="cropName">@for (crop of crops; track crop) { <option [value]="crop">{{ crop }}</option> }</select></label>
          <label>From <input type="date" name="cropStart" [(ngModel)]="startDate" required></label>
          <label>To <input type="date" name="cropEnd" [(ngModel)]="endDate" required></label>
          <button class="button primary" [disabled]="loading()">{{ loading() ? 'Checking…' : 'Show periods' }}</button>
        </form>
      }
    </section>

    @if (error(); as message) { <p class="notice error" role="alert">{{ message }}</p> }

    @if (rangeResult(); as result) {
      <section class="seasonal-results" aria-live="polite">
        <div class="seasonal-results-heading"><span>Your selected period</span><strong>{{ formatDate(result.startDate) }} – {{ formatDate(result.endDate) }}</strong></div>
        @if (latestForecast(); as forecast) {
          <aside class="seasonal-weather-context" aria-label="Current seven-day weather context">
            <div><span>Current 7-day weather context</span><strong>{{ forecast.forecastPeriodStart }} to {{ forecast.forecastPeriodEnd }}</strong></div>
            <div><span>Rainfall</span><strong>{{ totalRainfall() | number: '1.0-1' }} mm</strong></div>
            <div><span>Mean temperature</span><strong>{{ meanTemperature() | number: '1.0-1' }}°C</strong></div>
            <div><span>Mean humidity</span><strong>{{ meanHumidity() | number: '1.0-1' }}%</strong></div>
            <p>Predicted weather context for decision support; it does not change the calendar result.</p>
          </aside>
        } @else if (forecastUnavailable()) {
          <p class="seasonal-context-note">Current seven-day weather context is unavailable. You can still review the calendar-supported periods below.</p>
        }
        <div class="seasonal-card-grid">
          @for (crop of result.crops; track crop.cropName) {
            <article class="seasonal-crop-card" [class.no-match]="!crop.candidateWindows?.length">
              <h2>{{ crop.cropName }}</h2>
              @if (crop.candidateWindows?.length) {
                <p class="period-status available">Available period</p>
                @for (window of crop.candidateWindows; track window.id) { <p class="period-value">{{ monthNames(window.establishmentMonths) }}</p> }
              } @else {
                <p class="period-status unavailable">No matching period</p>
                @if (crop.alternativeWindows?.length) {
                  <p class="try-label">Try instead</p>
                  @for (window of crop.alternativeWindows; track window.id) { <p class="period-value">{{ monthNames(window.establishmentMonths) }}</p> }
                } @else { <p class="period-value">No alternative period available</p> }
              }
              @if (crop.explanation) { <p class="seasonal-evidence">{{ crop.explanation }}</p> }
              <a routerLink="/recommendations" class="seasonal-recommendation-link">View crop recommendations <span aria-hidden="true">→</span></a>
              <p class="seasonal-card-note">Suitability and approved climate-risk indicators are shown in the full forecast-based recommendation.</p>
            </article>
          }
        </div>
        <p class="seasonal-disclaimer">Seasonal planning is calendar reference information. Final crop suitability uses the current seven-day forecast and configured crop requirements.</p>
      </section>
    }

    @if (cropResult(); as result) {
      <section class="seasonal-results" aria-live="polite">
        <div class="seasonal-results-heading"><span>Available periods</span><strong>{{ result.cropName }}</strong></div>
        @if (result.candidateWindows?.length) {
          <div class="seasonal-window-grid">
            @for (window of result.candidateWindows; track window.id) {
              <article class="seasonal-window-card"><p class="period-status available">Available period</p><h2>{{ monthNames(window.establishmentMonths) }}</h2></article>
            }
          </div>
        } @else {
          <article class="seasonal-empty"><p class="period-status unavailable">No matching period</p>
            @if (result.alternativeWindows?.length) {
              <p class="try-label">Try instead</p>
              @for (window of result.alternativeWindows; track window.id) { <p class="period-value">{{ monthNames(window.establishmentMonths) }}</p> }
            } @else { <p>Try another date range.</p> }
          </article>
        }
      </section>
    }
  `,
})
export class SeasonalPlanningComponent {
  readonly crops = supportedCrops;
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly mode = signal<'dates' | 'crop'>('dates');
  readonly rangeResult = signal<SeasonalRangeResponse | null>(null);
  readonly cropResult = signal<SeasonalCropResponse | null>(null);
  readonly latestForecast = signal<LatestForecastResponse | null>(null);
  readonly forecastUnavailable = signal(false);
  readonly totalRainfall = computed(() => this.latestForecast()?.dailyForecasts.reduce((total, day) => total + day.rainfallMm, 0) ?? 0);
  readonly meanTemperature = computed(() => average(this.latestForecast()?.dailyForecasts.map(day => day.temperatureC) ?? []));
  readonly meanHumidity = computed(() => average(this.latestForecast()?.dailyForecasts.map(day => day.humidityPercent) ?? []));
  startDate = new Date().toISOString().slice(0, 10);
  endDate = new Date(new Date().setMonth(new Date().getMonth() + 3)).toISOString().slice(0, 10);
  cropName = 'Paddy';

  constructor(private readonly api: ApiService) {}

  setMode(mode: 'dates' | 'crop'): void {
    this.mode.set(mode);
    this.error.set(null);
    this.rangeResult.set(null);
    this.cropResult.set(null);
  }

  findCrops(): void {
    this.run(this.api.findRecommendedCropsByDateRange(this.startDate, this.endDate, null), value => {
      this.rangeResult.set(value);
      this.loadLatestForecast();
    });
  }

  findPeriods(): void {
    this.run(this.api.findSuitablePeriodsByCrop(this.cropName, this.startDate, this.endDate, null), value => this.cropResult.set(value));
  }

  monthNames(months: number[]): string {
    return months.map(month => new Intl.DateTimeFormat('en', { month: 'long', timeZone: 'UTC' }).format(new Date(Date.UTC(2026, month - 1, 1)))).join(' – ');
  }

  formatDate(value: string): string {
    return new Intl.DateTimeFormat('en', { day: 'numeric', month: 'short', year: 'numeric', timeZone: 'UTC' }).format(new Date(`${value}T00:00:00Z`));
  }

  private run<T>(request: Observable<T>, setResult: (value: T) => void): void {
    this.loading.set(true);
    this.error.set(null);
    this.rangeResult.set(null);
    this.cropResult.set(null);
    this.latestForecast.set(null);
    this.forecastUnavailable.set(false);
    request.pipe(finalize(() => this.loading.set(false))).subscribe({ next: setResult, error: error => this.error.set(error.message) });
  }

  private loadLatestForecast(): void {
    this.latestForecast.set(null);
    this.forecastUnavailable.set(false);
    this.api.getLatestForecast().subscribe({
      next: forecast => {
        this.latestForecast.set(forecast.dailyForecasts.length === 7 ? forecast : null);
        this.forecastUnavailable.set(forecast.dailyForecasts.length !== 7);
      },
      error: () => this.forecastUnavailable.set(true),
    });
  }
}

function average(values: number[]): number {
  return values.length ? values.reduce((total, value) => total + value, 0) / values.length : 0;
}
