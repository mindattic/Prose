#Requires -Version 5.1
<#
.SYNOPSIS
    Shuts down any running Prose.Hub, clears build cache, rebuilds, and publishes a
    standalone copy to C:\Apps\Prose\Prose.Hub\ - same pattern as Prose.KdpPublish's own
    tools/deploy.ps1 (see that file's own doc comment for the original design). Each
    Prose executable gets its own subfolder under C:\Apps\Prose\ (Prose.Hub today,
    room for more alongside it later) rather than one flat folder.

.DESCRIPTION
    1. Stops any running Prose.Hub.exe process.
    2. Removes bin/obj for Prose.Hub + Prose.Mcp + Prose.Core - a clean, timestamp-
       independent rebuild, not an incremental one (Prose.Hub references Prose.Mcp for
       ToolDispatch's reflection over the migrated MCP tool classes, and Prose.Core for
       everything else - all three need to be current together).
    3. dotnet publish (Release, win-x64, framework-dependent single file) -> C:\Apps\Prose\.
       Includes wwwroot (the dashboard) automatically, since this is a Web SDK project.
    4. Writes C:\Apps\Prose\launch.bat - always redeploys from source before launching
       (same reasoning as KdpPublish's launcher: never risk running a stale build), with
       a graceful fallback to the existing exe if the source repo path is missing.

.PARAMETER Launch
    After publishing, immediately run C:\Apps\Prose\launch.bat.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\deploy.ps1
    powershell -ExecutionPolicy Bypass -File tools\deploy.ps1 -Launch
#>
param([switch]$Launch)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$projDir = Split-Path $PSScriptRoot   # tools\ -> Prose.Hub\
$proj    = Join-Path $projDir 'Prose.Hub.csproj'
$out     = 'C:\Apps\Prose\Prose.Hub'
$exeName = 'Prose.Hub.exe'

# ── Stop running instance ──────────────────────────────────────────────────
Write-Host ''
Write-Host '  Stopping running instance...' -ForegroundColor Yellow
$procs = Get-Process 'Prose.Hub' -ErrorAction SilentlyContinue
if ($procs) {
    $procs | Stop-Process -Force
    Write-Host "    Stopped ($(@($procs).Count) process(es))" -ForegroundColor DarkYellow
} else {
    Write-Host '    Nothing running.' -ForegroundColor DarkGray
}

# A fixed sleep here isn't reliable: a 60+ MB single-file bundle can hold its own exe's file
# handle open for longer than the process object takes to report as exited (native library
# self-extraction/cleanup lags a confirmed Stop-Process). Found live: deploy failed with
# "Access to the path ... is denied" / GenerateBundle task failure even after Stop-Process
# had already succeeded and a fixed 800ms wait had elapsed. Poll until the target exe is
# actually openable for exclusive write instead of guessing a fixed delay.
$targetExe = Join-Path $out $exeName
if (Test-Path $targetExe) {
    Write-Host '  Waiting for file lock to release...' -ForegroundColor Yellow
    $unlocked = $false
    for ($i = 0; $i -lt 20; $i++) {
        try {
            $fs = [System.IO.File]::Open($targetExe, 'Open', 'Write', 'None')
            $fs.Close()
            $unlocked = $true
            break
        } catch {
            Start-Sleep -Milliseconds 500
        }
    }
    if ($unlocked) {
        Write-Host "    Released after $([math]::Round($i * 0.5, 1))s" -ForegroundColor DarkGray
    } else {
        Write-Host '    Still locked after 10s - proceeding anyway, publish may fail.' -ForegroundColor DarkYellow
    }
}

# ── Clear build cache ──────────────────────────────────────────────────────
Write-Host ''
Write-Host '  Clearing build cache (bin/obj)...' -ForegroundColor Cyan
foreach ($rel in @('Prose.Hub', 'Prose.Mcp', 'Prose.Core')) {
    $projRoot = Resolve-Path (Join-Path $projDir "..\$rel") -ErrorAction SilentlyContinue
    if (-not $projRoot) { continue }
    foreach ($d in @((Join-Path $projRoot 'bin'), (Join-Path $projRoot 'obj'))) {
        if (Test-Path $d) {
            try { Remove-Item -Recurse -Force $d -ErrorAction Stop }
            catch { Write-Host "    (could not fully remove $d : $($_.Exception.Message))" -ForegroundColor DarkYellow }
        }
    }
}

# ── Publish ─────────────────────────────────────────────────────────────────
Write-Host ''
Write-Host "  Publishing $proj" -ForegroundColor Cyan
Write-Host "    -> $out\$exeName"

dotnet publish $proj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained false `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    --output $out | Out-Host

if ($LASTEXITCODE -ne 0) {
    Write-Host "Publish failed (exit $LASTEXITCODE)." -ForegroundColor Red
    exit $LASTEXITCODE
}

$exe = Join-Path $out $exeName
if (-not (Test-Path $exe)) {
    Write-Host "Publish completed but $exeName not found at $exe." -ForegroundColor Red
    exit 1
}

# ── Write self-redeploying launcher ─────────────────────────────────────────
$deployPs1Path = Join-Path $PSScriptRoot 'deploy.ps1'
$launchBatPath = Join-Path $out 'launch.bat'
@(
    '@echo off',
    'title Prose Hub',
    "set `"DEPLOY_PS1=$deployPs1Path`"",
    'if exist "%DEPLOY_PS1%" (',
    '    echo Redeploying latest build from source...',
    '    powershell -NoProfile -ExecutionPolicy Bypass -File "%DEPLOY_PS1%"',
    '    if errorlevel 1 echo Redeploy failed - launching whatever is already in this folder instead.',
    ') else (',
    '    echo Source repo not found at "%DEPLOY_PS1%" - launching existing build without redeploying.',
    ')',
    'rem Local-only Hub (binds 127.0.0.1, never the thing deployed to prose.azurewebsites.net',
    'rem via azure-deploy.yml, a completely separate pipeline) - with neither DOTNET_ENVIRONMENT',
    'rem nor ASPNETCORE_ENVIRONMENT set, ASP.NET Core defaults EnvironmentName to "Production",',
    'rem which makes AddMindAtticAuthentication fail-close (no ConfigureDataProtection configured',
    'rem for local dev) and --reset-password silently unavailable via the Hub. Correcting the',
    'rem environment classification here (this launcher is regenerated fresh every deploy, not a',
    'rem production artifact) is the honest fix - not loosening the library''s production safety',
    'rem check itself, which stays intact in Program.cs for an actual production deployment.',
    'set "ASPNETCORE_ENVIRONMENT=Development"',
    "start `"`" `"%~dp0$exeName`""
) | Set-Content $launchBatPath -Encoding ascii

# ── Summary ────────────────────────────────────────────────────────────────
Write-Host ''
Write-Host '  Published successfully.' -ForegroundColor Green
Write-Host "    Exe    : $exe ($([math]::Round(((Get-Item $exe).Length / 1MB), 1)) MB)" -ForegroundColor Gray
Write-Host "    Launch : $launchBatPath" -ForegroundColor Gray
Write-Host ''

if ($Launch) {
    Write-Host '  Launching...' -ForegroundColor Cyan
    Start-Process $launchBatPath
}
