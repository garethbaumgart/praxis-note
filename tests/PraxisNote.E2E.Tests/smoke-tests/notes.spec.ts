import { test, expect } from '@playwright/test';
import { seedTestUser } from '../helpers/db-reset';
import { getMockAuthHeaders, MockUser } from '../helpers/mock-auth';

// Use unique user suffix for this test file to avoid interference with parallel tests
const USER_SUFFIX = 5;
let testUser: MockUser;

test.describe('Notes', () => {
  // Run tests serially to avoid race conditions with shared database
  test.describe.configure({ mode: 'serial' });

  test.beforeAll(async () => {
    testUser = await seedTestUser(USER_SUFFIX);
  });

  test.beforeEach(async ({ request }) => {
    // Clean up notes before each test
    const notes = await request.get('/api/notes', {
      headers: getMockAuthHeaders(testUser),
    });
    const noteList = await notes.json();
    for (const note of noteList) {
      await request.delete(`/api/notes/${note.id}`, {
        headers: getMockAuthHeaders(testUser),
      });
    }
  });

  test('can create and view a note', async ({ page, request }) => {
    // Create a note via API
    await request.post('/api/notes', {
      headers: getMockAuthHeaders(testUser),
      data: { content: 'Test note content' },
    });

    await setupAuth(page, testUser);
    await page.goto('/notes');

    // Verify note is visible in the grid
    await expect(page.getByText('Test note content')).toBeVisible();
  });

  test('can edit note content', async ({ page, request }) => {
    // Create a note via API with unique content
    const originalContent = `Original content ${Date.now()}`;
    const createRes = await request.post('/api/notes', {
      headers: getMockAuthHeaders(testUser),
      data: { content: originalContent },
    });
    const note = await createRes.json();

    await setupAuth(page, testUser);
    await page.goto('/notes');

    // Wait for the note to appear
    await expect(page.getByText(originalContent)).toBeVisible();

    // Find the note card with our content and click it to navigate to editor
    const noteCard = page.locator('.note-card').filter({ hasText: originalContent });
    await expect(noteCard).toBeVisible();
    await noteCard.click();

    // Wait for navigation to editor page
    await page.waitForURL(`**/notes/${note.id}`);

    // Wait for TipTap editor to be ready
    const editor = page.locator('.ProseMirror');
    await expect(editor).toBeVisible({ timeout: 5000 });

    // Wait for the editor to fully initialize and show the original content
    await expect(editor).toContainText('Original content', { timeout: 5000 });

    // Focus and clear using triple-click + delete (selects paragraph in TipTap)
    await editor.click({ clickCount: 3 });
    await page.waitForTimeout(100);
    await page.keyboard.press('Delete');
    await page.waitForTimeout(100);

    // Type new content
    await editor.type('Updated content', { delay: 50 });

    // Wait for TipTap to process the input
    await page.waitForTimeout(500);

    // Set up response listener for debounced PUT (auto-save)
    const updatePromise = page.waitForResponse(
      response => response.url().includes('/api/notes/') && response.request().method() === 'PUT',
      { timeout: 10000 }
    );

    // Trigger auto-save by waiting for the debounce (or press Ctrl+S)
    await page.keyboard.press('Control+s');

    // Wait for the API call to complete
    await updatePromise;

    // Verify via API that content was persisted
    const updatedNote = await request.get(`/api/notes/${note.id}`, {
      headers: getMockAuthHeaders(testUser),
    });
    const noteData = await updatedNote.json();
    // TipTap stores content as JSON, so check if the text is in the content
    expect(noteData.content).toContain('Updated content');
  });

  test('can delete a note', async ({ page, request }) => {
    // Create a note via API
    await request.post('/api/notes', {
      headers: getMockAuthHeaders(testUser),
      data: { content: 'Delete me' },
    });

    await setupAuth(page, testUser);
    await page.goto('/notes');

    // Verify note is visible
    await expect(page.getByText('Delete me')).toBeVisible();

    // Find the note card and hover to reveal delete button
    const noteCard = page.locator('.note-card').filter({ hasText: 'Delete me' });
    await noteCard.hover();

    // Click the delete button to start confirmation
    await noteCard.getByLabel('Delete note').click();

    // Click confirm to actually delete
    await noteCard.getByLabel('Confirm delete note').click();

    // Verify the note is no longer visible
    await expect(page.getByText('Delete me')).not.toBeVisible();
  });

  test('can promote checkbox to task on newly created note', async ({ request }) => {
    // This test verifies the fix for the bug where promoting a checkbox
    // on a newly created note would fail with a 404 error because
    // checkboxes weren't extracted during note creation.

    // Create a note with a checkbox via API (simulates creating a note with checkboxes)
    const tiptapContent = JSON.stringify({
      type: 'doc',
      content: [
        {
          type: 'taskList',
          content: [
            {
              type: 'taskItem',
              attrs: { checked: false },
              content: [
                {
                  type: 'paragraph',
                  content: [{ type: 'text', text: 'Buy groceries' }],
                },
              ],
            },
          ],
        },
      ],
    });

    const createRes = await request.post('/api/notes', {
      headers: getMockAuthHeaders(testUser),
      data: { content: tiptapContent },
    });
    expect(createRes.ok()).toBeTruthy();
    const note = await createRes.json();

    // Immediately try to promote the checkbox to a task (without editing the note first)
    // This should succeed now that CreateNote extracts checkboxes
    const promoteRes = await request.post(`/api/notes/${note.id}/checkboxes/cb-1/promote`, {
      headers: getMockAuthHeaders(testUser),
    });

    // Should succeed (200 OK), not fail with 404
    expect(promoteRes.ok()).toBeTruthy();
    expect(promoteRes.status()).toBe(200);

    const task = await promoteRes.json();
    expect(task.title).toBe('Buy groceries');
    expect(task.status).toBe('Todo');

    // Verify the task was actually created
    const tasksRes = await request.get('/api/tasks', {
      headers: getMockAuthHeaders(testUser),
    });
    const tasks = await tasksRes.json();
    const createdTask = tasks.find((t: any) => t.title === 'Buy groceries');
    expect(createdTask).toBeDefined();
    expect(createdTask.status).toBe('Todo');
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
