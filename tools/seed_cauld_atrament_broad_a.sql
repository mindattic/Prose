SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- HOUSE ATRAMENT — ACTIVE POPULATION BATCH A  (15 characters)
-- Universe: Cauld  (ID 0197E9C9-0002-7000-8000-000000000002)
-- 2026-07-05 · Idempotent (IF NOT EXISTS guards on all inserts)
-- Categories: Intelligence officers (3) · Soldiers (2) · Scrying operators (2)
--   Diplomats (2) · Merchant/front (1) · Spy/multi-House (2)
--   Liturgy contact (1) · Interrogation specialist (1) · Scholar (1)
-- ═══════════════════════════════════════════════════════════════════════════════

-- ── 1. Adelais Farenc ──────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Adelais Farenc')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Adelais Farenc', N'adelais-farenc', N'canon', 1,
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
        @id, N'Adelais Farenc', N'adelais-farenc', N'Adelais', N'Farenc', N'',
        N'human', N'human', N'female', N'she/her',
        34, N'alive',
        N'Intelligence officer; manages Atrament''s mid-tier informant network across three rival Houses.',
        N'Runs Atrament''s mid-tier informant network across three Houses. Precise, unhurried, reads a room by what it doesn''t say. Appears to be a household administrator. Never writes down what she needs to remember.',
        N'The intelligence structure''s quiet human center; demonstrates the cost of divided loyalty made habitual.',
        N'No POV.',
        N'House Atrament; central vine country estate',
        162, 57, N'slender, understated',
        N'dark brown', N'pinned coil', N'medium',
        N'brown', N'olive-fair', N'clear, fine-featured',
        N'none',
        N'Upright and unhurried; economical stillness between deliberate movements.',
        N'Estate administrator dress; muted colours; nothing that invites a second look.',
        N'none',
        N'Morning review of informant reports. Afternoon network management — coded correspondence, staged accidents of timing.',
        N'She has fed House Lacerta a curated subset of Atrament''s courier intercepts for three years to shield a sister held hostage by marriage to a Lacerta minor lord.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Vine country estates and border crossings; three-House operational zone',
        N'0', N'0',
        N'French medieval woman intelligence officer, dark pinned hair, olive skin, estate courtyard, surveillance, dark fantasy Buehlman portrait',
        N'French medieval woman, dark hair, olive skin, estate courtyard, dark fantasy',
        0, 0
    );
    PRINT N'Adelais Farenc seeded.';
END
ELSE PRINT N'Adelais Farenc already exists.';
GO

-- ── 2. Bertrand Noel ──────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Bertrand Noel')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Bertrand Noel', N'bertrand-noel', N'canon', 1,
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
        @id, N'Bertrand Noel', N'bertrand-noel', N'Bertrand', N'Noel', N'',
        N'human', N'human', N'male', N'he/him',
        43, N'alive',
        N'Knight; Scrying installation guard commander; handles what the diplomatic apparatus prefers not to.',
        N'Knight, Scrying installation commander. Survived his infusion under circumstances no record accurately reflects. Commands authority through stillness. Capable of exceptional violence when the installation requires it, which it has.',
        N'The violence Atrament keeps invisible — what happens when diplomacy cannot or will not start.',
        N'No POV.',
        N'House Atrament; garrison post, Scrying installation, vine country',
        193, 97, N'powerful, solid; the Knight''s frame worn as though it always fit',
        N'grey-streaked black', N'close-cropped', N'short',
        N'grey', N'tanned', N'weathered, with a jaw that has taken impacts and not complained',
        N'Subtle height gain (Knight)',
        N'Military upright; moves as though space arranges itself around him; rarely hurries.',
        N'Heavy garrison kit; rank insignia; no personal ornament.',
        N'Knight-grade Transmutation; bone density increase; cellular repair acceleration; old wounds that left no scars.',
        N'Patrols the Scrying installation at dawn and dusk. Drills his guard to a standard no one questions.',
        N'His infusion records were altered by a physician who declared him dead, then filed his survival as a second application. Bertrand owns that physician completely. The physician shows up for every shift.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Scrying installation and perimeter; vine country, central estate',
        N'0', N'0',
        N'Tall French medieval knight, grey-streaked black hair, grey eyes, tanned, heavy garrison kit, stone fortress, dark fantasy Buehlman portrait',
        N'Tall French medieval knight, grey-streaked hair, stone fortress interior, dark fantasy',
        0, 0
    );
    PRINT N'Bertrand Noel seeded.';
END
ELSE PRINT N'Bertrand Noel already exists.';
GO

-- ── 3. Clemence Rigaud ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Clemence Rigaud')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Clemence Rigaud', N'clemence-rigaud', N'canon', 1,
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
        @id, N'Clemence Rigaud', N'clemence-rigaud', N'Clemence', N'Rigaud', N'',
        N'human', N'human', N'female', N'she/her',
        27, N'alive',
        N'Scrying operator; monitors Sphere 31 civilian and population-movement feeds.',
        N'Scrying operator, three years certified. Methodical, quiet, preferred over senior colleagues for night shifts. Has developed strong opinions about specific Sphere 31 families that she keeps entirely to herself.',
        N'Observation as possession — the moral weight the Scrying apparatus creates in operators over time.',
        N'No POV.',
        N'House Atrament; Scrying installation, vine country',
        159, 52, N'slight, contained',
        N'auburn', N'loose braid', N'long',
        N'hazel', N'fair', N'clear; ink-stained at the cuffs',
        N'none',
        N'Slight forward lean; occupies space carefully, as though conserving it.',
        N'Plain operator''s robe; no ornament; always ink-stained.',
        N'none',
        N'Shift rotations at the Scrying chamber, maintenance logs, shift-change briefings. Evenings reviewing unauthorized observation records.',
        N'She has mapped a Sphere 31 family across fourteen observation sessions whose generational resemblance to the Colbert line is exact. She has filed each session as equipment noise.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Scrying chamber and adjacent archive; rarely leaves the installation',
        N'0', N'0',
        N'French medieval woman, auburn hair, hazel eyes, Scrying chamber blue glow, tired intent gaze, dark fantasy Buehlman portrait',
        N'French medieval woman, auburn hair, Scrying chamber glow, intent expression, dark fantasy',
        0, 0
    );
    PRINT N'Clemence Rigaud seeded.';
END
ELSE PRINT N'Clemence Rigaud already exists.';
GO

-- ── 4. Gautier Cros ───────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Gautier Cros')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Gautier Cros', N'gautier-cros', N'canon', 1,
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
        @id, N'Gautier Cros', N'gautier-cros', N'Gautier', N'Cros', N'Master',
        N'human', N'human', N'male', N'he/him',
        55, N'alive',
        N'Senior diplomat; Atrament''s primary voice at inter-House councils and treaty negotiations.',
        N'Senior diplomat, Atrament''s primary voice at inter-House councils for fifteen years. Manages every exchange as if the outcome is already settled. His memory for prior agreements is encyclopedic and entirely tactical.',
        N'The face Atrament shows the world — what composure costs when the foundation beneath it is fabricated.',
        N'No POV.',
        N'House Atrament; central estate; inter-House council circuit, vine country',
        174, 79, N'medium, trim; the build of a man who has never needed to be physically imposing',
        N'silver', N'neat, combed back', N'short',
        N'dark brown', N'olive', N'weathered but well-kept; diplomat''s face',
        N'none',
        N'Deliberate and unhurried; treats all movement as a form of argument.',
        N'Formal diplomatic robes; House colours; no weapon visible.',
        N'none',
        N'Council sessions, private audiences, estate dinners that are never merely social. He schedules every meeting himself.',
        N'He fabricated the atrocity evidence that anchored the Atrament-Fornax peace accord twenty years ago. The forgery is in a sealed vault. He is the last living person who knows the record is false.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Central vine country; inter-House council circuit; formal diplomatic corridors',
        N'0', N'0',
        N'Older French medieval diplomat, silver hair, formal dark robes, council chamber, composed authority, dark fantasy Buehlman portrait',
        N'French medieval diplomat, silver hair, formal dark robes, council chamber, dark fantasy',
        0, 0
    );
    PRINT N'Gautier Cros seeded.';
END
ELSE PRINT N'Gautier Cros already exists.';
GO

-- ── 5. Isabeau Mallet ─────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Isabeau Mallet')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Isabeau Mallet', N'isabeau-mallet', N'canon', 1,
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
        @id, N'Isabeau Mallet', N'isabeau-mallet', N'Isabeau', N'Mallet', N'',
        N'human', N'human', N'female', N'she/her',
        31, N'alive',
        N'Field intelligence agent; operates in House Calyx border territory under diplomatic cover.',
        N'Field agent. Runs intelligence operations in House Calyx border territory. Warm, socially fluent, exceptional at building trust across months. Has not slept well in six months.',
        N'The fraying point of any intelligence network — the agent whose first loyalty was already elsewhere.',
        N'No POV.',
        N'House Atrament; House Calyx border territory, field operational range',
        168, 64, N'athletic, medium; appears effortlessly approachable',
        N'brown', N'loose, practical', N'shoulder-length',
        N'green', N'tan', N'clear; the slight hollowness under the eyes is recent',
        N'none',
        N'Open, inviting posture that costs her nothing and costs others considerably.',
        N'Practical traveller''s dress; tools of social access, not aggression.',
        N'none',
        N'Field operations in Calyx border territory, periodic estate return. Reviews her cover story during sleepless hours.',
        N'She passed Atrament''s Scrying installation grid coordinates to House Calyx six months ago. The raid that followed killed three operators. She convinced herself they would have had time to evacuate.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'House Calyx border; vine country estate; courier routes between',
        N'0', N'0',
        N'French medieval woman, brown hair loose, green eyes, border forest, watchful ease, dark fantasy Buehlman portrait',
        N'French medieval woman, brown hair, green eyes, forest border road, dark fantasy',
        0, 0
    );
    PRINT N'Isabeau Mallet seeded.';
END
ELSE PRINT N'Isabeau Mallet already exists.';
GO

-- ── 6. Luc Bessiere ───────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Luc Bessiere')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Luc Bessiere', N'luc-bessiere', N'canon', 1,
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
        @id, N'Luc Bessiere', N'luc-bessiere', N'Luc', N'Bessiere', N'',
        N'human', N'human', N'male', N'he/him',
        38, N'alive',
        N'Wine merchant and intelligence logistics coordinator; manages drop points across the vine country trade circuit.',
        N'Operates as a wine merchant across central vine country. Real business, real routes, real accounts. The intelligence drops are secondary now, or he tells himself this. The savings account is primary.',
        N'What a network looks like from the logistics level — and what leaving one actually requires.',
        N'No POV.',
        N'House Atrament; vine country trade circuit; mobile',
        181, 88, N'broad-shouldered, solid; built for moving heavy things without complaint',
        N'dark brown', N'close-cropped', N'short',
        N'brown', N'tanned', N'road-weathered; the permanent squint of someone who judges distances',
        N'none',
        N'Broad, easy, commercially affable; appears to take up exactly the right amount of space.',
        N'Merchant''s practical coat; road-stained; quality material; no House identification.',
        N'none',
        N'Wine trade routes through central vine country; deliveries that serve two purposes. He keeps impeccable accounts for both ledgers.',
        N'He has diverted small fixed amounts from intelligence drop payments for three years into a private cache. He has eight months of exit money saved. He adds to it every week.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Vine country trade circuit; border markets; courier drop points',
        N'0', N'0',
        N'French medieval merchant, dark hair, broad shoulders, stone wine cellar, two ledgers, careful eyes, dark fantasy Buehlman portrait',
        N'French medieval merchant, broad shoulders, dark hair, wine cellar, dark fantasy',
        0, 0
    );
    PRINT N'Luc Bessiere seeded.';
END
ELSE PRINT N'Luc Bessiere already exists.';
GO

-- ── 7. Alienor Vidal ──────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Alienor Vidal')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Alienor Vidal', N'alienor-vidal', N'canon', 1,
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
        @id, N'Alienor Vidal', N'alienor-vidal', N'Alienor', N'Vidal', N'',
        N'human', N'human', N'female', N'she/her',
        44, N'alive',
        N'Agricultural administrator; Liturgy transit liaison; passes Liturgy movement data to Atrament in exchange for estate protection.',
        N'Liturgy transit liaison embedded within Atrament''s agricultural administration. Passes Liturgy movement orders without being asked how she obtained them. Highly useful. Does not accept dinner invitations.',
        N'The negotiation every House makes with Liturgy — the price of useful relationships made human.',
        N'No POV.',
        N'House Atrament; agricultural administration, central vine country estate',
        165, 68, N'medium, composed; the stillness of someone who has learned that the right answer is to say nothing',
        N'dark brown going iron-grey', N'pulled back tight', N'medium',
        N'dark brown', N'warm brown', N'clear, careful',
        N'none',
        N'Still, watchful; occupies the exact chair she was assigned and does not move from it.',
        N'Administrator''s modest dress; no indication of secondary role.',
        N'none',
        N'Agricultural administration work; Liturgy contact management via courier. Evenings in deliberate solitude she will not explain.',
        N'She has moved eleven persons through Liturgy transit points over seven years. One was a child, presented to her as a debt settlement. She has never asked what became of the child and cannot start.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Central vine country estate; Liturgy transit corridor, regional',
        N'0', N'0',
        N'French medieval woman, dark greying hair, warm brown skin, estate office, Liturgy contact, dark fantasy Buehlman portrait',
        N'French medieval woman, greying hair, warm brown skin, estate interior, dark fantasy',
        0, 0
    );
    PRINT N'Alienor Vidal seeded.';
END
ELSE PRINT N'Alienor Vidal already exists.';
GO

-- ── 8. Marguerite Bonnet ──────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Marguerite Bonnet')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Marguerite Bonnet', N'marguerite-bonnet', N'canon', 1,
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
        @id, N'Marguerite Bonnet', N'marguerite-bonnet', N'Marguerite', N'Bonnet', N'',
        N'human', N'human', N'female', N'she/her',
        26, N'alive',
        N'Myrmidon assigned to diplomatic escort duty; observes and records everything said near her.',
        N'Myrmidon assigned to diplomatic escort duty. Quiet, reliable, unobtrusive enough that senior officials speak freely near her. She has noticed this. She writes things down.',
        N'Unintended witness — what the intelligence structure does not know it is building inside its own soldiers.',
        N'No POV.',
        N'House Atrament; garrison, vine country estate; diplomatic circuit',
        172, 70, N'athletic, unremarkable; the build of someone trained to stand at attention for hours',
        N'sandy brown', N'practical braid', N'medium',
        N'blue-grey', N'fair, freckled', N'clear',
        N'none',
        N'Correct escort posture; quiet, blends into formation; habit of noting all faces.',
        N'Standard Myrmidon escort kit; clean, unremarkable; no personal ornament.',
        N'none',
        N'Garrison duty, escort assignments, equipment maintenance. Carries a notebook inside her pack and fills it each evening.',
        N'She has filled four hand-stitched notebooks with verbatim overheard conversation from diplomatic escort missions — senior ministers, House advisors, private corridor arguments. She has told no one they exist.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Vine country estate; diplomatic circuit; inter-House corridors',
        N'0', N'0',
        N'Young French medieval female soldier, sandy hair braided, blue-grey eyes, freckled, escort duty, diplomatic hall, dark fantasy Buehlman portrait',
        N'Young French medieval woman soldier, sandy hair, escort duty, diplomatic hall, dark fantasy',
        0, 0
    );
    PRINT N'Marguerite Bonnet seeded.';
END
ELSE PRINT N'Marguerite Bonnet already exists.';
GO

-- ── 9. Yseult Laval ───────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Yseult Laval')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Yseult Laval', N'yseult-laval', N'canon', 1,
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
        @id, N'Yseult Laval', N'yseult-laval', N'Yseult', N'Laval', N'',
        N'human', N'human', N'female', N'she/her',
        61, N'alive',
        N'Retired senior ambassador; manages six-House envoy relationships from Atrament''s political architecture.',
        N'Former ambassador to House Draught. Retired in place within Atrament''s political architecture. Manages relationships with envoys from six Houses with the easy authority of someone who has outlasted three Lords.',
        N'Institutional memory; the human cost of endurance in a House that trades survival for information.',
        N'No POV.',
        N'House Atrament; central estate; formerly stationed at House Draught, twenty years',
        160, 62, N'slight but commanding; age has not reduced her, only concentrated her',
        N'white', N'elegant updo', N'medium',
        N'dark grey', N'olive', N'deeply lined; twenty years of Draught winters in the face',
        N'none',
        N'Slight, deliberate; commands rooms from her chair without rising.',
        N'Formal estate dress; House colours; no concession to age in presentation.',
        N'none',
        N'Advisory sessions, correspondence review, dinners where she guides younger diplomats without appearing to. She sleeps poorly.',
        N'She has maintained a private back-channel with a senior House Draught intelligence officer for eleven years — a relationship she was sent to exploit that became something else. His name appears in no Atrament archive.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Atrament central estate; visiting envoy circuit; rarely travels now',
        N'0', N'0',
        N'Older French medieval noblewoman, white hair elegant, dark grey eyes, olive skin, formal estate dress, diplomatic salon, dark fantasy Buehlman portrait',
        N'French medieval elder woman, white hair, formal estate dress, diplomatic salon, dark fantasy',
        0, 0
    );
    PRINT N'Yseult Laval seeded.';
END
ELSE PRINT N'Yseult Laval already exists.';
GO

-- ── 10. Raoul Serres ──────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Raoul Serres')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Raoul Serres', N'raoul-serres', N'canon', 1,
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
        @id, N'Raoul Serres', N'raoul-serres', N'Raoul', N'Serres', N'',
        N'human', N'human', N'male', N'he/him',
        35, N'alive',
        N'Field operative running three simultaneous cover identities for three separate principals.',
        N'Maintains three separate identities for three separate principals simultaneously. Exceptional cover artist. His file in Atrament''s archive is clean. His file in Ophiuchus''s archive is clean. He has no file with himself.',
        N'Identity as a technique — and what happens when the technique outlasts its practitioner''s sense of self.',
        N'No POV.',
        N'House Atrament (primary); field mobile; three simultaneous operational territories',
        173, 71, N'medium, deliberately unremarkable; the ideal body for cover work',
        N'mid-brown', N'varies by identity', N'short to medium',
        N'brown', N'medium', N'clear, unremarkable; the face you stop remembering on the road',
        N'none',
        N'Adjusts posture, gait, and register to match whichever identity he is currently running.',
        N'Changes by identity; carries three sets of clothes in a single bag.',
        N'none',
        N'Three cover identities, three residences, rotating schedule. Never more than eight consecutive days in one name.',
        N'He cannot identify which of his three cover identities was his original self. He has operated under simultaneous cover so long that the personal baseline is genuinely missing. He searches for it sometimes, quietly.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Vine country; Ophiuchus border territory; Liturgy transit corridors',
        N'0', N'0',
        N'French medieval man, deliberately unremarkable, brown hair, vine country crossroads, displacement in eyes, dark fantasy Buehlman portrait',
        N'French medieval man, unremarkable appearance, brown hair, crossroads, dark fantasy',
        0, 0
    );
    PRINT N'Raoul Serres seeded.';
END
ELSE PRINT N'Raoul Serres already exists.';
GO

-- ── 11. Blanche Aubert ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Blanche Aubert')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Blanche Aubert', N'blanche-aubert', N'canon', 1,
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
        @id, N'Blanche Aubert', N'blanche-aubert', N'Blanche', N'Aubert', N'',
        N'human', N'human', N'female', N'she/her',
        29, N'alive',
        N'Scrying operator; specializes in Sphere 31 population-movement analysis; maintaining a deliberate blind spot.',
        N'Scrying operator, second year. Specializes in Sphere 31 population-movement analysis. Thorough, accurate, excellent at pattern recognition. Has developed a specific blind spot she maintains with considerable effort.',
        N'The Scrying installation as ethical trap — what you cannot unknow once you have witnessed it.',
        N'No POV.',
        N'House Atrament; Scrying installation, vine country',
        157, 50, N'slight, fine-boned',
        N'ash-blonde', N'straight, simply tied', N'shoulder-length',
        N'pale grey', N'very fair', N'pale; the slight press of sleeplessness under the eyes',
        N'none',
        N'Quiet, contained; rarely initiates movement or conversation; useful stillness.',
        N'Operator''s plain robe; pale colours; hair tied simply back.',
        N'none',
        N'Scrying chamber shifts, population-movement reports, standard logs. Files one set with the archive; keeps a private set.',
        N'During a standard session she recorded Lord Renaud Colbert authorizing a Liturgy taking the public record credits to House Draught. She filed the session transcript under false instrument coordinates and has said nothing.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Scrying chamber and adjacent archive; residential quarters; rarely elsewhere',
        N'0', N'0',
        N'French medieval woman, ash-blonde hair, pale grey eyes, Scrying chamber glow, hidden guilt in expression, dark fantasy Buehlman portrait',
        N'French medieval woman, ash-blonde, pale grey eyes, Scrying chamber glow, dark fantasy',
        0, 0
    );
    PRINT N'Blanche Aubert seeded.';
END
ELSE PRINT N'Blanche Aubert already exists.';
GO

-- ── 12. Gilles Berthet ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Gilles Berthet')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Gilles Berthet', N'gilles-berthet', N'canon', 1,
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
        @id, N'Gilles Berthet', N'gilles-berthet', N'Gilles', N'Berthet', N'',
        N'human', N'human', N'male', N'he/him',
        49, N'alive',
        N'Paladin; House Atrament''s only current Transmutation success at that rank; deployed as deterrence, never publicly.',
        N'Paladin. House Atrament''s only current Transmutation success at that rank. Rarely deployed, never publicly. Functions as a deliberate absence in conversations — the option no one raises because raising it ends things.',
        N'The option Atrament keeps in reserve — what military capability looks like when it must remain theoretical.',
        N'No POV.',
        N'House Atrament; central estate, restricted quarters; vine country',
        199, 118, N'massive, post-human; skeletal reinforcement evident at jaw, brow, and shoulder',
        N'dark, shot through with silver', N'close-cropped', N'short',
        N'dark grey', N'tanned, scarred', N'deeply weathered; old Transmutation scarring at jaw and orbital ridge',
        N'Evident enhancement (Paladin)',
        N'Measured, controlled; post-human strength carried with deliberate restraint; never moves without reason.',
        N'Heavy training gear or estate formal; nothing in between; never appears in public.',
        N'Paladin-grade Transmutation; skeletal reinforcement at jaw, orbital ridge, and spine; musculature beyond natural proportion; accelerated cellular repair.',
        N'Drills alone or with a single trusted partner. No public training. Estate meetings when summoned. Visible absence otherwise.',
        N'He knows Lord Renaud is waiting for a situation that justifies deploying him in a way that cannot be attributed to Atrament. He has mapped three exit routes from vine country in case that order comes.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Atrament central estate; restricted Paladin quarters; perimeter only',
        N'0', N'0',
        N'Massive French medieval Paladin, dark silver-shot hair, scarred jaw, post-human frame, stone estate interior, dark fantasy Buehlman portrait',
        N'Massive French medieval Paladin, dark hair, scarred jaw, stone estate, dark fantasy',
        0, 0
    );
    PRINT N'Gilles Berthet seeded.';
END
ELSE PRINT N'Gilles Berthet already exists.';
GO

-- ── 13. Mathilde Leconte ──────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Mathilde Leconte')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Mathilde Leconte', N'mathilde-leconte', N'canon', 1,
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
        @id, N'Mathilde Leconte', N'mathilde-leconte', N'Mathilde', N'Leconte', N'Master',
        N'human', N'human', N'female', N'she/her',
        37, N'alive',
        N'Senior archivist (cover); intelligence interrogation specialist; controls which information reaches which desk.',
        N'Works out of Atrament''s central estate under the title of senior archivist. Extracts information from people who did not intend to provide it. Has a complete confession she has not filed.',
        N'The intelligence network''s internal immune system — and what it does when it cannot act on what it knows.',
        N'No POV.',
        N'House Atrament; central estate archive and interview rooms, vine country',
        167, 65, N'medium, composed; the stillness of someone who creates silences professionally',
        N'dark auburn', N'severe bun', N'medium',
        N'dark brown', N'olive', N'clear, faintly ink-stained at the temples',
        N'none',
        N'Economical, precise; occupies minimal space and invites others to fill the gap she creates.',
        N'Archivist''s practical dress; dark colours; ink-stained but controlled.',
        N'none',
        N'Archive work, subject interviews, reading intelligence summaries first. Controls which information reaches which desk, and when.',
        N'An interrogation produced a full signed confession naming Isabeau Mallet as a House Calyx informant. She suppressed the report because Isabeau holds a letter Mathilde wrote to an Oathless contact six years ago.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Atrament central estate archive and interview rooms; vine country',
        N'0', N'0',
        N'French medieval woman, dark auburn hair in severe bun, dark brown eyes, archive candlelight, interrogation quiet, dark fantasy Buehlman portrait',
        N'French medieval woman, dark auburn bun, archive candlelight, composed, dark fantasy',
        0, 0
    );
    PRINT N'Mathilde Leconte seeded.';
END
ELSE PRINT N'Mathilde Leconte already exists.';
GO

-- ── 14. Etienne Roux ──────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Etienne Roux')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Etienne Roux', N'etienne-roux', N'canon', 1,
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
        @id, N'Etienne Roux', N'etienne-roux', N'Etienne', N'Roux', N'',
        N'human', N'human', N'male', N'he/him',
        23, N'alive',
        N'Junior intelligence courier; delivers sealed packages across vine country and neighboring territories.',
        N'Junior courier, two years in the field. Fast, discreet, takes pride in delivery intact and on schedule. Has never questioned a package''s contents. Has noted that two prior handlers are dead.',
        N'Expendability as structure — what the network looks like from inside when you are the disposable part.',
        N'No POV.',
        N'House Atrament; field mobile; courier routes, vine country and borders',
        172, 67, N'lean, built for sustained pace',
        N'light brown', N'loose, road-disordered', N'short',
        N'green', N'fair', N'clear; always slightly flushed from movement',
        N'none',
        N'Quick, economical, always in transit; rarely fully still; eyes moving.',
        N'Traveller''s road clothes; no House identification; built for moving.',
        N'none',
        N'Courier routes through vine country and into neighboring territories. Pickup, delivery, receipt signature. Asks no questions. Keeps moving.',
        N'He has unknowingly carried bait packages — sealed intelligence designed to expose enemy couriers by how it is collected — on at least four runs. Two former handlers who assigned these runs are dead. He is beginning to understand the arithmetic.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Vine country courier circuit; border crossings; neighboring House territories',
        N'0', N'0',
        N'Young French medieval courier, light brown hair, green eyes, vine country road, travelling fast, dark fantasy Buehlman portrait',
        N'Young French medieval courier, light hair, green eyes, road travel, dark fantasy',
        0, 0
    );
    PRINT N'Etienne Roux seeded.';
END
ELSE PRINT N'Etienne Roux already exists.';
GO

-- ── 15. Beatris Faure ─────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Beatris Faure')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Beatris Faure', N'beatris-faure', N'canon', 1,
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
        @id, N'Beatris Faure', N'beatris-faure', N'Beatris', N'Faure', N'',
        N'human', N'human', N'female', N'she/her',
        46, N'alive',
        N'Scholar; analyzes Sphere 31 observation data for strategic pattern and technological extraction opportunity.',
        N'Scholar. Analyzes Sphere 31 observation data for strategic pattern and technological extraction. Has produced a proposal she believes could permanently change Atrament''s position among the Houses. Has told no one it exists.',
        N'The ambition that could transform or destroy Atrament — an idea that arrives before its institutional time.',
        N'No POV.',
        N'House Atrament; archive and analysis chamber, central estate, vine country',
        163, 70, N'soft medium; moves as though always slightly distracted by a better problem',
        N'dark brown streaked silver', N'loose, often forgotten', N'medium',
        N'brown', N'warm brown', N'clear; ink on her hands and frequently on her chin',
        N'none',
        N'Head slightly forward; reads every surface as though it might contain useful data.',
        N'Scholar''s layered robe; ink everywhere; no ornament whatsoever.',
        N'none',
        N'Observation data analysis; cross-referencing Sphere 31 technology with Cauld capability gaps. Proposals written late, alone.',
        N'She submitted a formal proposal arguing Atrament could open two-way contact with Sphere 31 rather than only observe it, filed under a false author''s name three months ago. No response has come. She adds to the proposal anyway.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Archive and analysis chamber, central estate; rarely leaves the installation complex',
        N'0', N'0',
        N'French medieval scholar woman, dark silver-streaked hair loose, warm brown skin, archive candlelight, ink-stained, dark fantasy Buehlman portrait',
        N'French medieval scholar, dark streaked hair, warm brown skin, archive candlelight, dark fantasy',
        0, 0
    );
    PRINT N'Beatris Faure seeded.';
END
ELSE PRINT N'Beatris Faure already exists.';
GO
