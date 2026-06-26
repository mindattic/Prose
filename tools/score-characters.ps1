# score-characters.ps1
# Waits for the current entity scoring job (non-characters) to finish,
# then launches the character scoring pass.
#
# Usage: pwsh tools/score-characters.ps1

$RunPodUrl   = "https://tqxz1z99hvtvcq-8000.proxy.runpod.net"
$RunPodKey   = "vllm_key_aJaFlrQrTzfyhVPC6dWoqDonVaQAxd060oYwo81f"
$RunPodModel = "qwen2.5-72b-32k"

Write-Host "Waiting for non-character scoring job to finish..."
while (Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object { $_.StartTime -gt (Get-Date).AddHours(-12) }) {
    Write-Host "  Still running... $(Get-Date -Format 'HH:mm:ss')"
    Start-Sleep -Seconds 120
}

Write-Host "Non-character job complete. Starting character scoring (1714 entities, 10 ballots each)..."
$logFile    = "entity-scoring-characters.log"
$logFileErr = "entity-scoring-characters-err.log"

dotnet run --project v3/StreetSamurai.Blazor -- `
    --review-entity --type character `
    --ballots 10 --prose 2 --unrated `
    "--local-url" $RunPodUrl `
    "--local-key" $RunPodKey `
    "--local-model" $RunPodModel `
    2>&1 | Tee-Object -FilePath $logFile

Write-Host "Character scoring complete. Log: $logFile"
