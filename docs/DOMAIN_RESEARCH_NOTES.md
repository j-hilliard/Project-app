# Domain Research Notes
## Planning + Scheduling for Industrial Services / Turnaround Work

---

## 1. What a Scheduling App Needs in This Context

### Core Problem
Stronghold fields craft workers (Pipefitters, Boilermakers, Welders, Electricians, Crane Operators, etc.) on industrial turnarounds and maintenance jobs. The scheduler needs to know:
- Who is available (not already assigned, not on PTO, cert not expired)
- What jobs need coverage (by craft, by date)
- Which crafts are short (demand > assigned)
- Who is about to roll off (ending soon) and where they can go next

### Resource Model
- Resources have a **primary craft** and may have secondary skills
- Resources are associated with a **company** (CSL, ETS, STS, STG) and **branch**
- Resources carry **certifications** (OSHA-10, OSHA-30, H2S, CPR, rigging, etc.) with expiration dates
- Assignments are typically multi-day or multi-week (not hourly scheduling)

### Assignment Types
- **Short-cycle**: 1–5 days (turnaround prep, FCO work, spot maintenance)
- **Long-cycle**: 2–8 weeks (full turnaround projects)
- Assignments need: resource, job/work-package, craft, start date, end date, shift (day/night/rotation)

### Conflict Detection
1. **Double-booking**: same resource assigned to two jobs on overlapping dates
2. **Unavailable**: resource has an AvailabilityBlock covering the assignment dates
3. **Uncertified**: resource lacks a required cert for the job type
4. **Over-capacity**: more people assigned than a job's max crew size (if tracked)

### Shortage Calculation
`gap = demand.requiredHeadcount - COUNT(assignments WHERE craft = craft AND dates overlap)`
- Demand comes from: estimates + approved unconverted staffing plans + work packages
- Shortage threshold: gap > 0

### Roll-Off / Available-Soon
- **Ending soon**: assignment.endDate within N days of today (default 7 days)
- **Available soon**: resource has no assignment starting within N days after their current assignment ends
- These two signals together = "who can I move to the next job"

### Suggested Matching
Match rolling-off resources to upcoming demand:
1. Filter demand starting ≤ 14 days after the resource's roll-off date
2. Match by craft
3. Optionally filter by location proximity / branch

---

## 2. What a Planning App Needs in This Context

### Core Problem
Before scheduling people, a PM or estimator needs to know **what work needs to happen and in what order**. The step-out plan is a structured field walk: "here's every action we need to take, who does it, and in what sequence."

### Step-Out Planning Workflow
1. Receive an estimate or FCO
2. Walk the site (mentally or physically) and list every task
3. Order tasks with dependencies (Step 2 can't start until Step 1 is done)
4. Identify parallel tasks (Step 1.5 can run while Step 2 is happening)
5. Identify craft requirements per task
6. Estimate durations
7. Generate work packages for scheduling
8. Track actuals as work proceeds

### Decimal Step Numbering
In the field, steps are inserted between other steps constantly:
- "1" → "1.5" → "2" (inserted between 1 and 2)
- "2" → "2.1" → "2.2" → "3"
This is why step codes must be strings, not integers.

### Dependencies
- **Sequential**: Step 3 cannot start until Steps 1 AND 2 are complete
- **Parallel**: Step 1.5 can run simultaneously with Step 2
- **Permit-blocked**: Step 3 requires a permit (Step 2 = get permit, Step 3 = do work)

### Work Packages
A work package is a schedulable unit generated from a step-out plan:
- Has a craft requirement
- Has a date range
- Can be assigned to resources in the scheduling app
- Has a link back to the source plan/estimate/FCO

### Actuals
As work progresses, capture:
- Actual labor hours per craft per day
- Actual material consumption
- Actual equipment usage
- Compare against planned/estimated baseline

---

## 3. How Planning Feeds Scheduling

```
Step-Out Plan
    ↓
  Step with craft="Pipefitter", requiredPeople=3, duration=2 days
    ↓
  Generate Work Package
    ↓
  WorkPackage: craft=Pipefitter, qty=3, start=Aug-05, end=Aug-06, ReadyForScheduling=true
    ↓
  SchedulingDemandService picks it up
    ↓
  Dispatcher assigns 3 Pipefitters to this work package
    ↓
  Assignments created: PF-001, PF-002, PF-003 → WorkPackage-XYZ
```

---

## 4. How Planning/Scheduling Feed Estimate Variance

### Estimate Baseline
From the estimate: planned hours per position × bill rate = planned billable value; × cost rate = planned cost.

### Actuals
From work package actual labor entries:
- actual hours × bill rate (from rate book) = actual billable value
- actual hours × cost rate (from cost book) = actual cost

### Delta/Variance
```
delta_hours = actual_hours - planned_hours
delta_billable = actual_billable - planned_billable
delta_cost = actual_cost - planned_cost
delta_margin = actual_margin_pct - planned_margin_pct
```

Positive delta_cost = over budget. Negative delta_billable = under-billing.

### FCO Delta
Same math but scoped to the FCO scope change (not the full estimate). An FCO adds or changes scope; variance tells you whether the FCO work was executed as priced.

---

## 5. FCO / Change Order Document Requirements

Per industry standard (Autodesk, NECA, etc.), a signable change order must include:

**Header Section**
- Project name and address
- Original contract/estimate number
- Client/customer name and contact
- Contractor name and contact
- FCO/Change Order number
- Date issued

**Scope Section**
- Description of changed scope vs. original estimate
- Reason/basis for change (owner request, site condition, design change, etc.)
- Reference to original estimate section if applicable

**Schedule Section**
- Schedule impact: added or reduced days
- Revised completion date if applicable

**Cost Breakdown**
- Labor: position, hours, rate, total per position
- Material: item, quantity, unit cost, total
- Equipment: item, qty, days/hours, rate, total
- Subtotals for each category
- Markup / overhead / profit percentage
- Tax if applicable
- Total FCO amount
- Updated total contract/estimate value

**Approval Section**
- Requested by (field name, signature, date)
- Prepared by (estimator name, signature, date)
- Client approval (name, signature, date)
- Contractor approval (PM name, signature, date)

**Status Tracking**
- Draft → Submitted → Approved / Rejected / Signed
- Revision history (if FCO is revised before approval)

---

## 6. Crew Composition Patterns

Common industrial turnaround crew compositions:
- **Pipefitting crew**: General Foreman (1), Foreman (1–2), Journeymen (4–8), Helpers (2–4)
- **Welding crew**: Foreman (1), Welder JM (2–4), Fitter JM (2–4)
- **Boilermaker crew**: Foreman (1), BM JM (4–6), Helpers (2–3)
- **Crane/rigging**: Crane Operator (1), Rigger (2–4), Oiler (1)
- **Safety**: Safety Watch (1–2 per crew), Safety Supervisor (1 per site)
- **NDT**: NDT Tech (1–2)

These patterns inform the crew template library and the scheduling suggested-match logic.
