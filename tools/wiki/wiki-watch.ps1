<#
.SYNOPSIS
    Keeps a universe's wiki and Prose in step, on change rather than on a clock (PRS-T1049). The Scheduler job
    Prose.GLMZ.Wiki.Sync runs it every 5 minutes; most runs find nothing pending and exit in a second.

.DESCRIPTION
    Two signals, neither of which touches the database to find out:
      Prose changed  the Hub stamps %LOCALAPPDATA%\MindAttic\Prose\wiki\changes\<universe>.stamp after every save that
                     changes what the wiki shows (Prose.Core WikiChangeSignal). Pending when the stamp is newer than
                     the last sync's start.
      The wiki changed  one MediaWiki API call (wikireconcile.py poll): an edit by anyone but the sync since the last
                     reconciliation check.

    When Prose changed: wait until writing has been quiet for -QuietMinutes (a writing session is one sync, not fifty),
    or until the first pending change is -MaxWaitMinutes old, then reconcile first (so a person's edit is held, never
    overwritten) and sync. When only the wiki changed: reconcile (people's edits become proposals on
    Project:Reconciliation). Once a day, a full sync and check run regardless, catching anything the stamps cannot see
    (raw SQL, a book exported by hand).

    State: watch.json in %LOCALAPPDATA%\MindAttic\Prose\wiki\<universe>\. Log: watch.log there (the sync's own is sync.log).
#>
param(
    [string]$Universe = 'glmz',
    [int]$QuietMinutes = 5,
    [int]$MaxWaitMinutes = 60,
    [int]$DailyHours = 24
)
$ErrorActionPreference = 'Stop'
$state = Join-Path $env:LOCALAPPDATA "MindAttic\Prose\wiki\$Universe"
$changes = Join-Path $env:LOCALAPPDATA 'MindAttic\Prose\wiki\changes'
$watchFile = Join-Path $state 'watch.json'
$log = Join-Path $state 'watch.log'
New-Item -ItemType Directory -Force -Path $state | Out-Null

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

# A sync already running owns the state folder: come back next tick.
$lockPath = Join-Path $state 'sync.lock'
if (Test-Path $lockPath) {
    try { $probe = [IO.File]::Open($lockPath, 'Open', 'ReadWrite', 'None'); $probe.Close() }
    catch [IO.IOException] { exit 0 }
}

$now = (Get-Date).ToUniversalTime()
$w = Read-Watch
$lastSync = Get-Utc $w.lastSyncStart

# ---- Prose: the newest stamp for this universe (or one the Hub could not name) --------------------------------------
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

# ---- the wiki: edits by people since the last check -------------------------------------------------------------------
$py = Join-Path $PSScriptRoot 'wikireconcile.py'
& python -I $py poll --universe $Universe 2>&1 | Out-Null
$wikiPending = $LASTEXITCODE -eq 10

$runSync = $false
if ($daily) { $runSync = $true; $why = "daily full sync (last: $(if ($lastSync) { $lastSync.ToString('u') } else { 'never' }))" }
elseif ($prosePending) {
    $quiet = ($now - $stamp).TotalMinutes -ge $QuietMinutes
    $waited = ($now - (Get-Utc $w.pendingSince)).TotalMinutes -ge $MaxWaitMinutes
    if ($quiet -or $waited) { $runSync = $true; $why = "Prose changed at $($stamp.ToString('u'))" + $(if (-not $quiet) { " (still writing; waited $MaxWaitMinutes min)" } else { '' }) }
}

if (-not $runSync -and -not $wikiPending) { exit 0 }

# Reconcile first: a person's edit is held before any sync could write over it.
if ($wikiPending -or $runSync) {
    Write-Log "reconciling: $(if ($wikiPending) { 'the wiki was edited' } else { 'before the sync' })"
    & python -I -u $py check --universe $Universe --tidy 2>&1 | ForEach-Object { Add-Content -Path $log -Value "  $_" -Encoding utf8 }
}
if ($runSync) {
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
        # And check again, so the report reflects the wiki as the sync left it.
        & python -I -u $py check --universe $Universe 2>&1 | ForEach-Object { Add-Content -Path $log -Value "  $_" -Encoding utf8 }
    } elseif ($code -eq 3) {
        Write-Log 'another sync is running; next tick'
    } else {
        Write-Log "sync FAILED ($code); see sync.log"
        exit $code
    }
}
