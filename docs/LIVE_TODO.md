# Live Implementation TODO
## Platform Expansion: Portal + Planning + Scheduling

Last updated: 2026-04-28

---

## Active Phase: PHASE 1 — Portal Shell + App Registration

### Current Task
Creating portal module, planning/scheduling shells, PortalController

### In Progress
- [x] Phase 0: Docs created
- [ ] Phase 1: apps.ts updated with portal/planning/scheduling
- [ ] Phase 1: router/index.ts updated (new routes + redirect to /portal)
- [ ] Phase 1: Portal module created (PortalDashboardView)
- [ ] Phase 1: Planning module shell created
- [ ] Phase 1: Scheduling module shell created
- [ ] Phase 1: PortalController.cs created
- [ ] Phase 1: Frontend build passes
- [ ] Phase 1: API build passes

### Next 3 Tasks After Phase 1
1. Phase 2: EF bootstrap cleanup (DatabaseBootstrapper, split reference/demo seed)
2. Phase 3: Planning entities + migration (StepOutPlan, StepOutStep, WorkPackage, FcoDocument)
3. Phase 4: Scheduling entities + demand adapter (Resource, Assignment, SchedulingDemandService)

### Blocked Items
- None currently

### Files Being Changed Now
- `webapp/src/apps.ts`
- `webapp/src/router/index.ts`
- `webapp/src/modules/portal/` (new)
- `webapp/src/modules/planning/` (new)
- `webapp/src/modules/scheduling/` (new)
- `Api/Controllers/PortalController.cs` (new)

### Tests to Run After This Chunk
- `cd webapp && npm run build` (frontend compile check)
- `cd Api && dotnet build` (backend compile check)
- Manual: navigate to /portal, /planning, /scheduling in browser
- Manual: verify /estimating/* routes still work

---

## Upcoming Phases

| Phase | Description | Status |
|-------|-------------|--------|
| 0 | Analysis + Docs | DONE |
| 1 | Portal Shell + App Registration | IN PROGRESS |
| 2 | EF Bootstrap Cleanup | PENDING |
| 3 | Planning Foundation (entities, endpoints, UI) | PENDING |
| 4 | Scheduling Foundation (entities, demand adapter, UI) | PENDING |
| 5 | Actuals + Rate/Cost + Delta + FCO | PENDING |
| 6 | Coverage / Alerts / Conflicts / Roll-Off | PENDING |
| 7 | Refinement + Tests + Docs | PENDING |
