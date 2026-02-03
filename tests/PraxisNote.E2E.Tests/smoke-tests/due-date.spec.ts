import { test, expect } from '@playwright/test';
import { seedTestUser } from '../helpers/db-reset';
import { getMockAuthHeaders, MockUser } from '../helpers/mock-auth';

// Use unique user suffix for this test file to avoid interference with parallel tests
const USER_SUFFIX = 2;
let testUser: MockUser;

// Format date as YYYY-MM-DD in local time (not UTC)
function formatLocalDate(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

test.describe('Due Dates', () => {
  // Run tests serially to avoid race conditions with shared database
  test.describe.configure({ mode: 'serial' });

  test.beforeAll(async () => {
    testUser = await seedTestUser(USER_SUFFIX);
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
    const dueDateStr = formatLocalDate(tomorrow);

    const setDueDateRes = await request.put(`/api/tasks/${task.id}/due-date`, {
      headers: getMockAuthHeaders(testUser),
      data: { date: dueDateStr },
    });
    expect(setDueDateRes.ok()).toBe(true);

    await setupAuth(page, testUser);
    await page.goto('/tasks');

    // Target desktop grid (visible at test viewport)
    const desktopGrid = page.locator('[class*="md:grid"]');

    // Verify due date badge shows "Tomorrow"
    const taskCard = desktopGrid.locator('.group').filter({ hasText: 'Task with due date' });
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
    const dueDateStr = formatLocalDate(tomorrow);
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

    // Target desktop grid (visible at test viewport)
    const desktopGrid = page.locator('[class*="md:grid"]');

    // Verify due date text is not visible, but calendar icon is
    const taskCard = desktopGrid.locator('.group').filter({ hasText: 'Task to clear due date' });
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
    const dueDateStr = formatLocalDate(today);

    await request.put(`/api/tasks/${task.id}/due-date`, {
      headers: getMockAuthHeaders(testUser),
      data: { date: dueDateStr },
    });

    await setupAuth(page, testUser);
    await page.goto('/tasks');

    // Target desktop grid (visible at test viewport)
    const desktopGrid = page.locator('[class*="md:grid"]');
    const taskCard = desktopGrid.locator('.group').filter({ hasText: 'Task due today' });
    // Use button selector to avoid matching task title "Task due today"
    const dueDateButton = taskCard.locator('button').filter({ hasText: 'Today' });
    await expect(dueDateButton).toBeVisible();
    await expect(dueDateButton).toHaveClass(/text-due-today-foreground/);
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
    const dueDateStr = formatLocalDate(yesterday);

    await request.put(`/api/tasks/${task.id}/due-date`, {
      headers: getMockAuthHeaders(testUser),
      data: { date: dueDateStr },
    });

    await setupAuth(page, testUser);
    await page.goto('/tasks');

    // Target desktop grid (visible at test viewport)
    const desktopGrid = page.locator('[class*="md:grid"]');
    const taskCard = desktopGrid.locator('.group').filter({ hasText: 'Overdue task' });
    // Class is on the button, not the span text inside
    const dueDateButton = taskCard.locator('button').filter({ hasText: 'Yesterday' });
    await expect(dueDateButton).toBeVisible();
    await expect(dueDateButton).toHaveClass(/text-overdue-foreground/);
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

    // Target desktop grid (visible at test viewport)
    const desktopGrid = page.locator('[class*="md:grid"]');
    const taskCard = desktopGrid.locator('.group').filter({ hasText: 'Task without due date' });
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
    const dueDateStr = formatLocalDate(tomorrow);
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

    // Target desktop grid (visible at test viewport)
    const desktopGrid = page.locator('[class*="md:grid"]');
    const taskCard = desktopGrid.locator('.bg-done .group').filter({ hasText: 'Completed task with due date' });
    const dueDateButton = taskCard.locator('button').filter({ hasText: 'Tomorrow' });
    await expect(dueDateButton).toBeVisible();
    await expect(dueDateButton).toHaveClass(/line-through/);
  });

});

async function setupAuth(page: any, user: MockUser): Promise<void> {
  // Use setExtraHTTPHeaders to ensure ALL requests get auth headers
  // This is more reliable than page.route() which can have timing issues
  await page.setExtraHTTPHeaders(getMockAuthHeaders(user));
}
