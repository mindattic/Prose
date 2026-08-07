-- RFC 0006/0007: per-universe species. Characters get a nullable SpeciesId FK to the
-- universe-scoped Species row, so 'human' resolves to GLMZ-human vs Fantasy-human via the
-- character's own universe. Species already carries UniverseId; this links each character to
-- its universe's definition. Temporal OFF/ON dance (Characters is system-versioned).
SET QUOTED_IDENTIFIER ON;
GO
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Characters' AND COLUMN_NAME='SpeciesId')
BEGIN
    ALTER TABLE dbo.Characters SET (SYSTEM_VERSIONING = OFF);
    ALTER TABLE dbo.Characters ADD SpeciesId UNIQUEIDENTIFIER NULL;
    ALTER TABLE dbo.Characters_History ADD SpeciesId UNIQUEIDENTIFIER NULL;
    ALTER TABLE dbo.Characters SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.Characters_History));
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_Characters_Species')
    ALTER TABLE dbo.Characters WITH NOCHECK
        ADD CONSTRAINT FK_Characters_Species FOREIGN KEY (SpeciesId) REFERENCES dbo.Species(Id);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Characters_SpeciesId')
    CREATE INDEX IX_Characters_SpeciesId ON dbo.Characters(SpeciesId) WHERE SpeciesId IS NOT NULL;
GO
