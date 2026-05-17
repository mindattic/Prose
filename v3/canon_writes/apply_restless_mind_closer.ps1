# Inserts the new closing beat for A Restless Mind: "The Door Across The Hall"
# -- the cup-of-tea doorway scene from the rewritten synopsis. This replaces
# the structural gap left when the old Pixel beat moved to Inside the Cage.

$ErrorActionPreference = 'Stop'
$connStr = 'Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;'
$conn = New-Object System.Data.SqlClient.SqlConnection $connStr

$restlessMindId = [Guid]'5A0959EB-5619-BF91-F59F-FB8632C80259'
$beatText = Get-Content -Raw -Encoding UTF8 'D:\Projects\MindAttic\StreetSamurai\v3\canon_writes\beat_a_restless_mind_closer.md'
$beatGuid = [Guid]::NewGuid()

Write-Host ("New beat GUID: {0}" -f $beatGuid)
Write-Host ("Beat text length: {0} chars" -f $beatText.Length)

$conn.Open()

# Idempotency: skip if a beat with this title already exists in the chapter
$check = $conn.CreateCommand()
$check.CommandText = "SELECT COUNT(*) FROM ChapterBeats WHERE ChapterId = @c AND Title = N'The Door Across The Hall';"
[void]$check.Parameters.AddWithValue('@c', $restlessMindId)
$existing = [int]$check.ExecuteScalar()
$check.Dispose()
if ($existing -gt 0) {
    Write-Host "Beat already exists; no-op."
    $conn.Close()
    return
}

$tx = $conn.BeginTransaction()
try {
    $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
    $cmd.CommandText = @"
INSERT INTO ChapterBeats (BeatGuid, ChapterId, [Index], Title, Synopsis, [Text], Act, StructureRole, SceneType, FacetTag, InWorldDate)
VALUES (@guid, @ch, 0, N'The Door Across The Hall', N'', @text, 0, N'', N'scene', N'', NULL);
"@
    [void]$cmd.Parameters.AddWithValue('@guid', $beatGuid)
    [void]$cmd.Parameters.AddWithValue('@ch',   $restlessMindId)
    [void]$cmd.Parameters.AddWithValue('@text', $beatText)
    $rows = $cmd.ExecuteNonQuery()
    Write-Host ("Beat inserted: {0} row(s)" -f $rows)
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
