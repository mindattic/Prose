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

$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT Id, Text FROM Beats WHERE Number > 12866"
$reader = $cmd.ExecuteReader()
$rows = @()
while ($reader.Read()) {
    $rows += [PSCustomObject]@{ Id = $reader.GetGuid(0); Text = $reader.GetString(1) }
}
$reader.Close()

Write-Host "Processing $($rows.Count) beats..."
foreach ($row in $rows) {
    $t = $row.Text
    $fixed = $t.Replace('--', " $emdash ").Replace("  $emdash  ", " $emdash ")
    # Fix note header "N - Title" -> "N (emdash) Title" only on first line
    $lines = $fixed -split "`n", 2
    if ($lines[0] -match '^(\d+) - (.+)$') {
        $lines[0] = "$($Matches[1]) $emdash $($Matches[2])"
        $fixed = ($lines -join "`n")
    }
    if ($fixed -ne $t) {
        $hash = Sha256Hex $fixed
        $u = $conn.CreateCommand()
        $u.CommandText = "UPDATE Beats SET Text=@Text, TextHash=@Hash, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id"
        $u.Parameters.AddWithValue("@Text", $fixed) | Out-Null
        $u.Parameters.AddWithValue("@Hash", $hash) | Out-Null
        $u.Parameters.AddWithValue("@Id", $row.Id) | Out-Null
        $u.ExecuteNonQuery() | Out-Null
    }
}
Write-Host "Fix pass complete."
$conn.Close()
