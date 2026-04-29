# QA Evidence - Project-app Runtime Audit - 2026-04-29

Sweep ID: `QA_AUDIT_20260429_PROJECT_APP_RUNTIME`

Scope: read-only audit of the active `Project-app` repo on branch `feat/pm-module`. Estimating code was not edited.

## Commands Run

| Command | Result | Notes |
|---|---|---|
| `dotnet build --no-restore --configuration Release` | PASS | Backend/API/Data/Shared compiled. NSwag ran successfully in Release. Warnings: AutoMapper NU1903 high severity advisory; nullable warning in `Api/Program.cs(126,55)`. |
| `npm.cmd --prefix webapp install` | PASS | Installed missing frontend dependencies and Playwright package dependencies. |
| `npm.cmd --prefix webapp run build:dev` | PASS | Vite build succeeded and emitted Planning/Scheduling/Portal chunks. |
| `dotnet test --no-restore --configuration Release` | PASS / NO TEST OUTPUT | Repo has only `Api`, `Data`, and `Shared` csproj files; no backend test project was discovered. |
| `SKIP_GLOBAL_SETUP=true npx.cmd playwright test tests/e2e/verify-scheduling-planning.spec.ts --project=chromium-mocked --workers=1` | PASS BUT WEAK | Global setup skipped, two tests passed, but screenshots are blank dark pages. This spec only checks absence of error banners, not visible app content. Treat as inadequate verification until strengthened. |

## Screenshot Evidence

- `docs/qa-evidence/QA_AUDIT_20260429_PROJECT_APP_RUNTIME/scheduling-dashboard.png`
- `docs/qa-evidence/QA_AUDIT_20260429_PROJECT_APP_RUNTIME/step-out-plans.png`

Both screenshots are blank dark shells despite the Playwright test passing. This is a QA finding: the existing planning/scheduling smoke spec can produce false positives.

## Findings

1. Backend compile is currently green in Release.
2. Frontend compile is currently green after dependencies were installed.
3. No backend unit/integration test project exists in this repo.
4. Playwright global setup can mutate/seed data, so safe audits must keep `SKIP_GLOBAL_SETUP=true` unless Joseph approves live mutation.
5. The current planning/scheduling Playwright smoke test is insufficient because it passed while screenshots showed no visible content.
6. Runtime architecture still has drift from the canonical docs: Planning routes are Step-Out-first, portal still has old FCO/recent-estimate signals, lifecycle gates are soft, and Scheduling still uses string craft/free-text assignment patterns.

## Follow-up For Claude

- Strengthen `verify-scheduling-planning.spec.ts` to assert visible page text/content, not only absence of error banners.
- Keep estimating frozen.
- Fix architecture drift before adding new feature polish.
