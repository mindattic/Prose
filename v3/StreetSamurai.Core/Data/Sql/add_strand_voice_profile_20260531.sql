-- Per-strand voice-profile snapshot. Captured once, the first time any beat
-- in a strand is narrated, from the then-current default voice profile, then
-- frozen here. Every later (re)record reuses these values + the deterministic
-- seed instead of the live global settings, so a beat recorded today sounds
-- like beats laid down weeks ago even if the default profile/model changed.
--
--   VoiceModel       — ElevenLabs model id locked for the strand (e.g. eleven_v3)
--   VoiceStability   — locked baseline stability
--   VoiceSimilarity  — locked baseline similarity_boost
--   VoiceStyle       — locked baseline style
--   VoiceSeed        — deterministic generation seed (derived from the strand id)
--
-- All nullable: existing rows stay valid (NULL = "not yet narrated"; the
-- snapshot is taken lazily on the next synthesis). Re-runnable via COL_LENGTH
-- guards.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ARITHABORT ON;

IF COL_LENGTH('dbo.Strands', 'VoiceModel') IS NULL
    EXEC(N'ALTER TABLE [dbo].[Strands] ADD [VoiceModel] NVARCHAR(100) NULL;');

IF COL_LENGTH('dbo.Strands', 'VoiceStability') IS NULL
    EXEC(N'ALTER TABLE [dbo].[Strands] ADD [VoiceStability] FLOAT NULL;');

IF COL_LENGTH('dbo.Strands', 'VoiceSimilarity') IS NULL
    EXEC(N'ALTER TABLE [dbo].[Strands] ADD [VoiceSimilarity] FLOAT NULL;');

IF COL_LENGTH('dbo.Strands', 'VoiceStyle') IS NULL
    EXEC(N'ALTER TABLE [dbo].[Strands] ADD [VoiceStyle] FLOAT NULL;');

IF COL_LENGTH('dbo.Strands', 'VoiceSeed') IS NULL
    EXEC(N'ALTER TABLE [dbo].[Strands] ADD [VoiceSeed] INT NULL;');
