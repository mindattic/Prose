#Requires -Version 5.1
<#
.SYNOPSIS
    Publishes the Prose desktop apps into one folder, C:\Apps\MindAttic\Prose\, and optionally
    starts one. The one deploy script for this repo.

.DESCRIPTION
    Layout:

        C:\Apps\MindAttic\Prose\
            deploy.bat            the desktop shortcuts point here: `deploy.bat Hub`,
                                  `deploy.bat Writer`, `deploy.bat Launcher`, or no argument to
                                  deploy everything and open the Launcher. Redeploys from source
                                  (via this script), then starts the app.
            Launcher.exe          picks an app; starts the Hub if it isn't up
            Writer.exe            the book editor (a WebView2 window onto the Hub)
            Hub.exe               the resident server - owns wwwroot\ in this folder
            wwwroot\              Hub's static assets, _content\ and _framework\

    Rebuild only when changed: without -Force an app is republished only when its deployed exe
    is missing, deploy.bat is missing, or a source file of that app (its project directory plus
    every project it references, transitively, plus linked files and the repo's Directory.*
    files) is newer than the deployed exe. Otherwise the stop + publish is skipped, so the call
    deploy.bat makes on every launch is a fast no-op.

    For an app that does need publishing: stop it (matched by path), wait for the file locks to
    actually release, publish framework-dependent single-file, then (re)write deploy.bat.

    KdpPublish is NOT built here. It was decoupled into its own repo 2026-10-04
    (github.com/mindattic/KdpPublish) with its own deploy pipeline, publishing standalone to
    C:\Apps\KdpPublish\ (C:\Apps\KdpPublish\deploy.bat). The Launcher still opens it from there
    (Prose.Launcher\MainWindow.xaml.cs's OpenKdp).

.PARAMETER Apps
    Which to publish. Default: all of them (Hub, Writer, Launcher).

.PARAMETER Force
    Republish unconditionally, with a clean rebuild: the apps' build output (bin/obj under the
    artifacts path below, for the app's project and everything it references) is deleted first.

.PARAMETER Launch
    Start Launcher.exe when the deploy finishes. Equivalent to -Start Launcher, kept because
    src\Prose.Hub\tools\deploy.ps1 forwards it as a bare switch.

.PARAMETER Start
    Start THIS app when the deploy finishes. This is what deploy.bat uses: one desktop icon per
    app, each of which republishes from source (when changed) and then opens that app, so you can
    never be looking at a stale build.

    Only the apps in -Apps are stopped and replaced. Starting Writer or the Launcher never touches
    the Hub: they connect to the running one, or start the deployed Hub.exe when none is up.

.NOTES
    Exit codes: 0 ok, 1 deploy failed (deploy.bat falls back to launching the existing build),
    2 deployed but the started Hub never became healthy (deploy.bat does NOT start another).

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File deploy.ps1
    powershell -ExecutionPolicy Bypass -File deploy.ps1 -Force
    powershell -ExecutionPolicy Bypass -File deploy.ps1 -Apps Hub -Start Hub
    powershell -ExecutionPolicy Bypass -File deploy.ps1 -Apps Writer -Start Writer
#>
param(
    # Deliberately NOT [ValidateSet]: `powershell -File script.ps1 -Apps Hub,Writer` hands this
    # parameter the single STRING "Hub,Writer" (and `-Apps Hub Writer` silently binds only "Hub"
    # and drops the rest). ValidateSet rejects the first and misses the second. Since deploy.bat
    # invokes this through -File, the argument is normalised and checked below instead, so all
    # three spellings work and a typo gets a real error rather than a silent no-op.
    [string[]]$Apps = @('Hub', 'Writer', 'Launcher'),
    [switch]$Force,
    [switch]$Launch,
    [string]$Start = ''
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$known = @('Hub', 'Writer', 'Launcher')
$Apps = @($Apps | ForEach-Object { $_ -split '[,;\s]+' } | Where-Object { $_ })
$unknown = @($Apps | Where-Object { $known -notcontains $_ })
if ($unknown) {
    Write-Host "  Unknown app(s): $($unknown -join ', '). Valid: $($known -join ', ')." -ForegroundColor Red
    exit 1
}
if (-not $Apps) { $Apps = $known }
# Normalise casing and order (Hub first, so a Writer/Launcher started afterwards finds it current).
$Apps = @($known | Where-Object { $Apps -contains $_ })

# -Launch is the older spelling of "-Start Launcher" and is still forwarded as a bare switch by
# src\Prose.Hub\tools\deploy.ps1, so it has to keep working.
if ($Launch -and -not $Start) { $Start = 'Launcher' }
if ($Start) {
    $match = @($known | Where-Object { $_ -eq $Start })
    if (-not $match) {
        Write-Host "  Unknown -Start app '$Start'. Valid: $($known -join ', ')." -ForegroundColor Red
        exit 1
    }
    $Start = $match[0]                       # normalise the caller's casing
    # Starting an app that was not deployed would risk opening a stale build - the exact failure
    # these entry points exist to prevent. (Deployed = checked, and republished only if changed.)
    if ($Apps -notcontains $Start) { $Apps = @($known | Where-Object { $Apps -contains $_ -or $_ -eq $Start }) }
}

$repo    = $PSScriptRoot
$srcRoot = Join-Path $repo 'src'
$root    = 'C:\Apps\MindAttic\Prose'
$deployBatPath = Join-Path $root 'deploy.bat'

# Publishing every project through one artifacts path keeps the build graph out of the repo's
# bin/obj entirely, which is what lets this run while Prose.Mcp.exe holds its own DLLs open for
# the length of a Claude Code session. Killing that process would end the user's session.
$artifacts = Join-Path $env:LOCALAPPDATA 'Prose\build-artifacts'

$catalog = @{
    Hub        = @{ Proj = 'Prose.Hub\Prose.Hub.csproj';               Exe = 'Hub.exe';           Out = $root }
    Writer     = @{ Proj = 'Prose.Writer\Prose.Writer.csproj';         Exe = 'Writer.exe';        Out = $root }
    Launcher   = @{ Proj = 'Prose.Launcher\Prose.Launcher.csproj';     Exe = 'Launcher.exe';      Out = $root }
}

# ── Change detection ──────────────────────────────────────────────────────────
$sourceExtensions = @('.cs', '.csproj', '.xaml', '.razor', '.cshtml', '.css', '.js', '.json',
                      '.props', '.targets', '.resx')
# bin/obj plus the obj_*/bin_* isolated-build folders Directory.Build.props excludes.
$skipDirPattern = '^(bin|obj|bin_.*|obj_.*|node_modules|\.vs)$'

# The project, every project it references (transitively), and files linked in from outside
# its directory (Prose.Launcher compiles Prose.Writer\HubProcess.cs).
function Get-AppSources {
    param([string]$ProjPath)

    $projects = New-Object System.Collections.Generic.List[string]
    $files    = New-Object System.Collections.Generic.List[string]
    $queue    = New-Object System.Collections.Generic.Queue[string]
    $queue.Enqueue([System.IO.Path]::GetFullPath($ProjPath))

    while ($queue.Count -gt 0) {
        $p = $queue.Dequeue()
        if ($projects -contains $p -or -not (Test-Path $p)) { continue }
        $projects.Add($p)
        $dir  = Split-Path $p
        $text = [System.IO.File]::ReadAllText($p)
        foreach ($m in [regex]::Matches($text, 'Include="([^"]+)"')) {
            $inc = $m.Groups[1].Value
            if ($inc -notmatch '^\.\.[\\/]' -or $inc -match '[*?$]') { continue }
            $full = [System.IO.Path]::GetFullPath((Join-Path $dir $inc))
            if ($full -like '*.csproj') { $queue.Enqueue($full) }
            else { $files.Add($full) }
        }
    }

    foreach ($d in @($repo, $srcRoot)) {
        foreach ($n in 'Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props') {
            $f = Join-Path $d $n
            if (Test-Path $f) { $files.Add($f) }
        }
    }

    [pscustomobject]@{ Projects = @($projects); Files = @($files) }
}

# First source file under $Dirs (or in $Files) newer than $Since, or $null. Walks the tree by
# hand so bin/obj/node_modules are pruned instead of enumerated and then filtered.
function Find-NewerSource {
    param([string[]]$Dirs, [string[]]$Files, [datetime]$Since)

    foreach ($f in $Files) {
        if ((Test-Path $f) -and [System.IO.File]::GetLastWriteTimeUtc($f) -gt $Since) { return $f }
    }

    $stack = New-Object System.Collections.Generic.Stack[string]
    foreach ($d in $Dirs) { if (Test-Path $d) { $stack.Push($d) } }
    while ($stack.Count -gt 0) {
        $d = $stack.Pop()
        foreach ($f in [System.IO.Directory]::EnumerateFiles($d)) {
            if ($sourceExtensions -notcontains [System.IO.Path]::GetExtension($f).ToLowerInvariant()) { continue }
            if ([System.IO.File]::GetLastWriteTimeUtc($f) -gt $Since) { return $f }
        }
        foreach ($sub in [System.IO.Directory]::EnumerateDirectories($d)) {
            if ([System.IO.Path]::GetFileName($sub) -notmatch $skipDirPattern) { $stack.Push($sub) }
        }
    }
    return $null
}

# Why $name needs publishing, or $null when the deployed build is current.
function Get-PublishReason {
    param([string]$Name, $Sources)

    $exe = Join-Path $catalog[$Name].Out $catalog[$Name].Exe
    if ($Force)                         { return '-Force' }
    if (-not (Test-Path $exe))          { return "$($catalog[$Name].Exe) is not deployed" }
    if (-not (Test-Path $deployBatPath)) { return 'deploy.bat is missing' }

    $exeTime = (Get-Item $exe).LastWriteTimeUtc
    $dirs    = @($Sources.Projects | ForEach-Object { Split-Path $_ })
    $newer   = Find-NewerSource -Dirs $dirs -Files $Sources.Files -Since $exeTime
    if ($newer) { return "source changed: $($newer.Substring($repo.Length + 1))" }
    return $null
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

# -Force: delete this app's build output (bin, obj, publish under the artifacts path) for its
# project and every project it references, so the publish is a genuinely clean rebuild.
function Clear-BuildOutput {
    param($Sources)
    foreach ($p in $Sources.Projects) {
        $projName = [System.IO.Path]::GetFileNameWithoutExtension($p)
        foreach ($kind in 'bin', 'obj', 'publish') {
            $d = Join-Path $artifacts "$kind\$projName"
            if (Test-Path $d) {
                try { Remove-Item -Recurse -Force $d -ErrorAction Stop }
                catch { Write-Host "    (could not fully remove $d : $($_.Exception.Message))" -ForegroundColor DarkYellow }
            }
        }
    }
}

Write-Host ''
Write-Host "  Deploying to $root$(if ($Force) { ' (-Force: clean rebuild)' })" -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $root | Out-Null

$cleaned = @{}
$published = @()
foreach ($name in $Apps) {
    $app  = $catalog[$name]
    $proj = Join-Path $srcRoot $app.Proj
    $out  = $app.Out
    $exe  = Join-Path $out $app.Exe

    if (-not (Test-Path $proj)) {
        Write-Host "  ! $name - project not found at $proj" -ForegroundColor Red
        exit 1
    }

    $sources = Get-AppSources $proj
    $reason  = Get-PublishReason $name $sources
    if (-not $reason) {
        Write-Host "  $name - up to date, skipped" -ForegroundColor DarkGray
        continue
    }

    Write-Host ''
    Write-Host "  $name ($reason)" -ForegroundColor Cyan
    if ($Force) {
        # Shared projects (Prose.Core under the Hub) are cleaned once per run, not once per app.
        $todo = @($sources.Projects | Where-Object { -not $cleaned.ContainsKey($_) })
        $todo | ForEach-Object { $cleaned[$_] = $true }
        Clear-BuildOutput ([pscustomobject]@{ Projects = $todo })
    }
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
        exit 1
    }
    if (-not (Test-Path $exe)) {
        Write-Host "  $name published but $($app.Exe) is not at $exe." -ForegroundColor Red
        exit 1
    }

    # An incremental publish copies an up-to-date bundle with its ORIGINAL timestamp, which can be
    # older than a source file that was touched without changing the build. Stamp the exe so the
    # change check above treats this publish as current instead of republishing on every launch.
    (Get-Item $exe).LastWriteTimeUtc = [DateTime]::UtcNow

    $published += $name
    Write-Host "    -> $exe ($([math]::Round(((Get-Item $exe).Length / 1MB), 1)) MB)" -ForegroundColor Gray
}

# ── deploy.bat ────────────────────────────────────────────────────────────────
# The single entry point in the deploy folder; every desktop shortcut points at it with the app as
# its argument. It redeploys FIRST (this script, which is a fast no-op when nothing changed), then
# starts, so you can never be looking at a stale exe. When the repo is gone or the publish failed
# it still launches whatever is already in the folder, so a missing source tree leaves a working
# icon. Rewritten every run (only when its content differs) so it cannot drift from this script.
$exeLines = $known | ForEach-Object { "if /i `"%APP%`"==`"$_`" set `"EXE=$($catalog[$_].Exe)`"" }
$deployBat = @(
    '@echo off',
    "rem Generated by $repo\deploy.ps1 - edits here are overwritten.",
    'rem Usage: deploy.bat [Hub | Writer | Launcher]',
    'rem   With an app: redeploy that app if its source changed, then start it.',
    'rem   No argument: redeploy every Prose app that changed, then open the Launcher (the app picker).',
    'setlocal',
    "set `"DEPLOY_PS1=$repo\deploy.ps1`"",
    'set "APP=%~1"',
    'set "APPS=%~1"',
    'if "%APP%"=="" set "APP=Launcher"',
    "if `"%APPS%`"==`"`" set `"APPS=$($known -join ',')`"",
    'set "EXE="'
) + $exeLines + @(
    'if not defined EXE (',
    "    echo Unknown app `"%~1`". Valid: $($known -join ', '), or no argument for all of them.",
    '    pause',
    '    exit /b 1',
    ')',
    'title Prose %APP%',
    'if not exist "%DEPLOY_PS1%" goto norepo',
    'powershell -NoProfile -ExecutionPolicy Bypass -File "%DEPLOY_PS1%" -Apps %APPS% -Start %APP%',
    'if not errorlevel 1 exit /b 0',
    'rem Exit 2: deployed and started, but the Hub never answered /api/health. Starting another',
    'rem would only fight the first one for port 5900.',
    'if errorlevel 2 goto startfailed',
    'echo Redeploy failed - launching whatever is already in this folder instead.',
    'goto fallback',
    ':norepo',
    'echo Source repo not found at "%DEPLOY_PS1%" - launching existing build without redeploying.',
    ':fallback',
    'if not exist "%~dp0%EXE%" (',
    '    echo %EXE% is not in %~dp0 - nothing to launch.',
    '    pause',
    '    exit /b 1',
    ')',
    'rem The Hub binds 127.0.0.1 only and is never the Azure deployment. Without an explicit',
    'rem environment it defaults to Production, which makes AddMindAtticAuthentication fail closed.',
    'set "ASPNETCORE_ENVIRONMENT=Development"',
    'start "" "%~dp0%EXE%"',
    'exit /b 0',
    ':startfailed',
    'echo.',
    'echo   DEPLOY FAILED -- %APP% did not come up. See the output above.',
    'pause',
    'exit /b 1'
)
$batText = ($deployBat -join "`r`n") + "`r`n"
$current = if (Test-Path $deployBatPath) { [System.IO.File]::ReadAllText($deployBatPath) } else { '' }
if ($current -ne $batText) {
    [System.IO.File]::WriteAllText($deployBatPath, $batText, [System.Text.Encoding]::ASCII)
    Write-Host ''
    Write-Host "  Wrote $deployBatPath" -ForegroundColor Gray
}

# The per-app launchers deploy.bat replaced. Nothing calls them any more.
foreach ($old in 'launch.bat', 'Hub.bat', 'Writer.bat') {
    $p = Join-Path $root $old
    if (Test-Path $p) {
        Remove-Item $p -Force
        Write-Host "  Removed legacy $p" -ForegroundColor DarkGray
    }
}

Write-Host ''
Write-Host '  Deploy complete.' -ForegroundColor Green
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

# Which Hub an app that depends on one (Writer, Launcher) is about to use, and whether it serves
# the current editor. Both apps find a running Hub or start the deployed one themselves
# (Prose.Writer\HubProcess.cs); this only reports, so the deploy output is not silent about it.
function Show-HubState {
    param([string]$AppName)

    $health = $null
    try { $health = Invoke-RestMethod -Uri $HubHealthUrl -TimeoutSec 3 } catch { }

    # Read through PSObject: under Set-StrictMode -Version Latest a missing property THROWS, and an
    # older Hub's health reply has no builtUtc — which failed the whole deploy after Writer had
    # already been published, printing "DEPLOY FAILED - Nothing was launched".
    function Get-HealthField([string]$Name) {
        if ($null -eq $health -or $health -is [string]) { return $null }
        $prop = $health.PSObject.Properties[$Name]
        if ($prop) { return $prop.Value } else { return $null }
    }

    $hubExe = Join-Path $catalog.Hub.Out $catalog.Hub.Exe
    $built = $null
    if ($health) {
        Write-Host "  Prose Hub is running (PID $(Get-HealthField 'pid'), build $(Get-HealthField 'build')) - $AppName will connect to it." -ForegroundColor Green
        # PowerShell 5.1's JSON reader leaves dates as strings; 7 converts them.
        $builtField = Get-HealthField 'builtUtc'
        if ($builtField -is [datetime]) { $built = $builtField.ToUniversalTime() }
        elseif ($builtField) { $built = [datetime]::Parse($builtField, $null, 'RoundtripKind').ToUniversalTime() }
    }
    elseif ($running = @(Get-Process Hub -ErrorAction SilentlyContinue | Where-Object { $_.Path -and $_.Path -eq $hubExe })) {
        # Running but not healthy (a 503 throws in Invoke-RestMethod): it is NOT absent, and the
        # Writer will wait for it rather than start a second one.
        Write-Host "  Prose Hub is running (PID $($running[0].Id)) but not answering $HubHealthUrl yet - $AppName will wait for it." -ForegroundColor Yellow
        Write-Host "  /api/health is fail-closed: this is usually SQL Server, or migrations still applying." -ForegroundColor DarkYellow
        $built = (Get-Item $hubExe).LastWriteTimeUtc
    }
    else {
        if (-not (Test-Path $hubExe)) {
            Write-Host "  No Hub is running and none is deployed at $hubExe." -ForegroundColor Red
            Write-Host "  $AppName will open but cannot reach one - run deploy.bat Hub." -ForegroundColor Red
            return
        }
        Write-Host "  No Hub is answering - $AppName will start $hubExe." -ForegroundColor Cyan
        $built = (Get-Item $hubExe).LastWriteTimeUtc
    }

    # What the Hub serves: the editor (Prose.WriterUi), its own endpoints, and the services under
    # both. Not Prose.Writer - that is the window, and it was just deployed.
    $served = @('Prose.WriterUi', 'Prose.Hub', 'Prose.Core') | ForEach-Object { Join-Path $srcRoot $_ }
    $newest = Get-ChildItem $served -Recurse -File -Include *.cs, *.razor, *.css, *.js, *.csproj -ErrorAction SilentlyContinue |
              Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } |
              Sort-Object LastWriteTimeUtc -Descending |
              Select-Object -First 1

    if ($built -and $newest -and $newest.LastWriteTimeUtc -gt $built) {
        Write-Host "  ! That Hub was built $($built.ToLocalTime()), before the latest change to what it serves:" -ForegroundColor Yellow
        Write-Host "      $($newest.FullName.Substring($srcRoot.Length + 1)) ($($newest.LastWriteTime))" -ForegroundColor DarkYellow
        Write-Host "    The editor inside $AppName comes from the Hub, so that change is not in it yet." -ForegroundColor DarkYellow
        Write-Host "    Run deploy.bat Hub to pick it up." -ForegroundColor DarkYellow
    }
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
        # started one, and deploy.bat's "DEPLOY FAILED" branch is what should fire (exit 2).
        if (Test-HubHealthy) {
            Write-Host "  Prose Hub is already running and healthy at $HubBaseUrl - connecting to it, not starting a second." -ForegroundColor Green
            if ($published -contains 'Hub') {
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
                exit 2
            }
        }
        Write-Host "  Prose Hub is up and reachable at $HubBaseUrl." -ForegroundColor Green
    }
    else {
        # Writer and Launcher attach to the Hub, and start the deployed one when none is up.
        # Skipped when the Hub was published in this same run: it was stopped for that, and the
        # app starting it is exactly what comes next.
        if ($published -notcontains 'Hub') { Show-HubState $Start }
        Write-Host "  Starting $Start..." -ForegroundColor Cyan
        Start-Process $startExe -WorkingDirectory $startApp.Out
    }
}
exit 0
