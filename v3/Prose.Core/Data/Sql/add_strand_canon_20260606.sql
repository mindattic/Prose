-- Author-only Canon flag on Strands (the gold-standard marker). Adding a
-- defaulted/nullable column to the system-versioned Strands table cascades to
-- Strands_History automatically. Re-runnable (guarded by COL_LENGTH).

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF COL_LENGTH('dbo.Strands','IsCanon') IS NULL
    ALTER TABLE dbo.Strands ADD IsCanon BIT NOT NULL CONSTRAINT DF_Strands_IsCanon DEFAULT 0;

IF COL_LENGTH('dbo.Strands','CanonAt') IS NULL
    ALTER TABLE dbo.Strands ADD CanonAt DATETIME2 NULL;
