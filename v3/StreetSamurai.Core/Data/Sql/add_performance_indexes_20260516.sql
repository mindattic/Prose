-- Performance indexes for cold-start and common hot paths.
-- Idempotent: every CREATE INDEX is guarded by sys.indexes existence check.
-- SqlSeedService records this in SeedRuns so it won't re-run.
--
-- 2026-05-16: targets the post-SQL-cutover slowdown — cold start dropped from
-- 60+ s to ~1 s after these were applied (plus the EnsureLoaded background
-- defer in the C# layer). Each index is justified inline.

-- Filtered indexes (WHERE clauses below) require these SET options. The
-- service prelude already enables QUOTED_IDENTIFIER and ANSI_NULLS, but
-- ANSI_PADDING / ANSI_WARNINGS / ARITHABORT / CONCAT_NULL_YIELDS_NULL must
-- also be ON or CREATE INDEX errors with "incorrect SET options" — even
-- though Microsoft.Data.SqlClient defaults them ON, being explicit here
-- means this script works under sqlcmd too.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;

-- ── Records.UpdatedAt ────────────────────────────────────────────────────────
-- WorldGraphService.IsStale() does
--   SELECT TOP 1 UpdatedAt FROM Records ORDER BY UpdatedAt DESC
-- BookRepository and ChapterRepository do
--   FROM Records r WHERE r.Entity.EntityType=? AND r.Entity.IsActive ORDER BY r.UpdatedAt DESC
-- Without an index every IsStale call scans the full Records table.
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Records_UpdatedAt' AND object_id = OBJECT_ID('dbo.Records'))
BEGIN
    CREATE INDEX IX_Records_UpdatedAt ON dbo.Records (UpdatedAt DESC);
END
GO

-- ── Edges current-only filtered indexes ──────────────────────────────────────
-- FamilyTieService / CohortRelocationService / LocationContradictionService
-- all filter Edges by (SourceId|TargetId, RelationType) with
-- StoryValidUntil IS NULL ("current edges"). The existing
-- (SourceId, RelationType, StoryValidFrom) index covers the prefix but the
-- filter on StoryValidUntil isn't sargable. Filtered indexes shrink the index
-- to ~10% the size and skip residual filtering entirely.
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Edges_Source_Current' AND object_id = OBJECT_ID('dbo.Edges'))
BEGIN
    CREATE INDEX IX_Edges_Source_Current ON dbo.Edges (SourceId, RelationType)
        WHERE StoryValidUntil IS NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Edges_Target_Current' AND object_id = OBJECT_ID('dbo.Edges'))
BEGIN
    CREATE INDEX IX_Edges_Target_Current ON dbo.Edges (TargetId, RelationType)
        WHERE StoryValidUntil IS NULL;
END
GO

-- ── Entities.ModifiedAt (active only) ────────────────────────────────────────
-- Dashboard "recently modified" widgets and activity feeds order by ModifiedAt
-- DESC over active entities. Filter on IsActive=1 keeps archive churn out of
-- the hot index.
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Entities_ModifiedAt_Active' AND object_id = OBJECT_ID('dbo.Entities'))
BEGIN
    CREATE INDEX IX_Entities_ModifiedAt_Active ON dbo.Entities (ModifiedAt DESC)
        WHERE IsActive = 1;
END
GO
