SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- HOUSE LACERTA — ACTIVE POPULATION BATCH A  (15 characters)
-- Universe: Cauld  (ID 0197E9C9-0002-7000-8000-000000000002)
-- 2026-07-05 · Idempotent (IF NOT EXISTS guards on all inserts)
-- Westernmost House; Atlantic cliff coast; Iberian/Portuguese/Basque heritage.
-- Categories: Scrying operators (3) · Knights (1) · Practitioners (1)
--   Merchants (2) · Scholars (1) · Explorers (1) · Liturgy contacts (1)
--   Myrmidon veterans (2) · Stone engineers (1) · Domestic (1) · Recruits (1)
-- ═══════════════════════════════════════════════════════════════════════════════

-- ── 1. Gonçalo Bettencourt ───────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Gonçalo Bettencourt')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Gonçalo Bettencourt', N'goncalo-bettencourt', N'canon', 1,
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
        @id, N'Gonçalo Bettencourt', N'goncalo-bettencourt', N'Gonçalo', N'Bettencourt', N'',
        N'human', N'human', N'male', N'he/him',
        52, N'alive',
        N'Chief Scrying Operator, Lacerta Chamber; twenty years at the apparatus; first to formally document the western anomaly and the person who then suppressed his own report',
        N'Twenty years at the Lacerta Chamber apparatus. His full written account of the western anomaly was returned with a notation to reclassify it as instrument drift. He reclassified the log entry. He did not destroy the original.',
        N'The suppressed-log holder; his private fourteen-volume account is the most complete record of the western anomaly in existence.',
        N'No POV.',
        N'House Lacerta; Lacerta Chamber cliff installation',
        171, 74, N'lean, desk-built, twenty years of instrument work in his hands',
        N'iron-gray', N'combed back', N'short',
        N'brown', N'olive, cliff-weathered', N'lined, quiet',
        N'none',
        N'Methodical and contained; the stillness of long, close attention',
        N'Heavy wool work garments, practical, oil-stained',
        N'none',
        N'Apparatus calibration at dawn, log review, staff briefings, evening instrument checks.',
        N'He has kept fourteen private log volumes on the anomaly, hidden behind a false panel in his quarters.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Lacerta Chamber cliff installation',
        N'0', N'0',
        N'Iberian man 52, iron-gray hair combed back, lean, stone Scrying Chamber interior, Atlantic cliff, focused meticulous expression, medieval dark fantasy, Buehlman register',
        N'A weathered Iberian man of 52 at a stone Scrying apparatus, iron-gray hair combed back, olive skin, focused and guarded expression',
        0, 0
    );
    PRINT N'Gonçalo Bettencourt seeded.';
END
ELSE PRINT N'Gonçalo Bettencourt already exists.';
GO

-- ── 2. Beatriz Fonseca ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Beatriz Fonseca')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Beatriz Fonseca', N'beatriz-fonseca', N'canon', 1,
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
        @id, N'Beatriz Fonseca', N'beatriz-fonseca', N'Beatriz', N'Fonseca', N'',
        N'human', N'human', N'female', N'she/her',
        24, N'alive',
        N'Junior Scrying Operator, Lacerta Chamber; six months posted; only person who has heard the western anomaly as well as seen it',
        N'Six months at the Chamber. On her fourth shift she experienced what the log calls instrument error. She heard it before she saw it. She has not reported what she heard. She is not certain language would hold it.',
        N'The one witness whose sensory experience of the anomaly exceeds what any instrument or report could hold.',
        N'No POV.',
        N'House Lacerta; Lacerta Chamber cliff installation, recruit intake',
        163, 55, N'slight, indoor-pale from overnight shifts',
        N'dark brown', N'tied back', N'medium',
        N'hazel', N'olive, indoor-pale', N'young, unremarkable',
        N'none',
        N'Alert and slightly taut; she has spent six months watching something she cannot name',
        N'Operator''s working dress, plain',
        N'none',
        N'Apparatus monitoring, log transcription, maintenance rotations, overnight shifts. She has reviewed her own shift log seventeen times.',
        N'During the anomaly she heard a single word. She transcribed it phonetically and hid the paper inside her mattress.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Lacerta Chamber cliff installation',
        N'0', N'0',
        N'young Iberian woman 24, dark brown hair tied back, hazel eyes, Scrying Chamber interior night, alert taut expression, stone cliff installation, medieval dark fantasy',
        N'A young Iberian woman of 24 at a stone Scrying apparatus at night, dark hair tied back, hazel eyes, alert and slightly afraid',
        0, 0
    );
    PRINT N'Beatriz Fonseca seeded.';
END
ELSE PRINT N'Beatriz Fonseca already exists.';
GO

-- ── 3. Ezpela Larramendi ─────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ezpela Larramendi')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ezpela Larramendi', N'ezpela-larramendi', N'canon', 1,
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
        @id, N'Ezpela Larramendi', N'ezpela-larramendi', N'Ezpela', N'Larramendi', N'Dame',
        N'human', N'human', N'female', N'she/her',
        37, N'alive',
        N'Dame; western patrol circuit captain; Knight; three seasons running patrols past the authorized boundary without reporting what she found',
        N'Dame and western patrol captain. Three seasons she has extended her circuit two leagues past the authorized boundary. Her patrol reports stop at the line. The structure she found on the sea-stack appears in no report.',
        N'The Knight who crossed the authorized line and found a structure; her silence is the entire question.',
        N'No POV.',
        N'House Lacerta; western patrol zone; Basque origins',
        175, 70, N'lean-athletic, slightly taller and denser from Knight infusion',
        N'dark auburn', N'braided back', N'medium',
        N'green', N'olive, wind-weathered', N'patrol-marked, clear',
        N'Subtle height gain, increased density — the Knight''s mark',
        N'Economical and watchful; field-trained to observe before committing to motion',
        N'Military dark green, patrol-worn, no decoration',
        N'Knight-grade Catalyst infusion; increased bone density, marginal height gain, enhanced recovery',
        N'Dawn patrol briefings, western circuit patrol, evening debriefs she edits before submitting.',
        N'Three seasons ago her patrol found a worked-stone structure on a sea-stack two leagues past the authorized boundary.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Western patrol circuit; Lacerta outer boundary',
        N'0', N'0',
        N'Basque-Iberian woman Dame 37, dark auburn braided hair, green eyes, military dark green, Atlantic cliff patrol at dawn, Knight-enhanced build, watchful expression, medieval dark fantasy',
        N'A Basque-Iberian woman of 37 in military dark green, dark auburn hair braided back, green eyes, at an Atlantic cliff at dawn, watchful and contained',
        0, 0
    );
    PRINT N'Ezpela Larramendi seeded.';
END
ELSE PRINT N'Ezpela Larramendi already exists.';
GO

-- ── 4. Sancho Ferreira ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Sancho Ferreira')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Sancho Ferreira', N'sancho-ferreira', N'canon', 1,
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
        @id, N'Sancho Ferreira', N'sancho-ferreira', N'Sancho', N'Ferreira', N'',
        N'human', N'human', N'male', N'he/him',
        40, N'alive',
        N'Myrmidon corporal, southern garrison; eleven-year veteran; one of the soldiers ordered to carry the unauthorized Catalyst dead',
        N'Eleven-year Myrmidon corporal. He was on the southern barracks when seventeen soldiers died of unauthorized Catalyst infusions. The official record lists them as combat dead. He helped carry the bodies. He counted the faces.',
        N'The soldier whose body count does not match the official record; his knowledge has no safe destination.',
        N'No POV.',
        N'House Lacerta; southern garrison',
        183, 89, N'stocky and campaign-hardened',
        N'salt-and-pepper', N'close-cropped', N'short',
        N'brown', N'dark olive, campaign-marked', N'weathered, scarred at the right forearm',
        N'none',
        N'Solid and watchful; keeps his face clear of what he is thinking',
        N'Corps garrison kit, worn but maintained',
        N'none',
        N'Garrison duties, weapons rotation, monthly southern patrol. Evenings reading casualty records he is not authorized to access.',
        N'He helped carry seventeen bodies from the unauthorized Catalyst chamber. He counted their faces before they were wrapped.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Southern garrison; Lacerta Corps',
        N'0', N'0',
        N'stocky Iberian soldier 40, salt-pepper cropped hair, brown eyes, garrison armor dark green, stone southern barracks, closed watchful expression, medieval dark fantasy',
        N'A stocky Iberian soldier of 40, salt-and-pepper hair, garrison armor, standing in a stone barracks with a closed and careful expression',
        0, 0
    );
    PRINT N'Sancho Ferreira seeded.';
END
ELSE PRINT N'Sancho Ferreira already exists.';
GO

-- ── 5. Constança Mota ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Constança Mota')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Constança Mota', N'constanca-mota', N'canon', 1,
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
        @id, N'Constança Mota', N'constanca-mota', N'Constança', N'Mota', N'',
        N'human', N'human', N'female', N'she/her',
        45, N'alive',
        N'House Practitioner; administers Catalyst infusions; holds the only accurate mortality count for the procedure',
        N'House Practitioner who administers the Catalyst infusion. Forty-one procedures performed. Thirty-three died. She keeps both counts in a private ledger separate from the official record. The official count reads differently.',
        N'The practitioner who knows the true mortality rate; her private ledger is both evidence and confession.',
        N'No POV.',
        N'House Lacerta; estate medical quarters',
        162, 61, N'slight, precise in movement',
        N'brown with silver threads', N'pinned back', N'short',
        N'dark brown', N'medium olive, indoor', N'composed, tired around the eyes',
        N'none',
        N'Careful and precise; she handles fragile things daily and has extended this to everything',
        N'Practitioner''s working attire, clean and unembellished',
        N'none',
        N'Infusion preparation, post-procedure monitoring, supply requisition, official record entry. Private ledger updated after each procedure.',
        N'She administered a flawed batch on the Commander''s verbal order. Three soldiers died. Her private ledger records this.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'House Lacerta Infusion Chamber; estate medical quarters',
        N'0', N'0',
        N'Iberian woman 45, brown-silver hair pinned, dark brown eyes, Practitioner''s attire, stone medical chamber candlelight, precise and tired expression, medieval dark fantasy',
        N'An Iberian woman of 45, hair pinned back with silver threads, working in a stone medical chamber, expression composed and quietly tired',
        0, 0
    );
    PRINT N'Constança Mota seeded.';
END
ELSE PRINT N'Constança Mota already exists.';
GO

-- ── 6. Mencía Arroyo ─────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Mencía Arroyo')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Mencía Arroyo', N'mencia-arroyo', N'canon', 1,
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
        @id, N'Mencía Arroyo', N'mencia-arroyo', N'Mencía', N'Arroyo', N'',
        N'human', N'human', N'female', N'she/her',
        48, N'alive',
        N'Atlantic coast supply merchant; contracts with three Lacerta cliff installations; has protected an Oathless contact in her supply network for two years',
        N'Supplies three coastal installations by sea route. One of her regular harbor contacts is Oathless and has been for two years. Mencía discovered this four months after hiring her and has said nothing.',
        N'The merchant whose protection of one Oathless contact is the thread that could unravel her commercial standing.',
        N'No POV.',
        N'House Lacerta; cliff port, Atlantic coast supply routes',
        161, 70, N'stocky and sea-weathered',
        N'black with gray streaks', N'worn loose or tied', N'medium',
        N'brown', N'dark olive, sun-and-salt marked', N'heavily weathered, expressive',
        N'none',
        N'Grounded and self-contained; shows less than she sees',
        N'Merchant''s practical wool, sea-salt marked, well-made',
        N'none',
        N'Harbor market at dawn, coastal installation supply runs, evening accounts. Weekly contact with the harbor widow.',
        N'The harbor contact Greta Vás is Oathless. Mencía discovered this four months after hiring her and said nothing.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Atlantic coast supply routes; cliff port; northern landing',
        N'0', N'0',
        N'stocky Iberian merchant woman 48, black-gray streaked hair, Atlantic harbor stone dock, practical wool, grounded self-contained expression, medieval dark fantasy',
        N'A stocky Iberian merchant woman of 48, black hair with gray streaks, at an Atlantic harbor dock in practical sea-worn wool',
        0, 0
    );
    PRINT N'Mencía Arroyo seeded.';
END
ELSE PRINT N'Mencía Arroyo already exists.';
GO

-- ── 7. Elvira Gomes ──────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Elvira Gomes')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Elvira Gomes', N'elvira-gomes', N'canon', 1,
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
        @id, N'Elvira Gomes', N'elvira-gomes', N'Elvira', N'Gomes', N'',
        N'human', N'human', N'female', N'she/her',
        56, N'alive',
        N'Estate scholar; pre-House era Atlantic records; has found three historical accounts of the western anomaly predating the Lacerta Chamber',
        N'Pre-House Atlantic scholar. She has traced the western anomaly to three independent accounts predating the Lacerta Chamber by three centuries. The oldest uses a word she cannot translate. She has not published her findings.',
        N'The scholar who proved the anomaly predates the House; the word she cannot translate is the crux.',
        N'No POV.',
        N'House Lacerta; estate library and archive',
        160, 65, N'soft-built, slightly stooped from decades over documents',
        N'white', N'worn loose', N'medium',
        N'light brown', N'pale olive, indoor', N'ink-stained hands, unmarked face',
        N'none',
        N'Stooped and absorbed; rarely fully present in the room she is in',
        N'Scholar''s work dress, practical and ink-marked',
        N'none',
        N'Library research, archive cross-reference, scholarly correspondence. No social obligations she accepts.',
        N'She accessed a Liturgy doctrinal text under a borrowed credential and found the same untranslatable word from her oldest source.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'House Lacerta estate library and archive',
        N'0', N'0',
        N'elderly Iberian scholar woman 56, white hair loose, pale olive skin, surrounded by stacked documents in stone library, absorbed remote expression, medieval dark fantasy',
        N'An Iberian woman of 56, white hair loose, seated in a stone library surrounded by document stacks, absorbed and slightly remote from the present',
        0, 0
    );
    PRINT N'Elvira Gomes seeded.';
END
ELSE PRINT N'Elvira Gomes already exists.';
GO

-- ── 8. Afonso Cabral ─────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Afonso Cabral')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Afonso Cabral', N'afonso-cabral', N'canon', 1,
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
        @id, N'Afonso Cabral', N'afonso-cabral', N'Afonso', N'Cabral', N'',
        N'human', N'human', N'male', N'he/him',
        33, N'alive',
        N'Atlantic explorer and navigator; returned from an unauthorized western voyage one crew member short; has not corrected the official record',
        N'Sailed west past the authorized chart boundary two seasons ago with three crew. Returned with two. The missing man is listed as lost overboard. Afonso has not corrected this record.',
        N'The explorer who left someone behind and came back with something undeclared; both facts remain buried.',
        N'No POV.',
        N'House Lacerta; Atlantic cliff harbor',
        177, 74, N'lean and sea-hardened',
        N'dark brown', N'cut short', N'short',
        N'hazel', N'very dark olive, sun-weathered', N'Atlantic-worn, clear',
        N'none',
        N'Relaxed in the upper body; his hands are very still, which they weren''t before the voyage',
        N'Navigator''s practical clothing, Atlantic-worn',
        N'none',
        N'Vessel maintenance, chart work, supply requests for the next voyage he keeps finding reasons to defer.',
        N'He left crewman Pável alive on a sea-stack west after Pável found the hidden fragment in the hold.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Atlantic cliff harbor; sea routes; western boundary',
        N'0', N'0',
        N'lean Iberian navigator 33, dark brown hair, hazel eyes, very dark weathered skin, Atlantic harbor, chart in hand, carefully neutral expression, medieval dark fantasy',
        N'A lean Iberian navigator of 33, dark hair, hazel eyes, very dark weathered skin, at an Atlantic harbor, expression carefully neutral',
        0, 0
    );
    PRINT N'Afonso Cabral seeded.';
END
ELSE PRINT N'Afonso Cabral already exists.';
GO

-- ── 9. Toda Esteban ──────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Toda Esteban')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Toda Esteban', N'toda-esteban', N'canon', 1,
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
        @id, N'Toda Esteban', N'toda-esteban', N'Toda', N'Esteban', N'Sister',
        N'human', N'human', N'female', N'she/her',
        44, N'alive',
        N'Liturgy tithe and census officer stationed in the cliff port; officially compliance reporting; actually monitoring the Scrying Chamber on unauthorized orders from an unverified handler',
        N'Liturgy tithe officer stationed in the cliff port. Her official function is compliance reporting. For three years she has filed supplementary reports on Scrying Chamber access patterns to a handler outside the standard compliance office.',
        N'The Liturgy officer whose handler exists outside the official Liturgy structure; she does not know who she reports to.',
        N'No POV.',
        N'House Lacerta; cliff port (Liturgy posting)',
        167, 63, N'moderate, formally held',
        N'black', N'in a bun', N'long (pinned)',
        N'dark brown', N'medium olive', N'composed, unmarked',
        N'none',
        N'Professionally composed; the formal stillness of Liturgy training now worn as armor',
        N'Liturgy official attire, formal gray, plain',
        N'none',
        N'Port census rounds, tithe collection, monthly estate compliance review, encrypted correspondence she seals herself.',
        N'Her supplementary Chamber reports went to a handler who does not appear in any Liturgy directory she can access.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Cliff port; estate monthly compliance visit; Liturgy correspondence range',
        N'0', N'0',
        N'Iberian Liturgy sister 44, black hair in bun, dark brown eyes, formal gray attire, cliff port stone interior, professionally neutral expression, medieval dark fantasy',
        N'An Iberian woman of 44 in formal gray Liturgy attire, black hair in a bun, standing in a stone cliff port interior, expression professionally neutral',
        0, 0
    );
    PRINT N'Toda Esteban seeded.';
END
ELSE PRINT N'Toda Esteban already exists.';
GO

-- ── 10. Lopo Carvalho ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Lopo Carvalho')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Lopo Carvalho', N'lopo-carvalho', N'canon', 1,
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
        @id, N'Lopo Carvalho', N'lopo-carvalho', N'Lopo', N'Carvalho', N'',
        N'human', N'human', N'male', N'he/him',
        38, N'alive',
        N'Myrmidon veteran; twelve years; his unit was ambushed on intelligence only three officers knew; he is two steps from a name',
        N'Twelve-year Myrmidon. His unit was ambushed on a route only three officers knew. Four men died. He has spent three years identifying which officer sold the information. He is close.',
        N'The veteran turned internal investigator, two steps from a name that will put him in danger.',
        N'No POV.',
        N'House Lacerta; Lacerta Corps garrison',
        180, 83, N'lean-muscled, compact',
        N'dark brown', N'close-cropped', N'short',
        N'brown', N'dark olive', N'scar on jaw from the ambush engagement, otherwise unmarked',
        N'none',
        N'Alert and self-contained; moves through rooms like someone working a question',
        N'Corps garrison kit, practical',
        N'none',
        N'Standard Myrmidon rotations. Evenings he maintains a private written account of the ambush.',
        N'He has identified the informant as Captain Aldeiro of the northern installation, who reports to Ferran Alcaine.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Lacerta Corps garrison; southern patrol range',
        N'0', N'0',
        N'lean Iberian soldier 38, dark cropped hair, jaw scar, garrison armor, stone barracks corridor, alert watchful expression, medieval dark fantasy',
        N'A lean Iberian soldier of 38, dark cropped hair, scar on jaw, in garrison armor in a stone corridor, alert expression',
        0, 0
    );
    PRINT N'Lopo Carvalho seeded.';
END
ELSE PRINT N'Lopo Carvalho already exists.';
GO

-- ── 11. Rodrigo Vasconcelos ──────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Rodrigo Vasconcelos')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Rodrigo Vasconcelos', N'rodrigo-vasconcelos', N'canon', 1,
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
        @id, N'Rodrigo Vasconcelos', N'rodrigo-vasconcelos', N'Rodrigo', N'Vasconcelos', N'',
        N'human', N'human', N'male', N'he/him',
        50, N'alive',
        N'Coastal merchant and navigator; eighteen years on the Atlantic cliff routes; found unidentified wreckage west of the Chamber and has not declared it',
        N'Eighteen years on the Atlantic cliff supply routes. Last spring he pulled a piece of wreckage from two leagues west of the Chamber — worked material, no known origin. It is under a floor board in his warehouse.',
        N'The merchant whose hidden wreckage is material evidence connecting the anomaly to something physical and recent.',
        N'No POV.',
        N'House Lacerta; Atlantic cliff coast, harbor district',
        174, 82, N'stocky-built, sea-settled weight',
        N'gray-brown', N'worn short', N'short',
        N'dark brown', N'very dark olive, sun-and-salt weathered', N'deeply weathered, lines at the eyes',
        N'none',
        N'Solid and unhurried; eighteen years on the water have settled into a stillness that has nothing to do with ease',
        N'Navigator''s wool, sea-salt maintained',
        N'none',
        N'Morning tide charter, coastal supply runs, evening accounts. Checks the warehouse floor twice weekly.',
        N'He found a preserved human hand in the wreckage. He buried it in beach shingle at low tide and has told no one.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Atlantic cliff coast routes; harbor warehouse',
        N'0', N'0',
        N'stocky Iberian navigator 50, gray-brown hair, very dark weathered skin, Atlantic harbor dock, solid unhurried expression, medieval dark fantasy',
        N'A stocky Iberian man of 50, gray-brown hair, very dark weathered skin, standing at an Atlantic harbor dock in navigator''s wool, solid expression',
        0, 0
    );
    PRINT N'Rodrigo Vasconcelos seeded.';
END
ELSE PRINT N'Rodrigo Vasconcelos already exists.';
GO

-- ── 12. Inés Faria ───────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Inés Faria')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Inés Faria', N'ines-faria', N'canon', 1,
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
        @id, N'Inés Faria', N'ines-faria', N'Inés', N'Faria', N'',
        N'human', N'human', N'female', N'she/her',
        29, N'alive',
        N'Overnight Scrying Operator; discovered unauthorized log access using a key issued to a retired operator; has traced it to the Heir',
        N'Overnight Scrying Operator. Six weeks ago she found anomalous access patterns in the log: someone reviewing the Chamber records between third and fourth hour using a key issued to an operator retired two seasons ago.',
        N'The operator who traced unauthorized log access to the Heir; she has not yet decided what to do.',
        N'No POV.',
        N'House Lacerta; Lacerta Chamber overnight installation',
        165, 58, N'slim, indoor-careful',
        N'dark brown', N'pinned up', N'medium (pinned)',
        N'brown', N'olive, indoor-pale from overnight shifts', N'young, attentive',
        N'none',
        N'Attentive and slightly over-careful; she listens to rooms she enters now',
        N'Operator''s working dress, neat',
        N'none',
        N'Overnight apparatus shift, daytime sleep, weekly staff briefing. Six weeks watching the same access anomaly.',
        N'She traced the unauthorized Chamber access to a key the Heir holds, acquired from a retired operator.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Lacerta Chamber overnight installation',
        N'0', N'0',
        N'young Iberian woman 29, dark brown hair pinned up, brown eyes, Scrying Chamber stone interior night, operator''s dress, attentive over-careful expression, medieval dark fantasy',
        N'An Iberian woman of 29, dark hair pinned up, in operator''s dress at a stone Scrying Chamber at night, expression careful and watchful',
        0, 0
    );
    PRINT N'Inés Faria seeded.';
END
ELSE PRINT N'Inés Faria already exists.';
GO

-- ── 13. Brais Nogueira ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Brais Nogueira')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Brais Nogueira', N'brais-nogueira', N'canon', 1,
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
        @id, N'Brais Nogueira', N'brais-nogueira', N'Brais', N'Nogueira', N'',
        N'human', N'human', N'male', N'he/him',
        44, N'alive',
        N'Stone engineer; sixteen years maintaining the Lacerta Chamber structure; found stress fractures he was ordered not to log; kept a record anyway',
        N'Stone engineer; sixteen years maintaining the Chamber structure. Six months ago he found stress fractures in the observation platform running in no pattern he can account for. He was ordered to repair them without logging. He made a record.',
        N'The engineer whose suppressed fracture report is the only record of something damaging the Chamber from outside.',
        N'No POV.',
        N'House Lacerta; Chamber structure and cliff installation',
        178, 92, N'heavy-set, stone-built',
        N'dark brown going gray', N'roughly kept', N'short',
        N'brown', N'olive, stone-dust marked', N'weathered, calloused hands',
        N'none',
        N'Heavy and deliberate; a man who reads stone the way others read faces',
        N'Heavy canvas work clothes, stone-dust marked',
        N'none',
        N'Dawn structure inspection, maintenance work, evening report review. Private fracture record hidden in his tool chest.',
        N'The fractures radiate from a single point on the western wall as if pushed from outside. He made a rubbing and hid it.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Lacerta Chamber structure and cliff installation platforms',
        N'0', N'0',
        N'heavy-set Iberian stone engineer 44, dark going gray hair, calloused hands, canvas work clothes, cliff chamber stone wall inspection, deliberate expression, medieval dark fantasy',
        N'A heavy-set Iberian man of 44, dark hair going gray, in canvas work clothes inspecting a stone chamber wall, deliberate and careful expression',
        0, 0
    );
    PRINT N'Brais Nogueira seeded.';
END
ELSE PRINT N'Brais Nogueira already exists.';
GO

-- ── 14. Aldara Rocha ─────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Aldara Rocha')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Aldara Rocha', N'aldara-rocha', N'canon', 1,
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
        @id, N'Aldara Rocha', N'aldara-rocha', N'Aldara', N'Rocha', N'',
        N'human', N'human', N'female', N'she/her',
        51, N'alive',
        N'Head housekeeper of the Lacerta estate; fourteen years managing domestic staff; found a receipt in a coat that matches no ledger she has ever processed',
        N'Fourteen years as head housekeeper of the Lacerta estate. Fourteen months ago she found a supply receipt in a coat sent for laundering that matched no ledger line she had ever processed.',
        N'The housekeeper whose accidental discovery of Ramiro''s theft makes her the most dangerous unintentional witness in the estate.',
        N'No POV.',
        N'House Lacerta; estate domestic staff',
        160, 68, N'round-built and practical',
        N'silver', N'tied back', N'short',
        N'brown', N'olive, indoor', N'composed, unremarkable',
        N'none',
        N'Efficient and unremarkable; moves through the estate as if she is part of its architecture',
        N'Plain working dress, housekeeper''s gray, always clean',
        N'none',
        N'Dawn staff briefings, domestic account reconciliation, staff dispute resolution, evening household review.',
        N'She memorized an off-ledger clearing account number from a receipt in Ramiro''s coat. She has not told the Treasurer.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'House Lacerta estate; domestic staff range',
        N'0', N'0',
        N'Iberian housekeeper 51, silver hair tied back, round-built, plain gray working dress, stone estate corridor, efficient unremarkable expression, medieval dark fantasy',
        N'An Iberian woman of 51, silver hair tied back, round-built, in plain gray working dress in a stone estate corridor, efficient expression',
        0, 0
    );
    PRINT N'Aldara Rocha seeded.';
END
ELSE PRINT N'Aldara Rocha already exists.';
GO

-- ── 15. Ramiro Teixeira ──────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ramiro Teixeira')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ramiro Teixeira', N'ramiro-teixeira', N'canon', 1,
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
        @id, N'Ramiro Teixeira', N'ramiro-teixeira', N'Ramiro', N'Teixeira', N'',
        N'human', N'human', N'male', N'he/him',
        18, N'alive',
        N'Myrmidon recruit, first posting; enlisted three weeks after his sister died from a Catalyst infusion; has told the Corps nothing about her',
        N'Enlisted three weeks after his sister died from a Catalyst infusion. He is eighteen, the youngest in his intake, and has not told the Corps he knew her. He trains hardest in the yard.',
        N'The youngest soldier whose grief is a delayed weapon pointed at an institution that does not know he is there.',
        N'No POV.',
        N'House Lacerta; intake barracks',
        170, 63, N'lean, not yet filled out',
        N'dark brown', N'close-cropped, intake standard', N'short',
        N'brown', N'medium olive', N'young, intake-pale turning weathered',
        N'none',
        N'Rigid from intake training; the over-correct posture of someone trying to look ready',
        N'Corps intake kit, standard issue',
        N'none',
        N'Intake training, weapons drill, barracks. Every free hour in the training yard. Sleeps four hours if lucky.',
        N'His sister was one of the seventeen who died from unauthorized Catalyst infusions. He does not know they were unauthorized.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Intake barracks; training grounds',
        N'0', N'0',
        N'young Iberian recruit 18, dark cropped hair, intake armor, stone training grounds cliff installation, rigid over-correct posture, grief behind composure, medieval dark fantasy',
        N'A young Iberian man of 18 in corps intake armor at a stone training ground, rigid posture, expression controlled and grieving',
        0, 0
    );
    PRINT N'Ramiro Teixeira seeded.';
END
ELSE PRINT N'Ramiro Teixeira already exists.';
GO
