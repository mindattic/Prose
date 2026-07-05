SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ============================================================
-- HOUSE ATRAMENT — UPPER HIERARCHY SEED (PART A)
-- The Cauld | UniverseId: 0197E9C9-0002-7000-8000-000000000002
-- Generated: 2026-07-05
-- Ruling Family (7) + Political Cabinet (6) + Military Command (7) = 20 total
-- Vine country, geographic center of continent, France analog.
-- Best intelligence apparatus in the Cauld. Medium military.
-- ============================================================

-- ============================================================
-- RULING FAMILY
-- ============================================================

-- 1. Lord Renaud Colbert — Lord of House Atrament
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Renaud Colbert')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Renaud Colbert', N'renaud-colbert', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns,
        Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage,
        HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure,
        SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Renaud Colbert', N'renaud-colbert', N'Renaud', N'Colbert', N'Lord',
        N'human', N'human', N'male', N'he/him', 58, N'alive',
        N'Lord of House Atrament',
        N'Renaud built Atrament''s intelligence network from scattered informants into the finest apparatus on the continent over thirty years. Charming, patient, and genuinely dangerous. He runs every audience as though he already knows the outcome. Believes information is the only currency that compounds without losing value. His sister''s death is the wound the whole apparatus was built to prevent recurring.',
        N'Patriarch whose intelligence web is Atrament''s greatest weapon and the source of its oldest, most carefully buried guilt.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        178, 82, N'wiry, lean; the build of a man whose danger was never physical',
        N'silver-streaked brown', N'close-cropped, precise', N'short',
        N'pale grey', N'olive', N'weathered; fine lines at the eyes and mouth',
        N'none',
        N'Deliberate stillness; gestures rare; always seated facing the room''s entrance.',
        N'Charcoal velvet, ink-dark lining, House seal ring — no other ornament.',
        N'none',
        N'Morning intelligence briefings; afternoon audiences designed to elicit information; evening cipher review with the Spymaster.',
        N'He sent Adelais on a mission he knew would likely kill her and has never told Isabeau, Gilles, or Gautier.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Atrament estate; all Houses except Lacerta; Sphere 31 intake contacts',
        N'0', N'0',
        N'Medieval French lord, silver-streaked brown hair, olive skin, grey eyes, charcoal velvet, vineyard estate, steampunk era',
        N'Medieval French lord, silver hair, olive skin, charcoal velvet, steampunk',
        0, 0
    );
    PRINT 'Renaud Colbert seeded.';
END
ELSE PRINT 'Renaud Colbert already exists.';
GO

-- 2. Lady Isabeau Colbert — Spouse of the Lord
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Isabeau Colbert')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Isabeau Colbert', N'isabeau-colbert', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns,
        Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage,
        HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure,
        SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Isabeau Colbert', N'isabeau-colbert', N'Isabeau', N'Colbert', N'Lady',
        N'human', N'human', N'female', N'she/her', 52, N'alive',
        N'Lady of House Atrament; co-architect of court intelligence',
        N'Isabeau manages Atrament''s social architecture — the salons, the seating arrangements, the meetings that appear accidental and yield intelligence. She is the smile that makes the observation comfortable. Her husband trusts her operational instincts more than any formal advisor''s. She receives her own separate intelligence reports and discusses them with no one except Renaud, and not always with him.',
        N'Soft power counterpart; runs the estate''s social life as an intelligence instrument with methods her husband formally cannot employ.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        165, 61, N'slight, precise',
        N'dark auburn', N'pinned chignon, elegant', N'medium',
        N'amber', N'warm beige', N'fine; reads younger than her age',
        N'none',
        N'Unhurried grace; moves through rooms as if she designed the layout.',
        N'Burgundy silk, warm gold trim, understated jewelry; always practically comfortable.',
        N'none',
        N'Curating guest lists, hosting salons, reading social dynamics; receives private intelligence reports; coaches household staff on observation.',
        N'She knows Renaud sacrificed Adelais knowingly; she has chosen to protect him from disclosure rather than demand explanation.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Atrament estate; formal social circuit across allied Houses',
        N'0', N'0',
        N'Medieval French noblewoman, dark auburn hair, amber eyes, warm beige skin, burgundy silk, vineyard estate salon, steampunk',
        N'Medieval French noblewoman, auburn hair, amber eyes, burgundy silk, steampunk salon',
        0, 0
    );
    PRINT 'Isabeau Colbert seeded.';
END
ELSE PRINT 'Isabeau Colbert already exists.';
GO

-- 3. Gautier Colbert — Heir to House Atrament
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Gautier Colbert')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Gautier Colbert', N'gautier-colbert', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns,
        Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage,
        HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure,
        SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Gautier Colbert', N'gautier-colbert', N'Gautier', N'Colbert', N'',
        N'human', N'human', N'male', N'he/him', 29, N'alive',
        N'Heir to House Atrament; Knight',
        N'Gautier survived his Catalyst infusion and uses his Knight''s frame as proof of fitness to rule. Brilliant at tactics, impatient with slow intelligence work. He wants to leverage Atrament''s network for action; his father wants to hold information as passive leverage. The gap between their approaches is widening into something that will need resolving before the succession.',
        N'Heir whose impatience with patient intelligence work threatens the equilibrium his father built over decades.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        185, 90, N'athletic, broad-shouldered; the Knight''s height worn comfortably',
        N'dark brown', N'loose, slightly unkempt', N'short',
        N'grey-green', N'olive', N'clear, angular; the youngest-looking Colbert by a decade',
        N'Subtle height gain (Knight)',
        N'Forward-leaning, fills space with restless energy; rarely sits; paces when thinking.',
        N'Military cut in deep green, Knight''s insignia, practical and well-maintained.',
        N'Single Catalyst infusion; Knight-grade Transmutation: slight height gain, skeletal density increase, accelerated wound closure. Infusion point at right wrist.',
        N'Corps exercises, strategy reviews, lobbying his father to convert intelligence holdings into active military strikes.',
        N'He suspects Renaud deliberately sent Adelais to her death; he has begun asking the Archivist questions that are not being answered.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Atrament estate; Corps training grounds; two allied House territories',
        N'0', N'0',
        N'Young medieval French knight, dark brown hair, grey-green eyes, olive skin, deep green military coat, steampunk estate',
        N'Young French knight, dark hair, grey-green eyes, military green, steampunk',
        0, 0
    );
    PRINT 'Gautier Colbert seeded.';
END
ELSE PRINT 'Gautier Colbert already exists.';
GO

-- 4. Blanche Colbert — Second Born
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Blanche Colbert')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Blanche Colbert', N'blanche-colbert', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns,
        Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage,
        HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure,
        SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Blanche Colbert', N'blanche-colbert', N'Blanche', N'Colbert', N'',
        N'human', N'human', N'female', N'she/her', 25, N'alive',
        N'Second Born; Archive analyst; refused transmutation',
        N'Blanche refused transmutation and chose the Archive by calculation, not cowardice. She has an extraordinary memory and reads cipher patterns that senior analysts miss. Underestimated constantly — by her father, by her brother, by everyone who sees the refusal first and the ability second. She is the most capable intelligence mind in the House and the only one without formal authority.',
        N'Overlooked second child who understands the intelligence apparatus better than anyone who holds formal power over it.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        162, 55, N'lean, slight; moves like someone who has learned to be overlooked',
        N'light brown', N'loose braid', N'long',
        N'pale amber', N'fair olive', N'clear, ink-smudged at the temples',
        N'none',
        N'Quick, small movements; habitually positions near exits; habit of noting all faces.',
        N'Plain linen, ink-stained fingers, no jewelry; deliberate plainness as social invisibility.',
        N'none',
        N'Archive cipher work, writing correspondence for her father, refusing social obligations, teaching herself languages from intercepted dispatches.',
        N'She declined the Catalyst after cross-referencing Atrament''s own mortality records and calculating an 81% death rate in the archive.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Archive wing, Atrament estate; rarely leaves the grounds willingly',
        N'0', N'0',
        N'Young medieval French woman, light brown braided hair, pale amber eyes, fair skin, plain linen, archive candlelight, steampunk',
        N'Young French noblewoman, plain linen, ink-stained, archive, steampunk candlelight',
        0, 0
    );
    PRINT 'Blanche Colbert seeded.';
END
ELSE PRINT 'Blanche Colbert already exists.';
GO

-- 5. Ermengarde Colbert — Dowager; network founder
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ermengarde Colbert')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ermengarde Colbert', N'ermengarde-colbert', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns,
        Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage,
        HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure,
        SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Ermengarde Colbert', N'ermengarde-colbert', N'Ermengarde', N'Colbert', N'Lady',
        N'human', N'human', N'female', N'she/her', 78, N'alive',
        N'Dowager; founder of the intelligence network; officially retired',
        N'Ermengarde built Atrament''s first systematic intelligence network forty years ago and handed it to her son improved. Now officially retired. She holds three informants Renaud does not know about and meets the Spymaster privately each week. She does not advise unless asked; she does not stop shaping outcomes. The House runs on her original architecture.',
        N'The network''s true architect; retired in name only; her shadow falls over every major decision still made in Atrament.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        160, 67, N'stout, settled; authority carried in stillness rather than size',
        N'white', N'pinned severely', N'short',
        N'dark grey', N'pale', N'deeply lined; seventy-eight years of watchfulness in the face',
        N'none',
        N'Slow and deliberate; never hurries; occupies rooms as though she owns the silence.',
        N'Severe black wool, House seal ring, no concession to fashion or occasion.',
        N'none',
        N'Morning walks, private correspondence, receiving old contacts; interfering in matters her son believes are already settled.',
        N'She ordered the assassination of Renaud''s first betrothed to secure the Fontaine match; Isabeau has never been told.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Atrament estate; private correspondence reaches further than anyone suspects',
        N'0', N'0',
        N'Elderly medieval French noblewoman, white pinned hair, dark grey eyes, pale skin, severe black wool, vineyard estate, steampunk',
        N'Elderly French noblewoman, white hair, severe black wool, steampunk estate interior',
        0, 0
    );
    PRINT 'Ermengarde Colbert seeded.';
END
ELSE PRINT 'Ermengarde Colbert already exists.';
GO

-- 6. Thibaut Vautrel — Cousin; Controller of the Northern Intelligence Hub
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Thibaut Vautrel')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Thibaut Vautrel', N'thibaut-vautrel', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns,
        Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage,
        HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure,
        SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Thibaut Vautrel', N'thibaut-vautrel', N'Thibaut', N'Vautrel', N'Master',
        N'human', N'human', N'male', N'he/him', 44, N'alive',
        N'Cousin of Lord Renaud; Controller of the Northern Intelligence Hub',
        N'Thibaut runs Atrament''s northern intelligence hub from a vineyard that functions as a courier way-station. He is Renaud''s cousin and the only family member who manages field operations directly. Loyal, blunt, occasionally brilliant, chronically undervalued. He travels under false identities and has not slept in his own bed three consecutive nights in five years.',
        N'Field intelligence anchor keeping the northern network functional while the estate politicizes the information it produces.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        175, 79, N'stocky, practical; a man whose body is a working tool',
        N'sandy brown', N'close-cropped, receding', N'short',
        N'hazel', N'ruddy, weathered', N'sun-scored; the face of someone who works outdoors under a false name',
        N'none',
        N'Practical, efficient; moves like someone always calculating the nearest exit.',
        N'Traveling merchant''s wool, unremarkable and dusty; nothing that invites second looks.',
        N'none',
        N'Courier coordination, agent briefings, wine production as cover, frequent travel under assumed names and documentation.',
        N'He has sold small, carefully misdirecting intelligence to House Lacerta for two years in exchange for private income.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Northern intelligence corridor; three allied House territories; Oathless borderlands',
        N'0', N'0',
        N'Middle-aged French merchant-spy, sandy brown hair, hazel eyes, ruddy skin, worn wool, vineyard road, steampunk countryside',
        N'Middle-aged French spy disguised as merchant, worn wool, steampunk countryside road',
        0, 0
    );
    PRINT 'Thibaut Vautrel seeded.';
END
ELSE PRINT 'Thibaut Vautrel already exists.';
GO

-- 7. Adelais Colbert — Deceased; sister of Lord Renaud; her legacy drives the House
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Adelais Colbert')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Adelais Colbert', N'adelais-colbert', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns,
        Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage,
        HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure,
        SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Adelais Colbert', N'adelais-colbert', N'Adelais', N'Colbert', N'Lady',
        N'human', N'human', N'female', N'she/her', 54, N'dead',
        N'Deceased; former field operative; sister of Lord Renaud; died 22 years ago',
        N'Atrament''s finest field operative in her generation and Renaud''s closest confidant. Died on a mission her brother assigned knowing the odds were against her. Her portrait hangs in three rooms. Methods codified into training doctrine the year after her death. Gautier grew up hearing her name as excellence; he has begun to wonder about the circumstances.',
        N'The absent center of family guilt; her death explains every security excess Renaud maintains and drives Gautier''s suspicion.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        170, 64, N'athletic; remembered as quick and decisive in all accounts',
        N'dark brown', N'worn long in the field', N'long',
        N'grey', N'olive', N'clear, angular; the Colbert cast',
        N'Subtle height gain (Knight)',
        N'Remembered as quick and decisive; the portraits show a woman who never seemed uncertain.',
        N'Remembered in field grey; Knight''s insignia; practical dress remembered as elegant.',
        N'Single Catalyst infusion; Knight-grade Transmutation: slight height gain, skeletal density increase. Infusion point at left wrist.',
        N'Legacy sustained through doctrine codified from her methods; her absence is the active presence shaping the House.',
        N'She knew Renaud had assigned her to die; she went anyway to give him plausible deniability and protect his rule.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Former range: continental field operations across all Houses',
        N'0', N'0',
        N'Medieval French female knight, dark brown hair, grey eyes, olive skin, field grey, portrait style, steampunk',
        N'Medieval French female knight, dark hair, grey eyes, field grey, steampunk portrait',
        0, 0
    );
    PRINT 'Adelais Colbert seeded.';
END
ELSE PRINT 'Adelais Colbert already exists.';
GO

-- ============================================================
-- POLITICAL CABINET
-- ============================================================

-- 8. Marguerite Deschamps — Chancellor
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Marguerite Deschamps')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Marguerite Deschamps', N'marguerite-deschamps', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns,
        Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage,
        HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure,
        SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Marguerite Deschamps', N'marguerite-deschamps', N'Marguerite', N'Deschamps', N'Mistress',
        N'human', N'human', N'female', N'she/her', 61, N'alive',
        N'Chancellor of House Atrament',
        N'Marguerite runs Atrament''s internal administration with siege-engineer precision. She controls access to Lord Renaud, manages the House legal apparatus, and is the only person who tells him no to his face. She is not liked. She does not require it. Her ledgers are accurate to the coin and her scheduling has not misfired in eleven years of service.',
        N'Administrative power behind the Lord; the friction that makes Atrament''s decisions survive contact with reality.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        167, 72, N'solid, upright; authority carried in posture',
        N'silver', N'severe chignon', N'medium',
        N'brown', N'dark brown', N'smooth, composed; a face that reveals nothing in audience',
        N'none',
        N'Upright and measured; occupies exactly the space required, never more.',
        N'Structured black wool, Chancellor''s seal chain, severe chignon, no ornamentation.',
        N'none',
        N'Morning petitions, legal disputes, household ledgers; controls the Lord''s schedule; reviews all formal correspondence before dispatch.',
        N'She is compiling a complete operational history of Atrament''s intelligence apparatus, intending to publish it after her death.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Atrament estate; legal and administrative circuit across the central continent',
        N'0', N'0',
        N'Older French noblewoman, silver chignon, dark brown skin, brown eyes, structured black wool, Chancellor seal chain, steampunk formal office',
        N'French Chancellor, silver chignon, dark brown skin, black wool, steampunk formal office',
        0, 0
    );
    PRINT 'Marguerite Deschamps seeded.';
END
ELSE PRINT 'Marguerite Deschamps already exists.';
GO

-- 9. Gilles Sennac — Spymaster
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Gilles Sennac')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Gilles Sennac', N'gilles-sennac', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns,
        Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage,
        HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure,
        SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Gilles Sennac', N'gilles-sennac', N'Gilles', N'Sennac', N'Master',
        N'human', N'human', N'male', N'he/him', 55, N'alive',
        N'Spymaster of House Atrament',
        N'Gilles has built a secondary network within Atrament''s apparatus to monitor persons taken from Sphere 31 for domestic and military service. He tracks their loyalty, contacts, and grief. He frames this as compassionate welfare — they are disoriented; he keeps them known. He is the only cabinet member who understands the Liturgy operations in operational rather than diplomatic terms.',
        N'The moral weight of Atrament''s information apparatus; his Sphere 31 surveillance makes the ethical question concrete and unavoidable.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        172, 74, N'lean, deliberately unmemorable in bearing and frame',
        N'black going grey', N'close-cropped', N'short',
        N'dark brown', N'brown', N'unremarkable; a face engineered to escape description',
        N'none',
        N'Effaces himself; enters rooms last, leaves first; never the center of any conversation.',
        N'Plain dark wool, no insignia, nothing that invites identification or memory.',
        N'none',
        N'Cipher analysis, handler debriefs, Sphere 31 intake interviews, private correspondence with informants across five Houses.',
        N'He has filed documented proof that three Sphere 31 persons were killed to silence operational liabilities; he has not acted on it.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Atrament estate; handler network across five Houses; Sphere 31 processing facilities',
        N'0', N'0',
        N'Middle-aged French spymaster, close-cropped greying black hair, dark brown eyes, brown skin, plain dark wool, candlelit cipher chamber, steampunk',
        N'French spymaster, greying dark hair, brown skin, plain dark wool, candlelit steampunk chamber',
        0, 0
    );
    PRINT 'Gilles Sennac seeded.';
END
ELSE PRINT 'Gilles Sennac already exists.';
GO

-- 10. Etienne Beaumont — Archivist
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Etienne Beaumont')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Etienne Beaumont', N'etienne-beaumont', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns,
        Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage,
        HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure,
        SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Etienne Beaumont', N'etienne-beaumont', N'Etienne', N'Beaumont', N'Master',
        N'human', N'human', N'male', N'he/him', 47, N'alive',
        N'Archivist of House Atrament',
        N'Etienne maintains the physical intelligence archive — forty years of intercepted correspondence, decoded ciphers, and handler reports. He knows where every secret is buried because he indexed it. Dry, precise, and quietly contemptuous of the people whose failures he catalogs. He has read more of Atrament''s history than anyone living, including the Lord.',
        N'The archive that makes Atrament dangerous; his knowledge outlasts any operative''s usefulness or anyone''s interest in concealing the past.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        168, 69, N'soft, stooped; a body shaped by decades of desk work',
        N'light brown, thinning', N'pushed back, careless', N'short',
        N'blue-grey', N'pale', N'ink-smudged at the temples; fine lines from reading in poor light',
        N'none',
        N'Stooped forward; moves through the archive as if navigating familiar water.',
        N'Ink-stained grey wool, reading spectacles, chalk dust; deliberately unimpressive.',
        N'none',
        N'Filing, cross-referencing, training junior archivists, cataloguing incoming intelligence; rarely leaves the archive wing before dark.',
        N'He knows Ermengarde ordered the assassination of Renaud''s first betrothed; he cross-referenced it last year and said nothing.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Archive wing, Atrament estate; administrative access to all House records',
        N'0', N'0',
        N'Middle-aged French archivist, thin light brown hair, blue-grey eyes, pale skin, ink-stained grey wool, reading spectacles, steampunk archive by candlelight',
        N'French archivist, pale skin, ink-stained grey wool, reading spectacles, steampunk archive',
        0, 0
    );
    PRINT 'Etienne Beaumont seeded.';
END
ELSE PRINT 'Etienne Beaumont already exists.';
GO

-- 11. Alienor Morel — Trade Ambassador
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Alienor Morel')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Alienor Morel', N'alienor-morel', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns,
        Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage,
        HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure,
        SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Alienor Morel', N'alienor-morel', N'Alienor', N'Morel', N'Mistress',
        N'human', N'human', N'female', N'she/her', 48, N'alive',
        N'Trade Ambassador of House Atrament; intelligence collector under commercial cover',
        N'Alienor negotiates Atrament''s trade agreements across three Houses and two Oathless borderlands, collecting intelligence under commercial cover. She speaks four dialects, reads contracts for the clauses that weren''t meant to be found, and returns home with more information than goods. She is the public face of a covert operation that no trade partner has identified.',
        N'Trade network as intelligence infrastructure; Alienor is both the instrument and the mask, the merchant and the handler.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        171, 66, N'medium, adaptable; a body that reads differently in every room',
        N'chestnut', N'loose curls, travel-worn', N'medium',
        N'green', N'warm tan', N'fine, travel-weathered; the look of someone always between destinations',
        N'none',
        N'Fluid, socially adaptive; adjusts bearing and register precisely to each room she enters.',
        N'Travel-worn quality cloth; regional dress variations deployed as diplomatic signal.',
        N'none',
        N'Diplomatic correspondence, market visits, coaching delegates, filing separate intelligence reports to Gilles; rarely in the same city two weeks running.',
        N'She is privately negotiating with an Oathless enclave her Lord does not know exists, using her own funds and contacts.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Trade circuits across three Houses and two Oathless borderlands; continental travel range',
        N'0', N'0',
        N'Middle-aged French trade ambassador, chestnut curls, green eyes, warm tan skin, regional merchant dress, steampunk market town',
        N'French trade ambassador, chestnut hair, green eyes, merchant dress, steampunk market',
        0, 0
    );
    PRINT 'Alienor Morel seeded.';
END
ELSE PRINT 'Alienor Morel already exists.';
GO

-- 12. Hugues Ferrand — Liturgy Liaison
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Hugues Ferrand')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Hugues Ferrand', N'hugues-ferrand', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns,
        Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage,
        HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure,
        SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Hugues Ferrand', N'hugues-ferrand', N'Hugues', N'Ferrand', N'Master',
        N'human', N'human', N'male', N'he/him', 50, N'alive',
        N'Liturgy Liaison of House Atrament; Sphere 31 intake coordinator',
        N'Hugues manages Atrament''s relationship with the Liturgy — negotiating which persons taken from Sphere 31 are assigned to the House and for what purposes. He is not unkind to the persons themselves. He is not honest about what happens when the Spymaster decides they are a liability. He manages this gap by never holding both facts at once.',
        N'The bureaucratic face of Sphere 31 exploitation; his practiced normalcy makes the apparatus''s worst function invisible even to himself.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        176, 83, N'heavy, thick through the chest; physical presence used as reassurance',
        N'grey-brown', N'sparse, short', N'short',
        N'pale blue', N'ruddy', N'florid, well-fed; the face of someone who has never had to run',
        N'none',
        N'Slow, deliberate; uses physical presence as a tool of reassurance and comfort.',
        N'Formal burgundy wool, Liturgy liaison pin, well-pressed; projects trustworthiness.',
        N'none',
        N'Liturgy correspondence, intake processing of Sphere 31 persons, scheduling Liturgy audiences, drafting assignment requests.',
        N'He has been skimming Liturgy placement fees for six years, attributing the amounts to administrative overhead in unsigned ledger entries.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Atrament estate; Liturgy processing facilities; Sphere 31 transit points',
        N'0', N'0',
        N'Middle-aged French official, grey-brown hair, pale blue eyes, ruddy skin, formal burgundy wool, Liturgy insignia, steampunk chancellery hall',
        N'French Liturgy official, grey hair, ruddy skin, burgundy wool, steampunk hall',
        0, 0
    );
    PRINT 'Hugues Ferrand seeded.';
END
ELSE PRINT 'Hugues Ferrand already exists.';
GO

-- 13. Raoul Bressac — Treasurer
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Raoul Bressac')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Raoul Bressac', N'raoul-bressac', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns,
        Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage,
        HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure,
        SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Raoul Bressac', N'raoul-bressac', N'Raoul', N'Bressac', N'Master',
        N'human', N'human', N'male', N'he/him', 53, N'alive',
        N'Treasurer of House Atrament',
        N'Raoul controls Atrament''s finances with the precision Gilles applies to intelligence. He pays informants, funds covert operations, and maintains the fiction that vineyard income alone accounts for the House''s wealth. The real funding sources are known only to him and the Lord. He has never been wrong about a number. He has never been entirely honest about one.',
        N'Financial architecture of the intelligence apparatus; the man who knows what every secret operation actually costs and who actually pays.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        173, 87, N'portly, restless; a body that never quite settles',
        N'dark brown, thinning', N'combed back, thin', N'short',
        N'brown', N'olive', N'sallow, ink-stained at the fingers; the look of a man whose work never stops',
        N'none',
        N'Fidgety hands; constantly calculating; rarely fully still even in formal audience.',
        N'Conservative dark wool, ink and chalk marks, treasurer''s chain of office.',
        N'none',
        N'Ledger review, payment scheduling for the intelligence network, quarterly audits, arguing with Gilles about operational expenditure.',
        N'He has skimmed Catalyst program funding to invest privately in Sphere 31 trade goods, anticipating House economy collapse.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Atrament estate; financial circuits and credit houses across the central continent',
        N'0', N'0',
        N'Middle-aged French treasurer, thinning dark hair, brown eyes, olive skin, dark wool, treasurer chain, ledger room steampunk candlelight',
        N'French treasurer, dark wool, treasurer chain, ledgers, steampunk candlelight',
        0, 0
    );
    PRINT 'Raoul Bressac seeded.';
END
ELSE PRINT 'Raoul Bressac already exists.';
GO

-- ============================================================
-- MILITARY COMMAND
-- ============================================================

-- 14. Clemence Vidal — Commander of the Myrmidon Corps (Paladin)
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Clemence Vidal')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Clemence Vidal', N'clemence-vidal', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns,
        Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage,
        HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure,
        SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Clemence Vidal', N'clemence-vidal', N'Clemence', N'Vidal', N'Dame',
        N'human', N'human', N'female', N'she/her', 44, N'alive',
        N'Commander of the Myrmidon Corps; Paladin',
        N'Clemence survived two Catalyst infusions and commands the Myrmidon Corps with the calm of someone who has decided death holds no particular terror. She does not plan intelligence operations. She executes them. The distinction matters to her. She is Paladin-grade, post-human in density and force, and the most physically dangerous person in any room she enters.',
        N'Military instrument of Atrament''s intelligence apparatus; the limit case of what the House can authorize when patience fails.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        195, 102, N'powerful, post-human; the Paladin''s density settled into something that reads as geological',
        N'black', N'close-cropped, severe', N'short',
        N'dark brown', N'dark brown', N'smooth, unmarked; the Transmutation heals everything except the infusion points',
        N'Evident enhancement (Paladin)',
        N'Utterly still at rest; explosive in motion; every movement has a specific purpose.',
        N'Corps black plate, Commander''s mark, no ornamentation beyond rank insignia.',
        N'Two Catalyst infusions; Paladin-grade Transmutation: significant height and density increase, accelerated recovery, enhanced strength. Infusion points at both wrists, scarred and worn.',
        N'Corps inspections, tactical planning, Paladin conditioning exercises, briefings from Gilles on prospective targets.',
        N'She has refused a third infusion; she knows another attempt will very likely kill her and has told no one in the Corps.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Atrament estate; all Corps deployment zones; intelligence target locations',
        N'0', N'0',
        N'Tall powerful medieval French female Paladin commander, black close-cropped hair, dark brown eyes, dark brown skin, black plate armor, Corps insignia, steampunk fortress',
        N'Female Paladin commander, black armor, dark skin, black hair, imposing, steampunk fortress',
        0, 0
    );
    PRINT 'Clemence Vidal seeded.';
END
ELSE PRINT 'Clemence Vidal already exists.';
GO

-- 15. Bertrand Arnal — First Captain (Knight)
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Bertrand Arnal')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Bertrand Arnal', N'bertrand-arnal', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns,
        Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage,
        HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure,
        SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Bertrand Arnal', N'bertrand-arnal', N'Bertrand', N'Arnal', N'',
        N'human', N'human', N'male', N'he/him', 38, N'alive',
        N'First Captain of the Myrmidon Corps; Knight',
        N'Bertrand has commanded Atrament''s field strike teams for eight years and carries seventeen confirmed intelligence-ordered operations. Methodical, rarely surprised, genuinely fond of his soldiers. He is the only officer who regularly tells Commander Clemence when a plan is bad, and the only one who has never been wrong in doing so. She listens.',
        N'The human cost of intelligence-ordered violence; Bertrand is its most capable agent and its most reluctant conscience.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        185, 92, N'broad, methodical; the Knight''s frame worn with eight years of field use',
        N'dark auburn', N'close-cropped', N'short',
        N'amber', N'warm brown', N'weathered, a thin scar along the left jaw from a field operation five years prior',
        N'Subtle height gain (Knight)',
        N'Wide-based stance, moves slow and sure; never found surprised in position.',
        N'Corps black, First Captain''s mark, practical and broken in from consistent use.',
        N'Single Catalyst infusion; Knight-grade Transmutation: slight height gain, skeletal density increase, accelerated wound closure. Infusion point at right wrist.',
        N'Team drills, mission planning, post-operation debriefs with Gilles, evening rounds with the Corps soldiers.',
        N'He keeps a private ledger of every person killed under intelligence orders — names, not target designations, going back eight years.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Atrament estate; all field operation zones; full Corps deployment range',
        N'0', N'0',
        N'Medieval French male knight, dark auburn hair, amber eyes, warm brown skin, black corps coat, First Captain insignia, steampunk military compound',
        N'French first captain knight, auburn hair, warm brown skin, corps black coat, steampunk',
        0, 0
    );
    PRINT 'Bertrand Arnal seeded.';
END
ELSE PRINT 'Bertrand Arnal already exists.';
GO

-- 16. Yseult Chamond — Second Captain (Knight)
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Yseult Chamond')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Yseult Chamond', N'yseult-chamond', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns,
        Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage,
        HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure,
        SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Yseult Chamond', N'yseult-chamond', N'Yseult', N'Chamond', N'',
        N'human', N'human', N'female', N'she/her', 31, N'alive',
        N'Second Captain of the Myrmidon Corps; Knight',
        N'Yseult routed an Oathless ambush with nine soldiers against thirty, using terrain and silence rather than direct engagement. Commander Clemence promoted her on the field. She is brilliant, competitive, and not yet seasoned enough to know where boldness becomes liability. First Captain Bertrand is watching her with the attention of someone who already knows where that line is.',
        N'Rising military talent whose aggressive brilliance needs shaping before it becomes the kind of liability it does not recognize itself as.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        183, 80, N'athletic, light; the Knight''s frame on a body built for speed over mass',
        N'auburn-red', N'short, practical', N'short',
        N'green', N'fair', N'freckled, outdoors-weathered; a face that is always slightly in motion',
        N'Subtle height gain (Knight)',
        N'Fast, light on feet, always scanning; never quite fully at rest.',
        N'Corps black, Second Captain''s mark, hair always pulled back, practical.',
        N'Single Catalyst infusion; Knight-grade Transmutation: slight height gain, skeletal density increase, accelerated wound closure. Infusion point at left wrist.',
        N'Unit tactics training, intelligence briefings recently cleared for, competitive field drills, studying Bertrand''s after-action methods.',
        N'Her after-action report on the ambush improved the numbers; the real casualty count on her side was worse than she filed.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Atrament estate; northern border zone; expanded deployment following field promotion',
        N'0', N'0',
        N'Young female medieval knight, short auburn hair, green eyes, fair freckled skin, black corps coat, Second Captain mark, steampunk military compound',
        N'Female second captain knight, auburn hair, green eyes, corps black, steampunk',
        0, 0
    );
    PRINT 'Yseult Chamond seeded.';
END
ELSE PRINT 'Yseult Chamond already exists.';
GO

-- 17. Luc Berthet — Infirmary Commander
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Luc Berthet')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Luc Berthet', N'luc-berthet', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns,
        Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage,
        HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure,
        SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Luc Berthet', N'luc-berthet', N'Luc', N'Berthet', N'Master',
        N'human', N'human', N'male', N'he/him', 50, N'alive',
        N'Infirmary Commander of the Myrmidon Corps',
        N'Luc manages the Corps infirmary and oversees post-transmutation recovery. He has watched more soldiers die from Catalyst infusion than from combat. He keeps meticulous mortality records and has lobbied for a lower infusion rate for three years. Renaud has not acted on any of his reports. Luc continues writing them.',
        N'Medical conscience of the transmutation program; his records document the human cost the military hierarchy has chosen not to read.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        174, 72, N'wiry, precise; a physician''s economy of motion',
        N'grey', N'short, neat', N'short',
        N'blue-grey', N'pale', N'lined at the eyes; the face of a man who has spent years at bedsides',
        N'none',
        N'Precise, economical; a physician''s hands always either occupied or deliberately still.',
        N'White physician''s coat over Corps grey; no insignia; always clean.',
        N'none',
        N'Infirmary rounds, transmutation recovery monitoring, training medics, writing mortality reports that accumulate on the Lord''s unfiled correspondence.',
        N'He has been overstating mortality statistics by a small margin to force a formal review of infusion rates; no one has noticed.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Infirmary wing, Atrament estate; field medical stations during Corps deployments',
        N'0', N'0',
        N'Middle-aged French military physician, grey hair, blue-grey eyes, pale skin, white coat over Corps grey, steampunk infirmary',
        N'French military physician, grey hair, white coat, Corps grey, steampunk infirmary',
        0, 0
    );
    PRINT 'Luc Berthet seeded.';
END
ELSE PRINT 'Luc Berthet already exists.';
GO

-- 18. Mathilde Roques — Senior Sergeant, 28 years; Knight
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Mathilde Roques')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Mathilde Roques', N'mathilde-roques', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns,
        Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage,
        HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure,
        SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Mathilde Roques', N'mathilde-roques', N'Mathilde', N'Roques', N'',
        N'human', N'human', N'female', N'she/her', 47, N'alive',
        N'Senior Sergeant of the Myrmidon Corps; 28 years'' service; Knight',
        N'Mathilde has served Atrament''s Corps for twenty-eight years. She trained Bertrand and remembers Clemence as a raw recruit. She gives orders precisely within her station, with the authority of someone who has outlasted four Lord-changes. The Corps runs on her institutional knowledge, her logistical competence, and the specific terror she induces in recruits.',
        N'Institutional memory of the Corps; the continuity that survives commanders, crises, and the succession of Lords above her.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        170, 76, N'solid, stocky; twenty-eight years of Corps service made permanent in the frame',
        N'iron grey', N'cropped close', N'short',
        N'brown', N'weathered tan', N'deeply lined; the face of someone who has spent three decades outdoors and in training yards',
        N'Subtle height gain (Knight)',
        N'Economy of motion; nothing wasted; ground-rooted stance from twenty-eight years of Corps practice.',
        N'Corps black, Sergeant''s marks worn smooth with years; practical.',
        N'Single Catalyst infusion at age nineteen; Knight-grade Transmutation established over twenty-eight years. Infusion scar at left wrist, worn smooth.',
        N'Recruits drill, supply inspection, mentoring officers who outrank her; the person everyone finds when something is actually broken.',
        N'She has accepted small payments from merchants seeking favorable Corps protection routing for eleven years without detection.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Atrament estate; all Corps training grounds; supply inspection routes across the central region',
        N'0', N'0',
        N'Middle-aged female medieval French sergeant, iron grey cropped hair, brown eyes, tanned weathered skin, worn black corps uniform, steampunk training yard',
        N'Female sergeant, iron grey hair, brown eyes, worn corps black, steampunk training yard',
        0, 0
    );
    PRINT 'Mathilde Roques seeded.';
END
ELSE PRINT 'Mathilde Roques already exists.';
GO

-- 19. Beatris Peyroux — Junior Officer; recently distinguished
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Beatris Peyroux')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Beatris Peyroux', N'beatris-peyroux', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns,
        Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage,
        HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure,
        SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Beatris Peyroux', N'beatris-peyroux', N'Beatris', N'Peyroux', N'',
        N'human', N'human', N'female', N'she/her', 24, N'alive',
        N'Junior Officer of the Myrmidon Corps; recently distinguished in cipher operations',
        N'Beatris decoded an intercepted cipher during a border crisis and identified a rival House''s supply route, allowing interdiction without confrontation. Promoted from courier to junior officer in one quarter. Precise, capable, and not yet sure what she has become. Senior officers are watching her more closely than she has learned to notice.',
        N'Fresh eyes on Atrament''s intelligence-military nexus; her disorientation at her own promotion makes the apparatus''s mechanisms visible.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        168, 61, N'lean, alert; a body still learning what rank requires of it',
        N'dark brown', N'short, practical', N'short',
        N'dark brown', N'medium brown', N'clear, young; the only person in the Corps who still looks surprised to be here',
        N'none',
        N'Eager, slightly over-precise; working hard to look like she belongs in every room.',
        N'Fresh Corps issue, officer''s mark, maintained with excessive and telling care.',
        N'none',
        N'Intelligence training, cipher study, field exercises, trying to decode what her new superiors actually want from her.',
        N'She decoded the cipher by mistake — she was reading the wrong message format and stumbled onto the answer; she has told no one.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Atrament estate; cipher operations wing; recently cleared for broader field access',
        N'0', N'0',
        N'Young female medieval French officer, short dark brown hair, dark brown eyes, medium brown skin, new corps uniform, officer mark, steampunk estate',
        N'Young female officer, dark brown hair, medium brown skin, new corps uniform, steampunk',
        0, 0
    );
    PRINT 'Beatris Peyroux seeded.';
END
ELSE PRINT 'Beatris Peyroux already exists.';
GO

-- 20. Guichard Pelat — Corps Transmutation Practitioner
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Guichard Pelat')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Guichard Pelat', N'guichard-pelat', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns,
        Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage,
        HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure,
        SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Guichard Pelat', N'guichard-pelat', N'Guichard', N'Pelat', N'Master',
        N'human', N'human', N'male', N'he/him', 41, N'alive',
        N'Corps Transmutation Practitioner; administrator of Catalyst infusions',
        N'Guichard administers Catalyst infusions to Atrament''s military candidates. He has performed thirty-seven procedures. Twenty-nine soldiers died. He is technically unmatched in the House''s understanding of transmutation biology and does not sleep soundly. He chose not to undergo transmutation himself, believing a practitioner should witness the process through unaltered physiology.',
        N'The mechanism of transmutation-as-system; Guichard holds the program''s true cost in his record and his body''s deliberately preserved vulnerability.',
        N'No POV.',
        N'House Atrament; vine country, central continent',
        177, 80, N'medium, deliberate; a body he has chosen to keep unaltered as a matter of professional principle',
        N'dark brown', N'pulled back, neat', N'medium',
        N'amber-brown', N'olive', N'clear but tired; fine lines from reading small script in infirmary light',
        N'none',
        N'Deliberate; hands always visible; moves as if performing a procedure even when not.',
        N'White practitioner''s coat, no Corps insignia, Transmutation Seal at the collar.',
        N'none',
        N'Candidate assessment, Catalyst procurement, infusion procedures, post-death documentation, counseling survivors; rarely socializes with Corps personnel.',
        N'He adjusts candidate selection to favor soldiers he personally likes, knowing this corrupts the process''s objectivity and does it anyway.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Atrament estate; Catalyst procurement routes; infirmary and Transmutation chamber',
        N'0', N'0',
        N'Middle-aged French alchemist-physician, dark brown pulled-back hair, amber eyes, olive skin, white coat, Transmutation seal, steampunk alchemical laboratory',
        N'French transmutation practitioner, white coat, olive skin, steampunk alchemical lab',
        0, 0
    );
    PRINT 'Guichard Pelat seeded.';
END
ELSE PRINT 'Guichard Pelat already exists.';
GO
