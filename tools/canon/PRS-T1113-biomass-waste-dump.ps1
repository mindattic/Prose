<#
PRS-T1113: the corponations use the Biomass as an all-purpose industrial waste dump (canon, Ryan 2026-11-22).
Run AFTER PRS-T1111-biomass-origin.ps1 -Apply. Reads both Biomass records live, adds "The Dump" section to the
document and one line to the place, and writes them back through --set-entity-fields WITHOUT --confirm-unread:
if a change would un-read beats, the Hub refuses and writes nothing, and this script stops.
Default is a dry run (patches written, nothing sent); -Apply sends. Prose's Azure SQL is paused until 2026-12-01.
#>
param([switch]$Apply)
$ErrorActionPreference = 'Stop'

$universe = 'glmz'
$repo = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$outDir = Join-Path $PSScriptRoot 'out'
New-Item -ItemType Directory -Force $outDir | Out-Null

$placeId = 'c55d61df-8b58-fc6b-a3b3-6bf72d04321b'
$docId = '3dc96af1-dda7-4c24-93aa-c3cf726d21d5'

$hub = 'http://127.0.0.1:5900'
function Test-Hub { try { (Invoke-WebRequest "$hub/api/health" -UseBasicParsing -TimeoutSec 5).StatusCode -eq 200 } catch { $false } }
if (-not (Test-Hub)) {
    Start-Process -FilePath 'C:\Apps\MindAttic\Prose\Hub.exe' -WorkingDirectory 'C:\Apps\MindAttic\Prose' -WindowStyle Minimized
    $deadline = (Get-Date).AddMinutes(5)
    while (-not (Test-Hub)) { if ((Get-Date) -gt $deadline) { throw 'the Prose Hub did not come up (is the Azure SQL database paused? check the Application event log)' }; Start-Sleep 3 }
}
$key = (Get-Content (Join-Path $env:LOCALAPPDATA 'MindAttic\Prose\Settings.json') -Raw | ConvertFrom-Json).HubApiKey

function Invoke-World([string[]]$Arguments) {
    $req = @{ handlerClass = 'WorldCli'; args = @('--universe', $universe) + $Arguments; universe = $universe; cwd = "$repo" }
    $headers = @{}; if ($key) { $headers['X-Prose-Key'] = $key }
    $body = [Text.Encoding]::UTF8.GetBytes(($req | ConvertTo-Json -Depth 4))
    Invoke-RestMethod -Method Post -Uri "$hub/api/cli-invoke" -Headers $headers -Body $body -ContentType 'application/json; charset=utf-8' -TimeoutSec 600
}

function Get-Fields([string]$Id) {
    $r = Invoke-World @('--get-entity-fields', '--id', $Id)
    if ($r.exitCode -ne 0) { throw "get-entity-fields $Id failed: $($r.error)$($r.output)" }
    [string]$r.output | ConvertFrom-Json
}

$dump = @'
## The Dump

Everyone knows, and nobody says it on the record: the Biomass is the cheapest landfill on the continent.

It takes anything. What it can digest, it eats. What it can't, it closes over and pulls under, and in a season there is nothing on the surface to show it was ever there. The corponations noticed decades ago. Unmarked waste haulers run south out of the GLMZ at night in convoys, articulated rigs and tankers with their transponders dark, to dumping sites along the border where the old Kentucky roads run straight into the flesh. They back up to the gradient, open up, and tip: chemical sludge, spent solvents, industrial slag, drums with the labels ground off, e-waste by the trailer-load, failed automata, whole decommissioned machines. Sometimes bodies people don't want found. The tissue closes over all of it within days. There is no paperwork, because officially no one goes south.

The locals do it too. Anyone who lives within an hour of the border has thrown something in: an old couch, a dead appliance, a car nobody wanted to pay to scrap. They drive down to the edge and throw it in just to stand there and watch the flesh take it. It is a thing to do on a Saturday night. Kids dare each other to walk closer.

It is an open secret, and maybe an expensive one. The monitoring stations that logged the growth surges after CAUTERIZE sit downslope of the busiest dumping sites more often than chance explains. Some of the people who read those numbers think the Biomass is not just tolerating what the corpos feed it. They think it is growing on it.

'@

$placeLine = 'An open secret: the corponations use it as an all-purpose industrial waste dump; unmarked haulers tip chemical sludge, industrial slag, e-waste and the occasional body at dumping sites along the Kentucky border, locals within an hour of it throw things in to watch the flesh take them, and the dumping may be feeding its growth surges.'

function Add-Dump([string]$Text) {
    if ($Text -match '(?m)^#+\s*The Dump') { return $Text }
    $m = [regex]::Match($Text, '(?m)^#+\s*Why No One Has Destroyed It\s*$')
    if (-not $m.Success) { throw 'no "Why No One Has Destroyed It" heading to splice The Dump in front of; edit by hand' }
    $level = ([regex]::Match($m.Value, '^#+')).Value
    $section = $dump -replace '(?m)^## ', "$level "
    $Text.Substring(0, $m.Index) + $section + $Text.Substring($m.Index)
}

function Edit-Tags($Tags, [string[]]$Add) {
    $list = @($Tags | Where-Object { $_ })
    foreach ($t in $Add) { if ($list -notcontains $t) { $list += $t } }
    , $list
}

# ---- the place ----------------------------------------------------------------------------------------------------
$place = Get-Fields $placeId
$summary = [string]$place.summary
if ($summary -notmatch 'waste dump') { $summary = ($summary.TrimEnd() + ' ' + $placeLine).Trim() }
$placePatch = [ordered]@{ summary = $summary }
if ($place.record.PSObject.Properties.Name -contains 'description') {
    $desc = [string]$place.record.description
    if ($desc -notmatch 'waste dump') { $desc = ($desc.TrimEnd() + ' ' + $placeLine).Trim() }
    $placePatch.description = $desc
}
$placePatch.tags = Edit-Tags $place.record.tags @('waste-dump', 'open-secret')

# ---- the document -------------------------------------------------------------------------------------------------
$doc = Get-Fields $docId
$docPatch = [ordered]@{}
foreach ($field in 'body', 'description') {
    if ($doc.record.PSObject.Properties.Name -contains $field -and $doc.record.$field) {
        $docPatch[$field] = Add-Dump ([string]$doc.record.$field)
    }
}
if ($docPatch.Count -eq 0) { throw "document $docId has neither body nor description; edit by hand" }
$docPatch.tags = Edit-Tags $doc.record.tags @('waste-dump', 'open-secret', 'corponations')

$utf8 = New-Object Text.UTF8Encoding($false)
$placeFile = Join-Path $outDir 'biomass-place.dump.patch.json'
$docFile = Join-Path $outDir 'biomass-document.dump.patch.json'
[IO.File]::WriteAllText($placeFile, ($placePatch | ConvertTo-Json -Depth 6), $utf8)
[IO.File]::WriteAllText($docFile, ($docPatch | ConvertTo-Json -Depth 6), $utf8)
Write-Host "patches: $placeFile, $docFile"
if (-not $Apply) { Write-Host 'dry run: nothing sent. Re-run with -Apply.'; return }

foreach ($p in @(@{ Id = $placeId; File = $placeFile }, @{ Id = $docId; File = $docFile })) {
    $r = Invoke-World @('--set-entity-fields', '--id', $p.Id, '--file', $p.File)
    if ($r.exitCode -ne 0) {
        Write-Host "REFUSED $($p.Id): $($r.error)$($r.output)"
        Write-Host 'If it un-reads beats, that is Ryan''s call (never --confirm-unread unattended). Stopping.'
        exit 2
    }
    Write-Host "wrote $($p.Id): $($r.output)"
}

$placeBack = Get-Fields $placeId
$docBack = Get-Fields $docId
if ($placeBack.summary -notmatch 'waste dump') { throw 'place waste-dump line did not read back' }
if (-not (($docBack.record.body, $docBack.record.description) -match 'The Dump')) { throw 'document Dump section did not read back' }
Write-Host 'read back OK. Next: tools\wiki\sync-wiki.ps1 (or wait for the 5-min watch), then check both pages on https://mindattic.com/glmz/.'
