# F1-verify entities in one book. -Ids limits it; -All verifies every entity the book tags (slow).
# Run in the FOREGROUND: Claude Code reaps long background shells under memory pressure (cycle 1).
# usage: powershell -File f1.ps1 -Slug <slug> [-Ids <guid,...>] [-All]
param([Parameter(Mandatory)][string]$Slug, [string[]]$Ids, [switch]$All)
[Console]::OutputEncoding = [Text.Encoding]::UTF8
Set-Location D:\Projects\MindAttic\Prose
function P { .\prose.cmd --universe glmz @args 2>&1 }
if ($All) { $raw = P --read-beats --slug $Slug --from 1 --to 4000 | Out-String; $Ids = [regex]::Matches($raw,'guid="([0-9a-f-]{36})"') | % { $_.Groups[1].Value } | Sort-Object -Unique }
$bad = 0
foreach ($g in $Ids) {
  $o = P --verify-entity begin --entity $g --node $Slug | Out-String
  try { $j = ($o -split '\[verify\]')[0] | ConvertFrom-Json } catch { "F1 ERR $g"; $bad++; continue }
  $r = P --verify-entity commit --nonce $j.Nonce | Out-String
  if ($r -notmatch '(?i)verified') { "F1 FAIL $($j.Name): $($r.Trim())"; $bad++ }
}
"F1: $(@($Ids).Count) entities, $bad failed"
