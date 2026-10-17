import { createRequire } from 'node:module';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';

const email = process.env.QA_ADMIN_EMAIL;
const password = process.env.QA_ADMIN_PASSWORD;
if (!email || !password) throw new Error('Administrator credentials were not supplied to the local audit process.');

const require = createRequire(import.meta.url);
const { chromium } = require('../src/frontend/node_modules/playwright');
const evidenceDir = path.resolve('qa-evidence');
await mkdir(evidenceDir, { recursive: true });

const browser = await chromium.launch({ executablePath: 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe', headless: false, slowMo: 300 });
const context = await browser.newContext({ viewport: { width: 1440, height: 900 } });
const page = await context.newPage();
const result = { login: null, generation: null, dashboard: null, recommendation: null, apiResponses: [] };
page.on('response', response => {
  if (response.url().includes('/api/')) result.apiResponses.push({ url: response.url(), status: response.status(), method: response.request().method() });
});

try {
  await page.goto('http://127.0.0.1:4300/login', { waitUntil: 'networkidle' });
  await page.locator('input[name="email"]').fill(email);
  await page.locator('input[name="password"]').fill(password);
  const loginResponse = page.waitForResponse(response => response.url().includes('/api/auth/login') && response.request().method() === 'POST');
  await page.getByRole('button', { name: 'Login' }).click();
  const login = await loginResponse;
  result.login = { status: login.status(), url: page.url() };
  await page.waitForURL('**/admin', { timeout: 10000 });

  const generationResponse = page.waitForResponse(response => response.url().includes('/api/admin/forecasts/operational') && response.request().method() === 'POST', { timeout: 90000 });
  await page.getByRole('button', { name: 'Generate operational forecast' }).click();
  const generation = await generationResponse;
  const generationBody = await generation.json().catch(() => null);
  result.generation = { status: generation.status(), body: generationBody, pageText: await page.locator('body').innerText() };
  await page.screenshot({ path: path.join(evidenceDir, 'AUTH-01-admin-operational-forecast.png'), fullPage: true });

  if (generation.status() === 200 && generationBody?.forecastRunId) {
    await page.goto('http://127.0.0.1:4300/', { waitUntil: 'networkidle' });
    await page.locator('.forecast-days article').first().waitFor({ state: 'visible', timeout: 15000 });
    const latest = await page.evaluate(async () => (await fetch('/api/forecasts/latest')).json());
    result.dashboard = { forecastRunId: latest.forecastRunId, modelVersion: latest.modelVersion, start: latest.forecastPeriodStart, end: latest.forecastPeriodEnd, days: latest.dailyForecasts };
    await page.screenshot({ path: path.join(evidenceDir, 'AUTH-02-fresh-forecast-dashboard.png'), fullPage: true });

    await page.goto('http://127.0.0.1:4300/recommendations', { waitUntil: 'networkidle' });
    const recommendationResponse = page.waitForResponse(response => response.url().includes('/api/recommendations/evaluate') && response.request().method() === 'POST');
    await page.getByRole('button', { name: 'Generate recommendation' }).click();
    const recommendation = await recommendationResponse;
    const recommendationBody = await recommendation.json();
    await page.locator('.crop-card').first().waitFor({ state: 'visible', timeout: 15000 });
    result.recommendation = { forecastRunId: recommendationBody.forecastRunId, crops: recommendationBody.crops.map(crop => ({ cropName: crop.cropName, assessmentState: crop.assessmentState, overallScore: crop.overallScore, rank: crop.rank })) };
    await page.screenshot({ path: path.join(evidenceDir, 'AUTH-03-fresh-forecast-recommendations.png'), fullPage: true });
  }
} finally {
  await context.close();
  await browser.close();
  await writeFile(path.join(evidenceDir, 'authenticated-operational-workflow.json'), JSON.stringify(result, null, 2));
}
