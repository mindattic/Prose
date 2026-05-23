-- Beat.Kind: what role this beat plays in the prose. Default "prose"
-- (regular narrative paragraph). Other values render differently in the
-- writer/listener:
--   prose       — regular narrative beat
--   book-title  — front-matter title page (Text=title, BeatTitle=author)
--   dedication  — centered italic line (Text=dedication)
--   quote       — blockquote / epigraph (Text=quote, BeatTitle=attribution)
-- Free-form string so new kinds can be added without a schema migration.
-- IsChapterStart stays as its own BIT and is orthogonal — a quote that
-- opens a chapter is Kind='quote' + IsChapterStart=1 (i.e. an epigraph).

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ARITHABORT ON;

IF COL_LENGTH('dbo.Beats', 'Kind') IS NULL
BEGIN
    ALTER TABLE [dbo].[Beats]
        ADD [Kind] NVARCHAR(40) NOT NULL
            CONSTRAINT [DF_Beats_Kind] DEFAULT 'prose';
END
GO
