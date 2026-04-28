# Scheduling App — Step-Out Architecture Plan

**Created:** 2026-04-28  
**Status:** Approved — Implementation in progress

---

## Executive Summary

The Scheduling app turns demand (estimates, staffing plans, work packages) into named resource assignments. It surfaces coverage gaps, conflict detection, and roll-off forecasting. All entities are already modeled and migrated. All API controllers and endpoints are functional. The work remaining is:

1. Three new backend services: conflict detection, coverage calculation, suggested matching
2. Demo seed data for resources, certifications, and assignments
3. Real KPI data in portal and scheduling dashboards
4. Six frontend views to replace stubs

---

## Current-State Architecture

### What Is Done
- `Data/Models/Scheduling/` — Resource, Assignment, Craft, Certification, AvailabilityBlock — modeled and migrated
- `Api/Controllers/SchedulingController.cs` — all CRUD endpoints functional
- `Api/Services/SchedulingDemandService.cs` — demand deduplication complete
- `webapp/src/modules/scheduling/router/index.ts` — 6 routes registered
- `webapp/src/apiclient/client.ts` — NSwag-generated, includes all scheduling endpoints
- 4 apps registered in `apps.ts`: portal, estimating, planning, scheduling

### What Is Stub
All 6 scheduling views display "Coming Soon" placeholders. Portal dashboard KPIs are hardcoded to 0. No scheduling demo data exists.

---

## Recommended Target Architecture

### Data Flow
```
Demand Sources                  Services                    Views
──────────────                  ────────                    ─────
Estimates (Awarded/Pending) ─┐
StaffingPlans (Approved,    ─┤─ SchedulingDemandService ──► Jobs Board
  not converted)              │
WorkPackages (ReadyForSched) ┘

Resources + Assignments ──────── CoverageCalculationService ─► Coverage View
                                 SuggestedMatchService ───────► Roll-Off View
                                 AssignmentConflictService ───► Assignments View
```

### Multi-Tenancy
All endpoints filter by `CompanyCode` from JWT claims. Never pass company code from frontend — backend extracts from token.

### Conflict Detection Rules
- **Double-booking:** resource already has active assignment with overlapping dates
- **Unavailability:** resource has AvailabilityBlock covering the assignment period
- **Certification mismatch:** craft requires cert that resource doesn't hold or is expired

Conflicts are **advisory** — the dispatcher may override them. Hard blocking would prevent valid scheduling scenarios.

---

## Portal Structure

The portal dashboard (`/portal`) already calls `GET /api/v1/portal/dashboard`. The response shape includes `craftShortagesCount`, `jobsEndingSoonCount`, `peopleFreeSoonCount`, `alertCount`. Once backend services populate these fields with real data, the portal will automatically show real numbers — no frontend portal changes needed.

---

## Integration Approach

### Frontend → Backend
- Frontend uses NSwag-generated `SchedulingClient` from `webapp/src/apiclient/client.ts`
- All views use `useApiStore()` from `@/stores/apiStore.ts` to get the authenticated axios instance
- Pattern: `const client = new SchedulingClient(BASE_URL, axiosInstance)`

### Backend Service Registration
All three new services registered as `Scoped` in `Program.cs`, injected into `SchedulingController` and `PortalController` via constructor.

---

## Domain Model

### Scheduling Entities (all migrated)
| Entity | Key Fields | Purpose |
|--------|-----------|---------|
| Resource | ResourceId, CompanyCode, Name, CraftCode, Branch, IsActive | A schedulable person |
| Assignment | AssignmentId, ResourceId, JobSourceType, JobSourceId, Start, End, Shift, Status | Assigns person to job |
| Craft | CraftCode, Title, IsDirect | Master list of crafts |
| Certification | CertId, ResourceId, Type, ExpirationDate | Cert the person holds |
| AvailabilityBlock | BlockId, ResourceId, Start, End, Reason | Period when person is unavailable |

### Assignment Polymorphism
`JobSourceType` is a string: "Estimate", "StaffingPlan", or "WorkPackage". `JobSourceId` is the PK of the source record. This avoids EF table-per-hierarchy complexity while keeping demand references clean.

---

## Backend Plan

### New Services
1. `Api/Services/AssignmentConflictService.cs` — detects double-booking, unavailability, cert mismatches
2. `Api/Services/CoverageCalculationService.cs` — computes demand vs assigned headcount per craft
3. `Api/Services/SuggestedMatchService.cs` — ranks resources for open craft gaps

### Controller Changes
- `SchedulingController`: inject all 3 services; wire `GET /coverage`, `GET /available-soon`, `GET /suggested-matches`; add conflict check to create/update assignment
- `PortalController`: inject CoverageCalculationService; replace hardcoded zeros

### Demo Seed
4 resources (2 Pipefitters, 1 Electrician, 1 Crane Operator), certifications, and assignments that demonstrate:
- Pipefitter shortage (demand 4, assigned 2)
- 2 people ending soon (within 5 days)
- Crane Operator fully covered

---

## Frontend Plan

### View Build Order
1. Scheduling Dashboard — KPI cards + recent jobs
2. Jobs Board — demand table with assign action
3. Resources Board — resource CRUD + cert management
4. Assignments View — assignment CRUD + conflict warnings
5. Coverage View — craft gap analysis with progress bars
6. Roll-Off View — ending soon + suggested next job

### Patterns (from estimating module)
- `useApiStore()` for auth axios instance
- PrimeVue DataTable, Dialog, Card, Toast, ConfirmDialog
- `onMounted` → load → `ProgressSpinner` while pending → `Message` on error
- No manual CompanyCode — extracted from JWT server-side

---

## DB Migration Strategy

All entities are already migrated. No new migrations required for base implementation.

Optional (if audit trail needed): `AddConflictLog` migration with `ConflictId, AssignmentId, ConflictType, OverriddenBy, OverriddenAt, Notes`. Not included in initial implementation.

---

## Risks & Assumptions

| Risk | Mitigation |
|------|------------|
| NSwag must regenerate after controller DTO changes | Run `cd Api && nswag run nswag.json` after every controller change |
| Demo data must be idempotent | All seed methods guard with `AnyAsync()` before inserting |
| Conflict detection should not hard-block | Return conflicts as warnings; include `override: true` flag in response |
| Step code sorting (Planning) | `StepOutStep.SortOrder` is decimal; DB ORDER BY handles "1", "1.2", "1.5" correctly |

---

## Acceptance Criteria

- [ ] Portal KPIs show real numbers with demo data loaded
- [ ] Scheduling Dashboard loads with real KPI counts  
- [ ] Jobs Board lists all demand from all three source types
- [ ] Resources Board shows 4 demo resources with craft badges and cert pills
- [ ] Assignments View allows CRUD and shows conflict warnings
- [ ] Coverage View shows Pipefitter gap highlighted red
- [ ] Roll-Off View shows resources ending soon
- [ ] No regressions in estimating, portal, or auth flows
- [ ] NSwag client in sync after every controller change
