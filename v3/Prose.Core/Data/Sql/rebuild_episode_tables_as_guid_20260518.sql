-- Switch Episodes.Id from INT IDENTITY to UNIQUEIDENTIFIER, matching the
-- canonical entity convention everywhere else in the DB. Episode IDs are
-- generated as UUIDv7 by the application (Guid.CreateVersion7) — sortable by
-- creation time, globally unique, suitable as the principal key.
--
-- All Episode tables are empty at the time of this migration (the feature
-- has not yet been driven end-to-end), so the cleanest path is drop +
-- recreate. Re-running this seed is idempotent: each CREATE is guarded by
-- OBJECT_ID checks.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ARITHABORT ON;

-- Order matters for FK drop.
IF OBJECT_ID('dbo.EpisodeSurveys',     'U') IS NOT NULL DROP TABLE [dbo].[EpisodeSurveys];
IF OBJECT_ID('dbo.EpisodeCorrections', 'U') IS NOT NULL DROP TABLE [dbo].[EpisodeCorrections];
IF OBJECT_ID('dbo.EpisodeBeats',       'U') IS NOT NULL DROP TABLE [dbo].[EpisodeBeats];
IF OBJECT_ID('dbo.Episodes',           'U') IS NOT NULL DROP TABLE [dbo].[Episodes];
GO

CREATE TABLE [dbo].[Episodes] (
    [Id]                      UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [PK_Episodes] PRIMARY KEY,
    [Slug]                    NVARCHAR(200)     NOT NULL CONSTRAINT [DF_Episodes_Slug] DEFAULT N'',
    [Seed]                    NVARCHAR(1000)    NOT NULL,
    [Title]                   NVARCHAR(400)     NOT NULL,
    [VoiceId]                 NVARCHAR(64)      NULL,
    [StartedAt]               DATETIME2(7)      NOT NULL CONSTRAINT [DF_Episodes_StartedAt] DEFAULT SYSUTCDATETIME(),
    [GenerationCompletedAt]   DATETIME2(7)      NULL,
    [AudioCompletedAt]        DATETIME2(7)      NULL,
    [Status]                  NVARCHAR(32)      NOT NULL CONSTRAINT [DF_Episodes_Status] DEFAULT N'queued',
    [CharsNarrated]           INT               NOT NULL CONSTRAINT [DF_Episodes_CharsNarrated] DEFAULT 0,
    [Error]                   NVARCHAR(MAX)     NULL,
    [ScriptMarkdownPath]      NVARCHAR(400)     NULL,
    [ScriptPdfPath]           NVARCHAR(400)     NULL,
    [CombinedAudioPath]       NVARCHAR(400)     NULL
);

CREATE INDEX        [IX_Episodes_StartedAt] ON [dbo].[Episodes] ([StartedAt] DESC);
CREATE INDEX        [IX_Episodes_Status]    ON [dbo].[Episodes] ([Status]);
CREATE UNIQUE INDEX [IX_Episodes_Slug]      ON [dbo].[Episodes] ([Slug]);
GO

CREATE TABLE [dbo].[EpisodeBeats] (
    [Id]              INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_EpisodeBeats] PRIMARY KEY,
    [EpisodeId]       UNIQUEIDENTIFIER  NOT NULL,
    [Index]           INT               NOT NULL,
    [Text]            NVARCHAR(MAX)     NOT NULL,
    [AudioPath]       NVARCHAR(400)     NULL,
    [GeneratedAt]     DATETIME2(7)      NOT NULL CONSTRAINT [DF_EpisodeBeats_GeneratedAt] DEFAULT SYSUTCDATETIME(),
    [NarratedAt]      DATETIME2(7)      NULL,
    [DurationSec]     FLOAT             NULL,
    [WasCorrected]    BIT               NOT NULL CONSTRAINT [DF_EpisodeBeats_WasCorrected] DEFAULT 0,

    CONSTRAINT [FK_EpisodeBeats_Episodes]
        FOREIGN KEY ([EpisodeId]) REFERENCES [dbo].[Episodes]([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_EpisodeBeats_Episode_Index]
    ON [dbo].[EpisodeBeats] ([EpisodeId], [Index]);
GO

CREATE TABLE [dbo].[EpisodeCorrections] (
    [Id]           INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_EpisodeCorrections] PRIMARY KEY,
    [EpisodeId]    UNIQUEIDENTIFIER  NOT NULL,
    [BeatIndex]    INT               NULL,
    [Note]         NVARCHAR(MAX)     NOT NULL,
    [CapturedAt]   DATETIME2(7)      NOT NULL CONSTRAINT [DF_EpisodeCorrections_CapturedAt] DEFAULT SYSUTCDATETIME(),
    [Applied]      BIT               NOT NULL CONSTRAINT [DF_EpisodeCorrections_Applied] DEFAULT 0,

    CONSTRAINT [FK_EpisodeCorrections_Episodes]
        FOREIGN KEY ([EpisodeId]) REFERENCES [dbo].[Episodes]([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_EpisodeCorrections_Episode] ON [dbo].[EpisodeCorrections] ([EpisodeId]);
CREATE INDEX [IX_EpisodeCorrections_Applied] ON [dbo].[EpisodeCorrections] ([Applied]);
GO

CREATE TABLE [dbo].[EpisodeSurveys] (
    [Id]            INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_EpisodeSurveys] PRIMARY KEY,
    [EpisodeId]     UNIQUEIDENTIFIER  NOT NULL,
    [Score]         INT               NOT NULL,
    [Pacing]        INT               NULL,
    [Voice]         INT               NULL,
    [Notes]         NVARCHAR(MAX)     NULL,
    [WasInbox]      BIT               NOT NULL CONSTRAINT [DF_EpisodeSurveys_WasInbox] DEFAULT 0,
    [CompletedAt]   DATETIME2(7)      NOT NULL CONSTRAINT [DF_EpisodeSurveys_CompletedAt] DEFAULT SYSUTCDATETIME(),

    CONSTRAINT [FK_EpisodeSurveys_Episodes]
        FOREIGN KEY ([EpisodeId]) REFERENCES [dbo].[Episodes]([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_EpisodeSurveys_Episode] ON [dbo].[EpisodeSurveys] ([EpisodeId]);
GO
