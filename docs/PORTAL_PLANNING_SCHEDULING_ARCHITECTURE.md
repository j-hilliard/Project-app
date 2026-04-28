# Portal + Planning + Scheduling — Architecture

## Platform Vision

One login. One portal. Three apps. Same repo, same stack.

```
User logs in → Company select → Portal Dashboard
                                     ↓
                    ┌────────────────┼────────────────┐
                    ▼                ▼                ▼
              Estimating         Planning         Scheduling
              (existing)          (new)             (new)
```

---

## Technology Stack

| Layer | Technology |
|-------|-----------|
| Frontend | Vue 3 + TypeScript + Pinia + PrimeVue |
| Backend | C# .NET + ASP.NET Core Web API |
| ORM | Entity Framework Core Code First |
| Auth | JWT (2-step: login → select-company) |
| API Client | NSwag auto-generated TypeScript (webapp/src/apiclient/client.ts) |
| Database | SQL Server (EF migrations as source of truth) |

---

## Repo Structure

```
Api/                          ← C# backend
  Controllers/                ← All API controllers (IDbContextFactory pattern)
  Services/                   ← Business services
  Program.cs                  ← Startup, auth, NSwag, DB bootstrap (lines 177-183)
  nswag.json                  ← Generates webapp/src/apiclient/client.ts
Data/
  AppDbContext.cs             ← Single DbContext, all entities
  Migrations/                 ← EF migrations
  DBInitializer.cs            ← Seed logic (SeedRoles, SeedCompanies, SeedDefaultUsers)
  Bootstrap/                  ← NEW: DatabaseBootstrapper, ReferenceDataSeeder, DemoDataSeeder
Shared/
  Enumerations/               ← AuthorizationRole enum
webapp/src/
  apps.ts                     ← App registry (portal, estimating, planning, scheduling)
  types.d.ts                  ← App, AppRoute, RouteMeta types
  router/index.ts             ← Main router
  stores/appStore.ts          ← currentApp + menu computed from apps.ts
  modules/
    portal/                   ← NEW: portal dashboard
    estimating/               ← EXISTING: full estimating module
    planning/                 ← NEW: planning/PM module
    scheduling/               ← NEW: scheduling module
```

---

## App Boundaries

### Estimating (EXISTING — do not break)
- Estimates, staffing plans, rate/cost books, bid workflow
- Commercial pricing truth
- Forecast inputs

### Planning (NEW)
- Step-out plans (execution sequencing with decimal step numbering)
- Work packages (schedulable operational demand)
- FCO / Change Orders (with signable document generation)
- Actuals capture (labor, material, equipment)
- Estimate/FCO delta + variance reporting

### Scheduling (NEW)
- Resources/people (name, craft, branch, certs)
- Assignments (resource → job, dates, craft, shift)
- Availability blocks (PTO, blackout)
- Coverage calculation (demand vs assigned)
- Conflict detection (double-booking, unavailable, uncertified)
- Roll-off / ending-soon / available-soon
- Suggested next-job matching

### Portal
- Landing dashboard at `/`
- App tile launcher
- Summary KPI widgets (reads from all three apps via thin endpoints)
- Alert surface

---

## Data Flow

```
Estimating → Planning → Scheduling → Actuals/Variance → Portal/Forecast

1. Estimating: create estimate or staffing plan (commercial baseline)
2. Planning: create step-out plan linked to estimate/staffing/FCO
             → generate work packages
3. Scheduling: work packages + estimates + unconverted staffing plans = demand
               → assign resources to demand
4. Actuals: record actual labor/material/equipment against work packages
            → compute delta vs estimate/FCO baseline
5. Portal: aggregate summaries from all three domains
```

---

## Hard Data Rules

### Scheduling Demand Sources
```sql
-- Estimates (active work)
SELECT * FROM Estimates
WHERE CompanyCode = @company
  AND Status IN ('Awarded', 'Pending')

UNION ALL

-- Approved unconverted staffing plans (future demand)
SELECT * FROM StaffingPlans
WHERE CompanyCode = @company
  AND ConvertedEstimateId IS NULL
  AND Status = 'Approved'

UNION ALL

-- Work packages ready for scheduling
SELECT * FROM WorkPackages
WHERE CompanyCode = @company
  AND ReadyForScheduling = 1
```

### Dedupe Rule
`StaffingPlan.ConvertedEstimateId IS NOT NULL` → plan excluded from demand. Only the linked estimate counts. Enforced in `SchedulingDemandService`.

### No Unsafe Cross-Domain Writes
- Planning reads Estimating data via adapters/source links
- Scheduling reads Estimating + Planning data via adapters
- Actuals stored in Planning-owned tables (WorkPackageActualLabor, etc.)
- No controller mutates estimate/staffing-plan rows from planning or scheduling

---

## EF Bootstrap Pattern

### Target State (Phase 2)
```csharp
// Program.cs
if (config.GetValue<bool>("Database:AutoMigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var bootstrapper = scope.ServiceProvider.GetRequiredService<DatabaseBootstrapper>();
    await bootstrapper.RunAsync(ct);
}

// DatabaseBootstrapper.cs
public async Task RunAsync(CancellationToken ct)
{
    await _db.Database.MigrateAsync(ct);           // create + apply all pending
    await _referenceSeeder.SeedAsync(_db, ct);      // crafts, roles, companies
    if (_config.GetValue<bool>("Database:SeedDemoData"))
        await _demoSeeder.SeedAsync(_db, ct);       // dev-only demo data
}
```

### Config Flags
```json
// appsettings.json (production default)
"Database": {
  "AutoMigrateOnStartup": true,
  "SeedReferenceData": true,
  "SeedDemoData": false
}

// appsettings.Development.json
"Database": {
  "SeedDemoData": true
}
```

### Current State
- `DBInitializer.cs` is a static class
- SeedRoles + SeedCompanies = reference data (fine as-is)
- SeedDefaultUsers = demo data (needs env gating in Phase 2)

---

## NSwag Client Generation

After adding any new API controller/endpoint:
```bash
cd Api && nswag run nswag.json
```
This regenerates `webapp/src/apiclient/client.ts`. Never manually edit that file.

---

## Migration Commands

```bash
# Add migration (run from Data/ project)
cd Data && dotnet ef migrations add <MigrationName> --startup-project ../Api

# Apply migrations manually
cd Data && dotnet ef database update --startup-project ../Api
```

---

## Step Numbering Design

Step-out plan steps use decimal-like numbering to support inserting steps between existing ones:
- `StepCode` (string): "1", "1.2", "1.5", "1.7", "2" — display value
- `SortOrder` (decimal): 1.0, 1.2, 1.5, 1.7, 2.0 — sort key

This allows inserting step "1.3" between "1.2" and "1.5" without renumbering all steps.

---

## FCO Document Structure

A signable FCO must include:
- Project/contract info
- Estimate/job reference
- Client info
- FCO number + date
- Requested by / prepared by
- Changed scope description
- Reason/basis for change
- Schedule impact (added days)
- Labor breakdown
- Material breakdown
- Equipment breakdown
- Markup/overhead/profit
- Updated contract/estimate value
- Approval/signature fields
- Document status (Draft/Sent/Approved/Rejected/Signed)
- Revision history

---

## Open Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| No test projects | Low confidence in regressions | Add xUnit project in Phase 7 |
| NSwag post-build warning in CI | May block client sync | Verify nswag command after adding controllers |
| DBInitializer seed in production | Demo users created in prod if not gated | Phase 2 fix: SeedDemoData flag |
| Large migration count | Schema changes become risky | Keep migrations small and focused |
| AppLayout AiChatSidebar shows on all routes | Minor UX noise on portal/planning/scheduling | Handle in Phase 7 refinement |
