Add-Type -AssemblyName System.Data
$connStr = "Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Integrated Security=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection $connStr
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = @"
SELECT CAST(bn.SortKey AS INT) AS SK, b.Id AS BeatId, b.Text AS Text,
  ISNULL(bep.EntityName,'(none)') AS POV
FROM BeatNodes bn
JOIN Beats b ON b.Id = bn.BeatId
LEFT JOIN BeatEntityPresence bep ON bep.BeatId = b.Id AND bep.PresenceType = 'pov'
WHERE bn.NodeId = '019F6364-41EE-7FDA-B271-708280A4AC9E' AND bn.IsEnabled = 1
ORDER BY CAST(bn.SortKey AS INT)
"@
$reader = $cmd.ExecuteReader()
$rows = @()
while ($reader.Read()) {
    $rows += [PSCustomObject]@{
        SK = $reader["SK"]
        BeatId = $reader["BeatId"].ToString()
        POV = $reader["POV"]
        Text = $reader["Text"]
    }
}
$conn.Close()

Write-Host "Total beats: $($rows.Count)"

$outDir = "D:\Projects\MindAttic\StreetSamurai\audit-outlines-2026-08-05\structural"
$numChunks = 8
$chunkSize = [Math]::Ceiling($rows.Count / $numChunks)

for ($i = 0; $i -lt $numChunks; $i++) {
    $start = $i * $chunkSize
    if ($start -ge $rows.Count) { break }
    $end = [Math]::Min($start + $chunkSize - 1, $rows.Count - 1)
    $slice = $rows[$start..$end]
    $chunkNum = $i + 1
    $path = Join-Path $outDir ("chunk-{0:D2}.txt" -f $chunkNum)
    $sb = New-Object System.Text.StringBuilder
    foreach ($r in $slice) {
        [void]$sb.AppendLine("=====BEAT SK=$($r.SK) ID=$($r.BeatId) POV=$($r.POV)=====")
        [void]$sb.AppendLine($r.Text)
        [void]$sb.AppendLine("")
    }
    [System.IO.File]::WriteAllText($path, $sb.ToString(), [System.Text.Encoding]::UTF8)
    Write-Host "Wrote $path : SK $($slice[0].SK) to $($slice[-1].SK) ($($slice.Count) beats)"
}
