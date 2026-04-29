# Implementation Worklog
## Platform Expansion: Portal + Planning + Scheduling

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
