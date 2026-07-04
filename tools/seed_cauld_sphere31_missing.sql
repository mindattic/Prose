SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- CAULD UNIVERSE — SPHERE 31 (EARTH) TAKEN PERSONS
-- Universe: fantasy-steampunk (ID 0197E9C9-0002-7000-8000-000000000002)
-- 2026-07-04
-- 26 characters; idempotent (IF NOT EXISTS guards on all inserts)
-- Column order (44 non-system cols, Zora pattern):
--   Id Name Slug FirstName LastName TitlePrefix Species KindOfBeing
--   Gender Pronouns Age LifeStatus Role Description NarrativeFunction
--   NarrationVoice Heritage HeightCm WeightKg Build HairColor HairStyle
--   HairLength EyeColor SkinTone Complexion VisibleAugmentations
--   PostureMovement PhysicalClothingStyle Augmentations DailyLife
--   PsychologySecret SpeechVocabulary SpeechCadence SpeechSubtext
--   SpeechUnderPressure SpeechIntimacyRegister TerritoryRange
--   BioBatteryMaxCapacity BioBatteryRecovery MidjourneyPrompt Dalle3Prompt
--   Rating VoteCount
-- ═══════════════════════════════════════════════════════════════════════════════

-- ── Asha Degree ──────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Asha Degree')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Asha Degree', N'asha-degree', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Asha Degree', N'asha-degree', N'Asha', N'Degree', N'',
        N'human', N'human', N'female', N'she/her', 9, N'alive',
        N'Child ward; gifted to a House estate family by a House member who arranged the Piercing as a personal gift.',
        N'Taken from Sphere 31 (Earth), North Carolina, February 2000. Left home alone in the middle of a winter night. Was seen walking a highway in the rain; ran into the woods when a driver stopped. Backpack recovered a year later, buried at a construction site, containing items her family did not recognize. In the Cauld she is a child ward in a House estate — not a slave in name, but belonging to a family that did not produce her. She was nine when she arrived. She is now older. The backpack''s buried items were left by the Liturgy intermediary who prepared her transit point.',
        N'Evidence that the mechanism does not distinguish by age. She arrived as a child and the estate absorbed her the way estates absorb things given to them.',
        N'No POV.',
        N'Sphere 31 (Earth), North Carolina, United States',
        120, 23, N'child-slight',
        N'dark tight curls', N'worn simply', N'short',
        N'dark brown', N'deep brown', N'clear',
        N'none', N'child posture, not yet calibrated for concealment',
        N'whatever the estate provides',
        N'none',
        N'She is a ward of an estate. The family does not mistreat her and does not consider her their own. She has learned the Cauld''s language. She does not remember the highway or the rain.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House estate, interior territory',
        N'0', N'0',
        N'nine-year-old girl, dark tight curls, House estate interior, Cauld fantasy-steampunk, Buehlman dark register',
        N'nine-year-old girl, estate interior, dark fantasy',
        0, 0
    );
    PRINT 'Asha Degree seeded.';
END
ELSE PRINT 'Asha Degree already exists.';
GO

-- ── Sherrill Levitt ───────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Sherrill Levitt')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Sherrill Levitt', N'sherrill-levitt', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Sherrill Levitt', N'sherrill-levitt', N'Sherrill', N'Levitt', N'',
        N'human', N'human', N'female', N'she/her', 47, N'alive',
        N'Domestic labor conscript; assigned to House estate service alongside her daughter.',
        N'Taken from Sphere 31 (Earth), Springfield, Missouri, June 1992. Vanished overnight from her home with her daughter and daughter''s friend — three women, one house, no signs of struggle. The simultaneous taking was a single-operation Piercing, unusual in scale. In the Cauld she and her daughter Suzie were assigned to House estate service together. They are in the same House but not the same quarters. She is 47, which in a House estate means domestic supervisory labor — managing other conscripts, running the household rhythms she already knew how to run in Sphere 31.',
        N'One of the Springfield Three. Her presence alongside her daughter is the only thing that resembles continuity. She knows what happened. She does not say it aloud.',
        N'No POV.',
        N'Sphere 31 (Earth), Springfield, Missouri, United States',
        168, 65, N'average',
        N'light brown', N'practical', N'shoulder-length',
        N'brown', N'light-medium', N'clear',
        N'none', N'a woman who has managed households before and is managing one now under different terms',
        N'estate-issued domestic clothing',
        N'none',
        N'She and her daughter are in the same House. She knows this. It is the single fact she is organizing her life around.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House estate, domestic quarters',
        N'0', N'0',
        N'woman in her late forties, estate domestic interior, Cauld fantasy-steampunk, Buehlman dark register',
        N'middle-aged woman, estate interior, dark fantasy',
        0, 0
    );
    PRINT 'Sherrill Levitt seeded.';
END
ELSE PRINT 'Sherrill Levitt already exists.';
GO

-- ── Suzie Streeter ────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Suzie Streeter')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Suzie Streeter', N'suzie-streeter', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Suzie Streeter', N'suzie-streeter', N'Suzie', N'Streeter', N'',
        N'human', N'human', N'female', N'she/her', 19, N'alive',
        N'Forced marriage pool; assigned to a House loyalist family following arrival.',
        N'Taken from Sphere 31 (Earth), Springfield, Missouri, June 1992. Daughter of Sherrill Levitt. Vanished with her mother and her friend from the family home overnight. She was 19, recently graduated. In the Cauld she was assessed on arrival and placed in the forced marriage pool — young, healthy, no specialist profile. She was given to a mid-tier House loyalist family within three months. She and her mother are in the same House territory. They see each other occasionally. Her mother''s supervisory position was arranged partly to keep this visible proximity.',
        N'One of the Springfield Three. The cruelty of her situation is the half-proximity — close enough that both she and her mother know the other is present, not close enough that either has autonomy over it.',
        N'No POV.',
        N'Sphere 31 (Earth), Springfield, Missouri, United States',
        165, 58, N'young-slight',
        N'brown', N'worn simply', N'medium',
        N'brown', N'light-medium', N'clear',
        N'none', N'a young woman performing compliance and measuring every room she is in',
        N'what her assigned household provides',
        N'none',
        N'Her mother is nearby. This is the only thing she has. She is 19 and in a marriage she did not choose in a world she did not enter by choice.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House loyalist estate, same territory as her mother',
        N'0', N'0',
        N'young woman, nineteen, House estate interior, Cauld fantasy-steampunk, Buehlman dark register',
        N'young woman, estate interior, dark fantasy',
        0, 0
    );
    PRINT 'Suzie Streeter seeded.';
END
ELSE PRINT 'Suzie Streeter already exists.';
GO

-- ── Stacy McCall ──────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Stacy McCall')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Stacy McCall', N'stacy-mccall', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Stacy McCall', N'stacy-mccall', N'Stacy', N'McCall', N'',
        N'human', N'human', N'female', N'she/her', 18, N'alive',
        N'Domestic labor; placed in estate service in a different House from the Levitt women.',
        N'Taken from Sphere 31 (Earth), Springfield, Missouri, June 1992. Friend of Suzie Streeter, sleeping over at the Levitt house after graduation. She was 18, a guest — the only one of the three with no family connection to the house. In the Cauld the logistics separated her from the Levitts. She was processed and placed in estate domestic service in a different House. She has no one from her origin. She does not know where Sherrill and Suzie ended up.',
        N'One of the Springfield Three. The one with no anchor in the Cauld. She was a guest when she was taken and a stranger when she arrived.',
        N'No POV.',
        N'Sphere 31 (Earth), Springfield, Missouri, United States',
        163, 56, N'young-slight',
        N'blonde', N'worn simply', N'medium',
        N'blue', N'light', N'clear',
        N'none', N'trying to be unremarkable — the posture of someone who learned quickly that visibility is cost',
        N'what estate labor is issued',
        N'none',
        N'She does not know where the Levitt women are. She was 18 and sleeping over at a friend''s house.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House estate, domestic service, different territory from the Levitts',
        N'0', N'0',
        N'young woman, eighteen, estate labor interior, Cauld fantasy-steampunk, Buehlman dark register',
        N'young woman, estate service, alone, dark fantasy',
        0, 0
    );
    PRINT 'Stacy McCall seeded.';
END
ELSE PRINT 'Stacy McCall already exists.';
GO

-- ── Kyron Horman ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Kyron Horman')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Kyron Horman', N'kyron-horman', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Kyron Horman', N'kyron-horman', N'Kyron', N'Horman', N'',
        N'human', N'human', N'male', N'he/him', 7, N'alive',
        N'Child ward; gift to a House officer family with no children.',
        N'Taken from Sphere 31 (Earth), Portland, Oregon, June 2010. Disappeared from his elementary school after a science fair. His stepmother was the last person to see him. He was never captured on any camera leaving — because he did not leave through a door. He is now a ward of a House officer family in the Cauld. He was seven when he arrived. He no longer speaks any language from Sphere 31. He has grown up in the Cauld and understands it as the only world he has ever known.',
        N'A child who arrived young enough that the Cauld is simply his world. He does not remember Portland or the science fair. This is its own kind of horror — not the memory of displacement but the absence of one.',
        N'No POV.',
        N'Sphere 31 (Earth), Portland, Oregon, United States',
        117, 21, N'child-slight',
        N'red', N'short', N'short',
        N'green', N'light freckled', N'clear',
        N'none', N'a boy who has grown up in the Cauld and moves like someone who belongs here',
        N'House ward clothing',
        N'none',
        N'He does not remember Sphere 31. He was seven.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House officer estate',
        N'0', N'0',
        N'red-haired boy, House ward, Cauld fantasy-steampunk interior, Buehlman dark register',
        N'red-haired boy, estate interior, dark fantasy',
        0, 0
    );
    PRINT 'Kyron Horman seeded.';
END
ELSE PRINT 'Kyron Horman already exists.';
GO

-- ── Bryce Laspisa ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Bryce Laspisa')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Bryce Laspisa', N'bryce-laspisa', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Bryce Laspisa', N'bryce-laspisa', N'Bryce', N'Laspisa', N'',
        N'human', N'human', N'male', N'he/him', 19, N'alive',
        N'Myrmidon conscript; assigned to front-line combat unit.',
        N'Taken from Sphere 31 (Earth), California, August 2013. Was driving home from college, making erratic calls to family — the perceptual distortion some individuals near a membrane access point experience before Piercing. His car ended up at a reservoir. He was not in it and not in the water. In the Cauld he was assessed as physically capable and conscripted into a Myrmidon unit. He was 19, athletic, and had no particular skills that would divert him to specialized labor. He carries weapons he did not choose in a war he has no stake in.',
        N'A young man taken in the middle of what looked like a crisis in Sphere 31 and dropped into a war. The strange calls to his parents are the membrane''s effect on people near access points — disorientation, nonlinear thought. His family interpreted it as breakdown. It was transit.',
        N'No POV.',
        N'Sphere 31 (Earth), California, United States',
        180, 75, N'athletic',
        N'dark brown', N'short', N'short',
        N'brown', N'olive', N'clear',
        N'none', N'a soldier''s posture acquired under duress',
        N'Myrmidon field kit',
        N'none',
        N'He is in a war. He was 19 when he arrived. He understands the language now but not the politics.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Active front, Myrmidon unit assignment',
        N'0', N'0',
        N'young man, nineteen, Myrmidon armor, Cauld front-line, fantasy-steampunk warfare, Buehlman dark register',
        N'young man, soldier, front line, dark fantasy',
        0, 0
    );
    PRINT 'Bryce Laspisa seeded.';
END
ELSE PRINT 'Bryce Laspisa already exists.';
GO

-- ── Jane Beaumont ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Jane Beaumont')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Jane Beaumont', N'jane-beaumont', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Jane Beaumont', N'jane-beaumont', N'Jane', N'Beaumont', N'',
        N'human', N'human', N'female', N'she/her', 9, N'alive',
        N'Child slave; older sibling in a group take; assigned to estate labor once old enough.',
        N'Taken from Sphere 31 (Earth), Glenelg Beach, South Australia, January 1966. Oldest of the three Beaumont children. Vanished from a public beach in broad daylight with her two younger siblings. Witnesses saw them with an unknown man — a Liturgy intermediary. All three were Pierced simultaneously. Jane, at nine, retained more of Sphere 31 than her siblings. She arrived knowing she had siblings and that they had all been together on a beach. She was told they are in the same House territory. She is not permitted to verify this.',
        N'The eldest of three siblings taken simultaneously. She is old enough to know what was lost and young enough that the Cauld has had time to fill in over it. She is the one who will carry this.',
        N'No POV.',
        N'Sphere 31 (Earth), Glenelg Beach, South Australia, Australia',
        132, 27, N'child-slight',
        N'blonde', N'beach-worn then estate-kept', N'medium',
        N'blue', N'light', N'clear',
        N'none', N'a girl who arrived with her siblings and learned quickly to show nothing',
        N'estate-issued',
        N'none',
        N'She arrived with Arnna and Grant. She knows they are somewhere in the same territory. She does not know if they remember the beach.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House estate territory, same House as siblings',
        N'0', N'0',
        N'nine-year-old girl, blonde, Cauld estate interior, 1966 South Australian beach origin, Buehlman dark register',
        N'nine-year-old girl, estate interior, dark fantasy',
        0, 0
    );
    PRINT 'Jane Beaumont seeded.';
END
ELSE PRINT 'Jane Beaumont already exists.';
GO

-- ── Arnna Beaumont ────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Arnna Beaumont')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Arnna Beaumont', N'arnna-beaumont', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Arnna Beaumont', N'arnna-beaumont', N'Arnna', N'Beaumont', N'',
        N'human', N'human', N'female', N'she/her', 7, N'alive',
        N'Child ward; middle Beaumont sibling; placed in estate care.',
        N'Taken from Sphere 31 (Earth), Glenelg Beach, South Australia, January 1966. Middle of the three Beaumont children. Seven years old when she arrived. She remembers less of Sphere 31 than Jane and more than Grant. She remembers the beach and the man and her sister holding her hand. She is in the same House territory as both siblings but not the same quarters.',
        N'Middle child of the Beaumont three. Old enough to remember, young enough that the Cauld has had time to fill in over it.',
        N'No POV.',
        N'Sphere 31 (Earth), Glenelg Beach, South Australia, Australia',
        117, 20, N'child-slight',
        N'blonde', N'estate-kept', N'short',
        N'blue', N'light', N'clear',
        N'none', N'a seven-year-old who watches adults carefully',
        N'estate-issued',
        N'none',
        N'She remembers Jane''s hand. She knows Jane is somewhere nearby. She does not know where Grant is.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House estate territory, same House as siblings',
        N'0', N'0',
        N'seven-year-old girl, blonde, Cauld estate interior, Buehlman dark register',
        N'seven-year-old girl, estate interior, dark fantasy',
        0, 0
    );
    PRINT 'Arnna Beaumont seeded.';
END
ELSE PRINT 'Arnna Beaumont already exists.';
GO

-- ── Grant Beaumont ────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Grant Beaumont')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Grant Beaumont', N'grant-beaumont', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Grant Beaumont', N'grant-beaumont', N'Grant', N'Beaumont', N'',
        N'human', N'human', N'male', N'he/him', 4, N'alive',
        N'Child ward; youngest Beaumont sibling; placed in estate family care.',
        N'Taken from Sphere 31 (Earth), Glenelg Beach, South Australia, January 1966. Youngest of the Beaumont children. Four years old. He does not remember the beach. He does not remember his sisters. He is the one who arrived and was simply absorbed — the youngest of the three and the cleanest slate. He is in the same House territory as Jane and Arnna and does not know this.',
        N'Youngest of the Beaumont three. He arrived at four and the Cauld is simply the world. He does not know he has sisters nearby.',
        N'No POV.',
        N'Sphere 31 (Earth), Glenelg Beach, South Australia, Australia',
        100, 16, N'toddler-slight',
        N'blonde', N'estate-kept', N'short',
        N'blue', N'light', N'clear',
        N'none', N'a small child who grew up in an estate and belongs here in every way he understands',
        N'estate-issued',
        N'none',
        N'He does not remember the beach. He does not know his sisters are nearby.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House estate territory, same House as siblings',
        N'0', N'0',
        N'young boy, blonde, Cauld estate interior, Buehlman dark register, child ward',
        N'young boy, estate interior, dark fantasy',
        0, 0
    );
    PRINT 'Grant Beaumont seeded.';
END
ELSE PRINT 'Grant Beaumont already exists.';
GO

-- ── Danielle Imbo ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Danielle Imbo')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Danielle Imbo', N'danielle-imbo', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Danielle Imbo', N'danielle-imbo', N'Danielle', N'Imbo', N'',
        N'human', N'human', N'female', N'she/her', 34, N'alive',
        N'Domestic labor conscript; assigned to estate service.',
        N'Taken from Sphere 31 (Earth), Philadelphia, Pennsylvania, February 2000. Left a bar with Richard Patrone and vanished with him — two people, one vehicle, no trace. The vehicle was Pierced along with them and stripped for Liturgy materials cataloguing. Danielle and Richard were processed separately. She was placed in estate domestic service. He was sent to labor conscription. They are not in the same territory.',
        N'She and Richard were taken together and separated on arrival. Standard Liturgy procedure — companions are split to prevent organized resistance. She does not know where he is.',
        N'No POV.',
        N'Sphere 31 (Earth), Philadelphia, Pennsylvania, United States',
        165, 60, N'average',
        N'dark brown', N'styled then estate-issued', N'shoulder-length',
        N'brown', N'light-medium', N'clear',
        N'none', N'a woman who has stopped expecting anything to be what it appeared',
        N'estate domestic issue',
        N'none',
        N'She was taken with Richard. She does not know where Richard is. She was 34 with a son she will not see again.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House estate, domestic service',
        N'0', N'0',
        N'woman in her mid-thirties, estate domestic interior, Cauld fantasy-steampunk, Buehlman dark register',
        N'woman, estate service interior, dark fantasy',
        0, 0
    );
    PRINT 'Danielle Imbo seeded.';
END
ELSE PRINT 'Danielle Imbo already exists.';
GO

-- ── Richard Patrone ───────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Richard Patrone')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Richard Patrone', N'richard-patrone', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Richard Patrone', N'richard-patrone', N'Richard', N'Patrone', N'',
        N'human', N'human', N'male', N'he/him', 39, N'alive',
        N'Labor conscript; quarry and construction detail.',
        N'Taken from Sphere 31 (Earth), Philadelphia, Pennsylvania, February 2000. Taken alongside Danielle Imbo when the vehicle they were in was Pierced. He was 39 and assessed for labor conscription — older than typical Myrmidon intake but physically capable. Sent to quarry and construction work, which is where adult male conscripts without specialist skills end up. He does not know where Danielle is.',
        N'Taken with Danielle, separated on arrival. He was 39 and driving. Now he is in a quarry.',
        N'No POV.',
        N'Sphere 31 (Earth), Philadelphia, Pennsylvania, United States',
        178, 82, N'stocky',
        N'dark brown', N'short', N'short',
        N'brown', N'medium', N'clear',
        N'none', N'a man doing heavy work with the efficiency of someone who has stopped thinking about why',
        N'labor conscript issue',
        N'none',
        N'He does not know where Danielle is. He was 39. He is in a quarry.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Labor conscript detail, quarry territory',
        N'0', N'0',
        N'man in his late thirties, Cauld quarry labor, fantasy-steampunk work site, Buehlman dark register',
        N'man, quarry, labor, dark fantasy',
        0, 0
    );
    PRINT 'Richard Patrone seeded.';
END
ELSE PRINT 'Richard Patrone already exists.';
GO

-- ── Jennifer Kesse ────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Jennifer Kesse')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Jennifer Kesse', N'jennifer-kesse', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Jennifer Kesse', N'jennifer-kesse', N'Jennifer', N'Kesse', N'',
        N'human', N'human', N'female', N'she/her', 24, N'alive',
        N'Skilled labor; assigned to House financial records office — her accounting knowledge translated directly.',
        N'Taken from Sphere 31 (Earth), Orlando, Florida, January 2006. Young financial analyst. The unknown person who parked her car was a Liturgy intermediary. In the Cauld her numeracy and records management skills were identified. A House financial administrator trained her over three months and she now works the estate''s ledger accounting. She does this because the alternative is domestic service, and because she is good at it, and because being useful is its own kind of survival strategy.',
        N'A young professional whose specific skills made her worth more to the Cauld as a specialist than as domestic labor. She is alive and functional and doing work she was trained for in a world she did not agree to enter.',
        N'No POV.',
        N'Sphere 31 (Earth), Orlando, Florida, United States',
        163, 56, N'slight',
        N'blonde', N'professional then estate-kept', N'medium',
        N'blue', N'light', N'clear',
        N'none', N'a young professional doing her job very carefully in conditions she did not choose',
        N'estate administrative issue',
        N'none',
        N'She is running ledgers for a House. She was 24 and a financial analyst. The currency is different.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House administrative territory, records office',
        N'0', N'0',
        N'young woman, twenty-four, House records office, Cauld fantasy-steampunk, Buehlman dark register',
        N'young woman, administrative office interior, dark fantasy',
        0, 0
    );
    PRINT 'Jennifer Kesse seeded.';
END
ELSE PRINT 'Jennifer Kesse already exists.';
GO

-- ── Tammy Lynn Leppert ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Tammy Lynn Leppert')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Tammy Lynn Leppert', N'tammy-lynn-leppert', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Tammy Lynn Leppert', N'tammy-lynn-leppert', N'Tammy Lynn', N'Leppert', N'',
        N'human', N'human', N'female', N'she/her', 18, N'alive',
        N'Witness removal; taken because she saw a Piercing in progress and talked about it; now in Liturgy service.',
        N'Taken from Sphere 31 (Earth), Rockledge, Florida, July 1983. Minor actress with a background role in Scarface. She had been erratic and frightened before her disappearance and told people she had witnessed something she would not specify. She had witnessed a Piercing — a street-level scout operation near the film location. She saw more than she should have seen. She told people. The Liturgy removed her. In the Cauld she was placed in Liturgy administrative service specifically because she was an actress — she could memorize, perform, and recite. She now delivers formal Liturgy announcements at House ceremonies. She performs the words of people who took her.',
        N'The Liturgy removed a witness and then put the witness to work. She is effective at her role. This is the detail that makes it worse.',
        N'No POV.',
        N'Sphere 31 (Earth), Rockledge, Florida, United States',
        165, 56, N'slight',
        N'blonde', N'Liturgy-formal dress', N'medium',
        N'blue', N'light', N'clear',
        N'none', N'performer''s posture — straight, deliberate, trained to project',
        N'Liturgy formal dress',
        N'none',
        N'She saw a Piercing and she talked about it. Now she announces Liturgy ceremonies. She is good at this. She was always good at performing.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Liturgy administrative territory',
        N'0', N'0',
        N'young woman, eighteen, Liturgy formal dress, ceremony interior, Cauld fantasy-steampunk, Buehlman dark register',
        N'young woman, formal ceremony, Liturgy service, dark fantasy',
        0, 0
    );
    PRINT 'Tammy Lynn Leppert seeded.';
END
ELSE PRINT 'Tammy Lynn Leppert already exists.';
GO

-- ── Brian Shaffer ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Brian Shaffer')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Brian Shaffer', N'brian-shaffer', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Brian Shaffer', N'brian-shaffer', N'Brian', N'Shaffer', N'',
        N'human', N'human', N'male', N'he/him', 27, N'alive',
        N'Medical specialist conscript; assigned to House field hospital.',
        N'Taken from Sphere 31 (Earth), Columbus, Ohio, April 2006. Medical student. Last seen on camera entering a bar; never captured leaving through any exit. The Piercing was precise — taken from inside a building with full camera coverage. In the Cauld his medical knowledge was identified during intake. He was not sent to Myrmidon. He was sent to a House field hospital and assigned to work alongside the Cauld''s own practitioners. His Sphere 31 medical training requires substantial retraining but he understands bodies and he learns. He is kept because skilled medical personnel are hard to produce.',
        N'A medical student taken for his skills and put to work in a field hospital. He is the one in the room with the most education and the least power. He saves lives in a war he was conscripted into.',
        N'No POV.',
        N'Sphere 31 (Earth), Columbus, Ohio, United States',
        180, 77, N'lean',
        N'brown', N'short', N'short',
        N'brown', N'light-medium', N'clear',
        N'none', N'a physician''s posture — focused, economical, present',
        N'field hospital issue',
        N'none',
        N'He is in a field hospital. He was 27 and a medical student. He treats injuries he has no names for in a language he learned under duress.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House field hospital, front-adjacent territory',
        N'0', N'0',
        N'young man, twenty-seven, field hospital interior, Cauld fantasy-steampunk, Buehlman dark register',
        N'young man, field hospital, dark fantasy',
        0, 0
    );
    PRINT 'Brian Shaffer seeded.';
END
ELSE PRINT 'Brian Shaffer already exists.';
GO

-- ── Andrew Gosden ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Andrew Gosden')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Andrew Gosden', N'andrew-gosden', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Andrew Gosden', N'andrew-gosden', N'Andrew', N'Gosden', N'',
        N'human', N'human', N'male', N'he/him', 14, N'alive',
        N'Liturgy acolyte; semi-voluntary recruit; one of the rare cases of a willing departure from Sphere 31.',
        N'Taken from Sphere 31 (Earth), Doncaster, England, September 2007. He bought a one-way ticket to London. He left his return fare at home. He told no one where he was going. He was fourteen. The one-way ticket and the deliberate nature of the departure suggest he was recruited — that a Liturgy scout had contacted him, told him something, and arranged his transit to London where a membrane access point existed. He arrived in the Cauld having chosen to come, in the way a fourteen-year-old can choose anything when an adult with a convincing story tells him there is something better. In the Cauld he was placed in Liturgy acolyte training. He is older now, educated in Liturgy doctrine, and functional within the institution that took him. He has not decided yet whether he was rescued or stolen.',
        N'The one who came willingly — or as willingly as a fourteen-year-old who was told a story and believed it can come willingly. He is the Cauld''s argument that some takings are not takings. He is also evidence of what the Liturgy does with that argument.',
        N'No POV.',
        N'Sphere 31 (Earth), Doncaster / London, England',
        160, 50, N'adolescent-slight',
        N'brown', N'Liturgy acolyte formal', N'short',
        N'brown', N'light', N'clear',
        N'none', N'an acolyte''s posture — disciplined, observant, trained',
        N'Liturgy acolyte dress',
        N'none',
        N'He was 14 and he came because someone told him something worth leaving for. He has not decided if what they told him was true.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Liturgy training territory',
        N'0', N'0',
        N'teenage boy, Liturgy acolyte dress, Cauld institutional interior, fantasy-steampunk, Buehlman dark register',
        N'teenage boy, Liturgy interior, dark fantasy',
        0, 0
    );
    PRINT 'Andrew Gosden seeded.';
END
ELSE PRINT 'Andrew Gosden already exists.';
GO

-- ── Brandon Swanson ───────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Brandon Swanson')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Brandon Swanson', N'brandon-swanson', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Brandon Swanson', N'brandon-swanson', N'Brandon', N'Swanson', N'',
        N'human', N'human', N'male', N'he/him', 19, N'alive',
        N'Myrmidon conscript; active front assignment.',
        N'Taken from Sphere 31 (Earth), rural Minnesota, May 2008. His car went off a road; he called his parents and was walking home in the dark when he said "Oh s***" and the call went dead. His parents were on the phone. The membrane was thin there — a rural road in Minnesota sits near a geographic access point, and the Liturgy was running a harvest that night. He was taken mid-sentence. His parents heard the moment of transit and will spend the rest of their lives interpreting it as something else. He was 19, physically capable, no specialized training. Myrmidon.',
        N'His parents heard the taking. The "Oh s***" is the moment of transit — surprise, not injury, not a fall. He is in a war. They are on a phone that went silent.',
        N'No POV.',
        N'Sphere 31 (Earth), rural Minnesota, United States',
        180, 77, N'athletic',
        N'brown', N'short', N'short',
        N'brown', N'light', N'clear',
        N'none', N'a soldier''s posture, acquired fast',
        N'Myrmidon field kit',
        N'none',
        N'He is in a war. His parents think he fell into a river. He said "Oh s***" and arrived here.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Active front, Myrmidon unit',
        N'0', N'0',
        N'young man, nineteen, Myrmidon kit, Cauld front line, fantasy-steampunk warfare, Buehlman dark register',
        N'young man, soldier, dark fantasy, front line',
        0, 0
    );
    PRINT 'Brandon Swanson seeded.';
END
ELSE PRINT 'Brandon Swanson already exists.';
GO

-- ── Skye Budnick ──────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Skye Budnick')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Skye Budnick', N'skye-budnick', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Skye Budnick', N'skye-budnick', N'Skye', N'Budnick', N'',
        N'human', N'human', N'female', N'she/her', 20, N'alive',
        N'Domestic service; assigned to House estate following arrival in Tokyo transit.',
        N'Taken from Sphere 31 (Earth), Tokyo, Japan, 2012. Booked a one-way flight to Japan alone without telling family. Was briefly spotted in Tokyo. Tokyo has a confirmed membrane access point in its eastern districts — the Liturgy runs transit operations from it. In the Cauld she was assessed as healthy and young with no specialist skills and placed in domestic service. She speaks Japanese and English and neither is useful in the Cauld except that she learns the Cauld''s language faster than some because she is already multilingual.',
        N'A young woman who arrived alone having already made a deliberate departure — from her family, from the expected path. The Cauld absorbed her without knowing or caring what she was looking for.',
        N'No POV.',
        N'Sphere 31 (Earth), Tokyo, Japan',
        162, 55, N'slight',
        N'brown', N'practical', N'medium',
        N'brown', N'light-medium', N'clear',
        N'none', N'a young woman who moves like she is still deciding whether to trust the room',
        N'estate domestic issue',
        N'none',
        N'She bought a one-way ticket because she was looking for something. She arrived here.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House estate, domestic territory',
        N'0', N'0',
        N'young woman, twenty, estate domestic interior, Cauld fantasy-steampunk, Buehlman dark register',
        N'young woman, estate interior, dark fantasy',
        0, 0
    );
    PRINT 'Skye Budnick seeded.';
END
ELSE PRINT 'Skye Budnick already exists.';
GO

-- ── Sharon Pretorius ──────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Sharon Pretorius')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Sharon Pretorius', N'sharon-pretorius', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Sharon Pretorius', N'sharon-pretorius', N'Sharon', N'Pretorius', N'',
        N'human', N'human', N'female', N'she/her', 13, N'alive',
        N'Child slave; taken via intermediary-assisted street-level Piercing.',
        N'Taken from Sphere 31 (Earth), Dayton, Ohio, 1973. Thirteen years old. A witness saw her being restrained by an unknown man near her home. The man was a Liturgy intermediary conducting a street-level pick for a House order. She was not a directed gift but a bulk order: young, female, no particular target, just a body for a House estate''s domestic needs. In the Cauld she was placed in estate service at thirteen. She is older now. She has learned the language and the rhythms of the House she was given to.',
        N'Taken by a man in the street in front of a witness. The witness''s testimony exists in Sphere 31 records. The man was never identified. She is in a House estate.',
        N'No POV.',
        N'Sphere 31 (Earth), Dayton, Ohio, United States',
        150, 45, N'adolescent-slight',
        N'dark brown', N'estate-issued', N'medium',
        N'brown', N'medium', N'clear',
        N'none', N'the posture of someone who learned to make herself unremarkable',
        N'estate domestic issue',
        N'none',
        N'A man grabbed her in the street in front of a witness. She is in an estate now. The witness''s account still exists in Sphere 31. It has resolved nothing.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House estate, domestic territory',
        N'0', N'0',
        N'teenage girl, estate domestic interior, Cauld fantasy-steampunk, Buehlman dark register',
        N'teenage girl, estate interior, dark fantasy',
        0, 0
    );
    PRINT 'Sharon Pretorius seeded.';
END
ELSE PRINT 'Sharon Pretorius already exists.';
GO

-- ── Jason Jolkowski ───────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Jason Jolkowski')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Jason Jolkowski', N'jason-jolkowski', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Jason Jolkowski', N'jason-jolkowski', N'Jason', N'Jolkowski', N'',
        N'human', N'human', N'male', N'he/him', 19, N'alive',
        N'Myrmidon conscript; standard intake for young male without specialist skills.',
        N'Taken from Sphere 31 (Earth), Omaha, Nebraska, June 2001. Was walking to meet a coworker for a ride to work — a short, familiar route. He never arrived. No witnesses. The access point was on that block — the Liturgy runs efficient harvests on street-level points, and he was there at the right time for the wrong operation. In the Cauld he was processed and conscripted. Young, male, no specialist profile. Myrmidon.',
        N'A young man walking to work who stepped through a point the Liturgy was using. There is no more complexity than that. He was 19 and walking to work.',
        N'No POV.',
        N'Sphere 31 (Earth), Omaha, Nebraska, United States',
        178, 75, N'average',
        N'brown', N'short', N'short',
        N'brown', N'light-medium', N'clear',
        N'none', N'a soldier''s posture, newly acquired',
        N'Myrmidon field kit',
        N'none',
        N'He was walking to work. He is in a war. He was 19.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Active front, Myrmidon unit',
        N'0', N'0',
        N'young man, nineteen, Myrmidon kit, Cauld front line, fantasy-steampunk, Buehlman dark register',
        N'young man, soldier, front line, dark fantasy',
        0, 0
    );
    PRINT 'Jason Jolkowski seeded.';
END
ELSE PRINT 'Jason Jolkowski already exists.';
GO

-- ── Summer Wells ──────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Summer Wells')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Summer Wells', N'summer-wells', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Summer Wells', N'summer-wells', N'Summer', N'Wells', N'',
        N'human', N'human', N'female', N'she/her', 5, N'alive',
        N'Child ward; taken from rural property; placed in estate family care.',
        N'Taken from Sphere 31 (Earth), Rogersville, Tennessee, June 2021. Five years old. Vanished from her family''s rural property. Massive searches found nothing because she was not on the property anymore. In the Cauld she was placed with an estate family as a ward. She was five. She does not remember Tennessee.',
        N'Five years old. The Cauld receives the very young without ceremony and the estate absorbs them. She does not know what was taken from her.',
        N'No POV.',
        N'Sphere 31 (Earth), Rogersville, Tennessee, United States',
        107, 18, N'child-small',
        N'light brown', N'loose', N'medium',
        N'blue', N'light', N'clear',
        N'none', N'a small child, unrestricted movement within the estate',
        N'estate-issued',
        N'none',
        N'She does not remember Tennessee. She was five.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House estate, child ward quarters',
        N'0', N'0',
        N'five-year-old girl, light brown hair, Cauld estate interior, Buehlman dark register',
        N'young child, estate interior, dark fantasy',
        0, 0
    );
    PRINT 'Summer Wells seeded.';
END
ELSE PRINT 'Summer Wells already exists.';
GO

-- ── Jodi Huisentruit ──────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Jodi Huisentruit')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Jodi Huisentruit', N'jodi-huisentruit', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Jodi Huisentruit', N'jodi-huisentruit', N'Jodi', N'Huisentruit', N'',
        N'human', N'human', N'female', N'she/her', 27, N'alive',
        N'Skilled rhetoric specialist; assigned to House communications — her broadcast training translated to formal announcement and diplomatic messaging.',
        N'Taken from Sphere 31 (Earth), Mason City, Iowa, June 1995. Television news anchor, 27. Vanished from her apartment parking lot on the way to anchor the morning news. Evidence of a struggle at her car — physical contact from a Liturgy intermediary managing the transit point. She resisted. In the Cauld her specific skill set — clear articulation, memorization, performance under pressure, authoritative vocal delivery — was identified as valuable to a House communications function. She now drafts and delivers formal House messaging to coalition partners and internal staff. She does this because the alternative was worse and because she is, professionally, exactly what the House needed.',
        N'A journalist and anchor taken for what she was trained to do and doing it. The horror is the fit. She is genuinely good at this job. The House has not noticed that she is also observing everything.',
        N'No POV.',
        N'Sphere 31 (Earth), Mason City, Iowa, United States',
        165, 58, N'slight',
        N'blonde', N'House formal dress for announcements', N'medium',
        N'blue', N'light', N'clear',
        N'none', N'news anchor posture — straight, present, projecting confidence for an audience that is also watching her',
        N'House formal messaging dress',
        N'none',
        N'She is delivering House communications. She was an anchor. She knows how to perform certainty she does not feel. She is watching everything.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House communications territory, formal announcement chambers',
        N'0', N'0',
        N'young woman, twenty-seven, House formal announcement chamber, Cauld fantasy-steampunk, Buehlman dark register',
        N'young woman, formal chamber, herald, dark fantasy',
        0, 0
    );
    PRINT 'Jodi Huisentruit seeded.';
END
ELSE PRINT 'Jodi Huisentruit already exists.';
GO

-- ── Macin Smith ───────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Macin Smith')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Macin Smith', N'macin-smith', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Macin Smith', N'macin-smith', N'Macin', N'Smith', N'',
        N'human', N'human', N'male', N'he/him', 17, N'alive',
        N'Liturgy scout-in-training; semi-voluntary recruit identified through social vulnerability.',
        N'Taken from Sphere 31 (Earth), St. George, Utah, September 2015. Seventeen-year-old from a reportedly controlling religious home environment. He left. He was sighted in several western US cities in the weeks after he disappeared, which means a Liturgy recruiter was tracking him and letting him run before making contact. The recruiter offered him something — a way out, a better place, the suggestion that what he was leaving was not worth returning to. He came willingly enough. In the Cauld he was placed in Liturgy scout training — a natural fit for someone who can blend into unfamiliar environments and has reason to distrust authority structures. He is being trained to find people in Sphere 31 and bring them through.',
        N'A teenager recruited from a bad situation by an institution that will use him to replicate that bad situation for others. He does not know this yet. Or he is beginning to.',
        N'No POV.',
        N'Sphere 31 (Earth), St. George, Utah, United States',
        175, 65, N'adolescent-slight',
        N'brown', N'Liturgy scout practical dress', N'short',
        N'brown', N'light-medium', N'clear',
        N'none', N'someone who learned to move through unfamiliar environments without drawing attention',
        N'Liturgy scout issue',
        N'none',
        N'He left a controlling home and was recruited by a controlling institution. He is learning to scout for it.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Liturgy scout training territory',
        N'0', N'0',
        N'teenage boy, Liturgy scout clothing, Cauld training grounds, fantasy-steampunk, Buehlman dark register',
        N'teenage boy, Liturgy training, dark fantasy',
        0, 0
    );
    PRINT 'Macin Smith seeded.';
END
ELSE PRINT 'Macin Smith already exists.';
GO

-- ── William Tyrrell ───────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'William Tyrrell')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'William Tyrrell', N'william-tyrrell', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'William Tyrrell', N'william-tyrrell', N'William', N'Tyrrell', N'',
        N'human', N'human', N'male', N'he/him', 3, N'alive',
        N'Child ward; gift to a House family; the Spider-Man suit was the last image of him in Sphere 31.',
        N'Taken from Sphere 31 (Earth), Kendall, New South Wales, Australia, September 2014. Three years old. Was wearing a Spider-Man suit when he vanished from a foster grandmother''s garden. In the Cauld he was placed with a House family as a ward. He was three. The Spider-Man suit arrived with him; it was taken as a Gifted material curiosity by the receiving Liturgy officer and catalogued. He does not remember the garden or the suit or Sphere 31.',
        N'Three years old in a Spider-Man suit. The image that defines his disappearance in Sphere 31 is the suit. The suit is in a Liturgy materials catalogue.',
        N'No POV.',
        N'Sphere 31 (Earth), Kendall, New South Wales, Australia',
        95, 14, N'toddler',
        N'light brown', N'estate-issued — no longer in the Spider-Man suit', N'short',
        N'blue', N'light', N'clear',
        N'none', N'a toddler, now slightly older, belonging entirely to the estate',
        N'estate-issued children''s clothing',
        N'none',
        N'He was three and wearing a Spider-Man suit. The suit is in a Liturgy catalogue. He does not remember any of this.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House estate, child ward quarters',
        N'0', N'0',
        N'toddler, light brown hair, Cauld estate interior, Buehlman dark register, child ward',
        N'toddler, estate interior, dark fantasy',
        0, 0
    );
    PRINT 'William Tyrrell seeded.';
END
ELSE PRINT 'William Tyrrell already exists.';
GO

-- ── Elizabeth Bain ────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Elizabeth Bain')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Elizabeth Bain', N'elizabeth-bain', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Elizabeth Bain', N'elizabeth-bain', N'Elizabeth', N'Bain', N'',
        N'human', N'human', N'female', N'she/her', 22, N'alive',
        N'Domestic labor conscript; the wrongful conviction her disappearance caused in Sphere 31 is not known to her.',
        N'Taken from Sphere 31 (Earth), Scarborough, Ontario, Canada, June 1990. University student, 22. Disappeared after a tennis match. A man was wrongly convicted of her murder and spent years in prison. Her body was never found because she was not dead — she was in the Cauld. She does not know that an innocent man was imprisoned for her disappearance. The Cauld has no mechanism by which she could know this. In the Cauld she was placed in domestic service. She is older now, fluent in the Cauld''s language, functioning.',
        N'She is the invisible harm made concrete: a woman alive in service in another world while a man in her world went to prison for killing her. The harm is real on both sides of the membrane and invisible on both sides.',
        N'No POV.',
        N'Sphere 31 (Earth), Scarborough, Ontario, Canada',
        165, 58, N'slight',
        N'dark brown', N'estate domestic issue', N'medium',
        N'dark brown', N'light-medium', N'clear',
        N'none', N'a woman who has been in domestic service long enough that it is simply her life now',
        N'estate domestic issue',
        N'none',
        N'A man was wrongly convicted of her murder. She does not know this. She is alive in another world.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House estate, domestic service',
        N'0', N'0',
        N'woman in her twenties, estate domestic interior, Cauld fantasy-steampunk, Buehlman dark register',
        N'woman, estate service, dark fantasy',
        0, 0
    );
    PRINT 'Elizabeth Bain seeded.';
END
ELSE PRINT 'Elizabeth Bain already exists.';
GO

-- ── Johnny Gosch ──────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Johnny Gosch')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Johnny Gosch', N'johnny-gosch', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Johnny Gosch', N'johnny-gosch', N'Johnny', N'Gosch', N'',
        N'human', N'human', N'male', N'he/him', 12, N'alive',
        N'Child slave; taken via vehicle-assisted Liturgy intermediary operation; assigned to House estate labor.',
        N'Taken from Sphere 31 (Earth), West Des Moines, Iowa, September 1982. Twelve-year-old on his morning paper route. Witnesses saw a man and a car — a Liturgy intermediary with a vehicle, conducting a street-level pick in a residential area before sunrise. He was twelve. In the Cauld he was old enough to be assessed for light labor. House estate work: errands, hauling, whatever the estate needs. His disappearance became famous in Sphere 31 and led to identification programs for missing children. In the Cauld no one knows this or cares.',
        N'A twelve-year-old taken on his paper route before sunrise. His case changed how Sphere 31 thinks about missing children. In the Cauld he is doing estate labor.',
        N'No POV.',
        N'Sphere 31 (Earth), West Des Moines, Iowa, United States',
        150, 42, N'adolescent-slight',
        N'blonde', N'estate labor issue', N'short',
        N'blue', N'light', N'clear',
        N'none', N'a boy who grew up doing estate labor and has learned to be efficient at it',
        N'estate labor issue',
        N'none',
        N'He was twelve and delivering newspapers before sunrise. He is in an estate doing labor. His case changed Sphere 31. The Cauld does not know this.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House estate, labor assignment',
        N'0', N'0',
        N'boy, blonde, estate labor setting, Cauld fantasy-steampunk, Buehlman dark register',
        N'boy, estate labor, dark fantasy',
        0, 0
    );
    PRINT 'Johnny Gosch seeded.';
END
ELSE PRINT 'Johnny Gosch already exists.';
GO

-- ── Josh Guimond ──────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Josh Guimond')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Josh Guimond', N'josh-guimond', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Josh Guimond', N'josh-guimond', N'Josh', N'Guimond', N'',
        N'human', N'human', N'male', N'he/him', 21, N'alive',
        N'Myrmidon conscript; taken on a short walk between two points; active front assignment.',
        N'Taken from Sphere 31 (Earth), Collegeville, Minnesota, November 2002. College student walking from a party to his dorm — a walk of a few hundred meters. The path crossed a membrane access point the Liturgy was harvesting that night. He did not arrive at his dorm. He was 21. In the Cauld he was assessed as healthy, young, and without specialist skills. Myrmidon conscript.',
        N'A young man walking a short path who stepped through a point the Liturgy was using. The gap between party and dorm is the entirety of the case in Sphere 31 and the entirety of his transit in the Cauld.',
        N'No POV.',
        N'Sphere 31 (Earth), Collegeville, Minnesota, United States',
        178, 75, N'athletic',
        N'brown', N'short', N'short',
        N'brown', N'light', N'clear',
        N'none', N'a soldier''s posture, newly acquired under front-line conditions',
        N'Myrmidon field kit',
        N'none',
        N'He was walking 200 meters between a party and his dorm. He is in a war.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Active front, Myrmidon unit',
        N'0', N'0',
        N'young man, twenty-one, Myrmidon kit, Cauld front line, fantasy-steampunk warfare, Buehlman dark register',
        N'young man, soldier, front line, dark fantasy',
        0, 0
    );
    PRINT 'Josh Guimond seeded.';
END
ELSE PRINT 'Josh Guimond already exists.';
GO

PRINT 'Sphere 31 missing persons seed complete.';
GO
