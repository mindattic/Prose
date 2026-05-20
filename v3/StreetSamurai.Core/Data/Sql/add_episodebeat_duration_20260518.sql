-- Add EpisodeBeats.DurationSec for the /listen player's progress bar.
-- Actual narration duration in seconds, computed from the WAV data-chunk size
-- divided by the byte rate (88200 for 16-bit mono 44.1 kHz). Null until the
-- TTS pipeline finishes for that beat; the UI falls back to a text-length
-- estimate (~15 chars/sec) while waiting.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ARITHABORT ON;

IF COL_LENGTH('dbo.EpisodeBeats', 'DurationSec') IS NULL
BEGIN
    ALTER TABLE [dbo].[EpisodeBeats]
        ADD [DurationSec] FLOAT NULL;
END
GO
