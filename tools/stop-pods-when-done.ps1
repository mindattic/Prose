# stop-pods-when-done.ps1
# Watches the two entity-scoring jobs. When both exit AND the DB confirms zero
# unrated non-character entities, stops both RunPod pods via the API.
#
# Usage:
#   $env:RUNPOD_API_KEY = "your_key_here"
#   pwsh tools/stop-pods-when-done.ps1

param(
    [int]$A100Pid    = 142276,   # Magenta Opossum scoring job
    [int]$OlivePid   = 4612,     # Olive Blackbird scoring job
    [string]$A100Pod  = "tqxz1z99hvtvcq",
    [string]$OlivePod = "ios3aii3ubt1po",
    [int]$PollSeconds = 60
)

$ApiKey = $env:RUNPOD_API_KEY
if (-not $ApiKey) {
    Write-Host "ERROR: Set `$env:RUNPOD_API_KEY before running." -ForegroundColor Red
    exit 1
}

$Db  = "(localdb)\MSSQLLocalDB"
$DbN = "StreetSamurai"

function Get-UnratedCount {
    $q = @"
SELECT COUNT(*) FROM Entities e
WHERE e.EntityType NOT IN ('character','person','organization')
  AND e.IsActive = 1
  AND NOT EXISTS (
      SELECT 1 FROM EntityReviewSummaries s
      WHERE s.EntityId = CAST(e.Id AS nvarchar(50))
  )
"@
    $raw = sqlcmd -S $Db -d $DbN -Q $q -h -1 2>$null
    $line = ($raw | Where-Object { $_ -match '^\s*\d+' } | Select-Object -First 1)
    return [int]($line.Trim())
}

function Stop-Pod([string]$podId) {
    $body = @{ query = "mutation { podStop(input: { podId: `"$podId`" }) { id } }" } | ConvertTo-Json
    $resp = Invoke-RestMethod `
        -Uri "https://api.runpod.io/graphql?api_key=$ApiKey" `
        -Method POST `
        -ContentType "application/json" `
        -Body $body
    return $resp
}

Write-Host "=== Pod watcher started ==="
Write-Host "  A100  PID $A100Pid  → pod $A100Pod"
Write-Host "  Olive PID $OlivePid → pod $OlivePod"
Write-Host "  Polling every ${PollSeconds}s"
Write-Host ""

while ($true) {
    $a100Running  = [bool](Get-Process -Id $A100Pid  -ErrorAction SilentlyContinue)
    $oliveRunning = [bool](Get-Process -Id $OlivePid -ErrorAction SilentlyContinue)
    $unrated      = Get-UnratedCount

    $ts = [datetime]::Now.ToString("HH:mm:ss")
    Write-Host "[$ts] A100=$(if($a100Running){'running'}else{'DONE'})  Olive=$(if($oliveRunning){'running'}else{'DONE'})  Unrated=$unrated"

    if (-not $a100Running -and -not $oliveRunning -and $unrated -eq 0) {
        Write-Host ""
        Write-Host "Both jobs done, zero unrated entities. Stopping pods..." -ForegroundColor Green

        Write-Host "  Stopping $A100Pod (Magenta Opossum)..."
        $r1 = Stop-Pod $A100Pod
        Write-Host "  Response: $($r1 | ConvertTo-Json -Compress)"

        Write-Host "  Stopping $OlivePod (Olive Blackbird)..."
        $r2 = Stop-Pod $OlivePod
        Write-Host "  Response: $($r2 | ConvertTo-Json -Compress)"

        Write-Host ""
        Write-Host "Pods stopped. Billing ended." -ForegroundColor Green
        break
    }

    if (-not $a100Running -and -not $oliveRunning -and $unrated -gt 0) {
        Write-Host "  Both jobs exited but $unrated unrated entities remain — jobs may have errored." -ForegroundColor Yellow
        Write-Host "  NOT stopping pods. Investigate and restart if needed."
        break
    }

    Start-Sleep -Seconds $PollSeconds
}
