# PROJECT SCHEDULING PERSONNEL TODO
## Stronghold Enterprise — Scheduling Module Implementation Tasks
**Document Version:** 1.0
**Date:** 2026-04-28
**Status:** APPROVED — Implementation Pending
**Spec Reference:** `docs/PROJECT_SCHEDULING_PERSONNEL_SPEC.md`

Tags: `[REUSE]` `[EXTEND]` `[BUILD NEW]`

---

## DEPENDENCY ORDER

```
Phase S1 (Data Models — Resource Expansion + Reference Tables)
  → Phase S2 (Migration)
  → Phase S3 (Backend Controllers + Endpoints)
  → Phase S4 (NSwag Sync)
  → Phase S5 (Frontend — Resource Master)
  → Phase S6 (Frontend — Jobs Board + Demand State)
  → Phase S7 (Frontend — Demand Summary Drawer + Assignment Modal)
  → Phase S8 (Seed Data)
  → Phase S9 (QA + Regression)
```

No frontend work starts before Phase S4 (NSwag client regenerated).
No assignment modal work starts before S5 (craft dropdown needs Craft reference data in place).

---

## ESTIMATING BOUNDARY

**All tasks in this document: Estimating = NO.**

No task here touches `webapp/src/modules/estimating/`, existing Estimate controllers, StaffingPlan controllers, or any existing Estimate/StaffingPlan model. Scheduling reads Estimate and StaffingPlan records as demand sources — it does not write to them.

If any task below turns out to require touching Estimating code, STOP and report to Joseph with:
1. Why the change is necessary
2. Exact file(s) affected
3. Change type (read-only / contract expansion / schema change / UI change / calculation change)
4. Regression risk (low / medium / high)
5. Safest alternative

---

## PHASE S1 — DATA MODELS

### S1-001 — Craft Reference Table `[BUILD NEW]`
**File:** `Data/Models/Craft.cs`

**Fields:** CraftId (int PK), CompanyCode (string), CraftCode (string, unique per company), Name (string), Description (string?), IsActive (bool default true), CreatedAt, UpdatedAt

**Unique index:** CompanyCode + CraftCode

**Acceptance:** Compiles; unique index applied; FK from Resource.PrimaryCraftId → Crafts.CraftId with OnDelete Restrict

---

### S1-002 — Certification Reference Table `[BUILD NEW]`
**File:** `Data/Models/Certification.cs`

**Fields:** CertificationId (int PK), CompanyCode (string), CertCode (string, unique per company), Name (string), Description (string?), DefaultExpirationMonths (int?), IsActive (bool default true), CreatedAt, UpdatedAt

**Unique index:** CompanyCode + CertCode

**Acceptance:** Compiles; unique index applied

---

### S1-003 — Expand Resource Model `[EXTEND]`
**File:** `Data/Models/Resource.cs`

**Add or verify fields:**
- `EmployeeId` (string?, nullable, indexed per company)
- `FirstName` (string, required)
- `LastName` (string, required)
- `PrimaryCraftId` (int, FK → Crafts, required, OnDelete Restrict)
- `Region` (string?, nullable)
- `Branch` (string?, nullable)
- `EmploymentStatus` (enum: Active|Inactive|OnLeave|Terminated, default Active)
- `ShiftEligibility` (enum: Day|Night|Rotating|Any, default Any)
- `Phone` (string?, nullable)
- `Email` (string?, nullable)
- `Notes` (string?, nullable)

**Navigation:** HasMany<ResourceCraft>(Cascade), HasMany<ResourceCertification>(Cascade), HasMany<AvailabilityBlock>

**Acceptance:** Compiles; FK to Crafts; EmploymentStatus default Active; no breaking change to existing columns

---

### S1-004 — ResourceCraft Junction (Secondary Crafts) `[BUILD NEW]`
**File:** `Data/Models/ResourceCraft.cs`

**Fields:** ResourceCraftId (int PK), ResourceId (int FK→Resources Cascade), CraftId (int FK→Crafts Restrict), IsPrimary (bool default false), CreatedAt

**Unique index:** ResourceId + CraftId

**Note:** PrimaryCraftId on Resource remains for fast filtering. SecondaryCrafts live in this junction with IsPrimary = false. When a resource's primary craft is set, a corresponding ResourceCraft row with IsPrimary = true may also exist for consistency — or just rely on the FK column directly. Confirm approach in implementation.

**Acceptance:** Compiles; unique index applied

---

### S1-005 — ResourceCertification Junction `[BUILD NEW]`
**File:** `Data/Models/ResourceCertification.cs`

**Fields:** ResourceCertificationId (int PK), ResourceId (int FK→Resources Cascade), CertificationId (int FK→Certifications Restrict), IssueDate (DateOnly?), ExpirationDate (DateOnly?), Notes (string?), CreatedAt

**Unique index:** ResourceId + CertificationId

**Acceptance:** Compiles; expiration date queryable for "certs expiring within N days" filter

---

### S1-006 — Add CraftId to Assignment `[EXTEND]`
**File:** `Data/Models/Assignment.cs`

**Add:** `CraftId` (int, FK → Crafts, required, OnDelete Restrict)

**Also verify Assignment has:**
- `DemandSourceType` (string or enum: Estimate | StaffingPlan | WorkPackage)
- `DemandSourceId` (int)
- `StartDate` (DateOnly)
- `EndDate` (DateOnly)
- `ShiftType` (string or enum: Day | Night | Rotating)
- `Notes` (string?)
- `Status` (string or enum: Active | Cancelled)

**Acceptance:** Migration clean; CraftId NOT NULL; no existing assignment data lost (migration must handle any existing rows if table has data)

---

## PHASE S2 — MIGRATION

### S2-001 — Generate and Apply Migration `[BUILD NEW]`
**Command:** `cd Data && dotnet ef migrations add AddSchedulingPersonnelModel --startup-project ../Api`

**Migration must:**
- Create `Crafts` table
- Create `Certifications` table
- Create `ResourceCrafts` junction table
- Create `ResourceCertifications` junction table
- Add columns to `Resources` table (EmployeeId, FirstName, LastName, PrimaryCraftId, Region, Branch, EmploymentStatus, ShiftEligibility, Phone, Email, Notes)
- Add `CraftId` column to `Assignments` table

**Acceptance:** `dotnet ef migrations list` shows migration as applied; `dotnet build` succeeds; no existing data lost

---

## PHASE S3 — BACKEND CONTROLLERS AND ENDPOINTS

### S3-001 — Create CraftController `[BUILD NEW]`
**File:** `Api/Controllers/CraftController.cs`
**Route prefix:** `api/v1/scheduling/crafts`

**Endpoints:**
- `GET /` — list crafts for company (IsActive = true by default; `?includeInactive=true` to see all)
- `POST /` — create craft (CraftCode + Name required; unique per company)
- `PUT /:id` — update craft
- `DELETE /:id` — soft delete (sets IsActive = false); blocked if any Resource references this CraftId

**Acceptance:** CRUD round-trips; soft delete returns 200; hard delete of referenced craft returns 409

---

### S3-002 — Create CertificationController `[BUILD NEW]`
**File:** `Api/Controllers/CertificationController.cs`
**Route prefix:** `api/v1/scheduling/certifications`

**Endpoints:**
- `GET /` — list certs for company
- `POST /` — create cert
- `PUT /:id` — update cert
- `DELETE /:id` — soft delete; blocked if any ResourceCertification references this CertId

**Acceptance:** CRUD round-trips; soft delete works

---

### S3-003 — Extend ResourceController (Full Fields + Filtering) `[EXTEND]`
**File:** `Api/Controllers/SchedulingController.cs` (or ResourceController.cs — wherever Resources CRUD lives)
**Route prefix:** `api/v1/scheduling/resources`

**Update create/update endpoints** to accept and persist all new fields:
- EmployeeId, FirstName, LastName, PrimaryCraftId, Region, Branch, EmploymentStatus, ShiftEligibility, Phone, Email, Notes
- SecondaryCrafts: `[ { "craftId": 3 }, { "craftId": 5 } ]` — replace junction rows on update
- Certifications: `[ { "certificationId": 2, "expirationDate": "2027-06" } ]` — replace junction rows on update

**Update list endpoint filters:**
- `?craftId=X` — PrimaryCraftId = X OR ResourceCraft.CraftId = X
- `?region=X`
- `?branch=X`
- `?status=Active` (default) | `?status=all`
- `?certExpiringDays=30` — resources with any cert expiring within N days
- `?availableFrom=2026-04-15&availableTo=2026-06-15` — excludes resources with full AvailabilityBlock overlap

**Acceptance:** All filters work independently and in combination; SecondaryCrafts and Certifications return as nested arrays in GET response

---

### S3-004 — Create Resource Import Endpoint `[BUILD NEW]`
**File:** `Api/Controllers/SchedulingController.cs` (or ImportController.cs)

**Endpoints:**
- `GET /api/v1/scheduling/resources/import/template` — returns CSV template file
- `POST /api/v1/scheduling/resources/import/validate` — multipart CSV; returns validation results
- `POST /api/v1/scheduling/resources/import` — upsert by EmployeeId

**Upsert logic:**
- EmployeeId match on same CompanyCode → update
- No match → create
- SecondaryCrafts and Certifications replaced (not appended) per import row
- Duplicate EmployeeId within same import file → error row (row-level, not 500)

**CSV columns:** EmployeeId, FirstName, LastName, CraftCode, SecondaryCraftCodes, Region, Branch, EmploymentStatus, ShiftEligibility, Phone, Email, Certs

**Acceptance:** 50-row CSV round-trips; unknown CraftCode → row error not 500; duplicate EmployeeId within file → row error; blank EmployeeId rows create new records each time

---

### S3-005 — Create Available Resources Endpoint `[BUILD NEW]`
**File:** `Api/Controllers/SchedulingController.cs`
**Route:** `GET /api/v1/scheduling/resources/available`

**Required parameters:** `craftId` (int, required), `startDate`, `endDate`

**Optional parameters:** `region`, `branch`, `demandSourceType`, `demandSourceId`

**Filtering rules (in order):**
1. PrimaryCraftId = craftId OR ResourceCraft.CraftId = craftId
2. EmploymentStatus = Active
3. No AvailabilityBlock where BlockStart ≤ endDate AND BlockEnd ≥ startDate (full conflict → exclude)
4. No Assignment where ExistingStart ≤ endDate AND ExistingEnd ≥ startDate AND Status ≠ Cancelled (full conflict → exclude)

**Partial conflict:** Resources with partial overlap (not full exclusion) are included in results with `hasPartialConflict: true` and `conflictSummary: [...]`.

**Response per resource:**
```json
{
  "resourceId": 42,
  "displayName": "John Martinez",
  "primaryCraft": "Pipefitter",
  "region": "Gulf Coast",
  "certifications": ["NCCER", "H2S"],
  "hasPartialConflict": false,
  "conflictSummary": []
}
```

**Hard rule:** craftId is required. 400 if missing.

**Acceptance:** Returns empty array (not error) when no resources match; craftId missing returns 400; results exclude full conflicts; partial conflicts flagged

---

### S3-006 — Update Assignment Create Endpoint: Demand State Validation `[EXTEND]`
**File:** `Api/Controllers/SchedulingController.cs` (or AssignmentController.cs)
**Route:** `POST /api/v1/scheduling/assignments`

**Add validation before creating assignment:**
1. If `DemandSourceType = 'Estimate'` or `DemandSourceType = 'StaffingPlan'` → HTTP 422 `DemandNotReleased` (Forecast demand — not assignable)
2. If `DemandSourceType = 'WorkPackage'`:
   - Load WorkPackage where WorkPackageId = DemandSourceId
   - If `WorkPackage.ReadyForScheduling = false` → HTTP 422 `DemandNotReleased`
   - Load backing WorkOrder via WorkPackage.WorkOrderId
   - If `WorkOrder.Status NOT IN ('Released', 'InProgress')` → HTTP 422 `DemandNotReleased`
3. Validate `CraftId != null` and Craft.IsActive = true → else HTTP 400 `CraftRequired`
4. Validate resource availability (no full AvailabilityBlock conflict) → else HTTP 409 `ResourceUnavailable`
5. Validate no full Assignment conflict → else HTTP 409 `ResourceAlreadyAssigned`
6. Validate Resource.EmploymentStatus = Active → else HTTP 422 `ResourceNotActive`

**Acceptance:** All six validation rules tested with unit or integration tests; 422 responses include machine-readable reason code

---

### S3-007 — Update Jobs Board Demand Endpoint: Add Demand State `[EXTEND]`
**File:** `Api/Controllers/SchedulingController.cs`
**Route:** `GET /api/v1/scheduling/jobs`

**Add `demandState` field to each row response:**
```json
{
  "demandState": "Released",  // or "Forecast"
  "isAssignable": true,
  ...
}
```

**Demand state calculation:**
- Source = Estimate or StaffingPlan → `demandState = "Forecast"`, `isAssignable = false`
- Source = WorkPackage AND ReadyForScheduling = true AND WorkOrder.Status IN ('Released', 'InProgress') → `demandState = "Released"`, `isAssignable = true`
- Source = WorkPackage AND any condition fails → `demandState = "Forecast"`, `isAssignable = false`

**Add `demandState` filter parameter:** `?demandState=All` (default) | `Forecast` | `Released`

**Acceptance:** Both demand states appear in response; filter parameter returns correct subset; isAssignable reflects server-side truth

---

## PHASE S4 — NSWAG SYNC

### S4-001 — Run NSwag After S3 Controllers Are Built `[REUSE]`

**Steps:**
1. `cd Api && dotnet build`
2. Start API on local profile
3. `cd Api && nswag run nswag.json /variables:Configuration=Debug`
4. Verify `webapp/src/apiclient/client.ts` has new client classes
5. `cd webapp && npm run build` — 0 TypeScript errors

**New client classes expected:**
- `CraftsClient`
- `CertificationsClient`
- `SchedulingResourcesClient` (or updated SchedulingClient)

**Acceptance:** TypeScript build passes; new endpoint methods visible in client.ts

---

## PHASE S5 — FRONTEND: RESOURCE MASTER

### S5-001 — Update ResourcesBoardView (Full Fields + Filters) `[EXTEND]`
**File:** `webapp/src/modules/scheduling/views/ResourcesBoardView.vue`

**Add columns:** Employee ID, Primary Craft, Region, Branch, Status badge, Shift, Cert count
**Add filters:** Craft (dropdown from GET /crafts), Region, Status, Cert expiring within (days dropdown)
**Add header buttons:** [+ New Resource] → navigate to `/scheduling/resources/new`; [Import] → navigate to `/scheduling/resources/import`
**Keep:** existing table structure, pagination, search

**Acceptance:** Craft filter populates from API; status filter works; existing resources still display

---

### S5-002 — Build ResourceFormView `[BUILD NEW]`
**File:** `webapp/src/modules/scheduling/views/ResourceFormView.vue`
**Routes:** `/scheduling/resources/new` and `/scheduling/resources/:id`

**Layout:** Two-column form. Left: Identity + Craft + Certifications. Right: Scheduling fields + Contact + Notes.

**Fields:**
- EmployeeId (text, optional)
- FirstName, LastName (required)
- EmploymentStatus (dropdown: Active | Inactive | OnLeave | Terminated)
- PrimaryCraft (dropdown, populated from GET /crafts)
- SecondaryCrafts (multi-select chips or add/remove list, populated from GET /crafts)
- Region (text or dropdown — use text if no reference table yet)
- Branch (text or dropdown)
- ShiftEligibility (dropdown: Day | Night | Rotating | Any)
- Certifications table: rows of [Cert dropdown] + [Expiration date optional] + [Remove]
  - [+ Add Certification] button adds a row
- Phone, Email (optional)
- Notes (textarea)

**Actions:** [Save] → POST or PUT; [Cancel] → navigate back to ResourcesBoardView; [Delete] (edit mode) → confirm dialog → DELETE resource

**Acceptance:** Create + edit round-trips; SecondaryCrafts save correctly; Certifications save correctly; form validates required fields before submit

---

### S5-003 — Build ResourceImportView `[BUILD NEW]`
**File:** `webapp/src/modules/scheduling/views/ResourceImportView.vue`
**Route:** `/scheduling/resources/import`

**Layout:** 4-step flow: (1) Download template → (2) Upload file → (3) Column mapping with auto-detection → (4) Validate and Import

**Behavior:**
- Template download: GET /resources/import/template
- Upload: multipart form-data
- Validate: POST /resources/import/validate — shows row-level errors in a table
- Import: POST /resources/import — shows result summary (created / updated / errors)

**Acceptance:** 50-row CSV imports successfully; validation errors show per-row with field and reason; error rows do not block clean rows from importing

---

### S5-004 — Update Scheduling Router `[EXTEND]`
**File:** `webapp/src/modules/scheduling/router/index.ts` (or scheduling routes section in main router)

**Add routes:**
```
/scheduling/resources/new       → ResourceFormView
/scheduling/resources/import    → ResourceImportView
/scheduling/resources/:id       → ResourceFormView
```

**Note:** `/resources/import` must appear before `/resources/:id` in route order to avoid import being caught by the dynamic `:id` segment.

**Acceptance:** All three routes navigate correctly; :id route in edit mode loads resource data

---

## PHASE S6 — FRONTEND: JOBS BOARD DEMAND STATE

### S6-001 — Update JobsBoardView for Demand State `[EXTEND]`
**File:** `webapp/src/modules/scheduling/views/JobsBoardView.vue`

**Changes:**
1. Add `demandState` column (badge: `Forecast` gray, `Released` green)
2. Conditionally disable [Assign] button when `isAssignable = false`
3. When [Assign] is disabled, show tooltip: "WorkOrder not released — assign after WO is released"
4. Add demand state filter to filter bar: [All ▼ | Forecast Only | Released Only]
5. Pass selected filter value to `?demandState=` API parameter

**Acceptance:** Forecast rows show gray badge and disabled Assign; Released rows show green badge and active Assign; filter refreshes table

---

## PHASE S7 — FRONTEND: DEMAND SUMMARY DRAWER + ASSIGNMENT MODAL

### S7-001 — Build DemandSummaryDrawer Component `[BUILD NEW]`
**File:** `webapp/src/modules/scheduling/components/DemandSummaryDrawer.vue`

**Trigger:** Double-click jobs board row (or [View] button)
**Type:** Right-side overlay drawer (`position: fixed; right: 0`); closes on [✕] or Escape key; does not navigate

**Sections:** Header (source badge + ref number + name), Details (project/client/site/status/dates), Authorization (WO + CommAuth + FCO count + Assignable status), Craft Requirements table (Craft | Required | Assigned | Gap)

**Action button:**
- If `isAssignable = true` → `[Assign Resource]` (primary, active) → emits `open-assignment-modal` event
- If `isAssignable = false` → `[Not Released — Assign Disabled]` (disabled, gray)

**API call:** Drawer loads demand detail from `GET /api/v1/scheduling/jobs/:demandSourceType/:demandSourceId` — a new detail endpoint that returns the full demand summary data.

**Acceptance:** Drawer opens on double-click; closes cleanly; displays all sections; assign button correctly enabled/disabled based on isAssignable; Craft Requirements gap column shows correct counts

---

### S7-002 — Build AssignmentModal Component `[BUILD NEW]`
**File:** `webapp/src/modules/scheduling/components/AssignmentModal.vue`

**Type:** PrimeVue `<Dialog>` modal. Not a page navigation.

**Sections:**
1. Job Context (display-only: job name, source type + ref, WO ref, dates)
2. Assignment Details: Craft dropdown (required, from GET /crafts), Start/End dates (default from demand window), Shift dropdown, Notes
3. Available Resources list (loaded from GET /resources/available?craftId=X&startDate=Y&endDate=Z on craft change)
4. Resource list row: Name | Region | Certs (comma list) | Conflicts | radio/select to choose

**Resource list behavior:**
- ● green dot = Fully Available
- ○ yellow dot = Partial Conflict (shown with conflict summary in Conflicts column)
- Full conflicts excluded from list

**Action:** [Create Assignment] → POST /scheduling/assignments → on success: close modal, show toast, refresh drawer's Craft Requirements counts

**Acceptance:** Craft dropdown populated; changing craft re-loads resource list; partial conflicts shown; full conflicts excluded; successful save shows toast and refreshes demand summary

---

### S7-003 — Demand Detail API Endpoint `[BUILD NEW]`
**File:** `Api/Controllers/SchedulingController.cs`
**Route:** `GET /api/v1/scheduling/jobs/{demandSourceType}/{demandSourceId}`

Returns full demand summary:
- All fields from jobs board row
- Project, client, site (from backing estimate or WorkOrder → Project)
- WO number + status, CommAuth number + value
- FCO count (open FCOs on the WorkOrder)
- Craft requirements: list of `{ craftCode, name, requiredQty, assignedQty }` (requiredQty from WorkPackage or Estimate labor rows; assignedQty from Assignments table)
- isAssignable (bool)

**Acceptance:** Returns correct data for Estimate, StaffingPlan, and WorkPackage source types; isAssignable reflects real demand state

---

## PHASE S8 — SEED DATA

### S8-001 — Seed Craft Reference Data `[EXTEND]`
**File:** `Data/DBInitializer.cs` or `Data/ReferenceDataSeeder.cs`

**Seed crafts (idempotent — check CraftCode before insert):**
PP, WL, RI, SC, IN, EL, OL, HL, CR (see full list in SPEC doc Section 9)

**Acceptance:** Seeder runs twice without creating duplicates; crafts visible in GET /crafts response

---

### S8-002 — Seed Certification Reference Data `[EXTEND]`
**File:** `Data/DBInitializer.cs` or `Data/ReferenceDataSeeder.cs`

**Seed certs (idempotent — check CertCode before insert):**
NCCER, H2S, OSHA10, OSHA30, CRANE, RIGGING1 (see full list in SPEC doc Section 9)

**Acceptance:** Seeder runs twice without creating duplicates; certs visible in GET /certifications response

---

### S8-003 — Seed Demo Resources `[EXTEND]`
**File:** `Api/Controllers/DevController.cs`
**Endpoint:** `POST /api/v1/dev/seed-scheduling-personnel`

**Seed 15–20 demo resources** with variety of:
- Crafts (PP, WL, RI, SC)
- Regions (Gulf Coast, Texas, Southeast)
- Branches (Baton Rouge, Houston, Beaumont)
- Certifications (NCCER, H2S, OSHA10)
- A few with PTO availability blocks
- A few with existing assignments (so conflict detection is demonstrable)

**Idempotent:** Check EmployeeId before insert; seed twice = no duplicates.

**Acceptance:** After seeding, ResourcesBoardView shows populated list; available-resources endpoint returns filtered results

---

## PHASE S9 — QA AND REGRESSION

### S9-001 — Backend Validation Tests
- Assignment against Estimate demand → 422 DemandNotReleased
- Assignment against WorkPackage with unreleased WO → 422 DemandNotReleased
- Assignment with null CraftId → 400 CraftRequired
- Assignment against inactive resource → 422 ResourceNotActive
- Assignment with full availability conflict → 409 ResourceUnavailable
- Assignment with full assignment conflict → 409 ResourceAlreadyAssigned
- CSV import with unknown CraftCode → row-level error, not 500
- CSV import with duplicate EmployeeId in file → row-level error

### S9-002 — Frontend Smoke Checks
- JobsBoardView: Forecast rows show gray badge and disabled Assign
- JobsBoardView: Released rows show green badge and active Assign
- Demand state filter changes table content
- DemandSummaryDrawer opens on double-click; closes on ✕
- DemandSummaryDrawer: Forecast row shows "Not Released — Assign Disabled"
- DemandSummaryDrawer: Released row shows active Assign button
- AssignmentModal: craft dropdown populates from Craft table
- AssignmentModal: changing craft re-loads resource list
- AssignmentModal: partial conflicts shown with ○ indicator
- ResourceFormView: create → edit → delete round-trip
- ResourceImportView: template downloads; 50-row CSV imports cleanly

### S9-003 — Regression: Estimating Unaffected
- Estimates module loads and behaves identically after all scheduling migrations
- No Estimate, StaffingPlan, RateBook, or CostBook data affected
- Existing scheduling views (Dashboard, Coverage, Roll-Off, Assignments) still load

### S9-004 — Update QA_REGRESSION_CHECKLIST.md `[EXTEND]`
Add lane:
- QA-SCHED-PERS: Scheduling Personnel (20+ items covering demand state, resource CRUD, import, drawer, modal, assignment validation)

---

## IMPACTED FILES SUMMARY

### New Backend Files
| File | Purpose |
|------|---------|
| `Data/Models/Craft.cs` | Craft reference table |
| `Data/Models/Certification.cs` | Certification reference table |
| `Data/Models/ResourceCraft.cs` | Secondary crafts junction |
| `Data/Models/ResourceCertification.cs` | Certification junction with expiration |
| `Api/Controllers/CraftController.cs` | CRUD for Craft reference |
| `Api/Controllers/CertificationController.cs` | CRUD for Certification reference |

### Extended Backend Files
| File | Change |
|------|--------|
| `Data/Models/Resource.cs` | New fields: EmployeeId, FirstName, LastName, PrimaryCraftId, Region, Branch, EmploymentStatus, ShiftEligibility, Phone, Email, Notes |
| `Data/Models/Assignment.cs` | Add CraftId (required FK) |
| `Data/AppDbContext.cs` | Add DbSet<Craft>, DbSet<Certification>, DbSet<ResourceCraft>, DbSet<ResourceCertification>; fluent config for new FKs |
| `Api/Controllers/SchedulingController.cs` | Extended resource CRUD, import endpoints, available-resources endpoint, demand state calculation, assignment validation |
| `Data/DBInitializer.cs` or `ReferenceDataSeeder.cs` | Add craft + cert seed data |
| `Api/Controllers/DevController.cs` | Add demo resource seed endpoint |

### New Frontend Files
| File | Purpose |
|------|---------|
| `webapp/src/modules/scheduling/views/ResourceFormView.vue` | Create/edit resource |
| `webapp/src/modules/scheduling/views/ResourceImportView.vue` | CSV import |
| `webapp/src/modules/scheduling/components/DemandSummaryDrawer.vue` | Read-only demand detail drawer |
| `webapp/src/modules/scheduling/components/AssignmentModal.vue` | Craft-controlled assignment modal |

### Extended Frontend Files
| File | Change |
|------|--------|
| `webapp/src/modules/scheduling/views/ResourcesBoardView.vue` | New columns, craft/region/status filters, Import + New Resource buttons |
| `webapp/src/modules/scheduling/views/JobsBoardView.vue` | Demand state badge, disabled Assign on Forecast rows, demand state filter, DemandSummaryDrawer integration |
| Scheduling router file | 3 new resource routes |

### No Estimating Files Changed
Zero changes to `webapp/src/modules/estimating/`, `Api/Controllers/EstimatesController.cs`, `Api/Controllers/StaffingPlansController.cs`, or any existing Estimate/StaffingPlan model.
