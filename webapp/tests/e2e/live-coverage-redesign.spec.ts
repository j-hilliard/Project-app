import { test, expect } from '@playwright/test';

test('Craft Coverage — redesigned dashboard loads, no error banner', async ({ page }) => {
    const failed: string[] = [];
    page.on('requestfailed', req => {
        if (req.url().includes('localhost:7211')) {
            failed.push(`${req.url()} — ${req.failure()?.errorText}`);
        }
    });

    await page.goto('https://localhost:7210/scheduling/coverage', { waitUntil: 'networkidle' });
    await page.waitForTimeout(3000);

    await page.screenshot({
        path: 'tests/e2e/screenshots/coverage-redesign.png',
    });

    // No error banner
    const banner = page.locator('.p-message-error, [class*="p-message-error"]');
    await expect(banner).toHaveCount(0, {
        message: `Error banner visible. Failed requests: ${failed.join(', ')}`,
    });

    // KPI strip present
    await expect(page.locator('.kpi-strip')).toBeVisible({ timeout: 6000 });

    // Filter bar present
    await expect(page.locator('.filter-bar')).toBeVisible();
});

test('Craft Coverage — bar rows render and double-click opens drawer', async ({ page }) => {
    await page.goto('https://localhost:7210/scheduling/coverage', { waitUntil: 'networkidle' });
    await page.waitForTimeout(3000);

    const rows = page.locator('.craft-bar-row');
    const count = await rows.count();

    if (count === 0) {
        // No data — verify empty state is visible instead of an error
        await expect(page.locator('.empty-state')).toBeVisible();
        await page.screenshot({ path: 'tests/e2e/screenshots/coverage-empty-state.png' });
        return;
    }

    // First row is visible and has a craft-code badge
    await expect(rows.first().locator('.craft-code')).toBeVisible();
    await expect(rows.first().locator('.bar-track')).toBeVisible();

    // Double-click opens the detail drawer
    await rows.first().dblclick();
    await page.waitForTimeout(800);

    const sidebar = page.locator('.p-sidebar');
    await expect(sidebar).toBeVisible({ timeout: 4000 });

    // Drawer shows the drawer KPI section
    await expect(page.locator('.drawer-kpis')).toBeVisible({ timeout: 3000 });

    await page.screenshot({
        path: 'tests/e2e/screenshots/coverage-drawer-open.png',
    });

    // Close drawer
    const closeBtn = page.locator('.p-sidebar-close');
    if (await closeBtn.isVisible()) await closeBtn.click();
});
