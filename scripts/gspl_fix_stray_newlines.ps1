$ErrorActionPreference = 'Stop'   # fail loudly: a SqlException is otherwise non-terminating
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
Add-Type -AssemblyName System.Data

$connStr = "Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

function Sha256Hex([string]$text) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($text.Trim())
    return ([System.BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-', '').ToLower()
}

# --- Pass 1: trim leading/trailing whitespace on every beat in the four books ---
$cmd = $conn.CreateCommand()
$cmd.CommandText = @"
SELECT bt.Id, bt.Text FROM Nodes c
JOIN Nodes p ON p.Id=c.ParentNodeId
JOIN BeatNodes bn ON bn.NodeId=c.Id
JOIN Beats bt ON bt.Id=bn.BeatId
WHERE p.NodeCode IN ('MATTHEW','MARK','LUKE','JOHN') AND bn.IsEnabled=1
"@
$rdr = $cmd.ExecuteReader()
$rows = @()
while ($rdr.Read()) { $rows += [pscustomobject]@{ Id = $rdr.GetGuid(0); Text = $rdr.GetString(1) } }
$rdr.Close()

$trimmed = 0
foreach ($r in $rows) {
    $new = $r.Text.Trim()
    if ($new -ne $r.Text) {
        $c2 = $conn.CreateCommand()
        $c2.CommandText = "UPDATE Beats SET Text=@T, TextHash=@H, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id"
        $c2.Parameters.AddWithValue("@T", $new) | Out-Null
        $c2.Parameters.AddWithValue("@H", (Sha256Hex $new)) | Out-Null
        $c2.Parameters.AddWithValue("@Id", $r.Id) | Out-Null
        $c2.ExecuteNonQuery() | Out-Null
        $trimmed++
    }
}
Write-Host "pass 1: trimmed $trimmed beats"

# --- Pass 2: report any remaining odd-LF beats (genuine mid-paragraph strays, GSPL 5g1) ---
$cmd2 = $conn.CreateCommand()
$cmd2.CommandText = @"
SELECT p.NodeCode, c.Title, bt.Id, bt.Text FROM Nodes c
JOIN Nodes p ON p.Id=c.ParentNodeId
JOIN BeatNodes bn ON bn.NodeId=c.Id
JOIN Beats bt ON bt.Id=bn.BeatId
WHERE p.NodeCode IN ('MATTHEW','MARK','LUKE','JOHN') AND bn.IsEnabled=1
  AND (LEN(bt.Text)-LEN(REPLACE(bt.Text,CHAR(10),''))) % 2 = 1
"@
$rdr2 = $cmd2.ExecuteReader()
$rem = @()
while ($rdr2.Read()) { $rem += [pscustomobject]@{ Book = $rdr2.GetString(0); Node = $rdr2.GetString(1); Id = $rdr2.GetGuid(2); Text = $rdr2.GetString(3) } }
$rdr2.Close()
Write-Host "pass 2: $($rem.Count) beats still have an odd LF count"

$fixed = 0
foreach ($r in $rem) {
    # find single newlines that are not part of a \n\n pair
    $m = [regex]::Matches($r.Text, "(?<!`n)`n(?!`n)")
    Write-Host ("  {0} | {1} | {2} lone newline(s)" -f $r.Book, $r.Node.Substring(0,[Math]::Min(34,$r.Node.Length)), $m.Count)
    foreach ($x in $m) {
        $s = [Math]::Max(0, $x.Index - 45)
        $ctx = $r.Text.Substring($s, [Math]::Min(95, $r.Text.Length - $s)) -replace "`n", "<<LF>>"
        Write-Host "       ...$ctx..."
    }
    if ($m.Count -gt 0) {
        # GSPL 5g1 rule: collapse a lone newline to a single space, except after a hyphen
        $new = [regex]::Replace($r.Text, "(?<=-)`n(?!`n)", "")
        $new = [regex]::Replace($new, "(?<!`n)`n(?!`n)", " ")
        $new = [regex]::Replace($new, "  +", " ")
        $c3 = $conn.CreateCommand()
        $c3.CommandText = "UPDATE Beats SET Text=@T, TextHash=@H, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id"
        $c3.Parameters.AddWithValue("@T", $new) | Out-Null
        $c3.Parameters.AddWithValue("@H", (Sha256Hex $new)) | Out-Null
        $c3.Parameters.AddWithValue("@Id", $r.Id) | Out-Null
        $c3.ExecuteNonQuery() | Out-Null
        $fixed++
    }
}
$conn.Close()
Write-Host "pass 2: collapsed lone newlines in $fixed beats"
