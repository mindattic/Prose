<#
PRS-T1117: GLMZ transport canon gaps (Ryan 2026-11-22). Creates the Ferrogate Aero FA-40 heavy lift (the Sow,
the Biomass waste hauler) and fills the Ferrogate Aero FA-9's empty record with its shift-lift role.
FA-9 goes through --set-entity-fields WITHOUT --confirm-unread: if it would un-read beats the Hub refuses and this
stops. Default is a dry run (records written to out\, nothing sent); -Apply sends.
Prose's Azure SQL free tier paused for November 2026; run this on or after 2026-12-01.
#>
param([switch]$Apply)
$ErrorActionPreference = 'Stop'

$universe = 'glmz'
$repo = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$outDir = Join-Path $PSScriptRoot 'out'
New-Item -ItemType Directory -Force $outDir | Out-Null

$fa9Id = '019f5284-b79f-7f4c-b756-bce71986a848'
$fa40Name = 'Ferrogate Aero FA-40 heavy lift'
$fa40Marker = Join-Path $outDir 'fa40.created-id.txt'

$fa40 = [ordered]@{
    aliases           = @('the Sow', 'FA-40', 'gut-runner')
    category          = 'commercial_air'
    manufacturer      = 'Ferrogate Aero'
    description       = "The waste hauler. A flat-bellied slab of an airframe on four ducted lift fans, a hydrogen turbine for cruise, a clamshell belly that opens over the drop and a release rig for slung waste cans. Crews call it the Sow. It is loud the way a building is loud: a deep four-beat drone you feel in a window before you hear it.`n`nThe FA-40 flies the gut run: lift at night from the industrial pads, transponders off past the Ohio, south over Kentucky to the dumping sites on the Biomass border, hold a few hundred meters up out of the haze, open the belly, and turn for home lighter. Corpo contracts are flown by Ferrogate Aero crews and by subcontractors under Carrion Logistics' reclamation licences; the gray-zone end of the trade is reclaimed FA-40s and improvised crane rigs flying the same route for cash and no questions. From the southern districts the Sows read as a line of running lights droning south under the Thread, and the same line coming back an hour later, higher. Gut-run pilots are well paid, never talk about it, and keep the windows sealed; everyone knows what the smell on a returning airframe means."
    propulsion        = 'Four ducted lift fans, hydrogen turbine cruise'
    speed             = '220 km/h cruise loaded'
    capacity          = '40 t slung or internal'
    range             = '1,000 km (the gut run and back)'
    tier_availability = 'Commercial heavy-lift license; gray-zone airframes unlicensed'
    cost              = 'Φ3,800,000 new; reclaimed airframes change hands for a fraction, no paperwork'
    autonomy          = 'Two-pilot crew; autopilot for the transit legs, hand-flown over the drop'
    armament          = 'None. Some gray-zone rigs carry flares for corpo patrols that pretend to object.'
    common_usage      = 'Industrial waste disposal into the Biomass (the gut run); heavy construction lift on the side.'
    story_hooks       = @(
        'A gut-run crew opens the belly over the drop site and something on the Biomass surface opens back.',
        'A pilot is paid triple to fly one sealed can south and never told what is in it.',
        'A reclaimed Sow comes back from the gut run with a stowaway nobody loaded.'
    )
    tags              = @('transportation', 'aircraft', 'commercialair', 'cargo', 'biomass', 'waste', 'gut-run', 'night', 'corporate', 'gray-zone', 'ferrogate')
}
$fa40Summary = 'Ferrogate Aero FA-40 heavy lift (the Sow): the heavy VTOL waste hauler that flies the gut run south at night and dumps industrial waste into the Biomass.'

$fa9 = [ordered]@{
    summary           = 'Ferrogate Aero FA-9: the transit bus of the sky; at shift change the shift-lift, chartered by corponations to move wage workers.'
    aliases           = @('the FA-9', 'the shift-lift')
    category          = 'public_transit'
    manufacturer      = 'Ferrogate Aero'
    description       = "The transit bus of the sky: boxy, loud, gray-white, reliable, always late and always full. Contractors fly it empty and call that a perk. At shift change it is the shift-lift: corponations charter FA-9s by the block to move wage workers between the residential stacks and the plants. Standing room, strap-hung, sixty bodies where the plate says forty, the fare docked from pay before it is earned. It flies the low lanes under the Sparrows and the corporate shuttles, and the SkyLux traffic far above it never has to look down."
    propulsion        = 'Tilt-rotor VTOL, hydrogen turbine'
    capacity          = '40 seated (rated); 60 at shift change'
    tier_availability = 'Tier 1-3 (chartered shift-lifts); contractors'
    autonomy          = 'Autonomous on fixed routes, pilot aboard by regulation'
    common_usage      = 'Shift-change commuting for wage workers, contractor transport, the cheap way up.'
    tags              = @('transportation', 'aircraft', 'publictransit', 'vtol', 'commute', 'wage-workers', 'ferrogate', 'tier 1', 'tier 3')
}

$hub = 'http://127.0.0.1:5900'
function Test-Hub { try { (Invoke-WebRequest "$hub/api/health" -UseBasicParsing -TimeoutSec 5).StatusCode -eq 200 } catch { $false } }

$utf8 = New-Object Text.UTF8Encoding($false)
$fa40File = Join-Path $outDir 'fa40.record.json'
$fa9File = Join-Path $outDir 'fa9.patch.json'
[IO.File]::WriteAllText($fa40File, ($fa40 | ConvertTo-Json -Depth 6), $utf8)
[IO.File]::WriteAllText($fa9File, ($fa9 | ConvertTo-Json -Depth 6), $utf8)
Write-Host "records: $fa40File, $fa9File"
if (-not $Apply) { Write-Host 'dry run: nothing sent. Re-run with -Apply.'; return }

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

if (Test-Path $fa40Marker) {
    Write-Host "FA-40 already created ($(Get-Content $fa40Marker)); skipping create."
} else {
    $r = Invoke-World @('--create-entity', '--type', 'transportation', '--name', $fa40Name, '--summary', $fa40Summary, '--file', $fa40File)
    if ($r.exitCode -ne 0) { throw "create-entity FA-40 failed: $($r.error)$($r.output)" }
    $id = [regex]::Match([string]$r.output, '[0-9a-f]{8}-?[0-9a-f]{4}-?[0-9a-f]{4}-?[0-9a-f]{4}-?[0-9a-f]{12}').Value
    [IO.File]::WriteAllText($fa40Marker, $id, $utf8)
    Write-Host "created FA-40: $id ($($r.output))"
}

$r = Invoke-World @('--set-entity-fields', '--id', $fa9Id, '--file', $fa9File)
if ($r.exitCode -ne 0) {
    Write-Host "REFUSED FA-9: $($r.error)$($r.output)"
    Write-Host 'If it un-reads beats, that is Ryan''s call (never --confirm-unread unattended). Stopping.'
    exit 2
}
Write-Host "wrote FA-9: $($r.output)"
Write-Host 'Next: tools\wiki\sync-wiki.ps1 (or wait for the 5-min watch), then check both pages on https://mindattic.com/glmz/.'
