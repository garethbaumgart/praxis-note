import { test, expect, type APIRequestContext } from '@playwright/test';
import { resetDatabase, seedTestUser } from '../helpers/db-reset';

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

  test('test login endpoint creates session', async ({ request }) => {
    // Seed test user in database
    const testUser = await seedTestUser();

    // Use test login endpoint
    const loginResponse = await request.post('/api/auth/test-login', {
      data: {
        userId: testUser.userId,
        email: testUser.email,
        name: testUser.name,
      },
    });

    expect(loginResponse.ok()).toBeTruthy();
    const loginBody = await loginResponse.json();
    expect(loginBody.message).toBe('Test login successful');

    // Verify session is established - /api/auth/me should work
    const meResponse = await request.get('/api/auth/me');
    expect(meResponse.ok()).toBeTruthy();

    const meBody = await meResponse.json();
    expect(meBody.email).toBe(testUser.email);
    expect(meBody.name).toBe(testUser.name);
  });

  test('authenticated user can access tasks API', async ({ request }) => {
    // Seed and login
    const testUser = await seedTestUser();
    await request.post('/api/auth/test-login', {
      data: {
        userId: testUser.userId,
        email: testUser.email,
        name: testUser.name,
      },
    });

    // Now tasks endpoint should work
    const response = await request.get('/api/tasks');
    expect(response.ok()).toBeTruthy();

    const tasks = await response.json();
    expect(Array.isArray(tasks)).toBeTruthy();
  });

  test('logout clears session', async ({ request }) => {
    // Seed and login
    const testUser = await seedTestUser();
    await request.post('/api/auth/test-login', {
      data: {
        userId: testUser.userId,
        email: testUser.email,
        name: testUser.name,
      },
    });

    // Verify logged in
    let meResponse = await request.get('/api/auth/me');
    expect(meResponse.ok()).toBeTruthy();

    // Logout
    const logoutResponse = await request.post('/api/auth/logout');
    expect(logoutResponse.ok()).toBeTruthy();

    // Should be logged out now
    meResponse = await request.get('/api/auth/me');
    expect(meResponse.status()).toBe(401);
  });
});
