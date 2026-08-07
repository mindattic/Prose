-- add_universe_embeddings_20260615.sql
-- ───────────────────────────────────────────────────────────────────────────
-- RFC 0006 Step 2 — close the silent embedding leak. EntityEmbeddings and
-- ProseEmbeddings are searched via raw VECTOR_DISTANCE SQL that bypasses the EF
-- query filter, so "find similar canon/prose" returned hits from ALL universes.
-- Denormalize a UniverseId onto both tables (stamped at embed time, backfilled
-- to GLMZ) and the FindSimilar* queries now filter on it. Both tables are
-- non-temporal, so the NOT NULL DEFAULT backfills every existing row to GLMZ.
-- Idempotent.
-- ───────────────────────────────────────────────────────────────────────────

SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.EntityEmbeddings','U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.EntityEmbeddings') AND name = 'UniverseId')
    ALTER TABLE dbo.EntityEmbeddings ADD UniverseId UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT DF_EntityEmbeddings_UniverseId DEFAULT '0197e9c9-0001-7000-8000-000000000001';
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EntityEmbeddings_UniverseId' AND object_id = OBJECT_ID('dbo.EntityEmbeddings'))
    CREATE INDEX IX_EntityEmbeddings_UniverseId ON dbo.EntityEmbeddings(UniverseId);
GO

IF OBJECT_ID('dbo.ProseEmbeddings','U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ProseEmbeddings') AND name = 'UniverseId')
    ALTER TABLE dbo.ProseEmbeddings ADD UniverseId UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT DF_ProseEmbeddings_UniverseId DEFAULT '0197e9c9-0001-7000-8000-000000000001';
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProseEmbeddings_UniverseId' AND object_id = OBJECT_ID('dbo.ProseEmbeddings'))
    CREATE INDEX IX_ProseEmbeddings_UniverseId ON dbo.ProseEmbeddings(UniverseId);
GO
