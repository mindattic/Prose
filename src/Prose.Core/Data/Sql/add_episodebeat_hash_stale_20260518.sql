-- Per Legion 4/4 (high tier, confidence 8-9): close the writer-recording
-- desync risk. Add four columns to EpisodeBeats:
--
--   TextHash       — SHA-256 hex of the beat's prose at record time. Compared
--                    against the writer's ChapterBeat.Text on save; mismatch
--                    flips Stale=true and nulls AudioPath.
--   SourceBeatGuid — when this episode is a chapter recording, points at the
--                    ChapterBeat.Id (JSON-side string Guid) so we can match
--                    1:1 even after splits/merges.
--   Stale          — flagged when the source prose drifted past the recording.
--                    UI surfaces it; player can refuse to play stale beats.
--   LastRequestId  — the ElevenLabs request-id of the last successful synth.
--                    Lets a single re-recorded beat pull stitching context
--                    from its neighbours on next render (closes gap e).

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ARITHABORT ON;

IF COL_LENGTH('dbo.EpisodeBeats', 'TextHash')       IS NULL ALTER TABLE [dbo].[EpisodeBeats] ADD [TextHash]       NVARCHAR(64) NULL;
GO
IF COL_LENGTH('dbo.EpisodeBeats', 'SourceBeatGuid') IS NULL ALTER TABLE [dbo].[EpisodeBeats] ADD [SourceBeatGuid] NVARCHAR(64) NULL;
GO
IF COL_LENGTH('dbo.EpisodeBeats', 'Stale')          IS NULL ALTER TABLE [dbo].[EpisodeBeats] ADD [Stale]          BIT NOT NULL CONSTRAINT [DF_EpisodeBeats_Stale] DEFAULT 0;
GO
IF COL_LENGTH('dbo.EpisodeBeats', 'LastRequestId')  IS NULL ALTER TABLE [dbo].[EpisodeBeats] ADD [LastRequestId]  NVARCHAR(64) NULL;
GO

-- Index on SourceBeatGuid for the desync check's O(1) lookup of
-- "EpisodeBeat where SourceBeatGuid = <ChapterBeat.Id>".
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_EpisodeBeats_SourceBeatGuid' AND object_id = OBJECT_ID('dbo.EpisodeBeats'))
    CREATE INDEX [IX_EpisodeBeats_SourceBeatGuid] ON [dbo].[EpisodeBeats] ([SourceBeatGuid]) WHERE [SourceBeatGuid] IS NOT NULL;
GO
