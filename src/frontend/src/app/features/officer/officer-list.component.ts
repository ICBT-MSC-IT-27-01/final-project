import { Component, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { ApiService } from '../../core/api.service';
import { RecommendationReviewSummaryResponse } from '../../core/api.models';

@Component({
  selector: 'app-officer-list',
  imports: [RouterLink],
  template: `
    <section class="page-title">
      <p class="eyebrow">Agricultural Officer</p>
      <h1>Pending Recommendation Reviews</h1>
      <p>Officer validation is separate from AI scoring and cannot edit crop rankings or evidence.</p>
    </section>

    <button type="button" class="button secondary" (click)="load()" [disabled]="isLoading()">Refresh</button>

    @if (error(); as message) {
      <div class="notice error" role="alert">{{ message }}</div>
    }

    <section class="table-panel" aria-label="Pending reviews">
      @if (isLoading()) {
        <p>Loading pending reviews...</p>
      } @else if (!reviews().length) {
        <p>No pending recommendations are currently available for officer review.</p>
      } @else {
        <table>
          <thead>
            <tr>
              <th>Recommendation</th>
              <th>Forecast Run</th>
              <th>Soil</th>
              <th>Status</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (review of reviews(); track review.recommendationId) {
              <tr>
                <td>#{{ review.recommendationId }}</td>
                <td>{{ review.forecastRunId }}</td>
                <td>{{ review.soilType || 'Not supplied' }}</td>
                <td>{{ review.latestValidationStatus || 'Pending Validation' }}</td>
                <td><a [routerLink]="['/officer/recommendations', review.recommendationId]">Review</a></td>
              </tr>
            }
          </tbody>
        </table>
      }
    </section>
  `,
})
export class OfficerListComponent {
  readonly reviews = signal<RecommendationReviewSummaryResponse[]>([]);
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);

  constructor(private readonly api: ApiService) {
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.error.set(null);
    this.api
      .listPendingValidations()
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: reviews => this.reviews.set(reviews),
        error: error => this.error.set(error.message),
      });
  }
}
