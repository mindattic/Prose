# score-characters.ps1
# 1. Waits for the current entity scoring job (non-characters) to finish.
# 2. Runs character scoring (1714 entities, 10 ballots each).
# 3. Runs weapon-ammo linker (ensures every firearm has an ammo edge).
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

# ── Step 1: Character scoring ────────────────────────────────────────────────
Write-Host ""
Write-Host "Step 1 of 2: Character scoring (1714 entities, 10 ballots each)..."
dotnet run --project v3/StreetSamurai.Cli -- `
    --review-entity --type character `
    --ballots 10 --prose 2 --unrated `
    "--local-url" $RunPodUrl `
    "--local-key" $RunPodKey `
    "--local-model" $RunPodModel `
    2>&1 | Tee-Object -FilePath "entity-scoring-characters.log"

Write-Host "Character scoring complete."

# ── Step 2: Weapon → Ammo linker ─────────────────────────────────────────────
Write-Host ""
Write-Host "Step 2 of 2: Linking weapons to ammo types (718 unlinked weapons)..."
dotnet run --project v3/StreetSamurai.Cli -- `
    --link-weapon-ammo `
    "--local-url" $RunPodUrl `
    "--local-key" $RunPodKey `
    "--local-model" $RunPodModel `
    2>&1 | Tee-Object -FilePath "weapon-ammo-link.log"

Write-Host "Weapon-ammo linking complete. Log: weapon-ammo-link.log"
Write-Host ""
Write-Host "All done."
