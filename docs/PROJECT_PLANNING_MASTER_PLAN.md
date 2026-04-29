# PROJECT_PLANNING_MASTER_PLAN — ARCHIVED
**Status:** SUPERSEDED — Do Not Use as Source of Truth
**Archived:** 2026-04-28
**Original Version:** 1.0

---

This document is retained for audit trail only. It contains the following conflicts that made it unsafe as a dev reference:

- **Entity name conflict:** used `ProjectPlan` as top-level container — canonical name is `Project`
- **Entity name conflict:** used `TaskActual` — canonical name is `ActualEntry`
- **Gantt contradiction:** "Recommended: dhtmlx-gantt" text was never cleaned up after decision to build in-house SVG
- **Scope conflict:** Decision #3 marked `StepOutSubStep` as post-MVP — overruled; SubStep is MVP scope (Phase 7, P1-008)

---

## Replaced By

| Topic | Canonical Document |
|-------|--------------------|
| Business process / 12-stage lifecycle | `docs/PROJECT_LIFECYCLE_MASTER_PLAN.md` |
| Entity ownership, FK cascade rules | `docs/PROJECT_DATA_OWNERSHIP_AND_TRACEABILITY.md` |
| Status lifecycles, authorization gates | `docs/PROJECT_STAGE_GATE_AND_STATUS_MODEL.md` |
| Actuals, variance model | `docs/PROJECT_ACTUALS_AND_VARIANCE_MODEL.md` |
| UI route map, Gantt architecture | `docs/PROJECT_PLANNING_UI_MAP.md` |
| Build phases and task list | `docs/PROJECT_PLANNING_MASTER_TODO.md` |

See `docs/PROJECT_HANDOFF_INDEX.md` for the full canonical reading order.
