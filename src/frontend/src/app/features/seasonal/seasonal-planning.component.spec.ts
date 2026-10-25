import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiService } from '../../core/api.service';
import { SeasonalPlanningComponent } from './seasonal-planning.component';

describe('SeasonalPlanningComponent', () => {
  const api = {
    findRecommendedCropsByDateRange: vi.fn(),
    findSuitablePeriodsByCrop: vi.fn(),
    getLatestForecast: vi.fn(),
  };
  let fixture: ComponentFixture<SeasonalPlanningComponent>;
  let component: SeasonalPlanningComponent;

  beforeEach(async () => {
    vi.clearAllMocks();
    await TestBed.configureTestingModule({
      imports: [RouterTestingModule, SeasonalPlanningComponent],
      providers: [{ provide: ApiService, useValue: api }],
    }).compileComponents();
    fixture = TestBed.createComponent(SeasonalPlanningComponent);
    component = fixture.componentInstance;
  });

  it('shows matching periods and API-provided alternative periods without claiming suitability', () => {
    api.findRecommendedCropsByDateRange.mockReturnValue(of(rangeResponse()));
    api.getLatestForecast.mockReturnValue(of(latestForecast()));

    component.findCrops();
    fixture.detectChanges();

    expect(api.findRecommendedCropsByDateRange).toHaveBeenCalledWith(component.startDate, component.endDate, null);
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Available period');
    expect(text).toContain('No matching period');
    expect(text).toContain('Try instead');
    expect(text).toContain('Current 7-day weather context');
    expect(text).toContain('28 mm');
    expect(text).toContain('View crop recommendations');
    expect(text).not.toContain('Highly Suitable');
  });

  it('switches to crop search and displays only the returned matching calendar periods', () => {
    api.findSuitablePeriodsByCrop.mockReturnValue(of(cropResponse()));

    component.setMode('crop');
    component.findPeriods();
    fixture.detectChanges();

    expect(api.findSuitablePeriodsByCrop).toHaveBeenCalledWith('Paddy', component.startDate, component.endDate, null);
    expect(fixture.nativeElement.textContent).toContain('Available periods');
    expect(fixture.nativeElement.textContent).toContain('October');
  });
});

function window(id: string, months: number[]) {
  return { id, season: 'Maha', establishmentMonths: months, harvestMonths: [2], sourcePages: [1], evidenceStatus: 'CALENDAR_SUPPORTED_CANDIDATE', interpretation: '', conditions: [], requiresLocalReview: true };
}

function rangeResponse() {
  return {
    startDate: '2026-10-01', endDate: '2026-10-31', assessmentType: 'HISTORICAL_SEASONAL_CLIMATE', dataSource: '', evidenceStatus: 'CALENDAR_SUPPORTED_CANDIDATES', climateMonths: [], limitations: [],
    crops: [
      { cropName: 'Paddy', evidenceStatus: 'CALENDAR_SUPPORTED_CANDIDATE', suitabilityCategory: null, explanation: '', missingFactors: [], calendarEvidence: { geographicApplicability: '', documentedConditions: [], sourceUrls: [], validationStatus: '', limitations: [] }, candidateWindows: [window('paddy-maha', [10, 11])] },
      { cropName: 'Maize', evidenceStatus: 'CLIMATE_CONTEXT_ONLY', suitabilityCategory: null, explanation: '', missingFactors: [], calendarEvidence: { geographicApplicability: '', documentedConditions: [], sourceUrls: [], validationStatus: '', limitations: [] }, candidateWindows: [], alternativeWindows: [window('maize-yala', [4, 5])] },
    ],
  };
}

function cropResponse() {
  return {
    cropName: 'Paddy', startDate: '2026-10-01', endDate: '2026-10-31', assessmentType: 'HISTORICAL_SEASONAL_CLIMATE', dataSource: '', evidenceStatus: 'CALENDAR_SUPPORTED_CANDIDATE', climateMonths: [], candidateMonths: ['October'], explanation: '', limitations: [], calendarEvidence: { geographicApplicability: '', documentedConditions: [], sourceUrls: [], validationStatus: '', limitations: [] }, candidateWindows: [window('paddy-maha', [10, 11])], alternativeWindows: [window('paddy-yala', [4, 5])],
  };
}

function latestForecast() {
  return {
    forecastRunId: 'run-1', modelVersion: 'v1', forecastDate: '2026-10-10', createdAt: '2026-10-10T00:00:00Z',
    forecastPeriodStart: '2026-10-11', forecastPeriodEnd: '2026-10-17', district: 'Anuradhapura District', freshnessStatus: 'Current', limitations: [],
    dailyForecasts: Array.from({ length: 7 }, (_, index) => ({ targetDate: `2026-10-${11 + index}`, rainfallMm: 4, temperatureC: 30, humidityPercent: 70 })),
  };
}
