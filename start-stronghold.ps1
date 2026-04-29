#Requires -Version 5.1
$ErrorActionPreference = 'Continue'
$AppRoot = $PSScriptRoot

function Get-StrongholdPortOwner {
    param([int]$Port)

    $conn = Get-NetTCPConnection -State Listen -LocalPort $Port -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($null -eq $conn) { return $null }

    $proc = Get-Process -Id $conn.OwningProcess -ErrorAction SilentlyContinue
    [pscustomobject]@{
        Port = $Port
        ProcessId = $conn.OwningProcess
        ProcessName = if ($proc) { $proc.ProcessName } else { "unknown" }
        Path = if ($proc) { $proc.Path } else { "" }
    }
}

Clear-Host
Write-Host ""
Write-Host "  STRONGHOLD ENTERPRISE ESTIMATING  -- Starting Up" -ForegroundColor Cyan
Write-Host ""

Write-Host "  Checking SQL Server Express..." -ForegroundColor Yellow
$svc = Get-Service -Name 'MSSQL$SQLEXPRESS' -ErrorAction SilentlyContinue
if ($null -eq $svc) {
    Write-Host "  ERROR: SQL Server Express not found." -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}
if ($svc.Status -ne 'Running') {
    Write-Host "  Starting SQL Server Express..." -ForegroundColor Yellow
    Start-Service 'MSSQL$SQLEXPRESS' -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 4
}
Write-Host "  SQL Server Express: OK" -ForegroundColor Green

$apiAlreadyRunning = $false
$apiOwner = Get-StrongholdPortOwner -Port 7211
if ($apiOwner) {
    if ($apiOwner.ProcessName -eq "node") {
        Write-Host "  WARNING: Node/Vite is occupying API port 7211. Stopping PID $($apiOwner.ProcessId) so the API can bind correctly." -ForegroundColor Yellow
        Stop-Process -Id $apiOwner.ProcessId -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
    } elseif ($apiOwner.ProcessName -in @("dotnet", "Api")) {
        Write-Host "  API port 7211 is already owned by $($apiOwner.ProcessName). Reusing existing API process PID $($apiOwner.ProcessId)." -ForegroundColor Green
        $apiAlreadyRunning = $true
    } else {
        Write-Host "  ERROR: Port 7211 is occupied by $($apiOwner.ProcessName) PID $($apiOwner.ProcessId)." -ForegroundColor Red
        Write-Host "  7211 is reserved for the .NET API. Stop that process before starting Stronghold." -ForegroundColor Red
        Read-Host "Press Enter to exit"
        exit 1
    }
}

Write-Host "  Starting API..." -ForegroundColor Yellow
if (-not $apiAlreadyRunning) {
    Start-Process powershell -WorkingDirectory $AppRoot -ArgumentList "-NoExit", "-Command", "dotnet run --project Api --launch-profile https"
}

Write-Host "  Starting Cloudflare tunnel..." -ForegroundColor Yellow
Stop-Process -Name "cloudflared" -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2
$cfLog = "$env:TEMP\cf-tunnel-$((Get-Date).Ticks).log"
Start-Process powershell -WindowStyle Minimized -ArgumentList "-NoExit", "-Command", "& 'C:\Program Files (x86)\cloudflared\cloudflared.exe' tunnel --url https://localhost:7211 2>&1 | Tee-Object -FilePath '$cfLog'"

$vueOwner = Get-StrongholdPortOwner -Port 7210
if ($vueOwner -and $vueOwner.ProcessName -ne "node") {
    Write-Host "  ERROR: Port 7210 is occupied by $($vueOwner.ProcessName) PID $($vueOwner.ProcessId)." -ForegroundColor Red
    Write-Host "  7210 is reserved for the Vue dev server. Stop that process before starting Stronghold." -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "  Starting Vue dev server..." -ForegroundColor Yellow
if ($vueOwner -and $vueOwner.ProcessName -eq "node") {
    Write-Host "  Vue port 7210 is already owned by node. Reusing existing Vue process PID $($vueOwner.ProcessId)." -ForegroundColor Green
} else {
    Start-Process powershell -WorkingDirectory "$AppRoot\webapp" -ArgumentList "-NoExit", "-Command", "npm run dev"
}

Write-Host "  Waiting for API..." -ForegroundColor Yellow
Add-Type -AssemblyName System.Net.Http
$handler = [System.Net.Http.HttpClientHandler]::new()
$handler.ServerCertificateCustomValidationCallback = [System.Net.Http.HttpClientHandler]::DangerousAcceptAnyServerCertificateValidator
$http = [System.Net.Http.HttpClient]::new($handler)
$http.Timeout = [TimeSpan]::FromSeconds(3)
$apiReady = $false
for ($i = 0; $i -lt 40; $i++) {
    Start-Sleep -Seconds 2
    try {
        $lb = [System.Net.Http.StringContent]::new('{"username":"x","password":"x"}', [System.Text.Encoding]::UTF8, 'application/json')
        $lr = $http.PostAsync('https://localhost:7211/api/auth/login', $lb).GetAwaiter().GetResult()
        if ([int]$lr.StatusCode -ge 200) { $apiReady = $true; break }
    } catch { }
}
$http.Dispose()
$apiOwner = Get-StrongholdPortOwner -Port 7211
if ($apiOwner -and $apiOwner.ProcessName -notin @("dotnet", "Api")) {
    Write-Host "  ERROR: API port 7211 is owned by $($apiOwner.ProcessName), not the Stronghold API. Data calls will fail." -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}
if ($apiReady) { Write-Host "  API: Ready" -ForegroundColor Green } else { Write-Host "  API still starting..." -ForegroundColor Yellow }

Write-Host "  Waiting for Vue..." -ForegroundColor Yellow
$h2 = [System.Net.Http.HttpClientHandler]::new()
$h2.ServerCertificateCustomValidationCallback = [System.Net.Http.HttpClientHandler]::DangerousAcceptAnyServerCertificateValidator
$http2 = [System.Net.Http.HttpClient]::new($h2)
$http2.Timeout = [TimeSpan]::FromSeconds(3)
for ($i = 0; $i -lt 20; $i++) {
    Start-Sleep -Seconds 2
    try { $null = $http2.GetAsync('https://localhost:7210').GetAwaiter().GetResult(); Write-Host "  Vue: Ready" -ForegroundColor Green; break } catch { }
}
$http2.Dispose()

Write-Host "  Waiting for tunnel URL..." -ForegroundColor Yellow
$tunnelUrl = $null
for ($i = 0; $i -lt 20; $i++) {
    Start-Sleep -Seconds 2
    if (Test-Path $cfLog) {
        $m = Select-String -Path $cfLog -Pattern "trycloudflare\.com" | Select-Object -First 1
        if ($m) { $tunnelUrl = ($m.Line -replace '.*https://', 'https://') -replace '\s.*', ''; break }
    }
}

$cfToken = $env:CF_API_TOKEN
if ($tunnelUrl -and $cfToken) {
    try {
        $sb = (@{ name = "TUNNEL_URL"; text = $tunnelUrl; type = "secret_text" } | ConvertTo-Json)
        $sr = Invoke-RestMethod -Uri "https://api.cloudflare.com/client/v4/accounts/95714f404b7d0fdfc428c954c99b46a0/workers/scripts/stronghold-agent/secrets" -Method PUT -Headers @{ Authorization = "Bearer $cfToken"; "Content-Type" = "application/json" } -Body $sb
        if ($sr.success) { Write-Host "  Worker URL updated." -ForegroundColor Green }
    } catch { Write-Host "  Worker update failed: $_" -ForegroundColor Yellow }
}

# Open API swagger first so browser accepts the dev cert for port 7211
# before the app makes cross-origin fetch requests to it.
Start-Process "https://localhost:7211/swagger"
Start-Sleep -Seconds 2
Start-Process "https://localhost:7210"

Write-Host ""
Write-Host "  App:  https://localhost:7210" -ForegroundColor Green
Write-Host "  API:  https://localhost:7211" -ForegroundColor Green
Write-Host "  Foundry: https://stronghold-agent.j-travishilliard.workers.dev" -ForegroundColor Cyan
Write-Host ""
Read-Host "Press Enter to close"
