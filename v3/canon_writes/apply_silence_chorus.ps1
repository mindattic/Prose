# Atomically rewrites Silence + Chorus canon and retires the 6 Silence support-tech
# entities (the carbon-piezo-supercapacitor stack that only existed to power the
# old myth). Idempotent — safe to re-run; final state is what matters.
#
# Uses Microsoft.Data.SqlClient with parameterized SqlCommand so the JSON
# contents survive without any SQL escaping concerns.

$ErrorActionPreference = 'Stop'

$connStr = 'Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;'

# System.Data.SqlClient is built into Windows PowerShell 5.1 / .NET Framework.
# Microsoft.Data.SqlClient via Add-Type fails on transitive dependencies in PS
# 5.1, and we don't need its newer features here.
$conn = New-Object System.Data.SqlClient.SqlConnection $connStr

$silenceJson = Get-Content -Raw 'D:\Projects\MindAttic\StreetSamurai\v3\canon_writes\silence_new.json'
$chorusJson  = Get-Content -Raw 'D:\Projects\MindAttic\StreetSamurai\v3\canon_writes\chorus_new.json'

# Sanity check JSON parses
try { $null = $silenceJson | ConvertFrom-Json } catch { throw "silence_new.json failed to parse: $_" }
try { $null = $chorusJson  | ConvertFrom-Json } catch { throw "chorus_new.json failed to parse: $_"  }

$silenceId = [Guid]'CD8CE222-DE5F-44C4-B6F6-5C18721C1050'
$chorusId  = [Guid]'4AB24F74-61D4-4F45-B326-7C6B98C96279'

# 6 Silence support-tech entities to archive. Identified by the support-tech
# survey 2026-05-16 — each one existed only to underpin one of the powers
# Silence no longer has. They go inactive but stay in the table so any
# historical reference can still resolve the name.
$supportTechToArchive = @(
    'Cascades TNG-7 Blade-Surface Triboelectric Nanogenerator Film',
    'Graphene Ultracapacitor Tsuka Module',
    'Nakago Electrode Interconnect Harness',
    'Piezoelectric Shingane Core – PZT/Carbon-Fiber Composite',
    'PZT Piezoelectric Composite Shingane Core',
    'TENG Triboelectric Nanogenerator Mune Film'
)

$conn.Open()
$tx = $conn.BeginTransaction()
try {
    # ── Silence: Records.Json + Weapons.Description + Entity.Description ──
    $cmd = $conn.CreateCommand()
    $cmd.Transaction = $tx
    $cmd.CommandText = 'UPDATE Records SET Json = @json, UpdatedAt = SYSUTCDATETIME() WHERE EntityId = @id;'
    [void]$cmd.Parameters.AddWithValue('@json', $silenceJson)
    [void]$cmd.Parameters.AddWithValue('@id',   $silenceId)
    $rows = $cmd.ExecuteNonQuery()
    Write-Host "Silence Records.Json: $rows row(s) updated"
    $cmd.Dispose()

    # Mirror the description summary into the typed Weapons row + Entity row
    # so the encyclopedia tile / wiki preview pick up the new lore without a
    # round-trip through the JSON repository.
    $silenceObj = $silenceJson | ConvertFrom-Json
    $cmd = $conn.CreateCommand()
    $cmd.Transaction = $tx
    $cmd.CommandText = 'UPDATE Entities SET Description = @desc, ModifiedAt = SYSUTCDATETIME() WHERE Id = @id;'
    [void]$cmd.Parameters.AddWithValue('@desc', $silenceObj.description)
    [void]$cmd.Parameters.AddWithValue('@id',   $silenceId)
    $rows = $cmd.ExecuteNonQuery()
    Write-Host "Silence Entities.Description: $rows row(s) updated"
    $cmd.Dispose()

    # ── Chorus: same shape ──
    $cmd = $conn.CreateCommand()
    $cmd.Transaction = $tx
    $cmd.CommandText = 'UPDATE Records SET Json = @json, UpdatedAt = SYSUTCDATETIME() WHERE EntityId = @id;'
    [void]$cmd.Parameters.AddWithValue('@json', $chorusJson)
    [void]$cmd.Parameters.AddWithValue('@id',   $chorusId)
    $rows = $cmd.ExecuteNonQuery()
    Write-Host "Chorus Records.Json: $rows row(s) updated"
    $cmd.Dispose()

    $chorusObj = $chorusJson | ConvertFrom-Json
    $cmd = $conn.CreateCommand()
    $cmd.Transaction = $tx
    $cmd.CommandText = 'UPDATE Entities SET Description = @desc, ModifiedAt = SYSUTCDATETIME() WHERE Id = @id;'
    [void]$cmd.Parameters.AddWithValue('@desc', $chorusObj.description)
    [void]$cmd.Parameters.AddWithValue('@id',   $chorusId)
    $rows = $cmd.ExecuteNonQuery()
    Write-Host "Chorus Entities.Description: $rows row(s) updated"
    $cmd.Dispose()

    # ── Archive the 6 Silence-power support-tech entities ──
    foreach ($name in $supportTechToArchive) {
        $cmd = $conn.CreateCommand()
        $cmd.Transaction = $tx
        $cmd.CommandText = @'
UPDATE Entities
   SET IsActive   = 0,
       Status     = 'archived',
       ArchivedAt = SYSUTCDATETIME(),
       ModifiedAt = SYSUTCDATETIME()
 WHERE Name = @name
   AND EntityType = 'technology'
   AND IsActive = 1;
'@
        [void]$cmd.Parameters.AddWithValue('@name', $name)
        $rows = $cmd.ExecuteNonQuery()
        Write-Host ("Support-tech archived: {0} ({1} row)" -f $name, $rows)
        $cmd.Dispose()
    }

    $tx.Commit()
    Write-Host '`n=== Committed ==='
}
catch {
    $tx.Rollback()
    Write-Host "ROLLED BACK: $_"
    throw
}
finally {
    $conn.Close()
}
