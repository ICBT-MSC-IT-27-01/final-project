import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiService, FrontendApiError } from '../../core/api.service';
import { RegisteredHistoryDetailComponent } from './registered-history-detail.component';
import { RegisteredHistoryBlockedComponent } from './registered-history-blocked.component';

describe('Registered recommendation history components', () => {
  const api = {
    listRecommendationHistory: vi.fn(),
    getRecommendationHistoryDetail: vi.fn(),
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('loads a paginated authenticated history list and renders empty states', async () => {
    api.listRecommendationHistory.mockReturnValue(of({ items: [], page: 1, pageSize: 10, totalCount: 0 }));
    await TestBed.configureTestingModule({
      imports: [RegisteredHistoryBlockedComponent],
      providers: [{ provide: ApiService, useValue: api }, provideRouter([])],
    }).compileComponents();

    const fixture = TestBed.createComponent(RegisteredHistoryBlockedComponent);
    fixture.detectChanges();

    expect(api.listRecommendationHistory).toHaveBeenCalledWith(1, 10);
    expect(fixture.nativeElement.textContent).toContain('No saved recommendations');
  });

  it('shows a safe disabled-feature error without rendering fabricated records', async () => {
    api.listRecommendationHistory.mockReturnValue(throwError(() => new FrontendApiError('Recommendation history is disabled.', 503)));
    await TestBed.configureTestingModule({
      imports: [RegisteredHistoryBlockedComponent],
      providers: [{ provide: ApiService, useValue: api }, provideRouter([])],
    }).compileComponents();

    const fixture = TestBed.createComponent(RegisteredHistoryBlockedComponent);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Recommendation history is disabled.');
    expect(fixture.nativeElement.textContent).not.toContain('Paddy');
  });

  it('renders stored detail evidence, missing factors and insufficient evidence', async () => {
    api.getRecommendationHistoryDetail.mockReturnValue(of(sampleDetail()));
    await TestBed.configureTestingModule({
      imports: [RegisteredHistoryDetailComponent],
      providers: [
        { provide: ApiService, useValue: api },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => '44' } } },
        },
      ],
    }).compileComponents();

    const fixture: ComponentFixture<RegisteredHistoryDetailComponent> = TestBed.createComponent(RegisteredHistoryDetailComponent);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(api.getRecommendationHistoryDetail).toHaveBeenCalledWith(44);
    expect(text).toContain('Stored forecast context is partial.');
    expect(text).toContain('Chilli');
    expect(text).toContain('InsufficientEvidence');
    expect(text).toContain('Missing: Soil');
  });
});

function sampleDetail() {
  return {
    recommendationId: 44,
    forecastRunId: '44444444-4444-4444-4444-444444444444',
    soilType: null,
    createdAt: '2026-10-10T00:00:00Z',
    forecast: {
      forecastRunId: '44444444-4444-4444-4444-444444444444',
      forecastDate: '2026-10-10',
      targetStartDate: '2026-10-11',
      targetEndDate: '2026-10-17',
      modelVersion: 'v1',
      days: [{ targetDate: '2026-10-11', rainfall: 1, temperature: 30, humidity: 70 }],
    },
    crops: [
      {
        cropName: 'Chilli',
        evaluationStatus: 'InsufficientEvidence',
        rainfallScore: null,
        temperatureScore: null,
        humidityScore: null,
        soilScore: null,
        overallScore: null,
        suitabilityCategory: null,
        rank: null,
        unavailableFactors: ['Soil'],
        factors: [{ factor: 'Soil', isEvaluable: false, score: null, aggregatedValue: null, configuredWeight: 25, effectiveWeight: null, explanation: 'Missing soil.' }],
        climateRisks: [],
        explanation: 'Stored historical explanation.',
      },
    ],
    provenance: ['Stored recommendation history'],
    limitations: ['Stored forecast context is partial.'],
  };
}
