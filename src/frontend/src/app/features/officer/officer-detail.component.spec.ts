import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiService, FrontendApiError } from '../../core/api.service';
import { RecommendationReviewResponse } from '../../core/api.models';
import { OfficerDetailComponent } from './officer-detail.component';

describe('OfficerDetailComponent', () => {
  const api = {
    getRecommendationReview: vi.fn(),
    submitRecommendationValidation: vi.fn(),
  };
  let fixture: ComponentFixture<OfficerDetailComponent>;
  let component: OfficerDetailComponent;

  beforeEach(async () => {
    vi.clearAllMocks();
    api.getRecommendationReview.mockReturnValue(of(sampleReview()));
    await TestBed.configureTestingModule({
      imports: [OfficerDetailComponent],
      providers: [
        { provide: ApiService, useValue: api },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => '42' } } } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(OfficerDetailComponent);
    component = fixture.componentInstance;
  });

  it('loads recommendation details for the route recommendation id', () => {
    expect(api.getRecommendationReview).toHaveBeenCalledWith(42);
    expect(component.review()?.recommendationId).toBe(42);
  });

  it('submits only approved validation metadata and trims optional comments', () => {
    api.submitRecommendationValidation.mockReturnValue(of({
      id: 5,
      recommendationId: 42,
      officerUserId: 8,
      status: 'Validated',
      comment: 'Evidence reviewed.',
      createdAt: '2026-10-10T00:00:00Z',
    }));
    component.status = 'Validated';
    component.comment = '  Evidence reviewed.  ';

    component.submit();

    expect(api.submitRecommendationValidation).toHaveBeenCalledWith(42, {
      status: 'Validated',
      comment: 'Evidence reviewed.',
    });
    expect(api.submitRecommendationValidation.mock.calls[0][1].officerUserId).toBeUndefined();
    expect(component.success()).toBe('Validation submitted.');
  });

  it('shows the feature-disabled response from the backend safely', () => {
    api.submitRecommendationValidation.mockReturnValue(throwError(() => new FrontendApiError('Agricultural Officer validation is disabled.', 503)));

    component.submit();

    expect(component.error()).toBe('Agricultural Officer validation is disabled.');
  });
});

function sampleReview(): RecommendationReviewResponse {
  return {
    recommendationId: 42,
    forecastRunId: '33333333-3333-3333-3333-333333333333',
    userId: null,
    soilType: null,
    createdAt: '2026-10-10T00:00:00Z',
    evidenceSnapshotJson: '{}',
    crops: [
      {
        cropId: 1,
        cropName: 'Paddy',
        rainfallScore: 80,
        temperatureScore: 75,
        humidityScore: null,
        soilScore: null,
        overallScore: 77,
        suitabilityCategory: 'Suitable',
        rank: 1,
        evaluationStatus: 'PartiallyEvaluated',
        explanation: 'Paddy explanation',
        evidenceSnapshotJson: '{}',
      },
    ],
    validationHistory: [],
  };
}
