import { test, expect, Page } from '@playwright/test';
import { seedTestUser } from '../helpers/db-reset';
import { getMockAuthHeaders, MockUser } from '../helpers/mock-auth';

const USER_SUFFIX = 10;
let testUser: MockUser;

test.describe('Settings — AI Keys', () => {
  test.describe.configure({ mode: 'serial' });

  test.beforeAll(async () => {
    testUser = await seedTestUser(USER_SUFFIX);
  });

  test.beforeEach(async ({ request }) => {
    // Clean up AI keys before each test
    for (const provider of ['Anthropic', 'OpenAI', 'Gemini']) {
      await request.delete(`/api/ai-keys/${provider}`, {
        headers: getMockAuthHeaders(testUser),
      }).catch(() => { /* ignore 404 */ });
    }
  });

  test('can add, view, and remove a Gemini AI key', async ({ page }) => {
    await setupAuth(page, testUser);
    await page.goto('/settings');

    // Find AI & API Keys section
    const aiSection = page.locator('section').filter({ hasText: 'AI & API Keys' });
    await expect(aiSection).toBeVisible();

    // Find Gemini provider card and expand it
    const geminiCard = aiSection.locator('app-ai-key-provider-card').filter({ hasText: 'Google Gemini' });
    await expect(geminiCard).toBeVisible();
    await geminiCard.locator('button').first().click();

    // Enter a test API key
    const keyInput = geminiCard.locator('input[type="password"]');
    await expect(keyInput).toBeVisible();
    await keyInput.fill('AIzaTestKey1234567890');

    // Click Validate & Save
    await geminiCard.getByText('Validate & Save').click();

    // Wait for the key to be saved — should show Connected status
    await expect(geminiCard.getByText('Connected')).toBeVisible({ timeout: 15000 });

    // Verify key hint is displayed
    await geminiCard.locator('button').first().click(); // re-expand drawer
    await expect(geminiCard.locator('code')).toBeVisible();

    // Remove the key
    await geminiCard.getByLabel('Remove API key').click();

    // Verify Connected status is gone
    await expect(geminiCard.getByText('Connected')).not.toBeVisible({ timeout: 5000 });
  });
});

async function setupAuth(page: Page, user: MockUser): Promise<void> {
  await page.setExtraHTTPHeaders(getMockAuthHeaders(user));
}
