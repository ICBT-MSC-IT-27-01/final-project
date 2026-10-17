import { createRequire } from 'node:module';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';

const require = createRequire(import.meta.url);
const { chromium } = require('../src/frontend/node_modules/playwright');
const evidenceDir = path.resolve('qa-evidence');
const observations = [];
const publicCases = [];

await mkdir(evidenceDir, { recursive: true });
const browser = await chromium.launch({
  executablePath: 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
  headless: false,
  slowMo: 500,
});
const context = await browser.newContext({ viewport: { width: 1440, height: 900 } });
const page = await context.newPage();
page.on('response', async response => {
  const url = response.url();
  if (url.includes('/api/')) observations.push({ at: new Date().toISOString(), url, status: response.status() });
});
page.on('console', message => {
  if (message.type() === 'error') observations.push({ at: new Date().toISOString(), consoleError: message.text() });
});

async function capture(name) {
  await page.screenshot({ path: path.join(evidenceDir, `${name}.png`), fullPage: true });
}
async function rangeCase(id, start, end, label) {
  await page.goto('http://127.0.0.1:4300/seasonal-planning', { waitUntil: 'networkidle' });
  const forms = page.locator('form');
  const form = forms.nth(0);
  await form.locator('input[name="rangeStart"]').fill(start);
  await form.locator('input[name="rangeEnd"]').fill(end);
  await form.getByRole('button', { name: 'Explore evidence' }).click();
  await page.waitForTimeout(1200);
  const body = await page.locator('body').innerText();
  const error = await page.locator('[role="alert"]').allTextContents();
  const cards = await page.locator('.crop-card').count();
  publicCases.push({ id, label, start, end, error, cards, cardText: await page.locator('.crop-card').allTextContents(), evidence: `${id}.png` });
  await capture(id);
}

try {
  await page.goto('http://127.0.0.1:4300/', { waitUntil: 'networkidle' });
  await capture('PUB-01-home-desktop');
  await page.goto('http://127.0.0.1:4300/recommendations', { waitUntil: 'networkidle' });
  const recommendationResponse = page.waitForResponse(response =>
    response.url().includes('/api/recommendations/evaluate') && response.request().method() === 'POST');
  await page.getByRole('button', { name: 'Generate recommendation' }).click();
  await recommendationResponse;
  await page.locator('.crop-card').first().waitFor({ state: 'visible', timeout: 15000 });
  publicCases.push({
    id: 'PUB-04-live-recommendation',
    label: 'Live seven-day forecast to crop suitability request',
    error: await page.locator('[role="alert"]').allTextContents(),
    cropHeadings: await page.locator('.crop-card h2').allTextContents(),
    cropResults: await page.locator('.crop-card').evaluateAll(cards => cards.map(card => ({
      crop: card.querySelector('h2')?.textContent?.trim(),
      renderedText: card.textContent?.replace(/\s+/g, ' ').trim(),
    }))),
    evidence: 'PUB-04-live-recommendation.png',
  });
  await capture('PUB-04-live-recommendation');
  await capture('PUB-02-recommendations-desktop');
  await page.goto('http://127.0.0.1:4300/seasonal-planning', { waitUntil: 'networkidle' });
  await capture('PUB-03-seasonal-desktop');

  await rangeCase('SP-01-seven-days', '2026-10-17', '2026-10-24', 'Seven days');
  await rangeCase('SP-02-thirty-days', '2026-10-17', '2026-11-16', 'Thirty days');
  await rangeCase('SP-03-three-months', '2026-10-17', '2027-01-17', 'Three months');
  await rangeCase('SP-04-six-months', '2026-10-17', '2027-04-17', 'Six months');
  await rangeCase('SP-05-twelve-months', '2026-10-17', '2027-10-17', 'Twelve months');
  await rangeCase('SP-05b-custom-valid', '2026-12-03', '2027-02-11', 'Custom valid range');
  await rangeCase('SP-05c-past', '2025-10-17', '2025-10-24', 'Past range');
  await rangeCase('SP-06-reversed', '2027-04-17', '2026-10-17', 'Reversed range');
  await rangeCase('SP-07-over-366', '2026-10-17', '2027-10-19', 'More than 366 days');
  await page.goto('http://127.0.0.1:4300/seasonal-planning', { waitUntil: 'networkidle' });
  const emptyForm = page.locator('form').nth(0);
  await emptyForm.locator('input[name="rangeStart"]').fill('');
  await emptyForm.locator('input[name="rangeEnd"]').fill('');
  await emptyForm.getByRole('button', { name: 'Explore evidence' }).click();
  publicCases.push({ id: 'SP-08-empty', label: 'Empty required date inputs', nativeValidity: await emptyForm.locator('input[name="rangeStart"]').evaluate(input => input.validationMessage), evidence: 'SP-08-empty.png' });
  await capture('SP-08-empty');

  for (const crop of ['Paddy', 'Maize', 'Green Gram', 'Cowpea', 'Groundnut', 'Chilli']) {
    await page.goto('http://127.0.0.1:4300/seasonal-planning', { waitUntil: 'networkidle' });
    const form = page.locator('form').nth(1);
    await form.locator('select[name="crop"]').selectOption({ label: crop });
    await form.locator('input[name="cropStart"]').fill('2026-10-17');
    await form.locator('input[name="cropEnd"]').fill('2027-04-17');
    await form.getByRole('button', { name: 'Explore calendar evidence' }).click();
    await page.waitForTimeout(1200);
    const error = await page.locator('[role="alert"]').allTextContents();
    const heading = await page.locator('h2').allTextContents();
    publicCases.push({ id: `CP-${crop.replaceAll(' ', '-').toUpperCase()}`, label: `${crop} to cultivation period`, error, heading, body: await page.locator('body').innerText(), evidence: `CP-${crop.replaceAll(' ', '-').toLowerCase()}.png` });
    await capture(`CP-${crop.replaceAll(' ', '-').toLowerCase()}`);
  }

  await page.setViewportSize({ width: 768, height: 1024 });
  await page.goto('http://127.0.0.1:4300/seasonal-planning', { waitUntil: 'networkidle' });
  await capture('RSP-01-tablet');
  await page.setViewportSize({ width: 390, height: 844 });
  await capture('RSP-02-mobile');
  await page.goto('http://127.0.0.1:4300/admin', { waitUntil: 'networkidle' });
  await capture('SEC-01-admin-anonymous');
  await page.goto('http://127.0.0.1:4300/officer', { waitUntil: 'networkidle' });
  await capture('SEC-02-officer-anonymous');
} finally {
  await context.close();
  await browser.close();
  await writeFile(path.join(evidenceDir, 'visible-browser-observations.json'), JSON.stringify({ publicCases, observations }, null, 2));
}
