-- Fractional indexing for EpisodeBeats. Avoids O(N) renumbering on every split
-- or insertion, which would have been crippling on a 2000-beat chapter.
--
-- Design:
--   • [Index] stays as the stable identifier for a beat within its episode.
--     Audio paths embed it ({Index:D3}.wav), API URLs use it
--     (/api/episodes/{id}/audio/{index}), and once issued it never changes.
--   • [SortKey] is the new mutable order. UI sorts by SortKey ASC.
--   • On split: new beat's SortKey = (prev.SortKey + next.SortKey) / 2.
--     Zero updates to neighbours.
--   • On move: just update SortKey.
--   • [Index] of new beats is max(Index)+1 within the episode (still unique).
--
-- Backfill SortKey = [Index] * 100.0 so existing rows order identically while
-- leaving large gaps for splits between any two siblings (~50 splits between
-- same pair before double precision runs out).

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ARITHABORT ON;

IF COL_LENGTH('dbo.EpisodeBeats', 'SortKey') IS NULL
BEGIN
    ALTER TABLE [dbo].[EpisodeBeats]
        ADD [SortKey] FLOAT NOT NULL CONSTRAINT [DF_EpisodeBeats_SortKey] DEFAULT 0;
END
GO

-- Backfill: SortKey = Index * 100 for every existing row whose SortKey is at
-- the default zero. New rows on inserts after this point will be assigned
-- proper SortKey values by the application.
UPDATE [dbo].[EpisodeBeats]
SET    [SortKey] = [Index] * 100.0
WHERE  [SortKey] = 0;
GO

-- Index on (EpisodeId, SortKey) so the ordered read is a clean range scan.
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_EpisodeBeats_Episode_SortKey' AND object_id = OBJECT_ID('dbo.EpisodeBeats'))
BEGIN
    CREATE INDEX [IX_EpisodeBeats_Episode_SortKey]
        ON [dbo].[EpisodeBeats] ([EpisodeId], [SortKey]);
END
GO
