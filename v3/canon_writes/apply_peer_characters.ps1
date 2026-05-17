# Inserts 10 peer-tier freelancer characters as Entity + Record rows. Idempotent
# per character (skip if a character with the same name already exists).

$ErrorActionPreference = 'Stop'
$connStr = 'Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;'
$conn = New-Object System.Data.SqlClient.SqlConnection $connStr

$files = @(
    'D:\Projects\MindAttic\StreetSamurai\v3\canon_writes\peer_characters\01_echo.json',
    'D:\Projects\MindAttic\StreetSamurai\v3\canon_writes\peer_characters\02_maeve.json',
    'D:\Projects\MindAttic\StreetSamurai\v3\canon_writes\peer_characters\03_stash.json',
    'D:\Projects\MindAttic\StreetSamurai\v3\canon_writes\peer_characters\04_felix.json',
    'D:\Projects\MindAttic\StreetSamurai\v3\canon_writes\peer_characters\05_yuki.json',
    'D:\Projects\MindAttic\StreetSamurai\v3\canon_writes\peer_characters\06_bear.json',
    'D:\Projects\MindAttic\StreetSamurai\v3\canon_writes\peer_characters\07_sumi.json',
    'D:\Projects\MindAttic\StreetSamurai\v3\canon_writes\peer_characters\08_aleksei.json',
    'D:\Projects\MindAttic\StreetSamurai\v3\canon_writes\peer_characters\09_wren.json',
    'D:\Projects\MindAttic\StreetSamurai\v3\canon_writes\peer_characters\10_sparrow.json'
)

function ConvertTo-Slug {
    param([string]$Name)
    $s = $Name.ToLower()
    $s = $s -replace '[^a-z0-9]+', '-'
    $s = $s -replace '^-|-$', ''
    return $s
}

$conn.Open()

foreach ($file in $files) {
    $jsonText = Get-Content -Raw -Encoding UTF8 $file
    $obj = $jsonText | ConvertFrom-Json
    $name = $obj.name
    $slug = ConvertTo-Slug -Name $name

    # Idempotency: skip if an active character with this name already exists
    $check = $conn.CreateCommand()
    $check.CommandText = "SELECT COUNT(*) FROM Entities WHERE EntityType = 'character' AND Name = @n AND IsActive = 1;"
    [void]$check.Parameters.AddWithValue('@n', $name)
    $existing = [int]$check.ExecuteScalar()
    $check.Dispose()
    if ($existing -gt 0) {
        Write-Host ("Skipped (already exists): {0}" -f $name)
        continue
    }

    $id = [Guid]::NewGuid()
    $idCompact = $id.ToString('N')
    $jsonFinal = $jsonText -replace '^\{', ('{"id":"' + $idCompact + '",')

    $tx = $conn.BeginTransaction()
    try {
        $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
        $cmd.CommandText = "INSERT INTO Entities (Id, EntityType, Name, Slug, Status, Description, CreatedAt, ModifiedAt, IsActive) VALUES (@id, 'character', @n, @s, 'canon', @d, SYSUTCDATETIME(), SYSUTCDATETIME(), 1);"
        [void]$cmd.Parameters.AddWithValue('@id', $id)
        [void]$cmd.Parameters.AddWithValue('@n',  $name)
        [void]$cmd.Parameters.AddWithValue('@s',  $slug)
        [void]$cmd.Parameters.AddWithValue('@d',  $obj.description)
        $cmd.ExecuteNonQuery() | Out-Null; $cmd.Dispose()

        $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
        $cmd.CommandText = 'INSERT INTO Records (EntityId, Json, UpdatedAt) VALUES (@id, @j, SYSUTCDATETIME());'
        [void]$cmd.Parameters.AddWithValue('@id', $id)
        [void]$cmd.Parameters.AddWithValue('@j',  $jsonFinal)
        $cmd.ExecuteNonQuery() | Out-Null; $cmd.Dispose()

        $tx.Commit()
        Write-Host ("Inserted: {0} (id={1}, slug={2})" -f $name, $id, $slug)
    } catch {
        $tx.Rollback()
        Write-Host ("ROLLED BACK on {0}: {1}" -f $name, $_)
        throw
    }
}

$conn.Close()
Write-Host "=== Done ==="
