import { test, expect } from '@playwright/test';
import { resetDatabase, seedTestUser } from '../helpers/db-reset';
import { getMockAuthHeader } from '../helpers/mock-auth';

test.describe('Authentication', () => {
  test.beforeEach(async () => {
    await resetDatabase();
  });

  test('unauthenticated user sees login page', async ({ page }) => {
    await page.goto('/');

    // Should see the login button
    await expect(page.getByText('Continue with Google')).toBeVisible();
  });

  test('unauthenticated API request returns 401', async ({ request }) => {
    const response = await request.get('/api/tasks');
    expect(response.status()).toBe(401);
  });

  test('mock auth header authenticates user', async ({ request }) => {
    // Seed test user in database
    const testUser = await seedTestUser();

    // Make authenticated request with mock header
    const meResponse = await request.get('/api/auth/me', {
      headers: getMockAuthHeader(testUser),
    });

    expect(meResponse.ok()).toBeTruthy();

    const meBody = await meResponse.json();
    expect(meBody.email).toBe(testUser.email);
    expect(meBody.name).toBe(testUser.name);
  });

  test('authenticated user can access tasks API', async ({ request }) => {
    // Seed test user
    const testUser = await seedTestUser();
    const headers = getMockAuthHeader(testUser);

    // Tasks endpoint should work with mock auth header
    const response = await request.get('/api/tasks', { headers });
    expect(response.ok()).toBeTruthy();

    const tasks = await response.json();
    expect(Array.isArray(tasks)).toBeTruthy();
  });

  test('request without mock header is unauthenticated', async ({ request }) => {
    // Seed test user (but don't include header)
    await seedTestUser();

    // Without the header, should get 401
    const response = await request.get('/api/auth/me');
    expect(response.status()).toBe(401);
  });
});
