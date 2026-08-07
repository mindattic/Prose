$ErrorActionPreference = 'Stop'   # fail loudly: a SqlException is otherwise non-terminating
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
Add-Type -AssemblyName System.Data

$connStr = "Server=(localdb)\MSSQLLocalDB;Database=Prose;Trusted_Connection=True;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

$GlossaryNodeId = [guid]"019FA026-3758-7A57-AF65-EC1DB4303EF3"

$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT bn.BeatId, b.Text FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId=@NodeId AND bn.IsEnabled=1"
$cmd.Parameters.AddWithValue("@NodeId", $GlossaryNodeId) | Out-Null
$reader = $cmd.ExecuteReader()
$rows = @()
while ($reader.Read()) {
    $id = $reader.GetGuid(0)
    $text = $reader.GetString(1)
    $heading = ($text -split "`n")[0].Trim()
    $rows += [PSCustomObject]@{ BeatId = $id; Heading = $heading }
}
$reader.Close()

Write-Host "Read $($rows.Count) enabled glossary entries."

$sorted = $rows | Sort-Object -Property @{ Expression = { $_.Heading } } -Culture "en-US"

$sortKey = 100.0
foreach ($row in $sorted) {
    $u = $conn.CreateCommand()
    $u.CommandText = "UPDATE BeatNodes SET SortKey=@SortKey WHERE NodeId=@NodeId AND BeatId=@BeatId"
    $u.Parameters.AddWithValue("@SortKey", $sortKey) | Out-Null
    $u.Parameters.AddWithValue("@NodeId", $GlossaryNodeId) | Out-Null
    $u.Parameters.AddWithValue("@BeatId", $row.BeatId) | Out-Null
    $u.ExecuteNonQuery() | Out-Null
    $sortKey += 100.0
}

Write-Host "Resorted $($sorted.Count) glossary entries alphabetically, SortKey 100..$([int]($sortKey-100))."
Write-Host "--- First 5 ---"
$sorted | Select-Object -First 5 | ForEach-Object { Write-Host $_.Heading }
Write-Host "--- Last 5 ---"
$sorted | Select-Object -Last 5 | ForEach-Object { Write-Host $_.Heading }

$conn.Close()
