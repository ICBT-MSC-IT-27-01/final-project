import { Page } from '@playwright/test';

const forecastRunId = 'b2d45e38-72fd-4b3d-87b5-418891a485c2';

export async function mockPhase16Api(page: Page): Promise<void> {
  await page.route('**/api/forecasts/latest', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify(latestForecast()),
  }));

  await page.route('**/api/recommendations/evaluate', async route => {
    const body = route.request().postDataJSON() as { soilType?: string | null };
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(recommendationEvaluation(body.soilType ?? null)),
    });
  });

  await page.route('**/api/seasonal-recommendations/by-date-range', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify(seasonalRange()),
  }));

  await page.route('**/api/seasonal-recommendations/by-crop', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify(seasonalCrop()),
  }));

  await page.route('**/api/recommendations', async route => {
    const body = route.request().postDataJSON() as { soilType?: string | null };
    const evaluation = recommendationEvaluation(body.soilType ?? null);
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        recommendationId: 1002,
        forecastRunId: evaluation.forecastRunId,
        userId: 3,
        soilType: evaluation.soilType,
        createdAt: '2026-10-13T09:10:00Z',
        forecast: evaluation.forecast,
        crops: evaluation.crops,
      }),
    });
  });

  await page.route('**/api/recommendations/history**', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({
      items: [
        {
          recommendationId: 1001,
          forecastRunId,
          userId: null,
          soilType: null,
          createdAt: '2026-10-13T09:00:00Z',
          cropResultCount: 6,
          topCropName: 'Paddy',
          topSuitabilityCategory: 'Suitable',
          topOverallScore: 74,
        },
      ],
      page: 1,
      pageSize: 10,
      totalCount: 1,
    }),
  }));

  await page.route('**/api/recommendations/1001', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify(historyDetail()),
  }));

  await page.route('**/api/validations/pending', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify([
      {
        recommendationId: 1001,
        forecastRunId,
        userId: null,
        soilType: null,
        createdAt: '2026-10-13T09:00:00Z',
        latestValidationStatus: null,
        latestValidationCreatedAt: null,
      },
    ]),
  }));

  await page.route('**/api/validations/recommendations/1001', async route => {
    if (route.request().method() === 'POST') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 501,
          recommendationId: 1001,
          officerUserId: 7,
          status: 'Needs Review',
          comment: 'Test fixture comment',
          createdAt: '2026-10-13T09:05:00Z',
        }),
      });
      return;
    }

    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(officerReview()),
    });
  });

  await page.route('**/api/auth/login', async route => {
    const body = route.request().postDataJSON() as { email?: string };
    if (body.email === 'bad@example.test') {
      await route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'Invalid email or password.' }),
      });
      return;
    }

    const role = body.email?.startsWith('officer')
      ? 'Agricultural Officer'
      : body.email?.startsWith('admin')
        ? 'Administrator'
        : 'Registered User';

    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        accessToken: `phase16-${role}`,
        expiresAt: '2026-10-13T10:00:00Z',
        userId: role === 'Administrator' ? 1 : role === 'Agricultural Officer' ? 2 : 3,
        name: role,
        email: body.email,
        role,
      }),
    });
  });

  await page.route('**/api/auth/register', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({
      userId: 3,
      name: 'Registered Test User',
      email: 'registered@example.test',
      role: 'Registered User',
      createdAt: '2026-10-13T09:00:00Z',
    }),
  }));

  await page.route('**/api/admin/**', route => {
    if (route.request().url().includes('/api/admin/forecasts/operational')) {
      return route.fulfill({
        status: 503,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'Operational forecast generation is disabled in Phase 16 browser tests.' }),
      });
    }

    return route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ items: adminRows(route.request().url()), page: 1, pageSize: 20, totalCount: 1 }),
    });
  });
}

export async function signInAs(page: Page, role: 'Registered User' | 'Agricultural Officer' | 'Administrator'): Promise<void> {
  await page.addInitScript(selectedRole => {
    const user = {
      id: selectedRole === 'Administrator' ? 1 : selectedRole === 'Agricultural Officer' ? 2 : 3,
      email: selectedRole === 'Administrator'
        ? 'admin@example.test'
        : selectedRole === 'Agricultural Officer'
          ? 'officer@example.test'
          : 'registered@example.test',
      role: selectedRole,
    };
    window.localStorage.setItem('anuradhapura-ai-access-token', `phase16-${selectedRole}`);
    window.localStorage.setItem('anuradhapura-ai-current-user', JSON.stringify(user));
  }, role);
}

function latestForecast() {
  return {
    forecastRunId,
    modelVersion: 'v1',
    forecastDate: '2026-10-12',
    createdAt: '2026-10-13T07:23:52Z',
    forecastPeriodStart: '2026-10-13',
    forecastPeriodEnd: '2026-10-19',
    district: 'Anuradhapura District',
    freshnessStatus: 'Operational',
    limitations: ['Forecast values are AI predictions, not observed weather.'],
    dailyForecasts: Array.from({ length: 7 }, (_, index) => ({
      targetDate: `2026-10-${13 + index}`,
      rainfallMm: index + 1,
      temperatureC: 27 + index / 10,
      humidityPercent: 74 - index,
    })),
  };
}

function recommendationEvaluation(soilType: string | null) {
  return {
    forecastRunId,
    soilType,
    forecast: {
      forecastRunId,
      forecastDate: '2026-10-12',
      targetStartDate: '2026-10-13',
      targetEndDate: '2026-10-19',
      modelVersion: 'v1',
      createdAt: '2026-10-13T07:23:52Z',
      days: latestForecast().dailyForecasts.map(day => ({
        targetDate: day.targetDate,
        rainfall: day.rainfallMm,
        temperature: day.temperatureC,
        humidity: day.humidityPercent,
      })),
    },
    crops: ['Paddy', 'Maize', 'Green Gram', 'Cowpea', 'Groundnut', 'Chilli'].map((cropName, index) => ({
      cropName,
      evaluationStatus: 'Evaluated',
      rainfallScore: 80 - index,
      temperatureScore: 75 - index,
      humidityScore: 70 - index,
      soilScore: null,
      overallScore: 84 - index,
      suitabilityCategory: 'Highly Suitable',
      rank: index + 1,
      assessmentState: 'COMPLETE',
      assessmentBasis: 'Mandatory weather factors assessed; optional soil omitted.',
      unavailableFactors: ['Soil'],
      factors: [
        {
          factor: 'Rainfall',
          isEvaluable: true,
          score: 80 - index,
          aggregatedValue: 28,
          configuredWeight: 40,
          effectiveWeight: 0.615,
          explanation: 'Synthetic Phase 16 fixture rainfall evidence.',
        },
        {
          factor: 'Temperature',
          isEvaluable: true,
          score: 75 - index,
          aggregatedValue: 27.3,
          configuredWeight: 15,
          effectiveWeight: 0.231,
          explanation: 'Synthetic Phase 16 fixture temperature evidence.',
        },
        {
          factor: 'Humidity',
          isEvaluable: true,
          score: 70 - index,
          aggregatedValue: 71,
          configuredWeight: 10,
          effectiveWeight: 0.154,
          explanation: 'Synthetic Phase 16 fixture humidity evidence.',
        },
        {
          factor: 'Soil',
          isEvaluable: false,
          score: null,
          aggregatedValue: null,
          configuredWeight: 25,
          effectiveWeight: null,
          explanation: 'Soil was not supplied.',
        },
      ],
      evidenceCoverage: {
        evaluatedFactorCount: 3,
        totalFactorCount: 4,
        evaluatedConfiguredWeightTotal: 65,
      },
      climateRisks: [],
      explanation: `${cropName} Phase 16 browser-test suitability explanation.`,
    })),
    limitations: [
      'Soil was not supplied and is excluded from the weather-only calculation.',
    ],
  };
}

function historyDetail() {
  return {
    recommendationId: 1001,
    forecastRunId,
    soilType: null,
    createdAt: '2026-10-13T09:00:00Z',
    forecast: {
      forecastRunId,
      forecastDate: '2026-10-12',
      targetStartDate: '2026-10-13',
      targetEndDate: '2026-10-19',
      modelVersion: 'v1',
      days: recommendationEvaluation(null).forecast.days,
    },
    crops: recommendationEvaluation(null).crops,
    provenance: ['Phase 16 browser fixture.'],
    limitations: ['Stored history is evidence snapshot based.'],
  };
}

function officerReview() {
  return {
    recommendationId: 1001,
    forecastRunId,
    userId: null,
    soilType: null,
    createdAt: '2026-10-13T09:00:00Z',
    evidenceSnapshotJson: JSON.stringify({
      forecast: {
        days: recommendationEvaluation(null).forecast.days.map(day => ({
          targetDate: day.targetDate,
          rainfall: { value: day.rainfall },
          temperature: { value: day.temperature },
          humidity: { value: day.humidity },
        })),
      },
    }),
    crops: recommendationEvaluation(null).crops.map((crop, index) => ({
      cropId: index + 1,
      cropName: crop.cropName,
      rainfallScore: crop.rainfallScore,
      temperatureScore: crop.temperatureScore,
      humidityScore: crop.humidityScore,
      soilScore: crop.soilScore,
      overallScore: crop.overallScore,
      suitabilityCategory: crop.suitabilityCategory,
      rank: crop.rank,
      evaluationStatus: crop.evaluationStatus,
      explanation: crop.explanation,
      evidenceSnapshotJson: '{}',
    })),
    validationHistory: [],
  };
}

function adminRows(url: string): unknown[] {
  if (url.includes('/admin/users')) {
    return [{ id: 1, name: 'Admin', email: 'admin@example.test', role: 'Administrator', isActive: true, createdAt: '2026-10-13T09:00:00Z' }];
  }

  return [{ id: 1, name: 'Paddy', isActive: true, createdAt: '2026-10-13T09:00:00Z' }];
}

function seasonalWindow(id: string, months: number[]) {
  return { id, season: 'Maha', establishmentMonths: months, harvestMonths: [2], sourcePages: [1], evidenceStatus: 'CALENDAR_SUPPORTED_CANDIDATE', interpretation: '', conditions: [], requiresLocalReview: true };
}

function seasonalEvidence() {
  return { geographicApplicability: 'Anuradhapura', documentedConditions: [], sourceUrls: [], validationStatus: '', limitations: [] };
}

function seasonalRange() {
  return {
    startDate: '2026-10-18', endDate: '2027-01-18', assessmentType: 'HISTORICAL_SEASONAL_CLIMATE', dataSource: '', evidenceStatus: 'CALENDAR_SUPPORTED_CANDIDATES', climateMonths: [], limitations: [],
    crops: [
      { cropName: 'Paddy', evidenceStatus: 'CALENDAR_SUPPORTED_CANDIDATE', suitabilityCategory: null, explanation: '', missingFactors: [], calendarEvidence: seasonalEvidence(), candidateWindows: [seasonalWindow('paddy-maha', [10, 11])] },
      { cropName: 'Maize', evidenceStatus: 'CLIMATE_CONTEXT_ONLY', suitabilityCategory: null, explanation: '', missingFactors: [], calendarEvidence: seasonalEvidence(), candidateWindows: [], alternativeWindows: [seasonalWindow('maize-yala', [4, 5])] },
    ],
  };
}

function seasonalCrop() {
  return {
    cropName: 'Paddy', startDate: '2026-10-18', endDate: '2027-01-18', assessmentType: 'HISTORICAL_SEASONAL_CLIMATE', dataSource: '', evidenceStatus: 'CALENDAR_SUPPORTED_CANDIDATE', climateMonths: [], candidateMonths: ['October'], explanation: '', limitations: [], calendarEvidence: seasonalEvidence(), candidateWindows: [seasonalWindow('paddy-maha', [10, 11])], alternativeWindows: [seasonalWindow('paddy-yala', [4, 5])],
  };
}
