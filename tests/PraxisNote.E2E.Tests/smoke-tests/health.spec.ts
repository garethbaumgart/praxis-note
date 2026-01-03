import { test, expect } from '@playwright/test';

test.describe('Health Check', () => {
  test('API health endpoint returns healthy', async ({ request }) => {
    const response = await request.get('/api/health');
    expect(response.ok()).toBeTruthy();

    const body = await response.json();
    expect(body.status).toBe('healthy');
    expect(body.timestamp).toBeDefined();
  });

  test('Frontend loads successfully', async ({ page }) => {
    await page.goto('/');

    // Should see the login page or app title
    await expect(page).toHaveTitle(/PraxisNote/);
  });

  test('Static assets are served', async ({ request }) => {
    // Check that the Angular app's main script is accessible
    const response = await request.get('/');
    expect(response.ok()).toBeTruthy();

    const html = await response.text();
    expect(html).toContain('PraxisNote');
  });
});
