$ErrorActionPreference = 'Stop'
$cs = 'Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;'
$editDir = 'D:\Projects\MindAttic\StreetSamurai\engine\data\exports\edits2'
$slug = 'the-one-who-doesnt-stop-019e609c'

Add-Type -AssemblyName System.Data
$conn = New-Object System.Data.SqlClient.SqlConnection $cs
$conn.Open()
$tx = $conn.BeginTransaction()
try {
    $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
    $cmd.CommandText = "SELECT Id FROM Strands WHERE Slug=@s"
    [void]$cmd.Parameters.AddWithValue('@s', $slug)
    $strandId = $cmd.ExecuteScalar()
    if (-not $strandId) { throw "Strand not found for slug $slug" }
    Write-Output "Strand: $strandId"

    $files = Get-ChildItem -Path $editDir -Filter 'beat-*.txt' | Sort-Object Name
    $total = 0
    foreach ($f in $files) {
        if ($f.Name -notmatch 'beat-(\d+)\.txt') { continue }
        $num = [int]$Matches[1]
        $text = (Get-Content -Raw -Encoding UTF8 $f.FullName).TrimEnd("`r","`n")
        $u = $conn.CreateCommand(); $u.Transaction = $tx
        $u.CommandText = @"
UPDATE b SET b.Text=@t, b.Stale=1, b.WasCorrected=1, b.TextHash=NULL, b.AudioPath=NULL, b.UpdatedAt=SYSUTCDATETIME()
FROM Beats b
JOIN StrandBeats sb ON sb.BeatId=b.Id AND sb.StrandId=@sid
WHERE b.Number=@n
"@
        [void]$u.Parameters.AddWithValue('@t', $text)
        [void]$u.Parameters.AddWithValue('@n', $num)
        [void]$u.Parameters.AddWithValue('@sid', $strandId)
        $rows = $u.ExecuteNonQuery()
        Write-Output ("beat {0}: {1} row(s), {2} chars" -f $num, $rows, $text.Length)
        if ($rows -ne 1) { throw "Beat $num updated $rows rows (expected 1)" }
        $total += $rows
    }
    $tx.Commit()
    Write-Output "COMMITTED. $total beats updated."

    $v = $conn.CreateCommand()
    $v.CommandText = "SELECT CASE WHEN Text LIKE N'%'+NCHAR(8212)+N'%' THEN 'em-dash OK' ELSE 'check' END, LEFT(Text,52) FROM Beats WHERE Number=1273"
    $r = $v.ExecuteReader(); while ($r.Read()) { Write-Output ("1273: {0} | {1}" -f $r.GetString(0), $r.GetString(1)) }; $r.Close()
}
catch { $tx.Rollback(); Write-Output "ROLLED BACK: $($_.Exception.Message)"; throw }
finally { $conn.Close() }
