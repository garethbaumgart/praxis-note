import { test, expect } from '@playwright/test';
import { seedTestUser } from '../helpers/db-reset';
import { getMockAuthHeaders, MockUser } from '../helpers/mock-auth';
import { seedArchivedTask, seedRecentDoneTask } from '../helpers/archive-helpers';

// Use unique user suffix for this test file to avoid interference with parallel tests
const USER_SUFFIX = 5;
let testUser: MockUser;

test.describe('Archive', () => {
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

    // Also clean archived tasks
    const archivedTasks = await request.get('/api/tasks?includeArchived=true', {
      headers: getMockAuthHeaders(testUser),
    });
    const archivedList = await archivedTasks.json();
    for (const task of archivedList) {
      await request.delete(`/api/tasks/${task.id}`, {
        headers: getMockAuthHeaders(testUser),
      });
    }
  });

  test('archive count badge shows number of archived tasks', async ({ page }) => {
    // Seed an archived task (completed > 7 days ago)
    await seedArchivedTask(testUser.userId, 'Archived Task 1', 10);
    await seedArchivedTask(testUser.userId, 'Archived Task 2', 14);

    await setupAuth(page, testUser);
    await page.goto('/tasks');

    // Wait for the archive button to appear (should show count of 2)
    const archiveButton = page.getByLabel('Show archived tasks');
    await expect(archiveButton).toBeVisible();
    await expect(archiveButton.locator('span')).toHaveText('2');
  });

  test('archive toggle switches Done column to Archive view', async ({ page }) => {
    // Seed both a recent done task and an archived task
    await seedRecentDoneTask(testUser.userId, 'Recent Done Task');
    await seedArchivedTask(testUser.userId, 'Old Archived Task', 10);

    await setupAuth(page, testUser);
    await page.goto('/tasks');

    // Initially should see the recent done task
    await expect(page.getByText('Recent Done Task')).toBeVisible();
    await expect(page.getByText('Old Archived Task')).not.toBeVisible();

    // Column header should be labeled "Done"
    await expect(page.locator('.bg-done').getByText('Done', { exact: true })).toBeVisible();

    // Click the archive button
    const archiveButton = page.getByLabel('Show archived tasks');
    await archiveButton.click();

    // Now should see archived task and column label should change to "Archive"
    await expect(page.getByText('Old Archived Task')).toBeVisible();
    await expect(page.getByText('Recent Done Task')).not.toBeVisible();

    // Should have archive styling (amber background)
    await expect(page.locator('.bg-archive')).toBeVisible();
    await expect(page.locator('.bg-archive').getByText('Archive', { exact: true })).toBeVisible();
  });

  test('archive view can be toggled back to Done view', async ({ page }) => {
    await seedRecentDoneTask(testUser.userId, 'Recent Task');
    await seedArchivedTask(testUser.userId, 'Archived Task', 10);

    await setupAuth(page, testUser);
    await page.goto('/tasks');

    // Toggle to archive view
    await page.getByLabel('Show archived tasks').click();
    await expect(page.getByText('Archived Task')).toBeVisible();

    // Toggle back to done view
    await page.getByLabel('Show recent tasks').click();
    await expect(page.getByText('Recent Task')).toBeVisible();
    await expect(page.getByText('Archived Task')).not.toBeVisible();
  });

  test('archived tasks can be edited', async ({ page }) => {
    await seedArchivedTask(testUser.userId, 'Edit This Archived Task', 10);

    await setupAuth(page, testUser);
    await page.goto('/tasks');

    // Toggle to archive view
    await page.getByLabel('Show archived tasks').click();

    // Click on task title to edit
    const taskTitle = page.getByText('Edit This Archived Task');
    await taskTitle.click();

    // Should enter edit mode - a textarea should be focused
    const textarea = page.locator('textarea:focus');
    await expect(textarea).toBeVisible();
    await expect(textarea).toHaveValue('Edit This Archived Task');
  });

  test('no archive button when there are no archived tasks', async ({ page }) => {
    // Only seed a recent done task, no archived ones
    await seedRecentDoneTask(testUser.userId, 'Recent Only Task');

    await setupAuth(page, testUser);
    await page.goto('/tasks');

    // Archive button should not be visible
    await expect(page.getByLabel('Show archived tasks')).not.toBeVisible();
  });

  test('empty archive view shows appropriate message', async ({ page, request }) => {
    // Seed an archived task first so button appears
    const archivedId = await seedArchivedTask(testUser.userId, 'Temp Archived Task', 10);

    await setupAuth(page, testUser);
    await page.goto('/tasks');

    // Toggle to archive view
    await page.getByLabel('Show archived tasks').click();
    await expect(page.getByText('Temp Archived Task')).toBeVisible();

    // Delete the archived task
    await request.delete(`/api/tasks/${archivedId}`, {
      headers: getMockAuthHeaders(testUser),
    });

    // Refresh page
    await page.goto('/tasks');

    // Archive button should no longer be visible (count is 0)
    await expect(page.getByLabel('Show archived tasks')).not.toBeVisible();
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
