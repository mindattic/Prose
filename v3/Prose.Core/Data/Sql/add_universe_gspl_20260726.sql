-- add_universe_gspl_20260726.sql
-- ───────────────────────────────────────────────────────────────────────────
-- 3rd Universe: GSPL — "Gospel: History vs. Heritage" (SS-A2 / SS-LAW-15).
-- A nonfiction-evidentiary universe: entertaining New Testament prose,
-- citation-grounded (docs/GSPL.md), spectrum-of-scholarship craft rules on
-- top of docs/CRAFT.md — same universe/node/beat schema as GLMZ/SCRY, no
-- schema fork. See docs/GSPL.md and docs/gospel/README.md for the doctrine.
--
-- Unlike GLMZ/Fantasy, GSPL has no matching Universe.GsplId constant in
-- Universe.cs — none is needed. Program.cs's Finalize() (added 2026-07-26)
-- eagerly resolves IUniverseContext before any CLI dispatch runs, so every
-- command's universe scoping is DB-driven off this table already; the
-- Universe.GlmzId/FantasyId constants exist only for GLMZ's IsGlmz
-- default-fallback logic and the now-superseded bootstrap fast path
-- (UniverseBootstrap.ResolveWellKnownId), neither of which GSPL needs.
--
-- Idempotent: safe to re-run.
-- ───────────────────────────────────────────────────────────────────────────

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Universe WHERE Id = '0197e9c9-0003-7000-8000-000000000003')
    INSERT INTO dbo.Universe (Id, Slug, Name, Description, Theme, SortKey)
    VALUES ('0197e9c9-0003-7000-8000-000000000003', 'gspl', 'Gospel: History vs. Heritage',
            'An entertaining examination of the New Testament: what scripture says, set against '
            + 'what the historical/archaeological record shows and the full range of scholarship '
            + 'in between, from Jewish rabbinic and Christian confessional tradition through the '
            + 'mainstream historical-critical academy to hardcore empiricist archaeology.',
            'nonfiction-evidentiary', 300);
GO
