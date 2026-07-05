SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- House Calyx — 70 Characters
-- Eastern plains, Danube basin. The oldest House. Conservative on Transmutation. Agricultural power.

-- 1. Vladimír Kossuth
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Vladimír Kossuth')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Vladimír Kossuth', N'vladimir-kossuth', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Vladimír Kossuth', N'vladimir-kossuth', N'Vladimír', N'Kossuth', N'Lord',
        N'human', N'human', N'male', N'he/him', 62, N'alive',
        N'Lord of House Calyx; senior political authority of the eastern plains',
        N'Vladimír Kossuth has ruled the eastern plains for nineteen years with the patience of a man who has watched empires fall. He was present as a young aide when Sinter destroyed itself — one of fewer than a dozen witnesses who returned alive from that territory. His memory of what he saw has never been committed to any record he did not personally control. He attends every Compact ceremony with the gravity of a man who helped write the liturgy himself, and speaks in council with the cadence of someone who learned early that most things said in rooms of power are said to be forgotten. He has three children by two marriages; both marriages were political; the children are not.',
        N'Vladimír is the living connection to the pre-Sinter era. His memory is a political weapon that every other House wants access to, and he knows it.',
        N'No POV.',
        N'House Calyx; eastern plains, Danube basin',
        182, 89, N'stocky',
        N'white', N'close-cropped', N'short',
        N'grey', N'warm olive', N'weathered',
        N'none',
        N'Still and deliberate; sits or stands at the same measured weight regardless of circumstance',
        N'Dark wool robes with the Calyx sigil worked in bronze thread; no military insignia despite thirty years of war',
        N'none',
        N'Morning: reviews intelligence dispatches alone. Afternoon: council sessions or correspondence. Evening: the locked records archive where no aide is permitted.',
        N'He saw Sinter die. What he actually witnessed contradicts the official history in two specific particulars — one of which would destabilize a current alliance if spoken aloud. He has carried this for fifty years and considers it a responsibility, not a burden. He has recently begun to wonder who he will tell before he dies.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Calyx capital estates; occasional diplomatic travel to Atrament',
        N'0', N'0',
        N'Old Eastern European lord in dark wool robes, white close-cropped hair, olive weathered face, still deliberate posture, candlelit stone council chamber, medieval fantasy, dark atmospheric --ar 2:3',
        N'A 62-year-old lord in dark wool robes with a bronze sigil, white hair, weathered olive skin, sitting quietly in a stone archive chamber, medieval fantasy',
        0, 0
    );
    PRINT 'Vladimír Kossuth seeded.';
END
ELSE PRINT 'Vladimír Kossuth already exists.';
GO

-- 2. Dragomira Fehér
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Dragomira Fehér')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Dragomira Fehér', N'dragomira-feher', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Dragomira Fehér', N'dragomira-feher', N'Dragomira', N'Fehér', N'Dame',
        N'human', N'human', N'female', N'she/her', 47, N'alive',
        N'Senior Calyx military commander; Knight; commands the northern border regiment',
        N'Dragomira Fehér has fought in six named engagements and survived all of them, which is an unremarkable number until you learn that three of the six were retreats. She received her infusion at thirty-one and the enhancement was modest — two inches of height, denser bone, reflexes that edge past natural limits. She commands the northern regiment with the particular authority of someone who has earned the right not to explain herself. Her soldiers are loyal in the specific way soldiers are loyal to an officer they believe will not spend them unnecessarily.',
        N'Dragomira is a test of what loyalty means under institutional pressure. She let an Oathless unit escape a killing field because she recognized their commander. That decision follows her.',
        N'No POV.',
        N'House Calyx; northern border plains, Danube basin',
        175, 78, N'military-lean',
        N'dark brown', N'pulled back', N'long',
        N'brown', N'medium brown', N'scarred',
        N'Subtle height gain; increased skeletal density visible in jaw and brow; vascular prominence on forearms',
        N'Economical and forward-weighted; moves like someone who has made peace with being watched',
        N'Field uniform, dark wool, functional; the Dame''s badge worn low on the chest rather than at the throat',
        N'Knight — first infusion at thirty-one; moderate enhancement',
        N'Morning drills with the regiment. Reviews dispatch reports. Makes rounds of the fortification line before dusk. Reads after dark.',
        N'Seven years ago she recognized the Oathless commander leading a unit across her killing field — her training partner from her first year in service. She gave an order that cleared the eastern flank. The Oathless unit escaped. The official record says the wind shifted and obscured the signal. She has not spoken to that commander since, and does not know if he is still alive.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Northern Calyx border; occasional travel to the capital for command councils',
        N'0', N'0',
        N'Middle-aged Eastern European woman knight in dark wool field uniform, long dark hair pulled back, scarred face, forward-weighted military posture, stone fortification wall background, medieval fantasy, Buehlman register --ar 2:3',
        N'A 47-year-old woman knight in dark wool field uniform, long dark hair, scarred face, standing at a stone fortification in Eastern European medieval fantasy style',
        0, 0
    );
    PRINT 'Dragomira Fehér seeded.';
END
ELSE PRINT 'Dragomira Fehér already exists.';
GO

-- 3. Bogdan Sárosi
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Bogdan Sárosi')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Bogdan Sárosi', N'bogdan-sarosi', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Bogdan Sárosi', N'bogdan-sarosi', N'Bogdan', N'Sárosi', N'',
        N'human', N'human', N'male', N'he/him', 78, N'alive',
        N'Retired Calyx Myrmidon; elder veteran of the Battle of Three Rivers; estate pensioner',
        N'Bogdan Sárosi is old enough that the pension he draws was set at a rate from before the last grain adjustment, and no one has corrected it in his favor or against. He was in the Battle of Three Rivers at twenty-six — one of the defining catastrophes of the Calyx military history, where the regiment was caught on a ford crossing and destroyed by Fornax archers. He survived. He has been asked about this survival many times over fifty years, and his answer has never been the same twice, which people take for the inconsistency of age.',
        N'Bogdan is the shame hidden inside a heroic story. He knows exactly how he survived and has never told anyone, and the not-telling has become its own kind of monument.',
        N'No POV.',
        N'House Calyx; eastern plains, Danube basin',
        168, 71, N'lean',
        N'white', N'unkempt', N'short',
        N'faded blue', N'pale', N'deeply weathered',
        N'none',
        N'Slow and careful on his feet; favors his left side without acknowledging it; looks at exits',
        N'Old Myrmidon''s coat worn soft from decades; a pension token on a cord around his neck',
        N'none',
        N'Sits in the estate courtyard when weather permits. Tends a small garden he planted twenty years ago. Drinks more than he eats. Talks freely to anyone who will listen about everything except the ford crossing.',
        N'He survived Three Rivers by pulling himself under the bodies of the dead and lying still for two days in water that turned red and then brown. He counted his breathing so he would not drown. He has not told anyone this because he was found downstream being called a hero, and he let that stand. The shame of it is not that he hid — anyone would have hidden — but that he never corrected the story when it would have mattered.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Eastern Calyx estates; rarely travels',
        N'0', N'0',
        N'Very old Eastern European man, white unkempt hair, pale deeply weathered face, worn soldier''s coat, slow careful posture, estate courtyard garden, medieval fantasy, melancholic --ar 2:3',
        N'A 78-year-old retired soldier, white hair, weathered pale face, worn old coat, sitting in a medieval estate courtyard garden',
        0, 0
    );
    PRINT 'Bogdan Sárosi seeded.';
END
ELSE PRINT 'Bogdan Sárosi already exists.';
GO

-- 4. Ilona Vásárhelyi
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ilona Vásárhelyi')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ilona Vásárhelyi', N'ilona-vasarhelyi', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Ilona Vásárhelyi', N'ilona-vasarhelyi', N'Ilona', N'Vásárhelyi', N'',
        N'human', N'human', N'female', N'she/her', 34, N'alive',
        N'Calyx Scrying installation operator; assigned to long-observation Sphere 31 monitoring post',
        N'Ilona Vásárhelyi has been observing the same Sphere 31 location — a cluster of farmhouses in a river valley — for two years and four months. Her assignment was routine: catalog agricultural practices, note any technology worth reporting to the design corps. She has filed forty-three reports in that time. In none of them does she mention that she has begun to recognize the inhabitants by name, by the rhythm of their days, by the way one woman hangs the washing differently in winter. She tells herself she is being thorough.',
        N'Ilona is an observer who has forgotten she is supposed to be invisible. Her attachment to a family she cannot speak to or protect is a quiet catastrophe building toward a moment when she will have to choose what she is willing to do.',
        N'No POV.',
        N'House Calyx; Scrying installation, eastern plains',
        163, 58, N'average',
        N'auburn', N'braided', N'long',
        N'green', N'fair', N'clear',
        N'none',
        N'Slightly hunched from hours at the observation lens; startles easily when spoken to from behind',
        N'Operator''s grey tunic and trousers; ink-stained fingers; a small pouch of personal items worn on the belt',
        N'none',
        N'Six-hour observation shifts, twice daily. Files reports. Eats in the operator''s mess. Spends her free hours reviewing old observation notes from her assigned Sphere, ostensibly for pattern analysis.',
        N'She has named the family she watches. The grandmother is Marta. The youngest child is called Pip because she couldn''t make out the real name. She knows this is a violation of operator protocol and possibly of something more fundamental. She has started keeping a private journal of Sphere 31 days she has witnessed, written in a personal cipher. The journal is hidden in her bunk.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Eastern Calyx Scrying installation; rarely leaves',
        N'0', N'0',
        N'Young Eastern European woman in grey operator''s tunic, auburn braided hair, green eyes, hunched slightly over a Scrying lens, dimly lit stone chamber, medieval fantasy --ar 2:3',
        N'A 34-year-old woman in grey uniform with auburn braided hair and ink-stained fingers, leaning over a glowing Scrying lens in a dark stone chamber',
        0, 0
    );
    PRINT 'Ilona Vásárhelyi seeded.';
END
ELSE PRINT 'Ilona Vásárhelyi already exists.';
GO

-- 5. Miroslav Kovač
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Miroslav Kovač')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Miroslav Kovač', N'miroslav-kovac', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Miroslav Kovač', N'miroslav-kovac', N'Miroslav', N'Kovač', N'Master',
        N'human', N'human', N'male', N'he/him', 51, N'alive',
        N'Calyx Transmutation practitioner; licensed infusion administrator; northern province',
        N'Miroslav Kovač has administered the Catalyst to twenty-three Calyx soldiers and officers over seventeen years of practice. Five survived. He keeps a ledger — not the official record, which tracks dosage and timing, but his own, which records the names of the dead and a line about what they told him they hoped for when they lived. Some hoped for simple things. A farm. A child''s wedding. One woman wanted only to visit a city she had heard of but never seen. He has never shown the ledger to anyone. He performs the infusion ceremony with scrupulous care and a steady hand, and after each death he adds a line to the ledger and does not drink for a week.',
        N'Miroslav is the moral weight of the Transmutation system made personal. He is the practitioner who has not become numb, and that is both what makes him exceptional and what is slowly breaking him.',
        N'No POV.',
        N'House Calyx; northern province infusion facility',
        177, 82, N'average',
        N'salt-and-pepper', N'combed back', N'short',
        N'dark brown', N'medium olive', N'lined',
        N'none',
        N'Precise and careful; a slight tremor in his right hand that he controls by pressing it against surfaces when he thinks no one is watching',
        N'Practitioner''s dark coat, always clean; the licensed infusion seal on a chain at his breast',
        N'none',
        N'Prepares infusion materials in the mornings. Consults with candidates in the afternoons. Writes in the ledger on nights when there is something to record. Reads Calyx ritual texts he was given by a patient who died.',
        N'The ledger. He has memorized every name. On the anniversary of his first patient''s death — a nineteen-year-old soldier named Péter — he goes to the river and sits for an hour. He has also begun to wonder if he is selecting candidates badly: whether the survival rate could be improved if he refused more people, or whether the 80% death rate is simply the cost of the thing and cannot be improved without better science than the Cauld currently possesses. He does not know which is true. He is afraid to find out.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Northern Calyx province; occasional travel to capital for certification review',
        N'0', N'0',
        N'Middle-aged Slavic man in a clean dark practitioner''s coat, salt-and-pepper hair combed back, deeply lined face, precise careful posture, stone medical chamber, medieval fantasy, somber --ar 2:3',
        N'A 51-year-old Transmutation practitioner in a dark coat with a certification seal, salt-and-pepper hair, lined face, standing carefully in a stone infusion chamber',
        0, 0
    );
    PRINT 'Miroslav Kovač seeded.';
END
ELSE PRINT 'Miroslav Kovač already exists.';
GO

-- 6. Petronela Orbán
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Petronela Orbán')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Petronela Orbán', N'petronela-orban', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Petronela Orbán', N'petronela-orban', N'Petronela', N'Orbán', N'',
        N'human', N'human', N'female', N'she/her', 55, N'alive',
        N'Senior estate administrator; manages grain allocation and storage for the eastern Calyx estates',
        N'Petronela Orbán has been managing grain stores for twenty-two years and has never once failed to deliver the House''s expected tithe. This is considered a small miracle given the variability of harvests in the eastern plains, and she accepts the praise with the composure of someone who knows how she has achieved it. She keeps meticulous records, double-checked and triple-signed. She is known as someone who cannot be deceived about a number.',
        N'Petronela is the story of what institutional loyalty looks like from the inside when it stops being enough — a woman who has prepared for the House to lose before anyone else in the House has admitted the possibility.',
        N'No POV.',
        N'House Calyx; eastern estate administration complex',
        161, 74, N'stocky',
        N'dark grey', N'pinned up', N'medium',
        N'brown', N'warm tan', N'clear',
        N'none',
        N'Brisk and efficient; always seems to know where the nearest exit is',
        N'Practical wool dress in Calyx brown; an administrator''s satchel always over one shoulder',
        N'none',
        N'Audits grain inventories in the mornings. Meets with estate stewards in the afternoons. Reviews the secret reserve ledger alone, after dark, in a room to which only she has a key.',
        N'For nine years she has been skimming a small, very precise percentage from the official tithe — within the margin of acceptable shrinkage — and diverting it to a hidden reserve in an estate granary she has marked as structurally unsound on the public records. The reserve is now large enough to feed two hundred people for a year. She intends to use it if Calyx loses. She does not think of this as treason. She thinks of it as arithmetic.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Eastern Calyx estate complex; rarely travels beyond administrative district',
        N'0', N'0',
        N'Middle-aged Eastern European woman administrator in practical wool dress, dark grey hair pinned up, warm tan complexion, brisk efficient posture, stone estate office surrounded by grain records, medieval fantasy --ar 2:3',
        N'A 55-year-old estate administrator in brown wool with grey hair and a heavy satchel, surrounded by grain ledgers in a stone office, medieval fantasy',
        0, 0
    );
    PRINT 'Petronela Orbán seeded.';
END
ELSE PRINT 'Petronela Orbán already exists.';
GO

-- 7. Csilla Eszterházy
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Csilla Eszterházy')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Csilla Eszterházy', N'csilla-esterhazy', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Csilla Eszterházy', N'csilla-esterhazy', N'Csilla', N'Eszterházy', N'',
        N'human', N'human', N'female', N'she/her', 29, N'alive',
        N'Junior archivist; Calyx historical records division; Sinter Crisis documentation',
        N'Csilla Eszterházy came to the archives from the eastern university at twenty-three, assigned to cross-reference the Sinter Crisis records as part of a systematic preservation project. She is efficient, thorough, and in possession of a scholar''s particular kind of courage — the kind that keeps going even when the document tells you something you did not want to find. She is the youngest person to be given unsupervised access to the sealed Crisis records in the archive''s history, which she was told was because of her excellent work. She has since wondered whether it was because no one expected her to read them carefully.',
        N'Csilla has found what should not be there. The crisis point is whether she tells someone, and if so, who — knowing that the wrong choice could make her evidence disappear along with her.',
        N'No POV.',
        N'House Calyx; capital archive complex',
        166, 57, N'lean',
        N'black', N'loose', N'long',
        N'dark brown', N'warm brown', N'clear',
        N'none',
        N'Quietly focused; the stillness of someone reading even when they are not reading',
        N'Scholar''s plain dark dress; always ink on the side of her left hand',
        N'none',
        N'Works in the sealed records room from morning to midday. Transcribes in the open archive in the afternoons. Eats late. Reviews her private notes before sleeping.',
        N'She found records in the Sinter Crisis file showing that a Calyx agent was present inside Sinter''s capital in the final weeks before the collapse — not as a witness, but as an active party. The official history says Calyx learned of Sinter''s fall the same week as everyone else. The document she found is dated three months earlier. She has made a copy and hidden it, and she has told no one because the document bears a seal she recognizes as still active.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Calyx capital; the archive complex',
        N'0', N'0',
        N'Young Eastern European woman scholar in dark dress, black loose long hair, ink-stained hand, very still focused posture, candlelit stone archive filled with old documents, medieval fantasy --ar 2:3',
        N'A 29-year-old archivist with black hair and ink-stained fingers, sitting very still among old documents in a candlelit stone archive room, medieval fantasy',
        0, 0
    );
    PRINT 'Csilla Eszterházy seeded.';
END
ELSE PRINT 'Csilla Eszterházy already exists.';
GO

-- 8. Tibor Halász
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Tibor Halász')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Tibor Halász', N'tibor-halasz', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Tibor Halász', N'tibor-halasz', N'Tibor', N'Halász', N'',
        N'human', N'human', N'male', N'he/him', 19, N'alive',
        N'Calyx Myrmidon; junior conscript; first campaign, northern border regiment',
        N'Tibor Halász is eight months into his first campaign and has not yet been in a real engagement, only a skirmish at a river crossing where he threw his spear and did not see where it landed. He comes from a fishing village in the eastern lowlands, where fish were pulled from cold water with nets and patience. His older brother Andor was offered infusion two years ago at twenty-one, accepted without hesitation, and died on the third day. Tibor knows this. He has been asked twice already whether he would accept infusion when eligible, and both times he has said he doesn''t know yet, which his sergeant considers an acceptable answer for now.',
        N'Tibor is the cost of the Transmutation system made visible and young — the boy watching what the House asks of him and not yet knowing what he will answer.',
        N'No POV.',
        N'House Calyx; northern border regiment camp',
        172, 65, N'lean',
        N'light brown', N'short and practical', N'short',
        N'blue', N'fair', N'clear',
        N'none',
        N'Slightly too alert; the tension of someone listening for something he doesn''t know the name of',
        N'Standard Myrmidon''s field kit, slightly too large in the shoulders; a fishing cord bracelet on his left wrist',
        N'none',
        N'Drills, sentry duty, camp maintenance. Writes letters home that he has not sent yet because he doesn''t know what to say.',
        N'He has already decided he will refuse infusion if it is offered. He made this decision standing at his brother''s grave. He has not told anyone because he does not know what happens to Myrmidons who refuse — whether it is permitted or whether it marks you. He is waiting to see what happens to someone else first.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Northern Calyx border regiment; eastern plains origin',
        N'0', N'0',
        N'Young Eastern European soldier boy, light brown hair, blue eyes, slightly too-large Myrmidon field kit, a cord bracelet on his wrist, alert watchful expression, military camp background, medieval fantasy --ar 2:3',
        N'A 19-year-old conscript soldier with light brown hair and blue eyes, wearing a slightly large military uniform and a cord bracelet, in a medieval camp setting',
        0, 0
    );
    PRINT 'Tibor Halász seeded.';
END
ELSE PRINT 'Tibor Halász already exists.';
GO

-- 9. Erzsébet Nádor
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Erzsébet Nádor')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Erzsébet Nádor', N'erzsebet-nador', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Erzsébet Nádor', N'erzsebet-nador', N'Erzsébet', N'Nádor', N'',
        N'human', N'human', N'female', N'she/her', 41, N'alive',
        N'Calyx intelligence officer; courier liaison and diplomatic correspondence analyst',
        N'Erzsébet Nádor is one of three people in Calyx''s intelligence apparatus with authorization to read diplomatic correspondence before it is routed. She has used this position honestly and with genuine skill for fourteen years. She has also been feeding selected pieces of that correspondence to an Atrament contact for seven of those fourteen years, through a series of exchanges that began when she was asked a question she could not refuse answering. She has since had several opportunities to stop and has not taken them, which she has explained to herself in different ways at different times.',
        N'Erzsébet is the double agent who believes her own justifications. The story is whether she is right — whether what she has given Atrament has actually served some larger purpose — or whether she has simply been useful to someone else''s agenda for seven years.',
        N'No POV.',
        N'House Calyx; capital intelligence division',
        167, 63, N'average',
        N'chestnut brown', N'neatly arranged', N'medium',
        N'hazel', N'medium olive', N'clear',
        N'none',
        N'Composed and socially fluent; makes eye contact in a way that seems open and is not',
        N'Well-maintained practical dress appropriate to her station; a leather document wallet on her belt',
        N'none',
        N'Reads and routes diplomatic correspondence in the mornings. Meets couriers and liaisons. Writes analyses for senior staff. Meets her Atrament contact twice monthly through an intermediary she has never seen face-to-face.',
        N'She began passing information because she believed Calyx''s senior staff was making a catastrophic strategic error and that Atrament needed to know in order to prevent a war that would kill more people than the secret. She may have been right. The error was corrected, though she cannot know if she caused the correction. She has continued passing information since because stopping is now more dangerous than continuing, and because some part of her has accepted that she works for two masters and tells herself the masters'' interests are aligned. They are not always aligned.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Calyx capital; diplomatic circuit',
        N'0', N'0',
        N'Composed Eastern European woman intelligence officer, chestnut hair neatly arranged, hazel eyes, olive complexion, well-maintained dress with a leather document wallet, candlelit correspondence room, medieval fantasy --ar 2:3',
        N'A 41-year-old woman intelligence officer with chestnut hair and hazel eyes, composed expression, holding documents in a medieval stone correspondence room',
        0, 0
    );
    PRINT 'Erzsébet Nádor seeded.';
END
ELSE PRINT 'Erzsébet Nádor already exists.';
GO

-- 10. Zoltán Fekete
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Zoltán Fekete')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Zoltán Fekete', N'zoltan-fekete', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Zoltán Fekete', N'zoltan-fekete', N'Zoltán', N'Fekete', N'',
        N'human', N'human', N'male', N'he/him', 53, N'alive',
        N'Senior Calyx military officer; Paladin; commands the central strategic reserve',
        N'Zoltán Fekete is the most visibly enhanced officer in Calyx''s active command — six feet five, his proportions subtly wrong to an eye accustomed to ordinary men, his veins prominent in his neck and forearms even through cloth. He has undergone three infusions over twenty years. After each one he spent a month in recovery and returned to duty with more capability and less of what he privately identifies as hesitation. He commands the strategic reserve with a decisiveness that his officers admire and that he himself is beginning to question.',
        N'Zoltán is the Paladin who can feel his mind changing and cannot determine whether the new version of him makes better decisions or simply faster ones. He is the argument against Transmutation made from the inside.',
        N'No POV.',
        N'House Calyx; central strategic reserve, Danube basin',
        196, 112, N'military-lean',
        N'black', N'close-cropped', N'short',
        N'dark amber', N'deep brown', N'scarred',
        N'Evident enhancement — significant height, altered proportions; vascular prominence visible at neck and forearms; eyes carry an amber cast not present in unenhanced persons',
        N'Carries weight differently than unenhanced men; deliberate and slightly too-still between movements',
        N'Officer''s dark uniform, Calyx colors; no decorative elements; the Paladin''s mark on his right hand',
        N'Paladin — three infusions over twenty years; significant physical enhancement',
        N'Command briefings at dawn. Reviews tactical maps. Walks the reserve lines. Trains alone after dark for an hour.',
        N'He cannot sleep more than three hours without waking with thoughts that feel borrowed — tactical assessments so rapid and cold they do not feel like thinking, they feel like receiving. He does not know whether this is a consequence of Transmutation or simply who he has become. He has not reported this to the infusion practitioners because he suspects reporting it would end his command. He is also afraid they would tell him it is normal.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Central Calyx; strategic reserve command',
        N'0', N'0',
        N'Tall imposing Eastern European Paladin officer, black close-cropped hair, amber eyes, visibly enhanced proportions and vascular prominence, dark military uniform, strategic map table, medieval fantasy, ominous --ar 2:3',
        N'A 53-year-old Paladin military commander, very tall with subtly wrong proportions, black hair, amber eyes, dark uniform, standing over a medieval tactical map',
        0, 0
    );
    PRINT 'Zoltán Fekete seeded.';
END
ELSE PRINT 'Zoltán Fekete already exists.';
GO

-- 11. Anasztázia Teleki
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Anasztázia Teleki')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Anasztázia Teleki', N'anasztazia-teleki', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Anasztázia Teleki', N'anasztazia-teleki', N'Anasztázia', N'Teleki', N'Mistress',
        N'human', N'human', N'female', N'she/her', 38, N'alive',
        N'Liturgy transit officer; assigned to Calyx district; processes Sphere 31 arrivals',
        N'Anasztázia Teleki has been the Liturgy''s transit processing officer in the Calyx district for nine years. Her job is to receive persons extracted from Sphere 31, assess their condition, assign labor classifications, and route them to their designated placements. She does this with the efficiency her posting requires. She also, for the past three years, has been quietly adjusting the labor classifications of one extended family — a mother, her two adult sons, and an elderly grandmother — to ensure they are assigned to the same estate rather than split across three provinces. She does not know these people. She has never met them. She read their file and made a decision.',
        N'Anasztázia is the small human act inside an inhuman system — and the story is what happens when the small act is noticed.',
        N'No POV.',
        N'House Calyx; Liturgy transit processing station, eastern district',
        164, 61, N'average',
        N'blonde', N'pinned', N'medium',
        N'grey', N'fair', N'clear',
        N'none',
        N'Bureaucratically composed; handles paper with the fluency of someone who has learned to make decisions look routine',
        N'Liturgy officer''s grey uniform; the transit authority seal on a cord',
        N'none',
        N'Processes transit files from morning through midday. Conducts arrival interviews in the afternoons. Reviews the family''s reassignment records before filing them — takes thirty seconds, changes a code, moves on.',
        N'She cannot explain why this family specifically. The file moved her in a way she cannot articulate. The grandmother was described in the intake notes as asking, three times during the transit, whether her family was safe. Anasztázia has been asking herself for three years whether what she is doing is mercy or self-indulgence — because the system takes thousands and she has helped four. If she is discovered, she will face reassignment at minimum and investigation at worst. She has not stopped.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Calyx Liturgy district office; transit processing station',
        N'0', N'0',
        N'Young Eastern European woman in Liturgy grey uniform, blonde pinned hair, grey eyes, bureaucratically composed expression, stone transit office with stacked files, medieval fantasy --ar 2:3',
        N'A 38-year-old Liturgy transit officer in grey uniform with blonde pinned hair, sitting at a stone desk covered in transit files, medieval fantasy',
        0, 0
    );
    PRINT 'Anasztázia Teleki seeded.';
END
ELSE PRINT 'Anasztázia Teleki already exists.';
GO

-- 12. Béla Szabó
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Béla Szabó')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Béla Szabó', N'bela-szabo', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Béla Szabó', N'bela-szabo', N'Béla', N'Szabó', N'',
        N'human', N'human', N'male', N'he/him', 44, N'alive',
        N'Grain merchant; controls eastern Calyx trade routes; licensed buyer for House Calyx estates',
        N'Béla Szabó is the kind of man who is always a little more helpful than necessary, which is how you get people to stop looking at you carefully. He operates a grain brokerage that handles contracts for eleven eastern estates and has done so for sixteen years with a spotless record. He is genuinely personable and a good host. He has also been using a network of shell proxies — family members, distant relations, old business partners — to quietly acquire land from estates forced to sell in lean years or war-pressure situations. He now owns, through those proxies, approximately a fifth of the best farmland in eastern Calyx. No record names him as owner of all of it.',
        N'Béla is the slow accumulation of power through the gaps that war creates. He is not a villain; he is someone who saw an opportunity and kept taking it, and has not yet been asked to reckon with what he has become.',
        N'No POV.',
        N'House Calyx; eastern trade routes, Danube basin',
        179, 94, N'stocky',
        N'brown', N'neat', N'short',
        N'brown', N'warm tan', N'clear',
        N'none',
        N'Expansive and hospitable; uses physical space to suggest generosity',
        N'Prosperous merchant''s clothing, practical but well-made; a merchant guild token at his belt',
        N'none',
        N'Meets with estate stewards and buyers in the mornings. Reviews proxy accounts in the afternoons. Entertains regularly — good food, conversation, carefully selected guests.',
        N'He started buying land to have something real after a bad trade year. He continued because it worked. He is now at the point where unwinding the network would require him to explain how it was constructed, which would expose the acquisitions. He has also realized that the farmland gives him leverage over noble families who are technically his superiors, and he has begun to use that leverage in very small ways. He tells himself he has not crossed any line. He is wrong.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Eastern Calyx; trade circuit between estates',
        N'0', N'0',
        N'Prosperous middle-aged Eastern European grain merchant, brown neat hair, warm tan complexion, well-made practical clothing, expansive hospitable posture, medieval trading hall, fantasy --ar 2:3',
        N'A 44-year-old grain merchant with neat brown hair, warm tan skin, well-made clothes, standing expansively in a medieval trading hall',
        0, 0
    );
    PRINT 'Béla Szabó seeded.';
END
ELSE PRINT 'Béla Szabó already exists.';
GO

-- 13. Katalin Mészáros
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Katalin Mészáros')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Katalin Mészáros', N'katalin-meszaros', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Katalin Mészáros', N'katalin-meszaros', N'Katalin', N'Mészáros', N'',
        N'human', N'human', N'female', N'she/her', 36, N'alive',
        N'Calyx field healer; attached to the northern border regiment',
        N'Katalin Mészáros has been treating soldiers for eleven years and knows how to read a wound the way a scholar reads a document — what it says about how the engagement went, what weapons were used, what the distance was, which side was advancing when the injury was made. She has learned something she was not meant to learn: some of the wounds coming in from what is officially described as Fornax engagement are not consistent with Fornax weapons or Fornax tactics. They are consistent with Calyx weapons. She knows what interrogation injuries look like. She has kept this knowledge to herself.',
        N'Katalin is the healer who has accumulated evidence of a crime she cannot name without implicating people she cannot reach. Her story is whether she keeps her silence until silence becomes complicity.',
        N'No POV.',
        N'House Calyx; northern border regiment medical station',
        162, 60, N'average',
        N'dark brown', N'tied back', N'medium',
        N'brown', N'medium brown', N'weathered',
        N'none',
        N'Efficient and matter-of-fact with patients; watchful reserve with officers',
        N'Healer''s grey apron over plain dress; always carries a kit satchel; sleeves rolled to the elbow',
        N'none',
        N'Morning treatment rounds. Surgical cases when they arise — which is often. Writes patient notes in a private shorthand that only she can read.',
        N'She has identified seven soldiers in the past two years with wounds she cannot reconcile with the official account of where they were taken. Three of them told her nothing. Two of them told her things she wishes she had not heard, which she recorded in the private shorthand and then rewrote in a cipher she constructed herself. She is not sure what she is going to do with what she knows. She is sure she is not ready to do nothing.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Northern Calyx border; regiment medical station',
        N'0', N'0',
        N'Practical Eastern European woman healer, dark brown tied-back hair, rolled sleeves, grey apron over plain dress, weathered face, field medical station with wounded soldiers, medieval fantasy --ar 2:3',
        N'A 36-year-old field healer with dark tied-back hair, rolled sleeves, and a grey apron, treating patients in a medieval stone medical station',
        0, 0
    );
    PRINT 'Katalin Mészáros seeded.';
END
ELSE PRINT 'Katalin Mészáros already exists.';
GO

-- 14. Gábor Horváth
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Gábor Horváth')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Gábor Horváth', N'gabor-horvath', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Gábor Horváth', N'gabor-horvath', N'Gábor', N'Horváth', N'Master',
        N'human', N'human', N'male', N'he/him', 48, N'alive',
        N'Master engineer; maintains Calyx Scrying installations; eastern district supervisor',
        N'Gábor Horváth has been maintaining the Calyx Scrying installations for twenty years. He understands their construction better than any Liturgy official who has visited to inspect them, which is a diplomatic friction he manages by appearing deferential. He is not deferential. He has found something in the easternmost installation''s structure that does not appear in any construction record: a membrane interface geometry that is not built — it formed. The installation was built around it. He has been trying to understand this for three years without telling anyone.',
        N'Gábor is the engineer who found a fact that rewrites the history of how Scrying installations work. What he does with it — and who finds out first — is the story.',
        N'No POV.',
        N'House Calyx; Scrying installation network, eastern district',
        180, 88, N'stocky',
        N'grey-brown', N'unkempt', N'short',
        N'green', N'fair', N'weathered',
        N'none',
        N'Hands-first; approaches problems physically before analytically; tool calluses on both palms',
        N'Working engineer''s heavy coat, always worn; tool belt; chalk dust on his sleeves',
        N'none',
        N'Inspection rounds of the installations — covers three sites weekly. Performs calibrations. Spends extra hours at the easternmost installation, ostensibly on maintenance tasks he has listed as routine.',
        N'The formed membrane interface in the eastern installation predates the installation by at least two hundred years based on the mineral deposits around it. This means either a natural Scrying site exists — something the Liturgy claims is impossible — or someone built a Scrying installation here before the current calendar and the record was erased. Either possibility unsettles him in different ways. He has made measurements and drawings, hidden them in a waterproof case in the installation''s maintenance shaft, and told no one.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Eastern Calyx Scrying installation network',
        N'0', N'0',
        N'Middle-aged Eastern European engineer in heavy work coat with tool belt, grey-brown unkempt hair, green eyes, chalk-dusted sleeves, Scrying installation stone chamber with geometric membrane apparatus, medieval fantasy --ar 2:3',
        N'A 48-year-old master engineer with grey-brown hair and a tool belt, examining a glowing geometric membrane apparatus in a medieval stone installation chamber',
        0, 0
    );
    PRINT 'Gábor Horváth seeded.';
END
ELSE PRINT 'Gábor Horváth already exists.';
GO

-- 15. Veronika Batthyány
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Veronika Batthyány')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Veronika Batthyány', N'veronika-batthyany', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Veronika Batthyány', N'veronika-batthyany', N'Veronika', N'Batthyány', N'Lady',
        N'human', N'human', N'female', N'she/her', 57, N'alive',
        N'Calyx diplomatic envoy to Atrament; senior political representative',
        N'Veronika Batthyány has spent fifteen years as Calyx''s primary diplomatic face to Atrament, which has made her one of the most sophisticated political operators in either territory. She is fluent in three languages and in the specific grammar of what goes unsaid. She knows that Atrament''s intelligence apparatus is running at least one agent inside Calyx''s senior staff. She identified this probability eighteen months ago through patterns in which information Atrament negotiators had and which they did not. She has not told anyone in Calyx — not because she is not loyal, but because she is hunting the agent herself and telling anyone would collapse the hunt.',
        N'Veronika is the diplomat who is now running a counter-intelligence operation she has no official authority to run, for reasons she has not fully examined, because she has learned that power over information is the only power that lasts.',
        N'No POV.',
        N'House Calyx; Atrament diplomatic circuit; capital for reporting',
        170, 68, N'lean',
        N'silver', N'elaborately pinned', N'long',
        N'grey-blue', N'pale olive', N'clear',
        N'none',
        N'Precisely measured; every gesture positioned; at rest she still looks like she is composing a sentence',
        N'Diplomatic formal dress in Calyx colors, always correct to the occasion; a House sigil ring on her right hand',
        N'none',
        N'Diplomatic meetings, correspondence, formal dinners. Also, since eighteen months ago: quiet interviews with couriers, careful review of which Calyx staff members are present when specific information crosses the channel, a private log she writes in a cipher she invented at forty-three.',
        N'She knows who the Atrament agent is. She identified them two months ago. She has not arrested them because she has been feeding the agent carefully selected false information for the past six weeks and wants to see how Atrament uses it before she closes the operation. She is aware this decision is not sanctioned by anyone. She is also aware that if the agent is someone senior, the arrest will be a political event that will not be in her control. The counter-operation is partly intelligence work and partly, she acknowledges to herself alone, a desire to stay in control of the ending.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Atrament diplomatic circuit; Calyx capital',
        N'0', N'0',
        N'Elegant older Eastern European noblewoman diplomat, silver elaborately pinned hair, grey-blue eyes, pale olive skin, formal dress in dark Calyx colors, precise poised posture, diplomatic hall, medieval fantasy --ar 2:3',
        N'A 57-year-old diplomat with silver pinned hair and grey-blue eyes, precisely poised in formal dark dress, in a medieval diplomatic reception hall',
        0, 0
    );
    PRINT 'Veronika Batthyány seeded.';
END
ELSE PRINT 'Veronika Batthyány already exists.';
GO

-- 16. László Pálffy
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'László Pálffy')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'László Pálffy', N'laszlo-palffy', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'László Pálffy', N'laszlo-palffy', N'László', N'Pálffy', N'',
        N'human', N'human', N'male', N'he/him', 26, N'alive',
        N'Junior Scrying operator; assigned to standard survey rotation, eastern installation',
        N'László Pálffy has been a Scrying operator for three years and is regarded as technically competent and unambitious — the kind of junior operator who files complete reports on time and does not request assignment changes. He was assigned a standard survey rotation eight months ago that included a Sphere he was told had been fully catalogued and required only monitoring for changes. He noticed something in that Sphere that should not be there: construction signatures in a membrane-adjacent zone that match no current House''s methodology, but match historical records he looked up afterward from Fornax''s pre-Compact era.',
        N'László has stumbled into evidence that another House has been using an officially catalogued Sphere without filing with the Liturgy. He is junior enough that reporting means almost nothing and not reporting means almost everything.',
        N'No POV.',
        N'House Calyx; eastern Scrying installation',
        174, 68, N'lean',
        N'light brown', N'short and neat', N'short',
        N'brown', N'medium', N'clear',
        N'none',
        N'Careful and methodical; the slightly contracted posture of someone who prefers not to be noticed',
        N'Standard operator''s grey tunic; a small measuring kit in a leather case at his belt',
        N'none',
        N'Observation shifts, report filing, equipment calibration. Extra hours reviewing the flagged Sphere on pretext of pattern analysis — officially logged as standard enhanced monitoring.',
        N'He has not reported what he found because he is not certain, and he is afraid that uncertainty will make him look like an operator who imagines things. He is also afraid that certainty will make him into someone who knows something dangerous. He has been very carefully not thinking about which House the construction signatures might belong to, and has been largely unsuccessful.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Eastern Calyx Scrying installation',
        N'0', N'0',
        N'Young Eastern European man in grey operator''s tunic, light brown neat hair, careful contracted posture, examining a Scrying lens in a dimly lit stone chamber, medieval fantasy --ar 2:3',
        N'A 26-year-old junior Scrying operator with light brown hair, leaning carefully over an observation lens in a medieval stone chamber',
        0, 0
    );
    PRINT 'László Pálffy seeded.';
END
ELSE PRINT 'László Pálffy already exists.';
GO

-- 17. Rozália Esterházy
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Rozália Esterházy')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Rozália Esterházy', N'rozalia-esterhazy', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Rozália Esterházy', N'rozalia-esterhazy', N'Rozália', N'Esterházy', N'',
        N'human', N'human', N'female', N'she/her', 62, N'alive',
        N'Head cook and provisions officer; regiment cookhouse, northern border',
        N'Rozália Esterházy has been feeding soldiers for thirty-five years. She has served under nine different commanders and survived the transition from each to the next because whoever is in charge needs to eat. She has an exact memory for faces and for what a person''s appetite tells her about how they are managing. A soldier who stops eating before a battle is about to break. A commander who eats more than usual when reading dispatches is afraid of what the dispatches say. She has never been wrong about this and has never been asked her opinion, which is fine with her.',
        N'Rozália holds more institutional knowledge about the regiment''s morale and leadership than any officer. The story is whether she ever uses it — and what happens when someone realizes she knows what she knows.',
        N'No POV.',
        N'House Calyx; northern border regiment cookhouse',
        158, 81, N'stocky',
        N'white', N'pinned up', N'medium',
        N'brown', N'warm tan', N'weathered',
        N'none',
        N'Economical and purposeful in the kitchen; reads a room the way she reads a fire — by what it is doing, not what it looks like',
        N'Cook''s heavy apron over practical wool; sleeves always rolled; flour or ash on her forearms',
        N'none',
        N'Pre-dawn fire lighting and morning meal. Supply inventory. Midday and evening meals. Talks to the soldiers who linger at the cookhouse after eating, which is most of them.',
        N'She knows that the current regiment commander has been eating nothing but bread and salt for six days, which she has seen before in men who have received orders they cannot execute with the resources they have. She has not told anyone. She is watching to see whether he eats in the morning or sends a dispatch rider first.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Northern Calyx border regiment',
        N'0', N'0',
        N'Older Eastern European woman cook, white pinned hair, warm tan weathered face, heavy apron, flour on her forearms, large stone cookhouse fire, regiment camp background, medieval fantasy --ar 2:3',
        N'A 62-year-old cook with white pinned hair and a heavy apron, standing at a large stone cookhouse fire in a medieval military camp',
        0, 0
    );
    PRINT 'Rozália Esterházy seeded.';
END
ELSE PRINT 'Rozália Esterházy already exists.';
GO

-- 18. Sándor Zichy
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Sándor Zichy')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Sándor Zichy', N'sandor-zichy', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Sándor Zichy', N'sandor-zichy', N'Sándor', N'Zichy', N'',
        N'human', N'human', N'male', N'he/him', 22, N'alive',
        N'Calyx Myrmidon; veteran of one major engagement; reassigned to eastern border regiment',
        N'Sándor Zichy survived his first major engagement by lying still in a frozen marsh for two days while the battle moved around him and then past him. He went in expecting to fight and instead found himself below the surface of the water with his face tilted at an angle that let him breathe, listening to sounds he cannot now describe accurately. When he was found downstream he told the truth about what happened. No one found this shameful. The engagement was a rout and there were no survivors from his original position, and survival in whatever form was considered fortunate. He was promoted and reassigned. He is now calculating in a way that concerns his new unit — patient, cold, and very interested in where the exits are.',
        N'Sándor is what survival does to a person who was not built to be a survivor. He has not decided whether what the marsh made him is something he wants to be.',
        N'No POV.',
        N'House Calyx; eastern border regiment',
        176, 72, N'lean',
        N'dark brown', N'close-cropped', N'short',
        N'dark brown', N'medium', N'clear',
        N'none',
        N'Still and watchful; identifies exits in every space he enters; the patience of someone who has learned that patience is sometimes all there is',
        N'Standard Myrmidon field kit, well maintained; his gear always secured and ready',
        N'none',
        N'Drills with the new unit. Sentry rotations. Sits at the edge of the fire at night rather than the center. Volunteers for night watch.',
        N'He does not know who he is now. The person who went into the marsh was frightened and confused. The person who came out was still frightened, but with a coldness underneath it. He has been thinking about whether he would do the same thing again — the hiding, the stillness, the survival — and has concluded that he would, and is not sure whether that means he is a coward or simply someone who understands the arithmetic of survival. He is waiting to be in another engagement to find out.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Eastern Calyx border regiment',
        N'0', N'0',
        N'Young Eastern European soldier, dark brown close-cropped hair, dark eyes, still watchful expression, lean build, standard military kit, sitting at a fire''s edge in a winter camp, medieval fantasy --ar 2:3',
        N'A 22-year-old soldier with close-cropped dark hair and very still watchful eyes, sitting at the edge of a campfire in a winter military camp',
        0, 0
    );
    PRINT 'Sándor Zichy seeded.';
END
ELSE PRINT 'Sándor Zichy already exists.';
GO

-- 19. Margit Apponyi
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Margit Apponyi')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Margit Apponyi', N'margit-apponyi', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Margit Apponyi', N'margit-apponyi', N'Margit', N'Apponyi', N'Dame',
        N'human', N'human', N'female', N'she/her', 39, N'alive',
        N'Dame; Calyx Knight; commands a border garrison outpost, western sector',
        N'Margit Apponyi received her infusion at thirty-three and survived with the moderate enhancement that most Calyx Knights carry — increased bone density, marginally heightened endurance, the slight height gain that marks the transformation. She commands a garrison outpost on the western sector with quiet competence and a reputation for being harder on herself than on her troops. She was married at twenty-five to a Myrmidon named Bálint. She filed his death in the field five years ago. The death record is accurate in that Bálint no longer serves House Calyx. It is inaccurate in that he is not dead.',
        N'Margit is the officer who committed a crime of the heart and has been living with its shape for five years. The story is whether Bálint''s continued existence — and her protection of it — can survive the war''s demands.',
        N'No POV.',
        N'House Calyx; western sector garrison',
        173, 74, N'athletic',
        N'auburn', N'braided and pinned', N'long',
        N'green', N'fair', N'scarred',
        N'Subtle height gain; increased skeletal density visible in posture and movement; vascular prominence on neck',
        N'Upright and precise in a way that reads as controlled rather than rigid; a slight economy to her movements',
        N'Dame''s field uniform; garrison sword at her hip; the Knight badge worn at the collar',
        N'Knight — first infusion at thirty-three; moderate enhancement',
        N'Morning garrison inspection. Administrative correspondence. Afternoon weapons practice with junior troops. Evening review of western sector intelligence reports.',
        N'Bálint went Oathless after refusing to execute a group of prisoners. She found out through a contact in the Oathless networks two months before she was required to file casualty records. She filed him as dead in an engagement that actually produced no casualties she knew of. She has heard nothing of him since and does not know if he survived his first year Oathless. The not-knowing is worse than the knowing was.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Western Calyx sector garrison',
        N'0', N'0',
        N'Determined Eastern European woman knight in field uniform, auburn hair braided and pinned, green eyes, scarred face, Knight badge at collar, stone garrison wall background, medieval fantasy --ar 2:3',
        N'A 39-year-old woman knight with auburn braided hair, green eyes, and a scarred face, wearing a dark field uniform with a knight badge at her collar at a stone garrison',
        0, 0
    );
    PRINT 'Margit Apponyi seeded.';
END
ELSE PRINT 'Margit Apponyi already exists.';
GO

-- 20. Kristóf Nádasdy
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Kristóf Nádasdy')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Kristóf Nádasdy', N'kristof-nadasdy', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Kristóf Nádasdy', N'kristof-nadasdy', N'Kristóf', N'Nádasdy', N'',
        N'human', N'human', N'male', N'he/him', 33, N'alive',
        N'Oathless; former Calyx Myrmidon; operates as a terrain guide for border refugees',
        N'Kristóf Nádasdy was a Calyx Myrmidon for eight years — a reliable soldier with a gift for terrain navigation and a talent for keeping his unit fed in unfamiliar country. He went Oathless four years ago when he was given an order he refused. The order was to execute a group of twelve prisoners, including three who were clearly civilians, at a position where no officers were present to witness his compliance. He refused, let the prisoners walk, and walked away from the House at the same time. He has been operating in the border regions since, guiding people who cannot afford official transit across terrain he knows better than anyone still drawing Calyx pay.',
        N'Kristóf is the moral choice made at full cost — a man who did what he believed was right and has been paying the price every day since, without regret and without comfort.',
        N'No POV.',
        N'Eastern Calyx border regions; Oathless territory',
        181, 80, N'lean',
        N'dark brown', N'rough and grown out', N'medium',
        N'brown', N'medium', N'weathered',
        N'none',
        N'Alert and low; moves through terrain with the ease of someone who has been hunted before',
        N'No House insignia; rough practical traveling clothes in browns and greys; a knife worn at the small of his back',
        N'none',
        N'Guides groups across border terrain two to three times per month. Scouts Calyx patrol routes. Camps in positions he has mapped and prepared. Never stays in the same location two nights running.',
        N'He knows where the patrol gaps are in Calyx''s eastern border surveillance — not because he has been gathering intelligence deliberately, but because he has been surviving in those gaps for four years. He has considered whether he should share this knowledge with someone, and has not decided with whom sharing it would be right rather than merely useful.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Eastern Calyx border wilderness; Oathless territory',
        N'0', N'0',
        N'Lean Eastern European man in rough practical traveling clothes, dark brown hair grown out, weathered face, alert low movement, dense border forest background, medieval fantasy, gritty --ar 2:3',
        N'A 33-year-old former soldier in rough brown traveling clothes, dark hair grown out, weathered, moving alertly through a dense border forest',
        0, 0
    );
    PRINT 'Kristóf Nádasdy seeded.';
END
ELSE PRINT 'Kristóf Nádasdy already exists.';
GO

-- 21. Borbála Rákóczi
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Borbála Rákóczi')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Borbála Rákóczi', N'borbala-rakoczi', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Borbála Rákóczi', N'borbala-rakoczi', N'Borbála', N'Rákóczi', N'',
        N'human', N'human', N'female', N'she/her', 71, N'alive',
        N'Elder scholar; Calyx historical institute; last surviving witness to original Compact negotiations',
        N'Borbála Rákóczi was fourteen years old when her father, a Calyx diplomatic aide, brought her to the Compact negotiations as his scribe. She remembers every face and every argument, and she has total recall of a specific procedural dispute over which Houses were present for the final signing and which arrived after the fact but were recorded as original signatories. She has published three monographs on Compact history, none of which mention what she knows about that signing dispute because she has been waiting, for fifty-seven years, for the right moment — and because she is not sure the right moment exists.',
        N'Borbála is the keeper of a fact that delegitimizes one of the seven current Houses'' founding status. She has outlived everyone she might have told. The story is whether she dies with it.',
        N'No POV.',
        N'House Calyx; historical institute, capital',
        155, 51, N'lean',
        N'white', N'simply pinned', N'short',
        N'faded blue', N'pale', N'deeply lined',
        N'none',
        N'Very still; her attention is precise and total when engaged; otherwise appears to be somewhere else',
        N'Scholar''s plain dark dress; a writing case she has carried for forty years',
        N'none',
        N'Writes in the mornings when her hands permit. Receives junior scholars who come to ask her questions she mostly answers. Reads in the afternoons. Attends Compact ceremonies with an expression no one has been able to interpret.',
        N'She knows which of the seven Houses was not present for the final Compact signing — was still a day''s ride away — and was recorded as a signatory anyway, by an error or a deliberate falsification she cannot distinguish. She knows the House. She knows because she wrote the attendance record herself, as her father''s scribe, and the name was added in different ink after she had finished. She has been deciding whether to say anything for fifty-seven years and has concluded that she will not, because the consequences would be larger than she can manage and she is seventy-one years old. She has written it down in a sealed document addressed to no one.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Calyx capital historical institute',
        N'0', N'0',
        N'Very old Eastern European woman scholar, white simply pinned hair, pale deeply lined face, faded blue eyes, plain dark dress, writing case in hand, candlelit archive study, medieval fantasy --ar 2:3',
        N'A 71-year-old scholar with white hair and deeply lined pale face, very still, holding an old writing case in a candlelit medieval archive study',
        0, 0
    );
    PRINT 'Borbála Rákóczi seeded.';
END
ELSE PRINT 'Borbála Rákóczi already exists.';
GO

-- 22. Attila Bethlen
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Attila Bethlen')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Attila Bethlen', N'attila-bethlen', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Attila Bethlen', N'attila-bethlen', N'Attila', N'Bethlen', N'',
        N'human', N'human', N'male', N'he/him', 45, N'alive',
        N'Senior Calyx counter-intelligence officer; foreign agent identification and monitoring',
        N'Attila Bethlen has spent twenty years finding foreign agents inside Calyx''s institutional fabric. He has identified two who are currently active. He has not arrested them. He has been managing them — feeding each one selected information, monitoring the downstream effects, adjusting the feed — for fourteen months. He considers this the most sophisticated operation he has ever run. He is also aware that it is entirely unauthorized, that the agents'' handlers believe the operation is still producing genuine intelligence, and that if anything goes wrong he will face charges from three directions simultaneously.',
        N'Attila is the intelligence officer who has turned a security failure into a personal strategic instrument and must now decide when — and whether — to close it.',
        N'No POV.',
        N'House Calyx; capital intelligence division; classified operational range',
        178, 83, N'average',
        N'brown', N'neat', N'short',
        N'grey', N'medium', N'clear',
        N'none',
        N'Unremarkable by design; the deliberate averageness of a man who has learned that forgettable is a skill',
        N'Plain dress of a mid-level administrative official; nothing that marks him as intelligence',
        N'none',
        N'Reviews intelligence reports that cross his desk. Maintains contact with the two managed agents through cutouts. Writes his operational log in a cipher stored in a location separate from all official files.',
        N'He identified the two agents fourteen months ago and made the decision to manage rather than arrest in a single afternoon, alone, without consultation, and has been committed to that decision since. What he has not examined is whether the decision was operationally correct or whether it was the decision of a man who discovered he had power over something and could not bring himself to end it. He is beginning to suspect the latter. He is also beginning to suspect that one of the two agents has noticed something is wrong with the quality of the intelligence they have been receiving.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Calyx capital; classified operational range',
        N'0', N'0',
        N'Deliberately unremarkable middle-aged Eastern European man, neat brown hair, grey eyes, plain administrative dress, neutral expression, candlelit stone office, medieval fantasy --ar 2:3',
        N'A 45-year-old counter-intelligence officer with neat brown hair, deliberately unremarkable in plain administrative clothing, in a candlelit medieval stone office',
        0, 0
    );
    PRINT 'Attila Bethlen seeded.';
END
ELSE PRINT 'Attila Bethlen already exists.';
GO

-- 23. Dorottya Thököly
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Dorottya Thököly')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Dorottya Thököly', N'dorottya-thokoly', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Dorottya Thököly', N'dorottya-thokoly', N'Dorottya', N'Thököly', N'Mistress',
        N'human', N'human', N'female', N'she/her', 43, N'alive',
        N'Senior Transmutation practitioner; Ophiuchus-trained; assigned to Calyx military',
        N'Dorottya Thököly trained in Ophiuchus for six years before returning to Calyx service. She has a survival rate of approximately sixty percent on first infusion — three times the standard — and has been the subject of two formal Liturgy inquiries into her methodology, both of which she cooperated with fully and which found nothing irregular. She knows why her rate is higher and cannot explain it to anyone''s satisfaction, including her own. Something in her preparation protocol — possibly the timing of the Catalyst dosing, possibly the candidate selection criteria she uses that she has never fully articulated in writing — produces better outcomes. She does not know which element it is.',
        N'Dorottya represents the terrifying position of someone who saves more lives than anyone else and cannot reproduce her results reliably. Every infusion is a question she cannot answer.',
        N'No POV.',
        N'House Calyx; central infusion facility, capital district',
        168, 65, N'lean',
        N'dark auburn', N'severely pulled back', N'long',
        N'dark brown', N'warm olive', N'clear',
        N'none',
        N'Systematic and unhurried; moves through a preparation as if conducting a ceremony she has performed in a dream',
        N'Practitioner''s dark coat, Ophiuchus-style rather than Calyx standard; the dual certification seals at her breast',
        N'none',
        N'Candidate assessments in the mornings. Preparation in the afternoons on infusion days, which come twice monthly. Writes private notes on each case in a code she developed from Ophiuchus notation.',
        N'Her private notes contain a pattern she has been building for seven years — a correlation between a specific physical marker in candidates and survival outcomes. The marker is subtle enough that she cannot be certain it is real rather than confirmation bias. She has been watching for twelve more cases before she will consider publishing. She is afraid that if she publishes and is wrong, the false correlation will be used to exclude viable candidates. She is equally afraid that if she is right, the knowledge will be used in ways she cannot control.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Calyx capital infusion facility; occasional travel to Ophiuchus for consultation',
        N'0', N'0',
        N'Serious Eastern European woman practitioner, dark auburn severely pulled-back hair, dark brown eyes, lean precise posture, dark Ophiuchus-style coat with dual certification seals, stone infusion chamber, medieval fantasy --ar 2:3',
        N'A 43-year-old Transmutation practitioner with dark auburn hair severely pulled back, in a dark Ophiuchus-style coat, standing precisely in a stone infusion chamber',
        0, 0
    );
    PRINT 'Dorottya Thököly seeded.';
END
ELSE PRINT 'Dorottya Thököly already exists.';
GO

-- 24. Imre Wesselényi
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Imre Wesselényi')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Imre Wesselényi', N'imre-wesselenyi', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Imre Wesselényi', N'imre-wesselenyi', N'Imre', N'Wesselényi', N'',
        N'human', N'human', N'male', N'he/him', 59, N'alive',
        N'Senior Calyx military commander; Paladin; commands the western defensive line',
        N'Imre Wesselényi stands six feet eight inches in bare feet, with the proportions of a man who has been remade three times by Catalyst — his neck broader than most men''s shoulders, his hands capable of breaking things they were not designed to break. He has commanded the western defensive line for eleven years and holds it with a reputation for absolute tactical steadiness. He speaks rarely and in short sentences. He has also, in the last four years, developed a profound and private relationship with the Calyx ritual observances around Bheur — not the ceremonial version the House officially performs, but the older textual tradition, which he has been studying alone, in the earliest hours before his command staff wakes.',
        N'Imre is the most physically formidable officer in Calyx''s service and privately the most uncertain about what comes after. His faith is not comfort — it is a question he cannot stop asking in a body that no longer looks like it has questions.',
        N'No POV.',
        N'House Calyx; western defensive line command',
        205, 128, N'military-lean',
        N'black with grey', N'close-cropped', N'short',
        N'dark amber', N'deep brown', N'scarred',
        N'Evident enhancement — extreme height, dramatically altered proportions; broad neck and hands; amber-cast eyes; vascular prominence on forearms and throat',
        N'Carries himself as if constantly adjusting for the size of spaces that were not built for him',
        N'Western line command uniform, dark and plain; no decorative elements; the Paladin''s triple mark on both hands',
        N'Paladin — three infusions over thirty years; significant physical enhancement',
        N'Pre-dawn: reads the old Bheur texts alone. Dawn: command briefings. Daytime: defensive line inspection, tactical assessments. Late evening: writes in a personal journal that no one has seen.',
        N'He is afraid of what Bheur means for someone like him. The old texts describe the afterlife as shaped by who you were in life — the impression you left on the membrane between living and whatever comes next. He does not know what impression a body like his leaves. He worries, specifically, that the Transmutation has changed something essential, and that what waits for him is shaped by the thing he has become rather than the person he started as. He has never said this to anyone.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Western Calyx defensive line; capital for council',
        N'0', N'0',
        N'Enormous Eastern European Paladin commander, extremely tall with dramatically broad proportions, black-grey close-cropped hair, amber eyes, deep brown scarred skin, dark plain command uniform, stone fortress wall at dawn, medieval fantasy, imposing --ar 2:3',
        N'A 59-year-old Paladin commander of extreme height and broad proportions, black-grey hair, amber eyes, dark uniform, standing at a stone fortress wall at dawn',
        0, 0
    );
    PRINT 'Imre Wesselényi seeded.';
END
ELSE PRINT 'Imre Wesselényi already exists.';
GO

-- 25. Judit Perényi
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Judit Perényi')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Judit Perényi', N'judit-perenyi', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Judit Perényi', N'judit-perenyi', N'Judit', N'Perényi', N'',
        N'human', N'human', N'female', N'she/her', 31, N'alive',
        N'Senior Scrying operator; specialist in advanced-technology Sphere observation',
        N'Judit Perényi has been assigned to advanced-technology Spheres for six years — the Spheres where observation requires interpretation rather than simple cataloguing, because what is being made bears no resemblance to anything in the Cauld. She is one of four operators in Calyx authorized for that assignment. She has been spending her observation hours, for the past year and a half, watching what appears to be a conflict on a scale that dwarfs anything the Living War has produced — mass mechanized movement, coordinated destruction across distances that her Scrying lens can barely frame. She files her mandatory reports. The reports do not include her private conclusion: that what she is watching is a war that has already ended, preserved in a Sphere where time runs differently.',
        N'Judit has been watching history — possibly the worst event she can conceive of — through a window she cannot enter, and is beginning to wonder what it means for the Living War that something so much worse has already happened somewhere else.',
        N'No POV.',
        N'House Calyx; advanced Scrying installation, capital district',
        165, 59, N'lean',
        N'black', N'cropped short', N'short',
        N'dark brown', N'warm brown', N'clear',
        N'none',
        N'Abstracted in stillness; her attention when engaged is total; between observations she seems somewhere else',
        N'Senior operator''s grey uniform with the advanced classification badge; minimal personal effects',
        N'none',
        N'Long observation shifts, five hours minimum. Report writing. Consults with the other three senior operators on methodology questions. Spends personal time reviewing her private observation notes.',
        N'She has been watching what she believes is the aftermath of a war involving weapons she cannot name — the Spheres show devastated landscapes that do not match any natural event she has studied. She has concluded that something in another Sphere ended civilization-level conflict by consuming it. She does not know whether this should make her feel better or worse about the Living War. She is leaning toward worse.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Calyx capital advanced Scrying installation',
        N'0', N'0',
        N'Young Eastern European woman Scrying specialist, black cropped hair, dark brown eyes, grey senior operator''s uniform, abstracted focused expression, sophisticated Scrying lens apparatus in stone chamber, medieval fantasy --ar 2:3',
        N'A 31-year-old senior Scrying operator with black cropped hair and an abstracted expression, leaning toward a sophisticated lens apparatus in a medieval stone installation',
        0, 0
    );
    PRINT 'Judit Perényi seeded.';
END
ELSE PRINT 'Judit Perényi already exists.';
GO

-- 26. Miklós Andrássy
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Miklós Andrássy')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Miklós Andrássy', N'miklos-andrassy', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Miklós Andrássy', N'miklos-andrassy', N'Miklós', N'Andrássy', N'',
        N'human', N'human', N'male', N'he/him', 67, N'alive',
        N'Senior estate steward; Kossuth family primary estate; forty years of service',
        N'Miklós Andrássy has managed the Kossuth family''s primary estate for forty years, which means he has managed it through three successions and two sieges and a period in the middle of the Sinter Crisis that is not in any official record and which he refers to, when he refers to it at all, as the difficult winter. He has outlasted four estate lords, two of whom he personally assisted in decisions whose consequences are buried in the estate''s east pasture. He is old and careful and does not speak unless spoken to in council, where he is occasionally invited to speak.',
        N'Miklós is the institutional memory of an estate''s crimes — a man who has been too useful to remove and knows things that make him impossible to remove. His story is what happens when someone finally wants those things said aloud.',
        N'No POV.',
        N'House Calyx; Kossuth primary estate, eastern plains',
        172, 69, N'lean',
        N'white', N'neatly combed', N'short',
        N'pale blue', N'pale', N'deeply lined',
        N'none',
        N'Attentive and slightly bowed; moves through the estate as if cataloguing it; knows every room''s contents without looking',
        N'Steward''s practical dark livery, immaculate; the estate seal on a ring worn on the small finger',
        N'none',
        N'Morning estate inspection. Accounts review. Meets with the estate''s tenants on rotating schedules. Retires early. Does not sleep well.',
        N'During the difficult winter, certain things were buried in the east pasture that should not have been buried there — things the estate lord at the time had done and needed to not exist. Miklós arranged the burial. He knows where it is and what is there, and he has never told anyone, and the current Lord Vladimír Kossuth has never asked, which Miklós has sometimes thought about.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Kossuth primary estate, eastern Calyx plains',
        N'0', N'0',
        N'Very old Eastern European estate steward, white neatly combed hair, pale blue eyes, pale deeply lined face, immaculate dark livery, estate seal ring on small finger, stone estate interior, medieval fantasy --ar 2:3',
        N'A 67-year-old estate steward with white hair, pale lined face, and pale blue eyes, immaculate in dark livery, standing in a stone medieval estate hall',
        0, 0
    );
    PRINT 'Miklós Andrássy seeded.';
END
ELSE PRINT 'Miklós Andrássy already exists.';
GO

-- 27. Fanni Dessewffy
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Fanni Dessewffy')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Fanni Dessewffy', N'fanni-dessewffy', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Fanni Dessewffy', N'fanni-dessewffy', N'Fanni', N'Dessewffy', N'',
        N'human', N'human', N'female', N'she/her', 18, N'alive',
        N'Calyx Myrmidon; youngest conscript in her regiment; three months in service',
        N'Fanni Dessewffy was a farmer''s daughter who knew how to butcher animals before she arrived at camp, which gave her a practical edge in the first weeks of training that she did not know to attribute to that fact. She is good at the physical requirements of the work in a way that has drawn sergeant''s attention. She killed her first man in a skirmish on the second month, at a river crossing in bad light, with no particular drama. She did not expect to be as calm as she was. She has been thinking about this ever since.',
        N'Fanni is the new soldier who discovers she is good at something she has no framework for understanding. Her story is what she becomes before anyone helps her understand what is happening to her.',
        N'No POV.',
        N'House Calyx; eastern border regiment, new conscript cohort',
        164, 58, N'lean',
        N'light brown', N'practical braid', N'long',
        N'grey', N'fair', N'clear',
        N'none',
        N'Practical and direct; moves without ceremony; the ease of someone raised on physical work',
        N'Standard Myrmidon kit, fitted better than Tibor''s; her own knife in addition to the issue blade',
        N'none',
        N'Drills, sentry, camp work. Eats fully and well, which her sergeant notes approvingly. Spends evenings sharpening her blades and not talking.',
        N'She was calm when she killed. Not numb — calm. She has been thinking about whether that means something is wrong with her. The honest answer she keeps arriving at is that she does not know, and that the answer matters less than she thought it would. She has not written home. She is not sure what she would say.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Eastern Calyx border regiment',
        N'0', N'0',
        N'Young Eastern European girl soldier, light brown braided hair, grey eyes, fair complexion, standard military kit, sharpening a knife at a campfire, calm direct expression, medieval fantasy --ar 2:3',
        N'An 18-year-old female conscript with light brown braided hair and calm grey eyes, sharpening a knife at a medieval military campfire',
        0, 0
    );
    PRINT 'Fanni Dessewffy seeded.';
END
ELSE PRINT 'Fanni Dessewffy already exists.';
GO

-- 28. Jenő Batthyány
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Jenő Batthyány')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Jenő Batthyány', N'jeno-batthyany', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Jenő Batthyány', N'jeno-batthyany', N'Jenő', N'Batthyány', N'',
        N'human', N'human', N'male', N'he/him', 52, N'alive',
        N'Wine merchant; supplies noble districts across three Houses; unregistered intelligence source',
        N'Jenő Batthyány supplies wine to noble households in Calyx, Atrament, and nominally Fornax, which means he has a reason to be in every room where people with power drink. He is a good host, an easy conversationalist, and a patient listener. He does not formally work for any House''s intelligence service. He does not have to. He sells information the way he sells wine — to whoever wants it, at a price calibrated to what they can pay and what it is worth, with no commitment to exclusivity.',
        N'Jenő is the information broker who has maintained neutrality by being genuinely useful to everyone. His story is what happens when someone he has sold to uses the information in a way he did not anticipate.',
        N'No POV.',
        N'House Calyx; Atrament; Fornax noble districts',
        176, 91, N'stocky',
        N'grey-brown', N'well-groomed', N'short',
        N'brown', N'warm tan', N'clear',
        N'none',
        N'Warm and open by habit; uses physical ease as a tool; never forgets a name',
        N'Prosperous merchant''s good-quality practical clothing; always a flask of something notable on his person',
        N'none',
        N'Travel between estates and noble households on a circuit that takes three months to complete. Delivers wine. Attends dinners. Listens. Rides on.',
        N'He has been doing this for twenty years and has told himself it is merely commerce in a rare good — information — and that he bears no responsibility for what buyers do with what they buy. He heard something three months ago at an Atrament dinner that he has not yet sold because he does not know what to do with it. It concerns a plan that has not yet been executed. He is aware that not selling it is also a choice.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Three-House circuit; noble districts',
        N'0', N'0',
        N'Prosperous middle-aged Eastern European wine merchant, grey-brown well-groomed hair, warm tan complexion, quality practical clothing, easy warm posture, noble dining hall background, medieval fantasy --ar 2:3',
        N'A 52-year-old wine merchant with grey-brown hair, warm easy manner, quality clothing, carrying a wine flask at a medieval noble dinner',
        0, 0
    );
    PRINT 'Jenő Batthyány seeded.';
END
ELSE PRINT 'Jenő Batthyány already exists.';
GO

-- 29. Hona Károlyi
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Hona Károlyi')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Hona Károlyi', N'hona-karolyi', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Hona Károlyi', N'hona-karolyi', N'Hona', N'Károlyi', N'Mistress',
        N'human', N'human', N'female', N'she/her', 46, N'alive',
        N'Senior Liturgy archivist; assigned to Calyx district; twenty years in posting',
        N'Hona Károlyi was assigned to Calyx by the Liturgy at twenty-six and has not requested a transfer in twenty years, which the Liturgy''s central administration has noted without acting on it. She manages the Calyx district''s transit archives — records of every extraction from Sphere 31, every placement, every person. She is technically Liturgy but practically Calyx, in the way that a person becomes what they live among when they live among it long enough. She has been slowing certain transit orders — specifically orders for the extraction of persons in the youngest and oldest age ranges — through a combination of procedural querying and filing delays.',
        N'Hona is the institutional insider who has become, over twenty years, a very slow saboteur — and who does not fully acknowledge to herself that this is what she is doing.',
        N'No POV.',
        N'House Calyx; Liturgy district archive, eastern plains',
        162, 66, N'average',
        N'dark brown with grey', N'simply arranged', N'medium',
        N'brown', N'medium', N'lined',
        N'none',
        N'Steady and patient; the quality of attention of someone who reads documents for a living',
        N'Liturgy grey uniform, worn soft from years of use; Calyx-made practical shoes that are not regulation',
        N'none',
        N'Archive management. Transit order processing. Procedural querying on flagged orders — which she flags herself, using criteria she has refined over years. Writing to the central administration about process improvements that always require an extended review period.',
        N'She does not call what she does interference. She calls it due diligence. The distinction has become thinner over the years. She has slowed or effectively halted seventy-three transit orders in twenty years, and the persons those orders targeted have no idea she exists. She has never met any of them. She sometimes reviews their later placement files to see where they ended up, and considers the placements a kind of outcome data.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Calyx Liturgy district archive; rarely travels',
        N'0', N'0',
        N'Middle-aged Eastern European woman in worn Liturgy grey uniform, dark brown and grey simply arranged hair, steady patient expression, stone archive office full of transit records, medieval fantasy --ar 2:3',
        N'A 46-year-old Liturgy archivist in worn grey uniform with dark and grey hair, patient expression, surrounded by transit records in a medieval stone archive',
        0, 0
    );
    PRINT 'Hona Károlyi seeded.';
END
ELSE PRINT 'Hona Károlyi already exists.';
GO

-- 30. Péter Sigray
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Péter Sigray')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Péter Sigray', N'peter-sigray', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Péter Sigray', N'peter-sigray', N'Péter', N'Sigray', N'',
        N'human', N'human', N'male', N'he/him', 35, N'alive',
        N'Calyx military engineer; fortification design; currently assigned to western defensive review',
        N'Péter Sigray designed a modular defensive fortification system seven years ago that would allow faster repositioning of defensive lines as the front shifted — a problem that had cost Calyx three engagements in the preceding decade by making their static defenses predictable. His design was reviewed, praised in writing by two senior officers, and filed. It has not been implemented. The obstacle is Lord Béri Zay, who built the current fixed fortification network at considerable personal expense and political capital, and who is still alive and politically active. Péter understands this. He has been redesigning the same system for seven years.',
        N'Péter is the engineer who made a better thing and learned that better is not the deciding criterion. His story is what happens when the person blocking the better thing is finally gone — and whether he is still the right person to build it.',
        N'No POV.',
        N'House Calyx; western defensive review team, capital engineering division',
        178, 79, N'average',
        N'sandy brown', N'unkempt', N'short',
        N'blue', N'fair', N'weathered',
        N'none',
        N'Talks with his hands; thinks out loud; the energy of someone with a project he cannot put down',
        N'Engineer''s practical coat with the design division badge; always rolls of plans under one arm',
        N'none',
        N'Reviews western fortification surveys in the mornings. Drafts revised plans in the afternoons. Attends design review meetings where his proposals are received with polite interest and not adopted.',
        N'He has made peace with the delay — or something he identifies as peace. He has also spent the seven years improving the design in ways he could not have done if it had been built too early. He knows it is now substantially better than the original. He does not know if Lord Zay will die before Calyx needs it, and has begun to sketch contingency designs for scenarios where the fixed fortifications fail and something has to be built fast and under fire.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Calyx capital engineering division; western fortification survey areas',
        N'0', N'0',
        N'Energetic Eastern European male engineer, sandy brown unkempt hair, blue eyes, weathered face, practical coat with badge, architectural plans under his arm, stone planning room, medieval fantasy --ar 2:3',
        N'A 35-year-old military engineer with sandy brown hair and blue eyes, talking with his hands over architectural plans in a medieval stone planning room',
        0, 0
    );
    PRINT 'Péter Sigray seeded.';
END
ELSE PRINT 'Péter Sigray already exists.';
GO

-- 31. Orsolya Pálffy
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Orsolya Pálffy')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Orsolya Pálffy', N'orsolya-palffy', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Orsolya Pálffy', N'orsolya-palffy', N'Orsolya', N'Pálffy', N'Dame',
        N'human', N'human', N'female', N'she/her', 44, N'alive',
        N'Dame; commands eastern Calyx garrison; conducting unauthorized ceasefire negotiations',
        N'Orsolya Pálffy commands the eastern garrison, which sits on the most active stretch of the Fornax contact line and has been engaged in low-intensity fighting for eleven consecutive years without a single documented pause. She has served three tours here. She has also, for the past eight months, been meeting with her Fornax counterpart — a woman named Richter, whose first name she does not know — at a position between the lines, after dark, to negotiate a local and informal cessation of hostilities along a specific three-kilometer stretch. The arrangement has held for six months. No one on either side has filed a report.',
        N'Orsolya is doing something she was never given authority to do and which has, by any measure, worked. The story is what the Houses do when they find out.',
        N'No POV.',
        N'House Calyx; eastern garrison, Fornax contact line',
        171, 72, N'athletic',
        N'brown', N'cut short', N'short',
        N'brown', N'medium', N'scarred',
        N'Subtle height gain; increased density in posture and movement; vascular prominence at wrists',
        N'Deliberate and physically confident; the ease of someone who has been the most dangerous person in a room many times',
        N'Dame''s field uniform; the garrison command badge at her shoulder; always armed',
        N'Knight — first infusion at thirty-seven; moderate enhancement',
        N'Garrison command duties from dawn. Patrols the contact line in the afternoons. Twice monthly, after dark, walks to a specified point and meets a Fornax officer she considers a professional equal. Returns before dawn.',
        N'She does not know what will happen to her when the arrangement is discovered. She has considered this carefully. She believes the arrangement is saving lives at a rate that justifies the risk to her career and her freedom. She also knows that what she is doing fits the definition of unauthorized negotiation with an enemy force and that the consequences, if Calyx''s senior command decides to make an example, are not survivable. She has chosen not to stop.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Eastern Calyx garrison; Fornax contact line',
        N'0', N'0',
        N'Determined Eastern European woman knight commander, short brown hair, scarred face, dark field uniform with garrison badge, confident military posture, night-lit border contact line, medieval fantasy --ar 2:3',
        N'A 44-year-old woman knight commander with short brown hair and a scarred face, standing at a night-lit border contact line in medieval military uniform',
        0, 0
    );
    PRINT 'Orsolya Pálffy seeded.';
END
ELSE PRINT 'Orsolya Pálffy already exists.';
GO

-- 32. Rudolf Forgách
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Rudolf Forgách')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Rudolf Forgách', N'rudolf-forgach', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Rudolf Forgách', N'rudolf-forgach', N'Rudolf', N'Forgách', N'',
        N'human', N'human', N'male', N'he/him', 74, N'alive',
        N'Retired senior officer; military advisory role; institutional memory of the Calyx defensive network',
        N'Rudolf Forgách has been in every major Calyx campaign of the last fifty years, not because he is invulnerable but because he has been consistent: he does not take unnecessary risks, he does not try to win engagements he has already calculated as lost, and he has always known when to retreat before the retreat becomes a rout. He was kept in service past the standard age by three successive commands because of what he knows about the defensive network''s construction — where the weak points are, which fortifications were built to look like more than they are, which streams flood in which months and make which approaches impassable. He helped build it. He knows where it is hollow.',
        N'Rudolf is the kind of asset a House keeps alive past usefulness — except he is still useful, and the danger is that Fornax may understand this too.',
        N'No POV.',
        N'House Calyx; capital advisory staff; occasional field consultation',
        174, 68, N'lean',
        N'white', N'short', N'short',
        N'faded brown', N'pale', N'deeply lined',
        N'none',
        N'Very deliberate; the economy of movement of a man who has learned his body will not do everything he asks of it',
        N'Retired officer''s plain coat; no insignia by his own preference; good walking boots',
        N'none',
        N'Advisory meetings when called upon. Long walks through the capital to keep his legs working. Reviews maps alone in the evenings, noting things that have changed.',
        N'He has been keeping a mental map of every vulnerability in the Calyx defensive network for fifty years, updated with each change he has learned of. He has never written it down, which makes him the only complete copy. He has been approached once, subtly and indirectly, by someone he believed was a Fornax contact. He refused. He does not know if they will try again or whether they will decide to remove him instead.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Calyx capital; occasional field consultation across eastern region',
        N'0', N'0',
        N'Very old Eastern European retired officer, white short hair, faded brown eyes, pale deeply lined face, plain coat, deliberate economical movement, capital stone streets at dusk, medieval fantasy --ar 2:3',
        N'A 74-year-old retired military officer with white hair and a deeply lined pale face, walking deliberately through medieval stone streets at dusk',
        0, 0
    );
    PRINT 'Rudolf Forgách seeded.';
END
ELSE PRINT 'Rudolf Forgách already exists.';
GO

-- 33. Teréz Csáky
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Teréz Csáky')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Teréz Csáky', N'terez-csaky', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Teréz Csáky', N'terez-csaky', N'Teréz', N'Csáky', N'',
        N'human', N'human', N'female', N'she/her', 28, N'alive',
        N'Calyx healer; apprenticed to a senior practitioner; assigned to Scrying installation district',
        N'Teréz Csáky was trained as a healer and has been good at it for four years without anything unusual occurring, which is itself unusual given what she discovered about herself two years ago: she can feel the membrane. Not the Scrying installation''s mechanical interface — the membrane itself, through stone and distance, a pressure behind her sternum that changes when she is near an active site. She discovered this by accident, visiting a colleague at the installation. She has not reported it. She does not know who to report it to, or whether reporting it would result in study, service requirements, or something she has no framework for.',
        N'Teréz has an ability that the Liturgy would reclassify her entire life around. Her story is how long she can hold the secret, and what she does when she can''t.',
        N'No POV.',
        N'House Calyx; Scrying installation district, eastern region',
        165, 57, N'average',
        N'chestnut', N'simply pinned', N'medium',
        N'hazel', N'warm tan', N'clear',
        N'none',
        N'Attentive and careful with patients; near the installation sites she goes slightly still and inward',
        N'Healer''s grey apron over practical dress; a small personal herb kit at her belt',
        N'none',
        N'Healer''s rounds in the mornings. Patient consultations. Visits the installation district twice weekly on official errands. Stays longer than the errands require.',
        N'She has been testing the range of what she can feel. She can sense the primary installation from approximately four hundred meters. The secondary one is weaker — she has to be within a hundred meters. She has never attempted to do anything with the sensitivity beyond locate it. She is afraid of what happens if she tries to push into whatever she is feeling.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Eastern Calyx Scrying installation district; healer''s circuit',
        N'0', N'0',
        N'Young Eastern European woman healer, chestnut simply pinned hair, hazel eyes, warm tan complexion, grey apron over practical dress, standing near a Scrying installation with a slightly inward expression, medieval fantasy --ar 2:3',
        N'A 28-year-old healer with chestnut hair and hazel eyes, standing near a glowing Scrying installation in a medieval stone chamber, with a subtly inward expression',
        0, 0
    );
    PRINT 'Teréz Csáky seeded.';
END
ELSE PRINT 'Teréz Csáky already exists.';
GO

-- 34. Ambrus Zichy
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ambrus Zichy')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ambrus Zichy', N'ambrus-zichy', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Ambrus Zichy', N'ambrus-zichy', N'Ambrus', N'Zichy', N'',
        N'human', N'human', N'male', N'he/him', 38, N'alive',
        N'Oathless; Sphere 31 origin; former Calyx Myrmidon; border wilderness operator',
        N'Ambrus Zichy was taken from Sphere 31 at age eight, raised in a Calyx agricultural estate as a servant''s child, and entered Myrmidon service at sixteen when the estate''s lord offered him the option as an alternative to a labor reclassification. He served twelve years. He went Oathless at twenty-eight when he refused an order that he understood immediately and completely and which he has never repeated aloud to anyone since. He has been operating in the border wilderness for ten years, occasionally crossing paths with Kristóf Nádasdy''s refugee guide network without formal affiliation.',
        N'Ambrus is the Sphere 31 arrival who became the thing the Cauld made of him, and then refused it. He carries childhood memories from Sphere 31 that have degraded into images — a kitchen, a specific smell he cannot name, a sound of voices — and does not know what to do with them.',
        N'No POV.',
        N'Eastern Calyx border wilderness; Oathless territory; Sphere 31 origin',
        179, 78, N'lean',
        N'dark brown', N'cropped rough', N'short',
        N'dark brown', N'medium warm', N'weathered',
        N'none',
        N'Low and economical; the posture of someone who has spent years making as small a target as possible',
        N'Rough practical traveling clothes; no House marking; a worn pack with everything he needs to survive for a week',
        N'none',
        N'Moves camp every two to three nights. Hunts and forages. Occasionally assists border refugee groups without asking for payment. Sits at fires and sometimes goes somewhere else in his head that he does not try to name.',
        N'He has a fragment of a memory from Sphere 31 — a woman''s voice saying something in a language he no longer speaks, in a tone that means safety — and he does not know if it is his mother or someone else''s, and has concluded it does not matter because the person is thirty years and an impossible distance away. What he has not concluded is what he is for now, in the Cauld, as an Oathless man in his late thirties who is very good at surviving and not sure what surviving is in service of.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Eastern Calyx border wilderness; Sphere 31 origin',
        N'0', N'0',
        N'Lean Eastern European-featured man in rough practical traveling clothes, dark brown cropped hair, dark warm complexion, weathered face, low economical movement, dense border forest at dusk, medieval fantasy, gritty --ar 2:3',
        N'A 38-year-old Oathless man with dark brown hair and a weathered face, moving low and careful through a dense medieval border forest at dusk',
        0, 0
    );
    PRINT 'Ambrus Zichy seeded.';
END
ELSE PRINT 'Ambrus Zichy already exists.';
GO

-- 35. Nándor Révay
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Nándor Révay')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Nándor Révay', N'nandor-revay', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Nándor Révay', N'nandor-revay', N'Nándor', N'Révay', N'',
        N'human', N'human', N'male', N'he/him', 42, N'alive',
        N'Veteran Scrying operator; assigned to single Sphere for eleven years; withholds complete reports',
        N'Nándor Révay has been observing the same Sphere — a mountainous northern territory in Sphere 31 — for eleven years. His reports are complete according to the Liturgy''s definition: he files on schedule, includes all required categories, flags nothing as unusual. His reports are also incomplete in a specific way he has chosen: they do not include the people. He has been watching the same cluster of communities for eleven years. He knows which family expanded into a new house, which elder died in which winter, which child grew up to take over a forge. He has given them all names. None of this appears in any official document.',
        N'Nándor is the observer who has built an entire private world out of the people he watches. His story is what happens when the Liturgy decides to extract some of them.',
        N'No POV.',
        N'House Calyx; Scrying installation, northern observation post',
        175, 77, N'average',
        N'grey-brown', N'unkempt', N'medium',
        N'pale grey', N'fair', N'weathered',
        N'none',
        N'Looks through things rather than at them; the distance of someone who spends most of their seeing elsewhere',
        N'Operator''s grey tunic, worn and slightly unkempt; ink everywhere; a private journal he keeps in a cipher',
        N'none',
        N'Long observation shifts that exceed his official assignment. Files minimal compliant reports. Writes in the private journal. Eats irregularly. Sleeps when he cannot stay awake.',
        N'He knows the Liturgy has had this Sphere on a candidate list for extraction operations for three years. He has been filing reports that make the territory appear as unproductive as possible — sparse population, poor technology, no relevant agricultural techniques — to keep it off the extraction schedule. He cannot do this indefinitely. He has no plan for when it stops working.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Northern Calyx Scrying observation post',
        N'0', N'0',
        N'Gaunt middle-aged Eastern European man, grey-brown unkempt hair, pale grey eyes, weathered face, grey operator''s tunic, looking through a Scrying lens with distant expression, dark stone observation chamber, medieval fantasy --ar 2:3',
        N'A 42-year-old Scrying operator with grey-brown unkempt hair and distant pale grey eyes, peering through a Scrying lens in a dark medieval stone chamber',
        0, 0
    );
    PRINT 'Nándor Révay seeded.';
END
ELSE PRINT 'Nándor Révay already exists.';
GO

-- 36. Klára Berényi
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Klára Berényi')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Klára Berényi', N'klara-berenyi', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Klára Berényi', N'klara-berenyi', N'Klára', N'Berényi', N'',
        N'human', N'human', N'female', N'she/her', 33, N'alive',
        N'Calyx linguist; specializes in languages of the Fallen Houses; archival research',
        N'Klára Berényi is one of three people in the Cauld who can read Old Sinter fluently, which is considered an academic specialty with no practical application since Sinter collapsed forty-seven years ago. She has been studying its textual remains for eight years. Six months ago she found a document in Calyx''s sealed archive — filed under a miscellaneous category by someone who did not read it — that appears to be a Sinter text. It is written in a code she has begun to break. The coded text contains warnings, instructions, and a date. The date is after Sinter fell. Which means someone who could write Old Sinter in cipher survived the collapse, long enough to write this document.',
        N'Klára found a survivor''s message from a House that is supposed to be entirely gone. Everything the Cauld believes about Sinter''s fall may be wrong.',
        N'No POV.',
        N'House Calyx; capital archive, linguistic research division',
        167, 60, N'lean',
        N'dark brown', N'loose', N'medium',
        N'brown', N'warm brown', N'clear',
        N'none',
        N'The focused stillness of a codebreaker; not physically still but mentally absent from her surroundings',
        N'Scholar''s practical dress; always a cipher-working sheet folded in her pocket',
        N'none',
        N'Works in the archive. Reads Old Sinter texts. Works the cipher in her rooms after dark when no one can observe her progress.',
        N'She has broken enough of the cipher to know what it says. She has not broken the last section, which appears to be coordinates or a location. She is afraid of what the location might be — whether Sinter''s survivors went somewhere, built something, became something — and is simultaneously afraid of finishing the cipher and of stopping.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Calyx capital archive; linguistic research',
        N'0', N'0',
        N'Young Eastern European woman linguist, dark brown loose hair, warm brown complexion, focused expression, scholar''s practical dress, surrounded by Old Sinter documents and cipher sheets in a candlelit archive, medieval fantasy --ar 2:3',
        N'A 33-year-old female linguist with dark brown loose hair, intensely focused over Old Sinter documents and cipher sheets in a candlelit medieval archive',
        0, 0
    );
    PRINT 'Klára Berényi seeded.';
END
ELSE PRINT 'Klára Berényi already exists.';
GO

-- 37. Vilmos Ráday
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Vilmos Ráday')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Vilmos Ráday', N'vilmos-raday', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Vilmos Ráday', N'vilmos-raday', N'Vilmos', N'Ráday', N'',
        N'human', N'human', N'male', N'he/him', 54, N'alive',
        N'Head cook; has served the same Calyx regiment for thirty years; custodian of regimental morale',
        N'Vilmos Ráday has cooked for the Harmincas Regiment for thirty years and has survived every engagement the regiment has survived by staying behind the line and keeping the food going. He is not a fighter and has never pretended to be. He is, in the specific way of people who feed soldiers, one of the most important non-combatants in the regiment''s functioning. He knows things about the men and women he feeds — their habits, their fears, their loyalties, their breaking points — that no officer has access to because no officer eats with them the way he watches them eat.',
        N'Vilmos holds the regiment''s true morale record in his head, compiled from thirty years of observation. He is the character who knows the human truth that the official record cannot see.',
        N'No POV.',
        N'House Calyx; Harmincas Regiment cookhouse, eastern border',
        169, 88, N'stocky',
        N'salt-and-pepper', N'close-cropped', N'short',
        N'brown', N'dark warm', N'weathered',
        N'none',
        N'Efficient and calm; the unhurried steadiness of someone whose job always needs doing regardless of what else is happening',
        N'Cook''s heavy apron and practical clothes; carries a large worn knife he has had for twenty-five years',
        N'none',
        N'Pre-dawn through midday: cooking. Afternoons: supply inventory, fire maintenance, food preparation for the next day. Evenings: eating with whoever lingers, watching, listening.',
        N'He knows which three soldiers in the regiment are close to the breaking point — not desertion, they are not that kind, but the kind of break that happens in the field and gets people killed. He has been working around this knowledge for two months, feeding those three men larger portions and keeping them near the fire at night without making it obvious. He has not told their sergeant because the sergeant is one of the three.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Eastern Calyx border; Harmincas Regiment',
        N'0', N'0',
        N'Stocky middle-aged Eastern European man cook, salt-and-pepper close-cropped hair, dark warm weathered face, heavy apron, carrying an old large knife, active campfire cookhouse at dusk, medieval fantasy --ar 2:3',
        N'A 54-year-old regimental cook with salt-and-pepper hair and a dark weathered face, wearing a heavy apron and carrying an old large knife at a campfire cookhouse',
        0, 0
    );
    PRINT 'Vilmos Ráday seeded.';
END
ELSE PRINT 'Vilmos Ráday already exists.';
GO

-- 38. Marguerite Lacombe
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Marguerite Lacombe')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Marguerite Lacombe', N'marguerite-lacombe', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Marguerite Lacombe', N'marguerite-lacombe', N'Marguerite', N'Lacombe', N'',
        N'human', N'human', N'female', N'she/her', 37, N'alive',
        N'Atrament-origin deep-cover agent in Calyx; grain trade administrator; long-term intelligence asset',
        N'Marguerite Lacombe was planted in Calyx by Atrament''s intelligence apparatus twelve years ago, at twenty-five, with a cover identity as a grain trade administrator and instructions to build relationships in the eastern estate network. She has done this successfully. She has also, in the last four years, stopped believing that Atrament''s interests and the interests of the people she works among are compatible, and has been feeding her handlers information that is technically accurate and strategically useless. She has not formally defected because she does not know how, and because the life she has here is the life she has.',
        N'Marguerite is the spy who has genuinely become the person she was pretending to be — and cannot get out of either identity without destroying one of them.',
        N'No POV.',
        N'House Calyx; eastern grain trade administration; Atrament origin',
        166, 61, N'average',
        N'light brown', N'neatly arranged', N'medium',
        N'grey', N'fair', N'clear',
        N'none',
        N'Socially comfortable and fluent in Calyx norms; occasionally a slight miscalibration she has learned to pre-empt',
        N'Eastern Calyx estate administrator''s practical dress; nothing that marks her as anything other than what she claims',
        N'none',
        N'Estate trade administration work, which is genuine and competent. Monthly contact with her Atrament handler, through a channel she has been using for twelve years and has increasingly used to pass minimally useful information. Evenings with colleagues she has known for a decade.',
        N'She has friends here. She has a life here. The woman she was at twenty-five is not who she is now, and the woman she is now does not want to be recalled to Atrament, does not want to be exposed in Calyx, and cannot see a path through either outcome. She has been considering, for the past year, whether she could simply stop being an agent by doing nothing — filing nothing, reporting nothing, going silent — and whether Atrament would eventually conclude she had been turned or had died.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Eastern Calyx estate district; Atrament origin',
        N'0', N'0',
        N'Young French-origin woman administrator in practical Eastern European dress, light brown neatly arranged hair, grey eyes, fair complexion, socially comfortable posture, eastern estate office, medieval fantasy --ar 2:3',
        N'A 37-year-old French-origin woman posing as a Calyx administrator, light brown hair, grey eyes, in practical eastern estate dress, looking comfortable in a medieval stone office',
        0, 0
    );
    PRINT 'Marguerite Lacombe seeded.';
END
ELSE PRINT 'Marguerite Lacombe already exists.';
GO

-- 39. Pál Festetics
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Pál Festetics')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Pál Festetics', N'pal-festetics', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Pál Festetics', N'pal-festetics', N'Pál', N'Festetics', N'Lord',
        N'human', N'human', N'male', N'he/him', 65, N'alive',
        N'Lord; head of the Calyx Grain Council; controls agricultural allocation and export agreements',
        N'Pál Festetics has led the Calyx Grain Council for fourteen years and understands grain as a political instrument with the same sophistication that Veronika Batthyány understands diplomatic language. He has been deliberately slowing grain shipments to two of Calyx''s current allies — not enough to constitute a breach of agreement, always within the tolerance margin — while privately negotiating improved terms for the next contract cycle. He has done this without consulting Lord Vladimír Kossuth or the diplomatic corps. He considers it a commercial matter.',
        N'Pál is the man who treats political alliance as a business negotiation. His story is what happens when his optimization creates a gap at a moment when the gap matters militarily.',
        N'No POV.',
        N'House Calyx; Grain Council, capital; eastern agricultural estates',
        178, 92, N'stocky',
        N'white', N'neatly combed', N'short',
        N'pale blue', N'pale', N'florid',
        N'none',
        N'Confident and expansive; the authority of a man accustomed to being the most important person in a room',
        N'Wealthy lord''s formal dress in Calyx colors; a grain council seal ring on his right hand',
        N'none',
        N'Council meetings. Private negotiations with trade representatives. Reviews grain shipment schedules with an attention that his staff finds unusual for a lord of his station.',
        N'He has told himself this is commercial optimization. He is beginning to suspect it is something closer to the kind of leverage a man takes when he is not sure the House''s senior political structure values him correctly. He has been passing the shipment delays off as logistics problems. He has become very skilled at inventing logistics problems.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Calyx capital; eastern agricultural estate circuit',
        N'0', N'0',
        N'Older Eastern European lord, white neatly combed hair, pale florid face, confident expansive posture, wealthy formal dress in dark Calyx colors, council chamber, medieval fantasy --ar 2:3',
        N'A 65-year-old lord with white hair and a florid pale face, confident in wealthy formal Calyx dress, at a medieval stone council table',
        0, 0
    );
    PRINT 'Pál Festetics seeded.';
END
ELSE PRINT 'Pál Festetics already exists.';
GO

-- 40. Rózsa Hunyadi
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Rózsa Hunyadi')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Rózsa Hunyadi', N'rozsa-hunyadi', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Rózsa Hunyadi', N'rozsa-hunyadi', N'Rózsa', N'Hunyadi', N'Dame',
        N'human', N'human', N'female', N'she/her', 41, N'alive',
        N'Dame; three tours on the Fornax border; military liaison to peace negotiation sessions',
        N'Rózsa Hunyadi lost her right hand to a Fornax blade on her second tour at thirty-one. The surgeons could not save it. A Scrying-derived design for a mechanical prosthetic had been sitting in the design corps files for six years — it was finally approved for fabrication on her behalf, becoming the first military prosthetic issued in Calyx. The hand works better than expected. She uses it to sign her name at peace negotiation sessions that she privately considers ceremonial theater rather than genuine diplomacy, which has not stopped her from preparing for each session with scrupulous seriousness.',
        N'Rózsa is the soldier who has been through enough to believe peace is worth taking seriously, at the same time she cannot make herself believe the negotiations she attends will produce it.',
        N'No POV.',
        N'House Calyx; peace negotiation circuit; western border',
        168, 69, N'athletic',
        N'dark brown', N'pulled back', N'long',
        N'brown', N'warm tan', N'scarred',
        N'Subtle height gain; increased density; right hand is mechanical prosthetic from Scrying-derived design',
        N'Moves with the precision of someone who has relearned a body; the prosthetic hand carries itself with particular care',
        N'Dame''s formal dress for negotiation sessions; field uniform otherwise; the mechanical hand visible and ungloved',
        N'Knight — first infusion at thirty-four; moderate enhancement; right hand prosthetic',
        N'Preparation for negotiation sessions when assigned. Reviews Fornax tactical reports in parallel — she is a liaison, not a negotiator, and keeps her own assessment of what is actually happening at the border.',
        N'She believes the current negotiation cycle will fail because the Fornax representative does not have authority to offer what Calyx''s minimum requirement is, and the Calyx representative does not understand this because he has never been on the border. She has written this assessment twice. It has not been forwarded past the session secretary. She has considered going directly to Lord Vladimír Kossuth and has not done it yet because she is not sure it would change anything.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Calyx peace negotiation circuit; western border',
        N'0', N'0',
        N'Determined Eastern European woman knight, dark brown pulled-back hair, scarred warm tan face, mechanical right hand prosthetic, Dame''s formal dress, negotiation chamber, medieval fantasy --ar 2:3',
        N'A 41-year-old woman knight with dark brown hair, a scarred face, and a visible mechanical right hand prosthetic, in formal dress at a medieval stone negotiation table',
        0, 0
    );
    PRINT 'Rózsa Hunyadi seeded.';
END
ELSE PRINT 'Rózsa Hunyadi already exists.';
GO

-- 41. Elek Draskovich
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Elek Draskovich')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Elek Draskovich', N'elek-draskovich', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Elek Draskovich', N'elek-draskovich', N'Elek', N'Draskovich', N'',
        N'human', N'human', N'male', N'he/him', 20, N'alive',
        N'Calyx Myrmidon; sole survivor of his original unit; reassigned to second regiment',
        N'Elek Draskovich''s original unit — forty-one Myrmidons, one sergeant — was destroyed in a river ambush on his first campaign. He was at the rear of the column getting water from a side stream when the engagement began. By the time he reached the column''s position, it was over. He was found by a scouting party three days later, having survived alone in hostile terrain. The official record notes him as a survivor of an engagement. It does not specify that he did not fight in it. His new regiment does not know this, and he has not told them, and he is acutely aware every day that they are treating him as a combat veteran.',
        N'Elek is living inside a misunderstanding about his courage that he did not create and cannot safely correct. His story is whether the man the regiment believes he is becomes the man he actually is.',
        N'No POV.',
        N'House Calyx; second regiment assignment, eastern border',
        173, 69, N'lean',
        N'dark brown', N'short', N'short',
        N'brown', N'medium warm', N'clear',
        N'none',
        N'Quietly attentive; carries himself with the slight excess of care of someone monitoring how they appear',
        N'Standard Myrmidon kit, well-maintained; his unit''s badge worn correctly; nothing that calls attention',
        N'none',
        N'Drills. Sentry. Tries to observe what genuine combat veterans do and reproduce it. Eats little. Talks less.',
        N'He did not run. He was not there. The distinction matters to him enormously and he cannot make it matter to anyone else without explaining the full sequence of events, which would make him look like a man who was getting water while his unit died. He does not know if that is cowardice. He has spent four months trying to decide and has gotten nowhere.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Eastern Calyx border; second regiment',
        N'0', N'0',
        N'Young Eastern European soldier, dark brown short hair, brown eyes, medium warm complexion, standard military kit, quietly watchful expression, military camp, medieval fantasy --ar 2:3',
        N'A 20-year-old soldier with dark brown hair and quiet watchful eyes, standing carefully in a medieval military camp with standard kit',
        0, 0
    );
    PRINT 'Elek Draskovich seeded.';
END
ELSE PRINT 'Elek Draskovich already exists.';
GO

-- 42. Magdolna Kanizsai
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Magdolna Kanizsai')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Magdolna Kanizsai', N'magdolna-kanizsai', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Magdolna Kanizsai', N'magdolna-kanizsai', N'Magdolna', N'Kanizsai', N'Mistress',
        N'human', N'human', N'female', N'she/her', 48, N'alive',
        N'Transmutation practitioner; survived failed infusion without enhancement; continues to practice',
        N'Magdolna Kanizsai underwent her own infusion at thirty-one at her own request. She survived it. The Catalyst did not enhance her — she received none of the physical changes that mark a Knight. The practitioners who reviewed her case found nothing wrong with the preparation. She was documented as a statistical outlier and returned to active practice. She has been practicing for seventeen years since, with a standard survival rate for a competent practitioner, and is considered one of the best preparation specialists in Calyx''s western province — meaning she is skilled at doing everything that surrounds the infusion, even though she cannot demonstrate the outcome on her own body.',
        N'Magdolna is the practitioner who tried and received nothing, and has been serving others'' transformations for seventeen years since. The story is what that cost has made of her.',
        N'No POV.',
        N'House Calyx; western province infusion facility',
        163, 67, N'average',
        N'dark grey', N'pulled back', N'medium',
        N'dark brown', N'warm olive', N'lined',
        N'none',
        N'Methodical and unhurried; a warmth toward candidates that is not false but that she maintains carefully',
        N'Practitioner''s dark coat, western style; the licensed infusion seal; no Knight badge',
        N'none',
        N'Prepares candidates with more time per session than is standard. Reviews each case file thoroughly. Does not tell candidates her own history unless they ask, which they rarely do.',
        N'She is not bitter about her own failed enhancement. She examined that possibility carefully and concluded that she is not, which she trusts because she has examined it more than once. What she has not fully examined is what she would have done with the Knight''s rank if it had come — whether she would have fought, or stayed in practice, or something else. She sometimes thinks the absence clarified who she actually was.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Western Calyx province infusion facility',
        N'0', N'0',
        N'Middle-aged Eastern European woman practitioner, dark grey pulled-back hair, warm olive complexion, lined face, dark practitioner''s coat with certification seal but no knight badge, stone infusion chamber, medieval fantasy --ar 2:3',
        N'A 48-year-old Transmutation practitioner with dark grey pulled-back hair and a warm lined face, in a dark coat in a stone infusion chamber, with a quietly steady expression',
        0, 0
    );
    PRINT 'Magdolna Kanizsai seeded.';
END
ELSE PRINT 'Magdolna Kanizsai already exists.';
GO

-- 43. Benedek Forgách
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Benedek Forgách')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Benedek Forgách', N'benedek-forgach', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Benedek Forgách', N'benedek-forgach', N'Benedek', N'Forgách', N'',
        N'human', N'human', N'male', N'he/him', 56, N'alive',
        N'Senior Calyx border commander; accepts a cut from a logistics fraud operation',
        N'Benedek Forgách commands a border regiment with genuine competence and a reputation for keeping his soldiers equipped above minimum standard. He has been receiving duplicate supply deliveries on two line items for two years, delivered to a third location he has listed as a forward depot, then quietly liquidated by a quartermaster sergeant he has a cordial relationship with. The surplus is split. He tells himself this is a rainy-day reserve — that the system is too brittle, that he is building resilience, that no one has been shorted because the original supply quantities were padded. Some of this is true.',
        N'Benedek is the officer who has crossed a line so gradually that he cannot identify the exact moment of crossing and now relies on that ambiguity for his sense of himself.',
        N'No POV.',
        N'House Calyx; border regiment command, eastern sector',
        181, 87, N'stocky',
        N'salt-and-pepper', N'neat', N'short',
        N'grey-green', N'medium', N'weathered',
        N'none',
        N'Solid and authoritative; the ease of a man who has been in command long enough to stop performing command',
        N'Border commander''s field uniform; well-maintained; a personal knife he has carried since his first campaign',
        N'none',
        N'Command duties. Supply reviews — with extra attention to the duplicate order lines. Walks the regiment perimeter in the evening. Does not think about the arrangement when he can help it.',
        N'The arrangement is not survivable if discovered. He knows this. He also knows that the quartermaster sergeant knows enough to end him, and that the relationship is now one of mutual dependency rather than superior and subordinate. He has been trying to remember when that happened and cannot.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Eastern Calyx border regiment',
        N'0', N'0',
        N'Solid middle-aged Eastern European border commander, salt-and-pepper neat hair, grey-green eyes, weathered face, well-maintained field uniform, evening perimeter walk, medieval fantasy --ar 2:3',
        N'A 56-year-old border regiment commander with salt-and-pepper hair and grey-green eyes, solid and weathered, walking a medieval military perimeter at evening',
        0, 0
    );
    PRINT 'Benedek Forgách seeded.';
END
ELSE PRINT 'Benedek Forgách already exists.';
GO

-- 44. Vilhelmina Sigray
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Vilhelmina Sigray')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Vilhelmina Sigray', N'vilhelmina-sigray', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Vilhelmina Sigray', N'vilhelmina-sigray', N'Vilhelmina', N'Sigray', N'Mistress',
        N'human', N'human', N'female', N'she/her', 52, N'alive',
        N'Liturgy transit commander; oversees Sphere 31 extraction operations for Calyx district',
        N'Vilhelmina Sigray commands the transit operations that move persons from Sphere 31 into Calyx service. She has managed three extraction cohorts in the last four years, totaling several hundred persons. She applies for reassignment every year. It is denied every year, with notes that call her work excellent and essential. She is deeply uncomfortable with what she does — has been since the first cohort, when she learned what the transit looks like from the receiving end rather than the command side. She has not let her discomfort affect the efficiency of operations, because she is a careful person and she knows that efficiency from her side is better than the alternative.',
        N'Vilhelmina is the institutional participant who cannot make herself stop participating, and who has decided that doing it carefully is morally meaningful even though she suspects it is not.',
        N'No POV.',
        N'House Calyx; Liturgy transit command station, Calyx district',
        166, 70, N'average',
        N'silver-blonde', N'formally arranged', N'medium',
        N'blue', N'fair', N'clear',
        N'none',
        N'Formally composed; the controlled affect of someone who has separated professional from personal for professional reasons',
        N'Liturgy transit commander''s uniform, formal and exact; the commander''s transit seal on a chain',
        N'none',
        N'Operational planning for extraction cohorts. Arrival processing oversight. Files reassignment requests that go nowhere. Drinks in the evenings, not heavily, but consistently.',
        N'She has concluded that the system is wrong but that her removal from it would make no difference to the system and only to her conscience. She is also aware this is the argument everyone makes who stays in a wrong thing. She does not know what else to do with this awareness.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Calyx Liturgy transit command; extraction circuit',
        N'0', N'0',
        N'Formal middle-aged woman Liturgy commander, silver-blonde formally arranged hair, blue eyes, fair complexion, precise Liturgy transit uniform, stone command station, medieval fantasy, controlled expression --ar 2:3',
        N'A 52-year-old Liturgy transit commander with silver-blonde hair and blue eyes, formally composed in exact uniform, in a medieval stone command station',
        0, 0
    );
    PRINT 'Vilhelmina Sigray seeded.';
END
ELSE PRINT 'Vilhelmina Sigray already exists.';
GO

-- 45. Gertrúd Batthyány
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Gertrúd Batthyány')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Gertrúd Batthyány', N'gertrud-batthyany', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Gertrúd Batthyány', N'gertrud-batthyany', N'Gertrúd', N'Batthyány', N'',
        N'human', N'human', N'female', N'she/her', 61, N'alive',
        N'Estate accounts administrator; manages ledgers for three noble families simultaneously',
        N'Gertrúd Batthyány has been managing accounts for noble families for thirty years and is trusted by all three of her current clients precisely because she is known to keep their affairs private. This has created a situation she has never discussed with anyone: two of her three clients are officially at war with each other — a border dispute that has produced two formal engagements and one death — but are simultaneously conducting grain trade through intermediaries whose records Gertrúd manages. The arrangement is profitable for both families and has been ongoing for six years. She does the accounting for both sides of the trade and both sides of the war debt.',
        N'Gertrúd is the administrator who has made herself necessary to an arrangement so irregular that any of its participants would destroy her to keep it quiet — and who has concluded that knowing this is itself a form of security.',
        N'No POV.',
        N'House Calyx; noble estate accounting circuit, eastern region',
        159, 68, N'stocky',
        N'white', N'severely pinned', N'medium',
        N'brown', N'pale', N'deeply lined',
        N'none',
        N'Precise and contained; the affect of someone who has learned that knowing things is safer than saying them',
        N'Administrator''s practical dark dress; a heavy accounts satchel she never sets down in public',
        N'none',
        N'Moves between estates on a regular circuit. Reviews and reconciles accounts. Files separately for each client with strict confidentiality. Writes nothing she would not want read.',
        N'She has realized that her knowledge of the back-channel trade makes her simultaneously protected and at risk. The families need her because she is the only person who knows both sides of the arrangement and has kept it running cleanly. But if either family decided the arrangement needed to end and all evidence destroyed, her utility would end with it. She has been considering what form of insurance she might construct, and has not yet settled on one.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Eastern Calyx noble estate circuit',
        N'0', N'0',
        N'Older Eastern European woman estate accountant, white severely pinned hair, pale deeply lined face, precise contained expression, dark practical dress, heavy accounts satchel, stone estate accounting room, medieval fantasy --ar 2:3',
        N'A 61-year-old estate accountant with white pinned hair and a pale lined face, precise and contained, holding a heavy accounts satchel in a medieval stone estate office',
        0, 0
    );
    PRINT 'Gertrúd Batthyány seeded.';
END
ELSE PRINT 'Gertrúd Batthyány already exists.';
GO

-- 46. Jancsi Horvath
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Jancsi Horvath')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Jancsi Horvath', N'jancsi-horvath', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Jancsi Horvath', N'jancsi-horvath', N'Jancsi', N'Horvath', N'',
        N'human', N'human', N'male', N'he/him', 55, N'alive',
        N'Senior farmer; eastern Calyx estate landholder; Sphere 31 origin; thirty-three years in the Cauld',
        N'Jancsi Horvath was taken from Sphere 31 at twenty-two. The language of his placement region was close enough to Hungarian that he could function within a season. He spent his first decade in agricultural labor on an estate whose lord was not cruel and whose overseer was. He saved enough in his second decade to purchase a small plot. In his third decade he has land, two grown children born in the Cauld, and a reputation as one of the most reliable dryland farmers in the eastern province. He prays to Bheur at the Compact ceremonies with the same gestures as everyone around him. He does not know exactly what the words mean and has never asked.',
        N'Jancsi is what the Cauld does to a person who decides to survive and then to live. He is what successful integration costs, and what it makes, and what it cannot give back.',
        N'No POV.',
        N'House Calyx; eastern estate province; Sphere 31 origin',
        174, 82, N'stocky',
        N'dark grey', N'short and practical', N'short',
        N'dark brown', N'medium warm', N'weathered',
        N'none',
        N'Grounded and unhurried; the posture of a man comfortable in his body and his land',
        N'Farmer''s practical clothing in Calyx regional style; work-worn and maintained; no insignia or affiliation marks',
        N'none',
        N'Works his land from before dawn. Manages two hired hands he treats as subordinates and not as servants. Attends the estate market on the tenth of each month. Eats with his children when they visit.',
        N'He has one clear memory of Sphere 31 — the smell of a specific fuel burning, which he cannot name in the Cauld''s language, and which he smelled once in the twenty years he has been on his own land, rising from a Scrying installation''s machinery. He stood very still. He has never gone back to that installation. He does not know what he would do if the smell meant what he thinks it might mean.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Eastern Calyx estate province; Sphere 31 origin',
        N'0', N'0',
        N'Older Eastern European farmer, dark grey short hair, dark brown eyes, medium warm complexion, weathered face, practical farm clothing, standing on his land at sunrise, medieval fantasy, grounded --ar 2:3',
        N'A 55-year-old farmer with dark grey hair and a weathered warm face, standing solidly on his cultivated land at sunrise in medieval Eastern European style',
        0, 0
    );
    PRINT 'Jancsi Horvath seeded.';
END
ELSE PRINT 'Jancsi Horvath already exists.';
GO

-- 47. Ágnes Prónay
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ágnes Prónay')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ágnes Prónay', N'agnes-pronay', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Ágnes Prónay', N'agnes-pronay', N'Ágnes', N'Prónay', N'',
        N'human', N'human', N'female', N'she/her', 24, N'alive',
        N'Junior Scrying operator; assigned to a Sphere declared uninhabited; concealing active observation',
        N'Ágnes Prónay has been a Scrying operator for eighteen months, assigned to a Sphere the Liturgy''s catalog lists as uninhabited — no permanent settlements, no technology of interest, survey complete. She was assigned for routine monitoring of atmospheric and terrain changes. In her fourth week, she found people. Not many — a small community in a valley that the initial survey had missed because the survey window was during the community''s winter dispersal. She has been watching them for four months. She has not filed a report that includes them.',
        N'Ágnes has found people in a Sphere the system has already decided does not have people. Her decision not to report is protecting them — or delaying the inevitable — and she does not know which.',
        N'No POV.',
        N'House Calyx; Scrying installation, routine monitoring post',
        161, 54, N'lean',
        N'blonde', N'braided', N'long',
        N'blue', N'fair', N'clear',
        N'none',
        N'Young and earnest; has not yet learned to perform composure; her attention is visibly total',
        N'Standard junior operator''s grey tunic; her personal notes tucked into the tunic pocket',
        N'none',
        N'Observation shifts. Files terrain and atmospheric reports that are accurate and exclude the community. Spends personal hours trying to understand the community''s language from lip movements and written materials she can observe through the lens.',
        N'She has been watching long enough to understand that the community is small — perhaps forty people — and that they have no idea they are being observed. She has also learned that one Liturgy extraction operation targets Spheres of this population density. Her report schedule has two months before a review that will require her to confirm her Sphere''s uninhabited classification. She does not know what she is going to do.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'House Calyx; routine Scrying monitoring post',
        N'0', N'0',
        N'Young Eastern European woman operator, blonde braided hair, blue eyes, earnest expression, junior grey operator''s tunic, leaning intently over a Scrying lens, stone chamber, medieval fantasy --ar 2:3',
        N'A 24-year-old junior Scrying operator with blonde braided hair and earnest blue eyes, leaning intently over an observation lens in a medieval stone chamber',
        0, 0
    );
    PRINT 'Ágnes Prónay seeded.';
END
ELSE PRINT 'Ágnes Prónay already exists.';
GO

-- 48. Mihály Thuróczy
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Mihály Thuróczy')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Mihály Thuróczy', N'mihaly-thuroczy', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Mihály Thuróczy', N'mihaly-thuroczy', N'Mihály', N'Thuróczy', N'',
        N'human', N'human', N'male', N'he/him', 49, N'alive',
        N'Oathless; former Calyx military intelligence officer; burned civilian suspect lists',
        N'Mihály Thuróczy was a senior military intelligence officer for twenty years. He was respected and effective. At forty-six he was assigned to compile and maintain lists of Calyx civilians suspected of Atrament sympathy — not for active prosecution, he was told, but for contingency planning. He compiled the lists for seven months. He then burned them, along with his copies of the methodology and source files, and walked out of the intelligence division''s building on a morning in spring and did not return. He has been Oathless for three years.',
        N'Mihály is the intelligence professional who found his limit. He knows what is on the replacement list for those burned files — he knows what kind of person the division will assign to rebuild the work — and he is trying to decide whether going Oathless was enough or whether he should do something more.',
        N'No POV.',
        N'House Calyx border region; Oathless territory',
        176, 79, N'average',
        N'grey', N'close-cropped', N'short',
        N'pale grey', N'medium', N'lined',
        N'none',
        N'Analytical and quiet; the watchfulness of someone who has been trained to observe and has not stopped',
        N'Plain practical clothing; nothing military; nothing that connects him to intelligence service',
        N'none',
        N'Moves between small communities in the border region, offering practical skills — literacy, document drafting, accounting — in exchange for food and shelter. Monitors Calyx intelligence activity from a distance. Considers his options.',
        N'He knows the division replaced him within two months and is rebuilding the lists. He also knows that the person who assigned him the work is still in a senior position and knows he burned the files. He has been waiting three years to see if Calyx would pursue him aggressively or let him go. The pursuit has been quiet. He is not sure if that means they have given up or that they are waiting for him to contact someone they want to identify.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Eastern Calyx border communities; Oathless territory',
        N'0', N'0',
        N'Analytical middle-aged Eastern European man, grey close-cropped hair, pale grey eyes, lined face, plain practical clothing, quiet watchful expression, small border community, medieval fantasy --ar 2:3',
        N'A 49-year-old former intelligence officer with grey close-cropped hair and quiet watchful pale grey eyes, in plain practical clothes in a small medieval border community',
        0, 0
    );
    PRINT 'Mihály Thuróczy seeded.';
END
ELSE PRINT 'Mihály Thuróczy already exists.';
GO

-- 49. Antónia Majláth
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Antónia Majláth')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Antónia Majláth', N'antonia-majlath', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Antónia Majláth', N'antonia-majlath', N'Antónia', N'Majláth', N'Lady',
        N'human', N'human', N'female', N'she/her', 63, N'alive',
        N'Lady of the eastern estates; survived four political purges; accumulating favors over forty years',
        N'Antónia Majláth has survived four political purges by appearing harmless and actually being harmless, which is genuinely how she has survived — she did nothing in any of the four periods that could be prosecuted, not because she was calculating but because she simply continued doing what she had always done: managing her estates, attending the ceremonies, writing polite letters, and helping people with small things they needed. In forty years of this, she has done a great many small things for a great many people, and has never asked for anything in return. She is now sixty-three and has no idea what she has accumulated.',
        N'Antónia is the political actor who has no political ambition, which has made her the most dangerous kind of political actor. The story is who realizes this first and what they want from it.',
        N'No POV.',
        N'House Calyx; eastern estates; capital for ceremonies',
        165, 71, N'average',
        N'white', N'simply arranged', N'medium',
        N'faded blue', N'pale', N'gently lined',
        N'none',
        N'Warmly present and unhurried; the ease of someone who genuinely does not feel threatened',
        N'Lady''s practical estate dress, well-made but not ostentatious; the Majláth family sigil ring',
        N'none',
        N'Estate management. Correspondence. Visitors — she has many, because she is known as someone who helps. Ceremonies, which she attends faithfully. Dinner with her household in the evenings.',
        N'She does not know she has accumulated anything that could be called power. She genuinely believes she has been spending the last forty years being a decent person and managing her land. She is not wrong. She also has, without ever intending it, done favors for people who now occupy positions that would not be accessible to anyone else. She has never thought to call on this. Someone is about to suggest to her that she should.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Eastern Calyx estates; capital for ceremonies',
        N'0', N'0',
        N'Older Eastern European noblewoman, white simply arranged hair, faded blue eyes, pale gently lined face, warmly present expression, practical well-made estate dress, eastern estate hall, medieval fantasy --ar 2:3',
        N'A 63-year-old noblewoman with white hair and faded blue eyes, warmly present expression, in practical well-made estate dress in a medieval eastern estate hall',
        0, 0
    );
    PRINT 'Antónia Majláth seeded.';
END
ELSE PRINT 'Antónia Majláth already exists.';
GO

-- 50. Kornél Nádasdy
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Kornél Nádasdy')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Kornél Nádasdy', N'kornel-nadasdy', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Kornél Nádasdy', N'kornel-nadasdy', N'Kornél', N'Nádasdy', N'',
        N'human', N'human', N'male', N'he/him', 44, N'alive',
        N'Calyx Champion; only active Champion in Calyx service; strategic deterrent asset',
        N'Kornél Nádasdy is seven feet eleven inches, with proportions that have ceased to resemble ordinary human construction — his reach is extraordinary, his bone density such that conventional weapons require specific placement to harm him effectively, his metabolism operating at a rate that requires twice the caloric intake of an ordinary man. He has not spoken to anyone in a non-command capacity in three years, which is partly a function of the isolation that comes with his appearance and partly a choice he made at some point and cannot precisely date. He is the most physically capable Calyx soldier alive. He is also very lonely, in a way he has not spoken aloud since the third infusion.',
        N'Kornél is the cost of the highest achievement the Transmutation system produces — a man who has become something extraordinary and has lost the ordinary thing he did not know he needed.',
        N'No POV.',
        N'House Calyx; strategic command; assigned as needed across the eastern theater',
        241, 157, N'athletic',
        N'black', N'close-cropped', N'short',
        N'amber-gold', N'deep brown', N'scarred',
        N'Pronounced — form is clearly post-human; extreme height, dramatically altered proportions; amber-gold eyes; vascular prominence across entire visible body; proportions inhuman at a glance',
        N'Carries himself through space that was built for smaller people; moves with the precision of someone who has learned to be careful about what he touches',
        N'Calyx military uniform modified for his dimensions; no decorative elements; the Champion''s mark covers his left hand and forearm',
        N'Champion — many infusions over twenty years; extreme physical enhancement; post-human form',
        N'Strategic briefings where required. Training alone, in the pre-dawn, in a space cleared for him. Eating — twice what an ordinary man eats. Long silences in his assigned quarters.',
        N'He is lonely. This is the thing he cannot say. There is no one his size to talk to. There are very few people who can look at him without flinching, and the ones who can are officers for whom he is primarily an asset. He used to tell himself the isolation was manageable. He has stopped telling himself this and has not yet found anything to replace it with.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Eastern Calyx military theater; strategic deployment',
        N'0', N'0',
        N'Immense Eastern European Champion warrior, clearly post-human proportions, extreme height, black close-cropped hair, amber-gold eyes, deep brown scarred skin, military uniform modified for inhuman dimensions, stone fortress courtyard, medieval fantasy, imposing and lonely --ar 2:3',
        N'A 44-year-old Calyx Champion of extreme inhuman proportions and height, black hair, amber-gold eyes, deep brown skin, standing alone in a medieval stone fortress courtyard',
        0, 0
    );
    PRINT 'Kornél Nádasdy seeded.';
END
ELSE PRINT 'Kornél Nádasdy already exists.';
GO

-- 51. Dömötör Bánffy
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Dömötör Bánffy')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Dömötör Bánffy', N'domotor-banffy', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Dömötör Bánffy', N'domotor-banffy', N'Dömötör', N'Bánffy', N'',
        N'human', N'human', N'male', N'he/him', 58, N'alive',
        N'Senior Calyx officer; commanded the relief column that arrived too late at the Siege of Veszkény',
        N'Dömötör Bánffy commanded the relief column for the Siege of Veszkény ten years ago. The column arrived six hours after the garrison fell. Four hundred and twelve soldiers died in those six hours. He has been writing his account of why the column was delayed for ten years. He has finished it forty times and burned it forty times, because each version contains a different distribution of responsibility among himself, his supply officer, the lord who issued the original orders, and the road conditions. He has not yet found the version that is true.',
        N'Dömötör is a man doing penance for a specific failure through an act of writing that cannot produce absolution because he cannot determine what he is actually guilty of.',
        N'No POV.',
        N'House Calyx; capital command staff; field consultation as required',
        179, 86, N'stocky',
        N'grey', N'neat', N'short',
        N'dark brown', N'warm tan', N'heavily lined',
        N'none',
        N'Solid and steady in company; alone he is very still for long stretches that others take for calm',
        N'Senior officer''s formal uniform; nothing that draws attention; no campaign decorations despite an extensive record',
        N'none',
        N'Command staff duties. Writes in the evenings in a room he keeps locked. Burns what he writes. Has been doing this for ten years.',
        N'He has considered every variable in the delay. He has never reached a version that fully exculpates him, and never reached one that fully convicts him. What he has reached, in forty drafts, is this: he made a decision at a road junction that cost three hours, and that decision was defensible by every criterion he was trained on, and four hundred and twelve people died because of it. He does not know what to do with something that is both defensible and catastrophic.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Calyx capital command staff; eastern field consulting',
        N'0', N'0',
        N'Heavy-featured Eastern European senior officer, grey neat hair, dark brown eyes, warm tan heavily lined face, solid steady posture, candlelit private room with papers and a fire, medieval fantasy, somber --ar 2:3',
        N'A 58-year-old senior military officer with grey hair and a heavily lined face, sitting alone at a candlelit desk with papers in a medieval stone room',
        0, 0
    );
    PRINT 'Dömötör Bánffy seeded.';
END
ELSE PRINT 'Dömötör Bánffy already exists.';
GO

-- 52. Ágota Kolozsvári
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ágota Kolozsvári')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ágota Kolozsvári', N'agota-kolozsvari', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Ágota Kolozsvári', N'agota-kolozsvari', N'Ágota', N'Kolozsvári', N'',
        N'human', N'human', N'female', N'she/her', 39, N'alive',
        N'Calyx scholar; studies religious texts of the Fallen Houses; Cresset reexamination',
        N'Ágota Kolozsvári has been studying the Fallen Houses'' religious traditions for twelve years. Cresset is the one she cannot put down. Three years ago she found evidence in a comparative textual study that Cresset''s fall was not military defeat — the conventional account — but an internal religious schism so severe that the winning faction destroyed all primary documentation and absorbed themselves into a surviving House under a negotiated silence. She cannot prove this. What she can demonstrate is that the textual record has gaps of a specific kind — not damage, not decay, but selective absence — that suggest intentional removal.',
        N'Ágota has found evidence of a cover-up large enough to have been a House. The story is whether the House that absorbed Cresset''s survivors is still carrying the arrangement — and whether anyone still alive would recognize what she is describing.',
        N'No POV.',
        N'House Calyx; capital archive, religious studies division',
        164, 62, N'average',
        N'dark brown', N'loosely tied', N'long',
        N'dark brown', N'warm brown', N'clear',
        N'none',
        N'The intensity of someone following a thread through dense material; loses track of physical surroundings when absorbed',
        N'Scholar''s plain dark dress; always a notebook; ink on her left hand',
        N'none',
        N'Works in the archive six days a week. Writes correspondence to scholars in other Houses who do not know why she is asking about specific textual gaps. Reviews her evidence in the evenings.',
        N'She has identified the most likely candidate for which surviving House absorbed Cresset''s winners. She has not written this down anywhere because the candidate is a current House in good standing and the evidence is circumstantial. She is also aware that if she is right, and if anyone from that House''s current leadership understands the historical arrangement, her research puts her in a dangerous position. She is continuing anyway because she is a scholar and she cannot not continue.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Calyx capital archive; occasional travel for textual research',
        N'0', N'0',
        N'Intense Eastern European woman scholar, dark brown loosely tied long hair, dark eyes, warm brown complexion, plain dark dress, ink on left hand, surrounded by comparative religious texts in archive, medieval fantasy --ar 2:3',
        N'A 39-year-old female scholar with dark brown hair and ink-stained hands, intensely focused over comparative religious texts in a medieval stone archive',
        0, 0
    );
    PRINT 'Ágota Kolozsvári seeded.';
END
ELSE PRINT 'Ágota Kolozsvári already exists.';
GO

-- 53. Levente Wass
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Levente Wass')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Levente Wass', N'levente-wass', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Levente Wass', N'levente-wass', N'Levente', N'Wass', N'',
        N'human', N'human', N'male', N'he/him', 21, N'alive',
        N'Calyx Myrmidon; six months in service; has killed three men in two engagements',
        N'Levente Wass grew up in a village two days east of the capital, where the war was something that happened at a remove and came back in the form of men with different faces than when they left. He is six months in service and has been in two engagements. In the first, he killed one man. In the second, two. He writes letters home to his mother that describe camp food and weather and the names of his bunkmates. He does not know why he cannot write about the engagements. He is not ashamed of what he did. He is not sure what he feels, and the not-knowing is what he does not write about.',
        N'Levente is the ordinary young soldier trying to locate his experience inside a framework the war has not given him. He is what the war does before it has done its worst.',
        N'No POV.',
        N'House Calyx; eastern border regiment, active service',
        174, 71, N'lean',
        N'light brown', N'short', N'short',
        N'blue-grey', N'fair', N'clear',
        N'none',
        N'Alert and functional; the unremarkable physical ease of someone young and fit; no particular tension visible',
        N'Standard Myrmidon kit; the regiment''s regimental mark on his shoulder patch; a writing kit in his pack',
        N'none',
        N'Drills. Sentry. Regular duties. Writes letters he finishes and mails. Sleeps adequately. Does not talk about the engagements.',
        N'He has not told anyone that after the second engagement he sat with one of the men he killed for a while before the burial details arrived. He does not know why he did this. He has been trying to work out what it meant. He has not arrived at a conclusion and suspects he will not arrive at one soon.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Eastern Calyx border regiment',
        N'0', N'0',
        N'Young Eastern European soldier, light brown short hair, blue-grey eyes, fair complexion, standard military kit, unreadable calm expression, military camp fire in background, medieval fantasy --ar 2:3',
        N'A 21-year-old soldier with light brown hair and blue-grey eyes, in standard military kit, with a quietly unreadable expression at a medieval camp fire',
        0, 0
    );
    PRINT 'Levente Wass seeded.';
END
ELSE PRINT 'Levente Wass already exists.';
GO

-- 54. Rebeka Teleki
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Rebeka Teleki')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Rebeka Teleki', N'rebeka-teleki', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Rebeka Teleki', N'rebeka-teleki', N'Rebeka', N'Teleki', N'',
        N'human', N'human', N'female', N'she/her', 45, N'alive',
        N'Merchant; trades in Scrying-derived goods; knows provenance of designs across Spheres',
        N'Rebeka Teleki has spent twenty years in the business of moving goods derived from Scrying design work — manufactured objects whose blueprints came from observed Spheres, built locally and sold through a network of buyers who consider their provenance irrelevant to their usefulness. She knows otherwise. She worked in design processing early in her career and can identify which design came from which Sphere by specific manufacturing signatures. Several of the goods she trades were designed from Sphere 31. She knows what the originals look like in their natural context. She does not mention this to anyone.',
        N'Rebeka is the merchant who sees the supply chain whole and trades in goods she knows were taken from people who never agreed to give them. Her story is whether this knowledge eventually becomes something she cannot keep neutral.',
        N'No POV.',
        N'House Calyx; eastern trade circuit; multi-House commerce network',
        167, 66, N'average',
        N'auburn', N'neatly arranged', N'medium',
        N'green', N'fair', N'clear',
        N'none',
        N'Professionally pleasant and materially focused; knows the value of everything she handles',
        N'Merchant''s quality practical clothing; a design catalogue she carries in a leather case',
        N'none',
        N'Travels the eastern trade circuit on a six-week rotation. Sells goods, negotiates contracts, reviews new design arrivals. Occasionally handles goods she recognizes from Sphere 31 originals and files them like any other item.',
        N'She recognizes approximately one in eight of the Scrying-derived goods she handles as having originated in Sphere 31 designs. She has never told anyone this. She has also never refused to sell one. She has told herself this is because the goods are already made, the designs already copied, and her refusal would change nothing. She thinks this is probably true. She also thinks she has been thinking it for too long to know if she still believes it.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Eastern Calyx trade circuit; multi-House commerce network',
        N'0', N'0',
        N'Practical Eastern European woman merchant, auburn neatly arranged hair, green eyes, fair complexion, quality practical clothing, leather design catalogue case, trading market, medieval fantasy --ar 2:3',
        N'A 45-year-old merchant woman with auburn hair and green eyes, professionally pleasant, holding a leather catalogue case in a medieval trading market',
        0, 0
    );
    PRINT 'Rebeka Teleki seeded.';
END
ELSE PRINT 'Rebeka Teleki already exists.';
GO

-- 55. Barnabás Kossuth
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Barnabás Kossuth')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Barnabás Kossuth', N'barnabas-kossuth', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Barnabás Kossuth', N'barnabas-kossuth', N'Barnabás', N'Kossuth', N'',
        N'human', N'human', N'male', N'he/him', 31, N'alive',
        N'Independent information broker; distant Kossuth family relation; unofficial and unauthorized',
        N'Barnabás Kossuth is distantly related to Lord Vladimír Kossuth — a third cousin''s son through a minor branch — which is a connection he has cultivated carefully without ever being explicit about it. The name opens doors. He is not employed by Calyx''s intelligence service or by anyone else. He collects information in the spaces the name gives him access to and sells it to whoever will pay, with no loyalty to any buyer. He has been doing this for four years and has not yet collected anything that has gotten him killed, which he considers evidence that he is skilled rather than evidence that he has been lucky.',
        N'Barnabás is the free agent operating inside a system that does not have official space for free agents — a man who will eventually collect something too important to sell safely.',
        N'No POV.',
        N'House Calyx; capital and eastern estates; multi-House information circuit',
        176, 73, N'lean',
        N'dark brown', N'neatly arranged', N'short',
        N'brown', N'warm tan', N'clear',
        N'none',
        N'Charming and comfortable in most social registers; slightly too interested in information to pass as merely curious',
        N'Well-dressed without ostentation; always looks like he belongs somewhere',
        N'none',
        N'Attends social events he has managed to be invited to. Meets buyers in neutral locations. Reviews what he has collected and decides what to sell and to whom. Maintains the Kossuth connection carefully — cordial notes, occasional appearances at family events, never overstaying.',
        N'He has not yet met Lord Vladimír, who does not know he exists. He intends to meet him eventually, when he has something worth bringing. He has been waiting for four years for that something. He has recently begun to wonder whether he will recognize it when it arrives, or whether it has already passed through his hands and he missed it.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Calyx capital; eastern estates; multi-House circuit',
        N'0', N'0',
        N'Young Eastern European man information broker, dark brown neatly arranged hair, brown eyes, warm tan complexion, well-dressed without ostentation, social event hall, charming composed expression, medieval fantasy --ar 2:3',
        N'A 31-year-old independent information broker with dark brown hair, well-dressed and charming, at a medieval noble social event',
        0, 0
    );
    PRINT 'Barnabás Kossuth seeded.';
END
ELSE PRINT 'Barnabás Kossuth already exists.';
GO

-- 56. Zsuzsa Deák
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Zsuzsa Deák')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Zsuzsa Deák', N'zsuzsa-deak', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Zsuzsa Deák', N'zsuzsa-deak', N'Zsuzsa', N'Deák', N'',
        N'human', N'human', N'female', N'she/her', 67, N'alive',
        N'Midwife and healer; eastern estates; forty years of practice; keeper of illegitimacy',
        N'Zsuzsa Deák has been delivering children in the eastern estates for forty years. She has delivered children for noble families, merchant families, and tenant farmers, and in all cases she has kept what she observed to herself. She has also, over forty years, developed the ability to identify specific physical markers that indicate a child''s parentage differs from what the family records will state. She knows of seven current children in noble families who are not the children of the men who believe they are. She has told no one. She has never been asked.',
        N'Zsuzsa carries forty years of secrets about the nobility''s actual bloodlines. She is the quiet catastrophe waiting in anyone''s genealogy. Her story is whether she dies with it or whether someone finally asks her the right question.',
        N'No POV.',
        N'House Calyx; eastern estates; midwife''s circuit',
        158, 64, N'stocky',
        N'white', N'loosely pinned', N'medium',
        N'grey', N'warm tan', N'very lined',
        N'none',
        N'Warm and matter-of-fact; the ease of someone who has seen everything and made peace with most of it',
        N'Midwife''s practical dark dress; always a delivery kit bag over one shoulder',
        N'none',
        N'Travels the estate circuit on request — no schedule, only summons. Delivers children when called. Treats ordinary ailments between deliveries. Returns to her cottage in the eastern village where she has lived for thirty years.',
        N'She has never used what she knows. She is not sure whether this is ethics or cowardice or simply the absence of a reason. She has occasionally thought about what she would say if one of the seven children grew up and asked her a specific question. She has a specific answer for each of the seven and has rehearsed none of them aloud.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Eastern Calyx estate circuit; local village',
        N'0', N'0',
        N'Old Eastern European midwife, white loosely pinned hair, grey eyes, warm tan very lined face, warm matter-of-fact expression, practical dark dress, midwife''s bag, eastern estate, medieval fantasy --ar 2:3',
        N'A 67-year-old midwife with white hair and a very lined warm face, carrying a delivery bag, with a calm matter-of-fact expression at a medieval estate doorway',
        0, 0
    );
    PRINT 'Zsuzsa Deák seeded.';
END
ELSE PRINT 'Zsuzsa Deák already exists.';
GO

-- 57. Balázs Széchenyi
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Balázs Széchenyi')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Balázs Széchenyi', N'balazs-szechenyi', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Balázs Széchenyi', N'balazs-szechenyi', N'Balázs', N'Széchenyi', N'',
        N'human', N'human', N'male', N'he/him', 40, N'alive',
        N'Engineer; maintains secondary Scrying installation; conducting unauthorized far-Sphere observation',
        N'Balázs Széchenyi maintains the secondary Scrying installation on the eastern border — a smaller, older facility that requires more hands-on calibration than the primary sites. In the course of that calibration work, he discovered that the installation''s geometry, when adjusted in a specific non-standard configuration, can reach Spheres much further from the standard membrane topology than any official installation is rated for. He has been visiting these distant Spheres alone, after his maintenance shifts, for eighteen months. What he has found there he has told no one.',
        N'Balázs has access to Spheres that no current Liturgy catalogue includes. What he is seeing may be the membrane''s true extent — or it may be something the Liturgy already knows about and has kept from the Houses.',
        N'No POV.',
        N'House Calyx; secondary Scrying installation, eastern border',
        177, 81, N'average',
        N'dark brown', N'unkempt', N'medium',
        N'dark brown', N'warm tan', N'weathered',
        N'none',
        N'Absorbed and slightly distracted; looks at components rather than people during conversations',
        N'Engineer''s work coat, always carrying calibration tools; chalk and grease marks on his sleeves',
        N'none',
        N'Maintenance of the installation — genuinely demanding work that takes most of the day. After shifts: two to three hours in the non-standard configuration, observing what he calls the deep Spheres. Files standard maintenance reports.',
        N'He has found three deep Spheres in eighteen months. One of them appears to have no membrane — no boundary between what he is and what is there. He does not know what that means and is afraid to investigate further. He is also unable to stop returning to look.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Eastern Calyx secondary Scrying installation',
        N'0', N'0',
        N'Absorbed Eastern European engineer, dark brown unkempt hair, dark eyes, weathered warm complexion, work coat with calibration tools and chalk marks, examining unusual Scrying installation configurations alone at night, medieval fantasy --ar 2:3',
        N'A 40-year-old engineer with dark brown unkempt hair, absorbed expression, calibrating a Scrying installation alone at night in a medieval stone chamber',
        0, 0
    );
    PRINT 'Balázs Széchenyi seeded.';
END
ELSE PRINT 'Balázs Széchenyi already exists.';
GO

-- 58. Illyés Bocskai
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Illyés Bocskai')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Illyés Bocskai', N'illyes-bocskai', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Illyés Bocskai', N'illyes-bocskai', N'Illyés', N'Bocskai', N'',
        N'human', N'human', N'male', N'he/him', 27, N'alive',
        N'Oathless; went Oathless six months ago after his unit was destroyed under suspicious orders',
        N'Illyés Bocskai''s unit was assigned to an advance position that the available intelligence made clearly untenable — a position with no retreat route, facing an engagement the unit''s sergeant told him privately could not be survived. The unit was destroyed. Illyés survived because he disobeyed a direct order to hold position and pulled himself and two others back before the line collapsed. Those two died in the withdrawal. He went Oathless because he concluded the unit was sent to die deliberately, as part of a maneuver that required a credible loss in that position to justify a territorial concession made the following week. He has been trying to find evidence of this for six months while also surviving as a newly Oathless man.',
        N'Illyés is the youngest Oathless character — still in the phase where the anger is fresh and the evidence he needs is real. His story is whether he finds it before someone finds him.',
        N'No POV.',
        N'Eastern Calyx border wilderness; Oathless territory',
        175, 74, N'lean',
        N'black', N'rough', N'short',
        N'dark brown', N'dark warm', N'weathered',
        N'none',
        N'Urgent and controlled; the constant motion of someone who knows he cannot stay still for long',
        N'Rough practical clothes, nothing identifying; all insignia removed; a pack he built himself',
        N'none',
        N'Moves through border terrain, avoiding Calyx patrols. Has been attempting to reach former regimental contacts who might know the command side of what happened at his position. Has made contact with two; both refused to speak to him.',
        N'He believes the maneuver that killed his unit was authorized at a level above his regiment''s commander and below the Lord''s council — a staff officer acting on policy that was never made explicit. He has a name. He cannot prove the name belongs to the decision. He is twenty-seven years old and has been Oathless for six months and is running out of people he can ask.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Eastern Calyx border wilderness',
        N'0', N'0',
        N'Young Eastern European Oathless man, black rough short hair, dark brown eyes, dark warm weathered complexion, rough practical clothes with all insignia removed, urgent controlled movement, border forest, medieval fantasy --ar 2:3',
        N'A 27-year-old newly Oathless soldier with black hair and dark eyes, urgent and controlled, in rough practical clothes in a medieval border forest',
        0, 0
    );
    PRINT 'Illyés Bocskai seeded.';
END
ELSE PRINT 'Illyés Bocskai already exists.';
GO

-- 59. Adéle Brunner
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Adéle Brunner')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Adéle Brunner', N'adele-brunner', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Adéle Brunner', N'adele-brunner', N'Adéle', N'Brunner', N'',
        N'human', N'human', N'female', N'she/her', 59, N'alive',
        N'Senior estate manager; Sphere 31 origin; thirty-seven years in Calyx; most capable administrator in the eastern province',
        N'Adéle Brunner was taken from Sphere 31 at twenty-two from a city she has since determined, from Scrying glimpses, was Vienna. She arrived in Calyx as a domestic, was placed in a noble household, and was recognized within three years by the household''s head as someone capable of managing more than a kitchen. She has been managing estates in increasing complexity for thirty-four years. She is now the senior administrator for the largest estate complex in the eastern province and is considered by the families she serves as simply the best at the work, with no particular thought given to her origins. She sometimes catches herself counting in German, which she no longer speaks to anyone.',
        N'Adéle is the Sphere 31 arrival who became the best version of what the Cauld asked of her and is now, at fifty-nine, wondering what the best version of herself would have been in the world she left.',
        N'No POV.',
        N'House Calyx; eastern province estate complex; Sphere 31 origin',
        165, 68, N'average',
        N'silver', N'neatly arranged', N'medium',
        N'blue', N'fair', N'clearly lined',
        N'none',
        N'Precise and efficient with a warmth that is genuine and not performative; runs complex systems as naturally as breathing',
        N'Senior administrator''s practical dress in Calyx regional style, well-made; the estate manager''s seal ring on her right hand',
        N'none',
        N'Complex estate administration — accounts, staff management, supply coordination, tenant relations. Does it well and finds it satisfying. Counts in German when she is stressed. Does not think about Sphere 31 most days.',
        N'She has been in the Cauld for thirty-seven years, which is longer than she was alive before she arrived. She is not sure what she would be if she went back — whether the person she was at twenty-two would recognize the person she is at fifty-nine, and which of them would be right. She does not grieve this clearly. She finds it interesting, in the way a scholar finds something interesting, from a slight remove. She has told no one in Calyx anything about where she came from, because it stopped mattering to anyone around her a long time ago.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Eastern Calyx province estate complex; Sphere 31 origin',
        N'0', N'0',
        N'Distinguished older Germanic-featured woman administrator, silver neatly arranged hair, blue eyes, fair clearly lined complexion, efficient warm expression, well-made administrator''s dress with estate seal ring, stone estate management room, medieval fantasy --ar 2:3',
        N'A 59-year-old estate manager of Germanic heritage with silver hair and blue eyes, precise and warm, in well-made Calyx administrative dress in a medieval stone estate office',
        0, 0
    );
    PRINT 'Adéle Brunner seeded.';
END
ELSE PRINT 'Adéle Brunner already exists.';
GO

-- 60. Csongor Vay
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Csongor Vay')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Csongor Vay', N'csongor-vay', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Csongor Vay', N'csongor-vay', N'Csongor', N'Vay', N'',
        N'human', N'human', N'male', N'he/him', 19, N'alive',
        N'Calyx Myrmidon; youngest in his regiment; recommended for infusion; has agreed without fully understanding',
        N'Csongor Vay is the youngest soldier in the Beszterce Regiment, which is not a distinction he sought. He was recommended for infusion by his commander after two months of service, on the basis of physical capability and what his commander called an unusual natural toughness. He said yes. He said it in the way that people say yes when a superior asks them something that sounds like an opportunity and they do not yet know what they are agreeing to. He has since learned, from older soldiers, what infusion is. He is now waiting for a practitioner availability date and trying not to think about it.',
        N'Csongor is the 80% death rate made personal and immediate — a specific young man who has said yes to something he cannot unsay, waiting for the date.',
        N'No POV.',
        N'House Calyx; Beszterce Regiment, eastern border',
        177, 72, N'athletic',
        N'dark blond', N'short', N'short',
        N'blue', N'fair', N'clear',
        N'none',
        N'Physically capable and slightly too careful; the specific stillness of someone trying not to show what he is thinking',
        N'Standard Myrmidon kit, in good condition; nothing personal beyond a ring he was given before deployment',
        N'none',
        N'Full regiment duties. Drills particularly hard since the recommendation — he has decided that if he is going to do this he will at least go in at peak condition. Eats well. Sleeps less than he should.',
        N'He has done the arithmetic. He has heard the number from three different soldiers and it has been consistent: approximately one in five survive. He agreed to the infusion before he knew this number. He is trying to determine whether knowing the number changes what he should do, and whether there is a way to withdraw consent that would not mark him as someone who refused. He has concluded that there is not. He has not concluded what he is going to do.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Beszterce Regiment, eastern Calyx border',
        N'0', N'0',
        N'Young Eastern European soldier, dark blond short hair, blue eyes, fair complexion, athletic build, standard military kit, expression of controlled unease, military training ground, medieval fantasy --ar 2:3',
        N'A 19-year-old athletic soldier with dark blond hair and blue eyes, in standard kit, with controlled unease in his expression at a medieval military training ground',
        0, 0
    );
    PRINT 'Csongor Vay seeded.';
END
ELSE PRINT 'Csongor Vay already exists.';
GO

-- 61. Piroska Rédey
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Piroska Rédey')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Piroska Rédey', N'piroska-redey', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Piroska Rédey', N'piroska-redey', N'Piroska', N'Rédey', N'',
        N'human', N'human', N'female', N'she/her', 37, N'alive',
        N'Senior Scrying operator; unauthorized membrane stability research; understands installation variance',
        N'Piroska Rédey has been a Scrying operator for thirteen years and has spent the last five of them conducting unauthorized research into why some Scrying installations are more stable than others. The Liturgy''s official position is that stability variance results from site preparation quality and maintenance. Piroska has determined, through observations she has conducted without official sanction, that site preparation and maintenance account for less than half the variance. The remainder correlates with something she can only describe as membrane-inherent topology — some sites sit closer to natural thin points. She cannot explain how she reached this conclusion without admitting she has been observing the membrane directly rather than through standard protocols.',
        N'Piroska knows something fundamental about how the membrane works that the Liturgy does not know, or knows and has not shared. Her story is whether the knowledge is publishable or dangerous.',
        N'No POV.',
        N'House Calyx; primary Scrying installation, capital district',
        163, 57, N'lean',
        N'dark red', N'braided close', N'medium',
        N'green', N'fair', N'clear',
        N'none',
        N'Methodical and slightly inward; more comfortable with instruments than with conversations',
        N'Senior operator''s grey uniform; always carrying a measuring instrument of her own construction',
        N'none',
        N'Official observation shifts. Maintenance tasks she volunteers for because they give her proximity to the installation interface. Private measurement sessions logged as maintenance calibration.',
        N'She has been measuring membrane topology for five years without a theoretical framework that would allow her to publish. She has the data; she lacks the vocabulary to present it without either inventing new terminology — which would require explanation — or describing her methodology, which would expose the unauthorized observations. She has been writing a paper for two years that she has not shown to anyone.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Calyx capital Scrying installation',
        N'0', N'0',
        N'Methodical Eastern European woman operator, dark red braided hair, green eyes, fair complexion, senior grey uniform with personal measuring instrument, Scrying installation interface, medieval fantasy --ar 2:3',
        N'A 37-year-old Scrying operator with dark red braided hair and green eyes, methodically examining a Scrying installation interface with a personal measuring instrument',
        0, 0
    );
    PRINT 'Piroska Rédey seeded.';
END
ELSE PRINT 'Piroska Rédey already exists.';
GO

-- 62. Demeter Gorsa
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Demeter Gorsa')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Demeter Gorsa', N'demeter-gorsa', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Demeter Gorsa', N'demeter-gorsa', N'Demeter', N'Gorsa', N'',
        N'human', N'human', N'male', N'he/him', 80, N'alive',
        N'Oldest living Calyx witness to the fall of Sinter; pension holder; unreliable narrator of his own history',
        N'Demeter Gorsa was present when Sinter destroyed itself. He was thirty years old. He has been telling the story for fifty years and has told it so many times, to so many different audiences, with so many small adjustments for purpose and listener, that he has lost track of which version is closest to what he actually saw. He knows this about himself. He finds it concerning in a way that he can no longer fully feel — it has become a low-frequency dread he carries the way an old wound carries weather.',
        N'Demeter is the last eyewitness to a world-shaping event, and the worst possible last eyewitness, because he has been talking for fifty years and cannot tell anyone — including himself — which version is true.',
        N'No POV.',
        N'House Calyx; eastern province; pensioner''s accommodation',
        165, 58, N'lean',
        N'white', N'thin and unkempt', N'short',
        N'faded brown', N'pale', N'very deeply lined',
        N'none',
        N'Very slow and deliberate; talks as he moves, as if narrating himself; occasionally stops mid-sentence',
        N'Old man''s practical clothing, clean but worn; a pension token on a cord; a flask he keeps filled',
        N'none',
        N'Sits. Talks to anyone who will listen. Drinks moderately. Has been visited by five different parties in the last ten years asking about Sinter. Told each a different version.',
        N'He knows he has told incompatible versions to at least three of the five parties who visited. He also knows that somewhere in all the versions is the actual event, and he can sometimes feel the shape of it — what he saw, where he was standing, what the air smelled like — without being able to separate that core from the fifty years of retelling that have grown around it. He has been trying to write down only that core for three years and cannot get it onto paper in a form that satisfies him.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Eastern Calyx province; local town',
        N'0', N'0',
        N'Very old Eastern European man, white thin unkempt hair, faded brown eyes, pale very deeply lined face, old practical clothing, flask in hand, talking gesture, stone town square, medieval fantasy --ar 2:3',
        N'An 80-year-old man with white unkempt hair and very deeply lined pale face, gesturing as he talks, holding a flask in a medieval stone town square',
        0, 0
    );
    PRINT 'Demeter Gorsa seeded.';
END
ELSE PRINT 'Demeter Gorsa already exists.';
GO

-- 63. Zelma Aranka
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Zelma Aranka')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Zelma Aranka', N'zelma-aranka', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Zelma Aranka', N'zelma-aranka', N'Zelma', N'Aranka', N'',
        N'human', N'human', N'female', N'she/her', 43, N'alive',
        N'Village grain accounts administrator; found a ciphered payment notation from the Sinter Crisis period',
        N'Zelma Aranka manages the grain accounts for three villages in the eastern lowlands, which is unglamorous work done well by someone who cares about it. Two years ago, reviewing ledgers from a transfer of estate records, she found a notation from the Sinter Crisis era — a payment line entered in cipher, for an amount that would have been significant at the time, made to persons listed only as coded initials. She has been working on the cipher intermittently for two years. She has broken part of it. The payment was made from an account she can trace to a Calyx estate that still exists, owned by a family that still exists.',
        N'Zelma has stumbled into a financial record of something that someone paid to have done during the Sinter Crisis and does not know what that something was. She is about to find out.',
        N'No POV.',
        N'House Calyx; eastern lowlands village administration',
        160, 63, N'average',
        N'brown', N'simply pinned', N'medium',
        N'brown', N'warm tan', N'clear',
        N'none',
        N'Methodical and patient; the focus of someone who does careful work without drama',
        N'Village administrator''s practical dress; a ledger case she carries everywhere',
        N'none',
        N'Village account management — genuinely useful work. Reviews the cipher notation in the evenings, incrementally. Has told no one.',
        N'She has broken enough of the cipher to know the initials of the recipients. One set of initials matches a name she knows from Calyx historical records as someone who was in the Sinter territory in the final weeks. The payment amount, converted to current rates, was very large. She does not know what was being paid for, and she is not sure she wants to know, and she is very sure she cannot stop looking.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Eastern Calyx lowlands villages',
        N'0', N'0',
        N'Practical Eastern European woman village administrator, brown simply pinned hair, warm tan complexion, methodical focused expression, practical dress with ledger case, stone village office with old records, medieval fantasy --ar 2:3',
        N'A 43-year-old village administrator with brown pinned hair, methodically focused over old ledger records in a medieval stone village office',
        0, 0
    );
    PRINT 'Zelma Aranka seeded.';
END
ELSE PRINT 'Zelma Aranka already exists.';
GO

-- 64. Hungária Messzlényi
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Hungária Messzlényi')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Hungária Messzlényi', N'hungaria-messzlenyi', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Hungária Messzlényi', N'hungaria-messzlenyi', N'Hungária', N'Messzlényi', N'Lady',
        N'human', N'human', N'female', N'she/her', 50, N'alive',
        N'Lady of a border estate; Paladin; three infusions; physically powerful and emotionally distant',
        N'Hungária Messzlényi underwent her first infusion at thirty, her second at thirty-seven, her third at forty-four. She was told before the third that it was inadvisable — that the cumulative risk increased with each infusion and that she was already enhanced beyond the standard for her rank. She underwent it anyway. She is now six feet four, with the physical presence of someone who has been remade three times, and manages her border estate with a competence that everyone around her respects and no one around her is comfortable with. She has become, she knows, someone people are careful around.',
        N'Hungária is the question of what Transmutation costs in the self — whether what she gained is worth what she lost, and whether the person she was before the first infusion would have chosen this if she had known.',
        N'No POV.',
        N'House Calyx; border estate, western edge of the eastern province',
        192, 102, N'athletic',
        N'dark brown', N'closely pinned', N'medium',
        N'amber', N'warm olive', N'scarred',
        N'Evident enhancement — significant height, altered proportions; amber eyes with unusual depth; vascular prominence at temples and wrists',
        N'Carries herself through her estate with the care of someone who has learned to be precise about the space she occupies; physically commanding without intending to be',
        N'Lady''s formal dress modified for her height; the Paladin''s double mark on both hands',
        N'Paladin — three infusions over twenty years; significant physical enhancement',
        N'Estate management — which she is capable of and engaged by. Training in the private yard at dawn. Long silences on the estate walls in the evening, which her staff has learned to leave undisturbed.',
        N'She has been asking herself, for the past two years, whether the person she was at twenty-nine would have chosen this. She has concluded that the person she was at twenty-nine would not have made the third infusion. She has also concluded that the person she was at twenty-nine could not have survived the border engagements she has survived since, which means the question is circular. She has not found a way out of the circle.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Western border estate, eastern Calyx province',
        N'0', N'0',
        N'Tall imposing Eastern European Paladin noblewoman, dark brown closely pinned hair, amber eyes, warm olive scarred complexion, modified formal dress, precise powerful movement, stone border estate wall at evening, medieval fantasy --ar 2:3',
        N'A 50-year-old Paladin noblewoman of exceptional height and presence, dark brown hair, amber eyes, in modified formal dress standing on a stone border estate wall',
        0, 0
    );
    PRINT 'Hungária Messzlényi seeded.';
END
ELSE PRINT 'Hungária Messzlényi already exists.';
GO

-- 65. Endre Szapáry
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Endre Szapáry')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Endre Szapáry', N'endre-szapary', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Endre Szapáry', N'endre-szapary', N'Endre', N'Szapáry', N'',
        N'human', N'human', N'male', N'he/him', 38, N'alive',
        N'Military supply contractor; embedded in logistics fraud he cannot exit',
        N'Endre Szapáry supplies military materiel to Calyx border regiments under contract and has done so profitably for eight years. The profit has been supplemented for three years by bribes to a logistics officer who steers contracts to him regardless of competitive pricing. He is aware the arrangement is illegal. He is also aware that the logistics officer knows enough about the financial irregularities on his side — which are genuine, not fabricated — to destroy him if the arrangement ended badly. He has been trying to find a way to extract himself from the arrangement for a year and a half and has not found one.',
        N'Endre is the corruptant who discovered that corruption has momentum — that it is easier to keep a corrupt arrangement running than to end it, and that this is how people stay in corrupt arrangements for the rest of their lives.',
        N'No POV.',
        N'House Calyx; military supply contract circuit, eastern border',
        177, 84, N'average',
        N'brown', N'neat', N'short',
        N'brown', N'medium warm', N'clear',
        N'none',
        N'Professionally confident with an undercurrent of watchfulness; careful about who is in a room before speaking',
        N'Prosperous contractor''s quality practical clothing; always has a contract document on his person',
        N'none',
        N'Supply management and contract administration, which he does competently. Monthly meetings with the logistics officer. Reviews his exit options in the evenings and finds them consistently inadequate.',
        N'He has considered three options: pay someone to document the officer''s side of the arrangement and use it to negotiate a clean exit; disappear from the contracting circuit entirely; or continue until either the arrangement is discovered or the officer rotates out. The first requires resources he does not have and could make things worse. The second would cost him everything he has built. The third is the one he keeps choosing by not choosing anything.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Eastern Calyx border contracting circuit',
        N'0', N'0',
        N'Professionally dressed Eastern European supply contractor, neat brown hair, medium warm complexion, watchful expression under confident manner, quality practical clothing, contract documents, medieval supply office, fantasy --ar 2:3',
        N'A 38-year-old military supply contractor with neat brown hair, professionally confident with a watchful undercurrent, holding contract documents in a medieval supply office',
        0, 0
    );
    PRINT 'Endre Szapáry seeded.';
END
ELSE PRINT 'Endre Szapáry already exists.';
GO

-- 66. Lujza Vécsey
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Lujza Vécsey')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Lujza Vécsey', N'lujza-vecsey', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Lujza Vécsey', N'lujza-vecsey', N'Lujza', N'Vécsey', N'',
        N'human', N'human', N'female', N'she/her', 29, N'alive',
        N'Liturgy scout; assigned to Calyx region; identifying new Scrying site candidates; withholding one',
        N'Lujza Vécsey works for the Liturgy as a field scout — she travels the physical terrain of the Calyx region looking for sites with the geological and atmospheric characteristics that correlate with Scrying installation suitability. She has found three candidates in the past year and reported two. The third is a site she found in a valley that is currently occupied by a small farming community that has been there for four generations. The Liturgy''s standard procedure for occupied candidate sites involves relocation. She has been sitting on the report for four months.',
        N'Lujza has found a site that would benefit the Liturgy and displace a community she has spent time in. She is deciding whether her job description has a limit.',
        N'No POV.',
        N'House Calyx; Liturgy field scouting circuit',
        164, 59, N'lean',
        N'dark brown', N'practical braid', N'long',
        N'hazel', N'warm tan', N'weathered',
        N'none',
        N'Easy in terrain and slightly uncomfortable in offices; the directness of someone who prefers the field',
        N'Liturgy scout''s practical traveling kit; no formal uniform in the field; the scout seal in a sealed case in her pack',
        N'none',
        N'Field scouting across the Calyx region. Surveying and mapping candidate sites. Writing reports. Not writing the report for the third site. Passing through the valley twice since the discovery, telling herself she is verifying measurements.',
        N'She has been in the valley three times. She knows the name of the headman and the name of his dog. She has not told anyone in the Liturgy the site exists. She is not sure whether she is going to report it, suppress it, or do something she has not yet figured out. She is also aware that the Liturgy''s territory surveys are thorough, and that if she suppresses the report and a different scout finds the site, the consequences for her are significant.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Calyx region field scouting circuit',
        N'0', N'0',
        N'Young Eastern European woman Liturgy scout, dark brown practical braided hair, hazel eyes, warm tan weathered face, practical traveling kit, easy in terrain, forested valley, medieval fantasy --ar 2:3',
        N'A 29-year-old Liturgy field scout with dark brown braided hair, practical traveling kit, easy and direct in expression, standing in a forested medieval valley',
        0, 0
    );
    PRINT 'Lujza Vécsey seeded.';
END
ELSE PRINT 'Lujza Vécsey already exists.';
GO

-- 67. Ákos Beöthy
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ákos Beöthy')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ákos Beöthy', N'akos-beothy', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Ákos Beöthy', N'akos-beothy', N'Ákos', N'Beöthy', N'Master',
        N'human', N'human', N'male', N'he/him', 55, N'alive',
        N'Master forgemaster; runs Calyx''s primary field equipment forge; producing substandard steel',
        N'Ákos Beöthy has run the Calyx eastern forge complex for eighteen years and has produced field equipment — blades, tools, hardware — to a consistent standard for most of that time. For two years he has been producing substandard steel. The fuel supply was compromised — a supplier substitution that changed the burn temperature in a way that affected tempering — and the resulting equipment fails faster than specification allows. He has filed eleven complaints with the procurement authority. He has been told to file no more complaints. The equipment is in the field.',
        N'Ákos is the craftsman who has been forbidden from fixing the thing he is responsible for — and who must decide whether that prohibition is the end of his responsibility or the beginning of something else.',
        N'No POV.',
        N'House Calyx; eastern forge complex, capital district supply chain',
        176, 95, N'stocky',
        N'grey', N'close-cropped', N'short',
        N'dark brown', N'dark warm', N'weathered',
        N'none',
        N'Heavy-set and deliberate; the physical authority of someone who works with his hands and has done it for thirty years',
        N'Forgemaster''s heavy work apron over practical clothing; burn marks and metal dust on his forearms',
        N'none',
        N'Runs the forge from dawn to late afternoon. Reviews the supply chain documentation he has kept since the first complaint — all eleven responses, all filed and dated. Considers his options.',
        N'He knows soldiers in the field are carrying equipment that will fail sooner than it should. He does not know specifically when or how many or who they are. He has been told the fuel problem has been corrected — which it has not; he can tell from the output. He is considering whether he can route a complaint past the procurement authority, and whether doing so would result in action on the steel or action on him.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Eastern Calyx forge complex; supply circuit',
        N'0', N'0',
        N'Stocky older Eastern European forgemaster, grey close-cropped hair, dark warm weathered complexion, heavy work apron with burn marks, solid deliberate posture, active medieval forge with fire and metal, fantasy --ar 2:3',
        N'A 55-year-old forgemaster with grey hair and a dark weathered face, solid in a heavy work apron at an active medieval forge',
        0, 0
    );
    PRINT 'Ákos Beöthy seeded.';
END
ELSE PRINT 'Ákos Beöthy already exists.';
GO

-- 68. Filep Dessewffy
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Filep Dessewffy')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Filep Dessewffy', N'filep-dessewffy', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Filep Dessewffy', N'filep-dessewffy', N'Filep', N'Dessewffy', N'',
        N'human', N'human', N'male', N'he/him', 72, N'alive',
        N'Head archivist; Calyx historical records; knows what has been removed and where the copies are',
        N'Filep Dessewffy has been head archivist of the Calyx historical records for thirty years. In that time, seventeen documents have been removed from the archive''s collection by parties with sufficient authority to do so — lords, senior officers, one Liturgy official. He kept copies of all seventeen. The copies are stored in a location he has not recorded in writing. He has not used them, displayed them, or told anyone they exist. He is seventy-two years old and has not yet decided what he is keeping them for.',
        N'Filep is the archivist who made himself a safeguard without knowing what he would be safeguarding against. He is the last line of documentary defense for historical truth, and he has never been asked to deploy it.',
        N'No POV.',
        N'House Calyx; capital historical archive',
        171, 66, N'lean',
        N'white', N'neat', N'short',
        N'pale grey', N'pale', N'deeply lined',
        N'none',
        N'Very precise in small physical movements; handles documents with the care of someone for whom paper is not metaphor',
        N'Archivist''s dark practical coat; spectacles he has worn for twenty years; ink on the right hand always',
        N'none',
        N'Archive management, which he does with the thoroughness of a man who loves it. Occasionally reviews the secret location in his memory — not the documents themselves, which he does not revisit, just the location, to confirm it is intact. Reads in the evenings. Sleeps adequately.',
        N'He has been head archivist long enough to have seen four political shifts, three of which produced document removal requests. He made copies because he believed historical record was more important than political convenience, and he still believes this. What he has not resolved is whether the copies are a resource — something to use — or simply evidence that he has done his job correctly. He suspects he will die without resolving this, and has begun to think about who he might tell the location to.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Calyx capital historical archive',
        N'0', N'0',
        N'Old Eastern European head archivist, white neat hair, pale deeply lined face, pale grey eyes, spectacles, dark archivist''s coat, ink on right hand, precise careful movement, vast candlelit archive, medieval fantasy --ar 2:3',
        N'A 72-year-old head archivist with white hair, spectacles, and ink-stained hands, precise in a dark coat among vast candlelit shelves in a medieval stone archive',
        0, 0
    );
    PRINT 'Filep Dessewffy seeded.';
END
ELSE PRINT 'Filep Dessewffy already exists.';
GO

-- 69. Maria Vasile
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Maria Vasile')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Maria Vasile', N'maria-vasile', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Maria Vasile', N'maria-vasile', N'Maria', N'Vasile', N'',
        N'human', N'human', N'female', N'she/her', 33, N'alive',
        N'Scrying operator; Sphere 31 origin; raised in Calyx from childhood; anomalous membrane sensitivity',
        N'Maria Vasile was taken from Sphere 31 at age eight, placed in domestic service in an eastern estate household, and educated above the standard for her position by an archivist in the household who recognized her ability. She is now a trained Scrying operator with six years of service and a solid professional record. She has also, since early childhood, been able to sense the membrane without instruments — a low pressure she has learned to interpret as proximity and stability, which she has never reported because she learned before she was ten years old that saying things that make people look at you strangely has consequences.',
        N'Maria is the Sphere 31 arrival who grew up inside the very system that extracted her, and who carries an ability she cannot explain that the system she works for would urgently want to know about.',
        N'No POV.',
        N'House Calyx; primary Scrying installation; Sphere 31 origin',
        163, 58, N'lean',
        N'dark brown', N'braided', N'long',
        N'dark brown', N'warm medium', N'clear',
        N'none',
        N'Composed and careful; the watchfulness of someone who learned early to monitor how she is being perceived',
        N'Standard senior operator''s grey uniform; a small worn piece of jewelry she has had since before Sphere 31 transit',
        N'none',
        N'Observation shifts. Files complete and accurate reports. Occasionally senses the membrane''s condition before the instruments register it and preemptively adjusts calibration — logged as technician''s intuition.',
        N'She remembers Sphere 31 in fragments: a specific smell, a sound of vehicles she cannot name in Cauld language, a woman''s face. She does not know if the woman was her mother. She has watched Sphere 31 Sphere assignments with the specific and self-aware hope that she will never be assigned to the location she came from, because she does not know what she would do if she recognized it. She suspects she would not report it.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Calyx primary Scrying installation; Sphere 31 origin',
        N'0', N'0',
        N'Young Eastern European woman Scrying operator with Romanian heritage, dark brown braided hair, dark eyes, warm medium complexion, composed watchful expression, grey senior operator''s uniform, Scrying installation, medieval fantasy --ar 2:3',
        N'A 33-year-old woman Scrying operator with dark brown braided hair and composed watchful eyes, in grey senior operator''s uniform at a medieval Scrying installation',
        0, 0
    );
    PRINT 'Maria Vasile seeded.';
END
ELSE PRINT 'Maria Vasile already exists.';
GO

-- 70. Bogumił Kádár
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Bogumił Kádár')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Bogumił Kádár', N'bogumil-kadar', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Bogumił Kádár', N'bogumil-kadar', N'Bogumił', N'Kádár', N'',
        N'human', N'human', N'male', N'he/him', 36, N'alive',
        N'Senior Myrmidon; passed over for infusion three times; best tactical mind in his regiment',
        N'Bogumił Kádár has served in the Calyx military for fourteen years and is widely acknowledged within his regiment as its best tactical mind — not its best fighter, though he is capable, but the person the sergeants and junior officers defer to when the engagement does not go as planned. He has been passed over for infusion three times. Each time, a candidate he considers his inferior in judgment and experience has been selected instead. He has not asked why. He suspects, and is probably right, that the answer is that someone senior finds him more useful as a Myrmidon with his knowledge than as a Knight whose patronage obligations would shift.',
        N'Bogumił is the brilliant subordinate being deliberately kept subordinate — the man whose value depends on his dependency, and who has not yet decided what to do about this.',
        N'No POV.',
        N'House Calyx; eastern border regiment, senior Myrmidon',
        180, 83, N'athletic',
        N'dark brown', N'short and practical', N'short',
        N'grey-green', N'medium warm', N'weathered',
        N'none',
        N'Alert and precise; the physical ease of someone who has made peace with his body as a tool; watches officers with a quality of attention they cannot identify as appraisal',
        N'Standard Myrmidon field kit, well-maintained; no insignia beyond his rank; always has a tactical map folded in a coat pocket',
        N'none',
        N'Regimental duties at a senior level. Informal consultation from junior officers who know where the actual knowledge in the regiment lives. Tactical analysis in the evenings. Has been thinking, for six months, about what he would do if he were offered infusion by someone outside his current command chain.',
        N'He has identified the officer whose patronage he believes is suppressing his infusion candidacy. He cannot prove it and is not sure proving it would change anything. He has also identified that the officer''s power over him diminishes exactly as much as Bogumił''s options increase — and that the only way to increase his options without going Oathless is to become so visibly indispensable in a field engagement that passing him over again becomes politically untenable. He is waiting for that engagement.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Eastern Calyx border regiment',
        N'0', N'0',
        N'Experienced Eastern European senior Myrmidon, dark brown short hair, grey-green eyes, medium warm weathered complexion, standard military kit with tactical map in pocket, alert precise posture, military planning ground, medieval fantasy --ar 2:3',
        N'A 36-year-old senior Myrmidon with dark brown hair and grey-green alert eyes, precise and capable, with a tactical map in his coat pocket at a medieval military planning ground',
        0, 0
    );
    PRINT 'Bogumił Kádár seeded.';
END
ELSE PRINT 'Bogumił Kádár already exists.';
GO
