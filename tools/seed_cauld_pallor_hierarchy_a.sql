SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ============================================================
-- HOUSE PALLOR — UPPER HIERARCHY SEED (PART A)
-- The Cauld | UniverseId: 0197E9C9-0002-7000-8000-000000000002
-- Generated: 2026-07-04
-- Ruling Family (9) + Political Cabinet (7) + Military Command (8) + Consort (1) = 25 total
-- ============================================================

-- ============================================================
-- RULING FAMILY
-- ============================================================

-- 1. Lord Aldwyn Cairn — Lord of House Pallor
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Aldwyn Cairn')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Aldwyn Cairn', N'aldwyn-cairn', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Aldwyn Cairn', N'aldwyn-cairn', N'Aldwyn', N'Cairn', N'Lord', N'human', N'human',
        N'male', N'he/him', 63, N'alive',
        N'Lord of House Pallor',
        N'Aldwyn Cairn has ruled House Pallor for twenty-two years, long enough that even his failures have acquired the texture of policy. He is sixty-three — broad-shouldered in the way of men who were once physically imposing and are now simply large, the soldier''s frame gone to statesman. His iron-grey hair is kept close; his hands, when still, rest flat on surfaces as though measuring them. He convened the Kellmouth Accord fifteen years ago, a tri-council summit intended to unify the three peoples'' veto councils into a single legislative voice. The Morvic delegation walked out on the second day over a seating precedence the Anglic faction claimed, and Aldwyn failed to intervene before the walkout became irrevocable. The Morvic have used their permanent veto as a lever ever since, requiring political payment before any significant military decision can proceed. He has learned to pay. He has not forgiven himself for needing to.',
        N'The flawed patriarch whose unspoken knowledge forms the structural fault line of the House; his chosen silence is the mechanism that holds everything together and the guarantee it will eventually collapse.',
        N'No POV.',
        N'House Pallor; Anglic lowlands, Channel coast',
        188, 91,
        N'broad-shouldered; the soldier''s frame gone to statesman; thick through the chest; carries the weight of decades in his posture',
        N'iron-grey', N'close-cropped', N'short', N'grey', N'pale, weathered', N'deep-lined, sun-scored',
        N'none',
        N'Deliberate, never hurried. Sits only in chairs that face the room''s entrance. Speaks with pauses his subordinates have learned to wait out without filling.',
        N'Heavy wool and leather in House Pallor''s coastal grey and deep blue. A chain of office from his father''s time, heavier than it looks. Never armed in council; always armed in private.',
        N'none',
        N'Dawn review of dispatches with the Chancellor. Morning council when parliament sits. Afternoons to the war table or intelligence briefing. Evenings he walks the rampart alone — a habit begun when Rowena died that has not stopped in fourteen years.',
        N'The Kellmouth walkout was engineered by his own Chancellor of the time — now dead — who was paid by House Garnet to ensure tri-council unification failed. Aldwyn discovered this nine years later through intelligence Oswald Wraith surfaced. He has never acted on it because doing so now would expose that he knew for nine years and chose silence — an admission that would end his rule more certainly than any Morvic veto.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'The Channel lowlands; the war council chamber; the Lord''s formal receiving halls; occasional inspection visits to the northern Scrying installation. Does not travel into Morvic territory without weeks of political preparation.',
        N'0', N'0',
        N'Portrait of a weathered medieval lord, early sixties, iron-grey close-cropped hair, grey eyes, broad-shouldered in heavy dark wool coastal grey and deep blue, chain of office; war council chamber with nautical charts and a brass steam-powered scrying apparatus; medieval European steampunk; dramatic side lighting; dignified and burdened',
        N'A medieval lord in his early sixties at the head of a war table. Iron-grey close-cropped hair, grey eyes, and a deeply lined weathered face. Heavy dark wool in coastal grey and deep blue. A heavy chain of office. Maps and a brass-fitted scrying apparatus behind him. Medieval European with steampunk elements.',
        0, 0
    );
END
ELSE PRINT 'Aldwyn Cairn already exists.';
GO

-- 2. Lady Rowena Cairn (née Bevan) — Deceased Spouse
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Rowena Cairn')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Rowena Cairn', N'rowena-cairn', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Rowena Cairn', N'rowena-cairn', N'Rowena', N'Cairn', N'Lady', N'human', N'human',
        N'female', N'she/her', 49, N'dead',
        N'Former Lady-Consort of House Pallor; deceased',
        N'Rowena Cairn was born Rowena Bevan of the Kellian fishing clans, the third daughter of a coastal alderman whose primary value to the Cairn family was his faction''s four council votes. She transformed a diplomatic convenience into a personal force: the Kellian council voted with Aldwyn on thirteen consecutive resolutions during her lifetime and have voted against him on nine of the last eleven since her death. She was slight and dark-auburn haired, with the hazel eyes and olive complexion of the Kellian coast — she moved through the formal halls of Pallor as though the ceremony were an inconvenience she had agreed to accommodate. She died at forty-nine during the coastal fever, having refused to leave the infirmary stations she had ordered established along the waterfront settlements. She sat with the sick for eleven days. Those who were there say she walked in knowing the risk and never asked to be recorded as having done so.',
        N'The absent moral center whose influence persists through every character who loved or relied on her; her death is the wound the House has not treated and will not name.',
        N'No POV.',
        N'House Pallor; Kellian coast, western fishing clans; born Bevan family',
        165, 58,
        N'slight, long-limbed; moved with deliberate economy',
        N'dark auburn', N'pinned back in working style', N'long', N'hazel', N'olive', N'clear, fine-lined',
        N'none',
        N'Moved through formal ceremony as though it were an inconvenience she had agreed to accommodate. Rarely still — always doing something with her hands.',
        N'Kellian working wool in coastal blues and undyed grey. Functional over formal. Wore a single piece of Kellian shellwork at the throat on state occasions.',
        N'none',
        N'Deceased. In her time: dawn correspondence, mid-morning touring the market districts and waterfront settlements she considered her real constituency, afternoons managing Kellian council relations. She rarely ate formally if she could avoid it.',
        N'Before she died, Rowena wrote four letters to Gwenith Aldermoor with specific instructions for their delivery. Three have been delivered at undisclosed times to undisclosed recipients. The fourth Gwenith has not delivered; it describes a private meeting Rowena had with the Liturgy Liaison six months before her death — a meeting Aldwyn has never been told occurred and whose subject Gwenith will not name.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Deceased. Her range was the Kellian coast, the infirmary stations, the waterfront settlements, and the formal council halls she could not avoid.',
        N'0', N'0',
        N'Portrait of a medieval lady in her forties, dark auburn hair pinned back, hazel eyes, olive complexion, slight in Kellian working wool and coastal blue; an infirmary doorway background; medieval European; candlelight; intelligent and quietly forceful expression',
        N'A medieval lady in her mid-forties stands in a doorway between a council chamber and an infirmary. Dark auburn hair pinned back, hazel eyes, olive skin. She wears functional Kellian wool in coastal blues. A piece of shellwork rests at her throat. Medieval setting, candlelight, quietly determined.',
        0, 0
    );
END
ELSE PRINT 'Rowena Cairn already exists.';
GO

-- 3. Cerdic Cairn — The Heir
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Cerdic Cairn')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Cerdic Cairn', N'cerdic-cairn', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Cerdic Cairn', N'cerdic-cairn', N'Cerdic', N'Cairn', N'', N'human', N'human',
        N'male', N'he/him', 31, N'alive',
        N'Heir to House Pallor; eldest child of Lord Aldwyn Cairn',
        N'Cerdic Cairn is thirty-one years old and has been the heir for all thirty-one of them, a weight he carries with the particular tension of someone who has never been permitted to choose anything else. He has his father''s grey eyes and his mother''s Kellian olive complexion, a combination that reads as perpetual alertness. He did not undergo Transmutation — his father forbade it on the grounds that the Heir cannot be risked in infusion — and Cerdic considers this the defining injustice of his life, though he would not say so to anyone except his sister Morwenna. He chafes against the tri-council veto system with a fury he keeps parliamentary in public and does not bother to keep parliamentary in private. He believes his father''s patience in the face of Morvic obstructionism has let three Draught raids go inadequately answered and that the channel coast is measurably less safe for it.',
        N'The heir whose impatience is both the House''s danger and its possible salvation — he is not wrong about what needs to change, only about how fast it can change without breaking what holds the House together.',
        N'No POV.',
        N'House Pallor; Anglic-Kellian lineage, Channel coast',
        183, 82,
        N'lean and angular; coiled tension he has not fully learned to disguise',
        N'dark brown', N'brushed back', N'medium', N'grey', N'olive-pale', N'clear, fine-featured',
        N'none',
        N'Quick-moving, rarely settled. Walks faster than the situation requires. Interrupts, catches himself, does not apologize.',
        N'Pallor military-formal: grey wool, minimal ornamentation. Avoids the ceremonial clothes his father favors. Wears a knife he has not yet had occasion to use.',
        N'none',
        N'Military briefings he attends as observer. Council sessions he endures. Evenings riding the coast road alone when he can manage it. He reads widely and admits it to no one he thinks it would impress.',
        N'Cerdic has been in quiet encrypted correspondence with a Draught intelligence contact — a captain who participated in the third Channel breach. He is not negotiating surrender; he believes the Draught raids are being directed by someone inside the Kellian council who profits from coastal disruption. He is probably correct, and has no idea that the Spymaster Oswald Wraith has been watching the same contact for two years and is waiting to see what Cerdic does with the information before deciding whether to intervene.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'The Channel coast; the war table observation gallery; the heir''s formal offices he uses as rarely as possible; the coast road he rides alone.',
        N'0', N'0',
        N'Portrait of a young medieval lord in his early thirties, dark brown hair brushed back, grey eyes, olive-pale complexion, lean in grey military wool; a coastal fortification; medieval European; natural morning light; intelligent and impatient expression held in check',
        N'A young medieval lord in his early thirties at a coastal fortification wall. Dark brown hair brushed back, grey eyes, olive-pale skin. Grey military wool with minimal ornamentation. His expression is alert and restrained. A grey sea behind him. Medieval European.',
        0, 0
    );
END
ELSE PRINT 'Cerdic Cairn already exists.';
GO

-- 4. Dame Morwenna Cairn — Second Born; Knight-rank; Military Liaison
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Morwenna Cairn')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Morwenna Cairn', N'morwenna-cairn', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Morwenna Cairn', N'morwenna-cairn', N'Morwenna', N'Cairn', N'Dame', N'human', N'human',
        N'female', N'she/her', 28, N'alive',
        N'Second born of Lord Pallor; Military Liaison to the Myrmidon Corps; Knight-rank',
        N'Morwenna Cairn is twenty-eight years old and spent six of them doing what the second-born of a ruling house can do that the heir cannot — she underwent Transmutation at twenty-two and survived. She is the first of the Cairn ruling line in three generations to take infusion, and the fact of it has given her a standing among soldiers that her brother Cerdic will never acquire by title alone. She has her mother''s hazel eyes and auburn coloring, her frame marginally heightened and densified from the infusion in the efficient, purposeful way of Knight-grade augmentation in House Pallor. She serves as Military Liaison between her father''s cabinet and the Corps Commander, a position she was given partly to honor her Transmutation and partly because she is better at managing Paladin Bran Morcant than anyone else in the family.',
        N'The second child whose capability exceeds the heir''s and whose private uncertainty about the terms of her own survival forms the most interesting fault line in the ruling family.',
        N'No POV.',
        N'House Pallor; Anglic-Kellian lineage, Channel coast',
        171, 70,
        N'compact and efficient; Knight-grade density adds substance without obvious mass; moves like someone who has learned to trust her body',
        N'auburn', N'pulled back and pinned for work', N'long', N'hazel', N'olive', N'clear, fine',
        N'Subtle height gain, increased density',
        N'Economic, purposeful movement. No wasted motion. Watches before speaking in unfamiliar rooms.',
        N'Corps liaison dress: half-armored over Pallor grey. Functional and deliberately not ceremonial. Wears the Knight''s seal at her belt rather than her throat.',
        N'Knight-rank Transmutation: modest increase in physical density and resilience; heightened muscle efficiency',
        N'Early mornings in the Corps training yards — she runs with junior officers, which Commander Morcant considers appropriate and Cerdic finds infuriating. Afternoons in liaison briefings or at the war table. Evenings reviewing Corps operations reports her father receives only in summary.',
        N'Morwenna does not know that the Corps Practitioner administered a diluted infusion dose at her Transmutation — a quiet safeguard applied without consent to ruling-family candidates. She recently found an oblique reference in an archived medical record she was not supposed to see that suggests her dosage was below standard protocol. She has not confirmed what this means for the genuine strength of her augmentation and she is afraid to ask anyone who would know, because the Infirmary Commander who keeps the honest ledger is not someone she knows how to approach without alerting someone else first.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'The Corps training yards; the war table liaison offices; coastal fortifications; occasional field inspection along the northern installations.',
        N'0', N'0',
        N'Portrait of a young noblewoman-knight in her late twenties, auburn hair pinned back, hazel eyes, olive complexion, compact in half-armor over Pallor grey; a training yard or coastal fortification; medieval European steampunk; natural light; capable and quietly watchful expression',
        N'A young noblewoman-knight in her late twenties in a training yard. Auburn hair pulled back, hazel eyes, olive skin. Half-armor over grey wool. A Knight seal at her belt. Her posture is economic and assured. Medieval European, natural light.',
        0, 0
    );
END
ELSE PRINT 'Morwenna Cairn already exists.';
GO

-- 5. Saoirse Cairn — Ward; candidate for diplomatic marriage
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Saoirse Cairn')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Saoirse Cairn', N'saoirse-cairn', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Saoirse Cairn', N'saoirse-cairn', N'Saoirse', N'Cairn', N'', N'human', N'human',
        N'female', N'she/her', 17, N'alive',
        N'Ward of House Pallor; niece of Lord Aldwyn Cairn; candidate for diplomatic marriage to House Garnet',
        N'Saoirse Cairn is seventeen, a ward since her parents died in the coastal fever that took Rowena — she was four, and remembers Rowena as warmth and the smell of saltwater and nothing else specific. She has the Morvic dark eyes and angular features of her mother''s line alongside the Anglic pallor of her father''s, a combination that reads in the formal Pallor halls as carefully composed rather than naturally settled. She is being prepared for a marriage alliance with House Garnet that will be negotiated over the next two years, and she knows this the way she knows the weather — a fact of her environment, not a question she has been invited to consider. She is sharper than anyone responsible for her future has bothered to measure.',
        N'The political instrument who is becoming a person faster than the House can adjust for; her unformed future is the wildcard the institution has not accounted for.',
        N'No POV.',
        N'House Pallor; Morvic heritage through her mother; Anglic heritage through her father; ward raised in Pallor''s Channel coast household',
        163, 54,
        N'slight; still growing into her height; holds herself carefully in formal settings',
        N'dark brown-black', N'braided formally for court; loose in private', N'long', N'dark brown', N'olive-pale', N'clear, fine',
        N'none',
        N'Composed in formal settings — the posture is taught, not natural. In private rooms, quick and restless.',
        N'Court dress as required. In private: practical clothes she prefers. Wears nothing that identifies her allegiance because she has not been asked which it is.',
        N'none',
        N'Formal lessons and court preparation in the mornings. Afternoons nominally free but observed. She has been smuggling correspondence through the kitchen staff to a Liturgy initiate she met three months ago during the Liturgy''s formal visit to the House.',
        N'A Liturgy initiate who visited House Pallor three months ago told her, in a brief unsupervised exchange in the library, that she would be "looked at" before the year was out. Saoirse does not know what this means. She has told no one — not because she is afraid, but because she does not yet know whom to trust with information she doesn''t understand. She has been watching the Liturgy Liaison Eithne Colm closely since that conversation and has noted that Eithne watches her back.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'The formal Pallor household; the library; the kitchen passages she uses for private correspondence; the coastal gardens when permitted.',
        N'0', N'0',
        N'Portrait of a teenage noblewoman, seventeen, dark brown-black hair braided formally, dark brown eyes, olive-pale complexion, slight and composed in Pallor court dress; a library background with maps; medieval European; soft natural light; expression carefully composed with sharp attention underneath',
        N'A teenage girl of seventeen in court dress in a library. Dark brown-black hair braided formally, dark brown eyes, olive-pale skin. Her posture is careful and taught. Sharp attention behind a composed expression. Books and maps around her. Medieval European, soft light.',
        0, 0
    );
END
ELSE PRINT 'Saoirse Cairn already exists.';
GO

-- 6. Dowager Fionnuala Cairn — Aldwyn's mother; 81; still dangerous
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Fionnuala Cairn')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Fionnuala Cairn', N'fionnuala-cairn', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Fionnuala Cairn', N'fionnuala-cairn', N'Fionnuala', N'Cairn', N'Lady', N'human', N'human',
        N'female', N'she/her', 81, N'alive',
        N'Dowager of House Pallor; mother of Lord Aldwyn Cairn; informal controller of the Anglic council bloc',
        N'Fionnuala Cairn is eighty-one years old and has outlasted two Lords before her son, three Spymasters, and what she refers to as "the Garnet pretense" — her term for House Garnet''s fiction that they pursue the Scrying installations in good faith. She is small now in the way of very old women who were never large, white-haired and pale-eyed, and she moves through the formal halls of Pallor with the unhurried authority of someone who has nothing left to perform for. She controls three Anglic council votes through personal loyalties that predate her son''s rule — men and women now in their sixties who attended her husband''s court as children and have not revised their allegiances since. She is formally retired from everything. She is not retired from anything.',
        N'The institutional memory and shadow power behind the current Lord; her past decisions — one in particular regarding the current military Commander — have shaped the House''s power structure in a way that is only now becoming visible.',
        N'No POV.',
        N'House Pallor; Anglic highlands, northern shores; born to a border military family',
        162, 60,
        N'small and slight; white-haired; moves without hurry or hesitation',
        N'white', N'dressed formally; pinned close', N'short', N'pale blue', N'parchment-pale', N'deeply lined, fine-boned',
        N'none',
        N'Moves through formal spaces with the authority of someone who has nothing left to prove. Rarely raises her voice. Never needs to.',
        N'Formal Anglic dress: dark wool, no ornamentation she hasn''t earned. Wears her late husband''s seal ring, which has not been formally surrendered to the current Lord.',
        N'none',
        N'Late mornings — she no longer rises at dawn. Correspondence with the three Anglic councillors whose votes she controls. Afternoon visitors she selects herself. She reads until the light fails and keeps no candle after.',
        N'Fionnuala arranged Aldwyn''s marriage to Rowena over Aldwyn''s objection — he had wanted to marry Edda, now the consort of Paladin Bran Morcant. Fionnuala blocked it on the grounds that Edda''s family had compromised loyalty to a rival House in the previous generation. She has watched Bran Morcant accumulate thirty years of unchecked military authority and has begun to wonder whether the compromised loyalty she feared in Edda''s line was real or was manufactured by someone who wanted Edda unavailable to Aldwyn for an entirely different reason.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'The Dowager''s formal apartments; the council anteroom when she chooses to appear; the northern correspondence rooms. She has not left the central Pallor household in four years.',
        N'0', N'0',
        N'Portrait of an elderly medieval noblewoman in her eighties, white hair pinned formally, pale blue eyes, parchment complexion, small and utterly still in dark formal wool; a firelit receiving room; medieval European; intimate candlelight; absolute composure and authority',
        N'An elderly noblewoman in her eighties sits in a firelit receiving room. White hair pinned formally, pale blue eyes, deeply lined parchment skin. Dark formal wool with no unnecessary ornamentation. Perfectly still posture. Complete composure. Medieval European interior, warm firelight.',
        0, 0
    );
END
ELSE PRINT 'Fionnuala Cairn already exists.';
GO

-- 7. Ser Dougal Cairn — Cousin; Commander of the Northern Scrying Installation; Knight-rank
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Dougal Cairn')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Dougal Cairn', N'dougal-cairn', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Dougal Cairn', N'dougal-cairn', N'Dougal', N'Cairn', N'Ser', N'human', N'human',
        N'male', N'he/him', 44, N'alive',
        N'Cousin of Lord Aldwyn Cairn; Commander of the Northern Scrying Installation; Knight-rank',
        N'Dougal Cairn is forty-four years old and has commanded the northern installation for eleven of them, long enough that he has begun to think of the posting as his rather than as a trust the House placed in him. He is sandy-haired, broad, with the ruddy complexion of a man who spends real hours in the northern coastal wind. He underwent Transmutation at thirty-one — survived in the first seventy-two hours, recovered in a week, which is considered fast and was considered a sign. He is not stupid and not treasonous and not quite careful enough about the distinction. He corresponds with a Draught intermediary because he believes he is building a private buffer arrangement that will keep the northern coast safer without requiring Aldwyn''s approval, which he believes would not be forthcoming. He is wrong about several things in this assessment.',
        N'The loyal cousin who has crossed a line he has convinced himself does not exist; the mechanism through which outside intelligence has entered the House without anyone knowing to look for it.',
        N'No POV.',
        N'House Pallor; Anglic-Morvic mixed heritage, northern coast',
        180, 88,
        N'broad and solid; Knight-grade density evident in the way he occupies space; ruddy from northern weather',
        N'sandy brown', N'practical military cut', N'short', N'blue', N'ruddy, weathered', N'coarse-grained',
        N'Subtle height gain, increased density',
        N'Solid and deliberate; occupies space confidently. Tends to stand rather than sit in briefings.',
        N'Northern military wool in Pallor grey and coastal brown. Practical armor for installation work. Dressed for function.',
        N'Knight-rank Transmutation: increased physical density and resilience; modest strength enhancement',
        N'Installation operations review at dawn. Scrying apparatus maintenance and log review through the morning. Afternoons to correspondence he does not record in the installation log. Evening patrol of the northern perimeter he insists on conducting himself.',
        N'Dougal has been passing what he believes is inert data to a Draught intermediary — obsolete Scrying coordinates and decommissioned position logs — in exchange for Draught coastal intelligence he considers valuable. He has never verified that the data he provides is truly inert. One of those logs contained the observation window data for a Sphere 31 transit event that the Spymaster does not know Dougal had access to and has now passed to an enemy intelligence service. Second Captain Fergal Dunne has noticed the anomalous access pattern and is compiling a report he intends to bring to the Spymaster.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'The northern Scrying installation and its perimeter; occasional visits to the Pallor central household, which feels formal to him now. He has not been south of the installation road in two years.',
        N'0', N'0',
        N'Portrait of a medieval knight-commander in his mid-forties, sandy-brown hair, blue eyes, ruddy weathered complexion, broad and solid in northern military wool and armor; a coastal fortification with a brass Scrying apparatus; medieval European steampunk; overcast northern light; capable and somewhat too comfortable expression',
        N'A knight-commander in his mid-forties at a northern coastal fortification. Sandy-brown hair, blue eyes, ruddy weathered skin. Practical military wool and armor. A Scrying apparatus with brass fittings behind him. Overcast northern light. Medieval European with steampunk elements.',
        0, 0
    );
END
ELSE PRINT 'Dougal Cairn already exists.';
GO

-- 8. Dame Isolde Fairbraith — Cousin married into House Thorn; returned; Knight-rank
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Isolde Fairbraith')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Isolde Fairbraith', N'isolde-fairbraith', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Isolde Fairbraith', N'isolde-fairbraith', N'Isolde', N'Fairbraith', N'Dame', N'human', N'human',
        N'female', N'she/her', 39, N'alive',
        N'Cousin of House Pallor by birth; former House Thorn affiliate by marriage; returned following husband''s death; advisor without portfolio; Knight-rank',
        N'Isolde Fairbraith was born a Cairn cousin and left at twenty-four through marriage into House Thorn''s military branch, where she underwent Transmutation at twenty-six and achieved Knight-rank in the Thorn Corps before her husband''s death brought her home three years ago. She kept her married name — partly from habit and partly because Fairbraith is a Thorn-legible name and being legible has uses. She has dark red hair, green eyes, the fair freckled complexion of the northern Anglic line, and she moves through the Pallor household with the careful attention of someone very good at not being the most important person in a room. House Thorn still considers her theirs in some sense. She knows it, Aldwyn''s household knows it, and neither party has yet decided to make that tension explicit.',
        N'The returned exile whose knowledge of another House''s internal structure is either a resource or a liability depending on who she decides to trust; she knows exactly where her loyalties lie, and that clarity is what makes her dangerous.',
        N'No POV.',
        N'House Pallor; Anglic by birth; twelve years affiliated with House Thorn through marriage; returned',
        168, 66,
        N'slight with Knight augmentation density; moves with careful deliberation in unfamiliar rooms',
        N'dark red', N'worn loose or loosely braided', N'long', N'green', N'fair', N'fine, freckled at the cheeks',
        N'Subtle height gain, increased density',
        N'Deliberate and unhurried; rarely volunteers her position in a room. Watches doors.',
        N'Practical dress — neither Pallor formal nor Thorn military. She has not adopted Pallor''s coastal grey and appears to consider this unremarkable. Wears no House colors.',
        N'Knight-rank Transmutation achieved under House Thorn Corps protocols: physical density increase, resilience, modest strength enhancement',
        N'Mornings in the archive reviewing Thorn-adjacent treaties she has authorized access to. Afternoons in informal meetings Aldwyn''s household considers non-critical — which is where the real decisions often begin. She writes letters to Thorn contacts she does not mention to the Chancellor.',
        N'Isolde''s husband did not die of campaign wounds as officially recorded. He was killed on orders she believes trace to the House Thorn Spymaster — a clearance she can almost prove. She returned to Pallor specifically to be beyond that reach. She has not shared what she knows of Thorn''s internal operations because she is still deciding what to trade it for, and because the person she would most logically tell — Oswald Wraith — may already be running an exchange with Thorn that would make her information dangerous to deliver.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'The Pallor central household; the archive; the informal council anteroom. She does not go to the northern installation.',
        N'0', N'0',
        N'Portrait of a medieval noblewoman-knight in her late thirties, dark red hair worn loose, green eyes, fair freckled complexion, slight with Knight density; plain practical dress with no House colors; a Pallor stone corridor; medieval European; cool interior light; watchful and composed expression',
        N'A noblewoman-knight in her late thirties in a stone corridor. Dark red hair worn loose, green eyes, fair freckled skin. Practical dress with no House insignia. Deliberate posture. Watchful expression. Medieval European, cool interior light.',
        0, 0
    );
END
ELSE PRINT 'Isolde Fairbraith already exists.';
GO

-- 9. Lord Beorn Cairn (DECEASED) — Elder brother of Aldwyn; died at Kellmouth
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Beorn Cairn')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Beorn Cairn', N'beorn-cairn', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Beorn Cairn', N'beorn-cairn', N'Beorn', N'Cairn', N'Lord', N'human', N'human',
        N'male', N'he/him', 42, N'dead',
        N'Deceased former Heir of House Pallor; elder brother of Lord Aldwyn Cairn; died at the Battle of Kellmouth Breach',
        N'Beorn Cairn was the first son and original heir, twenty years dead. He died at forty-two commanding the naval defense at the Battle of Kellmouth Breach — the Draught raid that succeeded in landing forces on Pallor soil for the first time in forty years. The official account blames inadequate positioning of the harbor chain. The unofficial account, circulated in Corps barracks for two decades, blames Beorn''s command decisions in the final hours. Aldwyn spent years defending his brother''s reputation and built a substantial portion of his political legitimacy on the argument that Beorn was betrayed by his subordinates, not outcommanded. Beorn was large, dark-haired, with the grey eyes of the Cairn line — a formidable presence by every account of the soldiers who served under him, which makes the question of what happened at Kellmouth no simpler.',
        N'The martyr-figure whose ambiguous legacy is the foundation on which the current Lord built his political identity; the truth of what Beorn actually did at Kellmouth is the crack in that foundation.',
        N'No POV.',
        N'House Pallor; Anglic, Channel coast',
        190, 95,
        N'large, dark-haired; physically formidable by all surviving accounts',
        N'dark brown-black', N'military cut', N'short', N'grey', N'pale', N'strong-featured, weathered',
        N'none',
        N'Deceased. No record.',
        N'Full Pallor military dress. Those who knew him say he rarely wore the ornamentation of his rank.',
        N'none',
        N'Deceased.',
        N'The night before Kellmouth, Beorn wrote a letter found in his effects afterward. In it, he describes a decision made that morning: he ordered the harbor chain cut to allow Pallor merchantmen — including the Kellian fishing fleet that Rowena Bevan''s family ran — to escape before the Draught fleet closed the harbor approach. He called it a lesser sacrifice. He knew it would open the channel. The letter has never been made public. Lord Aldwyn has it, has read it, and has never decided whether releasing it would honor his brother''s memory or destroy it. The veteran soldier Godwin Marsh was on the harbor wall when the chain was cut and knows whose voice gave the order.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Deceased. His range was the northern coast and the Pallor naval command. His portrait hangs in the war council chamber.',
        N'0', N'0',
        N'Portrait of a deceased medieval lord-commander in his early forties, dark brown-black hair military cut, grey eyes, large and formidable in full Pallor armor; a harbor-wall background with a Draught naval assault in the distance; medieval European; stormy dramatic light; the expression of a man who has made a decision he cannot unmake',
        N'A medieval lord-commander in his early forties at a harbor wall. Dark brown-black hair, grey eyes, large and formidable in full armor. Beyond him a harbor with a naval assault beginning. Stormy light. His expression is set and irreversible. Medieval European historical portrait style.',
        0, 0
    );
END
ELSE PRINT 'Beorn Cairn already exists.';
GO

-- ============================================================
-- POLITICAL CABINET
-- ============================================================

-- 10. Mistress Gwenith Aldermoor — Chancellor
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Gwenith Aldermoor')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Gwenith Aldermoor', N'gwenith-aldermoor', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Gwenith Aldermoor', N'gwenith-aldermoor', N'Gwenith', N'Aldermoor', N'Mistress', N'human', N'human',
        N'female', N'she/her', 57, N'alive',
        N'Chancellor of House Pallor; senior political administrator; has served three Lords and one Lady',
        N'Gwenith Aldermoor has been Chancellor of House Pallor for nineteen years, having served in lesser administrative roles for fourteen before that. She is fifty-seven, silver-streaked brown hair worn in a tight working knot, brown eyes, medium build — she looks exactly like someone who has spent thirty-three years managing everything the Lords of Pallor could not be bothered to manage themselves, which is accurate. She has served through the end of Aldwyn''s father''s rule, through the Kellmouth disaster, through Rowena''s death, through the Accord''s collapse. She does not offer opinions she has not been asked for. She is almost never not asked.',
        N'The institutional anchor who knows more about the House''s actual functioning than anyone alive, and whose withheld action — Rowena''s fourth letter — is the most consequential administrative decision she has ever made.',
        N'No POV.',
        N'House Pallor; Kellian, coastal administrative family; several generations of House service',
        166, 72,
        N'medium build; practical economy of movement; never rushes but always arrives exactly when expected',
        N'silver-streaked brown', N'tight working knot', N'medium', N'brown', N'medium, warm', N'fine-lined, composed',
        N'none',
        N'Precise, compact movement. She never crosses a room without a reason. She is the one person in the household who can stop a conversation by entering it.',
        N'Pallor administrative grey and dark blue. Always with a dispatch case or folio. Wears a small wax-seal ring she uses for correspondence and nothing else decorative.',
        N'none',
        N'Before dawn review of overnight dispatches. Dawn briefing with the Lord. The remainder of the day sorting what reaches Aldwyn from what does not, which is most of her actual work. She reads the intelligence reports before Oswald Wraith formally presents them and has done so for twelve years.',
        N'Gwenith holds Lady Rowena''s fourth undelivered letter. She has read it. The letter names Gwenith as the person Rowena trusted most and instructs her to tell Aldwyn the truth about a meeting Rowena had with the Liturgy Liaison six months before she died. The meeting was not a routine visit. Gwenith has not delivered the letter because doing so would require her to explain how Rowena knew she was dying before the fever had progressed to its lethal stage, which in turn would open a question about the Liturgy''s relationship to Rowena''s death that Gwenith does not believe the House can survive answering.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'The Chancellor''s offices; the Lord''s receiving rooms; the diplomatic correspondence archive; the formal council chambers. She is wherever the House''s paperwork is, which is everywhere.',
        N'0', N'0',
        N'Portrait of a medieval chancellor in her late fifties, silver-streaked brown hair in a tight knot, brown eyes, medium build in Pallor administrative grey and dark blue; a dispatch-filled office with maps and correspondence; medieval European; candlelight and daylight both; composed and quietly authoritative expression',
        N'A medieval chancellor in her late fifties at a desk covered in dispatches. Silver-streaked brown hair in a tight knot, brown eyes, medium build. Pallor administrative grey and dark blue. A wax-seal ring on one finger. Composed expression, complete authority. Medieval European, mixed candlelight and daylight.',
        0, 0
    );
END
ELSE PRINT 'Gwenith Aldermoor already exists.';
GO

-- 11. Master Oswald Wraith — Spymaster
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Oswald Wraith')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Oswald Wraith', N'oswald-wraith', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Oswald Wraith', N'oswald-wraith', N'Oswald', N'Wraith', N'Master', N'human', N'human',
        N'male', N'he/him', 51, N'alive',
        N'Spymaster of House Pallor; Director of Intelligence Operations',
        N'Oswald Wraith is fifty-one and has run House Pallor''s intelligence apparatus for sixteen years, which is long enough to have built it twice — once as inherited structure and once as his own. He is dark-haired going grey at the temples, pale grey eyes, olive complexion, and he gives the impression of being slightly less present than he actually is, which he has cultivated carefully. He knows about Sphere 31 operations — knows more than any other person in the House, including Aldwyn. He knows the transit logs, the membrane schedules, the domestic-placement pipeline. He has filed none of this into the formal intelligence record. What he does with what he knows has never been tested, which is precisely the situation he has maintained.',
        N'The intelligence apparatus personified — he knows what everyone else in the House is doing, and the moment anyone discovers what he knows and has sat on, the entire intelligence structure becomes a liability rather than an asset.',
        N'No POV.',
        N'House Pallor; Morvic, interior hill country; recruited into intelligence work at nineteen',
        176, 79,
        N'medium build; gives the impression of being slightly less present than he is',
        N'dark brown going grey at temples', N'neatly kept', N'short', N'pale grey', N'olive', N'fine, unremarkable',
        N'none',
        N'Moves quietly and arrives in rooms before people notice he''s entered. Never the first to speak in a group. Always the last to leave.',
        N'Undistinguished dark wool that reads as staff rather than official. He dresses to be forgotten and has made a study of succeeding at it.',
        N'none',
        N'He does not have a legible daily schedule, which is intentional. His agents report to him at irregular hours. He reads the Chancellor''s dispatch queue before she presents it to Aldwyn — she knows he does this and has allowed it for eleven years without acknowledgment from either party.',
        N'Oswald has intercepted a Liturgy transit manifest that names Saoirse Cairn — Lord Aldwyn''s ward — as scheduled for "membrane evaluation." The Liturgy has never previously evaluated a member of a ruling family''s ward household. He does not know what the evaluation involves. He has told no one and has been watching Eithne Colm, the Liturgy Liaison, for signs of the operation''s shape, because he has decided that understanding what the Liturgy wants with Saoirse is worth more to him than warning Aldwyn before he understands it himself.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Wherever he needs to be, which is never where people expect. He maintains a private office the Lord''s household does not have the location of.',
        N'0', N'0',
        N'Portrait of a medieval intelligence officer in his early fifties, dark brown hair greying at temples, pale grey eyes, olive complexion, medium build in undistinguished dark wool; a dim stone corridor or private office; medieval European; minimal light; expression of calm, total attention',
        N'A medieval intelligence officer in his early fifties in a dim stone corridor. Dark brown hair greying at the temples, pale grey eyes, olive skin. Undistinguished dark wool. An expression of calm, total attention. He looks like a member of the household staff. He is not. Medieval European, minimal light.',
        0, 0
    );
END
ELSE PRINT 'Oswald Wraith already exists.';
GO

-- 12. Master Hereward Denn — House Archivist
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Hereward Denn')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Hereward Denn', N'hereward-denn', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Hereward Denn', N'hereward-denn', N'Hereward', N'Denn', N'Master', N'human', N'human',
        N'male', N'he/him', 65, N'alive',
        N'House Archivist of Pallor; keeper of records, genealogy, treaty texts, and Scrying logs',
        N'Hereward Denn is sixty-five years old and has been House Archivist for twenty-eight of them, a tenure that makes him the longest-serving named official in Pallor''s current administration. He is bald except for a white fringe, brown-eyed, weathered, with the slightly distracted air of someone processing two conversations simultaneously — one present, one in the records he was reviewing this morning. He maintains the official archive and a separate private ledger of what the official archive omits or elides, a practice he began in his first year when he discovered that his predecessor had been quietly removing documents on request. He knows where every body is buried, sometimes literally — he maintains the House''s private interment records alongside the genealogy.',
        N'The keeper of every secret the House has formally committed to paper and several it has not; the repository of a genealogical anomaly that could restructure the succession if it became known.',
        N'No POV.',
        N'House Pallor; Kellian, river-valley archival family; several generations of records work',
        171, 68,
        N'medium, slightly stooped from decades at a writing desk; hands always ink-stained',
        N'white fringe, otherwise bald', N'close fringe', N'very short', N'brown', N'weathered, medium', N'deeply lined',
        N'none',
        N'Slightly distracted presence; rarely fully in a room. Answers questions with better precision than expected. Never forgets anything he has read.',
        N'Archivist''s practical wool in dark brown and grey. Always with ink on his fingers. Wears a document-case at his belt.',
        N'none',
        N'Mornings cataloguing and cross-referencing new documents before they reach the formal archive. Afternoons receiving queries he answers from memory before consulting records. Evenings alone in the private section of the archive, which no one else has keys to.',
        N'Hereward discovered three years ago that the House genealogy as publicly recorded is false. Lord Aldwyn has a surviving elder sibling — a child born before Beorn, given at birth to a Morvic family under a discretion arrangement brokered by Fionnuala''s husband, never acknowledged, and removed from the official record. That sibling is now an adult somewhere in Pallor''s territory. Hereward has done nothing with this knowledge because he cannot determine whether Lord Aldwyn knows, and acting without knowing that is the most dangerous thing he can imagine doing.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'The archive and its adjacent reading rooms. He has not left the central Pallor compound in years and does not appear to notice.',
        N'0', N'0',
        N'Portrait of an elderly medieval archivist in his mid-sixties, bald with a white fringe, brown eyes, weathered complexion, slightly stooped in dark brown archival wool; a vaulted archive room with scroll cases and brass lamp fixtures; medieval European steampunk; warm amber lamplight; distracted and intensely knowing expression',
        N'An elderly archivist in his mid-sixties in a vaulted archive room. Bald with a white fringe, brown eyes, ink-stained hands. Dark brown wool. Scroll cases and brass lamp fixtures around him. Warm amber light. His expression is distracted and profoundly knowing. Medieval European with steampunk lamp elements.',
        0, 0
    );
END
ELSE PRINT 'Hereward Denn already exists.';
GO

-- 13. Mistress Catriona Vell — Trade Ambassador
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Catriona Vell')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Catriona Vell', N'catriona-vell', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Catriona Vell', N'catriona-vell', N'Catriona', N'Vell', N'Mistress', N'human', N'human',
        N'female', N'she/her', 43, N'alive',
        N'Trade Ambassador of House Pallor; manager of inter-House commerce and border market operations',
        N'Catriona Vell is forty-three, auburn-haired, green-eyed, fair-complexioned with the Anglic-Morvic border heritage that gives her equal fluency in both peoples'' commercial customs. She has managed House Pallor''s trade relationships for nine years and considers Oswald Wraith''s intelligence assessments of House Garnet to be optimistic to the point of negligence. She has not said this in formal council because she has no official basis for contradiction — the formal reports support Wraith''s conclusions. She has been quietly building her own basis for three years.',
        N'The one official who has independently arrived at the correct assessment of House Garnet''s intentions and is sitting on proof she obtained through methods that would embarrass the House if disclosed — the problem of being right in the wrong way.',
        N'No POV.',
        N'House Pallor; Anglic-Morvic mixed heritage, border settlements east of the Channel lowlands',
        169, 67,
        N'medium build; moves efficiently through crowded spaces; has the permanently alert posture of a negotiator',
        N'auburn', N'worn down when working, pinned for formal occasions', N'long', N'green', N'fair', N'fine, freckled',
        N'none',
        N'Efficient and direct in movement. Tends to arrive at appointments slightly early and use the time to read the room. Not warm in greeting; pleasant once engaged.',
        N'Pallor trade dress — good quality, moderate formality. She can shift toward either Anglic or Morvic style depending on who she''s meeting, and does so deliberately.',
        N'none',
        N'Morning review of border market reports from factor agents. Formal correspondence and trade negotiation through the day. She maintains a private set of records she has never submitted as official dispatches — her independent intelligence operation on House Garnet''s shipping movements, funded by routing a small fraction of border market receipts through a third-party account she controls.',
        N'Catriona has compiled a detailed dossier showing that House Garnet is deliberately disrupting Pallor''s northern shipping lanes through intermediaries — a finding that contradicts Oswald Wraith''s official assessment, which rates Garnet as a neutral commercial competitor. She obtained this dossier using funds she redirected from border market receipts without authorization. She is not stealing; she is paying for intelligence the Spymaster is either unwilling or unable to surface. She does not know whether Wraith is wrong or lying, and the distinction matters enormously for what she does with what she knows.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'The border market towns; the trade ambassador''s formal offices; the eastern transit roads. She is the most traveled member of the cabinet.',
        N'0', N'0',
        N'Portrait of a medieval trade ambassador in her early forties, auburn hair, green eyes, fair freckled complexion, medium build in good-quality trade dress; a border market or formal negotiation room; medieval European; warm natural light; alert and precise expression',
        N'A medieval trade ambassador in her early forties at a negotiating table. Auburn hair, green eyes, fair freckled skin. Good-quality trade dress in moderate formality. Alert, precise expression. A border market or formal negotiation room behind her. Medieval European, warm natural light.',
        0, 0
    );
END
ELSE PRINT 'Catriona Vell already exists.';
GO

-- 14. Eithne Colm — Liturgy Liaison
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Eithne Colm')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Eithne Colm', N'eithne-colm', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Eithne Colm', N'eithne-colm', N'Eithne', N'Colm', N'', N'human', N'human',
        N'female', N'she/her', 34, N'alive',
        N'Liturgy Liaison to House Pallor; attached representative of the Liturgy institution; reports formally to both the Liturgy and the House',
        N'Eithne Colm is thirty-four, black-haired and dark-eyed, with the olive complexion of Kellian coastal stock and the particular composure of someone inducted into the Liturgy at age nine — she has never not known this as her life, which gives her a serenity that people who meet her for the first time read as coldness. She reports formally to both the Liturgy and to Lord Aldwyn''s chancellor. Neither fully trusts her, and she has made peace with that. She attends formal council as an observer, speaks rarely, and files Liturgy reports on the House''s status that she writes with an eye toward both audiences without fully serving either.',
        N'The divided instrument — the one character in the House with genuine obligations to an outside power, managing an active Liturgy operation within the House that no one has authorized; her serenity is either genuine or the most disciplined performance in the building.',
        N'No POV.',
        N'House Pallor; Kellian coastal family origin; inducted into the Liturgy at age nine; Liturgy-trained',
        164, 60,
        N'slight, very still; the composure of someone who has never not belonged to an institution',
        N'black', N'worn simply, pulled back', N'long', N'dark brown', N'olive', N'clear, fine',
        N'none',
        N'Very still in rooms, almost never the first to move. Speaks in complete sentences. Listens with her whole attention in a way that makes people want to fill the silence.',
        N'Liturgy formal dress: grey and white, simple cut, no House affiliation visible. She is the only person in Pallor''s formal rooms who wears no House colors.',
        N'none',
        N'Morning Liturgy observances she performs in her own quarters. Formal daily report to the Liturgy''s central house by written dispatch. Afternoon attendance in whatever the House considers routine, which is where she does her actual work. She has been watching Saoirse Cairn every afternoon for three months.',
        N'Eithne arranged, on direct Liturgy senior authority instruction, for a person from Sphere 31 to be brought through the membrane and placed in the Pallor household as domestic staff three months ago. She was told only that the person was "expected to become relevant" and was given no further information. She does not know the person''s identity or which of the household''s staff they are — she was instructed to arrange the transit, not to know the result. She has been watching every servant in the household since, trying to identify who it is, and has become convinced that whoever was brought through has already begun to matter in ways she cannot see.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'The Liturgy liaison offices; the formal council chambers; the household common areas where she observes. She is present wherever the House gathers and visible enough to be ignored.',
        N'0', N'0',
        N'Portrait of a medieval Liturgy liaison in her mid-thirties, black hair pulled back, dark brown eyes, olive complexion, slight in Liturgy grey and white formal dress; a formal council chamber or quiet stone corridor; medieval European; cool diffuse light; expression of absolute composure that may or may not be genuine serenity',
        N'A medieval Liturgy liaison in her mid-thirties in a formal council chamber. Black hair pulled back, dark brown eyes, olive skin. Liturgy grey and white, simple cut. Very still. Complete composure. Cool diffuse light. Medieval European.',
        0, 0
    );
END
ELSE PRINT 'Eithne Colm already exists.';
GO

-- 15. Master Wulfric Bassett — Treasurer and Chamberlain
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Wulfric Bassett')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Wulfric Bassett', N'wulfric-bassett', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Wulfric Bassett', N'wulfric-bassett', N'Wulfric', N'Bassett', N'Master', N'human', N'human',
        N'male', N'he/him', 61, N'alive',
        N'Treasurer and Chamberlain of House Pallor; manager of House finances and domestic operations',
        N'Wulfric Bassett is sixty-one years old and has managed House Pallor''s finances for seventeen of them, a tenure during which the books have always balanced at formal review and have never quite balanced in the private ledger he keeps in his head. He is balding, florid, grey-eyed, with the broad build of an Anglic interior merchant family and the perpetual mild distraction of a man running three financial calculations simultaneously. He is good at his job and has been quietly covering a discrepancy in the naval maintenance budget for four years — smoothing it across line items, shifting quarterly allocations to absorb the gap. No formal audit has come close to finding it.',
        N'The administrator whose hidden financial maneuvering makes him the most compromised person in the cabinet; his secret is not theft but silence, which in some ways is worse.',
        N'No POV.',
        N'House Pallor; Anglic, interior merchant family, town commerce background',
        173, 86,
        N'broad, slightly florid; the build of a man who has eaten well and sat much; carries himself with the solidity of someone used to being believed',
        N'brown going grey, balding', N'short fringe', N'very short', N'grey', N'florid, pale', N'heavy-featured',
        N'none',
        N'Solid and deliberate; occupies his chair as though he grew there. Speaks slowly enough that people assume he is being careful, which he is, but not always about what they think.',
        N'Formal chamberlain dress: dark wool, House Pallor blue. Carries a ledger folio at all times. Always seated at meetings if a chair is available.',
        N'none',
        N'Dawn review of overnight financial reports. Morning management of the domestic household budget alongside the naval allocation review he has been quietly restructuring for four years. Afternoon formal treasury presentations to the Lord that he has rehearsed until they are unreviewable. He does not sleep as well as he used to.',
        N'The naval maintenance shortfall Wulfric has been covering exists because four years ago he quietly paid a blackmail demand from a Draught intermediary who had evidence of his presence when Beorn Cairn''s final letter was first discovered and read aloud in the effects room. Wulfric was there by accident — a junior treasury official at the time, attending to an unrelated matter. He heard the letter''s content. The blackmailer knew he heard it. He paid in silence, reallocated the funds from the naval maintenance budget, and has been covering the gap ever since. He has told no one, including the Chancellor, that the letter describes Beorn''s deliberate order to cut the harbor chain.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'The treasury offices; the domestic management suites; the formal council chambers. He has no reason to leave the central compound and does not.',
        N'0', N'0',
        N'Portrait of a medieval treasurer in his early sixties, balding with a grey fringe, grey eyes, florid complexion, broad in formal chamberlain dark wool and House blue; a treasury office with ledger stacks; medieval European; warm interior light; composed and carefully unrevealing expression',
        N'A medieval treasurer in his early sixties in a treasury office. Balding with a grey fringe, grey eyes, florid skin. Dark formal wool in House blue. A ledger folio on the desk. Stacks of records around him. Warm interior light. Composed expression, carefully unrevealing. Medieval European.',
        0, 0
    );
END
ELSE PRINT 'Wulfric Bassett already exists.';
GO

-- 16. Mistress Rhiannon Cawl — Diplomat at House Garnet; currently abroad
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Rhiannon Cawl')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Rhiannon Cawl', N'rhiannon-cawl', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Rhiannon Cawl', N'rhiannon-cawl', N'Rhiannon', N'Cawl', N'Mistress', N'human', N'human',
        N'female', N'she/her', 38, N'alive',
        N'House Pallor diplomat assigned resident to House Garnet; currently abroad; her reports have not been acknowledged in three months',
        N'Rhiannon Cawl is thirty-eight and has been House Pallor''s resident diplomat at House Garnet for five years — long enough to know the difference between a House that is diplomatically indifferent to Pallor''s interests and one that is actively working against them. She is dark brown-haired, blue-eyed, fair, with the Kellian coastal look she has used carefully in Garnet''s court to position herself as less significant than she is. Three months ago she dispatched a formal intelligence report detailing House Garnet''s Scrying operations on Sphere 31 transit windows that correspond specifically with Pallor territorial shipping lanes. The report has not been acknowledged. She does not know if it was received.',
        N'The intelligence asset in the field who has discovered the right thing and cannot get anyone at home to confirm they heard her — her isolation is the vulnerability the House doesn''t know it has.',
        N'No POV.',
        N'House Pallor; Kellian coastal; several years in Garnet''s formal diplomatic halls',
        167, 63,
        N'medium, careful; carries herself at House Garnet as slightly less significant than she is',
        N'dark brown', N'worn simply for court', N'medium', N'blue', N'fair', N'clear, fine',
        N'none',
        N'Moves through Garnet''s court with studied social economy — visible enough, memorable for nothing specific. Listens before speaking in every room.',
        N'Garnet-appropriate court dress with Pallor formal insignia she is required to wear and which she has noted makes her precisely as visible as she needs to be, no more.',
        N'none',
        N'Resident diplomacy: formal court appearances at Garnet, commercial liaison, treaty maintenance. Private: intelligence observation and the increasingly urgent dispatch reports she sends to Pallor through the official courier chain. She has written twelve dispatches in three months and received formal acknowledgment of two of them.',
        N'Rhiannon has recently learned that her handler at House Pallor — the contact point who receives her dispatches before they reach the Chancellor — is one of Oswald Wraith''s secondary agents, and that her reports have been filtered before reaching Aldwyn. She does not know what was removed from the reports that did arrive, or why the intelligence report on Garnet''s Sphere 31 operations has been held without acknowledgment for three months. She is now considering bypassing the dispatch chain entirely and writing directly to the Chancellor — which would expose the parallel intelligence filter Wraith has built into the diplomatic correspondence system.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Garnet''s formal diplomatic halls and court; resident diplomat''s assigned quarters at Garnet. She has not been home to Pallor in fourteen months.',
        N'0', N'0',
        N'Portrait of a medieval diplomat in her late thirties, dark brown hair, blue eyes, fair complexion, medium build in Garnet court dress with Pallor formal insignia; a foreign court hall; medieval European; formal candlelit interior; expression of contained urgency behind practiced diplomacy',
        N'A medieval diplomat in her late thirties in a foreign court hall. Dark brown hair, blue eyes, fair skin. Garnet court dress with Pallor formal insignia at her collar. Practiced diplomatic composure with contained urgency underneath. Formal candlelit medieval interior.',
        0, 0
    );
END
ELSE PRINT 'Rhiannon Cawl already exists.';
GO

-- ============================================================
-- MILITARY COMMAND
-- ============================================================

-- 17. Bran Morcant — Commander of the Myrmidon Corps; Paladin-rank
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Bran Morcant')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Bran Morcant', N'bran-morcant', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Bran Morcant', N'bran-morcant', N'Bran', N'Morcant', N'', N'human', N'human',
        N'male', N'he/him', 55, N'alive',
        N'Commander of the Myrmidon Corps of House Pallor; Paladin-rank; has the Lord''s full confidence',
        N'Bran Morcant is fifty-five years old and has commanded the Myrmidon Corps for fourteen of them. He is enormous — Paladin augmentation has taken him to two hundred and five centimeters and restructured his proportions into something no tailor has ever comfortably accommodated, his eyes now the amber-gold that Paladin infusion imposes on the iris, which never quite reads as human in low light. He carries the Lord''s full confidence and knows it, which has always been true and has been actively dangerous for approximately the last seven years. He is professionally brilliant, personally fair to those who serve under him, and accumulating authority at a rate that the Dowager Fionnuala has noticed and no one else in Aldwyn''s cabinet has been willing to name.',
        N'The military commander who has crossed from trusted general to shadow authority without anyone formally acknowledging the transition — the House''s most capable person in its most dangerous position.',
        N'No POV.',
        N'House Pallor; Anglic, northern military family; Corps from age eighteen',
        205, 115,
        N'enormous; Paladin augmentation has restructured his proportions entirely; he occupies space in a way that reorganizes rooms around him',
        N'iron-grey', N'close military cut', N'short', N'amber-gold (Paladin augmentation)', N'pale', N'weathered, strong-featured',
        N'Evident enhancement — significant height, altered proportions, changed eyes',
        N'Moves with the deliberate economy of someone accustomed to being the largest person in any room and to the attention that brings. Never gestures unnecessarily.',
        N'Pallor military command dress scaled for his augmented frame. Full armor on campaign; formal uniform at council. The amber eyes draw attention he does not seek and has stopped trying to deflect.',
        N'Paladin-rank Transmutation: pronounced physical enhancement — significant height increase, restructured skeletal and muscle density, altered eye color to amber-gold; operating at post-enhanced capacity across all physical dimensions',
        N'Dawn to the Corps training yards. Morning command briefings. Afternoons in operational planning or, increasingly, in direct advisory sessions with Lord Aldwyn that run longer than the Chancellor schedules for them. He reads more than his officers know.',
        N'Bran''s wife Edda was the woman Lord Aldwyn wanted to marry before the Dowager Fionnuala blocked it. Bran knows this. He married Edda three years after Aldwyn married Rowena. He has spent thirty years measuring himself against Aldwyn as commander, husband, and man, and winning by most objective counts. He has never been certain whether his loyalty to the House is genuine service or a very sophisticated form of personal competition, and the question has become more pressing as the gap between his power and Aldwyn''s narrows.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'The Corps command headquarters; the training yards; the war table; field installations on inspection. He has the Lord''s permission to range wherever the Corps operates, which is most of Pallor.',
        N'0', N'0',
        N'Portrait of an enormous medieval paladin-commander in his mid-fifties, iron-grey close-cut hair, amber-gold eyes from augmentation, pale weathered complexion, massively built in full Pallor military command armor; a command chamber or training ground; medieval European steampunk; strong directional light; formidable and completely composed',
        N'A medieval paladin-commander in his mid-fifties, enormously built from Paladin augmentation. Iron-grey close-cut hair, amber-gold eyes. Full Pallor military command armor. A command chamber behind him. Strong directional light. Formidable and completely composed. Medieval European with steampunk brass elements.',
        0, 0
    );
END
ELSE PRINT 'Bran Morcant already exists.';
GO

-- 18. Dame Deirdre Harrow — First Captain; ground operations; Knight-rank
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Deirdre Harrow')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Deirdre Harrow', N'deirdre-harrow', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Deirdre Harrow', N'deirdre-harrow', N'Deirdre', N'Harrow', N'Dame', N'human', N'human',
        N'female', N'she/her', 42, N'alive',
        N'First Captain of the Myrmidon Corps; ground operations commander; Knight-rank',
        N'Deirdre Harrow is forty-two, black-haired and dark-eyed, Morvic-born from the coastal fishing communities that have fed the Corps with soldiers for three generations. She underwent Transmutation at twenty-five, survived on the first infusion with no complications, and has spent seventeen years earning every step of her command through ground operations rather than the naval Scrying work that earns the Corps its formal prestige. She and Commander Bran Morcant respect each other professionally and disagree about almost everything operational. She believes the Corps has been progressively hollowed of land capability to feed the Scrying apparatus program, and that the day the channel is breached again — which she considers when, not if — the House will discover its celebrated naval intelligence operation cannot hold a beach.',
        N'The fault line in the Corps command structure — the ground voice in a naval-prestige institution, whose disagreement with the Commander is kept professional and whose knowledge of the Practitioner''s infusion anomaly makes her the most dangerous witness who has so far chosen silence.',
        N'No POV.',
        N'House Pallor; Morvic, coastal fishing community; Corps from age twenty',
        174, 72,
        N'compact and direct; Knight augmentation adds density rather than height; moves with the efficiency of someone who has fought in confined spaces',
        N'black', N'cropped short for function', N'short', N'dark brown', N'medium brown', N'weathered, strong',
        N'Subtle height gain, increased density',
        N'Direct and economical in movement. Makes decisions quickly and announces them flatly. Does not perform consideration she is not engaged in.',
        N'Field armor over Pallor Corps grey. She wears nothing ceremonial that can be avoided. Her Knight''s seal is on her armor''s pauldron.',
        N'Knight-rank Transmutation: increased physical density and resilience; heightened impact resistance; modest strength gain',
        N'Pre-dawn in the ground-operations training yard running drills she designs herself. Morning command briefings with garrison officers. Afternoons on the fortification walls she considers Pallor''s actual first line of defense. She files a formal land-capability assessment to Commander Morcant quarterly that he formally receives and does not formally respond to.',
        N'Deirdre was present as a junior escort officer at the infusion session fifteen years ago in which the Corps Practitioner administered a diluted dose to Morwenna Cairn. She did not know it at the time. She pieced it together later when she accessed the Practitioner''s records during a casualty review and found a dosage notation inconsistent with Morwenna''s recorded augmentation outcome. She has said nothing because she cannot determine from that record alone whether the Practitioner acted on orders or independently, and bringing the evidence before the wrong person first would bury it permanently.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'The ground-operations training yards; the coastal fortification walls; the Corps command chamber. She does not attend Scrying apparatus briefings unless ordered to.',
        N'0', N'0',
        N'Portrait of a medieval knight-captain in her early forties, black hair cropped short, dark eyes, medium brown complexion, compact in field armor over Corps grey; a coastal fortification wall; medieval European; overcast sea light; expression of complete professional composure containing a strongly held opinion',
        N'A medieval knight-captain in her early forties on a coastal fortification wall. Black hair cropped short, dark brown eyes, medium brown skin. Field armor over grey Corps wool. Composed expression with a strongly held opinion inside it. Overcast sea light. Medieval European.',
        0, 0
    );
END
ELSE PRINT 'Deirdre Harrow already exists.';
GO

-- 19. Ser Fergal Dunne — Second Captain; garrison defense; Knight-rank
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Fergal Dunne')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Fergal Dunne', N'fergal-dunne', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Fergal Dunne', N'fergal-dunne', N'Fergal', N'Dunne', N'Ser', N'human', N'human',
        N'male', N'he/him', 48, N'alive',
        N'Second Captain of the Myrmidon Corps; garrison and installation defense commander; Knight-rank',
        N'Fergal Dunne is forty-eight, auburn-haired going grey, blue-eyed, ruddy with long service — the one who stays when others march, which is an institutional role he arrived at partly by temperament and partly because his gift is for systems and logistics rather than field command. He is the one who knows where the arrows are stored, which wall sections are two seasons from needing replacement, and which garrison officers will still be functional at hour thirty of a siege. Both Commander and First Captain consider him unambiguously reliable, which is accurate, and do not watch him closely, which has allowed him to notice something neither of them has.',
        N'The institutional backbone whose careful competence makes him the most likely person to catch an internal threat — and whose report, when it reaches the wrong desk, will be the mechanism that exposes Dougal Cairn.',
        N'No POV.',
        N'House Pallor; Kellian, agricultural interior; Corps from age twenty-two',
        178, 84,
        N'solid and patient; Knight augmentation adds density to an already substantial frame; moves as if he has measured the distance before walking it',
        N'auburn going grey', N'practical military cut', N'short', N'blue', N'ruddy', N'coarse-grained, weathered',
        N'Subtle height gain, increased density',
        N'Patient and methodical. Stands near walls in rooms. Arrives at every meeting knowing the exits.',
        N'Garrison-duty wool and armor, functional and well-maintained. His Corps seal is on his gauntlet.',
        N'Knight-rank Transmutation: increased physical density and resilience; enhanced endurance',
        N'Dawn inspection of garrison positions and wall sections. Morning logistics review and supply chain assessment. Afternoons reviewing installation Scrying logs as part of his standard security audit — where he has noticed an anomalous access pattern troubling him for six weeks. Evening reports to Commander Morcant on garrison status.',
        N'Fergal has discovered that someone has been accessing the northern installation''s Scrying logs in a pattern inconsistent with routine review — viewing specific windows cross-referenced against transit schedules in a way that suggests external intelligence use. He has compiled a formal report and intends to bring it to Spymaster Oswald Wraith. He does not know that the person responsible is Dougal Cairn, and he does not know that Wraith is already tracking Cerdic Cairn''s Draught correspondence and is calculating whether a second Cairn family breach changes when he should act.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'The garrison positions; the installation perimeter roads; the Corps logistics offices. He has been to the central Pallor household three times in five years.',
        N'0', N'0',
        N'Portrait of a medieval garrison captain in his late forties, auburn hair going grey, blue eyes, ruddy weathered complexion, solid in garrison wool and armor; a fortification wall; medieval European; grey overcast light; patient and observant expression',
        N'A medieval garrison captain in his late forties at a fortification wall. Auburn hair going grey, blue eyes, ruddy weathered skin. Solid garrison wool and armor. Patient, observant expression. Grey overcast light. Medieval European.',
        0, 0
    );
END
ELSE PRINT 'Fergal Dunne already exists.';
GO

-- 20. Mistress Brynhild Crane — Infirmary Commander
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Brynhild Crane')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Brynhild Crane', N'brynhild-crane', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Brynhild Crane', N'brynhild-crane', N'Brynhild', N'Crane', N'Mistress', N'human', N'human',
        N'female', N'she/her', 44, N'alive',
        N'Infirmary Commander of the Myrmidon Corps; field hospital administrator; has kept soldiers alive through things they should not have survived',
        N'Brynhild Crane is forty-four, blonde going grey, blue-eyed, with the broad fair complexion of Morvic-Anglic border heritage. She is not Transmutation-enhanced — she chose the infirmary over the Corps line at twenty-two and has never reconsidered, on the grounds that treating the enhanced requires a different objectivity than becoming one. She has the slightly distant quality of someone always running a patient assessment, and she has never told a soldier what she actually thinks their odds are when the odds are bad. She saves more of them than the numbers warrant. She does not discuss how. She has kept a private ledger for sixteen years.',
        N'The record-keeper of what Transmutation actually costs — she holds the only honest account of the Practitioner''s dosage pattern, and her decision about what to do with it will determine whether the selective infusion practice is exposed or continues indefinitely.',
        N'No POV.',
        N'House Pallor; Morvic-Anglic mixed heritage, border community; infirmary-trained from age eighteen',
        170, 71,
        N'broad-shouldered and deliberate; carries no augmentation; her physical authority comes entirely from how she moves',
        N'blonde going grey', N'braided back for work', N'long', N'blue', N'fair, ruddy from field exposure', N'weathered, open',
        N'none',
        N'Deliberate and economical. Touches things before she examines them — surfaces, tools, patients. Never startles.',
        N'Infirmary grey with Corps insignia. She wears no augmentation markers because she has none, and has never dressed to suggest otherwise.',
        N'none',
        N'Pre-dawn infirmary rounds. Morning case reviews and supply audits. The field hospital readiness assessment she files monthly that no one has ever challenged. Afternoons treating non-critical cases. Evenings alone in the private archive room with the ledger.',
        N'Brynhild has maintained a private ledger for sixteen years documenting every infusion death she has witnessed or reviewed — the administering Practitioner, infusion strength, candidate background, and outcome anomalies. The ledger would prove that Corps Practitioner Niamh Sorrall has been selectively administering diluted doses to politically connected or strategically valuable candidates. Brynhild has not reported it because she cannot determine whether Niamh acts on her own authority or on orders from above, and bringing it to the wrong person first would bury it permanently. She has recently noted that Dame Morwenna Cairn''s infusion record matches the diluted-dose pattern and that Morwenna has begun asking questions about dosage protocols.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'The infirmary complex; the field hospital stations along the coastal fortification line; the private archive room where she keeps the ledger.',
        N'0', N'0',
        N'Portrait of a medieval infirmary commander in her mid-forties, blonde hair going grey braided back, blue eyes, broad fair weathered complexion, in infirmary grey with Corps insignia; an infirmary ward with field equipment and brass lamp fittings; medieval European steampunk; warm clinical light; calm and completely observant expression',
        N'A medieval infirmary commander in her mid-forties in an infirmary ward. Blonde hair going grey braided back, blue eyes, broad fair weathered skin. Infirmary grey, Corps insignia. Warm clinical light. Examining something with complete attention. Medieval European with steampunk medical fittings.',
        0, 0
    );
END
ELSE PRINT 'Brynhild Crane already exists.';
GO

-- 21. Alasdair Cope — Senior Sergeant; 27 years service; institutional memory
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Alasdair Cope')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Alasdair Cope', N'alasdair-cope', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Alasdair Cope', N'alasdair-cope', N'Alasdair', N'Cope', N'', N'human', N'human',
        N'male', N'he/him', 52, N'alive',
        N'Senior Sergeant of the Myrmidon Corps; twenty-seven years service; institutional memory of the Corps',
        N'Alasdair Cope is fifty-two years old and has served the Corps for twenty-seven of them without undergoing Transmutation — not from failure, but from choice, having watched three close friends die on the Practitioner''s table in his second year and declined to present himself since. He is salt-and-pepper haired, brown-eyed, weathered-medium, broad from decades of physical service. He is the person junior officers go to when they want to know what actually happened in an incident versus what the official record says, and he is the person senior officers visit when they need to know what the junior officers are actually doing. He knows every officer''s real record versus official one and has never volunteered the gap between them unprompted.',
        N'The institutional conscience who carries the one piece of information that would dismantle the Corps'' most decorated rising officer — and who has honored a dead man''s wish to stay silent, a choice that becomes less sustainable the higher Tibalt Fenn rises.',
        N'No POV.',
        N'House Pallor; Anglic-Kellian, working waterfront community; Corps from age twenty-five',
        182, 87,
        N'broad and weathered; the build of sustained physical service; non-augmented but carries himself with the ease of someone who has never needed the advantage',
        N'salt-and-pepper', N'close military', N'short', N'brown', N'weathered medium', N'deeply lined, open',
        N'none',
        N'Moves through the Corps as if the corridors belong to him, which in the sense that matters they do. Never reports to a room he has not already assessed.',
        N'Standard Corps wool and armor, worn in a way that communicates decades rather than rank. His Sergeant''s stripes are the only distinction he wears.',
        N'none',
        N'Pre-dawn with the Corps before officers arrive — this is where he does most of his actual work. Days split between supervising junior soldiers and existing as the person both officers and soldiers route around formal channels to reach. Evenings with the handful of veterans worth talking to.',
        N'Alasdair knows that Ser Tibalt Fenn''s decorated reputation rests on an action Tibalt did not perform. During the operation in which Fenn was decorated, Tibalt was unconscious from fever. A Morvic soldier named Cray performed the breach that shattered the Draught position — and that Fenn was credited with because Cray died in the same action, leaving Alasdair as the only conscious witness. Before Cray died he asked Alasdair to stay quiet; he had his reasons, and Alasdair has honored them for two years. He has watched Fenn advance on borrowed credit and has not spoken. He is not certain he can keep not speaking indefinitely.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'The Corps training yards; the garrison corridors; the veteran''s common room he uses as an informal intelligence post. He has not left the Corps compound in months.',
        N'0', N'0',
        N'Portrait of a medieval senior sergeant in his early fifties, salt-and-pepper hair, brown eyes, weathered medium complexion, broad in worn Corps wool and armor with sergeant stripes; a Corps training yard or garrison corridor; medieval European; practical natural light; complete situational awareness held very quietly',
        N'A medieval senior sergeant in his early fifties in a Corps training yard. Salt-and-pepper hair, brown eyes, weathered medium skin. Worn Corps wool and armor, sergeant stripes. Complete situational awareness held very quietly. Practical natural light. Medieval European.',
        0, 0
    );
END
ELSE PRINT 'Alasdair Cope already exists.';
GO

-- 22. Godwin Marsh — Veteran soldier near retirement
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Godwin Marsh')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Godwin Marsh', N'godwin-marsh', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Godwin Marsh', N'godwin-marsh', N'Godwin', N'Marsh', N'', N'human', N'human',
        N'male', N'he/him', 49, N'alive',
        N'Veteran soldier of the Myrmidon Corps; twenty-four years service; approaching retirement',
        N'Godwin Marsh is forty-nine years old and has served the Corps for twenty-four of them without particular distinction and without particular failure — the service record that reads as reliable rather than remarkable, which is accurate. He is brown-haired going grey, hazel-eyed, weathered-pale, with a slight persistent tremor in the right hand from a wound in the third Draught channel response that the infirmary closed but never fully healed. He intends to retire to the northern harbor towns and run fishing charters. He has been intending this for three years. He has not left. He tells himself the timing is not right and does not examine the reason too closely.',
        N'The living witness to Beorn Cairn''s harbor chain order — the one person outside the ruling family who knows what actually happened at Kellmouth, and who has been carrying that knowledge for twenty years while considering whether to write it down before he dies.',
        N'No POV.',
        N'House Pallor; Anglic, northern harbor town; Corps from age twenty-five',
        179, 83,
        N'solid service build gone slightly to rest; the slight tremor in the right hand most people take for age',
        N'brown going grey', N'unstyled military cut', N'short', N'hazel', N'weathered pale', N'deeply lined',
        N'none',
        N'Deliberate and careful with the right hand. Stands when he could sit. Has the habit of checking exits that twenty-four years of Corps conditioning has made involuntary.',
        N'Standard Corps garrison wool, no longer kept to inspection standard. He stopped polishing the details two years ago.',
        N'none',
        N'Light garrison duties. Mornings in the armory doing inventory work he has volunteered for because it is quiet. Afternoons near the harbor wall watching the channel. He has begun drafting something in a private journal three times and destroyed it each time.',
        N'Godwin was on the harbor wall at Kellmouth when the harbor chain was cut. He saw the officer who gave the order clearly and at close range. It was not a Draught saboteur, as the official record states. It was Lord Beorn Cairn, who gave the order directly and clearly and then turned back to the harbor approach as though the decision was finished. Godwin has told no one in twenty years. He plans to write it down before he retires and die having done one honest thing. He has been planning this for three years and has not done it.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'The garrison armory; the harbor wall; the veteran''s barracks. He does not range far and has stopped wanting to.',
        N'0', N'0',
        N'Portrait of a veteran medieval soldier in his late forties, brown hair going grey, hazel eyes, weathered pale complexion, slightly built-down in garrison wool; a harbor wall overlooking the channel; medieval European; grey sea light; expression of a man who has been carrying something heavy for a long time',
        N'A veteran medieval soldier in his late forties on a harbor wall above the channel. Brown hair going grey, hazel eyes, weathered pale skin. Garrison wool, no longer to standard. His expression is that of a man who has been carrying something heavy for a long time. Grey sea light. Medieval European.',
        0, 0
    );
END
ELSE PRINT 'Godwin Marsh already exists.';
GO

-- 23. Ser Tibalt Fenn — Junior officer recently distinguished; Knight-rank
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Tibalt Fenn')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Tibalt Fenn', N'tibalt-fenn', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Tibalt Fenn', N'tibalt-fenn', N'Tibalt', N'Fenn', N'Ser', N'human', N'human',
        N'male', N'he/him', 26, N'alive',
        N'Junior officer of the Myrmidon Corps; recently distinguished; watched by both the Commander and the Spymaster; Knight-rank',
        N'Tibalt Fenn is twenty-six years old and has been decorated for a tactical breach action that broke a Draught position two years ago and made his career in a single afternoon. He is light brown-haired, grey-eyed, fair, with the modest Knight augmentation that has put him marginally taller and denser than he was at twenty-three when he took infusion. He received the decoration with convincing composure and has performed his reputation flawlessly since. Commander Morcant considers him a candidate for accelerated command. Spymaster Wraith has flagged him as a rising officer worth cultivating. Neither of them knows what Alasdair Cope knows.',
        N'The decorated officer whose reputation belongs to a dead man — not precisely a fraud because he did not arrange it, but unable to return what was given without destroying everything he has become.',
        N'No POV.',
        N'House Pallor; Kellian coastal; Corps from age twenty',
        177, 78,
        N'slight Knight augmentation; carries his enhanced frame with the care of someone still learning to trust it',
        N'light brown', N'military neat', N'short', N'grey', N'fair', N'clear, fine',
        N'Subtle height gain, increased density',
        N'Controlled and careful in formal settings. Moves with the deliberation of someone performing competence rather than exercising it.',
        N'Full Corps dress, kept precisely to standard. He wears the decoration he was given and has not examined whether it belongs to him.',
        N'Knight-rank Transmutation: modest height increase, increased density and resilience',
        N'Morning drills. Command briefings where he contributes carefully and accurately. Afternoons training junior soldiers he outranks but is learning not to condescend to. He has been thinking about the action at the Draught position for two years and has still not found anyone present who can tell him what actually happened in the breach.',
        N'Tibalt knows his reputation rests on an action he did not perform. He was unconscious from fever during the breach and woke to find the position taken and himself credited. He has never spoken to anyone who was present and conscious. He does not know that Alasdair Cope is the only living witness, and he does not know that Cope has honored a dead soldier''s request to stay silent — a request Tibalt cannot petition because he does not know to whom the credit actually belongs.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'The Corps training yards; command briefings; junior officers'' quarters. He has been posted to field observation twice in the last year and acquitted himself adequately, which he takes as evidence that the debt may eventually be dischargeable through future action.',
        N'0', N'0',
        N'Portrait of a young medieval knight-officer in his mid-twenties, light brown hair, grey eyes, fair complexion, slight Knight augmentation, in full Corps dress with a decoration at his collar; a Corps command chamber or training yard; medieval European; clear daylight; composed expression with something unresolved underneath',
        N'A young medieval knight-officer in his mid-twenties in Corps dress. Light brown hair, grey eyes, fair skin. Full dress with a decoration at his collar. Composed expression, something unresolved underneath. Corps training yard. Medieval European, clear daylight.',
        0, 0
    );
END
ELSE PRINT 'Tibalt Fenn already exists.';
GO

-- 24. Mistress Niamh Sorrall — Corps Transmutation Practitioner
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Niamh Sorrall')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Niamh Sorrall', N'niamh-sorrall', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Niamh Sorrall', N'niamh-sorrall', N'Niamh', N'Sorrall', N'Mistress', N'human', N'human',
        N'female', N'she/her', 46, N'alive',
        N'Corps Transmutation Practitioner of House Pallor; administers Catalyst infusions before campaigns; certified by the Transmutation authority; has given last rites to candidates who died on her table',
        N'Niamh Sorrall is forty-six, dark-red haired, amber-eyed, with the olive-fair complexion of Kellian inland stock. She has administered infusions for nineteen years and has presided over eighty-three deaths — she knows the exact number because she keeps count in a way she has never explained to anyone who asked. She is calm in the infirmary with an absolute calm that soldiers who have seen her at a deathbed describe as the most comforting thing they have witnessed and that those who know her well describe as something else entirely. She was not ordered to administer diluted doses to politically connected candidates. She decided to do this herself. She tells herself she is saving lives. She has made thirty-one further exceptions since the first.',
        N'The practitioner whose unilateral compassion has corrupted the Corps'' augmentation record — when the Infirmary Commander''s ledger surfaces, every secret in the Corps collapses onto her.',
        N'No POV.',
        N'House Pallor; Kellian, inland medical family; Practitioner certified at age twenty-seven',
        165, 62,
        N'slight and precise; moves with the deliberation of someone who has learned that haste at the wrong moment costs a life',
        N'dark red', N'worn back and pinned for work', N'long', N'amber', N'olive-fair', N'clear, carefully composed',
        N'none',
        N'Precise and unhurried. Speaks at a pace calibrated to produce calm in whoever she is speaking to. Never moves quickly in the infirmary.',
        N'Practitioner''s formal grey with Transmutation authority insignia. The amber eyes are her own, not augmentation — she has never taken infusion and has never explained why.',
        N'none',
        N'Pre-campaign: infusion preparation and candidate review. Between campaigns: infirmary consultation, Catalyst supply management, the case review files she maintains separately from the official infusion record. She spends an hour each evening with the private infusion ledger that is distinct from Brynhild Crane''s and does not overlap with it in a way either of them is aware of.',
        N'Niamh has been selectively administering diluted Catalyst doses to candidates she judges politically valuable or connected to the ruling family — reducing their mortality risk while also reducing the potency of their augmentation outcome. She began fourteen years ago with a ruling-family candidate she could not bear to watch die, told herself it was a single exception, and has made thirty-one further exceptions since. She does not know that Brynhild Crane has reconstructed the pattern from official infirmary records, and she does not know that Dame Morwenna Cairn has begun asking questions about dosage protocols that suggest Morwenna has found an edge of the truth.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'The Practitioner''s preparation rooms; the infusion chamber; the Catalyst storage vault she is the sole keyholder for. She does not leave the infirmary complex during campaign preparation periods.',
        N'0', N'0',
        N'Portrait of a medieval Transmutation Practitioner in her mid-forties, dark red hair pinned back, amber eyes, olive-fair complexion, slight and precise in Practitioner grey with Transmutation authority insignia; an infusion chamber with Catalyst apparatus and brass fittings; medieval European steampunk; clinical warm light; absolute composed calm that costs something to maintain',
        N'A medieval Transmutation Practitioner in her mid-forties in an infusion chamber. Dark red hair pinned back, amber eyes, olive-fair skin. Practitioner grey with authority insignia. Catalyst apparatus and brass fittings around her. Clinical warm light. Absolute composed calm. Medieval European steampunk.',
        0, 0
    );
END
ELSE PRINT 'Niamh Sorrall already exists.';
GO

-- ============================================================
-- CONSORT (informal political figure, story-generative)
-- ============================================================

-- 25. Edda Morcant — Consort of Commander Bran Morcant; the woman Aldwyn wanted to marry
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Edda Morcant')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Edda Morcant', N'edda-morcant', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Edda Morcant', N'edda-morcant', N'Edda', N'Morcant', N'Mistress', N'human', N'human',
        N'female', N'she/her', 54, N'alive',
        N'Consort of Commander Bran Morcant; informal political figure in House Pallor; the woman Lord Aldwyn Cairn sought to marry before the Dowager intervened',
        N'Edda Morcant is fifty-four years old and has been the Commander''s consort for thirty-one of them — an institution in House Pallor as solid and unremarked as the harbor wall. She was steel-blonde in her youth, now fully silver, with the Nordic blue eyes and fair complexion of the northern Anglic military families that have intermarried with the Morcant line for three generations. She holds no official position in the household but has more actual influence over the Corps officers'' social and domestic lives than any formal appointment could grant. She is warm, quiet, and very good at being underestimated, which she has found useful for thirty-one years.',
        N'The silent third point in the triangle that has defined the Lord''s political and personal life for three decades; her unasked question about why the Dowager blocked her marriage to Aldwyn is the thread that, if pulled, would restructure everything the House understands about its own history.',
        N'No POV.',
        N'House Pallor; Anglic, northern military family; Morcant household for thirty-one years',
        170, 68,
        N'medium build, poised; no augmentation; carries herself with the ease of someone who has never needed official authority to exercise real influence',
        N'silver (was steel-blonde)', N'worn simply', N'medium', N'blue', N'fair, Nordic', N'fine-lined, warm',
        N'none',
        N'Warm and composed. Occupies whatever space she is in without claiming it. Listens with more attention than people realize she is paying.',
        N'Northern Anglic formal dress, quality materials, understated. She wears no Corps insignia and no House authority markers. She does not need them.',
        N'none',
        N'The Commander''s household in the Corps complex. Morning correspondence with officers'' families she considers part of her informal brief. Afternoons with the small circle of senior Corps spouses she has cultivated for thirty years. She reads more than anyone in the household knows.',
        N'Edda knows that Lord Aldwyn wanted to marry her. The Dowager Fionnuala blocked it when Edda was twenty-three. Edda has never been told why. She has spent thirty-one years performing loyalty to Bran with complete sincerity and found, genuinely, that the performance became real somewhere in the second decade. What she cannot stop thinking about is whether the disqualifying thing Fionnuala found in her family''s history was real — and if it was, whether the same thing applies to her children. She has been on the edge of asking Fionnuala directly for three years. She has not done it because she suspects Fionnuala is waiting for Edda to ask in order to decide what to do with the answer.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'The Commander''s household in the Corps complex; the senior officers'' social spaces; the formal Pallor household when accompanying Bran to council. She is present everywhere she chooses to be and invisible in all of it.',
        N'0', N'0',
        N'Portrait of a medieval commander''s consort in her mid-fifties, silver hair worn simply, blue eyes, fair Nordic complexion, medium build in quality understated northern Anglic dress; a warm interior room in the Corps complex; medieval European; warm domestic firelight; genuine warmth and deep private attention',
        N'A medieval commander''s consort in her mid-fifties in a warm interior room. Silver hair, blue eyes, fair Nordic skin. Quality understated dress, no insignia. Warm domestic firelight. Genuinely warm expression, paying very close attention to something just off frame. Medieval European.',
        0, 0
    );
END
ELSE PRINT 'Edda Morcant already exists.';
GO

