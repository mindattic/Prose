# Inserts the network/AI worldbuilding document as a Document entity.
# Idempotent: skips if a document with this title already exists.

$ErrorActionPreference = 'Stop'
$connStr = 'Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;'
$conn = New-Object System.Data.SqlClient.SqlConnection $connStr

$title = "The Network in 2225: An Operator's Guide"
$slug  = 'the-network-in-2225-an-operators-guide'
$body  = Get-Content -Raw -Encoding UTF8 'D:\Projects\MindAttic\StreetSamurai\v3\canon_writes\network_doc.md'

Write-Host ("Document body length: {0} chars" -f $body.Length)

$conn.Open()

# Idempotency
$check = $conn.CreateCommand()
$check.CommandText = "SELECT COUNT(*) FROM Entities WHERE EntityType = 'document' AND Name = @n AND IsActive = 1;"
[void]$check.Parameters.AddWithValue('@n', $title)
$existing = [int]$check.ExecuteScalar()
$check.Dispose()
if ($existing -gt 0) {
    Write-Host "Document already exists; no-op."
    $conn.Close()
    return
}

$id = [Guid]::NewGuid()
$idCompact = $id.ToString('N')

# Build the Records.Json by hand. ConvertTo-Json was hanging on the 13k-char
# body. Manual escaping is faster and gives us full control over the shape.
function Escape-JsonString {
    param([string]$s)
    if ($null -eq $s) { return '' }
    return $s.Replace('\','\\').Replace('"','\"').Replace("`r",'').Replace("`n",'\n').Replace("`t",'\t')
}

$descSummary = "Operator-perspective reference document on the Network in 2225 -- ambient-field model rather than VR/cyberspace, BCI proprioceptive integration, Phi-as-quantum-compute-time economics, the plural AI ecosystem (sanctioned/unsanctioned/ELFs), and the rogue-AI long-con framing. Compiled by Sparrow Costa-Tagaq for in-network distribution."

$bodyEscaped  = Escape-JsonString $body
$descEscaped  = Escape-JsonString $descSummary
$titleEscaped = Escape-JsonString $title

$docJson = '{"id":"' + $idCompact + '","name":"' + $titleEscaped + '","type":"document","aliases":["Network 2225","Operator Network Guide"],"description":"' + $descEscaped + '","category":"essay","file_name":"the_network_in_2225.md","title":"' + $titleEscaped + '","author":"Sparrow Costa-Tagaq (compiled)","body":"' + $bodyEscaped + '","tags":["worldbuilding","network","ai","phi","quanta","elf","rogue-ai","technology","operator-guide"]}'

$tx = $conn.BeginTransaction()
try {
    $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
    $cmd.CommandText = "INSERT INTO Entities (Id, EntityType, Name, Slug, Status, Description, CreatedAt, ModifiedAt, IsActive) VALUES (@id, 'document', @n, @s, 'canon', @d, SYSUTCDATETIME(), SYSUTCDATETIME(), 1);"
    [void]$cmd.Parameters.AddWithValue('@id', $id)
    [void]$cmd.Parameters.AddWithValue('@n',  $title)
    [void]$cmd.Parameters.AddWithValue('@s',  $slug)
    [void]$cmd.Parameters.AddWithValue('@d',  $descSummary)
    $cmd.ExecuteNonQuery() | Out-Null; $cmd.Dispose()

    $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
    $cmd.CommandText = 'INSERT INTO Records (EntityId, Json, UpdatedAt) VALUES (@id, @j, SYSUTCDATETIME());'
    [void]$cmd.Parameters.AddWithValue('@id', $id)
    [void]$cmd.Parameters.AddWithValue('@j',  $docJson)
    $cmd.ExecuteNonQuery() | Out-Null; $cmd.Dispose()

    $tx.Commit()
    Write-Host ("Inserted document: {0} (id={1})" -f $title, $id)
}
catch {
    $tx.Rollback()
    Write-Host "ROLLED BACK: $_"
    throw
}
finally {
    $conn.Close()
}
