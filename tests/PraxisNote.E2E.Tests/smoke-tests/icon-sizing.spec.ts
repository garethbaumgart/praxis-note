import { test, expect } from '@playwright/test';
import { seedTestUser } from '../helpers/db-reset';
import { getMockAuthHeaders, MockUser } from '../helpers/mock-auth';

// Use unique user suffix for this test file to avoid interference with parallel tests
const USER_SUFFIX = 3;
let testUser: MockUser;

test.describe('Icon Sizing', () => {
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

  test('comment icon matches comment text font-size', async ({ page, request }) => {
    // Create a task with a comment
    const createRes = await request.post('/api/tasks', {
      headers: getMockAuthHeaders(testUser),
      data: { title: 'Task with comment' },
    });
    const task = await createRes.json();

    await request.post(`/api/tasks/${task.id}/comments`, {
      headers: getMockAuthHeaders(testUser),
      data: { content: 'This is a test comment' },
    });

    await setupAuth(page, testUser);
    await page.goto('/tasks');

    // Target desktop grid (visible at test viewport)
    const desktopGrid = page.locator('[class*="md:grid"]');

    // Wait for task to be visible
    await expect(desktopGrid.getByText('Task with comment')).toBeVisible();

    // Expand comments section by clicking the comment toggle button
    const commentToggle = desktopGrid.getByLabel('Show comments');
    await commentToggle.click();

    // Wait for comment to be visible
    await expect(desktopGrid.getByText('This is a test comment')).toBeVisible();

    // Get the comment row container
    const commentRow = desktopGrid.locator('.group\\/comment').first();
    const commentIcon = commentRow.locator('i.pi-comment');
    const commentText = commentRow.locator('span').first();

    // Get computed font-sizes
    const iconFontSize = await commentIcon.evaluate((el) =>
      window.getComputedStyle(el).fontSize
    );
    const textFontSize = await commentText.evaluate((el) =>
      window.getComputedStyle(el).fontSize
    );

    // Icon and text should have the same font-size (both inherit from parent's text-xs)
    expect(iconFontSize).toBe(textFontSize);
  });

  test('add comment icon matches placeholder text font-size', async ({ page, request }) => {
    // Create a task
    await request.post('/api/tasks', {
      headers: getMockAuthHeaders(testUser),
      data: { title: 'Task for icon test' },
    });

    await setupAuth(page, testUser);
    await page.goto('/tasks');

    // Target desktop grid (visible at test viewport)
    const desktopGrid = page.locator('[class*="md:grid"]');

    await expect(desktopGrid.getByText('Task for icon test')).toBeVisible();

    // Expand comments section by clicking the comment toggle button
    const commentToggle = desktopGrid.getByLabel('Show comments');
    await commentToggle.click();

    // Find the task card and the add comment textarea within it
    const taskCard = desktopGrid.locator('.group').filter({ hasText: 'Task for icon test' });
    const plusIcon = taskCard.locator('i.pi-plus');
    const textarea = taskCard.getByPlaceholder('Add comment...');

    await expect(plusIcon).toBeVisible();
    await expect(textarea).toBeVisible();

    // Get computed font-sizes
    const iconFontSize = await plusIcon.evaluate((el) =>
      window.getComputedStyle(el).fontSize
    );
    const textareaFontSize = await textarea.evaluate((el) =>
      window.getComputedStyle(el).fontSize
    );

    // Icon and textarea should have the same font-size
    expect(iconFontSize).toBe(textareaFontSize);
  });

  test('delete button icon matches confirm text font-size', async ({ page, request }) => {
    // Create a task
    await request.post('/api/tasks', {
      headers: getMockAuthHeaders(testUser),
      data: { title: 'Delete icon test' },
    });

    await setupAuth(page, testUser);
    await page.goto('/tasks');

    // Target desktop grid (visible at test viewport)
    const desktopGrid = page.locator('[class*="md:grid"]');
    const taskCard = desktopGrid.locator('.group').filter({ hasText: 'Delete icon test' });

    // Hover to reveal delete button, then click to show confirmation
    await taskCard.hover();
    await taskCard.getByLabel('Delete task').click();

    // Now the confirm button with icon and text should be visible
    const confirmButton = taskCard.getByLabel('Confirm delete task');
    const trashIcon = confirmButton.locator('i.pi-trash');
    const confirmText = confirmButton.locator('span');

    // Get computed font-sizes
    const iconFontSize = await trashIcon.evaluate((el) =>
      window.getComputedStyle(el).fontSize
    );
    const textFontSize = await confirmText.evaluate((el) =>
      window.getComputedStyle(el).fontSize
    );

    // Icon and text should have the same font-size (both inherit from button's text-xs)
    expect(iconFontSize).toBe(textFontSize);
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
