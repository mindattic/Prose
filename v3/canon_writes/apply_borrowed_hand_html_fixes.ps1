# Two surgical REPLACE() patches on A Borrowed Hand chapter HTML, mirroring
# the beat-level patches applied earlier.

$ErrorActionPreference = 'Stop'
$connStr = 'Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;'
$conn = New-Object System.Data.SqlClient.SqlConnection $connStr

$borrowedHandId = [Guid]'019DD24F-EB04-7E9F-B9C9-01450389A8B9'

$conn.Open()
$tx = $conn.BeginTransaction()
try {
    # Patch 1: "The hamon is cold blue under the saya, the bank is empty"
    # (the canonical "Eighty-Five Thousand" beat ending, mirrored in chapter HTML)
    $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
    $cmd.CommandText = 'UPDATE Chapters SET Html = REPLACE(Html, @old, @new), ModifiedAt = SYSUTCDATETIME() WHERE Id = @id;'
    [void]$cmd.Parameters.AddWithValue('@old', 'The hamon is cold blue under the saya, the bank is empty')
    [void]$cmd.Parameters.AddWithValue('@new', 'The saya is wet from the trench')
    [void]$cmd.Parameters.AddWithValue('@id',  $borrowedHandId)
    $rows = $cmd.ExecuteNonQuery()
    Write-Host ("Patch 1 (cold blue under saya): {0} row(s)" -f $rows)
    $cmd.Dispose()

    # Patch 2: "cold blue hamon almost invisible in the rain, the bank still empty"
    # plus the longer trailing passage if present
    $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
    $cmd.CommandText = 'UPDATE Chapters SET Html = REPLACE(Html, @old, @new), ModifiedAt = SYSUTCDATETIME() WHERE Id = @id;'
    [void]$cmd.Parameters.AddWithValue('@old', 'cold blue hamon almost invisible in the rain, the bank still empty because the bank has been empty since the trench and the bank is not the point. *The blade is the point.*')
    [void]$cmd.Parameters.AddWithValue('@new', 'the saya wet and dark in the rain. The blade was always the point. *The blade is the point.*')
    [void]$cmd.Parameters.AddWithValue('@id',  $borrowedHandId)
    $rows = $cmd.ExecuteNonQuery()
    Write-Host ("Patch 2 (rain hamon): {0} row(s)" -f $rows)
    $cmd.Dispose()

    # Mirror into Records.Json.html
    $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
    $cmd.CommandText = "SELECT Html FROM Chapters WHERE Id = @id;"
    [void]$cmd.Parameters.AddWithValue('@id', $borrowedHandId)
    $newHtml = [string]$cmd.ExecuteScalar()
    $cmd.Dispose()

    $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
    $cmd.CommandText = "UPDATE Records SET Json = JSON_MODIFY(Json, '`$.html', CONVERT(NVARCHAR(MAX), @h)), UpdatedAt = SYSUTCDATETIME() WHERE EntityId = @id;"
    [void]$cmd.Parameters.AddWithValue('@h',  $newHtml)
    [void]$cmd.Parameters.AddWithValue('@id', $borrowedHandId)
    $cmd.ExecuteNonQuery() | Out-Null; $cmd.Dispose()
    Write-Host "Mirror to Records.Json.html done"

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
