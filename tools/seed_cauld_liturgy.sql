SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- CAULD UNIVERSE — THE LITURGY SEED
-- Run: sqlcmd -S "(localdb)\MSSQLLocalDB" -d StreetSamurai -i tools\seed_cauld_liturgy.sql
-- 2026-07-04 | The Liturgy (faction + 3 characters) + 4 Relics (equipment)
-- Universe: fantasy-steampunk (ID 0197E9C9-0002-7000-8000-000000000002)
-- ═══════════════════════════════════════════════════════════════════════════════


-- ╔═══════════════════════════════════════════════════════════════════════╗
-- ║  THE LITURGY — faction                                               ║
-- ╚═══════════════════════════════════════════════════════════════════════╝

IF NOT EXISTS (SELECT 1 FROM Factions WHERE Name = N'The Liturgy')
BEGIN
    DECLARE @liturgyId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@liturgyId, N'faction', N'The Liturgy', N'the-liturgy', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Factions (Id, Name, Slug, Sector, Tier, Allegiance, Motto, Description, Ideology, Territory, Leadership, NarrativeFunction, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @liturgyId, N'The Liturgy', N'the-liturgy',
        N'religious / scientific',
        N'Independent Sect',
        N'Formally unaffiliated with any House; predates the current coalition structure; will remain after it',
        N'The loyal are seen. The seen are changed.',
        N'The religious-scientific sect that administers the Gifted Ceremony and distributes Relics. They existed before the current Houses. They will exist after. Their stated purpose is the identification of the loyal and the rewarding of that loyalty with the Gifts: the Ceremony (biological transformation via alien material) and Relics (physical objects of alien origin that enhance capability without infusion). They do not fight wars. They do not take territory. They arrive. They assess. They administer or they don''t. They leave. No House has ever successfully placed an agent inside the Liturgy at Canon level or above. No House has ever successfully forced a Lector to administer to someone the Lector assessed as unworthy. Both of these facts are known. Neither is discussed at coalition tables.',
        N'Loyalty is the only currency that matters and the only currency that cannot be counterfeited — not permanently, not before a Lector notices. The alien material does not lie. The Ceremony reveals the subject. The Ceremony rewards the revealed. The Houses believe they control this instrument. The Liturgy has allowed them to believe this because the Houses provide the population from which the loyal emerge. The arrangement is convenient. It is not permanent. The Silence will decide when it ends.',
        N'Distributed; the Liturgy maintains no fixed territory. Lectors travel continuously. The Liturgy''s internal facilities — where Canon-level proceedings occur and where alien material is held — are not on any House map.',
        N'The Silence (governing body; never publicly identified). Canon-tier members (senior practitioners; identifiable by the absence of House affiliation markings; formally addressed as Canon followed by their given name). Lectors (field practitioners; the face of the Liturgy that soldiers see).',
        N'The load-bearing institution under the entire power structure of the Cauld. Without the Liturgy''s cooperation, the Houses cannot administer the Ceremony. Without the Ceremony, the loyalty instrument collapses. Without the loyalty instrument, the Houses'' claim to authority over their Myrmidons becomes logistical rather than sacred. The Liturgy is the story''s proof that the most powerful entity in the Cauld does not hold a front, a Scrying installation, or a Catalyst refinery. It holds the franchise.',
        N'medieval religious-scientific order, Liturgy Lectors in travel-worn ceremonial robes marked with alien-material sigils, no House insignia, WW1-era European aesthetic translated into dark fantasy ceremony, alien material contained in precise ritual equipment, Buehlman dark fantasy register, candlelight and precision',
        N'medieval religious order, ceremonial robes, alien material ritual equipment, no faction markings, dark fantasy',
        0, 0
    );
    PRINT 'The Liturgy seeded.';
END
ELSE PRINT 'The Liturgy already exists.';
GO


-- ╔═══════════════════════════════════════════════════════════════════════╗
-- ║  LITURGY CHARACTERS                                                  ║
-- ╚═══════════════════════════════════════════════════════════════════════╝

-- ── Lector Orin Caul — field practitioner, active in the western coalition ───
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Lector Orin Caul')
BEGIN
    DECLARE @orinId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@orinId, N'character', N'Lector Orin Caul', N'lector-orin-caul', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @orinId, N'Lector Orin Caul', N'lector-orin-caul', N'Orin', N'Caul', N'Lector',
        N'human', N'human', N'male', N'he/him', 47, N'alive',
        N'Liturgy Lector; field practitioner; current posting covers the western coalition front and Caul Mor',
        N'Lector Orin Caul has been administering the Gifted Ceremony for nineteen years, which means he has watched approximately four hundred people die from first infusion and watched approximately one hundred survive it. He has distributed sixty-three Relics. He knows exactly how many of each. He does not keep a written record. The numbers are the kind that stay. He carries the alien material in a sealed case of a material no House metallurgist has been able to identify. The case is not locked. He has never been asked what would happen if someone tried to take it. He carries no weapon. He travels alone. The Houses provide escort when he requests it; he requests it rarely and only to protect the material from accidental contact, not to protect himself.',
        N'The Liturgy''s face at soldier-level. The person through whom the Houses'' relationship with the Liturgy actually operates. What he reports to his Canon, and what his Canon reports upward to the Silence, is the story''s access to what the Liturgy actually knows about the state of loyalty in the western coalition.',
        N'The Liturgy does not take a POV in prose in the current design; Orin appears from outside, in other characters'' sections — as the arriving presence, the assessment, the departure',
        N'Traveling practitioner; no fixed origin; his accent suggests the eastern interior but he has not confirmed this',
        178, 74, N'lean and self-contained; the build of someone who walks long distances regularly and does not carry unnecessary weight',
        N'grey-brown, fading',
        N'practical; tied back when working',
        N'medium',
        N'dark grey',
        N'medium-dark; weathered by constant travel',
        N'permanently weathered; the specific look of a person who sleeps in many different conditions',
        N'none (Lectors do not receive the Ceremony; this is Liturgy doctrine; whether this is sacrifice or policy is not discussed)',
        N'present; the specific quality of attention that soldiers find either comforting or disconcerting depending on what they are hiding; he does not fidget; he does not reassure; he listens',
        N'travel-worn ceremonial robes over functional travel clothing; the robes carry alien-material sigils worked into the fabric; no House insignia of any kind; the sealed case always present',
        N'none',
        N'Travel between assessment postings. Arrive. Speak with nominated soldiers, and sometimes soldiers who have not been nominated but the Lector has decided to speak with anyway. Assess. Administer or decline. Distribute Relics when the assessment determines. Depart. File a report to his Canon through a channel that does not go through any House communication system.',
        N'Lector Orin Caul declined the Ceremony himself when he entered the Liturgy as a practitioner — this is standard doctrine. What is not standard is that he was offered the Ceremony before he entered the Liturgy, by House Ophiuchus, and declined that too. He has never explained this to anyone inside the Liturgy. He has never explained it to anyone inside a House. The assessment he made of himself at that point — whatever it was — is the thing that drove his entry into the Liturgy rather than the ascendance track. He knows something about his own worthiness assessment that he has not disclosed.',
        N'Direct; precise without being cold; the vocabulary of someone who has learned to say what he means in the minimum words, because the people he speaks with are often afraid and elaboration does not help them',
        N'Measured; calibrated to the person he is speaking with; slower with people who need more time; not slower with people who do not',
        N'Always asking what the person in front of him actually believes, beneath what they are saying',
        N'Identical to his normal register; the Liturgy trains for this; a Lector who changes under pressure is a Lector who cannot be trusted to assess under pressure',
        N'Does not have one in the conventional sense; the closest thing is the quiet that follows an assessment in which he has told someone they will not receive the Ceremony today',
        N'Western coalition front; Caul Mor; wherever the active nomination queue sends him',
        0, 0,
        N'Liturgy Lector, male, medium-dark weathered skin, greying hair tied back, travel-worn ceremonial robes with alien-material sigils, sealed alien-material case, present watchful quality, western front context, Buehlman dark fantasy register, no House markings',
        N'Liturgy Lector, ceremonial robes with strange sigils, sealed case, watchful, dark fantasy',
        0, 0
    );
    PRINT 'Lector Orin Caul seeded.';
END
GO

-- ── Canon Sibylle Vaur — senior Liturgy authority, western canonical district ─
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Canon Sibylle Vaur')
BEGIN
    DECLARE @sibylleId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@sibylleId, N'character', N'Canon Sibylle Vaur', N'canon-sibylle-vaur', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @sibylleId, N'Canon Sibylle Vaur', N'canon-sibylle-vaur', N'Sibylle', N'Vaur', N'Canon',
        N'human', N'human', N'female', N'she/her', 63, N'alive',
        N'Liturgy Canon; senior authority; oversees Lector assignments and worthiness assessments for the western and central districts; direct channel to The Silence',
        N'Canon Sibylle Vaur has been in the Liturgy for forty-one years. She was a Lector for twenty-two of them before the Silence elevated her to Canon, which means she administered the Ceremony approximately nine hundred times, watched approximately seven hundred and twenty people die from first infusion, and carries every one of those numbers in the same way Lector Caul carries his. She is the Silence''s voice in the western coalition''s territory, which means she is the person who decides whether a Lector''s declined assessment stands or gets escalated, and whether an unusual Relic distribution pattern requires a report above her level. She has an office in Caul Mor under a name that does not appear in any House record. She has attended every coalition table negotiation for the past twelve years without being on the attendance list.',
        N'The story''s access to Liturgy authority above the Lector level. The person who actually receives the reports on what the Houses are doing and what the Lectors are observing. Her presence at coalition tables — invisible and unlisted — is the story''s clearest demonstration that the Liturgy is not a passive instrument.',
        N'Canon Sibylle Vaur does not take a POV in the current design; she appears at the canonical authority level from outside',
        N'The interior; spent significant time in the eastern district before her Canon appointment; no regional accent that anyone has been able to place, which is itself a Liturgy training outcome',
        170, 68, N'upright; still; the build of someone who has learned that stillness communicates authority more effectively than any physical presence',
        N'white',
        N'precisely arranged; functional; nothing is accidental',
        N'short',
        N'light grey-blue',
        N'pale; interior-preferring; not as weathered as the Lectors who travel constantly',
        N'the specific complexion of someone who has spent decades in close environments with alien material — it does not appear to have harmed her; it has not been documented whether it has changed her',
        N'none (Canons do not receive the Ceremony; Lectors do not; this is doctrine)',
        N'absolutely minimal movement; she is the most still person in any room she enters; this is not anxiety — it is the accumulation of forty-one years of being trained to assess others'' movement rather than signal through her own',
        N'Canon-grade Liturgy robes; more formal than Lector travel wear; alien-material sigils more complex and more numerous; she does not carry the material case because she does not administer; she carries a ledger that no House has ever been allowed to see',
        N'none',
        N'Review Lector reports. Conduct her own assessments at the Canon level when escalated. Attend the meetings she attends without being listed. Write her reports to the Silence through the channel that exists outside all House communication systems. The rhythm is administrative. The content is not.',
        N'Canon Sibylle Vaur has assessed, at the Canon level, that something in the alien material supply has changed in the past seven years. The change is subtle — not in the Ceremony outcomes, but in the Relic behavior. Specific Relics are doing things they did not do before: the Holdstone is pulsing on threat patterns it never responded to previously; the Vigil Glass is showing images at non-thin-membrane sites. She has reported this to the Silence. The Silence has not responded. She does not know whether the non-response is instruction to continue monitoring or instruction to stop.',
        N'Formal; precise; the vocabulary of institutional authority that has been exercised long enough to become natural rather than performed',
        N'Slow; deliberate; never rushed; the cadence of someone who has decided that what she says next will be exactly what she means to say',
        N'Always asking what the institution behind the person in front of her actually needs, which is usually different from what that person has been authorized to request',
        N'Becomes more formal and more minimal simultaneously; fewer words, more weight per word; the Liturgy trains this specifically',
        N'Does not have a conventional one; the closest is the silence after she has read something in a Lector''s report that requires careful thought before response',
        N'Caul Mor (primary office); western and central coalition district; the coalition tables she attends without appearing on attendance lists',
        0, 0,
        N'Liturgy Canon, female, pale complexion, precisely arranged white hair, light grey-blue eyes, absolutely still posture, formal Canon-grade ceremonial robes with complex alien-material sigils, carrying an unseen ledger, Caul Mor interior office context, Buehlman dark fantasy authority register',
        N'senior Liturgy Canon, white hair, formal ceremonial robes with alien sigils, very still, dark fantasy administrative authority',
        0, 0
    );
    PRINT 'Canon Sibylle Vaur seeded.';
END
GO

-- ── Lector Drava — a Lector who declined to administer to a Warrior King ─────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Lector Drava')
BEGIN
    DECLARE @dravaId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@dravaId, N'character', N'Lector Drava', N'lector-drava', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Characters (
        Id, Name, Slug, FirstName, LastName, TitlePrefix,
        Species, KindOfBeing, Gender, Pronouns, Age, LifeStatus, Role,
        Description, NarrativeFunction, NarrationVoice,
        Heritage, HeightCm, WeightKg, Build,
        HairColor, HairStyle, HairLength, EyeColor, SkinTone, Complexion,
        VisibleAugmentations, PostureMovement, PhysicalClothingStyle,
        Augmentations, DailyLife,
        PsychologySecret, SpeechVocabulary, SpeechCadence,
        SpeechSubtext, SpeechUnderPressure, SpeechIntimacyRegister,
        TerritoryRange, BioBatteryMaxCapacity, BioBatteryRecovery,
        MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount
    ) VALUES (
        @dravaId, N'Lector Drava', N'lector-drava', N'Drava', N'', N'Lector',
        N'human', N'human', N'ambiguous', N'they/them', 34, N'alive',
        N'Liturgy Lector; known specifically for having declined to administer the Ceremony to a seated Warrior King; currently under Canon review — not disciplinary, investigative',
        N'Lector Drava is the Lector who, fourteen months ago, arrived at the Forge Hearth at House Vulcanus''s request to assess a Warrior King candidate — a senior Paladin the House was preparing to formally recognize — and after a two-hour assessment, declined to administer the Ceremony. They did not explain the declination to the House. They did not need to. The Liturgy does not explain declinations. House Vulcanus filed a formal protest with the Liturgy, which the Liturgy received, acknowledged, and did not act on. Lector Drava has continued posting. They are currently under Canon review — not for the declination, which was within their authority, but because Canon Vaur wants to understand the assessment criteria they applied. Drava has provided a report. Vaur has not yet decided whether the report is sufficient.',
        N'The story''s proof that the Liturgy''s authority over the Ceremony is real, not ceremonial. The Lector who exercised it against a House''s explicit institutional preference and was not removed. The character whose criteria for the declination become, if they are ever understood, the key to understanding what the Liturgy actually assesses for.',
        N'No POV in the current design; appears at the specific incident level — the moment of the declination, witnessed by the House figures who were present',
        N'Mixed origin; the accent and physical type suggest multiple regions simultaneously; the Liturgy recruits across the Cauld',
        166, 63, N'compact; economical; the build of someone who has learned not to carry anything extra, physically or otherwise',
        N'dark brown',
        N'close-cropped',
        N'short',
        N'brown',
        N'warm medium brown',
        N'clear; younger than their experience suggests',
        N'none',
        N'precise; watchful; slightly more careful about physical space than Lector Caul — not anxious, but calibrated toward the minimal footprint',
        N'Lector travel robes; less worn than Caul''s; they have been Lector for eleven years but something in how they wear it suggests they are still deciding whether this is the life',
        N'none',
        N'Standard Lector posting rhythm, disrupted by the current Canon review; Drava is not suspended — they are continuing assessments — but Canon Vaur has flagged their postings for closer report scrutiny than standard',
        N'Lector Drava knows what they assessed for in the Warrior King candidate at the Forge Hearth. The report they provided to Canon Vaur is accurate as far as it goes. It does not include the specific thing they perceived during the assessment that made the declination unavoidable — not because they are concealing it, but because they do not have a Liturgy-sanctioned vocabulary for what they perceived. Something in the candidate''s loyalty was genuine and was also wrong in a way the doctrine does not describe. They have been trying to find the language for it since the assessment. They have not found it.',
        N'Careful; more formal than the situation usually requires; they are aware that everything they say is currently being weighted',
        N'Slightly slower than natural since the Canon review began; deliberate',
        N'Often asking what the person in front of them will do with what they''re about to say',
        N'Becomes more careful rather than faster; the economy of language tightens further',
        N'Professional; not warm; not cold; the register of someone who is genuinely uncertain whether warmth is appropriate in their current position',
        N'Eastern coalition district; Canon review postings as assigned',
        0, 0,
        N'Liturgy Lector, ambiguous gender, warm medium brown skin, close-cropped dark hair, economical compact build, travel Lector robes, precise careful posture, Forge Hearth context, Buehlman dark fantasy, the specific atmosphere of a declining assessment',
        N'Liturgy Lector, ceremonial travel robes, careful precise posture, dark fantasy, assessment context',
        0, 0
    );
    PRINT 'Lector Drava seeded.';
END
GO


-- ╔═══════════════════════════════════════════════════════════════════════╗
-- ║  RELICS — equipment entities                                         ║
-- ╚═══════════════════════════════════════════════════════════════════════╝

-- ── The Holdstone ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM EquipmentItems WHERE Name = N'The Holdstone')
BEGIN
    DECLARE @holdId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@holdId, N'equipment', N'The Holdstone', N'the-holdstone', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO EquipmentItems (Id, Name, Slug, Manufacturer, Category, Tier, Legality, BrandName, ProductName,
        Description, TacticalUse, CulturalContext, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @holdId, N'The Holdstone', N'the-holdstone',
        N'The Liturgy (alien origin; distributed, not manufactured)',
        N'Relic', N'Liturgy-granted; non-transferable by doctrine',
        N'Restricted to Liturgy-assessed recipients; not legally acquirable',
        N'The Liturgy', N'The Holdstone',
        N'A smooth palm-sized stone distributed by the Liturgy as a Relic. It stays at slightly above body temperature regardless of external conditions — a heat that does not warm the hand but is consistently present. When the carrier is approaching physical danger they have not yet consciously perceived, the stone pulses with a brief, distinct heat increase: enough to register, not enough to be confused with ambient warmth. The stone''s material does not match any Sphere catalogue entry any House has produced. It cannot be carved, scratched, or marked by any local material. Soldiers who carry it report treating the pulse as reliable within approximately a month of receiving it. Soldiers who receive it and later betray what the Liturgy assessed as their loyalty find it cold: the warmth stops. Whether the stone itself changes or whether the carrier has changed is not understood. The Liturgy does not comment. It is the most commonly distributed Relic; the Liturgy has given more Holdstones than all other Relics combined. This is significant but the significance has not been explained.',
        N'Passive threat detection via thermal pulse; reliable at short range for threats not yet consciously perceived. Not a weapon.',
        N'Receiving a Holdstone is visible confirmation of the Lector''s assessment. Soldiers who know what to look for can identify a carrier. A cold Holdstone is a statement the carrier usually cannot explain without admitting something.',
        N'small smooth palm-sized alien stone, warm amber undertone, alien material unlike local rock, dark fantasy prop, soft warm glow, soldier''s hand',
        N'alien stone relic, warm amber glow, small smooth pebble, dark fantasy',
        0, 0
    );
    PRINT 'The Holdstone seeded.';
END
GO

-- ── The Binding Cord ──────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM EquipmentItems WHERE Name = N'The Binding Cord')
BEGIN
    DECLARE @cordId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@cordId, N'equipment', N'The Binding Cord', N'the-binding-cord', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO EquipmentItems (Id, Name, Slug, Manufacturer, Category, Tier, Legality, BrandName, ProductName,
        Description, TacticalUse, CulturalContext, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @cordId, N'The Binding Cord', N'the-binding-cord',
        N'The Liturgy (alien origin; distributed, not manufactured)',
        N'Relic', N'Liturgy-granted; non-transferable by doctrine',
        N'Restricted to Liturgy-assessed recipients; not legally acquirable',
        N'The Liturgy', N'The Binding Cord',
        N'A braided length of alien fiber that collapses to a small coil when not in use and adheres to a wound, sealing it at a rate no field medicine explains. The wound closes at the point of contact within minutes of application; deeper wounds take longer but the adhesion is immediate. The fiber does not need to be removed — it integrates with the healing tissue and is gone by the time the wound has closed. A new length of cord is present when the carrier next reaches into the case it came in; where this length comes from is the question no one has been able to answer. The Liturgy distributes it specifically to Myrmidons who have demonstrated care for their unit''s survival above their own advancement. The distribution criterion is more specific than it sounds: the Lector is not assessing altruism. They are assessing a specific quality of attention to the people around them that has no simpler name. The Binding Cord has been used on an enemy soldier twice in documented record; both times, it worked. Both times, the carrier was subsequently assessed by a Canon rather than a Lector. The outcomes of those assessments are not in the distribution records.',
        N'Emergency wound closure at point of contact; integrates with healing tissue and disappears; self-replenishing. Application requires the user to stop and apply.',
        N'Marks its carrier as someone the Liturgy assessed as oriented toward the survival of others. Two documented cases of use on enemy soldiers suggest it finds people who will do that before they have been tested.',
        N'braided alien fiber cord, iridescent muted sheen, wound-closing relic, coiled in a small case, Buehlman dark fantasy prop',
        N'alien fiber cord, small coil, iridescent sheen, wound-sealing relic, dark fantasy',
        0, 0
    );
    PRINT 'The Binding Cord seeded.';
END
GO

-- ── The Vigil Glass ───────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM EquipmentItems WHERE Name = N'The Vigil Glass')
BEGIN
    DECLARE @glassId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@glassId, N'equipment', N'The Vigil Glass', N'the-vigil-glass', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO EquipmentItems (Id, Name, Slug, Manufacturer, Category, Tier, Legality, BrandName, ProductName,
        Description, TacticalUse, CulturalContext, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @glassId, N'The Vigil Glass', N'the-vigil-glass',
        N'The Liturgy (alien origin; distributed, not manufactured)',
        N'Relic', N'Liturgy-granted; non-transferable by doctrine',
        N'Possession by non-recipient is a Liturgy matter, not a House matter',
        N'The Liturgy', N'The Vigil Glass',
        N'A small lens of alien material that, held to the eye at a thin-membrane site, allows perception of signals from adjacent Spheres that apparatus cannot capture. The perception is not visual in the conventional sense — operators who have used it describe seeing the shapes of places, hearing sounds without source, perceiving physical structures from the adjacent Sphere in a form that is stable enough to describe but not stable enough to transcribe as Scry data. This makes the Vigil Glass simultaneously more powerful and less practically useful than a Scrying apparatus: it cannot produce the catalogue notation that drives manufacturing. What it produces is understanding — a witness to an adjacent Sphere, without the filter of the apparatus''s formal translation. Rare. The Liturgy has specific criteria for who receives this Relic and has never explained them to a House. The documented recipients have all been Scrying operators of long tenure and, in two cases, ordinary soldiers who demonstrated thin-membrane perception without apparatus. The Houses know this distribution pattern. They do not know what the Lectors saw in those two soldiers. As of seven months ago, Canon Sibylle Vaur is tracking reports that the Vigil Glass is showing images at non-thin-membrane sites, which it has never done before. This is the report she filed with the Silence that has not been responded to.',
        N'Unmediated cross-Sphere perception at thin-membrane sites; not catalogue-ready but provides strategic intelligence Scrying apparatus cannot produce. Currently showing images at non-thin-membrane sites — behavior change not yet explained.',
        N'The rarest distributed Relic. Receiving one is an assessment result no House has decoded. The anomaly of non-thin-membrane operation has not been publicized; only Canon-tier and above is tracking it.',
        N'small alien lens held to the eye, translucent alien material, cross-Sphere imagery visible through it, thin-membrane site atmospheric background, Buehlman dark fantasy',
        N'alien lens relic, held to eye, cross-Sphere vision, dark fantasy',
        0, 0
    );
    PRINT 'The Vigil Glass seeded.';
END
GO

-- ── The Meridian Clasp ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM EquipmentItems WHERE Name = N'The Meridian Clasp')
BEGIN
    DECLARE @claspId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@claspId, N'equipment', N'The Meridian Clasp', N'the-meridian-clasp', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO EquipmentItems (Id, Name, Slug, Manufacturer, Category, Tier, Legality, BrandName, ProductName,
        Description, TacticalUse, CulturalContext, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @claspId, N'The Meridian Clasp', N'the-meridian-clasp',
        N'The Liturgy (alien origin; distributed, not manufactured)',
        N'Relic', N'Liturgy-granted; distributed alongside Ceremony administration only',
        N'Cannot be requested or purchased; Lector-determined distribution only',
        N'The Liturgy', N'The Meridian Clasp',
        N'A small brooch of alien material, distributed alongside the Ceremony itself for subjects the Lector has determined are high-risk for the thermal shock of first infusion. Worn against the skin, it stabilizes the wearer''s internal temperature during the acute first-infusion phase — the fever that kills approximately 80% of subjects cannot be eliminated, but in a subject wearing the Clasp, it is measurably less violent. The mortality rate for Clasp-accompanied ceremonies is approximately 60% rather than 80%. This is the effect described in the Ichor Compound records that House Ophiuchus has been developing independently — the Houses have been trying to produce this effect chemically; the Liturgy has had the Relic version for as long as anyone in the Liturgy can document. Whether House Ophiuchus''s Ichor Compound is derived from reverse-engineering the Clasp or arrived at independently is not resolved. The Liturgy has not been asked. The Clasp is not distributed on request — the Lector determines risk assessment independently. A House that wants to increase its soldiers'' survival odds cannot simply request Clasp-accompanied ceremonies for all of them.',
        N'Thermal stabilization during first infusion. Reduces acute mortality from ~80% to ~60% in high-risk subjects. Worn against the skin during the Ceremony. No documented effect outside infusion context.',
        N'The Relic that most clearly demonstrates the gap between what Houses want and what the Liturgy controls. Every House would request Clasp-accompanied ceremonies for all candidates. The Lector decides — not the House.',
        N'small alien brooch worn against skin, thermal stabilization relic, alien material with subtle inner warmth, ceremonial context, Buehlman dark fantasy',
        N'alien brooch relic, worn against skin, thermal stabilization, dark fantasy ceremony context',
        0, 0
    );
    PRINT 'The Meridian Clasp seeded.';
END
GO


PRINT '═══════════════════════════════════════════════════════════════';
PRINT 'LITURGY SEED COMPLETE';
PRINT '1 Faction  |  3 Characters  |  4 Relics (equipment)';
PRINT '═══════════════════════════════════════════════════════════════';
