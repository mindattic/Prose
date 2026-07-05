SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- =============================================================================
-- HOUSE CALYX -- FULL HIERARCHY SEED
-- Eastern plains, Danube basin -- oldest agricultural Great House of the Cauld
-- Universe: Fantasy / Steampunk (0197E9C9-0002-7000-8000-000000000002)
-- 2026-07-04
-- 53 characters: Ruling Family, Cabinet, Military, Scrying, Domestic, Oathless
-- =============================================================================

-- ---------------------------------------------------------------------------
-- RULING FAMILY (9 characters)
-- ---------------------------------------------------------------------------

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Radovan Calyx-Fekete')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Radovan Calyx-Fekete', N'radovan-calyx-fekete', N'canon', 1,
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
        @id, N'Radovan Calyx-Fekete', N'radovan-calyx-fekete', N'Radovan', N'Calyx-Fekete', N'Lord',
        N'human', N'human', N'male', N'he/him', 62, N'alive',
        N'Lord of House Calyx; presides over the eastern plains from Korvarat Hall; thirty-one years of war decisions behind him and a growing awareness that the House''s agricultural power is not what it was',
        N'Radovan Calyx-Fekete has ruled for thirty-one years by the method his mother taught him: control the grain, control the allegiances. He is not a general. He survived the battlefield through caution and the good sense to put competent people between himself and the dying. He is a farmer''s son elevated to a lord''s chair who has never forgotten what that means when the harvest fails. The eastern plains give House Calyx its power and its vulnerability in the same season. His great fear is that the other Houses have finally noticed this. He is aging into a settled calm his cabinet mistakes for wisdom and his children mistake for indifference.',
        N'The gravity well all House Calyx decisions orbit. His authorization of the catastrophic infusion campaign is the wound at the center of the House that no one will speak aloud.',
        N'No POV.',
        N'House Calyx; eastern plains, Danube basin; ancestral seat at Korvarat Hall',
        178, 96, N'heavyset; the solidness of a man who spent his twenties on campaign and his sixties behind a table',
        N'grey, formerly dark brown', N'close-cropped', N'short',
        N'dark brown', N'ruddy', N'lined; the face of a man who has done a great deal of worrying without letting people see it',
        N'none',
        N'deliberate and grounded; moves as though the floor belongs to him; formal occasions stiffen him but he never looks uncertain in his own hall',
        N'practical Calyx ceremonial dress for state occasions; working clothes that are well-made but unornate in private; always the House sigil somewhere on his person',
        N'none; he refused Transmutation on grounds that a lord who cannot be killed by ordinary means ceases to understand what he is asking others to die for',
        N'Morning estate accounts with the Seneschal. Midday agricultural reports from territory stewards. Afternoon whatever the cabinet has accumulated. Evening private, usually with Lady Zsofia. He sleeps poorly and has for ten years.',
        N'He authorized the infusion campaign twenty-five years ago that killed forty percent of his Corps and then blamed the Transmutation Practitioner publicly. The true order came from him -- proceeding at half-readiness to meet a campaign deadline set by his own political ambition. He has carried this for a quarter century and it has become indistinguishable from his character.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Korvarat Hall and surrounding eastern Calyx plains; formal diplomatic travel to the Compact grounds twice yearly',
        N'0', N'0',
        N'heavyset lord in his sixties, grey close-cropped hair, dark brown eyes, ruddy lined face, practical Calyx noble dress with House sigil, stone medieval hall, deliberate grounded posture, dark fantasy steampunk',
        N'heavyset lord in his sixties, grey hair, dark eyes, ruddy face, Calyx noble dress, stone hall, grounded posture',
        0, 0
    );
    PRINT 'Radovan Calyx-Fekete seeded.';
END
ELSE PRINT 'Radovan Calyx-Fekete already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Zsofia Calyx-Fekete')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Zsofia Calyx-Fekete', N'zsofia-calyx-fekete', N'canon', 1,
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
        @id, N'Zsofia Calyx-Fekete', N'zsofia-calyx-fekete', N'Zsofia', N'Calyx-Fekete', N'Lady',
        N'human', N'human', N'female', N'she/her', 58, N'alive',
        N'Lady of House Calyx; the diplomatic architect of the House''s alliances for thirty years; the reason Calyx has not been economically strangled by its neighbors',
        N'Zsofia was born to a minor plains family and married into the House at nineteen -- a match meant to be practical that became something else. She is the architect of every alliance that kept Calyx from absorption into the Fornax military sphere during the decade when Radovan was at his most impulsive. She does this through correspondence, through the careful placement of people, through the reading of gifts and seating arrangements at occasions men think are purely ceremonial. She does not fight. She has not needed to. The House stands partly because of the fields and partly because she has been very good at this for a very long time.',
        N'The diplomatic intelligence of the House made into a person. Her fifteen-year correspondence with House Atrament''s Spymaster is the buried mechanism that holds Calyx together -- and the secret that could detonate it.',
        N'No POV.',
        N'House Calyx; born Vranica, a minor Calyx plains noble line; married into the ruling family at nineteen',
        165, 68, N'slender but carrying the solid health of a woman who has walked the estate grounds every morning for thirty years',
        N'dark with silver running through it', N'coiled and pinned at the nape', N'long when down',
        N'hazel', N'olive', N'clear; the composed surface of a woman who has learned to present nothing that can be used against her',
        N'none',
        N'precise and unhurried; enters rooms as though she has already decided how she will leave them',
        N'Calyx ceremonial colors -- deep burgundy and harvest gold -- in good fabric, never ostentatious; jewelry is House heirlooms worn as signal, not decoration',
        N'none',
        N'Morning correspondence. Midday overseeing household preparation. Afternoon receiving visitors -- envoys, agricultural factors, the Chaplain. Evening with Radovan or, if he is occupied, her own books and a private journal written in cipher.',
        N'For fifteen years she has been conducting a correspondence with House Atrament''s Spymaster through a coded channel embedded in agricultural export paperwork. She believes this is the only reason Calyx has not been economically strangled. She has never told Radovan. She has told herself she is protecting him. She is not certain this is entirely true.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Korvarat Hall and the eastern plains; diplomatic travel as needed; her influence through correspondence extends further than her physical presence',
        N'0', N'0',
        N'woman in her late fifties, dark hair silvering at temples pinned elegantly, hazel eyes, olive clear skin, Calyx ceremonial burgundy and gold, composed precise posture, stone manor interior, dark fantasy',
        N'woman in her late fifties, silvering dark hair pinned, olive skin, hazel eyes, burgundy gold ceremonial dress, composed posture, stone manor',
        0, 0
    );
    PRINT 'Zsofia Calyx-Fekete seeded.';
END
ELSE PRINT 'Zsofia Calyx-Fekete already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Mirela Calyx-Fekete')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Mirela Calyx-Fekete', N'mirela-calyx-fekete', N'canon', 1,
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
        @id, N'Mirela Calyx-Fekete', N'mirela-calyx-fekete', N'Mirela', N'Calyx-Fekete', N'Lady',
        N'human', N'human', N'female', N'she/her', 34, N'alive',
        N'Heir to House Calyx; eldest child of Radovan and Zsofia; being groomed for rule and increasingly resistant to the terms being attached to it',
        N'Mirela is sharper than the cabinet knows and more impatient than her mother has managed to fully conceal. She has sat in on enough administrative sessions to understand the House''s agricultural accounts better than the Treasurer presents them. She has spent fourteen years watching her father''s careful caution and concluded that the time for careful caution is ending. What she resents is not the inheritance -- she wants to rule -- but the expectation that ruling means managing the House through the right marriage rather than the right decisions. She has been separating these two ideas in a way that is going to cause a significant problem.',
        N'The heir who will inherit a House in worse condition than anyone is admitting, at a moment when the Cauld is shifting around Calyx faster than her father can track.',
        N'No POV.',
        N'House Calyx; eldest child of the ruling line; eastern plains',
        170, 65, N'athletic; she rides every morning and has since she was eight',
        N'dark brown', N'loose, often tucked behind one ear when working', N'medium',
        N'green', N'olive', N'clear; she has her mother''s composure at distance and loses it faster up close',
        N'none',
        N'quick and purposeful; moves through the house as though she is already running it; pauses to listen at doors she is not supposed to linger near',
        N'practical noble dress that edges toward the military without crossing into it; riding clothes whenever she can justify it',
        N'none; has not yet been presented for Transmutation consideration',
        N'Mornings riding the estate perimeter, gathering territory intelligence she is not authorized to collect. Administrative sessions with whoever will include her. Reading the correspondence her father assigns her to summarize. Informal conversations with senior military staff that her parents have not fully noticed she is conducting.',
        N'She has deliberately failed three arranged meetings with political marriage candidates by feeding their representatives false yield projections for Calyx''s agricultural output. Two prospects withdrew on their own. She has a fourth set of false figures prepared. She does not know what she will do when her parents notice the pattern.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Korvarat Hall and eastern Calyx plains; permitted diplomatic travel with escort',
        N'0', N'0',
        N'woman in her early thirties, dark brown hair loose, green eyes, olive skin, practical noble dress edging military, athletic build, purposeful posture, stone manor hall, dark fantasy',
        N'woman in her thirties, dark hair loose, green eyes, olive skin, practical noble dress, athletic build, purposeful posture, stone hall',
        0, 0
    );
    PRINT 'Mirela Calyx-Fekete seeded.';
END
ELSE PRINT 'Mirela Calyx-Fekete already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Donat Calyx-Fekete')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Donat Calyx-Fekete', N'donat-calyx-fekete', N'canon', 1,
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
        @id, N'Donat Calyx-Fekete', N'donat-calyx-fekete', N'Donat', N'Calyx-Fekete', N'Ser',
        N'human', N'human', N'male', N'he/him', 31, N'alive',
        N'Second born son of House Calyx; Knight by first infusion survived at twenty-six; more capable than the succession has any use for',
        N'Donat took the infusion during a border campaign where the alternative was being useful or being protected, and he chose useful. He survived the process that had killed or maimed most of his infusion cohort across the House''s historical record. This made him something the family is proud of and does not know how to manage. He is better at field command than anyone currently holding the role and is constitutionally incapable of performing the decorative younger son. He conducts informal conversations with the senior Corps staff that no one has explicitly authorized. The House has not decided what to do with him and he has begun making his own decisions.',
        N'The second son more capable than his role -- the investigation he is quietly running about the arc of Transmutation is going to put him in contact with information the House would rather he not have.',
        N'No POV.',
        N'House Calyx; second child of the ruling line; Knight by first infusion at twenty-six',
        183, 85, N'lean-muscled; the infusion added height and density; he moves slightly differently than before and people who knew him notice',
        N'dark brown', N'short, practical', N'short',
        N'dark brown', N'warm medium', N'clear; regularized somewhat by the infusion',
        N'Subtle height gain, increased density; the scale reads slightly wrong at close distance for those who do not know what they are looking at',
        N'easy and economical; integrated the infusion''s changes fully; slightly more still than an unaltered person at rest',
        N'military practical dress; the family sigil without ornament; avoids the ceremonial versions of Calyx dress',
        N'First infusion survived at twenty-six; conservative Calyx protocol; enhanced musculature and bone density',
        N'Attached to the Corps in a secondary command role. Morning weapons drill and field assessment. Afternoon administrative duties that underuse him. Extended time with the Transmutation Practitioner that he has not reported to his father.',
        N'He has been meeting secretly with an Oathless former Champion expelled from House Draught, trying to understand the full arc of Transmutation before he commits to further infusions. He wants to know what he is agreeing to. He has not told his father, the Practitioner, or the Commander.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Korvarat Hall and Corps territory; border operations when assigned',
        N'0', N'0',
        N'young man in his early thirties, dark brown hair short, dark eyes, warm medium skin, lean military build with slight Knight infusion height gain, practical dress with Calyx sigil, economical movement, stone manor, dark fantasy',
        N'young man early thirties, dark hair, dark eyes, lean military build, practical noble dress, slight additional height, economical posture, stone manor',
        0, 0
    );
    PRINT 'Donat Calyx-Fekete seeded.';
END
ELSE PRINT 'Donat Calyx-Fekete already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Petronela Calyx-Fekete')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Petronela Calyx-Fekete', N'petronela-calyx-fekete', N'canon', 1,
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
        @id, N'Petronela Calyx-Fekete', N'petronela-calyx-fekete', N'Petronela', N'Calyx-Fekete', N'',
        N'human', N'human', N'female', N'she/her', 18, N'alive',
        N'Youngest child of House Calyx; being shaped for a political marriage alliance with House Atrament; presenting as more naive than she is',
        N'Petronela has spent eighteen years watching two older siblings navigate a house full of adults who believe children can be managed, and she has learned precisely how adults behave when they think they are not being observed. She is presented to visitors as the pleasant youngest daughter -- musical, soft-spoken, amenable. She is these things. She is also paying careful attention to everything the arrangement around her is designed to produce. She has spent the last six months in extended conversation with the Bheur Chaplain. Her family believes she is devout. She is devout. She is also preparing a response that does not involve Atrament or a marriage contract.',
        N'The youngest sibling who appears to be a political instrument and is actually an agent. Her vow petition is a time bomb running underneath the marriage negotiations.',
        N'No POV.',
        N'House Calyx; youngest child of the ruling line; eastern plains',
        163, 55, N'slight; she has not yet finished growing',
        N'auburn', N'braided with Calyx ceremonial cord woven in', N'long',
        N'grey', N'pale olive', N'clear; the careful composure of someone practicing it',
        N'none',
        N'still and deliberate in the presence of adults; quicker and less careful when she believes she is unobserved',
        N'Calyx formal dress appropriate to her age; the Bheur devotional cord worn visibly at her wrist, which is both genuine and a message',
        N'none',
        N'Morning lessons with a tutor she has replaced twice at her quiet suggestion. Midday formal occasions when visitors are present. Afternoon with the Chaplain, which her parents have encouraged as evidence of piety. Evening music, which is genuine and which gives her privacy that is not questioned.',
        N'She has already drafted and memorized a formal vow petition to the Bheur priesthood. The moment a marriage is announced, she intends to file it -- a religious vow that in Calyx tradition constitutes grounds for refusing a secular contract. She has been measuring the Chaplain for six months to determine whether he will support the petition or report it to her parents.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Korvarat Hall and immediate grounds; most restricted movement of the three children',
        N'0', N'0',
        N'young woman of eighteen, auburn braided hair with ceremonial cord, grey eyes, pale olive skin, slight build, Calyx formal dress, Bheur devotional cord at wrist, still careful posture, stone manor hall, dark fantasy',
        N'young woman eighteen, auburn braided hair, grey eyes, pale skin, formal dress, devotional cord at wrist, composed posture, stone hall',
        0, 0
    );
    PRINT 'Petronela Calyx-Fekete seeded.';
END
ELSE PRINT 'Petronela Calyx-Fekete already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Borbala Calyx-Fekete')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Borbala Calyx-Fekete', N'borbala-calyx-fekete', N'canon', 1,
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
        @id, N'Borbala Calyx-Fekete', N'borbala-calyx-fekete', N'Borbala', N'Calyx-Fekete', N'Lady',
        N'human', N'human', N'female', N'she/her', 81, N'alive',
        N'Dowager Lady of House Calyx; Radovan''s mother; the actual mind behind the House during her husband''s campaigns; still sharp, still dangerous, and not telling anyone what she knows',
        N'Borbala has outlived two wars, a husband, and three political advisors who underestimated her. She is eighty-one and uses a cane she treats as an accessory rather than an aid. She ran House Calyx for eleven years while her husband conducted campaigns, and she ran it better than he did. She has watched Radovan rule for thirty-one years and reached a verdict she keeps to herself. She says almost nothing at formal occasions. The things she does say are quoted in corridors afterward, as people try to determine what she meant.',
        N'The repository of everything the House has survived. Her secret about Radovan''s birth is the inheritance crisis that has never detonated because she has never permitted it to.',
        N'No POV.',
        N'House Calyx; born Borbala Hegedus; married into the ruling line sixty-three years ago',
        160, 56, N'wiry; the weight has come down from what it was but nothing else has',
        N'white', N'pinned elaborately; the style has not changed in forty years', N'long when down',
        N'pale blue', N'papery pale', N'deeply lined; the specific quality of a face that has expressed everything and arrived at stillness',
        N'none',
        N'deliberate; she uses the cane with authority; she stops exactly where she intends to stop and not elsewhere',
        N'Calyx formal dress in the older style; the Dowager''s colors, which are specifically hers and not the current Lady''s; she does not update and has not been asked to',
        N'none',
        N'No formal schedule. Present at meals when she chooses to be. Receives three or four visitors daily in her private rooms -- staff and family come to her. Reads in the mornings. She knows everything that happens in the house within twenty-four hours of its happening and has never explained how.',
        N'She knows that Radovan is not her biological son. Her actual child died at birth. The midwife -- dead for thirty years -- quietly substituted an infant born the same night to a household servant woman who also died. Radovan''s inheritance through the paternal line is legitimate; the maternal contribution is not Calyx. No one else has ever known. She has decided that the truth would destroy what she spent her life building for a fact that changes nothing about who Radovan has been.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Korvarat Hall; her movement has narrowed with age; she receives rather than travels',
        N'0', N'0',
        N'old woman in her eighties, white hair elaborately pinned, pale blue eyes, papery pale deeply lined face, Calyx Dowager formal dress, deliberate movement with cane treated as accessory, stone manor interior, dark fantasy authority',
        N'old woman eighties, white pinned hair, pale blue eyes, deeply lined face, Dowager formal dress, cane, stone manor interior',
        0, 0
    );
    PRINT 'Borbala Calyx-Fekete seeded.';
END
ELSE PRINT 'Borbala Calyx-Fekete already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Stanimir Varga')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Stanimir Varga', N'stanimir-varga', N'canon', 1,
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
        @id, N'Stanimir Varga', N'stanimir-varga', N'Stanimir', N'Varga', N'',
        N'human', N'human', N'male', N'he/him', 45, N'alive',
        N'Cousin of the ruling family; garrison commander; passed over for Corps command three times; the most reliable military officer in the House and the one most likely to say things the Lord does not want to hear',
        N'Stanimir is the cousin who did the work. While the heir was managed and the second son went to campaigns, Stanimir spent twenty years building the eastern garrison defenses with insufficient resources and inadequate staff rotations. He has been passed over for Corps Commander three times. The official reason is seniority. The actual reason is that he speaks plainly to the Lord and Radovan has never liked it. He is aware of both. He has continued doing the work because someone has to, and because the garrison failing would hurt people he knows by name.',
        N'The institutional knowledge in the cousin''s role -- the person who keeps things working while the formal hierarchy manages its politics. His unauthorized investigation is going to intersect with the House''s worst secret.',
        N'No POV.',
        N'House Calyx, cousin branch of the Calyx-Fekete line; garrison command, eastern plains',
        181, 88, N'solid; field-built and maintained through twenty years of active garrison duty',
        N'dark with early grey', N'short, military practical', N'short',
        N'brown', N'weathered olive', N'weathered; the face of someone who has spent more time outside than in',
        N'none',
        N'economical and tactical; orients to exits and entrances when he enters any room; a garrison habit he cannot turn off',
        N'military working dress; the family sigil without ornament; owns no formal wear that fits correctly and has not bought any',
        N'none; was offered infusion consideration twice and deferred both times citing garrison requirements',
        N'Garrison inspection every morning. Resource allocation and supply review. Evening coordination with the Corps command staff. He reads military history for two hours before sleep. On the three occasions he was passed over for promotion, he did not change this routine.',
        N'He has been conducting an unauthorized investigation into the true order of command for the failed infusion campaign twenty-five years ago. He has traced the administrative paper trail far enough to know that Nandor Takacs received blame for an order that came from elsewhere -- and that whoever gave the actual order is still alive and inside the House. He does not yet know if it was Lord Radovan.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Calyx garrison and eastern plains defense perimeter; Korvarat Hall on rotation',
        N'0', N'0',
        N'man in his mid-forties, dark hair going grey, brown eyes, weathered olive skin, solid military build, working dress with Calyx sigil, tactical economical posture, stone garrison interior, dark fantasy',
        N'man forties, dark greying hair, brown eyes, weathered face, military working dress, solid build, stone garrison, dark fantasy',
        0, 0
    );
    PRINT 'Stanimir Varga seeded.';
END
ELSE PRINT 'Stanimir Varga already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Dragomira Enyedi')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Dragomira Enyedi', N'dragomira-enyedi', N'canon', 1,
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
        @id, N'Dragomira Enyedi', N'dragomira-enyedi', N'Dragomira', N'Enyedi', N'',
        N'human', N'human', N'female', N'she/her', 39, N'alive',
        N'Calyx cousin who married into House Atrament and returned after her husband''s death; a woman with divided loyalties that everyone can see and no one will name directly',
        N'Dragomira married the Atrament alliance at twenty-two. Her husband died seven years ago in a border engagement she still does not discuss. She came back to Calyx because there was nowhere else she had a real claim to, and the House accepted her back because returning a Calyx woman from an allied House signals something specific in Compact protocol. She has been here two years and everyone is polite and watchful. She is polite in return. She knows the Atrament internal geography -- its people, its factions, how the Spymaster''s office actually works. This is the thing both Houses are most interested in and least willing to simply ask her for.',
        N'The person with firsthand knowledge of Calyx''s most important rival, who has every reason not to share it freely. Her degrading intelligence reports are a passive rebellion with a countdown.',
        N'No POV.',
        N'Born Calyx cousin line; married into House Atrament for twelve years; returned to Calyx two years ago after her husband''s death',
        168, 63, N'lean; Atrament court bearing she has not fully shed and probably cannot',
        N'chestnut', N'braided and coiled in the Atrament fashion', N'long',
        N'hazel', N'olive', N'carefully composed; the composure of someone monitoring their own expression',
        N'none',
        N'poised; Atrament court training visible in her posture; she stands slightly differently than the Calyx-native family members',
        N'a hybrid of Calyx and Atrament styles that satisfies neither House''s conventions fully; she is aware of this and has not resolved it',
        N'none',
        N'No formal role and she has not been assigned one. Attends meals and some formal occasions. Time with Lady Zsofia, whose relationship with her is careful and genuinely warm. She writes letters she seals herself. She has declined three invitations to serve as an informal Atrament liaison.',
        N'She was sent back to Calyx deliberately by House Atrament as an intelligence asset, instructed to file reports through a channel embedded in her personal correspondence. She has been filing increasingly vague and useless reports for eighteen months, passively degrading the arrangement. She is waiting to see how long it takes Atrament to notice. She resents being used and has not yet decided what she will do when they do.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Korvarat Hall; her movement is technically free but socially bounded by the ambiguity of her position',
        N'0', N'0',
        N'woman in her late thirties, chestnut hair braided in Atrament court fashion, hazel eyes, olive composed face, hybrid Calyx-Atrament noble dress, poised bearing, stone manor hall, dark fantasy, divided loyalties',
        N'woman late thirties, chestnut braided hair Atrament style, hazel eyes, olive skin, hybrid noble dress, poised bearing, stone manor',
        0, 0
    );
    PRINT 'Dragomira Enyedi seeded.';
END
ELSE PRINT 'Dragomira Enyedi already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ilona Calyx-Fekete')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ilona Calyx-Fekete', N'ilona-calyx-fekete', N'canon', 1,
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
        @id, N'Ilona Calyx-Fekete', N'ilona-calyx-fekete', N'Ilona', N'Calyx-Fekete', N'Dame',
        N'human', N'human', N'female', N'she/her', 37, N'dead',
        N'Radovan''s elder sister; Paladin who died in the catastrophic infusion campaign twenty-five years ago; her portrait hangs in the war room and her judgment is still cited as precedent',
        N'Ilona was better than her brother at nearly everything the House required and had fewer opportunities to demonstrate it. She took her first infusion at twenty-two and survived; her second at twenty-five. She was the most capable military officer House Calyx had produced in a generation -- meticulous, honest with her officers about uncertainty, and constitutionally incapable of issuing an order she believed was wrong. She died at thirty-seven in the campaign that killed forty percent of the Corps, in the same infusion disaster that Radovan later blamed on the Practitioner. Her portrait shows her at thirty-three, before the last campaign. The family cites what she would have decided. It is the most powerful voice in the room and she is not there to correct it.',
        N'The dead whose presence structures everything. What she knew before she died, and what died with her, is the investigation no one living has been able to complete.',
        N'No POV.',
        N'House Calyx; elder sister of Radovan; died twenty-five years ago at age thirty-seven',
        186, 92, N'Paladin-built; substantially altered by two infusions; the record of someone the conservative Calyx protocol could not hold back',
        N'dark brown, close-cropped in her later years', N'close-cropped', N'short',
        N'pale luminous grey; changed by the second infusion', N'warm medium', N'regularized by multiple infusions; the specific clarity of altered skin',
        N'Evident enhancement -- significant height, altered proportions, the pale luminous eyes of a multiple-infusion Paladin; this is the record of what she was when she died',
        N'not recorded in her own words; known through the accounts of those who served beside her, who all describe the same thing: she moved as though she had already seen how the engagement would end',
        N'formal Calyx military dress in her portrait; practical Corps attire in all surviving accounts',
        N'Two infusions survived; Paladin-grade conservative Calyx protocol; significant height gain, altered musculature, changed eyes',
        N'She is dead. The record of her daily life is what others remember: early mornings at drill, meticulous correspondence, willing to be wrong in front of her officers if the wrong answer was the honest one.',
        N'The secret that died with her: she had discovered that the Transmutation Practitioner was receiving orders from someone outside the normal command chain to calibrate specific infusions for specific outcomes. She had compiled evidence. She carried it with her to the campaign. Everything she carried died with her, and the man she suspected of issuing those orders is still alive inside the House.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Dead; in life her territory was the eastern plains Corps and the border operations she commanded',
        N'0', N'0',
        N'portrait of a tall woman in her thirties, dark close-cropped hair, pale luminous grey eyes from Paladin infusion, warm medium skin, strong post-infusion build, formal Calyx military dress, the specific quality of someone who was better than her circumstances, stone hall memorial portrait, dark fantasy',
        N'portrait tall woman, dark close-cropped hair, pale luminous eyes, Calyx military dress, strong build, stone hall, dark fantasy memorial',
        0, 0
    );
    PRINT 'Ilona Calyx-Fekete seeded.';
END
ELSE PRINT 'Ilona Calyx-Fekete already exists.';
GO

-- ---------------------------------------------------------------------------
-- POLITICAL CABINET (7 characters)
-- ---------------------------------------------------------------------------

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Bogdan Szabo')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Bogdan Szabo', N'bogdan-szabo', N'canon', 1,
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
        @id, N'Bogdan Szabo', N'bogdan-szabo', N'Bogdan', N'Szabo', N'',
        N'human', N'human', N'male', N'he/him', 52, N'alive',
        N'Chancellor of House Calyx; manages all political correspondence, treaty negotiations, and alliance maintenance; has served three Lords',
        N'Bogdan Szabo has managed House Calyx''s political correspondence for twenty-two years across three lordships, which means he has outlasted two of his employers and intends to outlast the third. He is the kind of man who is never visibly the most important person in a room and is frequently the most important person in the room. He manages alliances through the patient accumulation of small obligations -- a delayed response here, an early release of information there -- and he has been doing it long enough that the pattern has become invisible even to himself. He presents to the Lord as cautious and to visitors as accommodating. He is neither.',
        N'The political machinery of the House given a face. His personal enrichment through treaty delay is the corruption that, if found, would unwind several of the House''s most important relationships simultaneously.',
        N'No POV.',
        N'House Calyx; eastern plains administrative staff; career diplomat and correspondence manager',
        175, 82, N'soft; the build of a man who has spent twenty years behind a desk and considers this a form of discipline',
        N'thinning grey', N'combed precisely over the thinning', N'short',
        N'grey', N'pale', N'slightly florid; the complexion of someone who eats and drinks at diplomatic functions as a professional obligation',
        N'none',
        N'controlled and minimal; he moves as little as possible and when he does move it is toward a specific purpose',
        N'cabinet formal wear in good fabric; House Calyx colors worn without personal expression; the clothes of someone who does not intend to be noticed for his appearance',
        N'none',
        N'Morning: correspondence review and drafting responses for the Lord''s signature. Midday: formal meetings with visiting envoys or trade representatives. Afternoon: the cabinet session. Evening: private correspondence he drafts himself, in a hand that does not appear in the House records.',
        N'For twelve years he has been systematically delaying certain agricultural treaty negotiations by days or weeks to allow secondary commodity deals to close first, generating a personal income through broker intermediaries. Over twelve years the aggregate sum would ruin him if it were attributed to a single source. He has been very careful about attribution.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Korvarat Hall; formal diplomatic travel to Compact grounds and allied Houses as required',
        N'0', N'0',
        N'man in his early fifties, thinning grey hair combed precisely, grey eyes, pale slightly florid face, cabinet formal wear in Calyx colors, controlled minimal movement, stone manor interior, dark fantasy bureaucratic authority',
        N'man fifties, thinning grey hair, grey eyes, pale face, formal Calyx cabinet dress, controlled posture, stone manor interior',
        0, 0
    );
    PRINT 'Bogdan Szabo seeded.';
END
ELSE PRINT 'Bogdan Szabo already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Csenge Horvath')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Csenge Horvath', N'csenge-horvath', N'canon', 1,
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
        @id, N'Csenge Horvath', N'csenge-horvath', N'Csenge', N'Horvath', N'',
        N'human', N'human', N'female', N'she/her', 47, N'alive',
        N'Spymaster of House Calyx; runs intelligence operations against rival Houses; has operatives in Fornax and Atrament; knows things she has decided not to report',
        N'Csenge Horvath runs House Calyx''s intelligence operations with the specific efficiency of someone who has decided that the information she collects is only as useful as the judgment she applies to it. She has operatives in Fornax monitoring arms production and in Atrament monitoring the diplomatic circuit. She receives their reports, evaluates them, and decides what the Lord needs to know -- which is not always everything she knows. She has been doing this for fourteen years and she has never been caught out in an omission significant enough to end her tenure. She is aware that this could change.',
        N'The intelligence apparatus of the House given a conscience it does not officially have. Her decision to suppress information about Lady Zsofia is the story''s live wire.',
        N'No POV.',
        N'House Calyx; intelligence operations, Korvarat Hall and wider network',
        167, 63, N'compact and unremarkable by design; she has cultivated physical inconspicuousness as a professional discipline',
        N'black', N'practical, pulled back flat', N'short',
        N'dark brown', N'medium warm', N'clear; the composure of someone who has spent decades presenting nothing readable',
        N'none',
        N'still and watchful; she stands where she can see multiple entrances; she is the only person in the cabinet who never sits with her back to a door',
        N'cabinet formal wear chosen to be forgettable; she does not wear the House colors prominently; the only person in her position who is harder to place at an official function than she should be',
        N'none',
        N'Morning review of operative reports. Analysis and decision on what reaches the Lord. Midday cabinet session. Afternoon running her own correspondence through channels the Lord does not have complete access to. She maintains a private cipher archive that she keeps physically on her person.',
        N'She has known about Lady Zsofia''s correspondence with House Atrament''s Spymaster for three years and has chosen not to report it. Her reason: she cross-checked the correspondence against Calyx''s negotiating outcomes over fifteen years and concluded the channel has been protective. She may be wrong. She has decided she would rather be wrong than report it -- and she is aware that this decision has made her complicit in whatever the Lady is doing.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Korvarat Hall and her operative network; she travels without announcement when her work requires it',
        N'0', N'0',
        N'woman in her late forties, black hair pulled back flat, dark brown eyes, medium warm skin, forgettable cabinet dress, still watchful posture, always positioned with sight lines to multiple entrances, stone manor, dark fantasy intelligence',
        N'woman late forties, black flat pulled-back hair, dark eyes, forgettable formal dress, watchful still posture, stone manor interior',
        0, 0
    );
    PRINT 'Csenge Horvath seeded.';
END
ELSE PRINT 'Csenge Horvath already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Miklos Fekete-Tanacs')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Miklos Fekete-Tanacs', N'miklos-fekete-tanacs', N'canon', 1,
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
        @id, N'Miklos Fekete-Tanacs', N'miklos-fekete-tanacs', N'Miklos', N'Fekete-Tanacs', N'',
        N'human', N'human', N'male', N'he/him', 63, N'alive',
        N'House Archivist of Calyx; maintains all records, genealogy, treaty texts, and Scrying logs; has held this position for thirty-five years and has made himself the only person who knows where everything is',
        N'Miklos Fekete-Tanacs has been the House Archivist for thirty-five years, which means he has outlasted two major wars, three changes of senior cabinet, and one attempted reorganization of the archive system by a Lord who gave up after six weeks. He knows where everything is because he put it there. He also knows where several things are that the current Lord does not know exist. He is meticulous, methodical, and pleasant in the way that people who do not need anyone''s approval are pleasant -- it costs him nothing.',
        N'The keeper of records who has buried the one record that would change everything -- the genealogical gap in the ruling line that he discovered and sealed twenty-two years ago.',
        N'No POV.',
        N'House Calyx; archivist staff; eastern plains; the Fekete-Tanacs family are a minor clerical line with a long history of House service',
        171, 74, N'thin; the build of a man who moves through stacks and corridors and considers the archive his physical world',
        N'white, wispy', N'loose; he does not manage it', N'short',
        N'watery blue', N'pale', N'papery; the indoor pallor of a man who has spent thirty-five years in rooms lit by lanterns',
        N'none',
        N'careful and precise; moves through the archive without disturbing anything; outside the archive he looks slightly lost',
        N'archivist practical dress in muted colors; he does not wear the House colors in any pronounced way; his clothes are good and old and maintained',
        N'none',
        N'Every morning in the archive before anyone else arrives. Midday the mandatory cabinet briefing on records requests. Afternoon back in the archive. He has not missed a day of work in eleven years and considers this a personal record worth maintaining.',
        N'Twenty-two years ago he discovered a genealogical irregularity in the Calyx-Fekete birth records -- a gap coinciding with the current Lord''s birth -- that would complicate the succession significantly. He sealed the relevant documents in a sub-archive accessible only through a key he keeps on his person. He visits the sealed section twice a year and has never opened what he sealed. He has told himself he is protecting the House. He is aware this is also protecting himself from the consequences of having found it.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'The Calyx archive and Korvarat Hall; he rarely leaves the estate and does not travel to Compact grounds if he can avoid it',
        N'0', N'0',
        N'old man in his early sixties, white wispy hair, watery blue eyes, pale papery skin, muted archivist practical dress, careful precise movement, surrounded by records and lantern light, stone archive interior, dark fantasy scholar',
        N'old man sixties, white wispy hair, pale blue eyes, pale papery face, muted practical dress, careful posture, stone archive with lanterns',
        0, 0
    );
    PRINT 'Miklos Fekete-Tanacs seeded.';
END
ELSE PRINT 'Miklos Fekete-Tanacs already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Erzsebet Podmanicky')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Erzsebet Podmanicky', N'erzsebet-podmanicky', N'canon', 1,
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
        @id, N'Erzsebet Podmanicky', N'erzsebet-podmanicky', N'Erzsebet', N'Podmanicky', N'',
        N'human', N'human', N'female', N'she/her', 44, N'alive',
        N'Trade Ambassador of House Calyx; manages commerce with other Houses and border markets; presents as warmly diplomatic and is the most calculating person in the cabinet',
        N'Erzsebet Podmanicky manages House Calyx''s agricultural trade relationships with the efficiency of someone who understands that the grain contracts are, in the Cauld''s current configuration, more important than most military agreements. She is warm in meetings, precise in private, and has cultivated a reputation for reliability that has become one of the House''s most valuable commercial assets. She is the person other Houses'' trade ambassadors trust to produce an honest accounting. This reputation is real and it is also, in the specific way that all commercial reputations are, constructed.',
        N'The person whose reliability is the House''s commercial credibility -- and who has been quietly skimming that credibility for twelve years.',
        N'No POV.',
        N'House Calyx; trade and commercial affairs; eastern plains and border market operations',
        170, 67, N'elegant; the build of someone who considers how they are perceived a professional tool',
        N'warm brown', N'arranged carefully for occasions; practical when not receiving visitors', N'medium',
        N'brown', N'warm olive', N'clear; maintained with care appropriate to her professional position',
        N'none',
        N'easy and socially calibrated; she reads rooms and adjusts within them; she is the best mover in the cabinet',
        N'the best clothes she wears, period; she treats her appearance as a commercial argument and has the budget for it',
        N'none',
        N'Morning review of trade reports and commodity prices. Receiving visiting merchants and allied-House trade representatives. Cabinet session midday. Afternoon managing the correspondence from border market factors. She works later than anyone else in the cabinet.',
        N'For twelve years she has been skimming a small but consistent percentage of every major agricultural contract through broker intermediaries, depositing the aggregate into a false-name merchant account in Lacerta-adjacent territory. She began because she wanted security that the House could not give her. She has not stopped because stopping would require explaining how it started. The sum is now substantial enough that discovery would be a terminal event.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Korvarat Hall; border markets and allied House trade installations; she travels more than any other cabinet member',
        N'0', N'0',
        N'woman in her mid-forties, warm brown carefully arranged hair, brown eyes, warm olive clear skin, excellent clothes that function as a commercial argument, easy socially calibrated movement, stone manor reception room, dark fantasy merchant diplomat',
        N'woman mid-forties, warm brown hair, brown eyes, olive skin, excellent formal dress, easy socially calibrated movement, stone manor reception room',
        0, 0
    );
    PRINT 'Erzsebet Podmanicky seeded.';
END
ELSE PRINT 'Erzsebet Podmanicky already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ambrus Tarjan')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ambrus Tarjan', N'ambrus-tarjan', N'canon', 1,
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
        @id, N'Ambrus Tarjan', N'ambrus-tarjan', N'Ambrus', N'Tarjan', N'Brother',
        N'human', N'human', N'male', N'he/him', 55, N'alive',
        N'Liturgy Liaison attached to House Calyx; officially represents the Liturgy to the House and the House to the Liturgy; reports to both; trusted by neither as much as they believe',
        N'Brother Ambrus Tarjan has been the Liturgy''s official presence at Korvarat Hall for sixteen years. He attends every significant ceremony, every major departure, every death. He files reports to the Liturgy on House Calyx''s doctrinal compliance and on the state of the Sphere 31 transit operations in the territory. He is present at everything and trusted by both sides in the specific way that intermediaries are trusted -- as long as nothing important is actually at stake. He has a genuinely warm relationship with the Chaplain, which is the only relationship in the House he does not manage for professional purposes.',
        N'The institutional Liturgy presence -- the character through whom the Liturgy''s actual agenda intersects with the House''s daily life. His management of unregistered Sphere 31 persons is a mechanism the House does not know exists.',
        N'No POV.',
        N'House Calyx, Liturgy affiliate; attached to Korvarat Hall sixteen years; reports to Liturgy central operations',
        174, 79, N'paunchy; the softness of a man whose discipline is intellectual rather than physical',
        N'bald with a grey fringe', N'the fringe maintained with more care than is strictly necessary', N'short',
        N'pale grey', N'ruddy pale', N'the specific complexion of someone who is slightly uncomfortable at most temperatures',
        N'none',
        N'deliberate and unhurried; he occupies space with the confidence of someone whose institutional backing is larger than his personal authority',
        N'Liturgy formal robes for ceremonies; practical clerical dress otherwise; always the Liturgy emblem prominently displayed',
        N'none',
        N'Morning Liturgy devotional practice alone. Attending House ceremonies and formal occasions. Filing reports -- one copy to the House, one to the Liturgy, and a third that goes somewhere neither party sees. Afternoon meetings with the Chaplain, which are the most genuinely unguarded part of his week.',
        N'He has been systematically misreporting the number of Sphere 31 persons arriving in Calyx territory, allowing some to remain unregistered in House service outside Liturgy oversight. He does this not out of mercy -- he does it because unregistered persons can be placed in positions that generate leverage over both the persons and the House, leverage the Liturgy can call on without formally acknowledging it exists.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Korvarat Hall and surrounding Calyx territory; Liturgy transit stations on the regional circuit; he travels to Liturgy central twice yearly',
        N'0', N'0',
        N'man in his mid-fifties, bald with grey fringe maintained carefully, pale grey eyes, ruddy pale complexion, Liturgy formal robes with emblem, deliberate unhurried movement, stone manor chapel or hall, dark fantasy institutional authority',
        N'man mid-fifties, bald with grey fringe, pale grey eyes, ruddy face, Liturgy robes with emblem, deliberate posture, stone chapel interior',
        0, 0
    );
    PRINT 'Ambrus Tarjan seeded.';
END
ELSE PRINT 'Ambrus Tarjan already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Adorjan Nemes')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Adorjan Nemes', N'adorjan-nemes', N'canon', 1,
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
        @id, N'Adorjan Nemes', N'adorjan-nemes', N'Adorjan', N'Nemes', N'',
        N'human', N'human', N'male', N'he/him', 50, N'alive',
        N'Treasurer and Chamberlain of House Calyx; manages House finances and resource allocation; meticulous, anxious, and carrying information he has decided the Lord cannot be told',
        N'Adorjan Nemes has managed House Calyx''s finances for fourteen years with the specific anxiety of someone who understands exactly how thin the margin is between an agricultural House in the Cauld and a House that has stopped being viable. He presents the accounts in cabinet sessions with enough complexity to convey competence and enough simplification to prevent the cabinet from understanding what they are actually looking at. He has been doing this for three years. He does not enjoy it. He does not know what he would do if he stopped.',
        N'The treasurer sitting on the information that the House''s agricultural accounts are in critical condition -- the character whose silence is making the succession crisis worse at exactly the moment it is becoming unavoidable.',
        N'No POV.',
        N'House Calyx; treasury and household accounts; Korvarat Hall',
        172, 77, N'medium; the build of a man who forgets to eat when the accounts are difficult, which is most of the time now',
        N'sandy grey', N'combed with the care of someone who manages anxiety through small controlled actions', N'short',
        N'brown', N'sallow', N'the complexion of someone who has not been sleeping enough for three years',
        N'none',
        N'slightly forward-leaning; he moves toward things as though meeting them halfway will make them less alarming',
        N'cabinet formal dress, always slightly wrinkled by midday; he does not notice this',
        N'none',
        N'In the accounts before dawn. Cabinet session midday. Afternoon managing requisition requests and supply correspondence. He has been asking the kitchen to send food to his office for three years because he cannot reliably stop working long enough to attend meals.',
        N'The House''s agricultural accounts are in worse condition than anyone in the cabinet knows. Three consecutive partial harvests and an extended military supply commitment have quietly eroded the war chest to a level where a bad fourth season would force renegotiation of the House''s treaty obligations. He has not told the Lord. He has been waiting for the harvest figures to improve. They have not improved.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Korvarat Hall; the treasury offices and account rooms; he rarely leaves the estate',
        N'0', N'0',
        N'man in his early fifties, sandy grey hair carefully combed, brown anxious eyes, sallow complexion from insufficient sleep, slightly wrinkled cabinet formal dress, forward-leaning posture, stone treasury office surrounded by account books, dark fantasy',
        N'man fifties, sandy grey combed hair, anxious brown eyes, sallow face, wrinkled formal dress, forward-leaning posture, stone office with account books',
        0, 0
    );
    PRINT 'Adorjan Nemes seeded.';
END
ELSE PRINT 'Adorjan Nemes already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Veronika Szanto')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Veronika Szanto', N'veronika-szanto', N'canon', 1,
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
        @id, N'Veronika Szanto', N'veronika-szanto', N'Veronika', N'Szanto', N'',
        N'human', N'human', N'female', N'she/her', 36, N'alive',
        N'House Calyx diplomat currently posted to House Ophiuchus; able representative; extending her posting for reasons her dispatches do not fully explain',
        N'Veronika Szanto was posted to House Ophiuchus eighteen months ago to manage the relationship between Calyx''s agricultural trade interests and Ophiuchus''s scholarly appetite for Calyx grain data, which is more significant than it sounds -- Ophiuchus uses agricultural yield modeling in its membrane research. She is good at her job. Her dispatches are thorough and her negotiations have produced two favorable agreements. She has also extended her posting twice, citing legitimate diplomatic progress that is real but incomplete as a reason. House Calyx has not yet sent anyone to check on her.',
        N'The diplomat abroad whose personal complication is going to eventually require a recall -- at exactly the moment her absence from Korvarat is becoming notable.',
        N'No POV.',
        N'House Calyx; diplomatic corps; currently attached to House Ophiuchus installation; eastern plains origin',
        168, 60, N'lively; the build of someone whose physical energy matches the intellectual pace she maintains',
        N'dark auburn', N'worn loosely when not at formal occasions', N'medium',
        N'green', N'warm medium', N'clear; the outdoor health of someone who was not expecting to like where she was posted and does',
        N'none',
        N'animated and quick; she speaks with her hands at Ophiuchus in a way she would not permit herself at Korvarat; she has become slightly more Ophiuchus in her manner than she has noticed',
        N'diplomatic formal dress in Calyx colors for official occasions; considerably less formal dress in practice, which is an Ophiuchus influence she has not resisted',
        N'none',
        N'At House Ophiuchus installation. Morning meetings with her Ophiuchus counterpart. Afternoon dispatch drafting and agricultural data coordination. Evenings that have increasingly included a specific Ophiuchus scholar named Sebastiano whose work on membrane physics she has found genuinely interesting and who has found her likewise.',
        N'She has been extending her posting because she has fallen in love with an Ophiuchus scholar named Sebastiano, who studies membrane physics and who does not know she is a diplomat whose assignment is finite. She is afraid that if she is recalled, the assignment will end and so will everything else. She has been filing genuine diplomatic progress as cover. She has not decided what she will do when the cover is no longer sufficient.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Ophiuchus installation; formally assigned abroad; her Calyx territory is effectively suspended for the duration of the posting',
        N'0', N'0',
        N'woman in her mid-thirties, dark auburn hair loose, green eyes, warm medium skin, Calyx diplomatic dress worn less formally than at home, lively animated movement, Ophiuchus stone scholarly interior, dark fantasy diplomatic',
        N'woman mid-thirties, dark auburn hair loose, green eyes, warm skin, diplomatic dress worn informally, animated movement, Ophiuchus stone interior',
        0, 0
    );
    PRINT 'Veronika Szanto seeded.';
END
ELSE PRINT 'Veronika Szanto already exists.';
GO

-- ---------------------------------------------------------------------------
-- MILITARY COMMAND (9 characters)
-- ---------------------------------------------------------------------------

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Istvan Racz')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Istvan Racz', N'istvan-racz', N'canon', 1,
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
        @id, N'Istvan Racz', N'istvan-racz', N'Istvan', N'Racz', N'Commander',
        N'human', N'human', N'male', N'he/him', 54, N'alive',
        N'Commander of the Calyx Myrmidon Corps; Paladin by multiple infusions; severe, methodical, and haunted by his survival of the campaign that killed forty percent of his cohort',
        N'Istvan Racz runs the Calyx Corps with the specific severity of someone who has seen what inadequate preparation costs and has never forgiven himself for not having prevented it. He survived the infusion campaign twenty-five years ago that killed most of his cohort. He does not know why he survived and the not-knowing has shaped every decision he has made since. He is an excellent commander in the sense that the Corps is well-run, well-drilled, and produces consistent results. He is a difficult commander in the sense that he tolerates no waste, no excuses, and no departure from protocol that he has not personally authorized.',
        N'The Corps''s institutional authority -- and the man whose survival of the catastrophic campaign is connected to the secret Nandor Takacs has been carrying for twenty-five years.',
        N'No POV.',
        N'House Calyx; Myrmidon Corps command; eastern plains and campaign territory; Paladin by multiple infusions',
        195, 112, N'post-human-proportioned; the Paladin infusions have built him substantially beyond his original frame; he is difficult to overlook in any room',
        N'iron grey', N'short, military practical', N'short',
        N'pale luminous; changed by multiple infusions', N'pallid; the specific pallor of someone whose skin has been through more than one infusion', N'the regularized clarity of post-Paladin skin; not healthy-looking, but not unwell',
        N'Evident enhancement -- significant height gain, altered proportions, changed eyes; he is visibly post-Knight in a way that is unmistakable even to people who do not know what they are looking at',
        N'still and deliberate; the economy of a large man who has learned to move without taking up more space than he intends; never hurried',
        N'Corps command dress; functional and worn without ornament; he has removed every decorative element from the standard Commander''s uniform',
        N'Multiple infusions survived; Paladin-grade conservative Calyx protocol; the most extensively transformed officer the House has produced since Ilona Calyx-Fekete',
        N'Morning Corps inspection. Midday planning session with the Captains. Afternoon reviewing operational reports and supply assessments. He does not attend social functions unless directly ordered by the Lord. He runs the Corps''s drill himself twice a week.',
        N'He survived the infusion that killed most of his cohort twenty-five years ago, and he has never understood why. He is afraid to ask Nandor Takacs directly because he suspects the answer would mean his survival was not random -- that he was selected for it. He does not know for what purpose. The not-knowing has been with him for twenty-five years and he has built his entire command philosophy around the premise that he must earn the survival in retrospect, every day, by the quality of the Corps.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Calyx Corps territory and eastern plains campaign range; Korvarat Hall for command meetings',
        N'0', N'0',
        N'very large man in his mid-fifties, iron grey short hair, pale luminous eyes from Paladin infusion, pallid regularized skin, significantly post-human proportions from multiple infusions, Corps command dress without ornament, still deliberate movement, stone Corps hall, dark fantasy military authority',
        N'very large man fifties, iron grey hair, pale luminous eyes, pallid skin, post-human build from Paladin infusions, functional military dress, still deliberate posture, stone military hall',
        0, 0
    );
    PRINT 'Istvan Racz seeded.';
END
ELSE PRINT 'Istvan Racz already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Katalin Meszaros')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Katalin Meszaros', N'katalin-meszaros', N'canon', 1,
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
        @id, N'Katalin Meszaros', N'katalin-meszaros', N'Katalin', N'Meszaros', N'Dame',
        N'human', N'human', N'female', N'she/her', 38, N'alive',
        N'First Captain of the Calyx Corps; Dame by first infusion; ground operations commander; excellent tactician with a limitation she has been routing around for four years',
        N'Katalin Meszaros is the best tactical planner in the Calyx Corps and the person Commander Racz trusts most in operational planning. She was born in the agricultural territories; her family still works the fields east of Korvarat. She took the infusion at thirty-two and survived. She commands ground operations with precision and a specific reluctance to explain her tactical choices in more detail than necessary, which Racz has interpreted as confidence and which is not entirely incorrect.',
        N'The first captain whose hidden constraint -- she will not order crop destruction -- is going to intersect catastrophically with the operational requirements of the next major campaign.',
        N'No POV.',
        N'House Calyx Corps, ground operations command; Knight by first infusion at thirty-two; born in the Calyx agricultural territories',
        175, 72, N'lean-muscled; the infusion added height and density to an already active frame',
        N'dark, pulled back tight for field operations', N'severe practical pull-back', N'short when down',
        N'dark brown', N'weathered medium', N'weathered; she has her family''s outdoor complexion and the infusion has not changed it',
        N'Subtle height gain, increased density; perceptible at close range to someone who knows what they are looking at',
        N'direct and efficient; the movement of someone who has integrated the infusion changes and whose default is action over stillness',
        N'Corps field dress for operations; formal Corps dress for Korvarat appearances, worn with the specific discomfort of someone who would rather be somewhere else',
        N'First infusion survived at thirty-two; conservative Calyx protocol; enhanced musculature and bone density',
        N'Before dawn: field assessment and troop positioning review. Morning operations briefing with the Commander. Midday planning. Afternoon with her unit in training. She rides to the agricultural territory east of Korvarat once a month to see her family, which she has not reported to the Corps roster as personal time.',
        N'She was born in the agricultural territories and her family still works the fields. For four years she has been routing her unit around known crop areas during operations, claiming tactical necessity. The tactical justifications are real but they are not the reason. She cannot order a crop burn she knows will destroy a family''s season. She has managed this so far. She knows she cannot manage it in every scenario the Corps might face.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Calyx Corps operational territory and eastern plains campaign range',
        N'0', N'0',
        N'woman in her late thirties, dark hair pulled back severely, dark brown eyes, weathered medium skin, lean-muscled build with Knight infusion height gain, Corps field dress, direct efficient movement, stone Corps hall or field terrain, dark fantasy military',
        N'woman late thirties, dark hair pulled back severely, dark eyes, weathered skin, lean military build, Corps dress, direct posture, stone military interior or field',
        0, 0
    );
    PRINT 'Katalin Meszaros seeded.';
END
ELSE PRINT 'Katalin Meszaros already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Gergely Balaton')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Gergely Balaton', N'gergely-balaton', N'canon', 1,
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
        @id, N'Gergely Balaton', N'gergely-balaton', N'Gergely', N'Balaton', N'Ser',
        N'human', N'human', N'male', N'he/him', 42, N'alive',
        N'Second Captain of the Calyx Corps; Ser by first infusion; garrison and defense commander; reliable, competent, and quietly subsidizing the wages of his lowest-paid soldiers out of garrison funds',
        N'Gergely Balaton commands the Calyx garrison and defense operations with the steady competence of someone who understands that a garrison that feels expendable will eventually behave like one. He has petitioned the Lord three times in seven years for wage adjustments for the lower ranks. The petitions were received and tabled. He has found a different solution. He is not comfortable with the solution, but he is more comfortable with it than with the alternative.',
        N'The garrison commander whose quiet redistribution of funds is going to surface in an accounts audit at the worst possible moment -- which is the moment the Treasurer''s hidden crisis is also coming to light.',
        N'No POV.',
        N'House Calyx Corps, garrison and defense command; Knight by first infusion; eastern plains',
        185, 92, N'broad; the infusion added to a naturally solid frame; he is physically imposing without intending to be',
        N'brown, short', N'military short', N'short',
        N'blue-grey', N'ruddy', N'weathered and ruddy; the face of someone who runs garrison inspections in all weather',
        N'Subtle height gain, increased density; the additional breadth is more noticeable on his naturally broad frame',
        N'solid and grounded; he plants himself where he stands; does not fidget; the garrison commander''s habit of being the fixed point in a moving situation',
        N'Corps garrison dress; functional; he has a dress uniform he wears to Korvarat meetings that fits correctly and that he is proud of without being able to say so',
        N'First infusion survived; conservative Calyx protocol; enhanced musculature and bone density',
        N'Morning garrison inspection in all weather. Midday operational briefing with the Commander. Afternoon managing the defense roster and supply requisitions. He reviews the garrison accounts himself every week, which is how he knows exactly how much room he has and exactly how much he has been using.',
        N'For five years he has been supplementing the wages of the garrison''s lowest-paid soldiers from discretionary funds, recording the difference in the accounts as supply costs. He began because the Lord would not hear petitions. He has continued because he has no better solution and cannot stop without explaining the discrepancy. He reviews the gap monthly. It is growing slowly. The Treasurer''s anxiety about supply requests has made Gergely''s own requests more scrutinized, which is making the concealment harder.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Calyx garrison installations and defense perimeter; eastern plains',
        N'0', N'0',
        N'broad man in his early forties, brown short hair, blue-grey eyes, ruddy weathered face, solid build enhanced by Knight infusion, Corps garrison dress, solid grounded posture, stone garrison interior, dark fantasy military',
        N'broad man early forties, brown hair, blue-grey eyes, ruddy weathered face, solid build, garrison dress, grounded posture, stone garrison interior',
        0, 0
    );
    PRINT 'Gergely Balaton seeded.';
END
ELSE PRINT 'Gergely Balaton already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Orsolya Veres')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Orsolya Veres', N'orsolya-veres', N'canon', 1,
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
        @id, N'Orsolya Veres', N'orsolya-veres', N'Orsolya', N'Veres', N'Dame',
        N'human', N'human', N'female', N'she/her', 40, N'alive',
        N'Specialist Captain commanding Scrying installation defense; Dame by first infusion; former Scrying operator who cross-trained to military; has not been the same since something she saw ten years ago',
        N'Orsolya Veres spent eight years as a Scrying operator before she requested a military transfer that everyone in the installation was surprised by and that she has never fully explained. She cross-trained, took the infusion at thirty-three, survived. She now commands the defense perimeter around the Calyx Scrying installation with a thoroughness that the Commander considers excessive and that she considers the minimum. She is calm, technically precise, and carries something she will not talk about.',
        N'The specialist who saw something in the apparatus and transferred out of it -- the character whose unexplained knowledge is a door into the Scrying installation''s secrets.',
        N'No POV.',
        N'House Calyx Corps, Scrying installation defense; Dame by first infusion at thirty-three; formerly of the Scrying installation staff',
        174, 69, N'wiry; the infusion refined rather than enlarged her frame; she is compact and fast',
        N'black, cropped', N'cropped close, military', N'short',
        N'dark', N'warm medium', N'clear; the regularization of the infusion is evident in the smoothness of her skin',
        N'Subtle height gain, increased density; on a wiry frame the density is more notable than the height',
        N'watchful and controlled; she surveys perimeters as a habit she cannot turn off even inside Korvarat Hall',
        N'Corps specialist dress; functional and without ornament; she wears it with the ease of someone who stopped thinking about clothes the day she transferred',
        N'First infusion survived at thirty-three; conservative Calyx protocol; enhanced musculature and density on a naturally wiry frame',
        N'Perimeter inspection before dawn. Morning briefing with the Scrying installation''s Head Operator. Midday Corps command session. Afternoon running the installation defense rotation. She does not enter the apparatus room anymore. Everyone has noticed this. No one has asked.',
        N'Ten years ago, during a long watch session, she saw in the Scrying apparatus a parallel world in which House Calyx did not exist -- not destroyed, not conquered, simply absent from the history of the Cauld as though it had never formed. She transferred to the military after that session. She has never described what she saw to anyone. She does not enter the apparatus room because she is afraid she will see it again.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Calyx Scrying installation and its defense perimeter; Corps operational range',
        N'0', N'0',
        N'woman in her early forties, black cropped hair, dark eyes, warm medium clear skin, wiry build enhanced by Knight infusion, Corps specialist dress without ornament, watchful perimeter-surveying posture, stone installation exterior, dark fantasy military',
        N'woman early forties, black cropped hair, dark eyes, warm skin, wiry military build, functional dress, watchful posture, stone installation exterior',
        0, 0
    );
    PRINT 'Orsolya Veres seeded.';
END
ELSE PRINT 'Orsolya Veres already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Benedek Kiraly')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Benedek Kiraly', N'benedek-kiraly', N'canon', 1,
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
        @id, N'Benedek Kiraly', N'benedek-kiraly', N'Benedek', N'Kiraly', N'',
        N'human', N'human', N'male', N'he/him', 49, N'alive',
        N'Infirmary Commander; heads the Corps field hospital attached to the Calyx Myrmidon; technically brilliant; has conducted unauthorized procedures on Sphere 31 soldiers that he has never recorded',
        N'Benedek Kiraly has been running the Calyx Corps infirmary for fifteen years with the specific emotional distance of someone who has decided that the work is more important than the feelings about the work. He is technically brilliant -- the Corps''s casualty rate in his tenure is the lowest it has been in two generations -- and interpersonally minimal. He asks questions about symptoms and does not ask questions about anything else. He treats everyone who comes through the infirmary with the same quality of attention regardless of their rank, which has made him trusted by the soldiers and respected by the command in the specific way that useful neutrality is respected.',
        N'The infirmary''s technical excellence concealing unauthorized experiments -- the character whose unrecorded procedures are both the Corps''s best-kept secret and a potential weapon for whoever finds out about them.',
        N'No POV.',
        N'House Calyx Corps, infirmary command; attached to the Calyx Myrmidon; not transmuted',
        175, 75, N'precise; the build of someone who keeps himself in functional condition as a professional discipline, no more',
        N'sandy brown', N'practical, unmaintained beyond the minimum', N'short',
        N'light grey', N'pale', N'clear in the clinical way of someone who monitors their own health as an instrument of their work',
        N'none',
        N'minimal and precise; he occupies the minimum space required by the current task; does not gesture unless demonstrating a procedure',
        N'infirmary practical dress; he is never in anything else; the Corps dress uniform he owns has been worn to one formal occasion in fifteen years',
        N'none; he was considered for infusion at forty and declined without explanation',
        N'In the infirmary from before dawn. Rounds of the wards. Morning triage and case review. Afternoon surgical and treatment work. He eats at the infirmary desk. He has not left the Corps installation voluntarily for a social purpose in two years.',
        N'In the aftermath of two campaigns, he conducted unauthorized procedures on Sphere 31 soldiers who were not recovering through standard treatment -- experimental applications of Catalyst residue in controlled doses. Some of the procedures worked. He has never recorded them in official logs. The soldiers who recovered were told their recovery was natural. He has the full record in a private notation system he keeps in his own shorthand, hidden in the infirmary supply inventory.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Calyx Corps infirmary and installation; he almost never leaves the Corps grounds',
        N'0', N'0',
        N'man in his late forties, sandy brown hair unmaintained, light grey eyes, pale clinical skin, precise minimal build, infirmary practical dress, minimal precise movement, stone infirmary interior with medical equipment, dark fantasy clinical authority',
        N'man late forties, sandy hair, grey eyes, pale face, precise build, infirmary practical dress, minimal posture, stone infirmary interior',
        0, 0
    );
    PRINT 'Benedek Kiraly seeded.';
END
ELSE PRINT 'Benedek Kiraly already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Rozalia Fekete')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Rozalia Fekete', N'rozalia-fekete', N'canon', 1,
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
        @id, N'Rozalia Fekete', N'rozalia-fekete', N'Rozalia', N'Fekete', N'',
        N'human', N'human', N'female', N'she/her', 51, N'alive',
        N'Senior Sergeant of the Calyx Corps; twenty-six years of service; the institutional memory of the Corps; knows every officer''s weaknesses and every soldier''s history',
        N'Rozalia Fekete has been in the Calyx Corps for twenty-six years. She has served under four Commanders and has outlasted all of them except the current one. She knows the Corps''s institutional history in the way that only someone who was present for it can know it -- not from records but from having been there, having made decisions, having seen consequences. Commanders consult her when they want to know how something was done before, or whether something has been tried. She answers precisely and says nothing more than was asked. She runs her soldiers with the fairness of someone who has personally witnessed what unfairness costs.',
        N'The institutional memory of the Corps -- and the character carrying a specific act of obedience that she cannot undo and cannot explain without making it worse.',
        N'No POV.',
        N'House Calyx Corps; twenty-six years of service; eastern plains and campaign territory',
        167, 73, N'hard; the specific hardness of a body that has been in active military service for twenty-six years and has not stopped',
        N'grey with brown remaining', N'short, practical, no concession to appearance', N'short',
        N'dark brown', N'weathered medium', N'weathered; scarred at the jaw from a campaign engagement twelve years ago',
        N'none',
        N'upright and contained; the posture of someone who stopped letting her body show what she was thinking before she was thirty',
        N'Corps working dress; worn smooth in places from years of use; she has never replaced any piece of it before it needed to be replaced',
        N'none',
        N'First to rise in the Corps barracks. Morning inspection of the lower ranks before the officers appear. Running the sergeants'' briefing. Afternoon managing the disciplinary and training roster. She is the last person the Commander speaks to before issuing a significant order and the first he tells when the order has consequences he did not anticipate.',
        N'Twenty-two years ago she was ordered to execute a soldier for desertion. She carried out the order. She knew the soldier -- Ander Biro, from the Danube basin villages -- and she knew his absence had been caused by an administrative error: a patrol order that had been lost in the paperwork and never reached him. She obeyed the order. He died. She has run the Corps with absolute fairness ever since, as though that could balance something that it cannot.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Calyx Corps barracks and training grounds; campaign range when deployed',
        N'0', N'0',
        N'woman in her early fifties, grey and brown close-cropped hair, dark brown eyes, weathered medium skin with jaw scar from old campaign, hard military build from twenty-six years service, Corps working dress worn smooth with use, upright contained posture, stone Corps barracks, dark fantasy veteran',
        N'woman early fifties, grey brown close-cropped hair, dark eyes, weathered scarred jaw, hard military build, worn Corps dress, upright posture, stone barracks',
        0, 0
    );
    PRINT 'Rozalia Fekete seeded.';
END
ELSE PRINT 'Rozalia Fekete already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Laszlo Barath')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Laszlo Barath', N'laszlo-barath', N'canon', 1,
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
        @id, N'Laszlo Barath', N'laszlo-barath', N'Laszlo', N'Barath', N'',
        N'human', N'human', N'male', N'he/him', 48, N'alive',
        N'Veteran Calyx Corps soldier near retirement; twenty-three years of service; deeply tired; planning a small farm; carrying something he has never told anyone',
        N'Laszlo Barath has been in the Calyx Corps for twenty-three years and has not made it past senior soldier because he has never wanted more than his current position requires. He is wry, quiet, and fundamentally decent in the specific way that very tired people are decent -- it costs them something and they pay it anyway. He is well-liked in the lower ranks in a way that officers are not. He has three months until he is eligible to draw his retirement allocation and buy the farm plot east of the Danube basin that he has been thinking about for six years.',
        N'The ordinary soldier near retirement -- whose carried archive of the dead is the story''s access to the full human cost of what the House and the Corps have been doing.',
        N'No POV.',
        N'House Calyx Corps; twenty-three years of service; eastern plains campaign range',
        174, 80, N'medium; worn down from years of active service in the way that is not quite injury but is not not-injury either',
        N'greying brown', N'unstyled', N'short',
        N'hazel', N'weathered medium', N'weathered; the outdoor wear of someone who has spent twenty-three years in the field',
        N'none',
        N'unhurried and slightly off to one side; he has the specific posture of someone who has learned to occupy space that does not attract attention',
        N'Corps working dress that is clean but not particularly maintained; he has stopped caring about the details of his appearance in the way that someone does when they can see the end of the thing',
        N'none; never offered infusion consideration, which suits him',
        N'Regular duties at the Corps, now mostly administrative support and training assistance for younger soldiers. He spends significant time in the afternoons alone, which no one questions because it looks like rest. He is writing. Not correspondence -- something else.',
        N'He carries, physically, in a waterproofed packet inside his Corps kit, the letters of every soldier under his command who died without family to send their effects to. Not to deliver them -- there is no one. He carries them because someone should. He has been doing this for eleven years. The packet is now substantial. He intends to bury it on whatever farm he manages to buy, in the corner of the plot nearest the road. He has not told anyone this.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Calyx Corps barracks and immediate territory; campaign range during active deployment, which he hopes is finished',
        N'0', N'0',
        N'man in his late forties, greying brown unstyled hair, hazel eyes, weathered medium face, worn-down medium build, Corps working dress clean but unmaintained, unhurried off-to-one-side posture, stone barracks or outdoor field, dark fantasy veteran near retirement',
        N'man late forties, greying brown hair, hazel eyes, weathered face, worn Corps dress, unhurried posture, stone barracks, dark fantasy',
        0, 0
    );
    PRINT 'Laszlo Barath seeded.';
END
ELSE PRINT 'Laszlo Barath already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Eszter Varga')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Eszter Varga', N'eszter-varga', N'canon', 1,
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
        @id, N'Eszter Varga', N'eszter-varga', N'Eszter', N'Varga', N'',
        N'human', N'human', N'female', N'she/her', 24, N'alive',
        N'Junior officer of the Calyx Corps; recently distinguished in a border skirmish; being watched by the command; her official report contains a significant omission',
        N'Eszter Varga distinguished herself eight months ago during a border skirmish in a way that the Corps command has been discussing since. She was commended for decisive action in a difficult engagement. She has been watched closely since then, which she finds both motivating and slightly alarming. She is quick, technically capable, and still in the phase of military service where she is calibrating when to follow orders and when to act on judgment. She has now acted on judgment once and been rewarded for it. She does not know if this is the lesson the Corps intended to teach her.',
        N'The junior officer being watched for potential -- whose commendation is built on an omission that would produce a very different assessment if the truth were known.',
        N'No POV.',
        N'House Calyx Corps; junior officer rank; eastern plains and border territory',
        168, 62, N'quick; lean and fast; she has not yet reached the physical plateau that the years bring',
        N'dark', N'pulled back for field operations, loose otherwise', N'medium',
        N'dark', N'warm medium', N'clear; young; the outdoor health of someone who has been in the field for two years',
        N'none',
        N'quick and attentive; she orients toward the person most senior in the room from habit and then makes herself stop doing it, because she has decided it reads as anxious',
        N'Corps working dress, well-maintained; she is at the stage of caring about her appearance as a signal of competence',
        N'none; being assessed for infusion consideration in the next cycle',
        N'Standard Corps junior officer duties. Morning drill and training with her unit. Midday briefings. Afternoon field assessment and patrol coordination. She has been spending additional time in the evenings reviewing the border engagement documentation, looking for the discrepancy between what happened and what she reported.',
        N'In the border skirmish for which she was commended, she saved a Fornax soldier''s life rather than capturing him as her orders specified. The soldier was wounded and she judged that capture would mean death in transit. She acted on instinct. Her official report states the soldier escaped during the fighting. The Fornax soldier is alive. She has not heard from him. She is aware that if anyone on the Fornax side describes the encounter, her report will not match.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Calyx Corps barracks and border patrol range; eastern plains',
        N'0', N'0',
        N'young woman in her mid-twenties, dark hair pulled back or loose, dark eyes, warm medium clear skin, lean quick build, well-maintained Corps dress, attentive quick posture, stone Corps barracks or border field terrain, dark fantasy junior military',
        N'young woman mid-twenties, dark hair, dark eyes, warm skin, lean build, Corps dress, attentive posture, stone barracks or field',
        0, 0
    );
    PRINT 'Eszter Varga seeded.';
END
ELSE PRINT 'Eszter Varga already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Nandor Takacs')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Nandor Takacs', N'nandor-takacs', N'canon', 1,
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
        @id, N'Nandor Takacs', N'nandor-takacs', N'Nandor', N'Takacs', N'',
        N'human', N'human', N'male', N'he/him', 61, N'alive',
        N'Transmutation Practitioner attached to the Calyx Corps; administers infusions before campaigns; publicly blamed for the catastrophic campaign twenty-five years ago; the blame is not entirely false and not entirely true',
        N'Nandor Takacs has been the Transmutation Practitioner for House Calyx for thirty years. He is the most experienced infusion administrator the House has, which means he is the person who has watched the most people survive and the most people die in the moments after the Catalyst enters their system. He performs this work with the specific steadiness of someone who cannot afford to show what it costs him. He prepares each infusion with more care than the protocol requires and more time than the schedule allows. He has been publicly blamed for the catastrophic campaign twenty-five years ago. He has never corrected the record.',
        N'The practitioner holding the secret at the center of the House''s worst history -- the one character who knows exactly who ordered what and has chosen, for reasons he believes are rational, never to say so.',
        N'No POV.',
        N'House Calyx Corps, Transmutation practice; attached to the Calyx Myrmidon; eastern plains',
        170, 68, N'careful; thin in the way of someone who forgets to eat when they are working, which is most of the time',
        N'white', N'loose; not managed; falls where it falls', N'medium',
        N'pale grey', N'pale', N'the pallor of a man who has spent thirty years in the specific proximity to Catalysts that leaves its mark without transforming',
        N'none; he has never taken the infusion',
        N'deliberate in the extreme; he moves as though the floor might not support him if he does not test it first; the caution of a man who has seen what happens when caution fails',
        N'Practitioner''s working dress; the specific garments that allow the procedure without contamination; he changes into them before any infusion and out of them after; it is the closest thing he has to a ritual',
        N'none',
        N'Morning preparation and review of the infusion schedules. Individual consultations with candidates before any procedure. The procedures themselves, which he never rushes. Afternoon recording in the official log. Evening in his private notation, which records what the official log does not.',
        N'He calibrated the infusions for the failed campaign twenty-five years ago to produce a higher casualty rate than standard practice would expect. He did this under orders from the then-Spymaster -- who is dead -- who was acting on instructions that Nandor never fully understood, to thin the cohort before a major campaign the Spymaster believed would fail anyway. Radovan blamed him publicly. He has never corrected this. The Spymaster is dead. The order is unverifiable. He has decided that explaining the truth would only distribute the shame without resolving anything -- and he is not certain this decision is right.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Calyx Corps Transmutation facility and infirmary; he rarely leaves the installation',
        N'0', N'0',
        N'old man in his early sixties, white loose hair, pale grey eyes, pale Catalyst-adjacent complexion, careful thin build, Practitioner working dress worn with ritual precision, deliberate testing movement, stone Transmutation facility, dark fantasy practitioner',
        N'old man sixties, white loose hair, pale grey eyes, pale face, thin build, Practitioner working dress, deliberate careful movement, stone Transmutation facility',
        0, 0
    );
    PRINT 'Nandor Takacs seeded.';
END
ELSE PRINT 'Nandor Takacs already exists.';
GO

-- ---------------------------------------------------------------------------
-- SCRYING INSTALLATION STAFF (5 characters)
-- ---------------------------------------------------------------------------

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Terez Koronczai')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Terez Koronczai', N'terez-koronczai', N'canon', 1,
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
        @id, N'Terez Koronczai', N'terez-koronczai', N'Terez', N'Koronczai', N'',
        N'human', N'human', N'female', N'she/her', 58, N'alive',
        N'Head Scrying Operator of House Calyx; thirty-five years at the apparatus; knows things that no one else in the House knows and has decided not to report most of them',
        N'Terez Koronczai has been at the Calyx Scrying apparatus for thirty-five years, which makes her the person who has looked into more parallel worlds for longer than any other living operator in House Calyx. She runs the observation schedule, trains new operators, files the official logs, and maintains the House''s relationship with the Liturgy''s transit coordination. She is meticulous in her logs in the specific way that someone is meticulous in their official records when their private records are more complete. She has a quality of absolute stillness at the apparatus that the newer operators have tried to emulate and have not managed.',
        N'The operator who has seen more than she has reported -- and who has been running a private surveillance of a specific Sphere 31 individual for twenty years, convinced of significance she has not named.',
        N'No POV.',
        N'Calyx Scrying installation; thirty-five years of service; eastern plains, Korvarat Hall adjacent installation',
        164, 65, N'still; the build of someone whose primary physical discipline has been sitting very quietly for thirty-five years',
        N'white with ash', N'pinned back plainly', N'medium when down',
        N'grey', N'pale olive', N'the specific indoor paleness of someone whose primary view has been through an apparatus rather than a window',
        N'none',
        N'absolutely still at the apparatus; outside it she moves with the slightly disoriented quality of someone emerging from deep water',
        N'operator working dress; practical and dark-colored to reduce visual distraction during observation sessions; she has been wearing variations of the same style for thirty years',
        N'none',
        N'Long-watch sessions at the apparatus -- she takes the longest watches, the ones no other operator has the endurance to maintain. Filing official logs after each session. Briefing the Liturgy Liaison on what the logs contain, which is not all of what she observed. The rest goes into her private notation, in a shorthand she developed herself.',
        N'For twenty years she has been observing a specific individual in Sphere 31 -- a man who appears in her apparatus sessions with a regularity that she has concluded cannot be coincidental. She has filed no reports about him. She has compiled a private log documenting everything she has seen across twenty years of sessions. She believes this person is significant to the Cauld in a way the Liturgy does not know. She has not decided whether to report this or to wait for the significance to become legible.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Calyx Scrying installation; she almost never leaves the installation grounds',
        N'0', N'0',
        N'woman in her late fifties, white and ash hair pinned plainly, grey eyes, pale olive indoor skin, still build, operator working dress in dark practical colors, absolute stillness at the apparatus, stone Scrying installation interior, dark fantasy observer',
        N'woman late fifties, white ash pinned hair, grey eyes, pale olive skin, dark practical dress, absolute stillness, stone Scrying installation interior',
        0, 0
    );
    PRINT 'Terez Koronczai seeded.';
END
ELSE PRINT 'Terez Koronczai already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Pal Banyai')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Pal Banyai', N'pal-banyai', N'canon', 1,
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
        @id, N'Pal Banyai', N'pal-banyai', N'Pal', N'Banyai', N'',
        N'human', N'human', N'male', N'he/him', 44, N'alive',
        N'Long-watch Scrying operator; fifteen years at the apparatus; methodical, devoutly religious; treats each observation session as a form of Bheur practice; believes he made contact with something in the membrane',
        N'Pal Banyai has been at the Calyx apparatus for fifteen years and treats each observation session with the preparation and attention of someone approaching a religious rite. He is methodical in his logs, consistent in his observations, and regarded by Terez as reliable in precisely the way that operators who are not brilliant are reliable -- he misses things that require interpretive leaps and he never misses the things that are actually there. He prays in the Bheur form every morning. This is not unusual at House Calyx, which is the most ritualistically observant of the Seven Houses.',
        N'The operator whose religious interpretation of what he observed in the membrane is a different kind of witness to the same phenomenon that Terez has been tracking empirically.',
        N'No POV.',
        N'Calyx Scrying installation; fifteen years of service; eastern plains',
        176, 78, N'medium; the build of someone who sits for long periods and moves deliberately when he moves',
        N'medium brown', N'plain and unmaintained; he cuts it himself', N'short',
        N'dark brown', N'medium', N'indoor pale; the same quality of apparatus-adjacent pallor as Terez, fifteen years in',
        N'none',
        N'deliberate and slightly reverent; he moves through the installation as though it is a place that requires a particular quality of attention',
        N'operator working dress; neat; he maintains his appearance with the same care he applies to the observation sessions',
        N'none',
        N'Morning Bheur devotional practice. Then the apparatus. Long-watch sessions, methodically recorded. He eats at the observation post during long watches and does not consider this a hardship. Evening prayer. He has been doing this, with minor variations, for fifteen years.',
        N'Eight months ago, during a long-watch session, he believes he made actual two-way contact -- brief, ambiguous, and impossible to fully describe -- with something in the membrane between worlds. Not a world on either side. The membrane itself. He has told no one. He prays about it in the Bheur form every morning, which is not unusual at House Calyx. What is unusual is what he is praying about. He is not certain whether what he experienced was genuine contact or the specific kind of fatigue-distortion that long watches produce. He is almost certain it was not distortion.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Calyx Scrying installation; eastern plains; he leaves the installation for Bheur services in the nearby village and not for much else',
        N'0', N'0',
        N'man in his mid-forties, medium brown plain hair, dark brown eyes, medium indoor-pale skin, medium build from long sedentary sessions, neat operator working dress, deliberate reverent movement, stone Scrying installation interior, dark fantasy religious observer',
        N'man mid-forties, medium brown hair, dark eyes, pale indoor skin, neat operator dress, deliberate posture, stone Scrying installation interior',
        0, 0
    );
    PRINT 'Pal Banyai seeded.';
END
ELSE PRINT 'Pal Banyai already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Berta Nemeth')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Berta Nemeth', N'berta-nemeth', N'canon', 1,
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
        @id, N'Berta Nemeth', N'berta-nemeth', N'Berta', N'Nemeth', N'',
        N'human', N'human', N'female', N'she/her', 38, N'alive',
        N'Long-watch Scrying operator; ten years at the apparatus; skeptical of mystical interpretations; running an unauthorized private log of Liturgy transit takings from Calyx territory',
        N'Berta Nemeth has been at the Calyx apparatus for ten years and has concluded that the most important thing a Scrying operator can do is record precisely what is observed rather than what the observer believes is significant. She is the counterweight to Pal''s religious interpretation and to Terez''s extended focus on a single subject -- Berta watches everything with the same quality of attention and records it in precise, non-interpretive language. Her logs are the best in the installation by technical standard. Terez relies on them. Berta considers this appropriate.',
        N'The empiricist in the installation whose private log of Liturgy transit operations may be the closest thing anyone has to a systematic record of what the Liturgy is actually doing with the takings.',
        N'No POV.',
        N'Calyx Scrying installation; ten years of service; eastern plains',
        165, 61, N'practical; the build of someone who has made no concessions to aesthetics that are not also concessions to function',
        N'auburn', N'pulled back flat for observation sessions; loose otherwise', N'medium',
        N'brown', N'pale', N'clear; indoor pale with the slight flush of someone who goes for a walk every evening as a deliberate counter to the sedentary work',
        N'none',
        N'direct and efficient; she moves toward what she needs and stops when she arrives; the posture of someone who considers excess movement a form of imprecision',
        N'operator working dress, practically maintained; she owns nothing that is not practical',
        N'none',
        N'Morning apparatus session. Logging. Midday consultation with Terez on the observation schedule. Afternoon apparatus session. Evening walk around the installation perimeter, which she considers a professional obligation to maintain her physical calibration. Then her private log.',
        N'For eight years she has been keeping a private, unauthorized log of every Sphere 31 person the Liturgy has taken from Calyx territory, recording dates, estimated ages, and inferred circumstances from what she observes through the apparatus. The log covers eight years of data. She has identified a pattern that suggests the takings are not incidental -- that specific profile criteria are being applied to the selections -- but she cannot yet determine whether the pattern is House-directed, Liturgy-directed, or both. She has not reported this to anyone.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Calyx Scrying installation; eastern plains; she leaves for her evening walk and returns; nothing else takes her from the installation',
        N'0', N'0',
        N'woman in her late thirties, auburn hair pulled back flat, brown eyes, pale practical skin, practical build, operator working dress practically maintained, direct efficient movement, stone Scrying installation interior, dark fantasy empiricist observer',
        N'woman late thirties, auburn hair pulled flat, brown eyes, pale skin, practical dress, direct efficient posture, stone Scrying installation interior',
        0, 0
    );
    PRINT 'Berta Nemeth seeded.';
END
ELSE PRINT 'Berta Nemeth already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Sandor Feher')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Sandor Feher', N'sandor-feher', N'canon', 1,
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
        @id, N'Sandor Feher', N'sandor-feher', N'Sandor', N'Feher', N'',
        N'human', N'human', N'male', N'he/him', 55, N'alive',
        N'Technical Maintenance Chief of the Calyx Scrying installation; keeps the apparatus running; does not observe through it; made an unauthorized modification eight years ago that he has been monitoring with increasing anxiety ever since',
        N'Sandor Feher does not Scry. He fixes the apparatus when it breaks, maintains the resonance coupling, replaces the structural components on their service schedule, and keeps the installation in the physical condition that allows the operators to do their work. He has been doing this for twenty years. He is good at it. He is the person the operators call at three in the morning when something stops working, and he is the person who arrives with the right tools already in his kit. He does not ask what they saw. He fixes what broke.',
        N'The maintenance chief whose unauthorized modification has been a slow-running catastrophe for eight years -- the installation''s structural integrity is on a countdown that only he knows about.',
        N'No POV.',
        N'Calyx Scrying installation, technical maintenance; twenty years of service; eastern plains',
        177, 85, N'capable; the build of someone whose work is physical and who has stayed in the condition the work requires',
        N'dark going grey', N'short, practical', N'short',
        N'pale blue', N'weathered', N'weathered; the outdoor weathering of someone who maintains an installation''s exterior components in all conditions',
        N'none',
        N'capable and practical; he moves toward whatever needs fixing; when nothing needs fixing he is still and slightly at a loss',
        N'maintenance working dress; heavy and practical; he wears it with the comfort of someone who has worn the same style for twenty years',
        N'none',
        N'Morning: inspection of the apparatus''s external components and the resonance housing. Midday: whatever maintenance the day''s sessions require. Afternoon: the structural inspection rotation that covers every load-bearing element of the installation on a twelve-week cycle. He checks one specific component every week that is not on the official rotation.',
        N'Eight years ago he modified the apparatus''s resonance coupling -- a recalibration he believed would improve signal clarity without disclosing it to Terez or the Liturgy''s installation oversight. It does improve signal clarity. It also places a persistent stress on a structural component of the resonance housing that he has been monitoring monthly since. He calculates the component has three to seven years of service life remaining under the current stress load. He does not know how to disclose the modification without explaining why he made it without authorization. He has been unable to find a way to correct it without making the original modification visible.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Calyx Scrying installation and its immediate grounds and exterior; he rarely leaves the installation',
        N'0', N'0',
        N'man in his mid-fifties, dark hair going grey, pale blue eyes, weathered outdoor skin, capable working build, heavy practical maintenance dress, capable purposeful movement, stone Scrying installation exterior and mechanical spaces, dark fantasy maintenance worker',
        N'man mid-fifties, dark greying hair, pale blue eyes, weathered face, heavy practical dress, capable purposeful movement, stone installation exterior and mechanical spaces',
        0, 0
    );
    PRINT 'Sandor Feher seeded.';
END
ELSE PRINT 'Sandor Feher already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Emese Takacs')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Emese Takacs', N'emese-takacs', N'canon', 1,
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
        @id, N'Emese Takacs', N'emese-takacs', N'Emese', N'Takacs', N'',
        N'human', N'human', N'female', N'she/her', 22, N'alive',
        N'Newly trained Scrying operator; six months at the apparatus; overwhelmed; has seen something she cannot explain and has told no one',
        N'Emese Takacs completed her operator training six months ago and has been at the apparatus since. She is the youngest operator the installation has had in twelve years. Terez has described her logs as technically adequate and her observation endurance as developing. Emese has described the work to no one outside the installation because she does not know how. She is not sure the work is what she was told it would be. She is also not sure that what she saw in her third supervised session is something that can happen, or something that only appears to happen, or something that she should have reported immediately and now cannot report without explaining why she did not.',
        N'The newest operator whose unexplained vision is a door into what the apparatus does to the people who look through it -- particularly when what they see is themselves.',
        N'No POV.',
        N'Calyx Scrying installation; newly assigned; eastern plains',
        162, 56, N'slight; young; not yet carrying the specific physical weight that the observation work accumulates over years',
        N'dark brown', N'loose; she has not adopted the operator''s pull-back style yet and has not been told to', N'medium',
        N'dark', N'warm medium', N'clear; young; she has not yet developed the indoor pallor of the long-term operators',
        N'none',
        N'watchful and slightly tense; she has not yet found the stillness the long-term operators have; she sits correctly at the apparatus and is otherwise in motion',
        N'operator working dress, new; she is the only operator whose dress has not been worn to softness yet',
        N'none',
        N'Observation sessions as scheduled -- she has not been assigned long watches yet. Logging under Terez''s supervision. Review of prior observation records to develop her pattern recognition. She goes home to the installation dormitory each evening and does not participate in the operators'' informal evening conversations, which she finds difficult to enter.',
        N'During her third supervised observation session, she saw her own face in the apparatus, reflected in what appeared to be a contemporaneous parallel world. The version of her in that world was dying -- not violently, but the way a person dies when they have decided to stop. She said nothing about this to Terez, who was in the room. She has not seen it again in subsequent sessions, which she finds almost worse than if it had recurred. She returns to the apparatus every watch and looks for it. She has not found it. She does not know what she will do if she does.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Calyx Scrying installation; she is new enough that her movement is effectively limited to the installation and immediate grounds',
        N'0', N'0',
        N'young woman in her early twenties, dark brown loose hair, dark eyes, warm medium clear skin, slight young build, new operator working dress not yet worn soft, watchful slightly tense posture, stone Scrying installation interior, dark fantasy new observer overwhelmed',
        N'young woman early twenties, dark loose hair, dark eyes, warm skin, new operator dress, watchful tense posture, stone Scrying installation interior',
        0, 0
    );
    PRINT 'Emese Takacs seeded.';
END
ELSE PRINT 'Emese Takacs already exists.';
GO

-- ---------------------------------------------------------------------------
-- DOMESTIC STAFF -- Part 1 (characters 31-41)
-- ---------------------------------------------------------------------------

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Jozsef Kovacsics')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Jozsef Kovacsics', N'jozsef-kovacsics', N'canon', 1,
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
        @id, N'Jozsef Kovacsics', N'jozsef-kovacsics', N'Jozsef', N'Kovacsics', N'',
        N'human', N'human', N'male', N'he/him', 67, N'alive',
        N'Seneschal of Korvarat Hall; has managed the entire household for thirty-eight years across two lordships; has been here longer than Lord Radovan and makes this gently apparent',
        N'Jozsef Kovacsics has been managing Korvarat Hall for thirty-eight years. He arrived under Radovan''s father and has outlasted the man who hired him by twenty years. He manages the household accounts, the staff rosters, the provisioning for both the feast and the field hospital, the maintenance schedule for the estate, and the running question of what it costs to keep a Great House functioning when the agricultural accounts are tighter than the Lord acknowledges. He does this with the specific authority of someone who has been doing it correctly for long enough that the question of whether he should be doing it no longer arises.',
        N'The household''s institutional authority and memory -- and the character whose unauthorized redistribution of funds to dead servants'' families is the most quietly moral act of fraud in the House.',
        N'No POV.',
        N'Korvarat Hall domestic staff; Seneschal for thirty-eight years; born in the eastern plains',
        173, 72, N'upright despite his age; the posture of someone who decided forty years ago what standing correctly meant and has not reconsidered',
        N'white', N'combed precisely; the same style for forty years', N'short',
        N'pale blue', N'parchment pale', N'lined; the lining of someone who has spent forty years being responsible for things that other people would prefer not to think about',
        N'none',
        N'upright and measured; he enters rooms before the people he is managing them for and leaves after them; this is not something he has thought about, it is simply what he does',
        N'Seneschal formal dress in the Calyx household colors; immaculate; he considers his appearance part of the household''s standard and maintains it accordingly',
        N'none',
        N'Before anyone else wakes: reviewing the day''s accounts and provisioning schedule. Morning: household staff briefing, then consultation with the Head Cook. Midday: the Lord''s administrative session, which Jozsef attends as the household''s representative. Afternoon: whatever the day has produced that requires his intervention, which is always something. He works until the house is settled for the night and then reviews tomorrow''s accounts.',
        N'For twenty years he has been routing small unauthorized payments from household discretionary accounts to the families of servants who died in House Calyx service. The amounts are small individually. The aggregate is not. He began because Lord Radovan''s father had promised these families support and then died before following through, and Jozsef had made himself the keeper of that promise. He has never calculated what would happen if the accounts were audited by someone who knew what to look for. He is sixty-seven and has decided this is no longer his problem to solve.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Korvarat Hall and the immediate estate; he does not leave the estate except for provisioning trips to the market town',
        N'0', N'0',
        N'old man in his late sixties, white precisely combed hair, pale blue eyes, parchment pale lined face, upright posture despite age, immaculate Seneschal formal dress in Calyx household colors, measured movement, stone manor great hall or account rooms, dark fantasy household authority',
        N'old man late sixties, white combed hair, pale blue eyes, lined pale face, upright posture, immaculate formal Seneschal dress, stone manor interior',
        0, 0
    );
    PRINT 'Jozsef Kovacsics seeded.';
END
ELSE PRINT 'Jozsef Kovacsics already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Agota Mraz')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Agota Mraz', N'agota-mraz', N'canon', 1,
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
        @id, N'Agota Mraz', N'agota-mraz', N'Agota', N'Mraz', N'',
        N'human', N'human', N'female', N'she/her', 63, N'alive',
        N'Head Cook of Korvarat Hall; has been at the kitchen for thirty-seven years; knew the previous Lord and does not think the current one is half as good; knows the preferences of every person in the house and occasionally extracts information from visiting diplomats by reading what they will and will not eat',
        N'Agota Mraz has run the Korvarat kitchen for thirty-seven years. She knew Lord Radovan''s father, cooked for his campaigns, and has a private opinion of the current Lord that she expresses only through the quality of what she sends to his table on the evenings when she has heard he has made a decision she disagrees with. Visiting diplomats eat at Korvarat and do not notice that the Head Cook has learned to read appetite as a form of intelligence -- what a man refuses to eat, what he asks for twice, what he pushes to the side of the plate tells Agota things about his state of mind that no official briefing contains.',
        N'The kitchen as intelligence operation -- the character whose access to the daily life of the House is more complete than anyone officially acknowledges, and whose one act of passing information may have saved lives or may have made things worse.',
        N'No POV.',
        N'Korvarat Hall kitchen; thirty-seven years of service; eastern plains born',
        158, 78, N'substantial; the build of a woman who has been standing at a kitchen station for thirty-seven years and whose physical presence in the kitchen is its own kind of authority',
        N'grey', N'pinned back for kitchen work', N'medium when down',
        N'warm brown', N'ruddy warm', N'ruddy from thirty-seven years of kitchen heat; clear; the complexion of someone whose primary environment is hot and honest about it',
        N'none',
        N'grounded and authoritative in the kitchen; slower and more watchful outside it; she does not move through the rest of the house as easily as she moves through her own territory',
        N'kitchen practical dress; apron; the same style for thirty years; she has made one concession to the formality of the house, which is a specific clean apron she wears when the Lord or Lady comes to the kitchen, which they do twice a year',
        N'none',
        N'Before dawn at the kitchen. The day''s provisioning organized before breakfast. Breakfast served. Midday preparations. The diplomatic meal preparations when visitors are present, during which she is more attentive than usual to what comes back to the kitchen. Evening: the kitchen settled, the stores accounted for, tomorrow''s provisioning list written.',
        N'Twelve years ago, during a border skirmish occupation, she passed information about House Calyx troop movements to a neutral Oathless courier. She believed the information would prevent a specific engagement that she knew from the kitchen -- from what the officers said over meals -- was going to kill people unnecessarily. She does not know if her information changed anything. She has never been found out. She has never entirely stopped watching for the opportunity to do it again, if the situation is the same.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Korvarat Hall kitchen and provisioning routes; she does not leave the estate except for the weekly market run, which she conducts herself',
        N'0', N'0',
        N'woman in her early sixties, grey hair pinned for kitchen work, warm brown eyes, ruddy warm complexion from years of kitchen heat, substantial build, kitchen practical dress and clean apron, grounded authoritative movement in her own kitchen, stone kitchen interior, dark fantasy cook',
        N'woman early sixties, grey hair pinned back, warm eyes, ruddy face, substantial build, kitchen dress and apron, authoritative movement, stone kitchen interior',
        0, 0
    );
    PRINT 'Agota Mraz seeded.';
END
ELSE PRINT 'Agota Mraz already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Tibor Peto')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Tibor Peto', N'tibor-peto', N'canon', 1,
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
        @id, N'Tibor Peto', N'tibor-peto', N'Tibor', N'Peto', N'',
        N'human', N'human', N'male', N'he/him', 35, N'alive',
        N'Sous-chef of Korvarat Hall; second in the kitchen under Agota; ambitious, capable, and conducting a subtle campaign against the Treasurer through his food',
        N'Tibor Peto is the best technical cook in the Korvarat kitchen and the one most likely to take Agota''s position when she retires, which Agota has not announced and Tibor has not asked about. He works well, produces results, and maintains the kitchen''s standards on the days Agota is managing provisioning rather than cooking. He has the specific ambition of someone who is good enough to be patient. He is not, in this one regard, actually patient.',
        N'The sous-chef whose petty revenge on the Treasurer is going to complicate the Treasurer''s ability to manage the accounts crisis -- because it''s worsening the anxiety that is already making Adorjan unreliable.',
        N'No POV.',
        N'Korvarat Hall kitchen; sous-chef; eastern plains born; twelve years at the house',
        174, 73, N'focused; the build of someone whose work is physical and precise and who keeps himself in the condition the work requires',
        N'dark', N'short, kitchen practical', N'short',
        N'dark', N'warm medium', N'warm; the kitchen heat has given him the same ruddy quality as Agota, though less pronounced',
        N'none',
        N'focused and direct in the kitchen; more social and watchful outside it; he is the kitchen''s ambassador to the rest of the household',
        N'kitchen practical dress; maintained well; he is more careful about his appearance than Agota and slightly resents that she isn''t',
        N'none',
        N'Before dawn in the kitchen with Agota. Morning prep and station management. He runs the kitchen on the days Agota is at the market. Afternoon prep. He stays until the kitchen is closed and the stores are secured. He reviews the supply request rejections every week.',
        N'For three years he has been introducing small quantities of a specific traditional Calyx herb -- not illegal, not poisonous, but known in eastern plains folk medicine to worsen anxiety in people who already carry it -- into the Treasurer Adorjan Nemes''s morning meals. Adorjan''s anxiety manifests as obsessive review of supply requests, and Tibor''s kitchen supply requests are the ones being repeatedly denied. He began as petty revenge. It has become a habit he is not sure he can stop without confronting what he has been doing.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Korvarat Hall kitchen and provisioning routes; he occasionally makes market runs with Agota',
        N'0', N'0',
        N'man in his mid-thirties, dark short kitchen-practical hair, dark eyes, warm medium skin with kitchen heat flush, focused build, kitchen practical dress, direct focused movement, stone kitchen interior, dark fantasy kitchen ambition',
        N'man mid-thirties, dark short hair, dark eyes, warm skin, kitchen dress, direct posture, stone kitchen interior',
        0, 0
    );
    PRINT 'Tibor Peto seeded.';
END
ELSE PRINT 'Tibor Peto already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Marta Brandt')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Marta Brandt', N'marta-brandt', N'canon', 1,
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
        @id, N'Marta Brandt', N'marta-brandt', N'Marta', N'Brandt', N'',
        N'human', N'human', N'female', N'she/her', 19, N'alive',
        N'Kitchen assistant; taken from Sphere 31 sixteen months ago and absorbed into household work; does not understand what happened to her; has an unexplained intuitive ability with mechanical systems that frightens her',
        N'Marta Brandt was taken sixteen months ago. She does not understand what the Cauld is, what the Liturgy wanted with her, or why she ended up in a kitchen rather than somewhere else. She works because the work is legible and because it keeps the questions from being the only thing in her head. She is quiet, competent, and observant in the specific way of someone who is trying to understand the rules of a situation without being able to ask what they are. She has a Sphere 31 name and a Sphere 31 surname that nobody at Korvarat has asked about, which she finds both a relief and an erasure.',
        N'The Sphere 31 person the House has absorbed without fully registering -- the character whose intuitive ability with mechanical systems is a door into what the Liturgy may actually have been selecting for.',
        N'No POV.',
        N'Kitchen staff; Sphere 31 origin, unregistered; sixteen months at Korvarat Hall',
        162, 55, N'small; not fully adjusted to the food available here yet; she was taken in the middle of a period of her life when she was not eating particularly well',
        N'light brown', N'loose; she does not know the Cauld styles and has not been told any', N'medium',
        N'hazel', N'medium fair', N'clear; the health of the kitchen has begun to show in her; she looks better than she did six months ago',
        N'none',
        N'careful and slightly minimized; she takes up less space than she needs to; the posture of someone who is trying to avoid notice as a survival strategy in a situation she does not yet understand',
        N'kitchen working dress provided by the household; it is not her style and she is aware of this in the way of someone trying not to think about it',
        N'none',
        N'Kitchen work from before dawn. She does whatever Agota or Tibor assigns. She is competent with food preparation and has picked up the kitchen''s procedures quickly. In the evenings she is largely alone in the dormitory. She has been trying to learn the language of the Cauld from context.',
        N'She can fix mechanical things. Kitchen equipment that breaks -- she touches it and understands intuitively what is wrong and how to correct it. She comes from a Sphere where she worked in a factory, which might explain the intuition, but she has told no one this because she does not know whether the ability is the reason she was taken. She is afraid that if it is, and if anyone here finds out, something will change about her situation in a way she cannot predict.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Korvarat Hall kitchen and dormitory; her movement is effectively bounded by the estate',
        N'0', N'0',
        N'young woman of nineteen, light brown loose hair, hazel eyes, medium fair skin beginning to recover health, small careful build, kitchen household dress not her own style, minimized careful posture, stone kitchen interior, dark fantasy Sphere 31 displaced person',
        N'young woman nineteen, light brown loose hair, hazel eyes, medium fair skin, small build, kitchen dress, careful minimized posture, stone kitchen interior',
        0, 0
    );
    PRINT 'Marta Brandt seeded.';
END
ELSE PRINT 'Marta Brandt already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Lorincz Hajdu')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Lorincz Hajdu', N'lorincz-hajdu', N'canon', 1,
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
        @id, N'Lorincz Hajdu', N'lorincz-hajdu', N'Lorincz', N'Hajdu', N'',
        N'human', N'human', N'male', N'he/him', 69, N'alive',
        N'Butler of Korvarat Hall; manages the serving staff and all formal occasions; has served three generations of the family; carries the House''s dignity in his posture alone; witnessed something at a formal occasion twenty-two years ago that he has never spoken of',
        N'Lorincz Hajdu has been the Butler at Korvarat Hall for forty-three years. He manages the serving staff, the formal occasions, the protocol of who stands where and what order the dishes are presented in and which dignitary''s glass is refilled first. He does this with the unobtrusive perfection of someone who has decided that the House''s public face is his professional responsibility and has not reconsidered this in four decades. He is the most formally dressed person in the household at all times. His posture alone carries the House''s dignity at occasions when the Lord''s does not.',
        N'The butler whose management of a covered-up death twenty-two years ago is the thing he is most defined by -- a choice made in the House''s interest that was also made in his own, and that he has never been able to separate.',
        N'No POV.',
        N'Korvarat Hall; Butler for forty-three years; three generations of household service',
        177, 74, N'upright; the specific uprightness of someone who has been standing correctly for forty years and whose body has incorporated it',
        N'white', N'impeccably maintained', N'short',
        N'grey', N'papery pale', N'papery; the pallor of a man whose primary light has been candles and fires for forty years',
        N'none',
        N'the most precisely controlled movement in the household; he does not make unnecessary gestures; he appears in exactly the place he is needed and not elsewhere',
        N'Butler formal dress; immaculate; he considers his presentation indistinguishable from the House''s presentation and maintains it accordingly',
        N'none',
        N'Present before any formal occasion begins. Managing the serving staff through the day''s requirements. Present at every significant formal event. He knows the serving staff individually and manages them with the even-handedness of someone who has learned that the household''s smooth functioning depends on the staff trusting the person managing them. He is the last person to leave any formal occasion. He is meticulous about sight lines.',
        N'Twenty-two years ago he witnessed the previous Lady of the House -- a woman who died fifteen years ago -- cause the death of a visiting diplomat at a formal occasion through what appeared to be an accidental fall but was not. He managed the aftermath without being instructed to. He arranged for the body to be removed quietly and the account of the evening to be consistent. He has never spoken of it. The incident has shaped every formal occasion he has managed since -- he is meticulous about sight lines because he knows what happens when someone uses the blind ones.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Korvarat Hall and the formal estate spaces; he does not leave the estate',
        N'0', N'0',
        N'old man in his late sixties, white impeccably maintained hair, grey eyes, papery pale face, impeccably upright posture, immaculate Butler formal dress, precisely controlled movement, stone manor formal hall or reception rooms, dark fantasy household authority',
        N'old man late sixties, white maintained hair, grey eyes, papery face, immaculate formal dress, perfectly upright posture, stone manor formal hall',
        0, 0
    );
    PRINT 'Lorincz Hajdu seeded.';
END
ELSE PRINT 'Lorincz Hajdu already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Erzso Balogh')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Erzso Balogh', N'erzso-balogh', N'canon', 1,
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
        @id, N'Erzso Balogh', N'erzso-balogh', N'Erzso', N'Balogh', N'',
        N'human', N'human', N'female', N'she/her', 55, N'alive',
        N'Head Housekeeper of Korvarat Hall; manages all household cleaning, laundry, and linen; practical, no-nonsense, the second most powerful person in the domestic staff; preparing in secret for the day the House falls',
        N'Erzso Balogh runs the household''s physical maintenance with the efficiency of someone who has decided that a House that cannot maintain its own linen does not deserve to maintain its territory. She manages twelve staff directly, the linen inventory, the cleaning rotation, and the relationship with the Laundry Master that is the practical center of the household''s textile operation. She is not warm, but she is fair, and her staff knows the difference. The Seneschal trusts her completely and defers to her on domestic matters without reservation. She is the most competent person in the downstairs by any practical measure.',
        N'The housekeeper whose emergency liquidation assessment is the most practical response anyone in the House has made to the approaching crisis -- and the one nobody in authority knows exists.',
        N'No POV.',
        N'Korvarat Hall household staff; Head Housekeeper for twenty years; eastern plains born',
        164, 70, N'practical; the build of someone whose work is physical and who has stayed in the condition the work requires for twenty years',
        N'iron grey', N'pulled back severely for work', N'short when down',
        N'dark grey', N'weathered medium', N'clear; the indoor weathering of someone whose primary work is physical and whose primary environment is stone and cold water',
        N'none',
        N'direct and purposeful; she does not circle toward her destination; she moves through the house as though the shortest path is the only path',
        N'housekeeper practical dress; clean to a standard that is itself a form of authority; she does not own anything that is not practical',
        N'none',
        N'Before dawn inspecting the previous night''s linen. Morning staff assignments. Managing the cleaning rotation through the day. Consultation with the Seneschal on household accounts. Evening: reviewing tomorrow''s schedule and updating the private ledger she keeps in the linen store.',
        N'For seven years she has been maintaining a running private valuation of every item in Korvarat Hall that could be liquidated in an emergency evacuation -- the silver service, the tapestries, the horses, the stores. She updates it quarterly. She was eight years old during the last occupation and spent three weeks in the root cellar with four other children while the House was held. She has promised herself she will never be that unprepared again. She has not told anyone about the valuation.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Korvarat Hall and immediate estate; she does not leave the estate',
        N'0', N'0',
        N'woman in her mid-fifties, iron grey hair pulled back severely, dark grey eyes, weathered medium skin, practical build, housekeeper practical dress clean with authority, direct purposeful movement, stone manor linen rooms or corridors, dark fantasy household authority',
        N'woman mid-fifties, iron grey pulled-back hair, dark eyes, weathered skin, practical dress, direct posture, stone manor corridors or linen rooms',
        0, 0
    );
    PRINT 'Erzso Balogh seeded.';
END
ELSE PRINT 'Erzso Balogh already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Kata Horvath')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Kata Horvath', N'kata-horvath', N'canon', 1,
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
        @id, N'Kata Horvath', N'kata-horvath', N'Kata', N'Horvath', N'',
        N'human', N'human', N'female', N'she/her', 34, N'alive',
        N'Household maid; has been at Korvarat since childhood, starting at age ten; knows every secret of the house by proximity; has been transcribing overheard conversations in a private cipher for sixteen years',
        N'Kata Horvath has been at Korvarat Hall since she was ten years old. She arrived as a kitchen helper and moved to the household staff at fourteen. She has been in the house for twenty-four years. She knows which floorboards creak, which doors stick, which corridors carry sound from which rooms. She knows these things not because she has investigated them but because she has been here long enough that the house has become something she simply knows, the way you know the layout of the place you have lived in for your entire adult life.',
        N'The maid who has been everywhere for long enough -- her cipher archive is sixteen years of the House''s actual history, written in a notation that only she can read.',
        N'No POV.',
        N'Korvarat Hall household staff; has been at the house for twenty-four years; eastern plains born',
        165, 60, N'quiet; the specific physical quality of someone who has learned to occupy space without drawing attention to themselves over twenty-four years of practice',
        N'dark', N'pulled back for work; loose on her day of rest', N'medium',
        N'dark brown', N'warm medium', N'clear; the indoor health of someone who has spent most of their life in a well-maintained house',
        N'none',
        N'smooth and largely invisible; she moves through the house without friction; people in the house do not notice her movement as a category of thing worth noticing',
        N'household maid dress; clean and maintained; she has the same dress in three versions and rotates them',
        N'none',
        N'Household duties through the day -- room preparation, cleaning, serving at meals, whatever the Head Housekeeper assigns. She has developed a reputation for not being where she is not supposed to be, which is not entirely accurate. In the evenings she writes.',
        N'Since she was eighteen she has been transcribing overheard conversations in a private cipher she developed from traditional Calyx embroidery pattern notation. She has sixteen years of records covering every significant thing she has heard in this house. She has no particular plan for the records. She started because the people in the house were interesting in a way she could not discuss with them, and she has continued because stopping would mean deciding that none of it mattered. The archive is hidden in the embroidery frame she keeps in her room.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Korvarat Hall; she has not left the estate in four years and does not feel the absence of this',
        N'0', N'0',
        N'woman in her mid-thirties, dark hair pulled back for work, dark brown eyes, warm medium skin, quiet practical build, clean household maid dress, smooth nearly-invisible movement, stone manor corridors or rooms, dark fantasy domestic servant observer',
        N'woman mid-thirties, dark pulled-back hair, dark eyes, warm skin, household maid dress, smooth quiet movement, stone manor interior',
        0, 0
    );
    PRINT 'Kata Horvath seeded.';
END
ELSE PRINT 'Kata Horvath already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Fruzsina Toth')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Fruzsina Toth', N'fruzsina-toth', N'canon', 1,
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
        @id, N'Fruzsina Toth', N'fruzsina-toth', N'Fruzsina', N'Toth', N'',
        N'human', N'human', N'female', N'she/her', 22, N'alive',
        N'Household maid; arrived eight months ago; placed by the Spymaster from the same village as her family; reports gossip in exchange for farm protection she can barely justify to herself',
        N'Fruzsina Toth came to Korvarat eight months ago from the agricultural villages east of the estate. She is competent, quiet, and watches everything with the anxious attentiveness of someone who is not sure whether she is doing the thing she is supposed to be doing well enough. She is pleasant with the other staff. She gets along with Kata, who has been patient with her. She sends letters home to her family every two weeks. The letters contain, along with family news, whatever she has heard in the house that week that seems potentially significant to a person she has never met in person.',
        N'The planted maid -- the character whose arrangement with the Spymaster is the most direct example of how intelligence operations reach into the household''s intimate spaces.',
        N'No POV.',
        N'Korvarat Hall household staff; eight months; placed from the eastern agricultural villages',
        162, 57, N'watchful; the build of someone from the agricultural villages who is noticeably not used to the house''s physical environment yet',
        N'light brown', N'loose; she has not adopted the house style yet', N'medium',
        N'green-hazel', N'pale warm', N'the outdoor pale of someone who spent her life in agricultural light and has moved indoors',
        N'none',
        N'watchful and slightly over-attentive; she has not yet learned to make her attentiveness invisible; she listens in a way that is slightly more obvious than she realizes',
        N'household maid dress provided by the house; it does not quite fit; she has not asked for alterations',
        N'none',
        N'Household duties as assigned by the Head Housekeeper. She is more thorough than she needs to be, which has been noted approvingly. In the evenings she writes her letters home, which take longer than the family content requires.',
        N'She was placed in the house eight months ago by Spymaster Csenge Horvath, who grew up in the same village as Fruzsina''s mother. In exchange for regular reports of household gossip -- no specific targets, no specific subjects, whatever she happens to hear -- her family''s farm has been receiving favorable treatment in the requisition assessments. Fruzsina understands the arrangement imperfectly and follows it with the specific anxiety of someone who knows they are being used but cannot see the full shape of the use or the full extent of the risk.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Korvarat Hall; eight months; she has returned to her village once since arriving',
        N'0', N'0',
        N'young woman of twenty-two, light brown loose hair, green-hazel eyes, pale warm outdoor skin, watchful build, household dress that does not quite fit, over-attentive slightly obvious listening posture, stone manor interior, dark fantasy domestic intelligence',
        N'young woman twenty-two, light brown loose hair, green-hazel eyes, pale outdoor skin, ill-fitting maid dress, watchful posture, stone manor interior',
        0, 0
    );
    PRINT 'Fruzsina Toth seeded.';
END
ELSE PRINT 'Fruzsina Toth already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Zsuzsa Molnar')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Zsuzsa Molnar', N'zsuzsa-molnar', N'canon', 1,
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
        @id, N'Zsuzsa Molnar', N'zsuzsa-molnar', N'Zsuzsa', N'Molnar', N'',
        N'human', N'human', N'female', N'she/her', 28, N'alive',
        N'Household maid; six years at Korvarat; listens too carefully; has been cataloguing the personal entanglements of significant House members without knowing exactly why',
        N'Zsuzsa Molnar has been at Korvarat Hall for six years and has the quiet observant quality of someone who is more comfortable knowing what is going on around her than not knowing. She is efficient, unobtrusive, and has developed a reputation for being reliable in the specific way that people who notice everything and say nothing about it are reliable -- they do not generate incidents, and they are not the source of any gossip that anyone can trace. This is not an accident.',
        N'The maid whose catalogue of entanglements is a passive intelligence asset that nobody authorized -- the one character accumulating leverage without a plan for it, which is the most dangerous kind.',
        N'No POV.',
        N'Korvarat Hall household staff; six years; eastern plains',
        166, 62, N'observant; the build of someone who has spent six years learning to be useful without being noticed',
        N'dark', N'pulled back for work; she has adopted the house style', N'medium',
        N'brown', N'medium', N'clear; the even-toned indoor health of six years in a well-maintained household',
        N'none',
        N'quiet and contained; she has learned to make her attentiveness invisible in a way Fruzsina has not yet managed; she occupies exactly the space her role requires',
        N'household maid dress; maintained and fitted correctly; six years has given her time to get this right',
        N'none',
        N'Household duties through the day. She has taken on some supervisory responsibility for the newer staff, which the Head Housekeeper has authorized informally. She is reliable enough to be given tasks without close oversight. In the evenings she makes notes that she describes to herself as personal observations.',
        N'For six years she has been cataloguing the romantic and personal entanglements of significant people in the house. Not for any specific purpose. She has told herself this is simply the way she processes what she observes, that the information is not leverage unless she decides to use it. She has not decided. She is waiting -- she understands now, if not when she started -- for a moment of sufficient injustice that would make the decision feel like justice rather than calculation.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Korvarat Hall; she leaves the estate for the village market on her day of rest',
        N'0', N'0',
        N'woman in her late twenties, dark pulled-back hair, brown eyes, medium even-toned skin, quiet observant build, well-fitted household maid dress, contained quiet movement, stone manor interior, dark fantasy domestic observer',
        N'woman late twenties, dark pulled-back hair, brown eyes, medium skin, well-fitted maid dress, quiet contained movement, stone manor interior',
        0, 0
    );
    PRINT 'Zsuzsa Molnar seeded.';
END
ELSE PRINT 'Zsuzsa Molnar already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Reka Szabo')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Reka Szabo', N'reka-szabo', N'canon', 1,
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
        @id, N'Reka Szabo', N'reka-szabo', N'Reka', N'Szabo', N'',
        N'human', N'human', N'female', N'she/her', 31, N'alive',
        N'Lady''s personal attendant; attends Lady Zsofia; has been reading the Lady''s private correspondence for four years and has chosen not to act on any of it because she genuinely likes her',
        N'Reka Szabo has attended Lady Zsofia for four years. She manages the Lady''s schedule, her personal correspondence that does not go through the Chancellor, her wardrobe, and the private rhythm of her day. She is impeccably discreet in the sense that she says nothing about what she knows and is very good at giving the impression that she does not know very much. She took the position expecting to be indifferent to the person she was attending. She was not prepared for the Lady to be the kind of person she is.',
        N'The attendant who knows the most and has done the least with it -- whose genuine affection for Lady Zsofia is the most human obstruction to the intelligence chain the Spymaster doesn''t know is broken.',
        N'No POV.',
        N'Korvarat Hall; Lady''s attendant for four years; eastern plains minor family',
        167, 61, N'careful; the specific physical bearing of someone who has spent four years attending a person of authority and absorbed some of the posture',
        N'auburn', N'neat and arranged; she has adopted some of Lady Zsofia''s style', N'medium',
        N'amber', N'warm medium', N'clear; well-maintained; she takes her appearance seriously as a reflection of the position',
        N'none',
        N'poised and attentive; she mirrors some of Lady Zsofia''s composure without being aware this has happened',
        N'attendant''s dress in the Lady''s household colors; well-made; she has been given two new dresses per year and cares for them accordingly',
        N'none',
        N'With Lady Zsofia through the day -- morning preparation, accompanying her to meetings when the Lady wishes, managing the afternoon correspondence, the evening preparation. Her schedule is the Lady''s schedule. She has almost no time that is entirely her own.',
        N'She has been reading Lady Zsofia''s private correspondence for four years. She knows about the Atrament channel. She knows things that would destroy careers. She has never acted on any of it because she genuinely likes Lady Zsofia -- the Lady is not what Reka expected when she took the position, and Reka has found herself in the uncomfortable situation of protecting someone she was prepared to be professionally indifferent to. She is not sure what she will do if the thing she knows about the Atrament channel is ever asked of her directly.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Korvarat Hall; she travels with Lady Zsofia when the Lady travels',
        N'0', N'0',
        N'woman in her early thirties, auburn neat arranged hair, amber eyes, warm medium clear skin, poised careful build, attendant''s dress in Lady''s household colors, poised attentive movement, stone manor private chambers or formal rooms, dark fantasy personal attendant',
        N'woman early thirties, auburn arranged hair, amber eyes, warm skin, attendant dress in household colors, poised attentive posture, stone manor private chambers',
        0, 0
    );
    PRINT 'Reka Szabo seeded.';
END
ELSE PRINT 'Reka Szabo already exists.';
GO

-- ---------------------------------------------------------------------------
-- DOMESTIC STAFF -- Part 2 (characters 41-51) + OATHLESS ADJACENT (52-53)
-- ---------------------------------------------------------------------------

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Miklos Feher')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Miklos Feher', N'miklos-feher', N'canon', 1,
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
        @id, N'Miklos Feher', N'miklos-feher', N'Miklos', N'Feher', N'',
        N'human', N'human', N'male', N'he/him', 56, N'alive',
        N'Stable Master of Korvarat Hall; manages all horses and transportation; knows exactly when someone leaves at night and who they go to; providing a monthly discreet departure for someone he has protected for six years',
        N'Miklos Feher has run the Korvarat stables for twenty-two years. He knows every horse by name and temperament, knows the condition of every piece of tack, and knows when every horse returns and from where by the state of its hooves and coat. He is excellent at his work in the way that people who have done the same job for twenty years and still find it genuinely interesting are excellent. He manages the grooms with the specific patience of someone who understands that the horses are more consistent than the people and calibrates his expectations accordingly.',
        N'The stable master who knows the most about who leaves and when -- and who has been running a protected departure route for six years without asking enough questions to know whether he should stop.',
        N'No POV.',
        N'Korvarat Hall stables and transportation; Stable Master for twenty-two years',
        180, 88, N'outdoor-weathered; the solid build of someone who has spent twenty-two years doing physical work outside in all weather',
        N'weathered grey', N'short, unmaintained by design', N'short',
        N'pale blue', N'deeply weathered', N'deeply weathered; the outdoor complexion of someone for whom the concept of indoor skin does not fully apply',
        N'none',
        N'grounded and easy; the movement of someone comfortable in a physical environment who has no reason to be uncomfortable about it',
        N'stable working dress; practical; worn to a patina that suggests twenty years of use; he considers this appropriate',
        N'none',
        N'Before dawn: morning feeding and assessment of every horse. The day''s transportation scheduling. Managing the grooms through the work. Evening settling of the horses. He is the last person in the stables each night. He knows what has left and what has returned.',
        N'For six years he has been providing a horse, a tack configuration that leaves no distinctive marks, and a quiet departure route to the same person from the household, once a month. He does not know the full reason for the departures. He knows the person is frightened and the departures are necessary. He has not asked more because he has decided that what he does not ask about, he does not need to answer for. He has not reported it. He has told himself this is a form of loyalty. He is not always sure to what.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Korvarat Hall stables and estate grounds; delivery and transportation routes throughout Calyx territory',
        N'0', N'0',
        N'man in his mid-fifties, weathered grey hair short, pale blue eyes, deeply weathered outdoor face, solid outdoor build, worn stable working dress, grounded easy movement, stone stable interior or estate grounds, dark fantasy stable master',
        N'man mid-fifties, grey hair, pale blue eyes, weathered face, solid build, worn stable dress, grounded movement, stone stable interior',
        0, 0
    );
    PRINT 'Miklos Feher seeded.';
END
ELSE PRINT 'Miklos Feher already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Balazs Csikos')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Balazs Csikos', N'balazs-csikos', N'canon', 1,
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
        @id, N'Balazs Csikos', N'balazs-csikos', N'Balazs', N'Csikos', N'',
        N'human', N'human', N'male', N'he/him', 27, N'alive',
        N'Groom at Korvarat stables; good with horses; quiet with people; has discovered that one of the horses has a habitual route that leads to a sealed building in the outer estate',
        N'Balazs Csikos has been a groom at Korvarat for four years. He is good at the work in the simple way of someone whose primary interest has always been horses and who does not find this embarrassing. He and Miklos have a comfortable working relationship built on the shared understanding that the horses are the primary concern and everything else is secondary. He does not talk much. He observes carefully. He has noticed something about one of the horses that he has not yet decided what to do with.',
        N'The groom who found the anomaly -- his discovery about the horse''s trained route is going to intersect with whatever Gyorgy Nador found in the sealed eastern chamber fifteen years ago.',
        N'No POV.',
        N'Korvarat Hall stables; groom for four years; eastern plains born',
        177, 78, N'easy; the build of someone whose work is physical and whose relationship with his body is uncomplicated',
        N'light brown', N'short, unmaintained', N'short',
        N'light brown', N'warm medium', N'the outdoor health of someone whose work is outside',
        N'none',
        N'easy and unhurried around horses; slightly more careful around people; he defaults to horses when both are available',
        N'groom working dress; practical; worn in the way that good-quality working clothes become after four years of daily use',
        N'none',
        N'Stable work with Miklos and the other groom from before dawn. The full routine of a working stable. Afternoon exercise of the horses that need it. He talks to the horses more than he talks to the other staff.',
        N'One of the horses -- a grey gelding called Korom -- has been trained to return to a specific location in the eastern estate without being directed there, as though it has a habitual route that predates its acquisition by the stable. Balazs has followed the route. It ends at a sealed stone building in the outer estate grounds. He has not gone inside. He has not told Miklos. He is watching to see whether anyone uses the building -- and whether anyone uses Korom specifically -- before he decides what to do with what he has noticed.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Korvarat Hall stables and estate grounds; exercise routes through the eastern estates',
        N'0', N'0',
        N'young man in his late twenties, light brown short hair, light brown eyes, warm medium outdoor skin, easy physical build, groom working dress worn soft with use, easy movement around horses careful movement around people, stone stable interior or estate grounds, dark fantasy groom',
        N'young man late twenties, light brown hair, light eyes, warm outdoor skin, easy build, worn groom dress, easy posture around horses, stone stable or estate grounds',
        0, 0
    );
    PRINT 'Balazs Csikos seeded.';
END
ELSE PRINT 'Balazs Csikos already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ilka Varga')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ilka Varga', N'ilka-varga', N'canon', 1,
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
        @id, N'Ilka Varga', N'ilka-varga', N'Ilka', N'Varga', N'',
        N'human', N'human', N'female', N'she/her', 23, N'alive',
        N'Groom at Korvarat stables; taken from Sphere 31 fourteen months ago; placed in stables because she worked with horses in her origin world; has been mentally mapping the local geography trying to understand where she is',
        N'Ilka Varga was taken fourteen months ago. She worked with horses in her origin Sphere and the Liturgy''s placement system put her in stables, which is the most legible thing that has happened to her since the taking. The horses she understands. The people are harder. The Cauld is harder. She has not told anyone much about where she came from because she does not know how to begin explaining it, and because she has been watching what happened to Marta in the kitchen and has decided that being legible is not necessarily safer than not being legible.',
        N'The Sphere 31 person in the stables -- her geography-mapping project is going to produce a record of the estate''s outer territory that nobody in authority knows exists.',
        N'No POV.',
        N'Korvarat Hall stables; groom for fourteen months; Sphere 31 origin, unregistered',
        163, 59, N'watchful; the build of someone from a different world who has been working physically for fourteen months and is beginning to look like someone who belongs here, which she is not sure is what she wants',
        N'blonde', N'practical; she learned the groom''s style quickly because it was the first thing someone told her how to do here', N'short',
        N'blue', N'fair', N'fair; beginning to show the outdoor health of the stable work',
        N'none',
        N'watchful and slightly oriented toward exits; the posture of someone who has been tracking possible routes for fourteen months',
        N'groom working dress provided by the stable; it fits; she has been here long enough for this to have been sorted out',
        N'none',
        N'Stable work as assigned. She is good with horses in the way that people from her origin Sphere are good with horses, which is similar enough to be usable and different enough that Miklos has noticed a few things she does differently and found them effective. In the evenings she sits in a specific spot in the hayloft and writes in a notation she developed herself.',
        N'She has been mentally mapping the local geography for fourteen months, based on the routes horses return from, the direction of the sun, the character of the terrain she can see from the estate perimeter, and whatever she can piece together from conversations she partially understands. She is trying to determine whether there is a direction that might lead back to anything she recognizes. She has not found anything yet. She writes the map in a notation that does not look like a map. She does not know what she will do with the map if she finishes it.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Korvarat Hall stables and exercise routes; the exercise routes are the most useful intelligence she has',
        N'0', N'0',
        N'young woman in her early twenties, blonde short practical hair, blue eyes, fair outdoor skin beginning to show stable work health, watchful build, groom working dress, watchful exit-oriented posture, stone stable interior or estate grounds, dark fantasy Sphere 31 displaced person',
        N'young woman early twenties, blonde practical hair, blue eyes, fair skin, groom working dress, watchful posture, stone stable interior or estate grounds',
        0, 0
    );
    PRINT 'Ilka Varga seeded.';
END
ELSE PRINT 'Ilka Varga already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Gyorgy Nador')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Gyorgy Nador', N'gyorgy-nador', N'canon', 1,
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
        @id, N'Gyorgy Nador', N'gyorgy-nador', N'Gyorgy', N'Nador', N'',
        N'human', N'human', N'male', N'he/him', 71, N'alive',
        N'Groundskeeper of Korvarat Hall; manages the estate grounds and outer defenses; has been here for forty years; discovered a tunnel and sealed chamber in the eastern garden fifteen years ago and has never reported it',
        N'Gyorgy Nador has been the Korvarat groundskeeper for forty years. He knows every tree, every drainage channel, every section of outer wall. He manages the estate grounds with the authority of someone for whom the estate has become an extension of his own body -- he knows when something is wrong before he can see it because he knows what right feels like. He is the oldest member of the domestic staff. He works more slowly than he did at forty but more completely, because he does not miss things he has been seeing for four decades.',
        N'The groundskeeper who found what is in the eastern estate -- and whose sealed chamber discovery is going to connect to whatever Balazs Csikos has found about the horse''s route.',
        N'No POV.',
        N'Korvarat Hall grounds and outer defenses; Groundskeeper for forty years; eastern plains born',
        174, 76, N'ancient-solid; the specific build of a man who has done outdoor physical work for forty years and whose body has organized itself around this',
        N'white', N'unmaintained; it goes where it goes', N'medium',
        N'faded blue', N'deeply weathered', N'deeply weathered; the complexion of forty years outdoors in the eastern plains; he is the color of the estate''s exterior walls',
        N'none',
        N'slow and completely certain; he moves through the grounds as though he owns them more surely than the Lord does, because he has been maintaining them for forty years and the Lord has not',
        N'groundskeeper working dress; very old; very well-maintained; the paradox of someone who keeps everything in perfect condition including equipment that is well past replacement',
        N'none',
        N'Before dawn walking the grounds. The day''s maintenance assessment and work. Management of the drainage channels and outer wall condition. He has two younger assistants who do the heavier work he no longer does. He directs them with the precision of someone who knows exactly what needs to be done and does not need to demonstrate it. He visits the eastern garden three or four times a year.',
        N'Fifteen years ago he discovered a tunnel beneath the eastern garden -- stone-lined, well-made, with the specific quality of something that was built to last and sealed to prevent casual entry. The tunnel leads to a chamber containing equipment he cannot identify and has never been able to identify since. He has never reported it. He visits it three or four times a year. Nothing in the chamber has changed in fifteen years. He has told himself this means it is inactive and harmless. He is becoming less able to sustain this conclusion.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Korvarat Hall grounds and outer estate; he knows this territory better than anyone else alive',
        N'0', N'0',
        N'very old man in his early seventies, white unmaintained hair, faded blue eyes, deeply weathered complexion the color of the estate walls, ancient-solid outdoor build, very old but perfectly maintained groundskeeper dress, slow certain movement, stone estate grounds and gardens, dark fantasy old servant',
        N'very old man seventies, white hair, faded blue eyes, deeply weathered face, solid build, old well-maintained groundskeeper dress, slow certain movement, estate grounds and gardens',
        0, 0
    );
    PRINT 'Gyorgy Nador seeded.';
END
ELSE PRINT 'Gyorgy Nador already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Klara Ferenczi')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Klara Ferenczi', N'klara-ferenczi', N'canon', 1,
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
        @id, N'Klara Ferenczi', N'klara-ferenczi', N'Klara', N'Ferenczi', N'',
        N'human', N'human', N'female', N'she/her', 48, N'alive',
        N'House Physician of Korvarat Hall; treats the family and high-ranking staff; has access to everyone through their ailments; has been tracking a pattern of stress illness that points at the Chancellor''s office',
        N'Klara Ferenczi has been the Korvarat physician for fourteen years. She treats the family, the senior staff, and the cabinet members when they present, which they all do eventually because everyone has ailments and the alternative to Klara is sending for someone from the market town. She is good at her work in the quiet undemonstrative way of someone who learned early that people are more candid with a physician than with almost anyone else, and who has used this access with the discretion it requires.',
        N'The physician whose pattern-recognition on stress-related illness is going to map the hidden crisis in the cabinet before the cabinet knows it is visible -- a clinical diagnosis of an institutional problem.',
        N'No POV.',
        N'Korvarat Hall; House Physician for fourteen years; trained in the market town and one of the border academies',
        166, 64, N'clinical; the build of someone who keeps herself in functional condition as a professional discipline',
        N'dark streaked with grey', N'pulled back; she manages it with the same care she applies to clinical hygiene', N'medium',
        N'grey', N'pale olive', N'clear; the physician''s specific clarity of someone who monitors her own health as rigorously as her patients''',
        N'none',
        N'precise and observant; she enters rooms noticing what she notices and does not announce it; she is among the quietest movers in the household',
        N'physician practical dress; clean to a clinical standard; she changes between patient visits',
        N'none',
        N'Morning rounds of anyone she is treating. Responding to summons through the day. Maintaining the medical store and reviewing what has been used and what needs restocking. Afternoons she often has time that is nominally her own, during which she writes -- not patient records, which are kept separately, but the analysis that does not belong in official records.',
        N'For two years she has been tracking a pattern of stress-related ailments in the household -- disrupted sleep, chronic tension, appetite disruption -- and mapping them against the social dynamics of the house. The pattern suggests something generating hidden pressure in the administrative structure. Her current hypothesis, developed across two years of informal observation, points at the Chancellor''s office as the source. She does not yet know what the mechanism is. She has not reported her hypothesis. She is a physician, not an investigator, and she is not sure this distinction protects her.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Korvarat Hall and the immediate estate; she travels to the market town for supplies quarterly',
        N'0', N'0',
        N'woman in her late forties, dark hair streaked grey pulled back clinically, grey eyes, pale olive clear skin, clinical precise build, physician practical dress clinical-clean, precise quiet observant movement, stone manor medical room or corridors, dark fantasy physician',
        N'woman late forties, dark grey streaked hair pulled back, grey eyes, pale olive skin, physician practical dress, precise quiet movement, stone manor medical room',
        0, 0
    );
    PRINT 'Klara Ferenczi seeded.';
END
ELSE PRINT 'Klara Ferenczi already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Balint Szekeres')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Balint Szekeres', N'balint-szekeres', N'canon', 1,
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
        @id, N'Balint Szekeres', N'balint-szekeres', N'Balint', N'Szekeres', N'Father',
        N'human', N'human', N'male', N'he/him', 64, N'alive',
        N'Bheur Chaplain of Korvarat Hall; officiates at all House rites; has served three generations; suspects the Liturgy extracts something from the dead during the Bheur ceremonies and cannot decide what to do about this',
        N'Balint Szekeres has officiated at every birth, death, marriage, and crisis at Korvarat Hall for thirty years. He knows the family in the specific way that the person who speaks at the moments that matter most knows a family -- he has been the theological container for every significant thing that has happened here. He conducts the Bheur rites with the full care and precision of someone for whom these rites are the center of his vocation. He has a genuinely warm relationship with Brother Ambrus Tarjan, which is the only relationship in the house that is uncomplicated.',
        N'The priest whose suspicion about the Bheur death-ceremonies is a door into what the Liturgy is actually doing -- and whose crisis of faith is quietly the most significant theological problem in the House.',
        N'No POV.',
        N'Korvarat Hall chapel and the surrounding estate; Chaplain for thirty years; trained in the Calyx Bheur tradition',
        171, 73, N'slightly bent from decades of bowing; the specific curvature of a man whose professional posture involves inclining toward something',
        N'white', N'maintained with the care of someone whose appearance is part of their office', N'short',
        N'dark brown', N'sallow', N'sallow; the indoor pallor of someone who spends a great deal of time in stone chapels',
        N'none',
        N'careful and bowed; he moves through the house as though every corridor is an approach to something significant; this has served him in the chapels and made him slightly awkward everywhere else',
        N'Bheur clerical dress; immaculate for ceremonies; practical versions for daily work; always the Calyx-tradition devotional elements prominently worn',
        N'none',
        N'Morning devotional practice in the chapel before anyone else arrives. The day''s rites and pastoral duties -- the family, the staff, whoever comes to him. Afternoon study and preparation for upcoming rites. He meets with Brother Ambrus Tarjan twice a week. Evening prayer. He has been sleeping less well for two years.',
        N'Thirty years of officiating at Bheur death-ceremonies has brought him to a suspicion he cannot name without admitting what he suspects. The Liturgy''s specific manner of attending the death-rites -- what they bring, how they position themselves, what they take when they leave -- suggests something is extracted from the dying or the dead. He does not know what. He does not know if this is what the Bheur tradition has always been and he has simply not seen it, or something the Liturgy has introduced. His faith rests on the premise that the dead go somewhere. The Liturgy''s behavior is inconsistent with this premise. He continues the rites. He has nothing else.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Korvarat Hall chapel and estate; he accompanies the family to major Bheur sites annually',
        N'0', N'0',
        N'old man in his mid-sixties, white maintained hair, dark brown eyes, sallow lined face with devotional expression, slightly bent posture from decades of bowing, immaculate Bheur clerical dress with Calyx-tradition devotional elements, careful approach movement, stone chapel interior, dark fantasy priest faith crisis',
        N'old man mid-sixties, white hair, dark eyes, sallow lined face, immaculate clerical dress, slightly bent careful posture, stone chapel interior',
        0, 0
    );
    PRINT 'Balint Szekeres seeded.';
END
ELSE PRINT 'Balint Szekeres already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Aniko Fekete')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Aniko Fekete', N'aniko-fekete', N'canon', 1,
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
        @id, N'Aniko Fekete', N'aniko-fekete', N'Aniko', N'Fekete', N'',
        N'human', N'human', N'female', N'she/her', 45, N'alive',
        N'House Librarian of Korvarat Hall; manages and has read most of the household library; found a sealed archive section five years ago containing the original Liturgy treaty in terms that bear no resemblance to the public version',
        N'Aniko Fekete has been the Korvarat librarian for sixteen years and has read approximately eighty percent of what the library contains, which is more than anyone else has managed. She organizes the collection with the precision of someone who believes that a library whose contents cannot be found quickly is a library that has failed its purpose. She assists the Archivist on occasion when the archive and the library''s collections overlap. She is pleasant, quiet, and has been carrying a significant document for five years without deciding what to do with it.',
        N'The librarian who found the original treaty -- the character whose discovery is the most politically explosive thing currently sitting unreported in the House, and the one the Archivist does not know about.',
        N'No POV.',
        N'Korvarat Hall library; Librarian for sixteen years; born to a minor Calyx administrative family',
        168, 64, N'precise; the build of someone whose primary physical activity is moving through stacks and whose mind is more active than her body',
        N'dark auburn', N'pinned for work; she has a specific way of pinning it that takes thirty seconds and that she has done every morning for sixteen years', N'medium',
        N'dark', N'warm medium', N'clear; the indoor complexion of someone whose primary environment is candlelit and stone',
        N'none',
        N'careful and observant; she moves through the library as a known terrain and through the rest of the house as an interesting unknown',
        N'librarian practical dress in muted colors; well-maintained; she owns one formal dress that she wears to the significant family occasions she is required to attend',
        N'none',
        N'In the library before most of the household wakes. The day''s cataloguing and reference requests. Assistance to the Archivist when overlap requires it. Afternoons are often her most uninterrupted research time. She has been spending several of those afternoons in the lower library, near the false panel, not opening it.',
        N'Five years ago she discovered a sealed archive section behind a false panel in the lower library, accessible through a key left in a book of agricultural tables by the previous librarian. The section contains the original treaty establishing House Calyx''s relationship with the Liturgy. The terms are nothing like the version that appears in the public records -- the original grants the Liturgy substantially more authority over the House''s Sphere 31 operations than anyone currently in the House appears to know. She has read it twice. She has not moved it. She has not told anyone. She is not sure who in the House could be trusted with it.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Korvarat Hall library and the archive when assisting the Archivist; she rarely leaves the estate',
        N'0', N'0',
        N'woman in her mid-forties, dark auburn hair pinned in a specific working style, dark eyes, warm medium indoor skin, precise careful build, muted librarian practical dress, careful observant movement, stone library interior with candles and stacks, dark fantasy scholar',
        N'woman mid-forties, dark auburn pinned hair, dark eyes, warm indoor skin, muted practical dress, careful posture, stone library interior with candles',
        0, 0
    );
    PRINT 'Aniko Fekete seeded.';
END
ELSE PRINT 'Aniko Fekete already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Csanad Fekete')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Csanad Fekete', N'csanad-fekete', N'canon', 1,
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
        @id, N'Csanad Fekete', N'csanad-fekete', N'Csanad', N'Fekete', N'',
        N'human', N'human', N'male', N'he/him', 14, N'alive',
        N'Page at Korvarat Hall; young household messenger; hears everything pages are not supposed to hear; carrying a memorized conversation from four months ago that he does not yet understand',
        N'Csanad Fekete has been a page at Korvarat for two years. He is fourteen, quick, and has learned that the way to hear things you are not supposed to hear is to be where you are supposed to be slightly longer than anyone expects. He is good at his work. He runs messages faster than the other page, knows the house''s layout well, and has the specific usefulness of a young person who has not yet become someone anyone needs to account for.',
        N'The page who carries a memorized conversation -- the youngest character holding an explosive piece of information and the one least equipped to understand what it means.',
        N'No POV.',
        N'Korvarat Hall; page for two years; a minor Calyx family connection placed him in the household',
        158, 52, N'growing; he is taller than he was six months ago and has not yet caught up with himself',
        N'brown', N'unmaintained beyond the basic', N'short',
        N'dark brown', N'warm medium', N'clear; young; the unlined skin of fourteen',
        N'none',
        N'quick and slightly too present for a page; he has not yet learned to be invisible; he compensates by being fast enough that his presence in a corridor reads as transit rather than observation',
        N'page uniform; it is being let down at the hem for the third time this year',
        N'none',
        N'Running messages through the house. Waiting in the antechambers. Being in the corridors between assignments, which is where pages are and which is where the things that are worth knowing are said. He is trying to get better at being less visibly attentive.',
        N'Four months ago he was in a corridor he was not supposed to linger in and overheard a conversation between Lord Radovan and Spymaster Csenge Horvath. He did not understand most of it. Something in the Lord''s voice -- a quality of controlled fear he had never heard in the Lord before -- made him memorize it word for word. He has been repeating the words silently to himself every night since, trying to understand them. He has not told anyone. He does not know if what he is carrying is important or whether he just thinks it is because of the tone.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Korvarat Hall; pages do not leave the estate without escort',
        N'0', N'0',
        N'boy of fourteen, brown unmaintained hair, dark brown eyes, warm medium skin, growing taller than he has caught up with, page uniform being let down at the hem, quick movement slightly too present for invisibility, stone manor corridors, dark fantasy young page',
        N'boy fourteen, brown hair, dark eyes, warm skin, page uniform being let down, quick movement, stone manor corridors',
        0, 0
    );
    PRINT 'Csanad Fekete seeded.';
END
ELSE PRINT 'Csanad Fekete already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Eniko Varad')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Eniko Varad', N'eniko-varad', N'canon', 1,
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
        @id, N'Eniko Varad', N'eniko-varad', N'Eniko', N'Varad', N'',
        N'human', N'human', N'female', N'she/her', 16, N'alive',
        N'Page at Korvarat Hall; quiet, reliable; has discovered she can retain every conversation she hears verbatim and is terrified this will be noticed',
        N'Eniko Varad has been a page at Korvarat for three years and has developed a reputation for being unusually reliable in carrying messages accurately, which is a reputation she is careful not to capitalize on in ways that would draw more attention. She is quieter than Csanad, more naturally invisible, and better at being where she is supposed to be in a way that does not read as attentiveness. She has had this reputation since she arrived. She now understands why.',
        N'The page with verbatim recall -- the most complete living record of conversations in Korvarat Hall and the character whose ability is the kind of thing the Liturgy would find notable if they knew about it.',
        N'No POV.',
        N'Korvarat Hall; page for three years; eastern plains minor family',
        162, 54, N'quiet; the specific build of someone who has been practicing being less visible for three years',
        N'dark', N'pulled back neatly; she manages her appearance as a form of invisibility', N'medium',
        N'dark', N'olive', N'clear; young; beginning to show the indoor quality of someone who lives in stone corridors',
        N'none',
        N'still and precise; she has mastered a version of the page''s invisibility that Csanad has not; she is harder to notice when she wants to be',
        N'page uniform; well-maintained; she has taken care of it in a way that reflects the same discipline she applies to everything else',
        N'none',
        N'Running messages. Waiting in antechambers. Being in corridors between assignments. She is better at this than Csanad and she is not sure whether being better at it is something she should try to be. She practices being forgettable in the evenings in a way she cannot explain to anyone.',
        N'She has had verbatim recall of every conversation she hears for as long as she can remember. She understood it was unusual when she began working in the House and realized no one else could do what she could do. She is afraid that if this is discovered, it will be notable to the Liturgy -- that it might be the kind of thing that gets someone taken. She has been doing her best to appear ordinary. She is sixteen and has been managing this fear alone for three years.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Korvarat Hall; pages do not leave without escort',
        N'0', N'0',
        N'girl of sixteen, dark pulled-back hair, dark eyes, olive clear young skin, quiet invisible build, well-maintained page uniform, still precise invisible movement, stone manor corridors or antechambers, dark fantasy young page with hidden ability',
        N'girl sixteen, dark pulled-back hair, dark eyes, olive young skin, well-maintained page uniform, still precise movement, stone manor corridors',
        0, 0
    );
    PRINT 'Eniko Varad seeded.';
END
ELSE PRINT 'Eniko Varad already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ilona Bekefi')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ilona Bekefi', N'ilona-bekefi', N'canon', 1,
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
        @id, N'Ilona Bekefi', N'ilona-bekefi', N'Ilona', N'Bekefi', N'',
        N'human', N'human', N'female', N'she/her', 58, N'alive',
        N'Laundry Master of Korvarat Hall; the most underestimated person in the house; has been reading correspondence left in pockets for twenty years; keeps a ledger in her grandmother''s dialect that could end careers',
        N'Ilona Bekefi has been managing the Korvarat laundry for twenty years. She receives the house''s garments, manages the washing and pressing, and returns them. In twenty years of doing this she has found, in pockets and folds and tucked into cuffs and collars, more correspondence than most people in this house have written. She does not search for these things. They come to her. She reads what she finds. She says nothing. She has been doing this for twenty years and has never once been asked if she found anything, which tells her more about how the house perceives her than anything else.',
        N'The laundry master who holds more than anyone in authority suspects -- the character whose ledger is the House''s most complete unofficial record and the most dangerous thing currently sitting unreported at Korvarat.',
        N'No POV.',
        N'Korvarat Hall laundry; Laundry Master for twenty years; eastern plains born',
        160, 69, N'substantial; the build of someone whose work is physical and wet and hot and who has been doing it for twenty years',
        N'grey', N'pinned back for laundry work; functional', N'medium when down',
        N'dark brown', N'warm medium', N'ruddy warm; the steam and heat of the laundry room has given her the same kind of complexion as the kitchen staff',
        N'none',
        N'grounded and practical; she moves with the ease of someone whose physical work is completely known to her; she is the most at ease person in the domestic staff',
        N'laundry working dress; practical; not maintained beyond the functional; she considers this appropriate given what happens to it',
        N'none',
        N'Before dawn receiving the previous day''s garments. The day''s washing and pressing. Returning the cleaned garments. She is meticulous about what she returns -- nothing is sent back in a state that could embarrass the person it belongs to. She reads in the evenings, in the laundry room, after everyone else has left.',
        N'For twenty years she has been reading correspondence found in pockets, in folded linens, tucked into garments left for pressing. She does not search for these -- they come to her. Over twenty years she has compiled a ledger written in her grandmother''s Transylvanian dialect, which no one else in the House reads, documenting what she has found. The ledger contains enough to end three careers in the cabinet and complicate two in the military command. She has never used any of it. She is waiting -- she has realized only recently that she is waiting -- for something she has not yet identified.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Korvarat Hall laundry and the estate; she does not leave except for the soap and lye supply runs',
        N'0', N'0',
        N'woman in her late fifties, grey pinned-back hair, dark brown eyes, ruddy warm steam-complexion, substantial working build, laundry working dress functional, grounded practical movement, stone laundry room with steam and drying lines, dark fantasy laundry intelligence',
        N'woman late fifties, grey pinned hair, dark eyes, ruddy warm face, substantial build, functional laundry dress, grounded movement, stone laundry room with steam',
        0, 0
    );
    PRINT 'Ilona Bekefi seeded.';
END
ELSE PRINT 'Ilona Bekefi already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Pal Szabo')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Pal Szabo', N'pal-szabo', N'canon', 1,
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
        @id, N'Pal Szabo', N'pal-szabo', N'Pal', N'Szabo', N'',
        N'human', N'human', N'male', N'he/him', 44, N'alive',
        N'Head of Household Guards at Korvarat Hall; not Myrmidons but the people who guard the Lord and Lady''s private rooms; former Corps soldier; has been running an informal intelligence assessment of the household''s loyalty',
        N'Pal Szabo took an injury in the Corps eight years ago that ended his active service and moved him to the household guard, which he has run with the professional caution of someone who knows that the people trying to reach the Lord are not always outside the estate. He manages six guards directly and the informal trust network that runs alongside them. He is direct, reliable, and has been quietly running an investigation that nobody authorized.',
        N'The household guard''s investigator -- the character closest to identifying the planted intelligence assets in the house, and the one who is going to hit a wall when one of his two suspects is someone he trusts.',
        N'No POV.',
        N'Korvarat Hall household guard; formerly Calyx Corps; attached to the private rooms and family security',
        179, 86, N'solid and scarred; the build of a Corps soldier who took a serious injury eight years ago and has maintained himself as rigorously as the injury permits',
        N'dark with grey', N'short, military practical', N'short',
        N'dark', N'scarred medium', N'scarred at the neck and shoulder from the Corps injury; otherwise clear',
        N'none',
        N'still and positioned; he stands where he can see the most of any room he is in; the specific stillness of someone trained to not move until movement is required',
        N'household guard dress; functional; worn with the same care as his Corps dress because he considers the standards to be the same',
        N'none',
        N'Daily security rotation and guard scheduling. Assessment of the household''s physical security and the private rooms'' access points. He attends every significant formal occasion as the family''s physical security, which puts him in the same rooms as the cabinet at the moments that matter. Evening review of the day''s assessments.',
        N'He has been systematically testing every person in the household against a behavioral profile he developed from his Corps intelligence training, trying to identify which members of the household are external intelligence assets. He has narrowed the field to two suspects. One of them he believes is correct. He has not reported his findings to the Spymaster because one of the two suspects he is tracking is someone he trusts more than almost anyone else in the house, and he is afraid of being right.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Korvarat Hall private rooms and formal spaces; the estate interior is his primary territory',
        N'0', N'0',
        N'man in his mid-forties, dark hair going grey, dark eyes, scarred medium skin at neck and shoulder, solid Corps-trained build with injury accommodation, household guard dress, still positioned movement in formal rooms, stone manor private rooms, dark fantasy household security',
        N'man mid-forties, dark greying hair, dark eyes, scarred neck and shoulder, solid build, household guard dress, still positioned posture, stone manor private rooms',
        0, 0
    );
    PRINT 'Pal Szabo seeded.';
END
ELSE PRINT 'Pal Szabo already exists.';
GO

-- ---------------------------------------------------------------------------
-- OATHLESS ADJACENT (2 characters)
-- ---------------------------------------------------------------------------

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Dragos Niculescu')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Dragos Niculescu', N'dragos-niculescu', N'canon', 1,
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
        @id, N'Dragos Niculescu', N'dragos-niculescu', N'Dragos', N'Niculescu', N'',
        N'human', N'human', N'male', N'he/him', 49, N'alive',
        N'Former House Calyx operative; officially Oathless; still used by the House for operations they cannot officially authorize; went Oathless twelve years ago as the price of his silence about what he found',
        N'Dragos Niculescu was a House Calyx operative for fifteen years before he went Oathless. He was good at the work. He is still good at the work, which is the only reason the House keeps using him for the operations they cannot put under their own name. He operates in the border territories and the edge of Compact-adjacent spaces where Calyx needs reach and cannot officially have it. He accepts the arrangement. He has regretted accepting it for twelve years. He has not yet found a reason to refuse that is larger than his need for the payment and the protection the informal arrangement provides.',
        N'The Oathless operative whose silence is the House''s insurance against its own history -- and whose regret about accepting that arrangement is the story''s access to whether the silence holds.',
        N'No POV.',
        N'Oathless; operates in border territories and Compact-adjacent spaces; was formerly assigned to Calyx eastern plains intelligence operations',
        178, 84, N'careful; the build of someone who has spent forty-nine years making himself hard to read and has succeeded at it in the way that has costs',
        N'dark', N'short, unmaintained as a choice', N'short',
        N'dark', N'warm olive', N'weathered; the outdoor wear of someone who has spent most of his adult life in places that are not inside',
        N'none',
        N'careful and economical; the movement of someone who defaults to not being noticed; he moves through spaces as though he has already mapped them',
        N'practical working dress without House markings; the clothing of someone who needs to cross several kinds of territory without being identified as belonging to any of them',
        N'none',
        N'He does not have a fixed schedule. He operates on assignment. Between assignments he is in the border territories, which is where he lives in the specific Oathless way of living: without fixed address, without fixed affiliation, without fixed obligation. He thinks about the document he found twelve years ago less than he used to.',
        N'He went Oathless twelve years ago not because he lost faith in House Calyx but because he found evidence -- during an operation in the House''s own archive, which he was not supposed to be accessing -- implicating a senior House figure in the deliberate ordering of the failed infusion campaign twenty-five years ago in terms that contradicted the official account. The House''s response was not to investigate. It was to offer him Oathless status and an ongoing informal arrangement as the price of his silence. He accepted. He has regretted accepting ever since. The informal arrangement pays and the alternative seemed worse. He is no longer certain it was.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Border territories between Calyx and adjacent Houses; Compact-adjacent spaces; he moves where the work requires him',
        N'0', N'0',
        N'man in his late forties, dark short unmaintained hair, dark eyes, warm olive weathered face, careful medium-solid build, practical working dress without House markings, economical mapped movement, border territory exterior or stone edge-spaces, dark fantasy Oathless operative',
        N'man late forties, dark short hair, dark eyes, olive weathered face, practical unmarked dress, economical careful movement, border territory exterior or stone spaces',
        0, 0
    );
    PRINT 'Dragos Niculescu seeded.';
END
ELSE PRINT 'Dragos Niculescu already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Bozena Kral')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Bozena Kral', N'bozena-kral', N'canon', 1,
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
        @id, N'Bozena Kral', N'bozena-kral', N'Bozena', N'Kral', N'',
        N'human', N'human', N'female', N'she/her', 42, N'alive',
        N'Oathless former Scrying operator from House Ophiuchus taking shelter in Calyx territory; expelled not for failure but for reporting what she saw to the wrong people; waiting for someone in authority to ask her what she found',
        N'Bozena Kral was a Scrying operator for House Ophiuchus for eleven years before she was expelled. Not for misconduct. Not for failure. For reporting what she observed in the apparatus to the people she believed were the appropriate recipients of significant information. Those people passed her report to the Liturgy. The Liturgy informed House Ophiuchus that she was unstable. House Ophiuchus expelled her quietly and apologized to the Liturgy. She has been in Calyx territory for two years. The House knows she is there. It tolerates her presence because her Scrying expertise has informal value. She provides consultation. She does not push. She waits.',
        N'The expelled operator who holds the most significant unreported Scrying discovery in the current narrative -- the character the Calyx Scrying staff does not know they should be talking to.',
        N'No POV.',
        N'Oathless; sheltering in Calyx eastern territory near the Scrying installation; formerly of House Ophiuchus Scrying operations',
        165, 61, N'watchful; the build of someone who has been managing her own physical and practical situation for two years without institutional support',
        N'brown', N'practical; managed in the way of someone who no longer has access to the resources that made appearance a consideration', N'medium',
        N'grey-green', N'pale', N'pale; the indoor pallor of an operator who has spent eleven years at an apparatus, now translated into outdoor pale because she has been living outside institutional walls for two years',
        N'none',
        N'watchful and positioned; she has the operator''s stillness and she uses it differently now -- not for observation sessions but for reading situations she cannot afford to misread',
        N'practical dress without House markings; the clothing of someone who has shed institutional identity and has not yet found a replacement',
        N'none',
        N'She manages her own schedule, which is the freedom and the problem of the Oathless. She provides consultation to the Calyx installation when asked, which is infrequently. She has been offered three positions that would require her to formally affiliate and has declined all three. She waits. She does not know how much longer she can wait.',
        N'What she saw in the apparatus before she was expelled was a systematic pattern in the Liturgy''s membrane transit operations suggesting the Sphere 31 takings are not incidental -- that specific selection criteria are being applied, consistently, across multiple Houses and multiple decades, toward an outcome she could not fully identify but whose direction she could infer. She reported this. The Liturgy was told. She was expelled. She has been waiting for two years for someone in a position of authority to ask her what she saw. No one has asked. She is not sure if this means no one is ready, or no one is aware she exists, or no one is safe.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Calyx eastern territory near the Scrying installation; her movement is technically free but practically bounded by resources',
        N'0', N'0',
        N'woman in her early forties, brown practical hair, grey-green eyes, pale indoor-to-outdoor skin, watchful build, practical dress without House markings, operator stillness translated to situational awareness, stone or outdoor Calyx eastern territory, dark fantasy expelled operator waiting',
        N'woman early forties, brown practical hair, grey-green eyes, pale skin, practical unmarked dress, watchful stillness, stone or outdoor Calyx eastern territory',
        0, 0
    );
    PRINT 'Bozena Kral seeded.';
END
ELSE PRINT 'Bozena Kral already exists.';
GO

PRINT 'House Calyx hierarchy seed complete -- 53 characters.';
GO

