-- add_beat_entity_stale_20260618.sql
-- ───────────────────────────────────────────────────────────────────────────
-- Adds Beat.EntityStale (bool) + BeatEntityMentions junction table.
--
-- Beat.EntityStale = true means an entity this beat mentions was updated
-- after the beat was written. Audio staleness (Beat.Stale) is separate.
-- EntityRamificationService sets this flag; the author clears it after review.
--
-- BeatEntityMentions tracks which entity names appear in which beats,
-- so entity-update propagation is O(mentions) rather than O(all beats).
--
-- Pattern: SYSTEM_VERSIONING OFF → ALTER + _History → ON
-- Idempotent. Run under QUOTED_IDENTIFIER ON (sqlcmd -I).
-- ───────────────────────────────────────────────────────────────────────────

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ── Beat.EntityStale (temporal table) ───────────────────────────────────────

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Beats' AND temporal_type = 2)
    ALTER TABLE dbo.Beats SET (SYSTEM_VERSIONING = OFF);
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Beats') AND name = 'EntityStale')
    ALTER TABLE dbo.Beats ADD EntityStale bit NOT NULL DEFAULT 0;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Beats_History') AND name = 'EntityStale')
    ALTER TABLE dbo.Beats_History ADD EntityStale bit NOT NULL DEFAULT 0;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Beats' AND temporal_type = 2)
    ALTER TABLE dbo.Beats SET (SYSTEM_VERSIONING = ON
        (HISTORY_TABLE = dbo.Beats_History, DATA_CONSISTENCY_CHECK = OFF));
GO

-- ── BeatEntityMentions junction ──────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'BeatEntityMentions')
BEGIN
    CREATE TABLE dbo.BeatEntityMentions (
        BeatId     uniqueidentifier NOT NULL,
        EntityId   uniqueidentifier NOT NULL,
        EntityName nvarchar(200)    NOT NULL,
        EntityType nvarchar(50)     NOT NULL DEFAULT '',
        CreatedAt  datetime2        NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_BeatEntityMentions PRIMARY KEY (BeatId, EntityId),
        CONSTRAINT FK_BeatEntityMentions_Beat
            FOREIGN KEY (BeatId) REFERENCES dbo.Beats(Id) ON DELETE CASCADE,
        CONSTRAINT FK_BeatEntityMentions_Entity
            FOREIGN KEY (EntityId) REFERENCES dbo.Entities(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_BeatEntityMentions_EntityId ON dbo.BeatEntityMentions(EntityId);
END
GO
