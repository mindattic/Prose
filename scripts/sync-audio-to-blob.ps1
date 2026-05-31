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

Write-Host "[sync-audio-to-blob] Reconciling local and blob (newest wins per beat)..." -ForegroundColor Cyan
Push-Location $BlazorProj
try {
    # Single bidirectional pass: for every beat, the side with the newer
    # last-modified timestamp wins and gets copied to the other. Same code
    # path the always-on AudioReconciliationBackgroundService runs, but
    # invoked synchronously here so the deploy waits for it.
    #
    # -c Release is required: MindAttic.Deploy's preDeploy step builds this
    # project in Release, so --no-build must target Release too. Without it,
    # `dotnet run` defaults to Debug and reuses a stale (possibly months-old)
    # Debug build that predates recent fixes -- which is how a resolved
    # AudioStore:ConnectionString could still throw at deploy time.
    & dotnet run -c Release --no-build --no-restore -- --sync-audio
    $code = $LASTEXITCODE
} finally {
    Pop-Location
}

if ($code -eq 0) {
    Write-Host "[sync-audio-to-blob] OK: local + blob in sync." -ForegroundColor Green
} elseif ($code -eq 1) {
    Write-Warning '[sync-audio-to-blob] Some copies failed. Re-running is safe; fix connectivity then deploy again.'
} elseif ($code -eq 2) {
    Write-Warning '[sync-audio-to-blob] Audio store is not in dual-write mode. Configure AudioStore:Provider=dual.'
}

exit $code
