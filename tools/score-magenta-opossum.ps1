# score-magenta-opossum.ps1
# Entity scoring job for Magenta Opossum (A100 SXM). Runs all non-character types
# in sequence; --unrated skips anything already scored by either pod.
#
# Usage: pwsh tools/score-magenta-opossum.ps1

$MagentaUrl   = "https://tqxz1z99hvtvcq-8000.proxy.runpod.net"
$MagentaKey   = "vllm_key_aJaFlrQrTzfyhVPC6dWoqDonVaQAxd060oYwo81f"
$MagentaModel = "Qwen/Qwen3-32B"

$LogFile   = "entity-scoring-magenta.log"
$StartTime = Get-Date

Write-Host "=== Magenta Opossum (A100 SXM) entity scoring — ALL types ==="
Write-Host "URL   : $MagentaUrl"
Write-Host "Model : $MagentaModel"
Write-Host ""

dotnet "D:\Temp\olive-build\StreetSamurai.Cli.dll" `
    --review-entity `
    --ballots 10 --prose 2 --unrated `
    "--local-url" $MagentaUrl `
    "--local-key" $MagentaKey `
    "--local-model" $MagentaModel `
    2>&1 | Tee-Object -FilePath $LogFile -Append

$Elapsed = [datetime]::UtcNow - $StartTime
Write-Host "=== Magenta Opossum done. Elapsed: $($Elapsed.ToString('hh\:mm\:ss')) ==="
