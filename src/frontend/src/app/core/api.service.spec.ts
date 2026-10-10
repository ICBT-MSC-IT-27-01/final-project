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

  it('requests registered history without a user id ownership parameter', () => {
    const { service, http } = setup();

    service.listRecommendationHistory(2, 10).subscribe();

    const request = http.expectOne('/api/recommendations/history?page=2&pageSize=10');
    expect(request.request.method).toBe('GET');
    expect(request.request.params.has('userId')).toBe(false);
    request.flush({ items: [], page: 2, pageSize: 10, totalCount: 0 });
    http.verify();
  });

  it('requests registered history detail by route id only', () => {
    const { service, http } = setup();

    service.getRecommendationHistoryDetail(77).subscribe();

    const request = http.expectOne('/api/recommendations/77');
    expect(request.request.method).toBe('GET');
    expect(request.request.body).toBeNull();
    request.flush(sampleHistoryDetail());
    http.verify();
  });

  it('requests the public latest forecast without authentication or request body', () => {
    const { service, http } = setup();

    service.getLatestForecast().subscribe();

    const request = http.expectOne('/api/forecasts/latest');
    expect(request.request.method).toBe('GET');
    expect(request.request.body).toBeNull();
    request.flush(sampleLatestForecast());
    http.verify();
  });

  it('uses verified administrator contracts for role and active-status changes', () => {
    const { service, http } = setup();

    service.updateAdminUserRole(5, 'Agricultural Officer').subscribe();
    service.setAdminResourceActiveStatus('crops', 8, false).subscribe();

    const roleRequest = http.expectOne('/api/admin/users/5/role');
    expect(roleRequest.request.method).toBe('PUT');
    expect(roleRequest.request.body).toEqual({ role: 'Agricultural Officer' });
    roleRequest.flush({});

    const statusRequest = http.expectOne('/api/admin/crops/8/active-status');
    expect(statusRequest.request.method).toBe('PATCH');
    expect(statusRequest.request.body).toEqual({ isActive: false });
    statusRequest.flush({ id: 8, name: 'Paddy', isActive: false });
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

function sampleHistoryDetail() {
  return {
    recommendationId: 77,
    forecastRunId: '33333333-3333-3333-3333-333333333333',
    soilType: null,
    createdAt: '2026-10-10T00:00:00Z',
    forecast: null,
    crops: [],
    provenance: ['Stored recommendation history'],
    limitations: ['Stored forecast evidence is unavailable.'],
  };
}

function sampleLatestForecast() {
  return {
    forecastRunId: '44444444-4444-4444-4444-444444444444',
    modelVersion: 'v1',
    forecastDate: '2026-10-10',
    createdAt: '2026-10-10T00:00:00Z',
    forecastPeriodStart: '2026-10-11',
    forecastPeriodEnd: '2026-10-17',
    district: 'Anuradhapura',
    freshnessStatus: 'Unknown',
    limitations: ['Forecast values are AI-predicted weather forecasts, not measured observations.'],
    dailyForecasts: Array.from({ length: 7 }, (_, index) => ({
      targetDate: `2026-10-${11 + index}`,
      rainfallMm: index + 1,
      temperatureC: 28 + index,
      humidityPercent: 70 + index,
    })),
  };
}
