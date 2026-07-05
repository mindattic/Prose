SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- HOUSE DRAUGHT — ELDER/FRINGE POPULATION BATCH B  (15 characters)
-- Universe: Cauld  (ID 0197E9C9-0002-7000-8000-000000000002)
-- 2026-07-05 · Idempotent (IF NOT EXISTS guards on all inserts)
-- Categories: Veterans (3) · Oathless (3) · Elderly civilians (3)
--   Failed Transmutation (2) · Bheur-obsessed elders (2) · Paladin/Champion (2)
-- ═══════════════════════════════════════════════════════════════════════════════

-- ── 1. Halvor Drenk ──────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Halvor Drenk')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Halvor Drenk', N'halvor-drenk', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (@id, N'Halvor Drenk', N'halvor-drenk', N'Halvor', N'Drenk', N'', N'human', N'human', N'male', N'he/him', 67, N'alive',
        N'Veteran sub-commander; nineteen engagements survived; cited in officer academies for battlefield positioning.',
        N'Lean and scar-mapped. Known across the fjords for reading a fight in seconds. His name appears in three garrison manuals. No one has noted which spot he occupied in each of those nineteen engagements.',
        N'Draught glory as a structure built on bodies one step ahead of your own.',
        N'No POV.', N'House Draught; Skjoldhavn fjord district', 183, 79, N'lean, wiry',
        N'iron-grey', N'close-cropped', N'short', N'pale blue', N'weathered pale Nordic', N'sun-and-wind roughened',
        N'none', N'completely still; moves only when he has decided where', N'worn campaign leathers; no decorative rank pins',
        N'none',
        N'Reviews engagement maps alone each morning. Accepts no apprentices. Avoids funerals.',
        N'He positioned himself behind a dying man in eleven of his nineteen engagements, always during first contact.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Skjoldhavn fjord district', N'0', N'0',
        N'lean Norse veteran, iron-grey hair, campaign leathers, stone fjord fortress, dark fantasy portrait',
        N'Lean Norse veteran, campaign leathers, iron-grey hair, stone fortress',
        0, 0);
    PRINT N'Halvor Drenk seeded.';
END
ELSE PRINT N'Halvor Drenk already exists.';
GO

-- ── 2. Bryndis Vaelmark ──────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Bryndis Vaelmark')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Bryndis Vaelmark', N'bryndis-vaelmark', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (@id, N'Bryndis Vaelmark', N'bryndis-vaelmark', N'Bryndis', N'Vaelmark', N'', N'human', N'human', N'female', N'she/her', 44, N'alive',
        N'Oathless former sea-raider; courier and intelligence broker in the outer fjords; executed on sight by House patrols.',
        N'Oath-broken at thirty-six after she refused to fire a coastal village her captain had been paid to clear. Runs supply lines no sanctioned Draught trader touches. Executed on sight. Keeps moving.',
        N'Oathless survival inside Draught territory; the permanent cost of a single refusal.',
        N'No POV.', N'House Draught (renounced); outer coastal fjords, no fixed base', 168, 61, N'lean, rope-muscled',
        N'ash-brown', N'loose', N'medium', N'grey-green', N'weathered olive-Nordic', N'salt-roughened',
        N'none', N'low center of gravity; never stands with back to open space', N'undyed wool and canvas; no House colors; hood up by habit',
        N'none',
        N'Rotates six coastal caches. Sleeps in different sites each night. Accepts coin only, never credit.',
        N'She still carries the transit log naming her captain''s payment from a Liturgy magistrate for the clearing.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Outer fjords, no fixed base', N'0', N'0',
        N'Oathless Norse woman, undyed wool hood, coastal fjord cliffs, dark fantasy portrait, hunted',
        N'Oathless Norse woman, undyed wool, coastal fjord cliffs',
        0, 0);
    PRINT N'Bryndis Vaelmark seeded.';
END
ELSE PRINT N'Bryndis Vaelmark already exists.';
GO

-- ── 3. Gudrun Harskeld ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Gudrun Harskeld')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Gudrun Harskeld', N'gudrun-harskeld', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (@id, N'Gudrun Harskeld', N'gudrun-harskeld', N'Gudrun', N'Harskeld', N'', N'human', N'human', N'female', N'she/her', 78, N'alive',
        N'Draught civilian trainer; forty years of daily drills; never placed on an engagement roster.',
        N'Forty years of daily drills have given her a soldier''s body on a civilian''s record. She outperforms recruits half her age. Her absence from every engagement roster has never been formally discussed.',
        N'The gap between martial preparation and combat; what Draught service can mean without the violence.',
        N'No POV.', N'House Draught; Haldvik settlement, inland', 165, 68, N'stocky, muscle-dense',
        N'white', N'braided close', N'short', N'pale grey', N'fair, deeply lined', N'weathered',
        N'none', N'parade-ground straight; never lets it go even at rest', N'practical training wool; nothing decorative',
        N'none',
        N'Wakes before dawn, drills alone, leads two training cohorts. Eats with recruits. Sleeps early.',
        N'Garrison-Prefect Harskeld, her late husband, falsified her medical fitness assessments for eleven consecutive years to keep her off raid rosters.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Haldvik settlement, training compound', N'0', N'0',
        N'elderly Norse woman, white braided hair, training spear, garrison courtyard, dark fantasy portrait',
        N'Elderly Norse woman with spear, garrison courtyard, white braid',
        0, 0);
    PRINT N'Gudrun Harskeld seeded.';
END
ELSE PRINT N'Gudrun Harskeld already exists.';
GO

-- ── 4. Ivar Kolbe ────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ivar Kolbe')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ivar Kolbe', N'ivar-kolbe', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (@id, N'Ivar Kolbe', N'ivar-kolbe', N'Ivar', N'Kolbe', N'', N'human', N'human', N'male', N'he/him', 34, N'alive',
        N'Failed Transmutation survivor; administrative logistics clerk; House doctrine designates him a survivor.',
        N'The Xerum 525 took his left arm''s mobility and left his skin gray-mottled. He did not die. House doctrine calls him a survivor. He keeps ledgers and has been told to be grateful.',
        N'What failed Transmutation looks like from inside House Draught''s framework of gratitude.',
        N'No POV.', N'House Draught; Vordvik garrison town', 177, 74, N'formerly athletic, now asymmetric',
        N'dark blond', N'unkempt', N'short', N'pale brown; left pupil permanently dilated', N'pale with gray mottling', N'damaged; faint chemical scarring',
        N'none (failed Transmutation; left arm rigidity, skin pigmentation damage)',
        N'left shoulder held rigid; compensatory lean rightward',
        N'garrison-issue grey; left sleeve pinned back',
        N'none',
        N'Logs supply inventories at Vordvik. Eats alone. Watches the training yard through the window.',
        N'He volunteered for Transmutation specifically to escape a betrothal his father arranged; he has told no one.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Vordvik garrison town, supply depot', N'0', N'0',
        N'young Norse man, pinned sleeve, gray mottled skin, garrison ledger room, dark fantasy portrait',
        N'Young Norse man, pinned sleeve, gray mottled skin, garrison room',
        0, 0);
    PRINT N'Ivar Kolbe seeded.';
END
ELSE PRINT N'Ivar Kolbe already exists.';
GO

-- ── 5. Ragnhild Mossvik ──────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ragnhild Mossvik')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ragnhild Mossvik', N'ragnhild-mossvik', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (@id, N'Ragnhild Mossvik', N'ragnhild-mossvik', N'Ragnhild', N'Mossvik', N'', N'human', N'human', N'female', N'she/her', 71, N'alive',
        N'Retired Draught civilian; unofficial keeper of the dead-roll; eight thousand names documented over forty years.',
        N'She has written down every Draught casualty she can document — eight thousand names in forty years of letters to garrison commanders. She believes the Bheur leaves a pattern in the dead. It hasn''t shown itself yet.',
        N'Draught''s relationship to death as obsession; whether grief becomes knowledge or remains wish.',
        N'No POV.', N'House Draught; Mossvik coastal village', 160, 62, N'slight, hunched at the shoulders',
        N'white', N'loose, unkempt', N'long', N'dark brown', N'pale, dry, papery', N'deeply lined',
        N'none', N'perpetually bent over her work; slow shuffling gait', N'dark undyed wool; ink-stained hands and cuffs',
        N'none',
        N'Writes names. Sends letters to garrisons requesting casualty lists. Eats when she remembers to.',
        N'She has deliberately omitted twenty-three names from her ledger — men and women she decided deserved to be forgotten.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Mossvik coastal village, personal residence', N'0', N'0',
        N'elderly Norse woman, white loose hair, ink-stained hands, candlelit stone room, dark fantasy portrait',
        N'Elderly Norse woman, white loose hair, ink-stained hands, candlelit room',
        0, 0);
    PRINT N'Ragnhild Mossvik seeded.';
END
ELSE PRINT N'Ragnhild Mossvik already exists.';
GO

-- ── 6. Ulf Bonecrown ─────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ulf Bonecrown')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ulf Bonecrown', N'ulf-bonecrown', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (@id, N'Ulf Bonecrown', N'ulf-bonecrown', N'Ulf', N'Bonecrown', N'', N'human', N'human', N'male', N'he/him', 48, N'alive',
        N'Champion; northern fjord perimeter commander; twenty-three years post-ascension.',
        N'Eight feet tall, bone-plated at the shoulders, no hair left after twenty-three years of slow change. What remains of the man who walked into the infusion chamber can still recognize the smell of woodsmoke.',
        N'The terminal cost of Transmutation — what Champion-rank extracts from continuous selfhood.',
        N'No POV.', N'House Draught; Bonecrown Hold, far northern fjords', 244, 198, N'massive, post-human; bone-plated',
        N'none', N'none', N'none', N'white, no visible iris', N'pale grey, thickened hide', N'post-human',
        N'Pronounced post-human (Champion); bone-plate growth at shoulders and crown, full depigmentation, no hair',
        N'slow and deliberate; the ground carries his weight audibly',
        N'open-front iron-banded leather; nothing else fits',
        N'Bone-plating at shoulders and crown; altered pigmentation; no remaining hair; white irises',
        N'Patrols northern perimeter. Eats raw quantities twice daily. Does not sleep inside buildings.',
        N'His belt pouch contains a carved driftwood toy his son made at age four, unopened for eleven years.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Northern fjord perimeter, Bonecrown Hold', N'0', N'0',
        N'massive post-human Norse Champion, bone-plated shoulders, white eyes, fjord snowfield, dark fantasy portrait',
        N'Massive post-human Champion, bone-plated, white eyes, northern fjord',
        0, 0);
    PRINT N'Ulf Bonecrown seeded.';
END
ELSE PRINT N'Ulf Bonecrown already exists.';
GO

-- ── 7. Skarde Halvkeld ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Skarde Halvkeld')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Skarde Halvkeld', N'skarde-halvkeld', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (@id, N'Skarde Halvkeld', N'skarde-halvkeld', N'Skarde', N'Halvkeld', N'', N'human', N'human', N'male', N'he/him', 62, N'alive',
        N'Senior Draught officer; seventeen engagements; personally funds and cultivates the medical detail of every unit he commands.',
        N'Decorated five times for valor. The medics assigned to his unit receive better rations, better quarters, and more personal loyalty than the officers above them. He has needed their full attention three times.',
        N'Survival through institutional cultivation as self-interest; Draught honor worn inward for personal preservation.',
        N'No POV.', N'House Draught; Skarveld garrison, eastern fjord', 188, 91, N'broad, well-maintained',
        N'reddish-grey', N'close-cut', N'short', N'blue', N'ruddy Nordic', N'weathered',
        N'none', N'commanding; occupies the center of any room without appearing to try', N'campaign leathers with five honor-pins; always clean',
        N'none',
        N'Reviews unit medical supplies personally. Inspects medic quarters weekly. Runs morning drills with officers.',
        N'He bribed the unit medic at Skarveld Crossing with grain-rights; four soldiers died while she prioritized him.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Skarveld garrison and eastern fjord patrol zone', N'0', N'0',
        N'broad Norse officer, reddish-grey hair, five honor-pins, garrison hall, dark fantasy portrait',
        N'Broad Norse officer, five honor-pins, garrison hall, reddish hair',
        0, 0);
    PRINT N'Skarde Halvkeld seeded.';
END
ELSE PRINT N'Skarde Halvkeld already exists.';
GO

-- ── 8. Astrid Fjordbreak ─────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Astrid Fjordbreak')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Astrid Fjordbreak', N'astrid-fjordbreak', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (@id, N'Astrid Fjordbreak', N'astrid-fjordbreak', N'Astrid', N'Fjordbreak', N'', N'human', N'human', N'female', N'she/her', 41, N'alive',
        N'Oathless former Knight; trades Liturgy intelligence for food and passage through non-Draught harbors.',
        N'The Transmutation gave her a Knight''s strength, then her captain ordered her to fire a hamlet with children still inside. She refused. Her oath was revoked on the shore. She left her sword there.',
        N'Where Draught''s moral floor is; what a Knight''s body does when it outlasts the House that made it.',
        N'No POV.', N'House Draught (renounced); coastal borderlands, non-Draught harbors', 178, 82, N'athletic, Knight-dense',
        N'red-brown', N'roughly cut', N'medium', N'amber', N'weathered pale', N'weathered',
        N'Subtle height gain (Knight); compact muscle density, slightly elevated body temperature',
        N'tactical readiness at rest; reads exits before faces',
        N'stripped of all House markings; roughspun over mail scraps',
        N'Knight-ascension: compact muscle density, elevated resting temperature, slight stature gain',
        N'Trades Liturgy patrol route intelligence for shelter. Moves every four days. No fixed camp.',
        N'She knows where the hamlet''s survivors relocated and has refused three Liturgy offers to sell that location.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Coastal borderlands, non-Draught harbors', N'0', N'0',
        N'Oathless Norse woman Knight, red-brown hair, stripped armor, coastal borderlands, dark fantasy portrait',
        N'Oathless Norse woman, stripped armor, red-brown hair, coastal cliffs',
        0, 0);
    PRINT N'Astrid Fjordbreak seeded.';
END
ELSE PRINT N'Astrid Fjordbreak already exists.';
GO

-- ── 9. Leif Volgrimsen ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Leif Volgrimsen')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Leif Volgrimsen', N'leif-volgrimsen', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (@id, N'Leif Volgrimsen', N'leif-volgrimsen', N'Leif', N'Volgrimsen', N'', N'human', N'human', N'male', N'he/him', 80, N'alive',
        N'Master close-combat trainer; never placed on a raid roster; has concealed this for fifty-eight years.',
        N'The finest spear instructor in three districts. Hundreds of his students have died in raids he never attended. He teaches them everything except the thing he understands best: how fear keeps you home.',
        N'Draught martial culture as performance; what happens when preparation replaces participation for a lifetime.',
        N'No POV.', N'House Draught; Volgrimsen training compound, inland', 176, 74, N'lean, aged but functional',
        N'white', N'thin and loose', N'short', N'faded blue', N'fair, deeply lined', N'dry, lined',
        N'none', N'instructor''s bearing; corrects others'' posture reflexively even in conversation', N'training wool, practical, always clean',
        N'none',
        N'Teaches four sessions daily. Reviews student records at night. Drinks alone when training ends.',
        N'At twenty-two he faked illness to avoid his first raid by pressing hot iron to his palm through cloth.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Volgrimsen training compound and surrounding settlement', N'0', N'0',
        N'elderly Norse man, white hair, training spear, garrison practice yard, dark fantasy portrait',
        N'Elderly Norse man, white hair, training spear, practice yard',
        0, 0);
    PRINT N'Leif Volgrimsen seeded.';
END
ELSE PRINT N'Leif Volgrimsen already exists.';
GO

-- ── 10. Ingrid Skarven ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ingrid Skarven')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ingrid Skarven', N'ingrid-skarven', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (@id, N'Ingrid Skarven', N'ingrid-skarven', N'Ingrid', N'Skarven', N'', N'human', N'human', N'female', N'she/her', 29, N'alive',
        N'Failed Transmutation survivor; reports monthly for re-evaluation; has waited four years for a promised second infusion.',
        N'The Xerum took her left eye and the hearing in her right ear in the same convulsion. She was promised a second infusion four years ago. She still reports monthly in case today is the day.',
        N'False institutional hope as prolonged cruelty; what Draught does with those it has already spent.',
        N'No POV.', N'House Draught; Skarven recruiting station', 170, 65, N'compact, still athletic',
        N'dark brown', N'shaved left, loose right', N'short left, medium right', N'dark brown (right); left eye scarred-closed', N'pale Nordic with burn-patterned scarring along left jaw', N'scarred',
        N'none (failed Transmutation; left eye loss, partial right-ear deafness)',
        N'right side forward; compensates for blind left; angles toward doorways',
        N'recruit-grade wool; no rank markings despite four years of waiting',
        N'none',
        N'Reports monthly for evaluation. Trains with recruits. Helps candidates prepare; does not explain the odds.',
        N'The recruiter who approved her Transmutation was her uncle; she has never told the evaluation board.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Skarven recruiting station and barracks', N'0', N'0',
        N'young Norse woman, scarred jaw, shaved left side, recruit wool, garrison, dark fantasy portrait',
        N'Young Norse woman, scarred jaw, shaved left side, recruit wool',
        0, 0);
    PRINT N'Ingrid Skarven seeded.';
END
ELSE PRINT N'Ingrid Skarven already exists.';
GO

-- ── 11. Ragnar Veidh ─────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ragnar Veidh')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ragnar Veidh', N'ragnar-veidh', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (@id, N'Ragnar Veidh', N'ragnar-veidh', N'Ragnar', N'Veidh', N'', N'human', N'human', N'male', N'he/him', 74, N'alive',
        N'Retired sea-captain; sole survivor of a forty-one-person crew; convinced the Bheur chose him specifically.',
        N'His entire crew — forty-one people — died in an ambush at the Skjold channel mouth. He swam two miles in winter sea and walked out alone. He has not stopped asking himself why.',
        N'Survivor guilt made theological; the Bheur as a framework for making death mean something about the living.',
        N'No POV.', N'House Draught; Skjoldhavn fjord district, retired shoreline cottage', 181, 83, N'gaunt, formerly powerful',
        N'white', N'loose, unkempt', N'long', N'pale grey', N'weathered, deep-lined, cold-reddened', N'weathered',
        N'none', N'stands at shorelines; watches the water; seems always to be waiting for something', N'old sea-captain''s wool; salt-weathered; kept functional but never replaced',
        N'none',
        N'Walks to the shore at dawn. Sits. Returns. Eats little. Repeats the ambush''s sequence to anyone who will listen.',
        N'He was below deck relieving himself when the ambush began; he never told any of the forty-one families this.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Skjoldhavn fjord district, shoreline cottage', N'0', N'0',
        N'gaunt elderly Norse sea-captain, white loose hair, fjord shoreline, grey dawn, dark fantasy portrait',
        N'Gaunt elderly Norse captain, white loose hair, fjord shoreline dawn',
        0, 0);
    PRINT N'Ragnar Veidh seeded.';
END
ELSE PRINT N'Ragnar Veidh already exists.';
GO

-- ── 12. Thora Grimsal ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Thora Grimsal')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Thora Grimsal', N'thora-grimsal', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (@id, N'Thora Grimsal', N'thora-grimsal', N'Thora', N'Grimsal', N'', N'human', N'human', N'female', N'she/her', 52, N'alive',
        N'Paladin; four post-ascension engagements; led the infusion cohort that produced her rank and three corpses.',
        N'The Xerum killed the three candidates beside her in the infusion chamber. She walked out changed. She has been called courageous for going first. She went first because she calculated the odds favored her.',
        N'Self-interest wearing the face of sacrifice; what Transmutation''s lottery looks like when someone thinks it through.',
        N'No POV.', N'House Draught; Grimsal Hold, coastal cliffs', 186, 93, N'powerful, Paladin-dense',
        N'silver-blonde', N'close-cropped', N'short', N'pale gold (Transmutation effect)', N'pale, faintly luminous', N'clear, slightly otherworldly',
        N'Evident enhancement (Paladin); increased density, pale gold irises, veins visible at temples under stress',
        N'completely still under pressure; relaxed authority at rest',
        N'Draught campaign heavy leather; Paladin-rank iron shoulder clasps',
        N'Paladin-ascension: increased bone and muscle density, pale gold iris pigmentation, elevated healing rate',
        N'Leads coastal patrol units. Reviews officers'' plans before engagements. Runs the infusion lottery twice annually.',
        N'She went first because she calculated her infusion odds were better than the three candidates beside her; all three died.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Grimsal Hold and coastal patrol zone', N'0', N'0',
        N'powerful Norse Paladin woman, silver-blonde hair, gold eyes, coastal cliff fortress, dark fantasy portrait',
        N'Norse Paladin woman, silver hair, gold eyes, coastal cliff fortress',
        0, 0);
    PRINT N'Thora Grimsal seeded.';
END
ELSE PRINT N'Thora Grimsal already exists.';
GO

-- ── 13. Gunnar Solvik ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Gunnar Solvik')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Gunnar Solvik', N'gunnar-solvik', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (@id, N'Gunnar Solvik', N'gunnar-solvik', N'Gunnar', N'Solvik', N'', N'human', N'human', N'male', N'he/him', 58, N'alive',
        N'Draught sub-officer; fifteen engagements; has declined three field promotions; survival method is unremarkable on purpose.',
        N'Decorated as reliable and solid. He has declined three field promotions citing health concerns that resolved within a week of each engagement. No one has noted the pattern. He is depending on that.',
        N'Survival as practiced mediocrity; what Draught warriors actually do when they understand what rank costs.',
        N'No POV.', N'House Draught; Solviken garrison', 180, 88, N'solid, unremarkable',
        N'grey-brown', N'short', N'short', N'grey', N'ruddy', N'weathered',
        N'none', N'neither commanding nor servile; calibrated to disappear into any room', N'standard sub-officer kit; deliberately kept without additional rank markers',
        N'none',
        N'Executes orders competently. Volunteers for perimeter over assault positions. Maintains a record of middling accomplishment.',
        N'He keeps a list of every sub-officer who accepted promotion in his district; sixteen of them are dead.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Solviken garrison and surrounds', N'0', N'0',
        N'unremarkable Norse sub-officer, grey-brown hair, garrison tunic, stone barracks hall, dark fantasy portrait',
        N'Unremarkable Norse sub-officer, grey hair, garrison barracks hall',
        0, 0);
    PRINT N'Gunnar Solvik seeded.';
END
ELSE PRINT N'Gunnar Solvik already exists.';
GO

-- ── 14. Sigrid Oarvik ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Sigrid Oarvik')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Sigrid Oarvik', N'sigrid-oarvik', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (@id, N'Sigrid Oarvik', N'sigrid-oarvik', N'Sigrid', N'Oarvik', N'', N'human', N'human', N'female', N'she/her', 55, N'alive',
        N'Oathless former raider; declared Oathless at thirty-two for attacking a Liturgy envoy during a taking.',
        N'She broke an envoy''s jaw and two fingers before the guards restrained her. Her brother was loaded onto the Liturgy barge while she was on the ground. She has been Oathless for twenty-three years.',
        N'The Liturgy taking as a breaking point; Oathless status as the cost of an involuntary refusal.',
        N'No POV.', N'House Draught (renounced); northern fjord borderlands, non-Draught harbors', 172, 70, N'raider-lean',
        N'dark red', N'pulled back', N'medium', N'dark brown', N'weathered pale Nordic', N'weathered',
        N'none', N'coiled; reads hands before faces; watches for restraint gestures', N'stripped House markings; salvaged raider gear, layered',
        N'none',
        N'Works coastal escort for coin. Stays in non-Draught harbors. Has twice tried to trace her brother through Liturgy contacts.',
        N'She received word three years ago that her brother died of fever in Liturgy service; she continues looking anyway.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Northern fjord borderlands, non-Draught harbors', N'0', N'0',
        N'Oathless Norse woman, dark red hair, salvaged raider gear, fjord harbor night, dark fantasy portrait',
        N'Oathless Norse woman, dark red hair, salvaged armor, fjord harbor',
        0, 0);
    PRINT N'Sigrid Oarvik seeded.';
END
ELSE PRINT N'Sigrid Oarvik already exists.';
GO

-- ── 15. Helga Wormvik ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Helga Wormvik')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Helga Wormvik', N'helga-wormvik', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (@id, N'Helga Wormvik', N'helga-wormvik', N'Helga', N'Wormvik', N'', N'human', N'human', N'female', N'she/her', 84, N'alive',
        N'Draught civilian elder; trained her whole life; keeps a private ledger of officer decisions that caused preventable deaths.',
        N'Eighty-four years and every joint working. She has outlived three cohorts of Draught fighters. She trains alone at dawn and in the evening reviews a private document she has not shown anyone.',
        N'Accumulated civilian knowledge against institutional doctrine; what Draught suppresses in favor of aggression.',
        N'No POV.', N'House Draught; Wormvik coastal settlement', 162, 64, N'compact, stringy-muscled',
        N'white', N'pinned back', N'short', N'dark amber', N'deeply lined, weathered pale', N'deeply lined',
        N'none', N'straight; moves without wasted motion; carries a short blade at her hip by habit', N'practical daily wool; no ornamentation; always carries a short blade',
        N'none',
        N'Tends a small garden. Trains alone at dawn. Reviews her private ledger by lamplight each evening.',
        N'Her ledger names seventeen officers whose decisions caused preventable deaths; the garrison''s reporting channel is commanded by one of them.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Wormvik coastal settlement', N'0', N'0',
        N'elderly Norse woman, white pinned hair, short blade at hip, coastal stone cottage, dark fantasy portrait',
        N'Elderly Norse woman, white hair, short blade, coastal stone cottage',
        0, 0);
    PRINT N'Helga Wormvik seeded.';
END
ELSE PRINT N'Helga Wormvik already exists.';
GO
