-- add_universe_graph_20260615.sql
-- ───────────────────────────────────────────────────────────────────────────
-- RFC 0006 Step 5 — defense-in-depth universe scoping for the graph/ledger
-- tables that are reachable by direct DbSet scans (not only via a scoped entity
-- id): Edges (typed relations), EntityStateEvents (story-state ledger), and
-- CharacterReadModels (the materialized projection). Each gets a denormalized
-- UniverseId (from its parent entity). Every existing row's parent is GLMZ, so
-- NOT NULL DEFAULT GLMZ is a correct backfill. Edges + EntityStateEvents are
-- system-versioned (dance); CharacterReadModels is not. Partial-state safe.
-- Run under QUOTED_IDENTIFIER ON (EF default; sqlcmd -I).
-- ───────────────────────────────────────────────────────────────────────────

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ── Edges (temporal) ────────────────────────────────────────────────────────
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Edges' AND temporal_type = 2)
    ALTER TABLE dbo.Edges SET (SYSTEM_VERSIONING = OFF);
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Edges') AND name = 'UniverseId')
    ALTER TABLE dbo.Edges ADD UniverseId UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT DF_Edges_UniverseId DEFAULT '0197e9c9-0001-7000-8000-000000000001';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Edges_History') AND name = 'UniverseId')
    ALTER TABLE dbo.Edges_History ADD UniverseId UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT DF_EdgesH_UniverseId DEFAULT '0197e9c9-0001-7000-8000-000000000001';
GO
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Edges' AND temporal_type = 2)
    ALTER TABLE dbo.Edges SET (SYSTEM_VERSIONING = ON
        (HISTORY_TABLE = dbo.Edges_History, DATA_CONSISTENCY_CHECK = OFF));
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Edges_UniverseId' AND object_id = OBJECT_ID('dbo.Edges'))
    CREATE INDEX IX_Edges_UniverseId ON dbo.Edges(UniverseId);
GO

-- ── EntityStateEvents (temporal) ──────────────────────────────────────────────
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EntityStateEvents' AND temporal_type = 2)
    ALTER TABLE dbo.EntityStateEvents SET (SYSTEM_VERSIONING = OFF);
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.EntityStateEvents') AND name = 'UniverseId')
    ALTER TABLE dbo.EntityStateEvents ADD UniverseId UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT DF_EntityStateEvents_UniverseId DEFAULT '0197e9c9-0001-7000-8000-000000000001';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.EntityStateEvents_History') AND name = 'UniverseId')
    ALTER TABLE dbo.EntityStateEvents_History ADD UniverseId UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT DF_EntityStateEventsH_UniverseId DEFAULT '0197e9c9-0001-7000-8000-000000000001';
GO
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EntityStateEvents' AND temporal_type = 2)
    ALTER TABLE dbo.EntityStateEvents SET (SYSTEM_VERSIONING = ON
        (HISTORY_TABLE = dbo.EntityStateEvents_History, DATA_CONSISTENCY_CHECK = OFF));
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EntityStateEvents_UniverseId' AND object_id = OBJECT_ID('dbo.EntityStateEvents'))
    CREATE INDEX IX_EntityStateEvents_UniverseId ON dbo.EntityStateEvents(UniverseId);
GO

-- ── CharacterReadModels (non-temporal) ────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.CharacterReadModels') AND name = 'UniverseId')
    ALTER TABLE dbo.CharacterReadModels ADD UniverseId UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT DF_CharacterReadModels_UniverseId DEFAULT '0197e9c9-0001-7000-8000-000000000001';
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CharacterReadModels_UniverseId' AND object_id = OBJECT_ID('dbo.CharacterReadModels'))
    CREATE INDEX IX_CharacterReadModels_UniverseId ON dbo.CharacterReadModels(UniverseId);
GO
