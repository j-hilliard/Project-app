# Live QA TODO

This is the living demo-readiness scoreboard. Update it whenever testing finds a defect, a fix lands, or a checked item proves incomplete.

## Rules

- This file is a coordination board, not automatic permission to change code.
- **Closure authority:** Only Joseph or Codex may check off live TODO items as complete. Claude may add implementation notes and may claim "ready for verification," but must not close an item unless the note explicitly says `Joseph waived this requirement` or `Joseph accepted this as-is`, with date/rationale/evidence.
- **Claude self-certification rule:** If Claude checks an item without Joseph/Codex verification, Codex must uncheck it during the next audit and add `Reopened reason: checked by implementer without independent verification`.
- **Implementation vs verification split:** Claude is treated as the implementer. Codex is the QA/orchestration owner responsible for audit sweeps, screenshots, evidence packets, regression checklist updates, and final TODO state.
- An open item means `investigate/decide`, not `fix immediately`.
- If Joseph accepts a behavior as-is, move the item to `Accepted As-Is` with the decision, rationale, date, and any evidence. Do not keep re-reporting accepted behavior as a defect unless new evidence changes the risk.
- Do not mark an item complete until it has evidence: file paths, screenshots, build/test output, or explicit manual verification notes.
- If Joseph explicitly marks an item complete after personally testing it, treat it as complete and do not re-audit it unless Joseph asks, a related code change lands, or new evidence shows a regression.
- If an item is checked but later fails verification, uncheck it immediately and add why under `Reopened reason`.
- Every fix that lands here must also have a matching regression checklist item in `docs/QA_REGRESSION_CHECKLIST.md`.
- Every subagent audit must read this file and report whether each item relevant to its lane is `OPEN`, `DONE`, `BLOCKED`, `REOPENED`, or `ACCEPTED AS-IS`.
- Do not delete old items. Move them to `Completed` only after verification.
- Going forward, every Codex audit sweep must create a Claude-readable evidence packet:
  - screenshots copied under `docs/qa-evidence/<sweep-id>/`
  - a handoff markdown file under `docs/` with exact screenshot paths
  - each finding written granularly: what is wrong, why it is wrong, likely files, how to fix, and regression test
  - no vague "looks wrong" findings without visible/code evidence
- Platform expansion work must follow `docs/PLATFORM_EXPANSION_MASTER_CONTRACT.md`. That document is the stable Claude-facing implementation/audit contract for Portal + Planning + Scheduling.
- **Requirements intake rule:** Joseph's requirements are the source of truth. Before Codex verifies or closes work, new requirements must be translated into live TODO acceptance gates with expected behavior, likely files, verification evidence, and matching regression checklist IDs.
- **Traceability rule:** Claude planning docs, Claude TODOs, and implementation claims do not become QA truth automatically. Codex must compare them against Joseph's pasted requirements and this live QA board, then add/reopen TODO items for any missing requirement, mismatch, or unverifiable claim.
- **Verification sequence:** Requirements first, TODO/checklist update second, implementation audit third, screenshots/API/build evidence fourth, checkbox closure last.

## Open Blockers

### P0-001: Status Vocabulary Must Be Consistent End-To-End

- [ ] Fix status storage/display mismatch.
- **Problem:** Frontend uses/display-patches `Submitted for Approval`, while backend status patch currently accepts `Submitted`; DB status column is max length 20, so the 22-character display label is unsafe as a stored value.
- **Impact:** Submit for Approval can fail during demo, and analytics/AI/manpower can classify submitted work inconsistently.
- **Likely files:** `Api/Controllers/EstimatesController.cs`, `Data/AppDbContext.cs`, `Data/Models/Estimate.cs`, `webapp/src/modules/estimating/features/estimate-form/views/EstimateFormView.vue`, `webapp/src/modules/estimating/features/estimate-list/views/EstimateListView.vue`, `webapp/src/modules/estimating/features/analytics/utils/forecast.ts`, `webapp/src/modules/estimating/stores/aiChatStore.ts`.
- **Expected behavior:** Pick one persisted status value and one display label strategy. Recommended: store `Submitted`, display `Submitted for Approval`.
- **Verification required:** API patch accepts chosen workflow value/mapping; list badge/filter works; Revenue Forecast/manpower/AI normalize submitted estimates correctly.
- **Regression checklist:** `QA-EST-004`, `QA-EST-009`, `QA-EST-013`, `QA-SP-004`.
- **Latest verification:** 2026-04-26 Codex resweep. Improved but not closed. `Data/AppDbContext.cs` now allows status length 30, and `Api/Controllers/EstimatesController.cs` accepts both `Submitted` and `Submitted for Approval`. However visible seeded rows still store/display `Submitted`, while the estimate list filter offers only `Submitted for Approval` and list filtering is exact-match server-side. `statusSeverity()` also lacks a `Submitted` mapping. Submitted rows can still be filtered/displayed inconsistently.
- **Reopened reason:** N/A.

### P0-002: Submit For Approval Must Be A Real Workflow

- [ ] Complete Submit for Approval flow.
- **Problem:** Button exists, but flow is not fully reliable until status backend contract is fixed and customer-review/send behavior is validated.
- **Impact:** Demo-critical workflow can look correct but fail at final status update.
- **Likely files:** `webapp/src/modules/estimating/features/estimate-form/views/EstimateFormView.vue`, `Api/Controllers/EstimatesController.cs`, `Api/Services/ProposalPdfService.cs`.
- **Expected behavior:** Save latest estimate, generate/open customer-safe PDF, user reviews/sends or confirms, status changes automatically.
- **Verification required:** PDF excludes internal cost/margin; status does not change before confirmation; after confirmation status updates in header and list.
- **Regression checklist:** `QA-EST-003`, `QA-EST-007`, `QA-EST-008`, `QA-EST-009`.
- **Reopened reason:** N/A.

### P0-003: Estimate List Needs Award/Lost/Revision Workflow Actions

- [ ] Add and verify list-page workflow actions.
- **Problem:** Estimate list still only exposes Edit, Clone as Scenario, Delete. Award/Won, Lost/Declined, and Create Revision are not available as business actions.
- **Impact:** Users must open/edit records or use manual/backend status paths instead of a smooth workflow.
- **Likely files:** `webapp/src/modules/estimating/features/estimate-list/views/EstimateListView.vue`, `Api/Controllers/EstimatesController.cs`, `Api/Controllers/RevisionsController.cs`.
- **Expected behavior:** Submitted/Pending rows can be marked Awarded; eligible rows can be marked Lost with required reason; revisions are deliberate and auditable.
- **Verification required:** Actions appear only for appropriate statuses; status changes persist; reports update; no real data is mutated except approved `QA_TEST_<timestamp>` records.
- **Regression checklist:** `QA-EST-010`, `QA-EST-011`, `QA-EST-012`.
- **Reopened reason:** N/A.

### P0-004: Lost Reason Must Be Enforced Server-Side And Through AI/API

- [ ] Require lost reason for all Lost/Declined transitions.
- **Problem:** UI can require a reason, but API/AI status patch can still mark Lost without one unless guarded server-side.
- **Impact:** Audit trail is incomplete and reports cannot explain lost work.
- **Likely files:** `Api/Controllers/EstimatesController.cs`, `webapp/src/modules/estimating/stores/aiChatStore.ts`, `Api/Services/AiService.cs`.
- **Expected behavior:** Any transition to Lost/Declined requires a reason, regardless of source.
- **Verification required:** Empty lost reason is rejected; valid reason succeeds; AI action cannot bypass this on real records.
- **Regression checklist:** `QA-EST-011`, `QA-EST-013`.
- **Latest verification:** 2026-04-26 Codex resweep. Backend now rejects `Lost` when `LostReason` is missing in `Api/Controllers/EstimatesController.cs`, and the estimate list Lost dialog disables confirm until a reason is selected. Still needs live API/AI workflow proof before closing.
- **Reopened reason:** N/A.

### P0-005: Revenue Forecast Avg Margin / Future Profit Must Use Correct API Shape

- [x] Fix Avg Margin and Future Profit source.
- **Completed date:** 2026-04-26 (session 2).
- **Fix applied:** `RevenueForecastView.vue` line ~705 now reads `analyticsResp.data?.kpis?.avgMarginPct` (was `analyticsResp.data?.avgMarginPct`). Build passed. Codex screenshots showing FAIL were captured BEFORE this fix landed.
- **Verification required:** Live browser test — AVG MARGIN card must show a non-zero % and FUTURE PROFIT must equal Future Revenue × that margin when staffing plans exist for the year.
- **Regression checklist:** `QA-AN-002`.
- **Latest verification:** PASS in live browser. Screenshot `docs/qa-evidence/QA_AUDIT_20260426_TODO_RESWEEP/revenue-forecast.png` shows `AVG MARGIN 31%`, `FUTURE REVENUE $5.7M`, and `FUTURE PROFIT $1.8M`.
- **Do not re-flag unless:** AnalyticsController response shape changes or the fix is reverted.

### P0-006: Revenue Forecast KPI And Chart Totals Must Reconcile

- [ ] Make KPI cards and chart use the same business definitions.
- **Problem:** Pending KPI uses raw totals while chart pipeline can be confidence-weighted; ALL/year/future-period windows can also diverge.
- **Impact:** Dashboard tells different stories in the same screen, especially QTD/Q2/YTD/ALL.
- **Likely files:** `webapp/src/modules/estimating/features/analytics/views/RevenueForecastView.vue`, `webapp/src/modules/estimating/features/analytics/utils/forecast.ts`.
- **Expected behavior:** One top-level period filter controls both KPIs and chart. Q1/Q2/Q3/Q4/QTD/YTD/ALL render correct labels and reconcile numerically.
- **Verification required:** For MTD, Q1, Q2, Q3, Q4, QTD, YTD, ALL, record KPI values and chart sums; differences must be explained by labels, not hidden logic.
- **Regression checklist:** `QA-AN-007`, `QA-AN-010`, `QA-AN-011`, `QA-AN-013`, `QA-AN-015`, `QA-AN-016`.
- **Latest verification:** 2026-04-26 Codex resweep. Improved but still open. Screenshot `docs/qa-evidence/QA_AUDIT_20260426_TODO_RESWEEP/revenue-forecast.png` shows one top-level Period dropdown, no separate chart filter, and separate chart series for `Pipeline` and `Staffing Pipeline`. Numeric reconciliation across every period was not completed in this sweep, so do not close.
- **Reopened reason:** N/A.

### P0-010: Boss Demo Acceptance - Realistic Demo Data Must Cover 2025, 2026, And 2027

- [ ] Verify demo data supports last year, current year, and 2027 forecast scenarios.
- **Problem:** Boss requires realistic-looking historical data for last year, current year, and staffing plans filled out for 2027. Current seed evidence is mostly 2026-focused; 2027 staffing and 2025 history have not been verified.
- **Impact:** Boss-demo critical. Analytics, AI Q&A, and forecasts cannot demonstrate year-over-year and future planning without credible data.
- **Likely files/data:** SQL Express demo data, `Api/Controllers/DevController.cs`, `Data` seed scripts, Revenue Forecast, Manpower Forecast.
- **Expected behavior:** 2025 has completed historical estimates/jobs with revenue; 2026 has active/pending/awarded/lost estimates; 2027 has future staffing plans with labor rows and non-zero forecast values.
- **Verification required:** Read-only API/DB reconciliation showing record counts and totals by year/status/source; screenshots of 2025, 2026, and 2027 forecast pages.
- **Regression checklist:** `QA-DATA-001`, `QA-DATA-002`, `QA-AN-015`, `QA-AN-016`.
- **Reopened reason:** N/A.

### P0-011: Boss Demo Acceptance - KPI Drilldown To Source Invoice/Estimate

- [ ] Verify KPI cards can drill into the records behind the number.
- **Problem:** Boss requirement says KPI should accurately show values and allow drill-down to an individual invoice/source record if needed. Current Revenue Forecast KPI/chart reconciliation is already open, and source drilldown has not been verified.
- **Impact:** Boss-demo critical. A KPI number without traceability is hard to defend in a demo.
- **Likely files:** `webapp/src/modules/estimating/features/analytics/views/RevenueForecastView.vue`, estimate list/detail routes, proposal/PDF flow.
- **Expected behavior:** Clicking or otherwise drilling from a KPI/category reveals contributing estimates/staffing plans/invoices with enough detail to reconcile totals and open an individual source record/PDF where available.
- **Verification required:** For at least Awarded Revenue, Pending Pipeline, and Future Revenue, show the KPI value, drilldown records, summed total, and opening one source estimate/proposal.
- **Regression checklist:** `QA-AN-018`, `QA-AN-019`.
- **Latest verification:** 2026-04-26 Codex resweep. Partial. Screenshot `docs/qa-evidence/QA_AUDIT_20260426_TODO_RESWEEP/revenue-awarded-drilldown.png` shows Awarded Revenue drilldown with contributing estimate rows. Future Revenue is still not clickable/drillable to staffing plan source records, so the boss requirement remains partial.
- **Reopened reason:** N/A.

### P0-012: Three-Dot Row Menu Must Work Reliably

- [x] Fix list action visibility/rendering and fallback navigation.
- **Problem:** Workflow actions are moving into a three-dot menu. If the PrimeVue `Menu` is nested inside table/card rows or clipped by containers, actions can become invisible or unusable.
- **Impact:** Demo-critical. Award/Lost/Revision/Submit actions may appear missing even after being implemented.
- **Likely files:** `webapp/src/modules/estimating/features/estimate-list/views/EstimateListView.vue`, `webapp/src/modules/estimating/features/staffing-list/views/StaffingListView.vue`.
- **Expected behavior:** Estimate list uses the three-dot menu rendered outside clipping containers with fallback `Open`. Staffing list uses hover-revealed card actions; Joseph verified that hover is the intended way to reveal them.
- **Verification required:** Estimate row menu opens and is not clipped; staffing card actions appear on hover; both paths provide a way to open the record.
- **Regression checklist:** `QA-UI-006`.
- **Latest verification:** 2026-04-26 Codex resweep. Partial. Estimate list row menu works and includes fallback `Open`; evidence: `docs/qa-evidence/QA_AUDIT_20260426_TODO_RESWEEP/estimate-row-menu-open-1600.png`. Staffing list does not have a three-dot/dropdown menu (`ellipsis 0` in Playwright); it still uses hover-only card buttons. Evidence: `docs/qa-evidence/QA_AUDIT_20260426_TODO_RESWEEP/staffing-menu-check.png`.
- **Joseph verification:** 2026-04-26 Joseph confirmed staffing list actions are verified and intentionally shown on hover. Do not fail staffing list solely for lacking a three-dot menu.
- **Do not re-flag unless:** Estimate menu stops opening, estimate menu is clipped, staffing hover actions stop appearing, or there is no fallback way to open a record.
- **Reopened reason:** N/A.

### P0-013: LaborGrid Sticky TOTAL Column Must Stay Visible And Aligned

- [ ] Fix sticky right-side TOTAL column in `LaborGrid`.
- **Problem:** The labor grid is horizontally scrollable and the TOTAL column is a key demo value. If it unsticks, clips, misaligns, or disappears, users cannot trust totals while entering crews/schedules.
- **Impact:** Demo-critical for both estimate and staffing plan forms.
- **Likely files:** `webapp/src/modules/estimating/features/estimate-form/components/LaborGrid.vue`, related CSS.
- **Expected behavior:** TOTAL column remains fixed on the right while horizontal scrolling, aligns with each labor row and the total footer, and does not overlap delete/actions or rate columns. Row trash can remains available, but is hidden until the row is hovered.
- **Verification required:** Screenshots at left scroll, middle scroll, and far-right scroll for an estimate and a staffing plan with multiple labor rows/days.
- **Regression checklist:** `QA-LG-003`.
- **Latest implementation:** 2026-04-27 Codex removed the separate fixed rail and restored one uniform table layout for `All` and week views. The right-side ST/OT/DT/ST$/OT$/DT$/TOTAL/delete block uses the same sticky table-column structure in both modes, and the row trash icon is hidden until row hover. `npm run build:dev` passed.
- **Reopened reason:** N/A.

### P0-014: Crew Template Dialog Must Have Empty State And Demo Templates

- [ ] Add crew-template empty state and ensure demo seed includes at least 3 usable crew templates.
- **Problem:** AI/UX demo depends on loading crew templates. If no templates exist or the dialog is blank, the user sees a dead workflow.
- **Impact:** Demo-critical for AI crew assembly and manual staffing/estimate creation.
- **Likely files:** `webapp/src/modules/estimating/features/staffing-form/views/StaffingFormView.vue`, `webapp/src/modules/estimating/features/estimate-form/views/EstimateFormView.vue`, `Api/Controllers/DevController.cs`.
- **Expected behavior:** Crew template dialog shows a helpful empty state when none exist; seeded demo data contains at least 3 realistic crew templates with positions/quantities; applying a template produces labor rows with rates when a rate book is loaded.
- **Verification required:** Screenshot dialog with templates; verify at least 3 seeded templates; apply one to estimate and one to staffing plan; verify labor rows and totals.
- **Regression checklist:** `QA-SP-006`, `QA-UI-007`, `QA-DATA-003`.
- **Latest verification:** 2026-04-26 Codex resweep. Partial. Screenshot `docs/qa-evidence/QA_AUDIT_20260426_TODO_RESWEEP/crew-templates-light.png` shows 3 seeded templates: Boilermaker Crew, Standard Piping Crew, and Turnaround Package Crew. Applying a template to estimate and staffing plan was not live-verified in this sweep.
- **Reopened reason:** N/A.

### P0-015: Exact Demo Seed Coverage For 2025 Estimates And 2027 Staffing Plans

- [ ] Seed or verify 25 realistic 2025 estimates and 10 realistic 2027 staffing plans.
- **Problem:** Broad demo-data coverage is already required, but tomorrow's boss list specifies exact seed targets: 25 estimates for 2025 and 10 staffing plans for 2027.
- **Impact:** Demo-critical. Historical AI Q&A, revenue KPIs, and future manpower forecast are weak without enough believable records.
- **Likely files/data:** `Api/Controllers/DevController.cs`, SQL Express demo data, Revenue Forecast, Manpower Forecast.
- **Expected behavior:** 2025 includes 25 realistic estimates with statuses, dates, labor rows, summaries/revenue, clients/locations, and enough variety for historical Q&A. 2027 includes 10 future staffing plans with labor rows, non-zero Rough Labor, and forecast impact.
- **Verification required:** Read-only count and total by year/source/status; screenshots for 2025 Revenue Forecast and 2027 staffing/manpower forecast. Do not reset/seed live SQL Express without Joseph approval.
- **Regression checklist:** `QA-DATA-001`, `QA-DATA-002`, `QA-DATA-004`.
- **Reopened reason:** N/A.

### P1-007: Regression Test Suite Must Avoid Mutating Demo Data By Default

- [ ] Add safe mocked regression tests and isolate live tests.
- **Problem:** Existing Playwright global setup and live specs can seed/create/update records, which is unsafe before demo unless explicitly approved.
- **Impact:** A broad test run can alter demo data.
- **Likely files:** `webapp/tests/e2e/globalSetup.ts`, `webapp/tests/e2e/live-*.spec.ts`, `webapp/package.json`.
- **Expected behavior:** Mocked tests can run without DB mutation; live tests require explicit `QA_TEST_<timestamp>` approval.
- **Verification required:** Document safe commands; prove no seed/reset/delete endpoints are called in mocked test mode.
- **Regression checklist:** Add/verify tests for `QA-EST-005` through `QA-EST-013` and `QA-AN-010` through `QA-AN-016`.
- **Reopened reason:** N/A.

### P1-008: Clean Up Stale Or Contradictory Checklist Language

- [ ] Remove checklist contradictions that tell agents to test old behavior.
- **Problem:** Some old entries still mention selecting/saving status directly or old chart expectations.
- **Impact:** Agents may test the wrong target and claim broken workflow is correct.
- **Likely files:** `docs/QA_REGRESSION_CHECKLIST.md`, `docs/SUBAGENT_DEMO_AUDIT_BRIEF.md`.
- **Expected behavior:** Checklist consistently describes status as read-only workflow and chart as top-period-driven.
- **Verification required:** Search checklist for stale wording like manual status setting and conflicting QTD/YTD expectations.
- **Regression checklist:** This document plus `QA-EST-004` through `QA-EST-013`, `QA-AN-009` through `QA-AN-016`.
- **Reopened reason:** N/A.

## Platform Expansion Control Items

These items govern the Portal / Planning / Scheduling expansion. They are not permission for Codex to implement app code; they are the audit gates Codex will use to manage Claude's implementation work.

### PLATFORM-PHASE-0: Claude Phase 0 Analysis / Research / Docs

- [ ] Verify Phase 0 is complete.
- **Status:** Claude started Phase 0. Awaiting implementation notes, changed files, and evidence.
- **Phase 0 expected output:** Actual repo inspection, corrected repo-path assumptions, architecture doc, domain research notes, live implementation todo, implementation worklog, regression checklist, test run log, bootstrap/migration/seed summary, test setup summary, estimating/staffing/rate/cost model summary, and phased implementation plan.
- **Do not close until Codex verifies:**
  - `docs/PORTAL_PLANNING_SCHEDULING_ARCHITECTURE.md` exists and reflects actual repo paths.
  - `docs/DOMAIN_RESEARCH_NOTES.md` exists and covers scheduling, planning, actuals/delta, and FCO document expectations.
  - `docs/LIVE_TODO.md` exists and is current for Claude's implementation work.
  - `docs/IMPLEMENTATION_WORKLOG.md` exists and records commands/files/decisions.
  - `docs/REGRESSION_CHECKLIST.md` exists or Claude explicitly maps to `docs/QA_REGRESSION_CHECKLIST.md`.
  - `docs/TEST_RUN_LOG.md` exists and records discovered test/build commands, even if none have run yet.
  - Claude documents actual locations for `Api/Program.cs`, `Data/AppDbContext.cs`, `Data/Migrations`, `Data/DBInitializer.cs`, `Data/DesignTimeContextFactory.cs`, `webapp/src/apps.ts`, `webapp/src/router/index.ts`, and existing test/e2e/build setup.
  - No app source code is marked complete solely from analysis.
- **Codex verification required:** Read Claude's changed docs, compare against actual repo structure, and report PASS/FAIL with file paths.
- **Regression checklist:** `QA-PLAT-001`, `QA-PLAT-003`, `QA-BOOT-001`.
- **Latest Codex spot-check:** 2026-04-28. Phase 0 is not closeable yet. `docs/IMPLEMENTATION_WORKLOG.md` incorrectly says "No test projects, no design-time factory" even though `Data/DesignTimeContextFactory.cs` exists. `git status --short` also showed `webapp/src/apps.ts`, `webapp/src/router/index.ts`, and `webapp/src/modules/portal/` changed, but no `Api/Controllers/PortalController.cs`, `webapp/src/modules/planning/`, or `webapp/src/modules/scheduling/` visible yet despite the worklog claiming those were created. Build/test commands are still pending.
- **Reopened reason:** N/A.

### PLATFORM-001: Claude Must Follow The Platform Master Contract

- [ ] Verify Claude's implementation matches the platform master contract.
- **Problem:** Platform work can easily drift into generic scheduling screens, invented repo paths, or mixed domain ownership if the implementation prompt is not anchored in the repo and business rules.
- **Expected behavior:** Claude reads `docs/PLATFORM_EXPANSION_MASTER_CONTRACT.md` before implementation, uses actual repo paths, keeps Estimating/Planning/Scheduling/Portal boundaries separate, and records work in the required docs.
- **Verification required:** Codex checks changed files against the contract after each Claude chunk and records any violations as live TODO items.
- **Regression checklist:** `QA-PLAT-001`, `QA-PLAT-002`, `QA-BOOT-001`, `QA-SCHED-001`, `QA-SCHED-002`.
- **Reopened reason:** N/A.

### PLATFORM-002: Platform TODO Closure Requires Joseph Or Codex Verification

- [ ] Enforce closure authority on all platform-expansion tasks.
- **Problem:** If Claude marks its own TODOs complete, the project loses independent QA and Joseph becomes the middle man again.
- **Expected behavior:** Claude may write `Ready for Codex verification`, but checkboxes remain open until Joseph or Codex verifies with evidence. If Joseph waives a requirement, Claude must quote that waiver and Codex must move it to `Accepted As-Is`.
- **Verification required:** Codex audits all newly checked platform/TODO items and reopens any self-certified items without independent evidence.
- **Regression checklist:** `QA-PLAT-003`.
- **Reopened reason:** N/A.

### PLATFORM-003: Platform Expansion Must Not Regress Existing Estimating Demo

- [ ] Verify Estimating remains intact while Portal/Planning/Scheduling are added.
- **Problem:** A new portal/scheduling app can break existing estimating routes, AI sidebar context, staffing plans, rate/cost book workflows, or demo data.
- **Expected behavior:** `/estimating` routes still load; estimate/staffing workflows still pass existing demo regressions; cost books/rate books are not deleted or replaced; AI demo gates remain accepted unless fresh evidence fails.
- **Verification required:** Codex runs or documents safe regression checks after each platform chunk, with screenshots where UI changes are visible.
- **Regression checklist:** `QA-PLAT-004`, plus existing `QA-EST`, `QA-SP`, `QA-RCB`, `QA-AI`, and `QA-AN` gates.
- **Reopened reason:** N/A.

### PLATFORM-004: Phase 1 Portal Shell And App Registration Must Build

- [ ] Verify Phase 1 portal/app-shell implementation.
- **Problem:** Claude can update `apps.ts` and router imports before creating the actual module shells, leaving the frontend broken even though the TODO looks checked.
- **Expected behavior:** `/` loads the portal/dashboard; portal has visible Estimating, Planning / PM, and Scheduling entry cards; `/estimating` still works; `/planning` loads a Planning shell; `/scheduling` loads a Scheduling shell; frontend build passes with no missing module imports.
- **Likely files:** `webapp/src/apps.ts`, `webapp/src/router/index.ts`, `webapp/src/modules/portal/**`, `webapp/src/modules/planning/**`, `webapp/src/modules/scheduling/**`.
- **Verification required:** `npm`/frontend build result, screenshots for `/`, `/estimating`, `/planning`, `/scheduling`, and code inspection proving app registry entries match real routes.
- **Regression checklist:** `QA-PLAT-002`, `QA-PLAT-004`, `QA-PLAN-001`, `QA-SCHED-004`.
- **Latest Codex spot-check:** 2026-04-28. `webapp/src/router/index.ts` imports `@/modules/planning/router` and `@/modules/scheduling/router`, but `git status --short` only showed `webapp/src/modules/portal/` as untracked. Do not close until missing modules exist and the frontend build passes.
- **Reopened reason:** N/A.

### PLATFORM-005: Phase 1 Portal Dashboard Endpoint Must Exist And Stay Thin

- [ ] Verify backend portal summary endpoint.
- **Problem:** The portal should consume backend read models, not duplicate domain logic in Vue. Claude's worklog claims `Api/Controllers/PortalController.cs` exists, but the file was not visible in the current git status spot-check.
- **Expected behavior:** `GET /api/v1/portal/dashboard` exists, compiles, respects existing auth/company conventions, and returns a thin summary read model without mutating estimating/planning/scheduling records.
- **Likely files:** `Api/Controllers/PortalController.cs`, any portal service/read-model files Claude adds, `Api/Program.cs` only if service registration is needed.
- **Verification required:** Backend build, endpoint code review, and browser/network/API evidence once the app is running.
- **Regression checklist:** `QA-PLAT-002`, `QA-PLAT-004`.
- **Latest Codex spot-check:** 2026-04-28. Claimed in `docs/IMPLEMENTATION_WORKLOG.md`, but not visible in `git status --short`. Treat as unverified.
- **Reopened reason:** N/A.

### PLATFORM-006: Phase 2 EF Bootstrap Cleanup Must Use Real Data Project Paths

- [ ] Verify migration-based bootstrap and seed split.
- **Problem:** The repo uses top-level `Data/`, not `Api/Data/`. Bootstrap cleanup can easily break startup or migrations if Claude edits the wrong project or ignores `Data/DesignTimeContextFactory.cs`.
- **Expected behavior:** Startup uses a clear migration-based bootstrap service; no real lifecycle uses `EnsureCreated`; reference seed and demo seed are separated and idempotent; config flags control auto-migrate/reference seed/demo seed; design-time factory is preserved/updated.
- **Likely files:** `Api/Program.cs`, `Data/AppDbContext.cs`, `Data/DBInitializer.cs`, `Data/DesignTimeContextFactory.cs`, new `Data` bootstrap/seeder classes, config files.
- **Verification required:** Backend build, code inspection, documented config behavior, and rerun-safe seed proof without wiping demo data.
- **Regression checklist:** `QA-BOOT-001`, `QA-PLAT-001`.
- **Latest Codex check:** 2026-04-28 recovery pass. Root cause for the visible empty Quote Log was that port `7211`, which `webapp/.env` expects to be the .NET API, was owned by a `node.exe` process instead of the API. Codex stopped the wrong listener, started the .NET API with the `https` launch profile, confirmed it listened on `https://localhost:7211` and connected to SQL, then ran additive `POST /api/v1/dev/seed`. Seed returned `200` with "Seed complete"; read-only authenticated checks returned `43` CSL estimates and `12` CSL staffing plans. Still open: `Program.cs` currently ignores the new `Database:AutoMigrateOnStartup` config value and runs the bootstrapper whenever a SQL connection string exists unless `SkipDatabaseInitialization=true`; `DemoDataSeeder` only seeds users while the richer demo seed still lives in `DevController`.
- **Reopened reason:** N/A.

### PLATFORM-007: Phase 3 Planning Must Be Real Step-Out / Work-Package Planning

- [ ] Verify Planning foundation.
- **Problem:** Planning must not become a generic notes page or a thin placeholder route.
- **Expected behavior:** Planning has real step-out plan entities/endpoints/UI with source links, step codes like `1`, `1.2`, `1.5`, dependencies, parallel work, craft/headcount requirements, and work-package generation.
- **Likely files:** `Data/Models/**`, `Data/AppDbContext.cs`, `Api/Controllers/**Planning**`, planning services, migrations, `webapp/src/modules/planning/**`.
- **Verification required:** Migration/entity/code review, backend build, frontend build, screenshot of step-out plan editor, evidence that steps/dependencies persist, and work package generation output.
- **Regression checklist:** `QA-PLAN-001`, `QA-PLAN-002`, `QA-PLAN-003`, `QA-PLAN-004`.
- **Reopened reason:** N/A.

### PLATFORM-008: Phase 4 Scheduling Demand Must Enforce Source Rules And Dedupe

- [ ] Verify Scheduling demand adapter.
- **Problem:** Scheduling is business-critical because it can double count work if it includes both a converted staffing plan and its estimate.
- **Expected behavior:** Scheduling demand includes qualifying estimates, approved unconverted staffing plans, and scheduling-ready planning work packages. It excludes any staffing plan where `ConvertedEstimateId != null`. The exact estimate statuses that count as schedulable demand are documented.
- **Likely files:** scheduling services/controllers, `Data/Models/StaffingPlan.cs`, `Data/Models/Estimate.cs`, `Data/AppDbContext.cs`, `webapp/src/modules/scheduling/**`.
- **Verification required:** API output showing one approved unconverted staffing plan included, one converted staffing plan excluded, one linked estimate included when status qualifies, and no duplicate demand rows.
- **Regression checklist:** `QA-SCHED-001`, `QA-SCHED-002`, `QA-SCHED-003`.
- **Reopened reason:** N/A.

### PLATFORM-009: Scheduling Must Own Resources, Assignments, Coverage, And Conflicts

- [ ] Verify Scheduling resource/assignment/coverage behavior.
- **Problem:** Scheduling must own actual people assignment, not estimate labor rows or planning notes.
- **Expected behavior:** Scheduling has resources/people, craft/skill/cert support as feasible, availability blocks, assignments, visible shortages, ending-soon, available-soon, and conflict detection for double-booking/unavailable/uncertified assignments.
- **Likely files:** scheduling entities/services/controllers, scheduling migrations, `webapp/src/modules/scheduling/**`.
- **Verification required:** Screenshots/API evidence for resources, assignments, coverage/shortages, ending soon, available soon, and at least one surfaced conflict.
- **Regression checklist:** `QA-SCHED-004`, `QA-SCHED-005`.
- **Reopened reason:** N/A.

### PLATFORM-010: Actuals And Estimate/FCO Delta Must Use Rate Book And Cost Book Logic

- [ ] Verify actual-vs-estimate delta.
- **Problem:** Actuals are required scope, not a future nice-to-have. A pile of hours is not enough; the app must compare actual charge/cost/margin against estimate/FCO baselines.
- **Expected behavior:** Work package or step actuals can record labor/material/equipment where implemented; labor actuals compute billable value from rate book logic and internal cost from cost book logic; delta views show planned vs actual hours, charge, cost, margin, variance amount, and variance percent.
- **Likely files:** planning actuals services/controllers/entities, rate/cost integration services, delta read models, `webapp/src/modules/planning/**`.
- **Verification required:** Code inspection plus sample delta output for at least one estimate or FCO-linked work package.
- **Regression checklist:** `QA-ACT-001`.
- **Reopened reason:** N/A.

### PLATFORM-011: FCO / Change Order Must Produce A Signable Document

- [ ] Verify FCO workflow and document output.
- **Problem:** FCO/change-order planning has to produce a real signable business document, not just a record in a table.
- **Expected behavior:** FCO links to an estimate, can be planned through Planning/work packages, tracks document status, and generates printable/PDF-ready output with project/client info, change number/date, changed scope, reason, schedule impact, cost breakdown, updated value, approval/signature fields, and revision/status history where feasible.
- **Likely files:** FCO/change entities/controllers/services, document generation service/view, `webapp/src/modules/planning/**`.
- **Verification required:** Generated document screenshot/PDF/HTML artifact and code evidence that it is tied to a source estimate/FCO record.
- **Regression checklist:** `QA-FCO-001`, `QA-ACT-001`.
- **Reopened reason:** N/A.

### PLATFORM-012: Platform Demo Seed Must Prove Planning And Scheduling Scenarios

- [ ] Verify platform seed/demo scenarios.
- **Problem:** A scheduling/planning app without realistic demo records cannot prove demand, coverage, shortages, roll-off, available-soon, conflicts, actuals, or FCO flow.
- **Expected behavior:** Demo seed includes planning step-out plans with dependencies/parallel steps, scheduling resources/crafts, assignments ending soon, people freeing up soon, at least one shortage, at least one conflict, one approved unconverted staffing plan that appears as demand, and one converted staffing plan that does not appear separately.
- **Likely files:** new demo seeder classes under `Data/`, any existing seed migration/bootstrap classes.
- **Verification required:** Read-only API/DB evidence plus screenshots from Planning and Scheduling dashboards. Do not run destructive reset/seed flows without Joseph approval.
- **Regression checklist:** `QA-DATA-001`, `QA-SCHED-002`, `QA-SCHED-003`, `QA-SCHED-004`, `QA-PLAN-004`.
- **Reopened reason:** N/A.

### PLATFORM-013: Every Claude Chunk Needs A Codex Evidence Packet Before Closure

- [ ] Enforce evidence packet for platform chunks.
- **Problem:** Checked boxes without evidence recreate the same problem where Joseph becomes the tester and middle man.
- **Expected behavior:** For each Claude chunk, Codex records changed files, build/test results, screenshots where UI changed, exact failures, likely files, fix guidance, and the matching regression checklist IDs.
- **Verification required:** Evidence packet under `docs/qa-evidence/<sweep-id>/` when UI is involved, plus a Claude-readable handoff markdown file under `docs/`.
- **Regression checklist:** `QA-PLAT-003`, `QA-PLAT-004`.
- **Reopened reason:** N/A.

### PLATFORM-014: Scheduling Step-Out Plan Must Be Requirements-Traceable Before Coding

- [ ] Verify Claude's planning-only scheduling docs capture Joseph's requirements before implementation starts.
- **Problem:** Claude is now being asked to produce `SCHEDULING_STEP_OUT_PLAN.md` and `SCHEDULING_TODO.md` before coding. If those docs miss Joseph's actual requirements, later verification will be testing against an incomplete plan instead of the real business need.
- **Expected behavior:** The plan docs are based on actual repo inspection and explicitly cover one portal/two apps, shared auth, Vue/C#/EF Code First, database self-build/migration behavior, estimating-to-scheduling source data, job endings, people availability, craft shortages, reassignment/move-to-next-job flow, notifications, and isolation tradeoffs.
- **Likely files:** `SCHEDULING_STEP_OUT_PLAN.md`, `SCHEDULING_TODO.md`, `docs/LIVE_TODO.md`, `docs/IMPLEMENTATION_WORKLOG.md`, `docs/REGRESSION_CHECKLIST.md`.
- **Verification required:** Codex reads the generated planning docs, maps each Joseph requirement to an acceptance gate, and updates this live QA board and the regression checklist before any implementation work is accepted.
- **Regression checklist:** `QA-PLAT-005`, `QA-SCHED-006`.
- **Reopened reason:** N/A.

### PLATFORM-015: Master Requirements Traceability Must Stay Current

- [ ] Maintain the platform requirements traceability map.
- **Problem:** Joseph's master requirements are broad enough that Claude can accidentally satisfy a narrow slice and still miss required behavior. Codex needs a stable map from Joseph's requirements to QA gates.
- **Expected behavior:** `docs/PLATFORM_REQUIREMENTS_TRACEABILITY.md` maps each major requirement area to live QA gates and regression checklist IDs. When Joseph changes scope, Codex updates the map before verifying or closing related work.
- **Likely files:** `docs/PLATFORM_REQUIREMENTS_TRACEABILITY.md`, `docs/LIVE_QA_TODO.md`, `docs/QA_REGRESSION_CHECKLIST.md`.
- **Verification required:** Compare Joseph's latest requirements against the traceability map and confirm every major area has a live QA gate and a regression checklist item.
- **Regression checklist:** `QA-PLAT-006`.
- **Latest Codex update:** 2026-04-28 initial traceability map created from Joseph's full master prompt.
- **Reopened reason:** N/A.

### PLATFORM-016: Platform Phase Definition-Of-Done Must Include Real Tests And Evidence

- [ ] Enforce phase completion gates.
- **Problem:** A phase can look done in Claude's TODO while builds, screenshots, endpoint checks, or regression checks have not run.
- **Expected behavior:** Every claimed completed phase has backend build, frontend build, real available tests or documented gaps, regression checklist updates, worklog entries, live TODO updates, unresolved failures, and next tasks.
- **Likely files:** `docs/LIVE_TODO.md`, `docs/IMPLEMENTATION_WORKLOG.md`, `docs/TEST_RUN_LOG.md`, `docs/REGRESSION_CHECKLIST.md`, `docs/QA_REGRESSION_CHECKLIST.md`.
- **Verification required:** Codex checks the claimed phase against the definition-of-done before closure and records missing evidence as reopened/open.
- **Regression checklist:** `QA-PLAT-007`.
- **Reopened reason:** N/A.

### PLATFORM-017: Generated API Client / NSwag Changes Must Be Tracked

- [ ] Verify generated client workflow for new API endpoints.
- **Problem:** The repo has NSwag/openapi behavior in the build path, and prior builds showed an NSwag command warning. If Claude adds Portal/Planning/Scheduling endpoints without keeping generated clients in sync, frontend integration can silently drift.
- **Expected behavior:** Claude documents whether generated clients are used for new endpoints, runs the correct generation command when needed, or explicitly documents why direct API calls are currently used. Build warnings are not ignored if they affect client sync.
- **Likely files:** `Api/nswag.json`, project build files, generated client output if present, new frontend API services/stores.
- **Verification required:** Code review plus build/test evidence showing API changes and frontend calls are aligned.
- **Regression checklist:** `QA-PLAT-008`.
- **Latest Codex check:** 2026-04-28. `dotnet build --no-restore --configuration Release` passed and NSwag executed successfully against `Api/nswag.json`. Still open until Codex verifies whether frontend uses generated client output or direct API calls for new scheduling/planning/portal endpoints and whether the generated artifacts changed as expected.
- **Reopened reason:** N/A.

### PLATFORM-018: Shared Demand/Coverage Logic Must Not Live Only In Vue

- [ ] Verify core operational truth is centralized appropriately.
- **Problem:** The master requirements call out duplicated schedule/labor/coverage logic and warn against browser-side fan-out becoming the source of operational truth. If Scheduling/Planning repeats estimating frontend calculations, dashboards can disagree again.
- **Expected behavior:** Demand aggregation, converted-plan dedupe, coverage gaps, assignment conflicts, ending-soon, and available-soon calculations live in backend/shared services or documented read models where they can be tested and reused. Vue views consume results and handle presentation.
- **Likely files:** scheduling/planning services/controllers, shared calculation services, existing manpower/forecast logic, `webapp/src/modules/**` views/stores.
- **Verification required:** Code inspection and at least one API/test evidence path showing the calculation is not only a frontend computed value.
- **Regression checklist:** `QA-PLAT-009`, `QA-SCHED-001`, `QA-SCHED-008`.
- **Reopened reason:** N/A.

### PLATFORM-019: Claude Scheduling Implementation Queue Must Be Verified Item-By-Item

- [ ] Audit Claude's current scheduling implementation queue.
- **Problem:** Claude is now working from a broad checklist that includes planning docs, scheduling services, Program.cs wiring, DevController seed data, portal/dashboard KPI fixes, multiple Scheduling views, Planning views, and a final regression pass. If Codex verifies only the final UI, implementation gaps can hide underneath.
- **Claude queue captured 2026-04-28:** `SCHEDULING_STEP_OUT_PLAN.md`, `SCHEDULING_TODO.md`, `AssignmentConflictService.cs`, `CoverageCalculationService.cs`, `SuggestedMatchService.cs`, Program.cs service registration, SchedulingController wiring, scheduling demo seed data, Portal/Scheduling dashboard KPIs, SchedulingDashboardView, JobsBoardView, ResourcesBoardView, AssignmentsView, CoverageView, RollOffView, StepOutPlanList/Form, WorkPackageList, FcoList, and full regression checklist.
- **Expected behavior:** Each queue item must have code/docs evidence, build/test evidence where applicable, and an explicit PASS/FAIL/NOT VERIFIED verdict before Codex closes related live TODO items.
- **Likely files:** `SCHEDULING_STEP_OUT_PLAN.md`, `SCHEDULING_TODO.md`, scheduling/planning services/controllers/views, `Api/Program.cs`, `Data/**`, `Api/Controllers/DevController.cs`, dashboard views/services.
- **Verification required:** Codex creates an evidence packet after Claude's chunk showing which checklist items exist, which compile, which are functional, and which violate architecture boundaries.
- **Regression checklist:** `QA-PLAT-007`, `QA-PLAT-009`, `QA-PLAN-005`, `QA-SCHED-004`, `QA-SCHED-007`, `QA-SCHED-008`.
- **Latest Codex check:** 2026-04-29 Project-app audit. Backend Release build passed and NSwag ran successfully; frontend `build:dev` passed after dependencies were installed. `verify-scheduling-planning.spec.ts` passed with `SKIP_GLOBAL_SETUP=true`, but both screenshots are blank dark pages, so the spec is too weak and does not prove visible runtime health. Still open for item-by-item functional proof, API output/dedupe evidence, visible-content screenshots, and final regression checklist.
- **Reopened reason:** N/A.

### PLATFORM-020: Platform Demo Seed Must Not Be Controller-Only Lifecycle Seed

- [ ] Verify scheduling/planning seed follows the bootstrap rules.
- **Problem:** Claude's visible queue says "Add scheduling demo seed data to DevController". That can be acceptable only as a temporary explicit dev/demo endpoint. It violates the master requirements if it becomes the primary database lifecycle seed path.
- **Expected behavior:** Reference/demo seed responsibilities live in controlled, idempotent seed/bootstrap classes under the real `Data/` structure. Any `DevController` seed endpoint is explicit, additive, demo-only, safe to rerun, and not the only place platform seed data exists.
- **Likely files:** `Api/Controllers/DevController.cs`, `Data/Bootstrap/**`, `Data/DBInitializer.cs`, `Api/Program.cs`, `Api/appsettings*.json`.
- **Verification required:** Code inspection showing seed path separation, config-controlled bootstrap behavior, no destructive reset use, and read-only API/DB proof that platform demo seed can be rerun safely without wiping demo data.
- **Regression checklist:** `QA-BOOT-001`, `QA-BOOT-002`, `QA-DATA-005`.
- **Latest Codex check:** 2026-04-28. `Api/Controllers/DevController.cs` has scheduling resource/assignment seed code and build passes. Still open: Codex has not verified that equivalent platform seed is separated into controlled `Data/Bootstrap` lifecycle seed classes, nor that the dev seed is additive/idempotent for the new scheduling records.
- **Reopened reason:** N/A.

### PLATFORM-021: Project Planning Master Plan Docs Must Be Repo-Grounded

- [ ] Verify Claude creates the project-planning master docs before coding.
- **Status:** Upcoming / queued from Joseph's 2026-04-28 requirements.
- **Problem:** The next planning prompt is research/architecture-only and explicitly says "Do NOT start coding yet." If Claude codes first or writes generic docs, the plan will not be useful as an implementation contract.
- **Expected behavior:** Claude inspects the actual repo and creates `PROJECT_PLANNING_MASTER_PLAN.md`, `PROJECT_PLANNING_MASTER_TODO.md`, `PROJECT_PLANNING_DATA_MODEL.md`, and `PROJECT_PLANNING_UI_MAP.md`. The docs must use actual repo structure/names and distinguish reuse existing, extend existing, and build new.
- **Likely files:** `PROJECT_PLANNING_MASTER_PLAN.md`, `PROJECT_PLANNING_MASTER_TODO.md`, `PROJECT_PLANNING_DATA_MODEL.md`, `PROJECT_PLANNING_UI_MAP.md`, possibly `docs/IMPLEMENTATION_WORKLOG.md` if Claude records the planning pass.
- **Verification required:** Codex reads all four docs, checks them against the actual repo and Joseph's prompt, and reports PASS/FAIL. No source-code implementation should be accepted under this item.
- **Regression checklist:** `QA-PLAN-006`.
- **Reopened reason:** N/A.

### PLATFORM-022: Project Planning Domain Model Must Cover Timeline, Gantt, Calendar, And Hierarchy

- [ ] Verify project-planning domain model depth.
- **Status:** Upcoming / queued from Joseph's 2026-04-28 requirements.
- **Problem:** A generic task list or simple calendar would miss the core requirement: project -> phase -> task -> subtask -> step-out plan, with baselines, milestones, planned/actual dates, variance, Gantt/calendar views, and drill-down.
- **Expected behavior:** The plan defines entities/relationships for Project, ProjectTimeline, ProjectPhase, PlanTask, PlanTaskDependency, PlanMilestone, StepOutPlan, StepOutStep, StepOutSubStep, TaskAssignment, TaskScheduleStatus, TaskProgressSnapshot, TimelineBaseline, TimelineVariance, CalendarEntry/equivalent, audit/status/ownership fields, and hierarchy/dependency storage.
- **Likely files:** `PROJECT_PLANNING_DATA_MODEL.md`, `PROJECT_PLANNING_MASTER_PLAN.md`, `PROJECT_PLANNING_UI_MAP.md`.
- **Verification required:** Codex confirms each required entity/capability is present, ownership is stated, fields/relationships are specified, and performance/migration risks for deep hierarchies are discussed.
- **Regression checklist:** `QA-PLAN-007`, `QA-PLAN-009`.
- **Reopened reason:** N/A.

### PLATFORM-023: Project Planning Must Define Estimate/FCO Traceability And Schedule Health

- [ ] Verify traceability and ahead/behind model.
- **Status:** Upcoming / queued from Joseph's 2026-04-28 requirements.
- **Problem:** The planning platform must prove traceability from estimate -> FCO -> project task and task -> FCO -> estimate, and it must calculate whether work is ahead, on track, or behind. These cannot be vague dashboard labels.
- **Expected behavior:** The plan defines source of truth for estimates, FCOs, project plans, phases, tasks, milestones, step-out plans, and scheduling assignments. It also defines validation for missing/mismatched estimate/FCO links, what happens when estimates/FCOs change, and formulas for planned finish, actual finish, forecast finish, percent complete, schedule variance, milestone/phase/task slippage, critical tasks, overdue status, and downstream impact.
- **Likely files:** `PROJECT_PLANNING_MASTER_PLAN.md`, `PROJECT_PLANNING_DATA_MODEL.md`, `PROJECT_PLANNING_UI_MAP.md`.
- **Verification required:** Codex checks the traceability model and ahead/behind formulas against Joseph's requirements and flags any missing source-of-truth or data-integrity rules.
- **Regression checklist:** `QA-PLAN-008`, `QA-PLAN-010`, `QA-FCO-002`.
- **Reopened reason:** N/A.

### PLATFORM-024: End-To-End Project Lifecycle Master Docs Must Be Created Before Coding

- [ ] Verify Claude creates the lifecycle operating model docs before implementation.
- **Status:** Upcoming / queued from Joseph's 2026-04-28 lifecycle requirements.
- **Problem:** This new prompt expands scope beyond Planning/Gantt into the full lifecycle: estimating, authorization, project initiation, work orders, scheduling, execution, actuals, change control, closeout, and lessons learned. Claude must not code before producing the architecture package.
- **Expected behavior:** Claude creates/updates `PROJECT_LIFECYCLE_MASTER_PLAN.md`, `PROJECT_DATA_OWNERSHIP_AND_TRACEABILITY.md`, `PROJECT_STAGE_GATE_AND_STATUS_MODEL.md`, `PROJECT_ACTUALS_AND_VARIANCE_MODEL.md`, and `PROJECT_PLANNING_MASTER_TODO.md` under `docs/`. The docs must be grounded in actual repo paths and current models/controllers/migrations/docs.
- **Likely files:** `docs/PROJECT_LIFECYCLE_MASTER_PLAN.md`, `docs/PROJECT_DATA_OWNERSHIP_AND_TRACEABILITY.md`, `docs/PROJECT_STAGE_GATE_AND_STATUS_MODEL.md`, `docs/PROJECT_ACTUALS_AND_VARIANCE_MODEL.md`, `docs/PROJECT_PLANNING_MASTER_TODO.md`.
- **Verification required:** Codex confirms all five docs exist, are repo-grounded, answer the 10 required questions, and do not claim implementation. Codex must flag any coding/source change under this planning-only gate unless Joseph separately approved implementation.
- **Regression checklist:** `QA-LIFE-001`, `QA-PLAT-006`.
- **Reopened reason:** N/A.

### PLATFORM-025: Lifecycle Plan Must Model Estimate -> Authorization -> Project -> WorkOrder Gates

- [ ] Verify commercial authorization and work-release gates.
- **Status:** Upcoming / queued from Joseph's 2026-04-28 lifecycle requirements.
- **Problem:** The system must not model Estimate -> PO automatically. It needs a flexible commercial authorization concept and a WorkOrder release gate so executable work cannot float without approved commercial traceability.
- **Expected behavior:** The plan defines Estimate as the commercial baseline, CommercialAuthorization as PO/signed proposal/contract/NTP/work authorization/release order, Project as the execution umbrella, and WorkOrder as released execution package. It must support one estimate to many work orders and one project to many work orders.
- **Likely files:** lifecycle docs, ownership/traceability docs, stage-gate docs.
- **Verification required:** Codex checks that rules are explicit: no released WorkOrder without approved Estimate + valid CommercialAuthorization; no StepOutPlan without WorkOrder; portal KPI language uses `Projects at Risk` rather than raw pending FCO count.
- **Regression checklist:** `QA-LIFE-002`, `QA-LIFE-003`, `QA-PLAT-010`.
- **Reopened reason:** N/A.

### PLATFORM-026: Lifecycle Plan Must Define FCO, Actuals, Variance, And Authorized-Value Risk

- [ ] Verify change control and actuals operating model.
- **Status:** Upcoming / queued from Joseph's 2026-04-28 lifecycle requirements.
- **Problem:** Actuals are the truth and FCO/change order is controlled business change. The plan must cover both cost and billable actuals, not only charged values.
- **Expected behavior:** The plan defines FCO as a first-class controlled object tied to Estimate and WorkOrder, with impacted tasks/steps where appropriate. It defines ActualEntry/equivalent tied to WorkOrder, optional task/step/substep/FCO, actual cost, billable amount, labor/equipment/material/subcontract values, actual start/finish, completion, estimate-vs-actual, FCO-vs-actual, project/work-order/task rollups, authorized value vs forecast/actual risk, and future estimating feedback.
- **Likely files:** lifecycle docs, actuals/variance docs, ownership/traceability docs, stage-gate docs.
- **Verification required:** Codex verifies no orphan FCO/actuals model is accepted, no actuals without WorkOrder linkage, and forecast/actual exceeding authorized value is treated as risk.
- **Regression checklist:** `QA-ACT-002`, `QA-FCO-003`, `QA-LIFE-004`.
- **Reopened reason:** N/A.

### PLATFORM-027: Lifecycle Plan Must Define Stage Gates, Status Models, Closeout, And Lessons Learned

- [ ] Verify full project lifecycle status model.
- **Status:** Upcoming / queued from Joseph's 2026-04-28 lifecycle requirements.
- **Problem:** The platform needs more than screens. It needs lifecycle controls from opportunity through closeout and historical estimating feedback.
- **Expected behavior:** The plan maps the lifecycle through opportunity/bid intake, estimate development, commercial authorization, project initiation, planning/PM, work release, scheduling, execution, monitoring/controlling, change control, actuals/variance/financial learning, and closeout. It also maps the PM lifecycle lens: initiating, planning, executing, monitoring/controlling, closing.
- **Likely files:** lifecycle master plan, stage-gate/status model, actuals/variance model, planning TODO.
- **Verification required:** Codex checks status models for Estimate, Project, WorkOrder, Task/Step/SubStep, milestones, at-risk/on-track/late rules, stage transitions/gates, closeout state, and lessons learned/future-estimating loop.
- **Regression checklist:** `QA-LIFE-005`, `QA-LIFE-006`, `QA-PLAN-010`.
- **Reopened reason:** N/A.

### PLATFORM-028: Developer Handoff Docs Must Be Canonical And Conflict-Free

- [x] Verify the handoff package is canonical and stale planning docs are reconciled.
- **Status:** Completed by Codex on 2026-04-28 after Joseph authorized Codex to perform doc cleanup.
- **Problem:** The lifecycle docs are directionally strong, but the full doc set is not yet safe for developer handoff. `PROJECT_PLANNING_MASTER_PLAN.md` and `PROJECT_PLANNING_DATA_MODEL.md` still contain stale/conflicting guidance against the newer lifecycle model.
- **Conflicts to resolve:** `ProjectPlan` vs `Project`, `TaskActual` vs `ActualEntry`, in-house SVG Gantt vs install/use a Gantt library, and `StepOutSubStep` as MVP leaf level vs post-MVP-only.
- **Expected behavior:** Claude creates `docs/PROJECT_HANDOFF_INDEX.md` with final approved reading order, canonical source of truth by topic, superseded/archived docs, and unresolved decisions if any. Stale docs are updated, archived, or clearly marked superseded so developers do not treat them as equal truth.
- **Canonical docs:** `docs/PLATFORM_EXPANSION_MASTER_CONTRACT.md`, `docs/PROJECT_LIFECYCLE_MASTER_PLAN.md`, `docs/PROJECT_DATA_OWNERSHIP_AND_TRACEABILITY.md`, `docs/PROJECT_PLANNING_UI_MAP.md`, `docs/PROJECT_PLANNING_MASTER_TODO.md`, `docs/PLATFORM_REQUIREMENTS_TRACEABILITY.md`.
- **Do not use as source of truth until reconciled:** `docs/PROJECT_PLANNING_MASTER_PLAN.md`, `docs/PROJECT_PLANNING_DATA_MODEL.md`.
- **Verification required:** Codex confirms the handoff index exists, the canonical stack is explicit, stale docs cannot reasonably be mistaken for current truth, and the four named conflicts are resolved in favor of the lifecycle model unless Joseph explicitly approves a different choice.
- **Regression checklist:** `QA-LIFE-007`, `QA-PLAT-003`.
- **Verification evidence:** `docs/PROJECT_HANDOFF_INDEX.md` created with canonical reading order/source-of-truth/locked decisions/superseded docs. `docs/PROJECT_PLANNING_MASTER_PLAN.md` and `docs/PROJECT_PLANNING_DATA_MODEL.md` are tombstoned. `docs/PROJECT_PLANNING_MASTER_TODO.md` and `docs/PROJECT_PLANNING_UI_MAP.md` now use `Project`, `ProjectDetailView`, `/api/v1/projects`, and `ActualEntry`; `StepOutSubStep` remains in MVP scope. Searches confirmed no active canonical-doc references to `ProjectPlan`, `ProjectPlanView`, `ProjectPlans`, `TaskActual`, `/api/v1/project-plans`, `DbSet<ProjectPlan>`, `DbSet<TaskActual>`, or `dhtmlx-gantt`; remaining matches are archived/superseded context only.
- **Reopened reason:** N/A.


### P1-UX-001: Estimate + Staffing Plan List Actions Must Be Hover-Only With Dropdown

- [x] Hide all row actions until hover; move workflow actions into three-dot dropdown.
- **Completed date:** 2026-04-26.
- **Verification evidence:** Joseph manual verification via screenshot — estimate list shows Edit (pencil), Delete (trash), and three-dot menu icon in ACTIONS column. Confirmed working.
- **Regression checklist:** QA-EST-014, QA-UI-006.

### P1-SP-002: Staffing Plan Missing Submit for Approval Flow

- [ ] Add Submit for Approval button and PATCH status flow to staffing plan form.
- **Problem:** A Submit for Approval button and PATCH endpoint now exist, but the workflow is still incomplete and status handling is inconsistent.
- **Impact:** Management cannot formally review staffing plans; there is no audit trail of plan approval.
- **Likely files:** `webapp/src/modules/estimating/features/staffing-form/views/StaffingFormView.vue`, `webapp/src/modules/estimating/features/staffing-list/views/StaffingListView.vue`, `api/Controllers/StaffingPlansController.cs`
- **Expected behavior:** A "Submit for Approval" button is visible on saved Draft plans (not new, not converted). Clicking saves successfully, shows a confirm dialog, then PATCHes status to a validated submitted value. Status badge updates immediately. The form must not expose an editable status dropdown that bypasses the workflow. Staffing list filter/badge must include submitted status.
- **Verification required:** Screenshot of staffing plan header showing Submit for Approval button on a saved Draft; status badge showing "Submitted for Approval" after confirmation; no editable status dropdown; list can filter/style submitted plans.
- **Regression checklist:** `QA-SP-009`.
- **Latest verification:** 2026-04-26 Codex verification. Screenshot `webapp/test-results/qa/QA_AUDIT_20260426_verify/staffing-wait.png` shows the staffing list loads with `Plans: 12`, non-zero Rough Labor, and labor preview pills for visible 2027 plans. Code inspection shows the form has a Submit for Approval button, but still exposes an editable `STATUS` dropdown.
- **Latest resweep:** 2026-04-26 Codex resweep. The editable staffing status dropdown has been replaced with a read-only status tag. Still open: `Submit for Approval` is visible even on a new unsaved staffing plan, `savePlan()` catches save errors without rethrowing so submit can continue after a failed save, and backend PATCH still accepts any status string / does not guard converted plans (`Api/Controllers/StaffingPlansController.cs`).
- **Reopened reason:** Still open for saved-plan-only visibility, failed-save behavior, and backend status validation/converted-plan guard.

### P1-SP-003: Header-Only Test Staffing Plan Still Visible In Demo Data

- [x] Remove or complete invalid header-only test staffing plan after explicit approval.
- **Problem:** Staffing list still shows `[PW-TEST] Live SP 1777212480558` with `Rough Labor: $0` and no labor preview pills. This is the exact invalid test-agent behavior where a staffing plan header is created without meaningful labor demand.
- **Impact:** Demo viewers can see bad test data, manpower forecast can be polluted or misleading, and agents may falsely claim staffing workflow coverage without labor rows.
- **Likely files/data:** SQL Express `StaffingPlans`/`StaffingLaborRows` data for the visible `[PW-TEST]` plan; QA agent prompt/checklist; `docs/QA_REGRESSION_CHECKLIST.md`.
- **Expected behavior:** Demo data should not contain visible header-only test staffing plans unless the scenario is explicitly testing empty-plan validation. Any test staffing plan must have labor rows, non-zero Rough Labor, preview pills, and forecast impact.
- **Verification required:** Staffing list screenshot shows no `[PW-TEST]` header-only plan with `Rough Labor: $0`, or the plan has been completed with labor rows and forecast count verified.
- **Evidence:** Screenshot `webapp/test-results/qa/QA_AUDIT_20260426_staffing/staffing-list.png` shows `[PW-TEST] Live SP 1777212480558` with `Rough Labor: $0`.
- **Regression checklist:** `QA-SP-001`, `QA-SP-005`.
- **Latest verification:** PASS in live UI. Screenshot `docs/qa-evidence/QA_AUDIT_20260426_TODO_RESWEEP/staffing-search-pw-test.png` shows search `PW-TEST` returns `Plans: 0` / `No staffing plans found`. Main staffing screenshot also shows visible cards have non-zero Rough Labor and labor preview pills.
- **Do not re-flag unless:** A `[PW-TEST]` or other header-only staffing plan becomes visible again in demo data.
- **Reopened reason:** N/A.

### P1-SP-004: Expired Unconverted Staffing Plans Must Be Flagged For Review

- [ ] Detect stale staffing plans whose scheduled start date has arrived or passed and which have not been converted to an estimate.
- **Problem:** A staffing plan is only a forecast/opportunity until it becomes an estimate. If a plan starts on Jan 1, 2027 and today is Jan 1, 2027, but it still has no converted estimate, the app should not quietly treat it as normal future revenue or clean manpower demand.
- **Impact:** Revenue forecast, manpower forecast, and pipeline KPIs can overstate future work by counting opportunities that should have already converted, been awarded, been lost, or been reviewed.
- **Likely files:** `webapp/src/modules/estimating/features/staffing-list/views/StaffingListView.vue`, `webapp/src/modules/estimating/features/analytics/utils/forecast.ts`, `webapp/src/modules/estimating/features/analytics/views/RevenueForecastView.vue`, `Api/Controllers/StaffingPlansController.cs`.
- **Expected behavior:** Any unconverted staffing plan with `startDate <= today` is flagged as `Expired`, `Stale`, or `Needs Review`. It should appear in a review lane/filter and should not be counted as ordinary future revenue/pipeline. Do not auto-mark it Lost; provide a deliberate review action such as Convert, Mark Lost/Declined with reason, or Archive.
- **Verification required:** Create or find a staffing plan with labor rows, `convertedEstimateId = null`, and start date on/before today. Confirm it is visibly flagged for review, excluded or separately categorized from future projections, and not double-counted as awarded/active work.
- **Regression checklist:** `QA-SP-010`, `QA-AN-017`.
- **Reopened reason:** N/A.

### P0-020: Light Mode Must Be Fully Audited And Demo-Readable

- [ ] Fix and verify light mode across the full app.
- **Problem:** Light mode was not included in prior "every button / every page" audits. Joseph found obvious contrast failures after clicking the theme toggle: page titles turn white on pale backgrounds, disabled/secondary buttons are barely visible, and some action buttons/links do not have enough contrast.
- **Impact:** Demo-critical. The theme toggle is visible in the top bar; if clicked during demo, the app can immediately look broken or unprofessional.
- **Likely files:** `webapp/src/assets/css/style.css`, `webapp/src/assets/css/theme.css`, `webapp/src/components/layout/TheTopBar.vue`, shared button/page header styles, feature views that use hardcoded `text-white`, `text-slate-*`, or low-opacity button classes.
- **Expected behavior:** Every page and modal is readable in both dark and light mode. Page titles, subtitles, disabled buttons, outlined buttons, text buttons, table headers, active nav items, empty states, form labels, placeholders, and action buttons must meet basic contrast and be visually obvious.
- **Verification required:** For each primary demo page, click the top-bar theme toggle into light mode, capture screenshots, and verify all text/buttons are readable and all controls are discoverable. Repeat in dark mode to ensure the fix does not regress the main demo theme.
- **Evidence:** `docs/qa-evidence/QA_LIGHT_MODE_20260426/crew-templates-light.png` and `docs/qa-evidence/QA_LIGHT_MODE_20260426/new-estimate-light.png`. Joseph also provided screenshots showing the same issue.
- **Regression checklist:** `QA-UI-008`, `QA-UI-009`.
- **Latest verification:** 2026-04-26 Codex resweep. Partial improvement. Fresh screenshots `docs/qa-evidence/QA_AUDIT_20260426_TODO_RESWEEP/crew-templates-light.png` and `docs/qa-evidence/QA_AUDIT_20260426_TODO_RESWEEP/estimate-new-light.png` show those two pages are now readable in light mode. Full-app light-mode sweep across every primary page/modal is still required before closing.
- **Reopened reason:** N/A.

### P0-021: Revision Snapshot Must Capture Current Estimate State

- [ ] Replace confusing snapshot workflow with one simple Create Revision workflow.
- **Product decision:** Simplify revisions. There should not be both `Save + Revision` and drawer `+ Create Revision` snapshot paths. The user should click one clear `Create Revision` action that creates the next sequential editable revision copy of the estimate.
- **UX contract:** A revision is not just a hidden snapshot note. It should behave like an editable version of the estimate: create Rev N, edit Rev N, save Rev N, view prior revisions, and deliberately revert/restore if needed.
- **Problem:** Joseph created/saved a revision after changing the estimate, but Revision History still showed the old counts/totals: revision card showed `6 labor` and `Total: $240,093` while the form visibly had `8 rows` and Summary/Grand Total `$291,378`.
- **Impact:** Demo-critical if revisions are shown. Revision history becomes untrustworthy because it can label a stale DB snapshot as `CURRENT` while the visible estimate has different labor rows and revenue.
- **Likely cause:** Current design has multiple revision creation paths. `Save + Revision` saves rows/summary before snapshot, while drawer `+ Create Revision` directly POSTs `/api/v1/estimates/{id}/revisions` and can snapshot stale SQL data. The broader UX issue is that snapshots and editable revisions are being mixed.
- **Likely files:** `webapp/src/modules/estimating/features/estimate-form/components/RevisionDrawer.vue`, `webapp/src/modules/estimating/features/estimate-form/views/EstimateFormView.vue`, `webapp/src/modules/estimating/stores/estimateStore.ts`, `Api/Controllers/RevisionsController.cs`.
- **Expected behavior:** A single `Create Revision` button creates a new editable copy/version of the current estimate as the next sequential revision number. The new revision opens for editing. When saved, it persists as that revision number. The user can view prior revisions and revert/restore back to a prior revision intentionally.
- **Workflow target:**
  1. User opens current estimate/revision.
  2. User clicks `Create Revision`.
  3. System saves or snapshots the current source safely, creates the next sequential revision copy, and opens it.
  4. User edits the revision normally.
  5. User clicks `Save`; it saves as that revision number.
  6. Revision history clearly shows Rev 1, Rev 2, Rev 3, etc. with row counts/totals that match each saved revision.
  7. User can revert/restore to a prior revision through a deliberate action.
- **Do not close if:** The app still exposes two different revision creation paths, or if `Create Revision` creates a card from stale database state instead of an editable sequential revision copy.
- **Verification required:** Start from an estimate with 6 labor rows and Rev 1. Click `Create Revision` and confirm Rev 2 opens as an editable copy. Add 2 rows, save Rev 2, refresh, and verify Rev 2 shows 8 labor rows and the new Grand Total while Rev 1 still shows 6 labor rows and the old total. Revert/restore Rev 1 and verify the estimate returns to 6 rows/old total.
- **Evidence:** Joseph-provided screenshots in chat show visible Labor `8 rows`, Summary Grand Total `$291,378`, while Revision History current Rev 2 shows `6 labor` and `Total: $240,093`.
- **Regression checklist:** `QA-EST-015`, `QA-EST-016`.
- **Latest verification:** 2026-04-26 Codex resweep. Still open/fail by code inspection. The estimate form still exposes `Save + Revision`, the drawer still exposes `+ Create Revision`, and drawer copy still says `snapshot your current work`. This violates the product decision for one simple editable sequential revision workflow.
- **Reopened reason:** N/A.

## Deferred Until After Demo

### P0-022: AI Review Left-On-The-Table Must Use Past Estimate Details

- [ ] Revisit after demo.
- **Decision:** Deferred by Joseph until after the demo on 2026-04-26. Do not treat this as a blocker for tomorrow's demo and do not assign test agents to verify it during demo-readiness sweeps unless Joseph explicitly reopens it.
- **Problem:** Claude partially wired the feature, but the AI cannot reliably call the new details tool yet. `get_estimate_details` exists in `ToolExecutorService.ExecuteAsync()`, and Valero seed records `25-0031-VLO` / `26-0031-VLO` exist additively, but `get_estimate_details` is missing from `BuildDbTools()`. The system prompt also does not instruct the model to call `search_estimates` then `get_estimate_details` for same-client/location review comparisons. The `Review with AI` button still sends the old generic rate-anomaly prompt.
- **Impact:** Post-demo enhancement. Do not use this scenario in tomorrow's demo unless Joseph reopens it.
- **Likely files:** `Api/Services/ToolExecutorService.cs`, `Api/Services/AiService.cs`, `webapp/src/modules/estimating/features/estimate-form/views/EstimateFormView.vue`, `webapp/src/modules/estimating/stores/aiChatStore.ts`, `Api/Controllers/DevController.cs`.
- **Expected behavior:** Open `26-0031-VLO`, click `Review with AI`, and AI should search Valero Port Arthur history, call `get_estimate_details` for `25-0031-VLO`, compare labor and equipment, list missing positions/equipment, name the 2025 CDU job, and estimate roughly `$262K` left on the table with line-item math.
- **Verification required:** Screenshot/chat transcript showing the AI response names `Valero Port Arthur CDU Turnaround 2025`, lists missing crew and equipment, gives dollar values, and offers to add missing items. Confirm the math comes from seeded data, not fabrication.
- **Regression checklist:** `QA-AI-014`.
- **Reopened reason:** N/A.

## Verification Sweeps

### 2026-04-29 Codex Sweep: Project-app Runtime Audit

Evidence handoff: `docs/QA_EVIDENCE_20260429_PROJECT_APP_RUNTIME.md`

Screenshot packet: `docs/qa-evidence/QA_AUDIT_20260429_PROJECT_APP_RUNTIME/`

Build/test verification:

- `dotnet build --no-restore --configuration Release` passed. NSwag ran successfully. Warnings: AutoMapper NU1903 advisory and nullable warning in `Api/Program.cs(126,55)`.
- `npm.cmd --prefix webapp install` passed because frontend dependencies were missing in this checkout.
- `npm.cmd --prefix webapp run build:dev` passed.
- `dotnet test --no-restore --configuration Release` exited 0, but there are no backend test projects in this repo.
- `SKIP_GLOBAL_SETUP=true npx.cmd playwright test tests/e2e/verify-scheduling-planning.spec.ts --project=chromium-mocked --workers=1` passed, but the captured screenshots are blank dark pages. Treat this as an inadequate smoke test, not proof the UI is visibly correct.

| ID | Verdict | Evidence |
|----|---------|----------|
| PLATFORM-019 | OPEN / PARTIAL | Build and basic smoke ran, but screenshots are blank and queue items still need item-by-item functional proof. |
| PLATFORM-004 | OPEN / PARTIAL | Frontend builds and planning/scheduling routes exist, but visible UI proof failed because screenshots show blank dark pages. |
| PLATFORM-005 | OPEN / PARTIAL | Portal endpoint exists, but portal still uses old signals such as pending FCO/recent estimates instead of the locked project-health shell language. |
| PLATFORM-016 | OPEN / PARTIAL | Test trail exists, but no backend test project exists and the planning/scheduling smoke test needs stronger visible-content assertions. |
| PLATFORM-017 | OPEN / PARTIAL | Release build now runs NSwag successfully; keep tracking because earlier Project-app build attempt showed NSwag fragility before rerun. |
### 2026-04-26 Codex Sweep: TODO Resweep

Evidence handoff: `docs/QA_EVIDENCE_20260426_TODO_RESWEEP.md`

Screenshot packet: `docs/qa-evidence/QA_AUDIT_20260426_TODO_RESWEEP/`

Build verification:

- `dotnet build --no-restore --configuration Release` passed with warnings only.
- `npm.cmd run build:dev` passed.

| ID | Verdict | Evidence |
|----|---------|----------|
| P0-001 | OPEN / PARTIAL | Status length/backend acceptance improved, but submitted rows still mix `Submitted` and `Submitted for Approval`, and exact-match filters/badges do not fully normalize. |
| P0-004 | OPEN / PARTIAL | Backend lost-reason guard exists; live API/AI proof still needed. |
| P0-005 | DONE | `revenue-forecast.png` shows `AVG MARGIN 31%`, `FUTURE REVENUE $5.7M`, and `FUTURE PROFIT $1.8M`. |
| P0-006 | OPEN / PARTIAL | Chart now uses top period control and includes Staffing Pipeline, but full numeric reconciliation is still pending. |
| P0-011 | OPEN / PARTIAL | Awarded drilldown works (`revenue-awarded-drilldown.png`); Future Revenue still lacks staffing source drilldown. |
| P0-012 | DONE / ACCEPTED | Estimate row menu works (`estimate-row-menu-open-1600.png`). Joseph verified staffing list actions are intentionally hover-revealed rather than a three-dot menu (`staffing-menu-check.png`). |
| P0-014 | OPEN / PARTIAL | 3 seeded templates visible (`crew-templates-light.png`); apply flow not verified. |
| P1-SP-002 | OPEN | Staffing status badge is read-only now, but submit action visibility/save-error/backend validation issues remain. |
| P1-SP-003 | DONE | Search `PW-TEST` returns `Plans: 0` (`staffing-search-pw-test.png`). |
| P0-020 | OPEN / PARTIAL | Light mode improved on Crew Templates and New Estimate; full-app light sweep still needed. |
| P0-021 | FAIL / OPEN | Duplicate revision/snapshot flows still exist. |
| P0-022 | DEFERRED | Joseph deferred this until after the demo. Historical finding remains: `get_estimate_details` is not exposed in `BuildDbTools()` and Review button still uses old generic prompt. |

### 2026-04-26 Codex Sweep: Live Build + Screenshot Verification

Build verification:

- `dotnet build --no-restore --configuration Release` passed with warnings only:
  - `NU1903` AutoMapper 14.0.0 high severity advisory.
  - NSwag post-build warning: command resolved as `run ../Api/nswag.json...` and exited 9009, but MSBuild still reported success.
- `npm.cmd run build:dev` passed.

Live screenshot evidence:

- Revenue Forecast: `webapp/test-results/qa/QA_AUDIT_20260426_verify/revenue-wait.png`
- Estimate list: `webapp/test-results/qa/QA_AUDIT_20260426_verify/estimates-wait.png`
- New estimate form: `webapp/test-results/qa/QA_AUDIT_20260426_verify/new-estimate-wait.png`
- Staffing plans list: `webapp/test-results/qa/QA_AUDIT_20260426_verify/staffing-wait.png`

| ID | Verdict | Evidence |
|----|---------|----------|
| P0-001 | OPEN / PARTIAL | New estimate form now shows a read-only `DRAFT` badge and no editable estimate status dropdown (`new-estimate-wait.png`). However, the estimate API still accepts `Submitted` while frontend submit actions still patch `Submitted for Approval` (`Api/Controllers/EstimatesController.cs`, `EstimateFormView.vue`, `EstimateListView.vue`). Forecast normalization also does not map `Submitted for Approval` to pipeline. |
| P0-002 | OPEN | Submit for Approval still relies on the rejected `Submitted for Approval` API value. The PDF download step exists, but customer-safe PDF content and final status update are not fully verified. |
| P0-003 | OPEN / PARTIAL | Estimate list workflow actions exist in code through the row menu: Open, Submit for Approval, Mark as Won, Mark as Lost, Create Revision. The visible list loads (`estimates-wait.png`), but final verification is blocked by the submitted-status mismatch and server-side lost-reason gap. |
| P0-004 | OPEN | Backend estimate status PATCH still does not require `LostReason` when setting `Lost`; it only stores the reason if supplied. |
| P0-005 | FAIL / OPEN | Live Revenue Forecast screenshot shows real Awarded Revenue `$3.1M`, Future Revenue `$7.2M`, but `AVG MARGIN 0%` and `FUTURE PROFIT $0` (`revenue-wait.png`). Code still reads `analyticsResp.data?.avgMarginPct`; API returns `kpis.avgMarginPct`. |
| P0-006 | FAIL / OPEN | Revenue chart now follows the top period control structurally and no separate Monthly/Quarterly/Cumulative controls are visible, but chart/KPI math still uses different definitions: pending KPI uses raw totals while chart pipeline is confidence-weighted and chart omits staffing amount from the displayed pipeline. |
| P0-010 | OPEN / PARTIAL | Seed code now contains realistic 2025 estimate history and 2027 staffing plans. Live CSL screenshot shows future staffing plan cards with non-zero Rough Labor and labor pills. Full DB/year/status reconciliation was not performed. |
| P0-011 | OPEN / PARTIAL | Awarded/Pending/Lost/Total KPI drilldown table exists in Revenue Forecast code and rows route to estimate detail. Future Revenue is not drillable to staffing/source records, so the boss requirement is only partially covered. |
| P0-012 | SUPERSEDED - DONE / ACCEPTED | This old sweep note is superseded by the later verification above. Estimate row menu is verified with `estimate-row-menu-open-1600.png`; Joseph also verified staffing plan actions are intentionally hover-revealed rather than a three-dot/dropdown menu. Do not reopen P0-012 for that staffing hover behavior. |
| P0-013 | OPEN / PARTIAL | LaborGrid code has sticky right classes on ST/OT/DT/$/TOTAL columns and total footer. No scroll-position screenshots were captured, so final visual alignment is not verified. |
| P0-014 | OPEN / PARTIAL | Estimate and staffing crew dialogs now have empty states; `DevController` seeds at least 5 crew templates across companies. Applying a crew template to both an estimate and staffing plan was not live-verified in this sweep. |
| P0-015 | OPEN / PARTIAL | Code contains 25 seeded 2025 estimates and 10 seeded 2027 staffing plans across CSL + ETS. Live CSL staffing list shows 12 plans total and visible 2027 cards with non-zero Rough Labor; if the demo stays in CSL only, verify whether 5 CSL 2027 plans is enough or whether Joseph wants all 10 under the selected company. |
| P1-SP-002 | OPEN | Staffing form has Submit for Approval button, but still exposes editable STATUS dropdown and backend accepts any status string. |
| P1-SP-003 | OPEN / PARTIAL | Current loaded CSL staffing list screenshot does not show the old `[PW-TEST]` header-only plan in the visible first page and visible cards have non-zero Rough Labor. Full DB/list search for the old record was not performed, so do not close yet. |
| P1-SP-004 | OPEN | Expired/stale unconverted staffing plan detection was not found in list or forecast logic. Revenue Forecast still counts all non-converted staffing plans in Future Revenue regardless of whether their start date is already past. |

### 2026-04-26 Codex Sweep: Claude TODO Completion Check

Build verification:

- `npm.cmd run build:dev` passed.
- `dotnet build --no-restore --configuration Release` passed with warnings only (`AutoMapper` advisory and NSwag `run` command warning).

No newly open item was safe to move to Completed in this sweep.

| ID | Verdict | Evidence |
|----|---------|----------|
| P0-001 | OPEN | Estimate frontend still PATCHes `Submitted for Approval` (`EstimateFormView.vue:589`), while backend `PatchStatus` only accepts `Submitted` (`EstimatesController.cs:485`). Forecast normalization also does not map `Submitted for Approval` to the submitted/pipeline bucket (`forecast.ts:88`, `forecast.ts:160`). |
| P0-002 | OPEN | Submit button and PDF call exist (`EstimateFormView.vue:24`, `EstimateFormView.vue:575`), but final status PATCH uses the rejected `Submitted for Approval` value. Customer-safe PDF content still needs manual/pdf verification. |
| P0-003 | OPEN / PARTIAL | Estimate list now shows Award, Lost, and Create Revision actions (`EstimateListView.vue:109`, `EstimateListView.vue:117`, `EstimateListView.vue:125`), but downstream status/reporting verification is blocked by status vocabulary mismatch and server-side Lost reason gap. |
| P0-004 | OPEN | Backend estimate status PATCH does not require `LostReason` when `Status = Lost`; it only copies the reason if supplied (`EstimatesController.cs:489`). AI can still send `lostReason: null` (`aiChatStore.ts:382`). |
| P0-005 | OPEN | Revenue Forecast calls `/api/v1/analytics/dashboard`, but still reads `analyticsResp.data?.avgMarginPct` instead of `analyticsResp.data?.kpis?.avgMarginPct` (`RevenueForecastView.vue:680`, `RevenueForecastView.vue:689`; API returns `kpis.avgMarginPct` in `AnalyticsController.cs:100`). |
| P0-006 | OPEN / PARTIAL | Q1/Q2/Q3/Q4 tabs and chart date window exist (`RevenueForecastView.vue:282`, `RevenueForecastView.vue:350`), but KPI Pending uses raw totals while chart Pipeline uses weighted `pipelineAmount`; chart also omits `staffingAmount` from displayed pipeline (`RevenueForecastView.vue:435`, `RevenueForecastView.vue:568`, `forecast.ts:220`, `forecast.ts:271`). |
| P1-007 | OPEN | `globalSetup.ts` still seeds dev data by default (`globalSetup.ts:39`), and live specs still call seed/live mutation helpers. Safe mocked mode exists via `SKIP_GLOBAL_SETUP`, but live isolation is not complete. |
| P1-008 | OPEN | Checklist still has stale wording such as manually setting status to `Submitted for Approval` and saving (`QA_REGRESSION_CHECKLIST.md:271`) despite the desired workflow being action-driven/read-only status. |
| P1-SP-002 | OPEN | Earlier sweep: Staffing Submit button existed, but form still exposed editable `STATUS` dropdown with old options (`StaffingFormView.vue:120`, `StaffingFormView.vue:594`) and status PATCH accepted any string (`StaffingPlansController.cs:235`). The old note that staffing list filter/badge omitted `Submitted for Approval` is superseded by the live sweep above; that part is now present. |
| P1-SP-003 | OPEN | Prior screenshot evidence still shows `[PW-TEST] Live SP 1777212480558` with `Rough Labor: $0`. No approved DB cleanup was performed in this sweep. |
| P1-SP-004 | OPEN | No stale/expired unconverted staffing plan detection found in staffing list or forecast logic. Unconverted staffing plans are still included if `convertedEstimateId` is empty (`forecast.ts:244`). |

## Completed

Move verified items here only after the checkbox is complete and evidence is attached.

### P0-007: Boss Demo Acceptance - AI Must Fill Complete Estimates And Staffing Plans

- [x] Verify AI can accurately create/fill both an estimate and a staffing plan from natural language.
- **Completed date:** 2026-04-26.
- **Verification evidence:** Joseph manual verification: "the agent is working flawless we can scratch them off the to do."
- **Scope closed:** AI estimate/staffing form-fill demo gate, including header fill, rates/crews/labor, and review/apply flow.
- **Regression checklist:** `QA-AI-001`, `QA-AI-002`, `QA-SP-001`, `QA-SP-005`, `QA-SP-006`.
- **Do not re-flag unless:** A later AI prompt fails, provider config changes, or form-fill/regression evidence contradicts the manual verification.

### P0-008: Boss Demo Acceptance - AI Must Answer Live DB Job Questions

- [x] Verify AI can answer jobs today, upcoming jobs, and customer/location history questions from the database.
- **Completed date:** 2026-04-26.
- **Verification evidence:** Joseph manual verification: "the agent is working flawless we can scratch them off the to do."
- **Scope closed:** AI live DB Q&A demo gate for active jobs, upcoming jobs, and customer/location/history questions.
- **Regression checklist:** `QA-AI-003`, `QA-AI-004`, `QA-AI-005`.
- **Do not re-flag unless:** A later prompt produces fabricated, missing, or source-mismatched job/revenue data.

### P0-009: Boss Demo Acceptance - Review With AI Must Work

- [x] Verify "Review with AI" produces a useful estimate/staffing review.
- **Completed date:** 2026-04-26.
- **Verification evidence:** Joseph manual verification: "the agent is working flawless we can scratch them off the to do."
- **Scope closed:** Review with AI demo gate.
- **Regression checklist:** `QA-AI-006`.
- **Do not re-flag unless:** Review with AI stops responding or returns generic/non-record-specific output.

### P0-016: AI Estimate Search Tool Must Support City/State Filtering

- [x] Verify city/state filtering works for AI customer/location estimate questions.
- **Completed date:** 2026-04-26.
- **Verification evidence:** Joseph manual verification: "the agent is working flawless we can scratch them off the to do."
- **Scope closed:** AI customer/location/date-window query behavior for demo prompts.
- **Regression checklist:** `QA-AI-005`, `QA-AI-007`.
- **Do not re-flag unless:** Customer/location prompts return records from the wrong city/state or omit matching source records.

### P0-017: AI Prompt Must Route Active And Upcoming Job Questions To DB Tools

- [x] Verify AI active and upcoming jobs prompts work for the demo.
- **Completed date:** 2026-04-26.
- **Verification evidence:** Joseph manual verification: "the agent is working flawless we can scratch them off the to do."
- **Scope closed:** "Jobs going on today" and "jobs coming up" demo prompt behavior.
- **Regression checklist:** `QA-AI-003`, `QA-AI-004`, `QA-AI-008`.
- **Do not re-flag unless:** Active/upcoming job answers stop matching start/end date source data.

### P0-018: AI Provider Config Must Be Demo-Ready Without Exposing Secrets

- [x] Verify local AI provider configuration is present and usable.
- **Completed date:** 2026-04-26.
- **Verification evidence:** Joseph manual verification that the agent is working flawlessly.
- **Scope closed:** AI provider readiness for the demo path. No secret value was exposed in this document.
- **Regression checklist:** `QA-AI-009`.
- **Do not re-flag unless:** AI endpoint starts failing, provider/model config changes, or a key appears missing/placeholder.

### P0-019: Exact Boss Demo AI Prompt Pack Must Pass

- [x] Verify Joseph's exact demo prompt pack.
- **Completed date:** 2026-04-26.
- **Verification evidence:** Joseph manual verification: "the agent is working flawless we can scratch them off the to do."
- **Scope closed:** DOW estimate prompt, P66 estimate/per diem prompt, upcoming jobs prompt, jobs today prompt, quarter revenue prompt, and Shell-this-year prompt.
- **Regression checklist:** `QA-AI-010`, `QA-AI-011`, `QA-AI-012`, `QA-AI-013`.
- **Do not re-flag unless:** One of the exact boss-demo prompts fails in testing or during rehearsal.

### P1-SP-001: Staffing Plan Missing Full Job Cost Analysis Section

- [x] Replace simplified 3-card "Rough Cost Analysis" with full Job Cost Analysis component.
- **Problem:** Staffing plan only showed Rough Revenue / Rough Internal Cost / Rough Margin as 3 plain number cards. Estimates had the full `<JobCostAnalysis>` component with burden rate badges and a per-position cost breakdown table; staffing plans did not.
- **Impact:** Users could not see burden rate detail or per-position cost breakdown on staffing plans, making the plan less useful for management review.
- **Files verified:** `webapp/src/modules/estimating/features/staffing-form/views/StaffingFormView.vue`, `webapp/src/modules/estimating/features/estimate-form/components/JobCostAnalysis.vue`.
- **Expected behavior:** When labor rows exist, staffing plan shows the same `<JobCostAnalysis>` component as the estimate form, plus a Summary section (Rough Revenue / Internal Cost / Gross Profit / Gross Margin %).
- **Evidence:** Screenshot `webapp/test-results/qa/QA_AUDIT_20260426_staffing/staffing-form.png` shows `INTERNAL USE ONLY`, `LABOR BURDEN APPLIED`, burden chips, per-position cost table, and Plan Summary on staffing plan `SP-S-26-0018-CVX`. Code evidence: `StaffingFormView.vue:278`, `JobCostAnalysis.vue:23`, `JobCostAnalysis.vue:35`, `JobCostAnalysis.vue:89`. `npm.cmd run build:dev` passed. `dotnet build --no-restore --configuration Release` passed with warnings only.
- **Regression checklist:** `QA-SP-008`.

## Accepted As-Is

Move items here only after Joseph explicitly accepts the current behavior. Include:

- **Decision date**
- **Accepted behavior**
- **Rationale**
- **Evidence reviewed**
- **Do not re-flag unless**

Accepted items are not defects. Subagents should report them as `ACCEPTED AS-IS`, not `FAIL`, unless the implementation changes or new evidence shows the accepted behavior is causing a different risk.

## Out Of Scope Unless Reopened By Joseph

These completed demo gates were manually verified by Joseph and must not be re-audited in routine sweeps:

- P0-007: AI fills complete estimates and staffing plans.
- P0-008: AI answers live DB job questions.
- P0-009: Review with AI works.
- P0-016: AI estimate search supports city/state filtering.
- P0-017: AI routes active/upcoming job questions to DB tools.
- P0-018: AI provider config is demo-ready.
- P0-019: Exact boss demo AI prompt pack passes.

Only reopen these if Joseph explicitly asks, the AI/provider/tool code changes, or a fresh demo prompt fails.


