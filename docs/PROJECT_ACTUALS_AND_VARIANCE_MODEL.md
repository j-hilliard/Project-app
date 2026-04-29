# PROJECT ACTUALS AND VARIANCE MODEL
## Stronghold Enterprise — Actuals Capture, Rollup, Variance, and Feedback Loop
**Document Version:** 1.0  
**Date:** 2026-04-28  
**Status:** APPROVED — Implementation Pending  

---

## OVERVIEW

Actuals are the truth. This module captures what actually happened — how much things cost, how much we billed, and how it compares to what we planned. The purpose is:

1. **Project control** — know when you're over budget or off schedule in real time
2. **Change protection** — prove what was authorized, what was extra, and what changed
3. **Invoice readiness** — know what you are entitled to bill and what you have billed
4. **Future estimating** — feed historical actual rates, durations, and costs back into the estimating process

---

## ACTUALS ENTITY: ActualEntry

The `ActualEntry` is the grain-level record of actual cost and billable value. Every executed cost event is an ActualEntry.

### Entity Definition

```
ActualEntryId          int PK
CompanyCode            nvarchar(10) required
WorkOrderId            int FK → WorkOrders, Restrict  -- REQUIRED, no orphan actuals
FcoDocumentId          int? FK → FcoDocuments, SetNull  -- if this cost was FCO-authorized work
PlanTaskId             int? FK → PlanTasks, SetNull     -- optional task-level trace
StepOutStepId          int? FK → StepOutSteps, SetNull  -- optional step-level trace
StepOutSubStepId       int? FK → StepOutSubSteps, SetNull -- optional substep trace
ActualType             nvarchar(30) required  -- Labor|Equipment|Material|Subcontract|Other
ActualDate             datetime2 required
Description            nvarchar(500) optional
Position               nvarchar(200) optional  -- for Labor: crew position
CraftCode              nvarchar(50) optional   -- for Labor: craft code
StHours                decimal(10,2) default 0 -- for Labor: straight-time hours
OtHours                decimal(10,2) default 0 -- for Labor: overtime hours
DtHours                decimal(10,2) default 0 -- for Labor: double-time hours
CostAmount             decimal(18,2) required  -- what it cost us (burden + base cost)
BillableAmount         decimal(18,2) default 0 -- what we are entitled to bill the customer
BilledAmount           decimal(18,2) default 0 -- what was actually put on an invoice
IsConfirmed            bit default false       -- confirmed for final billing/reporting
EnteredBy              nvarchar(100) required
Notes                  nvarchar(max) optional
CreatedAt              datetimeoffset required
```

### ActualType Values

| Type | When Used |
|------|-----------|
| `Labor` | Field labor hours and cost. Most common. Ties to CraftCode, Position, hours. |
| `Equipment` | Equipment rental, owned equipment utilization. |
| `Material` | Materials, consumables purchased for the job. |
| `Subcontract` | Third-party subcontractors. |
| `Other` | Miscellaneous costs (permits, fees, travel, etc.). |

### Key Design Decisions

**CostAmount vs BillableAmount vs BilledAmount:**
- `CostAmount` = what we actually paid / incurred (what it cost us)
- `BillableAmount` = what the contract/rate book entitles us to charge (bill rate × hours)
- `BilledAmount` = what we put on the actual invoice (may differ from BillableAmount due to negotiations, holdbacks, or invoicing cycles)

For labor: `BillableAmount = (StHours × BillStRate) + (OtHours × BillOtRate) + (DtHours × BillDtRate)`
For non-labor: `BillableAmount` = manually entered or calculated from contract markup

**Why both CostAmount and BillableAmount?**
This is the margin lens. You want to see:
- Are we incurring more cost than our rate book expected?
- Are we billing what we are entitled to bill?
- Where is margin being lost (cost higher than expected, or billing lower than entitled)?

---

## ROLLUP STRUCTURE

### Level 1: WorkOrder Rollup

```
WorkOrderActualSummary {
  WorkOrderId
  TotalActualHours     = SUM(StHours + OtHours + DtHours) WHERE ActualType = 'Labor'
  TotalCostAmount      = SUM(CostAmount)
  TotalBillableAmount  = SUM(BillableAmount)
  TotalBilledAmount    = SUM(BilledAmount)
  ByType {
    LaborCost          = SUM(CostAmount) WHERE ActualType = 'Labor'
    EquipmentCost      = SUM(CostAmount) WHERE ActualType = 'Equipment'
    MaterialCost       = SUM(CostAmount) WHERE ActualType = 'Material'
    SubcontractCost    = SUM(CostAmount) WHERE ActualType = 'Subcontract'
    OtherCost          = SUM(CostAmount) WHERE ActualType = 'Other'
  }
}
```

### Level 2: Project Rollup (across all WorkOrders)

```
ProjectActualSummary {
  ProjectId
  TotalActualCost      = SUM(WO.TotalCostAmount) for all WOs in project
  TotalBillableAmount  = SUM(WO.TotalBillableAmount)
  TotalBilledAmount    = SUM(WO.TotalBilledAmount)
}
```

### Level 3: Estimate vs Actual Rollup

```
EstimateVsActual {
  EstimateId
  EstimatedCost        = EstimateSummary.InternalCostTotal
  EstimatedBillable    = EstimateSummary.GrandTotal
  EstimatedMarginPct   = EstimateSummary.GrossMarginPct
  ActualCost           = SUM via Project → WorkOrders → ActualEntries
  ActualBillable       = SUM(BillableAmount) via same chain
  ActualBilled         = SUM(BilledAmount) via same chain
  CostVariance         = ActualCost - EstimatedCost           (positive = over estimate)
  BillableVariance     = ActualBillable - EstimatedBillable   (positive = billed more than estimated)
  ActualMarginPct      = (ActualBillable - ActualCost) / ActualBillable × 100
  MarginVariance       = ActualMarginPct - EstimatedMarginPct
}
```

### Level 4: FCO vs Actual Rollup

```
FcoVsActual {
  FcoDocumentId
  FcoAuthorizedAmount  = FcoDocument.TotalFcoAmount
  ActualCostForFco     = SUM(ActualEntry.CostAmount) WHERE FcoDocumentId = X
  ActualBillableForFco = SUM(ActualEntry.BillableAmount) WHERE FcoDocumentId = X
  FcoCostVariance      = ActualCostForFco - FcoAuthorizedAmount
  FcoBillableVariance  = ActualBillableForFco - FcoAuthorizedAmount
}
```

### Level 5: Authorized Value vs Forecast vs Actual

```
CommAuthExposure {
  CommercialAuthorizationId
  AuthorizedValue        = CommercialAuthorization.AuthorizedValue
  AllocatedToWOs         = SUM(WorkOrder.AuthorizedValue) WHERE CommAuthId = X AND Status != 'Cancelled'
  ForecastBillable       = AllocatedToWOs
                         + SUM(FcoDocument.TotalFcoAmount) WHERE LinkedWorkOrderId IN (WOs for this CommAuth)
                           AND FcoDocument.Status IN ('Approved', 'Signed')
  ActualBilled           = SUM(ActualEntry.BilledAmount) via WOs for this CommAuth
  RemainingAuthorized    = AuthorizedValue - ActualBilled
  RevenueExposure        = AuthorizedValue - ForecastBillable  (negative = overage risk)
  UnbilledEntitlement    = AllocatedToWOs - ActualBilled       (what we can still bill)
}
```

---

## ESTIMATE VS ACTUAL

### Purpose
Compare what we bid/estimated to what actually happened. The primary feedback loop for future estimating.

### Key Metrics

| Metric | Formula | Meaning |
|--------|---------|---------|
| CostVariance | ActualCost − EstimatedCost | + = over budget; − = under budget |
| BillableVariance | ActualBillable − EstimatedBillable | + = billed more than estimated |
| MarginVariance | ActualMarginPct − EstimatedMarginPct | Margin eroded or improved |
| LaborHourVariance | ActualLaborHours − EstimatedLaborHours | Over or under on hours |
| LaborRateVariance | ActualLaborCost/ActualLaborHours − EstimatedRate | Rate higher/lower than expected |

### Labor Deep Dive
```
For each crew position:
  EstimatedHours  = SUM(LaborRow.StHours + OtHours + DtHours) WHERE Position = X
  ActualHours     = SUM(ActualEntry.StHours + OtHours + DtHours) WHERE Position = X AND ActualType = 'Labor'
  HourVariance    = ActualHours - EstimatedHours

  EstimatedBillRate = LaborRow.BillStRate (weighted average)
  ActualBillRate    = ActualEntry.BillableAmount / ActualHours  (effective rate)
  RateVariance      = ActualBillRate - EstimatedBillRate
```

---

## WORKORDER VS ACTUAL

### Purpose
Track cost and billing performance per released work order. Each WO has an `AuthorizedValue` — the portion of the commercial authorization allocated to that scope.

### Key Metrics

| Metric | Formula |
|--------|---------|
| WO Cost Variance | SUM(ActualEntry.CostAmount) − [estimated cost portion for WO] |
| WO Billing Variance | SUM(ActualEntry.BillableAmount) − WorkOrder.AuthorizedValue |
| WO Overbilling Risk | If SUM(ActualEntry.BillableAmount) > WorkOrder.AuthorizedValue |
| WO Percent Complete | From PlanTask / StepOutStep completion status |

---

## FCO VS ACTUAL

### Purpose
Verify that FCO-authorized work was done within the approved FCO scope and cost.

### FCO Labor Detail
`FcoLaborLine` records hold the authorized cost breakdown per position:
```
FcoLaborLine.StHours × FcoLaborLine.BillStRate
+ FcoLaborLine.OtHours × FcoLaborLine.BillOtRate
+ FcoLaborLine.DtHours × FcoLaborLine.BillDtRate
= FcoLaborLine.Subtotal
SUM(FcoLaborLine.Subtotal) = FcoDocument.TotalFcoAmount (baseline)
```

### Actual Comparison
For actuals tied to the FCO (`ActualEntry.FcoDocumentId = X`):
```
FCO Authorized Cost      = FcoDocument.TotalFcoAmount
FCO Actual Cost          = SUM(ActualEntry.CostAmount) WHERE FcoDocumentId = X
FCO Cost Variance        = FCO Actual Cost - FCO Authorized Cost
FCO Actual Billable      = SUM(ActualEntry.BillableAmount) WHERE FcoDocumentId = X
```

---

## COST VS BILLABLE VARIANCE

### The Margin Protection Model

At any point in a project, you want to know:
1. What did we actually spend? (CostAmount)
2. What are we entitled to bill? (BillableAmount)
3. What have we actually billed? (BilledAmount)
4. Is there a gap between entitlement and billing? (Billing Gap)

```
Billing Gap          = BillableAmount - BilledAmount      (positive = have not yet invoiced)
Margin Erosion       = CostAmount > BillableAmount         (we're spending more than we can bill)
Margin at Completion = (TotalBillable - TotalCost) / TotalBillable × 100
```

### Early Warning Signals

| Signal | Condition | Recommended Action |
|--------|-----------|-------------------|
| Margin Erosion | CostAmount > BillableAmount for any week | Investigate cost overrun; consider FCO |
| Overbilling Risk | ForecastBillable > AuthorizedValue | Initiate FCO or request supplemental auth |
| Unbilled Accumulation | BilledAmount << BillableAmount for extended period | Submit invoice; do not let entitlement age |
| ForecastExceedsAuth | See Stage 9 monitoring rules | PM action required immediately |

---

## FUTURE ESTIMATING FEEDBACK LOOP

### Purpose
Make every future estimate better by learning from every actual.

### Feedback Data to Capture

At project closeout (or any time after actuals are confirmed):

**Labor Rate Actuals**
```
For each Position on a job:
  ActualStHoursRate = SUM(CostAmount for Labor) / SUM(StHours) for this Position
  vs.
  CostBook rate for same Position
  → Flag if variance > 10%
  → Feed into CostBook review workflow
```

**Labor Duration Actuals**
```
For each StepOutStep/SubStep:
  PlannedDurationHours  = StepOutStep.DurationHours
  ActualDurationHours   = (ActualEnd - ActualStart) in hours  OR  ActualEntry sum for that step
  DurationVariance      = ActualDurationHours - PlannedDurationHours
  → Flag steps where actual > planned × 1.25 (25% overrun)
  → Store as reference in a future LessonsLearned view
```

**Overall Cost Performance**
```
HistoricalCostPerformance {
  EstimateId
  JobType          = Estimate.JobType
  Client           = Estimate.Client
  PlannedCost      = EstimateSummary.InternalCostTotal
  ActualCost       = (from ActualEntry rollup)
  PerformancePct   = PlannedCost / ActualCost × 100  (100 = perfect; >100 = came in under)
  Notes            = Project.LessonsLearnedNotes
}
```

### Lessons Learned Reference View (Future)
A read-only view in the Estimating module showing:
- Historical jobs by type and client
- Planned vs actual hours per position/craft
- Planned vs actual cost
- Schedule performance (planned vs actual duration)
- FCO history (how much scope changed)
- Lessons learned notes

This view helps estimators calibrate future bids with real-world performance data.

---

## API ENDPOINTS FOR ACTUALS AND VARIANCE

### ActualEntryController (`/api/v1/actual-entries`)
- `GET /` — list (filter: workOrderId, projectId, estimateId, fcoDocumentId, actType, date range)
- `GET /:id` — single entry
- `POST /` — create (validates WorkOrderId exists and is Released/InProgress)
- `PUT /:id` — update (only if IsConfirmed = false)
- `PATCH /:id/confirm` — confirm entry (sets IsConfirmed = true)
- `DELETE /:id` — delete (only if IsConfirmed = false)

### Rollup Endpoints
- `GET /api/v1/variance/estimate/:estimateId` — estimate vs actual summary
- `GET /api/v1/variance/project/:projectId` — project actual summary + WO breakdown
- `GET /api/v1/variance/work-order/:workOrderId` — WO actual detail + by type breakdown
- `GET /api/v1/variance/fco/:fcoDocumentId` — FCO authorized vs actual
- `GET /api/v1/variance/comm-auth/:authId` — authorized value vs forecast vs actual

### Schedule Health Endpoints
- `GET /api/v1/schedule-health/projects` — all projects with health status
- `GET /api/v1/schedule-health/project/:projectId` — project detail with phase and task health
- `GET /api/v1/schedule-health/project/:projectId/critical-path` — critical path chain
- `GET /api/v1/schedule-health/milestones` — all milestones with slippage status

---

## DEMO SEED DATA: THREE REFERENCE PROJECTS

### Project A: Valero Pasadena Turnaround — Execution Plan
- Status: Active, At Risk
- Estimate: Linked, Awarded
- CommercialAuthorization: PO, Active, $1.4M
- WorkOrder: Released, $1.25M
- Project phases: Mobilization (Complete), Main Execution (InProgress, +8 days behind), Demobilization (NotStarted)
- FCO: 1 approved FCO for $85K (scope addition)
- Milestone: "50% Completion Gate" — Missed (slipped 3 days)
- Actuals: ~$720K cost recorded, ~$780K billable entered, ~$680K billed
- Health: Behind (one phase behind, one milestone missed)
- Forecast: 8 days past baseline end

### Project B: Cat-Spec Reactor Catalyst Changeout — Planning Baseline
- Status: Planning, On Track
- Estimate: Linked, Awarded
- CommercialAuthorization: NTP, Active, $420K
- WorkOrder: Draft (not yet released)
- Project phases: 2 phases defined with tasks and dependencies
- Milestones: 3 milestones, all Pending and future
- No FCOs
- No Actuals yet
- Health: On Track (in planning, no execution yet)
- Good baseline and dependency structure for Gantt demo

### Project C: Tank 14 Inspection and Repair — Completed Demo
- Status: Closed
- Estimate: Linked, Awarded
- CommercialAuthorization: PO, Closed
- WorkOrder: Closed
- All phases and tasks: Complete
- FCOs: 2 FCOs (1 weather delay extension, 1 additional scope), both Signed
- Actuals: Fully entered and confirmed — $340K cost, $360K billable, $360K billed
- Variance: +$22K cost overrun vs estimate; margin came in at 5.5% vs estimated 8%
- Lessons Learned: "Underestimated excavation difficulty; add 20% contingency for underground work at this site"
- Health: Closed; full variance report available
