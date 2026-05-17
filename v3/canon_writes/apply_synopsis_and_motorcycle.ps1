# Applies the rewritten Bearing Teeth + A Restless Mind synopses and inserts
# Kyle's motorcycle. Uses SQL Server JSON_MODIFY for in-place synopsis patching
# of Records.Json -- avoids ConvertFrom-Json on the 28k+ char chapter blobs,
# which hung PowerShell in a prior attempt.

$ErrorActionPreference = 'Stop'
$connStr = 'Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;'
$conn = New-Object System.Data.SqlClient.SqlConnection $connStr

$bearingTeethId = [Guid]'019D6143-AB61-752D-A68E-0BC71595CD6C'
$restlessMindId = [Guid]'5A0959EB-5619-BF91-F59F-FB8632C80259'

$bearingTeethSynopsis = Get-Content -Raw -Encoding UTF8 'D:\Projects\MindAttic\StreetSamurai\v3\canon_writes\bearing_teeth_synopsis.txt'
$restlessMindSynopsis = Get-Content -Raw -Encoding UTF8 'D:\Projects\MindAttic\StreetSamurai\v3\canon_writes\a_restless_mind_synopsis.txt'
$motorcycleJson       = Get-Content -Raw -Encoding UTF8 'D:\Projects\MindAttic\StreetSamurai\v3\canon_writes\motorcycle_record.json'

# Generate motorcycle ID and patch the JSON in PowerShell using string-replace
# (avoids ConvertFrom-Json on the larger motorcycle JSON too; the motorcycle
# template was written without an id field on purpose).
$motorcycleId = [Guid]::NewGuid()
$motorcycleIdCompact = $motorcycleId.ToString('N')
$motorcycleJsonFinal = $motorcycleJson -replace '^\{', ('{"id":"' + $motorcycleIdCompact + '",')

$conn.Open()

# Idempotency check
$check = $conn.CreateCommand()
$check.CommandText = "SELECT COUNT(*) FROM Entities WHERE EntityType='transportation' AND Name LIKE N'%Kyle%Motorcycle%';"
$existing = [int]$check.ExecuteScalar()
$check.Dispose()
$skipMotorcycleInsert = ($existing -gt 0)

$tx = $conn.BeginTransaction()
try {
    # Bearing Teeth: update Chapters.Synopsis + patch Records.Json via JSON_MODIFY
    $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
    $cmd.CommandText = 'UPDATE Chapters SET Synopsis = @s, ModifiedAt = SYSUTCDATETIME() WHERE Id = @id;'
    [void]$cmd.Parameters.AddWithValue('@s',  $bearingTeethSynopsis)
    [void]$cmd.Parameters.AddWithValue('@id', $bearingTeethId)
    $cmd.ExecuteNonQuery() | Out-Null; $cmd.Dispose()

    $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
    $cmd.CommandText = "UPDATE Records SET Json = JSON_MODIFY(Json, '`$.synopsis', CONVERT(NVARCHAR(MAX), @s)), UpdatedAt = SYSUTCDATETIME() WHERE EntityId = @id;"
    [void]$cmd.Parameters.AddWithValue('@s',  $bearingTeethSynopsis)
    [void]$cmd.Parameters.AddWithValue('@id', $bearingTeethId)
    $cmd.ExecuteNonQuery() | Out-Null; $cmd.Dispose()
    Write-Host "Bearing Teeth synopsis updated (Chapters + Records)"

    # A Restless Mind: same shape
    $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
    $cmd.CommandText = 'UPDATE Chapters SET Synopsis = @s, ModifiedAt = SYSUTCDATETIME() WHERE Id = @id;'
    [void]$cmd.Parameters.AddWithValue('@s',  $restlessMindSynopsis)
    [void]$cmd.Parameters.AddWithValue('@id', $restlessMindId)
    $cmd.ExecuteNonQuery() | Out-Null; $cmd.Dispose()

    $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
    $cmd.CommandText = "UPDATE Records SET Json = JSON_MODIFY(Json, '`$.synopsis', CONVERT(NVARCHAR(MAX), @s)), UpdatedAt = SYSUTCDATETIME() WHERE EntityId = @id;"
    [void]$cmd.Parameters.AddWithValue('@s',  $restlessMindSynopsis)
    [void]$cmd.Parameters.AddWithValue('@id', $restlessMindId)
    $cmd.ExecuteNonQuery() | Out-Null; $cmd.Dispose()
    Write-Host "A Restless Mind synopsis updated (Chapters + Records)"

    # Motorcycle: Entity + Record only (Transportation subtype skipped)
    if (-not $skipMotorcycleInsert) {
        # Use a regex extract for the description field rather than parsing the
        # whole JSON. Description sits up near the top of the file and the
        # pattern is bounded.
        $descMatch = [regex]::Match($motorcycleJsonFinal, '"description":"((?:[^"\\]|\\.)*)"')
        if (-not $descMatch.Success) { throw "Could not extract motorcycle description for Entity row" }
        # Unescape standard JSON escapes the simple way
        $desc = $descMatch.Groups[1].Value -replace '\\n',[char]10 -replace '\\"','"' -replace '\\\\','\'

        $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
        $cmd.CommandText = "INSERT INTO Entities (Id, EntityType, Name, Slug, Status, Description, CreatedAt, ModifiedAt, IsActive) VALUES (@id, 'transportation', @name, @slug, 'canon', @desc, SYSUTCDATETIME(), SYSUTCDATETIME(), 1);"
        [void]$cmd.Parameters.AddWithValue('@id',   $motorcycleId)
        [void]$cmd.Parameters.AddWithValue('@name', "Kyle's Motorcycle -- Unbranded Matte-Black Standard")
        [void]$cmd.Parameters.AddWithValue('@slug', 'kyles-motorcycle-unbranded-matte-black-standard')
        [void]$cmd.Parameters.AddWithValue('@desc', $desc)
        $cmd.ExecuteNonQuery() | Out-Null; $cmd.Dispose()

        $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
        $cmd.CommandText = 'INSERT INTO Records (EntityId, Json, UpdatedAt) VALUES (@id, @json, SYSUTCDATETIME());'
        [void]$cmd.Parameters.AddWithValue('@id',   $motorcycleId)
        [void]$cmd.Parameters.AddWithValue('@json', $motorcycleJsonFinal)
        $cmd.ExecuteNonQuery() | Out-Null; $cmd.Dispose()
        Write-Host ("Motorcycle inserted: {0}" -f $motorcycleId)
    } else {
        Write-Host "Motorcycle entity already exists -- skipping insert"
    }

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
