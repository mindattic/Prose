<#
  SessionStart hook - ensures the Prose Hub (v3/Prose.Hub, the standalone always-on
  service holding the resident UniverseGraphService/DocContextStack/EntityContextStack
  "Trinity" + the migrated MCP-tool/CLI-command dispatch) is running AND current,
  redeploying automatically when source has changed since the last deploy.

  Phase 2 (explicit user decision): "the hub is running, Prose is working; hub goes
  down, Prose is down." Prose.Cli and Prose.Mcp both hard-gate on the Hub being healthy
  at startup (see Prose.Core.Services.HubGate) - this hook exists so that gate almost
  never actually fires in practice, not because the Hub is optional.

  Staleness must be automatic, not a manual step (explicit user requirement - "you must
  make deployment seamless and easy"): Prose.Hub bundles Prose.Cli + Prose.Mcp +
  Prose.Core into ONE deployed exe (C:\Apps\MindAttic\Prose\Hub.exe, written by
  v3\tools\deploy-apps.ps1). Any source change to any of those four projects means the
  deployed exe no longer reflects reality until redeployed. This hook compares the
  deployed exe's timestamp against the newest .cs file across all four project trees and
  redeploys automatically when it's behind - the fast path (nothing changed) just
  health-checks and starts the existing exe directly, never re-invoking the full
  rebuild+republish for no reason.

  Scope, 2026-09-12 (explicit user decision): this hook deploys and runs THE HUB ONLY, and
  a session then uses that build for its whole life. Writer.exe and KdpPublish.exe are
  never touched here - they are separate single-file bundles, a session that never opens
  them should not pay to republish them, and republishing an exe the user is looking at
  would stop it. Rebuilding all of them together is what the /redeploy skill is for
  (.prose/commands/redeploy.md).
#>
$ErrorActionPreference = 'Continue'

$repoRoot   = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$healthUrl  = 'http://127.0.0.1:5900/api/health'
# Moved 2026-09-11 from C:\Apps\Prose\Prose.Hub\Prose.Hub.exe: every Prose app now deploys into
# one folder, C:\Apps\MindAttic\Prose\, and the Hub's assembly is Hub. The old location is still
# checked so a machine that has not run the new deploy yet still finds a Hub to start.
$deployedExe = 'C:\Apps\MindAttic\Prose\Hub.exe'
$legacyExe   = 'C:\Apps\Prose\Prose.Hub\Prose.Hub.exe'
if (-not (Test-Path $deployedExe) -and (Test-Path $legacyExe)) { $deployedExe = $legacyExe }
$deployPs1  = Join-Path $repoRoot 'v3\tools\deploy-apps.ps1'
$proj       = Join-Path $repoRoot 'v3\Prose.Hub\Prose.Hub.csproj'

function Test-HubHealthy {
    try {
        $resp = Invoke-WebRequest -Uri $healthUrl -UseBasicParsing -TimeoutSec 2
        return $resp.StatusCode -eq 200
    } catch { return $false }
}

function Get-NewestSourceMtime {
    $roots = @('Prose.Hub', 'Prose.Mcp', 'Prose.Cli', 'Prose.Core') |
        ForEach-Object { Join-Path $repoRoot "v3\$_" } | Where-Object { Test-Path $_ }
    $newest = [DateTime]::MinValue
    foreach ($root in $roots) {
        $files = Get-ChildItem -Path $root -Recurse -Filter '*.cs' -File -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
        foreach ($f in $files) { if ($f.LastWriteTimeUtc -gt $newest) { $newest = $f.LastWriteTimeUtc } }
    }
    return $newest
}

$startedSomething = $false

try {
    # This Hub is local-only (binds 127.0.0.1; never the thing deployed to
    # prose.azurewebsites.net via azure-deploy.yml, a completely separate pipeline). With
    # neither DOTNET_ENVIRONMENT nor ASPNETCORE_ENVIRONMENT set, ASP.NET Core defaults
    # EnvironmentName to "Production", which makes AddMindAtticAuthentication fail-close (no
    # ConfigureDataProtection configured for local dev) and silently drops --reset-password
    # from the Hub at every startup. Start-Process inherits the parent's environment block, so
    # setting this here (not in Program.cs, which keeps the library's real production safety
    # check intact) covers all three launch paths below.
    $env:ASPNETCORE_ENVIRONMENT = 'Development'

    $needsRedeploy = $false
    if (-not (Test-Path $deployedExe)) {
        $needsRedeploy = $true
    } else {
        $exeTime = (Get-Item $deployedExe).LastWriteTimeUtc
        $srcTime = Get-NewestSourceMtime
        if ($srcTime -gt $exeTime) { $needsRedeploy = $true }
    }

    if ($needsRedeploy -and (Test-Path $deployPs1)) {
        # Only the Hub: a session start has no reason to republish Writer/Launcher/KdpPublish,
        # and each one is a separate single-file bundle.
        & powershell -NoProfile -ExecutionPolicy Bypass -File $deployPs1 -Apps Hub *> $null
    }

    if (-not (Test-HubHealthy)) {
        if (Test-Path $deployedExe) {
            Start-Process -FilePath $deployedExe -WorkingDirectory (Split-Path $deployedExe) -WindowStyle Normal
            $startedSomething = $true
        } elseif (Test-Path $proj) {
            # Deployed copy doesn't exist and deploy.ps1 isn't available/failed - fall back to
            # an ad-hoc source build so the Hub is at least running somehow.
            & dotnet build $proj --configuration Release *> $null
            $exeDir = Join-Path $repoRoot 'v3\Prose.Hub\bin\Release\net10.0'
            $exe    = Join-Path $exeDir 'Hub.exe'
            if (Test-Path $exe) {
                Start-Process -FilePath $exe -WorkingDirectory $exeDir -WindowStyle Normal
                $startedSomething = $true
            } else {
                Start-Process -FilePath 'dotnet' -ArgumentList @('run', '--project', $proj, '--no-build', '--configuration', 'Release') -WorkingDirectory $repoRoot -WindowStyle Normal
                $startedSomething = $true
            }
        }
    }
} catch {
    Write-Error "[start-prose-hub] failed to redeploy/launch: $_"
}

# Final reachability check, surfaced to both the user (systemMessage) and the model
# (additionalContext) at session start - per author request 2026-09-04, "mcp up now?"
# should not require an ad-hoc tool call every session. Start-Process above is async, so
# when we just launched something, poll briefly instead of judging on the first sample.
$healthy = Test-HubHealthy
if (-not $healthy -and $startedSomething) {
    for ($i = 0; $i -lt 10 -and -not $healthy; $i++) {
        Start-Sleep -Seconds 1
        $healthy = Test-HubHealthy
    }
}

if ($healthy) {
    # Name the build that is actually serving. The whole point of deploying to a fixed path is
    # that a session is never unknowingly talking to a debug build from `dotnet run`.
    $serving = Get-Process 'Hub', 'Prose.Hub' -ErrorAction SilentlyContinue |
               Where-Object { $_.Path } | Select-Object -First 1 -ExpandProperty Path
    $from = if ($serving) { $serving } else { $deployedExe }
    $msg  = "[Prose Hub] MCP reachable - $healthUrl responded OK. Serving from $from " +
            "(rebuild everything with /redeploy)."
} else {
    $msg = "[Prose Hub] MCP UNREACHABLE - $healthUrl did not respond. Prose MCP tools and " +
           "`prose` CLI commands will fail until the Hub is running (see Prose.Core.Services.HubGate). " +
           "Check for a stuck Hub.exe process or a port 5900 conflict."
}

@{
    systemMessage = $msg
    hookSpecificOutput = @{ hookEventName = 'SessionStart'; additionalContext = $msg }
} | ConvertTo-Json -Depth 5 -Compress
exit 0
