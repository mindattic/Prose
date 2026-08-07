$ErrorActionPreference = 'Stop'   # fail loudly: a SqlException is otherwise non-terminating
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
Add-Type -AssemblyName System.Data

$connStr = "Server=(localdb)\MSSQLLocalDB;Database=Prose;Trusted_Connection=True;TrustServerCertificate=True;"
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
$cmd.CommandText = "SELECT bn.BeatId, b.Text FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'"
$reader = $cmd.ExecuteReader()
$rows = @()
while ($reader.Read()) {
    $rows += [PSCustomObject]@{ Id = $reader.GetGuid(0); Text = $reader.GetString(1) }
}
$reader.Close()

$fixedCount = 0
foreach ($row in $rows) {
    $t = $row.Text
    $lines = $t -split "`n", 2
    if ($lines[0] -match '^(\d+) - (.+)$') {
        $lines[0] = "$($Matches[1]) $emdash $($Matches[2])"
        $fixed = ($lines -join "`n")
        $hash = Sha256Hex $fixed
        $u = $conn.CreateCommand()
        $u.CommandText = "UPDATE Beats SET Text=@Text, TextHash=@Hash, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id"
        $u.Parameters.AddWithValue("@Text", $fixed) | Out-Null
        $u.Parameters.AddWithValue("@Hash", $hash) | Out-Null
        $u.Parameters.AddWithValue("@Id", $row.Id) | Out-Null
        $u.ExecuteNonQuery() | Out-Null
        $fixedCount++
    }
}
Write-Host "Fixed $fixedCount legacy note headers."
$conn.Close()
