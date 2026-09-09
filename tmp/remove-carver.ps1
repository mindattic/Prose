$ids=@('01a07e1b-0cd0-7ffe-a55e-32fa8e0c8d9e','01a07e1b-0cd0-7112-957f-23e83216292a','01a07e1b-0cd0-73cd-9306-481219da1064','01a07e1b-0cd0-72b3-a866-c2334bbdba6a','01a07e1b-0cd3-7fec-8e73-7b25a931ed4a')
$sable='<entity repo="character" guid="019d6143-a6c0-7bbd-b014-4496b7f489a7">Sable</entity>'
foreach($id in $ids){
  $raw=(& dotnet run --project v3\Prose.Cli --no-build -- --beat show --id $id --universe glmz | Out-String)
  $lines=$raw -split '?
'
  $u=0
  for($i=0;$i -lt $lines.Count;$i++){if($lines[$i] -match '^UpdatedAt:'){$u=$i;break}}
  if($u -eq 0){continue}
  $txt=($lines[($u+1)..($lines.Count-1)] -join [Environment]::NewLine).Trim()
  $txt=$txt -replace 'Carver-9','Dock Nine'
  $txt=$txt -replace 'Carver''s proof',("the anonymous client's proof, routed through "+$sable)
  $txt=$txt -replace ('proof <entity repo="character" guid="01a06ee1-0649-7ae1-a23d-7697700ced27">Carver</entity> would accept'),("proof the anonymous client would accept through "+$sable)
  $txt=$txt -replace ('<entity repo="character" guid="01a06ee1-0649-7ae1-a23d-7697700ced27">Carver</entity> paid you'),("the anonymous client paid you through "+$sable)
  $txt=$txt -replace ('<entity repo="character" guid="01a06ee1-0649-7ae1-a23d-7697700ced27">Carver</entity>'),('the anonymous client')
  $tmp="tmp\beat-$id.txt"
  Set-Content -LiteralPath $tmp -Value $txt -Encoding UTF8
  Get-Content -LiteralPath $tmp -Raw | dotnet run --project v3\Prose.Cli --no-build -- --beat update --id $id --text - --universe glmz | Out-Host
}
