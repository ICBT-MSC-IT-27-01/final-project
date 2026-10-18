import { DecimalPipe, NgClass } from '@angular/common';
import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { ApiService } from '../../core/api.service';
import { RecommendationCropResponse, RecommendationEvaluationResponse, RecommendationEvidenceSource, supportedCrops } from '../../core/api.models';
import { firstForecastDayAlreadyStarted } from '../../core/forecast-timing';

@Component({
  selector: 'app-recommendation-page',
  imports: [DecimalPipe, FormsModule, NgClass],
  template: `
    <section class="page-title">
      <p class="eyebrow">Anuradhapura District</p>
      <h1>Crop Recommendation</h1>
      <p>See how the latest seven-day forecast compares with the six crops in this study.</p>
    </section>

    <div class="recommendation-intro">
    <form class="panel form-panel" (ngSubmit)="submit()">
      <p class="eyebrow">NEW ASSESSMENT</p>
      <h2>Check crop suitability</h2>
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
      <small>Enter a soil type only if you know its name. An unrecognised soil type cannot be assessed.</small>


      <button type="submit" class="button primary" [disabled]="isLoading()">
        {{ isLoading() ? 'Checking...' : 'Check six crops' }}
      </button>
    </form>
    <aside class="panel guidance-panel">
      <p class="eyebrow">HOW IT WORKS</p>
      <h2>How the result is worked out</h2>
      <p>The latest eligible seven-day forecast is compared with the approved requirements for each supported crop.</p>
      <div class="guidance-step"><span>01</span><div><strong>Weather factors</strong><p>Rainfall, temperature and humidity are assessed separately.</p></div></div>
      <div class="guidance-step"><span>02</span><div><strong>Transparent scoring</strong><p>Scores and ranks appear only when the required evidence is available.</p></div></div>
      <div class="guidance-step"><span>03</span><div><strong>Local judgment</strong><p>Review the limitations and seek agricultural advice before acting.</p></div></div>
    </aside>
    </div>

    @if (error(); as message) {
      <div class="notice error" role="alert">{{ message }}</div>
    }

    @if (recommendation(); as result) {
      <section class="forecast-strip" aria-label="Forecast provenance">
        <div>
          <span>Result</span>
          <strong>For information</strong>
        </div>
        <div>
          <span>Weather information</span>
          <strong>Seven-day AI forecast</strong>
        </div>
        <div>
          <span>Forecast period</span>
          <strong>{{ result.forecast.targetStartDate }} to {{ result.forecast.targetEndDate }}</strong>
        </div>
        <div>
          <span>Soil</span>
          <strong>{{ result.soilType || 'Not provided' }}</strong>
        </div>
      </section>

      @if (firstForecastDayAlreadyStarted(result.forecast.createdAt, result.forecast.targetStartDate)) {
        <div class="notice warning" role="status">The forecast was issued after its first listed day had begun. This result does not cover seven complete future days from issue time. Check the dates and seek local agricultural advice before acting.</div>
      }

      @if (soilWasNotAssessed(result)) {
        <div class="notice warning">Soil was not assessed{{ result.soilType ? ' because this soil type was not matched' : ' because no soil type was provided' }}. The result uses only the available weather factors.</div>
      }

      @if (allCropsUnsuitable(result)) {
        <div class="notice warning" role="status"><strong>No suitable crop was found for this seven-day forecast.</strong> All six crops fall outside at least part of the configured weather requirements. The order below only compares their scores; the first crop is not a planting recommendation.</div>
      }

      <p class="result-guide">Each weather factor gets 100 when the seven-day value is inside its configured range and 0 when outside. The combined score uses the configured weights; it is not the probability of a successful harvest.</p>

      @if (weatherSummary(result); as summary) {
        <section class="forecast-summary assessment-summary" aria-label="Weather values used for the assessment">
          <div><span>Rain over seven days</span><strong>{{ summary.rainfall | number: '1.0-2' }} mm total</strong></div>
          <div><span>Average temperature</span><strong>{{ summary.temperature | number: '1.0-2' }} °C</strong></div>
          <div><span>Average humidity</span><strong>{{ summary.humidity | number: '1.0-2' }}%</strong></div>
        </section>
      }

      @if (result.limitations.length) {
        <section class="notice warning" aria-label="Recommendation limitations">
          @for (limitation of result.limitations; track limitation) {
            <p>{{ limitation }}</p>
          }
        </section>
      }

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
          <article class="crop-card" [ngClass]="crop.evaluationStatus" [class.unsuitable]="crop.suitabilityCategory === 'Unsuitable'">
            <header>
              <span class="rank">{{ crop.rank ? (crop.assessmentState === 'PARTIALLY_ASSESSED' ? 'Indicative order ' : 'Relative order ') + crop.rank : 'Not ranked' }}</span>
              <h2>{{ crop.cropName }}</h2>
            </header>
            <p class="score">
              @if (crop.overallScore !== null) {
                {{ crop.overallScore | number: '1.0-2' }}<span class="score-unit"> / 100 weighted score</span>
              } @else {
                No score
              }
            </p>
            @if (crop.assessmentState === 'PARTIALLY_ASSESSED') {
              <p>Only available factor evidence is shown. No overall suitability score, category or comparable rank is assigned.</p>
            }
            <p class="crop-category">{{ categoryLabel(crop) }}</p>
            <p class="crop-explanation">{{ categoryExplanation(crop) }}</p>

            @if (crop.unavailableFactors.length) {
              <div class="missing">Not assessed: {{ crop.unavailableFactors.join(', ') }}</div>
            }

            <div class="factor-list">
              @for (factor of crop.factors; track factor.factor) {
                <div>
                  <span>{{ factor.factor }}</span>
                  <strong>{{ factorStatus(factor.isEvaluable, factor.score) }}</strong>
                </div>
              }
            </div>

            @if (crop.climateRisks.length) {
              <div class="risks">{{ crop.climateRisks.join(', ') }}</div>
            }

            <details class="crop-details">
              <summary>Evidence, sources &amp; limitations</summary>
              @if (crop.assessmentBasis) {
                <p><strong>Assessment basis:</strong> {{ crop.assessmentBasis }}</p>
              }
              <p><strong>Full explanation:</strong> {{ crop.explanation }}</p>
              <p><strong>Forecast record:</strong> {{ result.forecastRunId }}</p>
              @if (crop.evidenceSources?.length) {
                <div class="factor-list" aria-label="Evidence sources">
                  @for (source of uniqueSources(crop.evidenceSources); track source.sourceUrl) {
                    <div>
                      <span>{{ sourceLabel(source.evidenceStatus) }}</span>
                      @if (isPublicUrl(source.sourceUrl)) {
                        <a [href]="source.sourceUrl" target="_blank" rel="noopener noreferrer">{{ source.title }}</a>
                      } @else {
                        <span>{{ source.title }} (project record)</span>
                      }
                    </div>
                  }
                </div>
              }
              @if (crop.scientificLimitations?.length) {
                <div class="notice warning">Limitations: {{ (crop.scientificLimitations || []).join(' ') }}</div>
              }
            </details>

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
  readonly firstForecastDayAlreadyStarted = firstForecastDayAlreadyStarted;
  readonly expectedCropCount = supportedCrops.length;
  readonly recommendation = signal<RecommendationEvaluationResponse | null>(null);
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);
  district = 'Anuradhapura District';
  soilType = '';

  constructor(private readonly api: ApiService) {}

  uniqueSources(sources: RecommendationEvidenceSource[] | undefined): RecommendationEvidenceSource[] {
    return [...new Map((sources || []).map(source => [source.sourceUrl, source])).values()];
  }

  soilWasNotAssessed(result: RecommendationEvaluationResponse): boolean {
    return result.crops.some(crop => crop.unavailableFactors.some(factor => factor.toLowerCase() === 'soil'));
  }

  allCropsUnsuitable(result: RecommendationEvaluationResponse): boolean {
    return result.crops.length > 0 && result.crops.every(crop => crop.suitabilityCategory === 'Unsuitable');
  }

  weatherSummary(result: RecommendationEvaluationResponse): { rainfall: number; temperature: number; humidity: number } | null {
    const days = result.forecast.days;
    if (days.length !== 7) return null;
    return {
      rainfall: days.reduce((total, day) => total + day.rainfall, 0),
      temperature: days.reduce((total, day) => total + day.temperature, 0) / days.length,
      humidity: days.reduce((total, day) => total + day.humidity, 0) / days.length,
    };
  }

  factorStatus(isEvaluable: boolean, score: number | null): string {
    if (!isEvaluable || score === null) return 'Not assessed';
    if (score === 100) return 'Within range';
    if (score === 0) return 'Outside range';
    return `${score} / 100`;
  }

  categoryLabel(crop: RecommendationCropResponse): string {
    if (crop.suitabilityCategory) return crop.suitabilityCategory;
    if (crop.assessmentState === 'PARTIALLY_ASSESSED' || crop.evaluationStatus === 'PartiallyEvaluated') return 'Partly assessed';
    return 'Not enough information';
  }

  categoryExplanation(crop: RecommendationCropResponse): string {
    if (crop.suitabilityCategory === 'Unsuitable') return 'The available weather factors do not meet this crop’s configured requirements for this forecast period.';
    if (!crop.suitabilityCategory) return 'There is not enough information to give this crop a reliable suitability category.';
    return 'This category is based on the available forecast and configured crop requirements. Check the factors and limitations below.';
  }

  sourceLabel(status: string): string {
    return status.includes('OWNER_APPROVED') ? 'Project-approved configuration' : 'Supporting source';
  }

  isPublicUrl(url: string): boolean {
    return /^https:\/\//i.test(url);
  }

  submit(): void {
    this.isLoading.set(true);
    this.error.set(null);
    this.recommendation.set(null);
    this.api
      .evaluateRecommendation({
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
