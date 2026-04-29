# QA Regression Checklist
# Read this entire file before testing. Verify ALL items in your lane.
# For every item, produce: PASS | FAIL | NOT VERIFIED
# Never skip an item silently.

Any subagent report that does not include an explicit checklist verdict table is incomplete and should be rejected by the orchestrator.
For UI/reporting defects, include concrete evidence: screenshot path, visible values, selected filters, and what changed after the click/filter action.
Every subagent must also read `docs/LIVE_QA_TODO.md` and report OPEN/DONE/BLOCKED/REOPENED/ACCEPTED AS-IS for relevant live TODO items.
If a live TODO checkbox is marked complete but verification fails, the subagent must report it as REOPENED with evidence.
If Joseph has accepted a behavior as-is in `docs/LIVE_QA_TODO.md`, do not fail that behavior again unless the implementation changed or new evidence creates a new risk. Report it as ACCEPTED AS-IS with the linked TODO evidence.
Only Joseph or Codex may close live TODO items. If Claude checked an item complete without Joseph/Codex evidence, report it as REOPENED.
Portal / Planning / Scheduling implementation must also be checked against `docs/PLATFORM_EXPANSION_MASTER_CONTRACT.md`.

---

## Domain Lanes and ID Prefixes

| Domain | Prefix | Who verifies |
|--------|--------|-------------|
| Labor Grid / OT Calculator | `QA-LG` | Estimating Workflow Auditor |
| Estimates | `QA-EST` | Estimating Workflow Auditor |
| Staffing Plans | `QA-SP` | Staffing & Forecast Auditor |
| Analytics / Revenue Forecast | `QA-AN` | Staffing & Forecast Auditor |
| Rate Book / Cost Book | `QA-RCB` | Estimating Workflow Auditor |
| UI / Layout / Controls | `QA-UI` | UI/UX Auditor |
| AI Demo Readiness | `QA-AI` | AI Demo Auditor |
| Demo Data Readiness | `QA-DATA` | AI Demo Auditor + Staffing & Forecast Auditor |
| Platform Shell / App Registry | `QA-PLAT` | Platform QA Auditor |
| Database Bootstrap / Seed | `QA-BOOT` | Backend QA Auditor |
| Planning / PM | `QA-PLAN` | Planning QA Auditor |
| Scheduling | `QA-SCHED` | Scheduling QA Auditor |
| Actuals / Delta | `QA-ACT` | Planning + Cost QA Auditor |
| FCO / Change Order | `QA-FCO` | Planning + Document QA Auditor |
| Project Lifecycle / Stage Gates | `QA-LIFE` | Platform Lifecycle Auditor |
| Architecture Guardrails | `QA-ARCH` | Backend + Frontend Architecture Auditor |

---

## QA-ARCH Lane — Architecture Guardrail Checks

> Run `bash tools/architecture-checks/run-all-checks.sh` from the repo root.
> Include the full output and exit code in every audit evidence packet.
> Backend checks hard-fail. Frontend checks may be warn-mode only while the targeted cleanup batch is actively in progress.
> Use strict frontend mode once Batches 8-9 land. Permanent warn mode is a failure.

### QA-ARCH-001: AppDbContext must not contain inline entity configuration
- **Rule:** ARCH-BE-002 + ARCH-BE-007 + ARCH-BE-008
- **Verification:** `check-backend.sh` ARCH-BE-002 and ARCH-BE-008 both PASS
- **Regression trigger:** Any `modelBuilder.Entity<T>` block added back to `Data/AppDbContext.cs`
- **Fixed in:** Batch 1 (2026-04-29)

### QA-ARCH-002: Cleaned controllers must not accept raw EF entities as [FromBody]
- **Rule:** ARCH-BE-001
- **Applies to:** ProjectController, WorkOrderController, PlanningController, SchedulingController
- **Verification:** `check-backend.sh` ARCH-BE-001 PASS
- **Regression trigger:** Any new endpoint in these controllers binds a raw EF entity type
- **Fixed in:** Batch 1 (2026-04-29)

### QA-ARCH-003: Required service files must exist
- **Rule:** ARCH-BE-005 + ARCH-BE-006
- **Verification:** `check-backend.sh` ARCH-BE-005 both PASS
- **Regression trigger:** `FcoDocumentService.cs` or `WorkOrderFinancialService.cs` deleted or moved
- **Fixed in:** Batch 1 (2026-04-29)

### QA-ARCH-004: StatusUpdateRequest must not be redefined in controllers
- **Rule:** ARCH-BE-003
- **Verification:** `check-backend.sh` ARCH-BE-003 PASS
- **Regression trigger:** Any controller adds `record StatusUpdateRequest` inline
- **Fixed in:** Batch 1 (2026-04-29)

### QA-ARCH-005: PM/Scheduling views must not import useApiStore directly (WARN mode — pending Batch 3-4)
- **Rule:** ARCH-FE-001
- **Current state:** 16 violations — warn-only until views are refactored
- **Verification after Batch 4:** `check-frontend.sh --strict` ARCH-FE-001 PASS
- **Escalate to fail:** Add `--strict-frontend` to `run-all-checks.sh` after Batch 4

### QA-ARCH-006: PM/Scheduling views must not call apiStore.api directly (WARN mode — pending Batch 3-4)
- **Rule:** ARCH-FE-002
- **Current state:** 53 violations — warn-only until views are refactored

### QA-ARCH-007: PM/Scheduling views must not define formatters locally (WARN mode — pending Batch 2-4)
- **Rules:** ARCH-FE-003, ARCH-FE-004, ARCH-FE-005, ARCH-FE-006
- **Current state:** 15+4+10+2 violations — warn-only until design system and service layer land

### QA-ARCH-008: run-all-checks.sh must exit 0 on every audit
- **Verification:** Run `bash tools/architecture-checks/run-all-checks.sh`; confirm final line is `OVERALL RESULT: PASSED`
- **This is the single gate** — if it fails, all other ARCH items are suspect

---

### 2026-04-29 Update: no permanent warn mode
- **Joseph decision:** Batches 1-5 are partial progress, not final cleanup completion.
- **Rule:** Any warn-mode architecture check is temporary only while its targeted cleanup batch is actively in progress.
- **Batch mapping:** frontend direct API/helper warnings must become hard-fail after PM/Scheduling view refactor Batches 8-9; DevController ownership must become hard-fail after Batch 6; final cleanup cannot pass while known violations are hidden as warnings.

### QA-ARCH-012: DevController must stay thin after Batch 6
- **Rule:** Dev/seed/bootstrap orchestration belongs in services/orchestrators, not controller monoliths
- **Verification after Batch 6:** `DevController` exposes narrow endpoints only; architecture check hard-fails oversized or seed-heavy controller drift
- **Regression trigger:** seed/reset orchestration is added back to `Api/Controllers/DevController.cs`

### QA-ARCH-013: PM/Scheduling service layer must be feature/domain split
- **Rule:** giant `usePlanningService` / `useSchedulingService` wrappers are not enough
- **Verification after Batches 8-9:** feature services exist for project, work order, work package, FCO, resources, assignments, coverage, and demand where applicable
- **Regression trigger:** views depend on one large module service wrapper for unrelated feature domains

### QA-ARCH-014: Warn-mode checks must become hard-fail when their batch is complete
- **Rule:** guardrails are enforcement, not permanent suggestions
- **Verification:** after Batches 8-10, architecture checks fail the build/audit for direct API view access, duplicate helpers, shared UI bypass, raw EF request bodies, inline DbContext config, and DevController monolith drift
- **Regression trigger:** cleanup branch passes while known forbidden patterns remain as warnings

### QA-ARCH-015: Cleanup branch cannot be accepted until updated definition of done is met
- **Rule:** Batches 1-5 are partial progress, not final cleanup completion
- **Verification:** `DevController` thin, bootstrap verified, shared UI system exists and is used, feature services are split, architecture scripts/docs are updated, builds/tests/route smoke/QA checks pass, and branch is pushed for review
- **Regression trigger:** implementation worklog or final response claims cleanup complete before all definition-of-done items are evidenced

---

## Number Reconciliation Rule (ALL lanes)

> Do not just inspect charts visually. Reconcile the numbers.
> If any KPI card shows $0 while the chart shows nonzero values for the same filter and time period, report a **High** severity reporting defect.
> If any filter control (year, date range, status, branch) changes one part of the screen but not another, report a **High** severity filter defect.

---

## Labor Grid / OT Calculator

### QA-LG-001: DT Weekends Billing Logic
- **Past failure:** Sunday was always charged DT regardless of the DT Weekends setting (hardcoded `if (dow === 0) return DT`). With "No DT Weekends" selected, Sundays still showed double-time — billing dispute risk.
- **Fixed:** `dtWeekends` changed from boolean to 3-option string: `'none'` / `'sun_only'` / `'sat_sun'`. Logic now checks the string value.
- **Verify:**
  - Open an estimate. Set DT W/E = "No DT Weekends" → Sunday cells must show OT hours, NOT DT hours.
  - Set DT W/E = "Sundays Only" → Sundays show DT, Saturdays show OT.
  - Set DT W/E = "Sat & Sun" → Both Saturday and Sunday show DT.
  - The dropdown must show exactly 3 options: "No DT Weekends", "Sundays Only", "Sat & Sun".

### QA-LG-002: Weekly View Gap — Partial Last Week
- **Past failure:** When a week tab had fewer than 7 job days (partial last week), only the job days were rendered as columns. The sticky left/right panels created a large visual gap.
- **Fixed:** Always renders a full Mon–Sun 7-column week. Out-of-range days are grayed out and disabled.
- **Verify:**
  - Open an estimate with a partial last week (job ends mid-week).
  - Click that week tab — must show 7 day columns (Mon–Sun).
  - Days outside the job date range must be visually dimmed and inputs must be disabled (not editable).
  - No large visual gap between the left sticky panel and right totals panel.

### QA-LG-003: Sticky TOTAL Column Remains Visible And Aligned
- **Past failure:** Horizontal scrolling in the labor grid could make right-side totals hard to see, clipped, or misaligned from rows.
- **Correct behavior:** The TOTAL column remains sticky on the right and stays aligned with each row and footer total.
- **Verify:**
  - Open an estimate with multiple labor rows and enough days to require horizontal scrolling.
  - Scroll left, middle, and far right. TOTAL column must remain visible and aligned.
  - Repeat on a staffing plan labor grid.
  - TOTAL must not overlap rate columns or row cells.
  - Row trash/delete action must still exist, but should stay hidden until that row is hovered.
  - Capture screenshots at each scroll position.

---

## Estimates

### QA-EST-001: ConfirmDialog Not Resolving on Delete
- **Past failure:** Delete buttons used `ConfirmDialog` in templates but it was never imported. Vue warned "Failed to resolve component: ConfirmDialog" and deletes silently failed.
- **Fixed:** Explicit `import ConfirmDialog from 'primevue/confirmdialog'` added to each affected view.
- **Verify:**
  - Attempt to delete an estimate from the list view.
  - A confirmation dialog must appear before deletion proceeds.
  - No Vue console warning about unresolved ConfirmDialog.

### QA-EST-002: Submitted Status Missing from Filter and Badge Map
- **Past failure:** Estimate status filter dropdown only included: Draft, Pending, Awarded, Lost, Canceled. "Submitted" was missing. Estimates in Submitted status rendered with no badge color and could not be filtered.
- **Fixed:** "Submitted for Approval" added to `statusOptions` and `'Submitted': 'info'` added to `statusSeverity` map.
- **Verify:**
  - Estimate list filter dropdown must include "Submitted for Approval" as an option.
  - An estimate in Submitted status must show a colored (info/blue) badge — not blank/unstyled.

### QA-EST-003: PDF/Email Bid Submission Workflow
- **Past failure:** Submit flow only offered "Export PDF." No email-to-customer or formal approval submission.
- **Verify:**
  - On a Draft estimate, clicking "Submit" must open a dialog to confirm customer email.
  - After confirming, estimate status must change to "Submitted for Approval."
  - The PDF generated must exclude internal cost, cost rates, and gross margin fields.

---

## Staffing Plans

### QA-SP-001: Staffing Plans Must Not Be Header-Only
- **Past failure:** Test agents created staffing plans with header data only and `Rough Labor: $0`. Downstream manpower forecast showed zeros for those plans.
- **Correct behavior:** Every meaningful staffing plan must include scheduled labor rows.
- **Verify:**
  - A staffing plan with labor rows must show a non-zero Rough Labor Total on the plan card.
  - Labor preview pills must appear on the plan card.
  - Manpower Forecast must count workers from those labor rows.
  - When converted to an estimate, labor rows must carry forward.

### QA-SP-002: Load Crew Dead Button in Staffing Form
- **Past failure:** LaborGrid emits `loadCrew` but StaffingFormView had no `@loadCrew` listener. Clicking "Load Crew" did nothing.
- **Fixed:** "Load Crew" button hidden in staffing context (`:showLoadCrew="false"` prop).
- **Verify:**
  - Open a Staffing Plan's labor grid.
  - "Load Crew" button must NOT be visible.
  - Estimate labor grid may still show it (if wired) or also hide it — confirm no dead buttons exist.

---

## Analytics / Revenue Forecast

### QA-AN-001: Duplicate Year Dropdown
- **Past failure:** Two `selectedYear` Dropdown components rendered simultaneously — one in the toolbar and one inside the FINANCIAL DASHBOARD section header. Inner one truncated to "2...".
- **Fixed:** Inner dropdown removed.
- **Verify:**
  - Open Revenue Forecast.
  - Exactly ONE year dropdown must be visible (toolbar only).
  - No year selector must appear inside any section header or card.

### QA-AN-002: AVG MARGIN = 0% / FUTURE PROFIT = $0 — Wrong API URL
- **Past failure:** `loadData()` called `/api/v1/analytics` which does not exist. Correct route is `/api/v1/analytics/dashboard`. Silent `.catch` returned null so `analyticsAvgMargin` stayed 0.
- **Fixed:** API URL corrected to `/api/v1/analytics/dashboard`.
- **Verify:**
  - Open Revenue Forecast with year = 2026 (the seeded data year).
  - AVG MARGIN must NOT be 0% — it must reflect a real margin from awarded estimates.
  - FUTURE PROFIT must NOT be $0 — it must show a projected value.
  - If both remain $0, check network tab for 404 on the analytics endpoint.

### QA-AN-003: Dead View Tabs
- **Past failure:** "All Jobs" and "Compare" tabs set `activeView` but no template content was conditionally rendered on it. Clicking tabs did nothing visible.
- **Fixed:** Tabs removed or content wired.
- **Verify:**
  - Every visible tab in Revenue Forecast must show distinct content when clicked.
  - No tab may exist that does nothing when clicked.

### QA-AN-004: Ghost Filter Controls
- **Past failure:** `selectedBranch` and `selectedCustomer` dropdowns were bound to refs never consumed in any computed or filter logic. Changing them had zero effect.
- **Fixed:** Controls removed.
- **Verify:**
  - No Branch or Customer filter dropdown must appear in Revenue Forecast unless it demonstrably filters the visible data when changed.

### QA-AN-005: Manpower Forecast Opens Empty
- **Past failure:** All KPI cards showed "—" on first load. User had to manually click "Run Forecast" before any data appeared.
- **Fixed:** Forecast auto-runs on mount with default date range.
- **Verify:**
  - Open Manpower Forecast page.
  - KPI cards and chart must be populated immediately — no manual action required.
  - No "failed to load manpower data" error must appear.

### QA-AN-006: Revenue Chart Ignores Year Selector
- **Past failure:** `allRevenueRows` computed in RevenueForecastView hardcodes `currentYear` (line ~348) instead of `selectedYear`. Changing the year dropdown updates KPI cards but leaves the Revenue vs Cost Analysis chart unchanged — chart still shows the current year's data regardless of selection.
- **Number reconciliation rule:** Do not just read the code or inspect the chart visually. Change the year dropdown and verify BOTH the KPI cards AND the chart update to match the new year. If the KPI cards show $0 for a year with no data but the chart still shows bars — that is a **High** severity defect.
- **Verify:**
  - Open Revenue Forecast. Confirm year = 2026 → KPI cards show real values AND chart shows Jan–Dec 2026 bars.
  - Change year to 2025 → KPI cards must go to $0 AND chart x-axis must also shift to 2025 (or show empty bars for 2025).
  - Change back to 2026 → both must repopulate.
  - If KPIs and chart are out of sync for ANY year selection → FAIL.

### QA-AN-007: Dashboard KPI Cards and Chart Must Reconcile on Every Filter Change
- **Past failure:** Financial Dashboard KPI cards showed $0 for Awarded/Pending/Total Pipeline while the Revenue vs Cost Analysis chart still displayed nonzero revenue lines for the same year/YTD filter. KPIs and chart were driven by different data paths and fell out of sync.
- **Number reconciliation rule:** Do not rely on visual inspection alone. For each filter state, confirm that KPI card values and chart values are consistent with each other.
- **Verify every run:**
  - Select each time tab in sequence: MTD → QTD → YTD → ALL.
  - Change year dropdown (e.g., 2026 → 2025 → back to 2026).
  - After each change, confirm KPI cards AND chart both update together.
  - If Awarded Revenue KPI = $0, the Awarded Revenue chart line must also be $0 (flat) for that period.
  - If Pending Pipeline KPI = $0, the Pipeline chart line must also be flat.
  - If Lost Revenue KPI = $0, the Lost Revenue chart line must also be flat.
  - Future Revenue card may legitimately differ from chart (it covers all future periods, not just the selected filter) — but this must be clearly labeled on screen. If it is NOT labeled as filter-independent, report it.
- **Evidence required:** Screenshot before filter change, screenshot after filter change. Both must be included in the report.
- **Severity if failed:** High.

---

## Rate Book / Cost Book

### QA-RCB-001: Import/Export Stub Buttons on Rate Book Page
- **Past failure:** "Import" and "Export" buttons showed "not yet implemented" toasts.
- **Fixed:** Both buttons removed.
- **Verify:**
  - Open Rate Books page.
  - No Import or Export button must be visible unless it is fully functional.

### QA-RCB-002: Equipment Math Ignores Rate Type
- **Past failure:** Equipment rows with rate type "hourly", "weekly", or "monthly" all calculated as `rate × qty × days` regardless of type. A monthly-rate item on a 30-day job was billed as if it were a daily rate ($30,000 instead of $1,000).
- **Verify:**
  - Add an equipment row with type "Monthly", rate $1,000, qty 1, on a 30-day job.
  - Subtotal must be $1,000 (1 month) — NOT $30,000.
  - Verify "Weekly" and "Hourly" types also calculate correctly for their respective units.

### QA-RCB-003: Cost Book Only Covers Labor — Internal Cost Understated
- **Past failure:** Job Cost Analysis only applied cost book rates to labor rows. Equipment and expense rows had no internal cost applied, causing gross margin to be overstated for any estimate with equipment or expenses.
- **Verify:**
  - Create an estimate with labor + equipment + expenses.
  - Internal cost must reflect all three line item types — not just labor.
  - Gross margin = (Total Bill − Total Internal Cost) / Total Bill. Must not be 100% when equipment/expenses are present.

---

## UI / Layout / Controls

### QA-UI-001: Export PNG Stub Button — Manpower Forecast
- **Past failure:** "Export PNG" button showed "PNG export coming soon" toast. Visible, enabled, but fake.
- **Fixed:** Button removed.
- **Verify:**
  - Open Manpower Forecast.
  - No "Export PNG" button must be visible unless it is fully functional.

### QA-UI-002: Sidebar/Content Overlap at 1280px Viewport
- **Past failure:** At 1280px screen width, the left navigation sidebar overlapped the main page content, covering the page title and top portion of estimate forms.
- **Verify:**
  - Set browser viewport to exactly 1280px wide.
  - Open an estimate form.
  - Page title and all form content must be fully visible — no sidebar overlap.

---

## UI / Layout / Controls (continued)

### QA-UI-003: Dev Reset/Seed Buttons Must Not Be Visible in Demo
- **Past failure:** `EstimateListView.vue` exposes "Reset" (red, danger) and "Seed Dev Data" buttons guarded only by `import.meta.env.DEV`. In `npm run dev` mode (the typical local workflow), `DEV === true` and both buttons are fully visible — including the red "Reset — Delete ALL CSL data (dev only)" tooltip.
- **Correct behavior:** Dev controls must never be visible during a demo or in production.
- **Verify:**
  - Open the Estimates list page.
  - If any "Reset" or "Seed Dev Data" button is visible, report as **P0 — Demo Killer**.
  - Confirm the demo is served from a production build (`npm run build && npm run preview`) or that a secondary guard (hostname / explicit env var) prevents the buttons from rendering.

### QA-UI-004: confusing Save Snapshot vs View History Button Labels
- **Past failure:** Both "Save + Revision" and "Revisions" buttons are shown simultaneously in the estimate form toolbar when editing an existing estimate. To a non-technical observer the labels are nearly identical.
- **Verify:** Button labels in the estimate form toolbar are clearly distinct and self-explanatory (e.g., "Save Snapshot" and "View History" rather than "Save + Revision" / "Revisions").

### QA-UI-005: Report Toolbar Controls Must Not Clip or Overlap
- **Past failure:** Revenue Forecast toolbar controls at the top-right, especially the year dropdown, rendered clipped against the page edge/container while export buttons and refresh controls were visible.
- **Verify:**
  - Open Revenue Forecast at desktop widths used for demo, including 1280px and 1600px.
  - Export PDF, Export CSV, refresh, and year dropdown controls must be fully visible.
  - The selected year text and dropdown chevron must not be clipped or overlapped.
  - Changing the year must leave the toolbar stable; no jump, crop, or hidden menu.

### QA-UI-006: List/Card Workflow Actions Are Discoverable And Not Clipped
- **Boss-demo requirement:** Estimate row workflow actions must be available through a clean three-dot menu. Staffing plan cards may use hover-revealed action buttons; Joseph accepted this behavior on 2026-04-26.
- **Verify:**
  - Open Estimate List and Staffing Plan List.
  - Confirm row/card actions are not visually cluttering every row before hover.
  - Estimate List: hover/open the three-dot menu. Menu must not be clipped by table/scroll containers and must include fallback `Open`.
  - Staffing Plan List: hover a staffing card and verify `Open`/workflow action buttons become visible.
  - Click `Open` or equivalent and verify the correct record opens.

### QA-UI-007: Crew Template Dialog Has Empty State And Usable Template State
- **Boss-demo requirement:** Crew template loading must look intentional whether templates exist or not.
- **Verify:**
  - Open crew template dialog from an estimate and a staffing plan.
  - If templates exist, at least 3 realistic templates should be visible in demo data.
  - If no templates exist in a non-demo environment, dialog must show a helpful empty state, not a blank panel.
  - Selecting a template must be possible from the populated state.

### QA-UI-008: Light Mode Must Be Audited On Every Primary Page
- **Past failure:** QA only audited dark mode. Clicking the visible theme toggle exposed light-mode contrast defects: white page titles on pale backgrounds, disabled buttons barely visible, low-contrast links/buttons, and washed-out form/header controls.
- **Correct behavior:** Light mode is a supported visible app state because the theme toggle is available in the top bar. Every primary page must be readable and usable in light mode.
- **Verify every run:**
  - Click the top-bar theme toggle to switch to light mode.
  - Visit every primary demo page: Estimates list, New Estimate, Staffing Plans list, Staffing Plan form, Rate Books, Cost Book, Crew Templates, Revenue Forecast, Manpower Forecast, Calendar/Reports if used in demo, and AI chat/sidebar.
  - On each page, verify page title, subtitle, form labels, placeholders, buttons, disabled buttons, links, empty states, table headers, status badges, card text, and modal text are clearly readable.
  - Capture screenshots for every page tested in light mode.
  - Switch back to dark mode and confirm the same pages still look correct.
- **Evidence required:** Screenshot path for each page in light mode and at least one matching dark-mode control screenshot.
- **Severity if failed:** High; P0 if the page is in the demo path or the primary action button is hard to see.

### QA-UI-009: Theme Toggle Is A Full-App Regression Trigger
- **Past failure:** Agents treated the theme toggle as a cosmetic control and did not use it during full audits.
- **Correct behavior:** Clicking theme toggle starts a full UI regression pass in the selected mode.
- **Verify every run:**
  - Theme toggle must be clicked during UI audits.
  - The auditor must explicitly report PASS/FAIL for dark mode and light mode.
  - Any report that says "all pages/buttons tested" but does not include light-mode screenshots is incomplete.
  - Any hardcoded dark-theme utility classes (`text-white`, `text-slate-*`, dark-only backgrounds) must be checked visually in light mode.

---

## Staffing Plans (continued)

### QA-SP-003: Re-apply Rates Button Wired in StaffingFormView
- **Past failure:** `LaborGrid` emits `applyRates` when "Re-apply Rates" is clicked. `StaffingFormView` had no `@applyRates` handler — button emitted into a void.
- **Fixed:** `@applyRates="store.applyRateBookToRows()"` added to `LaborGrid` in `StaffingFormView.vue`.
- **Verify:**
  - Open a Staffing Plan that has a rate book loaded.
  - Click "Re-apply Rates."
  - Labor row rates must update. If nothing visibly changes (and rates were modified manually), the button is still dead — FAIL.

### QA-SP-004: Submitted for Approval Estimates Must Appear in Manpower Forecast
- **Past failure:** `shouldIncludeSource` in `forecast.ts` only passes `Awarded`, `Draft`, `Pending`, `Proposed`. An estimate with status `"Submitted for Approval"` fell through to `return false` and was silently excluded from all headcount counts.
- **Verify:**
  - Create or find an estimate in "Submitted for Approval" status with labor rows and dates in the forecast range.
  - Run Manpower Forecast — the estimate's headcount must appear in the counts.
  - If the estimate is invisible to the forecast, FAIL.

### QA-SP-005: Agent-Created Staffing Plans Must Include Labor Rows
- **Past failure:** Automated/manual test agents created staffing plans with only header fields, then reported staffing coverage as tested even though the plan card showed `Rough Labor: $0` and no usable demand entered the forecast.
- **Correct behavior:** A staffing-plan test is not valid unless it adds at least one scheduled labor row with craft/position, shift, date range, quantity, and rate/cost values where applicable.
- **Verify every run:**
  - Any `QA_TEST_<timestamp>` staffing plan created by an agent must show labor preview pills on the card.
  - `Rough Labor` must be greater than `$0` unless the explicit scenario is "empty staffing plan validation."
  - Opening the plan must show the same labor rows that appear on the card.
  - Manpower Forecast must count those rows in the correct dates.
  - If an agent creates only a header and calls the staffing workflow tested, mark the agent report incomplete.

---

## Estimates (continued)

### QA-EST-004: Submitted for Approval Consistency Across List, Badge, API, and Header
- **Past failure:** Status was treated as a manually editable form field and the vocabulary was inconsistent across UI, API, AI, and reports.
- **Correct behavior:** Header status is read-only. The estimate list may filter by Submitted for Approval, and all backend/frontend status values must reconcile.
- **Verify:**
  - Open an estimate form. Status must appear as a read-only badge; no editable status dropdown may be present.
  - Set status to "Submitted for Approval" and save. In the estimate list, the status badge must show blue/info — not blank.
  - The list filter dropdown must also include "Submitted for Approval" so estimates in that status can be found.
  - New lifecycle rule: the estimate header must show status as a read-only badge only. If the form header exposes an editable status dropdown, FAIL even if the list filter works.
  - Backend status patching must accept "Submitted for Approval" or have an explicit tested mapping to the backend status value.

### QA-EST-005: Save Automatically Creates or Keeps Draft
- **Past failure:** Users had to manually select Draft from a status dropdown. Status behaved like a database field instead of a workflow result.
- **Correct behavior:** Saving a new or unsent estimate automatically creates/keeps the estimate in Draft.
- **Verify:**
  - Create a new estimate and save without touching any status control.
  - Header badge must show Draft after save.
  - Estimate list row must show Draft.
  - If a status dropdown is required to make this happen, FAIL.

### QA-EST-006: Estimate Header Status Must Be Read-Only
- **Past failure:** `EstimateHeader.vue` rendered an editable STATUS dropdown, letting users manually jump statuses.
- **Correct behavior:** The header may display status as a badge, but must not allow status editing.
- **Verify:**
  - Open new and existing estimate forms.
  - No editable STATUS dropdown must appear in the header.
  - Status must be visible as a badge or equivalent read-only display.
  - Lost reason may be displayed for lost estimates, but status itself must not be changed from the header.

### QA-EST-007: Submit for Approval Is a Dedicated Action
- **Past failure:** Submission was represented as manually selecting a status value or exporting a PDF only.
- **Correct behavior:** A Draft estimate has a clear "Submit for Approval" action in the form toolbar/header.
- **Verify:**
  - Open a saved Draft estimate.
  - "Submit for Approval" must be visible as an action button.
  - The button must not appear as a status dropdown option.
  - The button should be unavailable for unsaved new estimates until an estimate id exists, or it should save first as part of the flow.

### QA-EST-008: Submit Flow Generates Reviewable Customer PDF Before Status Change
- **Past failure:** Users could change status without reviewing/sending the customer document.
- **Correct behavior:** Submit for Approval must generate/open the customer-safe proposal PDF for review before marking the estimate submitted.
- **Verify:**
  - Click Submit for Approval on a Draft estimate.
  - The current estimate is saved first.
  - Proposal PDF opens or appears for review.
  - PDF excludes internal cost, Cost Book rates, gross profit, and margin.
  - Status must not change until the user confirms the submit/send step.

### QA-EST-009: Submitted Status Updates Automatically After Submit Confirmation
- **Past failure:** Users had to manually set Submitted/Pending after export.
- **Correct behavior:** After the submit/send confirmation, status updates automatically.
- **Verify:**
  - Complete the Submit for Approval flow.
  - Header badge updates to Submitted for Approval or the agreed equivalent.
  - Estimate list row updates without manual header editing.
  - API/backend accepted status value must match frontend/reporting normalization.

### QA-EST-010: Estimate List Has Award/Won Quick Action
- **Past failure:** Users had to open the estimate and edit status manually to mark awarded.
- **Correct behavior:** Submitted/Pending estimates can be marked Awarded/Won from the estimate list.
- **Verify:**
  - On the estimate list, submitted/pending rows expose an Award/Won action.
  - Confirming the action updates status to Awarded/Won.
  - Awarded estimates appear correctly in Revenue Forecast and reports.

### QA-EST-011: Estimate List Has Lost/Declined Quick Action With Required Reason
- **Past failure:** Lost status could be selected manually, and reason handling was buried in the header.
- **Correct behavior:** Lost/Declined is a list action and requires a reason before status changes.
- **Verify:**
  - On the estimate list, eligible rows expose Mark Lost/Declined.
  - Clicking opens a reason dialog.
  - Confirm is disabled until a reason is selected/entered.
  - After confirm, status changes to Lost/Declined and reports show it in lost revenue.

### QA-EST-012: Estimate List Has Create Revision Workflow Action
- **Past failure:** Revision behavior was mixed with save snapshots and manual status editing.
- **Correct behavior:** Create Revision is a deliberate workflow action from the list or a clear revision panel. It creates the next sequential editable revision copy and must not be confused with normal Save or a one-time snapshot note.
- **Verify:**
  - Eligible estimates expose Create Revision.
  - Action creates or opens the next sequential editable revision linked to the original estimate.
  - User can edit that revision and save it under that revision number.
  - Original estimate remains auditable.
  - User can tell "Save Draft" from "Create Revision" without reading docs.

### QA-EST-013: AI and API Cannot Bypass Status Workflow Unsafely
- **Past failure:** AI/status endpoints could patch status directly without matching the UI workflow vocabulary.
- **Correct behavior:** AI status changes must use the same guarded workflow semantics or be limited to disposable/approved test records.
- **Verify:**
  - `update_estimate_status` AI action and `/api/v1/estimates/{id}/status` accept the same status vocabulary used by UI/reporting.
  - Lost status from AI/API requires a lost reason.
  - "Submitted for Approval" does not fail due to backend accepting only "Submitted."
  - Agents must not use AI/API status patching on real estimates without explicit approval.

### QA-EST-015: Revision Snapshot Must Match Visible Estimate State
- **Past failure:** A current revision showed `6 labor` and `Total: $240,093` while the visible estimate form showed `8 rows` and Grand Total `$291,378`. Revision history captured stale database state instead of the current visible estimate.
- **Correct behavior:** There is one simple Create Revision workflow. It creates the next sequential editable revision copy, and saving that revision persists row counts/totals under that revision number.
- **Verify every run:**
  - Use an approved disposable `QA_TEST_<timestamp>` estimate.
  - Save an estimate/revision with 6 labor rows as Rev 1.
  - Click the single `Create Revision` action.
  - The app must create/open Rev 2 as an editable copy.
  - Add 2 additional labor rows to Rev 2 and verify the visible Labor section shows 8 rows.
  - Save Rev 2 and verify Summary Grand Total changes.
  - Revision History must show Rev 1 with 6 labor rows/old total and Rev 2 with 8 labor rows/new total.
  - Refresh the page and reopen Revision History; Rev 2 must still show 8 labor rows and the new Grand Total.
  - Restore/revert Rev 1 and verify the estimate returns to Rev 1's row count and total.
- **Evidence required:** Screenshot of Rev 1 before creating Rev 2; screenshot of editable Rev 2 after adding rows; screenshot after saving showing revision card count/total; screenshot after refresh.
- **Severity if failed:** High; P0 if revisions are shown in demo.

### QA-EST-016: All Revision Creation Entry Points Must Use The Same Save/Snapshot Contract
- **Past failure:** `Save + Revision` saved current rows before snapshot, but drawer `+ Create Revision` could snapshot stale DB data because it posted directly to the revision endpoint.
- **Correct behavior:** There should be only one revision creation action. Remove or hide duplicate snapshot-style entry points such as `Save + Revision` plus drawer `+ Create Revision`, unless every visible entry point calls the exact same editable sequential revision workflow.
- **Verify every run:**
  - Estimate form must not show both `Save + Revision` and drawer `+ Create Revision` as separate ways to create different revision artifacts.
  - The visible action should be named clearly, preferably `Create Revision`.
  - Clicking it creates the next sequential revision number.
  - Saving that revision saves into that revision number, not the previous estimate/revision.
  - Revision Compare must show the labor/revenue delta between revisions correctly.
  - Restoring a revision must restore the exact row count and totals shown on that revision card.

---

## Analytics (continued)

### QA-AN-008: MTD and QTD Tabs Must Respect Selected Year
- **Past failure:** `periodDateRange` computed used `now.getFullYear()` for MTD and QTD date calculations. Selecting year 2025 while on MTD tab caused KPI cards to filter to April 2026 (today's month in 2026), not April 2025 — creating a logical mismatch with the year displayed in the toolbar.
- **Fixed:** MTD/QTD now use `selectedYear.value` for the year component. For past years, "to" is set to end of that month/quarter rather than "now."
- **Verify:**
  - Change year to 2025. Click MTD tab. KPI cards must show data for April 2025, not April 2026.
  - Change year back to 2026. MTD must show data for April 2026 (current month to today).
  - Chart x-axis must also reflect 2025 or 2026 accordingly.

### QA-AN-009: Revenue Forecast Chart Must Follow MTD/QTD/YTD Period Selector
- **Superseded target:** QA-AN-010 through QA-AN-016 define the current required behavior. The chart must follow the top period selector, but QTD/Q2 should render the full quarter structure, not a single unreadable point.
- **Past failure:** Revenue Forecast chart (Revenue vs Cost Analysis) always showed the full selected year (Jan–Dec) regardless of which time period tab was active. KPI cards correctly filtered to MTD/QTD/YTD but the chart stayed on the full year — visually contradicting the KPI totals on the same screen.
- **Fixed:** Chart data now uses the period date range from `periodDateRange`. When MTD is selected, chart shows only the current month's bars. QTD shows the current quarter. YTD shows Jan→current month. ALL shows the full year.
- **Number reconciliation rule:** If KPI cards show $1.5M Awarded for MTD but the chart shows bars extending through Jan–Jun, that is a FAIL.
- **Verify:**
  - With year = 2026 and tab = YTD: KPI cards show year-to-date totals AND chart x-axis shows Jan 2026 → current month only.
  - Switch to MTD: KPI cards update AND chart collapses to show only the current month bar.
  - Switch to ALL: chart expands to show all available data across all months.
  - KPI card totals and chart bar heights must be consistent for the same period — not contradictory.
  - Screenshot before and after tab change as evidence.

### QA-AN-010: Top Period Tabs Include Quarter Filters
- **Past failure:** Revenue Forecast only exposed MTD, QTD, YTD, and ALL, leaving users unable to intentionally inspect Q1/Q2/Q3/Q4 periods.
- **Correct behavior:** Top-level period tabs include MTD, Q1, Q2, Q3, Q4, QTD, YTD, and ALL.
- **Verify:**
  - Open Revenue Forecast.
  - Confirm all eight tabs are visible in the primary Financial Dashboard filter bar.
  - Q1/Q2/Q3/Q4 must be top-level business filters, not hidden chart controls.

### QA-AN-011: One Filter System Controls KPIs and Chart
- **Past failure:** KPI cards used MTD/QTD/YTD/ALL while the chart had its own Monthly/Quarterly/Cumulative controls, creating contradictory results.
- **Correct behavior:** Selecting a top period tab updates both KPI cards and Revenue vs Cost chart together.
- **Verify:**
  - Select each period tab: MTD, Q1, Q2, Q3, Q4, QTD, YTD, ALL.
  - After each click, KPI cards and chart must both reflect the selected period.
  - No separate chart filter may produce a different business period than the top tabs.

### QA-AN-012: No Separate Monthly/Quarterly/Cumulative Chart Filter
- **Past failure:** Revenue vs Cost Analysis exposed separate Monthly, Quarterly, and Cumulative buttons that made users think the chart had different filters from the KPI cards.
- **Correct behavior:** Chart grouping should be derived automatically from the selected top period, or any display toggle must be clearly non-filtering and impossible to contradict KPI totals.
- **Verify:**
  - Revenue vs Cost Analysis must not show Monthly/Quarterly/Cumulative as independent period filters.
  - If any chart display toggle remains, changing it must not alter the period represented by the chart.

### QA-AN-013: QTD/Q2 Chart Shows Full Quarter Structure
- **Past failure:** QTD/Q2 with $1.5M Awarded displayed as a single barely visible April point, visually communicating $0 even though KPI cards showed revenue.
- **Correct behavior:** QTD/Q2 should show Apr, May, and Jun for 2026. Future months in the quarter must be visible at $0 or projected/pipeline values.
- **Verify:**
  - Select QTD while current date is in Q2 2026.
  - Chart x-axis must show Apr 2026, May 2026, and Jun 2026.
  - Awarded/Pipeline/Lost values must reconcile to KPI totals for the selected period.
  - If KPI shows $1.5M but chart visually looks empty or $0, FAIL.

### QA-AN-014: Quarter Definitions Are Correct
- **Past failure:** QTD/quarter logic was confused in review and not visibly testable.
- **Correct behavior:** Q1 = Jan-Mar, Q2 = Apr-Jun, Q3 = Jul-Sep, Q4 = Oct-Dec.
- **Verify:**
  - Select each quarter tab and inspect chart labels.
  - Q1 shows Jan, Feb, Mar.
  - Q2 shows Apr, May, Jun.
  - Q3 shows Jul, Aug, Sep.
  - Q4 shows Oct, Nov, Dec.

### QA-AN-015: Future Periods Are Selectable for Projections
- **Past failure:** Dashboard behavior was anchored to current/past data, making it hard to inspect future quarters and projected pipeline.
- **Correct behavior:** User can select future quarters/year periods and see $0 actuals plus projected/pipeline/staffing-plan context where available.
- **Verify:**
  - While current date is Apr 26, 2026, select Q3 and Q4 for 2026.
  - Chart must show Jul-Sep or Oct-Dec labels.
  - Actual awarded/lost may be $0, but pending/pipeline/future revenue should appear if records exist in those future dates.
  - Empty future periods must still render clearly, not disappear.

### QA-AN-016: KPI and Chart Totals Reconcile Numerically
- **Past failure:** Agents called the dashboard working by visual inspection even when KPI and chart communicated different totals.
- **Correct behavior:** Testers must reconcile numbers, not just glance at charts.
- **Verify:**
  - For each selected period, sum visible chart data for Awarded, Pipeline, and Lost.
  - Compare against Awarded Revenue, Pending Pipeline, and Lost Revenue KPI cards.
  - Minor rounding differences from `$1.5M` formatting are acceptable, but category totals must be logically consistent.
  - Include screenshots and the visible values in the report.

### QA-MF-001: Manpower Forecast Must Have Position Filter
- **Past failure:** With real-world data, the position breakdown table shows 30+ positions with no way to filter. Users cannot isolate a specific craft (e.g., "show me only Pipefitter Journeyman") without scrolling through every row.
- **Fixed:** A POSITION FILTER dropdown added to the filter bar. When a position is selected, only that position's rows appear in the breakdown table and chart. Selecting "All Positions" restores the full view.
- **Verify:**
  - Run Manpower Forecast. Filter bar must include a POSITION FILTER dropdown (in addition to existing Include/Status/Positions controls).
  - Select a specific position — table must collapse to show only that position's row.
  - Deselect (clear filter) — all positions return.
  - TOTAL row at bottom must reflect only the visible/filtered positions.

### QA-SP-006: Staffing Plans Must Support Crew Templates
- **Past failure:** Staffing plans had no "Load Crew" button. Adding labor rows required clicking "+ Add Employee" one position at a time, making it impractical to build a crew of 10+ people. Estimate forms had crew template support but staffing forms did not.
- **Fixed:** Crew template button enabled in staffing form. Clicking it opens the crew template picker dialog, same as the estimate form.
- **Verify:**
  - Open a Staffing Plan labor grid.
  - A "Load Crew" button must be visible.
  - Clicking it must open a crew template picker dialog.
  - Selecting a template must populate labor rows with the template's positions and quantities.
  - If the button is not present or clicking it does nothing → FAIL.

### QA-SP-007: Staffing Plans Must Show Rough Cost and Margin
- **Past failure:** Staffing plans only showed billing totals (Rough Labor Revenue). There was no internal cost calculation, so planners had no way to know if a job was profitable at the planning stage. A job could be bid at a loss without any warning.
- **Fixed:** A "Rough Cost Analysis" panel added below the labor grid in StaffingFormView. It shows: Rough Revenue (labor billing total), Rough Internal Cost (from cost book), and Rough Margin %. Driven by cost book rates for each position.
- **Verify:**
  - Open a Staffing Plan with labor rows.
  - A "Rough Cost Analysis" section must be visible below the labor grid.
  - Rough Revenue must match the labor billing subtotal.
  - Rough Internal Cost must be non-zero when cost book rates exist for the positions.
  - Rough Margin % = (Revenue − Cost) / Revenue — must display correctly.
  - If cost book rates are missing for a position, that must be flagged (not silently $0 margin).

### QA-SP-008: Staffing Plans Must Use Full Job Cost Analysis Detail
- **Past failure:** Staffing plans showed only simplified Rough Revenue / Rough Internal Cost / Rough Margin cards, while estimates showed the full Job Cost Analysis experience with burden badges and per-position cost detail.
- **Correct behavior:** Staffing plans with labor rows should expose enough cost detail for management review, matching the estimate form's Job Cost Analysis expectations where practical.
- **Verify:**
  - Open a Staffing Plan with labor rows.
  - Confirm burden/detail information is visible, not only three summary cards.
  - Confirm per-position cost breakdown is shown for labor rows.
  - Confirm missing Cost Book rates are flagged clearly.
  - Confirm the staffing summary includes Rough Revenue, Internal Cost, Gross Profit, and Gross Margin %.
  - Capture screenshot evidence of the burden/detail section and cost table.

### QA-SP-009: Staffing Plans Must Support Submit for Approval Workflow
- **Past failure:** Staffing plans had Save and Convert to Estimate, but no formal Submit for Approval workflow before conversion.
- **Correct behavior:** Saved Draft staffing plans can be submitted for management review without converting them to estimates.
- **Verify:**
  - Open a saved Draft staffing plan.
  - A "Submit for Approval" action must be visible.
  - The action must not appear for new unsaved plans or converted plans unless the workflow explicitly supports that state.
  - Clicking Submit for Approval must save current changes, show a confirmation/review step, then update status automatically.
  - Status badge must update to Submitted for Approval or the agreed display equivalent.
  - Backend/API status endpoint must exist or the UI action must not be shown.
  - Reports/forecast must classify submitted staffing plans correctly.

### QA-SP-010: Expired Unconverted Staffing Plans Must Be Flagged
- **Past failure:** Staffing plans could remain in the future-demand/pipeline flow even after their scheduled start date arrived, despite not being converted to an estimate. That makes stale opportunities look like real upcoming work.
- **Correct behavior:** A staffing plan with `convertedEstimateId = null` and `startDate <= today` must be visibly flagged as Expired/Stale/Needs Review and must be separated from normal future demand.
- **Verify:**
  - Find or create an approved disposable staffing plan with labor rows, `convertedEstimateId = null`, and start date on or before today.
  - Staffing list/card must show an Expired/Stale/Needs Review badge or equivalent review state.
  - Manpower Forecast must not count it as normal clean future demand without a stale/review indicator.
  - Revenue Forecast/Future Revenue must not treat it as ordinary future projection unless clearly separated as stale pipeline.
  - The app must not auto-mark it Lost; Lost/Declined requires a deliberate user action and reason.
  - If the plan is converted to an estimate, the expired/stale staffing-plan warning must clear and the estimate becomes source of truth.

### QA-AN-017: Expired Staffing Plans Must Not Inflate Future Revenue
- **Past failure:** Future Revenue could include staffing-plan opportunities even after the planned start date had arrived without conversion to an estimate.
- **Correct behavior:** Expired/stale unconverted staffing plans must be excluded from normal future revenue totals or shown in a separate review bucket that is clearly not booked/projected revenue.
- **Verify:**
  - Select a forecast period containing an expired unconverted staffing plan.
  - Future Revenue and pipeline KPIs must not silently include that plan as ordinary future work.
  - If stale value is displayed, it must be labeled separately as Needs Review/Stale Pipeline.
  - Converting the staffing plan to an estimate must move value from staffing-plan forecast source to estimate source without double counting.

### QA-AN-018: KPI Drilldown Shows Contributing Source Records
- **Boss-demo requirement:** KPI cards must be defendable and traceable to the records behind the number.
- **Correct behavior:** Awarded Revenue, Pending Pipeline, Lost Revenue, Total Pipeline, Weighted Pipeline, and Future Revenue expose a drilldown path to contributing estimates/staffing plans/invoices or proposal PDFs where available.
- **Verify:**
  - Open Revenue Forecast for a year/period with data.
  - For Awarded Revenue, open the drilldown/source list and record the visible source estimate numbers and values.
  - Sum the drilldown values and compare to the KPI value, allowing only rounding differences from `$K/$M` formatting.
  - Open one contributing estimate/source record from the drilldown.
  - If no drilldown exists, report FAIL for the boss-demo requirement.

### QA-AN-019: KPI Drilldown Works For Future Staffing Forecast
- **Boss-demo requirement:** Future staffing plan projections must be traceable to individual future plans/invoices/source records.
- **Correct behavior:** Future Revenue or staffing forecast KPI lets the user see which future staffing plans/estimates contribute to the number and open the source record.
- **Verify:**
  - Select 2027 or another future period with staffing plans.
  - Open Future Revenue / forecast drilldown.
  - Confirm each contributing plan has a non-zero labor forecast and date range.
  - Open one future staffing plan from the drilldown and verify the labor rows match the forecast contribution.

---

## AI Demo Readiness

### QA-AI-001: AI Completes Estimate Header, Rate Book, And Crew/Labor Rows
- **Boss-demo requirement:** Agent must accurately fill out an estimate, including header, loading rates, adding crew templates/labor, and creating usable totals.
- **Verify:**
  - On a new estimate, ask: `Create a new estimate for Shell Deer Park TX turnaround, both shifts, 14 days starting June 10 2026. Load matching Shell Deer Park rates and add a boilermaker crew with 1 General Foreman, 4 Boilermaker Journeymen, and 2 Boilermaker Helpers.`
  - AI must stage changes for review before applying.
  - After Apply, header fields must include client, city/state, dates, duration, shift, and job type.
  - Active Rate Book must be loaded or a clear exact/nearest-rate suggestion must appear.
  - Labor rows must be present with positions, quantities/shifts, bill rates, and non-zero totals.
  - If AI fills only the header or rows have no rates when a matching rate book exists, FAIL.

### QA-AI-002: AI Completes Staffing Plan Header, Rate Book, And Crew/Labor Rows
- **Boss-demo requirement:** Agent must accurately fill out a staffing plan, not just its header.
- **Verify:**
  - On a new staffing plan, ask: `Create a staffing plan for Dow Deer Park TX starting January 6 2027 for 21 days, day shift, load matching or nearest rates, and add a turnaround crew/template.`
  - AI must stage changes for review before applying.
  - Header must be populated with client/location/dates/duration/shift.
  - A rate book must be loaded or clearly suggested.
  - Labor rows or a crew template must be applied with non-zero Rough Labor.
  - Staffing plan card/list must show labor preview pills after save if a save is approved.
  - Header-only staffing plans are FAIL unless the explicit test is empty-plan validation.

### QA-AI-003: AI Answers Jobs Going On Today From DB
- **Boss-demo requirement:** User can ask "what jobs are going on today" and AI returns every current job based on estimate start/end dates.
- **Verify:**
  - Ask the AI: `What jobs are going on today?`
  - AI must use live/company-scoped DB data, not guess.
  - Answer must list each active awarded job where `startDate <= today <= endDate`.
  - Reconcile against API/DB source records and report any missing/extra job.
  - Include job number/name/client/location/start/end where available.

### QA-AI-004: AI Answers Jobs Coming Up In The Next Two Weeks
- **Boss-demo requirement:** User can ask "what jobs are coming up" and AI checks jobs starting in the next 14 days.
- **Verify:**
  - Ask: `What jobs are coming up in the next two weeks?`
  - AI must return estimates/jobs with start dates from today through today + 14 days.
  - Answer must include status and dates so the user knows whether they are awarded, submitted, pending, or draft.
  - Reconcile result against source records.
  - If AI only gives generic pipeline totals, FAIL.

### QA-AI-005: AI Answers Customer/Location History With Count, Duration, And Revenue
- **Boss-demo requirement:** User can ask questions like "How many jobs have we done for Dow in Deer Park Texas over the last year?" and receive job count, each duration, and total revenue.
- **Verify:**
  - Ask: `How many jobs have we done for Dow in Deer Park Texas over the last year? Tell me the number of jobs, duration of each one, and total revenue generated.`
  - AI must filter by customer and location and date window.
  - Answer must include count, each job's start/end/duration, and total revenue.
  - Reconcile against estimates/source records.
  - If the client/location has no data, AI must clearly say no matching records rather than inventing jobs.

### QA-AI-006: Review With AI Produces Record-Specific Review
- **Boss-demo requirement:** Review with AI must work and be useful.
- **Verify:**
  - Open an estimate or staffing plan with header, rate book, labor rows, and totals.
  - Click `Review with AI`.
  - AI response must reference the current record's actual client, dates, loaded rate book, labor rows, totals, and any missing/risky fields.
  - It should identify readiness for save/submit or list specific blockers.
  - Generic responses that do not reference current form values are FAIL.

### QA-AI-007: AI Search Tool Supports City And State Filters
- **Boss-demo requirement:** Customer/location questions must be filtered by both customer and location.
- **Verify:**
  - Ask a customer/location history question such as `How many jobs have we done for Dow in Deer Park Texas over the last year?`
  - Confirm the AI/tool path can filter by client, city, state, and date window.
  - The returned jobs must match the requested city/state, not merely the customer name.
  - Reconcile the answer against source records.

### QA-AI-008: Active And Upcoming Job Questions Use Date-Range Tooling
- **Boss-demo requirement:** AI must answer active and upcoming job questions from estimate date ranges.
- **Verify:**
  - Ask `What jobs are going on today?` and verify it uses active-date logic.
  - Ask `What jobs are coming up in the next two weeks?` and verify it uses a date range from today through today + 14 days.
  - Answers must include job number/name, status, client/location, start date, and end date where available.
  - Reconcile both answers against source records.

### QA-AI-009: AI Provider Config Is Present Without Exposing Secrets
- **Boss-demo requirement:** AI must be online for the demo.
- **Verify:**
  - Confirm the local AI provider config/key is present and non-placeholder without printing the key value.
  - Confirm the configured model/provider matches the intended demo path.
  - Send a harmless prompt through the AI sidebar or API and confirm a successful response.
  - Reports and screenshots must not expose API keys or secrets.

### QA-AI-010: Exact Demo Prompt - DOW Deer Park Both-Shift Estimate
- **Boss-demo prompt:** `Create new estimate for DOW Chemical in Deer Park Texas, starting June 10 2026 and lasting 10 days. It will be both day and night shift 10 hours each with OT after 40 no dt. start me off with a PM and a foreman on both days and nights and add me a pipefitter crew to start`
- **Verify:**
  - AI stages changes for review before applying.
  - Header sets client `DOW Chemical`, city `Deer Park`, state `TX`, start `2026-06-10`, end `2026-06-19`, days `10`, shift `Both`, hours/shift `10`.
  - OT method maps to weekly 40 (`OT > 40 hrs/wk`) and DT weekend setting maps to `No DT Weekends`.
  - Labor rows include PM and Foreman for both day and night, plus a pipefitter crew.
  - Rows have rates from an active/suggested rate book where available and non-zero totals after apply.
  - If it only fills the header, omits night/day split, or ignores OT/DT rules, FAIL.

### QA-AI-011: Exact Demo Prompt - P66 Linden NDT Estimate With Per Diem
- **Boss-demo prompt:** `Create estimate for P66 in Linden NJ Starting oct 1, 2026 and lasting 30 days. It will be day shift only 12 hour shifts with ot after 8 and dt on sundays. Start me off with a ndt crew and a foreman. add standard per diem for this job.`
- **Verify:**
  - Header sets client `P66`/`Phillips 66`, city `Linden`, state `NJ`, start `2026-10-01`, end `2026-10-30`, days `30`, shift `Day`, hours/shift `12`.
  - OT method maps to daily 8 (`OT > 8 hrs/day`) and DT weekend setting maps to Sundays only.
  - Labor rows include an NDT crew and a Foreman with non-zero totals.
  - Standard per diem is added from rate book expense items when available.
  - If AI cannot add per diem because no action/tool exists, it must clearly say so and the tester must mark the per diem portion FAIL.
  - If no matching P66/Linden rate book exists, AI must suggest the nearest rate book rather than fabricating rates.

### QA-AI-012: Exact Demo Prompt Pack - Live Job And Revenue Q&A
- **Boss-demo prompts:**
  - `What jobs do i have coming up?`
  - `What jobs are going on today?`
  - `Whats my estimated revenue for this quarter?`
- **Verify:**
  - Upcoming jobs answer uses jobs with start dates from today forward, preferably the next 14 days unless the response asks for a broader clarification.
  - Jobs today answer uses `startDate <= today <= endDate` and appropriate awarded/active status logic.
  - Quarter revenue answer uses the current quarter for the selected/company context and states what statuses are included.
  - All answers are reconciled against source records/API data.
  - AI must not answer from memory or generic pipeline text if a DB tool can answer.

### QA-AI-013: Exact Demo Prompt - Shell Jobs This Year
- **Boss-demo prompt:** `How many jobs have we done for Shell this year?`
- **Verify:**
  - AI filters Shell/Shell Oil estimates for the current selected year/date context.
  - Answer includes job count and enough detail to defend the count: job numbers/names, date ranges, statuses, and revenue total where available.
  - If "done" is interpreted as completed/awarded only, the AI must state that interpretation. If it includes submitted/pending too, it must say so.
  - Reconcile against source records.

### QA-AI-014: Review With AI Finds Left-On-The-Table Revenue From Past Jobs
- **Status:** DEFERRED until after the demo by Joseph on 2026-04-26. Do not run this in demo-readiness sweeps unless Joseph explicitly reopens it.
- **Post-demo requirement:** `Review with AI` should compare the current estimate to a similar past awarded job and explain missing labor/equipment revenue.
- **Verify:**
  - Open `26-0031-VLO` / `Valero Port Arthur Hydrocracker Turnaround`.
  - Click `Review with AI`.
  - AI must search same client/location history and use a full-detail past estimate tool, not just summary search results.
  - AI response must name `25-0031-VLO` / `Valero Port Arthur CDU Turnaround 2025`.
  - AI must list missing or undercounted crew such as Foreman, Pipefitter Journeyman, Pipefitter Helper, Welder Journeyman, Boilermaker Journeyman, Crane Operator, Safety Watch, and NDT Technician where supported by source data.
  - AI must list missing equipment such as cranes, manlifts, forklift, welding machines, air compressors, and light towers.
  - AI must include dollar values per missing category and a total left-on-the-table estimate around `$262K`, allowing reasonable rounding from seeded rates/hours.
  - AI must offer to add missing labor/equipment instead of silently changing the estimate.
  - If the response is generic, does not name the 2025 CDU job, or cannot access labor/equipment rows for the past estimate, FAIL.

---

## Demo Data Readiness

### QA-DATA-001: Realistic Demo Data Exists For 2025, 2026, And 2027
- **Boss-demo requirement:** Demo data must support historical, current, and future forecast storytelling.
- **Verify:**
  - 2025 has realistic completed/awarded/lost historical estimates with revenue.
  - 2026 has current-year awarded, submitted/pending, lost, and active jobs.
  - 2027 has staffing plans with labor rows, non-zero Rough Labor/Future Revenue, and forecast impact.
  - Data must look realistic: recognizable industrial clients, locations, dates, durations, roles, rates, and totals.
  - Do not create/seed/reset data during this check without explicit approval.

### QA-DATA-002: 2027 Staffing Plans Are Fully Populated And Forecasted
- **Boss-demo requirement:** 2027 staffing plans must show that future manpower/revenue forecasting works.
- **Verify:**
  - Find 2027 staffing plans.
  - Each demo-facing plan must have labor rows, schedule dates, role quantities, rates, and non-zero Rough Labor.
  - Revenue Forecast and Manpower Forecast must show the 2027 plans when 2027/future period is selected.
  - Converted 2027 staffing plans must not double-count beside their estimates.

### QA-DATA-003: Demo Seed Includes At Least 3 Crew Templates
- **Boss-demo requirement:** Crew template workflow must have realistic data to show.
- **Verify:**
  - Inspect crew template list/API in read-only mode.
  - At least 3 realistic templates must exist for the demo company.
  - Each template must contain meaningful positions/quantities, not empty shells.
  - Applying a template with an active rate book must create labor rows with rates.

### QA-DATA-004: Exact Seed Counts - 25 Estimates For 2025 And 10 Staffing Plans For 2027
- **Boss-demo requirement:** Seed data must support historical 2025 and future 2027 storytelling.
- **Verify:**
  - Count demo-company estimates with 2025 dates: expected 25.
  - Count demo-company staffing plans with 2027 dates: expected 10.
  - 2025 estimates must have statuses, summaries/revenue, realistic clients/locations, and labor rows where appropriate.
  - 2027 staffing plans must have labor rows and non-zero Rough Labor.
  - Do not run reset/seed commands against live SQL Express without Joseph approval.

---

## Platform Shell / App Registry

### QA-PLAT-001: Repo Truth And Real Paths Are Documented Before Implementation
- **Platform requirement:** Claude must inspect the actual repo before coding and must not invent paths.
- **Verify:**
  - Architecture/worklog docs identify the real locations of `Api/Program.cs`, `Data/AppDbContext.cs`, `Data/Migrations`, `Data/DBInitializer.cs`, `Data/DesignTimeContextFactory.cs`, `webapp/src/apps.ts`, and `webapp/src/router/index.ts`.
  - Any prompt path that conflicts with the repo is corrected in writing.
  - No new feature is placed under invented paths like `Api/Data/AppDbContext.cs` or `Api/Migrations`.

### QA-PLAT-002: Portal Home And App Tiles Load Without Breaking Estimating
- **Platform requirement:** One login, one portal shell, three app entry points.
- **Verify:**
  - `/` loads a Portal dashboard/launcher, not a dead page.
  - Portal has visible entry points for Estimating, Planning / PM, and Scheduling.
  - `/estimating` and key existing estimating routes still load.
  - Portal uses backend summary/read-model data where possible; it must not duplicate scheduling/planning business logic in the Vue view.

### QA-PLAT-003: Claude Cannot Self-Close TODO Items
- **Governance requirement:** Only Joseph or Codex closes TODO items.
- **Verify:**
  - Any Claude-updated TODO item marked complete includes either Joseph/Codex verification evidence or an explicit Joseph waiver/accepted-as-is note.
  - If Claude checked an item complete with only implementation notes, mark it REOPENED and cite the item.

### QA-PLAT-004: Existing Estimating Demo Regressions Still Pass After Platform Work
- **Platform requirement:** New apps must not break the existing demo-critical estimating app.
- **Verify:**
  - Existing estimate list, new estimate form, staffing plan list/form, rate book, cost book, crew template, revenue forecast, manpower forecast, and AI sidebar smoke checks still pass.
  - Existing completed/accepted AI demo items in `docs/LIVE_QA_TODO.md` remain closed unless fresh evidence fails.
  - Cost Book data is not deleted, reset, or replaced.

### QA-PLAT-005: Planning Docs Are Requirements-Traceable Before Coding
- **Platform requirement:** Claude's scheduling/planning step-out docs must be verified against Joseph's requirements before implementation starts.
- **Verify:**
  - `SCHEDULING_STEP_OUT_PLAN.md` and `SCHEDULING_TODO.md` exist when Claude claims planning is complete.
  - The docs are grounded in actual repo paths and naming, not invented greenfield structure.
  - Each Joseph requirement is mapped to acceptance criteria or an explicit open question/risk.
  - Missing requirements become open items in `docs/LIVE_QA_TODO.md` before coding is accepted.
  - Claude does not treat planning-doc completion as implementation completion.

### QA-PLAT-006: Platform Requirements Traceability Map Is Current
- **Platform requirement:** Joseph's master requirements must be testable and traceable before Codex verifies Claude's work.
- **Verify:**
  - `docs/PLATFORM_REQUIREMENTS_TRACEABILITY.md` exists.
  - It maps the master requirements to live QA gates and regression checklist IDs.
  - New Joseph requirements are added to the map before related implementation is accepted.
  - Any missing or ambiguous requirement becomes an open live QA item instead of being silently assumed.

### QA-PLAT-007: Phase Definition-Of-Done Requires Tests And Evidence
- **Platform requirement:** A platform phase is not complete until implementation, tests, worklog, regression updates, and evidence are complete.
- **Verify:**
  - Claude's claimed phase includes updated `docs/LIVE_TODO.md`, `docs/IMPLEMENTATION_WORKLOG.md`, and `docs/TEST_RUN_LOG.md`.
  - Backend build and frontend build were run or a blocker is documented.
  - Available tests/regressions were run or their absence is documented.
  - UI-visible changes have screenshot evidence.
  - Failures are listed with likely cause and next action.

### QA-PLAT-008: Generated API Client / NSwag Sync Is Handled
- **Platform requirement:** API surface changes must stay aligned with frontend integration.
- **Verify:**
  - Claude identifies whether this repo uses generated clients for the touched endpoints.
  - If generated clients are used, the proper NSwag/openapi generation command is run and documented.
  - If direct API calls are used for a temporary slice, that decision is documented with follow-up risk.
  - Build warnings related to NSwag/client generation are recorded and assessed, not hand-waved.

### QA-PLAT-009: Core Demand And Coverage Logic Is Not Frontend-Only
- **Platform requirement:** Core operational truth should be reusable and testable, not trapped in Vue computed logic.
- **Verify:**
  - Scheduling demand aggregation is implemented in backend/shared services or read models.
  - Converted-plan dedupe is tested or directly verifiable outside a Vue view.
  - Coverage gaps, assignment conflicts, ending-soon, and available-soon logic are not only browser-side calculations.
  - Vue views/stores consume backend/read-model outputs for operational truth and focus on presentation/workflow state.

### QA-PLAT-010: Portal KPI Language Uses Projects At Risk
- **Lifecycle requirement:** Portal-level risk should surface project execution risk, not only raw FCO counts.
- **Verify:**
  - Lifecycle docs specify portal KPI text as `Projects at Risk`.
  - FCO metrics may remain in estimating/planning detail dashboards.
  - Portal dashboard model does not make pending FCO count the primary project-risk KPI.

---

## Database Bootstrap / Seed

### QA-BOOT-001: Migration-Based Bootstrap With Separated Seed Responsibilities
- **Platform requirement:** EF Core migrations are the source of truth; seed logic is explicit and safe.
- **Verify:**
  - Startup uses `Database.Migrate()` or an equivalent migration-based bootstrap.
  - No real lifecycle path uses `EnsureCreated`.
  - Reference seed and demo/sample seed are separated or clearly staged with config flags.
  - Demo seed is configurable and not assumed for higher environments.
  - Seed logic is idempotent and safe to rerun.
  - Seed logic is not moved into a controller as the primary lifecycle path.

### QA-BOOT-002: Platform Demo Seed Is Not Controller-Only
- **Platform requirement:** Demo seed may have an explicit dev endpoint, but lifecycle seed must be controlled, separated, and idempotent.
- **Verify:**
  - Scheduling/planning reference data and demo data are not only implemented inside `DevController`.
  - Any `DevController` seed endpoint is explicit, additive, demo-only, and safe to rerun.
  - Startup/bootstrap config decides whether reference/demo seed runs.
  - Seed code does not wipe live/demo data without explicit approval.

---

## Platform Demo Data

### QA-DATA-005: Platform Demo Seed Proves Scheduling And Planning Scenarios
- **Platform requirement:** Platform demo data must prove the Planning/Scheduling workflows, not just populate empty screens.
- **Verify:**
  - Demo data includes resources/people, crafts, certifications where implemented, assignments, availability, and at least one conflict.
  - Demo data includes one approved unconverted staffing plan that appears as demand.
  - Demo data includes one converted staffing plan that does not appear as separate demand.
  - Demo data includes shortages by craft, assignments ending soon, and people becoming available soon.
  - Demo data includes at least one step-out plan and generated/schedulable work package when Planning foundation is claimed complete.

---

## Planning / PM

### QA-PLAN-001: Planning App Is A Real Operational Module, Not Notes
- **Platform requirement:** Planning owns step-out plans, work packages, FCO planning, and actuals.
- **Verify:**
  - `/planning` loads a Planning / PM app shell.
  - Planning views and backend endpoints are separated from Estimating and Scheduling responsibilities.
  - Planning records link to source estimates/staffing/FCOs through explicit source links/read models, not unsafe estimate table mutation.

### QA-PLAN-002: Step-Out Step Numbering Supports Insertable Values
- **Platform requirement:** Step numbering supports values like `1`, `1.2`, `1.5`, `1.7`, `2`.
- **Verify:**
  - Step model supports a display step code/string and a sortable key, or an equivalent that preserves inserted steps.
  - Creating/editing steps with decimal-like display values persists and renders in order.
  - Implementation does not rely on a simple integer-only sequence for business-visible step numbering.

### QA-PLAN-003: Step Dependencies And Parallel Work Persist And Validate
- **Platform requirement:** Step-out plans model execution sequencing, dependency handling, and parallel work.
- **Verify:**
  - Steps can define predecessor/successor dependencies.
  - A step cannot depend on itself.
  - Circular dependencies are prevented or reported as a validation defect.
  - Parallel steps can be represented distinctly from dependency sequencing.

### QA-PLAN-004: Work Package Generation Feeds Scheduling Demand
- **Platform requirement:** Planning feeds Scheduling through work packages.
- **Verify:**
  - A step-out plan can generate one or more work packages.
  - Work packages include source link, craft/headcount, planned dates, status, client/site refs, and scheduling-ready demand fields.
  - Scheduling demand includes work packages with ready/schedulable status.

### QA-PLAN-005: Planning Dashboard And Handoff Views Are Functional
- **Platform requirement:** Planning / PM is an operational app, not only data tables.
- **Verify:**
  - Planning dashboard shows plans by status, plans needing staffing, upcoming work packages, and blocked plans where data exists.
  - Step-Out Plan Detail / Editor supports add/remove/reorder steps, dependencies, craft/headcount, duration, status, and notes.
  - Work Package / Scheduling Handoff view makes it clear which packages are ready for scheduling.
  - Planning UI does not mutate estimate commercial fields directly.

### QA-PLAN-006: Project Planning Master Docs Are Repo-Grounded
- **Project-planning requirement:** Claude must create the four planning documents from actual repo inspection before coding.
- **Verify:**
  - `PROJECT_PLANNING_MASTER_PLAN.md`, `PROJECT_PLANNING_MASTER_TODO.md`, `PROJECT_PLANNING_DATA_MODEL.md`, and `PROJECT_PLANNING_UI_MAP.md` exist.
  - The docs identify actual repo structure, frontend modules, backend controllers/services, EF DbContext/migrations/bootstrap, auth/company scoping, API conventions, and existing estimate/planning/scheduling concepts.
  - The docs recommend exactly where the project-planning capability lives and how it fits the shared portal.
  - The docs distinguish reuse existing, extend existing, and build new.
  - No implementation/source-code change is claimed as part of this planning-only gate.

### QA-PLAN-007: Project Planning Domain Model Covers Required Entities
- **Project-planning requirement:** The proposed domain model must support the full project planning hierarchy and timeline concepts.
- **Verify:**
  - Plan covers Project, ProjectTimeline, ProjectPhase, PlanTask, PlanTaskDependency, PlanMilestone, StepOutPlan, StepOutStep, StepOutSubStep, TaskAssignment, TaskScheduleStatus, TaskProgressSnapshot, EstimateLink, FcoLink, TimelineBaseline, TimelineVariance, and CalendarEntry/equivalent.
  - Each entity has purpose, owner, key fields, relationships, and app boundary classification.
  - Hierarchy storage and dependency storage strategies are specified.
  - Audit/status/ownership fields are addressed.

### QA-PLAN-008: Estimate / FCO / Task Traceability Model Is Explicit
- **Project-planning requirement:** Tasks, FCOs, and estimates must be traceable both directions with clear source-of-truth rules.
- **Verify:**
  - Plan defines how tasks link to estimates and FCOs.
  - Plan defines how FCOs link to estimates.
  - Plan defines validation for missing estimate links, missing FCO links, and mismatched FCO/estimate/task links.
  - Plan states estimate source of truth remains Estimating and FCO source of truth is owned by the appropriate Planning/FCO model.
  - Plan states what happens when an estimate changes, an FCO changes, or a task loses/mismatches linkage.

### QA-PLAN-009: Gantt, Calendar, And Drill-Down Share One Planning Model
- **Project-planning requirement:** Gantt, calendar, and task detail must be different views of the same plan data.
- **Verify:**
  - Plan explains how Gantt visualizes project/phase/task timelines and dependencies.
  - Plan explains how Calendar visualizes tasks, due dates, milestones, workload, and filters.
  - Plan explains drill-down from project -> phase -> task -> subtask -> step-out plan.
  - Plan avoids separate disconnected data stores for Gantt, calendar, and task detail.
  - UI map includes routes/pages/components and breadcrumb/drill-down behavior.

### QA-PLAN-010: Ahead/Behind And Schedule Health Logic Is Defined
- **Project-planning requirement:** The platform must determine ahead, on track, behind, and downstream impact.
- **Verify:**
  - Plan defines planned start/finish, actual start/finish, forecast finish, duration, percent complete, baseline, current schedule, and variance.
  - Plan gives formulas or concrete logic for schedule variance, milestone slippage, phase slippage, task slippage, overdue detection, and ahead/on-track/behind indicators.
  - Plan describes dependency impact when a task slips.
  - Plan addresses upcoming milestone awareness and critical path considerations where appropriate.

---

## Scheduling

### QA-SCHED-001: Scheduling Demand Uses Explicit Status And Source Rules
- **Platform requirement:** Demand comes only from valid source types and documented statuses.
- **Verify:**
  - Demand service documents which estimate statuses count as schedulable demand.
  - Demand includes estimates that represent real work/jobs per that documented status rule.
  - Demand includes Planning work packages ready for scheduling.
  - Demand does not read every estimate blindly.

### QA-SCHED-002: Converted Staffing Plans Are Deduped
- **Platform requirement:** Converted staffing plans must not count separately from linked estimates.
- **Verify:**
  - Staffing plans where `ConvertedEstimateId != null` are excluded from scheduling demand and forecast demand.
  - The linked estimate appears as the source of truth when its status qualifies.
  - Add or run a focused test proving one converted staffing plan does not appear as a separate demand row.

### QA-SCHED-003: Approved Unconverted Staffing Plans Count As Future Demand
- **Platform requirement:** Approved staffing plans where `ConvertedEstimateId IS NULL` count as planned future demand.
- **Verify:**
  - An approved unconverted staffing plan appears in scheduling demand.
  - Labor rows/headcount from the plan drive craft demand.
  - Header-only staffing plans with no labor are not treated as valid coverage.

### QA-SCHED-004: Resources, Assignments, Shortages, Ending-Soon, And Available-Soon Are Visible
- **Platform requirement:** Scheduling owns actual people assignment and coverage visibility.
- **Verify:**
  - `/scheduling` loads a Scheduling app shell/dashboard.
  - Resources/people list loads.
  - Assignments list or board loads.
  - At least one shortage can be shown from demo/test data.
  - Ending-soon assignments/jobs and available-soon resources are surfaced.

### QA-SCHED-005: Assignment Conflict And Availability Rules Work
- **Platform requirement:** Scheduling detects bad assignments.
- **Verify:**
  - Double-booked resource assignments are detected.
  - Assignments during PTO/blackout/unavailable blocks are detected.
  - Certification/skill mismatch is detected or explicitly documented as a future-phase gap with TODO coverage.

### QA-SCHED-006: Scheduling Plan Covers Required Business Workflows
- **Platform requirement:** Scheduling must support the business workflows Joseph listed, not just generic calendar screens.
- **Verify:**
  - Plan covers estimates/staffing data feeding scheduling through explicit source rules.
  - Plan covers placing people on jobs, moving people to another job when one is ending, and notifying when people are about to become available.
  - Plan covers craft filtering/sorting, craft shortages, staffing gaps, current/upcoming work, and job ending visibility.
  - Plan documents how Scheduling stays separate from Estimating while still consuming estimate/staffing inputs.
  - Any deferred workflow is clearly listed with risk, phase, and acceptance criteria.

### QA-SCHED-007: Scheduling Filters, Boards, And Reassignment UX Work
- **Platform requirement:** Users need to actually work the schedule, not just view counts.
- **Verify:**
  - Jobs/work board can filter by craft, date range, status, and branch/location where those fields exist.
  - Resources/people board can filter or group by craft.
  - Assignment workflow can place or reassign a person to a job/work package.
  - Ending-soon jobs or assignments provide a clear path to move people to another job.
  - Shortages and conflicts are visually highlighted, not buried in raw JSON.

### QA-SCHED-008: Coverage Dashboard Compares Demand, Assigned, Available, And Gaps
- **Platform requirement:** Forecasting/dashboard must compare operational demand against actual staffing capacity.
- **Verify:**
  - Coverage output shows demand by craft for the selected date range.
  - Assigned headcount is shown separately from demand.
  - Available resources are shown separately from assigned resources.
  - Gaps/shortages by craft are calculated from demand minus assigned/available context.
  - Jobs/work packages ending soon and resources freeing up soon appear in the same operational dashboard or linked panels.

---

## Actuals / Estimate Delta

### QA-ACT-001: Actuals Capture Feeds Estimate/FCO Delta Reporting
- **Platform requirement:** Planning/Scheduling actuals must compare against estimate/FCO baselines.
- **Verify:**
  - Work package or step actual labor can be recorded.
  - Actual charge uses Rate Book logic where applicable.
  - Actual internal cost uses Cost Book logic where applicable.
  - Delta output shows planned vs actual hours, charge, cost, margin, amount delta, and percent delta.
  - Rate/cost/margin math is not duplicated only in frontend views.

### QA-ACT-002: Lifecycle Actuals Model Captures Cost And Billable Truth
- **Lifecycle requirement:** Actuals are the truth and must capture both internal cost and billable/charged values.
- **Verify:**
  - Lifecycle docs define actual labor hours, labor cost, equipment cost, material cost, subcontract cost, billable labor amount, billable total, invoice-ready/charged values where feasible, actual start/finish, completion, and progress.
  - Actuals require WorkOrder linkage.
  - Actuals optionally link to task/step/substep and FCO where applicable.
  - Rollups are defined for estimate, FCO, project, work order, task, and future estimating feedback.
  - Authorized value vs forecast/actual overrun is treated as a risk condition.

---

## FCO / Change Order

### QA-FCO-001: Signable FCO Document Can Be Generated
- **Platform requirement:** FCO/change work tied to estimates must produce a signable document.
- **Verify:**
  - FCO/change record links to an estimate.
  - Generated document includes project/client info, change number/date, changed scope, reason/basis, schedule impact, cost breakdown, updated value, status, and approval/signature fields.
  - Document can be printed/PDF-ready or otherwise reviewed without exposing internal-only fields unless intended.

### QA-FCO-002: Project Planning Plan Defines FCO Ownership And Integrity
- **Project-planning requirement:** FCOs must be connected to estimates and tasks with clear ownership and validation.
- **Verify:**
  - Project-planning master docs identify whether an FCO concept already exists in the repo.
  - If FCO is not clearly defined, docs label assumptions and propose the implementation approach.
  - FCO links enforce estimate association.
  - Tasks associated with FCOs can be traced back to the estimate.
  - Mismatched task/FCO/estimate relationships are treated as validation defects or review items.

### QA-FCO-003: Lifecycle FCO Model Requires Estimate And WorkOrder Linkage
- **Lifecycle requirement:** FCO / Change Order is controlled business change, not a note.
- **Verify:**
  - FCO belongs to an Estimate.
  - FCO belongs to a WorkOrder.
  - FCO can reference impacted task/step/substep where appropriate.
  - FCO includes description/scope change narrative, reason, commercial adjustment, schedule impact, approval status, author, approver, and dates.
  - Migration strategy for required estimate/work-order links audits/backfills existing nulls and reports unresolved orphans before enforcing NOT NULL.

---

## Project Lifecycle / Stage Gates

### QA-LIFE-001: Lifecycle Master Docs Are Created And Planning-Only
- **Lifecycle requirement:** Claude must create lifecycle operating-model docs before implementation.
- **Verify:**
  - `docs/PROJECT_LIFECYCLE_MASTER_PLAN.md` exists.
  - `docs/PROJECT_DATA_OWNERSHIP_AND_TRACEABILITY.md` exists.
  - `docs/PROJECT_STAGE_GATE_AND_STATUS_MODEL.md` exists.
  - `docs/PROJECT_ACTUALS_AND_VARIANCE_MODEL.md` exists.
  - `docs/PROJECT_PLANNING_MASTER_TODO.md` is updated for lifecycle scope.
  - Docs are grounded in actual repo findings and do not claim coding/source implementation under this planning-only gate.

### QA-LIFE-002: Estimate Authorization Project WorkOrder Chain Is Defined
- **Lifecycle requirement:** Executable work must not float without approved commercial traceability.
- **Verify:**
  - Estimate is defined as the commercial baseline and source of truth for original scope/cost/price/margin assumptions.
  - CommercialAuthorization is flexible enough for PO, signed proposal, contract, notice to proceed, work authorization, or release order.
  - Project is the execution umbrella tied to Estimate.
  - WorkOrder ties to Project, Estimate, and CommercialAuthorization.
  - Model supports one Estimate -> many WorkOrders and one Project -> many WorkOrders.
  - No released WorkOrder without approved Estimate and valid CommercialAuthorization.

### QA-LIFE-003: Step-Out And Work Release Gates Are Defined
- **Lifecycle requirement:** Step-out plans belong under released execution work.
- **Verify:**
  - StepOutPlan belongs to WorkOrder.
  - Traceability chain is Estimate -> CommercialAuthorization -> Project -> WorkOrder -> StepOutPlan -> StepOutStep -> StepOutSubStep.
  - No Step-Out without WorkOrder.
  - StepOutSubStep is included as the MVP leaf level with display numbering such as `1.1` and decimal durations like `.25`, `.5`, `1.5`.

### QA-LIFE-004: Authorized Value Risk Is Modeled
- **Lifecycle requirement:** The platform must protect payment and commercial exposure.
- **Verify:**
  - Docs define authorized value.
  - Forecast/actual exceeding authorized value raises risk.
  - Cost variance and billable variance are separate.
  - Estimate vs actual, FCO vs actual, work order vs actual, and project vs actual rollups are defined.

### QA-LIFE-005: Full Real-World Lifecycle Is Covered
- **Lifecycle requirement:** The model must cover the end-to-end work flow from opportunity to lessons learned.
- **Verify:**
  - Docs cover opportunity/preconstruction/bid intake.
  - Docs cover estimate development and revisions.
  - Docs cover commercial authorization.
  - Docs cover project initiation.
  - Docs cover planning/PM and work release.
  - Docs cover scheduling, execution, monitoring/controlling, change control, actuals/variance, closeout, and lessons learned.

### QA-LIFE-006: Stage Gate And Status Model Is Explicit
- **Lifecycle requirement:** Stage transitions and risk statuses must be clear enough to implement.
- **Verify:**
  - Status model exists for Estimate, Project, WorkOrder, Task/Step/SubStep, and milestone.
  - Stage gates define required prerequisites for release and progression.
  - At-risk threshold defaults to 5 days with optional per-project override.
  - On-track, at-risk, late, complete, and closeout states are defined with calculation or transition rules.
  - PM lifecycle lens is mapped practically: initiating, planning, executing, monitoring/controlling, closing.

### QA-LIFE-007: Developer Handoff Docs Are Canonical And Reconciled
- **Lifecycle requirement:** The doc package must be safe to hand to developers without contradictory sources of truth.
- **Verify:**
  - `docs/PROJECT_HANDOFF_INDEX.md` exists.
  - The handoff index names the final approved reading order.
  - The handoff index identifies the canonical source of truth per topic.
  - `docs/PROJECT_PLANNING_MASTER_PLAN.md` is updated, archived, or clearly marked superseded.
  - `docs/PROJECT_PLANNING_DATA_MODEL.md` is updated, archived, or clearly marked superseded.
  - The `ProjectPlan` vs `Project` model conflict is resolved.
  - The `TaskActual` vs `ActualEntry` model conflict is resolved.
  - The Gantt implementation contradiction is resolved.
  - The `StepOutSubStep` MVP-vs-post-MVP contradiction is resolved in favor of the current approved lifecycle requirement unless Joseph explicitly changes it.
  - No stale doc can reasonably be treated as equal truth to `PROJECT_LIFECYCLE_MASTER_PLAN.md` and `PROJECT_DATA_OWNERSHIP_AND_TRACEABILITY.md`.

---

## Adding New Items

When a new bug is found and fixed:
1. Assign the next ID in the relevant domain prefix sequence.
2. Fill out: Past failure, Fixed (brief), Verify steps.
3. The fix owner adds the entry — not the testing agent.
4. On the next agent run, the new item must produce PASS.

---

## PM + Scheduling Full Sweep Baseline - 2026-04-29

Evidence: `docs/QA_EVIDENCE_20260429_PM_SCHED_FULL.md`

| ID | Check | Current Status | Evidence / Notes |
|----|-------|----------------|------------------|
| QA-PLAT-011 | QA automation must target Project-app ports, not old repo ports. | FAIL | Playwright config targets `7210/7211`; port inspection showed old repo owns those ports. Project-app owns `7310/7311`. |
| QA-UI-010 | PM/Scheduling route audit must assert visible content and screenshots on Project-app. | FAIL | Prior smoke passed with blank screenshots and wrong ports; new audit has real screenshots and findings. |
| QA-PLAT-004 | `/planning` and `/scheduling` module roots load visible app shells. | FAIL | Root redirects resolved to `/projects` and `/dashboard`, near-blank pages. |
| QA-PLAN-011 | Planning root route redirects to `/planning/projects`. | FAIL | Evidence: `planning.png`. |
| QA-SCHED-010 | Scheduling root route redirects to `/scheduling/dashboard`. | FAIL | Evidence: `scheduling.png`. |
| QA-UI-011 | PM + Scheduling tables use Work Orders dense row baseline. | FAIL | Step-Out, Work Packages, FCO, Jobs, Resources, Assignments, Roll-Off show 65-68px rows vs Work Orders 46px. |
| QA-PLAN-012 | Planning list rows are compact, one-line, and ellipsized. | FAIL | Step-Out, Work Packages, and FCO fail row-height target. |
| QA-SCHED-011 | Scheduling list rows are compact, one-line, and ellipsized. | FAIL | Jobs, Resources, Assignments, Roll-Off fail row-height target. |
| QA-SCHED-012 | Jobs Board shows Forecast/Released demand state per row. | FAIL | Jobs Board shows source/status labels but no locked demand-state badges. |
| QA-SCHED-013 | Assignment create/update rejects Forecast/unreleased demand server-side. | FAIL | Logic audit found assignment endpoint accepts source/id without released demand validation. |
| QA-SCHED-014 | Scheduling assignments use controlled craft reference, not free-text. | FAIL | UI and model still use `craftCode` text patterns. |
| QA-PLAN-013 | WorkPackage list/detail use canonical package numbers. | FAIL | Detail can display raw `WP-{packageId}` and list does not prove WP number column. |
| QA-UI-012 | WorkPackage Ready toggle does not trigger row navigation. | NOT VERIFIED / RISK | Toggle sits inside clickable row; needs event-stop verification after fix. |
| QA-PLAN-014 | New Step-Out Plan route opens a create form or is removed. | FAIL | `/planning/step-out-plans/new` redirects back to list. |
| QA-FCO-003 | FCO create/update requires Estimate + WorkOrder traceability. | FAIL | FCO links remain nullable and controller does not hard-gate. |
| QA-LIFE-003 | WorkOrder release/status transitions enforce lifecycle gates. | FAIL | Status endpoint can bypass release gate. |
| QA-EST-020 | Awarded/linked estimates are protected from casual mutation. | OPEN / JOSEPH DECISION REQUIRED | Fix requires touching frozen Estimating endpoints, so Joseph approval is required before implementation. |

---

## Architecture / UI System Guardrail Lane - Added 2026-04-29

These checks are mandatory for the `refactor/pm-scheduling-foundation` cleanup branch and every PM/Scheduling batch after it. Codex/testers must treat failures as defects, not suggestions.

| ID | Check | Current Status | Evidence / Notes |
|----|-------|----------------|------------------|
| QA-ARCH-009 | PM/Scheduling pages use the shared UI system for page shell, header, filter bar, status tag, table/list, detail shell/card, row actions, and empty state. | NEW / REQUIRED | Work Orders dense list remains the density baseline until replaced by an explicit shared standard. Complements existing `QA-ARCH-005` through `QA-ARCH-007`. |
| QA-ARCH-010 | Dev seeding/bootstrap orchestration lives in services/bootstrap layers, not a monolith `DevController`. | NEW / REQUIRED | Seed/reset behavior must remain idempotent and environment-safe. |
| QA-ARCH-011 | Architecture guardrail docs and tester rules are updated before cleanup is called done. | NEW / REQUIRED | Cleanup is incomplete unless docs, regression checks, and tester/Codex rules reflect the permanent architecture rules. |
| QA-UI-013 | Shared date/currency/status display rules are consistent across PM and Scheduling. | NEW / REQUIRED | Tables/lists: `MMM d, yyyy`; detail views: `MMMM d, yyyy`; 4-digit year always. |
| QA-UI-014 | PM and Scheduling route-to-route visual consistency remains intact after refactors. | NEW / REQUIRED | Playwright screenshot pass must cover page shell, density, filter bars, row actions, badges, empty states, drawers/modals. |
| QA-BE-001 | Controllers remain thin transport layers after cleanup. | NEW / REQUIRED | Business logic belongs in services/handlers; WorkOrder financial rollups must be service-owned. |
| QA-BE-002 | `WorkOrderFinancialService` or equivalent owns work-order financial calculations. | NEW / REQUIRED | No inline financial rollup logic in `WorkOrderController`. |
| QA-BE-003 | Planning/Scheduling services preserve lifecycle gates and personnel-only scheduling boundaries. | NEW / REQUIRED | Scheduling remains personnel-only; Gantt/timeline remain Planning-only; Forecast demand remains not assignable. |
| QA-ARCH-012 | DevController is thin and seed/reset orchestration is service-owned after Batch 6. | NEW / REQUIRED | Joseph rejected the branch as complete while `DevController` remains a monolith. This must become hard-fail after extraction. |
| QA-ARCH-013 | PM/Scheduling service layer is split by feature/domain, not two giant wrappers. | NEW / REQUIRED | Required service split includes projects, work orders, work packages, FCOs, resources, assignments, coverage, and demand where applicable. |
| QA-ARCH-014 | Warn-mode architecture checks become hard-fail when their cleanup batch is complete. | NEW / REQUIRED | Permanent warn mode is not accepted. |
| QA-ARCH-015 | Cleanup branch is not accepted until Joseph's updated definition of done is fully evidenced. | NEW / REQUIRED | Batches 1-5 are partial progress only. |
| QA-UI-015 | Required shared UI primitives exist and are used by PM/Scheduling views. | NEW / REQUIRED | `ModulePageShell`, `ModulePageHeader`, `ModuleFilterBar`, `ModuleStatsStrip`, `AppStatusTag`, `AppDateValue`, `AppCurrencyValue`, `AppEmptyState`, `RowActionGroup`, `DetailCard`, `MetaGrid`. |
| QA-UI-016 | PM/Scheduling views do not retain route-by-route homemade shell/filter/header/card CSS islands. | NEW / REQUIRED | Views should orchestrate only and use shared UI structure. |

### Architecture Guardrail Retest Minimum

After each Claude cleanup batch, Codex/testers must run or manually verify:

- `git status --short --branch`
- backend build
- frontend build
- architecture-check script once it exists
- targeted grep/static scan for `useApiStore`, `apiStore.api`, local formatter/status helpers, raw EF `[FromBody]` entity patterns, and inline `modelBuilder.Entity`
- Playwright PM/Scheduling route audit against Project-app ports `7310/7311`, not old repo ports `7210/7211`
- screenshot comparison for PM/Scheduling density and visual consistency
