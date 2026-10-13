import { expect, test } from '@playwright/test';
import { mockPhase16Api, signInAs } from './phase16.fixtures';

test.beforeEach(async ({ page }) => {
  await mockPhase16Api(page);
});

test('public dashboard and recommendation workflow render forecast evidence and insufficient-evidence states', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'Latest AI Weather Forecast' })).toBeVisible();
  await expect(page.getByText('2026-10-13 to 2026-10-19')).toBeVisible();
  await expect(page.getByText('Paddy')).toBeVisible();
  await expect(page.getByText('Chilli')).toBeVisible();

  await page.getByRole('link', { name: 'Generate recommendation' }).click();
  await expect(page.getByRole('heading', { name: 'Crop Recommendation' })).toBeVisible();
  await page.getByRole('button', { name: 'Generate recommendation' }).click();

  await expect(page.getByText('Read-only', { exact: true })).toBeVisible();
  await expect(page.getByText('Forecast run')).toBeVisible();
  await expect(page.getByText('Missing: Humidity, Soil').first()).toBeVisible();
  await expect(page.getByText('InsufficientEvidence').first()).toBeVisible();
  await expect(page.getByText('Scientific/agronomic validation remains separate')).toBeVisible();
});

test('public recommendation workflow displays safe API failure messages', async ({ page }) => {
  await page.route('**/api/recommendations/evaluate', route => route.fulfill({
    status: 503,
    contentType: 'application/json',
    body: JSON.stringify({ message: 'No complete forecast is available.' }),
  }));

  await page.goto('/recommendations');
  await page.getByRole('button', { name: 'Generate recommendation' }).click();

  await expect(page.getByRole('alert')).toContainText('No complete forecast is available.');
});

test('registered user can register, log in, log out, and view history without role escalation', async ({ page }) => {
  await page.goto('/register');
  await expect(page.getByText('Registered User only')).toBeVisible();
  await expect(page.locator('select')).toHaveCount(0);
  await page.getByLabel('Name').fill('Registered Test User');
  await page.getByLabel('Email').fill('registered@example.test');
  await page.getByLabel('Password').fill('StrongPassword1!');
  await page.getByRole('button', { name: 'Register' }).click();
  await expect(page).toHaveURL(/\/login$/);

  await page.getByLabel('Email').fill('registered@example.test');
  await page.getByLabel('Password').fill('StrongPassword1!');
  await page.getByRole('button', { name: 'Login' }).click();
  await expect(page).toHaveURL(/\/recommendations$/);
  await expect(page.getByRole('link', { name: 'History' })).toBeVisible();

  await page.getByRole('link', { name: 'History' }).click();
  await expect(page.getByRole('heading', { name: 'Recommendation History' })).toBeVisible();
  await expect(page.getByText('Paddy (Suitable)')).toBeVisible();

  await page.getByRole('button', { name: 'Logout' }).click();
  await expect(page.getByRole('link', { name: 'Register', exact: true })).toBeVisible();
});

test('invalid login and protected route redirects are safe', async ({ page }) => {
  await page.goto('/admin');
  await expect(page).toHaveURL(/\/login$/);

  await page.getByLabel('Email').fill('bad@example.test');
  await page.getByLabel('Password').fill('wrong');
  await page.getByRole('button', { name: 'Login' }).click();

  await expect(page.getByRole('alert')).toContainText('Invalid email or password.');
});

test('agricultural officer can validate only through the officer workflow', async ({ page }) => {
  await signInAs(page, 'Agricultural Officer');
  await page.goto('/officer');

  await expect(page.getByRole('heading', { name: 'Pending Recommendation Reviews' })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Admin' })).toHaveCount(0);
  await page.getByRole('link', { name: 'Review' }).click();
  await expect(page.getByRole('heading', { name: 'Recommendation #1001' })).toBeVisible();
  await page.getByLabel('Status').selectOption('Needs Review');
  await page.getByLabel('Comments').fill('Test fixture comment');
  await page.getByRole('button', { name: 'Submit validation' }).click();

  await expect(page.getByRole('status')).toContainText('Validation submitted.');
});

test('administrator can view admin dashboard and forecast generation fails closed in E2E', async ({ page }) => {
  await signInAs(page, 'Administrator');
  await page.goto('/admin');

  await expect(page.getByRole('heading', { name: 'Administration' })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Admin' })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Officer' })).toHaveCount(0);
  await page.getByRole('button', { name: 'Generate operational forecast' }).click();

  await expect(page.getByRole('alert')).toContainText('Operational forecast generation is disabled in Phase 16 browser tests.');
});

test('mobile viewport keeps public recommendation content reachable', async ({ page }) => {
  await page.goto('/recommendations');
  await page.getByRole('button', { name: 'Generate recommendation' }).click();

  await expect(page.getByRole('heading', { name: 'Crop Recommendation' })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Paddy' })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Groundnut' })).toBeVisible();
});
