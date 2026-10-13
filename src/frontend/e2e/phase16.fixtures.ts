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
      evaluationStatus: index >= 4 ? 'InsufficientEvidence' : 'PartiallyEvaluated',
      rainfallScore: index >= 4 ? null : 80 - index,
      temperatureScore: index >= 4 ? null : 75 - index,
      humidityScore: null,
      soilScore: null,
      overallScore: index >= 4 ? null : 74 - index,
      suitabilityCategory: index >= 4 ? null : 'Suitable',
      rank: index >= 4 ? null : index + 1,
      unavailableFactors: ['Humidity', 'Soil'],
      factors: [
        {
          factor: 'Rainfall',
          isEvaluable: index < 4,
          score: index >= 4 ? null : 80 - index,
          aggregatedValue: index >= 4 ? null : 28,
          configuredWeight: 25,
          effectiveWeight: index >= 4 ? null : 0.5,
          explanation: 'Synthetic Phase 16 fixture rainfall evidence.',
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
        evaluatedFactorCount: index >= 4 ? 0 : 2,
        totalFactorCount: 4,
        evaluatedConfiguredWeightTotal: index >= 4 ? 0 : 50,
      },
      climateRisks: [],
      explanation: `${cropName} Phase 16 browser-test suitability explanation.`,
    })),
    limitations: [
      'Scientific/agronomic validation remains separate from technical system validation.',
      'Missing factors are not replaced with fabricated scores.',
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
    evidenceSnapshotJson: '{}',
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
