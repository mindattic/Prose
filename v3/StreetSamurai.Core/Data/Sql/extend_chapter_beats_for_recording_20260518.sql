-- ChapterBeats already exists relationally on this branch (44 rows on the
-- dev DB). It carries Title / Synopsis / StructureRole / Act / SceneType /
-- FacetTag — the narrative metadata is already in place.
--
-- This migration extends it with the columns needed for unified
-- recording-aware beats: SortKey for fractional ordering, audio path columns,
-- request-id for stitching, plus the two tone hints that didn't exist yet.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ARITHABORT ON;

IF COL_LENGTH('dbo.ChapterBeats', 'SortKey')        IS NULL ALTER TABLE [dbo].[ChapterBeats] ADD [SortKey]        FLOAT         NOT NULL CONSTRAINT [DF_ChapterBeats_SortKey] DEFAULT 0;
GO

-- Backfill SortKey = Index * 100 for the existing 44 rows so order is
-- preserved while leaving big gaps for fractional splits.
UPDATE [dbo].[ChapterBeats] SET [SortKey] = [Index] * 100.0 WHERE [SortKey] = 0;
GO

IF COL_LENGTH('dbo.ChapterBeats', 'EmotionalTone')  IS NULL ALTER TABLE [dbo].[ChapterBeats] ADD [EmotionalTone]  NVARCHAR(64)  NULL;
GO
IF COL_LENGTH('dbo.ChapterBeats', 'PaceHint')       IS NULL ALTER TABLE [dbo].[ChapterBeats] ADD [PaceHint]       NVARCHAR(64)  NULL;
GO

-- Recording state.
IF COL_LENGTH('dbo.ChapterBeats', 'AudioPath')      IS NULL ALTER TABLE [dbo].[ChapterBeats] ADD [AudioPath]      NVARCHAR(400) NULL;
GO
IF COL_LENGTH('dbo.ChapterBeats', 'DurationSec')    IS NULL ALTER TABLE [dbo].[ChapterBeats] ADD [DurationSec]    FLOAT         NULL;
GO
IF COL_LENGTH('dbo.ChapterBeats', 'NarratedAt')     IS NULL ALTER TABLE [dbo].[ChapterBeats] ADD [NarratedAt]     DATETIME2(7)  NULL;
GO
IF COL_LENGTH('dbo.ChapterBeats', 'LastRequestId')  IS NULL ALTER TABLE [dbo].[ChapterBeats] ADD [LastRequestId]  NVARCHAR(64)  NULL;
GO
IF COL_LENGTH('dbo.ChapterBeats', 'WasCorrected')   IS NULL ALTER TABLE [dbo].[ChapterBeats] ADD [WasCorrected]   BIT           NOT NULL CONSTRAINT [DF_ChapterBeats_WasCorrected] DEFAULT 0;
GO

-- Index for ordered chapter scans (the "read all beats in this chapter in
-- order" query the recording panel runs).
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_ChapterBeats_Chapter_SortKey' AND object_id = OBJECT_ID('dbo.ChapterBeats'))
    CREATE INDEX [IX_ChapterBeats_Chapter_SortKey] ON [dbo].[ChapterBeats] ([ChapterId], [SortKey]);
GO
