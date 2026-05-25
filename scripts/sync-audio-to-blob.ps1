# sync-audio-to-blob.ps1
# Pre-deploy hook called by MindAttic.Deploy. Pushes any locally-recorded
# beats whose bytes haven't made it to Azure Blob yet -- the catch-net for
# DualWriteAudioStore's background-upload failures (network blips, offline
# recordings, etc).
#
# Idempotent: re-running on an already-in-sync corpus is a fast no-op
# (one ExistsAsync round-trip per beat, ~3-5s for 1000 beats).
#
# Exit codes propagated from ss --sync-audio:
#   0 = in-sync or repaired successfully
#   1 = at least one upload failed; deploy should pause for investigation
#   2 = config error (no connection string, etc.)
#
# ASCII-only: PowerShell 5.1 parses .ps1 files as Windows-1252 by default,
# so non-ASCII characters in strings can confuse the parser into reading
# parens as sub-expressions. Stick to ASCII here.

$ErrorActionPreference = 'Stop'

# Repo root is one level up from this script's dir.
$RepoRoot = Resolve-Path "$PSScriptRoot\.."
$BlazorProj = Join-Path $RepoRoot "v3\StreetSamurai.Blazor"

if (-not (Test-Path $BlazorProj)) {
    Write-Error "[sync-audio-to-blob] Expected Blazor project at $BlazorProj"
    exit 1
}

Write-Host "[sync-audio-to-blob] Pushing local-only audio to blob..." -ForegroundColor Cyan
Push-Location $BlazorProj
try {
    # --push only: we never pull from blob to local during deploy. The
    # deploy-time job is to ensure cloud has everything local has, NOT to
    # rehydrate the local cache with cloud-only files. Manual --pull is
    # for that.
    & dotnet run --no-build --no-restore -- --sync-audio --push
    $code = $LASTEXITCODE
} finally {
    Pop-Location
}

if ($code -eq 0) {
    Write-Host "[sync-audio-to-blob] OK: local + blob in sync." -ForegroundColor Green
} elseif ($code -eq 1) {
    Write-Warning '[sync-audio-to-blob] Some uploads failed. Re-running is safe; fix Azure connectivity then deploy again.'
} elseif ($code -eq 2) {
    Write-Warning '[sync-audio-to-blob] Config error: verify dotnet user-secrets has AudioStore:ConnectionString set.'
}

exit $code
