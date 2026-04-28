# Scheduling Implementation TODO

**Created:** 2026-04-28  
**Status:** Complete — all phases done, agent review fixes applied

---

## Phase 1 — Backend Services ✅

### Step 1.1 — AssignmentConflictService ✅
- [x] Create `Api/Services/AssignmentConflictService.cs`
- [x] Detect double-booking, unavailability, cert mismatch
- [x] Register as Scoped in Program.cs
- [x] Wire conflict check into CreateAssignment and UpdateAssignment (advisory warnings)

### Step 1.2 — CoverageCalculationService ✅
- [x] Create `Api/Services/CoverageCalculationService.cs`
- [x] Returns `{ craftCode, demandCount, assignedCount, gap, gapPct }` per craft
- [x] Register as Scoped in Program.cs
- [x] Wire into `GET /scheduling/coverage`

### Step 1.3 — SuggestedMatchService ✅
- [x] Create `Api/Services/SuggestedMatchService.cs`
- [x] Finds resources rolling off within 14 days or unassigned, ranked by score
- [x] Register as Scoped in Program.cs
- [x] Wire into `GET /scheduling/suggested-matches`

### Step 1.4 — Real KPIs ✅
- [x] PortalController: all KPIs now real (craftShortagesCount, jobsEndingSoonCount, peopleFreeSoonCount, pendingFcoCount, alertCount)
- [x] SchedulingController Dashboard: all KPIs real
- [x] Build passes, NSwag client regenerated

### Step 1.5 — Additional Backend Fixes (post-review) ✅
- [x] Add `DELETE /scheduling/resources/{id}` endpoint (was missing, trash button returned 404)

---

## Phase 2 — Demo Seed Data ✅

### Step 2.1 — Scheduling Seed ✅
- [x] `SeedSchedulingData` method added to DevController
- [x] Crafts: PP=Pipefitter, EL=Electrician, CR=Crane Operator
- [x] Resources: Mike Torres (PP), Sarah Vance (PP), Darren Hill (EL), Angela Reyes (CR)
- [x] Certifications: Mike→OSHA-30+H2S, Sarah→OSHA-10, Darren→OSHA-30, Angela→Crane-Operator
- [x] Assignments: Mike+Sarah ending ~5 days (ending soon demo), Darren+Angela 60 days out
- [x] Verified: portal dashboard shows craftShortagesCount=10, jobsEndingSoonCount=2, peopleFreeSoonCount=2
- [x] Verified: scheduling dashboard shows endingSoonCount=2, availableSoonCount=2
- [x] Seed idempotent (AnyAsync guard)

---

## Phase 3 — Scheduling Frontend Views ✅

### Step 3.1 — Scheduling Dashboard ✅
- [x] Real KPI strip: Craft Shortages, Ending Soon, Available Soon, Active Jobs
- [x] Nav cards with orange warning borders on non-zero KPIs
- [x] Recent jobs DataTable (top 8) with source type Tag

### Step 3.2 — Jobs Board ✅
- [x] Filterable demand table with source type badges
- [x] "Assign" button per row → assignment create dialog
- [x] Conflict warning dialog on double-booking
- [x] Fixed: `await load()` called after assignment created (table was stale)

### Step 3.3 — Resources Board ✅
- [x] DataTable with craft filter, active/inactive toggle
- [x] Add/Edit Resource dialog (name, craftCode, branch, isActive)
- [x] Detail panel: certifications list (add/remove) + availability blocks
- [x] Uses `useConfirm()` for delete
- [x] Fixed: DELETE endpoint now exists in API

### Step 3.4 — Assignments View ✅
- [x] Full CRUD DataTable
- [x] Create/Edit dialog: resource, job, craft, dates, shift, status
- [x] Conflict warning dialog on save
- [x] Fixed: removed invalid `returnObject` prop from job Dropdown

### Step 3.5 — Coverage View ✅
- [x] Coverage table with progress bars (green ≥100%, yellow 70-99%, red <70%)
- [x] Shortage rows highlighted red
- [x] Suggestions dialog per craft row with suggested matches
- [x] Fixed: "Assign" button now navigates to `/scheduling/assignments` (was dead stub)
- [x] Fixed: added `useRouter` import

### Step 3.6 — Roll-Off View ✅
- [x] Ending-soon table with days countdown
- [x] 7/14/30 day filter SelectButton
- [x] Re-Assign dialog pre-filled with resource craft and start date
- [x] Fixed: removed invalid `returnObject` prop from job Dropdown

---

## Phase 4 — Planning Frontend Views ✅

### Step 4.1 — Step-Out Plan List ✅
- [x] Filterable plan list table
- [x] New Plan dialog → POST then navigate to form
- [x] Status filter, text search, delete with confirmation
- [x] Fixed: `severity="warning"` (was `'warn'` — invalid PrimeVue value, tags rendered unstyled)

### Step 4.2 — Step-Out Plan Form ✅
- [x] Plan header with inline status save
- [x] Steps CRUD (add/edit/delete) sorted by decimal sortOrder
- [x] Generate Work Packages button
- [x] Fixed: moved `computed` import to top (was at line 315, used at line 189)
- [x] Fixed: `/planning/step-out-plans/new` now redirects to list (was blank broken page)

### Step 4.3 — Work Packages ✅
- [x] Table with status/ready filters
- [x] Ready for Scheduling ToggleButton per row → PUT with readyForScheduling flip
- [x] Fixed: `severity="warning"` (was `'warn'`)

### Step 4.4 — FCO List ✅
- [x] FCO table with status filter
- [x] New FCO dialog → Draft status
- [x] Detail dialog with Submit/Approve/Reject workflow
- [x] Generate Document → HTML blob in new tab
- [x] Fixed: `fco.fcoDocumentId` (was `fco.fcoId` — undefined in all API URLs → 404 on every action)
- [x] Fixed: scheduleImpactDays shows "—" for null (was "0 days")

---

## Regression Checklist

- [x] API auth works: `estimator.csl / Stronghold2024` → JWT with company_code=CSL
- [x] Seed idempotent and verified: 4 resources, 4 assignments, certs present
- [x] Scheduling dashboard KPIs: craftShortagesCount=10, endingSoonCount=2
- [x] Portal KPIs: craftShortagesCount=10, jobsEndingSoonCount=2, peopleFreeSoonCount=2
- [x] All 6 scheduling routes registered to real views (not stubs)
- [x] All 4 planning routes registered to real views (not stubs)
- [x] TypeScript: 0 errors (only pre-existing tsconfig deprecation warnings)
- [x] Backend builds: 0 errors
- [ ] Estimating routes still load (manual browser verify)
- [ ] Portal loads at `/portal` with app tiles (manual browser verify)
- [ ] Login flow works end-to-end in browser (manual browser verify)
- [ ] Converted staffing plan NOT in jobs board demand (manual verify)
