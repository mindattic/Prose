SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- HOUSE ATRAMENT — ELDER/FRINGE POPULATION BATCH B  (15 characters)
-- Universe: Cauld  (ID 0197E9C9-0002-7000-8000-000000000002)
-- 2026-07-05 · Idempotent (IF NOT EXISTS guards on all inserts)
-- Focus: Veteran handlers · Bheur-watchers · Failed Transmutation survivors
--         Oathless/former-Oathless · Sphere 31 absorbed persons · Elderly servants · Knight
-- Names: drawn from French/Occitan pool; first-name repeats avoided against existing seeds
-- ═══════════════════════════════════════════════════════════════════════════════

-- ── 1. Arnaud Desanges ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Arnaud Desanges')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Arnaud Desanges', N'arnaud-desanges', N'canon', 1,
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
        @id, N'Arnaud Desanges', N'arnaud-desanges', N'Arnaud', N'Desanges', N'',
        N'human', N'human', N'male', N'he/him',
        71, N'alive',
        N'Retired Senior Handler, House Atrament Intelligence Directorate',
        N'Barrel-chested man softened by age and wine. Moves like someone who expects corners to hide things. His silences are decisions.',
        N'Repository of buried operational secrets; will only reveal them under duress or for a price.',
        N'No POV.',
        N'House Atrament; eastern estate annex',
        175, 94, N'heavy-set',
        N'white', N'swept back', N'short',
        N'grey', N'olive', N'weathered',
        N'none',
        N'Deliberate, measured; expects corners to hide things.',
        N'Heavy wool robe, ink-stained cuffs; no ornament.',
        N'none',
        N'Reviews reports he no longer officially receives; meets three contacts weekly; drinks the estate''s best wine without apology.',
        N'He holds a roster of twelve Atrament assets sold to Lacerta in the Fallow Year and never disclosed it.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Atrament estate, eastern annex',
        N'0', N'0',
        N'Elderly French intelligence officer, heavy-set, white hair, grey eyes, dark medieval wine country estate',
        N'Elderly heavy-set man, grey eyes, dark medieval wool robes, stone estate',
        0, 0
    );
    PRINT N'Arnaud Desanges seeded.';
END
ELSE PRINT N'Arnaud Desanges already exists.';
GO

-- ── 2. Heloise Voy ───────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Heloise Voy')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Heloise Voy', N'heloise-voy', N'canon', 1,
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
        @id, N'Heloise Voy', N'heloise-voy', N'Heloise', N'Voy', N'',
        N'human', N'human', N'female', N'she/her',
        68, N'alive',
        N'Temple Attendant, Bheur Vigil; covert Atrament informant',
        N'Thin as candle-wax, hands perpetually folded. Watches people die with professional calm. Speaks slowly, as if words cost something.',
        N'Death-adjacent intelligence source; unsettles interlocutors by knowing too much about endings and keeping perfectly calm.',
        N'No POV.',
        N'House Atrament; Temple of Bheur, Vine Quarter capital',
        158, 49, N'lean',
        N'white', N'pinned up', N'medium',
        N'pale blue', N'fair', N'papery',
        N'none',
        N'Still, contained; rarely gestures; moves at temple pace.',
        N'Grey temple robes; plain leather belt; no emblem.',
        N'none',
        N'Tends vigil candles; records names of the newly dead; reports salient intelligence to her handler each market day.',
        N'Seventeen dying persons whispered the same three syllables at death; she records them privately and has told no one.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Temple of Bheur, Vine Quarter capital',
        N'0', N'0',
        N'Elderly thin woman, white pinned hair, pale blue eyes, grey temple robes, candlelit medieval vigil chamber',
        N'Elderly thin woman, white hair, grey temple robes, candlelit vigil chamber',
        0, 0
    );
    PRINT N'Heloise Voy seeded.';
END
ELSE PRINT N'Heloise Voy already exists.';
GO

-- ── 3. Cecile de Carriere ─────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Cecile de Carriere')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Cecile de Carriere', N'cecile-de-carriere', N'canon', 1,
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
        @id, N'Cecile de Carriere', N'cecile-de-carriere', N'Cecile', N'de Carriere', N'',
        N'human', N'human', N'female', N'she/her',
        44, N'alive',
        N'Failed-infusion archivist; classified Myrmidon-incomplete',
        N'One arm is subtly wrong — the left slightly longer, the grip stronger than it should be. She keeps it close to her body and says nothing about why.',
        N'Embodies the cost of failed Transmutation; holds infusion records with institutional memory and personal stakes.',
        N'No POV.',
        N'House Atrament; infirmary district',
        163, 58, N'slight',
        N'dark brown', N'loose', N'medium',
        N'hazel', N'light brown', N'pale, slightly waxy',
        N'none',
        N'Holds left arm close; deliberate, self-conscious movement.',
        N'Archivist''s grey tunic, wrapped belt, flat boots.',
        N'none',
        N'Maintains infusion records; escorts failed-candidate families through estate protocol; avoids the west wing where Catalysts are stored.',
        N'She can feel active Transmutation events as warmth in her partial-infused left arm; tracks occurrences and has never reported it.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Atrament infirmary district',
        N'0', N'0',
        N'Medieval French woman, dark hair, slight build, archivist grey tunic, one arm subtly wrong, dark fantasy portrait',
        N'Woman, dark hair, grey archivist tunic, slight build, medieval stone infirmary',
        0, 0
    );
    PRINT N'Cecile de Carriere seeded.';
END
ELSE PRINT N'Cecile de Carriere already exists.';
GO

-- ── 4. Gilles Marcouf ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Gilles Marcouf')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Gilles Marcouf', N'gilles-marcouf', N'canon', 1,
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
        @id, N'Gilles Marcouf', N'gilles-marcouf', N'Gilles', N'Marcouf', N'',
        N'human', N'human', N'male', N'he/him',
        55, N'alive',
        N'Oathless cutout; tolerated by Atrament as a controlled asset',
        N'Compact, sun-darkened man who wears patience like a second skin. Technically Oathless. Atrament keeps him alive because he is useful and executes him because he is known.',
        N'Living proof Atrament chooses pragmatism over law; carries Oathless network knowledge the House tolerates but cannot fully control.',
        N'No POV.',
        N'House Atrament; Thornway fringe, border territory',
        170, 72, N'wiry',
        N'grey-brown', N'rough-cut', N'short',
        N'brown', N'dark tan', N'weathered, leathery',
        N'none',
        N'Low-center, watchful; never sits with back unguarded.',
        N'Worn travel coat, no House mark; two visible knives.',
        N'none',
        N'Lives in the Thornway; runs occasional errands for Atrament; avoids House gatherings; sleeps outdoors when he can.',
        N'He executed a Sphere 31 captive who spoke his name; what the captive knew he still cannot identify.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Thornway fringe, border Atrament',
        N'0', N'0',
        N'Weathered compact man, grey-brown hair, worn travel coat, two knives, medieval border road, dark fantasy portrait',
        N'Weathered man, worn travel coat, two knives, medieval border road',
        0, 0
    );
    PRINT N'Gilles Marcouf seeded.';
END
ELSE PRINT N'Gilles Marcouf already exists.';
GO

-- ── 5. Mathilde Forell ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Mathilde Forell')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Mathilde Forell', N'mathilde-forell', N'canon', 1,
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
        @id, N'Mathilde Forell', N'mathilde-forell', N'Mathilde', N'Forell', N'',
        N'human', N'human', N'female', N'she/her',
        45, N'alive',
        N'Senior Sphere 31 Intelligence Analyst, Atrament intelligence archive',
        N'Thinks in two idiom-sets simultaneously and sometimes forgets which world a phrase belongs to. Her reports are more accurate than her handlers expect or want.',
        N'Sphere 31 bridge; her analysis cuts closer to the truth than her handlers want and she knows it.',
        N'No POV.',
        N'House Atrament; intelligence annex, estate northern wing (Sphere 31 origin)',
        165, 61, N'lean',
        N'dark brown', N'straight, pulled back', N'medium',
        N'dark brown', N'light tan', N'fine-lined',
        N'none',
        N'Slightly abstracted; focuses past the person addressed.',
        N'Analyst''s grey, neat; writing board always under arm.',
        N'none',
        N'Writes cross-world analysis; trains new analysts; runs one dead-drop contact she has not disclosed; works past midnight.',
        N'She runs a dead-drop with a Sphere 31 Liturgy escapee that she has never disclosed to Atrament command.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Intelligence annex, estate northern wing',
        N'0', N'0',
        N'Middle-aged woman, dark brown hair, analytical expression, grey robes, writing board, medieval stone estate annex',
        N'Middle-aged woman, dark hair, grey robes, writing board, medieval stone annex',
        0, 0
    );
    PRINT N'Mathilde Forell seeded.';
END
ELSE PRINT N'Mathilde Forell already exists.';
GO

-- ── 6. Luc Crespin ───────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Luc Crespin')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Luc Crespin', N'luc-crespin', N'canon', 1,
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
        @id, N'Luc Crespin', N'luc-crespin', N'Luc', N'Crespin', N'',
        N'human', N'human', N'male', N'he/him',
        78, N'alive',
        N'Chief Steward, Atrament main estate; three-lord institutional memory',
        N'Stooped man of extraordinary precision. Knows where every candle-holder was placed in 1407. Has outlasted everyone who ever doubted him.',
        N'Three-lord institutional memory; holds secrets he has never been asked to reveal because no one thought to ask.',
        N'No POV.',
        N'House Atrament; main estate, steward''s hall',
        168, 65, N'slight, stooped',
        N'white', N'thin, cropped', N'short',
        N'pale green', N'fair', N'age-spotted, papery',
        N'none',
        N'Stooped but deliberate; moves without sound.',
        N'Black steward''s coat, silver buttons; immaculate always.',
        N'none',
        N'Manages estate logistics; briefs guests on House customs; personally reviews room assignments for intelligence placement value.',
        N'He burned correspondence between Atrament''s current lord and a Lacerta envoy naming three asset identities.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Atrament main estate',
        N'0', N'0',
        N'Elderly stooped man, white hair, black steward coat, silver buttons, stone estate hall, medieval dark fantasy',
        N'Elderly stooped man, black steward coat, silver buttons, stone estate hall',
        0, 0
    );
    PRINT N'Luc Crespin seeded.';
END
ELSE PRINT N'Luc Crespin already exists.';
GO

-- ── 7. Marguerite Sechard ────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Marguerite Sechard')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Marguerite Sechard', N'marguerite-sechard', N'canon', 1,
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
        @id, N'Marguerite Sechard', N'marguerite-sechard', N'Marguerite', N'Sechard', N'',
        N'human', N'human', N'female', N'she/her',
        63, N'alive',
        N'Retired Cross-House Handler; unofficial shadow adviser',
        N'Small woman of iron posture. Retired means she stopped drawing pay. Her networks still report to her and she reads every message.',
        N'Autonomous intelligence operation nested within official structure; her retirement is an illusion and she maintains it carefully.',
        N'No POV.',
        N'House Atrament; hill-fort villages, southern Atrament',
        155, 56, N'compact',
        N'silver', N'pinned tight', N'short',
        N'blue-grey', N'fair', N'fine-lined, sharp',
        N'none',
        N'Iron-straight; sits only when she chooses to.',
        N'Dark green practical wool; no ornament; locked satchel.',
        N'none',
        N'Reads intercepted correspondence in retirement; meets assets in village markets; advises her successor without being asked or acknowledged.',
        N'She turned a House Saburra asset who still operates; the asset reports to her personally, not Atrament command.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Hill-fort villages, southern Atrament',
        N'0', N'0',
        N'Older woman, silver hair pinned tight, dark green wool, compact build, medieval village market, dark fantasy portrait',
        N'Older woman, silver hair, dark green wool, locked satchel, medieval village',
        0, 0
    );
    PRINT N'Marguerite Sechard seeded.';
END
ELSE PRINT N'Marguerite Sechard already exists.';
GO

-- ── 8. Etienne Maulein ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Etienne Maulein')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Etienne Maulein', N'etienne-maulein', N'canon', 1,
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
        @id, N'Etienne Maulein', N'etienne-maulein', N'Etienne', N'Maulein', N'',
        N'human', N'human', N'male', N'he/him',
        29, N'alive',
        N'Failed Myrmidon; estate clerk with aberrant perception',
        N'Young man with a stillness that reads as vacancy and is not. His infusion took something from him and left something else. He notices pain before the person feeling it does.',
        N'Embodied cost of botched Transmutation; perceptual asset Atrament deploys carefully and watches more carefully.',
        N'No POV.',
        N'House Atrament; Vine Quarter, western holding',
        178, 73, N'lean',
        N'dark', N'loose', N'medium',
        N'amber', N'light olive', N'pale, slightly grey',
        N'none',
        N'Still to the point of unease; blinks rarely.',
        N'Plain clerk''s tunic, dark blue; no emblem.',
        N'none',
        N'Transcribes intelligence summaries; occasionally deployed to observe persons under questioning; avoids the infusion wing on principle.',
        N'He knows Catalyst batch D-7 killed six cohort members; the supplier had Atrament ties and the records were altered.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Vine Quarter west, estate clerk''s hall',
        N'0', N'0',
        N'Young pale man, dark loose hair, amber eyes, dark blue tunic, unnerving stillness, medieval stone estate interior',
        N'Young pale man, amber eyes, dark blue tunic, medieval stone estate interior',
        0, 0
    );
    PRINT N'Etienne Maulein seeded.';
END
ELSE PRINT N'Etienne Maulein already exists.';
GO

-- ── 9. Yolande Nochet ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Yolande Nochet')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Yolande Nochet', N'yolande-nochet', N'canon', 1,
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
        @id, N'Yolande Nochet', N'yolande-nochet', N'Yolande', N'Nochet', N'',
        N'human', N'human', N'female', N'she/her',
        34, N'alive',
        N'Head domestic servant; Sphere 31 origin; covert observer',
        N'Unhurried, precise, unremarkable by design. She has learned that the most dangerous thing in any estate is a servant people forget is present.',
        N'Embedded observer; knows every unmonitored space in the estate; Sphere 31 perspective sharpens her surveillance instincts.',
        N'No POV.',
        N'House Atrament; domestic quarters, main estate (Sphere 31 origin)',
        164, 60, N'medium',
        N'dark brown', N'pulled back', N'medium',
        N'dark', N'tan', N'smooth',
        N'none',
        N'Soft-footed; effaces herself; invisible when she chooses.',
        N'Servant''s grey, clean; flat shoes, never heeled.',
        N'none',
        N'Runs domestic staff; maintains estate schedules; has walked every corridor twice at night to learn the unmonitored gaps.',
        N'She has memorized three unmonitored gaps in the intelligence corridor and tested all three without being caught.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Main estate domestic wing',
        N'0', N'0',
        N'Young woman, dark brown hair pulled back, servant grey dress, tan skin, soft-footed, medieval estate domestic quarters',
        N'Young woman, dark hair pulled back, servant grey, tan skin, medieval estate',
        0, 0
    );
    PRINT N'Yolande Nochet seeded.';
END
ELSE PRINT N'Yolande Nochet already exists.';
GO

-- ── 10. Bertrand Gales ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Bertrand Gales')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Bertrand Gales', N'bertrand-gales', N'canon', 1,
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
        @id, N'Bertrand Gales', N'bertrand-gales', N'Bertrand', N'Gales', N'',
        N'human', N'human', N'male', N'he/him',
        66, N'alive',
        N'Senior Courier-Handler; forty years of uninterrupted clean message runs',
        N'Broad-shouldered man gone soft at the edges. Moves through crowds as if they open for him, because they do — he learned to make himself expected.',
        N'Operational infrastructure; forty years of undetected communication routes and a private archive no one knows exists.',
        N'No POV.',
        N'House Atrament; river crossing district, central Atrament',
        179, 88, N'heavy',
        N'grey-brown', N'close-cropped', N'short',
        N'brown', N'medium brown', N'weathered',
        N'none',
        N'Relaxed; projects belonging; takes up space naturally.',
        N'Merchant''s wool, no House color; broad belt, travel boots.',
        N'none',
        N'Manages courier networks; vets all new runners personally; takes two routes himself monthly to keep his instincts calibrated.',
        N'He copied every significant message he ever carried; the archive is sealed in a cistern under a false-name wayhouse.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'River crossing district, central Atrament',
        N'0', N'0',
        N'Broad-shouldered older man, grey-brown hair, merchant wool, travel boots, medieval river crossing, dark fantasy portrait',
        N'Broad older man, grey hair, merchant wool, travel boots, medieval river',
        0, 0
    );
    PRINT N'Bertrand Gales seeded.';
END
ELSE PRINT N'Bertrand Gales already exists.';
GO

-- ── 11. Clemence Arbel ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Clemence Arbel')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Clemence Arbel', N'clemence-arbel', N'canon', 1,
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
        @id, N'Clemence Arbel', N'clemence-arbel', N'Clemence', N'Arbel', N'',
        N'human', N'human', N'female', N'she/her',
        51, N'alive',
        N'Temple Attendant, Bheur upper precinct; intelligence asset',
        N'A woman whose devotion is genuine and whose surveillance is also genuine. She has never seen these as contradictions and does not plan to start.',
        N'Information channel through temple networks; genuine piety and professional surveillance coexist without contradiction.',
        N'No POV.',
        N'House Atrament; Temple of Bheur, upper precinct',
        161, 62, N'medium',
        N'auburn-grey', N'braided', N'long',
        N'green', N'fair', N'freckled',
        N'none',
        N'Serene, deliberate; moves at temple pace always.',
        N'Temple grey with copper thread border; vigil candle nearby.',
        N'none',
        N'Attends death rites; records last words for temple archives; passes intelligence to her handler in the market.',
        N'She believes her predecessor''s apparition named his poisoner in the crypt; she cannot prove it and has not reported it.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Temple of Bheur, upper precinct, Vine Quarter',
        N'0', N'0',
        N'Middle-aged woman, auburn-grey braid, freckled, copper-trimmed temple robes, candlelit medieval vigil, dark fantasy',
        N'Middle-aged woman, auburn-grey braid, freckled, temple robes, candlelit medieval vigil',
        0, 0
    );
    PRINT N'Clemence Arbel seeded.';
END
ELSE PRINT N'Clemence Arbel already exists.';
GO

-- ── 12. Guilhem Malemort ─────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Guilhem Malemort')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Guilhem Malemort', N'guilhem-malemort', N'canon', 1,
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
        @id, N'Guilhem Malemort', N'guilhem-malemort', N'Guilhem', N'Malemort', N'',
        N'human', N'human', N'male', N'he/him',
        48, N'alive',
        N'Knight, House Atrament; field operative, intelligence enforcement',
        N'A man who grew four inches with Transmutation and never stopped being surprised by it. Still reaches for doorframes he no longer needs to duck.',
        N'Institutional muscle for intelligence operations; embodies the physical cost of Transmutation in a diplomatic house.',
        N'No POV.',
        N'House Atrament; border field posts, active duty',
        196, 107, N'powerful',
        N'black', N'cropped', N'short',
        N'dark brown', N'dark brown', N'clear, slightly denser',
        N'Subtle height gain (Knight)',
        N'Occupies space differently than intended; unconscious physicality.',
        N'Knight''s traveling coat, dark wool; sword at belt.',
        N'Single Catalyst infusion; Knight-grade Transmutation: height gain 10cm, increased bone density, accelerated wound closure. Infusion point: left shoulder.',
        N'Escorts intelligence meetings; extracts Oathless when ordered; trains at dawn before the estate wakes; reports to his handler.',
        N'He killed a Sphere 31 laborer instead of his extraction target and filed no report; the error has no record.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Border field posts, Atrament territory',
        N'0', N'0',
        N'Tall powerful man, black cropped hair, dark brown skin, Knight traveling coat, sword, medieval border fortress, dark fantasy',
        N'Tall powerful man, dark skin, black hair, Knight coat, sword, medieval border',
        0, 0
    );
    PRINT N'Guilhem Malemort seeded.';
END
ELSE PRINT N'Guilhem Malemort already exists.';
GO

-- ── 13. Alienor Braque ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Alienor Braque')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Alienor Braque', N'alienor-braque', N'canon', 1,
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
        @id, N'Alienor Braque', N'alienor-braque', N'Alienor', N'Braque', N'',
        N'human', N'human', N'female', N'she/her',
        42, N'alive',
        N'Conditional asset; former Oathless absorbed rather than executed',
        N'A woman who understands precisely what keeping her alive cost Atrament, and repays it in full, and wonders when the ledger will close against her.',
        N'Demonstrates Atrament''s pragmatism; carries Oathless knowledge the House exploits while she carries a debt she cannot repay.',
        N'No POV.',
        N'House Atrament; Weld Vines, outer territory (formerly Oathless)',
        166, 64, N'athletic',
        N'dark red', N'loose', N'short',
        N'hazel', N'medium', N'weathered, scarred left jaw',
        N'none',
        N'Alert, low-center; never fully at ease indoors.',
        N'Practical dark wool, no insignia; blade at small of back.',
        N'none',
        N'Runs intelligence errands the House cannot officially run; maps Oathless movement in the Weld Vines; sleeps alert.',
        N'She knows a forty-person Oathless encampment in the Weld Vines and has not reported it; they sheltered her.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Weld Vines, outer Atrament territory',
        N'0', N'0',
        N'Athletic woman, dark red short hair, scarred jaw, practical dark wool, medieval vine country, dark fantasy portrait',
        N'Athletic woman, dark red short hair, scarred jaw, dark wool, medieval vines',
        0, 0
    );
    PRINT N'Alienor Braque seeded.';
END
ELSE PRINT N'Alienor Braque already exists.';
GO

-- ── 14. Aimeric Peyrat ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Aimeric Peyrat')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Aimeric Peyrat', N'aimeric-peyrat', N'canon', 1,
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
        @id, N'Aimeric Peyrat', N'aimeric-peyrat', N'Aimeric', N'Peyrat', N'',
        N'human', N'human', N'male', N'he/him',
        70, N'alive',
        N'Estate document specialist; master forger; classified position',
        N'Hands still nimble at seventy. Makes papers that have passed scrutiny in six Houses. The estate cannot function without the records he maintains and occasionally invents.',
        N'Operational backbone for covert identity work; every fabricated history in the House passes through his hands.',
        N'No POV.',
        N'House Atrament; document hall, estate sub-level',
        171, 68, N'medium, hunched at work',
        N'white', N'sparse, cropped', N'short',
        N'grey', N'fair', N'age-spotted',
        N'none',
        N'Hunched at desk; precise and deliberate in motion.',
        N'Dark work apron over plain wool; ink-stained permanently.',
        N'none',
        N'Produces documents on request; maintains false-identity archives; tests each ink batch for fade; works by lamplight only.',
        N'He forged a death record for a living Knight thirty years ago; the Knight now works for a rival House.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Document hall, estate sub-level',
        N'0', N'0',
        N'Elderly man, white sparse hair, dark work apron, ink-stained hands, lamplight document hall, medieval dark fantasy',
        N'Elderly man, white hair, ink-stained dark apron, lamplight document hall',
        0, 0
    );
    PRINT N'Aimeric Peyrat seeded.';
END
ELSE PRINT N'Aimeric Peyrat already exists.';
GO

-- ── 15. Adelais Maurs ────────────────────────────────────────────────────────
-- NOTE: Lord Renaud Colbert''s PsychologySecret references sending "Adelais" to die.
-- This character is that operative — returned alive, unrecognized, operating under a false identity.
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Adelais Maurs')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Adelais Maurs', N'adelais-maurs', N'canon', 1,
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
        @id, N'Adelais Maurs', N'adelais-maurs', N'Adelais', N'Maurs', N'',
        N'human', N'human', N'female', N'she/her',
        36, N'alive',
        N'Field operative, classified deceased; returned to Atrament estate under false identity',
        N'She moves through the estate under a false name. Three years since Lord Renaud''s mission should have killed her. She is deciding whether she wants justice or safety.',
        N'The mission the Lord thought would kill her; a reckoning he does not know is approaching from within his own estate.',
        N'No POV.',
        N'House Atrament; estate periphery (operating as southern factor''s assistant)',
        163, 57, N'lean, athletic',
        N'dark, recently cut short', N'close-cropped', N'short',
        N'grey-green', N'light', N'weathered, scar at left temple',
        N'none',
        N'Economical, still; moves as though exits are always being counted.',
        N'Working wool, factor''s colors; no House mark; hidden blade.',
        N'none',
        N'Moves through the estate as a factor''s assistant; maps Lord Renaud''s daily movements; has not decided whether to reveal herself.',
        N'Lord Renaud sent her to die on the Lacerta crossing; she survived and returned unrecognized as a factor''s assistant.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Estate periphery and market access roads',
        N'0', N'0',
        N'Young woman, short dark hair, scar at temple, working wool, medieval estate periphery, dark fantasy portrait',
        N'Young woman, short dark hair, scarred temple, working wool, medieval estate',
        0, 0
    );
    PRINT N'Adelais Maurs seeded.';
END
ELSE PRINT N'Adelais Maurs already exists.';
GO
