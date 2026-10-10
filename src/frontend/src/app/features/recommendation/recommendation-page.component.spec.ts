import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiService, FrontendApiError } from '../../core/api.service';
import { RecommendationResponse } from '../../core/api.models';
import { RecommendationPageComponent } from './recommendation-page.component';

describe('RecommendationPageComponent', () => {
  const api = {
    createRecommendation: vi.fn(),
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
    api.createRecommendation.mockReturnValue(of(sampleRecommendation()));
    component.soilType = '  Reddish Brown Earth  ';

    component.submit();

    expect(api.createRecommendation).toHaveBeenCalledWith({
      district: 'Anuradhapura District',
      soilType: 'Reddish Brown Earth',
    });
    expect(component.recommendation()?.crops).toHaveLength(6);
  });

  it('renders six crops, unranked insufficient evidence and missing factors', () => {
    component.recommendation.set(sampleRecommendation());

    fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Paddy');
    expect(text).toContain('Chilli');
    expect(text).toContain('Unranked');
    expect(text).toContain('InsufficientEvidence');
    expect(text).toContain('Missing: Soil');
  });

  it('shows safe forecast-unavailable errors', () => {
    api.createRecommendation.mockReturnValue(throwError(() => new FrontendApiError('No complete forecast is available.', 503)));

    component.submit();

    expect(component.error()).toBe('No complete forecast is available.');
    expect(component.recommendation()).toBeNull();
  });
});

function sampleRecommendation(): RecommendationResponse {
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
    recommendationId: 99,
    forecastRunId: '22222222-2222-2222-2222-222222222222',
    userId: null,
    soilType: null,
    createdAt: '2026-10-10T00:00:00Z',
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
  };
}
