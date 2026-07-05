SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ============================================================
-- HOUSE OPHIUCHUS — LOWER HIERARCHY SEED (PART B)
-- The Cauld | UniverseId: 0197E9C9-0002-7000-8000-000000000002
-- Generated: 2026-07-05
-- Scrying Staff (4) + Domestic Staff (5) + Oathless (1) = 10 total
-- Vigil Seat operators, estate household, former Myrmidon now deniable.
-- ============================================================

-- 1. Benedetta Alori — Head Scrying Operator, Vigil Seat
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Benedetta Alori')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Benedetta Alori', N'benedetta-alori', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Benedetta Alori', N'benedetta-alori', N'Benedetta', N'Alori', N'', N'human', N'human',
        N'female', N'she/her', 65, N'alive',
        N'Head Scrying Operator, Vigil Seat',
        N'Forty years watching Sphere Seven at the Vigil Seat without rotation. Her observational logs fill two storage rooms. Her private theory — the Sphere watches back — exists nowhere but her own mind.',
        N'Living archive of Sphere Seven; her unwritten theory is the story''s buried revelation.',
        N'No POV.',
        N'House Ophiuchus; southern peninsula',
        162, 54, N'slight, weathered',
        N'white', N'pulled back, pinned', N'medium',
        N'dark brown', N'olive', N'deeply lined; the face of long watches',
        N'none',
        N'Economy of stillness; forty years of observation have made all other movement feel wasteful.',
        N'Plain watch-robes in undyed wool; a heavy layer regardless of season; nothing that catches the eye.',
        N'none',
        N'Twelve-hour watches, meticulous log entries, training junior operators, correcting their assumptions about what they see.',
        N'Believes the Sphere has altered her perception of time; she no longer trusts her own chronology.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Ophiuchus estate; the Vigil Seat installation, northern Ridge',
        N'0', N'0',
        N'aged Italian woman watching glowing sphere stone chamber candlelight',
        N'elderly Italian woman observing luminous sphere stone chamber',
        0, 0
    );
    PRINT 'Benedetta Alori seeded.';
END
ELSE PRINT 'Benedetta Alori already exists.';
GO

-- 2. Ferrante Caschi — Long-watch Scrying Operator (believes the apparatus shows truth)
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ferrante Caschi')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ferrante Caschi', N'ferrante-caschi', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Ferrante Caschi', N'ferrante-caschi', N'Ferrante', N'Caschi', N'', N'human', N'human',
        N'male', N'he/him', 45, N'alive',
        N'Long-watch Scrying Operator, Vigil Seat; twenty years on the third watch',
        N'Twenty years on the Vigil Seat''s third watch. He annotates every anomaly as confirmed fact. The apparatus cannot lie — only the interpreter can err. He has written this in his logs four hundred times.',
        N'Foil to Ornella; his certainty makes him dangerous to anyone who challenges the record.',
        N'No POV.',
        N'House Ophiuchus; southern peninsula',
        178, 79, N'lean',
        N'dark brown going grey', N'close-cut', N'short',
        N'dark brown', N'warm olive', N'clear',
        N'none',
        N'Precise and settled in his observation station; annotates without looking up; rarely leaves his position mid-watch.',
        N'Practical watch-wool, always clean; House Ophiuchus colors at the collar only.',
        N'none',
        N'Night watch rotation, cross-referencing logs, filing written disputes against Ornella''s interpretation margin notes.',
        N'His certainty is armor; he fears that doubt would collapse twenty years of accumulated meaning.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Ophiuchus estate; the Vigil Seat installation, northern Ridge',
        N'0', N'0',
        N'middle-aged Italian man annotating manuscripts stone observation chamber intent',
        N'middle-aged Italian man annotating observation logs candlelight',
        0, 0
    );
    PRINT 'Ferrante Caschi seeded.';
END
ELSE PRINT 'Ferrante Caschi already exists.';
GO

-- 3. Ornella Merisi — Long-watch Scrying Operator (believes the apparatus shows desire, not truth)
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ornella Merisi')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ornella Merisi', N'ornella-merisi', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Ornella Merisi', N'ornella-merisi', N'Ornella', N'Merisi', N'', N'human', N'human',
        N'female', N'she/her', 43, N'alive',
        N'Long-watch Scrying Operator, Vigil Seat; twenty years on the day watch',
        N'Twenty years watching the same apparatus as Ferrante; her conclusion is the opposite. The Vigil Seat shows the observer''s desire, not the Sphere''s nature. She has written this nowhere.',
        N'Her skepticism is the lens that breaks open the central Scrying question.',
        N'No POV.',
        N'House Ophiuchus; southern peninsula',
        165, 60, N'slight',
        N'black', N'practical, loosely knotted', N'medium',
        N'dark brown', N'olive', N'clear',
        N'none',
        N'Quieter at her station than Ferrante; watches the apparatus the way one watches a person, not a machine.',
        N'Plain watch-wool; she stopped wearing Ophiuchus trim eight years ago and no one has asked why.',
        N'none',
        N'Day watch rotation, quiet observation, maintaining private coded notes that contradict the official record.',
        N'Fourteen months ago she saw something that confirmed her theory; she destroyed that log entry.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Ophiuchus estate; the Vigil Seat installation, northern Ridge',
        N'0', N'0',
        N'dark-haired Italian woman skeptical expression stone Scrying chamber Cauld',
        N'skeptical Italian woman stone observation chamber dark hair',
        0, 0
    );
    PRINT 'Ornella Merisi seeded.';
END
ELSE PRINT 'Ornella Merisi already exists.';
GO

-- 4. Taddeo Brivio — Technical Maintenance Chief, Vigil Seat
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Taddeo Brivio')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Taddeo Brivio', N'taddeo-brivio', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Taddeo Brivio', N'taddeo-brivio', N'Taddeo', N'Brivio', N'', N'human', N'human',
        N'male', N'he/him', 50, N'alive',
        N'Technical Maintenance Chief, Vigil Seat apparatus',
        N'Keeps the Vigil Seat apparatus operational. Has replaced every component twice without fully understanding what it does. The observers'' conclusions are none of his business, and he intends to keep it that way.',
        N'Practical keeper of the machinery; his studied neutrality makes him uniquely trustworthy to all parties.',
        N'No POV.',
        N'House Ophiuchus; southern peninsula',
        175, 88, N'stocky',
        N'brown', N'close-cropped', N'short',
        N'hazel', N'olive', N'weathered, work-roughened',
        N'none',
        N'Moves through the apparatus chamber like a man who owns it; the observers give him room instinctively.',
        N'Work leathers over undyed wool; tool loops worn in from years of the same seven tools.',
        N'none',
        N'Pre-dawn calibrations, component inventory, training an apprentice he has not yet decided to trust.',
        N'Deliberately delays one quarterly calibration so operators must request his help; he needs to feel indispensable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Ophiuchus estate; the Vigil Seat installation and maintenance corridors',
        N'0', N'0',
        N'stocky Italian craftsman repairing arcane apparatus stone corridor tools',
        N'stocky craftsman repairing arcane apparatus stone corridor',
        0, 0
    );
    PRINT 'Taddeo Brivio seeded.';
END
ELSE PRINT 'Taddeo Brivio already exists.';
GO

-- 5. Gaspare Velardi — Seneschal / Head Steward
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Gaspare Velardi')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Gaspare Velardi', N'gaspare-velardi', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Gaspare Velardi', N'gaspare-velardi', N'Gaspare', N'Velardi', N'', N'human', N'human',
        N'male', N'he/him', 87, N'alive',
        N'Seneschal; Head Steward, House Ophiuchus estate',
        N'Has administered the Ophiuchus estate since before Lord Orazio was born. He has watched three successions and considers each incoming Lord or Lady a temporary interruption he will outlast.',
        N'Institutional memory of House Ophiuchus; his loyalty belongs to the House, not its current holder.',
        N'No POV.',
        N'House Ophiuchus; southern peninsula',
        170, 62, N'lean, age-stooped',
        N'white', N'sparse, neat', N'short',
        N'dark brown', N'olive', N'deeply lined; a face that has outlasted most people it has known',
        N'none',
        N'A very old man who moves as though the building owes him passage; no detours, no concessions.',
        N'House Ophiuchus formal colors, impeccable; the same style for sixty years; nothing has needed changing.',
        N'none',
        N'Morning household briefings, managing estate accounts, mediating staff disputes with sixty-eight years of institutional authority.',
        N'Forty years ago he destroyed a deed proving a rival branch had prior claim to the estate.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Ophiuchus estate, all wings and outbuildings; does not leave',
        N'0', N'0',
        N'very elderly Italian steward formal estate hall authority white hair',
        N'very elderly Italian steward formal estate hall authoritative',
        0, 0
    );
    PRINT 'Gaspare Velardi seeded.';
END
ELSE PRINT 'Gaspare Velardi already exists.';
GO

-- 6. Serafina Ardito — Head Cook
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Serafina Ardito')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Serafina Ardito', N'serafina-ardito', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Serafina Ardito', N'serafina-ardito', N'Serafina', N'Ardito', N'', N'human', N'human',
        N'female', N'she/her', 62, N'alive',
        N'Head Cook, House Ophiuchus estate; thirty-five years in the kitchen',
        N'Thirty-five years feeding the Ophiuchus household. She knows which family members cannot keep secrets — they eat too fast under pressure. She has no opinion of greatness.',
        N'Ground-level observer; her practical contempt cuts through the House''s scholarly self-regard.',
        N'No POV.',
        N'House Ophiuchus; southern peninsula',
        158, 72, N'broad',
        N'grey', N'pinned up', N'medium (pinned)',
        N'dark brown', N'warm olive', N'weathered from heat; burn scars on both forearms',
        N'none',
        N'Efficient and entirely without ceremony; the kitchen floor has more authority than anyone in the main hall.',
        N'Practical wool under a heavy apron, kitchen-burnt at the hems for thirty years; she has not noticed.',
        N'none',
        N'Pre-dawn market orders, three meals daily, supervising four kitchen staff she considers broadly incompetent.',
        N'For eleven years she has secretly fed a debt-enslaved neighboring family the House does not know exists.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Ophiuchus estate kitchen, market, and supply routes',
        N'0', N'0',
        N'older Italian woman stone kitchen hearth practical cook apron firelight',
        N'older Italian cook stone kitchen hearth firelight apron',
        0, 0
    );
    PRINT 'Serafina Ardito seeded.';
END
ELSE PRINT 'Serafina Ardito already exists.';
GO

-- 7. Valerio Ponti — Butler
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Valerio Ponti')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Valerio Ponti', N'valerio-ponti', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Valerio Ponti', N'valerio-ponti', N'Valerio', N'Ponti', N'', N'human', N'human',
        N'male', N'he/him', 55, N'alive',
        N'Butler, House Ophiuchus estate',
        N'The House''s public face within its own walls. He has memorized every family member''s return hour for twelve years. He draws no conclusions. He keeps excellent records.',
        N'Inadvertent intelligence asset; his household records document every late return without comment or judgment.',
        N'No POV.',
        N'House Ophiuchus; southern peninsula',
        180, 76, N'upright, lean',
        N'silver', N'combed precisely', N'short',
        N'dark', N'pale olive', N'clear',
        N'none',
        N'Upright always; the bearing of someone who believes the House''s dignity requires a physical manifestation.',
        N'House Ophiuchus formal livery, pressed to an edge that guests notice even when the family no longer does.',
        N'none',
        N'Greeting arrivals, coordinating household staff, maintaining the evening household log with precise entry times.',
        N'For seven years has destroyed every record of one family member''s late returns, out of private loyalty.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Ophiuchus estate, all public rooms and entry points',
        N'0', N'0',
        N'silver-haired Italian butler formal posture stone estate entrance hall',
        N'silver-haired butler formal posture estate entrance hall',
        0, 0
    );
    PRINT 'Valerio Ponti seeded.';
END
ELSE PRINT 'Valerio Ponti already exists.';
GO

-- 8. Erminia Corsi — Groundskeeper
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Erminia Corsi')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Erminia Corsi', N'erminia-corsi', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Erminia Corsi', N'erminia-corsi', N'Erminia', N'Corsi', N'', N'human', N'human',
        N'female', N'she/her', 67, N'alive',
        N'Groundskeeper, House Ophiuchus estate; forty years on the grounds',
        N'Forty years tending the Ophiuchus estate grounds. She has never been asked what she has found buried there. Nobody has thought to ask. She has never volunteered.',
        N'Silent custodian of the House''s buried evidence; her knowledge is explosive if extracted.',
        N'No POV.',
        N'House Ophiuchus; southern peninsula',
        160, 63, N'wiry',
        N'salt-and-pepper', N'practical, loose', N'medium',
        N'grey-green', N'olive, sun-dark', N'deeply weathered; earth-ingrained hands',
        N'none',
        N'Sun-hardened and deliberate; moves around the grounds with the ease of someone navigating their own mind.',
        N'Heavy work wool, earth-stained permanently; carries tools she has had since her second year on the grounds.',
        N'none',
        N'Pre-dawn grounds walk, tool inventory, directing two younger groundskeepers who do not ask enough questions.',
        N'Knows three things are buried on the grounds that should not exist; one was buried last winter.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Ophiuchus estate grounds, gardens, and boundary walls',
        N'0', N'0',
        N'weathered Italian woman tending estate grounds dawn worn tools',
        N'weathered Italian woman estate grounds dawn worn tools',
        0, 0
    );
    PRINT 'Erminia Corsi seeded.';
END
ELSE PRINT 'Erminia Corsi already exists.';
GO

-- 9. Nunzio Fabri — Laundry Master
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Nunzio Fabri')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Nunzio Fabri', N'nunzio-fabri', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Nunzio Fabri', N'nunzio-fabri', N'Nunzio', N'Fabri', N'', N'human', N'human',
        N'male', N'he/him', 52, N'alive',
        N'Laundry Master, House Ophiuchus estate; twenty years reading pocket correspondence',
        N'Twenty years managing Ophiuchus estate laundry. Pocket correspondence passes through his hands daily. He has never discussed what he reads, but he forgets nothing.',
        N'Inadvertent intelligence archive; his specific discovery makes him a liability to the Seneschal.',
        N'No POV.',
        N'House Ophiuchus; southern peninsula',
        172, 80, N'medium',
        N'black going grey', N'neat, short', N'short',
        N'brown', N'olive', N'clear',
        N'none',
        N'Economical and practiced; folds by muscle memory; hands always busy; eyes steady and taking in everything.',
        N'Clean plain wool over a laundry smock; his own clothes are always pressed; he considers this professional.',
        N'none',
        N'Pre-dawn sorting, supervising two laundresses, returning garments pressed and pockets always empty.',
        N'Found a letter Gaspare wrote forty years ago admitting he destroyed a prior-claim deed; has kept it.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Ophiuchus estate laundry and household textile rooms',
        N'0', N'0',
        N'Italian laundry master folding estate linens stone basement focused',
        N'Italian laundry master folding linens stone basement focused',
        0, 0
    );
    PRINT 'Nunzio Fabri seeded.';
END
ELSE PRINT 'Nunzio Fabri already exists.';
GO

-- 10. Celestina Morra — Former Ophiuchus Myrmidon; Oathless
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Celestina Morra')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Celestina Morra', N'celestina-morra', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Celestina Morra', N'celestina-morra', N'Celestina', N'Morra', N'', N'human', N'human',
        N'female', N'she/her', 38, N'alive',
        N'Former Ophiuchus Myrmidon; Oathless; deniable operative',
        N'Removed from the Ophiuchus Myrmidon roster three years ago without stated cause. The House still pays her. Her work has no official record and leaves none.',
        N'Deniable instrument of House policy; she does what the scholars cannot be seen doing.',
        N'No POV.',
        N'House Ophiuchus; southern peninsula',
        167, 63, N'athletic',
        N'dark brown', N'close-cut', N'short',
        N'dark brown', N'warm olive', N'clear; a scar at the left wrist she does not explain',
        N'none',
        N'Military bearing she has made civilian-looking but not erased; her stillness is not domestic stillness.',
        N'Unremarkable traveling wool; nothing that announces a role; no House colors; left wrist always covered.',
        N'none',
        N'No fixed schedule; arrives when summoned; reports to one person whose name she will not say.',
        N'Knows her last operation was a murder, not an extraction; she accepted payment without asking questions.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'No permanent territory; wherever summoned; never identified at the scene',
        N'0', N'0',
        N'Italian ex-soldier woman plain clothes watchful alert estate shadows',
        N'watchful Italian ex-soldier plain clothes estate shadows',
        0, 0
    );
    PRINT 'Celestina Morra seeded.';
END
ELSE PRINT 'Celestina Morra already exists.';
GO
