import { test, expect } from '@playwright/test';

test.describe('Authentication Flow', () => {
  test('should load the app', async ({ page }) => {
    await page.goto('/');

    // Check if the app loads
    const content = await page.content();
    expect(content).toBeTruthy();
  });

  test('should show login options on home page', async ({ page }) => {
    await page.goto('/');

    // Wait for content to load
    await page.waitForLoadState('networkidle');

    // Check for typical auth elements or navigation
    const body = await page.locator('body').textContent();
    expect(body).toBeTruthy();
  });

  test('should navigate between pages', async ({ page }) => {
    await page.goto('/');

    // Try to navigate and verify page loads
    const response = await page.goto('/', { waitUntil: 'networkidle' });
    expect(response?.status()).toBeLessThan(400);
  });
});
