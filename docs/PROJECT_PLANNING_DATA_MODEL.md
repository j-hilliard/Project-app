# PROJECT_PLANNING_DATA_MODEL — ARCHIVED
**Status:** SUPERSEDED — Do Not Use as Source of Truth
**Archived:** 2026-04-28
**Original Version:** 1.0

---

This document is retained for audit trail only. It contains the following conflicts that made it unsafe as a dev reference:

- **Entity name conflict:** used `ProjectPlan` as top-level container — canonical name is `Project`
- **Entity name conflict:** used `TaskActual` — canonical name is `ActualEntry`
- **Model conflict:** did not include `CommercialAuthorization` or `WorkOrder` as execution-control entities in the hierarchy

---

## Replaced By

| Topic | Canonical Document |
|-------|--------------------|
| All entity definitions (Project, CommercialAuthorization, WorkOrder, StepOutSubStep, FcoLaborLine, ActualEntry, etc.) | `docs/PROJECT_LIFECYCLE_MASTER_PLAN.md` |
| FK cascade rules, module ownership, authorization gates | `docs/PROJECT_DATA_OWNERSHIP_AND_TRACEABILITY.md` |

See `docs/PROJECT_HANDOFF_INDEX.md` for the full canonical reading order.
