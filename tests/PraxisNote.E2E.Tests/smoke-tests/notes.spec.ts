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

    // Verify note is visible in the main content area (not sidebar)
    const main = page.locator('main');
    await expect(main.getByText('Test note content')).toBeVisible();
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

    // Wait for the note to appear in the main content area (with longer timeout for reliability)
    const main = page.locator('main');
    await expect(main.getByText(originalContent)).toBeVisible({ timeout: 10000 });

    // Find the note card with our content and click it to navigate to editor
    const noteCard = page.locator('.note-card').filter({ hasText: originalContent });
    await expect(noteCard).toBeVisible();
    await noteCard.click();

    // Wait for navigation to editor page
    await page.waitForURL(`**/notes/${note.id}`, { timeout: 10000 });

    // Wait for TipTap editor to be ready
    const editor = page.locator('.ProseMirror');
    await expect(editor).toBeVisible();

    // Wait for the editor to fully initialize and show the original content
    await expect(editor).toContainText(originalContent, { timeout: 10000 });

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

    // Verify note is visible in the main content area (not sidebar)
    const main = page.locator('main');
    await expect(main.getByText('Delete me')).toBeVisible();

    // Find the note card and hover to reveal delete button
    const noteCard = page.locator('.note-card').filter({ hasText: 'Delete me' });
    await noteCard.hover();

    // Click the delete button to start confirmation
    // Uses .first() because dual-button pattern renders mobile + desktop buttons
    await noteCard.getByLabel('Delete note').first().click();

    // Click confirm to actually delete
    await noteCard.getByLabel('Confirm delete note').first().click();

    // Verify the note is no longer visible in the main content area
    await expect(main.getByText('Delete me')).not.toBeVisible();
  });
});

async function setupAuth(page: any, user: MockUser): Promise<void> {
  // Use setExtraHTTPHeaders to ensure ALL requests get auth headers
  // This is more reliable than page.route() which can have timing issues
  await page.setExtraHTTPHeaders(getMockAuthHeaders(user));
}
