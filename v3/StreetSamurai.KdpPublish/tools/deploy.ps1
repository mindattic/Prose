#Requires -Version 5.1
<#
.SYNOPSIS
    Shuts down any running KdpPublish, clears build cache, rebuilds, and publishes a
    standalone copy to C:\Apps\KdpPublish\ — same process IdiotProof uses
    (tools/publish-all.ps1) to always have a fresh, independent deployed copy.

.DESCRIPTION
    1. Stops any running StreetSamurai.KdpPublish.exe process.
    2. Removes bin/obj for the KdpPublish + Core + Shared projects — a clean, timestamp-
       independent rebuild, not an incremental one. This is the BUILD cache only; the
       WebView2 user-data folders under %LocalAppData%\MindAttic\KdpPublish\ (which hold
       the KDP login session) are never touched, so the Amazon login survives redeploys.
    3. dotnet publish (Release, win-x64, framework-dependent single file) -> C:\Apps\KdpPublish\.
    4. Writes C:\Apps\KdpPublish\launch.bat — a standalone double-click launcher that
       doesn't depend on this repo existing at all.

.PARAMETER Launch
    After publishing, immediately run C:\Apps\KdpPublish\launch.bat.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\deploy.ps1
    powershell -ExecutionPolicy Bypass -File tools\deploy.ps1 -Launch
#>
param([switch]$Launch)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$projDir = Split-Path $PSScriptRoot   # tools\ -> StreetSamurai.KdpPublish\
$proj    = Join-Path $projDir 'StreetSamurai.KdpPublish.csproj'
$out     = 'C:\Apps\KdpPublish'
$exeName = 'StreetSamurai.KdpPublish.exe'

# ── Stop running instance ──────────────────────────────────────────────────
Write-Host ''
Write-Host '  Stopping running instance...' -ForegroundColor Yellow
$procs = Get-Process 'StreetSamurai.KdpPublish' -ErrorAction SilentlyContinue
if ($procs) {
    $procs | Stop-Process -Force
    Write-Host "    Stopped ($(@($procs).Count) process(es))" -ForegroundColor DarkYellow
    Start-Sleep -Milliseconds 800
} else {
    Write-Host '    Nothing running.' -ForegroundColor DarkGray
}

# ── Clear build cache ──────────────────────────────────────────────────────
# bin/obj only — never the WebView2 user-data folders, which hold the KDP login
# session and must survive a redeploy exactly as they survive a normal rebuild.
Write-Host ''
Write-Host '  Clearing build cache (bin/obj)...' -ForegroundColor Cyan
foreach ($rel in @('StreetSamurai.KdpPublish', 'StreetSamurai.Core', 'StreetSamurai.Shared')) {
    $projRoot = Resolve-Path (Join-Path $projDir "..\$rel") -ErrorAction SilentlyContinue
    if (-not $projRoot) { continue }
    foreach ($d in @((Join-Path $projRoot 'bin'), (Join-Path $projRoot 'obj'))) {
        if (Test-Path $d) {
            try { Remove-Item -Recurse -Force $d -ErrorAction Stop }
            catch { Write-Host "    (could not fully remove $d : $($_.Exception.Message))" -ForegroundColor DarkYellow }
        }
    }
}

# ── Publish ─────────────────────────────────────────────────────────────────
Write-Host ''
Write-Host "  Publishing $proj" -ForegroundColor Cyan
Write-Host "    -> $out\$exeName"

dotnet publish $proj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained false `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    --output $out | Out-Host

if ($LASTEXITCODE -ne 0) {
    Write-Host "Publish failed (exit $LASTEXITCODE)." -ForegroundColor Red
    exit $LASTEXITCODE
}

$exe = Join-Path $out $exeName
if (-not (Test-Path $exe)) {
    Write-Host "Publish completed but $exeName not found at $exe." -ForegroundColor Red
    exit 1
}

# ── Write standalone launcher ──────────────────────────────────────────────
# Doesn't depend on this repo existing — C:\Apps\KdpPublish\ is fully self-sufficient.
$launchBatPath = Join-Path $out 'launch.bat'
@(
    '@echo off',
    'title KdpPublish',
    "start `"`" `"%~dp0$exeName`""
) | Set-Content $launchBatPath -Encoding ascii

# ── Summary ────────────────────────────────────────────────────────────────
Write-Host ''
Write-Host '  Published successfully.' -ForegroundColor Green
Write-Host "    Exe    : $exe ($([math]::Round(((Get-Item $exe).Length / 1MB), 1)) MB)" -ForegroundColor Gray
Write-Host "    Launch : $launchBatPath" -ForegroundColor Gray
Write-Host ''

if ($Launch) {
    Write-Host '  Launching...' -ForegroundColor Cyan
    Start-Process $launchBatPath
}
