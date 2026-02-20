import { test, expect } from '@playwright/test';
import { seedTestUser } from '../helpers/db-reset';
import { getMockAuthHeaders, MockUser } from '../helpers/mock-auth';

// Use unique user suffix for this test file to avoid interference with parallel tests
const USER_SUFFIX = 6;
let testUser: MockUser;

test.describe('Meetings', () => {
  // Run tests serially to avoid race conditions with shared database
  test.describe.configure({ mode: 'serial' });

  test.beforeAll(async () => {
    testUser = await seedTestUser(USER_SUFFIX);
  });

  test.beforeEach(async ({ request }) => {
    // Clean up meetings and tasks before each test
    const meetings = await request.get('/api/meetings', {
      headers: getMockAuthHeaders(testUser),
    });
    const meetingList = await meetings.json();
    for (const meeting of meetingList) {
      await request.delete(`/api/meetings/${meeting.id}`, {
        headers: getMockAuthHeaders(testUser),
      });
    }

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

  test.skip('can promote checkbox to task from meeting notes', async ({ page, request }) => {
    // Create a meeting via API
    const createRes = await request.post('/api/meetings', {
      headers: getMockAuthHeaders(testUser),
      data: {
        title: 'Sprint Planning',
        meetingDate: new Date().toISOString(),
      },
    });
    const meeting = await createRes.json();

    // Create a note for the meeting with a checkbox
    const noteContent = JSON.stringify({
      type: 'doc',
      content: [
        {
          type: 'paragraph',
          content: [{ type: 'text', text: 'Meeting notes' }],
        },
        {
          type: 'taskList',
          content: [
            {
              type: 'taskItem',
              attrs: { checked: false },
              content: [
                {
                  type: 'paragraph',
                  content: [{ type: 'text', text: 'Follow up with team' }],
                },
              ],
            },
          ],
        },
      ],
    });

    await request.post(`/api/meetings/${meeting.id}/note`, {
      headers: getMockAuthHeaders(testUser),
      data: { content: noteContent },
    });

    await setupAuth(page, testUser);
    await page.goto(`/meetings/${meeting.id}`);

    // Wait for the editor to load
    const editor = page.locator('.ProseMirror');
    await expect(editor).toBeVisible({ timeout: 10000 });

    // Wait for content to load - the editor needs time to initialize and render the note content
    await page.waitForTimeout(1000);

    // Wait for the checkbox to appear
    const checkbox = editor.locator('li[data-type="taskItem"]').first();
    await expect(checkbox).toBeVisible({ timeout: 10000 });
    await expect(checkbox).toContainText('Follow up with team');

    // Hover over the checkbox to reveal the promote button
    await checkbox.hover();

    // Click the promote to task button
    const promoteButton = checkbox.locator('button[aria-label="Promote to task"]');
    await expect(promoteButton).toBeVisible({ timeout: 3000 });
    await promoteButton.click();

    // Wait for the promotion API call to complete
    await page.waitForResponse(
      response => response.url().includes('/promote') && response.request().method() === 'POST',
      { timeout: 10000 }
    );

    // Navigate to tasks page
    await page.goto('/tasks');

    // Verify the task was created
    const main = page.locator('main');
    await expect(main.getByText('Follow up with team')).toBeVisible({ timeout: 10000 });

    // Go back to the meeting to verify the checkbox shows the link badge
    await page.goto(`/meetings/${meeting.id}`);
    await expect(editor).toBeVisible({ timeout: 10000 });

    // Wait for the checkbox status badge to appear
    const checkboxWithBadge = editor.locator('li[data-type="taskItem"]').filter({ hasText: 'Follow up with team' });
    await expect(checkboxWithBadge).toBeVisible();

    // Verify the status badge is displayed (it should show "Todo" or similar)
    const badge = checkboxWithBadge.locator('.checkbox-status-badge');
    await expect(badge).toBeVisible({ timeout: 5000 });
  });

  test.skip('can create meeting note on first save', async ({ page, request }) => {
    // Create a meeting via API
    const createRes = await request.post('/api/meetings', {
      headers: getMockAuthHeaders(testUser),
      data: {
        title: 'Team Sync',
        meetingDate: new Date().toISOString(),
      },
    });
    const meeting = await createRes.json();

    await setupAuth(page, testUser);
    await page.goto(`/meetings/${meeting.id}`);

    // Wait for the editor to load
    const editor = page.locator('.ProseMirror');
    await expect(editor).toBeVisible({ timeout: 10000 });

    // Wait for editor to fully initialize
    await page.waitForTimeout(500);

    // Type some content including a checkbox
    await editor.click();
    await page.keyboard.type('Meeting notes with checkbox', { delay: 50 });
    await page.keyboard.press('Enter');

    // Use slash command to insert a task list
    await page.keyboard.type('/task', { delay: 50 });

    // Wait for slash command menu and select task list
    const slashMenu = page.locator('.slash-menu');
    await expect(slashMenu).toBeVisible({ timeout: 5000 });
    const taskListOption = slashMenu.locator('.slash-menu-item').filter({ hasText: 'Task List' }).first();
    await expect(taskListOption).toBeVisible();
    await taskListOption.click();

    // Type the checkbox text
    await page.waitForTimeout(200);
    await page.keyboard.type('Action item from meeting', { delay: 50 });

    // Save the note (Ctrl+S)
    await page.keyboard.press('Control+s');

    // Wait for the save API call to complete
    await page.waitForResponse(
      response => response.url().includes(`/api/meetings/${meeting.id}/note`) && response.request().method() === 'POST',
      { timeout: 10000 }
    );

    // Verify note was created by checking the API
    const noteRes = await request.get(`/api/meetings/${meeting.id}/note`, {
      headers: getMockAuthHeaders(testUser),
    });
    expect(noteRes.status()).toBe(200);
    const noteData = await noteRes.json();
    expect(noteData.content).toContain('Action item from meeting');

    // Now try to promote the checkbox
    const checkbox = editor.locator('li[data-type="taskItem"]').filter({ hasText: 'Action item from meeting' });
    await expect(checkbox).toBeVisible();
    await checkbox.hover();

    const promoteButton = checkbox.locator('button[aria-label="Promote to task"]');
    await expect(promoteButton).toBeVisible({ timeout: 3000 });
    await promoteButton.click();

    // Wait for the promotion API call
    await page.waitForResponse(
      response => response.url().includes('/promote') && response.request().method() === 'POST',
      { timeout: 10000 }
    );

    // Verify task was created
    await page.goto('/tasks');
    const main = page.locator('main');
    await expect(main.getByText('Action item from meeting')).toBeVisible({ timeout: 10000 });
  });
});

async function setupAuth(page: any, user: MockUser): Promise<void> {
  // Use setExtraHTTPHeaders to ensure ALL requests get auth headers
  // This is more reliable than page.route() which can have timing issues
  await page.setExtraHTTPHeaders(getMockAuthHeaders(user));
}
