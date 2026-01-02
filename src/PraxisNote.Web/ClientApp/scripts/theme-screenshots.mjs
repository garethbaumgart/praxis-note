import { chromium } from '@playwright/test';
import { mkdir } from 'fs/promises';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const screenshotsDir = join(__dirname, '../screenshots');
const mockupPath = join(__dirname, 'theme-mockup.html');

const themes = ['teal', 'violet', 'purple', 'slate', 'gray', 'emerald', 'cyan', 'amber', 'rose', 'indigo'];

async function captureThemeScreenshots() {
  await mkdir(screenshotsDir, { recursive: true });

  const browser = await chromium.launch();
  const context = await browser.newContext({
    viewport: { width: 1200, height: 700 }
  });
  const page = await context.newPage();

  // Load mockup HTML
  await page.goto(`file://${mockupPath}`);
  await page.waitForTimeout(500);

  for (const theme of themes) {
    console.log(`Capturing ${theme} theme...`);

    // Apply theme
    await page.evaluate((t) => window.applyTheme(t), theme);
    await page.waitForTimeout(200);

    // Take screenshot
    await page.screenshot({
      path: join(screenshotsDir, `done-${theme}.png`),
      fullPage: false
    });
  }

  await browser.close();

  console.log(`\nScreenshots saved to: ${screenshotsDir}`);
  console.log('\nFiles:');
  themes.forEach(t => console.log(`  - done-${t}.png`));
}

captureThemeScreenshots().catch(console.error);
