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
    // Create a note via API
    const createRes = await request.post('/api/notes', {
      headers: getMockAuthHeaders(testUser),
      data: { content: 'Original content' },
    });
    const note = await createRes.json();

    await setupAuth(page, testUser);
    await page.goto('/notes');

    // Click on the note to open the editor
    await page.getByText('Original content').click();

    // Wait for the editor dialog to appear
    await expect(page.getByRole('dialog')).toBeVisible();

    // Clear and update the content
    const textarea = page.locator('textarea[aria-label="Note content"]');
    await textarea.fill('Updated content');

    // Save the note and wait for the debounced API call to complete
    const updatePromise = page.waitForResponse(
      response => response.url().includes('/api/notes/') && response.request().method() === 'PUT'
    );
    await page.getByRole('button', { name: 'Save' }).click();
    await updatePromise;

    // Verify the dialog closed
    await expect(page.getByRole('dialog')).not.toBeVisible();

    // Verify the updated content is visible
    await expect(page.getByText('Updated content')).toBeVisible();

    // Verify the API was called correctly
    const updatedNote = await request.get(`/api/notes/${note.id}`, {
      headers: getMockAuthHeaders(testUser),
    });
    const noteData = await updatedNote.json();
    expect(noteData.content).toBe('Updated content');
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

    // Click the delete button
    await noteCard.getByLabel('Delete note').click();

    // Verify the note is no longer visible
    await expect(page.getByText('Delete me')).not.toBeVisible();
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
