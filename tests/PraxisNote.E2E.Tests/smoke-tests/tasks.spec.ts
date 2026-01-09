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

  test('click-to-edit title and delete button are accessible', async ({ page, request }) => {
    await request.post('/api/tasks', {
      headers: getMockAuthHeaders(testUser),
      data: { title: 'Action Test' },
    });

    await setupAuth(page, testUser);
    await page.goto('/tasks');

    const taskCard = page.locator('.group').filter({ hasText: 'Action Test' });

    // Hover to reveal delete button (hidden by default, shown on hover)
    await taskCard.hover();

    // Verify delete button is accessible after hovering
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

    await expect(page.getByText('Delete Me')).toBeVisible();

    const taskCard = page.locator('.group').filter({ hasText: 'Delete Me' });
    
    // Hover over the task card to reveal the delete button
    await taskCard.hover();
    
    // Click the delete button to trigger confirmation
    await taskCard.getByLabel('Delete task').click();
    
    // Click the confirmation button to complete deletion
    await taskCard.getByLabel('Confirm delete task').click();

    await expect(page.getByText('Delete Me')).not.toBeVisible();
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

    // Verify task starts in Todo column
    await expect(page.locator('.bg-todo').getByText('Workflow Task')).toBeVisible();

    // Move to InProgress via API
    await request.put(`/api/tasks/${task.id}/status`, {
      headers: getMockAuthHeaders(testUser),
      data: { status: 'InProgress' },
    });

    // Navigate to refresh data (setupAuth persists across navigation)
    await page.goto('/tasks');
    await expect(page.locator('.bg-inprogress').getByText('Workflow Task')).toBeVisible();

    // Move to Done via API
    await request.put(`/api/tasks/${task.id}/status`, {
      headers: getMockAuthHeaders(testUser),
      data: { status: 'Done' },
    });

    // Navigate to refresh data
    await page.goto('/tasks');
    await expect(page.locator('.bg-done').getByText('Workflow Task')).toBeVisible();
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
