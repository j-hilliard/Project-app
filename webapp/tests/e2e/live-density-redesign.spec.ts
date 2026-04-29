import { test, expect } from '@playwright/test';

test('Portal — 3-signal layout, no KPI strip, no error banner', async ({ page }) => {
    await page.goto('https://localhost:7210/', { waitUntil: 'networkidle' });
    await page.waitForTimeout(2500);

    await page.screenshot({ path: 'tests/e2e/screenshots/portal-signals.png' });

    // No error banner
    const errorBanner = page.locator('.p-message-error');
    await expect(errorBanner).toHaveCount(0);

    // Old KPI strip must be gone
    const kpiStrip = page.locator('.portal-kpi-strip');
    await expect(kpiStrip).toHaveCount(0);

    // App tile grid is present
    await expect(page.locator('.portal-app-grid')).toBeVisible();

    // Signals container present (contains either signals or all-clear message)
    await expect(page.locator('.portal-signals').first()).toBeVisible({ timeout: 5000 });
});

test('Scheduling Resources — compact ent-grid, no multi-line cert overflow', async ({ page }) => {
    await page.goto('https://localhost:7210/scheduling/resources', { waitUntil: 'networkidle' });
    await page.waitForTimeout(2500);

    await page.screenshot({ path: 'tests/e2e/screenshots/resources-compact.png' });

    // No error banner
    const errorBanner = page.locator('.p-message-error');
    await expect(errorBanner).toHaveCount(0);

    // DataTable rendered
    await expect(page.locator('.p-datatable')).toBeVisible();

    // Row height is compact — PrimeVue size="small" tr measures ~65px with striping;
    // verify it's under 80px (much tighter than default ~100px rows)
    const rows = page.locator('.p-datatable-tbody > tr');
    const rowCount = await rows.count();
    if (rowCount > 0) {
        const box = await rows.first().boundingBox();
        if (box) {
            expect(box.height).toBeLessThanOrEqual(80);
        }
    }
});

test('Scheduling Assignments — compact ent-grid, truncated job names', async ({ page }) => {
    await page.goto('https://localhost:7210/scheduling/assignments', { waitUntil: 'networkidle' });
    await page.waitForTimeout(2500);

    await page.screenshot({ path: 'tests/e2e/screenshots/assignments-compact.png' });

    // No error banner
    const errorBanner = page.locator('.p-message-error');
    await expect(errorBanner).toHaveCount(0);

    // DataTable rendered
    await expect(page.locator('.p-datatable')).toBeVisible();

    // Row height is compact
    const rows = page.locator('.p-datatable-tbody > tr');
    const rowCount = await rows.count();
    if (rowCount > 0) {
        const box = await rows.first().boundingBox();
        if (box) {
            expect(box.height).toBeLessThanOrEqual(80);
        }
    }
});
