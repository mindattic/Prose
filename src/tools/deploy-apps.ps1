#Requires -Version 5.1
<#
.SYNOPSIS
    Publishes every Prose desktop app into one folder: C:\Apps\MindAttic\Prose\.

.DESCRIPTION
    Same four phases as Automata's tools\deploy.ps1 and Prose.Hub\tools\deploy.ps1, which this
    replaces for multi-app deploys: stop what's running, wait for the file locks to actually
    release, publish framework-dependent single-file, then write ONE self-redeploying launch.bat.

    Layout:

        C:\Apps\MindAttic\Prose\
            launch.bat            redeploys from source, then starts Launcher.exe
            Launcher.exe          picks an app; starts the Hub if it isn't up
            Writer.exe            the book editor (a WebView2 window onto the Hub)
            Hub.exe               the resident server - owns wwwroot\ in this folder
            wwwroot\              Hub's static assets, _content\ and _framework\
            KdpPublish\           its own subfolder, deliberately

    KdpPublish is NOT flat with the others. It ships its own wwwroot\ and its own launch.bat and
    writes run logs beside its exe; publishing it into the same directory as the Hub would merge
    two unrelated wwwroot trees and have the two launch.bat files overwrite each other. Writer and
    Launcher have no content of their own, so they sit flat quite safely.

.PARAMETER Apps
    Which to publish. Default: all of them.

.PARAMETER Launch
    Start Launcher.exe when the publish finishes. Equivalent to -Start Launcher, kept because
    Prose.Hub\tools\deploy.ps1 forwards it as a bare switch.

.PARAMETER Start
    Start THIS app when the publish finishes, instead of the Launcher. This is what the
    deploy-*.bat entry points use: one desktop icon per app, each of which republishes from source
    and then opens that app, so you can never be looking at a stale build.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File src\tools\deploy-apps.ps1
    powershell -ExecutionPolicy Bypass -File src\tools\deploy-apps.ps1 -Apps Writer,Hub -Start Writer
#>
param(
    # Deliberately NOT [ValidateSet]: `powershell -File script.ps1 -Apps Hub,Writer` hands this
    # parameter the single STRING "Hub,Writer" (and `-Apps Hub Writer` silently binds only "Hub"
    # and drops the rest). ValidateSet rejects the first and misses the second. Since every
    # launch.bat invokes this through -File, the argument is normalised and checked below instead,
    # so all three spellings work and a typo gets a real error rather than a silent no-op.
    [string[]]$Apps = @('Hub', 'Writer', 'Launcher', 'KdpPublish'),
    [switch]$Launch,
    [string]$Start = ''
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$known = @('Hub', 'Writer', 'Launcher', 'KdpPublish')
$Apps = @($Apps | ForEach-Object { $_ -split '[,;\s]+' } | Where-Object { $_ })
$unknown = @($Apps | Where-Object { $known -notcontains $_ })
if ($unknown) {
    Write-Host "  Unknown app(s): $($unknown -join ', '). Valid: $($known -join ', ')." -ForegroundColor Red
    exit 1
}
if (-not $Apps) { $Apps = $known }

# -Launch is the older spelling of "-Start Launcher" and is still forwarded as a bare switch by
# Prose.Hub\tools\deploy.ps1, so it has to keep working.
if ($Launch -and -not $Start) { $Start = 'Launcher' }
if ($Start) {
    $match = @($known | Where-Object { $_ -eq $Start })
    if (-not $match) {
        Write-Host "  Unknown -Start app '$Start'. Valid: $($known -join ', ')." -ForegroundColor Red
        exit 1
    }
    $Start = $match[0]                       # normalise the caller's casing
    # Starting an app that was not published would open a stale build - the exact failure these
    # entry points exist to prevent.
    if ($Apps -notcontains $Start) { $Apps += $Start }
}

$srcRoot   = Split-Path $PSScriptRoot          # tools\ -> src\
$root = 'C:\Apps\MindAttic\Prose'

# Publishing every project through one artifacts path keeps the build graph out of the repo's
# bin/obj entirely, which is what lets this run while Prose.Mcp.exe holds its own DLLs open for
# the length of a Claude Code session. Killing that process would end the user's session.
$artifacts = Join-Path $env:LOCALAPPDATA 'Prose\build-artifacts'

$catalog = @{
    Hub        = @{ Proj = 'Prose.Hub\Prose.Hub.csproj';               Exe = 'Hub.exe';           Out = $root }
    Writer     = @{ Proj = 'Prose.Writer\Prose.Writer.csproj';         Exe = 'Writer.exe';        Out = $root }
    Launcher   = @{ Proj = 'Prose.Launcher\Prose.Launcher.csproj';     Exe = 'Launcher.exe';      Out = $root }
    KdpPublish = @{ Proj = 'Prose.KdpPublish\Prose.KdpPublish.csproj'; Exe = 'Prose.KdpPublish.exe'; Out = (Join-Path $root 'KdpPublish') }
}

function Stop-App {
    param([string]$ExePath)

    if (-not (Test-Path $ExePath)) { return }

    # Matched by PATH, not just by name: "Hub" and "Launcher" are generic enough that another
    # application could plausibly own a process by that name, and this stops processes forcibly.
    $leaf  = [System.IO.Path]::GetFileNameWithoutExtension($ExePath)
    $procs = Get-Process $leaf -ErrorAction SilentlyContinue |
             Where-Object { $_.Path -and ($_.Path -eq $ExePath) }
    if ($procs) {
        $procs | Stop-Process -Force
        Write-Host "    Stopped $leaf ($(@($procs).Count))" -ForegroundColor DarkYellow
    }

    # A fixed sleep is not reliable for a 70 MB single-file bundle: it can hold its own exe handle
    # open past the point the process reports as exited, and the publish then fails with "Access
    # to the path is denied". Poll for an exclusive write handle instead.
    for ($i = 0; $i -lt 20; $i++) {
        try { $fs = [System.IO.File]::Open($ExePath, 'Open', 'Write', 'None'); $fs.Close(); return }
        catch { Start-Sleep -Milliseconds 500 }
    }
    Write-Host "    $leaf still locked after 10s - publish may fail." -ForegroundColor DarkYellow
}

Write-Host ''
Write-Host "  Deploying to $root" -ForegroundColor Cyan

foreach ($name in $Apps) {
    $app  = $catalog[$name]
    $proj = Join-Path $srcRoot $app.Proj
    $out  = $app.Out
    $exe  = Join-Path $out $app.Exe

    if (-not (Test-Path $proj)) {
        Write-Host "  ! $name - project not found at $proj" -ForegroundColor Red
        exit 1
    }

    Write-Host ''
    Write-Host "  $name" -ForegroundColor Cyan
    Stop-App $exe

    dotnet publish $proj `
        --configuration Release `
        --runtime win-x64 `
        --self-contained false `
        --artifacts-path $artifacts `
        /p:PublishSingleFile=true `
        /p:IncludeNativeLibrariesForSelfExtract=true `
        --output $out | Out-Host

    if ($LASTEXITCODE -ne 0) {
        Write-Host "  $name publish failed (exit $LASTEXITCODE)." -ForegroundColor Red
        exit $LASTEXITCODE
    }
    if (-not (Test-Path $exe)) {
        Write-Host "  $name published but $($app.Exe) is not at $exe." -ForegroundColor Red
        exit 1
    }

    Write-Host "    -> $exe ($([math]::Round(((Get-Item $exe).Length / 1MB), 1)) MB)" -ForegroundColor Gray
}

# ── Self-redeploying launchers ─────────────────────────────────────────────
# Rebuild from source FIRST, then start, so you can never be looking at a stale exe. (KdpPublish's
# own header records why - a three-day-stale build silently missing an entire feature.)
#
# These are the in-folder copies, for launching from C:\Apps directly. The ones a desktop shortcut
# should point at live in the REPO root (deploy-*.bat) and are checked in: this folder can be
# deleted and rebuilt from source at any time, so an entry point that only exists here is an entry
# point that disappears the first time someone clears it out. Both spellings are generated from
# this one function so they cannot drift.
function Write-Launcher {
    param([string]$File, [string]$Title, [string]$AppList, [string]$StartApp, [string]$Dir = $root)

    $path = Join-Path $Dir $File
    @(
        '@echo off',
        "title $Title",
        "set `"DEPLOY_PS1=$(Join-Path $PSScriptRoot 'deploy-apps.ps1')`"",
        'if exist "%DEPLOY_PS1%" (',
        '    echo Redeploying latest build from source...',
        "    powershell -NoProfile -ExecutionPolicy Bypass -File `"%DEPLOY_PS1%`" -Apps $AppList -Start $StartApp",
        '    if not errorlevel 1 exit /b 0',
        '    echo Redeploy failed - launching whatever is already in this folder instead.',
        ') else (',
        '    echo Source repo not found at "%DEPLOY_PS1%" - launching existing build without redeploying.',
        ')',
        'rem Fallback only: reached when the repo is gone or the publish failed, so that a missing',
        'rem source tree still leaves a working icon. The happy path exits above, already launched.',
        'rem The Hub binds 127.0.0.1 only and is never the Azure deployment. Without an explicit',
        'rem environment it defaults to Production, which makes AddMindAtticAuthentication fail closed.',
        'set "ASPNETCORE_ENVIRONMENT=Development"',
        "start `"`" `"%~dp0$($catalog[$StartApp].Exe)`""
    ) | Set-Content $path -Encoding ascii

    Write-Host "    $path" -ForegroundColor Gray
}

Write-Host ''
Write-Host '  Launchers' -ForegroundColor Cyan

# Writer rebuilds the HUB as well as itself, and that is not belt-and-braces: the entire editor -
# every Razor component, the save path, the entity modal - is Prose.WriterUi compiled INTO Hub.exe.
# Writer.exe is just a WebView2 window pointed at it. Republishing only Writer.exe would leave the
# author editing in yesterday's UI while believing they had just redeployed it.
Write-Launcher -File 'Writer.bat'  -Title 'Prose Writer' -AppList 'Hub,Writer' -StartApp 'Writer'
Write-Launcher -File 'Hub.bat'     -Title 'Prose Hub'    -AppList 'Hub'        -StartApp 'Hub'
Write-Launcher -File 'launch.bat'  -Title 'Prose'        -AppList 'Hub,Writer,Launcher,KdpPublish' -StartApp 'Launcher'

# In its own subfolder, so plain launch.bat collides with nothing.
Write-Launcher -File 'launch.bat' -Title 'KdpPublish' -AppList 'KdpPublish' `
               -StartApp 'KdpPublish' -Dir $catalog.KdpPublish.Out

Write-Host ''
Write-Host '  Published successfully.' -ForegroundColor Green
Write-Host ''

# -- Hub health -------------------------------------------------------------
# The Hub is the one process every CLI command, MCP tool and Writer window forwards into, and
# /api/health is fail-closed (503 when SQL Server is unreachable), so a 200 means "usable", not
# merely "listening". Mirrors Prose.Writer\HubProcess.cs, which applies the same rule for the app.
$HubBaseUrl   = 'http://127.0.0.1:5900'
$HubHealthUrl = "$HubBaseUrl/api/health"

function Test-HubHealthy {
    try {
        $resp = Invoke-WebRequest -Uri $HubHealthUrl -UseBasicParsing -TimeoutSec 3
        return ($resp.StatusCode -ge 200 -and $resp.StatusCode -lt 300)
    } catch { return $false }
}

# Poll until the Hub answers. Startup includes an EF migration check against SQL Server, so this
# is seconds - and after a schema change (pending migrations apply on start) it can be longer.
function Wait-HubHealthy {
    param([int]$TimeoutSeconds = 90)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $tick = 0
    while ((Get-Date) -lt $deadline) {
        if (Test-HubHealthy) { return $true }
        Start-Sleep -Milliseconds 500
        $tick++
        if ($tick % 6 -eq 0) { Write-Host "    waiting for $HubHealthUrl ... ($([int]($tick / 2))s)" -ForegroundColor DarkYellow }
    }
    return $false
}

if ($Start) {
    $startApp = $catalog[$Start]
    $startExe = Join-Path $startApp.Out $startApp.Exe

    # The Hub binds 127.0.0.1 only and is never the Azure deployment. Without an explicit
    # environment it defaults to Production, which makes AddMindAtticAuthentication fail closed.
    # Set for every app here, not just the Hub: Writer and Launcher both start the Hub themselves
    # if it is not already up, and a child process inherits this.
    $env:ASPNETCORE_ENVIRONMENT = 'Development'

    if ($Start -eq 'Hub') {
        # Stop-App above matched by exact path; a Hub running from another install location, or
        # one that outlived the file-lock wait, may still own port 5900. Never start a second one
        # to fight it - connect to what is there. And do not report success until the new Hub
        # actually answers: a launched exe that never becomes healthy is a failed deploy, not a
        # started one, and the desktop icon's "DEPLOY FAILED" branch is what should fire.
        if (Test-HubHealthy) {
            Write-Host "  Prose Hub is already running and healthy at $HubBaseUrl - connecting to it, not starting a second." -ForegroundColor Green
            if ($Apps -contains 'Hub') {
                # The Hub at this install path was stopped before its files were replaced, so the
                # one answering may be another install's (or one that outlived the lock wait) and
                # NOT the build just published. Say so instead of reporting a plain success.
                Write-Host "  WARNING: the Hub was just redeployed, but a Hub was already answering before this one started." -ForegroundColor Yellow
                Write-Host "  It may be the OLD build. Compare the 'build' field of $HubHealthUrl with the previous value." -ForegroundColor Yellow
            }
        }
        else {
            Write-Host "  Starting Hub..." -ForegroundColor Cyan
            Start-Process $startExe -WorkingDirectory $startApp.Out
            if (-not (Wait-HubHealthy -TimeoutSeconds 90)) {
                Write-Host ''
                Write-Host "  Prose Hub was started but never answered $HubHealthUrl within 90s." -ForegroundColor Red
                Write-Host "  /api/health is fail-closed - it returns 503 while SQL Server is unreachable or a" -ForegroundColor Red
                Write-Host "  migration is still applying. Check the Hub's own console window, then re-run this" -ForegroundColor Red
                Write-Host "  icon: it will reconnect to a Hub that has since come up instead of starting another." -ForegroundColor Red
                exit 1
            }
        }
        Write-Host "  Prose Hub is up and reachable at $HubBaseUrl." -ForegroundColor Green
    }
    else {
        Write-Host "  Starting $Start..." -ForegroundColor Cyan
        Start-Process $startExe -WorkingDirectory $startApp.Out
    }
}
