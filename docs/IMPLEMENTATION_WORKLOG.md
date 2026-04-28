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
