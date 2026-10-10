import { DecimalPipe } from '@angular/common';
import { Component, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { ApiService, FrontendApiError } from '../../core/api.service';
import { RecommendationHistorySummaryResponse } from '../../core/api.models';

@Component({
  selector: 'app-registered-history-blocked',
  imports: [DecimalPipe, RouterLink],
  template: `
    <section class="page-title">
      <p class="eyebrow">Registered User</p>
      <h1>Recommendation History</h1>
      <p>Your saved recommendation history is loaded from the authenticated backend history endpoint.</p>
    </section>

    @if (error(); as message) {
      <div class="notice error" role="alert">{{ message }}</div>
    }

    @if (isLoading()) {
      <div class="notice" role="status">Loading recommendation history...</div>
    } @else if (!items().length && !error()) {
      <article class="panel">
        <h2>No saved recommendations</h2>
        <p>Generate a recommendation while logged in to save it to your account history.</p>
        <a routerLink="/recommendations" class="button primary">Generate a recommendation</a>
      </article>
    }

    @if (items().length) {
      <section class="table-panel">
        <table>
          <thead>
            <tr>
              <th>Created</th>
              <th>Forecast run</th>
              <th>Soil</th>
              <th>Top crop</th>
              <th>Score</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (item of items(); track item.recommendationId) {
              <tr>
                <td>{{ item.createdAt }}</td>
                <td><code>{{ item.forecastRunId }}</code></td>
                <td>{{ item.soilType || 'Not supplied' }}</td>
                <td>{{ item.topCropName || 'Unranked' }} {{ item.topSuitabilityCategory ? '(' + item.topSuitabilityCategory + ')' : '' }}</td>
                <td>{{ item.topOverallScore !== null ? (item.topOverallScore | number: '1.0-2') : 'No score' }}</td>
                <td><a [routerLink]="['/history', item.recommendationId]">Open</a></td>
              </tr>
            }
          </tbody>
        </table>
      </section>

      <div class="action-row">
        <button type="button" class="button secondary" (click)="changePage(-1)" [disabled]="page() <= 1 || isLoading()">Previous</button>
        <span>Page {{ page() }} of {{ totalPages() }}</span>
        <button type="button" class="button secondary" (click)="changePage(1)" [disabled]="page() >= totalPages() || isLoading()">Next</button>
      </div>
    }
  `,
})
export class RegisteredHistoryBlockedComponent {
  readonly items = signal<RecommendationHistorySummaryResponse[]>([]);
  readonly page = signal(1);
  readonly pageSize = 10;
  readonly totalCount = signal(0);
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);

  constructor(private readonly api: ApiService) {
    this.load();
  }

  totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount() / this.pageSize));
  }

  changePage(delta: number): void {
    const nextPage = this.page() + delta;
    if (nextPage < 1 || nextPage > this.totalPages()) {
      return;
    }

    this.page.set(nextPage);
    this.load();
  }

  private load(): void {
    this.isLoading.set(true);
    this.error.set(null);
    this.api
      .listRecommendationHistory(this.page(), this.pageSize)
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: page => {
          this.items.set(page.items);
          this.totalCount.set(page.totalCount);
        },
        error: (error: FrontendApiError) => {
          this.items.set([]);
          this.totalCount.set(0);
          this.error.set(error.message);
        },
      });
  }
}
