# Replaces The Walk beat's cage-entry ending with the new Sable-defers-to-
# tomorrow ending. Surgical -- uses SQL REPLACE with the exact old passage so
# the substitution targets the ending only.

$ErrorActionPreference = 'Stop'
$connStr = 'Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;'
$conn = New-Object System.Data.SqlClient.SqlConnection $connStr

$oldEnding = Get-Content -Raw -Encoding UTF8 'D:\Projects\MindAttic\StreetSamurai\v3\canon_writes\walk_old_ending.txt'
$newEnding = Get-Content -Raw -Encoding UTF8 'D:\Projects\MindAttic\StreetSamurai\v3\canon_writes\walk_new_ending.txt'

# Trim trailing newline from the captured old-ending file so the REPLACE
# matches the live row (which has no trailing newline on the last paragraph).
$oldEnding = $oldEnding.TrimEnd("`r", "`n")
$newEnding = $newEnding.TrimEnd("`r", "`n")

Write-Host ("Old ending length: {0} | New ending length: {1}" -f $oldEnding.Length, $newEnding.Length)

$conn.Open()
$tx = $conn.BeginTransaction()
try {
    # Sanity check that the old ending is present
    $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
    $cmd.CommandText = "SELECT CHARINDEX(@old, [Text]) FROM ChapterBeats WHERE Id = 31;"
    [void]$cmd.Parameters.AddWithValue('@old', $oldEnding)
    $pos = [int]$cmd.ExecuteScalar()
    $cmd.Dispose()
    if ($pos -eq 0) {
        throw "Old ending text not found in beat 31; aborting to avoid clobbering the wrong content"
    }
    Write-Host ("Old ending found at character position: {0}" -f $pos)

    $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
    $cmd.CommandText = "UPDATE ChapterBeats SET [Text] = REPLACE([Text], @old, @new) WHERE Id = 31;"
    [void]$cmd.Parameters.AddWithValue('@old', $oldEnding)
    [void]$cmd.Parameters.AddWithValue('@new', $newEnding)
    $rows = $cmd.ExecuteNonQuery()
    Write-Host ("Beat 31 (The Walk) ending replaced: {0} row(s)" -f $rows)
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
