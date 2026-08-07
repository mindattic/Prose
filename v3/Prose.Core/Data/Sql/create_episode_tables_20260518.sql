-- Episode domain — bedtime adventures (folk-hero Kyle, scoreable, audio-narrated).
-- Distinct from Books/Chapters: episodes are ephemeral and stand alone.
-- Granular by beat (paragraph) so audio narration streams + corrections target
-- specific paragraphs.
--
-- Created 2026-05-18 for the /listen pivot. See EpisodeGeneratorService /
-- EpisodeAudioService / engine/bushido_coda_v3/00_style_guide.md.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;

-- ── Episodes ────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Episodes', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Episodes] (
        [Id]                      INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Episodes] PRIMARY KEY,
        [Seed]                    NVARCHAR(1000)    NOT NULL,
        [Title]                   NVARCHAR(400)     NOT NULL,
        [VoiceId]                 NVARCHAR(64)      NULL,
        [StartedAt]               DATETIME2(7)      NOT NULL DEFAULT SYSUTCDATETIME(),
        [GenerationCompletedAt]   DATETIME2(7)      NULL,
        [AudioCompletedAt]        DATETIME2(7)      NULL,
        [Status]                  NVARCHAR(32)      NOT NULL DEFAULT N'queued',
        [CharsNarrated]           INT               NOT NULL DEFAULT 0,
        [Error]                   NVARCHAR(MAX)     NULL
    );

    CREATE INDEX [IX_Episodes_StartedAt] ON [dbo].[Episodes] ([StartedAt] DESC);
    CREATE INDEX [IX_Episodes_Status]    ON [dbo].[Episodes] ([Status]);
END
GO

-- ── EpisodeBeats ────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.EpisodeBeats', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[EpisodeBeats] (
        [Id]              INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_EpisodeBeats] PRIMARY KEY,
        [EpisodeId]       INT               NOT NULL,
        [Index]           INT               NOT NULL,
        [Text]            NVARCHAR(MAX)     NOT NULL,
        [AudioPath]       NVARCHAR(400)     NULL,
        [GeneratedAt]     DATETIME2(7)      NOT NULL DEFAULT SYSUTCDATETIME(),
        [NarratedAt]      DATETIME2(7)      NULL,
        [WasCorrected]    BIT               NOT NULL DEFAULT 0,

        CONSTRAINT [FK_EpisodeBeats_Episodes]
            FOREIGN KEY ([EpisodeId]) REFERENCES [dbo].[Episodes]([Id]) ON DELETE CASCADE
    );

    -- One beat per (episode, index) — paragraph ordering invariant.
    CREATE UNIQUE INDEX [IX_EpisodeBeats_Episode_Index]
        ON [dbo].[EpisodeBeats] ([EpisodeId], [Index]);
END
GO

-- ── EpisodeCorrections ──────────────────────────────────────────────────────
IF OBJECT_ID('dbo.EpisodeCorrections', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[EpisodeCorrections] (
        [Id]           INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_EpisodeCorrections] PRIMARY KEY,
        [EpisodeId]    INT               NOT NULL,
        [BeatIndex]    INT               NULL,
        [Note]         NVARCHAR(MAX)     NOT NULL,
        [CapturedAt]   DATETIME2(7)      NOT NULL DEFAULT SYSUTCDATETIME(),
        [Applied]      BIT               NOT NULL DEFAULT 0,

        CONSTRAINT [FK_EpisodeCorrections_Episodes]
            FOREIGN KEY ([EpisodeId]) REFERENCES [dbo].[Episodes]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_EpisodeCorrections_Episode] ON [dbo].[EpisodeCorrections] ([EpisodeId]);
    CREATE INDEX [IX_EpisodeCorrections_Applied] ON [dbo].[EpisodeCorrections] ([Applied]);
END
GO

-- ── EpisodeSurveys ──────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.EpisodeSurveys', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[EpisodeSurveys] (
        [Id]            INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_EpisodeSurveys] PRIMARY KEY,
        [EpisodeId]     INT               NOT NULL,
        [Score]         INT               NOT NULL,
        [Pacing]        INT               NULL,
        [Voice]         INT               NULL,
        [Notes]         NVARCHAR(MAX)     NULL,
        [WasInbox]      BIT               NOT NULL DEFAULT 0,
        [CompletedAt]   DATETIME2(7)      NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT [FK_EpisodeSurveys_Episodes]
            FOREIGN KEY ([EpisodeId]) REFERENCES [dbo].[Episodes]([Id]) ON DELETE CASCADE
    );

    -- One survey per episode.
    CREATE UNIQUE INDEX [IX_EpisodeSurveys_Episode] ON [dbo].[EpisodeSurveys] ([EpisodeId]);
END
GO
