-- Tag Episodes with their source Book + Chapter so chapter recordings reuse
-- the entire Episode pipeline (beats, audio, stitching, edits, re-record,
-- combined export). The /recordings hierarchy page groups by these columns.
--
-- BookId points at Books.Id (Guid). ChapterId stays as NVARCHAR — the Chapter
-- entity uses a string Id format on this branch. NULL on both = stand-alone
-- bedtime episode (existing flow).

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ARITHABORT ON;

IF COL_LENGTH('dbo.Episodes', 'BookId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Episodes] ADD [BookId] UNIQUEIDENTIFIER NULL;
END
GO

IF COL_LENGTH('dbo.Episodes', 'ChapterId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Episodes] ADD [ChapterId] NVARCHAR(64) NULL;
END
GO

-- Index for the hierarchy page's "all recordings for this book" query.
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Episodes_BookId' AND object_id = OBJECT_ID('dbo.Episodes'))
BEGIN
    CREATE INDEX [IX_Episodes_BookId] ON [dbo].[Episodes] ([BookId])
        WHERE [BookId] IS NOT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Episodes_ChapterId' AND object_id = OBJECT_ID('dbo.Episodes'))
BEGIN
    CREATE INDEX [IX_Episodes_ChapterId] ON [dbo].[Episodes] ([ChapterId])
        WHERE [ChapterId] IS NOT NULL;
END
GO
