# PROJECT STAGE GATE AND STATUS MODEL
## Stronghold Enterprise — Status Lifecycles, Transitions, Gates, At-Risk Rules
**Document Version:** 1.0  
**Date:** 2026-04-28  
**Status:** APPROVED — Implementation Pending  

---

## OVERVIEW

This document defines the exact status model for every entity in the project lifecycle, including valid transitions, gate conditions, and at-risk/on-track/late rules. All transitions are enforced server-side.

---

## ESTIMATE STATUS MODEL

### States

| Status | Meaning |
|--------|---------|
| `Draft` | Being built. Not yet submitted. Can be freely edited. |
| `Pending` | Under review. Minor edits allowed. |
| `Submitted for Approval` | Formal internal review. Limited edits. |
| `Awarded` | Customer has accepted. Commercial baseline locked. Forward flows now permitted. |
| `Lost` | Customer declined or awarded elsewhere. Reason required. No forward flows. |
| `Cancelled` | Withdrawn before decision. No forward flows. |

### Valid Transitions

```
Draft → Pending
Draft → Cancelled

Pending → Draft  (reopen for editing)
Pending → Submitted for Approval
Pending → Awarded  (expedited / direct award)
Pending → Lost
Pending → Cancelled

Submitted for Approval → Pending  (sent back for revision)
Submitted for Approval → Awarded
Submitted for Approval → Lost
Submitted for Approval → Cancelled

Awarded → Cancelled  (rare; admin only; requires note)
  NOTE: Cannot go back to Draft/Pending once Awarded
  NOTE: Cannot hard-delete an Estimate with downstream records (CommAuth, Project, ActualEntry)

Lost → Pending  (reopen if customer reconsiders; admin only)
Cancelled → Pending  (reopen; admin only)
```

### Gate Rules
- `Awarded` status enables: CommercialAuthorization creation, Project creation
- `Lost` or `Cancelled` blocks all forward flows
- `Lost` requires `LostReason` field (server-side enforced)

---

## COMMERCIALAUTHORIZATION STATUS MODEL

### States

| Status | Meaning |
|--------|---------|
| `Draft` | Being prepared. Not yet submitted to customer. |
| `Submitted` | Sent to customer for signature / PO issuance. |
| `Active` | Customer has authorized. Enables Project and WorkOrder creation. |
| `Superseded` | Replaced by a new/amended authorization (e.g., after FCO changes scope). |
| `Closed` | Job complete, authorization formally closed. |
| `Cancelled` | Authorization voided. No forward flows from this auth. |

### Valid Transitions

```
Draft → Submitted
Draft → Cancelled

Submitted → Active  (customer provides PO/NTP/signature)
Submitted → Cancelled

Active → Superseded  (new amended auth issued for same estimate)
Active → Closed  (job complete, admin only)

Superseded → [no further transitions — historical record]
Closed → [no further transitions]
Cancelled → Draft  (rare; admin only; reinstatement)
```

### Gate Rules
- `Active` status required before: Project can move to Active, WorkOrder can be Released
- If CommAuth is Cancelled/Superseded: existing WorkOrders freeze (cannot be Released); alert raised
- Multiple CommAuths can exist for one Estimate (original + amendments)
- Only one CommAuth per Estimate can be `Active` at a time (enforced by server)

---

## PROJECT STATUS MODEL

### States

| Status | Meaning |
|--------|---------|
| `Initiating` | Project created; ownership assigned; basic setup in progress. |
| `Planning` | Phases, tasks, milestones, and step-out plans being developed. No work released yet. |
| `Active` | At least one WorkOrder released; execution underway. |
| `Monitoring` | Active execution; schedule health being tracked; at-risk flags may be set. |
| `Closing` | All work complete; actuals being finalized; not yet formally closed. |
| `Closed` | All WOs closed; actuals confirmed; lessons learned captured. Read-only. |
| `OnHold` | Work paused (e.g., customer request, weather, permit delay). |
| `Cancelled` | Project voided; no further execution. |

### Valid Transitions

```
Initiating → Planning
Initiating → Cancelled

Planning → Active  (auto-transition when first WorkOrder is Released)
Planning → Cancelled

Active → Monitoring  (auto-transition after first actual is recorded or manually set)
Active → OnHold
Active → Cancelled

Monitoring → Closing  (when all tasks marked Complete)
Monitoring → OnHold
Monitoring → Cancelled

OnHold → Active  (resumption)
OnHold → Cancelled

Closing → Closed  (when all gate requirements met — see Gate 7 in ownership doc)
Closing → Active  (if work reopens)

Closed → [read-only; no transitions]
Cancelled → [no transitions]
```

### Auto-Transitions
- `Planning → Active`: triggered when first WorkOrder status = 'Released'
- `Active → Monitoring`: triggered when first ActualEntry is confirmed OR after 7 days of Active status
- `Monitoring → Closing`: triggered when ALL PlanTasks have Status IN ('Complete', 'Cancelled') — server check on task update

### Stage Rollup for Portal Display

```
Project Health (worst-case rollup):
  Complete    = all phases/tasks complete
  On Track    = no tasks At Risk or Behind
  At Risk     = at least one task At Risk (0 < FinishVariance ≤ AtRiskThresholdDays)
  Behind      = at least one task Behind (FinishVariance > AtRiskThresholdDays OR milestone slipped)
  Blocked     = at least one task Blocked
  On Hold     = Project.Status = 'OnHold'
  Cancelled   = Project.Status = 'Cancelled'
```

---

## WORKORDER STATUS MODEL

### States

| Status | Meaning |
|--------|---------|
| `Draft` | Being built. Not yet released. No field execution. |
| `Released` | Formally released to operations. Field execution may begin. |
| `InProgress` | Active execution underway. |
| `Complete` | All work finished. Final actuals being confirmed. |
| `Closed` | Final actuals confirmed. Work order formally closed. |
| `Cancelled` | Work order voided. No actuals should be entered. |

### Valid Transitions

```
Draft → Released  (gate: CommAuth Active + Estimate Awarded + AuthorizedValue check)
Draft → Cancelled

Released → InProgress  (auto: when first StepOutStep goes InProgress)
Released → Cancelled  (admin only; refund/void procedure required)

InProgress → Complete  (when all StepOutSteps/Plans complete)
InProgress → Cancelled  (admin only; exceptional)

Complete → Closed  (when ActualEntry.IsConfirmed = true for all actuals)
Complete → InProgress  (if rework required)

Closed → [read-only]
Cancelled → [no transitions]
```

### Gate: Draft → Released

Server-side checks (all must pass):
1. `WorkOrder.ProjectId` links to a Project with `CommercialAuthorization.Status = 'Active'`
2. `Estimate.Status = 'Awarded'`
3. `WorkOrder.AuthorizedValue > 0`
4. Sum of all Active/Released WO AuthorizedValues on same CommAuth ≤ CommAuth.AuthorizedValue
5. `StepOutPlan` linked (at least one StepOutPlan with WorkOrderId = this WorkOrder) — soft warning if missing

On failure: HTTP 422 with specific reason code per failed check.

---

## PLANTASK STATUS MODEL

### States

| Status | Meaning |
|--------|---------|
| `NotStarted` | Planned but work has not begun. |
| `InProgress` | Work underway. ActualStart is set. |
| `Complete` | Work finished. ActualEnd is set. |
| `Blocked` | Cannot proceed. Reason noted. |
| `Cancelled` | Task removed from scope. |

### Valid Transitions

```
NotStarted → InProgress  (soft gate: warn if no EstimateTaskLink; allow with confirmation)
NotStarted → Blocked
NotStarted → Cancelled

InProgress → Complete    (sets ActualEnd = today)
InProgress → Blocked
InProgress → Cancelled

Blocked → InProgress
Blocked → Cancelled

Complete → InProgress    (if rework required; admin only)
Cancelled → [no transitions]
```

### Health Calculation

```
ForecastEnd = ActualStart + (DurationDays × (1 - PercentComplete/100))
  IF ForecastEnd IS NULL: use PlannedEnd as ForecastEnd for health calc

FinishVariance = ForecastEnd - PlannedEnd  (days; positive = behind)
Threshold = Project.AtRiskThresholdDays (default 5)

Status        Condition
Complete      ActualEnd IS NOT NULL
On Track      FinishVariance <= 0
At Risk       0 < FinishVariance <= Threshold
Behind        FinishVariance > Threshold × 2 OR milestone linked to this task has slipped
Blocked       Task.Status = 'Blocked'
Not Started   ActualStart IS NULL AND PlannedStart > Today
Overdue       ActualStart IS NULL AND PlannedStart <= Today - 1
```

---

## STEPOUTSTEP STATUS MODEL

### States

| Status | Meaning |
|--------|---------|
| `Pending` | Not started. |
| `InProgress` | Actively being worked. |
| `Complete` | Finished. |
| `Blocked` | Halted. Blocker noted in Notes field. |

### Valid Transitions

```
Pending → InProgress   (gate: WorkOrder.Status IN ('Released', 'InProgress'))
Pending → Blocked

InProgress → Complete
InProgress → Blocked
InProgress → Pending   (if step needs to restart; rare)

Blocked → InProgress
Blocked → Pending

Complete → InProgress  (rework; rare)
```

### Gate: Pending → InProgress
- `StepOutPlan.WorkOrderId IS NOT NULL`
- `WorkOrder.Status IN ('Released', 'InProgress')`
- Returns HTTP 422 if WorkOrder not in an executable state

---

## STEPOUTSUBSTEP STATUS MODEL

Identical to StepOutStep. Same four states (Pending, InProgress, Complete, Blocked) and same transition rules.

### Additional Duration Tracking
- `DurationHours` (decimal): planned duration in hours (.25, .5, 1.0, 1.5)
- `ActualDurationHours` (decimal): set when Complete; calculated as `ActualEnd - ActualStart` in hours or manually entered

---

## MILESTONE STATUS MODEL

### States

| Status | Meaning |
|--------|---------|
| `Pending` | Future target. Not yet achieved. |
| `Achieved` | `ActualDate` recorded. Milestone met. |
| `Missed` | `PlannedDate` has passed without being achieved. System auto-detects; user confirms. |
| `Cancelled` | Milestone removed from scope. |

### Valid Transitions

```
Pending → Achieved   (sets ActualDate = today unless specified)
Pending → Missed     (auto-flagged by system when PlannedDate < Today AND ActualDate IS NULL)
Pending → Cancelled

Achieved → [read-only; historical record]
Missed → Achieved    (late achievement; ActualDate set to actual completion date)
Missed → Cancelled

Cancelled → [no transitions]
```

### Auto-Detection Logic (Server, ScheduleHealthService)
```
Run nightly or on demand:
  UPDATE Milestones
  SET Status = 'Missed'
  WHERE Status = 'Pending'
    AND PlannedDate < GETUTCDATE()
    AND ActualDate IS NULL

At-Risk Detection:
  Milestone is At Risk when:
    Status = 'Pending'
    AND PlannedDate <= GETUTCDATE() + Project.AtRiskThresholdDays
    AND ActualDate IS NULL
```

### IsDeadline Behavior
- `IsDeadline = true` flags milestones that are contractual or hard deadlines
- Missed deadline milestones trigger an escalation flag on the Project
- Displayed in red in all views (Gantt, Calendar, Milestone Tracker)

---

## FCODOCUMENT STATUS MODEL

### States

| Status | Meaning |
|--------|---------|
| `Draft` | Being prepared. Rates and amounts being calculated. |
| `Submitted` | Sent to customer for approval. |
| `Approved` | Customer has approved. Commercial impact recognized. |
| `Rejected` | Customer declined the FCO. |
| `Signed` | Physical/electronic signature received. Final state before work proceeds. |

### Valid Transitions

```
Draft → Submitted
Draft → [can be deleted if no linked actuals]

Submitted → Approved
Submitted → Rejected
Submitted → Draft  (customer sends back for revision)

Approved → Signed
Approved → Rejected  (rare; if approval rescinded)

Rejected → Draft   (if PM revises and resubmits)

Signed → [read-only; historical record]
```

### Gate Rules
- Cannot submit FCO without `LinkedEstimateId` (returns HTTP 422)
- Cannot submit FCO without `LinkedWorkOrderId` (returns HTTP 422)
- Cannot approve FCO without at least one `FcoLaborLine` OR `ScheduleImpactDays > 0`
- Signed FCO: `TotalFcoAmount` is locked; if schedule impact, `PlanTask.PlannedEnd` should be updated by PM

---

## AT-RISK / ON-TRACK / BEHIND RULES

### Task-Level Health

```
Threshold = Project.AtRiskThresholdDays (default: 5 days)

Complete          ActualEnd IS NOT NULL
On Track          FinishVariance <= 0
At Risk           0 < FinishVariance <= Threshold
Behind            FinishVariance > Threshold
                  OR any linked Milestone is Missed
Blocked           Task.Status = 'Blocked'
Not Started       ActualStart IS NULL AND PlannedStart > Today
Overdue           ActualStart IS NULL AND PlannedStart < Today
```

### Phase-Level Health (Rollup from Tasks)

```
Complete  = all tasks Complete or Cancelled
On Track  = no tasks At Risk, Behind, or Blocked; and no Missed milestones in phase
At Risk   = at least one task At Risk (but none Behind); or one milestone At Risk
Behind    = at least one task Behind; or any linked milestone Missed
Blocked   = at least one task Blocked (and no Behind tasks)
```

### Project-Level Health (Rollup from Phases)

```
Closed    = Project.Status = 'Closed'
On Hold   = Project.Status = 'OnHold'
Complete  = all phases Complete or Cancelled
On Track  = no phases At Risk, Behind, or Blocked
At Risk   = at least one phase At Risk
Behind    = at least one phase Behind OR any project-level milestone Missed
Blocked   = at least one phase Blocked (and no Behind phases)
```

### Forecast Exceeds Authorized Alert

```
IF SUM(ActualEntry.BillableAmount) + estimated_remaining_billable > CommercialAuthorization.AuthorizedValue
THEN raise ForecastExceedsAuthorized flag on Project

This is a visibility alert, not a hard block.
Action: PM must initiate FCO or request supplemental authorization.
```

---

## STAGE TRANSITIONS AND GATES SUMMARY TABLE

| Transition | Gate Condition | Gate Enforcer |
|-----------|----------------|---------------|
| Estimate → Awarded | Internal approval | EstimatesController |
| Estimate → Lost | LostReason required | EstimatesController |
| CommAuth → Active | Customer delivers PO/NTP/signature | CommercialAuthorizationController |
| Project created | Estimate=Awarded, CommAuth=Active | ProjectController |
| Project → Active | First WorkOrder Released | Auto-trigger on WorkOrderController |
| WorkOrder → Released | Estimate=Awarded, CommAuth=Active, authorized value check | WorkOrderController |
| StepOutStep → InProgress | WorkOrder in Released/InProgress | PlanningController / StepController |
| PlanTask → InProgress | Soft warning if no EstimateTaskLink | PlanTaskController |
| FcoDocument → Submitted | LinkedEstimateId + LinkedWorkOrderId required | FcoController |
| ActualEntry created | WorkOrderId required | ActualEntryController |
| Project → Closed | All WOs closed, actuals confirmed | ProjectController |
