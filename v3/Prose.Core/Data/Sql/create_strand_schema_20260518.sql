-- Unified beat/strand schema. Replaces the Books / Chapters / ChapterBeats
-- / Episodes / EpisodeBeats five-table split with two tables + a junction.
-- Beat owns prose AND audio in one row (no more bidirectional sync). Strand
-- replaces Book/Chapter/Episode — they're now just different Kind values on
-- the same table, with optional parent-child nesting.
--
-- Idempotent: IF NOT EXISTS guards on every object.

-- ── Beats ───────────────────────────────────────────────────────────────
IF OBJECT_ID(N'[dbo].[Beats]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Beats] (
        [Id]            UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [Slug]          NVARCHAR(200)    NULL,
        [Text]          NVARCHAR(MAX)    NOT NULL DEFAULT N'',
        [TextHash]      NVARCHAR(80)     NULL,

        [BeatTitle]     NVARCHAR(400)    NULL,
        [Synopsis]      NVARCHAR(MAX)    NULL,
        [StructureRole] NVARCHAR(80)     NULL,
        [Act]           INT              NOT NULL DEFAULT 0,
        [SceneType]     NVARCHAR(40)     NOT NULL DEFAULT N'scene',
        [FacetTag]      NVARCHAR(40)     NULL,
        [EmotionalTone] NVARCHAR(40)     NULL,
        [PaceHint]      NVARCHAR(40)     NULL,

        [VoiceId]       NVARCHAR(80)     NULL,
        [AudioPath]     NVARCHAR(400)    NULL,
        [NarratedAt]    DATETIME2        NULL,
        [DurationSec]   FLOAT            NULL,
        [LastRequestId] NVARCHAR(120)    NULL,
        [Stale]         BIT              NOT NULL DEFAULT 0,
        [WasCorrected]  BIT              NOT NULL DEFAULT 0,

        [CreatedAt]     DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        [UpdatedAt]     DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Beats_Slug' AND object_id = OBJECT_ID(N'[dbo].[Beats]'))
    CREATE INDEX [IX_Beats_Slug] ON [dbo].[Beats]([Slug]) WHERE [Slug] IS NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Beats_Stale' AND object_id = OBJECT_ID(N'[dbo].[Beats]'))
    CREATE INDEX [IX_Beats_Stale] ON [dbo].[Beats]([Stale]) WHERE [Stale] = 1;

-- ── Strands ─────────────────────────────────────────────────────────────
IF OBJECT_ID(N'[dbo].[Strands]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Strands] (
        [Id]                    UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [Slug]                  NVARCHAR(200)    NOT NULL,
        [Title]                 NVARCHAR(400)    NOT NULL DEFAULT N'',
        [Synopsis]              NVARCHAR(MAX)    NULL,

        [Kind]                  NVARCHAR(40)     NOT NULL DEFAULT N'strand',
        [Status]                NVARCHAR(40)     NOT NULL DEFAULT N'draft',

        [ParentStrandId]        UNIQUEIDENTIFIER NULL,
        [SortKey]               FLOAT            NOT NULL DEFAULT 0,

        [CombinedAudioPath]     NVARCHAR(400)    NULL,
        [ScriptMarkdownPath]    NVARCHAR(400)    NULL,
        [ScriptPdfPath]         NVARCHAR(400)    NULL,
        [VoiceId]               NVARCHAR(80)     NULL,

        [Seed]                  NVARCHAR(MAX)    NULL,
        [StartedAt]             DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        [GenerationCompletedAt] DATETIME2        NULL,
        [AudioCompletedAt]      DATETIME2        NULL,
        [CharsNarrated]         INT              NOT NULL DEFAULT 0,
        [LastPlayedBeatId]      UNIQUEIDENTIFIER NULL,
        [LastPlayedSec]         FLOAT            NULL,
        [Error]                 NVARCHAR(MAX)    NULL,

        [CreatedAt]             DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        [UpdatedAt]             DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Strands_ParentStrand')
    ALTER TABLE [dbo].[Strands]
        ADD CONSTRAINT [FK_Strands_ParentStrand]
        FOREIGN KEY ([ParentStrandId]) REFERENCES [dbo].[Strands]([Id]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Strands_Slug' AND object_id = OBJECT_ID(N'[dbo].[Strands]'))
    CREATE UNIQUE INDEX [IX_Strands_Slug] ON [dbo].[Strands]([Slug]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Strands_ParentStrandId_SortKey' AND object_id = OBJECT_ID(N'[dbo].[Strands]'))
    CREATE INDEX [IX_Strands_ParentStrandId_SortKey]
        ON [dbo].[Strands]([ParentStrandId], [SortKey])
        WHERE [ParentStrandId] IS NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Strands_Kind' AND object_id = OBJECT_ID(N'[dbo].[Strands]'))
    CREATE INDEX [IX_Strands_Kind] ON [dbo].[Strands]([Kind]);

-- ── StrandBeats junction ────────────────────────────────────────────────
IF OBJECT_ID(N'[dbo].[StrandBeats]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[StrandBeats] (
        [StrandId] UNIQUEIDENTIFIER NOT NULL,
        [BeatId]   UNIQUEIDENTIFIER NOT NULL,
        [SortKey]  FLOAT            NOT NULL DEFAULT 0,
        CONSTRAINT [PK_StrandBeats]   PRIMARY KEY ([StrandId], [BeatId]),
        CONSTRAINT [FK_StrandBeats_Strand] FOREIGN KEY ([StrandId]) REFERENCES [dbo].[Strands]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_StrandBeats_Beat]   FOREIGN KEY ([BeatId])   REFERENCES [dbo].[Beats]([Id])   ON DELETE CASCADE
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_StrandBeats_StrandId_SortKey' AND object_id = OBJECT_ID(N'[dbo].[StrandBeats]'))
    CREATE INDEX [IX_StrandBeats_StrandId_SortKey] ON [dbo].[StrandBeats]([StrandId], [SortKey]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_StrandBeats_BeatId' AND object_id = OBJECT_ID(N'[dbo].[StrandBeats]'))
    CREATE INDEX [IX_StrandBeats_BeatId] ON [dbo].[StrandBeats]([BeatId]);
