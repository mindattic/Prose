-- add_faction_relationship_tags_20260615.sql
-- ───────────────────────────────────────────────────────────────────────────
-- RFC 0007 — Faction relational schema, step 1:
-- FactionRelationshipTags bridges FactionRelationshipRow.Id → per-tag rows so
-- that FactionRelationship.Tags (List<string>, ~178 values across all factions)
-- can be persisted relationally. The universal EntityTags table is keyed to an
-- Entity.Id and cannot address a bridge row; this per-relationship tag table
-- mirrors the same shape.
--
-- System-versioned like every other Faction bridge table (see SystemVersionedTables
-- in StreetSamuraiDbContext). Idempotent IF NOT EXISTS guards. Run under
-- QUOTED_IDENTIFIER ON (EF default; sqlcmd -I).
-- ───────────────────────────────────────────────────────────────────────────

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ── Create base table ────────────────────────────────────────────────────────
IF OBJECT_ID(N'[dbo].[FactionRelationshipTags]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[FactionRelationshipTags] (
        [Id]                      BIGINT        IDENTITY(1,1) NOT NULL CONSTRAINT [PK_FactionRelationshipTags] PRIMARY KEY,
        [FactionRelationshipRowId] BIGINT        NOT NULL,
        [Position]                INT           NOT NULL DEFAULT 0,
        [Value]                   NVARCHAR(450) NOT NULL DEFAULT ''
    );
END;
GO

-- ── FK to FactionRelationships ───────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_FactionRelationshipTags_FactionRelationships_FactionRelationshipRowId'
)
    ALTER TABLE [dbo].[FactionRelationshipTags]
        ADD CONSTRAINT [FK_FactionRelationshipTags_FactionRelationships_FactionRelationshipRowId]
        FOREIGN KEY ([FactionRelationshipRowId])
        REFERENCES [dbo].[FactionRelationships] ([Id])
        ON DELETE CASCADE;
GO

-- ── Indexes ──────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FactionRelationshipTags_FactionRelationshipRowId_Position' AND object_id = OBJECT_ID(N'[dbo].[FactionRelationshipTags]'))
    CREATE INDEX [IX_FactionRelationshipTags_FactionRelationshipRowId_Position]
        ON [dbo].[FactionRelationshipTags] ([FactionRelationshipRowId], [Position]);
GO
