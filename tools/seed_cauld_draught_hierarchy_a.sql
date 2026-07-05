SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ============================================================
-- HOUSE DRAUGHT — UPPER HIERARCHY PART A
-- Ruling Family + Political Cabinet + Military Command
-- Universe: The Cauld (0197E9C9-0002-7000-8000-000000000002)
-- ~24 named characters
-- ============================================================

-- ============================================================
-- RULING FAMILY
-- ============================================================

-- 1. LORD HALVARD SKARDE — Lord of House Draught
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Halvard Skarde')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Halvard Skarde', N'halvard-skarde', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id,
        N'Halvard Skarde',
        N'halvard-skarde',
        N'Halvard',
        N'Skarde',
        N'Lord',
        N'human',
        N'human',
        N'male',
        N'he/him',
        63,
        N'alive',
        N'Lord of House Draught',
        N'Six decades of war have carved Halvard Skarde into something more monument than man. He commands the most militarized House in the Cauld with the flat certainty of someone who has stopped imagining alternatives. The failure no one mentions in his presence: twenty-two years ago he ordered a three-front withdrawal from the Keldmark Scrying Installation — a decision made in forty seconds under artillery fire — and House Fornax walked through the gap. Keldmark has been theirs ever since. Halvard knows the math was sound. He also knows his brother Eirik died in that withdrawal. He has spent two decades building a House that would never need to make that choice again.',
        N'Patriarch and ultimate arbiter. His approval is the prize every faction in House Draught competes for. His guilt over Keldmark and his unresolved grief over Gudrun define every major decision the House makes. The weight he carries is the engine of the House''s overreach.',
        N'No POV.',
        N'House Draught; northern fjord territories, glacial plateau and sea-raid coast',
        195,
        115,
        N'Broad-shouldered, dense with muscle running to weathered iron; built like a siege engine that learned patience',
        N'Iron-grey',
        N'Cropped close, utilitarian',
        N'Short',
        N'Pale grey',
        N'Fair, deeply weathered',
        N'Heavily lined across the brow and jaw; a scar running from the left temple to the ear from a Serpens blade taken twenty years ago',
        N'Subtle height gain, increased density',
        N'Completely still when thinking; moves with deliberate economy; never raises his voice — the room quiets without any signal from him',
        N'Heavy wool and hardened leather even at formal councils; the House seal on a plain iron ring, never a pendant; no decorative ironwork',
        N'Knight-grade Transmutation: marginal height gain, skeletal density increase, accelerated wound closure. Right hand carries the burn scarring from his infusion point.',
        N'Dawn weapons practice alone in the garrison yard; morning war council; private correspondence hour with Chancellor Thora; long stretches of silence in front of his wife''s portrait',
        N'He intercepted a Liturgy transit manifest four years ago that lists his dead wife Gudrun as ''taken'' rather than killed in action. He has told no one. He pays the Spymaster in private coin to find the answer. He does not know whether she is alive in Sphere 31 or dead on a transit table somewhere in the Liturgy system.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'House Draught primary estate; Scrying Installation frontier; neutral-ground treaty locations across the Cauld',
        N'0',
        N'0',
        N'Weathered Norse warlord, sixties, iron-grey cropped hair, pale grey eyes, heavily scarred jaw, heavy wool military coat over hardened leather, austere stone hall, dim firelight, medieval steampunk Scandinavia',
        N'A weathered Norse lord in his sixties, iron-grey hair cropped short, pale grey eyes, face deeply lined and scarred, wearing heavy military wool and hardened leather, standing in a stone garrison hall lit by iron sconces',
        0,
        0
    );
END ELSE PRINT 'Halvard Skarde already exists.';
GO

-- 2. GUDRUN SKARDE — deceased spouse of Lord Halvard (legacy character)
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Gudrun Skarde')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Gudrun Skarde', N'gudrun-skarde', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id,
        N'Gudrun Skarde',
        N'gudrun-skarde',
        N'Gudrun',
        N'Skarde',
        N'Lady',
        N'human',
        N'human',
        N'female',
        N'she/her',
        58,
        N'dead',
        N'Deceased spouse of Lord Halvard Skarde; the House''s lost strategic mind',
        N'Gudrun Skarde died — or appeared to die — during the Harrow Bay raid fourteen years ago. Her body was never recovered. The official account: she fell into the water during a House Serpens ambush and was lost to current. Lord Halvard buried an empty casket. She was, by every account, the tactical mind behind House Draught''s greatest decade of expansion — sharper than Halvard, twice as patient, and the only person who had ever made him second-guess a decision without making him feel diminished. The House never recovered its momentum after her loss. Her portrait hangs above the war table. Her opinion is invoked at every council, always posthumously, always in support of whatever position is most convenient to the speaker.',
        N'Absent presence. Every decision in House Draught is measured against what she would have thought of it. Her unresolved death is the structural crack in the House''s foundation.',
        N'No POV.',
        N'House Draught; northern fjord territories; born of a minor coastal family, married into the Skarde line',
        173,
        70,
        N'Lean and precise; described by those who knew her as someone who moved like she had already decided where everyone else in the room was going to end up',
        N'Dark auburn, almost brown',
        N'Typically braided back for function',
        N'Long',
        N'Deep brown',
        N'Fair with warm undertone',
        N'No visible scarring recorded; Knight-grade Transmutation left minimal external trace',
        N'Subtle height gain, increased density',
        N'Recalled as exceptionally still during councils; moved with premeditated efficiency in the field',
        N'Practical field dress with a single decorative silver clasp at the shoulder — the only ornament anyone recalls her wearing consistently',
        N'Knight-grade Transmutation; her files record the infusion was unusually clean with minimal recovery time — an anomaly the Infirmary noted but never explained.',
        N'Dead; no current daily life. Her schedule, as recalled by those who served under her, was famous for beginning before dawn and ending when the problem was solved.',
        N'Whether she chose transit through the Liturgy membrane or was taken against her will is unknown. The transit manifest that lists her name — filed by the Spymaster and now in the Archivist''s sealed holdings — is genuine. If she is alive in Sphere 31, she has been there for fourteen years under whatever name the Liturgy assigned her.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Deceased; formerly operated across all of House Draught''s strategic territory',
        N'0',
        N'0',
        N'Norse noblewoman warrior, early forties implied age, dark auburn braided hair, brown eyes, quiet authority, lean precise bearing, medieval steampunk Scandinavia fantasy setting, portrait style, firelit stone hall',
        N'A Norse noblewoman in her early forties, dark auburn hair braided back, brown eyes with striking focus, wearing practical military dress with a single silver clasp at the shoulder, painted in portrait style, warm firelight',
        0,
        0
    );
END ELSE PRINT 'Gudrun Skarde already exists.';
GO

-- 3. ASTRID SKARDE — Heir to House Draught
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Astrid Skarde')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Astrid Skarde', N'astrid-skarde', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id,
        N'Astrid Skarde',
        N'astrid-skarde',
        N'Astrid',
        N'Skarde',
        N'Lady',
        N'human',
        N'human',
        N'female',
        N'she/her',
        29,
        N'alive',
        N'Heir to House Draught; Knight-ranked Myrmidon',
        N'Astrid is everything House Draught respects — fast, decisive, unafraid of the sea, and utterly without sentimentality about violence. She achieved Knight rank at twenty-four, three years younger than her father managed it. What she chafes against: she has not been permitted to lead a sea-raid since being formally named heir. Every engagement she joins is structured, strategic, and calibrated for minimum risk to her person. She is being preserved. She hates it with a thoroughness that she has learned to perform as calm.',
        N'The heir whose patience is running out. Her arc is the question of whether she will act before she has authority to — and what that costs the House when she does.',
        N'No POV.',
        N'House Draught; northern fjord territories; born and raised in the garrison estate',
        185,
        80,
        N'Athletic and tightly coiled; the Knight augmentation has added half a head and considerable density without losing the speed',
        N'Pale gold, almost white in winter light',
        N'Pulled back hard when working, loose only in private',
        N'Long',
        N'Grey-blue',
        N'Fair',
        N'Clean; a small burn scar on the back of the right hand from her Transmutation infusion',
        N'Subtle height gain, increased density',
        N'Economical and controlled; the stillness has an edge to it — she moves like something about to be released',
        N'Field-practical: close-fitted leather and reinforced wool, nothing that would snag; the heir''s mark is a narrow silver braid woven into her left cuff, which she frequently removes',
        N'Knight-grade Transmutation: height gain, increased bone density, improved reaction threshold. Infusion completed at twenty-four with a four-day recovery — faster than average.',
        N'Morning sparring with garrison officers; tactical review of her father''s war councils; correspondence sessions she does not fully disclose to the Chancellor; riding the coastal cliffs alone when she has a decision to make',
        N'She found her father''s private correspondence about the Gudrun transit manifest eight months ago — she did not understand all of it but understands enough to know her mother may not be dead. She has told no one, including her father. She has been planning the forbidden raid for four months and has been in written communication with Commander Freyja and Cousin Gunnar. She is deciding whether to move before she has authority or lose the nerve entirely.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'House Draught estate; garrison; coastal patrol routes; supervised tactical engagements on the Fornax border',
        N'0',
        N'0',
        N'Young Norse warrior woman, late twenties, pale gold hair pulled back hard, grey-blue eyes, lean athletic build, close-fitted leather and reinforced wool, medieval steampunk Scandinavia, cold coastal light',
        N'A young Norse warrior woman in her late twenties, pale gold hair pulled sharply back, grey-blue eyes, wearing close-fitted leather and reinforced wool, standing in cold coastal light against stone battlements',
        0,
        0
    );
END ELSE PRINT 'Astrid Skarde already exists.';
GO

-- 4. LEIF SKARDE — Second born; Champion-rank
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Leif Skarde')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Leif Skarde', N'leif-skarde', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id,
        N'Leif Skarde',
        N'leif-skarde',
        N'Leif',
        N'Skarde',
        N'',
        N'human',
        N'human',
        N'male',
        N'he/him',
        31,
        N'alive',
        N'Second son of House Draught; Champion-rank Myrmidon; operates independently',
        N'Leif Skarde survived Transmutation twice — the first infusion at nineteen, the second at twenty-six. The second took three weeks to resolve and left him at two-eleven: shoulders that fill a doorframe, proportions no tailor''s pattern can accommodate, and eyes the flat white of pack ice. No House can formally designate a Champion, and Leif has never been named heir. He simply exists, adjacent to power and beholden to none of it. Six years of independent raids, contract enforcement for smaller Houses, and occasional diplomatic escort that functions as intimidation. The freedom is real. The loneliness is real. His father looks at him with something that takes equal parts awe and guilt to produce.',
        N'The Champion outside the hierarchy — he is the most dangerous person in any room and the only member of the family who answers to nothing. His relationship with Halvard is the House''s most complex emotional seam.',
        N'No POV.',
        N'House Draught; northern fjord territories by birth; currently operates across multiple House territories without fixed base',
        211,
        185,
        N'Post-human: the second infusion remade him entirely; the proportions are not simply larger but differently distributed, like the body made a series of decisions a normal skeleton would refuse',
        N'Pale ash, almost silver',
        N'Loose, rarely managed',
        N'Long',
        N'Flat white — the iris is barely distinguishable from the sclera',
        N'Fair',
        N'Minimal scarring for someone with his history; the Champion frame heals aggressively',
        N'Pronounced — post-human form; no House can designate or contain them',
        N'Unhurried in a way that large predators are unhurried; never needs to signal intent',
        N'Whatever he was last given or bought; the Champion augmentation renders him indifferent to weather, so he underdresses for conditions that send everyone else for cover',
        N'Champion-grade Transmutation: two successful infusions. Post-human height, mass, skeletal restructuring, dramatically accelerated recovery, altered ocular pigmentation. The second infusion changed his eyes permanently.',
        N'Contract raids; independent enforcement work; occasional months at the Draught estate that he treats as visits rather than homecomings',
        N'He knows his father blames himself for Eirik''s death at Keldmark, and he has been deliberately maintaining a warmer relationship with Halvard than he actually feels — because he watched his father come close to abdication in the years after Keldmark and made a private decision to prevent the collapse. He has been quietly managing Halvard''s emotional state for six years while performing ease. No one in the House knows.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Pan-Cauld; no fixed territory; returns to House Draught estate intermittently',
        N'0',
        N'0',
        N'Post-human Norse Champion, early thirties, silver-white hair loose, flat white eyes, massive altered frame, two meters eleven, relaxed bearing, medieval steampunk fantasy, cold northern light',
        N'A post-human Norse Champion in his early thirties, silver-ash hair loose to the shoulders, eyes nearly all white with barely visible iris, enormous altered frame at over two meters, wearing simple clothes in cold northern light',
        0,
        0
    );
END ELSE PRINT 'Leif Skarde already exists.';
GO

-- 5. BRYNDIS SKARDE — youngest child; being shaped for political marriage
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Bryndis Skarde')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Bryndis Skarde', N'bryndis-skarde', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id,
        N'Bryndis Skarde',
        N'bryndis-skarde',
        N'Bryndis',
        N'Skarde',
        N'Lady',
        N'human',
        N'human',
        N'female',
        N'she/her',
        18,
        N'alive',
        N'Youngest child of Lord Halvard; betrothed to a House Ophiuchus minor lord for political purposes',
        N'Bryndis is seventeen years younger than her sister Astrid — born while Halvard was in the field, raised largely by the Dowager and the household staff. She has her mother''s patience and her father''s stubbornness, neither of which have yet been tested at scale. Halvard has arranged her betrothal to a minor lord of House Ophiuchus: a political bridge to a House that Draught openly despises. Bryndis has not refused the arrangement. She has done something more dangerous: she has been systematically studying Ophiuchus doctrine, military method, and political structure with focused, private intent. Whether she plans to use that knowledge to serve the marriage or to destroy its terms from within is unclear even to her.',
        N'The youngest piece on the board who has started reading the rules. Her arc is the question of what a Draught girl does when she''s been traded to the enemy and decides to become fluent in them.',
        N'No POV.',
        N'House Draught; garrison estate; born and raised under Dowager Ragnhild''s primary influence',
        170,
        60,
        N'Slight but not fragile; moves carefully in the way of someone who learned early to be underestimated',
        N'Dark brown with a faint auburn cast, like her mother',
        N'Braided in House Draught style; she has not yet changed this',
        N'Long',
        N'Dark brown',
        N'Fair with warm undertone',
        N'Clear; no Transmutation',
        N'none',
        N'Precise and deliberate; she is still learning to use stillness as a tool, and sometimes it reads as youth rather than choice',
        N'House formal at councils, practical everywhere else; she has started incorporating Ophiuchus fabric patterns into her private dress — quietly, a hem at a time',
        N'None. No Transmutation.',
        N'Private study of Ophiuchus political and military texts; lessons with the Chancellor that officially cover treaty protocol and cover unofficially whatever she manages to redirect the conversation toward; long conversations with the Dowager that both of them describe as ''afternoon visits''',
        N'Through the Archivist''s access logs — which she found by accident during a document retrieval errand — she identified a Liturgy transit observation entry that references her mother Gudrun by name. She does not know what it means. She has not shown it to her father or anyone else. She is seventeen and has been sitting with this for three months, getting more precise about what questions to ask before she acts on it.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'House Draught garrison estate; formal councils; begins formal Ophiuchus court visits within the year',
        N'0',
        N'0',
        N'Young Norse noblewoman, eighteen, dark brown braided hair, watchful dark eyes, slight build, formal House dress with subtle Ophiuchus fabric detail at the hem, stone hall, medieval steampunk Scandinavia',
        N'A young Norse noblewoman of eighteen, dark brown braided hair, watchful dark brown eyes, slight precise bearing, wearing formal House dress with a subtle foreign fabric detail at the hem, standing in a stone hall',
        0,
        0
    );
END ELSE PRINT 'Bryndis Skarde already exists.';
GO

-- 6. RAGNHILD IVAR — Dowager; Lord Halvard's mother
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ragnhild Ivar')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ragnhild Ivar', N'ragnhild-ivar', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id,
        N'Ragnhild Ivar',
        N'ragnhild-ivar',
        N'Ragnhild',
        N'Ivar',
        N'Lady',
        N'human',
        N'human',
        N'female',
        N'she/her',
        81,
        N'alive',
        N'Dowager of House Draught; Lord Halvard''s mother; former ruling Lady',
        N'Ragnhild Ivar ruled House Draught for eleven years after her husband Oskar Skarde died of a wound gone septic — then handed authority to Halvard the day he returned from Keldmark. She has never explained that timing, and no one has dared ask. She is eighty-one, half a head shorter than she was at fifty (Paladin-grade Transmutation compresses the frame across decades), and has watched two full generational cycles of the Living War. She remembers when House Draught held a formal treaty with Serpens. She remembers when the Liturgy had three Installations. She says very little at councils. When she does speak, everyone else stops.',
        N'The institutional memory that the current generation does not know they are missing. Her silence is a form of pressure; every decision the House makes is being measured against a standard she has not announced.',
        N'No POV.',
        N'House Draught; born of the Ivar coastal family; married into Skarde line; ruled the House in her own right for eleven years',
        172,
        78,
        N'Compressed by age and long Transmutation: was taller; now dense and settled like stone that has finished shifting',
        N'White, fully',
        N'Pinned back, formal at all hours',
        N'Medium',
        N'Steel blue-grey',
        N'Fair, deeply aged',
        N'Profoundly lined; Paladin augmentation visible in the altered jaw and brow structure, the eyes with their slight luminescence in low light',
        N'Evident enhancement — significant height, altered proportions, changed eyes',
        N'Moves slowly by choice, not limitation; the deliberateness reads as authority rather than age',
        N'House formal exclusively; dark colours; the original Ivar family clasp at her throat, which she has worn since before Halvard was born',
        N'Paladin-grade Transmutation: two successful infusions over a lifetime; reduced height as the frame compresses with age, altered facial structure, slight ocular luminescence in low light. At eighty-one, the augmentation is visibly settled into her biology.',
        N'Morning audiences with Bryndis, which she frames as etiquette instruction; private reading; evening council reviews she attends without speaking; correspondence she manages through channels that predate Halvard''s appointment',
        N'She knows the Keldmark withdrawal was the correct decision militarily — she reviewed Halvard''s field notes herself, in the days after. She has spent twenty-two years not saying so, because she concluded that his guilt over Eirik is the discipline that has prevented him from repeating any version of that calculation. She is correct. She has never tested this theory by removing the variable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'House Draught estate; rarely leaves; the estate is arranged around her presence',
        N'0',
        N'0',
        N'Elderly Norse Dowager, eighties, white hair pinned formally, steel blue-grey eyes with slight luminescence, compressed powerful frame, dark formal dress, House family clasp, medieval steampunk Scandinavia, firelit stone interior',
        N'An elderly Norse noblewoman in her eighties, white hair pinned back formally, steel grey eyes with faint luminescence, wearing dark formal dress with an ancestral clasp at the throat, seated in a firelit stone hall with absolute stillness',
        0,
        0
    );
END ELSE PRINT 'Ragnhild Ivar already exists.';
GO

-- 7. GUNNAR DRAUGHT — cousin; northern coastal garrison commander
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Gunnar Draught')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Gunnar Draught', N'gunnar-draught', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id,
        N'Gunnar Draught',
        N'gunnar-draught',
        N'Gunnar',
        N'Draught',
        N'Ser',
        N'human',
        N'human',
        N'male',
        N'he/him',
        44,
        N'alive',
        N'First cousin of Lord Halvard; commander of the northern coastal garrisons',
        N'Gunnar Draught is Lord Halvard''s first cousin and has commanded the northern coastal garrison positions for nine years. He is competent in the specific way of someone raised with modest expectations of themselves: reliably adequate, occasionally brilliant when cornered. His loyalty to House Draught is assumed by everyone, including himself, because he has never encountered a situation that required him to choose between loyalty and something he wanted more. That situation is now arriving.',
        N'The cousin whose assumed loyalty becomes the story''s test case. He is the weight that tips the balance between the forbidden raid proceeding and being stopped — depending on which side of the line he lands on.',
        N'No POV.',
        N'House Draught; a cadet branch of the Skarde family that took the Draught territorial name two generations ago; northern coastal territory',
        188,
        95,
        N'Solid and broad; the Knight augmentation gives him a density that makes him look heavier than he is',
        N'Sandy brown going grey at the temples',
        N'Short, practical',
        N'Short',
        N'Brown',
        N'Fair, weathered',
        N'Several small scars across the left forearm from a Serpens engagement eight years ago',
        N'Subtle height gain, increased density',
        N'Reliable and direct; moves with the comfortable authority of someone whose rank is not questioned in his territory',
        N'Heavy garrison issue with Draught coastal insignia; functional and maintained rather than distinguished',
        N'Knight-grade Transmutation: standard height and density augmentation; no significant anomalies on record.',
        N'Garrison patrol reviews; morning briefings with his sub-commanders; private correspondence he has been conducting for the past four months that he has not disclosed to Lord Halvard',
        N'He has been in written communication with Astrid Skarde about the forbidden raid she is planning. He knows she will execute it with or without him. He is deciding whether to be present — which means deciding whether to betray Halvard by participating or betray Astrid by reporting her. He has reread every letter three times and has not yet answered the most recent one.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Northern coastal garrison territory; House Draught estate for formal councils',
        N'0',
        N'0',
        N'Norse garrison commander, mid-forties, sandy-brown greying hair, broad solid frame, garrison military uniform with coastal insignia, northern fjord setting, medieval steampunk Scandinavia',
        N'A Norse garrison commander in his mid-forties, sandy-brown hair greying at the temples, broad solid build, wearing heavy garrison military dress with coastal insignia, standing against a northern fjord backdrop',
        0,
        0
    );
END ELSE PRINT 'Gunnar Draught already exists.';
GO

-- 8. SIGRID KETIL — cousin who married into House Fornax and returned
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Sigrid Ketil')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Sigrid Ketil', N'sigrid-ketil', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id,
        N'Sigrid Ketil',
        N'sigrid-ketil',
        N'Sigrid',
        N'Ketil',
        N'Lady',
        N'human',
        N'human',
        N'female',
        N'she/her',
        38,
        N'alive',
        N'Draught cousin; formerly married into House Fornax; widowed and returned; complicated standing',
        N'Sigrid Ketil spent nine years as wife to a Fornax minor lord — long enough to learn their tactical doctrine, their contempt for improvisation, and their systematic approach to Scrying Installation management. When her husband died in a Serpens engagement, she returned to Draught with two young children and a widow''s portion. The House welcomed her home because family is family. They have not decided what to do with everything she knows. Neither has she.',
        N'The returned exile who is both asset and liability. She holds tactical intelligence about the House''s most hated rival and has not decided what to do with it. Her standing in House Draught is permanent but never comfortable.',
        N'No POV.',
        N'House Draught by birth; nine years embedded in House Fornax''s domestic and political structure; returned to Draught two years ago',
        175,
        68,
        N'Lean and contained; she moves like someone who spent years performing ease in a house where ease was not natural',
        N'Dark auburn',
        N'Down when at the Draught estate — she wore it up in Fornax style and has been slowly reverting',
        N'Medium',
        N'Grey-green',
        N'Fair',
        N'A small scar along the left collarbone; no Transmutation',
        N'none',
        N'Watchful; she has the habit of cataloguing exits, which she developed in Fornax and cannot entirely suppress',
        N'Draught practical with occasional Fornax tailoring details that she is phasing out deliberately, one garment at a time',
        N'None. No Transmutation.',
        N'Managing her children''s adjustment to the estate; attending House councils she was not formally invited to and has not been formally excluded from; long private walks she describes as thinking time',
        N'She carries the Fornax operational rotation schedule for the Keldmark Scrying Installation — the exact cycle vulnerabilities in their current deployment. She has not offered it to Lord Halvard. She does not know if she is protecting the nine years she spent building something in Fornax, or saving the information as leverage she has not yet decided to spend.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'House Draught estate; cautious re-integration into Draught social and political structure',
        N'0',
        N'0',
        N'Norse noblewoman, late thirties, dark auburn hair loosely down, grey-green watchful eyes, lean contained bearing, practical dress with faint Fornax tailoring details, stone hall interior, medieval steampunk Scandinavia',
        N'A Norse noblewoman in her late thirties, dark auburn hair worn down, grey-green eyes with a watchful quality, lean contained frame, wearing practical dress with subtle foreign tailoring details, standing in a stone hall interior',
        0,
        0
    );
END ELSE PRINT 'Sigrid Ketil already exists.';
GO

-- 9. EIRIK SKARDE — deceased; Lord Halvard's brother; legacy character
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Eirik Skarde')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Eirik Skarde', N'eirik-skarde', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id,
        N'Eirik Skarde',
        N'eirik-skarde',
        N'Eirik',
        N'Skarde',
        N'Ser',
        N'human',
        N'human',
        N'male',
        N'he/him',
        56,
        N'dead',
        N'Deceased; younger brother of Lord Halvard; Paladin-rank raider; killed at Keldmark covering the withdrawal',
        N'Eirik Skarde was a Paladin-rank raider and the second most feared Myrmidon in House Draught''s recent history. He died at Keldmark twenty-two years ago — during the withdrawal Halvard ordered — covering the retreat of forty-seven soldiers who made it out. No one who survived questions the order that got him killed. No one has ever said this to Halvard''s face. Eirik left behind no spouse, no children, and a reputation so large that the Corps still invokes his name in the oath ceremony. There is a training hall in the garrison named for him. Lord Halvard has never entered it.',
        N'The ghost at the foundation. His death is the source of Halvard''s guilt and the measure against which every act of House Draught courage is unconsciously compared. He shapes the House without being in it.',
        N'No POV.',
        N'House Draught; northern fjord territories; second son of Oskar and Ragnhild Skarde',
        200,
        130,
        N'By account: massive, even for a Paladin; described by survivors as moving through the Keldmark breach like a structure rather than a person',
        N'Dark brown',
        N'Short',
        N'Short',
        N'Blue-grey',
        N'Fair',
        N'Multiple engagement scars; died before anyone catalogued them in full',
        N'Evident enhancement — significant height, altered proportions, changed eyes',
        N'Described as someone who always knew exactly where he was standing and why; economy of motion carried to its extreme',
        N'Heavy Myrmidon field gear; described as indifferent to anything not functional',
        N'Paladin-grade Transmutation: two successful infusions; significant height, altered proportions, changed ocular structure. His file notes the second infusion was unusually fast — nine days — and left no recovery complications.',
        N'Dead; no current daily life.',
        N'A sealed letter addressed to Lord Halvard, written the night before the Keldmark engagement and never opened, is filed in the House Archivist''s secure holdings. The Archivist and the Spymaster both know it exists. Neither has told Halvard. The letter was written before the battle — which means Eirik knew, or suspected, that the engagement might kill him.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Deceased; formerly operated across all Draught combat territory',
        N'0',
        N'0',
        N'Massive Norse Paladin, would-be fifties, dark brown hair, blue-grey altered eyes, post-human proportions, heavy Myrmidon field gear, Keldmark engagement setting, smoke and firelight, medieval steampunk Scandinavia, heroic elegy tone',
        N'A massive Norse Paladin with altered proportions and blue-grey changed eyes, dark brown hair, wearing heavy Myrmidon field gear, depicted in an elegy portrait style with smoke and firelight, medieval steampunk Scandinavia',
        0,
        0
    );
END ELSE PRINT 'Eirik Skarde already exists.';
GO

-- ============================================================
-- POLITICAL CABINET
-- ============================================================

-- 10. THORA ULV — Chancellor
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Thora Ulv')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Thora Ulv', N'thora-ulv', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id,
        N'Thora Ulv',
        N'thora-ulv',
        N'Thora',
        N'Ulv',
        N'Mistress',
        N'human',
        N'human',
        N'female',
        N'she/her',
        67,
        N'alive',
        N'Chancellor of House Draught; has served three Lords; manages all political correspondence and negotiation',
        N'Thora Ulv has managed political correspondence and negotiation for three successive Lords of House Draught. She has outlasted a coup attempt, two succession crises, and one occasion where she personally rewrote a treaty clause over four sleepless days to prevent open war with House Serpens. She writes every significant letter herself and trusts no scribe with anything that matters. In the estimation of every other House Chancellor, she is the most dangerous non-combatant in the Cauld — not because she hoards secrets, but because she understands what other Houses want more clearly than they themselves do.',
        N'The institutional anchor. She knows where every political deal is buried and has outlasted everyone who made those deals. Her unauthorized back channel is the story''s most valuable thing no one knows about.',
        N'No POV.',
        N'House Draught; born of a Draught administrative family; has spent her entire adult career in the House''s political service',
        168,
        65,
        N'Trim and upright; age has not changed her posture; moves with complete economy of motion',
        N'White, formerly dark brown',
        N'Pinned back; never decorative',
        N'Short',
        N'Dark brown',
        N'Fair, aged',
        N'Minimal; a small scar on the right index finger from a sealing wax accident twenty years ago, which she considers a professional embarrassment',
        N'none',
        N'Economical and deliberate; the posture of someone who has spent decades projecting authority without physical scale to back it',
        N'Dark, precise House formal; ink-stained right index finger she does not bother hiding; the Chancellor''s seal on a thin chain',
        N'None. No Transmutation.',
        N'Pre-dawn correspondence review; morning audience with Lord Halvard; treaty and negotiation sessions; evening summary reports she writes herself, addressed to no one specific and filed for the next Chancellor',
        N'She has maintained an unauthorized private correspondence with her counterpart at House Ophiuchus for twenty years — a back channel that has, on three separate occasions, defused escalations that would have cost thousands of lives. She has never told any Lord about it. She considers it her most important work and intends to die with it undisclosed.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'House Draught estate; neutral treaty grounds; occasional travel to allied Houses for formal negotiation',
        N'0',
        N'0',
        N'Older Norse woman, late sixties, white hair pinned back precisely, dark brown sharp eyes, trim upright bearing, dark formal House dress, Chancellor''s seal on a chain, writing desk setting, medieval steampunk Scandinavia',
        N'An older Norse woman in her late sixties, white hair pinned back precisely, dark brown sharp eyes, trim upright bearing, wearing dark formal House dress with a Chancellor''s seal on a chain, seated at a writing desk in a stone hall',
        0,
        0
    );
END ELSE PRINT 'Thora Ulv already exists.';
GO

-- 11. KETIL RAGNAR — Spymaster
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ketil Ragnar')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ketil Ragnar', N'ketil-ragnar', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id,
        N'Ketil Ragnar',
        N'ketil-ragnar',
        N'Ketil',
        N'Ragnar',
        N'Master',
        N'human',
        N'human',
        N'male',
        N'he/him',
        52,
        N'alive',
        N'Spymaster of House Draught; runs intelligence operations across four Houses and three Liturgy installations',
        N'Ketil Ragnar runs the most productive intelligence network in the northern Cauld. He has assets in four Houses, three Liturgy installations, and two border markets. He is polite in the particular way of people who know exactly what you did last season — completely, without implication, which is somehow worse than being accused. He knows about the Sphere 31 transit operations. He has never given Lord Halvard the full picture. He compartmentalizes this information not out of disloyalty but because he has seen what happens when Lords learn too much too fast and make reactive decisions that burn his assets.',
        N'The man who knows too much and has decided what he will and will not say. The question the story eventually has to answer is whether his compartmentalization is protective or self-serving.',
        N'No POV.',
        N'House Draught; origin deliberately obscured in his own files; has served the intelligence function for sixteen years',
        182,
        88,
        N'Medium build, deliberately forgettable; he has spent years calibrating his appearance toward the average',
        N'Brown, unremarkable',
        N'Short, unremarkable',
        N'Short',
        N'Brown',
        N'Fair-medium',
        N'None visible; there are two, but they are not visible',
        N'none',
        N'Practiced neutrality in expression and movement; the kind of stillness that reads as mild until you notice he has been cataloguing everyone in the room since he entered',
        N'Unremarkable grey and brown; the Master''s seal small and worn on the inside of his left cuff where it requires a specific reach to see',
        N'None. No Transmutation.',
        N'Receives reports across an encrypted correspondence network before anyone else in the House is awake; attends the minimum required councils; moves the rest of the day through channels that are not on the House schedule',
        N'He has a Sphere 31 asset — a person taken from Sphere 31 who now works in House Serpens'' domestic staff — who has been passing him Serpens Scrying Installation rotation data for three years. He considers this asset irreplaceable. If Lord Halvard ever orders the asset burned, he will say the asset is dead and continue running them. He has already decided this.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'House Draught estate; the entire Cauld through proxy; physically unremarkable in any setting',
        N'0',
        N'0',
        N'Norse spymaster, early fifties, deliberately unremarkable brown hair and build, watchful brown eyes, grey-brown unremarkable clothing, stone corridor setting, medieval steampunk Scandinavia, the kind of man you forget you saw',
        N'A Norse spymaster in his early fifties, medium build and deliberately unremarkable coloring, watchful brown eyes, wearing plain grey-brown clothing, standing in a stone corridor in a way that makes him easy to overlook',
        0,
        0
    );
END ELSE PRINT 'Ketil Ragnar already exists.';
GO

-- 12. SOLVEIG BRENN — House Archivist
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Solveig Brenn')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Solveig Brenn', N'solveig-brenn', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id,
        N'Solveig Brenn',
        N'solveig-brenn',
        N'Solveig',
        N'Brenn',
        N'Mistress',
        N'human',
        N'human',
        N'female',
        N'she/her',
        59,
        N'alive',
        N'House Archivist; has maintained House Draught''s records for thirty-one years',
        N'Solveig Brenn has maintained House Draught''s records for thirty-one years: every treaty, genealogy, Scrying installation log, field report, and financial instrument the House has ever produced. She has photographic recall of everything she has ever read, and a professional ethic that forbids her from sharing what she knows unless formally asked. The gap between ''formally asked'' and ''actually needs to know'' is where most of her moral life is spent. She knows where every body is buried. In two specific cases, this is not metaphorical.',
        N'The keeper of the House''s true history. She is the only person who holds all three major secrets simultaneously — Eirik''s letter, Gudrun''s transit manifest, and Bjorn''s discrepancy. Her neutrality is her religion and her burden.',
        N'No POV.',
        N'House Draught; born of a minor administrative family; appointed Archivist at twenty-eight after the previous one died of a fever',
        165,
        62,
        N'Slight and precise; the kind of person who takes up exactly the space they intend to and not a centimeter more',
        N'Grey-brown, fading',
        N'Pulled back with functional pins; she loses one approximately every three days and replaces it without comment',
        N'Medium',
        N'Grey',
        N'Fair, indoor-pale',
        N'Ink stains on both hands that she has long since stopped trying to remove',
        N'none',
        N'Careful and quiet; the habitual tread of someone who works in silence and does not wish to disturb it',
        N'Simple dark working dress; the Archivist''s key ring on a heavy belt hook; ink on everything',
        N'None. No Transmutation.',
        N'Archive management; document retrieval for council and correspondence requests; the Archivist''s log she writes nightly in a cipher that only she reads',
        N'She is the custodian of Eirik Skarde''s unsealed letter, Gudrun Skarde''s Liturgy transit manifest, and the financial discrepancy records that Treasurer Bjorn Halvorsen believes are secure. She read all three when they were filed. She has said nothing to anyone. She believes in the neutrality of the archive with a ferocity that frightens her in her private moments — because she suspects the day is coming when someone will formally ask the right question, and she will have to answer it.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'The House Draught archive; rarely leaves it for more than a council session',
        N'0',
        N'0',
        N'Older Norse woman, late fifties, grey-brown hair with functional pins, grey eyes, slight precise frame, dark working dress, ink-stained hands, stone archive interior filled with documents and record cases, medieval steampunk Scandinavia',
        N'An older Norse woman in her late fifties, grey-brown hair pinned back functionally, grey eyes, slight precise bearing, wearing dark working dress with ink-stained hands, standing in a stone archive filled with document cases',
        0,
        0
    );
END ELSE PRINT 'Solveig Brenn already exists.';
GO

-- 13. IVAR HELGA — Trade Ambassador
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ivar Helga')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ivar Helga', N'ivar-helga', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id,
        N'Ivar Helga',
        N'ivar-helga',
        N'Ivar',
        N'Helga',
        N'Master',
        N'human',
        N'human',
        N'male',
        N'he/him',
        46,
        N'alive',
        N'Trade Ambassador; manages House Draught commercial relationships with allied Houses and border markets',
        N'Ivar Helga manages House Draught''s commerce with three allied Houses and the northern border markets. He is charming in a way that suggests he has practiced it, and his negotiations return favorable terms with sufficient consistency that no one looks too closely at his method. He designed a currency conversion mechanism four years ago that he described to Treasurer Bjorn Halvorsen as a more efficient routing system. He believed Bjorn was using it the same way he was. He is only now beginning to suspect this is not the case.',
        N'The charming man whose infrastructure is being used for something he didn''t intend. His arc is the discovery that the system he built has implications he wasn''t part of.',
        N'No POV.',
        N'House Draught; border market territory; born to a merchant family that attached itself to Draught service three generations ago',
        178,
        82,
        N'Medium build kept deliberately well-maintained; the kind of physical presentation that signals trustworthiness without attracting attention',
        N'Blond, well-kept',
        N'Short and neat',
        N'Short',
        N'Blue',
        N'Fair',
        N'None',
        N'none',
        N'Relaxed and open; he has learned to make people comfortable as a professional tool',
        N'The best cloth he can justify; not ostentatious, but consistently the best-dressed person in a border market meeting',
        N'None. No Transmutation.',
        N'Commerce negotiations at border markets and allied House courts; correspondence management; the private accounting he maintains in a notation system that is not the House system',
        N'He has been skimming a percentage of border market proceeds into a private account for seven years — approximately forty thousand marks accumulated. He designed the routing mechanism he gave Bjorn as a tool for his own use and assumed Bjorn adopted it for identical purposes. He has no exit plan. He has never genuinely thought about what happens when the audit finds him.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Border markets; allied House courts; House Draught estate for reporting sessions',
        N'0',
        N'0',
        N'Norse trade ambassador, mid-forties, neat blond hair, blue eyes, medium well-maintained build, fine cloth that avoids ostentation, border market setting, medieval steampunk Scandinavia',
        N'A Norse trade ambassador in his mid-forties, neat blond hair, blue eyes, medium well-maintained build, wearing fine cloth that stops just short of ostentation, standing in a border market hall',
        0,
        0
    );
END ELSE PRINT 'Ivar Helga already exists.';
GO

-- 14. INGRID MAELSTROM — Liturgy Liaison
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ingrid Maelstrom')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ingrid Maelstrom', N'ingrid-maelstrom', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id,
        N'Ingrid Maelstrom',
        N'ingrid-maelstrom',
        N'Ingrid',
        N'Maelstrom',
        N'',
        N'human',
        N'human',
        N'female',
        N'she/her',
        41,
        N'alive',
        N'Liturgy Liaison attached to House Draught; reports to both institutions; trusted by neither',
        N'Ingrid Maelstrom represents the Liturgy''s interests within House Draught. She reports upward to the Liturgy administration and laterally to Chancellor Thora on matters of mutual concern. Neither side fully trusts her. Both are correct not to. She has been posted here six years and has developed, against her training and better judgment, a genuine loyalty to House Draught that she has not yet been required to choose over her Liturgy obligations. She is waiting for that moment with the controlled dread of someone who has already made the choice in private and simply has not announced it.',
        N'The embedded agent who has gone native without officially defecting. Her arc is the moment her two loyalties are placed in direct opposition.',
        N'No POV.',
        N'Liturgy-assigned identity: House Draught vicinity; true origin: Sphere 31, taken at age nineteen',
        170,
        65,
        N'Medium; the Liturgy''s placement training emphasizes physical neutrality — she has no distinguishing features she did not cultivate',
        N'Dark brown',
        N'Down and simply managed — she avoids both obvious Draught styling and obvious Liturgy marking',
        N'Medium',
        N'Brown',
        N'Medium fair',
        N'None visible',
        N'none',
        N'Neutral and professional; the permanent slight tension of someone managing two sets of behavioral expectations simultaneously',
        N'Liturgy neutral grey — the uniform that signals institutional rather than House allegiance; she has started wearing a Draught-palette brooch that she tells herself is purely functional',
        N'None. No Transmutation.',
        N'Joint reporting sessions with the Chancellor and the Liturgy''s communication relay; access management for Scrying Installation observation logs; six years of private adjustment to a life she increasingly considers her real one',
        N'She is not from the Cauld. She was taken from Sphere 31 at age nineteen, trained by the Liturgy, and placed here as a long-duration observer. Her Cauld identity — documentation, family history, references — is completely fabricated. She does not know whether she was taken voluntarily or involuntarily. She has stopped trying to remember. She has not communicated with anyone from her actual origin in eleven years and is no longer certain she could identify her real name.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'House Draught estate; Liturgy communication relay points; Scrying Installation observation access',
        N'0',
        N'0',
        N'Norse woman, early forties, dark brown hair simply managed, neutral expression, Liturgy grey clothing with a single Draught brooch, medieval steampunk Scandinavia, stone hall interior, the look of someone holding two things at once',
        N'A Norse woman in her early forties, dark brown hair simply worn, neutral expression suggesting controlled duality, wearing Liturgy grey with a single Draught-colored brooch at the collar, standing in a stone hall interior',
        0,
        0
    );
END ELSE PRINT 'Ingrid Maelstrom already exists.';
GO

-- 15. BJORN HALVORSEN — Treasurer / Chamberlain
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Bjorn Halvorsen')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Bjorn Halvorsen', N'bjorn-halvorsen', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id,
        N'Bjorn Halvorsen',
        N'bjorn-halvorsen',
        N'Bjorn',
        N'Halvorsen',
        N'Master',
        N'human',
        N'human',
        N'male',
        N'he/him',
        55,
        N'alive',
        N'Treasurer and Chamberlain of House Draught; managing House finances while quietly servicing a debt he cannot disclose',
        N'Bjorn Halvorsen manages House Draught''s finances with the focused precision of someone who knows exactly what he is hiding and exactly how long before it is found. He is meticulous, reliable, and has served in this role for fourteen years. His son Kettl died of a failed Transmutation infusion three years ago, leaving gambling debts to a border market creditor who does not accept condolences as payment. Bjorn has been routing House funds through the currency conversion mechanism Ivar Helga designed to service the debt incrementally. He believed, at the time, that Ivar designed the system for identical purposes and considered this an unspoken mutual accommodation.',
        N'The embezzler who is almost out of time and has been writing his confession for eighteen months. His arc is the collision between his nearly-complete repayment and the audit that does not know it should wait eight months.',
        N'No POV.',
        N'House Draught; born of a garrison administrative family; appointed Treasurer fourteen years ago',
        180,
        95,
        N'Stocky and solid; the build of a man who used to work physically and shifted to a desk without the shape following',
        N'Brown going grey, kept short',
        N'Short and tidy',
        N'Short',
        N'Brown',
        N'Fair, indoor-pale',
        N'None',
        N'none',
        N'Contained and careful; he moves through the House like someone who is being careful not to draw attention, which has become his default bearing regardless of whether he is hiding anything at that specific moment',
        N'House formal; well-maintained; nothing that signals wealth he should not have',
        N'None. No Transmutation.',
        N'Financial review and ledger management; council attendance; the private correspondence he maintains with the border market creditor that he routes through a third-party agent so the letters are not traceable to him directly',
        N'His son Kettl died of a failed Transmutation infusion three years ago and left gambling debts totaling approximately ninety thousand marks. Bjorn has been servicing this debt via Ivar Helga''s currency conversion routing mechanism. The debt has eight months remaining. He has written his confession letter to Lord Halvard eleven times and plans to deliver it the day after the final payment clears. He has been planning this for eighteen months. He is only now realizing that Ivar may not have known what the mechanism was being used for — meaning he has been an unwitting co-conspirator rather than a knowing one.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'House Draught estate; financial review chambers; occasional border market travel he frames as commerce oversight',
        N'0',
        N'0',
        N'Norse treasurer, mid-fifties, brown-going-grey hair, stocky precise build, House formal dress, nothing that signals wealth, ledger-room setting, medieval steampunk Scandinavia, the bearing of a man carrying something heavy',
        N'A Norse treasurer in his mid-fifties, brown-going-grey hair, stocky contained build, wearing precise House formal dress, seated at a ledger desk in a stone chamber, the posture of a man who has been carrying something heavy for a long time',
        0,
        0
    );
END ELSE PRINT 'Bjorn Halvorsen already exists.';
GO

-- 16. RAGNAR SKJOLD — Diplomat serving at House Ophiuchus; currently abroad
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ragnar Skjold')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ragnar Skjold', N'ragnar-skjold', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id,
        N'Ragnar Skjold',
        N'ragnar-skjold',
        N'Ragnar',
        N'Skjold',
        N'Master',
        N'human',
        N'human',
        N'male',
        N'he/him',
        36,
        N'alive',
        N'House Draught diplomat posted to House Ophiuchus; currently abroad; has been sitting on explosive intelligence for five months',
        N'Ragnar Skjold has represented House Draught at House Ophiuchus''s court for four years. He is meticulous and systematic in a way that makes Ophiuchus scholars respect him despite themselves — he learned their library and archival classification systems, their research methodologies, their scholarly forms of address. This thoroughness has consequences: because he was precise and because he had deep archival access, he found something he was not supposed to find. He sends reports home. He does not send everything.',
        N'The diplomat abroad who knows something that would change everything if he came home to say it. His arc is the choice between the safety of distance and the obligation of return.',
        N'No POV.',
        N'House Draught; currently posted to House Ophiuchus territory; fourth year of posting',
        176,
        78,
        N'Medium and quietly impressive; he has learned to dress and move in Ophiuchus style without abandoning his Draught bearing entirely, which reads as versatility',
        N'Dark blond',
        N'Neat, slightly longer than Draught standard — an unconscious Ophiuchus adaptation',
        N'Short-medium',
        N'Grey',
        N'Fair',
        N'None',
        N'none',
        N'Precise and composed; the posture of someone accustomed to representing a House other people underestimate',
        N'A blend of Draught practical and Ophiuchus scholarly formal; he has been unconsciously drifting toward the latter for two years',
        N'None. No Transmutation.',
        N'Diplomatic duties at the Ophiuchus court; archival research that he frames as background scholarship; the increasingly elaborate private correspondence he maintains with himself, working out what to do with what he knows',
        N'Through Ophiuchus archival access he legitimately obtained, he discovered that the Keldmark Scrying Installation was not captured by House Fornax — it was surrendered. A secret codicil to a treaty signed by Lord Halvard''s predecessor traded Keldmark for something that the treaty does not name directly. He has been reading around this for five months trying to determine what was received in exchange. Coming home means explaining why he did not report it immediately. He does not have an answer to that question yet.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'House Ophiuchus court and archive; formally represents House Draught at Ophiuchus diplomatic functions',
        N'0',
        N'0',
        N'Norse diplomat, mid-thirties, dark blond hair slightly longer than military standard, grey watchful eyes, medium frame, Draught-practical dress with Ophiuchus scholarly detail, archive reading room setting, medieval steampunk Scandinavia',
        N'A Norse diplomat in his mid-thirties, dark blond hair slightly longer than military standard, grey watchful eyes, medium frame, wearing Draught military dress with scholarly Ophiuchus accents, standing in an archive reading room',
        0,
        0
    );
END ELSE PRINT 'Ragnar Skjold already exists.';
GO

-- ============================================================
-- MILITARY COMMAND
-- ============================================================

-- 17. FREYJA ULF — Commander of the Myrmidon Corps
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Freyja Ulf')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Freyja Ulf', N'freyja-ulf', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id,
        N'Freyja Ulf',
        N'freyja-ulf',
        N'Freyja',
        N'Ulf',
        N'Dame',
        N'human',
        N'human',
        N'female',
        N'she/her',
        49,
        N'alive',
        N'Commander of the House Draught Myrmidon Corps; Paladin rank; holds Lord Halvard''s unconditional confidence',
        N'Freyja Ulf has commanded House Draught''s Myrmidon Corps for twelve years. She is a Paladin — the second Transmutation took eleven days to resolve and left her at two meters with a quality of stillness in her face that newer soldiers find unsettling until they understand it as concentration. Lord Halvard trusts her without reservation. The Corps trusts her with the particular fervor of people who have marched through engagements they should not have survived. The problem is not disloyalty. The problem is that she now controls more of the practical machinery of House Draught than Lord Halvard does, and she has been in correspondence with Astrid Skarde about a raid he has specifically forbidden.',
        N'The Commander who has too much power and knows it, and has not yet decided what to do with that knowledge. Her arc is the question of whether she uses the authority she''s been given to serve the House or to serve the heir who will replace its current Lord.',
        N'No POV.',
        N'House Draught; northern fjord coastal family; entered the Corps at fifteen; has served no other institution',
        200,
        125,
        N'Paladin-grade: two meters, altered proportions, the frame of someone the second infusion rebuilt from first principles',
        N'Silver-white — her hair went white during the second infusion at thirty-seven and has not changed since',
        N'Braided and pinned for function at all times',
        N'Long',
        N'Grey with a faint amber luminescence in low light — the Paladin ocular change',
        N'Fair',
        N'A long scar running from the left shoulder across the collarbone — taken at the Harrow engagement nine years ago; the only significant scar the Paladin augmentation has not fully closed',
        N'Evident enhancement — significant height, altered proportions, changed eyes',
        N'Perfectly controlled; the stillness of someone who has learned to carry a Paladin''s weight without advertising it; very rarely gestures when speaking',
        N'Corps commander formal: reinforced field dress with Commander insignia; nothing decorative; the Corps seal on a plain iron disc at her chest',
        N'Paladin-grade Transmutation: two successful infusions; significant height, restructured proportions, altered ocular pigmentation with faint amber luminescence, dramatically accelerated healing. The second infusion changed her hair permanently.',
        N'Dawn field training with the Corps; morning operational review; Commander''s council with Halvor and Ragna; private correspondence she does not log through the official Corps channel',
        N'Two years ago, Lord Halvard met with her privately and gave her standing verbal orders to assume full command of House Draught if he is killed or incapacitated. He issued this authority without consulting his Chancellor, his heir, or any member of the ruling family. She has told no one. The night after the meeting, she wrote a sealed letter to herself documenting the exchange and filed it under a false subject heading in her personal records. She has been in correspondence with Astrid about the forbidden raid. She has not connected, in writing, these two facts.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'House Draught military territory; Corps installations; combat front when engaged',
        N'0',
        N'0',
        N'Norse Paladin commander, late forties, silver-white braided hair, amber-luminescent grey eyes, two-meter altered frame, Corps commander formal dress with iron seal, garrison setting, medieval steampunk Scandinavia',
        N'A Norse Paladin commander in her late forties, silver-white hair braided and pinned, grey eyes with faint amber luminescence, two-meter altered frame, wearing reinforced Corps commander dress with an iron seal, standing in a garrison interior',
        0,
        0
    );
END ELSE PRINT 'Freyja Ulf already exists.';
GO

-- 18. HALVOR BJORNSSON — First Captain, ground operations
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Halvor Bjornsson')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Halvor Bjornsson', N'halvor-bjornsson', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id,
        N'Halvor Bjornsson',
        N'halvor-bjornsson',
        N'Halvor',
        N'Bjornsson',
        N'Ser',
        N'human',
        N'human',
        N'male',
        N'he/him',
        43,
        N'alive',
        N'First Captain of the Myrmidon Corps; ground operations commander; Commander Freyja''s counterweight',
        N'Halvor Bjornsson commands ground operations with a directness that approaches rudeness and an operational record that makes the rudeness acceptable. He disagrees with Commander Freyja on almost every methodological question: she believes in patience and position; he believes in speed and pressure. Their campaigns work because the tension between their approaches generates solutions neither would find independently. Their working relationship is the fault line in the Corps. When they agree, it means one of them has conceded something they will want back later.',
        N'The fault line in the military hierarchy. His relationship with Freyja is productive and unstable in equal measure. His discovery of her correspondence with Astrid is the story''s military tripwire.',
        N'No POV.',
        N'House Draught; northern interior; entered the Corps at seventeen; has never served anywhere else',
        192,
        105,
        N'Big and dense with the Knight augmentation; moves like someone who makes tactical decisions faster than other people finish their sentences',
        N'Dark brown, heavy',
        N'Short, often dishevelled after morning training',
        N'Short',
        N'Brown',
        N'Fair, weathered',
        N'A scar across the bridge of the nose from a Serpens engagement six years ago; several smaller marks along the right forearm',
        N'Subtle height gain, increased density',
        N'Expansive and often abrupt; he gestures when talking strategy, which Freyja does not, and the contrast is visible in every joint council session',
        N'Corps field dress; functional and slightly battered; he considers immaculate kit a sign of a soldier who isn''t working hard enough',
        N'Knight-grade Transmutation: height gain, skeletal density increase, improved recovery. Standard augmentation outcome; no anomalies.',
        N'Pre-dawn physical training with the ground corps; morning operational briefings; joint Commander''s council he treats as a structured disagreement session; evening field review',
        N'Six years ago, during the Serpens southern engagement, he falsified a casualty report — inflated enemy losses to cover a tactical error that got twelve of his soldiers killed. Senior Sergeant Ulf Ketil witnessed the falsification and said nothing, because the Corps needed Halvor and because the error, under the conditions, could have been made by almost anyone. Halvor does not know Ulf saw him. He has never returned to this decision in any recorded reflection. He does not know if he has buried it or if he has simply stopped keeping the ledger.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'House Draught military territory; ground corps operational zones; forward positions during engagement',
        N'0',
        N'0',
        N'Norse military officer, early forties, dark brown heavy hair, scarred nose, big dense Knight-augmented build, battered Corps field dress, garrison training yard, medieval steampunk Scandinavia',
        N'A Norse military officer in his early forties, dark brown heavy hair, a scar across the bridge of his nose, big Knight-augmented build in battered Corps field dress, standing in a garrison training yard',
        0,
        0
    );
END ELSE PRINT 'Halvor Bjornsson already exists.';
GO

-- 19. RAGNA HALV — Second Captain; garrison and installation defense
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ragna Halv')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ragna Halv', N'ragna-halv', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id,
        N'Ragna Halv',
        N'ragna-halv',
        N'Ragna',
        N'Halv',
        N'Dame',
        N'human',
        N'human',
        N'female',
        N'she/her',
        38,
        N'alive',
        N'Second Captain of the Myrmidon Corps; garrison and Scrying Installation defense',
        N'Ragna Halv commands the garrison and Installation defense — the assignment that keeps her home while everyone else marches. She is not bitter about this. She chose it eight years ago when her daughter was born, and has spent those years making the garrison into something that would take a full Corps to crack. She is the one who stays. The defensive rotation she designed for the Scrying Installations has since been adopted by two other Houses. Neither credited her.',
        N'The stay-behind who has built something more significant than the people who left. Her unauthorized intake assessments are the story''s quiet institutional violation — the one that is being done for the right reasons by someone who knows she would be stopped if she asked.',
        N'No POV.',
        N'House Draught; interior garrison territory; entered the Corps at nineteen',
        186,
        90,
        N'Knight-augmented: lean and dense; she has the specific build of someone who has spent eight years doing garrison defense and field training simultaneously',
        N'Dark brown',
        N'Braided tightly; she is always either in the middle of something or about to be',
        N'Long',
        N'Brown',
        N'Fair-medium',
        N'A scar along the left jaw from a training accident twelve years ago, before her Transmutation',
        N'Subtle height gain, increased density',
        N'Efficient and pragmatic; she wastes no movement; the bearing of someone who has optimized the garrison and applies the same principles to herself',
        N'Corps field dress without decorative distinction; the Second Captain''s mark on the left shoulder; everything maintained to operational standard',
        N'Knight-grade Transmutation: height gain, density increase, improved wound recovery. Standard outcome.',
        N'Morning garrison inspection; Installation defense rotation review; training sessions she attends as participant, not observer; the unofficial intake assessment work she does not put in the Corps log',
        N'She has been running unauthorized Transmutation intake assessments on the garrison''s civilian support population — identifying candidates with the physiological markers for infusion survival — and submitting them to the Corps Practitioner without flagging them as civilian identifications. She believes she is building the next generation of the Corps. She has not requested authorization because she knows she would be denied it. The Practitioner has processed seven of her referrals without asking where they came from.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'House Draught garrison; Installation defense perimeter; does not leave the territory except for Corps command meetings',
        N'0',
        N'0',
        N'Norse garrison captain, late thirties, dark brown tightly braided hair, brown eyes, lean Knight-augmented build, Corps field dress with Second Captain insignia, garrison interior, medieval steampunk Scandinavia',
        N'A Norse garrison captain in her late thirties, dark brown hair tightly braided, brown eyes, lean Knight-augmented build, wearing Corps field dress with Second Captain insignia, standing in a garrison interior',
        0,
        0
    );
END ELSE PRINT 'Ragna Halv already exists.';
GO

-- 20. HELGA RASK — Infirmary Commander
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Helga Rask')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Helga Rask', N'helga-rask', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id,
        N'Helga Rask',
        N'helga-rask',
        N'Helga',
        N'Rask',
        N'Dame',
        N'human',
        N'human',
        N'female',
        N'she/her',
        51,
        N'alive',
        N'Infirmary Commander of the Myrmidon Corps; has overseen the field hospital for seventeen years',
        N'Helga Rask has kept soldiers alive through things that should have killed them. She has also watched forty-three people die from Transmutation infusions on her table over seventeen years as Infirmary Commander. She knows the numbers: the eighty-percent first-infusion mortality, the specific failure modes that look like success until day twelve, the mechanisms by which a surviving body sometimes decides, without announcement, that it has changed its mind. She does not discuss these numbers socially. She discusses them at intake, in full, every time, with a directness that has caused some candidates to walk away. She considers these her best outcomes.',
        N'The woman who counts the cost while everyone else celebrates the gain. Her unreleased survival-marker research is the story''s suppressed medical revolution — knowledge that could save lives and is being withheld for reasons that are ethically defensible and practically agonizing.',
        N'No POV.',
        N'House Draught; trained in field medicine before the Corps attached her as Infirmary Commander; Knight-grade Transmutation at thirty-one',
        188,
        95,
        N'Knight-augmented; the height gain reads as authority in a medical context; she moves through the infirmary like the building is organized around her, because it is',
        N'Brown going grey, equal parts both',
        N'Pulled back and pinned; functional; she does not adjust it during a procedure',
        N'Medium',
        N'Brown',
        N'Fair, with deep lines from twenty-five years of field work',
        N'A burn on the right forearm from a Transmutation infusion complication eight years ago; the scar is significant and she does not cover it',
        N'Subtle height gain, increased density',
        N'Steady and deliberate; the movement of someone who has learned that the infirmary runs on her affect as much as her skill',
        N'Infirmary dress: clean, practical, nothing that should not be in proximity to a patient',
        N'Knight-grade Transmutation at thirty-one: standard height gain, density increase, improved recovery. She uses the augmented recovery to work longer hours than any unaugmented person in the Corps.',
        N'Infirmary rounds; intake assessments before campaigns; the Transmutation infusion sessions she conducts with the Corps Practitioner present; the private research log she has been maintaining for eleven years',
        N'She has documented physiological survival markers for Transmutation across eleven years of intake assessment and believes she can predict, with approximately seventy-three percent accuracy, who will survive a first infusion. She has not published or reported this because she knows it will immediately be used to compel civilian infusions — and because twenty-seven percent of people her method labels likely survivors still die. She cannot determine whether a method that is right seventy-three percent of the time is a gift or a different kind of cruelty.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'House Draught infirmary; field hospital during campaigns; does not operate outside Corps medical territory',
        N'0',
        N'0',
        N'Norse field surgeon, early fifties, brown-going-grey hair pinned back, brown steady eyes, Knight-augmented height, infirmary dress, visible burn scar on right forearm, field hospital interior, medieval steampunk Scandinavia',
        N'A Norse field surgeon in her early fifties, brown-going-grey hair pinned back, brown steady eyes, tall Knight-augmented frame, wearing practical infirmary dress with a significant burn scar visible on the right forearm, standing in a field hospital interior',
        0,
        0
    );
END ELSE PRINT 'Helga Rask already exists.';
GO

-- 21. ULF KETIL — Senior Sergeant; 27 years in the Corps
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ulf Ketil')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ulf Ketil', N'ulf-ketil', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id,
        N'Ulf Ketil',
        N'ulf-ketil',
        N'Ulf',
        N'Ketil',
        N'',
        N'human',
        N'human',
        N'male',
        N'he/him',
        51,
        N'alive',
        N'Senior Sergeant of the Myrmidon Corps; twenty-seven years of service; institutional memory of the Corps',
        N'Ulf Ketil has been in the Myrmidon Corps for twenty-seven years. He has served under five commanding officers and remembers all of them with the particular precision of someone who had no reason to be politic about it. He knows what Commander Freyja''s official record says and what the actual record is. He knows the same about every officer in the Corps. He keeps nothing written down. He keeps everything else. He is also the reason the oath ceremony still invokes Eirik Skarde''s name — he insisted on it after the ceremony language was revised fifteen years ago and no one in command had the standing to argue with twenty-seven years of service about what the Corps remembers.',
        N'The institutional memory that the officer class cannot access or control. His knowledge of Halvor''s falsified report is the story''s loaded weapon. The question is whether he ever decides to use it, and what makes him decide.',
        N'No POV.',
        N'House Draught; northern fjord coast; entered the Corps at twenty-four; has never attempted Transmutation',
        183,
        100,
        N'Big and solid without augmentation: a natural frame that has spent nearly three decades in active military service; he is the same size as some Knights and does not carry a Knight''s density',
        N'Faded brown going grey',
        N'Short, kept that way out of habit',
        N'Short',
        N'Brown',
        N'Fair, deeply weathered',
        N'More scars than a thorough count would be worth; he stopped cataloguing them at some point in his thirties',
        N'none',
        N'The specific ease of someone who has nothing left to prove to anyone in the room; he moves like a man who arrived at this garrison before the current command structure was born',
        N'Corps field dress at the appropriate grade; well-maintained and unremarkable; the twenty-seven-year service mark on his left shoulder, which he has polished so many times the enamel is beginning to thin',
        N'None. No Transmutation. The fact that he has survived twenty-seven years in the Corps without augmentation is its own form of distinction, and he is aware that everyone in the building is aware of it.',
        N'Early morning corps review; training supervision for junior soldiers; the specific kind of mentorship he delivers through proximity rather than instruction',
        N'He witnessed First Captain Halvor Bjornsson falsify a casualty report six years ago — inflating enemy losses to cover a tactical error that got twelve soldiers killed. He said nothing at the time because the Corps needed Halvor and because the mistake, under the conditions of that engagement, could have been made by almost any ground commander. He has been carrying this for six years. He does not know what it would take to make him speak. He suspects he is waiting for an event that would make the silence cost more than the speaking.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'House Draught garrison; Corps training grounds; operational zones during engagement',
        N'0',
        N'0',
        N'Older Norse career soldier, early fifties, faded brown-grey hair, big natural build without augmentation, heavily scarred, Corps field dress with long-service mark, garrison interior, medieval steampunk Scandinavia, the bearing of someone who has been here longer than everyone',
        N'An older Norse career soldier in his early fifties, faded brown-grey hair, a big natural build without augmentation, heavily scarred face and hands, wearing Corps field dress with a worn long-service mark, standing in a garrison interior',
        0,
        0
    );
END ELSE PRINT 'Ulf Ketil already exists.';
GO

-- 22. SIGRUN ASKEL — veteran soldier near retirement
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Sigrun Askel')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Sigrun Askel', N'sigrun-askel', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id,
        N'Sigrun Askel',
        N'sigrun-askel',
        N'Sigrun',
        N'Askel',
        N'',
        N'human',
        N'human',
        N'female',
        N'she/her',
        49,
        N'alive',
        N'Veteran soldier; thirty years of service; three months from pension eligibility; no plan for after',
        N'Sigrun Askel has been in the Corps for thirty years. She never attempted Transmutation — she watched her bunkmate die on the table at twenty-two and decided the eighty-percent odds were not for her. She has survived this long on technique, situational instinct, and the precise economy of someone who learned early to spend herself correctly. She is three months from pension eligibility. Every time she imagines the civilian life after, the picture runs for about forty minutes and then stops, and she has to start it again from the beginning.',
        N'The soldier who has no country outside the war. Her retirement arc is the question that every institution eventually faces about what it does to the people it forms.',
        N'No POV.',
        N'House Draught; coastal garrison family; entered the Corps at nineteen',
        178,
        82,
        N'Lean and precise; thirty years of active service without augmentation has produced a body that runs on efficiency rather than mass',
        N'Grey, formerly dark brown',
        N'Short, practical',
        N'Short',
        N'Green-grey',
        N'Fair, weathered',
        N'Multiple small scars; the significant one is a blade scar across the left shoulder from a Serpens engagement twelve years ago that came close enough to the collarbone that she keeps the shoulder covered even in warm weather',
        N'none',
        N'The precise movement of someone who has spent thirty years making sure nothing she does is wasted',
        N'Corps field dress; clean and maintained; no service marks beyond the thirty-year stripe she wears on the right shoulder',
        N'None. No Transmutation.',
        N'Active patrol rotations she has not yet been reassigned from; morning sparring she uses partly as training and partly as the structure that keeps her day coherent; evenings she has stopped filling',
        N'Eight years ago she was offered a position training new recruits — a role that would have moved her permanently off active rotation and extended her career comfortably into her fifties and beyond. She declined because she did not want to watch people die in training before they had been soldiers. Since then she has watched fifteen of her contemporaries die in the field. She does not know if the trade was correct. She has been not-knowing this for eight years and suspects the answer is: it was not.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'House Draught garrison and patrol territory; has operated across most of Draught''s active combat zones over thirty years',
        N'0',
        N'0',
        N'Older Norse female soldier, late forties, grey short hair, lean precise unaugmented build, Corps field dress with thirty-year service stripe, garrison setting, medieval steampunk Scandinavia, the bearing of someone near the end of a long run',
        N'An older Norse female soldier in her late forties, grey short hair, lean precise unaugmented build, wearing Corps field dress with a thirty-year service stripe, standing in a garrison setting with the bearing of someone near the end of a long run',
        0,
        0
    );
END ELSE PRINT 'Sigrun Askel already exists.';
GO

-- 23. THORD BRYN — junior officer recently distinguished
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Thord Bryn')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Thord Bryn', N'thord-bryn', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id,
        N'Thord Bryn',
        N'thord-bryn',
        N'Thord',
        N'Bryn',
        N'Ser',
        N'human',
        N'human',
        N'male',
        N'he/him',
        24,
        N'alive',
        N'Junior officer; recently Knight-ranked; distinguished himself six weeks ago at the Harrenvalt engagement; currently being watched',
        N'Thord Bryn distinguished himself six weeks ago at the Harrenvalt engagement, where he reorganized a collapsing flank in forty-three seconds with no surviving senior officer to authorize the decision. He saved the position. He has been watched ever since: by Commander Freyja, who sees what he could become, and by Senior Sergeant Ulf, who has seen how quickly the Corps consumes what it decides it owns. Thord himself is still processing what it cost to become someone worth watching.',
        N'The newly made officer who has not yet decided what kind of soldier he intends to be. He is being shaped by two people with incompatible ideas about what he should become.',
        N'No POV.',
        N'House Draught; northern fjord coast; entered the Corps at eighteen; Knight Transmutation two months ago',
        190,
        100,
        N'Knight-augmented and still adjusting to it; he occasionally reaches for something at the wrong height or misjudges clearance in a doorway he would have fit through before the infusion',
        N'Dark blond',
        N'Short, no opinion about it',
        N'Short',
        N'Blue-grey',
        N'Fair',
        N'The burn scar from his Transmutation infusion point at the base of the left forearm; new enough that he is still noticing it',
        N'Subtle height gain, increased density',
        N'Still calibrating; the augmentation changed his physical relationship to space and he has not finished adjusting; the deliberateness reads as caution, which is not entirely wrong',
        N'Corps field dress; new Knight insignia that he has not yet learned to wear with ease',
        N'Knight-grade Transmutation two months ago: standard height gain, density increase. One of four survivors from a cohort of eleven.',
        N'Field operations; adjustment to Knight rank; the correspondence home he has been keeping current',
        N'He survived his Knight infusion two months ago — one of four survivors from eleven who attempted it that cycle. He watched seven people die over three days. He has not told his family. He answers their letters about his promotion without mentioning the word Transmutation. He does not know if this is protection or cowardice and suspects the distinction may not matter as much as he thought it would.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'House Draught garrison; field operational zones; still operating in Halvor Bjornsson''s ground command',
        N'0',
        N'0',
        N'Young Norse soldier, mid-twenties, dark blond short hair, blue-grey eyes, newly Knight-augmented build he is still adjusting to, Corps field dress with new Knight insignia, garrison setting, medieval steampunk Scandinavia',
        N'A young Norse soldier in his mid-twenties, dark blond hair, blue-grey eyes, a newly Knight-augmented build he is still learning to wear, dressed in Corps field gear with new Knight insignia, standing in a garrison yard',
        0,
        0
    );
END ELSE PRINT 'Thord Bryn already exists.';
GO

-- 24. GRIMHILD SOLVIG — Corps Transmutation Practitioner
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Grimhild Solvig')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Grimhild Solvig', N'grimhild-solvig', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id,
        N'Grimhild Solvig',
        N'grimhild-solvig',
        N'Grimhild',
        N'Solvig',
        N'Mistress',
        N'human',
        N'human',
        N'female',
        N'she/her',
        47,
        N'alive',
        N'Corps Transmutation Practitioner; administers infusions before campaigns; knows the death rate intimately',
        N'Grimhild Solvig has administered two hundred and fourteen Transmutation infusions over nineteen years as the Corps'' certified Practitioner. Of those, one hundred and seventy-one did not survive. She keeps a ledger — not the official record, which documents outcomes in clinical notation, but a private one with names, dates, and where she knew enough to write it, the thing each person had been looking forward to. She speaks at intake assessments with the clarity of someone who needs candidates to understand exactly what they are choosing. They choose it anyway. They always choose it.',
        N'The witness. She is the only person in the Corps whose job is to be present at the moment the House''s power source is manufactured — and to count the cost every time. Her unreleased second infusion is the story''s quiet moral mirror: she administers what she will not take.',
        N'No POV.',
        N'House Draught; trained as a Practitioner at the Liturgy''s Transmutation certification program; has served the Corps for nineteen years',
        186,
        92,
        N'Knight-augmented from her own first infusion at twenty-two; she has not pursued the second infusion that her Knight status technically enables',
        N'Silver-grey — went grey early, in her thirties; she does not know if this is from the infusion or from the years',
        N'Pinned back with the clinical precision of someone for whom loose hair during a procedure is a professional failure',
        N'Medium',
        N'Grey',
        N'Fair, indoor-pale',
        N'The Transmutation infusion scar at the base of her right wrist — she always makes sure candidates can see it when she explains the process',
        N'Subtle height gain, increased density',
        N'Deliberate and measured; she has the particular calm of someone who has decided, long ago, that calm is the only thing she can offer',
        N'Practitioner''s dress: clean, unornamented, practical; she wears the Practitioner''s certification seal on a chain but rarely displays it',
        N'Knight-grade Transmutation at twenty-two: standard height gain, density increase, improved recovery. She has been eligible for a second infusion for twenty-one years and has not pursued it.',
        N'Intake assessments before campaigns; the infusion sessions themselves; the private ledger she writes in after each session; correspondence with the Liturgy''s Practitioner certification board that she treats as a professional obligation and a personal distance',
        N'She survived her first infusion at twenty-two, one of three survivors from a cohort of seventeen. She has been eligible for a second infusion for twenty-one years and has never attempted it. She believes this is cowardice. She has continued to administer infusions to others for nineteen years because someone has to, and because she cannot assign that task to someone who does not understand what it costs when the person on the table dies. She has given last rites to people who died on her table. She has never discussed this with anyone.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'Not applicable.',
        N'House Draught infirmary and Transmutation chamber; does not leave the Corps medical complex except for campaign preparation briefings',
        N'0',
        N'0',
        N'Norse Transmutation practitioner, late forties, silver-grey hair pinned precisely, grey eyes with controlled calm, Knight-augmented height, clean Practitioner dress, Transmutation chamber interior, the infusion scar at the wrist visible, medieval steampunk Scandinavia',
        N'A Norse Transmutation practitioner in her late forties, silver-grey hair pinned back precisely, grey eyes with practiced calm, wearing clean Practitioner dress, standing in a stone Transmutation chamber with the infusion scar at her right wrist visible',
        0,
        0
    );
END ELSE PRINT 'Grimhild Solvig already exists.';
GO
