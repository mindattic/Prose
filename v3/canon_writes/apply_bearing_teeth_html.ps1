# Replaces Bearing Teeth's HTML body with the rewritten version (no Silence
# powers, motorcycle and vertical-Chicago atmosphere, laugh-or-cry ethos).
# Updates Chapters.Html and Records.Json (html field) atomically.

$ErrorActionPreference = 'Stop'
$connStr = 'Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;'
$conn = New-Object System.Data.SqlClient.SqlConnection $connStr

$bearingTeethId = [Guid]'019D6143-AB61-752D-A68E-0BC71595CD6C'
$newHtml = Get-Content -Raw -Encoding UTF8 'D:\Projects\MindAttic\StreetSamurai\v3\canon_writes\bearing_teeth_NEW_html.md'

Write-Host ("New HTML length: {0} chars" -f $newHtml.Length)

$conn.Open()
$tx = $conn.BeginTransaction()
try {
    # Chapters.Html
    $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
    $cmd.CommandText = 'UPDATE Chapters SET Html = @h, ModifiedAt = SYSUTCDATETIME() WHERE Id = @id;'
    [void]$cmd.Parameters.AddWithValue('@h',  $newHtml)
    [void]$cmd.Parameters.AddWithValue('@id', $bearingTeethId)
    $rows = $cmd.ExecuteNonQuery()
    Write-Host ("Chapters.Html: {0} row(s) updated" -f $rows)
    $cmd.Dispose()

    # Records.Json.html via JSON_MODIFY (avoids ConvertFrom-Json on the big blob)
    $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
    $cmd.CommandText = "UPDATE Records SET Json = JSON_MODIFY(Json, '`$.html', CONVERT(NVARCHAR(MAX), @h)), UpdatedAt = SYSUTCDATETIME() WHERE EntityId = @id;"
    [void]$cmd.Parameters.AddWithValue('@h',  $newHtml)
    [void]$cmd.Parameters.AddWithValue('@id', $bearingTeethId)
    $rows = $cmd.ExecuteNonQuery()
    Write-Host ("Records.Json.html: {0} row(s) updated" -f $rows)
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
