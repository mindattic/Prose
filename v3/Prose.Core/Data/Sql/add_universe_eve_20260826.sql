-- add_universe_eve_20260826.sql
-- ───────────────────────────────────────────────────────────────────────────
-- 8th Universe: EVE (SS-A2 / SS-LAW-15, RFC 0007 "Universe Interchange"). The
-- first non-literary consumer of the Prose engine — a game universe (a
-- collapsing-city survival-horror-parody set the night of Sunday, June 21,
-- 1998) fed to Prose from the sibling ExperimentEve repo via the interchange
-- format (docs/schemas/universe.schema.json). Registering the Universe row is
-- Stage 1 of RFC 0007; the 75-entity import runs separately via
-- `prose --universe-import <path>` once this row exists.
--
-- WorldFacts below is the interchange file's 11 `universe.rules` ids expanded
-- to one line each: the 7 that have full "rule"-type entities in
-- eve.universe.json use that entity's own summary verbatim; the 4 that are
-- referenced by id only (night-deadline, lighthouse-saves, hourly-chimes,
-- drip-feed-blueprints) are one-liners derived from the location/event
-- entities that actually carry that content in the same file (see the
-- "the-night" event, the seven lighthouse "location" entities, and the
-- "soak" character's stencil-blueprint detail).
--
-- Like HORROR/FICTION/EROTICA/GOSPEL, EVE has no matching Universe.<X>Id
-- constant in Universe.cs — none is needed; IUniverseContext is fully
-- DB-driven off this table.
--
-- Idempotent: safe to re-run.
-- ───────────────────────────────────────────────────────────────────────────

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Universe WHERE Id = '0197e9c9-0008-7000-8000-000000000008')
    INSERT INTO dbo.Universe (Id, Slug, Name, Description, Theme, UniversePrimer, WorldFacts, SortKey)
    VALUES (
        '0197e9c9-0008-7000-8000-000000000008',
        'eve',
        'Experiment Eve',
        'The shortest night of the year. The longest night of her life.',
        'eve',
        'An escapee of the Experiment crosses a collapsing city in one real-time night to reach a place called Providence, carrying an unsigned Father''s Day card. Setting: Kingsport, the Quiet Isle -- an unnamed coastal country, Sunday June 21, 1998, 8:00 PM to 5:11 AM. The shortest night of the year. The longest night of her life.',
        N'- No Exposition Dumps -- Everyone already knows what the Experiment was, so nobody explains it. Lore arrives via item descriptions, graffiti, placards, deleted files, and offhand clues only.
- Placeless, Wink-Named -- Never the real state or country; no real place or business names. Every name is a nod with the influence visible. Providence stays -- that IS the wink.
- 1998 Technology Only -- Landlines, pagers, fax lines, caller-ID boxes, dial-up that dies when the phone is picked up, VHS, paper maps, floppy disks. Nothing newer exists.
- Nostalgia Bait, Trademark-Scrubbed -- 1998 saturates everything but no real trademark is ever named. Comedy is dark but ludicrous -- Maniac Mansion tone.
- Father''s Day Is the Through-Line -- June 21, 1998 was Father''s Day; the whole night refracts fatherhood. Never stated. Only accumulated.
- Night Deadline -- The whole game is one real-time night; Kat must reach Providence by dawn (5:11 AM) or the deadline passes.
- Lighthouse Saves -- Save points are lighthouses scattered across the Isle (Billy Island, Thorn Island, Ottertail, Peach Beach, Low Country, Keeper''s Rock, the Point); each also functions as a diegetic landmark, not a menu abstraction.
- Hourly Chimes -- The City Hall bell counts every hour while the world holds still, keeping the real-time clock audible and diegetic.
- Routing Is the Puzzle -- Light puzzles, no fetch quests, no arbitrary combination locks. Blockers are diegetic; exploration is rewarded with the way around.
- Drip-Feed Blueprints -- Weapon/equipment blueprints are discovered gradually through the world (e.g. SOAK''s stencil diagrams double as the flamethrower blueprint) rather than granted all at once.
- The World Is at War With Itself -- STALKER-style A-Life scaled down: species wander, hunt in packs, fight rivals, eat kills, and grow; nests restock the streets; Erasure fights everything.',
        700
    );
GO
