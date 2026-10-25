import { expect, test } from '@playwright/test';
import { mockPhase16Api, signInAs } from './phase16.fixtures';

test.beforeEach(async ({ page }) => {
  await mockPhase16Api(page);
});

test('public forecast and recommendation show evidence and optional soil', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByRole('heading', { name: /Clearer weather insight/ })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Seven-day weather forecast' })).toBeVisible();
  await expect(page.getByText('Forecast run', { exact: false }).first()).toBeVisible();
  await page.goto('/recommendations');
  await page.getByRole('button', { name: 'Find recommendations' }).click();
  await expect(page.locator('.recommendation-row')).toHaveCount(6);
  await expect(page.getByText('Soil was not supplied. It is excluded from this calculation.')).toBeVisible();
  await page.locator('.recommendation-row').first().getByText('View details').click();
  await expect(page.getByText('configured weight 40%').first()).toBeVisible();
  await page.getByLabel('Find a crop').fill('Paddy');
  await expect(page.locator('.recommendation-row')).toHaveCount(1);
});

test('public recommendation reports a safe API failure', async ({ page }) => {
  await page.route('**/api/recommendations/evaluate', route => route.fulfill({
    status: 503, contentType: 'application/json',
    body: JSON.stringify({ message: 'No complete forecast is available.' }),
  }));
  await page.goto('/recommendations');
  await page.getByRole('button', { name: 'Find recommendations' }).click();
  await expect(page.getByRole('alert')).toContainText('No complete forecast is available.');
});

test('seasonal planning keeps calendar candidates separate from the LSTM forecast', async ({ page }) => {
  await page.goto('/seasonal-planning');
  await page.getByRole('button', { name: 'Check periods' }).click();
  await expect(page.getByText('Conditional calendar window').first()).toBeVisible();
  await expect(page.getByText('No matching period')).toBeVisible();
  await expect(page.getByText('These dates are separate from the seven-day AI forecast.')).toBeVisible();
  await page.getByRole('tab', { name: 'Search by crop' }).click();
  await page.getByRole('button', { name: 'Show periods' }).click();
  await expect(page.getByText('Candidate establishment periods')).toBeVisible();
  await expect(page.getByRole('heading', { name: 'October – November' })).toBeVisible();
});

test('registered user can register, sign in, save, review history and sign out', async ({ page }) => {
  await page.goto('/register');
  await expect(page.getByText('Registration creates a Registered User account.')).toBeVisible();
  await page.getByLabel('Full name').fill('Registered Test User');
  await page.getByLabel('Email address').fill('registered@example.test');
  await page.getByLabel('Password', { exact: true }).fill('StrongPassword1!');
  await page.getByRole('button', { name: 'Create account' }).click();
  await expect(page).toHaveURL(/\/login$/);
  await page.getByLabel('Email address').fill('registered@example.test');
  await page.getByLabel('Password', { exact: true }).fill('StrongPassword1!');
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await expect(page).toHaveURL(/\/recommendations$/);
  await page.getByRole('button', { name: 'Find recommendations' }).click();
  await expect(page.getByText('Saved to history', { exact: true })).toBeVisible();
  await page.goto('/history');
  await expect(page.getByRole('heading', { name: 'Recommendation History' })).toBeVisible();
  await expect(page.getByText('Paddy (Suitable)')).toBeVisible();
  await page.getByRole('button', { name: 'Sign out' }).click();
  await expect(page).toHaveURL('/');
});

test('anonymous staff route redirects to staff login and invalid credentials are handled', async ({ page }) => {
  await page.goto('/admin');
  await expect(page).toHaveURL(/\/staff\/login$/);
  await page.getByLabel('Email address').fill('bad@example.test');
  await page.getByLabel('Password', { exact: true }).fill('wrong');
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('Invalid email or password.');
});

test('officer can review evidence and submit validation in the role workspace', async ({ page }) => {
  await signInAs(page, 'Agricultural Officer');
  await page.goto('/officer');
  await expect(page.getByRole('heading', { name: 'Pending Recommendation Reviews' })).toBeVisible();
  await page.getByRole('link', { name: 'Review' }).click();
  await expect(page.getByRole('heading', { name: 'Recommendation #1001' })).toBeVisible();
  await expect(page.getByLabel('Stored seven day forecast evidence')).toBeVisible();
  await expect(page.getByLabel('Suitability factor results').first()).toBeVisible();
  await page.getByLabel('Status').selectOption('Needs Review');
  await page.getByLabel('Comments').fill('Test fixture comment');
  await page.getByRole('button', { name: 'Submit validation' }).click();
  await expect(page.getByRole('status')).toContainText('Validation submitted.');
});

test('administrator opens configuration and sees safe forecast failure', async ({ page }) => {
  await signInAs(page, 'Administrator');
  await page.goto('/admin');
  await expect(page.getByRole('heading', { name: 'Administration' })).toBeVisible();
  await page.getByRole('button', { name: 'Crops', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Approved crop profiles' })).toBeVisible();
  await page.getByRole('button', { name: 'Generate forecast' }).click();
  await expect(page.getByRole('alert')).toContainText('Operational forecast generation is disabled in Phase 16 browser tests.');
});

test('mobile navigation exposes all destinations including registration', async ({ page, isMobile }) => {
  test.skip(!isMobile, 'Mobile navigation is shown only at narrow widths.');
  await page.goto('/');
  await page.locator('.public-mobile-menu summary').click();
  const nav = page.getByRole('navigation', { name: 'Mobile public navigation' });
  await expect(nav.getByRole('link', { name: 'Create account' })).toBeVisible();
  await nav.getByRole('link', { name: 'Seasonal planning' }).click();
  await expect(page).toHaveURL('/seasonal-planning');
});
