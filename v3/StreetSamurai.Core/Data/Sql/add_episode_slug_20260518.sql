-- Slugified episode folders: engine/episodes/{slug}/ instead of /{id}/.
-- Slug is computed from the LLM-issued title at generation time. Collisions
-- with an existing slug get the integer id appended for uniqueness; most
-- slugs stay clean and human-readable.
--
-- Existing rows are backfilled to the integer-id form so they remain
-- locatable on disk for any partial test data.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ARITHABORT ON;

IF COL_LENGTH('dbo.Episodes', 'Slug') IS NULL
BEGIN
    ALTER TABLE [dbo].[Episodes] ADD [Slug] NVARCHAR(200) NOT NULL CONSTRAINT [DF_Episodes_Slug] DEFAULT N'';
END
GO

-- Backfill any blank slugs to the integer id (which is what previous code used
-- as a directory name) so existing artifacts on disk stay reachable.
UPDATE [dbo].[Episodes] SET [Slug] = CAST([Id] AS NVARCHAR(20))
WHERE [Slug] = N'' OR [Slug] IS NULL;
GO

-- Unique index after backfill so subsequent inserts must dedupe.
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Episodes_Slug' AND object_id = OBJECT_ID('dbo.Episodes'))
BEGIN
    CREATE UNIQUE INDEX [IX_Episodes_Slug] ON [dbo].[Episodes] ([Slug]);
END
GO
