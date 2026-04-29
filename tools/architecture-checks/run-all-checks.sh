#!/usr/bin/env bash
# run-all-checks.sh — master architecture check runner.
# Runs backend (hard-fail) and frontend (strict mode) guardrail checks.
# Batches 3 + 4 complete: frontend is now always strict.
#
# Run from the repo root. Exit 0 = all hard checks pass. Exit 1 = any violation.
set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
STRICT_FRONTEND=1
for arg in "$@"; do
  [[ "$arg" == "--no-strict-frontend" ]] && STRICT_FRONTEND=0
done

echo "══════════════════════════════════════════════════════"
echo "  Architecture Guardrail Checks"
echo "  $(date '+%Y-%m-%d %H:%M:%S')"
echo "══════════════════════════════════════════════════════"

backend_exit=0
frontend_exit=0

echo ""
echo "▶ Running backend checks (hard-fail)..."
echo "──────────────────────────────────────"
bash "$SCRIPT_DIR/check-backend.sh" || backend_exit=$?

echo ""
if [[ $STRICT_FRONTEND -eq 1 ]]; then
  echo "▶ Running frontend checks (strict mode)..."
  echo "──────────────────────────────────────"
  bash "$SCRIPT_DIR/check-frontend.sh" --strict || frontend_exit=$?
else
  echo "▶ Running frontend checks (warn mode)..."
  echo "──────────────────────────────────────"
  bash "$SCRIPT_DIR/check-frontend.sh" || frontend_exit=$?
fi

echo ""
echo "══════════════════════════════════════════════════════"
if [[ $backend_exit -ne 0 ]]; then
  echo "  OVERALL RESULT: FAILED (backend violations)"
  echo "  Backend exit: $backend_exit  Frontend exit: $frontend_exit"
  echo "══════════════════════════════════════════════════════"
  exit 1
elif [[ $STRICT_FRONTEND -eq 1 ]] && [[ $frontend_exit -ne 0 ]]; then
  echo "  OVERALL RESULT: FAILED (frontend violations in strict mode)"
  echo "  Backend exit: $backend_exit  Frontend exit: $frontend_exit"
  echo "══════════════════════════════════════════════════════"
  exit 1
else
  echo "  OVERALL RESULT: PASSED"
  echo "  Backend: OK  Frontend: $([ $frontend_exit -eq 0 ] && echo 'OK' || echo 'WARNINGS')"
  echo "══════════════════════════════════════════════════════"
  exit 0
fi
