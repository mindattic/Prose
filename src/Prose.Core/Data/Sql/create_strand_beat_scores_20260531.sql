-- Per-beat micro-scores for segment studies: every reviewer rates every beat
-- (1 = hurt the story, 3 = fine, 5 = highlight), producing a reviewer x beat
-- matrix that drives emergent clustering + per-beat Pareto/contested analysis.
-- Plus cluster-assignment columns on StrandReviews (set during post-run analysis).
-- Re-runnable via OBJECT_ID / COL_LENGTH guards.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ARITHABORT ON;

IF OBJECT_ID(N'[dbo].[StrandReviewBeatScores]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[StrandReviewBeatScores] (
        [ReviewId]   UNIQUEIDENTIFIER NOT NULL,
        [BeatNumber] INT              NOT NULL,
        [Score]      INT              NOT NULL,
        CONSTRAINT [PK_StrandReviewBeatScores] PRIMARY KEY ([ReviewId], [BeatNumber]),
        CONSTRAINT [FK_StrandReviewBeatScores_StrandReviews]
            FOREIGN KEY ([ReviewId]) REFERENCES [dbo].[StrandReviews]([Id]) ON DELETE CASCADE
    );
END;

IF COL_LENGTH('dbo.StrandReviews', 'ClusterId') IS NULL
    EXEC(N'ALTER TABLE [dbo].[StrandReviews] ADD [ClusterId] INT NULL;');

IF COL_LENGTH('dbo.StrandReviews', 'ClusterLabel') IS NULL
    EXEC(N'ALTER TABLE [dbo].[StrandReviews] ADD [ClusterLabel] NVARCHAR(60) NULL;');

-- Narrative-flow / cohesion score (study mode): does it hang together as a
-- sequence, independent of standalone beat quality. Guards the "narrative tissue."
IF COL_LENGTH('dbo.StrandReviews', 'FlowScore') IS NULL
    EXEC(N'ALTER TABLE [dbo].[StrandReviews] ADD [FlowScore] INT NULL;');
