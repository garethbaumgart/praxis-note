import { chromium } from '@playwright/test';
import { mkdir } from 'fs/promises';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const screenshotsDir = join(__dirname, '../screenshots/modals');
const mockupPath = join(__dirname, 'modal-mockup.html');

const designs = ['minimal', 'compact', 'cardHeader', 'inline', 'floating', 'command'];

async function captureModalScreenshots() {
  await mkdir(screenshotsDir, { recursive: true });

  const browser = await chromium.launch();
  const context = await browser.newContext({
    viewport: { width: 800, height: 500 }
  });
  const page = await context.newPage();

  await page.goto(`file://${mockupPath}`);
  await page.waitForTimeout(500);

  for (const design of designs) {
    console.log(`Capturing ${design} design...`);

    await page.evaluate((d) => window.applyDesign(d), design);
    await page.waitForTimeout(200);

    await page.screenshot({
      path: join(screenshotsDir, `modal-${design}.png`),
      fullPage: false
    });
  }

  await browser.close();

  console.log(`\nScreenshots saved to: ${screenshotsDir}`);
  console.log('\nDesigns:');
  designs.forEach(d => console.log(`  - modal-${d}.png`));
}

captureModalScreenshots().catch(console.error);
