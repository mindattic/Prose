-- Narrative metadata for every beat: what is this paragraph *accomplishing*.
-- Story-craft fields land on EpisodeBeats now (Step 1 of the architecture
-- consolidation). Step 2 will create a parallel ChapterBeats table with the
-- same shape, then Step 3 will unify both into one Beats table.
--
-- All new columns are nullable / default-zero so existing rows stay valid.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ARITHABORT ON;

IF COL_LENGTH('dbo.EpisodeBeats', 'BeatTitle')     IS NULL ALTER TABLE [dbo].[EpisodeBeats] ADD [BeatTitle]     NVARCHAR(200) NULL;
IF COL_LENGTH('dbo.EpisodeBeats', 'Synopsis')      IS NULL ALTER TABLE [dbo].[EpisodeBeats] ADD [Synopsis]      NVARCHAR(500) NULL;
IF COL_LENGTH('dbo.EpisodeBeats', 'StructureRole') IS NULL ALTER TABLE [dbo].[EpisodeBeats] ADD [StructureRole] NVARCHAR(64)  NULL;
IF COL_LENGTH('dbo.EpisodeBeats', 'Act')           IS NULL ALTER TABLE [dbo].[EpisodeBeats] ADD [Act]           INT           NOT NULL CONSTRAINT [DF_EpisodeBeats_Act] DEFAULT 0;
IF COL_LENGTH('dbo.EpisodeBeats', 'SceneType')     IS NULL ALTER TABLE [dbo].[EpisodeBeats] ADD [SceneType]     NVARCHAR(32)  NOT NULL CONSTRAINT [DF_EpisodeBeats_SceneType] DEFAULT N'scene';
IF COL_LENGTH('dbo.EpisodeBeats', 'FacetTag')      IS NULL ALTER TABLE [dbo].[EpisodeBeats] ADD [FacetTag]      NVARCHAR(32)  NULL;
IF COL_LENGTH('dbo.EpisodeBeats', 'EmotionalTone') IS NULL ALTER TABLE [dbo].[EpisodeBeats] ADD [EmotionalTone] NVARCHAR(64)  NULL;
IF COL_LENGTH('dbo.EpisodeBeats', 'PaceHint')      IS NULL ALTER TABLE [dbo].[EpisodeBeats] ADD [PaceHint]      NVARCHAR(64)  NULL;
GO

-- Index for queries like "find every climax beat in this book" (used by
-- future structure-aware editors).
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_EpisodeBeats_StructureRole' AND object_id = OBJECT_ID('dbo.EpisodeBeats'))
BEGIN
    CREATE INDEX [IX_EpisodeBeats_StructureRole]
        ON [dbo].[EpisodeBeats] ([StructureRole])
        WHERE [StructureRole] IS NOT NULL;
END
GO
