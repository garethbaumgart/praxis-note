import { test, expect, type Page } from '@playwright/test';
import { seedTestUser } from '../helpers/db-reset';
import { getMockAuthHeaders, MockUser } from '../helpers/mock-auth';

// Use unique user suffix for this test file to avoid interference with parallel tests
const USER_SUFFIX = 10;
let testUser: MockUser;

test.describe('Transcript Import', () => {
  test.describe.configure({ mode: 'serial' });

  test.beforeAll(async () => {
    testUser = await seedTestUser(USER_SUFFIX);
  });

  test('transcript import tab renders and accepts paste input', async ({ page }) => {
    await setupAuth(page, testUser);
    await page.goto('/meetings');

    // Click the Import button on the meetings page header
    const importButton = page.locator('button[aria-label="Import meetings"]');
    await expect(importButton).toBeVisible({ timeout: 10000 });
    await importButton.click();

    // Click the Transcript tab
    const transcriptTab = page.locator('button').filter({ hasText: 'Transcript' });
    await expect(transcriptTab).toBeVisible();
    await transcriptTab.click();

    // Verify the textarea is visible
    const textarea = page.locator('textarea');
    await expect(textarea).toBeVisible();

    // Type some transcript text
    await textarea.fill('Sample meeting transcript text from Google Gemini');

    // Verify the Parse button is enabled
    const parseButton = page.locator('button').filter({ hasText: 'Parse with AI' });
    await expect(parseButton).toBeEnabled();
  });
});

async function setupAuth(page: Page, user: MockUser): Promise<void> {
  await page.setExtraHTTPHeaders(getMockAuthHeaders(user));
}
