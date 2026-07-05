SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ============================================================
-- HOUSE OPHIUCHUS — UPPER HIERARCHY SEED (PART A)
-- The Cauld | UniverseId: 0197E9C9-0002-7000-8000-000000000002
-- Generated: 2026-07-04
-- Ruling Family (9) + Political Cabinet (7) + Military Command (8) = 24 total
-- The scholarly House; Italy analog; southern peninsula, Mediterranean-warm.
-- First to treat the membrane scientifically. House Venn merged generations ago.
-- ============================================================

-- ============================================================
-- RULING FAMILY
-- ============================================================

-- 1. Lord Orazio Venn-Ophiuchus — Head of House
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Orazio Venn-Ophiuchus')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Orazio Venn-Ophiuchus', N'orazio-venn-ophiuchus', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Orazio Venn-Ophiuchus', N'orazio-venn-ophiuchus', N'Orazio', N'Venn-Ophiuchus', N'Lord', N'human', N'human',
        N'male', N'he/him', 68, N'alive',
        N'Head of House Ophiuchus',
        N'Lord of House Ophiuchus for twenty-three years. A scholar who inherited a military situation he was not bred for and managed it through patience, alliance-building, and intelligence rather than force of arms. The Vigil Seat retreat — his forces withdrew on an assessment that the northern Ridge position was untenable, leaving four hundred soldiers to die holding it — is the wound his entire tenure has been organized around explaining. The diplomatic peace that followed bought the House two decades of stability and cost him the unambiguous respect of every officer who served there. He is sixty-eight, silver-haired, lean in the way of men whose bodies were never the point, and permanently tired around the eyes in a way that has nothing to do with sleep.',
        N'The political and moral center of House Ophiuchus; his unspoken knowledge about the Vigil Seat failure is the structural fault line from which every other House secret depends.',
        N'No POV.',
        N'House Ophiuchus; southern peninsula, Mediterranean coast and northern Ridge country',
        185, 82,
        N'lean, scholarly; a man whose body was never the point',
        N'silver', N'swept back, precise', N'short', N'dark brown', N'warm olive', N'weathered; tired around the eyes',
        N'none',
        N'Upright — the posture of someone who decided decades ago that the body would not betray the mind''s opinion of itself. Pauses before speaking in council; his advisers have learned to wait the pause out without filling it.',
        N'Dark wool, always formal; House Ophiuchus burgundy and midnight blue. Small House seal worn as a pin at the collar. Never informal in the public rooms.',
        N'none',
        N'Morning correspondence review with Chancellor Mancini. Afternoon study of Scrying logs, recently weighted toward translated Sphere 31 texts. Formal evening audiences. Reads until very late.',
        N'He did not order the Vigil Seat retreat on his own judgment. Dame Alessandra Torri assessed the position as untenable and he accepted her calculation. Bartolomeo died on her advice, relayed through him. He has carried the blame publicly for twenty-three years because naming Torri would have destroyed the Corps. Additionally: three days before the incursion, Bartolomeo sent him documented evidence that House Draco''s senior leadership had been compromised by Liturgy influence. He received it. He acted on Torri''s tactical assessment anyway. The document still exists. He tells himself it did not change the military situation. He does not entirely believe this.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Ophiuchus southern peninsula seat; the Vigil Seat installation (northern Ridge country); diplomatic transit as required',
        N'0', N'0',
        N'elderly Italian lord, late sixties, silver swept-back hair, warm olive skin, dark brown eyes, formal dark wool robes in burgundy and midnight blue, House seal brooch pin, austere Mediterranean stone study with candlelight and open manuscripts, dignified burdened expression --ar 2:3',
        N'A 68-year-old Italian nobleman in formal medieval scholarly attire, silver swept-back hair, olive skin, dark brown eyes, wearing dark wool in burgundy and midnight blue with a small House seal pin; seated in a stone study with candles and manuscripts; dignified, weathered, quietly burdened.',
        0, 0
    );
END
ELSE PRINT 'Orazio Venn-Ophiuchus already exists.';
GO

-- 2. Lady Cosima Venn-Ophiuchus (née Torretti) — Deceased Spouse
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Cosima Venn-Ophiuchus')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Cosima Venn-Ophiuchus', N'cosima-venn-ophiuchus', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Cosima Venn-Ophiuchus', N'cosima-venn-ophiuchus', N'Cosima', N'Venn-Ophiuchus', N'Lady', N'human', N'human',
        N'female', N'she/her', 54, N'dead',
        N'Lord''s Consort (deceased); foremost Sphere 31 translator of her generation',
        N'Died twelve years ago of a wasting illness that the House physicians could not name and the Liturgy''s Transmutation practitioners could not reverse. Born Torretti, a coastal scholarly family with no military tradition and a library that made the House Archive look cautious. She was the foremost translator of Sphere 31 texts in the Cauld at the time of her death — her work is partially understood by everyone who has looked at it since and fully understood by no one. She was slight, dark-haired, and moved through rooms the way a person moves through their own mind: quickly, reaching for the next thing. Orazio has not allowed anyone to attempt further translation of her private notes. He has also not destroyed them.',
        N'The absent intellectual whose suppressed final translation is the most consequential decision a dead character has made; her notes are the key to three different crises, none of which has surface yet.',
        N'No POV.',
        N'House Ophiuchus (by marriage); born Torretti family, coastal scholarly lineage, southern peninsula',
        162, 57,
        N'slender, scholarly; quick-moving, always reaching for the next thing',
        N'dark brown', N'loosely pinned when working', N'long', N'dark brown', N'warm olive', N'clear, fine-lined',
        N'none',
        N'Remembered as quick-moving, always reaching for a text or making a note. Rarely sat still for long. Formal when she had to be and impatient with herself for having to be.',
        N'Informal when working — practical wool, ink-stained sleeves. Formal when required, the House silk for audiences she found tiresome. The distinction was a personality, not a wardrobe.',
        N'none',
        N'Deceased. In her time: translation work in the mornings, Sphere 31 text sessions through the afternoons, evenings with Orazio and the children. She maintained her own notes separately from the Archive and asked that they be kept that way.',
        N'Three of her untranslated Sphere 31 texts contain medical literature from the other side describing her illness by name, with treatment protocols. She translated enough to understand this before she died. She chose not to tell Orazio because she did not trust the Liturgy with the knowledge that Sphere 31 medicine could cure what the Cauld could not. The texts remain in the Archive, partially readable by everyone who has looked at them since. Archivist Silvana Crispi has Cosima''s private translation notes in a separate, uncatalogued drawer and has told no one.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Deceased. In life: the House Archive, the translation room she maintained, the children''s wing, and wherever Orazio was working.',
        N'0', N'0',
        N'portrait of a medieval Italian scholar-lady, mid-forties, dark brown hair loosely pinned, olive skin, dark brown eyes, practical ink-stained wool, surrounded by Sphere 31 manuscripts and translation texts in warm candlelight, scholarly and quick-minded --ar 2:3',
        N'A medieval Italian scholar-woman in her mid-forties, dark brown hair loosely pinned, olive skin, dark brown eyes, wearing practical ink-stained wool; surrounded by manuscripts and Sphere 31 texts in candlelight; quick-minded expression, just looked up from her work.',
        0, 0
    );
END
ELSE PRINT 'Cosima Venn-Ophiuchus already exists.';
GO

-- 3. Marco Venn-Ophiuchus — The Heir
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Marco Venn-Ophiuchus')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Marco Venn-Ophiuchus', N'marco-venn-ophiuchus', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Marco Venn-Ophiuchus', N'marco-venn-ophiuchus', N'Marco', N'Venn-Ophiuchus', N'', N'human', N'human',
        N'male', N'he/him', 34, N'alive',
        N'Heir to House Ophiuchus',
        N'Eldest son of Lord Orazio. Formally groomed in diplomacy, cartography, and Scrying theory — work he is competent at and finds insufficient. He believes the House has been accumulating knowledge for decades without using it, and that the other Houses have mistaken his father''s patience for weakness. He chafes against the ceremonial weight of the heir role with an energy he keeps parliamentary in formal settings and does not bother to keep parliamentary in private. He is thirty-four, dark-curly-haired, and moves with the barely-contained tension of someone waiting for the room to acknowledge it is behind schedule.',
        N'The heir who will either repeat his father''s pattern or break it — his secret recruitment of the Commander is the mechanism that could collapse everything Orazio has held together for twenty-three years.',
        N'No POV.',
        N'House Ophiuchus; southern peninsula; born Venn-Ophiuchus main line',
        180, 80,
        N'medium-athletic; not a soldier but maintains himself; the energy of someone who is waiting',
        N'dark brown, curly', N'cut short, managed', N'short', N'dark brown', N'warm olive', N'clear',
        N'none',
        N'Moves with barely-contained energy. Paces when thinking. In formal settings he is precisely still, which reads as effort, not composure.',
        N'House Ophiuchus formal colors, cut for movement rather than ceremony. Less precise than his father; more precise than the situation requires. The clothes say he takes this seriously; the posture says not in the way they want him to.',
        N'none',
        N'Morning study in diplomacy and Scrying theory. Afternoons in the Corps training yards, watching rather than participating. Evenings with advisers or in private correspondence that does not go through the Chancellor.',
        N'He has made private contact with Commander Alessandra Torri to discuss what a more aggressive campaign against House Draco would look like in practice. He has not told his father. He does not know that Torri is the reason his father carries the Vigil Seat failure — he has recruited the architect of the event he blames Orazio for surviving.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Ophiuchus main seat; occasional supervised visits to the Vigil Seat installation; the training yards',
        N'0', N'0',
        N'young Italian noble, mid-thirties, dark curly hair cut short, warm olive skin, dark brown eyes, formal House Ophiuchus colors cut for movement, Mediterranean stone courtyard, coiled impatient energy, watching a training yard --ar 2:3',
        N'A 34-year-old Italian nobleman, dark curly hair cut short, olive skin, dark brown eyes, wearing House formal colors in movement-ready cut; standing at the edge of a training yard watching soldiers; the expression of someone who believes the world is moving too slowly.',
        0, 0
    );
END
ELSE PRINT 'Marco Venn-Ophiuchus already exists.';
GO

-- 4. Valentina Venn-Ophiuchus — The Second Born
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Valentina Venn-Ophiuchus')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Valentina Venn-Ophiuchus', N'valentina-venn-ophiuchus', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Valentina Venn-Ophiuchus', N'valentina-venn-ophiuchus', N'Valentina', N'Venn-Ophiuchus', N'', N'human', N'human',
        N'female', N'she/her', 31, N'alive',
        N'Foreign Observer and Intelligence Coordinator, House Ophiuchus; second-born child of Lord Orazio',
        N'The second child, passed over for the title by birth order alone. Sharper than her brother by every metric her tutors privately agree on. She runs a network of foreign observers — travelers, merchants, occasional soldiers — under the assumed name Sera Vale. The House knows she does intelligence work; they do not know its scope. She is thirty-one, dark-haired, lean, and has learned to enter rooms without causing them to rearrange themselves around her, which is a skill her brother has never acquired and probably could not.',
        N'The second child whose greater capability is the House''s most useful unacknowledged asset — and whose suppressed intelligence discovery about House Draco is the most urgent piece of information anyone in Ophiuchus is currently sitting on.',
        N'No POV.',
        N'House Ophiuchus; southern peninsula; born Venn-Ophiuchus main line',
        168, 62,
        N'lean, quick-moving; deliberate in how much space she takes up',
        N'dark brown', N'long, often braided for travel', N'long', N'dark brown', N'warm olive', N'clear',
        N'none',
        N'Moves to avoid being the center of attention — not shy, strategic. Has learned to enter rooms without causing them to rearrange themselves around her. Watches exits.',
        N'Deliberately unremarkable traveling wool in border-market neutrals when working. House formal colors only when she must be officially Valentina. She maintains three wardrobes: home, the road, and the assumed name.',
        N'none',
        N'Manages her observer network through correspondence channels her father does not know the full shape of. Receives and processes reports from contacts in border markets and other Houses. Trains physically every morning. Travels more than anyone in the main seat knows.',
        N'She has confirmed through her network that House Draco has a Sphere 31 person currently advising their Scrying operations — someone taken from the other side who understands the apparatus intuitively in ways no Cauld-born practitioner does. She has not told her father or brother. Revealing it exposes her network; the political implications would reshape every alliance in the Cauld and change the House''s relationship with the Liturgy. She is waiting until she knows who she can trust to act on it correctly. She is not sure that person currently exists in the House.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Operates across border markets and inter-House zones under her assumed identity; reports to the Ophiuchus main seat; travel as her network requires',
        N'0', N'0',
        N'young Italian noblewoman, early thirties, dark hair in practical travel braid, olive skin, dark brown eyes, deliberately unremarkable traveling wool, medieval border market setting, watchful intelligent expression, not trying to be noticed --ar 2:3',
        N'A 31-year-old Italian woman, dark hair in a practical braid, olive skin, dark brown eyes, wearing neutral traveling wool; standing in a medieval border market with her back to a wall; watchful, deliberate, clearly assessing the room.',
        0, 0
    );
END
ELSE PRINT 'Valentina Venn-Ophiuchus already exists.';
GO

-- 5. Rinaldo Venn-Ophiuchus — Ward and Political Marriage Candidate
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Rinaldo Venn-Ophiuchus')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Rinaldo Venn-Ophiuchus', N'rinaldo-venn-ophiuchus', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Rinaldo Venn-Ophiuchus', N'rinaldo-venn-ophiuchus', N'Rinaldo', N'Venn-Ophiuchus', N'', N'human', N'human',
        N'male', N'he/him', 19, N'alive',
        N'Ward of House Ophiuchus; diplomatic marriage candidate for House Serpens alliance',
        N'The orphaned son of a Venn-branch cousin, raised in the main household since he was eleven. His parents died in a border skirmish that no one in the House discusses in front of him. He is being shaped for a political marriage arrangement with House Serpens — formally cooperative in his training, diligent in language study and diplomatic etiquette, and miserable in private in a way he has learned to make invisible. He is nineteen, slight, dark-eyed, and still growing into a body that has not yet decided what it intends to be.',
        N'The ward whose private love and public compliance represent the human cost of the House''s political machinery; his decision to proceed regardless is the most quietly devastating choice in the ruling family.',
        N'No POV.',
        N'House Ophiuchus (Venn branch); orphaned cousin''s son, raised in main household from age eleven',
        173, 67,
        N'slight; still growing into himself; the careful economy of someone who has learned not to take up space',
        N'black', N'loose, to the shoulders', N'medium', N'dark brown', N'olive', N'clear',
        N'none',
        N'Cooperative in formal settings; the kind of compliance that is indistinguishable from resignation to anyone not looking closely. Moves carefully, as though aware of being assessed.',
        N'Whatever the House provides. He has never chosen his own clothing. He understands this is not accidental.',
        N'none',
        N'Formal instruction in diplomatic protocol, House Serpens etiquette, and the languages expected of a political marriage. Afternoon social presentation exercises. Evenings — the only unscheduled hours he has — he spends in correspondence he encrypts himself, using a code he learned from his father''s papers.',
        N'He is in love with a Corps soldier named Piero Battaglia — a Myrmidon, untitled, of no political value. He has encrypted their correspondence himself. He believes that if this is discovered the marriage alliance with House Serpens fails and the treaty that follows may cost lives in the next campaign season. He decided six months ago that he will proceed with the marriage. He has not told anyone he made this decision.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Ophiuchus main seat; occasional formal visits to House Serpens for alliance assessment; no independent travel',
        N'0', N'0',
        N'young Italian noble teenager, slight build, black hair loose to shoulders, olive skin, dark eyes, formal House clothing that fits him like a costume, Mediterranean stone interior, expression of someone who has agreed to something permanent --ar 2:3',
        N'A 19-year-old Italian youth in formal House clothing, slight build, black hair loose to his shoulders, olive skin, dark eyes; standing in a stone corridor; the clothes fit perfectly and look borrowed; an expression of determined resignation.',
        0, 0
    );
END
ELSE PRINT 'Rinaldo Venn-Ophiuchus already exists.';
GO

-- 6. Lady Lucrezia Venn-Ophiuchus — The Dowager
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Lucrezia Venn-Ophiuchus')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Lucrezia Venn-Ophiuchus', N'lucrezia-venn-ophiuchus', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Lucrezia Venn-Ophiuchus', N'lucrezia-venn-ophiuchus', N'Lucrezia', N'Venn-Ophiuchus', N'Lady', N'human', N'human',
        N'female', N'she/her', 82, N'alive',
        N'Dowager of House Ophiuchus; last Venn-born before the merger; Orazio''s mother',
        N'Eighty-two years old and the last person alive who was born Venn before the merger formalized. She holds a memory of what was promised and what was delivered when the two Houses joined, and she has held it carefully for sixty-odd years. She receives ambassadors without telling Orazio first. She corresponds with people he does not know she corresponds with. She still uses her own seal — older than his, Venn-carved — and wears it openly. Orazio has never asked her to stop. He has considered it and declined. She has noticed this.',
        N'The institutional memory who holds the House''s most dangerous succession secret and uses the knowing of it as currency; her debt-management of Archivist Crispi is the invisible architecture beneath the House''s political legitimacy.',
        N'No POV.',
        N'House Ophiuchus (Venn branch); born Venn before the merger; the oldest living memory of the two Houses as separate entities',
        163, 52,
        N'slight, age-shrunk; the posture of someone who has decided the body will not argue with the mind''s opinion of itself',
        N'white', N'formally arranged, always', N'medium', N'dark brown (still sharp)', N'olive', N'deep-lined; the face of someone who has outlasted most of the people she has known',
        N'none',
        N'The stillness of someone who decided that stillness reads as authority. Moves slowly by necessity, deliberately by choice. Her silences are longer than other people''s silences and mean more.',
        N'Immaculate. She dresses as though the House''s dignity requires it and always has. Her seal is different from Orazio''s — older, Venn-carved — and she wears it openly.',
        N'none',
        N'Morning correspondence she handles herself, shares with no one. Receives a rotating set of visitors she does not formally announce. Afternoon rest she considers beneath her and takes anyway. Evening review of whatever Archivist Crispi sends her.',
        N'The Venn-Ophiuchus merger included a private succession clause — never recorded in the public treaty — guaranteeing that a named Venn lateral branch would hold the title if the Ophiuchus main line failed. The clause makes Marco''s claim legally contestable. Archivist Silvana Crispi found the genealogical record four years ago and buried it. Lucrezia knows Silvana buried it and said nothing. Silvana is now in her debt with no named terms, which is the point. Lucrezia has also known since shortly after Cosima''s death that three of the untranslated Archive texts contain Sphere 31 medical literature. She has chosen not to tell Orazio. She is not entirely sure why, and this uncertainty is itself something she has never named aloud.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Ophiuchus main seat, her own wing, which she has made quietly into a parallel court; does not travel',
        N'0', N'0',
        N'elderly Italian noblewoman, eighties, white formally arranged hair, sharp dark brown eyes, olive deeply-lined skin, immaculate formal House robes, older Venn-carved seal at her wrist, stone reception chamber, completely still, authority not infirmity --ar 2:3',
        N'An 82-year-old Italian noblewoman in immaculate formal robes, white formally arranged hair, sharp dark brown eyes, deep-lined olive skin; seated in a stone reception chamber with a different older seal at her wrist; still and formidably composed.',
        0, 0
    );
END
ELSE PRINT 'Lucrezia Venn-Ophiuchus already exists.';
GO

-- 7. Ser Corrado Ophiuchus — Cousin, Eastern Garrison Commander
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Corrado Ophiuchus')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Corrado Ophiuchus', N'corrado-ophiuchus', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Corrado Ophiuchus', N'corrado-ophiuchus', N'Corrado', N'Ophiuchus', N'Ser', N'human', N'human',
        N'male', N'he/him', 45, N'alive',
        N'Eastern Garrison Commander, House Ophiuchus; first cousin to Lord Orazio',
        N'Old Ophiuchus blood, not from the Venn merger branch. Commands the House''s eastern garrison — the most contested territory on the peninsula, where House Draco''s incursions are most frequent and the Scrying installations most vulnerable. His military record is solid and his loyalty to Lord Orazio is genuine. He has held the eastern garrison for twelve years without losing a major installation. He is forty-five, Knight-rank, broad across the shoulders in the way that Transmutation makes inevitable, and his garrison soldiers would follow him into a fire he had told them was a bad idea.',
        N'The cousin whose genuine loyalty coexists with an unauthorized Liturgy arrangement that will eventually force a choice between the House and his own strategic judgment.',
        N'No POV.',
        N'House Ophiuchus; old Ophiuchus blood, eastern peninsular territory; no Venn merger lineage',
        192, 102,
        N'heavily built; Knight-rank density; broad across the shoulders; the size of a man who has been transformed and lived on it for fourteen years',
        N'dark, close-cropped, silver-streaked at the temples', N'close-cropped', N'short', N'dark', N'olive', N'weathered; a scar at the jaw and one across the back of the left hand',
        N'Subtle height gain, increased density',
        N'Takes up space and does not apologize for it. Slow in formal settings, fast when needed — soldiers who have served under him say the two versions are hard to reconcile until you have seen both in the same day.',
        N'Garrison practical — heavy wool and leather, House colors as patches rather than full formal. Keeps formal presentation for the rare occasion Lord Orazio visits; otherwise dresses for the eastern Ridge.',
        N'First-stage Transmutation (Xerum 525); height and density increase, enhanced strength and endurance; survived first infusion at thirty-one',
        N'Dawn to the fortification wall. Morning briefings with his garrison captains. Midday inspection rotation. Correspondence in the evening — official through proper channels, a second set of letters through channels he has established himself.',
        N'He has been corresponding directly with the Liturgy — not through the House Liaison, not through the Chancellor, not with Orazio''s knowledge. The Liturgy offered him intelligence on House Draco''s eastern troop movements in exchange for first access to any Sphere 31 persons discovered within his garrison''s jurisdiction. He accepted. He tells himself it is strategic. He has handed over two people in the last four years who did not know what they were. He has not asked what happened to them. He suspects Spymaster Orsini knows about the arrangement. Neither has said anything to the other.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Eastern garrison fortifications; border zones between Ophiuchus and House Draco territory; occasional transit to the main seat',
        N'0', N'0',
        N'medieval Italian garrison commander, mid-forties, Knight rank, heavily built at 192cm, close-cropped silver-streaked dark hair, scarred jaw, olive skin, garrison practical military wool and leather in House Ophiuchus colors, eastern ridge fortification wall at dawn --ar 2:3',
        N'A 45-year-old Italian garrison commander, Knight rank, heavily built at 192cm, close-cropped silver-streaked dark hair, scarred jaw, olive skin; wearing practical military wool and leather on a ridge fortification wall at dawn; solitary, authoritative.',
        0, 0
    );
END
ELSE PRINT 'Corrado Ophiuchus already exists.';
GO

-- 8. Bianca Moretti-Ophiuchus — Cousin Who Married Out and Returned
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Bianca Moretti-Ophiuchus')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Bianca Moretti-Ophiuchus', N'bianca-moretti-ophiuchus', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Bianca Moretti-Ophiuchus', N'bianca-moretti-ophiuchus', N'Bianca', N'Moretti-Ophiuchus', N'', N'human', N'human',
        N'female', N'she/her', 39, N'alive',
        N'Diplomatic Adviser, House Ophiuchus; returned from House Serpens; widowed',
        N'Born Ophiuchus, from the Moretti branch — a minor merchant family with two generations of diplomatic service. Married into House Serpens fifteen years ago as part of an alliance arrangement. Her husband, a Serpens military officer, died in a border skirmish with House Draco eight years ago. She returned to Ophiuchus with their two children and a comprehensive understanding of Serpens'' internal politics, intelligence networks, and strategic priorities that the House found immediately useful and has never stopped mining. Her standing in the House is complicated: she is valuable and not fully trusted, which she has come to consider an accurate assessment of her situation.',
        N'The divided-loyalties figure whose Serpens intelligence contacts and inadvertent information-sharing make her simultaneously the House''s best asset and a potential vulnerability she has not fully disclosed.',
        N'No POV.',
        N'House Ophiuchus (born, Moretti branch); House Serpens (by marriage, now widowed); southern peninsula merchant lineage',
        170, 68,
        N'medium build; precise body language — nothing wasted, nothing offered accidentally',
        N'dark auburn', N'worn short', N'short', N'hazel', N'olive', N'clear, with early lines at the corners of the eyes',
        N'none',
        N'Moves with the careful social calibration of someone who knows exactly what she is not quite trusted with and has decided to be indispensable anyway. Occasionally reaches for Serpens-style formal choices in her posture and catches herself.',
        N'Dresses in Ophiuchus colors but with a Serpens-style cut she has not entirely abandoned. She knows it shows. She has not entirely stopped.',
        N'none',
        N'Briefings with Spymaster Orsini on Serpens intelligence matters. Formal advisory sessions with Chancellor Mancini. Time with her two children, who are being raised in the main household and who miss their father''s House in ways they express differently from each other.',
        N'She maintained contact with a Serpens intelligence officer for three years after returning to Ophiuchus — nominally for her children''s inheritance claims. She ended the contact last year when she realized the information she had been sharing, nothing she considered operationally sensitive, had been used to map Ophiuchus diplomatic correspondence patterns. She does not know what House Serpens did with the map. She has told no one in the House because admitting it would end her position and possibly her children''s standing. She is also genuinely uncertain whether Spymaster Orsini already knows, which makes the calculation of disclosure worse.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Ophiuchus main seat; limited diplomatic transit rights to House Serpens for her children''s inheritance matters',
        N'0', N'0',
        N'medieval Italian diplomatic woman, late thirties, dark auburn short hair, hazel eyes, olive skin, Ophiuchus formal colors with a trace of Serpens style, Mediterranean court setting, careful controlled expression --ar 2:3',
        N'A 39-year-old Italian woman in Ophiuchus formal colors with a Serpens cut to the collar; dark auburn hair worn short, hazel eyes, olive skin; standing in a formal reception chamber with precise controlled posture; expression of someone calculating what is safe to say.',
        0, 0
    );
END
ELSE PRINT 'Bianca Moretti-Ophiuchus already exists.';
GO

-- 9. Lord Bartolomeo Venn-Ophiuchus — Dead Family Member; Legacy
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Bartolomeo Venn-Ophiuchus')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Bartolomeo Venn-Ophiuchus', N'bartolomeo-venn-ophiuchus', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Bartolomeo Venn-Ophiuchus', N'bartolomeo-venn-ophiuchus', N'Bartolomeo', N'Venn-Ophiuchus', N'Lord', N'human', N'human',
        N'male', N'he/him', 49, N'dead',
        N'Former Heir to House Ophiuchus (deceased); would have been Lord; elder brother of Orazio',
        N'Died twenty-three years ago at forty-nine, holding the Vigil Seat installation against a House Draco incursion while Orazio''s forces withdrew on the assessment that the position was untenable. He held for eleven hours. Every officer who served under him in those eleven hours became the core of Dame Alessandra Torri''s fiercest loyalists — and none of them know that Torri was the one who called the position untenable. His name is on the wall of the Vigil Seat installation. He was Knight-rank, heavily built, dark-eyed, the kind of man that rooms rearranged themselves around. Surviving soldiers describe his stillness during those eleven hours as a decision, not a default. He would have been Lord. Orazio has been having a conversation with his absence for twenty-three years.',
        N'The ghost whose absence structures everything — the failure Orazio has spent twenty-three years explaining, the loyalty Torri has spent twenty-three years silently carrying, and the intelligence he sent that Orazio received and set aside.',
        N'No POV.',
        N'House Ophiuchus; southern peninsula; Venn-Ophiuchus main line; elder brother of Lord Orazio',
        190, 99,
        N'heavily built; Knight-rank density; the kind of man rooms rearranged themselves around',
        N'dark brown', N'kept short, military', N'short', N'dark brown', N'olive', N'weathered; alive at forty-nine in a way that looked permanent',
        N'Subtle height gain, increased density',
        N'Described by surviving soldiers as still in a way that was a decision, not a default. Present in a room before anyone acknowledged him there.',
        N'Military formal when required; garrison practical otherwise. He was not interested in what he wore.',
        N'First-stage Transmutation (Xerum 525); height and density increase; enhanced strength and endurance; survived first infusion at thirty-three',
        N'Deceased. In life: commanded the northern Ridge garrison alongside his formal role as heir. Kept his own counsel and wrote to Orazio three times a week. In the eleven hours at the Vigil Seat, organized a defense that held longer than the math said it should.',
        N'Three days before the Vigil Seat incursion, he sent Orazio documented evidence that House Draco''s senior leadership had been compromised by Liturgy influence — a finding that would have changed the political calculus of the engagement. Orazio received it and chose to act on Torri''s tactical assessment anyway. Bartolomeo died not knowing his brother had the document. The document still exists. Orazio has kept it for twenty-three years.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Deceased. In life: the Vigil Seat installation and northern Ridge country; the Ophiuchus main seat for formal occasions.',
        N'0', N'0',
        N'memorial portrait of a medieval Italian knight in his late forties, dark hair, olive skin, heavily built at 190cm, Knight rank formal military wear in House Ophiuchus colors, northern stone fortification background, expression of command and permanent purpose --ar 2:3',
        N'A memorial portrait of a medieval Italian Knight in his late forties. Dark hair, dark eyes, olive skin, heavily built at 190cm. Formal military attire in House Ophiuchus colors. Northern stone fortification behind him. The face of someone who has decided the room will come to him. Painted quality, dignified.',
        0, 0
    );
END
ELSE PRINT 'Bartolomeo Venn-Ophiuchus already exists.';
GO

-- ============================================================
-- POLITICAL CABINET
-- ============================================================

-- 10. Mistress Fiora Mancini — Chancellor
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Fiora Mancini')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Fiora Mancini', N'fiora-mancini', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Fiora Mancini', N'fiora-mancini', N'Fiora', N'Mancini', N'Mistress', N'human', N'human',
        N'female', N'she/her', 57, N'alive',
        N'Chancellor, House Ophiuchus; has served four Lords and Ladies',
        N'Has served as Chancellor through four Lords and Ladies. Her knowledge of every alliance, every coded correspondence format, and every dynastic grudge active in the Cauld is encyclopedic. She manages the House''s formal diplomatic pipeline — incoming and outgoing — with a precision that looks effortless and is not. She is fifty-seven, grey-streaked, and moves through the House like part of the architecture: predictable, load-bearing, noticed only when something goes wrong. She rarely speaks in a meeting until she is certain of the room. When she does speak she is precise and relentless and seldom wrong.',
        N'The institutional operator whose unauthorized editing of the Lord''s correspondence has made her functionally the co-author of House policy for eight years — which is either service or a constitutional usurpation she has never fully named to herself.',
        N'No POV.',
        N'House Ophiuchus; southern peninsula; born Mancini family, minor administrative lineage with two generations of chancellery service',
        165, 67,
        N'medium build; the economy of someone who has been doing this work for thirty years',
        N'dark, grey-streaked', N'severe knot', N'long (pinned)', N'dark', N'olive', N'fine-lined; precise',
        N'none',
        N'Moves through the House like part of the architecture — predictable, load-bearing. Rarely speaks in a meeting until she is certain of the room. When she does, she is precise.',
        N'House Ophiuchus colors, always, precisely. The clothes say: this is my office, not my personality. She has not been informal at work in eleven years.',
        N'none',
        N'Dawn to her correspondence desk. Morning council attendance. Afternoon management of the diplomatic pipeline. Evening review of everything she has not yet resolved. She has not taken a full day of rest in eleven years and considers this a reasonable trade.',
        N'She has been quietly rewriting Lord Orazio''s more aggressive diplomatic correspondence before it reaches its recipients — softening ultimatums, removing insults that would have started wars, correcting strategic overreach she could see and he could not. She has done this forty-seven times in eight years. Orazio believes his measured diplomatic reputation is his own character. She has never told him. She is not certain whether what she has done is service or a form of constitutional usurpation she will one day have to account for to someone.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Ophiuchus main seat; diplomatic transit as required by active negotiations',
        N'0', N'0',
        N'medieval Italian chancellor, late fifties, grey-streaked dark hair in severe knot, dark eyes, olive skin, precise House Ophiuchus formal colors, administrative office filled with sealed correspondence and treaty documents, controlled authoritative expression --ar 2:3',
        N'A 57-year-old Italian chancellor, grey-streaked dark hair in a severe knot, dark eyes, olive skin; wearing precise House Ophiuchus colors; at a desk covered in sealed correspondence and treaty texts; controlled, exact, carrying more authority than her title officially grants.',
        0, 0
    );
END
ELSE PRINT 'Fiora Mancini already exists.';
GO

-- 11. Master Giacomo Orsini — Spymaster
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Giacomo Orsini')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Giacomo Orsini', N'giacomo-orsini', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Giacomo Orsini', N'giacomo-orsini', N'Giacomo', N'Orsini', N'Master', N'human', N'human',
        N'male', N'he/him', 51, N'alive',
        N'Spymaster, House Ophiuchus',
        N'Runs the House intelligence network. Cool, quiet, present in every significant meeting and remembered afterward by almost no one — a skill he has cultivated deliberately over thirty years. He knows the shape of every threat to the House before the threat knows it is a threat. He has refused every formal office that would require a distinguishing uniform or insignia. He is fifty-one, medium-built, and has constructed his appearance to be nondescript in the specific way that requires effort to maintain.',
        N'The holder of the House''s most explosive operational secret — the five Sphere 31 takings — whose decision to maintain controlled ignorance may be the thing that protects the House or the thing that destroys it.',
        N'No POV.',
        N'House Ophiuchus; southern peninsula; Orsini family, intelligence tradition across three generations',
        175, 74,
        N'medium; deliberately unremarkable; nothing that catches the eye',
        N'dark, going grey', N'nondescript', N'short', N'brown', N'pale olive', N'clear; no distinguishing features, which is the point',
        N'none',
        N'Cultivated invisibility over thirty years. Is present at every significant meeting; remembered afterward by almost no one. This is not accident and it is not passive.',
        N'Nondescript neutral wool. Nothing that catches the eye. He has refused every formal office that would require distinguishing dress.',
        N'none',
        N'Appears at specific meetings and nowhere else. His actual work hours are unknown to his own staff. His correspondence routes are unmapped by anyone else in the House. He reads a great deal. He has no known social habits, which he considers information security.',
        N'He knows that five people currently in House Ophiuchus service are Sphere 31 takings. Three of them do not know what they are. One is Myrmidon Fiamma Rossi. He has decided that maintained ignorance is operationally preferable to disclosure — three unknowing Sphere 31 persons are more useful than three people managing the political and personal crisis of that knowledge. He has not informed Lord Orazio or the Liturgy Liaison. He has also chosen not to report Ser Corrado''s unauthorized arrangement with the Liturgy, because he does not yet know what he intends to do with it. He is aware the Chancellor is editing the Lord''s outgoing correspondence; he has not intervened.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'The House main seat and a network of contacts across every zone of the Cauld; physically present in Ophiuchus territory but operationally borderless',
        N'0', N'0',
        N'medieval Italian spymaster, early fifties, deliberately nondescript appearance, medium build, going-grey dark hair, pale olive skin, neutral wool, unremarkable corner of a stone corridor, present and unnoticed --ar 2:3',
        N'A 51-year-old man deliberately constructed to be forgettable — medium build, dark hair going grey, pale olive skin, brown eyes, nondescript neutral wool — standing in a stone corridor; he is present in the frame but the eye wants to slide past him; medieval setting.',
        0, 0
    );
END
ELSE PRINT 'Giacomo Orsini already exists.';
GO

-- 12. Mistress Silvana Crispi — House Archivist
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Silvana Crispi')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Silvana Crispi', N'silvana-crispi', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Silvana Crispi', N'silvana-crispi', N'Silvana', N'Crispi', N'Mistress', N'human', N'human',
        N'female', N'she/her', 63, N'alive',
        N'House Archivist, House Ophiuchus; forty years in the Archive',
        N'Forty years in the Archive. She knows where every treaty text, genealogical record, Scrying log, and private correspondence is filed. She knows which records have been removed, and by whom, and when. She has never discussed this with anyone unprompted. She is sixty-three, white-haired, small, and moves through the Archive with the confidence of someone navigating their own mind. People underestimate her because she is small and carries reference materials and never raises her voice. She has learned not to correct this impression.',
        N'The keeper of the buried succession clause who is now the Dowager''s instrument — and the keeper of Cosima''s uncatalogued translation notes, which constitute a second crisis in a different drawer.',
        N'No POV.',
        N'House Ophiuchus; southern peninsula; Crispi family, archival and scribal tradition for four generations',
        158, 54,
        N'small, unhurried; the quiet authority of someone who knows where everything is',
        N'white', N'practical coil', N'medium (pinned)', N'dark', N'olive', N'deep-lined; ink-stained hands',
        N'none',
        N'Moves through the Archive with the confidence of someone navigating their own mind. Unhurried everywhere else. Twists the ring on her right hand when deciding something.',
        N'Practical wool; archival apron over everything; ink stains she has stopped trying to prevent. A family-crest ring on her right hand.',
        N'none',
        N'Arrives in the Archive before dawn. Catalogs, cross-references, receives requests and denies some of them. Lunch at her desk. Responds to Lady Lucrezia''s evening queries before anything else from the day''s backlog.',
        N'Four years ago she found a genealogical record establishing an uneliminated succession clause from the Venn-Ophiuchus merger — a private provision making Marco''s claim to the title legally contestable. She buried the record. Lady Lucrezia knows she buried it and has said nothing; Silvana is now in the Dowager''s debt with no named terms, which she understands is the point. She also has, in a separate uncatalogued drawer, Cosima Venn-Ophiuchus''s private translation notes — notes that show Cosima translated more of the Sphere 31 medical texts than the official record reflects, including the texts that describe her own illness. She has told no one about the drawer.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'The House Archive and adjacent record rooms; the treaty chamber for formal consultations',
        N'0', N'0',
        N'elderly Italian archivist, sixties, white hair in practical coil, polished lens frames, olive ink-stained skin, practical wool with archival apron, surrounded by organized shelves of treaty texts and genealogical records, quietly authoritative --ar 2:3',
        N'A 63-year-old Italian archivist, small at 158cm, white hair in a practical coil, lens frames, olive skin with deep lines; standing in the Archive surrounded by organized treaty texts; ink-stained hands, family ring; the calm of someone who knows where everything is.',
        0, 0
    );
END
ELSE PRINT 'Silvana Crispi already exists.';
GO

-- 13. Master Sergio Dellatorre — Trade Ambassador
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Sergio Dellatorre')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Sergio Dellatorre', N'sergio-dellatorre', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Sergio Dellatorre', N'sergio-dellatorre', N'Sergio', N'Dellatorre', N'Master', N'human', N'human',
        N'male', N'he/him', 44, N'alive',
        N'Trade Ambassador, House Ophiuchus',
        N'Manages all commercial relationships for the House — border market arrangements, inter-House commodity agreements, the informal economies that keep the southern peninsula fed and supplied through the Living War. Charming in negotiations, precise in contracts, trusted by Lord Orazio. He is forty-four, dark-haired, slightly heavy-built in the way of a man who eats well because eating well is a negotiating tool. He moves through border markets and formal courts with identical calibrated ease and has learned to make everyone in a room feel like the most important person in it.',
        N'The functionary whose long-running quiet embezzlement is almost not the point — the emotional complication is that he genuinely likes Lord Orazio, and this has delayed him past every deadline he has set for himself.',
        N'No POV.',
        N'House Ophiuchus; southern peninsula; Dellatorre family, merchant class with two generations of diplomatic service',
        178, 85,
        N'slightly heavy-built; the body of a man who eats well because eating well is a negotiating tool',
        N'dark brown', N'well-maintained', N'short', N'dark brown', N'warm olive', N'clear, warm complexion; easy smile',
        N'none',
        N'Comfortable everywhere. Moves through border markets and formal courts with the same calibrated ease. Makes everyone feel like the most important person present.',
        N'Well-dressed always. Knows what each piece signals and chooses accordingly — border market practical for border markets, formal for formal occasions. The transitions are seamless.',
        N'none',
        N'Travel, primarily. When at the main seat: morning briefings on commodity flows and treaty conditions. Afternoon correspondence. Evenings managing relationships with visiting merchant representatives. Keeps a private ledger that does not go through the Treasurer.',
        N'For eleven years he has been skimming between 0.1% and 0.3% of every treaty payment and commodity agreement passing through his hands. The total is significant enough to fund a comfortable life in a border market city he has already identified, under a name he has already established. He has the route planned. He is waiting for one more large agreement to close before he disappears. He has told himself this for a year. The thing that keeps stalling him is that Lord Orazio trusted him with this role and has never, in eleven years, given him cause to regret being trusted. He has not been able to fully articulate to himself why this matters. He continues his preparations.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Border markets across the Cauld; other Houses'' commercial districts; Ophiuchus main seat when not traveling',
        N'0', N'0',
        N'medieval Italian trade diplomat, mid-forties, dark hair, warm olive skin, easy smile, well-chosen merchant-fine traveling wool, border market setting, charming and privately calculating --ar 2:3',
        N'A 44-year-old Italian trade diplomat, dark hair, olive skin, warm expression, wearing well-chosen traveling wool; in a border market surrounded by merchants; the smile reaches his eyes; behind it, precise calculation.',
        0, 0
    );
END
ELSE PRINT 'Sergio Dellatorre already exists.';
GO

-- 14. Mistress Beatrice Farro — Liturgy Liaison
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Beatrice Farro')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Beatrice Farro', N'beatrice-farro', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Beatrice Farro', N'beatrice-farro', N'Beatrice', N'Farro', N'Mistress', N'human', N'human',
        N'female', N'she/her', 46, N'alive',
        N'Liturgy Liaison to House Ophiuchus; reports to both; trusted by neither',
        N'Officially attached to House Ophiuchus by the Liturgy. Reports to both. Neither fully trusts her, which she has come to consider an accurate description of her position rather than a problem to be solved. She is forty-six, brown-haired, grey-eyed, and present in every formal House function in institutional grey she has never fully adapted to. She does not volunteer information — from either side — that has not been explicitly requested. This policy has kept her in this role for eight years without incident, which is longer than any Liturgy Liaison typically holds a posting.',
        N'The double-facing functionary whose unauthorized discovery about Sphere 31 transit targeting is the most dangerous piece of information anyone in the House is sitting on — and who cannot report it without being recalled and silenced.',
        N'No POV.',
        N'Liturgy (institutional); born southern peninsula, minor family; joined the Liturgy at nineteen',
        167, 64,
        N'medium build; deliberate; the practiced presence of someone who has learned to be in rooms without appearing to take notes',
        N'brown', N'cut practically short', N'short', N'grey', N'pale olive', N'clear; slightly washed-out from indoor work',
        N'none',
        N'Deliberate. Chooses words and movements with the same care. Present in every formal function without appearing to be processing everything she is, in fact, processing.',
        N'Institutional Liturgy grey, always, by requirement. Occasionally reaches for Ophiuchus colors in informal moments and catches herself. The reaching is involuntary; the catching is a trained reflex.',
        N'none',
        N'Morning formal report to the Liturgy through official channels, carefully worded. Afternoon engagement with House functions she attends on behalf of the Liturgy. Evening compilation of observations she has never formally reported, in a notation system she developed herself.',
        N'Over eight years she has accessed Liturgy transit records she had no institutional authorization to see. She is personally convinced that the takings from Sphere 31 are not random — that the Liturgy selects specific persons based on criteria she has partially decoded, involving physiological markers, proximity to Scrying installations, and what she suspects is a cognitive profile. She has compiled evidence in a private notation system. She has told no one because the moment she tells anyone, the Liturgy recalls her and the evidence disappears with her. She is trying to identify a recipient with the institutional weight to act on it and the motivation not to suppress it. She has not found one yet.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Ophiuchus main seat; occasional Liturgy administrative sites; nowhere without institutional sanction',
        N'0', N'0',
        N'medieval Italian liturgy representative, mid-forties, brown short hair, grey eyes, pale olive skin, institutional grey formal attire, stone administrative chamber, carefully neutral expression with something urgent being suppressed --ar 2:3',
        N'A 46-year-old woman in institutional Liturgy grey, brown hair cut practically short, grey eyes, pale olive skin; in a stone administrative chamber; expression of careful neutrality over something urgent she is not saying; medieval setting.',
        0, 0
    );
END
ELSE PRINT 'Beatrice Farro already exists.';
GO

-- 15. Master Fulvio Gentile — Treasurer and Chamberlain
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Fulvio Gentile')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Fulvio Gentile', N'fulvio-gentile', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Fulvio Gentile', N'fulvio-gentile', N'Fulvio', N'Gentile', N'Master', N'human', N'human',
        N'male', N'he/him', 55, N'alive',
        N'Treasurer and Chamberlain, House Ophiuchus',
        N'Manages House finances, household logistics, and the movement of materials during campaigns. Competent. Quiet. He has held the position for eighteen years and is widely considered reliable. He is fifty-five, slight, grey-haired, and has the look of someone who rarely sees sun — which is accurate, since he has spent most of the last eighteen years in the accounts chamber. He tracks what everything costs. He has three ink calluses on his right hand and has had them since he was thirty.',
        N'The institutional functionary sitting on a financial discrepancy that implicates either the Spymaster or an unknown third party — his years of silence have now made the silence itself dangerous.',
        N'No POV.',
        N'House Ophiuchus; southern peninsula; Gentile family, administrative tradition across three generations',
        172, 68,
        N'slight; the body of someone whose work has always been at a desk',
        N'grey', N'neat, unmemorable', N'short', N'dark', N'pale, for the region', N'fine-lined; ink calluses on three right-hand fingers',
        N'none',
        N'Quiet, compact, efficient. Makes himself part of the background of a room except when the numbers require him to speak, at which point he is precise and relentless.',
        N'Subdued House colors, practical, never extravagant. He tracks what clothes cost.',
        N'none',
        N'Numbers. All day. A private ledger review at dawn he does alone. Formal budget sessions when council requires them. A second private review in the evening — also alone — which contains entries the first ledger does not.',
        N'Eight years ago a significant sum was allocated from House accounts to an intelligence operation logged under Spymaster Orsini''s name. The operation does not appear in any intelligence record Fulvio can access. Orsini denies authorizing it. Fulvio has been carrying the discrepancy in a shadow ledger, balancing it annually against other entries so it does not surface in formal review. He does not know if the operation existed and Orsini is lying, or if someone used Orsini''s authority to access the funds. Either answer is dangerous. Asking the question is also dangerous. He has been silent long enough that the silence is now dangerous too. He has told no one.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Ophiuchus main seat; the treasury vaults; occasional border market visits for commodity price verification',
        N'0', N'0',
        N'medieval Italian treasurer, mid-fifties, grey hair, pale olive skin, ink-stained hands, subdued House Ophiuchus formal colors, stone treasury office with ledgers and account books, quietly burdened focused expression --ar 2:3',
        N'A 55-year-old Italian treasurer, grey hair, pale skin, ink calluses on three fingers, wearing subdued House colors; seated in a stone treasury office surrounded by ledger books; focused expression with something suppressed; medieval administrative setting.',
        0, 0
    );
END
ELSE PRINT 'Fulvio Gentile already exists.';
GO

-- 16. Elena Marchetti — Diplomat Stationed at House Draco
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Elena Marchetti')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Elena Marchetti', N'elena-marchetti', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Elena Marchetti', N'elena-marchetti', N'Elena', N'Marchetti', N'', N'human', N'human',
        N'female', N'she/her', 36, N'alive',
        N'House Diplomat, House Ophiuchus; stationed at House Draco; niece of Sergeant Aldo Marchetti',
        N'Currently stationed at House Draco''s diplomatic seat under formal credentials. Sharp, adaptable, trained in four languages and the etiquette of three Houses. Has been at this posting for three years. Her reports to Chancellor Mancini on House Draco''s political terrain are considered the best intelligence the House has on that front. She is thirty-six, dark-haired — dressed now in House Draco-appropriate formal styles for her posting — and her eyes go very still when she is processing something dangerous, which is a tell she has not managed to eliminate.',
        N'The diplomat abroad whose suppressed discovery represents the most urgent geopolitical intelligence in the Cauld — and whose inability to trust the House with it isolates her precisely when isolation is most dangerous.',
        N'No POV.',
        N'House Ophiuchus; southern peninsula; Marchetti family, minor administrative lineage; niece of Senior Sergeant Aldo Marchetti',
        166, 62,
        N'medium build; adapts posture to the room she is in; quick eyes that go very still when processing something difficult',
        N'dark brown', N'in House Draco-appropriate formal style for her posting', N'medium', N'dark brown', N'olive', N'clear',
        N'none',
        N'Adapts to the room she is in — a professional skill refined over three years in Draco''s political environment. Her natural posture, when she forgets to manage it, is watchful and still.',
        N'House Draco-appropriate formal presentation for her posting, with Ophiuchus credentials displayed as required. Three years of adaptation has left traces she notices when she looks in a mirror.',
        N'none',
        N'Formal diplomatic functions at House Draco''s seat. Intelligence gathering: official reports to Chancellor Mancini, and an encrypted secondary stream to Spymaster Orsini. A careful correspondence with her uncle Aldo that she has kept deliberately vague for six months.',
        N'Six months ago she intercepted — through a source she cannot name in writing — an internal Draco document suggesting they have developed or are close to developing two-way membrane transit: a mechanism to send persons TO Sphere 31, not merely receive them. She has not reported it. She does not know who in House Ophiuchus to trust with it; its implications would reshape every alliance in the Cauld and every House''s relationship with the Liturgy. She is also afraid: if House Draco learns she has this, her diplomatic position ends in a way that is not diplomatic. The one person she might trust is her uncle Aldo — a soldier with no political standing and no ability to act on what she would tell him. She has been considering this for six months and has not written it down.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'House Draco diplomatic seat; formal transit access within Draco territory; does not leave without escort by protocol',
        N'0', N'0',
        N'young Italian diplomat, mid-thirties, dark hair in formal House Draco style, dark brown eyes that go still, olive skin, formal diplomatic attire in Draco colors, stone formal chamber, carefully composed expression with something urgent beneath it --ar 2:3',
        N'A 36-year-old Italian woman in formal diplomatic attire styled for House Draco, dark hair in Draco-appropriate formal presentation, dark brown eyes that go very still, olive skin; in a stone diplomatic chamber; composed expression over suppressed urgency; medieval setting.',
        0, 0
    );
END
ELSE PRINT 'Elena Marchetti already exists.';
GO

-- ============================================================
-- MILITARY COMMAND
-- ============================================================

-- 17. Dame Alessandra Torri — Commander of the Myrmidon Corps (Paladin)
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Alessandra Torri')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Alessandra Torri', N'alessandra-torri', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Alessandra Torri', N'alessandra-torri', N'Alessandra', N'Torri', N'Dame', N'human', N'human',
        N'female', N'she/her', 52, N'alive',
        N'Commander of the Myrmidon Corps, House Ophiuchus; Paladin rank',
        N'Paladin rank. Oversees all military operations for House Ophiuchus. Has Lord Orazio''s full confidence and the Corps'' loyalty — the latter built in part on the soldiers'' knowledge that she was at the Vigil Seat. What they do not know is what she told Orazio before the retreat. She is fifty-two, significantly transformed by Transmutation, and carries the particular care in movement that very large, very strong people develop when they have spent decades being careful about proximity.',
        N'The Commander whose strategic assessment twenty-three years ago set the shape of everything that followed — and who has let Orazio carry the public weight of it ever since, which is either loyalty or cowardice and she has not determined which.',
        N'No POV.',
        N'House Ophiuchus; southern peninsula; Torri family, military lineage across three generations',
        198, 112,
        N'significantly altered by Paladin-rank Transmutation; heavier through the shoulders; restructured proportions; deliberate, contained movement',
        N'dark, silver-streaked', N'cropped close', N'short', N'flat amber (changed from dark brown)', N'olive', N'weathered; the face of someone who has been making decisions with bodies in them for thirty years',
        N'Evident enhancement — significant height, altered proportions, changed eyes',
        N'The deliberate, contained movement of someone very large and very strong who has spent decades being careful about proximity. Takes up the room accurately, not aggressively.',
        N'Formal Corps military attire, always, precisely. Ophiuchus colors and rank insignia exact. Never informal when visible to soldiers.',
        N'Second-stage Transmutation (Xerum 525); significantly altered physiology — height increase, restructured proportions, enhanced strength beyond human norms, altered eye pigmentation (dark brown to flat amber); survived both infusions',
        N'Dawn review of all Corps operational dispatches. Morning tactical council with her captains. Midday available for Lord Orazio. Afternoon to the training ground — she still trains, alone, before the rest of the Corps arrives. Evening writing reports she has never sent.',
        N'She told Lord Orazio the Vigil Seat position was untenable. Bartolomeo Venn-Ophiuchus died because she said those words and he accepted them. Orazio has carried the blame publicly for twenty-three years. She has let him. The Corps'' loyalty is built partly on the belief that she was there trying to save the Vigil Seat. She was there. She was doing the math. She knows Sergeant Aldo Marchetti was in the room when she told Orazio. She has never spoken to him about it. She does not know whether Marco''s private approach to her is an opportunity or a repetition of the moment she has spent twenty-three years managing.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Ophiuchus main seat; all active Corps installations; field command as required',
        N'0', N'0',
        N'medieval Italian Paladin-rank Dame commander, early fifties, significantly augmented frame at 198cm, silver-streaked dark hair cropped close, flat amber eyes, formal Ophiuchus Corps military attire, stone command chamber, contained immense authority --ar 2:3',
        N'A 52-year-old Italian woman, Paladin rank, 198cm with significantly restructured proportions and flat amber eyes formerly dark brown; silver-streaked dark hair cropped close; formal Corps military attire in House Ophiuchus colors; standing in a stone command chamber; authority that fills the room without effort.',
        0, 0
    );
END
ELSE PRINT 'Alessandra Torri already exists.';
GO

-- 18. Ser Dante Esposito — First Captain, Ground Operations
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Dante Esposito')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Dante Esposito', N'dante-esposito', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Dante Esposito', N'dante-esposito', N'Dante', N'Esposito', N'Ser', N'human', N'human',
        N'male', N'he/him', 41, N'alive',
        N'First Captain, Myrmidon Corps, House Ophiuchus; ground operations commander',
        N'Ground operations commander. Brilliant tactically in a way that looks like instinct — it is not instinct, it is a compulsive study of terrain, enemy movement, and Corps capability that he conducts privately and does not discuss. The Commander treats war as an analytical problem. Dante treats it as a conversation. Their disagreements have never been resolved in the field; they have been managed, which both of them know is not the same thing. He is forty-one, Knight-rank, and goes entirely still when thinking, which unnerves people who do not know him and reassures people who do.',
        N'The tactical mind whose analysis of installation defense casualties is a slow-burning institutional crisis — and whose personality difference from the Commander is the Corps'' fault line.',
        N'No POV.',
        N'House Ophiuchus; southern peninsula; Esposito family, Corps tradition for three generations',
        189, 94,
        N'athletic; Knight-rank density; moves fast and precisely',
        N'dark brown', N'short, field-practical', N'short', N'dark brown (unchanged)', N'olive', N'clear; a few small scars at the jaw',
        N'Subtle height gain, increased density',
        N'Fast-moving, economical. Goes entirely still when thinking, which unnerves people who don''t know him. In conversation he has the quality of someone who has already processed the next three options.',
        N'Corps field practical, always. Has formal attire; wears it reluctantly, precisely, and removes it the moment protocol allows.',
        N'First-stage Transmutation (Xerum 525); height and density increase, enhanced strength and endurance; survived first infusion at thirty-two',
        N'In the field or preparing to be. When at the main seat: dawn to the training ground, morning briefings, afternoons reviewing Corps casualty and engagement records he has been maintaining separately from official logs for four years.',
        N'He has been documenting casualty patterns around Scrying installation defense missions for four years. The numbers show the Corps loses proportionally more soldiers to installation defense than to open field engagement — that protecting the installations costs more than losing them would. He believes the House has been treating the installations as symbols rather than assets and paying for it in bodies. He is planning to present this analysis to Lord Orazio directly, bypassing Commander Torri. He has not decided when. He is aware the Commander will view this as a challenge to her strategic authority, which it is, and that she will be right.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Active field operations; all Ophiuchus military zones; wherever the Corps deploys',
        N'0', N'0',
        N'medieval Italian knight captain, early forties, athletic at 189cm, dark hair short, dark brown eyes, olive skin, Corps field military attire in House Ophiuchus colors, field fortification or training ground, intense focused energy --ar 2:3',
        N'A 41-year-old Italian knight captain, 189cm, athletic, dark hair, dark eyes, olive skin; wearing Corps field military attire; at a field fortification; the posture of someone who is always two steps ahead and finds waiting physically uncomfortable.',
        0, 0
    );
END
ELSE PRINT 'Dante Esposito already exists.';
GO

-- 19. Dame Caterina Ferrara — Second Captain, Garrison and Installation Defense
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Caterina Ferrara')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Caterina Ferrara', N'caterina-ferrara', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Caterina Ferrara', N'caterina-ferrara', N'Caterina', N'Ferrara', N'Dame', N'human', N'human',
        N'female', N'she/her', 38, N'alive',
        N'Second Captain, Myrmidon Corps, House Ophiuchus; garrison and installation defense',
        N'Garrison and installation defense commander. She oversees the Corps when others march — the one who stays, maintains, coordinates logistics and replacements. Methodical where First Captain Esposito is instinctive; her relationship with him is a persistent low-grade friction that has never become open conflict and probably won''t, because both of them understand they need each other''s capabilities. She is thirty-eight, Knight-rank, auburn-haired, and her eyes changed with the Transmutation from brown to blue-grey, a shift she still occasionally notices in reflections.',
        N'The steadfast second whose divided private correspondence introduces the possibility that the garrison''s information security has been compromised in the most human possible way.',
        N'No POV.',
        N'House Ophiuchus; southern peninsula; Ferrara family, administrative and minor military tradition',
        184, 85,
        N'strong, methodical build; Knight-rank density; the body of someone who maintains herself the way she maintains the garrison',
        N'auburn', N'cut short for garrison practicality', N'short', N'blue-grey (changed from brown)', N'fair olive', N'clear; a scar at the jaw and forearm',
        N'Subtle height gain, increased density',
        N'Methodical. Checks the room before entering, the exits before sitting, the equipment before using it. The Corps under her is extremely well-supplied and well-maintained. She is the person who notices when something is missing before anyone else.',
        N'Corps garrison uniform maintained to a personal standard. Inspects her own kit the way she inspects everyone else''s: honestly.',
        N'First-stage Transmutation (Xerum 525); height and density increase; altered eye color (brown to blue-grey); enhanced endurance; survived first infusion at thirty',
        N'Dawn garrison perimeter inspection. Morning briefings with her installation commanders. Afternoon logistics and maintenance review. A private correspondence she encrypts herself that does not go through official channels.',
        N'She has been exchanging letters for two years with a soldier in House Serpens — a Knight named Aurel Kessler, whom she met during a border-market ceasefire negotiation. She believes this is a personal correspondence. She has not examined carefully enough whether Kessler''s questions, accumulated over two years, have been more systematically oriented toward Ophiuchus garrison dispositions than she would have noticed if she were not in love with him. She is aware that if Spymaster Orsini learns of this correspondence, her career ends. She is not entirely certain she cares, which is what she is most afraid of.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Ophiuchus garrison installations; the main seat during formal Corps councils; installation defense zones',
        N'0', N'0',
        N'medieval Italian woman knight dame, late thirties, auburn hair cut short, blue-grey eyes, fair olive skin, Corps garrison uniform in House Ophiuchus colors, stone garrison installation interior, methodical composed expression with something personal being held back --ar 2:3',
        N'A 38-year-old Italian woman knight, 184cm, strong build, auburn hair cut short, blue-grey eyes (changed), fair olive skin; wearing precise Corps garrison uniform; inside a stone installation; methodical expression with personal feeling being contained.',
        0, 0
    );
END
ELSE PRINT 'Caterina Ferrara already exists.';
GO

-- 20. Master Lorenzo Angioli — Infirmary Commander
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Lorenzo Angioli')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Lorenzo Angioli', N'lorenzo-angioli', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Lorenzo Angioli', N'lorenzo-angioli', N'Lorenzo', N'Angioli', N'Master', N'human', N'human',
        N'male', N'he/him', 48, N'alive',
        N'Infirmary Commander, Myrmidon Corps, House Ophiuchus',
        N'The physician who has kept soldiers alive through things they should not have survived — Transmutation failures, combat wounds, the long infections that follow installation sieges. He has been Infirmary Commander for fourteen years. He is respected by the Corps in the specific way that people respect someone they are afraid of needing. He is forty-eight, dark-curly-haired going grey, olive-skinned, with chemical and ink stains on his hands that do not come entirely off. He did not undergo Transmutation — he is a physician, not a soldier — and moves with the unhurried economy of someone who has learned that rushing in proximity to the dying costs lives.',
        N'The witness who has built a documented case against institutional Transmutation death-rate management; his ledger, in the right hands, is a reckoning; in the wrong hands, suppression.',
        N'No POV.',
        N'House Ophiuchus; southern peninsula; Angioli family, apothecary and physician tradition',
        173, 70,
        N'medium build; untransformed; the unhurried economy of someone who has learned rushing in proximity to the dying costs lives',
        N'dark brown, curly, going grey', N'kept back from his face', N'short', N'dark brown', N'olive', N'clear; chemical and ink stains on his hands that don''t come entirely off',
        N'none',
        N'Unhurried. The practiced stillness of someone who has learned that rushing near the ill and the dying costs people their lives. Listens more than he speaks in any context.',
        N'Physician''s practical: undyed linen over dark wool, always clean even in field conditions. Corps rank insignia worn because required, positioned precisely because precision is how he thinks.',
        N'none',
        N'Dawn rounds through the infirmary. Morning review of recovery cases and pending procedures. Afternoons — when not managing casualties — in his study with the private ledger. The ledger review he does last, alone, after everything else is finished.',
        N'Over fourteen years he has maintained a private ledger — separate from official Corps records — recording every death on his table, including the Transmutation rejection deaths officially classified as battle casualties. The ledger names sixty-one people. He has written each name himself. He is building a case against the Corps'' practice of concealing Transmutation rejection rates to avoid discouraging enlistment — a practice he believes has led candidates to undergo infusion without accurate survival information. He does not know who to bring it to. He knows whoever receives the ledger will face a choice between acting on it and suppressing it. He is trying to determine which outcome is more likely before he decides who to trust.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Ophiuchus main seat infirmary; field infirmary deployments during campaigns; Transmutation preparation chambers',
        N'0', N'0',
        N'medieval Italian physician, late forties, dark curly hair going grey, olive skin, chemical-stained hands, practical undyed linen and dark wool with Corps insignia, stone infirmary with medical apparatus and private ledger, quietly determined expression --ar 2:3',
        N'A 48-year-old Italian physician, medium build, dark curly hair going grey, olive skin, chemical-stained hands; wearing practical undyed linen and dark wool with Corps insignia; in a stone infirmary; an open private ledger beside him; expression of someone carrying information that has weight.',
        0, 0
    );
END
ELSE PRINT 'Lorenzo Angioli already exists.';
GO

-- 21. Aldo Marchetti — Senior Sergeant, Institutional Memory
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Aldo Marchetti')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Aldo Marchetti', N'aldo-marchetti', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Aldo Marchetti', N'aldo-marchetti', N'Aldo', N'Marchetti', N'', N'human', N'human',
        N'male', N'he/him', 56, N'alive',
        N'Senior Sergeant, Myrmidon Corps, House Ophiuchus; thirty-one years of service; uncle of Elena Marchetti',
        N'Thirty-one years in the Corps. Has served under four First Captains and every significant campaign the House has run since the Vigil Seat. He knows every officer''s actual record versus their official one and has the discretion not to conflate them. He is fifty-six, dense and worn in the way of someone who has spent three decades in physical service, dark-skinned, close-cropped grey, and his eyes show what they have seen only when they choose to. He is Elena Marchetti''s uncle. He writes to her carefully.',
        N'The institutional memory whose specific, witnessed knowledge is the structural secret of the House — and whose silence, maintained for twenty-three years across several possible motives, is the load-bearing wall.',
        N'No POV.',
        N'House Ophiuchus; southern peninsula; Marchetti family, working lineage; uncle of diplomat Elena Marchetti',
        182, 89,
        N'dense and worn; thirty-one years of service visible in how the body holds itself',
        N'dark grey (close-cropped)', N'close-cropped', N'short', N'dark brown', N'dark', N'weathered; the face of someone who has decided what to show and what not to',
        N'none',
        N'Still when standing, deliberate when moving. Economy of effort built over thirty-one years. In the field becomes a different person — faster, sharper; soldiers notice the transition.',
        N'Corps sergeant''s practical uniform, maintained with the precision of someone who respects the institution even when he is uncertain about its officers.',
        N'none',
        N'Dawn briefings with the junior soldiers of his unit. Mornings running training exercises and equipment checks. Afternoons with the officers he has served long enough to outlast three times over. Evenings, occasionally, a letter to Elena that he keeps deliberately vague.',
        N'He was in the room when Dame Alessandra Torri told Lord Orazio that the Vigil Seat position was untenable. He heard the words. He watched Orazio accept the assessment. Bartolomeo died on that calculation. He has kept this for twenty-three years — watched Orazio carry the blame publicly and said nothing. He does not know if he is protecting the Commander, protecting the Lord, or protecting the institution, and has stopped trying to determine which. He also knows that his niece Elena''s letters have been deliberately vague for six months in a way she is probably unaware of, and that whatever she is not writing to him may be the most important thing currently happening. He does not know how to signal to her that he would be safe to tell.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Ophiuchus main seat and active deployment zones; wherever the Corps is deployed',
        N'0', N'0',
        N'medieval veteran soldier sergeant, mid-fifties, dark skin, close-cropped grey hair, dark eyes, dense thirty-year service build, Corps sergeant''s practical uniform, stone garrison interior, expression of someone who has seen too much and chosen what to carry --ar 2:3',
        N'A 56-year-old veteran soldier, 182cm, dense build from thirty-one years of service, dark skin, close-cropped grey hair, dark steady eyes; wearing Corps sergeant''s practical uniform; in a stone garrison interior; the expression of someone who knows something and decided long ago not to say it.',
        0, 0
    );
END
ELSE PRINT 'Aldo Marchetti already exists.';
GO

-- 22. Ser Pietro Mazzini — Veteran Knight Near Retirement
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Pietro Mazzini')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Pietro Mazzini', N'pietro-mazzini', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Pietro Mazzini', N'pietro-mazzini', N'Pietro', N'Mazzini', N'Ser', N'human', N'human',
        N'male', N'he/him', 49, N'alive',
        N'Senior Knight, Myrmidon Corps, House Ophiuchus; twenty-one years at Knight rank; near retirement',
        N'Knight for twenty-one years. He tells anyone who asks that when he retires he plans to open a small school for children in a southern peninsula market town — he has the location chosen, the building price estimated, fifteen years of savings earmarked. He has a name for the school. He has thought about the kind of person he will be when he is no longer a soldier, and the thinking has been specific and serious. He is forty-nine, heavily built by Transmutation, greying, scarred at the forearms. He carries out assignments without supervision, trains junior soldiers without being asked, and is considered by the Corps to be exactly what he appears to be.',
        N'The veteran whose unreported war crime runs below everything he does in the years since as private penance — and whose planned retirement to innocence is built on a foundation he has not disclosed.',
        N'No POV.',
        N'House Ophiuchus; southern peninsula; Mazzini family, working lineage',
        191, 105,
        N'heavily built; Knight-rank density; the body of someone who has been in physical conflict for two decades and shows it',
        N'dark brown, heavily greyed', N'short, unmaintained', N'short', N'brown (unchanged)', N'olive', N'weathered; visible scars on both forearms and one at the left temple',
        N'Subtle height gain, increased density',
        N'Solid. Unhurried outside engagement, effective inside it. The movement of someone who knows exactly how much force they have and has stopped needing to prove it.',
        N'Corps uniform maintained but not polished. The uniform of someone who wears it because it is the uniform, not because of what it signals.',
        N'First-stage Transmutation (Xerum 525); height and density increase; enhanced strength and endurance; survived first infusion at twenty-eight',
        N'Carries out assignments without requiring supervision. Trains junior soldiers without being asked. Has spent the last year quietly mapping the southern peninsula market town he has chosen — distance to the nearest market, what the local families do for work, how many students there might realistically be.',
        N'Eight years ago, during a skirmish near a border installation, he executed a prisoner — a House Draco soldier who had surrendered and was in the process of destroying Scrying equipment. He acted without orders. Under the Liturgy''s codes governing prisoners of war it is a capital offense. He has never reported it. The two soldiers who witnessed it were killed in the next engagement. The school is, among other things, a penance he assigned himself: specific enough to mean something, private enough not to require confession.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Wherever the Corps deploys; the southern peninsula market town he visits on rest periods',
        N'0', N'0',
        N'medieval Italian veteran knight, late forties, heavyset at 191cm, greying dark hair, brown eyes, scarred forearms, Corps uniform worn practical not formal, southern peninsula market town visible through a stone window, expression of someone counting the days to something quiet --ar 2:3',
        N'A 49-year-old Italian veteran knight, 191cm, heavily built, greying dark hair, brown eyes, scarred forearms; wearing Corps uniform without ceremony; a southern peninsula town visible through a stone window; expression of someone who has decided on something quiet and is counting the days.',
        0, 0
    );
END
ELSE PRINT 'Pietro Mazzini already exists.';
GO

-- 23. Dame Fiamma Rossi — Junior Officer Recently Distinguished; Sphere 31 Taking (Unknown to Her)
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Fiamma Rossi')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Fiamma Rossi', N'fiamma-rossi', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Fiamma Rossi', N'fiamma-rossi', N'Fiamma', N'Rossi', N'Dame', N'human', N'human',
        N'female', N'she/her', 24, N'alive',
        N'Junior Officer (newly elevated Dame), Myrmidon Corps, House Ophiuchus; recently distinguished in field',
        N'Recently distinguished during a northern border engagement — held a position for six hours with twelve soldiers against a force four times larger, losing two. The Corps is watching her. She completed her first Transmutation infusion last month and survived, the early changes beginning. She is twenty-four, red-brown-haired — which has drawn comment her entire life in a region where that coloring is unusual — and is now aware that people are watching her and has not yet decided how to wear this.',
        N'The junior officer whose Sphere 31 origin — unknown to her — is the intersection point of several suppressed intelligence streams; her implausible infusion survival may surface what Spymaster Orsini has been sitting on.',
        N'No POV.',
        N'House Ophiuchus; raised in northern Ridge country; origin records incomplete from her first decade; Rossi family (adoptive)',
        168, 64,
        N'lean, combat-quick; early post-infusion changes beginning; the posture of someone recently aware of being watched',
        N'red-brown (unusual for the region)', N'practical, off the face', N'short', N'dark brown', N'olive', N'clear',
        N'Subtle height gain, increased density',
        N'Combat-quick — the movement economy of two years of field service. In formal settings has not yet learned how to hold still correctly; it reads as readiness, not discomfort.',
        N'Corps uniform adapted for field operations. Has not yet updated the rank insignia to reflect her new elevation. A Corpsman pointed this out. She said she would do it.',
        N'First-stage Transmutation (Xerum 525), very recent (last month); early-stage height increase and density changes in progress; full transformation continuing',
        N'Until last month: standard field rotation. Since the engagement and infusion: briefings she has not been told the purpose of, physical assessments by the Corps Practitioner she has been told are routine. She is aware they are not routine. She has not asked anyone directly.',
        N'She does not know what she is. Spymaster Orsini has reviewed Liturgy transit logs suggesting she is a Sphere 31 taking — a person transported from the other side who grew up in the Cauld without knowing her origin. She has had since childhood a recurring sensation she describes privately as recognizing mechanisms and apparatus she has never encountered, particularly Scrying installation equipment. She survived the Transmutation infusion at a rate the Corps Practitioner privately considers statistically improbable. She has attributed all of this to good instincts. She has not yet met anyone who would tell her otherwise.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Northern border zones (former posting); Ophiuchus main seat, recently relocated for assessment',
        N'0', N'0',
        N'young Italian woman knight dame, early twenties, red-brown hair unusual for region, dark eyes, olive skin, Corps uniform with new insignia not yet updated, northern ridge stone background, expression of someone being assessed and deciding how to respond --ar 2:3',
        N'A 24-year-old Italian woman, recently elevated to Dame rank, red-brown hair unusual for the region, dark eyes, olive skin; in Corps uniform with new insignia not yet added; against a northern ridge stone background; expression of someone who knows she is being watched and hasn''t decided what to do about it.',
        0, 0
    );
END
ELSE PRINT 'Fiamma Rossi already exists.';
GO

-- 24. Dame Orsolina Verdi — Corps Transmutation Practitioner
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Orsolina Verdi')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Orsolina Verdi', N'orsolina-verdi', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (Id, Name, Slug, FirstName, LastName, TitlePrefix, Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role, Description, NarrativeFunction, NarrationVoice, Heritage, HeightCm, WeightKg, Build, HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion, VisibleAugmentations, PostureMovement, PhysicalClothingStyle, Augmentations, DailyLife, PsychologySecret, SpeechVocabulary, SpeechCadence, SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister, TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @id, N'Orsolina Verdi', N'orsolina-verdi', N'Orsolina', N'Verdi', N'Dame', N'human', N'human',
        N'female', N'she/her', 44, N'alive',
        N'Corps Transmutation Practitioner, Myrmidon Corps, House Ophiuchus; Dame rank',
        N'Administers Transmutation infusions to Corps candidates before campaigns. She achieved Knight rank through the infusion herself — she underwent it to understand what she asks of others, which the Corps found eccentric and then found useful. She has given last rites to twenty-three people who died on her table. She does not discuss this number. She is forty-four, medium-built, dark-haired, and moves with the unhurried deliberateness of someone who has learned that the quality of stillness before an infusion affects candidate outcomes. Her eyes changed with the Transmutation from warm brown to flat black. She has not entirely stopped finding this strange.',
        N'The practitioner whose undisclosed predictive ability represents a secret that would transform — and potentially corrupt — the entire Transmutation system if disclosed; her assessment of Fiamma Rossi is her highest-confidence prediction in fifteen years.',
        N'No POV.',
        N'House Ophiuchus; southern peninsula; Verdi family, apothecary and physician lineage',
        181, 78,
        N'medium build; Knight-rank density; unhurried in all movement',
        N'dark brown', N'worn back, always', N'medium (pinned)', N'flat black (changed from warm brown)', N'olive', N'clear; the stillness of someone who has learned what rushing costs',
        N'Subtle height gain, increased density',
        N'Unhurried everywhere, always. Has learned that the quality of her stillness before an infusion affects candidate outcomes. Is never rushed. Is never uncertain-appearing, regardless of what she is actually certain about.',
        N'Practitioner''s formal: House Ophiuchus colors, formally cut, kept immaculate. The role requires presence; she provides it.',
        N'First-stage Transmutation (Xerum 525); underwent infusion to understand what she asks of candidates; height and density increase; altered eye color (warm brown to flat black); enhanced endurance',
        N'Candidate assessment — her term for what others call medical examination. Campaign preparation briefings. Ongoing research into Transmutation physiology through practitioner correspondence kept vague about her specific findings. Her private physiological assessment records are not filed with official Corps medical records.',
        N'Fifteen years of administering infusions have given her the ability to predict with approximately seventy percent accuracy which candidates will survive the first infusion, based on physiological markers — specific bone density ratios, nervous system response patterns, a quality of the sclera she has never fully articulated in writing. She has never written it down. She has never told anyone. She selects candidates based on her assessment and says nothing about her method. If the Corps knew this was possible it would destroy the meritocratic mythology the institution runs on and allow the House to restrict Transmutation to politically preferred candidates. She will not allow this. She is also aware that her assessment of Dame Fiamma Rossi was the highest-confidence prediction she has made in fifteen years, and that Fiamma''s survival means something about Fiamma''s physiology she cannot yet name.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Ophiuchus main seat and all Transmutation preparation chambers; field deployment before major campaigns',
        N'0', N'0',
        N'medieval Italian woman knight practitioner, early forties, dark hair worn back, flat black eyes, olive skin, formal Ophiuchus practitioner attire, stone preparation chamber with alchemical apparatus, unhurried authoritative expression --ar 2:3',
        N'A 44-year-old Italian woman, Dame rank, 181cm, dark hair worn back, flat black eyes (changed from brown), olive skin; wearing formal House Ophiuchus practitioner''s attire; in a stone preparation chamber with alchemical apparatus; unhurried, completely self-possessed.',
        0, 0
    );
END
ELSE PRINT 'Orsolina Verdi already exists.';
GO
