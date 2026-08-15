<#
.SYNOPSIS
    Registers (or updates) the Windows Task Scheduler task that runs the nightly AutoCorrect pass.

.DESCRIPTION
    The Prose app is CLI/MCP-only (v3/Prose.Writer and v3/Prose.Codex were deleted in commit
    ed22bd4f6, "Command-line only") - there is no continuously-running host process to hang an
    in-process wall-clock scheduler off of, unlike ContinuityLongSweepService/
    SanityScanBackgroundService's PeriodicTimer pattern. Windows Task Scheduler is the trigger
    instead: a real OS-level daily trigger, native DST handling, and native "run as soon as
    possible after a missed start" catch-up (this machine isn't on 24/7, so a missed night is the
    normal case, not an edge case).

    This machine's local time zone is already Central (Get-TimeZone confirmed 2026-08-14), so a
    plain local-time trigger IS 3:00 AM Central with no conversion needed.

    Idempotent: if the task already exists, its trigger/action are updated in place rather than
    creating a duplicate. Safe to re-run after editing this script.

.NOTES
    Time changed from 2:00 AM to 3:00 AM Central 2026-08-14 (mid-build user request - the 2 AM
    window for the first night had already passed by the time this was being registered).

    Starts in --dry-run (see the generated scripts\run-autocorrect-nightly.ps1). Once you've
    reviewed a few mornings' `prose --morning-report --universe <slug>` output and are comfortable
    with what it WOULD have merged/fixed, edit run-autocorrect-nightly.ps1 to drop --dry-run (no
    need to re-run this registration script for that one-line change).
#>

$ErrorActionPreference = 'Stop'

$TaskName    = 'ProseAutoCorrectNightly'
$TriggerTime = '03:00'
$RepoRoot    = 'D:\Projects\MindAttic\Prose'
$CliProject  = Join-Path $RepoRoot 'v3\Prose.Cli'
$LogDir      = Join-Path $RepoRoot 'logs\autocorrect'

if (-not (Test-Path $LogDir)) {
    New-Item -ItemType Directory -Path $LogDir -Force | Out-Null
}

$tz = Get-TimeZone
if ($tz.Id -ne 'Central Standard Time') {
    Write-Warning "Machine time zone is '$($tz.Id)', not Central Standard Time. The task will still fire at $TriggerTime LOCAL time, which will NOT be 3:00 AM Central unless you adjust `$TriggerTime accordingly."
}

# dotnet run rebuilds on every invocation; wrap in a small script so Task Scheduler's action is a
# single command line and the CLI's own stdout/stderr land in a dated log file for the morning
# report / manual troubleshooting to reference.
$RunnerScript = Join-Path $RepoRoot 'scripts\run-autocorrect-nightly.ps1'
$runnerLines = @(
    '$ErrorActionPreference = ''Continue''',
    ('$logDir = ''' + $LogDir + ''''),
    '$logFile = Join-Path $logDir (''autocorrect_{0:yyyy-MM-dd_HHmmss}.log'' -f (Get-Date))',
    ('Set-Location ''' + $RepoRoot + ''''),
    # Starts in --dry-run per the rollout plan: the first several nights should only refresh
    # Findings and prove the pipeline runs clean against real data before any mutation is trusted
    # live. Drop --dry-run below once you've reviewed a few mornings' `prose --morning-report`
    # output (see script header .NOTES).
    ('dotnet run --project ''' + $CliProject + ''' -- --auto-correct-nightly --dry-run *>&1 | Tee-Object -FilePath $logFile')
)
Set-Content -Path $RunnerScript -Value $runnerLines -Encoding utf8

$argument = '-NoProfile -ExecutionPolicy Bypass -File "' + $RunnerScript + '"'
$action = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument $argument

$trigger = New-ScheduledTaskTrigger -Daily -At $TriggerTime

$settings = New-ScheduledTaskSettingsSet `
    -StartWhenAvailable `
    -DontStopOnIdleEnd `
    -ExecutionTimeLimit (New-TimeSpan -Hours 4) `
    -RestartCount 1 -RestartInterval (New-TimeSpan -Minutes 15)

$existing = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
if ($existing) {
    Write-Host "[register-autocorrect-task] Task '$TaskName' already exists - updating trigger/action in place."
    Set-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Settings $settings | Out-Null
}
else {
    Write-Host "[register-autocorrect-task] Registering new task '$TaskName' at $TriggerTime local (Central) daily."
    Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Settings $settings `
        -Description 'Nightly AutoCorrect pass for Prose: pure-ML/deterministic detection + a whitelisted set of auto-fixes (duplicate entity merge, dangling-edge cleanup, cross-book continuity majority resolution). Zero LLM calls. Undo via prose --auto-correct-undo. See v3/Prose.Core/Services/AutoCorrectOrchestratorService.cs.' `
        | Out-Null
}

Write-Host "[register-autocorrect-task] Done. Verify with: Get-ScheduledTask -TaskName $TaskName | Get-ScheduledTaskInfo"
Write-Host "[register-autocorrect-task] Logs land in: $LogDir"
Write-Host "[register-autocorrect-task] To run once manually right now: Start-ScheduledTask -TaskName $TaskName"
