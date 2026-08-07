# gpu-health-monitor.ps1
# 60-second heartbeat for each RunPod vLLM pod.
# Detects dead vLLM servers and idle scoring jobs; self-heals both.
# Finds scoring jobs by command-line URL match — no fragile PID files.
#
# Usage: pwsh tools/gpu-health-monitor.ps1

$PollSeconds = 60
$VllmKey     = $env:VLLM_KEY   # set in environment; never hardcode here
$WorkDir     = "D:\Projects\MindAttic\Prose"
$LogFile     = "$WorkDir\gpu-health-monitor.log"
$DbServer    = "(localdb)\MSSQLLocalDB"
$DbName      = "Prose"

$Pods = @(
    [PSCustomObject]@{
        Name          = "Magenta Opossum"
        PodId         = "tqxz1z99hvtvcq"
        Url           = "https://tqxz1z99hvtvcq-8000.proxy.runpod.net"
        Script        = "$WorkDir\tools\score-magenta-opossum.ps1"
        RequiredModel = "Qwen3-32B"      # must appear in the model id — hard block if mismatch
    }
)

function Log([string]$msg) {
    $line = "[$(Get-Date -Format 'HH:mm:ss')] $msg"
    Write-Host $line
    Add-Content $LogFile $line
}

function Get-RunPodKey {
    (Get-Content "$env:APPDATA\MindAttic\LLM\runpod.json" | ConvertFrom-Json).apiKey
}

function Test-Vllm([string]$url) {
    try {
        $null = Invoke-RestMethod "$url/v1/models" `
            -Headers @{ Authorization = "Bearer $VllmKey" } -TimeoutSec 10 -ErrorAction Stop
        return $true
    } catch { return $false }
}

# Returns the first model id from the endpoint, or $null if unreachable
function Get-VllmModel([string]$url) {
    try {
        $r = Invoke-RestMethod "$url/v1/models" `
            -Headers @{ Authorization = "Bearer $VllmKey" } -TimeoutSec 10 -ErrorAction Stop
        return $r.data[0].id
    } catch { return $null }
}

# Hard block: verify model matches RequiredModel before ever launching a job
function Confirm-Model($pod) {
    $actual = Get-VllmModel $pod.Url
    if (-not $actual) { return $true }  # offline — let normal health flow handle it
    if ($actual -notlike "*$($pod.RequiredModel)*") {
        Log "MODEL MISMATCH on $($pod.Name): expected *$($pod.RequiredModel)* but got '$actual' — NOT launching scoring job. Shut this pod down."
        return $false
    }
    return $true
}

function Get-PodDesiredStatus([string]$podId) {
    $key = Get-RunPodKey
    $q = "{`"query`":`"{ pod(input:{podId:\`"$podId\`"}){desiredStatus} }`"}"
    try {
        $r = Invoke-RestMethod "https://api.runpod.io/graphql?api_key=$key" `
            -Method POST -ContentType "application/json" -Body $q -ErrorAction Stop
        return $r.data.pod.desiredStatus
    } catch { return "UNKNOWN" }
}

function Invoke-PodCycle([string]$podId, [string]$name) {
    $key  = Get-RunPodKey
    $stop = "{`"query`":`"mutation{podStop(input:{podId:\`"$podId\`"}){id}}`"}"
    $go   = "{`"query`":`"mutation{podResume(input:{podId:\`"$podId\`",gpuCount:1}){id desiredStatus}}`"}"
    Log "  $name — stopping pod..."
    Invoke-RestMethod "https://api.runpod.io/graphql?api_key=$key" -Method POST -ContentType "application/json" -Body $stop | Out-Null
    Start-Sleep 8
    Log "  $name — resuming pod..."
    Invoke-RestMethod "https://api.runpod.io/graphql?api_key=$key" -Method POST -ContentType "application/json" -Body $go | Out-Null
}

function Invoke-PodResume([string]$podId, [string]$name) {
    $key = Get-RunPodKey
    $go  = "{`"query`":`"mutation{podResume(input:{podId:\`"$podId\`",gpuCount:1}){id desiredStatus}}`"}"
    Log "  $name — resuming pod..."
    Invoke-RestMethod "https://api.runpod.io/graphql?api_key=$key" -Method POST -ContentType "application/json" -Body $go | Out-Null
}

function Wait-Vllm([string]$url, [string]$name, [int]$maxMin = 12) {
    for ($i = 1; $i -le ($maxMin * 4); $i++) {
        Start-Sleep 15
        if (Test-Vllm $url) { Log "  $name — vLLM UP"; return $true }
        if ($i % 4 -eq 0) { Log "  $name — still loading... ($([int]($i/4))m)" }
    }
    Log "  $name — vLLM failed to come up after ${maxMin}m"
    return $false
}

# Find the dotnet process scoring for a specific pod URL — no PID files needed
function Get-ScoringProcess([string]$podUrl) {
    $fragment = $podUrl.Split("//")[1].Split("-8000")[0]  # e.g. "tqxz1z99hvtvcq"
    Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -like "*$fragment*" -and $_.CommandLine -like "*review-entity*" } |
        Select-Object -First 1
}

function Start-ScoringJob($pod) {
    if (-not (Test-Path $pod.Script)) { Log "  $($pod.Name) — script missing: $($pod.Script)"; return }
    if (-not (Confirm-Model $pod)) { return }   # hard block on model mismatch
    $proc = Start-Process powershell `
        -ArgumentList "-NoProfile", "-File", $pod.Script `
        -WorkingDirectory $WorkDir -WindowStyle Normal -PassThru
    Log "  $($pod.Name) — scoring job launched PID $($proc.Id)"
}

function Get-UnratedCount {
    $q = "SELECT COUNT(*) FROM Entities e WHERE e.EntityType NOT IN ('character','person','organization') AND e.IsActive=1 AND NOT EXISTS (SELECT 1 FROM EntityReviewSummaries s WHERE s.EntityId=CAST(e.Id AS nvarchar(50)))"
    $raw = sqlcmd -S $DbServer -d $DbName -Q $q -h -1 2>$null
    $line = $raw | Where-Object { $_ -match '^\s*\d+' } | Select-Object -First 1
    return if ($line) { [int]$line.Trim() } else { -1 }
}

# ── Main loop ────────────────────────────────────────────────────────────────
Log "=== GPU Health Monitor started — ${PollSeconds}s heartbeat ==="
$Pods | ForEach-Object { Log "  $($_.Name)  $($_.PodId)  $($_.Url)" }

# Track consecutive vLLM failures per pod to avoid restart storms
$failCounts = @{}
$Pods | ForEach-Object { $failCounts[$_.PodId] = 0 }

while ($true) {
    $unrated = Get-UnratedCount
    if ($unrated -eq 0) { Log "All entities scored — monitor exiting."; break }

    $ts = Get-Date -Format "HH:mm:ss"
    $statusParts = @()

    foreach ($pod in $Pods) {
        $vllmOk = Test-Vllm $pod.Url
        $scorer  = Get-ScoringProcess $pod.Url
        $jobStatus = if ($scorer) { "job:PID $($scorer.ProcessId)" } else { "job:NONE" }

        if ($vllmOk) {
            $failCounts[$pod.PodId] = 0
            $statusParts += "$($pod.Name): OK  $jobStatus"

            # vLLM is up but scoring job isn't running — relaunch
            if (-not $scorer -and $unrated -gt 0) {
                Log "$($pod.Name): vLLM healthy but no scoring job — relaunching"
                Start-ScoringJob $pod
            }
        } else {
            $failCounts[$pod.PodId]++
            $fails = $failCounts[$pod.PodId]
            $statusParts += "$($pod.Name): DOWN (fail#$fails)  $jobStatus"

            # Only act after 2 consecutive failures to avoid false positives on transient blips
            if ($fails -ge 2) {
                Log "$($pod.Name): vLLM DOWN $fails consecutive — recovering..."
                $podStatus = Get-PodDesiredStatus $pod.PodId
                if ($podStatus -eq "EXITED") {
                    Invoke-PodResume $pod.PodId $pod.Name
                } else {
                    Invoke-PodCycle $pod.PodId $pod.Name
                }
                $failCounts[$pod.PodId] = 0
                if (Wait-Vllm $pod.Url $pod.Name) {
                    Start-ScoringJob $pod
                }
            }
        }
    }

    Log "$ts | Unrated:$unrated | $($statusParts -join ' | ')"
    Start-Sleep $PollSeconds
}

Log "=== GPU Health Monitor done ==="
