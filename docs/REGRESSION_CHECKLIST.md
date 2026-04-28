# Platform Expansion Regression Checklist

Run these checks after every implementation phase. Add ✅/❌ + date when verified.

---

## CORE: Estimating Must Not Break

| ID | Check | Status |
|----|-------|--------|
| REG-EST-01 | `/estimating/estimates` loads estimate list | ⬜ |
| REG-EST-02 | Can open an estimate form | ⬜ |
| REG-EST-03 | `/estimating/staffing-plans` loads staffing list | ⬜ |
| REG-EST-04 | `/estimating/rate-books` loads rate books | ⬜ |
| REG-EST-05 | `/estimating/analytics/revenue` loads revenue forecast | ⬜ |
| REG-EST-06 | `/estimating/analytics/manpower` loads manpower forecast | ⬜ |
| REG-EST-07 | AI chat sidebar still appears on estimate form | ⬜ |
| REG-EST-08 | Login → company select → estimating flow still works | ⬜ |

---

## PORTAL

| ID | Check | Status |
|----|-------|--------|
| REG-PORT-01 | `/` redirects to `/portal` | ⬜ |
| REG-PORT-02 | `/portal` loads PortalDashboardView | ⬜ |
| REG-PORT-03 | App tiles visible for Estimating, Planning, Scheduling | ⬜ |
| REG-PORT-04 | Estimating tile navigates to `/estimating/estimates` | ⬜ |
| REG-PORT-05 | Planning tile navigates to `/planning` | ⬜ |
| REG-PORT-06 | Scheduling tile navigates to `/scheduling/dashboard` | ⬜ |
| REG-PORT-07 | Portal dashboard API returns without 500 | ⬜ |

---

## PLANNING

| ID | Check | Status |
|----|-------|--------|
| REG-PLAN-01 | `/planning` loads without error | ⬜ |
| REG-PLAN-02 | `/planning/step-out-plans` loads list view | ⬜ |
| REG-PLAN-03 | Can create a step-out plan | ⬜ |
| REG-PLAN-04 | Steps with codes "1", "1.2", "1.5" sort correctly | ⬜ |
| REG-PLAN-05 | Work package generation endpoint responds | ⬜ |
| REG-PLAN-06 | FCO can be created | ⬜ |
| REG-PLAN-07 | FCO document generation returns HTML | ⬜ |
| REG-PLAN-08 | Actuals (labor) can be recorded against a work package | ⬜ |
| REG-PLAN-09 | Estimate delta endpoint returns planned vs actual | ⬜ |

---

## SCHEDULING

| ID | Check | Status |
|----|-------|--------|
| REG-SCHED-01 | `/scheduling/dashboard` loads without error | ⬜ |
| REG-SCHED-02 | Jobs endpoint returns estimates (Awarded/Pending) | ⬜ |
| REG-SCHED-03 | Jobs endpoint returns approved unconverted staffing plans | ⬜ |
| REG-SCHED-04 | Jobs endpoint EXCLUDES converted staffing plans | ⬜ |
| REG-SCHED-05 | Resources list loads | ⬜ |
| REG-SCHED-06 | Assignment can be created | ⬜ |
| REG-SCHED-07 | Assignment conflict detected (double-booking) | ⬜ |
| REG-SCHED-08 | Ending-soon endpoint returns assignments ending within 7 days | ⬜ |
| REG-SCHED-09 | Available-soon endpoint returns resources freeing up | ⬜ |
| REG-SCHED-10 | Coverage endpoint shows craft shortage | ⬜ |

---

## DATABASE / BOOTSTRAP

| ID | Check | Status |
|----|-------|--------|
| REG-DB-01 | DB auto-creates + migrates on first run (dev) | ⬜ |
| REG-DB-02 | Reference seed runs (roles, companies, crafts) | ⬜ |
| REG-DB-03 | Demo seed only runs when `Database:SeedDemoData = true` | ⬜ |
| REG-DB-04 | Seed is idempotent (safe to rerun) | ⬜ |
| REG-DB-05 | New migrations apply without error | ⬜ |

---

## DATA INTEGRITY

| ID | Check | Status |
|----|-------|--------|
| REG-DATA-01 | Converted staffing plan not counted in scheduling demand | ⬜ |
| REG-DATA-02 | Unconverted approved staffing plan IS in scheduling demand | ⬜ |
| REG-DATA-03 | Actuals stored in planning tables, not estimate rows | ⬜ |
| REG-DATA-04 | Rate book used for billable calc, cost book for internal cost | ⬜ |
| REG-DATA-05 | Company scoping enforced on all new endpoints | ⬜ |
