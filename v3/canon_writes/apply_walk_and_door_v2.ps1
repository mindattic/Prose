# Replaces The Walk ending (v2 -- Sable keeps hood ON, defers to tomorrow);
# replaces The Door Across The Hall (v2 -- removes the hood-back-recognition
# reference Kyle would not yet have); updates A Restless Mind synopsis (v2 --
# matches the structurally correct version where face-reveal is in chapter 4).

$ErrorActionPreference = 'Stop'
$connStr = 'Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;'
$conn = New-Object System.Data.SqlClient.SqlConnection $connStr

$walkV1 = Get-Content -Raw -Encoding UTF8 'D:\Projects\MindAttic\StreetSamurai\v3\canon_writes\walk_new_ending.txt'
$walkV2 = Get-Content -Raw -Encoding UTF8 'D:\Projects\MindAttic\StreetSamurai\v3\canon_writes\walk_new_ending_v2.txt'
$doorV2 = Get-Content -Raw -Encoding UTF8 'D:\Projects\MindAttic\StreetSamurai\v3\canon_writes\door_across_hall_v2.txt'
$synopsisV2 = Get-Content -Raw -Encoding UTF8 'D:\Projects\MindAttic\StreetSamurai\v3\canon_writes\a_restless_mind_synopsis_v2.txt'

$walkV1 = $walkV1.TrimEnd("`r","`n")
$walkV2 = $walkV2.TrimEnd("`r","`n")

$restlessMindId = [Guid]'5A0959EB-5619-BF91-F59F-FB8632C80259'

$conn.Open()
$tx = $conn.BeginTransaction()
try {
    # ---- The Walk: swap v1 (with hood-back) for v2 (hood stays on) ----
    $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
    $cmd.CommandText = "SELECT CHARINDEX(@old, [Text]) FROM ChapterBeats WHERE Id = 31;"
    [void]$cmd.Parameters.AddWithValue('@old', $walkV1)
    $pos = [int]$cmd.ExecuteScalar()
    $cmd.Dispose()
    if ($pos -eq 0) { throw "The Walk v1 ending not found; cannot safely swap" }

    $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
    $cmd.CommandText = "UPDATE ChapterBeats SET [Text] = REPLACE([Text], @old, @new) WHERE Id = 31;"
    [void]$cmd.Parameters.AddWithValue('@old', $walkV1)
    [void]$cmd.Parameters.AddWithValue('@new', $walkV2)
    $rows = $cmd.ExecuteNonQuery()
    Write-Host ("Beat 31 (The Walk) v2 swap: {0} row(s)" -f $rows)
    $cmd.Dispose()

    # ---- The Door Across The Hall: full text replace ----
    $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
    $cmd.CommandText = "UPDATE ChapterBeats SET [Text] = @t WHERE Title = N'The Door Across The Hall' AND ChapterId = @ch;"
    [void]$cmd.Parameters.AddWithValue('@t',  $doorV2)
    [void]$cmd.Parameters.AddWithValue('@ch', $restlessMindId)
    $rows = $cmd.ExecuteNonQuery()
    Write-Host ("The Door Across The Hall v2 swap: {0} row(s)" -f $rows)
    $cmd.Dispose()

    # ---- A Restless Mind synopsis: Chapters + Records.Json ----
    $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
    $cmd.CommandText = "UPDATE Chapters SET Synopsis = @s, ModifiedAt = SYSUTCDATETIME() WHERE Id = @id;"
    [void]$cmd.Parameters.AddWithValue('@s',  $synopsisV2)
    [void]$cmd.Parameters.AddWithValue('@id', $restlessMindId)
    $cmd.ExecuteNonQuery() | Out-Null; $cmd.Dispose()

    $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
    $cmd.CommandText = "UPDATE Records SET Json = JSON_MODIFY(Json, '`$.synopsis', CONVERT(NVARCHAR(MAX), @s)), UpdatedAt = SYSUTCDATETIME() WHERE EntityId = @id;"
    [void]$cmd.Parameters.AddWithValue('@s',  $synopsisV2)
    [void]$cmd.Parameters.AddWithValue('@id', $restlessMindId)
    $cmd.ExecuteNonQuery() | Out-Null; $cmd.Dispose()
    Write-Host "A Restless Mind synopsis v2 written to Chapters + Records"

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
