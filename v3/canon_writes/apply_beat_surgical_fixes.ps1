# Surgical edits to remove the last two Silence-power phrases from beats 20
# and 21 of A Borrowed Hand. Preserves scene beats and the italicized refrains;
# strips hamon-state and bank-discharge language.

$ErrorActionPreference = 'Stop'
$connStr = 'Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;'
$conn = New-Object System.Data.SqlClient.SqlConnection $connStr

$conn.Open()
$tx = $conn.BeginTransaction()
try {
    # Beat 20: Eighty-Five Thousand -- replace one phrase
    $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
    $cmd.CommandText = "UPDATE ChapterBeats SET [Text] = REPLACE([Text], @old, @new) WHERE Id = 20;"
    [void]$cmd.Parameters.AddWithValue('@old', 'the hamon is cold blue and the bank is empty and the weight is the weight he has carried')
    [void]$cmd.Parameters.AddWithValue('@new', 'the saya is wet from the trench and the weight is the weight he has carried')
    $rows = $cmd.ExecuteNonQuery()
    Write-Host ("Beat 20 replacement: {0} row(s)" -f $rows)
    $cmd.Dispose()

    # Beat 21: Rain, Then Anesthesia -- replace the longer hamon/bank passage
    $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
    $cmd.CommandText = "UPDATE ChapterBeats SET [Text] = REPLACE([Text], @old, @new) WHERE Id = 21;"
    [void]$cmd.Parameters.AddWithValue('@old', 'Silence is still in the borrowed grip, cold blue hamon almost invisible in the rain, the bank still empty because the bank has been empty since the trench and the bank is not the point. *The blade is the point.*')
    [void]$cmd.Parameters.AddWithValue('@new', 'Silence is still in the borrowed grip, the saya wet and dark in the rain. The blade was always the point. *The blade is the point.*')
    $rows = $cmd.ExecuteNonQuery()
    Write-Host ("Beat 21 replacement: {0} row(s)" -f $rows)
    $cmd.Dispose()

    $tx.Commit()
    Write-Host "=== Committed ==="
}
catch {
    $tx.Rollback()
    Write-Host "ROLLED BACK: $_"
    throw
}
finally {
    $conn.Close()
}
