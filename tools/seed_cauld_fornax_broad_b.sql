SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- HOUSE FORNAX — ELDER/FRINGE POPULATION BATCH B  (15 characters)
-- Universe: Cauld  (ID 0197E9C9-0002-7000-8000-000000000002)
-- 2026-07-05 · Idempotent (IF NOT EXISTS guards on all inserts)
-- Rhine-Danube; Germany analog; industrial and methodical.
-- Categories: Veterans (3) · Oathless (2) · Elderly civilians (3)
--   Failed Transmutation survivors (2) · Sphere 31 integrated (2)
--   Fallen House descendants (1) · Bheur-watchers (2)
-- ═══════════════════════════════════════════════════════════════════════════════

-- ── 1. Albrecht Voss ─────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Albrecht Voss')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Albrecht Voss', N'albrecht-voss', N'canon', 1,
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
        @id, N'Albrecht Voss', N'albrecht-voss', N'Albrecht', N'Voss', N'',
        N'human', N'human', N'male', N'he/him',
        67, N'alive',
        N'Retired Knight; weapons certification officer, Mainz Forge District.',
        N'Albrecht Voss spent twenty-six years in Fornax''s campaign garrison and came out the other side with a Knight''s frame, a certification badge, and the practiced manner of someone who has learned to sign his name to things without reading them twice. He runs the Mainz blade-inspection bureau now. He is thorough in the ways that do not threaten anyone, and comfortable in the ways that do not require him to remember.',
        N'The compromised certifier — institutional accountability wearing the skin of diligence.',
        N'No POV.',
        N'House Fornax; Mainz Forge District',
        187, 104, N'broad-shouldered, thickening with age',
        N'grey', N'close-cropped', N'short',
        N'pale blue', N'weathered pale', N'lined, broken capillaries across the nose',
        N'Subtle height gain (Knight)',
        N'military upright; favors the left knee when he thinks no one is watching; clasps hands behind the back at rest',
        N'old campaign surcoat repurposed as inspector''s coat; Fornax forge-certification seal on the breast',
        N'Knight-grade Transmutation — 6cm height increase; enhanced structural healing in middle age',
        N'Inspects blade batches by weight, flex, and edge-hold; signs certification ledgers; eats his noon meal at the same garrison hall table every day without variation.',
        N'He certified a batch of stress-fractured blades during the supply drought of three seasons past. Fourteen soldiers deployed with them. He has the garrison death records for that quarter filed on the bureau''s lower shelf. His commendation from the same period hangs above his desk.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Mainz Forge District, weapons certification bureau',
        N'0', N'0',
        N'broad-shouldered Germanic elder soldier in weathered forge-inspector coat, pale blue eyes, Mainz stone hall, medieval dark fantasy, Buehlman tone --ar 2:3',
        N'A 67-year-old Germanic man in an old military coat examining a sword in a stone forge hall',
        0, 0
    );
    PRINT N'Albrecht Voss seeded.';
END
ELSE PRINT N'Albrecht Voss already exists.';
GO

-- ── 2. Gerda Nachtigal ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Gerda Nachtigal')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Gerda Nachtigal', N'gerda-nachtigal', N'canon', 1,
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
        @id, N'Gerda Nachtigal', N'gerda-nachtigal', N'Gerda', N'Nachtigal', N'',
        N'human', N'human', N'female', N'she/her',
        73, N'alive',
        N'Retired chief trade archivist; keeper of fifty years of Fornax route agreements.',
        N'Gerda Nachtigal was the most dangerous person in the Frankfurt ledger hall for thirty years, and everyone there knew it and no one said it aloud. She remembered every clause of every route agreement she had ever filed. She still does. She is retired in name only: the junior archivists bring her disputes and she settles them with her eyes closed, sometimes literally.',
        N'The archive that walks — fifty years of Fornax trade law embodied in one elderly woman.',
        N'No POV.',
        N'House Fornax; Frankfurt ledger hall',
        158, 61, N'slight, stooped',
        N'white', N'pinned flat', N'short',
        N'grey', N'pale', N'heavily lined, liver-spotted',
        N'none',
        N'head carries forward of the shoulders; shuffles with precise purpose; rarely makes unnecessary gestures',
        N'black wool, ink-stained cuffs, silver ledger-clasp at the collar',
        N'none',
        N'Cross-references old route ledgers no junior clerk can decode; eats sparingly; takes one walk each afternoon around the archive courtyard; sleeps poorly and burns the notes she writes before morning.',
        N'Forty years ago she altered a secondary route ledger to hide her brother''s grain-smuggling operation, rerouting the record onto a legitimate Fornax channel. That channel now appears as the approved transit corridor in the Fornax-Liturgy passage treaty she helped draft. The Liturgy uses it quarterly to move Sphere 31 takings.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Frankfurt archive hall and adjoining ledger vault',
        N'0', N'0',
        N'elderly Germanic woman in black wool at a stone archive desk, white hair pinned flat, ink-stained hands, candlelit ledger hall, Buehlman dark medieval --ar 2:3',
        N'A 73-year-old woman in black wool at a candlelit archive desk, grey eyes, white hair, heavily lined face',
        0, 0
    );
    PRINT N'Gerda Nachtigal seeded.';
END
ELSE PRINT N'Gerda Nachtigal already exists.';
GO

-- ── 3. Hilde Brauer ──────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Hilde Brauer')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Hilde Brauer', N'hilde-brauer', N'canon', 1,
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
        @id, N'Hilde Brauer', N'hilde-brauer', N'Hilde', N'Brauer', N'',
        N'human', N'human', N'female', N'she/her',
        48, N'alive',
        N'Failed Transmutation survivor; recorder at the Cologne infusion trials.',
        N'The Xerum burned out the left side of Hilde Brauer''s body and left the right untouched, which the Transmutation overseers considered a useful data point and documented accordingly. She records infusion trials now — what the candidates look like going in, what they look like coming out, and the moments in between. She holds very still while she watches.',
        N'The cost made visible — what Transmutation''s failure rate looks like when it survives and keeps showing up to work.',
        N'No POV.',
        N'House Fornax; Cologne Transmutation Ward',
        162, 54, N'spare, left side atrophied',
        N'brown', N'pulled back', N'medium',
        N'brown (left eye clouded and fixed)', N'pale', N'facial burn patterning on left side; intact right side',
        N'Failed Transmutation scarring — left-side facial burn patterning, partial left arm paralysis; no ascending effect achieved',
        N'holds left arm against body; leads with the right shoulder; moves with economy born of chronic asymmetry',
        N'heavy right-hand work glove, plain recorder''s tunic, nothing that catches on equipment',
        N'Failed infusion — Xerum 525 initiated but collapsed mid-process; asymmetric tissue damage with no ascending effect',
        N'Documents each infusion trial from the observation alcove; logs time, concentration, subject response, and outcome category; submits reports to the Transmutation master; does not attend the disposal of subjects who do not survive.',
        N'She volunteered for Transmutation not from ambition but to void a betrothal contract her House overseer had arranged without her consent. The consent form she signed for the infusion was drafted by the same overseer. She signed it the day after he explained what would happen to her brother''s forge apprenticeship if she refused. The overseer retired eight months later. He is alive and comfortable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Cologne Transmutation Ward, observation alcove',
        N'0', N'0',
        N'woman with severe facial burn scarring on left side, brown hair pulled back, holding a recording ledger, stone infusion chamber, Buehlman dark medieval --ar 2:3',
        N'A 48-year-old woman with left-side burn scarring, brown hair, recording ledger in hand, stone chamber, asymmetric posture',
        0, 0
    );
    PRINT N'Hilde Brauer seeded.';
END
ELSE PRINT N'Hilde Brauer already exists.';
GO

-- ── 4. Klaus Odenwald ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Klaus Odenwald')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Klaus Odenwald', N'klaus-odenwald', N'canon', 1,
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
        @id, N'Klaus Odenwald', N'klaus-odenwald', N'Klaus', N'Odenwald', N'',
        N'human', N'human', N'male', N'he/him',
        59, N'alive',
        N'Oathless arms broker; formerly Fornax trade master, Hamburg post.',
        N'Klaus Odenwald was expelled from House Fornax eleven years ago for selling a Fornax-stamped blade consignment to a Lacerta purchasing agent — a transaction he maintains was ambiguous in the relevant contract language. He has been Oathless since, which means any House garrison executes him on sight, which means he has become extraordinarily careful about lines of sight.',
        N'The exile who knows where every weapon came from — the House''s institutional memory walking in hostile territory.',
        N'No POV.',
        N'Oathless; formerly House Fornax, Hamburg trade post',
        180, 88, N'stocky, merchant-solid',
        N'brown-grey', N'unkempt', N'medium',
        N'brown', N'pale-tan', N'weathered, stubbled, road-worn',
        N'none',
        N'reads every exit in a room within seconds of entering; moves relaxed until he doesn''t; never sits with his back to an open door',
        N'neutral border-wool, no House markings, traveling cloak that keeps both hands concealed',
        N'none',
        N'Moves between Rhine border markets under three working names; brokers weapons consignments to buyers who pay in advance and expect no receipts; sleeps in different locations; drinks alone and not excessively.',
        N'His son Pieter, twenty-three years old, serves in the Fornax garrison at Dusseldorf. Pieter believes his father died of lung plague eleven years ago. Klaus has seen him twice at the garrison gate from a distance without approaching. He carries a miniature portrait of Pieter as an infant that he has shown to no one.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Rhine border markets, traveling; no fixed address',
        N'0', N'0',
        N'stocky middle-aged man in plain wool traveling cloak, no House markings, Rhine border town, watchful eyes, Buehlman dark medieval --ar 2:3',
        N'A 59-year-old stocky man in a neutral wool cloak at a border market, brown eyes, weathered face, wary posture',
        0, 0
    );
    PRINT N'Klaus Odenwald seeded.';
END
ELSE PRINT N'Klaus Odenwald already exists.';
GO

-- ── 5. Marta Feuerbach ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Marta Feuerbach')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Marta Feuerbach', N'marta-feuerbach', N'canon', 1,
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
        @id, N'Marta Feuerbach', N'marta-feuerbach', N'Marta', N'Feuerbach', N'',
        N'human', N'human', N'female', N'she/her',
        52, N'alive',
        N'Returned Sphere 31 taking; technical document translator, Koblenz forge-yard.',
        N'Marta Feuerbach was taken by the Liturgy at thirty-one and returned at forty-two. She speaks Sphere 31 dialect with an accent that has never gone away and sometimes gestures in directions that do not correspond to what she''s saying. Fornax finds her useful for decoding Scrying Installation transcripts. She finds Fornax useful for something she has not disclosed.',
        N'The returned taking — what comes back is not quite what left, and it brought something with it that Fornax has not yet inventoried.',
        N'No POV.',
        N'House Fornax; returned Sphere 31 taking, Koblenz forge-yard',
        165, 60, N'average, slightly dislocated in movement',
        N'dark brown', N'unevenly cut', N'medium',
        N'hazel (gaze tends to fix on middle distance)', N'pale with discolored patches', N'windburned, irregular pigmentation consistent with eleven years of Sphere 31 exposure',
        N'none (Sphere 31 physiological variance — not Transmutation)',
        N'pauses mid-sentence; gestures to her left when describing things to her right; occasionally addresses a point slightly above the person she is speaking to',
        N'Fornax work-tunic over Sphere 31 woven undergarments she has refused to surrender',
        N'none',
        N'Translates Sphere 31 technical manuscripts for the Koblenz Scrying bureau; lapses into Sphere 31 dialect mid-sentence and finishes the thought in Fornax-standard without breaking pace; eats at irregular hours; has no close acquaintances.',
        N'Six months after her return, she sold a schematic for a Sphere 31 high-compression alloy forging process to weapons-master Dietrich Baur. Baur filed the process under his own name, received a Fornax innovation commendation, and paid her with a loaf of bread and two silver marks. She is calculating who owes whom and at what rate.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Koblenz forge-yard translation office',
        N'0', N'0',
        N'woman with pale skin and irregular pigmentation, dark brown unevenly cut hair, Sphere 31 garments under Fornax work-tunic, stone translation office, dislocated gaze, Buehlman dark medieval --ar 2:3',
        N'A 52-year-old woman with irregular skin pigmentation and unfocused gaze, translating documents in a stone office',
        0, 0
    );
    PRINT N'Marta Feuerbach seeded.';
END
ELSE PRINT N'Marta Feuerbach already exists.';
GO

-- ── 6. Oskar Grunwald ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Oskar Grunwald')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Oskar Grunwald', N'oskar-grunwald', N'canon', 1,
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
        @id, N'Oskar Grunwald', N'oskar-grunwald', N'Oskar', N'Grunwald', N'',
        N'human', N'human', N'male', N'he/him',
        69, N'alive',
        N'Mortuary elder; Bheur-watcher; unofficial witness-keeper of the Heidelberg death district.',
        N'Oskar Grunwald has attended two hundred and fourteen deaths in the Heidelberg mortuary district since he began keeping count. He attends as a witness, not a mourner. He asks survivors what the dying person last looked at, and he writes the answer in a ledger no one has ever asked to read. He believes this is important work, and he is correct, though not for the reason he believes.',
        N'The watcher who has made observation into devotion — and built something stranger than religion from the raw material of witness.',
        N'No POV.',
        N'House Fornax; Heidelberg mortuary district',
        174, 72, N'lean, deliberate',
        N'white', N'thinning', N'short',
        N'pale grey', N'pale', N'waxy, deep-set eyes, unhurried face',
        N'none',
        N'never rushes; holds himself at neck height when entering a room; watches exits and occupied chairs with equal attention',
        N'grey mortuary district coat, always clean hands regardless of what he has been doing',
        N'none',
        N'Attends every death in the Heidelberg district when permitted; questions survivors; transcribes in his witness ledger; walks the mortuary corridors once each evening; does not pray but moves his lips sometimes when alone with a body.',
        N'He has witnessed fourteen deaths where intervention was physically possible — six of those where he could have acted and chose not to. He counts these six separately from the two hundred and fourteen. He believes these six deaths carry more weight toward Bheur than any of the others. He has no theology that supports this and has never sought one.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Heidelberg mortuary district',
        N'0', N'0',
        N'lean elderly Germanic man in grey coat, pale grey eyes, standing at a stone mortuary bed, candlelight, waxy patient face, Buehlman dark medieval --ar 2:3',
        N'A 69-year-old lean man in a grey coat, pale grey eyes, standing witness beside a stone mortuary bed',
        0, 0
    );
    PRINT N'Oskar Grunwald seeded.';
END
ELSE PRINT N'Oskar Grunwald already exists.';
GO

-- ── 7. Walburga Stahl ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Walburga Stahl')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Walburga Stahl', N'walburga-stahl', N'canon', 1,
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
        @id, N'Walburga Stahl', N'walburga-stahl', N'Walburga', N'Stahl', N'',
        N'human', N'human', N'female', N'she/her',
        66, N'alive',
        N'Master armorer, Nuremberg plate workshop; absorbed from fallen House Kalmar.',
        N'Walburga Stahl was eight years old when House Kalmar fell and twelve when she hammered her first plate at the Fornax workshop she was placed in. She has been hammering for fifty-four years. She runs the Nuremberg plate workshop now and makes better armor than anyone currently serving under Fornax colors — which is not something she mentions.',
        N'The absorbed enemy — what a fallen House looks like after three generations of silence and work, still intact inside.',
        N'No POV.',
        N'House Fornax (absorbed from fallen House Kalmar at age 8); Nuremberg armorer''s quarter',
        163, 74, N'compact, forge-muscled',
        N'iron grey', N'braided', N'long',
        N'brown', N'ruddy', N'smoke-darkened, burn-scarred hands',
        N'none',
        N'plants feet wide; moves as if she expects the floor to push back; never turns her back to an open door',
        N'heavy leather forge apron over Fornax-stamped wool; Kalmar-style underweave banding at the wrists that no one in Fornax now recognizes as foreign',
        N'none',
        N'Opens the workshop at first light; inspects overnight work; runs three apprentices through pacing drills before the furnaces are hot; hammers herself for two hours each day regardless of other demands; argues with Fornax requisition officers and wins.',
        N'She has cast a replica of the Kalmar house seal and embedded it in the cornerstone of her workshop''s foundation wall, accessible only through a floor stone she installed herself. She has taught the old Kalmar metallurgical prayers to her apprentices as guild counting chants — the prayers specify hammer cadence. Three apprentices who completed training do not know what they know.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Nuremberg armorer''s quarter, plate workshop',
        N'0', N'0',
        N'compact older Germanic woman in leather forge apron, iron grey braided hair, stone plate workshop, Nuremberg, ruddy face, burn-scarred hands, Buehlman dark medieval --ar 2:3',
        N'A 66-year-old compact woman in a forge apron with iron grey braided hair, burn-scarred hands, stone workshop',
        0, 0
    );
    PRINT N'Walburga Stahl seeded.';
END
ELSE PRINT N'Walburga Stahl already exists.';
GO

-- ── 8. Reinhard Sommer ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Reinhard Sommer')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Reinhard Sommer', N'reinhard-sommer', N'canon', 1,
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
        @id, N'Reinhard Sommer', N'reinhard-sommer', N'Reinhard', N'Sommer', N'',
        N'human', N'human', N'male', N'he/him',
        63, N'alive',
        N'Garrison logistics master; 25-year veteran, now lame; Stuttgart supply post.',
        N'Reinhard Sommer broke his right leg at the Westmark siege and it healed wrong, which ended his campaign service and started his administrative career. He has managed Stuttgart''s garrison supply chain for nine years. He is meticulous, cheerful in a broad way that does not invite questions, and in possession of a memory for numbers that has never served him as well as it serves his ledger.',
        N'The logistics man whose good numbers hide a bad quarter — institutional guilt wearing the face of competence.',
        N'No POV.',
        N'House Fornax; Stuttgart garrison, supply post',
        179, 95, N'heavy, lame right leg',
        N'grey', N'close-cropped', N'short',
        N'green', N'pale', N'ruddy, broken-veined from cold campaigns',
        N'none',
        N'stands square; drags the right foot; holds his upper body commanding to compensate; clasps hands behind his back when uncomfortable',
        N'old campaign coat repurposed as administrative wool; no insignia; the leather is still campaign-grade and has not been replaced',
        N'none',
        N'Reviews daily supply counts; meets with requisition officers twice weekly; argues with the garrison armorer over blade-to-shield ratios; takes his evening meal with junior clerks and talks more than is necessary; files quarterly reports that are impeccable.',
        N'During the Westmark siege he falsified the winter-gear count: sixty sets of cold-weather kit were logged as issued that had not arrived. Three soldiers from the eastern picket died of exposure that quarter. Reinhard received a commendation for his supply management in the same period. The commendation is framed in his office and he has never taken it down.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Stuttgart garrison, supply administration post',
        N'0', N'0',
        N'heavy older Germanic soldier with a lame leg in repurposed campaign coat, green eyes, stone garrison office, Buehlman dark medieval --ar 2:3',
        N'A 63-year-old heavy man with a lame right leg in an old military coat, green eyes, standing in a garrison supply room',
        0, 0
    );
    PRINT N'Reinhard Sommer seeded.';
END
ELSE PRINT N'Reinhard Sommer already exists.';
GO

-- ── 9. Liesel Hartmann ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Liesel Hartmann')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Liesel Hartmann', N'liesel-hartmann', N'canon', 1,
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
        @id, N'Liesel Hartmann', N'liesel-hartmann', N'Liesel', N'Hartmann', N'',
        N'human', N'human', N'female', N'she/her',
        78, N'alive',
        N'Senior archivist; controls access to the Frankfurt vault of oldest trade agreements; active at 78.',
        N'Liesel Hartmann has not named a successor in twelve years of requests, which means that when she dies the Frankfurt archive''s oldest ledger vault will be accessible to exactly no one who can read it. She is aware of this. She considers it a reasonable arrangement. She did not get to be seventy-eight by making herself easy to replace.',
        N'The archive as hostage — the last person who can read the oldest agreements, and she has known it for years.',
        N'No POV.',
        N'House Fornax; Frankfurt archive tower',
        156, 55, N'frail, precise',
        N'white', N'neatly pinned', N'short',
        N'pale blue', N'pale', N'papery, deeply lined',
        N'none',
        N'moves very slowly and with total deliberation; places each foot; keeps her hands folded except when writing',
        N'archivist''s black, ink-stained fingers, small silver reading-glass on a chain at her collar',
        N'none',
        N'Deciphers old trade agreements no junior clerk holds the cipher key for; does not eat at her desk; walks the archive perimeter at midday; refuses all visitors before the second bell; controls access to the oldest ledger vault with a physical key she sleeps with.',
        N'She has memorized every clause of the secret Fornax-Liturgy transit passage treaty — a document she was permitted to read once and never permitted to copy. Eighteen months ago a man who gave no name came to the archive and asked her to recite one clause from memory. She did, accepted his payment, and he has not returned. She does not know what she sold or to whom.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Frankfurt archive tower, lower ledger vault',
        N'0', N'0',
        N'frail elderly woman in archivist''s black at a candlelit stone desk, white hair pinned, pale blue eyes, reading-glass on a chain, Frankfurt archive tower, Buehlman dark medieval --ar 2:3',
        N'A 78-year-old frail woman in black at a stone archive desk, white hair, pale blue eyes, silver reading-glass',
        0, 0
    );
    PRINT N'Liesel Hartmann seeded.';
END
ELSE PRINT N'Liesel Hartmann already exists.';
GO

-- ── 10. Hartmut Krantz ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Hartmut Krantz')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Hartmut Krantz', N'hartmut-krantz', N'canon', 1,
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
        @id, N'Hartmut Krantz', N'hartmut-krantz', N'Hartmut', N'Krantz', N'',
        N'human', N'human', N'male', N'he/him',
        51, N'alive',
        N'Furnace maintenance worker; partial Transmutation subject; Duisburg facility, night shift.',
        N'Hartmut Krantz went into the Duisburg infusion chamber at thirty-one and the process was stopped before it finished, which was supposed to kill him and did not. His right arm and shoulder ascended partway — not Knight, not Paladin, something the Transmutation records call ''indeterminate.'' The rest of him did not change. He works nights in furnace maintenance and keeps his right sleeve long.',
        N'The experiment that lived — partial Transmutation as ongoing medical study disguised as a work permit.',
        N'No POV.',
        N'House Fornax; Duisburg Transmutation facility',
        183, 96, N'asymmetric — right arm and shoulder enlarged and dense, rest of frame unremarkable',
        N'black-grey', N'short', N'short',
        N'dark brown (right eye slightly enlarged, tracking slower)', N'pale', N'Transmutation scarring along the boundary line across right shoulder and upper chest',
        N'Evident enhancement — right arm and shoulder only (failed mid-process Transmutation; partial ascension, no full outcome)',
        N'keeps right arm low and still in company; uses it only when furnace work requires; never raises it above shoulder height in view of others',
        N'long-sleeved forge overalls, right sleeve reinforced with additional material; avoids the Transmutation wing by habit',
        N'Partial Transmutation — infusion halted mid-process; right arm and shoulder achieved partial Knight-grade density; causes ongoing deterioration of the right shoulder joint',
        N'Maintains primary furnace arrays during night shifts; performs heavy maintenance work that would require two workers using his right arm alone; eats before the rest of the crew arrives and leaves before they do; communicates by written note more often than speech.',
        N'Transmutation master Konrad Ellner halted the infusion mid-process not because it was failing but to observe a partial outcome in a live subject. Hartmut was conscious for nineteen of the twenty minutes of the halting procedure and heard Ellner''s instructions to the attendant. He has not reported this because Ellner controls the work papers that allow him to remain in the facility rather than being transferred to a research annex as a permanent study subject.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Duisburg furnace district, maintenance section (night shift)',
        N'0', N'0',
        N'asymmetric man with one enlarged enhanced arm in long-sleeved forge overalls, dark eyes, Duisburg stone furnace hall, night shift, Buehlman dark medieval --ar 2:3',
        N'A 51-year-old man with an asymmetrically enlarged right arm in forge overalls, dark eyes, stone furnace hall',
        0, 0
    );
    PRINT N'Hartmut Krantz seeded.';
END
ELSE PRINT N'Hartmut Krantz already exists.';
GO

-- ── 11. Brunhild Metzger ─────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Brunhild Metzger')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Brunhild Metzger', N'brunhild-metzger', N'canon', 1,
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
        @id, N'Brunhild Metzger', N'brunhild-metzger', N'Brunhild', N'Metzger', N'',
        N'human', N'human', N'female', N'she/her',
        47, N'alive',
        N'Active Knight; garrison commander, Dortmund siege post; 22 years of service.',
        N'Brunhild Metzger went through Transmutation at twenty-five, made Knight inside four years, and spent the next eighteen running siege rotations on Fornax''s eastern line. She is missing three fingers from her left hand, carries a healed blade-cut across her left jaw, and commands the Dortmund garrison with the calm efficiency of someone who stopped being impressed by emergencies years ago.',
        N'The soldier''s soldier — competence that has crossed into something that looks like moral exhaustion and functions like it too.',
        N'No POV.',
        N'House Fornax; Dortmund siege garrison',
        188, 92, N'lean-powerful, left hand missing index, middle, and ring fingers',
        N'dark blonde', N'short', N'short',
        N'blue-grey', N'pale', N'weathered, healed blade scar across the left jaw',
        N'Subtle height gain (Knight)',
        N'soldier''s stillness; does not waste movement; favors the right side; holds the maimed hand slightly behind the body by habit',
        N'campaign plate over weathered Fornax surcoat; no decorative elements; the surcoat has been mended four times and she has mended it herself',
        N'Knight-grade Transmutation — 8cm height increase; enhanced healing rate (healed finger wounds in three weeks); enhanced structural load tolerance',
        N'Commands Dortmund garrison rotation; runs pre-dawn foot drills; meets with the supply master twice weekly; reads campaign reports before evening meal; sleeps in the garrison''s duty room rather than the officer''s quarters and has never explained why.',
        N'Eighteen months ago she let a Liturgy field officer escape a border skirmish alive. He presented her, seconds before she would have killed him, with a written manifest of Sphere 31 taking assignments for the next quarter — including the name of her nephew Pieter Metzger, a student at the Cologne letter-school. In exchange for the manifest and the name of the Fornax garrison adjutant who had authorized Pieter''s taking, she let the officer walk into the tree line. The adjutant is still serving. Pieter does not know he was marked.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Dortmund siege garrison, eastern perimeter',
        N'0', N'0',
        N'tall athletic woman in weathered campaign plate, dark blonde short hair, left hand missing three fingers, healed jaw scar, Dortmund stone garrison, Buehlman dark medieval --ar 2:3',
        N'A 47-year-old athletic woman in worn campaign plate armor, missing three fingers on her left hand, healed scar across the jaw',
        0, 0
    );
    PRINT N'Brunhild Metzger seeded.';
END
ELSE PRINT N'Brunhild Metzger already exists.';
GO

-- ── 12. Friedrich Kessler ────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Friedrich Kessler')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Friedrich Kessler', N'friedrich-kessler', N'canon', 1,
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
        @id, N'Friedrich Kessler', N'friedrich-kessler', N'Friedrich', N'Kessler', N'',
        N'human', N'human', N'male', N'he/him',
        84, N'alive',
        N'Elder civilian; last living witness to the fall of House Maren; Bonn civic quarter.',
        N'Friedrich Kessler was twelve years old when House Maren''s walls came down and Fornax''s clerks began cataloguing everything that had been Maren''s and assigning it a Fornax provenance. He watched them do it. He is eighty-four now, writes in a cipher no one has asked to learn, and attends House Fornax civic functions with the patient courtesy of a man who has outlived everyone who wronged him.',
        N'The living archive of a House that should not exist anymore — what occupation looks like from the inside, seventy years on, when the occupied man is still present.',
        N'No POV.',
        N'House Fornax (absorbed from fallen House Maren at age 12); Bonn civic quarter',
        170, 64, N'lean, bowed by age, still present in the chest',
        N'white', N'wispy', N'short',
        N'pale grey', N'pale', N'deeply lined, age-spotted, eyes clear',
        N'none',
        N'uses a walking staff; head still lifts when he hears military drill in the street; pauses before answering any question for a count of two',
        N'Fornax civilian robes; a Maren-style collar pin of no obvious significance he has worn every day for sixty years, which no Fornax official has ever recognized as foreign',
        N'none',
        N'Spends mornings writing in his cipher journal; accepts historical consultation requests from Fornax bureaucrats who do not know what they are asking; attends the Bonn civic council as an observer; eats one meal a day; does not sleep much and does not explain why.',
        N'He alone knows that the Maren metallurgical process Fornax now calls proprietary — and traces to a campaign conquest — was not captured in war. Three days before House Maren''s walls fell, Maren''s chief weapons-master Gerd Ulfen sold the full process documentation to a Fornax trade representative for a price Friedrich has never disclosed. Friedrich was in the room when Ulfen signed. The walls fell for other reasons.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Bonn civic quarter',
        N'0', N'0',
        N'very old lean Germanic man with a walking staff, pale grey eyes, Maren collar pin, Bonn stone civic hall, patient face, Buehlman dark medieval --ar 2:3',
        N'An 84-year-old lean man with a walking staff, pale grey eyes, deeply lined face, wearing a small foreign collar pin',
        0, 0
    );
    PRINT N'Friedrich Kessler seeded.';
END
ELSE PRINT N'Friedrich Kessler already exists.';
GO

-- ── 13. Elke Damm ────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Elke Damm')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Elke Damm', N'elke-damm', N'canon', 1,
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
        @id, N'Elke Damm', N'elke-damm', N'Elke', N'Damm', N'',
        N'human', N'human', N'female', N'she/her',
        43, N'alive',
        N'Oathless courier; formerly Fornax intelligence operative, Cologne post.',
        N'Elke Damm was a Fornax intelligence courier for twelve years and was good enough at it that when she found her own name on a Liturgy transit taking manifest, her first reaction was professional: she identified the authorization signature, confirmed the leak, and left the Cologne post within two hours without being seen. She has been Oathless for fourteen months and moves accordingly.',
        N'The spy who became the intelligence — she carries the document that names her handler as a traitor and is deciding what it is worth and to whom.',
        N'No POV.',
        N'Oathless; formerly House Fornax, Cologne intelligence post',
        167, 59, N'slight, fast',
        N'dark brown', N'cut short for travel', N'short',
        N'dark brown', N'pale-olive', N'tired, watchful, no cosmetics',
        N'none',
        N'back against wall when seated; nearest exit noted before sitting; carries weight on the balls of her feet even at rest',
        N'Oathless-neutral traveling clothes; two hidden inner pockets she installed herself; nothing that catches light',
        N'none',
        N'Moves between border towns on the Rhine-Danube corridor; carries messages and small packages under a working name; never stays two nights in the same place; eats what is available; does not drink.',
        N'The Liturgy transit manifest naming her as a Sphere 31 taking was authorized by Fornax intelligence chief Gerhard Venn — the man who recruited and trained her for twelve years. She kept the original document. She has had two opportunities to use it against Venn and declined both: not from hesitation but because the leverage the document holds is not yet worth more than the leverage Venn does not know she has.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Rhine-Danube border towns, traveling; no fixed address',
        N'0', N'0',
        N'slight dark-haired woman in plain neutral traveling clothes, dark eyes, watchful posture, Rhine border town at dusk, Buehlman dark medieval --ar 2:3',
        N'A 43-year-old slight woman with short dark hair in neutral traveling clothes, dark watchful eyes, border town',
        0, 0
    );
    PRINT N'Elke Damm seeded.';
END
ELSE PRINT N'Elke Damm already exists.';
GO

-- ── 14. Konrad Speer ─────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Konrad Speer')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Konrad Speer', N'konrad-speer', N'canon', 1,
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
        @id, N'Konrad Speer', N'konrad-speer', N'Konrad', N'Speer', N'',
        N'human', N'human', N'male', N'he/him',
        55, N'alive',
        N'Sphere 31 lineage interpreter; forge-yard administrator; Koblenz translation bureau.',
        N'Konrad Speer''s grandmother was taken by the Liturgy for Sphere 31 labor service, integrated, and returned carrying the language in her mouth and Sphere 31 manner in her hands. Konrad inherited both. He speaks Sphere 31 dialect as a first language and Fornax-standard as his second, which makes him unusual and useful and therefore alive in a House that does not know what else to do with him.',
        N'The Sphere 31 inside Fornax — what three generations of integration looks like when it still reads as foreign from both directions.',
        N'No POV.',
        N'House Fornax (Sphere 31 integrated lineage; grandmother was a Liturgy taking); Koblenz translation bureau',
        176, 80, N'average, administratively soft',
        N'black-grey', N'worn medium', N'medium',
        N'dark brown', N'medium-brown', N'clear, unhurried, unreadable at distance',
        N'none',
        N'speaks with his hands in a Sphere 31 gestural pattern he has never consciously adopted; comfortable in silence in a way Fornax-born people find unusual',
        N'Fornax administrative wool; a Sphere 31 woven underband visible at the collar that he has never explained and no one has directly asked him about',
        N'none',
        N'Translates Sphere 31 manuscripts for the Koblenz Scrying bureau; advises on Installation signal decryption; attends weekly bureau meetings and is never asked to chair them; eats at his desk; maintains a correspondence archive in a locked box under his bed.',
        N'He has maintained a coded correspondence with a Sphere 31 contact — name unknown, identity stable across nine years of exchange — passing small technical observations about Fornax Scrying Installation methods in exchange for information about what became of his grandmother''s original family in Sphere 31. He has decided that whether this constitutes intelligence is the wrong question to be asking.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Koblenz translation bureau',
        N'0', N'0',
        N'middle-aged man with medium-brown skin and Sphere 31 woven collar band, black-grey hair, stone translation office, Koblenz, Buehlman dark medieval --ar 2:3',
        N'A 55-year-old man with medium-brown skin and dark eyes at a stone translation desk, wearing a foreign woven collar band',
        0, 0
    );
    PRINT N'Konrad Speer seeded.';
END
ELSE PRINT N'Konrad Speer already exists.';
GO

-- ── 15. Siegrid Wolff ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Siegrid Wolff')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Siegrid Wolff', N'siegrid-wolff', N'canon', 1,
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
        @id, N'Siegrid Wolff', N'siegrid-wolff', N'Siegrid', N'Wolff', N'',
        N'human', N'human', N'female', N'she/her',
        62, N'alive',
        N'Furnace elder; informal Bheur-rite conductor; Essen furnace district worker.',
        N'Siegrid Wolff has worked the Essen furnaces for forty years. For twenty of those years she has conducted informal Bheur rites at shift shutdowns and at deaths on the furnace floor. No one asked her to start. No one has asked her to stop. The workers attend without being told to, and the Fornax overseers have decided this is the kind of problem that does not become smaller when examined directly.',
        N'The furnace-floor theology — what people make of Bheur when the institution offers them nothing and the work keeps killing them.',
        N'No POV.',
        N'House Fornax; Essen furnace district',
        161, 68, N'compact, smoke-weathered',
        N'ash-grey', N'braided', N'long',
        N'dark grey', N'ruddy-dark from furnace exposure', N'smoke-lined, burn marks on both forearms',
        N'none',
        N'moves slow and certain; offers silence before speaking; does not fill pauses; applies an ash mark to her own forehead with one finger at the start of every shift',
        N'heavy wool, leather forearm guards, ash mark on the forehead at shift start',
        N'none',
        N'Runs a furnace gang through full shifts; checks equipment others skip; conducts Bheur rites at shift-end for workers who want them; is consulted on deaths in the district; eats with the crew; does not discuss Bheur theology, only practice.',
        N'Three years ago a ventilation failure in the secondary furnace corridor caused a slow eleven-hour death for a worker named Anders Bruck. Siegrid had submitted two written maintenance reports about the ventilation fault in the months prior. She said nothing to the Transmutation overseers who investigated and ruled it an equipment failure of unknown origin. She conducted Anders Bruck''s Bheur rite herself, alone, the night he died. She has not submitted a written maintenance report since.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Essen furnace district',
        N'0', N'0',
        N'compact older woman with ash-grey braided hair and burn-marked forearms, ash mark on forehead, stone furnace hall, Essen, orange furnace glow, Buehlman dark medieval --ar 2:3',
        N'A 62-year-old compact woman with ash-grey braided hair and burned forearms, ash mark on forehead, standing before a furnace',
        0, 0
    );
    PRINT N'Siegrid Wolff seeded.';
END
ELSE PRINT N'Siegrid Wolff already exists.';
GO
