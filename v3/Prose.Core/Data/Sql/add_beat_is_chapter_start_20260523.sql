-- Chapter-as-flag, not chapter-as-child-strand. A beat with IsChapterStart=1
-- is rendered with a divider + heading (BeatTitle) above it. Replaces the
-- old "nested strand per chapter" model. One flat strand per work; chapter
-- structure lives on the beats themselves.
--
-- The actual fold of existing child strands into their root is done by the
-- C# data migration in ApplyMigrations/Program.cs, which runs after the
-- SQL migrations and uses DFS preorder to flatten each tree while marking
-- the first beat of each formerly-child strand as a chapter start.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ARITHABORT ON;

IF COL_LENGTH('dbo.Beats', 'IsChapterStart') IS NULL
BEGIN
    ALTER TABLE [dbo].[Beats]
        ADD [IsChapterStart] BIT NOT NULL
            CONSTRAINT [DF_Beats_IsChapterStart] DEFAULT 0;
END
GO
