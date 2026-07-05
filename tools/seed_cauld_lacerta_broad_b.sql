SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- HOUSE LACERTA — BROAD POPULATION BATCH B  (15 characters)
-- Universe: Cauld  (ID 0197E9C9-0002-7000-8000-000000000002)
-- 2026-07-05 · Idempotent (IF NOT EXISTS guards on all inserts)
-- Focus: Elder/fringe — veterans, Oathless at the sea edge, elderly civilians,
--   failed Transmutation survivors, deeply integrated Sphere 31 observers,
--   wandering practitioners, Bheur-watchers near the Chamber.
-- ═══════════════════════════════════════════════════════════════════════════════

-- ── 1. Aldara Mares ───────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Aldara Mares')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Aldara Mares', N'aldara-mares', N'canon', 1,
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
        @id, N'Aldara Mares', N'aldara-mares', N'Aldara', N'Mares', N'',
        N'human', N'human', N'female', N'she/her',
        71, N'alive',
        N'Retired Scrying clerk; forty years logging Chamber records; now a fixture on the cliff approach path.',
        N'Spent forty years logging the Chamber''s observation records. Retired but arrives before first watch, reads the water. The Scriers treat her like furniture. She has noticed things they have not.',
        N'The witness who has outlasted three administrations and knows what was quietly changed.',
        N'No POV.',
        N'House Lacerta; Atlantic cliff community, Chamber approach path',
        158, 52, N'slight, stooped',
        N'white', N'loose', N'medium',
        N'pale brown', N'olive-weathered', N'deeply creased, sun-lined',
        N'none',
        N'Slow and deliberate; uses a carved stick on rough ground',
        N'Plain wool in faded rust; practical for cliff wind',
        N'none',
        N'Walks the cliff path at dawn; watches the surf; speaks to Chamber staff; returns at dusk.',
        N'She recognized Lord Rodrigo Lacerta-Vante''s dead wife alive in a Sphere 31 observation feed and recorded nothing.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Chamber approach path; cliff community; Lacerta coastal village',
        N'0', N'0',
        N'elderly Portuguese woman, white loose hair, faded rust wool, Atlantic cliff path, pale brown eyes, dawn light, dark fantasy portrait',
        N'Old woman in rust wool, white loose hair, pale brown eyes, cliff path, dark fantasy',
        0, 0
    );
    PRINT N'Aldara Mares seeded.';
END
ELSE PRINT N'Aldara Mares already exists.';
GO

-- ── 2. Brais Souto ────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Brais Souto')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Brais Souto', N'brais-souto', N'canon', 1,
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
        @id, N'Brais Souto', N'brais-souto', N'Brais', N'Souto', N'',
        N'human', N'human', N'male', N'he/him',
        44, N'alive',
        N'Former infusion candidate; survived the Catalyst without elevation; assigned to Chamber maintenance.',
        N'The Catalyst took his elevation and gave him cold damage instead. Left hand stays cold; shoulder dislocates in wet weather. He maintains the Chamber. Nobody meets his eye.',
        N'The visible cost of failed Transmutation; what the system discards without removing from sight.',
        N'No POV.',
        N'House Lacerta; cliff-base maintenance settlement',
        174, 78, N'formerly strong, now asymmetric at the shoulders',
        N'black', N'cropped', N'short',
        N'dark brown', N'deep olive, mottled at the left arm from the infusion', N'stress-lined, pale at the lips',
        N'none',
        N'Protects his left side; positions his right toward people',
        N'Heavy canvas work clothes; left glove worn even indoors',
        N'none',
        N'Chamber maintenance shifts; avoids the infusion records room; drinks alone in the evenings.',
        N'He sold a vial of his cold-damaged blood to an Oathless alchemist named Sera Vite for three silver weights.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Chamber maintenance corridors; cliff-base settlement',
        N'0', N'0',
        N'Portuguese man mid-forties, asymmetric build, black cropped hair, dark brown eyes, Chamber corridor, worn canvas, left glove, dark fantasy',
        N'Man in canvas, black cropped hair, dark eyes, worn left glove, stone corridor, dark fantasy',
        0, 0
    );
    PRINT N'Brais Souto seeded.';
END
ELSE PRINT N'Brais Souto already exists.';
GO

-- ── 3. Catalina Vaz ───────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Catalina Vaz')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Catalina Vaz', N'catalina-vaz', N'canon', 1,
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
        @id, N'Catalina Vaz', N'catalina-vaz', N'Catalina', N'Vaz', N'',
        N'human', N'human', N'female', N'she/her',
        58, N'alive',
        N'Senior Scrying analyst; twenty-two years observing Sphere 31; her field notes are the House archive.',
        N'Twenty-two years observing Sphere 31. Her field notes are the House archive. She stopped writing everything she knows into the official logs six years ago.',
        N'The observer who crossed a line that has no name in Lacerta doctrine.',
        N'No POV.',
        N'House Lacerta; Scrying Installation, Atlantic cliff complex',
        163, 61, N'slender, composed',
        N'dark brown going silver at the temples', N'pinned back', N'medium',
        N'hazel', N'warm olive', N'clear, fine-lined',
        N'none',
        N'Still and watchful; the habit of long observation sessions in a fixed chair',
        N'Dark linen analyst''s coat; House Lacerta pin worn correctly',
        N'none',
        N'Long observation sessions; writes two logs daily — official and private, the private in a locked drawer.',
        N'Her private log names a Sphere 31 family with consistent invented names she has used for nine years.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Scrying Installation upper level; Lacerta archive wing',
        N'0', N'0',
        N'Portuguese woman late fifties, silver-templed dark hair pinned, hazel eyes, dark analyst coat, Scrying chamber interior, still posture, dark fantasy',
        N'Woman in dark analyst coat, silver-streaked pinned hair, hazel eyes, stone Scrying chamber, dark fantasy',
        0, 0
    );
    PRINT N'Catalina Vaz seeded.';
END
ELSE PRINT N'Catalina Vaz already exists.';
GO

-- ── 4. Daria Queirós ──────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Daria Queirós')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Daria Queirós', N'daria-queiros', N'canon', 1,
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
        @id, N'Daria Queirós', N'daria-queiros', N'Daria', N'Queirós', N'Dame',
        N'human', N'human', N'female', N'she/her',
        63, N'alive',
        N'Retired field Knight; advises House Lacerta''s coastal garrison on tactical deployments.',
        N'She took the Catalyst at twenty-seven. Survived when eight of her cohort did not. Thirty-six years of Knight enhancement sit in her bones. She advises the garrison with complete authority.',
        N'The veteran carrying the cost of being the cohort''s sole survivor.',
        N'No POV.',
        N'House Lacerta; cliff garrison, Atlantic coastal defense line',
        177, 82, N'broad, dense with Knight enhancement',
        N'silver-white', N'cropped very short', N'short',
        N'steel-gray', N'sun-darkened olive', N'deeply weathered, knife-scarred along the jawline',
        N'Subtle height gain (Knight)',
        N'Stands at tactical rest; spine aligned; weight evenly distributed at all times',
        N'Garrison wool, House colors; one old Knight''s clasp, nothing else',
        N'Knight-grade Catalyst infusion; increased bone density, marginal height gain, thirty-six years settled',
        N'Morning tactical reviews; garrison inspections; occasional weapons instruction she offers without being asked.',
        N'She executed Oathless man Tomás Fereira — confirmed innocent; her orders named him; she followed them and told no one.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Cliff garrison; Lacerta outer coastal defenses',
        N'0', N'0',
        N'weathered Portuguese woman, silver-white cropped hair, steel-gray eyes, garrison wool, jaw scar, cliff fortress, Knight bearing, dark fantasy',
        N'Silver-haired woman in garrison wool, jaw scar, steel-gray eyes, stone fortress, dark fantasy',
        0, 0
    );
    PRINT N'Daria Queirós seeded.';
END
ELSE PRINT N'Daria Queirós already exists.';
GO

-- ── 5. Ezpela Arrizabalaga ────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ezpela Arrizabalaga')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ezpela Arrizabalaga', N'ezpela-arrizabalaga', N'canon', 1,
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
        @id, N'Ezpela Arrizabalaga', N'ezpela-arrizabalaga', N'Ezpela', N'Arrizabalaga', N'',
        N'human', N'human', N'female', N'she/her',
        52, N'alive',
        N'Wandering hedge ritualist; moves between Lacerta cliff settlements; holds no House rank.',
        N'Basque by blood, Lacerta by long residence. No Transmutation, no allegiance. Binds pests, closes wounds, speaks at thresholds when someone is going to die. The House ignores her. The people do not.',
        N'The margin figure who performs what the House no longer bothers to perform.',
        N'No POV.',
        N'House Lacerta; coastal settlements, cliff-base communities',
        161, 58, N'compact, capable',
        N'dark brown streaked gray', N'loose or tied with cord', N'long',
        N'deep brown', N'olive-dark', N'sun-lined, practical',
        N'none',
        N'Unhurried ease of someone who has stopped worrying about arrival',
        N'Layered travel-worn wool in regional dye; carries a worn leather pack',
        N'none',
        N'Moves a circuit of six settlements; takes food and coin; sleeps in barns; rarely stays three nights.',
        N'She holds a Liturgy transit document bought from a Pallor forger six years ago for seven silver weights.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Lacerta coastal circuit; cliff-base communities, six settlements',
        N'0', N'0',
        N'Basque woman fifties, dark-gray loose long hair, olive skin, layered travel wool, Atlantic coastal path, wandering healer, dark fantasy',
        N'Woman in layered worn wool, dark-streaked gray hair, deep brown eyes, coastal path, dark fantasy',
        0, 0
    );
    PRINT N'Ezpela Arrizabalaga seeded.';
END
ELSE PRINT N'Ezpela Arrizabalaga already exists.';
GO

-- ── 6. Inés Peralba ───────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Inés Peralba')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Inés Peralba', N'ines-peralba', N'canon', 1,
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
        @id, N'Inés Peralba', N'ines-peralba', N'Inés', N'Peralba', N'',
        N'human', N'human', N'female', N'she/her',
        67, N'alive',
        N'Former lead Scrying analyst; retired after vision loss; still consulted as institutional memory.',
        N'Forty years at the observation lens took most of her sight. She navigates the Chamber from memory. The current analysts bring her transcripts. She catches things they cannot name.',
        N'Knowledge that outlasts the body that gathered it.',
        N'No POV.',
        N'House Lacerta; Scrying Installation, upper observation chamber',
        160, 55, N'thin, careful in movement',
        N'white', N'loose', N'medium',
        N'pale amber — damaged, clouded at the outer edge', N'pale olive', N'fine-lined, papery at the eyes',
        N'none',
        N'Navigates by touch and memory; moves slowly but accurately',
        N'Dark wool analyst''s coat worn to soft; House pin always straight',
        N'none',
        N'Mornings in the archive; afternoons consulting on current observations; evenings with transcripts read aloud to herself.',
        N'She sent a memo to Lord Lacerta-Vante fifteen years ago naming Sphere 31''s signals as directed contact; no reply came.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Scrying Installation archive; Chamber consultation room',
        N'0', N'0',
        N'elderly Portuguese woman, white loose hair, clouded amber eyes, dark wool analyst coat, stone archive, near-blind bearing, dark fantasy',
        N'Elderly woman in worn dark wool, white loose hair, clouded amber eyes, stone archive, dark fantasy',
        0, 0
    );
    PRINT N'Inés Peralba seeded.';
END
ELSE PRINT N'Inés Peralba already exists.';
GO

-- ── 7. Ramiro Figueira ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ramiro Figueira')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ramiro Figueira', N'ramiro-figueira', N'canon', 1,
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
        @id, N'Ramiro Figueira', N'ramiro-figueira', N'Ramiro', N'Figueira', N'',
        N'human', N'human', N'male', N'he/him',
        74, N'alive',
        N'House Lacerta''s only living Paladin; too infirm to serve; kept as a symbol and a memory.',
        N'The Catalyst elevated him at thirty-one. The war it elevated him for ended twenty years ago. He sits on the cliff terrace and watches the water with the patience of a man who has run out of enemies.',
        N'The living artifact — what a House keeps when it can no longer use what it built.',
        N'No POV.',
        N'House Lacerta; cliff estate, senior residential wing',
        192, 101, N'Paladin-enlarged, now diminished by age; his breadth persists',
        N'white', N'sparse, short', N'short',
        N'pale gray-green', N'olive, deeply sun-weathered', N'heavily lined; old Paladin scars beneath',
        N'Evident enhancement (Paladin)',
        N'Sits carefully; stands only with a staff; Paladin breadth persists',
        N'Aged formal House wool; no decorations since the war ended',
        N'Paladin-grade Catalyst infusion; height, bone mass, strength — now aged around the enhancement',
        N'Morning tea on the cliff terrace; afternoon rest; receives visitors; answers some of what they ask.',
        N'He covered the 2001 Chamber breach: rogue Scrier Anselmo Braz received a Sphere 31 reply; Ramiro chose retirement over execution.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Cliff estate residential wing; terrace overlooking the Atlantic',
        N'0', N'0',
        N'ancient Portuguese man, white sparse hair, Paladin frame aged, cliff terrace, pale gray-green eyes, House wool, survivor bearing, dark fantasy',
        N'Very old man with Paladin frame, white sparse hair, pale gray-green eyes, cliff terrace, Atlantic, dark fantasy',
        0, 0
    );
    PRINT N'Ramiro Figueira seeded.';
END
ELSE PRINT N'Ramiro Figueira already exists.';
GO

-- ── 8. Urraca del Acantilado ──────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Urraca del Acantilado')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Urraca del Acantilado', N'urraca-del-acantilado', N'canon', 1,
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
        @id, N'Urraca del Acantilado', N'urraca-del-acantilado', N'Urraca', N'del Acantilado', N'',
        N'human', N'human', N'female', N'she/her',
        39, N'alive',
        N'Oathless; tolerated at the cliff base for tide knowledge and storm warning; executed on sight inland.',
        N'No House, no name in any roll. The cliff communities call her del Acantilado because she has been here as long as anyone remembers. She knows when storms come. That is enough.',
        N'The Oathless who found the one tolerated position; the permanent exception that defines the rule.',
        N'No POV.',
        N'Oathless; Lacerta cliff base, coastal margin',
        166, 63, N'wiry, weathered',
        N'dark brown', N'salt-tangled, loose', N'long',
        N'dark gray', N'deeply tanned, sea-weathered', N'sun-cracked; keloid scar at the left cheek from an old knife',
        N'none',
        N'Quick and watchful; instinctive positioning of someone who always knows exits',
        N'Salvaged sailcloth and wool; nothing marking her to any House',
        N'none',
        N'Watches the tide; warns fishing boats; sleeps in a sea-cave alcove she has not named aloud.',
        N'She carries a sealed Sphere 31 glass bottle from a Chamber disposal tide; she believes it contains a recorded person.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Cliff base; sea-cave margin; outer fishing grounds — never inland',
        N'0', N'0',
        N'Oathless woman late thirties, salt-tangled dark hair, dark gray eyes, cliff base sea cave, salvaged sailcloth, left cheek scar, dark fantasy',
        N'Weathered woman in salvaged sailcloth, dark salt-tangled hair, dark gray eyes, cliff base, scar, dark fantasy',
        0, 0
    );
    PRINT N'Urraca del Acantilado seeded.';
END
ELSE PRINT N'Urraca del Acantilado already exists.';
GO

-- ── 9. Gonçalo Enes ───────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Gonçalo Enes')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Gonçalo Enes', N'goncalo-enes', N'canon', 1,
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
        @id, N'Gonçalo Enes', N'goncalo-enes', N'Gonçalo', N'Enes', N'',
        N'human', N'human', N'male', N'he/him',
        67, N'alive',
        N'Independent fisherman at the cliff base; no House rank; fifty years on these waters.',
        N'He has watched four Scrying administrations use the cliff waters for disposals. He knows the sites by depth. He has calculated, correctly, that silence improves his situation and telling would not.',
        N'The civilian witness who knows the cost and has chosen pragmatic silence.',
        N'No POV.',
        N'House Lacerta; cliff-base fishing settlement',
        171, 79, N'stocky, rope-worn hands',
        N'white and gray', N'close-cropped', N'short',
        N'brown', N'deep olive, salt-darkened', N'heavily weathered, cracked at the knuckles',
        N'none',
        N'Rolling forward lean from fifty years of hauling nets',
        N'Fishing wool and oilskin; boots repaired eleven times',
        N'none',
        N'Pre-dawn launch; returns by mid-morning; mends gear; drinks cider; speaks less than anyone expects.',
        N'Four Chamber disposal sites are marked in coded notation on the inside of his boat''s hull: depths, coordinates, and years.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Cliff-base fishing settlement; Atlantic coastal waters within two hours of the cliff',
        N'0', N'0',
        N'elderly Portuguese fisherman, white-gray close-cropped hair, brown eyes, deep olive skin, Atlantic cliff waters, dawn, oilskin, dark fantasy',
        N'Old fisherman in oilskin, white-gray hair, brown eyes, deep olive skin, pre-dawn cliff coast, dark fantasy',
        0, 0
    );
    PRINT N'Gonçalo Enes seeded.';
END
ELSE PRINT N'Gonçalo Enes already exists.';
GO

-- ── 10. Beatriz Landeiro ──────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Beatriz Landeiro')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Beatriz Landeiro', N'beatriz-landeiro', N'canon', 1,
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
        @id, N'Beatriz Landeiro', N'beatriz-landeiro', N'Beatriz', N'Landeiro', N'',
        N'human', N'human', N'female', N'she/her',
        46, N'alive',
        N'Chamber analyst; twenty years of Sphere 31 observation; has stopped calling what she does analysis.',
        N'She can identify seventy-three Sphere 31 individuals by gait through the observation lens. She has given them names. She has stopped calling what she does analysis.',
        N'Doctrine''s failure point — what observation becomes when the observer forgets the wall between them.',
        N'No POV.',
        N'House Lacerta; Scrying Installation, observation wing',
        162, 59, N'slight, tense',
        N'dark brown', N'straight, usually loose', N'medium',
        N'brown-black', N'warm olive', N'smooth, dark circles from long observation shifts',
        N'none',
        N'Leans forward habitually; the posture of the observation lens',
        N'Analyst coat; House pin removed two years ago without explanation',
        N'none',
        N'Double observation shifts she volunteers for; private coded notes in a notebook; meals eaten at her post.',
        N'She has a two-year-old child hidden with a cliff-base family under a false registry name; the father is uninformed.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Scrying Installation observation wing; cliff-base settlement via covert visits',
        N'0', N'0',
        N'Portuguese woman mid-forties, dark loose hair, brown-black eyes, dark circles, analyst coat without pin, Scrying chamber lens, dark fantasy',
        N'Woman in analyst coat, dark loose hair, brown-black eyes, observation chamber, haunted lean, dark fantasy',
        0, 0
    );
    PRINT N'Beatriz Landeiro seeded.';
END
ELSE PRINT N'Beatriz Landeiro already exists.';
GO

-- ── 11. Rodrigo Matos ─────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Rodrigo Matos')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Rodrigo Matos', N'rodrigo-matos', N'canon', 1,
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
        @id, N'Rodrigo Matos', N'rodrigo-matos', N'Rodrigo', N'Matos', N'',
        N'human', N'human', N'male', N'he/him',
        29, N'alive',
        N'Former infusion candidate; survived without elevation; reassigned to labor; carries permanent damage.',
        N'The Catalyst gave him permanent hand tremors and a migraine every third day without fail. He carries stone and lifts crates. He does not speak to the Knights who pass him.',
        N'Resentment given a name; the system''s discard who remains in view.',
        N'No POV.',
        N'House Lacerta; cliff estate labor compound',
        176, 74, N'strong through necessity, slightly uneven at the shoulders',
        N'black', N'loose and unkempt', N'medium',
        N'dark brown', N'olive-dark', N'pale at the lips from the migraines; stress-lined for his age',
        N'none',
        N'Tight-shouldered; holds hands in pockets to control the tremors when watched',
        N'Labor compound issue wool; nothing from before the infusion',
        N'none',
        N'Heavy labor shifts; migraine days spent in the bunk if permitted; evenings alone.',
        N'He stole the infusion logbook page naming Dame Daria Queirós as the Knight who administered his failed infusion.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Cliff estate labor compound; Chamber loading bay',
        N'0', N'0',
        N'young Portuguese man late twenties, black unkempt medium hair, dark brown eyes, labor compound wool, stone corridor, pale lips, dark fantasy',
        N'Young man in labor wool, black unkempt hair, dark brown eyes, pale lips, stone corridor, dark fantasy',
        0, 0
    );
    PRINT N'Rodrigo Matos seeded.';
END
ELSE PRINT N'Rodrigo Matos already exists.';
GO

-- ── 12. Elvira Borba ──────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Elvira Borba')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Elvira Borba', N'elvira-borba', N'canon', 1,
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
        @id, N'Elvira Borba', N'elvira-borba', N'Elvira', N'Borba', N'',
        N'human', N'human', N'female', N'she/her',
        68, N'alive',
        N'Chief Scrying archivist; forty-one years of records management; knows what was ordered destroyed.',
        N'She has indexed every observation log since the Chamber''s third expansion. She knows what was removed because she was told to remove it. She kept a copy. She always does.',
        N'The keeper of the double record — institutional memory that was not meant to survive.',
        N'No POV.',
        N'House Lacerta; Scrying Installation archive level',
        164, 62, N'compact, precise in movement',
        N'iron-gray', N'pinned tight', N'medium',
        N'dark hazel', N'pale olive', N'indoor-pale, fine-lined',
        N'none',
        N'Archivist''s precision; moves between shelves with total certainty',
        N'Dark wool; House archival badge; ink-stained at the right sleeve',
        N'none',
        N'Opens the archive before shift changes; catalogs; locks up herself; no assistants at the deep shelves.',
        N'Seven volumes of documents she was ordered to destroy are sealed in a cliff alcove below the archive stairwell.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Scrying Installation archive; cliff-face alcove below archive stairwell',
        N'0', N'0',
        N'elderly Portuguese archivist, iron-gray pinned hair, dark hazel eyes, dark wool, ink-stained sleeve, stone archive shelves, dark fantasy',
        N'Woman in dark wool, iron-gray pinned hair, dark hazel eyes, stone archive, ink-stained sleeve, dark fantasy',
        0, 0
    );
    PRINT N'Elvira Borba seeded.';
END
ELSE PRINT N'Elvira Borba already exists.';
GO

-- ── 13. Sancho Veiga ──────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Sancho Veiga')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Sancho Veiga', N'sancho-veiga', N'canon', 1,
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
        @id, N'Sancho Veiga', N'sancho-veiga', N'Sancho', N'Veiga', N'',
        N'human', N'human', N'male', N'he/him',
        55, N'alive',
        N'Itinerant hedge ritualist; serves cliff-base communities; holds no allegiance and accepts no station.',
        N'No fixed address and no desire for one. Goes where the dying need a threshold-keeper and the sick need a word said correctly. Takes food, coin when offered, leaves.',
        N'The liturgical function that outlasts the institution — ritual as survival tool.',
        N'No POV.',
        N'None declared; Lacerta margin communities',
        168, 71, N'average, slightly stooped from years on the road',
        N'gray-brown', N'shaggy', N'medium',
        N'pale green', N'sallow, road-weathered', N'uneven, sun-mottled',
        N'none',
        N'The stoop of long travel; comfortable crouching; stands for ceremony',
        N'Layered old wool, travel-worn; boots repaired with mismatched leather',
        N'none',
        N'Walks between communities; performs rites for births and deaths; sleeps in barns or doorways.',
        N'He witnessed the Liturgy taking of Aldona Freis at Canto Fundo village, reported nothing, and avoids that road entirely.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Lacerta cliff-base coastal margin; itinerant across six communities',
        N'0', N'0',
        N'itinerant Portuguese hedge ritualist, gray-brown shaggy hair, pale green eyes, layered travel wool, Atlantic coastal path, road stoop, dark fantasy',
        N'Man in layered worn wool, gray-brown shaggy hair, pale green eyes, coastal stone path, travel bundle, dark fantasy',
        0, 0
    );
    PRINT N'Sancho Veiga seeded.';
END
ELSE PRINT N'Sancho Veiga already exists.';
GO

-- ── 14. Constança Lousada ─────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Constança Lousada')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Constança Lousada', N'constanca-lousada', N'canon', 1,
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
        @id, N'Constança Lousada', N'constanca-lousada', N'Constança', N'Lousada', N'',
        N'human', N'human', N'female', N'she/her',
        78, N'alive',
        N'Eldest civilian at the cliff community; no formal role; consulted on death and things no one else will say.',
        N'She has outlived three Paladins, two Lords, and one Chamber collapse. She does not consider herself notable. She considers herself still here, which she has learned is a different thing entirely.',
        N'Duration as evidence — the witness who has persisted beyond every system built around her.',
        N'No POV.',
        N'House Lacerta; cliff community, oldest residential quarter',
        155, 48, N'very slight, diminished by age',
        N'white', N'loose', N'short',
        N'brown, heavy-lidded', N'olive, paper-thin at the backs of the hands', N'deeply lined, translucent at the temples',
        N'none',
        N'Seated more than standing; moves with great deliberateness; uses a stick',
        N'Plain dark wool mended many times; a forty-year shawl',
        N'none',
        N'Sits at her window in the morning; receives visitors; sleeps afternoons; watches Chamber lights at night.',
        N'She overheard a Scrier say the Lacerta Chamber''s Sphere 31 records were already copied in full from the other side.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Cliff community residential quarter; the window overlooking the Chamber approach',
        N'0', N'0',
        N'very old Portuguese woman, white loose short hair, brown heavy-lidded eyes, dark mended wool shawl, cliff community window, translucent, dark fantasy',
        N'Ancient woman at stone window, white short hair, brown eyes, dark wool shawl, cliff community, dark fantasy',
        0, 0
    );
    PRINT N'Constança Lousada seeded.';
END
ELSE PRINT N'Constança Lousada already exists.';
GO

-- ── 15. Ferran Xuclà ──────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ferran Xuclà')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ferran Xuclà', N'ferran-xucla', N'canon', 1,
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
        @id, N'Ferran Xuclà', N'ferran-xucla', N'Ferran', N'Xuclà', N'',
        N'human', N'human', N'male', N'he/him',
        43, N'alive',
        N'Provisional charter-holder at the sea edge; operates outside House law; tolerated for maritime utility.',
        N'A charter that is not entirely legitimate and everyone who matters knows it. He runs the cliff coast in a flat-bottomed boat and does not discuss his passenger manifest.',
        N'The necessary transgressor — the person the institution needs but cannot formally acknowledge.',
        N'No POV.',
        N'Lacerta coastal margin; Catalan origin',
        173, 76, N'lean and salt-hardened',
        N'dark brown', N'short, wind-pushed', N'short',
        N'dark green', N'olive, heavily tanned', N'sun-cracked; rope-burn scar across the right palm',
        N'none',
        N'Balance-stance of a man accustomed to small boat decks',
        N'Worn nautical wool and oilskin; no House markings anywhere',
        N'none',
        N'Runs coastal passages nobody asks questions about; never anchors in the same place two nights running.',
        N'He has ferried seventeen Oathless refugees around the cliff face in three years; the last three were children.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Lacerta cliff coast; coastal anchorages; sea-edge margins; northern fishing communities',
        N'0', N'0',
        N'Catalan sea-edge smuggler, dark brown wind-pushed hair, dark green eyes, oilskin, Atlantic cliff anchorage at dusk, dark fantasy',
        N'Lean man in oilskin, dark brown wind-pushed hair, dark green eyes, cliff anchorage dusk, dark fantasy',
        0, 0
    );
    PRINT N'Ferran Xuclà seeded.';
END
ELSE PRINT N'Ferran Xuclà already exists.';
GO
