import { test, expect } from '@playwright/test';

const evidenceRoot = '../docs/qa-evidence/QA_AUDIT_20260429_PLATFORM_SWEEP';

const routes = [
  { name: 'portal', path: '/portal', mustHave: /Estimating|Planning|Scheduling|Projects at Risk/i },
  { name: 'estimating-estimates', path: '/estimating/estimates', mustHave: /Quote Log|New Estimate/i },
  { name: 'planning-step-out-plans', path: '/planning/step-out-plans', mustHave: /Step-Out|Plan/i },
  { name: 'planning-work-packages', path: '/planning/work-packages', mustHave: /Work Package|Handoff/i },
  { name: 'planning-fco', path: '/planning/fco', mustHave: /FCO|Change/i },
  { name: 'scheduling-dashboard', path: '/scheduling/dashboard', mustHave: /Scheduling|Coverage|Shortage|Demand/i },
  { name: 'scheduling-jobs', path: '/scheduling/jobs', mustHave: /Jobs|Work|Demand/i },
  { name: 'scheduling-resources', path: '/scheduling/resources', mustHave: /Resources|People|Certification/i },
  { name: 'scheduling-assignments', path: '/scheduling/assignments', mustHave: /Assignments|Conflict/i },
  { name: 'scheduling-coverage', path: '/scheduling/coverage', mustHave: /Coverage|Craft|Gap|Shortage/i },
  { name: 'scheduling-rolloff', path: '/scheduling/roll-off', mustHave: /Ending|Roll|Available|Free/i },
];

test.describe('Platform route screenshot sweep', () => {
  for (const route of routes) {
    test(`${route.name} loads without error banner`, async ({ page }) => {
      const failedApiRequests: string[] = [];
      page.on('requestfailed', (request) => {
        const url = request.url();
        if (url.includes('localhost:7211') || url.includes('localhost:5047')) {
          failedApiRequests.push(`${url} - ${request.failure()?.errorText ?? 'unknown failure'}`);
        }
      });

      await page.goto(route.path, { waitUntil: 'domcontentloaded' });
      await page.waitForLoadState('networkidle', { timeout: 15_000 }).catch(() => {});
      await page.waitForTimeout(1_500);

      await expect(page.getByText(route.mustHave).first()).toBeVisible({ timeout: 15_000 });
      await expect(page.locator('.p-message-error, [class*="p-message-error"]')).toHaveCount(0, {
        message: `Error banner visible or API failed: ${failedApiRequests.join(', ')}`,
      });

      await page.screenshot({
        path: `${evidenceRoot}/${route.name}.png`,
        fullPage: true,
      });
    });
  }
});
