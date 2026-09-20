-- Species: first-class taxonomy for sentient life. The controlled vocabulary
-- Character.Species references (bridge by name). Exactly five rows. Non-sentient
-- machines are NOT a species — they live in the Automaton repo (the sentience
-- test, ARCHITECTURE.md §2a). Re-runnable: creates the table + idempotently
-- MERGEs the five canonical rows.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF OBJECT_ID(N'[dbo].[Species]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Species] (
        [Id]          UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_Species] PRIMARY KEY,
        [Name]        NVARCHAR(40)     NOT NULL,
        [Label]       NVARCHAR(80)     NOT NULL DEFAULT N'',
        [Description] NVARCHAR(MAX)    NOT NULL DEFAULT N'',
        [Sentient]    BIT              NOT NULL DEFAULT 1,
        [Examples]    NVARCHAR(MAX)    NOT NULL DEFAULT N''
    );
END;

-- Only the single-universe Name-only unique index (fresh DB, pre-universe). Once Species.UniverseId
-- exists, uniqueness is per-universe (a composite index), so a Name-only UNIQUE index would fail on
-- duplicate Names across universes (GLMZ + Fantasy 'human') — skip it.
IF COL_LENGTH(N'dbo.Species', N'UniverseId') IS NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Species_Name' AND object_id = OBJECT_ID(N'[dbo].[Species]'))
    CREATE UNIQUE INDEX [IX_Species_Name] ON [dbo].[Species]([Name]);

-- Idempotency guard: the original seed MERGE matches on [Name] alone. Once the multi-universe
-- migration adds Species.UniverseId, a single Name (e.g. 'human') can exist in MORE than one
-- universe (GLMZ + Fantasy), and a Name-only MERGE then matches multiple target rows → a
-- "MERGE attempted to UPDATE the same row more than once" error on re-run. On a fresh DB this
-- migration runs BEFORE add_universe_*, so no UniverseId column exists yet → seed the canonical 5
-- (add_universe later stamps them GLMZ). On an already-migrated DB the column exists and the 5 are
-- already seeded + universe-scoped, so skip (no-op) rather than fire the unsafe Name-only MERGE.
IF COL_LENGTH(N'dbo.Species', N'UniverseId') IS NULL
MERGE [dbo].[Species] AS t
USING (VALUES
    (N'human', N'Human',
     N'Baseline Homo sapiens. Cybernetics are near-universal in the GLMZ and do NOT change species — an augmented human is still human. The overwhelming majority of the population.',
     N'Kyle; most freelancers, corponation staff, and civilians'),
    (N'ai', N'AI',
     N'Artificial intelligences: built minds on a software substrate. Spans corporate-scale Superminds, Rogue AIs (from Fragments to Leviathans), and lesser digital minds. Sentient, non-biological.',
     N'Consensus (the merged-minds AI); Superminds; Rogue AIs'),
    (N'elf', N'E.L.F. (Emergent Lifeform)',
     N'Emergent Lifeforms — paratechnological digital beings that AROSE rather than were built, native to the Network''s deep layers. Sentient and alien in cognition; outsiders to the human/AI order.',
     N'ELFs sighted in the Network''s thin layers'),
    (N'synthetic', N'Synthetic',
     N'Engineered sentient life with a physical vessel — manufactured but feeling. Includes Ceramic Men (living gas held in a porcelain humanoid body). Distinct from mindless machines, which are Automata.',
     N'Ceramic Men; vessel-bound engineered minds'),
    (N'unknown', N'Unknown',
     N'Sentience of indeterminate or contested origin — classification pending, or deliberately left ambiguous in canon (the open-mysteries doctrine).',
     N'Entities whose nature is an open question')
) AS s ([Name],[Label],[Description],[Examples])
ON t.[Name] = s.[Name]
WHEN MATCHED THEN UPDATE SET t.[Label] = s.[Label], t.[Description] = s.[Description], t.[Examples] = s.[Examples], t.[Sentient] = 1
WHEN NOT MATCHED THEN
    INSERT ([Id],[Name],[Label],[Description],[Sentient],[Examples])
    VALUES (NEWID(), s.[Name], s.[Label], s.[Description], 1, s.[Examples]);
