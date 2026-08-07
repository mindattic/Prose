-- Persona reader-review system. Many Legion personas each read a strand and
-- write an honest in-character review + a 1-100 score (StrandReviews); the
-- Amazon-style aggregate (avg, distribution, synthesized summary) lives in
-- StrandReviewSummaries (one latest row per strand).
--
-- Strands/Beats are NOT system-versioned, so each review fingerprints the text
-- it read via ContentHash (+ BeatCount) to capture which version was reviewed.
--
-- Re-runnable via OBJECT_ID / index-existence guards.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ARITHABORT ON;

IF OBJECT_ID(N'[dbo].[StrandReviews]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[StrandReviews] (
        [Id]           UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_StrandReviews] PRIMARY KEY,
        [StrandId]     UNIQUEIDENTIFIER NOT NULL,
        [PersonaId]    NVARCHAR(40)     NOT NULL,
        [PersonaName]  NVARCHAR(80)     NOT NULL,
        [PersonaBlurb] NVARCHAR(400)    NULL,
        [ProviderId]   NVARCHAR(40)     NOT NULL,
        [Model]        NVARCHAR(80)     NULL,
        [Score]        INT              NOT NULL DEFAULT 0,
        [ReviewText]   NVARCHAR(MAX)    NOT NULL DEFAULT N'',
        [Improvements] NVARCHAR(MAX)    NULL,
        [ContentHash]  NVARCHAR(64)     NOT NULL DEFAULT N'',
        [BeatCount]    INT              NOT NULL DEFAULT 0,
        [ReviewedAt]   DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        [CreatedAt]    DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        [UpdatedAt]    DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [FK_StrandReviews_Strands]
            FOREIGN KEY ([StrandId]) REFERENCES [dbo].[Strands]([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_StrandReviews_StrandId_ReviewedAt' AND object_id = OBJECT_ID(N'[dbo].[StrandReviews]'))
    CREATE INDEX [IX_StrandReviews_StrandId_ReviewedAt]
        ON [dbo].[StrandReviews]([StrandId], [ReviewedAt]);

IF OBJECT_ID(N'[dbo].[StrandReviewSummaries]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[StrandReviewSummaries] (
        [Id]                    UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_StrandReviewSummaries] PRIMARY KEY,
        [StrandId]              UNIQUEIDENTIFIER NOT NULL,
        [GeneratedAt]           DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        [ReviewCount]           INT              NOT NULL DEFAULT 0,
        [AvgScore]              FLOAT            NOT NULL DEFAULT 0,
        [ScoreDistributionJson] NVARCHAR(MAX)    NULL,
        [SummaryMarkdown]       NVARCHAR(MAX)    NOT NULL DEFAULT N'',
        [ContentHash]           NVARCHAR(64)     NULL,
        CONSTRAINT [FK_StrandReviewSummaries_Strands]
            FOREIGN KEY ([StrandId]) REFERENCES [dbo].[Strands]([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_StrandReviewSummaries_StrandId' AND object_id = OBJECT_ID(N'[dbo].[StrandReviewSummaries]'))
    CREATE UNIQUE INDEX [IX_StrandReviewSummaries_StrandId]
        ON [dbo].[StrandReviewSummaries]([StrandId]);
