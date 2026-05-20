-- Track the three export artifacts that get filed for every episode:
--   ScriptMarkdownPath  — engine/episodes/{id}/script.md  (written when generation finishes)
--   ScriptPdfPath       — engine/episodes/{id}/script.pdf (rendered via QuestPDF)
--   CombinedAudioPath   — engine/episodes/{id}/episode.wav (concatenation of per-beat WAVs)
--
-- All three paths are stored relative to the data root so the same DB rows
-- survive a directory move. Null = not yet exported.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ARITHABORT ON;

IF COL_LENGTH('dbo.Episodes', 'ScriptMarkdownPath') IS NULL
BEGIN
    ALTER TABLE [dbo].[Episodes] ADD [ScriptMarkdownPath] NVARCHAR(400) NULL;
END
GO

IF COL_LENGTH('dbo.Episodes', 'ScriptPdfPath') IS NULL
BEGIN
    ALTER TABLE [dbo].[Episodes] ADD [ScriptPdfPath] NVARCHAR(400) NULL;
END
GO

IF COL_LENGTH('dbo.Episodes', 'CombinedAudioPath') IS NULL
BEGIN
    ALTER TABLE [dbo].[Episodes] ADD [CombinedAudioPath] NVARCHAR(400) NULL;
END
GO
