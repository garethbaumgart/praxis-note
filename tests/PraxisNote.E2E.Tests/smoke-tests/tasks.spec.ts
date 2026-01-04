import { test, expect } from '@playwright/test';
import { resetDatabase, seedTestUser } from '../helpers/db-reset';
import { getMockAuthHeaders, MockUser } from '../helpers/mock-auth';

let testUser: MockUser;

test.describe('Tasks', () => {
  test.beforeAll(async () => {
    await resetDatabase();
    testUser = await seedTestUser();
  });

  test.beforeEach(async ({ request }) => {
    // Clean up tasks before each test
    const tasks = await request.get('/api/tasks', {
      headers: getMockAuthHeaders(testUser),
    });
    const taskList = await tasks.json();
    for (const task of taskList) {
      await request.delete(`/api/tasks/${task.id}`, {
        headers: getMockAuthHeaders(testUser),
      });
    }
  });

  test('can create and view a task', async ({ page, request }) => {
    await request.post('/api/tasks', {
      headers: getMockAuthHeaders(testUser),
      data: { title: 'Test Task' },
    });

    await setupAuth(page, testUser);
    await page.goto('/tasks');

    await expect(page.getByText('Test Task')).toBeVisible();
    await expect(page.locator('.bg-todo').getByText('Test Task')).toBeVisible();
  });

  test('edit and delete buttons are accessible', async ({ page, request }) => {
    await request.post('/api/tasks', {
      headers: getMockAuthHeaders(testUser),
      data: { title: 'Action Test' },
    });

    await setupAuth(page, testUser);
    await page.goto('/tasks');

    const taskCard = page.locator('.group').filter({ hasText: 'Action Test' });

    // Verify action buttons exist and are accessible
    await expect(taskCard.getByLabel('Edit task')).toBeAttached();
    await expect(taskCard.getByLabel('Delete task')).toBeAttached();
  });

  test('can delete a task', async ({ page, request }) => {
    await request.post('/api/tasks', {
      headers: getMockAuthHeaders(testUser),
      data: { title: 'Delete Me' },
    });

    await setupAuth(page, testUser);
    await page.goto('/tasks');

    await expect(page.getByText('Delete Me')).toBeVisible();

    const taskCard = page.locator('.group').filter({ hasText: 'Delete Me' });
    await taskCard.getByLabel('Delete task').click();

    await expect(page.getByText('Delete Me')).not.toBeVisible();
  });

});

async function setupAuth(page: any, user: MockUser): Promise<void> {
  await page.route('**/api/**', async (route: any) => {
    const headers = {
      ...route.request().headers(),
      ...getMockAuthHeaders(user),
    };
    await route.continue({ headers });
  });
}
