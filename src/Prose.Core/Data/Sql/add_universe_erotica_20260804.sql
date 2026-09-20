-- add_universe_erotica_20260804.sql
-- ───────────────────────────────────────────────────────────────────────────
-- 6th Universe: EROTICA (SS-A2 / SS-LAW-15). Standing up alongside GLMZ
-- (cyberpunk), SCRY (fantasy), NONFICTION (formerly SOURCE/GSPL), FICTION
-- (formerly EPIC), and HORROR on the same universe/node/beat schema — no
-- schema fork. World facts, craft doctrine, and book bibles are TBD; seed
-- via docs/EROTICA.md + SetUniversalFacts and per-book briefs when the
-- flagship book is planned. Graphic adult content is already governed
-- project-wide (CLAUDE.md "Prose Content — Graphic Adult Content"); EROTICA
-- narrows to that as its primary genre rather than an occasional register.
--
-- Like GSPL/HORROR, EROTICA has no matching Universe.<X>Id constant in
-- Universe.cs — none is needed. Program.cs's Finalize() eagerly resolves
-- IUniverseContext before any CLI dispatch runs, so universe scoping is
-- fully DB-driven off this table.
--
-- Idempotent: safe to re-run.
-- ───────────────────────────────────────────────────────────────────────────

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Universe WHERE Id = '0197e9c9-0006-7000-8000-000000000006')
    INSERT INTO dbo.Universe (Id, Slug, Name, Description, Theme, SortKey)
    VALUES ('0197e9c9-0006-7000-8000-000000000006', 'erotica', 'EROTICA',
            'A graphic-adult-content fiction universe (world facts, craft doctrine, and book bibles to be defined).',
            'erotica', 500);
GO
