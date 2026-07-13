SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- CAULD UNIVERSE — HOUSE FORNAX FULL HIERARCHY (55 characters)
-- Ruling Family · Political Cabinet · Military Command · Scrying Staff
-- Domestic Staff · Oathless-Adjacent
-- Rhine-Danube; Germany analog; industrial and methodical.
-- Universe: scry (ID 0197E9C9-0002-7000-8000-000000000002)
-- 2026-07-04
-- ═══════════════════════════════════════════════════════════════════════════════

-- ─────────────────────────────────────────────────────────────────────────────
-- RULING FAMILY (1–9)
-- ─────────────────────────────────────────────────────────────────────────────

-- 1. LORD HARTMUT BRENNER
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Hartmut Brenner')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Hartmut Brenner', N'hartmut-brenner', N'canon', 1,
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
        @id, N'Hartmut Brenner', N'hartmut-brenner', N'Hartmut', N'Brenner', N'Lord',
        N'human', N'human', N'male', N'he/him', 63, N'alive',
        N'Lord of House Fornax; sovereign of the Rhine-Danube territories; retired Paladin commander who has governed the House for thirty-one years',
        N'Hartmut Brenner has led House Fornax for more than three decades and the weight of those years is written in him as clearly as the Paladin augmentation that altered his frame at thirty-two. He is a methodical man — the kind who makes decisions by exhausting every alternative first, which means his decisions, once made, do not change. The furnaces and the trade routes that define Fornax power are products of his administration as much as any inherited advantage. He is not a beloved lord; he is a respected one, which he has always judged to be the more durable outcome. He drinks in the evenings. He does not sleep well. He has not told his wife why.',
        N'The fulcrum of House Fornax''s entire political architecture. Every secret in the House either originates with him or is being kept from him. He is the character through whom all other Fornax figures define their loyalty, their ambition, or their silence.',
        N'No POV assigned. Observed from outside in tight third-limited; his register is controlled and precise, the vocabulary of a man who has learned to say exactly what he means and not a syllable more.',
        N'House Fornax; Rhine-Danube territories; born to the ruling family; third child elevated to heir after Friedrich''s death',
        192, 106, N'Paladin-enhanced; broad-shouldered and dense; not natural — the result of multiple Catalyst infusions at thirty-two and thirty-eight; he moves like a heavy thing that has learned to move quietly',
        N'iron-grey', N'close-cropped', N'very short',
        N'steel-grey; the second infusion''s mark', N'pale weathered', N'deeply lined; the specific weathering of a man who spent twenty years in the field before coming inside',
        N'Evident enhancement — significant height, altered proportions, changed eyes',
        N'deliberate; when he stops moving he becomes very still; the kind of stillness that makes rooms smaller',
        N'military-formal at all times; House Fornax insignia on the left breast; he does not own clothing that is not either uniform or its civilian equivalent',
        N'Multiple Catalyst infusions; elevated bone density; altered skeletal proportions; modest height gain above first-infusion baseline; steel-grey eye color is the second infusion''s mark',
        N'He rises before the household and reads overnight dispatches in his study. He meets with the Chancellor at the seventh hour. He walks the estate perimeter at noon — not for health but to see who is where. He takes his evening meal with Lady Ilse most nights. He does not read before sleeping because he cannot stop thinking when he reads.',
        N'He approved a Scrying-coordinated strike on a border village twenty-three years ago that killed sixty-two civilians in order to secure the Pallor-Fornax trade agreement. The strike was a calculation he made correctly by his own metrics — the agreement held for eighteen years. He has never told anyone, including Ilse. The Archivist Adalbert Grunewald documented it at the time without being ordered to and has held the record without using it for two decades, which is the thing about Grunewald that Hartmut cannot decipher.',
        N'Precise and technical; almost no metaphor; the vocabulary of someone who learned to talk in military briefings and never fully expanded beyond it',
        N'Even-paced; long sentences completed fully; does not ask questions unless he already knows the answer',
        N'Almost always evaluating the other person''s leverage position relative to his own',
        N'Goes quieter; sentences shorten to three words or fewer when something has gone very wrong',
        N'Does not have one; he expresses closeness through action rather than language; Ilse has noted this for thirty years',
        N'House Fornax territory; Rhine-Danube corridor; the other Houses as political terrain, not destinations',
        0, 0,
        N'Germanic lord in his early sixties, iron-grey close-cropped hair, steel-grey Paladin eyes, broad augmented frame, military-formal House Fornax regalia, Rhine-Danube stone great hall, WW1-adjacent dark fantasy architecture, bearing of a man who has governed at scale for thirty years, Buehlman dark fantasy register',
        N'older Germanic lord, military dress, augmented broad frame, stone hall, controlled expression, dark fantasy WW1 aesthetic',
        0, 0
    );
    PRINT 'Hartmut Brenner seeded.';
END
ELSE PRINT 'Hartmut Brenner already exists.';
GO

-- 2. LADY ILSE BRENNER (née Kraft)
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ilse Brenner')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ilse Brenner', N'ilse-brenner', N'canon', 1,
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
        @id, N'Ilse Brenner', N'ilse-brenner', N'Ilse', N'Brenner', N'Lady',
        N'human', N'human', N'female', N'she/her', 58, N'alive',
        N'Lady of House Fornax; co-administrator; manages internal House diplomacy; born Ilse Kraft of House Calyx',
        N'Ilse Brenner came to House Fornax at twenty-four as part of a political settlement and has spent thirty-four years making herself indispensable rather than decorative. She manages the internal diplomatic machinery of the House — the relationships between military staff and political cabinet, family and servants, the living and the dead weight of old decisions. She is gracious in the way of a person who has had to be gracious without feeling it long enough that the expression has become genuine. She loves Hartmut in a way that has survived his distance, his silences, and her growing suspicion that there are things he has done that she does not want confirmed.',
        N'The character who understands the emotional architecture of the House and is therefore the first to sense when it shifts. Her Calyx heritage is the vector through which outside pressure enters the Fornax interior.',
        N'No POV assigned. Third-limited when present; register is warm but precise, with an undercurrent of watchfulness she has learned to keep out of her face.',
        N'Born House Calyx; married into House Fornax at twenty-four; her Calyx blood is regarded by some Fornax loyalists as a permanent asterisk on her authority',
        169, 64, N'slight; the build of a woman who was never trained for combat and has nonetheless learned to be very difficult to move',
        N'ash-brown fading to grey at the temples', N'always arranged; she has never been seen with her hair undressed', N'worn up',
        N'grey-blue', N'fair', N'smooth for her age; she is careful about this without acknowledging that she is',
        N'none',
        N'composed; does not give ground physically; the Calyx formality she was raised in reads as Fornax authority by now',
        N'House Fornax formal dress for official occasions; Calyx embroidery still present in the linings of her outer garments, visible only if she chooses to show it',
        N'none',
        N'She reads dispatches in the morning while Hartmut is in his study. She manages the visiting diplomat calendar and personally interviews new domestic staff. She visits the Dowager Walburga twice a week, which she finds both terrible and necessary. She has been writing letters to her birth family in Calyx for fifteen years, and she writes them in a hand she does not use for House correspondence.',
        N'For fifteen years she has maintained a private correspondence with her birth family in House Calyx — what began as homesickness has grown into something that includes military movement observations drawn from Hartmut''s dispatches. She tells herself it is family connection. She has not examined whether that framing is accurate. The content of her most recent letters would be characterized as intelligence exchange by any third party who read them. Her attendant Agnetha Bock has been reading them for three years and has not told anyone, because she loves Ilse and is still deciding what that means.',
        N'Warm; socially adept; educated in Calyx''s scholarly tradition but operating in Fornax''s military-administrative registers; she code-switches between them without quite realizing she does',
        N'Controlled; she finishes other people''s sentences in her head but rarely aloud',
        N'Almost always monitoring what the other person is not saying; thirty years of reading Hartmut have made her exceptional at reading everyone else',
        N'Gets warmer, not colder; the warmth becomes protective coloration',
        N'Comes from a place she has not spoken aloud about in years; it sounds like the Calyx cadence she arrived with',
        N'House Fornax estate and surrounding territory; the Calyx border as the geography of her childhood',
        0, 0,
        N'woman in her late fifties, ash-brown hair going grey at temples, worn up, grey-blue eyes, fair composed complexion, Fornax formal dress with subtle Calyx embroidery, Rhine-Danube stone interior, gracious posture that does not give ground, dark fantasy WW1-adjacent register',
        N'older woman, formal dress, ash-brown hair up, grey-blue eyes, composed posture, stone interior, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Ilse Brenner seeded.';
END
ELSE PRINT 'Ilse Brenner already exists.';
GO

-- 3. ALBRECHT BRENNER — Heir
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Albrecht Brenner')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Albrecht Brenner', N'albrecht-brenner', N'canon', 1,
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
        @id, N'Albrecht Brenner', N'albrecht-brenner', N'Albrecht', N'Brenner', N'',
        N'human', N'human', N'male', N'he/him', 33, N'alive',
        N'Heir to House Fornax; apprentice administrator; eldest child of Hartmut and Ilse; unmarried',
        N'Albrecht Brenner is thirty-three and has spent most of that time being groomed for something he does not entirely want. He is genuinely talented — perhaps the most gifted administrator in the House''s current generation — with a precise grasp of the trade route economics and furnace production schedules that fund the Living War. He has sat in on six treaty negotiations and understood all of them better than anyone gave him credit for. What he resents is not the administration. He resents that he will inherit the war itself. He has looked at the Living War''s accounting and cannot find the exit in it, and no one will discuss this with him, and he has begun to suspect no one has an answer.',
        N'The heir who sees the system clearly and does not want to perpetuate it. His intelligence makes him dangerous to the status quo; his position makes him captive to it.',
        N'No POV assigned. Third-limited; register is precise and administrative, with dry humor used to avoid saying what he actually thinks.',
        N'House Fornax; Rhine-Danube territories; eldest child of the ruling pair; Calyx heritage through his mother regarded by some as a complication',
        183, 82, N'lean; not augmented; the build of someone who has been indoors most of his life; he rounds his shoulders, which makes him look shorter than he is',
        N'dark brown', N'worn simply; no attention paid to it', N'short',
        N'grey; the same steel-grey as his father without the Paladin infusion that altered Hartmut''s', N'pale', N'clear; he does not go outside enough',
        N'none',
        N'rounds his shoulders in formal spaces; taller than he presents',
        N'working dress; rarely out of the administrative office; dresses as though appearing before his father is always possible',
        N'none',
        N'He reviews production schedules every morning before the briefing. He attends cabinet meetings as an observer except when his father waves him in as a participant, which happens without warning. He corresponds with trade partners on behalf of the Chancellor. He works late. He has noticed himself beginning to drink the way his father does and has not yet decided whether to stop.',
        N'He has been in secret correspondence with a Liturgy scholar for fourteen months, gathering documented evidence that the Liturgy''s Sphere 31 extraction process constitutes a systematic atrocity against populations who have not consented to removal. He has three volumes of testimony. He cannot expose the Liturgy without destroying the House''s relationship with its Liaison. He cannot do nothing. He has been sitting with this for over a year and the weight of it is changing the shape of his face.',
        N'Precise and technical; defaults to administrative vocabulary when uncomfortable; uses numbers when he cannot use words',
        N'Measured; does not trail off but pauses before the sentence he actually wants to say',
        N'Almost always calculating what the other person will do with the information he gives them',
        N'Gets more precise, not less; sentences become shorter and more correct',
        N'Does not have one yet; he has been too careful for too long',
        N'House Fornax estate; the trade route corridor; the political cabinet',
        0, 0,
        N'young Germanic man in his early thirties, dark brown hair worn simply, grey eyes matching his father''s, lean unaugmented build with rounded shoulders, formal administrative dress, Rhine-Danube stone study, expression of someone thinking two problems ahead, dark fantasy WW1-adjacent register',
        N'young Germanic man, dark hair, grey eyes, lean build, formal dress, stone study, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Albrecht Brenner seeded.';
END
ELSE PRINT 'Albrecht Brenner already exists.';
GO

-- 4. GERDA BRENNER — Second-born; Dame; mounted reconnaissance commander
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Gerda Brenner')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Gerda Brenner', N'gerda-brenner', N'canon', 1,
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
        @id, N'Gerda Brenner', N'gerda-brenner', N'Gerda', N'Brenner', N'Dame',
        N'human', N'human', N'female', N'she/her', 31, N'alive',
        N'Second-born of House Fornax; Dame following Catalyst infusion at twenty-six; commands the House''s mounted reconnaissance force',
        N'Gerda Brenner survived the Catalyst infusion at twenty-six and the experience did what infusions do: altered her frame, changed her eyes, and shifted something underneath that she has not found the right word for. She commands the mounted reconnaissance force with a directness her captains find easier than her father''s careful measured authority. She is better at command than Albrecht would be and everyone in the household knows it except possibly Albrecht, who is too absorbed in administrative work to take measure of what his sister is. She does not resent him for this. She resents the structure that made it irrelevant.',
        N'The second-born who is more capable than the heir and has done something interesting with that surplus rather than turning bitter. Her secret desire transforms her from a military character into something more complicated.',
        N'No POV assigned. Third-limited; register is direct and physical, oriented toward the concrete.',
        N'House Fornax; Rhine-Danube territories; second child of the ruling pair; Dame-rank following Catalyst infusion at twenty-six',
        175, 74, N'Dame-enhanced; taller than she was before infusion; the density is visible in how she moves; her frame takes up space in a way that is not entirely natural',
        N'dark auburn', N'pulled back for field work', N'shoulder-length when loose',
        N'amber; the infusion''s mark — she was born with grey eyes like her father', N'warm fair', N'weathered; she is outside more than inside',
        N'Subtle height gain, increased density',
        N'field-practical; posture of someone who expects to be listened to and is usually right',
        N'field dress when in the field; Fornax formal when required and not a moment sooner',
        N'First Catalyst infusion at twenty-six; subtle height increase; elevated bone density; amber eye color is the signature marker',
        N'She is in the stables before the sun. She leads the dawn patrol herself three days in seven. She reviews intelligence reports in the early afternoon. She is in the Scrying installation records room two or three evenings a week — a habit she has told no one about — cataloguing apparatus manuals she cannot yet act on.',
        N'She does not want command. She wants to leave the mounted corps and become a Scrying operator — not as an observer but as someone who understands technically what the apparatus is actually doing. She has been studying the apparatus manuals for three years. The Head Operator Theodor Nacht discovered her reading them eight months ago and has been tutoring her after midnight rather than reporting it. She does not know what she will do when the choice between her corps and this becomes unavoidable.',
        N'Direct; field vocabulary; finds administrative language faintly suspect',
        N'Fast; says the first correct thing rather than the most careful one',
        N'Mostly what she actually means; has not learned to say less than she thinks',
        N'Goes to action rather than words; if words are required, becomes very short',
        N'Genuine and disarming; she just becomes more honest, which is either better or worse depending on the other person',
        N'Rhine-Danube frontier; House Fornax territory and the border approaches; the Scrying installation',
        0, 0,
        N'young Germanic woman in her early thirties, dark auburn hair pulled back, amber Dame-infusion eyes, Dame-enhanced taller frame, field dress, Rhine-Danube border landscape, physical authority, Buehlman dark fantasy register',
        N'young woman, dark auburn hair, amber eyes, field dress, Dame-enhanced build, border landscape, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Gerda Brenner seeded.';
END
ELSE PRINT 'Gerda Brenner already exists.';
GO

-- 5. KLAUS BRENNER — Youngest child; being shaped for political marriage
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Klaus Brenner')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Klaus Brenner', N'klaus-brenner', N'canon', 1,
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
        @id, N'Klaus Brenner', N'klaus-brenner', N'Klaus', N'Brenner', N'',
        N'human', N'human', N'male', N'he/him', 19, N'alive',
        N'Youngest child of House Fornax; being shaped for a political marriage to House Atrament',
        N'Klaus Brenner is nineteen and knows exactly what is being planned for him. He has been told this is an honor. He does not experience it as an honor. He is not political by nature — he is artistic, observant, and easily hurt in the way of people who have not yet built the scar tissue that protects them. He plays a stringed instrument badly and has been working at it for seven years because it is the one thing in the house that belongs entirely to him. He is being shaped for a marriage to a member of House Atrament he has not met, and his parents regard this as sensible. Klaus regards it as a sentence.',
        N'The youngest child who does not fit the mold and is being fitted anyway. His relationship with the page Anton Wirth is the most dangerous secret in the household for both of them.',
        N'No POV assigned. Third-limited; register of someone always choosing between what he actually thinks and what is safe to say.',
        N'House Fornax; Rhine-Danube territories; youngest child of the ruling pair',
        178, 68, N'slim; not yet fully grown into himself; will be striking in five years and is currently awkward about it',
        N'dark brown', N'carelessly maintained; not a statement, just inattention', N'short',
        N'grey', N'pale', N'clear; he blushes easily, which he finds mortifying',
        N'none',
        N'careful in formal spaces; natural and open in the music room or the stables when not watched',
        N'whatever has been laid out for him; he does not make clothing choices; this is itself a quiet form of resistance',
        N'none',
        N'He attends his lessons with the Librarian Sigrun Fels. He practices his instrument for two hours every day unless formal demands intrude. He finds reasons to be in the courtyard when the pages are running dispatches. He is learning Atrament customs from a text the Chancellor gave him and finds the exercise depressing.',
        N'He has been in love with the page Anton Wirth since they were twelve years old, and Anton knows and returns it. Both understand this cannot happen — Klaus is being married for political reasons and Anton is a page whose position depends on the Lord''s goodwill. Klaus has been quietly planting requests in diplomatic correspondence to secure Anton a posting at a foreign estate, putting him out of reach before anything forces a confrontation. He is nineteen and has already become practiced at one specific kind of political maneuvering.',
        N'Careful in formal settings; unguarded in private; vocabulary of books rather than administration or war',
        N'Uneven; fast when comfortable, halting when not',
        N'Always measuring whether what he wants to say is safe in this particular room with this particular person',
        N'Goes very still and very polite; every word becomes deliberate',
        N'Completely unguarded with Anton; never had to perform in that one relationship',
        N'House Fornax estate; his world is small and he is aware of its walls',
        0, 0,
        N'young Germanic man of nineteen, dark brown hair carelessly kept, grey eyes, slim build, formal dress sitting awkwardly on him, Rhine-Danube stone estate interior, expression of someone always thinking about something else, dark fantasy WW1-adjacent register',
        N'young man, nineteen, dark hair, grey eyes, slim build, formal dress, stone interior, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Klaus Brenner seeded.';
END
ELSE PRINT 'Klaus Brenner already exists.';
GO

-- 6. DOWAGER LADY WALBURGA BRENNER (née Stein)
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Walburga Brenner')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Walburga Brenner', N'walburga-brenner', N'canon', 1,
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
        @id, N'Walburga Brenner', N'walburga-brenner', N'Walburga', N'Brenner', N'Dowager Lady',
        N'human', N'human', N'female', N'she/her', 84, N'alive',
        N'Dowager Lady of House Fornax; Hartmut''s mother; has been present for every significant House decision for sixty-six years',
        N'Walburga Brenner is eighty-four years old and occupies the tower rooms above the east wing, where she sits by the window and receives visitors at her own convenience and no one else''s. She has been present for every significant decision House Fornax has made since she married into it at eighteen. She has watched five Lords govern and been useful to all of them. She is not senile. She is not frail. She has the look of a woman who has waited a very long time for something and has the patience of someone who has won by waiting before. She takes her meals alone. She has not spoken to Hartmut at length in three years.',
        N'The oldest living knowledge-holder in the House. Everything suppressed or forgotten, she remembers. She is simultaneously the House''s institutional memory and its most dangerous liability.',
        N'No POV assigned. Observed from outside; her register is the register of someone who does not need to perform anything for anyone anymore.',
        N'House Fornax; Rhine-Danube territories; born Walburga Stein; married into the ruling family sixty-six years ago',
        158, 49, N'slight; age has reduced her frame but not her presence',
        N'white; was dark brown', N'always arranged; she has not had an undressed hair day in sixty years', N'worn up',
        N'dark brown; the only feature that has not aged', N'pale', N'deeply lined; the specific lines of someone who has spent eighty-four years controlling her expression',
        N'none',
        N'absolutely still; she does not waste movement; visitors come to her',
        N'Fornax formal dress in the colors of her husband''s generation; she has not updated her wardrobe in fifteen years and this is a choice',
        N'none',
        N'She receives the House physician on Mondays. She receives Lady Ilse on Tuesdays and Fridays. She eats alone. She reads correspondence that Grosse brings her because she asked for it. She keeps her own records in a hand that has not changed since she was forty. She does not sleep much.',
        N'Hartmut is not her husband''s biological son. The previous Lord''s true heir was Friedrich, who died in battle. What Walburga knows, and has kept for sixty years, is that Hartmut was the child of her husband''s younger brother, given to them when that brother''s marriage was politically inconvenient. She agreed to the arrangement. She raised Hartmut as her own. She has never told him. The Archivist Grunewald knows; she does not know how. She has been waiting for him to use it for forty years and he has not, and this is the thing about Adalbert Grunewald she does not understand.',
        N'Sparse; does not explain herself; every sentence is complete and final',
        N'Deliberate; does not speak until she has decided what she wants the other person to do afterward',
        N'Always asking what the person in front of her is afraid of; eight decades of practice answering this',
        N'Does not register pressure in the way younger people do; she has been making the decisions other people feel pressure about for forty years',
        N'Does not have one currently; was intimate with her husband and that ended forty years ago',
        N'The tower rooms; the estate; she has not left the building in two years',
        0, 0,
        N'very old Germanic woman of eighty-four, white hair worn up in the style of sixty years ago, dark brown eyes unchanged by age, slight frame with absolute stillness, formal Fornax dress in an older fashion, stone tower room with a window, expression of someone waiting, Buehlman dark fantasy register',
        N'very old woman, white hair up, dark eyes, slight frame, old formal dress, stone tower room, still expression, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Walburga Brenner seeded.';
END
ELSE PRINT 'Walburga Brenner already exists.';
GO

-- 7. FRIEDRICH BRENNER — DEAD; elder brother; legacy still shapes everything
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Friedrich Brenner')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Friedrich Brenner', N'friedrich-brenner', N'canon', 1,
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
        @id, N'Friedrich Brenner', N'friedrich-brenner', N'Friedrich', N'Brenner', N'',
        N'human', N'human', N'male', N'he/him', 40, N'dead',
        N'Deceased elder son of House Fornax; died in battle twenty-five years ago at forty; his death placed Hartmut in the succession',
        N'Friedrich Brenner died at forty, in the third year of the eastern campaign, in an engagement the official record describes as a forward skirmish with House Draught cavalry. He was the true heir to House Fornax. By most accounts of people who were present, he was a better candidate for the lordship than his younger brother. He was also aware of certain things about the House''s lineage that he had learned from records no one was supposed to read, and he had an argument with Hartmut''s father the night before he rode out, and the butler Ruprecht was close enough to hear what the argument was about, and Friedrich rode out the next morning and did not come back. He has been dead for twenty-five years. His portrait in the upper corridor is the most honest face in the building.',
        N'The dead weight that shapes every living person''s position. His death was convenient enough that convenience is itself a character in his story. His absence is the fulcrum of the Dowager''s secret, the cousin Oskar''s knowledge, and the Archivist''s oldest file.',
        N'No POV. Deceased. Referenced in retrospect by other characters.',
        N'House Fornax; Rhine-Danube territories; eldest son of the ruling family by blood; died at forty without a direct heir',
        186, 90, N'Paladin-enhanced; was taller and broader than Hartmut; the portrait shows this clearly',
        N'dark brown; gone grey at the temples by the end', N'formal when in the house', N'short',
        N'brown', N'pale', N'clear until the final years; he was not sleeping well toward the end, which people remember now as portentous',
        N'Evident enhancement — significant height, altered proportions, changed eyes',
        N'was direct and unguarded in a way that was read as confidence and may have been something else',
        N'military formal; he was always dressed for the field even when not going to it',
        N'Multiple Catalyst infusions; Paladin-rank augmentation at time of death',
        N'He is not alive. His daily life ended twenty-five years ago. What remains is the record and the portrait.',
        N'Friedrich knew that Hartmut was not their father''s biological son. He had found the correspondence that documented the arrangement and confronted his father with it the night before he rode out. He was told the arrangement was final and that raising the matter again would have consequences. He rode into an engagement that should have been a minor patrol action. The question of whether the engagement was minor when he left, or only became fatal because of decisions made after he left, has never been formally examined. The Archivist has the correspondence. The cousin Oskar Brenner-Volk was in the field that day.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Deceased; Rhine-Danube territories in life',
        0, 0,
        N'Germanic man of forty, dark brown hair going grey at temples, brown eyes, Paladin-enhanced broad frame, military formal dress, formal portrait in a dark stone corridor, the expression of someone who died knowing something, dark fantasy WW1-adjacent register',
        N'Germanic man in his forties, military dress, augmented frame, formal portrait, stone corridor, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Friedrich Brenner seeded.';
END
ELSE PRINT 'Friedrich Brenner already exists.';
GO

-- 8. OSKAR BRENNER-VOLK — Cousin; Paladin; siege corps commander
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Oskar Brenner-Volk')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Oskar Brenner-Volk', N'oskar-brenner-volk', N'canon', 1,
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
        @id, N'Oskar Brenner-Volk', N'oskar-brenner-volk', N'Oskar', N'Brenner-Volk', N'',
        N'human', N'human', N'male', N'he/him', 45, N'alive',
        N'Cousin of the ruling family; Paladin; commands the House Fornax siege corps',
        N'Oskar Brenner-Volk carries the hyphenated name of the marriage that produced him — a cousin of the line, useful enough to elevate, close enough to trust with the siege equipment. He is a Paladin, which means he has come through the Catalyst twice and the second time changed him more than the first: his eyes went from grey to a flat amber, and something about his physical proportion shifted in a way he notices in mirrors and does not discuss. He commands the siege corps with a technical precision the engineers respect and the infantry finds excessive. He drinks beer with his sergeants after field exercises. He has not told anyone — not once in twenty-five years — what he saw on the eastern patrol the day Friedrich Brenner died.',
        N'The keeper of the most dangerous political secret in the House''s history. His loyalty to Hartmut is based entirely on shared silence, and he is old enough to be tired of what silence costs.',
        N'No POV assigned. Third-limited; his register is military-practical, slightly flat, the register of someone who has learned that words create records.',
        N'House Fornax; Rhine-Danube territories; cousin of the ruling family through the Brenner-Volk branch; Paladin-rank',
        194, 114, N'Paladin-enhanced; the second infusion added mass and a slight forward tilt to the shoulders that is not posture — it is structure',
        N'grey with broad silver streaks', N'close-cropped', N'very short',
        N'flat amber; the second infusion''s mark', N'weathered pale', N'marked; twenty years in the field',
        N'Evident enhancement — significant height, altered proportions, changed eyes',
        N'deliberate; the posture of a man carrying more than his physical weight',
        N'field dress or military formal; never anything in between',
        N'Multiple Catalyst infusions; Paladin-rank; altered skeletal proportions; flat amber eye color is the second infusion''s signature',
        N'He is in the field most of the time. When at the estate he reviews siege equipment maintenance logs, attends military briefings, and eats with his officers. He does not visit the Dowager. He has avoided being alone with Hartmut for three years because the conversation they have not had has been waiting that long.',
        N'He was on the eastern patrol the day Friedrich died. He saw the orders Friedrich received — they described a minor skirmish with minimal Draught presence. What Oskar arrived at was a forward engagement with a full Draught cavalry unit. He has never been able to prove the orders were changed or by whom. He knows that Hartmut had a strong motive. He knows that saying this out loud ends his career, his family''s position, and possibly his life. He has been carrying this for twenty-five years and it is the only thing he thinks about when he wakes at three in the morning.',
        N'Sparse; military vocabulary; does not volunteer information',
        N'Slow and final; each sentence arrives like a judgment',
        N'Always deciding whether this conversation is one he can walk away from',
        N'Goes formally polite in a way that is more alarming than anger',
        N'Does not exist; he burned that capacity in the eastern campaign',
        N'Rhine-Danube frontier; the siege corps positions; the estate when required',
        0, 0,
        N'Germanic man in his mid-forties, close-cropped grey-silver hair, flat amber Paladin eyes, augmented heavy frame with forward-shifted shoulders, military field dress, Rhine-Danube stone estate or field headquarters, expression of someone carrying something he will never set down, Buehlman dark fantasy register',
        N'heavily built man, military dress, silver-grey hair, amber eyes, augmented frame, stone setting, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Oskar Brenner-Volk seeded.';
END
ELSE PRINT 'Oskar Brenner-Volk already exists.';
GO

-- 9. HILDE KRENZ (née Brenner) — Cousin who married out and returned
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Hilde Krenz')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Hilde Krenz', N'hilde-krenz', N'canon', 1,
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
        @id, N'Hilde Krenz', N'hilde-krenz', N'Hilde', N'Krenz', N'',
        N'human', N'human', N'female', N'she/her', 42, N'alive',
        N'Fornax cousin who married into House Pallor; widowed at thirty-eight; returned to Fornax with intelligence she has not formally reported',
        N'Hilde Brenner married a Pallor officer named Albrecht Krenz at twenty-five, moved to Pallor territory, and spent thirteen years understanding how the other side of the channel thinks. Her husband died four years ago. She came back to House Fornax because there was nowhere else for a Fornax-born widow to go, and because she had spent four years in Pallor learning things she has not decided what to do with. She is not officially a spy. She has never been briefed, paid, or directed. She simply lived in another House for thirteen years and is very intelligent, and the result is a comprehensive picture of Pallor''s military and political architecture. She has not provided it to the Spymaster. She is deciding what she wants in return.',
        N'The wildcard — loyalty claims on two Houses, operational knowledge of one. Her return creates problems for everyone who thought they understood the Fornax political map.',
        N'No POV assigned. Third-limited; her register is composed and slightly oblique, the register of someone who has learned to hold information without letting it show.',
        N'Born House Fornax; married into House Pallor; widowed and returned; her Pallor years make her loyalty complicated',
        166, 61, N'practical; not a fighter but moves like someone who assesses a room on entry',
        N'dark brown', N'worn simply; the Pallor fashion, slightly different from Fornax', N'below shoulder',
        N'dark grey', N'fair', N'composed; slightly sun-marked from the Pallor coast',
        N'none',
        N'watchful; enters rooms slowly; sits with her back to walls when she can arrange it without being obvious',
        N'Fornax dress with Pallor tailoring influences she has not shed; this marks her as someone in-between',
        N'none',
        N'She attends formal meals when expected. She spends most of her time in the library. She walks the estate perimeter at intervals irregular enough not to be a patrol but regular enough to be surveillance. She has met twice with the Spymaster Marta Scholl, neither meeting initiated by Hilde, neither resulting in anything actionable.',
        N'Her husband did not die in battle. Hilde discovered at thirty-seven that Albrecht Krenz had been selling House Fornax military movement intelligence to House Draught. She confronted him. The confrontation was private. He died before it ended. She did not report this to Pallor authorities because explaining how she found out would have required explaining what she had been doing in the intervening years, which was also not standard spousal behavior. She has been carrying a clean death for four years and the knowledge that she killed her husband for betraying the House she was born into.',
        N'Pallor-inflected; Fornax-born vocabulary with thirteen years of another House''s idiom layered over it',
        N'Deliberate; never speaks first in a room she does not control',
        N'Almost always deciding what the other person knows and whether telling them the next thing costs her',
        N'Gets warmer; the specific warmth of someone using social ease as deflection',
        N'Genuine and unguarded; she has one person she trusts and it is no one in this house',
        N'House Fornax estate; she has not left the territory since returning',
        0, 0,
        N'Germanic woman in her early forties, dark brown below-shoulder hair, dark grey eyes, practical build, Fornax-Pallor mixed dress, Rhine-Danube stone estate interior, watchful entry posture, Buehlman dark fantasy register',
        N'woman in her forties, dark hair, grey eyes, mixed House dress, stone interior, watchful expression, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Hilde Krenz seeded.';
END
ELSE PRINT 'Hilde Krenz already exists.';
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- POLITICAL CABINET (10–16)
-- ─────────────────────────────────────────────────────────────────────────────

-- 10. REINHARD KESSLER — Chancellor
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Reinhard Kessler')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Reinhard Kessler', N'reinhard-kessler', N'canon', 1,
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
        @id, N'Reinhard Kessler', N'reinhard-kessler', N'Reinhard', N'Kessler', N'',
        N'human', N'human', N'male', N'he/him', 55, N'alive',
        N'Chancellor of House Fornax; manages all political correspondence, negotiations, and alliances; the true architect of Fornax trade dominance',
        N'Reinhard Kessler has been Chancellor for nineteen years and during that time has turned House Fornax''s trade route advantage from a geographic fact into a political weapon every other House depends on and resents. He is not brilliant in the way that announces itself; he is brilliant in the way that makes other people feel they thought of the solution themselves. He does not shout. He does not threaten. He sends a correctly-worded letter at the correct moment and waits for the other party to understand what they have agreed to. He is indispensable to Hartmut and knows this, and has been using that knowledge so carefully for so long that Hartmut has not yet noticed.',
        N'The man who actually runs Fornax''s political relationships and has been running a private side arrangement for twenty years. His exposure would be catastrophic.',
        N'No POV assigned. Third-limited; his register is the register of someone who is always the most informed person in the room and has learned to perform being only partially informed.',
        N'House Fornax; Rhine-Danube territories; career administrator; no notable family connections',
        178, 84, N'soft; the build of a man who has not lifted anything heavier than a dispatch case in twenty years; carries it with authority',
        N'silver-brown', N'perfectly maintained', N'short',
        N'pale blue', N'fair', N'unmarked; he has never been in a field',
        N'none',
        N'perfectly still; he has the posture of a man who has spent thirty years at a desk and is at peace with it',
        N'formal Fornax administrative dress at all times; always correctly dressed for the occasion; has never been seen in disarray',
        N'none',
        N'He reads overnight dispatches before anyone else is awake. He drafts all significant correspondence personally. He meets with the Trade Ambassador, the Spymaster, and the Liturgy Liaison on a rotating schedule. He maintains relationships with chancellors in every other House through correspondence that goes into the official record and through correspondence that does not.',
        N'For twenty years he has been skimming the treaty negotiations — not money, but information. He extracts intelligence from Fornax diplomatic proceedings and routes it to House Atrament''s diplomatic corps. Not for ideology. Not for payment. Because House Atrament''s Chancellor is the most intellectually formidable administrator he has ever encountered, and Kessler wants to be in correspondence with someone who can see what he sees. He has been feeding a rival House''s intelligence apparatus in exchange for the experience of being understood. He is aware this would end him.',
        N'Precise and formal; every word selected; vocabulary of someone who has read every treaty in the archive',
        N'Measured; each sentence has been constructed before it is spoken',
        N'Almost always presenting a position that is not his actual position',
        N'Gets more formal, not less; the language becomes archaic under pressure',
        N'Does not have one; intimacy is a vulnerability he has not permitted himself',
        N'House Fornax territory; the diplomatic circuits of all seven Houses as correspondence terrain',
        0, 0,
        N'Germanic man in his mid-fifties, silver-brown hair perfectly maintained, pale blue eyes, soft build carried with authority, formal Fornax administrative dress, Rhine-Danube stone study filled with correspondence, the expression of someone who already knows what you are about to say, dark fantasy WW1-adjacent register',
        N'man in his fifties, formal administrative dress, silver-brown hair, stone study with papers, composed expression, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Reinhard Kessler seeded.';
END
ELSE PRINT 'Reinhard Kessler already exists.';
GO

-- 11. MARTA SCHOLL — Spymaster; Knight
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Marta Scholl')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Marta Scholl', N'marta-scholl', N'canon', 1,
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
        @id, N'Marta Scholl', N'marta-scholl', N'Marta', N'Scholl', N'',
        N'human', N'human', N'female', N'she/her', 48, N'alive',
        N'Spymaster of House Fornax; Knight; runs intelligence operations against all six other Houses and knows everything about the Sphere 31 extraction program',
        N'Marta Scholl runs the House''s intelligence apparatus with the methodical patience that defines Fornax at its best, and she has something the Furnaces don''t: she is also a Knight, which means she survived the Catalyst and the physical change is subtle but present — slightly taller than she was, slightly denser, the kind of alteration you notice only if you knew her before. She has managed assets across three Houses simultaneously for eleven years. She does not communicate more than necessary. She knows things about every person in this file that they have not told anyone, which is both her professional strength and the reason she has not had a genuinely honest conversation in six years.',
        N'The intelligence holder. She knows too much to be comfortable with anyone, and her own secret is the only one that could actually end her.',
        N'No POV assigned. Third-limited; her register is sparse and evaluative.',
        N'House Fornax; Rhine-Danube territories; Knight-rank following Catalyst infusion at thirty-one',
        171, 68, N'Knight-enhanced; slightly taller and denser than before infusion; the change is subtle and she has used the subtlety deliberately',
        N'dark brown', N'worn simply; no vanity', N'short',
        N'hazel', N'warm fair', N'composed; not weathered; she is rarely in the field herself',
        N'Subtle height gain, increased density',
        N'economical; she does not signal her awareness of a room because she has already processed it',
        N'practical dress that reads as administrative until you notice it would not hinder movement',
        N'First Catalyst infusion at thirty-one; modest height gain; increased bone density; the change is subtle and she has kept it that way',
        N'She reads overnight intelligence summaries in her private office. She meets assets through intermediaries she has been cultivating for years. She writes no names in any document. She knows about the Chancellor''s arrangement with House Atrament and has not told Hartmut because she is still determining what to do with it.',
        N'She has a Sphere 31 asset she has never reported to Lord Hartmut: a former Liturgy functionary who escaped the institution three years ago and came to her rather than to the Liturgy''s recovery teams. They are in a relationship she has not classified to herself in language yet. She knows this compromises her position completely. The asset has information about the Liturgy''s extraction protocols that is genuinely valuable. She has not extracted it formally because extracting it formally would require acknowledging the asset exists.',
        N'Sparse; says the minimum that achieves the result; no filler',
        N'Even and slow; she is never in a hurry to finish a sentence',
        N'Always evaluating what the other person is giving away without knowing it',
        N'Does not change; pressure reads as ordinary conversation to her',
        N'Exists with one person only and she has not examined what that means for her operational judgment',
        N'House Fornax territory; the intelligence networks of all seven Houses as operational space',
        0, 0,
        N'Germanic woman in her late forties, dark brown short hair, hazel eyes, Knight-enhanced build, practical administrative dress, Rhine-Danube stone intelligence office, economical posture, expression that gives nothing, Buehlman dark fantasy register',
        N'woman in her forties, dark hair, hazel eyes, practical dress, stone office, composed expression, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Marta Scholl seeded.';
END
ELSE PRINT 'Marta Scholl already exists.';
GO

-- 12. ADALBERT GRUNEWALD — House Archivist
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Adalbert Grunewald')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Adalbert Grunewald', N'adalbert-grunewald', N'canon', 1,
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
        @id, N'Adalbert Grunewald', N'adalbert-grunewald', N'Adalbert', N'Grunewald', N'',
        N'human', N'human', N'male', N'he/him', 67, N'alive',
        N'House Archivist; maintains all records, genealogy, treaty texts, and Scrying logs; has served the House for forty years under two Lords',
        N'Adalbert Grunewald has maintained the House Fornax archive for forty years, and in that time the archive has become indistinguishable from the inside of his mind. He knows where every document is. He knows what every document means. He knows which documents are missing and what their absence proves. He is a small, careful man who smells of paper and takes up no space and is unfailingly courteous to everyone from the Lord to the kitchen assistant, which means that people speak freely in front of him and have for four decades. He has not used a single piece of information he has gathered in a way anyone has ever detected.',
        N'The man who knows everything and has done nothing with it — yet. His indecision is the most dangerous active force in the House.',
        N'No POV assigned. Third-limited; his register is archival, precise, and mildly warm; he is genuinely kind, which makes him harder to read than someone who is not.',
        N'House Fornax; Rhine-Danube territories; career archivist; no notable family connections; has served under the previous Lord and Hartmut',
        164, 58, N'slight and precise; moves economically; takes up very little physical space',
        N'white; was sandy brown', N'neat but unremarkable', N'short',
        N'pale grey', N'fair', N'fine; he has spent forty years indoors',
        N'none',
        N'moves quietly; the habit of forty years in a room where silence is the default',
        N'archival dress; practical; always ink-stained at the fingers',
        N'none',
        N'He arrives at the archive before the household wakes. He catalogs, cross-references, and files. He reads every document that passes through the House before it is filed, which means he reads everything. He takes a noon meal at his desk. He walks to the Scrying installation once a week to collect the observation logs. He has been doing this for forty years and has not taken a holiday in twelve.',
        N'He has documented two things that could end House Fornax as currently constituted: the village massacre Hartmut ordered twenty-three years ago, and the genealogical proof that Hartmut is not the previous Lord''s biological son. Both records exist in a cipher system he devised himself. He encrypted them the same week he discovered them. He has been holding them for two decades without using them, not because he is afraid of consequences but because he genuinely cannot decide whether using them would serve justice or only chaos. This indecision is the defining fact of his life and he is sixty-seven years old.',
        N'Archival and precise; references specific documents by date and catalog number in casual conversation; quotation from memory is his primary rhetorical mode',
        N'Unhurried; he never finishes a sentence before he means to',
        N'Almost always providing more information than was asked for in a way that makes you realize you asked the wrong question',
        N'Goes quieter and more formal; begins citing precedent',
        N'Warm and genuine; he is one of the few people in the House who does not perform his intimacy',
        N'The archive; the House estate; the Scrying installation once weekly',
        0, 0,
        N'slight Germanic man in his late sixties, white hair, pale grey eyes, ink-stained fingers, archival working dress, Rhine-Danube stone archive filled with documents and shelves, economical movement, the expression of someone who has read everything and filed it, dark fantasy WW1-adjacent register',
        N'slight older man, white hair, archival dress, ink-stained hands, stone archive with shelves, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Adalbert Grunewald seeded.';
END
ELSE PRINT 'Adalbert Grunewald already exists.';
GO

-- 13. HEDWIG PFEIFFER — Trade Ambassador
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Hedwig Pfeiffer')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Hedwig Pfeiffer', N'hedwig-pfeiffer', N'canon', 1,
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
        @id, N'Hedwig Pfeiffer', N'hedwig-pfeiffer', N'Hedwig', N'Pfeiffer', N'',
        N'human', N'human', N'female', N'she/her', 44, N'alive',
        N'Trade Ambassador of House Fornax; manages commercial relationships with all other Houses and border markets',
        N'Hedwig Pfeiffer has spent twenty years negotiating trade agreements on behalf of House Fornax and she is very good at it — good enough that the other Houses'' ambassadors have started bringing their best people to meetings with her, which she regards as a compliment and a useful intelligence source simultaneously. She is direct in the Fornax manner but warmer than most Fornax administrators, which is a studied warmth rather than a natural one, and she knows the difference even if her counterparts do not. She is currently negotiating agreements with three Houses at once and has not told the Chancellor she is doing so, because the Chancellor would ask questions about one of them.',
        N'The person managing House Fornax''s commercial lifeline and quietly running a private arrangement underneath it. Her exposure would rupture a major relationship.',
        N'No POV assigned. Third-limited; register is warm and mercantile, the register of someone who has learned to make every negotiation feel personal.',
        N'House Fornax; Rhine-Danube territories; career trade administrator; minor family connections in the merchant class',
        165, 63, N'practical; the build of someone who travels frequently',
        N'auburn', N'pinned up for formal meetings; loose in transit', N'shoulder-length',
        N'brown', N'warm fair', N'composed; she has the complexion of someone who sleeps well, which she does not',
        N'none',
        N'moves comfortably through formal spaces; she has been in rooms like this across seven Houses',
        N'formal trade dress; always correct for the specific House she is meeting; maintains a wardrobe sorted by diplomatic context',
        N'none',
        N'She holds briefings with the Chancellor on the official trade situation. She reads the merchant reports from the border markets. She drafts offers and counter-offers. She maintains a private correspondence channel that does not go through the official Fornax dispatch system.',
        N'She has been negotiating a separate trade agreement with House Lacerta that would create a bypass route around the central Fornax trade arteries — a route that would benefit her personally through a private arrangement with the Lacerta merchant guild. She tells herself this is entrepreneurship rather than malfeasance. She is not sure this framing holds. She has not told the Chancellor or Lord Hartmut. If the agreement completes, she will be wealthy enough to stop. She has been telling herself she will stop for three years.',
        N'Mercantile; warm; calibrated to the register of whoever she is speaking with',
        N'Paced for the other person; she matches cadences deliberately',
        N'Almost always working out what the other person needs to feel before they will agree to what she wants them to do',
        N'Gets warmer; the warmth becomes an offer she is making',
        N'Genuine; she actually likes people, which is rare in her role and is her greatest advantage',
        N'House Fornax territory; the trade routes of all seven Houses; border markets',
        0, 0,
        N'Germanic woman in her mid-forties, auburn shoulder-length hair, brown eyes, practical build for travel, formal trade dress, Rhine-Danube stone meeting chamber, composed expression calibrated for diplomacy, dark fantasy WW1-adjacent register',
        N'woman in her forties, auburn hair, formal trade dress, meeting chamber, composed expression, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Hedwig Pfeiffer seeded.';
END
ELSE PRINT 'Hedwig Pfeiffer already exists.';
GO

-- 14. BROTHER ULRICH DAMM — Liturgy Liaison
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ulrich Damm')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ulrich Damm', N'ulrich-damm', N'canon', 1,
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
        @id, N'Ulrich Damm', N'ulrich-damm', N'Ulrich', N'Damm', N'Brother',
        N'human', N'human', N'male', N'he/him', 51, N'alive',
        N'Liturgy Liaison attached to House Fornax; officially represents the Liturgy''s interests to the House and the House''s interests to the Liturgy; his actual loyalty is neither',
        N'Brother Ulrich Damm has been attached to House Fornax for nine years, long enough that the household has forgotten he is not quite of them. He reports to the Liturgy. He also reports to the House. He has been managing the space between these two obligations for nine years by giving each party slightly less than the full truth and making sure the gaps don''t align. He is professionally pleasant, theologically conservative, and deeply unhappy in a way he attributes to the demands of his position and that actually has a more specific source. He attends every Transmutation infusion he is permitted to observe. He takes very careful notes.',
        N'The representative of the institution that controls the membrane and the extraction program. His secret gives him leverage over the Liturgy that he is too afraid to use.',
        N'No POV assigned. Third-limited; his register is clerical and careful, with the specific quality of someone performing certainty about things he is no longer certain of.',
        N'The Liturgy; attached to House Fornax; no House affiliation by Liturgy rule',
        173, 76, N'medium; the build of a man who has been sedentary for two decades',
        N'grey-brown', N'short and neat', N'short',
        N'brown', N'pale', N'tired; the complexion of someone who has not been sleeping well for years',
        N'none',
        N'composed; the posture of institutional training; slightly stiff',
        N'Liturgy robes with House Fornax guest markings; he is always correctly marked as guest-affiliated rather than House-affiliated',
        N'none',
        N'He attends the daily House briefing as an observer. He files weekly reports to the Liturgy through their official dispatch system. He attends every Transmutation infusion the Corps commander will allow him to observe. He spends his evenings in his room with documents he does not leave where they can be found.',
        N'He has assembled documented evidence that the Liturgy has been deliberately botching Transmutation infusions in Houses that resist the Liturgy''s extraction quotas — adjusting Catalyst preparation protocols to increase the mortality rate as a political pressure tool. He obtained this evidence through nine years of observing infusions and cross-referencing mortality rates against Liturgy-House political correspondence. The evidence is real, detailed, and conclusive. He is too afraid to use it and too principled to destroy it. He has been carrying it for three years and the carrying has begun to show in his face.',
        N'Clerical; formal Liturgy vocabulary layered over ordinary speech; uses institutional language when uncomfortable',
        N'Even and unhurried; the pacing of prepared remarks even in casual conversation',
        N'Almost always managing how much the other person knows about what he knows',
        N'Goes more formal and Liturgy-inflected; the institutional voice takes over',
        N'Does not have one; he stopped permitting it when he obtained the evidence',
        N'House Fornax estate; Liturgy channels by correspondence; the installation infirmary when observing',
        0, 0,
        N'Germanic man in his early fifties, grey-brown hair, brown eyes, tired complexion, Liturgy robes with House Fornax guest markings, Rhine-Danube stone estate interior, composed posture with institutional stiffness, the expression of someone who has been carrying something heavy for a long time, dark fantasy WW1-adjacent register',
        N'man in his fifties, Liturgy robes, formal posture, stone interior, tired expression, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Ulrich Damm seeded.';
END
ELSE PRINT 'Ulrich Damm already exists.';
GO

-- 15. DIETRICH BAUER — Treasurer / Chamberlain
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Dietrich Bauer')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Dietrich Bauer', N'dietrich-bauer', N'canon', 1,
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
        @id, N'Dietrich Bauer', N'dietrich-bauer', N'Dietrich', N'Bauer', N'',
        N'human', N'human', N'male', N'he/him', 53, N'alive',
        N'Treasurer of House Fornax; manages all House finances and resource allocation',
        N'Dietrich Bauer has managed the House''s finances for sixteen years. He is meticulous, loyal, and visibly aging in a way that has accelerated in the last six months. He presents the financial reports at the cabinet meetings with the same controlled competence he has always shown, which means no one has noticed that the reports have become carefully partial. He takes his noon meal alone. He has been doing the same arithmetic every evening in his private ledger that he has been doing every evening for six months, and the answer is always the same, and the answer is unacceptable.',
        N'The man who knows the House is technically insolvent and is the only one who knows. His choices about when and how to reveal this will determine whether the House survives the revelation.',
        N'No POV assigned. Third-limited; his register is financial and precise; increasingly strained under the surface.',
        N'House Fornax; Rhine-Danube territories; career financial administrator',
        175, 79, N'medium; has lost weight in the past six months in a way several people have noticed',
        N'brown going grey', N'neat', N'short',
        N'brown', N'fair', N'strained; he looks like someone who has not slept well since spring',
        N'none',
        N'composed but increasingly tight; the posture of a man braced against something',
        N'formal administrative dress; slightly less well-maintained than it was two years ago',
        N'none',
        N'He reviews the accounts every morning and every evening. He prepares the financial reports for cabinet meetings. He corresponds with the furnace production supervisors and the trade route administrators. He has been quietly seeking emergency liquidity through back channels for six months without success.',
        N'He discovered six months ago that House Fornax is technically insolvent — the furnace output projections that underpin the House''s credit have been falsified for eleven years by a previous administrator who has since retired to the country. The House''s actual production capacity is seventeen percent below the projected figures used to secure its current debt obligations. If the creditors audit, the House falls. He has not told Hartmut because he has been trying to solve it first. He has not been able to solve it. He is running out of time and the weight of it is visible in his face.',
        N'Financial and precise; speaks in quantities and percentages; loses the vocabulary when genuinely frightened',
        N'Measured; has always been measured; the measurement has become effortful',
        N'Almost always calculating consequences two steps ahead of the current moment',
        N'Goes very still; the sentences become formal and short; he is frightened of what he sounds like under pressure',
        N'Was warm once; is not currently accessible to warmth',
        N'House Fornax estate; the furnace districts; the creditor networks',
        0, 0,
        N'Germanic man in his early fifties, brown hair going grey, brown eyes, slightly reduced build, formal administrative dress showing slight disarray, Rhine-Danube stone accounting office, posture of someone braced against something, dark fantasy WW1-adjacent register',
        N'man in his fifties, formal dress, stone office, strained expression, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Dietrich Bauer seeded.';
END
ELSE PRINT 'Dietrich Bauer already exists.';
GO

-- 16. BRUNHILDE STERNFELD — Ambassador; currently posted at House Atrament
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Brunhilde Sternfeld')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Brunhilde Sternfeld', N'brunhilde-sternfeld', N'canon', 1,
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
        @id, N'Brunhilde Sternfeld', N'brunhilde-sternfeld', N'Brunhilde', N'Sternfeld', N'',
        N'human', N'human', N'female', N'she/her', 38, N'alive',
        N'House Fornax ambassador currently posted at House Atrament; files regular intelligence reports home; has been at her post for two years',
        N'Brunhilde Sternfeld is not at the estate — she is two weeks'' travel away, in the diplomatic quarter of House Atrament, filing reports that have been getting subtly less specific for four months. She is a competent diplomat who has been in Atrament long enough to understand how the House thinks, which was the point of the posting. She speaks Atrament''s political idiom fluently. She has made friends there, which was not the point of the posting. She has also fallen in love with someone there, which was definitively not the point of the posting, and the love has begun to affect the specificity of her intelligence.',
        N'The diplomat abroad whose loyalty is being eroded by proximity. Her case illustrates the cost of the diplomatic posting system and creates a vulnerability in Fornax intelligence coverage at a House that matters.',
        N'No POV assigned. Would require third-limited from Atrament territory; register is polished and bilingual between Fornax formal and Atrament idiom.',
        N'House Fornax; Rhine-Danube territories; posted to House Atrament for two years',
        168, 61, N'slight; polished; the build of someone who has spent two years in Atrament''s more aesthetically conscious environment',
        N'blonde going darker with age', N'worn in Atrament fashion, which is longer and more elaborate than Fornax style', N'long',
        N'blue-grey', N'fair', N'composed; better-rested than most Fornax administrators',
        N'none',
        N'carries herself in Atrament''s more graceful register; the Fornax uprightness is still there underneath',
        N'Atrament diplomatic dress; she has adapted fully to local formal fashion, which causes mild comment in her home dispatches',
        N'none',
        N'She attends Atrament diplomatic functions. She files weekly reports through the official dispatch system. She spends more time than she should in the company of House Atrament''s Spymaster, who is brilliant and funny and sees exactly what she is and finds it interesting rather than threatening.',
        N'She has fallen in love with House Atrament''s Spymaster and has been managing the conflict between her feelings and her duty by softening her intelligence reports in ways she can justify individually but which in aggregate represent a meaningful degradation of Fornax coverage of Atrament. She has not decided what she is doing or why she is doing it. She has been telling herself each report is the last compromised one for four months. The Fornax Spymaster Marta Scholl has not yet noticed the pattern. When she does, she will.',
        N'Bilingual between Fornax formal and Atrament idiom; the Atrament idiom is winning',
        N'Warmer and more expansive than she was before the posting; she has changed',
        N'Often negotiating internally between what she owes Fornax and what she owes herself',
        N'Returns to crisp Fornax formal; the Atrament warmth drops away; this is the most honest version of her',
        N'Has one; the intimacy has contaminated her professional judgment and she knows it',
        N'House Atrament diplomatic quarter; the Atrament political circuits; Fornax as the home she is increasingly viewing at a distance',
        0, 0,
        N'Germanic woman in her late thirties, blonde hair worn in Atrament fashion, blue-grey eyes, slight polished build, Atrament diplomatic dress, Atrament diplomatic quarter, composed posture with Atrament warmth layered over Fornax uprightness, dark fantasy WW1-adjacent register',
        N'young woman, blonde hair in elaborate style, formal diplomatic dress, diplomatic interior, composed expression, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Brunhilde Sternfeld seeded.';
END
ELSE PRINT 'Brunhilde Sternfeld already exists.';
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- MILITARY COMMAND (17–25)
-- ─────────────────────────────────────────────────────────────────────────────

-- 17. COMMANDER SIEGRID VON ROTH — Paladin; Commander of the Myrmidon Corps
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Siegrid von Roth')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Siegrid von Roth', N'siegrid-von-roth', N'canon', 1,
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
        @id, N'Siegrid von Roth', N'siegrid-von-roth', N'Siegrid', N'von Roth', N'Commander',
        N'human', N'human', N'female', N'she/her', 56, N'alive',
        N'Commander of the House Fornax Myrmidon Corps; Paladin; the highest-ranking military figure in the House',
        N'Siegrid von Roth has commanded the House Fornax Myrmidon Corps for fourteen years and she has been a Paladin for twenty-two, and the two facts have shaped each other in ways she sometimes notices when she looks at the young soldiers under her command. She is formidable in the specific way of someone who has had the Catalyst twice and trained every day since: the augmentation is evident, the eyes have gone to a dark copper that no untransformed person has, and the mass is real rather than performed. She drives her corps harder than any previous commander. She has excellent reasons for this that she has not told anyone.',
        N'The military commander whose guilt over a past failure has become policy. Her driven command style is a form of reparation she can never complete, which makes her dangerous to herself and to her soldiers.',
        N'No POV assigned. Third-limited; her register is military-command: short sentences, active voice, no qualifications unless they are operational requirements.',
        N'House Fornax; Rhine-Danube territories; former field officer elevated to command at forty-two; Paladin-rank',
        182, 96, N'Paladin-enhanced; the second infusion added evident mass and altered her proportional frame; she was tall before and is taller now',
        N'dark grey; was black', N'close-cropped', N'very short',
        N'dark copper; the second infusion''s mark', N'weathered medium-fair', N'marked; twenty years in the field before command',
        N'Evident enhancement — significant height, altered proportions, changed eyes',
        N'completely controlled; the posture of someone who has chosen every physical habit deliberately',
        N'military dress at all times; no compromise; the insignia of command on the left shoulder',
        N'Multiple Catalyst infusions; Paladin-rank; significantly altered skeletal proportions; evident height and mass increase; dark copper eye color is the second infusion''s signature',
        N'She is at the corps grounds before dawn. She runs physical training with the soldiers rather than observing it. She conducts the command briefings at the sixth hour. She inspects equipment personally. She reads after-action reports for every engagement even when she was not present. She has not taken leave in four years.',
        N'In the last major engagement three years ago, she failed to give the order that would have extracted thirty soldiers from a collapsing position because she hesitated for forty seconds — she saw the field correctly but could not make herself act on what she saw. Thirty soldiers died. The official record attributes the loss to enemy action, which is technically accurate and morally incomplete. She was the enemy action. She drives the corps harder now because she is trying to make the corps good enough that her hesitation can never cost them again. She is aware this does not undo what happened.',
        N'Military command register; active voice; no decoration; she says what needs to be done and nothing else',
        N'Fast; she decides before she speaks; every sentence is a completed thought',
        N'Almost always evaluating readiness — of a plan, a soldier, a situation',
        N'Gets colder; becomes extremely precise; no emotion in the diction',
        N'Has one person she is not performing for, who is not in this House and does not know she is nearby',
        N'Rhine-Danube frontier; the corps positions; the Scrying installation perimeter',
        0, 0,
        N'Germanic woman in her mid-fifties, close-cropped dark grey hair, dark copper Paladin eyes, Paladin-enhanced tall and solid frame, military dress with commander insignia, Rhine-Danube stone corps headquarters, controlled posture, the expression of someone who has made a decision she cannot undo, Buehlman dark fantasy register',
        N'military commander, augmented build, dark grey hair, copper eyes, military dress, stone headquarters, controlled expression, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Siegrid von Roth seeded.';
END
ELSE PRINT 'Siegrid von Roth already exists.';
GO

-- 18. KONRAD HASSEL — First Captain; ground operations; Knight
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Konrad Hassel')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Konrad Hassel', N'konrad-hassel', N'canon', 1,
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
        @id, N'Konrad Hassel', N'konrad-hassel', N'Konrad', N'Hassel', N'',
        N'human', N'human', N'male', N'he/him', 41, N'alive',
        N'First Captain of the House Fornax Myrmidon Corps; commands ground operations; Knight',
        N'Konrad Hassel is the soldier who implements what the Commander decides, which means he is the person the soldiers actually see most days and the person who absorbs the consequences of command decisions that don''t survive contact with the field. He became a Knight at thirty-four — the infusion changed his height by four centimeters and his eye color from brown to a pale amber, and he has not thought about it much since then because there was always the next engagement to think about. He is thorough and reliable and privately carrying something that has nothing to do with the war.',
        N'The functional middle of the military hierarchy; his personal grief is entirely separate from his professional competence, which makes him interesting.',
        N'No POV assigned. Third-limited; his register is operational and slightly terse.',
        N'House Fornax; Rhine-Danube territories; career soldier; Knight-rank following Catalyst infusion at thirty-four',
        181, 86, N'Knight-enhanced; taller than before infusion; solid field build',
        N'brown', N'close-cropped', N'very short',
        N'pale amber; the infusion''s mark', N'fair weathered', N'field-marked',
        N'Subtle height gain, increased density',
        N'field-practical; deliberate movement',
        N'field dress or corps formal; never decorative',
        N'First Catalyst infusion at thirty-four; modest height gain; increased bone density; pale amber eye color is the infusion''s signature',
        N'He runs ground operations briefings at the seventh hour. He personally leads significant field exercises. He reviews patrol reports each afternoon. He writes three unsent letters a week and files them in a locked case under his bunk.',
        N'He has been writing letters to his estranged daughter for three years — letters he never sends because he does not know where she is. She was placed in Liturgy service eight years ago after her mother died and he was in the field and could not contest the placement. He has been told she took vows. He does not know that she was not born into this Cauld — she was a taken Sphere 31 child, placed in the Liturgy as a ward, and looks exactly like someone he loved twenty years ago. She is not biologically his daughter. He has never known this. The letters are addressed to a person who does not quite exist.',
        N'Operational; clear; economical; the vocabulary of a man who briefs soldiers',
        N'Measured and complete; he does not trail off',
        N'Almost always evaluating the operational feasibility of what he is hearing',
        N'Gets quieter and more deliberate; the operational clarity becomes a shield',
        N'Exists in the letters he does not send; that is the only place he permits it',
        N'Rhine-Danube frontier; the corps operational zone',
        0, 0,
        N'Germanic man in his early forties, close-cropped brown hair, pale amber Knight eyes, Knight-enhanced solid build, field dress, Rhine-Danube corps grounds or field setting, operational posture, the expression of someone who has put his grief somewhere out of the way, dark fantasy WW1-adjacent register',
        N'soldier in his forties, brown hair, amber eyes, field dress, Knight-enhanced build, corps setting, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Konrad Hassel seeded.';
END
ELSE PRINT 'Konrad Hassel already exists.';
GO

-- 19. IRMGARD WOLFF — Second Captain; garrison and defense; Knight
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Irmgard Wolff')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Irmgard Wolff', N'irmgard-wolff', N'canon', 1,
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
        @id, N'Irmgard Wolff', N'irmgard-wolff', N'Irmgard', N'Wolff', N'Dame',
        N'human', N'human', N'female', N'she/her', 39, N'alive',
        N'Second Captain of the House Fornax Myrmidon Corps; commands garrison and estate defense; Knight',
        N'Irmgard Wolff is responsible for keeping the estate defended while the First Captain takes the Corps to the field, which means she knows more about the estate''s internal vulnerabilities than anyone in the military structure. She became a Knight at thirty-one; the infusion gave her a centimeter and a half of height and changed her eyes from dark grey to a flat silver. She is organized and thorough, slightly behind Hassel in instinct but ahead of him in systematic preparation. She has been diverting a small, specific piece of information for eight months and has told herself she will stop when the situation resolves.',
        N'The defense specialist with a private vulnerability. Her betrayal is small but has large consequences if discovered, and her motivation is sympathetic enough to complicate a simple verdict.',
        N'No POV assigned. Third-limited; her register is systematic and organized.',
        N'House Fornax; Rhine-Danube territories; Knight-rank following Catalyst infusion at thirty-one',
        169, 71, N'Knight-enhanced; modestly taller; the density is in her hands particularly',
        N'dark brown', N'practical; always tied back', N'shoulder-length when loose',
        N'flat silver; the infusion''s mark', N'pale', N'clear; she is mostly indoors',
        N'Subtle height gain, increased density',
        N'deliberate; the movement of someone who has catalogued every room she is responsible for',
        N'corps dress; practical; never formal when she can avoid it',
        N'First Catalyst infusion at thirty-one; modest height gain; increased bone density; flat silver eye color is the infusion''s signature',
        N'She oversees garrison rotations and reviews the estate defense positions daily. She meets with the Head of Household Guards twice a week. She conducts the defensive exercise drills. She files a monthly defense readiness report. She has been sending an additional, unofficial report to someone outside the House once a month for eight months.',
        N'She has been accepting payments from a House Ophiuchus scholar to report on the technical specifications of Fornax siege engine configurations — not troop movements, just the engineering. Not for ideology: her aging father is bound by a debt obligation to a merchant lord who has been extracting labor payments for eleven years, and the money from the Ophiuchus scholar is the only way she can see to buy his freedom. She tells herself the information is technical rather than military and that this distinction matters. She is not certain it does.',
        N'Systematic; organized; the vocabulary of defense planning',
        N'Methodical; she completes her thoughts before delivering them',
        N'Almost always assessing structural vulnerabilities — in plans, positions, and people',
        N'Gets more organized under pressure, not less; the systematic quality becomes relentless',
        N'Warm but private; she does not share the underlying architecture of her decisions',
        N'House Fornax estate and perimeter; the garrison positions',
        0, 0,
        N'Germanic woman in her late thirties, dark brown hair tied back, flat silver Knight eyes, Knight-enhanced build, corps dress, Rhine-Danube stone garrison, organized posture, the expression of someone cataloguing a room, dark fantasy WW1-adjacent register',
        N'woman soldier in her thirties, dark hair, silver eyes, corps dress, garrison setting, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Irmgard Wolff seeded.';
END
ELSE PRINT 'Irmgard Wolff already exists.';
GO

-- 20. BALDUR EICHE — Captain; Scrying installation defense; Knight
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Baldur Eiche')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Baldur Eiche', N'baldur-eiche', N'canon', 1,
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
        @id, N'Baldur Eiche', N'baldur-eiche', N'Baldur', N'Eiche', N'',
        N'human', N'human', N'male', N'he/him', 35, N'alive',
        N'Captain assigned to Scrying installation defense; Knight; responsible for the physical security of the apparatus and its staff',
        N'Baldur Eiche guards the Scrying installation with thorough professional competence and privately does not believe a word of the institutional account of what the apparatus does. He has been guarding the installation for four years. He has watched the operators work. He has read the access protocols and the observation logs and the Liturgy''s official description of membrane contact. He does not think the description is accurate. He thinks the apparatus is theater — elaborate, expensive, politically essential theater that the Houses maintain because they are too invested to question it and the Liturgy is too powerful to contradict. He guards it anyway, because that is his assignment.',
        N'The skeptic in the room where faith is required. His doubt makes him very dangerous if he ever acts on it — and a curious liability while he does not.',
        N'No POV assigned. Third-limited; his register is precise and slightly detached, the register of someone observing rather than participating.',
        N'House Fornax; Rhine-Danube territories; Knight-rank following Catalyst infusion at twenty-nine',
        179, 82, N'Knight-enhanced; modestly taller and denser; carries the change well',
        N'dark blond', N'close-cropped', N'short',
        N'pale green; the infusion''s mark', N'fair', N'clear; he has the complexion of someone comfortable outdoors',
        N'Subtle height gain, increased density',
        N'watchful; installs himself at observation positions even in ordinary rooms',
        N'field dress; practical; well-maintained',
        N'First Catalyst infusion at twenty-nine; modest height gain; increased bone density; pale green eye color is the infusion''s signature',
        N'He rotates the guard posts at the installation every four hours. He reviews the security logs. He interviews new personnel cleared for installation access. He has been extracting specific technical information from the installation''s maintenance records through the groom Lise Raab, who thinks he is conducting security reviews.',
        N'He has never believed that Scrying is legitimate — he thinks the entire apparatus is a political religion, maintained because the Houses depend on the belief in it rather than on its actual function. He guards it not because he believes in it but because his career is attached to its defense. The information he is extracting through Lise Raab about night horse movements is not for security reviews: he is building a picture of who leaves the installation at night and when, because he suspects the Scrying logs are fabricated and he is trying to understand who is fabricating them and why.',
        N'Precise and slightly detached; prefers observation vocabulary to assertion vocabulary',
        N'Even and unhurried; he has time; he is waiting for evidence',
        N'Almost always gathering rather than revealing',
        N'Goes quieter; the detachment becomes complete',
        N'Does not have one; he finds trust difficult when he believes most institutional claims are performed',
        N'The Scrying installation and its perimeter; the estate grounds',
        0, 0,
        N'Germanic man in his mid-thirties, close-cropped dark blond hair, pale green Knight eyes, Knight-enhanced solid build, field dress, Rhine-Danube Scrying installation exterior, watchful observation posture, dark fantasy WW1-adjacent register',
        N'soldier in his thirties, dark blond hair, green eyes, field dress, installation exterior, watchful posture, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Baldur Eiche seeded.';
END
ELSE PRINT 'Baldur Eiche already exists.';
GO

-- 21. LENA FURCH — Infirmary Commander
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Lena Furch')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Lena Furch', N'lena-furch', N'canon', 1,
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
        @id, N'Lena Furch', N'lena-furch', N'Lena', N'Furch', N'',
        N'human', N'human', N'female', N'she/her', 45, N'alive',
        N'Infirmary Commander; oversees the field hospital attached to the House Fornax Myrmidon Corps',
        N'Lena Furch runs the field hospital with an efficiency that the Corps commander relies on and that costs Furch more than anyone in the command structure has calculated. She is not transmuted — she has always been a physician first and the infusion was never offered in a context that made it appropriate for her role. She has treated Paladin wounds and Catalyst failures and the ordinary grinding destruction of soldiers who have been in the field for years. She knows more about the physical cost of the Living War than anyone outside the hospital. She uses this knowledge carefully.',
        N'The medical witness — the person who sees the cost of every military decision in the bodies that pass through her care. Her specific moral failure creates a direct chain to a war crime the House has not examined.',
        N'No POV assigned. Third-limited; her register is clinical and controlled, with a quality of deliberate distance that is a professional practice rather than coldness.',
        N'House Fornax; Rhine-Danube territories; career military physician; no transmutation',
        167, 64, N'practical; the build of someone who has spent twenty years on her feet',
        N'brown', N'pinned back; always clear of her face', N'medium',
        N'brown', N'warm fair', N'composed; the specific composure of someone who has seen too much to be surprised',
        N'none',
        N'economical; wastes no movement in the infirmary; the same quality carries into ordinary rooms',
        N'medical working dress; practical; always clean regardless of what she has just dealt with',
        N'none',
        N'She begins rounds at the fifth hour. She reviews treatment records. She attends the morning briefing with the Corps command as the medical representative. She writes the monthly casualty and morbidity reports. She takes a walk at noon that is the one unstructured hour of her day, and she uses it to think about a decision she made seven years ago that she cannot undo.',
        N'Seven years ago she saved the life of a soldier who had taken a severe wound that, in her clinical judgment, would have been fatal without intervention. She intervened. Eight months later she learned that soldier had committed a summary execution of three prisoners in the eastern engagement — an action that was covered up rather than reported. She had not known what was coming. She could not have known. But she had chosen to save him when she could have let the judgment go the other way, and the three prisoners were dead, and she was why. She has never reported either the execution or her knowledge of it.',
        N'Clinical; precise; no embellishment; the vocabulary of someone who describes the body''s condition rather than its meaning',
        N'Even and unhurried; she has been trained to give bad news without urgency bleeding into it',
        N'Almost always evaluating what the other person can tolerate hearing',
        N'Gets quieter and more precise; the clinical register becomes a wall',
        N'Exists only in the part of her that she has sectioned off and labeled past; she does not visit it',
        N'The field hospital; the Corps grounds; the estate infirmary',
        0, 0,
        N'Germanic woman in her mid-forties, brown hair pinned back, brown eyes, practical build, medical working dress always clean, Rhine-Danube stone infirmary or field hospital, economical posture, the expression of someone who has calibrated exactly what she will and will not feel, dark fantasy WW1-adjacent register',
        N'woman in her forties, brown hair back, practical medical dress, infirmary setting, composed expression, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Lena Furch seeded.';
END
ELSE PRINT 'Lena Furch already exists.';
GO

-- 22. GREGOR HAIN — Senior Sergeant; 25+ years; institutional memory
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Gregor Hain')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Gregor Hain', N'gregor-hain', N'canon', 1,
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
        @id, N'Gregor Hain', N'gregor-hain', N'Gregor', N'Hain', N'',
        N'human', N'human', N'male', N'he/him', 52, N'alive',
        N'Senior Sergeant; twenty-seven years in the Corps; the institutional memory of the Fornax Myrmidon force',
        N'Gregor Hain has been in the Corps for twenty-seven years and has outlasted six captains and two commanders. He knows where the field equipment is actually stored as opposed to where the inventory says it is. He knows which patrol routes have been marked safe on the map and are not safe on the ground. He knows which soldiers will hold and which will fold under the specific pressure of waiting rather than the specific pressure of action, and this is a different and more useful knowledge than anything that appears in the training records. He is not transmuted. He is respected anyway, which is unusual enough to note.',
        N'The institutional memory of the Corps — the person who knows how things actually work beneath the official account. His specific shame is the engine of his most defining professional characteristic.',
        N'No POV assigned. Third-limited; his register is field-sergeant: direct, specific, no deference upward or downward.',
        N'House Fornax; Rhine-Danube territories; career non-commissioned officer; twenty-seven years of service; untransformed',
        177, 88, N'heavy; the natural build of a large man maintained by field work for twenty-seven years; no augmentation but the density is real',
        N'grey; was dark brown', N'close-cropped', N'very short',
        N'brown', N'weathered medium', N'deeply marked; the field in every line',
        N'none',
        N'solid and grounded; does not hurry; has never hurried in twenty-seven years and this has not cost him yet',
        N'field dress; no decoration; he has never cared about the presentation of rank',
        N'none',
        N'He runs the logistics briefing at the sixth hour. He inspects equipment with the junior soldiers after the morning drill. He eats with his section. He drinks beer in the evenings with anyone who wants company. He has been offered Transmutation three times. He has turned it down three times. He does not discuss this.',
        N'He has turned down Transmutation three times — each time citing faith, tradition, the value of the untransformed example in a Paladin-heavy command. The truth is he is terrified of the eighty percent mortality rate and has been since he watched his best friend not come back from the Catalyst at twenty-four. He has watched seventeen infusion candidates since then. Eleven of them died. The shame of his cowardice — as he names it privately — is why he has stayed in the Corps for twenty-seven years instead of taking the officer''s path that was offered to him twice. Officers get offered the Catalyst. Sergeants can decline.',
        N'Field-sergeant; direct; specific; no ceremony; calls things by their working names',
        N'Unhurried and complete; he has been giving orders for twenty-seven years and has the cadence of someone who expects to be heard the first time',
        N'Almost always deciding whether the person speaking to him is someone he can trust to hold',
        N'Does not change; he has been under pressure for twenty-seven years and has a standard operating mode for it',
        N'Warm and unguarded with soldiers who have earned it; this is his only intimacy register and it is genuine',
        N'The Corps grounds; the Rhine-Danube frontier; the field',
        0, 0,
        N'Germanic man in his early fifties, grey close-cropped hair, brown eyes, large heavy build through field work not augmentation, field dress, Rhine-Danube corps grounds, solid unhurried posture, the expression of someone who has seen everything and filed most of it as normal, dark fantasy WW1-adjacent register',
        N'older soldier, grey hair, heavy build, field dress, corps grounds, solid posture, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Gregor Hain seeded.';
END
ELSE PRINT 'Gregor Hain already exists.';
GO

-- 23. ERIKA MÖLLER — Veteran Soldier near retirement
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Erika Möller')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Erika Möller', N'erika-moller', N'canon', 1,
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
        @id, N'Erika Möller', N'erika-moller', N'Erika', N'Möller', N'',
        N'human', N'human', N'female', N'she/her', 48, N'alive',
        N'Veteran soldier in the House Fornax Myrmidon Corps; twenty-two years of service; planning to retire within the year',
        N'Erika Möller has served in the Corps for twenty-two years and has been planning to retire to her family''s mill for three of them. Each year something prevents the departure — a campaign, a staffing need, one more season. She is not transmuted. She is experienced enough that the Corps does not want to lose her and subtle enough not to become indispensable. She does her work without decoration and is liked by the soldiers around her in the way of someone who has never needed anything from them except that they do their jobs. She has a sister she has not seen in twelve years and thinks about most days.',
        N'The character with one foot out the door — whose worldview has shifted to one the institution cannot safely know about. Her retirement becomes either a relief or a threat depending on what she does with what she believes.',
        N'No POV assigned. Third-limited; her register is veteran-practical, without the energy of someone performing their role.',
        N'House Fornax; Rhine-Danube territories; veteran non-commissioned soldier; twenty-two years of service; untransformed',
        165, 67, N'solid field build; the body of someone who has done physical work for twenty years; beginning to feel the accumulated cost of it',
        N'brown going grey', N'braided; practical', N'medium',
        N'dark green', N'weathered medium-fair', N'marked; honest field weathering',
        N'none',
        N'deliberate and economical; she has stopped performing readiness and now just is or is not ready',
        N'field dress; worn; well-maintained but not new; she has not ordered new kit in four years because she keeps planning to leave',
        N'none',
        N'She completes her patrols and her equipment maintenance with the same unhurried thoroughness she has brought to them for twenty-two years. She eats with the section. She writes to her sister every week and the letters make the six-week round trip reliably. She has been reviewing retirement logistics — the housing, the transition, the pension structure — for three years without completing the paperwork.',
        N'She no longer believes the Living War is inevitable or ungovernable. She has spent twenty-two years watching the war''s logistical machinery and has concluded that the conflict is perpetuated deliberately by the Houses and the Liturgy combined — that there are specific decisions made in specific rooms that could end it, and that those decisions are not made because the war is more useful than peace to the people who run both. She has told no one this because saying it ends her life. She has been sitting with it for six years and it is the reason she keeps delaying the retirement: she has not figured out what, if anything, she is going to do with what she knows.',
        N'Veteran-practical; no waste; the vocabulary of someone who has named things accurately for twenty years',
        N'Unhurried; she has finished all the sentences she needed to finish; the remaining ones get the time they need',
        N'Almost always deciding whether the other person is someone she could trust with the thing she cannot say',
        N'Goes completely flat; gives the minimum required answer and nothing more',
        N'Warm with her section; a different warmth, older and less conditional, with her sister in the letters',
        N'The Corps grounds; the Rhine-Danube frontier; the mill in the east that she has not visited in twelve years',
        0, 0,
        N'Germanic woman in her late forties, brown hair going grey in a practical braid, dark green eyes, solid field build, worn field dress, Rhine-Danube corps grounds, deliberate unhurried posture, the expression of someone carrying a conclusion they have not acted on, dark fantasy WW1-adjacent register',
        N'veteran woman soldier, brown-grey braided hair, dark green eyes, worn field dress, corps setting, deliberate posture, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Erika Möller seeded.';
END
ELSE PRINT 'Erika Möller already exists.';
GO

-- 24. CASPAR REUTH — Junior Officer; recently distinguished; being watched
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Caspar Reuth')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Caspar Reuth', N'caspar-reuth', N'canon', 1,
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
        @id, N'Caspar Reuth', N'caspar-reuth', N'Caspar', N'Reuth', N'',
        N'human', N'human', N'male', N'he/him', 24, N'alive',
        N'Junior officer in the House Fornax Myrmidon Corps; recently distinguished in a raid on a House Draught scout post; being assessed for Catalyst candidacy',
        N'Caspar Reuth was commended three months ago for taking a House Draught scout post with eight soldiers and no losses. The action was clean, fast, and tactically sound, and the Corps command has noted him as a candidate for first infusion consideration. He accepts the attention with a composure that looks like confidence and is actually something closer to paralysis, because the commendation is built on an incomplete account of what happened at the scout post, and the incomplete account is a thing Caspar put there deliberately and does not yet understand why.',
        N'The new talent whose defining action concealed a moral choice he cannot yet name. His trajectory toward Transmutation candidacy is a clock counting down toward a moment of reckoning.',
        N'No POV assigned. Third-limited; register is young-officer: energetic, precise, increasingly strained underneath.',
        N'House Fornax; Rhine-Danube territories; junior officer; three years of service; untransformed; Catalyst candidacy under review',
        180, 75, N'lean field build; young; has not finished growing into his frame',
        N'dark brown', N'close-cropped', N'short',
        N'grey', N'fair', N'clear; the complexion of someone who has not been in the field long enough to weather',
        N'none',
        N'straight and composed; performing the command posture he has been trained for',
        N'field dress; well-maintained; he takes his presentation seriously in the way of someone who knows he is being watched',
        N'none',
        N'He runs his section''s morning drill. He attends the command briefings as the most junior officer present. He reviews tactics manuals in the evenings. He has been training harder since the commendation, because he is trying to become good enough that the commendation becomes retroactively deserved.',
        N'At the scout post, after the Draught soldiers had been disarmed, he looked at three of the youngest scouts — boys, effectively, no older than he was two years ago — and instead of taking them prisoner he told them the patrol routes away from Fornax territory and let them go. He does not know why he did this. The official action report accounts for the scout post''s complement without counting those three, and no one has audited the discrepancy. He is terrified someone will discover this. He is equally terrified of the day he understands his own reason, because understanding it will require deciding whether to do it again.',
        N'Officer-formal in briefings; more ordinary in field settings; he is still constructing his register',
        N'Fast and precise; he has been trained to deliver information efficiently',
        N'Almost always managing the gap between what he has done and what he is receiving credit for',
        N'Goes more precise and more formal; the training takes over; he sounds older than he is',
        N'Does not yet have one; he is twenty-four and has been performing competence for three years',
        N'The Corps grounds; the Rhine-Danube border zone',
        0, 0,
        N'young Germanic man of twenty-four, dark brown close-cropped hair, grey eyes, lean field build, well-maintained field dress, Rhine-Danube corps grounds, composed officer posture that is slightly too deliberate, dark fantasy WW1-adjacent register',
        N'young soldier in his mid-twenties, dark hair, grey eyes, field dress, corps setting, composed expression, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Caspar Reuth seeded.';
END
ELSE PRINT 'Caspar Reuth already exists.';
GO

-- 25. HILDEGARD GEIS — Transmutation Practitioner
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Hildegard Geis')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Hildegard Geis', N'hildegard-geis', N'canon', 1,
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
        @id, N'Hildegard Geis', N'hildegard-geis', N'Hildegard', N'Geis', N'',
        N'human', N'human', N'female', N'she/her', 44, N'alive',
        N'Transmutation Practitioner attached to the House Fornax Corps; prepares and administers Catalyst infusions before campaigns',
        N'Hildegard Geis has administered the Catalyst to one hundred and fourteen candidates in nineteen years. Eighty-one of them survived the infusion. She has never gotten the ratio wrong on paper: the records show eighty percent survival, which is accurate within the margin the Liturgy''s protocols define as acceptable. The margin is the thing she thinks about. She is a careful technician and a precise administrator and she understands what the Catalyst actually does to the body in a level of detail that no commander she has ever served under has wanted to hear. She does not offer this information. She does her work. She keeps careful records. Some of the records she keeps in a ledger no one else reads.',
        N'The person who administers death or transformation on behalf of the institution — and who has used that position to carry out three personal acts of mercy that are indistinguishable from murder on the official record.',
        N'No POV assigned. Third-limited; her register is precise and clinical, with an evenness that is the result of sustained deliberate control.',
        N'House Fornax; Rhine-Danube territories; Liturgy-trained Transmutation Practitioner attached to the Corps; nineteen years of service',
        163, 59, N'slight; the build of someone who works with precision instruments rather than physical force',
        N'brown', N'pinned back; always clear of her face during infusion procedures', N'medium',
        N'dark brown', N'warm fair', N'composed; she has the look of someone who has made a decision and finished making it',
        N'none',
        N'precise and still; she does not gesture; her hands are quiet',
        N'working dress that is clean and practical; always the same; she does not vary her presentation',
        N'none',
        N'She prepares the Catalyst according to standard protocol, which she can do from memory in any conditions. She reviews candidates'' physical assessments before any infusion. She documents every infusion outcome in the official record. She maintains a second, private ledger. She attends the Chaplain''s morning rite on the days before infusions.',
        N'Three candidates over the past seven years came to her privately, before their scheduled infusions, and asked her to adjust the preparation in a specific way. All three were facing something they could not name in words that would be understood by a military tribunal — one a threat, one a discovery, one a debt — and all three had concluded that dying in the Catalyst was preferable to what was coming. She adjusted the preparation. All three died. The official record shows three standard infusion failures, which statistically fall within the expected range. She carried out their final wish. She has not told anyone. She attends the Chaplain''s rite before infusions because she has begun to need the ritual in a way she cannot explain to herself.',
        N'Precise and technical; the vocabulary of biochemical process; does not use metaphor for the Catalyst''s action',
        N'Even and measured; she has learned to give information without inflection',
        N'Almost always assessing whether the person speaking to her is someone who wants the true answer or the comfortable one',
        N'Does not change; she reached her current state through sustained pressure and has stabilized there',
        N'Closed; the three candidates are the closest she has come to intimacy in years and they are dead',
        N'The infusion chamber; the Corps medical space; the Scrying installation chapel',
        0, 0,
        N'Germanic woman in her mid-forties, brown hair pinned back, dark brown eyes, slight precise build, clean working dress, Rhine-Danube infusion chamber, very still hands, the expression of someone who has finished deciding something, dark fantasy WW1-adjacent register',
        N'slight woman in her forties, brown hair, dark eyes, working dress, infusion chamber, still expression, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Hildegard Geis seeded.';
END
ELSE PRINT 'Hildegard Geis already exists.';
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SCRYING INSTALLATION STAFF (26–31)
-- ─────────────────────────────────────────────────────────────────────────────

-- 26. THEODOR NACHT — Head Scrying Operator
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Theodor Nacht')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Theodor Nacht', N'theodor-nacht', N'canon', 1,
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
        @id, N'Theodor Nacht', N'theodor-nacht', N'Theodor', N'Nacht', N'',
        N'human', N'human', N'male', N'he/him', 61, N'alive',
        N'Head Scrying Operator; has been at the apparatus for thirty-five years; the most experienced observer in House Fornax',
        N'Theodor Nacht has been looking through the Scrying apparatus for thirty-five years and he has seen things that changed his understanding of what is on the other side of the membrane, what the membrane is, and what it means that Houses spend soldiers'' lives to control the right to look through it. He does not share these conclusions. He trains his operators carefully and documents his observations precisely and keeps the apparatus running in ways that satisfy the Liturgy''s quarterly audits. He has been tutoring the Lord''s second child Gerda Brenner in apparatus theory after midnight for eight months, which is the first thing in years that has given him something that feels like hope.',
        N'The deep-knowledge holder of the Scrying operation — the only person who knows there is a second apparatus beneath the first. His secret gives him a power over the Liturgy he has never used and may never use.',
        N'No POV assigned. Third-limited; his register is observational and precise, the register of a man who has spent thirty-five years describing what he sees without interpreting it for audiences who could not handle the interpretation.',
        N'House Fornax; Rhine-Danube territories; career Scrying operator; thirty-five years at the primary apparatus',
        171, 68, N'slight; the build of someone who has spent thirty-five years seated at an apparatus; his eyes are extraordinary',
        N'white; was dark', N'unkempt; he does not attend to it', N'overlong',
        N'pale grey; there is something about the quality of his gaze that is difficult to name — not supernatural, just the product of thirty-five years of trained observation', N'pale', N'fine; almost papery',
        N'none',
        N'still; he has learned to conserve movement because the apparatus rewards stillness; the quality has carried into ordinary life',
        N'installation working dress; plain; he has worn the same style for thirty years',
        N'none',
        N'He arrives at the apparatus before any operator on duty. He runs the calibration checks. He reviews the previous watch''s observation logs. He conducts the operator briefings. He spends his late evenings at the apparatus — officially reviewing long-watch documentation, actually operating the concealed secondary apparatus that no one except possibly Ernst Binder knows exists.',
        N'Beneath the primary Scrying apparatus there is a second installation — older, smaller, built by the previous Lord''s grandfather without Liturgy authorization or knowledge. Theodor found it at twenty-six, three years into his posting, when a maintenance access panel opened into a space that had no reason to exist. He has been the only person operating it for twenty years. What it shows him is different from what the primary apparatus shows — a different angle on the same membrane, possibly, or a different membrane entirely. He does not know. He has been documenting his observations in a private ledger since he was thirty. He has not told the Liturgy because telling them would mean losing the access. He has not told the House because he does not yet know what he has.',
        N'Observational; precise; the vocabulary of someone who has spent thirty-five years finding words for things that do not have standard names',
        N'Slow and exact; he does not speak until he has seen what he wants to say clearly',
        N'Almost always deciding how much of what he knows to offer and in what form',
        N'Gets slower, not faster; more careful; the observations become more precise under pressure, not less',
        N'Exists with Gerda Brenner; it is the intimacy of teacher and student and something else that he has not examined because examining it would require him to explain himself to himself',
        N'The Scrying installation; the primary apparatus room; the concealed secondary installation',
        0, 0,
        N'older Germanic man in his early sixties, white unkempt hair, pale grey observational eyes, slight build, plain installation working dress, Rhine-Danube Scrying apparatus room, very still posture, the expression of someone who has spent thirty-five years looking at something no one else has seen, dark fantasy WW1-adjacent register',
        N'slight older man, white hair, pale eyes, plain working dress, Scrying apparatus room, very still, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Theodor Nacht seeded.';
END
ELSE PRINT 'Theodor Nacht already exists.';
GO

-- 27. ANNALIESE BRANDT — Senior Scrying Operator
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Annaliese Brandt')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Annaliese Brandt', N'annaliese-brandt', N'canon', 1,
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
        @id, N'Annaliese Brandt', N'annaliese-brandt', N'Annaliese', N'Brandt', N'',
        N'human', N'human', N'female', N'she/her', 44, N'alive',
        N'Senior Scrying Operator; fifteen years at the apparatus; long-watch specialist; the methodical counterweight to the Head Operator''s intensity',
        N'Annaliese Brandt has been on the long watch for fifteen years and she is very good at it in the way of someone who has made systematic observation into a total practice. She documents precisely, files on schedule, and runs the calibration checks without prompting. The Head Operator trusts her more than any other operator. She is warm in the way of someone who genuinely likes the people she works with and genuinely finds the work interesting, and neither of these qualities has diminished over fifteen years, which is unusual enough at the apparatus that the Head Operator noted it in her annual review three times running before he stopped, because noting it implied he expected it to change.',
        N'The operator who has been compromised by what Scrying actually involves — watching real people live their lives until they become, to the watcher, people rather than subjects. Her case study is the cleanest version of the ethical problem the apparatus creates.',
        N'No POV assigned. Third-limited; her register is warm and methodical, with a quality of care that is not performed.',
        N'House Fornax; Rhine-Danube territories; career Scrying operator; fifteen years of service',
        164, 60, N'medium; she is still; the apparatus rewards stillness and she has given it',
        N'light brown', N'worn simply', N'medium',
        N'green', N'fair', N'composed; gentle',
        N'none',
        N'still and precise at the apparatus; warmer in the briefing room',
        N'installation working dress; neat; she has a small embroidered patch on her left cuff that is not regulation and that no one has told her to remove',
        N'none',
        N'She runs her watch from the third to the ninth hour, six days in seven. She documents her observations in the standard format. She runs a secondary analysis of observation patterns that the Head Operator asked for eleven years ago and that she has continued past its original purpose because she finds it interesting. She has been watching a specific household in Sphere 31 for eleven years.',
        N'She has been observing the same family in Sphere 31 for eleven years. She has given them names — not the names the apparatus assigns to observed subjects but real names, ones she chose based on what she has observed of their personalities. She has watched children born and grow. She has watched grief and recovery and ordinary days and two funerals. She knows this violates the professional protocols around observation distance. She cannot stop. It is not love — she is clear about this. It is something without a good name: the condition of having watched someone long enough that their reality has become more real to you than the institution you serve. She has never told Theodor Nacht.',
        N'Warm and methodical; documentation vocabulary in professional settings; more ordinary in conversation',
        N'Even and complete; she does not rush',
        N'Almost always attending to what the other person actually means rather than what they have said',
        N'Gets quieter and more careful; the warmth does not disappear but it becomes more deliberate',
        N'Genuine and unhurried; she is the same person in all registers, which is rare',
        N'The Scrying installation; the apparatus room; the installation common spaces',
        0, 0,
        N'Germanic woman in her mid-forties, light brown hair worn simply, green eyes, medium still build, installation working dress with small embroidered patch on left cuff, Rhine-Danube Scrying apparatus room, warm methodical posture, dark fantasy WW1-adjacent register',
        N'woman in her forties, light brown hair, green eyes, working dress, Scrying apparatus room, composed warm expression, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Annaliese Brandt seeded.';
END
ELSE PRINT 'Annaliese Brandt already exists.';
GO

-- 28. WILHELM FECHTER — Senior Scrying Operator; long-watch formalist
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Wilhelm Fechter')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Wilhelm Fechter', N'wilhelm-fechter', N'canon', 1,
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
        @id, N'Wilhelm Fechter', N'wilhelm-fechter', N'Wilhelm', N'Fechter', N'',
        N'human', N'human', N'male', N'he/him', 49, N'alive',
        N'Senior Scrying Operator; eighteen years at the apparatus; protocol formalist; the operator most trusted by the Liturgy auditors',
        N'Wilhelm Fechter has been at the apparatus for eighteen years and believes in the observation protocols with a sincerity that some of his colleagues find excessive and that the Liturgy''s quarterly auditors find gratifying. He is correct, thorough, and formally precise. His documentation is the cleanest in the installation — readable, properly cross-referenced, filed on time without exception. The Head Operator values him for his reliability. The Liturgy values him because his records always match what the Liturgy''s own logs show. This alignment is not coincidental.',
        N'The loyal operator whose loyalty has been redirected from accuracy toward the House he serves — a distinction that only becomes visible in a crisis, and that is genuinely dangerous in the meantime.',
        N'No POV assigned. Third-limited; his register is formal and documentation-oriented, the register of a man who has internalized the protocol so completely it has become his voice.',
        N'House Fornax; Rhine-Danube territories; career Scrying operator; eighteen years of service',
        176, 80, N'solid and unremarkable; the build of someone who does not think about his body',
        N'grey-brown', N'neat; correctly maintained', N'short',
        N'brown', N'fair', N'unmarked; the complexion of indoor work',
        N'none',
        N'correct and formal; protocol-straight posture; does not vary',
        N'installation working dress; always correctly pressed; he is the only operator whose uniform is always fully correct',
        N'none',
        N'He runs the watch from the ninth to the fifteenth hour. He documents in the standard format and then reviews the previous shift''s documentation for compliance gaps. He submits compliance variance reports monthly. He has never filed a variance against his own records.',
        N'He has been deliberately misfiling observations that implicate House Fornax in actions the Liturgy would classify as protocol violations — adjusting observation records to remove evidence of infusions conducted without Liturgy oversight and of Scrying sessions that targeted prohibited subjects. He does not do this for payment or political motive. He does it because he is loyal to House Fornax, believes the House is right to operate as it does, and has concluded that the record should reflect what serves the House rather than what happened. He is not troubled by this. He is certain it is correct. This certainty is what makes him genuinely dangerous.',
        N'Formal and protocol-precise; quotation from official documentation is his primary rhetorical mode',
        N'Measured; every sentence is complete; no trailing thought',
        N'Almost always determining whether the other person''s understanding of events matches the documented version',
        N'Goes more formal; produces citations; the protocol vocabulary becomes a wall',
        N'Does not have one; he is not accessible except through the official register',
        N'The Scrying installation; the apparatus room; Liturgy audit sessions',
        0, 0,
        N'Germanic man in his late forties, grey-brown neat hair, brown eyes, solid unremarkable build, perfectly correct installation working dress, Rhine-Danube Scrying apparatus room, protocol-straight posture, the expression of someone who has decided what the record should say, dark fantasy WW1-adjacent register',
        N'man in his forties, neat grey hair, correct uniform, Scrying apparatus room, formal posture, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Wilhelm Fechter seeded.';
END
ELSE PRINT 'Wilhelm Fechter already exists.';
GO

-- 29. DOROTHEA KRUG — Scrying Operator; sardonic; long-watch
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Dorothea Krug')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Dorothea Krug', N'dorothea-krug', N'canon', 1,
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
        @id, N'Dorothea Krug', N'dorothea-krug', N'Dorothea', N'Krug', N'',
        N'human', N'human', N'female', N'she/her', 38, N'alive',
        N'Scrying Operator; ten years at the apparatus; long-watch; the installation''s institutional skeptic',
        N'Dorothea Krug has been looking through the apparatus for ten years and her relationship to the work has settled into something that is professionally competent and personally exhausted. She documents accurately. She files on time. She is not Fechter''s precision and she is not Brandt''s warmth and she is not the Head Operator''s intensity; she is the person who has been doing the long watch for ten years and finds it less transcendent than everyone around her seems to, which she suspects is a comment on her rather than on the apparatus and which she has stopped trying to resolve. She is funny in the specific way of someone who has concluded that the only dignified response to certain situations is to notice how absurd they are.',
        N'The skeptic in a room full of true believers — not because she is wrong, but because she is the only one whose disenchantment is honest rather than concealed. Her secret is the most philosophically interesting thing anyone in the installation is doing.',
        N'No POV assigned. Third-limited; her register is sardonic and precise, with a quality of flatness that reads as exhaustion until you hear her make a joke.',
        N'House Fornax; Rhine-Danube territories; career Scrying operator; ten years of service',
        166, 62, N'medium; the body of someone who sits for long watches and occasionally forgets to eat',
        N'dark brown', N'loose; she does not pin it back with any consistency', N'below shoulder',
        N'brown', N'warm medium', N'tired; she looks like someone who has been working the long watch for ten years, which she has',
        N'none',
        N'slightly slumped at the apparatus; correct posture when she is being observed; she knows when she is being observed',
        N'installation working dress; worn; she does not press it',
        N'none',
        N'She runs the late watch, the fifteenth to the twenty-first hour. She documents. She files. She eats her dinner at the apparatus, which is technically against the protocols and that Theodor Nacht has decided not to notice. She spends the last hour of her watch in a behavior she has been conducting for six years that she has not told anyone about.',
        N'For six years she has been sending encoded signals through the Scrying apparatus — not as part of any sanctioned observation, but as an attempt to communicate with the observed world. She encodes them in the calibration noise at the edge of the apparatus''s operational frequency. She does not know if it works. She does not know if the membrane passes information in both directions. She has never received a response. She has been doing this for six years anyway because the alternative is accepting that the apparatus is entirely one-directional, and she finds that alternative more depressing than the possibility of failure. She has not examined what it means that she has been trying to talk to strangers in another world for six years rather than to anyone in this one.',
        N'Sardonic and precise; the vocabulary of someone who has been finding the correct words for things that don''t have standard names for ten years and has gotten very good at it',
        N'Flat and dry; the humor arrives in the same cadence as everything else, which makes it land harder',
        N'Almost always noticing what is absurd about the current situation and deciding whether this is the moment to say so',
        N'Goes completely flat; the sardonic quality disappears and what remains is very precise and very calm',
        N'Does not have one in this House; the six-year transmission project is the closest thing to intimacy she has and it is addressed to no one she has met',
        N'The Scrying installation; the late watch apparatus room; the installation common space',
        0, 0,
        N'Germanic woman in her late thirties, dark brown loose hair, brown eyes, medium tired build, worn installation working dress, Rhine-Danube Scrying apparatus room at night, slightly slumped posture that corrects when watched, dry expression, dark fantasy WW1-adjacent register',
        N'woman in her late thirties, dark hair, worn working dress, Scrying apparatus room at night, tired expression, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Dorothea Krug seeded.';
END
ELSE PRINT 'Dorothea Krug already exists.';
GO

-- 30. ERNST BINDER — Technical Maintenance Chief
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ernst Binder')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ernst Binder', N'ernst-binder', N'canon', 1,
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
        @id, N'Ernst Binder', N'ernst-binder', N'Ernst', N'Binder', N'',
        N'human', N'human', N'male', N'he/him', 55, N'alive',
        N'Technical Maintenance Chief of the Scrying installation; keeps the apparatus running; does not perform observations',
        N'Ernst Binder has kept the apparatus running for twenty years and has never looked through it. This is an institutional distinction — the operators observe; the maintenance chief maintains — that he has always respected as clearly as a property line. He knows the apparatus''s mechanical systems in their entirety. He knows its tolerances and its failure modes and where the calibration drifts if you do not correct it every third day. He does not know what the apparatus sees. He has always found this a satisfying division of labor. He found it slightly less satisfying three years ago when a maintenance access panel opened into a space that had no reason to be there.',
        N'The maintenance man who discovered the secret installation and said nothing — to anyone, including the Head Operator who is using it. The fact that both of them know independently, neither knowing the other knows, is the most explosive possible configuration.',
        N'No POV assigned. Third-limited; his register is technical and practical, the vocabulary of a man who thinks in mechanical systems.',
        N'House Fornax; Rhine-Danube territories; career installation maintenance; twenty years of service',
        174, 86, N'solid; the build of someone who lifts and carries and crawls through access spaces regularly',
        N'grey', N'short and practical', N'short',
        N'blue', N'weathered fair', N'marked; the specific marking of someone who works with materials and tools',
        N'none',
        N'practical and efficient; the posture of someone who assesses a room for maintenance needs automatically',
        N'work clothing; always marked by whatever he has been working on that day',
        N'none',
        N'He runs the daily calibration check at the fourth hour. He reviews the apparatus maintenance logs. He repairs faults as they are reported. He conducts his own inspection of the full installation every two weeks, including the access spaces. He has been logging one particular access space under a category he invented himself.',
        N'Three years ago a routine maintenance inspection opened an access panel that led into a secondary installation space he had no record of — a smaller apparatus, clearly older, clearly not part of the documented installation. He examined it for four hours. He determined it was operational. He determined it was being used — the calibration marks were fresh and the mechanical components showed regular contact. He said nothing to anyone, including the Head Operator Theodor Nacht, who he suspects is operating it. He has been cataloguing the secondary apparatus in his private maintenance log under the category designation UNREPORTED/LEGACY, which exists in no official document. He does not know what to do with what he has found. He is waiting to understand what it is before he decides.',
        N'Technical and mechanical; describes problems in terms of systems and tolerances; does not use abstract language',
        N'Practical and complete; says what needs to be said about the problem at hand',
        N'Almost always assessing the mechanical condition of whatever he is looking at',
        N'Gets more technical; focuses on the specific mechanical problem; emotion does not enter the vocabulary',
        N'Warm with people who understand how things work; cold with people who treat maintenance as invisible',
        N'The Scrying installation and all its maintenance spaces; the access tunnels; the secondary installation',
        0, 0,
        N'Germanic man in his mid-fifties, grey short hair, blue eyes, solid work-built frame, work clothing marked by the day''s maintenance, Rhine-Danube Scrying installation maintenance space or apparatus room, practical assessing posture, dark fantasy WW1-adjacent register',
        N'maintenance man in his fifties, grey hair, work clothes, installation maintenance space, practical posture, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Ernst Binder seeded.';
END
ELSE PRINT 'Ernst Binder already exists.';
GO

-- 31. FRIEDA LENZ — New Scrying Operator; recently trained; overwhelmed
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Frieda Lenz')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Frieda Lenz', N'frieda-lenz', N'canon', 1,
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
        @id, N'Frieda Lenz', N'frieda-lenz', N'Frieda', N'Lenz', N'',
        N'human', N'human', N'female', N'she/her', 22, N'alive',
        N'New Scrying Operator; completed training eight months ago; the most recent addition to the House Fornax installation',
        N'Frieda Lenz completed her operator training eight months ago and has been on the apparatus ever since, working supervised watches with Annaliese Brandt and unsupervised watches on the quiet morning hours when the installation has its lowest staffing. She is competent — her documentation is accurate and her calibration checks are reliable — but she is not yet a long-watch operator in the way the senior staff are long-watch operators, which is to say she still finds the apparatus startling rather than routine. Something happened in her first unsupervised watch that has not become routine no matter how many times she has been at the apparatus since.',
        N'The newcomer carrying the installation''s most acute crisis of epistemology — what she has seen cannot be explained within the institutional account of what Scrying is, and she is too new to know how to suppress it.',
        N'No POV assigned. Third-limited; register is careful and slightly pressured, the register of someone performing competence while internally frightened.',
        N'House Fornax; Rhine-Danube territories; newly trained Scrying operator; eight months of service',
        163, 55, N'slight; young; she has not yet stopped looking startled by the installation',
        N'light brown', N'worn simply; the same way every day', N'medium',
        N'hazel', N'fair', N'clear; she is twenty-two and has only recently stopped looking like she is about to ask if she is doing this correctly',
        N'none',
        N'careful and slightly braced; the posture of someone who is not certain the floor will not shift',
        N'installation working dress; correct; she has been wearing the same style since training because she has not yet felt authorized to vary from what she learned',
        N'none',
        N'She runs the early morning watch, the first to the seventh hour, when the installation is quietest. She documents. She runs the calibration checks twice instead of once because the redundancy makes her feel better. She reviews the previous watch''s logs before her own begins. She has been keeping a private record of something that she cannot figure out how to categorize in the official documentation.',
        N'During her first solo watch, a figure in the observed world looked directly at the apparatus — not at the observed scene, but at her, through the apparatus, with what she can only describe as recognition. She has told no one. She checks every watch to see if it happens again. It has happened twice more. The figure is the same. Each time, the recognition is clearer. She does not know what this means about the apparatus, about the membrane, or about herself. She knows that the official account of Scrying does not include the observed world looking back, and she does not know if this means the official account is incomplete, or if she is seeing something that is not there, or if something in Sphere 31 has found the apparatus and found her.',
        N'Careful; the vocabulary of someone who is checking each word before deploying it; the training vocabulary still more prominent than her natural register',
        N'Slightly too careful; she pauses before sentences that the senior operators would not pause before',
        N'Almost always managing her own uncertainty about whether what she has seen is real',
        N'Gets very careful; becomes focused on the technical elements of the situation; this is a learned response not a natural one',
        N'Does not have one yet; she is new and frightened and the frightened part takes most of the available space',
        N'The Scrying installation; the early morning apparatus room',
        0, 0,
        N'young Germanic woman of twenty-two, light brown hair worn simply, hazel eyes, slight build, correct installation working dress, Rhine-Danube Scrying apparatus room in early morning light, careful slightly braced posture, expression of someone managing something that has no category, dark fantasy WW1-adjacent register',
        N'young woman, light brown hair, hazel eyes, working dress, Scrying apparatus room, careful expression, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Frieda Lenz seeded.';
END
ELSE PRINT 'Frieda Lenz already exists.';
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- DOMESTIC STAFF (32–52)
-- ─────────────────────────────────────────────────────────────────────────────

-- 32. HEINRICH GROSSE — Seneschal / Head Steward
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Heinrich Grosse')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Heinrich Grosse', N'heinrich-grosse', N'canon', 1,
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
        @id, N'Heinrich Grosse', N'heinrich-grosse', N'Heinrich', N'Grosse', N'',
        N'human', N'human', N'male', N'he/him', 64, N'alive',
        N'Seneschal of House Fornax; manages the entire household staff, accounts, and provisioning; has served the House for thirty-eight years under three Lords',
        N'Heinrich Grosse has managed House Fornax''s household for thirty-eight years and knows where everything is — the stores, the staff, the silver, the petty resentments, the grief — with the same certainty and the same professional quietness. He served under Lord Hartmut''s father, managed the transition when Hartmut came to power, and has been running the household ever since with a competence so thorough that it is invisible. The household works because Grosse makes it work, and the household does not think about this because Grosse has arranged it that way. He has arranged a great many things over thirty-eight years that no one has thought to examine.',
        N'The most knowledgeable person in the household, carrying a secret about his own identity that would restructure the family''s understanding of itself. His thirty-eight years of service are simultaneously a professional achievement and a sustained act of proximity to the only thing he wanted and could not claim.',
        N'No POV assigned. Third-limited; his register is seneschal: warm authority, precise, the register of someone who manages everything without appearing to manage anything.',
        N'House Fornax; Rhine-Danube territories; career household administrator; thirty-eight years of service; born locally',
        176, 80, N'solid and upright; the build of a man who has been dignified for thirty-eight years',
        N'white; was sandy brown', N'always correctly arranged', N'short',
        N'grey', N'fair', N'fine and well-maintained; he attends to his appearance as carefully as he attends to the household',
        N'none',
        N'correct and unhurried; the specific dignity of someone who has never had to announce his authority because it has always been recognized',
        N'formal household dress; always complete; always correct; has not varied in thirty years',
        N'none',
        N'He rises at the fourth hour. He reviews the household accounts. He meets with the Head Cook at the fifth hour to approve the day''s provisions. He reviews staffing at the sixth hour. He receives the morning reports from every domestic department head before the household wakes. He manages the Lord''s correspondence schedule. He is the last person in the household to go to bed.',
        N'Heinrich Grosse is Lord Hartmut''s illegitimate half-brother. They share a father — the previous Lord, who had a relationship with Grosse''s mother, a kitchen maid, before his marriage. Grosse''s mother was sent away when the relationship was discovered. Grosse was raised by a cousin in the town. He found out at thirty, through a document he was not supposed to see. He was thirty-two when he engineered his position in the household by presenting such exceptional references that he was hired without the standard family background review. He has been present for thirty-two years. He has never claimed anything. He has never asked for anything. He does not know if Hartmut knows.',
        N'Seneschal-formal; warm but precise; the vocabulary of household management used for every subject',
        N'Measured and complete; he finishes every sentence',
        N'Almost always managing the other person''s comfort and their impression of the household',
        N'Does not change; he has been managing crises for thirty-eight years and his operating mode is already the crisis mode',
        N'Warm with the staff; professionally warm with the family; genuinely warm with no one currently living',
        N'The House Fornax estate; every room in it; he knows the building better than the building knows itself',
        0, 0,
        N'Germanic man in his mid-sixties, white hair correctly arranged, grey eyes, solid upright build, formal household dress always complete, Rhine-Danube stone estate interior, unhurried dignified posture, the expression of someone who manages everything without appearing to, dark fantasy WW1-adjacent register',
        N'older man, white hair, formal household dress, stone estate interior, dignified posture, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Heinrich Grosse seeded.';
END
ELSE PRINT 'Heinrich Grosse already exists.';
GO

-- 33. MARGARETHA ZINK — Head Cook
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Margaretha Zink')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Margaretha Zink', N'margaretha-zink', N'canon', 1,
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
        @id, N'Margaretha Zink', N'margaretha-zink', N'Margaretha', N'Zink', N'',
        N'human', N'human', N'female', N'she/her', 58, N'alive',
        N'Head Cook of House Fornax; has run the kitchen for thirty years; knew and preferred the previous Lord',
        N'Margaretha Zink has run the House Fornax kitchen for thirty years and knows the preferences of every person in the household — what they eat when they are stressed, what they refuse when they are grieving, what they order when they are celebrating something they cannot announce publicly. She fed Lord Hartmut''s father for seven years and has been feeding Hartmut for thirty, and her opinion of the comparative quality of the two Lords is not one she expresses in language. She expresses it by cooking the previous Lord''s favorite dish on the anniversary of his death, which is a statement everyone in the kitchen understands and that she has never been asked to explain.',
        N'The institutional memory of the household''s emotional life. She knows what the House feels by what it eats. Her secret is the most extreme act of institutional loyalty in the building.',
        N'No POV assigned. Third-limited; her register is warm, direct, and kitchen-practical, the register of a woman who has run the same space for thirty years and does not need anyone''s permission for anything in it.',
        N'House Fornax; Rhine-Danube territories; career household cook; thirty years of service',
        162, 76, N'solid; the build of someone who has been on her feet in a working kitchen for thirty years',
        N'grey-brown', N'pinned back; always clear of her face at work', N'medium',
        N'brown', N'warm medium', N'flushed and warm; the kitchen has been in her complexion for thirty years',
        N'none',
        N'authoritative and grounded; the kitchen is her domain and her posture knows it',
        N'kitchen working dress; always clean at the beginning of the day; the condition deteriorates as the day progresses',
        N'none',
        N'She arrives at the kitchen at the fourth hour. She meets with the Seneschal at the fifth to confirm provisions. She manages the breakfast, the noon meal, and the evening meal for the full household. She supervises the sous-chef and the kitchen assistant. She sometimes bribes useful information from visiting diplomats by reading what they eat and adjusting subsequent courses to create comfort and then dependency.',
        N'Twenty-two years ago she poisoned a House ambassador. The Dowager Walburga came to the kitchen and spoke to her for eleven minutes about the ambassador''s dietary habits. No explicit instruction was given. The instructions were clear enough. The ambassador died of what the House physician recorded as a cardiac event. Margaretha has never been asked about it, never been charged, never been rewarded. She has been waiting for the Dowager to explain what the ambassador had done to warrant it. Walburga has never said. Margaretha cooks the Dowager''s meals with particular care because she has concluded that whatever the reason was, Walburga was right, and she would do it again if asked.',
        N'Kitchen-direct; the vocabulary of preparation and material and time; she manages conversations the way she manages a meal',
        N'Fast and confident; she makes decisions and moves on',
        N'Almost always reading what the other person needs before they ask for it',
        N'Gets faster and more efficient; the kitchen management voice takes over completely',
        N'Warm with the kitchen staff; warmer than anyone expects when someone is genuinely in distress',
        N'The kitchen; the household provisioning routes; the estate dining spaces',
        0, 0,
        N'Germanic woman in her late fifties, grey-brown hair pinned back, brown eyes, solid kitchen build, working dress clean at start of day, Rhine-Danube stone kitchen, authoritative grounded posture, the expression of someone who runs this room and everyone in it knows it, dark fantasy WW1-adjacent register',
        N'older woman, grey-brown hair, kitchen dress, stone kitchen, solid build, authoritative posture, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Margaretha Zink seeded.';
END
ELSE PRINT 'Margaretha Zink already exists.';
GO

-- 34. PAULUS GEIST — Sous-Chef
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Paulus Geist')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Paulus Geist', N'paulus-geist', N'canon', 1,
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
        @id, N'Paulus Geist', N'paulus-geist', N'Paulus', N'Geist', N'',
        N'human', N'human', N'male', N'he/him', 34, N'alive',
        N'Sous-Chef of House Fornax; manages bulk daily cooking and kitchen operations in coordination with Margaretha Zink',
        N'Paulus Geist has worked in the House Fornax kitchen for nine years, the last four as sous-chef, and he is good at the work and genuinely fond of Margaretha Zink, which makes his other arrangement more uncomfortable than it would be otherwise. He is efficient, pleasant with the kitchen staff, and reliable. He has never done anything that the kitchen would call wrong. What he does with the information that passes through the kitchen in the course of his work — the provisioning patterns that indicate military preparation, the dietary signals that indicate which officers are in the estate and when — is a separate question that happens in a different room.',
        N'The informant embedded in the household''s most central function. His motivation is sympathetic and his situation is coercive. The question of whether he would stop if he could is genuinely open.',
        N'No POV assigned. Third-limited; his register is kitchen-practical with an undercurrent of the specific tension of someone managing two obligations that cannot both be fully met.',
        N'House Fornax; Rhine-Danube territories; kitchen sous-chef; nine years of service',
        174, 78, N'solid kitchen build; the hands of someone who works with food',
        N'brown', N'short and practical', N'short',
        N'brown', N'warm medium-fair', N'clear; he takes care of himself',
        N'none',
        N'warm and practical; he moves well in the kitchen and slightly more carefully outside it',
        N'kitchen working dress; clean; he is the one who maintains the kitchen''s overall appearance standards when Margaretha is focused on the food',
        N'none',
        N'He manages the bulk of daily preparation — the bread, the stock, the staff meals. He coordinates with the kitchen assistant. He handles the provisions intake. He reports to Margaretha on everything. He writes one additional report, monthly, through a channel the Liturgy''s local functionary gave him four years ago.',
        N'He is a Liturgy informant. Not for ideology: four years ago the Liturgy''s local representative informed him that his sister, whom he believed was living independently in the eastern towns, was in fact a Liturgy ward — effectively a hostage, held in conditions that were not comfortable but that could become worse. The representative asked him to report on provisioning patterns, which indicate when military operations are being planned. He has been doing this monthly for four years. He does not know if his sister is still there. The representative will not confirm it either way. He cannot stop because he cannot verify that stopping is safe.',
        N'Kitchen-practical; warmer than most of the domestic staff in ordinary conversation',
        N'Measured; he is usually thinking about two things at once and the cadence shows it',
        N'Almost always managing his own discomfort with his situation rather than the content of the conversation',
        N'Gets quieter and more precise; the practical vocabulary takes over',
        N'Warm with the kitchen staff genuinely; everything else is managed',
        N'The kitchen; the household provisioning routes',
        0, 0,
        N'Germanic man in his mid-thirties, brown short hair, brown eyes, solid kitchen build, kitchen working dress, Rhine-Danube stone kitchen, warm practical posture with an undercurrent of tension, dark fantasy WW1-adjacent register',
        N'man in his thirties, brown hair, kitchen dress, stone kitchen, warm but slightly strained expression, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Paulus Geist seeded.';
END
ELSE PRINT 'Paulus Geist already exists.';
GO

-- 35. YUSUF ALMAZ — Kitchen Assistant; taken from Sphere 31
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Yusuf Almaz')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Yusuf Almaz', N'yusuf-almaz', N'canon', 1,
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
        @id, N'Yusuf Almaz', N'yusuf-almaz', N'Yusuf', N'Almaz', N'',
        N'human', N'human', N'male', N'he/him', 21, N'alive',
        N'Kitchen assistant; taken from Sphere 31 two years ago; placed in the House Fornax household by Liturgy allocation; an exceptional natural cook',
        N'Yusuf Almaz was taken from Sphere 31 two years ago. He came from a city, from a family, from a life that had a specific smell and a specific sound and a specific language that the Cauld does not speak. He arrived in the House Fornax kitchen as a placement, which means he was assigned rather than chosen, and he has spent two years making himself useful in the only way that seemed both achievable and safe. Margaretha Zink recognized his talent at the end of the first month and has been teaching him since. He is, in her estimation, a better natural cook than she was at his age. She has not told him this because she thinks it would go to his head. He would not let it go to his head. He is too focused on other things.',
        N'The Sphere 31 taken person closest to the center of the household — young, brilliant, and quietly running an escape plan that uses the same curiosity that makes him an exceptional cook. His presence makes the Liturgy''s extraction process visible in its human cost.',
        N'No POV assigned. Third-limited; his register is the register of someone speaking in a learned language while thinking in their first one — slightly formal, very precise, occasionally catching on idioms that do not translate.',
        N'Born in Sphere 31; taken by the Liturgy two years ago; placed in House Fornax; his origin has no name in the Cauld''s catalogues',
        172, 66, N'lean; the build of someone who grew up doing physical work; he is getting stronger from kitchen work',
        N'black', N'close-cropped', N'very short',
        N'dark brown', N'warm dark', N'clear and warm; he has his mother''s skin',
        N'none',
        N'careful and controlled in public; natural and expressive in the kitchen when no one of consequence is watching',
        N'household working dress provided by the estate; he wears it correctly and it does not quite fit him in a way that has nothing to do with the size',
        N'none',
        N'He manages the morning preparation — the vegetable work, the stock, the bread prep that Margaretha assigns him, and increasingly the dishes she lets him develop independently. He has been memorizing the Cauld''s written language by reading every text he can access — the kitchen records, the provisioning logs, the scraps of correspondence that arrive with the ingredient deliveries.',
        N'He remembers everything about Sphere 31. He remembers his mother''s kitchen — the specific smells, the specific spices, the specific quality of the light in the afternoon. He has been teaching himself to read the Cauld''s written language in secret, using any text he can access, because he intends to eventually find and understand the apparatus that took him. He does not know if it can be reversed. He does not know if going back is possible. He knows that he will not spend the rest of his life as an allocation in someone else''s kitchen without understanding what was done to him and why. He has been in the Cauld for two years and he has read himself to functional literacy. He is the most quietly dangerous person in the domestic wing.',
        N'Formally precise in the Cauld''s language; occasionally catches on idioms; switches internally to his native language when doing complex reasoning; the translation is visible if you know what to look for',
        N'Careful and deliberate; he selects words rather than speaking naturally because speaking naturally is in a language no one else here knows',
        N'Almost always translating — the language and also the situation; he does not take anything in the Cauld at face value yet',
        N'Goes more formal and more careful; the translation slows down under pressure but does not stop',
        N'Does not have one here; the intimacy he has is in the language he cannot speak aloud',
        N'The kitchen; the household; the provisioning routes; the locations of every text he has found that teaches him something',
        0, 0,
        N'young man of twenty-one, Ethiopian-heritage, black close-cropped hair, dark brown eyes, warm dark skin, lean kitchen worker''s build, estate working dress that does not quite fit, Rhine-Danube stone kitchen, careful precise posture that relaxes when no one important is watching, dark fantasy WW1-adjacent register',
        N'young man of Ethiopian heritage, black hair, working dress, stone kitchen, careful posture, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Yusuf Almaz seeded.';
END
ELSE PRINT 'Yusuf Almaz already exists.';
GO

-- 36. RUPRECHT WEISS — Butler
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ruprecht Weiss')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ruprecht Weiss', N'ruprecht-weiss', N'canon', 1,
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
        @id, N'Ruprecht Weiss', N'ruprecht-weiss', N'Ruprecht', N'Weiss', N'',
        N'human', N'human', N'male', N'he/him', 57, N'alive',
        N'Butler of House Fornax; manages the serving staff and formal occasions; has served three generations of the ruling family',
        N'Ruprecht Weiss carries the House Fornax''s dignity in his posture and has been doing so for thirty-two years. He manages the serving staff, the formal occasions, the table settings, the reception of guests, and the thousand small ceremonies that distinguish a Great House from a military garrison. He does all of this correctly, always, without fail, and the household has come to experience his correctness as a law of nature rather than a sustained act of professional will. He served Lord Hartmut''s father. He remembers everything about that service, including one evening thirty-two years ago that he has never discussed with anyone, except — once, in a moment of weakness he regrets — with the maid Klara Vogel.',
        N'The witness to the event that may have caused Friedrich Brenner''s death. His silence has been a choice renewed every day for twenty-five years, and Klara Vogel''s knowledge of it is the leverage he is most afraid of and most trusts her never to use.',
        N'No POV assigned. Third-limited; his register is butler-formal: precise, warm, the register of someone who exists to facilitate rather than to be noticed.',
        N'House Fornax; Rhine-Danube territories; career household butler; thirty-two years of service',
        180, 79, N'upright and maintained; the build of a man who has held the same posture for thirty-two years',
        N'silver-white', N'always correct', N'short',
        N'grey', N'fair', N'fine; composed',
        N'none',
        N'the specific uprightness of a butler; his posture is his professional identity and it does not relax',
        N'formal household livery; always complete; he has never been seen out of it during working hours',
        N'none',
        N'He manages the serving staff''s daily schedule. He coordinates formal events with the Seneschal. He receives guests at the front entrance. He manages the household silver and the formal dining arrangements. He takes his meals with the senior domestic staff. He reads in the evenings. He does not sleep easily.',
        N'He witnessed the argument between Friedrich Brenner and Lord Hartmut''s father on the night before Friedrich rode out. He was close enough to hear that the argument concerned Friedrich''s knowledge of Hartmut''s parentage. He heard the Lord tell Friedrich that raising the matter again would have consequences. Friedrich rode out the next morning. He did not come back. Ruprecht has spent twenty-five years deciding whether what he witnessed constitutes evidence of deliberate harm or merely the last argument of a man who then died in an unfortunate engagement. He told Klara Vogel, once, late, in a moment he regrets. She has never used it. He trusts her. He is terrified of what trust means in this context.',
        N'Butler-formal; precise; the vocabulary of formal household management used for every subject',
        N'Measured and complete; every sentence is fully constructed before it is delivered',
        N'Almost always managing the other person''s experience of the conversation rather than expressing his own',
        N'Goes more formal; the butler register becomes a complete wall',
        N'Exists with Klara Vogel; she is the only person in thirty-two years he has been honest with about something that mattered',
        N'The House Fornax estate; every formal space in it',
        0, 0,
        N'Germanic man in his late fifties, silver-white hair always correct, grey eyes, upright maintained build, formal household livery complete, Rhine-Danube stone estate formal spaces, posture that has not relaxed in thirty-two years, the expression of someone managing what he knows, dark fantasy WW1-adjacent register',
        N'older butler, silver hair, formal livery, stone estate interior, upright posture, composed expression, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Ruprecht Weiss seeded.';
END
ELSE PRINT 'Ruprecht Weiss already exists.';
GO

-- 37. BERTHA SCHMITT — Head Housekeeper
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Bertha Schmitt')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Bertha Schmitt', N'bertha-schmitt', N'canon', 1,
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
        @id, N'Bertha Schmitt', N'bertha-schmitt', N'Bertha', N'Schmitt', N'',
        N'human', N'human', N'female', N'she/her', 52, N'alive',
        N'Head Housekeeper of House Fornax; manages all household cleaning, laundry, and linen; has been in service for twenty-six years',
        N'Bertha Schmitt runs the household''s physical maintenance with a thoroughness that the Seneschal relies on and that the domestic staff navigates carefully, because thoroughness in a housekeeper means accountability in everyone else. She knows where everything is in every room she is responsible for, which is most of them, and she notices when anything changes. She is fair to the staff under her and brisk with anyone who wastes her time. She has been at this House for twenty-six years. Before that she was in another life entirely, which she has not discussed with anyone in the building and which sent a letter via an Oathless contact three months ago that she has read thirty times and answered once.',
        N'The character with a secret family in enemy territory — a situation that creates both sympathy and a genuine security liability the House does not know about.',
        N'No POV assigned. Third-limited; her register is housekeeper-practical: direct, warm when appropriate, not interested in social performance.',
        N'House Fornax; Rhine-Danube territories; career household housekeeper; twenty-six years of service; formerly resident in House Draught territory',
        163, 68, N'solid and practical; the build of someone who has done physical household work for twenty-six years',
        N'grey; was brown', N'pinned tightly back', N'short',
        N'brown', N'medium fair', N'weathered in an indoor way; the warmth and the cleaning materials have both been in her face for decades',
        N'none',
        N'efficient and purposeful; she does not stop moving during working hours',
        N'housekeeper dress; practical; always correct; she has a particular standard for her own appearance that she applies to her staff',
        N'none',
        N'She rises at the fourth hour. She inspects the common rooms before the household wakes. She assigns the daily cleaning schedule. She manages the linen inventory. She reviews the staff''s work personally. She takes her noon meal with the other senior domestic staff. She reads her letters in her room with the door locked.',
        N'Twenty-two years ago her husband defected to House Draught territory, taking their young son with him. She has never understood why he went or what he offered them. She has maintained her secret in service for twenty-two years by never speaking of her previous life. Three months ago a letter arrived through an Oathless courier — the first contact in eleven years. Her son is twenty-eight. He is alive. He does not know where she is. She has answered the letter but has not told him her location because she does not know what it means if House Draught intelligence learns that the House Fornax head housekeeper is his mother, or what it would mean for him if they found out first.',
        N'Direct and practical; the vocabulary of household management; does not elaborate when she does not need to',
        N'Efficient; says what needs to be said once',
        N'Almost always assessing whether the other person is going to make more work for her',
        N'Gets faster and more direct; the warmth disappears and the efficiency takes over completely',
        N'Was warm once in a different life; currently accessible to warmth with the domestic staff who have been here a long time',
        N'The House Fornax estate; all the rooms she is responsible for; the letter routes through the Oathless contact',
        0, 0,
        N'Germanic woman in her early fifties, grey hair pinned tightly back, brown eyes, solid practical build, housekeeper dress always correct, Rhine-Danube stone estate interior, efficient purposeful posture, the expression of someone who notices everything, dark fantasy WW1-adjacent register',
        N'housekeeper in her fifties, grey hair pinned back, practical dress, stone estate interior, efficient posture, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Bertha Schmitt seeded.';
END
ELSE PRINT 'Bertha Schmitt already exists.';
GO

-- 38. KLARA VOGEL — Household Maid; has been here since childhood
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Klara Vogel')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Klara Vogel', N'klara-vogel', N'canon', 1,
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
        @id, N'Klara Vogel', N'klara-vogel', N'Klara', N'Vogel', N'',
        N'human', N'human', N'female', N'she/her', 31, N'alive',
        N'Household maid; has been in the House Fornax household since she was eight years old; knows the building better than most of the family',
        N'Klara Vogel came to the House at eight, as the daughter of a previous kitchen maid who died of fever, and she has been here ever since. She knows the building''s sounds — which floorboard speaks on which step, which corridor carries voices from the study at night, which door sticks in cold weather. She is quiet and capable and has been trusted by the household staff since she was fourteen. She has been in love with the butler Ruprecht Weiss since she was nineteen and has not acted on it and does not plan to, which is not absence of feeling but a specific kind of patience that has its own weight.',
        N'The keeper of the building''s most intimate knowledge — and of the Butler''s most dangerous secret. Her choice never to use it is the most active and sustained ethical act in the domestic wing.',
        N'No POV assigned. Third-limited; her register is quiet and observant, the register of someone who has spent twenty-three years learning to be useful without being noticed.',
        N'House Fornax; Rhine-Danube territories; has been in service since age eight; born locally',
        161, 56, N'slight and precise; she moves through the House with the ease of someone for whom the building is a body they have lived in for twenty-three years',
        N'brown', N'braided and pinned', N'long when loose',
        N'grey-green', N'warm fair', N'clear; she has the complexion of someone who has always been taken care of within limits',
        N'none',
        N'quiet and economical; she knows how to be in a room without registering in it',
        N'household maid dress; neat; she has her own small standard that matches the House''s without being showy',
        N'none',
        N'She manages the upper household rooms — the family''s private quarters, the study corridors, the guest rooms. She knows the state of each room at every hour of the day. She takes her meals with the other domestic staff. She attends to her specific duties and is done with them more quickly than the Housekeeper expects, which gives her time she does not account for.',
        N'She is in love with Ruprecht Weiss and has been for twelve years. She knows about the Friedrich incident because Ruprecht told her one evening after a formal dinner, when the wine and the weight of it together were briefly too much for him. She has never used this knowledge. She has never referred to it in his presence. She could hold it over him and it would change nothing she actually wants: she does not want leverage over him. She wants what she cannot have and has chosen to want it quietly for twelve years rather than disrupt the only context in which they can be near each other. This is either wisdom or the most expensive form of devotion in the building.',
        N'Quiet and observant; the vocabulary of someone who has been listening for twenty-three years and speaking carefully for almost as long',
        N'Unhurried; she does not speak until she has decided it is worth speaking',
        N'Almost always attending to what is actually happening rather than what is being said',
        N'Goes quieter; becomes very still; answers questions with the minimum that is accurate',
        N'Has one and it is entirely private and she has made no attempt to act on it',
        N'The House Fornax estate; every room of the upper household; the domestic staff quarters',
        0, 0,
        N'Germanic woman in her early thirties, brown hair braided and pinned, grey-green eyes, slight precise build, neat household dress, Rhine-Danube stone estate private quarters, quiet economical movement, the expression of someone who knows a room completely, dark fantasy WW1-adjacent register',
        N'young woman, brown hair braided, grey-green eyes, household dress, stone estate interior, quiet expression, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Klara Vogel seeded.';
END
ELSE PRINT 'Klara Vogel already exists.';
GO

-- 39. OTTILIE HAUPT — Household Maid; recently arrived; Atrament intelligence asset
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ottilie Haupt')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ottilie Haupt', N'ottilie-haupt', N'canon', 1,
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
        @id, N'Ottilie Haupt', N'ottilie-haupt', N'Ottilie', N'Haupt', N'',
        N'human', N'human', N'female', N'she/her', 24, N'alive',
        N'Household maid; arrived at House Fornax six months ago; presented excellent references; is a House Atrament intelligence asset',
        N'Ottilie Haupt arrived six months ago with references that the Seneschal verified through the standard channels, which she had arranged through a House Atrament handler who had prepared the standard channels for exactly this purpose. She is good at the work — she learns quickly, does not complain, and has the kind of pleasant ordinary presence that does not attract attention. She has been trying to access something useful for six months and has not yet succeeded, which her handler attributes to the household''s security culture and which Ottilie attributes to the fact that she has started to like the people here, and liking them is making her slow.',
        N'The spy who is being compromised by the proximity she was sent to exploit. Her case is a cleaner version of what Ambassador Sternfeld is experiencing at Atrament — loyalty erosion through genuine affection.',
        N'No POV assigned. Third-limited; her register is the slightly careful register of someone performing ordinariness.',
        N'House Atrament asset; placed in House Fornax six months ago; Atrament origin',
        165, 58, N'slight and tidy; the build someone selects when they want to appear unthreatening',
        N'light brown', N'neat; always the same', N'medium',
        N'blue', N'fair', N'clear; she takes care of herself in a way that is slightly more deliberate than ordinary household maids manage',
        N'none',
        N'pleasant and neat; the posture of someone performing ordinary presence',
        N'household maid dress; correct; slightly too correct for someone who has only been here six months',
        N'none',
        N'She performs her assigned household duties correctly. She has been mapping the estate''s daily rhythms — when the Lord meets with the Chancellor, which corridors are empty at which hours, how the domestic staff sorts correspondence before it reaches the Seneschal. She has not yet found access to anything that her handler would classify as actionable. She attends the staff meals and listens.',
        N'She was sent here as a House Atrament intelligence asset. Her handler underestimated how long it would take to gain access to anything useful. What the handler did not account for is that six months of living alongside the household staff of House Fornax would produce genuine affection — for Klara Vogel''s quiet competence, for Margaretha Zink''s warmth in the kitchen, for the household''s own specific personality. She is becoming unable to produce the actionable report her handler needs. She has not told the handler this. She does not know yet whether she is going to complete her assignment or find a way not to. She has been delaying this decision for three months.',
        N'Pleasant and slightly careful; Atrament idiom visible if you know what to listen for',
        N'Warm and ordinary; she is good at sounding like someone who has nothing particular on her mind',
        N'Almost always managing the gap between her affection for the people around her and her assignment',
        N'Gets more ordinary-pleasant; the social performance increases when she is afraid',
        N'Beginning to develop one with Klara Vogel; this is a complication she has not reported to her handler',
        N'The House Fornax estate; the Atrament handler''s contact schedule',
        0, 0,
        N'young Germanic woman of twenty-four, light brown neat hair, blue eyes, slight tidy build, household maid dress slightly too correct, Rhine-Danube stone estate interior, pleasant ordinary presence, the expression of someone performing not having a particular thing on her mind, dark fantasy WW1-adjacent register',
        N'young woman, light brown hair, maid dress, stone interior, pleasant expression, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Ottilie Haupt seeded.';
END
ELSE PRINT 'Ottilie Haupt already exists.';
GO

-- 40. ELSA NEHR — Household Maid; listens too carefully
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Elsa Nehr')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Elsa Nehr', N'elsa-nehr', N'canon', 1,
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
        @id, N'Elsa Nehr', N'elsa-nehr', N'Elsa', N'Nehr', N'',
        N'human', N'human', N'female', N'she/her', 28, N'alive',
        N'Household maid; six years in service at House Fornax; listens more carefully than her role requires',
        N'Elsa Nehr has been a household maid at House Fornax for six years and has spent most of that time in positions that happen to be adjacent to conversations. She is not unintelligent; she is also not working for anyone. She is not an asset or a spy or a recruited operative. She is a woman who finds people fascinating and wrong about things, who has been listening at keyholes and in corridors for six years because the interior life of a Great House is the most interesting thing she has ever encountered, and who has assembled, over that time, a comprehensive picture of House Fornax''s actual political situation that she intends to sell at the moment of maximum value.',
        N'The independent operator — no handler, no ideology, just accumulation and patience. She is the most purely mercenary figure in the household, which paradoxically makes her less predictable than the actual operatives.',
        N'No POV assigned. Third-limited; her register is pleasant and unremarkable until it very suddenly is not.',
        N'House Fornax; Rhine-Danube territories; household maid; six years of service; no other affiliation',
        167, 61, N'medium and unremarkable; she has spent six years making her physical presence unremarkable and she is very good at it',
        N'dark brown', N'neatly done; correct', N'below shoulder',
        N'brown', N'warm fair', N'clear; ordinary',
        N'none',
        N'designed to be ordinary; she has spent six years calibrating the exact level of visible presence that makes her invisible',
        N'household maid dress; indistinguishable from the other maids; this is intentional',
        N'none',
        N'She performs her household duties. She positions herself. She listens. She has been doing this for six years and no one has noticed because her household work is perfectly adequate and she does nothing that would give a reason to notice. She keeps her records in her head.',
        N'She has assembled, over six years of deliberate listening, a comprehensive picture of House Fornax''s actual political situation: the Chancellor''s arrangement with Atrament, the Treasurer''s insolvency discovery, the Spymaster''s unreported asset, the Dowager''s genealogical secret, the Butler''s knowledge of Friedrich''s death. She has not approached anyone yet because she has not identified the buyer who can pay enough to make the exposure worth the risk to herself. She is not working for any House. She is not working for the Liturgy. She is working for herself, and she intends to sell at exactly the right moment. She has been refining her assessment of what the right moment looks like for eighteen months.',
        N'Pleasant and ordinary; she has no identifiable register because she has spent six years ensuring she does not have one',
        N'Paced for the conversation; she matches the other person''s rhythm so precisely it reads as natural',
        N'Almost always cataloguing rather than participating',
        N'Does not change; she has prepared for pressure by having no reaction to it that reveals anything',
        N'Does not have one; intimacy creates a record and she does not leave records',
        N'The House Fornax estate; every corridor and room she has found a reason to be near',
        0, 0,
        N'Germanic woman in her late twenties, dark brown hair neatly done, brown eyes, medium unremarkable build, household dress indistinguishable from the others, Rhine-Danube stone estate corridors, perfectly calibrated ordinary presence, the expression of someone not listening that is actually the expression of someone listening, dark fantasy WW1-adjacent register',
        N'young woman, dark hair, maid dress, stone corridor, perfectly ordinary expression, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Elsa Nehr seeded.';
END
ELSE PRINT 'Elsa Nehr already exists.';
GO

-- 41. AGNETHA BOCK — Lady's Personal Attendant
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Agnetha Bock')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Agnetha Bock', N'agnetha-bock', N'canon', 1,
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
        @id, N'Agnetha Bock', N'agnetha-bock', N'Agnetha', N'Bock', N'',
        N'human', N'human', N'female', N'she/her', 29, N'alive',
        N'Personal attendant to Lady Ilse Brenner; came with Ilse from House Calyx at Ilse''s marriage; one of the few remaining Calyx loyalists in the household',
        N'Agnetha Bock came to House Fornax with Lady Ilse thirty-four years ago — which means she arrived at five, as the daughter of Ilse''s previous attendant, and has grown up here while remaining, in her loyalty, a Calyx person. She manages Lady Ilse''s private schedule, her correspondence, her dress, and the thousand private matters that belong to a lord''s wife rather than to the household generally. She loves Lady Ilse in the way of someone who has been adjacent to a person for twenty-nine of their own years. This love is the most complex thing in her life.',
        N'The attendant who is protecting Lady Ilse from a danger she is beginning to suspect is real — who has been silently reading evidence of what Ilse is doing and has not decided whether protection means silence or intervention.',
        N'No POV assigned. Third-limited; her register is attendant-close: warm, precise, and attuned to the person she serves.',
        N'Born House Calyx; came to House Fornax with Lady Ilse at age five; thirty-four years in Fornax service',
        162, 57, N'slight; the build of someone who matches their presence to the person they serve',
        N'dark brown', N'simply worn', N'medium',
        N'brown', N'warm olive-fair; Calyx heritage visible in the complexion', N'composed',
        N'none',
        N'attuned to the Lady''s presence; anticipatory; the movement of someone always thinking a half-step ahead of the person they serve',
        N'attendant dress; Calyx-influenced in the cut; she has maintained this as a quiet statement for thirty-four years',
        N'none',
        N'She manages Lady Ilse''s morning. She prepares her correspondence. She manages her private schedule. She helps compose and send Ilse''s letters. She has been reading those letters, including the ones addressed to Calyx, for three years. She has not told anyone. She has been composing in her head what she would say if she decided to.',
        N'She has been reading Lady Ilse''s correspondence with the Calyx family for three years — not to report it, but because she loves Ilse and was afraid for her and wanted to understand what Ilse was doing. What she has read has moved past the question of understanding into the question of what she is obligated to do. The letters are no longer homesickness. They are exchange. The content would be characterized as intelligence by anyone with experience. Agnetha has experience — she grew up in a House. She loves Ilse. She has been deciding for eight months whether love means protecting Ilse from consequences or protecting the House from what Ilse is doing. She cannot do both.',
        N'Attendant-close; warm; vocabulary calibrated entirely to Lady Ilse''s needs and comfort',
        N'Anticipatory; she begins responses before the other person has finished the thought',
        N'Almost always managing how much of what she knows is visible',
        N'Goes quiet and very careful; the anticipatory quality disappears and she becomes completely controlled',
        N'Has one and it is entirely with Lady Ilse and it is the source of her paralysis',
        N'Lady Ilse''s private quarters; the household spaces Ilse moves through; the correspondence routes',
        0, 0,
        N'young woman of twenty-nine, dark brown hair simply worn, brown eyes, warm olive-fair Calyx complexion, attendant dress with Calyx-influenced cut, Rhine-Danube stone estate private quarters, anticipatory attuned posture, the expression of someone deciding something, dark fantasy WW1-adjacent register',
        N'young woman, dark hair, attendant dress, stone private quarters, composed expression, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Agnetha Bock seeded.';
END
ELSE PRINT 'Agnetha Bock already exists.';
GO

-- 42. JOSEF WURM — Stable Master
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Josef Wurm')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Josef Wurm', N'josef-wurm', N'canon', 1,
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
        @id, N'Josef Wurm', N'josef-wurm', N'Josef', N'Wurm', N'',
        N'human', N'human', N'male', N'he/him', 55, N'alive',
        N'Stable Master of House Fornax; manages all horses and transportation; knows who leaves the estate at night and when',
        N'Josef Wurm has managed the House Fornax stables for twenty years and knows horses the way the Archivist knows documents: their individual histories, their specific temperaments, the way the right horse for a night ride differs from the right horse for a formal procession. He also knows, because the stables are how people leave, who leaves at night, when, and in what direction. He has never been asked to report this. He has chosen not to volunteer it. The reason for his discretion is personal and is the kind of reason that forecloses most moral arguments about what he should be doing.',
        N'The keeper of the estate''s exit record — and someone who has been running an unauthorized escape network that is the most active humanitarian operation in the building. His exposure would destroy him and the network simultaneously.',
        N'No POV assigned. Third-limited; his register is stable-direct: practical, warm with animals and people he trusts, laconic with everyone else.',
        N'House Fornax; Rhine-Danube territories; stable master; twenty years of service; his wife was taken from Sphere 31 and disappeared',
        179, 87, N'solid; the build of twenty years of physical stable work',
        N'grey-brown', N'short and practical', N'short',
        N'brown', N'weathered fair', N'marked; he is outdoors constantly',
        N'none',
        N'steady and grounded; the posture of someone whose center of gravity is always correct',
        N'stable working clothes; practical; always marked by the horses',
        N'none',
        N'He is in the stables at the fourth hour. He manages the feeding and exercise schedule. He maintains the equipment. He records the stable logs — which horses went out, with whom, in what direction. He maintains a separate, private record that does not match the official stable log in two specific ways.',
        N'His wife was taken from Sphere 31 twenty years ago, placed in service in a House he was never told the name of, and disappeared within a year of the placement. He was never given a reason or a location. He has been providing horses, false travel records, and safe routing information to a network of Sphere 31 taken persons attempting to flee the Cauld entirely — not because he knows where they are going or if escape is possible, but because he knows what was done to his wife and he cannot undo it and this is the only adjacent thing he can do. Two of his grooms know. He has not asked them to participate. They have anyway.',
        N'Laconic; the vocabulary of horses and practical logistics; warm only with people he has decided to trust',
        N'Unhurried; he has been in the stables for twenty years and the stables move at their own pace',
        N'Almost always deciding whether the person in front of him is someone he can help or someone who could cost him the network',
        N'Does not change; the practical register becomes more spare but does not alter in kind',
        N'Was warm once in a marriage that lasted three years before the Liturgy ended it; the warmth has gone into the horses and the network',
        N'The stables; the estate grounds; the night routes used by the network',
        0, 0,
        N'Germanic man in his mid-fifties, grey-brown short hair, brown eyes, solid stable-worker build, practical work clothes marked by horses, Rhine-Danube stone stables, steady grounded posture, the expression of someone who knows which way everyone leaves, dark fantasy WW1-adjacent register',
        N'stable master in his fifties, grey hair, work clothes, stone stable, solid posture, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Josef Wurm seeded.';
END
ELSE PRINT 'Josef Wurm already exists.';
GO

-- 43. TOBIAS KORN — Groom
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Tobias Korn')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Tobias Korn', N'tobias-korn', N'canon', 1,
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
        @id, N'Tobias Korn', N'tobias-korn', N'Tobias', N'Korn', N'',
        N'human', N'human', N'male', N'he/him', 22, N'alive',
        N'Groom in the House Fornax stables; works under the Stable Master; three years of service',
        N'Tobias Korn is twenty-two and has been working the House Fornax stables for three years. He is good with horses and comfortable with physical work and has the kind of uncomplicated cheerfulness that makes him easy to be around, which is probably why Josef Wurm has kept him close rather than sending him to the outer paddocks with the junior staff. He knows more than he is supposed to know. He has known it for a year. He has said nothing because he respects Josef Wurm more than he has respected anything in his working life and because the silence was already there when he helped the first person pack a saddlebag and he stepped into it rather than out of it.',
        N'The accidental accomplice — someone who became complicit not through recruitment but through a single act of ordinary kindness that he cannot now undo. His dilemma is whether the fear or the conviction comes first.',
        N'No POV assigned. Third-limited; his register is practical and slightly younger than his years suggest.',
        N'House Fornax; Rhine-Danube territories; groom; three years of service',
        176, 73, N'lean and capable; the build of someone who does physical work and is still young enough to carry it lightly',
        N'brown', N'short; unkempt by end of day', N'short',
        N'green', N'fair weathered', N'clear; outdoor complexion',
        N'none',
        N'easy and physical; he is comfortable in his body in the way of someone who has not yet had reason to be uncomfortable in it',
        N'stable working clothes; practical; always marked by the work',
        N'none',
        N'He manages his assigned horses through the day. He assists with the equipment maintenance. He is present in the stables more hours than his formal schedule requires because he likes being there. He helped pack a saddlebag for a Sphere 31 person leaving on one of Josef Wurm''s routes about a year ago, without being asked, because the person had too many things and too few hands.',
        N'He has witnessed Josef Wurm''s network for a year and has said nothing to anyone. He tells himself it is loyalty. The truth is he helped one person pack their saddlebag and is now complicit and the fear of what that means is larger than the fear that drove Josef to start the network. He does not know what he believes about what the network is doing. He knows he helped one person who was shaking and frightened and had too much to carry, and he could not have not helped, and now he is a part of something he did not sign up for and finds, on balance, that he cannot bring himself to regret.',
        N'Practical and direct; the vocabulary of someone whose education was the stables',
        N'Easy and unhurried; he is comfortable speaking',
        N'Almost always deciding in real time rather than ahead of time',
        N'Gets quieter; the easy quality drops; he becomes very direct',
        N'Open with Josef Wurm; more careful with everyone else since the saddlebag',
        N'The stables; the estate grounds',
        0, 0,
        N'young Germanic man of twenty-two, brown hair unkempt by end of day, green eyes, lean capable build, stable work clothes, Rhine-Danube stone stables, easy physical posture, the expression of someone who has done something he cannot undo and is not sure he wants to, dark fantasy WW1-adjacent register',
        N'young man, brown hair, work clothes, stone stable, easy posture, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Tobias Korn seeded.';
END
ELSE PRINT 'Tobias Korn already exists.';
GO

-- 44. LISE RAAB — Groom; handles the warhorses; informant for Captain Eiche
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Lise Raab')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Lise Raab', N'lise-raab', N'canon', 1,
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
        @id, N'Lise Raab', N'lise-raab', N'Lise', N'Raab', N'',
        N'human', N'human', N'female', N'she/her', 26, N'alive',
        N'Groom in the House Fornax stables; specializes in the warhorses; five years of service',
        N'Lise Raab handles the warhorses specifically — the animals that go out with the Corps, that are used in night operations, that are maintained to a different standard than the riding horses. She knows each one. She has the specific competence with large dangerous animals that is not fearlessness but rather precise attention — knowing where the animal is going to move before it moves. She has been in the stables for five years and she has been providing information to Captain Baldur Eiche for eight months in exchange for a promise she is beginning to doubt he can keep.',
        N'The groom caught between a promised reward and growing suspicion about what her information is actually being used for. Her situation creates an unintentional connection between the Scrying installation''s internal skepticism and the stable network.',
        N'No POV assigned. Third-limited; her register is direct and physical, the register of someone whose primary skill is reading non-verbal signals.',
        N'House Fornax; Rhine-Danube territories; groom; five years of service',
        168, 64, N'fit and capable; the specific build of someone who handles large animals regularly',
        N'dark blonde', N'braided; practical', N'long when loose',
        N'hazel', N'fair weathered', N'clear; outdoor work',
        N'none',
        N'confident and physical; the specific ease of someone at home with animals',
        N'stable working clothes; practical; she does not think about clothing',
        N'none',
        N'She manages the warhorse assignments and care schedule. She is present at the stables for longer hours than any other groom because the warhorses require it. She has been reporting which horses go out at which hours on which routes to Captain Baldur Eiche monthly for eight months.',
        N'Captain Eiche approached her eight months ago and told her he was conducting security reviews of the stable''s night operations and needed independent confirmation of the official records. He promised her a recommendation for Knight''s infusion candidacy in exchange for monthly reports on which horses were used on which routes at which hours. She has been providing them. She has begun to notice that his questions are not about security patterns — they are specifically about routes used by the Scrying installation staff and, she has recently realized, about Josef Wurm''s network, though Eiche has not named it as such. She does not know whether Eiche knows about the network or is approaching it from the Scrying angle. She is frightened. She has not stopped because she wants the candidacy and has not found a way out that does not cost her it.',
        N'Direct and physical; reads a room the way she reads a horse — by what it is about to do rather than what it is saying',
        N'Fast; she processes quickly and speaks quickly',
        N'Almost always reading the physical signals rather than the content',
        N'Goes very fast and very direct; the horse-reading quality becomes the only operating mode',
        N'Warm with the horses; more guarded with people since Eiche',
        N'The stables; the warhorse paddocks; the night routes she has been logging',
        0, 0,
        N'young Germanic woman of twenty-six, dark blonde braided hair, hazel eyes, fit capable build from animal handling, stable work clothes, Rhine-Danube stone stables or paddock, confident physical posture, the expression of someone reconsidering a decision she already made, dark fantasy WW1-adjacent register',
        N'young woman, dark blonde braid, work clothes, stable setting, capable posture, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Lise Raab seeded.';
END
ELSE PRINT 'Lise Raab already exists.';
GO

-- 45. ALWIN GEYER — Groundskeeper; 40 years on the estate
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Alwin Geyer')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Alwin Geyer', N'alwin-geyer', N'canon', 1,
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
        @id, N'Alwin Geyer', N'alwin-geyer', N'Alwin', N'Geyer', N'',
        N'human', N'human', N'male', N'he/him', 68, N'alive',
        N'Groundskeeper of House Fornax estate; manages the grounds and outer defenses; has been on the estate for forty-three years',
        N'Alwin Geyer has managed the House Fornax estate grounds for forty-three years and knows every inch of them — the drainage patterns, the places where the outer wall is weakest, the section of the east garden that drains poorly in autumn and where, if you were to bury something, it would stay buried. He is a deliberate and quiet man who does his work without requiring much management. The Seneschal considers him the most reliable member of the non-domestic staff. He is seventy-eight years old in his habits and sixty-eight in his body and has been thinking about his own burial for about six years, which is when he started finding the thought not unpleasant.',
        N'The man who knows where the bodies are — literally. His situation is the most extreme version of institutional loyalty in the household, and his equanimity about it is the most unsettling thing about him.',
        N'No POV assigned. Third-limited; his register is groundskeeper-deliberate: slow, specific, the vocabulary of land and weather.',
        N'House Fornax; Rhine-Danube territories; estate groundskeeper; forty-three years of service',
        172, 76, N'solid and weathered; the build of someone who has done outdoor physical work for forty years; beginning to slow',
        N'white; was dark brown', N'short and unkempt', N'short',
        N'grey', N'deeply weathered brown-fair', N'heavily lined; forty-three years of outdoor work in every line',
        N'none',
        N'slow and deliberate; the movement of someone who has never needed to hurry to get where he is going',
        N'outdoor working clothes; practical; he does not attend to his appearance beyond cleanliness',
        N'none',
        N'He is outside at first light. He manages the grounds, the outer wall maintenance, the drainage, the kitchen garden. He works steadily without urgency. He eats his noon meal outside when the weather allows. He walks the perimeter of the east garden at least once a week.',
        N'There are bodies in the east garden. Alwin Geyer buried them there in the early years of Lord Hartmut''s rule, when certain political problems were resolved in ways that the official record does not reflect. Hartmut gave the orders. Alwin did the work. There were four people, over a period of about three years, in the early period of the current Lord''s ascension. They have been in the east garden for nearly thirty years and no one has dug there since. Alwin and Hartmut have never spoken of it. Alwin has been tending the garden above them for thirty years. He has already decided where he will be buried and it is in that same garden, and he finds this thought peaceful rather than troubling.',
        N'Groundskeeper-deliberate; the vocabulary of soil and weather and season; does not say more than is needed',
        N'Very slow; he has all the time he needs and he uses it',
        N'Almost always deciding whether the other person can handle what he would actually say',
        N'Does not change; he has been at peace with what he knows for thirty years',
        N'Does not have one; forty-three years of working the same ground in the same way is the closest thing to it',
        N'The estate grounds; the outer wall; the east garden',
        0, 0,
        N'very old Germanic man of sixty-eight, white unkempt hair, grey eyes, solid weathered build beginning to slow, outdoor working clothes, Rhine-Danube estate grounds especially the east garden, deliberate slow movement, the expression of someone at peace with what he knows, dark fantasy WW1-adjacent register',
        N'old groundskeeper, white hair, work clothes, estate garden, slow deliberate posture, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Alwin Geyer seeded.';
END
ELSE PRINT 'Alwin Geyer already exists.';
GO

-- 46. DR. HANNELORE SEEL — House Physician
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Hannelore Seel')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Hannelore Seel', N'hannelore-seel', N'canon', 1,
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
        @id, N'Hannelore Seel', N'hannelore-seel', N'Hannelore', N'Seel', N'Doctor',
        N'human', N'human', N'female', N'she/her', 50, N'alive',
        N'House Physician of House Fornax; treats the family and high-ranking staff; has access to every person''s ailments and therefore to their secrets',
        N'Hannelore Seel has been the House Physician for sixteen years and in that time she has become the person in the household who knows the most about bodies — which is also, in a Great House, the person who knows the most about everything else. The ailments of the powerful are never purely physical. She is discreet in the way of someone who has sworn to be and has kept that oath for sixteen years, which has not prevented her from accumulating a comprehensive understanding of the household''s actual condition. She knows something about Lord Hartmut that she has told him and told no one else, and the weight of it has been in her face for eight months.',
        N'The physician who holds the House''s medical future — specifically the Lord''s prognosis — and has been ordered to suppress it. Her position is the most acute of the household''s ethical traps.',
        N'No POV assigned. Third-limited; her register is medical-precise: the vocabulary of diagnosis, with a warmth underneath it that emerges in the treatment room.',
        N'House Fornax; Rhine-Danube territories; House physician; sixteen years of service',
        166, 61, N'slight and precise; the build of someone who works with attention rather than force',
        N'dark brown going grey at the temples', N'pinned back during examinations; simply worn otherwise', N'medium',
        N'brown', N'warm fair', N'composed; she manages her expression as a professional practice',
        N'none',
        N'economical and deliberate; the movement of someone accustomed to working in small careful spaces',
        N'physician working dress; clean; practical',
        N'none',
        N'She sees patients from the seventh hour. She manages the household''s medical supplies. She files treatment records. She conducts the quarterly health reviews for the senior domestic staff. She writes in her private medical journal each evening — observations, diagnoses, the things she cannot put in the official record.',
        N'She has documented that Lord Hartmut''s Paladin augmentation is degrading in an unusual pattern — his body is rejecting the transformation in ways she has not encountered in her training or her sixteen years of practice. Within five to seven years, at current progression, he will begin to lose cognitive function. The process is irreversible. She told him six months ago. He received the information without visible response and told her to keep it out of all records — the official records and her private ones both. She has kept it out of the official records. She has not kept it out of her private journal, because she cannot. The entry is encrypted. She does not know what she will do when the encryption is not enough.',
        N'Medical-precise; the vocabulary of physiology and diagnosis; warm when she is treating rather than discussing',
        N'Measured and complete; she is trained to deliver information that is difficult to hear',
        N'Almost always assessing the other person''s capacity to handle what she might say',
        N'Gets quieter and more precise; the diagnostic mode takes over completely',
        N'Warm in the treatment context; the one space she trusts',
        N'The estate medical rooms; the household; the Corps infirmary when consulting with Furch',
        0, 0,
        N'Germanic woman in her early fifties, dark brown hair going grey at temples, brown eyes, slight precise build, physician working dress, Rhine-Danube stone estate medical room, economical deliberate movement, the expression of someone managing what she knows, dark fantasy WW1-adjacent register',
        N'woman physician in her fifties, dark hair greying, working dress, stone medical room, precise expression, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Hannelore Seel seeded.';
END
ELSE PRINT 'Hannelore Seel already exists.';
GO

-- 47. FATHER BENEDIKT RAUM — Chaplain / Bheur Priest
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Benedikt Raum')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Benedikt Raum', N'benedikt-raum', N'canon', 1,
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
        @id, N'Benedikt Raum', N'benedikt-raum', N'Benedikt', N'Raum', N'Father',
        N'human', N'human', N'male', N'he/him', 60, N'alive',
        N'Chaplain and Bheur Priest of House Fornax; officiates at House rites; the theological figure for the family',
        N'Father Benedikt Raum has been the House chaplain for twenty-two years and has officiated at every significant rite in that time — births, marriages, funerals, pre-campaign blessings, and the specific quiet ceremony that precedes a Transmutation infusion. He came to his position as a man of faith, which is to say he came with belief. He has spent twenty-two years watching what faith encounters in a Great House at war. He has not lost his belief. He has arrived at a version of it that would not be recognized by the institution that trained him and that he has been keeping to himself for three years, since the first time he was allowed to attend a Catalyst infusion from beginning to end.',
        N'The theological witness — the person whose institutional role requires him to provide comfort and meaning, and who has found evidence that the meaning structure he provides may be true in ways the Liturgy would find dangerous.',
        N'No POV assigned. Third-limited; his register is warm and theologically precise, the register of a man whose faith has become more specific rather than less through what he has observed.',
        N'House Fornax; Rhine-Danube territories; Bheur Priest; twenty-two years of service',
        170, 73, N'medium; the body of a man who has spent twenty-two years between ceremony and ordinary life',
        N'white; was grey; was brown', N'short and simple', N'short',
        N'brown', N'fair', N'composed; he has the face of someone who has had twenty-two years of practice listening',
        N'none',
        N'ceremonially correct in formal contexts; gentle in ordinary ones',
        N'priestly vestments for formal occasions; plain working dress otherwise',
        N'none',
        N'He conducts the morning rite at the sixth hour. He is available to the household for private counsel. He officiates at the House''s ceremonies. He has been attending Transmutation infusions as a spiritual observer for three years, since Commander von Roth granted him permission. He keeps a private theological journal.',
        N'He has been attending Transmutation infusions for three years and during that time he has observed something he cannot classify within any theological framework he was trained in: at the moment of the Catalyst''s crisis — the point at which the infusion either transforms or kills — he has seen, three times, something leave the body that is not breath and not heat and not any biological process he can name. He is a Bheur Priest. The Bheur doctrine holds that the afterlife is unconfirmable. He has been observing something that he believes is evidence that the doctrine is wrong. He is terrified of what this means for the Liturgy''s practice of membrane transit, for what happens to the dead who pass through the Liturgy''s operations, and for what twenty-two years of offering comfort based on unconfirmable belief has meant.',
        N'Theologically precise and warm; the vocabulary of Bheur doctrine used with care; he quotes carefully because he has started to believe the quotes are more accurate than previously understood',
        N'Unhurried and complete; the cadence of someone who has been delivering difficult truths gently for twenty-two years',
        N'Almost always attending to what the other person is carrying rather than what they are saying',
        N'Goes very still and very warm; his response to crisis is presence',
        N'Warm and genuine; he is the most accessible of the senior household figures',
        N'The House chapel; the estate; the infirmary when observing infusions',
        0, 0,
        N'older Germanic man of sixty, white hair, brown eyes, medium build, priestly vestments or plain working dress, Rhine-Danube stone chapel, gentle ceremonially correct posture, the expression of someone carrying a discovery they do not know what to do with, dark fantasy WW1-adjacent register',
        N'older priest, white hair, vestments or plain dress, stone chapel, gentle composed expression, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Benedikt Raum seeded.';
END
ELSE PRINT 'Benedikt Raum already exists.';
GO

-- 48. SIGRUN FELS — Librarian and Tutor
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Sigrun Fels')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Sigrun Fels', N'sigrun-fels', N'canon', 1,
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
        @id, N'Sigrun Fels', N'sigrun-fels', N'Sigrun', N'Fels', N'',
        N'human', N'human', N'female', N'she/her', 43, N'alive',
        N'Librarian and Tutor of House Fornax; manages the household library and educates the younger family members',
        N'Sigrun Fels manages the House Fornax library with the same care she applies to her teaching, which is to say she treats both the books and the students as things that deserve to be taken seriously without being overwhelmed. She is precise and warm and has a gift for finding the question inside the student''s actual question, which Klaus Brenner in particular finds both useful and occasionally unnerving. She has been at the House for eleven years. She is, in the library''s official catalogue, the author of several anonymous agricultural historical texts that the catalogue does not attribute to anyone. The most recent of these was added three years ago and is still being written.',
        N'The intellectual in the building who has been working on something that would be classified as sedition and hiding it in plain sight. Her work is the only comprehensive history of the Living War in the building, and it assigns blame.',
        N'No POV assigned. Third-limited; her register is tutor-warm: engaged, specific, the register of someone who finds ideas genuinely interesting and expects others to as well.',
        N'House Fornax; Rhine-Danube territories; library and tutor; eleven years of service',
        164, 59, N'slight; the build of someone who is always slightly more interested in what she is thinking than in her physical surroundings',
        N'auburn fading to brown', N'worn carelessly; she tucks it behind her ear constantly', N'below shoulder',
        N'green', N'warm fair', N'clear; she is mostly indoors but this suits her',
        N'none',
        N'distracted and warm; she frequently begins sentences about the room she is in and finishes them about the book she is thinking about',
        N'working dress; practical; always slightly ink-stained; she does not notice',
        N'none',
        N'She manages the library catalogue. She conducts Klaus Brenner''s lessons each morning. She assists Albrecht with research when asked. She spends her evenings reading and writing. She has been writing for eight years.',
        N'She wrote an illegal text — a history of the Living War that assigns blame to all seven Houses equally, including Fornax, documenting the decisions, the cover-ups, and the systematic atrocities that the official histories of each House have redacted from the record. She has been adding to it for eight years. It is concealed in the library in a binding marked as an agricultural census from sixty years ago. She does not know what she intends to do with it. She is not ready to expose it. She is not willing to destroy it. She keeps writing because she believes the record should exist even if no one ever reads it, which she is aware is a form of faith that sits oddly with her otherwise empirical temperament.',
        N'Tutor-warm; engaged; the vocabulary of someone who has read everything in the library and thought about most of it',
        N'Variable; she speeds up when interested and slows when formulating something precise',
        N'Almost always finding the question inside the question',
        N'Gets faster and more precise; the intellectual engagement intensifies under pressure rather than shutting down',
        N'Warm and genuinely interested; she is one of the few people in the household who listens to Klaus Brenner as though what he says matters',
        N'The library; the tutoring room; the household',
        0, 0,
        N'Germanic woman in her early forties, auburn-brown hair carelessly tucked behind ear, green eyes, slight distracted build, ink-stained working dress, Rhine-Danube stone library, warm distracted posture, the expression of someone finishing a thought about something you did not say, dark fantasy WW1-adjacent register',
        N'woman in her forties, auburn hair, ink-stained dress, stone library, warm distracted expression, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Sigrun Fels seeded.';
END
ELSE PRINT 'Sigrun Fels already exists.';
GO

-- 49. ANTON WIRTH — Page; Klaus Brenner's secret
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Anton Wirth')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Anton Wirth', N'anton-wirth', N'canon', 1,
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
        @id, N'Anton Wirth', N'anton-wirth', N'Anton', N'Wirth', N'',
        N'human', N'human', N'male', N'he/him', 17, N'alive',
        N'Page of House Fornax; carries messages and dispatches throughout the household; hears everything',
        N'Anton Wirth has been a page at House Fornax for four years and he has spent those four years moving through the building in the way that pages do — quickly, quietly, authorized to go everywhere, too young for most of the household to think carefully about what he is hearing. He is seventeen and he knows the building''s conversations as well as Klara Vogel knows its floorboards. He has never deliberately listened for anything other than the specific thing he was sent to carry. The information he has accumulated is a byproduct rather than a goal. What he does with Klaus Brenner in the music room in the late afternoons is not a byproduct. That is the most deliberate thing in his life.',
        N'The page whose position makes him simultaneously the most exposed and the most mobile person in the household. His relationship with Klaus Brenner is the one thing he has not been careful about and the one thing that could end everything.',
        N'No POV assigned. Third-limited; his register is the register of a seventeen-year-old trying to be as old as the situation requires.',
        N'House Fornax; Rhine-Danube territories; page; four years of service',
        170, 62, N'slim; still growing; the build of someone between the child he was and the adult he is becoming',
        N'dark brown', N'short; practical', N'short',
        N'brown', N'warm fair', N'clear; the complexion of a healthy seventeen-year-old who has not been in enough trouble yet to show in his face',
        N'none',
        N'quick and careful; the posture of someone trained to be unobtrusive and who has not quite finished learning it',
        N'page livery; correct; he wears it well enough for his role',
        N'none',
        N'He carries messages throughout the household from the sixth hour to the twentieth. He is in every corridor, every wing, every room that requires a dispatched message. He attends the late afternoon music hour in the room where Klaus Brenner practices, which is not a coincidence and which both of them pretend is one in front of other people.',
        N'He returns Klaus Brenner''s feelings and has since they were both twelve years old. He knows what happens to pages who have relationships with the family''s youngest son. He has been carefully and very quietly working through the message routes he carries to plant suggestions in diplomatic correspondence — requests for a specific posting at a foreign estate that would put him far enough away that the relationship would be too distant to matter to anyone in House Fornax. He is seventeen. He has no authority to arrange anything. He has been doing it anyway, threading requests through the message system with a precision that has surprised even him, because the alternative is waiting for the political marriage to happen and he cannot wait for that.',
        N'The register of a seventeen-year-old who is trying to speak in the vocabulary of adults; the underlying register is warm and specific and not quite formed yet',
        N'Fast and slightly breathless; he is always between one thing and the next',
        N'Almost always managing how visible he is and to whom',
        N'Goes very correct and very formal; the page training takes over completely',
        N'Has one and it is the most important thing in his life and he has been protecting it for five years',
        N'Every corridor and room in the House Fornax estate; the message routes between them',
        0, 0,
        N'young Germanic man of seventeen, dark brown short hair, brown eyes, slim still-growing build, page livery, Rhine-Danube stone estate corridors, quick careful movement, the expression of someone trying to be as old as the situation requires, dark fantasy WW1-adjacent register',
        N'teenage boy in page livery, dark hair, stone corridor, quick careful posture, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Anton Wirth seeded.';
END
ELSE PRINT 'Anton Wirth already exists.';
GO

-- 50. MATHILDA STIEL — Page
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Mathilda Stiel')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Mathilda Stiel', N'mathilda-stiel', N'canon', 1,
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
        @id, N'Mathilda Stiel', N'mathilda-stiel', N'Mathilda', N'Stiel', N'',
        N'human', N'human', N'female', N'she/her', 15, N'alive',
        N'Page of House Fornax; youngest active page; has been in service for eighteen months',
        N'Mathilda Stiel is fifteen and has been a page for eighteen months and she has an extraordinary memory and no current purpose for it. She carries messages correctly and efficiently and gets where she is sent without being told twice. She is quiet in rooms — the specific quiet of someone who is listening, though she is not listening for anything in particular; she just finds people fascinating and wrong about things and she retains what she hears without trying to. She does not yet know what she will do with what she is accumulating. She is fifteen. She will know eventually.',
        N'The youngest character in the household and the one whose trajectory is the most open. What she is gathering now will matter when she is twenty-five. She is the building''s longest-range investment in consequence.',
        N'No POV assigned. Third-limited; her register is the register of a fifteen-year-old who is more attentive than she has learned to perform.',
        N'House Fornax; Rhine-Danube territories; page; eighteen months of service',
        158, 48, N'slim; small for her age; still becoming',
        N'light brown', N'braided; practical', N'medium when loose',
        N'grey-blue', N'fair', N'clear; she is fifteen and it shows',
        N'none',
        N'quick and quiet; she has learned already how to be in a room without registering',
        N'page livery; it is slightly large on her; she does not care',
        N'none',
        N'She carries messages. She is where she is sent. She is quiet in rooms. She eats with the other pages. She remembers everything she hears, in the specific detail of someone for whom remembering is not an effort.',
        N'She has been memorizing every conversation she overhears for eighteen months — not for any purpose she can currently name. She is fifteen and she does not know yet that she is assembling leverage. She just finds people fascinating and wrong about things and she retains what she hears because it is interesting and because she has the instinct that interesting things are worth keeping. She has heard about the Chancellor''s arrangement with Atrament (a partial conversation, the key word), about the Treasurer''s distress (inferred from his face and a fragment of conversation with the Seneschal), about the page Anton and young Lord Klaus (by accident and kept immediately and completely). She is fifteen. She does not know what she will do with all of this. She will know eventually, and by then she will have more.',
        N'The register of a fifteen-year-old who is more careful than her age accounts for; she sounds younger than she is listening and older than she is speaking',
        N'Careful and light; she does not draw attention to her sentences',
        N'Almost always attending to what is actually happening in the room',
        N'Goes very quiet; the lightest presence imaginable',
        N'Does not have one; she is fifteen and has not yet had occasion to be known',
        N'The House Fornax estate; every corridor she is sent down',
        0, 0,
        N'young girl of fifteen, light brown braided hair, grey-blue eyes, slim small build, page livery slightly too large, Rhine-Danube stone estate corridors, quick quiet movement, the expression of someone remembering what you just said, dark fantasy WW1-adjacent register',
        N'young girl, light brown braid, page livery too large, stone corridor, quiet attentive expression, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Mathilda Stiel seeded.';
END
ELSE PRINT 'Mathilda Stiel already exists.';
GO

-- 51. GRETA HOLT — Laundry Master; reads pockets
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Greta Holt')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Greta Holt', N'greta-holt', N'canon', 1,
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
        @id, N'Greta Holt', N'greta-holt', N'Greta', N'Holt', N'',
        N'human', N'human', N'female', N'she/her', 56, N'alive',
        N'Laundry Master of House Fornax; has managed the laundry for twenty-two years; the person everyone underestimates',
        N'Greta Holt has managed the House Fornax laundry for twenty-two years and in that time the laundry has processed every garment worn by every person of consequence in the building, and garments come with pockets, and pockets come with contents. She is not a spy. She did not start reading the pockets strategically. The first time she found a document folded into a coat lining she set it on the Seneschal''s desk without reading it. The second time she read it before setting it on the Seneschal''s desk. By the fifth time she had started keeping the ones that seemed important. That was twenty years ago. She has eighteen original documents under the floorboards of the laundry room.',
        N'The person everyone overlooks who is holding the most concentrated archive of unprocessed leverage in the building. The question of what she does with it is the one she has been sitting with for twenty years.',
        N'No POV assigned. Third-limited; her register is laundry-practical: warm, direct, the register of someone who has spent twenty-two years being underestimated and has decided to use this as a resource rather than resent it.',
        N'House Fornax; Rhine-Danube territories; laundry master; twenty-two years of service',
        160, 70, N'solid; the build of twenty-two years of laundry work; strong hands',
        N'grey; was brown', N'covered by a working cap; always', N'short',
        N'brown', N'warm medium-fair', N'warm and flushed; the laundry heat has been in her face for twenty years',
        N'none',
        N'practical and warm; she is always moving; the laundry does not stop',
        N'laundry working dress; practical; always damp at the edges',
        N'none',
        N'She manages the laundry from the fourth hour. She receives the soiled garments. She sorts them. She checks the pockets before washing — this is technically protocol, to prevent damage to documents. She reads what is in the pockets before returning the items. She files the cleaned garments. She visits the floorboard in her private room once a month to review what she has.',
        N'She has eighteen original documents she removed from pockets over twenty years — correspondence scraps, signed notes, one sealed letter she has never opened, and two items that she recognized immediately as significant: a signed authorization in Hartmut''s hand for an action she does not have context to fully understand but that reads as an order to remove a person, and a partial financial document in the Treasurer''s hand that predates the current Treasurer and that appears to document the falsification she suspects has something to do with the insolvency she has inferred from the Treasurer''s recent distress. She does not know who to give these to or what price to name. She knows she is safer holding them than she would be if anyone knew she had them. She has been holding them for twenty years. She is fifty-six years old and she is starting to think she is going to die with them under the floor.',
        N'Warm and practical; the vocabulary of laundry and household management; uses domestic language for everything',
        N'Fast and warm; the laundry moves at its own pace and so does she',
        N'Almost always reading what people left in their pockets — not the literal documents but the person behind the choice to carry them',
        N'Gets practical and faster; the warmth remains but the efficiency takes over',
        N'Warm with the laundry staff; the kind of warmth that comes from twenty-two years of shared work',
        N'The laundry room; the domestic staff quarters; the building''s garment routes',
        0, 0,
        N'Germanic woman in her mid-fifties, grey hair under a working cap, brown eyes, solid strong-handed build, damp laundry working dress, Rhine-Danube stone laundry room with steam, warm practical posture, the expression of someone who knows what was in your pocket before you did, dark fantasy WW1-adjacent register',
        N'older woman, grey hair, laundry working dress, steam-filled stone laundry room, warm expression, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Greta Holt seeded.';
END
ELSE PRINT 'Greta Holt already exists.';
GO

-- 52. EBERHARD KRONE — Head of Household Guards
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Eberhard Krone')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Eberhard Krone', N'eberhard-krone', N'canon', 1,
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
        @id, N'Eberhard Krone', N'eberhard-krone', N'Eberhard', N'Krone', N'',
        N'human', N'human', N'male', N'he/him', 46, N'alive',
        N'Head of Household Guards; not Myrmidons — commands the guards who protect the Lord and Lady''s private spaces',
        N'Eberhard Krone commands the household guards — not the Corps, but the smaller force responsible specifically for the Lord and Lady''s private rooms, the study corridors, and the estate''s inner perimeter. He is a solid and experienced security professional who came to the House fourteen years ago from a military background and has done his job correctly ever since except for a specific period in the past year during which he has done his job incorrectly twice, for payment, and has been ashamed of it ever since and continues to do it because he does not know how to stop.',
        N'The security commander who has been bribed into allowing access to the Lord''s study — twice. He does not know what was accessed or by whom. His shame is compounding and his situation is becoming acute.',
        N'No POV assigned. Third-limited; his register is security-practical, with an undercurrent of shame that reads as excessive formality.',
        N'House Fornax; Rhine-Danube territories; head of household guards; fourteen years of service',
        183, 90, N'solid; the build of a former military man who has been doing security work for fourteen years',
        N'dark grey; was black', N'close-cropped', N'very short',
        N'brown', N'medium fair weathered', N'marked; no longer quite field-fresh',
        N'none',
        N'correct and formal; the security professional''s posture; slightly too stiff since last year',
        N'household guard uniform; always complete; the correctness of his uniform has increased since the bribes',
        N'none',
        N'He manages the guard rotation schedules. He reviews the access logs for the private areas. He conducts the morning security briefing with his guard contingent. He reviews the overnight logs each morning. He has received two payments in the past year. He has spent both of them on things he will not remember in ten years and thought about the payments every day since.',
        N'He has accepted two bribes in the past year — payments to allow specific individuals access to Lord Hartmut''s study during hours when the study was officially unoccupied. Both times he accepted the payment and adjusted the guard rotation to create a window. He does not know who the individuals were — the contact was indirect both times. He does not know what they were looking for or whether they found it. He is ashamed in a sustained and corrosive way. He cannot report the bribes without accounting for his own role. He cannot refuse further contact without exposing what he has already done. He has been waiting for a third approach and dreading it and has spent the last three months almost hoping it comes because the waiting is worse than the act.',
        N'Security-formal; the vocabulary of guard protocols and access control; slightly more formal than his role requires since last year',
        N'Measured and correct; he does not volunteer information',
        N'Almost always assessing the security implications of what is being said or asked',
        N'Gets more formal; the shame is not visible in his voice but it is in the excessive correctness of his diction',
        N'Does not have one currently; the shame has displaced most of the available space',
        N'The Lord and Lady''s private quarters; the study corridors; the estate''s inner perimeter',
        0, 0,
        N'Germanic man in his mid-forties, close-cropped dark grey hair, brown eyes, solid former-military build, household guard uniform always completely correct, Rhine-Danube stone estate private corridors, formally correct posture that is slightly too stiff, the expression of someone managing sustained shame, dark fantasy WW1-adjacent register',
        N'security commander in his forties, dark grey hair, guard uniform, stone private corridor, formal stiff posture, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Eberhard Krone seeded.';
END
ELSE PRINT 'Eberhard Krone already exists.';
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- OATHLESS-ADJACENT (53–55)
-- ─────────────────────────────────────────────────────────────────────────────

-- 53. NIKOLAUS BRAND — Former House Fornax; now Oathless; still used by the House
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Nikolaus Brand')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Nikolaus Brand', N'nikolaus-brand', N'canon', 1,
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
        @id, N'Nikolaus Brand', N'nikolaus-brand', N'Nikolaus', N'Brand', N'',
        N'human', N'human', N'male', N'he/him', 49, N'alive',
        N'Former House Fornax intelligence officer; went Oathless eight years ago; still used by the House for operations they cannot officially authorize',
        N'Nikolaus Brand was a House Fornax intelligence officer for sixteen years before he went Oathless at forty-one. The Oathless status solved a specific operational problem: the House could use him for work that could not officially exist, and because he carried no House affiliation, the work carried no official liability. This was the Spymaster Marta Scholl''s idea and Brand agreed to it because the arrangement came with safety guarantees for his family. He operates in the gray zones between Houses, does the work that is sent to him, and has been finding it increasingly grotesque as the years accumulate, which is not a conclusion he was authorized to reach.',
        N'The instrument of officially deniable operations — whose own moral position has shifted while the operations have not. His growing reluctance is the most specific leverage the House has against him and the most acute threat to the operations he conducts.',
        N'No POV assigned. Third-limited; his register is former-intelligence-officer: precise, low-information, the register of someone who has spent twenty-four years saying less than he knows.',
        N'Formerly House Fornax; currently Oathless; operates in the border zones between Houses',
        178, 81, N'lean and maintained; the build of someone who has to be able to move quickly and quietly at forty-nine',
        N'grey', N'short and unremarkable', N'short',
        N'dark brown', N'weathered pale', N'marked; the gray-zone work is in his face',
        N'none',
        N'economical and watchful; the posture of someone who is always thinking about exits',
        N'plain working clothing appropriate to whatever context he is operating in; he has no consistent dress',
        N'none',
        N'He operates in the gray zones — the border markets, the unmapped spaces between House territories, the routes that the official diplomatic correspondence does not acknowledge. He receives assignments through a contact system he helped design. He completes the work. He does not ask what happens next.',
        N'He went Oathless deliberately so the House could use him without accountability, which was Marta Scholl''s idea and which he agreed to because it came with safety guarantees for his family. What he did not anticipate is that the work would become increasingly grotesque as the years passed — that the operations he was asked to conduct would escalate in their moral cost in ways he accepted in the early years and that he now finds difficult to complete. He cannot stop because stopping means the family guarantees end. He cannot expose the arrangement because exposing it means the same thing. He has been in a situation he cannot exit for eight years and the lack of exit has begun to reshape him in ways that are visible to the people who have known him a long time.',
        N'Former-intelligence-officer: precise, low-information; says only what is operationally necessary',
        N'Slow and controlled; he has spent twenty-four years controlling what his cadence reveals',
        N'Almost always assessing the exit conditions of whatever he is currently in',
        N'Does not change; the control is the same at all pressure levels because that is what he trained for',
        N'Was warm once in his marriage; his family is alive because of what he is doing and he cannot tell them what it costs',
        N'The gray zones; the border markets; the unmapped spaces between Houses',
        0, 0,
        N'Germanic man in his late forties, grey short hair, dark brown eyes, lean maintained build, plain working clothes that match his current context, Cauld gray-zone border territory, watchful economical posture always calculating exits, Buehlman dark fantasy register',
        N'lean man in his late forties, grey hair, plain clothes, border territory, watchful posture, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Nikolaus Brand seeded.';
END
ELSE PRINT 'Nikolaus Brand already exists.';
GO

-- 54. RENATA QUELL — Oathless; sheltering in Fornax territory
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Renata Quell')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Renata Quell', N'renata-quell', N'canon', 1,
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
        @id, N'Renata Quell', N'renata-quell', N'Renata', N'Quell', N'',
        N'human', N'human', N'female', N'she/her', 36, N'alive',
        N'Oathless; sheltering in House Fornax border territory for three years; the House knows and tolerates it; her reason is not the one she has given',
        N'Renata Quell came to House Fornax territory three years ago from House Pallor, under circumstances she has described as a general exhaustion with institutional affiliation and which are true as far as they go. She is Oathless. She lives in the border zone with the House''s quiet knowledge — not officially sanctioned, not officially expelled, which is the Oathless arrangement that is most stable for everyone involved. She is competent and self-sufficient and does not cause problems, which is why the House tolerates her. She has been in the border zone for three years and has not made contact with the person she came to be near.',
        N'The character whose stated reason for being somewhere is entirely true and entirely insufficient as an explanation. Her presence in Fornax territory is the most purely personal decision in the Oathless-adjacent section and the one with the least obvious consequence — unless the person she came to be near finds out.',
        N'No POV assigned. Third-limited; her register is composed and private, the register of someone who has decided on a position and is executing it with patience.',
        N'Formerly House Pallor; currently Oathless; resident in House Fornax border territory for three years',
        167, 62, N'practical; the build of someone who has been living in border territory and managing for herself',
        N'dark red-brown', N'practical; worn simply', N'medium',
        N'hazel', N'warm fair', N'clear; she takes care of herself',
        N'none',
        N'self-contained; the posture of someone who has made a decision and is waiting for the moment to act on it',
        N'practical border-territory clothing; nothing that marks her as belonging to any House',
        N'none',
        N'She manages her own provision in the border zone. She occasionally does work — labor, translation, minor logistics — for the border market operations. She does not approach the estate. She watches the patrol schedules from a distance that is careful enough not to be noticed.',
        N'She is in Fornax border territory because Commander Siegrid von Roth is here. They were in Pallor together for a period Renata has not described to anyone. The relationship ended when Siegrid''s assignment ended and Siegrid returned to Fornax. Renata went Oathless rather than remain in Pallor after that. She has been three years in the border zone and has not made contact. She is waiting for something she cannot name — a sign that contact is possible, or a decision to accept that it is not, or some third thing she has not arrived at yet. Siegrid does not know she is here. Renata is not sure Siegrid would want to.',
        N'Composed and private; gives information about herself in exactly the quantities that satisfy the question without opening further ones',
        N'Even and controlled; she has been managing this for three years and the management is in her cadence',
        N'Almost always deciding what she is prepared to reveal about why she is here',
        N'Does not change; the composure is structural rather than performed',
        N'Has one; it is the reason she is here; she has not acted on it in three years and may not',
        N'House Fornax border territory; the gray zone; the border market',
        0, 0,
        N'woman in her mid-thirties, dark red-brown hair worn practically, hazel eyes, practical build for border living, plain clothing without House markings, Rhine-Danube border zone landscape, self-contained posture, the expression of someone waiting for something she is not sure she wants to arrive, dark fantasy WW1-adjacent register',
        N'woman in her thirties, dark hair, plain clothes, border territory, composed waiting expression, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Renata Quell seeded.';
END
ELSE PRINT 'Renata Quell already exists.';
GO

-- 55. JOACHIM FELD — Oathless; border intelligence contractor; working for both Fornax and Calyx
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Joachim Feld')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Joachim Feld', N'joachim-feld', N'canon', 1,
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
        @id, N'Joachim Feld', N'joachim-feld', N'Joachim', N'Feld', N'',
        N'human', N'human', N'male', N'he/him', 43, N'alive',
        N'Oathless border intelligence contractor; used by House Fornax for border intelligence work; his origin is unclear and he does not clarify it',
        N'Joachim Feld operates in the gray zone between Houses as an intelligence contractor, which in practice means he gathers and sells information about border conditions, patrol patterns, and movement along the Rhine-Danube approaches. He has been doing this for twelve years. He is good at it. He is not affiliated with any House, which is the point of the Oathless arrangement from the hiring House''s perspective. House Fornax has been using him for four years. He has been professionally cooperative and accurate. He is also currently working for House Calyx, which House Fornax does not know, and he has been managing the compartmentalization between these two clients for four years with an increasing elaborateness that is beginning to require more of him than he has.',
        N'The contractor who has independently committed to two clients who would consider each other enemies, for the entirely human reason that he could not figure out how to say no to the second one after he had already said yes to the first. His situation is a comedy about agency until it stops being one.',
        N'No POV assigned. Third-limited; his register is contractor-precise: professional, low-warmth, the register of someone who has been treating information as a commodity for twelve years and has the specific emotional flatness that produces.',
        N'Oathless; origin unclear; border zone operator; twelve years of independent contracting',
        176, 79, N'medium; the build of someone who travels and works physically but not in one specific physical discipline',
        N'brown going grey', N'worn practically', N'short',
        N'brown', N'weathered medium', N'marked; the border zone is in his face',
        N'none',
        N'economical and watchful; the posture of someone who assesses new environments automatically',
        N'practical travel clothing appropriate to the border zone; nothing memorable',
        N'none',
        N'He moves along the Rhine-Danube border zone gathering information through the observation and contact networks he has built over twelve years. He delivers reports to House Fornax through the contact system the Spymaster established. He delivers different reports to House Calyx through a contact system he established himself when Calyx approached him four years ago. He spends considerable energy ensuring that the information he provides each party is accurate but compartmentalized in ways that prevent either from deducing the other''s knowledge.',
        N'He is simultaneously working for House Fornax and House Calyx — not as a deliberate double agent pursuing an ideological or financial goal but because House Calyx approached him two years after Fornax did, with a request that seemed separable from his Fornax work, and he accepted rather than explain the conflict, and now four years have passed and both arrangements are deeply embedded and he cannot exit either without explaining why, and explaining why would require admitting the other exists. He has been managing this through increasingly elaborate compartmentalization. He is forty-three and he has been telling himself for four years that he will find a way out next season.',
        N'Contractor-precise; low-warmth; the vocabulary of information as commodity',
        N'Even and professional; he delivers information in the format that was agreed upon',
        N'Almost always computing the compartmentalization requirements of whatever is being said',
        N'Gets more professional; the personal register disappears completely',
        N'Does not have one; twelve years of treating information as a commodity has made intimacy structurally difficult',
        N'The Rhine-Danube border zone; the gray spaces between House territories; the contact systems for both clients',
        0, 0,
        N'Germanic man in his early forties, brown hair going grey, brown eyes, medium weathered build, practical travel clothing without markings, Rhine-Danube border zone or gray-zone market, economical watchful posture, the expression of someone computing what can safely be said, dark fantasy WW1-adjacent register',
        N'man in his forties, grey-brown hair, travel clothes, border zone setting, watchful expression, dark fantasy aesthetic',
        0, 0
    );
    PRINT 'Joachim Feld seeded.';
END
ELSE PRINT 'Joachim Feld already exists.';
GO

PRINT 'House Fornax hierarchy seed complete. 55 characters seeded.';
