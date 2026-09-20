-- add_universe_horror_20260803.sql
-- ───────────────────────────────────────────────────────────────────────────
-- 4th Universe: HORROR (SS-A2 / SS-LAW-15). A horror-genre fiction universe,
-- standing up alongside GLMZ (cyberpunk), SCRY (fantasy), and SOURCE
-- (citation-grounded nonfiction) on the same universe/node/beat schema —
-- no schema fork. World facts, craft doctrine, and book bibles are TBD;
-- seed via docs/universes/HORROR.md + SetUniversalFacts and per-book briefs.
--
-- Like GSPL, HORROR has no matching Universe.<X>Id constant in Universe.cs —
-- none is needed. Program.cs's Finalize() eagerly resolves IUniverseContext
-- before any CLI dispatch runs, so universe scoping is fully DB-driven off
-- this table; the well-known constants only exist for GLMZ/SCRY/SOURCE's
-- specific bootstrap fast paths.
--
-- Idempotent: safe to re-run.
-- ───────────────────────────────────────────────────────────────────────────

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Universe WHERE Id = '0197e9c9-0004-7000-8000-000000000004')
    INSERT INTO dbo.Universe (Id, Slug, Name, Description, Theme, SortKey)
    VALUES ('0197e9c9-0004-7000-8000-000000000004', 'horror', 'HORROR',
            'A horror-genre fiction universe (world facts, craft doctrine, and book bibles to be defined).',
            'horror', 400);
GO
