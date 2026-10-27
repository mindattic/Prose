#Requires -Version 5.1
<#
.SYNOPSIS
    Deploys Prose.Hub. Forwards to the repo-root run.ps1 -Apps Hub.

.DESCRIPTION
    Superseded 2026-09-11. The Hub used to be the only deployed Prose executable and owned its own
    folder at C:\Apps\Prose\Prose.Hub\. It now publishes as Hub.exe into C:\Apps\MindAttic\Prose\
    alongside Writer.exe and Launcher.exe, and one script (run.ps1 at the repo root) handles all
    of them.

    Kept as a forwarder rather than deleted because older callers use this exact path (the
    SessionStart hook's older copies, the /deploy runbook). New work should call the root
    run.ps1, or C:\Apps\MindAttic\Prose\run.bat Hub.

.PARAMETER Launch
    Start the Prose launcher after deploying.

.PARAMETER Force
    Clean rebuild even if nothing changed.
#>
param([switch]$Launch, [switch]$Force)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# tools\ -> Prose.Hub\ -> src\ -> repo root
$deployPs1 = Join-Path (Split-Path (Split-Path (Split-Path $PSScriptRoot))) 'run.ps1'
if (-not (Test-Path $deployPs1)) {
    Write-Host "run.ps1 not found at $deployPs1" -ForegroundColor Red
    exit 1
}

Write-Host ''
Write-Host '  This script now forwards to the repo-root run.ps1 -Apps Hub.' -ForegroundColor DarkGray

$forward = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $deployPs1, '-Apps', 'Hub')
if ($Launch) { $forward += '-Launch' }
if ($Force)  { $forward += '-Force' }

& powershell @forward
exit $LASTEXITCODE
