param([switch]$Apply)
$ErrorActionPreference = 'Stop'   # fail loudly: a SqlException is otherwise non-terminating

$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
Add-Type -AssemblyName System.Data

$conn = New-Object System.Data.SqlClient.SqlConnection("Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;")
$conn.Open()

function Sha256Hex([string]$t) {
    $s = [System.Security.Cryptography.SHA256]::Create()
    return ([System.BitConverter]::ToString($s.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($t.Trim()))) -replace '-','').ToLower()
}

# note -> chapter node + a distinctive keyword in that chapter's prose
$map = @(
 @{n=1;  node='019FA049-5D94-766F-A919-4623FD605028'; kw='Abraham'}
 @{n=23; node='019FA064-CBA6-7FF6-828B-72094212CB22'; kw='the mountain'}
 @{n=24; node='019FA064-CBA6-7FF6-828B-72094212CB22'; kw='Sermon on the Plain'}
 @{n=25; node='019FA064-CBA6-7FF6-828B-72094212CB22'; kw='hate your enemy'}
 @{n=27; node='019FA065-8069-7E01-98D2-686871E63831'; kw='daily bread'}
 @{n=29; node='019FA066-CCC6-746B-A184-E781F3461446'; kw='Legion'}
 @{n=30; node='019FA067-8522-77F9-898C-52F3ACA42AD1'; kw='tax-farming'}
 @{n=31; node='019FA068-3D3A-7CE3-BD9C-F0463448908A'; kw='dust of that place'}
 @{n=32; node='019FA068-DA02-71F2-AB5E-E84E36383284'; kw='Bethsaida'}
 @{n=33; node='019FA069-844F-7A56-A07C-D5037832F038'; kw='Beelzebul'}
 @{n=34; node='019FA069-844F-7A56-A07C-D5037832F038'; kw='Nineveh'}
 @{n=35; node='019FA06B-7580-76BD-93D9-2ADDCEE9AF4C'; kw='tekton'}
 @{n=36; node='019FA06C-2D81-7D7E-87EE-BC3BA620B663'; kw='Machaerus'}
 @{n=37; node='019FA06C-2D81-7D7E-87EE-BC3BA620B663'; kw='Salome'}
 @{n=38; node='019FA06C-F68B-7BC8-96BA-0F00A6216BD7'; kw='Canaanite'}
 @{n=39; node='019FA06D-89CE-7DFF-871E-E5AACFEA94DA'; kw='Caesarea Philippi'}
 @{n=40; node='019FA06E-4B5A-7AAB-80B3-3EF0B408119C'; kw='Tabor'}
 @{n=42; node='019FA06E-E541-76D7-8867-57D1241C3DDC'; kw='talents'}
 @{n=44; node='019FA070-1F63-7891-8ABA-40A617CF7273'; kw='Jericho'}
 @{n=48; node='019FA072-2582-769C-A2B5-85E052E09347'; kw='phylacteries'}
 @{n=49; node='019FA073-292F-7D88-973F-2FB76C93F677'; kw='not one stone'}
 @{n=50; node='019FA073-7BBC-79E9-B2A8-F4F23309C2A3'; kw='talent'}
)

$ok = 0; $miss = 0
foreach ($m in $map) {
    $c = $conn.CreateCommand()
    $c.CommandText = "SELECT b.Id, b.Text FROM BeatNodes bn JOIN Beats b ON b.Id=bn.BeatId WHERE bn.NodeId=@N AND bn.IsEnabled=1 AND b.Text NOT LIKE 'Then and Now%' ORDER BY bn.SortKey"
    $c.Parameters.AddWithValue("@N", [guid]$m.node) | Out-Null
    $r = $c.ExecuteReader(); $rows = @()
    while ($r.Read()) { $rows += [pscustomobject]@{ Id = $r.GetGuid(0); Text = $r.GetString(1) } }
    $r.Close()

    $done = $false
    foreach ($row in $rows) {
        if ($row.Text.Contains("[$($m.n)]")) { $done = $true; break }   # already cited somewhere
        $idx = $row.Text.IndexOf($m.kw, [System.StringComparison]::OrdinalIgnoreCase)
        if ($idx -lt 0) { continue }

        # find end of the sentence containing the keyword
        $end = -1
        for ($i = $idx; $i -lt $row.Text.Length - 1; $i++) {
            $ch = $row.Text[$i]
            if ($ch -eq '.' -or $ch -eq '?' -or $ch -eq '!') {
                $j = $i + 1
                while ($j -lt $row.Text.Length -and ($row.Text[$j] -eq '"' -or $row.Text[$j] -eq ')' -or $row.Text[$j] -eq ']')) { $j++ }
                if ($j -ge $row.Text.Length -or $row.Text[$j] -eq ' ' -or $row.Text[$j] -eq "`n") { $end = $j; break }
            }
        }
        if ($end -lt 0) { continue }

        # don't double-cite a sentence that already ends in a citation
        $tail = $row.Text.Substring([Math]::Max(0, $end - 6), [Math]::Min(6, $end))
        if ($tail -match '\[\d+\]\s*$') { continue }

        $new = $row.Text.Substring(0, $end) + " [$($m.n)]" + $row.Text.Substring($end)
        $new = $new -replace ("\[" + $m.n + "\]  +"), ("[" + $m.n + "] ")
        $sStart = [Math]::Max(0, $end - 150)
        Write-Host ("[{0}] ...{1}" -f $m.n, ($new.Substring($sStart, [Math]::Min(175, $new.Length - $sStart)) -replace "`n", " "))
        if ($Apply) {
            $u = $conn.CreateCommand()
            $u.CommandText = "UPDATE Beats SET Text=@T, TextHash=@H, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id"
            $u.Parameters.AddWithValue("@T", $new) | Out-Null
            $u.Parameters.AddWithValue("@H", (Sha256Hex $new)) | Out-Null
            $u.Parameters.AddWithValue("@Id", $row.Id) | Out-Null
            $u.ExecuteNonQuery() | Out-Null
        }
        $ok++; $done = $true; break
    }
    if (-not $done) { Write-Host "  !! no anchor found for note $($m.n) (kw '$($m.kw)')"; $miss++ }
}
$conn.Close()
if ($Apply) { Write-Host "APPLIED $ok (missed $miss)" } else { Write-Host "DRY RUN: $ok would be attached (missed $miss)" }
