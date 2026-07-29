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

$beatId = [guid]"BBE32C65-0010-42B9-B5A8-6A325814233D"
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT Text FROM Beats WHERE Id=@Id"
$cmd.Parameters.AddWithValue("@Id", $beatId) | Out-Null
$current = $cmd.ExecuteScalar()

$extra = "This chapter adds: uniquely named (with John) as the disciple sent ahead to prepare the Passover (22:8), where Mark/Matthew say only `"his disciples`"; also the specific three-exchange denial sequence and rooster-crow timing (22:54-62) [279]."
$updated = "$current`n`n$extra"
$hash = Sha256Hex $updated

$u = $conn.CreateCommand()
$u.CommandText = "UPDATE Beats SET Text=@Text, TextHash=@Hash, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id"
$u.Parameters.AddWithValue("@Text", $updated) | Out-Null
$u.Parameters.AddWithValue("@Hash", $hash) | Out-Null
$u.Parameters.AddWithValue("@Id", $beatId) | Out-Null
$u.ExecuteNonQuery() | Out-Null

Write-Host "Appended to SIMON (PETER)"
$conn.Close()
