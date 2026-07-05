SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- House Pallor — 70 Characters
-- Island House. Three peoples: Anglic, Kellian, Morvic. Naval tradition. Channel defines everything.

-- 1. Lord Aldwyn Caer-Mael
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Aldwyn Caer-Mael')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Aldwyn Caer-Mael', N'aldwyn-caer-mael', N'canon', 1,
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
        @id, N'Aldwyn Caer-Mael', N'aldwyn-caer-mael', N'Aldwyn', N'Caer-Mael', N'Lord',
        N'human', N'human', N'male', N'he/him', 58, N'alive',
        N'Lord Commander of House Pallor''s military forces; Anglic political authority',
        N'Aldwyn Caer-Mael has commanded House Pallor''s combined forces through two Draught incursions and one internal crisis that never became a civil war, mostly because he moved faster than the grievance could organize. He is not a man who doubts his decisions, which is different from a man who has made good ones. He was present at the third channel breach and gave the order that held the line. What the order cost — what he actually did to hold that line — is known by the Kellian council members who lost their fishing settlement to his authorized flood. He has not apologized. He does not intend to.',
        N'Aldwyn is the cost of command rendered in a single man: the decisions that worked and the decisions that worked at the expense of the people who trusted him. He is neither villain nor hero, and the story will not resolve which.',
        N'No POV.',
        N'House Pallor; Anglic lowlands, island capital',
        182, 91, N'military-stocky',
        N'silver-grey', N'close-cropped', N'short',
        N'pale grey', N'fair', N'weathered, deeply lined',
        N'none',
        N'Upright and controlled; the stillness of a man who has learned to read rooms before entering them',
        N'Military dress, dark wool with silver House insignia; no ceremonial ornament',
        N'none',
        N'Morning: dispatches and council correspondence. Midday: inspection rounds or war-council. Evening: alone with maps he has largely memorized. He does not sleep well.',
        N'He ordered the flooding of the Kellian settlement at Brae Crossing during the third breach. He believed — correctly — that it was the only viable channel to hold. The Kellian council knows. He carries this not as guilt but as the specific weight of a correct decision made at someone else''s cost. What troubles him is that he would make the same choice again, and he is not sure what that means about him.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Island capital and channel fortification line; occasional Atrament diplomatic sessions',
        N'0', N'0',
        N'Stern late-middle-aged British lord in dark military wool, silver-grey close-cropped hair, weathered lined face, rigid upright posture, stone war-council chamber, medieval fantasy, Buehlman dark register --ar 2:3',
        N'A 58-year-old lord commander in dark wool, silver hair, deeply lined face, standing before a medieval stone map table',
        0, 0
    );
    PRINT 'Aldwyn Caer-Mael seeded.';
END
ELSE PRINT 'Aldwyn Caer-Mael already exists.';
GO

-- 2. Catriona Fenn
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Catriona Fenn')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Catriona Fenn', N'catriona-fenn', N'canon', 1,
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
        @id, N'Catriona Fenn', N'catriona-fenn', N'Catriona', N'Fenn', N'Dame',
        N'human', N'human', N'female', N'she/her', 44, N'alive',
        N'Knight; captain of the Scrying vessel Pallor''s Reach; House Pallor''s senior naval command',
        N'Catriona Fenn received her Knight''s infusion at thirty-one and has commanded the island''s primary Scrying vessel for nine years. She is the architect of Pallor''s mobile Scrying doctrine — the idea that a Scrying apparatus mounted on a naval platform gives the island an advantage no continental House can replicate. She is respected, trusted by her crew, and currently conducting unauthorized negotiations with Draught intermediaries through back-channel contacts she inherited from her dead handler. She is not sure anymore where the line between operational initiative and treason runs, and she has no one left to ask.',
        N'Catriona is the story of a loyal officer operating past the boundaries of her authorization — not because she turned, but because the institution that authorized her collapsed and she kept moving. She is the question of whether loyalty to a House and loyalty to the House''s actual interests are the same thing.',
        N'No POV.',
        N'House Pallor; Anglic coast, channel waters, naval command',
        172, 71, N'athletic',
        N'dark red', N'braided back', N'long',
        N'green', N'fair', N'weathered, sun-lined',
        N'Subtle height gain; increased skeletal density visible in jaw and brow; vascular prominence on forearms',
        N'Forward-weighted and ship-adapted; moves with the slight lateral adjustment of someone long accustomed to a moving deck',
        N'Naval uniform, dark wool; Dame''s badge at the throat; salt-stained at the cuffs',
        N'Knight — first infusion at thirty-one; moderate enhancement',
        N'Dawn: ship status and navigation review. Day: Scrying observation sessions or channel patrol. Evening: correspondence and the encrypted dispatches she writes to contacts no one in House Pallor knows she has.',
        N'Her intelligence handler died four months ago, taking the authorization for her Draught contact operation with him. She has continued the operation on her own judgment because she believes she is close to something useful. She has not told Lord Aldwyn because she cannot explain why she kept going without authorization without also explaining that she may have already committed an actionable offense. Her sister Tamsin is the only person who knows any of this.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Channel waters; Pallor naval installations; occasional unauthorized contact points on the northern coast',
        N'0', N'0',
        N'Scottish woman knight in dark naval wool, dark red braided hair, weathered fair face, athletic forward posture, medieval ship deck at dusk, Buehlman dark fantasy --ar 2:3',
        N'A 44-year-old woman knight in dark naval uniform, dark red braided hair, weathered face, on a medieval ship deck',
        0, 0
    );
    PRINT 'Catriona Fenn seeded.';
END
ELSE PRINT 'Catriona Fenn already exists.';
GO

-- 3. Dougal Strathmore
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Dougal Strathmore')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Dougal Strathmore', N'dougal-strathmore', N'canon', 1,
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
        @id, N'Dougal Strathmore', N'dougal-strathmore', N'Dougal', N'Strathmore', N'',
        N'human', N'human', N'male', N'he/him', 61, N'alive',
        N'Paladin; senior infantry commander; veteran of all three Draught channel incursions',
        N'Dougal Strathmore is what happens when a man survives more infusions than anyone expected him to. Four Catalyst administrations over twenty years, each one carrying the standard eighty percent mortality rate, and he emerged from all four. He is two hundred centimeters of enhanced bone and muscle who speaks very quietly. His soldiers find this more frightening than shouting. He is not a cruel commander, but he is a thorough one, and thoroughness in his profession has a specific smell. He is currently aware that the last batch of Catalysts administered at Pallor''s installation was compromised, and he said nothing because he needed them to shore up a depleted line.',
        N'Dougal is the institutional cost of Transmutation: a man who has survived the process so many times that he has become something the House uses rather than serves. He knows this. He has made a kind of peace with it.',
        N'No POV.',
        N'House Pallor; Anglic channel fortification line, northern garrison',
        196, 113, N'heavily built',
        N'iron grey', N'shorn close', N'short',
        N'pale blue', N'fair', N'scarred, deeply weathered',
        N'Evident enhancement — significant height, altered proportions, eyes changed; jaw and brow dramatically thickened; vascular prominence across neck and forearms',
        N'Economical and very slow for his size; carries his weight without apology; never startles',
        N'Field uniform reinforced at the shoulders; no House insignia beyond what regulation requires',
        N'Paladin — four infusions over twenty years; significant enhancement throughout',
        N'Drills in the morning while the garrison sleeps. Reviews defensive positions. Eats alone. In the evening he writes letters to the families of soldiers under his command who have died. He has been writing these letters for thirty years.',
        N'He knew the last Catalyst batch was degraded before it was administered. He had reports from the practitioner''s assistant. He chose not to stop the infusions because three Knights were going into a position he needed held, and compromised Catalysts still work more often than not. One of the three died on the table. He told no one about the batch reports. He has the reports in a locked case in his quarters. He does not know what he will do with them.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Channel fortification line; northern garrison; occasional capital war-council',
        N'0', N'0',
        N'Enormous enhanced Scottish warrior, iron-grey shorn hair, scarred fair face, dramatically thickened jaw and brow, heavy field uniform, stone garrison wall, medieval fantasy, Buehlman register --ar 2:3',
        N'A 61-year-old Paladin, enormous frame, iron-grey hair, scarred face, dramatically enhanced physique, standing in a stone garrison',
        0, 0
    );
    PRINT 'Dougal Strathmore seeded.';
END
ELSE PRINT 'Dougal Strathmore already exists.';
GO

-- 4. Morveth Pencarrow
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Morveth Pencarrow')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Morveth Pencarrow', N'morveth-pencarrow', N'canon', 1,
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
        @id, N'Morveth Pencarrow', N'morveth-pencarrow', N'Morveth', N'Pencarrow', N'Dame',
        N'human', N'human', N'female', N'she/her', 39, N'alive',
        N'Knight; channel watch officer; Morvic-born, commanding the coastal observation posts',
        N'Morveth Pencarrow earned her infusion at thirty-three after nine years of channel watch service, which is longer than most survive if they are watching the channel with any real attention. She is Morvic on both sides — the coastal people whose fishing grounds have been raided twice in living memory — and she carries this not as resentment but as a specific understanding of what the channel costs when the House is not watching. She is thorough, politically careful, and has been sending weather and patrol pattern reports to a Draught contact for eleven months. She does this to get her captured son back. She does not know he has been dead for a year.',
        N'Morveth is a study in how reasonable decisions compound into disaster: each step of her betrayal is understandable, and none of it will save who she is trying to save.',
        N'No POV.',
        N'House Pallor; Morvic coast, channel watch installations',
        170, 66, N'lean-athletic',
        N'dark brown', N'tied back', N'medium',
        N'grey-green', N'medium warm', N'weathered',
        N'Subtle height gain; increased skeletal density visible in jaw and brow; vascular prominence on forearms',
        N'Alert and scanning even indoors; the posture of someone who has spent years watching a horizon',
        N'Channel watch uniform, oiled wool against the coast weather; Dame''s badge worn inside the collar',
        N'Knight — first infusion at thirty-three; modest enhancement',
        N'Pre-dawn: observation post rotation inspection. Morning: watch reports and dispatch coding. Afternoon: coast patrols or defensive position review. Evening: encodes the reports she sends across the channel.',
        N'Her son Tomas was captured during the second Draught incursion three years ago, taken as a hostage of opportunity. A Draught intermediary contacted her six months after and offered his return in exchange for channel intelligence. She agreed. She has been providing patrol patterns and weather windows for eleven months. What she cannot know: Tomas died in Draught custody fourteen months ago, and the intermediary is simply using the promise of his return to maintain her cooperation. She keeps sending because she cannot afford to stop.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Morvic coast channel watch; observation posts along the western channel face',
        N'0', N'0',
        N'Lean athletic Celtic woman knight in oiled wool channel watch uniform, dark brown hair tied back, grey-green eyes, weathered face, coastal cliff observation post, sea wind, Buehlman medieval fantasy --ar 2:3',
        N'A 39-year-old woman knight in oiled wool uniform, dark brown hair, weathered face, watching the sea from a coastal cliff',
        0, 0
    );
    PRINT 'Morveth Pencarrow seeded.';
END
ELSE PRINT 'Morveth Pencarrow already exists.';
GO

-- 5. Cormac Trevise
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Cormac Trevise')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Cormac Trevise', N'cormac-trevise', N'canon', 1,
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
        @id, N'Cormac Trevise', N'cormac-trevise', N'Cormac', N'Trevise', N'',
        N'human', N'human', N'male', N'he/him', 52, N'alive',
        N'Warden of Intelligence; coordinator of House Pallor''s spy network on the continent and island',
        N'Cormac Trevise has run House Pallor''s intelligence apparatus for eleven years. He is not the kind of man who appears in histories, which is exactly correct. He has twelve active operatives on the continent, four in Atrament, two in Draught, and one whom he suspects has been turned and is feeding him carefully managed information. He keeps the suspected turned operative active because the managed information is still useful when you know to invert it. He is very good at his work and this has made him a man who holds simultaneously contradictory truths without needing to resolve them, which is not a comfortable way to live.',
        N'Cormac is the story of institutional knowledge that cannot be shared — the man who knows too much to trust anyone with the whole picture, including himself.',
        N'No POV.',
        N'House Pallor; island capital, intelligence annex; occasional continental travel under cover',
        175, 78, N'average',
        N'brown going grey', N'unremarkable, side-parted', N'short',
        N'brown', N'fair-medium', N'clear, forgettable',
        N'none',
        N'Deliberately unmemorable in his bearing; nothing about his posture announces his function',
        N'Civilian dress, well-made but plain; nothing that marks House affiliation at a distance',
        N'none',
        N'Reads dispatch reports before anyone else is awake. Holds three brief meetings per day, never in the same location twice in a week. Writes summaries that omit the most important conclusions. Burns those conclusions.',
        N'He has three independent evidence sets pointing to three different people as the killer of the last Atrament ambassador. All three evidence sets are internally consistent. All three may be accurate — the death may have had three hands. He has buried all three because acting on any one of them would either destroy Pallor''s most important trade alliance or trigger an internal purge that would hollow out the intelligence apparatus he has spent eleven years building. He reviews the evidence every six months and then locks it away again.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Island capital; intelligence safe-houses across Pallor territory; occasional continental contact',
        N'0', N'0',
        N'Unremarkable middle-aged Irish-Celtic man in plain civilian dress, brown-grey hair, forgettable face, deliberately neutral bearing, candlelit intelligence office, medieval fantasy, Buehlman dark register --ar 2:3',
        N'A 52-year-old intelligence chief in plain civilian dress, unremarkable appearance, sitting at a desk in a medieval stone office',
        0, 0
    );
    PRINT 'Cormac Trevise seeded.';
END
ELSE PRINT 'Cormac Trevise already exists.';
GO

-- 6. Aldric Holt
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Aldric Holt')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Aldric Holt', N'aldric-holt', N'canon', 1,
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
        @id, N'Aldric Holt', N'aldric-holt', N'Aldric', N'Holt', N'',
        N'human', N'human', N'male', N'he/him', 19, N'alive',
        N'Myrmidon conscript; channel watch garrison, first posting',
        N'Aldric Holt is in his first year of garrison service, assigned to the channel watch rotation at one of the northern signal posts. He is an Anglic farmsteader''s son who joined because the alternative was another season of not enough grain to sell. He is not a coward. He is also not yet sure what he is, which is the condition of being nineteen and responsible for watching for ships in the dark. His older sister Sian works in the garrison armory and occasionally brings him food he does not ask for, which he receives without comment.',
        N'Aldric is the ground-level face of what the House''s military commitment costs in human terms: not glory, not cause, but a scared young man watching a dark channel because someone has to.',
        N'No POV.',
        N'House Pallor; northern channel watch, Anglic garrison',
        173, 68, N'lean',
        N'light brown', N'close-cropped', N'short',
        N'brown', N'fair', N'clear, young',
        N'none',
        N'Alert in the specific tense way of someone trying very hard not to look afraid',
        N'Standard garrison wool; fits poorly at the shoulders, which he has not reported',
        N'none',
        N'Four-hour watch rotations at the signal post. Eats in the garrison hall with the other conscripts. Writes letters home that he revises three times before sending. Sleeps badly.',
        N'During a Draught probe two months ago, he left his post for two hours — walked to the base of the cliff and sat in the dark because he could not make himself watch the channel one more minute. He returned before anyone noticed. The probe turned east and never came within signal range. He wrote a letter to his mother confessing this and has not sent it. It is in his kit under everything else. He does not know why he keeps it.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Northern channel watch; Anglic garrison signal posts',
        N'0', N'0',
        N'Young English-looking conscript soldier in ill-fitting garrison wool, light brown hair, scared careful face, stone signal post on a coastal cliff at night, medieval fantasy --ar 2:3',
        N'A 19-year-old conscript soldier in garrison wool, light brown hair, watching the sea from a stone coastal signal post at night',
        0, 0
    );
    PRINT 'Aldric Holt seeded.';
END
ELSE PRINT 'Aldric Holt already exists.';
GO

-- 7. Seren Calloway
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Seren Calloway')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Seren Calloway', N'seren-calloway', N'canon', 1,
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
        @id, N'Seren Calloway', N'seren-calloway', N'Seren', N'Calloway', N'',
        N'human', N'human', N'female', N'she/her', 22, N'alive',
        N'Myrmidon; second-year soldier, Kellian scouting unit',
        N'Seren Calloway is Kellian-born and joined service at twenty after her village''s scout quota was reassigned following the second channel breach. She survived the Draught probe at the Brae Crossing engagement, where her entire eight-person scouting unit was killed or disabled in the first exchange. She is the only one who walked out. She has filed a report, answered a debrief, and received a formal commendation. She does not feel commended. She is now assigned to the same unit designation reconstituted with seven strangers, and she has not told any of them what happened.',
        N'Seren is the survivor''s problem in active form: she is still in the institution that nearly killed her, surrounded by people who need to trust her account of events she has never told accurately.',
        N'No POV.',
        N'House Pallor; Kellian highlands, channel eastern scouting range',
        162, 55, N'athletic-lean',
        N'black', N'braided tight', N'medium',
        N'dark brown', N'warm brown', N'weathered',
        N'none',
        N'Economical and watchful; conserves movement; the specific stillness of someone who has been very still under very bad circumstances',
        N'Scout field kit, practical and worn in; Kellian-pattern work at the collar seam',
        N'none',
        N'Pre-dawn drills. Scouting rotations in pairs she finds unremarkable because she is the most experienced person in them by two full years. Reports. Eats. Does not drink with the unit. Sleeps lightly.',
        N'She survived the Brae Crossing by hiding under a dead horse for four hours. She was not injured, not pinned — she chose to hide, and she made the correct choice, and everyone else died anyway. She has told this story three different ways in three different debriefs. The version in the official record is the one that makes her sound like she was separated from the unit by terrain. None of the versions are the accurate one. She does not know what the accurate version would cost her.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Kellian channel scouting range; eastern approach terrain',
        N'0', N'0',
        N'Young Welsh-looking woman soldier in worn scout kit, black hair braided tight, watchful dark eyes, economical still posture, forested highland terrain, medieval fantasy, Buehlman register --ar 2:3',
        N'A 22-year-old woman scout in worn field kit, black braided hair, watchful expression, standing in forested highland terrain',
        0, 0
    );
    PRINT 'Seren Calloway seeded.';
END
ELSE PRINT 'Seren Calloway already exists.';
GO

-- 8. Bram Pethrick
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Bram Pethrick')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Bram Pethrick', N'bram-pethrick', N'canon', 1,
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
        @id, N'Bram Pethrick', N'bram-pethrick', N'Bram', N'Pethrick', N'',
        N'human', N'human', N'male', N'he/him', 24, N'alive',
        N'Myrmidon; gate garrison, harbor town post',
        N'Bram Pethrick is three years into his garrison posting and has a reputation for reliability that he has worked hard to cultivate because reliability is the best cover he knows. He is Anglic-born from the harbor district, the son of a fishmonger who has not been seen in the harbor for two years because his father is Oathless, living in the hill country north of the town. Bram brings him food once a month, on the nights when the garrison rotates and his absence is least likely to be counted. He has never told anyone. He would be executed if this were discovered — not for the food, but for the ongoing concealment.',
        N'Bram is the ordinary human cost of the House''s Oathless policy: a young soldier caught between the institution he serves and the father he will not abandon.',
        N'No POV.',
        N'House Pallor; harbor town gate garrison, northern hill country visits',
        176, 80, N'stocky',
        N'dark blond', N'plain, short-back', N'short',
        N'hazel', N'fair-medium', N'clear',
        N'none',
        N'Solid and reliable-looking; the deliberate ordinariness of someone with something to hide',
        N'Standard garrison wool, maintained carefully; nothing that stands out',
        N'none',
        N'Gate rotations; equipment maintenance; spends free hours playing cards with the other garrison soldiers because being present and visible is the best alibi. On monthly rotation nights, walks north for three hours and back.',
        N'His father Torvin was cast as Oathless four years ago for refusing to enforce a grain requisition order against Kellian farmsteaders he''d known for twenty years. Bram believes this makes his father right, not criminal. He has carried this belief and his monthly visits for two years without speaking of it to anyone. He knows that if his father were found, he himself would face execution as a knowing concealer. He has not decided whether this would change anything he does.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Harbor town gate garrison; northern hill country on monthly supply visits',
        N'0', N'0',
        N'Stocky young English-looking garrison soldier in maintained wool uniform, dark blond hair, reliable plain expression, harbor town stone gate, medieval fantasy --ar 2:3',
        N'A 24-year-old garrison soldier, stocky build, dark blond hair, plain trustworthy face, standing at a medieval harbor town gate',
        0, 0
    );
    PRINT 'Bram Pethrick seeded.';
END
ELSE PRINT 'Bram Pethrick already exists.';
GO

-- 9. Neve Wickham
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Neve Wickham')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Neve Wickham', N'neve-wickham', N'canon', 1,
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
        @id, N'Neve Wickham', N'neve-wickham', N'Neve', N'Wickham', N'',
        N'human', N'human', N'female', N'she/her', 21, N'alive',
        N'Myrmidon; channel signal tower lookout, southern post',
        N'Neve Wickham has been stationed at the southern channel signal tower for eighteen months and is considered one of the most reliable lookouts in the rotation — she has an exceptional eye and files accurate reports with minimal embellishment. She is also the only person who knows what she saw three months ago: a ship running dark, Draught hull profile, turning east before the watch line. She reported it as heavy fog obscuring the horizon. She does not know why she lied. She has been thinking about this ever since.',
        N'Neve is the story of a small betrayal with no apparent motive — which is the most frightening kind, because it suggests the person committing it may be the last one to understand their own reasons.',
        N'No POV.',
        N'House Pallor; southern channel watch, Anglic coast signal tower',
        160, 54, N'lean',
        N'auburn', N'tied back simply', N'medium',
        N'light grey', N'fair', N'wind-chapped',
        N'none',
        N'Very still on watch; moves with the specific efficiency of someone who has learned not to waste warmth in cold wind',
        N'Channel watch wool, heavily oiled; scarf wound at the throat; practical boots',
        N'none',
        N'Long watch rotations at the signal tower. Records observations in the station log with careful precision. Eats alone at the tower because she prefers the view to the noise of the garrison hall. Reports to her watch commander every four hours.',
        N'Three months ago she saw a ship running dark: no lights, no banners, Draught hull shape, bearing east and turning before it reached the alarm threshold. She filed her report as fog. She did not tell anyone. She has reviewed her decision many times since and cannot identify a reason for it. She was not threatened. She was not paid. She watched a ship that should have triggered a signal response, and she wrote down that she could not see clearly. She keeps waiting for the consequence of this and it has not come.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Southern channel signal tower; Anglic coast watch rotation',
        N'0', N'0',
        N'Young English-looking woman lookout in oiled wool channel watch gear, auburn hair tied back, light grey eyes, wind-chapped fair face, stone signal tower at sea, medieval fantasy --ar 2:3',
        N'A 21-year-old woman lookout in heavy wool, auburn hair, watching the channel from a stone tower in coastal medieval fantasy setting',
        0, 0
    );
    PRINT 'Neve Wickham seeded.';
END
ELSE PRINT 'Neve Wickham already exists.';
GO

-- 10. Tristan Mere
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Tristan Mere')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Tristan Mere', N'tristan-mere', N'canon', 1,
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
        @id, N'Tristan Mere', N'tristan-mere', N'Tristan', N'Mere', N'',
        N'human', N'human', N'male', N'he/him', 27, N'alive',
        N'Myrmidon; channel patrol boat crew, boarding party specialist',
        N'Tristan Mere is Anglic with an Atrament grandfather, which the garrison records note and which he does not think about very often. He has been on channel patrol for four years and is known for being calm in boarding actions, which is the kind of reputation that keeps getting you put in boarding parties. He is currently maintaining a correspondence with an Atrament soldier he met during a prisoner exchange two years ago, routing his letters through a merchant contact he pays from his garrison wages. The correspondence started as curiosity about the enemy and has become something he does not have a word for, in the Cauld''s language or any other.',
        N'Tristan is a quiet argument against the Living War rendered through the specific act of two soldiers who cannot stop writing to each other.',
        N'No POV.',
        N'House Pallor; channel patrol, Anglic harbor district',
        178, 75, N'lean-athletic',
        N'light brown', N'plain, unstyled', N'short',
        N'blue', N'fair-medium', N'clear, lightly weathered',
        N'none',
        N'Easy and unhurried when not working; moves with a competence that does not announce itself',
        N'Patrol crew gear, functional and salt-stained; nothing that marks him as particularly distinguished',
        N'none',
        N'Patrol rotations on the channel. Maintenance work on the boat. Writes letters in a shorthand he developed himself, in the evenings, in the cargo hold where no one is likely to read over his shoulder. Routes them through the merchant Tessaly Fen.',
        N'He has been corresponding with an Atrament patrol soldier named Bertoul for twenty-three months. The correspondence began when they were assigned to supervise a prisoner exchange and had three hours with nothing to do but talk across a table. Bertoul speaks some Pallor dialect. Tristan speaks some continental. They wrote down their names before leaving. Tristan sent a letter on impulse. Bertoul wrote back. He does not know what to call what this has become. He knows what the House would call it.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Channel patrol route; Anglic harbor district; letters routed via continental merchant contacts',
        N'0', N'0',
        N'Young English-looking patrol soldier in salt-stained crew gear, light brown hair, calm blue eyes, medieval harbor and channel waters, fantasy --ar 2:3',
        N'A 27-year-old patrol boat soldier in salt-stained gear, light brown hair, calm expression, on a medieval channel patrol vessel',
        0, 0
    );
    PRINT 'Tristan Mere seeded.';
END
ELSE PRINT 'Tristan Mere already exists.';
GO

-- 11. Gwenith Arlow
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Gwenith Arlow')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Gwenith Arlow', N'gwenith-arlow', N'canon', 1,
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
        @id, N'Gwenith Arlow', N'gwenith-arlow', N'Gwenith', N'Arlow', N'',
        N'human', N'human', N'female', N'she/her', 35, N'alive',
        N'Senior ship-mounted Scrying specialist; lead operator aboard Pallor''s Reach',
        N'Gwenith Arlow has been running Scrying observations from a moving ship for eight years, which is unusual enough that the Liturgy has studied her technique without quite understanding what she does differently from land-based operators. The short answer is that she has found she can perceive Spheres that standard operators cannot reach — what she experiences as a lower frequency of the membrane, accessible when the installation is in motion over water. She has not reported this to the Liturgy. The Spheres she reaches in this way are not in the catalogue, and she has been observing them privately for three years.',
        N'Gwenith holds a secret that sits at the intersection of the personal and the catastrophic: her unlicensed observations may be the most important Scrying data in Pallor''s history, or they may be a delusion she has been constructing alone. She does not know which.',
        N'No POV.',
        N'House Pallor; channel waters, Pallor''s Reach naval Scrying installation',
        164, 60, N'average',
        N'blonde going dark', N'loose, tied at the nape', N'long',
        N'grey-blue', N'fair', N'clear, lightly freckled',
        N'none',
        N'Precise and internally focused during observation sessions; otherwise easy and collegial; the gap between these two modes is pronounced',
        N'Practical ship clothing; operator''s tabard over wool; no adornment',
        N'none',
        N'Morning calibration checks on the Scrying apparatus. Observation sessions when the ship is underway. Filing reports that accurately describe what she observed in the licensed Spheres. After the crew is settled, private sessions in the observation chamber she does not log.',
        N'She has been observing an unlicensed Sphere for three years in which the island of Pallor does not exist — the channel is there, but the island is open water. She does not know if this Sphere represents a historical past, an alternate present, or something she does not have a framework for. She has been drawing what she sees in a private journal. She has not told the Liturgy because unlicensed Sphere observation is a capital offense, and because she is not sure the Liturgy would not simply seal what she''s found and pretend it doesn''t exist.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Channel waters aboard Pallor''s Reach; Pallor naval Scrying installation port',
        N'0', N'0',
        N'Welsh-looking woman Scrying operator in practical ship clothing, blonde-brown hair at nape, grey-blue eyes, concentrated expression, ship-mounted Scrying apparatus chamber, medieval fantasy --ar 2:3',
        N'A 35-year-old woman Scrying operator in practical ship clothing, blonde hair, focused expression, at a medieval ship-mounted Scrying apparatus',
        0, 0
    );
    PRINT 'Gwenith Arlow seeded.';
END
ELSE PRINT 'Gwenith Arlow already exists.';
GO

-- 12. Osian Brackley
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Osian Brackley')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Osian Brackley', N'osian-brackley', N'canon', 1,
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
        @id, N'Osian Brackley', N'osian-brackley', N'Osian', N'Brackley', N'',
        N'human', N'human', N'male', N'he/him', 48, N'alive',
        N'Senior Scrying operator; the island''s oldest active installation, northern coast',
        N'Osian Brackley has operated the northern Scrying installation for twenty-two years. He is a Kellian man who chose a technical posting over military service when the option was available and has never regretted this, except in the specific way of a person who has seen too many Spheres and is not sure anymore which world is the one he lives in. Eight years ago he stopped filing his observation records with the Liturgy. He still looks. He still records. His private journal, buried under the flagstone in his quarters, now contains twenty-two years of Sphere observations that the Liturgy believes stopped eight years ago.',
        N'Osian is the question of what an individual owes to an institution that would weaponize his knowledge: a man who has simply decided to keep the Cauld''s most comprehensive Sphere record to himself, and who has not yet decided what it''s for.',
        N'No POV.',
        N'House Pallor; northern coast Scrying installation, Kellian territory',
        177, 82, N'average',
        N'brown going white', N'unkempt', N'medium',
        N'brown', N'warm medium', N'clear, indoor-pale',
        N'none',
        N'Slow and deliberate; the posture of someone whose most important work is done sitting completely still',
        N'Operator''s working clothes, stained with the chalk and lamp-oil of long sessions; nothing ceremonial',
        N'none',
        N'Morning calibration of the installation equipment. Observation session mid-morning. Files a routine, uninformative report with the Liturgy liaison. Private observation session in the afternoon with the journal open. Supper alone. Records his day''s private findings before sleep.',
        N'His private journal contains observations of forty-one Spheres not in the Liturgy catalogue. Three of them show technological and social developments so far beyond the Cauld that he has no context for what he is seeing. One of those Spheres has been consistently accessible for six years and appears to show the same people — a community — going about their lives. He has named some of them. He has never told a soul. The journal contains enough to destabilize the Liturgy''s entire Sphere-catalogue authority, and he knows this, which is part of why he cannot burn it.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Northern coast Scrying installation; rarely leaves the post',
        N'0', N'0',
        N'Middle-aged Welsh-looking man in chalk-stained operator clothes, brown-white unkempt hair, indoor-pale face, slow deliberate bearing, stone Scrying chamber with apparatus, medieval fantasy --ar 2:3',
        N'A 48-year-old Scrying operator in stained working clothes, unkempt brown-white hair, sitting at a medieval stone Scrying apparatus',
        0, 0
    );
    PRINT 'Osian Brackley seeded.';
END
ELSE PRINT 'Osian Brackley already exists.';
GO

-- 13. Llinos Morvan
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Llinos Morvan')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Llinos Morvan', N'llinos-morvan', N'canon', 1,
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
        @id, N'Llinos Morvan', N'llinos-morvan', N'Llinos', N'Morvan', N'',
        N'human', N'human', N'female', N'she/her', 31, N'alive',
        N'Scrying operator; weapons research division, southern installation',
        N'Llinos Morvan is a Kellian Scrying operator who was assigned to the weapons research division two years ago for her precision and speed in capturing design schematics from Spheres. She is very good at her work and this has cost her significantly. Fourteen months ago she accessed a Sphere outside her authorized catalogue — Sphere 14, restricted, weapons-grade — and copied incendiary device schematics that were then built and tested. The device misfired during testing. Forty-three soldiers and engineers died. The incident report attributes the failure to manufacturing defect. Only Llinos and one engineer know the design came from an unauthorized Sphere. The engineer has said nothing. Llinos has said nothing.',
        N'Llinos is a study in compounding silence: each day she does not confess is a day that makes confession harder, and she is aware that she is building a structure of concealment on a foundation of dead people.',
        N'No POV.',
        N'House Pallor; southern Scrying installation, weapons research division',
        163, 58, N'lean',
        N'dark brown', N'pinned up', N'medium',
        N'brown', N'medium warm', N'clear, slightly hollowed',
        N'none',
        N'Precise and controlled; the particular stillness of someone suppressing constant anxiety',
        N'Operator''s working clothes; nothing that marks weapons division specifically',
        N'none',
        N'Authorized observation sessions in the morning. Writes technically accurate but carefully limited reports. Eats without tasting much. Her cousin Dame Isolde Morvan has confronted her three times and been refused. Sleeps badly.',
        N'She accessed Sphere 14 outside authorized hours because the standard Sphere catalogue didn''t have what she needed and she was impatient. The incendiary design she found and copied looked simpler than what Pallor''s engineers were attempting. She believed she was helping. Forty-three people died in the test detonation. She has told herself that the manufacturing failure may have contributed and that Sphere 14 designs are not always exactly replicable and that she cannot be certain. She knows she is lying to herself. Her cousin wants her to confess. She cannot.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Southern Scrying installation; weapons research division; rarely leaves the post',
        N'0', N'0',
        N'Welsh-looking young woman operator in working clothes, dark brown pinned hair, controlled expression hiding anxiety, stone Scrying chamber, medieval weapons research setting, Buehlman dark fantasy --ar 2:3',
        N'A 31-year-old woman Scrying operator in working clothes, dark brown hair, controlled tense expression, medieval stone chamber',
        0, 0
    );
    PRINT 'Llinos Morvan seeded.';
END
ELSE PRINT 'Llinos Morvan already exists.';
GO

-- 14. Edric Faul
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Edric Faul')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Edric Faul', N'edric-faul', N'canon', 1,
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
        @id, N'Edric Faul', N'edric-faul', N'Edric', N'Faul', N'',
        N'human', N'human', N'male', N'he/him', 54, N'alive',
        N'Veteran Scrying operator; Liturgy technical liaison for the Pallor installation network',
        N'Edric Faul is the Liturgy''s designated liaison for House Pallor''s Scrying installations, which means he is the man who signs off on what gets catalogued and what gets reported upward. He has been in this position for sixteen years and has used it the way a careful man uses any position of institutional trust: to do the authorized work precisely, and to do the unauthorized work invisibly. He has been cataloguing Spheres that do not appear in any Liturgy record, and eight months ago he found something he should have reported immediately: a Sphere where the membrane is thin enough that a physical object, correctly weighted and precisely positioned, might cross it.',
        N'Edric is a man at the exact moment before a catastrophic decision, building toward something he has not articulated even to himself. He is the story of what happens when an operator who knows too much finally sees a door.',
        N'No POV.',
        N'House Pallor; island Scrying installation network; Liturgy technical liaison office',
        179, 84, N'average',
        N'grey-white', N'trimmed short', N'short',
        N'pale blue', N'fair', N'weathered, indoor-yellowed',
        N'none',
        N'Meticulous and unhurried; moves through installations with proprietary ease',
        N'Liturgy technical liaison dress, dark grey; functional and authority-marking',
        N'none',
        N'Inspects installations on a six-week rotation. Files Liturgy reports that are technically accurate and strategically incomplete. Spends the hours he does not report on his private catalogue. Has begun acquiring materials — specific weights of metal, specific dimensions — without recording their purpose.',
        N'He has found a Sphere where the membrane''s structural integrity is measurably lower than any other catalogued site. He has calculated, using principles the Liturgy taught him, that an object of specific weight and geometry, positioned correctly during a membrane event, would not be reflected. It would pass through. He does not know what would happen to it on the other side. He has begun building the object. He has not told anyone. He does not know what he intends to do with this knowledge, but the object is now two weeks from completion.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Pallor Scrying installation network; Liturgy liaison office; private workshop',
        N'0', N'0',
        N'Middle-aged English-looking man in dark grey Liturgy technical dress, grey-white trimmed hair, pale blue eyes, meticulous bearing, stone Scrying installation, medieval fantasy --ar 2:3',
        N'A 54-year-old Liturgy technical liaison in dark grey dress, grey-white hair, methodical expression, at a medieval Scrying installation',
        0, 0
    );
    PRINT 'Edric Faul seeded.';
END
ELSE PRINT 'Edric Faul already exists.';
GO

-- 15. Rhoswyn Teal
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Rhoswyn Teal')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Rhoswyn Teal', N'rhoswyn-teal', N'canon', 1,
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
        @id, N'Rhoswyn Teal', N'rhoswyn-teal', N'Rhoswyn', N'Teal', N'',
        N'human', N'human', N'female', N'she/her', 29, N'alive',
        N'Scrying operator; newly transferred to ship-based operations aboard Pallor''s Reach',
        N'Rhoswyn Teal is Morvic-born and transferred to Pallor''s Reach six months ago after three years at a land installation, where she was considered talented enough to warrant the unusual posting. Ship-based Scrying is harder — the apparatus calibration shifts with every swell — but she has adapted faster than her supervisor expected. The problem she has not reported is that during her second week aboard, she found a Sphere in which she can see herself. Not a lookalike. Herself: same face, same scar on the left hand, doing work she does not recognize in a room she has never been in. And the double knows she is being watched.',
        N'Rhoswyn is a character sitting at the membrane''s most disquieting implication: that the observation goes both ways, and that whatever is on the other side of the glass has noticed you.',
        N'No POV.',
        N'House Pallor; Pallor''s Reach, channel waters; Morvic coast origin',
        161, 57, N'lean',
        N'black', N'short and practical', N'short',
        N'dark brown', N'medium warm', N'clear',
        N'none',
        N'Careful and attentive; moves with controlled deliberateness aboard ship; tends to face windows',
        N'Ship operator''s tabard over layered wool; practical and unadorned',
        N'none',
        N'Morning calibration. Authorized observation sessions. Files accurate reports. In the afternoons when the ship is at anchor and the apparatus is available, she returns to the Sphere she has not reported and tries to understand what the double is doing. The double has started leaving things near the membrane site — objects she cannot identify from this side.',
        N'During an unsanctioned observation session, she found a Sphere where her exact double exists — same face, identical scar on the left hand from a childhood accident. The double has noticed her and has started communicating: leaving written materials near the membrane site in that Sphere, in a script Rhoswyn cannot read. She does not know whether this Sphere is a genuine parallel world, a reflection, or something the Liturgy would classify as a contaminated observation. She has not told anyone because she cannot explain it without admitting to an unauthorized session.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Pallor''s Reach, channel waters; Morvic coast background',
        N'0', N'0',
        N'Young Welsh/Cornish-looking woman operator in ship tabard, short black hair, dark eyes, careful deliberate expression, medieval ship Scrying chamber, fantastical membrane visible, Buehlman dark register --ar 2:3',
        N'A 29-year-old woman Scrying operator, short black hair, intent expression, sitting at a medieval ship Scrying apparatus',
        0, 0
    );
    PRINT 'Rhoswyn Teal seeded.';
END
ELSE PRINT 'Rhoswyn Teal already exists.';
GO

-- 16. Penvran Cosse
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Penvran Cosse')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Penvran Cosse', N'penvran-cosse', N'canon', 1,
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
        @id, N'Penvran Cosse', N'penvran-cosse', N'Penvran', N'Cosse', N'',
        N'human', N'human', N'male', N'he/him', 42, N'alive',
        N'Scrying operator; Sphere 31 extraction liaison, coastal installation',
        N'Penvran Cosse is the Scrying operator who handles the Sphere 31 observation window for House Pallor — assisting Liturgy extraction teams by providing approach data, membrane timing, and extraction site coordinates. He has been doing this for eleven years. He is Morvic, quiet, technically precise, and two years ago he developed feelings for a woman from Sphere 31 he met during a pre-extraction interview. He altered her manifest entry to mark her as ineligible. A different Liturgy officer reviewed the case three months later and reversed the decision. She was taken. He has been searching House records for her location for twenty-one months.',
        N'Penvran is the institutional machinery of Sphere 31 extraction made personal: a man who worked inside the system long enough to try to protect one person from it, and who discovered that the system is specifically designed to be larger than individual sabotage.',
        N'No POV.',
        N'House Pallor; coastal Scrying installation, Sphere 31 extraction coordination',
        174, 76, N'average',
        N'dark brown', N'close-cropped', N'short',
        N'grey', N'medium warm', N'weathered',
        N'none',
        N'Controlled and methodical during work; slightly distant socially, the specific quality of someone conducting a private search',
        N'Operator''s working clothes; no Liturgy insignia because he is House-employed, not Liturgy-staff',
        N'none',
        N'Morning extraction coordination preparation. Observation sessions during extraction windows. Files standard liaison reports. In the evenings he combs through House Pallor''s domestic assignment records, looking for the woman he failed to protect under the name she might have been given on intake.',
        N'He met Saoirse of Sphere 31 during a standard pre-extraction assessment interview. She was thirty-one, a weaver, brought to the interview site by standard Liturgy process. He found her intelligent and calm in circumstances that usually produce panic, and he found himself coming back to the interview room three times more than protocol required. He altered her eligibility notation. Three months later a Liturgy supervisor reviewed the batch and reversed it. She was extracted. He has been searching for her for twenty-one months. He does not know if she is alive.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Coastal Scrying installation; Sphere 31 extraction coordination; House domestic records access',
        N'0', N'0',
        N'Middle-aged Cornish-looking man operator in working clothes, dark brown close-cropped hair, grey eyes, methodical distant expression, coastal stone Scrying installation, medieval fantasy --ar 2:3',
        N'A 42-year-old Scrying operator in working clothes, dark brown hair, distant preoccupied expression, medieval coastal stone installation',
        0, 0
    );
    PRINT 'Penvran Cosse seeded.';
END
ELSE PRINT 'Penvran Cosse already exists.';
GO

-- 17. Aelwyn Croft
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Aelwyn Croft')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Aelwyn Croft', N'aelwyn-croft', N'canon', 1,
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
        @id, N'Aelwyn Croft', N'aelwyn-croft', N'Aelwyn', N'Croft', N'Mistress',
        N'human', N'human', N'female', N'she/her', 50, N'alive',
        N'House Pallor''s primary Transmutation practitioner; administers all Knight-class infusions',
        N'Aelwyn Croft has been administering Catalyst infusions for twenty-three years. She has worked on seventy-two patients. Forty-one survived. She keeps a private ledger of the thirty-one dead: their names, the date, the Catalyst batch, her technical notes on what she observed as the infusion failed. Her first patient was her younger brother. She reviews the ledger once a year on the anniversary of his death. She has never discussed this practice with anyone. Her daughter Aderyn is training in her practice and does not know the ledger exists, or what it contains.',
        N'Aelwyn is what sustained institutional loss does to a person who cannot stop: a practitioner who has administered death to thirty-one people and keeps working because the alternative is abandoning the twenty-three years she has already spent.',
        N'No POV.',
        N'House Pallor; island medical and Transmutation facility, capital district',
        166, 63, N'average',
        N'silver-streaked brown', N'neatly pinned', N'medium',
        N'brown', N'fair', N'clear, deeply tired around the eyes',
        N'none',
        N'Composed and unhurried; the deliberateness of someone who has learned to move carefully around volatile materials',
        N'Practitioner''s robes, dark and clean; the implements of her work kept in a specific order she has maintained for twenty years',
        N'none',
        N'Patient assessments in the morning. Catalyst preparation. Infusion sessions when scheduled. Reports to House medical authority. One evening per year she retrieves the ledger from the locked case under her workbench and reads every name.',
        N'Her younger brother Emrys was her first infusion patient thirty years ago. She believed she was ready. She was not. He died on the table at twenty-two years old. She has administered infusions to seventy-one more people since then, and forty of them survived, and she considers this a ratio she has not yet earned the right to accept. Her daughter Aderyn has overheard enough fragments to have assembled the truth, and has not told her mother she knows. They are both carrying the same knowledge in a shared silence neither has broken.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Island capital Transmutation facility; House medical authority',
        N'0', N'0',
        N'Middle-aged Welsh-looking woman practitioner in dark robes, silver-streaked brown pinned hair, composed tired expression, stone medical chamber with practitioner implements, medieval fantasy, Buehlman dark register --ar 2:3',
        N'A 50-year-old Transmutation practitioner in dark robes, silver-streaked brown hair, composed but tired face, medieval stone medical chamber',
        0, 0
    );
    PRINT 'Aelwyn Croft seeded.';
END
ELSE PRINT 'Aelwyn Croft already exists.';
GO

-- 18. Meryn Voss
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Meryn Voss')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Meryn Voss', N'meryn-voss', N'canon', 1,
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
        @id, N'Meryn Voss', N'meryn-voss', N'Meryn', N'Voss', N'',
        N'human', N'human', N'male', N'he/him', 38, N'alive',
        N'Apprentice Transmutation practitioner under Mistress Aelwyn Croft',
        N'Meryn Voss has been Mistress Croft''s apprentice for six years and has shown a steady technical competence that has never quite risen to the level of brilliance his supervisor''s work demands. He is aware of this gap. Three years ago he administered his first solo infusion under supervision. The patient died on the table within four minutes of Catalyst introduction — a clean failure, the kind that happens to competent practitioners thirty percent of the time. Mistress Croft filed the report herself and listed the cause as equipment variance. She has never asked him for anything in return. He has been waiting for her to.',
        N'Meryn is defined by a debt he cannot repay to a person who has not named the price — which may mean no price exists, or may mean the price will be named at the worst possible moment.',
        N'No POV.',
        N'House Pallor; island capital Transmutation facility',
        175, 72, N'average',
        N'dark blond', N'neatly kept', N'short',
        N'grey-green', N'fair', N'clear',
        N'none',
        N'Careful and precise; slightly formal with his supervisor; more relaxed with patients, which is better for them',
        N'Practitioner''s working robes, kept clean; follows Mistress Croft''s standards in presentation',
        N'none',
        N'Assist with patient assessments. Catalyst preparation support. Observation during infusion sessions, stepping to lead only when Mistress Croft assigns it. Studies in the evenings. Checks the records of his failed patient''s family without knowing why.',
        N'His first solo infusion killed his patient. Mistress Croft covered for him by filing the report herself with an equipment-variance cause code. She has never mentioned it again. He has been waiting for three years for her to ask him to do something difficult in return — falsify a record, overlook a problem, assist with something irregular. She has not. He does not know if this means she expects nothing or if it means she is saving the debt for something he cannot yet imagine. Both possibilities frighten him.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Island capital Transmutation facility; patient wards',
        N'0', N'0',
        N'English-looking man in practitioner robes, dark blond hair, careful precise bearing, medieval stone medical chamber, slightly formal posture, Buehlman register --ar 2:3',
        N'A 38-year-old apprentice practitioner in working robes, dark blond hair, careful precise expression, medieval stone chamber',
        0, 0
    );
    PRINT 'Meryn Voss seeded.';
END
ELSE PRINT 'Meryn Voss already exists.';
GO

-- 19. Eldric Somers
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Eldric Somers')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Eldric Somers', N'eldric-somers', N'canon', 1,
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
        @id, N'Eldric Somers', N'eldric-somers', N'Eldric', N'Somers', N'',
        N'human', N'human', N'male', N'he/him', 63, N'alive',
        N'Retired Transmutation practitioner; now a healer for the lower garrison, non-Catalyst work only',
        N'Eldric Somers retired from formal Transmutation practice fifteen years ago after twenty-two years of active work. He now treats garrison soldiers for injuries, illness, and the standard complaints of hard physical service — nothing requiring Catalysts, which he has not touched since retirement. He is considered a reliable and compassionate healer by the garrison rank-and-file, who find him easier to speak to than the certified practitioners. What none of them know is that he is carrying a secret from his practitioner years that has never fully released him: a patient whose transformation deviated from every documented progression in his experience.',
        N'Eldric is a man who helped suppress knowledge and has spent fifteen years deciding whether suppression was the right call. He visits the person he helped hide twice a year. The visits have not helped him decide.',
        N'No POV.',
        N'House Pallor; lower garrison healer''s post; twice-yearly visits to a concealed location',
        172, 79, N'stocky, age-softened',
        N'white', N'unkempt, thinning', N'short',
        N'pale blue', N'fair', N'weathered, kind-lined',
        N'none',
        N'Slow and deliberate; the unhurried authority of a man who no longer has anything to prove',
        N'Plain healer''s robes, well-worn; no insignia; the working clothes of someone who does the work without wanting credit for it',
        N'none',
        N'Garrison healer''s rounds morning and afternoon. Treats injuries and illness. Talks more than most practitioners do, which the garrison soldiers value. Twice a year takes a three-day absence he describes as a family visit, which no one has questioned in fifteen years.',
        N'Twenty years ago he administered an infusion that produced a transformation he had never seen and has never seen since: not the standard progression to Knight, but something that continued. The patient did not die. The patient did not stop changing. He helped suppress the record — classified the infusion as a standard failure, listed the patient as dead. The patient is alive and has been living in concealment with Eldric''s assistance since then. He visits them twice a year. He cannot explain what they have become. He cannot decide whether concealing this from the House was protective or monstrous.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Lower garrison healer''s post; concealed location visited twice yearly',
        N'0', N'0',
        N'Old English-looking retired practitioner in plain worn robes, white unkempt hair, kind weathered face, garrison healer''s room with simple medical implements, medieval fantasy --ar 2:3',
        N'A 63-year-old retired practitioner in worn plain robes, white hair, kind weathered face, medieval garrison healer room',
        0, 0
    );
    PRINT 'Eldric Somers seeded.';
END
ELSE PRINT 'Eldric Somers already exists.';
GO

-- 20. Demelza Thorn
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Demelza Thorn')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Demelza Thorn', N'demelza-thorn', N'canon', 1,
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
        @id, N'Demelza Thorn', N'demelza-thorn', N'Demelza', N'Thorn', N'',
        N'human', N'human', N'female', N'she/her', 45, N'alive',
        N'Morvic Transmutation practitioner; also keeper of the coastal Morvic ritual observances',
        N'Demelza Thorn holds the two roles that define the Morvic people''s complicated position in House Pallor: she is a certified Transmutation practitioner under the House''s standard protocol, and she is the keeper of the old Morvic pre-infusion rituals that the House tolerates without endorsing. She has been quietly incorporating the ritual preparation — three days of specific diet, a set of physical practices, and a spoken observance she performs with the candidate — into her infusion protocol. Her last four infusion candidates have all survived. The standard survival rate is under thirty percent. She cannot publish this.',
        N'Demelza is the argument for knowledge the institution refuses to recognize: her results are better and she cannot tell anyone why without also explaining that she has been modifying the protocol the House certified her to follow.',
        N'No POV.',
        N'House Pallor; Morvic coast, certified practitioner''s post; coastal ritual sites',
        165, 64, N'average',
        N'dark grey', N'loose and long', N'long',
        N'green', N'medium warm', N'weathered, sea-lined',
        N'none',
        N'Grounded and calm; moves with unhurried authority; comfortable outdoors in a way the island practitioners are not',
        N'Practitioner''s robes in Morvic coastal cut; Morvic pattern at the hem; practical and weather-appropriate',
        N'none',
        N'Patient consultations. Catalyst preparation using the modified protocol she does not officially use. Infusion sessions. Attends to the ritual observation sites on the coast twice a month. Teaches the ritual preparation to candidates in advance of their infusion, describing it as a calming practice.',
        N'Her modified protocol has produced four consecutive survivors from a patient pool that should, statistically, have yielded one. She knows this is not chance. She believes the ritual preparation — the diet, the physical practice, the spoken observance — does something to the candidate''s physiology that makes the Catalyst integration more stable. She cannot prove this without a controlled study she cannot authorize. She cannot authorize it without admitting she has been modifying the certified protocol. If the House learned she''d been altering approved procedures, she would lose her certification regardless of the results.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Morvic coast; certified practitioner''s post; coastal ritual observance sites',
        N'0', N'0',
        N'Cornish-looking woman practitioner in Morvic-cut robes with coastal pattern, dark grey loose hair, calm grounded bearing, stone coastal chamber with ritual and medical objects, medieval fantasy --ar 2:3',
        N'A 45-year-old woman practitioner in patterned Morvic robes, dark grey loose hair, calm expression, stone coastal setting',
        0, 0
    );
    PRINT 'Demelza Thorn seeded.';
END
ELSE PRINT 'Demelza Thorn already exists.';
GO

-- 21. Wyn Carrow
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Wyn Carrow')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Wyn Carrow', N'wyn-carrow', N'canon', 1,
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
        @id, N'Wyn Carrow', N'wyn-carrow', N'Wyn', N'Carrow', N'',
        N'human', N'human', N'male', N'he/him', 34, N'alive',
        N'Oathless; former House Pallor Myrmidon, broke oath rather than execute Kellian civilians',
        N'Wyn Carrow was a six-year Myrmidon in good standing when he refused a direct order to execute a group of Kellian farmsteaders accused of sheltering Oathless. He refused in front of witnesses, accepted the consequence, and walked away from the House''s service into the hill country. He has been Oathless for four years. He now protects the farmsteading communities in the northern hills — the exact people he refused to kill. He is not bitter about House Pallor''s cause. He is specifically, precisely bitter about its methods. The distinction matters to him enormously and is invisible to the House.',
        N'Wyn is the moral argument embedded in a person: a man whose oath-breaking was an act of principle, who now lives with the institutional consequences of a correct decision, in a House that cannot recognize correct decisions that contradict orders.',
        N'No POV.',
        N'House Pallor origin; northern hill country, Kellian farming communities',
        180, 84, N'athletic',
        N'light brown', N'short, practical', N'short',
        N'hazel', N'fair-medium', N'weathered, outdoor-rough',
        N'none',
        N'Alert and economical; the bearing of someone who has replaced institutional safety with personal vigilance',
        N'Worn civilian clothes, practical for outdoor living; nothing that marks House origin',
        N'none',
        N'Patrols the northern hills between farmsteading settlements. Warns communities of garrison movements. Occasionally helps with farm work in exchange for food and shelter. Has taught himself to track in terrain he used to march through as a garrison soldier.',
        N'He still believes in House Pallor''s purpose: protecting the island, holding the channel, keeping the three peoples together. He broke his oath because the specific order he refused was wrong, not because the House is wrong. This distinction is one he has never been able to explain to anyone in the Oathless community who shelters him, because it sounds like a man who is halfway back to the institution that cast him out. He is afraid they are right.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Northern hill country; Kellian farming communities; avoids garrison patrol routes',
        N'0', N'0',
        N'Athletic Welsh-looking man in worn practical civilian clothes, light brown hair, hazel eyes, alert outdoor bearing, northern hill country farmstead, medieval fantasy --ar 2:3',
        N'A 34-year-old Oathless former soldier in worn civilian clothes, light brown hair, alert expression, standing in hill country terrain',
        0, 0
    );
    PRINT 'Wyn Carrow seeded.';
END
ELSE PRINT 'Wyn Carrow already exists.';
GO

-- 22. Isolde Falk
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Isolde Falk')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Isolde Falk', N'isolde-falk', N'canon', 1,
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
        @id, N'Isolde Falk', N'isolde-falk', N'Isolde', N'Falk', N'',
        N'human', N'human', N'female', N'she/her', 46, N'alive',
        N'Oathless; former House administrative officer, possesses a copy of Pallor''s intelligence ledger',
        N'Isolde Falk was a senior administrative officer in House Pallor''s intelligence annex until three years ago, when she uncovered evidence that a senior official had embezzled substantially from the channel defense fund. Before she could report this, she was made Oathless on unrelated grounds — a charge of unauthorized record access that she had committed in the course of uncovering the embezzlement. She understood what was happening and spent her last three days of House service copying the intelligence ledger. She has lived in the harbor district under a different name for three years. She has not used the ledger.',
        N'Isolde is a person with a weapon she cannot fire without destroying herself: a leveraged position that provides no safety because using the leverage would immediately expose her location to the institution trying to keep her silent.',
        N'No POV.',
        N'House Pallor origin; harbor district, living under false name',
        167, 68, N'average',
        N'brown-grey', N'worn, loosely pinned', N'medium',
        N'grey', N'fair', N'clear, watchful-lined',
        N'none',
        N'Economical and minimizing; moves through public spaces in ways designed not to be memorable',
        N'Plain civilian dress; nothing that marks administrative function or House origin',
        N'none',
        N'Works as a copying clerk for a harbor merchant who does not ask questions. Keeps the intelligence ledger copy in a waterproofed case under the floor of her rented room. Reviews it periodically to assess whether the information is still actionable. Watches for any sign that the House knows where she is.',
        N'She has a copy of House Pallor''s intelligence ledger from three years ago — names, cover identities, and contact protocols for every active operative on the continent and island. The ledger is three years old. Some of the information is certainly outdated. Some of it is certainly not. The person she copied it to expose is now promoted. She has not moved against him because doing so would require revealing herself to the House, and the House would execute her for Oathless concealment before it would punish the embezzler. She is waiting for a moment that may not come.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Harbor district; false identity; avoids intelligence annex personnel',
        N'0', N'0',
        N'Middle-aged English-looking woman in plain civilian dress, brown-grey hair loosely pinned, watchful grey eyes, harbor district street scene, medieval fantasy, minimizing posture --ar 2:3',
        N'A 46-year-old woman in plain civilian dress, brown-grey hair, watchful expression, medieval harbor district setting',
        0, 0
    );
    PRINT 'Isolde Falk seeded.';
END
ELSE PRINT 'Isolde Falk already exists.';
GO

-- 23. Gethen Ros
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Gethen Ros')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Gethen Ros', N'gethen-ros', N'canon', 1,
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
        @id, N'Gethen Ros', N'gethen-ros', N'Gethen', N'Ros', N'',
        N'human', N'human', N'male', N'he/him', 55, N'alive',
        N'Oathless; former Knight of House Pallor; deep-cover intelligence operative whose cover became permanent',
        N'Gethen Ros received his Knight''s infusion at thirty-one and served six years before Cormac Trevise''s predecessor ordered him to stage an oath-breaking and infiltrate the Oathless networks in the hill country. He did so. He was good at it. He is still doing it fifteen years later. The intelligence handler who authorized the operation died four years ago. The authorization chain no longer exists. No one in House Pallor''s current intelligence apparatus knows that Gethen Ros is still an active operative rather than a genuine Oathless. He has been waiting for someone to contact him. No one has.',
        N'Gethen is the institutional ghost: a man who completed his mission so thoroughly that the institution forgot about him, leaving him alive in an identity that has now outlasted the operation it was built for.',
        N'No POV.',
        N'House Pallor origin; hill country Oathless communities; intelligence network access',
        188, 97, N'athletic',
        N'iron grey', N'close-cropped', N'short',
        N'pale green', N'fair', N'deeply weathered, scarred',
        N'Subtle height gain; increased skeletal density visible in jaw and brow; vascular prominence on forearms',
        N'Economical and authoritative; the bearing of a Knight that fifteen years of Oathless life have not fully erased',
        N'Worn Oathless civilian gear; nothing that marks House service; the Knight''s enhancement is visible to anyone who looks carefully at his proportions',
        N'Knight — first infusion at thirty-one; modest enhancement still evident',
        N'Moves through hill country communities maintaining relationships with Oathless network contacts. Collects information he no longer has a channel to deliver. Has started writing intelligence reports in a standardized format and storing them, on the theory that someone will eventually come looking.',
        N'He has been a genuine intelligence operative masquerading as Oathless for fifteen years. His original handler is dead. The authorization for his operation died with that handler. He does not know if Cormac Trevise knows he exists. He has been collecting intelligence for four years with nowhere to deliver it. The reports are in a waterproofed case in the hill country. He has not decided at what point he should simply walk up to the intelligence annex and announce himself, which would require explaining fifteen years of silence and the full extent of what he knows about the Oathless networks he has infiltrated.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Hill country Oathless communities; former intelligence network routes',
        N'0', N'0',
        N'Older athletic English-looking man, iron grey close-cropped hair, scarred weathered face, enhanced Knight proportions, worn civilian hill country gear, Oathless camp setting, medieval fantasy --ar 2:3',
        N'A 55-year-old former knight in worn civilian clothes, iron grey hair, scarred face, enhanced physique, standing in hill country terrain',
        0, 0
    );
    PRINT 'Gethen Ros seeded.';
END
ELSE PRINT 'Gethen Ros already exists.';
GO

-- 24. Branwen Cull
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Branwen Cull')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Branwen Cull', N'branwen-cull', N'canon', 1,
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
        @id, N'Branwen Cull', N'branwen-cull', N'Branwen', N'Cull', N'',
        N'human', N'human', N'female', N'she/her', 28, N'alive',
        N'Oathless; cast out at twenty-two for striking a superior officer who was extorting Kellian farmsteaders',
        N'Branwen Cull struck Sergeant Harwick Dene across the face in front of eleven witnesses after three years of watching him take grain from Kellian farmsteaders below the legal requisition rate and pocket the difference. She knew what would happen. She did it anyway, which is a sentence that describes most of the significant moments in her six years of Oathless life. She is sheltered by the Kellian farmsteading communities whose situation she worsened by making a scene, which is its own kind of irony. She is currently planning to surrender herself to the House to testify against Dene, who has since been promoted.',
        N'Branwen is the argument that the institution''s punishment system can be used as a weapon against itself — if you are willing to pay what it costs, which she is, and if the institution is willing to listen, which it may not be.',
        N'No POV.',
        N'House Pallor origin; Kellian farming communities, northern territory',
        163, 61, N'lean-wiry',
        N'black', N'short, practical', N'short',
        N'dark brown', N'warm medium-dark', N'weathered, outdoor-rough',
        N'none',
        N'Direct and unguarded; moves without the minimizing habits of longer-term Oathless; has not yet learned to take up less space',
        N'Worn practical outdoor clothing; Kellian-pattern scarf at the neck that the farmsteaders gave her',
        N'none',
        N'Works with Kellian farming communities as a practical laborer and informal protector. Has sent two separate written requests to the House council for a hearing on Dene''s conduct. Both have been received and not answered. She is drafting a third. She is also preparing for the possibility that the answer will be a garrison squad rather than a council summons.',
        N'She struck Dene because no one else would, and the Kellian farmsteaders benefited from it even though she paid the cost. Her actual secret is simpler than a political calculation: she is planning to walk into the garrison captain''s office and surrender herself for processing, which will force the House to formally hear or formally ignore her account of Dene''s conduct. She knows she may be executed for Oathless concealment. She has decided this is acceptable if it puts Dene''s name on a record that someone will eventually read.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Kellian farming communities; northern Pallor territory',
        N'0', N'0',
        N'Young Welsh-Celtic woman in worn outdoor clothes, short black hair, dark eyes, direct unguarded bearing, Kellian farmstead setting, medieval fantasy --ar 2:3',
        N'A 28-year-old Oathless woman in worn outdoor clothes, short black hair, direct expression, medieval farmstead',
        0, 0
    );
    PRINT 'Branwen Cull seeded.';
END
ELSE PRINT 'Branwen Cull already exists.';
GO

-- 25. Padrig Ellory
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Padrig Ellory')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Padrig Ellory', N'padrig-ellory', N'canon', 1,
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
        @id, N'Padrig Ellory', N'padrig-ellory', N'Padrig', N'Ellory', N'',
        N'human', N'human', N'male', N'he/him', 41, N'alive',
        N'Oathless; former House Pallor intelligence operative; now an information broker',
        N'Padrig Ellory was a House Pallor intelligence operative for nine years before his family''s situation — specific, targeted, unjust — drove him out. He has been selling intelligence to Atrament for six years. He prices his intelligence by what damage it will do to House Pallor specifically, which is not a profitable pricing strategy but reflects his genuine sentiment. His brother Rhys still serves the House on the Kellian council and believes Padrig has been dead for three years. Someone told Rhys this. Someone was wrong or lied. The last piece of information Padrig sold may have contributed to a patrol ambush that killed thirty-one soldiers. He has not slept a full night since.',
        N'Padrig is a man discovering that hatred does not protect you from the weight of the specific harm it produces — and that the soldiers he never thought about individually are now named.',
        N'No POV.',
        N'House Pallor origin; harbor district and hill country, intelligence broker contacts',
        177, 74, N'lean',
        N'dark brown', N'unremarkable', N'short',
        N'dark brown', N'medium warm', N'clear, sleepless-marked',
        N'none',
        N'Controlled and professional when working; increasingly frayed at the edges in private',
        N'Plain civilian clothing chosen for unremarkability; no House markers; carries nothing that cannot be explained',
        N'none',
        N'Receives information from his network of former intelligence contacts. Packages it for his Atrament buyers. Walks the harbor district at night when he cannot sleep. Has not sent anything in the three weeks since the patrol ambush report reached him.',
        N'Thirty-one soldiers died in an ambush that used patrol timing Padrig sold to an Atrament factor six weeks earlier. He does not know for certain that his information was used. He knows the timing matches, and he knows Atrament buys patrol intelligence specifically to share with opportunistic raiding parties. He has been in this business long enough to know exactly what his information is used for. He told himself for six years that the soldiers of House Pallor were the institution, not people. He is no longer able to sustain this. He has not decided what to do next.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Harbor district; hill country contacts; Atrament broker meetings at coastal exchange points',
        N'0', N'0',
        N'Irish-looking man in plain civilian clothes, dark brown hair, controlled expression over visible sleeplessness, medieval harbor district, Buehlman dark register --ar 2:3',
        N'A 41-year-old information broker in plain clothes, dark brown hair, controlled but sleepless expression, medieval harbor district',
        0, 0
    );
    PRINT 'Padrig Ellory seeded.';
END
ELSE PRINT 'Padrig Ellory already exists.';
GO

-- 26. Hessa Mander
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Hessa Mander')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Hessa Mander', N'hessa-mander', N'canon', 1,
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
        @id, N'Hessa Mander', N'hessa-mander', N'Hessa', N'Mander', N'Mistress',
        N'human', N'human', N'female', N'she/her', 57, N'alive',
        N'Liturgy Archivist; senior officer for the Pallor station; longest-serving Liturgy representative on the island',
        N'Hessa Mander has represented the Liturgy''s interests in Pallor territory for nineteen years, which is long enough to have watched three House Pallor lords come and go and to have developed a clear-eyed understanding of where institutional loyalty ends and institutional compromise begins. She is scrupulous in her official duties and has spent the last eight months trying to decide what to do with what she discovered: the Pallor Liturgy station has been conducting unauthorized secondary extractions — taking persons from Sphere 31 off-manifest and delivering them to private buyers. The Liturgy''s own internal authority is the correct channel. She does not trust it.',
        N'Hessa is the institutional conscience in the moment before action: a person who has found a genuine wrong inside the organization she has spent her career serving, and who must decide whether the organization can police itself or whether exposing it requires destroying her position in it.',
        N'No POV.',
        N'Liturgy; House Pallor station, island capital',
        169, 70, N'average',
        N'white', N'neat, pulled back', N'medium',
        N'grey', N'fair', N'clear, composed-lined',
        N'none',
        N'Precise and authoritative; the posture of someone who has represented institutional power for long enough that it no longer requires effort',
        N'Liturgy Archivist robes, dark grey with silver station insignia; immaculate',
        N'none',
        N'Reviews extraction manifests and station reports in the morning. Holds official liaison meetings with House Pallor''s Scrying installation contacts. In the afternoons she has been conducting a private audit of the secondary extraction records, building a case file she has not yet decided how to use.',
        N'She has identified a pattern of off-manifest extractions running through the Pallor station for at least six years — persons taken from Sphere 31 outside the standard Liturgy quota system, delivered to unnamed private recipients. The financial trail runs through three cut-out accounts to a senior Liturgy officer above her own station. She could report this to the Liturgy''s internal authority. That authority is staffed by people who report to the officer she is investigating. She has given herself until the end of the quarter to decide whether she reports internally, reports to House Pallor (which would trigger a diplomatic crisis), or does nothing.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Pallor Liturgy station, island capital; Scrying installation liaison',
        N'0', N'0',
        N'Older English-looking woman in immaculate dark grey Liturgy robes with silver insignia, white pulled-back hair, composed authoritative bearing, stone Liturgy archive office, medieval fantasy --ar 2:3',
        N'A 57-year-old Liturgy Archivist in dark grey robes, white hair, authoritative composed expression, medieval stone archive',
        0, 0
    );
    PRINT 'Hessa Mander seeded.';
END
ELSE PRINT 'Hessa Mander already exists.';
GO

-- 27. Tobin Greth
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Tobin Greth')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Tobin Greth', N'tobin-greth', N'canon', 1,
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
        @id, N'Tobin Greth', N'tobin-greth', N'Tobin', N'Greth', N'',
        N'human', N'human', N'male', N'he/him', 33, N'alive',
        N'Liturgy extraction officer; Sphere 31 interview and assessment specialist',
        N'Tobin Greth is one of the Liturgy''s better extraction interviewers — methodical, not unkind, genuinely attentive to the disorientation that Sphere 31 persons experience in the interview process. He has conducted eighty-three assessments over four years. Eighty-two of them ended as the Liturgy intended. The eighty-third is Nathalie of Sphere 31, whom he interviewed seven months ago and has been finding procedural reasons to delay ever since. His case file for her now contains forty-one procedural addenda. A supervisor will review it within the month. The record will not survive scrutiny.',
        N'Tobin is a man who has discovered, through a specific person, that he cannot do the work the institution requires of him without looking at it from the outside. He has not resolved what this means. He is running out of procedural time.',
        N'No POV.',
        N'Liturgy; Pallor extraction station, interview facility',
        180, 78, N'average',
        N'light brown', N'neat, standard cut', N'short',
        N'blue', N'fair', N'clear',
        N'none',
        N'Professional and careful in official settings; visibly distracted when alone; a man conducting two parallel lives',
        N'Liturgy officer''s dress, dark grey; maintained correctly; nothing that signals the distress underneath',
        N'none',
        N'Morning: standard extraction preparation and case reviews. Afternoon: interviews. His afternoons have been dominated for seven months by the addenda he is adding to Case 83. He eats alone. He has stopped socializing with the other Liturgy staff in ways they have noticed without commenting on.',
        N'He is in love with a Sphere 31 woman he was assigned to assess for extraction. He has been manufacturing procedural delays for seven months to keep her from being extracted to a House assignment. He knows this cannot last. He does not know how to stop. What he has not told anyone — what he has barely told himself — is that he has been considering contacting Hessa Mander''s office to report an irregularity in the extraction process that would, as a side effect, freeze all Pallor station extractions pending review. He does not know if he can do this.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Liturgy extraction station, Pallor; interview facility',
        N'0', N'0',
        N'Young English-looking Liturgy officer in dark grey uniform, light brown hair, professionally neat but internally distracted, stone Liturgy interview room, medieval fantasy --ar 2:3',
        N'A 33-year-old Liturgy officer in dark grey dress, light brown hair, neat but distracted expression, medieval stone interview room',
        0, 0
    );
    PRINT 'Tobin Greth seeded.';
END
ELSE PRINT 'Tobin Greth already exists.';
GO

-- 28. Colm Varle
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Colm Varle')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Colm Varle', N'colm-varle', N'canon', 1,
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
        @id, N'Colm Varle', N'colm-varle', N'Colm', N'Varle', N'',
        N'human', N'human', N'male', N'he/him', 40, N'alive',
        N'Liturgy field scout; Sphere 31 community assessment and extraction candidate identification',
        N'Colm Varle has been observing the same Sphere 31 community for four years as part of the Pallor station''s extraction pipeline. He knows their names. He knows who is healthy, who is struggling, who has children, who works which hours. Last season his quota required him to mark twelve community members as extraction-eligible, knowing three would be selected. He marked them by his private assessment of who would survive the transition best. One of his selections was dead within two months of extraction. He is writing an apology letter he will never send.',
        N'Colm is the human face of the extraction system in the moment it can no longer be abstracted: a man who chose who would be taken, and who has to live with his criteria.',
        N'No POV.',
        N'Liturgy; Sphere 31 field observation; Pallor station',
        173, 72, N'average',
        N'brown', N'short, plain', N'short',
        N'brown', N'medium', N'weathered, outdoor-marked',
        N'none',
        N'Quiet and methodical outdoors; slightly withdrawn in the office; carries his work home with him in a way that does not have a name',
        N'Liturgy field scout''s practical clothing, unremarkable; built for outdoor observation work',
        N'none',
        N'Observation sessions through the Sphere 31 membrane site. Filing assessment reports. Updating community records. In the evenings he sits with a piece of paper he started writing on two months ago and has never finished.',
        N'He chose the twelve extraction candidates using his own criteria: he marked people who had no dependent family, who were physically resilient, who he judged most likely to adapt. One of his selected candidates — a man named Fenwick, thirty-four, no dependents, physically strong — died of a fever within eight weeks of extraction. Colm has his name written on the paper he cannot finish. He does not know if he could have chosen differently. He knows that Fenwick is dead because Colm put his name on a list.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Sphere 31 field observation sites; Pallor Liturgy station; outdoor transit routes',
        N'0', N'0',
        N'Middle-aged Irish-Scottish looking man in plain outdoor field clothes, brown hair, quiet withdrawn expression, Sphere 31 membrane observation site, medieval-fantasy Scrying setting --ar 2:3',
        N'A 40-year-old Liturgy field scout in plain outdoor clothes, brown hair, quiet withdrawn expression, at a medieval Scrying membrane site',
        0, 0
    );
    PRINT 'Colm Varle seeded.';
END
ELSE PRINT 'Colm Varle already exists.';
GO

-- 29. Wren Ashmore
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Wren Ashmore')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Wren Ashmore', N'wren-ashmore', N'canon', 1,
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
        @id, N'Wren Ashmore', N'wren-ashmore', N'Wren', N'Ashmore', N'',
        N'human', N'human', N'female', N'she/her', 26, N'alive',
        N'Junior Liturgy clerk; Pallor station records and manifests',
        N'Wren Ashmore has been a Liturgy clerk for two years and is considered efficient and conscientious. She is both of those things, which is why she noticed the discrepancy: sixty-three more persons appear in Pallor''s internal demographic records than in the official extraction manifests. Sixty-three people who arrived in the Cauld through the Liturgy''s infrastructure and were never logged in the standard system. The senior officer who would receive a discrepancy report is the same senior officer whose signature appears on all sixty-three anomalous intake documents. She has not filed the report.',
        N'Wren is a person standing at the entrance to a cover-up she did not participate in, trying to determine whether the door she is looking at leads somewhere she can survive entering.',
        N'No POV.',
        N'Liturgy; Pallor station records office',
        161, 56, N'lean',
        N'light brown', N'neat, tied back', N'medium',
        N'grey', N'fair', N'clear, young',
        N'none',
        N'Precise and organized at work; slightly stiff with senior staff; relaxes only outside the office',
        N'Liturgy clerk''s grey office dress; functional and unremarkable',
        N'none',
        N'Morning: manifests and correspondence processing. Afternoon: records filing and cross-reference audits. She has been running the demographic cross-reference as an unofficial audit for six weeks. She keeps the working notes in a personal ledger she carries home with her.',
        N'She has found sixty-three persons in Pallor''s demographic records with no corresponding extraction manifest. Someone extracted them — the Liturgy infrastructure was used, the intake records were created — but they were never formally logged. The signature on all sixty-three intake records belongs to Senior Officer Davan Myre, who is her supervisor''s supervisor. She does not know if these persons are alive, dead, privately assigned, or something else. She is afraid to report it because she is also afraid not to. She has told no one.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Pallor Liturgy station records office; walks home through the harbor district',
        N'0', N'0',
        N'Young English-looking woman in grey Liturgy clerk dress, light brown hair tied back, precise anxious expression, stone Liturgy records office, medieval fantasy --ar 2:3',
        N'A 26-year-old Liturgy clerk in grey dress, light brown hair, precise anxious expression, medieval records office',
        0, 0
    );
    PRINT 'Wren Ashmore seeded.';
END
ELSE PRINT 'Wren Ashmore already exists.';
GO

-- 30. Arvyn Pryce
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Arvyn Pryce')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Arvyn Pryce', N'arvyn-pryce', N'canon', 1,
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
        @id, N'Arvyn Pryce', N'arvyn-pryce', N'Arvyn', N'Pryce', N'',
        N'human', N'human', N'male', N'he/him', 66, N'alive',
        N'Estate steward; Caer-Mael household; has managed three successive Lords of House Pallor',
        N'Arvyn Pryce has been the Caer-Mael estate''s steward for thirty-one years and has outlasted two Lords before the current one, which makes him the institutional memory of the estate in ways that no record quite captures. He manages the household with calm efficiency and absolute discretion. He is the kind of man who prevents problems before they become incidents and incidents before they become records, and who has learned when those two functions serve the House and when they serve the Lord specifically. He is aware of the difference.',
        N'Arvyn is the quiet power of institutional continuity: a man who has survived three lords by being more necessary than any of them, and who carries what he has done on their behalf as a burden he cannot set down.',
        N'No POV.',
        N'House Pallor; Caer-Mael estate, island capital',
        173, 83, N'stocky, age-settled',
        N'white', N'neat, thinning', N'short',
        N'pale blue', N'fair', N'weathered, dignity-lined',
        N'none',
        N'Still and unhurried; the posture of a man who has managed people in rooms of power long enough to know the value of taking up exactly the right amount of space',
        N'Steward''s formal dress, dark and well-maintained; the House sigil at the breast pocket',
        N'none',
        N'Morning: household accounts and staff scheduling. Midday: manages estate operations. Afternoon: correspondence and supply oversight. Evenings: reviews records that he keeps more current and complete than the official archive.',
        N'Fifteen years ago, the previous Lord — Aldwyn''s predecessor — was embezzling from the channel defense fund. Arvyn discovered this and destroyed the correspondence that would have proved it before anyone else could find it. Not to protect the Lord, whom he did not particularly respect, but because the exposure would have destabilized the council at a moment when the channel defense budget was already depleted. The embezzled money went into a private fund the Lord then directed to shore up the defense anyway. The Lord died the following year. Arvyn has never told anyone. He thinks about it every time someone praises the House''s fiscal integrity.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Caer-Mael estate, island capital; occasional council liaison',
        N'0', N'0',
        N'Older dignified English-looking man in dark formal steward''s dress, white thinning hair, still authoritative bearing, stone estate interior, medieval fantasy --ar 2:3',
        N'A 66-year-old estate steward in dark formal dress, white hair, still dignified expression, medieval stone estate interior',
        0, 0
    );
    PRINT 'Arvyn Pryce seeded.';
END
ELSE PRINT 'Arvyn Pryce already exists.';
GO

-- 31. Eithne Caer-Mael
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Eithne Caer-Mael')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Eithne Caer-Mael', N'eithne-caer-mael', N'canon', 1,
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
        @id, N'Eithne Caer-Mael', N'eithne-caer-mael', N'Eithne', N'Caer-Mael', N'Lady',
        N'human', N'human', N'female', N'she/her', 54, N'alive',
        N'Lady of House Pallor; political voice for the Anglic people; the actual strategic mind behind several of Aldwyn''s decisions',
        N'Eithne Caer-Mael has been married to Lord Aldwyn for twenty-eight years and is consistently underestimated by people who mistake composure for passivity. She is the person Aldwyn discusses hard decisions with before he makes them, and she is often the person who sees the shape of those decisions more clearly than he does. The flooding of the Kellian settlement at Brae Crossing was her suggestion. She has let Aldwyn carry that weight before the council because she understands that a Lady''s political position requires the appearance of distance from the most controversial choices. She is entirely clear-eyed about what this costs him.',
        N'Eithne is the person behind the throne who is not behind the throne — a woman whose actual strategic function is invisible because she has designed it that way, and who has allowed someone she loves to bear the moral weight of decisions she made.',
        N'No POV.',
        N'House Pallor; island capital, Caer-Mael estate, council chambers',
        168, 65, N'average',
        N'silver-white', N'elegantly arranged', N'long',
        N'grey-green', N'fair', N'composed, fine-lined',
        N'none',
        N'Composed and deliberate; occupies space with the specific authority of someone who has never needed to raise her voice',
        N'House formal dress, dark and precise; the Lady''s sigil; no armor and no apology for it',
        N'none',
        N'Morning correspondence and council preparation. Holds her own set of appointments with council members, merchants, and diplomatic contacts — a parallel schedule to Aldwyn''s that is not officially recorded. Attends formal events at Aldwyn''s side. Makes the decisions that require her specific knowledge afterward.',
        N'She suggested the flooding of the Kellian settlement. She had run the defense analysis and saw no other viable channel to hold. She presented it to Aldwyn as an option; he accepted it and gave the order. She has never told the Kellian council members who blame him. She cannot tell them without destroying her own political position, and her political position is the instrument she uses to manage the three-people council system that keeps Pallor from fracturing. She considers this a correct calculation. She is aware it required her husband to carry a weight she placed on him.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Island capital; council chambers; Caer-Mael estate; informal political network across all three peoples',
        N'0', N'0',
        N'Elegant older Irish-looking woman in dark House formal dress, silver-white arranged hair, grey-green eyes, composed authoritative bearing, stone council chamber, medieval fantasy --ar 2:3',
        N'A 54-year-old Lady in dark formal dress, silver-white hair, composed expression, medieval stone council chamber',
        0, 0
    );
    PRINT 'Eithne Caer-Mael seeded.';
END
ELSE PRINT 'Eithne Caer-Mael already exists.';
GO

-- 32. Gwyneira Lorne
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Gwyneira Lorne')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Gwyneira Lorne', N'gwyneira-lorne', N'canon', 1,
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
        @id, N'Gwyneira Lorne', N'gwyneira-lorne', N'Gwyneira', N'Lorne', N'',
        N'human', N'human', N'female', N'she/her', 43, N'alive',
        N'Morvic estate manager; council liaison for the Morvic people; informal Draught back-channel contact',
        N'Gwyneira Lorne manages the Morvic coastal estates — a job that requires simultaneously managing three Anglic-Morvic boundary disputes, two Morvic fishing community quotas, and a relationship with the council that requires her to represent Morvic interests without appearing to advocate for them too strongly. She is very good at this. She has also been quietly running back-channel communications toward Draught for eighteen months — not negotiations exactly, but the infrastructure for negotiations — without the Morvic council''s knowledge. She has run the numbers. She does not believe Pallor can survive another channel breach at the current state of the coastal defenses.',
        N'Gwyneira is a pragmatist who has discovered that pragmatism taken to its logical end requires a kind of deception she did not plan to engage in: working toward an outcome she believes is necessary for the people she represents, in ways those people would not authorize.',
        N'No POV.',
        N'House Pallor; Morvic coastal estates; council liaison offices',
        166, 63, N'average',
        N'dark auburn', N'loosely braided', N'long',
        N'grey-green', N'medium warm', N'weathered, sea-touched',
        N'none',
        N'Practical and direct; moves with the efficiency of someone managing more than one thing at all times',
        N'Estate working dress, Morvic coastal pattern; functional and weather-adapted; council dress kept separate for formal occasions',
        N'none',
        N'Estate management in the morning. Council correspondence and liaison meetings. In the afternoons she works through the back-channel communication infrastructure she has been quietly building toward Draught contacts through Atrament merchants who have no idea what they are carrying.',
        N'She believes another major Draught breach will destroy the Morvic coastal settlements, which have not recovered from the second incursion. The Morvic council would not authorize her to explore a diplomatic channel with Draught — the community''s historical grievances run too deep. She has done it anyway, without telling them, because she thinks the alternative is watching her people''s coastline destroyed a second time. If she succeeds, she becomes a visionary who saved her community. If she fails, she is a traitor who negotiated behind the council''s back. She has made this calculation and accepted the risk. She has not told anyone.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Morvic coastal estates; council chambers; back-channel communications through continental merchant contacts',
        N'0', N'0',
        N'Welsh/Cornish-looking woman in practical Morvic coastal estate dress, dark auburn loosely braided hair, grey-green eyes, efficient practical bearing, coastal estate, medieval fantasy --ar 2:3',
        N'A 43-year-old estate manager in practical Morvic coastal dress, auburn hair, efficient expression, medieval coastal estate',
        0, 0
    );
    PRINT 'Gwyneira Lorne seeded.';
END
ELSE PRINT 'Gwyneira Lorne already exists.';
GO

-- 33. Cador Pellam
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Cador Pellam')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Cador Pellam', N'cador-pellam', N'canon', 1,
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
        @id, N'Cador Pellam', N'cador-pellam', N'Cador', N'Pellam', N'',
        N'human', N'human', N'male', N'he/him', 58, N'alive',
        N'Anglic land overseer; manages farmstead allocation and resource distribution across the eastern lowlands',
        N'Cador Pellam has been overseeing the eastern lowlands'' grain allocation for eighteen years. He considers himself a careful steward of Anglic heritage and applies this conviction to his administrative decisions in ways that are too consistent and too small to constitute formal discrimination but too systematic to be accidental. Kellian farmsteaders in his territory receive allocations that are five to eight percent below Anglic-equivalent holdings. Not dramatically — just enough that the pattern is visible to the people experiencing it and deniable to anyone reviewing the numbers without the right frame of reference.',
        N'Cador is the quieter form of institutional prejudice: a man who would genuinely deny bias if confronted, who has written it into a decade and a half of administrative decisions small enough that none of them alone proves anything.',
        N'No POV.',
        N'House Pallor; eastern lowlands; Anglic farming territory',
        179, 86, N'stocky',
        N'grey-brown', N'plain, side-parted', N'short',
        N'pale blue', N'fair', N'weathered, outdoor-rough',
        N'none',
        N'Solid and proprietorial; the bearing of a man who considers himself the rightful custodian of the land he administers',
        N'Working overseer''s dress, practical and weather-worn; nothing ceremonial',
        N'none',
        N'Rides the allocation circuit through the eastern farmsteads. Reviews supply requests. Approves or adjusts requisitions. Attends council sessions when lowland allocation is on the agenda. His son Cai occasionally visits from the harbor and does not ask about the work.',
        N'He does not believe he is discriminating. He believes he is managing resources with appropriate attention to which communities have demonstrated reliable productive yields, and he has convinced himself over eighteen years that the allocation patterns reflect legitimate agricultural assessment. He has never had the conversation that would require him to examine this belief from outside himself. If someone showed him the aggregate numbers across eighteen years and asked him to explain the Anglic-Kellian differential, he would find an explanation for each individual decision that avoided the pattern. He might even believe it.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Eastern lowlands farmsteading territory; Anglic council liaison',
        N'0', N'0',
        N'Stocky older English-looking overseer in working outdoor dress, grey-brown hair, proprietorial bearing, eastern lowland farmstead, medieval fantasy --ar 2:3',
        N'A 58-year-old land overseer in working outdoor dress, grey-brown hair, proprietorial expression, medieval farmstead',
        0, 0
    );
    PRINT 'Cador Pellam seeded.';
END
ELSE PRINT 'Cador Pellam already exists.';
GO

-- 34. Brennan Keld
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Brennan Keld')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Brennan Keld', N'brennan-keld', N'canon', 1,
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
        @id, N'Brennan Keld', N'brennan-keld', N'Brennan', N'Keld', N'',
        N'human', N'human', N'male', N'he/him', 71, N'alive',
        N'House Pallor''s oldest living historian; author of the definitive account of the channel breaches',
        N'Brennan Keld is a Kellian man who has spent fifty years writing the history of House Pallor''s military engagements, and whose three-volume account of the channel breaches is the authoritative text used in garrison training and council briefings. He is scrupulous, thorough, and has been carrying a lie since the second breach, when a Lord''s aide paid him to omit what actually happened to the Morvic coastal militia: they were sacrificed as a deliberate tactical decoy, not overwhelmed as the record states. He wrote what he was told. He has been waiting for someone to ask the right question for forty years.',
        N'Brennan is the historian who falsified the record and knows exactly what that costs — not in career terms, but in the specific weight of watching forty years of people teach and learn a version of events that is wrong in a way that still matters.',
        N'No POV.',
        N'House Pallor; island capital archive; Kellian heritage',
        168, 67, N'lean, age-stooped',
        N'white', N'thin, unkempt', N'short',
        N'faded green', N'medium warm', N'deeply aged, spotted',
        N'none',
        N'Slow and careful; moves with the deliberateness of a man conserving energy and also keeping his dignity',
        N'Scholar''s robes, worn and ink-stained; the specific dishevelment of someone whose priorities are not sartorial',
        N'none',
        N'Reads in the morning while his mind is clearest. Writes in the afternoon, currently working on a separate private manuscript. Receives the occasional scholarly visitor. Takes his meals slowly and eats more than he appears to.',
        N'The second channel breach''s Morvic coastal militia was not overwhelmed. It was deliberately left exposed to draw Draught''s main assault force away from the Anglic shore. The Lord''s council made this decision. Brennan was the only independent witness whose account would have mattered. He was paid to write "overwhelmed by superior numbers" and he did so. The true account is written in a separate manuscript locked in a chest under his bed. He has intended for years to arrange for it to be released posthumously. He is seventy-one and has still not made that arrangement. He is running out of time to decide whether he will.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Island capital archive; scholar''s quarters; occasional Kellian community visits',
        N'0', N'0',
        N'Old Irish-looking historian in worn ink-stained scholar''s robes, thin white hair, faded green eyes, slow careful bearing, stone archive library, medieval fantasy --ar 2:3',
        N'A 71-year-old historian in ink-stained robes, thin white hair, aged thoughtful face, medieval stone archive library',
        0, 0
    );
    PRINT 'Brennan Keld seeded.';
END
ELSE PRINT 'Brennan Keld already exists.';
GO

-- 35. Anwen Sylve
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Anwen Sylve')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Anwen Sylve', N'anwen-sylve', N'canon', 1,
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
        @id, N'Anwen Sylve', N'anwen-sylve', N'Anwen', N'Sylve', N'',
        N'human', N'human', N'female', N'she/her', 36, N'alive',
        N'Linguist and scholar; studies language patterns of Sphere 31 persons in Pallor service',
        N'Anwen Sylve is a Kellian linguist attached to the island''s administrative archive, commissioned to study the language acquisition patterns of Sphere 31 persons entering Cauld service — officially, to improve intake efficiency. She is genuinely fascinated by the work. She has identified twelve distinct Sphere 31 linguistic origins in the current Pallor service population, which is more than the Liturgy''s extraction records account for. She has also identified something the commission did not anticipate: a Sphere 31 woman in house domestic service who is encoding text in her needlework, in a Sphere 31 writing system Anwen is still learning to read.',
        N'Anwen has stumbled into a secret that may be nothing — homesickness expressed through craft — or may be something the House and Liturgy would both want to know about. She cannot determine which without finishing the decipherment, and she cannot finish the decipherment without deciding who she will tell when she does.',
        N'No POV.',
        N'House Pallor; island capital archive; Sphere 31 intake facilities',
        163, 59, N'lean',
        N'dark brown', N'loose, pinned partially', N'long',
        N'brown', N'warm medium', N'clear',
        N'none',
        N'Attentive and precise; a scholar''s bearing — head slightly forward, watching for the thing she has not yet seen',
        N'Scholar''s working clothes, practical; ink on her fingers as a permanent condition',
        N'none',
        N'Morning: reviewing Sphere 31 language acquisition records and conducting intake interviews. Afternoon: her own linguistic research, currently dominated by the needlework decipherment. She has acquired five pieces of the house servant''s work through legitimate channels and is working through the script systematically.',
        N'She has identified a domestic servant named Mira who has been encoding text in stitchwork using a Sphere 31 writing system. Anwen has acquired five embroidered pieces under the pretext of a crafts-pattern study. She has deciphered about forty percent of the script so far. What she can read appears to be a record of names, dates, and short observations — possibly a personal journal, possibly something else. She has not told the House or the Liturgy because she does not yet know if Mira is a person writing her memories into cloth for comfort, or a person communicating with someone she has not identified. She wants to finish reading before she decides.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Island capital archive; Sphere 31 intake facility; house estates where Sphere 31 persons are in service',
        N'0', N'0',
        N'Welsh-looking woman scholar in practical working clothes, dark brown loosely pinned hair, attentive forward bearing, stone archive with linguistic materials, medieval fantasy --ar 2:3',
        N'A 36-year-old woman linguist in practical clothes, dark brown hair, attentive expression, at a medieval stone archive desk',
        0, 0
    );
    PRINT 'Anwen Sylve seeded.';
END
ELSE PRINT 'Anwen Sylve already exists.';
GO

-- 36. Corentin Vael
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Corentin Vael')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Corentin Vael', N'corentin-vael', N'canon', 1,
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
        @id, N'Corentin Vael', N'corentin-vael', N'Corentin', N'Vael', N'',
        N'human', N'human', N'male', N'he/him', 49, N'alive',
        N'House Pallor archivist; keeper of council records and the island''s population census',
        N'Corentin Vael keeps the official records of House Pallor''s three-people council — minutes, census, allocation registers, and the documentation of formal decisions. He is a Morvic man who has held this position for seventeen years and who is constitutionally incapable of letting an inconsistency go unexamined, which is an excellent quality in an archivist and a complicated one in a man who has found three census families that do not appear to exist in any physical form. They are in the records. They receive grain allocation. No one has confirmed them in person for twelve years. The collection is traced to a senior Kellian council aide.',
        N'Corentin has found a thread that leads somewhere he is not sure he wants to go: a small fraud that may be a minor convenience operation, or may be the visible edge of something much larger, and he does not know which until he pulls.',
        N'No POV.',
        N'House Pallor; island capital council archive; three-people census records',
        174, 75, N'average',
        N'dark brown going grey', N'neatly kept', N'short',
        N'grey', N'medium warm', N'clear, indoor-pale',
        N'none',
        N'Precise and organized; the unhurried authority of a man who knows where everything is and what it means',
        N'Archivist''s formal robes, dark and clean; the council record seal at his breast',
        N'none',
        N'Morning: reviews overnight correspondence and updates the active record indices. Council session documentation when in session. Afternoon: his private investigation of the three ghost families, which he has been pursuing through collateral records for four months without accessing the main files where it would leave a trace.',
        N'Three families in the Anglic census district have been receiving grain allocation for twelve years without a single confirmed in-person household visit. The collection route for their allocation traces through a Kellian council aide''s household factoring account. Corentin has not yet confirmed whether the aide is collecting on behalf of real people in hiding, or appropriating the allocation directly. He is working backward through twelve years of records to determine the answer. He has not told anyone because his instinct is to understand before he acts, and because the aide in question is close to a council member who could have him reassigned.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Island capital council archive; census district records across all three peoples'' territories',
        N'0', N'0',
        N'Middle-aged Breton/Cornish-looking archivist in dark formal robes, dark brown-grey hair, precise organized bearing, stone council archive with ledgers, medieval fantasy --ar 2:3',
        N'A 49-year-old archivist in dark robes, dark brown-grey hair, precise expression, medieval stone council archive',
        0, 0
    );
    PRINT 'Corentin Vael seeded.';
END
ELSE PRINT 'Corentin Vael already exists.';
GO

-- 37. Tessaly Fen
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Tessaly Fen')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Tessaly Fen', N'tessaly-fen', N'canon', 1,
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
        @id, N'Tessaly Fen', N'tessaly-fen', N'Tessaly', N'Fen', N'',
        N'human', N'human', N'female', N'she/her', 44, N'alive',
        N'Ship merchant; licensed trader between Pallor and continental ports; intelligence conduit for Atrament',
        N'Tessaly Fen is an Anglic-born woman with an Atrament grandmother who has been running licensed cargo routes between the island and the continent for fourteen years. She is a successful merchant, a genuine Pallor citizen who prefers living on the island, and an Atrament intelligence asset who has been selling ship-movement and garrison-rotation observations for eight years. She does not consider herself a spy. She considers herself a businessperson who has found a secondary market for observations she makes anyway. The contradiction between her genuine Pallor preference and her ongoing Atrament relationship does not trouble her, which is itself a piece of information.',
        N'Tessaly is a mirror for the question of what loyalty means when a person''s identity is genuinely divided: she is not lying about loving her home, and she is not lying about selling its movements. Both can be true, which is what makes her dangerous.',
        N'No POV.',
        N'House Pallor; harbor district; continental trade routes; Atrament contact points',
        167, 64, N'lean',
        N'brown-gold', N'practical, loose', N'medium',
        N'light brown', N'warm fair', N'clear, lightly sun-touched',
        N'none',
        N'Easy and commercially confident; reads rooms and people with the specific attention of someone whose income depends on it',
        N'Merchant''s practical dress, well-made without ostentation; continental cut in the collar that marks her trade experience',
        N'none',
        N'Cargo arrangements and route planning in the morning. Meetings with buyers and factors. Sailing days she works the ship herself. Her Atrament observations are compiled during transit: ship counts, patrol timing, garrison flags she notes from the channel.',
        N'She has been an Atrament asset for eight years. She routes her observations through a factor in the Atrament trading quarter using a commercial code that reads as pricing dispute shorthand. She also carries letters for the Myrmidon Tristan Mere, whose correspondence with an Atrament soldier she handles without knowing the sender''s identity — she only knows the route. She genuinely does not consider this treasonous, which is either a very sophisticated or a very simple position, and she is not sure which.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Pallor harbor district; channel trade routes; continental Atrament ports',
        N'0', N'0',
        N'Anglo-French-looking woman merchant in well-made practical dress, brown-gold hair loose, easy commercial confidence, medieval harbor dock, Buehlman register --ar 2:3',
        N'A 44-year-old woman merchant in practical dress, brown-gold hair, confident expression, medieval harbor dock setting',
        0, 0
    );
    PRINT 'Tessaly Fen seeded.';
END
ELSE PRINT 'Tessaly Fen already exists.';
GO

-- 38. Huw Porrow
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Huw Porrow')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Huw Porrow', N'huw-porrow', N'canon', 1,
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
        @id, N'Huw Porrow', N'huw-porrow', N'Huw', N'Porrow', N'',
        N'human', N'human', N'male', N'he/him', 52, N'alive',
        N'Kellian wool and dye merchant; coastal trade; covert Oathless financial support',
        N'Huw Porrow is a successful Kellian merchant who has been trading wool and dye along the coastal route for twenty-two years. He is honest in his business dealings, respected by the harbor trade community, and has been covertly funding Oathless bands in the hill country for nine years because his nephew Gerren was cast out and Huw believes Gerren is innocent of whatever he was charged with. He does not ask what the Oathless bands are moving through the logistics contacts he provides. He considers himself a supporter, not a participant.',
        N'Huw is the comfortable moral position of the enabler who has defined his support narrowly enough to avoid seeing what it funds — a man who loves his nephew and has decided that loving him is enough justification for everything else.',
        N'No POV.',
        N'House Pallor; Kellian coast trading district; hill country contact routes',
        175, 84, N'stocky',
        N'dark grey', N'short, receding', N'short',
        N'brown', N'warm medium', N'weathered, outdoor-worked',
        N'none',
        N'Solid and direct; the commercial confidence of a man who has been doing the same work for twenty years and is good at it',
        N'Merchant''s working dress, practical and weather-adapted; Kellian coastal pattern at the collar',
        N'none',
        N'Wool and dye cargo management. Trade negotiations with coastal buyers. Monthly he routes a portion of his commercial revenue into payments that reach the hill country Oathless network through a series of commercial intermediaries. He has not seen Gerren in six years.',
        N'He believes Gerren was cast out for a refusal to participate in something the garrison was doing that was wrong — he does not know the specifics, which is deliberate. He has been funding the Oathless network for nine years. The network has grown from a small group sheltering his nephew into something larger and more organized that uses his logistics contacts for purposes he does not ask about. He knows the contacts are being used to move things that are not wool. He has not asked what they are. He has decided this is the correct level of involvement. He is wrong about what the correct level is.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Kellian coastal trading district; harbor route; hill country contact network',
        N'0', N'0',
        N'Stocky middle-aged Welsh-looking merchant in Kellian coastal working dress, dark grey hair, solid direct bearing, coastal harbor trade setting, medieval fantasy --ar 2:3',
        N'A 52-year-old Kellian merchant in coastal working dress, dark grey hair, solid direct expression, medieval harbor',
        0, 0
    );
    PRINT 'Huw Porrow seeded.';
END
ELSE PRINT 'Huw Porrow already exists.';
GO

-- 39. Carys Denn
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Carys Denn')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Carys Denn', N'carys-denn', N'canon', 1,
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
        @id, N'Carys Denn', N'carys-denn', N'Carys', N'Denn', N'',
        N'human', N'human', N'female', N'she/her', 31, N'alive',
        N'Merchant; coastal and continental trade; accidental holder of Liturgy extraction manifests',
        N'Carys Denn is a young merchant who expanded from coastal trade to continental routes eighteen months ago, which is when her problems started. She agreed to carry a crate for a factor at the Atrament trading quarter — a commercial arrangement she has made dozens of times. She opened the crate because it shifted wrong during loading. Inside: a set of Liturgy documents listing names of Sphere 31 persons earmarked for "private placement" rather than standard House assignment. She was supposed to deliver these to the Atrament quarter. She did not. She has the manifests. She does not know who to give them to.',
        N'Carys is an ordinary person who has stumbled into the operational heart of something institutional and dangerous, with no context for understanding what she has, and no clear path to safety regardless of what she does with it.',
        N'No POV.',
        N'House Pallor; harbor district; coastal and continental trade routes',
        165, 61, N'lean',
        N'red-brown', N'practical, tied back', N'medium',
        N'grey-green', N'fair-medium', N'clear',
        N'none',
        N'Energetic and commercially oriented; currently carrying an anxiety she has not told anyone about that has made her more careful and less profitable',
        N'Merchant''s practical dress; the working clothes of someone who loads her own cargo',
        N'none',
        N'Cargo management and trade negotiations, conducted with slightly less confidence than six months ago. She has been declining the Atrament quarter factor''s work since the crate incident without offering an explanation. The manifests are in a sealed case in her dock storage.',
        N'She has a set of Liturgy extraction manifests that appear to list sixty-eight Sphere 31 persons placed with private buyers rather than in standard House service. The manifests include names, physical descriptions, and destination notations that do not correspond to any House or garrison assignment she recognizes. She does not know what "private placement" means in the Liturgy''s operational terminology, but she understands that the documents were being transported secretly and that someone will eventually notice she did not deliver them. She has considered destroying them. She cannot bring herself to do it.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Harbor district; coastal trade routes; continental Atrament ports',
        N'0', N'0',
        N'Young Welsh-looking woman merchant in practical dock dress, red-brown tied back hair, grey-green eyes, energetic but anxious bearing, medieval harbor dock --ar 2:3',
        N'A 31-year-old woman merchant in practical clothes, red-brown hair, slightly anxious expression, medieval harbor dock',
        0, 0
    );
    PRINT 'Carys Denn seeded.';
END
ELSE PRINT 'Carys Denn already exists.';
GO

-- 40. Mira Westfall
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Mira Westfall')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Mira Westfall', N'mira-westfall', N'canon', 1,
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
        @id, N'Mira Westfall', N'mira-westfall', N'Mira', N'Westfall', N'',
        N'human', N'human', N'female', N'she/her', 39, N'alive',
        N'House Pallor intelligence operative; arrived from Sphere 31 nineteen years ago; fully integrated',
        N'Mira Westfall came from Sphere 31 at twenty, taken by a Liturgy extraction team and recognized by an intelligence officer who saw potential in someone who understood Sphere 31 patterns from the inside. She has been an active Pallor operative for fifteen years. She is effective, trusted, and completely integrated into Cauld life — she thinks in the Cauld''s language, dreams in it, and has not thought of herself as a Sphere 31 person in a decade. What she has not told her handlers is that she has identified three other Sphere 31 persons in Pallor''s court who arrived through channels that are not in the Liturgy''s extraction records.',
        N'Mira is the person who crossed the membrane and made it — and who is now holding information about other crossings that someone she cannot identify arranged, which raises questions about the membrane she thought she understood.',
        N'No POV.',
        N'House Pallor; intelligence network; island capital',
        162, 58, N'athletic-lean',
        N'dark brown', N'practical, medium length', N'medium',
        N'dark brown', N'warm medium-dark', N'clear',
        N'none',
        N'Alert and adaptive; the specific quality of someone who learned a new world in adulthood and has been performing fluency ever since',
        N'Practical civilian clothes for most operations; adapts dress to operational context with professional ease',
        N'none',
        N'Receives and processes intelligence assignments from Cormac Trevise''s office. Conducts surveillance and contact operations across the island and occasionally the continent. In her personal time, she has been quietly mapping the three unregistered Sphere 31 persons she has identified, trying to determine how they arrived and who placed them.',
        N'She has identified three people in Pallor''s administrative court and council staff who exhibit behavioral patterns she recognizes from Sphere 31 — patterns she would not expect from someone who had been in the Cauld since childhood. She has verified this through careful indirect contact: each of them has responded to specific Sphere 31 cultural references in ways that confirm her assessment. They did not come through the Liturgy. Someone brought them through a different channel. She does not know who or why, and she has not told Cormac Trevise because she is not sure whose asset they are.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Island capital and surrounding territory; intelligence contact network; Sphere 31 community access',
        N'0', N'0',
        N'Mixed-heritage woman in practical civilian clothes, dark brown hair, dark eyes, alert adaptive bearing, medieval island capital street scene, Buehlman dark register --ar 2:3',
        N'A 39-year-old intelligence operative in practical clothes, dark brown hair, alert watchful expression, medieval stone street',
        0, 0
    );
    PRINT 'Mira Westfall seeded.';
END
ELSE PRINT 'Mira Westfall already exists.';
GO

-- 41. Kevan Drust
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Kevan Drust')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Kevan Drust', N'kevan-drust', N'canon', 1,
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
        @id, N'Kevan Drust', N'kevan-drust', N'Kevan', N'Drust', N'',
        N'human', N'human', N'male', N'he/him', 44, N'alive',
        N'Liaison officer; ostensibly House Pallor''s representative at Atrament trading posts; triple-run double agent',
        N'Kevan Drust reports to House Pallor''s intelligence office as a liaison officer. He also reports to Atrament as a recruited asset. Both institutions believe they own him. A third party — he has never identified them, receives instructions only through a cut-out that changes every six months — has been running him for two years with a precision that suggests they know exactly what the other two are asking him for. The third party''s instructions have always arrived with what appears to be current knowledge of his other assignments. This is more frightening to him than anything else about his situation.',
        N'Kevan is a man who has discovered he is not the most sophisticated actor in the room he built around himself — someone has been managing him the way he manages the institutions he reports to, and he cannot find them.',
        N'No POV.',
        N'House Pallor origin; Atrament trading posts; cut-out contact points',
        178, 80, N'average',
        N'dark blond', N'professionally neat', N'short',
        N'blue-grey', N'fair', N'clear, controlled',
        N'none',
        N'Professional and unreadable; projects calm as a deliberate tool; does not relax',
        N'Liaison officer''s formal dress; presentable in both Pallor and Atrament registers',
        N'none',
        N'Liaison meetings with Atrament trade officials. Dispatches to Pallor''s intelligence office. The third party''s instructions arrive through a specific commercial channel he has been asked not to examine. He follows them. He is working on identifying the third party through pattern analysis of the cut-out changes and has not succeeded in two years.',
        N'Someone has been running him for two years and knows details about his other assignments that they should not be able to know. This person — or organization — has never asked him to do anything that overtly damaged Pallor or Atrament. Their instructions have invariably positioned him to be maximally useful to both his other handlers. He cannot determine whether the third party is protecting him, using him, or conducting an experiment he is not meant to understand. He is sleeping less. He has not told either Pallor or Atrament because he cannot explain a third handler without also explaining that he has been running both of them simultaneously.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Pallor intelligence office; Atrament trading posts; cut-out contact network',
        N'0', N'0',
        N'Professional-looking English-Celtic man in formal liaison dress, dark blond hair, controlled unreadable expression, medieval diplomatic meeting room, Buehlman register --ar 2:3',
        N'A 44-year-old liaison officer in formal dress, dark blond hair, controlled expression, medieval stone diplomatic chamber',
        0, 0
    );
    PRINT 'Kevan Drust seeded.';
END
ELSE PRINT 'Kevan Drust already exists.';
GO

-- 42. Saoirse Morrin
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Saoirse Morrin')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Saoirse Morrin', N'saoirse-morrin', N'canon', 1,
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
        @id, N'Saoirse Morrin', N'saoirse-morrin', N'Saoirse', N'Morrin', N'Dame',
        N'human', N'human', N'female', N'she/her', 35, N'alive',
        N'Knight; House Pallor intelligence field operative; continental extraction and contact specialist',
        N'Saoirse Morrin received her infusion at twenty-nine and has been a field operative for the House''s intelligence apparatus ever since. She is very good at the work — good enough that Cormac Trevise considers her his most reliable continental asset. Her most recent operation required her to develop a relationship with a Draught military attaché for intelligence purposes, which she did. She also killed him on departure, which was not in the operation order. She reported it as self-defense. The attaché was about to tell her something substantive about Draught''s actual strategic intentions when she realized she had become too attached to the process and killed him before she heard what he had to say.',
        N'Saoirse is a person who made a professional decision that was also a self-protective one, and who is living with the possibility that she terminated the most important intelligence she might ever have gathered because she could not manage her own response to the source.',
        N'No POV.',
        N'House Pallor; island intelligence office; continental field operations',
        173, 70, N'athletic',
        N'dark red', N'close-cropped', N'short',
        N'green', N'fair-medium', N'clear, deliberately unremarkable',
        N'Subtle height gain; increased skeletal density visible in jaw and brow; vascular prominence on forearms',
        N'Economical and professionally adaptive; presents different bearing in different operational contexts; her baseline is controlled and alert',
        N'Operational civilian dress adapted to context; nothing that marks House origin when working',
        N'Knight — first infusion at twenty-nine; modest enhancement',
        N'Currently between operations; in island capital for debrief and reassignment. Submits accurate operational reports. Does not mention the question she silenced. Works out in the garrison training yard at dawn.',
        N'She killed the Draught attaché nine seconds before he would have answered a direct question about Draught''s territorial intentions in the channel. She knows this with precision because she has replayed the conversation many times. She killed him because the relationship had become something she was no longer in control of, and she responded to that loss of control with the only definitive action available. She does not know what he would have said. She knows it was operationally significant. She has told Cormac it was self-defense, which is technically accurate and substantively false.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Island capital; continental field operations; Draught border territory',
        N'0', N'0',
        N'Irish-looking woman knight in operational civilian dress, close-cropped dark red hair, controlled alert expression, medieval continental setting, Buehlman dark register --ar 2:3',
        N'A 35-year-old woman knight in civilian dress, close-cropped dark red hair, controlled expression, medieval stone setting',
        0, 0
    );
    PRINT 'Saoirse Morrin seeded.';
END
ELSE PRINT 'Saoirse Morrin already exists.';
GO

-- 43. Fergal Carne
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Fergal Carne')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Fergal Carne', N'fergal-carne', N'canon', 1,
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
        @id, N'Fergal Carne', N'fergal-carne', N'Fergal', N'Carne', N'',
        N'human', N'human', N'male', N'he/him', 47, N'alive',
        N'Knight; House Pallor counter-intelligence; identifies and manages internal security threats',
        N'Fergal Carne received his infusion at thirty-six, late for a Knight, which is a reflection of how long it took the House to decide he was worth the Catalyst. He has been running Pallor''s internal counter-intelligence function for seven years and has developed a methodology he considers elegant: he feeds specific false information to people he suspects of informing and watches where it lands. Two of his most recent test subjects died in ways that suggest the false information was acted on against them. He has been sitting with this for three months.',
        N'Fergal is a man who has conducted what he can only call a lethal experiment using people as instruments, and who is now trying to determine whether the intelligence value of what he learned justifies the method by which he obtained it — which is not a question that has a clean answer.',
        N'No POV.',
        N'House Pallor; island capital intelligence office; counter-intelligence network',
        185, 94, N'athletic',
        N'iron grey', N'short, plain', N'short',
        N'pale grey', N'fair-medium', N'weathered, controlled',
        N'Subtle height gain; increased skeletal density visible in jaw and brow; vascular prominence on forearms',
        N'Still and observing; the specific quality of someone whose professional instinct is to watch before acting',
        N'Intelligence service dress, dark and formal; nothing that marks his specific function',
        N'Knight — first infusion at thirty-six; modest enhancement',
        N'Reviews intelligence reports and runs his counter-intelligence assessment process. Manages a small team of watchers he has never formally introduced to each other. In the evenings he reviews the files of his two dead test subjects and the timeline of events following his false-feed operations.',
        N'He fed specific fabricated intelligence to two people he suspected were informers. Both died within three months — one in what appeared to be a targeted Oathless attack, one in what appeared to be an accident. The timings match the information''s likely distribution to hostile parties. He cannot be certain his information caused their deaths. He is very nearly certain. He has not stopped the program. He has started including what he thinks of as a graduated test — false information that might identify an informer without necessarily getting them killed. He is not sure this makes him better.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Island capital; counter-intelligence network across Pallor territory',
        N'0', N'0',
        N'Athletic older Irish-Celtic man in dark intelligence service dress, iron grey hair, pale grey eyes, still observing bearing, stone intelligence office, Buehlman dark register --ar 2:3',
        N'A 47-year-old counter-intelligence knight in dark service dress, iron grey hair, still watchful expression, medieval stone office',
        0, 0
    );
    PRINT 'Fergal Carne seeded.';
END
ELSE PRINT 'Fergal Carne already exists.';
GO

-- 44. Isa Pendryn
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Isa Pendryn')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Isa Pendryn', N'isa-pendryn', N'canon', 1,
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
        @id, N'Isa Pendryn', N'isa-pendryn', N'Isa', N'Pendryn', N'Dame',
        N'human', N'human', N'female', N'she/her', 78, N'alive',
        N'Retired Dame of House Pallor; council advisor; memory of three generations of conflict',
        N'Isa Pendryn received her infusion at nineteen — younger than the House now permits — and served forty years as a Knight before her body''s age finally outpaced the enhancement. She is still taller than most people in any room she enters and moves with care rather than speed. She was at the first channel breach. She is the only person still alive who was there. She saw things at the first breach that she has kept for sixty years, including one specific thing: the Draught commander gave an order to spare the Kellian fishing settlement, and his own officers overrode him. She has never told anyone because it complicates the story House Pallor tells about Draught.',
        N'Isa is the living refutation of the House''s foundational narrative about its primary enemy — a woman who knows Draught''s command culture is more complicated than the enemy it has been made into, and who has decided, at seventy-eight, that she may be running out of time to decide whether this matters.',
        N'No POV.',
        N'House Pallor; island capital, council advisory function; Anglic-Morvic heritage',
        175, 71, N'lean, age-stooped',
        N'white', N'pinned simply', N'short',
        N'pale grey', N'warm fair', N'deeply aged, dignified-lined',
        N'Subtle height gain; increased skeletal density visible in jaw and brow; vascular prominence reduced with age but still present',
        N'Slow and deliberate; stoops slightly at the shoulder but carries herself with the authority of someone who has earned the right not to straighten up',
        N'Retired Dame''s formal dress, dark; the Dame''s badge worn at the throat as she has worn it for sixty years',
        N'Knight — first infusion at nineteen; modest enhancement now overlaid by age',
        N'Attends council sessions when asked, which is rarely because her opinions are too long and too well-grounded to be politically convenient. Reads. Receives visitors. Writes correspondence in a hand that is still precise. Thinks about the Draught commander''s order.',
        N'She was seventeen meters from the Draught commander at the first channel breach when he signaled his officers to halt the advance on the Kellian settlement. His officers did not halt. She saw the countermand. She understood what it meant. She was nineteen years old and did not have the context to act on it. She is seventy-eight now and wonders if telling someone — if telling Lord Aldwyn — would change anything about the current conflict, or only complicate it. She has written three letters to Aldwyn that she did not send. The most recent is three weeks old.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Island capital; council chambers; personal quarters',
        N'0', N'0',
        N'Very old English-looking woman in dark Dame''s formal dress, white pinned hair, pale grey eyes, dignified slow bearing, stone council chamber, medieval fantasy, Buehlman register --ar 2:3',
        N'A 78-year-old retired Dame in dark formal dress, white hair, dignified aged expression, medieval stone council chamber',
        0, 0
    );
    PRINT 'Isa Pendryn seeded.';
END
ELSE PRINT 'Isa Pendryn already exists.';
GO

-- 45. Owain Mast
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Owain Mast')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Owain Mast', N'owain-mast', N'canon', 1,
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
        @id, N'Owain Mast', N'owain-mast', N'Owain', N'Mast', N'',
        N'human', N'human', N'male', N'he/him', 82, N'alive',
        N'The oldest surviving officer in House Pallor''s service; functionally retired; still draws garrison pay',
        N'Owain Mast has been on House Pallor''s garrison pay roll for fifty-eight years. He was a logistics officer — supply chain, provisioning, route planning — for his entire career, and he was extraordinarily good at it. He has commendations for tactical brilliance at four named engagements. Every commendation was for a supply line placement or a provisioning decision that starved an enemy position or sustained a friendly one. He has never killed anyone. He has never corrected the commendation citations, which describe his contributions in language more suited to a field commander.',
        N'Owain is the argument that institutional bureaucracy cannot read its own work: a man whose genuine strategic genius was entirely logistical, who has spent fifty years watching the House misattribute it to battlefield command.',
        N'No POV.',
        N'House Pallor; island capital, retired garrison officer''s quarters',
        174, 65, N'lean, greatly age-reduced',
        N'white', N'wispy, mostly gone', N'very short',
        N'pale blue', N'fair', N'ancient, transparent-thin',
        N'none',
        N'Very slow and deliberate; still navigates stairs without assistance, which he considers a point of personal satisfaction',
        N'Old officer''s dress, worn to a comfortable softness; the commendation badges stored in a case in his room rather than worn',
        N'none',
        N'Breakfasts in the garrison hall when the weather permits him to walk there. Receives occasional visitors from garrison command who ask his memory of old supply routes. Sleeps in the afternoons. Has refused a bed in the infirmary three separate times.',
        N'He was never a battlefield commander. Every commendation he received describes supply decisions as tactical initiatives — the language of the commendation citations was written by officers who did not fully understand what he had done and defaulted to martial language. He did not correct them. He has spent fifty years watching other officers defer to his "battlefield reputation" and say nothing because the deference is comfortable and he has never enjoyed explaining logistics to people who consider it beneath strategy. He knows that what he actually did — starving a Draught position through supply line interdiction at the second breach — was more decisive than anything that happened on the ground. He has told no one.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Island capital, retired garrison quarters; garrison hall on good days',
        N'0', N'0',
        N'Very old Welsh-looking man in soft worn officer''s dress, wispy white hair, pale blue eyes, extremely slow deliberate movement, stone garrison hall, medieval fantasy --ar 2:3',
        N'An 82-year-old retired officer in soft worn dress, nearly gone white hair, ancient dignified expression, medieval stone garrison hall',
        0, 0
    );
    PRINT 'Owain Mast seeded.';
END
ELSE PRINT 'Owain Mast already exists.';
GO

-- 46. Rhiannon Clour
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Rhiannon Clour')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Rhiannon Clour', N'rhiannon-clour', N'canon', 1,
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
        @id, N'Rhiannon Clour', N'rhiannon-clour', N'Rhiannon', N'Clour', N'',
        N'human', N'human', N'female', N'she/her', 68, N'alive',
        N'Retired Scrying operator; now teaches younger operators at the island installation',
        N'Rhiannon Clour operated the island''s primary Scrying installation for twenty-eight years before her vision degraded enough to make precision observation unreliable. She now teaches. She is a good teacher — clear, patient, technically precise — and she has been quietly directing her students to look for a specific set of observable characteristics in their Sphere observations without explaining what they are looking for or why. Fifty years ago she saw something through the membrane that she has never found again. She does not know if it was real.',
        N'Rhiannon is a woman who has spent fifty years trying to verify a single observation that would change everything if it were real, using the only tools she has access to — younger eyes pointed at the membrane with instructions she cannot fully explain.',
        N'No POV.',
        N'House Pallor; island Scrying installation; teaching facility',
        163, 66, N'average, age-settled',
        N'white', N'loosely pinned', N'medium',
        N'faded blue', N'fair', N'aged, fine-wrinkled',
        N'none',
        N'Still and attentive; the patience of a teacher who understands that what she is waiting for may not come',
        N'Retired operator''s clothing, practical; the teacher''s sash of the installation school',
        N'none',
        N'Morning: teaching sessions with the junior operators. Afternoon: supervision of advanced students during observation sessions. She positions herself behind each student during their sessions and watches their faces as much as the apparatus.',
        N'Fifty years ago, during her second year as an operator, she observed a Sphere in which the Living War appeared to have ended — all seven Houses in council together, the Scrying apparatus under collective management. She observed it for twenty minutes before it closed. She has spent fifty years looking for it and directing her students to report any observation that matches a set of observable characteristics she has encoded as an "unusual membrane signature." She does not know if she saw a real Sphere, a hallucination from equipment exposure, or something the membrane does on rare occasions that the Liturgy does not catalogue. She is sixty-eight and running out of time.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Island Scrying installation; teaching facility; rarely travels',
        N'0', N'0',
        N'Older Welsh-looking woman in practical operator clothing with teacher''s sash, white loosely pinned hair, faded blue eyes, patient attentive bearing, stone Scrying installation, medieval fantasy --ar 2:3',
        N'A 68-year-old retired Scrying operator in practical clothes, white hair, patient expression, medieval stone Scrying installation',
        0, 0
    );
    PRINT 'Rhiannon Clour seeded.';
END
ELSE PRINT 'Rhiannon Clour already exists.';
GO

-- 47. Aldith Stow
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Aldith Stow')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Aldith Stow', N'aldith-stow', N'canon', 1,
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
        @id, N'Aldith Stow', N'aldith-stow', N'Aldith', N'Stow', N'',
        N'human', N'human', N'female', N'she/her', 18, N'alive',
        N'Newest garrison conscript; channel watch rotation, signal post support; daughter of Gruffydd Stow',
        N'Aldith Stow arrived at the channel garrison four months ago and has been assigned to signal post support — carrying messages and supplies between the watch points, which requires no reading. She is the daughter of the garrison armsmaker Gruffydd Stow and has a warm relationship with her father that neither of them would describe as warm. She cannot read. She has concealed this through careful attention, selective memory, and a bunkmate who traces her signature form for her in exchange for her watch rotation shifts. If discovered, she would be reassigned from garrison to manual labor corps. She is more afraid of the reassignment than of a Draught probe.',
        N'Aldith is the smallest possible version of institutional survival under disability: a young woman who has constructed an elaborate accommodation for a gap the institution cannot acknowledge, in order to stay in the only place she wants to be.',
        N'No POV.',
        N'House Pallor; channel garrison, signal post rotation',
        158, 52, N'lean',
        N'light brown', N'plain, braided back', N'medium',
        N'hazel', N'fair', N'clear, young, slightly wind-chapped',
        N'none',
        N'Energetic and task-oriented; stays busy because busyness reduces the number of reading-adjacent situations',
        N'Standard garrison wool; her father''s adjustments to the fit, which she has not told anyone about',
        N'none',
        N'Signal post support runs. Carrying dispatches and supplies. She has learned every route by landmark and timing. She eats in the garrison hall without sitting near anyone who reads documents aloud. She trades watch shifts for signature assistance with practiced ease.',
        N'She cannot read. She has concealed this from every institutional structure she has encountered since the age of twelve by memorizing documents read aloud to her, having her bunkmate trace her signature, and navigating the garrison''s written requirements by route and timing rather than text. Her father knows she struggles but not the full extent — he has adjusted her garrison kit to compensate for physical fit without asking why she asked. She is more afraid of being sent to the manual labor corps than of anything the channel might produce. She would like to stay.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Channel garrison; signal post rotation; supply routes',
        N'0', N'0',
        N'Young English-looking woman conscript in garrison wool, light brown braided hair, hazel eyes, energetic task-focused bearing, channel garrison, medieval fantasy --ar 2:3',
        N'An 18-year-old woman conscript in garrison wool, light brown braided hair, energetic expression, medieval channel garrison',
        0, 0
    );
    PRINT 'Aldith Stow seeded.';
END
ELSE PRINT 'Aldith Stow already exists.';
GO

-- 48. Pasco Tren
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Pasco Tren')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Pasco Tren', N'pasco-tren', N'canon', 1,
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
        @id, N'Pasco Tren', N'pasco-tren', N'Pasco', N'Tren', N'',
        N'human', N'human', N'male', N'he/him', 20, N'alive',
        N'Myrmidon; second year of service; channel garrison, Morvic background',
        N'Pasco Tren is a Morvic-born soldier in his second year of garrison service, assigned to the channel watch. He is unremarkable in his performance and popular enough in the garrison hall to avoid scrutiny. He practices the Morvic coastal observances — specific forms of prayer to Bheur, specific dietary restrictions during the dark months — in private and as quietly as he can manage, because mixed garrisons tolerate the practices without formally discouraging them, which in practice means they discourage them informally. His superior has noticed. He does not know this.',
        N'Pasco is the ground-level experience of belonging to a minority culture inside an institution that technically accepts you while practically eroding what makes you different.',
        N'No POV.',
        N'House Pallor; channel garrison, Morvic community connections',
        171, 69, N'lean',
        N'dark brown', N'short, plain', N'short',
        N'brown', N'medium warm', N'clear',
        N'none',
        N'Unremarkable and collegial in public; careful in private; the specific quality of someone managing a visibility that goes in two directions',
        N'Standard garrison wool; Morvic prayer cord worn inside his shirt',
        N'none',
        N'Garrison watch rotations. Eats in the hall with the unit. Participates enough in garrison social life to avoid standing out. In the early mornings, before the hall fills, he performs the Morvic observances in a corner of the garrison yard that faces east.',
        N'His superior Sergeant Aldrac has been watching Pasco''s Morvic observances for three weeks. Aldrac has not reported it because he is waiting for a moment when reporting it would be useful to him personally — either to demonstrate his vigilance to the command chain, or to leverage against Pasco for a specific request. Pasco does not know he is being watched. He is performing his observances in what he has assessed as the lowest-visibility window available to him in the garrison schedule. His assessment is correct. It did not account for Aldrac changing his morning routine.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Channel garrison; Morvic community in the nearest coastal town',
        N'0', N'0',
        N'Young Cornish/Morvic-looking soldier in garrison wool, dark brown hair, unremarkable careful expression, early morning garrison yard, medieval fantasy --ar 2:3',
        N'A 20-year-old Morvic soldier in garrison wool, dark brown hair, careful expression, early morning medieval garrison yard',
        0, 0
    );
    PRINT 'Pasco Tren seeded.';
END
ELSE PRINT 'Pasco Tren already exists.';
GO

-- 49. Morwenna Caul
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Morwenna Caul')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Morwenna Caul', N'morwenna-caul', N'canon', 1,
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
        @id, N'Morwenna Caul', N'morwenna-caul', N'Morwenna', N'Caul', N'',
        N'human', N'human', N'female', N'she/her', 23, N'alive',
        N'Garrison medic; Morvic traditional training; authorized for standard wound care',
        N'Morwenna Caul is a Morvic-trained healer working as a garrison medic, one of three at the channel fortification. She is competent in standard wound care and has been licensed to practice by the House medical authority under the standard garrison protocol. She has also been administering a Morvic plant compound — a preparation used in the coastal communities for what is described as easing the transition for the very ill — to soldiers she believes will not survive their injuries. Three of these soldiers survived. She does not know whether the compound helped them or whether she misjudged their condition.',
        N'Morwenna is a healer operating at the edge of her authorization, using knowledge she was not licensed to apply, and who cannot determine whether she is helping or whether she is treating people with an unlicensed preparation based on a flawed assessment of their chances.',
        N'No POV.',
        N'House Pallor; channel garrison medical station; Morvic coastal community connection',
        160, 57, N'lean',
        N'dark brown', N'braided', N'long',
        N'dark brown', N'warm medium-dark', N'clear',
        N'none',
        N'Careful and attentive with patients; competent and calm under pressure; carries her herb kit everywhere',
        N'Medic''s practical working clothes; healer''s sash; Morvic herb pouches at the belt',
        N'none',
        N'Wound treatment and patient monitoring in the garrison medical station. The compound administrations happen in the evenings when the other medics are not present. She has administered it to nine patients. Three survived against her expectation. She is reviewing her condition assessments.',
        N'She has been administering an unlicensed Morvic preparation to dying soldiers. Three recovered who she assessed as beyond recovery. She cannot determine whether: (a) the compound has a genuine therapeutic effect she was not trained to understand, (b) her condition assessments were wrong and they would have survived anyway, or (c) something else is happening. She has not told the other medics or the garrison physician because the compound is not authorized and because if it works, the explanation of why she used it involves Morvic healing knowledge the House medical authority would need to formally recognize or formally prohibit, and she does not know which they would choose.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Channel garrison medical station; Morvic community; coastal herb suppliers',
        N'0', N'0',
        N'Young Cornish/Morvic woman in medic''s practical clothes with Morvic herb pouches, dark brown braided hair, attentive careful expression, stone garrison medical station, medieval fantasy --ar 2:3',
        N'A 23-year-old woman garrison medic in practical clothes with herb pouches, dark brown braided hair, careful expression, medieval stone medical station',
        0, 0
    );
    PRINT 'Morwenna Caul seeded.';
END
ELSE PRINT 'Morwenna Caul already exists.';
GO

-- 50. Tegan Wren
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Tegan Wren')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Tegan Wren', N'tegan-wren', N'canon', 1,
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
        @id, N'Tegan Wren', N'tegan-wren', N'Tegan', N'Wren', N'',
        N'human', N'human', N'male', N'he/him', 26, N'alive',
        N'Head cook; northern garrison; responsible for feeding six hundred soldiers',
        N'Tegan Wren became head cook of the northern garrison at twenty-three, which is young for the position and reflects the particular talent he has for managing a kitchen that scales to six hundred without losing the quality that keeps soldiers from complaining about their food. He has been making up the difference between the official requisition rate and the actual food cost from his own wages for three years, because the official rate is below-market and he will not serve garrison soldiers substandard food when he can prevent it. The gap is now substantial. If the quartermaster audits, he will find an inexplicable surplus in food quality relative to the approved budget.',
        N'Tegan is the argument that genuine care, applied to a role the institution considers purely logistical, can quietly become a financial and administrative irregularity that could end his position — which is the specific tragedy of a person who takes their work more seriously than the institution does.',
        N'No POV.',
        N'House Pallor; northern garrison kitchen; market district contacts',
        177, 89, N'stocky',
        N'dark brown', N'short, heat-practical', N'short',
        N'dark brown', N'medium warm', N'flushed, heat-reddened',
        N'none',
        N'Efficient and warm in the kitchen; at his best when the work is heaviest; loses patience with administrative processes that reduce the quality of what he feeds people',
        N'Cook''s working clothes, practical and stained; the head cook''s apron; keeps himself clean above the waist when he leaves the kitchen',
        N'none',
        N'Dawn to midday: kitchen management and morning meal service. Midday: afternoon meal preparation. Evening: stock and supply accounting, which he reviews with the specific anxiety of someone who knows his numbers do not match the official budget. Twice a week he visits the Kellian market district to buy at fair price.',
        N'He has been paying the Kellian farms fair market price for their produce rather than the official below-market garrison requisition rate, making up the difference from his own wages. The garrison soldiers eat better than their budget should allow. The Kellian farmers receive fair payment for their goods. Over three years this has created a significant personal deficit and an inexplicable food quality surplus in the garrison record. He has not told anyone. He is not sure what he would say if asked. He knows the quartermaster will eventually notice.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Northern garrison kitchen; Kellian market district; supply routes',
        N'0', N'0',
        N'Stocky young Welsh-looking cook in working clothes and head cook''s apron, dark brown hair, heat-flushed warm face, garrison kitchen with fire and pots, medieval fantasy --ar 2:3',
        N'A 26-year-old garrison head cook in working clothes, dark brown hair, warm expression, medieval stone garrison kitchen',
        0, 0
    );
    PRINT 'Tegan Wren seeded.';
END
ELSE PRINT 'Tegan Wren already exists.';
GO

-- 51. Sian Holt
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Sian Holt')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Sian Holt', N'sian-holt', N'canon', 1,
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
        @id, N'Sian Holt', N'sian-holt', N'Sian', N'Holt', N'',
        N'human', N'human', N'female', N'she/her', 33, N'alive',
        N'Garrison engineer; channel fortification maintenance; older sister of Aldric Holt',
        N'Sian Holt has been maintaining the channel fortification apparatus for seven years. She is the most technically knowledgeable person in the garrison on the subject of what the channel''s defensive infrastructure can and cannot do, which is why it is particularly significant that she has been unable to get anyone to fund the repair of the northern battery''s structural flaw. She reported it eighteen months ago. The repair was approved in principle and never funded. She reports it again every three months. She has started keeping waterproofed copies of every report she has filed in a case she keeps outside the garrison, in case she needs to prove she warned them.',
        N'Sian is the engineer at the end of the paper trail — a person who has done everything an institution requires her to do, filed the reports, followed the process, and is now watching a structural problem persist because the institution''s funding cycle does not match the urgency of the problem.',
        N'No POV.',
        N'House Pallor; channel fortification garrison, northern battery',
        165, 64, N'lean-athletic',
        N'dark brown', N'tied back, practical', N'medium',
        N'brown', N'fair-medium', N'clear, work-roughened hands',
        N'none',
        N'Precise and focused; the engineer''s habit of assessing structures before she trusts them',
        N'Engineering working clothes, practical and tool-marked; the garrison engineer''s badge; keeps her younger brother''s food supply separate in her kit',
        N'none',
        N'Daily inspection of the channel fortification apparatus. Technical reports. The quarterly structural flaw report. When her brother Aldric is on rotation in the garrison, she brings him food without discussing it.',
        N'The northern battery has a structural fracture in the eastern foundation support that, under the stress of a concentrated Draught assault, could cause a cascading failure in the defensive line. She identified it eighteen months ago, has reported it eleven times, and has been told it is in the funding queue each time. She does not know if this is bureaucratic delay or deliberate deprioritization. She keeps copies outside the garrison because she wants to be able to prove, if it fails, that she warned them. She has not told anyone including her brother, who watches from the same battery.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Channel fortification; northern battery; garrison engineering office',
        N'0', N'0',
        N'Welsh-looking woman garrison engineer in work-marked practical clothes, dark brown tied-back hair, precise focused expression, channel fortification stone structure, medieval fantasy --ar 2:3',
        N'A 33-year-old woman garrison engineer in practical clothes, dark brown hair, focused expression, medieval stone channel fortification',
        0, 0
    );
    PRINT 'Sian Holt seeded.';
END
ELSE PRINT 'Sian Holt already exists.';
GO

-- 52. Cai Pellam
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Cai Pellam')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Cai Pellam', N'cai-pellam', N'canon', 1,
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
        @id, N'Cai Pellam', N'cai-pellam', N'Cai', N'Pellam', N'',
        N'human', N'human', N'male', N'he/him', 30, N'alive',
        N'Ship engineer; Pallor''s Reach; maintains the vessel''s Scrying apparatus machinery; son of Cador Pellam',
        N'Cai Pellam is the youngest engineer to serve aboard Pallor''s Reach and has been maintaining the ship''s Scrying apparatus machinery for three years. He is technically excellent, quietly observant, and has a complicated relationship with his father Cador, which is to say he left the lowland estates for a ship berth at seventeen specifically to not manage farmsteads and has not discussed this with Cador in any terms either of them would acknowledge. He has been building an unauthorized modification to the ship''s apparatus based on observations he has made of Gwenith Arlow''s technique during observation sessions. The modification would allow simultaneous multi-Sphere observation.',
        N'Cai is a young engineer who has observed something he cannot fully account for and is building the instrument to test his hypothesis — unauthorized, alone, and three months from a result that will either validate the observation or invalidate the last two years of work.',
        N'No POV.',
        N'House Pallor; Pallor''s Reach, channel waters; Anglic lowland origin',
        178, 79, N'athletic-lean',
        N'light brown', N'practical, salt-worn', N'short',
        N'pale blue', N'fair', N'weathered, oil-marked hands',
        N'none',
        N'Precise and capable with machinery; socially easy without being forthcoming; a young man who is comfortable with complexity and uncomfortable with simple questions about his family',
        N'Ship engineer''s working clothes, oil-stained and practical; maintains a working tidiness rather than a formal one',
        N'none',
        N'Apparatus maintenance and mechanical inspection during morning shifts. Emergency repairs as needed. In the off-watch hours he works on his modification in the cargo hold, where the noise of the ship masks the sounds of assembly. He has told no one, including Gwenith Arlow, whose technique inspired the modification.',
        N'He has been building a multi-Sphere observation capability by adapting the ship''s existing apparatus using components he has requisitioned as maintenance replacements. He has not told Gwenith because he is afraid she will either claim the development or shut it down, and he is not sure which outcome he fears more. He is three months from testing. If the modification works, he will need to decide whether to show it to Gwenith, report it to the installation authority, or keep it to himself. He has not decided.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Pallor''s Reach, channel waters; harbor port when docked',
        N'0', N'0',
        N'Young English-looking ship engineer in oil-stained practical clothes, light brown hair, precise capable expression, ship machinery compartment, medieval fantasy --ar 2:3',
        N'A 30-year-old ship engineer in practical oil-stained clothes, light brown hair, focused expression, medieval ship machinery compartment',
        0, 0
    );
    PRINT 'Cai Pellam seeded.';
END
ELSE PRINT 'Cai Pellam already exists.';
GO

-- 53. Eira Voss
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Eira Voss')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Eira Voss', N'eira-voss', N'canon', 1,
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
        @id, N'Eira Voss', N'eira-voss', N'Eira', N'Voss', N'',
        N'human', N'human', N'female', N'she/her', 55, N'alive',
        N'Paladin; senior ship assault specialist; channel breach veteran of two incursions',
        N'Eira Voss has survived three infusions and would not describe the experience of any of them as good. She is now nearly two meters tall with the build of someone whose skeleton was rebuilt from the inside, which is more or less what happened. She commands the ship assault unit aboard Pallor''s Reach — the close-quarters fighters who board enemy vessels or defend against boarding — and has done so for twelve years. During the last Draught incursion she killed a Draught Paladin in the cargo hold of a seized vessel. She found a letter on his body afterward. She cannot read the Draught script, but she recognized one word: her name.',
        N'Eira is a person sitting with evidence that she is known to the enemy — that there is a letter about her in a dead man''s pocket — and who has not told anyone because she does not yet know what it means or whether knowing would change anything she does.',
        N'No POV.',
        N'House Pallor; Pallor''s Reach, channel waters; naval assault command',
        192, 109, N'heavily built',
        N'silver', N'short, practical', N'short',
        N'pale blue', N'fair', N'scarred, weathered',
        N'Evident enhancement — significant height, altered proportions; eyes lighter than birth; skeletal structure visibly reinforced',
        N'Economical and massive; moves like someone who has made peace with the amount of space she occupies',
        N'Ship assault gear when working; plain heavy wool off-duty; the Paladin''s insignia worn without ceremony',
        N'Paladin — three infusions; significant enhancement',
        N'Morning combat drills with the assault unit. Ship assault readiness checks. Off-duty she reads, which the unit finds unexpected. The letter from the Draught Paladin is in a sealed case in her quarters.',
        N'The letter was in the dead Draught Paladin''s breast pocket, folded and sealed. It is written in Draught script. The only word she can read is her own name, twice, in the first paragraph. She has not shown it to anyone who can read Draught because she is not sure she wants to know what it says. She has not destroyed it because she thinks she needs to know. She has been in this position for two years.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Pallor''s Reach, channel waters; naval assault range',
        N'0', N'0',
        N'Imposing tall enhanced woman warrior in ship assault gear, short silver hair, scarred pale face, massively reinforced build, medieval ship deck, Buehlman dark fantasy --ar 2:3',
        N'A 55-year-old woman Paladin in ship assault gear, short silver hair, scarred face, dramatically enhanced physique, on a medieval ship deck',
        0, 0
    );
    PRINT 'Eira Voss seeded.';
END
ELSE PRINT 'Eira Voss already exists.';
GO

-- 54. Bronn Arwall
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Bronn Arwall')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Bronn Arwall', N'bronn-arwall', N'canon', 1,
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
        @id, N'Bronn Arwall', N'bronn-arwall', N'Bronn', N'Arwall', N'',
        N'human', N'human', N'male', N'he/him', 48, N'alive',
        N'Paladin; ground assault commander; channel defense specialist',
        N'Bronn Arwall received two infusions in his thirties and emerged from them as something large and difficult to stop. He commands the ground assault unit responsible for the channel''s landward defense and has held this position for nine years. He is effective. His last infusion was seven years ago. The practitioners have recommended a third infusion twice. He has declined both times, without explaining why, and the House has not pressed him on it because his service record makes pressing him feel inadvisable. The real reason: he is afraid that another infusion will alter him past the point where he recognizes himself in the mirror. He has looked every morning for seven years to confirm that the face looking back is still recognizably his.',
        N'Bronn is the Paladin''s existential problem made visible: a man who has accepted two transformations and discovered he can feel where his identity ends and the enhancement begins, and who will not cross that line again even at the institution''s request.',
        N'No POV.',
        N'House Pallor; channel fortification, ground assault command',
        194, 116, N'heavily built',
        N'dark brown going grey', N'close-cropped', N'short',
        N'dark brown', N'medium warm', N'scarred, weathered',
        N'Evident enhancement — significant height, altered proportions; eyes changed to a paler shade; vascular prominence throughout',
        N'Deliberate and controlled; moves with the awareness of someone who has learned to calibrate their own strength',
        N'Field command dress, reinforced; no ceremonial addition; the Paladin''s insignia worn low',
        N'Paladin — two infusions; significant enhancement',
        N'Command rounds and assault unit drills. Ground inspection of the channel fortification defensive line. In the mornings, before anyone else is up, he stands at the washbasin mirror for exactly two minutes. He has never missed this.',
        N'He declined the third infusion because he believes another transformation will change something fundamental about how he perceives and responds to the world — something that will not be visible to anyone else but that he will feel. He has watched Dougal Strathmore across six years of command meetings and seen what he is afraid of: a man who is entirely functional, entirely capable, and who does not seem to notice anymore when something human passes across his face. He is afraid of not noticing. He looks in the mirror every morning to confirm he still can.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Channel fortification ground defense; assault unit operational range',
        N'0', N'0',
        N'Large enhanced English-Celtic warrior in reinforced field dress, dark brown close-cropped hair, scarred medium-warm face, controlled powerful bearing, stone channel fortification, medieval fantasy --ar 2:3',
        N'A 48-year-old ground assault Paladin in reinforced field dress, dark brown hair, controlled expression, medieval stone channel fortification',
        0, 0
    );
    PRINT 'Bronn Arwall seeded.';
END
ELSE PRINT 'Bronn Arwall already exists.';
GO

-- 55. Uren Morke
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Uren Morke')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Uren Morke', N'uren-morke', N'canon', 1,
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
        @id, N'Uren Morke', N'uren-morke', N'Uren', N'Morke', N'',
        N'human', N'human', N'male', N'he/him', 37, N'alive',
        N'Champion of House Pallor; the island''s sole living Champion; channel defense supreme asset',
        N'Uren Morke is two meters and fifteen centimeters of post-human capacity in a House Pallor uniform. He has survived more infusions than the House''s practitioners can fully account for, and his body has arrived at a point that the standard progression chart has no notation for. He is not in pain. He is not distressed. He has more capacity than any ten soldiers the House could field. He also wants the Living War to end, not for ideological reasons but because he has been killed twice — genuinely stopped, restarted — and the experience has made him find the ongoing conflict difficult to take seriously as an institution. He has been making quiet unauthorized contacts across the war lines for four years.',
        N'Uren is the war''s ultimate weapon making unauthorized peace attempts — a person who has become so far beyond the conflict that he can see it from outside, and who is trying to end it without being authorized to do so by the institution that built him.',
        N'No POV.',
        N'House Pallor; capital and channel; full operational range across all Pallor territory',
        215, 148, N'post-human',
        N'white', N'very short', N'short',
        N'pale, almost luminous', N'fair', N'smooth, the specific quality of skin that has been remade',
        N'Pronounced — form is clearly post-human; proportions extreme; height and mass beyond standard human; eyes luminous; movement too controlled to be natural',
        N'Still in a way that is not human stillness; moves with absolute precision and no wasted motion; his presence in a room changes the room',
        N'Pallor military dress, custom-made; fits because it was made for his current form; no insignia that could be read as ceremonial',
        N'Champion — infusion count unclear; body has arrived at a post-human equilibrium',
        N'Serves in whatever capacity Lord Aldwyn requires. Has been twice declined the ability to operate on the continent independently, which he considers a bureaucratic oversight. In the margins of his scheduled duties he has contacts with intermediaries from four other Houses. He does not experience urgency, which means he has been conducting this operation with the patience of someone who cannot be rushed.',
        N'He has been in contact with intermediaries from Draught, Atrament, Fornax, and Calyx for four years, sounding out whether a general armistice framework exists that all seven Houses could be brought to. He has not told Lord Aldwyn. He does not know what he will do if one of his contacts reports to their House leadership and he is formally recalled. He is also aware that he cannot be compelled by conventional force to do anything he does not choose to do, which makes the recall order an interesting theoretical problem.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Full Pallor territory operational range; unauthorized continental contact network',
        N'0', N'0',
        N'Towering post-human warrior in custom military dress, white very short hair, almost luminous pale eyes and skin, inhuman precise stillness, stone great hall or open landscape, Buehlman dark fantasy --ar 2:3',
        N'A 37-year-old Champion of post-human scale, white hair, luminous eyes, wearing custom military dress, medieval stone great hall',
        0, 0
    );
    PRINT 'Uren Morke seeded.';
END
ELSE PRINT 'Uren Morke already exists.';
GO

-- 56. Idris Fell
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Idris Fell')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Idris Fell', N'idris-fell', N'canon', 1,
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
        @id, N'Idris Fell', N'idris-fell', N'Idris', N'Fell', N'',
        N'human', N'human', N'male', N'he/him', 29, N'alive',
        N'Knight; channel garrison assault unit; newest Knight in House Pallor service',
        N'Idris Fell received his infusion eleven months ago. He survived. The practitioners noted it was a clean take and assigned him to the channel assault unit under Bronn Arwall. He has not told anyone what he heard during the infusion: a pattern, not a voice exactly, that lasted perhaps four seconds before it resolved into the pain of transformation. The pattern was structured — interval and repetition. It may have been the sound the Catalyst made in his blood. It may have been something else. He does not have language for what it may have been and does not know who to ask.',
        N'Idris is the open question at the membrane between the body and whatever the infusion does to it — a young Knight who may have experienced something anomalous during transformation and who cannot find the answer without telling someone what happened.',
        N'No POV.',
        N'House Pallor; channel garrison, assault unit',
        180, 83, N'athletic',
        N'light brown', N'short, plain', N'short',
        N'brown', N'fair-medium', N'clear, new scarring on the left forearm from infusion',
        N'Subtle height gain; vascular prominence on arms; eyes changed fractionally lighter',
        N'Energetic and physically confident; still testing the edges of his new capacity with the care of someone mapping new territory',
        N'Channel garrison dress, new but broken in quickly; the Knight''s insignia worn with self-consciousness he has not yet shed',
        N'Knight — first infusion eleven months ago; modest enhancement',
        N'Assault unit drills and channel watch. Eats in the garrison hall. Trains more than required. Occasionally sits very still and listens, which his unit attributes to focus. He is listening for the pattern. He has not heard it again.',
        N'During the four seconds before the infusion pain peaked, he heard a structured pattern: interval, repetition, something that felt like it had a source rather than being an artifact of transformation. It lasted four seconds. He has not heard it since. He does not know whether it was the Catalyst interacting with his nervous system, whether it was something the membrane does at the moment of Transmutation, or whether it was a hallucination produced by pain and fear. He has not told anyone because he cannot describe it without sounding like he is reporting a supernatural experience.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Channel garrison; assault unit operational range',
        N'0', N'0',
        N'Young English-Celtic man in channel garrison dress with new Knight insignia, light brown hair, energetic bearing, new left forearm scar, stone garrison yard, medieval fantasy --ar 2:3',
        N'A 29-year-old Knight in garrison dress, light brown hair, energetic expression, new insignia, medieval stone garrison',
        0, 0
    );
    PRINT 'Idris Fell seeded.';
END
ELSE PRINT 'Idris Fell already exists.';
GO

-- 57. Dame Isolde Morvan
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Isolde Morvan')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Isolde Morvan', N'isolde-morvan', N'canon', 1,
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
        @id, N'Isolde Morvan', N'isolde-morvan', N'Isolde', N'Morvan', N'Dame',
        N'human', N'human', N'female', N'she/her', 42, N'alive',
        N'Knight; coastal patrol command; cousin of Llinos Morvan; holder of knowledge about unauthorized weapon design',
        N'Dame Isolde Morvan is Llinos Morvan''s first cousin and has known about the unauthorized weapon designs in Llinos''s workshop for six weeks. She found out at a family visit when Llinos showed her something she thought was safe to show. It was not safe. Isolde has not reported it. Her primary conflict is not ideological: the weapon design Llinos has developed is, on Isolde''s technical assessment, genuinely effective — and the implication of its effectiveness is that Llinos will be executed for having developed it without authorization.',
        N'Isolde is a military officer who discovered that someone she loves has committed an offense the House would punish severely, and who is sitting with the paralysis of knowing that reporting is institutionally correct and also the action that ends her cousin.',
        N'No POV.',
        N'House Pallor; coastal patrol; Morvan family estate',
        176, 77, N'athletic',
        N'dark brown', N'tied back, practical', N'medium',
        N'hazel', N'warm fair', N'clear, weathered from outdoor patrol',
        N'Subtle height gain; increased skeletal density visible in jaw and brow; vascular prominence on forearms',
        N'Capable and professionally assured; carries a specific stillness she has developed in the six weeks since the estate visit',
        N'Coastal patrol command dress; the Dame''s insignia worn for function not ceremony',
        N'Knight — two infusions; moderate enhancement',
        N'Coastal patrol command duties. Tactical assessments. Family correspondence with Llinos that has become more careful. She has drafted a report to garrison command three times and not submitted any of them.',
        N'She has seen Llinos''s unauthorized weapon modification. On technical grounds it is the most sophisticated adaptation she has seen a civilian develop — it extends the Scrying apparatus''s operational range in combat configuration in ways the official engineering teams have not achieved. It would also end Llinos''s life if she reported it, because unauthorized weapon development is capital under the military code. She has told no one. She has spent six weeks trying to find a path that preserves both her cousin''s life and the weapon design. She has not found one.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Coastal patrol range; Morvan estate; island capital when required',
        N'0', N'0',
        N'Athletic Cornish/Breton woman in coastal patrol command dress, dark brown tied-back hair, hazel eyes, capable restrained bearing, stone coastal garrison, medieval fantasy --ar 2:3',
        N'A 42-year-old woman Knight in coastal patrol dress, dark brown hair, hazel eyes, controlled expression, medieval coastal garrison',
        0, 0
    );
    PRINT 'Isolde Morvan seeded.';
END
ELSE PRINT 'Isolde Morvan already exists.';
GO

-- 58. Nessa Cray (DEAD)
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Nessa Cray')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Nessa Cray', N'nessa-cray', N'canon', 1,
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
        @id, N'Nessa Cray', N'nessa-cray', N'Nessa', N'Cray', N'',
        N'human', N'human', N'female', N'she/her', 51, N'dead',
        N'Former garrison supply clerk; murdered for knowledge of embezzlement; death recorded as fever',
        N'Nessa Cray died nine months ago of garrison fever, according to the garrison record. She did not die of fever. She died because she had been doing her job with precision for nineteen years and had therefore noticed exactly where the northern garrison supply accounts did not add up. She traced the discrepancy to a specific officer. She wrote it down and told one person. That person told someone else. The fever report was filed within the week. No one has formally questioned the record.',
        N'Nessa''s death is the paper trail no one has followed — the document that names an embezzlement thread, the person she told, and the eight days between the telling and the official fever death.',
        N'No POV.',
        N'House Pallor; northern garrison, formerly; deceased',
        163, 60, N'average',
        N'light brown, going grey', N'pinned back', N'medium',
        N'grey', N'fair', N'aged naturally; the unremarkable face of someone who spent her life in administrative roles',
        N'none',
        N'The stillness of the record — known now only by what she left behind',
        N'Supply clerk''s practical dress, as remembered; her supply ledger is in the archive',
        N'none',
        N'She worked the garrison supply accounts from first light. Ate in the supply office. Went home to her billet each evening. She spent the last week of her life, by what can be reconstructed, avoiding any situation where she would be alone with the officer she had identified.',
        N'She knew the northern garrison''s senior logistics officer had been skimming supply accounts for at least three years — a consistent small percentage across multiple categories that aggregated to sixty months of garrison pay. She told the paymaster''s deputy, Arrac, thinking the deputy would take it upward. The deputy told someone else. Nessa was dead eight days later. Arrac has been promoted since.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Northern garrison, where her memory persists in the supply archive',
        N'0', N'0',
        N'Faded portrait of a middle-aged English-looking woman supply clerk in practical dress, light brown-grey pinned hair, grey eyes, unremarkable administrative appearance, garrison supply office, memorial quality, medieval fantasy --ar 2:3',
        N'A 51-year-old woman supply clerk, light brown-grey hair, grey eyes, practical dress, memorial quality portrait, medieval garrison',
        0, 0
    );
    PRINT 'Nessa Cray seeded.';
END
ELSE PRINT 'Nessa Cray already exists.';
GO

-- 59. Elwin Penrose
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Elwin Penrose')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Elwin Penrose', N'elwin-penrose', N'canon', 1,
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
        @id, N'Elwin Penrose', N'elwin-penrose', N'Elwin', N'Penrose', N'',
        N'human', N'human', N'male', N'he/him', 62, N'alive',
        N'Senior Myrmidon trainer; Sphere 31 origin; arrived thirty years ago; fully integrated into Pallor service',
        N'Elwin Penrose came from Sphere 31 at thirty-two, recognized within his first year for his combat experience and placed in training roles. He has been a Myrmidon trainer for twenty-six years and is very good at it. He teaches what he calls the rules of engagement — specific conventions of how conflict is conducted between persons — in a framework he developed himself, combining what he remembers from his origin Sphere with the Cauld''s existing doctrine. His trainees perform consistently well. He has not told anyone that significant portions of his training doctrine come from conventions that did not originate in the Cauld.',
        N'Elwin is the institutional borrowing that cannot be acknowledged — a trainer who has been teaching Sphere 31 military ethics under a Cauld wrapper for thirty years, whose doctrine is now standard practice in Pallor''s garrison training.',
        N'No POV.',
        N'House Pallor; garrison training facility; fully integrated into Pallor community',
        175, 78, N'stocky-lean, age-settled',
        N'grey', N'close-cropped', N'short',
        N'brown', N'warm medium-dark', N'weathered, aged',
        N'none',
        N'The authority of a man who has been teaching the same material for twenty-six years and refined it to the point where every word is load-bearing',
        N'Trainer''s practical dress; the senior trainer''s stripe; nothing that marks Sphere 31 origin',
        N'none',
        N'Morning training sessions with new conscripts and junior Myrmidons. Assessment evaluations. He eats in the garrison hall with his trainees — an old habit from a world that had a different name for the same instinct. He writes his curriculum notes in a personal notation that is a hybrid of the two writing systems he has lived with.',
        N'His combat ethics framework contains specific prohibitions and conventions drawn from the laws of armed conflict in his origin Sphere. He calls them practical rules. He has never told anyone they originate in a Sphere 31 legal framework developed over centuries of conflict in a world the Cauld has no record of. If asked to source them, he would say he developed them from experience. This is not wrong — he did develop them, from experience that included thirty years in both worlds.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Garrison training facility; Pallor community, fully integrated',
        N'0', N'0',
        N'Experienced medium-dark weathered man in senior trainer''s dress with trainer''s stripe, close-cropped grey hair, authoritative calm bearing, garrison training yard, medieval fantasy --ar 2:3',
        N'A 62-year-old senior military trainer in practical dress, grey hair, warm medium-dark skin, authoritative expression, medieval garrison training yard',
        0, 0
    );
    PRINT 'Elwin Penrose seeded.';
END
ELSE PRINT 'Elwin Penrose already exists.';
GO

-- 60. Caera Donn
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Caera Donn')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Caera Donn', N'caera-donn', N'canon', 1,
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
        @id, N'Caera Donn', N'caera-donn', N'Caera', N'Donn', N'',
        N'human', N'human', N'female', N'she/her', 55, N'alive',
        N'Senior garrison healer; Sphere 31 origin; arrived twenty-three years ago; keeps a private list',
        N'Caera Donn was taken from Sphere 31 at thirty-two — she had been a hospital administrator — and assigned to medical support in Pallor''s garrison, where her skill became apparent. She has been a garrison healer for twenty years. She is methodical, patient, and trusted. She keeps a private list: the names of people she knew when she was taken, with their estimated current ages, maintained annually on the date of her taking. It is the most precisely maintained document she produces and it serves no practical purpose she can identify.',
        N'Caera is grief conducted as administrative practice — a person who has found that keeping the record is the only form of care available to the people she left behind, and who does it anyway.',
        N'No POV.',
        N'House Pallor; garrison medical facility; Sphere 31 community connection',
        162, 63, N'lean-average',
        N'dark brown going grey', N'pinned', N'medium',
        N'warm brown', N'medium-dark', N'clear, age-lined around the eyes',
        N'none',
        N'Precise and calm; the specific patience of a healer who has learned the Cauld''s medicine by doing it for twenty years',
        N'Healer''s practical working dress; the senior healer''s sash; nothing that marks Sphere 31 origin',
        N'none',
        N'Medical rounds and patient management. Teaching the garrison''s junior healers. Each year on the anniversary of her taking she sits in the evening and updates the list. She has done this twenty-three times. Her sister would be fifty-one now.',
        N'She keeps a list of everyone she knew when she was taken: their names, their last known circumstances, and her estimate of their current age. She updates it annually. The list is now fifty-seven names long. She does not know why she keeps it. She has considered destroying it and does not. She knows it is not practically useful. She has come to believe that the list is what fidelity to the people she cannot reach looks like in the absence of any other option.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Garrison medical facility; Pallor community, fully integrated',
        N'0', N'0',
        N'Middle-aged experienced healer in practical working dress with senior healer''s sash, dark brown-grey pinned hair, warm medium-dark skin, precise patient bearing, stone garrison medical room, medieval fantasy --ar 2:3',
        N'A 55-year-old woman healer in practical dress with healer''s sash, dark brown-grey hair, medium-dark skin, patient expression, medieval stone medical room',
        0, 0
    );
    PRINT 'Caera Donn seeded.';
END
ELSE PRINT 'Caera Donn already exists.';
GO

-- 61. Piers Vance
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Piers Vance')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Piers Vance', N'piers-vance', N'canon', 1,
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
        @id, N'Piers Vance', N'piers-vance', N'Piers', N'Vance', N'',
        N'human', N'human', N'male', N'he/him', 62, N'alive',
        N'Sphere 31 origin; Pallor court scholar; maintaining bilingual record in private notation',
        N'Piers Vance was taken from Sphere 31 at twenty-nine and spent a decade in administrative support before being recognized as a scholar and assigned to the court library. He has been there for twenty-three years. He is producing a history of the Cauld — a bilingual document written in a notation combining Cauld script with a personal cipher drawing from his origin Sphere''s writing system. The result is unreadable by anyone in the Cauld who has not been taught the cipher. He has taught no one. He does not know why he is writing it. He writes.',
        N'Piers is the scholar''s compulsion facing the unanswerable question: who is the record for when the archivist cannot go home and the people who could read it are unreachable across the membrane?',
        N'No POV.',
        N'House Pallor; court library; rarely travels beyond the island capital',
        170, 71, N'average',
        N'grey', N'somewhat untidy', N'medium',
        N'pale blue', N'fair', N'ink-stained at the fingers; otherwise unremarkable',
        N'none',
        N'Absorbed and reflective; the scholar''s conditional presence — he is in the room when the work does not demand attention',
        N'Scholar''s practical dress; ink-stained cuffs he has not replaced because replacing them interrupts the work',
        N'none',
        N'Library cataloguing, translation, council correspondence when asked. His own record is written in the early mornings before the library opens. He has been at this for twenty-three years and the document is approximately four hundred pages in his personal notation.',
        N'The bilingual record exists. It contains an account of the Cauld''s internal history as he has observed it, interwoven with what he can reconstruct of his origin Sphere''s history over the corresponding period. He cannot send it anywhere. He cannot give it to anyone. He does not know if it will survive him. He writes it because the alternative is to not write it, and that alternative is one he has considered and rejected every morning for twenty-three years.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Court library; island capital',
        N'0', N'0',
        N'Absorbed grey-haired scholar in ink-stained practical dress, pale blue eyes, untidy medium hair, reflective expression, stone court library with manuscripts, medieval fantasy --ar 2:3',
        N'A 62-year-old scholar in ink-stained practical dress, grey hair, pale blue eyes, absorbed expression, medieval stone court library',
        0, 0
    );
    PRINT 'Piers Vance seeded.';
END
ELSE PRINT 'Piers Vance already exists.';
GO

-- 62. Bethan Grue
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Bethan Grue')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Bethan Grue', N'bethan-grue', N'canon', 1,
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
        @id, N'Bethan Grue', N'bethan-grue', N'Bethan', N'Grue', N'',
        N'human', N'human', N'female', N'she/her', 34, N'alive',
        N'Council secretary; official transcript keeper; Anglic; quietly maintaining a parallel record',
        N'Bethan Grue has been keeping official transcripts of the three-people council for seven years. She is precise, discreet, and trusted absolutely. She has also been maintaining a second private transcript for four years — recording what was actually said during council sessions, including statements that were subsequently omitted from the official version. The divergences are not always significant. Some are.',
        N'Bethan is the keeper of what the record would be if the record were honest — and the person who has to decide what to do with four years of evidence that the official transcript is being managed.',
        N'No POV.',
        N'House Pallor; council chamber and archive; island capital',
        161, 59, N'average',
        N'light brown', N'very neatly kept, always pinned', N'short',
        N'green', N'fair', N'clear, indoor-pale',
        N'none',
        N'Precise and professionally invisible — the perfect secretary; people say things in front of her without registering her presence',
        N'Secretary''s formal dress, dark and precisely maintained; the council transcript seal at her breast',
        N'none',
        N'Council sessions: official transcription. Post-session: transcript preparation and submission. In the evenings in her private quarters: the second transcript, kept in a sealed box not in her official premises.',
        N'She has been maintaining a private transcript of actual council session content for four years. The divergences between official and private records range from minor rewordings to the complete omission of three statements she judges materially significant — one of which directly contradicts a subsequent official council decision. She has continued keeping it because the alternative is to participate in the management of the record she was trusted to maintain honestly, and she cannot bring herself to stop.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Council chamber; council archive; private quarters',
        N'0', N'0',
        N'Precise Welsh-looking woman in dark secretary''s dress, light brown neatly pinned hair, green eyes, professionally invisible attentive bearing, stone council chamber, medieval fantasy --ar 2:3',
        N'A 34-year-old woman council secretary in dark formal dress, light brown hair, green eyes, precise attentive expression, medieval stone council chamber',
        0, 0
    );
    PRINT 'Bethan Grue seeded.';
END
ELSE PRINT 'Bethan Grue already exists.';
GO

-- 63. Morcant Lune
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Morcant Lune')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Morcant Lune', N'morcant-lune', N'canon', 1,
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
        @id, N'Morcant Lune', N'morcant-lune', N'Morcant', N'Lune', N'',
        N'human', N'human', N'male', N'he/him', 47, N'alive',
        N'Morvic council representative; three-people council; has been shaping the submitted record to protect Morvic political position',
        N'Morcant Lune represents the Morvic people on the three-people council. He is experienced and politically capable. His problem: Morvic council proposals have been failing at a rate that would be politically damaging if the full record were visible. He has been working with the council archivist''s office — through normal channels, without explicitly requesting anything improper — to ensure that the submitted record emphasizes passed proposals and notes failed ones only in summary form. Bethan Grue''s private transcript has caught every instance.',
        N'Morcant is a politician who has discovered that protecting his people''s position requires managing the appearance of their political record — who has crossed from representation into manipulation so gradually he may not have noticed the line.',
        N'No POV.',
        N'House Pallor; council chamber; Morvic territory',
        178, 82, N'average',
        N'dark grey', N'neatly kept', N'short',
        N'dark grey', N'medium warm', N'clear, indoor-pale',
        N'none',
        N'Politically practiced and measured; reads every room before speaking; the specific quality of a man who has been managing a weak position for too long',
        N'Council representative''s formal dress, well-made; Morvic coastal pattern worked subtly into the collar',
        N'none',
        N'Council sessions and preparation. Private meetings with other representatives and with Morvic territorial leadership. The archivist''s office communications he conducts through normal professional channels, asking for nothing that sounds improper and receiving precisely what he needs.',
        N'He has been managing the council record''s presentation of Morvic proposals for three years through requests that are individually reasonable — emphasis, summary form, procedural accuracy — and collectively constitute a systematic editing of the failure rate. He has told himself at each step that he is ensuring accurate framing. He has not examined the cumulative effect. Bethan Grue has examined it in detail.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Council chamber; Morvic territorial council; island capital',
        N'0', N'0',
        N'Politically practiced Cornish/Morvic man in council representative''s formal dress with subtle Morvic collar pattern, dark grey hair, measured bearing, stone council chamber, medieval fantasy --ar 2:3',
        N'A 47-year-old male council representative in formal dress with Morvic collar, dark grey hair, measured expression, medieval stone council chamber',
        0, 0
    );
    PRINT 'Morcant Lune seeded.';
END
ELSE PRINT 'Morcant Lune already exists.';
GO

-- 64. Sioned Ferr
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Sioned Ferr')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Sioned Ferr', N'sioned-ferr', N'canon', 1,
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
        @id, N'Sioned Ferr', N'sioned-ferr', N'Sioned', N'Ferr', N'',
        N'human', N'human', N'female', N'she/her', 29, N'alive',
        N'Supply clerk; mixed Anglic-Morvic-Kellian heritage; thrives in administrative gaps',
        N'Sioned Ferr is the only person in House Pallor''s administrative structure whose heritage is a roughly even mixture of all three island peoples — Anglic grandmother, Morvic grandfather, Kellian mother, Anglic-Morvic father. The three-people council''s administrative structure has separate channels for each people and a general channel for cross-people matters. Sioned has learned that the general channel processes things faster than any specific one, and that she is the only person credentialed to use it without a dual-territory justification, because her heritage covers all three.',
        N'Sioned is the person the system produces when you bureaucratize ethnicity — a woman whose mixed heritage is a practical advantage, who navigates the House''s administrative structure by being the thing every single category cannot fully contain.',
        N'No POV.',
        N'House Pallor; supply administration; island capital and all three peoples'' territories',
        164, 60, N'lean',
        N'dark brown', N'loose and practical', N'medium',
        N'hazel, shifting green-brown', N'warm medium', N'clear',
        N'none',
        N'Energetic and socially elastic; code-switches registers between the three peoples'' administrative cultures with practiced ease',
        N'Supply clerk''s practical dress; wears elements from all three peoples'' regional textile traditions, which in her case reads as personal style rather than deliberate statement',
        N'none',
        N'Processes supply requisitions across all three peoples'' allocation territories. Attends general channel council meetings as administrative support. Has not formally documented her methodology because doing so would require the council to decide whether to formalize or prohibit it.',
        N'She has discovered that she can move supply requisitions approximately forty percent faster by routing everything through the general channel on the grounds that her mixed heritage gives her automatic cross-territory standing. This is technically correct and has never been formally adjudicated. She has been doing it for three years without challenge, possibly because the result is faster processing. She has not told anyone because formalization would mean the council deciding whether her methodology is legitimate, and she is not confident they would decide in her favor.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Island capital supply administration; all three peoples'' territories',
        N'0', N'0',
        N'Young mixed-heritage woman in supply clerk''s practical dress with regional textile elements, dark brown loose hair, hazel eyes, energetic socially elastic bearing, stone administrative office, medieval fantasy --ar 2:3',
        N'A 29-year-old mixed-heritage woman supply clerk in practical dress, dark brown hair, hazel eyes, energetic expression, medieval stone administrative office',
        0, 0
    );
    PRINT 'Sioned Ferr seeded.';
END
ELSE PRINT 'Sioned Ferr already exists.';
GO

-- 65. Rhys Ellory
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Rhys Ellory')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Rhys Ellory', N'rhys-ellory', N'canon', 1,
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
        @id, N'Rhys Ellory', N'rhys-ellory', N'Rhys', N'Ellory', N'',
        N'human', N'human', N'male', N'he/him', 38, N'alive',
        N'Kellian council representative; believes his brother Padrig is dead; brother is Oathless',
        N'Rhys Ellory is the Kellian people''s junior council representative — competent, principled, a useful voice on trade and coastal resource questions. He lost his brother Padrig six years ago, cast out as Oathless. He was told Padrig died attempting to return illegally. He believes this. The truth: Padrig is alive, is one of the more effective leaders in the hill country Oathless network, and has been using the presumed death to operate more freely. Huw Porrow''s funding moves through Padrig''s network. Rhys attends council meetings where Oathless security is discussed and advocates consistently for measured response.',
        N'Rhys is the sibling sitting on a committee that is deciding the fate of someone he loves and believes dead — whose institutional position directly shapes the conditions under which his brother operates, with no knowledge of that relationship.',
        N'No POV.',
        N'House Pallor; council chamber; Kellian territorial council',
        176, 78, N'average',
        N'dark brown', N'neatly kept, medium length', N'medium',
        N'brown', N'warm fair', N'clear',
        N'none',
        N'Principled and measured; carries a quality of grief that has become part of his political positions without his having named it as such',
        N'Council representative''s formal dress; Kellian coastal pattern worked into the collar and cuffs',
        N'none',
        N'Council sessions and preparation. Kellian territorial advocacy. He advocates for measured Oathless response policies because he believes his brother died resisting something cruel. He does not know his brother is alive and operating under the cover of his presumed death.',
        N'His brother Padrig is alive. Padrig is a significant figure in the hill country Oathless network, funded in part by Huw Porrow. Padrig has used the official report of his death as operational cover. Rhys believes Padrig died trying to come home. This belief has shaped every policy position Rhys has taken on Oathless matters for six years. The fact that Padrig is alive and has chosen not to contact him is the thing Rhys cannot know without the whole institutional structure of his grief collapsing.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Council chamber; Kellian territorial district; island capital',
        N'0', N'0',
        N'Welsh/Kellian man in council representative''s formal dress with Kellian collar and cuff pattern, dark brown medium hair, principled measured bearing, stone council chamber, medieval fantasy --ar 2:3',
        N'A 38-year-old Kellian council representative in formal dress, dark brown hair, principled expression, medieval stone council chamber',
        0, 0
    );
    PRINT 'Rhys Ellory seeded.';
END
ELSE PRINT 'Rhys Ellory already exists.';
GO

-- 66. Aderyn Croft
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Aderyn Croft')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Aderyn Croft', N'aderyn-croft', N'canon', 1,
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
        @id, N'Aderyn Croft', N'aderyn-croft', N'Aderyn', N'Croft', N'',
        N'human', N'human', N'female', N'she/her', 24, N'alive',
        N'Apprentice healer; garrison medical training; daughter of Aelwyn Croft; knows how her uncle died',
        N'Aderyn Croft is four months into her healing apprenticeship at the channel garrison under Caera Donn. She is attentive and quick to learn. Her uncle Madern died two years ago in what the family was told was a training accident. He was actually killed by a Knight using excessive force during Myrmidon training — a fact Aderyn knows because she was at the edge of the yard and watched the event from beginning to end. She was seventeen. She was told, quietly, that the correct thing was to let the family grieve and not complicate the garrison''s administration of a difficult matter.',
        N'Aderyn is a young person who was asked by an institution to swallow its error and who has been complying ever since, while training inside that institution and being taught to care for the people it produces.',
        N'No POV.',
        N'House Pallor; channel garrison medical station; Aelwyn Croft family',
        163, 57, N'lean',
        N'brown', N'braided back', N'medium',
        N'hazel', N'fair', N'clear, young',
        N'none',
        N'Attentive and capable in the medical station; carries a specific quality around garrison officers that Caera Donn has noticed and not yet asked about',
        N'Apprentice healer''s practical dress; the junior apprentice''s sash',
        N'none',
        N'Apprentice rounds and patient support under Caera Donn. Medical study in the evenings. She treats patients including Myrmidons who train under the Knight who killed her uncle. She treats them well. This costs her something she has not measured.',
        N'She witnessed her uncle Madern''s death at seventeen. The Knight responsible used a strike outside the permitted range. Madern died three days later. A garrison officer came to Aderyn the following day and explained that the official account would be training accident, and that confirming this would help everyone, including her family. She confirmed it. She has been inside the garrison''s medical system for four months and has said nothing. She does not know whether she is staying because she intends to eventually say something, or because she has found that knowing and not speaking is something she can sustain indefinitely.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Channel garrison medical station; Aelwyn Croft family home',
        N'0', N'0',
        N'Young Welsh-looking woman apprentice healer in practical dress with junior sash, brown braided hair, hazel eyes, attentive expression carrying specific weight, stone garrison medical room, medieval fantasy --ar 2:3',
        N'A 24-year-old woman apprentice healer in practical dress, brown braided hair, hazel eyes, attentive but heavy expression, medieval stone garrison',
        0, 0
    );
    PRINT 'Aderyn Croft seeded.';
END
ELSE PRINT 'Aderyn Croft already exists.';
GO

-- 67. Gruffydd Stow
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Gruffydd Stow')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Gruffydd Stow', N'gruffydd-stow', N'canon', 1,
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
        @id, N'Gruffydd Stow', N'gruffydd-stow', N'Gruffydd', N'Stow', N'',
        N'human', N'human', N'male', N'he/him', 50, N'alive',
        N'Garrison armsmaker; produces and maintains edged weapons for House Pallor; father of Aldith Stow',
        N'Gruffydd Stow has been making and repairing edged weapons for the garrison for twenty-six years. He is considered competent and reliable, not remarkable. This is a misassessment. Gruffydd has been incrementally refining the balance and edge geometry of garrison swords for thirty years using principles he developed himself — principles not part of any formal Cauld arms-making tradition — and the resulting weapons perform measurably better than the official standard. He has not patented the approach or told anyone. He makes each sword, tests it, and sends it out. The garrison soldiers fight better than average. No one has connected this to their arms.',
        N'Gruffydd is the quiet genius of the unglamorous specialty — a craftsman who has been improving the foundational tool of the House''s ground force for thirty years in ways that no one has noticed or credited, because no one looks at a sword the way he does.',
        N'No POV.',
        N'House Pallor; garrison armsmaking shop; channel garrison territory',
        178, 91, N'stocky',
        N'dark brown, going grey at the temples', N'short, work-practical', N'short',
        N'dark brown', N'warm fair', N'weathered, burn-marked hands',
        N'none',
        N'Deliberate and minimal with words; all his communication is through the work; the specific warmth of a man who expresses care through things rather than language',
        N'Armsmaker''s working clothes, heavy and heat-adapted; the forge apron',
        N'none',
        N'Weapons work from first light. Forge work in the mornings, grinding and finishing in the afternoons. He made the small adjustments to Aldith''s garrison kit fit without comment because she asked and because she is his daughter. He has not been able to ask her whether she is eating enough without asking through the vehicle of sending food.',
        N'He developed a blade geometry optimization over thirty years of practice-based iteration that produces weapons with better balance and cutting performance than the official garrison standard. He has no formal language for what he did — he cannot write a technical specification — but every sword he produces embodies it. He has never claimed it or described it to anyone. He believes, approximately correctly, that if he tried to explain it, no one in the formal arms production system would understand what he was saying.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Garrison armsmaking shop; channel garrison; occasional island capital supply contacts',
        N'0', N'0',
        N'Stocky Welsh-looking armsmaker in heavy working clothes and forge apron, dark brown-grey hair, weathered burn-marked hands, deliberate minimal expression, forge workshop with blade and tools, medieval fantasy --ar 2:3',
        N'A 50-year-old armsmaker in forge apron, dark brown-grey hair, weathered expression, burn-marked hands, medieval forge workshop',
        0, 0
    );
    PRINT 'Gruffydd Stow seeded.';
END
ELSE PRINT 'Gruffydd Stow already exists.';
GO

-- 68. Ceinwen Vael
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ceinwen Vael')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ceinwen Vael', N'ceinwen-vael', N'canon', 1,
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
        @id, N'Ceinwen Vael', N'ceinwen-vael', N'Ceinwen', N'Vael', N'',
        N'human', N'human', N'female', N'she/her', 27, N'alive',
        N'Morvic sailor; Pallor''s coastal fleet; spoke with a Draught sailor for twenty minutes before orders arrived',
        N'Ceinwen Vael is a Morvic sailor on the coastal patrol fleet, three years in service. Eight months ago her vessel intercepted a damaged Draught coastal skiff. She had orders to take the vessel and prisoners once the command came from the lead ship. It took twenty minutes. During those twenty minutes, Ceinwen was in the water beside the Draught skiff and found herself alongside a Draught sailor who was about the same age and also in the water. They exchanged words for twenty minutes. She does not know his name. She knows what he was afraid of, what he missed, and that he did not want to be there either. He was taken prisoner when the order came.',
        N'Ceinwen is the story that happens in the gap before the institutional order arrives — twenty minutes in which two people were just two people, and what she did with that afterward.',
        N'No POV.',
        N'House Pallor; coastal patrol fleet; Morvic coastal community',
        161, 58, N'lean-athletic',
        N'dark brown', N'salt-braided, loose', N'medium',
        N'brown', N'warm medium', N'weathered, salt-roughened',
        N'none',
        N'Practical and direct; the sailor''s economy of movement; carries something specific and unresolved when the conversation turns toward Draught',
        N'Sailor''s working clothes, weather-adapted; Morvic coastal pattern at the collar',
        N'none',
        N'Coastal patrol work. Watch rotations. Repairs and maintenance. She does not speak of the twenty minutes to her crewmates. When the prisoner transfer is discussed, she participates without describing her own portion of it.',
        N'She talked with a Draught sailor for twenty minutes while waiting for the order to take him prisoner. He was afraid of the channel in winter and described conditions on Draught vessels she understands as intelligence. He missed a specific piece of Draught coastline he described in terms she recognized from the Morvic fishing grounds. He did not want to be a sailor — assigned rather than volunteered — and was twenty-two years old. She does not know his name. She has not told anyone because she is not sure the information is hers to give and not sure what giving it would mean.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Coastal patrol fleet; Morvic coastal community; channel waters',
        N'0', N'0',
        N'Young Morvic woman sailor in weather-adapted coastal dress with Morvic collar, dark brown salt-braided hair, weathered practical bearing, coastal ship deck, medieval fantasy --ar 2:3',
        N'A 27-year-old woman sailor in coastal working dress, dark brown loose hair, practical weathered expression, medieval coastal ship deck',
        0, 0
    );
    PRINT 'Ceinwen Vael seeded.';
END
ELSE PRINT 'Ceinwen Vael already exists.';
GO

-- 69. Tamsin Fenn
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Tamsin Fenn')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Tamsin Fenn', N'tamsin-fenn', N'canon', 1,
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
        @id, N'Tamsin Fenn', N'tamsin-fenn', N'Tamsin', N'Fenn', N'',
        N'human', N'human', N'female', N'she/her', 36, N'alive',
        N'Schoolteacher; northern settlement; sister of Catriona Fenn; knows about unauthorized Draught negotiations',
        N'Tamsin Fenn teaches children in the northern settlement school, in daily contact with the children of garrison soldiers, Morvic fishing families, and Anglic farmers. She is good at the work. Her sister Catriona is a council aide. Six weeks ago Catriona, in distress, told Tamsin that a council member had been conducting unauthorized back-channel communications with a Draught diplomatic contact without the full council''s knowledge. Catriona did not give names, but gave enough that Tamsin could reconstruct two of the three parties from what Catriona had previously told her about council business. Tamsin has been carrying this for six weeks.',
        N'Tamsin is the civilian holding institutional intelligence she did not seek and cannot act on — a schoolteacher who knows something that could end two careers and a peace initiative, whose only relationship to the information is through her frightened sister.',
        N'No POV.',
        N'House Pallor; northern settlement school; Fenn family',
        165, 62, N'average',
        N'brown', N'loose and practical', N'medium',
        N'blue-grey', N'fair-medium', N'clear',
        N'none',
        N'Warm and direct with children; carefully measured with adults; has been slightly more measured than usual for six weeks',
        N'Teacher''s plain practical dress; the school sash',
        N'none',
        N'Morning and afternoon school sessions. Lesson preparation in the evenings. She has written three letters to Catriona she has not sent because she does not know what to say. She continues teaching. The children do not know anything is wrong. She is very careful about that.',
        N'Catriona told her that a council member had been communicating with a Draught diplomatic contact without full council authorization. From what Catriona had said previously about council business, Tamsin has reconstructed two likely candidates: the council member conducting the contact, and the aide managing the communication channel. She does not know whether this represents an unauthorized peace initiative, an intelligence exchange, or something else. She knows it is happening. She knows Catriona is frightened. She does not know what to do with any of it.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Northern settlement; school; Fenn family connections across the island',
        N'0', N'0',
        N'Young woman schoolteacher in plain practical dress with school sash, brown loose hair, blue-grey eyes, warm but carefully measured expression, northern settlement stone school room, medieval fantasy --ar 2:3',
        N'A 36-year-old woman schoolteacher in plain dress, brown hair, blue-grey eyes, warm but careful expression, medieval stone school room',
        0, 0
    );
    PRINT 'Tamsin Fenn seeded.';
END
ELSE PRINT 'Tamsin Fenn already exists.';
GO

-- 70. Urien Blackwood (DEAD)
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Urien Blackwood')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Urien Blackwood', N'urien-blackwood', N'canon', 1,
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
        @id, N'Urien Blackwood', N'urien-blackwood', N'Urien', N'Blackwood', N'',
        N'human', N'human', N'male', N'he/him', 53, N'dead',
        N'Former Oathless band leader; killed by his own lieutenant; official Pallor account attributes his death to a garrison operation',
        N'Urien Blackwood led the largest organized Oathless band in the hill country for eleven years. He was cast out at thirty-one and spent twenty-two years building something closer to a small governance structure than a raiding band — a community with rules, internal dispute resolution, and a specific avoidance of actions that would bring the full weight of Pallor''s military response down on the hill settlements. He was killed fourteen months ago. The official Pallor account credits a garrison intelligence operation. The actual account: he was killed by his lieutenant Aldra, who wanted to stop him from accepting a conditional amnesty offer from a council member conducting unauthorized Draught negotiations.',
        N'Urien is the posthumous shape of what the Oathless could have been — a man who built something that worked and was killed for the political inconvenience of what he was about to do — whose death has left the hill country with a vacuum and an official story that is wrong.',
        N'No POV.',
        N'House Pallor origin; hill country Oathless territory; deceased',
        182, 87, N'lean-athletic',
        N'black, greying at death', N'worn long', N'medium',
        N'grey', N'warm medium', N'weathered, hill-marked',
        N'none',
        N'As remembered: deliberate, direct, the authority of someone who built his own institution from nothing and knew exactly what it was worth',
        N'Oathless practical dress, well-maintained in his way; the marks of a man who lived outside for twenty-two years',
        N'none',
        N'As remembered: daily operations of the Oathless community from the hill country settlement. Negotiations with coastal communities for supply access. Enforcement of the band''s internal rules with consistent application. In his last months, back-channel negotiation through a cut-out with a council member whose conditional amnesty offer he had provisionally decided to accept.',
        N'Aldra killed him because Aldra calculated that the conditional amnesty offer, if accepted, would dissolve the hill country community by returning its leadership to House custody and dispersing the band. Aldra was wrong — the offer was for leadership return only, not band dispersal — but did not read the full terms. Urien is dead. Aldra leads the band now. The council member''s unauthorized offer is still in motion without its intended counterparty. The official garrison account attributes his death to a night operation that did not occur. Pallor''s garrison accepts the credit because it is convenient.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Hill country Oathless territory, where his memory persists in the community he built',
        N'0', N'0',
        N'Weathered English-Celtic man, memorial portrait quality, lean-athletic build, black-greying long worn hair, grey eyes, direct authority in posture, hill country landscape, medieval fantasy --ar 2:3',
        N'A 53-year-old Oathless leader, black-grey worn hair, grey eyes, weathered direct expression, hill country, memorial quality, medieval fantasy',
        0, 0
    );
    PRINT 'Urien Blackwood seeded.';
END
ELSE PRINT 'Urien Blackwood already exists.';
GO

PRINT 'House Pallor seed complete — 70 characters.';
GO
