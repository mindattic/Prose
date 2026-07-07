param(
    [string]$Providers = "claude-api",
    [string]$Effort    = "deep",
    [switch]$Delta
)

# GLMZ universe stories only (GIW is Fantasy/Cauld - separate universe, excluded)
$codes = @("ATTE","CxC","DWIACE","MNEMO","MxG","NxR","PNHL","SPRW","SRZR","STSH","TEST","UNDR","VATD")

$failed = @()
$start  = Get-Date

$total = $codes.Count
$i = 0

foreach ($code in $codes) {
    $i++
    $elapsed = (Get-Date) - $start
    Write-Host ""
    Write-Host "[$i/$total] $code  (elapsed $([int]$elapsed.TotalMinutes)m)" -ForegroundColor Cyan

    $deltaFlag = if ($Delta) { "--delta" } else { "" }
    dotnet run --project v3/StreetSamurai.Cli -- `
        --review-node --code $code $deltaFlag `
        --providers $Providers --effort $Effort --allow-votes

    if ($LASTEXITCODE -ne 0) {
        Write-Host "  FAILED: $code (exit $LASTEXITCODE)" -ForegroundColor Red
        $failed += $code
    }
}

$elapsed = (Get-Date) - $start
Write-Host ""
Write-Host "[review-all] Done. $($total - $failed.Count)/$total succeeded in $([int]$elapsed.TotalMinutes)m." -ForegroundColor Green
if ($failed.Count -gt 0) {
    Write-Host "[review-all] Failed: $($failed -join ', ')" -ForegroundColor Red
}
