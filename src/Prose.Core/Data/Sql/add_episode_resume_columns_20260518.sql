-- Resume + Continue support on /listen.
--   LastPlayedBeatIndex / LastPlayedSec — playback position, written from the
--     listen page so Resume picks up where the user fell asleep.
--   ParentEpisodeId — when an episode was generated via "Continue this story"
--     this points back at the source. Lets us reconstruct threads later.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ARITHABORT ON;

IF COL_LENGTH('dbo.Episodes', 'LastPlayedBeatIndex') IS NULL
BEGIN
    ALTER TABLE [dbo].[Episodes] ADD [LastPlayedBeatIndex] INT NULL;
END
GO

IF COL_LENGTH('dbo.Episodes', 'LastPlayedSec') IS NULL
BEGIN
    ALTER TABLE [dbo].[Episodes] ADD [LastPlayedSec] FLOAT NULL;
END
GO

IF COL_LENGTH('dbo.Episodes', 'ParentEpisodeId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Episodes] ADD [ParentEpisodeId] UNIQUEIDENTIFIER NULL;

    -- Self-FK with NO ACTION (don't cascade — deleting a parent shouldn't take
    -- the thread out). Index for quick "show me everything that branched from X".
    ALTER TABLE [dbo].[Episodes]
        ADD CONSTRAINT [FK_Episodes_Parent]
        FOREIGN KEY ([ParentEpisodeId]) REFERENCES [dbo].[Episodes]([Id]);

    CREATE INDEX [IX_Episodes_ParentEpisodeId] ON [dbo].[Episodes] ([ParentEpisodeId]);
END
GO
