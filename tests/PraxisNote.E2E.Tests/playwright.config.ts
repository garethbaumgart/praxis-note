import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './smoke-tests',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: process.env.CI ? [['github'], ['html', { open: 'never' }]] : 'html',

  globalSetup: './global-setup.ts',
  globalTeardown: './global-teardown.ts',

  use: {
    baseURL: 'http://localhost:5002',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
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
        reuseExistingServer: false,
        timeout: 120000,
        env: {
          ASPNETCORE_ENVIRONMENT: 'Development',
          ASPNETCORE_URLS: 'http://localhost:5002',
          ConnectionStrings__DefaultConnection:
            'Host=localhost;Port=5433;Database=praxisnote_e2e;Username=praxisnote;Password=e2eTestPassword',
        },
      },
});
