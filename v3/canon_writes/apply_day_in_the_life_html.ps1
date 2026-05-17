# Writes the full Day in the Life HTML body into the live chapter row.
# Chapter Id was assigned during the restructure: 367fdf7f-9760-4712-9f30-402a647d05d7.

$ErrorActionPreference = 'Stop'
$connStr = 'Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;'
$conn = New-Object System.Data.SqlClient.SqlConnection $connStr

$chapterId = [Guid]'367fdf7f-9760-4712-9f30-402a647d05d7'
$html = Get-Content -Raw -Encoding UTF8 'D:\Projects\MindAttic\StreetSamurai\v3\canon_writes\day_in_the_life_html.md'

Write-Host ("HTML length: {0} chars" -f $html.Length)

$conn.Open()
$tx = $conn.BeginTransaction()
try {
    $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
    $cmd.CommandText = 'UPDATE Chapters SET Html = @h, Status = ''draft'', ModifiedAt = SYSUTCDATETIME() WHERE Id = @id;'
    [void]$cmd.Parameters.AddWithValue('@h',  $html)
    [void]$cmd.Parameters.AddWithValue('@id', $chapterId)
    $rows = $cmd.ExecuteNonQuery()
    Write-Host ("Chapters.Html: {0} row(s)" -f $rows)
    $cmd.Dispose()

    $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
    $cmd.CommandText = "UPDATE Records SET Json = JSON_MODIFY(Json, '`$.html', CONVERT(NVARCHAR(MAX), @h)), UpdatedAt = SYSUTCDATETIME() WHERE EntityId = @id;"
    [void]$cmd.Parameters.AddWithValue('@h',  $html)
    [void]$cmd.Parameters.AddWithValue('@id', $chapterId)
    $rows = $cmd.ExecuteNonQuery()
    Write-Host ("Records.Json.html: {0} row(s)" -f $rows)
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
