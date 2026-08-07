-- Per-run narration progress counters on Strand. NarratedBeatCount climbs
-- as the loop confirms each beat's TTS synthesis; TotalBeatsToNarrate is
-- the snapshot of the ordered-beats count when NarrateAsync started. Pairs
-- give the polling UI a single int to read instead of scanning the whole
-- beats collection ("narrated 47 / 200" without re-loading 200 rows).
--
-- Both default to 0 so existing rows are valid; the narration loop resets
-- them at the start of each run.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ARITHABORT ON;

IF COL_LENGTH('dbo.Strands', 'NarratedBeatCount') IS NULL
    EXEC(N'ALTER TABLE [dbo].[Strands] ADD [NarratedBeatCount] INT NOT NULL CONSTRAINT [DF_Strands_NarratedBeatCount] DEFAULT 0;');

IF COL_LENGTH('dbo.Strands', 'TotalBeatsToNarrate') IS NULL
    EXEC(N'ALTER TABLE [dbo].[Strands] ADD [TotalBeatsToNarrate] INT NOT NULL CONSTRAINT [DF_Strands_TotalBeatsToNarrate] DEFAULT 0;');
