import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiService, FrontendApiError } from '../../core/api.service';
import { RecommendationEvaluationResponse } from '../../core/api.models';
import { RecommendationPageComponent } from './recommendation-page.component';

describe('RecommendationPageComponent', () => {
  const api = {
    evaluateRecommendation: vi.fn(),
  };
  let fixture: ComponentFixture<RecommendationPageComponent>;
  let component: RecommendationPageComponent;

  beforeEach(async () => {
    vi.clearAllMocks();
    await TestBed.configureTestingModule({
      imports: [RecommendationPageComponent],
      providers: [{ provide: ApiService, useValue: api }],
    }).compileComponents();

    fixture = TestBed.createComponent(RecommendationPageComponent);
    component = fixture.componentInstance;
  });

  it('submits the backend recommendation contract without user id spoofing', () => {
    api.evaluateRecommendation.mockReturnValue(of(sampleRecommendation()));
    component.soilType = '  Reddish Brown Earth  ';

    component.submit();

    expect(api.evaluateRecommendation).toHaveBeenCalledWith({
      district: 'Anuradhapura District',
      soilType: 'Reddish Brown Earth',
    });
    expect(component.recommendation()?.crops).toHaveLength(6);
  });

  it('renders six crops, not-ranked insufficient evidence and missing factors', () => {
    component.recommendation.set(sampleRecommendation());

    fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Paddy');
    expect(text).toContain('Chilli');
    expect(text).toContain('Not ranked');
    expect(text).toContain('Not enough information');
    expect(text).toContain('Not assessed: Soil');
  });

  it('warns when every crop is unsuitable and keeps internal evidence paths non-clickable', () => {
    const result = sampleRecommendation();
    result.crops = result.crops.map(crop => ({
      ...crop,
      suitabilityCategory: 'Unsuitable',
      evidenceSources: [{ title: 'Approved suitability matrix', sourceUrl: 'docs/qa/PHASE3_SUITABILITY_CONFIGURATION_TEMPLATE.md', publicationDetails: null, location: null, evidenceStatus: 'OWNER_APPROVED' }],
    }));
    component.recommendation.set(result);

    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;

    expect(element.textContent).toContain('No suitable crop was found');
    expect(element.textContent).toContain('first crop is not a planting recommendation');
    expect(element.querySelector('a[href^="docs/qa/"]')).toBeNull();
    expect(element.textContent).toContain('Approved suitability matrix (project record)');
  });

  it('shows seven-day aggregates and explains strict factor bands without implying forecast confidence', () => {
    const result = sampleRecommendation();
    result.forecast.days = Array.from({ length: 7 }, (_, index) => ({
      targetDate: `2026-10-${11 + index}`,
      rainfall: 2,
      temperature: 28,
      humidity: 70,
    }));
    result.crops[0].factors = [
      { factor: 'Rainfall', isEvaluable: true, score: 0, aggregatedValue: 14, configuredWeight: 0.4, effectiveWeight: 0.6, explanation: 'Outside the configured range.' },
      { factor: 'Temperature', isEvaluable: true, score: 100, aggregatedValue: 28, configuredWeight: 0.15, effectiveWeight: 0.23, explanation: 'Inside the configured range.' },
    ];
    component.recommendation.set(result);

    fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('14 mm total');
    expect(text).toContain('28 °C');
    expect(text).toContain('70%');
    expect(text).toContain('Within range');
    expect(text).toContain('Outside range');
    expect(text).toContain('not the probability of a successful harvest');
  });

  it('warns when a recommendation uses a forecast issued during its first target day', () => {
    const result = sampleRecommendation();
    result.forecast.createdAt = '2026-10-11T15:38:02Z';
    component.recommendation.set(result);

    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('does not cover seven complete future days');
  });

  it('shows safe forecast-unavailable errors', () => {
    api.evaluateRecommendation.mockReturnValue(throwError(() => new FrontendApiError('No complete forecast is available.', 503)));

    component.submit();

    expect(component.error()).toBe('No complete forecast is available.');
    expect(component.recommendation()).toBeNull();
  });
});

function sampleRecommendation(): RecommendationEvaluationResponse {
  const crops = ['Paddy', 'Maize', 'Green Gram', 'Cowpea', 'Groundnut', 'Chilli'].map((cropName, index) => ({
    cropName,
    evaluationStatus: index === 5 ? 'InsufficientEvidence' : 'PartiallyEvaluated',
    rainfallScore: index === 5 ? null : 70,
    temperatureScore: index === 5 ? null : 80,
    humidityScore: null,
    soilScore: null,
    overallScore: index === 5 ? null : 75 - index,
    suitabilityCategory: index === 5 ? null : 'Suitable',
    rank: index === 5 ? null : index + 1,
    unavailableFactors: index === 5 ? ['Soil'] : ['Humidity', 'Soil'],
    factors: [
      {
        factor: 'Soil',
        isEvaluable: false,
        score: null,
        aggregatedValue: null,
        configuredWeight: 25,
        effectiveWeight: null,
        explanation: 'Soil evidence was not supplied.',
      },
    ],
    evidenceCoverage: {
      evaluatedFactorCount: index === 5 ? 0 : 2,
      totalFactorCount: 4,
      evaluatedConfiguredWeightTotal: index === 5 ? 0 : 50,
    },
    climateRisks: index === 0 ? ['Low rainfall'] : [],
    explanation: `${cropName} explanation`,
  }));

  return {
    forecastRunId: '22222222-2222-2222-2222-222222222222',
    soilType: null,
    forecast: {
      forecastRunId: '22222222-2222-2222-2222-222222222222',
      forecastDate: '2026-10-10',
      targetStartDate: '2026-10-11',
      targetEndDate: '2026-10-17',
      modelVersion: 'v1',
      createdAt: '2026-10-10T00:00:00Z',
      days: [
        { targetDate: '2026-10-11', rainfall: 1, temperature: 30, humidity: 70 },
      ],
    },
    crops,
    limitations: [
      'Crop requirement values are configuration-dependent and should be interpreted with agronomic review.',
    ],
  };
}
