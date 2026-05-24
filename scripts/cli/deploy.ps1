# deploy.ps1 -- StreetSamurai deploy pipeline.
#
# StreetSamurai ships via GitHub Actions: a push to master triggers
# .github/workflows/azure-deploy.yml, which restores → publishes → pushes
# to the Azure App Service "streetsamurai" slot. This local /deploy
# wrapper makes sure the working tree is fully prepared before that fires:
#
#   1. Pulls subscribed components from the sibling MindAttic.UiUx
#      repo via sync-streetsamurai.ps1 (CSS markers in wwwroot/app.css,
#      JS files in wwwroot/js, etc.). Skip with -NoSync.
#   2. Runs a local Release build of the Blazor host so we catch
#      compile errors before they reach GitHub Actions. Skip with -NoBuild.
#   3. Reports any uncommitted changes from the sync.
#   4. With -Push: stages those changes, commits, and pushes master to
#      trigger the Azure deploy. Without -Push: prints the staged diff
#      and tells the user to commit + push manually.
#
# The -Push gate is intentional: pushing to master triggers a production
# deploy. We don't want a sync script silently triggering that.
#
# Usage:
#   powershell -File scripts/cli/deploy.ps1              # sync + build, no push
#   powershell -File scripts/cli/deploy.ps1 -Push        # sync + build + push
#   powershell -File scripts/cli/deploy.ps1 -NoBuild     # sync only
#   powershell -File scripts/cli/deploy.ps1 -NoSync      # build only (skip sync)

param(
    [switch]$NoSync,
    [switch]$NoBuild,
    [switch]$Push
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot   = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$blazorProj = Join-Path $repoRoot 'v3\StreetSamurai.Blazor\StreetSamurai.Blazor.csproj'
$blazorRoot = Split-Path -Parent $blazorProj

# ---------------------------------------------------------------------------
# 1. Pull subscribed components from MindAttic.UiUx
# ---------------------------------------------------------------------------
# Sibling-repo discovery mirrors the pattern in v3/StreetSamurai.Blazor/
# StreetSamurai.Blazor.csproj's SyncMindAtticComponents target. Override
# location with MINDATTIC_COMPONENTS_ROOT when the repo lives elsewhere.
if (-not $NoSync) {
    $componentsRoot = if ($env:MINDATTIC_COMPONENTS_ROOT) {
        $env:MINDATTIC_COMPONENTS_ROOT
    } else {
        Join-Path (Split-Path -Parent $repoRoot) 'MindAttic.UiUx'
    }
    $syncScript = Join-Path $componentsRoot 'sync\sync-streetsamurai.ps1'

    if (-not (Test-Path $syncScript)) {
        Write-Warning "MindAttic.UiUx sync script not found at: $syncScript"
        Write-Warning "Skipping component sync. Set MINDATTIC_COMPONENTS_ROOT or pass -NoSync to silence."
    } else {
        Write-Host "==> Syncing MindAttic.UiUx -> StreetSamurai ..."
        & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $syncScript -BlazorRoot $blazorRoot
        if ($LASTEXITCODE -ne 0) {
            Write-Error "sync-streetsamurai.ps1 failed (exit $LASTEXITCODE) -- aborting deploy."
        }
        Write-Host "    sync OK"
        Write-Host ""
    }
}

# ---------------------------------------------------------------------------
# 2. Local Release build -- catches compile errors before GitHub Actions does
# ---------------------------------------------------------------------------
if (-not $NoBuild) {
    Write-Host "==> Building StreetSamurai.Blazor (Release) ..."
    Push-Location $repoRoot
    try {
        & dotnet build $blazorProj -c Release --nologo
        if ($LASTEXITCODE -ne 0) {
            Write-Error "dotnet build failed (exit $LASTEXITCODE) -- fix errors before deploying."
        }
    } finally { Pop-Location }
    Write-Host "    build OK"
    Write-Host ""
}

# ---------------------------------------------------------------------------
# 3. Inspect git status for uncommitted sync output
# ---------------------------------------------------------------------------
Push-Location $repoRoot
try {
    $status = & git status --porcelain
} finally { Pop-Location }

if (-not $status) {
    Write-Host "==> Working tree clean. Nothing to commit; nothing to deploy."
    if ($Push) { Write-Host "    (-Push had no effect -- sync produced no changes.)" }
    exit 0
}

Write-Host "==> Uncommitted changes after sync:"
Write-Host ("-" * 60)
$status | ForEach-Object { Write-Host "    $_" }
Write-Host ("-" * 60)
Write-Host ""

# ---------------------------------------------------------------------------
# 4. -Push branch: commit + push to trigger Azure deploy
# ---------------------------------------------------------------------------
if (-not $Push) {
    Write-Host "Not pushing (default mode is sync + build, no push)."
    Write-Host "Review the diff above, then either:"
    Write-Host "  *commit + push manually, or"
    Write-Host "  *re-run with -Push to commit and push these changes."
    exit 0
}

Push-Location $repoRoot
try {
    $branch = (& git rev-parse --abbrev-ref HEAD).Trim()
    if ($branch -ne 'master' -and $branch -ne 'main') {
        Write-Warning "Current branch is '$branch'. Azure deploy only fires for master."
        Write-Warning "Push will go to '$branch' -- won't trigger production deploy."
    }

    Write-Host "==> Staging sync output ..."
    & git add v3/StreetSamurai.Blazor/wwwroot
    if ($LASTEXITCODE -ne 0) { Write-Error "git add failed." }

    $stamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
    $msg   = "Sync MindAttic.UiUx for deploy ($stamp)"
    Write-Host "==> Committing: $msg"
    & git commit -m $msg
    if ($LASTEXITCODE -ne 0) { Write-Error "git commit failed." }

    Write-Host "==> Pushing $branch ..."
    & git push origin $branch
    if ($LASTEXITCODE -ne 0) { Write-Error "git push failed." }
} finally { Pop-Location }

Write-Host ""
Write-Host "==> Done. Azure CI/CD will build + deploy from the push."

# Best-effort: derive the GitHub Actions URL from the remote so the user can
# click through to watch the run. Failures are silent -- this is decoration.
try {
    $originUrl = (& git -C $repoRoot remote get-url origin).Trim()
    $slug = [regex]::Match($originUrl, '[:/]([^/]+/[^/]+?)(\.git)?$').Groups[1].Value
    if ($slug) {
        Write-Host "    Watch progress: https://github.com/$slug/actions"
    }
} catch { }
exit 0
