SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ============================================================
-- HOUSE OPHIUCHUS — BROAD POPULATION SEED (PART A)
-- The Cauld | UniverseId: 0197E9C9-0002-7000-8000-000000000002
-- Generated: 2026-07-05
-- Active population: soldiers, membrane researchers, Transmutation
-- practitioners and trainees, Sphere archivists, intelligence
-- analysts, young researchers — 15 total
-- ============================================================

-- 1. Marta Segni — Membrane Researcher, Vigil Seat
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Marta Segni')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Marta Segni', N'marta-segni', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Marta Segni', N'marta-segni', N'Marta', N'Segni', N'Mistress',
        N'human', N'human', N'female', N'she/her', 35, N'alive',
        N'Membrane researcher, Vigil Seat installation, House Ophiuchus; six years at the primary Scrying site',
        N'Ophiuchus membrane researcher, six years at the Vigil Seat. Most cited active researcher in the House on membrane topology. Methodical, publication-hungry, precise in notation. Her colleagues consider her indispensable. She considers indispensability useful cover.',
        N'The researcher whose falsified data conceals the most important finding in Ophiuchus science — and who is racing to control how it enters the world before the Corps takes it from her.',
        N'No POV.',
        N'House Ophiuchus; Vigil Seat installation, northern Ridge country',
        163, 61,
        N'medium; desk-scholar''s economy; moves quickly between stations',
        N'dark brown', N'tied back when working, always', N'medium', N'dark brown', N'warm olive', N'clear; fine ink calluses on two fingers',
        N'none',
        N'Quick between stations, still when recording. The economy of someone who has spent six years at the same installation learning exactly how much each movement costs.',
        N'Scholar''s practical wool; House Ophiuchus research insignia at the collar; ink stains she stopped trying to prevent in year three.',
        N'none',
        N'Dawn to the Vigil Seat observation gallery. Morning Scrying sessions logged and reviewed. Afternoons writing papers; evenings reviewing the private records she does not submit for publication.',
        N'Her three most-cited papers include a core dataset she constructed by interpolating between actual readings rather than recording them. The real numbers show the membrane responds measurably to proximity of Transmutation-enhanced bodies — a finding she falsified because she believes the Corps would have weaponized it before she could publish with appropriate safeguards. She has been intending to correct the record for two years. The distance between her intention and her action has been growing.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Vigil Seat installation and Ophiuchus main seat scholarly division; occasional transit to inter-House research sessions',
        N'0', N'0',
        N'medieval Italian scholar-woman, mid-thirties, dark hair tied back, Vigil Seat Scrying observation gallery, manuscripts and aperture equipment, candlelight, precise expression --ar 2:3',
        N'A 35-year-old Italian woman researcher in a Scrying observation gallery, dark hair tied back, ink-stained fingers, precise focused expression, medieval scholarly setting.',
        0, 0
    );
    PRINT 'Marta Segni seeded.';
END
ELSE PRINT 'Marta Segni already exists.';
GO

-- 2. Luca Ferri — Transmutation Trainee / Myrmidon Candidate
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Luca Ferri')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Luca Ferri', N'luca-ferri', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Luca Ferri', N'luca-ferri', N'Luca', N'Ferri', N'',
        N'human', N'human', N'male', N'he/him', 22, N'alive',
        N'Myrmidon candidate, House Ophiuchus; first Transmutation infusion scheduled in thirty days',
        N'Myrmidon candidate, first Transmutation infusion scheduled in thirty days. Looks like a soldier. Studies like a scholar — which is exactly what he is, and what the Corps has not realized.',
        N'The covert scholar whose Transmutation candidacy is entirely tactical — and whose death would constitute an institutional critique no one in the House would know how to hear.',
        N'No POV.',
        N'House Ophiuchus; main seat Corps training grounds; born southern peninsula, Ferri family, working lineage',
        177, 75,
        N'lean, young; not a soldier''s body yet but learning to move like one',
        N'black', N'short, field-practical', N'short', N'brown', N'olive', N'clear',
        N'none',
        N'Fits into the Corps training environment without drawing attention to the effort it takes. Keeps scholarly materials under his bunk, reads after lights. Has learned to look like he is resting when he is memorizing.',
        N'Corps candidate training wool; equipment maintained precisely; the uniform of someone who wants to be exactly unremarkable.',
        N'none',
        N'Corps training rotations, weapons drills, equipment maintenance. Evenings with scholarly texts he brought himself, kept under his bunk. Thirty days until the infusion.',
        N'He applied for Myrmidon candidacy specifically because Transmutation trainees receive unsupervised access to the restricted stacks of the Vigil Seat archive — a collection including several hundred untranslated Sphere 31 texts he has spent three years trying to access through official scholarly channels. He has been denied each time. He is prepared to survive the infusion or die in the attempt to read those texts. He does not fully believe the first outcome is likely, and he applied anyway.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Corps training grounds at the Ophiuchus main seat; scheduled transit to Vigil Seat installation for infusion preparation',
        N'0', N'0',
        N'young Italian soldier-scholar, early twenties, black hair short, lean, garrison training ground at dusk, concealed book under arm, expression of controlled calculation --ar 2:3',
        N'A 22-year-old Italian man in Corps candidate uniform, black hair, lean build; at a garrison training ground; the posture of someone pretending to belong.',
        0, 0
    );
    PRINT 'Luca Ferri seeded.';
END
ELSE PRINT 'Luca Ferri already exists.';
GO

-- 3. Adamo Conti — Senior Membrane Theorist
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Adamo Conti')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Adamo Conti', N'adamo-conti', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Adamo Conti', N'adamo-conti', N'Adamo', N'Conti', N'Master',
        N'human', N'human', N'male', N'he/him', 48, N'alive',
        N'Senior membrane theorist, House Ophiuchus scholarly division; most-cited active scholar on membrane consciousness',
        N'House Ophiuchus membrane theorist, twelve years published. His foundational text on membrane consciousness has shaped the House''s scholarly program for a decade. He is respected. He has earned most of it.',
        N'The plagiarist whose twelve years of built reputation creates a structural trap: the only way to expose the theft is to expose the institution that celebrated it, which is his own.',
        N'No POV.',
        N'House Ophiuchus; main seat scholarly division; born Conti family, minor academic lineage, southern peninsula',
        176, 84,
        N'slightly heavy-built; professorial; the body of someone whose work has always been at a desk',
        N'grey-brown, thinning', N'unremarkable, unmaintained', N'short', N'dark brown', N'olive', N'clear; the self-possession of someone who stopped worrying about appearances when the reputation made it unnecessary',
        N'none',
        N'Deliberate in formal settings; expansive when discussing his own work, which is most of the time. Has a scholar''s habit of finishing other people''s sentences with the correct answer, which most of his colleagues have stopped noticing.',
        N'Scholar''s wool, well-maintained but not polished. The clothes of someone who has decided the work is the presentation.',
        N'none',
        N'Morning correspondence review and committee obligations. Afternoons writing and revising. Evening reading. A private review session each week of materials he keeps in a locked cabinet he has held since thirty-six.',
        N'His foundational paper on membrane consciousness — the most cited Ophiuchus scholarly text of the past generation — was developed from raw observational data he transcribed from a dead colleague''s private notes without attribution. He changed the notation system and one variable. The colleague, Elisabetta Neri, died in a Transmutation rejection three weeks before the paper was submitted. Her original notes are in his locked cabinet, in a folder labeled with her name. He has told himself for twelve years that this is a form of memorial. He knows what it actually is.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Ophiuchus scholarly division and main seat; inter-House academic symposia as invited; occasional Vigil Seat consulting sessions',
        N'0', N'0',
        N'medieval Italian academic, late forties, grey-brown thinning hair, olive skin, professorial wool, stone scholarly chamber, locked cabinet behind him, expression of a man comfortable with his reputation --ar 2:3',
        N'A 48-year-old Italian scholar in professorial wool, grey-brown hair, olive skin; in a stone study; a locked cabinet behind him; the comfortable expression of a man who stopped being afraid a long time ago.',
        0, 0
    );
    PRINT 'Adamo Conti seeded.';
END
ELSE PRINT 'Adamo Conti already exists.';
GO

-- 4. Emilia Gallo — Sphere 31 Cataloguer
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Emilia Gallo')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Emilia Gallo', N'emilia-gallo', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Emilia Gallo', N'emilia-gallo', N'Emilia', N'Gallo', N'',
        N'human', N'human', N'female', N'she/her', 41, N'alive',
        N'Sphere 31 cataloguer, Ophiuchus off-world materials division; eleven years',
        N'Sphere 31 cataloguer, eleven years in the Ophiuchus off-world materials division. Most fluent reader of Sphere 31 notation in the House. She is working on something she has not told her supervisor about.',
        N'The archivist who may be a Sphere 31 taking building a private roster of others — which would make her the most dangerous person in the House who does not know it.',
        N'No POV.',
        N'House Ophiuchus; off-world materials archive, main seat; born Gallo family, southern peninsula, scribal tradition',
        165, 65,
        N'medium; the unhurried body of someone who has spent eleven years at a desk she has made entirely her own',
        N'dark, worn in a long braid when working', N'braided', N'long', N'hazel', N'warm olive', N'clear; seldom sees the sun directly',
        N'none',
        N'Quiet and contained. Moves through the archive the way someone moves through their own reasoning — no wasted steps, no backtracking. In conversation, she waits longer than people expect before responding.',
        N'Practical archival wool; a reading apron she has worn for seven years; the braid over her shoulder when she is working, which is most of the time.',
        N'none',
        N'Cataloguing Sphere 31 materials eight hours daily. Evenings composing music she has been composing for twenty years. The two activities have recently begun to overlap in ways she finds difficult to describe.',
        N'Eleven years ago she began composing music privately using a harmonic notation system she developed herself. Three years ago she catalogued a Sphere 31 text containing the same notation, independently developed, applied to compositions structurally similar to her own. She has since identified twenty-two Sphere 31 texts with correlating material. She has concluded she may be a Sphere 31 taking. She has been compiling a private list of other House members whose knowledge or skill shows similar inexplicable parallels to Sphere 31 cultural material. The list currently names seven people.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Ophiuchus off-world materials archive; restricted stacks access by appointment; does not travel',
        N'0', N'0',
        N'medieval Italian archivist, early forties, dark hair in long braid, hazel eyes, warm olive skin, archive shelves of Sphere 31 manuscripts, private notation journal beside the cataloguing work --ar 2:3',
        N'A 41-year-old Italian archivist, dark braided hair, hazel eyes, warm olive skin; among Sphere 31 manuscripts; a private notation journal beside her cataloguing work; quiet, contained.',
        0, 0
    );
    PRINT 'Emilia Gallo seeded.';
END
ELSE PRINT 'Emilia Gallo already exists.';
GO

-- 5. Renata Pellegrini — Intelligence Analyst
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Renata Pellegrini')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Renata Pellegrini', N'renata-pellegrini', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Renata Pellegrini', N'renata-pellegrini', N'Renata', N'Pellegrini', N'',
        N'human', N'human', N'female', N'she/her', 33, N'alive',
        N'Intelligence analyst, House Ophiuchus; cross-references Scrying observation logs against inter-House movement records',
        N'House Ophiuchus intelligence analyst, cross-referencing Scrying observation logs against inter-House movement records. Thorough, disciplined. Identified a surveillance discrepancy two months ago, reported it to Spymaster Orsini, and has been waiting for any indication it was acted on.',
        N'The analyst whose correct intelligence finding has been received and suppressed — making her either the person who uncovers the source or the next thing in the House to be quietly managed.',
        N'No POV.',
        N'House Ophiuchus; intelligence analysis division, main seat; born Pellegrini family, administrative lineage',
        167, 63,
        N'medium; compact efficiency; the body of someone whose work requires stillness and whose mind never provides it',
        N'dark brown', N'cut short', N'short', N'brown', N'olive', N'clear; fine-lined at the corners of the eyes',
        N'none',
        N'Economical and precise. Files everything immediately, notes cross-references in the margin before the thought leaves. Has a habit of tapping a single finger on her desk when reading something she disagrees with.',
        N'Ophiuchus intelligence division practical: dark wool, nothing that catches the eye; subdued House colors; the attire of someone whose job is to observe, not be observed.',
        N'none',
        N'Cross-referencing Scrying logs against inter-House movement records, morning to evening. Filing her reports through official channels. Not filing the one thing she is waiting on Orsini to act on.',
        N'Two months ago she intercepted a fragment of what she identified as unauthorized Liturgy routing — a coded transmission originating from within the House''s scholarly division, sent directly to Liturgy administrative channels, bypassing Liaison Farro entirely. She compiled her evidence into a formal report and delivered it in person to Spymaster Orsini. He acknowledged receipt in writing. Nothing has happened since. She is now considering two possibilities: that Orsini is protecting the source, or that Orsini is the source. She has not yet determined which question to ask first.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Ophiuchus intelligence analysis division; main seat records chambers; limited supervised transit for inter-House data exchange',
        N'0', N'0',
        N'medieval Italian intelligence analyst, early thirties, dark short hair, brown eyes, olive skin, stone records chamber with cross-referenced logs, expression of someone waiting for an answer that is not coming --ar 2:3',
        N'A 33-year-old Italian woman analyst, dark short hair, olive skin; at a stone records desk with cross-referenced observation logs; the expression of someone who filed a report and is still waiting.',
        0, 0
    );
    PRINT 'Renata Pellegrini seeded.';
END
ELSE PRINT 'Renata Pellegrini already exists.';
GO

-- 6. Ottavia Ricci — Young Researcher, Membrane Topology
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ottavia Ricci')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ottavia Ricci', N'ottavia-ricci', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Ottavia Ricci', N'ottavia-ricci', N'Ottavia', N'Ricci', N'',
        N'human', N'human', N'female', N'she/her', 26, N'alive',
        N'Young researcher, membrane topology, Vigil Seat installation, House Ophiuchus; eight months at the primary Scrying site',
        N'Young Ophiuchus membrane researcher, eight months at the Vigil Seat. Technically brilliant. Developed a predictive model for membrane thinning that her supervisor filed under his own name three weeks ago. She has her original notes.',
        N'The stolen researcher whose original dated notes are the detonator for multiple simultaneous crises — if she decides to use them, and if she understands what the model actually predicts.',
        N'No POV.',
        N'House Ophiuchus; Vigil Seat installation, northern Ridge country; born Ricci family, scholarly lineage, southern peninsula',
        160, 57,
        N'slight; young; the body of someone still growing into what her mind already is',
        N'dark brown', N'loosely tied', N'medium', N'dark brown', N'olive', N'clear',
        N'none',
        N'Fast-moving through the installation galleries, then completely still when working. Asks direct questions in a register that the senior researchers find presumptuous and her junior peers find bracing.',
        N'Scholar''s practical wool, functional; she dates every page of her research notation and signs the bottom of each. She started doing this three weeks ago.',
        N'none',
        N'Morning Scrying observation sessions. Afternoons writing research notes she dates and signs on every page. She has dated and signed everything for the past three weeks.',
        N'Eight months ago she developed a topological model predicting membrane thinning points with approximately seventy-three percent accuracy, validated against historical Vigil Seat records. She shared the draft with her supervisor, Master Rodrigo Vianello, for preliminary review. Three weeks later Vianello submitted it to the House scholarly council under his name with two formatting changes. A colleague who attended the session told her. She has her original dated notation. She has not filed a formal challenge. She is trying to determine whether the model contains something Vianello is suppressing, or whether he simply took credit for her work because he could. Both possibilities concern her in different ways.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Vigil Seat installation and adjacent research galleries; returning to main seat quarterly for supervisory review',
        N'0', N'0',
        N'young Italian woman researcher, mid-twenties, slight, dark hair loosely tied, stone Vigil Seat gallery with membrane aperture diagram, dated research notebook held close, expression of someone deciding --ar 2:3',
        N'A 26-year-old Italian woman in scholar''s wool, dark hair loosely tied; in a stone gallery near a membrane diagram; holding a dated notebook; an expression of someone who has decided something but not yet what.',
        0, 0
    );
    PRINT 'Ottavia Ricci seeded.';
END
ELSE PRINT 'Ottavia Ricci already exists.';
GO

-- 7. Nicola Caputi — Knight, Vigil Seat Guard
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Nicola Caputi')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Nicola Caputi', N'nicola-caputi', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Nicola Caputi', N'nicola-caputi', N'Nicola', N'Caputi', N'Ser',
        N'human', N'human', N'male', N'he/him', 32, N'alive',
        N'Knight, Vigil Seat installation guard detail, House Ophiuchus; seven years at the installation',
        N'Knight, Vigil Seat installation guard, seven years. Reliable, close-mouthed. Witnessed seven unexplained disappearances in installation proximity and filed each one under maintenance incidents on Second Captain Ferrara''s instruction. The seventh disappeared while holding his hand.',
        N'The witness whose private journal of seven Takings is the most complete record of membrane transit events in the House — locked in a box no one knows exists, in a room he has not left in seven years.',
        N'No POV.',
        N'House Ophiuchus; Vigil Seat installation, northern Ridge country; born Caputi family, Corps tradition',
        188, 91,
        N'athletic-dense; Knight-rank; the body of someone who has been physically transformed and has spent seven years in a static posting',
        N'black', N'military short', N'short', N'dark brown', N'olive', N'weathered; a small scar at the left brow',
        N'Subtle height gain, increased density',
        N'Still on the perimeter, fast when needed. The Corps movement economy of someone who has learned that ninety-five percent of guard duty is patience. Rarely initiates conversation. Answers precisely.',
        N'Corps garrison uniform maintained to personal standard; installation guard insignia; no ornamentation.',
        N'First-stage Transmutation (Xerum 525); height and density increase, enhanced strength and endurance; survived first infusion at twenty-five',
        N'Dawn to the installation perimeter. Guard rotation through day and evening. A private journal entry before sleep, every night, since the first Taking seven years ago.',
        N'He has witnessed seven persons disappear in unexplained proximity to the Vigil Seat installation. Second Captain Caterina Ferrara instructed him each time to file each incident under maintenance records as equipment-proximity anomalies. He complied. The seventh was a Sphere 31 translator named Tomas Delfi, assigned to his escort that day. Delfi''s hand was in his when the Taking occurred. He felt it. He has written nothing about any of this in any official record. He keeps a private journal in a locked box he has told no one about; it contains seven detailed accounts, written the same night each Taking occurred.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Vigil Seat installation perimeter and guard rotations; assigned installation zones; has not left the installation grounds in over a year',
        N'0', N'0',
        N'medieval Italian knight, early thirties, black military-short hair, dark eyes, olive skin, Vigil Seat outer gallery at dusk, Corps garrison uniform, expression of controlled grief --ar 2:3',
        N'A 32-year-old Italian Knight at the Vigil Seat installation, black hair, olive skin; in Corps garrison uniform; standing the perimeter at dusk; grief being held very still.',
        0, 0
    );
    PRINT 'Nicola Caputi seeded.';
END
ELSE PRINT 'Nicola Caputi already exists.';
GO

-- 8. Serafina Amati — Dame, Scholarly Division Transmutation Practitioner
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Serafina Amati')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Serafina Amati', N'serafina-amati', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Serafina Amati', N'serafina-amati', N'Serafina', N'Amati', N'Dame',
        N'Dame, Transmutation practitioner, Ophiuchus scholarly division; eleven years administering infusions to researchers and scholars',
        N'Dame, House Ophiuchus scholarly division Transmutation practitioner. Eleven years administering infusions to researchers seeking field access. Her mortality rate over eleven years is fifty-eight percent, significantly below published Corps figures. She has not disclosed her method.',
        N'The practitioner whose independently developed protocol improvements are the civilian mirror of the Corps program — two people arriving, separately, at the same dangerous truth about Transmutation mortality.',
        N'No POV.',
        N'House Ophiuchus; scholarly division Transmutation preparation chambers, main seat; born Amati family, physician lineage',
        182, 79,
        N'strong; Dame-rank density; unhurried in all movement; the body of someone who underwent Transmutation to understand what she asks of others',
        N'dark', N'worn back, always', N'medium (pinned)', N'amber (changed from dark brown)', N'olive', N'clear; the stillness of a face that has learned what rushing costs',
        N'Subtle height gain, increased density',
        N'Unhurried everywhere. Has learned that the quality of stillness before an infusion affects candidate outcomes. The transformation changed her eyes and she still occasionally notices this in reflections, eleven years later.',
        N'Practitioner''s formal: House Ophiuchus scholarly colors, precisely cut; the clothes of someone whose authority is institutional and who has no interest in supplementing it with presentation.',
        N'First-stage Transmutation (Xerum 525); underwent infusion to understand what she asks of candidates; height and density increase; altered eye color (dark brown to amber); enhanced endurance; survived first infusion at twenty-eight',
        N'Candidate assessments, scheduled and otherwise. Infusion preparation when candidates are ready. Private research evenings that are not filed under House records.',
        N'Over eleven years she has made forty-three protocol adjustments to the standard Transmutation infusion procedure — timing modifications, Xerum 525 concentration calibrations, sequence alterations — none of which she has disclosed to the Corps program or to the House Transmutation oversight body. Her adjusted mortality rate is fifty-eight percent. She does not know what the Corps'' actual suppressed figure is. She has been corresponding in deliberately vague terms with Corps Practitioner Dame Orsolina Verdi for two years. Both are aware the other is doing something similar. Neither has been specific enough to confirm it. Both are waiting for the other to go first.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Ophiuchus scholarly division Transmutation preparation chambers; main seat infirmary consultation; limited Vigil Seat access for field-candidate preparation',
        N'0', N'0',
        N'medieval Italian Dame practitioner, late thirties, dark hair worn back, amber eyes, olive skin, stone preparation chamber with alchemical infusion apparatus, calm authority, expression of someone who has given last rites --ar 2:3',
        N'A 39-year-old Italian Dame in Transmutation practitioner''s attire, dark hair worn back, amber eyes, olive skin; in a stone preparation chamber; calm, unhurried, carrying something private.',
        0, 0
    );
    PRINT 'Serafina Amati seeded.';
END
ELSE PRINT 'Serafina Amati already exists.';
GO

-- 9. Rodrigo Vianello — Senior Scrying Researcher
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Rodrigo Vianello')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Rodrigo Vianello', N'rodrigo-vianello', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Rodrigo Vianello', N'rodrigo-vianello', N'Rodrigo', N'Vianello', N'Master',
        N'Senior Scrying researcher, Vigil Seat installation, House Ophiuchus; fifteen years; supervises the membrane observation program',
        N'Senior Scrying researcher, Vigil Seat, fifteen years. Discovered the same anomaly four times in fifteen years. Filed it as equipment error each time. He is the only person in the Cauld who knows he made this choice.',
        N'The senior researcher suppressing the most important finding in Ophiuchus science for fifteen years — and who stole a junior researcher''s work specifically to keep suppressing it.',
        N'No POV.',
        N'House Ophiuchus; Vigil Seat installation, northern Ridge country; born Vianello family, merchant lineage with one generation of scholarly service',
        180, 78,
        N'medium; scholar; the body of someone whose mind has always been the relevant instrument',
        N'dark, going grey; unkempt in a way that reads as preoccupied', N'irregular, unmaintained', N'short', N'dark brown', N'olive', N'clear; the particular strain of someone carrying something for a long time',
        N'none',
        N'Preoccupied in formal settings; expansive and precise when discussing historical Scrying records, which he prefers to discussing current ones. Has a habit of looking at the aperture chamber when he thinks no one is watching.',
        N'Scholar''s wool, unmaintained; the clothes of someone whose attention has been elsewhere for fifteen years.',
        N'none',
        N'Scheduled Scrying observation sessions. Research correspondence with other Houses'' scholars — deliberately vague. Afternoons reviewing the private documents he wrote four years ago and destroyed the following morning, rewriting them from memory.',
        N'Four times in fifteen years, his Scrying sessions recorded a threefold aperture widening when sessions extended past nine hours. Each time he filed the reading as equipment calibration error and corrected the log. The fourth time he wrote an eight-page document outlining what the finding meant, then destroyed it the following morning. He has not replicated the session since. He has also filed young researcher Ottavia Ricci''s topological model — which accurately predicts the locations where this effect occurs — under his own name, with two formatting changes. He did this because her model would attract scholarly scrutiny that would make his suppressed findings impossible to continue suppressing. He knows exactly what he has done. He does not know what to do with this knowledge.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Vigil Seat installation and surrounding research territory; inter-House scholarly correspondence but no personal travel in six years',
        N'0', N'0',
        N'medieval Italian Scrying researcher, mid-forties, dark hair going grey unkempt, olive skin, Vigil Seat observation chamber with membrane aperture, suppressed urgency, preoccupied expression --ar 2:3',
        N'A 44-year-old Italian researcher in scholar''s wool, dark greying hair, olive skin; at the Vigil Seat aperture chamber; the expression of a man who has been making the same decision for fifteen years.',
        0, 0
    );
    PRINT 'Rodrigo Vianello seeded.';
END
ELSE PRINT 'Rodrigo Vianello already exists.';
GO

-- 10. Costanza Prati — Senior Sphere 31 Archivist
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Costanza Prati')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Costanza Prati', N'costanza-prati', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Costanza Prati', N'costanza-prati', N'Costanza', N'Prati', N'Mistress',
        N'Senior Sphere 31 archivist, Ophiuchus off-world materials division; twenty-two years',
        N'Senior Sphere 31 archivist, twenty-two years, Ophiuchus off-world materials. Has read more Sphere 31 text than any living scholar in the Cauld. Four years ago she found something that rewrites everything the House knows about Transmutation mortality.',
        N'The archivist sitting on a Sphere 31 Transmutation protocol with a ninety-one percent survival rate — which, if disclosed, would remake the Cauld''s power structure within a generation and cannot be disclosed safely.',
        N'No POV.',
        N'House Ophiuchus; off-world materials archive, main seat; born Prati family, archival tradition, southern peninsula',
        162, 66,
        N'slight-medium; the unhurried economy of someone who has been at this desk for twenty-two years',
        N'silver-streaked dark', N'worn in a coil', N'medium (pinned)', N'dark brown', N'warm olive', N'fine-lined; the face of someone who has spent twenty-two years reading things most people do not know exist',
        N'none',
        N'Unhurried everywhere. Never rushed by requests, which the scholarly division interprets as thoroughness. Is actually unhurried because she has decided, four years ago, that nothing else is as important as what she is working on privately.',
        N'Archival practical: dark wool, reading apron, ink-stained hands; a second drawer key on a chain around her neck that she has worn for four years.',
        N'none',
        N'Eight hours cataloguing Sphere 31 materials. An hour reviewing her private translation project. A second drawer review in the evening she does alone, after the archive closes.',
        N'Four years ago, during routine Sphere 31 cataloguing, she translated a section of text describing a staged physiological augmentation procedure — an analog to Transmutation, applied in three phases using what the text calls a compound catalyst. The clinical data in the surrounding texts documents a survival rate of approximately ninety-one percent across a cohort of four hundred subjects. She has spent four years translating adjacent materials to confirm this figure. She has confirmed it. She has not told Lord Orazio, the Corps, or Liturgy Liaison Farro. She is trying to find a path to introducing this knowledge that does not end with the Liturgy claiming the data and every Ophiuchus scholar who has read it. She has not found one yet.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Ophiuchus off-world materials archive; restricted stacks by her own long-standing assignment; Vigil Seat archive annex twice annually',
        N'0', N'0',
        N'medieval Italian senior archivist, early fifties, silver-streaked dark hair in coil, warm olive skin, surrounded by Sphere 31 manuscripts, private locked drawer, expression of someone holding something enormous very quietly --ar 2:3',
        N'A 50-year-old Italian archivist, silver-streaked dark hair in a coil, warm olive skin; among Sphere 31 manuscripts; a second drawer key at her neck; the expression of someone who has known something for four years.',
        0, 0
    );
    PRINT 'Costanza Prati seeded.';
END
ELSE PRINT 'Costanza Prati already exists.';
GO

-- 11. Bernardo Losi — Military Intelligence Scout
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Bernardo Losi')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Bernardo Losi', N'bernardo-losi', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Bernardo Losi', N'bernardo-losi', N'Bernardo', N'Losi', N'',
        N'human', N'human', N'male', N'he/him', 28, N'alive',
        N'Military intelligence scout, Myrmidon Corps, House Ophiuchus; field transit escort for Scrying research teams',
        N'Military intelligence scout, unenhanced, attached to Scrying research teams for field transit. Competent, deliberately unremarkable. Four months ago a House Draco agent at a border market offered him payment for field observation reports. He has not yet provided any.',
        N'The scout who accepted a spy''s money and hasn''t delivered — and whose window to extract himself without consequences is closing while the deadline the agent set has already passed.',
        N'No POV.',
        N'House Ophiuchus; field transit between main seat and Vigil Seat; born Losi family, working lineage, southern peninsula',
        175, 73,
        N'scout''s lean; built for distance and light load; compact and unremarkable by design',
        N'dark', N'short', N'short', N'brown', N'olive', N'clear',
        N'none',
        N'The deliberate unremarkability of someone whose job requires him to be unnoticed. Makes himself background in rooms he does not need to control. In the field, quick and precise — the two modes are hard to reconcile until you have seen both in the same day.',
        N'Field practical: neutral wool, light load, nothing that catches the eye across a border market or a ridge trail. Has never been told to dress this way. It is instinct, which is also what the House Draco contact said he noticed about him.',
        N'none',
        N'Field transit escort for research teams moving between the main seat and Vigil Seat installations. Waiting. Calculating how long before the House Draco contact sends someone to ask why the first report has not arrived.',
        N'Four months ago a man introduced himself at a Calyx-border market as a physician seeking transit escort recommendations. He was not a physician. By the end of the conversation he had offered Bernardo a sum equivalent to six months'' Corps pay for periodic field observation reports — installation patrol rotations near Scrying sites, personnel movement patterns, nothing that would require him to know he was betraying anyone specifically. Bernardo accepted because his mother''s treatment costs three times his monthly salary. He has not provided a report. He is trying to find a path out of the arrangement that does not require the House to know he agreed to it. The deadline the man set for the first report passed three weeks ago.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Field transit between main seat and Vigil Seat; border market zones during team supply runs; territory outside formal routes that he keeps deliberately unmapped',
        N'0', N'0',
        N'young Italian scout, late twenties, lean, dark hair short, olive skin, border market setting, neutral field wool, expression of someone calculating a deadline approaching --ar 2:3',
        N'A 28-year-old Italian scout, lean, dark, in neutral traveling wool; at a border market; the expression of someone who accepted something he cannot undo.',
        0, 0
    );
    PRINT 'Bernardo Losi seeded.';
END
ELSE PRINT 'Bernardo Losi already exists.';
GO

-- 12. Ippazio Moro — Champion-rank Senior Membrane Scientist
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ippazio Moro')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ippazio Moro', N'ippazio-moro', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Ippazio Moro', N'ippazio-moro', N'Ippazio', N'Moro', N'Champion',
        N'Champion-rank senior membrane scientist, Ophiuchus scholarly division; the oldest and most transformed scholar in the House',
        N'The Ophiuchus scholarly division''s only Champion-rank researcher. Six years ago he determined what the membrane actually responds to — and that the Scrying installations are not doing what anyone believes. He is sixty-seven. He has been deciding who to tell.',
        N'The Champion-rank scientist who holds the foundational misapprehension underlying the Cauld''s entire military and scholarly program — and who is running out of time to tell anyone before the knowledge dies with him.',
        N'No POV.',
        N'House Ophiuchus; Vigil Seat installation and scholarly division, main seat; born Moro family, academic lineage, southern peninsula; three-stage Transmutation survivor',
        204, 130,
        N'significantly restructured by Champion-rank Transmutation; heavier through the frame than any natural proportion; moves with the particular care of someone who has been this large for twenty years and has learned to be exact',
        N'white (formerly dark brown)', N'kept back from a face that has changed significantly', N'short', N'deep amber-gold (changed significantly from dark brown)', N'olive (deepened by Transmutation)', N'deeply lined; the particular weathering of someone whose face has been restructured twice and settled into this',
        N'Pronounced post-human transformation; 204cm; restructured proportions; significantly altered physiology across three infusions',
        N'The deliberate contained movement of someone very large who has spent twenty years being careful about proximity. Sits when others stand. Moves through doorways with practiced attention. His stillness has the quality of something that could, in a previous decade, have been very fast.',
        N'Scholar''s formal, always; House Ophiuchus colors in a cut made for his proportions, because nothing ready-made has fit since his third infusion. The clothes are immaculate. The body beneath them is extraordinary.',
        N'Third-stage Transmutation (Xerum 525); three infusions survived over thirty years; significantly altered physiology — height increase to 204cm, restructured proportions, enhanced strength beyond human norms, altered eye color and pigmentation; the oldest Champion-rank survivor in the House',
        N'Mornings in the Vigil Seat observation chamber — no longer running sessions himself, only reviewing others'' records. Afternoons writing and crossing out. He has identified the person he intends to tell. He has not yet told her.',
        N'Six years ago, during a sustained Scrying session at the Vigil Seat accompanied by two Knight-rank military escorts, the aperture widened by a factor of three compared to every other recorded session in fifteen years of logs. He ran the comparison sixteen times. The differential is attributable to the proximity of Transmutation-enhanced bodies, not to the Scrying apparatus or the operator. The membrane responds to Cauld biology — specifically to the physiological changes produced by Transmutation. The Scrying installations are not passive observation instruments. In his current best theory, built over six years of private calculation, they are a signal. He has not published this. He has identified membrane researcher Marta Segni as the one person in the House currently capable of understanding what he will tell her. He has not yet told her. He is sixty-seven and has been deciding for six years.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Vigil Seat installation and Ophiuchus scholarly division main seat; no longer travels; the observation chamber and the adjacent study are now his entire territory',
        N'0', N'0',
        N'ancient Champion-rank Italian scholar, late sixties, post-human proportions at 204cm, white hair, deep amber-gold eyes, formal House Ophiuchus scholarly colors in custom cut, Vigil Seat chamber, the stillness of someone deciding --ar 2:3',
        N'A 67-year-old Italian Champion-rank scholar, 204cm, white hair, deep amber-gold eyes, significantly restructured proportions; in formal House colors in a custom cut; in the Vigil Seat chamber; the stillness of a man who has known something for six years.',
        0, 0
    );
    PRINT 'Ippazio Moro seeded.';
END
ELSE PRINT 'Ippazio Moro already exists.';
GO

-- 13. Lavinia Peri — Newest Archive Researcher
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Lavinia Peri')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Lavinia Peri', N'lavinia-peri', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Lavinia Peri', N'lavinia-peri', N'Lavinia', N'Peri', N'',
        N'human', N'human', N'female', N'she/her', 20, N'alive',
        N'Newest researcher, Vigil Seat archive, House Ophiuchus; two weeks in post; recently certified',
        N'Newest Ophiuchus Vigil Seat archive researcher, two weeks in. Twenty years old, recently certified. On her second day she found a three-month gap in the historical observation logs where records should exist. She has not told anyone.',
        N'The newest arrival who found the most sensitive gap in the House''s record on her second day — and who has no institutional framework yet to understand what she has found or who it would be safe to tell.',
        N'No POV.',
        N'House Ophiuchus; Vigil Seat archive, northern Ridge country; born Peri family, minor scholarly lineage, southern peninsula',
        158, 54,
        N'slight; young; still growing into the authority her certification grants her',
        N'dark brown', N'loose; not yet disciplined into a working style', N'medium', N'brown', N'olive', N'clear; the openness of someone who has not yet learned what to keep off her face',
        N'none',
        N'Eager and careful in equal measure. Asks questions, then waits to see if asking was a mistake. Has been making a mental list of things to ask about when she knows who is safe to ask.',
        N'Scholar''s practical wool, new-looking; House Ophiuchus research insignia applied with the precision of someone who checked it twice.',
        N'none',
        N'Indexing historical observation logs, nine hours daily. Making a mental list of administrative discrepancies she has noted, waiting until she knows enough about the institution to know who to ask.',
        N'On her second day she was assigned to index historical observation logs from the Vigil Seat archive. In the master index she found a reference to a log set from seven years ago covering a continuous three-month period — numbered, cross-referenced, properly filed in the index as present. The logs themselves are not in the archive. The index entries reference them as existing. They are not there. She checked three times before accepting this was not an organizational error. She is twenty years old, two weeks into her first scholarly post, and she does not know whether this is an administrative oversight, something everyone already knows about, or something she has found by accident that she was not meant to find. She has not mentioned it to anyone. She is waiting until she understands enough about the institution to know which of those possibilities she is dealing with.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Vigil Seat archive; the indexed stacks she is assigned; has not yet been cleared for restricted access',
        N'0', N'0',
        N'young Italian archive researcher, twenty, slight, dark hair loose, stone Vigil Seat archive shelves, open index ledger, expression of someone who found something unexpected and doesn''t yet know what it means --ar 2:3',
        N'A 20-year-old Italian woman, slight, dark hair loose; at the Vigil Seat archive shelves; holding an open index ledger; the expression of someone who found a gap where there should not be one.',
        0, 0
    );
    PRINT 'Lavinia Peri seeded.';
END
ELSE PRINT 'Lavinia Peri already exists.';
GO

-- 14. Giacinta Fulvi — Transmutation Trainee, Second Attempt
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Giacinta Fulvi')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Giacinta Fulvi', N'giacinta-fulvi', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Giacinta Fulvi', N'giacinta-fulvi', N'Giacinta', N'Fulvi', N'',
        N'human', N'human', N'female', N'she/her', 34, N'alive',
        N'Transmutation practitioner trainee, House Ophiuchus scholarly division; survived first infusion rejection; attempting second infusion in ten days',
        N'Transmutation practitioner trainee; survived first infusion rejection three years ago without transforming. Attempting a second infusion in ten days. Has identified the calibration error from the first attempt. Has written letters to be found if the second attempt kills her.',
        N'The second-attempt candidate whose success or failure will produce data no one else in the Cauld currently has — and whose death would release four confessions she has been carrying for other people.',
        N'No POV.',
        N'House Ophiuchus; scholarly division, main seat; born Fulvi family, minor administrative lineage, southern peninsula',
        166, 64,
        N'medium; untransformed; the particular quality of someone who has been in a body that rejected a fundamental change and has spent three years living with it unchanged',
        N'dark brown', N'cut short, practical', N'short', N'dark brown', N'olive', N'clear; fine-lined at the eyes; the specific strain of someone who has been deciding something for three years',
        N'none',
        N'Careful and unhurried in a way that is not her natural mode — she has learned it in the three years since the rejection. Checks equipment twice. Signs everything. Has given her friend four sealed letters with specific instructions for each.',
        N'Scholar''s practical wool; the same clothes she has been wearing for six weeks because she has not been shopping and has not explained why.',
        N'none',
        N'Preparation protocols for the second infusion, conducted privately. Review of her identified calibration correction. Evening: the four sealed letters, given to a friend named Oncia Tessari with instructions not to open them unless Giacinta does not return from the preparation chamber.',
        N'Her first Transmutation infusion three years ago was rejected — she survived but did not transform, which the official record calls a statistical non-event. Through three years of private research into Transmutation physiology she has identified a specific timing error in the standard protocol that would explain her particular rejection pattern: the Xerum 525 concentration was at the correct volume but introduced four seconds early relative to her specific nervous system response signature, which she has since measured herself. She is attempting a second infusion in ten days and has quietly corrected the timing. She has not told Dame Serafina Amati, who will administer the infusion, what she has found or what she intends. She has not told the House. She has written four letters — to her family, to her supervisor, to Amati, and to Infirmary Commander Lorenzo Angioli — to be opened if the second attempt kills her. The letters tell the truth about everything: the correction, the research, and three other things she has been keeping for people who cannot keep them safely themselves.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Ophiuchus scholarly division; Transmutation preparation chambers; has not left the main seat in six weeks',
        N'0', N'0',
        N'medieval Italian woman, early thirties, dark practical short hair, olive skin, stone preparation chamber, four sealed letters on the desk beside her notes, pre-infusion stillness --ar 2:3',
        N'A 34-year-old Italian woman in scholar''s wool, dark short hair, olive skin; in a stone preparation chamber; four sealed letters beside her research notes; the stillness of someone who has made a decision.',
        0, 0
    );
    PRINT 'Giacinta Fulvi seeded.';
END
ELSE PRINT 'Giacinta Fulvi already exists.';
GO

-- 15. Edoardo Boschi — Knight, Vigil Seat Garrison Veteran
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Edoardo Boschi')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Edoardo Boschi', N'edoardo-boschi', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Edoardo Boschi', N'edoardo-boschi', N'Edoardo', N'Boschi', N'Ser',
        N'human', N'human', N'male', N'he/him', 45, N'alive',
        N'Knight, Vigil Seat garrison, House Ophiuchus; ten years at the installation; has outlasted four garrison commanders',
        N'Knight, Vigil Seat garrison, ten years. Has outlasted four garrison commanders. Privately convinced the Scrying apparatus is observing something adjacent to Sphere 31 rather than Sphere 31 itself. He has no evidence. He has been making notes for seven years.',
        N'The garrison veteran whose private conviction about what the apparatus is actually observing may be either delusional or the second most important scientific finding in the House — and he has no way to tell which.',
        N'No POV.',
        N'House Ophiuchus; Vigil Seat installation, northern Ridge country; born Boschi family, minor Corps tradition, southern peninsula',
        190, 98,
        N'heavy-set; Knight-rank; ten years of static garrison posting visible in a body that has maintained its mass without changing much else',
        N'dark, close-cropped, greying at the temples', N'close-cropped', N'short', N'dark brown', N'olive', N'weathered; the face of someone who has spent ten years looking at the same walls and thinking',
        N'Subtle height gain, increased density',
        N'Still on the perimeter; efficient inside the installation; the garrison patience of someone who has outlasted four commanders and expects to outlast more. Speaks rarely in formal settings. In private he is more voluble than people expect, which is why he has learned to keep the private time very private.',
        N'Corps garrison uniform maintained precisely; installation garrison insignia; the clothes of someone who has been wearing this uniform for ten years and has stopped noticing it.',
        N'First-stage Transmutation (Xerum 525); height and density increase, enhanced strength and endurance; survived first infusion at thirty-two',
        N'Dawn perimeter inspection. Guard rotation through day and evening. A private journal he keeps in his quarters — two hundred and seven entries — that he began seven years ago the night after the apparatus activated without an operator.',
        N'Seven years ago, during a nighttime garrison shift, he was alone in the Vigil Seat outer gallery when the Scrying apparatus activated without an operator. The aperture opened, held for approximately four minutes, and closed. Through it he observed what he initially took to be Sphere 31 — Mediterranean coastline, recognizable terrain features. But the coastline did not match any charted section of the southern peninsula or any Sphere 31 location he had studied in mandatory installation briefings. He has since reviewed every available Sphere 31 observation record. The terrain he saw does not appear in any of them. He has been making notes for seven years, two hundred and seven entries, documenting subsequent anomalies and building what he believes is a case that the apparatus is not observing Sphere 31 but something adjacent to it — a parallel, or an echo, or a different layer he does not have the language to name. He is aware he may simply be wrong. He has told no one, because he is a garrison soldier and not a scientist, and because telling someone is the point of no return on a belief he is not certain enough to defend.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.',
        N'Vigil Seat installation perimeter and outer galleries; garrison quarters; has not left the installation grounds in over two years',
        N'0', N'0',
        N'medieval Italian veteran knight, mid-forties, dark close-cropped greying hair, olive skin, Vigil Seat outer gallery at night, Corps garrison uniform, private journal in hand, expression of someone who has been thinking the same thought for seven years --ar 2:3',
        N'A 45-year-old Italian Knight, dark greying hair, olive skin, Corps garrison uniform; in the Vigil Seat outer gallery at night; holding a private journal; the expression of a man who saw something seven years ago and has never stopped thinking about it.',
        0, 0
    );
    PRINT 'Edoardo Boschi seeded.';
END
ELSE PRINT 'Edoardo Boschi already exists.';
GO
