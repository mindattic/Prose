# Dump books to <Work>\B_<slug>.txt (tagged) and B_<slug>_clean.txt (tags stripped), UTF-8.
# usage: powershell -File dump.ps1 -Work <dir> [-Books atte,blst,...]   (default: every slug in <Work>\books.txt)
param([Parameter(Mandatory)][string]$Work, [string[]]$Books)
[Console]::OutputEncoding = [Text.Encoding]::UTF8   # else em dashes arrive as "ΓÇö" (cycle 1 lesson)
$OutputEncoding = [Text.Encoding]::UTF8
if (-not $Books) { $Books = Get-Content "$Work\books.txt" | ? { $_.Trim() } | % { ($_ -split '\s+')[0] } }
Set-Location D:\Projects\MindAttic\Prose
foreach ($s in $Books) {
  $raw = (.\prose.cmd --universe glmz --read-beats --slug $s --from 1 --to 4000 2>&1 | Out-String)
  [IO.File]::WriteAllText("$Work\B_$s.txt", $raw, [Text.UTF8Encoding]::new($false))
  [IO.File]::WriteAllText("$Work\B_${s}_clean.txt", ($raw -replace '<entity [^>]*>','' -replace '</entity>',''), [Text.UTF8Encoding]::new($false))
  if ($raw -match 'ΓÇ') { "WARNING $s dump is mojibake: fix the console encoding before using it" }
  "$s beats=" + ([regex]::Matches($raw,'(?m)^--- \[').Count)
}
