# Implementation Worklog
## Platform Expansion: Portal + Planning + Scheduling

---

## 2026-04-29 — Batch 2: Frontend Foundation / Design System

**Branch:** `refactor/pm-scheduling-foundation`

**What changed:**

### Design system — `webapp/src/ui/`
- `webapp/src/ui/composables/useFormatters.ts` — `fmtDate`, `fmtDateLong`, `fmtCurrency`, `fmtCurrencyZero`
- `webapp/src/ui/utils/severity.ts` — 11 domain-specific severity functions + generic fallback:
  `projectStatusSeverity`, `workOrderStatusSeverity`, `planStatusSeverity`, `workPackageStatusSeverity`,
  `fcoStatusSeverity`, `assignmentStatusSeverity`, `phaseStatusSeverity`, `taskStatusSeverity`,
  `stepStatusSeverity`, `milestoneStatusSeverity`, `sourceTagSeverity`, `statusSeverity`
- `webapp/src/ui/index.ts` — barrel re-export for all ui utilities

### Module service layers
- `webapp/src/modules/planning/services/usePlanningService.ts` — wraps all 22 planning API calls
  (projects, work orders, step-out plans, work packages, FCOs)
- `webapp/src/modules/scheduling/services/useSchedulingService.ts` — wraps all 17 scheduling API calls
  (dashboard, jobs, resources, certifications, assignments, coverage, roll-off)

**Build result:** `npm run build` — ✓ built in 4.63s, 0 errors
**Architecture checks:** `run-all-checks.sh` — PASSED (ARCH-DS-001 now PASS; frontend ARCH-FE violations remain in warn mode until Batch 3-4)

---

## 2026-04-29 — Batch 1: Backend Foundation Cleanup + Guardrails

**Branch:** `refactor/pm-scheduling-foundation` (off `feat/pm-module` @ `03c6d8a`)

**What changed:**

### AppDbContext configuration split
- `Data/AppDbContext.cs` — `OnModelCreating` replaced with `ApplyConfigurationsFromAssembly`
- 55 `IEntityTypeConfiguration<T>` classes created across 4 subdirectories:
  - `Data/Configurations/Core/` — 9 entity configs (User, Company, Role, etc.)
  - `Data/Configurations/Estimating/` — 21 entity configs (Estimate, RateBook, CostBook, etc.)
  - `Data/Configurations/Planning/` — 20 entity configs (Project, WorkOrder, FcoDocument, etc.)
  - `Data/Configurations/Scheduling/` — 5 entity configs (Resource, Assignment, etc.)
- No migration generated (structural change only; no schema diff)

### Business logic extraction
- `Api/Services/Planning/FcoDocumentService.cs` — FCO HTML document generation extracted from PlanningController
- `Api/Services/WorkOrders/WorkOrderFinancialService.cs` — Financial calculation extracted from WorkOrderController

### API contracts
- `Api/Contracts/Common/StatusUpdateRequest.cs` — moved out of CommercialAuthorizationController
- `Api/Contracts/Projects/` — `CreateProjectRequest`, `UpdateProjectRequest`
- `Api/Contracts/WorkOrders/` — `CreateWorkOrderRequest`, `UpdateWorkOrderRequest`
- `Api/Contracts/Planning/` — 7 records (StepOutPlan, StepOutStep, WorkPackage, FcoDocument)
- `Api/Contracts/Scheduling/` — 6 records (Resource, Assignment, Certification, AvailabilityBlock)

### Controller rewrites (contract-based [FromBody])
- `ProjectController.cs` — Create/Update use contract records
- `WorkOrderController.cs` — Create/Update use contract records; GetFinancials delegates to service
- `PlanningController.cs` — 7 endpoints use contract records; GenerateFcoDocument delegates to service
- `SchedulingController.cs` — 6 endpoints use contract records
- `CommercialAuthorizationController.cs` — StatusUpdateRequest moved to Contracts.Common

### Guardrails + automation
- `docs/ARCHITECTURE_GUARDRAILS.md` — canonical rules document (9 backend + 6 frontend + 2 design system rules)
- `tools/architecture-checks/check-backend.sh` — ARCH-BE-001 through ARCH-BE-009 (hard-fail)
- `tools/architecture-checks/check-frontend.sh` — ARCH-FE-001 through ARCH-FE-006 + DS-001 (warn mode until Batch 3-4)
- `tools/architecture-checks/run-all-checks.sh` — master runner; exits 0 ✓

**Build result:** `dotnet build` — 0 errors (1 pre-existing CS8604 warning in Program.cs)
**Migration check:** `dotnet ef migrations list` — no pending migrations
**Architecture checks:** `run-all-checks.sh` — PASSED (17 backend PASSes, 1 DevController WARN expected)

**Deferred to future batch:**
- DevController seeder extraction (PmLifecycleSeedService, SchedulingSeedService) — ARCH-BE-009 currently warn-only
- CommercialAuthorizationController full contract refactor
- Frontend refactor (Batches 2-4)

---

## 2026-04-28 — Phase 0: Analysis + Docs

**What changed:** Created platform expansion documentation.

**Files created:**
- `docs/LIVE_TODO.md`
- `docs/IMPLEMENTATION_WORKLOG.md` (this file)
- `docs/REGRESSION_CHECKLIST.md`
- `docs/TEST_RUN_LOG.md`
- `docs/PORTAL_PLANNING_SCHEDULING_ARCHITECTURE.md`
- `docs/DOMAIN_RESEARCH_NOTES.md`

**Verified repo facts:**
- Api/, Data/, Shared/, webapp/ — top-level structure confirmed
- AppDbContext at `Data/AppDbContext.cs`
- Migrations at `Data/Migrations/`
- DBInitializer at `Data/DBInitializer.cs` — static class, SeedRoles/SeedCompanies/SeedDefaultUsers, each gated by AnyAsync()
- DB bootstrap in `Api/Program.cs` lines 177–183: `context.Database.Migrate()` then `DbInitializer.Initialize(context)`
- apps.ts only registers `estimating`
- Router at `webapp/src/router/index.ts` — `/` redirects to `/estimating/estimates`
- App type defined in `webapp/src/types.d.ts`
- NSwag generates `webapp/src/apiclient/client.ts` from `Api/nswag.json`
- No test projects, no design-time factory
- Estimate statuses: Draft, Pending, Awarded, Lost, Canceled
- `StaffingPlan.ConvertedEstimateId` — nullable int (null = not converted)

**Regressions checked:** N/A (docs only)

**Next:** Phase 1 — portal shell + app registration

---

## 2026-04-28 — Phase 1: Portal Shell + App Registration

**What changed:** Added portal/planning/scheduling apps to the platform.

**Files changed:**
- `webapp/src/apps.ts` — added portal, planning, scheduling entries
- `webapp/src/router/index.ts` — added /portal, /planning, /scheduling routes; changed default redirect to /portal
- `webapp/src/modules/portal/` — new module (router + PortalDashboardView)
- `webapp/src/modules/planning/` — new module shell (router + placeholder views)
- `webapp/src/modules/scheduling/` — new module shell (router + placeholder views + SchedulingDashboardView)
- `Api/Controllers/PortalController.cs` — new controller with GET /api/v1/portal/dashboard

**Migration names added:** None (Phase 1 is frontend + thin backend only)

**Commands run:**
- `cd webapp && npm run build` — PENDING
- `cd Api && dotnet build` — PENDING

**Test results:** PENDING

**Regressions checked:** PENDING

**Next:** Phase 2 — EF bootstrap cleanup

---

## 2026-04-28 14:34:58 -05:00 — Codex QA Intake: Project Lifecycle Operating Model

**What changed:** Added QA coverage for Joseph's expanded end-to-end project lifecycle requirements before Claude starts any lifecycle implementation.

**Why:** The new scope adds commercial authorization, projects, work orders, FCO control, actuals, variance, stage gates, closeout, and lessons learned. These need explicit audit gates so Claude cannot treat them as generic planning placeholders.

**Files touched:**
- `docs/LIVE_QA_TODO.md`
- `docs/QA_REGRESSION_CHECKLIST.md`
- `docs/PLATFORM_REQUIREMENTS_TRACEABILITY.md`

**Migration names added:** None.

**Commands run:**
- `git branch --show-current`
- `rg -n "PLATFORM-024|PLATFORM-025|PLATFORM-026|PLATFORM-027|QA-LIFE|QA-ACT-002|QA-FCO-003|QA-PLAT-010" docs`
- `Get-Content docs\PLATFORM_REQUIREMENTS_TRACEABILITY.md`

**Test results:** No build/test run. Docs-only QA scope update.

**Regressions checked:** Added/verified traceability for `QA-LIFE-001` through `QA-LIFE-006`, `QA-ACT-002`, `QA-FCO-003`, and `QA-PLAT-010`.

**Regressions found/fixed:** Fixed the audit gap where the new lifecycle operating model was not yet mapped to live QA gates and regression IDs.

**Blockers:** Claude's lifecycle docs are not created/verified yet. No implementation should be accepted against these lifecycle requirements until the requested docs exist and are repo-grounded.

**Next step:** Audit Claude's `PROJECT_LIFECYCLE_MASTER_PLAN.md`, ownership/traceability docs, stage/status model, actuals/variance model, and updated planning TODO against `PLATFORM-024` through `PLATFORM-027`.

---

## 2026-04-28 14:48:54 -05:00 — Codex QA Intake: Developer Handoff Cleanup

**What changed:** Added a QA gate for Joseph's doc review requiring a clean developer handoff index and reconciliation of stale planning docs.

**Why:** Claude's lifecycle docs are strong, but `PROJECT_PLANNING_MASTER_PLAN.md` and `PROJECT_PLANNING_DATA_MODEL.md` still conflict with the newer lifecycle source of truth. The dev team needs one canonical reading order and no equal-truth stale docs.

**Files touched:**
- `docs/LIVE_QA_TODO.md`
- `docs/QA_REGRESSION_CHECKLIST.md`
- `docs/PLATFORM_REQUIREMENTS_TRACEABILITY.md`

**Migration names added:** None.

**Commands run:**
- `Get-ChildItem docs -Filter "PROJECT*.md" | Select-Object Name,Length,LastWriteTime`
- `rg -n "PLATFORM-028|QA-LIFE-007|PROJECT_HANDOFF_INDEX|ProjectPlan|TaskActual|StepOutSubStep|Gantt" docs\LIVE_QA_TODO.md docs\QA_REGRESSION_CHECKLIST.md docs\PLATFORM_REQUIREMENTS_TRACEABILITY.md`

**Test results:** No build/test run. Docs-only QA scope update.

**Regressions checked:** Added `PLATFORM-028` and `QA-LIFE-007` for canonical handoff package verification.

**Regressions found/fixed:** Captured the doc drift risk around `ProjectPlan` vs `Project`, `TaskActual` vs `ActualEntry`, Gantt implementation approach, and `StepOutSubStep` MVP scope.

**Blockers:** `docs/PROJECT_HANDOFF_INDEX.md` does not exist yet. Stale docs remain unsafe as equal source-of-truth until Claude performs the requested doc-only consolidation pass.

**Next step:** Audit Claude's handoff cleanup output and verify the canonical stack before any lifecycle implementation begins.

---

## 2026-04-29 — Batch 2: PM + Scheduling Density Sweep (Phase 1)

**Estimating impact: NO**

**What changed:** Applied `ent-grid` / `ent-grid-clickable` standard to all planning and scheduling DataTables that were missing it. Converted link-in-cell navigation patterns to DataTable `@row-click` where applicable.

**Files changed:**
- `webapp/src/modules/planning/views/StepOutPlanListView.vue` — added `ent-grid ent-grid-clickable`, `@row-click` for plan navigation; removed `<a class="plan-link">` column body; removed redundant arrow-right Button; added `@click.stop` to trash button; removed unused `.plan-link` CSS
- `webapp/src/modules/planning/views/FcoListView.vue` — added `ent-grid ent-grid-clickable`, `@row-click` → `openDetail()`; removed redundant `pi-eye` Button; added `@click.stop` to `pi-file` (Generate Document) Button
- `webapp/src/modules/scheduling/views/SchedulingDashboardView.vue` — added `ent-grid` to Active Demand DataTable
- `webapp/src/modules/planning/views/StepOutPlanFormView.vue` — added `ent-grid` to Steps DataTable; added `@click.stop` to pencil/trash step action buttons; added `ent-grid ent-grid-clickable` + `@row-click` to Generated Work Packages DataTable (navigates to `/planning/work-packages/:id`)

**CoverageView.vue — D-04 VOID:** Confirmed view uses custom `CraftCoverageBar` components, not a DataTable. No `ent-grid` applicable. Single-click/dblclick interaction pattern is correct for this visualization view. No change made.

**Build result:** `npm run build:dev` — PASS (✓ built in 5.47s, zero errors)

**Defects addressed:**
- D-01 StepOutPlanListView — FIXED PENDING REVIEW
- D-02 FcoListView — FIXED PENDING REVIEW
- D-03 SchedulingDashboardView Active Demand — FIXED PENDING REVIEW
- D-04 CoverageView — VOID (no DataTable)
- D-05 StepOutPlanFormView Steps + WP tables — FIXED PENDING REVIEW
- DD-01 StepOutPlanListView link-in-cell — FIXED PENDING REVIEW
- DD-02 FcoListView pi-eye dead button — FIXED PENDING REVIEW

**Remaining open defects (Phase 2+):**
- DD-03 ProjectDetailView WO tab link-in-cell
- DD-04 WorkOrderDetailView step-out plans tab plan-link
- DD-05 JobsBoardView button-in-cell
- DD-06 AssignmentsView button-in-cell
- DD-07 RollOffView button-in-cell
- Numbering: PRJ-DEMO-*, WO-DEMO-*, FCO-DEMO-* (Phase 3)

**Next:** Codex verify Batch 2 on all 5 routes, then Phase 2 dead-end fixes on approval.

---

## 2026-04-28 14:56:29 -05:00 — Codex Doc Cleanup: Canonical Developer Handoff

**What changed:** Completed the developer handoff cleanup after Joseph explicitly authorized Codex to perform doc cleanup.

**Why:** The lifecycle docs were strong, but stale planning docs and canonical UI/TODO references still contained old terms that would confuse implementation.

**Files touched:**
- `docs/PROJECT_PLANNING_MASTER_PLAN.md`
- `docs/PROJECT_PLANNING_DATA_MODEL.md`
- `docs/PROJECT_HANDOFF_INDEX.md`
- `docs/PROJECT_PLANNING_MASTER_TODO.md`
- `docs/PROJECT_PLANNING_UI_MAP.md`
- `docs/PROJECT_LIFECYCLE_MASTER_PLAN.md`
- `docs/LIVE_QA_TODO.md`
- `docs/QA_REGRESSION_CHECKLIST.md`

**Migration names added:** None.

**Commands run:**
- `rg -n "ProjectPlan|TaskActual|dhtmlx-gantt|/api/v1/project-plans|ProjectPlanView|ProjectPlans|DbSet<ProjectPlan>|DbSet<TaskActual>" ...`
- `rg -n "StepOutSubStep|ProjectDetailView|ActualEntry|/api/v1/projects|Custom in-house SVG|useCalendarGrid|CommercialAuthorization|WorkOrder" ...`
- `Test-Path` checks for all handoff-index docs.

**Test results:** Docs-only verification passed. No backend/frontend build run.

**Regressions checked:** `PLATFORM-028`, `QA-LIFE-007`, `QA-PLAT-003`.

**Regressions found/fixed:** Resolved active canonical references to `ProjectPlanView`, `/api/v1/project-plans`, `ProjectPlans`, and `TaskActuals`; tombstoned stale docs; created handoff index.

**Remaining allowed references:** `ProjectPlan`, `TaskActual`, and `dhtmlx-gantt` remain only in archived/superseded/conflict context, not as active implementation guidance.

**Next step:** Claude can implement from `docs/PROJECT_HANDOFF_INDEX.md`; Codex should audit implementation against the canonical stack.

---

## 2026-04-29 — Codex QA Sweep: PM + Scheduling Full Audit

**What changed:** Codex performed a QA-only PM/Scheduling audit in manual polling mode and created a Project-app evidence packet.

**Why:** Joseph assigned Codex as QA/audit/regression authority and required a full density, functionality, logic, numbering, drill-down, and Playwright proof pass across Planning/PM and Scheduling.

**Files touched by Codex QA:**
- `docs/QA_EVIDENCE_20260429_PM_SCHED_FULL.md`
- `docs/qa-evidence/QA_AUDIT_20260429_PM_SCHED_FULL/**`
- `docs/LIVE_QA_TODO.md`
- `docs/QA_REGRESSION_CHECKLIST.md`
- `docs/TEST_RUN_LOG.md`

**Product code changed:** No.

**Commands / tools run:**
- canonical docs and QA docs read
- port/process inspection for `7310/7311/7210/7211`
- QA-only Playwright script `webapp/test-results/qa/QA_AUDIT_20260429_PM_SCHED_FULL/pm-scheduling-audit.mjs`
- read-only subagent audits: UI Explorer, Logic Auditor, Regression Guard

**Test results:** FAIL WITH FINDINGS. 15 PM/Scheduling routes/screens checked; 14 route-level findings plus backend logic defects were recorded in `docs/LIVE_QA_TODO.md`.

**Regressions found:** Existing Playwright config points at old repo ports `7210/7211`; PM/Scheduling density is inconsistent; jobs board lacks Forecast/Released demand-state proof; backend gates for WorkOrder/FCO/assignment release are too soft.

**Next step:** Waiting for Claude fixes. Codex must rerun the same Project-app `7310/7311` audit after Claude reports changes and Joseph approves closure.

---

## 2026-04-29 - Codex QA Guardrail Update: PM/Scheduling Foundation Cleanup

**What changed:** Codex updated the QA/todo/regression docs so future PM/Scheduling cleanup batches are audited for architecture and UI-system drift, not only route clicks.

**Why:** Joseph paused feature work and required permanent guardrails for the `refactor/pm-scheduling-foundation` cleanup: service-layer API access, shared UI primitives, thin controllers, DTO/contracts, split EF configuration, seeding/bootstrap ownership, architecture checks, and tester enforcement.

**Files touched by Codex QA:**
- `docs/LIVE_QA_TODO.md`
- `docs/QA_REGRESSION_CHECKLIST.md`
- `docs/TEST_RUN_LOG.md`
- `docs/IMPLEMENTATION_WORKLOG.md`

**Product code changed:** No.

**Commands / tools run:**
- Read current QA docs and existing PM-SCHED findings.
- Updated QA docs only.

**Test results:** Docs-only QA operating update. No app build or browser run was required for this doc change.

**Regressions added for enforcement:** `ARCH-001` through `ARCH-008` in `LIVE_QA_TODO.md`; linked existing `QA-ARCH-001` through `QA-ARCH-008` and added unique `QA-ARCH-009` through `QA-ARCH-011`, `QA-UI-013`, `QA-UI-014`, and backend architecture checks in `QA_REGRESSION_CHECKLIST.md`.

**Next step:** When Claude creates `refactor/pm-scheduling-foundation`, Codex must audit each batch against the new architecture lane and fail the branch for forbidden PM/Scheduling view API calls, duplicate helpers, raw EF request bodies, inline DbContext config, UI-system bypass, or missing architecture-check scripts.

---

## 2026-04-29 - Codex QA Guardrail Update: Cleanup Continuation Batches 6-10

**What changed:** Codex recorded Joseph's review that the pushed `refactor/pm-scheduling-foundation` branch is improved but not accepted as Batches 1-5 complete.

**Why:** Joseph identified unfinished foundation work: `DevController` monolith cleanup, startup/bootstrap verification, a real shared UI component system, PM/Scheduling view refactor onto shared primitives, feature/domain service split, and stronger tester/Codex enforcement.

**Files touched by Codex QA:**
- `docs/LIVE_QA_TODO.md`
- `docs/QA_REGRESSION_CHECKLIST.md`
- `docs/TEST_RUN_LOG.md`
- `docs/IMPLEMENTATION_WORKLOG.md`

**Product code changed:** No.

**Commands / tools run:**
- Read current branch status and QA docs.
- Updated QA docs only.

**Test results:** Docs-only QA operating update. No app build or browser run was required for this doc change.

**Regressions added for enforcement:** `ARCH-009` through `ARCH-014` in `LIVE_QA_TODO.md`; `QA-ARCH-012` through `QA-ARCH-015`, `QA-UI-015`, and `QA-UI-016` in `QA_REGRESSION_CHECKLIST.md`.

**Next step:** Codex must fail cleanup closure until Batches 6-10 are evidenced: thin `DevController`, verified bootstrap, real shared UI system used by PM/Scheduling, feature-split services, hard-fail architecture checks, passing builds, route smoke, and Project-app QA evidence.
