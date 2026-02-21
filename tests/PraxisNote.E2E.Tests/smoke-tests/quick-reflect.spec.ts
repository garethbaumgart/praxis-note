import { test, expect, type Page } from '@playwright/test';
import { seedTestUser } from '../helpers/db-reset';
import { getMockAuthHeaders, MockUser } from '../helpers/mock-auth';

// Use unique user suffix for this test file to avoid interference with parallel tests
const USER_SUFFIX = 9;
let testUser: MockUser;

test.describe('Quick Reflect', () => {
  test.describe.configure({ mode: 'serial' });

  test.beforeAll(async () => {
    testUser = await seedTestUser(USER_SUFFIX);
  });

  test.beforeEach(async ({ request }) => {
    // Clean up meetings before each test
    const meetings = await request.get('/api/meetings', {
      headers: getMockAuthHeaders(testUser),
    });
    const meetingList = await meetings.json();
    for (const meeting of meetingList) {
      await request.delete(`/api/meetings/${meeting.id}`, {
        headers: getMockAuthHeaders(testUser),
      });
    }
  });

  test('can complete quick reflect with emoji selections', async ({ page, request }) => {
    // Create a meeting with behavioral analysis
    const createRes = await request.post('/api/meetings', {
      headers: getMockAuthHeaders(testUser),
      data: {
        title: 'Test Meeting',
        meetingDate: new Date().toISOString(),
        transcriptContent: 'Sample transcript',
        behavioralAnalysis: JSON.stringify({
          speakingDynamics: {
            talkTimeByParticipant: [{ participant: 'User', percentage: 50, duration: '5m' }],
            interruptionPatterns: [],
            questionVsStatementRatio: { 'User': 0.5 },
          },
          sentimentTone: {
            participantSentiments: [{ participant: 'User', sentiment: 'positive', score: 0.8 }],
            toneShifts: [],
            emotionalIndicators: [],
          },
          communicationPatterns: {
            overallClarity: 0.8,
            followUpPatterns: [],
            engagementLevels: [{ participant: 'User', level: 'high', indicators: [] }],
          },
          redFlags: [],
        }),
        status: 'Ready',
      },
    });
    const meeting = await createRes.json();

    await setupAuth(page, testUser);
    await page.goto(`/meetings/${meeting.id}`);

    // Wait for page to load
    await expect(page.locator('h1, input[type="text"]')).toBeVisible({ timeout: 10000 });

    // Navigate to meeting with quick reflect query param
    await page.goto(`/meetings/${meeting.id}?openQuickReflect=true`);

    // Wait for quick reflect dialog to open
    const dialog = page.locator('.quick-reflect-dialog');
    await expect(dialog).toBeVisible({ timeout: 5000 });

    // Verify all 4 dimensions are shown
    await expect(page.getByText('Talk time')).toBeVisible();
    await expect(page.getByText('Engagement')).toBeVisible();
    await expect(page.getByText('Tone')).toBeVisible();
    await expect(page.getByText('Interruptions')).toBeVisible();

    // Select an emoji for each dimension (selecting the medium option)
    const emojiButtons = page.locator('.emoji-btn');
    await expect(emojiButtons).toHaveCount(12); // 4 dimensions × 3 levels

    // Select medium option for each dimension (indices 1, 4, 7, 10)
    await emojiButtons.nth(1).click(); // Talk time - medium
    await emojiButtons.nth(4).click(); // Engagement - medium
    await emojiButtons.nth(7).click(); // Tone - medium
    await emojiButtons.nth(10).click(); // Interruptions - medium

    // Verify selected state
    await expect(emojiButtons.nth(1)).toHaveClass(/selected/);
    await expect(emojiButtons.nth(4)).toHaveClass(/selected/);
    await expect(emojiButtons.nth(7)).toHaveClass(/selected/);
    await expect(emojiButtons.nth(10)).toHaveClass(/selected/);

    // Click save button
    const saveButton = page.getByRole('button', { name: 'Save' });
    await expect(saveButton).toBeVisible();
    await saveButton.click();

    // Wait for completion state
    await expect(page.getByText('Reflected')).toBeVisible({ timeout: 5000 });

    // Dialog should auto-close after 1.5s
    await expect(dialog).not.toBeVisible({ timeout: 3000 });

    // Verify reflection was saved
    const reflectionRes = await request.get(`/api/meetings/${meeting.id}`, {
      headers: getMockAuthHeaders(testUser),
    });
    const updatedMeeting = await reflectionRes.json();
    expect(updatedMeeting.reflectionData).toBeTruthy();

    const reflection = JSON.parse(updatedMeeting.reflectionData);
    expect(reflection.selfAssessedTalkTime).toBe(50);
    expect(reflection.selfAssessedEngagement).toBe('Moderate');
    expect(reflection.selfAssessedTone).toBe('Neutral');
    expect(reflection.interruptionAwareness).toBe('Partially');
  });

  test('nav dot shows for unreflected meetings and disappears after reflection', async ({ page, request }) => {
    // Create a meeting with behavioral analysis but no reflection
    const createRes = await request.post('/api/meetings', {
      headers: getMockAuthHeaders(testUser),
      data: {
        title: 'Unreflected Meeting',
        meetingDate: new Date().toISOString(),
        transcriptContent: 'Sample transcript',
        behavioralAnalysis: JSON.stringify({
          speakingDynamics: {
            talkTimeByParticipant: [{ participant: 'User', percentage: 50, duration: '5m' }],
            interruptionPatterns: [],
            questionVsStatementRatio: { 'User': 0.5 },
          },
          sentimentTone: {
            participantSentiments: [{ participant: 'User', sentiment: 'positive', score: 0.8 }],
            toneShifts: [],
            emotionalIndicators: [],
          },
          communicationPatterns: {
            overallClarity: 0.8,
            followUpPatterns: [],
            engagementLevels: [{ participant: 'User', level: 'high', indicators: [] }],
          },
          redFlags: [],
        }),
        status: 'Ready',
      },
    });
    const meeting = await createRes.json();

    await setupAuth(page, testUser);
    await page.goto('/meetings');

    // Wait for meetings to load
    await page.waitForTimeout(1000);

    // Check for nav dot indicator on Meetings nav item
    const meetingsNavButton = page.locator('nav button').filter({ hasText: 'Meetings' });
    const navDot = meetingsNavButton.locator('span[aria-label="Unreflected meetings available"]');
    await expect(navDot).toBeVisible({ timeout: 5000 });

    // Navigate to meeting and complete quick reflect
    await page.goto(`/meetings/${meeting.id}?openQuickReflect=true`);

    // Wait for dialog
    const dialog = page.locator('.quick-reflect-dialog');
    await expect(dialog).toBeVisible({ timeout: 5000 });

    // Select emojis and save
    const emojiButtons = page.locator('.emoji-btn');
    await emojiButtons.nth(1).click();
    await emojiButtons.nth(4).click();
    await emojiButtons.nth(7).click();
    await emojiButtons.nth(10).click();

    const saveButton = page.getByRole('button', { name: 'Save' });
    await saveButton.click();

    // Wait for completion
    await expect(page.getByText('Reflected')).toBeVisible({ timeout: 5000 });
    await expect(dialog).not.toBeVisible({ timeout: 3000 });

    // Go back to meetings page
    await page.goto('/meetings');
    await page.waitForTimeout(1000);

    // Nav dot should be gone
    await expect(navDot).not.toBeVisible();
  });
});

async function setupAuth(page: Page, user: MockUser): Promise<void> {
  await page.setExtraHTTPHeaders(getMockAuthHeaders(user));
}
