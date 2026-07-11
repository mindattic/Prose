# score-olive-blackbird.ps1
# Runs entity scoring for the large late-sequence types on Olive Blackbird (L40S).
# Run while Magenta Opossum (A100) continues the main job — both use --unrated so
# there are no double-scores; the first pod to finish an entity wins, the other skips.
#
# Olive Blackbird pod: ios3aii3ubt1po (L40S x1, $0.99/hr)
# Target types (large, late in the default sequence — A100 won't reach for hours):
#   document(2067), place(696), vocabulary(692), archetype(574), quote(587)  = 4616 entities
#
# Usage: pwsh tools/score-olive-blackbird.ps1

$OliveUrl   = "https://ios3aii3ubt1po-11434.proxy.runpod.net"
$OliveKey   = "vllm_key_aJaFlrQrTzfyhVPC6dWoqDonVaQAxd060oYwo81f"
$OliveModel = "qwen2.5:32b-instruct"

# Large types that appear late in the default knownTypes sequence.
# The A100 works through: cyberware→genemod→transportation→automaton→subsidiary→
# entertainment→apparel→material→pharmaceutical→consumer_good→faction→place→
# contract→document→motif→vocabulary→news→archetype→quote
# We front-load Olive Blackbird on the biggest of those so it's working in parallel.
$Types = @("document", "place", "vocabulary", "archetype", "quote", "news", "subsidiary", "entertainment", "apparel")

Write-Host "=== Olive Blackbird (L40S) entity scoring ==="
Write-Host "URL   : $OliveUrl"
Write-Host "Model : $OliveModel"
Write-Host "Types : $($Types -join ', ')"
Write-Host ""

$LogFile = "entity-scoring-olive.log"
$StartTime = Get-Date

foreach ($Type in $Types) {
    Write-Host "[$([datetime]::UtcNow.ToString('HH:mm:ss'))] Starting type: $Type"
    dotnet "D:\Temp\olive-build\StreetSamurai.Cli.dll" `
        --review-entity `
        --type $Type `
        --ballots 10 --prose 2 --unrated `
        "--local-url" $OliveUrl `
        "--local-key" $OliveKey `
        "--local-model" $OliveModel `
        2>&1 | Tee-Object -FilePath $LogFile -Append
    Write-Host "[$([datetime]::UtcNow.ToString('HH:mm:ss'))] Finished type: $Type"
    Write-Host ""
}

$Elapsed = [datetime]::UtcNow - $StartTime
Write-Host "=== Olive Blackbird done. Elapsed: $($Elapsed.ToString('hh\:mm\:ss')) ==="
Write-Host "Log: $LogFile"
