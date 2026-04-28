@echo off
echo Starting Stronghold API + Cloudflare tunnel...

:: 7211 is reserved for the .NET API. If Vite/Node grabs it, the UI loads with no data.
powershell -NoProfile -ExecutionPolicy Bypass -Command "$c=Get-NetTCPConnection -State Listen -LocalPort 7211 -ErrorAction SilentlyContinue | Select-Object -First 1; if ($c) { $p=Get-Process -Id $c.OwningProcess -ErrorAction SilentlyContinue; if ($p.ProcessName -eq 'node') { Write-Host 'Node/Vite is occupying API port 7211. Stopping it.' -ForegroundColor Yellow; Stop-Process -Id $c.OwningProcess -Force; Start-Sleep -Seconds 2 } elseif ($p.ProcessName -ne 'dotnet') { Write-Host ('ERROR: Port 7211 is occupied by ' + $p.ProcessName + ' PID ' + $c.OwningProcess) -ForegroundColor Red; exit 1 } }"
if errorlevel 1 (
  echo.
  echo Could not start safely because API port 7211 is occupied.
  pause
  exit /b 1
)

:: Start API
start "Stronghold API" /D "%~dp0Api" cmd /c "set ASPNETCORE_ENVIRONMENT=Local && dotnet run"

:: Wait for API to be ready
timeout /t 10 /nobreak >nul

:: Start tunnel and capture URL
start "Cloudflare Tunnel" cmd /c ""C:\Program Files (x86)\cloudflared\cloudflared.exe" tunnel --url https://localhost:7211 2>&1 | tee %TEMP%\cf-tunnel.log"

echo.
echo API starting on https://localhost:7211
echo Tunnel starting... check the "Cloudflare Tunnel" window for the trycloudflare.com URL
echo Copy that URL and update the Foundry tool server URL.
echo.
pause
