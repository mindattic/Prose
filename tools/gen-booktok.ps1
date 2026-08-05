# Standalone #booktok announcement video generator — no DB required.
#
# Composites -CoverPath onto a 3D mockup template, then generates a short AI
# image-to-video clip (hand shows the cover, opens it, flips pages) via the chosen
# provider, and assembles a vertical 1080x1920 MP4. Costs real money per call unless
# -DryRun, which stops after the local ImageMagick mockup + payload validation.
#
# NOTE: the AI clip's page-flip motion is generic/blurred — there is no real interior
# page-spread art to render, so it will not show legible page content.
#
# Usage:
#   tools/gen-booktok.ps1 -CoverPath cover.jpg -Title "Attendance" -Provider kling -DryRun
#   tools/gen-booktok.ps1 -CoverPath cover.jpg -Title "Attendance" -Provider runway -Yes

param(
    [Parameter(Mandatory = $true)]
    [string]$CoverPath,

    [Parameter(Mandatory = $true)]
    [string]$Title,

    [Parameter(Mandatory = $true)]
    [ValidateSet("kling", "runway", "sora")]
    [string]$Provider,

    [string]$Template = "default",
    [int]$Duration = 8,
    [string]$Prompt,
    [switch]$DryRun,
    [switch]$Yes
)

if (-not (Test-Path $CoverPath)) {
    Write-Host "[gen-booktok] Cover not found: $CoverPath" -ForegroundColor Red
    exit 1
}

$dotnetArgs = @(
    "run", "--project", "v3/StreetSamurai.Cli", "--",
    "--booktok", "--standalone",
    "--cover-path", $CoverPath,
    "--title", $Title,
    "--provider", $Provider,
    "--template", $Template,
    "--duration", $Duration
)
if ($Prompt) { $dotnetArgs += @("--prompt", $Prompt) }
if ($DryRun) { $dotnetArgs += "--dry-run" }
if ($Yes)    { $dotnetArgs += "--yes" }

Write-Host ""
Write-Host "[gen-booktok] $Title  (provider=$Provider, duration=${Duration}s$(if ($DryRun) { ', DRY RUN' }))" -ForegroundColor Cyan

& dotnet @dotnetArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host "[gen-booktok] FAILED (exit $LASTEXITCODE)" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "[gen-booktok] Done." -ForegroundColor Green
