# PROJECT SCHEDULING PERSONNEL SPEC
## Stronghold Enterprise — Scheduling Module: Personnel Assignment
**Document Version:** 1.0
**Date:** 2026-04-28
**Status:** APPROVED — Locked Business Requirements
**Companion TODO:** `docs/PROJECT_SCHEDULING_PERSONNEL_TODO.md`

---

## 1. SCHEDULING PURPOSE AND BOUNDARIES

### What Scheduling Owns
Scheduling answers one question: **Who goes where, when, and with what craft?**

Scheduling owns:
- Resource Master (people, identity, craft, region, availability)
- Craft reference table
- Certification reference table
- Availability/blackout/PTO blocks
- Assignments (who is assigned to which job, which craft, which dates)
- Coverage analysis (demand vs assigned headcount per craft)
- Roll-off / ending-soon / available-soon views

### What Scheduling Does NOT Own
- Project timelines, phases, or WBS — those live in Planning/PM
- Gantt charts or PM calendars — Planning only
- Estimates, staffing plans, rate books, work packages — read-only references
- WorkOrders, StepOutPlans, FCOs — read-only references for demand context
- Any commercial calculation or bid pricing logic

### Module Boundary Rule
Scheduling **reads** demand from Estimating and Planning. It does not write to those modules. All assignment logic is internal to Scheduling.

---

## 2. FORECAST DEMAND VS RELEASED DEMAND (LOCKED)

Scheduling demand has two mutually exclusive states. The distinction exists because workforce planning must begin before formal work authorization, but assignments must only be created against formally released work.

### Forecast Demand
**Definition:** Demand that is visible but not yet assignable.

**Sources:**
- Awarded Estimates not yet backed by a released WorkOrder
- Approved unconverted StaffingPlans (`ConvertedEstimateId IS NULL`)

**Behavior:**
- Appears on jobs board with `Forecast` badge (gray)
- Demand summary drawer opens on double-click (read-only)
- Assign button is disabled — label: "Not Released — Assign Disabled"
- `Assignable` field in drawer shows: `✗ No — WorkOrder not released`
- Useful for workforce planning: scheduler can see what's coming and identify craft shortages in advance

**Backend:** Assignment create endpoint rejects demand in Forecast state with HTTP 422 `DemandNotReleased`. This is server-side enforcement, not just a UI guard.

### Released / Assignable Demand
**Definition:** Demand that is both visible and assignable.

**Sources:**
- WorkPackages with `ReadyForScheduling = true`
- **And** the backing WorkOrder has `Status IN ('Released', 'InProgress')`

**Behavior:**
- Appears on jobs board with `Released` badge (green)
- Demand summary drawer opens on double-click
- Assign button is active — opens AssignmentModal
- `Assignable` field in drawer shows: `✓ Yes — Released WorkOrder backs this demand`

**Key rule:** A WorkPackage can have `ReadyForScheduling = true` set by Planning, but if the WorkOrder is still in Draft status, the demand remains Forecast — not assignable. The WorkOrder release is the gate.

### Demand State on Jobs Board

```
Jobs Board
┌─────────┬───────────────────────────────────────────┬────────────┬─────────┐
│ State   │ Job / Demand                              │ Source     │ Actions │
├─────────┼───────────────────────────────────────────┼────────────┼─────────┤
│Forecast │ Bayou Refinery — Phase 2                  │ Estimate   │[View]   │
│         │ EST-2026-014                               │            │[Assign⛔]│
├─────────┼───────────────────────────────────────────┼────────────┼─────────┤
│Released │ Gulf Coast Tie-In — Main Execution         │ WorkPackage│[View]   │
│         │ WP-2026-007  WO: WO-2026-003  [Released]  │            │[Assign] │
└─────────┴───────────────────────────────────────────┴────────────┴─────────┘
```

**Filter bar options:** All (default) | Forecast Only | Released Only

---

## 3. JOBS BOARD ROW BEHAVIOR (LOCKED)

**Double-click any row:** Opens Demand Summary Drawer (right-side overlay, read-only).
**Single-click [View]:** Same as double-click.
**Single-click [Assign]:** Opens AssignmentModal directly (only active on Released rows).

**Columns:**
| Column | Notes |
|--------|-------|
| State badge | Forecast (gray) or Released (green) |
| Source type | Estimate / StaffingPlan / WorkPackage |
| Reference # | Estimate number, SP number, or WP number |
| Name | Job or plan name |
| Client | From the source record |
| Craft Shortages | Count of crafts where Required > Assigned |
| Start / End | Planned dates from source |
| Actions | [View] always; [Assign] only if Released |

---

## 4. DEMAND SUMMARY DRAWER SPEC (LOCKED)

**Trigger:** Double-click jobs board row, or [View] button.
**Type:** Right-side overlay drawer. Read-only. No edit controls.

### Drawer Sections

**Header:**
- Source type badge (Estimate / StaffingPlan / WorkPackage)
- Reference number
- Job/demand name
- Close [✕] button

**Details:**
- Project (if a Project is linked via WorkOrder → Project)
- Client
- Site / Location
- Status (of the source record)
- Planned Start / Planned End

**Authorization:**
- WorkOrder: number + status tag (or "No WorkOrder" for Forecast-only demand)
- CommercialAuthorization: number + authorized value (or "None")
- FCO Count: number of open FCOs linked to this WO
- Assignable: ✓ Yes (Released) or ✗ No — WorkOrder not released (Forecast)

**Craft Requirements:**
- Table: Craft | Required Qty | Currently Assigned | Gap
- Gap column is red when Required > Assigned, green when met

**Action:**
- [Assign Resource] button — active only when demand is Released; otherwise shows "Not Released — Assign Disabled" (non-interactive)

---

## 5. ASSIGNMENT MODAL SPEC (LOCKED)

**Trigger:** [Assign Resource] in drawer, or [Assign] button on Released jobs board row.
**Type:** Modal dialog. Not a page navigation.

### Locked Rules
1. Job context is auto-populated from the demand row and is display-only in the modal.
2. Craft field is a **dropdown** from the Craft reference table. **No free-text entry. Ever.**
3. Resource list is filtered dynamically based on craft + dates + availability.
4. Assignment create is blocked server-side if demand is not in Released state.

### Modal Layout

```
┌──────────────────────────────────────────────────────────────┐
│  Assign Resource                                        [✕]  │
├──────────────────────────────────────────────────────────────┤
│  JOB CONTEXT (display-only)                                  │
│  Job:    Gulf Coast Tie-In — Main Execution                  │
│  Source: WorkPackage WP-2026-007                             │
│  WO:     WO-2026-003  [Released]                             │
│  Dates:  Apr 15 – Jun 15, 2026                               │
├──────────────────────────────────────────────────────────────┤
│  ASSIGNMENT DETAILS                                          │
│  Craft:  [Pipefitter ▼]         ← dropdown, required        │
│  Start:  [Apr 15, 2026 📅]                                   │
│  End:    [Jun 15, 2026 📅]                                   │
│  Shift:  [Day ▼]                                             │
│  Notes:  [                                               ]   │
├──────────────────────────────────────────────────────────────┤
│  AVAILABLE RESOURCES  (filtered: craft / certs / dates)     │
│  ┌──────────────────┬────────┬──────────┬──────────────────┐ │
│  │ Name             │ Region │ Certs    │ Conflicts        │ │
│  ├──────────────────┼────────┼──────────┼──────────────────┤ │
│  │ ● John Martinez  │ Gulf   │ NCCER    │ None             │ │
│  │ ● Sarah Tran     │ Gulf   │ NCCER    │ None             │ │
│  │ ○ Mike Boudreaux │ TX     │ NCCER    │ Apr 20–30 PTO    │ │
│  │ ✗ Dave Johnson   │ Gulf   │ —        │ Apr 15–May 1 (full)│ │
│  └──────────────────┴────────┴──────────┴──────────────────┘ │
│  ● Fully Available  ○ Partial Conflict  ✗ Full Conflict      │
│  [Select Resource from list — click row to select]           │
├──────────────────────────────────────────────────────────────┤
│  Optional: Region filter [▼]  Branch filter [▼]              │
├──────────────────────────────────────────────────────────────┤
│                             [Cancel]  [Create Assignment]    │
└──────────────────────────────────────────────────────────────┘
```

### Resource List Filtering Rules
1. PrimaryCraft = selected Craft **OR** SecondaryCraft includes selected Craft
2. No AvailabilityBlock overlapping the assignment date range (full conflict → excluded with ✗)
3. No existing Assignment overlapping the date range (full conflict → excluded with ✗; partial overlap → shown with ○)
4. Region/Branch filter is optional — applied as secondary filter chips
5. Cert filter: if the demand record specifies required certs, resources missing those certs are excluded

### On Save
POST to `/api/v1/scheduling/assignments`:
```json
{
  "resourceId": 42,
  "demandSourceType": "WorkPackage",
  "demandSourceId": 7,
  "craftId": 3,
  "startDate": "2026-04-15",
  "endDate": "2026-06-15",
  "shiftType": "Day",
  "notes": "Lead pipefitter for Phase 2"
}
```

Server validates: DemandSourceType/Id resolves to Released demand (HTTP 422 if not). Resource has no full conflict for the date range (HTTP 409 if conflict).

---

## 6. RESOURCE MASTER DATA MODEL (LOCKED)

### Core Identity Fields
| Field | Type | Required | Notes |
|-------|------|----------|-------|
| ResourceId | int | PK | Auto-generated |
| CompanyCode | string | Yes | Multi-tenant key |
| EmployeeId | string | No | HR/AD import identifier; nullable |
| FirstName | string | Yes | |
| LastName | string | Yes | |
| DisplayName | string | Computed | FirstName + " " + LastName |
| EmploymentStatus | enum | Yes | Active, Inactive, OnLeave, Terminated — default Active |

### Craft and Location
| Field | Type | Required | Notes |
|-------|------|----------|-------|
| PrimaryCraftId | int FK | Yes | FK → Crafts; required for filtering |
| Region | string | No | e.g. "Gulf Coast", "Texas" |
| Branch | string | No | Yard/HomeLocation name |

### Scheduling
| Field | Type | Required | Notes |
|-------|------|----------|-------|
| ShiftEligibility | enum | No | Day, Night, Rotating, Any — default Any |

### Contact (Optional)
| Field | Type | Required |
|-------|------|----------|
| Phone | string | No |
| Email | string | No |

### Notes
| Field | Type | Required |
|-------|------|----------|
| Notes | string | No |

### Navigation Collections
- **SecondaryCrafts** → ResourceCraft junction (ResourceId, CraftId, IsPrimary=false)
- **Certifications** → ResourceCertification junction (ResourceId, CertificationId, IssueDate?, ExpirationDate?)
- **AvailabilityBlocks** → AvailabilityBlock table (ResourceId, StartDate, EndDate, Reason, BlockType)
- **Assignments** → Assignment table (ResourceId, ...)

---

## 7. CSV IMPORT / UPSERT WORKFLOW

### Purpose
Load resources in bulk from an HR export, spreadsheet, or field roster. Required at launch — not a future feature.

### Import Template Columns
```
EmployeeId | FirstName | LastName | CraftCode | SecondaryCraftCodes | Region | Branch | EmploymentStatus | ShiftEligibility | Phone | Email | Certs
```

- `SecondaryCraftCodes`: comma-separated list of CraftCodes (e.g. "WL,SC")
- `Certs`: pipe-separated list of CertCode + optional expiration (e.g. "NCCER|2027-06|H2S")
- `EmploymentStatus` and `ShiftEligibility` accept enum values; blanks default to Active / Any

### Endpoints
- `GET /api/v1/scheduling/resources/import/template` — returns the CSV template file
- `POST /api/v1/scheduling/resources/import/validate` — accepts multipart CSV, returns validation results (row-level errors: unknown CraftCode, missing required field, unknown CertCode)
- `POST /api/v1/scheduling/resources/import` — performs upsert

### Upsert Logic
1. If `EmployeeId` matches an existing Resource for the same CompanyCode → **update** that record
2. If no match → **create** new record
3. SecondaryCrafts and Certifications are **replaced** on each import (not appended) — import is idempotent

### Validation Rules
- `FirstName` and `LastName` required
- `CraftCode` must match an existing Craft.CraftCode for the company
- `SecondaryCraftCodes` unknown codes → row-level warning, not hard error
- Duplicate `EmployeeId` within the same import file → error (ambiguous upsert target)

### Response
```json
{
  "created": 38,
  "updated": 4,
  "skipped": 0,
  "errors": [
    { "row": 12, "field": "CraftCode", "value": "ZZ", "reason": "Unknown craft code" }
  ]
}
```

---

## 8. FUTURE AD SYNC NOTE

When Active Directory or HR system integration is implemented, it will use the same upsert endpoint (`/api/v1/scheduling/resources/import` or a dedicated `/sync` variant). No new module or ownership boundary is created. Scheduling still owns the Resource Master. AD sync is an automated import source.

Fields most likely supplied by AD sync: EmployeeId, FirstName, LastName, Email, EmploymentStatus.

Fields that AD sync will NOT know: PrimaryCraft, Region, Branch, ShiftEligibility, Certifications — these must remain manually managed in Scheduling or supplemented from a separate HR export.

---

## 9. CRAFT AND CERTIFICATION REFERENCE MODEL

### Craft Table
| Field | Type | Notes |
|-------|------|-------|
| CraftId | int PK | |
| CompanyCode | string | Multi-tenant |
| CraftCode | string | Short code: "PP", "WL", "RI", "SC", "IN", "EL", "OL", "HL", "CR" |
| Name | string | Full name: "Pipefitter", "Welder", etc. |
| Description | string? | Optional |
| IsActive | bool | Default true; soft-delete |

Unique index: CompanyCode + CraftCode

**Admin-managed.** Used as dropdown source for assignment modal, resource form, and demand filtering.

**Seed data (reference):**
| CraftCode | Name |
|-----------|------|
| PP | Pipefitter |
| WL | Welder |
| RI | Rigger |
| SC | Scaffold Builder |
| IN | Instrument Technician |
| EL | Electrician |
| OL | Operator |
| HL | Helper / Laborer |
| CR | Crane Operator |

### Certification Table
| Field | Type | Notes |
|-------|------|-------|
| CertificationId | int PK | |
| CompanyCode | string | Multi-tenant |
| CertCode | string | Short code: "NCCER", "H2S", "OSHA10", etc. |
| Name | string | Full name |
| Description | string? | Optional |
| DefaultExpirationMonths | int? | If set, used as default when adding cert to resource |
| IsActive | bool | Default true; soft-delete |

Unique index: CompanyCode + CertCode

**Seed data (reference):**
| CertCode | Name | Default Expiry |
|----------|------|----------------|
| NCCER | NCCER Core | — |
| H2S | H2S Alive | 12 months |
| OSHA10 | OSHA 10-Hour | — |
| OSHA30 | OSHA 30-Hour | — |
| CRANE | Crane Operator | 24 months |
| RIGGING1 | Rigging Level 1 | — |

---

## 10. ASSIGNMENT VALIDATION RULES

These rules are enforced server-side in AssignmentController. UI guards mirror them but are not the enforcement mechanism.

### Rule 1: Demand Must Be Released
```
REQUIRES:
  DemandSourceType = 'WorkPackage'
  WorkPackage.ReadyForScheduling = true
  BackingWorkOrder.Status IN ('Released', 'InProgress')
REJECTS:
  Any assignment against Estimate or StaffingPlan demand (Forecast state)
  HTTP 422: DemandNotReleased
```

### Rule 2: CraftId Is Required
```
REQUIRES:
  CraftId != null
  Craft must exist and IsActive = true for this CompanyCode
REJECTS:
  Any assignment without CraftId
  HTTP 400: CraftRequired
  Free-text craft — not accepted; CraftId is the only valid input
```

### Rule 3: No Full Availability Conflict
```
REQUIRES:
  No AvailabilityBlock for this Resource where
    BlockStart <= AssignmentEnd AND BlockEnd >= AssignmentStart
REJECTS:
  HTTP 409: ResourceUnavailable
  Response includes conflicting block dates and reason
```

### Rule 4: No Full Assignment Conflict
```
REQUIRES:
  No existing Assignment for this Resource where
    ExistingStart <= NewEnd AND ExistingEnd >= NewStart
    AND Status NOT IN ('Cancelled')
REJECTS:
  HTTP 409: ResourceAlreadyAssigned
  Response includes conflicting assignment details
```

### Rule 5: Resource Must Be Active
```
REQUIRES:
  Resource.EmploymentStatus = 'Active'
REJECTS:
  HTTP 422: ResourceNotActive
  Inactive, OnLeave, or Terminated resources cannot be assigned
```

### Partial Conflict (Informational Only)
If a resource has a partial availability or assignment conflict within the date range (not full overlap), the available-resources endpoint returns the resource with a conflict summary. The UI shows the ○ indicator. The user can still select this resource — partial conflicts are informational warnings, not hard blocks. The server records the assignment and the conflict is visible on the schedule.
