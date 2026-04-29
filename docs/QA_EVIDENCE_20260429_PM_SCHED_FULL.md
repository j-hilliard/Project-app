# QA Evidence - PM + Scheduling Full Sweep - 2026-04-29

Sweep ID: `QA_AUDIT_20260429_PM_SCHED_FULL`
Repo: `Project-app`
Branch/commit at audit start: `feat/pm-module` / `e329003`
Watch mode: manual polling mode. This environment does not provide a persistent background watcher.

## Important Port Correction

Do not use the earlier `7210/7211` Playwright smoke as Project-app evidence. Port inspection on 2026-04-29 showed:

- `7310` = Project-app Vue dev server
- `7311` = Project-app API
- `7210` = old `stronghold-enterprise-estimating` Vue dev server
- `7211` = old `stronghold-enterprise-estimating` API

The valid Project-app evidence for this sweep is under:

- `docs/qa-evidence/QA_AUDIT_20260429_PM_SCHED_FULL/`

## Commands / Tools Used

| Command / Tool | Result | Notes |
|---|---|---|
| Canonical docs + QA docs read | PASS | `PROJECT_HANDOFF_INDEX`, platform contract, lifecycle/ownership/status/actuals/UI/TODO/scheduling specs, live TODO, regression checklist, test log, worklog. |
| Port/process inspection | PASS | Confirmed Project-app owns `7310/7311`; old repo owns `7210/7211`. |
| QA-only Playwright route audit script | PASS WITH FINDINGS | Ran against `https://localhost:7310`; 15 PM/Scheduling routes/screens; 14 findings. |
| UI Explorer subagent | PASS WITH FINDINGS | Read-only route/screen/density/drilldown inventory. |
| Logic Auditor subagent | PASS WITH FINDINGS | Read-only backend/business-rule audit. |
| Regression Guard subagent | PASS WITH FINDINGS | Read-only test coverage and safe-command audit. |

## Route / Screen Matrix

See generated matrix:

- `docs/qa-evidence/QA_AUDIT_20260429_PM_SCHED_FULL/pm-scheduling-audit-results.md`
- `docs/qa-evidence/QA_AUDIT_20260429_PM_SCHED_FULL/pm-scheduling-audit-results.json`

## Screenshot Packet

Key screenshots:

- `docs/qa-evidence/QA_AUDIT_20260429_PM_SCHED_FULL/portal.png`
- `docs/qa-evidence/QA_AUDIT_20260429_PM_SCHED_FULL/planning-projects.png`
- `docs/qa-evidence/QA_AUDIT_20260429_PM_SCHED_FULL/planning-work-orders.png`
- `docs/qa-evidence/QA_AUDIT_20260429_PM_SCHED_FULL/planning-step-out-plans.png`
- `docs/qa-evidence/QA_AUDIT_20260429_PM_SCHED_FULL/planning-work-packages.png`
- `docs/qa-evidence/QA_AUDIT_20260429_PM_SCHED_FULL/planning-fco.png`
- `docs/qa-evidence/QA_AUDIT_20260429_PM_SCHED_FULL/scheduling-dashboard.png`
- `docs/qa-evidence/QA_AUDIT_20260429_PM_SCHED_FULL/scheduling-jobs.png`
- `docs/qa-evidence/QA_AUDIT_20260429_PM_SCHED_FULL/scheduling-resources.png`
- `docs/qa-evidence/QA_AUDIT_20260429_PM_SCHED_FULL/scheduling-assignments.png`
- `docs/qa-evidence/QA_AUDIT_20260429_PM_SCHED_FULL/scheduling-coverage.png`
- `docs/qa-evidence/QA_AUDIT_20260429_PM_SCHED_FULL/scheduling-roll-off.png`

## Route + Screen Inventory Matrix

| Route | Screen | Current Purpose | Dense Standard | Drill-down / Detail | Numbering Risk | Logic / Visual Issues | Priority |
|---|---|---|---|---|---|---|---|
| `/portal` | Portal dashboard | App launcher + cross-app alert surface | N/A | Tiles route to modules | Low | Renders; monitor portal thinness language | P3 |
| `/planning` | Planning root | Module entry redirect | N/A | Should redirect to Projects | Low | Redirect resolves to blank `/projects` | P1 |
| `/planning/projects` | Projects list | Project execution log | PASS, 46px rows | Row click to detail | Low | Good baseline-adjacent behavior | P3 |
| `/planning/projects/:id` | Project detail | Project phases/tasks/WOs/milestones/baseline | PASS/PARTIAL | Links to work orders | Low | Project -> WO link may omit query expected by WO back button | P2 |
| `/planning/work-orders` | Work Orders list | Released execution packages | PASS baseline, 46px rows | Row click to detail | Low | Baseline dense standard | P3 |
| `/planning/work-orders/:id` | Work Order detail | Release/status/financial control | PASS/PARTIAL | Links to step-out plans and work packages | Low | Release/status gates require backend hardening | P1 |
| `/planning/step-out-plans` | Step-Out Plans list | Sequenced execution plans | FAIL, 65px rows | Clickable grid | Medium | Rows too tall; zero step counts in seeded rows | P2 |
| `/planning/step-out-plans/new` | New Step-Out Plan | Expected create route | FAIL | Redirects to list | N/A | Route is misleading/dead as create route | P1 |
| `/planning/step-out-plans/:id` | Step-Out detail/form | Edit steps/generate packages | PARTIAL | Internal grids | Medium | Generated package rows lack deep-link proof | P2 |
| `/planning/work-packages` | Work Packages list | Field-facing packages ready for scheduling | FAIL, 68px rows | Row click to detail | High | Ready toggle in clickable row; numbering not proven | P1 |
| `/planning/work-packages/:id` | Work Package detail | Field package context/gates/notes | PARTIAL | Context chips to project/WO | High | Raw `WP-{id}` risk; field-safe behavior needs regression proof | P1 |
| `/planning/fco` | FCO list | Change orders | FAIL, 65px rows | Detail is modal/icon only | Medium | FCO traceability gates missing; no route deep link | P1 |
| `/scheduling` | Scheduling root | Module entry redirect | N/A | Should redirect to dashboard | Low | Redirect resolves to blank `/dashboard` | P1 |
| `/scheduling/dashboard` | Scheduling dashboard | KPI/nav overview and recent jobs | PASS basic | Cards route to boards | Low | Needs dashboard drilldown proof after fixes | P3 |
| `/scheduling/jobs` | Jobs Board | Work demand from estimates/staffing/WPs | FAIL, 65px rows | No clickable/detail affordance detected | High | No Forecast/Released state badge; assignment gate drift | P1 |
| `/scheduling/resources` | Resources | Resource/cert management | FAIL, 65px rows | Modal detail; grid not marked clickable | Low | Dense standard mismatch; controlled craft model pending | P2 |
| `/scheduling/assignments` | Assignments | Assignment log/create/edit | FAIL, 65px rows | Dialogs only; grid not marked clickable | High | Free-text craft/source fallback risks | P1 |
| `/scheduling/coverage` | Craft Coverage | Demand vs assigned by craft | PASS custom | Double-click drawer | Low | Trend fallback simulates data if endpoint missing | P1 |
| `/scheduling/roll-off` | Ending Soon | Reassignment planning | FAIL, 65px rows | Reassign dialog | Medium | Blank craft defaults to `PP` risk | P2 |
## Headline Findings

1. `/planning` and `/scheduling` redirects render blank/near-blank because they resolve to `/projects` and `/dashboard` rather than the module routes.
2. Work Orders is the density baseline at 46px rows. Step-Out, Work Packages, FCO, Jobs, Resources, Assignments, and Roll-Off show 65-68px rows.
3. Jobs Board does not show locked `Forecast` / `Released` demand-state badges.
4. Scheduling assignment APIs/UI still allow old source/craft patterns: assignment saves can target forecast/unreleased demand and free-text craft remains.
5. WorkPackage list/detail numbering is inconsistent; detail can expose raw `WP-{packageId}` instead of a canonical business package number.
6. FCO validation is too soft: FCO can be saved without mandatory Estimate + WorkOrder traceability.
7. WorkOrder status/release gates are bypassable through status endpoint and do not fully validate the Estimate -> CommercialAuthorization -> Project -> WorkOrder chain.
8. Existing Playwright coverage is insufficient. A prior smoke passed while screenshots were blank, and standard config still points at old repo ports.

## Cleanup

No seeded data or user-approved records were deleted. Only QA evidence files/screenshots/logs were created.

