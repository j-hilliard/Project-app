# Architecture Guardrails

These rules are non-negotiable. They exist to prevent technical debt patterns that were
already cleaned up from re-entering the codebase. Every PR and every Codex audit sweep
must verify these rules are satisfied. `tools/architecture-checks/run-all-checks.sh`
automates the checks.

---

## Backend Rules

### ARCH-BE-001 — No raw EF entity `[FromBody]` in cleaned controllers

Controllers for the Planning and Scheduling domains must NOT accept raw EF entity classes
as `[FromBody]` parameters. All input must flow through a dedicated contract record in
`Api/Contracts/`.

**Applies to:** ProjectController, WorkOrderController, PlanningController, SchedulingController

**Violation pattern:** `[FromBody] Project `, `[FromBody] WorkOrder `, `[FromBody] FcoDocument `,
`[FromBody] Resource `, `[FromBody] Assignment `, etc.

**Why:** Raw entity binding lets callers set any navigation property or DB-generated field
(e.g., `CompanyCode`, `CreatedAt`). Contracts enforce an explicit allow-list of writable fields.

**Exception:** CommercialAuthorizationController — partially cleaned, pending its own contract batch.

---

### ARCH-BE-002 — AppDbContext must not contain inline entity configuration

`Data/AppDbContext.cs` must NOT contain `modelBuilder.Entity<T>` calls in `OnModelCreating`.
All entity configuration must live in `Data/Configurations/` as `IEntityTypeConfiguration<T>`
classes, discovered via `ApplyConfigurationsFromAssembly`.

**Why:** A 737-line `OnModelCreating` is unmaintainable and makes domain boundaries invisible.

---

### ARCH-BE-003 — `StatusUpdateRequest` must not be defined inline in controllers

The shared contract record lives in `Api/Contracts/Common/StatusUpdateRequest.cs`.
No controller may redefine it locally.

---

### ARCH-BE-004 — FCO HTML generation must live in `FcoDocumentService`

`Api/Controllers/PlanningController.cs` must NOT contain `BuildFcoHtml` or any inline
HTML-generation method. All FCO document construction belongs in
`Api/Services/Planning/FcoDocumentService`.

---

### ARCH-BE-005 — Financial calculation must live in `WorkOrderFinancialService`

`Api/Controllers/WorkOrderController.cs` must NOT contain inline financial calculation logic.
All calculation belongs in `Api/Services/WorkOrders/WorkOrderFinancialService`.

---

### ARCH-BE-006 — Required service files must exist

The following files must always be present:
- `Api/Services/Planning/FcoDocumentService.cs`
- `Api/Services/WorkOrders/WorkOrderFinancialService.cs`

---

### ARCH-BE-007 — Required contract directories must exist

The following directories must always be present and non-empty:
- `Api/Contracts/Common/`
- `Api/Contracts/Planning/`
- `Api/Contracts/Projects/`
- `Api/Contracts/Scheduling/`
- `Api/Contracts/WorkOrders/`

---

### ARCH-BE-008 — Entity configuration directories must exist

The following directories must always be present:
- `Data/Configurations/Core/`
- `Data/Configurations/Estimating/`
- `Data/Configurations/Planning/`
- `Data/Configurations/Scheduling/`

---

### ARCH-BE-009 — DevController must not be a monolith (warn-only until extraction is complete)

`Api/Controllers/DevController.cs` must be < 300 lines after seeder extraction.
Currently in warning mode — this will become a hard failure once the seeder services
are extracted to `Api/Services/Dev/`.

---

## Frontend Rules

These rules apply to ALL `.vue` files under `webapp/src/modules/planning/` and
`webapp/src/modules/scheduling/`.

### ARCH-FE-001 — PM/Scheduling views must NOT import `useApiStore` directly

All API calls must go through a module-level service composable
(e.g., `usePlanningService`, `useSchedulingService`). Views are orchestration-only.

**Violation pattern:** `import.*useApiStore` in any planning or scheduling `.vue` file.

---

### ARCH-FE-002 — PM/Scheduling views must NOT call `apiStore.api` directly

Even if a view imports the store for other purposes, it must not call
`apiStore.api.<method>` directly.

**Violation pattern:** `apiStore\.api\.` in any planning or scheduling `.vue` file.

---

### ARCH-FE-003 — PM/Scheduling views must NOT define `fmtDate` locally

Date formatting must come from `webapp/src/ui/composables/useFormatters.ts` (or equivalent
shared composable). Local redefinitions create divergent formatting behavior.

**Violation pattern:** `function fmtDate|const fmtDate|fmtDate =` in view files.

---

### ARCH-FE-004 — PM/Scheduling views must NOT define `fmtCurrency` locally

Same rule as ARCH-FE-003 for currency formatting.

**Violation pattern:** `function fmtCurrency|const fmtCurrency|fmtCurrency =` in view files.

---

### ARCH-FE-005 — PM/Scheduling views must NOT define `statusSeverity` locally

Status-to-severity mapping belongs in `webapp/src/ui/utils/severity.ts`. Local copies
produce inconsistent badge colors when the status vocabulary evolves.

**Violation pattern:** `function statusSeverity|const statusSeverity|statusSeverity =` in view files.

---

### ARCH-FE-006 — PM/Scheduling views must NOT define `sourceTagSeverity` locally

Same rule as ARCH-FE-005 for source tag severity.

**Violation pattern:** `function sourceTagSeverity|const sourceTagSeverity|sourceTagSeverity =` in view files.

---

## Design System Rules

### ARCH-DS-001 — `webapp/src/ui/` must exist and be required by at least one view

The `webapp/src/ui/` directory is the platform design system. It must not be ornamental.
At least one component/composable/token from it must be imported by a planning or scheduling view.

---

### ARCH-DS-002 — Shared formatters must live in `webapp/src/ui/`

`fmtDate`, `fmtCurrency`, `statusSeverity`, `sourceTagSeverity` — exactly one canonical
implementation each, exported from `webapp/src/ui/composables/useFormatters.ts` and
`webapp/src/ui/utils/severity.ts`.

---

## Enforcement

- `tools/architecture-checks/run-all-checks.sh` — runs all backend and frontend checks
- Backend checks (ARCH-BE) hard-fail; frontend checks (ARCH-FE) are in warn mode until Batch 3-4 lands
- Codex audit sweeps must run `run-all-checks.sh` and include its exit code and output in evidence packets
- Any PR that introduces a violation must be blocked until the violation is resolved or explicitly waived by Joseph with a dated rationale

---

## Change History

| Date | Batch | Change |
|------|-------|--------|
| 2026-04-29 | Batch 1 | Initial rules created. Backend rules enforced. Frontend rules in warn mode. |
