const { chromium } = require('../src/frontend/node_modules/@playwright/test');
const fs = require('node:fs');
const path = require('node:path');

const evidenceDir = path.join(__dirname, 'evidence');
fs.mkdirSync(evidenceDir, { recursive: true });

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true });
  const summary = {};
  try {
    for (const [name, viewport] of Object.entries({ desktop: { width: 1440, height: 900 }, mobile: { width: 390, height: 844 } })) {
      const page = await browser.newPage({ viewport });
      await page.goto('http://127.0.0.1:4200/', { waitUntil: 'networkidle' });
      await page.getByRole('heading', { name: 'Seven-day weather forecast' }).waitFor();
      summary[`${name}ForecastDays`] = await page.locator('.dashboard-forecast-days article').count();
      await page.screenshot({ path: path.join(evidenceDir, `${name}-public-light.png`), fullPage: true });
      if (name === 'mobile') {
        await page.locator('.public-mobile-menu summary').click();
        summary.mobileRegisterVisible = await page.getByRole('navigation', { name: 'Mobile public navigation' }).getByRole('link', { name: 'Create account' }).isVisible();
      }
      await page.goto('http://127.0.0.1:4200/recommendations', { waitUntil: 'networkidle' });
      await page.getByRole('button', { name: 'Find recommendations' }).click();
      await page.locator('.recommendation-row').first().waitFor();
      summary[`${name}RecommendationRows`] = await page.locator('.recommendation-row').count();
      summary[`${name}AllUnsuitableWarning`] = await page.getByText('All assessed crops are Unsuitable', { exact: false }).isVisible();
      await page.screenshot({ path: path.join(evidenceDir, `${name}-recommendation-light.png`), fullPage: true });
      await page.getByRole('button', { name: 'Switch to dark mode' }).click();
      await page.screenshot({ path: path.join(evidenceDir, `${name}-recommendation-dark.png`), fullPage: true });
      await page.close();
    }
    const page = await browser.newPage();
    await page.goto('http://127.0.0.1:4200/seasonal-planning');
    await page.locator('input[name="rangeStart"]').fill('2026-11-01');
    await page.locator('input[name="rangeEnd"]').fill('2026-11-30');
    await page.getByRole('button', { name: 'Check periods' }).click();
    await page.getByText('Conditional calendar window').first().waitFor();
    summary.rangeCandidateCards = await page.locator('.seasonal-crop-card').count();
    await page.getByRole('tab', { name: 'Search by crop' }).click();
    await page.getByRole('button', { name: 'Show periods' }).click();
    await page.getByText('Candidate establishment periods').waitFor();
    summary.cropCandidateWindows = await page.locator('.seasonal-window-card').count();
    await page.close();
    console.log(JSON.stringify(summary));
  } finally {
    await browser.close();
  }
})().catch(error => { console.error(error.message); process.exitCode = 1; });
