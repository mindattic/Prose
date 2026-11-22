<#
PRS-T1111: the Biomass fell from space (canon decision, Ryan 2026-11-22).
Reads both Biomass records live from the Hub, splices in the hidden true origin, and writes them back
through --set-entity-fields WITHOUT --confirm-unread: if a change would un-read beats, the Hub refuses
and writes nothing, and this script stops. Default is a dry run (patches written, nothing sent); -Apply sends.
Prose's Azure SQL free tier paused for November 2026; run this on or after 2026-12-01.
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

$trueOrigin = @'
## What Fell

*True origin: hidden in-world. No one in the GLMZ knows this; the lab-leak account above is the cover story.*

Pinnacle BioSynthetics never invented ZD-7741. In 2147 something came down out of the sky over northern Alabama, and enough of it survived the fall to be worth recovering. Pinnacle got to it first, quietly, and carried what was left into a secret laboratory buried beneath its Huntsville campus. ZD-7741 was the designation it gave the recovered material in its own records; the regeneration program was what Pinnacle was trying to make the material do.

It got loose underground. The waste line, the drainage, the groundwater of the Tennessee River Valley: all of that happened, only not to anything human hands had made. The official account kept every true detail except the one that mattered, which is where it came from. It has been eating, consuming and growing ever since.

Out past the Earth, crews have a name for it already. Bio Mass: the drifting flesh every ship fires on at sight. Whatever fell on Huntsville came from out there.

'@

function Add-TrueOrigin([string]$Text) {
    if ($Text -match '(?m)^#+\s*What Fell') { return $Text }
    $m = [regex]::Match($Text, '(?m)^#+\s*Consumption\s*$')
    if (-not $m.Success) { throw 'no "Consumption" heading to splice the true origin in front of; edit by hand' }
    $level = ([regex]::Match($m.Value, '^#+')).Value
    $section = $trueOrigin -replace '(?m)^## ', "$level "
    $Text.Substring(0, $m.Index) + $section + $Text.Substring($m.Index)
}

function Edit-Tags($Tags, [string[]]$Remove, [string[]]$Add) {
    $list = @($Tags | Where-Object { $_ -and ($Remove -notcontains $_) })
    foreach ($t in $Add) { if ($list -notcontains $t) { $list += $t } }
    , $list
}

$placeSummary = 'The Biomass, a living mass of human-derived flesh that has consumed most of the former southeastern United States around Huntsville, advancing northward and consuming human infrastructure in its path. Officially the product of a 2147 lab leak at Pinnacle BioSynthetics; in truth (hidden in-world) it fell from space, was secretly recovered by Pinnacle, and got loose from its underground Huntsville lab.'

# ---- the place ----------------------------------------------------------------------------------------------------
$place = Get-Fields $placeId
$placePatch = [ordered]@{ summary = $placeSummary }
if ($place.record.PSObject.Properties.Name -contains 'description') { $placePatch.description = $placeSummary }
$placePatch.tags = Edit-Tags $place.record.tags @('engineered-vegetation') @('flesh', 'extraterrestrial', 'cover-up', 'pinnacle-biosynthetics')

# ---- the document -------------------------------------------------------------------------------------------------
$doc = Get-Fields $docId
$docPatch = [ordered]@{}
foreach ($field in 'body', 'description') {
    if ($doc.record.PSObject.Properties.Name -contains $field -and $doc.record.$field) {
        $docPatch[$field] = Add-TrueOrigin ([string]$doc.record.$field)
    }
}
if ($docPatch.Count -eq 0) { throw "document $docId has neither body nor description; edit by hand" }
$docPatch.tags = Edit-Tags $doc.record.tags @('bioweapon') @('extraterrestrial', 'cover-up', 'pinnacle-biosynthetics', 'huntsville')

$utf8 = New-Object Text.UTF8Encoding($false)
$placeFile = Join-Path $outDir 'biomass-place.patch.json'
$docFile = Join-Path $outDir 'biomass-document.patch.json'
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
if ($placeBack.summary -ne $placeSummary) { throw 'place summary did not read back' }
if (-not (($docBack.record.body, $docBack.record.description) -match 'What Fell')) { throw 'document true origin did not read back' }
Write-Host 'read back OK. Next: tools\wiki\sync-wiki.ps1 (or wait for the 5-min watch), then check both pages on https://mindattic.com/glmz/.'
