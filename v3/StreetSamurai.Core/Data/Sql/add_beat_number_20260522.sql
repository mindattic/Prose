-- Stable integer Number on every Beat. The guid Id is the durable globally
-- unique handle; Number is a small human-readable counter so the user (and
-- a CLI assistant) can refer to a beat as "Beat #134" without having to
-- copy/paste the 36-char guid every time. Numbers are assigned at creation
-- time and never change — unlike the positional "BEAT 042" badge in the
-- writer UI which shifts when beats are reordered, inserted, or deleted.
--
-- Each ALTER / UPDATE / index step is wrapped in EXEC(N'…') so SQL Server
-- treats them as separate batches. The seed runner strips GO and runs the
-- script as one ExecuteSqlRawAsync batch — without EXEC, the UPDATE would
-- reference [Number] before the ALTER had committed the new column.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ARITHABORT ON;

IF COL_LENGTH('dbo.Beats', 'Number') IS NULL
    EXEC(N'ALTER TABLE [dbo].[Beats] ADD [Number] INT NULL;');

-- Backfill in deterministic CreatedAt + Id order. WHERE Number IS NULL so
-- a re-run only touches new rows that haven't been numbered yet.
EXEC(N'
;WITH ordered AS (
    SELECT [Id], ROW_NUMBER() OVER (ORDER BY [CreatedAt], [Id]) AS rn
    FROM [dbo].[Beats]
    WHERE [Number] IS NULL
)
UPDATE b
SET b.[Number] = o.rn + ISNULL((SELECT MAX([Number]) FROM [dbo].[Beats] WHERE [Number] IS NOT NULL), 0)
FROM [dbo].[Beats] b
JOIN ordered o ON o.[Id] = b.[Id];
');

-- Lock the column to NOT NULL now that every row has a value.
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Beats') AND name = 'Number' AND is_nullable = 1)
    EXEC(N'ALTER TABLE [dbo].[Beats] ALTER COLUMN [Number] INT NOT NULL;');

-- Unique index so "find Beat #134" is an O(log n) seek. Also enforces the
-- single-writer assumption at the DB level — if two concurrent inserts pick
-- the same MAX+1, one will fail with a duplicate-key error rather than
-- silently producing colliding numbers.
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_Beats_Number' AND object_id = OBJECT_ID('dbo.Beats'))
    EXEC(N'CREATE UNIQUE INDEX [UX_Beats_Number] ON [dbo].[Beats] ([Number]);');
