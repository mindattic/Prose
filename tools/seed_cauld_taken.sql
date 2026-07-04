SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- CAULD UNIVERSE — ZORA MATIĆ
-- A woman taken from her own Sphere via witnessed pierce arranged by a senior
-- Corvus figure. She recognizes the weapons. She knows the marks.
-- Universe: fantasy-steampunk (ID 0197E9C9-0002-7000-8000-000000000002)
-- 2026-07-04
-- ═══════════════════════════════════════════════════════════════════════════════

IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Zora Matić')
BEGIN
    DECLARE @zoraId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@zoraId, N'character', N'Zora Matić', N'zora-matic', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @zoraId, N'Zora Matić', N'zora-matic', N'Zora', N'Matić', N'',
        N'human', N'human', N'female', N'she/her', 29, N'alive',
        N'Taken via witnessed pierce; brought to House Corvus by arrangement with the Liturgy; origin Sphere unknown to the Cauld''s catalogues; not a prisoner in name, not free in practice',

        N'Zora Matić was in her own city, going about her own life. She turned a corner — or reached for something on a shelf, or stepped through a doorway — and then she was in a stone chamber. Robed figures. A man she had never seen who knew exactly who she was. She had not been warned. She had not been asked. She arrived.

She is in the Cauld now. She has been for six months. She has learned enough of the language to understand what is said to her and not enough to say everything she thinks. She is housed in Corvus territory as a ward of the senior figure who arranged her arrival. She is called a guest. The distinction between guest and property is maintained by no one with any particular conviction.

What no one in the Cauld has accounted for: Zora comes from a Sphere where these weapons exist. She has handled rifles like the ones Corvus soldiers carry. She has seen proof stamps. She knows what a factory roll mark looks like, and she knows what it looks like when someone has put a plate over one. She has been in the Cauld for six months and she has not said this to anyone, because she is still working out who would benefit from knowing she knows.',

        N'The story''s access to seeing the Cauld from outside it. The one person present who has a frame of reference — who recognizes the weapons, the covered marks, the lazy obliteration of evidence that would take two minutes to read if anyone looked at it with the right eyes. She is also the story''s access to what the witnessed pierce actually costs: not dramatically, not mystically, but practically — the specific experience of one moment being where you were and the next moment being here, with no transition, no warning, no explanation that makes sense in any framework she came in with.',

        N'Zora Matić does not have a POV yet in the current design, but she is written toward one. Third-close. Her register is dry, specific, and oriented toward the physical detail of things — the result of a person whose primary survival strategy in an incomprehensible situation is to observe carefully and commit nothing to anyone until she understands the board.',

        N'A Sphere the Cauld has no catalogue name for; she calls it home; she does not describe it to anyone in the Cauld because she cannot tell yet whether describing it would help her',
        168, 62, N'practical; the build of someone who moved through the world physically before all of this',
        N'dark brown', N'worn; whatever arrangement she arrived in has been maintained because the alternatives require resources she does not have', N'shoulder-length',
        N'brown', N'warm medium-dark', N'clear; the specific quality of a person who has been functioning under sustained stress without breaking — not unaffected, just not broken yet',
        N'none',
        N'watchful; very still in rooms she does not control; moves economically when she does move; the posture of someone calibrating constantly',
        N'what she arrived in, supplemented by what has been provided; she wears nothing that marks her as belonging to Corvus or any House; this is not a choice anyone offered her — she simply does not have House-marked clothing and has not been given it, which is itself a statement about her status',
        N'none',
        N'She is in Corvus territory, in the household of the man who arranged her arrival. She is not confined to a room but her movement is understood to be bounded by what is acceptable to him. She has been learning the language from observation. She has been mapping the installation — not conspicuously, not with materials that could be found, just in her head, the way you map a space when you are thinking about exits.',

        N'Zora Matić recognizes the weapons. She comes from a Sphere where bolt-action rifles of the pattern Corvus soldiers carry were manufactured in factories whose names she knows. She has seen a Scrying notation block on a crate in the installation — SCRY-1918-DE-31 — and she knows, from context she cannot explain to anyone here without explaining everything, that the date and the designation mean something specific. She has found a rifle in the installation with a stock that has been replaced and a plate over the roll mark that does not sit flush. She did not touch it. She looked at it for four seconds and moved on. She has not decided yet what to do with what she knows, but she knows that what she knows is the most dangerous thing she is carrying in the Cauld, and the only thing she arrived with.',

        N'Specific, spare, and mostly in the language of her origin Sphere because she does not yet have the Cauld vocabulary for precision; she uses the Cauld language for social navigation and falls back to her own for anything that matters',
        N'Slow and deliberate — partly because she is working in a language she is still acquiring, partly because she has learned that in this household, words said quickly are words said carelessly',
        N'Almost always asking what the other person already knows and whether telling them this thing is an advantage or a liability',
        N'Goes quieter rather than louder; answers questions with questions; has never raised her voice in the Cauld and does not intend to',
        N'Does not have one; intimacy is not available to her in her current position and she is not pretending otherwise',
        N'Corvus installation and surrounding territory; the limits of what the household''s understanding of her status permits',
        0, 0,
        N'woman in her late twenties, warm medium-dark skin, dark brown shoulder-length hair worn simply, watchful expression, very still posture, practical clothing without House markings, House Corvus stone interior, WW1-adjacent fantasy architecture, Buehlman dark fantasy register, the specific quality of a person who is mapping every room she enters',
        N'watchful young woman, practical clothing, stone interior, dark fantasy, no House markings, still posture',
        0, 0
    );
    PRINT 'Zora Matić seeded.';
END
ELSE PRINT 'Zora Matić already exists.';
GO

PRINT 'Seed complete.';
