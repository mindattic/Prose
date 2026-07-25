-- SS-A47: Four-dimensional per-beat scoring (Swain doctrine).
-- Idempotent — each column is added only if it does not yet exist.
IF COL_LENGTH('dbo.NodeReviewBeatScores', 'ScoreBeat') IS NULL
    EXEC(N'ALTER TABLE [dbo].[NodeReviewBeatScores] ADD [ScoreBeat] INT NULL;');
IF COL_LENGTH('dbo.NodeReviewBeatScores', 'ScoreChapter') IS NULL
    EXEC(N'ALTER TABLE [dbo].[NodeReviewBeatScores] ADD [ScoreChapter] INT NULL;');
IF COL_LENGTH('dbo.NodeReviewBeatScores', 'ScoreArc') IS NULL
    EXEC(N'ALTER TABLE [dbo].[NodeReviewBeatScores] ADD [ScoreArc] INT NULL;');
IF COL_LENGTH('dbo.NodeReviewBeatScores', 'ScoreStory') IS NULL
    EXEC(N'ALTER TABLE [dbo].[NodeReviewBeatScores] ADD [ScoreStory] INT NULL;');
