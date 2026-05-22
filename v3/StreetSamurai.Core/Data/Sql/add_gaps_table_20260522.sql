-- Gaps: first-class entities for the silence between two adjacent beats.
-- The user-facing model is: a strand contains an alternating sequence of
-- Beats and Gaps. Each can be recorded; the combined narration walks them
-- in order and stitches them into one cohesive audio file.
--
-- Lazy materialisation: a Gap row only exists when the user has customised
-- it (changed its duration, attached a recorded ambient clip, or written
-- production notes). Otherwise the silence engine computes a default from
-- the adjacent beats' SceneType / terminator punctuation. Keeps the table
-- small — 99% of the time the default is correct.
--
-- A Gap is anchored by the (AboveBeatId, BelowBeatId) pair. If the beats
-- are later reordered so they're no longer adjacent, the Gap row becomes
-- orphaned (not rendered) — a future cleanup pass can sweep them.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ARITHABORT ON;

IF OBJECT_ID('dbo.Gaps', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Gaps]
    (
        [Id]            UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_Gaps] PRIMARY KEY,
        -- Sequential human-readable handle. Globally unique across the table
        -- so "Gap #47" is unambiguous without a strand qualifier.
        [Number]        INT              NOT NULL,
        [AboveBeatId]   UNIQUEIDENTIFIER NOT NULL,
        [BelowBeatId]   UNIQUEIDENTIFIER NOT NULL,
        -- The silence (or recorded clip) duration in milliseconds. Set to 0
        -- for "no gap at all"; user can override the auto-computed default.
        [DurationMs]    INT              NOT NULL CONSTRAINT [DF_Gaps_DurationMs] DEFAULT 0,
        -- Optional recorded audio file for non-silence gaps (rain, ambient
        -- room tone, a long sigh). When set, the silence engine inserts this
        -- file's contents between the two beats instead of digital silence.
        [AudioPath]     NVARCHAR(400)    NULL,
        [NarratedAt]    DATETIME2(7)     NULL,
        [LastRequestId] NVARCHAR(120)    NULL,
        -- Free-form note ("long sigh", "rain", "phone vibrates") so the
        -- writer can describe what they want here even before recording.
        [Notes]         NVARCHAR(500)    NULL,
        -- Marks gaps whose audio is out of date with the duration or notes.
        [Stale]         BIT              NOT NULL CONSTRAINT [DF_Gaps_Stale] DEFAULT 0,
        [CreatedAt]     DATETIME2(7)     NOT NULL CONSTRAINT [DF_Gaps_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [UpdatedAt]     DATETIME2(7)     NOT NULL CONSTRAINT [DF_Gaps_UpdatedAt] DEFAULT SYSUTCDATETIME(),

        CONSTRAINT [FK_Gaps_AboveBeat] FOREIGN KEY ([AboveBeatId]) REFERENCES [dbo].[Beats]([Id]),
        CONSTRAINT [FK_Gaps_BelowBeat] FOREIGN KEY ([BelowBeatId]) REFERENCES [dbo].[Beats]([Id])
    );

    -- One gap per ordered beat pair. The unique constraint is the data
    -- invariant: a strand can't have two different silences between the
    -- same two adjacent beats.
    CREATE UNIQUE INDEX [UX_Gaps_Pair] ON [dbo].[Gaps] ([AboveBeatId], [BelowBeatId]);
    -- Number is globally unique so callers can resolve "Gap #47" without a
    -- strand context — mirrors the Beat.Number contract.
    CREATE UNIQUE INDEX [UX_Gaps_Number] ON [dbo].[Gaps] ([Number]);
END
GO
