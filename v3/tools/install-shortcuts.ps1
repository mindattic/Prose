#Requires -Version 5.1
<#
.SYNOPSIS
    Creates or updates the desktop shortcuts for the MindAttic apps.

.DESCRIPTION
    One icon per app, each pointing at that repo's `deploy-<app>.bat`, so double-clicking always
    republishes from source into C:\Apps\ and then launches the build it just made. You can never
    open a stale exe by accident - which is the failure this convention exists to prevent
    (KdpPublish's own launcher header records a three-day-stale build silently missing a feature).

    Shortcuts point at the REPO, never into C:\Apps\. That folder is a build output: it gets wiped
    and regenerated, and an icon pointing into it breaks the first time that happens.

    Idempotent - safe to re-run. It rewrites the target/icon of a shortcut it already owns and
    reports what it changed. Entries whose .bat or icon is missing are skipped with a reason
    rather than silently producing a broken icon.

.PARAMETER Desktop
    Where to write. Defaults to the real Desktop, which is redirected on this machine, so resolve
    it rather than assuming $env:USERPROFILE\Desktop.

.PARAMETER WhatIf
    Show what would change without touching anything.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File v3\tools\install-shortcuts.ps1
    powershell -ExecutionPolicy Bypass -File v3\tools\install-shortcuts.ps1 -WhatIf
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$Desktop = [Environment]::GetFolderPath('Desktop')
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$mindAttic = 'D:\Projects\MindAttic'

# Name is the .lnk's filename AND its label, so changing one renames the icon on the desktop.
$shortcuts = @(
    @{ Name = 'Prose Writer'; Bat = "$mindAttic\Prose\deploy-writer.bat";          Icon = "$mindAttic\Prose\assets\writer.ico"
       Desc = 'Republish and open the Prose book editor' }
    @{ Name = 'Prose Hub';    Bat = "$mindAttic\Prose\deploy-hub.bat";             Icon = "$mindAttic\Prose\assets\hub.ico"
       Desc = 'Republish and start the Prose Hub (serves /writer and /repo)' }
    @{ Name = 'Prose';        Bat = "$mindAttic\Prose\deploy-prose.bat";           Icon = "$mindAttic\Prose\assets\M.ico"
       Desc = 'Republish all Prose apps and open the launcher' }
    @{ Name = 'KdpPublish';   Bat = "$mindAttic\Prose\deploy-kdp.bat";             Icon = "$mindAttic\Prose\assets\kdp.ico"
       Desc = 'Republish and open KdpPublish' }
    @{ Name = 'IdiotProof';   Bat = "$mindAttic\IdiotProof\deploy-idiotproof.bat"; Icon = "$mindAttic\IdiotProof\idiotproof.ico"
       Desc = 'Republish and start IdiotProof (Monitor + Blazor)' }
)

if (-not (Test-Path $Desktop)) {
    Write-Host "  Desktop not found at $Desktop" -ForegroundColor Red
    exit 1
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
    $sc.WorkingDirectory = Split-Path $s.Bat
    $sc.IconLocation     = $icon
    $sc.Description      = $s.Desc
    $sc.Save()

    $verb = if ($existed) { 'updated' } else { 'created' }
    Write-Host ("    {0,-14} {1,-8} -> {2}" -f $s.Name, $verb, $s.Bat) -ForegroundColor Gray
    $changed++
}

Write-Host ''
Write-Host "  $changed shortcut(s) written$(if ($skipped) { ", $skipped skipped" })." -ForegroundColor Green
Write-Host ''
