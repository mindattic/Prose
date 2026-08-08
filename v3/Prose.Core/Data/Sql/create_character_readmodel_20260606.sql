-- CharacterReadModels: materialized read-model projection (CQRS-lite).
-- Caches the expensive CharacterMapper.Materialize output (the 25-bridge
-- fan-out, ~50-80s cold over 1200+ characters) as one JSON blob per character
-- so bulk full reads become a single indexed column read. DERIVED, not canon —
-- the relational Character row + bridges remain the source of truth.
--
-- Deliberately NOT system-versioned: refreshing this on every write must not
-- bloat the temporal history of the canonical Characters table. Backfill /
-- prune via `prose --rebuild-readmodel`. Re-runnable.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF OBJECT_ID(N'[dbo].[CharacterReadModels]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CharacterReadModels] (
        [CharacterId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_CharacterReadModels] PRIMARY KEY,
        [Json]        NVARCHAR(MAX)    NOT NULL,
        [Version]     INT              NOT NULL DEFAULT 0,
        [RefreshedAt] DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CharacterReadModels_Version' AND object_id = OBJECT_ID(N'[dbo].[CharacterReadModels]'))
    CREATE INDEX [IX_CharacterReadModels_Version] ON [dbo].[CharacterReadModels]([Version]);
