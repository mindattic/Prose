-- add_universe_gospel_20260812.sql
-- ───────────────────────────────────────────────────────────────────────────
-- 7th Universe: GOSPEL (SS-A2 / SS-LAW-15). A citation-grounded nonfiction
-- universe standing up alongside GLMZ/SCRY/NONFICTION/HORROR/FICTION/EROTICA
-- on the same universe/node/beat schema — no schema fork. Houses the four
-- "Gospel: History vs. Heritage" books (Matthew, Mark, Luke, John), moved out
-- of the shared NONFICTION universe into their own universe so their master
-- glossary (shared historical/theological vocabulary — Pharisee, Sanhedrin,
-- Sabbath, denarius, etc.) doesn't mix with unrelated nonfiction titles.
--
-- Like HORROR/FICTION/EROTICA, GOSPEL has no matching Universe.<X>Id constant
-- in Universe.cs — none is needed; IUniverseContext is fully DB-driven off
-- this table.
--
-- Idempotent: safe to re-run.
-- ───────────────────────────────────────────────────────────────────────────

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Universe WHERE Id = '0197e9c9-0007-7000-8000-000000000007')
    INSERT INTO dbo.Universe (Id, Slug, Name, Description, Theme, SortKey)
    VALUES ('0197e9c9-0007-7000-8000-000000000007', 'gospel', 'GOSPEL',
            'Citation-grounded nonfiction universe for the four Gospel books (Matthew, Mark, Luke, John) -- "History vs. Heritage" comparative biblical history.',
            'gospel', 600);
GO
