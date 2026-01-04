import { test, expect, Page } from '@playwright/test';
import { resetDatabase, seedTestUser } from '../helpers/db-reset';
import { getMockAuthHeaders, MockUser } from '../helpers/mock-auth';

/**
 * UX Analysis: Tasks Page - Desktop vs Mobile Comparison
 *
 * This test suite captures screenshots and documents UX issues when comparing
 * the Task functionality between desktop and mobile viewports.
 */

let testUser: MockUser;

test.describe('Tasks UX Analysis', () => {
  test.beforeAll(async () => {
    await resetDatabase();
    testUser = await seedTestUser();
  });

  test('Desktop - Kanban layout and task interactions', async ({ page, request }) => {
    // Set desktop viewport
    await page.setViewportSize({ width: 1280, height: 720 });

    // Create test tasks
    await createTaskViaAPI(request, testUser, 'Desktop Task 1');
    await createTaskViaAPI(request, testUser, 'Desktop Task 2');

    await authenticateAndGo(page, testUser, '/tasks');

    // Screenshot: Desktop Kanban layout
    await page.screenshot({
      path: 'screenshots/ux-analysis/01-desktop-kanban-layout.png',
      fullPage: true,
    });

    // Verify three-column layout
    const todoColumn = page.locator('.bg-todo').first();
    const inProgressColumn = page.locator('.bg-inprogress').first();
    const doneColumn = page.locator('.bg-done').first();

    await expect(todoColumn).toBeVisible();
    await expect(inProgressColumn).toBeVisible();
    await expect(doneColumn).toBeVisible();

    // Screenshot: Add Task button with keyboard shortcut
    const addButton = page.getByRole('button', { name: /Add Task/i });
    await expect(addButton).toBeVisible();
    await addButton.screenshot({
      path: 'screenshots/ux-analysis/02-desktop-add-button.png',
    });

    // Hover over task card to reveal actions
    const taskCard = page.locator('.group').first();
    await taskCard.hover();
    await page.waitForTimeout(300);

    await page.screenshot({
      path: 'screenshots/ux-analysis/03-desktop-hover-actions.png',
    });

    // Verify edit/delete buttons are visible on hover
    const editButton = taskCard.getByLabel('Edit task');
    const deleteButton = taskCard.getByLabel('Delete task');
    await expect(editButton).toBeVisible();
    await expect(deleteButton).toBeVisible();

    // Open Add Task dialog
    await addButton.click();
    await page.waitForTimeout(300);
    await page.screenshot({
      path: 'screenshots/ux-analysis/04-desktop-add-dialog.png',
    });

    // Close dialog
    await page.keyboard.press('Escape');
  });

  test('Mobile - Stacked layout and missing interactions', async ({ page, request }) => {
    // Set mobile viewport (iPhone 13 dimensions)
    await page.setViewportSize({ width: 390, height: 844 });

    // Create test tasks
    await createTaskViaAPI(request, testUser, 'Mobile Task 1');
    await createTaskViaAPI(request, testUser, 'Mobile Task 2');
    await createTaskViaAPI(request, testUser, 'Mobile Task 3');

    await authenticateAndGo(page, testUser, '/tasks');

    // Screenshot: Mobile stacked layout (top)
    await page.screenshot({
      path: 'screenshots/ux-analysis/05-mobile-stacked-top.png',
    });

    // Screenshot: Mobile full page (shows all columns stacked)
    await page.screenshot({
      path: 'screenshots/ux-analysis/06-mobile-stacked-fullpage.png',
      fullPage: true,
    });

    // Screenshot: Add button still shows keyboard shortcut
    const addButton = page.getByRole('button', { name: /Add Task/i });
    await addButton.screenshot({
      path: 'screenshots/ux-analysis/07-mobile-add-button-with-kbd.png',
    });

    // Screenshot: Task card without visible actions (no hover on touch)
    const taskCard = page.locator('.group').first();
    await taskCard.screenshot({
      path: 'screenshots/ux-analysis/08-mobile-task-no-actions.png',
    });

    // Tap on task (simulating touch) - actions still won't appear
    await taskCard.tap();
    await page.waitForTimeout(300);
    await taskCard.screenshot({
      path: 'screenshots/ux-analysis/09-mobile-task-after-tap.png',
    });

    // Scroll to bottom to see Done column
    await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
    await page.waitForTimeout(300);
    await page.screenshot({
      path: 'screenshots/ux-analysis/10-mobile-scrolled-bottom.png',
    });

    // Open Add dialog - check if it fits
    await page.evaluate(() => window.scrollTo(0, 0));
    await addButton.click();
    await page.waitForTimeout(300);
    await page.screenshot({
      path: 'screenshots/ux-analysis/11-mobile-add-dialog.png',
    });
  });

  test('Generate UX Issues Report', async () => {
    const report = `
================================================================================
                    TASKS PAGE UX ANALYSIS REPORT
                       Desktop vs Mobile Comparison
================================================================================

CRITICAL ISSUES
================================================================================

ISSUE #1: Edit/Delete Actions Not Accessible on Mobile
--------------------------------------------------------------------------------
Location: task-card.component.ts lines 73-91
Current:  Actions use \`opacity-0 group-hover:opacity-100\`
Problem:  Touch devices don't trigger hover states
Impact:   Users CANNOT edit or delete tasks on mobile

Recommendations:
  A. Add always-visible action icons on mobile
  B. Implement long-press context menu
  C. Add swipe-to-reveal actions (like iOS mail)
  D. Show actions on tap (toggle visibility)

--------------------------------------------------------------------------------

ISSUE #2: Drag-and-Drop Unavailable on Touch Devices
--------------------------------------------------------------------------------
Location: tasks.page.ts (PrimeNG Draggable/Droppable)
Current:  Uses mouse events (mousedown, mousemove, mouseup)
Problem:  Touch events not handled
Impact:   Users CANNOT change task status on mobile

Recommendations:
  A. Add tap-to-change-status UI (status dropdown or buttons)
  B. Implement touch-based drag (many libraries support this)
  C. Add swipe gestures (swipe left = In Progress, right = Done)

--------------------------------------------------------------------------------

MEDIUM ISSUES
================================================================================

ISSUE #3: Stacked Columns Require Excessive Scrolling
--------------------------------------------------------------------------------
Location: tasks.page.ts line 34 (grid-cols-1 md:grid-cols-3)
Current:  All three columns stack vertically on mobile
Problem:  Must scroll through entire Todo and In Progress to see Done
Impact:   Reduced overview, harder to see task status distribution

Recommendations:
  A. Horizontal swipe carousel for columns
  B. Collapsible accordion sections
  C. Tab-based navigation (Todo | In Progress | Done tabs)
  D. Floating column selector

--------------------------------------------------------------------------------

LOW PRIORITY ISSUES
================================================================================

ISSUE #4: Keyboard Shortcut Indicator on Mobile
--------------------------------------------------------------------------------
Location: tasks.page.ts line 23
Current:  \`<kbd>⌘⇧N</kbd>\` shown on Add Task button
Problem:  Irrelevant for touch-only users
Impact:   Minor - wastes small amount of button space

Fix: Hide with \`hidden md:inline\`

--------------------------------------------------------------------------------

ISSUE #5: Fixed Dialog Width
--------------------------------------------------------------------------------
Location: tasks.page.ts line 147 (style: { width: '420px' })
Current:  Dialog has fixed 420px width
Problem:  May overflow on screens < 420px (iPhone SE = 375px)
Impact:   Minor - PrimeNG may handle this automatically

Fix: Use \`max-width: min(420px, calc(100vw - 32px))\`

--------------------------------------------------------------------------------

MISSING MOBILE FEATURES
================================================================================

1. No pull-to-refresh
2. No haptic feedback on interactions
3. No gesture-based task management
4. No quick-add via floating action button (FAB)
5. No bulk selection/actions

================================================================================
                         PRIORITY IMPLEMENTATION ORDER
================================================================================

Phase 1 - Critical (Must Fix)
-----------------------------
1. Make edit/delete accessible on mobile (tap or visible icons)
2. Add alternative to drag-drop for status changes

Phase 2 - Important
-------------------
3. Improve mobile column navigation

Phase 3 - Polish
----------------
4. Hide keyboard shortcut on mobile
5. Responsive dialog width
6. Add mobile-specific enhancements

================================================================================
                              SCREENSHOTS CAPTURED
================================================================================

Desktop:
  01-desktop-kanban-layout.png    - Full Kanban board view
  02-desktop-add-button.png       - Add Task button with kbd shortcut
  03-desktop-hover-actions.png    - Task card with hover actions visible
  04-desktop-add-dialog.png       - Add Task dialog

Mobile:
  05-mobile-stacked-top.png       - Top of stacked layout
  06-mobile-stacked-fullpage.png  - Full page showing all columns
  07-mobile-add-button-with-kbd.png - Add button (still shows ⌘⇧N)
  08-mobile-task-no-actions.png   - Task card (no visible actions)
  09-mobile-task-after-tap.png    - Task card after tap (still no actions)
  10-mobile-scrolled-bottom.png   - Bottom of page (Done column)
  11-mobile-add-dialog.png        - Add dialog on mobile

================================================================================
`;

    console.log(report);

    // Also write to a file
    const fs = await import('fs');
    fs.writeFileSync(
      'screenshots/ux-analysis/UX-ISSUES-REPORT.txt',
      report.trim()
    );
  });
});

// Helper function to authenticate and navigate
async function authenticateAndGo(
  page: Page,
  user: MockUser,
  path: string
): Promise<void> {
  // Set up request interception to add mock auth header to all API calls
  await page.route('**/api/**', async (route) => {
    const headers = {
      ...route.request().headers(),
      ...getMockAuthHeaders(user),
    };
    await route.continue({ headers });
  });

  // Navigate to the page
  await page.goto(path, { waitUntil: 'networkidle' });

  // Wait for the tasks page to load
  await page.waitForSelector('h1, .bg-todo', { timeout: 15000 });

  // Small delay for UI to stabilize
  await page.waitForTimeout(500);
}

// Helper function to create a task via API
async function createTaskViaAPI(
  request: any,
  user: MockUser,
  title: string
): Promise<void> {
  await request.post('/api/tasks', {
    headers: getMockAuthHeaders(user),
    data: { title },
  });
}
