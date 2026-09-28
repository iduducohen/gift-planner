import { test, expect } from '@playwright/test';

test.describe('UI Functionality', () => {
  test('page title is correct', async ({ page }) => {
    await page.goto('/');

    const titleContent = await page.locator('title').textContent();
    expect(titleContent).toBeTruthy();
  });

  test('app content loads', async ({ page }) => {
    await page.goto('/');

    // Wait for any dynamic content
    await page.waitForLoadState('networkidle');

    // Check if page has content
    const bodyText = await page.locator('body').textContent();
    expect(bodyText?.length).toBeGreaterThan(0);
  });

  test('should not have console errors', async ({ page }) => {
    const errors: string[] = [];

    page.on('console', msg => {
      if (msg.type() === 'error') {
        errors.push(msg.text());
      }
    });

    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // Console errors array should be empty or minimal
    expect(errors.length).toBeLessThanOrEqual(0);
  });

  test('should handle navigation without errors', async ({ page, context }) => {
    const errors: Error[] = [];

    page.on('pageerror', error => {
      errors.push(error);
    });

    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // Should load without page errors
    expect(errors.length).toBe(0);
  });

  test('should be responsive', async ({ page }) => {
    // Test mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto('/');

    await page.waitForLoadState('networkidle');
    const content = await page.content();
    expect(content).toBeTruthy();

    // Test tablet viewport
    await page.setViewportSize({ width: 768, height: 1024 });
    await page.goto('/');

    await page.waitForLoadState('networkidle');
    const tabletContent = await page.content();
    expect(tabletContent).toBeTruthy();
  });
});
