# Audits Beats.TextHash against the actual prose, across every universe.
#
# WHY THIS MATTERS (it is not cosmetic):
# NodeReviewService decides which beats changed since they were last scored by comparing
# Beat.TextHash against the NodeReviewBeatScore.BeatTextHash recorded at review time. If a
# prose edit leaves TextHash stale, the edited beat compares EQUAL and looks UNCHANGED — so
# it silently keeps a score that was awarded to different words, and no report flags it.
# Prose changes without the system noticing. That is the failure this audit exists to catch.
#
# The DbContext now recomputes the hash on every save (StampBeatTextHash), so EF paths can no
# longer drift. RAW SQL BYPASSES EF ENTIRELY — direct `UPDATE Beats SET Text=...` from a
# script or an ad-hoc session cannot be caught there. This script is that backstop; run it
# after any raw-SQL prose edit, and before a review pass you intend to trust.
#
# Dry-run by default. -Fix recomputes the stale hashes.
#
# What -Fix means downstream: a repaired beat will (correctly) no longer match the hash
# recorded at its last review, so the next review pass sees it as changed and re-scores it.
# That is the intended outcome — it stops a stale score being trusted. It does NOT touch the
# Stale flag, which is what narration/audio uses.

# -Universe scopes the audit. Default GSPL, because GLMZ and especially SCRY/VIGL are
# actively authored from other sessions: a beat mid-write legitimately shows a NULL hash
# (the trigger doing its job on an in-flight raw write), and reporting that as "drift" here
# is noise. Pass -Universe all when you deliberately want the whole picture.
param(
    [switch]$Fix,
    [ValidateSet('gspl','glmz','scry','all')]
    [string]$Universe = 'gspl'
)
$ErrorActionPreference = 'Stop'

$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
. "$PSScriptRoot\gspl_db.ps1"

$conn = Open-SS
$sha = [System.Security.Cryptography.SHA256]::Create()

function Get-BeatHash([string]$t) {
    if ($null -eq $t) { $t = '' }
    return ([System.BitConverter]::ToString($sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($t.Trim()))) -replace '-', '').ToLower()
}

$uniName = @{
    '0197e9c9-0001-7000-8000-000000000001' = 'GLMZ'
    '0197e9c9-0002-7000-8000-000000000002' = 'SCRY'
    '0197e9c9-0003-7000-8000-000000000003' = 'GSPL'
}

$uniIds = @{
    gspl = "'0197E9C9-0003-7000-8000-000000000003'"
    glmz = "'0197E9C9-0001-7000-8000-000000000001'"
    scry = "'0197E9C9-0002-7000-8000-000000000002'"
}
$scope = ''
if ($Universe -ne 'all') { $scope = "  AND n.UniverseId = $($uniIds[$Universe])" }
Write-Host ("scope: {0}" -f $Universe.ToUpper())

$cmd = $conn.CreateCommand()
$cmd.CommandText = @"
SELECT DISTINCT bt.Id, bt.Text, bt.TextHash, n.UniverseId, n.Slug
FROM Nodes n
JOIN BeatNodes bn ON bn.NodeId = n.Id
JOIN Beats bt ON bt.Id = bn.BeatId
WHERE bn.IsEnabled = 1
$scope
"@
$rdr = $cmd.ExecuteReader()
$stale = New-Object System.Collections.ArrayList
$nulls = New-Object System.Collections.ArrayList
$ok = 0
while ($rdr.Read()) {
    $txt = ''
    if (-not $rdr.IsDBNull(1)) { $txt = $rdr.GetString(1) }
    $u = $rdr.GetGuid(3).ToString()
    $uname = $u
    if ($uniName.ContainsKey($u)) { $uname = $uniName[$u] }
    $row = [pscustomobject]@{ Id = $rdr.GetGuid(0); Uni = $uname; Slug = $rdr.GetString(4); Hash = (Get-BeatHash $txt) }
    if ($rdr.IsDBNull(2)) { [void]$nulls.Add($row); continue }
    if ($rdr.GetString(2).ToLower() -ne $row.Hash) { [void]$stale.Add($row) } else { $ok++ }
}
$rdr.Close()

Write-Host ("in step        : {0}" -f $ok)
Write-Host ("STALE hash     : {0}   <- edited prose the review pass would treat as unchanged" -f $stale.Count)
Write-Host ("NULL hash      : {0}   <- never stamped; cannot be compared at all" -f $nulls.Count)

foreach ($grp in @(($stale + $nulls) | Group-Object Uni | Sort-Object Name)) {
    Write-Host ("  {0,-6} {1}" -f $grp.Name, $grp.Count)
    foreach ($n in ($grp.Group | Group-Object Slug | Sort-Object Count -Descending | Select-Object -First 6)) {
        Write-Host ("      {0,-46} {1}" -f $n.Name, $n.Count)
    }
}

if (-not $Fix) {
    Write-Host ""
    Write-Host "DRY RUN - nothing written. Re-run with -Fix to stamp the correct hashes."
    $conn.Close()
    return
}

$done = 0
foreach ($r in ($stale + $nulls)) {
    [void](Invoke-SSNonQuery $conn "UPDATE Beats SET TextHash=@H WHERE Id=@Id" @{ H = $r.Hash; Id = $r.Id } -Expect 1 -What "stamp hash $($r.Id)")
    $done++
}
Write-Host ""
Write-Host ("stamped {0} beat(s)" -f $done)

# prove it: re-read and confirm zero drift remains
$v = $conn.CreateCommand()
$v.CommandText = $cmd.CommandText
$vr = $v.ExecuteReader()
$bad = 0
while ($vr.Read()) {
    $txt = ''
    if (-not $vr.IsDBNull(1)) { $txt = $vr.GetString(1) }
    if ($vr.IsDBNull(2)) { $bad++; continue }
    if ($vr.GetString(2).ToLower() -ne (Get-BeatHash $txt)) { $bad++ }
}
$vr.Close()
if ($bad -gt 0) { throw "VERIFY FAILED: $bad beat(s) still drifted after the fix" }
Write-Host "verified: 0 drifted, 0 unstamped"
$conn.Close()
