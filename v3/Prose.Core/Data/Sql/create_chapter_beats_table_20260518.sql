-- Step 2 — Promote ChapterBeat from JSON-embedded inside Chapter.Records.Json
-- to a proper relational table mirroring the EpisodeBeats shape. Same columns,
-- same fractional SortKey, same audio fields, same narrative metadata. From
-- now on the writer and the recording panel can talk to the same shape.
--
-- The migration here CREATES the table; one-time backfill from JSON happens in
-- ChapterBeatMigrationService on the next host startup so we don't need to
-- write SQL JSON-parsing logic.
--
-- ChapterId stays as NVARCHAR(64) to match the existing Chapter.Id format on
-- this branch. EpisodeId stays nullable so this table can later coalesce with
-- EpisodeBeats (Step 3).

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ARITHABORT ON;

IF OBJECT_ID('dbo.ChapterBeats', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ChapterBeats] (
        [Id]              INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_ChapterBeats] PRIMARY KEY,

        [ChapterId]       NVARCHAR(64)      NOT NULL,
        [BookId]          UNIQUEIDENTIFIER  NULL,

        -- Identity / order
        [BeatId]          NVARCHAR(64)      NOT NULL,  -- the JSON-id we carried before (stable)
        [SortKey]         FLOAT             NOT NULL,
        [Index]           INT               NOT NULL,

        -- Prose
        [Text]            NVARCHAR(MAX)     NOT NULL DEFAULT N'',

        -- Narrative metadata
        [BeatTitle]       NVARCHAR(200)     NULL,
        [Synopsis]        NVARCHAR(500)     NULL,
        [StructureRole]   NVARCHAR(64)      NULL,
        [Act]             INT               NOT NULL DEFAULT 0,
        [SceneType]       NVARCHAR(32)      NOT NULL DEFAULT N'scene',
        [FacetTag]        NVARCHAR(32)      NULL,
        [EmotionalTone]   NVARCHAR(64)      NULL,
        [PaceHint]        NVARCHAR(64)      NULL,

        -- Recording state (mirrors EpisodeBeats; same Step 3 unification target)
        [AudioPath]       NVARCHAR(400)     NULL,
        [DurationSec]     FLOAT             NULL,
        [GeneratedAt]     DATETIME2(7)      NOT NULL DEFAULT SYSUTCDATETIME(),
        [NarratedAt]      DATETIME2(7)      NULL,
        [LastRequestId]   NVARCHAR(64)      NULL,
        [WasCorrected]    BIT               NOT NULL DEFAULT 0
    );
END
GO

IF OBJECT_ID('dbo.ChapterBeats', 'U') IS NOT NULL AND NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = 'IX_ChapterBeats_Chapter_BeatId' AND object_id = OBJECT_ID('dbo.ChapterBeats'))
    CREATE UNIQUE INDEX [IX_ChapterBeats_Chapter_BeatId] ON [dbo].[ChapterBeats] ([ChapterId], [BeatId]);
GO
IF OBJECT_ID('dbo.ChapterBeats', 'U') IS NOT NULL AND NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = 'IX_ChapterBeats_Chapter_SortKey' AND object_id = OBJECT_ID('dbo.ChapterBeats'))
    CREATE INDEX [IX_ChapterBeats_Chapter_SortKey] ON [dbo].[ChapterBeats] ([ChapterId], [SortKey]);
GO
IF OBJECT_ID('dbo.ChapterBeats', 'U') IS NOT NULL AND NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = 'IX_ChapterBeats_Book' AND object_id = OBJECT_ID('dbo.ChapterBeats'))
    CREATE INDEX [IX_ChapterBeats_Book] ON [dbo].[ChapterBeats] ([BookId]) WHERE [BookId] IS NOT NULL;
GO
IF OBJECT_ID('dbo.ChapterBeats', 'U') IS NOT NULL AND NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = 'IX_ChapterBeats_StructureRole' AND object_id = OBJECT_ID('dbo.ChapterBeats'))
    CREATE INDEX [IX_ChapterBeats_StructureRole] ON [dbo].[ChapterBeats] ([StructureRole]) WHERE [StructureRole] IS NOT NULL;
GO
