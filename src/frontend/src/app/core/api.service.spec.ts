import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { ApiService, FrontendApiError } from './api.service';

describe('ApiService', () => {
  function setup() {
    TestBed.configureTestingModule({
      providers: [ApiService, provideHttpClient(), provideHttpClientTesting()],
    });

    return {
      service: TestBed.inject(ApiService),
      http: TestBed.inject(HttpTestingController),
    };
  }

  it('posts recommendation requests without a user id ownership field', () => {
    const { service, http } = setup();

    service.createRecommendation({ district: 'Anuradhapura District', soilType: 'Reddish Brown Earth' }).subscribe();

    const request = http.expectOne('/api/recommendations');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      district: 'Anuradhapura District',
      soilType: 'Reddish Brown Earth',
    });
    expect(request.request.body.userId).toBeUndefined();
    request.flush(sampleRecommendation());
    http.verify();
  });

  it('maps forecast-unavailable responses to safe frontend errors', async () => {
    const { service, http } = setup();
    const errorPromise = new Promise<FrontendApiError>(resolve => {
      service.createRecommendation({ district: 'Anuradhapura District' }).subscribe({
        error: error => resolve(error),
      });
    });

    const request = http.expectOne('/api/recommendations');
    request.flush({ message: 'No complete forecast is available.' }, { status: 503, statusText: 'Service Unavailable' });

    await expect(errorPromise).resolves.toMatchObject({
      message: 'No complete forecast is available.',
      status: 503,
    });
    http.verify();
  });

  it('posts officer validation with trusted identity omitted from the body', () => {
    const { service, http } = setup();

    service.submitRecommendationValidation(42, { status: 'Validated', comment: 'Looks consistent.' }).subscribe();

    const request = http.expectOne('/api/validations/recommendations/42');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ status: 'Validated', comment: 'Looks consistent.' });
    expect(request.request.body.officerUserId).toBeUndefined();
    request.flush({ id: 1, recommendationId: 42, officerUserId: 7, status: 'Validated', comment: 'Looks consistent.', createdAt: '2026-10-10T00:00:00Z' });
    http.verify();
  });
});

function sampleRecommendation() {
  return {
    recommendationId: 1,
    forecastRunId: '11111111-1111-1111-1111-111111111111',
    userId: null,
    soilType: 'Reddish Brown Earth',
    createdAt: '2026-10-10T00:00:00Z',
    forecast: {
      forecastRunId: '11111111-1111-1111-1111-111111111111',
      forecastDate: '2026-10-10',
      targetStartDate: '2026-10-11',
      targetEndDate: '2026-10-17',
      modelVersion: 'v1',
      createdAt: '2026-10-10T00:00:00Z',
      days: [],
    },
    crops: [],
  };
}
