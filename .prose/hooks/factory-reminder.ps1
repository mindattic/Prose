# UserPromptSubmit: one line - the factory's current next action and the forbidden list
# (RFC 0015 section 6). Cached for 60 seconds so a burst of prompts costs one Hub call.
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.Encoding]::UTF8
$cache = Join-Path $env:TEMP 'prose-factory-line.txt'
try {
    if ((Test-Path $cache) -and ((Get-Date) - (Get-Item $cache).LastWriteTime).TotalSeconds -lt 60) {
        Write-Output (Get-Content $cache -Raw)
        exit 0
    }
    $line = Invoke-RestMethod -Uri 'http://127.0.0.1:5900/api/factory/next?format=line' -TimeoutSec 4
    Set-Content -Path $cache -Value $line -Encoding utf8
    Write-Output $line
} catch {
    Write-Output 'FACTORY unreachable (Hub down?) - do no story work from memory.'
}
exit 0
