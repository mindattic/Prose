# Inserts two new chapters into Bushido Coda (Day in the Life + Inside the Cage),
# renumbers existing chapters to the new 8-chapter spine, and moves 5 beats from
# old "A Restless Mind" to new "Inside the Cage". All atomic.
#
# Final spine:
#   1. Bearing Teeth          (existing, renumbered)
#   2. Day in the Life        (NEW -- mission → home → Pixel pulls bullets → bed → next day)
#   3. A Restless Mind        (existing, beats trimmed to noodle/walk/pixel)
#   4. Inside the Cage        (NEW -- receives the cage / market / Tier 1 / courtyard / thanks beats)
#   5. The Rogue AI           (existing, renumbered)
#   6. The Interview          (existing, renumbered)
#   7. Street Meat            (existing, renumbered)
#   8. A Borrowed Hand        (existing, renumbered)
#
# Synopses for the two new chapters are placeholders here; final synopses
# (with new directives folded in) get a separate pass.

$ErrorActionPreference = 'Stop'
$connStr = 'Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;'
$conn = New-Object System.Data.SqlClient.SqlConnection $connStr

$bookId = [Guid]'EB91080D-9C9C-4F2B-9B40-5FA5996BDEA1'
$restlessMindId = [Guid]'5A0959EB-5619-BF91-F59F-FB8632C80259'

# Existing chapter IDs by current Number 1-6
$bearingTeethId  = [Guid]'019D6143-AB61-752D-A68E-0BC71595CD6C'   # 1 → 1
# restlessMindId is 2 → 3
$rogueAIId       = [Guid]'6D75764C-C8DA-8E32-FD73-3DD5C43E92E2'   # 3 → 5
$interviewId     = [Guid]'019DAD5F-DB77-766B-9D54-8FB43A11BE18'   # 4 → 6
$streetMeatId    = [Guid]'019DB31F-E888-7C97-A049-65978B5CCDB3'   # 5 → 7
$borrowedHandId  = [Guid]'019DD24F-EB04-7E9F-B9C9-01450389A8B9'   # 6 → 8

# New chapter IDs
$dayInLifeId = [Guid]::NewGuid()
$insideCageId = [Guid]::NewGuid()

Write-Host "New chapter IDs:"
Write-Host "  Day in the Life: $dayInLifeId"
Write-Host "  Inside the Cage: $insideCageId"

# Beats to move: from A Restless Mind to Inside the Cage
$beatsToMove = @(32, 33, 34, 35, 36)  # Recognition Inside The Cage, Market Fight, Tier 1 At Three, Chrysanthemum Courtyard, The Thanks

# Idempotency check: if a chapter named "Day in the Life" already exists for this
# book, abort -- the script has already run.
$conn.Open()
$check = $conn.CreateCommand()
$check.CommandText = "SELECT COUNT(*) FROM Chapters c JOIN Entities e ON e.Id = c.Id WHERE c.BookId = @b AND e.Name IN (N'Day in the Life', N'Inside the Cage');"
[void]$check.Parameters.AddWithValue('@b', $bookId)
$existing = [int]$check.ExecuteScalar()
$check.Dispose()
if ($existing -gt 0) {
    Write-Host "ABORT -- $existing chapter(s) already inserted; script appears to have run. No-op."
    $conn.Close()
    return
}

# Placeholder synopses -- short and clear, marking that prose rewrites follow.
# Synopses live in separate UTF-8 files because Windows PowerShell 5.1 reads
# script files as ANSI by default and mojibakes Unicode em-dashes inline.
$dayInLifeSynopsis  = Get-Content -Raw -Encoding UTF8 'D:\Projects\MindAttic\StreetSamurai\v3\canon_writes\day_in_the_life_synopsis.txt'
$insideCageSynopsis = Get-Content -Raw -Encoding UTF8 'D:\Projects\MindAttic\StreetSamurai\v3\canon_writes\inside_the_cage_synopsis.txt'

# Minimal valid Chapter Records.Json -- full lore-rich version regenerates on next save.
function New-ChapterRecordsJson {
    param([Guid]$Id, [string]$Title, [string]$Synopsis, [int]$Number)
    $idCompact = $Id.ToString('N')
    $bookIdCompact = $bookId.ToString('N')
    $nowIso = [DateTime]::UtcNow.ToString('o')
    $obj = [ordered]@{
        id        = $idCompact
        book_id   = $bookIdCompact
        number    = $Number
        title     = $Title
        synopsis  = $Synopsis
        characters = @('Kyle Ellen Corbin-Vister')
        status    = 'draft'
        html      = ''
        beats     = @()
        created   = $nowIso
        modified  = $nowIso
    }
    return ($obj | ConvertTo-Json -Depth 10 -Compress)
}

$dayInLifeJson  = New-ChapterRecordsJson -Id $dayInLifeId  -Title 'Day in the Life'  -Synopsis $dayInLifeSynopsis  -Number 2
$insideCageJson = New-ChapterRecordsJson -Id $insideCageId -Title 'Inside the Cage' -Synopsis $insideCageSynopsis -Number 4

$tx = $conn.BeginTransaction()
try {
    # -- Phase 1: shift existing chapters to a non-conflicting position band --
    # BookChapterOrder.Position is unique on (BookId, Position). Bump everyone
    # by 100 so we can re-place them after the inserts without index conflicts.
    $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
    $cmd.CommandText = "UPDATE BookChapterOrder SET Position = Position + 100 WHERE BookId = @b;"
    [void]$cmd.Parameters.AddWithValue('@b', $bookId)
    $cmd.ExecuteNonQuery() | Out-Null
    $cmd.Dispose()
    Write-Host "Phase 1 done: BookChapterOrder positions shifted +100"

    # -- Phase 2: insert Entity + Record + Chapters rows for the two new chapters --
    foreach ($entry in @(
        @{ Id=$dayInLifeId;  Title='Day in the Life';  Slug='day-in-the-life-bushido-coda';  Synopsis=$dayInLifeSynopsis;  Json=$dayInLifeJson;  FinalNumber=2 },
        @{ Id=$insideCageId; Title='Inside the Cage'; Slug='inside-the-cage-bushido-coda'; Synopsis=$insideCageSynopsis; Json=$insideCageJson; FinalNumber=4 }
    )) {
        # Entity row (universal base -- every entity has one)
        $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
        $cmd.CommandText = @"
INSERT INTO Entities (Id, EntityType, Name, Slug, Status, Description, CreatedAt, ModifiedAt, IsActive)
VALUES (@id, 'chapter', @name, @slug, 'draft', @desc, SYSUTCDATETIME(), SYSUTCDATETIME(), 1);
"@
        [void]$cmd.Parameters.AddWithValue('@id',   $entry.Id)
        [void]$cmd.Parameters.AddWithValue('@name', $entry.Title)
        [void]$cmd.Parameters.AddWithValue('@slug', $entry.Slug)
        [void]$cmd.Parameters.AddWithValue('@desc', $entry.Synopsis)
        $cmd.ExecuteNonQuery() | Out-Null
        $cmd.Dispose()

        # Records row (canonical JSON)
        $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
        $cmd.CommandText = 'INSERT INTO Records (EntityId, Json, UpdatedAt) VALUES (@id, @json, SYSUTCDATETIME());'
        [void]$cmd.Parameters.AddWithValue('@id',   $entry.Id)
        [void]$cmd.Parameters.AddWithValue('@json', $entry.Json)
        $cmd.ExecuteNonQuery() | Out-Null
        $cmd.Dispose()

        # Chapters subtype row
        $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
        $cmd.CommandText = @"
INSERT INTO Chapters (Id, BookId, Number, Title, Synopsis, Status, Html, CreatedAt, ModifiedAt)
VALUES (@id, @bookId, @num, @title, @syn, 'draft', '', SYSUTCDATETIME(), SYSUTCDATETIME());
"@
        [void]$cmd.Parameters.AddWithValue('@id',     $entry.Id)
        [void]$cmd.Parameters.AddWithValue('@bookId', $bookId)
        [void]$cmd.Parameters.AddWithValue('@num',    $entry.FinalNumber)
        [void]$cmd.Parameters.AddWithValue('@title',  $entry.Title)
        [void]$cmd.Parameters.AddWithValue('@syn',    $entry.Synopsis)
        $cmd.ExecuteNonQuery() | Out-Null
        $cmd.Dispose()

        Write-Host ("Inserted chapter: {0} (Number={1})" -f $entry.Title, $entry.FinalNumber)
    }

    # -- Phase 3: renumber existing Chapters.Number to the new 8-chapter spine --
    # 1 → 1 (no change)
    # 2 → 3 (A Restless Mind)
    # 3 → 5 (Rogue AI)
    # 4 → 6 (Interview)
    # 5 → 7 (Street Meat)
    # 6 → 8 (Borrowed Hand)
    $renumberMap = @(
        @{ Id = $restlessMindId; NewNumber = 3 },
        @{ Id = $rogueAIId;      NewNumber = 5 },
        @{ Id = $interviewId;    NewNumber = 6 },
        @{ Id = $streetMeatId;   NewNumber = 7 },
        @{ Id = $borrowedHandId; NewNumber = 8 }
    )
    foreach ($r in $renumberMap) {
        $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
        $cmd.CommandText = 'UPDATE Chapters SET Number = @n, ModifiedAt = SYSUTCDATETIME() WHERE Id = @id;'
        [void]$cmd.Parameters.AddWithValue('@n',  $r.NewNumber)
        [void]$cmd.Parameters.AddWithValue('@id', $r.Id)
        $cmd.ExecuteNonQuery() | Out-Null
        $cmd.Dispose()
    }
    Write-Host "Phase 3 done: existing Chapters.Number renumbered"

    # -- Phase 4: rebuild BookChapterOrder for the new 8-chapter spine --
    # Positions are 0-indexed: 0..7
    $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
    $cmd.CommandText = 'DELETE FROM BookChapterOrder WHERE BookId = @b;'
    [void]$cmd.Parameters.AddWithValue('@b', $bookId)
    $cmd.ExecuteNonQuery() | Out-Null
    $cmd.Dispose()

    $orderMap = @(
        @{ Pos = 0; Id = $bearingTeethId },
        @{ Pos = 1; Id = $dayInLifeId    },
        @{ Pos = 2; Id = $restlessMindId },
        @{ Pos = 3; Id = $insideCageId   },
        @{ Pos = 4; Id = $rogueAIId      },
        @{ Pos = 5; Id = $interviewId    },
        @{ Pos = 6; Id = $streetMeatId   },
        @{ Pos = 7; Id = $borrowedHandId }
    )
    foreach ($o in $orderMap) {
        $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
        $cmd.CommandText = 'INSERT INTO BookChapterOrder (BookId, ChapterId, Position) VALUES (@b, @c, @p);'
        [void]$cmd.Parameters.AddWithValue('@b', $bookId)
        [void]$cmd.Parameters.AddWithValue('@c', $o.Id)
        [void]$cmd.Parameters.AddWithValue('@p', $o.Pos)
        $cmd.ExecuteNonQuery() | Out-Null
        $cmd.Dispose()
    }
    Write-Host "Phase 4 done: BookChapterOrder rebuilt with 8 entries"

    # -- Phase 5: move 5 beats from A Restless Mind to Inside the Cage --
    foreach ($beatId in $beatsToMove) {
        $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx
        $cmd.CommandText = 'UPDATE ChapterBeats SET ChapterId = @newCh WHERE Id = @id AND ChapterId = @oldCh;'
        [void]$cmd.Parameters.AddWithValue('@newCh', $insideCageId)
        [void]$cmd.Parameters.AddWithValue('@oldCh', $restlessMindId)
        [void]$cmd.Parameters.AddWithValue('@id',    $beatId)
        $rows = $cmd.ExecuteNonQuery()
        $cmd.Dispose()
        Write-Host ("Moved beat {0}: {1} row(s)" -f $beatId, $rows)
    }

    $tx.Commit()
    Write-Host "`n=== Committed -- Bushido Coda is now 8 chapters ==="
    Write-Host "New chapter IDs (save these):"
    Write-Host "  Day in the Life: $dayInLifeId"
    Write-Host "  Inside the Cage: $insideCageId"
}
catch {
    $tx.Rollback()
    Write-Host "ROLLED BACK: $_"
    throw
}
finally {
    $conn.Close()
}
