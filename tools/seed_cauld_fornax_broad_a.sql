SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- HOUSE FORNAX — ACTIVE POPULATION BATCH A  (15 characters)
-- Universe: Cauld  (ID 0197E9C9-0002-7000-8000-000000000002)
-- 2026-07-05 · Idempotent (IF NOT EXISTS guards on all inserts)
-- Rhine-Danube; Germany analog; industrial and methodical.
-- Categories: Soldiers / Knights / Paladin (5) · Scrying operators (3)
--   Scholars (2) · Artisans (1) · Merchants (1) · Intelligence (2) · Young noble (1)
-- ═══════════════════════════════════════════════════════════════════════════════

-- ── 1. Liesel Dorn ───────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Liesel Dorn')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Liesel Dorn', N'liesel-dorn', N'canon', 1,
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
        @id, N'Liesel Dorn', N'liesel-dorn', N'Liesel', N'Dorn', N'',
        N'human', N'human', N'female', N'she/her',
        26, N'alive',
        N'Third-year Scrying operator; western Rhine Installation; specialises in Sphere 31 industrial mapping.',
        N'Liesel Dorn watches a parallel world through instruments she doesn''t fully understand and transcribes what she sees into a report language designed to make the impossible sound routine. She is good at her job and getting better, which means she is increasingly aware of what her supervisors don''t pass up the chain. She found the discrepancy eighteen months into her post: a set of Sphere 31 coordinates she had recorded twice, once in her log and once in a file she had never filed. The second file had someone else''s notation on it. She has told no one. She is still deciding what the silence is buying her.',
        N'The operator who sees too much and is learning whether knowledge is currency or liability.',
        N'No POV.',
        N'House Fornax; Rhine Scrying Installation, western observation post',
        163, 54, N'slight, fine-boned',
        N'dark brown', N'pinned bun', N'medium',
        N'grey', N'medium olive', N'clear, ink-smudged at the temple',
        N'none',
        N'hunched forward at the observation frame; holds tension in the shoulders; rarely blinks during active sessions',
        N'dark wool operator''s uniform, ink-stained cuffs, no ornamentation',
        N'none',
        N'Twelve-hour observation shifts transcribing Sphere 31 readings; eats at her station; walks the installation perimeter once each night to clear her eyes.',
        N'Fourteen months ago she intercepted a signal sequence from Sphere 31 showing Fornax trade delegation insignia alongside Liturgy acquisition glyphs in the same frame — a meeting neither party is supposed to have had. She logged it in her personal cipher rather than the official record and has not submitted it. She is waiting to understand what it means before deciding who it harms to know.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Rhine Scrying Installation, western wing',
        N'0', N'0',
        N'young Germanic woman in dark wool uniform, hunched over a glowing medieval scrying apparatus, stone observation chamber, ink-stained hands, candlelight, Buehlman dark fantasy --ar 2:3',
        N'A 26-year-old woman in dark wool at a glowing medieval observation instrument, grey eyes, focused and wary, candlelit stone chamber',
        0, 0
    );
    PRINT N'Liesel Dorn seeded.';
END
ELSE PRINT N'Liesel Dorn already exists.';
GO

-- ── 2. Elke Vrain ────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Elke Vrain')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Elke Vrain', N'elke-vrain', N'canon', 1,
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
        @id, N'Elke Vrain', N'elke-vrain', N'Elke', N'Vrain', N'Master',
        N'human', N'human', N'female', N'she/her',
        41, N'alive',
        N'Master gunsmith at the Great Furnaces; designs Paladin-grade crossbow mechanisms; Fornax''s most decorated living armorer.',
        N'Elke Vrain learned her trade from her father and surpassed him by twenty-three. She makes weapons for people who will use them to kill other people, and she has made a permanent arrangement with this fact — it sits in her chest where guilt would sit in someone else, and it does not move. What moves her is the work. She prototypes at night when the Furnace quiets, testing mechanisms no one has commissioned, following problems to their end because problems have ends and she enjoys finding them. The Furnace owns her labor. It doesn''t own what she invents after dark.',
        N'The artisan whose loyalty is to craft first; the institution benefits but doesn''t command her.',
        N'No POV.',
        N'House Fornax; Great Furnace district, southern smithing quarter',
        172, 78, N'broad-shouldered, forge-hardened',
        N'ash blonde', N'tied back loose', N'long',
        N'pale blue', N'fair', N'heat-scarred, permanently ruddy at the cheeks',
        N'none',
        N'stands at the forge like she owns it; unhurried; never raises her voice near a flame',
        N'leather apron over heavy wool, forge-blackened gloves tucked in belt, no ornament except a copper punch-mark pin she designed herself',
        N'none',
        N'Dawn to dark at the main Furnace on commission work; private workshop after dark on unlicensed prototypes; eats when she remembers.',
        N'She has completed a Paladin-grade repeating crossbow mechanism she has never logged with the Furnace inventory office. It is hidden in a false floor in her private workshop. She has received an offer from an Oathless arms runner — more than a year''s wages in Rhine-weight silver — and has not refused it. She has also not yet delivered the weapon. She is not certain she will.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Great Furnace district, Fornax heartland',
        N'0', N'0',
        N'muscular Germanic woman in leather apron at a great medieval forge, ash blonde hair tied back, forge-glow, soot and heat, Buehlman dark fantasy --ar 2:3',
        N'A 41-year-old broad-shouldered woman in leather apron, pale blue eyes, ash blonde hair, medieval forge glow, serious expression',
        0, 0
    );
    PRINT N'Elke Vrain seeded.';
END
ELSE PRINT N'Elke Vrain already exists.';
GO

-- ── 3. Dietmar Hauf ──────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Dietmar Hauf')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Dietmar Hauf', N'dietmar-hauf', N'canon', 1,
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
        @id, N'Dietmar Hauf', N'dietmar-hauf', N'Dietmar', N'Hauf', N'Knight',
        N'human', N'human', N'male', N'he/him',
        34, N'alive',
        N'Knight; eastern Rhine border patrol; Transmutation survivor; rated marksman; keeps to himself at campfires.',
        N'Dietmar Hauf is competent, conscientious, and the kind of soldier who improves any unit he joins, which is why no one has looked too closely at how he got here. He survived the Catalyst on his third application — which everyone in his unit finds admirable and which is not what happened. He is a man who has made a specific calculation about what survival is worth, and he made it in the infirmary anteroom when no one else was present, and his brother was still alive to be moved from one cot to another. He does his job very well. He has been doing it very well for seven years.',
        N'The soldier who earned his rank through an act he has never named and cannot undo.',
        N'No POV.',
        N'House Fornax; Rhine border patrol zone, eastern crossing',
        196, 108, N'heavily built, wide-framed',
        N'dark brown', N'close-cropped', N'short',
        N'dark brown, left pupil permanently dilated', N'pale', N'weathered, fine scar tissue at left jaw',
        N'Subtle height gain (Knight)',
        N'wide stance; occupies space deliberately; never positions himself with his back uncovered',
        N'Knight''s plate with Rhine garrison markings, no decorative elements, helmet carried rather than worn when off-duty',
        N'Transmutation: Catalyst infusion, Rhine garrison batch, survived; height increase of 13 cm post-infusion; permanent left-pupil dilation, Transmutation marker.',
        N'Border patrol rotations; weekly weapons inspection; trains junior soldiers in blade technique; sits at the edge of campfires and does not explain why.',
        N'His brother Erwin was assigned to the same Catalyst cohort. Dietmar paid the infirmary attendant three months'' wages in Rhine silver to swap their batch assignments before infusion. Erwin received the experimental extraction lot. He died on the table within the hour. The attendant accepted the payment, made no written record of the swap, and was killed in a Liturgy border raid six months later. Dietmar''s service record is clean.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Rhine border patrol zone, eastern crossing',
        N'0', N'0',
        N'tall Germanic knight in heavy plate armor, dark cropped hair, one dilated pupil, Rhine river border, mist and stone, Buehlman dark fantasy --ar 2:3',
        N'A 34-year-old tall knight in heavy plate, dark hair, one dilated pupil, stern expression, Rhine river crossing',
        0, 0
    );
    PRINT N'Dietmar Hauf seeded.';
END
ELSE PRINT N'Dietmar Hauf already exists.';
GO

-- ── 4. Gisela Wendl ──────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Gisela Wendl')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Gisela Wendl', N'gisela-wendl', N'canon', 1,
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
        @id, N'Gisela Wendl', N'gisela-wendl', N'Gisela', N'Wendl', N'Scholar',
        N'human', N'human', N'female', N'she/her',
        48, N'alive',
        N'Transmutation failure researcher; Great Furnace annex; cross-references batch records against autopsy findings; publishes nothing she discovers.',
        N'Gisela Wendl has spent seventeen years reading the records of the dead to understand why they died and what could have been done differently. The answer is always the same and she has stopped writing it in her official reports. She keeps a private ledger in cipher. She has excellent institutional relationships, a spotless research record, and a mind that will not stop following numbers to where they lead. The numbers lead somewhere she is not ready to go.',
        N'The scholar who has found the institutional crime and is choosing the cost of knowing it.',
        N'No POV.',
        N'House Fornax; Transmutation research annex, Great Furnace complex',
        159, 62, N'slight, scholar''s frame',
        N'iron grey', N'neat bun', N'medium',
        N'brown', N'medium', N'indoor-pale, habitually narrowed eyes',
        N'none',
        N'very still; watchful; rarely gestures; has a habit of tilting her head slightly left when she has found an inconsistency',
        N'dark wool scholar''s robe, quill callus on right hand, measurement marks inked on left cuff',
        N'none',
        N'Reads Transmutation autopsy reports in the morning; cross-references against batch records in the afternoon; writes in the private cipher ledger after dark.',
        N'Her private ledger documents eighteen consecutive Catalyst batches — spanning four years — where conscript candidates received adulterated Xerum 525 while noble candidates from the same cohorts received verified pure extraction stock. The authorization signature on the adulterated batch purchase orders belongs to Paladin Albrecht Vogt. She identified this cross-reference nine months ago and has said nothing to anyone.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Transmutation research annex, Great Furnace complex',
        N'0', N'0',
        N'iron-haired Germanic scholarly woman in dark wool robe, candlelit stone study, stacked ledgers and anatomical diagrams, reading glasses, Buehlman dark fantasy --ar 2:3',
        N'A 48-year-old woman scholar in dark wool robe at a candlelit desk stacked with research ledgers, iron-grey bun, focused and still',
        0, 0
    );
    PRINT N'Gisela Wendl seeded.';
END
ELSE PRINT N'Gisela Wendl already exists.';
GO

-- ── 5. Werner Krauss ─────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Werner Krauss')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Werner Krauss', N'werner-krauss', N'canon', 1,
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
        @id, N'Werner Krauss', N'werner-krauss', N'Werner', N'Krauss', N'',
        N'human', N'human', N'male', N'he/him',
        52, N'alive',
        N'Rhine merchant; controls three river crossings; senior delegate to the Fornax trade council; known for hospitality and punctual delivery.',
        N'Werner Krauss is the kind of man whose company people enjoy and whose reliability they depend on, which makes him one of the most useful people on either side of the Rhine. He knows this. He has been trading long enough to understand that every crossing has a price and the most important thing is to know who sets it. He is warm, punctual, and genuinely generous with his table. He is also, in a very precise sense, not working for one employer.',
        N'The merchant whose reliability is his cover; the institution trusts him precisely because he has never failed it.',
        N'No POV.',
        N'House Fornax; Rhine river crossing town, three-route junction',
        178, 91, N'heavyset, prosperous',
        N'salt-and-pepper', N'combed, parted', N'short',
        N'hazel', N'ruddy', N'wind-burnt, deeply creased around the eyes',
        N'none',
        N'expansive; takes the full space of a room; handshakes firm and held a beat longer than strictly necessary',
        N'merchant''s good wool, Fornax trade seal on belt clasp, fur-trimmed collar in colder months, always dressed slightly better than the occasion requires',
        N'none',
        N'Manages three river crossings and their toll ledgers; hosts visiting traders three evenings each week; delivers weekly market reports to the Furnace administrators.',
        N'For six years he has been passing Fornax weapons specifications to a House Calyx intermediary through a laundress on the northern transit route who believes she is carrying correspondence for a wool syndicate. The intermediary pays in Calyx iron-weight credit notes he cannot spend outside Calyx-controlled markets. He has not spent a single note. He keeps them in a sealed box under the floorboards of his river crossing office and does not know what he intends to do with them.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Rhine river crossing town, three-route junction',
        N'0', N'0',
        N'heavyset Germanic merchant in good wool coat, salt-and-pepper hair, Fornax trade seal belt clasp, Rhine river trading town, candlelit hall, Buehlman dark fantasy --ar 2:3',
        N'A 52-year-old prosperous merchant in good wool coat, hazel eyes, salt-and-pepper hair, warm expression, medieval Rhine river trading hall',
        0, 0
    );
    PRINT N'Werner Krauss seeded.';
END
ELSE PRINT N'Werner Krauss already exists.';
GO

-- ── 6. Adelheid Roth ─────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Adelheid Roth')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Adelheid Roth', N'adelheid-roth', N'canon', 1,
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
        @id, N'Adelheid Roth', N'adelheid-roth', N'Adelheid', N'Roth', N'',
        N'human', N'human', N'female', N'she/her',
        31, N'alive',
        N'Fornax border intelligence officer; runs a four-person informant network along Liturgy transit thresholds; officially reports only to Rhine command.',
        N'Adelheid Roth is very good at being ordinary. She shifts clothes, accent, and name with context the way other people shift expression, and she has been doing it long enough that she is no longer certain which version is the first one. She works for Fornax intelligence. She works, in parallel, for the Liturgy. She was turned three years ago. The price was her younger sister''s release from a taking. She was shown documentation confirming the release. She does not believe the documentation.',
        N'The double agent whose loyalty was bought with a proof of life she cannot verify.',
        N'No POV.',
        N'House Fornax; western border intelligence post, Fornax-Liturgy threshold',
        167, 61, N'lean, deliberately unremarkable',
        N'auburn', N'worn loose when traveling, pinned when reporting', N'medium',
        N'pale green', N'medium fair', N'unremarkable; designed to be',
        N'none',
        N'moves to be forgotten; deliberate ordinariness; posture and gait shift with whatever role she is wearing',
        N'shifts with context: Fornax border official''s wool one day, traveling merchant''s plain clothes the next; nothing that names her',
        N'none',
        N'Runs four informants along Liturgy transit borders; submits weekly Fornax intelligence reports; maintains a Liturgy dead drop at a waystation granary two hours east.',
        N'Three years ago Liturgy agents approached her with documentation showing her sister Maren — taken in a border acquisition eighteen months earlier — alive and listed as voluntarily laboring in a Liturgy grain district. The document bore a Liturgy acquisition officer''s seal she was not able to authenticate. She agreed to work for them in exchange for Maren''s continued safety. She has not found a way to confirm whether her sister is alive, or whether the dead drop she maintains still has a reader on the other end.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Western border intelligence post; Liturgy transit threshold; waystation dead drop route',
        N'0', N'0',
        N'unremarkable auburn-haired woman in traveling clothes at a medieval stone waystation, pale green eyes, deliberate stillness, border road, Buehlman dark fantasy --ar 2:3',
        N'A 31-year-old woman in plain traveling clothes at a medieval stone waystation, auburn hair loose, pale green eyes, expression carefully neutral',
        0, 0
    );
    PRINT N'Adelheid Roth seeded.';
END
ELSE PRINT N'Adelheid Roth already exists.';
GO

-- ── 7. Manfred Stein ─────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Manfred Stein')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Manfred Stein', N'manfred-stein', N'canon', 1,
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
        @id, N'Manfred Stein', N'manfred-stein', N'Manfred', N'Stein', N'',
        N'human', N'human', N'male', N'he/him',
        19, N'alive',
        N'Minor Fornax noble''s third son; billeted at the eastern Rhine garrison; petitioning for a Catalyst candidacy slot he has not legitimately qualified for.',
        N'Manfred Stein arrived at the garrison with better clothes than the other junior officers and a surname that opened one door out of every three. He has mistaken this for competence. He attaches himself to Knight training sessions uninvited and describes himself in letters home as advancing rapidly. He is not advancing rapidly. He is also not as healthy as the Transmutation programme requires, and this is the only fact about himself he has managed to conceal successfully.',
        N'The young man whose ambition has outrun his body; the system will test whether the gap can be hidden.',
        N'No POV.',
        N'House Fornax; minor noble estate, eastern Rhine tributary; currently billeted at eastern garrison',
        174, 68, N'lean-young, not yet fully filled out',
        N'straw blonde', N'somewhat unkempt, garrison-short but imperfectly maintained', N'short',
        N'blue', N'fair, unweathered', N'unmarked, indoor-soft',
        N'none',
        N'overcompensating energy; moves like someone auditioning; laughs a half-beat before the rest of the room',
        N'young noble''s wool, better than his station warrants; already wearing an officer''s collar insignia he has not been assigned',
        N'none',
        N'Attaches himself to Knight training sessions uninvited; reads Transmutation survival accounts in the barrack library; writes letters home claiming advancement.',
        N'His pre-candidacy physical examination recorded a cardiac arrhythmia that, under standard Furnace protocol, would disqualify him from Catalyst consideration. He paid the regimental physician two pieces of amber jewelry taken from his mother''s dressing table to document the examination as unremarkable. The amended record was filed four months ago. The physician spent one piece of amber on a Rhine river crossing toll and has the other in his coat pocket.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Eastern Rhine garrison, junior officer billets',
        N'0', N'0',
        N'young Germanic nobleman in slightly wrong officer''s uniform, straw blonde hair, eager expression, Rhine garrison stone courtyard, early morning, Buehlman dark fantasy --ar 2:3',
        N'A 19-year-old young man in ill-fitting noble uniform, straw blonde hair, blue eyes, eager expression, medieval stone garrison courtyard',
        0, 0
    );
    PRINT N'Manfred Stein seeded.';
END
ELSE PRINT N'Manfred Stein already exists.';
GO

-- ── 8. Albrecht Vogt ─────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Albrecht Vogt')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Albrecht Vogt', N'albrecht-vogt', N'canon', 1,
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
        @id, N'Albrecht Vogt', N'albrecht-vogt', N'Albrecht', N'Vogt', N'Paladin',
        N'human', N'human', N'male', N'he/him',
        44, N'alive',
        N'Paladin; commands the eastern Rhine Scrying Installation; authorises all Sphere 31 observation schedules; submits weekly intelligence summaries to Lord Brenner.',
        N'Albrecht Vogt commands the most sensitive installation in eastern Fornax territory and does so with the quiet authority of a man who understands that his rank is also a kind of wall. He is thoughtful, organised, and respected by every operator under his direction. He also controls what leaves the installation and what doesn''t. For two years he has been determining what is intelligence and what is merely data, and applying this distinction in a direction that suits him.',
        N'The compromised authority figure whose betrayal is invisible precisely because he is effective.',
        N'No POV.',
        N'House Fornax; eastern Rhine Scrying Installation',
        204, 126, N'post-human Paladin frame; substantial',
        N'silver-white', N'close-cropped', N'short',
        N'pale grey, slightly luminous', N'fair', N'unusually smooth for his age, Transmutation effect',
        N'Evident enhancement (Paladin)',
        N'deliberate economy of movement; a very large man who wastes nothing; stillness that reads as patience and is occasionally something else',
        N'Paladin''s reinforced field coat with Furnace circuit-mark embossing; no decorative elements; always carries a bound summary ledger',
        N'Transmutation: two-stage Paladin infusion, Furnace-grade Xerum 525; significant height, mass, and sensory augmentation; minor light-sensitivity; skin-smoothing effect.',
        N'Reviews all incoming Scrying transcripts before classification; personally oversees Sphere 31 contact sessions; submits weekly summaries to Lord Brenner — summaries he composes himself from a redacted version of the actual record.',
        N'For two years he has been compressing Sphere 31 industrial observation readings — manufacturing tolerances, material yields, production layouts — and selling summarised notation to a House Draught merchant fleet agent who meets him twice yearly at a Rhine tributary waystation. He tells himself the intelligence is commercial rather than military. He keeps a second ledger in which he has carefully noted which readings he has sold and maintains the fiction, in writing, that the distinction matters.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Eastern Rhine Scrying Installation; Rhine-valley command transit',
        N'0', N'0',
        N'imposing silver-haired Germanic Paladin in reinforced coat, post-human physique, Scrying Installation control room, glowing instruments, stone chamber, Buehlman dark fantasy --ar 2:3',
        N'A 44-year-old silver-haired Paladin of massive frame before a glowing medieval scrying installation, pale luminous eyes, composed authority',
        0, 0
    );
    PRINT N'Albrecht Vogt seeded.';
END
ELSE PRINT N'Albrecht Vogt already exists.';
GO

-- ── 9. Klaus Weimer ──────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Klaus Weimer')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Klaus Weimer', N'klaus-weimer', N'canon', 1,
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
        @id, N'Klaus Weimer', N'klaus-weimer', N'Klaus', N'Weimer', N'',
        N'human', N'human', N'male', N'he/him',
        22, N'alive',
        N'Myrmidon; Rhine garrison; Transmutation survivor, six months post-infusion; assigned to heavy labour and perimeter patrol; speaks rarely.',
        N'Klaus Weimer survived the Catalyst and came out the other side with a body that doesn''t quite fit and a memory with a gap in the middle of it that he keeps pressing on the way you press a bruise. He knows the gap is from the fever. He knows the fever was three days. He doesn''t know what he did during those three days, not all of it, and the attending Paladin who sealed the incident file has been careful not to tell him. Klaus is strong and compliant and assigned to work that doesn''t require him to be near other soldiers in confined spaces.',
        N'The survivor who doesn''t know what he survived; the institution has decided for him.',
        N'No POV.',
        N'House Fornax; Rhine garrison, Myrmidon barracks',
        188, 104, N'dense muscle, early Transmutation frame',
        N'black', N'military-short', N'short',
        N'dark brown, unfocused mid-distance', N'medium olive', N'heat-flushed, minor scarring from infusion',
        N'Early Transmutation evident; mass increase and acuity shift, not yet full Knight physique',
        N'over-controlled; moves like a man trying not to break things; avoids confined spaces without being able to explain why',
        N'Myrmidon''s heavy wool uniform, one size too small across the shoulders post-Transmutation; no ornamentation',
        N'Catalyst first infusion, Rhine garrison batch, survived; height increase of 6 cm; mass increase; Myrmidon rank, Knight-track candidacy pending second infusion review.',
        N'Weapons drills; heavy hauling and perimeter patrol assignments; avoids the garrison common room after dark; does not discuss the Catalyst with other Myrmidons.',
        N'During his Catalyst fever he killed two other candidates in the infirmary anteroom — both were restrained and neither was conscious. The attending Paladin sealed the incident file as combat-incapacitation and recorded both deaths as Catalyst failure. Klaus has a fragmentary image from the gap: his own hands, and a sound he cannot name. He has not asked what the image is. The Paladin has not offered.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Rhine garrison, Myrmidon barracks and patrol perimeter',
        N'0', N'0',
        N'young heavily built Germanic Myrmidon in too-tight wool uniform, black hair cropped close, dark unfocused eyes, Rhine garrison yard, overcast, Buehlman dark fantasy --ar 2:3',
        N'A 22-year-old densely muscled soldier in ill-fitting wool, black hair, dark unfocused eyes, standing in a medieval garrison yard',
        0, 0
    );
    PRINT N'Klaus Weimer seeded.';
END
ELSE PRINT N'Klaus Weimer already exists.';
GO

-- ── 10. Reinhard Dorn ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Reinhard Dorn')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Reinhard Dorn', N'reinhard-dorn', N'canon', 1,
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
        @id, N'Reinhard Dorn', N'reinhard-dorn', N'Reinhard', N'Dorn', N'Sergeant',
        N'human', N'human', N'male', N'he/him',
        37, N'alive',
        N'Infantry sergeant; western Rhine border post; no Transmutation; fifteen years'' service; rated excellent by every commander he has served under.',
        N'Reinhard Dorn is the kind of soldier who makes institutions work — steady, precise, and loyal to the unit in the way that is worth more than loyalty to the House. He is also the kind of man who has spent fifteen years making certain his service record is so clean that no one has cause to look at anything else. He doesn''t apply for Transmutation candidacy. He doesn''t give a reason. His commanders find him reliable and don''t press.',
        N'The exemplary soldier carrying a hereditary secret that would end him if the institution looked at it directly.',
        N'No POV.',
        N'House Fornax; western Rhine border post, infantry regiment',
        180, 85, N'field-fit, regulation trim',
        N'dark blonde', N'close-cropped', N'short',
        N'brown', N'tanned', N'weathered, field-scarred at the left forearm',
        N'none',
        N'correct military bearing that has become second nature; economical movement; never hurries; never hesitates',
        N'infantry sergeant''s uniform maintained beyond regulation standard; no ornamentation; boots always clean regardless of conditions',
        N'none',
        N'Drills his squad at dawn; maintains weapons and kit; processes border transit paperwork; avoids regimental record review sessions without appearing to.',
        N'His mother''s birth record — filed in the Rhine district registry office in Brenne — shows she was the daughter of a registered Oathless who was executed in the year of her birth. The record was suppressed by the midwife who delivered her, who owed Reinhard''s grandfather a debt in grain. If the record surfaces, Reinhard''s Oathless heritage is documentable and his execution is protocol, not discretion.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Western Rhine border post, infantry regiment',
        N'0', N'0',
        N'weathered Germanic infantry sergeant in dark military wool, dark blonde close-cropped hair, Rhine border stone post, overcast, Buehlman dark fantasy --ar 2:3',
        N'A 37-year-old infantry sergeant in well-maintained military uniform, dark blonde hair, weathered face, standing at a medieval border post',
        0, 0
    );
    PRINT N'Reinhard Dorn seeded.';
END
ELSE PRINT N'Reinhard Dorn already exists.';
GO

-- ── 11. Friedrich Kast ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Friedrich Kast')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Friedrich Kast', N'friedrich-kast', N'canon', 1,
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
        @id, N'Friedrich Kast', N'friedrich-kast', N'Friedrich', N'Kast', N'Supervisor',
        N'human', N'human', N'male', N'he/him',
        55, N'alive',
        N'Senior Scrying supervisor; central Rhine Installation; reviews all incoming transcripts before classification; has held this post for nineteen years.',
        N'Friedrich Kast has spent nineteen years being the person who decides what becomes official. He has done this job correctly and with institutional good faith for seventeen of those years. Two years ago he found something in an unredacted Sphere 31 session transcript that should not have been there, and the something named someone he cannot afford to name. He is a methodical man. He is currently being methodical about a decision that gets harder the longer he makes it.',
        N'The institutional gatekeeper who has found the evidence of treason and is calculating the price of each possible action.',
        N'No POV.',
        N'House Fornax; central Rhine Scrying Installation, senior supervisor wing',
        174, 79, N'slight, scholar''s frame rounded at the shoulders from decades at a desk',
        N'white', N'receding, neat', N'short',
        N'pale blue', N'indoor-pale', N'veined hands, fine lines around the eyes',
        N'none',
        N'slightly hunched, precision in movement; every gesture calculated and deliberate',
        N'senior staff''s dark wool robe with Installation insignia, ink-stained at both cuffs, reading lens on a cord around his neck',
        N'none',
        N'Reviews all incoming Scrying transcripts before classification; assigns readings to junior operators; composes weekly summaries for House command that have been inaccurate for two years.',
        N'Two years ago a Sphere 31 session transcript captured Lord-tier correspondence glyphs — unique to Fornax House council members — appearing in a Liturgy acquisition coordination sequence. He cross-referenced the glyph signature against the Furnace''s senior register and identified it as belonging to his brother-in-law, Elder Councilor Bertram Seel. He has not submitted the transcript. He has not destroyed it either. He is keeping it in a sealed oilcloth packet behind the false back of his filing cabinet, and he is trying to determine his price.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Central Rhine Scrying Installation, senior supervisor wing',
        N'0', N'0',
        N'white-haired elderly Germanic official in dark wool robe, bent over classified Scrying records, candlelit stone office, reading lens, Buehlman dark fantasy --ar 2:3',
        N'A 55-year-old white-haired supervisor in dark wool robe bent over stacked classified documents, pale blue eyes, reading lens on cord',
        0, 0
    );
    PRINT N'Friedrich Kast seeded.';
END
ELSE PRINT N'Friedrich Kast already exists.';
GO

-- ── 12. Brunhild Sauer ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Brunhild Sauer')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Brunhild Sauer', N'brunhild-sauer', N'canon', 1,
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
        @id, N'Brunhild Sauer', N'brunhild-sauer', N'Brunhild', N'Sauer', N'Knight',
        N'human', N'human', N'female', N'she/her',
        38, N'alive',
        N'Knight; Rhine garrison champion; runs weekly Transmutation-track assessments; known for honest evaluations that ruin expectations.',
        N'Brunhild Sauer is the kind of soldier whose presence ends arguments before they start, and she has never been sure whether this is a quality she''d recommend. She is large, precise, and direct in a way the garrison has stopped trying to moderate. She runs the Transmutation-track assessment sessions because she is the most qualified person available, and she runs them without sentimentality, which is why her pass rate is lower than any previous Knight''s and her rejection letters are shorter. She has one secret. She keeps it the same way she keeps everything: without visible effort.',
        N'The meritocratic authority whose own survival rests on a procedural fraud she has buried in plain sight.',
        N'No POV.',
        N'House Fornax; Rhine garrison, Knight command wing',
        195, 107, N'powerful, Knight-frame',
        N'copper-red', N'tight braid', N'long',
        N'green', N'freckled fair', N'weathered, drill-scarred at the knuckles',
        N'Subtle height gain (Knight)',
        N'coiled readiness; rarely fully still; occupies space without trying to; corrects her posture when she notices she''s doing it',
        N'Knight''s plate with Rhine garrison markings, copper-chased pauldron indicating garrison champion rank; functional, no ceremony',
        N'Transmutation: Catalyst infusion, Rhine garrison batch, survived; height increase of 12 cm post-infusion; standard Knight augmentation profile.',
        N'Morning weapons drills; challenges Myrmidons she thinks are getting soft; inspects Rhine crossing defenses; runs three weekly Transmutation-track assessment sessions.',
        N'She failed her first Catalyst attempt and survived by chance — the batch she received was defective and produced no infusion effect, which the attendant misread as a successful rejection and recorded as a pass. She has a survivor''s documented batch number that does not correspond to any issued extraction lot in the Furnace records. Her Paladin commanding officer believes she completed a standard infusion. The discrepancy exists in three separate records and she has not corrected any of them.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Rhine garrison, Knight command wing and drill ground',
        N'0', N'0',
        N'tall copper-haired Germanic knight-woman in heavy plate armor, copper-chased pauldron, Rhine garrison drill yard, morning, Buehlman dark fantasy --ar 2:3',
        N'A 38-year-old tall red-haired woman in heavy plate armor, green eyes, copper pauldron, at a medieval garrison drill yard',
        0, 0
    );
    PRINT N'Brunhild Sauer seeded.';
END
ELSE PRINT N'Brunhild Sauer already exists.';
GO

-- ── 13. Konrad Streb ─────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Konrad Streb')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Konrad Streb', N'konrad-streb', N'canon', 1,
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
        @id, N'Konrad Streb', N'konrad-streb', N'Konrad', N'Streb', N'',
        N'human', N'human', N'male', N'he/him',
        29, N'alive',
        N'Line infantry; Rhine middle garrison; one commendation; petitions for a Catalyst slot every quarter; maintains a private record of how each candidate in his intake has died.',
        N'Konrad Streb wants the infusion the way some men want rank and others want land — not for what it means but for what it stops him being. He is a good soldier and a patient one, and he has been patient for seven years, which is the kind of patience that starts to look like something else. He files his quarterly petition with supporting documentation and has each time been ranked below the threshold. He has decided to stop letting the threshold be the deciding factor.',
        N'The ambitious soldier who has moved from petitioning to forging; the system''s gatekeeping and his response to it.',
        N'No POV.',
        N'House Fornax; Rhine middle garrison, line infantry barracks',
        182, 88, N'athletic, field-fit',
        N'brown', N'medium length, pulled back for drill', N'medium',
        N'hazel', N'medium', N'sun-weathered, one horizontal scar at the right cheekbone',
        N'none',
        N'alert, impatient; always measuring gaps in formations; reads rooms for hierarchies before anything else',
        N'line infantry uniform with one commendation bar; keeps it pressed; studies officers'' bearing and copies what he finds useful',
        N'none',
        N'Infantry rotation duties; quarterly petition filings; maintains a private journal accounting every Catalyst candidate he has known and how they died; reads the casualty notices carefully.',
        N'He has submitted four petition letters bearing Knight Brunhild Sauer''s co-endorsement signature. She has endorsed none of them. He has studied her signature from garrison orders and unit commendation certificates and can reproduce it at speed. The latest forged petition was filed three weeks ago. Brunhild Sauer has not been asked to verify it.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Rhine middle garrison, line infantry barracks',
        N'0', N'0',
        N'athletic Germanic soldier in infantry uniform with commendation bar, brown hair tied back, hazel eyes, Rhine garrison interior, determined expression, Buehlman dark fantasy --ar 2:3',
        N'A 29-year-old soldier in military uniform, commendation bar, brown hair pulled back, hazel eyes, determined expression, medieval garrison',
        0, 0
    );
    PRINT N'Konrad Streb seeded.';
END
ELSE PRINT N'Konrad Streb already exists.';
GO

-- ── 14. Walburga Menk ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Walburga Menk')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Walburga Menk', N'walburga-menk', N'canon', 1,
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
        @id, N'Walburga Menk', N'walburga-menk', N'Walburga', N'Menk', N'Master Scholar',
        N'human', N'human', N'female', N'she/her',
        60, N'alive',
        N'Senior weapons scholar; Great Furnace archive wing; institutional memory of forty years; advises Lord Brenner on historical precedent; has not left the Furnace district in eleven years.',
        N'Walburga Menk knows where everything is. This includes the things that were misfiled, the things that were meant to be destroyed, and the things that were hidden in the confidence that no one who remembered them would still be working. She has not left the Furnace district in eleven years because she doesn''t need to — everything she requires comes to her, including the questions she wasn''t asked and the answers she chose not to give. She is patient, methodical, and fully aware that the archive is the institution''s memory and she is the archive.',
        N'The scholar who has held a founding secret for twenty years and chosen indispensability over disclosure.',
        N'No POV.',
        N'House Fornax; Great Furnace administrative complex, archive wing',
        160, 67, N'compact, unhurried',
        N'white-silver', N'neat bun, two pins', N'medium',
        N'dark brown', N'medium olive', N'deeply lined, scholar''s indoor complexion',
        N'none',
        N'unhurried authority; rooms quiet when she enters; has a habit of pausing before answering that some read as consideration and others as warning',
        N'senior scholar''s robe, Furnace gold trim on collar, always carries a bound notebook and a set of three archival keys on a ring at her waist',
        N'none',
        N'Maintains the weapons specification archive; advises Lord Brenner on historical precedent; trains junior scholars; receives all significant misfiled material before it reaches the general archive.',
        N'Twenty years ago she found a misfiled document in the Furnace archive basement: the original Xerum 525 extraction formula, predating the current standard, with a survivability rate of 41% rather than 20%. The document bore the seal of Lord Brenner''s predecessor and a destruction order that was never carried out. She placed it in a sealed oilcloth in her private archive box. She has never told anyone. She calculated, at the time, that knowledge of the original formula would make her permanently necessary. She has not recalculated since.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Great Furnace administrative complex, archive wing',
        N'0', N'0',
        N'elderly white-haired Germanic scholar-woman in gold-trimmed robes, stone archive lined with ledgers and specification drawers, candlelight, archival keys at her waist, Buehlman dark fantasy --ar 2:3',
        N'A 60-year-old woman in gold-trimmed scholar''s robes in a stone archive, white hair in a neat bun, dark brown eyes, composed authority',
        0, 0
    );
    PRINT N'Walburga Menk seeded.';
END
ELSE PRINT N'Walburga Menk already exists.';
GO

-- ── 15. Oskar Veld ───────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Oskar Veld')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Oskar Veld', N'oskar-veld', N'canon', 1,
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
        @id, N'Oskar Veld', N'oskar-veld', N'Oskar', N'Veld', N'',
        N'human', N'human', N'male', N'he/him',
        45, N'alive',
        N'Fornax counterespionage officer; monitors Rhine merchant correspondence for encoding patterns; runs two paid informants inside the merchant guild; looks like a trade clerk.',
        N'Oskar Veld has spent twenty years being unremarkable by design, and he has gotten very good at it. He works in a building that looks like a transit records office and holds a title that reads as administrative, and he has two informants, one active operation, and a confirmed foreign asset he has not arrested because arresting him now would be wasteful. He thinks in systems. He tracks seventeen separate correspondence routes and maintains a separate mental ledger for each. He has told his superiors about five of them.',
        N'The counterintelligence operative running an unsanctioned double-feed operation for personal credit.',
        N'No POV.',
        N'House Fornax; Rhine trade monitoring post, intelligence division',
        176, 80, N'unremarkable, deliberately so',
        N'mousy brown-grey', N'neatly parted', N'short',
        N'grey', N'fair', N'indoor-pale, nothing memorable',
        N'none',
        N'invisible by design; moves to be furniture until he decides not to be; never makes the first sound in a room',
        N'middle-rank administrative wool, no insignia; looks precisely like a trade records clerk',
        N'none',
        N'Monitors cross-House merchant correspondence for encoding patterns; runs two paid guild informants; writes intelligence reports in three layers of meaning depending on which page is absent.',
        N'Fourteen months ago he confirmed Werner Krauss as a House Calyx intelligence asset. He has not arrested him, reported him to superiors, or indicated in any filed document that Krauss has been identified. Instead he has been feeding Krauss selectively falsified Fornax weapons specifications through the laundress courier — details authentic enough to validate but with tolerances altered in ways that would matter in combat. He is waiting for the false specifications to reach Calyx command. When they do, the credit for the operation will be entirely his.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Rhine trade monitoring post; intelligence division; merchant guild surveillance routes',
        N'0', N'0',
        N'unremarkable mousy-haired Germanic man in plain administrative wool, grey eyes, stone office, merchant correspondence spread across the desk, candlelight, Buehlman dark fantasy --ar 2:3',
        N'A 45-year-old nondescript man in plain wool at a desk covered in merchant correspondence, grey eyes, carefully blank expression',
        0, 0
    );
    PRINT N'Oskar Veld seeded.';
END
ELSE PRINT N'Oskar Veld already exists.';
GO
