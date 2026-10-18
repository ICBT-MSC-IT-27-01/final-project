import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiService, FrontendApiError } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { RecommendationEvaluationResponse, RecommendationResponse } from '../../core/api.models';
import { RecommendationPageComponent } from './recommendation-page.component';

describe('RecommendationPageComponent', () => {
  const api = {
    evaluateRecommendation: vi.fn(),
    createRecommendation: vi.fn(),
  };
  const auth = {
    hasRole: vi.fn(),
  };
  let fixture: ComponentFixture<RecommendationPageComponent>;
  let component: RecommendationPageComponent;

  beforeEach(async () => {
    vi.clearAllMocks();
    auth.hasRole.mockReturnValue(false);
    await TestBed.configureTestingModule({
      imports: [RecommendationPageComponent],
      providers: [
        { provide: ApiService, useValue: api },
        { provide: AuthService, useValue: auth },
      ],
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
    expect(text).toContain('Needs more information');
    expect(text).toContain('Missing: Soil');
  });

  it('saves a registered user recommendation through the authenticated history endpoint', () => {
    auth.hasRole.mockReturnValue(true);
    api.createRecommendation.mockReturnValue(of(savedRecommendation()));

    component.submit();

    expect(api.createRecommendation).toHaveBeenCalledWith({
      district: 'Anuradhapura District',
      soilType: null,
    });
    expect(api.evaluateRecommendation).not.toHaveBeenCalled();
    expect(component.recommendation()?.forecastRunId).toBe('44444444-4444-4444-4444-444444444444');
    expect(component.savedRecommendationId()).toBe(10);
  });

  it('explains optional soil re-normalisation without calling a complete weather assessment partial', () => {
    const result = completeWeatherRecommendation();
    component.recommendation.set(result);

    fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Soil was not supplied. It is excluded from this calculation.');
    expect(text).toContain('80');
    expect(text).not.toContain('a partial assessment is never presented as full suitability');
  });

  it('filters returned crops by name, category and approved climate-risk indicators without changing evaluation data', () => {
    component.recommendation.set(sampleRecommendation());
    component.cropQuery.set('paddy');

    expect(component.visibleCrops().map(crop => crop.cropName)).toEqual(['Paddy']);

    component.cropQuery.set('');
    component.selectedCategory.set('Suitable');
    expect(component.visibleCrops()).toHaveLength(5);

    component.selectedCategory.set('');
    component.selectedRisk.set('Low rainfall');
    expect(component.visibleCrops().map(crop => crop.cropName)).toEqual(['Paddy']);
    expect(component.recommendation()?.crops).toHaveLength(6);
  });

  it('sorts only the displayed result list and can clear result filters', () => {
    component.recommendation.set(sampleRecommendation());
    component.sortOrder.set('name');

    expect(component.visibleCrops()[0].cropName).toBe('Chilli');
    expect(component.hasActiveResultFilters()).toBe(true);

    component.clearResultFilters();

    expect(component.sortOrder()).toBe('rank');
    expect(component.visibleCrops()[0].cropName).toBe('Paddy');
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

function savedRecommendation(): RecommendationResponse {
  const result = completeWeatherRecommendation();
  return {
    recommendationId: 10,
    forecastRunId: '44444444-4444-4444-4444-444444444444',
    userId: 7,
    soilType: null,
    createdAt: '2026-10-10T00:00:00Z',
    forecast: result.forecast,
    crops: result.crops,
  };
}

function completeWeatherRecommendation(): RecommendationEvaluationResponse {
  const result = sampleRecommendation();
  return {
    ...result,
    crops: result.crops.map((crop, index) => ({
      ...crop,
      assessmentState: 'COMPLETE',
      assessmentBasis: 'Mandatory weather factors assessed; optional soil omitted.',
      evaluationStatus: 'Evaluated',
      overallScore: 80 - index,
      suitabilityCategory: 'Highly Suitable',
      rank: index + 1,
      unavailableFactors: ['Soil'],
    })),
  };
}
