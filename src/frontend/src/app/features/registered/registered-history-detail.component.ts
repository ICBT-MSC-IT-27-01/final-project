import { DecimalPipe } from '@angular/common';
import { Component, computed, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { ApiService, FrontendApiError } from '../../core/api.service';
import { RecommendationHistoryDetailResponse } from '../../core/api.models';

@Component({
  selector: 'app-registered-history-detail',
  imports: [DecimalPipe, RouterLink],
  template: `
    <section class="page-title">
      <p class="eyebrow">Registered User</p>
      <h1>Saved Recommendation #{{ recommendationId() }}</h1>
      <p>Historical recommendations are displayed from stored evidence snapshots and are not recalculated in the browser.</p>
    </section>

    @if (error(); as message) {
      <div class="notice error" role="alert">{{ message }}</div>
    }

    @if (isLoading()) {
      <div class="notice" role="status">Loading saved recommendation...</div>
    }

    @if (detail(); as item) {
      <section class="forecast-strip">
        <div><span>Forecast run</span><strong>{{ item.forecastRunId }}</strong></div>
        <div><span>Created</span><strong>{{ item.createdAt }}</strong></div>
        <div><span>Soil</span><strong>{{ item.soilType || 'Not supplied' }}</strong></div>
        <div><span>Forecast period</span><strong>{{ forecastPeriod(item) }}</strong></div>
      </section>

      @if (item.limitations.length) {
        <div class="notice warning">
          @for (limitation of item.limitations; track limitation) {
            <p>{{ limitation }}</p>
          }
        </div>
      }

      @if (item.forecast?.days?.length) {
        <section class="forecast-days" aria-label="Stored forecast days">
          @for (day of item.forecast!.days; track day.targetDate) {
            <article>
              <strong>{{ day.targetDate }}</strong>
              <span>Rainfall: {{ valueOrMissing(day.rainfall) }}</span>
              <span>Temp: {{ valueOrMissing(day.temperature) }}</span>
              <span>Humidity: {{ valueOrMissing(day.humidity) }}</span>
            </article>
          }
        </section>
      }

      <section class="crop-grid">
        @for (crop of item.crops; track crop.cropName) {
          <article class="crop-card">
            <header>
              <span class="rank">{{ crop.rank ? '#' + crop.rank : 'Unranked' }}</span>
              <h2>{{ crop.cropName }}</h2>
            </header>
            <p class="score">{{ crop.overallScore !== null ? (crop.overallScore | number: '1.0-2') : 'No score' }}</p>
            <p>{{ crop.suitabilityCategory || crop.evaluationStatus }}</p>
            <p>{{ crop.explanation }}</p>
            @if (crop.unavailableFactors.length) {
              <p class="missing">Missing: {{ crop.unavailableFactors.join(', ') }}</p>
            }
            @if (crop.climateRisks.length) {
              <p class="risks">Risks: {{ crop.climateRisks.join(', ') }}</p>
            }
            <div class="factor-list">
              @for (factor of crop.factors; track factor.factor) {
                <div>
                  <span>{{ factor.factor }}</span>
                  <strong>{{ factor.isEvaluable ? (factor.score ?? 'No score') : 'Missing' }}</strong>
                </div>
              }
            </div>
          </article>
        }
      </section>

      <p><a routerLink="/history">Back to history</a></p>
    }
  `,
})
export class RegisteredHistoryDetailComponent {
  readonly detail = signal<RecommendationHistoryDetailResponse | null>(null);
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);
  readonly recommendationId = computed(() => Number(this.route.snapshot.paramMap.get('id')));

  constructor(
    private readonly route: ActivatedRoute,
    private readonly api: ApiService,
  ) {
    this.load();
  }

  forecastPeriod(item: RecommendationHistoryDetailResponse): string {
    if (!item.forecast?.targetStartDate || !item.forecast.targetEndDate) {
      return 'Stored period unavailable';
    }

    return `${item.forecast.targetStartDate} to ${item.forecast.targetEndDate}`;
  }

  valueOrMissing(value: number | null): string {
    return value === null ? 'Missing' : String(value);
  }

  private load(): void {
    this.isLoading.set(true);
    this.error.set(null);
    this.api
      .getRecommendationHistoryDetail(this.recommendationId())
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: detail => this.detail.set(detail),
        error: (error: FrontendApiError) => {
          this.detail.set(null);
          this.error.set(error.message);
        },
      });
  }
}
