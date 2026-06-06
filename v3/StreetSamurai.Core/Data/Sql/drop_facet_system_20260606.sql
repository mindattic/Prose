-- ============================================================================
-- Drop the retired facet system (2026-06-06).
--
-- The six-voice facet engine (WOUND/IDEAL/ID/SHADOW/MASK/GHOST) was removed from
-- code on 2026-06-06; character voice now comes from documented Psychology +
-- SpeechPatterns + NarrationVoice. This clears its schema:
--   * the four Facet* tables (all system-versioned) + their _History shadows
--   * the FacetTag columns on Beats / ChapterBeats (system-versioned) and
--     EpisodeBeats (plain)
--   * any stale facetRules blob inside the stored literary_rules setting
--
-- A full DB backup (StreetSamurai-pre-facet-drop-*.bak), a portable dump, and a
-- targeted FacetTag snapshot were taken before this ran (see archives/db/).
--
-- Idempotent: every step is guarded by an existence check, so re-running is safe.
-- For system-versioned tables we must SET SYSTEM_VERSIONING = OFF before any
-- DROP TABLE / DROP COLUMN, dropping from base AND _History so the schemas stay
-- in sync when versioning is turned back on.
-- ============================================================================

SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- 1) Facet* tables. Drop children (FK -> Facets) before the parent. ----------
IF OBJECT_ID('dbo.FacetTriggers','U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.FacetTriggers SET (SYSTEM_VERSIONING = OFF);
    DROP TABLE dbo.FacetTriggers;
    IF OBJECT_ID('dbo.FacetTriggers_History','U') IS NOT NULL DROP TABLE dbo.FacetTriggers_History;
END

IF OBJECT_ID('dbo.FacetCoreMemories','U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.FacetCoreMemories SET (SYSTEM_VERSIONING = OFF);
    DROP TABLE dbo.FacetCoreMemories;
    IF OBJECT_ID('dbo.FacetCoreMemories_History','U') IS NOT NULL DROP TABLE dbo.FacetCoreMemories_History;
END

IF OBJECT_ID('dbo.FacetVoiceProhibitions','U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.FacetVoiceProhibitions SET (SYSTEM_VERSIONING = OFF);
    DROP TABLE dbo.FacetVoiceProhibitions;
    IF OBJECT_ID('dbo.FacetVoiceProhibitions_History','U') IS NOT NULL DROP TABLE dbo.FacetVoiceProhibitions_History;
END

IF OBJECT_ID('dbo.Facets','U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Facets SET (SYSTEM_VERSIONING = OFF);
    DROP TABLE dbo.Facets;
    IF OBJECT_ID('dbo.Facets_History','U') IS NOT NULL DROP TABLE dbo.Facets_History;
END

-- 2) Beats.FacetTag (system-versioned). -------------------------------------
IF COL_LENGTH('dbo.Beats','FacetTag') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Beats SET (SYSTEM_VERSIONING = OFF);
    ALTER TABLE dbo.Beats DROP COLUMN FacetTag;
    IF COL_LENGTH('dbo.Beats_History','FacetTag') IS NOT NULL
        ALTER TABLE dbo.Beats_History DROP COLUMN FacetTag;
    ALTER TABLE dbo.Beats
        SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.Beats_History, DATA_CONSISTENCY_CHECK = ON));
END

-- 3) ChapterBeats.FacetTag (system-versioned). ------------------------------
IF COL_LENGTH('dbo.ChapterBeats','FacetTag') IS NOT NULL
BEGIN
    ALTER TABLE dbo.ChapterBeats SET (SYSTEM_VERSIONING = OFF);
    ALTER TABLE dbo.ChapterBeats DROP COLUMN FacetTag;
    IF COL_LENGTH('dbo.ChapterBeats_History','FacetTag') IS NOT NULL
        ALTER TABLE dbo.ChapterBeats_History DROP COLUMN FacetTag;
    ALTER TABLE dbo.ChapterBeats
        SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.ChapterBeats_History, DATA_CONSISTENCY_CHECK = ON));
END

-- 4) EpisodeBeats.FacetTag (plain table). -----------------------------------
IF COL_LENGTH('dbo.EpisodeBeats','FacetTag') IS NOT NULL
    ALTER TABLE dbo.EpisodeBeats DROP COLUMN FacetTag;

-- 5) Strip any stale facetRules blob from the stored literary_rules setting. -
UPDATE dbo.Settings SET Json = JSON_MODIFY(Json, '$.facetRules', NULL)
    WHERE [Key] = 'literary_rules' AND ISJSON(Json) = 1 AND JSON_QUERY(Json, '$.facetRules') IS NOT NULL;
UPDATE dbo.Settings SET Json = JSON_MODIFY(Json, '$.FacetRules', NULL)
    WHERE [Key] = 'literary_rules' AND ISJSON(Json) = 1 AND JSON_QUERY(Json, '$.FacetRules') IS NOT NULL;

COMMIT;
GO
