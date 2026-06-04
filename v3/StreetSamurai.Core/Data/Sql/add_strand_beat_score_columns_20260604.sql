-- Score / ScoredAt aggregate columns on Strands and Beats. The review pipeline
-- writes these via RecomputeScoresAsync (strand-level and per-beat % shown in
-- the writer + /strands badge). They were added to the EF model and created on
-- fresh local DBs via EnsureCreated, but no prod .sql migration ever shipped —
-- so Azure SQL lacked them and ApplyMigrations' fold step threw
-- "Invalid column name 'Score' / 'ScoredAt'". Idempotent: COL_LENGTH guards.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ARITHABORT ON;

IF COL_LENGTH('dbo.Strands', 'Score') IS NULL
    EXEC(N'ALTER TABLE [dbo].[Strands] ADD [Score] FLOAT NULL;');

IF COL_LENGTH('dbo.Strands', 'ScoredAt') IS NULL
    EXEC(N'ALTER TABLE [dbo].[Strands] ADD [ScoredAt] DATETIME2 NULL;');

IF COL_LENGTH('dbo.Beats', 'Score') IS NULL
    EXEC(N'ALTER TABLE [dbo].[Beats] ADD [Score] FLOAT NULL;');

IF COL_LENGTH('dbo.Beats', 'ScoredAt') IS NULL
    EXEC(N'ALTER TABLE [dbo].[Beats] ADD [ScoredAt] DATETIME2 NULL;');
