import { DecimalPipe, NgClass } from '@angular/common';
import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { ApiService } from '../../core/api.service';
import { RecommendationResponse, supportedCrops } from '../../core/api.models';

@Component({
  selector: 'app-recommendation-page',
  imports: [DecimalPipe, FormsModule, NgClass],
  template: `
    <section class="page-title">
      <p class="eyebrow">Public and registered workflow</p>
      <h1>Crop Recommendation</h1>
      <p>Backend-generated rankings, evidence coverage and forecast provenance are shown as returned.</p>
    </section>

    <form class="panel form-panel" (ngSubmit)="submit()">
      <label>
        District
        <input name="district" [(ngModel)]="district" readonly aria-describedby="district-scope" />
      </label>
      <small id="district-scope">Only Anuradhapura District is supported.</small>

      <label>
        Soil type
        <input
          name="soilType"
          [(ngModel)]="soilType"
          maxlength="100"
          placeholder="Optional; leave blank if unknown"
        />
      </label>

      <button type="submit" class="button primary" [disabled]="isLoading()">
        {{ isLoading() ? 'Generating...' : 'Generate recommendation' }}
      </button>
    </form>

    @if (error(); as message) {
      <div class="notice error" role="alert">{{ message }}</div>
    }

    @if (recommendation(); as result) {
      <section class="forecast-strip" aria-label="Forecast provenance">
        <div>
          <span>Recommendation</span>
          <strong>#{{ result.recommendationId }}</strong>
        </div>
        <div>
          <span>Forecast run</span>
          <strong>{{ result.forecastRunId }}</strong>
        </div>
        <div>
          <span>Forecast period</span>
          <strong>{{ result.forecast.targetStartDate }} to {{ result.forecast.targetEndDate }}</strong>
        </div>
        <div>
          <span>Model</span>
          <strong>{{ result.forecast.modelVersion || 'Not provided' }}</strong>
        </div>
      </section>

      <section class="forecast-days" aria-label="Seven day forecast">
        @for (day of result.forecast.days; track day.targetDate) {
          <article>
            <strong>{{ day.targetDate }}</strong>
            <span>{{ day.rainfall | number: '1.0-2' }} mm</span>
            <span>{{ day.temperature | number: '1.0-2' }} deg C</span>
            <span>{{ day.humidity | number: '1.0-2' }}%</span>
          </article>
        }
      </section>

      <section class="crop-grid" aria-label="Crop recommendations">
        @for (crop of result.crops; track crop.cropName) {
          <article class="crop-card" [ngClass]="crop.evaluationStatus">
            <header>
              <span class="rank">{{ crop.rank ? '#' + crop.rank : 'Unranked' }}</span>
              <h2>{{ crop.cropName }}</h2>
            </header>
            <p class="score">
              @if (crop.overallScore !== null) {
                {{ crop.overallScore | number: '1.0-2' }}
              } @else {
                No score
              }
            </p>
            <p>{{ crop.suitabilityCategory || crop.evaluationStatus }}</p>
            <p>{{ crop.explanation }}</p>

            @if (crop.unavailableFactors.length) {
              <div class="missing">Missing: {{ crop.unavailableFactors.join(', ') }}</div>
            }

            <div class="factor-list">
              @for (factor of crop.factors; track factor.factor) {
                <div>
                  <span>{{ factor.factor }}</span>
                  <strong>{{ factor.isEvaluable ? (factor.score | number: '1.0-2') : 'Unavailable' }}</strong>
                </div>
              }
            </div>

            @if (crop.climateRisks.length) {
              <div class="risks">{{ crop.climateRisks.join(', ') }}</div>
            }
          </article>
        }
      </section>

      @if (result.crops.length !== expectedCropCount) {
        <div class="notice warning">Expected six approved crops; backend returned {{ result.crops.length }}.</div>
      }
    }
  `,
})
export class RecommendationPageComponent {
  readonly expectedCropCount = supportedCrops.length;
  readonly recommendation = signal<RecommendationResponse | null>(null);
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);
  district = 'Anuradhapura District';
  soilType = '';

  constructor(private readonly api: ApiService) {}

  submit(): void {
    this.isLoading.set(true);
    this.error.set(null);
    this.recommendation.set(null);
    this.api
      .createRecommendation({
        district: this.district,
        soilType: this.soilType.trim() || null,
      })
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: result => this.recommendation.set(result),
        error: error => this.error.set(error.message),
      });
  }
}
