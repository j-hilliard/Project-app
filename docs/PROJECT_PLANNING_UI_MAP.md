# PROJECT PLANNING UI MAP
## Stronghold Enterprise — Planning / PM Module
**Document Version:** 1.0  
**Date:** 2026-04-28  
**Status:** APPROVED — Implementation Pending  

---

## ROUTE MAP

All planning routes are nested under `/planning` with the shared `AppLayout.vue` shell.

```
/planning                                           → redirect → /planning/projects
/planning/projects                                  → ProjectListView
/planning/projects/new                              → ProjectDetailView (create mode)
/planning/projects/:id                              → ProjectDetailView (edit/view mode)
/planning/projects/:id/gantt                        → ProjectGanttView
/planning/projects/:id/calendar                     → ProjectCalendarView
/planning/projects/:id/phases/:phaseId              → ProjectPhaseView
/planning/projects/:id/phases/:phaseId/tasks/:taskId → PlanTaskView
/planning/step-out-plans                            → StepOutPlanListView  (EXISTING)
/planning/step-out-plans/new                        → StepOutPlanFormView  (EXISTING)
/planning/step-out-plans/:id                        → StepOutPlanFormView  (EXISTING)
/planning/work-packages                             → WorkPackageListView  (EXISTING)
/planning/fco                                       → FcoListView          (EXISTING)
/planning/milestones                                → MilestoneView
/planning/health                                    → ScheduleHealthView
/planning/traceability                              → TraceabilityView
```

---

## UPDATED PLANNING MENU (apps.ts)

```
Planning Module Sidebar:
├── Projects
│   ├── All Projects         /planning/projects
│   └── New Project          /planning/projects/new
├── Plans
│   ├── Step-Out Plans       /planning/step-out-plans   (existing)
│   └── New Step-Out Plan    /planning/step-out-plans/new  (existing)
├── Work & Change
│   ├── Work Packages        /planning/work-packages    (existing)
│   └── FCO / Change Orders  /planning/fco              (existing)
├── Views
│   ├── Milestone Tracker    /planning/milestones
│   └── Schedule Health      /planning/health
└── Traceability
    └── Estimate → Task Matrix /planning/traceability
```

---

## PAGE MAP

---

### ProjectListView (`/planning/projects`)

**Purpose:** Entry point for all project plans. Shows all projects for the current company with health status at a glance.

**Layout:**
```
┌─────────────────────────────────────────────────────────────┐
│  [+ New Project]  [View: Table | Card]  [Filters: ▼]        │
├────────────────┬────────┬────────┬───────┬──────┬───────────┤
│ Project #      │ Name   │ Client │ Dates │Status│ Health    │
├────────────────┼────────┼────────┼───────┼──────┼───────────┤
│ PRJ-2026-001   │ Bayou… │ Refin… │ Apr…  │Active│ ⬤ At Risk │
│ PRJ-2026-002   │ Gulf…  │ Pipel… │ Jun…  │Plann.│ ⬤ On Track│
└────────────────┴────────┴────────┴───────┴──────┴───────────┘
[Pagination: 25 | 50 | 100]
```

**Card View:**
```
┌───────────────────────────────┐
│ PRJ-2026-001        [Active]  │
│ Bayou Refinery Turnaround     │
│ Client: Motiva Refinery       │
│ Apr 1 – Jun 30, 2026          │
│ ⬤ At Risk  |  3 phases  |  72%│
└───────────────────────────────┘
```

**Filters:** Status (All|Planning|Active|Complete|OnHold|Cancelled), Client, Year, Quick Search  
**Actions:** Click row/card → ProjectDetailView; + New Project → ProjectDetailView create mode  
**Data source:** `GET /api/v1/projects?companyCode=X&status=Y`

---

### ProjectDetailView (`/planning/projects/:id`)

**Purpose:** Project overview with tab navigation into Gantt, Calendar, and drill-down to phases.

**Layout:**
```
┌─────────────────────────────────────────────────────────────────────┐
│  ← Back to Projects                                    [Edit] [⚙️]  │
│  Bayou Refinery Turnaround 2026          [Active]                   │
│  Client: Motiva | Site: Bayou | Apr 1 – Jun 30, 2026                │
├──────────┬──────────┬──────────┬──────────┬──────────┬─────────────┤
│ 3 Phases │ 15 Tasks │ 2 Milest.│   72%    │ -3 days  │ ⬤ At Risk  │
│          │          │ (1 at risk│complete  │ variance │            │
├──────────┴──────────┴──────────┴──────────┴──────────┴─────────────┤
│ [Overview] [Gantt] [Calendar] [Milestones] [Traceability]           │
├─────────────────────────────────────────────────────────────────────┤
│  OVERVIEW TAB                                                        │
│                                                                      │
│  Phases:                                                             │
│  ┌──────────────────────┬────────┬────────┬──────┬────────────────┐ │
│  │ Phase                │ Start  │ End    │Tasks │ Health         │ │
│  ├──────────────────────┼────────┼────────┼──────┼────────────────┤ │
│  │ Mobilization         │ Apr 1  │ Apr 15 │  4   │ ⬤ Complete    │ │
│  │ Main Execution       │ Apr 15 │ Jun 15 │  9   │ ⬤ At Risk     │ │
│  │ Demobilization       │ Jun 15 │ Jun 30 │  2   │ ⬤ Not Started │ │
│  └──────────────────────┴────────┴────────┴──────┴────────────────┘ │
│  [+ Add Phase]                                                       │
└─────────────────────────────────────────────────────────────────────┘
```

**KPI strip fields:** Phase count, Task count, Milestone count (X at risk), % complete, Schedule variance (days), Health status  
**Tabs:** Overview | Gantt | Calendar | Milestones | Traceability  
**Actions:** Edit project → form dialog; ⚙️ dropdown → Lock Baseline, Archive, Cancel  
**Data source:** `GET /api/v1/projects/:id/full`

---

### ProjectGanttView (`/planning/projects/:id/gantt`)

**Purpose:** Custom SVG Gantt chart showing project phases, tasks, milestones, and dependencies.

**Layout:**
```
┌─────────────────────────────────────────────────────────────────────────────┐
│ [← Back]  Bayou Refinery Turnaround          [Zoom: Day|Week|Month] [⚙️]   │
│ Filters: [All Phases ▼] [All Status ▼] [All Craft ▼] [☐ Show Baseline]     │
├─────────────────────┬───────────────────────────────────────────────────────┤
│ Phase / Task        │ Apr          │ May          │ Jun                      │
│                     │ 1  8  15  22 │ 1  8  15  22 │ 1  8  15  22            │
├─────────────────────┼───────────────────────────────────────────────────────┤
│ ▼ Mobilization      │ ██████████   │              │                          │
│   Crew Staging      │ ████         │              │                          │
│   Site Prep         │     ██████   │              │                          │
│   Safety Brief      │         ██   │              │                          │
│   ◆ Mob Complete    │          ◆   │              │                          │
├─────────────────────┼───────────────────────────────────────────────────────┤
│ ▼ Main Execution    │              │ ██████████████████   │                  │
│   Pipe Fit          │              │ ████████             │                  │  
│   Weld              │              │     ── ─ ─ ─ ─      │  (behind, dashed)│
│   ◆ 50% Milestone   │              │              ◆        │                  │
├─────────────────────┼───────────────────────────────────────────────────────┤
│ ▼ Demobilization    │              │              │ ██████  │                │
│   Closeout          │              │              │ ████    │                │
└─────────────────────┴───────────────────────────────────────────────────────┘
                                              ▲ Today (red line)
```

**SVG Rendering Rules:**
- Phase rows: colored band (Phase.Color) spanning full date range; bold text; collapse/expand chevron
- Task bars: solid rectangle in phase color (lighter); height: 20px; rounded corners
- Actual overlay: darker shade rendered over planned bar up to ActualStart to Today
- Forecast extension: dashed/striped bar from today to ForecastEnd when task is behind
- Baseline ghost: gray transparent bar behind current bar (shown when "Show Baseline" checked)
- Milestone diamonds: ◆ at PlannedDate; red fill if IsDeadline; gray outline if future; green check if Achieved
- Dependency arrows: horizontal line from predecessor bar right edge → successor bar left edge; 90° elbow
- Today line: vertical dashed red line at current date
- Critical path: red stroke outline on critical task bars
- Scroll: SVG right panel scrolls horizontally; left panel is fixed width; both scroll vertically in sync

**Zoom Levels:**
- Month: column = 1 month, label = "April 2026"
- Week: column = 1 week, label = "Apr 1-7"
- Day: column = 1 day, label = "Apr 1"

**Interactions:**
- Click task bar → emit `task-click` → open PlanTaskView in side panel or navigate
- Click phase row → expand/collapse children
- Click milestone ◆ → open Milestone edit dialog
- Click dependency arrow → show dependency details
- Hover any bar → tooltip: title, planned dates, % complete, variance days

**Component:** `webapp/src/modules/planning/components/GanttChart.vue`  
**Technology:** SVG, no third-party library — built in-house

---

### ProjectCalendarView (`/planning/projects/:id/calendar`)

**Purpose:** Calendar month view showing task bars and milestone flags. Extends `useCalendarGrid` composable extracted from EstimateCalendarView.

**Layout:**
```
┌───────────────────────────────────────────────────────────────────┐
│ [← Apr 2026]  April 2026  [May 2026 →]   [Today]   Filters: [▼]  │
├──────┬──────┬──────┬──────┬──────┬──────┬──────┐                  │
│  Sun │  Mon │  Tue │  Wed │  Thu │  Fri │  Sat │                  │
├──────┼──────┼──────┼──────┼──────┼──────┼──────┤                  │
│      │   1  │   2  │   3  │   4  │   5  │   6  │                  │
│      │ ──────────── Crew Staging ─────────────  │                  │
│      │ ◆ Kick-off                              │                  │
├──────┼──────┼──────┼──────┼──────┼──────┼──────┤                  │
│   7  │   8  │   9  │  10  │  11  │  12  │  13  │                  │
│  ─── Crew Staging ───────────────────────────  │                  │
│  ──────────────── Site Prep ──────────────────  │                  │
├──────┴──────┴──────┴──────┴──────┴──────┴──────┘                  │
```

**Event Rendering:**
- Task bars: multi-day colored bars (same lane algorithm as EstimateCalendarView)
- Milestone flags: single-day ◆ icon; red if IsDeadline; green if Achieved
- Overflow: "+N more" pill on days with > 3 events; click opens overflow dialog
- Colors: phase color for task bars; status-based for milestones

**Filters:** Phase, Craft, Status, Owner  
**Click task bar:** Navigate to PlanTaskView  
**Click milestone:** Open Milestone edit dialog  
**Data source:** `GET /api/v1/plan-tasks?planId=X` + `GET /api/v1/milestones?planId=X`  
**Composable:** `webapp/src/shared/composables/useCalendarGrid.ts`

---

### ProjectPhaseView (`/planning/projects/:id/phases/:phaseId`)

**Purpose:** Drill-down into a single phase showing its task list with inline subtask expansion.

**Layout:**
```
┌─────────────────────────────────────────────────────────────────────┐
│ ← Bayou Refinery Turnaround > Main Execution Phase                 │
│ Apr 15 – Jun 15, 2026  |  9 tasks  |  ⬤ At Risk  |  +3 days       │
├─────────┬───────────────┬────────┬────────┬────────┬───────────────┤
│ [+Task] │ Title         │ Start  │ End    │  %     │ Health        │
├─────────┼───────────────┼────────┼────────┼────────┼───────────────┤
│ ▼       │ Pipe Fit      │ Apr 15 │ May 15 │  80%   │ ⬤ On Track   │
│         │   Measure     │ Apr 15 │ Apr 22 │ 100%   │ ✓ Complete    │
│         │   Cut         │ Apr 22 │ Apr 30 │  90%   │ ⬤ On Track   │
│         │   Fit         │ Apr 30 │ May 15 │  60%   │ ⬤ At Risk    │
│ ►       │ Weld          │ May 15 │ Jun 10 │   0%   │ ⬤ Overdue    │
│ ►       │ Inspection    │ Jun 10 │ Jun 15 │   0%   │ ⬤ Not Started│
└─────────┴───────────────┴────────┴────────┴────────┴───────────────┘
```

**Features:**
- Collapse/expand subtasks (ParentTaskId children)
- Inline % complete update (click → slider dialog)
- Status pills with health colors
- Dependency indicators (arrow icon if task has predecessors)
- Row actions: Edit (→ PlanTaskView), Add Subtask, Delete, Mark Complete
- Add task button → PlanTaskView create mode with phaseId preset

**Data source:** `GET /api/v1/plan-tasks?phaseId=X`

---

### PlanTaskView (`/planning/projects/:id/phases/:phaseId/tasks/:taskId`)

**Purpose:** Full task detail and editing. Central PM form for recording planned, actual, and forecast dates, managing links to estimates and FCOs, and logging progress.

**Layout:**
```
┌─────────────────────────────────────────────────────────────────────┐
│ ← Main Execution > Weld                                [Save] [✖]  │
├─────────────────────────────────┬───────────────────────────────────┤
│ LEFT PANEL: Task Detail         │ RIGHT PANEL: Tabs                 │
│                                 │                                   │
│ Title: [Weld]                   │ [Links] [Deps] [Progress] [Actuals│
│ Type:  Task ▼                   │                                   │
│ Status: InProgress ▼            │ LINKS TAB:                        │
│ Phase: Main Execution           │ ┌─ Estimate Links ──────────────┐ │
│ Parent: —                       │ │ EST-2026-014 Bayou Refin. [X]│ │
│                                 │ │ [+ Add Estimate Link]         │ │
│ ── Timeline ──────────────────  │ └───────────────────────────────┘ │
│ Planned Start: [May 15, 2026]   │ ┌─ FCO Links ───────────────────┐ │
│ Planned End:   [Jun 10, 2026]   │ │ FCO-001 Scope Change    [X]  │ │
│ Duration:      26 days          │ │ [+ Add FCO Link]              │ │
│                                 │ └───────────────────────────────┘ │
│ Baseline Start: May 15, 2026    │                                   │
│ Baseline End:   Jun 5, 2026     │ ┌─ Step-Out Plan ───────────────┐ │
│ Baseline Duration: 21 days      │ │ [Link Step-Out Plan]          │ │
│                                 │ │ → /planning/step-out-plans/12 │ │
│ Actual Start:  [—]              │ └───────────────────────────────┘ │
│ Actual End:    [—]              │                                   │
│ Forecast End:  Jun 15 (+5 days) │ DEPS TAB:                        │
│                                 │ Predecessors:                     │
│ % Complete: ████████░░ 80%      │   Pipe Fit (FinishToStart, 0d)   │
│ [Update Progress]               │ Successors:                       │
│                                 │   Inspection (FinishToStart, 0d) │
│ ── Assignment ─────────────────  │ [+ Add Dependency]               │
│ Craft: Pipefitter               │                                   │
│ Assigned To: [—]                │ PROGRESS TAB:                     │
│ Owner: J. Hilliard              │ Date       | %   | Status         │
│                                 │ 2026-05-01 | 25% | InProgress     │
│ ── Health ─────────────────────  │ 2026-05-08 | 50% | InProgress     │
│ Status: ⬤ At Risk               │ 2026-05-15 | 80% | InProgress     │
│ Finish Variance: +5 days        │ [Record Snapshot]                 │
│ Schedule Var: -5 days           │                                   │
│ Baseline Finish Var: +10 days   │ ACTUALS TAB:                     │
│                                 │ Date       | Hrs | Cost           │
│                                 │ 2026-05-10 | 10h | $1,200        │
│                                 │ [+ Log Actuals]                   │
└─────────────────────────────────┴───────────────────────────────────┘
```

**Left Panel Fields:**
- Title, TaskType (Task|Milestone|Gate), Status (dropdown with transition guard)
- Phase (display only), Parent task (display with link)
- Planned Start/End, Duration (auto-calculated)
- Baseline Start/End, Duration (display only — locked)
- Actual Start/End (date inputs)
- Forecast End (calculated display + manual override option)
- % Complete slider (0–100)
- Craft, Assigned To, Owner
- Health summary: status pill, finish variance, schedule variance, baseline variance

**Right Panel Tabs:**

**Links Tab:**
- Estimate Links table: EstimateNumber | Name | LinkType | Notes | [Remove]
- [+ Add Estimate Link] → modal search estimates by number/name
- FCO Links table: FcoNumber | Title | LinkType | Notes | [Remove]
- [+ Add FCO Link] → modal search FCO documents
- Step-Out Plan: display current link + [Link] / [Unlink] + [Open →]

**Dependencies Tab:**
- Predecessors table: Task Title | Dependency Type | Lag Days | [Remove]
- Successors table: Task Title | Dependency Type | Lag Days | [Remove]
- [+ Add Dependency] → modal: search tasks in same plan → select type + lag

**Progress Tab:**
- History table of TaskProgressSnapshots: Date | % | Forecast End | Status | Reporter | Notes
- [Record Snapshot] button → inline form: %, Forecast End (optional), notes
- Mini trend line showing % over time (optional post-MVP)

**Actuals Tab:**
- Table of ActualEntries: Date | Craft | Hours | Cost | Billable | Entered By | Notes | [Delete]
- [+ Log Actuals] button → inline form: Date, Craft, Hours, Cost, Notes

**Status Transition Guard:**
- Changing Status to InProgress → check EstimateTaskLink exists → if not, show warning modal: "This task has no estimate link. Add one before activating."
- Admin can bypass with a confirmation flag

**Data sources:**
- `GET /api/v1/plan-tasks/:id` — full task detail
- `PATCH /api/v1/plan-tasks/:id/status` — status change
- `PATCH /api/v1/plan-tasks/:id/progress` — % and forecast
- `POST /api/v1/plan-tasks/:id/links/estimates` — add estimate link
- `POST /api/v1/plan-tasks/:id/links/fcos` — add FCO link
- `POST /api/v1/plan-tasks/:id/snapshots` — record progress snapshot
- `POST /api/v1/plan-tasks/:id/actuals` — log actual hours/cost

---

### MilestoneView (`/planning/milestones`)

**Purpose:** Company-wide or project-filtered list of milestones with slippage detection.

**Layout:**
```
┌──────────────────────────────────────────────────────────────────────┐
│ Filters: [Project ▼] [Phase ▼] [Status ▼] [☐ Deadlines Only]        │
├────────────────────┬───────────┬───────────┬────────┬────────┬───────┤
│ Milestone Name     │ Project   │ Phase     │Planned │ Actual │Status │
├────────────────────┼───────────┼───────────┼────────┼────────┼───────┤
│ ◆ Mob Complete     │ Bayou…    │ Mob.      │ Apr 15 │ Apr 14 │✓ Done │
│ ◆ 50% Milestone    │ Bayou…    │ Main Ex.  │ May 20 │ —      │⚠ AtRisk│
│ ◆🔴 Client FAC    │ Bayou…    │ Main Ex.  │ Jun 30 │ —      │🔴 Slip│
│ ◆ Kick-off         │ Gulf…     │ Phase 1   │ Jun 15 │ —      │● Pend │
└────────────────────┴───────────┴───────────┴────────┴────────┴───────┘
[+ New Milestone]
```

**Status indicators:**
- ✓ Done (green) — ActualDate set
- ⚠ At Risk (yellow) — PlannedDate within 7 days, not achieved
- 🔴 Slipped (red) — PlannedDate passed, not achieved
- ● Pending (gray) — future
- ✖ Cancelled (muted)

**IsDeadline column:** 🔴 badge when true  
**Row actions:** Edit, Achieve (set ActualDate = today), Mark Missed, Delete  
**Data source:** `GET /api/v1/milestones?companyCode=X&planId=Y`

---

### ScheduleHealthView (`/planning/health`)

**Purpose:** Company-wide schedule health dashboard. Shows ahead/behind status for every active project at a glance.

**Layout:**
```
┌───────────────────────────────────────────────────────────────────────┐
│ Schedule Health — All Projects                             [Refresh]  │
│                                                                        │
│ Summary: 1 On Track  |  1 At Risk  |  0 Behind  |  0 Blocked          │
│                                                                        │
├───────────────┬────────┬─────┬──────────┬──────────┬─────────────────┤
│ Project       │ Status │  %  │ Variance │Milest.   │ Health          │
├───────────────┼────────┼─────┼──────────┼──────────┼─────────────────┤
│ Bayou…        │ Active │ 72% │ +5 days  │ 1 at risk│ ⬤ At Risk      │
│ Gulf…         │ Active │ 10% │ 0 days   │ none     │ ⬤ On Track     │
└───────────────┴────────┴─────┴──────────┴──────────┴─────────────────┘

[By Phase View] [By Task View] [Critical Tasks Only]

Critical Tasks (across all projects):
┌──────────────────────────┬───────────┬──────────┬──────────┬─────────┐
│ Task                     │ Project   │ % Done   │ Variance │ Status  │
├──────────────────────────┼───────────┼──────────┼──────────┼─────────┤
│ Weld                     │ Bayou…    │   80%    │ +5 days  │ At Risk │
└──────────────────────────┴───────────┴──────────┴──────────┴─────────┘
```

**Health pill logic:** On Track (green) | At Risk (yellow) | Behind (red) | Blocked (orange) | Complete (muted)  
**Variance:** displayed as "+N days behind" or "-N days ahead"  
**Drill:** click project row → ProjectDetailView; click task → PlanTaskView  
**Data source:** `GET /api/v1/schedule-health/projects`

---

### TraceabilityView (`/planning/traceability`)

**Purpose:** Estimate → FCO → Task traceability matrix. Shows the full chain for any estimate or project. Highlights broken links and delayed work.

**Layout:**
```
┌───────────────────────────────────────────────────────────────────────┐
│ Traceability Matrix                  [Filter by Estimate ▼] [▼ More]  │
│                                                                        │
│ ⚠ 1 Broken Link Detected                         [View Broken Links]  │
│                                                                        │
├─────────────────────┬─────────────────────┬───────────────┬───────────┤
│ Estimate            │ FCO                 │ Task          │ Health    │
├─────────────────────┼─────────────────────┼───────────────┼───────────┤
│ EST-2026-014        │ —                   │ Crew Staging  │ ✓ Done    │
│ Bayou Refinery Q1   │ —                   │ Site Prep     │ ✓ Done    │
│                     │ FCO-001             │ Weld          │ ⬤ AtRisk  │
│                     │ Scope Change        │               │           │
├─────────────────────┼─────────────────────┼───────────────┼───────────┤
│ EST-2026-022        │ —                   │ Phase 1 Kick  │ ● Pending │
│ Gulf Coast Tie-In   │ —                   │ ROW Survey    │ ● Pending │
└─────────────────────┴─────────────────────┴───────────────┴───────────┘

[Delayed Work by Estimate]
┌─────────────────────┬───────────────────────────────┬───────────────┐
│ Estimate            │ Delayed Task                  │ Variance      │
├─────────────────────┼───────────────────────────────┼───────────────┤
│ EST-2026-014        │ Weld (Main Execution Phase)   │ +5 days       │
└─────────────────────┴───────────────────────────────┴───────────────┘

[Broken Links]
┌───────────────────────────────────────────────────────┬─────────────┐
│ Task                                                  │ Problem     │
├───────────────────────────────────────────────────────┼─────────────┤
│ (no broken links in demo data)                        │             │
└───────────────────────────────────────────────────────┴─────────────┘
```

**Sections:**
1. Traceability matrix: Estimate → FCO → Task → Health (grouped by estimate)
2. Delayed Work by Estimate: tasks with FinishVariance > 0, grouped by estimate
3. Broken Links: tasks with archived/cancelled estimate links

**Data sources:**
- `GET /api/v1/traceability/project/:planId/chain`
- `GET /api/v1/traceability/broken-links?companyCode=X`
- `GET /api/v1/traceability/estimates/:id/delayed`

---

## COMPONENT MAP

### New Components

| Component | File | Purpose |
|-----------|------|---------|
| GanttChart | `webapp/src/modules/planning/components/GanttChart.vue` | Custom SVG Gantt renderer |
| GanttTaskRow | `webapp/src/modules/planning/components/GanttTaskRow.vue` | Single task row in Gantt |
| GanttPhaseRow | `webapp/src/modules/planning/components/GanttPhaseRow.vue` | Phase group header row in Gantt |
| GanttDateRuler | `webapp/src/modules/planning/components/GanttDateRuler.vue` | Date column header with zoom |
| GanttBar | `webapp/src/modules/planning/components/GanttBar.vue` | SVG bar element (planned + actual + forecast) |
| GanttMilestoneDiamond | `webapp/src/modules/planning/components/GanttMilestoneDiamond.vue` | ◆ milestone marker |
| GanttDependencyArrow | `webapp/src/modules/planning/components/GanttDependencyArrow.vue` | SVG dependency line/arrow |
| TaskHealthPill | `webapp/src/modules/planning/components/TaskHealthPill.vue` | Colored status pill (On Track / At Risk / Behind / Blocked) |
| MilestoneStatusIcon | `webapp/src/modules/planning/components/MilestoneStatusIcon.vue` | ◆ with status coloring |
| EstimateLinkSelector | `webapp/src/modules/planning/components/EstimateLinkSelector.vue` | Search + add estimate link modal |
| FcoLinkSelector | `webapp/src/modules/planning/components/FcoLinkSelector.vue` | Search + add FCO link modal |
| DependencyPicker | `webapp/src/modules/planning/components/DependencyPicker.vue` | Add task dependency modal |
| ProgressSliderDialog | `webapp/src/modules/planning/components/ProgressSliderDialog.vue` | % complete update dialog |
| ActualsForm | `webapp/src/modules/planning/components/ActualsForm.vue` | Log actual hours/cost inline form |
| BaselineLockDialog | `webapp/src/modules/planning/components/BaselineLockDialog.vue` | Confirm + reason for baseline lock |

### Shared Composables

| Composable | File | Purpose |
|-----------|------|---------|
| useCalendarGrid | `webapp/src/shared/composables/useCalendarGrid.ts` | Month grid, lane algorithm, overflow — extracted from EstimateCalendarView |
| useGanttLayout | `webapp/src/modules/planning/composables/useGanttLayout.ts` | Date → pixel conversion, zoom, scroll sync |
| useScheduleHealth | `webapp/src/modules/planning/composables/useScheduleHealth.ts` | Client-side health calculations (FinishVariance, status) |

---

## DRILL-DOWN FLOW

### Full Navigation Path (Portal to Execution Detail)

```
Portal Dashboard
  [Planning tile]
       │
       ▼
  ProjectListView  (/planning/projects)
  [click project row / card]
       │
       ▼
  ProjectDetailView  (/planning/projects/:id)   [tab: Overview]
  [click Gantt tab]              [click phase row]
       │                              │
       ▼                              ▼
  ProjectGanttView            ProjectPhaseView  (/planning/projects/:id/phases/:phaseId)
  (/planning/projects/:id/gantt)  [click task row]
  [click task bar]                   │
       │                              ▼
       └──────────┬────────────► PlanTaskView
                  │              (/planning/projects/:id/phases/:phaseId/tasks/:taskId)
                  │              [click Step-Out Plan link]
                  │                   │
                  │                   ▼
                  │              StepOutPlanFormView  (/planning/step-out-plans/:id)
                  │              [existing view, enhanced with back-link to task]
                  │
                  ▼
             MilestoneView (from Milestones tab in ProjectDetailView)
             ScheduleHealthView (from /planning/health)
             TraceabilityView (from Traceability tab in ProjectDetailView)
```

### Back Navigation Rules
- All Planning sub-pages show breadcrumb: `← Project Name > Phase Name > Task Name`
- Back always navigates to the immediate parent context, not the browser history
- URL always reflects full hierarchy (`:id/phases/:phaseId/tasks/:taskId`) for bookmarkability

---

## GANTT INTERACTIONS

| Interaction | Response |
|-------------|---------|
| Click phase row header | Expand/collapse child task rows |
| Click task bar | Open task detail (right panel or navigate to PlanTaskView) |
| Hover task bar | Tooltip: Title, Planned Start/End, % Complete, FinishVariance |
| Click milestone ◆ | Open milestone edit dialog |
| Toggle "Show Baseline" | Toggle gray baseline ghost bars behind current bars |
| Change Zoom | Recalculate column width; redraw date ruler; bars re-render |
| Scroll horizontal | Right SVG panel scrolls; left label panel stays fixed |
| Scroll vertical | Both panels scroll in sync |
| Filter by Phase | Hide/show phase rows and their tasks |
| Filter by Status | Dim rows that don't match |
| Filter by Craft | Show only tasks where CraftCode matches |
| Click Dependency Arrow | Tooltip: predecessor task title, dependency type, lag days |

### Gantt SVG Architecture

```
GanttChart.vue
  ├── GanttDateRuler.vue (sticky column header SVG)
  │    └── [date labels at zoom-appropriate intervals]
  ├── Left Panel (div, fixed width, overflow hidden)
  │    └── GanttPhaseRow.vue (repeating)
  │         └── GanttTaskRow.vue (repeating, indented for subtasks)
  └── Right Panel (div, overflow-x scroll)
       └── SVG canvas (full width = date range × columnWidth)
            ├── Background weekend/today highlights
            ├── [Phase background bands]
            ├── GanttBar.vue (per task, absolute positioned)
            │    ├── [Planned bar rect]
            │    ├── [Actual overlay rect]
            │    └── [Forecast dashed extension rect]
            ├── GanttMilestoneDiamond.vue (per milestone)
            └── GanttDependencyArrow.vue (per dependency)
```

**SVG Coordinate System:**
- X axis: date → pixels using `(date - viewStart) / dayWidth * columnWidth`
- Y axis: row index × rowHeight (32px default)
- Scroll offset: tracked in `useGanttLayout` composable; applied as SVG viewBox transform

---

## CALENDAR INTERACTIONS

| Interaction | Response |
|-------------|---------|
| Click prev/next month | Navigate month; reload events for new range |
| Click Today | Jump to current month |
| Click task bar | Navigate to PlanTaskView |
| Click milestone ◆ | Open milestone edit dialog |
| Click "+N more" overflow pill | Open day overflow dialog listing all events |
| Filter by Phase | Hide tasks not in selected phase |
| Filter by Craft | Hide tasks not matching craft |

---

## TASK DETAIL INTERACTIONS (PlanTaskView)

| Interaction | Response |
|-------------|---------|
| Change Status dropdown | Validate transition (InProgress requires estimate link); save |
| Update % Complete slider | PATCH /progress; recalculate ForecastEnd |
| Override Forecast End | Save manual override; shown with indicator "(manual)" |
| + Add Estimate Link | Open EstimateLinkSelector modal; POST link; refresh Links tab |
| Remove Estimate Link | Confirm dialog; DELETE link; refresh |
| + Add FCO Link | Open FcoLinkSelector modal; POST link; refresh |
| + Add Dependency | Open DependencyPicker modal; validate no cycle; POST; refresh Deps tab |
| Record Snapshot | Inline form → POST snapshot; refresh Progress tab |
| Log Actuals | Inline form → POST actual; refresh Actuals tab |
| Link Step-Out Plan | Search step-out plans → select → PATCH task.LinkedStepOutPlanId |
| Open Step-Out Plan | Navigate to /planning/step-out-plans/:id |

---

## MILESTONE INTERACTIONS

| Interaction | Response |
|-------------|---------|
| Click Achieve | Set ActualDate = today, Status = Achieved; confirm dialog |
| Click Mark Missed | Set Status = Missed; confirm dialog |
| Edit Planned Date | Update PlannedDate; recalculate slippage |
| Edit milestone in Gantt | Opens milestone edit dialog overlay |
| Filter by Deadline | Show only IsDeadline = true milestones |

---

## STEP-OUT PLAN INTERACTIONS (Existing — Enhanced)

The existing `StepOutPlanFormView.vue` receives one enhancement: a back-link to the parent PlanTask.

| Addition | Detail |
|----------|--------|
| Breadcrumb: `← Task: Weld > Step-Out Plan` | If LinkedPlanTaskId is set, show task name as back-link |
| Link/Unlink Task | Allow linking/unlinking the step-out plan to a PlanTask from within the form |

No other changes to existing StepOutPlanFormView behavior.

---

## PORTAL DASHBOARD UPDATES

**File:** `webapp/src/modules/portal/views/PortalDashboardView.vue`

The existing Planning tile shows "Pending FCO count." This is updated to show planning health:

```
Planning Tile:
  [→ Planning]
  2 Active Projects
  ⚠ 1 At Risk
```

The KPI strip gains two new items:
- **Projects At Risk** — count of Projects with health = AtRisk or Behind
- **Milestones Slipping** — count of milestones where PlannedDate < Today + 7 and not achieved

**Data source:** Updated `GET /api/v1/portal/dashboard` response

---

---

## SCHEDULING UI MAP

**Scope boundary:** Scheduling UI is personnel assignment only. No Gantt, no project phases, no PM calendar. Those live in Planning.

---

### Scheduling Routes

```
/scheduling                          → redirect → /scheduling/dashboard
/scheduling/dashboard                → SchedulingDashboardView  (EXISTING)
/scheduling/jobs                     → JobsBoardView            (EXISTING — behavior updated)
/scheduling/resources                → ResourcesBoardView       (EXISTING — fields expanded)
/scheduling/resources/new            → ResourceFormView         (BUILD NEW)
/scheduling/resources/:id            → ResourceFormView         (BUILD NEW)
/scheduling/resources/import         → ResourceImportView       (BUILD NEW)
/scheduling/assignments              → AssignmentsView          (EXISTING)
/scheduling/coverage                 → CoverageView             (EXISTING)
/scheduling/roll-off                 → RollOffView              (EXISTING)
```

---

### Jobs Board — Forecast vs Released Demand (LOCKED)

The jobs board shows **all demand** so schedulers can plan ahead — but only **Released demand** is assignable. Forecast demand rows are visible and informational only.

**Row states:**

| Badge | Color | Assign Button | Sources |
|-------|-------|---------------|---------|
| `Forecast` | Gray | Disabled | Awarded Estimates; approved unconverted StaffingPlans |
| `Released` | Green | Active | WorkPackages where ReadyForScheduling=true AND WorkOrder.Status IN (Released, InProgress) |

**Filter bar:** Jobs board includes a demand state filter: [All ▼ | Forecast Only | Released Only]. Default: All.

**Backend enforcement:** The Assignment create endpoint validates that the DemandSourceType/DemandSourceId resolves to a Released demand record. Attempting to POST an assignment against a Forecast demand record returns HTTP 422 with reason `DemandNotReleased`.

---

### Jobs Board — Demand Summary Drawer (LOCKED)

**Trigger:** Double-click any row on `/scheduling/jobs`.

**Behavior:** Opens a right-side drawer (not a navigation, not an edit form). The drawer is read-only.

**Drawer layout:**
```
┌───────────────────────────────────────────────────────────┐
│  [Source: Estimate]  EST-2026-014                    [✕]  │
│  Bayou Refinery Turnaround — Main Execution               │
├───────────────────────────────────────────────────────────┤
│  Project:       PRJ-2026-001                              │
│  Client:        Motiva Refinery                           │
│  Site:          Bayou, LA                                 │
│  Status:        Active                    [Tag]           │
│  Planned Start: Apr 15, 2026                              │
│  Planned End:   Jun 15, 2026                              │
├───────────────────────────────────────────────────────────┤
│  Authorization                  [Forecast] or [Released]  │
│  WorkOrder:     WO-2026-001  [Released]                   │
│  CommAuth:      CA-2026-001  $1,250,000                   │
│  FCO Count:     2 open                                    │
│  Assignable:    ✓ Yes (Released WO backs this demand)     │
├───────────────────────────────────────────────────────────┤
│  Craft Requirements                                       │
│  ┌────────────────┬──────┬────────────┐                   │
│  │ Craft          │ Qty  │ Assigned   │                   │
│  ├────────────────┼──────┼────────────┤                   │
│  │ Pipefitter     │  12  │  8         │                   │
│  │ Welder         │   6  │  6 ✓       │                   │
│  │ Rigger         │   4  │  2         │                   │
│  └────────────────┴──────┴────────────┘                   │
├───────────────────────────────────────────────────────────┤
│                                    [Assign Resource]       │
└───────────────────────────────────────────────────────────┘
```

**Fields shown:**
- Source type badge (Estimate / StaffingPlan / WorkPackage)
- Reference number (Estimate number, SP number, or WP number)
- Job/plan name
- Project name (if backed by a WorkOrder → Project)
- Client, Site
- Status
- Planned Start / Planned End
- Authorization section: WorkOrder number + status, CommAuth number + authorized value, FCO count (open FCOs), Released for Execution (boolean — WorkOrder.Status IN ('Released', 'InProgress'))
- Craft requirements table: Craft | Required Qty | Currently Assigned count (from Assignments table)
- [Assign Resource] button → opens Assignment Modal

**No edit controls in this drawer.** To edit the source demand, user navigates to Estimating or Planning.

**[Assign Resource] button** is active only when demand state is **Released**. For Forecast demand rows the button reads `[Not Released — Assign Disabled]` and is non-interactive. Clicking a Forecast row's drawer shows the Assignable field as `✗ No — WorkOrder not released`.

---

### Assignment Modal — Redesign (LOCKED)

**Trigger:** [Assign Resource] button in demand summary drawer, or [Assign] button on jobs board row.

**Behavior:** Modal dialog — NOT a page navigation. Craft dropdown is required; free-text craft entry is prohibited.

**Modal layout:**
```
┌──────────────────────────────────────────────────────────────┐
│  Assign Resource                                        [✕]  │
├──────────────────────────────────────────────────────────────┤
│  JOB CONTEXT (read-only)                                     │
│  Job:    Bayou Refinery Turnaround                           │
│  Source: Estimate EST-2026-014                               │
│  Dates:  Apr 15 – Jun 15, 2026                               │
├──────────────────────────────────────────────────────────────┤
│  ASSIGNMENT DETAILS                                          │
│  Craft:  [Pipefitter ▼]              ← dropdown, required    │
│  Start:  [Apr 15, 2026 📅]                                   │
│  End:    [Jun 15, 2026 📅]                                   │
│  Shift:  [Day ▼]                                             │
│  Notes:  [                       ]                           │
├──────────────────────────────────────────────────────────────┤
│  AVAILABLE RESOURCES  (filtered by craft / certs / dates)   │
│  ┌──────────────────┬────────┬──────────┬──────────────────┐ │
│  │ Name             │ Region │ Certs    │ Conflicts        │ │
│  ├──────────────────┼────────┼──────────┼──────────────────┤ │
│  │ ● John Martinez  │ Gulf   │ NCCER,H2S│ None             │ │
│  │ ● Sarah Tran     │ Gulf   │ NCCER    │ None             │ │
│  │ ○ Mike Boudreaux │ TX     │ NCCER    │ Apr 20–30 (PTO)  │ │
│  └──────────────────┴────────┴──────────┴──────────────────┘ │
│  ● Available  ○ Partial conflict                             │
│  [Select]                                                    │
├──────────────────────────────────────────────────────────────┤
│                             [Cancel]  [Create Assignment]    │
└──────────────────────────────────────────────────────────────┘
```

**Behavior rules:**
- Job context section is populated from the demand row — fields are display-only.
- Craft dropdown is sourced from the Craft reference table (CraftCode + Name). Changing craft re-filters the resource list.
- Resource list filters: PrimaryCraft OR SecondaryCraft matches selected craft; no AvailabilityBlock overlapping date range; no existing Assignment overlapping date range. Region/Branch filter optional — shown as a secondary filter chip.
- Conflicts shown inline per resource row — partial conflicts shown with ○ indicator; full conflicts excluded from list.
- On [Create Assignment]: POST to `/api/v1/scheduling/assignments` with ResourceId, DemandSourceType, DemandSourceId, CraftId, StartDate, EndDate, ShiftType, Notes.
- Drawer refreshes Craft Requirements table after save.

---

### Resource Master — Form (ResourceFormView)

**Route:** `/scheduling/resources/new` and `/scheduling/resources/:id`

**Layout:**
```
┌───────────────────────────────────────────────────────────────┐
│  ← Resources                                    [Save] [✖]   │
│  John Martinez                                                │
├──────────────────────────────────┬────────────────────────────┤
│  IDENTITY                        │  SCHEDULING                │
│  Employee ID:  [EMP-1042      ]  │  Shift:    [Day ▼]         │
│  First Name:   [John          ]  │  Region:   [Gulf Coast ▼]  │
│  Last Name:    [Martinez      ]  │  Branch:   [Baton Rouge ▼] │
│  Status:       [Active ▼]        │                            │
│                                  │  CONTACT (optional)        │
│  CRAFT                           │  Phone:  [              ]  │
│  Primary:      [Pipefitter ▼]    │  Email:  [              ]  │
│  Secondary:    [+ Add Craft]     │                            │
│    Welder [✕]                    │  NOTES                     │
│                                  │  [                      ]  │
│  CERTIFICATIONS                  │                            │
│  ┌────────────────┬──────────┐   │                            │
│  │ Cert           │ Expires  │   │                            │
│  ├────────────────┼──────────┤   │                            │
│  │ NCCER Core     │ —        │   │                            │
│  │ H2S Alive      │ 2027-06  │   │                            │
│  └────────────────┴──────────┘   │                            │
│  [+ Add Certification]           │                            │
└──────────────────────────────────┴────────────────────────────┘
```

**Fields:**
- EmployeeId (text), FirstName, LastName (required)
- EmploymentStatus dropdown: Active | Inactive | OnLeave | Terminated
- PrimaryCraft dropdown (FK → Crafts)
- SecondaryCrafts: multi-select or add/remove chip list (junction ResourceCraft)
- Region dropdown (reference list), Branch/Yard/HomeLocation dropdown
- ShiftEligibility dropdown: Day | Night | Rotating | Any
- Certifications table: add/remove rows with CertId (dropdown) + ExpirationDate (optional)
- Phone, Email (optional)
- Notes

**Data sources:**
- `GET /api/v1/scheduling/resources/:id`
- `POST /api/v1/scheduling/resources`
- `PUT /api/v1/scheduling/resources/:id`
- `GET /api/v1/scheduling/crafts` (reference dropdown)
- `GET /api/v1/scheduling/certifications` (reference dropdown)

---

### Resource Import View (ResourceImportView)

**Route:** `/scheduling/resources/import`

**Purpose:** CSV/spreadsheet bulk upload for resources. Maps columns to Resource fields.

**Layout:**
```
┌──────────────────────────────────────────────────────────────┐
│  Import Resources from CSV                                   │
├──────────────────────────────────────────────────────────────┤
│  1. Download template:  [Download CSV Template]              │
│                                                              │
│  2. Upload file:        [Choose File...]   employees.csv     │
│                                                              │
│  3. Column Mapping (auto-detected, adjustable):              │
│  ┌────────────────────┬──────────────────────────────────┐   │
│  │ CSV Column         │ Maps To                          │   │
│  ├────────────────────┼──────────────────────────────────┤   │
│  │ Employee_ID        │ EmployeeId          ▼            │   │
│  │ First_Name         │ FirstName           ▼            │   │
│  │ Last_Name          │ LastName            ▼            │   │
│  │ Craft_Code         │ PrimaryCraft        ▼            │   │
│  │ Region             │ Region              ▼            │   │
│  │ Branch             │ Branch              ▼            │   │
│  └────────────────────┴──────────────────────────────────┘   │
│                                                              │
│  Preview: 42 rows detected. 0 errors.                        │
│  [Validate]   [Import 42 Resources]                          │
└──────────────────────────────────────────────────────────────┘
```

**Behavior:**
- Template download provides a CSV with the required column headers.
- Upload triggers column auto-detection. Unrecognized columns show a "Skip" option.
- Validate step calls `/api/v1/scheduling/resources/import/validate` — returns row-level errors (unknown CraftCode, missing required field, duplicate EmployeeId).
- Import calls `POST /api/v1/scheduling/resources/import` — upsert behavior: EmployeeId match → update; no match → create.
- Result: success count, skip count, error rows with reason.

**Note on AD sync:** Future integration with Active Directory or HR systems will feed data into this same endpoint. Scheduling module ownership does not change — AD sync is an import source, not a separate module.

---

## AI SIDEBAR CONTEXT (Planning Module)

The existing `AiChatSidebar.vue` detects route name and provides contextual AI prompts.

**Add to context detection in AppLayout.vue:**
```javascript
if (routeName.includes('project')) context = 'planning-project'
if (routeName.includes('gantt')) context = 'planning-gantt'
if (routeName.includes('task')) context = 'planning-task'
if (routeName.includes('health')) context = 'planning-health'
if (routeName.includes('traceability')) context = 'planning-traceability'
```

**AI context prompts (future implementation):**
- `planning-project`: "This project is X days behind. Which phases are driving the slippage?"
- `planning-task`: "This task has no estimate link — would you like me to find matching estimates?"
- `planning-health`: "Show me all projects with critical path tasks behind schedule"
- `planning-traceability`: "Which estimates have delayed tasks and what is the total variance?"
