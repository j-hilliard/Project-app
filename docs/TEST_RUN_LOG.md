# Test Run Log

Format: Date | Phase | Command | Purpose | Result | Notes

---

| Date | Phase | Command | Purpose | Result | Notes |
|------|-------|---------|---------|--------|-------|
| 2026-04-28 | 0 | none | Docs only, no build | N/A | Phase 0 creates docs, no code changes |
| 2026-04-28 | Platform DB startup check | `dotnet build --no-restore --configuration Release` | Confirm backend compiles before DB diagnosis | PASS | Build succeeded with known AutoMapper advisory and NSwag post-build warning. |
| 2026-04-28 | Platform DB startup check | `dotnet run --project Api\Api.csproj --no-build --configuration Release` with `ASPNETCORE_ENVIRONMENT=Local` | Verify API startup and migration bootstrap | FAIL | Startup enters `DatabaseBootstrapper`, starts migrations, then SQL connection fails: SQL Server requires encryption but this machine/process does not support it. Command timed out while EF retry policy continued retrying. |
| 2026-04-28 | Platform DB startup check | PowerShell `System.Data.SqlClient.SqlConnection` probe to `.\\SQLEXPRESS/master` | Confirm SQL connectivity independent of app code | FAIL | SQL Express service is running, but direct connection probes failed with SSPI/encryption-related errors. |
| 2026-04-28 | Data recovery | `Get-NetTCPConnection` / `Get-Process` for ports `7210`, `7211` | Find why frontend had no data | FAIL -> ROOT CAUSE FOUND | Port `7211` was owned by `node.exe`, not the .NET API. The frontend expects `VITE_APP_API_BASE_URL=https://localhost:7211`, so it was pointed at the wrong process. |
| 2026-04-28 | Data recovery | Stop wrong `7211` listener, start `dotnet run --project Api\\Api.csproj --launch-profile https` | Restore .NET API on expected port | PASS | API listened on `https://localhost:7211` and `http://localhost:5047`; migrations ran and SQL connection succeeded. |
| 2026-04-28 | Data recovery | `POST https://localhost:7211/api/v1/dev/seed` | Restore additive demo data | PASS | Seed returned `200` with "Seed complete (additive - existing data preserved)." |
| 2026-04-28 | Data recovery | Authenticated read-only checks for `/api/v1/estimates` and `/api/v1/staffing-plans` | Confirm data returned | PASS | CSL returned `43` estimates and `12` staffing plans. |
| 2026-04-28 | Platform scheduling queue audit | `dotnet build --no-restore --configuration Release` | Check Claude's current Portal/Planning/Scheduling backend compiles | PASS | Build succeeded. Warnings: AutoMapper high severity advisory and nullable schema processor warning in `Api/Program.cs`. NSwag executed successfully during build. |
| 2026-04-28 | Platform scheduling queue audit | `npm.cmd --prefix webapp run build:dev` | Check Claude's current Portal/Planning/Scheduling frontend compiles | PASS | Vite build succeeded with scheduling/planning/portal chunks emitted. Functional route/API verification still pending. |
| 2026-04-28 | Handoff doc cleanup | `rg -n "ProjectPlan|TaskActual|dhtmlx-gantt|/api/v1/project-plans|ProjectPlanView|ProjectPlans|DbSet<ProjectPlan>|DbSet<TaskActual>" ...` | Verify stale terms were removed from active canonical docs | PASS | Remaining matches are only archived/superseded/conflict context in tombstones, handoff index, QA notes, or worklog. |
| 2026-04-28 | Handoff doc cleanup | `Test-Path` checks for all handoff-index docs | Verify every doc named in `PROJECT_HANDOFF_INDEX.md` exists | PASS | All eight canonical docs plus the handoff index exist. |

| 2026-04-29 | Project-app runtime audit | `dotnet build --no-restore --configuration Release` | Verify backend/API/Data/Shared compile and NSwag generation | PASS | Build succeeded. NSwag ran successfully. Warnings: AutoMapper NU1903 advisory and nullable warning in `Api/Program.cs(126,55)`. |
| 2026-04-29 | Project-app runtime audit | `npm.cmd --prefix webapp install` | Restore missing frontend dependencies needed for build/test | PASS | Dependencies installed; 0 npm vulnerabilities reported. |
| 2026-04-29 | Project-app runtime audit | `npm.cmd --prefix webapp run build:dev` | Verify frontend compile after Claude platform changes | PASS | Vite build succeeded with portal/planning/scheduling chunks emitted. |
| 2026-04-29 | Project-app runtime audit | `dotnet test --no-restore --configuration Release` | Check for backend tests | PASS / NO TEST OUTPUT | Repo has no backend test project; command exited 0 without test output. |
| 2026-04-29 | Project-app runtime audit | `SKIP_GLOBAL_SETUP=true npx.cmd playwright test tests/e2e/verify-scheduling-planning.spec.ts --project=chromium-mocked --workers=1` | Initial planning/scheduling smoke | SUPERSEDED / INVALID FOR PROJECT-APP ROUTE PROOF | Later port inspection showed this Playwright config targets `7210/7211`, which were owned by the old repo. Do not use this as Project-app UI evidence. Use `docs/QA_EVIDENCE_20260429_PM_SCHED_FULL.md`. |
| 2026-04-29 | PM/Scheduling full QA sweep | Manual polling mode | Watch-mode declaration | INFO | Persistent background watch is not supported in this session; Codex is operating in manual polling mode at turn start and when Claude reports changes. |
| 2026-04-29 | PM/Scheduling full QA sweep | Port/process inspection | Prevent testing wrong repo | FAIL -> ROOT CAUSE FOUND | Project-app owns `7310/7311`; old handoff repo owns `7210/7211`. Existing Playwright config points at `7210/7211`, so earlier route smoke evidence is superseded for Project-app. |
| 2026-04-29 | PM/Scheduling full QA sweep | `node test-results/qa/QA_AUDIT_20260429_PM_SCHED_FULL/pm-scheduling-audit.mjs` | Route/density/visual/logic audit across PM + Scheduling | FAIL WITH FINDINGS | 15 routes/screens checked against Project-app `7310/7311`; 14 findings. Evidence: `docs/QA_EVIDENCE_20260429_PM_SCHED_FULL.md`. Cleanup: only QA screenshots/logs created; no seeded data deleted. |
| 2026-04-29 | Architecture cleanup QA guardrails | QA docs update only | Teach Codex/tester lanes to fail PM/Scheduling structure drift during `refactor/pm-scheduling-foundation` cleanup | PASS / DOCS ONLY | Added `ARCH-001` through `ARCH-008` to `LIVE_QA_TODO.md` and added/linked unique `QA-ARCH-009` through `QA-ARCH-011`, `QA-UI-*`, and `QA-BE-*` guardrails in `QA_REGRESSION_CHECKLIST.md`. Existing `QA-ARCH-001` through `QA-ARCH-008` from Claude's Batch 1 remain intact. No product code changed. |
---

## Known Commands

```bash
# Frontend build
cd webapp && npm run build

# Frontend type check
cd webapp && npm run type-check

# Backend build
cd Api && dotnet build

# Add EF migration
cd Data && dotnet ef migrations add <Name> --startup-project ../Api

# Regenerate NSwag client
cd Api && nswag run nswag.json
```

## Test Infrastructure Notes

- No backend test projects exist as of 2026-04-28.
- No frontend unit test setup found.
- Playwright e2e tests exist in `webapp/tests/e2e/`; use `SKIP_GLOBAL_SETUP=true` to avoid mutating demo data.
- Build checks (`dotnet build` and `npm run build`) are the primary automated gate.
- Manual browser verification is required for UI changes.



