# Merges four duplicate GSPL place pairs so the surviving row is CUMULATIVE.
#
# Each pair is one rich "keeper" (has a Places detail row + EntityTags, seeded during the
# Matthew pass) and one bare "stub" (Entities row only, seeded during the Mark pass). The
# stubs are NOT junk - each holds specific content the keeper lacks, chiefly the Mark verse
# citations and a few concrete details. So this folds the stub's unique content into the
# keeper by targeted insertion at verified anchors, adds the stub's name as a PlaceAlias so
# lookups by the plain name still resolve, records the old name in DeprecatedEntityNames,
# then retires the stub with IsActive=0 (never a hard DELETE).
#
# All eight rows have BeatEntityPresence = 0, so nothing points at either side.
# Dry-run by default; pass -Apply to write.

param([switch]$Apply)

$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
Add-Type -AssemblyName System.Data

$conn = New-Object System.Data.SqlClient.SqlConnection("Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;")
$conn.Open()
$em = [char]8212
$GSPL = [guid]"0197E9C9-0003-7000-8000-000000000003"

function Exec([string]$sql, [hashtable]$p) {
    $c = $conn.CreateCommand(); $c.CommandText = $sql
    foreach ($k in $p.Keys) { [void]$c.Parameters.AddWithValue("@$k", $p[$k]) }
    return $c.ExecuteNonQuery()
}
function Scalar([string]$sql, [hashtable]$p) {
    $c = $conn.CreateCommand(); $c.CommandText = $sql
    foreach ($k in $p.Keys) { [void]$c.Parameters.AddWithValue("@$k", $p[$k]) }
    return $c.ExecuteScalar()
}

# Each edit: anchor text that must already exist, and what to insert immediately after it.
$merges = @(
  @{
    keeper = '019F9FF0-A297-7E40-9B8E-983C61540F7F'; keeperName = 'Bethany (near Jerusalem)'
    stub   = '6ECEB07D-CB63-4148-A24D-DD129F3BCC5C'; stubName   = 'Bethany'
    aliases = @('Bethany')
    edits = @(
      @{ anchor = '(a mile and a half) from Jerusalem on the road to Jericho'
         insert = ", a distance John states himself as about fifteen stadia (11:18)" }
      @{ anchor = 'even though nothing excavated can specifically confirm the Gospel events said to have happened there.'
         insert = " Mark, for his part, uses the village as the staging point and evident lodging place for Jesus and the disciples across the final Jerusalem week (11:1, 11)." }
      @{ anchor = 'Cited in: Matthew 21:17; 26:6.'
         insert = " Mark 11:1, 11. John 11:18." }
    )
  }
  @{
    keeper = '019F9FF0-A244-7580-977D-B28FC83A1152'; keeperName = 'Caesarea Philippi'
    stub   = '4B7AEF23-E7D3-4B35-972C-5652CCAB46BC'; stubName   = 'Caesarea Philippi (Panias/Banias)'
    aliases = @('Panias', 'Banias')
    edits = @(
      @{ anchor = 'votive niches cut into the cliff face date to around 200 BCE onward.'
         insert = " The niches were cut to hold statues of Pan, Echo, and Hermes." }
      @{ anchor = 'in the exact location he places them relative to the cave and spring.'
         insert = " Augustus granted the region to Herod in 20 BCE; where precisely Herod's Augusteum then stood " + $em + " at Banias itself, or at the nearby site of Omrit " + $em + " remains an unresolved dispute among archaeologists (see OMRIT)." }
      @{ anchor = 'Cited in: Matthew (beat covering 16:13).'
         insert = " Mark 8:27-30." }
    )
  }
  @{
    keeper = '019F9FF0-A23B-7989-BB6E-0F36525C3212'; keeperName = 'Gennesaret (region)'
    stub   = '9161A2EC-1595-4061-A638-5F11B05C3B61'; stubName   = 'Gennesaret'
    aliases = @('Gennesaret')
    edits = @(
      @{ anchor = 'in Jewish War 3.10.8 (Thackeray translation'
         insert = ", cited in some editions as 3.516-521" }
      @{ anchor = 'of its specific reputation for exceptional agricultural fertility in exactly this period.'
         insert = " Independently of the text, the first-century fishing boat recovered from the lake mud nearby in 1986 gives physical corroboration of the region's fishing economy." }
      @{ anchor = 'Cited in: Matthew (beat covering 14:34)'
         insert = "; Mark 6:53" }
    )
  }
  @{
    keeper = '019F9FF0-A2C3-7804-9711-A099D0FD2442'; keeperName = "Praetorium (Pilate's headquarters)"
    stub   = '8CBC0D62-590D-4D3C-BDE4-DBCDD35A8AE3'; stubName   = 'Praetorium'
    aliases = @('Praetorium')
    edits = @(
      @{ anchor = 'as to which building that actually was (see PONTIUS PILATE for the man himself).'
         insert = " Mark places the mocking and the scourging at the same location (15:16), describing it as the governor's residential and administrative compound." }
      @{ anchor = 'Cited in: Matthew 27:27;'
         insert = " Mark 15:16;" }
    )
  }
)

$fail = 0
foreach ($m in $merges) {
    Write-Host ("=== {0}" -f $m.keeperName)
    $desc = [string](Scalar "SELECT Description FROM Entities WHERE Id=@Id" @{ Id = [guid]$m.keeper })
    $orig = $desc.Length
    foreach ($e in $m.edits) {
        $at = $desc.IndexOf($e.anchor, [System.StringComparison]::Ordinal)
        if ($at -lt 0) {
            Write-Host ("    ANCHOR NOT FOUND: '{0}...'" -f $e.anchor.Substring(0, [Math]::Min(55, $e.anchor.Length)))
            $fail++
            continue
        }
        if ($desc.IndexOf($e.insert, [System.StringComparison]::Ordinal) -ge 0) {
            Write-Host ("    already merged, skipping: '{0}...'" -f $e.insert.Trim().Substring(0, [Math]::Min(45, $e.insert.Trim().Length)))
            continue
        }
        $desc = $desc.Insert($at + $e.anchor.Length, $e.insert)
        Write-Host ("    + {0} chars after '...{1}'" -f $e.insert.Length, $e.anchor.Substring([Math]::Max(0, $e.anchor.Length - 40)))
    }
    Write-Host ("    description {0} -> {1} chars" -f $orig, $desc.Length)
    Write-Host ("    aliases to add: {0}" -f ($m.aliases -join ', '))
    Write-Host ("    retire stub: {0}" -f $m.stubName)

    if (-not $Apply) { continue }

    [void](Exec "UPDATE Entities SET Description=@D, ModifiedAt=SYSUTCDATETIME() WHERE Id=@Id" @{ D = $desc; Id = [guid]$m.keeper })

    # aliases on the surviving Places row, so the retired name still resolves
    $pos = [int](Scalar "SELECT ISNULL(MAX(Position),-1)+1 FROM PlaceAliases WHERE PlaceId=@P" @{ P = [guid]$m.keeper })
    foreach ($a in $m.aliases) {
        $have = [int](Scalar "SELECT COUNT(*) FROM PlaceAliases WHERE PlaceId=@P AND Value=@V" @{ P = [guid]$m.keeper; V = $a })
        if ($have -gt 0) { Write-Host "    alias exists: $a"; continue }
        # PlaceAliases.Id is a bigint IDENTITY, same as DeprecatedEntityNames.Id - omit it.
        [void](Exec "INSERT INTO PlaceAliases (PlaceId, Position, Value) VALUES (@P, @Pos, @V)" @{ P = [guid]$m.keeper; Pos = $pos; V = $a })
        $pos++
    }

    # name-resolution breadcrumb
    $dep = [int](Scalar "SELECT COUNT(*) FROM DeprecatedEntityNames WHERE EntityId=@E AND DeprecatedName=@D" @{ E = [guid]$m.keeper; D = $m.stubName })
    if ($dep -eq 0) {
        # Id is a bigint IDENTITY - omit it. (Passing a GUID here fails silently-ish:
        # the .NET exception is non-terminating, so the script carries on and reports
        # success for the surrounding steps.)
        [void](Exec @"
INSERT INTO DeprecatedEntityNames (UniverseId, DeprecatedName, CanonicalName, EntityId, Notes, AddedAt)
VALUES (@U, @D, @C, @E, @N, SYSUTCDATETIME())
"@ @{ U = $GSPL; D = $m.stubName; C = $m.keeperName; E = [guid]$m.keeper
      N = "Duplicate place row merged into the canonical entry; unique content folded in, row retired." })
    }

    # soft-retire the stub (never a hard DELETE)
    [void](Exec "UPDATE Entities SET IsActive=0, Status='merged', ArchivedAt=SYSUTCDATETIME(), ModifiedAt=SYSUTCDATETIME() WHERE Id=@Id" @{ Id = [guid]$m.stub })
    Write-Host "    APPLIED"
}

Write-Host ""
if ($fail -gt 0) { Write-Host "$fail anchor(s) not found - nothing should be trusted until that is resolved." }
if ($Apply) { Write-Host "APPLIED." } else { Write-Host "DRY RUN - nothing written. Re-run with -Apply." }
$conn.Close()
