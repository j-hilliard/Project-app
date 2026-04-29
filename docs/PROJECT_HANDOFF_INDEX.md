# PROJECT HANDOFF INDEX
## Stronghold Enterprise — Developer Handoff Package
**Version:** 1.0
**Date:** 2026-04-28
**Status:** ACTIVE — Canonical Reference

---

## SECTION 1: READING ORDER

Read these docs in order before starting implementation. Do not skip to the build plan without reading 1–4.

| # | Document | Purpose |
|---|----------|---------|
| 1 | `PLATFORM_EXPANSION_MASTER_CONTRACT.md` | Tech stack, module boundaries, toolchain, bootstrap pattern — read first |
| 2 | `PROJECT_LIFECYCLE_MASTER_PLAN.md` | 12-stage business operating model, all entity definitions, stage gates, MVP scope |
| 3 | `PROJECT_DATA_OWNERSHIP_AND_TRACEABILITY.md` | Module ownership, all FK/cascade rules, 7 authorization gates, actuals rollup |
| 4 | `PROJECT_STAGE_GATE_AND_STATUS_MODEL.md` | All status lifecycles, valid transitions, at-risk formulas, auto-transitions |
| 5 | `PROJECT_ACTUALS_AND_VARIANCE_MODEL.md` | ActualEntry entity, 5 rollup levels, variance formulas, demo seed data specs |
| 6 | `PROJECT_PLANNING_UI_MAP.md` | Route tree, page map, custom SVG Gantt architecture, Scheduling UI specs, drill-down flow |
| 7 | `PROJECT_PLANNING_MASTER_TODO.md` | Planning/PM phases 0–13, REUSE/EXTEND/BUILD NEW tags, full impacted files list |
| 8 | `PROJECT_SCHEDULING_PERSONNEL_SPEC.md` | Scheduling personnel: demand state model, jobs board, drawer, assignment modal, Resource Master, CSV import |
| 9 | `PROJECT_SCHEDULING_PERSONNEL_TODO.md` | Scheduling personnel implementation tasks (Phases S1–S9) |
| 10 | `PLATFORM_REQUIREMENTS_TRACEABILITY.md` | QA gates and verification framework |

---

## SECTION 2: CANONICAL SOURCE OF TRUTH PER TOPIC

| Topic | Canonical Doc | Section |
|-------|--------------|---------|
| Tech stack, module boundaries | PLATFORM_EXPANSION_MASTER_CONTRACT.md | All |
| 12-stage lifecycle, entity chain | PROJECT_LIFECYCLE_MASTER_PLAN.md | All |
| `Project` entity definition | PROJECT_LIFECYCLE_MASTER_PLAN.md | §Project entity |
| `CommercialAuthorization` entity | PROJECT_LIFECYCLE_MASTER_PLAN.md | §CommercialAuthorization entity |
| `WorkOrder` entity | PROJECT_LIFECYCLE_MASTER_PLAN.md | §WorkOrder entity |
| `StepOutSubStep` entity | PROJECT_LIFECYCLE_MASTER_PLAN.md | §StepOutSubStep entity |
| `FcoLaborLine` entity | PROJECT_LIFECYCLE_MASTER_PLAN.md | §FcoLaborLine entity |
| `ActualEntry` entity | PROJECT_ACTUALS_AND_VARIANCE_MODEL.md | §ActualEntry entity |
| Module ownership, FK cascade rules | PROJECT_DATA_OWNERSHIP_AND_TRACEABILITY.md | All |
| 7 authorization gates | PROJECT_DATA_OWNERSHIP_AND_TRACEABILITY.md | §Authorization Gates |
| Entity status lifecycles | PROJECT_STAGE_GATE_AND_STATUS_MODEL.md | All |
| At-risk / behind / on-track rules | PROJECT_STAGE_GATE_AND_STATUS_MODEL.md | §At-Risk Rules |
| Variance rollup model | PROJECT_ACTUALS_AND_VARIANCE_MODEL.md | §Rollup Structure |
| CommAuth exposure / authorized-value risk | PROJECT_ACTUALS_AND_VARIANCE_MODEL.md | §CommAuthExposure |
| Demo seed data (3 reference projects) | PROJECT_ACTUALS_AND_VARIANCE_MODEL.md | §Demo Seed Data |
| UI route tree | PROJECT_PLANNING_UI_MAP.md | §Route Map |
| Custom SVG Gantt sub-component tree | PROJECT_PLANNING_UI_MAP.md | §Gantt Architecture |
| Build phases + full task list (Planning/PM) | PROJECT_PLANNING_MASTER_TODO.md | All phases |
| Scheduling demand state model | PROJECT_SCHEDULING_PERSONNEL_SPEC.md | §2 |
| Scheduling jobs board behavior | PROJECT_SCHEDULING_PERSONNEL_SPEC.md | §3, §4 |
| Assignment modal spec | PROJECT_SCHEDULING_PERSONNEL_SPEC.md | §5 |
| Resource Master data model | PROJECT_SCHEDULING_PERSONNEL_SPEC.md | §6 |
| CSV import / upsert workflow | PROJECT_SCHEDULING_PERSONNEL_SPEC.md | §7 |
| Craft / Certification reference model | PROJECT_SCHEDULING_PERSONNEL_SPEC.md | §9 |
| Assignment validation rules | PROJECT_SCHEDULING_PERSONNEL_SPEC.md | §10 |
| Scheduling build task list | PROJECT_SCHEDULING_PERSONNEL_TODO.md | All phases |
| QA verification gates | PLATFORM_REQUIREMENTS_TRACEABILITY.md | All |

---

## SECTION 3: LOCKED DECISIONS

These are final. Do not re-open without explicit PM approval.

| Decision | Resolution | Canonical Doc |
|----------|-----------|---------------|
| Gantt rendering | Custom in-house SVG — zero third-party libraries | PROJECT_PLANNING_UI_MAP.md |
| Top-level execution entity name | `Project` (not `ProjectPlan`) | PROJECT_LIFECYCLE_MASTER_PLAN.md |
| Authorization entity name | `CommercialAuthorization` | PROJECT_LIFECYCLE_MASTER_PLAN.md |
| Grain-level actuals entity name | `ActualEntry` (not `TaskActual`) | PROJECT_ACTUALS_AND_VARIANCE_MODEL.md |
| StepOutSubStep scope | **MVP** — Phase 7, task P1-008 | PROJECT_PLANNING_MASTER_TODO.md |
| StepOut levels | L1/L2 via `ParentStepId` self-ref on StepOutStep; L3 leaf = StepOutSubStep | PROJECT_LIFECYCLE_MASTER_PLAN.md |
| FcoDocument required fields | `LinkedEstimateId` + `LinkedWorkOrderId` — both required, HTTP 422 if missing | PROJECT_DATA_OWNERSHIP_AND_TRACEABILITY.md |
| FcoLaborLine rates | Auto-filled from `Estimate.RateBookId → RateBook.RateBookLaborRates` | PROJECT_LIFECYCLE_MASTER_PLAN.md |
| At-risk threshold | Default 5 days `FinishVariance`; per-project override via `Project.AtRiskThresholdDays` | PROJECT_STAGE_GATE_AND_STATUS_MODEL.md |
| Calendar composable | Extract `useCalendarGrid` from `EstimateCalendarView` — do not copy-paste | PROJECT_PLANNING_UI_MAP.md |
| NSwag sync | Manual CLI only: `cd Api && nswag run nswag.json /variables:Configuration=Debug` — NOT build-triggered | PROJECT_DATA_OWNERSHIP_AND_TRACEABILITY.md |
| Critical path algorithm | Simple longest-chain for MVP | PROJECT_LIFECYCLE_MASTER_PLAN.md |
| Portal KPI change | Replace "Pending FCO count" with "Projects at Risk" count | PROJECT_PLANNING_UI_MAP.md |
| Baseline locking | Explicit `TimelineBaseline` snapshot table (not EF shadow properties) | PROJECT_LIFECYCLE_MASTER_PLAN.md |
| Scheduling scope | **Personnel assignment only** — no Gantt, no PM timeline in Scheduling module | PLATFORM_EXPANSION_MASTER_CONTRACT.md |
| WorkOrder role | **First-class PM entity** — gates step-out plans, FCOs, actuals, and schedulable demand | PROJECT_LIFECYCLE_MASTER_PLAN.md |
| Scheduling demand states | **Forecast** (visible, not assignable) and **Released** (visible, assignable) — enforced server-side | PROJECT_SCHEDULING_PERSONNEL_SPEC.md §2 |
| Assignment craft field | **Dropdown from Craft reference table only** — free-text craft prohibited on all assignments | PROJECT_SCHEDULING_PERSONNEL_SPEC.md §5, §10 |
| Assignability gate | Only WorkPackages backed by a Released/InProgress WorkOrder are assignable — HTTP 422 `DemandNotReleased` otherwise | PROJECT_SCHEDULING_PERSONNEL_SPEC.md §10 |
| Resource import | CSV/spreadsheet import required at launch; upsert by EmployeeId; AD sync future path into same endpoint | PROJECT_SCHEDULING_PERSONNEL_SPEC.md §7, §8 |
| Estimating module | **FROZEN — ~90% complete** — do not touch without explicit Joseph approval | PLATFORM_EXPANSION_MASTER_CONTRACT.md |

---

## SECTION 4: CANONICAL ENTITY CHAIN

The full execution chain from commercial award to field closeout:

```
Estimate  (commercial baseline — Estimating module)
  → CommercialAuthorization  (customer PO/NTP/contract — Planning module)
    → Project  (execution umbrella — Planning module)
      → WorkOrder  (field execution gate — Planning module)
        → StepOutPlan  (execution sequencing — Planning module)
          → StepOutStep  (L1/L2, self-ref via ParentStepId)
            → StepOutSubStep  (L3 leaf, MVP scope)
        → FcoDocument  (change control — requires WorkOrder + Estimate links)
          → FcoLaborLine  (structured labor breakdown, rates from Estimate.RateBookId)
        → ActualEntry  (grain-level actuals — requires WorkOrderId)
```

**Hard gates (HTTP 422 on violation):**
1. No `CommercialAuthorization` created without `Estimate.Status = 'Awarded'`
2. No `Project` created without `CommercialAuthorization.Status = 'Active'`
3. No `WorkOrder` released without `CommercialAuthorization.Status = 'Active'` + authorized value check
4. No `StepOutPlan` without `WorkOrderId`
5. No `FcoDocument` submitted without `LinkedEstimateId`
6. No `FcoDocument` submitted without `LinkedWorkOrderId`
7. No `ActualEntry` created without `WorkOrderId`

---

## SECTION 5: SUPERSEDED / ARCHIVED DOCS

Do not use these as implementation reference. Retained for audit trail only.

| File | Archived Reason | Replaced By |
|------|----------------|-------------|
| `PROJECT_PLANNING_MASTER_PLAN.md` | Used `ProjectPlan`/`TaskActual` (old names); contained uncleared Gantt library recommendation; marked StepOutSubStep as post-MVP (overruled) | PROJECT_LIFECYCLE_MASTER_PLAN.md |
| `PROJECT_PLANNING_DATA_MODEL.md` | Built on old `ProjectPlan`/`TaskActual` model; did not include `CommercialAuthorization` or `WorkOrder` in the hierarchy | PROJECT_LIFECYCLE_MASTER_PLAN.md + PROJECT_DATA_OWNERSHIP_AND_TRACEABILITY.md |

---

## SECTION 6: UNRESOLVED DECISIONS

None. All decisions from the planning session are locked.

---

## SECTION 7: VERIFICATION

Run before implementation starts to confirm no canonical doc still references stale entity names:

```bash
# Should only hit archived docs + this index's Section 5
rg "ProjectPlan" docs/ --include="*.md" -l

# Should only hit archived docs + this index's Section 5
rg "TaskActual" docs/ --include="*.md" -l

# Should return zero hits
rg "dhtmlx" docs/ --include="*.md"

# Should return zero hits in any canonical doc
rg "project-plans" docs/ --include="*.md"

# Should show P1-008 and Phase 7 — confirms MVP scope
rg "StepOutSubStep" docs/PROJECT_PLANNING_MASTER_TODO.md
```
