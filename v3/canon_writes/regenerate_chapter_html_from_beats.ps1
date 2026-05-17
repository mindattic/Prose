# Regenerates the chapter-level Html column for A Restless Mind and Inside the
# Cage by assembling each chapter's beats in Id order with section breaks.
# Required because the old A Restless Mind Html still describes the cage scene
# that has now moved to Inside the Cage, and Inside the Cage itself has empty
# Html despite having 6 beats.

$ErrorActionPreference = 'Stop'
$connStr = 'Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;'
$conn = New-Object System.Data.SqlClient.SqlConnection $connStr

$chapters = @(
    @{ Id = [Guid]'5A0959EB-5619-BF91-F59F-FB8632C80259'; Title = 'A Restless Mind' },
    @{ Id = [Guid]'CF64FEFC-01E9-4BA9-8EC1-B760C8B9398D'; Title = 'Inside the Cage' }
)

$conn.Open()

foreach ($ch in $chapters) {
    # Pull beats in Id order
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = 'SELECT Title, [Text] FROM ChapterBeats WHERE ChapterId = @c ORDER BY Id;'
    [void]$cmd.Parameters.AddWithValue('@c', $ch.Id)
    $reader = $cmd.ExecuteReader()
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine('# ' + $ch.Title)
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine('*Protagonist: Kyle Ellen Corbin-Vister*')
    [void]$sb.AppendLine('')
    $first = $true
    while ($reader.Read()) {
        if (-not $first) {
            [void]$sb.AppendLine('')
            [void]$sb.AppendLine('---')
            [void]$sb.AppendLine('')
        }
        $title = $reader['Title']
        if ($title -and $title.Trim().Length -gt 0) {
            [void]$sb.AppendLine('## ' + $title)
            [void]$sb.AppendLine('')
        }
        [void]$sb.AppendLine($reader['Text'])
        $first = $false
    }
    $reader.Close()
    $cmd.Dispose()

    $html = $sb.ToString()
    Write-Host ("{0}: assembled HTML {1} chars from beats" -f $ch.Title, $html.Length)

    # Write to Chapters.Html and Records.Json.html in a single transaction
    $tx = $conn.BeginTransaction()
    try {
        $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
        $cmd.CommandText = 'UPDATE Chapters SET Html = @h, ModifiedAt = SYSUTCDATETIME() WHERE Id = @id;'
        [void]$cmd.Parameters.AddWithValue('@h',  $html)
        [void]$cmd.Parameters.AddWithValue('@id', $ch.Id)
        $cmd.ExecuteNonQuery() | Out-Null; $cmd.Dispose()

        $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
        $cmd.CommandText = "UPDATE Records SET Json = JSON_MODIFY(Json, '`$.html', CONVERT(NVARCHAR(MAX), @h)), UpdatedAt = SYSUTCDATETIME() WHERE EntityId = @id;"
        [void]$cmd.Parameters.AddWithValue('@h',  $html)
        [void]$cmd.Parameters.AddWithValue('@id', $ch.Id)
        $cmd.ExecuteNonQuery() | Out-Null; $cmd.Dispose()

        $tx.Commit()
        Write-Host ("  -> committed")
    } catch {
        $tx.Rollback()
        Write-Host ("  -> ROLLED BACK: {0}" -f $_)
        throw
    }
}

$conn.Close()
Write-Host "=== Done ==="
