-- Fold the Gaps table into Beats. The gap that follows a beat is now a
-- property of the beat itself (Beats.GapAfterMs / GapAfterAudioPath). The
-- standalone Gaps table goes away. Lazy semantics preserved: GapAfterMs IS
-- NULL means "use the computed default from SceneType + terminator
-- punctuation"; an explicit value (including 0) is an override.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ARITHABORT ON;

-- 1. Add the two new columns to Beats. Both nullable: null = "no override".
IF COL_LENGTH('dbo.Beats', 'GapAfterMs') IS NULL
BEGIN
    ALTER TABLE [dbo].[Beats] ADD [GapAfterMs] INT NULL;
END
GO

IF COL_LENGTH('dbo.Beats', 'GapAfterAudioPath') IS NULL
BEGIN
    ALTER TABLE [dbo].[Beats] ADD [GapAfterAudioPath] NVARCHAR(400) NULL;
END
GO

-- 2. Backfill from Gaps (if it exists). Each Gap row anchored by
--    AboveBeatId becomes Beats.GapAfterMs on that beat. Only copy when the
--    target beat doesn't already have a value (idempotent re-runs).
IF OBJECT_ID('dbo.Gaps', 'U') IS NOT NULL
BEGIN
    UPDATE b
        SET b.GapAfterMs        = g.DurationMs,
            b.GapAfterAudioPath = g.AudioPath
        FROM dbo.Beats b
        INNER JOIN dbo.Gaps g ON g.AboveBeatId = b.Id
        WHERE b.GapAfterMs IS NULL;
END
GO

-- 3. Drop the Gaps table and its FKs. Done last so the backfill above runs
--    even if a prior partial run left columns added but Gaps still present.
IF OBJECT_ID('dbo.Gaps', 'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Gaps_AboveBeat')
        ALTER TABLE [dbo].[Gaps] DROP CONSTRAINT [FK_Gaps_AboveBeat];
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Gaps_BelowBeat')
        ALTER TABLE [dbo].[Gaps] DROP CONSTRAINT [FK_Gaps_BelowBeat];

    DROP TABLE [dbo].[Gaps];
END
GO
