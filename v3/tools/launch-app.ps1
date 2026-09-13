#Requires -Version 5.1
<#
.SYNOPSIS
    Starts one already-published Prose app from C:\Apps\MindAttic\Prose\.

.DESCRIPTION
    The counterpart to deploy-apps.ps1, and deliberately the smaller half: this only STARTS things.
    It never publishes.

    That separation is the point. Publishing has to stop a running Hub to overwrite a 70 MB
    single-file exe, which drops every CLI/MCP client and loses the Hub's in-memory state — far too
    much to do to someone who only asked to open a window. `/redeploy` is when you want new code;
    `/launch` is when you want the app.

    The trade is that you could open a stale build, so this never lets that happen silently: it
    compares the deployed exe against the newest source file and says so. Warn, don't rebuild —
    the choice stays the author's.

.PARAMETER App
    writer | hub | kdp | launcher | wiki (case-insensitive; "kdp" is KdpPublish, "repo" is "wiki").

    "wiki" is not an executable: the entity browser is a page served by the running Hub at
    /repo, so that target makes sure the Hub is up and then opens a browser at it.

.PARAMETER Force
    Start a second instance even if one is already running. Refused for the Hub, which is a
    singleton by construction.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File v3\tools\launch-app.ps1 -App writer
#>
param(
    [Parameter(Mandatory = $true)][string]$App,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = 'C:\Apps\MindAttic\Prose'
$v3   = Split-Path $PSScriptRoot

# Aliases exist because the author types what the app is called, not what the file is called.
$catalog = @{
    writer   = @{ Exe = "$root\Writer.exe";                      Name = 'Writer';     Proc = 'Writer' }
    hub      = @{ Exe = "$root\Hub.exe";                         Name = 'Hub';        Proc = 'Hub' }
    kdp      = @{ Exe = "$root\KdpPublish\Prose.KdpPublish.exe"; Name = 'KdpPublish'; Proc = 'Prose.KdpPublish' }
    launcher = @{ Exe = "$root\Launcher.exe";                    Name = 'Launcher';   Proc = 'Launcher' }
}

$key = $App.Trim().ToLowerInvariant()
if ($key -eq 'kdppublish') { $key = 'kdp' }
if ($key -in @('repo', 'repos', 'entities', 'encyclopedia')) { $key = 'wiki' }

# ── The wiki is a page, not a process ──────────────────────────────────────
# It is Prose.WriterUi compiled into Hub.exe and rendered live from SQL, so "launching" it means
# making sure the Hub is up and pointing a browser at it. Handled before the exe catalog because
# there is no Wiki.exe to look for and never will be.
if ($key -eq 'wiki') {
    $wikiUrl = 'http://127.0.0.1:5900/repo'

    $healthy = $false
    try { $healthy = (Invoke-WebRequest -Uri 'http://127.0.0.1:5900/api/health' -UseBasicParsing -TimeoutSec 5).StatusCode -eq 200 }
    catch { $healthy = $false }

    if (-not $healthy) {
        if (-not (Test-Path "$root\Hub.exe")) {
            Write-Host "  The wiki is served by the Hub, and Hub.exe is not deployed. Run /redeploy Hub." -ForegroundColor Red
            exit 1
        }
        Write-Host '  Hub is not up - starting it first (the wiki is a page it serves)...' -ForegroundColor Cyan
        $env:ASPNETCORE_ENVIRONMENT = 'Development'
        Start-Process "$root\Hub.exe" -WorkingDirectory $root

        for ($i = 0; $i -lt 40; $i++) {
            Start-Sleep -Milliseconds 500
            try {
                if ((Invoke-WebRequest -Uri 'http://127.0.0.1:5900/api/health' -UseBasicParsing -TimeoutSec 5).StatusCode -eq 200) {
                    $healthy = $true; break
                }
            } catch { }
        }
        if (-not $healthy) {
            Write-Host '  Hub never became healthy. /api/health is fail-closed, so check SQL Server is reachable.' -ForegroundColor Red
            exit 1
        }
    }

    $chrome = @(
        "$env:ProgramFiles\Google\Chrome\Application\chrome.exe",
        "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe",
        "$env:LocalAppData\Google\Chrome\Application\chrome.exe"
    ) | Where-Object { Test-Path $_ } | Select-Object -First 1

    if ($chrome) { Start-Process $chrome $wikiUrl } else { Start-Process $wikiUrl }

    Write-Host "  Opened $wikiUrl" -ForegroundColor Green
    Write-Host '  Every repository in the current universe, live from SQL. Click through to an entity' -ForegroundColor DarkGray
    Write-Host '  for its record, relationships, where it appears in prose, and its version history.' -ForegroundColor DarkGray
    Write-Host ''
    exit 0
}

if (-not $catalog.ContainsKey($key)) {
    Write-Host "  Unknown app '$App'. Valid: writer, hub, kdp, launcher, wiki." -ForegroundColor Red
    exit 1
}

$target = $catalog[$key]

if (-not (Test-Path $target.Exe)) {
    Write-Host "  $($target.Name) is not deployed - $($target.Exe) does not exist." -ForegroundColor Red
    Write-Host "  Publish it first:  /redeploy $($target.Name)" -ForegroundColor Yellow
    exit 1
}

# ── Already running? ───────────────────────────────────────────────────────
# Matched by PATH, not just name: "Hub" and "Launcher" are generic enough to collide with
# something unrelated, and this decides whether to start a second copy.
$running = @(Get-Process $target.Proc -ErrorAction SilentlyContinue |
             Where-Object { $_.Path -and $_.Path -eq $target.Exe })

if ($running -and -not $Force) {
    $p = $running[0]
    Write-Host "  $($target.Name) is already running (PID $($p.Id), started $($p.StartTime))." -ForegroundColor Green
    if ($key -eq 'hub') {
        Write-Host "  The Hub is a singleton - only one process can bind 127.0.0.1:5900." -ForegroundColor DarkGray
        Write-Host "  To pick up new code use /redeploy Hub, which stops and replaces it." -ForegroundColor DarkGray
    }
    else {
        Write-Host "  Use -Force to start another instance anyway." -ForegroundColor DarkGray
    }
    exit 0
}

if ($running -and $key -eq 'hub') {
    Write-Host "  Refusing to start a second Hub: only one process can bind 127.0.0.1:5900," -ForegroundColor Red
    Write-Host "  and the second would fail on startup. Use /redeploy Hub to replace the running one." -ForegroundColor Red
    exit 1
}

# ── Staleness: never open an old build without saying so ───────────────────
$deployed = (Get-Item $target.Exe).LastWriteTime
$newestSource = Get-ChildItem $v3 -Recurse -File -Include *.cs, *.razor, *.csproj, *.css, *.js -ErrorAction SilentlyContinue |
                Where-Object { $_.FullName -notmatch '\\(bin|obj|artifacts)\\' } |
                Sort-Object LastWriteTime -Descending |
                Select-Object -First 1

Write-Host ''
if ($newestSource -and $newestSource.LastWriteTime -gt $deployed) {
    Write-Host "  ! $($target.Name) was published $deployed, but source has changed since:" -ForegroundColor Yellow
    Write-Host "      $($newestSource.FullName.Replace($v3, 'v3')) ($($newestSource.LastWriteTime))" -ForegroundColor DarkYellow
    Write-Host "    Starting the deployed build anyway. Run /redeploy $($target.Name) to pick the change up." -ForegroundColor DarkYellow
    Write-Host ''
}
else {
    Write-Host "  $($target.Name) is current (published $deployed)." -ForegroundColor DarkGray
}

# ── Start ──────────────────────────────────────────────────────────────────
# Set for every app, not just the Hub: Writer and Launcher each start the Hub themselves when it
# is not up, and a child process inherits this. Without it ASP.NET Core defaults to Production,
# which makes AddMindAtticAuthentication fail closed.
$env:ASPNETCORE_ENVIRONMENT = 'Development'

Write-Host "  Starting $($target.Name)..." -ForegroundColor Cyan
Start-Process $target.Exe -WorkingDirectory (Split-Path $target.Exe)

# The Hub is the one whose readiness other things depend on, so it is worth waiting for.
# /api/health is fail-closed (503 when SQL Server is unreachable), so a 200 means genuinely usable.
if ($key -eq 'hub' -or $key -eq 'writer' -or $key -eq 'launcher') {
    for ($i = 0; $i -lt 40; $i++) {
        Start-Sleep -Milliseconds 500
        try {
            $code = (Invoke-WebRequest -Uri 'http://127.0.0.1:5900/api/health' -UseBasicParsing -TimeoutSec 5).StatusCode
            if ($code -eq 200) { Write-Host "  Hub healthy (200)." -ForegroundColor Green; break }
        }
        catch { }
    }
}

Write-Host ''
