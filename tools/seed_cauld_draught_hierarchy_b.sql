SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- CAULD UNIVERSE — HOUSE DRAUGHT LOWER HIERARCHY (PART B)
-- Scrying Installation Staff + Domestic Staff + Oathless Adjacent
-- Universe: fantasy-steampunk (ID 0197E9C9-0002-7000-8000-000000000002)
-- 2026-07-04
-- 27 characters; idempotent (IF NOT EXISTS guards on all inserts)
-- ═══════════════════════════════════════════════════════════════════════════════

-- ── Sigrun Thorvaldsen ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Sigrun Thorvaldsen')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Sigrun Thorvaldsen', N'sigrun-thorvaldsen', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Sigrun Thorvaldsen', N'sigrun-thorvaldsen', N'Sigrun', N'Thorvaldsen', N'',
        N'human', N'human', N'female', N'she/her', 62, N'alive',
        N'Head Scrying Operator; Grimsvík Installation; House Draught.',
        N'A lean woman with white-streaked grey hair kept in a practical braid. She moves with unhurried certainty — the posture of someone who has outlasted five senior officers and two installation commanders and does not need anyone to know it. Her eyes move differently from other people''s: a slow methodical sweep that never quite resolves into normal social eye contact. Thirty-five years at the apparatus has changed something in the way she looks at things.',
        N'The person most likely to have noticed what the apparatus actually shows. Her sustained silence about what she has observed is the central tension of the installation. Any story that cracks that silence alters everything downstream.',
        N'No POV.',
        N'House Draught; Grimsvík Installation, northern fjord territory',
        168, 67, N'lean-wiry',
        N'white with grey undertones', N'practical braid', N'long',
        N'pale grey', N'fair', N'weathered and lined',
        N'none',
        N'Unhurried and deliberate. She rarely initiates movement — she waits for movement to become necessary.',
        N'Dark practical wool, always the same cut. She considers variety a distraction. A copper ring on her right hand that she has never explained.',
        N'None.',
        N'Arrives before the morning watch and leaves after it. Reads her operators'' reports in filing order, not subject priority. Eats alone. Does not attend social functions inside the installation. Walks the cliff path above the fjord at dusk regardless of weather.',
        N'She has charted forty years of observation data and identified a pattern: the Liturgy''s takings from Sphere 31 cluster within eight to twelve months of major social disruptions in that world, and the demographic profile of those taken shifts during these windows toward younger and healthier individuals. She believes the Liturgy is not selecting opportunistically but harvesting deliberately — timing its collections to periods when disappearances are least traceable. She has never written this down. She does not trust the installation''s record systems to remain private.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Grimsvík Installation and immediate surrounding fjord territory; does not travel.',
        N'0', N'0',
        N'gaunt older woman, white hair in long braid, pale grey eyes, dark wool, medieval Norse steampunk scrying installation interior, serious expression, Buehlman dark register, portrait',
        N'older woman, white hair in a practical braid, pale grey eyes, dark wool clothing, medieval fantasy observatory interior, serious expression, portrait lighting',
        0, 0
    );
    PRINT 'Sigrun Thorvaldsen seeded.';
END
ELSE PRINT 'Sigrun Thorvaldsen already exists.';
GO

-- ── Halvor Oladapo ────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Halvor Oladapo')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Halvor Oladapo', N'halvor-oladapo', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Halvor Oladapo', N'halvor-oladapo', N'Halvor', N'Oladapo', N'',
        N'human', N'human', N'male', N'he/him', 51, N'alive',
        N'Senior Scrying Operator; Grimsvík Installation; House Draught. Twenty years watching a single Sphere 31 focal zone.',
        N'Broad-shouldered man with close-cropped dark hair going grey at the temples and a West African heritage that places him as the son of a trade-family line long established in House Draught''s port districts. He has spent twenty years watching the same Sphere 31 focal zone and has developed what he privately calls ''familiarity'' with the recurring subjects — a familiarity his senior operator Sigrun Thorvaldsen regards as a professional error. He is warm in conversation, generous with his time, and entirely convinced that the apparatus shows intention.',
        N'Counterweight to Bryndis Kjaersgaard''s cold empiricism. Represents the risk of anthropomorphizing data — and the possibility that the anthropomorphism is not entirely wrong.',
        N'No POV.',
        N'House Draught; Grimsvík Installation; family origin in the port district trading families',
        182, 87, N'broad-shouldered',
        N'dark brown going grey at temples', N'close-cropped', N'short',
        N'dark brown', N'deep brown', N'clear',
        N'none',
        N'Leans forward when interested, which is often. Occupies space with ease. Does not notice that he talks about Sphere 31 subjects by name as though they are people he knows.',
        N'Practical installation wool over a faded trading-house shirt — he keeps the shirt as a private reminder of where his family came from.',
        N'None.',
        N'Long watch rotations by preference — he volunteers for the extended overnight shifts because the apparatus resolves differently in those hours. Keeps a personal observation journal alongside the official logs. Argues with Bryndis Kjaersgaard at least twice a week.',
        N'For six years he has been encoding private signals in his official observation logs using a notation system only he understands — minor variations in punctuation and line spacing that, read by his key, form messages directed at a specific recurring figure in the Sphere 31 focal zone. He believes this figure has been responding in kind, leaving patterned arrangements in her observable environment that mirror his codes. He is not entirely wrong about the patterns. He is wrong about what causes them.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Grimsvík Installation; occasional travel to the port district to visit family.',
        N'0', N'0',
        N'broad-shouldered man, dark skin, close-cropped greying hair, warm expression, Norse steampunk scrying installation, medieval fantasy, portrait',
        N'broad-shouldered middle-aged man, dark brown skin, short greying hair, medieval fantasy observatory, warm expression, portrait',
        0, 0
    );
    PRINT 'Halvor Oladapo seeded.';
END
ELSE PRINT 'Halvor Oladapo already exists.';
GO

-- ── Bryndis Kjaersgaard ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Bryndis Kjaersgaard')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Bryndis Kjaersgaard', N'bryndis-kjaersgaard', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Bryndis Kjaersgaard', N'bryndis-kjaersgaard', N'Bryndis', N'Kjaersgaard', N'',
        N'human', N'human', N'female', N'she/her', 44, N'alive',
        N'Senior Scrying Operator; Grimsvík Installation; House Draught. Empiricist. Disagrees with Halvor Oladapo about everything the apparatus shows.',
        N'Compact and athletic, with the contained energy of someone who has learned to keep her opinions behind her teeth until she has evidence. Her hair is dark auburn kept short for practicality. She has been at the apparatus for eighteen years and has written three formal theoretical papers on signal interpretation, all of which are considered foundational at two other Houses. She considers Halvor Oladapo a gifted observer and a professional liability.',
        N'The institutionally credentialed voice of cold empiricism at the installation — which makes her hidden falsification of data more catastrophic, not less, for anyone who has built on her work.',
        N'No POV.',
        N'House Draught; Grimsvík Installation; academic background through a scholarly House exchange program',
        170, 68, N'compact-athletic',
        N'dark auburn', N'short and practical', N'short',
        N'pale green', N'fair', N'clear',
        N'none',
        N'Precise and economical. Takes up exactly the space required and no more. Her stillness is different from Sigrun''s — it is controlled rather than earned.',
        N'Installation grey-wool with the small emblem of the inter-House scholarly exchange she completed at twenty-six. She wears it every day. It has faded to near-invisibility.',
        N'None.',
        N'Methodical rotation schedule, never volunteer hours. Writes formal reports in full sentences. Reviews Halvor''s logs with visible patience that she does not feel. Corresponds with researchers at two other Houses who cite her theoretical work.',
        N'Twelve years ago she published a theoretical model of apparatus signal interpretation that was adopted as foundational by two other Houses. Four years ago she discovered an averaging error in the source data she had used — a flaw that makes patterns appear where there are none. The model is wrong. The military allocations two Houses have made based on her work are based on a fiction. She has been quietly introducing compensatory distortions into her current logs to sustain the model''s predictions rather than retract it. She cannot see a path to admission that does not end her career and her credibility in both Houses simultaneously.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Grimsvík Installation; occasional travel for scholarly correspondence.',
        N'0', N'0',
        N'compact woman, dark auburn short hair, pale green eyes, grey wool, Norse steampunk observatory, serious expression, medieval fantasy, portrait',
        N'compact woman, short auburn hair, pale green eyes, grey wool uniform, medieval fantasy observatory, controlled expression, portrait',
        0, 0
    );
    PRINT 'Bryndis Kjaersgaard seeded.';
END
ELSE PRINT 'Bryndis Kjaersgaard already exists.';
GO

-- ── Ketil Magnusson ───────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ketil Magnusson')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ketil Magnusson', N'ketil-magnusson', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Ketil Magnusson', N'ketil-magnusson', N'Ketil', N'Magnusson', N'',
        N'human', N'human', N'male', N'he/him', 58, N'alive',
        N'Technical Maintenance Chief; Grimsvík Installation; House Draught. Keeps the apparatus running. Does not do the observing.',
        N'A heavyset man with hands that are always faintly stained with machine-oil he can never quite wash out. He carries his weight forward, head slightly down, in the posture of someone who spends most of his time in low-ceilinged maintenance corridors. His hair is iron-grey, his beard is trimmed for safety near moving parts. He knows the apparatus better than anyone alive — including what it is no longer capable of.',
        N'The person who holds the technical ground truth of the installation. His silence about the apparatus''s degraded state makes every observation in the past nine years unreliable. He is the engine of an epistemological disaster that no one has yet noticed.',
        N'No POV.',
        N'House Draught; Grimsvík Installation; raised in the fjord fishing settlements',
        178, 91, N'heavyset-practical',
        N'iron-grey', N'short, trimmed for work', N'short',
        N'pale blue', N'ruddy', N'weathered',
        N'none',
        N'Head-down, forward-leaning. Moves through the installation like a man who knows which corridor floods in heavy rain.',
        N'Heavy canvas work-clothes over reinforced wool. Tool loops on both hips. His right boot has a crack in the sole he has repaired six times rather than replace the boot.',
        N'None.',
        N'In the maintenance bay before first light. Sleeps there three nights a week on a narrow cot he installed without asking permission. Eats whatever is left in the installation kitchen after the operators have taken what they want. Has not taken a full day away from the apparatus in four years.',
        N'The apparatus''s primary resonance chamber cracked nine years ago. The replacement component was requisitioned through the standard supply chain and never delivered — lost in a Draught logistics backlog that no one followed up on. Ketil has been compensating manually ever since, running the apparatus on a secondary harmonic configuration that functions but introduces an unknown degradation margin into every observation. Every scrying report generated in the past nine years has come from an instrument operating outside its design parameters. The official maintenance records say the primary configuration is nominal. He wrote those records himself.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Grimsvík Installation; rarely leaves the building.',
        N'0', N'0',
        N'heavyset middle-aged man, iron-grey hair and beard, oil-stained hands, heavy canvas work clothes, Norse steampunk machine room, medieval fantasy, portrait',
        N'heavyset man, grey beard, work-worn clothing, oil-stained hands, medieval fantasy machinery interior, portrait',
        0, 0
    );
    PRINT 'Ketil Magnusson seeded.';
END
ELSE PRINT 'Ketil Magnusson already exists.';
GO

-- ── Astrid Vilhjalmsen ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Astrid Vilhjalmsen')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Astrid Vilhjalmsen', N'astrid-vilhjalmsen', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Astrid Vilhjalmsen', N'astrid-vilhjalmsen', N'Astrid', N'Vilhjalmsen', N'',
        N'human', N'human', N'female', N'she/her', 23, N'alive',
        N'Junior Scrying Operator; Grimsvík Installation; House Draught. Trained eight months ago. Has seen something the senior operators have dismissed.',
        N'A slight young woman with pale blonde hair she has started keeping pinned back since her third week at the apparatus — she reached for it reflexively during a long observation and pulled herself out of the focus state. She has not made that mistake again. She is attentive, methodical, and sufficiently careful with her words that the senior operators have begun to treat her as reliable. She is not wrong about what she has seen.',
        N'The new observer who saw something the experienced operators missed — or chose not to see. Her private investigation is the story''s live wire. She represents the possibility that the apparatus''s subjects are aware of observation.',
        N'No POV.',
        N'House Draught; Grimsvík Installation; trained at the fjord-plateau operator''s school',
        172, 63, N'slight',
        N'pale blonde', N'pinned back', N'medium',
        N'blue-grey', N'fair', N'clear',
        N'none',
        N'Alert and still during observation. In conversation, slightly over-precise — she is aware that she is junior and compensates by choosing words carefully.',
        N'Standard installation grey-wool. Her own addition: a small sewn pocket on her left forearm where she keeps a folded piece of paper — the transcription she made the first time.',
        N'None.',
        N'Volunteers for observation shifts that overlap with Sigrun''s to study how the senior operator reads the apparatus. Keeps two sets of notes — the official log and a private journal she brought from home. Reviews her private journal every evening.',
        N'In her third week at the apparatus, a Sphere 31 subject appeared to look directly into the focal point and mouth words. She transcribed the lip movement from memory as: "we know you are watching." Senior operators told her she was tired and constructing patterns from noise. She said nothing further. In month two and month four the same event occurred — same subject, same phrase. She has now documented three instances in her private journal with annotated observation timestamps. She does not know what to do with this. She knows the senior operators will not listen.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Grimsvík Installation; occasional visits home to the plateau settlements.',
        N'0', N'0',
        N'young woman, pale blonde hair pinned back, blue-grey eyes, grey wool, Norse steampunk observatory, attentive expression, medieval fantasy, portrait',
        N'young woman, pale blonde hair pinned back, grey-blue eyes, grey wool uniform, medieval fantasy observatory, alert expression, portrait',
        0, 0
    );
    PRINT 'Astrid Vilhjalmsen seeded.';
END
ELSE PRINT 'Astrid Vilhjalmsen already exists.';
GO

-- ── Ivar Kolbeinsson ─────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ivar Kolbeinsson')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ivar Kolbeinsson', N'ivar-kolbeinsson', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Ivar Kolbeinsson', N'ivar-kolbeinsson', N'Ivar', N'Kolbeinsson', N'',
        N'human', N'human', N'male', N'he/him', 74, N'alive',
        N'Retired Scrying Operator; lives near Grimsvík Installation; informally consulted when the active operators cannot explain something.',
        N'A solid, aged man who walks with a cane that he does not actually need — he uses it to slow himself down, because he found in his sixties that he was moving too fast through moments worth noticing. His hair and beard are fully white. He lives in a stone house two kilometres from the installation, keeps geese, and is visited by active operators more often than the official record would indicate. He answers questions in the order he chooses, not the order they are asked.',
        N'The repository of institutional knowledge that was never written down — including the knowledge that the current Head Operator is not the first person to reach her conclusion about the Liturgy. His silence about what he was told preserves the existing order. Breaking it would cost him something he has already decided he cannot afford.',
        N'No POV.',
        N'House Draught; retired from Grimsvík Installation; now settled in the fjord-side settlement below the installation',
        176, 78, N'aged-solid',
        N'fully white', N'loose and short', N'short',
        N'pale blue', N'fair', N'deeply lined',
        N'none',
        N'Deliberate and unhurried. Uses his cane as a thinking tool — taps it once when he is about to say something important.',
        N'Old wool in dark brown, well-mended. He dresses as he always has. He has not updated his wardrobe in fifteen years and sees no reason to.',
        N'None.',
        N'Rises early, feeds the geese, walks the path along the fjord in the direction of the installation and back. Receives visitors in the afternoon. Reads. Keeps a cold-stove fire he does not light until winter genuinely requires it.',
        N'Twenty-two years ago, Ivar developed the same theory that Sigrun Thorvaldsen has now arrived at independently: that the Liturgy times its Sphere 31 takings to coincide with social disruption periods in that world, selecting during windows of maximum untraceability. He committed his findings to a formal paper. Three weeks later a Liturgy functionary visited him at the installation, described the theory as ''a navigational error in your analytical method,'' and advised him to let it go. He burned the paper that evening. He retired four years later. He visits the installation occasionally, watches Sigrun work, and has never told her that he was there first. He has never told her that the Liturgy knew enough to come and say nothing.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Stone house below Grimsvík Installation; the fjord path between them; occasional visits inside the installation.',
        N'0', N'0',
        N'elderly man, white hair and beard, pale blue eyes, dark brown wool, medieval Norse coastal settlement, contemplative expression, Buehlman dark register, portrait',
        N'elderly man, white hair, pale blue eyes, dark mended wool, medieval fantasy coastal setting, thoughtful expression, portrait',
        0, 0
    );
    PRINT 'Ivar Kolbeinsson seeded.';
END
ELSE PRINT 'Ivar Kolbeinsson already exists.';
GO

-- ── Gudrun Steinsdottir ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Gudrun Steinsdottir')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Gudrun Steinsdottir', N'gudrun-steinsdottir', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Gudrun Steinsdottir', N'gudrun-steinsdottir', N'Gudrun', N'Steinsdottir', N'',
        N'human', N'human', N'female', N'she/her', 67, N'alive',
        N'Seneschal; House Draught estate at Drauchtholt. Manages the entire household — staff, accounts, provisioning, formal obligations. Has served longer than the current Lord has been alive.',
        N'A stout, authoritative woman with white hair worn in a tight coil and eyes the colour of old pewter that have seen the estate through two Lords and never once looked surprised. She moves through the household like weather — steady, pervasive, impossible to avoid. The staff does not fear her exactly; they fear being the thing she has to notice. The current Lord calls her by her first name in private and her title in company, and she has never given him reason to regret either choice.',
        N'The estate''s institutional memory. She knows where every secret is stored and has access to every account, every room, every arrangement. Her moral position — having processed the paperwork for an involuntary taking — is the household''s loaded weapon.',
        N'No POV.',
        N'House Draught; Drauchtholt estate; family has served Draught households for three generations',
        163, 70, N'stout-authoritative',
        N'white', N'tight coil', N'medium',
        N'pewter grey', N'fair', N'lined and composed',
        N'none',
        N'Carries herself with the settled permanence of the building itself. Rarely hurries. When she does hurry, the staff notices.',
        N'Dark grey estate wool with the household silver pin at the collar — she has worn a variant of this pin for forty years and replaces it only when the previous one is too worn to present respectably.',
        N'None.',
        N'Reviews the household accounts before breakfast. Conducts staff briefings at the same hour every morning without exception. Inspects the guest quarters personally before any arrival. Is the last person in the estate to sleep and the first to rise.',
        N'Five years ago she received a Liturgy transfer order signed by the current Lord, listing a household servant named Maret Olsen under the heading "voluntary transit offering" with a delivery date four days out. Gudrun had spoken with Maret two days earlier; the woman had asked for a half-day leave to consult a dressmaker about a winter wedding. Gudrun processed the paperwork without modification. Maret was gone before the week ended. Gudrun has managed the household with the same efficiency since. She has not spoken of it to anyone. She has not entirely decided what she believes about herself.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Drauchtholt estate and its immediate provisioning range; the town below the plateau for accounts.',
        N'0', N'0',
        N'stout older woman, white hair in tight coil, pewter eyes, dark grey wool, silver pin at collar, medieval Norse estate interior, authoritative expression, Buehlman dark register, portrait',
        N'stout older woman, white hair in coil, grey eyes, dark grey wool dress, silver household pin, medieval fantasy estate interior, composed expression, portrait',
        0, 0
    );
    PRINT 'Gudrun Steinsdottir seeded.';
END
ELSE PRINT 'Gudrun Steinsdottir already exists.';
GO

-- ── Ragnar Ellison ────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ragnar Ellison')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ragnar Ellison', N'ragnar-ellison', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Ragnar Ellison', N'ragnar-ellison', N'Ragnar', N'Ellison', N'',
        N'human', N'human', N'male', N'he/him', 56, N'alive',
        N'Head Cook; House Draught estate at Drauchtholt. Has run the kitchen for thirty-five years. Knows the preference of every person in the House.',
        N'A large man — thick through the chest and arms in the way of men who have spent a lifetime carrying heavy pots — with a red beard now threaded heavily with white and a face that reads as perpetually amused. He is not sentimental about the family. He is precise about their preferences: the Lord''s intolerance for bitter greens, the eldest son''s inability to eat shellfish without a rash he refuses to admit to, the Lady''s private fondness for the commoner''s bread he makes on the last day of every month and leaves at the kitchen door of her suite without acknowledgement. He has never been thanked for the bread. He has never stopped making it.',
        N'The practitioner of an informal intelligence operation that the House has benefited from without naming. His extraction of the Ophiuchus diplomat''s intelligence established him as an asset the House uses without employing — and never intends to pay.',
        N'No POV.',
        N'House Draught; Drauchtholt estate; family origin in the coastal fishing settlements; trade-line English surname two generations back',
        183, 108, N'large and broad',
        N'red, heavily white-threaded', N'full beard, loose', N'short',
        N'amber-brown', N'ruddy', N'weathered',
        N'none',
        N'Takes up room without apology. Commands his kitchen with volume and efficiency. The only person in the estate who can make Gudrun Steinsdottir wait.',
        N'A cook''s apron over heavy wool — he considers his kitchen a working place and dresses accordingly. He has one formal wool jacket that he wears for the Lord''s dinner parties and then hangs back in the same place every time.',
        N'None.',
        N'In the kitchen by predawn to manage the morning fire. Runs the day''s provisioning by memory. Extracts a private assessment of every visiting diplomat from what they will and will not eat at the first meal. Teaches his sous-chef whether she asks or not.',
        N'Three years ago a visiting Ophiuchus diplomat began speaking openly about House Draught''s western fortification gaps during a dinner at which Ragnar had introduced a sauce containing a tincture known to lower inhibition at altitude — a recipe he has never shared and describes as ''a southern reduction.'' The intelligence was passed to the Lord and used operationally. The diplomat left his personal signet ring in the kitchen drain the following morning — he must have removed it during the meal in agitation. Ragnar found it. He cleaned it, identified it, and placed it in a crock of dried herbs where it has remained for three years. He has never mentioned finding it. He considers it fair payment for services rendered without acknowledgement.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Drauchtholt kitchen and provisioning routes to the lower market.',
        N'0', N'0',
        N'large broad man, red and white beard, amber eyes, cook''s apron over wool, Norse steampunk estate kitchen, amused expression, medieval fantasy, portrait',
        N'large broad man, red beard streaked white, amber eyes, cook''s apron, medieval fantasy kitchen, confident expression, portrait',
        0, 0
    );
    PRINT 'Ragnar Ellison seeded.';
END
ELSE PRINT 'Ragnar Ellison already exists.';
GO

-- ── Ingrid Haugen ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ingrid Haugen')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ingrid Haugen', N'ingrid-haugen', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Ingrid Haugen', N'ingrid-haugen', N'Ingrid', N'Haugen', N'',
        N'human', N'human', N'female', N'she/her', 33, N'alive',
        N'Sous-Chef; House Draught estate at Drauchtholt. Second in the kitchen. Has ambitions Ragnar Ellison knows about and has chosen not to discourage.',
        N'A medium-built woman with warm brown hair usually tucked under a kitchen cloth and flour on her wrists that she has stopped bothering to clean off until the end of the day. She is precise in the way that people are precise when they have been told they are not the best at something and have decided to prove it through detail. She respects Ragnar Ellison without being fond of him, which is the right relationship to have with someone who may be in your seat in ten years and shows no signs of leaving.',
        N'The ambition underneath the deference. Her private cookbook is a proxy for the larger question of whether she will leave service to become something the estate did not make her. Her relationship with Ragnar — mentor and obstacle simultaneously — is the thing that keeps her here.',
        N'No POV.',
        N'House Draught; Drauchtholt estate; family from the eastern plateau farming district',
        169, 66, N'medium',
        N'warm brown', N'tucked under kitchen cloth', N'medium',
        N'hazel', N'fair', N'flour-dusted',
        N'none',
        N'Efficient and purposeful in the kitchen. In conversation, slightly guarded — she is aware that expressing ambition in a household is a mistake.',
        N'Kitchen cloth over practical wool. Her own addition: a small folded cloth in her apron pocket that she uses exclusively to clean her hands when leaving the kitchen — a habit she developed as a marker between kitchen-self and other-self.',
        N'None.',
        N'Arrives in the kitchen after Ragnar and stays later than him. Manages the junior staff more ably than Ragnar notices. Spends her one evening off each week writing in her quarters.',
        N'She has been writing a cookbook — systematic, annotated, with seasonal variations and notes on supply chain substitutions for the winters when the southern routes close. She has four years of work stored in a wrapped oilcloth bundle in her quarters. Ragnar has found it twice while reorganizing the kitchen stores. He has added small notes in his own hand each time — a correction to a reduction time, an improvement to a spice ratio — and returned it without comment. Ingrid believes he does not know about the book. She is wrong. She is also, in those specific notes, becoming a better cook than she was.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Drauchtholt kitchen and immediate estate; market trips on provisioning days.',
        N'0', N'0',
        N'woman in her thirties, warm brown hair tucked under kitchen cloth, hazel eyes, flour on wrists, medieval Norse estate kitchen, intent expression, medieval fantasy, portrait',
        N'woman, warm brown hair, hazel eyes, kitchen cloth over wool, flour-dusted, medieval fantasy kitchen, focused expression, portrait',
        0, 0
    );
    PRINT 'Ingrid Haugen seeded.';
END
ELSE PRINT 'Ingrid Haugen already exists.';
GO

-- ── Thora Wellholt ────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Thora Wellholt')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Thora Wellholt', N'thora-wellholt', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Thora Wellholt', N'thora-wellholt', N'Thora', N'Wellholt', N'',
        N'human', N'human', N'female', N'she/her', 31, N'alive',
        N'Kitchen assistant; House Draught estate at Drauchtholt. Taken from Sphere 31 eleven years ago. Fully adapted. No one in the household knows her origin except Gudrun Steinsdottir, who processed her intake.',
        N'A slight woman who moves through the kitchen with the quietness of someone who spent years learning to be useful without drawing attention. Her Cauld-language is fluent, her accent unmarked. She laughs at the same things the other kitchen staff laugh at. She has been here eleven years and the household regards her as belonging to it the way a well-fitted door belongs to its frame.',
        N'The Sphere 31 presence inside the domestic interior — the person who crossed the membrane and decided, as a matter of survival, to close the door behind her. Her perfect adaptation is itself a form of grief that has chosen a different name.',
        N'No POV.',
        N'Sphere 31 (Earth); absorbed into House Draught estate at Drauchtholt eleven years ago; nominal heritage recorded as a northern settlement origin to cover her intake',
        165, 62, N'slight',
        N'dark brown', N'loose braid', N'long',
        N'dark brown', N'medium-brown', N'clear',
        N'none',
        N'Economical and quiet. She occupies a kitchen space without appearing to claim it. She has learned that being unobtrusive is not the same as being invisible, and she practices the distinction carefully.',
        N'Plain kitchen wool. She chose the plainest available when she arrived and has maintained that choice. She has one personal item: a small river-stone she carried through the transit and keeps in her left pocket.',
        N'None.',
        N'Kitchen work from early morning. Takes her meals with the other kitchen staff. Spends her evenings in her room with the window open regardless of temperature. She has not told anyone about this habit.',
        N'She remembers her former life with undiminished clarity — the exact smell of her apartment, her mother''s voice, the particular brand of instant coffee she was drinking at the kitchen table when the taking happened. She has never told anyone she remembers. She has told herself, as a practical matter, that she lives here now. She dreams in both languages: the Cauld''s in the early part of the night, Sphere 31''s in the hours before morning. She has not decided whether this is grief or something that does not have a name.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Drauchtholt kitchen and estate grounds.',
        N'0', N'0',
        N'slight woman, dark brown hair in loose braid, dark brown eyes, plain kitchen wool, medieval Norse estate kitchen, quiet expression, Buehlman dark register, portrait',
        N'slight woman, dark brown hair, dark eyes, plain wool clothing, medieval fantasy kitchen, quiet composed expression, portrait',
        0, 0
    );
    PRINT 'Thora Wellholt seeded.';
END
ELSE PRINT 'Thora Wellholt already exists.';
GO

-- ── Ulf Sorensen ──────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ulf Sorensen')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ulf Sorensen', N'ulf-sorensen', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Ulf Sorensen', N'ulf-sorensen', N'Ulf', N'Sorensen', N'',
        N'human', N'human', N'male', N'he/him', 52, N'alive',
        N'Butler; House Draught estate at Drauchtholt. Manages the serving staff and all formal occasions. Carries the House''s dignity in his posture alone.',
        N'A lean, precise man who stands so straight that junior staff have privately speculated he was Transmuted in his youth and something went subtly wrong. He was not. He simply decided, at nineteen, that the House''s dignity was something a person could embody as a professional discipline, and has not deviated since. He knows which family members leave at strange hours, which guests drink more than they should, and which of the Lord''s conversations are not meant to be overheard. He has forgotten more information than most people will ever acquire.',
        N'The formal surface of the household — and the keeper of its most detailed informal knowledge. His loyalty is structural rather than personal; he serves the House, not the Lord, and the distinction has never been tested to its limit. Yet.',
        N'No POV.',
        N'House Draught; Drauchtholt estate; entered service at seventeen; has served no other household',
        180, 84, N'lean-precise',
        N'dark brown going silver at the temples', N'swept back, formal', N'short',
        N'grey', N'fair', N'smooth and controlled',
        N'none',
        N'Perfectly upright. Never fidgets. His hands are always folded or occupied with a specific purpose — he does not permit himself intermediate gestures.',
        N'Formal estate livery maintained to a standard the tailor''s guild would accept as exemplary. His own clothes — the two sets he owns outside livery — are maintained with identical care.',
        N'None.',
        N'First formal staff member visible in the morning, last to leave public rooms at night. Conducts a private walk of the estate after the household retires — he calls this ''the close'' and has done it every night for thirty years. Has never explained it to anyone.',
        N'The Lord''s youngest son has returned home before dawn on fourteen separate occasions in the past two years, always approaching from the direction of the lower market quarter, always with a different account of where he has been. Ulf knows what is in the lower market quarter at those hours. He has not reported it, has not noted it in any record, and has not spoken of it. His private assessment — which he has articulated to himself precisely once and then put away — is that the youngest son is the only member of the Lord''s family capable of running this estate without destroying it, and whatever he is doing in the lower market quarter is his own business until it stops being that.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Drauchtholt estate interior; formal occasions in the surrounding noble district.',
        N'0', N'0',
        N'lean precise man, dark silver-templed hair, grey eyes, formal estate livery, Norse medieval estate, composed expression, Buehlman dark register, portrait',
        N'lean middle-aged man, dark hair silver at temples, grey eyes, formal livery, medieval fantasy estate interior, composed expression, portrait',
        0, 0
    );
    PRINT 'Ulf Sorensen seeded.';
END
ELSE PRINT 'Ulf Sorensen already exists.';
GO

-- ── Ragnhild Bjornstad ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Ragnhild Bjornstad')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Ragnhild Bjornstad', N'ragnhild-bjornstad', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Ragnhild Bjornstad', N'ragnhild-bjornstad', N'Ragnhild', N'Bjornstad', N'',
        N'human', N'human', N'female', N'she/her', 49, N'alive',
        N'Head Housekeeper; House Draught estate at Drauchtholt. Manages all cleaning, linen, laundry, and guest quarters. Has found things.',
        N'A sturdy woman with dark blonde hair now mostly grey, kept back with practical pins. She is the kind of person who is described as reliable so consistently that the word has become invisible, which is exactly the position from which she has observed everything. She manages the household''s physical fabric — every room, every linen, every guest quarter — with the systematic attention of someone who understands that a household tells its truth in the condition of its objects.',
        N'The material witness. She does not interpret what she finds — she catalogues it, precisely, and keeps it. Her discovery of the sealed document in the guest room is the most operationally dangerous secret in the household, and she does not yet know how dangerous it is.',
        N'No POV.',
        N'House Draught; Drauchtholt estate; raised in the estate''s dependency village; entered household service at fourteen',
        166, 72, N'sturdy',
        N'dark blonde, mostly grey', N'practical pins', N'medium',
        N'pale brown', N'fair', N'practical and unadorned',
        N'none',
        N'Methodical. Moves through rooms in a fixed pattern she has never consciously articulated but has never deviated from. Her hands are always doing something.',
        N'Plain housekeeper''s wool in dark grey. She has worn the same style for twenty years. Her only adornment is a small clasp at her collar that was her mother''s.',
        N'None.',
        N'Inspects every room on a rotating schedule she carries in her head. Oversees linen inventories weekly. Knows the condition of every mattress, curtain, and floorboard in the estate. Checks the guest quarters personally after any departure.',
        N'Three years ago she turned the mattress in the east guest room — the one reserved for senior Liturgy visitors — and found, fixed to the base of the second slat with a fold of waxed cloth, a sealed document. She did not open it. She pressed a clay impression of the wax seal before replacing the document exactly as she had found it. When she later compared the impression against the estate''s formal seal reference, it matched the personal signet of a Champion of House Draught — Aldric Wulfssen — who was officially recorded as killed in action in the Living War eight years ago. She visits the east guest room weekly. The document is still there.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Drauchtholt estate interior — every room, every annex.',
        N'0', N'0',
        N'sturdy woman, greying dark blonde hair pinned back, pale brown eyes, dark grey wool, medieval Norse estate, practical expression, Buehlman dark register, portrait',
        N'sturdy middle-aged woman, greying hair pinned back, dark grey wool, medieval fantasy estate interior, practical expression, portrait',
        0, 0
    );
    PRINT 'Ragnhild Bjornstad seeded.';
END
ELSE PRINT 'Ragnhild Bjornstad already exists.';
GO

-- ── Leif Hakonsen ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Leif Hakonsen')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Leif Hakonsen', N'leif-hakonsen', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Leif Hakonsen', N'leif-hakonsen', N'Leif', N'Hakonsen', N'',
        N'human', N'human', N'male', N'he/him', 43, N'alive',
        N'Household servant; House Draught estate at Drauchtholt. Has been here since childhood. Loyalty genuine but complicated by what he saw when he was twelve.',
        N'A medium-built man with sandy hair going thin at the crown and the slightly distant quality of someone who has never quite left a specific moment behind. He is genuinely good at his work, genuinely fond of the household, and genuinely aware that he has been carrying a secret for thirty years that he has never decided whether to release. The fondness and the secret coexist without resolution. He has learned to live in that space.',
        N'The household''s undelivered message. What he witnessed as a child — the previous Lady burning the letter about a daughter who survived — is a thread that, pulled, could rewrite the family''s history. He has never pulled it because he is not sure whose hands it should go into.',
        N'No POV.',
        N'House Draught; Drauchtholt estate; born in the estate''s dependency village; entered household service at ten',
        175, 79, N'medium',
        N'sandy, thinning at the crown', N'short and plain', N'short',
        N'pale blue', N'fair', N'slightly weathered',
        N'none',
        N'Unhurried and reliable. Has the posture of a man who has always moved through spaces that belong to other people and has made his peace with that.',
        N'Standard household livery. Has worn a variant of it for thirty years. It fits him now the way water fits a basin.',
        N'None.',
        N'Takes the morning rounds of the formal rooms. Manages the fires in the family wing. Is the person staff send to find something that has been misplaced — he knows where things are.',
        N'When he was twelve he walked into the library annex and found the previous Lady of the House burning a letter. In the seconds before the paper caught he read three lines: a name he later understood to be the eldest daughter killed at the Hessenvald crossing, the phrase "survived the crossing," and a place-name he has never been able to identify. The Lady looked at him. He left. She died six years later without ever speaking to him about it. He has carried those three lines for thirty years. He has never found anyone in the household he trusted enough to tell, and has begun to wonder if that moment has passed.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Drauchtholt estate and the dependency village below the plateau.',
        N'0', N'0',
        N'man in his forties, sandy thinning hair, pale blue eyes, household livery, medieval Norse estate interior, slightly distant expression, Buehlman dark register, portrait',
        N'man in forties, sandy hair, pale blue eyes, household livery, medieval fantasy estate, thoughtful expression, portrait',
        0, 0
    );
    PRINT 'Leif Hakonsen seeded.';
END
ELSE PRINT 'Leif Hakonsen already exists.';
GO

-- ── Solveig Makinen ───────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Solveig Makinen')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Solveig Makinen', N'solveig-makinen', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Solveig Makinen', N'solveig-makinen', N'Solveig', N'Makinen', N'',
        N'human', N'human', N'female', N'she/her', 27, N'alive',
        N'Household servant; House Draught estate at Drauchtholt. Recently arrived. Still learning the political terrain. Considerably smarter than she allows the household to notice.',
        N'A slight, quick woman with dark eyes and black hair she keeps short, from a Finnish-heritage trade family settled two generations back in House Draught''s eastern district. She is new enough that the household has not yet assigned her a fixed opinion, which is exactly the position she prefers. Gudrun Steinsdottir has praised her diligence twice in three months. She accepted the praise with appropriate modesty and filed the fact that Gudrun is watching her.',
        N'The operative in plain sight. Her purpose in the household is specific and concealed. She represents the way personal history can motivate institutional infiltration — and the question of what she will do if she actually finds what she is looking for.',
        N'No POV.',
        N'House Draught; Drauchtholt estate; family origin in the Finnish-heritage trade communities of the eastern district',
        171, 64, N'slight-quick',
        N'black', N'short', N'short',
        N'dark brown', N'light brown', N'clear',
        N'none',
        N'Efficient and observant. Moves quickly and quietly. Has learned to appear to be looking at the thing in front of her while actually tracking something else.',
        N'Standard household wool. She chose the plainest options from what was issued. She wears nothing personal.',
        N'None.',
        N'Takes all available task assignments, particularly those that involve the estate''s record rooms and storage annexes. Has been thorough enough in her work that Gudrun has begun assigning her to the areas Solveig specifically wants access to. She considers this satisfying.',
        N'She applied for this position through a false reference from a short-term household she worked at in the western district. Her grandfather, Paavo Makinen, was conscripted by House Draught in the border engagement thirty-nine years ago and was never returned — no death record was ever filed, no discharge was ever processed, and no pension was paid to his family. She believes a conscription ledger or a field accounting record in the Drauchtholt archive will tell her what actually happened to him. She has not yet found the archive section she needs. She is patient. Gudrun has told her twice that she has a future in estate service, which Solveig has understood correctly as a warning about how long she may need to stay.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Drauchtholt estate; record rooms and storage annexes by preference.',
        N'0', N'0',
        N'slight young woman, short black hair, dark brown eyes, plain household wool, medieval Norse estate, alert contained expression, Buehlman dark register, portrait',
        N'young woman, short black hair, dark eyes, plain wool uniform, medieval fantasy estate interior, contained alert expression, portrait',
        0, 0
    );
    PRINT 'Solveig Makinen seeded.';
END
ELSE PRINT 'Solveig Makinen already exists.';
GO

-- ── Freyja Asmundsen ──────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Freyja Asmundsen')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Freyja Asmundsen', N'freyja-asmundsen', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Freyja Asmundsen', N'freyja-asmundsen', N'Freyja', N'Asmundsen', N'',
        N'human', N'human', N'female', N'she/her', 35, N'alive',
        N'Lady''s personal attendant; House Draught estate at Drauchtholt. The most intimate observer in the household.',
        N'A poised woman with light auburn hair and the particular quality of attention that develops in people who spend years in close proximity to someone they are not allowed to fully know. She is warm in the way that professional intimacy sometimes produces warmth — genuine without being personal, present without being exposed. She has been the Lady''s attendant for eight years and has watched everything that eight years holds.',
        N'The witness to the Lady''s private life — including the correspondence that suggests the Lady is in contact with someone who should not exist. Freyja''s decision about what to do with that knowledge is the hinge the Lady''s entire arc turns on.',
        N'No POV.',
        N'House Draught; Drauchtholt estate; family from the noble household service tradition of the western fjord district',
        167, 61, N'poised',
        N'light auburn', N'dressed formally, pinned for the Lady''s occasions', N'long',
        N'warm brown', N'fair', N'clear',
        N'none',
        N'Upright and attentive without appearing to pay attention. She has refined the art of being present in a room without disturbing it.',
        N'Formal attendant''s dress — above serving staff, below the family. Keeps it impeccable. Her personal clothing, worn on her single half-day off each week, is simpler and considerably more comfortable.',
        N'None.',
        N'Present for the Lady''s morning preparation, formal occasions, and evening close. Manages the Lady''s correspondence intake — sorting, delivering sealed letters, and carrying sealed letters out when asked. Has never once been asked to open anything.',
        N'The Lady of the House has been corresponding for four years with a correspondent she refers to in her letters only as ''the steady hand.'' Freyja has carried five of those letters personally — sealed — and opened and resealed two of them without leaving a mark. ''The steady hand'' is not a lover. The correspondence is operational: specific requests about Liturgy transit schedules for particular seasonal windows, questions about the membrane''s physical state at precise dates. Freyja has concluded that the correspondent is someone who was taken from Sphere 31 and subsequently returned — which the Liturgy''s formal doctrine says is impossible. She does not know what this means about the Lady. She does not know what it means about the Liturgy. She has said nothing to anyone.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Drauchtholt estate; accompanies the Lady on formal occasions in the surrounding district.',
        N'0', N'0',
        N'poised woman, light auburn hair formally pinned, warm brown eyes, formal attendant dress, medieval Norse estate, composed expression, Buehlman dark register, portrait',
        N'poised woman, auburn hair, warm eyes, formal attendant clothing, medieval fantasy estate interior, composed attentive expression, portrait',
        0, 0
    );
    PRINT 'Freyja Asmundsen seeded.';
END
ELSE PRINT 'Freyja Asmundsen already exists.';
GO

-- ── Skarde Eriksen ────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Skarde Eriksen')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Skarde Eriksen', N'skarde-eriksen', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Skarde Eriksen', N'skarde-eriksen', N'Skarde', N'Eriksen', N'',
        N'human', N'human', N'male', N'he/him', 57, N'alive',
        N'Stable Master; House Draught estate at Drauchtholt. Manages all horses and transport. Knows when someone leaves at night. Has kept secrets about departures that were never recorded.',
        N'A large, weathered man who smells of horse and hay and does not apologise for either. His face is deeply lined from decades of outdoor work in a climate that makes no concessions. He has been managing this stable since he was twenty-two, which means he has managed it through two Lords, one siege that did not reach the estate, and seven years of Living War supply disruptions. He knows each horse by personality. He knows each rider by how they treat a horse.',
        N'The keeper of night departures. The stable is the estate''s exit point, and Skarde is its only reliable witness. His secret about the officer who came back riderless is the thread that connects the household''s visible history to the one the official record does not contain.',
        N'No POV.',
        N'House Draught; Drauchtholt estate; raised in the fjord-side horse-breeding settlements',
        186, 97, N'large-weathered',
        N'dark grey, formerly dark brown', N'short', N'short',
        N'pale blue', N'ruddy and weathered', N'deeply lined',
        N'none',
        N'Unhurried and grounded. Moves the way people move when they have spent decades around animals that read body language — without sudden gestures, without wasted movement.',
        N'Heavy stable wool over a canvas work jacket. His boots are the finest thing he owns and are resoled every two years. He considers good boots a professional obligation.',
        N'None.',
        N'Morning stable rounds before first light. Evening inspection after the last rider returns. Manages the estate''s transport schedules, the farrier appointments, and the feed provisioning. Sleeps in the stable loft three nights a week by preference.',
        N'Seven years ago, at three in the morning, a horse returned to the stable without its rider. The horse belonged to Jorunn Baldersen, a senior officer of House Draught. The animal was unsettled and lathered — it had been ridden hard and returned alone. Skarde unsaddled it, cooled it, bedded it, and recorded nothing. Four days later an official notice announced Baldersen''s death in combat. Skarde still has the horse. He calls it Steady. He has never mentioned the night it came back alone. He does not know that one of the kitchen staff hired six months ago is Baldersen''s daughter.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Drauchtholt estate stable and grounds; transport routes to the market town below the plateau.',
        N'0', N'0',
        N'large weathered man, dark grey hair, pale blue eyes, heavy stable wool, Norse steampunk estate stable, calm expression, medieval fantasy, portrait',
        N'large weathered man, grey hair, pale blue eyes, stable work clothing, medieval fantasy estate stable, calm expression, portrait',
        0, 0
    );
    PRINT 'Skarde Eriksen seeded.';
END
ELSE PRINT 'Skarde Eriksen already exists.';
GO

-- ── Bjorn Lindqvist ───────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Bjorn Lindqvist')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Bjorn Lindqvist', N'bjorn-lindqvist', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Bjorn Lindqvist', N'bjorn-lindqvist', N'Bjorn', N'Lindqvist', N'',
        N'human', N'human', N'male', N'he/him', 18, N'alive',
        N'Groom and stable hand; House Draught estate at Drauchtholt. Young, quiet, observant. The horses trust him more than anyone.',
        N'A young man still filling out — tall but not yet settled into his height, with fair hair that falls across his forehead and the kind of patience with animals that is either innate or the result of very early and very specific damage. He speaks rarely and listens carefully and has the quality of someone who has learned that what gets said in a stable, to a horse, is different from what gets said anywhere else. Skarde regards him as the most naturally gifted groom he has trained in twenty years.',
        N'A witness who has taken a terrible action in silence and has not yet decided what kind of person that makes him. His knowledge of the body in the marsh is an undetonated charge in a story about what the estate protects and what it discards.',
        N'No POV.',
        N'House Draught; Drauchtholt estate; family from a Swedish-heritage fjord settlement; entered stable service at fifteen',
        180, 74, N'young-lean',
        N'fair, long enough to fall forward', N'loose', N'medium',
        N'grey-green', N'fair', N'clear',
        N'none',
        N'Still and careful around animals. Around people, slightly too careful — the posture of someone deciding in real time how much to give away.',
        N'Stable work clothes that are always slightly too large for him — he has not yet finished growing into either his body or his position.',
        N'None.',
        N'Morning stabling and feeding. Evening brushdown and check. Spends his spare time in the stalls rather than the staff quarters. Eats quickly and returns to the horses. Sleeps badly.',
        N'Eight months ago he found a body in the marsh at the estate''s eastern edge — a man, not a battle casualty, without uniform or weapon wounds, with minimal decomposition suggesting recent death. He moved the body deeper into the water using a fence pole, covered the area with marsh grass, and walked back to the stable. He has told no one. He checks the spot twice a week from a distance to confirm the water is undisturbed. He does not know who the man was. He was seventeen when he found it. He has not slept more than five consecutive hours since.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Drauchtholt estate stable and grounds; the marsh path along the eastern boundary.',
        N'0', N'0',
        N'young man eighteen, fair hair falling forward, grey-green eyes, oversized stable work clothes, Norse steampunk estate stable, guarded expression, medieval fantasy, portrait',
        N'young man, fair hair, grey-green eyes, work clothing, medieval fantasy estate stable, guarded expression, portrait',
        0, 0
    );
    PRINT 'Bjorn Lindqvist seeded.';
END
ELSE PRINT 'Bjorn Lindqvist already exists.';
GO

-- ── Eirik Grimsdal ────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Eirik Grimsdal')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Eirik Grimsdal', N'eirik-grimsdal', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Eirik Grimsdal', N'eirik-grimsdal', N'Eirik', N'Grimsdal', N'',
        N'human', N'human', N'male', N'he/him', 64, N'alive',
        N'Groundskeeper; House Draught estate at Drauchtholt. Forty years on these grounds. Knows where things are buried that are not supposed to exist.',
        N'A weathered, solid man with a white beard and soil permanently under his fingernails that he has stopped trying to remove. He has a groundskeeper''s relationship with the estate''s land — intimate, unsentimental, total. He knows every drain, every foundation crack, every place where something was buried and then forgotten by everyone except him. He is unhurried in speech and meticulous in work. He does not gossip. He does not need to.',
        N'The estate''s geological memory. The four concealed sites he maintains are, collectively, a map of the household''s hidden history. Any one of them, surfaced, becomes a story. He has been maintaining them for decades with no stated purpose. The question is what he is waiting for.',
        N'No POV.',
        N'House Draught; Drauchtholt estate; raised locally; entered estate service at twenty-four and has never left',
        181, 89, N'weathered-solid',
        N'white', N'short beard and cropped head', N'short',
        N'pale grey', N'ruddy', N'deeply weathered',
        N'none',
        N'Slow and deliberate. Bends at the knees from decades of habit. Pauses before answering questions, not from slowness but from the habit of being certain.',
        N'Canvas ground-work clothes in brown and grey. Wears the same jacket in all weather — it has been re-waterproofed so many times the original colour is a matter of speculation.',
        N'None.',
        N'Walks the entire grounds at first light. Manages drainage, planting, the outer wall maintenance, and the state of the defensive earthworks. Spends an hour each week at four locations on the grounds that are not noted in any maintenance schedule.',
        N'Over forty years he has located and maintained four things buried on the estate grounds that should not exist: a sealed chest of Liturgy-marked equipment that arrived without a manifest some thirty years ago and was never claimed, which he has relocated twice as drainage works shifted; a cache of Ophiuchus medical texts in waxed waterproof wrapping buried near the north orchard, which he has not opened; the skeleton of a horse that was supposed to have been sold fifteen years ago and was instead ridden to its death in a drainage gully and covered over by someone in a hurry; and a human burial, older than his time at the estate, that he found when he was twenty-four while digging a water channel. He has tended the burial seasonally for forty years, weeding around the stones he placed. He has never told anyone it is there. He is not sure why he keeps doing it.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Drauchtholt estate full grounds, including the outer earthworks and the marsh boundary.',
        N'0', N'0',
        N'weathered old man, white beard, pale grey eyes, canvas ground-work clothes, Norse steampunk estate grounds, deliberate expression, medieval fantasy, portrait',
        N'weathered elderly man, white beard, grey eyes, canvas work clothing, medieval fantasy estate grounds, steady expression, portrait',
        0, 0
    );
    PRINT 'Eirik Grimsdal seeded.';
END
ELSE PRINT 'Eirik Grimsdal already exists.';
GO

-- ── Helga Vidarsen ────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Helga Vidarsen')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Helga Vidarsen', N'helga-vidarsen', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Helga Vidarsen', N'helga-vidarsen', N'Helga', N'Vidarsen', N'',
        N'human', N'human', N'female', N'she/her', 48, N'alive',
        N'House Physician; House Draught estate at Drauchtholt. Treats family and high-ranking staff. Has made one decision she cannot undo.',
        N'A precise woman with dark hair streaked grey at the temples and the characteristic stillness of physicians who have learned that showing what they have just diagnosed does the patient no good. She is competent, trusted, and in possession of everyone''s secrets in the way that medicine inevitably produces — because the body does not lie even when its owner does. She has access to every member of the family and most of the senior staff. She has used that access once in a way that will follow her.',
        N'The physician who falsified a record to protect the succession apparatus — and is now the only person who knows the succession is being built on a fiction. Her second set of private notes in cipher is the most dangerous document in the estate, because it makes her complicity legible.',
        N'No POV.',
        N'House Draught; Drauchtholt estate; trained at the House''s medical collegium; practising at the estate for sixteen years',
        172, 69, N'precise',
        N'dark, greying at temples', N'practical bun', N'medium',
        N'dark brown', N'medium', N'controlled',
        N'none',
        N'Economical and still. Her diagnostic attention — the habit of noticing everything — is visible as a slight pause before she answers questions, as though she is noting your condition before engaging with your words.',
        N'Physician''s dark grey wool over a formal examination coat. Carries a locked case at all times. Her personal effect: a small ring of polished bone on her right hand, origin unspecified.',
        N'None.',
        N'Morning rounds of the family wing. Scheduled examinations for staff by request. Keeps the estate''s medical records in a locked chest in her rooms. Writes in the evenings, in her formal records and in a second document she keeps in a cipher she devised herself.',
        N'Three years ago she diagnosed the Lord''s eldest son — the designated successor — with a progressive neurological condition she estimates will leave him cognitively and physically incapable of command within eight to ten years. She delivered the finding privately to the Lord. The Lord directed her to amend the examination record to reflect normal findings for a man of his age. She amended the record. She has continued treating the son under the notation ''seasonal fatigue and overwork'' while the full succession apparatus — the alliances, the training protocols, the political arrangements — continues to be built around a man who will not be capable of bearing any of it. She has begun keeping a second set of clinical notes in personal cipher. If those notes are ever found, they implicate her. If they are destroyed, the record becomes permanently false.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Drauchtholt estate; medical collegium in the district town on quarterly visits.',
        N'0', N'0',
        N'precise woman, dark hair greying at temples, dark brown eyes, physician''s dark grey coat, medieval Norse estate, controlled expression, Buehlman dark register, portrait',
        N'precise woman, dark hair with grey at temples, dark brown eyes, physician''s coat, medieval fantasy estate, controlled expression, portrait',
        0, 0
    );
    PRINT 'Helga Vidarsen seeded.';
END
ELSE PRINT 'Helga Vidarsen already exists.';
GO

-- ── Sigrid Johannsdottir ──────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Sigrid Johannsdottir')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Sigrid Johannsdottir', N'sigrid-johannsdottir', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Sigrid Johannsdottir', N'sigrid-johannsdottir', N'Sigrid', N'Johannsdottir', N'',
        N'human', N'human', N'female', N'she/her', 54, N'alive',
        N'Chaplain and Bheur Priest; House Draught estate at Drauchtholt. Officiates at all House rites. Has private doubts about what the Liturgy does with the dead.',
        N'A composed woman with silver-streaked dark hair and the particular bearing of someone who has officiated at enough deaths to have either resolved the question of what death means or learned to hold it without resolution. She performs the Bheur rites with complete formal precision. The precision has not been evidence of faith for eleven years. She has not told anyone this. She is not sure anyone would know what to do with the information.',
        N'The theological figure whose private crisis is a direct consequence of Liturgy interference. Her doubt about the Bheur — rooted in a specific, verifiable event — is a thread that connects the household''s spiritual architecture to the question of what the Liturgy actually does to the dead.',
        N'No POV.',
        N'House Draught; Drauchtholt estate; trained at the House''s Bheur seminary in the plateau city; assigned to the estate eighteen years ago',
        164, 68, N'composed',
        N'dark with silver streaking', N'formal coil', N'medium',
        N'grey-brown', N'fair', N'composed and still',
        N'none',
        N'Carries herself with ceremonial uprightness that is entirely automatic — she does not have to think about it anymore. In private conversation, slightly more yielding than her formal posture suggests.',
        N'Formal chaplain''s vestments for rites; plain dark wool for daily work. She keeps the vestments in perfect condition. This is the one act of faith she has not examined.',
        N'None.',
        N'Morning devotions, which she performs from habit and not belief. Manages the House''s ritual calendar. Counsels family members who request it. Officiates at births, marriages, deaths, and the seasonal war-rites that House Draught observes. Sits with the dying.',
        N'Eleven years ago she administered last rites to a man who met every criterion for death by both medical and theological standards — she watched the breathing cease, held his wrist for four full minutes and found no pulse, and spoke the complete Bheur commendation over him. Six hours later the man walked out of the Liturgy preparation chamber unassisted. Two days after that, the Liturgy sent a note describing the event as ''a notation error; subject transferred, not deceased.'' She has performed every subsequent death rite with the same formal precision and without conviction. She does not know what the Bheur is. She suspects the Liturgy does not know either, and that this does not trouble the Liturgy the way it troubles her.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Drauchtholt estate; the House''s ritual sites in the surrounding district.',
        N'0', N'0',
        N'composed woman, dark silver-streaked hair in formal coil, grey-brown eyes, chaplain''s dark vestments, medieval Norse estate chapel, serious expression, Buehlman dark register, portrait',
        N'composed woman, dark hair with silver, grey-brown eyes, formal chaplain vestments, medieval fantasy estate chapel, serious expression, portrait',
        0, 0
    );
    PRINT 'Sigrid Johannsdottir seeded.';
END
ELSE PRINT 'Sigrid Johannsdottir already exists.';
GO

-- ── Gunnar Thorsson ───────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Gunnar Thorsson')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Gunnar Thorsson', N'gunnar-thorsson', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Gunnar Thorsson', N'gunnar-thorsson', N'Gunnar', N'Thorsson', N'',
        N'human', N'human', N'male', N'he/him', 16, N'alive',
        N'Page; House Draught estate at Drauchtholt. Young messenger. Hears everything he is not supposed to. Will become something important.',
        N'A wiry adolescent with the slightly unfinished quality of a face that has not decided yet what it will settle into. He runs messages through the estate quickly and efficiently and has been doing so since he was thirteen. He has an excellent memory and has been told so often enough that he now treats it as a professional resource. He is frequently in the room when important things are said, because pages are invisible in the way that furniture is invisible, until someone needs to sit down.',
        N'The accidental archivist. He started collecting overheard information because it was interesting. He has since built something considerably more dangerous. He represents the question of what the household''s secrets look like when assembled by someone who has no stake in keeping them.',
        N'No POV.',
        N'House Draught; Drauchtholt estate; family from the estate''s dependency village; entered page service at thirteen',
        172, 60, N'wiry-adolescent',
        N'sandy brown', N'short and slightly unruly', N'short',
        N'hazel', N'fair', N'unfinished, youth',
        N'none',
        N'Quick-moving and apparently inattentive. He has learned that looking purposeful and slightly rushed is the best camouflage for lingering near a conversation.',
        N'Page''s livery in the House colours. He keeps it neat because he has understood that neatness makes people stop seeing you.',
        N'None.',
        N'Runs messages throughout the estate from early morning. Attends formal occasions as a runner. Has no fixed evening duty, which means no one tracks his movements after the dinner hour.',
        N'He began memorising overheard conversations three years ago because he was bored and found he could do it. He later devised a shorthand notation system — symbols derived from the estate''s accounting marks — and began transcribing each evening. He now has fourteen months of household intelligence stored in a hollowed-out devotional text he keeps in his room: conversations between the Lord and his officers, between Gudrun and the senior staff, between family members who forgot he was in the hallway. He has recently begun to understand some of what he has recorded. He knows that this is dangerous. He has not stopped because he does not know how to stop something he started for a reason that no longer applies.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Drauchtholt estate interior, every wing and corridor.',
        N'0', N'0',
        N'teenage boy sixteen, sandy hair, hazel eyes, page livery, Norse medieval estate corridor, quick alert expression, Buehlman dark register, portrait',
        N'teenage boy, sandy hair, hazel eyes, page livery, medieval fantasy estate corridor, alert expression, portrait',
        0, 0
    );
    PRINT 'Gunnar Thorsson seeded.';
END
ELSE PRINT 'Gunnar Thorsson already exists.';
GO

-- ── Colm Halvorsen ────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Colm Halvorsen')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Colm Halvorsen', N'colm-halvorsen', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Colm Halvorsen', N'colm-halvorsen', N'Colm', N'Halvorsen', N'',
        N'human', N'human', N'male', N'he/him', 58, N'alive',
        N'Laundry Master; House Draught estate at Drauchtholt. The person everyone underestimates. Has been reading correspondence left in pockets for twenty years.',
        N'A solid, unremarkable man with a round face and grey eyes and the quality of near-invisibility that long practice at an unregarded job produces. He manages the estate''s laundry — the full weight of it, for a household of this size, which is considerable — with the efficiency of someone who has reduced the work to a system and the system to muscle memory. He does not socialise above his station and does not draw attention to himself. He has not, in twenty years, ever been asked what he found in a pocket.',
        N'The estate''s unwitting intelligence service. He is the only person in the household who has held the Liturgy''s list with his own name on it and stayed silent. His reason for staying — and the copy he keeps — are the most specific and particular secrets in this batch.',
        N'No POV.',
        N'House Draught; Drauchtholt estate; Irish-heritage family two generations settled in the Draught port district; entered laundry service at thirty-six after previous work in the harbour warehouses',
        174, 83, N'solid-unremarkable',
        N'grey-brown, thinning', N'short and plain', N'short',
        N'grey', N'fair', N'unremarkable',
        N'none',
        N'Methodical and unhurried. Gives the impression of a man who has nowhere particular to be, which is the impression he has cultivated for twenty years.',
        N'Laundry work-clothes in undyed wool. He wears the same clothes to work every day and considers this sensible. He has one good wool jacket for the occasions when Gudrun requires the full household staff to be presentable.',
        N'None.',
        N'Manages the laundry operation from early morning. Sorts pockets as a routine step before washing. Has done this for twenty years. Processes what he finds — correspondence, notes, coins, personal effects — with complete discretion. Returns everything. Checks the lining of a specific laundry basket every morning.',
        N'Twelve years ago he found a letter in the inner breast pocket of the Lord''s riding coat. The letter was from the Liturgy, on formal stationery, listing three names under the heading "Voluntary Transit — Scheduled." Two of the names were household servants who disappeared that month. The third name was his own. He replaced the coat with the letter exactly as he had found it, as though he had found nothing at all. That evening he wrote a precise copy of the letter from memory and concealed it in the linen liner of a laundry basket he has used for twenty-two years. He checks the basket every morning. He has not decided what the copy is for. He wonders, sometimes, whether the Liturgy changed its mind about him or simply forgot. He has never found an answer. He is no longer sure he wants one.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Drauchtholt laundry and estate service corridors; the provisioning route to the cloth merchants in the town below.',
        N'0', N'0',
        N'solid unremarkable man, greying brown hair, grey eyes, undyed wool work clothes, medieval Norse estate laundry, neutral expression, Buehlman dark register, portrait',
        N'solid middle-aged man, grey eyes, plain wool work clothing, medieval fantasy estate laundry, neutral expression, portrait',
        0, 0
    );
    PRINT 'Colm Halvorsen seeded.';
END
ELSE PRINT 'Colm Halvorsen already exists.';
GO

-- ── Orm Dagsson ───────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Orm Dagsson')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Orm Dagsson', N'orm-dagsson', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Orm Dagsson', N'orm-dagsson', N'Orm', N'Dagsson', N'',
        N'human', N'human', N'male', N'he/him', 61, N'alive',
        N'Head of Household Guards; House Draught estate at Drauchtholt. Guards the Lord and Lady''s private quarters at night. Old. Reliable. Carrying something he has never spoken aloud.',
        N'A thick-set man with a face heavily scarred from two campaigns he served in before transitioning to household guard duty twenty-three years ago. His hair is white and close-cropped. He moves through the estate''s private corridors at night with the specific authority of someone who has the Lord''s personal confidence. He is not a Myrmidon. He is something quieter and, in certain respects, more dangerous: the man the Lord trusts with physical proximity at his most vulnerable, which requires a different kind of loyalty than combat.',
        N'The man who knows the previous Lord''s treason and has chosen to carry it to his death rather than disrupt the current Lord''s House. His loyalty is informed and deliberate — he has weighed the cost of speaking and decided the cost of silence is lower. The question is whether that arithmetic will hold.',
        N'No POV.',
        N'House Draught; Drauchtholt estate; veteran of two Living War campaigns; household guard for twenty-three years',
        179, 92, N'thick-scarred',
        N'white, close-cropped', N'close-cropped', N'short',
        N'pale blue', N'weathered', N'scar-marked',
        N'none',
        N'Still and watchful. Does not shift his weight unless he is moving with purpose. His stillness is not relaxation — it is the readiness state of a man who has spent twenty-three years in corridors that require it.',
        N'Guard''s dark wool over a padded underjacket he has worn so long it has moulded to his shape. His one personal item: a short blade he has carried since his second campaign that he has never been without, indoors or out.',
        N'None.',
        N'Posts at the Lord''s private wing from the dinner hour until first light, four nights in seven. The other three nights go to two subordinate guards he has trained and trusts conditionally. Sleeps in the day. Eats in the evening. Has no household relationships except with Gudrun, who ensures his meals are timed to his schedule.',
        N'Twenty-three years ago he was posted outside the Lord''s study while the previous Lord met privately with a Liturgy envoy. The door was old and the acoustics of that corridor are particular. He heard the previous Lord agree, in precise terms, to provide ''transit-ready subjects from within the household population'' at a rate of delivery the envoy specified, in exchange for a political intervention the Liturgy would make on the House''s behalf in an ongoing territorial dispute. The previous Lord died of a chest illness two years later. Orm has served the current Lord faithfully since. He does not believe the son knows what the father arranged. He has decided, with full deliberation, to carry this until he dies. He is sixty-one and in reasonable health. He expects to have a few more years.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Drauchtholt estate private quarters and residential wing; the approach corridors to the Lord''s suite.',
        N'0', N'0',
        N'thick-set scarred man, white close-cropped hair, pale blue eyes, dark guard''s wool, medieval Norse estate corridor, watchful expression, Buehlman dark register, portrait',
        N'thick-set man, white hair, scarred face, dark guard clothing, medieval fantasy estate corridor, watchful expression, portrait',
        0, 0
    );
    PRINT 'Orm Dagsson seeded.';
END
ELSE PRINT 'Orm Dagsson already exists.';
GO

-- ── Sif Ragnarsen ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Sif Ragnarsen')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Sif Ragnarsen', N'sif-ragnarsen', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Sif Ragnarsen', N'sif-ragnarsen', N'Sif', N'Ragnarsen', N'',
        N'human', N'human', N'female', N'she/her', 44, N'alive',
        N'Household seamstress and mender; House Draught estate at Drauchtholt. Twenty-five years repairing uniforms returned from the Living War. Has been counting the damage.',
        N'A medium-built woman with auburn hair kept back with a strip of cloth she cuts fresh each morning from the linen remnants. Her hands are precise and calloused. She repairs everything the estate produces and receives — household linens, family clothing, and the uniforms returned from the Living War. She does not gossip and is not sought for conversation. She is, consequently, left entirely alone with her work and her observations.',
        N'The accidental military analyst. She does not have the context to fully interpret what she has read in the damage patterns, but she has the data — and she has begun marking certain uniforms with a private notation. The mark is a thread. If someone reads it, everything she knows becomes transferable.',
        N'No POV.',
        N'House Draught; Drauchtholt estate; trained in the estate''s household craft program at fifteen; has worked there since',
        165, 67, N'medium',
        N'auburn', N'strip-cloth back', N'medium',
        N'hazel', N'fair', N'needle-marked on the fingertips',
        N'none',
        N'Compact and economical. She spends most of her time bent over work, which gives her a slight forward tilt in conversation that reads as attentiveness and is.',
        N'Plain work-wool in dark blue. Her own choice — she decided early that dressing plainly meant the family stopped looking at her, and has maintained that logic ever since.',
        N'None.',
        N'Works from morning to evening in the repair room off the main laundry. Receives uniforms from the estate''s military dispatch point and processes them in the order received. Keeps her own inventory of what comes in and goes out, separate from the official dispatch log.',
        N'She has been cataloguing uniform damage patterns for fifteen years, first in memory and later in a private notation system she keeps in the margins of her personal inventory book. Three separate engagements — described in official House dispatches as ''swift actions with minimal contact'' — produced uniforms consistent with prolonged close-quarters fighting: multiple overlapping blade repairs, boot leather shredded from sustained running on broken ground, collar and shoulder damage inconsistent with the mounted engagements officially reported. She has begun marking those uniforms with a single thread of undyed linen sewn into a secondary seam before they go back out — a mark she knows means nothing to anyone who does not know to look for it. She does not know what she intends to do. She knows the information is real.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Drauchtholt estate repair room; the dispatch corridor to the household''s military intake point.',
        N'0', N'0',
        N'medium woman, auburn hair in cloth strip, hazel eyes, dark blue work-wool, medieval Norse estate repair room, intent expression, Buehlman dark register, portrait',
        N'woman in her forties, auburn hair, hazel eyes, dark wool work dress, medieval fantasy estate sewing room, intent expression, portrait',
        0, 0
    );
    PRINT 'Sif Ragnarsen seeded.';
END
ELSE PRINT 'Sif Ragnarsen already exists.';
GO

-- ── Gudlaug Eriksen ───────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Gudlaug Eriksen')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Gudlaug Eriksen', N'gudlaug-eriksen', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Gudlaug Eriksen', N'gudlaug-eriksen', N'Gudlaug', N'Eriksen', N'',
        N'human', N'human', N'female', N'she/her', 39, N'alive',
        N'Oathless; formerly Dame Gudlaug Eriksen of House Draught. The House uses her for deniable operations. The arrangement is transactional on both sides and the history between them is not simple.',
        N'A lean woman with close-cropped dark hair and the Knight''s subtle augmentation visible in the precise way she carries her additional height — she stands eight centimetres above her baseline and the density of her is wrong in the way that augmented people are wrong, the body slightly too present for its volume. She went Oathless nine years ago and the House has never publicly acknowledged why. The work they send her is the kind that cannot be traced back to a House that officially has no contact with her.',
        N'The deniable instrument with a private purpose. She takes the House''s contracts not for the payment but for proximity — she needs to remain close enough to the House''s operational chain to identify a specific officer. The transactional surface of the arrangement conceals an investigation that has been running for nine years.',
        N'No POV.',
        N'House Draught; Oathless for nine years; formerly assigned to the House''s western operations cohort; current location variable',
        178, 79, N'lean-augmented',
        N'dark brown, close-cropped', N'close-cropped', N'short',
        N'dark grey', N'fair', N'weathered',
        N'Subtle height gain, increased density',
        N'Contained and deliberate. Her augmented build reads in the way she takes up space — she is not large but she is more present than her dimensions suggest. Moves with the specific economy of someone trained for long field operations.',
        N'Practical travelling wool in dark grey and brown — nothing that marks her for any House. No insignia. Nothing personal except a folded piece of paper she keeps in an interior pocket she checks by habit.',
        N'Knight-grade Transmutation; Xerum 525 infusion series completed fifteen years ago; full survival; height elevated approximately 8cm above baseline; bone density significantly above human norm; healing rate improved. No further infusions since going Oathless.',
        N'Takes the contracts the House sends through an intermediary. Completes them efficiently and without contact with House officers directly. Lives in a rented room in the town below the plateau when not on assignment. Spends the time between contracts reviewing what she knows and what she still needs to find.',
        N'Nine years ago she ran a deniable operation for House Draught — the word used in the brief was "neutralisation" — against a person described to her as an active Liturgy intelligence asset working against the House''s interests. She executed the assignment cleanly. Three months later she learned through an informant in a neutral House that the person she killed had been a Sphere 31 taking who had been used by a senior House officer as expendable bait in a separate operation and then listed as an intelligence threat to justify her removal once the operation concluded. Gudlaug went Oathless within the month. She takes the House''s contracts now because she needs to remain inside their operational chain long enough to confirm the name of the officer who constructed that brief. She has narrowed it to two people. She is not in a hurry. She is very patient.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Variable; operates throughout House Draught territory and its borders; rented room in the market town below Drauchtholt between assignments.',
        N'0', N'0',
        N'lean woman, close-cropped dark hair, dark grey eyes, dark grey travelling wool, no insignia, Oathless former Knight, medieval Norse steampunk, contained expression, Buehlman dark register, portrait',
        N'lean woman, short dark hair, dark eyes, grey-brown travelling clothes, no insignia, medieval fantasy, contained expression, portrait',
        0, 0
    );
    PRINT 'Gudlaug Eriksen seeded.';
END
ELSE PRINT 'Gudlaug Eriksen already exists.';
GO

-- ── Maris Volkov ──────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Maris Volkov')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Maris Volkov', N'maris-volkov', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Maris Volkov', N'maris-volkov', N'Maris', N'Volkov', N'',
        N'human', N'human', N'male', N'he/him', 47, N'alive',
        N'Oathless; sheltering in House Draught territory. The House knows, tolerates it, and has never stated why. The reason is specific and has never been spoken aloud by either party.',
        N'A composed man with dark Slavic features and a stillness that reads as either patience or absolute self-control — he has never given anyone the occasion to determine which. He has been sheltering in the territory below Drauchtholt for six years. He does not interact with the estate. He lives in the lower market town, pays for what he takes, and has never once named his reason for being here. Neither has anyone from the estate asked.',
        N'The unspoken debt made visible. His presence in House territory is a permanent, passive reminder of what the previous Lord did. He does not need to threaten. The geometry of the arrangement does that for him. He is here because this is the safest place on the continent for a man who holds what he holds.',
        N'No POV.',
        N'Oathless; Slavic heritage from the eastern Cauld territories; current residence in the market town below Drauchtholt; prior location undisclosed',
        177, 82, N'composed',
        N'dark brown with grey threads', N'short and neat', N'short',
        N'dark brown', N'medium-olive', N'composed',
        N'none',
        N'Entirely still when not moving. When he moves, it is deliberate and unhurried. He has the quality of someone who has never needed to prove anything and is aware that this is itself a proof of something.',
        N'Practical clothes in dark wool — nothing that marks him as belonging to any House or faction. He dresses to be forgotten and has largely succeeded.',
        N'None.',
        N'Lives quietly in the market town. Keeps a small room. Eats at a particular table in a particular inn at particular hours. Has a small number of local acquaintances who know nothing about him except that he pays reliably and asks very little. Reads. Waits.',
        N'Thirty years ago he witnessed — and documented — an incident in which the previous Lord of House Draught ordered the summary execution of a group of seventeen refugees from a neutral settlement, in violation of the inter-House compact governing non-combatant populations. The documentation is precise: names, dates, a map of the site, and the previous Lord''s order in his own recorded words. If produced and authenticated, it would constitute grounds for stripping the current Lord''s family of its inter-House alliance standing retroactively. He has never threatened to produce it. He does not need to. The current Lord''s family understands the arrangement without its terms ever having been stated. He is here because both parties find this preferable to the alternative. He has never told anyone where the documentation is. He has not decided what he will do with it when he dies.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Market town below Drauchtholt; House Draught territory by tolerance; does not enter the estate.',
        N'0', N'0',
        N'composed man, dark olive skin, dark brown hair with grey, dark wool, medieval Norse market town, still watchful expression, Buehlman dark register, portrait',
        N'composed man, olive skin, dark hair with grey, dark plain wool, medieval fantasy market town, still expression, portrait',
        0, 0
    );
    PRINT 'Maris Volkov seeded.';
END
ELSE PRINT 'Maris Volkov already exists.';
GO

-- ── Niri Svensdottir ──────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Niri Svensdottir')
BEGIN
    DECLARE @id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@id, N'character', N'Niri Svensdottir', N'niri-svensdottir', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @id, N'Niri Svensdottir', N'niri-svensdottir', N'Niri', N'Svensdottir', N'',
        N'human', N'human', N'female', N'she/her', 19, N'alive',
        N'Kitchen servant; House Draught estate at Drauchtholt. Youngest kitchen staff. Hired six months ago. Her reason for being here and Skarde Eriksen''s secret are connected in a way neither of them knows.',
        N'A young woman with dark eyes and light brown hair she keeps tied back, still learning what expressions are appropriate for which situations in a household at this level. She arrived six months ago, works hard, asks sensible questions, and has already been noted by Ingrid Haugen as someone worth teaching. She does not talk much about her family. She does not talk much at all.',
        N'The accidental revelation waiting to happen. Her connection to Jorunn Baldersen — the officer whose horse came home alone, whose death Skarde witnessed was not what the record says — sits latent in the kitchen. Skarde has seen her face. The question of whether he will speak is the story''s pivot.',
        N'No POV.',
        N'House Draught; Drauchtholt estate; family from the dependency settlements below the plateau; father died in the Living War when she was twelve',
        168, 58, N'young',
        N'light brown', N'tied back', N'medium',
        N'dark brown', N'medium-fair', N'clear',
        N'none',
        N'Slightly guarded and observant. She is learning the rhythms of the household and has not yet relaxed into them. Around the horses, on the few occasions she has had cause to cross the stable yard, she has paused in a way Skarde Eriksen has noticed.',
        N'Plain kitchen wool. New, still too stiff. She has not been here long enough for the estate''s fabric to become her own.',
        N'None.',
        N'Morning kitchen preparation, midday service, afternoon cleaning and prep. Learns quickly. Takes correction without resentment, which Ingrid has noticed. Returns to her room in the dependency quarters in the evenings and writes letters home to her mother that she sends once a month.',
        N'She is the illegitimate daughter of Jorunn Baldersen — the House Draught officer whose horse returned to Skarde''s stable alone, at three in the morning, four days before his death was officially announced. She does not know this. She knows her father''s name. She was told he died in combat when she was twelve. She has heard the name Baldersen once since arriving at the estate — Leif Hakonsen mentioned it in passing in the context of an old provisioning dispute — and felt something she could not explain and then let it go. Skarde Eriksen has seen her face twice. He has not spoken. He is sixty-one. He has not decided when the right time would be, or if there is one.',
        N'Not applicable.', N'Not applicable.',
        N'Not applicable.', N'Not applicable.', N'Not applicable.',
        N'Drauchtholt estate kitchen and dependency quarters; the dependency village below the plateau.',
        N'0', N'0',
        N'young woman nineteen, light brown hair tied back, dark brown eyes, plain kitchen wool, medieval Norse estate kitchen, guarded expression, Buehlman dark register, portrait',
        N'young woman, light brown hair, dark eyes, plain wool kitchen dress, medieval fantasy estate kitchen, guarded expression, portrait',
        0, 0
    );
    PRINT 'Niri Svensdottir seeded.';
END
ELSE PRINT 'Niri Svensdottir already exists.';
GO
