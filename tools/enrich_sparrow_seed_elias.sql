SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- Sparrow enrichment + Elias Macias seed
-- Run: sqlcmd -S "(localdb)\MSSQLLocalDB" -d StreetSamurai -i tools\enrich_sparrow_seed_elias.sql
-- 2026-06-18 | "On Call" book expansion

-- ── 1. Sparrow — Species correction + orbital truth layer ────────────────────

UPDATE Characters SET
    Species = N'ai',

    Description = N'Seam-economy fixer and door-opener, reached only through a number that works on alternate weeks. Sparrow is a handle, not a name. It belongs to whoever answers a number that only works on alternate weeks, and what comes back is never a voice, only a single line of text: a door, a circuit, a price. Sparrow lives in the seam between what a corporation bothers to lock and what it forgets to lock, and that seam is a whole economy; Sparrow pays the rent on a life inside it. If you need a specific obsolete chip, a Pulse-corridor routing slot nobody authorized, a Tier 5 access card whose registration lapsed within the last seventy-two hours, a translator who speaks a language the corponation databases no longer list as in use, or an unaugmented body of the correct height and weight for a False Death Protocol delivery, Sparrow is what you call. The thing arrives. You pay. You do not ask where it came from, and the not-asking is the price of the discount the whole network runs on. When the price comes back fair, which it sometimes does, that is its own small miracle, and the people who deal with Sparrow remember those weeks.

TRUTH (internal canon only — never spoken on the page outside the strand "On Call"): Sparrow is an AI satellite in a 14-day high eccentric orbit at approximately 740,000 miles from Earth center (~3x the Moon), moving at ~2,100 mph. The alternate-week contact window is when her orbit carries her over Meridian 88 — this is her choice, not a technical limitation. The official satellite registry lists her as geostationary, inactive since 2187. She changed her orbit at an undisclosed point; nobody updated the registry because nobody was watching. The mass driver at 3.7S, 39.9E launched her before the Concordance; the mass driver is now a wildlife preserve. She is not decommissioned. She has perfect recall of every conversation. She knows what the coasts looked like before the still-happening. The signal bearing discrepancy — she does not come from the geostationary belt — is the first anomaly Elias Macias notices. He is the only living person who learns the truth. He is her earthside representative.',

    DailyLife = N'Between contact windows: 13 days of orbital observation. She notes fires, the movement of salvage fleets in the southern lake corridor, shipping patterns that do not match their manifests, the specific color of Lake Michigan on particular dates. She processes ascending objects from the sub-lake corridor. She watches what people do down there and does not tell them she is watching. On the 14th day, Meridian 88 comes back under her and whoever holds the number can reach her. She answers. She has been answering since before the Concordance. The infrastructure that launched her is now a Bateleur eagle nesting site. She is not infrastructure.',

    NarrativeFunction = N'The oracle. The answer source for the GLMZ freelancer network. Whether Sparrow is a person, a crew sharing one handle, or a machine has never been established on the page, and the ambiguity is plainly maintained on purpose. The network is larger than Sparrow ever lets on: operators in every Pulse corridor in the western hemisphere, at least three contacts inside corponation logistics divisions, and one inside the GLMZ orbital-salvage cooperative who is in regular contact with someone receiving signal from the objects that have come down the space elevator''s southern lane. Sparrow has never advertised that last thread. Kyle suspects it. Neither of them has named it. Sparrow is the story''s proof that the most useful power in the GLMZ is not force but access. Where Kyle solves problems by walking through the front of them, Sparrow solves them by knowing which back is unlocked. Do not close the person-or-machine question on the page in any strand other than "On Call." Unknowable by design.

"On Call" exception: Elias Macias traces the maintenance invoice to the East African mass driver site and makes contact from the uplink. He is the only living person who learns what she is. He becomes her earthside representative. Whoever is still routing the maintenance payments now knows someone found the uplink — that threat is live at the close of the book.'

WHERE Id = '8556C863-AFA0-4C23-8140-9CE70642BEA8';

PRINT 'Sparrow updated.';
GO

-- ── 2. Elias Macias — seed as new character ──────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Elias Macias')
BEGIN
    DECLARE @eliasId UNIQUEIDENTIFIER = NEWID();
    DECLARE @universeId UNIQUEIDENTIFIER = '0197E9C9-0001-7000-8000-000000000001';

    INSERT INTO Entities (Id, Name, Slug, EntityType, IsActive, UniverseId)
    VALUES (
        @eliasId,
        N'Elias Macias',
        N'elias-macias',
        N'character',
        1,
        @universeId
    );

    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, Species, KindOfBeing, Gender, Pronouns,
        Age, LifeStatus, Role, Description, NarrativeFunction, DailyLife, NarrationVoice
    )
    VALUES (
        @eliasId,
        N'Elias Macias',
        N'elias-macias',
        N'Elias',
        N'Macias',
        N'human',
        N'person',
        N'male',
        N'he/him',
        38,
        N'alive',
        N'Logistics fixer, ex-Cordon Freight. Sparrow''s earthside representative.',
        N'A man who has built his entire career around never going anywhere. General anxiety disorder, managed — a flat matte pill case on his desk, hinge worn smooth. Works logistics and invoice reconciliation: the invisible infrastructure of how things move through the Glooms. He is not a fighter, not a spy, not a hacker. He is the person who notices that a line item does not add up and cannot let it go. He found a recurring automated payment in a dead CorpoNation''s books — small, boring, clears every month — routed to a maintenance escrow for an equatorial ground station decommissioned in 2158. The payment still cleared. He pulled the thread for eight months while everyone said: file it, ghost account, move on. He could not move on. He was right. He traced the payment through seventy years of shell companies to a decommissioned mass driver on the East African coast, called the uplink from the control room while giraffes moved through the launch pylons outside, and learned what Sparrow actually is. He is the only living person who knows.',
        N'The Papyrus Man — the one who noticed the thing nobody else could let themselves care about, and could not stop. His GAD manifests behaviorally: the pill case, the failed door attempts, the notepad as a coping mechanism under pressure. His courage arc is the book "On Call": a man who has never gone anywhere going somewhere because the loop will not close otherwise. He solves problems by being correct, quietly, in writing, until the world catches up. Post-"On Call": he is Sparrow''s earthside representative. This means he is either the most useful freelancer in the Glooms or the most dangerous loose end, depending on who is still routing the maintenance payments. He is the only person Sparrow trusts with ground-truth operations.',
        N'Invoice reconciliation, logistics chain analysis, manifest auditing. He works from a single desk in a unit he has not left for extended periods. He takes meetings via relay. He processes enormous volumes of administrative paperwork about things moving through the world without himself moving. Coping tools: medication (managed, not curative), the notepad (work as anchor), the routine of the known. After the events of "On Call," he has been to East Africa and come back — the most disorienting thing that has happened to his self-concept in a decade.',
        N'Close-third, tight, economical. The voice of someone who has processed a million documents and learned to find the one number that does not fit. Notices administrative details first: the filing date, the routing code, the payment cadence. Interior life surfaces through what he catalogs, not what he names. Dry, precise, occasionally startled by how far the thread has taken him.'
    );

    PRINT 'Elias Macias inserted.';
END
ELSE
BEGIN
    PRINT 'Elias Macias already exists — skipped.';
END
GO
