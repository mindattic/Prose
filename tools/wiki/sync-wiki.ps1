<#
.SYNOPSIS
    Syncs a Prose universe into its MediaWiki: every entity, relationship and canon-book appearance, one command.

.DESCRIPTION
    1. Makes sure the Prose Hub is up (starts C:\Apps\MindAttic\Prose\Hub.exe if not).
    2. Through the Hub: --export global (every live entity), --universe-export (edges), and --read-beats for each
       canon book in tools\wiki\<universe>.books.json (which entities each book names).
    3. With -Retag, --tag-entity-text as a dry run, so entity text links to the entities it names. It reads every
       entity, which is slow on Azure and spends serverless/free-offer capacity, so by default the last result is reused.
    4. tools\wiki\prose2wiki.py builds the pages, pushes only what changed, deletes pages of entities that no longer
       exist, redirects renamed ones, purges, and writes the validation report (repair.md / repair.json).

    State lives in %LOCALAPPDATA%\MindAttic\Prose\wiki\<universe>\, so every run is incremental. The wiki login is
    the Vault bucket %APPDATA%\MindAttic\Wiki\<universe>.json (api, site, user, password).

.EXAMPLE
    powershell -File tools\wiki\sync-wiki.ps1 -Universe glmz
    powershell -File tools\wiki\sync-wiki.ps1 -Universe glmz -Retag
    powershell -File tools\wiki\sync-wiki.ps1 -Universe glmz -NoPush      # build and report only
#>
param(
    [Parameter(Mandatory)][string]$Universe,
    [string]$DisplayName = $Universe.ToUpperInvariant(),
    [string]$Theme = 'cyberspace',
    [int]$Workers = 4,
    [switch]$Retag,
    [switch]$NoPush,
    [switch]$NoPurge
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path (Split-Path $PSScriptRoot)
$state = Join-Path $env:LOCALAPPDATA "MindAttic\Prose\wiki\$Universe"
$books = Join-Path $PSScriptRoot "$Universe.books.json"
$cred = Join-Path $env:APPDATA "MindAttic\Wiki\$Universe.json"
foreach ($f in @($books, $cred)) { if (-not (Test-Path $f)) { throw "missing $f" } }
New-Item -ItemType Directory -Force -Path $state, (Join-Path $state 'beats'), (Join-Path $state 'out') | Out-Null

# One sync per universe at a time: two runs share this state folder and would overwrite each other's files.
try {
    $lock = [IO.File]::Open((Join-Path $state 'sync.lock'), 'OpenOrCreate', 'ReadWrite', 'None')
} catch [IO.IOException] {
    Write-Host "another wiki sync for '$Universe' is already running (lock: $(Join-Path $state 'sync.lock')); not starting a second one."
    exit 3
}
# The last run's log, for unattended (Scheduler) runs.
Start-Transcript -Path (Join-Path $state 'sync.log') -Force | Out-Null

function Write-Step([string]$Text) { Write-Host "[$((Get-Date).ToString('HH:mm:ss'))] $Text" }

# ---- the Hub ---------------------------------------------------------------------------------------------------
$hub = 'http://127.0.0.1:5900'
function Test-Hub { try { (Invoke-WebRequest "$hub/api/health" -UseBasicParsing -TimeoutSec 5).StatusCode -eq 200 } catch { $false } }
if (-not (Test-Hub)) {
    Write-Step 'starting the Prose Hub'
    Start-Process -FilePath 'C:\Apps\MindAttic\Prose\Hub.exe' -WorkingDirectory 'C:\Apps\MindAttic\Prose' -WindowStyle Minimized
    $deadline = (Get-Date).AddMinutes(2)
    while (-not (Test-Hub)) { if ((Get-Date) -gt $deadline) { throw 'the Prose Hub did not come up' }; Start-Sleep 3 }
}
$key = (Get-Content (Join-Path $env:LOCALAPPDATA 'MindAttic\Prose\Settings.json') -Raw | ConvertFrom-Json).HubApiKey

# Runs a Prose CLI handler on the Hub (what prose.cmd does, without rebuilding the CLI first).
function Invoke-Prose([string]$Handler, [string[]]$Arguments, [string]$Method, [switch]$Raw) {
    $all = if ($Raw) { $Arguments } else { @('--universe', $Universe) + $Arguments }
    $req = @{ handlerClass = $Handler; args = $all; universe = $Universe; cwd = $repo }
    if ($Method) { $req.method = $Method }
    $body = $req | ConvertTo-Json -Depth 4
    $headers = @{}; if ($key) { $headers['X-Prose-Key'] = $key }
    $r = Invoke-RestMethod -Method Post -Uri "$hub/api/cli-invoke" -Headers $headers -Body ([Text.Encoding]::UTF8.GetBytes($body)) `
        -ContentType 'application/json; charset=utf-8' -TimeoutSec 7200
    if ($r.exitCode -ne 0) { throw "$Handler failed ($($r.exitCode)): $($r.error)$($r.output)" }
    return [string]$r.output
}

# ---- export --------------------------------------------------------------------------------------------------------
Write-Step 'exporting every entity'
$out = Invoke-Prose 'ExportCli' @('--export', 'global')
$zip = [regex]::Match($out, 'wrote:\s*(.+\.zip)').Groups[1].Value.Trim()
if (-not $zip) { throw "export did not report its file: $out" }
$lastZip = Join-Path $state 'last-export.txt'
if (Test-Path $lastZip) { $old = (Get-Content $lastZip -Raw).Trim(); if ($old -and $old -ne $zip -and (Test-Path $old)) { Remove-Item $old } }
Set-Content $lastZip $zip

Write-Step 'exporting relationships (interchange)'
$interchange = Join-Path $state "$Universe.universe.json"
Invoke-Prose 'UniverseInterchangeCli' @($Universe, $interchange) -Method 'RunExportAsync' -Raw | Out-Null

$slugs = (Get-Content $books -Raw | ConvertFrom-Json).PSObject.Properties.Name
Write-Step "reading $($slugs.Count) canon books"
foreach ($slug in $slugs) {
    $json = Invoke-Prose 'ReadBeatsCli' @('--read-beats', '--slug', $slug, '--format', 'json')
    $start = $json.IndexOf('[')
    if ($start -lt 0) { throw "no beats came back for $slug" }
    [IO.File]::WriteAllText((Join-Path $state "beats\$slug.json"), $json.Substring($start), [Text.UTF8Encoding]::new($false))
}

$tagged = Join-Path $state "$Universe.tagged.json"
if ($Retag -or -not (Test-Path $tagged)) {
    Write-Step 'tagging entity text (dry run; slow on Azure)'
    Invoke-Prose 'TagEntityTextCli' @('--tag-entity-text', '--out', $tagged) | Out-Null
} else {
    Write-Step "reusing entity tags from $((Get-Item $tagged).LastWriteTime) (pass -Retag to refresh)"
}

# ---- build and push ------------------------------------------------------------------------------------------------
$py = @('-I', '-u', (Join-Path $PSScriptRoot 'prose2wiki.py'), '--universe', $DisplayName, '--export-zip', $zip,
        '--interchange', $interchange, '--beats-dir', (Join-Path $state 'beats'), '--book-titles', $books,
        '--tagged', $tagged, '--out', (Join-Path $state 'out'), '--phase', 'all', '--theme', $Theme,
        '--cred', $cred, '--workers', $Workers)
if (-not $NoPush) { $py += '--push' }
if (-not $NoPush -and -not $NoPurge) { $py += '--purge' }
Write-Step ($(if ($NoPush) { 'building pages (no push)' } else { 'building and pushing pages' }))
& python @py 2>&1 | ForEach-Object { "$_" }   # through the pipeline so the transcript keeps python's output
if ($LASTEXITCODE -ne 0) { throw "prose2wiki exited $LASTEXITCODE" }
Write-Step "done. Validation report: $(Join-Path $state 'out\repair.md')"
Stop-Transcript | Out-Null
