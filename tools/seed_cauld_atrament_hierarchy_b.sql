SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- CAULD UNIVERSE — HOUSE ATRAMENT LOWER HIERARCHY (PART B)
-- Scrying Installation Staff + Domestic Staff + Oathless Adjacent
-- Universe: scry (ID 0197E9C9-0002-7000-8000-000000000002)
-- 2026-07-05
-- 20 characters; idempotent (IF NOT EXISTS guards on all inserts)
-- ═══════════════════════════════════════════════════════════════════════════════

-- ── Ermengarde Vauclaire ──────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ermengarde Vauclaire')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ermengarde Vauclaire', N'ermengarde-vauclaire', N'canon', 1,
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
        @id, N'Ermengarde Vauclaire', N'ermengarde-vauclaire', N'Ermengarde', N'Vauclaire', N'',
        N'human', N'human', N'female', N'she/her', 58, N'alive',
        N'Head Scrying Operator; House Atrament installation; thirty-one years at the apparatus.',
        N'Thirty-one years at the apparatus have left Ermengarde with the stillness of someone who watches rather than acts. She administers the Scrying wing with minimal ceremony: schedules honored, reports filed in correct cipher, nothing shared without authorization. Her authority is quiet and absolute.',
        N'Institutional memory of the Scrying apparatus; controls what observations are reported upstream and what disappears.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        162, 58, N'lean, slight',
        N'silver-white', N'pulled back tight', N'long',
        N'pale grey', N'warm olive', N'weathered, deeply lined',
        N'none',
        N'Stands very still; moves only when necessary; never gestures.',
        N'Dark wool, high collar, no ornament, ink-stained fingers.',
        N'none',
        N'Reviews overnight logs at dawn; assigns watch rotations; audits cipher transcripts; meets the Intelligence Steward at midday.',
        N'Witnessed Sphere 31''s eastern coast submerge into fog twelve years ago; filed it as instrument drift; the coast is gone.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Scrying wing and adjacent corridors; estate grounds.',
        N'0', N'0',
        N'Aged woman silver hair pinned back brass scrying apparatus stone chamber candlelight medieval steampunk portrait',
        N'Elderly woman in dark wool at brass apparatus candlelit stone room',
        0, 0
    );
    PRINT 'Ermengarde Vauclaire seeded.';
END
ELSE PRINT 'Ermengarde Vauclaire already exists.';
GO

-- ── Thibaut Gervais ───────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Thibaut Gervais')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Thibaut Gervais', N'thibaut-gervais', N'canon', 1,
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
        @id, N'Thibaut Gervais', N'thibaut-gervais', N'Thibaut', N'Gervais', N'',
        N'human', N'human', N'male', N'he/him', 41, N'alive',
        N'Senior long-watch Scrying operator; overnight shift; House Atrament installation.',
        N'Thibaut runs the overnight watch with competent indifference: calibrates the apparatus, transcribes what he sees, asks no questions beyond his brief. Other operators find him reliable and incurious. He is neither.',
        N'False reliability; his competence masks active intelligence-sharing with a rival House.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        178, 82, N'stocky, broad-shouldered',
        N'dark brown, receding', N'close-cropped', N'very short',
        N'hazel', N'medium olive', N'unremarkable',
        N'none',
        N'Relaxed, unhurried; occupies space easily; never seems rushed.',
        N'Worn leather jerkin, practical boots, minimal upkeep.',
        N'none',
        N'Monitors apparatus midnight to dawn; sleeps mornings; leaves the estate each market day by the south gate.',
        N'Sells observation session timestamps and target coordinates to a House Renalt courier at the Fournier mill.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Scrying wing; estate grounds; south-gate market route.',
        N'0', N'0',
        N'Stocky middle-aged man at brass instruments dark stone chamber candlelight medieval steampunk',
        N'Stocky man at brass instruments dark stone chamber candlelight',
        0, 0
    );
    PRINT 'Thibaut Gervais seeded.';
END
ELSE PRINT 'Thibaut Gervais already exists.';
GO

-- ── Raoul Bressac ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Raoul Bressac')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Raoul Bressac', N'raoul-bressac', N'canon', 1,
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
        @id, N'Raoul Bressac', N'raoul-bressac', N'Raoul', N'Bressac', N'Master',
        N'human', N'human', N'male', N'he/him', 49, N'alive',
        N'Technical Maintenance Chief; Scrying apparatus; House Atrament installation.',
        N'Raoul keeps the apparatus running through inherited knowledge and improvised repair. No peer in the Cauld for calibrating observation crystals — he knows it and refuses to document his methods. His workshop admits no visitors and returns no borrowed tools.',
        N'Technical gatekeeper; his indispensability makes him nearly impossible to audit or replace.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        171, 79, N'wiry, nimble-fingered',
        N'reddish-brown going grey', N'cropped close', N'short',
        N'dark brown', N'light olive', N'freckled',
        N'none',
        N'Hunches over work; straightens abruptly when addressed; very precise hands.',
        N'Leather apron always worn, tools at belt, oil-stained sleeves.',
        N'none',
        N'Inspects apparatus at dawn and dusk; repairs equipment; requisitions supply through the Seneschal; trains no assistants by design.',
        N'Substituted cheaper replica calibration stones in three secondary nodes; pocketed fourteen hundred marks over four years.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Maintenance workshop; Scrying wing; does not leave the estate.',
        N'0', N'0',
        N'Wiry man with tools examining brass apparatus stone workshop medieval steampunk setting',
        N'Wiry man with tools in stone workshop brass machinery medieval',
        0, 0
    );
    PRINT 'Raoul Bressac seeded.';
END
ELSE PRINT 'Raoul Bressac already exists.';
GO

-- ── Blanche Ferreol ───────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Blanche Ferreol')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Blanche Ferreol', N'blanche-ferreol', N'canon', 1,
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
        @id, N'Blanche Ferreol', N'blanche-ferreol', N'Blanche', N'Ferreol', N'',
        N'human', N'human', N'female', N'she/her', 23, N'alive',
        N'Junior Scrying operator; afternoon and evening watch; House Atrament installation.',
        N'Blanche has been on the apparatus eight months. She is attentive and precise in transcription, and she has told no one about the session in her third week when she observed a Sphere 31 market crowd that included a face matching her brother, dead four years.',
        N'Point of wonder and dread; her unexplained sighting destabilizes what Scrying is supposed to mean.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        165, 56, N'slight, fine-boned',
        N'pale blonde', N'loose', N'shoulder-length',
        N'light blue', N'fair', N'pale',
        N'none',
        N'Leans toward the apparatus; flinches at unexpected sounds; very still otherwise.',
        N'Plain grey shift, hair often unbound, no jewelry.',
        N'none',
        N'Afternoon and early evening watch; transcribes sessions; reviews prior logs obsessively; eats quietly with the other junior operators.',
        N'Her brother Adhemar was Liturgy-taken three years before she believed he died; he may be alive in Sphere 31.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Scrying wing; junior staff quarters.',
        N'0', N'0',
        N'Young pale woman leaning over brass scrying lens wide eyes stone chamber medieval steampunk',
        N'Young blonde woman at brass lens stone chamber candlelight',
        0, 0
    );
    PRINT 'Blanche Ferreol seeded.';
END
ELSE PRINT 'Blanche Ferreol already exists.';
GO

-- ── Hugues Monfort ────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Hugues Monfort')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Hugues Monfort', N'hugues-monfort', N'canon', 1,
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
        @id, N'Hugues Monfort', N'hugues-monfort', N'Hugues', N'Monfort', N'Master',
        N'human', N'human', N'male', N'he/him', 52, N'alive',
        N'Seneschal; estate finance and staff administration; House Atrament.',
        N'Hugues has administered the estate''s finances and staff for nineteen years. He coordinates supply chains, manages creditors, and mediates disputes with the patience of a man who knows most problems resolve if not escalated. The house runs because he permits it to.',
        N'Administrative authority; controls resources and can accelerate or strangle any house operation from within.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        176, 88, N'heavyset, authoritative bearing',
        N'iron grey', N'combed flat', N'short',
        N'grey-green', N'medium fair', N'ruddy-cheeked',
        N'none',
        N'Upright, deliberate; hands clasped behind his back; never fidgets.',
        N'Charcoal wool with house livery trim, always pressed and correct.',
        N'none',
        N'Morning staff review; supply ledger audit; creditor correspondence; afternoon household arbitration; evening accounts reconciliation.',
        N'Has embezzled two thousand marks into a cache beneath the east granary floor over eleven years.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'The estate in its entirety.',
        N'0', N'0',
        N'Heavyset grey-haired man in estate livery stone manor hall medieval French steampunk',
        N'Heavyset man in grey livery stone manor hall medieval',
        0, 0
    );
    PRINT 'Hugues Monfort seeded.';
END
ELSE PRINT 'Hugues Monfort already exists.';
GO

-- ── Marguerite Dufresne ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Marguerite Dufresne')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Marguerite Dufresne', N'marguerite-dufresne', N'canon', 1,
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
        @id, N'Marguerite Dufresne', N'marguerite-dufresne', N'Marguerite', N'Dufresne', N'Mistress',
        N'human', N'human', N'female', N'she/her', 45, N'alive',
        N'Head Cook; estate kitchens and diplomatic table; House Atrament.',
        N'Marguerite commands the kitchen with an efficiency that serves both estate hospitality and the intelligence apparatus — her menus calibrate to visitor moods, her staff report overheard conversation upward, and the quality of her table is Atrament''s most effective diplomatic tool.',
        N'Kitchen as intelligence node; food as hospitality instrument and information harvest.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        163, 74, N'sturdy, strong arms',
        N'dark chestnut', N'pulled into tight cap', N'medium',
        N'brown', N'warm medium brown', N'clear',
        N'none',
        N'Never stops moving in the kitchen; utterly still when observing others.',
        N'Linen apron over dark dress, hair covered, sleeves always rolled.',
        N'none',
        N'Market before dawn; three meal services; debriefs serving staff on overheard conversation after dinner service.',
        N'Deliberately underseasoning envoy Gauthier of House Sylvaine''s portions to provoke irritable candor at the dinner table.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Estate kitchens and market district.',
        N'0', N'0',
        N'Strong middle-aged woman directing kitchen staff stone hearth medieval French manor estate',
        N'Woman in apron directing kitchen stone hearth medieval manor',
        0, 0
    );
    PRINT 'Marguerite Dufresne seeded.';
END
ELSE PRINT 'Marguerite Dufresne already exists.';
GO

-- ── Etienne Pellerin ──────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Etienne Pellerin')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Etienne Pellerin', N'etienne-pellerin', N'canon', 1,
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
        @id, N'Etienne Pellerin', N'etienne-pellerin', N'Etienne', N'Pellerin', N'',
        N'human', N'human', N'male', N'he/him', 28, N'alive',
        N'Sous-chef; estate kitchens; arrived from southern ports three years ago.',
        N'Etienne is technically superior to most head cooks he has worked under and patient enough not to show it often. He arrived three years ago from the southern ports, speaks three trade dialects, and has never fully explained why he left his last posting.',
        N'Competence that exceeds his station; the unexplained past implies ambition or danger not yet surfaced.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        180, 77, N'lean, tall',
        N'black', N'short, close-cropped', N'short',
        N'dark brown', N'deep brown', N'clear',
        N'none',
        N'Economical; tall frame held loose; rarely looks directly at who he addresses.',
        N'White linen work clothes, spotlessly maintained despite the labor.',
        N'none',
        N'Prep cook from dawn; specialty sauces and guest pastries; Tuesday market run; reads kitchen intelligence summaries alongside the Head Cook.',
        N'Stole a Transmutation Catalyst vial from a traveling merchant''s saddlebag; it is hidden behind the salt store.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Estate kitchens; Tuesday market circuit.',
        N'0', N'0',
        N'Tall lean dark-skinned man in kitchen whites stone medieval kitchen focused expression',
        N'Tall dark-skinned man in kitchen whites stone hearth medieval',
        0, 0
    );
    PRINT 'Etienne Pellerin seeded.';
END
ELSE PRINT 'Etienne Pellerin already exists.';
GO

-- ── Gautier Orsenne ───────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Gautier Orsenne')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Gautier Orsenne', N'gautier-orsenne', N'canon', 1,
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
        @id, N'Gautier Orsenne', N'gautier-orsenne', N'Gautier', N'Orsenne', N'',
        N'human', N'human', N'male', N'he/him', 55, N'alive',
        N'Butler; twenty-two years at the estate; guest reception and household staff.',
        N'Gautier has held the butler''s position for twenty-two years and moves through the estate as though he owns the air in it. His voice is soft, his diction impeccable, his recall of every guest preference, slight, and dietary restriction total. He forgets nothing.',
        N'Living archive of every guest, preference, and private indiscretion the house has ever hosted.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        174, 71, N'slim, erect',
        N'white', N'close-cropped', N'very short',
        N'pale blue', N'fair', N'lined',
        N'none',
        N'Perfectly upright; moves in straight lines; hands at sides when standing.',
        N'Black formal livery, white gloves, polished shoes without exception.',
        N'none',
        N'Supervises footmen; manages wine cellar; receives arriving guests; briefs the Lord on visitor preferences before each audience.',
        N'Has intercepted Ermengarde Vauclaire''s personal correspondence for six years; reads it, reseals it, acts on nothing.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'The estate''s public and reception rooms.',
        N'0', N'0',
        N'Elderly white-haired butler in black livery stone manor entrance hall medieval French estate',
        N'White-haired butler in black livery stone hall medieval manor',
        0, 0
    );
    PRINT 'Gautier Orsenne seeded.';
END
ELSE PRINT 'Gautier Orsenne already exists.';
GO

-- ── Alienor Bassac ────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Alienor Bassac')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Alienor Bassac', N'alienor-bassac', N'canon', 1,
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
        @id, N'Alienor Bassac', N'alienor-bassac', N'Alienor', N'Bassac', N'Mistress',
        N'human', N'human', N'female', N'she/her', 48, N'alive',
        N'Head Housekeeper; thirty domestic staff; guest-wing intelligence; House Atrament.',
        N'Alienor manages thirty domestic staff with precision she attributes to routine and her staff attribute to intimidation. The estate''s guest quarters are maintained to exacting standards — surfaces clean, linens fresh, contents of luggage noted each day in a ledger she keeps under her bed.',
        N'Domestic intelligence node; guest quarters are her domain and her information asset.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        167, 69, N'medium, brisk',
        N'auburn going grey', N'tightly braided', N'medium',
        N'green', N'light olive', N'clear',
        N'none',
        N'Quick, angular; pivots sharply; always carrying something; talks while moving.',
        N'Grey wool dress, heavy key-ring at belt, sensible shoes.',
        N'none',
        N'Morning staff assignments; guest-room inspection; afternoon supply inventory; evening debrief of chamber staff on guest behavior.',
        N'Accepts two silver marks weekly from minor lord Raimon Taillefer to overlook her staff searching his rivals'' rooms.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Guest quarters and domestic staff corridors.',
        N'0', N'0',
        N'Austere middle-aged woman with heavy key ring inspecting guest chamber medieval French manor',
        N'Austere woman with key ring in stone guest room medieval manor',
        0, 0
    );
    PRINT 'Alienor Bassac seeded.';
END
ELSE PRINT 'Alienor Bassac already exists.';
GO

-- ── Luc Favre ─────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Luc Favre')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Luc Favre', N'luc-favre', N'canon', 1,
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
        @id, N'Luc Favre', N'luc-favre', N'Luc', N'Favre', N'',
        N'human', N'human', N'male', N'he/him', 22, N'alive',
        N'Household servant; luggage, tables, messages; arrived two years ago with forged references.',
        N'Luc is punctual, reliable, and careful to stay unremarkable. He handles luggage, sets tables, and runs messages without complaint. He arrived two years ago with a convincing letter of reference and a carefully maintained story about his village of origin.',
        N'Concealed Sphere 31 origin; his presence is structural irony — the intelligence house has missed him entirely.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        172, 68, N'slight, quiet',
        N'light brown', N'unkempt', N'short',
        N'grey', N'pale', N'slightly sallow',
        N'none',
        N'Small movements; avoids doorways when others are present; never runs.',
        N'House livery, always correctly buttoned, hair never quite tamed.',
        N'none',
        N'Luggage handling; table setting; message running; evening lantern rounds; careful attention to which doors are guarded.',
        N'Escaped Liturgy service in Sphere 31 three years ago; his real name is not Luc; the reference letter was forged.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Estate public rooms and corridors; avoids guard posts.',
        N'0', N'0',
        N'Young pale man in house livery carrying luggage stone manor corridor medieval',
        N'Young man in house livery stone manor corridor medieval',
        0, 0
    );
    PRINT 'Luc Favre seeded.';
END
ELSE PRINT 'Luc Favre already exists.';
GO

-- ── Beatris Morel ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Beatris Morel')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Beatris Morel', N'beatris-morel', N'canon', 1,
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
        @id, N'Beatris Morel', N'beatris-morel', N'Beatris', N'Morel', N'',
        N'human', N'human', N'female', N'she/her', 19, N'alive',
        N'Household servant; guest-wing duties; message courier between Seneschal and House Calvaire.',
        N'Beatris has worked at the estate since she was fourteen and is trusted with guest-wing duties. For two years she has carried sealed notes between the Seneschal and the estate steward of House Calvaire — notes she has never been told not to read.',
        N'Innocent courier who knows more than she understands; what she carries is a liability for the Seneschal.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        161, 54, N'slight, quick',
        N'dark brown', N'loose braid', N'medium',
        N'brown', N'medium warm brown', N'clear',
        N'none',
        N'Quick, light-footed; bobs her head when addressed; rarely meets eyes.',
        N'House livery, small blue ribbon in braid she is not supposed to wear.',
        N'none',
        N'Chamber service; message carrying; assists Head Housekeeper; afternoon errands to the village market.',
        N'Has read every sealed note; knows Hugues Monfort is negotiating a private land transfer with House Calvaire''s steward.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Guest wing and village market quarter.',
        N'0', N'0',
        N'Young dark-haired girl in house livery with sealed note stone manor corridor medieval French',
        N'Young woman in livery with letter stone corridor medieval manor',
        0, 0
    );
    PRINT 'Beatris Morel seeded.';
END
ELSE PRINT 'Beatris Morel already exists.';
GO

-- ── Yseult Cornant ────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Yseult Cornant')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Yseult Cornant', N'yseult-cornant', N'canon', 1,
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
        @id, N'Yseult Cornant', N'yseult-cornant', N'Yseult', N'Cornant', N'',
        N'human', N'human', N'female', N'she/her', 25, N'alive',
        N'Lady''s personal attendant; schedule, correspondence, and reputation management.',
        N'Yseult manages the Lady Atrament''s schedule, dress, correspondence, and reputation with quiet competence. She is the last person the Lady speaks to at night and the first she sees in the morning. She has held the position fourteen months and has been offered a better one.',
        N'Access to the Lady''s private intentions; the outside offer she has refused signals where her real loyalty sits.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        168, 60, N'graceful, composed',
        N'red-gold', N'elaborately styled daily', N'long',
        N'amber', N'fair', N'freckled',
        N'none',
        N'Poised, unhurried; speaks softly; always a half-step behind the Lady.',
        N'Fine wool, modest cut, hair in a new arrangement each morning.',
        N'none',
        N'Dawn attendance; wardrobe and schedule management; intercepts low-priority correspondence; evening debrief; private cipher journal.',
        N'Knows the Lady is in secret correspondence with House Lacerta''s information minister; her freedom depends on her silence.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'The Lady''s private apartments and formal rooms.',
        N'0', N'0',
        N'Elegant red-haired young woman attending noblewoman stone chamber medieval French estate',
        N'Young woman attending noblewoman stone chamber medieval manor',
        0, 0
    );
    PRINT 'Yseult Cornant seeded.';
END
ELSE PRINT 'Yseult Cornant already exists.';
GO

-- ── Gilles Chevenard ──────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Gilles Chevenard')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Gilles Chevenard', N'gilles-chevenard', N'canon', 1,
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
        @id, N'Gilles Chevenard', N'gilles-chevenard', N'Gilles', N'Chevenard', N'',
        N'human', N'human', N'male', N'he/him', 43, N'alive',
        N'Stable Master; fourteen horses, three mules, carriage fleet; House Atrament.',
        N'Gilles manages fourteen horses, three mules, and the carriage fleet with the authority of someone who trusts animals over people. He is sparse with words, generous with labor, and the best judge of horseflesh in three territories. His opinion on visitors forms at the stables.',
        N'First point of contact for arriving visitors; what he observes at the stables precedes anything the House learns indoors.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        183, 94, N'broad, muscular',
        N'dark brown', N'cropped', N'short',
        N'dark grey', N'tanned, weathered', N'deeply lined',
        N'none',
        N'Heavy, deliberate; comfortable in mud; adjusts slightly when entering stone rooms.',
        N'Oiled leather work clothes, permanently hay-dusted, wide boots always muddy.',
        N'none',
        N'Dawn feeding and inspection; afternoon training; farrier coordination; receives arriving horses; observes riders before they enter the house.',
        N'The prized grey stallion Argentan is a stolen House Miraud war-mount; Gilles dyes its brand marking monthly.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Estate stables, carriage yard, three-territory horse circuit.',
        N'0', N'0',
        N'Broad dark-haired man in leather work clothes with grey horse stone medieval stable',
        N'Broad man with grey horse in stone stables medieval setting',
        0, 0
    );
    PRINT 'Gilles Chevenard seeded.';
END
ELSE PRINT 'Gilles Chevenard already exists.';
GO

-- ── Bertrand Vasseur ──────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Bertrand Vasseur')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Bertrand Vasseur', N'bertrand-vasseur', N'canon', 1,
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
        @id, N'Bertrand Vasseur', N'bertrand-vasseur', N'Bertrand', N'Vasseur', N'',
        N'human', N'human', N'male', N'he/him', 61, N'alive',
        N'Groundskeeper; thirty-seven years on the estate grounds; vine terraces and walls.',
        N'Bertrand has tended the estate grounds for thirty-seven years, through two Lords and one siege. He knows every stone, drain, and sightline on the property with the thoroughness of a man who has never thought about leaving. He is slower now and more deliberate.',
        N'Living map of the estate''s vulnerabilities; his silence about the tunnel is a dormant structural threat.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        170, 76, N'weathered, wiry',
        N'white', N'sparse, uncombed', N'short',
        N'pale blue', N'very tanned', N'deeply lined',
        N'none',
        N'Stooped, slow; pauses to feel the soil; never hurries.',
        N'Mud-stained wool, wide straw hat, ancient boots beyond repair.',
        N'none',
        N'Grounds patrol at dawn; vine maintenance; seasonal planting schedules; evening walk of the walls and outer gardens.',
        N'Found a tunnel beneath the south garden wall twelve years ago exiting beyond the estate boundary; uses it on new moons; told no one.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Estate grounds and outer walls; vine terraces to south garden.',
        N'0', N'0',
        N'Old weathered man tending vines near stone estate wall medieval French countryside overcast',
        N'Old man tending vines by stone wall medieval French countryside',
        0, 0
    );
    PRINT 'Bertrand Vasseur seeded.';
END
ELSE PRINT 'Bertrand Vasseur already exists.';
GO

-- ── Clemence Aubert ───────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Clemence Aubert')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Clemence Aubert', N'clemence-aubert', N'canon', 1,
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
        @id, N'Clemence Aubert', N'clemence-aubert', N'Clemence', N'Aubert', N'',
        N'human', N'human', N'female', N'she/her', 39, N'alive',
        N'House Physician; Collegium Orbonne trained; estate infirmary and intelligence-adjacent health reports.',
        N'Clemence trained at the Collegium Orbonne and accepted the estate posting for laboratory access. She treats staff and guests with equal competence and visible disinterest in their relative status. The Lord consults her on visitors'' health; she tells him what is medically relevant.',
        N'Medical access as intelligence lever; gatekeeps health information and controls what the Lord learns about visitor vulnerabilities.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        166, 62, N'medium, careful hands',
        N'dark auburn', N'short bob', N'short',
        N'hazel', N'medium fair', N'clear',
        N'none',
        N'Very steady normally; slightly over-controlled when her hands are being watched.',
        N'Dark linen physician''s coat, always buttoned, hair pinned severely back.',
        N'none',
        N'Morning rounds; laboratory work; afternoon consultations; monthly health reports for the Lord; evening compound distillation.',
        N'Self-prescribes three grains of somnifer nightly; her hands shake before the first dose; managing this two years.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Estate infirmary and laboratory; patient chambers as required.',
        N'0', N'0',
        N'Dark-haired woman in physician''s coat examining herbs stone room medieval setting',
        N'Woman in dark coat in stone herb room medieval manor',
        0, 0
    );
    PRINT 'Clemence Aubert seeded.';
END
ELSE PRINT 'Clemence Aubert already exists.';
GO

-- ── Renaud Chabrier ───────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Renaud Chabrier')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Renaud Chabrier', N'renaud-chabrier', N'canon', 1,
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
        @id, N'Renaud Chabrier', N'renaud-chabrier', N'Renaud', N'Chabrier', N'',
        N'human', N'human', N'male', N'he/him', 57, N'alive',
        N'Chaplain and Bheur officiant; estate chapel; thirty years administering last rites.',
        N'Renaud has administered last rites and conducted seasonal observances for thirty years. He does it with care and without conviction. The dying find him comforting. He finds their comfort disturbing. He has never spoken of this to anyone and does not intend to.',
        N'Spiritual void at the house''s center; his private unbelief frames the Bheur as unknowable rather than reassuring.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        173, 75, N'medium, slightly stooped',
        N'grey-white', N'tonsured', N'very short',
        N'watery blue', N'pale', N'age-spotted',
        N'none',
        N'Gentle, unhurried; hands folded; makes eye contact with the dying but not the living.',
        N'Grey-white robes, Bheur medallion worn face-inward against his chest.',
        N'none',
        N'Morning observance; pastoral visits to staff; records estate deaths and births; assists Physician at deathbeds; evening private study.',
        N'Has not believed in Bheur since performing last rites over his own teacher thirty years ago; fears he damns those he tends.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Estate chapel and burial ground; staff quarters.',
        N'0', N'0',
        N'Older tonsured priest in grey robes performing rites candlelit stone chapel medieval estate',
        N'Old priest in grey robes stone chapel candlelight medieval manor',
        0, 0
    );
    PRINT 'Renaud Chabrier seeded.';
END
ELSE PRINT 'Renaud Chabrier already exists.';
GO

-- ── Adelais Garnet ────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Adelais Garnet')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Adelais Garnet', N'adelais-garnet', N'canon', 1,
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
        @id, N'Adelais Garnet', N'adelais-garnet', N'Adelais', N'Garnet', N'Mistress',
        N'human', N'human', N'female', N'she/her', 44, N'alive',
        N'Laundry Master; twenty-two years; reads every pocket before washing — and reads what she finds.',
        N'Adelais supervises the laundry operation and has developed, over twenty-two years, the habit of checking every pocket before washing. She is thorough, discreet, and has learned more about the house''s actual business from discarded slips of paper than most staff will ever know.',
        N'Accidental intelligence archive; the document she found creates a structural threat to Thibaut and potentially the House.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        160, 72, N'strong arms, sturdy',
        N'dark blonde', N'perpetually damp', N'medium',
        N'pale grey', N'fair', N'reddened from steam',
        N'none',
        N'Arms always occupied; stands wide; speaks without stopping work.',
        N'Heavy linen apron, plain dress beneath, sleeves permanently rolled past elbows.',
        N'none',
        N'Pre-dawn sorting; supervised wash cycles; personal pocket-check protocol never delegated; afternoon mending; drying-room inspection at dusk.',
        N'Found Thibaut Gervais''s letter confirming he sells Scrying schedules to House Renalt; keeps it pressed in her prayer book.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Laundry hall and drying courts; does not leave the estate.',
        N'0', N'0',
        N'Sturdy woman sorting laundry steam-filled stone washroom medieval manor focused expression',
        N'Woman sorting laundry steam-filled stone washroom medieval manor',
        0, 0
    );
    PRINT 'Adelais Garnet seeded.';
END
ELSE PRINT 'Adelais Garnet already exists.';
GO

-- ── Isabeau Duroc ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Isabeau Duroc')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Isabeau Duroc', N'isabeau-duroc', N'canon', 1,
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
        @id, N'Isabeau Duroc', N'isabeau-duroc', N'Isabeau', N'Duroc', N'Dame',
        N'human', N'human', N'female', N'she/her', 35, N'alive',
        N'Head of Household Guards; thirty-person force; Knight-level Transmuted.',
        N'Isabeau commands thirty household guards with the efficiency of someone who has survived things most of her guards never face. She is not cruel but sets a standard for force she expects her people to reach and her charges never to provoke.',
        N'Authorized force with an unauthorized origin; her loyalty is to a private debt, not the House hierarchy.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        178, 84, N'powerful, athletic',
        N'black', N'cropped short', N'very short',
        N'dark brown', N'deep brown', N'clear',
        N'Subtle height gain (Knight)',
        N'Controlled power; never wastes motion; stillness that communicates threat without display.',
        N'House guard livery reinforced at joints, sword always present, no concession to decoration.',
        N'Knight-level Transmutation infusion; sponsored outside House Atrament authority; enhanced strength and recovery.',
        N'Dawn guard muster; security rotation assignments; threat assessment briefings; evening guard training; reports to Seneschal, not the Lord.',
        N'Took Knight infusion sponsored by Pardoner Merevin Solat outside House Atrament authority; owes him an unspecified debt.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Full estate perimeter; all entry points and guard posts.',
        N'0', N'0',
        N'Powerful dark-skinned woman in reinforced guard armor stone medieval courtyard commanding bearing',
        N'Powerful woman in guard armor stone courtyard medieval manor',
        0, 0
    );
    PRINT 'Isabeau Duroc seeded.';
END
ELSE PRINT 'Isabeau Duroc already exists.';
GO

-- ── Mathilde Soragne ──────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Mathilde Soragne')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Mathilde Soragne', N'mathilde-soragne', N'canon', 1,
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
        @id, N'Mathilde Soragne', N'mathilde-soragne', N'Mathilde', N'Soragne', N'',
        N'human', N'human', N'female', N'she/her', 37, N'alive',
        N'Former House Atrament intelligence officer; expelled; used for deniable work; Oathless-adjacent.',
        N'Mathilde was expelled from House Atrament seven years ago after burning a dossier the House intended to use to destroy a vine-country family she had come to know during an assignment. The House uses her for deniable work and never mentions the expulsion.',
        N'Moral counterweight to the intelligence apparatus; the operative who refused one order and still does the work on her own terms.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        169, 65, N'lean, careful',
        N'dark brown', N'worn loose', N'shoulder-length',
        N'dark green', N'olive', N'clear',
        N'none',
        N'Relaxed alertness; changes her gait depending on who is watching.',
        N'Traveling clothes in neutral tones, no house markings, always a cloak.',
        N'none',
        N'No fixed schedule; enters through the south gatehouse when summoned; otherwise operates in the vine towns and trade roads.',
        N'Still holds copies of three Atrament dossiers from her service years; has never used them and has not destroyed them.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Vine towns and trade roads; estate south gatehouse by arrangement.',
        N'0', N'0',
        N'Lean dark-haired woman in traveling cloak medieval vine country road watchful expression',
        N'Woman in traveling cloak on medieval country road watchful',
        0, 0
    );
    PRINT 'Mathilde Soragne seeded.';
END
ELSE PRINT 'Mathilde Soragne already exists.';
GO

-- ── Peire Loubens ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Peire Loubens')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Peire Loubens', N'peire-loubens', N'canon', 1,
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
        @id, N'Peire Loubens', N'peire-loubens', N'Peire', N'Loubens', N'',
        N'human', N'human', N'male', N'he/him', 31, N'alive',
        N'Oathless; outer vineyard laborer under a false name; former Scrying operator.',
        N'Peire works the outer vineyard under a false name, known to the groundskeeper and two guards and no one else on the estate. He is useful and undemanding, which is the only politics available to him. What he knows about Scrying apparatus would interest Atrament considerably.',
        N'Oathless with insider technical knowledge; his presence is a liability for Atrament and an asset they do not know they have.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        174, 73, N'lean, calloused hands',
        N'dark, shaggy', N'unkempt', N'medium',
        N'amber-brown', N'tanned, medium', N'weathered',
        N'none',
        N'Stoops deliberately; affects a laborer''s gait; drops it entirely when alone.',
        N'Rough linen worker''s clothes, worn hat, nothing identifying.',
        N'none',
        N'Vineyard labor at dawn; avoids the main estate buildings; eats alone; watches the Scrying tower''s light patterns from the hill.',
        N'Former House Miraud Scrying operator; defected three years ago; carries memorized calibration sequences for Sphere 19 that Atrament has never accessed.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Outer vineyard only; avoids the main estate.',
        N'0', N'0',
        N'Lean dark-haired man in rough worker clothes in vineyard medieval countryside watchful expression',
        N'Lean man in rough clothes in vineyard medieval countryside watchful',
        0, 0
    );
    PRINT 'Peire Loubens seeded.';
END
ELSE PRINT 'Peire Loubens already exists.';
GO
