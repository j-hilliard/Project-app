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
