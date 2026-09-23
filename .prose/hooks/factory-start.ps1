# SessionStart: open a factory session and inject the factory's live next action (RFC 0015 section 6).
# The plan is not a document: it is computed by the Hub from the book's state. If the Hub is
# unreachable, say so plainly and tell the session not to work from memory. Never blocks start.
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.Encoding]::UTF8
$hub = 'http://127.0.0.1:5900'
$sessionId = ''
try { $raw = [Console]::In.ReadToEnd(); if ($raw) { $sessionId = ($raw | ConvertFrom-Json).session_id } } catch { }

$head = ''
try { $head = (& git -C 'D:\Projects\MindAttic\Prose' rev-parse HEAD 2>$null) } catch { }

$ready = $false
for ($i = 0; $i -lt 8 -and -not $ready; $i++) {
    try { $h = Invoke-RestMethod -Uri "$hub/api/health" -TimeoutSec 2; $ready = ($h.status -eq 'ok') } catch { Start-Sleep -Milliseconds 750 }
}
if (-not $ready) {
    Write-Output "FACTORY UNREACHABLE - the Prose Hub is not answering at $hub."
    Write-Output "Start it: powershell -NoProfile -File D:\Projects\MindAttic\Prose\src\tools\deploy-apps.ps1 -Start Hub"
    Write-Output "Do no story work from memory until the factory answers."
    exit 0
}

try {
    $body = @{ claudeSessionId = $sessionId; gitHead = "$head".Trim() } | ConvertTo-Json -Compress
    $block = Invoke-RestMethod -Method Post -Uri "$hub/api/factory/session/start" -Body $body -ContentType 'application/json' -TimeoutSec 20
    Write-Output $block
} catch {
    Write-Output "FACTORY ERROR - $($_.Exception.Message). Run: prose --factory next"
}
exit 0
