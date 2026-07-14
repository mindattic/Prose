param(
    [string]$Providers = "claude-api",
    [string]$Effort    = "deep",
    [switch]$Delta
)

# GLMZ universe stories only (GIW is Fantasy/Caul - separate universe, excluded)
# Excluded: BCODA (WIP/redesign), JOY (not cleared), SM1/SM2 (delete-ready), SS/Rook (series roots)
$codes = @("ATTE","BLST","CxC","DWIACE","IxS","MNEMO","MxG","NxR","PNHL","SPRW","SRZR","STSH","TEST","UNDR","VATD")

# Slug-only stories (no NodeCode set)
$slugs = @("it-came-from-iowa-019f3eb2")

$failed = @()
$start  = Get-Date
$deltaFlag = if ($Delta) { "--delta" } else { "" }

$total = $codes.Count + $slugs.Count
$i = 0

foreach ($code in $codes) {
    $i++
    $elapsed = (Get-Date) - $start
    Write-Host ""
    Write-Host "[$i/$total] $code  (elapsed $([int]$elapsed.TotalMinutes)m)" -ForegroundColor Cyan

    dotnet run --project v3/StreetSamurai.Cli -- `
        --review-node --code $code $deltaFlag `
        --providers $Providers --effort $Effort --allow-votes

    if ($LASTEXITCODE -ne 0) {
        Write-Host "  FAILED: $code (exit $LASTEXITCODE)" -ForegroundColor Red
        $failed += $code
    }
}

foreach ($slug in $slugs) {
    $i++
    $elapsed = (Get-Date) - $start
    Write-Host ""
    Write-Host "[$i/$total] $slug  (elapsed $([int]$elapsed.TotalMinutes)m)" -ForegroundColor Cyan

    dotnet run --project v3/StreetSamurai.Cli -- `
        --review-node --slug $slug $deltaFlag `
        --providers $Providers --effort $Effort --allow-votes

    if ($LASTEXITCODE -ne 0) {
        Write-Host "  FAILED: $slug (exit $LASTEXITCODE)" -ForegroundColor Red
        $failed += $slug
    }
}

$elapsed = (Get-Date) - $start
Write-Host ""
Write-Host "[review-all] Done. $($total - $failed.Count)/$total succeeded in $([int]$elapsed.TotalMinutes)m." -ForegroundColor Green
if ($failed.Count -gt 0) {
    Write-Host "[review-all] Failed: $($failed -join ', ')" -ForegroundColor Red
}
