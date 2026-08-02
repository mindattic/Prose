SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID('tempdb..#Losers') IS NOT NULL DROP TABLE #Losers;
CREATE TABLE #Losers (Id UNIQUEIDENTIFIER PRIMARY KEY, EntityType NVARCHAR(50), Name NVARCHAR(500), WinnerId UNIQUEIDENTIFIER);
;WITH Ranked AS (
    SELECT Id, EntityType, Name, CreatedAt,
        FIRST_VALUE(Id) OVER (PARTITION BY EntityType, LOWER(Name) ORDER BY CreatedAt ASC, Id ASC ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING) AS WinnerId,
        ROW_NUMBER() OVER (PARTITION BY EntityType, LOWER(Name) ORDER BY CreatedAt ASC, Id ASC) AS rn
    FROM Entities WHERE IsActive = 1 AND EntityType NOT IN ('quote') AND Name NOT IN ('(untitled document)', '(unattributed quote)')
)
INSERT INTO #Losers (Id, EntityType, Name, WinnerId) SELECT Id, EntityType, Name, WinnerId FROM Ranked WHERE rn > 1;

DECLARE @loserCount INT = (SELECT COUNT(*) FROM #Losers);
PRINT 'Loser rows identified: ' + CAST(@loserCount AS VARCHAR(20));

BEGIN TRANSACTION;

-- ── BeatEntities (PK: BeatId, EntityId) ──────────────────────────────────
-- Step 1: drop loser rows that would collide with an existing winner row for the same beat.
DELETE be
FROM BeatEntities be
JOIN #Losers l ON l.Id = be.EntityId
WHERE EXISTS (SELECT 1 FROM BeatEntities be2 WHERE be2.BeatId = be.BeatId AND be2.EntityId = l.WinnerId);
PRINT 'BeatEntities collision rows deleted: ' + CAST(@@ROWCOUNT AS VARCHAR(20));

-- Step 2: reassign the rest.
UPDATE be
SET be.EntityId = l.WinnerId
FROM BeatEntities be
JOIN #Losers l ON l.Id = be.EntityId;
PRINT 'BeatEntities rows reassigned: ' + CAST(@@ROWCOUNT AS VARCHAR(20));

-- ── BeatEntityMentions (PK: BeatId, EntityId) ────────────────────────────
DELETE bem
FROM BeatEntityMentions bem
JOIN #Losers l ON l.Id = bem.EntityId
WHERE EXISTS (SELECT 1 FROM BeatEntityMentions bem2 WHERE bem2.BeatId = bem.BeatId AND bem2.EntityId = l.WinnerId);
PRINT 'BeatEntityMentions collision rows deleted: ' + CAST(@@ROWCOUNT AS VARCHAR(20));

UPDATE bem
SET bem.EntityId = l.WinnerId
FROM BeatEntityMentions bem
JOIN #Losers l ON l.Id = bem.EntityId;
PRINT 'BeatEntityMentions rows reassigned: ' + CAST(@@ROWCOUNT AS VARCHAR(20));

-- ── BeatEntityPresence (PK: BeatId, EntityId) ────────────────────────────
DELETE bep
FROM BeatEntityPresence bep
JOIN #Losers l ON l.Id = bep.EntityId
WHERE EXISTS (SELECT 1 FROM BeatEntityPresence bep2 WHERE bep2.BeatId = bep.BeatId AND bep2.EntityId = l.WinnerId);
PRINT 'BeatEntityPresence collision rows deleted: ' + CAST(@@ROWCOUNT AS VARCHAR(20));

UPDATE bep
SET bep.EntityId = l.WinnerId
FROM BeatEntityPresence bep
JOIN #Losers l ON l.Id = bep.EntityId;
PRINT 'BeatEntityPresence rows reassigned: ' + CAST(@@ROWCOUNT AS VARCHAR(20));

-- ── CharacterAffiliations.FactionId (surrogate PK, no collision risk) ────
UPDATE ca
SET ca.FactionId = l.WinnerId
FROM CharacterAffiliations ca
JOIN #Losers l ON l.Id = ca.FactionId;
PRINT 'CharacterAffiliations.FactionId rows reassigned: ' + CAST(@@ROWCOUNT AS VARCHAR(20));

-- ── Soft-deactivate every loser Entity row (matches existing EfRepository.Delete pattern) ──
UPDATE e
SET e.IsActive = 0, e.Status = 'archived', e.ArchivedAt = SYSUTCDATETIME(), e.ModifiedAt = SYSUTCDATETIME()
FROM Entities e
JOIN #Losers l ON l.Id = e.Id;
PRINT 'Entities soft-deactivated: ' + CAST(@@ROWCOUNT AS VARCHAR(20));

COMMIT TRANSACTION;
PRINT 'COMMITTED.';

-- ── Verification ──────────────────────────────────────────────────────────
SELECT EntityType, COUNT(*) AS RemainingDupeGroups
FROM (
    SELECT EntityType, LOWER(Name) AS Name, COUNT(*) c
    FROM Entities
    WHERE IsActive = 1 AND EntityType NOT IN ('quote')
      AND Name NOT IN ('(untitled document)', '(unattributed quote)')
    GROUP BY EntityType, LOWER(Name)
    HAVING COUNT(*) > 1
) x
GROUP BY EntityType;
