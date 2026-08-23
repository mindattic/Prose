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
  Prose.Core into ONE deployed exe (C:\Apps\Prose\Prose.Hub\Prose.Hub.exe, written by
  v3\Prose.Hub\tools\deploy.ps1 - same pattern as Prose.KdpPublish). Any source change
  to any of those four projects means the deployed exe no longer reflects reality until
  redeployed. This hook compares the deployed exe's timestamp against the newest .cs
  file across all four project trees and redeploys automatically when it's behind -
  the fast path (nothing changed) just health-checks and starts the existing exe
  directly, never re-invoking the full rebuild+republish for no reason.
#>
$ErrorActionPreference = 'Continue'

$repoRoot   = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$healthUrl  = 'http://127.0.0.1:5900/api/health'
$deployedExe = 'C:\Apps\Prose\Prose.Hub\Prose.Hub.exe'
$deployPs1  = Join-Path $repoRoot 'v3\Prose.Hub\tools\deploy.ps1'
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

try {
    $needsRedeploy = $false
    if (-not (Test-Path $deployedExe)) {
        $needsRedeploy = $true
    } else {
        $exeTime = (Get-Item $deployedExe).LastWriteTimeUtc
        $srcTime = Get-NewestSourceMtime
        if ($srcTime -gt $exeTime) { $needsRedeploy = $true }
    }

    if ($needsRedeploy -and (Test-Path $deployPs1)) {
        & powershell -NoProfile -ExecutionPolicy Bypass -File $deployPs1 *> $null
    }

    if (-not (Test-HubHealthy)) {
        if (Test-Path $deployedExe) {
            Start-Process -FilePath $deployedExe -WorkingDirectory (Split-Path $deployedExe) -WindowStyle Normal
        } elseif (Test-Path $proj) {
            # Deployed copy doesn't exist and deploy.ps1 isn't available/failed - fall back to
            # an ad-hoc source build so the Hub is at least running somehow.
            & dotnet build $proj --configuration Release *> $null
            $exeDir = Join-Path $repoRoot 'v3\Prose.Hub\bin\Release\net10.0'
            $exe    = Join-Path $exeDir 'Prose.Hub.exe'
            if (Test-Path $exe) {
                Start-Process -FilePath $exe -WorkingDirectory $exeDir -WindowStyle Normal
            } else {
                Start-Process -FilePath 'dotnet' -ArgumentList @('run', '--project', $proj, '--no-build', '--configuration', 'Release') -WorkingDirectory $repoRoot -WindowStyle Normal
            }
        }
    }
} catch {
    Write-Error "[start-prose-hub] failed to redeploy/launch: $_"
}

Write-Output '{}'
exit 0
