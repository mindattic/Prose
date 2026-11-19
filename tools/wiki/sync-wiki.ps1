<#
.SYNOPSIS
    Syncs a Prose universe into its MediaWiki: every entity, relationship and canon-book appearance, one command.

.DESCRIPTION
    1. Makes sure the Prose Hub is up (starts C:\Apps\MindAttic\Prose\Hub.exe if not).
    2. Through the Hub: --export global (every live entity), --universe-export (edges), and --read-beats for each
       canon book in <UNIVERSE>.Wiki\<universe>.books.json, the site repo (which entities each book names).
    2b. Each canon book whose text changed: --export-node --preview into the state folder (.md .pdf .epub .docx .txt
       and the blurb), plus its cover from the publish folder. prose2wiki makes a page per book and per chapter and
       uploads the cover and the files.
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
    [switch]$NoPurge,
    # The wiki's site repo (books file, intro, logo, theme): <UNIVERSE>.Wiki beside Prose, e.g. GLMZ.Wiki (PRS-T1052).
    [string]$Site
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path (Split-Path $PSScriptRoot)
if (-not $Site) { $Site = Join-Path (Split-Path $repo) "$($Universe.ToUpperInvariant()).Wiki" }
if (-not (Test-Path (Join-Path $Site "$Universe.books.json"))) { $Site = $PSScriptRoot }  # a universe without a site repo yet
$state = Join-Path $env:LOCALAPPDATA "MindAttic\Prose\wiki\$Universe"
$books = Join-Path $Site "$Universe.books.json"
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
    # A cold start takes 1-2 minutes, longer while Azure resumes a paused serverless database.
    $deadline = (Get-Date).AddMinutes(5)
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

$slugs = (Get-Content $books -Raw | ConvertFrom-Json).PSObject.Properties.Name | Where-Object { -not $_.StartsWith('_') }  # _series is not a book
Write-Step "reading $($slugs.Count) canon books"
foreach ($slug in $slugs) {
    $json = Invoke-Prose 'ReadBeatsCli' @('--read-beats', '--slug', $slug, '--format', 'json')
    $start = $json.IndexOf('[')
    if ($start -lt 0) { throw "no beats came back for $slug" }
    [IO.File]::WriteAllText((Join-Path $state "beats\$slug.json"), $json.Substring($start), [Text.UTF8Encoding]::new($false))
}

# Where each book is on sale: Node.PublishUrl / Asin (the Hub's --list-books --json). A Hub without those columns in
# its JSON just leaves the wiki without store links.
$storeFile = Join-Path $state 'books.store.json'
try {
    $listed = Invoke-Prose 'ListNodesCli' @('--list-books', '--kind', 'book', '--json')
    $rows = $listed.Substring($listed.IndexOf('[')) | ConvertFrom-Json
    $store = @{}
    # Keyed by slug and by code: the books file names some books by their code (pxl, mxg).
    foreach ($r in $rows) {
        if ($r.PublishUrl -or $r.Asin) {
            $entry = @{ url = $r.PublishUrl; asin = $r.Asin }
            $store[$r.Slug] = $entry
            if ($r.Code) { $store[$r.Code.ToLowerInvariant()] = $entry }
        }
    }
    [IO.File]::WriteAllText($storeFile, ($store | ConvertTo-Json -Depth 3), [Text.UTF8Encoding]::new($false))
    Write-Step "store links for $(@($rows | Where-Object { $_.PublishUrl -or $_.Asin }).Count) published book(s)"
} catch { Write-Host "  store links not read: $($_.Exception.Message)" }

# The story checks (PRS-T1067): each book's registered plants and payoffs, and the continuity store's open
# contradictions. Read-only; a failure leaves those pages as they were.
$storyDir = Join-Path $state 'story'
New-Item -ItemType Directory -Force -Path $storyDir | Out-Null
try {
    $plants = @{}
    foreach ($slug in $slugs) {
        $o = Invoke-Prose 'PlantPayoffCli' @('--list-plants', '--slug', $slug, '--json')
        $i = $o.IndexOf('[')
        $plants[$slug] = if ($i -ge 0) { @($o.Substring($i) | ConvertFrom-Json) } else { @() }
    }
    [IO.File]::WriteAllText((Join-Path $storyDir 'plants.json'), ($plants | ConvertTo-Json -Depth 6), [Text.UTF8Encoding]::new($false))
    Write-Step "plants and payoffs: $(($plants.Values | ForEach-Object { $_.Count } | Measure-Object -Sum).Sum)"
} catch { Write-Host "  plants not read: $($_.Exception.Message)" }
try {
    # Exit 1 means "there are open contradictions": the list is still the answer.
    $req = @{ handlerClass = 'ContinuityCli'; args = @('--universe', $Universe, '--continuity', 'contradictions'); universe = $Universe; cwd = $repo } | ConvertTo-Json -Depth 4
    $headers = @{}; if ($key) { $headers['X-Prose-Key'] = $key }
    $r = Invoke-RestMethod -Method Post -Uri "$hub/api/cli-invoke" -Headers $headers -Body ([Text.Encoding]::UTF8.GetBytes($req)) -ContentType 'application/json; charset=utf-8' -TimeoutSec 1800
    [IO.File]::WriteAllText((Join-Path $storyDir 'contradictions.txt'), [string]$r.output, [Text.UTF8Encoding]::new($false))
    Write-Step "continuity contradictions read"
} catch { Write-Host "  contradictions not read: $($_.Exception.Message)" }

# The books themselves: every format, from the database as it is now, into the state folder (a --preview export,
# which leaves the publish folder alone). Only books whose text changed since their last export are exported again.
$booksDir = Join-Path $state 'books'
$publishDir = $null
try {
    $settings = Get-Content (Join-Path $env:LOCALAPPDATA 'MindAttic\Prose\Settings.json') -Raw | ConvertFrom-Json
    $publishDir = $settings.UniverseExportDirectories.$Universe
} catch { }
$config = Get-Content $books -Raw | ConvertFrom-Json
foreach ($slug in $slugs) {
    $folder = Join-Path $booksDir $slug
    $source = (Get-FileHash (Join-Path $state "beats\$slug.json") -Algorithm SHA1).Hash
    $marker = Join-Path $folder 'source.sha1'
    $current = (Test-Path $marker) -and ((Get-Content $marker -Raw).Trim() -eq $source) -and
               (Get-ChildItem $folder -Recurse -Filter '* V*.md' -ErrorAction SilentlyContinue)
    if (-not $current) {
        Write-Step "exporting the book $slug"
        if (Test-Path $folder) { Remove-Item $folder -Recurse -Force }
        New-Item -ItemType Directory -Force -Path $folder | Out-Null
        $out = Invoke-Prose 'ExportNodeCli' @('--export-node', '--slug', $slug, '--preview', $folder)
        if ($out -notmatch 'all formats') { Write-Host "  $slug exported only partly: $($out.Trim())" }
        Set-Content $marker $source
    }
    # Its cover: the publish folder's cover.jpg (the one KDP gets), when the book has one.
    $code = $config.$slug.code
    # Downloads the export could not make (the read gate holds back every format but .md while beats are unread):
    # the published edition from the publish folder, the one that was read and went to KDP. The chapters on the wiki
    # still follow the latest text.
    if ($publishDir -and $code -and (Test-Path (Join-Path $publishDir $code))) {
        $exported = Get-ChildItem $folder -Directory | Select-Object -First 1
        if ($exported) {
            foreach ($ext in 'pdf', 'epub', 'docx', 'txt') {
                if (-not (Get-ChildItem $exported.FullName -Filter "* V*.$ext" -ErrorAction SilentlyContinue)) {
                    $edition = Get-ChildItem (Join-Path $publishDir $code) -Filter "* V*.$ext" -ErrorAction SilentlyContinue |
                        Sort-Object LastWriteTime | Select-Object -Last 1
                    if ($edition) { Copy-Item $edition.FullName $exported.FullName -Force }
                }
            }
        }
    }
    if ($publishDir -and $code) {
        foreach ($name in 'cover.jpg', 'cover.png') {
            $cover = Join-Path (Join-Path $publishDir $code) $name
            if (Test-Path $cover) { Copy-Item $cover (Join-Path $folder $name) -Force; break }
        }
    }
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
        '--cred', $cred, '--workers', $Workers, '--books-dir', $booksDir, '--site-dir', $Site)
$logo = Join-Path $Site "$Universe.logo.png"
if (Test-Path $logo) { $py += @('--logo', $logo) }
if (Test-Path $storeFile) { $py += @('--store', $storeFile) }
if (Test-Path $storyDir) { $py += @('--story-dir', $storyDir) }
if (-not $NoPush) { $py += '--push' }
if (-not $NoPush -and -not $NoPurge) { $py += '--purge' }
Write-Step ($(if ($NoPush) { 'building pages (no push)' } else { 'building and pushing pages' }))
# Through the pipeline so the transcript keeps python's output. 'Continue' here: under 'Stop', python's first stderr
# line (a traceback, a warning) became a terminating error that hid the rest of it.
$ErrorActionPreference = 'Continue'
& python @py 2>&1 | ForEach-Object { "$_" }
$ErrorActionPreference = 'Stop'
if ($LASTEXITCODE -ne 0) { throw "prose2wiki exited $LASTEXITCODE" }
Write-Step "done. Validation report: $(Join-Path $state 'out\repair.md')"
Stop-Transcript | Out-Null
