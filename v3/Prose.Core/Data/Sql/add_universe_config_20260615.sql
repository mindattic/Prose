-- add_universe_config_20260615.sql
-- ───────────────────────────────────────────────────────────────────────────
-- RFC 0006 Step 1 — segregate world CONFIG by universe. Adds UniverseId to the
-- Settings table (composite key Key+UniverseId so the same key recurs per
-- universe) and the Species table. Existing rows backfill to GLMZ; the three
-- operational keys (action_configs, tts.rules, users.accounts) are tagged with
-- the SHARED sentinel (0197e9c9-0099-7000-8000-000000000099) so every universe sees the one copy.
--
-- Settings is system-versioned, so the column + PK change uses the
-- SYSTEM_VERSIONING OFF → ALTER → ON dance. Partial-state safe + idempotent.
-- Run under QUOTED_IDENTIFIER ON (EF default; sqlcmd -I).
-- ───────────────────────────────────────────────────────────────────────────

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ── 1. Settings.UniverseId + composite PK (Key, UniverseId) ─────────────────
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Settings' AND temporal_type = 2)
    ALTER TABLE dbo.Settings SET (SYSTEM_VERSIONING = OFF);
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Settings') AND name = 'UniverseId')
    ALTER TABLE dbo.Settings ADD UniverseId UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT DF_Settings_UniverseId DEFAULT '0197e9c9-0001-7000-8000-000000000001';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Settings_History') AND name = 'UniverseId')
    ALTER TABLE dbo.Settings_History ADD UniverseId UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT DF_SettingsH_UniverseId DEFAULT '0197e9c9-0001-7000-8000-000000000001';
GO
-- Tag the operational/shared keys so they're visible from every universe.
UPDATE dbo.Settings SET UniverseId = '0197e9c9-0099-7000-8000-000000000099'
    WHERE [Key] IN ('action_configs', 'tts.rules', 'users.accounts')
      AND UniverseId <> '0197e9c9-0099-7000-8000-000000000099';
GO
-- Swap the primary key from (Key) to (Key, UniverseId).
DECLARE @pk sysname = (SELECT kc.name FROM sys.key_constraints kc
                       WHERE kc.parent_object_id = OBJECT_ID('dbo.Settings') AND kc.type = 'PK');
IF @pk IS NOT NULL AND NOT EXISTS (
        SELECT 1 FROM sys.index_columns ic
        JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
        WHERE ic.object_id = OBJECT_ID('dbo.Settings') AND c.name = 'UniverseId'
          AND ic.index_id = (SELECT unique_index_id FROM sys.key_constraints WHERE name = @pk))
    EXEC('ALTER TABLE dbo.Settings DROP CONSTRAINT [' + @pk + ']');
GO
IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID('dbo.Settings') AND type = 'PK')
    ALTER TABLE dbo.Settings ADD CONSTRAINT PK_Settings PRIMARY KEY ([Key], UniverseId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Settings' AND temporal_type = 2)
    ALTER TABLE dbo.Settings SET (SYSTEM_VERSIONING = ON
        (HISTORY_TABLE = dbo.Settings_History, DATA_CONSISTENCY_CHECK = OFF));
GO

-- ── 2. Species.UniverseId (non-temporal) ───────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Species') AND name = 'UniverseId')
    ALTER TABLE dbo.Species ADD UniverseId UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT DF_Species_UniverseId DEFAULT '0197e9c9-0001-7000-8000-000000000001';
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Species_UniverseId' AND object_id = OBJECT_ID('dbo.Species'))
    CREATE INDEX IX_Species_UniverseId ON dbo.Species(UniverseId);
GO
-- Species.Name was globally unique; make it unique per universe instead.
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Species_Name' AND object_id = OBJECT_ID('dbo.Species'))
    DROP INDEX IX_Species_Name ON dbo.Species;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Species_Universe_Name' AND object_id = OBJECT_ID('dbo.Species'))
    CREATE UNIQUE INDEX UX_Species_Universe_Name ON dbo.Species(UniverseId, Name);
GO
