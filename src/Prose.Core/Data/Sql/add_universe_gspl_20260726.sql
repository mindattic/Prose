-- add_universe_gspl_20260726.sql
-- ───────────────────────────────────────────────────────────────────────────
-- 3rd Universe: originally created here as GSPL — "Gospel: History vs.
-- Heritage" (SS-A2 / SS-LAW-15) — RENAMED 2026-08-04 to NONFICTION/nonfiction,
-- broadened from "GSPL specifically" to "any exhaustively-researched, popular
-- narrative nonfiction subject" (the Gospel project remains its first/only
-- book so far). The rename itself was applied as a one-off UPDATE, never
-- saved as its own script — this file is updated in place (2026-08-09) to
-- insert the CURRENT values directly, so running it fresh doesn't resurrect
-- the old GSPL name. A nonfiction-evidentiary universe: entertaining,
-- citation-grounded prose, spectrum-of-scholarship craft rules on top of
-- docs/CRAFT.md — same universe/node/beat schema as GLMZ/SCRY, no schema fork.
--
-- Unlike GLMZ/Fantasy, this universe has no matching Universe.<X>Id constant
-- in Universe.cs — none is needed. Program.cs's Finalize() (added 2026-07-26)
-- eagerly resolves IUniverseContext before any CLI dispatch runs, so every
-- command's universe scoping is DB-driven off this table already; the
-- Universe.GlmzId/FantasyId constants exist only for GLMZ's IsGlmz
-- default-fallback logic and the now-superseded bootstrap fast path
-- (UniverseBootstrap.ResolveWellKnownId), neither of which this universe needs.
--
-- Idempotent: safe to re-run.
-- ───────────────────────────────────────────────────────────────────────────

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Universe WHERE Id = '0197e9c9-0003-7000-8000-000000000003')
    INSERT INTO dbo.Universe (Id, Slug, Name, Description, Theme, SortKey)
    VALUES ('0197e9c9-0003-7000-8000-000000000003', 'nonfiction', 'NONFICTION',
            'Citation-grounded nonfiction research (formerly GSPL/Gospel) - any exhaustively-researched, '
            + 'popular narrative nonfiction subject where every claim traces to a real source; not scoped '
            + 'to religious/historical topics specifically, though that has been every book so far.',
            'nonfiction-evidentiary', 300);
GO
