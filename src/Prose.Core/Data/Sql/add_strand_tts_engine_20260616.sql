-- add_strand_tts_engine_20260616.sql
-- ───────────────────────────────────────────────────────────────────────────
-- Adds TtsEngine (NVARCHAR 40, nullable) to Strands + Strands_History.
--
-- Meaning: which TTS backend narrates this strand.
--   NULL / 'elevenlabs' → ElevenLabs (existing behaviour, all existing rows).
--   'kokoro'            → Kokoro-82M via PythonTtsService.
--   'piper'             → Piper via PiperTtsService.
--
-- Column re-use mapping for non-ElevenLabs engines
-- (avoids new columns; documented here as the canonical reference):
--   VoiceId         → engine voice id (kokoro: e.g. "af_sky"; piper: e.g. "en_US-ryan-high")
--   VoiceStability  → piper: length_scale (speed 0.5–2.0); kokoro: unused (null)
--   VoiceStyle      → piper: noise_scale (0.0–1.0); kokoro: speed (0.5–2.0)
--   VoiceSimilarity → unused for local engines (null)
--   VoiceModel      → unused for local engines (null)
--
-- Pattern: SYSTEM_VERSIONING OFF → ALTER + _History → ON
-- Idempotent. Run under QUOTED_IDENTIFIER ON (sqlcmd -I).
-- ───────────────────────────────────────────────────────────────────────────

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Strands' AND temporal_type = 2)
    ALTER TABLE dbo.Strands SET (SYSTEM_VERSIONING = OFF);
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Strands') AND name = 'TtsEngine')
    ALTER TABLE dbo.Strands ADD TtsEngine NVARCHAR(40) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Strands_History') AND name = 'TtsEngine')
    ALTER TABLE dbo.Strands_History ADD TtsEngine NVARCHAR(40) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Strands' AND temporal_type = 2)
    ALTER TABLE dbo.Strands SET (SYSTEM_VERSIONING = ON
        (HISTORY_TABLE = dbo.Strands_History, DATA_CONSISTENCY_CHECK = OFF));
GO
