#!/usr/bin/env bash
# ARCH-BE check script — exits non-zero if any backend guardrail is violated.
# Run from the repo root.
set -euo pipefail

PASS=0
FAIL=1
errors=0
warnings=0

pass()  { echo "[PASS] $1"; }
fail()  { echo "[FAIL] $1"; ((errors++)) || true; }
warn()  { echo "[WARN] $1"; ((warnings++)) || true; }
header(){ echo ""; echo "=== $1 ==="; }

# ── ARCH-BE-001: No raw EF entity [FromBody] in cleaned controllers ───────────
header "ARCH-BE-001 — No raw EF entity [FromBody] in cleaned controllers"
cleaned_controllers=(
  "Api/Controllers/ProjectController.cs"
  "Api/Controllers/WorkOrderController.cs"
  "Api/Controllers/PlanningController.cs"
  "Api/Controllers/SchedulingController.cs"
)
raw_entity_pattern='\[FromBody\] (Project|WorkOrder|FcoDocument|StepOutPlan|StepOutStep|WorkPackage|Resource|Assignment|Certification|AvailabilityBlock) '
be001_hits=0
for f in "${cleaned_controllers[@]}"; do
  if [[ -f "$f" ]]; then
    hits=$(grep -En "$raw_entity_pattern" "$f" 2>/dev/null || true)
    if [[ -n "$hits" ]]; then
      echo "$hits" | while read -r line; do echo "  $f: $line"; done
      be001_hits=1
    fi
  else
    fail "Controller not found: $f"
    ((errors++)) || true
  fi
done
if [[ $be001_hits -eq 0 ]]; then
  pass "No raw EF entity [FromBody] found in cleaned controllers."
else
  fail "Raw EF entity [FromBody] found — add a contract record in Api/Contracts/."
fi

# ── ARCH-BE-002: AppDbContext must not contain inline entity config ────────────
header "ARCH-BE-002 — AppDbContext must not contain inline entity config"
if [[ -f "Data/AppDbContext.cs" ]]; then
  inline_hits=$(grep -c "modelBuilder\.Entity<" Data/AppDbContext.cs 2>/dev/null) || inline_hits=0
  if [[ "$inline_hits" -gt 0 ]]; then
    fail "Data/AppDbContext.cs contains $inline_hits modelBuilder.Entity<> call(s). Move to IEntityTypeConfiguration<T> classes."
  else
    pass "AppDbContext.cs has no inline entity config."
  fi
else
  fail "Data/AppDbContext.cs not found."
fi

# ── ARCH-BE-003: StatusUpdateRequest must not be redefined in controllers ─────
header "ARCH-BE-003 — StatusUpdateRequest must not be defined inline in controllers"
inline_status=$(grep -rn "record StatusUpdateRequest" Api/Controllers/ 2>/dev/null || true)
if [[ -n "$inline_status" ]]; then
  echo "$inline_status"
  fail "StatusUpdateRequest is redefined in a controller. Use Api/Contracts/Common/StatusUpdateRequest.cs."
else
  pass "StatusUpdateRequest not redefined inline."
fi

# ── ARCH-BE-004: BuildFcoHtml must not exist in PlanningController ────────────
header "ARCH-BE-004 — FCO HTML generation must live in FcoDocumentService"
if [[ -f "Api/Controllers/PlanningController.cs" ]]; then
  fco_hits=$(grep -c "BuildFcoHtml\|private.*string.*Html\b" Api/Controllers/PlanningController.cs 2>/dev/null) || fco_hits=0
  if [[ "$fco_hits" -gt 0 ]]; then
    fail "PlanningController.cs contains inline FCO HTML generation. Move to Api/Services/Planning/FcoDocumentService."
  else
    pass "No inline FCO HTML generation in PlanningController.cs."
  fi
else
  fail "Api/Controllers/PlanningController.cs not found."
fi

# ── ARCH-BE-005: Required service files must exist ───────────────────────────
header "ARCH-BE-005 — Required service files must exist"
required_services=(
  "Api/Services/Planning/FcoDocumentService.cs"
  "Api/Services/WorkOrders/WorkOrderFinancialService.cs"
)
for f in "${required_services[@]}"; do
  if [[ -f "$f" ]]; then
    pass "Found: $f"
  else
    fail "Missing required service: $f"
  fi
done

# ── ARCH-BE-006: Required contract directories must exist and be non-empty ────
header "ARCH-BE-006 — Required contract directories must exist"
required_contract_dirs=(
  "Api/Contracts/Common"
  "Api/Contracts/Planning"
  "Api/Contracts/Projects"
  "Api/Contracts/Scheduling"
  "Api/Contracts/WorkOrders"
)
for d in "${required_contract_dirs[@]}"; do
  if [[ -d "$d" ]] && [[ -n "$(ls -A "$d" 2>/dev/null)" ]]; then
    pass "Non-empty: $d"
  elif [[ -d "$d" ]]; then
    fail "Directory exists but is empty: $d"
  else
    fail "Missing required contract directory: $d"
  fi
done

# ── ARCH-BE-007: Entity configuration directories must exist ──────────────────
header "ARCH-BE-007 — Entity configuration directories must exist"
required_config_dirs=(
  "Data/Configurations/Core"
  "Data/Configurations/Estimating"
  "Data/Configurations/Planning"
  "Data/Configurations/Scheduling"
)
for d in "${required_config_dirs[@]}"; do
  if [[ -d "$d" ]] && [[ -n "$(ls -A "$d" 2>/dev/null)" ]]; then
    pass "Non-empty: $d"
  elif [[ -d "$d" ]]; then
    fail "Configuration directory is empty: $d"
  else
    fail "Missing entity configuration directory: $d"
  fi
done

# ── ARCH-BE-008: AppDbContext uses ApplyConfigurationsFromAssembly ────────────
header "ARCH-BE-008 — AppDbContext uses ApplyConfigurationsFromAssembly"
if [[ -f "Data/AppDbContext.cs" ]]; then
  if grep -q "ApplyConfigurationsFromAssembly" Data/AppDbContext.cs 2>/dev/null; then
    pass "ApplyConfigurationsFromAssembly found in AppDbContext.cs."
  else
    fail "AppDbContext.cs does not call ApplyConfigurationsFromAssembly."
  fi
else
  fail "Data/AppDbContext.cs not found."
fi

# ── ARCH-BE-009: DevController line count (warn-only) ────────────────────────
header "ARCH-BE-009 — DevController size (warn-only until seeder extraction is complete)"
if [[ -f "Api/Controllers/DevController.cs" ]]; then
  dev_lines=$(wc -l < Api/Controllers/DevController.cs)
  if [[ "$dev_lines" -gt 300 ]]; then
    warn "DevController.cs is $dev_lines lines (target: < 300). Extract seeder services to Api/Services/Dev/."
  else
    pass "DevController.cs is $dev_lines lines (within limit)."
  fi
else
  warn "Api/Controllers/DevController.cs not found (expected for dev builds)."
fi

# ── Summary ───────────────────────────────────────────────────────────────────
echo ""
echo "─────────────────────────────────────────"
echo "Backend check complete. Errors: $errors  Warnings: $warnings"
echo "─────────────────────────────────────────"

if [[ $errors -gt 0 ]]; then
  echo "RESULT: FAILED — $errors backend guardrail violation(s). See ARCHITECTURE_GUARDRAILS.md."
  exit 1
else
  echo "RESULT: PASSED"
  exit 0
fi
