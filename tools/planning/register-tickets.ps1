<#
.SYNOPSIS
    Register the read-along ticket tree (docs/planning/read-along-tickets.json) in MindAttic.Tickets (RA-T2.1.2.1).

.DESCRIPTION
    Walks the tickets in id order (the plan's build order), so every parent is registered before its children,
    and runs one tickets-CLI command per node. The command template is a parameter because the CLI's flags live
    in the MindAttic.Tickets repo: check `tickets --help` once and pass the matching template if it differs.
    Done nodes (Epic 1, shipped in 64e84d3) are registered closed with that commit as evidence.

    -WhatIf (the default until you pass -Apply) prints every command and runs nothing.
    With -Apply, the PRS-T number each command prints is captured and written back into the JSON as externalId,
    so a second run skips what is already registered.

.EXAMPLE
    ./tools/planning/register-tickets.ps1                 # dry run
    ./tools/planning/register-tickets.ps1 -Apply
#>
[CmdletBinding()]
param(
    [string] $Json = (Join-Path $PSScriptRoot '..\..\docs\planning\read-along-tickets.json'),
    [string] $Project = 'PRS',
    # {kind} {title} {parent} {status} {body} are substituted; quote-safe via argument array, not string concat.
    [string[]] $Template = @('tickets', 'add', '--project', '{project}', '--kind', '{kind}', '--title', '{title}',
                             '--parent', '{parent}', '--status', '{status}', '--body', '{body}'),
    [string] $ShippedCommit = '64e84d3',
    [switch] $Apply
)
$ErrorActionPreference = 'Stop'
$doc = Get-Content -Raw -Encoding utf8 $Json | ConvertFrom-Json
$byId = @{}
foreach ($t in $doc.tickets) { $byId[$t.id] = $t }

$kindMap = @{ epic = 'epic'; feature = 'feature'; story = 'story'; task = 'task'; decision = 'decision' }

function Body($t) {
    $parts = @("Plan id: $($t.id)")
    foreach ($f in 'estimate', 'depends', 'files', 'do', 'test', 'doneWhen', 'acceptance', 'question', 'options', 'record') {
        if ($t.PSObject.Properties.Name -contains $f -and $t.$f) {
            $v = if ($t.$f -is [array]) { $t.$f -join ', ' } else { $t.$f }
            $parts += "${f}: $v"
        }
    }
    if ($t.status -eq 'done') { $parts += "Shipped in $ShippedCommit." }
    $parts -join "`n"
}

$registered = 0
foreach ($t in $doc.tickets) {
    if ($t.PSObject.Properties.Name -contains 'externalId' -and $t.externalId) { continue }
    $parent = if ($t.parent -and $byId[$t.parent].PSObject.Properties.Name -contains 'externalId') { $byId[$t.parent].externalId } else { '' }
    $subst = @{
        '{project}' = $Project; '{kind}' = $kindMap[$t.level]; '{title}' = "$($t.id) · $($t.title)";
        '{parent}' = $parent; '{status}' = $(if ($t.status -eq 'done') { 'closed' } else { 'open' }); '{body}' = (Body $t)
    }
    $cmd = foreach ($a in $Template) { $x = $a; foreach ($k in $subst.Keys) { $x = $x.Replace($k, [string]$subst[$k]) }; $x }
    # Drop "--parent ''" when the node is a root.
    $clean = New-Object System.Collections.Generic.List[string]
    for ($i = 0; $i -lt $cmd.Count; $i++) {
        if ($cmd[$i] -eq '--parent' -and [string]::IsNullOrEmpty($cmd[$i + 1])) { $i++; continue }
        $clean.Add($cmd[$i])
    }
    if (-not $Apply) {
        Write-Host ("WHATIF: " + (($clean | ForEach-Object { if ($_ -match '\s') { "'$_'" } else { $_ } }) -join ' ').Split("`n")[0])
        continue
    }
    $out = & $clean[0] @($clean | Select-Object -Skip 1) 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) { throw "tickets CLI failed on $($t.id): $out" }
    $m = [regex]::Match($out, "$Project-T\d+")
    if (-not $m.Success) { throw "No $Project-T id in the CLI output for $($t.id): $out" }
    $t | Add-Member -NotePropertyName externalId -NotePropertyValue $m.Value -Force
    $registered++
    # Persist after each node, so a failure part-way loses nothing.
    $doc | ConvertTo-Json -Depth 8 | Set-Content -Encoding utf8 $Json
}
if ($Apply) { Write-Host "Registered $registered ticket(s). Re-render docs/USER_STORIES.md with: tickets db render" }
