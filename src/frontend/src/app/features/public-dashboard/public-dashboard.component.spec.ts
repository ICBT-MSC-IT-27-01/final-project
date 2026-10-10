import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiService, FrontendApiError } from '../../core/api.service';
import { LatestForecastResponse } from '../../core/api.models';
import { PublicDashboardComponent } from './public-dashboard.component';

describe('PublicDashboardComponent', () => {
  const api = {
    getLatestForecast: vi.fn(),
  };
  let fixture: ComponentFixture<PublicDashboardComponent>;

  beforeEach(async () => {
    vi.clearAllMocks();
    await TestBed.configureTestingModule({
      imports: [PublicDashboardComponent],
      providers: [{ provide: ApiService, useValue: api }, provideRouter([])],
    }).compileComponents();
  });

  it('loads and renders seven persisted AI forecast days with units', () => {
    api.getLatestForecast.mockReturnValue(of(sampleLatestForecast()));

    fixture = TestBed.createComponent(PublicDashboardComponent);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(api.getLatestForecast).toHaveBeenCalledOnce();
    expect(text).toContain('Anuradhapura');
    expect(text).toContain('2026-10-11 to 2026-10-17');
    expect(text).toContain('v1');
    expect(text).toContain('1 mm rainfall');
    expect(text).toContain('28 deg C mean temperature');
    expect(text).toContain('70% mean humidity');
    expect(text).toContain('AI-predicted weather forecasts');
    expect(fixture.nativeElement.querySelectorAll('.forecast-days article')).toHaveLength(7);
  });

  it('shows a no-forecast state for HTTP 404 responses', () => {
    api.getLatestForecast.mockReturnValue(throwError(() => new FrontendApiError('No complete forecast is available.', 404)));

    fixture = TestBed.createComponent(PublicDashboardComponent);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('No seven-day AI weather forecast is available yet.');
    expect(text).not.toContain('1 mm rainfall');
  });

  it('shows a service-unavailable state for HTTP 503 responses', () => {
    api.getLatestForecast.mockReturnValue(throwError(() => new FrontendApiError('Forecast data is temporarily unavailable.', 503)));

    fixture = TestBed.createComponent(PublicDashboardComponent);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Forecast data is temporarily unavailable. Please try again later.');
    expect(text).not.toContain('1 mm rainfall');
  });

  it('shows a generic state for HTTP 500 responses', () => {
    api.getLatestForecast.mockReturnValue(throwError(() => new FrontendApiError('Forecast request failed.', 500)));

    fixture = TestBed.createComponent(PublicDashboardComponent);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('The latest forecast could not be loaded.');
    expect(text).not.toContain('1 mm rainfall');
  });

  it('rejects malformed forecast responses with fewer than seven days', () => {
    api.getLatestForecast.mockReturnValue(of({ ...sampleLatestForecast(), dailyForecasts: [] }));

    fixture = TestBed.createComponent(PublicDashboardComponent);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('The latest forecast response is incomplete.');
    expect(fixture.nativeElement.querySelectorAll('.forecast-days article')).toHaveLength(0);
  });
});

function sampleLatestForecast(): LatestForecastResponse {
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
