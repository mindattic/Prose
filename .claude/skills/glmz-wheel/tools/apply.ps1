# Apply one reviewed docket <Work>\gs_c<Cycle>_<Slug>.json to its book, the whole path:
# archive, splice, tag, capture, re-read the changed beats, re-dump the book for the next focus,
# F1-verify every entity tagged in the changed beats. -DryOnly stops after the dry run.
# usage: powershell -File apply.ps1 -Work <dir> -Slug <slug> -Cycle <n> -Order <work order id> [-DryOnly]
param([Parameter(Mandatory)][string]$Work, [Parameter(Mandatory)][string]$Slug, [Parameter(Mandatory)][string]$Cycle,
      [string]$Order = '', [switch]$DryOnly)
[Console]::OutputEncoding = [Text.Encoding]::UTF8
Set-Location D:\Projects\MindAttic\Prose
function P { .\prose.cmd --universe glmz @args 2>&1 }
$dock = "$Work\gs_c${Cycle}_$Slug.json"
if (-not (Test-Path $dock) -or (Get-Content -Raw $dock).Trim() -eq '[]') { "NOCHANGE $Slug"; exit }
$dry = P --splice-beats --node $Slug --file $dock | Select-Object -Last 1
"DRY: $dry"
if ($DryOnly -or "$dry" -notmatch 'all counts match') { exit }
P --archive-book --slug $Slug --reason "GLMZ focus wheel cycle $Cycle, focus $Slug (order $Order)" | Select-Object -Last 1
P --splice-beats --node $Slug --file $dock --apply | Select-Object -Last 1
P --tag-entities --slug $Slug | Select-Object -Last 1
P --factory capture --node $Slug | Select-Object -Last 3
Push-Location $Work; $pos = ((python "$PSScriptRoot\pos.py" $Slug $dock) -split ' ') | ? { $_ } | Sort-Object { [int]$_ } -Unique; Pop-Location
foreach ($p in $pos) { P --read-beats --slug $Slug --from $p --to $p --mark-read --read-by "claude wheel c$Cycle" | Out-Null }
& "$PSScriptRoot\dump.ps1" -Work $Work -Books $Slug | Out-Null
$raw = [IO.File]::ReadAllText("$Work\B_$Slug.txt")
$ids = @(); foreach ($p in $pos) { $m = [regex]::Match($raw, "(?ms)^--- \[$p\] .*?(?=^--- \[|\z)"); $ids += [regex]::Matches($m.Value, 'guid="([0-9a-f-]{36})"') | % { $_.Groups[1].Value } }
& "$PSScriptRoot\f1.ps1" -Slug $Slug -Ids ($ids | Sort-Object -Unique)
P --read-status --slug $Slug | Select-Object -Last 1
P --ruling violations --node $Slug | Select-Object -Last 1
Add-Content "$Work\applied.txt" "gs_c${Cycle}_$Slug.json"
"changed positions: $($pos -join ' ')"
