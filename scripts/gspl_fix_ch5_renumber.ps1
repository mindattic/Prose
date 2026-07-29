$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
Add-Type -AssemblyName System.Data

$connStr = "Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

function Sha256Hex([string]$text) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($text.Trim())
    $hash = $sha.ComputeHash($bytes)
    return ([System.BitConverter]::ToString($hash) -replace '-', '').ToLower()
}

$emdash = [char]8212
$offset = 63   # ch5's notes landed as 1-17 instead of continuing after ch8's 63

# ---- Renumber the 17 misnumbered Notes rows (SortKey 3200-4000 in the Notes node) ----
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT b.Id, b.Text FROM BeatNodes bn JOIN Beats b ON b.Id=bn.BeatId WHERE bn.NodeId='019FA96D-7D48-75E0-9BD9-2190171276DC' AND bn.SortKey BETWEEN 3200 AND 4000 ORDER BY bn.SortKey"
$reader = $cmd.ExecuteReader()
$noteRows = @()
while ($reader.Read()) { $noteRows += [PSCustomObject]@{ Id = $reader.GetGuid(0); Text = $reader.GetString(1) } }
$reader.Close()

if ($noteRows.Count -ne 17) { Write-Host "EXPECTED 17 rows, got $($noteRows.Count) -- ABORTING"; $conn.Close(); exit 1 }

foreach ($row in $noteRows) {
    $lines = $row.Text -split "`n", 2
    if ($lines[0] -match "^(\d+) $emdash (.+)$") {
        $oldNum = [int]$Matches[1]
        $newNum = $oldNum + $offset
        $lines[0] = "$newNum $emdash $($Matches[2])"
        $fixed = ($lines -join "`n")
        $hash = Sha256Hex $fixed
        $u = $conn.CreateCommand()
        $u.CommandText = "UPDATE Beats SET Text=@Text, TextHash=@Hash, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id"
        $u.Parameters.AddWithValue("@Text", $fixed) | Out-Null
        $u.Parameters.AddWithValue("@Hash", $hash) | Out-Null
        $u.Parameters.AddWithValue("@Id", $row.Id) | Out-Null
        $u.ExecuteNonQuery() | Out-Null
        Write-Host "  renumbered note $oldNum -> $newNum"
    } else {
        Write-Host "  WARNING: header pattern did not match for beat $($row.Id): $($lines[0])"
    }
}

# ---- Fix [[N]] references in ch5's 3 chapter beats + 8 glossary entries ----
function Fix-References([guid]$nodeId, [string]$label) {
    $c = $conn.CreateCommand()
    $c.CommandText = "SELECT b.Id, b.Text FROM BeatNodes bn JOIN Beats b ON b.Id=bn.BeatId WHERE bn.NodeId=@NodeId"
    $c.Parameters.AddWithValue("@NodeId", $nodeId) | Out-Null
    $r = $c.ExecuteReader()
    $rows = @()
    while ($r.Read()) { $rows += [PSCustomObject]@{ Id = $r.GetGuid(0); Text = $r.GetString(1) } }
    $r.Close()

    foreach ($row in $rows) {
        $t = $row.Text
        $fixed = $t
        # Replace from 17 down to 1 (order-independent since brackets are exact tokens, but descending avoids any doubt)
        for ($i = 17; $i -ge 1; $i--) {
            $fixed = $fixed.Replace("[$i]", "[$($i + $offset)]")
        }
        if ($fixed -ne $t) {
            $hash = Sha256Hex $fixed
            $u = $conn.CreateCommand()
            $u.CommandText = "UPDATE Beats SET Text=@Text, TextHash=@Hash, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id"
            $u.Parameters.AddWithValue("@Text", $fixed) | Out-Null
            $u.Parameters.AddWithValue("@Hash", $hash) | Out-Null
            $u.Parameters.AddWithValue("@Id", $row.Id) | Out-Null
            $u.ExecuteNonQuery() | Out-Null
            Write-Host "  fixed references in $label beat $($row.Id)"
        }
    }
}

Fix-References ([guid]"019FA96C-5EA0-7E7A-B025-CF3F824AC465") "ch5-chapter"

# Glossary: only touch the 8 rows belonging to ch5 (SortKey 1700-2050), not the whole shared node
$cg = $conn.CreateCommand()
$cg.CommandText = "SELECT b.Id, b.Text FROM BeatNodes bn JOIN Beats b ON b.Id=bn.BeatId WHERE bn.NodeId='019FA96D-8DD0-70E4-8D98-34AC48833B7E' AND bn.SortKey BETWEEN 1700 AND 2050"
$rg = $cg.ExecuteReader()
$glossRows = @()
while ($rg.Read()) { $glossRows += [PSCustomObject]@{ Id = $rg.GetGuid(0); Text = $rg.GetString(1) } }
$rg.Close()
if ($glossRows.Count -ne 8) { Write-Host "EXPECTED 8 glossary rows, got $($glossRows.Count) -- check manually" }
foreach ($row in $glossRows) {
    $t = $row.Text
    $fixed = $t
    for ($i = 17; $i -ge 1; $i--) { $fixed = $fixed.Replace("[$i]", "[$($i + $offset)]") }
    if ($fixed -ne $t) {
        $hash = Sha256Hex $fixed
        $u = $conn.CreateCommand()
        $u.CommandText = "UPDATE Beats SET Text=@Text, TextHash=@Hash, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id"
        $u.Parameters.AddWithValue("@Text", $fixed) | Out-Null
        $u.Parameters.AddWithValue("@Hash", $hash) | Out-Null
        $u.Parameters.AddWithValue("@Id", $row.Id) | Out-Null
        $u.ExecuteNonQuery() | Out-Null
        Write-Host "  fixed references in ch5-glossary beat $($row.Id)"
    }
}

Write-Host "DONE renumbering ch5 notes 1-17 -> 64-80."
$conn.Close()
