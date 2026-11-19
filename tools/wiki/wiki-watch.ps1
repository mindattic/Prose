<#
.SYNOPSIS
    Keeps a universe's wiki and Prose in step, on change rather than on a clock (PRS-T1049, PRS-T1064).

.DESCRIPTION
    Two signals, neither of which touches the database to find out:
      Prose changed  the Hub stamps %LOCALAPPDATA%\MindAttic\Prose\wiki\changes\<universe>.stamp after every save that
                     changes what the wiki shows (Prose.Core WikiChangeSignal). Pending when the stamp is newer than
                     the last sync's start.
      The wiki changed  one MediaWiki API call (wikireconcile.py poll): an edit by anyone but the sync since the last
                     reconciliation check. (The wiki is on shared hosting that cannot call this PC, so this side asks.)

    When Prose changed: wait until writing has been quiet for -QuietMinutes (a writing session is one sync, not fifty),
    or until the first pending change is -MaxWaitMinutes old, then reconcile first (so a person's edit is held, never
    overwritten) and sync. When only the wiki changed: reconcile (people's edits become proposals on
    GLMZ Wiki:Reconciliation). Once a day, a full sync and check run regardless, catching anything the stamps cannot
    see (raw SQL, a write from a process without the signal).

    -Daemon: stay resident (the Scheduler starts it at logon and keeps it running). A FileSystemWatcher on the stamp
    folder wakes it the moment the Hub stamps a change; between stamps it looks at the wiki every -WikiPollSeconds.
    Without -Daemon it runs one tick and exits (a manual run, or the old 5-minute Scheduler tick).

    State: watch.json in %LOCALAPPDATA%\MindAttic\Prose\wiki\<universe>\. Log: watch.log there (the sync's own is sync.log).
#>
param(
    [string]$Universe = 'glmz',
    [int]$QuietMinutes = 2,
    [int]$MaxWaitMinutes = 30,
    [int]$DailyHours = 24,
    [switch]$Daemon,
    [int]$WikiPollSeconds = 60
)
$ErrorActionPreference = 'Stop'
$state = Join-Path $env:LOCALAPPDATA "MindAttic\Prose\wiki\$Universe"
$changes = Join-Path $env:LOCALAPPDATA 'MindAttic\Prose\wiki\changes'
$watchFile = Join-Path $state 'watch.json'
$log = Join-Path $state 'watch.log'
$py = Join-Path $PSScriptRoot 'wikireconcile.py'
New-Item -ItemType Directory -Force -Path $state, $changes | Out-Null

function Write-Log([string]$Text) {
    $line = "[$((Get-Date).ToString('yyyy-MM-dd HH:mm:ss'))] $Text"
    Write-Host $line
    Add-Content -Path $log -Value $line -Encoding utf8
}
function Read-Watch {
    if (Test-Path $watchFile) { try { return Get-Content $watchFile -Raw | ConvertFrom-Json } catch { } }
    return [pscustomobject]@{}
}
function Save-Watch($w) { $w | ConvertTo-Json | Set-Content $watchFile -Encoding utf8 }
function Set-Field($w, [string]$Name, $Value) {
    if ($w.PSObject.Properties[$Name]) { $w.$Name = $Value } else { $w | Add-Member -NotePropertyName $Name -NotePropertyValue $Value }
}
function Get-Utc([string]$Text) {
    if (-not $Text) { return $null }
    return [datetime]::Parse($Text.Trim(), [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::AdjustToUniversal -bor [Globalization.DateTimeStyles]::AssumeUniversal)
}
function Test-SyncRunning {
    $lockPath = Join-Path $state 'sync.lock'
    if (-not (Test-Path $lockPath)) { return $false }
    try { $probe = [IO.File]::Open($lockPath, 'Open', 'ReadWrite', 'None'); $probe.Close(); return $false }
    catch [IO.IOException] { return $true }
}
function Invoke-Reconcile([string[]]$Extra) {
    & python -I -u $py check --universe $Universe @Extra 2>&1 | ForEach-Object { Add-Content -Path $log -Value "  $_" -Encoding utf8 }
}

# One look at both sides; syncs and reconciles when something is pending. Returns the sync's exit code (0 = fine).
function Invoke-Tick([bool]$PollWiki = $true) {
    if (Test-SyncRunning) { return 0 }  # a sync already running owns the state folder: next time
    $now = (Get-Date).ToUniversalTime()
    $w = Read-Watch
    $lastSync = Get-Utc $w.lastSyncStart

    # Prose: the newest stamp for this universe (or one the Hub could not name)
    $stamp = $null
    foreach ($name in "$($Universe.ToLowerInvariant()).stamp", '_unknown.stamp') {
        $f = Join-Path $changes $name
        if (Test-Path $f) {
            $t = Get-Utc (Get-Content $f -Raw)
            if ($t -and (-not $stamp -or $t -gt $stamp)) { $stamp = $t }
        }
    }
    $prosePending = $stamp -and (-not $lastSync -or $stamp -gt $lastSync)
    if ($prosePending -and -not $w.PSObject.Properties['pendingSince']) { Set-Field $w 'pendingSince' $now.ToString('o'); Save-Watch $w }
    $daily = -not $lastSync -or ($now - $lastSync).TotalHours -ge $DailyHours

    # The wiki: edits by people since the last check
    $wikiPending = $false
    if ($PollWiki) {
        & python -I $py poll --universe $Universe 2>&1 | Out-Null
        $wikiPending = $LASTEXITCODE -eq 10
    }

    $runSync = $false; $why = ''
    if ($daily) { $runSync = $true; $why = "daily full sync (last: $(if ($lastSync) { $lastSync.ToString('u') } else { 'never' }))" }
    elseif ($prosePending) {
        $quiet = ($now - $stamp).TotalMinutes -ge $QuietMinutes
        $waited = ($now - (Get-Utc $w.pendingSince)).TotalMinutes -ge $MaxWaitMinutes
        if ($quiet -or $waited) { $runSync = $true; $why = "Prose changed at $($stamp.ToString('u'))" + $(if (-not $quiet) { " (still writing; waited $MaxWaitMinutes min)" } else { '' }) }
    }
    if (-not $runSync -and -not $wikiPending) { return 0 }

    # Reconcile first: a person's edit is held before any sync could write over it.
    Write-Log "reconciling: $(if ($wikiPending) { 'the wiki was edited' } else { 'before the sync' })"
    Invoke-Reconcile @('--tidy')
    if (-not $runSync) { return 0 }

    Write-Log "syncing: $why"
    $start = (Get-Date).ToUniversalTime()
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'sync-wiki.ps1') -Universe $Universe
    $code = $LASTEXITCODE
    if ($code -eq 0) {
        $w = Read-Watch
        Set-Field $w 'lastSyncStart' $start.ToString('o')
        if ($w.PSObject.Properties['pendingSince']) { $w.PSObject.Properties.Remove('pendingSince') }
        Save-Watch $w
        Write-Log 'sync done'
        Invoke-Reconcile @()  # the report reflects the wiki as the sync left it
    } elseif ($code -eq 3) {
        Write-Log 'another sync is running; next time'
        return 0
    } else {
        Write-Log "sync FAILED ($code); see sync.log"
    }
    return $code
}

if (-not $Daemon) {
    exit (Invoke-Tick)
}

# ---- resident: woken by the Hub's stamp, the wiki looked at every minute ---------------------------------------------
$mutex = New-Object Threading.Mutex($false, "Global\MindAttic.Prose.WikiWatch.$Universe")
if (-not $mutex.WaitOne(0)) { Write-Host "a wiki watcher for '$Universe' is already running."; exit 0 }
$fsw = New-Object IO.FileSystemWatcher $changes, '*.stamp'
$fsw.NotifyFilter = [IO.NotifyFilters]'LastWrite, FileName'
$fsw.EnableRaisingEvents = $true
Register-ObjectEvent $fsw Changed -SourceIdentifier WikiStamp | Out-Null
Register-ObjectEvent $fsw Created -SourceIdentifier WikiStampNew | Out-Null
Write-Log "watcher started (push from the Hub's stamp; the wiki every $WikiPollSeconds s; quiet $QuietMinutes min)"
$lastWikiPoll = [datetime]::MinValue
try {
    while ($true) {
        # Wake on a stamp, or after a short wait so a pending Prose change is synced once writing goes quiet.
        $e = Wait-Event -Timeout 20
        while ($e) { Remove-Event -EventIdentifier $e.EventIdentifier; $e = Get-Event | Select-Object -First 1 }
        $pollWiki = ((Get-Date) - $lastWikiPoll).TotalSeconds -ge $WikiPollSeconds
        if ($pollWiki) { $lastWikiPoll = Get-Date }
        try { [void](Invoke-Tick $pollWiki) } catch { Write-Log "tick failed: $($_.Exception.Message)" }
    }
} finally {
    Unregister-Event WikiStamp -ErrorAction SilentlyContinue
    Unregister-Event WikiStampNew -ErrorAction SilentlyContinue
    $fsw.Dispose(); $mutex.ReleaseMutex()
}
