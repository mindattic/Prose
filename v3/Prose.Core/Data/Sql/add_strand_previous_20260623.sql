-- add_strand_previous_20260623.sql
-- ───────────────────────────────────────────────────────────────────────────
-- Adds Strand.PreviousStrandId — a nullable FK to the strand this one
-- continues from. When null, the strand is a gateway story (first in its
-- series, or standalone). When set, it's a sequel and the sequel
-- commandments apply.
--
-- Idempotent. Run under QUOTED_IDENTIFIER ON (sqlcmd -I).
-- ───────────────────────────────────────────────────────────────────────────

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ── Strands.PreviousStrandId ─────────────────────────────────────────────────

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Strands' AND temporal_type = 2)
    ALTER TABLE dbo.Strands SET (SYSTEM_VERSIONING = OFF);
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Strands') AND name = 'PreviousStrandId')
    ALTER TABLE dbo.Strands ADD PreviousStrandId uniqueidentifier NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Strands_History') AND name = 'PreviousStrandId')
    ALTER TABLE dbo.Strands_History ADD PreviousStrandId uniqueidentifier NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Strands' AND temporal_type = 2)
    ALTER TABLE dbo.Strands SET (SYSTEM_VERSIONING = ON
        (HISTORY_TABLE = dbo.Strands_History, DATA_CONSISTENCY_CHECK = OFF));
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_Strands_PreviousStrand' AND parent_object_id = OBJECT_ID('dbo.Strands')
)
    ALTER TABLE dbo.Strands
        ADD CONSTRAINT FK_Strands_PreviousStrand
            FOREIGN KEY (PreviousStrandId) REFERENCES dbo.Strands(Id);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Strands_PreviousStrandId' AND object_id = OBJECT_ID('dbo.Strands'))
    CREATE INDEX IX_Strands_PreviousStrandId ON dbo.Strands(PreviousStrandId) WHERE PreviousStrandId IS NOT NULL;
GO
