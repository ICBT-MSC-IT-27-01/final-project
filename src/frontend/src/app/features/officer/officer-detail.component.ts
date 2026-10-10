import { DecimalPipe } from '@angular/common';
import { Component, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { ApiService } from '../../core/api.service';
import { RecommendationReviewResponse, validationStatuses } from '../../core/api.models';

@Component({
  selector: 'app-officer-detail',
  imports: [DecimalPipe, FormsModule],
  template: `
    <section class="page-title">
      <p class="eyebrow">Officer review</p>
      <h1>Recommendation #{{ recommendationId() }}</h1>
      <p>Validation metadata is recorded separately from the original AI recommendation.</p>
    </section>

    @if (error(); as message) {
      <div class="notice error" role="alert">{{ message }}</div>
    }
    @if (success(); as message) {
      <div class="notice success" role="status">{{ message }}</div>
    }

    @if (review(); as item) {
      <section class="forecast-strip">
        <div><span>Forecast run</span><strong>{{ item.forecastRunId }}</strong></div>
        <div><span>Created</span><strong>{{ item.createdAt }}</strong></div>
        <div><span>Soil</span><strong>{{ item.soilType || 'Not supplied' }}</strong></div>
        <div><span>User</span><strong>{{ item.userId ?? 'Public' }}</strong></div>
      </section>

      <section class="crop-grid">
        @for (crop of item.crops; track crop.cropId) {
          <article class="crop-card">
            <header>
              <span class="rank">{{ crop.rank ? '#' + crop.rank : 'Unranked' }}</span>
              <h2>{{ crop.cropName }}</h2>
            </header>
            <p class="score">{{ crop.overallScore !== null ? (crop.overallScore | number: '1.0-2') : 'No score' }}</p>
            <p>{{ crop.suitabilityCategory || crop.evaluationStatus }}</p>
            <p>{{ crop.explanation }}</p>
          </article>
        }
      </section>

      <form class="panel form-panel" (ngSubmit)="submit()">
        <h2>Submit validation</h2>
        <label>
          Status
          <select name="status" [(ngModel)]="status">
            @for (option of statuses; track option) {
              <option [value]="option">{{ option }}</option>
            }
          </select>
        </label>
        <label>
          Comments
          <textarea name="comment" [(ngModel)]="comment" maxlength="1000" rows="4" placeholder="Optional"></textarea>
        </label>
        <button type="submit" class="button primary" [disabled]="isSaving()">Submit validation</button>
      </form>

      <section class="table-panel">
        <h2>Validation history</h2>
        @if (!item.validationHistory.length) {
          <p>No validation record has been submitted yet.</p>
        } @else {
          @for (entry of item.validationHistory; track entry.id) {
            <article class="history-entry">
              <strong>{{ entry.status }}</strong>
              <span>{{ entry.createdAt }}</span>
              <p>{{ entry.comment || 'No comment supplied.' }}</p>
            </article>
          }
        }
      </section>
    }
  `,
})
export class OfficerDetailComponent {
  readonly statuses = validationStatuses;
  readonly review = signal<RecommendationReviewResponse | null>(null);
  readonly isLoading = signal(false);
  readonly isSaving = signal(false);
  readonly error = signal<string | null>(null);
  readonly success = signal<string | null>(null);
  readonly recommendationId = computed(() => Number(this.route.snapshot.paramMap.get('id')));
  status = 'Pending Validation';
  comment = '';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly api: ApiService,
  ) {
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.error.set(null);
    this.api
      .getRecommendationReview(this.recommendationId())
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: review => this.review.set(review),
        error: error => this.error.set(error.message),
      });
  }

  submit(): void {
    this.isSaving.set(true);
    this.error.set(null);
    this.success.set(null);
    this.api
      .submitRecommendationValidation(this.recommendationId(), {
        status: this.status,
        comment: this.comment.trim() || null,
      })
      .pipe(finalize(() => this.isSaving.set(false)))
      .subscribe({
        next: () => {
          this.success.set('Validation submitted.');
          this.load();
        },
        error: error => this.error.set(error.message),
      });
  }
}
