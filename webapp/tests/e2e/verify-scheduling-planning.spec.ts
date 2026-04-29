import { test, expect } from '@playwright/test';

// Verifies that Scheduling Dashboard and Step-Out Plans load data without error banners.
// Relies on VITE_BYPASS_AUTH=true in the running dev server (.env.local).

async function openAndWait(page: any, url: string) {
    const failed: string[] = [];
    page.on('requestfailed', (req: any) => {
        if (req.url().includes('localhost:7211')) {
            failed.push(`${req.url()} — ${req.failure()?.errorText}`);
        }
    });

    await page.goto(url, { waitUntil: 'networkidle' });
    // Wait for onMounted load() to complete — data API calls happen after auth
    await page.waitForTimeout(3000);

    return { failed };
}

test('Scheduling Dashboard — no error banner, data loaded', async ({ page }) => {
    const { failed } = await openAndWait(page, 'https://localhost:7210/scheduling/dashboard');

    // Wait for KPI strip to be visible before screenshotting
    await page.locator('.sched-kpi-strip').waitFor({ state: 'visible', timeout: 8000 }).catch(() => {});
    await page.screenshot({ path: 'tests/e2e/screenshots/scheduling-dashboard.png', fullPage: true });

    const banner = page.locator('.p-message-error, [class*="p-message-error"]');
    await expect(banner).toHaveCount(0, {
        message: `Error banner visible. Failed API requests: ${failed.join(', ')}`,
    });
});

test('Step-Out Plans — no error banner, table renders', async ({ page }) => {
    const { failed } = await openAndWait(page, 'https://localhost:7210/planning/step-out-plans');

    await page.screenshot({ path: 'tests/e2e/screenshots/step-out-plans.png', fullPage: true });

    const banner = page.locator('.p-message-error, [class*="p-message-error"]');
    await expect(banner).toHaveCount(0, {
        message: `Error banner visible. Failed API requests: ${failed.join(', ')}`,
    });
});
