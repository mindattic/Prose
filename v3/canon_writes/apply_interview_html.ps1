# Replaces The Interview HTML with the powers-stripped rewrite.

$ErrorActionPreference = 'Stop'
$connStr = 'Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;'
$conn = New-Object System.Data.SqlClient.SqlConnection $connStr

$interviewId = [Guid]'019DAD5F-DB77-766B-9D54-8FB43A11BE18'
$newHtml = Get-Content -Raw -Encoding UTF8 'D:\Projects\MindAttic\StreetSamurai\v3\canon_writes\interview_NEW_html.md'

Write-Host ("New HTML length: {0} chars" -f $newHtml.Length)

$conn.Open()
$tx = $conn.BeginTransaction()
try {
    $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
    $cmd.CommandText = 'UPDATE Chapters SET Html = @h, ModifiedAt = SYSUTCDATETIME() WHERE Id = @id;'
    [void]$cmd.Parameters.AddWithValue('@h',  $newHtml)
    [void]$cmd.Parameters.AddWithValue('@id', $interviewId)
    $rows = $cmd.ExecuteNonQuery()
    Write-Host ("Chapters.Html: {0} row(s)" -f $rows)
    $cmd.Dispose()

    $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
    $cmd.CommandText = "UPDATE Records SET Json = JSON_MODIFY(Json, '`$.html', CONVERT(NVARCHAR(MAX), @h)), UpdatedAt = SYSUTCDATETIME() WHERE EntityId = @id;"
    [void]$cmd.Parameters.AddWithValue('@h',  $newHtml)
    [void]$cmd.Parameters.AddWithValue('@id', $interviewId)
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
