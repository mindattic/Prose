SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
-- ============================================================================
-- CorpoNation rename: "Corponation" → "CorpoNation" across all prose fields
--
-- WHAT THIS TOUCHES:
--   Entities.Name, Entities.Description
--   Beats.Text
--   Strands.Synopsis
--   Corponations prose fields (FoundingStory, FullText, KeyDetail, RelationshipToBig20, SecurityForce)
--   ChapterBeats.Text, ChapterBeats.Synopsis
--   Chapters.Synopsis
--
-- WHAT THIS DOES NOT TOUCH:
--   Entities.EntityType       ('corponation' type discriminator — code contract, unchanged)
--   Entities.Slug             (URL keys)
--   Any column used as a code identifier
--
-- Safe to run multiple times (REPLACE is idempotent once correct).
-- Assumptions: SQL_Latin1_General_CP1_CI_AS collation (default); REPLACE is
--   case-insensitive for matching and outputs the exact replacement string.
-- ============================================================================

BEGIN TRANSACTION;

-- ── Entities ─────────────────────────────────────────────────────────────────

UPDATE Entities
SET Name = REPLACE(REPLACE(Name, 'Corponations', 'CorpoNations'), 'Corponation', 'CorpoNation')
WHERE Name LIKE '%orponation%'
  AND Name NOT LIKE '%CorpoNation%';

UPDATE Entities
SET [Description] = REPLACE(REPLACE([Description], 'Corponations', 'CorpoNations'), 'Corponation', 'CorpoNation')
WHERE [Description] LIKE '%orponation%';

-- ── Beats ────────────────────────────────────────────────────────────────────

UPDATE Beats
SET [Text] = REPLACE(REPLACE([Text], 'Corponations', 'CorpoNations'), 'Corponation', 'CorpoNation')
WHERE [Text] LIKE '%orponation%';

-- ── Strands ──────────────────────────────────────────────────────────────────

UPDATE Strands
SET Title = REPLACE(REPLACE(Title, 'Corponations', 'CorpoNations'), 'Corponation', 'CorpoNation')
WHERE Title LIKE '%orponation%';

UPDATE Strands
SET Synopsis = REPLACE(REPLACE(Synopsis, 'Corponations', 'CorpoNations'), 'Corponation', 'CorpoNation')
WHERE Synopsis LIKE '%orponation%';

-- ── Corponations prose fields ─────────────────────────────────────────────────

UPDATE Corponations
SET
    FoundingStory       = REPLACE(REPLACE(FoundingStory,       'Corponations', 'CorpoNations'), 'Corponation', 'CorpoNation'),
    FullText            = REPLACE(REPLACE(FullText,            'Corponations', 'CorpoNations'), 'Corponation', 'CorpoNation'),
    KeyDetail           = REPLACE(REPLACE(KeyDetail,           'Corponations', 'CorpoNations'), 'Corponation', 'CorpoNation'),
    RelationshipToBig20 = REPLACE(REPLACE(RelationshipToBig20, 'Corponations', 'CorpoNations'), 'Corponation', 'CorpoNation'),
    SecurityForce       = REPLACE(REPLACE(SecurityForce,       'Corponations', 'CorpoNations'), 'Corponation', 'CorpoNation')
WHERE
    FoundingStory       LIKE '%orponation%'
    OR FullText         LIKE '%orponation%'
    OR KeyDetail        LIKE '%orponation%'
    OR RelationshipToBig20 LIKE '%orponation%'
    OR SecurityForce    LIKE '%orponation%';

-- ── ChapterBeats (legacy) ─────────────────────────────────────────────────────

UPDATE ChapterBeats
SET
    [Text]    = REPLACE(REPLACE([Text],    'Corponations', 'CorpoNations'), 'Corponation', 'CorpoNation'),
    Synopsis  = REPLACE(REPLACE(Synopsis,  'Corponations', 'CorpoNations'), 'Corponation', 'CorpoNation')
WHERE [Text] LIKE '%orponation%' OR Synopsis LIKE '%orponation%';

-- ── Chapters (legacy) ────────────────────────────────────────────────────────

UPDATE Chapters
SET Synopsis = REPLACE(REPLACE(Synopsis, 'Corponations', 'CorpoNations'), 'Corponation', 'CorpoNation')
WHERE Synopsis LIKE '%orponation%';

-- ── Definition entity: rename and annotate grammar rule ───────────────────────
-- Updates any entity whose name IS the concept (e.g. "Corponation" as a world doc/concept).
-- If no such entity exists this is a no-op; add one manually via the UI.

UPDATE Entities
SET
    Name = 'CorpoNation',
    [Description] = ISNULL([Description], '') +
        CASE WHEN [Description] NOT LIKE '%proper noun%'
             THEN CHAR(13)+CHAR(10)+CHAR(13)+CHAR(10) +
                  '[Grammar] CorpoNation is a proper noun always written with conjoined capitals: ' +
                  'CorpoNation (singular), CorpoNations (plural). ' +
                  'Example: "All CorpoNations, as sovereign states, maintain a well-organized militia."'
             ELSE ''
        END
WHERE LOWER(Name) = 'corponation'
  AND EntityType IN ('concept', 'document', 'worldbuilding', 'faction', 'organization');

COMMIT;

-- ── Verification counts ───────────────────────────────────────────────────────
SELECT 'Entities.Name remaining'        AS [check], COUNT(*) AS hits FROM Entities        WHERE Name        LIKE '%Corponation%' AND Name        NOT LIKE '%CorpoNation%';
SELECT 'Entities.Description remaining' AS [check], COUNT(*) AS hits FROM Entities        WHERE [Description] LIKE '%Corponation%' AND [Description] NOT LIKE '%CorpoNation%';
SELECT 'Beats.Text remaining'           AS [check], COUNT(*) AS hits FROM Beats            WHERE [Text]      LIKE '%Corponation%' AND [Text]        NOT LIKE '%CorpoNation%';
SELECT 'Strands.Synopsis remaining'     AS [check], COUNT(*) AS hits FROM Strands          WHERE Synopsis    LIKE '%Corponation%' AND Synopsis      NOT LIKE '%CorpoNation%';
