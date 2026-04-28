# Platform Requirements Traceability

Source: Joseph's Portal / Planning / Scheduling master requirements pasted to Codex on 2026-04-28.

Purpose: this is the QA spine for the platform expansion. Claude may plan or implement, but Codex verifies against this mapping before any live TODO item is closed.

## Verification Order

1. Convert Joseph requirement into live QA acceptance gates.
2. Add or update matching `docs/QA_REGRESSION_CHECKLIST.md` entries.
3. Audit Claude's plan/code against the gates.
4. Capture evidence: file paths, build/test output, screenshots/API output where applicable.
5. Close only after Joseph or Codex verification.

## Requirement Map

| Requirement Area | Must Prove | Live QA Gate | Regression Gate |
|---|---|---|---|
| Repo truth before coding | Real paths are inspected; no invented `Api/Data` or wrong migrations paths; existing structure is documented. | `PLATFORM-PHASE-0`, `PLATFORM-001` | `QA-PLAT-001`, `QA-PLAT-005` |
| One platform / one login / app shell | Portal is a shared shell with Estimating, Planning, Scheduling; `/estimating` remains intact. | `PLATFORM-003`, `PLATFORM-004` | `QA-PLAT-002`, `QA-PLAT-004` |
| Portal stays thin | Portal consumes backend summary/read models and does not own planning/scheduling business logic. | `PLATFORM-005` | `QA-PLAT-002` |
| Business boundaries | Estimating owns commercial records; Planning owns step-out/work packages/FCO readiness; Scheduling owns people/resources/assignments/coverage. | `PLATFORM-001`, `PLATFORM-007`, `PLATFORM-009` | `QA-PLAT-005`, `QA-PLAN-001`, `QA-SCHED-004` |
| EF Code First / bootstrap cleanup | Migrations are source of truth; no `EnsureCreated`; reference seed and demo seed are separate; config controls bootstrap behavior. | `PLATFORM-006` | `QA-BOOT-001` |
| Generated API client / NSwag | If API surface changes and repo uses generated clients, generation commands are identified and run or the gap is documented. | `PLATFORM-017` | `QA-PLAT-008` |
| Planning app foundation | Planning has real step-out plans, source links, steps, dependencies, parallel work, craft/headcount, work packages. | `PLATFORM-007` | `QA-PLAN-001`, `QA-PLAN-002`, `QA-PLAN-003`, `QA-PLAN-004`, `QA-PLAN-005` |
| Granular step-out behavior | Supports multiple steps, `1 / 1.2 / 1.5` style ordering, duration, people count, craft, status, notes, work-package handoff. | `PLATFORM-007` | `QA-PLAN-002`, `QA-PLAN-003`, `QA-PLAN-004` |
| Scheduling demand sources | Demand comes from qualifying estimates, approved unconverted staffing plans, and ready planning work packages. | `PLATFORM-008` | `QA-SCHED-001`, `QA-SCHED-003` |
| Converted staffing dedupe | Staffing plans with `ConvertedEstimateId != null` do not count separately. | `PLATFORM-008` | `QA-SCHED-002` |
| Scheduling domain ownership | Scheduling owns resources, availability, PTO/blackout, assignments, conflicts, shortages, roll-off, available-soon. | `PLATFORM-009` | `QA-SCHED-004`, `QA-SCHED-005`, `QA-SCHED-007` |
| Scheduling UX | Users can filter/sort/group by craft/date/status/branch where supported, see gaps, assign/reassign people, and see ending-soon/available-soon. | `PLATFORM-009` | `QA-SCHED-004`, `QA-SCHED-006`, `QA-SCHED-007` |
| Dashboard/forecast integration | Platform/manpower style dashboard compares demand, assigned headcount, available resources, craft gaps, ending-soon jobs, freeing-up resources. | `PLATFORM-005`, `PLATFORM-008`, `PLATFORM-009` | `QA-SCHED-008`, existing `QA-AN-*` |
| Shared cleanup | Core demand/coverage/conflict logic moves to backend/shared services where appropriate; views do not become the operational source of truth. | `PLATFORM-001`, `PLATFORM-008`, `PLATFORM-009`, `PLATFORM-018` | `QA-PLAT-009`, `QA-SCHED-001` |
| Demo seed | Seed proves estimates, staffing, planning, resources, assignments, shortages, conflicts, ending-soon, available-soon, converted-plan dedupe. | `PLATFORM-012`, `PLATFORM-020` | `QA-DATA-001`, `QA-DATA-002`, `QA-DATA-005`, `QA-BOOT-002`, `QA-SCHED-002`, `QA-SCHED-004` |
| Tests and safe verification | Backend/frontend builds run; real test commands are documented; e2e avoids destructive reset/default mutation. | `PLATFORM-013`, `PLATFORM-016`, `P1-007` | `QA-PLAT-007`, existing `P1-007` checks |
| Live TODO/worklog process | Claude keeps implementation docs current, but only Joseph/Codex closes QA TODOs. | `PLATFORM-002`, `PLATFORM-013` | `QA-PLAT-003`, `QA-PLAT-007` |
| Claude current queue | Claude's visible scheduling checklist must be audited task-by-task, not only as a final app smoke test. | `PLATFORM-019` | `QA-PLAT-007`, `QA-PLAN-005`, `QA-SCHED-007`, `QA-SCHED-008` |
| Full project planning master plan | Claude must produce planning-only docs grounded in the repo before coding project timelines/Gantt/calendar/task hierarchy. | `PLATFORM-021` | `QA-PLAN-006` |
| Project planning domain model | Project planning must model plans, timelines, phases, tasks, dependencies, milestones, step-out details, baselines, variance, and calendar entries. | `PLATFORM-022` | `QA-PLAN-007`, `QA-PLAN-009` |
| Estimate/FCO/task traceability and schedule health | The plan must define bidirectional estimate/FCO/task traceability, ownership, validation, ahead/behind formulas, slippage, and downstream impact. | `PLATFORM-023` | `QA-PLAN-008`, `QA-PLAN-010`, `QA-FCO-002` |

## Auditor Actions When Claude Claims A Phase Is Done

- Compare changed files to the relevant Live QA gate.
- Run the required build/test commands listed by Claude, plus any repo-standard checks Codex knows are required.
- For UI-visible work, capture screenshots under `docs/qa-evidence/<sweep-id>/`.
- If a requirement is missing from Claude's plan/code, add or reopen a live QA TODO item before asking for another implementation pass.
- If Claude self-checks an item without Joseph/Codex evidence, reopen it.

## Current Known Risks

- Bootstrap cleanup is dangerous because demo data already disappeared once when the API did not own `7211` and seed behavior was split between `Data` and `Api/Controllers/DevController.cs`.
- Existing worklog claims must be verified against actual files; previous spot-check found at least one wrong statement about `Data/DesignTimeContextFactory.cs`.
- Planning and Scheduling routes/entities can be scaffolded easily but still fail the business goal if they are placeholders.
- Demand math is a high-risk area because converted staffing plans must never double count with estimates.
