<#
  Stop scheduled tasks stealing focus.

  THE PROBLEM
  A scheduled task whose principal LogonType is "Interactive" runs on your desktop. If its action
  is a console executable, Windows gives it a real console window — which appears, takes focus,
  and disappears. MindAttic.Automata.Tick repeats every 5 minutes, all day, so that is a stolen
  keystroke every five minutes forever.

  THE FIX
  LogonType S4U — "Run whether user is logged on or not", without storing a password. The task
  runs in a non-interactive session, so no window is ever created. Nothing else about it changes:
  same user, same privilege level, same schedule, same executable.

  This is not a workaround. MindAtticBobNightlyBackup on this machine is already S4U and is the
  reason it has never flashed. This just brings the remaining offender into line with it.

  WHAT S4U COSTS YOU — read this before running
  An S4U task gets a local logon token with no network credentials. Local resources (localhost
  SQL Server, the filesystem, loopback HTTP) work exactly as before; a task that authenticates to
  a REMOTE machine with your Windows identity would start failing. The tick runner does not look
  like it does that, and the script verifies it still exits 0 afterwards — but that is the one
  behaviour worth knowing about.

  An S4U task also runs when you are logged off, where an Interactive one does not. For a "tick"
  runner that is usually wanted; if it is not, say so and use the -WrapInConhost switch instead,
  which keeps Interactive and merely hides the console.

  RUN IT AS ADMINISTRATOR. Changing a task's principal is denied otherwise.

  ROLLBACK — one line per task, also elevated:
    $t = Get-ScheduledTask -TaskName 'MindAttic.Automata.Tick'
    Set-ScheduledTask -TaskName 'MindAttic.Automata.Tick' -Principal (
      New-ScheduledTaskPrincipal -UserId $t.Principal.UserId -LogonType Interactive -RunLevel Limited)
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    # Report what would change and touch nothing.
    [switch]$WhatIfOnly,

    # Keep Interactive and wrap the action in `conhost --headless` instead. Suppresses the window
    # without changing credentials or logged-off behaviour. Use this if S4U breaks something.
    [switch]$WrapInConhost
)

$ErrorActionPreference = 'Stop'

$Tasks = @(
    # The only Interactive task left. The other two that were here are gone, not fixed:
    # StreetSamurai-NightlyFactSweep pointed at a deleted repo and had been exiting 1 nightly;
    # ProseAutoCorrectNightly wrote prose unattended, which is what RFC 0009 forbids. Both were
    # unregistered 2026-09-20 on the author's instruction.
    'MindAttic.Automata.Tick'          # every 5 minutes — the one you actually notice
)

function Test-Elevated {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    (New-Object Security.Principal.WindowsPrincipal $id).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not $WhatIfOnly -and -not (Test-Elevated)) {
    Write-Warning 'Not elevated. Re-run this in an Administrator PowerShell, or pass -WhatIfOnly to preview.'
    Write-Output  '  Start-Process powershell -Verb RunAs -ArgumentList ''-NoProfile'',''-File'',''D:\Projects\MindAttic\Prose\scripts\fix-task-focus-stealing.ps1'''
    return
}

Write-Output ''
Write-Output 'Task                            Before        After         Verify'
Write-Output '------------------------------  ------------  ------------  ------------------------'

foreach ($name in $Tasks) {
    $task = Get-ScheduledTask -TaskName $name -ErrorAction SilentlyContinue
    if (-not $task) {
        Write-Output ('{0,-30}  {1}' -f $name, 'not present on this machine — skipped')
        continue
    }

    $before = $task.Principal.LogonType

    if ($before -eq 'S4U' -and -not $WrapInConhost) {
        Write-Output ('{0,-30}  {1,-12}  {2,-12}  {3}' -f $name, $before, $before, 'already correct')
        continue
    }

    if ($WhatIfOnly) {
        $after = if ($WrapInConhost) { 'Interactive+conhost' } else { 'S4U' }
        Write-Output ('{0,-30}  {1,-12}  {2,-12}  {3}' -f $name, $before, $after, '(preview only)')
        continue
    }

    try {
        if ($WrapInConhost) {
            # Keeps the interactive token; conhost --headless allocates no visible console.
            $actions = @()
            foreach ($a in $task.Actions) {
                if ($a.Execute -match 'conhost') { $actions += $a; continue }
                $inner = '"{0}"' -f $a.Execute
                if ($a.Arguments) { $inner += ' ' + $a.Arguments }
                $actions += New-ScheduledTaskAction -Execute 'conhost.exe' `
                    -Argument ('--headless ' + $inner) -WorkingDirectory $a.WorkingDirectory
            }
            Set-ScheduledTask -TaskName $name -Action $actions | Out-Null
            $after = 'Interactive+conhost'
        }
        else {
            $principal = New-ScheduledTaskPrincipal `
                -UserId $task.Principal.UserId `
                -LogonType S4U `
                -RunLevel $task.Principal.RunLevel
            Set-ScheduledTask -TaskName $name -Principal $principal | Out-Null
            $after = (Get-ScheduledTask -TaskName $name).Principal.LogonType
        }
    }
    catch {
        Write-Output ('{0,-30}  {1,-12}  {2,-12}  {3}' -f $name, $before, 'FAILED', $_.Exception.Message)
        continue
    }

    # Prove it still works. A task that stopped stealing focus by stopping working is not a fix,
    # and the whole risk of S4U is a credential change that only shows up at run time.
    $verify = 'not run'
    try {
        Start-ScheduledTask -TaskName $name
        $deadline = (Get-Date).AddSeconds(90)
        do {
            Start-Sleep -Seconds 2
            $info = Get-ScheduledTaskInfo -TaskName $name
            $state = (Get-ScheduledTask -TaskName $name).State
        } while ($state -eq 'Running' -and (Get-Date) -lt $deadline)

        $verify = if ($state -eq 'Running') { 'still running after 90s' }
                  elseif ($info.LastTaskResult -eq 0) { 'ran OK (exit 0)' }
                  else { 'EXIT {0} — consider rollback' -f $info.LastTaskResult }
    }
    catch { $verify = 'could not verify: ' + $_.Exception.Message }

    Write-Output ('{0,-30}  {1,-12}  {2,-12}  {3}' -f $name, $before, $after, $verify)
}

Write-Output ''
Write-Output 'Done. Nothing in the Prose codebase spawns a visible console — every ProcessStartInfo'
Write-Output 'in v3 already sets CreateNoWindow. The Hub keeps its own console on purpose, so you'
Write-Output 'can read what it printed when it will not start; that one is meant to be there.'
