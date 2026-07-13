SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- CAULD UNIVERSE — HOUSE PALLOR LOWER HIERARCHY (PART B)
-- Scrying Installation Staff + Domestic Staff + Oathless Adjacent
-- Universe: scry (ID 0197E9C9-0002-7000-8000-000000000002)
-- 2026-07-04
-- 26 characters; idempotent (IF NOT EXISTS guards on all inserts)
-- ═══════════════════════════════════════════════════════════════════════════════

-- ── Oswald Hatch ──────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Oswald Hatch')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Oswald Hatch', N'oswald-hatch', N'canon', 1,
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
        @id, N'Oswald Hatch', N'oswald-hatch', N'Oswald', N'Hatch', N'Master',
        N'human', N'human', N'male', N'he/him', 62, N'alive',
        N'Head Scrying Operator, Pallor Naval Apparatus Station; thirty-seven years at the installation.',
        N'Has worked the apparatus longer than anyone alive in the station. Arrives an hour before his shift and leaves an hour after it ends; he has never explained this and no one has successfully questioned it. His eyes carry a faint milkiness at the outer rim from decades of apparatus proximity — a condition the House physician cannot classify and he has never sought to treat. Speaks rarely. Watches much. The juniors find him unreadable. The seniors find him right too often to dismiss.',
        N'The operator who has been at the data long enough to see what the data is doing.',
        N'No POV.',
        N'House Pallor; Anglic coastal settlements, Pallor Main',
        173, 79, N'lean and angular; sparse',
        N'grey-white', N'close-cropped', N'very short',
        N'pale grey', N'pale', N'weathered; faint milkiness at the outer rim of both eyes from apparatus proximity',
        N'none',
        N'Still. He does not shift position during a watch. When he moves it is deliberate and slightly slow, as though conserving something.',
        N'Dark wool, unadorned; the same cut he has worn for fifteen years, let out once at the shoulders',
        N'none',
        N'Arrives before his shift; leaves after it. Studies his own logs in the hour between. Eats his evening meal at the station rather than in staff quarters. Has not taken a leave of absence in nine years. No one has asked him to.',
        N'He has cross-referenced thirty-seven years of observation logs and identified six individuals in Sphere 31 who appear in the data of three separate operators across two distinct apparatus installations over three decades. The apparatus has no coordinating function — it cannot focus itself. Someone set the targeting criteria before the current installation was built. He has never written the theory down. He is afraid of what it would mean if he is right, and more afraid of what it would mean if someone else is right first.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Pallor Naval Apparatus Station; shoreline path he walks at dawn; staff quarters adjacent to the station',
        N'0', N'0',
        N'62-year-old man, grey-white close-cropped hair, pale grey eyes with faint milky rim, dark unadorned wool, lean and still, medieval steampunk naval observatory, dim instrument light, Buehlman dark register',
        N'62-year-old man, grey-white cropped hair, pale grey eyes, dark wool coat, lean and weathered, steampunk observatory setting',
        0, 0
    );
    PRINT 'Oswald Hatch seeded.';
END
ELSE PRINT 'Oswald Hatch already exists.';
GO

-- ── Morwenna Pryce ────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Morwenna Pryce')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Morwenna Pryce', N'morwenna-pryce', N'canon', 1,
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
        @id, N'Morwenna Pryce', N'morwenna-pryce', N'Morwenna', N'Pryce', N'',
        N'human', N'human', N'female', N'she/her', 48, N'alive',
        N'Senior Scrying Operator, Pallor Naval Apparatus Station; twenty-one years watching the same Sphere 31 sector.',
        N'Speaks freely about what she observes. Names her subjects. Tracks their households across years. She considers this methodological precision; her colleagues consider it a professional deformation she has made peace with rather than corrected. Dougal Coyne will not work the same watch as her if it can be avoided, and the feeling is mutual. She has the better memory and she knows it.',
        N'The operator who has looked at the same people for twenty years and has opinions about them.',
        N'No POV.',
        N'House Pallor; Kellian highlands, western coast',
        164, 61, N'slight; upright posture',
        N'dark auburn', N'loosely pinned', N'long',
        N'dark hazel', N'medium warm', N'clear; fine lines at the corners of both eyes',
        N'none',
        N'Precise and deliberate at the apparatus. Outside of it she is brisk, mildly impatient, and has strong opinions about how the station''s logs should be formatted.',
        N'Practical wool; Kellian weave in the lining of her coat; keeps an ink-stained apron on the peg by her station',
        N'none',
        N'Works long watches. Eats at her station. Keeps a naming ledger for observed subjects that she updates weekly. Goes home to a small house near the station each night; the house is full of secondary ledgers no one else has been shown.',
        N'She has documented forty-three instances of Sphere 31 subjects looking up at nothing — windows, ceilings, open sky — at the precise moment apparatus observation focused on them. The timestamps are exact; she cross-referenced the apparatus calibration logs herself. Senior operators call it confirmation bias. She has a private ledger at home with all forty-three entries. She is not wrong. She has not yet understood the mechanism by which she is right.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Pallor Naval Apparatus Station; small house within walking distance; Kellian highland territory on rare leave',
        N'0', N'0',
        N'48-year-old woman, dark auburn hair loosely pinned, hazel eyes, practical Kellian-weave wool, medieval steampunk observatory, focused and deliberate, Buehlman dark register',
        N'48-year-old woman, dark auburn hair, hazel eyes, wool coat with Celtic lining, steampunk observatory setting',
        0, 0
    );
    PRINT 'Morwenna Pryce seeded.';
END
ELSE PRINT 'Morwenna Pryce already exists.';
GO

-- ── Dougal Coyne ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Dougal Coyne')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Dougal Coyne', N'dougal-coyne', N'canon', 1,
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
        @id, N'Dougal Coyne', N'dougal-coyne', N'Dougal', N'Coyne', N'',
        N'human', N'human', N'male', N'he/him', 54, N'alive',
        N'Senior Scrying Operator, Pallor Naval Apparatus Station; twenty-four years; holds that personalization of observed subjects is a professional failure.',
        N'Believes the apparatus shows surface patterns and that operators who name or narrate their subjects are dangerously compromised. Has been professionally correct in every documented case. He and Morwenna Pryce have not worked the same watch in six years. The disagreement is not personal, which makes it worse — they have both decided the other is wrong in a way that cannot be resolved by conversation.',
        N'The operator who refuses sentiment as a methodology and is correct about this in every way that does not matter.',
        N'No POV.',
        N'House Pallor; Anglic midlands, Pallor Main',
        178, 86, N'broad-shouldered; solidly built',
        N'dark brown going grey at the temples', N'side-parted', N'short',
        N'brown', N'medium pale', N'ruddy; broken capillaries across the nose',
        N'none',
        N'Settled and heavy. Moves with the confidence of someone who expects to be taken seriously and usually is.',
        N'Dark practical clothing; a worn leather shoulder-guard he never explains and has worn since his first year at the station',
        N'none',
        N'Keeps standard hours. Writes precise shift reports. Eats dinner with two colleagues whose names he does not share with the others. Goes home and does not bring the work with him, which is the only professional discipline he has that Morwenna lacks.',
        N'At nineteen he was flagged as a Liturgy transit candidate and came within one conversation of being selected. His House sponsor intervened; the reason given was administrative. He has never told anyone. He built his professional methodology — treat subjects as data, never as people — after that year. He knows exactly what it is to be observed from the other side of the apparatus. He has made a philosophy out of not knowing it.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Pallor Naval Apparatus Station; staff quarters; Anglic midland town on leave',
        N'0', N'0',
        N'54-year-old man, dark brown hair greying at temples, brown eyes, broad-shouldered, worn leather shoulder-guard, medieval steampunk observatory, settled and solid, Buehlman dark register',
        N'54-year-old man, dark brown greying hair, broad-shouldered, leather shoulder-guard, steampunk observatory',
        0, 0
    );
    PRINT 'Dougal Coyne seeded.';
END
ELSE PRINT 'Dougal Coyne already exists.';
GO

-- ── Bran Ashwick ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Bran Ashwick')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Bran Ashwick', N'bran-ashwick', N'canon', 1,
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
        @id, N'Bran Ashwick', N'bran-ashwick', N'Bran', N'Ashwick', N'Master',
        N'human', N'human', N'male', N'he/him', 44, N'alive',
        N'Technical Maintenance Chief, Pallor Naval Apparatus Station; keeps the apparatus running; has never performed an observation.',
        N'Knows the apparatus''s tolerances, material fatigue points, calibration drift curves, and operational limits better than any operator knows the data it produces. Blunt and competent. Mildly contemptuous of the operators'' metaphysical interpretations of what is, in his view, a very precise lens in need of regular maintenance. The operators need him more than he needs any of them and everyone knows it.',
        N'The man who knows what the machine can and cannot do, which is more than anyone observing through it knows.',
        N'No POV.',
        N'House Pallor; Anglic port district, Pallor Main',
        180, 88, N'stocky; heavily built through the chest and arms',
        N'black, coarse', N'roughly kept', N'short',
        N'dark brown', N'medium', N'weathered; grease-stained hands that never fully clean',
        N'none',
        N'Practical and efficient. Fills whatever space he is in without claiming it. Sets things down on surfaces as though they will stay there until he retrieves them.',
        N'Heavy canvas work clothes; leather apron with pockets he has added himself over the years; boots resoled three times',
        N'none',
        N'Arrives before the operators. Checks the apparatus before every shift cycle. Eats fast. Stays late when something is not right. Keeps his own maintenance logs in a format no one else uses and has never been asked to explain.',
        N'The apparatus has been running with a fundamental calibration error for eleven years. He found it three months into his tenure. The error does not change what the operators see — it shifts which sphere the apparatus is actually targeting. They believe they are observing Sphere 31. He is not certain they are. He has not corrected the error because doing so would require shutting the apparatus down for sixty days and he does not know what that would do to the operators'' ongoing observation threads. He has been meaning to raise it. He has not raised it. Eleven years have passed.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Pallor Naval Apparatus Station; maintenance workshops; port district when sourcing parts',
        N'0', N'0',
        N'44-year-old man, black coarse hair, dark brown eyes, heavy canvas work clothes, leather apron, stocky and powerful, medieval steampunk apparatus maintenance workshop, Buehlman dark register',
        N'44-year-old stocky man, black hair, leather apron, workshop tools, steampunk observatory maintenance setting',
        0, 0
    );
    PRINT 'Bran Ashwick seeded.';
END
ELSE PRINT 'Bran Ashwick already exists.';
GO

-- ── Fionnuala Maddoch ─────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Fionnuala Maddoch')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Fionnuala Maddoch', N'fionnuala-maddoch', N'canon', 1,
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
        @id, N'Fionnuala Maddoch', N'fionnuala-maddoch', N'Fionnuala', N'Maddoch', N'',
        N'human', N'human', N'female', N'she/her', 23, N'alive',
        N'Junior Scrying Operator, Pallor Naval Apparatus Station; four months in post; recently trained.',
        N'Precise and quick to learn, not yet worn into the patterns the senior operators mistake for professionalism. The seniors find her questions exhausting. She asks them anyway. She has not yet learned that the exhaustion is the point — that it is how the senior operators have always responded to anyone who noticed something they did not want noticed.',
        N'The newest pair of eyes at the apparatus, and the only one who has seen something true in her first months.',
        N'No POV.',
        N'House Pallor; Kellian hill settlements, interior territory',
        162, 56, N'slight; alert posture',
        N'dark brown, thick', N'pulled back tightly', N'long',
        N'grey-green', N'medium warm; Kellian highland complexion', N'clear; wind-roughened from the coastal posting',
        N'none',
        N'Forward and precise at the apparatus. Takes notes constantly. Asks follow-up questions after being dismissed. Has not learned to stop yet.',
        N'Standard station-issue observer''s coat; Kellian weave scarf she brought from home; practical boots',
        N'none',
        N'Works every shift she is assigned and takes voluntary overtime. Reads the station''s older logs during downtime. Eats quickly. Goes home to shared junior operator quarters and spends the evenings in her notes.',
        N'In her third month of operation she observed a Sphere 31 subject she recognized: a person she watched being taken by the Liturgy when she was eleven years old, from a hill village in Kellian territory. The subject is now embedded in Sphere 31 life — thirty-six years old, with a family, no apparent memory of origin. She reported the sighting to her shift supervisor. She was told it was a resemblance error and to log it as a calibration anomaly. She was told, as a child, that what she saw at eleven was not a Liturgy taking. She now knows it was. The senior operators dismissed what she saw. She is not wrong.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Pallor Naval Apparatus Station; junior staff quarters; Kellian interior on leave',
        N'0', N'0',
        N'23-year-old woman, dark brown thick hair pulled back, grey-green eyes, station observer coat, slight and alert, medieval steampunk observatory, focused intensity, Buehlman dark register',
        N'23-year-old woman, dark brown hair, grey-green eyes, wool observer coat, steampunk observatory, intent expression',
        0, 0
    );
    PRINT 'Fionnuala Maddoch seeded.';
END
ELSE PRINT 'Fionnuala Maddoch already exists.';
GO

-- ── Rowena Calder ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Rowena Calder')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Rowena Calder', N'rowena-calder', N'canon', 1,
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
        @id, N'Rowena Calder', N'rowena-calder', N'Rowena', N'Calder', N'',
        N'human', N'human', N'female', N'she/her', 71, N'alive',
        N'Retired Scrying Operator; informally consulted by the Pallor Naval Apparatus Station; no official position for nine years.',
        N'Retired under pressure in her final active year after filing a report documenting what she believed was a deliberate communication attempt originating from Sphere 31. The Lord-in-Residence at the time ordered the records destroyed and told her the interpretation was apophenia. She accepted her retirement pension without argument. She lives three streets from the installation. When the apparatus behaves in ways that confound current operators, she is the person they quietly send someone to visit.',
        N'The retired expert who is waiting for someone to ask the right question, and knows that no one has.',
        N'No POV.',
        N'House Pallor; Anglic coastal settlements, Pallor Main',
        158, 53, N'slight; age has taken height but not bearing',
        N'white, fine', N'simply pinned', N'short',
        N'pale blue-grey', N'pale', N'deeply lined; clear-eyed',
        N'none',
        N'Unhurried. Sits with perfect stillness. Has stopped moving faster than she needs to and is evidently at peace with this.',
        N'Good wool, well-maintained; the clothing of someone who was once near-important and has kept the habits without the position',
        N'none',
        N'Keeps her own hours. Reads. Walks the shoreline at low tide. Receives occasional visitors from the station with the courtesy of someone who expected them and did not hurry. Tends a small garden that she approaches with the same methodical attention she once gave the apparatus logs.',
        N'She kept a copy of the report the Lord-in-Residence ordered destroyed. In nine years of retirement she has studied it further and identified a secondary pattern within the data that was not present in the original report — a pattern that suggests the communication attempt was not originating from Sphere 31 subjects but from whatever mechanism operates the membrane itself. She has told no one. She is waiting for someone to ask the right question. In nine years, no one has asked it.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Coastal residential street adjacent to the Pallor Naval Apparatus Station; shoreline walk at low tide',
        N'0', N'0',
        N'71-year-old woman, white fine hair simply pinned, pale blue-grey eyes, good wool coat, slight and still, coastal medieval steampunk town, Buehlman dark register',
        N'71-year-old woman, white hair, pale blue-grey eyes, wool coat, coastal town, composed and watchful expression',
        0, 0
    );
    PRINT 'Rowena Calder seeded.';
END
ELSE PRINT 'Rowena Calder already exists.';
GO

-- ── Aldwyn Thorne ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Aldwyn Thorne')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Aldwyn Thorne', N'aldwyn-thorne', N'canon', 1,
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
        @id, N'Aldwyn Thorne', N'aldwyn-thorne', N'Aldwyn', N'Thorne', N'Master',
        N'human', N'human', N'male', N'he/him', 67, N'alive',
        N'Seneschal of House Pallor; head of all household staff; has served in this position for forty-one years under two successive Lords.',
        N'Has managed the Pallor household since before the current Lord was born. Served his father before him and knows the shape of a Pallor Lord in ways the Pallor Lords themselves do not. Nothing in the household moves without his knowledge; most things cannot move without his authorization. He voices no opinion on House policy. He has many opinions. The staff fear his disappointment more than they fear punishment, which is how he prefers it.',
        N'The household''s true institutional memory — older than the Lord''s tenure and more complete.',
        N'No POV.',
        N'House Pallor; Anglic midlands, Pallor Main',
        174, 74, N'upright and precise; age has not softened his posture',
        N'silver-white', N'smoothly side-parted', N'short',
        N'grey', N'pale', N'lined; composed at all times',
        N'none',
        N'Economy of movement. Every gesture has been reduced to exactly what is needed. Has not wasted a motion in thirty years.',
        N'House livery of excellent quality; personal effects that are never ostentatious and always correct',
        N'none',
        N'Rises before the household. Retires after the household. Reviews accounts every morning before the first meal. Attends every formal occasion and several informal ones. Is present at departures and arrivals he was not informed of. Has not explained how he knows.',
        N'He has known for sixteen years that the current Lord has a living half-sibling — born before the legal marriage, to a Kellian woman — who works as a dockhand at Morvic port. Both the Lord and Aldwyn know. Neither has acknowledged it aloud in sixteen years. Aldwyn manages the dockhand''s quarterly stipend from household accounts, coded as "harbor provisioning." Eight months ago a new household servant arrived and has been looking at that account line with the particular kind of care that suggests she was told to look.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'House Pallor estate; all household wings; estate grounds when required',
        N'0', N'0',
        N'67-year-old man, silver-white hair side-parted, grey eyes, House livery, upright and precise, Pallor estate interior, grand medieval steampunk manor, Buehlman dark register',
        N'67-year-old man, silver hair, grey eyes, formal household livery, upright bearing, medieval manor interior',
        0, 0
    );
    PRINT 'Aldwyn Thorne seeded.';
END
ELSE PRINT 'Aldwyn Thorne already exists.';
GO

-- ── Catriona Fletch ───────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Catriona Fletch')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Catriona Fletch', N'catriona-fletch', N'canon', 1,
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
        @id, N'Catriona Fletch', N'catriona-fletch', N'Catriona', N'Fletch', N'Mistress',
        N'human', N'human', N'female', N'she/her', 58, N'alive',
        N'Head Cook of House Pallor; thirty-five years in the kitchen; knows the food preferences of every person who has sat at a Pallor table.',
        N'Is not sentimental about the family. Is not sentimental about food. Is entirely precise about both. Knows the preference of every person in the household and every significant guest in thirty-five years of service — not as warmth but as intelligence. Visiting diplomats are read by what they will not eat. She has been doing this quietly since before anyone thought to instruct her to.',
        N'The person in the household who has been extracting intelligence through hospitality for three decades without being asked to.',
        N'No POV.',
        N'House Pallor; Anglic coastal settlements, Pallor Main; Morvic maternal line',
        160, 72, N'solid; built for a life of physical work',
        N'iron grey, formerly black', N'tightly pinned under a kitchen cap', N'long when unpinned',
        N'dark brown', N'medium warm; Morvic maternal complexion', N'heat-ruddy; flour-dusted in working hours',
        N'none',
        N'Efficient and unhurried at once — a rhythm the kitchen moves around rather than she around it.',
        N'Kitchen working clothes of good practical quality; her own aprons, never the House-issue ones; boots resoled each year',
        N'none',
        N'Arrives in the kitchen before first light and leaves after the last meal is cleared. Runs the kitchen as a precision operation. Does not tolerate waste. Has not taken a sick day in twenty years. Her sous-chef Eithne has ambitions Catriona has chosen not to discourage.',
        N'She has kept a private record of every diplomatic visit for twenty-two years, encoded in the kitchen''s provisioning logs as dietary notes and preferences. Three years ago, a Liturgy envoy refused the salt-preserved fish that is a standard feature of Pallor formal hospitality. Salt-preserved fish is used in Liturgy transit preparation — it appears in the preparation rites described in a document she was not supposed to have read. She has told no one. She has been adding salt-preserved fish to more menus since the envoy''s visit. She is running an experiment. She is close to certain of the result.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'House Pallor kitchens and service corridors; market provisioning routes; estate storage',
        N'0', N'0',
        N'58-year-old woman, iron-grey hair pinned under kitchen cap, dark brown eyes, practical kitchen apron, solid and efficient, Pallor estate kitchen, medieval steampunk grand manor, Buehlman dark register',
        N'58-year-old woman, grey hair under cap, dark eyes, kitchen apron, medieval manor kitchen setting, composed and watchful',
        0, 0
    );
    PRINT 'Catriona Fletch seeded.';
END
ELSE PRINT 'Catriona Fletch already exists.';
GO

-- ── Eithne Gallen ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Eithne Gallen')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Eithne Gallen', N'eithne-gallen', N'canon', 1,
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
        @id, N'Eithne Gallen', N'eithne-gallen', N'Eithne', N'Gallen', N'',
        N'human', N'human', N'female', N'she/her', 34, N'alive',
        N'Sous-Chef of House Pallor; second in the kitchen; has ambitions the Head Cook knows about and has chosen not to discourage.',
        N'Skilled and careful. Catriona Fletch knows about her ambitions and interprets the non-discouragement as permission; Catriona interprets it as an opportunity she is permitting to develop into something informative. They work together with exact professional cordiality. Eithne has never asked what Catriona actually thinks of her. She has been afraid of the answer for two years.',
        N'The sous-chef whose ambitions are pointed in a direction that will not end in the Head Cook''s position.',
        N'No POV.',
        N'House Pallor; Kellian coastal settlements, western port district',
        165, 63, N'lean and capable; quick hands',
        N'auburn-brown', N'single braid kept off the face', N'long',
        N'pale green', N'medium warm', N'clear; light freckles across the nose',
        N'none',
        N'Quick and efficient at the station. Keeps her preparations organized with a precision that slightly exceeds what the kitchen requires. Thinks ahead.',
        N'Kitchen working clothes, slightly better quality than station-issue; her own knives in a wrapped personal roll she brought from her previous posting',
        N'none',
        N'Works full kitchen hours. Keeps notes on recipes that she describes as personal development. Sends correspondence after the evening service, which she addresses at a writing desk in her quarters and which she does not discuss.',
        N'Her ambition is not Catriona''s position. She wants to cook for the Liturgy''s inner table, which she has decided is the real seat of power in the Cauld. For two years she has corresponded with a person presenting as a Liturgy kitchen steward, under a false name, offering observations about Pallor household hospitality patterns and guest preferences in exchange for what she believes is an informal candidacy discussion. She does not know the "steward" is a Scrying monitor reporting to the Pallor naval installation. She has been answering questions she was not aware she was being asked.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'House Pallor kitchens; service corridors; private quarters for correspondence',
        N'0', N'0',
        N'34-year-old woman, auburn-brown hair in a single braid, pale green eyes, kitchen working clothes, quick and capable, medieval steampunk manor kitchen, Buehlman dark register',
        N'34-year-old woman, auburn braid, green eyes, kitchen apron, medieval manor kitchen, focused expression',
        0, 0
    );
    PRINT 'Eithne Gallen seeded.';
END
ELSE PRINT 'Eithne Gallen already exists.';
GO

-- ── Cerdic Lune ───────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Cerdic Lune')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Cerdic Lune', N'cerdic-lune', N'canon', 1,
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
        @id, N'Cerdic Lune', N'cerdic-lune', N'Cerdic', N'Lune', N'',
        N'human', N'human', N'male', N'he/him', 28, N'alive',
        N'Kitchen assistant, House Pallor; taken from Sphere 31 at age sixteen; twelve years in service; considered entirely unremarkable by the household.',
        N'Arrived in the Cauld during a period of irregular Liturgy activity that was never fully documented. Took the name Cerdic Lune in his second year, replacing the name he arrived with. Speaks flawless Anglic, knows the household''s rhythms as if born into them, and is consistent and reliable at every task he is given. The household considers him unremarkable. He is not unremarkable.',
        N'The Sphere 31 person who adapted completely and has been quietly counting something no one else has noticed.',
        N'No POV.',
        N'Sphere 31 (Earth); absorbed into House Pallor domestic service twelve years ago; considers Pallor Main his territory',
        171, 68, N'medium build; moves quietly',
        N'dark brown', N'simply cut', N'short',
        N'dark brown', N'medium-dark', N'clear',
        N'none',
        N'Quiet, efficient, and slightly outside the social clusters of the other staff without appearing to exclude himself. The horses calm when he enters the kitchen yard. The kitchen staff stopped noticing him years ago.',
        N'House-issue working clothes, kept clean; practical boots; no personal adornments',
        N'none',
        N'Works kitchen hours without complaint. Runs errands between kitchen and storerooms. Sleeps in staff quarters. Eats after the main kitchen staff. Has not attempted to leave Pallor territory in twelve years.',
        N'Since his third year in the House he has been leaving a mark — three scored lines, the lowest angled — on the underside of the kitchen door''s lowest hinge, each time he believes a person from Sphere 31 has passed through the household. There are forty-seven marks. He does not know what he expects to happen. He knows that someone should be counting. He has been the only one counting. No one has ever asked what the marks are.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'House Pallor kitchens; service corridors; estate grounds; has not left Pallor Main territory',
        N'0', N'0',
        N'28-year-old man, dark brown hair, dark brown eyes, house-issue working clothes, quiet and unremarkable, medieval steampunk manor kitchen, Buehlman dark register',
        N'28-year-old man, dark hair and eyes, plain working clothes, manor kitchen setting, quietly watchful expression',
        0, 0
    );
    PRINT 'Cerdic Lune seeded.';
END
ELSE PRINT 'Cerdic Lune already exists.';
GO

-- ── Wulfric Crane ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Wulfric Crane')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Wulfric Crane', N'wulfric-crane', N'canon', 1,
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
        @id, N'Wulfric Crane', N'wulfric-crane', N'Wulfric', N'Crane', N'Master',
        N'human', N'human', N'male', N'he/him', 55, N'alive',
        N'Butler of House Pallor; manager of serving staff and formal household occasions; the House''s dignity expressed in a person.',
        N'The House''s dignity is his only self-presentation; he performs no other. Which family member comes home at strange hours, which visitors use the servants'' entrance rather than the main approach, which departures are entered in the household log and which are not — all of this is known to him. He does not record it anywhere beyond the formal log, and the formal log is maintained with a precision he considers essential.',
        N'The butler who is preserving a record that someone else is trying to erase, without having decided why.',
        N'No POV.',
        N'House Pallor; Anglic midlands, Pallor Main',
        182, 80, N'tall; carries himself as though the House depends on it, which in a certain sense it does',
        N'iron grey', N'smoothly side-parted', N'short',
        N'grey-blue', N'pale', N'composed; not a line out of place even under pressure',
        N'none',
        N'Precise. Still when stillness is required; in motion when motion is. Manages formal occasions with a quality of controlled invisibility.',
        N'House formal livery of the best available quality; maintained to a standard that exceeds what the household formally requires',
        N'none',
        N'Manages every formal occasion and several informal ones. Reviews the household log at the end of each day and the beginning of each morning. Oversees serving staff rotations. Attends to the Lord and Lady''s formal requirements with a consistency that has never faltered in twenty-three years.',
        N'Four years ago he discovered that a family member was removing pages from the House''s log of Liturgy transit requests — not destroying them, but removing them before anyone else could consult the log. He discovered it in the second year of the removals. Since then, whenever he finds a gap, he reconstructs the missing page from memory the same evening and inserts a clean copy before the log is next consulted. He does not know why he does this. He has not asked himself. He considers it his function to preserve the record. What anyone does with the record is not his concern.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'House Pallor estate; all formal and service corridors; entrance hall and reception rooms',
        N'0', N'0',
        N'55-year-old man, iron-grey hair side-parted, grey-blue eyes, formal house livery, tall and precisely composed, Pallor estate grand interior, medieval steampunk manor, Buehlman dark register',
        N'55-year-old man, grey hair, formal livery, tall and composed, medieval manor butler, controlled expression',
        0, 0
    );
    PRINT 'Wulfric Crane seeded.';
END
ELSE PRINT 'Wulfric Crane already exists.';
GO

-- ── Rhiannon Marsh ────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Rhiannon Marsh')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Rhiannon Marsh', N'rhiannon-marsh', N'canon', 1,
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
        @id, N'Rhiannon Marsh', N'rhiannon-marsh', N'Rhiannon', N'Marsh', N'Mistress',
        N'human', N'human', N'female', N'she/her', 49, N'alive',
        N'Head Housekeeper of House Pallor; manages household cleaning, linen, laundry, and guest quarters.',
        N'Has found things over twenty-two years in this position. Most of what she finds she reports to the Seneschal. Some of what she finds she does not report because she cannot yet determine to whom reporting it would be safe.',
        N'The housekeeper who knows where something is hidden in the east wing and has been deciding what to do about it for three years.',
        N'No POV.',
        N'House Pallor; Kellian highlands; second-generation household staff',
        166, 64, N'medium; practical and unhurried',
        N'dark brown, streaked with grey', N'pinned precisely', N'medium length',
        N'dark hazel', N'medium warm; highland complexion', N'clear; slightly tired around the eyes',
        N'none',
        N'Efficient and self-contained. Has a quality of being present in a room without disrupting its atmosphere.',
        N'Household-quality working clothes, scrupulously maintained; a ring of keys at her belt that she never sets down unnecessarily',
        N'none',
        N'Manages linen rotations, cleaning schedules, guest quarters preparation, and the dozen small maintenance matters that constitute the invisible half of household operations. Reports to the Seneschal. Has her own network of junior staff who trust her judgment over the formal hierarchy.',
        N'Three years ago, behind a heavy laundry press in the east wing, she found a door that does not appear on any house plan she has been able to access. She has explored twelve feet of passage before it turns into darkness she has not continued into. She has not told the Lord. She has not told Aldwyn. She has been taking measurements and drawing a map on the inside back cover of a household linen inventory ledger. She is still deciding what the map is for and who it belongs to.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'House Pallor estate; all domestic wings; guest quarters; laundry and storage areas',
        N'0', N'0',
        N'49-year-old woman, dark brown and grey hair precisely pinned, hazel eyes, housekeeper keys at belt, practical household working clothes, Pallor estate interior, medieval steampunk manor, Buehlman dark register',
        N'49-year-old woman, dark greying hair pinned, hazel eyes, keys at belt, medieval manor housekeeper, composed and watchful',
        0, 0
    );
    PRINT 'Rhiannon Marsh seeded.';
END
ELSE PRINT 'Rhiannon Marsh already exists.';
GO

-- ── Beorn Leach ───────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Beorn Leach')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Beorn Leach', N'beorn-leach', N'canon', 1,
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
        @id, N'Beorn Leach', N'beorn-leach', N'Beorn', N'Leach', N'',
        N'human', N'human', N'male', N'he/him', 38, N'alive',
        N'General household servant of House Pallor; born on the estate; has known nothing else.',
        N'Loyalty to the House is the only mode of being he has ever practised, and it has cost him twice. He is not without intelligence; he has simply never been given the information necessary to apply it in his own interest.',
        N'The loyal servant whose loyalty has been used against him once, and who is waiting to understand whether it is being used against him again.',
        N'No POV.',
        N'House Pallor; estate-born; Anglic stock; has not left Pallor Main territory',
        176, 81, N'solid; broad through the shoulders; built for carrying things',
        N'dark blond going brown', N'roughly kept', N'short',
        N'pale blue', N'pale', N'weather-roughened from outdoor estate work',
        N'none',
        N'Straightforward and slightly slow-looking, which is not the same as being slow.',
        N'House-issue working clothes; practical boots; a jacket he mends himself when it wears through',
        N'none',
        N'Performs general household and estate labor. Takes assignments without complaint. Eats in the servants'' hall. Has no correspondence and no acquaintances outside the estate that anyone is aware of.',
        N'He twice helped a Kellian man enter the household unannounced at night — a man he understood to be connected to the Oathless former House member Osric Thane. He was paid for the first visit. The second time he was not paid, because the Kellian man knew that Beorn had helped the first time and also knew something about Beorn that Beorn had not shared with anyone. The Kellian has not returned in three months. Beorn does not know whether the operation ended or the man is dead. He has been waiting.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'House Pallor estate; grounds; service corridors; has not left Pallor Main territory',
        N'0', N'0',
        N'38-year-old man, dark blond hair, pale blue eyes, house-issue working clothes, broad-shouldered and solid, medieval steampunk manor estate, Buehlman dark register',
        N'38-year-old man, blond hair, blue eyes, working clothes, broad shoulders, medieval manor estate exterior',
        0, 0
    );
    PRINT 'Beorn Leach seeded.';
END
ELSE PRINT 'Beorn Leach already exists.';
GO

-- ── Saoirse Dunne ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Saoirse Dunne')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Saoirse Dunne', N'saoirse-dunne', N'canon', 1,
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
        @id, N'Saoirse Dunne', N'saoirse-dunne', N'Saoirse', N'Dunne', N'',
        N'human', N'human', N'female', N'she/her', 26, N'alive',
        N'General household servant of House Pallor; arrived eight months ago; consistently underestimated.',
        N'Works efficiently, keeps her own counsel, and is consistently underestimated by the staff who have been here longer. She does not correct this. She finds it useful.',
        N'The recent arrival who is here for a reason no one in the household has identified yet.',
        N'No POV.',
        N'House Pallor; Kellian settlements, interior hill territory; clan-adjacent placement',
        163, 57, N'slight; quick and unhurried at once',
        N'dark red-brown', N'loosely braided', N'long',
        N'brown', N'medium warm; Kellian hill complexion', N'clear',
        N'none',
        N'Occupies less space than she takes up. Moves quietly. Asks questions that sound like small talk.',
        N'House-issue working clothes, neat; nothing that invites attention',
        N'none',
        N'Works standard household hours. Does her assignments without error. Eats in the servants'' hall and listens more than she speaks. Has not formed close attachments with other staff, which reads as reserve but is actually discipline.',
        N'She was placed in the household by a Kellian clan council with one task: determine whether the Pallor seneschal is managing a quiet payment to a living blood relation of the current Lord. She has found the account line in the provisioning records, coded as "harbor provisioning." She knows what it is. She does not yet know who receives the payment. She is deciding whether to report to the clan council now, or to stay long enough to find out who the recipient is and what the clan would actually do with that information.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'House Pallor estate; domestic service corridors; estate grounds',
        N'0', N'0',
        N'26-year-old woman, dark red-brown loose braid, brown eyes, house-issue servant clothes, slight and quiet, medieval steampunk manor interior, Buehlman dark register',
        N'26-year-old woman, red-brown braided hair, brown eyes, plain servant clothes, medieval manor interior, listening expression',
        0, 0
    );
    PRINT 'Saoirse Dunne seeded.';
END
ELSE PRINT 'Saoirse Dunne already exists.';
GO

-- ── Isolde Varren ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Isolde Varren')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Isolde Varren', N'isolde-varren', N'canon', 1,
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
        @id, N'Isolde Varren', N'isolde-varren', N'Isolde', N'Varren', N'',
        N'human', N'human', N'female', N'she/her', 42, N'alive',
        N'Personal attendant to the Lady of House Pallor; nine years in intimate service; the most complete observer in the household.',
        N'Has served the Lady for nine years with a degree of discretion that has earned her access to the Lady''s private correspondence, schedule, and confidences. She is the most intimate observer in the household. She uses this position for no purpose that serves herself. She is not sure this is wisdom.',
        N'The attendant who has watched the Lady''s face closely enough to know what the Lady is not saying.',
        N'No POV.',
        N'House Pallor; Anglic midlands; maternal Morvic line',
        167, 62, N'medium; composed and quietly self-effacing',
        N'dark brown, smooth', N'neatly dressed', N'shoulder length',
        N'grey', N'medium', N'clear; faint shadows under the eyes from years of irregular sleep',
        N'none',
        N'Present without intruding. Moves through the Lady''s spaces as though anticipating where she will be needed rather than waiting to be called.',
        N'Clean, practical clothing of good quality — just below the level that would invite comment; the clothing of someone whose role is to be noticed as little as possible',
        N'none',
        N'Keeps the Lady''s schedule, correspondence, and appointments. Attends all private and formal occasions where the Lady is present. Sleeps lightly; has learned to do so over nine years of irregular hours.',
        N'For two years she has observed the Lady composing correspondence through channels that bypass the House''s formal transit documentation — letters that leave the estate without being logged. Three of those letters were written about the Lord and Lady''s fourteen-year-old heir. Isolde has never read the letters. She has read the Lady''s face each time one is sent: the particular combination of resolve and guilt that she has learned to distinguish from grief. She believes the heir is being considered for a Liturgy taking. She does not know whether the Lady is opposing the consideration or arranging it.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'House Pallor estate; the Lady''s private quarters and reception rooms; formal estate spaces',
        N'0', N'0',
        N'42-year-old woman, smooth dark brown hair neatly dressed, grey eyes, quality practical clothing, composed and self-effacing, Pallor estate private chambers, medieval steampunk manor, Buehlman dark register',
        N'42-year-old woman, dark brown hair, grey eyes, good plain clothing, medieval manor private chamber, watchful composed expression',
        0, 0
    );
    PRINT 'Isolde Varren seeded.';
END
ELSE PRINT 'Isolde Varren already exists.';
GO

-- ── Hereward Goode ────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Hereward Goode')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Hereward Goode', N'hereward-goode', N'canon', 1,
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
        @id, N'Hereward Goode', N'hereward-goode', N'Hereward', N'Goode', N'Master',
        N'human', N'human', N'male', N'he/him', 52, N'alive',
        N'Stable Master of House Pallor; eighteen years managing estate horses and all transport; the record of who leaves at night.',
        N'Knows every horse by gait, temperament, and recovery pattern. Knows when someone has ridden hard by reading the animal the next morning. Keeps no written record. Does not need one. Quiet in the way that men are quiet when they have decided not to speak rather than when they have nothing to say.',
        N'The stable master who holds in memory seven departures that were never logged and does not yet know what they add up to.',
        N'No POV.',
        N'House Pallor; Anglic coastal settlements, eastern shore; stable work since age twelve',
        183, 91, N'large; broad-shouldered; heavy through the arms from years of physical work',
        N'grey-brown', N'roughly kept; hat-pressed', N'short',
        N'pale grey', N'ruddy; weather-beaten', N'deeply weathered; broken nose set badly long ago',
        N'none',
        N'Economical and unhurried. The horses read his stillness as safety. He has learned to extend this to people when it is useful.',
        N'Working stable clothes in good repair; heavy boots; a coat that has belonged to him for fifteen years and shows it',
        N'none',
        N'Rises before dawn. Works through the day with one meal taken standing in the stable yard. Checks every horse at evening before he retires. Taught his nephew Alasdair Moss everything he knows about reading animals. Sees visitors and departures from the stable yard that the household log does not record.',
        N'In six years he has logged, in memory only, seven departures that appear in no household record: four by the Lord''s eldest child, one by the Chaplain, and two by a visitor who arrived and left without ever entering the main house. Once he was offered a bribe to forget one of those seven. He returned the coin without comment and has spoken of none of them. He does not yet know what they add up to. He is waiting for the pattern to become legible.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'House Pallor stables and estate grounds; stable yard; service routes to market',
        N'0', N'0',
        N'52-year-old man, grey-brown hair, pale grey eyes, broken nose, working stable coat, large and weathered, medieval steampunk manor stable yard at dawn, Buehlman dark register',
        N'52-year-old man, grey-brown hair, pale eyes, broken nose, weathered stable coat, medieval manor stable setting',
        0, 0
    );
    PRINT 'Hereward Goode seeded.';
END
ELSE PRINT 'Hereward Goode already exists.';
GO

-- ── Alasdair Moss ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Alasdair Moss')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Alasdair Moss', N'alasdair-moss', N'canon', 1,
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
        @id, N'Alasdair Moss', N'alasdair-moss', N'Alasdair', N'Moss', N'',
        N'human', N'human', N'male', N'he/him', 19, N'alive',
        N'Groom and stable hand of House Pallor; two years in post; Hereward Goode''s nephew; the horses trust him more than they trust anyone else.',
        N'Young and quiet. Placed at the stables by his uncle after both his parents were killed in a Draught coastal raid. Considered unremarkable by the household. The horses do not consider him unremarkable — they read him as safe in a way they do not read most people, including Hereward.',
        N'The groom who was taught everything his uncle knows and has learned things his uncle did not teach him.',
        N'No POV.',
        N'House Pallor; Anglic coastal settlement, eastern shore; parents killed in Draught raid two years ago',
        170, 65, N'lean; young muscle, not yet at full weight',
        N'dark blond', N'unstyled', N'short',
        N'pale blue', N'pale; weather-roughened', N'clear; young',
        N'none',
        N'Still around animals. Quick but unhurried. Speaks rarely and listens well.',
        N'Working stable clothes in basic repair; his father''s jacket, too large at the shoulders, worn on cold mornings',
        N'none',
        N'Works stable hours alongside his uncle. Tends animals in the early morning before Hereward arrives. Reads the horses each evening after the last riders have returned. Does not seek the company of the other junior staff.',
        N'Hereward taught him to read horses by their condition after a ride — sweat pattern, gait shift, temperament change. He has been applying this for a year and is better at it than Hereward realizes, because Hereward taught him the method but not what Hereward himself uses it for. He can now identify, by reading the horses in the morning, exactly who rode hard the night before. He has not told Hereward everything he has seen. He is deciding who the information belongs to and who would want it.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'House Pallor stables; estate grounds; stable yard',
        N'0', N'0',
        N'19-year-old man, dark blond unstyled hair, pale blue eyes, basic stable clothes, lean and quiet, medieval steampunk manor stables at dawn, Buehlman dark register',
        N'19-year-old man, dark blond hair, pale blue eyes, stable work clothes, lean, medieval manor stable setting',
        0, 0
    );
    PRINT 'Alasdair Moss seeded.';
END
ELSE PRINT 'Alasdair Moss already exists.';
GO

-- ── Fergal Bricke ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Fergal Bricke')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Fergal Bricke', N'fergal-bricke', N'canon', 1,
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
        @id, N'Fergal Bricke', N'fergal-bricke', N'Fergal', N'Bricke', N'',
        N'human', N'human', N'male', N'he/him', 68, N'alive',
        N'Groundskeeper of House Pallor; forty years managing estate grounds and outer defenses; knows where things are buried.',
        N'Has managed the grounds under two Lords. Knows every drain, soft-patch, and structural weakness in the outer walls. Knows the estate''s ground the way some men know scripture — the kind of knowledge that was assembled over decades and cannot be transferred in a conversation. Has seen things buried. Has sometimes been the one who buried them.',
        N'The man who has been keeping the estate''s physical secrets for forty years, two of which are in the north meadow.',
        N'No POV.',
        N'House Pallor; Morvic territories, western settlement; second generation on this estate',
        169, 76, N'wiry; strong for his age; stooped now only slightly',
        N'white, sparse', N'wind-disordered', N'short',
        N'dark brown', N'dark; Morvic western complexion', N'deeply weathered; earth-stained hands',
        N'none',
        N'Slow-looking but deliberate. Works without pausing. Covers more ground in a day than men half his age.',
        N'Heavy canvas working clothes; earth-stained boots; a belt knife he has carried since before the current Lord was born',
        N'none',
        N'Works from first light to last. Manages a small grounds crew of two junior staff. Knows the estate''s outer defenses better than the household guards. Has never taken more than three days'' leave at one stretch in forty years.',
        N'There are two things buried in the north meadow he has never reported and never discussed. The first is the House charter''s original third article — the version superseded forty years ago, which altered the three-people Council veto structure in a way that was never publicly explained and that the current structure cannot be easily derived from. The second is a set of Scrying apparatus lenses removed from the Pallor naval installation under the previous Lord, eighteen years before the current apparatus was installed. He buried both at the previous Lord''s direction. The lenses are operational. He checked them three years ago.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'House Pallor estate grounds; outer walls; north meadow; drainage routes',
        N'0', N'0',
        N'68-year-old man, white sparse hair, dark brown eyes, dark weathered complexion, canvas groundskeeper clothes, earth-stained, medieval steampunk manor estate grounds, Buehlman dark register',
        N'68-year-old man, white hair, dark complexion, canvas work clothes, belt knife, medieval manor estate grounds',
        0, 0
    );
    PRINT 'Fergal Bricke seeded.';
END
ELSE PRINT 'Fergal Bricke already exists.';
GO

-- ── Deirdre Ashby ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Deirdre Ashby')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Deirdre Ashby', N'deirdre-ashby', N'canon', 1,
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
        @id, N'Deirdre Ashby', N'deirdre-ashby', N'Deirdre', N'Ashby', N'Mistress',
        N'human', N'human', N'female', N'she/her', 46, N'alive',
        N'House Physician of House Pallor; access to every ailment in the household; made one decision eight years ago she cannot undo.',
        N'Treats the family and senior staff. Has access to every ailment, which means access to every secret the body keeps. Is careful with what she knows and conservative with what she says. The decision she made eight years ago has not gone away. She has been waiting for the right moment to do something about it. The right moment has not arrived. She has stopped being certain it will.',
        N'The physician who has proof of something the household cannot survive knowing and has been carrying it for eight years.',
        N'No POV.',
        N'House Pallor; Anglic midlands; trained in Pallor Main; Kellian maternal line',
        168, 65, N'medium; precise in movement',
        N'dark brown, threaded with silver', N'pinned back from the face', N'medium length',
        N'dark brown', N'medium warm', N'clear; careful; a quality of held attention in the eyes',
        N'none',
        N'Measured. Does not hurry even when hurrying would be noticed. Holds information longer than is strictly comfortable before acting on it.',
        N'Clean practical physician''s clothing; a leather case she carries on house calls; work boots that have never looked new',
        N'none',
        N'Attends daily to the family''s health and any staff matters referred to her. Keeps meticulous patient records. Has a locked cabinet in her workroom that she has never explained to anyone and has not been asked to explain.',
        N'Eight years ago she told the Lord that his father''s declining symptoms were consistent with advanced age. They were not. They were consistent with repeated sub-lethal Xerum 525 exposure — consistent with someone testing the old Lord''s tissue tolerance before an unsanctioned Transmutation attempt. She said nothing at the time because she had no proof. The old Lord died fourteen months later. She has had proof for six years: a preserved tissue sample in a sealed wax case inside the locked cabinet. She does not know what to do with it. She has not destroyed it. Every year she tells herself she will decide the following year.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'House Pallor physician''s workroom; family and senior staff quarters; estate interior',
        N'0', N'0',
        N'46-year-old woman, dark brown and silver hair pinned back, dark brown eyes, physician''s coat, precise and measured, Pallor estate physician workroom, medieval steampunk manor interior, Buehlman dark register',
        N'46-year-old woman, dark hair with silver, brown eyes, physician''s practical coat, leather case, medieval manor medical room',
        0, 0
    );
    PRINT 'Deirdre Ashby seeded.';
END
ELSE PRINT 'Deirdre Ashby already exists.';
GO

-- ── Brynhild Sorn ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Brynhild Sorn')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Brynhild Sorn', N'brynhild-sorn', N'canon', 1,
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
        @id, N'Brynhild Sorn', N'brynhild-sorn', N'Brynhild', N'Sorn', N'Mistress',
        N'human', N'human', N'female', N'she/her', 59, N'alive',
        N'Chaplain and Bheur Priest of House Pallor; officiates all House rites and funerary practices; twenty-two years in post.',
        N'Officiates every House rite with complete precision and evident conviction. Has done so for twenty-two years. Her formal practice is considered exemplary. Her private doubt is not about the Bheur — she has made peace with the unknowable. The doubt is about the Liturgy and what it is doing with the rites she performs.',
        N'The chaplain who has stopped being certain that the rites she says are neutral, and says them anyway.',
        N'No POV.',
        N'House Pallor; Morvic territories, northern settlements; theologically trained in Pallor Main',
        172, 69, N'upright; carries her height with a quality of formal stillness',
        N'silver-white, thick', N'dressed high and formally', N'long when down',
        N'pale grey-blue', N'pale; Morvic northern complexion', N'fine and still; composed even in private',
        N'none',
        N'Formal stillness. Does not fidget. Has a quality of occupying her position in any room completely without needing more of it.',
        N'House chaplain''s vestments for formal rites; plain good wool for informal hours; always clean and correct',
        N'none',
        N'Officiates all births, deaths, formal occasions, and private rites at the family''s request. Keeps a private study with theological texts she has collected over twenty-two years. Consults with the family on Bheur questions. Has not missed a single House rite in twenty-two years.',
        N'She obtained, through sources she will not name even to herself, a fragment of Liturgy internal documentation suggesting the transit preparation rites the Liturgy administers to taken persons are a modified Bheur practice — not sending the dead onward as the tradition describes, but preventing the living from accessing wherever they would otherwise go at the moment of death. She does not know if this is true. She performs House rites unchanged. She is no longer certain they are neutral. She says the words each time and watches what they do to the people listening. She has not stopped. She does not know what stopping would mean for the people she has already spoken over.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'House Pallor chapel and rite spaces; family quarters when called; chaplain''s private study',
        N'0', N'0',
        N'59-year-old woman, silver-white thick hair dressed formally, pale grey-blue eyes, chaplain''s vestments, upright and formally still, Pallor estate chapel, medieval steampunk manor, Buehlman dark register',
        N'59-year-old woman, silver hair formally dressed, pale grey-blue eyes, priest''s vestments, composed and still, medieval manor chapel',
        0, 0
    );
    PRINT 'Brynhild Sorn seeded.';
END
ELSE PRINT 'Brynhild Sorn already exists.';
GO

-- ── Gwenith Pell ──────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Gwenith Pell')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Gwenith Pell', N'gwenith-pell', N'canon', 1,
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
        @id, N'Gwenith Pell', N'gwenith-pell', N'Gwenith', N'Pell', N'',
        N'human', N'human', N'female', N'she/her', 15, N'alive',
        N'Page of House Pallor; carries messages throughout the household; fifteen years old; has been keeping confidences for three years.',
        N'Has been carrying messages since she was twelve. Has kept every confidence she was given, not because she was told to but because she understood from the first week that her usefulness depended on it. Hears everything she is not supposed to hear. Has been carrying that too. Aldwyn Thorne has been watching her for a year to determine whether she can be trusted with something larger.',
        N'The page who has been tested and passed and does not know it, and who is now carrying something she does not know how to classify.',
        N'No POV.',
        N'House Pallor; Anglic midlands; estate-adjacent family; placed in service at age twelve',
        152, 44, N'slight; still growing',
        N'light brown', N'braided back neatly for work', N'long',
        N'grey-brown', N'medium pale', N'clear; young',
        N'none',
        N'Quick and neat. Moves through the household like someone who has learned which floorboards creak.',
        N'Page''s livery, correctly worn; practical boots; hair always braided for work',
        N'none',
        N'Carries messages throughout the day. Waits in corridors. Runs between wings. Is present in rooms where significant people talk and is consistently underestimated by those people because she is fifteen and a page.',
        N'Six weeks ago she overheard a conversation between two Scrying operators in a service corridor that named a specific Sphere 31 individual being observed at the Lord''s personal request — not through Liturgy authorization channels, not as part of the installation''s formal observation log. She did not understand most of what was said. She understood that the observation was not supposed to exist officially. She has not told Aldwyn, who she has begun to trust with small things, because she does not yet know if this is something he would want to know or something he already knows and would want her to have not heard.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'House Pallor estate; all wings and corridors; service passages',
        N'0', N'0',
        N'15-year-old girl, light brown braided hair, grey-brown eyes, page''s livery, slight and quick, Pallor estate corridors, medieval steampunk manor, Buehlman dark register',
        N'15-year-old girl, light brown braided hair, grey-brown eyes, page livery, slight build, medieval manor corridor',
        0, 0
    );
    PRINT 'Gwenith Pell seeded.';
END
ELSE PRINT 'Gwenith Pell already exists.';
GO

-- ── Mervyn Poole ──────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Mervyn Poole')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Mervyn Poole', N'mervyn-poole', N'canon', 1,
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
        @id, N'Mervyn Poole', N'mervyn-poole', N'Mervyn', N'Poole', N'',
        N'human', N'human', N'male', N'he/him', 53, N'alive',
        N'Laundry Master of House Pallor; twenty years in post; the person everyone underestimates; has been reading correspondence left in pockets for two decades.',
        N'Is consistently underestimated. This is not an accident and has never been an accident. He has worked the household laundry for twenty years and in twenty years he has read everything he has ever found in a pocket. He has told no one. He has not been asked. He has been waiting for a reason that has not yet arrived.',
        N'The laundry master who has been holding a letter in a sealed jar behind the boiler for nine years, waiting to know what to do with it.',
        N'No POV.',
        N'House Pallor; Anglic port district; laundry work since age fourteen',
        171, 77, N'medium; soft-looking in a way that is misleading',
        N'grey-brown, thinning', N'flat to the head', N'short',
        N'pale brown', N'medium pale', N'mild; unremarkable; a face no one remembers after leaving the room',
        N'none',
        N'Unhurried and self-effacing. Occupies less social space than he is entitled to by age and position. This is the method.',
        N'Working laundry clothes of practical quality; an apron that is always clean; boots that squeak slightly, which he has never had repaired because it tells him when someone is approaching',
        N'none',
        N'Works the laundry from early morning until evening. Manages linen sorting, washing, drying, pressing, and return. Reviews every garment before it leaves his hands. Eats alone by preference. Has a small private area near the boiler that is technically a storage alcove and practically his office.',
        N'Nine years ago, in the inner coat pocket of a Liturgy courier who stayed a single night, he found a letter. The letter listed four names — persons taken from Sphere 31 — with a notation beside each. Three were marked "INTEGRATED." One was marked "PENDING RECOVERY." The fourth name was the Lord''s sister, who the House was told had drowned crossing the channel seven years before the letter was written. He kept the letter. It has been in a sealed wax jar behind the boiler for nine years. He reads it once a year to verify his memory. He has never told anyone. He does not know why he has not told anyone. He does not think it is loyalty.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'House Pallor laundry and linen rooms; service corridors; storage alcove near the boiler',
        N'0', N'0',
        N'53-year-old man, grey-brown thinning hair, pale brown eyes, laundry working apron, soft-looking and unremarkable, medieval steampunk manor laundry room, steam and warm light, Buehlman dark register',
        N'53-year-old man, grey-brown hair, forgettable face, laundry apron, medieval manor laundry room, steam lighting, mild expression',
        0, 0
    );
    PRINT 'Mervyn Poole seeded.';
END
ELSE PRINT 'Mervyn Poole already exists.';
GO

-- ── Sigrid Halke ──────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Sigrid Halke')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Sigrid Halke', N'sigrid-halke', N'canon', 1,
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
        @id, N'Sigrid Halke', N'sigrid-halke', N'Sigrid', N'Halke', N'',
        N'human', N'human', N'female', N'she/her', 56, N'alive',
        N'Head of Household Guards of House Pallor; manages the Lord''s personal nighttime security; old, reliable, carrying something she has never spoken aloud.',
        N'Not the Myrmidons. Not the naval garrison. The people who are actually in the corridor when the Lord sleeps. She manages them. She has done so for seventeen years. Old. Reliable. Has the quality of someone who has seen a number of things that most people in the house have not and has concluded that silence is its own kind of competence.',
        N'The guard commander who helped cover a death thirteen years ago and has been trusting the person who ordered it ever since, without quite knowing why.',
        N'No POV.',
        N'House Pallor; Draught heritage on her father''s side — her father was taken prisoner in the second channel breach; Anglic mother; raised in Pallor Main',
        175, 78, N'solid; hard through the shoulders; built for standing watch',
        N'blonde going white', N'short, close, practical', N'short',
        N'pale blue', N'pale; northern complexion', N'weathered; scarred at the left jaw from an old wound',
        N'none',
        N'Still and alert at once. Has the watchfulness of someone who is always aware of who is behind her. Manages the guard rotations with a precision the guards respect but find slightly unnerving.',
        N'Guard''s practical clothing in dark colors; a personal short blade she has carried since she was twenty-two; boots she has broken in over years',
        N'none',
        N'Manages guard rotations, schedules, and the private security arrangements for the Lord and Lady''s personal chambers. Reviews the night guard at first light. Does not delegate the chamber-door assignments. Has seventeen years of institutional knowledge about who comes and goes and when.',
        N'She knows who killed the previous Head of Household Guards thirteen years ago and staged it as an accidental fall from the east stairs. She knows because she helped cover it at the time, on the orders of someone who is still in the household. She was told the death was necessary and was given a reason she found adequate. She trusted the person who told her. She has continued to trust them for thirteen years. She does not understand why. She thinks it might be because she has too much to lose by stopping, and that this is not the same as innocence.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'House Pallor private chambers and corridors; guard quarters; estate perimeter during formal occasions',
        N'0', N'0',
        N'56-year-old woman, blonde going white cropped hair, pale blue eyes, jaw scar, dark guard clothing, solid and watchful, Pallor estate night corridor, medieval steampunk manor, Buehlman dark register',
        N'56-year-old woman, white-blonde short hair, jaw scar, pale blue eyes, dark practical guard clothing, medieval manor corridor at night',
        0, 0
    );
    PRINT 'Sigrid Halke seeded.';
END
ELSE PRINT 'Sigrid Halke already exists.';
GO

-- ── Osric Thane ───────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Osric Thane')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Osric Thane', N'osric-thane', N'canon', 1,
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
        @id, N'Osric Thane', N'osric-thane', N'Osric', N'Thane', N'',
        N'human', N'human', N'male', N'he/him', 44, N'alive',
        N'Former House Pallor junior Scrying operator; Oathless for fourteen years; covertly used by the House for operations the Council cannot officially authorize.',
        N'Broke his House oath fourteen years ago after witnessing a Liturgy transit he believed was unauthorized — a child taken without standard documentation, outside the formal request channels, with no House witness present. He could not prove it at the time. He broke his oath anyway. The House uses him now for border operations because he is useful and because the alternative is an Oathless person with detailed knowledge of Pallor''s Scrying apparatus infrastructure who has a grievance. The relationship is transactional on both sides. There is history on both sides.',
        N'The Oathless operative who has found what he was looking for and now does not know what to do about it.',
        N'No POV.',
        N'House Pallor; Anglic midlands origin; formally Oathless; no current House affiliation',
        179, 82, N'lean; moves with a quality of deliberate inconspicuousness',
        N'dark brown, shot with grey', N'simply kept', N'short',
        N'hazel', N'medium pale', N'weathered; lines from outdoor work and irregular sleep',
        N'none',
        N'Has learned to take up very little space. Moves through environments as someone passing through rather than arriving.',
        N'Practical travel clothing; nothing that names a House or a faction; the clothing of someone between postings',
        N'none',
        N'Operates along Pallor''s territorial borders on assignments the House''s formal staff cannot be seen to conduct. Lives without fixed address in Pallor territory. Receives payment in goods and tolerance rather than coin. Reports to a single Pallor contact whose name he has never confirmed.',
        N'He has spent fourteen years tracking the child who was taken in the unauthorized transit he witnessed. He has found them. They are now an adult embedded in the Liturgy''s own administrative structure — a mid-rank functionary who shows no indication of knowing where they came from. He does not know whether to extract them, warn them, or observe further. Any action requires trusting House Pallor enough to tell them what he knows, and he cannot do that. He is also aware that a woman named Ylva Strand, sheltering in Pallor territory, is looking for someone matching the same description. He has not told her. He is not sure why.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Pallor territorial borders; no fixed address; moves between border settlements',
        N'0', N'0',
        N'44-year-old man, dark brown greying hair, hazel eyes, plain travel clothing, lean and inconspicuous, Pallor border territory, medieval steampunk landscape, Buehlman dark register',
        N'44-year-old man, dark greying hair, hazel eyes, plain practical coat, lean build, medieval border territory, wary expression',
        0, 0
    );
    PRINT 'Osric Thane seeded.';
END
ELSE PRINT 'Osric Thane already exists.';
GO

-- ── Ylva Strand ───────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ylva Strand')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ylva Strand', N'ylva-strand', N'canon', 1,
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
        @id, N'Ylva Strand', N'ylva-strand', N'Ylva', N'Strand', N'',
        N'human', N'human', N'female', N'she/her', 38, N'alive',
        N'Oathless; Draught heritage; sheltering in House Pallor territory for two years under informal tolerance.',
        N'The House knows she is here. It has not expelled her. No arrangement has been named between them. Both parties understand the shape of the understanding without having stated it, which is the only kind of agreement each is willing to make with the other. She is Draught. The channel has been breached three times in living memory. The tolerance is its own statement.',
        N'The Oathless woman whose stated value to the House is not why she is actually here, and who does not know how close she is to finding what she came for.',
        N'No POV.',
        N'Draught territories; sheltering in House Pallor coastal zone; no current House affiliation',
        168, 66, N'lean; strong; capable of hard physical work',
        N'pale blonde, nearly white', N'roughly braided or simply tied', N'long',
        N'pale grey-blue', N'very pale; northern Draught complexion', N'weathered; wind-burned across the cheekbones',
        N'none',
        N'Economical and unhurried. Has a Draught directness that reads as bluntness in Pallor company and that she does not modify.',
        N'Practical working clothes of Draught make; a waterproofed outer layer; boots suited to coastal terrain',
        N'none',
        N'Lives in a coastal settlement within Pallor territory, tolerated by local inhabitants who understand the House''s silence as instruction. Does odd skilled labor — rigging, rope work, small hull repair. Is not asking for anything. Is very quietly looking for something.',
        N'The House tolerates her because she knows the location of a portable naval Scrying apparatus hidden by House Pallor after the third Draught breach — an apparatus the Liturgy ordered decommissioned that the House never destroyed. She has not confirmed this knowledge plainly; the Lord has not asked plainly. Neither party has named the arrangement. Her actual reason for staying is not the apparatus. Fourteen years ago her younger brother disappeared in a Draught coastal settlement under circumstances she has spent fourteen years concluding were an unauthorized Liturgy taking. She has come close enough to confirming this that she cannot leave. She does not know that Osric Thane has found the person she is looking for.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'House Pallor coastal zone; coastal settlement; shoreline and harbor territory',
        N'0', N'0',
        N'38-year-old woman, near-white blonde braided hair, pale grey-blue eyes, Draught waterproofed coat, lean and weathered, Pallor coastal settlement, medieval steampunk shoreline, Buehlman dark register',
        N'38-year-old woman, white-blonde braided hair, pale grey-blue eyes, weatherproofed coat, lean, medieval coastal settlement setting',
        0, 0
    );
    PRINT 'Ylva Strand seeded.';
END
ELSE PRINT 'Ylva Strand already exists.';
GO

-- ── Aldric Swane ──────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Aldric Swane')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Aldric Swane', N'aldric-swane', N'canon', 1,
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
        @id, N'Aldric Swane', N'aldric-swane', N'Aldric', N'Swane', N'',
        N'human', N'human', N'male', N'he/him', 31, N'alive',
        N'Oathless; presents as an independent maritime navigator; arrived in Pallor territory three months ago; has not been expelled.',
        N'Claims to be a navigator between postings. Is not a navigator. Speaks precisely, makes himself useful without appearing to try, and asks only questions that can be explained by context. He has asked many questions that way. Has not formed attachments. Has been careful not to be interesting.',
        N'The Liturgy plant who has discovered he does not know who actually sent him, and is running out of time to work it out.',
        N'No POV.',
        N'Cross-House; parentage unclear; presents as Anglic; Morvic speech patterns emerge under stress',
        177, 75, N'medium; controlled; presents as unremarkable and succeeds at it',
        N'dark brown', N'neatly kept; nothing memorable', N'short',
        N'brown', N'medium pale', N'clear; young enough to read as unthreatening',
        N'none',
        N'Measured and deliberate. Has learned to present his attention as casual when it is not.',
        N'Practical travel clothes; quality that implies competence without wealth; nothing that names a faction',
        N'none',
        N'Has been lodging in the coastal settlement for three months. Performs occasional skilled labor and offers himself as a contact for maritime information, both of which are genuine competencies. Moves between the settlement and the harbor without drawing comment. Has been studying who comes and goes from Pallor territory.',
        N'He was sent into Pallor territory by a Liturgy faction to determine whether House Pallor is knowingly sheltering Ylva Strand and how close her search has come to the unauthorized transit records from fourteen years ago. He found Ylva within his first two weeks and has assessed that she is very close. He has not reported back yet because within his third week he identified two pieces of evidence suggesting that the faction who instructed him is not the faction he believed he was serving when he accepted the assignment. He does not know who actually sent him or what they intend to do with his report. He has been trying to work this out for six weeks. He is aware that the longer he delays, the more dangerous the delay becomes.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'House Pallor coastal settlement; harbor; service routes he has established as a navigator cover',
        N'0', N'0',
        N'31-year-old man, dark brown hair neatly kept, brown eyes, practical travel clothes, medium build and controlled manner, medieval steampunk coastal settlement, Buehlman dark register',
        N'31-year-old man, dark brown hair, brown eyes, practical coat, medium build, medieval coastal town, carefully neutral expression',
        0, 0
    );
    PRINT 'Aldric Swane seeded.';
END
ELSE PRINT 'Aldric Swane already exists.';
GO
