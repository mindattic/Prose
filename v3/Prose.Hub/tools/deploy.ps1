#Requires -Version 5.1
<#
.SYNOPSIS
    Deploys Prose.Hub. Forwards to v3\tools\deploy-apps.ps1.

.DESCRIPTION
    Superseded 2026-09-11. The Hub used to be the only deployed Prose executable and owned its own
    folder at C:\Apps\Prose\Prose.Hub\. It now publishes as Hub.exe into C:\Apps\MindAttic\Prose\
    alongside Writer.exe, Launcher.exe and KdpPublish\, and one script handles all of them.

    Kept as a forwarder rather than deleted because several things call it by this exact path -
    the SessionStart hook's older copies, launch.bat files already written into C:\Apps\, and the
    /deploy runbook. Those keep working; new work should call deploy-apps.ps1 directly.

.PARAMETER Launch
    Start the Prose launcher after publishing.
#>
param([switch]$Launch)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$deployApps = Join-Path (Split-Path (Split-Path $PSScriptRoot)) 'tools\deploy-apps.ps1'
if (-not (Test-Path $deployApps)) {
    Write-Host "deploy-apps.ps1 not found at $deployApps" -ForegroundColor Red
    exit 1
}

Write-Host ''
Write-Host '  This script now forwards to v3\tools\deploy-apps.ps1 -Apps Hub.' -ForegroundColor DarkGray

$forward = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $deployApps, '-Apps', 'Hub')
if ($Launch) { $forward += '-Launch' }

& powershell @forward
exit $LASTEXITCODE
