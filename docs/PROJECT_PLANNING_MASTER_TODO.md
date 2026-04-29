# PROJECT PLANNING MASTER TODO
## Stronghold Enterprise — Full Lifecycle Implementation Plan
**Document Version:** 3.0  
**Date:** 2026-04-28  
**Status:** APPROVED — Implementation Pending  
**Supersedes:** v2.0

Tags: `[REUSE]` `[EXTEND]` `[BUILD NEW]`

---

## DEPENDENCY ORDER

```
Phase 0 (Migration Cleanup)
  → Phase 1 (New Data Models)
  → Phase 2 (Extend Existing Models)
  → Phase 3 (Migration)
  → Phase 4 (Backend Controllers + Services)
  → Phase 5 (NSwag Sync)
  → Phase 6 (Frontend Shell — Project + WO)
  → Phase 7 (Step-Out Hierarchy)
  → Phase 8 (FCO v2 with Labor Lines)
  → Phase 9 (Actuals)
  → Phase 10 (Gantt + Calendar)
  → Phase 11 (Health Dashboard + Traceability)
  → Phase 12 (Demo Seed Data)
  → Phase 13 (QA + Regression)
```

No Phase 4+ work starts before Phase 3 (migration applied and building).  
No Phase 5+ work starts before Phase 4 (controllers exist for NSwag to read).  
No frontend work starts before Phase 5 (NSwag client generated).

---

## PHASE 0 — MIGRATION CLEANUP

### P0-001 — Audit and Resolve Previous Planning Migration `[EXTEND]`
**Context:** Migrations `20260428145427_AddPlanningEntities` and `20260428145542_AddSchedulingEntities` were added on 2026-04-28. However, the corresponding C# model files for legacy planning tables and current lifecycle entities may not exist as separate files. Audit status is unclear.

**Action:**
1. Run `cd Data && dotnet ef migrations list --startup-project ../Api` — confirm which migrations are applied
2. Check whether corresponding model files exist under `Data/Models/`
3. If model files are MISSING but migrations exist: the tables exist in DB but C# models are absent — need to create model files to match
4. Decision: If prior planning migration is incomplete or inconsistent, create a rollback migration OR rewrite models to match. Document resolution.

**Acceptance:** `dotnet ef migrations list` shows clean state. Model files exist for any applied migration.

---

## PHASE 1 — NEW DATA MODELS

### P1-001 — Create CommercialAuthorization Model `[BUILD NEW]`
**File:** `Data/Models/CommercialAuthorization.cs`

**Fields:** CommercialAuthorizationId, CompanyCode, EstimateId (FK→Estimates Restrict), AuthorizationType (PO|SignedProposal|Contract|NTP|WorkAuthorization|ReleaseOrder), AuthorizationNumber, AuthorizedValue (decimal 18,2), AuthorizedBy, ReceivedDate?, EffectiveDate?, ExpirationDate?, Status (Draft|Submitted|Active|Superseded|Closed|Cancelled), Notes, DocumentReference, CreatedBy, CreatedAt, UpdatedAt

**Acceptance:** Compiles; FK to Estimates with Restrict delete; status default 'Draft'

---

### P1-002 — Create Project Model `[BUILD NEW]`
**File:** `Data/Models/Project.cs`

**Fields:** ProjectId, CompanyCode, ProjectNumber (unique compound with CompanyCode), Name, EstimateId (FK→Estimates Restrict), CommercialAuthorizationId? (FK→CommercialAuthorizations SetNull), Client, ClientCode?, Site?, City?, State?, JobLetter?, PlannedStart?, PlannedEnd?, BaselineStart?, BaselineEnd?, ActualStart?, ActualEnd?, ForecastEnd?, Status (Initiating|Planning|Active|Monitoring|Closing|Closed|OnHold|Cancelled), AtRiskThresholdDays (int default 5), OwnerUserId?, LessonsLearnedNotes?, CreatedBy, CreatedAt, UpdatedAt

**Navigation:** HasMany<ProjectPhase>(Cascade), HasMany<Milestone>(Cascade), HasMany<TimelineBaseline>(Cascade), HasMany<WorkOrder>

**Acceptance:** Compiles; unique index on CompanyCode+ProjectNumber; FKs correct

---

### P1-003 — Create WorkOrder Model `[BUILD NEW]`
**File:** `Data/Models/WorkOrder.cs`

**Fields:** WorkOrderId, CompanyCode, WorkOrderNumber (unique compound with CompanyCode), ProjectId (FK→Projects Restrict), EstimateId (FK→Estimates Restrict), CommercialAuthorizationId (FK→CommercialAuthorizations Restrict), Title, Description?, Scope?, AuthorizedValue (decimal 18,2 default 0), PlannedStart?, PlannedEnd?, ActualStart?, ActualEnd?, ForecastEnd?, Status (Draft|Released|InProgress|Complete|Closed|Cancelled), ReleasedBy?, ReleasedAt?, CreatedBy, CreatedAt, UpdatedAt

**Navigation:** HasMany<StepOutPlan>(SetNull), HasMany<WorkPackage>(SetNull), HasMany<FcoDocument>(Restrict), HasMany<ActualEntry>(Restrict)

**Acceptance:** Compiles; three required FKs; status default 'Draft'

---

### P1-004 — Create ProjectPhase Model `[BUILD NEW or VERIFY]`
**File:** `Data/Models/ProjectPhase.cs` (may already exist from prior migration — verify first)

**Fields:** PhaseId, ProjectId (FK→Projects Cascade), Name, Description?, PlannedStart?, PlannedEnd?, BaselineStart?, BaselineEnd?, ActualStart?, ActualEnd?, ForecastEnd?, SortOrder, Status (Planning|Active|Complete|OnHold|Cancelled), Color?

**Acceptance:** Exists and compiles. If prior version exists, verify fields match spec.

---

### P1-005 — Create PlanTask Model `[BUILD NEW or VERIFY]`
**File:** `Data/Models/PlanTask.cs`

**Fields:** TaskId, PhaseId (FK→ProjectPhases Cascade), ParentTaskId? (self-ref NoAction), Title, Description?, TaskType (Task|Milestone|Gate), LinkedEstimateId? (FK→Estimates Restrict), LinkedFcoDocumentId? (FK→FcoDocuments Restrict), LinkedStepOutPlanId? (FK→StepOutPlans SetNull), PlannedStart?, PlannedEnd?, DurationDays, BaselineStart?, BaselineEnd?, BaselineDuration?, ActualStart?, ActualEnd?, ForecastEnd?, PercentComplete (decimal 5,2), Status, CraftCode?, AssignedTo?, OwnerUserId?, SortOrder, IsMilestone, CreatedBy, CreatedAt, UpdatedAt

**Acceptance:** Self-ref FK compiles with NoAction; all junction FKs resolve

---

### P1-006 — Create Milestone Model `[BUILD NEW or VERIFY]`
**File:** `Data/Models/Milestone.cs`

**Fields:** MilestoneId, ProjectId (FK→Projects Cascade), PhaseId? (FK→ProjectPhases SetNull), TaskId? (FK→PlanTasks SetNull), Name, Description?, PlannedDate, BaselineDate?, ActualDate?, Status (Pending|Achieved|Missed|Cancelled), IsDeadline, IsCritical, Color?, CreatedBy, CreatedAt

**Acceptance:** Multiple optional FKs with correct cascade rules

---

### P1-007 — Create TaskDependency Model `[BUILD NEW or VERIFY]`
**File:** `Data/Models/TaskDependency.cs`

**Fields:** DependencyId, SuccessorTaskId (FK→PlanTasks Cascade), PredecessorTaskId (FK→PlanTasks NoAction), DependencyType (FinishToStart|StartToStart|FinishToFinish|StartToFinish), LagDays default 0

**Unique index:** (SuccessorTaskId, PredecessorTaskId)

---

### P1-008 — Create StepOutSubStep Model `[BUILD NEW]`
**File:** `Data/Models/StepOutSubStep.cs`

**Fields:** SubStepId, StepId (FK→StepOutSteps Cascade), SubStepCode (nvarchar 20), SortOrder (decimal 10,4), Title, Description?, DurationHours (decimal 8,2)?, RequiredPeople (default 1), CraftCode?, IsParallel (bit), Status (Pending|InProgress|Complete|Blocked), ActualStart?, ActualEnd?, ActualDurationHours (decimal 8,2)?, Notes?

**Acceptance:** Cascade FK to StepOutSteps; DurationHours supports .25/.5/1.5

---

### P1-009 — Create FcoLaborLine Model `[BUILD NEW]`
**File:** `Data/Models/FcoLaborLine.cs`

**Fields:** FcoLaborLineId, FcoDocumentId (FK→FcoDocuments Cascade), Position (required), LaborType (Direct|Indirect), CraftCode?, NavCode?, StHours (decimal 10,2), OtHours (decimal 10,2), DtHours (decimal 10,2), BillStRate (decimal 18,4), BillOtRate (decimal 18,4), BillDtRate (decimal 18,4), Subtotal (decimal 18,2), SortOrder, Notes?

**Rate lookup:** Populated from `Estimate.RateBookId → RateBook.RateBookLaborRates WHERE Position = X` at line creation time.

**Acceptance:** Cascade FK; precision on rates/subtotal; compiles

---

### P1-010 — Create ActualEntry Model `[BUILD NEW]`
**File:** `Data/Models/ActualEntry.cs`

**Fields:** ActualEntryId, CompanyCode, WorkOrderId (FK→WorkOrders Restrict), FcoDocumentId? (FK→FcoDocuments SetNull), PlanTaskId? (FK→PlanTasks SetNull), StepOutStepId? (FK→StepOutSteps SetNull), StepOutSubStepId? (FK→StepOutSubSteps SetNull), ActualType (Labor|Equipment|Material|Subcontract|Other), ActualDate, Description?, Position?, CraftCode?, StHours (decimal 10,2), OtHours, DtHours, CostAmount (decimal 18,2), BillableAmount (decimal 18,2), BilledAmount (decimal 18,2), IsConfirmed (bit), EnteredBy, Notes?, CreatedAt

**Critical:** WorkOrderId OnDelete(Restrict) — no actuals deleted when WO is touched

**Acceptance:** All six FKs compile; WorkOrderId Restrict; all decimal precision correct

---

### P1-011 — Create EstimateTaskLink Model `[BUILD NEW or VERIFY]`
**File:** `Data/Models/EstimateTaskLink.cs`

**Fields:** LinkId, TaskId (FK→PlanTasks Cascade), EstimateId (FK→Estimates Restrict), LinkType (Primary|Supporting|Reference), Notes?, IsArchived bit default false, CreatedAt

**Unique index:** (TaskId, EstimateId)

---

### P1-012 — Create FcoTaskLink Model `[BUILD NEW or VERIFY]`
**File:** `Data/Models/FcoTaskLink.cs`

**Fields:** LinkId, TaskId (FK→PlanTasks Cascade), FcoDocumentId (FK→FcoDocuments Restrict), LinkType (ScopeAddition|ScheduleImpact|Reference), Notes?, CreatedAt

**Unique index:** (TaskId, FcoDocumentId)

---

### P1-013 — Create TimelineBaseline Model `[BUILD NEW or VERIFY]`
**File:** `Data/Models/TimelineBaseline.cs`

**Fields:** BaselineId, ProjectId (FK→Projects Cascade), EntityType (Project|Phase|Task), EntityId (int — naked, not live FK), SnapshotDate, PlannedStart?, PlannedEnd?, DurationDays?, BaselineReason?, BaselineLabel?, LockedBy, LockedAt

---

### P1-014 — Create TaskProgressSnapshot Model `[BUILD NEW or VERIFY]`
**File:** `Data/Models/TaskProgressSnapshot.cs`

**Fields:** SnapshotId, TaskId (FK→PlanTasks Cascade), SnapshotDate, PercentComplete (decimal 5,2), ForecastEnd?, Status, ReportedBy, Notes?, CreatedAt

---

---

## PHASE 2 — EXTEND EXISTING MODELS

### P2-001 — Add ParentStepId and DurationHours to StepOutStep `[EXTEND]`
**File:** `Data/Models/StepOutStep.cs`

**Add:**
```csharp
public int? ParentStepId { get; set; }
public StepOutStep? ParentStep { get; set; }
public ICollection<StepOutStep> SubSteps { get; set; }
public ICollection<StepOutSubStep> SubStepLeafs { get; set; }
public decimal? DurationHours { get; set; }  // replaces DurationMinutes for display; keep both for migration safety
```

**Fluent API:**
```csharp
entity.HasOne(e => e.ParentStep).WithMany(e => e.SubSteps)
      .HasForeignKey(e => e.ParentStepId).OnDelete(DeleteBehavior.NoAction);
```

**Acceptance:** Self-ref FK with NoAction compiles; existing data unaffected (ParentStepId defaults to NULL)

---

### P2-002 — Add WorkOrderId to StepOutPlan `[EXTEND]`
**File:** `Data/Models/StepOutPlan.cs`

**Add:**
```csharp
public int? WorkOrderId { get; set; }
public WorkOrder? WorkOrder { get; set; }
```

**Fluent API:** `OnDelete(SetNull)` — if WorkOrder is cancelled, plan becomes unlinked (flagged as orphan)

**Acceptance:** Nullable; existing data unaffected; new plans should have WorkOrderId set

---

### P2-003 — Add WorkOrderId and LinkedWorkOrderId to FcoDocument `[EXTEND]`
**File:** `Data/Models/FcoDocument.cs`

**Add:**
```csharp
public int? LinkedWorkOrderId { get; set; }
public WorkOrder? LinkedWorkOrder { get; set; }
```

**Fluent API:** `OnDelete(Restrict)` — FcoDocument blocks WorkOrder deletion

**Note on LaborBreakdownJson:** Keep the field in place for now. New `FcoLaborLine` entities are authoritative. LaborBreakdownJson becomes legacy/deprecated — do NOT write to it from new code.

**Acceptance:** FK added; existing LaborBreakdownJson data preserved; new FCOs use FcoLaborLine

---

### P2-004 — Add WorkOrderId to WorkPackage `[EXTEND]`
**File:** `Data/Models/WorkPackage.cs`

**Add:**
```csharp
public int? WorkOrderId { get; set; }
public WorkOrder? WorkOrder { get; set; }
```

**Fluent API:** `OnDelete(SetNull)`

**Acceptance:** Nullable; existing data unaffected

---

### P2-005 — Register All New DbSets in AppDbContext `[EXTEND]`
**File:** `Data/AppDbContext.cs`

**Add DbSets:**
```csharp
public DbSet<CommercialAuthorization> CommercialAuthorizations => Set<CommercialAuthorization>();
public DbSet<Project> Projects => Set<Project>();
public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
public DbSet<ProjectPhase> ProjectPhases => Set<ProjectPhase>();
public DbSet<PlanTask> PlanTasks => Set<PlanTask>();
public DbSet<TaskDependency> TaskDependencies => Set<TaskDependency>();
public DbSet<Milestone> Milestones => Set<Milestone>();
public DbSet<EstimateTaskLink> EstimateTaskLinks => Set<EstimateTaskLink>();
public DbSet<FcoTaskLink> FcoTaskLinks => Set<FcoTaskLink>();
public DbSet<TimelineBaseline> TimelineBaselines => Set<TimelineBaseline>();
public DbSet<TaskProgressSnapshot> TaskProgressSnapshots => Set<TaskProgressSnapshot>();
public DbSet<StepOutSubStep> StepOutSubSteps => Set<StepOutSubStep>();
public DbSet<FcoLaborLine> FcoLaborLines => Set<FcoLaborLine>();
public DbSet<ActualEntry> ActualEntries => Set<ActualEntry>();
```

**Add all Fluent API configurations** for new entities and FK extensions in `OnModelCreating`.

**Acceptance:** `dotnet build` passes; EF model validation passes; no duplicate entity configs

---

## PHASE 3 — MIGRATION

### P3-001 — Generate Migration: AddFullProjectLifecycleEntities `[BUILD NEW]`

**Pre-check:** Run `cd Data && dotnet ef migrations list --startup-project ../Api` to confirm prior migration state. If prior planning migrations are inconsistent, resolve per P0-001 first.

**Command:**
```bash
cd Data
dotnet ef migrations add AddFullProjectLifecycleEntities --startup-project ../Api
```

**Verify migration file creates:**
- CommercialAuthorizations table
- Projects table
- WorkOrders table
- ProjectPhases table (or confirms it exists correctly)
- PlanTasks table
- TaskDependencies table
- Milestones table
- EstimateTaskLinks table
- FcoTaskLinks table
- TimelineBaselines table
- TaskProgressSnapshots table
- StepOutSubSteps table
- FcoLaborLines table
- ActualEntries table
- ALTER StepOutPlans: add WorkOrderId, ParentStepId, DurationHours
- ALTER WorkPackages: add WorkOrderId
- ALTER FcoDocuments: add LinkedWorkOrderId

**Apply:**
```bash
cd Data
dotnet ef database update --startup-project ../Api
```

**Acceptance:** Migration runs to completion; no SQL errors; schema verified in SQL Server Management Studio

---

## PHASE 4 — BACKEND CONTROLLERS + SERVICES

### P4-001 — Create CommercialAuthorizationController `[BUILD NEW]`
**File:** `Api/Controllers/CommercialAuthorizationController.cs`  
**Route:** `api/v1/commercial-authorizations`

**Endpoints:**
- `GET /` — list (filter: companyCode, estimateId, status)
- `GET /:id` — detail
- `POST /` — create (validates Estimate.Status = 'Awarded')
- `PUT /:id` — update
- `PATCH /:id/status` — status transition (validates single Active per Estimate)
- `DELETE /:id` — soft-delete (only if Draft and no linked Project)

**Gate enforcement:** Only one CommAuth per Estimate can be 'Active' at a time.

---

### P4-002 — Create ProjectController `[BUILD NEW]`
**File:** `Api/Controllers/ProjectController.cs`  
**Route:** `api/v1/projects`

**Endpoints:**
- `GET /` — list (filter: companyCode, status, estimateId, health)
- `GET /:id` — detail with phase summary
- `GET /:id/full` — detail + phases + tasks + milestones + WOs
- `POST /` — create (gate: Estimate.Awarded + CommAuth.Active)
- `PUT /:id` — update
- `PATCH /:id/status` — status transitions (gate: closeout validation)
- `POST /:id/baseline` — lock TimelineBaseline

---

### P4-003 — Create WorkOrderController `[BUILD NEW]`
**File:** `Api/Controllers/WorkOrderController.cs`  
**Route:** `api/v1/work-orders`

**Endpoints:**
- `GET /` — list (filter: projectId, estimateId, status, companyCode)
- `GET /:id` — detail
- `POST /` — create (validates Project, CommAuth, Estimate)
- `PUT /:id` — update
- `PATCH /:id/release` — release (full gate check: CommAuth Active, AuthorizedValue check)
- `PATCH /:id/status` — other transitions
- `DELETE /:id` — only if Draft; block if actuals exist

---

### P4-004 — Create ProjectPhaseController `[BUILD NEW]`
**File:** `Api/Controllers/ProjectPhaseController.cs`  
**Route:** `api/v1/projects/:projectId/phases`

**Endpoints:** CRUD + sort order update + cascade delete

---

### P4-005 — Create PlanTaskController `[BUILD NEW]`
**File:** `Api/Controllers/PlanTaskController.cs`  
**Route:** `api/v1/plan-tasks`

**Endpoints:** Full CRUD + PATCH /status (with EstimateTaskLink soft gate) + PATCH /progress + estimate/fco link management + actuals + snapshots

---

### P4-006 — Create MilestoneController `[BUILD NEW]`
**File:** `Api/Controllers/MilestoneController.cs`  
**Route:** `api/v1/milestones`

**Endpoints:** CRUD + PATCH /achieve + PATCH /miss

---

### P4-007 — Extend PlanningController: StepOutSubStep `[EXTEND]`
**File:** `Api/Controllers/PlanningController.cs`

**Add endpoints:**
- `GET /api/v1/planning/step-out-steps/:stepId/sub-steps`
- `POST /api/v1/planning/step-out-steps/:stepId/sub-steps`
- `PUT /api/v1/planning/sub-steps/:id`
- `PATCH /api/v1/planning/sub-steps/:id/status`
- `DELETE /api/v1/planning/sub-steps/:id`

---

### P4-008 — Extend PlanningController: FcoLaborLine `[EXTEND]`
**File:** `Api/Controllers/PlanningController.cs`

**Add endpoints:**
- `GET /api/v1/planning/fco/:fcoId/labor-lines`
- `POST /api/v1/planning/fco/:fcoId/labor-lines` — with rate lookup from Estimate.RateBook
- `PUT /api/v1/planning/fco/labor-lines/:id`
- `DELETE /api/v1/planning/fco/labor-lines/:id`
- `GET /api/v1/planning/fco/:fcoId/rate-book-positions` — returns positions from estimate's rate book for dropdown

---

### P4-009 — Create ActualEntryController `[BUILD NEW]`
**File:** `Api/Controllers/ActualEntryController.cs`  
**Route:** `api/v1/actual-entries`

**Endpoints:** CRUD + PATCH /confirm + list by workOrderId/projectId/estimateId

---

### P4-010 — Create ScheduleHealthService `[BUILD NEW]`
**File:** `Api/Services/ScheduleHealthService.cs`

**Methods:**
- `CalculateTaskHealth(PlanTask task, DateTime today) → TaskHealthDto`
- `CalculatePhaseHealth(int phaseId, DateTime today) → PhaseHealthDto`
- `CalculateProjectHealth(int projectId, DateTime today) → ProjectHealthDto`
- `GetMilestoneSlippage(int projectId, DateTime today) → List<MilestoneSlippageDto>`
- `GetCriticalPath(int projectId) → List<int>` (simple longest-chain)

---

### P4-011 — Create ScheduleHealthController `[BUILD NEW]`
**File:** `Api/Controllers/ScheduleHealthController.cs`  
**Route:** `api/v1/schedule-health`

**Endpoints:** Projects health list, project detail health, critical path, milestones

---

### P4-012 — Create VarianceService `[BUILD NEW]`
**File:** `Api/Services/VarianceService.cs`

**Methods:**
- `GetEstimateVsActual(int estimateId) → EstimateVsActualDto`
- `GetWorkOrderActuals(int workOrderId) → WorkOrderActualSummaryDto`
- `GetProjectActuals(int projectId) → ProjectActualSummaryDto`
- `GetFcoVsActual(int fcoDocumentId) → FcoVsActualDto`
- `GetCommAuthExposure(int commAuthId) → CommAuthExposureDto`

---

### P4-013 — Create VarianceController `[BUILD NEW]`
**File:** `Api/Controllers/VarianceController.cs`  
**Route:** `api/v1/variance`

**Endpoints:** Matches VarianceService methods

---

### P4-014 — Create TraceabilityController `[BUILD NEW]`
**File:** `Api/Controllers/TraceabilityController.cs`  
**Route:** `api/v1/traceability`

**Endpoints:**
- `GET /broken-links` — orphans, missing WO on FCO, etc.
- `GET /project/:projectId/chain` — full estimate→WO→step chain
- `GET /estimates/:id/tasks` — tasks linked to estimate
- `GET /estimates/:id/delayed` — delayed tasks

---

### P4-015 — Update PortalController: Add Planning KPIs `[EXTEND]`
**File:** `Api/Controllers/PortalController.cs`

**Add to dashboard response:**
- `projectsAtRiskCount` — Projects WHERE health = 'AtRisk' or 'Behind'
- `milestonesAtRiskCount` — Milestones WHERE PlannedDate ≤ Today + AtRiskThreshold AND not Achieved
- `pendingWorkOrderReleaseCount` — WorkOrders WHERE Status = 'Draft' AND all gates would pass
- Replace `pendingFcoCount` field label with `projectsAtRiskCount`

---

## PHASE 5 — NSWAG SYNC

### P5-001 — Run NSwag to Regenerate TypeScript Client `[REUSE]`

**Context:** NSwag is MANUAL in this repo. NOT triggered by dotnet build.

**Steps:**
1. Build the API: `cd Api && dotnet build`
2. Start the API on Local profile (required for NSwag runtime introspection)
3. Run: `cd Api && nswag run nswag.json /variables:Configuration=Debug`
4. Output: `webapp/src/apiclient/client.ts` — verify new client classes exist
5. Run: `cd webapp && npm run build` — verify 0 TypeScript errors

**New client classes expected after this phase:**
- `CommercialAuthorizationsClient`
- `ProjectsClient`
- `WorkOrdersClient`
- `ProjectPhasesClient`
- `PlanTasksClient`
- `MilestonesClient`
- `ActualEntriesClient`
- `ScheduleHealthClient`
- `VarianceClient`
- `TraceabilityClient`

**Acceptance:** TypeScript build passes; all new client classes visible in client.ts

---

## PHASE 6 — FRONTEND SHELL (PROJECT + WORK ORDER)

### P6-001 — Create ProjectListView `[BUILD NEW + REUSE PATTERN]`
**File:** `webapp/src/modules/planning/views/ProjectListView.vue`  
**Pattern:** Follows EstimateListView (table + card dual view, filters, pagination)  
**Columns:** Project #, Name, Client, Status, WO Count, Health, Start, End  
**Actions:** Click → ProjectDetailView; + New Project  
**Data:** `GET /api/v1/projects`

---

### P6-002 — Create ProjectDetailView `[BUILD NEW]`
**File:** `webapp/src/modules/planning/views/ProjectDetailView.vue`  
**Layout:** Header (project meta + CommAuth status), KPI strip (health, WOs, milestones, % complete, variance), Tab bar (Overview | Gantt | Calendar | Milestones | Traceability | Actuals), Phase list overview  
**Data:** `GET /api/v1/projects/:id/full`

---

### P6-003 — Create WorkOrderListView `[BUILD NEW]`
**File:** `webapp/src/modules/planning/views/WorkOrderListView.vue`  
**Columns:** WO #, Project, Status, Authorized $, Actual Cost, Billing %, Start, End  
**Actions:** Create WO, Release WO, View WO  
**Data:** `GET /api/v1/work-orders?projectId=X`

---

### P6-004 — Create WorkOrderDetailView `[BUILD NEW]`
**File:** `webapp/src/modules/planning/views/WorkOrderDetailView.vue`  
**Layout:** WO header, StepOutPlans tab, WorkPackages tab, Actuals tab, FCO tab  
**Gate display:** Show release gate status (CommAuth check, AuthorizedValue check)

---

### P6-005 — Create CommercialAuthorizationView `[BUILD NEW]`
**File:** `webapp/src/modules/planning/views/CommercialAuthorizationView.vue`  
**Purpose:** Manage authorizations for an estimate/project  
**Data:** `GET /api/v1/commercial-authorizations?estimateId=X`

---

### P6-006 — Update Planning Router `[EXTEND]`
**File:** `webapp/src/modules/planning/router/index.ts`

**Add routes:**
```
/planning/projects
/planning/projects/new
/planning/projects/:id
/planning/projects/:id/gantt
/planning/projects/:id/calendar
/planning/projects/:id/phases/:phaseId
/planning/projects/:id/phases/:phaseId/tasks/:taskId
/planning/projects/:id/work-orders
/planning/projects/:id/work-orders/:woId
/planning/milestones
/planning/health
/planning/traceability
/planning/actuals
```

---

### P6-007 — Update Planning Menu in apps.ts `[EXTEND]`
**File:** `webapp/src/apps.ts`

**Replace/extend Planning menu:**
```
Projects section:  All Projects, New Project
Work Orders:       Work Orders
Plans section:     Step-Out Plans, New Plan (existing)
Work & Change:     Work Packages (existing), FCO/Change Orders (existing)
Views:             Milestone Tracker, Schedule Health, Actuals
Traceability:      Estimate → Task Matrix
```

---

## PHASE 7 — STEP-OUT HIERARCHY (SUBSTEPS)

### P7-001 — Extend StepOutPlanFormView with SubStep Support `[EXTEND]`
**File:** `webapp/src/modules/planning/views/StepOutPlanFormView.vue`

**Add:**
- L3 SubStep rows inline under L2 StepOutStep rows
- Add SubStep button (only appears on L2 steps)
- SubStep row shows: SubStepCode, Title, DurationHours, RequiredPeople, CraftCode, Status
- Collapse/expand L2 → SubSteps
- Duration input as decimal hours (.25, .5, 1.0, 1.5)
- Status transitions gated by WorkOrder.Status

**Data:** `GET /api/v1/planning/step-out-steps/:stepId/sub-steps`

---

### P7-002 — Update StepOutStep Rows for ParentStepId Tree `[EXTEND]`
**File:** `webapp/src/modules/planning/views/StepOutPlanFormView.vue`

**Add:**
- Tree structure: L1 steps (ParentStepId IS NULL) → L2 steps (ParentStepId = L1 stepId)
- Collapse/expand per L1 step
- Visual indent for L2 rows
- Add L2 step button on L1 rows

---

### P7-003 — Duration Hours Input Component `[BUILD NEW]`
**File:** `webapp/src/modules/planning/components/DurationHoursInput.vue`  
**Purpose:** Input that accepts decimal hours (.25, .5, 1.5, 2.0) and displays as "15 min", "30 min", "1h 30m"  
**Acceptance:** .25 → "15 min", .5 → "30 min", 1.0 → "1 hour", 1.5 → "1h 30min"

---

## PHASE 8 — FCO V2 (LABOR LINES + WORKORDER LINK)

### P8-001 — Extend FcoListView / FcoFormView with WorkOrder Link `[EXTEND]`
**File:** `webapp/src/modules/planning/views/FcoListView.vue` + `FcoFormView.vue` (create if needed)

**Add:**
- WorkOrder selector field (dropdown of WOs for the linked estimate)
- Required validation: submit blocked if LinkedWorkOrderId IS NULL

---

### P8-002 — Build FcoLaborLines Tab in FCO Form `[BUILD NEW]`
**File:** `webapp/src/modules/planning/views/FcoFormView.vue` or FCO detail component

**Add:**
- Labor Lines tab
- Position dropdown (populated from estimate's rate book via `GET /api/v1/planning/fco/:id/rate-book-positions`)
- Hours inputs: ST, OT, DT (decimal, 2 places)
- Rates: auto-filled from rate book on position selection; display-only
- Subtotal: calculated inline (StHours × StRate + etc.)
- Total FCO Amount: sum of all line subtotals
- Add / Delete labor line
- Note: replaces manual entry into old LaborBreakdownJson; old JSON field not shown to user

---

## PHASE 9 — ACTUALS

### P9-001 — Build ActualEntryView (per WorkOrder) `[BUILD NEW]`
**File:** `webapp/src/modules/planning/views/ActualEntryView.vue`  
**Layout:** WO header, filter by type/date, table of entries, + Log Entry button  
**Entry form:** Type dropdown, date, position/craft (for labor), hours, cost, billable, notes  
**Actions:** Confirm entry (lock), Edit (only if unconfirmed), Delete (only if unconfirmed)

---

### P9-002 — Build VarianceDashboardView `[BUILD NEW]`
**File:** `webapp/src/modules/planning/views/VarianceDashboardView.vue`  
**Layout:** Filter by project/estimate, Summary KPIs (estimated vs actual cost, margin), WO breakdown table, FCO breakdown table  
**Data:** `GET /api/v1/variance/project/:id`

---

## PHASE 10 — GANTT + CALENDAR (CUSTOM SVG)

### P10-001 — Build GanttChart Component `[BUILD NEW]`
**File:** `webapp/src/modules/planning/components/GanttChart.vue`  
**Architecture:** SVG-based, no third-party library. See PROJECT_PLANNING_UI_MAP.md for full spec.  
**Sub-components:** GanttBar, GanttMilestoneDiamond, GanttDependencyArrow, GanttDateRuler, GanttPhaseRow, GanttTaskRow  
**Composable:** `webapp/src/modules/planning/composables/useGanttLayout.ts`

---

### P10-002 — Build ProjectGanttView `[BUILD NEW]`
**File:** `webapp/src/modules/planning/views/ProjectGanttView.vue`  
**Wraps:** GanttChart with project data, filters, zoom, baseline toggle

---

### P10-003 — Extract useCalendarGrid Composable `[EXTEND]`
**File:** `webapp/src/shared/composables/useCalendarGrid.ts`  
**Extract from:** `webapp/src/modules/estimating/features/analytics/views/CalendarView.vue`  
**Verify:** Estimating calendar still works after extraction

---

### P10-004 — Build ProjectCalendarView `[BUILD NEW]`
**File:** `webapp/src/modules/planning/views/ProjectCalendarView.vue`  
**Uses:** `useCalendarGrid` composable, renders PlanTask date ranges as event bars

---

## PHASE 11 — HEALTH DASHBOARD + TRACEABILITY

### P11-001 — Build ScheduleHealthView `[BUILD NEW]`
**File:** `webapp/src/modules/planning/views/ScheduleHealthView.vue`  
**Layout:** Summary banner (X on track, Y at risk, Z behind), project table with health pills, critical tasks section

---

### P11-002 — Build TraceabilityView `[BUILD NEW]`
**File:** `webapp/src/modules/planning/views/TraceabilityView.vue`  
**Layout:** Estimate → WO → Task matrix, broken links section, delayed work by estimate

---

### P11-003 — Build MilestoneView `[BUILD NEW]`
**File:** `webapp/src/modules/planning/views/MilestoneView.vue`  
**Layout:** Filter by project/status, milestone table with slippage days, achieve/miss actions

---

## PHASE 12 — DEMO SEED DATA

### P12-001 — Add ProjectLifecycle Seed to DevController `[EXTEND]`
**File:** `Api/Controllers/DevController.cs`  
**Endpoint:** `POST /api/v1/dev/seed-project-lifecycle`

**Seed Project A:** Valero Pasadena Turnaround — Active, At Risk, FCO, missed milestone, actuals partial  
**Seed Project B:** Cat-Spec Reactor Catalyst Changeout — Planning, On Track, good baseline, no FCO  
**Seed Project C:** Tank 14 Inspection and Repair — Closed, full actuals, 2 FCOs, variance visible

**Full seed requirements:** See `PROJECT_ACTUALS_AND_VARIANCE_MODEL.md` — Demo Seed section.  
**Idempotent:** Check before insert; seeding twice must not create duplicates.

---

## PHASE 13 — QA + REGRESSION

### P13-001 — Backend Smoke Tests
- All new controllers return 200/201 with demo data
- WorkOrder release gate returns 422 when CommAuth not Active
- FCO submit returns 422 when LinkedWorkOrderId missing
- ActualEntry creation validates WorkOrderId
- Traceability broken-links endpoint returns orphan StepOutPlans

### P13-002 — Frontend Smoke Tests
- Portal "Projects at Risk" KPI shows correct count
- Project list loads with health pills
- Project plan view: phase list, Gantt tab, Milestones tab
- Work Order release shows gate status; release button fires correctly
- FCO form: WorkOrder required; labor line rate auto-fill works
- Step-Out form: L1 → L2 tree + L3 substep leaf
- Duration .25 displays as "15 min"
- Actuals entry saves and shows in variance dashboard

### P13-003 — Regression: Estimating Module Unaffected
- Estimates, staffing plans, rate books, cost books, analytics, calendar all load
- No regressions in QA-EST, QA-SP, QA-AN, QA-RCB lanes
- NSwag sync verified after new controllers

### P13-004 — Update QA_REGRESSION_CHECKLIST.md `[EXTEND]`
Add new gates:
- QA-COMM: CommercialAuthorization gates (5 items)
- QA-PROJ: Project lifecycle (10 items)
- QA-WO: WorkOrder release and execution (8 items)
- QA-STEP: StepOut hierarchy (substep, duration) (6 items)
- QA-FCO2: FCO v2 with labor lines and WO link (8 items)
- QA-ACT: Actuals entry and rollup (8 items)
- QA-VAR: Variance reporting (6 items)
- QA-HEALTH: Schedule health (6 items)

---

## PHASE 14 — SCHEDULING PERSONNEL EXPANSION

> **All Scheduling personnel tasks have moved to their own dedicated document.**
> See `docs/PROJECT_SCHEDULING_PERSONNEL_TODO.md` for the full task list (Phases S1–S9).
> That document is the authoritative TODO for Resource Master, Craft/Cert reference tables, CSV import, demand state model, Demand Summary Drawer, and Assignment Modal.

The tasks below (P14-001 through P14-020) are superseded by `PROJECT_SCHEDULING_PERSONNEL_TODO.md`. They are retained here for reference only and must not be treated as implementation guidance.

---

### ~~P14-001 through P14-020~~ (Superseded — see PROJECT_SCHEDULING_PERSONNEL_TODO.md)

These tasks extend the existing Scheduling module (which already has basic views) to match the locked business requirements. Scheduling owns these entities — Planning does not touch them.

### P14-001 — Expand Resource Model `[EXTEND]`
**File:** `Data/Models/Resource.cs` (verify existing fields; add missing)

**Required fields (add if missing):**
- `EmployeeId` (string, nullable — imported from HR/AD systems)
- `FirstName` (string, required)
- `LastName` (string, required)
- `DisplayName` (computed: FirstName + " " + LastName; store if needed for search)
- `PrimaryCraftId` (FK → Crafts, required)
- `Region` (string, nullable)
- `Branch` (string, nullable — Yard/HomeLocation)
- `EmploymentStatus` (enum: Active | Inactive | OnLeave | Terminated; default Active)
- `ShiftEligibility` (enum: Day | Night | Rotating | Any; default Any)
- `Phone` (string, nullable)
- `Email` (string, nullable)
- `Notes` (string, nullable)

**Navigation:** HasMany<ResourceCraft>, HasMany<ResourceCertification>, HasMany<AvailabilityBlock>

**Acceptance:** Compiles; FK to Crafts with Restrict; EmploymentStatus default Active; migration clean

---

### P14-002 — Create Craft Reference Table `[BUILD NEW]`
**File:** `Data/Models/Craft.cs`

**Fields:** CraftId, CompanyCode, CraftCode (e.g. "PP", "WL", "RI"), Name, Description?, IsActive (bool default true), CreatedAt, UpdatedAt

**Unique index:** CompanyCode + CraftCode

**Acceptance:** Compiles; AdminController or CraftController exposes CRUD; seed with common craft codes

---

### P14-003 — Create Certification Reference Table `[BUILD NEW]`
**File:** `Data/Models/Certification.cs`

**Fields:** CertificationId, CompanyCode, CertCode, Name, Description?, DefaultExpirationMonths (int?), IsActive (bool default true), CreatedAt, UpdatedAt

**Acceptance:** Compiles; AdminController or CertificationController exposes CRUD

---

### P14-004 — Create ResourceCraft Junction (SecondaryCrafts) `[BUILD NEW]`
**File:** `Data/Models/ResourceCraft.cs`

**Fields:** ResourceCraftId, ResourceId (FK → Resources Cascade), CraftId (FK → Crafts Restrict), IsPrimary (bool), CreatedAt

**Note:** PrimaryCraft FK on Resource still exists for fast filtering; SecondaryCrafts are in this junction. Both must be consistent.

**Acceptance:** Compiles; unique index ResourceId+CraftId; ResourceController returns SecondaryCrafts list

---

### P14-005 — Create ResourceCertification Junction `[BUILD NEW]`
**File:** `Data/Models/ResourceCertification.cs`

**Fields:** ResourceCertificationId, ResourceId (FK → Resources Cascade), CertificationId (FK → Certifications Restrict), IssueDate?, ExpirationDate?, Notes?, CreatedAt

**Acceptance:** Compiles; unique index ResourceId+CertificationId; expiration check used in assignment filtering

---

### P14-006 — Add CraftId to Assignment Model `[EXTEND]`
**File:** `Data/Models/Assignment.cs`

**Add:** `CraftId` (FK → Crafts, required — no free-text craft on assignments)

**Also verify Assignment has:** DemandSourceType (enum: Estimate | StaffingPlan | WorkPackage), DemandSourceId (int), StartDate, EndDate, ShiftType, Notes, Status

**Acceptance:** Migration clean; assignment creation requires CraftId; no free-text craft field exposed in API

---

### P14-007 — Create CraftController `[BUILD NEW]`
**File:** `Api/Controllers/CraftController.cs`  
**Route:** `api/v1/scheduling/crafts`

**Endpoints:** `GET /` (list for company), `POST /`, `PUT /:id`, `DELETE /:id` (soft delete — set IsActive=false)

---

### P14-008 — Create CertificationController `[BUILD NEW]`
**File:** `Api/Controllers/CertificationController.cs`  
**Route:** `api/v1/scheduling/certifications`

**Endpoints:** `GET /`, `POST /`, `PUT /:id`, `DELETE /:id` (soft delete)

---

### P14-009 — Extend ResourceController with Full Fields `[EXTEND]`
**File:** `Api/Controllers/SchedulingController.cs` (or ResourceController.cs)

**Update create/update endpoints** to accept and persist all new fields (EmployeeId, FirstName, LastName, PrimaryCraftId, Region, Branch, EmploymentStatus, ShiftEligibility, Phone, Email, Notes, SecondaryCrafts list, Certifications list).

**Add filter parameters to list endpoint:** craft, region, branch, status, certExpiring (days), availableFrom/To

---

### P14-010 — Create Resource CSV Import Endpoint `[BUILD NEW]`
**File:** `Api/Controllers/SchedulingController.cs` (or ImportController.cs)

**Endpoints:**
- `POST /api/v1/scheduling/resources/import/validate` — accepts multipart CSV, returns validation results (error rows, unknown craft codes, missing required fields, duplicate EmployeeIds)
- `POST /api/v1/scheduling/resources/import` — upsert: EmployeeId match → update; no match → create

**CSV Template columns:** EmployeeId, FirstName, LastName, CraftCode, SecondaryCraftCodes (comma-separated), Region, Branch, EmploymentStatus, ShiftEligibility, Phone, Email, Certs (pipe-separated "NCCER|2027-06")

**Acceptance:** 50-row CSV round-trips cleanly; unknown CraftCode returns error row, not 500; duplicate EmployeeId updates, not duplicates

---

### P14-011 — Extend AssignmentController: Craft-Filtered Resource Lookup `[EXTEND]`
**File:** `Api/Controllers/SchedulingController.cs`

**Add endpoint:** `GET /api/v1/scheduling/resources/available`

**Parameters:** craftId (required), startDate, endDate, region?, branch?, demandSourceType?, demandSourceId?

**Returns:** Resources matching craft (Primary OR Secondary), with no conflicting AvailabilityBlock for the date range, with no conflicting Assignment for the date range. Each result includes conflict summary if partial overlap exists.

**Acceptance:** Craft filter required — endpoint rejects requests without craftId; results contain conflict flag per resource

---

### P14-012 — Run NSwag for New Scheduling Controllers `[REUSE]`
Run NSwag after P14-007 through P14-011 are implemented.

New client classes expected:
- `CraftsClient`
- `CertificationsClient`
- Updated `SchedulingClient` (resources/import endpoints)

---

### P14-013 — Update ResourcesBoardView for Full Resource Fields `[EXTEND]`
**File:** `webapp/src/modules/scheduling/views/ResourcesBoardView.vue`

**Add columns:** Employee ID, Primary Craft, Region, Branch, Status, Shift, Cert count  
**Add filters:** Craft (dropdown from Craft table), Region, Status, Cert expiring within (days)  
**Add action:** [Import] button → navigate to ResourceImportView  
**Add action:** [+ New Resource] → navigate to ResourceFormView

---

### P14-014 — Build ResourceFormView `[BUILD NEW]`
**File:** `webapp/src/modules/scheduling/views/ResourceFormView.vue`  
**Spec:** See PROJECT_PLANNING_UI_MAP.md — Resource Master Form section  
**Routes:** `/scheduling/resources/new` and `/scheduling/resources/:id`

---

### P14-015 — Build ResourceImportView `[BUILD NEW]`
**File:** `webapp/src/modules/scheduling/views/ResourceImportView.vue`  
**Spec:** See PROJECT_PLANNING_UI_MAP.md — Resource Import View section  
**Route:** `/scheduling/resources/import`

---

### P14-016 — Build Demand Summary Drawer (JobsBoardView) `[EXTEND]`
**File:** `webapp/src/modules/scheduling/views/JobsBoardView.vue`  
**File:** `webapp/src/modules/scheduling/components/DemandSummaryDrawer.vue` (new)

**Behavior:** Double-click on jobs board row opens DemandSummaryDrawer (right-side overlay panel). Drawer is read-only. Shows all fields per PROJECT_PLANNING_UI_MAP.md → Jobs Board — Demand Summary Drawer spec. Assign button inside drawer opens AssignmentModal.

---

### P14-017 — Build Craft-Controlled AssignmentModal `[EXTEND → BUILD NEW COMPONENT]`
**File:** `webapp/src/modules/scheduling/components/AssignmentModal.vue` (replace or build new)

**Behavior:** Craft dropdown (not free text). Resource list filtered by craft/certs/dates/region. Conflicts shown inline. Job context auto-populated from demand row. On save: POST assignment with CraftId. See PROJECT_PLANNING_UI_MAP.md → Assignment Modal spec.

---

### P14-018 — Scheduling Router Update `[EXTEND]`
**File:** `webapp/src/modules/scheduling/router/index.ts` (or scheduling routes in main router)

**Add routes:**
```
/scheduling/resources/new       → ResourceFormView
/scheduling/resources/import    → ResourceImportView
/scheduling/resources/:id       → ResourceFormView
```

---

### P14-019 — Add Craft/Cert Seed Data `[EXTEND]`
**File:** `Data/DBInitializer.cs` or `Data/ReferenceDataSeeder.cs`

**Seed common craft codes:** PP (Pipefitter), WL (Welder), RI (Rigger), SC (Scaffold Builder), IN (Instrument Tech), EL (Electrician), OL (Operator), HL (Helper/Laborer), CR (Crane Operator)

**Seed common certs:** NCCER Core, H2S Alive, OSHA 10, OSHA 30, Crane Operator, Rigging Level 1

**Idempotent:** Check CraftCode existence before insert.

---

### P14-020 — Future: AD Sync Integration Path (Planning Note, Not Implementation)

**Note:** When AD/HR sync is implemented, it will POST to the same `/api/v1/scheduling/resources/import` endpoint (or a dedicated `/api/v1/scheduling/resources/sync` variant). Module ownership does not change — Scheduling still owns the Resource entity. AD sync is an automated import source, not a separate module. No new module or ownership boundary is created.

This task is a placeholder to document the integration path. Implementation is deferred.

---

## IMPACTED FILES SUMMARY

### Backend — New Files
| File | Purpose |
|------|---------|
| `Data/Models/CommercialAuthorization.cs` | NEW |
| `Data/Models/Project.cs` | NEW |
| `Data/Models/WorkOrder.cs` | NEW |
| `Data/Models/ProjectPhase.cs` | NEW or verify |
| `Data/Models/PlanTask.cs` | NEW or verify |
| `Data/Models/Milestone.cs` | NEW or verify |
| `Data/Models/TaskDependency.cs` | NEW or verify |
| `Data/Models/EstimateTaskLink.cs` | NEW or verify |
| `Data/Models/FcoTaskLink.cs` | NEW or verify |
| `Data/Models/TimelineBaseline.cs` | NEW or verify |
| `Data/Models/TaskProgressSnapshot.cs` | NEW or verify |
| `Data/Models/StepOutSubStep.cs` | NEW |
| `Data/Models/FcoLaborLine.cs` | NEW |
| `Data/Models/ActualEntry.cs` | NEW |
| `Api/Controllers/CommercialAuthorizationController.cs` | NEW |
| `Api/Controllers/ProjectController.cs` | NEW |
| `Api/Controllers/WorkOrderController.cs` | NEW |
| `Api/Controllers/ProjectPhaseController.cs` | NEW |
| `Api/Controllers/PlanTaskController.cs` | NEW |
| `Api/Controllers/MilestoneController.cs` | NEW |
| `Api/Controllers/ActualEntryController.cs` | NEW |
| `Api/Controllers/ScheduleHealthController.cs` | NEW |
| `Api/Controllers/VarianceController.cs` | NEW |
| `Api/Controllers/TraceabilityController.cs` | NEW |
| `Api/Services/ScheduleHealthService.cs` | NEW |
| `Api/Services/VarianceService.cs` | NEW |

### Backend — Extended Files
| File | Change |
|------|--------|
| `Data/Models/StepOutStep.cs` | Add ParentStepId, DurationHours |
| `Data/Models/StepOutPlan.cs` | Add WorkOrderId |
| `Data/Models/FcoDocument.cs` | Add LinkedWorkOrderId |
| `Data/Models/WorkPackage.cs` | Add WorkOrderId |
| `Data/AppDbContext.cs` | 14 new DbSets + Fluent API |
| `Api/Controllers/PlanningController.cs` | Add StepOutSubStep + FcoLaborLine endpoints |
| `Api/Controllers/PortalController.cs` | Add planning KPIs, replace FCO count |
| `Api/Controllers/DevController.cs` | Add lifecycle seed |

### Frontend — New Files
| File | Purpose |
|------|---------|
| `webapp/src/modules/planning/views/ProjectListView.vue` | Project list |
| `webapp/src/modules/planning/views/ProjectDetailView.vue` | Project detail shell |
| `webapp/src/modules/planning/views/WorkOrderListView.vue` | WO list per project |
| `webapp/src/modules/planning/views/WorkOrderDetailView.vue` | WO detail |
| `webapp/src/modules/planning/views/CommercialAuthorizationView.vue` | Auth management |
| `webapp/src/modules/planning/views/MilestoneView.vue` | Milestone tracker |
| `webapp/src/modules/planning/views/ScheduleHealthView.vue` | Health dashboard |
| `webapp/src/modules/planning/views/TraceabilityView.vue` | Traceability matrix |
| `webapp/src/modules/planning/views/ActualEntryView.vue` | Actuals per WO |
| `webapp/src/modules/planning/views/VarianceDashboardView.vue` | Variance summary |
| `webapp/src/modules/planning/views/ProjectGanttView.vue` | Gantt chart |
| `webapp/src/modules/planning/views/ProjectCalendarView.vue` | Calendar view |
| `webapp/src/modules/planning/components/GanttChart.vue` | Custom SVG Gantt |
| `webapp/src/modules/planning/components/GanttBar.vue` | Gantt bar |
| `webapp/src/modules/planning/components/GanttMilestoneDiamond.vue` | Milestone marker |
| `webapp/src/modules/planning/components/GanttDependencyArrow.vue` | Dep arrow |
| `webapp/src/modules/planning/components/GanttDateRuler.vue` | Date column header |
| `webapp/src/modules/planning/components/DurationHoursInput.vue` | Decimal hours input |
| `webapp/src/modules/planning/stores/planningStore.ts` | Planning Pinia store |
| `webapp/src/modules/planning/composables/useGanttLayout.ts` | Gantt layout logic |
| `webapp/src/shared/composables/useCalendarGrid.ts` | Shared calendar grid |

### Frontend — Extended Files
| File | Change |
|------|--------|
| `webapp/src/modules/planning/router/index.ts` | 12 new routes |
| `webapp/src/apps.ts` | Expanded planning menu |
| `webapp/src/modules/planning/views/StepOutPlanFormView.vue` | SubStep L3 + ParentStep tree |
| `webapp/src/modules/planning/views/FcoListView.vue` | WorkOrder link field |
| `webapp/src/modules/portal/views/PortalDashboardView.vue` | Projects at Risk KPI |
| `webapp/src/modules/scheduling/views/ResourcesBoardView.vue` | Full resource fields, craft/region filters, Import + New buttons |
| `webapp/src/modules/scheduling/views/JobsBoardView.vue` | Double-click → DemandSummaryDrawer |

### Phase 14 — Scheduling Model Expansion — New Files
| File | Purpose |
|------|---------|
| `Data/Models/Craft.cs` | Craft reference table |
| `Data/Models/Certification.cs` | Certification reference table |
| `Data/Models/ResourceCraft.cs` | ResourceCraft junction (secondary crafts) |
| `Data/Models/ResourceCertification.cs` | ResourceCertification junction with expiration |
| `Api/Controllers/CraftController.cs` | CRUD for crafts |
| `Api/Controllers/CertificationController.cs` | CRUD for certifications |
| `webapp/src/modules/scheduling/views/ResourceFormView.vue` | Create/edit resource |
| `webapp/src/modules/scheduling/views/ResourceImportView.vue` | CSV import |
| `webapp/src/modules/scheduling/components/DemandSummaryDrawer.vue` | Read-only demand summary |
| `webapp/src/modules/scheduling/components/AssignmentModal.vue` | Craft-controlled assignment modal |
