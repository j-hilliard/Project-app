#!/usr/bin/env bash
# ARCH-FE check script — reports frontend guardrail violations.
# By default runs in WARN mode (exits 0 even on violations) until Batch 3-4 refactors land.
# Pass --strict to make violations hard failures.
#
# Run from the repo root.
set -euo pipefail

STRICT=0
for arg in "$@"; do
  [[ "$arg" == "--strict" ]] && STRICT=1
done

errors=0
warnings=0

pass()    { echo "[PASS] $1"; }
report()  {
  if [[ $STRICT -eq 1 ]]; then
    echo "[FAIL] $1"; ((errors++)) || true
  else
    echo "[WARN] $1"; ((warnings++)) || true
  fi
}
warn()    { echo "[WARN] $1"; ((warnings++)) || true; }
header()  { echo ""; echo "=== $1 ==="; }

PM_DIRS="webapp/src/modules/planning webapp/src/modules/scheduling"

check_pattern() {
  local rule="$1"
  local description="$2"
  local pattern="$3"
  header "$rule — $description"
  hits=$(grep -rEn "$pattern" $PM_DIRS --include="*.vue" 2>/dev/null || true)
  if [[ -n "$hits" ]]; then
    echo "$hits" | head -20
    local count
    count=$(echo "$hits" | wc -l)
    report "$count violation(s) found for $rule. See docs/ARCHITECTURE_GUARDRAILS.md."
  else
    pass "No violations found."
  fi
}

# ── ARCH-FE-001: No direct useApiStore import ─────────────────────────────────
check_pattern \
  "ARCH-FE-001" \
  "PM/Scheduling views must not import useApiStore directly" \
  "import.*useApiStore"

# ── ARCH-FE-002: No direct apiStore.api calls ─────────────────────────────────
check_pattern \
  "ARCH-FE-002" \
  "PM/Scheduling views must not call apiStore.api directly" \
  "apiStore\.api\."

# ── ARCH-FE-003: No local fmtDate definition ──────────────────────────────────
check_pattern \
  "ARCH-FE-003" \
  "PM/Scheduling views must not define fmtDate locally" \
  "(function fmtDate|const fmtDate\s*=|fmtDate\s*=\s*(function|\(|d\b))"

# ── ARCH-FE-004: No local fmtCurrency definition ─────────────────────────────
check_pattern \
  "ARCH-FE-004" \
  "PM/Scheduling views must not define fmtCurrency locally" \
  "(function fmtCurrency|const fmtCurrency\s*=|fmtCurrency\s*=\s*(function|\())"

# ── ARCH-FE-005: No local statusSeverity definition ──────────────────────────
check_pattern \
  "ARCH-FE-005" \
  "PM/Scheduling views must not define statusSeverity locally" \
  "(function statusSeverity|const statusSeverity\s*=|statusSeverity\s*=\s*(function|\(|\{))"

# ── ARCH-FE-006: No local sourceTagSeverity definition ───────────────────────
check_pattern \
  "ARCH-FE-006" \
  "PM/Scheduling views must not define sourceTagSeverity locally" \
  "(function sourceTagSeverity|const sourceTagSeverity\s*=|sourceTagSeverity\s*=\s*(function|\(|\{))"

# ── ARCH-DS-001: webapp/src/ui/ must exist ───────────────────────────────────
header "ARCH-DS-001 — webapp/src/ui/ design system must exist"
if [[ -d "webapp/src/ui" ]] && [[ -n "$(ls -A webapp/src/ui 2>/dev/null)" ]]; then
  pass "webapp/src/ui/ exists and is non-empty."
else
  warn "webapp/src/ui/ does not exist or is empty. Design system not yet created (expected until Batch 2)."
fi

# ── Summary ───────────────────────────────────────────────────────────────────
echo ""
echo "─────────────────────────────────────────"
mode_label="WARN"
[[ $STRICT -eq 1 ]] && mode_label="STRICT"
echo "Frontend check complete [$mode_label mode]. Errors: $errors  Warnings: $warnings"
echo "─────────────────────────────────────────"

if [[ $STRICT -eq 1 ]] && [[ $errors -gt 0 ]]; then
  echo "RESULT: FAILED — $errors frontend guardrail violation(s). See docs/ARCHITECTURE_GUARDRAILS.md."
  exit 1
elif [[ $warnings -gt 0 ]]; then
  echo "RESULT: WARNINGS — $warnings frontend violations pending refactor (Batches 3-4). Add --strict once views are cleaned."
  exit 0
else
  echo "RESULT: PASSED"
  exit 0
fi
