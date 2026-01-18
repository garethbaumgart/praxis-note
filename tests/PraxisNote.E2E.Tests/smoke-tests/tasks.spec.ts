import { test, expect } from '@playwright/test';
import { seedTestUser } from '../helpers/db-reset';
import { getMockAuthHeaders, MockUser } from '../helpers/mock-auth';

// Use unique user suffix for this test file to avoid interference with parallel tests
const USER_SUFFIX = 4;
let testUser: MockUser;

test.describe('Tasks', () => {
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

  test('can create and view a task', async ({ page, request }) => {
    await request.post('/api/tasks', {
      headers: getMockAuthHeaders(testUser),
      data: { title: 'Test Task' },
    });

    await setupAuth(page, testUser);
    await page.goto('/tasks');

    // Target desktop grid (visible at test viewport) - task appears in both mobile and desktop layouts
    const desktopGrid = page.locator('[class*="md:grid"]');
    await expect(desktopGrid.getByText('Test Task')).toBeVisible();
    await expect(desktopGrid.locator('.bg-todo').getByText('Test Task')).toBeVisible();
  });

  test('click-to-edit title and delete button are accessible', async ({ page, request }) => {
    await request.post('/api/tasks', {
      headers: getMockAuthHeaders(testUser),
      data: { title: 'Action Test' },
    });

    await setupAuth(page, testUser);
    await page.goto('/tasks');

    // Target desktop grid (visible at test viewport)
    const desktopGrid = page.locator('[class*="md:grid"]');
    const taskCard = desktopGrid.locator('.group').filter({ hasText: 'Action Test' });

    // Verify delete button is accessible
    await expect(taskCard.getByLabel('Delete task')).toBeAttached();

    // Verify title is clickable (click-to-edit pattern)
    const titleText = taskCard.getByText('Action Test');
    await expect(titleText).toBeVisible();
    await expect(titleText).toHaveClass(/cursor-pointer/);
  });

  test('can delete a task', async ({ page, request }) => {
    await request.post('/api/tasks', {
      headers: getMockAuthHeaders(testUser),
      data: { title: 'Delete Me' },
    });

    await setupAuth(page, testUser);
    await page.goto('/tasks');

    // Target desktop grid (visible at test viewport)
    const desktopGrid = page.locator('[class*="md:grid"]');
    await expect(desktopGrid.getByText('Delete Me')).toBeVisible();

    const taskCard = desktopGrid.locator('.group').filter({ hasText: 'Delete Me' });

    // Hover to reveal delete button, then click to show confirmation
    await taskCard.hover();
    await taskCard.getByLabel('Delete task').click();

    // Click confirm to delete
    await taskCard.getByLabel('Confirm delete task').click();

    await expect(desktopGrid.getByText('Delete Me')).not.toBeVisible();
  });

  test('task moves through kanban states: Todo -> InProgress -> Done', async ({ page, request }) => {
    // Create a new task
    const createRes = await request.post('/api/tasks', {
      headers: getMockAuthHeaders(testUser),
      data: { title: 'Workflow Task' },
    });
    const task = await createRes.json();

    await setupAuth(page, testUser);
    await page.goto('/tasks');

    // Target desktop grid (visible at test viewport)
    const desktopGrid = page.locator('[class*="md:grid"]');

    // Verify task starts in Todo column
    await expect(desktopGrid.locator('.bg-todo').getByText('Workflow Task')).toBeVisible();

    // Move to InProgress via API
    await request.put(`/api/tasks/${task.id}/status`, {
      headers: getMockAuthHeaders(testUser),
      data: { status: 'InProgress' },
    });

    // Navigate to refresh data (setupAuth persists across navigation)
    await page.goto('/tasks');
    await expect(desktopGrid.locator('.bg-inprogress').getByText('Workflow Task')).toBeVisible();

    // Move to Done via API
    await request.put(`/api/tasks/${task.id}/status`, {
      headers: getMockAuthHeaders(testUser),
      data: { status: 'Done' },
    });

    // Navigate to refresh data
    await page.goto('/tasks');
    await expect(desktopGrid.locator('.bg-done').getByText('Workflow Task')).toBeVisible();
  });

  test('can toggle priority flag on and off', async ({ page, request }) => {
    // Create a task via API
    const createRes = await request.post('/api/tasks', {
      headers: getMockAuthHeaders(testUser),
      data: { title: 'Priority Test Task' },
    });
    const task = await createRes.json();

    await setupAuth(page, testUser);
    await page.goto('/tasks');

    // Target desktop grid (visible at test viewport)
    const desktopGrid = page.locator('[class*="md:grid"]');
    const taskCard = desktopGrid.locator('app-task-card').filter({ hasText: 'Priority Test Task' });

    // Verify task is visible and priority is off (outline flag icon)
    await expect(taskCard).toBeVisible();
    await expect(taskCard.getByLabel('Mark as priority')).toBeVisible();
    await expect(taskCard.locator('i.pi-flag')).toBeVisible();

    // Toggle priority ON via API
    const toggleOnRes = await request.patch(`/api/tasks/${task.id}/priority`, {
      headers: getMockAuthHeaders(testUser),
    });
    if (!toggleOnRes.ok()) {
      console.log('Toggle ON failed:', toggleOnRes.status(), await toggleOnRes.text());
    }
    expect(toggleOnRes.ok()).toBeTruthy();

    // Refresh to see the change
    await page.goto('/tasks');

    // Verify priority is now on (filled flag icon with rose color)
    await expect(taskCard.locator('i.pi-flag-fill')).toBeVisible();
    await expect(taskCard.getByLabel('Remove priority')).toBeVisible();
    await expect(taskCard.getByLabel('Remove priority')).toHaveClass(/text-danger/);

    // Toggle priority OFF via API
    const toggleOffRes = await request.patch(`/api/tasks/${task.id}/priority`, {
      headers: getMockAuthHeaders(testUser),
    });
    expect(toggleOffRes.ok()).toBeTruthy();

    // Refresh to see the change
    await page.goto('/tasks');

    // Verify priority is off again
    await expect(taskCard.locator('i.pi-flag')).toBeVisible();
    await expect(taskCard.getByLabel('Mark as priority')).toBeVisible();
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
