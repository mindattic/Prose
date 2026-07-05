SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ============================================================
-- HOUSE LACERTA — FULL HIERARCHY SEED
-- 54 characters: Ruling Family, Political Cabinet, Military
-- Command, Scrying Installation, Domestic Staff, Oathless.
-- House Lacerta: Westernmost House; Atlantic cliff coast;
-- Iberian/Portugal analog; exploratory Scrying tradition.
-- The Lacerta Chamber at the cliff edge documents phenomena
-- no other House has recorded.
-- Generated: 2026-07-04
-- ============================================================

-- ============================================================
-- SECTION 1: RULING FAMILY (9)
-- ============================================================

-- 1. Lord Rodrigo Lacerta-Vante
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Rodrigo Lacerta-Vante')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Rodrigo Lacerta-Vante', N'rodrigo-lacerta-vante', N'canon', 1,
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
        @id, N'Rodrigo Lacerta-Vante', N'rodrigo-lacerta-vante', N'Rodrigo', N'Lacerta-Vante', N'Lord',
        N'human', N'human', N'male', N'he/him', 58, N'alive',
        N'Lord of House Lacerta; thirty years of Atlantic isolation and hard continental bargaining',
        N'Rodrigo inherited the House at twenty-six when his father died without warning during a winter siege. He governs with the discipline of a man who knows the sea is at his back: no retreat, no allies close enough to matter in time. He took the first Catalyst infusion at twenty-two — a Knight''s enhancement that gave him presence without spectacle. His wife Marisol died eight years ago and he has not remarried. He reviews the Scrying Chamber logs personally before the household wakes and has done so for eleven years without exception. The House respects him. They are not certain they know him.',
        N'The weight-bearer whose long tenure has not made him soft; he holds a private certainty about the western anomaly that shapes every decision in the House.',
        N'No POV.',
        N'House Lacerta; Atlantic cliff coast; continental border by arrangement',
        188, 91, N'broad-shouldered, dense with Knight enhancement, carries the weight of long command in his stillness',
        N'iron-gray', N'cropped close', N'short',
        N'sea-gray', N'deeply weathered olive', N'sun-lined, Atlantic-worn',
        N'Subtle height gain, increased density — the Knight''s mark, almost invisible after thirty-six years',
        N'Still and deliberate; commands space without appearing to try; comfortable sitting at the edge of a room watching',
        N'Heavy wool in House colors — dark green and salt-white — functional over formal; a commander''s dress even when seated for dinner',
        N'Knight-grade Catalyst infusion; increased bone density, marginal height gain, enhanced physical recovery',
        N'Rises before dawn to review Chamber logs privately; holds weekly command briefings; dines with family twice weekly; signs all political correspondence himself',
        N'For eleven years he has reviewed the Lacerta Chamber logs in private. The western phenomena has been increasing in frequency since Marisol died. He believes — with a certainty that frightens him — it is intentional and directed. He does not know what to do with this belief, so he has told no one, including his children.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Lacerta estate; cliff-top command post; Lacerta Chamber (full access)',
        N'0', N'0',
        N'weathered Iberian lord late fifties, iron-gray hair cropped short, sea-gray eyes, dark green and salt-white heavy wool, Atlantic cliff estate at dusk, commanding stillness, photorealistic portrait, cinematic lighting',
        N'A weathered Iberian man in his late fifties. Iron-gray hair cropped close, sea-gray eyes that have spent thirty years watching the Atlantic. Dark green and salt-white wool. Standing at the cliff edge at dusk with his hands clasped behind his back, commanding without performing it.',
        0, 0
    );
    PRINT 'Rodrigo Lacerta-Vante seeded.';
END
ELSE PRINT 'Rodrigo Lacerta-Vante already exists.';
GO

-- 2. Lady Marisol Lacerta-Vante (deceased — her presence still shapes the House)
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Marisol Lacerta-Vante')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Marisol Lacerta-Vante', N'marisol-lacerta-vante', N'canon', 1,
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
        @id, N'Marisol Lacerta-Vante', N'marisol-lacerta-vante', N'Marisol', N'Lacerta-Vante', N'Lady',
        N'human', N'human', N'female', N'she/her', 47, N'dead',
        N'Deceased Lady of House Lacerta; Rodrigo''s wife; mother of Catalina, Brais, and Ysolde; died eight years ago',
        N'Marisol died eight years ago of what the House officially recorded as fever. She was forty-seven. She had managed the estate through two military campaigns, negotiated a border adjustment that saved the northern grazing land, and knew every servant''s name, wage, and trouble. The staff still speaks of her in present tense by accident. Her portrait hangs in the main hall where every entering visitor sees it before they see the Lord. Rodrigo chose that placement. The kitchen still makes her favorite dishes on the anniversary of her death, without anyone asking. She left a journal. No one but Rodrigo knows what it contains.',
        N'The absence that shapes every relationship in the House; the truth of how she died is the House''s most dangerous secret and has not been spoken aloud by anyone still living.',
        N'No POV.',
        N'House Lacerta; Atlantic cliff coast',
        164, 58, N'slight, graceful, unmarked by augmentation',
        N'dark chestnut', N'loose waves', N'long',
        N'amber-brown', N'warm olive', N'clear and unmarked',
        N'none',
        N'recalled by those who knew her as moving with the particular ease of someone who was always precisely where she meant to be',
        N'estate dress in House dark green and salt-white, worn with less formality than the title required',
        N'none',
        N'In her final weeks, she conducted three nighttime visits to the Lacerta Chamber alone, staying hours at the apparatus looking west. She died four days after the third visit.',
        N'In her final weeks Marisol made three unauthorized visits to the Lacerta Chamber at night and spent hours at the apparatus looking west. She died four days after the third visit. The head operator at the time retired the following season and has never spoken of it. The journal she kept in her final month is the only written record. Rodrigo has read it. He has told no one what it says.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Lacerta estate (deceased)',
        N'0', N'0',
        N'painted portrait Iberian noblewoman, dark chestnut hair loose waves, amber-brown eyes, warm olive skin, estate dress green and white, soft candlelight, serene expression with something unresolved behind it',
        N'A painted portrait of an Iberian noblewoman in her mid-forties, dark chestnut hair in loose waves, amber-brown eyes, wearing estate dress in green and white. Soft candlelight. Serene expression, and something behind it that the serenity does not quite contain.',
        0, 0
    );
    PRINT 'Marisol Lacerta-Vante seeded.';
END
ELSE PRINT 'Marisol Lacerta-Vante already exists.';
GO

-- 3. Catalina Lacerta-Vante (Heir)
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Catalina Lacerta-Vante')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Catalina Lacerta-Vante', N'catalina-lacerta-vante', N'canon', 1,
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
        @id, N'Catalina Lacerta-Vante', N'catalina-lacerta-vante', N'Catalina', N'Lacerta-Vante', N'Lady',
        N'human', N'human', N'female', N'she/her', 32, N'alive',
        N'Heir of House Lacerta; manages eastern land disputes; has her father''s eyes and none of his patience for men who confuse volume with authority',
        N'Catalina has known since she was sixteen that she was the heir and has spent every year since learning to hate what the role costs her. She has her father''s eyes and his discipline, and none of his patience for military men who confuse volume with authority. She has managed the House''s eastern land disputes for four years without losing a single acre. She speaks four languages and has twice refused marriage proposals she recognized as leverage operations rather than alliances. She visits the Lacerta Chamber every fortnight under the pretext of administrative review. What she reads in those logs she has told no one.',
        N'The heir who has already outgrown the role she has not yet been given; her secret knowledge of the Chamber logs sets her on a collision course with her father.',
        N'No POV.',
        N'House Lacerta estate; eastern land holdings; Lacerta Chamber (informal access)',
        167, 61, N'lean, precise, the stillness of someone who has learned to conserve effort',
        N'dark brown', N'pulled back severely', N'long',
        N'dark gray', N'weathered olive', N'sea-wind marked',
        N'none',
        N'Economical and deliberate; she stops fully when she stops, moves with purpose when she moves; never fidgets',
        N'House colors, dark green and salt-white, in working dress — formal enough for her position, plain enough to move freely',
        N'none',
        N'Morning correspondence review; weekly land-dispute hearings; fortnightly Chamber visits she logs as administrative; evening diplomatic reading; manages household accounts alongside the Treasurer',
        N'She has been accessing the Lacerta Chamber logs for two years by persuading a trusted operator to include copies in the administrative file. She knows about the western anomaly. She has not told her father she knows, because she is afraid of what he will do — and because she suspects he already knows and has chosen silence, which is worse.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Lacerta estate; eastern land holdings; Lacerta Chamber (fortnightly)',
        N'0', N'0',
        N'sharp-featured Iberian noblewoman early thirties, dark brown hair pulled severely back, dark gray eyes, dark green House dress, Atlantic stone interior, intense controlled expression, photorealistic',
        N'A sharp-featured Iberian woman in her early thirties, dark gray eyes, dark hair pulled severely back, wearing dark green House dress. Seated at a stone desk with Atlantic light through a narrow window, posture precise, expression controlled and measuring.',
        0, 0
    );
    PRINT 'Catalina Lacerta-Vante seeded.';
END
ELSE PRINT 'Catalina Lacerta-Vante already exists.';
GO

-- 4. Brais Lacerta-Vante (Second Born, Paladin)
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Brais Lacerta-Vante')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Brais Lacerta-Vante', N'brais-lacerta-vante', N'canon', 1,
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
        @id, N'Brais Lacerta-Vante', N'brais-lacerta-vante', N'Brais', N'Lacerta-Vante', N'',
        N'human', N'human', N'male', N'he/him', 28, N'alive',
        N'Second-born son of House Lacerta; Paladin; commands outer cliff perimeter; chose to become formidable rather than wait to be needed',
        N'Brais was seventeen when Catalina was confirmed as heir. He spent eleven years becoming someone who did not need the title. He took his first Catalyst infusion at nineteen with House sanction. The second infusion — which the House did not sanction — he underwent alone at a Draught field station two years later, trading services to a Draught officer for access to their Practitioner. He survived both. His eyes changed to silver-pale after the second. He has never commented on this. He patrols the outer cliff installations daily and sleeps badly.',
        N'The second-born who made himself formidable to matter in a role the birth order foreclosed; the unsanctioned second infusion is a secret that could destabilize his standing in the House.',
        N'No POV.',
        N'House Lacerta outer cliff installations; perimeter territory',
        202, 116, N'post-human in scale from Paladin enhancement — significantly taller and denser than his natural frame, altered proportions',
        N'dark brown', N'very close-cropped', N'short',
        N'silver-pale', N'olive, slightly cooled in tone by the Paladin process', N'unmarked, clean',
        N'Evident enhancement — significant height, altered proportions, changed eyes',
        N'Occupies space carefully, as if still adjusting to a body that has grown beyond the architecture it lives in; formal in stillness, loose in motion',
        N'Military dark green without House decoration — functional, worn',
        N'Paladin-grade Catalyst series; two infusions; substantially increased height and mass, silver-pale eye coloration, enhanced strength and recovery. Second infusion was unsanctioned.',
        N'Dawn perimeter patrol of cliff installations; physical training; afternoon attendance at military briefings; correspondence with Draught contacts he does not share; rarely attends House social functions',
        N'The second infusion was done at Draught, unsanctioned by House Lacerta. The Practitioner there extracted a promise from him in exchange for the procedure. He has kept that promise for four years, at the cost of two intelligence reports he has quietly buried rather than share with the House. He does not know whether he will be able to keep keeping it.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Lacerta outer cliff installations; perimeter',
        N'0', N'0',
        N'powerfully built young Iberian man, abnormally tall from Paladin enhancement, silver-pale eyes, dark close-cropped hair, military dark green, cliff perimeter at dawn, watchful sleepless expression, photorealistic fantasy-steampunk',
        N'A powerfully built young Iberian man, abnormally tall from Paladin enhancement, silver-pale eyes that were not that color at birth, dark hair cropped close. Military dark green at a cliff perimeter at dawn. Watchful expression of someone who doesn''t sleep enough and has decided this is acceptable.',
        0, 0
    );
    PRINT 'Brais Lacerta-Vante seeded.';
END
ELSE PRINT 'Brais Lacerta-Vante already exists.';
GO

-- 5. Ysolde Lacerta-Vante (Youngest Child)
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ysolde Lacerta-Vante')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ysolde Lacerta-Vante', N'ysolde-lacerta-vante', N'canon', 1,
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
        @id, N'Ysolde Lacerta-Vante', N'ysolde-lacerta-vante', N'Ysolde', N'Lacerta-Vante', N'Lady',
        N'human', N'human', N'female', N'she/her', 21, N'alive',
        N'Youngest child of House Lacerta; being shaped for political marriage; has been underestimated so systematically that she has turned it into a skill',
        N'Ysolde has her mother''s eyes and her father''s refusal to accept conditions she did not choose. She is the youngest of Rodrigo''s children and has been treated for most of her life as the one who can be spared for politics — prepared for a marriage she did not arrange. She is not passive about this. She has read more political philosophy than the Chancellor and discusses it with no one in the House because she has learned that being taken seriously is contingent on appearing to agree. She is learning to wait. She is not patient by nature.',
        N'The youngest child who has been underestimated so systematically that she has turned it into a skill; her private correspondence may be the most consequential document in the House.',
        N'No POV.',
        N'House Lacerta estate',
        162, 53, N'slight, soft-featured, her mother''s build without her mother''s ease in it',
        N'dark brown with auburn highlights', N'loose', N'long',
        N'amber-brown', N'warm olive', N'softer than her siblings, less marked by Atlantic weather',
        N'none',
        N'Unhurried in public, deliberately so; her stillness is chosen, not natural',
        N'House dress in green and white, worn with a grace she does not always feel',
        N'none',
        N'Morning lessons with the Librarian; correspondence in the afternoon; formal dinners; managed social obligations to visiting delegations; increasingly difficult to locate in the evenings',
        N'She has been corresponding for eight months with a junior officer of House Ophiuchus, arranged through the Librarian who does not know the correspondence has turned into a negotiation. She is not in love with him. She is engineering a political alliance her father will not be able to refuse once it is complete — because he did not think of it first and she did.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Lacerta estate',
        N'0', N'0',
        N'young Iberian noblewoman 21, dark brown hair with auburn highlights loose, amber-brown eyes, warm olive skin, House green dress, Atlantic estate interior soft window light, contemplative expression, photorealistic',
        N'A young Iberian woman of twenty-one. Dark hair with auburn highlights worn loose to her shoulders, amber-brown eyes, wearing a House green dress. Seated by a stone window with Atlantic light, expression thoughtful and carefully neutral.',
        0, 0
    );
    PRINT 'Ysolde Lacerta-Vante seeded.';
END
ELSE PRINT 'Ysolde Lacerta-Vante already exists.';
GO

-- 6. Dowager Urraca Alcazar-Vante
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Urraca Alcazar-Vante')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Urraca Alcazar-Vante', N'urraca-alcazar-vante', N'canon', 1,
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
        @id, N'Urraca Alcazar-Vante', N'urraca-alcazar-vante', N'Urraca', N'Alcazar-Vante', N'Lady',
        N'human', N'human', N'female', N'she/her', 80, N'alive',
        N'Dowager of House Lacerta; Rodrigo''s mother; has attended four successions and outlived everyone who thought she wouldn''t',
        N'Urraca outlived her husband by thirty-one years and her eldest son by twenty, and has attended four successions in this House with varying degrees of approval. She is Rodrigo''s mother and has no formal role in the House''s political operation, which has never been a limitation. She has been a central node of its intelligence network since her husband''s tenure. She is physically frail in the way of a person who has chosen, at eighty, not to make their frailty anyone else''s concern. She moves slowly and hears everything. The servants are more afraid of her than they are of the Lord.',
        N'The eldest surviving authority; she holds information about the family line that she has never shared with Rodrigo, and she will not share it until the right moment — which she may not live to choose.',
        N'No POV.',
        N'House Lacerta estate; her private suite',
        157, 53, N'slight with age, commanding entirely through presence',
        N'white', N'pulled back tightly', N'short',
        N'very dark brown', N'heavily lined, papery', N'age-spotted, formidably marked',
        N'none',
        N'Slow and deliberate; she has mastered the art of arriving at a conclusion before anyone else has noticed she was moving toward one',
        N'Dark estate dress, simply cut; she stopped caring about fashion in her sixties and the household noticed',
        N'none',
        N'Late rising; morning attendance from senior household staff who report to her by habit; afternoon review of visitor movements; evening correspondence written in her own hand',
        N'She had the previous House Spymaster — the one before Daria Escobés — quietly disappeared twenty-two years ago. He had discovered something about the legitimacy of the family line that would have destabilized Rodrigo''s claim. The secret has not gone away. The ward Amadeu Lacerta-Riba is part of the answer, and Urraca is the only living person who holds all of it.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Lacerta estate; private suite',
        N'0', N'0',
        N'elderly Iberian dowager 80, white hair pulled tightly back, very dark sharp eyes, heavily lined face, dark estate dress, seated in carved chair, commanding frailty, stone estate interior, photorealistic',
        N'An eighty-year-old Iberian woman, white hair pulled tightly back, very dark eyes that miss nothing, heavily lined face, seated in a carved chair in a stone estate interior. Physically slight with age but holding the room by presence alone.',
        0, 0
    );
    PRINT 'Urraca Alcazar-Vante seeded.';
END
ELSE PRINT 'Urraca Alcazar-Vante already exists.';
GO

-- 7. Ferran Alcaine (Cousin, Paladin, Military Command)
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ferran Alcaine')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ferran Alcaine', N'ferran-alcaine', N'canon', 1,
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
        @id, N'Ferran Alcaine', N'ferran-alcaine', N'Ferran', N'Alcaine', N'',
        N'human', N'human', N'male', N'he/him', 44, N'alive',
        N'First cousin of Lord Rodrigo; Paladin; commands the northern cliff installation; House Lacerta''s strongest military field asset for twenty years',
        N'Ferran is Rodrigo''s first cousin and was, for twelve years, his most reliable field commander. He holds the northern cliff installation — strategically critical and politically marginal, which suits both of them. He is a two-infusion Paladin. His eyes went pale gold after the second. He has never commented on this. The Corps officers at the northern installation prefer him to the main estate''s political atmosphere. He visits the main House four times per year. He and Rodrigo do not argue. They do not discuss anything substantive in company.',
        N'The cousin whose two decades of military service conceal a double-reporting arrangement that makes him the most structurally dangerous person in the House.',
        N'No POV.',
        N'Northern cliff installation; House Lacerta military territory',
        197, 110, N'Paladin-scale: substantially taller and wider than his natural frame, altered proportions, dense',
        N'black', N'military close-crop', N'short',
        N'pale gold', N'dark olive, campaign-weathered', N'heavily weathered, wind-cut',
        N'Evident enhancement — significant height, altered proportions, changed eyes',
        N'Formal and economical; his scale makes a room feel smaller; he has learned to move through doorways without thinking about it',
        N'Military dark green, undecorated; he does not attend formal occasions if he can find a reason not to',
        N'Paladin-grade Catalyst series; two infusions; substantially increased height and mass, pale gold eye coloration, enhanced strength and recovery',
        N'Pre-dawn inspection of northern installation; morning officer briefings; afternoon patrol coordination; weekly intelligence summary to the main House; quarterly visits to Rodrigo that run for exactly one day',
        N'He has been providing quarterly intelligence summaries to House Atrament for six years — not the official ones but a second report, compiled separately, covering House Lacerta''s military disposition and readiness. He tells himself he is managing a contingency. He is increasingly uncertain this is true.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Northern cliff installation; House Lacerta military territory',
        N'0', N'0',
        N'powerfully built dark Iberian man 44, Paladin-enhanced height, pale gold eyes, black close-cropped hair, military dark green, northern cliff installation, stern distant expression, photorealistic fantasy-steampunk',
        N'A powerfully built Iberian-descent man of forty-four, significantly tall from Paladin enhancement, pale gold eyes, black hair cropped close. Dark military green at a cliff installation overlooking grey Atlantic. Expression controlled and distant.',
        0, 0
    );
    PRINT 'Ferran Alcaine seeded.';
END
ELSE PRINT 'Ferran Alcaine already exists.';
GO

-- 8. Inés Torreverde-Lacerta (Cousin, Knight, Married Out and Back)
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Inés Torreverde-Lacerta')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Inés Torreverde-Lacerta', N'ines-torreverde-lacerta', N'canon', 1,
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
        @id, N'Inés Torreverde-Lacerta', N'ines-torreverde-lacerta', N'Inés', N'Torreverde-Lacerta', N'Lady',
        N'human', N'human', N'female', N'she/her', 38, N'alive',
        N'Cousin of Lord Rodrigo; Knight; spent ten years in House Atrament by political marriage; returned after her husband died; manages eastern diplomatic relationships with unusual thoroughness',
        N'Inés took the Catalyst infusion at twenty-three and married into House Atrament at twenty-five, a political arrangement that benefited both Houses. Her husband died in a riding accident eleven years ago. She returned to House Lacerta the following season and has never fully explained what happened. She carries herself with the composure of someone who has navigated a foreign House for a decade and arrived home knowing things she was not supposed to bring back. She manages the House''s diplomatic relationships with the eastern Houses with a thoroughness that suggests she knows more about those Houses than any report she has filed.',
        N'The cousin whose decade in House Atrament makes her an intelligence asset the House does not fully know how to use; her husband''s death was not an accident.',
        N'No POV.',
        N'House Lacerta estate; diplomatic contact range',
        172, 68, N'Knight-enhanced: slightly taller and denser than her natural frame, carried with practiced ease',
        N'dark brown', N'worn loose below shoulders', N'long',
        N'green-brown', N'warm olive', N'composed, very little sun damage',
        N'Subtle height gain, increased density — the Knight''s mark',
        N'Composed and still; she occupies space with the economy of someone who spent ten years in a House where posture was intelligence',
        N'Estate dress, green and white; she dresses to be appropriate rather than noticed, which in this House means she is often the most interesting person in the room',
        N'Knight-grade Catalyst infusion; increased bone density, marginal height gain, enhanced recovery',
        N'Morning diplomatic correspondence; afternoon meetings with visiting delegations; consultation with the Chancellor; occasional late evenings in the records room she attributes to thoroughness',
        N'Her husband''s death was not a riding accident. He had discovered she was passing House Atrament intelligence to House Lacerta through a private arrangement she had set up without authorization from either House. She killed him before he could report her. She came back to Lacerta. The intelligence she built over that decade still feeds the House''s political picture. No one has asked how she knows what she knows.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Lacerta estate; eastern diplomatic range',
        N'0', N'0',
        N'Iberian noblewoman 38, dark brown hair loose to shoulders, green-brown eyes, Knight-enhanced build, estate dress green and white, composed watchful expression, stone interior, photorealistic',
        N'An Iberian woman of thirty-eight, dark brown hair loose to her shoulders, green-brown eyes. Wearing dark green estate dress, standing in a stone estate interior. Knight-enhanced posture — slightly taller and held with a composure that has been trained into place. Nothing in her expression she did not put there.',
        0, 0
    );
    PRINT 'Inés Torreverde-Lacerta seeded.';
END
ELSE PRINT 'Inés Torreverde-Lacerta already exists.';
GO

-- 9. Amadeu Lacerta-Riba (Ward)
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Amadeu Lacerta-Riba')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Amadeu Lacerta-Riba', N'amadeu-lacerta-riba', N'canon', 1,
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
        @id, N'Amadeu Lacerta-Riba', N'amadeu-lacerta-riba', N'Amadeu', N'Lacerta-Riba', N'',
        N'human', N'human', N'male', N'he/him', 19, N'alive',
        N'Ward of House Lacerta; orphaned son of a minor allied lord; quietly competent at logistics; notices inconsistencies in documents',
        N'Amadeu arrived at the House at seven as the orphaned son of a minor allied lord killed at the northern border. He was raised alongside Ysolde, who is two years older and has never treated him as a ward rather than a sibling, which has complicated his sense of where he belongs. He is not a member of the ruling family by blood — or so the official record states. He has been well-educated, is quietly competent at logistics and supply management, and has a habit of noticing inconsistencies in documents. He is not certain why the Dowager watches him with that particular expression.',
        N'The ward whose parentage is not what the record says; his presence in the House is the living consequence of a secret the Dowager has held for twelve years.',
        N'No POV.',
        N'House Lacerta estate',
        176, 71, N'lean, still growing into his frame, a young man who moves with more care than his age usually produces',
        N'dark auburn', N'slightly unruly', N'short',
        N'hazel', N'warm olive', N'clear, young',
        N'none',
        N'Quiet and observant; he has learned to be useful without requiring notice',
        N'House livery, dark green, in the working style assigned to junior household members',
        N'none',
        N'Morning training with junior officers; afternoon work in supply and logistics; tutoring with the Librarian three afternoons weekly; attends House dinners; observes more than he speaks',
        N'He has found three documents in the House archive that do not match the official record of his father''s identity. He has said nothing and shown them to no one. He is not sure what he would do with the answer if he found it — but he suspects the Dowager already knows, and he is too afraid of what that means to ask her.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Lacerta estate',
        N'0', N'0',
        N'young Iberian ward 19, dark auburn hair slightly unruly, hazel eyes, House livery dark green, estate stone corridor, quiet attentive expression, supply documents in hand, photorealistic',
        N'A young Iberian man of nineteen, dark auburn hair slightly unruly, hazel eyes. Wearing House livery in dark green, standing in a stone estate corridor holding a sheaf of supply documents he has clearly been studying carefully. Expression quiet and attentive.',
        0, 0
    );
    PRINT 'Amadeu Lacerta-Riba seeded.';
END
ELSE PRINT 'Amadeu Lacerta-Riba already exists.';
GO

-- ============================================================
-- SECTION 2: POLITICAL CABINET (7)
-- ============================================================

-- 10. Chancellor Ezequiel Marrón-Cía
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ezequiel Marrón-Cía')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ezequiel Marrón-Cía', N'ezequiel-marron-cia', N'canon', 1,
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
        @id, N'Ezequiel Marrón-Cía', N'ezequiel-marron-cia', N'Ezequiel', N'Marrón-Cía', N'',
        N'human', N'human', N'male', N'he/him', 52, N'alive',
        N'Chancellor of House Lacerta; manages all political correspondence and alliance negotiations; nineteen years under two Lords',
        N'Ezequiel has managed House Lacerta''s political correspondence for nineteen years under two Lords, and in that time has cultivated a professional manner so carefully maintained that most people who have worked with him for a decade cannot tell when he is afraid. He handles every treaty negotiation, every alliance approach, every border dispute letter. He knows the precise diplomatic weight of every word he omits from a document. He is excellent at his work. He has also been, for three years, exploring what House Fornax might offer a skilled Chancellor who came to them with nineteen years of Lacerta''s correspondence already memorized.',
        N'The political manager whose institutional loyalty is conditional; his private Fornax negotiation is the mechanism by which the House''s vulnerability becomes someone else''s weapon.',
        N'No POV.',
        N'House Lacerta estate; cabinet offices',
        171, 79, N'lean and desk-built, well-maintained for his age',
        N'silver', N'carefully side-parted', N'short',
        N'pale brown', N'indoor-pale olive', N'age-lined but not weathered — a man who has spent nineteen years indoors',
        N'none',
        N'Precise and controlled; his movements are small and deliberate; he has learned to communicate nothing through posture',
        N'Formal dark attire, House colors, always correct to the occasion',
        N'none',
        N'Morning priority correspondence review; drafting all outgoing treaty communications; afternoon consultation with the Lord and Inés; evening summary document prepared before the household retires',
        N'He has been in cautious indirect correspondence with a House Fornax intermediary for three years, not to defect but to establish a personal arrangement — a guaranteed position — if House Lacerta falls or is absorbed. He tells himself this is prudence. He is aware it constitutes treason in practice if not in intent. The Laundry Master found one of his drafts in his coat pocket fourteen months ago and has not said so.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Lacerta estate; cabinet offices',
        N'0', N'0',
        N'Iberian chancellor 52, silver carefully side-parted hair, pale brown eyes, formal dark attire, stone office interior afternoon light, ordered correspondence on desk, professionally unreadable expression, photorealistic',
        N'An Iberian man of fifty-two, silver hair carefully side-parted, pale brown eyes, wearing formal dark attire. Seated at a desk covered in ordered correspondence in a stone estate office, posture precisely correct, expression professionally unreadable.',
        0, 0
    );
    PRINT 'Ezequiel Marrón-Cía seeded.';
END
ELSE PRINT 'Ezequiel Marrón-Cía already exists.';
GO

-- 11. Spymaster Daria Escobés
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Daria Escobés')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Daria Escobés', N'daria-escobes', N'canon', 1,
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
        @id, N'Daria Escobés', N'daria-escobes', N'Daria', N'Escobés', N'',
        N'human', N'human', N'female', N'she/her', 46, N'alive',
        N'Spymaster of House Lacerta; runs intelligence operations against all six other Houses; has never once looked like what she is',
        N'Daria has been running House Lacerta''s intelligence operations for twelve years and has never looked like what she is, which she considers the primary qualification for the role. She has placed agents in all six other Houses, three of them personally. She does not share the full network with the Lord — not because she is disloyal, but because she has learned that Lords who know full networks eventually do something political with a name, and then that agent is gone. She has been working for two years on something she has told no one: placing an agent inside the Liturgy itself.',
        N'The spymaster whose unauthorized Liturgy placement is either the most brilliant move in the House''s intelligence history or the act that gets the House dissolved.',
        N'No POV.',
        N'House Lacerta; intelligence network reach; no fixed visible schedule',
        163, 58, N'slight and unremarkable — her primary professional asset',
        N'dark brown', N'short, pragmatic cut', N'short',
        N'very dark brown, almost black', N'medium olive', N'clear, unmemorable',
        N'none',
        N'She has mastered the art of not existing in a room; her exits are noticed more often than her arrivals',
        N'Plain dark working clothes that fit and do not distinguish; she owns nothing embroidered',
        N'none',
        N'No fixed schedule visible to household staff; reliably in her office before dawn and after midnight; communicates through written notes she burns after reading responses',
        N'She placed an agent inside the Liturgy two years ago. The placement took two years to engineer and the agent is functional. She has not told Lord Rodrigo because she is not certain he would sanction it — and if he ordered her to pull the agent, she would have to refuse or comply, and she has not finished deciding which. The agent''s most recent report states that the Liturgy is monitoring the Lacerta Chamber''s western anomaly documentation.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Lacerta; full intelligence reach by proxy',
        N'0', N'0',
        N'unremarkable Iberian woman 46, dark brown short practical hair, near-black eyes, plain dark working clothes, stone corridor late night, deliberate invisibility, photorealistic',
        N'An Iberian woman of forty-six, dark hair cut short and practically, near-black eyes, wearing plain dark working clothes — nothing that would distinguish her in a crowd. Standing in a stone corridor at night, expression entirely neutral, as if she is not particularly anywhere.',
        0, 0
    );
    PRINT 'Daria Escobés seeded.';
END
ELSE PRINT 'Daria Escobés already exists.';
GO

-- 12. Archivist Tomás Errieta
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Tomás Errieta')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Tomás Errieta', N'tomas-errieta', N'canon', 1,
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
        @id, N'Tomás Errieta', N'tomas-errieta', N'Tomás', N'Errieta', N'',
        N'human', N'human', N'male', N'he/him', 67, N'alive',
        N'House Archivist; thirty-one years keeping every treaty, genealogical record, and Scrying log; the institutional memory made flesh',
        N'Tomás has been the House Archivist for thirty-one years. He arrived as a young man and never left, which is a form of institutional absorption the records do not document but that is visible in the way he moves through the stacks — like a person who has memorized every room of a building in the dark. He knows where every treaty, every genealogical record, every Scrying log is filed. He also knows that two treaty texts have been replaced with copies since his arrival, and the originals are hidden in a location only he knows. He has read them. He has not decided what to do with what they say.',
        N'The institutional memory whose hidden treaty originals could reopen territorial claims that would destabilize the entire western coast.',
        N'No POV.',
        N'House Lacerta archive; estate records room',
        168, 71, N'slight and stooped from decades over documents',
        N'white', N'badly managed', N'short',
        N'watery blue', N'pale with age spots', N'ink-stained fingers, dry',
        N'none',
        N'Absorbed and slightly absent from the present room; moves through stacks with the ease of long memory',
        N'Archival work clothes, practical and ink-stained; formal dress is a costume he wears badly',
        N'none',
        N'Archive opens at dawn; Tomás is usually already inside. Cataloguing, preservation work, responding to records requests. Eats at his desk. Rarely attends formal dinners.',
        N'Twenty years ago he discovered that the original texts of two border treaties would prove House Lacerta has a legal claim on territory currently administered by House Pallor. The previous archivist had replaced them with edited copies. Tomás has kept the originals hidden. He has not told the Lord because he is not certain a border war is worth what those documents could prove, and he is the only person alive who knows the question exists.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Lacerta archive',
        N'0', N'0',
        N'elderly Iberian archivist 67, white badly managed hair, watery blue eyes, ink-stained fingers, surrounded by stacked leather-bound records, stone archive narrow windows afternoon light, absorbed expression, photorealistic',
        N'An elderly Iberian man of sixty-seven, white wispy hair, watery blue eyes, ink-stained fingers. Surrounded by towers of bound records in a stone archive lit by narrow windows. Expression completely absorbed, slightly remote from the present.',
        0, 0
    );
    PRINT 'Tomás Errieta seeded.';
END
ELSE PRINT 'Tomás Errieta already exists.';
GO

-- 13. Trade Ambassador Ramiro Alcántara
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ramiro Alcántara')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ramiro Alcántara', N'ramiro-alcantara', N'canon', 1,
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
        @id, N'Ramiro Alcántara', N'ramiro-alcantara', N'Ramiro', N'Alcántara', N'',
        N'human', N'human', N'male', N'he/him', 41, N'alive',
        N'Trade Ambassador of House Lacerta; manages border market agreements, shipping lanes, raw material supply chains; has never returned from a summit without an agreement',
        N'Ramiro manages House Lacerta''s commercial relationships. He is personable, well-dressed by estate standards, and has an instinct for what a counterpart wants from a negotiation before they have finished their first sentence. He has represented the House at six trade summits in five years and has never returned without a signed agreement. He travels three or four months per year. The House considers him an asset without complications. What the House does not know is that each agreement has included a small personal commission that Ramiro has been accumulating off-ledger for five years.',
        N'The competent trade operator whose private theft is either sustainable indefinitely or the thing that brings him down at the worst moment.',
        N'No POV.',
        N'House Lacerta estate; border markets; neutral-ground trade venues',
        177, 80, N'well-maintained for his age, travel-tested without showing it',
        N'dark brown with gray temples', N'neatly kept', N'short',
        N'dark brown', N'olive, travel-brightened', N'clear, well-maintained',
        N'none',
        N'Warmly professional in company; a man who makes rooms comfortable and uses that comfort',
        N'Well-made travel clothing in House colors — better quality than his rank strictly requires',
        N'none',
        N'Morning market correspondence; preparing for or recovering from trade delegation visits; weekly briefing with the Treasurer; travels three to four months per year',
        N'He has been taking a personal commission of one to three percent on every commercial agreement he finalizes, deposited into an account at a neutral clearing institution. Over five years this has become substantial. He has a plan to retire outside House Lacerta''s territory. He has not yet decided whether to leave before or after he is discovered.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Lacerta estate; border markets; travel range',
        N'0', N'0',
        N'Iberian trade ambassador 41, dark brown hair gray temples, dark brown eyes, well-maintained travel clothing House colors, trade meeting interior, warmly professional expression, photorealistic',
        N'An Iberian man of forty-one, dark brown hair with gray at the temples, dark brown eyes. Well-made travel clothing in House colors. Seated across a table in a meeting room, expression warmly professional, one hand resting on a trade document he has already won.',
        0, 0
    );
    PRINT 'Ramiro Alcántara seeded.';
END
ELSE PRINT 'Ramiro Alcántara already exists.';
GO

-- 14. Liturgy Liaison Izarne Narvaez
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Izarne Narvaez')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Izarne Narvaez', N'izarne-narvaez', N'canon', 1,
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
        @id, N'Izarne Narvaez', N'izarne-narvaez', N'Izarne', N'Narvaez', N'Sister',
        N'human', N'human', N'female', N'she/her', 39, N'alive',
        N'Liturgy Liaison attached to House Lacerta; reports House affairs to the Liturgy while reporting Liturgy affairs to the House; has been here long enough to develop opinions about both',
        N'Izarne was attached to House Lacerta six years ago as the official Liturgy representative — she reports House Lacerta''s affairs to the Liturgy while the House believes she reports the Liturgy''s affairs to them. This dual structure is known and accepted. What is not known is that Izarne has become genuinely uncertain which side she is on. She has been at House Lacerta long enough to develop opinions about its people, and long enough to understand that what the Liturgy intends to do with the Lacerta Chamber''s western anomaly data is not something the House would sanction.',
        N'The intermediary whose conflicted loyalty is the mechanism through which the Liturgy may move against the House; her moment of choice has not yet come.',
        N'No POV.',
        N'House Lacerta estate; Liturgy communication channel',
        165, 60, N'slight, formal in posture by training',
        N'black', N'ritual braid', N'long',
        N'dark brown', N'medium olive', N'composed, unmarked',
        N'none',
        N'Formal and still in official settings; in private she has a stillness that is harder to read — the stillness of a person working something through',
        N'Liturgy formal attire overlaid with House Lacerta colors — two institutional identities worn simultaneously',
        N'none',
        N'Morning Liturgy correspondence sealed by protocol; attendance at House cabinet meetings as observer; weekly private meeting with Lord Rodrigo; private religious observances in her quarters',
        N'The Liturgy instructed her to report every detail of the western anomaly documentation, and she has complied — until the most recent quarterly report, in which she omitted the frequency increase that Cándida Olaiz documented. She does not know why she made that choice. She has not corrected it. She is beginning to understand that the omission is itself a decision, and that she has already crossed a line she cannot return across.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Lacerta estate; Liturgy communication range',
        N'0', N'0',
        N'Liturgy liaison 39, black hair ritual braid, dark brown eyes, formal Liturgy attire with House Lacerta colors, stone estate chapel interior, conflicted composed expression, photorealistic fantasy-steampunk',
        N'A woman of thirty-nine, black hair in a formal ritual braid, dark brown eyes, wearing Liturgy formal attire overlaid with House Lacerta colors. Standing in a small stone estate chapel, expression composed and containing something that has not yet been composed.',
        0, 0
    );
    PRINT 'Izarne Narvaez seeded.';
END
ELSE PRINT 'Izarne Narvaez already exists.';
GO

-- 15. Treasurer Aldara Pérez-Cimarro
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Aldara Pérez-Cimarro')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Aldara Pérez-Cimarro', N'aldara-perez-cimarro', N'canon', 1,
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
        @id, N'Aldara Pérez-Cimarro', N'aldara-perez-cimarro', N'Aldara', N'Pérez-Cimarro', N'Mistress',
        N'human', N'human', N'female', N'she/her', 55, N'alive',
        N'Treasurer of House Lacerta; seventeen years managing House finances; technically a criminal by the Cauld''s financial codes and has not slept easily since',
        N'Aldara has managed House Lacerta''s finances for seventeen years. She inherited the accounts from a man who had held the role for twenty-six years, and when she reviewed his ledgers she found a twelve-year discrepancy that, if reported, would have ended with an investigation implicating a deceased general and several officers still alive. She made a decision. She absorbed the discrepancy over four years through adjustments so gradual they cannot easily be audited. She is meticulous. She is technically a criminal. She has not slept easily since.',
        N'The fiscal anchor whose twelve-year cover-up is the quiet structural weakness in every financial report the House relies on.',
        N'No POV.',
        N'House Lacerta estate; accounts room',
        160, 65, N'compact and desk-built; a woman who has not left her work for long in seventeen years',
        N'salt-and-pepper', N'pulled back tight', N'short',
        N'hazel', N'olive, indoor-worn', N'tired around the eyes, otherwise composed',
        N'none',
        N'Precise and economical; she does not waste movement or words and applies the same standard to money',
        N'Plain practical working dress; her one concession to formality is that her clothes are always clean',
        N'none',
        N'Dawn to dusk in the accounts room five days a week; weekly meeting with the Lord; quarterly meeting with Ramiro; monthly supply review with the Seneschal; has not taken a full day away from the accounts in seven years',
        N'The general whose accounts she absorbed was not just financially dishonest — he had been diverting House funds to pay for unauthorized Catalyst infusions for soldiers who died from the process. Seventeen deaths. She covered both the financial crime and the casualty record. She did it not for herself but because exposing it would destroy the Corps'' reputation at a moment the House couldn''t afford. She is not entirely certain this reasoning was honest.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Lacerta estate; accounts room',
        N'0', N'0',
        N'Iberian treasurer 55, salt-and-pepper hair pulled tight, hazel eyes, plain practical dress, accounts ledgers candlelight, stone office, methodical exhausted expression, photorealistic',
        N'An Iberian woman of fifty-five, salt-and-pepper hair pulled tightly back, hazel eyes, plain practical working dress. Seated in a stone accounts room surrounded by ledgers, a candle burning low, expression methodical and quietly tired.',
        0, 0
    );
    PRINT 'Aldara Pérez-Cimarro seeded.';
END
ELSE PRINT 'Aldara Pérez-Cimarro already exists.';
GO

-- 16. Diplomat Luca Barnera (currently stationed at House Atrament)
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Luca Barnera')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Luca Barnera', N'luca-barnera', N'canon', 1,
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
        @id, N'Luca Barnera', N'luca-barnera', N'Luca', N'Barnera', N'',
        N'human', N'human', N'male', N'he/him', 33, N'alive',
        N'Resident diplomat of House Lacerta, currently posted to House Atrament; charming, quick, and in serious inconvenient love with the person he was sent to observe',
        N'Luca was posted to House Atrament as House Lacerta''s resident diplomat two years ago. He is charming, intellectually quick, and has spent his posting cultivating relationships across all levels of Atrament''s political structure — which is, technically, his job. The difficulty is that he has also fallen into serious, inconvenient love with the person he was posted to observe. He is fully aware this compromises his professional capacity. He has not done anything about it, in either direction, and the situation is becoming untenable.',
        N'The diplomat abroad whose compromised heart is either the most useful asset the House has in House Atrament or the mechanism by which Atrament turns him into one of theirs.',
        N'No POV.',
        N'House Atrament (current posting); House Lacerta by affiliation',
        179, 72, N'lean, travel-maintained, presents well',
        N'dark brown', N'slightly wavy, kept neat', N'short',
        N'warm brown', N'olive, travel-brightened', N'clear, youthful',
        N'none',
        N'Warmly open in company; his charm is genuine, which makes it more dangerous than calculated charm would be',
        N'Formal diplomatic attire in House Lacerta dark green and white; well-made, appropriate to the House Atrament context',
        N'none',
        N'Diplomatic correspondence and meeting attendance at House Atrament; social navigation of Atrament''s political strata; private correspondence to House Lacerta three times weekly; the relationship he is not acknowledging occupies the remainder',
        N'He withheld intelligence from his reports for four months. He told the person he was surveilling that they were being observed, in a moment he cannot entirely account for. They have not reported him. They have not stopped seeing him. He does not know what to do. He writes to the Spymaster twice weekly without mentioning any of this.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Atrament (current); House Lacerta diplomatic range',
        N'0', N'0',
        N'young Iberian diplomat 33, dark wavy brown hair, warm brown eyes, formal diplomatic attire House Lacerta colors, elegant French-style reception hall, charming expression not quite reaching his eyes, photorealistic',
        N'A young Iberian man of thirty-three, dark wavy brown hair, warm brown eyes. Formal diplomatic attire in dark green and white House Lacerta colors. Standing in an elegant Atrament reception hall, expression pleasant and warm and containing something he is not showing.',
        0, 0
    );
    PRINT 'Luca Barnera seeded.';
END
ELSE PRINT 'Luca Barnera already exists.';
GO

-- ============================================================
-- SECTION 3: MILITARY COMMAND (9)
-- ============================================================

-- 17. Commander Dame Miren Salcedo (Paladin)
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Miren Salcedo')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Miren Salcedo', N'miren-salcedo', N'canon', 1,
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
        @id, N'Miren Salcedo', N'miren-salcedo', N'Miren', N'Salcedo', N'Dame',
        N'human', N'human', N'female', N'she/him', 47, N'alive',
        N'Commander of the Lacerta Myrmidon Corps; three-infusion Paladin; eight years commanding; knows exactly what the Corps can hold and has not told the Lord',
        N'Miren has commanded the Lacerta Myrmidon Corps for eight years. She survived three Catalyst infusions, which is rarer than two and not common enough for anyone to be casual about it. Her eyes changed color with the third. She has never commented on this. She is a frightening commander in a mobile engagement and a competent one in defense. She knows exactly what the Corps can hold against a sustained northern assault. She has not shared this assessment with the Lord, because the real number — the casualties they can absorb before the line breaks — is not one she believes he is ready to hear.',
        N'The commander who has been managing the gap between what she reports and what she knows for two years; if that gap closes in the field, the House will have no warning.',
        N'No POV.',
        N'Lacerta Myrmidon Corps; cliff installations; field range',
        196, 101, N'three-infusion Paladin scale — significantly taller than any untransformed person, dense and altered in proportion',
        N'iron-gray', N'cropped close', N'short',
        N'pale blue', N'weathered olive', N'campaign-marked, wind-cut',
        N'Evident enhancement — significant height, altered proportions, changed eyes',
        N'Commands space simply by being present; her scale and stillness do the work before she speaks',
        N'Military dark green, undecorated; she considers decoration a liability',
        N'Paladin-grade Catalyst series; three infusions; substantially increased height and mass, pale blue eye coloration, significantly enhanced strength and recovery',
        N'Dawn Corps inspection; morning briefings with Captains; afternoon field assessments; weekly report to the Lord; late evenings reviewing casualty projections she does not share',
        N'The Corps can hold the northern cliff line against a standard engagement for approximately six weeks. Against a joint Fornax-Draught assault — which intelligence suggests is being planned — she estimates three. She has been preparing a private defensive strategy involving abandoning the outer installations and consolidating at the main estate. She has not told the Lord because he will not authorize the abandonment, and she needs to be able to act without authorization if it comes to it.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Lacerta Myrmidon Corps; cliff installations',
        N'0', N'0',
        N'powerfully built Iberian female commander 47, Paladin three-infusion scale, iron-gray cropped hair, pale blue eyes, military dark green, cliff installation, authority without performance, photorealistic fantasy-steampunk',
        N'A powerfully built Iberian woman of forty-seven, significantly taller than average from three Catalyst infusions, iron-gray hair cropped close, pale blue eyes that were once dark brown. Military dark green at a cliff installation, expression of someone who knows things she cannot say.',
        0, 0
    );
    PRINT 'Miren Salcedo seeded.';
END
ELSE PRINT 'Miren Salcedo already exists.';
GO

-- 18. First Captain Raimundo Casteles (Knight)
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Raimundo Casteles')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Raimundo Casteles', N'raimundo-casteles', N'canon', 1,
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
        @id, N'Raimundo Casteles', N'raimundo-casteles', N'Raimundo', N'Casteles', N'',
        N'human', N'human', N'male', N'he/him', 40, N'alive',
        N'First Captain of the Lacerta Corps; ground operations commander; earned this posting through a campaign that his colleagues call decisive and he remembers as lucky',
        N'Raimundo has been First Captain for three years, earning this posting through a border campaign that the record describes as decisive. He is technically capable — organized, reliable, good at logistics — and has not yet been in an engagement of sufficient severity to test whether the version of himself in that campaign was real or a product of circumstances he cannot replicate. He thinks about this more than he admits. He trains harder than anyone in the Corps and sleeps less. He is the ground operations commander for a force whose defense plan he has not been fully briefed on.',
        N'The ground operations commander whose competence under real pressure is an untested quantity; his private fear is the structural weakness in the Corps'' chain of command.',
        N'No POV.',
        N'Lacerta Myrmidon Corps; ground operations range',
        183, 87, N'Knight-enhanced: slightly taller and denser than his natural frame',
        N'dark brown', N'military cut', N'short',
        N'dark brown', N'weathered olive', N'campaign-marked',
        N'Subtle height gain, increased density — the Knight''s mark',
        N'Intense and forward-moving; trains with his soldiers rather than supervising; always in motion',
        N'Military dark green, Corps standard',
        N'Knight-grade Catalyst infusion; increased bone density, marginal height gain, enhanced physical recovery',
        N'Dawn formation inspection; morning ground operations planning; afternoon training he participates in rather than supervises; evening after-action reviews',
        N'The campaign that made his reputation worked because the enemy''s lead officer made an error that had nothing to do with Raimundo''s decisions. He saw it happen and exploited it. He has never corrected the official account. He does not know if he would hold under fire in a scenario where luck did not intervene first. He is very afraid of finding out.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Lacerta Myrmidon Corps; ground operations',
        N'0', N'0',
        N'Iberian military captain 40, dark brown hair and eyes, Knight-enhanced build, dark green Corps uniform, cliff installation training ground, intense focused expression, photorealistic',
        N'An Iberian man of forty, dark brown hair, dark eyes, Knight-enhanced build in dark green military uniform. Standing on a training ground near the cliff installations watching a formation, expression focused and not quite at ease.',
        0, 0
    );
    PRINT 'Raimundo Casteles seeded.';
END
ELSE PRINT 'Raimundo Casteles already exists.';
GO

-- 19. Second Captain (Dame) Sabela Oria (Knight)
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Sabela Oria')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Sabela Oria', N'sabela-oria', N'canon', 1,
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
        @id, N'Sabela Oria', N'sabela-oria', N'Sabela', N'Oria', N'Dame',
        N'human', N'human', N'female', N'she/her', 35, N'alive',
        N'Second Captain; garrison and estate perimeter commander; plans for what goes wrong rather than what is supposed to go right; eleven weeks pregnant',
        N'Sabela commands the garrison and the estate''s defensive perimeter — the unglamorous half of a Corps posting that she has turned into something resembling an art form. Her defensive protocols are meticulous. She is known among the Corps soldiers as someone who plans for what goes wrong, which makes her unpopular with optimists and invaluable to everyone else. She is also, at present, eleven weeks pregnant. She has not told the Corps, the Commander, or the Physician. Knight-enhanced physiology delays some visible indicators. She does not know how long she has before concealment becomes impossible.',
        N'The garrison commander whose personal situation creates a structural gap in the defensive command at the worst possible moment.',
        N'No POV.',
        N'Lacerta garrison; estate defensive perimeter',
        170, 66, N'Knight-enhanced: slightly taller and denser than her natural frame',
        N'chestnut', N'military braid', N'long',
        N'green', N'olive', N'composed, outdoor-marked',
        N'Subtle height gain, increased density — the Knight''s mark',
        N'Controlled and forward; her posture is doing extra work and she knows it',
        N'Military dark green, Corps garrison standard',
        N'Knight-grade Catalyst infusion; increased bone density, marginal height gain, enhanced physical recovery',
        N'Dawn garrison inspection; perimeter review; defensive fortification planning; officer meetings; has reorganized her personal schedule to avoid situations where physical changes would be noticed',
        N'She is pregnant. The father is a soldier in her own garrison — a relationship that violates Corps conduct protocols. She has not told him. She has not told anyone. She is calculating whether there is a version of this that does not end her career and has not found it. She needs six more weeks of concealment, maybe eight, and she does not think she will get them.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Lacerta garrison; estate defensive perimeter',
        N'0', N'0',
        N'Iberian female military captain 35, chestnut hair military braid, green eyes, Knight-enhanced build, dark green garrison uniform, estate defensive perimeter, controlled posture doing extra work, photorealistic',
        N'An Iberian woman of thirty-five, chestnut hair in a precise military braid, green eyes, dark green garrison uniform. Standing at the estate perimeter, posture controlled and forward-looking, doing the work of containing something that has no place in this role.',
        0, 0
    );
    PRINT 'Sabela Oria seeded.';
END
ELSE PRINT 'Sabela Oria already exists.';
GO

-- 20. Specialist Captain Basilio Tordaya (Knight, Scrying defense)
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Basilio Tordaya')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Basilio Tordaya', N'basilio-tordaya', N'canon', 1,
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
        @id, N'Basilio Tordaya', N'basilio-tordaya', N'Basilio', N'Tordaya', N'',
        N'human', N'human', N'male', N'he/him', 38, N'alive',
        N'Specialist Captain; commands security for the Lacerta Chamber installation; rigorous about perimeter security and less rigorous about one specific access point',
        N'Basilio commands the security detail assigned to the Lacerta Chamber — the soldiers who are supposed to prevent unauthorized access to the apparatus. He is the formal point of contact between the Corps and the Scrying staff, which means he interacts with Cándida Olaiz weekly and has developed a relationship she would describe as someone asking questions he does not have the authority to ask. He is rigorous about perimeter security. He is less rigorous about a particular access point he has been told, unofficially, to leave unmonitored.',
        N'The security officer whose selective enforcement of access protocols is either incompetence or a managed vulnerability; the person who arranged it has not yet used it in a way that can be proven.',
        N'No POV.',
        N'Lacerta Chamber security perimeter; cliff installations',
        181, 83, N'Knight-enhanced: slightly taller and denser than his natural frame',
        N'black', N'military cut', N'short',
        N'dark brown', N'dark olive', N'weathered',
        N'Subtle height gain, increased density — the Knight''s mark',
        N'Watchful and deliberate at perimeter; walks the cliff edge at night, which he attributes to personal habit',
        N'Military dark green, security variant',
        N'Knight-grade Catalyst infusion; increased bone density, marginal height gain, enhanced physical recovery',
        N'Perimeter checks around the Chamber installation three times daily; weekly security briefing with First Captain; officer coordination with Cándida''s staff; late-night cliff walks',
        N'He was approached fourteen months ago by someone claiming to represent a private scholarly institution — not a House, or so they claimed — and was paid to leave a specific access route to the Chamber exterior unmonitored during particular watch shifts. He has complied on three occasions. He does not know who has used the access point or for what. He has told himself it is probably harmless. He is increasingly certain it is not.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Lacerta Chamber security perimeter',
        N'0', N'0',
        N'Iberian security captain 38, black hair, dark brown eyes, dark olive skin, Knight-enhanced build, dark green security uniform, Lacerta Chamber cliff exterior at night, wary unsettled expression, photorealistic',
        N'An Iberian man of thirty-eight, black hair, dark brown eyes, dark olive skin, Knight-enhanced build in dark green security uniform. Standing at the exterior perimeter of the Lacerta Chamber at the cliff edge at night, expression watchful and not entirely settled.',
        0, 0
    );
    PRINT 'Basilio Tordaya seeded.';
END
ELSE PRINT 'Basilio Tordaya already exists.';
GO

-- 21. Infirmary Commander Pilar Andurra
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Pilar Andurra')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Pilar Andurra', N'pilar-andurra', N'canon', 1,
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
        @id, N'Pilar Andurra', N'pilar-andurra', N'Pilar', N'Andurra', N'',
        N'human', N'human', N'female', N'she/her', 44, N'alive',
        N'Infirmary Commander; runs the field hospital attached to the Lacerta Corps; files casualty reports with a specificity that buries the most dangerous numbers in technically correct categories',
        N'Pilar runs the field hospital attached to the Lacerta Corps and has been doing so for fourteen years. She has a clinical detachment that the soldiers find reassuring until they are in her infirmary, at which point they find it frightening. She is competent, direct, and has made decisions in the field that saved lives at the cost of official policy. She files her casualty reports accurately. She files her Catalyst-related casualty reports with a level of specificity that buries the most dangerous numbers in categories that are technically correct and practically misleading.',
        N'The medical officer whose accurate-but-buried reports are the only record of how many soldiers have died from Catalyst infusions rather than enemy action.',
        N'No POV.',
        N'Lacerta Corps infirmary; field hospital',
        163, 62, N'compact and clinical, built for endurance rather than strength',
        N'dark brown', N'practical bun', N'long',
        N'dark brown', N'medium olive', N'indoor-marked, precise',
        N'none',
        N'Clinical and efficient; she moves through her infirmary without wasted motion and through political settings with the same economy',
        N'Clinical infirmary attire; formal Corps dress for required occasions',
        N'none',
        N'Dawn infirmary rounds; twice-weekly surgical reviews; casualty reporting that takes longer than it should; field readiness assessments; monthly Catalyst protocol review',
        N'In fourteen years she has documented forty-three Catalyst deaths in the Lacerta Corps — soldiers who died not from enemy action but from the infusion process. Eleven of those deaths occurred under Commander Miren''s tenure and were authorized with inadequate screening. The reports are accurate and are filed in a sub-category the political cabinet does not routinely review. She has not brought this to the Lord''s attention because she does not know if the Commander knew the candidates were high-risk and approved them anyway.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Lacerta Corps infirmary; field hospital',
        N'0', N'0',
        N'Iberian field physician 44, dark brown hair practical bun, dark brown eyes, clinical infirmary attire, field hospital interior stone arches, clinical expression, photorealistic fantasy-steampunk',
        N'An Iberian woman of forty-four, dark brown hair in a practical bun, dark brown eyes, clinical attire. In a field hospital interior with stone arches, reviewing a casualty ledger with focused professional neutrality.',
        0, 0
    );
    PRINT 'Pilar Andurra seeded.';
END
ELSE PRINT 'Pilar Andurra already exists.';
GO

-- 22. Senior Sergeant Xoán Carroyo (Knight, 28 years in the Corps)
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Xoán Carroyo')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Xoán Carroyo', N'xoan-carroyo', N'canon', 1,
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
        @id, N'Xoán Carroyo', N'xoan-carroyo', N'Xoán', N'Carroyo', N'',
        N'human', N'human', N'male', N'he/him', 53, N'alive',
        N'Senior Sergeant; twenty-eight years in the Lacerta Corps; institutional memory made flesh; knows where the Corps'' historical bodies are buried',
        N'Xoán has been in the Lacerta Corps for twenty-eight years and has served under four commanders, surviving two engagements that the official record calls victories and that he remembers as managed disasters. He has trained more soldiers than anyone currently in the Corps. The junior officers go to him before they go to their Captains, which the Captains know and tolerate because Xoán''s advice is generally better than theirs. He knows every practice and unofficial protocol the Corps has developed in three decades. He also knows which officers fifteen years ago falsified their battle reports — including the current Commander.',
        N'The sergeant whose institutional knowledge is a resource and a threat in equal measure; his silence about the Commander''s falsified report is the quiet ground on which their whole chain of command rests.',
        N'No POV.',
        N'Lacerta Myrmidon Corps; training grounds',
        182, 91, N'Knight-enhanced and dense from twenty-eight years of physical service',
        N'gray', N'short military cut', N'short',
        N'brown', N'very weathered dark olive', N'deeply weather-marked, every year of the Cauld on his face',
        N'Subtle height gain, increased density — the Knight''s mark',
        N'Moves with the ease of someone who has done this for twenty-eight years; his knee is bad and he does not show it',
        N'Corps uniform worn with the ease of something he was born in',
        N'Knight-grade Catalyst infusion; increased bone density, marginal height gain, enhanced physical recovery',
        N'Pre-dawn, he is on the training ground before the junior soldiers arrive. He does not run the formations anymore — his knee — but he watches and speaks to individuals afterward. He eats with the enlisted. He sleeps in the barracks wing by choice.',
        N'Fifteen years ago he witnessed Commander Miren — then a Captain — alter a battle report after an engagement in which three soldiers died due to orders she gave and then changed on the record. The soldiers'' deaths became enemy action rather than command error. He has never spoken of it. He has served under her since she was promoted past him. He tells himself the Corps needs her more than it needs accountability for those three men. He is not certain this is true.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Lacerta Myrmidon Corps; training grounds; barracks',
        N'0', N'0',
        N'weathered Iberian sergeant 53, gray military cut, brown eyes, heavily weathered dark olive skin, Knight-enhanced Corps uniform worn with decades of ease, training ground, watchful paternal expression, photorealistic',
        N'A weathered Iberian man of fifty-three, gray hair in a military cut, brown eyes, built like someone who has absorbed twenty-eight years of physical discipline. Corps uniform worn like a second skin. Standing on a training ground watching junior soldiers, expression watchful, tired, and precise.',
        0, 0
    );
    PRINT 'Xoán Carroyo seeded.';
END
ELSE PRINT 'Xoán Carroyo already exists.';
GO

-- 23. Veteran Lupe Garraiz (Knight, near retirement)
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Lupe Garraiz')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Lupe Garraiz', N'lupe-garraiz', N'canon', 1,
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
        @id, N'Lupe Garraiz', N'lupe-garraiz', N'Lupe', N'Garraiz', N'Dame',
        N'human', N'human', N'female', N'she/her', 51, N'alive',
        N'Veteran Corps soldier near retirement; twenty-four years of service; retiring in four months; the only person in the House who has seriously thought about what lies west',
        N'Lupe has been in the Lacerta Corps for twenty-four years and has been eligible for retirement for three. She stayed because she was not ready and because nothing particular was waiting on the other side. Something is waiting now. She has spent eight years systematically saving money, doing small paid jobs during leave, and has a plan. The plan involves a boat and the Atlantic. She is the only person in the Corps — possibly in the House — who has thought seriously about what lies west. She is retiring in four months and has told no one what she intends to do after.',
        N'The veteran whose departure takes the Corps'' second-best institutional memory with it; what she plans to do with her freedom is the most interesting thing about the House''s future.',
        N'No POV.',
        N'Lacerta Myrmidon Corps; transition to Lacerta territory fringe',
        171, 70, N'Knight-enhanced and seasoned, compact strength',
        N'silver-streaked dark brown', N'cropped short', N'short',
        N'amber-brown', N'weathered olive', N'sun and wind-marked',
        N'Subtle height gain, increased density — the Knight''s mark',
        N'Efficient and economical; she has spent twenty-four years moving without waste and continues it in everything',
        N'Corps uniform, wearing it with the ease of long practice; has already started acquiring civilian clothing',
        N'Knight-grade Catalyst infusion; increased bone density, marginal height gain, enhanced physical recovery',
        N'Duties reduced in preparation for retirement; trains junior soldiers in terrain navigation; spends evenings reviewing nautical charts she has been collecting quietly for years',
        N'She is going to sail west when she retires. She has been studying the Atlantic approaches for eight years, spoken to every former sailor she could find, and saved enough to outfit a boat. She knows the House would consider this insane. She does not know what she will find. She has begun to believe, from things she has overheard about the Scrying Chamber, that something west might be worth finding.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Lacerta Myrmidon Corps',
        N'0', N'0',
        N'veteran Iberian female soldier 51, silver-streaked dark hair cropped short, amber-brown eyes, Corps uniform, cliff edge overlooking Atlantic, looking west, expression that is not military at all, photorealistic',
        N'A veteran Iberian woman of fifty-one, silver-streaked dark hair cropped short, amber-brown eyes, Corps uniform. Standing at a cliff edge overlooking a grey Atlantic, looking west. Expression that is not military at all.',
        0, 0
    );
    PRINT 'Lupe Garraiz seeded.';
END
ELSE PRINT 'Lupe Garraiz already exists.';
GO

-- 24. Junior Officer Iker Balanza
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Iker Balanza')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Iker Balanza', N'iker-balanza', N'canon', 1,
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
        @id, N'Iker Balanza', N'iker-balanza', N'Iker', N'Balanza', N'',
        N'human', N'human', N'male', N'he/him', 24, N'alive',
        N'Junior officer noted in the Commander''s last quarterly report; attached to the Lacerta Chamber security detail; being considered for the first Catalyst infusion',
        N'Iker distinguished himself six months ago during a northern border probe that the Corps officers have since described as quick thinking under fire. Commander Miren noted him in her quarterly report. He has been assigned to Basilio Tordaya''s detail since then and has been offered the first Catalyst infusion at the end of the current season. He carries himself with the careful confidence of someone who knows they are being watched and is managing the performance of merit. He is competent. He is also the reason the northern border probe went the way it did, and that reason is not what anyone credits him with.',
        N'The rising officer whose reputation is built on something other than what the reputation claims; the truth of the border probe is a weapon for whoever finds it.',
        N'No POV.',
        N'Lacerta Myrmidon Corps; Chamber security detail',
        175, 72, N'lean and fit, untransformed, built for speed rather than mass',
        N'dark brown', N'military cut', N'short',
        N'dark brown', N'olive', N'young, unmarked',
        N'none',
        N'Alert and controlled; the expression of someone who is very aware of being observed and managing what he shows',
        N'Corps uniform, worn correctly and crisply',
        N'none',
        N'Attached to Chamber security detail under Basilio; additional training sessions with Senior Sergeant Carroyo; preparing for Catalyst evaluation at season end; attends officer social functions more carefully than he attends anything else',
        N'During the northern border probe, he saw a soldier in his unit commit a war crime against a captured enemy scout. Rather than report it, he killed the captured scout to eliminate the evidence, then wrote the encounter report to show a clean engagement. His commanding officer praised his decisiveness. He has been called decisive ever since. He does not feel decisive. He feels like a person who solved a problem by making it worse.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Lacerta Myrmidon Corps; Chamber security perimeter',
        N'0', N'0',
        N'young Iberian military officer 24, dark brown hair and eyes, olive skin, Corps uniform, Lacerta Chamber security perimeter, alert controlled expression managing what he shows, photorealistic',
        N'A young Iberian man of twenty-four, dark brown hair and eyes, Corps uniform. Standing at the Lacerta Chamber security perimeter, expression alert and controlled — the expression of someone who is very aware of being observed.',
        0, 0
    );
    PRINT 'Iker Balanza seeded.';
END
ELSE PRINT 'Iker Balanza already exists.';
GO

-- 25. Transmutation Practitioner Nuria Salves
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Nuria Salves')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Nuria Salves', N'nuria-salves', N'canon', 1,
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
        @id, N'Nuria Salves', N'nuria-salves', N'Nuria', N'Salves', N'',
        N'human', N'human', N'female', N'she/her', 49, N'alive',
        N'Transmutation Practitioner attached to the Lacerta Corps; administers Catalyst infusions before campaigns; has been running an unauthorized screening protocol for nineteen years',
        N'Nuria administers the Catalyst infusions for the Lacerta Corps. She is the person who stands between a soldier and an 80% lethal process and is supposed to do so objectively. She has done this for nineteen years. She has a survival rate above the Cauld average. This is partly because she is skilled, and partly because she has been quietly refusing to administer to candidates she assesses as high-risk, based on physical and psychological markers she has developed over nineteen years that are entirely personal and have no official sanction. She has never been challenged on a refusal because she phrases her assessments carefully.',
        N'The practitioner who has been running an unauthorized screening protocol for two decades; the soldiers she saved are alive because of decisions she had no sanctioned right to make.',
        N'No POV.',
        N'Lacerta Corps medical wing; infusion chamber',
        162, 60, N'compact and still; a person who has spent nineteen years in assessment work',
        N'dark brown going gray', N'pulled back', N'medium',
        N'pale brown', N'olive', N'indoor-careful',
        N'none',
        N'Careful and contained; she moves through assessments with the deliberateness of someone who knows that her decisions are consequential',
        N'Clinical assessment attire; practical and unadorned',
        N'none',
        N'Pre-infusion assessments in the medical wing; monthly review of candidate lists submitted by commanding officers; records maintenance; consultation with Pilar on post-infusion outcomes',
        N'Her unofficial screening protocol has prevented, she estimates, nineteen deaths in nineteen years. It is always framed as a medical finding that delays rather than refuses. Four of those delays resulted in the candidate eventually completing a successful infusion under better conditions. The other fifteen were quietly retired from consideration. She is aware that if the Commander reviewed her decisions carefully, she would find nineteen cases where Nuria substituted her personal judgment for the Corps''. She believes she was right every time. She is aware this is exactly what every person who abuses authority believes.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Lacerta Corps medical wing',
        N'0', N'0',
        N'Iberian Catalyst practitioner 49, dark going-gray hair, pale brown eyes, clinical assessment attire, medical wing stone arches, careful expression making an unauthorized decision, photorealistic fantasy-steampunk',
        N'An Iberian woman of forty-nine, dark hair going gray, pale brown eyes, clinical assessment attire. Seated in a stone medical wing reviewing a candidate file with the focused attention of someone making a decision no one has officially authorized her to make.',
        0, 0
    );
    PRINT 'Nuria Salves seeded.';
END
ELSE PRINT 'Nuria Salves already exists.';
GO

-- ============================================================
-- SECTION 4: SCRYING INSTALLATION (6)
-- ============================================================

-- 26. Head Scrying Operator Cándida Olaiz
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Cándida Olaiz')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Cándida Olaiz', N'candida-olaiz', N'canon', 1,
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
        @id, N'Cándida Olaiz', N'candida-olaiz', N'Cándida', N'Olaiz', N'',
        N'human', N'human', N'female', N'she/her', 62, N'alive',
        N'Head Scrying Operator; thirty-four years at the Lacerta Chamber apparatus; has built toward a conclusion she has not put in any official report',
        N'Cándida has been at the Lacerta Chamber''s apparatus for thirty-four years. She came as a young woman to an installation already old, trained under the previous head operator until he retired blind, and has been the primary observer since. She knows the apparatus the way other people know languages — not as something learned but as something inhabited. Her eyes have adjusted to the Chamber''s dim work environment so thoroughly that she is uncomfortable in direct sunlight and does most reading by candlelight even when she does not need to. She has documented the western anomaly in more detail than any other living person. She has concluded something she has not put in a report.',
        N'The head operator who has spent thirty-four years building toward a conclusion that no official channel will accept; her private assessment of the western anomaly is the most important document in the House.',
        N'No POV.',
        N'The Lacerta Chamber; cliff installation',
        158, 57, N'small and pale, worn to the dimensions of her work',
        N'white', N'perpetually slightly unkempt', N'short',
        N'pale gray', N'very pale — decades in the Chamber', N'dim-light pale, fine-lined',
        N'none',
        N'Absorbed and slightly absent from any room that is not the Chamber; moves through the apparatus housings with the ease of long memory',
        N'Operator attire, practical and dark; she does not own formal clothing that fits well anymore',
        N'none',
        N'She arrives at the Chamber before her staff. She leaves after them. She eats in the Chamber more often than not. She has not taken a leave period in nine years. She files quarterly reports to the Lord and keeps a private parallel set.',
        N'After thirty-four years she has concluded that the Lacerta Chamber is not observing the western anomaly. The anomaly is observing the Chamber. Something west of the Atlantic is aware of the apparatus and has been making deliberate contact attempts for at least eleven years, at increasing frequency. She has not put this in any official report. She wrote it in a private account she keeps in the Chamber itself. She showed this account to Lady Marisol the night before Marisol''s third visit to the apparatus. She has not spoken of Marisol''s visit to anyone since Marisol died.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'The Lacerta Chamber; cliff installation',
        N'0', N'0',
        N'elderly pale Iberian operator 62, white slightly unkempt hair, pale gray eyes, dim Scrying Chamber interior, surrounded by apparatus, completely absorbed expression, photorealistic fantasy-steampunk',
        N'A small pale woman of sixty-two, white hair slightly unkempt from long hours without attention to it, pale gray eyes from decades in dim light. Operator attire in a dimly lit stone Scrying Chamber, surrounded by complex apparatus, expression completely absorbed in what she is observing.',
        0, 0
    );
    PRINT 'Cándida Olaiz seeded.';
END
ELSE PRINT 'Cándida Olaiz already exists.';
GO

-- 27. Long-watch Operator Bernat Lledo
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Bernat Lledo')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Bernat Lledo', N'bernat-lledo', N'canon', 1,
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
        @id, N'Bernat Lledo', N'bernat-lledo', N'Bernat', N'Lledo', N'',
        N'human', N'human', N'male', N'he/him', 45, N'alive',
        N'Long-watch Scrying Operator; fourteen years at the apparatus; draws more long-watch shifts than any other operator; no longer certain where the watch ends and he begins',
        N'Bernat has been at the Lacerta Chamber for fourteen years and has drawn more long-watch shifts than any other operator in that time, partly because he is good at the work and partly because he does not resist the assignment the way other operators do. The long watch — eight to twelve hours at the apparatus in near-total darkness — is considered the most mentally taxing assignment on the installation. Bernat is not sure it is taxing. He is not entirely sure, at this point, where the watch ends and he begins. He sees patterns. He has been told to document those patterns and not interpret them. He documents them. He interprets them privately.',
        N'The operator whose long-watch immersion has blurred the line between observation and fixation; he is seeing something, and he may be the least reliable witness to what it is.',
        N'No POV.',
        N'The Lacerta Chamber; cliff installation',
        174, 69, N'slightly gaunt from irregular eating and long Chamber hours',
        N'dark brown', N'unwashed-looking', N'short',
        N'dark brown', N'Chamber-pale', N'dim-light pale, hollow around the eyes',
        N'none',
        N'The fixed-point attention of someone who has spent thousands of hours looking at the same thing; in any other room he is slightly absent',
        N'Operator attire; he eats when reminded',
        N'none',
        N'Long-watch shifts that run longer than scheduled; eats when reminded; sleeps between watches in the Chamber dormitory in short intervals; maintains a private pattern log he has not shared with Cándida',
        N'He has begun to receive what he can only describe as answers. When he settles into a long watch and holds his focus on the western anomaly without moving, without documenting, simply attending — there is a response. A pattern that organizes in reaction to his attention specifically. He cannot prove this. He is not certain he is not imagining it. He has told no one, including Cándida, because he is afraid of what she will say and more afraid she will say she already knows.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'The Lacerta Chamber; cliff installation',
        N'0', N'0',
        N'gaunt pale Iberian long-watch operator 45, dark unwashed hair, dark brown eyes, long-watch attire, dim Scrying Chamber interior, fixed intense attention on apparatus, photorealistic fantasy-steampunk',
        N'A slightly gaunt Iberian man of forty-five, dark hair not recently tended, dark brown eyes with the fixed quality of someone who has spent thousands of hours looking at the same thing. Sitting at apparatus in a dim stone Scrying Chamber, expression of intense attention that has no visible object in the room.',
        0, 0
    );
    PRINT 'Bernat Lledo seeded.';
END
ELSE PRINT 'Bernat Lledo already exists.';
GO

-- 28. Long-watch Operator Rosalía Garín
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Rosalía Garín')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Rosalía Garín', N'rosalia-garin', N'canon', 1,
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
        @id, N'Rosalía Garín', N'rosalia-garin', N'Rosalía', N'Garín', N'',
        N'human', N'human', N'female', N'she/her', 39, N'alive',
        N'Long-watch Scrying Operator; nine years at the Chamber; slept more than two hours at a stretch in only three of them; carrying an undocumented observation from her fourth year',
        N'Rosalía has been at the Lacerta Chamber for nine years and slept more than two hours at a stretch in only three of them. She does not discuss this. She presents at briefings, performs her duties, and files accurate reports. What happened during her fourth year at the apparatus — a watch shift in which she saw something the apparatus should not have been able to show — she has never documented. She has never spoken of it. She has accommodated a broken sleep architecture because the alternative is being alone with what she has not told anyone, and broken sleep is more manageable than that.',
        N'The operator carrying an undocumented observation that may be the most significant single data point in the western anomaly record.',
        N'No POV.',
        N'The Lacerta Chamber; cliff installation',
        165, 57, N'slight, worn by years of inadequate sleep',
        N'dark auburn', N'always tied back tightly', N'long',
        N'dark brown', N'pale from Chamber work', N'dark circles, hollowed by five years of poor sleep',
        N'none',
        N'Precise and contained during working hours; she has learned to manage the appearance of being well-rested; she takes cliff walks at night when she cannot sleep, which is most nights',
        N'Operator attire; she changed her watch shift preferences to avoid long watches after the fourth year',
        N'none',
        N'Shorter watch shifts by her own request; standard observation duties; methodical filing; leaves the installation for cliff walks when she cannot sleep',
        N'In her fourth year at the apparatus, during a night watch, the Chamber showed her something on the western projection that was not Sphere 31, not any world on record, and not consistent with any known membrane geography. It lasted eleven minutes. She screamed. The night supervisor came and the projection had resolved to the standard western scan. She wrote it up as an equipment calibration anomaly. She has never accounted for those eleven minutes, and she has spent five years afraid it will happen again.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'The Lacerta Chamber; cliff installation',
        N'0', N'0',
        N'pale exhausted Iberian female operator 39, dark auburn hair tied back tightly, dark circles, operator attire, cliff edge outside Chamber installation at night insomnia, looking west, not peaceful, photorealistic',
        N'An Iberian woman of thirty-nine, dark auburn hair tied back tightly, dark circles under dark brown eyes, operator attire. Standing at the cliff edge outside the Chamber installation at night, unable to sleep, looking west at the Atlantic with an expression that is not peaceful.',
        0, 0
    );
    PRINT 'Rosalía Garín seeded.';
END
ELSE PRINT 'Rosalía Garín already exists.';
GO

-- 29. Long-watch Operator Andreu Montoy
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Andreu Montoy')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Andreu Montoy', N'andreu-montoy', N'canon', 1,
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
        @id, N'Andreu Montoy', N'andreu-montoy', N'Andreu', N'Montoy', N'',
        N'human', N'human', N'male', N'he/him', 51, N'alive',
        N'Long-watch Scrying Operator; twenty-two years at the Chamber; believes the western anomaly is a naturally occurring membrane perforation; has not told anyone because he cannot prove it',
        N'Andreu has been at the Lacerta Chamber for twenty-two years. He is the middle generation of the current operator staff — past the period when the apparatus is overwhelming and before the long career''s full weight. He is competent, methodical, and has the Chamber''s operational protocols largely memorized. He is also, in the last four years, increasingly certain that what the apparatus shows to the west is not the surface of an alternate Earth but a place where the membrane has thinned to near-transparency, and what lies beyond it is not another world but the space between worlds.',
        N'The operator whose theoretical framework for the western anomaly may be the most accurate model anyone has produced; he has not shared it because he cannot prove it and because he is afraid of what the Liturgy would do if it were true.',
        N'No POV.',
        N'The Lacerta Chamber; cliff installation',
        177, 74, N'lean and slightly distracted-looking, the build of someone who walks a great deal in thought',
        N'gray', N'shaggy', N'medium',
        N'blue-gray', N'pale from Chamber work', N'thoughtful lines, not weather-marked',
        N'none',
        N'Not quite present in any room that is not the Chamber; tends to stop mid-motion when thinking',
        N'Operator attire; keeps a private theoretical journal in his quarters that he does not bring to the Chamber',
        N'none',
        N'Standard watch rotations; calibration reviews; assists with technical documentation; teaches new operators observation protocols; private journal writing in evenings',
        N'He believes the western anomaly is a naturally occurring membrane perforation — not an engineered observation point but a place where the fabric between worlds is structurally compromised and accelerating. He also believes that if the Liturgy understands what it is, the Liturgy will try to use it, and that using it will make the perforation catastrophically worse. He has not told anyone because he has no proof and because he is afraid of how that fear sounds when spoken aloud.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'The Lacerta Chamber; cliff installation',
        N'0', N'0',
        N'pale middle-aged Iberian man 51, gray shaggy hair, blue-gray eyes, Chamber operator attire, Scrying apparatus interior, not looking at apparatus but at middle distance, theoretical expression, photorealistic fantasy-steampunk',
        N'A pale Iberian man of fifty-one, gray shaggy hair, blue-gray eyes, operator attire. In a Scrying Chamber interior, not looking at the apparatus but at a point in the middle distance, working through a theory he cannot quite disprove.',
        0, 0
    );
    PRINT 'Andreu Montoy seeded.';
END
ELSE PRINT 'Andreu Montoy already exists.';
GO

-- 30. Technical Maintenance Chief Gorka Arrieta
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Gorka Arrieta')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Gorka Arrieta', N'gorka-arrieta', N'canon', 1,
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
        @id, N'Gorka Arrieta', N'gorka-arrieta', N'Gorka', N'Arrieta', N'',
        N'human', N'human', N'male', N'he/him', 44, N'alive',
        N'Technical Maintenance Chief; keeps the Lacerta Chamber apparatus running; has been making unauthorized modifications for six years that no one knows about',
        N'Gorka keeps the Lacerta Chamber apparatus running. This is the entire scope of his official responsibility and it has taken most of his waking hours for seventeen years. He does not observe through the apparatus. He understands the mechanics of how it works better than anyone alive, because he has been repairing and modifying it for seventeen years and the original installation team is mostly dead. He has made a series of unauthorized modifications over the last six years that have substantially improved the apparatus''s range and resolution. He has not told anyone.',
        N'The maintenance chief whose unauthorized modifications created the conditions that allow the western anomaly to be as visible as it currently is; the apparatus is not the same apparatus it was when the anomaly was first observed.',
        N'No POV.',
        N'The Lacerta Chamber; maintenance tunnels; cliff installation',
        180, 84, N'solid and practical, built for physical work in confined spaces',
        N'black with gray at temples', N'practical short cut', N'short',
        N'dark brown', N'olive', N'hands permanently stained with maintenance compounds',
        N'none',
        N'Practical and direct; he is most at ease inside a piece of equipment he is fixing; formal settings make him uncomfortable',
        N'Maintenance attire; he changes into something cleaner for briefings and changes back immediately after',
        N'none',
        N'Dawn to dusk in the maintenance tunnels and equipment housings; weekly operational status review with Cándida; quarterly equipment report to the Commander; keeps a private log of every modification he has made',
        N'The apparatus''s current range and resolution are products of six years of unauthorized modifications he made because he was curious about what the western scan was showing and wanted a cleaner image. He did not know the modifications would produce what they produced. He is now afraid to tell anyone because the unauthorized work is a protocol breach, because the improved apparatus may be the reason the anomaly''s apparent frequency has increased — he suspects it was always at that frequency and they simply could not see it — and because he does not know how to undo what he has done without degrading the system entirely.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'The Lacerta Chamber; maintenance tunnels',
        N'0', N'0',
        N'Iberian maintenance chief 44, black hair gray temples, dark brown eyes, olive skin stained hands, maintenance attire, equipment tunnels inside Lacerta Chamber, focused practical expression, photorealistic fantasy-steampunk',
        N'An Iberian man of forty-four, black hair going gray at the temples, dark brown eyes, hands stained from maintenance compounds. Crouching in an equipment tunnel inside the Lacerta Chamber installation, working on a piece of apparatus with focused practical attention.',
        0, 0
    );
    PRINT 'Gorka Arrieta seeded.';
END
ELSE PRINT 'Gorka Arrieta already exists.';
GO

-- 31. New Operator Aina Morell
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Aina Morell')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Aina Morell', N'aina-morell', N'canon', 1,
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
        @id, N'Aina Morell', N'aina-morell', N'Aina', N'Morell', N'',
        N'human', N'human', N'female', N'she/her', 22, N'alive',
        N'New Scrying Operator; seven months at the Lacerta Chamber; the most promising trainee Cándida has seen in a decade; has been observing anomaly contact-states since her first week',
        N'Aina has been a Lacerta Chamber operator for seven months. She trained for four months before that. She is precise, attentive, and has a natural aptitude for the long-focus work that usually takes years to develop. Cándida Olaiz has said she is the most promising trainee she has seen in a decade. Aina is quietly pleased by this and has not told Cándida what she saw in her first week at the apparatus, before she knew what she was seeing or whether it was standard. She has been waiting to see if it happens again. It has happened three times since.',
        N'The newest operator who has been observing the anomaly''s contact-state since her first week; she lacks the context to know how unusual this is, which may make her the most accurate reporter of it.',
        N'No POV.',
        N'The Lacerta Chamber; cliff installation',
        161, 53, N'slight and precise, still sun-colored from before the Chamber work faded it',
        N'dark brown', N'short', N'short',
        N'dark brown', N'olive', N'still warm-toned — not yet Chamber-pale',
        N'none',
        N'Quietly intent; she works through her lunch period without appearing to notice; asks Gorka technical questions he finds surprisingly specific',
        N'Operator training attire; still has the posture of someone not yet fully shaped by the Chamber''s rhythms',
        N'none',
        N'Standard operator training rotations; observation documentation; calibration checks; consistently works through her lunch period; asks technical questions; keeps a private log at home',
        N'In her first week at the apparatus, during a routine scan, the western projection organized into something that looked — for approximately forty seconds — like a response to her presence specifically. Not to the apparatus. To her. It changed when she leaned forward and resolved when she sat back. She has not told Cándida because she does not know how to say it without sounding like a new operator''s overinterpretation. The three subsequent incidents are documented in a private log she keeps at home, not at the Chamber.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'The Lacerta Chamber; cliff installation',
        N'0', N'0',
        N'young Iberian operator 22, short dark brown hair, dark brown eyes, olive skin still sun-warm, training operator attire, Lacerta Chamber apparatus, quietly intent expression, photorealistic fantasy-steampunk',
        N'A young Iberian woman of twenty-two, short dark hair, dark brown eyes, skin still olive and warm before the Chamber work fades it. Sitting at the Scrying apparatus in training attire, expression quietly intent, watching something in the western projection she has not yet decided how to describe.',
        0, 0
    );
    PRINT 'Aina Morell seeded.';
END
ELSE PRINT 'Aina Morell already exists.';
GO

-- ============================================================
-- SECTION 5: DOMESTIC STAFF (21)
-- ============================================================

-- 32. Seneschal Custodio Frades
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Custodio Frades')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Custodio Frades', N'custodio-frades', N'canon', 1,
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
        @id, N'Custodio Frades', N'custodio-frades', N'Custodio', N'Frades', N'Master',
        N'human', N'human', N'male', N'he/him', 61, N'alive',
        N'Seneschal of House Lacerta; twenty-nine years managing the household; has been here longer than the current Lord; diverts a small sum each quarter for a relative who cannot work',
        N'Custodio has managed the House Lacerta household for twenty-nine years and has seen three Lords in that time, of whom he considers the current one the best and the most difficult. He manages all accounts, provisioning, staff assignments, and the logistical machinery that keeps a Great House operational. He has memorized the preferences, allergies, and working habits of everyone in the House down to the junior kitchen staff. He also, quietly and without anyone''s knowledge, diverts a small amount from the household accounts each quarter to support a relative who cannot work.',
        N'The household''s central nervous system; his private fund diversion is the smallest and most forgivable secret in the estate and the one he is most ashamed of.',
        N'No POV.',
        N'House Lacerta estate; all household areas',
        172, 76, N'upright and trim from decades of formal household management',
        N'white', N'neatly trimmed', N'short',
        N'warm brown', N'olive, indoor-pale with age', N'composed, age-lined',
        N'none',
        N'Precise and attentive; the estate runs through him and he through it; rarely seen at rest',
        N'Formal household management attire in House colors; always correct to the occasion',
        N'none',
        N'Pre-dawn accounts review; morning staff briefings; supply coordination; formal occasion planning; late evening accounts reconciliation',
        N'His younger sister was injured in a border skirmish twelve years ago and cannot work. He has been sending her a small quarterly payment drawn from rounding errors in the household accounts — amounts so small the Treasurer''s review has never flagged them, but which have accumulated to a figure that, specifically audited, would constitute theft. He has been doing this for eleven years. He tells himself he will find another way. He has not found another way.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Lacerta estate; all household areas',
        N'0', N'0',
        N'dignified Iberian seneschal 61, white neatly trimmed hair, warm brown eyes, formal household management attire, estate main corridor morning light, reviewing provisioning list, photorealistic',
        N'A dignified Iberian man of sixty-one, white hair neatly trimmed, warm brown eyes, formal household management attire. Standing in the estate main corridor in early morning light, reviewing a provisioning list with the ease of someone who has run this household for three decades.',
        0, 0
    );
    PRINT 'Custodio Frades seeded.';
END
ELSE PRINT 'Custodio Frades already exists.';
GO

-- 33. Head Cook Remedios Parés
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Remedios Parés')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Remedios Parés', N'remedios-pares', N'canon', 1,
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
        @id, N'Remedios Parés', N'remedios-pares', N'Remedios', N'Parés', N'',
        N'human', N'human', N'female', N'she/her', 58, N'alive',
        N'Head Cook; thirty-five years at this House; cooked for three Lords; does not think the current one is half as good as his wife was; knows something about how that wife died',
        N'Remedios has cooked for this House for thirty-five years. She cooked for Lord Rodrigo''s father, she cooked through two campaigns, she cooked the meal that Marisol ate on the morning of the day she died. She knew Lady Marisol better than most of the House''s political staff did, because Marisol visited the kitchen and understood that the cook is the person in a household who knows everything because everyone passes through a kitchen. Remedios does not think the current Lord is half the person Marisol was. She is not wrong to think this. She does not say it.',
        N'The keeper of thirty-five years of household memory, including details about Marisol''s death that no one has thought to ask her about.',
        N'No POV.',
        N'House Lacerta kitchen; estate provisioning range',
        159, 72, N'substantial, built from thirty-five years of kitchen labor',
        N'gray-streaked dark', N'always covered while working', N'medium',
        N'dark brown', N'olive, flushed from kitchen heat', N'kitchen-warm, full-faced',
        N'none',
        N'Commands the kitchen the way a general commands a field; she does not raise her voice and does not need to',
        N'Kitchen working attire; a clean apron is her equivalent of formal dress',
        N'none',
        N'In the kitchen before dawn; daily menu planning and execution; twice-weekly procurement review with the Seneschal; the kitchen is her domain entirely and no one, including the Seneschal, tells her how to run it',
        N'She prepared the herbal compounds that the House Physician prescribed for Marisol in her final illness. The dosage in the Physician''s written order did not match what she was given to prepare. She noticed. She prepared what she was given and did not say anything because she did not know if the discrepancy was error or something else, and because naming the Physician would destroy a person she was not certain deserved to be destroyed. She has thought about it every day for eight years.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Lacerta kitchen and estate',
        N'0', N'0',
        N'Iberian head cook 58, gray-streaked hair covered by working cloth, dark brown eyes, substantial build, large estate kitchen at dawn, commanding authority, photorealistic',
        N'An Iberian woman of fifty-eight, gray-streaked hair covered by a working cloth, dark brown eyes, substantial build. Standing in the center of a large stone estate kitchen before dawn, hands ready, the authority of someone who has commanded this room for thirty-five years.',
        0, 0
    );
    PRINT 'Remedios Parés seeded.';
END
ELSE PRINT 'Remedios Parés already exists.';
GO

-- 34. Sous-chef Pau Giralt
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Pau Giralt')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Pau Giralt', N'pau-giralt', N'canon', 1,
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
        @id, N'Pau Giralt', N'pau-giralt', N'Pau', N'Giralt', N'',
        N'human', N'human', N'male', N'he/him', 34, N'alive',
        N'Sous-chef; Remedios''s second in the kitchen; grew up in the lower estate household; has been receiving letters from a man who claims to be his father and is a member of House Fornax',
        N'Pau is Remedios''s second in the kitchen, a position he was given three years ago after the previous sous-chef married out. He is genuinely talented and has learned to read Remedios''s preferences well enough that the kitchen runs smoothly even though she is not an easy person to work with. He grew up in House Lacerta''s lower estate household and has been in the kitchen since his early teens. He has recently received a series of letters from a man who claims to be his biological father — and who is, according to the letters, a member of House Fornax.',
        N'The kitchen second whose parentage question, if real, makes him an unwitting intelligence asset for any House that decides to use it.',
        N'No POV.',
        N'House Lacerta kitchen',
        175, 73, N'lean and kitchen-capable',
        N'dark brown', N'curly, kept somewhat neat', N'short',
        N'dark brown', N'olive, kitchen-warm', N'clear, youthful',
        N'none',
        N'Easy and capable in motion through the kitchen; slightly distracted since the letters began arriving',
        N'Kitchen attire; he has no formal dress and borrows it for required occasions',
        N'none',
        N'In the kitchen from before dawn; manages afternoon preparations and junior staff during Remedios''s rest period; reads his letters from Fornax late at night in his quarters',
        N'He doesn''t know if the man writing from House Fornax is genuinely his father. He was told his father was dead. The letters are detailed enough to be credible and specific enough to be alarming. He has not responded to the most recent three. He has also not destroyed them. He does not know if someone in this House is aware he is receiving them, or whether the letters arriving through the normal correspondence route is itself significant.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Lacerta kitchen',
        N'0', N'0',
        N'young Iberian sous-chef 34, dark curly brown hair, dark brown eyes, kitchen attire, estate kitchen afternoon, capable easy movement, slightly distracted expression, photorealistic',
        N'A young Iberian man of thirty-four, dark curly brown hair, dark brown eyes, kitchen attire. Working at a prep surface with the ease of long practice, expression not entirely in the kitchen.',
        0, 0
    );
    PRINT 'Pau Giralt seeded.';
END
ELSE PRINT 'Pau Giralt already exists.';
GO

-- 35. Kitchen Assistant Maisie Colbrook (Sphere 31)
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Maisie Colbrook')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Maisie Colbrook', N'maisie-colbrook', N'canon', 1,
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
        @id, N'Maisie Colbrook', N'maisie-colbrook', N'Maisie', N'Colbrook', N'',
        N'human', N'human', N'female', N'she/her', 19, N'alive',
        N'Kitchen assistant; taken from Sphere 31 three years ago; has made herself invisible in the kitchen; does not know she looks almost exactly like the portrait of Lady Marisol in the main hall',
        N'Maisie was taken from Sphere 31 three years ago and placed in the Lacerta estate kitchen by Liturgy arrangement. She does not know the mechanism of how she arrived. She knows she was in one place and then she was somewhere else, and the somewhere else has been the kitchen for three years. She does not speak of Sphere 31 because the people she has tried to speak of it to have not known what to say. She is quiet, capable, and has learned the kitchen''s rhythms with the thoroughness of someone who has decided this is where she lives now. She does not know she looks almost exactly like the portrait in the main hall.',
        N'The Sphere 31 arrival whose physical resemblance to the dead Lady Marisol is either coincidence or the most deliberate thing the Liturgy has done; her presence in the House is unexplained.',
        N'No POV.',
        N'Sphere 31 (Earth); House Lacerta kitchen (current)',
        164, 55, N'slight, quiet in her body',
        N'dark chestnut', N'loose', N'medium',
        N'amber-brown', N'warm olive', N'clear, young',
        N'none',
        N'Quiet and contained; she has made herself invisible in the household and the household has absorbed her the way estates absorb things given to them',
        N'Kitchen working attire; whatever the estate provides',
        N'none',
        N'Kitchen duties from early morning to mid-afternoon; she developed a habit of walking the estate grounds after the main work is done; she is learning to read the Cauld''s written language',
        N'Three weeks ago she walked into the main hall to deliver something and saw the portrait. She stopped. She stood there for a long time. She has not walked through the main hall since. She has not told anyone about that moment and does not have the social standing in this House to know who she could tell, or what telling would do.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Lacerta kitchen and estate grounds',
        N'0', N'0',
        N'young woman from Sphere 31, 19, dark chestnut hair loose, amber-brown eyes, warm olive skin, estate kitchen attire, Cauld fantasy-steampunk interior, quiet present expression, resemblance to a noblewoman she does not know she shares, photorealistic',
        N'A young woman of nineteen, dark chestnut hair worn loose, amber-brown eyes, warm olive skin, estate kitchen attire. Expression quiet and present — someone who has decided to be here because here is what she has. She looks, unmistakably, like the woman in the portrait in the hall.',
        0, 0
    );
    PRINT 'Maisie Colbrook seeded.';
END
ELSE PRINT 'Maisie Colbrook already exists.';
GO

-- 36. Butler Cristóbal Aldaz
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Cristóbal Aldaz')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Cristóbal Aldaz', N'cristobal-aldaz', N'canon', 1,
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
        @id, N'Cristóbal Aldaz', N'cristobal-aldaz', N'Cristóbal', N'Aldaz', N'',
        N'human', N'human', N'male', N'he/him', 64, N'alive',
        N'Butler of House Lacerta; thirty-one years managing the serving staff and formal occasions; has been carrying a secret about Lord Rodrigo for twenty-two years',
        N'Cristóbal has been the House Lacerta butler for thirty-one years, through three Lords and what he privately estimates as forty-seven formal occasions that required him to carry the House''s dignity entirely on his posture while the people he served fell apart around him. He manages the serving staff without visible effort, coordinates every formal occasion with a precision that makes improvisation invisible, and has never once in thirty-one years looked less than immaculate during service. He has also been carrying a secret for twenty-two years that would, if spoken, destroy the current Lord and possibly the House, and he has chosen never to speak it.',
        N'The three-generation butler whose secret is the House''s most dangerous kept confidence; his silence is a kind of loyalty that costs him something every day.',
        N'No POV.',
        N'House Lacerta estate; formal reception areas',
        178, 75, N'upright and precise, the posture of someone who has carried a House''s dignity for thirty-one years',
        N'white', N'impeccably kept', N'short',
        N'pale brown', N'olive, carefully maintained', N'composed, age-precise',
        N'none',
        N'Perfect posture at all times; he moves through formal occasions as if choreographed; in private he allows himself to sit',
        N'Formal butler attire in House colors; always impeccable; a crease on his jacket would register as personal failure',
        N'none',
        N'Manages morning staff assignments; oversees all formal occasions from planning through execution; liaison between household staff and the ruling family; never retires before the last family member has retired',
        N'Twenty-two years ago he witnessed the then-thirty-six-year-old Rodrigo strike a man in uncontrolled rage — a minor official who had said something about Marisol. The man fell, hit the stone floor, and did not get up. Rodrigo paid to have it recorded as a fall from a horse. Cristóbal was the only witness. He has served Rodrigo faithfully ever since — not out of complicity but out of something that might be respect. Rodrigo has never, before or since, lost control in that way, and has spent twenty-two years being the man that incident might have foreclosed. Cristóbal believes this matters. He is not certain it does.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Lacerta estate; formal reception areas',
        N'0', N'0',
        N'impeccable elderly Iberian butler 64, white hair precisely kept, pale brown eyes, formal service attire, estate formal dining room, perfect posture, expression of practiced discretion carrying weight, photorealistic',
        N'An Iberian man of sixty-four, white hair impeccably kept, pale brown eyes, formal butler attire in perfect order. Standing in a formal estate dining room with the posture of someone who has carried this household''s dignity for thirty-one years, expression of practiced discretion, nothing visible of the weight behind it.',
        0, 0
    );
    PRINT 'Cristóbal Aldaz seeded.';
END
ELSE PRINT 'Cristóbal Aldaz already exists.';
GO

-- 37. Head Housekeeper Consuelo Eguibar
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Consuelo Eguibar')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Consuelo Eguibar', N'consuelo-eguibar', N'canon', 1,
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
        @id, N'Consuelo Eguibar', N'consuelo-eguibar', N'Consuelo', N'Eguibar', N'Mistress',
        N'human', N'human', N'female', N'she/her', 55, N'alive',
        N'Head Housekeeper; twenty years managing the estate''s physical fabric; exacting and fair with her staff; practices a banned devotional sect privately in her quarters',
        N'Consuelo manages the household''s cleaning staff, linen, and the material condition of the estate — the state of the furnishings, the readiness of guest quarters at short notice. She has been doing this for twenty years and has the estate''s physical state memorized to the level of which floorboard creaks and which window latch sticks. She is exacting and fair, with a genuine warmth toward the junior maids that she does not extend to anyone above her in the household hierarchy. She attends religious observances privately, in her quarters, that are not the Bheur rites the House formally recognizes.',
        N'The domestic infrastructure manager whose personal religious practice, if known, would remove her from the House; her faith and her loyalty occupy the same quiet space.',
        N'No POV.',
        N'House Lacerta estate; all household rooms',
        162, 64, N'compact and purposeful',
        N'dark brown going gray', N'tight bun', N'medium',
        N'dark brown', N'olive', N'twenty years of indoor work',
        N'none',
        N'Moves through the estate with the ease of long memorization; inspects rooms with focused practical attention',
        N'Household management attire; always clean and correct',
        N'none',
        N'Dawn inspection of all rooms used by the family; morning staff assignments; weekly linen review; coordinating guest room preparation for diplomatic visits; evening prayers conducted privately',
        N'She is a practicing member of a banned devotional sect that venerates Sphere 31 as a kind of parallel continuation — a place where the dead of this world go on living, observed but unobservable. The sect has been outlawed by the Liturgy for forty years. She is not the only member in the House. She does not know who else is. She has never sought to find out because knowing would make her responsible for them.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Lacerta estate; all household rooms',
        N'0', N'0',
        N'Iberian head housekeeper 55, dark going-gray hair tight bun, dark brown eyes, household management attire, estate corridor morning inspection, precise exacting movement, photorealistic',
        N'An Iberian woman of fifty-five, dark hair going gray in a tight bun, dark brown eyes, household management attire. Moving through an estate corridor in morning light, inspecting a room''s condition with the focused attention of someone who has memorized every imperfection in this building.',
        0, 0
    );
    PRINT 'Consuelo Eguibar seeded.';
END
ELSE PRINT 'Consuelo Eguibar already exists.';
GO

-- 38. Household Maid Maite Izarra (longtime)
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Maite Izarra')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Maite Izarra', N'maite-izarra', N'canon', 1,
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
        @id, N'Maite Izarra', N'maite-izarra', N'Maite', N'Izarra', N'',
        N'human', N'human', N'female', N'she/her', 41, N'alive',
        N'Senior household maid; in the estate since age twelve; knows every room''s preferences and every person''s routine; has been in love with the Seneschal for nineteen years',
        N'Maite has been in the household since she was twelve and remembers the estate under Lord Rodrigo''s father. She is the senior maid by tenure and is recognized as such by the staff in informal ways the official hierarchy does not document. She knows which rooms have the bad damp smell in winter, knows that Lady Catalina does not like the morning curtains drawn before she rings for it, knows that the Seneschal''s afternoon tea is a ritual that cannot be disrupted. She has been in love with the Seneschal for nineteen years. She has never once said this.',
        N'The longest-serving maid whose unremarked love is both the most private thing in the household and the emotional ground for a loyalty that has never been tested.',
        N'No POV.',
        N'House Lacerta estate; household areas',
        160, 58, N'compact and practiced, moves through the estate without visible effort',
        N'chestnut', N'braided', N'long',
        N'brown', N'olive', N'indoor-warm, marked by years of domestic labor',
        N'none',
        N'Familiar and efficient in the estate; she has cleaned these rooms for nearly thirty years and her body knows them',
        N'House livery in dark green; worn with the ease of a second skin',
        N'none',
        N'Standard maid duties with the authority of a senior; manages junior maids'' morning assignments in practice if not in title',
        N'She has been in love with Custodio Frades for nineteen years and has told no one. She also worked out his fund diversion for his sister years ago — not from the accounts but from his behavior, from what he does when the quarterly accounts are finished. She has never told anyone. Not because she approves of the theft but because it is not her secret to tell. And because the day she tells it is the day the thread that connects them is cut.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Lacerta estate; household areas',
        N'0', N'0',
        N'Iberian senior maid 41, chestnut braided hair, brown eyes, House livery dark green, estate interior morning, familiar easy movement, expression carrying something she does not show, photorealistic',
        N'An Iberian woman of forty-one, chestnut hair in a braid, brown eyes, House livery. Moving through an estate interior with the ease of someone who has cleaned these rooms for nearly thirty years, expression doing the quiet work of keeping something to herself.',
        0, 0
    );
    PRINT 'Maite Izarra seeded.';
END
ELSE PRINT 'Maite Izarra already exists.';
GO

-- 39. Household Maid Dera Carvallo (recent arrival, Pallor intelligence asset)
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Dera Carvallo')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Dera Carvallo', N'dera-carvallo', N'canon', 1,
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
        @id, N'Dera Carvallo', N'dera-carvallo', N'Dera', N'Carvallo', N'',
        N'human', N'human', N'female', N'she/her', 23, N'alive',
        N'Household maid; six months in the estate; placed through a trade contact vouching system; House Pallor intelligence asset; has found something more interesting than she was sent to find',
        N'Dera arrived six months ago and was taken on by the Housekeeper on the recommendation of a mid-level trade contact. She is competent, pleasant, and speaks with an accent she attributes to the eastern estates. Her green eyes are unusual for Lacerta but not remarked upon. She is a House Pallor intelligence asset, placed through the same trade network that supplied the vouching contact. She is gathering information at the rate the household allows — which is quite a lot.',
        N'The intelligence maid whose handlers have not told her what specifically they want, which means she is collecting everything and will eventually collect the wrong thing.',
        N'No POV.',
        N'House Lacerta estate (cover); House Pallor intelligence network (actual)',
        167, 62, N'lean and capable; she has traveled extensively and it shows in how she carries herself',
        N'dark brown', N'natural is red-brown — she has dyed it for cover', N'medium',
        N'green', N'olive, sun-darkened from travel', N'clear but travel-marked',
        N'none',
        N'Pleasant and unhurried; she has learned to be invisible in a household that does not look at its staff',
        N'House livery; she wears it correctly and without discomfort, which took practice',
        N'none',
        N'Standard maid duties; has positioned herself to cover the guest quarters and secondary sitting rooms adjacent to the Chancellor''s office; reports to handlers through coded correspondence via the trade post',
        N'Her handlers told her to focus on the Chancellor''s correspondence. What she actually found interesting is the operator Bernat Lledo''s pattern log, which she saw left out in the Chamber dormitory and read enough of to understand it is significant without understanding what it means. She has not reported it yet because she is trying to understand what she is reporting before she sends it. She is running out of time on the delay.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Lacerta estate (cover)',
        N'0', N'0',
        N'young woman intelligence asset, 23, dark brown dyed hair green eyes unusual for Lacerta, olive sun-darkened skin, House livery, estate secondary corridor, pleasant helpful expression concealing intent, photorealistic',
        N'A young woman of twenty-three, dark brown hair and green eyes that don''t quite match the Iberian look she is otherwise presenting, House livery. In a secondary estate corridor, expression pleasant and unhurried, the posture of someone who has learned to be invisible.',
        0, 0
    );
    PRINT 'Dera Carvallo seeded.';
END
ELSE PRINT 'Dera Carvallo already exists.';
GO

-- 40. Household Maid Soledad Arrés (listens carefully)
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Soledad Arrés')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Soledad Arrés', N'soledad-arres', N'canon', 1,
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
        @id, N'Soledad Arrés', N'soledad-arres', N'Soledad', N'Arrés', N'',
        N'human', N'human', N'female', N'she/her', 31, N'alive',
        N'Household maid; eight years in the estate; noticed by her senior for listening too carefully; has been composing written accounts of House conversations for seven years for no handler and no purpose she can name',
        N'Soledad has been in the household for eight years. She does her work carefully, keeps her own company, and is noticed mainly in her absence. She has been described by the Housekeeper as reliable and by the senior maid Maite as someone who listens a bit too carefully, which Maite has never explained. Soledad listens carefully because she has been, for the past seven years, composing written accounts of what she hears in this household — not for any employer, not for any House, but for herself, in a locked box under her bed, for reasons she has never been able to fully articulate.',
        N'The maid who has been archiving the House''s private conversations for seven years without purpose or handler; the most comprehensive and most purposeless intelligence record in the estate.',
        N'No POV.',
        N'House Lacerta estate; private family corridor',
        161, 59, N'slight and unremarkable, which is her primary professional quality',
        N'dark brown', N'unremarkable' , N'medium',
        N'very dark brown', N'olive', N'clear, indoor-marked',
        N'none',
        N'Very still when listening; she has made herself invisible and uses that invisibility',
        N'House livery; she wears it without distinction',
        N'none',
        N'Standard maid duties; assigned primarily to the family''s private corridor; writes in the evenings in her quarters for an hour or two; sleeps well; does not appear troubled by what she writes',
        N'She has been writing down what she hears for seven years. The locked box contains approximately four hundred pages of documented conversations, observations, and events — including, without her understanding their significance, details of Marisol''s last weeks and at least two conversations between Lord Rodrigo and Cándida Olaiz that have never appeared in any official record. She has not read back through most of what she''s written. She does not know why she does it. She does not know what to do with it.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Lacerta estate; private family corridor',
        N'0', N'0',
        N'unremarkable Iberian household maid 31, dark brown hair, dark brown eyes, House livery, estate private corridor, very still listening posture, photorealistic',
        N'An Iberian woman of thirty-one, dark brown hair, dark brown eyes, House livery. Standing in a private estate corridor, very still, expression of someone who has made themselves invisible and is using that invisibility.',
        0, 0
    );
    PRINT 'Soledad Arrés seeded.';
END
ELSE PRINT 'Soledad Arrés already exists.';
GO

-- 41. Lady's Personal Attendant Begoña Uriguen
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Begoña Uriguen')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Begoña Uriguen', N'begona-uriguen', N'canon', 1,
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
        @id, N'Begoña Uriguen', N'begona-uriguen', N'Begoña', N'Uriguen', N'',
        N'human', N'human', N'female', N'she/her', 35, N'alive',
        N'Personal attendant to Lady Catalina; six years managing the heir''s schedule and correspondence; in love with Catalina; Catalina knows; neither of them has addressed it',
        N'Begoña attends Lady Catalina as personal attendant — she manages the heir''s clothing, schedule, correspondence routing, and personal needs. She has done this for six years and knows Catalina''s working habits better than anyone else in the House. She is discreet, thorough, and maintains her composure under conditions that regularly require her to absorb the consequences of Catalina''s political frustration. She has been in love with Catalina for six years. Catalina knows. Neither of them has addressed this. They continue to work together in a state of acknowledged but unspoken mutual understanding that is the most delicately maintained arrangement in the estate.',
        N'The attendant whose personal situation is both the House''s least likely destabilizing factor and, in the event of Catalina''s forced political marriage, its most certain one.',
        N'No POV.',
        N'House Lacerta estate; Catalina''s quarters and schedule range',
        163, 60, N'neat and contained, the posture of someone who manages a demanding person with care',
        N'dark brown', N'neatly pinned', N'medium',
        N'dark brown', N'warm olive', N'composed, indoor-careful',
        N'none',
        N'Composed and attentive; she has learned to be exactly as present as Catalina needs and exactly as invisible otherwise',
        N'Formal attendant attire in House colors; always appropriate, never competing',
        N'none',
        N'Catalina''s schedule is Begoña''s schedule; she routes correspondence, manages personal appointments, accompanies to formal functions; sleeps in a room adjoining Catalina''s quarters',
        N'Catalina showed her the Chamber logs two years ago — handed them over without a word and let her read. Begoña is the only person outside the Chamber staff who knows the full scope of the western anomaly documentation. She has told no one. She does not know whether Catalina showed her because she trusts her, or because she needed a witness who would keep the secret, or because she wanted company in knowing it. She has not asked. The answer might change something she is not ready to change.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Lacerta estate; Catalina''s range',
        N'0', N'0',
        N'Iberian lady''s attendant 35, dark brown hair neatly pinned, dark brown eyes, warm olive skin, formal attendant attire, estate private sitting room, composed careful expression, photorealistic',
        N'An Iberian woman of thirty-five, dark brown hair neatly pinned, dark brown eyes, formal attendant attire. In an estate private sitting room, adjusting something for the morning''s occasion, expression composed and careful, containing what it contains.',
        0, 0
    );
    PRINT 'Begoña Uriguen seeded.';
END
ELSE PRINT 'Begoña Uriguen already exists.';
GO

-- 42. Stable Master Gaspar Mendi
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Gaspar Mendi')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Gaspar Mendi', N'gaspar-mendi', N'canon', 1,
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
        @id, N'Gaspar Mendi', N'gaspar-mendi', N'Gaspar', N'Mendi', N'',
        N'human', N'human', N'male', N'he/him', 57, N'alive',
        N'Stable Master; twenty-three years managing the estate horses; knows every departure from the estate by horse or foot; has been tracking a monthly visitor who leaves by the eastern cliff path',
        N'Gaspar has managed the House Lacerta stables for twenty-three years. He knows every horse in the estate, their temperaments and conditions. He is also, by consequence, the person who knows when someone leaves the estate — because anyone leaving by horse requires a horse, and all horses go through Gaspar. He also knows who leaves without a horse. He has been tracking one specific person who leaves the estate on foot without horses at roughly the same time each new moon. He has not identified them. He is getting closer.',
        N'The stable master whose systematic knowledge of departures is the House''s most passive but most reliable surveillance mechanism; whoever leaves monthly has not accounted for him.',
        N'No POV.',
        N'House Lacerta stables; estate grounds range',
        176, 83, N'solid and practical, built from twenty-three years of outdoor physical work',
        N'dark gray', N'practical short cut', N'short',
        N'brown', N'deeply weathered olive', N'sun and wind-cut, outdoor-marked',
        N'none',
        N'Watchful and practical; at ease with horses in a way he is not at ease with most people',
        N'Stable working attire; weather-worn and functional',
        N'none',
        N'Dawn stable rounds; horse care and training oversight; ride preparation; late evening stable check before night grooms take over; keeps a private log of every horse taken and every departure',
        N'He has been tracking the monthly foot departures for four months. The person leaves from the eastern cliff path — accessible only to someone who knows the estate''s terrain — and returns before dawn. Two months ago he found a piece of fabric on the path: dark green Corps wool, officer-grade. He has not yet acted on this. He is deciding whether the information belongs to the Lord, the Commander, or no one yet.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Lacerta stables; estate grounds',
        N'0', N'0',
        N'weathered Iberian stable master 57, dark gray hair, brown eyes, stable work attire, estate stables at dawn, hand on horse flank, practical watchful expression, photorealistic',
        N'A weathered Iberian man of fifty-seven, dark gray hair, brown eyes, stable work attire. Hand on a horse''s flank in the estate stables at dawn, expression practical and watchful — a man who pays attention to what leaves and what comes back.',
        0, 0
    );
    PRINT 'Gaspar Mendi seeded.';
END
ELSE PRINT 'Gaspar Mendi already exists.';
GO

-- 43. Groom Ander Zuloaga
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ander Zuloaga')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ander Zuloaga', N'ander-zuloaga', N'canon', 1,
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
        @id, N'Ander Zuloaga', N'ander-zuloaga', N'Ander', N'Zuloaga', N'',
        N'human', N'human', N'male', N'he/him', 28, N'alive',
        N'Groom; four years in the estate stables; sold the Lord''s travel schedule once to pay a debt; has been waiting for consequences for fourteen months',
        N'Ander has been a groom at House Lacerta for four years and is competent with horses, personable with the other stable staff, and has been living since last year in a specific variety of private terror. He sold the Lord''s travel schedule once — to a contact he was told represented a commercial interest — and was paid in cash, which he needed for a debt. He has not been contacted again. Nothing happened that he could trace to what he did. The silence has not reassured him. He works with the focused attention of a person who has decided that being good at his job is the only thing he can control.',
        N'The groom whose single act of information-selling may have no consequences or may have consequences that have simply not arrived yet.',
        N'No POV.',
        N'House Lacerta stables',
        174, 76, N'lean and practical, outdoor-built',
        N'dark brown', N'practical cut', N'short',
        N'brown', N'olive', N'outdoor-marked',
        N'none',
        N'Focused and attentive; he works harder than he needs to and it shows',
        N'Stable working attire',
        N'none',
        N'Standard groom duties; horse care, tack maintenance, preparation of mounts; works closely with Gaspar, which is its own form of pressure',
        N'The contact who bought the Lord''s travel schedule has not contacted him again in fourteen months. He has considered confessing to Gaspar and decided against it. He has considered confessing to the Spymaster and decided against it. He has decided against everything. He cannot determine whether the silence means the information was harmless, or whether whatever it was used for has already happened and he simply does not know.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Lacerta stables',
        N'0', N'0',
        N'young Iberian groom 28, dark brown hair and eyes, stable work attire, estate stable interior, focused attentive expression carrying something private, photorealistic',
        N'A young Iberian man of twenty-eight, dark brown hair, brown eyes, stable work attire. Grooming a horse in an estate stable, expression focused and attentive and doing work to contain something that is not the work.',
        0, 0
    );
    PRINT 'Ander Zuloaga seeded.';
END
ELSE PRINT 'Ander Zuloaga already exists.';
GO

-- 44. Groom Florinda Arce
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Florinda Arce')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Florinda Arce', N'florinda-arce', N'canon', 1,
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
        @id, N'Florinda Arce', N'florinda-arce', N'Florinda', N'Arce', N'',
        N'human', N'human', N'female', N'she/her', 24, N'alive',
        N'Groom; Gaspar''s niece; placed in the estate two years ago to disappear from something Gaspar has never explained; good with difficult horses; prefers the stable to the staff common room',
        N'Florinda is Gaspar''s niece, which means she was placed here with a specific context that Gaspar has never explained to anyone. She arrived two years ago. She works well, has a good instinct with horses, and does not discuss where she came from before the estate. The other stable staff have collectively decided not to ask. Gaspar keeps a specific watch over her without appearing to, which she has noticed and has not commented on because she is grateful, and because the alternative to being here was worse than she wants to think about.',
        N'The stable hand whose unnamed prior situation connects to something Gaspar will not discuss; her presence in the House is a favor owed to no one Gaspar is willing to name.',
        N'No POV.',
        N'House Lacerta stables',
        162, 59, N'lean and capable, outdoor-built',
        N'dark brown', N'practical', N'medium',
        N'brown', N'olive', N'outdoor-marked',
        N'none',
        N'Quiet and capable; at ease with horses in a way she is not at ease with people; prefers the stable to the staff common room',
        N'Stable working attire',
        N'none',
        N'Standard groom duties alongside Ander; she is better with the difficult horses than anyone except Gaspar; spends her evenings in the stable by preference',
        N'She is here because she witnessed something in her home village that the people involved did not want witnessed, and Gaspar arranged for her to disappear into the estate rather than be made to disappear by whoever wanted her quiet. She does not know who those people were or whether they have looked for her. She does not know whether Gaspar knows exactly what she saw or whether his protectiveness is general family obligation. She has not asked.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Lacerta stables',
        N'0', N'0',
        N'young Iberian female groom 24, dark brown hair and eyes, stable work attire, estate stable, quiet careful expression, photorealistic',
        N'A young Iberian woman of twenty-four, dark brown hair and brown eyes, stable work attire. Working with a horse in the estate stable, expression quiet and careful — someone who has made themselves useful in a place and is paying attention to whether it stays safe.',
        0, 0
    );
    PRINT 'Florinda Arce seeded.';
END
ELSE PRINT 'Florinda Arce already exists.';
GO

-- 45. Groundskeeper Esteban Sarriá
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Esteban Sarriá')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Esteban Sarriá', N'esteban-sarria', N'canon', 1,
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
        @id, N'Esteban Sarriá', N'esteban-sarria', N'Esteban', N'Sarriá', N'',
        N'human', N'human', N'male', N'he/him', 68, N'alive',
        N'Groundskeeper; forty years managing the estate grounds and outer defenses; has found three things buried on the estate that he reburied without telling anyone; one was a body',
        N'Esteban has managed the House Lacerta estate grounds for forty years. He knows this land at the level of what the soil does after rain and where the cliff face is stable and where it is not. He is the person who walks every part of the estate regularly. In forty years he has found three things buried that were not planted by him, and in all three cases he has reburied them without telling anyone. One of them was a body.',
        N'The groundskeeper whose forty years of silence about what he has found is either discretion or complicity, and he has never been sure which; he is sixty-eight years old and the question of what to do with this knowledge is becoming more pressing.',
        N'No POV.',
        N'House Lacerta estate grounds; cliff perimeter',
        170, 69, N'lean and weathered to leather, a man who has worked outdoors for forty years',
        N'white', N'weathered', N'short',
        N'dark brown', N'very weathered dark olive', N'forty years of sun and Atlantic wind',
        N'none',
        N'Slow and steady outdoors; he moves through the estate grounds with the ease of long memorization',
        N'Outdoor working attire, heavily weathered',
        N'none',
        N'Dawn estate rounds; ground maintenance and repair; cliff-face safety checks; seasonal planting coordination; he is the only person who regularly walks every part of the estate',
        N'The body he reburied was a man he did not recognize, found thirty-two years ago in the eastern garden — young, dead of what looked like a head wound, wearing clothes that were not estate livery. He reburied the body because the previous Lord was in the middle of a succession crisis and the timing was terrible. He has carried this for thirty-two years. He is sixty-eight years old and the question of what to do with this knowledge has become more pressing as he gets closer to the point where he won''t be able to act on it.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Lacerta estate grounds; cliff perimeter',
        N'0', N'0',
        N'elderly weathered Iberian groundskeeper 68, white hair, dark brown eyes, very weathered dark olive skin, outdoor work attire, estate grounds at dawn, knowing watchful expression, photorealistic',
        N'An elderly Iberian man of sixty-eight, white hair, dark brown eyes, skin weathered to leather by forty years of outdoor work. Standing in the estate grounds at dawn, holding a tool, expression of someone who has known this land longer than most of the residents have been alive and knows things about it that the land has never told anyone else.',
        0, 0
    );
    PRINT 'Esteban Sarriá seeded.';
END
ELSE PRINT 'Esteban Sarriá already exists.';
GO

-- 46. House Physician Asunción Balda
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Asunción Balda')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Asunción Balda', N'asuncion-balda', N'canon', 1,
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
        @id, N'Asunción Balda', N'asuncion-balda', N'Asunción', N'Balda', N'Doctor',
        N'human', N'human', N'female', N'she/her', 50, N'alive',
        N'House Physician; sixteen years treating the family and senior staff; told Lord Rodrigo two years ago that he is dying; told by him to tell no one; has not told anyone',
        N'Asunción has been the House Lacerta physician for sixteen years. She treats the family, the senior staff, and on rotation the Corps soldiers who come to the estate infirmary. She has access to everyone — because a physician is allowed into rooms that political relationships govern and cannot be asked to leave. She knows that Lord Rodrigo''s Knight infusion from thirty-six years ago is interacting badly with normal physiological aging, producing a cardiovascular degradation that is accelerating. She told him two years ago. He told her not to tell anyone else. She has not.',
        N'The physician who holds the Lord''s most consequential secret; the question of when, if ever, she tells is the question of what loyalty to a patient costs.',
        N'No POV.',
        N'House Lacerta estate; medical room; estate-wide patient range',
        163, 61, N'composed and clinical, built for the endurance of a physician''s life',
        N'dark brown going gray', N'pulled back', N'medium',
        N'dark brown', N'olive', N'composed, indoor-careful',
        N'none',
        N'Precise and contained; she moves through professional settings with focused economy and through social settings with the same economy, which makes her harder to read than most',
        N'Physician''s attire; clean and practical',
        N'none',
        N'Morning patient rounds; afternoon consultation hours; twice-weekly Corps infirmary rotation; medical records with a precision that keeps most of her actual findings out of the routine review',
        N'Lord Rodrigo is dying. His heart and vascular system are failing from a long-term interaction of the Knight enhancement and age — a known but rare complication she estimates will produce obvious symptoms in two to four years and become fatal within five to eight. He knows. He has instructed her to tell no one. She is beginning to doubt this was the right decision, because the people making political decisions for the House are planning for a future built on an incorrect understanding of who will be there to execute it.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Lacerta estate; medical room',
        N'0', N'0',
        N'Iberian house physician 50, dark going-gray hair pulled back, dark brown eyes, physician''s attire, estate medical room, careful professional expression containing private knowledge, photorealistic',
        N'An Iberian woman of fifty, dark hair going gray pulled back, dark brown eyes, physician''s attire. In the estate medical room reviewing a patient record, expression focused and professional, and something beneath it that the professionalism is working to contain.',
        0, 0
    );
    PRINT 'Asunción Balda seeded.';
END
ELSE PRINT 'Asunción Balda already exists.';
GO

-- 47. Chaplain / Bheur Priest Isidoro Zubikarai
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Isidoro Zubikarai')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Isidoro Zubikarai', N'isidoro-zubikarai', N'canon', 1,
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
        @id, N'Isidoro Zubikarai', N'isidoro-zubikarai', N'Isidoro', N'Zubikarai', N'Father',
        N'human', N'human', N'male', N'he/him', 66, N'alive',
        N'Chaplain and Bheur Priest of House Lacerta; thirty-one years officiating at House rites; has arrived independently at a theological conclusion about the western anomaly that the Scrying staff reached through direct observation',
        N'Isidoro officiates the Bheur rites for House Lacerta — the formal observances around death, remembrance, and the unconfirmable question of what follows. He has done this for thirty-one years and presided over forty-seven deaths in this House. He is a gentle man, curious and soft-spoken, and has been increasingly troubled by the relationship between his theology and what he has heard, over the years, about what the Lacerta Chamber shows to the west. He has arrived, through independent theological reasoning, at a conclusion that the Scrying staff reached through direct observation.',
        N'The chaplain whose private theology has converged with the operators'' observations; he is the person most likely to put the theological and the empirical together, and the Liturgy would consider both conclusions heretical.',
        N'No POV.',
        N'House Lacerta estate; chapel; estate-wide pastoral range',
        169, 64, N'slight and soft-spoken, the build of a life spent indoors with books',
        N'white', N'thin', N'short',
        N'dark brown', N'olive, age-spotted', N'gently lined, unhurried',
        N'none',
        N'Unhurried and attentive; he listens more than he speaks, which is considered a chaplain''s quality and is in his case also genuine curiosity',
        N'Chaplain''s formal attire for rites; simpler dress for daily life; he has worn variations of the same clothing for thirty years',
        N'none',
        N'Morning prayer; attending the family at mealtimes as a conversational presence; presiding over rites as needed; afternoon theological reading and writing; increasingly spends evenings in conversation with the Physician',
        N'He has come to believe that the Bheur — the unconfirmable afterlife — can be approached empirically, and that what the Lacerta Chamber shows to the west is consistent with what he would expect if the Bheur were a physical location: a place the living cannot reach but can observe at the edge of observation. He has not said this to the Liturgy because the Liturgy''s relationship with unorthodox theology is not tolerant. He is sixty-six years old and has begun to wonder if he should say it to someone before he runs out of years.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Lacerta estate; chapel',
        N'0', N'0',
        N'elderly Iberian chaplain 66, thin white hair, dark brown eyes, chaplain''s formal attire, estate chapel candlelight, contemplative gentle expression, theological text in hand, photorealistic',
        N'An elderly Iberian man of sixty-six, thin white hair, dark brown eyes, chaplain''s formal attire. Seated in a small stone estate chapel by candlelight, holding a theological text, expression of someone who has been thinking about something for years and has almost arrived at a conclusion he is afraid to reach.',
        0, 0
    );
    PRINT 'Isidoro Zubikarai seeded.';
END
ELSE PRINT 'Isidoro Zubikarai already exists.';
GO

-- 48. Librarian / Tutor Salomé Orte
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Salomé Orte')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Salomé Orte', N'salome-orte', N'canon', 1,
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
        @id, N'Salomé Orte', N'salome-orte', N'Salomé', N'Orte', N'',
        N'human', N'human', N'female', N'she/her', 43, N'alive',
        N'House Librarian and Tutor; fourteen years managing the library and teaching the family''s younger members; built an unofficial curriculum for Ysolde that the Lord never saw',
        N'Salomé manages the House Lacerta library and has tutored the family''s younger members for fourteen years. She taught Ysolde and Amadeu, and before them the children of two diplomatic households that rotated through the estate. She is learned, precise, and has a genuine delight in ideas that she allows herself to express with students and nowhere else. She has built, over fourteen years, an unofficial curriculum for Ysolde that the official record does not reflect — supplementing the formal syllabus with philosophical and political texts the House did not sanction.',
        N'The tutor who shaped the heir''s political mind in ways the Lord never authorized; what Ysolde has done with that education is the downstream consequence.',
        N'No POV.',
        N'House Lacerta library; estate tutorial range',
        163, 62, N'slight and scholar-built, the posture of someone who has spent fourteen years surrounded by books',
        N'dark brown with silver threads', N'somewhat loose', N'medium',
        N'dark brown', N'olive, book-pale', N'soft-lined, absorbed',
        N'none',
        N'Animated and precise when talking about ideas; quiet and observant otherwise',
        N'Scholar''s attire; practical and ink-marked; she forgets to dress formally for occasions until someone reminds her',
        N'none',
        N'Morning library administration; afternoon tutorial sessions; managing research requests from the cabinet; evenings in her own reading, which is extensive and eclectic',
        N'She has been providing Ysolde with texts about political agency, Cauld legal history, and the historical cases in which junior family members successfully negotiated their own terms rather than accepting arranged marriages. She did not anticipate what Ysolde would do with this education. She is beginning to suspect, from Ysolde''s recent reading requests, that something is already in motion that she started without intending to.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Lacerta library',
        N'0', N'0',
        N'Iberian librarian 43, dark brown hair silver threads, dark brown eyes, scholar''s attire, estate library warm afternoon light, surrounded by volumes, absorbed expression, photorealistic',
        N'An Iberian woman of forty-three, dark brown hair with silver threads, dark brown eyes, scholar''s attire. Surrounded by stacked volumes in a library with warm afternoon light, expression absorbed and slightly delighted — someone who has spent fourteen years in a room full of ideas and hasn''t tired of it.',
        0, 0
    );
    PRINT 'Salomé Orte seeded.';
END
ELSE PRINT 'Salomé Orte already exists.';
GO

-- 49. Page Pello Lazkano
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Pello Lazkano')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Pello Lazkano', N'pello-lazkano', N'canon', 1,
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
        @id, N'Pello Lazkano', N'pello-lazkano', N'Pello', N'Lazkano', N'',
        N'human', N'human', N'male', N'he/him', 14, N'alive',
        N'Page; two years carrying messages through the estate; has become so unremarkable in motion that most senior staff have stopped registering his passage through their conversations',
        N'Pello has been a page at House Lacerta for two years and carries messages between the household''s various wings and offices. He is fast, reliable, and has learned to be so unremarkable in motion that most senior staff have stopped registering his passage through their conversations. He is fourteen. He has overheard things in two years of rapid passage through the estate''s most private corridors that no fourteen-year-old should have heard, and he does not fully understand most of them, which is the thing that has kept him from talking about them.',
        N'The page who has heard fragments of every significant conversation in the House; his partial understanding is both protection and danger.',
        N'No POV.',
        N'House Lacerta estate; all message routes',
        158, 47, N'slight and still growing, a boy who has learned to move without being seen',
        N'dark brown', N'short, sometimes unruly', N'short',
        N'dark brown', N'olive', N'clear, young',
        N'none',
        N'Quick and self-effacing in motion; he is learning that being invisible is a skill',
        N'House livery in dark green, slightly large on a still-growing frame',
        N'none',
        N'Message runs from dawn until the household retires; eats quickly and in motion; has developed the habit of remembering exact words from conversations he passes through',
        N'Three months ago, waiting outside the Chancellor''s office to deliver a document, he overheard a conversation between the Chancellor and an unfamiliar man. The phrase he caught most clearly was: "when the Lord is gone, the terms hold." He does not know what this means. He has not told anyone. He is fourteen and the people he would tell are the people the phrase was about.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Lacerta estate; all message routes',
        N'0', N'0',
        N'young Iberian page 14, dark brown hair and eyes, House livery slightly large, estate corridor, alert quick expression, photorealistic',
        N'A young Iberian boy of fourteen, dark brown hair, brown eyes, House livery that fits like it was made for someone slightly larger. Moving through a stone estate corridor with the quick quiet efficiency of a page who has learned to be invisible, expression alert.',
        0, 0
    );
    PRINT 'Pello Lazkano seeded.';
END
ELSE PRINT 'Pello Lazkano already exists.';
GO

-- 50. Page Mencía Ularrain
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Mencía Ularrain')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Mencía Ularrain', N'mencia-ularrain', N'canon', 1,
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
        @id, N'Mencía Ularrain', N'mencia-ularrain', N'Mencía', N'Ularrain', N'',
        N'human', N'human', N'female', N'she/her', 15, N'alive',
        N'Page; eighteen months in the estate; conducting a systematic campaign to read every document that passes through her hands; fifteen years old and extraordinarily good at it',
        N'Mencía has been a page at House Lacerta for eighteen months. She is younger than Pello but has been developing the page''s skills more deliberately — she is conducting, without naming it, a systematic campaign to read every document that passes through her hands, and to pass through the conversations of every office in the estate. She is fifteen. She is extraordinarily good at it. The senior staff have noticed she is smart and quick. They have not noticed that she is applying both qualities in a direction that has nothing to do with message delivery.',
        N'The page operating an informal intelligence collection with no handler and no plan; what she does with what she is gathering will depend entirely on what kind of person she becomes.',
        N'No POV.',
        N'House Lacerta estate; all message routes',
        155, 46, N'slight and quick, a girl who has learned to be underestimated',
        N'dark brown', N'sometimes braided, sometimes loose', N'medium',
        N'dark brown', N'olive', N'clear, young',
        N'none',
        N'Sharp and attentive in a way that fifteen-year-olds are not usually noticed to be; she arrives early to every office she services',
        N'House livery; she wears it correctly and without complaint',
        N'none',
        N'Message runs; she uses routes that pass through the most information-dense areas; has developed a habit of arriving early and leaving late to every office; keeps notes in a cipher she invented herself',
        N'She is collecting information. She does not have a plan for it. A cipher-notebook documents two months of detailed observations about the House''s political operations, personal habits, and private movements. She is fifteen. She does not yet know what she is going to do with her life, but she knows it will use this skill. She is very afraid of being underestimated for much longer, because the people who underestimate her are becoming assets.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Lacerta estate; all message routes',
        N'0', N'0',
        N'young Iberian page 15, dark brown hair, dark brown eyes, House livery, estate corridor, sharp attentive expression doing more work than a page''s expression should, photorealistic',
        N'A young Iberian girl of fifteen, dark brown hair, dark brown eyes, House livery. Moving through a stone estate corridor with quick deliberate efficiency, expression sharp and attentive in a way that fifteen-year-olds are not usually noticed to be.',
        0, 0
    );
    PRINT 'Mencía Ularrain seeded.';
END
ELSE PRINT 'Mencía Ularrain already exists.';
GO

-- 51. Laundry Master Generosa Sorzano
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Generosa Sorzano')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Generosa Sorzano', N'generosa-sorzano', N'canon', 1,
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
        @id, N'Generosa Sorzano', N'generosa-sorzano', N'Generosa', N'Sorzano', N'',
        N'human', N'human', N'female', N'she/her', 52, N'alive',
        N'Laundry Master; twenty-three years managing estate laundry; the person everyone underestimates; has been reading correspondence left in pockets for twenty-three years; has a private ledger',
        N'Generosa manages the estate laundry — the cleaning of all household and personal linen, clothing, and fabric. She has been doing this for twenty-three years. The laundry is a room that everyone forgets exists until something goes wrong with it, and in twenty-three years nothing has gone wrong on Generosa''s watch. She is the person everyone underestimates. She has been reading correspondence left in pockets for twenty-three years. Not systematically, not for any purpose — simply because she is meticulous about the garments she receives, checking pockets is part of the process, and what was in the pockets was interesting, and she started keeping notes. She has a ledger.',
        N'The laundry master whose inadvertent intelligence collection is the House''s most comprehensive and least expected record of private business; the ledger is the most dangerous document on the estate.',
        N'No POV.',
        N'House Lacerta laundry; estate-wide garment range',
        158, 63, N'compact and practical, built from twenty-three years of laundry work',
        N'salt-and-pepper', N'pulled back practically', N'medium',
        N'dark brown', N'olive', N'hands roughened permanently from water and lye',
        N'none',
        N'Methodical and unhurried; she checks pockets with routine attention and nothing she finds there surprises her anymore',
        N'Laundry room working attire; she owns one formal dress she has worn twice',
        N'none',
        N'Laundry operations from dawn to mid-afternoon; management of laundry staff; collection and return of garments; private ledger writing in the evenings after dinner',
        N'The ledger documents twenty-three years of pocket contents and what she made of them. The most dangerous item is a letter she found in the Chancellor''s coat seventeen months ago — a draft, never sent, of terms for a private arrangement with House Fornax. She has the letter. She kept it. It is in the ledger. She has told no one. She is a laundry master with the Chancellor''s unsent treason in her hands and no idea what a person does with that.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Lacerta laundry; estate-wide',
        N'0', N'0',
        N'Iberian laundry master 52, salt-and-pepper hair pulled back, dark brown eyes, roughened hands, laundry room attire, checking garment pocket with routine attention, photorealistic',
        N'An Iberian woman of fifty-two, salt-and-pepper hair pulled back, dark brown eyes, hands roughened from twenty-three years of water and lye. Standing in the estate laundry room, checking the pockets of a coat with the routine attention of someone for whom this has become a very interesting habit.',
        0, 0
    );
    PRINT 'Generosa Sorzano seeded.';
END
ELSE PRINT 'Generosa Sorzano already exists.';
GO

-- 52. Head of Household Guards Aurelio Landa (Knight)
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Aurelio Landa')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Aurelio Landa', N'aurelio-landa', N'canon', 1,
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
        @id, N'Aurelio Landa', N'aurelio-landa', N'Aurelio', N'Landa', N'',
        N'human', N'human', N'male', N'he/him', 46, N'alive',
        N'Head of Household Guards; nine years commanding the Lord''s personal security; was offered a bribe to allow access to the Lord''s private study; refused; did not report it',
        N'Aurelio commands the household guards — the thirty soldiers responsible for the personal security of the Lord, the family, and the estate''s interior perimeter. His people are not campaign soldiers but security professionals. He took the Catalyst infusion at thirty and has been in this posting for nine years. He is thorough, precise, and has the specific paranoia of a person whose job is to think about everything that can go wrong and prevent it. He was offered a bribe last year to allow access to the Lord''s private quarters at night. He refused. He did not report it.',
        N'The household security commander whose decision not to report the bribe attempt is either personal discretion or a structural gap that whoever made the attempt knows about.',
        N'No POV.',
        N'House Lacerta estate; Lord''s private quarters and interior perimeter',
        183, 88, N'Knight-enhanced: slightly taller and denser than his natural frame',
        N'dark brown going gray', N'short, neat', N'short',
        N'brown', N'olive, weathered', N'composed, alert-marked',
        N'Subtle height gain, increased density — the Knight''s mark',
        N'Watchful and deliberate; he has been running threat assessments in his head for nine years and the habit has become the posture',
        N'Guard commander attire; formal when required, security-practical at all other times',
        N'Knight-grade Catalyst infusion; increased bone density, marginal height gain, enhanced physical recovery',
        N'Pre-dawn security review; guard rotation coordination; weekly threat assessment; regular walkthrough of the Lord''s private quarters and family areas; monthly briefing with the Commander',
        N'The bribe came through a channel he could not trace, offering significant money for a single night''s unmonitored access to the Lord''s private study. He refused. He did not report it because the intermediary who approached him was a member of the ruling family he cannot name without destroying that person and possibly the House''s political stability. He has been watching that family member more carefully for a year. He has found nothing. He does not know if the watch is working or if the person is simply more careful than he is.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Lacerta estate; Lord''s private quarters and perimeter',
        N'0', N'0',
        N'Iberian household guard commander 46, dark brown going gray hair, brown eyes, Knight-enhanced build, guard attire, estate interior corridor at night, watchful alert posture, photorealistic',
        N'An Iberian man of forty-six, dark brown hair going gray, brown eyes, Knight-enhanced build in guard commander attire. Standing in the estate interior corridor at night, watchful posture, expression of someone who has been running threat assessments for nine years and has found something he does not know what to do with.',
        0, 0
    );
    PRINT 'Aurelio Landa seeded.';
END
ELSE PRINT 'Aurelio Landa already exists.';
GO

-- ============================================================
-- SECTION 6: OATHLESS ADJACENT (2)
-- ============================================================

-- 53. Iñaki Azpilcueta (Former Member, Oathless, Knight)
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Iñaki Azpilcueta')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Iñaki Azpilcueta', N'inaki-azpilcueta', N'canon', 1,
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
        @id, N'Iñaki Azpilcueta', N'inaki-azpilcueta', N'Iñaki', N'Azpilcueta', N'',
        N'human', N'human', N'male', N'he/him', 43, N'alive',
        N'Former Lacerta Corps soldier; Oathless; Knight; left the Corps six years ago after refusing a direct order from Commander Miren; still used for operations the House cannot officially authorize',
        N'Iñaki served the Lacerta Corps for twelve years, took the Catalyst infusion at twenty-five, and left the oath at thirty-seven under circumstances the official record describes as a voluntary release. The unofficial circumstances are that he refused a direct order from Commander Miren — an order he has not, in six years, described to anyone, and that she has not either. He operates in the space between Houses now, taking work from anyone who offers it and affiliated with no one. House Lacerta uses him for operations they cannot officially authorize. He is paid. He does not pretend he is doing it out of loyalty.',
        N'The former soldier whose undisclosed reason for leaving is the key to an order Commander Miren does not want examined; he is a loose resource and a structural vulnerability simultaneously.',
        N'No POV.',
        N'Cliff settlement at the edge of Lacerta territory; operational range as needed',
        182, 86, N'Knight-enhanced: slightly taller and denser, worn by six years outside estate infrastructure',
        N'dark brown', N'not recently cut', N'medium',
        N'dark brown', N'olive, weathered from outdoor living', N'outdoor-worn',
        N'Subtle height gain, increased density — the Knight''s mark',
        N'Guarded and economic; the posture of someone who has decided no institutional loyalty is worth what he was asked to give',
        N'Non-House practical attire; no livery, no Corps colors',
        N'Knight-grade Catalyst infusion; increased bone density, marginal height gain, enhanced physical recovery',
        N'No fixed schedule; operates from a cliff settlement at the edge of Lacerta territory; receives work through a contact who does not use his name; appears at the estate four times per year on request',
        N'The order he refused was to kill a Scrying operator who had documented observations that Commander Miren wanted removed from the record. He does not know if the operator is still alive. He does not know if the documentation survived. He left rather than comply and rather than report, which he has understood, in six years of reflection, makes him complicit either way. He has considered using what he knows. He is not sure who he would bring it to — and the Spymaster is the obvious answer, and he does not trust her.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Cliff settlement; Lacerta territory fringe; operational range',
        N'0', N'0',
        N'weathered Oathless Iberian former soldier 43, dark brown hair not recently cut, dark brown eyes, Knight-enhanced build, non-House practical attire, Atlantic cliff settlement, guarded expression, photorealistic fantasy-steampunk',
        N'An Iberian man of forty-three, dark brown hair not recently cut, dark brown eyes, Knight-enhanced build. Non-House practical attire — no livery, no Corps colors. Standing at the edge of a cliff settlement with the Atlantic behind him, expression guarded in the way of someone who has decided that no institutional loyalty is worth what he was asked to give.',
        0, 0
    );
    PRINT 'Iñaki Azpilcueta seeded.';
END
ELSE PRINT 'Iñaki Azpilcueta already exists.';
GO

-- 54. Maider Esparza (Oathless, Taking Shelter in Lacerta Territory)
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Maider Esparza')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Maider Esparza', N'maider-esparza', N'canon', 1,
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
        @id, N'Maider Esparza', N'maider-esparza', N'Maider', N'Esparza', N'',
        N'human', N'human', N'female', N'she/her', 37, N'alive',
        N'Oathless; former House Draught Scrying operator; living in Lacerta cliff territory for fourteen months; has documentation of the western anomaly from the Draught apparatus; trying to reach Cándida Olaiz',
        N'Maider has been living in a cave settlement in the outer cliff territory of House Lacerta for fourteen months, which the House knows about and tolerates for reasons that have never been written down. She came from House Draught, where she was a junior Scrying operator, and left under circumstances she describes as irreconcilable differences with the Liturgy''s management of what she observed. She is not a military asset, not a political asset, and not a threat. She is a person who saw something at a Draught Scrying installation and chose to become Oathless rather than file the falsified report she was told to file.',
        N'The Oathless operator whose refused report from House Draught may be corroborating data for the Lacerta Chamber''s western anomaly; she carries documentation that the Liturgy does not know exists.',
        N'No POV.',
        N'Cave settlement; outer Lacerta cliff territory',
        164, 61, N'travel-worn and outdoor-lived, a woman who has been moving for fourteen months',
        N'dark brown', N'grown out unevenly', N'medium',
        N'dark brown', N'olive, travel-worn', N'outdoor-marked, tired-lined',
        N'none',
        N'Watchful and direct; she has been deciding who to trust for fourteen months and has not yet decided',
        N'Travel-worn practical clothing; no House colors, nothing that would mark her as affiliated',
        N'none',
        N'Living by trade and small assistance work in the cliff settlement; occasional contact with Lacerta border guards who have been instructed to leave her alone; has been trying for three months to arrange a meeting with anyone from the Lacerta Chamber',
        N'What she saw at the House Draught apparatus was a western scan — the same direction the Lacerta Chamber is known for. She was told to record it as apparatus malfunction and report nothing. She could not do it. What she saw was too specifically organized to be malfunction. She went Oathless rather than falsify the report. She has the original documentation on her person. She believes it matches what the Lacerta Chamber has been seeing for eleven years. She does not know how to get it to Cándida Olaiz without the Liturgy finding out.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Cave settlement; outer Lacerta cliff territory',
        N'0', N'0',
        N'weathered Oathless former Scrying operator 37, dark brown hair grown out unevenly, dark brown eyes, travel-worn practical clothing no House colors, Atlantic cliff settlement, determined wary expression, photorealistic fantasy-steampunk',
        N'An Iberian woman of thirty-seven, dark brown hair grown out unevenly, dark brown eyes, travel-worn practical clothing — no House colors, nothing to mark her as affiliated. Standing at the entrance of a cliff settlement, expression determined and watchful, someone who has made a decision she cannot unmake and is still deciding whether she was right.',
        0, 0
    );
    PRINT 'Maider Esparza seeded.';
END
ELSE PRINT 'Maider Esparza already exists.';
GO

-- ============================================================
-- END: HOUSE LACERTA FULL HIERARCHY — 54 characters seeded
-- ============================================================
