#Requires -Version 5.1
<#
.SYNOPSIS
    Creates or updates the desktop shortcuts for the MindAttic apps.

.DESCRIPTION
    One icon per app, each pointing at the generated `deploy.bat` in that app's deploy folder
    (C:\Apps\MindAttic\Prose\deploy.bat with the app as its argument; C:\Apps\<App>\deploy.bat for
    the other repos). deploy.bat opens a menu first: 1) Deploy & Run republishes from source (the
    repo's deploy.ps1 -Force) and then launches the app; 2) Run launches the deployed build as it
    is. When the repo is missing or the publish fails, deploy.bat still launches the existing build.

    deploy.bat is written by each repo's deploy.ps1. This script regenerates the Prose one first
    with `deploy.ps1 -LauncherOnly` (writes the launcher only - nothing is built or stopped), so
    the shortcuts always open the current launcher. On a fresh machine run the other repos'
    deploy.ps1 once before this script; entries whose deploy.bat does not exist yet are skipped.

    Idempotent - safe to re-run. It rewrites the target/icon of a shortcut it already owns and
    reports what it changed. Entries whose .bat or icon is missing are skipped with a reason
    rather than silently producing a broken icon.

.PARAMETER Desktop
    Where to write. Defaults to the real Desktop, which is redirected on this machine, so resolve
    it rather than assuming $env:USERPROFILE\Desktop.

.PARAMETER WhatIf
    Show what would change without touching anything.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File src\tools\install-shortcuts.ps1
    powershell -ExecutionPolicy Bypass -File src\tools\install-shortcuts.ps1 -WhatIf
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$Desktop = [Environment]::GetFolderPath('Desktop')
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$mindAttic = 'D:\Projects\MindAttic'
$prose     = 'C:\Apps\MindAttic\Prose\deploy.bat'
$proseDeploy = Join-Path $mindAttic 'Prose\deploy.ps1'

# Name is the .lnk's filename AND its label, so changing one renames the icon on the desktop.
# Args is passed to deploy.bat: the Prose deploy.bat takes the app; no argument offers to deploy
# all of them, then opens the Launcher.
$shortcuts = @(
    @{ Name = 'Prose Writer'; Bat = $prose;                          Args = 'Writer'; Icon = "$mindAttic\Prose\assets\writer.ico"
       Desc = 'Deploy & Run, or Run, the Prose book editor' }
    @{ Name = 'Prose Hub';    Bat = $prose;                          Args = 'Hub';    Icon = "$mindAttic\Prose\assets\hub.ico"
       Desc = 'Deploy & Run, or Run, the Prose Hub (serves /writer and /repo)' }
    @{ Name = 'Deploy Prose'; Bat = $prose;                          Args = '';       Icon = "$mindAttic\Prose\assets\M.ico"
       Desc = 'Deploy & Run every Prose app, or Run the launcher' }
    @{ Name = 'KdpPublish';   Bat = 'C:\Apps\KdpPublish\deploy.bat'; Args = '';       Icon = "$mindAttic\Prose\assets\kdp.ico"
       Desc = 'Deploy & Run, or Run, KdpPublish' }
    @{ Name = 'IdiotProof';   Bat = 'C:\Apps\IdiotProof\deploy.bat'; Args = '';       Icon = "$mindAttic\IdiotProof\idiotproof.ico"
       Desc = 'Deploy & Run, or Run, IdiotProof (Monitor + Blazor)' }
)

if (-not (Test-Path $Desktop)) {
    Write-Host "  Desktop not found at $Desktop" -ForegroundColor Red
    exit 1
}

# Regenerate the Prose launcher so the shortcuts open the current one. -LauncherOnly writes the
# .bat and exits: no build, no process stopped.
if ((Test-Path $proseDeploy) -and $PSCmdlet.ShouldProcess($prose, 'Regenerate launcher')) {
    & powershell -NoProfile -ExecutionPolicy Bypass -File $proseDeploy -LauncherOnly | Out-Host
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  Could not regenerate $prose (exit $LASTEXITCODE) - keeping the existing one." -ForegroundColor DarkYellow
    }
}

Write-Host ''
Write-Host "  Desktop shortcuts -> $Desktop" -ForegroundColor Cyan
Write-Host ''

$shell   = New-Object -ComObject WScript.Shell
$changed = 0
$skipped = 0

foreach ($s in $shortcuts) {
    if (-not (Test-Path $s.Bat)) {
        Write-Host ("    {0,-14} skipped - no {1}" -f $s.Name, $s.Bat) -ForegroundColor DarkYellow
        $skipped++
        continue
    }

    $lnk     = Join-Path $Desktop "$($s.Name).lnk"
    $existed = Test-Path $lnk

    # An icon is cosmetic: a missing .ico is not a reason to skip the shortcut itself.
    $icon = if (Test-Path $s.Icon) { "$($s.Icon),0" } else { "$($s.Bat),0" }
    if (-not (Test-Path $s.Icon)) {
        Write-Host ("    {0,-14} note - icon not found at {1}" -f '', $s.Icon) -ForegroundColor DarkYellow
    }

    if (-not $PSCmdlet.ShouldProcess($lnk, $(if ($existed) { 'Update shortcut' } else { 'Create shortcut' }))) { continue }

    $sc = $shell.CreateShortcut($lnk)
    $sc.TargetPath       = $s.Bat
    $sc.Arguments        = $s.Args
    $sc.WorkingDirectory = Split-Path $s.Bat
    $sc.IconLocation     = $icon
    $sc.Description      = $s.Desc
    $sc.Save()

    $verb = if ($existed) { 'updated' } else { 'created' }
    Write-Host ("    {0,-14} {1,-8} -> {2} {3}" -f $s.Name, $verb, $s.Bat, $s.Args) -ForegroundColor Gray
    $changed++
}

Write-Host ''
Write-Host "  $changed shortcut(s) written$(if ($skipped) { ", $skipped skipped" })." -ForegroundColor Green
Write-Host ''
