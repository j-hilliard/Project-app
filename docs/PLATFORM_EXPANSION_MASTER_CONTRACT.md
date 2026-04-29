# Platform Expansion Master Contract

Last updated: 2026-04-28

This document is the stable Claude-facing implementation contract for evolving this repo into one portal with three business apps: Estimating, Planning / PM, and Scheduling.

Codex owns auditing, testing, screenshots, evidence packets, live TODO state, and regression verification. Claude may implement, but Claude does not self-certify completion unless Joseph explicitly waives a requirement or accepts behavior as-is.

## Repo Truth

Use the real repository layout:

- `Api/` for the ASP.NET API.
- `Data/` for `AppDbContext`, models, migrations, seed/bootstrap, and design-time factory.
- `Shared/` for shared constants/enums/attributes.
- `webapp/` for Vue 3 + TypeScript frontend.

Before editing, identify the actual locations of:

- `Api/Program.cs`
- `Data/AppDbContext.cs`
- `Data/Migrations/`
- `Data/DBInitializer.cs`
- `Data/DesignTimeContextFactory.cs`
- `webapp/src/apps.ts`
- `webapp/src/router/index.ts`
- `webapp/src/modules/estimating/`
- test, Playwright, build, lint, and NSwag/generation setup

If a prompt path conflicts with the real repo, use the real path and document the correction.

## Role Split

Claude implementation responsibilities:

- Make safe, incremental code changes when explicitly assigned implementation.
- Keep architecture/worklog/test docs current.
- Add or update tests where practical.
- Write `Ready for Codex verification` when a task appears complete.

Codex QA/orchestration responsibilities:

- Audit and test completed work.
- Capture screenshots under `docs/qa-evidence/<sweep-id>/`.
- Write Claude-readable evidence handoffs.
- Update `docs/LIVE_QA_TODO.md` and regression checklists.
- Reopen checked items if verification fails or if Claude self-closed without independent evidence.

Only Joseph or Codex checks an item complete. Claude may close only when explicitly documenting `Joseph waived this requirement` or `Joseph accepted this as-is`, including date/rationale/evidence.

## App Boundaries

Estimating owns:

- Estimates, staffing plans, rate books, cost books, proposal/bid workflows, forecast inputs, commercial estimate truth, and commercial baseline values.

Planning / PM owns:

- CommercialAuthorizations, Projects, WorkOrders (first-class PM entity — the operational authorization unit that gates step-out plans, FCOs, and actuals), step-out plans, work breakdown, execution sequencing, permit/tool/material staging, work packages, FCO/change planning, operational actuals, and estimate/FCO execution variance.
- Gantt charts and PM calendars live here and ONLY here. Scheduling has no Gantt, no PM timeline.

Scheduling owns:

- Resources/people, crafts, skills/certifications, availability, PTO/blackout, assignments, conflicts, coverage gaps, roll-off, ending-soon, and available-soon.
- Scheduling is personnel assignment only — who goes where, when, and with what craft/cert. Scheduling does not manage project timelines, WBS, or phase structures.

Portal owns:

- Landing dashboard, app launcher, summary widgets, cross-app navigation, and alert surface. Portal consumes read models and must stay thin.

Do not put actual people assignment logic into estimating rows. Do not duplicate bid pricing logic in scheduling. Do not turn planning into generic notes.

## Required Data Flow

The platform flow is:

Estimating -> Planning -> Scheduling -> Actuals/Variance -> Portal/Forecast

Scheduling demand has two states (see Scheduling Requirements section for full model):

- **Forecast demand** (visible, not assignable): awarded Estimates; approved unconverted StaffingPlans.
- **Released demand** (visible, assignable): WorkPackages with `ReadyForScheduling = true` backed by a released WorkOrder.

Hard dedupe rule: If `StaffingPlan.ConvertedEstimateId != null`, that staffing plan must not produce independent demand. The linked estimate is the source of truth.

Planning output:

- Step-out plans create ordered/parallel work steps.
- Work packages become schedulable operational demand.
- Actuals captured against work packages/steps feed estimate/FCO variance reports.

## Step-Out Planning Requirements

Step-out plans must be first-class operational records, not notes.

Step numbering must support insertable display values such as:

- `1`
- `1.2`
- `1.5`
- `1.7`
- `2`

Use a display value plus sortable key approach where needed, for example `StepCode` and `SortOrder`.

Each plan needs source links to estimate, staffing plan, FCO/change, job, work package, or ad hoc work. Each step needs title, description, craft/headcount, duration, planned dates, dependencies, parallel flag/group, status, permit/material/tool fields, area/location, and notes.

## Scheduling Requirements

Scheduling is **personnel assignment only**. It answers: "Who do we have, what craft are they, and where do they need to go?" It does not own project timelines, WBS, or PM planning structures — those live in Planning.

### Demand State Model (LOCKED)

Scheduling demand has two mutually exclusive states. Both are visible on the jobs board. Only Released demand is assignable.

**Forecast Demand** — visible, not assignable:
- Source: awarded Estimates not yet backed by a released WorkOrder; approved unconverted StaffingPlans.
- Jobs board displays `Forecast` badge. Assign button is disabled.
- Purpose: lets schedulers see upcoming work and begin workforce planning before formal release.

**Released / Assignable Demand** — visible and assignable:
- Source: WorkPackages with `ReadyForScheduling = true` where `WorkOrder.Status IN ('Released', 'InProgress')`.
- Jobs board displays `Released` badge. Assign button is active.
- Assignment creation is blocked server-side against Forecast demand — enforced in AssignmentController, not just the UI.

Hard rule: users must not assign people to work that has not been formally released through a WorkOrder.

### Scheduling Module Ownership

- **Resource Master** belongs to Scheduling. Identity, craft, region, availability, certifications, import, future AD sync.
- **Craft reference table** belongs to Scheduling. Admin-managed; required for all assignments.
- **Certification reference table** belongs to Scheduling. Admin-managed; includes expiration tracking.
- **Assignments** belong to Scheduling. Every assignment requires a CraftId from the Craft reference table — no free-text craft on any assignment record.

### Detailed Scheduling Specs

UI behavior, data model fields, demand summary drawer, assignment modal, Resource Master form, CSV import, and TODO task list are in:
- `docs/PROJECT_SCHEDULING_PERSONNEL_SPEC.md` — spec and behavior
- `docs/PROJECT_SCHEDULING_PERSONNEL_TODO.md` — implementation tasks

## Actuals, Delta, And FCO Requirements

Planning/Scheduling actuals must support variance reporting against estimate/FCO baselines:

- Actual labor hours.
- Actual labor cost.
- Actual billable labor value.
- Actual material/equipment cost where feasible.
- Planned vs actual charge, cost, margin, delta amount, and delta percent.

Rate Book = charge/bill side.

Cost Book = internal cost side.

Do not duplicate rate/cost math in random Vue views. Prefer backend services/read models.

FCO/change-order support must include:

- Estimate linkage.
- Changed scope.
- Reason/basis.
- Schedule impact.
- Labor/material/equipment breakdown.
- Updated value.
- Status/revision history where feasible.
- Signable/printable document output with approval/signature fields.

## EF / Bootstrap Rules

- Never use `EnsureCreated` for the real lifecycle.
- Migrations are the source of truth.
- Refactor toward `DatabaseBootstrapper`, `ReferenceDataSeeder`, and `DemoDataSeeder`.
- Separate reference data from demo/sample data.
- Use idempotent seed logic.
- Demo seed must be configurable and off by default for higher environments.
- Preserve/update `Data/DesignTimeContextFactory.cs`.
- Do not move seed logic into controllers.

## Required Docs For Implementation Work

Claude implementation work must maintain:

- `docs/PORTAL_PLANNING_SCHEDULING_ARCHITECTURE.md`
- `docs/DOMAIN_RESEARCH_NOTES.md`
- `docs/LIVE_TODO.md`
- `docs/IMPLEMENTATION_WORKLOG.md`
- `docs/REGRESSION_CHECKLIST.md`
- `docs/TEST_RUN_LOG.md`
- `docs/PROJECT_SCHEDULING_PERSONNEL_SPEC.md` (scheduling spec — behavior and data model)
- `docs/PROJECT_SCHEDULING_PERSONNEL_TODO.md` (scheduling implementation tasks)

Codex QA work must maintain:

- `docs/LIVE_QA_TODO.md`
- `docs/QA_REGRESSION_CHECKLIST.md`
- `docs/SUBAGENT_DEMO_AUDIT_BRIEF.md`
- `docs/qa-evidence/<sweep-id>/`
- Claude-readable evidence handoff files under `docs/`

## Phase Gates

Phase 1: Portal shell and app registration.

- `/` loads portal.
- Estimating tile works.
- Planning tile/route works.
- Scheduling tile/route works.
- Existing `/estimating` routes still work.

Phase 2: EF bootstrap cleanup.

- Startup uses migrations.
- Reference/demo seed separated.
- Seed is idempotent.
- Config flags documented.

Phase 3: Planning foundation.

- Step-out plans can be created/edited.
- Step values like `1`, `1.2`, `1.5` persist.
- Dependencies/parallel flags persist.
- Work package generation works.

Phase 4: Scheduling foundation.

- Demand includes estimates and approved unconverted staffing plans.
- Converted staffing plans are deduped.
- Resources/assignments load.
- Shortages, ending-soon, and available-soon are visible.

Phase 5: Actuals, delta, and FCO.

- Actuals can be recorded.
- Estimate/FCO deltas show hours, charge, cost, margin, amount, and percent.
- Signable FCO document can be generated.

## Verification Standard

After every implementation chunk, Codex or an assigned QA agent must verify:

- Files changed match the phase.
- Existing estimating demo still loads.
- Boundaries are preserved.
- Tests/builds ran or failure is documented.
- Screenshots/evidence exist for UI changes.
- Live TODO and regression checklist were updated.

No unchecked evidence, no closure.
