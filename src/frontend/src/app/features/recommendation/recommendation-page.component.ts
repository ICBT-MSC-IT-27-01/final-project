import { DecimalPipe } from '@angular/common';
import { Component, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize, map } from 'rxjs/operators';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import {
  RecommendationCropResponse,
  RecommendationEvaluationResponse,
  RecommendationResponse,
  supportedCrops,
} from '../../core/api.models';

@Component({
  selector: 'app-recommendation-page',
  imports: [DecimalPipe, FormsModule],
  template: `
    <section class="page-title recommendation-title">
      <p class="eyebrow">Anuradhapura District</p>
      <h1>Crop Recommendation</h1>
      <p>See the crops assessed from the latest seven-day weather forecast.</p>
    </section>

    <form class="panel recommendation-search" (ngSubmit)="submit()">
      <div class="search-intro">
        <div>
          <h2>Find recommendations</h2>
          <p>The latest forecast is used automatically.</p>
        </div>
        <div class="scope-badge" aria-label="Supported location">
          <span>Location</span>
          <strong>{{ district }}</strong>
        </div>
      </div>

      <div class="search-fields">
        <label>
          Soil type <span class="optional">Optional</span>
          <input
            name="soilType"
            [(ngModel)]="soilType"
            maxlength="100"
            placeholder="For example: Reddish Brown Earth"
            aria-describedby="soil-help"
          />
          <small id="soil-help">Leave blank if unknown.</small>
        </label>
        <button type="submit" class="button primary search-submit" [disabled]="isLoading()">
          {{ isLoading() ? 'Checking…' : 'Find recommendations' }}
        </button>
      </div>
    </form>

    @if (error(); as message) {
      <div class="notice error" role="alert">{{ message }}</div>
    }

    @if (recommendation(); as result) {
      <section class="recommendation-result-header" aria-label="Recommendation result summary">
        <div>
          <p class="eyebrow">Results</p>
          <h2>Crops assessed for this forecast</h2>
          <p>{{ visibleCrops().length }} of {{ result.crops.length }} crops shown</p>
        </div>
        <div class="forecast-period"><span>Forecast period</span><strong>{{ result.forecast.targetStartDate }} to {{ result.forecast.targetEndDate }}</strong></div>
      </section>

      <section class="recommendation-trust-bar" aria-label="Forecast provenance">
        <div>
          <span>Evaluation</span>
          <strong>{{ savedRecommendationId() ? 'Saved to history' : 'Read-only' }}</strong>
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

      @if (!soilType.trim()) {
        <div class="notice warning">Soil was not supplied. It is excluded from this calculation.</div>
      }

      @if (savedRecommendationId()) {
        <div class="notice success">This recommendation has been saved to your recommendation history.</div>
      }

      @if (result.limitations.length) {
        <details class="recommendation-details notice warning"><summary>Important information</summary>@for (limitation of result.limitations; track limitation) { <p>{{ limitation }}</p> }</details>
      }

      <section class="panel result-explorer" aria-label="Search and filter recommendation results">
        <div class="explorer-heading">
          <div>
            <p class="eyebrow">Filter results</p>
            <h2>Find a crop</h2>
          </div>
          <button type="button" class="button secondary" (click)="clearResultFilters()" [disabled]="!hasActiveResultFilters()">
            Clear filters
          </button>
        </div>
        <div class="result-controls">
          <label>
            Find a crop
            <input name="cropQuery" [ngModel]="cropQuery()" (ngModelChange)="cropQuery.set($event)" placeholder="Search Paddy, Maize or Chilli" />
          </label>
          <label>
            Suitability category
            <select name="categoryFilter" [ngModel]="selectedCategory()" (ngModelChange)="selectedCategory.set($event)">
              <option value="">All categories</option>
              @for (category of categoryOptions(); track category) {
                <option [value]="category">{{ category }}</option>
              }
            </select>
          </label>
          <label>
            Climate risk
            <select name="riskFilter" [ngModel]="selectedRisk()" (ngModelChange)="selectedRisk.set($event)">
              <option value="">All risk indicators</option>
              @for (risk of riskOptions(); track risk) {
                <option [value]="risk">{{ risk }}</option>
              }
            </select>
          </label>
          <label>
            Sort results
            <select name="sortOrder" [ngModel]="sortOrder()" (ngModelChange)="sortOrder.set($event)">
              <option value="rank">Recommendation rank</option>
              <option value="score">Overall score</option>
              <option value="name">Crop name</option>
            </select>
          </label>
        </div>
      </section>

      <section class="recommendation-list" aria-label="Crop recommendations">
        @for (crop of visibleCrops(); track crop.cropName) {
          <article class="crop-card recommendation-row" [class.needs-information]="isPartiallyAssessed(crop)">
            <span class="recommendation-rank">{{ crop.rank ? '#' + crop.rank : '—' }}</span>
            <div>
              <h2>{{ crop.cropName }}</h2>
              <span class="recommendation-status" [class.needs-information]="isPartiallyAssessed(crop)">{{ crop.suitabilityCategory || 'Needs more information' }}</span>
            </div>
            <div class="recommendation-why">
              <span>{{ isPartiallyAssessed(crop) ? 'Available now' : 'Why shown' }}</span>
              <p>{{ isPartiallyAssessed(crop) ? 'Factor-level information only. Not ranked.' : (crop.assessmentBasis || crop.explanation) }}</p>
            </div>
            <details class="crop-details">
              <summary>View details</summary>
              @if (isPartiallyAssessed(crop)) { <p>No overall score, category, or comparable rank is assigned.</p> } @else { <p><strong>Overall score:</strong> {{ crop.overallScore | number: '1.0-2' }}</p> }
              <p>{{ crop.explanation }}</p>
              @if (crop.unavailableFactors.length) { <div class="missing">Missing: {{ crop.unavailableFactors.join(', ') }}</div> }
              <div class="factor-list">
                @for (factor of crop.factors; track factor.factor) {
                  <div><span>{{ factor.factor }}</span><strong>{{ factor.isEvaluable ? (factor.score | number: '1.0-2') : 'Unavailable' }}</strong></div>
                }
              </div>
              @if (crop.climateRisks.length) { <div class="risks">{{ crop.climateRisks.join(', ') }}</div> }
              @if (crop.evidenceSources?.length) {
                <div class="factor-list" aria-label="Evidence sources">
                  @for (source of crop.evidenceSources; track source.sourceUrl) { <div><span>{{ source.evidenceStatus }}</span><a [href]="source.sourceUrl" target="_blank" rel="noopener noreferrer">{{ source.title }}</a></div> }
                </div>
              }
              @if (crop.scientificLimitations?.length) { <div class="missing">Limitations: {{ (crop.scientificLimitations || []).join(' ') }}</div> }
            </details>
          </article>
        }
      </section>

      <details class="recommendation-details forecast-details">
        <summary>View seven-day forecast</summary>
        <section class="forecast-days" aria-label="Seven day forecast">
          @for (day of result.forecast.days; track day.targetDate) {
            <article><strong>{{ day.targetDate }}</strong><span>{{ day.rainfall | number: '1.0-2' }} mm</span><span>{{ day.temperature | number: '1.0-2' }} deg C</span><span>{{ day.humidity | number: '1.0-2' }}%</span></article>
          }
        </section>
      </details>

      @if (!visibleCrops().length) {
        <div class="notice warning">No crop matches the current result filters. Clear filters to view all returned crops.</div>
      }

      @if (result.crops.length !== expectedCropCount) {
        <div class="notice warning">Expected six approved crops; backend returned {{ result.crops.length }}.</div>
      }
    }
  `,
})
export class RecommendationPageComponent {
  readonly expectedCropCount = supportedCrops.length;
  readonly recommendation = signal<RecommendationEvaluationResponse | null>(null);
  readonly savedRecommendationId = signal<number | null>(null);
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);
  readonly cropQuery = signal('');
  readonly selectedCategory = signal('');
  readonly selectedRisk = signal('');
  readonly sortOrder = signal<'rank' | 'score' | 'name'>('rank');
  readonly categoryOptions = computed(() => uniqueSorted(
    this.recommendation()?.crops
      .map(crop => crop.suitabilityCategory)
      .filter((category): category is string => category !== null) ?? [],
  ));
  readonly riskOptions = computed(() => uniqueSorted(this.recommendation()?.crops.flatMap(crop => crop.climateRisks) ?? []));
  readonly visibleCrops = computed(() => this.filterAndSortCrops(this.recommendation()?.crops ?? []));
  district = 'Anuradhapura District';
  soilType = '';

  constructor(
    private readonly api: ApiService,
    private readonly auth: AuthService,
  ) {}

  submit(): void {
    this.isLoading.set(true);
    this.error.set(null);
    this.recommendation.set(null);
    this.savedRecommendationId.set(null);
    this.clearResultFilters();
    const request = {
      district: this.district,
      soilType: this.soilType.trim() || null,
    };

    const recommendation$ = this.auth.hasRole('Registered User')
      ? this.api.createRecommendation(request).pipe(map(result => {
        this.savedRecommendationId.set(result.recommendationId);
        return this.toEvaluationResponse(result);
      }))
      : this.api.evaluateRecommendation(request);

    recommendation$
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: result => this.recommendation.set(result),
        error: error => this.error.set(error.message),
      });
  }

  clearResultFilters(): void {
    this.cropQuery.set('');
    this.selectedCategory.set('');
    this.selectedRisk.set('');
    this.sortOrder.set('rank');
  }

  hasActiveResultFilters(): boolean {
    return this.cropQuery().trim().length > 0
      || this.selectedCategory().length > 0
      || this.selectedRisk().length > 0
      || this.sortOrder() !== 'rank';
  }

  isPartiallyAssessed(crop: RecommendationCropResponse): boolean {
    return crop.assessmentState === 'PARTIALLY_ASSESSED'
      || crop.overallScore === null
      || crop.suitabilityCategory === null
      || crop.rank === null;
  }

  private filterAndSortCrops(crops: RecommendationCropResponse[]): RecommendationCropResponse[] {
    const query = this.cropQuery().trim().toLocaleLowerCase();
    const category = this.selectedCategory();
    const risk = this.selectedRisk();
    const sortOrder = this.sortOrder();

    return crops
      .filter(crop => !query || crop.cropName.toLocaleLowerCase().includes(query))
      .filter(crop => !category || crop.suitabilityCategory === category)
      .filter(crop => !risk || crop.climateRisks.includes(risk))
      .slice()
      .sort((left, right) => {
        if (sortOrder === 'name') {
          return left.cropName.localeCompare(right.cropName);
        }
        if (sortOrder === 'score') {
          return (right.overallScore ?? Number.NEGATIVE_INFINITY) - (left.overallScore ?? Number.NEGATIVE_INFINITY)
            || left.cropName.localeCompare(right.cropName);
        }
        return (left.rank ?? Number.MAX_SAFE_INTEGER) - (right.rank ?? Number.MAX_SAFE_INTEGER)
          || left.cropName.localeCompare(right.cropName);
      });
  }

  private toEvaluationResponse(result: RecommendationResponse): RecommendationEvaluationResponse {
    return {
      forecastRunId: result.forecastRunId,
      soilType: result.soilType,
      forecast: result.forecast,
      crops: result.crops,
      limitations: [],
    };
  }
}

function uniqueSorted(values: string[]): string[] {
  return [...new Set(values)].sort((left, right) => left.localeCompare(right));
}
