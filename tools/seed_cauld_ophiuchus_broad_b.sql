SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- HOUSE OPHIUCHUS — BROAD POPULATION BATCH B  (15 characters)
-- Universe: Cauld  (ID 0197E9C9-0002-7000-8000-000000000002)
-- 2026-07-05 · Idempotent (IF NOT EXISTS guards on all inserts)
-- Focus: Elder / fringe — 40-yr Sphere watchers, Oathless former researchers,
--   elderly librarians with institutional memory, failed Transmutation survivors
--   who became case studies, Sphere 31 people absorbed into research roles.
-- ═══════════════════════════════════════════════════════════════════════════════

-- ── 1. Bartolomeo Galli ──────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Bartolomeo Galli')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Bartolomeo Galli', N'bartolomeo-galli', N'canon', 1,
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
        @id, N'Bartolomeo Galli', N'bartolomeo-galli', N'Bartolomeo', N'Galli', N'',
        N'human', N'human', N'male', N'he/him',
        71, N'alive',
        N'Senior Scrying analyst; 43 years watching Sphere 31 Sector 7; unofficial institutional memory of the installation.',
        N'Spent 43 years watching a single Sphere 31 district through the Scrying glass. Knows the names of dead strangers across two generations. His observation logs are the most complete record Ophiuchus holds.',
        N'Living archive of Sphere 31 Sector 7; his records contain evidence no one has catalogued or acted on.',
        N'No POV.',
        N'House Ophiuchus; Scrying installation, southern observatory floor',
        178, 94, N'heavy-set, sedentary scholar''s body; once broad, now settled',
        N'white', N'cropped close', N'short',
        N'dark brown', N'warm olive', N'weathered; deep folds at the eyes',
        N'none',
        N'Still; the practiced patience of someone trained to wait without moving.',
        N'Plain scholar''s robes, ink-stained; House Ophiuchus seal on a cord at his chest.',
        N'none',
        N'Morning log review, afternoon observation shift, evening transcription. Has not left the installation in eleven months.',
        N'Three years ago he watched a Sphere 31 boy cross a Liturgy transit route — meaning a taking — and did not log it.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Ophiuchus Scrying installation, southern observatory floor',
        N'0', N'0',
        N'elderly Italian scholar, white cropped hair, ink-stained plain robes, warm olive skin, observatory with glowing Scrying glass, dark medieval fantasy portrait',
        N'Elderly Italian man, white hair cropped close, plain ink-stained robes, warm olive skin, Scrying observatory interior, medieval dark fantasy portrait',
        0, 0
    );
    PRINT N'Bartolomeo Galli seeded.';
END
ELSE PRINT N'Bartolomeo Galli already exists.';
GO

-- ── 2. Caterina Volpe ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Caterina Volpe')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Caterina Volpe', N'caterina-volpe', N'canon', 1,
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
        @id, N'Caterina Volpe', N'caterina-volpe', N'Caterina', N'Volpe', N'',
        N'human', N'human', N'female', N'she/her',
        74, N'alive',
        N'Chief Librarian; Ophiuchus Restricted Archive; forty years administering documents no junior scholar has ever accessed.',
        N'Runs the restricted stacks alone. No assistant has held her clearance. She is seventy-four and has refused retirement four times. The archive would not function without her.',
        N'Gatekeeper of every suppressed Ophiuchus record; her silence has protected people and concealed crimes in equal measure.',
        N'No POV.',
        N'House Ophiuchus; archive vaults, southern seat',
        158, 52, N'slight, precise; the contained frame of a woman who never wastes motion',
        N'silver-white', N'neat bun', N'medium',
        N'grey', N'pale olive', N'fine-featured; the skin of someone who has worked indoors for forty years',
        N'none',
        N'Economical; never a wasted movement; navigates the stacks as if she memorized every step.',
        N'Dark grey scholar''s robes, plain; a small ring of keys always at her belt.',
        N'none',
        N'Opens the archive at dawn, closes at dusk. No scheduled breaks. Eats over the ledger. Refuses company.',
        N'She burned a Liturgy transit log naming a senior Ophiuchus chancellor as having arranged a Sphere 31 taking in exchange for tariff relief.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Ophiuchus Archive, restricted vaults, southern seat',
        N'0', N'0',
        N'elderly Italian woman, silver-white neat bun, grey eyes, dark grey plain robes, key ring at belt, ancient stone archive vaults, candlelight, medieval dark fantasy portrait',
        N'Elderly Italian woman, silver bun, grey eyes, dark grey robes, key ring at belt, ancient stone archive vaults, candlelight, medieval portrait',
        0, 0
    );
    PRINT N'Caterina Volpe seeded.';
END
ELSE PRINT N'Caterina Volpe already exists.';
GO

-- ── 3. Dante Ferro ───────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Dante Ferro')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Dante Ferro', N'dante-ferro', N'canon', 1,
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
        @id, N'Dante Ferro', N'dante-ferro', N'Dante', N'Ferro', N'',
        N'human', N'human', N'male', N'he/him',
        54, N'alive',
        N'Living Transmutation case study; first infusion survivor; 24 years post-infusion; documents his own condition for researchers.',
        N'Survived the infusion at odds his researchers described as catastrophic. No enhancement followed. The Xerum 525 arrested without resolving. He is the most studied living failure in Ophiuchus records.',
        N'A warning in living form; his case holds evidence that the consent procedure is corruptible.',
        N'No POV.',
        N'House Ophiuchus; infirmary wing, southern seat',
        174, 61, N'gaunt, asymmetric; infusion damage apparent in his gait',
        N'thin brown-grey', N'loose, unwashed', N'medium',
        N'pale hazel; one pupil permanently irregular', N'warm brown', N'pallid beneath the warmth; persistent low-grade jaundice',
        N'none (failed infusion; no enhancement achieved)',
        N'Slightly hunched; favors his left side; gait uneven. Does not draw attention to it.',
        N'Research subject''s grey linen; no House insignia; a handwritten case number on a tag at his collar.',
        N'Xerum 525 first infusion survivor; no enhancement achieved; progressive systemic disorder.',
        N'Weekly examinations, written symptom reports, library access by permission. Fills notebooks between appointments.',
        N'His consent to a second infusion attempt was forged — he was unconscious; researchers signed for him; he discovered the fraud four years later and said nothing.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Ophiuchus infirmary wing; library by permission',
        N'0', N'0',
        N'gaunt Italian man, fifties, thin brown-grey hair, pale jaundiced complexion, grey linen with handwritten case tag, stone infirmary, medieval dark fantasy portrait',
        N'Gaunt Italian man in his fifties, thin brown-grey hair, pale jaundiced complexion, plain grey linen, stone infirmary, medieval portrait',
        0, 0
    );
    PRINT N'Dante Ferro seeded.';
END
ELSE PRINT N'Dante Ferro already exists.';
GO

-- ── 4. Silvana Orsi ──────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Silvana Orsi')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Silvana Orsi', N'silvana-orsi', N'canon', 1,
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
        @id, N'Silvana Orsi', N'silvana-orsi', N'Silvana', N'Orsi', N'',
        N'human', N'human', N'female', N'she/her',
        67, N'alive',
        N'Oathless; former Ophiuchus Scrying researcher; sheltered inside the House by an undisclosed benefactor for nine years.',
        N'Declared Oathless nine years ago after a research dispute that was never officially documented. She should be dead. She is not. Someone inside the House is keeping her.',
        N'Her continued existence proves complicity somewhere in the House; what she knows explains why she was exiled.',
        N'No POV.',
        N'House Ophiuchus (formerly); sheltered location undisclosed, old Venn wing',
        163, 57, N'spare, deliberate; a body accustomed to moving without being seen',
        N'iron grey', N'cut short, practical', N'short',
        N'dark hazel', N'medium olive', N'weathered; the face of someone who stopped sleeping well years ago',
        N'none',
        N'Learned stillness — not natural calm. Reads a room before entering it.',
        N'Plain undyed wool; nothing that marks House affiliation.',
        N'none',
        N'Confined to a section of the old Venn wing; researches in hiding; rarely moves during daylight hours.',
        N'She carries a sealed vial of unregistered Xerum 525 stolen on the day she fled — the only sample outside official Ophiuchus inventory.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Hidden; old Venn wing, southern seat',
        N'0', N'0',
        N'older Italian woman, iron grey short hair, plain undyed wool, shadowed stone hidden chamber, wary and still, Buehlman dark medieval portrait',
        N'Older Italian woman, iron grey short hair, plain undyed wool, shadowed stone chamber, wary expression, dark medieval portrait',
        0, 0
    );
    PRINT N'Silvana Orsi seeded.';
END
ELSE PRINT N'Silvana Orsi already exists.';
GO

-- ── 5. Lorenzo Acqua ─────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Lorenzo Acqua')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Lorenzo Acqua', N'lorenzo-acqua', N'canon', 1,
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
        @id, N'Lorenzo Acqua', N'lorenzo-acqua', N'Lorenzo', N'Acqua', N'',
        N'human', N'human', N'male', N'he/him',
        58, N'alive',
        N'Senior translation assistant; Sphere 31 native taken at age 13; 45 years in Ophiuchus service; specialist in Sphere 31 written languages.',
        N'Taken from a Sphere 31 coastal village at thirteen. He learned faster than his handlers expected and rose beyond the role they designed for him. No Ophiuchus scholar reads Sphere 31 manuscripts as accurately.',
        N'The Sphere 31 perspective made institutional; his cooperation costs him something the House has never tallied.',
        N'No POV.',
        N'Originally Sphere 31, southern coastal village; House Ophiuchus (45 years in service)',
        172, 73, N'medium, careful; a man who learned to occupy exactly the right amount of space',
        N'dark brown going grey', N'neat, close', N'short',
        N'dark brown', N'warm medium brown', N'even; fine lines at the eyes from decades of close reading',
        N'none',
        N'Measured; adapts posture and register to whoever is present. Never draws attention to the adaptation.',
        N'Scholar''s robes with a minor House Ophiuchus mark; practical, not ceremonial.',
        N'none',
        N'Translation work in the archive, briefings with senior analysts, Scrying gallery attendance by invitation. Works long hours without complaint.',
        N'He found the Scrying log from the year he was taken; the researcher''s notation read "one male child, approximately 13, fit and adaptable" — he has told no one.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Ophiuchus Archive, translation offices; Scrying gallery by invitation',
        N'0', N'0',
        N'middle-aged Mediterranean man, neat dark brown-grey hair, scholar''s robes with small House seal, candlelit archive reading desk, medieval dark fantasy portrait',
        N'Middle-aged Mediterranean man, neat dark hair going grey, scholar''s robes, archive reading desk, candlelight, medieval portrait',
        0, 0
    );
    PRINT N'Lorenzo Acqua seeded.';
END
ELSE PRINT N'Lorenzo Acqua already exists.';
GO

-- ── 6. Elena Moro ────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Elena Moro')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Elena Moro', N'elena-moro', N'canon', 1,
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
        @id, N'Elena Moro', N'elena-moro', N'Elena', N'Moro', N'',
        N'human', N'human', N'female', N'she/her',
        69, N'alive',
        N'Senior Scrying analyst; 38 years assigned the same installation sector; specialist in demographic continuity tracking.',
        N'Documented thirty-eight years of one Sphere 31 district without interruption. Every face in the record she can place. The House considers her invaluable. She knows this gives her a kind of protection.',
        N'Simultaneous witness and accomplice; her moral compromise with the Scrying record is the tension she carries.',
        N'No POV.',
        N'House Ophiuchus; Scrying installation, eastern gallery',
        165, 68, N'sturdy, methodical; the body of someone who has waited at the glass for thirty-eight years',
        N'dark brown with heavy grey', N'pulled back', N'medium',
        N'green-brown', N'warm olive', N'weathered; clear apart from the lines',
        N'none',
        N'Steady and unhurried; a body that learned patience. Keeps her hands clean, always.',
        N'Practical scholar''s robes; no ornamentation. A plain band at her wrist she does not explain.',
        N'none',
        N'Observation shifts, demographic log updates, biannual briefings with senior command. Eats alone. Has done so for years.',
        N'She identified a Sphere 31 scholar as a viable Liturgy extraction target, then retracted her own report and replaced it with a notation marking the district uninhabited.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Ophiuchus Scrying installation, eastern gallery',
        N'0', N'0',
        N'older Italian woman, dark brown-grey hair pulled back, plain scholar''s robes, Scrying installation eastern gallery, medieval dark fantasy portrait',
        N'Older Italian woman, dark brown hair going grey pulled back, plain robes, Scrying gallery interior, medieval portrait',
        0, 0
    );
    PRINT N'Elena Moro seeded.';
END
ELSE PRINT N'Elena Moro already exists.';
GO

-- ── 7. Marco Cielo ───────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Marco Cielo')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Marco Cielo', N'marco-cielo', N'canon', 1,
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
        @id, N'Marco Cielo', N'marco-cielo', N'Marco', N'Cielo', N'',
        N'human', N'human', N'male', N'he/him',
        81, N'alive',
        N'Retired Chief Archivist; blind since age 73; retains complete organizational memory of the archive''s physical and documentary structure.',
        N'Ran the archive for thirty-one years. Went blind eight years ago and refused retirement. He locates documents by memory, running his hands along shelves with the certainty of a man reading a room he built.',
        N'Institutional memory made flesh; what he knows and can no longer see may be the safest secret in the House.',
        N'No POV.',
        N'House Ophiuchus; archive vaults and reading halls, southern seat',
        176, 64, N'thin, fragile-looking; still surprisingly sure-footed within familiar walls',
        N'white', N'sparse, close', N'short',
        N'pale brown, clouded', N'pale olive', N'paper-thin at the hands; age-spotted',
        N'none',
        N'Slow but precise; uses a carved ash staff; does not let it become a crutch.',
        N'Worn deep-blue scholar''s robes; repaired many times; the colour was brighter once.',
        N'none',
        N'Present in the archive every morning; navigates by touch and memory; allows junior archivists to read to him; refuses scribes.',
        N'He memorized a section officially destroyed forty years ago — it proves the Scrying installation predates the House''s founding by two generations, voiding the founding charter.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Ophiuchus Archive, main vaults and reading halls',
        N'0', N'0',
        N'very elderly Italian man, white sparse hair, clouded pale brown eyes, worn deep-blue robes, carved ash staff, stone archive interior, medieval dark fantasy portrait',
        N'Very elderly Italian man, white sparse hair, blind pale brown eyes, worn dark blue robes, carved ash staff, stone archive interior, medieval portrait',
        0, 0
    );
    PRINT N'Marco Cielo seeded.';
END
ELSE PRINT N'Marco Cielo already exists.';
GO

-- ── 8. Rinaldo Festo ─────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Rinaldo Festo')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Rinaldo Festo', N'rinaldo-festo', N'canon', 1,
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
        @id, N'Rinaldo Festo', N'rinaldo-festo', N'Rinaldo', N'Festo', N'',
        N'human', N'human', N'male', N'he/him',
        52, N'alive',
        N'Transmutation research subject; partial Xerum 525 survivor; progressive crystallization; ongoing case study for twenty-two years.',
        N'Took the infusion at thirty. The Xerum 525 arrested mid-process. What emerged was not enhancement but calcification — progressive crystallization of the extremities his researchers find riveting and he finds terrifying.',
        N'The specific cost of failed Transmutation held in one body; his active deception reveals how the research apparatus can be gamed.',
        N'No POV.',
        N'House Ophiuchus; research wing, southern seat',
        173, 70, N'medium; left arm visibly wrong under the sleeve — stiffer, slightly heavier',
        N'dark brown', N'unwashed, loose', N'medium',
        N'dark brown', N'warm medium brown', N'tired; strain lines at the mouth',
        N'none (active concealment; crystallization hidden under clothing)',
        N'Holds his left arm close to his body. Covers the asymmetry when he can.',
        N'Loose-sleeved research subject''s linen; always long sleeves regardless of season.',
        N'Xerum 525 partial survivor; Transmutation arrested mid-process; progressive crystallization of left arm; concealed from researchers.',
        N'Twice-weekly examinations, symptom reporting, restricted library access. Spends evenings writing notebooks he does not share.',
        N'The crystallization has spread to his wrist and forearm; he has hidden this from researchers for three years by wrapping the arm before examinations and falsifying his own symptom logs.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Research wing, Ophiuchus southern seat',
        N'0', N'0',
        N'Italian man, fifties, dark unwashed hair, loose long-sleeved linen, holding left arm close, tired dark eyes, stone research chamber, medieval dark fantasy portrait',
        N'Italian man in his fifties, dark hair, loose long-sleeved plain linen, holding left arm close, stone research chamber, medieval portrait',
        0, 0
    );
    PRINT N'Rinaldo Festo seeded.';
END
ELSE PRINT N'Rinaldo Festo already exists.';
GO

-- ── 9. Lucrezia Ombra ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Lucrezia Ombra')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Lucrezia Ombra', N'lucrezia-ombra', N'canon', 1,
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
        @id, N'Lucrezia Ombra', N'lucrezia-ombra', N'Lucrezia', N'Ombra', N'',
        N'human', N'human', N'female', N'she/her',
        65, N'alive',
        N'Oathless; former senior Transmutation researcher; carries knowledge of a suppressed infusion process; hidden inside the House for six years.',
        N'Left Ophiuchus six years ago under circumstances the official record does not explain. She knows a Transmutation protocol that was suppressed rather than published. Someone in the current research corps chose that silence. She knows who.',
        N'The suppressed knowledge she carries could destabilize the Transmutation program; her survival is itself a political act.',
        N'No POV.',
        N'House Ophiuchus (formerly); location concealed, old Venn wing',
        161, 55, N'lean, compact; the coiled patience of someone used to not being found',
        N'dark brown-grey', N'pulled flat against her head', N'short',
        N'dark brown', N'medium olive', N'marked by tension; deep lines at the corners of her mouth',
        N'none',
        N'Controlled; never lets her weight settle as though always ready to move.',
        N'Undyed wool, practical; nothing that identifies her.',
        N'none',
        N'Research in hiding using texts she memorized before she left. Eats when food is brought. Rarely speaks above a murmur.',
        N'She was questioned about her sheltering knight''s identity and lied convincingly; the knight is Dame Lyra''s former research partner, whose name she has protected for six years.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Hidden; old Venn wing, southern seat',
        N'0', N'0',
        N'older Italian woman, dark brown-grey hair flat against head, plain undyed wool, shadowed stone hidden chamber, controlled wary expression, Buehlman dark medieval portrait',
        N'Older Italian woman, dark brown-grey hair flat against head, plain undyed wool, stone hidden chamber, controlled expression, medieval portrait',
        0, 0
    );
    PRINT N'Lucrezia Ombra seeded.';
END
ELSE PRINT N'Lucrezia Ombra already exists.';
GO

-- ── 10. Fiora Venn ───────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Fiora Venn')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Fiora Venn', N'fiora-venn', N'canon', 1,
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
        @id, N'Fiora Venn', N'fiora-venn', N'Fiora', N'Venn', N'',
        N'human', N'human', N'female', N'she/her',
        84, N'alive',
        N'Senior Librarian; last direct descendant of House Venn; nominal keeper of the Venn-Ophiuchus merger documentation.',
        N'Eighty-four years old. The last person alive who remembers House Venn as a separate entity. She has served Ophiuchus faithfully for sixty years. She has not forgiven the merger. This is not a contradiction.',
        N'The merger''s ghost made flesh; her compliance and her consent are not the same thing.',
        N'No POV.',
        N'House Venn (legacy); House Ophiuchus; archive, southern seat',
        155, 47, N'small, deliberate; the careful economy of the very old and still commanding',
        N'white, fine', N'pinned with a Venn family clip she has never set aside', N'medium',
        N'pale hazel, still sharp', N'pale olive, heavily lined', N'paper-thin at the hands; age-mottled; the face still commanding',
        N'none',
        N'Upright past any physical reason for it; will not concede to age in her spine.',
        N'Ophiuchus robes worn over a Venn-silver underlayer she has worn every day since the merger was finalized.',
        N'none',
        N'Archive consultation, junior librarian supervision, brief correspondence with the House lord she answers precisely and without warmth.',
        N'The original Venn merger contract, hidden in a sealed stone compartment in the old Venn archive wing, contains two clauses Ophiuchus violated in its first decade; she has known this for sixty years and said nothing.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Ophiuchus Archive, Venn wing and main reading halls',
        N'0', N'0',
        N'ancient Italian noblewoman, white fine hair with antique family clip, pale olive deeply lined face, formal dark robes over silver Venn underlayer, old stone archive, medieval dark fantasy portrait',
        N'Ancient Italian noblewoman, white fine hair with antique clip, pale olive deeply lined face, formal dark robes over silver underlayer, stone archive, medieval portrait',
        0, 0
    );
    PRINT N'Fiora Venn seeded.';
END
ELSE PRINT N'Fiora Venn already exists.';
GO

-- ── 11. Valentina Sere ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Valentina Sere')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Valentina Sere', N'valentina-sere', N'canon', 1,
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
        @id, N'Valentina Sere', N'valentina-sere', N'Valentina', N'Sere', N'',
        N'human', N'human', N'female', N'she/her',
        63, N'alive',
        N'Senior Scrying analyst; Sphere 31 communications-pattern specialist; twenty-six years in installation service.',
        N'Spent twenty-six years mapping Sphere 31 communication patterns. She knows the signal methods of that world better than any Ophiuchus scholar alive. This knowledge has become intimate in ways the House has not noticed.',
        N'The cost of sustained observation paid in full; she has already crossed the line the Scrying charter forbids.',
        N'No POV.',
        N'House Ophiuchus; Scrying installation, northern signal gallery',
        166, 62, N'medium, sedentary; ink always on her fingers; a slight forward lean from years at the glass',
        N'dark brown, beginning to silver', N'loosely pinned', N'medium',
        N'warm brown', N'medium olive', N'fine-featured; deep focus lines between the brows',
        N'none',
        N'Leans slightly forward; years at the Scrying glass have built it into her posture.',
        N'Standard scholar''s robes, lightly personalized; a dried pressed flower inside her log cover that no one has ever noticed.',
        N'none',
        N'Signal-pattern analysis, log cross-referencing, quarterly briefings with senior command. Works past authorized hours without recording the overtime.',
        N'Eighteen months ago she sent a personal message into Sphere 31 disguised as a Scrying calibration signal, encoding it in the settlement''s own frequency pattern; she believes it was received and answered.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Ophiuchus Scrying installation, northern signal gallery',
        N'0', N'0',
        N'older Italian woman, dark brown hair silvering, loosely pinned, plain scholar''s robes, leaning toward glowing Scrying signal equipment, medieval dark fantasy portrait',
        N'Older Italian woman, dark brown hair pinning silver, plain scholar''s robes, leaning toward glowing signal equipment, Scrying installation gallery, medieval portrait',
        0, 0
    );
    PRINT N'Valentina Sere seeded.';
END
ELSE PRINT N'Valentina Sere already exists.';
GO

-- ── 12. Corrado Bellis ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Corrado Bellis')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Corrado Bellis', N'corrado-bellis', N'canon', 1,
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
        @id, N'Corrado Bellis', N'corrado-bellis', N'Corrado', N'Bellis', N'',
        N'human', N'human', N'male', N'he/him',
        70, N'alive',
        N'Former Transmutation documentation specialist; accidental low-dose exposure survivor; nominally retired; informally present throughout the research wing.',
        N'Documented two hundred and eleven Transmutation deaths over thirty years. Then a containment failure exposed him to a sub-lethal dose and his body did something unexpected. He has not told his researchers everything he noticed.',
        N'The observer who became the observed; his hidden enhancement makes him quietly dangerous to his own institution.',
        N'No POV.',
        N'House Ophiuchus; Transmutation research wing and adjacent archive, southern seat',
        180, 84, N'solid, unhurried; the contained physicality of a man who spent decades ensuring he was not the subject',
        N'silver-white', N'close-cropped', N'short',
        N'blue-grey', N'pale-medium olive', N'even; the practiced unremarkable face of someone used to watching others be studied',
        N'none (minor residual enhancement deliberately concealed)',
        N'Measured; occupies space without demanding it. Rarely the loudest person present.',
        N'Plain dark wool; no House insignia since formal retirement.',
        N'Accidental low-dose Xerum 525 exposure; minor thermal-perception enhancement (detects fever and pain by proximity); unreported and concealed.',
        N'No official role. Present everywhere — archive consultations, informal meals with current researchers, long walks through the installation.',
        N'The exposure gave him a thermal-perception enhancement he has never reported; he uses it to monitor Dame Lyra''s health without her knowledge or consent, and has done so for four years.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Transmutation research wing and adjacent archive; full southern seat by informal access',
        N'0', N'0',
        N'silver-haired Italian man, seventies, pale olive skin, plain dark wool, stone Transmutation research corridor, quietly observant expression, medieval dark fantasy portrait',
        N'Silver-haired Italian man in his seventies, plain dark wool, pale olive skin, medieval stone research corridor, quietly observant expression, dark fantasy portrait',
        0, 0
    );
    PRINT N'Corrado Bellis seeded.';
END
ELSE PRINT N'Corrado Bellis already exists.';
GO

-- ── 13. Giacomo Luce ─────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Giacomo Luce')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Giacomo Luce', N'giacomo-luce', N'canon', 1,
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
        @id, N'Giacomo Luce', N'giacomo-luce', N'Giacomo', N'Luce', N'',
        N'human', N'human', N'male', N'he/him',
        77, N'alive',
        N'Senior scholar; Sphere boundary phenomena and membrane-edge observation specialist; forty years of increasingly unorthodox theoretical work.',
        N'Spent forty years studying not what is visible in Sphere 31 but what is visible at the edge of the Scrying frame — the artifacts, the distortions, the places where the membrane is thin.',
        N'The researcher asking a question the House officially does not want answered; a structural threat to the Scrying program''s premises.',
        N'No POV.',
        N'House Ophiuchus; theoretical research offices, southern seat',
        174, 63, N'thin, distracted; the body of someone who let time work on him without noticing',
        N'white', N'disordered; he forgets about it', N'medium',
        N'pale blue', N'fair olive, paled with age', N'thin-skinned; light-veined at the temples',
        N'none',
        N'Slouched; leans toward whatever he is examining; pulls upright only when challenged.',
        N'Old scholar''s robes, comfortable, carrying the stains of forty years'' work; a magnifying lens on a cord he uses for close examination of texts.',
        N'none',
        N'Theoretical writing, archive consultation, solitary Scrying gallery attendance after hours. Argues with his own notes in the margins.',
        N'He has calculated that the installation''s membrane-edge distortions indicate an intelligence in Sphere 31 observing back, and has told no one in the House for eleven years.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Theoretical research offices; Scrying gallery after authorized hours',
        N'0', N'0',
        N'elderly Italian scholar, white disordered hair, pale blue eyes, old ink-stained robes, magnifying lens on cord, studying membrane-edge Scrying frame, medieval dark fantasy portrait',
        N'Elderly Italian man, white disordered hair, pale blue eyes, old stained robes, magnifying lens on cord, medieval stone study, dark fantasy portrait',
        0, 0
    );
    PRINT N'Giacomo Luce seeded.';
END
ELSE PRINT N'Giacomo Luce already exists.';
GO

-- ── 14. Bianca Sera ──────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Bianca Sera')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Bianca Sera', N'bianca-sera', N'canon', 1,
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
        @id, N'Bianca Sera', N'bianca-sera', N'Bianca', N'Sera', N'Dame',
        N'human', N'human', N'female', N'she/her',
        62, N'alive',
        N'Senior Dame; veteran of thirty-seven years; current overseer of Myrmidon candidate selection and pre-infusion conditioning.',
        N'Took Xerum 525 at twenty-five. Survived at odds she has since calculated precisely. Spent thirty-seven years becoming what the infusion made. Now she selects who takes the infusion next — and does not trust her own judgment.',
        N'The institution''s conscience embedded in its most morally compromised administrative role.',
        N'No POV.',
        N'House Ophiuchus; Transmutation training compound, southern seat',
        183, 86, N'Knight''s build — enhanced height, denser muscle structure; visible in her proportions without being extreme',
        N'silver-streaked black', N'short, practical', N'short',
        N'dark brown', N'warm olive', N'weathered; fine scars at the jaw and left forearm from early service',
        N'Subtle height gain (Knight)',
        N'Precisely upright; military posture of forty years; it is no longer a decision.',
        N'Knight''s field dress in Ophiuchus colours; no ceremonial concessions; armour functional and worn.',
        N'Xerum 525 Knight-grade infusion survivor; subtle height gain, enhanced muscle density; standard Knight profile.',
        N'Candidate evaluation, conditioning oversight, weekly briefings with Transmutation research staff. Reads candidate files at night and makes marks she does not enter into the official record.',
        N'Twelve years ago she signed falsified survival-rate records — the true figure was 23%, not the reported 61% — at the request of two Knight commanders protecting their own reputations.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Ophiuchus Transmutation training compound; research wing by access',
        N'0', N'0',
        N'Italian woman, sixties, silver-streaked black short hair, Knight''s armour in dark burgundy and midnight blue, warm olive skin, training compound, veteran expression, medieval dark fantasy portrait',
        N'Italian woman in her sixties, silver-streaked black hair, Knight armour in dark burgundy and blue, warm olive skin, medieval training compound, veteran expression, dark fantasy portrait',
        0, 0
    );
    PRINT N'Bianca Sera seeded.';
END
ELSE PRINT N'Bianca Sera already exists.';
GO

-- ── 15. Alessandra Mote ──────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Alessandra Mote')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Alessandra Mote', N'alessandra-mote', N'canon', 1,
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
        @id, N'Alessandra Mote', N'alessandra-mote', N'Alessandra', N'Mote', N'',
        N'human', N'human', N'female', N'she/her',
        76, N'alive',
        N'Senior scholar; Comparative Sphere Studies; forty-three years; author of the foundational classification system Ophiuchus still uses to organize all Scrying data.',
        N'Built the classification system Ophiuchus uses to organize Scrying data across all observed Spheres. Published forty-three years of comparative work. Received every commendation the House offers. Her foundational paper contains a fact she knows is wrong.',
        N'Academic fraud embedded in the foundation of the House''s scholarly apparatus; her secret is structural, not personal.',
        N'No POV.',
        N'House Ophiuchus; comparative research offices, southern seat',
        164, 58, N'spare, upright; the carriage of someone accustomed to intellectual authority',
        N'white', N'precisely maintained, single silver braid', N'long',
        N'grey-green', N'pale olive', N'fine-featured; the controlled face of a woman performing certainty for forty years',
        N'none',
        N'Composed; economical gestures; rarely fidgets. The performance of certainty is complete.',
        N'Senior scholar''s robes in Ophiuchus colours, formal; the House''s academic commendation marks on a collar pin she wears every day.',
        N'none',
        N'Writing, graduate consultation, archive access, periodic address to the research assembly. Retirement would require explaining why she stopped.',
        N'Her foundational comparative paper credited a technology as Cauld-indigenous when she knew it was reverse-engineered from Sphere 31 source material; a second set of notes proving this is sealed in a stone box behind her office wall.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Comparative research offices; archive; House assembly chamber by invitation',
        N'0', N'0',
        N'elderly Italian noblewoman, white precisely maintained silver braid, pale olive skin, formal dark robes with academic commendation pin, stone scholarly study, medieval dark fantasy portrait',
        N'Elderly Italian noblewoman, white silver braid, pale olive skin, formal dark robes with academic pin, stone scholarly study, medieval portrait',
        0, 0
    );
    PRINT N'Alessandra Mote seeded.';
END
ELSE PRINT N'Alessandra Mote already exists.';
GO
