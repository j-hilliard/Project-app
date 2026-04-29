# PROJECT DATA OWNERSHIP AND TRACEABILITY
## Stronghold Enterprise — Entity Ownership, FK Chains, Authorization Gates
**Document Version:** 1.0  
**Date:** 2026-04-28  
**Status:** APPROVED — Implementation Pending  

---

## ENTITY OWNERSHIP BOUNDARIES

No entity may be written to by a module that does not own it. "Reads" are permissible across boundaries; "writes" are not.

### ESTIMATING MODULE — Owns

| Entity | Write Rules |
|--------|-------------|
| Estimate | Any update to Estimate fields. Status transitions gate downstream flows. |
| EstimateRevision | Created by Estimating only (snapshot on save). |
| EstimateSummary | Recalculated by Estimating only. |
| LaborRow | Written by Estimating only. |
| EquipmentRow | Written by Estimating only. |
| ExpenseRow | Written by Estimating only. |
| FcoEntry | Written by Estimating only (thin $ line item within estimate scope). NOT used for PM traceability. |
| RateBook / RateBookLaborRate / RateBookEquipmentRate / RateBookExpenseItem | Written by Estimating only. |
| CostBook / CostBookLaborRate / CostBookEquipmentRate / CostBookExpense / CostBookOverheadItem | Written by Estimating only. |
| StaffingPlan / StaffingLaborRow | Written by Estimating only. |
| CrewTemplate / CrewTemplateRow | Written by Estimating only. |

### PLANNING / PM MODULE — Owns

| Entity | Write Rules |
|--------|-------------|
| CommercialAuthorization | Created and managed by Planning. Linked to Estimate (read-only ref). |
| Project | Created from awarded Estimate + CommAuth. Managed by Planning. |
| ProjectPhase | Created and managed by Planning under a Project. |
| PlanTask | Created and managed by Planning under a ProjectPhase. |
| TaskDependency | Created/deleted by Planning (PlanTaskController). |
| Milestone | Created and managed by Planning. |
| TimelineBaseline | Created by Planning (explicit lock action). Read-only after lock. |
| EstimateTaskLink | Created/deleted by Planning (PlanTaskController). Estimate record is read-only. |
| FcoTaskLink | Created/deleted by Planning (PlanTaskController). FcoDocument record is read-only for this operation. |
| TaskProgressSnapshot | Written by Planning (progress update action). |
| WorkOrder | Created and managed by Planning. Release gated by authorization rules. |
| StepOutPlan | Written by Planning. Linked to WorkOrder (required in business logic). |
| StepOutStep | Written by Planning. |
| StepOutSubStep | Written by Planning. |
| StepDependency | Written by Planning. |
| StepResourceReq | Written by Planning. |
| WorkPackage | Created by Planning. ReadyForScheduling flag set by Planning. |
| FcoDocument | Created and managed by Planning. Linked to Estimate (read-only ref) and WorkOrder. |
| FcoLaborLine | Written by Planning (FcoDocument controller). Rates read from Estimating's RateBook. |
| ActualEntry | Written by Planning. WorkOrderId required. |

### SCHEDULING MODULE — Owns

| Entity | Write Rules |
|--------|-------------|
| Resource | Written by Scheduling only. |
| Craft | Written by Scheduling only (reference data). |
| Certification | Written by Scheduling only. |
| AvailabilityBlock | Written by Scheduling only. |
| Assignment | Written by Scheduling only. WorkPackage is read-only reference. |

### PORTAL MODULE — Owns

Nothing. Portal reads from all three modules via API aggregation endpoints.

---

## REQUIRED LINKS AND FOREIGN KEYS

### Non-Negotiable Foreign Keys (Hard Business Rules)

| Entity | FK | Rule |
|--------|-----|------|
| CommercialAuthorization | EstimateId → Estimates | REQUIRED. Every auth belongs to exactly one estimate. OnDelete: Restrict. |
| Project | EstimateId → Estimates | REQUIRED. Every project traces to an estimate. OnDelete: Restrict. |
| Project | CommercialAuthorizationId → CommercialAuthorizations | Business-required before Project can go Active. DB: nullable FK, business logic enforces. OnDelete: SetNull. |
| WorkOrder | ProjectId → Projects | REQUIRED. OnDelete: Restrict. |
| WorkOrder | EstimateId → Estimates | REQUIRED. Denormalized for fast traceability. OnDelete: Restrict. |
| WorkOrder | CommercialAuthorizationId → CommercialAuthorizations | REQUIRED. OnDelete: Restrict. |
| StepOutPlan | WorkOrderId → WorkOrders | REQUIRED in business logic. DB: nullable for migration safety; business logic enforces. OnDelete: SetNull. |
| FcoDocument | LinkedEstimateId → Estimates | REQUIRED. Staged enforcement (audit nulls → backfill → enforce NOT NULL). OnDelete: Restrict. |
| FcoDocument | LinkedWorkOrderId → WorkOrders | REQUIRED. Staged enforcement. OnDelete: Restrict. |
| FcoLaborLine | FcoDocumentId → FcoDocuments | REQUIRED. OnDelete: Cascade. |
| ActualEntry | WorkOrderId → WorkOrders | REQUIRED. OnDelete: Restrict. |
| ProjectPhase | ProjectId → Projects | REQUIRED. OnDelete: Cascade. |
| PlanTask | PhaseId → ProjectPhases | REQUIRED. OnDelete: Cascade. |
| PlanTask | ParentTaskId → PlanTasks | OPTIONAL (self-ref). OnDelete: NoAction. |
| TaskDependency | SuccessorTaskId → PlanTasks | REQUIRED. OnDelete: Cascade. |
| TaskDependency | PredecessorTaskId → PlanTasks | REQUIRED. OnDelete: NoAction. |
| Milestone | ProjectId → Projects | REQUIRED. OnDelete: Cascade. |
| Milestone | PhaseId → ProjectPhases | OPTIONAL. OnDelete: SetNull. |
| Milestone | TaskId → PlanTasks | OPTIONAL. OnDelete: SetNull. |
| EstimateTaskLink | TaskId → PlanTasks | REQUIRED. OnDelete: Cascade. |
| EstimateTaskLink | EstimateId → Estimates | REQUIRED. OnDelete: Restrict. |
| FcoTaskLink | TaskId → PlanTasks | REQUIRED. OnDelete: Cascade. |
| FcoTaskLink | FcoDocumentId → FcoDocuments | REQUIRED. OnDelete: Restrict. |
| StepOutSubStep | StepId → StepOutSteps | REQUIRED. OnDelete: Cascade. |
| StepOutStep | ParentStepId → StepOutSteps | OPTIONAL (self-ref). OnDelete: NoAction. |
| WorkPackage | WorkOrderId → WorkOrders | OPTIONAL (business logic enforces). OnDelete: SetNull. |
| TimelineBaseline | ProjectId → Projects | REQUIRED. OnDelete: Cascade. |
| TaskProgressSnapshot | TaskId → PlanTasks | REQUIRED. OnDelete: Cascade. |
| ActualEntry | FcoDocumentId → FcoDocuments | OPTIONAL. OnDelete: SetNull. |
| ActualEntry | PlanTaskId → PlanTasks | OPTIONAL. OnDelete: SetNull. |
| ActualEntry | StepOutStepId → StepOutSteps | OPTIONAL. OnDelete: SetNull. |
| ActualEntry | StepOutSubStepId → StepOutSubSteps | OPTIONAL. OnDelete: SetNull. |

---

## TRACEABILITY CHAIN

### Primary Forward Chain (Estimate → Execution)

```
Estimate
  └─► CommercialAuthorization (via CommercialAuthorization.EstimateId)
        └─► Project (via Project.EstimateId + Project.CommercialAuthorizationId)
              └─► WorkOrder (via WorkOrder.ProjectId + WorkOrder.EstimateId + WorkOrder.CommercialAuthorizationId)
                    └─► StepOutPlan (via StepOutPlan.WorkOrderId)
                          └─► StepOutStep (via StepOutStep.PlanId)
                                └─► StepOutSubStep (via StepOutSubStep.StepId)
```

### Planning Chain (Project → Task → FCO)

```
Project
  └─► ProjectPhase (via ProjectPhase.ProjectId)
        └─► PlanTask (via PlanTask.PhaseId)
              ├─► EstimateTaskLink → Estimate (many-to-many)
              └─► FcoTaskLink → FcoDocument (many-to-many)
```

### Change Control Chain

```
FcoDocument
  ├─► Estimate (via FcoDocument.LinkedEstimateId — required)
  ├─► WorkOrder (via FcoDocument.LinkedWorkOrderId — required)
  ├─► FcoLaborLine[] (structured labor cost breakdown)
  └─► FcoTaskLink → PlanTask[] (impacted tasks)
```

### Actuals Chain

```
ActualEntry
  ├─► WorkOrder (required — no orphan actuals)
  ├─► FcoDocument? (optional — if FCO-authorized work)
  ├─► PlanTask? (optional — task-level tracing)
  ├─► StepOutStep? (optional — step-level tracing)
  └─► StepOutSubStep? (optional — sub-step-level tracing)
```

### Complete Traceability (All Paths)

```
Forward from Estimate:
  Estimate → CommAuth → Project → WorkOrder → StepOutPlan → Steps → SubSteps
  Estimate → CommAuth → Project → Phase → PlanTask → FCO links

Backward from any execution artifact:
  StepOutSubStep → StepOutStep → StepOutPlan → WorkOrder → Project → CommAuth → Estimate
  PlanTask → ProjectPhase → Project → CommAuth → Estimate
  ActualEntry → WorkOrder → Project → CommAuth → Estimate
  FcoDocument → WorkOrder → Project + FcoDocument → Estimate

FCO full chain:
  FcoDocument.LinkedEstimateId → Estimate.RateBookId → RateBook (rate lookup)
  FcoDocument.LinkedWorkOrderId → WorkOrder → Project → CommAuth
  FcoDocument → FcoLaborLine[] (cost breakdown)
  FcoDocument → FcoTaskLink → PlanTask (impact mapping)
```

---

## AUTHORIZATION GATING RULES

These rules are enforced server-side in controllers. They are NOT just documentation — they are validated on every relevant write operation.

### Gate 1: Project Creation
```
REQUIRES:
  Estimate.Status = 'Awarded'
  CommercialAuthorization.Status = 'Active'
  CommercialAuthorization.EstimateId = Estimate.EstimateId
BLOCKS:
  Cannot create Project if Estimate.Status != 'Awarded'
  Cannot create Project if no Active CommercialAuthorization linked to the Estimate
```

### Gate 2: WorkOrder Release
```
REQUIRES:
  Project.CommercialAuthorizationId IS NOT NULL
  CommercialAuthorization.Status = 'Active'
  Estimate.Status = 'Awarded'
  WorkOrder.AuthorizedValue <= CommercialAuthorization.AuthorizedValue
    (minus sum of other Active/Released WorkOrders on same CommAuth)
BLOCKS:
  Cannot transition WorkOrder.Status to 'Released' if any gate fails
  Returns HTTP 422 with specific gate failure reason
```

### Gate 3: StepOutPlan Execution
```
REQUIRES:
  StepOutPlan.WorkOrderId IS NOT NULL
  WorkOrder.Status IN ('Released', 'InProgress')
BLOCKS:
  Cannot transition StepOutStep.Status to 'InProgress' if WorkOrder not released
```

### Gate 4: FCO Creation
```
REQUIRES:
  FcoDocument.LinkedEstimateId IS NOT NULL
  FcoDocument.LinkedWorkOrderId IS NOT NULL
  WorkOrder.ProjectId = Project linked to the same Estimate
BLOCKS:
  Cannot save FcoDocument without both required FKs
```

### Gate 5: ActualEntry
```
REQUIRES:
  ActualEntry.WorkOrderId IS NOT NULL
  WorkOrder.Status IN ('Released', 'InProgress', 'Complete')
BLOCKS:
  Cannot save ActualEntry without WorkOrderId
  Cannot enter actuals against a Draft or Cancelled WorkOrder
```

### Gate 6: PlanTask Activation
```
REQUIRES (soft warning, not hard block):
  At least one EstimateTaskLink exists for the task
BEHAVIOR:
  If no EstimateTaskLink: warn user "No estimate link — this task has no commercial baseline"
  Allow activation with user confirmation (admin override)
```

### Gate 7: Project Closeout
```
REQUIRES:
  All WorkOrders for this Project: Status IN ('Complete', 'Closed', 'Cancelled')
  No Milestone with Status = 'Pending' (unless explicitly waived)
BLOCKS:
  Cannot set Project.Status = 'Closed' if any WorkOrder is InProgress or Released
```

---

## ORPHAN-PREVENTION RULES

### FcoDocument Orphan Prevention
- `FcoDocument.LinkedEstimateId` — staged to NOT NULL: audit nulls first, backfill, then enforce
- `FcoDocument.LinkedWorkOrderId` — staged to NOT NULL: same approach
- **Hard delete of Estimate blocked** by `EstimateTaskLink.EstimateId OnDelete(Restrict)` and `FcoDocument.LinkedEstimateId OnDelete(Restrict)`
- Estimate can only be archived/cancelled, not hard-deleted, if it has linked downstream records

### WorkOrder Orphan Prevention
- `WorkOrder.ProjectId OnDelete(Restrict)` — cannot delete Project with active WorkOrders
- `WorkOrder.EstimateId OnDelete(Restrict)` — cannot delete/archive Estimate with active WorkOrders
- WorkOrders must be Cancelled/Closed before Project can be deleted

### ActualEntry Orphan Prevention
- `ActualEntry.WorkOrderId OnDelete(Restrict)` — cannot delete WorkOrder with actuals
- Actuals are permanent historical records; they do not cascade-delete

### StepOutPlan Orphan Prevention
- `StepOutPlan.WorkOrderId OnDelete(SetNull)` — if WorkOrder is deleted, plan becomes unlinked
- An unlinked StepOutPlan (WorkOrderId IS NULL) is flagged as orphaned in the traceability health check

### CommercialAuthorization Orphan Prevention
- `CommercialAuthorization.EstimateId OnDelete(Restrict)` — Estimate cannot be hard-deleted if auth exists
- Multiple CommercialAuthorizations can exist for one Estimate (amendments, change authorizations)

---

## BROKEN LINK / TRACEABILITY HEALTH QUERIES

These queries power the `GET /api/v1/traceability/broken-links` endpoint:

### Orphaned StepOutPlans (no WorkOrder)
```sql
SELECT PlanId, Name, CompanyCode
FROM StepOutPlans
WHERE WorkOrderId IS NULL
  AND Status != 'Archived'
```

### FCO Documents Missing Estimate Link
```sql
SELECT FcoDocumentId, FcoNumber, Title
FROM FcoDocuments
WHERE LinkedEstimateId IS NULL
```

### FCO Documents Missing WorkOrder Link
```sql
SELECT FcoDocumentId, FcoNumber, Title
FROM FcoDocuments
WHERE LinkedWorkOrderId IS NULL
  AND Status != 'Draft'
```

### ActualEntry Records Without WorkOrder (should never happen — constraint prevents it)
```sql
SELECT ActualEntryId, ActualDate, CostAmount
FROM ActualEntries
WHERE WorkOrderId NOT IN (SELECT WorkOrderId FROM WorkOrders)
```

### PlanTasks Linked to Archived/Cancelled Estimates
```sql
SELECT etl.LinkId, etl.TaskId, etl.EstimateId
FROM EstimateTaskLinks etl
JOIN Estimates e ON etl.EstimateId = e.EstimateId
WHERE etl.IsArchived = false
  AND e.Status IN ('Cancelled', 'Lost')
```

### Projects Without Active CommercialAuthorization
```sql
SELECT p.ProjectId, p.Name, p.Status
FROM Projects p
LEFT JOIN CommercialAuthorizations ca
  ON ca.CommercialAuthorizationId = p.CommercialAuthorizationId
WHERE p.Status NOT IN ('Initiating', 'Cancelled')
  AND (p.CommercialAuthorizationId IS NULL OR ca.Status != 'Active')
```

### WorkOrders with AuthorizedValue Exceeding CommAuth
```sql
SELECT wo.WorkOrderId, wo.WorkOrderNumber, wo.AuthorizedValue,
       ca.AuthorizedValue AS CommAuthValue,
       SUM(wo2.AuthorizedValue) AS TotalAllocated
FROM WorkOrders wo
JOIN CommercialAuthorizations ca ON wo.CommercialAuthorizationId = ca.CommercialAuthorizationId
JOIN WorkOrders wo2 ON wo2.CommercialAuthorizationId = ca.CommercialAuthorizationId
  AND wo2.Status NOT IN ('Cancelled')
GROUP BY wo.WorkOrderId, wo.WorkOrderNumber, wo.AuthorizedValue, ca.AuthorizedValue
HAVING SUM(wo2.AuthorizedValue) > ca.AuthorizedValue
```

---

## ACTUALS ROLLUP DESIGN

### Rollup Hierarchy

```
ActualEntry (grain: one cost or billable line)
  ↓ GROUP BY WorkOrderId
WorkOrder Totals:
  TotalActualCost      = SUM(ActualEntry.CostAmount) WHERE WorkOrderId = X
  TotalBillableAmount  = SUM(ActualEntry.BillableAmount) WHERE WorkOrderId = X
  TotalBilledAmount    = SUM(ActualEntry.BilledAmount) WHERE WorkOrderId = X
  ↓ GROUP BY ProjectId (via WorkOrder)
Project Totals:
  TotalActualCost      = SUM across all WorkOrders in Project
  TotalBillableAmount  = SUM across all WorkOrders in Project
  ↓ GROUP BY EstimateId (via WorkOrder)
Estimate vs Actual:
  EstimatedCost        = EstimateSummary.InternalCostTotal
  ActualCost           = SUM of ActualEntries linked through WorkOrders → Project → CommAuth → Estimate
  CostVariance         = ActualCost - EstimatedCost
  EstimatedBillable    = EstimateSummary.GrandTotal
  ActualBillable       = SUM of ActualEntry.BillableAmount
  BillableVariance     = ActualBillable - EstimatedBillable
```

### FCO Actuals Rollup

```
FcoDocument Rollup:
  FcoLaborLine subtotals  → FcoDocument.TotalFcoAmount (recalculated on save)
  ActualEntry WHERE FcoDocumentId = X:
    ActualCostForFco     = SUM(ActualEntry.CostAmount)
    FcoVariance          = ActualCostForFco - FcoDocument.TotalFcoAmount
```

### Authorized Value vs Forecast vs Actual

```
CommercialAuthorization.AuthorizedValue   (what customer authorized)
  vs.
WorkOrder.AuthorizedValue × count         (what we allocated to WOs)
  vs.
ForecastBillableTotal                     (what we expect to bill)
  = SUM(WorkOrder.AuthorizedValue) [for active WOs]
  + SUM(FcoDocument.TotalFcoAmount) [for approved FCOs not yet in a new CommAuth]
  vs.
ActualBilledTotal                         (what we actually invoiced)
  = SUM(ActualEntry.BilledAmount) across all WOs on this CommAuth

RevenueExposure = AuthorizedValue - ForecastBillableTotal  (negative = overage risk)
```

---

## NSWAG SYNC REQUIREMENT

NSwag is **manual CLI** in this repo. It is NOT triggered by `dotnet build`.

### When to Run NSwag
Run after EVERY addition of a new controller or new endpoint:
```bash
# From the Api directory, with .NET app configured for Local profile:
cd Api
nswag run nswag.json /variables:Configuration=Debug
```

Output: `webapp/src/apiclient/client.ts`

This must happen BEFORE frontend development starts on any new API controller, otherwise TypeScript types will not exist.

### NSwag Workflow
1. Add new controller(s) to `Api/Controllers/`
2. Build the API: `cd Api && dotnet build`
3. Run NSwag: `nswag run nswag.json /variables:Configuration=Debug` (from Api dir, app must be startable)
4. Verify `webapp/src/apiclient/client.ts` updated with new client classes
5. Run `cd webapp && npm run build` to verify TypeScript compiles

### New Controllers Requiring NSwag Sync
After implementing the lifecycle model, these new controllers will require NSwag re-run:
- `CommercialAuthorizationController`
- `ProjectController`
- `WorkOrderController`
- `StepOutSubStepController` (or handled in PlanningController)
- `FcoLaborLineController` (or handled in PlanningController)
- `ActualEntryController`
- `ScheduleHealthController`
- `TraceabilityController`
- `ProjectPhaseController`
- `PlanTaskController`
- `MilestoneController`
