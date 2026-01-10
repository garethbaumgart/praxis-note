import { test, expect } from '@playwright/test';
import { resetDatabase, seedTestUser } from '../helpers/db-reset';
import { getMockAuthHeaders, MockUser } from '../helpers/mock-auth';

let testUser: MockUser;

test.describe('Due Dates', () => {
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

  test('can set due date via API and see it displayed', async ({ page, request }) => {
    // Create a task
    const createRes = await request.post('/api/tasks', {
      headers: getMockAuthHeaders(testUser),
      data: { title: 'Task with due date' },
    });
    const task = await createRes.json();

    // Set due date to tomorrow
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    const dueDateStr = tomorrow.toISOString().split('T')[0];

    const setDueDateRes = await request.put(`/api/tasks/${task.id}/due-date`, {
      headers: getMockAuthHeaders(testUser),
      data: { date: dueDateStr },
    });
    expect(setDueDateRes.ok()).toBe(true);

    await setupAuth(page, testUser);
    await page.goto('/tasks');

    // Verify due date badge shows "Tomorrow"
    const taskCard = page.locator('.group').filter({ hasText: 'Task with due date' });
    await expect(taskCard.getByText('Tomorrow')).toBeVisible();
  });

  test('can clear due date via API', async ({ page, request }) => {
    // Create a task with a due date
    const createRes = await request.post('/api/tasks', {
      headers: getMockAuthHeaders(testUser),
      data: { title: 'Task to clear due date' },
    });
    const task = await createRes.json();

    // Set due date
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    const dueDateStr = tomorrow.toISOString().split('T')[0];
    await request.put(`/api/tasks/${task.id}/due-date`, {
      headers: getMockAuthHeaders(testUser),
      data: { date: dueDateStr },
    });

    // Clear the due date
    const clearRes = await request.delete(`/api/tasks/${task.id}/due-date`, {
      headers: getMockAuthHeaders(testUser),
    });
    expect(clearRes.ok()).toBe(true);

    await setupAuth(page, testUser);
    await page.goto('/tasks');

    // Verify due date text is not visible, but calendar icon is
    const taskCard = page.locator('.group').filter({ hasText: 'Task to clear due date' });
    await expect(taskCard.getByText('Tomorrow')).not.toBeVisible();
    await expect(taskCard.getByLabel('Set due date')).toBeVisible();
  });

  test('displays today due date with amber styling', async ({ page, request }) => {
    // Create a task
    const createRes = await request.post('/api/tasks', {
      headers: getMockAuthHeaders(testUser),
      data: { title: 'Task due today' },
    });
    const task = await createRes.json();

    // Set due date to today
    const today = new Date();
    const dueDateStr = today.toISOString().split('T')[0];

    await request.put(`/api/tasks/${task.id}/due-date`, {
      headers: getMockAuthHeaders(testUser),
      data: { date: dueDateStr },
    });

    await setupAuth(page, testUser);
    await page.goto('/tasks');

    const taskCard = page.locator('.group').filter({ hasText: 'Task due today' });
    const dueDateBadge = taskCard.getByText('Today');
    await expect(dueDateBadge).toBeVisible();
    await expect(dueDateBadge).toHaveClass(/text-amber-600/);
  });

  test('displays overdue date with red styling and exclamation icon', async ({ page, request }) => {
    // Create a task
    const createRes = await request.post('/api/tasks', {
      headers: getMockAuthHeaders(testUser),
      data: { title: 'Overdue task' },
    });
    const task = await createRes.json();

    // Set due date to yesterday
    const yesterday = new Date();
    yesterday.setDate(yesterday.getDate() - 1);
    const dueDateStr = yesterday.toISOString().split('T')[0];

    await request.put(`/api/tasks/${task.id}/due-date`, {
      headers: getMockAuthHeaders(testUser),
      data: { date: dueDateStr },
    });

    await setupAuth(page, testUser);
    await page.goto('/tasks');

    const taskCard = page.locator('.group').filter({ hasText: 'Overdue task' });
    const dueDateBadge = taskCard.getByText('Yesterday');
    await expect(dueDateBadge).toBeVisible();
    await expect(dueDateBadge).toHaveClass(/text-red-500/);
    // Verify exclamation icon is present
    await expect(taskCard.locator('.pi-exclamation-circle')).toBeVisible();
  });

  test('calendar icon is always visible in icon row', async ({ page, request }) => {
    // Create a task without a due date
    await request.post('/api/tasks', {
      headers: getMockAuthHeaders(testUser),
      data: { title: 'Task without due date' },
    });

    await setupAuth(page, testUser);
    await page.goto('/tasks');

    const taskCard = page.locator('.group').filter({ hasText: 'Task without due date' });
    // Calendar icon should be visible without hover
    await expect(taskCard.getByLabel('Set due date')).toBeVisible();
  });

  test('due date on completed task shows strikethrough', async ({ page, request }) => {
    // Create a task
    const createRes = await request.post('/api/tasks', {
      headers: getMockAuthHeaders(testUser),
      data: { title: 'Completed task with due date' },
    });
    const task = await createRes.json();

    // Set due date
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    const dueDateStr = tomorrow.toISOString().split('T')[0];
    await request.put(`/api/tasks/${task.id}/due-date`, {
      headers: getMockAuthHeaders(testUser),
      data: { date: dueDateStr },
    });

    // Move to Done
    await request.put(`/api/tasks/${task.id}/status`, {
      headers: getMockAuthHeaders(testUser),
      data: { status: 'Done' },
    });

    await setupAuth(page, testUser);
    await page.goto('/tasks');

    const taskCard = page.locator('.bg-done .group').filter({ hasText: 'Completed task with due date' });
    const dueDateButton = taskCard.locator('button').filter({ hasText: 'Tomorrow' });
    await expect(dueDateButton).toBeVisible();
    await expect(dueDateButton).toHaveClass(/line-through/);
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
