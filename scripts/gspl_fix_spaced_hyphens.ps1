# Converts the spaced ASCII hyphen used as a dash (" - ") into the series' spaced em dash.
#
# Why: the campaign swept for the DOUBLE hyphen (" -- ") and normalised 1,236 note headers,
# but never checked the single spaced hyphen in body prose. JOHN has 0 of them and 1,174 em
# dashes; MARK had 726 hyphens against only 519 em dashes, so a whole book was reading
# cheaper than its siblings. The corpus convention is the SPACED em dash (4,935 of 4,936).
#
# Guard: a numeric/date range must NOT become an em dash. " (c. 20 BCE - 50 CE) " and
# " (c. 56 - c. 120 CE) " are ranges; a dash merely FOLLOWING a verse number
# (" Luke 11:51 - the two figures ") is a real dash and is converted. So the skip rule keys
# off what comes AFTER the hyphen, not before: skip when the right side opens a number,
# optionally behind a "c." circa marker.
#
# Covers enabled beats in all four books, plus GSPL entity descriptions (the merged place
# descriptions carry the same defect). Dry-run by default; pass -Apply to write.

param([switch]$Apply, [int]$ShowSkips = 40)
$ErrorActionPreference = 'Stop'   # fail loudly: a SqlException is otherwise non-terminating

$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
Add-Type -AssemblyName System.Data

$conn = New-Object System.Data.SqlClient.SqlConnection("Server=(localdb)\MSSQLLocalDB;Database=Prose;Trusted_Connection=True;TrustServerCertificate=True;")
$conn.Open()
$emDash = [string][char]0x2014
$GSPL = [guid]"0197E9C9-0003-7000-8000-000000000003"

function Sha256Hex([string]$text) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    return ([System.BitConverter]::ToString($sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($text.Trim()))) -replace '-', '').ToLower()
}

$script:skipped = New-Object System.Collections.ArrayList

# Replace " - " with " em " except where the right-hand side opens a numeric range.
function Fix-Dashes([string]$text, [string]$label) {
    $sb = New-Object System.Text.StringBuilder
    $i = 0; $n = 0
    while ($i -lt $text.Length) {
        $at = $text.IndexOf(' - ', $i, [System.StringComparison]::Ordinal)
        if ($at -lt 0) { [void]$sb.Append($text.Substring($i)); break }
        [void]$sb.Append($text.Substring($i, $at - $i))
        $rest = $text.Substring($at + 3)
        # A range looks like "(c. 56 - c. 120 CE)" or "(c. 20 BCE - 50 CE)": the right side
        # opens either a circa marker, or a bare number carrying an era label. A dash that
        # merely precedes a verse list ("same verse - 8:12, 22:13") is NOT a range - if that
        # were skipped, its closing partner would still convert and leave a mismatched pair.
        if ($rest -match '^(c\.\s*\d|\d+\s*(BCE|BCE\b|CE|BC|AD)\b)') {
            # numeric range - leave the ASCII hyphen alone
            $s = [Math]::Max(0, $at - 40)
            [void]$script:skipped.Add(("{0}: ...{1}..." -f $label, (($text.Substring($s, [Math]::Min(90, $text.Length - $s))) -replace "`n", ' ')))
            [void]$sb.Append(' - ')
        } else {
            [void]$sb.Append(' ' + $emDash + ' ')
            $n++
        }
        $i = $at + 3
    }
    return @{ Text = $sb.ToString(); Count = $n }
}

# ---- beats ----
$cmd = $conn.CreateCommand()
$cmd.CommandText = @"
SELECT p.NodeCode, c.Title, bt.Id, bt.Text
FROM Nodes c
JOIN Nodes p ON p.Id = c.ParentNodeId
JOIN BeatNodes bn ON bn.NodeId = c.Id
JOIN Beats bt ON bt.Id = bn.BeatId
WHERE p.NodeCode IN ('MATTHEW','MARK','LUKE','JOHN')
  AND bn.IsEnabled = 1
  AND CHARINDEX(' - ', bt.Text) > 0
ORDER BY p.NodeCode, c.SortKey
"@
$rdr = $cmd.ExecuteReader()
$beats = @()
while ($rdr.Read()) { $beats += [pscustomobject]@{ Book = $rdr.GetString(0); Chapter = $rdr.GetString(1); Id = $rdr.GetGuid(2); Text = $rdr.GetString(3) } }
$rdr.Close()

$perBook = @{}; $beatsTouched = 0
foreach ($b in $beats) {
    $res = Fix-Dashes $b.Text ("{0} / {1}" -f $b.Book, $b.Chapter)
    if ($res.Count -eq 0) { continue }
    $beatsTouched++
    if (-not $perBook.ContainsKey($b.Book)) { $perBook[$b.Book] = 0 }
    $perBook[$b.Book] += $res.Count
    if ($Apply) {
        $c2 = $conn.CreateCommand()
        $c2.CommandText = "UPDATE Beats SET Text=@T, TextHash=@H, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id"
        [void]$c2.Parameters.AddWithValue("@T", $res.Text)
        [void]$c2.Parameters.AddWithValue("@H", (Sha256Hex $res.Text))
        [void]$c2.Parameters.AddWithValue("@Id", $b.Id)
        [void]$c2.ExecuteNonQuery()
    }
}

# ---- GSPL entity descriptions ----
$cmd2 = $conn.CreateCommand()
$cmd2.CommandText = "SELECT Id, Name, Description FROM Entities WHERE UniverseId=@U AND IsActive=1 AND Description IS NOT NULL AND CHARINDEX(' - ', Description) > 0"
[void]$cmd2.Parameters.AddWithValue("@U", $GSPL)
$rdr2 = $cmd2.ExecuteReader()
$ents = @()
while ($rdr2.Read()) { $ents += [pscustomobject]@{ Id = $rdr2.GetGuid(0); Name = $rdr2.GetString(1); Desc = $rdr2.GetString(2) } }
$rdr2.Close()

$entFixes = 0; $entsTouched = 0
foreach ($e in $ents) {
    $res = Fix-Dashes $e.Desc ("entity / " + $e.Name)
    if ($res.Count -eq 0) { continue }
    $entsTouched++; $entFixes += $res.Count
    if ($Apply) {
        $c3 = $conn.CreateCommand()
        $c3.CommandText = "UPDATE Entities SET Description=@D, ModifiedAt=SYSUTCDATETIME() WHERE Id=@Id"
        [void]$c3.Parameters.AddWithValue("@D", $res.Text)
        [void]$c3.Parameters.AddWithValue("@Id", $e.Id)
        [void]$c3.ExecuteNonQuery()
    }
}

Write-Host "beats:"
foreach ($k in ($perBook.Keys | Sort-Object)) { Write-Host ("  {0,-8} {1} hyphens -> em dash" -f $k, $perBook[$k]) }
Write-Host ("  beats touched      : {0}" -f $beatsTouched)
Write-Host ("entities:")
Write-Host ("  descriptions touched: {0}  ({1} hyphens)" -f $entsTouched, $entFixes)
Write-Host ""
Write-Host ("LEFT ALONE as numeric ranges: {0}" -f $script:skipped.Count)
$shown = 0
foreach ($s in $script:skipped) { if ($shown -ge $ShowSkips) { break }; Write-Host ("  " + $s); $shown++ }
Write-Host ""
if ($Apply) { Write-Host "APPLIED." } else { Write-Host "DRY RUN - nothing written. Re-run with -Apply." }
$conn.Close()
