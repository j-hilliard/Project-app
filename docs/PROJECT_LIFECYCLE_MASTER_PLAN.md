# PROJECT LIFECYCLE MASTER PLAN
## Stronghold Enterprise — Full Project Lifecycle Operating Model
**Document Version:** 2.0  
**Date:** 2026-04-28  
**Status:** APPROVED — Architecture Locked — Implementation Pending  
**Supersedes:** PROJECT_PLANNING_MASTER_PLAN.md (v1.0)

---

## EXECUTIVE SUMMARY

This document defines the complete operating model for evolving the Stronghold Enterprise platform from an estimating tool into a full end-to-end project lifecycle system covering every stage from bid intake through closeout.

The platform follows industry-standard project management structure (Initiating → Planning → Executing → Monitoring & Controlling → Closing) mapped to the real industrial/commercial workflow: estimate, authorize, plan, release, execute, track, change-control, capture actuals, close out.

**One platform. One login. One data model. No orphan records. No disconnected modules.**

The chain is:
```
Estimate → CommercialAuthorization → Project → WorkOrder → StepOutPlan → Step → SubStep
```

Every piece of executed work traces back to an Estimate through this chain. Nothing floats without traceability.

---

## CURRENT REPO STATE (Verified 2026-04-28)

### What EXISTS and Can Be Reused

| Entity / Area | File | Reuse |
|---|---|---|
| Estimate, EstimateRevision, EstimateSummary | Data/Models/ | Full reuse — commercial baseline, source of truth |
| LaborRow, EquipmentRow, ExpenseRow | Data/Models/ | Full reuse — estimate line items |
| FcoEntry | Data/Models/ | Reuse as thin $ line item within estimate scope ONLY |
| StaffingPlan, StaffingLaborRow | Data/Models/ | Full reuse — pre-project staffing intent |
| RateBook, RateBookLaborRate/EquipRate/ExpenseItem | Data/Models/ | Full reuse — drives FCO labor cost |
| CostBook, CostBookLaborRate/EquipRate/Expense/Overhead | Data/Models/ | Full reuse — internal cost baseline |
| CrewTemplate, CrewTemplateRow | Data/Models/ | Full reuse — standard crew configs |
| StepOutPlan | Data/Models/ | EXTEND — add WorkOrderId FK, make WorkOrderId required |
| StepOutStep | Data/Models/ | EXTEND — add ParentStepId self-ref, change DurationMinutes to DurationHours decimal |
| StepDependency | Data/Models/ | Full reuse — step dependencies |
| StepResourceReq | Data/Models/ | Full reuse — craft/headcount per step |
| WorkPackage | Data/Models/ | EXTEND — add WorkOrderId FK, link to scheduling demand |
| FcoDocument | Data/Models/ | EXTEND — add LinkedWorkOrderId, enforce LinkedEstimateId NOT NULL, replace JSON blobs with FcoLaborLine |
| Craft, Resource, Certification, AvailabilityBlock, Assignment | Data/Models/ | Full reuse — scheduling entities |
| AppDbContext, Migrations | Data/ | Extend — new entities need new migration |
| Auth (User, Company, Role, Permission) | Data/Models/ | Full reuse |
| EstimatesController, StaffingPlansController | Api/Controllers/ | Full reuse |
| PlanningController (currently handles FCO + step-out) | Api/Controllers/ | Extend / refactor into dedicated controllers |
| SchedulingController | Api/Controllers/ | Full reuse |
| PortalController | Api/Controllers/ | Extend — add project/WO KPIs |
| CalendarView.vue pattern | webapp/src/modules/estimating/ | Extract to shared composable |
| AppLayout.vue, portal shell, 4-app structure | webapp/src/ | Full reuse |

### What DOES NOT EXIST (Must Be Built)

| Entity | Status |
|---|---|
| `CommercialAuthorization` | Does not exist — new entity |
| `Project` | Does not exist — new entity (supersedes the earlier planning-only top-level project draft) |
| `ProjectPhase` | Uncertain — migration may exist but C# model file not confirmed |
| `PlanTask` | Uncertain — migration may exist but C# model file not confirmed |
| `WorkOrder` | Does not exist — new entity |
| `StepOutSubStep` | Does not exist — new entity (L3 leaf level) |
| `FcoLaborLine` | Does not exist — replaces LaborBreakdownJson blob |
| `ActualEntry` | Does not exist — new entity |
| `Milestone` | Does not exist — new entity |
| `TaskDependency` | Does not exist — new entity |
| `TimelineBaseline` | Does not exist — new entity |
| `EstimateTaskLink` | Does not exist — new junction |
| `FcoTaskLink` | Does not exist — new junction |

### Where the Repo Is Too Thin

| Area | Problem | Fix |
|---|---|---|
| FCO model | `LaborBreakdownJson` is an unstructured string blob — no rate lookup, no structured cost calc | Replace with `FcoLaborLine` entity |
| Project entity | No concept of "Project as execution umbrella" | Add `Project` entity |
| Authorization gate | No commercial authorization model | Add `CommercialAuthorization` |
| Work release | No `WorkOrder` entity; work floats without release gate | Add `WorkOrder` |
| Step hierarchy | `StepOutStep` has no parent-child FK (only decimal string code) | Add `ParentStepId` self-ref |
| Sub-step level | No `StepOutSubStep` (L3 leaf) | Add new entity |
| Actuals | No formal `ActualEntry` entity; actuals not systematically captured | Add `ActualEntry` |
| StepOutPlan ownership | Links directly to Estimate/StaffingPlan via nullable FK — no WorkOrder gate | Add `WorkOrderId` required FK |

---

## FULL BUSINESS WORKFLOW: ESTIMATE TO CLOSEOUT

### STAGE 1: Opportunity / Bid Intake

**What happens:** Customer opportunity identified. Pre-award context captured. Estimating team creates a bid.

**System actions:**
- Create `Estimate` (status: Draft)
- Link optional `StaffingPlan` as pre-bid labor intent
- Use `RateBook` from applicable MSA/contract to set bill rates
- Use `CostBook` to calculate internal cost and margin

**Business rule:** No authorization, no project, no work order exists yet. This is pre-award only.

**Ownership:** Estimating module.

---

### STAGE 2: Estimate Development and Approval

**What happens:** Estimate developed through revisions. Internal review. Submitted for approval.

**System actions:**
- `Estimate.Status` lifecycle: Draft → Pending → (Submitted for Approval)
- `EstimateRevision` records each snapshot
- `EstimateSummary` tracks bill/cost/margin totals
- Rate changes, crew changes, scope changes all captured in revisions
- `LaborRow`, `EquipmentRow`, `ExpenseRow` reflect current scope

**Business rule:** Only estimates in Pending/Awarded status can proceed to authorization. Lost/Cancelled estimates are excluded from all forward flows.

**Ownership:** Estimating module.

---

### STAGE 3: Commercial Authorization

**What happens:** Customer approves the work. A commercial authorization is received (PO, signed proposal, contract, NTP, work authorization, release order). This is the green light.

**System actions:**
- Mark `Estimate.Status = 'Awarded'`
- Create `CommercialAuthorization` linked to the Estimate:
  - `AuthorizationType`: PO | SignedProposal | Contract | NTP | WorkAuthorization | ReleaseOrder
  - `AuthorizedValue`: the $ amount the customer has authorized
  - `AuthorizationNumber`: PO#, contract#, NTP#, etc.
  - `Status`: Draft → Submitted → Active | Superseded | Closed

**Business rule:** No `Project` can be created without a `CommercialAuthorization.Status = 'Active'` linked to an `Estimate.Status = 'Awarded'`.

**Ownership:** Planning module (authorization record), Estimating module (estimate status update).

---

### STAGE 4: Project Initiation

**What happens:** Project is created from the awarded Estimate + authorization. Ownership assigned. High-level milestones established.

**System actions:**
- Create `Project` linked to `Estimate` and `CommercialAuthorization`
- Set `Project.PlannedStart`, `Project.PlannedEnd`
- Assign `Project.OwnerUserId`
- Create top-level `Milestone` records (kick-off, key gates, completion)
- Status: Planning

**Business rule:** Project number auto-generated. Project cannot progress to Active without at minimum one `Milestone` and a `PlannedStart`/`PlannedEnd`.

**Ownership:** Planning module.

---

### STAGE 5: Planning / PM

**What happens:** Project manager develops the execution plan. Phases, tasks, dependencies, timelines, step-out plans. Schedule baseline locked.

**System actions:**
- Create `ProjectPhase` records (groupings within project)
- Create `PlanTask` records within phases (scheduled work units with planned dates)
- Define `TaskDependency` records (predecessor-successor)
- Create `EstimateTaskLink` records (link tasks to estimates for traceability)
- Create `StepOutPlan` records (linked to WorkOrders once released — or draft-linked pending release)
- Develop `StepOutStep` hierarchy (levels 1 and 2 via ParentStepId self-ref)
- Add `StepOutSubStep` records (level 3 leaf detail)
- Lock `TimelineBaseline` (snapshot of planned dates)

**Business rule:** StepOutPlan requires WorkOrderId (see Stage 6). Planning can develop step-out in draft form before WO is released.

**Ownership:** Planning module.

---

### STAGE 6: Work Release

**What happens:** PM creates and releases Work Orders. A Work Order is the internal authorization to deploy labor and resources. Execution cannot begin without a released Work Order.

**System actions:**
- Create `WorkOrder` linked to `Project`, `Estimate`, `CommercialAuthorization`
- `WorkOrder.Status`: Draft → Released → InProgress → Complete → Closed
- Link `StepOutPlan.WorkOrderId = WorkOrder.WorkOrderId`
- When released: `WorkPackage` records flagged `ReadyForScheduling = true`

**Business rule:**
- No `WorkOrder` without `Estimate.Status = 'Awarded'` AND `CommercialAuthorization.Status = 'Active'`
- No `StepOutPlan` execution begins without `WorkOrder.Status = 'Released'` (draft planning is OK)
- Authorized value on WorkOrder must not exceed CommercialAuthorization.AuthorizedValue

**Ownership:** Planning module (work order creation/release), with gate enforced server-side.

---

### STAGE 7: Scheduling

**What happens:** Scheduling team uses work packages (derived from work orders and step-out plans) to assign specific resources to specific work windows.

**System actions:**
- `WorkPackage` records appear in Scheduling Jobs Board when `ReadyForScheduling = true`
- `Assignment` records created: `Resource` → `WorkPackage`, with start/end dates and shift
- Coverage calculation: demand (craft headcount from WorkPackage) vs assigned (Assignment count)
- Conflict detection: double-booking, cert mismatch, unavailability (advisory)

**Business rule:** Scheduling READS planning demand. Scheduling OWNS assignments. Planning OWNS work packages. No direct write-back from Scheduling to Planning except via `ActualEntry` rollup (see Stage 9).

**Ownership:** Scheduling module.

---

### STAGE 8: Execution

**What happens:** Field work begins. Task/step/substep status updated. Milestones achieved or missed. Progress tracked.

**System actions:**
- `PlanTask.Status` updated (NotStarted → InProgress → Complete | Blocked)
- `PlanTask.ActualStart`, `PlanTask.ActualEnd` recorded
- `PlanTask.PercentComplete` updated
- `TaskProgressSnapshot` records captured periodically
- `StepOutStep.Status` updated (Pending → InProgress → Complete | Blocked)
- `StepOutSubStep.Status` updated
- `Milestone.ActualDate` set when achieved; `Status = 'Achieved'`
- Schedule health calculations run continuously (FinishVariance, ForecastEnd)

**Business rule:** Actual start/end on tasks and steps drive schedule variance calculations. System flags `At Risk` (FinishVariance > 5 days) or `Behind` (milestone slipped or variance > 10 days).

**Ownership:** Planning module (task/step progress), Scheduling module (assignment actuals).

---

### STAGE 9: Monitoring & Controlling

**What happens:** Continuous tracking of planned vs actual vs forecast. At-risk identification. Cost and revenue exposure monitoring.

**System calculations (server-side, ScheduleHealthService):**
- `ForecastEnd = Today + (DurationDays × (1 − PercentComplete/100))`
- `FinishVariance = ForecastEnd − PlannedEnd` (positive = behind)
- `CostVariance = ActualCost − EstimatedCost` (from ActualEntry rollup)
- `RevenueExposure = AuthorizedValue − ForecastBillableTotal`
- `ForecastExceedsAuthorized` flag when ForecastBillableTotal > CommercialAuthorization.AuthorizedValue

**Business rule:** When ForecastExceedsAuthorized is true, system raises a risk flag. This is NOT an automatic block — it is a visibility alert for PM action (typically triggers FCO initiation).

**Ownership:** Portal (aggregates), Planning/PM module (project-level health).

---

### STAGE 10: Change Control (FCO / Change Order)

**What happens:** Scope or schedule changes occur. FCO formally documents the change, captures commercial and schedule impact, and is tied to both the original estimate and the work order.

**System actions:**
- Create `FcoDocument` linked to `Estimate.LinkedEstimateId` (required) AND `WorkOrder.LinkedWorkOrderId` (required)
- Add `FcoLaborLine` records: Position from rate book → hours → rates auto-filled → subtotal calculated
- `FcoDocument.ScheduleImpactDays` set
- `FcoDocument.TotalFcoAmount` = sum of FcoLaborLine subtotals + markup
- `FcoTaskLink` records created (link FCO to affected PlanTasks/StepOutSteps)
- FCO approval workflow: Draft → Submitted → Approved | Rejected → Signed
- On approval: `CommercialAuthorization.AuthorizedValue` may be amended OR a supplemental authorization created

**Business rule:**
- Every FCO must have `LinkedEstimateId` (NOT NULL)
- Every FCO must have `LinkedWorkOrderId` (NOT NULL)
- FCO labor rates come from `Estimate.RateBook.RateBookLaborRates` (via LinkedEstimateId chain)
- No orphan FCOs

**Ownership:** Planning module (FCO management), Estimating (rate source).

---

### STAGE 11: Actuals Capture

**What happens:** Actual cost, hours, and billable amounts are recorded against the work order, task, step, and optionally the FCO that authorized the work.

**System actions:**
- Create `ActualEntry` records:
  - `WorkOrderId` (required)
  - `ActualType`: Labor | Equipment | Material | Subcontract | Other
  - `CostAmount`, `BillableAmount`, `BilledAmount`
  - Optional: `PlanTaskId`, `StepOutStepId`, `StepOutSubStepId`, `FcoDocumentId`
- Roll up to WorkOrder, PlanTask, Project, Estimate for variance reporting

**Business rule:** No `ActualEntry` without `WorkOrderId`. If actuals are tied to FCO-authorized work, `FcoDocumentId` link is required.

**Ownership:** Planning module (actuals entry and rollup).

---

### STAGE 12: Closeout

**What happens:** Work complete. Actuals finalized. Invoicing done. Project formally closed. Lessons learned captured.

**System actions:**
- All `WorkOrder.Status = 'Complete'` then → `Closed`
- `Project.Status = 'Closed'`
- `CommercialAuthorization.Status = 'Closed'`
- `Estimate.Status = 'Awarded'` (stays as awarded historical record)
- Capture `Project.LessonsLearnedNotes` (free text)
- Final variance report generated: estimate vs actual, WO vs actual, FCO vs actual
- Data frozen for historical reference (no edits, read-only access)

**Business rule:** Project cannot be Closed until all WorkOrders are Complete/Closed and all ActualEntry records are confirmed.

**Ownership:** Planning module.

---

## RECOMMENDED ARCHITECTURE

### Module Ownership Boundaries

```
┌──────────────────────────────────────────────────────────────────────────┐
│  PORTAL (thin — reads all three, owns nothing)                           │
│  Projects at Risk | Upcoming Milestones | WO Release Issues | FCO Alerts │
└────────────────────────┬─────────────────────────────────────────────────┘
                         │
         ┌───────────────┼───────────────────┐
         ▼               ▼                   ▼
┌─────────────────┐  ┌────────────────┐  ┌──────────────────┐
│  ESTIMATING     │  │  PLANNING / PM  │  │  SCHEDULING      │
│                 │  │                │  │                  │
│ Estimate        │  │ CommAuth       │  │ Resource         │
│ EstimateRevision│  │ Project        │  │ Craft            │
│ FcoEntry        │  │ ProjectPhase   │  │ Certification    │
│ LaborRow etc.   │  │ PlanTask       │  │ AvailabilityBlock│
│ RateBook        │  │ Milestone      │  │ Assignment       │
│ CostBook        │  │ WorkOrder      │  │                  │
│ StaffingPlan    │  │ StepOutPlan    │  │ READS from:      │
│                 │  │ StepOutStep    │  │  WorkPackage     │
│ Source of truth │  │ StepOutSubStep │  │  (owned by PM)   │
│ for commercial  │  │ FcoDocument    │  │                  │
│ data / rates    │  │ FcoLaborLine   │  │ Owns:            │
│                 │  │ ActualEntry    │  │  assignments     │
│                 │  │ WorkPackage    │  │  conflict logic  │
└─────────────────┘  └────────────────┘  └──────────────────┘
```

### Entity Ownership Hierarchy

**Estimating owns (source of truth — read-only to Planning/Scheduling):**
- Estimate, EstimateRevision, EstimateSummary
- LaborRow, EquipmentRow, ExpenseRow
- FcoEntry (thin $ line items within estimate)
- RateBook, RateBookLaborRate, RateBookEquipmentRate, RateBookExpenseItem
- CostBook and all sub-entities
- StaffingPlan, StaffingLaborRow
- CrewTemplate, CrewTemplateRow

**Planning / PM owns:**
- CommercialAuthorization
- Project, ProjectPhase, PlanTask, TaskDependency
- Milestone, TimelineBaseline, EstimateTaskLink
- WorkOrder
- StepOutPlan, StepOutStep, StepOutSubStep, StepDependency, StepResourceReq
- WorkPackage (produced by planning, consumed by scheduling)
- FcoDocument, FcoLaborLine, FcoTaskLink
- ActualEntry
- TaskProgressSnapshot

**Scheduling owns:**
- Resource, Craft, Certification, AvailabilityBlock
- Assignment

**Portal owns:**
- Nothing. Aggregates reads from all three.

---

## DOMAIN MODEL (Complete Entity List)

### New Entities Required

#### CommercialAuthorization
The formal customer authorization to proceed with the work. Could be a PO, signed proposal, contract, NTP, release order, or work authorization.

```
CommercialAuthorizationId  int PK
CompanyCode                nvarchar(10) required
EstimateId                 int FK → Estimates, Restrict (required — every auth belongs to an estimate)
AuthorizationType          nvarchar(50) required  -- PO|SignedProposal|Contract|NTP|WorkAuthorization|ReleaseOrder
AuthorizationNumber        nvarchar(100) required  -- PO#, contract#, NTP#
AuthorizedValue            decimal(18,2) required  -- what customer authorized us to do/charge
AuthorizedBy               nvarchar(200) optional  -- customer contact who authorized
ReceivedDate               datetime2 optional
EffectiveDate              datetime2 optional
ExpirationDate             datetime2 optional
Status                     nvarchar(30) default 'Draft'  -- Draft|Submitted|Active|Superseded|Closed|Cancelled
Notes                      nvarchar(max) optional
DocumentReference          nvarchar(500) optional  -- attachment filename/URL
CreatedBy                  nvarchar(100) required
CreatedAt                  datetimeoffset default UtcNow
UpdatedAt                  datetimeoffset default UtcNow
```

#### Project
The execution umbrella for awarded work. One project per awarded estimate (for MVP). Owns the stages, phases, milestones, and work orders.

```
ProjectId                  int PK
CompanyCode                nvarchar(10) required
ProjectNumber              nvarchar(50) required, unique(CompanyCode+ProjectNumber)
Name                       nvarchar(200) required
EstimateId                 int FK → Estimates, Restrict (required)
CommercialAuthorizationId  int? FK → CommercialAuthorizations, SetNull (required before Active)
Client                     nvarchar(200) required
ClientCode                 nvarchar(50) optional
Site                       nvarchar(200) optional
City                       nvarchar(100) optional
State                      nvarchar(50) optional
JobLetter                  nvarchar(10) optional
PlannedStart               datetime2 optional
PlannedEnd                 datetime2 optional
BaselineStart              datetime2 optional
BaselineEnd                datetime2 optional
ActualStart                datetime2 optional
ActualEnd                  datetime2 optional
ForecastEnd                datetime2 optional
Status                     nvarchar(30) default 'Initiating'  -- Initiating|Planning|Active|Monitoring|Closing|Closed|Cancelled
AtRiskThresholdDays        int default 5  -- per-project override for at-risk detection
OwnerUserId                int? optional
LessonsLearnedNotes        nvarchar(max) optional
CreatedBy                  nvarchar(100) required
CreatedAt                  datetimeoffset default UtcNow
UpdatedAt                  datetimeoffset default UtcNow
```

#### WorkOrder
The internal execution package for approved work. Gates all execution. Every executed work item must trace to a WorkOrder.

```
WorkOrderId                int PK
CompanyCode                nvarchar(10) required
WorkOrderNumber            nvarchar(50) required, unique(CompanyCode+WorkOrderNumber)
ProjectId                  int FK → Projects, Restrict (required)
EstimateId                 int FK → Estimates, Restrict (required)
CommercialAuthorizationId  int FK → CommercialAuthorizations, Restrict (required)
Title                      nvarchar(200) required
Description                nvarchar(max) optional
Scope                      nvarchar(max) optional
AuthorizedValue            decimal(18,2) default 0  -- portion of CommAuth.AuthorizedValue this WO covers
PlannedStart               datetime2 optional
PlannedEnd                 datetime2 optional
ActualStart                datetime2 optional
ActualEnd                  datetime2 optional
ForecastEnd                datetime2 optional
Status                     nvarchar(30) default 'Draft'  -- Draft|Released|InProgress|Complete|Closed|Cancelled
ReleasedBy                 nvarchar(100) optional
ReleasedAt                 datetimeoffset optional
CreatedBy                  nvarchar(100) required
CreatedAt                  datetimeoffset default UtcNow
UpdatedAt                  datetimeoffset default UtcNow
```

#### StepOutSubStep
The third and leaf level of execution detail under a StepOutStep (L3). Supports granular time tracking for FCO billing.

```
SubStepId                  int PK
StepId                     int FK → StepOutSteps, Cascade (required)
SubStepCode                nvarchar(20) required  -- "3.1.1", "3.1.2"
SortOrder                  decimal(10,4) required  -- 3.11, 3.12 for sort
Title                      nvarchar(300) required
Description                nvarchar(max) optional
DurationHours              decimal(8,2) optional  -- .25, .5, 1.5 (decimal hours)
RequiredPeople             int default 1
CraftCode                  nvarchar(50) optional
IsParallel                 bit default false  -- can run in parallel with sibling
Status                     nvarchar(30) default 'Pending'  -- Pending|InProgress|Complete|Blocked
ActualStart                datetime2 optional
ActualEnd                  datetime2 optional
ActualDurationHours        decimal(8,2) optional
Notes                      nvarchar(max) optional
SortOrder                  int default 0
```

#### FcoLaborLine
Replaces the `LaborBreakdownJson` blob on FcoDocument. Structured labor line with rate lookup from the estimate's rate book.

```
FcoLaborLineId             int PK
FcoDocumentId              int FK → FcoDocuments, Cascade
Position                   nvarchar(200) required  -- matches RateBookLaborRate.Position
LaborType                  nvarchar(30) default 'Direct'  -- Direct|Indirect
CraftCode                  nvarchar(50) optional
NavCode                    nvarchar(50) optional
StHours                    decimal(10,2) default 0
OtHours                    decimal(10,2) default 0
DtHours                    decimal(10,2) default 0
BillStRate                 decimal(18,4) default 0  -- denormalized from rate book at creation
BillOtRate                 decimal(18,4) default 0
BillDtRate                 decimal(18,4) default 0
Subtotal                   decimal(18,2) default 0  -- (StHours×BillStRate)+(OtHours×BillOtRate)+(DtHours×BillDtRate)
SortOrder                  int default 0
Notes                      nvarchar(max) optional
```

Rate lookup chain: `FcoDocument.LinkedEstimateId → Estimate.RateBookId → RateBook → RateBookLaborRates WHERE Position = X`

#### ActualEntry
Formal recorded actual (cost and billable) against a work order. The universal rollup source for all variance analysis.

```
ActualEntryId              int PK
CompanyCode                nvarchar(10) required
WorkOrderId                int FK → WorkOrders, Restrict (required — no orphan actuals)
FcoDocumentId              int? FK → FcoDocuments, SetNull (optional — if work is FCO-authorized)
PlanTaskId                 int? FK → PlanTasks, SetNull (optional — task-level tracing)
StepOutStepId              int? FK → StepOutSteps, SetNull (optional — step-level tracing)
StepOutSubStepId           int? FK → StepOutSubSteps, SetNull (optional — sub-step-level tracing)
ActualType                 nvarchar(30) required  -- Labor|Equipment|Material|Subcontract|Other
ActualDate                 datetime2 required
Description                nvarchar(500) optional
Position                   nvarchar(200) optional  -- for labor: the crew position
CraftCode                  nvarchar(50) optional
StHours                    decimal(10,2) default 0  -- for labor
OtHours                    decimal(10,2) default 0
DtHours                    decimal(10,2) default 0
CostAmount                 decimal(18,2) required  -- what it actually cost us
BillableAmount             decimal(18,2) default 0  -- what we are entitled to bill
BilledAmount               decimal(18,2) default 0  -- what we actually invoiced
IsConfirmed                bit default false  -- confirmed for billing/reporting
EnteredBy                  nvarchar(100) required
Notes                      nvarchar(max) optional
CreatedAt                  datetimeoffset default UtcNow
```

#### ProjectPhase
Planning grouping within a project. Phases have their own dates and status.

```
PhaseId                    int PK
ProjectId                  int FK → Projects, Cascade
Name                       nvarchar(200) required
Description                nvarchar(max) optional
PlannedStart               datetime2 optional
PlannedEnd                 datetime2 optional
BaselineStart              datetime2 optional
BaselineEnd                datetime2 optional
ActualStart                datetime2 optional
ActualEnd                  datetime2 optional
ForecastEnd                datetime2 optional
SortOrder                  int default 0
Status                     nvarchar(30) default 'Planning'  -- Planning|Active|Complete|OnHold|Cancelled
Color                      nvarchar(20) optional  -- hex for Gantt
```

#### PlanTask
Scheduled work unit within a phase. Has full timeline lifecycle (planned/baseline/actual/forecast) and links to estimates and FCOs.

```
TaskId                     int PK
PhaseId                    int FK → ProjectPhases, Cascade
ParentTaskId               int? FK → PlanTasks (self-ref, NoAction)
Title                      nvarchar(300) required
Description                nvarchar(max) optional
TaskType                   nvarchar(30) default 'Task'  -- Task|Milestone|Gate
LinkedEstimateId           int? FK → Estimates, Restrict  -- primary estimate (convenience)
LinkedFcoDocumentId        int? FK → FcoDocuments, Restrict  -- primary FCO (convenience)
LinkedStepOutPlanId        int? FK → StepOutPlans, SetNull
PlannedStart               datetime2 optional
PlannedEnd                 datetime2 optional
DurationDays               int default 0
BaselineStart              datetime2 optional
BaselineEnd                datetime2 optional
BaselineDuration           int optional
ActualStart                datetime2 optional
ActualEnd                  datetime2 optional
ForecastEnd                datetime2 optional
PercentComplete            decimal(5,2) default 0
Status                     nvarchar(30) default 'NotStarted'
CraftCode                  nvarchar(50) optional
AssignedTo                 nvarchar(100) optional
OwnerUserId                int? optional
SortOrder                  int default 0
IsMilestone                bit default false
CreatedBy                  nvarchar(100) required
CreatedAt                  datetimeoffset default UtcNow
UpdatedAt                  datetimeoffset default UtcNow
```

#### Milestone
Named date checkpoint. Hard deadline or critical gate. Independent of PlanTask.IsMilestone flag.

```
MilestoneId                int PK
ProjectId                  int FK → Projects, Cascade
PhaseId                    int? FK → ProjectPhases, SetNull
TaskId                     int? FK → PlanTasks, SetNull
Name                       nvarchar(200) required
Description                nvarchar(max) optional
PlannedDate                datetime2 required
BaselineDate               datetime2 optional
ActualDate                 datetime2 optional
Status                     nvarchar(30) default 'Pending'  -- Pending|Achieved|Missed|Cancelled
IsDeadline                 bit default false
IsCritical                 bit default false
Color                      nvarchar(20) optional
CreatedBy                  nvarchar(100) required
CreatedAt                  datetimeoffset default UtcNow
```

#### TaskDependency
Predecessor-successor relationships between PlanTasks with dependency type and lag.

```
DependencyId               int PK
SuccessorTaskId            int FK → PlanTasks, Cascade
PredecessorTaskId          int FK → PlanTasks, NoAction
DependencyType             nvarchar(30) default 'FinishToStart'
LagDays                    int default 0
```

#### EstimateTaskLink (junction)
Many-to-many: PlanTask ↔ Estimate.

```
LinkId                     int PK
TaskId                     int FK → PlanTasks, Cascade
EstimateId                 int FK → Estimates, Restrict
LinkType                   nvarchar(30) default 'Primary'  -- Primary|Supporting|Reference
Notes                      nvarchar(max) optional
IsArchived                 bit default false
CreatedAt                  datetimeoffset default UtcNow
UNIQUE INDEX (TaskId, EstimateId)
```

#### FcoTaskLink (junction)
Many-to-many: PlanTask ↔ FcoDocument.

```
LinkId                     int PK
TaskId                     int FK → PlanTasks, Cascade
FcoDocumentId              int FK → FcoDocuments, Restrict
LinkType                   nvarchar(30) default 'ScopeAddition'  -- ScopeAddition|ScheduleImpact|Reference
Notes                      nvarchar(max) optional
CreatedAt                  datetimeoffset default UtcNow
UNIQUE INDEX (TaskId, FcoDocumentId)
```

#### TimelineBaseline
Locked snapshot of planned dates. Enables current-vs-baseline variance.

```
BaselineId                 int PK
ProjectId                  int FK → Projects, Cascade
EntityType                 nvarchar(30) required  -- 'Project'|'Phase'|'Task'
EntityId                   int required  -- naked int (snapshot, not live FK)
SnapshotDate               datetime2 required
PlannedStart               datetime2 optional
PlannedEnd                 datetime2 optional
DurationDays               int optional
BaselineReason             nvarchar(500) optional
BaselineLabel              nvarchar(100) optional  -- "Original", "Post-Storm Re-baseline"
LockedBy                   nvarchar(100) required
LockedAt                   datetimeoffset required
```

#### TaskProgressSnapshot
Periodic progress capture for trend analysis.

```
SnapshotId                 int PK
TaskId                     int FK → PlanTasks, Cascade
SnapshotDate               datetime2 required
PercentComplete            decimal(5,2) required
ForecastEnd                datetime2 optional
Status                     nvarchar(30) required
ReportedBy                 nvarchar(100) required
Notes                      nvarchar(max) optional
CreatedAt                  datetimeoffset required
```

### Existing Entities to Extend

#### StepOutPlan (EXTEND)
```
ADD: WorkOrderId  int? FK → WorkOrders, SetNull
-- Required in business logic (enforced via controller), nullable in DB for migration safety
-- SourceType / LinkedEstimateId / LinkedStaffingPlanId / LinkedFcoDocumentId remain but become
-- secondary/convenience; canonical traceability goes through WorkOrder → Project → Estimate
```

#### StepOutStep (EXTEND)
```
ADD: ParentStepId  int? FK → StepOutSteps, NoAction
-- Enables proper parent-child tree queries
-- Decimal StepCode still used for display numbering
CHANGE: DurationMinutes int? → DurationHours decimal(8,2)?
-- Supports .25 hr, .5 hr, 1.5 hr (user requirement)
-- Note: consider whether existing data needs migration (multiply existing minutes by 1/60)
```

#### FcoDocument (EXTEND)
```
ADD: LinkedWorkOrderId  int? FK → WorkOrders, Restrict
-- Required in business logic; staged safely: nullable first, NOT NULL after backfill
-- LaborBreakdownJson: keep for backwards compatibility; new FcoLaborLine rows are authoritative
-- Note: do NOT drop LaborBreakdownJson until FcoLaborLine is fully wired
```

#### WorkPackage (EXTEND)
```
ADD: WorkOrderId  int? FK → WorkOrders, SetNull
-- Direct link from schedulable demand to work order (cleaner than SourceType/SourceId polymorphic)
```

---

## FULL ENTITY RELATIONSHIP CHAIN

```
Estimate (commercial baseline — Estimating module)
│  ├─ EstimateRevision[]
│  ├─ EstimateSummary
│  ├─ LaborRow[], EquipmentRow[], ExpenseRow[]
│  ├─ FcoEntry[]   (thin $ line items — Estimating only, not used for PM traceability)
│  └─ RateBook → RateBookLaborRates (drives FCO labor cost)
│
├─► CommercialAuthorization  (authorization to proceed — Planning)
│     └─► Project  (execution umbrella — Planning)
│           ├─ ProjectPhase[]
│           │    └─ PlanTask[]
│           │         ├─ TaskDependency[]
│           │         ├─ EstimateTaskLink → Estimate
│           │         ├─ FcoTaskLink → FcoDocument
│           │         ├─ TaskProgressSnapshot[]
│           │         └─ Milestone links
│           ├─ Milestone[]
│           ├─ TimelineBaseline[]
│           └─► WorkOrder[]  (released execution package — Planning)
│                 ├─ StepOutPlan[]  (execution breakdown)
│                 │    └─ StepOutStep[]  (L1/L2 via ParentStepId self-ref)
│                 │         └─ StepOutSubStep[]  (L3 leaf level — NEW)
│                 ├─ WorkPackage[]  (schedulable demand — consumed by Scheduling)
│                 │    └─ Assignment[]  (resource assigned — Scheduling module)
│                 ├─► FcoDocument[]  (change orders — Planning)
│                 │    ├─ FcoLaborLine[]  (structured labor $ lines — NEW)
│                 │    └─ FcoTaskLink → PlanTask
│                 └─ ActualEntry[]  (recorded actuals — Planning)
```

---

## STAGE-GATE MODEL SUMMARY

| Stage | Gate | Entity Created | Prerequisite |
|-------|------|----------------|-------------|
| 1. Bid | — | Estimate (Draft) | None |
| 2. Dev | Internal approval | Estimate (Pending→Awarded) | Estimate exists |
| 3. Auth | Customer authorization | CommercialAuthorization (Active) | Estimate.Status = Awarded |
| 4. Initiate | — | Project | CommAuth.Status = Active |
| 5. Plan | Baseline lock | ProjectPhase, PlanTask, Milestone | Project exists |
| 6. Release | Work order release | WorkOrder (Released) | Project + CommAuth Active |
| 7. Schedule | Assignment | Assignment | WorkPackage.ReadyForScheduling = true |
| 8. Execute | — | Step/SubStep progress | WorkOrder.Status = Released |
| 9. Monitor | At-risk flag | HealthCheck results | Task actuals exist |
| 10. FCO | FCO approval | FcoDocument (Approved) | LinkedEstimateId + LinkedWorkOrderId |
| 11. Actuals | Confirmation | ActualEntry (Confirmed) | WorkOrder exists |
| 12. Close | Final confirmation | Project.Status = Closed | All WOs Complete/Closed |

---

## TIMELINE / MILESTONE / VARIANCE MODEL

See `PROJECT_STAGE_GATE_AND_STATUS_MODEL.md` for full state diagrams.

### Key Variance Calculations

```
ForecastEnd         = Today + (DurationDays × (1 − PercentComplete/100))
FinishVariance      = ForecastEnd − PlannedEnd           (+ve = behind)
ScheduleVariance    = PlannedEnd − ForecastEnd            (+ve = ahead)
BaselineFinishVar   = ForecastEnd − BaselineEnd           (vs locked baseline)
CostVariance        = ActualCost − EstimatedCost
RevenueExposure     = AuthorizedValue − ForecastBillable
```

### At-Risk Threshold Rules
- Default global threshold: 5 days FinishVariance
- Per-project override: `Project.AtRiskThresholdDays`
- Behind: FinishVariance > (threshold × 2) OR milestone has slipped
- At Risk: 0 < FinishVariance ≤ threshold OR milestone within threshold days of slipping
- On Track: FinishVariance ≤ 0

---

## SCHEDULING INTEGRATION MODEL

### Planning → Scheduling Data Flow
1. `PlanTask` + `StepOutPlan` drive `WorkPackage` creation (craft + headcount + dates)
2. `WorkPackage.ReadyForScheduling = true` surfaces in Scheduling Jobs Board
3. `Assignment` records created: Resource → WorkPackage
4. `Assignment` completion triggers `ActualEntry` creation (hours logged back to Planning)

### Ownership at Boundary
- Planning OWNS: WorkPackage (creates, manages ReadyForScheduling)
- Scheduling OWNS: Assignment (creates, conflict-detects)
- Planning receives back: ActualEntry (from assignment completion data)

---

## FCO LABOR RATE LOOKUP MODEL

When creating an `FcoLaborLine` on an `FcoDocument`:

```
FcoDocument.LinkedEstimateId
  → Estimate.RateBookId
  → RateBook
  → RateBook.RateBookLaborRates (filter WHERE Position = selected position)
  → Auto-fill: BillStRate, BillOtRate, BillDtRate
```

User selects Position from dropdown (populated from the estimate's rate book positions). Rates are denormalized onto `FcoLaborLine` at creation time. If the rate book changes later, existing FCO lines are NOT retroactively updated (they represent the rate at the time of the FCO).

---

## CLOSEOUT AND LESSONS LEARNED MODEL

### Closeout Requirements
- All `WorkOrder.Status` = Complete or Closed
- All `Milestone.Status` = Achieved or Cancelled (no Pending milestones)
- All `ActualEntry.IsConfirmed = true`
- Final variance report generated
- `Project.LessonsLearnedNotes` populated (optional but encouraged)

### Historical Reference
- Closed projects are read-only
- Estimate-to-actual comparison remains available for all time
- Actuals and variances feed the "Lessons Learned / Historical Reference" view
- Future estimates can reference historical actuals for similar work types

---

## MVP vs PHASE 2 vs PHASE 3

### MVP (Must Ship)
- CommercialAuthorization entity + basic CRUD
- Project entity with stages + milestone basics
- WorkOrder entity (create, release, track)
- StepOutSubStep (L3 leaf level)
- StepOutStep ParentStepId self-ref
- FcoDocument: add WorkOrderId FK, replace LaborBreakdownJson with FcoLaborLine
- ActualEntry (basic Labor + Equipment entry)
- ProjectPhase + PlanTask + Milestone (timeline tracking)
- Schedule health (FinishVariance, on-track/at-risk/behind)
- Custom SVG Gantt chart
- Portal "Projects at Risk" KPI

### Phase 2 (Next Sprint)
- Full TaskDependency + critical path (simple longest-chain)
- TimelineBaseline locking
- Traceability matrix view (Estimate → WO → Task)
- Actuals rollup reports (WO vs actual, Project vs actual)
- FcoTaskLink + EstimateTaskLink full UI
- Schedule variance alerting and notifications

### Phase 3 (Future)
- Full CPM critical path algorithm
- Actuals import from field systems
- Lessons learned structured capture + search
- AI assistant integration across planning views
- Multi-estimate / multi-WO project scenarios
- Client portal view (read-only project status)
