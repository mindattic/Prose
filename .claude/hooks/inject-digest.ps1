<#
  SessionStart hook - injects docs/BIBLE.digest.md as authoritative context.
  Emits Claude Code hook JSON on stdout. PowerShell 5.1 / Win-1252 safe:
  every non-ASCII char is escaped to \uXXXX so the JSON is pure ASCII.
  If the digest is missing/empty, emits {}.
#>
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$digest   = Join-Path $repoRoot 'docs\BIBLE.digest.md'

if (-not (Test-Path $digest)) { Write-Output '{}'; exit 0 }
$body = Get-Content -LiteralPath $digest -Raw -Encoding UTF8
if ([string]::IsNullOrWhiteSpace($body)) { Write-Output '{}'; exit 0 }

$preamble = @'
The following StreetSamurai Codex digest is the AUTHORITATIVE source of truth for what this
project IS, is NOT, and the laws that keep it coherent (engine invariants + Bushido Coda narrative
continuity). Treat it as binding. Full detail lives in docs/BIBLE.md; the append-only
docs/AMENDMENTS.md wins on conflict; stories + status live in docs/USER_STORIES.md. Do not violate
the Laws. Canon is the SQL database, not files.

'@

$text = $preamble + $body

# JSON-escape to pure ASCII (manual; no ConvertTo-Json dependency on non-ASCII handling)
$sb = New-Object System.Text.StringBuilder
foreach ($ch in $text.ToCharArray()) {
  $code = [int][char]$ch
  switch ($ch) {
    '"'  { [void]$sb.Append('\"') }
    '\'  { [void]$sb.Append('\\') }
    "`b" { [void]$sb.Append('\b') }
    "`f" { [void]$sb.Append('\f') }
    "`n" { [void]$sb.Append('\n') }
    "`r" { [void]$sb.Append('\r') }
    "`t" { [void]$sb.Append('\t') }
    default {
      if ($code -lt 32 -or $code -gt 126) {
        [void]$sb.Append('\u' + $code.ToString('x4'))
      } else {
        [void]$sb.Append($ch)
      }
    }
  }
}
$escaped = $sb.ToString()

$json = '{"hookSpecificOutput":{"hookEventName":"SessionStart","additionalContext":"' + $escaped + '"}}'
Write-Output $json
exit 0
