SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- HOUSE DRAUGHT — ACTIVE POPULATION BATCH A  (35 characters)
-- Universe: Cauld  (ID 0197E9C9-0002-7000-8000-000000000002)
-- 2026-07-04 · Idempotent (IF NOT EXISTS guards on all inserts)
-- Categories: Myrmidons (7) · Scrying operators (5) · Transmutation (4)
--   Scholars / archivists (4) · Artisans / engineers (4) · Merchants (2)
--   Minor nobles (3) · Liturgy contacts (3) · Spies (3)
-- ═══════════════════════════════════════════════════════════════════════════════

-- ── 1. Ingrid Halvardsdóttir ─────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ingrid Halvardsdóttir')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ingrid Halvardsdóttir', N'ingrid-halvardsdottir', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext,
        SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Ingrid Halvardsdóttir', N'ingrid-halvardsdottir', N'Ingrid', N'Halvardsdóttir', N'',
        N'human', N'human', N'female', N'she/her',
        20, N'alive',
        N'Junior Myrmidon; first raid season; promoted into her predecessor''s post under circumstances she has not disclosed.',
        N'Ingrid Halvardsdóttir joined the garrison the same autumn her elder brother took the infusion and died from it. She drills harder than anyone in her intake class and says nothing about why. She came from a fishing settlement on the outer fjords where everyone trains because it is understood to be required, and she has carried that understanding into service like a tool she forgets she is holding. Her unit leader considers her reliable. She is. She is also the reason the unit leader''s predecessor is listed as missing rather than dead, and she has decided this is a thing she can live with.',
        N'The question of what a person will carry and what they will say, and whether those are the same list.',
        N'No POV.',
        N'House Draught; fishing settlement, outer northern fjords',
        168, 62, N'lean, athletic, not yet fully hardened',
        N'dark brown', N'tight braid', N'medium',
        N'grey-green', N'fair with wind-burn', N'clear, slightly reddened at the cheeks',
        N'none',
        N'upright garrison posture; moves with the economical efficiency of someone trying not to waste anything',
        N'standard Myrmidon garrison kit; no personal ornamentation',
        N'none',
        N'Dawn drills, weapons maintenance, unit briefings, evening watch rotations. She fills free hours with additional training rather than conversation.',
        N'She killed a ranking sergeant who was breaking during a night raid — put her spear through him from behind and let the tide account for it. He was posted missing and received a posthumous commendation. She was promoted into his post. She checks the garrison commendation board sometimes, where his name is carved, and feels nothing she has a word for.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Garrison barracks and training grounds, northern fjord',
        N'0', N'0',
        N'young Norse woman, garrison tunic, tight dark braid, grey-green eyes, stone fjord fortress interior, dark fantasy portrait, Buehlman register',
        N'A young Norse woman in garrison tunic, dark hair tightly braided, grey-green eyes, stone fjord fortress interior, expression controlled and exhausted, dark fantasy portrait',
        0, 0
    );
    PRINT N'Ingrid Halvardsdóttir seeded.';
END
ELSE PRINT N'Ingrid Halvardsdóttir already exists.';
GO

-- ── 2. Torsten Blackwave ─────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Torsten Blackwave')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Torsten Blackwave', N'torsten-blackwave', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext,
        SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Torsten Blackwave', N'torsten-blackwave', N'Torsten', N'Blackwave', N'',
        N'human', N'human', N'male', N'he/him',
        23, N'alive',
        N'Myrmidon corporal; three raid seasons; known for steadiness under pressure.',
        N'Torsten Blackwave has been on three raid seasons and has the build to show for it — broad, scarred at the shoulder, quick on wet decks. He is a corporal because he is steady and because he does not panic, which is the thing his officers value most and the thing he finds easiest to supply. What he finds harder: the names. He tallied them after the fog-navigation error, from things the dead were carrying — seventeen names and six blanks, written on the inside of his boot sole in his own hand. He does not consider himself a man haunted by the past. He considers himself a man with a list.',
        N'The gap between the official version of an event and what the people who were there carry out of it.',
        N'No POV.',
        N'House Draught; sea-raid fleet, northern coastline',
        182, 85, N'broad-shouldered, raid-hardened, scarred at the left shoulder',
        N'black', N'cropped close', N'short',
        N'dark brown', N'olive-fair', N'weathered, salt-scored',
        N'none',
        N'solid balance-stance from years on wet decks; speaks and moves without wasted motion',
        N'practical raid gear; wool and oilskin; no ornamentation except a worn ring on the right hand',
        N'none',
        N'Raid-season deployments, navigation drills, unit leadership duties. In garrison he is steady and speaks less than the others expect.',
        N'His unit burned a fishing settlement during a fog navigation error — wrong fjord, wrong village, twenty-three dead. He tallied names from what the dead were carrying: a tally stick, a sewing kit, a prayer token. He has seventeen names and six blanks written on the inside of his boot sole. He addresses them sometimes, in the dark, when the ship is quiet. He does not consider this prayer.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Northern coastline, sea-raid fleet and garrison rotations',
        N'0', N'0',
        N'Norse man, broad shoulders, black hair cropped, scarred, oilskin raid gear, fjord sea background, dark fantasy portrait, Buehlman register',
        N'A broad-shouldered Norse man in oilskin raid gear, black hair cropped, a scar at the left shoulder, standing on a fjord deck in grey weather, expression steady and private, dark fantasy portrait',
        0, 0
    );
    PRINT 'Torsten Blackwave seeded.';
END
ELSE PRINT 'Torsten Blackwave already exists.';
GO

-- ── 3. Skarde Veltunsen ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Skarde Veltunsen')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Skarde Veltunsen', N'skarde-veltunsen', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext,
        SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Skarde Veltunsen', N'skarde-veltunsen', N'Skarde', N'Veltunsen', N'',
        N'human', N'human', N'male', N'he/him',
        28, N'alive',
        N'Myrmidon corporal; eligible for infusion two years running; has twice deferred on administrative pretexts.',
        N'Skarde Veltunsen has been eligible for the infusion for two years. He is competent, respected, and has twice manufactured administrative reasons to delay the appointment — a false injury report, a fabricated equipment shortage that needed his supervision. He is good at this. He is good at most things involving logistics, which is how he has explained away the delays to his officers, who are not paying close enough attention. What he has not explained to anyone: he was present when his closest friend died during infusion at nineteen. He watched what happened in the room. He has a word for what he saw. He does not use it.',
        N'The difference between cowardice and knowledge, and whether it matters which one is true.',
        N'No POV.',
        N'House Draught; garrison barracks, inland fjord post',
        179, 80, N'rangy, deliberate, more capable than he looks',
        N'ash blond', N'loose, slightly unkempt', N'shoulder-length',
        N'pale blue', N'fair, freckled', N'clear',
        N'none',
        N'measured, careful; tends to stay near walls in unfamiliar spaces',
        N'standard garrison kit, kept functional rather than presentable',
        N'none',
        N'Patrol rotations and unit logistics; eligible for infusion scheduling, which he has twice deferred on administrative grounds. He eats alone most evenings.',
        N'He was present when his closest friend died during the infusion at nineteen. The official record called it a clean failure. Skarde saw the senior practitioner administer a second measure when the first was not resolving — not a rescue attempt, a decision made in the room without explanation. He has never said what he saw. He does not know if it was murder or mercy, and he has not found a way to ask.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Garrison barracks and patrol routes, inland fjord territory',
        N'0', N'0',
        N'lean ash-blond Norse man, garrison tunic, pale blue eyes, stone fjord garrison interior, dark fantasy portrait, watchful expression',
        N'A lean ash-blond Norse man in garrison tunic, pale blue eyes, standing in a stone corridor, expression watchful and self-contained, dark fantasy portrait style',
        0, 0
    );
    PRINT 'Skarde Veltunsen seeded.';
END
ELSE PRINT 'Skarde Veltunsen already exists.';
GO

-- ── 4. Hild Ormsdóttir ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Hild Ormsdóttir')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Hild Ormsdóttir', N'hild-ormsdottir', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext,
        SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Hild Ormsdóttir', N'hild-ormsdottir', N'Hild', N'Ormsdóttir', N'',
        N'human', N'human', N'female', N'she/her',
        25, N'alive',
        N'Myrmidon sergeant; border patrol unit; trusted by officers and unit alike; asset for House Vael.',
        N'Hild Ormsdóttir runs her unit with the compressed efficiency of someone who has stopped wasting anything — time, words, goodwill. She is an excellent sergeant. Her officers trust her patrol decisions. Her soldiers follow her into difficult ground without being told why. She was captured at a border crossing three years ago and released within four days, unharmed, which she told the debrief was because of an extraction error on the enemy''s part. This is not what happened. She passes patrol information to a House Vael contact at a border market three times a year. She has told herself her loyalty to Draught and her loyalty to her brother can both be true.',
        N'The architecture of a divided loyalty and how long it holds before it becomes a trap.',
        N'No POV.',
        N'House Draught; northern border garrison',
        171, 68, N'compact, very fit, moves with practiced economy',
        N'red-brown', N'tight practical bun', N'medium',
        N'dark hazel', N'fair', N'clear, minimal scarring',
        N'none',
        N'rapid, efficient movement; stillness when listening; the posture of someone who has learned to take up the exact space she needs',
        N'garrison sergeant''s kit, worn precisely; single iron bead on the braid, personal',
        N'none',
        N'Unit command, patrol scheduling, officer briefings. Three times a year she visits a border market on personal time, which her officer has noted and not questioned.',
        N'She was turned by House Vael three years ago after they captured and released her without explanation, and simultaneously freed her brother who had been taken to Sphere 31 on a House Draught disciplinary order. She passes patrol rotations and approach routes to a Vael contact three times per year. They have never harmed what she loves. She has told herself this means they are honorable. She knows this logic and knows what it costs her.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Northern border garrison, patrol routes, border market',
        N'0', N'0',
        N'compact Norse woman, red-brown hair in bun, hazel eyes, garrison sergeant kit, stone border fortress, dark fantasy portrait, sharp expression',
        N'A compact Norse woman in garrison sergeant kit, red-brown hair in a tight bun, hazel eyes, standing in a stone border fortress, expression sharp and composed, dark fantasy portrait',
        0, 0
    );
    PRINT N'Hild Ormsdóttir seeded.';
END
ELSE PRINT N'Hild Ormsdóttir already exists.';
GO

-- ── 5. Ketil Svensson ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ketil Svensson')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ketil Svensson', N'ketil-svensson', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext,
        SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Ketil Svensson', N'ketil-svensson', N'Ketil', N'Svensson', N'Ser',
        N'human', N'human', N'male', N'he/him',
        32, N'alive',
        N'Knight; mid-grade unit officer; three years post-infusion; official record clean; actual infusion history is not.',
        N'Ketil Svensson is a Knight three years past his infusion, with the build and eye-change to match. He leads his unit with the economy of someone who has been through enough that most things no longer cost him anything he can name. He is widely considered one of the garrison''s better mid-grade officers. What he manages privately is the awareness that his clean official infusion record is not his — that the practitioner who wrote it down could tell a different story — and that the practitioner in question has, once, over a formal dinner, said something quiet and precise that he has been unable to stop turning over since.',
        N'What it means to owe someone your life and have that debt held without explicit terms.',
        N'No POV.',
        N'House Draught; northern fjord garrison',
        190, 108, N'powerfully built; Knight-enhanced frame; clearly post-infusion',
        N'dark blond', N'cropped close', N'very short',
        N'pale grey', N'weathered tan', N'clear, minimal lines',
        N'Subtle height gain and increased density characteristic of a Knight-level infusion survivor. Eyes pale to an atypical degree; pupils track a fraction faster than baseline.',
        N'controlled, unhurried; the deliberate movement of someone aware of their own size and its effect on a room',
        N'officer''s tunic over reinforced underlayer; functional, not ceremonial',
        N'Knight-level infusion survivor; enhanced strength and frame density; accelerated ocular tracking',
        N'Unit command, officer duties, occasional consultation on post-infusion conditioning for younger Myrmidons. He maintains a deliberate distance from Mistress Valgerd Crucibleborn that he has not explained to anyone.',
        N'His first infusion failed — he died on the table for approximately forty seconds before the senior practitioner, Valgerd Crucibleborn, resuscitated him using an unauthorized technique she has never formally documented. His official record reflects a clean success. Valgerd has never spoken of it directly. Once, over dinner, she said quietly that she sometimes considered what it would mean to undo something one had worked very hard to preserve. He has been thinking about what she meant for two years.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Northern fjord garrison, officer''s quarters and patrol command',
        N'0', N'0',
        N'tall powerfully built Norse knight, dark blond cropped hair, pale grey eyes, knight''s tunic, fjord fortress great hall, dark fantasy portrait, post-infusion physique',
        N'A tall powerfully built Norse man in a knight''s tunic, dark blond hair cropped close, pale grey eyes with an unusual precision to them, standing in a fjord fortress great hall, dark fantasy portrait',
        0, 0
    );
    PRINT 'Ketil Svensson seeded.';
END
ELSE PRINT 'Ketil Svensson already exists.';
GO

-- ── 6. Ragnhild Stormborn ────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ragnhild Stormborn')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ragnhild Stormborn', N'ragnhild-stormborn', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext,
        SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Ragnhild Stormborn', N'ragnhild-stormborn', N'Ragnhild', N'Stormborn', N'',
        N'human', N'human', N'female', N'she/her',
        19, N'alive',
        N'Junior Myrmidon; recently arrived; distinguished by performing every task correctly and saying almost nothing.',
        N'Ragnhild Stormborn arrived at the garrison four months ago and has since distinguished herself by drilling correctly, eating without complaint, and saying almost nothing. The intake officers noted on her file that she is quiet and considered this a promising trait. She is not quiet because she is careful or disciplined; she is quiet because she reported her father to the garrison for Oathless contact in the belief that a formal inquiry would protect him, and he was executed within a week. She found out through a supply roster change. His name disappeared. His bunk was reassigned. No one told her directly. She has not spoken voluntarily since, except to answer questions.',
        N'The cost of doing the right thing for the wrong outcome, and whether you can tell the difference from inside it.',
        N'No POV.',
        N'House Draught; new intake barracks, northern fjord',
        165, 59, N'slight, wiry, not yet the build of a soldier',
        N'strawberry blonde', N'loose, unstyled', N'long',
        N'blue', N'very fair', N'clear, still young',
        N'none',
        N'correct garrison posture; moves without spontaneity; the stillness of someone who has stopped initiating',
        N'standard intake kit, unmodified',
        N'none',
        N'Drills, bunk assignments, garrison routine. She performs all of it correctly and speaks only when spoken to.',
        N'She reported her father to the garrison for Oathless contact because she believed a formal inquiry would protect him from extrajudicial handling. He was executed inside the week. No one told her directly; she found out when his name disappeared from the supply roster and his bunk was reassigned. She has not spoken voluntarily since. She does not know if she would do it differently. She has not let herself finish that thought.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Intake barracks, northern fjord garrison',
        N'0', N'0',
        N'young Norse woman, strawberry blonde loose hair, blue eyes, intake garrison tunic, stone barracks, dark fantasy portrait, blank expression',
        N'A young Norse woman in intake garrison tunic, strawberry blonde hair loose, blue eyes, standing in stone barracks, expression blank and internal, dark fantasy portrait',
        0, 0
    );
    PRINT 'Ragnhild Stormborn seeded.';
END
ELSE PRINT 'Ragnhild Stormborn already exists.';
GO

-- ── 7. Björn Áskelsson ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Björn Áskelsson')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Björn Áskelsson', N'bjorn-askelsson', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext,
        SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Björn Áskelsson', N'bjorn-askelsson', N'Björn', N'Áskelsson', N'',
        N'human', N'human', N'male', N'he/him',
        26, N'alive',
        N'Myrmidon squad leader; sea-raid specialist; carrying evidence of a general''s war crimes in his bedroll.',
        N'Björn Áskelsson leads a sea-raid squad that his commanding officer describes as effective and that Björn would describe, privately, as still alive. He is a careful leader — not cautious, careful, which is different — and has the open-water instincts that come from three seasons and the willingness to think. He is also carrying something that has been changing what his instincts are for: a Knight''s personal journal found in a burned farmhouse during a river campaign eight months ago, documenting in the Knight''s own handwriting that a sitting general ordered civilian killings and had them attributed to Oathless. Björn has kept the journal in his bedroll. He is trying to decide whether having it makes him powerful or simply dead.',
        N'Intelligence as weight: the moment information becomes too dangerous to hold and too valuable to surrender.',
        N'No POV.',
        N'House Draught; sea-raid fleet, western approaches',
        186, 92, N'tall, athletic, raid-hardened',
        N'red-blond', N'braided', N'medium',
        N'grey-blue', N'fair, sea-weathered', N'lined at the eyes from squinting into salt wind',
        N'none',
        N'open-water ease; reads distance and movement instinctively; sleeps light',
        N'raid gear, oilskin, wool; belt knife worn; no ceremony',
        N'none',
        N'Squad command, raid planning, sea-route reconnaissance. He sleeps in his bedroll and has not allowed it to be inspected since the river campaign.',
        N'During a river campaign he found a Knight''s personal journal in the wreckage of a burned farmhouse. The journal documents, in that Knight''s own handwriting, that General Hrafnkel Draught ordered the killing of a civilian settlement and had the deaths attributed to an Oathless cell. The Knight whose journal it was died in the same campaign, officially in battle. Björn has kept the journal in his bedroll for eight months. He does not know if the information makes him powerful or marks him as someone who needs to disappear.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Sea-raid fleet, western fjord approaches, garrison rotations',
        N'0', N'0',
        N'tall red-blond Norse man, braided hair, grey-blue eyes, oilskin raid gear, fjord seascape, dark fantasy portrait, alert expression',
        N'A tall red-blond Norse man in oilskin raid gear, hair braided, grey-blue eyes, standing on a fjord deck against grey sky, expression alert and privately troubled, dark fantasy portrait',
        0, 0
    );
    PRINT N'Björn Áskelsson seeded.';
END
ELSE PRINT N'Björn Áskelsson already exists.';
GO

-- ── 8. Sigrid Maalvikdóttir ──────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Sigrid Maalvikdóttir')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Sigrid Maalvikdóttir', N'sigrid-maalvikdottir', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext,
        SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Sigrid Maalvikdóttir', N'sigrid-maalvikdottir', N'Sigrid', N'Maalvikdóttir', N'',
        N'human', N'human', N'female', N'she/her',
        34, N'alive',
        N'Senior Scrying operator; twelve years of service; has been removing a specific woman from Liturgy extraction records for seven years.',
        N'Sigrid Maalvikdóttir has been operating Scrying installations for twelve years and is considered one of the division''s most reliable senior observers — methodical, never logged an anomalous incident, never submitted a session report requiring clarification. What the reports no longer reflect: she has been removing a specific set of Sphere 31 coordinates from indexable records for seven years. A woman. A teacher. A daughter now twelve years old. Sigrid does not know her name. She has given her one internally. She cannot say with certainty when the observation became protection, or what she is protecting the woman from. She continues because stopping would require deciding.',
        N'Attachment formed across a one-way window, and what the person on the visible side of the glass owes the person on the other.',
        N'No POV.',
        N'House Draught; Scrying Installation 7, northern headland',
        173, 66, N'lean, precise in movement, slightly hollow at the eyes from long sessions',
        N'dark', N'pinned back neatly', N'long',
        N'dark brown', N'medium olive', N'clear, fine lines at the eyes',
        N'none',
        N'controlled, deliberate; very still when observing; reads other people''s attention carefully',
        N'Scrying division uniform, practical; no ornamentation',
        N'none',
        N'Long installation observation sessions, session log maintenance, trainee supervision. She arrives early and is the last to leave most days. She edits certain session logs after the trainees are gone.',
        N'She has been observing the same woman in Sphere 31 for seven years — a teacher, living an ordinary life, with a daughter now twelve. Sigrid has been systematically removing her coordinates from the Liturgy''s extraction index — making her invisible to prioritization. She does not know the woman''s name. She has given her one internally. She is not certain when the observation became protection or what exactly she is doing. She is certain she will continue.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Scrying Installation 7, northern headland; Scrying division offices',
        N'0', N'0',
        N'Norse woman, dark hair pinned, dark eyes, Scrying installation chamber, soft apparatus light, dark fantasy portrait, intent expression',
        N'A Norse woman with dark hair pinned back, dark eyes, seated at a Scrying installation apparatus in soft stone-chamber light, expression intent and private, dark fantasy portrait',
        0, 0
    );
    PRINT N'Sigrid Maalvikdóttir seeded.';
END
ELSE PRINT N'Sigrid Maalvikdóttir already exists.';
GO

-- ── 9. Eirik Thornwall ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Eirik Thornwall')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Eirik Thornwall', N'eirik-thornwall', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext,
        SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Eirik Thornwall', N'eirik-thornwall', N'Eirik', N'Thornwall', N'',
        N'human', N'human', N'male', N'he/him',
        27, N'alive',
        N'Scrying operator; three years on the inner fjord installation; has encoded a message through the membrane and is waiting to see if anything changes.',
        N'Eirik Thornwall has worked the inner fjord installation for three years. He is thorough and technically capable and tends to stay late in the observation bay for reasons that have shifted, over the past year, from professional investment to something he would struggle to name. A year of regular sessions fixed on one apartment window in Sphere 31. Three months ago he encoded a message in the calibration harmonics of the apparatus and directed it at that window. He does not know if the membrane transmits harmonic information. He checks the window every session. The apartment shows signs of habitation. He logs nothing about the window.',
        N'What it means to reach toward someone who cannot know you are there, and what you are actually reaching for.',
        N'No POV.',
        N'House Draught; Scrying Installation 4, inner fjord',
        177, 76, N'average build, slightly hollow around the eyes from long sessions',
        N'light brown', N'roughly cut', N'short',
        N'grey-green', N'pale', N'clear, faintly shadowed under the eyes',
        N'none',
        N'slightly forward-leaning when engaged; retreats into stillness when uncertain; tends to arrive at conversations a half-step behind',
        N'Scrying division uniform, worn without attention to appearance',
        N'none',
        N'Observation sessions, calibration maintenance, log filing. He has been staying late in the observation bay for a year without logging overtime.',
        N'Three months ago he encoded a message in the calibration harmonics of the inner fjord installation apparatus, directed at a specific apartment window in Sphere 31 he had been regularly observing for a year. He does not know whether harmonic encoding carries through the membrane. He does not know what the message would mean to whoever received it, if anyone did. He checks the window every session and logs nothing about it.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Scrying Installation 4, inner fjord; installation maintenance rooms',
        N'0', N'0',
        N'young Norse man, light brown hair, grey-green eyes, Scrying installation chamber, soft light, dark fantasy portrait, searching expression',
        N'A young Norse man with light brown hair and grey-green eyes at a Scrying installation apparatus in soft stone-chamber light, expression searching and inward, dark fantasy portrait',
        0, 0
    );
    PRINT 'Eirik Thornwall seeded.';
END
ELSE PRINT 'Eirik Thornwall already exists.';
GO

-- ── 10. Leif Daggerfjord ─────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Leif Daggerfjord')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Leif Daggerfjord', N'leif-daggerfjord', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext,
        SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Leif Daggerfjord', N'leif-daggerfjord', N'Leif', N'Daggerfjord', N'',
        N'human', N'human', N'male', N'he/him',
        22, N'alive',
        N'Junior Scrying operator; second year; watched a murder in Sphere 31 where the killer looked back through the apparatus for eleven seconds.',
        N'Leif Daggerfjord is in his second year of installation work, which is early enough that most of what he observes still carries weight. He is diligent and has not yet developed the practiced detachment that smooths everything into data. This is relevant because of what he saw three months ago: a man in Sphere 31 killed in his kitchen, and the killer turning to look directly at the Scrying point for eleven seconds afterward — not at random, directly. Leif logged it as atmospheric interference. He has told no one. He has not been able to reach full observation depth since. He is not sure whether he is afraid because someone can see through or because he thinks someone already did.',
        N'The moment an observer realizes the apparatus works both ways, and what they do with that.',
        N'No POV.',
        N'House Draught; Scrying Installation 7, northern headland',
        175, 72, N'average, slightly tense in the shoulders',
        N'dirty blond', N'unkempt', N'medium',
        N'light grey', N'fair', N'clear, slightly drawn',
        N'none',
        N'tense at the shoulders; avoids eye contact in conversation; sits with his back to walls when he can',
        N'junior Scrying division uniform, kept correct but not neat',
        N'none',
        N'Scheduled observation sessions, junior calibration work, log filing. He has not achieved full observation depth in three months and has not reported this.',
        N'During a routine observation session three months ago, he watched a man in Sphere 31 be murdered in his own kitchen — and the killer turned and looked directly at the Scrying point for eleven seconds before leaving. Not at random: directly, steadily, for eleven seconds. He logged the session as atmospheric interference. He has told no one. He cannot reach full observation depth now. He is not certain whether he is afraid because someone on the other side can see through the apparatus, or because he thinks they already did and chose to let him know.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Scrying Installation 7, northern headland',
        N'0', N'0',
        N'young Norse man, dirty blond hair, light grey eyes, Scrying chamber, uneasy expression, dark fantasy portrait',
        N'A young Norse man with dirty blond hair and light grey eyes at a Scrying installation, expression uneasy and avoidant, stone chamber, dark fantasy portrait',
        0, 0
    );
    PRINT 'Leif Daggerfjord seeded.';
END
ELSE PRINT 'Leif Daggerfjord already exists.';
GO

-- ── 11. Astrid Vonn ──────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Astrid Vonn')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Astrid Vonn', N'astrid-vonn', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext,
        SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Astrid Vonn', N'astrid-vonn', N'Astrid', N'Vonn', N'',
        N'human', N'human', N'female', N'she/her',
        30, N'alive',
        N'Military intelligence extraction specialist, Scrying division; has been selling extracted Sphere 31 patterns to an Oathless intermediary for eight months.',
        N'Astrid Vonn runs military intelligence extraction from Sphere 31 observation — identifying usable technical and tactical information in recorded sessions and translating it into reports for the officer corps. She is good at this. She has also been selling extracted patterns to an Oathless intermediary for eight months, in sessions arranged through a border market contact. She asked to be paid in Cauld coin. She was paid in Sphere 31 currency she cannot spend. The intermediary told her she would understand eventually. She has been thinking about that answer for six months. She is beginning to think the currency''s uselessness was the information itself, and she does not know what question it answers.',
        N'The moment someone realizes they have been shaped by the transaction rather than the other way around.',
        N'No POV.',
        N'House Draught; military intelligence division, garrison headquarters',
        169, 61, N'composed, self-contained, precise in presentation',
        N'ash blonde', N'tight braid', N'medium',
        N'light brown', N'pale gold', N'clear, professional',
        N'none',
        N'controlled posture; does not show agitation; speaks deliberately',
        N'intelligence division uniform, impeccably kept',
        N'none',
        N'Military intelligence extraction sessions, report preparation, officer briefings. Border market visits arranged around her regular schedule and logged as personal time.',
        N'She has been selling the Sphere 31 military patterns she extracts to an Oathless intermediary for eight months. She asked to be paid in Cauld coin. She was paid in Sphere 31 currency she cannot use. The intermediary told her she would understand eventually. She has been thinking about that answer for six months. She is beginning to believe the currency''s uselessness was not an insult or an error but the actual message — and she does not know what it is answering.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Garrison headquarters intelligence division, border market routes',
        N'0', N'0',
        N'Norse woman, ash blonde braid, light brown eyes, garrison intelligence office, dark fantasy portrait, composed expression',
        N'A Norse woman with ash blonde hair in a tight braid, light brown eyes, seated in a garrison intelligence office, expression composed and privately calculating, dark fantasy portrait',
        0, 0
    );
    PRINT 'Astrid Vonn seeded.';
END
ELSE PRINT 'Astrid Vonn already exists.';
GO

-- ── 12. Gunnar Ironsight ─────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Gunnar Ironsight')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Gunnar Ironsight', N'gunnar-ironsight', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext,
        SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Gunnar Ironsight', N'gunnar-ironsight', N'Gunnar', N'Ironsight', N'',
        N'human', N'human', N'male', N'he/him',
        38, N'alive',
        N'Veteran Scrying operator; now primarily a trainer; has never described the thing he killed inside the installation twenty-two years ago.',
        N'Gunnar Ironsight trained for installation work at nineteen and has been in the division in some capacity for sixteen years, the last six spent primarily training new operators. He is a thorough instructor — systematic about procedure, specific about what operators might encounter in a solo session. He has a detailed list of hazards. He has never added to it the thing he encountered at twenty-two: something that appeared within the installation room itself, not through the membrane but between the apparatus components. He killed it, or believes he did. He covered the marks on the stone with an equipment rack that has not been moved in sixteen years.',
        N'The experienced person who withholds the most important thing, and what it costs every new person who walks into the room without knowing.',
        N'No POV.',
        N'House Draught; Scrying installation training facility, inner fjord',
        183, 88, N'solid, still strong, the build of a man who has worked physically his whole life',
        N'iron grey', N'cropped close', N'very short',
        N'dark grey', N'weathered fair', N'lined, experienced',
        N'none',
        N'solid, deliberate; takes up space without effort; slow to turn but fast when he does',
        N'training division uniform, worn correctly; equipment-marked hands',
        N'none',
        N'Operator training, session supervision, installation procedure documentation. He does not conduct solo sessions himself anymore and has not for six years.',
        N'Twenty-two years ago, during a solo session, something appeared inside the installation room — not through the membrane, within the room, between the apparatus components. He killed it, or believes he did. He covered the marks on the stone with an equipment rack that has not been moved since. He has trained operators for a decade. He describes, in careful detail, every hazard a solo operator might face. He has never described this one.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Scrying installation training facility, inner fjord; installation chambers',
        N'0', N'0',
        N'weathered Norse man, iron grey cropped hair, dark grey eyes, Scrying installation chamber, dark fantasy portrait, heavy experienced expression',
        N'A weathered Norse man with iron grey cropped hair and dark grey eyes standing in a Scrying installation chamber, expression heavy and experienced, dark fantasy portrait',
        0, 0
    );
    PRINT 'Gunnar Ironsight seeded.';
END
ELSE PRINT 'Gunnar Ironsight already exists.';
GO

-- ── 13. Mistress Valgerd Crucibleborn ────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Valgerd Crucibleborn')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Valgerd Crucibleborn', N'valgerd-crucibleborn', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext,
        SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Valgerd Crucibleborn', N'valgerd-crucibleborn', N'Valgerd', N'Crucibleborn', N'Mistress',
        N'human', N'human', N'female', N'she/her',
        42, N'alive',
        N'Senior transmutation practitioner; eighteen years of service; has deliberately killed two candidates she judged too dangerous for the House to hold once transformed.',
        N'Valgerd Crucibleborn has administered Xerum 525 infusions for eighteen years. She has guided seven survivors and is credited with six failures, which is considered a reasonable record. She is sought out by officers who want their candidates handled well. She is precise and unhurried and does not tell candidates things she does not believe. She also keeps a locked notebook in a personal chest that describes, in her own handwriting, two infusion failures she arranged deliberately — candidates she judged too dangerous for the House to hold once transformed. She overdosed them precisely. She documented her reasoning. She has considered destroying the notebook for five years and has not.',
        N'The practitioner who believes she is protecting her institution while committing the act that would destroy her within it.',
        N'No POV.',
        N'House Draught; transmutation laboratory, garrison headquarters',
        167, 64, N'deliberate in movement, controlled posture, hands always still',
        N'silver-streaked dark', N'twisted back', N'long',
        N'dark brown', N'olive, lined', N'experienced, minimal expression at rest',
        N'none',
        N'unhurried, precise; speaks in measured sentences; does not gesture when she can avoid it',
        N'practitioner''s coat over dark underlayer; functional, professional; a single locked case she carries personally',
        N'none',
        N'Infusion preparation, candidate assessment, procedure administration, post-infusion monitoring. She checks the lock on her personal chest every morning before leaving her quarters.',
        N'She has deliberately failed two strong infusion candidates by overdosing them on Xerum 525 precisely enough to ensure failure — both judged to be too capable and too volatile for the House to safely hold once transformed. Both deaths are credited as natural failures in the official record. She keeps a notebook documenting each decision and her reasoning, locked in a personal chest. She has considered destroying it for five years. She has not. She has also resuscitated one candidate — Ketil Svensson — using an unauthorized technique after a genuine failure, and holds that debt quietly.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Transmutation laboratory, garrison headquarters; practitioner''s quarters',
        N'0', N'0',
        N'older Norse woman, silver-streaked dark hair twisted back, dark eyes, transmutation laboratory, practitioner''s coat, dark fantasy portrait, precise expression',
        N'An older Norse woman with silver-streaked dark hair twisted back, dark eyes, standing in a transmutation laboratory in a practitioner''s coat, expression precise and controlled, dark fantasy portrait',
        0, 0
    );
    PRINT 'Valgerd Crucibleborn seeded.';
END
ELSE PRINT 'Valgerd Crucibleborn already exists.';
GO

-- ── 14. Master Orm Haldurssen ────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Orm Haldurssen')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Orm Haldurssen', N'orm-haldurssen', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext,
        SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Orm Haldurssen', N'orm-haldurssen', N'Orm', N'Haldurssen', N'Master',
        N'human', N'human', N'male', N'he/him',
        29, N'alive',
        N'Transmutation practitioner; second year working with Xerum 525; has been self-dosing diluted compound for six months.',
        N'Orm Haldurssen has been working with Xerum 525 for two years and has developed a reputation for careful preparation work and an unusual interest in the compound''s pharmacological properties beyond their primary application. He is engaged and curious in a way his seniors find professionally appropriate. They do not know that the specific curiosity driving most of his study is a self-experiment he began six months ago: very small dilutions of Xerum 525, dosed twice weekly, produce a sustained cognitive enhancement he has been documenting under the heading of professional pharmacological observation. He has not identified the accumulation threshold. He is doing excellent work and does not want to stop.',
        N'The experiment that works until it doesn''t, and the person who knows the risk and has decided to keep going.',
        N'No POV.',
        N'House Draught; transmutation laboratory, garrison headquarters',
        176, 78, N'sharp, slightly hectic energy, precise hands',
        N'light brown', N'neatly combed', N'short',
        N'hazel', N'fair', N'clear, slightly bright-eyed',
        N'none',
        N'quick and precise; tends to think faster than he speaks; checks his own hands periodically without appearing to',
        N'practitioner''s coat over clean underlayer; always neat',
        N'none',
        N'Compound preparation, infusion support, pharmacological documentation. He doses himself twice weekly from a personal preparation he keeps labeled as a dilution standard.',
        N'Six months ago he discovered that very small dilutions of Xerum 525 produce a sustained cognitive enhancement — faster recall, heightened pattern recognition — without triggering transformation. He has been self-dosing twice weekly since. He keeps a private log framed as professional pharmacological observation. He has not identified the accumulation threshold. He does not know whether the enhancement is genuinely cognitive improvement or a symptom of early compound interaction. He is doing the best work of his career and has decided this takes priority over answering those questions.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Transmutation laboratory and preparation rooms, garrison headquarters',
        N'0', N'0',
        N'young Norse man, light brown hair, hazel eyes, transmutation preparation room, practitioner''s coat, dark fantasy portrait, alert and slightly wired expression',
        N'A young Norse man in a practitioner''s coat with light brown hair and hazel eyes in a transmutation preparation room, expression alert and slightly wired, dark fantasy portrait',
        0, 0
    );
    PRINT 'Orm Haldurssen seeded.';
END
ELSE PRINT 'Orm Haldurssen already exists.';
GO

-- ── 15. Freyja Stonepath ─────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Freyja Stonepath')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Freyja Stonepath', N'freyja-stonepath', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext,
        SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Freyja Stonepath', N'freyja-stonepath', N'Freyja', N'Stonepath', N'',
        N'human', N'human', N'female', N'she/her',
        24, N'alive',
        N'First-year transmutation trainee; applied specifically to survive her own infusion; has found one real factor and one false one and cannot tell them apart.',
        N'Freyja Stonepath applied to the transmutation division with a plan, which is unusual — most candidates are assigned. She applied specifically because she intends to survive her own infusion, and she believed that working as a practitioner was the path to developing a preparation protocol that would make that possible. She has spent three years on it. The protocol''s central mechanism is wrong. She has also accidentally identified a genuine contributing factor alongside the false one, and she cannot distinguish which of her ideas is real. She plans to self-infuse before she is assigned a candidate. She is twenty-four years old and believes she is being scientific.',
        N'Confidence in a theory that is half right, and what happens when the wrong half is the one you acted on.',
        N'No POV.',
        N'House Draught; transmutation laboratory, garrison headquarters',
        164, 58, N'small, precise, energetic at rest',
        N'auburn', N'loose waves', N'medium',
        N'bright green', N'fair', N'clear, intent',
        N'none',
        N'forward-leaning when engaged; moves quickly; rarely fully still',
        N'trainee''s coat over working underlayer; tends to have compound stains on the cuffs',
        N'none',
        N'Compound preparation assistance, training protocol work, independent study. She fills her evenings with preparation research and her mornings with self-monitoring she logs in a private format.',
        N'She applied to the transmutation division specifically in order to survive her own infusion — a plan three years in development. She has identified what she believes is a preparation protocol that improves survival odds. The protocol''s central mechanism is wrong. She has also accidentally stumbled on a genuine contributing factor, which sits alongside the false theory in her notes, and she cannot distinguish which one is real. She plans to self-infuse before she is assigned a candidate. She has not told anyone. She believes she is being scientific and that the distinction matters.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Transmutation laboratory and preparation rooms, garrison headquarters',
        N'0', N'0',
        N'young Norse woman, auburn hair, green eyes, transmutation preparation room, trainee coat, dark fantasy portrait, focused expression',
        N'A young Norse woman in a trainee''s coat, auburn hair, bright green eyes, in a transmutation preparation room, expression focused and privately determined, dark fantasy portrait',
        0, 0
    );
    PRINT 'Freyja Stonepath seeded.';
END
ELSE PRINT 'Freyja Stonepath already exists.';
GO

-- ── 16. Halvor Brokenchain ───────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Halvor Brokenchain')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Halvor Brokenchain', N'halvor-brokenchain', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext,
        SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Halvor Brokenchain', N'halvor-brokenchain', N'Halvor', N'Brokenchain', N'',
        N'human', N'human', N'male', N'he/him',
        33, N'alive',
        N'Transmutation practitioner, advisory capacity after surviving his own unauthorized partial self-infusion; believes the effect he felt was a hallucination; it was not.',
        N'Halvor Brokenchain works in the transmutation division in an advisory capacity his superiors describe as a sensible use of his knowledge while they decide what he is now. The accident: eighteen months ago he survived what he told the investigation was accidental Xerum 525 exposure. The investigation accepted this. The truth is an unauthorized partial self-infusion he barely survived. The larger secret is that the partial effect was real — he achieved Knight-level physical state for approximately three hours before the incomplete transformation reversed. He believes he hallucinated it. He has not tried again. The experiment was structurally sound, and he does not know this.',
        N'A discovery that is real but was never confirmed, and the person who is the only one who could have confirmed it.',
        N'No POV.',
        N'House Draught; garrison infirmary and transmutation recovery wing',
        178, 82, N'slightly stooped, still strong, carries himself carefully',
        N'dark brown shot with premature grey', N'longer than regulation', N'medium',
        N'dark brown', N'weathered', N'lined earlier than his age, left pupil slightly irregular',
        N'Left pupil slightly irregular in shape — a residual of the partial infusion. Fine tremor in the left hand under physical stress.',
        N'careful, deliberate; manages the left hand tremor with practiced attention; does not call attention to it',
        N'advisory coat, worn loosely; practical underlayer',
        N'Residual of partial Xerum 525 infusion: left pupil irregularity, intermittent fine tremor in left hand under stress; no transformation achieved',
        N'Advisory consultations, written technical reports, occasional supervised preparation work. He has learned which hours of the day the left-hand tremor is least visible and arranges his schedule accordingly.',
        N'He conducted an unauthorized partial self-infusion eighteen months ago and survived the reversal. He told the investigation it was accidental exposure and was believed. The deeper secret: the partial effect was real — he achieved Knight-level physical state for approximately three hours before the incomplete transformation reversed on its own. He is certain he hallucinated it. He has not tried again. He does not know the experiment was structurally sound. He does not know that the adjuvant he was missing is something he could find.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Garrison infirmary and transmutation recovery wing, garrison headquarters',
        N'0', N'0',
        N'Norse man, dark hair shot with early grey, irregular left pupil, advisory coat, garrison recovery wing, dark fantasy portrait, careful expression',
        N'A Norse man in an advisory coat, dark hair with early grey, a slightly irregular left pupil, standing in a garrison recovery wing, expression careful and internally preoccupied, dark fantasy portrait',
        0, 0
    );
    PRINT 'Halvor Brokenchain seeded.';
END
ELSE PRINT 'Halvor Brokenchain already exists.';
GO

-- ── 17. Mistress Gudrun Pale ─────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Gudrun Pale')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Gudrun Pale', N'gudrun-pale', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext,
        SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Gudrun Pale', N'gudrun-pale', N'Gudrun', N'Pale', N'Mistress',
        N'human', N'human', N'female', N'she/her',
        36, N'alive',
        N'Genealogical archivist; found that the House''s most celebrated commander is not bloodline-eligible; has been sitting on this for eleven months and is now receiving anonymous notes.',
        N'Gudrun Pale has been the House''s genealogical archivist for nine years and is very good at it — precise, methodical, protective of the records in a way her colleagues describe as bordering on religious. Eleven months ago she found something in a century-old intake register that she has been unable to put back. Dame Sigurlaug Haraldsdóttir, the House''s most celebrated living commander, was adopted during the famine and is not bloodline-eligible for her rank. The records were altered by a House official who has been dead for twelve years. Three weeks ago Gudrun began receiving anonymous notes. They cite the specific archive shelf and folio. Someone else has found it.',
        N'What happens when a secret starts receiving correspondence — and whether knowing becomes irreversible.',
        N'No POV.',
        N'House Draught; genealogical archive, garrison headquarters',
        166, 60, N'soft, very still, precise in small gestures',
        N'pale blonde', N'in a bun', N'long',
        N'light blue', N'very fair', N'clear, quiet',
        N'none',
        N'very still at rest; moves between tasks without announcing herself; tends to be in the room before people notice',
        N'archivist''s work coat over plain underlayer; ink on the right hand most days',
        N'none',
        N'Archive work, genealogical records maintenance, intake log cataloguing. She visits the specific shelf and folio periodically without pulling the record. She checks it to make sure it is still there.',
        N'Eleven months ago she found that Dame Sigurlaug Haraldsdóttir — the House''s most celebrated living commander — was adopted during the famine of 4917 and is not bloodline-eligible for her rank. The intake records were altered by a House official dead for twelve years. Exposing this would collapse a key military alliance Sigurlaug personally holds together. Gudrun has not disclosed it. Three weeks ago she began receiving anonymous notes that cite the exact archive shelf and folio number where the intake record sits. Someone else has found it, and they are letting her know they know she found it first.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Genealogical archive, garrison headquarters',
        N'0', N'0',
        N'pale Norse woman, pale blonde hair in bun, light blue eyes, archive room, dark fantasy portrait, still and watchful expression',
        N'A pale Norse woman with pale blonde hair in a bun and light blue eyes in an archive room surrounded by records, expression still and watchful, dark fantasy portrait',
        0, 0
    );
    PRINT 'Gudrun Pale seeded.';
END
ELSE PRINT 'Gudrun Pale already exists.';
GO

-- ── 18. Master Ivar Manuscriptsson ───────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ivar Manuscriptsson')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ivar Manuscriptsson', N'ivar-manuscriptsson', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext,
        SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Ivar Manuscriptsson', N'ivar-manuscriptsson', N'Ivar', N'Manuscriptsson', N'Master',
        N'human', N'human', N'male', N'he/him',
        31, N'alive',
        N'Military historian and campaign archivist; has been removing pages from the official record for two years to write a secret history of the House''s manufactured victories.',
        N'Ivar Manuscriptsson is the House''s campaign archivist, which involves maintaining the official military record and which has, for the past two years, involved something additional he has not disclosed. He has been removing pages from the campaign archive slowly enough that no audit has flagged the gaps. The pages document the interior of victories: reattributed field decisions, fabricated engagements, borrowed tactical records from dead officers. He is writing a secret history. He intends to send it to a neutral scholarly house outside the Seven. He has eighty-three pages and no safe route to send them. He has not stopped.',
        N'The archivist who becomes the crime he is documenting, one removed page at a time.',
        N'No POV.',
        N'House Draught; campaign archive, garrison headquarters',
        180, 79, N'slight scholar''s build, hunched at the shoulders from desk work',
        N'brown', N'parted, slightly disheveled', N'medium',
        N'grey', N'pale indoor', N'clear, ink-stained fingers',
        N'none',
        N'hunched forward over work; straightens when spoken to; tends to have one hand in a document at all times',
        N'archivist''s coat, ink-marked; plain underlayer',
        N'none',
        N'Campaign archive management, official chronicle writing, cataloguing. He works late, which everyone attributes to diligence.',
        N'He has been systematically removing pages from the House''s campaign archive for two years — slowly enough that no audit has flagged the gaps. The removed pages document how the House''s victories were manufactured: reattributed field decisions, invented engagements, tactical records borrowed from dead officers and reassigned to living ones. He is writing a secret history from the removed pages. He intends to send it to a neutral scholarly house outside the Seven. He has eighty-three pages. He has no route to send them yet and has not stopped writing.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Campaign archive, garrison headquarters',
        N'0', N'0',
        N'slight Norse man, brown hair, grey eyes, archive room, archivist coat, dark fantasy portrait, hunched industrious expression',
        N'A slight Norse man in an archivist''s coat, brown hair, grey eyes, surrounded by campaign records in a stone archive room, expression industrious and internally absorbed, dark fantasy portrait',
        0, 0
    );
    PRINT 'Ivar Manuscriptsson seeded.';
END
ELSE PRINT 'Ivar Manuscriptsson already exists.';
GO

-- ── 19. Mistress Solveig Greymantle ──────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Solveig Greymantle')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Solveig Greymantle', N'solveig-greymantle', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext,
        SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Solveig Greymantle', N'solveig-greymantle', N'Solveig', N'Greymantle', N'Mistress',
        N'human', N'human', N'female', N'she/her',
        40, N'alive',
        N'Membrane scholar; most capable theorist in the House research division; has calculated that cumulative Scrying use is fracturing the membrane over generational timescales; burned the paper; is writing it again.',
        N'Solveig Greymantle has been studying Scrying theory and membrane behavior for fifteen years and is the most technically capable scholar in the House''s research division. She is invited, on occasion, to consult for the Liturgy, which she treats as a professional courtesy both parties find useful. She is careful about what she shares. More specifically: she has calculated that cumulative installation use across all Seven Houses is introducing progressive micro-fractures into the membrane. She wrote the paper. She burned it. She is writing the second version more conservatively and is afraid the conservative version is the optimistic one.',
        N'The person who knows the largest true thing in the room and has no one safe to tell.',
        N'No POV.',
        N'House Draught; membrane studies division, inner fjord',
        170, 65, N'deliberate, slightly distracted air; very precise when she focuses',
        N'iron grey', N'loose knot', N'medium',
        N'dark grey', N'olive-pale', N'clear, finely lined',
        N'none',
        N'deliberate, measured; tends to stop mid-sentence when a thought catches her; returns to the sentence',
        N'scholar''s coat over plain underlayer; personal notebook always present',
        N'none',
        N'Membrane theory study, Scrying data review, occasional Liturgy consultation. She is writing something in a private journal she has told colleagues is a theoretical paper. She has been writing it for four months. She has burned one version.',
        N'She has calculated that cumulative Scrying installation use across all Seven Houses is introducing micro-fractures into the membrane — not detectable by any individual House but progressive and cumulative. Her extrapolation puts membrane coherence failure between sixty and ninety years out. She wrote the paper. She burned it. She is writing the second version with more conservative assumptions. She is afraid the conservative version is the optimistic one. She does not know who she could tell. She suspects that whoever she told would first need to decide whether they wanted to know.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Membrane studies division, inner fjord; Liturgy consultation visits',
        N'0', N'0',
        N'older Norse woman, iron grey hair in a loose knot, dark grey eyes, research chamber, scholar coat, dark fantasy portrait, preoccupied expression',
        N'An older Norse woman with iron grey hair in a loose knot and dark grey eyes in a research chamber, surrounded by Scrying diagrams, expression preoccupied and privately burdened, dark fantasy portrait',
        0, 0
    );
    PRINT 'Solveig Greymantle seeded.';
END
ELSE PRINT 'Solveig Greymantle already exists.';
GO

-- ── 20. Thora Bookbinding ────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Thora Bookbinding')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Thora Bookbinding', N'thora-bookbinding', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext,
        SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Thora Bookbinding', N'thora-bookbinding', N'Thora', N'Bookbinding', N'',
        N'human', N'human', N'female', N'she/her',
        28, N'alive',
        N'Junior archivist; found her family''s name in a century-old membrane transit record; has been spending time near the Scrying division for five months, waiting for access she doesn''t have.',
        N'Thora Bookbinding works in the general archive, cataloguing the House''s older administrative records — a role that is unglamorous and largely invisible, which suits her for reasons that have also been invisible. Five months ago, cataloguing a century-old labor intake register, she found three entries bearing her own family name: relatives sent through the membrane to Sphere 31 as part of a disciplinary action against her great-grandfather. They would be old or dead, but their descendants might not be. She has spent five months trying to find a way to use Scrying installation logs to locate where they were sent. She needs access she does not have. She has been spending time near the Scrying division, being helpful, waiting for someone to ask her why.',
        N'The person who knows the record exists and is trying to get to it by becoming indispensable to the people who hold the keys.',
        N'No POV.',
        N'House Draught; general archive, garrison headquarters',
        163, 57, N'slight, unhurried, easy to overlook',
        N'honey brown', N'loose braid', N'long',
        N'warm brown', N'fair-tan', N'clear',
        N'none',
        N'quiet, self-effacing; very observant; tends to be in the corner of a room rather than the center',
        N'archivist''s work coat over plain underlayer; tends to carry a cataloguing ledger',
        N'none',
        N'Cataloguing, record maintenance, general archive support. She has found reasons to spend time near the Scrying division office for five months, offering to file, to carry, to assist.',
        N'While cataloguing a century-old labor intake register she found three entries bearing her own family name — relatives sent through the membrane to Sphere 31 as part of a disciplinary action against her great-grandfather. They would be old or dead by now, but their descendants might not be. She has spent five months attempting to cross-reference Scrying installation session logs to identify what coordinates correspond to where they were sent. She needs access to restricted records she does not hold. She has been becoming helpful to the Scrying division for five months and has not yet found anyone to ask.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'General archive and garrison headquarters; Scrying division offices (adjacent)',
        N'0', N'0',
        N'slight Norse woman, honey brown braid, warm brown eyes, archive room, dark fantasy portrait, observant quiet expression',
        N'A slight Norse woman with a honey brown braid and warm brown eyes in a stone archive room, expression observant and quietly purposeful, dark fantasy portrait',
        0, 0
    );
    PRINT 'Thora Bookbinding seeded.';
END
ELSE PRINT 'Thora Bookbinding already exists.';
GO

-- ── 21. Master Ragnar Hammerfall ─────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ragnar Hammerfall')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ragnar Hammerfall', N'ragnar-hammerfall', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext,
        SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Ragnar Hammerfall', N'ragnar-hammerfall', N'Ragnar', N'Hammerfall', N'Master',
        N'human', N'human', N'male', N'he/him',
        35, N'alive',
        N'Master smith for Sphere 31 weapon replication; embeds a stress fracture point in every officer-grade weapon he produces; three officers have died from it so far.',
        N'Ragnar Hammerfall is the garrison''s master smith for Sphere 31 weapon replication — the person who takes what the Scrying operators extract and makes it real in the forge. He is skilled and experienced and his work is trusted entirely, which is the relevant fact. The other relevant fact is that for the last four years he has been embedding a stress fracture point into every officer-grade edged weapon he produces — at the junction of the guard and the tang, invisible to inspection, catastrophic under maximum combat stress. Three officers whose weapons he made have died in the field. He considers this an accounting. He has not decided where the list ends.',
        N'Justice administered without authority, and the point at which an accounting becomes something else.',
        N'No POV.',
        N'House Draught; armory and forge, garrison headquarters',
        188, 102, N'massive, forge-built, heavily scarred hands',
        N'dark red', N'thick, short', N'short',
        N'grey-brown', N'weathered ruddy', N'heavily lined, forge-scored',
        N'none',
        N'deliberate, heavy; moves like someone who has learned to be careful around hot metal and has applied this to everything else; speaks in short sentences',
        N'forge apron over work underlayer; no ornamentation; hands always marked',
        N'none',
        N'Forge work, Sphere 31 weapon adaptation, weapons production, officer commissions. He fulfills every commission to specification. The specification includes the flaw.',
        N'For four years he has been embedding a stress fracture point into every officer-grade edged weapon he produces — positioned at the junction of the guard and the tang, invisible under standard inspection, catastrophic under maximum combat stress. Three officers whose weapons he made have died in the field in the last four years. He keeps a list of what each officer did. He considers this an accounting. He has not decided where the list ends, which means the list is still active.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Armory and forge, garrison headquarters; officer weapons commissioning',
        N'0', N'0',
        N'massive Norse smith, dark red hair, scarred hands, forge apron, garrison armory, dark fantasy portrait, deliberate expression',
        N'A massive Norse smith in a forge apron with dark red hair and heavily scarred hands standing in a garrison armory, expression deliberate and privately weighing something, dark fantasy portrait',
        0, 0
    );
    PRINT 'Ragnar Hammerfall seeded.';
END
ELSE PRINT 'Ragnar Hammerfall already exists.';
GO

-- ── 22. Bryndis Gearwright ───────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Bryndis Gearwright')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Bryndis Gearwright', N'bryndis-gearwright', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext,
        SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Bryndis Gearwright', N'bryndis-gearwright', N'Bryndis', N'Gearwright', N'',
        N'human', N'human', N'female', N'she/her',
        27, N'alive',
        N'Scrying installation mechanist; discovered a harmonic resonance that induces shared Sphere 31 visions in bystanders; has tested it on three colleagues without their knowledge.',
        N'Bryndis Gearwright maintains the Scrying installation components for the inner fjord facility — calibration, harmonic tuning, structural integrity of the apparatus. She is precise and technically inventive, which her supervisors value. She has also been running an undisclosed experiment alongside her regular maintenance work for eight months. While calibrating, she discovered a harmonic resonance in the apparatus that induces brief shared visual phenomena in anyone within twelve feet — partial glimpses of what the installation is currently observing. She tested it deliberately on three colleagues who did not know. One reported accurate Sphere 31 imagery in recurring dreams for two weeks. She is building a device to control the frequency. She has told no one.',
        N'The engineer who treats people as instruments in an experiment they never consented to, and what she does with what she finds.',
        N'No POV.',
        N'House Draught; Scrying installation maintenance, inner fjord',
        168, 63, N'quick, precise, tool-marked hands',
        N'dark', N'short crop', N'very short',
        N'dark brown', N'medium dark', N'clear',
        N'none',
        N'fast and efficient; thinks in technical sequences; pauses mid-sentence when an engineering problem surfaces',
        N'maintenance coverall over underlayer; always has at least one tool in a pocket',
        N'none',
        N'Installation maintenance, calibration, component testing. She has been running an additional calibration protocol that is not in the official documentation. She conducts it at low-traffic hours.',
        N'While calibrating the inner fjord installation components, she discovered that a specific harmonic resonance in the apparatus induces brief shared visual phenomena in anyone within twelve feet — partial glimpses of what the installation is actively observing. She tested it deliberately on three colleagues who did not know they were test subjects. One reported accurate and specific Sphere 31 imagery in recurring dreams for two weeks afterward. She is building a device to control the frequency with precision. She has told no one. She has decided that what she finds will justify how she found it.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Scrying installation maintenance areas, inner fjord facility',
        N'0', N'0',
        N'young Norse woman, dark short hair, dark eyes, installation maintenance access, coverall, dark fantasy portrait, focused and absorbed expression',
        N'A young Norse woman in a maintenance coverall with dark short hair and dark brown eyes inside a Scrying installation access passage, expression focused and privately absorbed, dark fantasy portrait',
        0, 0
    );
    PRINT 'Bryndis Gearwright seeded.';
END
ELSE PRINT 'Bryndis Gearwright already exists.';
GO

-- ── 23. Master Ulf Stonethrow ────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ulf Stonethrow')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ulf Stonethrow', N'ulf-stonethrow', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext,
        SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Ulf Stonethrow', N'ulf-stonethrow', N'Ulf', N'Stonethrow', N'Master',
        N'human', N'human', N'male', N'he/him',
        44, N'alive',
        N'Senior garrison engineer; has been building a concealed room into the new eastern wing for two years; does not know who it is for.',
        N'Ulf Stonethrow has been the garrison''s senior engineer for eleven years, responsible for fortress construction, modification, and infrastructure maintenance. He is methodical and trusted and has excellent operational judgment. He is also, and has been for two years, building something he has not disclosed: a concealed room in the garrison''s new eastern wing, behind a water cistern, sized for one person, ventilated through normal ducting, provisioned three weeks ahead at any given time. He restocks it every few months. He does not know who the room is for. He tells himself it is practical contingency planning. He has been building in secret for two years for a reason he cannot name, and has decided that this is acceptable because he is not wrong.',
        N'The person who is building something necessary without knowing why, and the moment the reason arrives.',
        N'No POV.',
        N'House Draught; fortress engineering division, garrison headquarters',
        185, 98, N'heavy, stone-worker''s frame, built for load-bearing',
        N'steel grey', N'very short, almost shaved', N'very short',
        N'pale blue', N'weathered', N'heavily lined, work-worn',
        N'none',
        N'steady, deliberate, unhurried; speaks only when something needs to be said; very good at being in a space without announcing himself',
        N'work coat and underlayer; tool belt; always dust-marked somewhere',
        N'none',
        N'Construction oversight, structural planning, maintenance scheduling. He checks the concealed room''s provisions once a month, alone, after the late shift. He has been doing this for two years.',
        N'He has been building a concealed room into the garrison''s new eastern wing for two years — accessible through a removable panel behind a water cistern, sized for one person, ventilated through the normal ducting, provisioned three weeks at a time. He restocks it periodically, alone. He does not know who the room is for. He has tried to think clearly about this and has not succeeded. He tells himself it is practical contingency planning. He has been building in secret for a reason he cannot name and has decided this is acceptable because whatever the reason turns out to be, it will not have been wrong.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Fortress engineering division, garrison construction zones, new eastern wing',
        N'0', N'0',
        N'heavy older Norse man, steel grey shaved head, pale blue eyes, garrison construction site, work coat, dark fantasy portrait, steady and unexpressive expression',
        N'A heavy older Norse man with a steel grey nearly-shaved head and pale blue eyes at a garrison construction site in a work coat, expression steady and unexpressive, dark fantasy portrait',
        0, 0
    );
    PRINT 'Ulf Stonethrow seeded.';
END
ELSE PRINT 'Ulf Stonethrow already exists.';
GO

-- ── 24. Helga Coldforge ──────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Helga Coldforge')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Helga Coldforge', N'helga-coldforge', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext,
        SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Helga Coldforge', N'helga-coldforge', N'Helga', N'Coldforge', N'',
        N'human', N'human', N'female', N'she/her',
        23, N'alive',
        N'Apprentice smith; has been in correspondence with a House Keldric artisan for seven months; some of what she has shared constitutes military intelligence.',
        N'Helga Coldforge is two years into her smithing apprenticeship, working under Ragnar Hammerfall on Sphere 31 weapon adaptations. She is capable and enthusiastic and has a particular talent for understanding how materials transfer force. She also has a correspondence she has not disclosed: seven months of letters with an artisan from House Keldric, exchanged through a neutral factor they both know from a border market visit. She thinks of it as professional friendship. Some of what she has shared constitutes military-applicable intelligence under House law. She has not looked at the letters from that angle. She is beginning to understand that the other person has looked at them from exactly that angle.',
        N'The point at which professional admiration becomes something the House would call treason, and whether the person sharing knows it yet.',
        N'No POV.',
        N'House Draught; armory and forge, garrison headquarters',
        165, 61, N'compact, strong for her age, tool-confident hands',
        N'bright copper', N'tied back', N'medium',
        N'green', N'freckled fair', N'clear, young',
        N'none',
        N'quick and direct; enthusiastic about technical problems; slightly guarded when talking about anything outside the forge',
        N'apprentice forge apron over underlayer; hair always tied back near heat',
        N'none',
        N'Apprentice forge work, material preparation, Sphere 31 adaptation study. She writes a letter approximately every two weeks through a neutral factor and has been for seven months.',
        N'She has been in correspondence with an artisan from House Keldric for seven months — they met at a border market during a supply evaluation and have been exchanging technical diagrams and design knowledge through a neutral factor. She thinks of it as professional friendship and intellectual exchange. Some of what she has shared — force-transfer patterns for specific blade geometries adapted from Sphere 31 extraction — constitutes military-applicable intelligence under House law. She has not examined the letters from that angle. She is beginning to understand that the person writing back has examined them from exactly that angle, and has been the entire time.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Armory and forge, garrison headquarters; border market contacts',
        N'0', N'0',
        N'young Norse woman, bright copper hair tied back, green eyes, forge apron, garrison armory, dark fantasy portrait, enthusiastic and slightly guarded expression',
        N'A young Norse woman in a forge apron with bright copper hair tied back and green eyes in a garrison armory, expression enthusiastic and slightly guarded, dark fantasy portrait',
        0, 0
    );
    PRINT 'Helga Coldforge seeded.';
END
ELSE PRINT 'Helga Coldforge already exists.';
GO

-- ── 25. Kettlebjörn Saltway ──────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Kettlebjörn Saltway')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Kettlebjörn Saltway', N'kettlebjorn-saltway', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext,
        SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Kettlebjörn Saltway', N'kettlebjorn-saltway', N'Kettlebjörn', N'Saltway', N'',
        N'human', N'human', N'male', N'he/him',
        39, N'alive',
        N'House-patronized trader, border market routes; has been collecting passage tolls from Oathless cells for three years and now knows their structure better than the House does.',
        N'Kettlebjörn Saltway runs the border market routes under House Draught patronage — a quiet, unglamorous role requiring good memory, reliable relationships, and the ability to hold information without showing it. He has developed all three skills past what the House requires. He has also been collecting passage tolls from Oathless cells using his routes for three years — they pay to move undetected, and he accepts. The money matters less than what it bought him: he now knows the structure, safe houses, and key personnel of three Oathless networks the House has been hunting for years. He is waiting for the moment when this knowledge is most valuable, and has begun to suspect that moment will not come.',
        N'Information held as power that has no clean moment to spend itself, and what the holder becomes while waiting.',
        N'No POV.',
        N'House Draught; border market routes, northern coastline',
        176, 90, N'stout, deliberate, radiates mercantile solidity',
        N'sandy grey', N'unkempt, wind-tousled', N'short',
        N'pale hazel', N'weathered, sun-darkened', N'lined, road-worn',
        N'none',
        N'slow and deliberate; gives the impression of being entirely unhurried; misses very little',
        N'merchant''s travel coat over practical underlayer; well-worn boots; belt purse always present',
        N'none',
        N'Border route trading, market negotiation, House supply logistics. Three times a year he collects an additional payment that does not appear in the accounts.',
        N'For three years he has been collecting passage tolls from Oathless cells using his border routes — they pay to move undetected and he accepts. The money matters less than what it purchased: he now knows the structure, safe houses, and key personnel of three Oathless networks that the House has been actively hunting for years. He has not decided what to do with this. He is waiting for the moment when the knowledge is most valuable. He is beginning to suspect that moment will not arrive, which would mean he has been accumulating leverage for its own sake — a thing he would prefer not to believe about himself.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Border market routes, northern coastline; neutral trading posts',
        N'0', N'0',
        N'stout middle-aged Norse trader, sandy grey unkempt hair, hazel eyes, border market, travel coat, dark fantasy portrait, unhurried watchful expression',
        N'A stout middle-aged Norse trader with sandy grey hair and hazel eyes at a border market in a travel coat, expression unhurried and watching everything, dark fantasy portrait',
        0, 0
    );
    PRINT N'Kettlebjörn Saltway seeded.';
END
ELSE PRINT N'Kettlebjörn Saltway already exists.';
GO

-- ── 26. Vigdis Ironwife ──────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Vigdis Ironwife')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Vigdis Ironwife', N'vigdis-ironwife', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext,
        SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Vigdis Ironwife', N'vigdis-ironwife', N'Vigdis', N'Ironwife', N'',
        N'human', N'human', N'female', N'she/her',
        32, N'alive',
        N'House trade factor; maintains two complete identities — one for Draught, one neutral — and has a child under each; the neutral account manager just died.',
        N'Vigdis Ironwife manages House Draught''s trade relationships in the neutral western ports — negotiations, contracts, long-term arrangements with suppliers the House prefers not to acknowledge publicly. She is composed and very good at this. She has also been maintaining a second, entirely separate professional identity as a neutral independent factor who trades openly with Draught''s enemies, and has a son under the first identity and a daughter under the second. The children have never met. The account manager who maintained her neutral network died three weeks ago. She needs a replacement without either identity discovering the other. She is performing steadiness. She is excellent at it.',
        N'A compartmentalized life at the moment a single pin comes loose, and what performance costs the person doing it.',
        N'No POV.',
        N'House Draught; neutral port trading network, western coastline',
        172, 67, N'composed, well-kept, professionally presented at all times',
        N'dark auburn', N'styled', N'long',
        N'dark blue', N'fair', N'clear, maintained',
        N'none',
        N'composed and deliberate; nothing unplanned in her bearing; reads rooms very quickly',
        N'quality merchant''s coat appropriate to whichever identity she is operating under; always appropriate, never memorable',
        N'none',
        N'Factor negotiations, contract management, port relationship maintenance. She is writing two sets of business correspondence every week from two different desks in two different cities, and has been for six years.',
        N'She maintains two complete professional identities — one as a House Draught-affiliated factor and one as a neutral independent merchant who trades openly with Draught''s enemies. She has a son under the first identity and a daughter under the second. The children have never met. She has held this compartmentalization for six years without a structural failure. Three weeks ago the account manager maintaining her neutral network died suddenly. She must find a replacement without either identity discovering the other exists. She is performing steadiness. She is very good at it. She has not been able to eat with any real appetite for three weeks.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Neutral western ports, House Draught trade offices, western coastline',
        N'0', N'0',
        N'Norse woman, dark auburn styled hair, dark blue eyes, merchant factor office, quality coat, dark fantasy portrait, composed and calculating expression',
        N'A Norse woman with dark auburn styled hair and dark blue eyes in a merchant factor office in a quality coat, expression composed and privately calculating, dark fantasy portrait',
        0, 0
    );
    PRINT 'Vigdis Ironwife seeded.';
END
ELSE PRINT 'Vigdis Ironwife already exists.';
GO

-- ── 27. Lady Sigrun Valdisdóttir ─────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Sigrun Valdisdóttir')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Sigrun Valdisdóttir', N'sigrun-valdisdottir', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext,
        SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Sigrun Valdisdóttir', N'sigrun-valdisdottir', N'Sigrun', N'Valdisdóttir', N'Lady',
        N'human', N'human', N'female', N'she/her',
        22, N'alive',
        N'Minor noble; administrative posting; has filed nine rejected transfer requests for Scrying; a Liturgy contact has offered her a position outside House authority and she has not declined.',
        N'Sigrun Valdisdóttir is a second-rank family member with a minor administrative posting she is almost completely unsuited for and performs adequately. She has also filed nine separate requests to transfer to the Scrying operations division, each rejected with the word "premature" by the same officer — her father — who has not explained further. Last month a Liturgy field contact offered her a junior observer position outside House authority. She said she would consider it. She has been considering it for four weeks and has not declined. She does not know if she is thinking or waiting for permission to stop waiting for her father to say yes.',
        N'The minor noble who is offered a door out from the wrong direction, and what they do when they realize the offer has a price.',
        N'No POV.',
        N'House Draught; administrative division, garrison headquarters',
        166, 60, N'tall for the family line, self-conscious about it',
        N'light brown', N'pinned', N'long',
        N'grey-blue', N'fair', N'clear, slightly flushed when frustrated',
        N'none',
        N'correct noble posture, held with effort; tends to stand very still when being spoken at by her father',
        N'minor noble''s administrative dress, appropriate and unmemorable',
        N'none',
        N'Administrative filing, officer scheduling support, garrison correspondence. She has nine rejected transfer requests filed in date order. She reviews the Liturgy contact''s offer in her mind approximately once an hour.',
        N'She has filed nine administrative requests to transfer to the Scrying operations division. Each rejection bears her father''s signature and the word "premature." Last month a Liturgy field contact offered her a junior observer position — outside House authority, inside the Liturgy''s structure. She said she would consider it. She has been considering it for four weeks. She has not declined. She does not know if she is genuinely thinking the offer through or simply waiting for permission from her father that she has already decided not to wait for.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Administrative division, garrison headquarters',
        N'0', N'0',
        N'young Norse noblewoman, light brown pinned hair, grey-blue eyes, garrison administrative office, minor noble dress, dark fantasy portrait, controlled frustrated expression',
        N'A young Norse noblewoman with light brown pinned hair and grey-blue eyes in a garrison administrative office in minor noble dress, expression controlled with a private frustration underneath, dark fantasy portrait',
        0, 0
    );
    PRINT N'Sigrun Valdisdóttir seeded.';
END
ELSE PRINT N'Sigrun Valdisdóttir already exists.';
GO

-- ── 28. Lord Vilhjalm Geirhardsson ───────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Vilhjalm Geirhardsson')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Vilhjalm Geirhardsson', N'vilhjalm-geirhardsson', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext,
        SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Vilhjalm Geirhardsson', N'vilhjalm-geirhardsson', N'Vilhjalm', N'Geirhardsson', N'Lord',
        N'human', N'human', N'male', N'he/him',
        20, N'alive',
        N'Minor noble in garrison logistics; stole a Liturgy transit manifest a month ago; three of the thirty-one scheduled names are garrison workers he knows.',
        N'Vilhjalm Geirhardsson is in a garrison logistics role he is managing acceptably, by which he means he completes the forms without major errors and has not lost anything irreplaceable. He has been managing something else for four weeks, less acceptably: a Liturgy transit manifest he stole from a diplomatic pouch while serving as courier escort, because the seal was already broken and he was curious. The manifest schedules thirty-one persons from Sphere 31 for transit over the next six months. Three of the names are garrison workers he knows — a cook, a farrier, a laundress. He cannot put the manifest back. He cannot put it down.',
        N'The impulsive act that converts a bystander into a witness, and what a twenty-year-old does with knowledge he was never meant to have.',
        N'No POV.',
        N'House Draught; logistics division, garrison headquarters',
        180, 75, N'rangy, not yet filled out, still a young man''s build',
        N'dark blond', N'disheveled', N'medium',
        N'grey', N'pale indoor', N'clear, young',
        N'none',
        N'slightly loose posture that tightens when he''s thinking about the manifest; tends to reach for his coat pocket',
        N'minor noble''s logistics dress; carries a coat he wears regardless of temperature',
        N'none',
        N'Logistics paperwork, supply scheduling, courier escort duties. He carries the manifest in his coat every day. He has been doing this for four weeks.',
        N'He stole a sealed Liturgy transit manifest from a diplomatic pouch a month ago — impulsively, because the seal was already broken and he was curious. The manifest schedules thirty-one persons from Sphere 31 for membrane transit over the next six months. Three of the named persons are garrison workers he knows by sight and name: a cook, a farrier, a laundress. He does not know if they know their names are on the list. He cannot report it without confessing the theft. He cannot put the manifest back. He cannot put it down either. He has been carrying it in his coat for four weeks.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Logistics division, garrison headquarters; courier routes',
        N'0', N'0',
        N'young Norse nobleman, dark blond disheveled hair, grey eyes, garrison logistics office, coat, dark fantasy portrait, anxious and thinking expression',
        N'A young Norse nobleman with dark blond disheveled hair and grey eyes in a garrison logistics office, hand near his coat pocket, expression anxious and thinking, dark fantasy portrait',
        0, 0
    );
    PRINT 'Vilhjalm Geirhardsson seeded.';
END
ELSE PRINT 'Vilhjalm Geirhardsson already exists.';
GO

-- ── 29. Lady Dagny Frostmantle ───────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Dagny Frostmantle')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Dagny Frostmantle', N'dagny-frostmantle', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext,
        SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Dagny Frostmantle', N'dagny-frostmantle', N'Dagny', N'Frostmantle', N'Lady',
        N'human', N'human', N'female', N'she/her',
        24, N'alive',
        N'Minor noble; courier and diplomatic support; in coded correspondence with a Sphere 31 academic; both of them are being directed by a third party neither has identified.',
        N'Dagny Frostmantle handles courier and diplomatic support — message delivery, document escort, the administrative logistics of information moving between the garrison and outside parties. She is good at being invisible in rooms, which is professionally useful. She has also been in coded correspondence for eight months with a Sphere 31 academic who was membrane-transited and officially returned three years ago. The academic believes they are writing to a Liturgy research official. Dagny believes she is receiving suppressed intelligence. Both are being directed by a third party neither of them has identified. The letters have been arriving regularly. The information in them is very specific. Dagny has learned that specific information is sometimes a thing someone wants you to have.',
        N'Two people being used against each other''s interests by a party that has never introduced itself.',
        N'No POV.',
        N'House Draught; diplomatic courier service, garrison headquarters',
        169, 63, N'contained, capable, easy to not notice',
        N'dark brown', N'loose, practical', N'medium',
        N'warm hazel', N'olive-tan', N'clear',
        N'none',
        N'minimal, self-contained; occupies exactly the space she needs; reads the room on entry and adjusts immediately',
        N'courier''s practical dress; comfortable footwear; no ornamentation',
        N'none',
        N'Courier assignments, diplomatic document escort, message relay. She opens the letters from Sphere 31 at her desk, alone, in the evening.',
        N'For eight months she has been in coded correspondence with a Sphere 31 academic who was membrane-transited and officially returned three years ago. The academic believes they are writing to a Liturgy research official. Dagny believes she is receiving intelligence the Liturgy suppresses. Both are being directed by a third party neither of them has identified. The letters have been arriving regularly. The information in them is very specific and very useful. Dagny has learned that specific information arriving uselessly is not a problem — but specific information arriving usefully, reliably, from a direction she did not ask it from, is something else entirely.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Garrison headquarters, diplomatic courier routes, garrison administrative offices',
        N'0', N'0',
        N'young Norse noblewoman, dark brown loose hair, hazel eyes, garrison courier office, practical dress, dark fantasy portrait, attentive and contained expression',
        N'A young Norse noblewoman with dark brown loose hair and hazel eyes in a garrison courier office, expression attentive and contained, dark fantasy portrait',
        0, 0
    );
    PRINT 'Dagny Frostmantle seeded.';
END
ELSE PRINT 'Dagny Frostmantle already exists.';
GO

-- ── 30. Mátyás Selby ─────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Mátyás Selby')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Mátyás Selby', N'matyas-selby', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext,
        SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Mátyás Selby', N'matyas-selby', N'Mátyás', N'Selby', N'',
        N'human', N'human', N'male', N'he/him',
        38, N'alive',
        N'Liturgy field agent; membrane access point; allows unauthorized transit in exchange for intelligence he routes through the membrane to a recipient he cannot name or locate.',
        N'Mátyás Selby was assigned to House Draught''s membrane access point seven years ago as a Liturgy field agent, nominally overseeing transit logistics and compliance. He is known to the House as cooperative and thorough. He has been allowing unauthorized membrane transit for three years — not for money, but in exchange for intelligence from Sphere 31 individuals who pass through. He routes the collected intelligence back through the membrane using a technique one of the unauthorized travelers taught him. He does not know who receives it on the other side. He has begun to believe the recipient is not in any location he has a name for. He tells himself he is running an intelligence operation. He cannot characterize its objective.',
        N'The intelligence officer who has lost track of what he is actually serving, and whether that matters if the work is real.',
        N'No POV.',
        N'House Draught; membrane access point, northern headland; Liturgy field assignment',
        175, 80, N'compact, watchful, radiates professional neutrality',
        N'dark brown', N'practical cut', N'short',
        N'dark brown', N'medium brown', N'clear, attentive',
        N'none',
        N'still and contained; catalogues a room''s occupants without appearing to; speaks in a register that communicates reliable harmlessness',
        N'Liturgy field officer''s coat over plain underlayer; nothing that draws attention',
        N'none',
        N'Transit oversight, compliance verification, access point management. He conducts unauthorized transit at irregular hours when the watch rotation allows a window. He has been doing this for three years.',
        N'He has been allowing unauthorized membrane transit for three years, not for payment but in exchange for intelligence from Sphere 31 individuals who pass through. He routes the collected intelligence through the membrane using a technique one of the unauthorized travelers showed him. He does not know who receives it. He has begun to think the recipient may not be in any location the Liturgy has mapped — or any location he has a name for. He tells himself he is running an intelligence operation. He cannot name its beneficiary or its objective. He continues.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Membrane access point, northern headland; Liturgy transit facilities',
        N'0', N'0',
        N'compact middle-aged man, dark hair, dark eyes, Liturgy transit facility, field officer coat, dark fantasy portrait, professionally neutral expression',
        N'A compact middle-aged man with dark hair and dark eyes in a Liturgy transit facility in a field officer coat, expression professionally neutral with watchfulness underneath, dark fantasy portrait',
        0, 0
    );
    PRINT N'Mátyás Selby seeded.';
END
ELSE PRINT N'Mátyás Selby already exists.';
GO

-- ── 31. Thorunn Gatewarden ───────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Thorunn Gatewarden')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Thorunn Gatewarden', N'thorunn-gatewarden', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext,
        SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Thorunn Gatewarden', N'thorunn-gatewarden', N'Thorunn', N'Gatewarden', N'',
        N'human', N'human', N'female', N'she/her',
        45, N'alive',
        N'Long-service Liturgy membrane access officer; has kept a private record of every person taken from Sphere 31 on her watch for twenty-two years; intends to send it back through before she dies.',
        N'Thorunn Gatewarden has been the membrane access officer at the northern headland post for twenty-two years. She is the person who processes the transit orders and watches people go through, and she has been doing it long enough to have stopped reacting to it in any visible way. What she has been doing in parallel for twenty-two years: recording every name. Every person taken from Sphere 31. Every transit order. Who signed it. Who received the person on the Cauld side. The record fills four Sphere 31 notebooks a transit subject gave her years ago. It names over four hundred individuals. Before she dies, she intends to send it back through the membrane, addressed to no one, on the assumption that someone on the other side will understand.',
        N'The witness who has been recording what the institution does not want recorded, and the question of whether the record will reach anyone who can use it.',
        N'No POV.',
        N'House Draught; membrane access point, northern headland; Liturgy long-service post',
        162, 68, N'heavy-boned, immovable posture, built for endurance',
        N'white-grey', N'braided', N'very long',
        N'dark grey', N'deeply weathered fair', N'heavily lined, experienced',
        N'none',
        N'immovable, unhurried; processes transit without affect; people find her reassuring and do not know why',
        N'Liturgy access officer''s uniform, worn correctly and without variation for twenty years',
        N'none',
        N'Transit processing, access point management, compliance record maintenance. She writes in the Sphere 31 notebooks at the end of every watch. She has been doing this for twenty-two years.',
        N'For twenty-two years she has been recording every person taken from Sphere 31 through her access point — names, physical descriptions, the transit orders, who signed them, who received the individual on the Cauld side. The record is written in four Sphere 31 notebooks a transit subject gave her years ago in exchange for a kind word and a meal. It names over four hundred individuals. Before she dies, she intends to send it back through the membrane addressed to no one, on the assumption that someone there will understand what four hundred names and the orders that moved them means.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Membrane access point, northern headland; Liturgy transit post',
        N'0', N'0',
        N'older Norse woman, white-grey braided hair, dark grey eyes, membrane transit access point, Liturgy uniform, dark fantasy portrait, weathered and immovable expression',
        N'An older Norse woman with white-grey braided hair and dark grey eyes at a membrane transit access point in a Liturgy uniform, expression weathered and immovable, dark fantasy portrait',
        0, 0
    );
    PRINT 'Thorunn Gatewarden seeded.';
END
ELSE PRINT 'Thorunn Gatewarden already exists.';
GO

-- ── 32. Asta Keldesdóttir ────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Asta Keldesdóttir')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Asta Keldesdóttir', N'asta-keldesdottir', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext,
        SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Asta Keldesdóttir', N'asta-keldesdottir', N'Asta', N'Keldesdóttir', N'',
        N'human', N'human', N'female', N'she/her',
        26, N'alive',
        N'Junior Liturgy administrative official, House liaison; processed a transit manifest three months ago that included a childhood friend; checks the intake log for the name every morning.',
        N'Asta Keldesdóttir has been assigned to the House Draught garrison as a junior Liturgy administrative official for two years, processing transit paperwork and managing liaison correspondence. Three months ago she processed and approved a transit manifest that included a woman she grew up with — a childhood friend who had married a garrison laborer and was unaware she was listed. Asta signed the document and went home and made dinner. She checks the intake logs for her friend''s name every morning before anything else. The name has not appeared. She does not know what this means. She has begun entering false names into verification logs to test whether anyone monitors the gaps.',
        N'The official who did the institutional thing and has been trying to find out what happened ever since without asking directly.',
        N'No POV.',
        N'House Draught; Liturgy liaison office, garrison headquarters',
        167, 61, N'neatly presented, slightly rigid posture',
        N'pale blonde', N'strict bun', N'long',
        N'light blue', N'fair', N'clear, maintained',
        N'none',
        N'correct and contained; slightly over-precise in movement; tends to go very still when she is afraid',
        N'Liturgy administrative uniform, impeccably kept',
        N'none',
        N'Liaison paperwork, transit documentation, garrison correspondence. She checks the intake log first thing every morning. She has been doing this for three months.',
        N'Three months ago she processed and approved a transit manifest that included a woman she grew up with — a childhood friend, married to a garrison laborer, unaware she was listed. Asta signed the document. She went home and made dinner. She checks the intake log for her friend''s name every morning. The name has not appeared. She does not know whether this means her friend was not taken, escaped the transit, or was entered under a different name. Three weeks ago she began adding false names to the verification log to see if anyone monitors for anomalies. No one has responded. She does not know if this is reassuring.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Liturgy liaison office, garrison headquarters; transit documentation office',
        N'0', N'0',
        N'young Norse woman, pale blonde hair in strict bun, light blue eyes, Liturgy administrative office, uniform, dark fantasy portrait, contained and quietly frightened expression',
        N'A young Norse woman with pale blonde hair in a strict bun and light blue eyes in a Liturgy administrative office, expression contained with a quiet fear underneath, dark fantasy portrait',
        0, 0
    );
    PRINT N'Asta Keldesdóttir seeded.';
END
ELSE PRINT N'Asta Keldesdóttir already exists.';
GO

-- ── 33. Nils Darkwater ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Nils Darkwater')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Nils Darkwater', N'nils-darkwater', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext,
        SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Nils Darkwater', N'nils-darkwater', N'Nils', N'Darkwater', N'',
        N'human', N'human', N'male', N'he/him',
        34, N'alive',
        N'House Draught intelligence officer; runs networks for two Houses simultaneously; was born in House Vael; can no longer reliably recall his birth name.',
        N'Nils Darkwater runs House Draught''s western intelligence network. He is thorough, trusted, and produces reliable results, which is precisely how he has remained trusted — the intelligence is genuine because genuine intelligence is how he stays in place. What is not genuine: he was born in House Vael under a name he placed in an archive eleven years ago along with the rest of his previous life. He runs intelligence for both Houses simultaneously. The Vael network receives the smaller share of what he knows; the Draught network gets the larger because the larger is how he survives. He has been trying to remember his birth name every night for three months. It surfaces sometimes as a sound. Not as a word.',
        N'The person who inhabited an identity long enough that the original has become the fiction, and what is left when neither version is complete.',
        N'No POV.',
        N'House Draught; intelligence division, garrison headquarters; western networks',
        181, 84, N'unremarkably average; this is deliberate',
        N'mousy brown', N'forgettable cut', N'short',
        N'grey-brown', N'medium pale', N'clear, professionally forgettable',
        N'none',
        N'unremarkable in all ways; moves through rooms without attracting attention; has practiced this for eleven years',
        N'intelligence officer''s working dress; nothing memorable, nothing absent',
        N'none',
        N'Intelligence network management, officer briefings, field asset coordination. He attempts to recall his birth name each night before sleep. He allows approximately ten minutes for this before stopping.',
        N'He was born in House Vael under a name he no longer reliably recalls and was placed in House Draught eleven years ago as a long-term asset. He runs intelligence networks for both Houses simultaneously; the Draught network produces genuine intelligence because producing genuine intelligence is the only way to remain trusted. He has been trying to remember his birth name every night for three months. It surfaces sometimes as a shape in his mouth, a partial sound. Not as a word. He is beginning to treat this as data about what eleven years does to a person, which is itself a thing the person he was eleven years ago would never have said.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Intelligence division, garrison headquarters; western intelligence network',
        N'0', N'0',
        N'deliberately average Norse man, mousy brown hair, grey-brown eyes, intelligence office, working dress, dark fantasy portrait, professionally unremarkable expression',
        N'A deliberately average Norse man with mousy brown hair and grey-brown eyes in an intelligence office, expression professionally unremarkable — the expression of someone who has practiced being forgotten, dark fantasy portrait',
        0, 0
    );
    PRINT 'Nils Darkwater seeded.';
END
ELSE PRINT 'Nils Darkwater already exists.';
GO

-- ── 34. Runa Ashenmark ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Runa Ashenmark')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Runa Ashenmark', N'runa-ashenmark', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext,
        SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Runa Ashenmark', N'runa-ashenmark', N'Runa', N'Ashenmark', N'',
        N'human', N'human', N'female', N'she/her',
        29, N'alive',
        N'Intelligence asset under trader cover; has known for six months that her handler is feeding her false intelligence; cannot decide whether reporting it is a test or an exposure.',
        N'Runa Ashenmark works as an intelligence asset under cover as a trader''s assistant — a role she has held for four years reliably and without incident. What has been true for six months: she knows her handler is feeding her false intelligence. She detected it through access to two separate information streams her handler doesn''t know she has. She has continued operating normally while collecting evidence. She cannot decide what to do: if the House is running the handler as a test, then reporting is a test she will be watched for failing; if the handler is independent, then reporting exposes her access. She has been making no decision for six months. She is aware that no decision is itself a decision.',
        N'The moment a competent person realizes the competent thing to do cannot be identified, and what they do instead.',
        N'No POV.',
        N'House Draught; intelligence network, border markets; trader cover',
        166, 62, N'unremarkable, blends easily; this is professional',
        N'warm brown', N'loose practical plait', N'medium',
        N'light brown', N'medium tan', N'clear',
        N'none',
        N'neutral and unremarkable; moves without drawing attention; very good at appearing to have no internal weather',
        N'trader''s assistant dress; practical, forgettable, appropriate to context',
        N'none',
        N'Field asset work, courier cover, border market presence. She has been maintaining a separate log of her handler''s intelligence inconsistencies for six months. She keeps it in a format that looks like a personal accounts ledger.',
        N'She has known for six months that her handler is feeding her false intelligence — detectable only because she has access to two separate information streams her handler doesn''t know she has. She has continued operating normally while quietly collecting evidence of the inconsistencies. She cannot report it: if the House is running the handler as a test of her, then reporting is a test she will be watched for failing; if the handler is independent, then reporting exposes her access. She has been making no decision for six months. She is aware that no decision is also a decision, and she is aware that she is aware of this, and she still cannot move.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Border markets, trader network routes, intelligence field network',
        N'0', N'0',
        N'unremarkable Norse woman, warm brown plait, light brown eyes, border market, trader dress, dark fantasy portrait, professionally neutral expression with something held underneath',
        N'An unremarkable Norse woman with a warm brown plait and light brown eyes at a border market in trader''s dress, expression professionally neutral with something held carefully underneath, dark fantasy portrait',
        0, 0
    );
    PRINT 'Runa Ashenmark seeded.';
END
ELSE PRINT 'Runa Ashenmark already exists.';
GO

-- ── 35. Brekka Tidewatch ─────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Brekka Tidewatch')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Brekka Tidewatch', N'brekka-tidewatch', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext,
        SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Brekka Tidewatch', N'brekka-tidewatch', N'Brekka', N'Tidewatch', N'',
        N'human', N'human', N'male', N'he/him',
        31, N'alive',
        N'Oathless spy embedded at the garrison docks for fourteen months; has suppressed actionable intelligence twice to protect specific people; his cell is two weeks from sending someone to check on him.',
        N'Brekka Tidewatch came in as a two-month insertion — dock laborer, intelligence on supply movements, out before anyone looked twice. He is still here, fourteen months in. His Oathless cell has noticed the intelligence has become sparse and delayed. The truth: he has suppressed two pieces of actionable intelligence because acting on them would have killed specific people — a dock cook who gave him food during a bad week, and a young Myrmidon whose name he learned by accident and has not been able to put in a report. His cell is approximately two weeks from sending someone to check on him. He has not decided what he will do when they arrive. He tells himself this is because he has not had time to think. He knows this is not the reason.',
        N'The spy who went so deep he found something worth protecting, and the moment the cell arrives to find out why he stopped sending intelligence.',
        N'No POV.',
        N'Oathless; garrison docks, harbor district (embedded)',
        177, 83, N'dock-worker''s frame, callused hands, built for cargo work',
        N'dark brown', N'practical short', N'short',
        N'grey-blue', N'weathered medium fair', N'weathered, harbor-marked',
        N'none',
        N'dock-worker''s ease with physical labor; conserves himself; very good at being invisible in work',
        N'dock laborer''s work clothes; functional, worn, appropriate to the role',
        N'none',
        N'Dock labor — cargo handling, manifest checking, equipment maintenance. He works hard enough to be invisible. He does not make friends. He has made two.',
        N'He came in as a two-month Oathless insertion and is fourteen months in. His cell has noticed the intelligence has become sparse. The truth: he has suppressed two pieces of actionable intelligence — a dock cook who gave him food during a bad week, and a young Myrmidon whose name he learned by accident and has not been able to enter in a report. His Oathless cell is approximately two weeks from sending someone to verify his status. He has not decided what he will do when that person arrives. He tells himself he has not had time to think about it clearly. He knows this is not why.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Garrison docks and harbor district (embedded); Oathless network (external)',
        N'0', N'0',
        N'weathered Norse dock laborer, dark brown short hair, grey-blue eyes, harbor dock, work clothes, dark fantasy portrait, working man''s ease with private tension underneath',
        N'A weathered Norse man in dock laborer''s work clothes with dark brown short hair and grey-blue eyes on a harbor dock, expression a working man''s ease with private tension underneath, dark fantasy portrait',
        0, 0
    );
    PRINT 'Brekka Tidewatch seeded.';
END
ELSE PRINT 'Brekka Tidewatch already exists.';
GO
