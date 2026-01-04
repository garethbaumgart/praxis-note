import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright config for UX Analysis tests
 *
 * Run with: npx playwright test --config=playwright.ux-analysis.config.ts
 */
export default defineConfig({
  testDir: './ux-analysis',
  fullyParallel: false, // Run sequentially for cleaner screenshots
  forbidOnly: !!process.env.CI,
  retries: 0,
  workers: 1,
  reporter: [['html', { open: 'always' }], ['list']],

  globalSetup: './global-setup.ts',
  globalTeardown: './global-teardown.ts',

  use: {
    baseURL: process.env.BASE_URL || 'http://localhost:5002',
    trace: 'on',
    screenshot: 'on',
    video: 'on',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  webServer: process.env.CI
    ? undefined
    : {
        command: 'dotnet run --project ../../src/PraxisNote.Web --no-build',
        url: 'http://localhost:5002/api/health',
        reuseExistingServer: !process.env.CI,
        timeout: 120000,
        env: {
          ASPNETCORE_ENVIRONMENT: 'E2E',
          ASPNETCORE_URLS: 'http://localhost:5002',
          ConnectionStrings__DefaultConnection:
            'Host=localhost;Port=5433;Database=praxisnote_e2e;Username=praxisnote;Password=testpassword',
        },
      },
});
