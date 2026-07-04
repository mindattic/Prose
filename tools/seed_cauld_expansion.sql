SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- CAULD UNIVERSE — WORLD EXPANSION SEED
-- Run: sqlcmd -S "(localdb)\MSSQLLocalDB" -d StreetSamurai -i tools\seed_cauld_expansion.sql
-- 2026-07-04 | The Seven Houses, their people, places, weapons, and Catalysts
-- Universe: fantasy-steampunk (ID 0197E9C9-0002-7000-8000-000000000002)
-- ═══════════════════════════════════════════════════════════════════════════════

DECLARE '0197E9C9-0002-7000-8000-000000000002' UNIQUEIDENTIFIER = '0197E9C9-0002-7000-8000-000000000002';
DECLARE @now DATETIME2 = GETDATE();


-- ╔═══════════════════════════════════════════════════════════════════════╗
-- ║  SECTION 1 — THE SEVEN HOUSES (Factions)                            ║
-- ╚═══════════════════════════════════════════════════════════════════════╝

-- ── House Corvus — intelligence, signals, cryptography ───────────────────────
IF NOT EXISTS (SELECT 1 FROM Factions WHERE Name = N'House Corvus')
BEGIN
    DECLARE @corvusId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@corvusId, N'faction', N'House Corvus', N'house-corvus', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Factions (Id, Name, Slug, Sector, Tier, Allegiance, Motto, Description, Ideology, Territory, Leadership, NarrativeFunction, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @corvusId, N'House Corvus', N'house-corvus',
        N'military / intelligence',
        N'House',
        N'current coalition with House Noctua (western front)',
        N'What the eye sees, the hand commands.',
        N'The intelligence House. House Corvus built its power on a single premise: you cannot outfight information. Their Scrying installation in the northern reaches — the Corvin Station — is the oldest continuously operating station in the Cauld, cataloguing signals from Spheres other Houses have not yet identified. They Scried radio communication before any other House and have run encrypted signal operations for forty years. Their soldiers often carry equipment the enemy cannot identify on sight. This advantage has made them valuable coalition partners and dangerous enemies. The coalition table without House Corvus is operating partially blind; they have turned this into the cornerstone of their entire strategic position.',
        N'Information is the only weapon that does not run out of ammunition. A war fought with superior intelligence ends sooner than one fought with superior numbers. House Corvus controls what signals enter the Cauld from the Sphere catalogue and therefore controls what technologies their coalition can anticipate and what the enemy coalition believes it understands. They do not seek the most dramatic victories. They seek the most consistent advantage.',
        N'Northern reaches; elevated ridge country where atmospheric crystal access is optimal for Scrying; the Corvin Station and its surrounding garrison settlements',
        N'Warrior King Vael Skaros (Paladin-rank; thirty years of campaign command). The Keeper position is currently contested following the death of the previous Keeper at Bheur''s Crossing three months ago.',
        N'The information broker House. Story tension generator through intelligence gaps: what Corvus knows and hasn''t shared, and why. The House that could change coalition dynamics at any moment and hasn''t — which means they''re waiting for the right moment, or the right price.',
        N'medieval military intelligence installation, signal towers on a northern highland ridge, atmospheric crystal formations in low cloud, WW1-era communications technology aesthetic in fantasy architecture, House Corvus raven heraldry, Buehlman dark fantasy, dawn light and fog',
        N'medieval military intelligence installation on a highland ridge, signal towers, WW1-era communications aesthetic in stone-and-timber architecture, northern atmosphere, dawn',
        0, 0
    );
    PRINT 'House Corvus seeded.';
END
GO

-- ── House Vulcanus — heavy industry, artillery, manufacturing ────────────────
IF NOT EXISTS (SELECT 1 FROM Factions WHERE Name = N'House Vulcanus')
BEGIN
    DECLARE @vulcanusId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@vulcanusId, N'faction', N'House Vulcanus', N'house-vulcanus', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Factions (Id, Name, Slug, Sector, Tier, Allegiance, Motto, Description, Ideology, Territory, Leadership, NarrativeFunction, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @vulcanusId, N'House Vulcanus', N'house-vulcanus',
        N'industrial / military',
        N'House',
        N'coalition with Houses Atrax and Cetus (eastern front)',
        N'The Forge does not argue with fire.',
        N'The manufacturing House. House Vulcanus controls the largest production complex in the Cauld — the Forge Hearth — because they were first to Scry the industrial manufacturing methods of a parallel world''s revolution. Their equipment is often a generation ahead of other Houses in volume if not in novelty, and their territory produces more Alloy 41 in a week than most Houses see in a season. They build Pattern-3 rifles, Hailmakers, Chromite Black components, and a third of everything else in active military use. In a long war — which the Living War has always been — this matters more than tactical genius. The Forge Hearth''s output sets the tempo of the war. Vulcanus is not the most interesting House to negotiate with. It is the most important one to keep in coalition.',
        N'Technological supremacy is the only kind that cannot be neutralized by a clever general or a shifted coalition. A House with superior manufacturing wins every war — eventually. House Vulcanus does not seek to be the most strategically agile or the most politically sophisticated. It seeks to produce more, better, faster than anyone else can respond to. The Forge Hearth is not merely a Scrying installation. It is a proof of concept.',
        N'Eastern industrial district; heavy mineral deposits, river access for transport, permanent coal-haze from the forges; the Forge Hearth district and surrounding production settlements',
        N'Keeper Maret Delys (technical authority; civilian; untransmuted; the most powerful person in House Vulcanus by institutional reality). The Warrior King position is held by a rotating committee of senior Paladins — Vulcanus is the one House where the Keeper outranks the Warrior King in institutional practice.',
        N'The industrial scale of the war made concrete. The House that exists to prove the war could be won by production alone — and has been trying for three generations. The counterpoint to every story about individual heroism: here is what a House looks like when it stops caring about individuals and starts caring about output.',
        N'medieval industrial fortress district with coal-smoke haze, foundries visible through stone arches, Scrying station tower at center, WW1-era weapons manufacturing aesthetic, forge-glow and grey industrial sky, Buehlman dark fantasy',
        N'medieval industrial manufacturing district, forge works, stone chimneys producing coal smoke, WW1-industrial aesthetic in fantasy setting',
        0, 0
    );
    PRINT 'House Vulcanus seeded.';
END
GO

-- ── House Atrax — logistics, supply chains, borderlands ──────────────────────
IF NOT EXISTS (SELECT 1 FROM Factions WHERE Name = N'House Atrax')
BEGIN
    DECLARE @atraxId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@atraxId, N'faction', N'House Atrax', N'house-atrax', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Factions (Id, Name, Slug, Sector, Tier, Allegiance, Motto, Description, Ideology, Territory, Leadership, NarrativeFunction, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @atraxId, N'House Atrax', N'house-atrax',
        N'logistics / trade',
        N'House',
        N'coalition with Houses Vulcanus and Cetus (eastern front); supply relationship with all Houses',
        N'The web holds everything.',
        N'The logistics House. House Atrax does not control the most impressive Scrying installation and does not produce the most striking weapons. What it controls is the distribution infrastructure that moves everyone else''s weapons from production to front. It has made itself indispensable through delivery: controlling which routes are safe, which crossings are maintained, which supply trains get priority. In coalition negotiations, House Atrax is always present, always useful, and the House no one can afford to antagonize. The Atrax position at any table is the quietest and the most load-bearing.',
        N'Everyone needs something. The House that controls the flow of supplies does not need to fight anyone directly — it simply adjusts the flow. A coalition that cannot feed its soldiers is not a coalition; it is a hostage situation. House Atrax operates on the principle that dependency is the deepest loyalty, and it has spent two generations making sure that everyone in the Cauld depends on something Atrax controls.',
        N'The borderlands; crossroads settlements, major supply routes, river crossings; Atrax territory is not contiguous — it is a network of corridors and nodes that follow logistics rather than military geography',
        N'The Seat (currently Provisional — House Atrax has been operating without a confirmed Seat for eight months following an internal succession dispute that has not been resolved publicly). A working council of route-directors and depot commanders fills the institutional vacuum without fanfare.',
        N'The web of dependency that keeps everyone in the war. The House that makes leaving the coalition more expensive than staying. The logistics face of a conflict that would collapse in weeks without reliable supply. The conscript Vessa Kaur is its entry point.',
        N'medieval borderland crossroads, fortified supply depot, Atrax spiderweb heraldry, WW1-era logistics and supply route aesthetic in fantasy setting, convoy routes through contested terrain, Buehlman dark fantasy',
        N'medieval logistics stronghold at crossroads, supply route aesthetic, spider heraldry, WW1-era military logistics in fantasy setting',
        0, 0
    );
    PRINT 'House Atrax seeded.';
END
GO

-- ── House Noctua — night warfare, chemical weapons, field operations ──────────
IF NOT EXISTS (SELECT 1 FROM Factions WHERE Name = N'House Noctua')
BEGIN
    DECLARE @noctuaId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@noctuaId, N'faction', N'House Noctua', N'house-noctua', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Factions (Id, Name, Slug, Sector, Tier, Allegiance, Motto, Description, Ideology, Territory, Leadership, NarrativeFunction, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @noctuaId, N'House Noctua', N'house-noctua',
        N'military / special operations',
        N'House',
        N'coalition with House Corvus (western front, Bheur''s Crossing)',
        N'The dark is ours.',
        N'The night warfare House. House Noctua Scried chemical weapon delivery systems from a Sphere in which they were used to terrible effect in a continental war, and they have been developing and deploying those systems ever since. They are also the specialists in nocturnal operations, infiltration, and unconventional force application. Other Houses contract their services for operations that cannot bear witness. Dame Thessaly Brennan effectively runs their field operations. Their Miasma Mortars are the weapon that makes coalition partners uneasy and keeps enemies from advancing after dark.',
        N'Every weapon a House can deploy in daylight, an enemy can see coming. The decisive action in any campaign happens in the margins — before dawn, after dark, in the fog that a morning offensive calls a visibility problem and a night operation calls cover. House Noctua has spent two generations refining operations that other Houses consider too costly or too unpredictable. The result is a House that other Houses hire for the things they do not want traced back to them.',
        N'The middle-front; contested ground, heavily mined, held by a Noctua-Corvus coalition; forward operating positions that change with the tactical situation',
        N'The Warrior King position is held by an unnamed senior figure who has not appeared at the coalition table in six months. Dame Thessaly Brennan (approaching Paladin-rank) effectively runs all field operations without holding the formal title.',
        N'The moral complexity of the war made operational. The House that does what other Houses need done and charges accordingly. The story''s confrontation with what a soldier becomes when the war stops happening to them and starts being what they''re good at.',
        N'medieval nocturnal military operations, dark field kit, Noctua owl heraldry, WW1-era chemical weapon deployment aesthetic, trench positions at night, moonlight and Miasma mortar smoke, Buehlman dark fantasy',
        N'nocturnal medieval military, dark field kit, owl heraldry, WW1 chemical warfare aesthetic in fantasy, trench line at night',
        0, 0
    );
    PRINT 'House Noctua seeded.';
END
GO

-- ── House Cetus — maritime control, border trade, river systems ───────────────
IF NOT EXISTS (SELECT 1 FROM Factions WHERE Name = N'House Cetus')
BEGIN
    DECLARE @cetusId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@cetusId, N'faction', N'House Cetus', N'house-cetus', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Factions (Id, Name, Slug, Sector, Tier, Allegiance, Motto, Description, Ideology, Territory, Leadership, NarrativeFunction, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @cetusId, N'House Cetus', N'house-cetus',
        N'maritime / trade',
        N'House',
        N'coalition with Houses Vulcanus and Atrax (eastern coalition)',
        N'The deep does not forgive.',
        N'The maritime House. House Cetus Scried submarine vessel designs and oceanographic survey tools from a Sphere that had already explored the deep sea, and parlayed those into a stranglehold on coastal and riverine trade. Their soldiers do not fight the same battles as inland Houses; they fight for chokepoints, for ports, for the specific bridges and crossings that move everything worth moving. They are the least glamorous and most consistently profitable House in any coalition — a distinction the other Houses find difficult to argue with when they need their supply routes maintained.',
        N'The Houses that fight over the interior exhaust themselves. The House that controls the borders controls what exits and what enters. House Cetus has never sought the most territory or the most dramatic victories. It has sought the most defensible position: the coast, the river deltas, the places where the map runs out of land. From there it watches everything that moves and charges for the watching.',
        N'Southern coast; river systems and major waterway trade routes; coastal fortifications and the key river delta checkpoints',
        N'The Admiral-Seat (currently Provisional following a succession dispute). An operational council of river-corridor commanders manages day-to-day military decisions.',
        N'The part of the war that looks like commerce. The House that makes the Living War financially sustainable for everyone — for a price. The proof that not every House fights the war the same way, and that some of them are winning by not appearing to compete.',
        N'medieval coastal fortress, Cetus whale heraldry, submarine vessel in stone drydock, river delta trade routes, WW1-era naval technology aesthetic in fantasy setting, grey sea and coastal cliffs, Buehlman dark fantasy',
        N'medieval coastal fortress, whale heraldry, river delta, WW1 naval aesthetic in fantasy setting, coastal cliffs',
        0, 0
    );
    PRINT 'House Cetus seeded.';
END
GO

-- ── House Fornax — Catalyst production, the alchemical core ──────────────────
IF NOT EXISTS (SELECT 1 FROM Factions WHERE Name = N'House Fornax')
BEGIN
    DECLARE @fornaxId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@fornaxId, N'faction', N'House Fornax', N'house-fornax', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Factions (Id, Name, Slug, Sector, Tier, Allegiance, Motto, Description, Ideology, Territory, Leadership, NarrativeFunction, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @fornaxId, N'House Fornax', N'house-fornax',
        N'alchemical / industrial',
        N'House',
        N'formally unaffiliated; supply relationship with all Houses; has never joined a coalition',
        N'What we make, we own.',
        N'The Catalyst House. House Fornax does not control a front. It does not need to. Every House in the Cauld needs what Fornax produces: the Catalyst supply that makes Transmutation possible. The Amber Wards, deep in the interior, are the most carefully protected site in the Living War — not because any single coalition would gain from attacking them, but because every coalition would lose if they were destroyed. House Fornax has leveraged this mutual dependency into a position of leverage that operates entirely outside the coalition system. They supply everyone who can pay. They have never joined a coalition. They have never needed to. The Seat is Emric Haed.',
        N'Transmutation is not merely a loyalty instrument. It is the only thing that makes a Myrmidon something more than a soldier with a Scried rifle. Every House in every coalition needs Catalyst supply. House Fornax makes the substance that makes all of this possible, and House Fornax will continue to make it — at the price it determines, for the Houses it chooses to supply. The ideology is clarity. The leverage is absolute.',
        N'The interior; buffer zones surrounding the Amber Wards complex; administratively separate from any House territory in the coalition sense',
        N'The Seat: Emric Haed (civilian; untransmuted by political design; the institutional authority of House Fornax and the only person who can authorize Catalyst allocation adjustments unilaterally). House Fornax has no Warrior King.',
        N'The power that sits above the war. The story''s proof that some leverage transcends military force. The House that every other House needs and none of them can fully control. The civilian face of the war''s deepest dependency.',
        N'interior alchemical production complex, amber-tinted atmospheric staining from ventilation stacks, institutional precision architecture, Fornax furnace heraldry, buffer zone perimeter, medieval-alchemical aesthetic, Buehlman dark fantasy',
        N'alchemical production facility, amber atmospheric tinting, furnace heraldry, medieval-industrial interior, buffer zone',
        0, 0
    );
    PRINT 'House Fornax seeded.';
END
GO


-- ╔═══════════════════════════════════════════════════════════════════════╗
-- ║  SECTION 2 — CHARACTERS                                              ║
-- ╚═══════════════════════════════════════════════════════════════════════╝

-- ── Vael Skaros — Warrior King, House Corvus, Paladin-rank ───────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Vael Skaros')
BEGIN
    DECLARE @vaelId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@vaelId, N'character', N'Vael Skaros', N'vael-skaros', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
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
        @vaelId, N'Vael Skaros', N'vael-skaros', N'Vael', N'Skaros', N'Warrior King',
        N'human', N'human', N'male', N'he/him', 58, N'alive',
        N'Warrior King of House Corvus; the House''s most experienced military commander; Paladin-rank Transmuted',
        N'Thirty years of campaign command have produced in Vael Skaros a quality that his Myrmidons describe, without irony, as stillness. He does not perform authority; it is the residue of having made enough correct decisions in situations where wrong decisions meant watching specific people die. The Paladin-stage Transmutation has restructured his body: elevated temperature, reinforced bone groups, low-light ocular modification that produces a pale luminescence in dim conditions, hands approximately twelve percent larger than a baseline human''s. The structural ridging along his jaw and orbital bones reads as severity from a distance and as something more complicated up close. His white hair went that color in his late thirties, which is within the documented range for Paladin-stage progress. He knows what House Corvus''s signals advantage is worth. He knows that advantage is degrading, and he has not told anyone.',
        N'The weight of experience made physical. The story''s proof of what Transmutation produces over decades — not just the capability but the accumulated cost of everything it took to reach Paladin. His secret (the degrading signals advantage) is the story''s clock: the most powerful intelligence House in the northern coalition is operating on a diminishing asset, and only its Warrior King knows.',
        N'Close third; sparse and tactical; his POV sections are information-dense and physically grounded; he processes space as threat assessment and political conversations as logistics problems',
        N'Northern reaches; highland origin; mixed ancestry from the ridge-country settlements; dark-complexioned for the north',
        193, 107, N'heavy-boned and broad; not bulked in the bodybuilder sense — the weight is structural, the result of three decades of Transmutation reinforcing the skeleton',
        N'white (went white in his late thirties; within Paladin-stage documented range)',
        N'cut short; military; no concession to appearance',
        N'short',
        N'pale grey; luminescent in low light — the Paladin-stage ocular modification',
        N'deep brown',
        N'smooth where scarring existed; Transmutation has closed the texture of older damage',
        N'structural ridging along the jaw and orbital bones; hands visibly larger than baseline; sub-dermal reinforcement at the temples appears as faint geometric patterning in strong light',
        N'moves with extreme economy; no wasted motion; when he stops he is very still; the stillness reads as a choice, not rest; the stillness is wrong in the way that only soldiers who have learned to be very still in contested spaces can produce',
        N'standard Myrmidon field coat, heavily worn; no decorative rank insignia; rank is communicated by how everyone else behaves in his presence',
        N'Paladin-stage Transmutation: structural reinforcement of all major bone groups; elevated body temperature (+2.1°C above baseline); low-light ocular modification producing pale luminescence; sub-dermal geometric patterning at temples and jaw; hands 12% larger than baseline; wound response significantly above baseline — minor wounds close within hours; apparent aging slowed past normal human rate',
        N'Wakes before light. Reviews overnight signal intercepts before anyone else has read them. Holds a standing morning briefing that no one skips. Eats field rations even at the command station. Does not distinguish between field and command conditions. The day is operational from the moment he is awake.',
        N'House Corvus''s signals advantage has been degrading for two years. The Sphere whose radio designs they Scried has advanced past radio — and their current Keeper rotation cannot read the new signals. He has not told the coalition. He is managing the decline while searching for a solution. If the advantage disappears before he finds one, everything he has built the House''s position on evaporates. He has not told Dame Thessaly Brennan. He has not told the coalition table. He has not told the person who would most need to know.',
        N'Sparse; field-calibrated; the fewest words that accomplish the briefing; no unnecessary qualifiers; the vocabulary of someone who has learned that hesitation is expensive and elaboration is a luxury',
        N'Slow; never rushed; the cadence of someone who has learned that hesitation costs and panic costs more',
        N'Often asking "what are you going to do about it" rather than the stated question',
        N'Quieter; paradoxically so; the more acute the situation, the less he says and the more weight each word carries',
        N'Does not have one in formal usage. Occasionally, with the handful of people who have served with him long enough, his vocabulary shifts to the plural "we" even when referring to himself. This has been observed. It has not been discussed.',
        N'Northern reaches; the Corvin Station and surrounding highland garrison positions; Bheur''s Crossing during active coalition operations',
        0, 0,
        N'Paladin-rank military commander, deep brown skin, white hair cut short, pale grey luminescent eyes, structural bone ridging along jaw and temple, heavy-boned imposing frame, worn military field coat, very still posture, northern ridge battlefield, WW1-era military command aesthetic in dark fantasy, Buehlman register, dramatic side lighting',
        N'Paladin-rank commander, dark skin, white hair, luminescent grey eyes, bone ridging, worn field coat, northern battlefield',
        0, 0
    );
    PRINT 'Vael Skaros seeded.';
END
GO

-- ── Maret Delys — Keeper, House Vulcanus ─────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Maret Delys')
BEGIN
    DECLARE @maretId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@maretId, N'character', N'Maret Delys', N'maret-delys', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
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
        @maretId, N'Maret Delys', N'maret-delys', N'Maret', N'Delys', N'Keeper',
        N'human', N'human', N'female', N'she/her', 43, N'alive',
        N'Keeper of the Forge Hearth Scrying station; technical authority of House Vulcanus; the most powerful person in the House by institutional reality',
        N'Maret Delys runs the Forge Hearth station on a principle she has never articulated to the rotating committee of Paladins who nominally hold the Warrior King position: whoever controls what gets manufactured controls what the war is. She has been Keeper for nine years. In that time she has expanded the Sphere catalogue from forty-one active signals to sixty-three, introduced Chromite Black into the production line, and declined the first infusion twice — once officially, once when it was offered informally by a Paladin who thought she would accept it if it came from someone she respected. She was not impolite about the refusal. She has not explained it. The explanation is that the Scrying chair requires the Keeper''s full attention, and Transmutation recovery does not allow full attention, and she will not leave the chair.',
        N'The civilian intelligence of the war — the person who decides what technologies enter the Cauld from which Spheres, and therefore decides who wins in the long run. Her secret (the unauthorized re-opened Sphere) is the story''s access to what unauthorized Scrying looks like when done by someone who is very good at it and very careful about why.',
        N'Close third; technical and sensory; a POV that translates the Scrying process into concrete physical experience — the hum of the apparatus, the specific quality of a signal arriving from a closed Sphere',
        N'Eastern industrial district native; working-class origins; advancement through technical distinction',
        167, 61, N'lean and angular; the build of someone who works seated for long periods but also walks the station floor; not sedentary — purposefully in motion when not in the chair',
        N'dark brown, nearly black',
        N'pulled back; practical; she works near equipment',
        N'medium',
        N'amber-brown',
        N'medium brown; coal-haze weathered from years in the Forge Hearth atmosphere',
        N'marked by the Forge Hearth environment; always a faint residue of the atmospheric particulate; not unhealthy, just lived-in',
        N'none (untransmuted; declined twice)',
        N'focused; moves through the station with proprietary authority; absolutely no hesitation about where things are or how they work; the Forge Hearth is an extension of her working memory',
        N'station-work clothing; heavy canvas, Forge Hearth issue; ink-stained from the signal transcription logs; functional and not performing anything',
        N'none',
        N'Arrives at the station before the morning shift. Runs the signal log review personally — does not delegate this. Supervises the transcription team. Holds three formal briefings a day with the production schedule. Eats at the station. Rarely sleeps there but has done so every night this week.',
        N'For eight months, Maret Delys has been running an unauthorized side-channel: cataloguing Scry contacts from a Sphere that House Vulcanus formally classified as closed three years ago following a bad signal event. The contacts from that Sphere are fragmentary but describe manufacturing processes that would give Vulcanus a decade of additional production advantage. She has not reported this because reopening a classified Sphere requires a council vote, and she does not trust the council to not leak the intelligence before she has extracted everything useful. She is running the most valuable asset the House has and the House does not know it exists.',
        N'Technical and precise; specific; numbers and specifications are her natural register; she becomes uncomfortable in rooms where conversations stay at the level of politics without arriving at the level of fact',
        N'Mid-pace; accelerates when discussing something interesting; slows almost to a stop when uncertain, which is rare enough to be noticeable',
        N'Often asking "do you understand what I''m telling you or should I try a different word"',
        N'Faster and more precise; does not catastrophize; routes around problems; the emotional register that might read as panic in someone else reads in her as acceleration',
        N'Does not do small talk. Intimate moments are characterized by sharing technical information she wouldn''t share with most people — the specificity of the offer is the intimacy.',
        N'The Forge Hearth district; eastern industrial territory',
        0, 0,
        N'female Keeper of a Scrying station, medium brown skin coal-haze weathered, dark hair pulled back, amber eyes, lean angular frame, canvas station clothing ink-stained, signal transcription logs surrounding her, industrial-alchemical Forge Hearth environment, WW1-industrial fantasy aesthetic, focused authority, Buehlman dark fantasy',
        N'female Scrying station Keeper, dark hair pulled back, amber eyes, canvas work clothing, industrial alchemical setting',
        0, 0
    );
    PRINT 'Maret Delys seeded.';
END
GO

-- ── Dame Thessaly Brennan — House Noctua, approaching Paladin ────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Dame Thessaly Brennan')
BEGIN
    DECLARE @thessalyId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@thessalyId, N'character', N'Dame Thessaly Brennan', N'dame-thessaly-brennan', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
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
        @thessalyId, N'Dame Thessaly Brennan', N'dame-thessaly-brennan', N'Thessaly', N'Brennan', N'Dame',
        N'human', N'human', N'female', N'she/her', 37, N'alive',
        N'Field commander, House Noctua; three infusions completed; approaching Paladin threshold; de facto commander of Noctua operations in the Warrior King''s extended absence',
        N'Dame Thessaly Brennan''s body has been through three infusions and looks late-twenties, which means Transmutation is working correctly and which does not particularly affect her professional assessment of what she needs to do next. The low-light ocular modification is partial — her eyes reflect in dim conditions, but not with Paladin''s full luminescence. The sub-dermal hardening along her jaw and the backs of her hands is visible under direct light as a faint surface texture change. Her body temperature runs elevated enough that in cold conditions she produces light visible steam. She has professional contempt for any House that mistakes unconventional warfare for dishonesty. House Corvus, which achieves its goals through signal manipulation, has her respect. Houses that object to the Miasma Mortar do not.',
        N'Soren''s future antagonist or ally, depending on coalition alignment. The person who does the war''s dirty work with genuine conviction and professional precision. The story''s confrontation with what a soldier becomes when the war stops being something that happens to them and starts being what they''re good at. Do not make her a villain. She is very good at her job.',
        N'Close third; tactical-kinetic; a POV that registers space as threat-or-cover first and everything else second; her prose sections would have the same rhythm as a night assault — dark, fast, purposeful',
        N'The middle-front territories; mixed ancestry; her family has been in the war zones for three generations, which means she was born into conditions that other soldiers arrive at',
        174, 72, N'wiry, not slight; the musculature of someone physically modified to move efficiently through difficult terrain in darkness',
        N'near-black; the Transmutation progression has shifted the undertone slightly toward blue-black',
        N'tight braids kept close to the skull; functional for field operations',
        N'medium-length braids',
        N'grey-green with a faint luminescence at the outer edge of the iris — approaching but not yet at Paladin-stage',
        N'warm brown',
        N'clear; the Transmutation has resolved earlier scarring',
        N'sub-dermal hardening along the jaw and backs of the hands; eyes reflect in low light; elevated body temperature produces visible steam in cold conditions',
        N'habitually low; even in rooms with adequate ceiling height she moves as if she expects to need to duck; the body has been calibrated for field movement and has not fully adjusted to non-field environments',
        N'Noctua field kit; dark cloth, no reflective elements, everything secured to eliminate noise; worn but maintained',
        N'Three infusion-stages: elevated body temperature; partial low-light ocular modification; sub-dermal jaw and hand hardening; slightly accelerated wound response (faster than Knight, slower than Paladin)',
        N'Sleeps in the morning after night operations. Reviews positions at dusk. Field operations begin two hours after dark. Eats irregularly and does not appear to notice.',
        N'Dame Thessaly Brennan believes the Warrior King position at House Noctua is occupied by someone who has been dead for two months. She has evidence. She has been managing House Noctua''s field operations as if the Warrior King is merely unavailable, because revealing the evidence means a succession crisis in the middle of a coalition offensive. She has not decided what she will do when this is no longer sustainable.',
        N'Operational; direct; maximum information in minimum syllables — the vocabulary of someone who gives orders in low-visibility conditions',
        N'Fast by default; she gives and receives orders at the pace of field action',
        N'Often saying "there is a problem and here is what we are doing about it" simultaneously, in the same sentence',
        N'Identical to her normal mode, which is disconcerting to people who expect escalation as a stress signal',
        N'Rare and sounds like a briefing; the content is personal but the delivery is operational; she has not learned a different register for that',
        N'Middle-front; contested territory; forward operating positions and the Noctua side of Bheur''s Crossing',
        0, 0,
        N'female military commander, warm brown skin, near-black braids close to skull, grey-green eyes with faint luminescence at edges, wiry frame, dark Noctua field kit, habitually low posture, sub-dermal hardening visible at jaw, Buehlman dark fantasy, moonlit nocturnal field operations',
        N'female field commander, warm brown skin, braided dark hair, luminescent grey-green eyes, dark field kit, nocturnal battlefield',
        0, 0
    );
    PRINT 'Dame Thessaly Brennan seeded.';
END
GO

-- ── Emric Haed — The Seat, House Fornax ──────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Emric Haed')
BEGIN
    DECLARE @emricId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@emricId, N'character', N'Emric Haed', N'emric-haed', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
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
        @emricId, N'Emric Haed', N'emric-haed', N'Emric', N'Haed', N'The Seat',
        N'human', N'human', N'male', N'he/him', 64, N'alive',
        N'The Seat of House Fornax; controls Catalyst supply policy; the most leveraged civilian authority in the Cauld',
        N'Emric Haed has declined Transmutation on the public grounds that the Seat of House Fornax should not physically align with any stage of the ascendance track — the position requires impartiality, and a visibly Transmuted Seat implies a stake in the process that contradicts the House''s supply neutrality. This position is ideological and it is also conveniently true that Transmutation carries an 80% first-infusion mortality rate, and Emric Haed is genuinely very comfortable in his current body. He is warm, precise, and remarkably candid in private about the mechanics of Catalyst pricing and coalition dependency. The warmth is genuine. The candor is calculated. The combination has kept him in the Seat for fourteen years without a formal challenge. He controls what every army in the Cauld needs to make its soldiers into something more than soldiers, and he has turned that control into the most stable power base in the Living War.',
        N'The civilian face of the war''s leverage economy. The story''s proof that the most powerful person in the Cauld is not a soldier, a Champion, or a Warrior King — it is the man who controls what all of them need. His secret (the unauthorized third-party Catalyst sale) is the story''s bomb: if it surfaces, Fornax''s claimed neutrality collapses and every House realigns simultaneously.',
        N'Close third; administrative-formal; a POV that processes power relations as supply-chain logistics, which is how he actually thinks',
        N'Interior settlements; generations of Catalyst refinement workers in the family, before House Fornax absorbed the independent operations and made the family''s expertise a loyalty test that they passed',
        175, 84, N'soft; the build of a man who has never needed to fight anything directly and is quietly grateful for this',
        N'white-grey',
        N'neat; institutional; cut to signal that he is not a field person',
        N'short',
        N'pale blue',
        N'light; interior-dwelling; sun-avoidant',
        N'the complexion of a man who is very rarely cold or wet or working outdoors',
        N'none',
        N'formal; seated authority; moves minimally and deliberately; everything is placed; he does not move without intention',
        N'Fornax administrative clothing; precise, quality fabric, nothing that would look out of place in a council chamber; he does not dress for the field because he does not go to the field',
        N'none',
        N'Morning review of supply manifests. Series of scheduled meetings. The day is structured and documented. Nothing that isn''t documented. Evenings are his own and not documented.',
        N'For fourteen months, Emric Haed has been selling Catalyst supply to a third party outside the coalition system. The buyer''s identity is unknown to him — the arrangement is intermediated. He does not know whether the buyer is a House, an independent actor, or something he doesn''t have a category for. He knows the price is very good. He knows this is the kind of transaction that ends with everyone involved dead if it surfaces. He is very good at maintaining the institutional surface. He has been very good at it for fourteen months.',
        N'Formal; institutional; the vocabulary of supply-chain negotiation; warm in person, which is the dangerous part — warmth makes people feel like they have been understood when they have been processed',
        N'Measured; every sentence arrives considered; he does not improvise in negotiations',
        N'Always asking "what do you need from me and what will you give for it" — stated or unstated',
        N'Becomes more formal; the warmth recedes; he negotiates rather than reacts',
        N'Genuine; surprisingly direct; the man who controls everything through institutional distance is oddly candid in private — which is how he has kept the people who know him most from seeing what he is actually doing',
        N'The Amber Wards; interior buffer zones; the Fornax administrative complex',
        0, 0,
        N'male institutional authority, light skin, white-grey hair neatly cut, pale blue eyes, soft build, precise formal Fornax administrative clothing, seated council chamber authority posture, medieval alchemical institutional aesthetic, Buehlman dark fantasy',
        N'male civilian authority figure, light skin, grey hair, formal institutional clothing, council chamber setting',
        0, 0
    );
    PRINT 'Emric Haed seeded.';
END
GO

-- ── Vessa Kaur — Conscript, House Atrax logistics ────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Vessa Kaur')
BEGIN
    DECLARE @vessaId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@vessaId, N'character', N'Vessa Kaur', N'vessa-kaur', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
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
        @vessaId, N'Vessa Kaur', N'vessa-kaur', N'Vessa', N'Kaur', N'',
        N'human', N'human', N'female', N'she/her', 23, N'alive',
        N'Supply convoy driver, House Atrax logistics; conscript; no Transmutation access',
        N'Vessa Kaur has been driving supply convoys under fire for fourteen months, which means she has learned to assess a threat situation without stopping to think about it, which means she is competent at something the war does not formally recognize her as competent at because she is a conscript and conscripts are not in the system that formalizes competence. She notices things she is not supposed to have access to. She reads the Scry catalogue fragments that come through on sensitive supply manifests — destination-coded fragments that travel with Catalyst shipments and production schedules. She has been doing this without authorization for eight months. She understands more about what is being manufactured and why than any conscript is supposed to understand. She does not yet know what to do with this knowledge.',
        N'The ground-level witness. The reader''s entry point into what the war costs the people outside the Transmutation hierarchy. The story''s proof that competence and insight do not require Transmutation — and that the system does not acknowledge this.',
        N'Close third; practical-sensory; a POV that processes the war as physical experience — the weight of the cab, the road conditions, the sound of small-arms fire at a distance and what it means for the route — without the frame of strategy or ideology',
        N'Coastal resettlement camps; South Asian diaspora; family origin from the settlement corridor near what was the Bay of Bengal before the Cauld''s territorial compression',
        162, 58, N'slight-to-medium; the build of someone who is physically active but not maintained on military rations',
        N'dark brown-black',
        N'braided and coiled for work; practical',
        N'long',
        N'dark brown',
        N'warm brown',
        N'weathered by the road; sun and cold and convoy dust',
        N'none (conscripts are not offered Transmutation)',
        N'efficient; the movement of someone who has been driving supply convoys under fire for fourteen months; assesses threats without stopping to think about it',
        N'conscript logistics kit; functional, worn, repaired multiple times; boots that have been fixed three times; nothing that belongs to her specifically except the way she carries it',
        N'none',
        N'Convoy schedule; pre-dawn departure on active routes; checks vehicle before the team is awake; long hours in the cab; post-route vehicle maintenance; sleeps when possible.',
        N'For eight months, Vessa Kaur has been reading the Scry catalogue fragments that come through on sensitive supply manifests — destination-coded data that is supposed to be routed without being read by the logistics personnel handling it. She has been reading it. She understands more about what is being manufactured, where, and why than any conscript is supposed to understand. She does not know what to do with this knowledge. She knows that having it is dangerous. She has not told anyone.',
        N'Practical; road-calibrated; the vocabulary of someone who has learned multiple things from multiple sources with no formal register to organize them in',
        N'Fast under pressure; slower in rest periods; adapts to the situation',
        N'Often checking whether the person she''s talking to is going to be a problem',
        N'Goes quiet; makes decisions; talks less; acts more',
        N'Rare and tentative; she has not had enough rest to develop a reliable one',
        N'The borderland supply routes; House Atrax logistics corridors',
        0, 0,
        N'young South Asian woman, warm brown skin, dark braided hair coiled for work, dark brown eyes, slight build, worn conscript logistics kit, weathered complexion, supply convoy context, WW1-era military logistics aesthetic in dark fantasy, Buehlman register',
        N'young South Asian woman, dark braided hair, worn conscript uniform, supply convoy, dark fantasy military logistics',
        0, 0
    );
    PRINT 'Vessa Kaur seeded.';
END
GO

-- ── Champion Sable — unaffiliated, past Paladin, the story's limit case ───────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Champion Sable')
BEGIN
    DECLARE @sableId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@sableId, N'character', N'Champion Sable', N'champion-sable', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
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
        @sableId, N'Champion Sable', N'champion-sable', N'Sable', N'', N'Champion',
        N'human', N'human', N'ambiguous', N'they/them',
        0, N'alive',
        N'Champion; no House affiliation; appears at intervals no House has established a pattern for',
        N'What survives across contradictory witness accounts: silver eyes (Champion-stage ocular change, consistently reported); a quality of stillness that reads as wrong to anyone who has been in combat (nothing alive should be that still in a contested zone); a name that is the only name anyone has for them. Everything else — height, coloring, apparent age, the specific physical changes of Champion-stage Transmutation — contradicts from account to account. This may be because witnesses are not reliable. It may be because the Champion-stage changes are significant enough that different observers are reading the same body differently. The Houses that have worked with Champion Sable do not publicly acknowledge it. They do not ask questions when the work is done.',
        N'The proof of what full ascendance produces — and the story''s limit case on what Transmutation is and is not. Not a villain, not a conventional ally. The presence that proves the Living War is embedded in something larger than any coalition. Do not explain them. Do not resolve them. Do not reduce them to a motivation the story needs to articulate. Trust the reader.',
        N'Champion Sable does not have a POV in the current story design; they appear from outside, in other characters'' POV sections',
        N'unknown; the accounts disagree on everything',
        0, 0, N'the descriptions that survive are contradictory; a structural quality that reads as very large to some observers and very still to others; what all accounts agree on is that Champion Sable in motion is disconcerting to watch',
        N'the accounts disagree',
        N'the accounts disagree',
        N'the accounts disagree',
        N'silver; the Champion-stage ocular change; the one detail all accounts confirm',
        N'the accounts disagree',
        N'the accounts agree on one thing: the Transmutation has erased everything that would let an observer place their origin',
        N'Champion-stage: the physical changes at full ascendance are significant; what is consistently reported is a quality of stillness that reads as wrong to combat-experienced observers; wings have been reported but not confirmed on the page',
        N'the stillness; when they move, the movement is economical past the point of human efficiency; they use cover instinctively in conditions where no one is threatening them, which suggests the habit is older than memory',
        N'nothing consistent across accounts; always described as functional and unidentifiable by House',
        N'Champion-stage Transmutation; the full extent is not on the page; what is visible does not add up to a complete list',
        N'unknown',
        N'The story''s deepest structural mystery. What they want, why they appear, what they are working toward: unknown. The Houses do not ask. The story does not answer. The secret is that there may not be a legible human motivation remaining — or there may be one that requires the whole story to articulate.',
        N'sparse; when Champion Sable speaks, witnesses remember what was said',
        N'patient; described by everyone who has spoken with them',
        N'always aware of the question behind the question',
        N'does not appear to experience pressure in the conventional sense; identical quality under fire as in a supply office',
        N'none that has been witnessed; this does not mean it doesn''t exist',
        N'unregistered; appears where and when they determine',
        0, 0,
        N'Champion-rank ascendant, ambiguous appearance, silver eyes, physically still in a way that reads as uncanny, unidentifiable functional clothing, contested battlefield, Buehlman dark fantasy, deep shadow and ambient light, liminal presence',
        N'Champion-rank figure, silver eyes, ambiguous appearance, deep shadow, battlefield',
        0, 0
    );
    PRINT 'Champion Sable seeded.';
END
GO

-- ── Grim — Named monster, the Mossland's ancient predator ───────────────────
IF NOT EXISTS (SELECT 1 FROM Characters WHERE Name = N'Grim')
BEGIN
    DECLARE @grimId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@grimId, N'character', N'Grim', N'grim-mossland', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
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
        @grimId, N'Grim', N'grim-mossland', N'Grim', N'', N'',
        N'creature', N'creature', N'n/a', N'it/it',
        0, N'alive',
        N'Native predator of the Mossland; the name soldiers gave it; it does not have one',
        N'Eighty years of House Ophiuchus incident reports describe Grim as: large, low to the ground, moving like something that has learned that cover exists and how to use it. Estimates of size range from three to five meters, which suggests Grim is rarely seen clearly. Its coloring reads as the Mossland''s own — not adaptive camouflage but inherent pigmentation, as if Grim and the Mossland grew in the same conditions for long enough that they arrived at the same color. The service records use the word "patient" in incident reports written by people who were not writing metaphors. It has territories. It waits near the routes armies use. It has been doing this for longer than any living person can testify to.',
        N'The native horizon. The thing that will be in the Mossland after the Living War ends, if it ends, because it was there before the war started. The story''s proof that the Houses'' war is contained inside something larger and older that does not recognize the war as the primary event. Do not resolve Grim. Do not make Grim a villain. Grim is nature. It eats what it finds.',
        N'Grim has no POV. It appears in other characters'' POV sections as an environmental presence — not a monster-movie threat but an ecological fact',
        N'The Mossland; no recorded origin; predates all House presence in the territory',
        0, 0, N'large; low to the ground; estimates range from three to five meters — the inconsistency in service records suggests it is rarely seen clearly',
        N'the Mossland''s color; inherent pigmentation, not adaptive camouflage',
        N'n/a',
        N'n/a',
        N'not reliably recorded; survivors describe seeing it after they have moved away from where it was',
        N'the Mossland''s color; see Build',
        N'n/a',
        N'none; not Transmuted; not a failed infusion; native to the Mossland and continuous with it',
        N'low; methodical; the service records use the word "patient" in incident reports written by people who were not writing metaphors',
        N'n/a',
        N'none (biological; native; what it is physically is what it is, not what something did to it)',
        N'The Mossland is its territory. The western-facing routes along the eastern ridge see the most incident reports. Hours around dusk and dawn are when most encounters are recorded. No other pattern confirmed.',
        N'The story''s unanswered question: whether Grim is learning. Eighty years of incident reports show it using the same routes that armies use. It has territories that avoid active combat zones and concentrate at the margins. This could be adaptive intelligence. It could be coincidence. The Houses have not allocated resources to determine which.',
        N'n/a',
        N'n/a',
        N'n/a',
        N'n/a',
        N'n/a',
        N'The Mossland; the eastern ridge; the margins of active fronts',
        0, 0,
        N'massive native predator of a mossland, low profile, camouflaged in moss-green and grey, very still and patient, enormous physical presence implied rather than shown, WW1-era trench warfare backdrop, misty mossland at dusk, Buehlman dark fantasy horror register',
        N'massive mossland predator, moss-grey camouflage, low profile, misty mossland dawn, Buehlman dark fantasy',
        0, 0
    );
    PRINT 'Grim seeded.';
END
GO


-- ╔═══════════════════════════════════════════════════════════════════════╗
-- ║  SECTION 3 — PLACES                                                  ║
-- ╚═══════════════════════════════════════════════════════════════════════╝

-- ── The Corvin Station — House Corvus Scrying installation ───────────────────
IF NOT EXISTS (SELECT 1 FROM Places WHERE Name = N'The Corvin Station')
BEGIN
    DECLARE @corvinId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@corvinId, N'place', N'The Corvin Station', N'the-corvin-station', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Places (Id, Name, Slug, Territory, Tier, Climate, Description, Demographics, Economy, PowerStructure, AtmosphereFeel, GeoLat, GeoLng, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @corvinId, N'The Corvin Station', N'the-corvin-station',
        N'Northern reaches; House Corvus territory',
        N'Scrying Installation',
        N'Cold; highland; frequent low cloud producing the atmospheric crystal conditions optimal for Scrying',
        N'House Corvus''s Scrying installation in the northern reaches — the oldest continuously operating station in the Cauld. Built into a converted signal-tower complex that predates the current coalition structure by at least three generations. The installation has grown organically over decades: the original tower at the center, surrounded by the listening buildings, the transcription halls, the signal archive, and the housing blocks for the rotation crews. It runs twenty-four-hour operations; there is always someone in the chair. The atmospheric crystal conditions in the northern ridges make this one of the most reliable Scrying sites in the Cauld, which is why House Corvus has defended it against four separate interdiction attempts without meaningful loss. The current threat is House Atrax''s perimeter probes — three years of careful testing that has not yet become an assault.',
        N'Rotation crews of forty to sixty technical personnel; a Myrmidon garrison; Keeper staff; the Warrior King''s command element when present in the northern reaches; a small civilian support settlement outside the perimeter',
        N'The station does not produce goods; it produces intelligence, which it trades through coalition agreements for everything the House needs. The garrison''s logistical supply runs through House Atrax routes, which is a dependency everyone at the Corvin Station is aware of and no one discusses aloud.',
        N'The Keeper runs the technical operation; the garrison commander runs the defensive perimeter; the Warrior King visits but is rarely in residence. Three authority lines in productive tension for thirty years.',
        N'The hum of the Scrying apparatus is constant at the center. The transcription halls are quiet in a way that is maintained rather than natural. The garrison perimeter is standard field conditions — cold, functional, no concession to comfort. The ridge wind is present on days without low cloud and the low cloud is present on most days.',
        0.0, 0.0,
        N'ancient converted signal tower complex, northern highland ridge, atmospheric crystal formations in low cloud, WW1-era signals intelligence installation aesthetic in stone-and-timber architecture, House Corvus raven heraldry, garrison perimeter, Buehlman dark fantasy, dawn light and highland fog',
        N'signal tower complex on northern ridge, low cloud and atmospheric crystals, raven heraldry, WW1 signals aesthetic in fantasy',
        0, 0
    );
    PRINT 'The Corvin Station seeded.';
END
GO

-- ── The Forge Hearth — House Vulcanus industrial district ────────────────────
IF NOT EXISTS (SELECT 1 FROM Places WHERE Name = N'The Forge Hearth')
BEGIN
    DECLARE @forgeId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@forgeId, N'place', N'The Forge Hearth', N'the-forge-hearth', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Places (Id, Name, Slug, Territory, Tier, Climate, Description, Demographics, Economy, PowerStructure, AtmosphereFeel, GeoLat, GeoLng, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @forgeId, N'The Forge Hearth', N'the-forge-hearth',
        N'Eastern industrial district; House Vulcanus territory',
        N'Industrial District',
        N'Moderate in climate terms; permanently obscured by forge-smoke; significantly elevated ambient temperature near the forges',
        N'House Vulcanus''s Scrying installation is not a separate facility — it is the center of a working industrial district. The Forge Hearth began as a standard station and expanded over two generations into the manufacturing complex that now surrounds it. The installation remains the technical heart of the district, but what most visitors see is the forge works: the foundries, the production lines, the workshops where Scried weapons move from design to manufacturable form. The forge-smoke is constant. The noise of production never stops. Inside the district, the output can be measured in what arrives at every front in every coalition: Pattern-3 rifles, Hailmakers, Chromite Black components, and a third of everything else in active military use.',
        N'Two thousand-plus production workers and engineers; a smaller Myrmidon contingent than most House installations (Vulcanus''s power is production, not combat strength); Keeper and technical staff; House Atrax logistics contractors for the delivery network',
        N'The Forge Hearth produces. It does not sell for coin; it trades output for coalition commitments, supply priority agreements, and Catalyst access from Fornax. Keeper Maret Delys controls the production schedule, which means she controls the trading leverage.',
        N'Keeper Maret Delys is the institutional authority. A rotating committee of senior Paladins holds the Warrior King title and does not challenge the Keeper''s decisions. This is unique among the Seven Houses.',
        N'The Forge Hearth smells of metal and burning coal and working-machine oil. It is not comfortable in the way settlements aspire to be comfortable. The forge-glow is visible at night for twelve kilometers. The coal-haze is permanent and the eastern sky above the district has not been clear in forty years.',
        0.0, 0.0,
        N'medieval industrial fortress district, coal-smoke haze, foundries visible through stone arches, Scrying station tower at center, WW1-era weapons manufacturing aesthetic in stone and iron, forge-glow and grey industrial sky, Buehlman dark fantasy',
        N'industrial medieval district, forge works, coal smoke, stone chimneys, WW1 manufacturing aesthetic in fantasy',
        0, 0
    );
    PRINT 'The Forge Hearth seeded.';
END
GO

-- ── Bheur's Crossing — the contested bridge ───────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Places WHERE Name = N'Bheur''s Crossing')
BEGIN
    DECLARE @bheurId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@bheurId, N'place', N'Bheur''s Crossing', N'bheurs-crossing', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Places (Id, Name, Slug, Territory, Tier, Climate, Description, Demographics, Economy, PowerStructure, AtmosphereFeel, GeoLat, GeoLng, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @bheurId, N'Bheur''s Crossing', N'bheurs-crossing',
        N'Middle-front; Caul River crossing; nominally Noctua-Corvus coalition',
        N'Contested Site',
        N'Wet; the river creates its own microclimate; morning mist persistent and reliable — useful for cover, costly for visibility',
        N'The only passable bridge across the Caul River at the midpoint of the western front. Whoever holds Bheur''s Crossing controls the movement of anything larger than a supply pack from one side of the front to the other. Four different coalitions have held this crossing in the last century; it changes hands through formal assault, assassination, and treaty in proportions that vary by decade. The current Noctua-Corvus coalition has held for seven years — longer than most — primarily because Noctua''s night operations make a direct assault unacceptably costly and Corvus''s signal advantage makes a flanking operation difficult to conceal. The bridge itself has been repaired eleven times by documented record; the reconstruction materials become more elaborate with each iteration, each holder building in their own defensive design. The stones are worn smooth by generations of boot traffic. The reinforcement additions are industrial and the original construction is medieval and they have not been reconciled aesthetically.',
        N'The garrison rotates; currently Noctua-dominant with Corvus signal personnel embedded; civilian use of the crossing is nominally permitted and practically rare',
        N'Bheur''s Crossing is economic leverage disguised as military position. Control of the crossing translates directly to tariffs on coalition supply movement and bargaining power at the coalition table.',
        N'Noctua operational command (Dame Thessaly Brennan''s authority in the field); Corvus signal advisory (embedded, not commanding); joint casualty authority that has never been formally tested',
        N'The mist in the mornings. The specific sound of a river crossing that both sides know and neither side has been able to fully secure. The worn stones. The industrial additions that do not match the medieval base. The sense of being at a point the war will keep returning to.',
        0.0, 0.0,
        N'contested ancient bridge over a mist-covered river, medieval stone construction with industrial repair additions, WW1-era garrison aesthetic, Noctua and Corvus military presence, Buehlman dark fantasy, morning mist and grey light',
        N'ancient contested bridge, mist-covered river, medieval stone with industrial additions, WW1 military aesthetic in fantasy',
        0, 0
    );
    PRINT 'Bheur''s Crossing seeded.';
END
GO

-- ── The Amber Wards — House Fornax Catalyst complex ──────────────────────────
IF NOT EXISTS (SELECT 1 FROM Places WHERE Name = N'The Amber Wards')
BEGIN
    DECLARE @amberId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@amberId, N'place', N'The Amber Wards', N'the-amber-wards', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Places (Id, Name, Slug, Territory, Tier, Climate, Description, Demographics, Economy, PowerStructure, AtmosphereFeel, GeoLat, GeoLng, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @amberId, N'The Amber Wards', N'the-amber-wards',
        N'Interior; buffer zone protected by collective interest',
        N'Restricted Facility',
        N'Interior; dry compared to the fronts; the ventilation stacks produce amber-tinted atmospheric discoloration visible at twenty kilometers',
        N'House Fornax''s Catalyst production complex. Located in the interior at a distance from every active front that all parties have honored without a formal treaty, because no treaty was necessary — destroying the Amber Wards would be mutually assured logistical catastrophe. The complex is not large by manufacturing standards; Catalyst refinement requires precision rather than volume. What is large is the security apparatus and the buffer zone. The amber atmospheric staining from the ventilation stacks is visible at twenty kilometers in clear conditions. Inside the perimeter, access is controlled by authorization layers that even senior House Fornax personnel navigate with documentation. The Seat is the only authority who can authorize access to the inner production facility without a council vote. This has never been challenged.',
        N'Technical refinement staff (small, specialized, House Fornax-only); administrative personnel; a security apparatus primarily deterrent rather than combat-calibrated; the Seat''s administrative element',
        N'The Amber Wards is not where Catalyst is sold. It is where Catalyst is made. Sales occur through intermediaries and supply manifests. Nobody pays the Amber Wards directly; they pay House Fornax.',
        N'The Seat (Emric Haed) is the administrative and production authority. The technical refinement director reports to the Seat, not the council. There is no Warrior King of House Fornax.',
        N'Calm; deliberate; the amber staining in the air is noticeable at the perimeter and becomes more pronounced inside; the complex smells of something chemical that is not unpleasant but is not natural; institutional precision to everything, unlike the Houses'' military operations',
        0.0, 0.0,
        N'interior alchemical production complex, amber-tinted atmospheric staining from ventilation stacks, buffer zone perimeter, institutional precision medieval-industrial architecture, Fornax furnace heraldry, Buehlman dark fantasy, amber light quality',
        N'alchemical production facility, amber atmospheric tinting, buffer zone, furnace heraldry, medieval-industrial interior',
        0, 0
    );
    PRINT 'The Amber Wards seeded.';
END
GO

-- ── Caul Mor — the neutral market city ───────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Places WHERE Name = N'Caul Mor')
BEGIN
    DECLARE @caulMorId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@caulMorId, N'place', N'Caul Mor', N'caul-mor', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Places (Id, Name, Slug, Territory, Tier, Climate, Description, Demographics, Economy, PowerStructure, AtmosphereFeel, GeoLat, GeoLng, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @caulMorId, N'Caul Mor', N'caul-mor',
        N'Central interior; equidistant from multiple fronts by design',
        N'Neutral Settlement',
        N'Continental; cold winters, wet springs; natural bowl geography moderates wind',
        N'The only major settlement in the Cauld that all coalitions have agreed is not a military target. The agreement is economic, not ethical: Caul Mor is where everyone resupplies, treats their wounded beyond field-hospital capacity, rotates soldiers off the front, and conducts the informal diplomacy that the coalition table formalizes. A settlement of approximately forty thousand — an enormous concentration for the Cauld''s war-thinned populations. The neutral status is maintained by collective self-interest so robust that it has never required formal codification. Threatening Caul Mor is the one thing that would genuinely unite all seven Houses against a common actor.',
        N'Forty thousand permanent and rotating residents; highest diversity of any settlement in the Cauld; every House present in some institutional capacity; soldiers on rotation leave; merchants from every House; the Oathless in their hiring halls; physicians, Transmutation practitioners in civilian practice, and the permanent residents who have decided to live at the intersection of all of this',
        N'Supply, medicine, equipment, intelligence, contract labor. The Caul Mor market does not produce weapons; it moves everything produced elsewhere. The Oathless hiring halls are the largest civilian employment exchange in the territory.',
        N'A civilian council (no House controls it; all Houses have observers). The council''s authority is limited and largely administrative. The actual power in Caul Mor is the market, and the market belongs to whoever has goods that other people need.',
        N'Dense; crowded for the Cauld; the noise level is unusual for a territory defined by attrition. Soldiers on leave behave differently than soldiers in the field. Merchants negotiate loudly. The Oathless hiring halls produce a specific ambient sound of people trying to get work. The permanent residents navigate the chaos by instinct.',
        0.0, 0.0,
        N'dense neutral market settlement, high medieval architecture, WW1-era military personnel on leave mixed with merchants and Oathless contract workers, crowded market district, every House faction visible, Buehlman dark fantasy, morning market light',
        N'neutral medieval market city, crowded, every faction represented, WW1-era military mixed with civilian, dark fantasy',
        0, 0
    );
    PRINT 'Caul Mor seeded.';
END
GO

-- ── The Thin Places — distributed phenomenon sites ────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Places WHERE Name = N'The Thin Places')
BEGIN
    DECLARE @thinId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@thinId, N'place', N'The Thin Places', N'the-thin-places', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Places (Id, Name, Slug, Territory, Tier, Climate, Description, Demographics, Economy, PowerStructure, AtmosphereFeel, GeoLat, GeoLng, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @thinId, N'The Thin Places', N'the-thin-places',
        N'Distributed; specific geographic sites rather than a contiguous territory',
        N'Phenomenon Location',
        N'Varies by site; all confirmed sites share atmospheric crystal concentration above regional average, which appears to correlate with the membrane thinning',
        N'Collective term for geographic sites where the membrane between Spheres is demonstrably thinner — where a natural perception of cross-Sphere signals is theoretically possible without apparatus. The Mossland has a confirmed site. Ships Rock has one. The Corvin Station was built near one. There are believed to be several others. The Houses fight over confirmed Thin Places because the ability to Scry without an installation means the knowledge cannot be controlled by any House''s authorization system. An unauthorized Scrying from a Thin Place cannot be intercepted, logged, or taxed. Everything the Houses'' power apparatus depends on rests on controlling who can Scry and what they can see. A Thin Place that someone learns to use independently is a House''s strategic nightmare.',
        N'Thin Places are not inhabited; they are found, marked, and either incorporated into installations or actively suppressed',
        N'The economic value of a Thin Place is the independence it offers from House Scrying control — a negative value from the Houses'' perspective and a very high positive value from everyone else''s',
        N'Contested; any confirmed Thin Place triggers a House response within weeks of confirmation',
        N'The accounts vary by site: the Mossland site is wet and cold and smells of the bog; Ships Rock is exposed and loud with wind. The quality all accounts report in common is not atmospheric but cognitive — the sense that something being perceived is coming from somewhere the observer cannot see, and that they are perceiving it correctly. This is not metaphor. This is what the Thin Places do.',
        0.0, 0.0,
        N'mystical geographic sites with atmospheric crystal formations, membrane-between-worlds visual effect, medieval-fantasy landscape, liminal light quality, Buehlman dark fantasy, sense of presence from elsewhere',
        N'mystical liminal geographic sites, crystal formations, liminal light, dark fantasy',
        0, 0
    );
    PRINT 'The Thin Places seeded.';
END
GO


-- ╔═══════════════════════════════════════════════════════════════════════╗
-- ║  SECTION 4 — WEAPONS                                                 ║
-- ╚═══════════════════════════════════════════════════════════════════════╝

-- ── Pattern-3 Battle Rifle — standard Myrmidon issue ─────────────────────────
IF NOT EXISTS (SELECT 1 FROM Weapons WHERE Name = N'Pattern-3 Battle Rifle')
BEGIN
    DECLARE @p3Id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@p3Id, N'weapon', N'Pattern-3 Battle Rifle', N'pattern-3-battle-rifle', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Weapons (Id, Name, Slug, Manufacturer, Category, Tier, Legality, Description, Specifications, TacticalUse, CulturalContext, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @p3Id, N'Pattern-3 Battle Rifle', N'pattern-3-battle-rifle',
        N'House Vulcanus (primary); licensed variants from Houses Corvus and Noctua',
        N'Rifle',
        N'Standard Myrmidon issue',
        N'House-authorized',
        N'The standard-issue Myrmidon battle rifle. Bolt-action. Scry-derived from Sphere 1914-UK (catalogue designation SCRY-1914-UK-7; Lee-Enfield No.1 Mk.III pattern). Manufactured in Alloy 41 with Yggdra-wood stock. The Pattern-3 designation comes from the third-generation manufacturing refinement — the Scry-original design required metallurgical adjustment for Alloy 41 properties before reliable production at the Forge Hearth''s scale was achieved. Current production pattern functions within 4% of the Sphere-original specifications.',
        N'Bolt-action. 10-round magazine. Alloy 41 receiver. Yggdra-wood stock. Caliber: 7.7mm (local designation: Round 7.7). Bayonet-compatible (Pattern-3 bayonet is a separately Scried design). Effective range: 550m. Maximum range: 900m. Weight: 4.1kg unloaded.',
        N'Line infantry engagement at medium to long range; suppression; the primary weapon in any Myrmidon front-line action. A trained Myrmidon delivers accurate fire at the same rate as the Sphere-original design.',
        N'The Pattern-3 is the visible symbol of Myrmidon status. A conscript carrying a Sap-Axe and the soldier beside them carrying a Pattern-3 communicate their position in the House hierarchy without speaking. The weapon has been in production long enough that Forge Hearth versions and licensed variants are subtly distinguishable to anyone who has handled both — the stamping differs.',
        N'bolt-action rifle, Alloy 41 construction, Yggdra-wood stock, WW1 Lee-Enfield aesthetic translated into fantasy manufacturing, detailed hardware, Buehlman dark fantasy',
        N'bolt-action rifle, metal and wood construction, WW1 aesthetic in fantasy setting',
        0, 0
    );
    PRINT 'Pattern-3 Battle Rifle seeded.';
END
GO

-- ── Bergmann-Pattern Pistol — officer sidearm ────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Weapons WHERE Name = N'Bergmann-Pattern Pistol')
BEGIN
    DECLARE @bergId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@bergId, N'weapon', N'Bergmann-Pattern Pistol', N'bergmann-pattern-pistol', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Weapons (Id, Name, Slug, Manufacturer, Category, Tier, Legality, Description, Specifications, TacticalUse, CulturalContext, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @bergId, N'Bergmann-Pattern Pistol', N'bergmann-pattern-pistol',
        N'House Corvus (primary)',
        N'Pistol',
        N'Officer carry; limited production',
        N'House-authorized; officers and senior Myrmidons only',
        N'Semi-automatic pistol Scried from Sphere 1918-DE (catalogue designation SCRY-1918-DE-8; Bergmann-Bayard/Parabellum hybrid pattern). House Corvus began production as an officer sidearm when signal operations required a compact weapon that could be carried without impeding intelligence equipment. Alloy 41 construction with machined Alloy 41 grip panels — distinctively Corvus to people who know the difference. Production volume is low; the weapon carries a status signal.',
        N'Semi-automatic. 8-round magazine. Caliber: 9mm (local designation: Round 9C). Alloy 41 frame and grip panels. Length: 24cm. Weight: 0.9kg. Effective range: 50m.',
        N'Close-quarters; officer carry; signal station defense; last-resort weapon for intelligence personnel operating in hostile territory without primary weapon access.',
        N'The Bergmann-Pattern carries the specific status signal of a Corvus-affiliated officer. Having one on a non-Corvus soldier typically means the weapon was taken; this is considered in poor taste across most coalition agreements but not formally prohibited.',
        N'elegant semi-automatic pistol, Alloy 41 frame, machined grip panels, WW1 officer sidearm aesthetic translated into fantasy manufacturing, compact and precise, Buehlman dark fantasy',
        N'semi-automatic pistol, metal construction, WW1 officer sidearm aesthetic in fantasy setting',
        0, 0
    );
    PRINT 'Bergmann-Pattern Pistol seeded.';
END
GO

-- ── Miasma Mortar — chemical shell delivery, House Noctua ────────────────────
IF NOT EXISTS (SELECT 1 FROM Weapons WHERE Name = N'Miasma Mortar')
BEGIN
    DECLARE @miasmaId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@miasmaId, N'weapon', N'Miasma Mortar', N'miasma-mortar', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Weapons (Id, Name, Slug, Manufacturer, Category, Tier, Legality, Description, Specifications, TacticalUse, CulturalContext, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @miasmaId, N'Miasma Mortar', N'miasma-mortar',
        N'House Noctua',
        N'Mortar / Chemical delivery',
        N'Specialist; not standard Myrmidon issue',
        N'House Noctua authorization required; coalition notification required for use in shared operations',
        N'Chemical shell delivery system Scried from Sphere 1917-FR (catalogue designation SCRY-1917-FR-12; Stokes mortar with chemical-round adaptation). The Miasma Mortar is not a generic term — it refers specifically to the Noctua-manufactured system, which includes the mortar tube, the mounting bipod, and the House-proprietary Miasma shells. The gas formula is Noctua''s own development: the Sphere design was adapted and chemically refined through an internal process the House has not shared with coalition partners. The mortar delivers the shell. The gas is the weapon. Exposure produces respiratory restriction, visual degradation, and temporary disorientation. Fatality depends on concentration and exposure duration.',
        N'Muzzle-loading mortar tube. 81mm bore. Range: 200–1,800m. Crew: 2 minimum. Miasma shell type M-4 (current standard). Shell weight: 4.4kg. Gas yield: effective radius approximately 30m in standard wind conditions. Dispersion time: 8–12 minutes.',
        N'Pre-assault softening; defensive harassment; fortification clearance; used most often by House Noctua in the final hour before a night assault when the Miasma has time to settle before the infantry advance.',
        N'The weapon that makes other Houses uneasy about coalition with Noctua. The effectiveness is undeniable; the method produces a specific recoil among soldiers who have seen it used. Noctua''s position is that squeamishness is not tactical and the war does not reward it. Nobody has successfully argued otherwise at a coalition table.',
        N'WW1 Stokes mortar pattern with alchemical chemical shell, dark tactical aesthetic, Noctua House owl markings, mist and Miasma smoke in background, Buehlman dark fantasy military',
        N'mortar tube with chemical shells, WW1 aesthetic in fantasy setting, smoke and mist',
        0, 0
    );
    PRINT 'Miasma Mortar seeded.';
END
GO

-- ── The Hailmaker — Lewis Gun pattern, crew-served ───────────────────────────
IF NOT EXISTS (SELECT 1 FROM Weapons WHERE Name = N'The Hailmaker')
BEGIN
    DECLARE @hailId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@hailId, N'weapon', N'The Hailmaker', N'the-hailmaker', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Weapons (Id, Name, Slug, Manufacturer, Category, Tier, Legality, Description, Specifications, TacticalUse, CulturalContext, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @hailId, N'The Hailmaker', N'the-hailmaker',
        N'House Vulcanus (original); Houses Corvus and Noctua in licensed production',
        N'Machine gun (light, crew-served)',
        N'Myrmidon; specialist crew',
        N'House-authorized; crew-trained personnel only',
        N'Light machine gun Scried from Sphere 1915-UK (catalogue designation SCRY-1915-UK-3; Lewis Gun pattern). The weapon was named by the first Myrmidon crew who fired it during live testing — the sound was described as "like the sky throwing stones." The name preceded the formal designation and outlasted it. The pan magazine holds 47 rounds and feeds from the top. The barrel is air-cooled through a finned shroud adapted from the Sphere original using locally available aluminum-equivalent alloys. The weapon fires 550 rounds per minute and is the closest thing the Cauld has to sustained fire support at the squad level.',
        N'Gas-operated. 47-round top-fed pan magazine. Caliber: 7.7mm (cross-compatible with Pattern-3 Round 7.7). Weight: 11.8kg with bipod. Crew: 2 (gunner and loader-assistant). Rate of fire: 550rpm. Effective range: 600m.',
        N'Defensive position holding; suppression during assault; flanking support. Particularly effective in low-visibility conditions — the Hailmaker can be fired accurately in the same conditions that House Noctua''s night operations prefer.',
        N'A Hailmaker position is a tactical anchor. Attacking a defended position with a Hailmaker is a categorically different action than attacking without one. Soldiers understand this viscerally. The weapon has been in the Cauld long enough to have its own folklore: crew superstitions about the pan magazines, about what to do when the barrel runs hot, about the specific sound that means a jam is incoming.',
        N'WW1 Lewis Gun pattern light machine gun, top-fed pan magazine, air-cooled barrel shroud, Alloy 41 construction, crew-served bipod position, Buehlman dark fantasy military',
        N'light machine gun with pan magazine and barrel shroud, WW1 Lewis Gun aesthetic in fantasy setting',
        0, 0
    );
    PRINT 'The Hailmaker seeded.';
END
GO

-- ── Sap-Axe — conscript tool-weapon, local manufacture ───────────────────────
IF NOT EXISTS (SELECT 1 FROM Weapons WHERE Name = N'Sap-Axe')
BEGIN
    DECLARE @sapId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@sapId, N'weapon', N'Sap-Axe', N'sap-axe', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Weapons (Id, Name, Slug, Manufacturer, Category, Tier, Legality, Description, Specifications, TacticalUse, CulturalContext, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @sapId, N'Sap-Axe', N'sap-axe',
        N'Local fabrication (not Scried)',
        N'Melee / Entrenching',
        N'Conscript issue (universal)',
        N'Unrestricted',
        N'The tool-weapon that identifies a conscript at a distance. Not Scried. Not manufactured by any House. The Sap-Axe is fabricated by local blacksmiths in any settlement large enough to maintain a forge — a short-hafted axe with an entrenching-adze on the reverse blade, weighted toward the axe edge for combat use, toward the adze edge for trench work. Conscripts receive them at induction. Myrmidons stop carrying them when they receive their first Pattern-3. What distinguishes the Sap-Axe from a standard axe is the adze adaptation and the weight distribution: designed to be used in both roles without being exceptional at either.',
        N'Haft: Yggdra-wood, 45cm. Head: local iron, forged, not cast. Axe edge weight: 0.8kg. Adze reverse: 0.3kg. Total weight: 1.1kg. Balance point: 15cm below head. No standardization of manufacture; regional variation is expected.',
        N'Close combat (inadequate against a Myrmidon with a Pattern-3 except within grappling range); trench digging and reinforcement; field construction; the weapon used when everything else has failed or been lost.',
        N'The clearest marker of the gap between conscript and Myrmidon. A Myrmidon issued one as emergency equipment and a conscript issued one at induction look at the same object differently. The Sap-Axe is what the war costs the people the Houses have not invested in.',
        N'short-hafted war axe with entrenching adze reverse, local ironwork, Yggdra-wood haft, conscript-worn aesthetic, utilitarian and reliable, WW1 infantry entrenching tool aesthetic in fantasy',
        N'short-hafted axe with adze reverse, local ironwork, wood handle, conscript tool-weapon',
        0, 0
    );
    PRINT 'Sap-Axe seeded.';
END
GO


-- ╔═══════════════════════════════════════════════════════════════════════╗
-- ║  SECTION 5 — MATERIALS (Catalysts and Alchemical Substances)         ║
-- ╚═══════════════════════════════════════════════════════════════════════╝

-- ── Xerum 525 — primary Transmutation Catalyst ───────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Materials WHERE Name = N'Xerum 525')
BEGIN
    DECLARE @xerumId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@xerumId, N'material', N'Xerum 525', N'xerum-525', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Materials (Id, Name, Slug, Category, Tier, BrandName, ProductName, TierAvailability, Cost, Description, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @xerumId, N'Xerum 525', N'xerum-525',
        N'Catalyst',
        N'Restricted',
        N'Xerum 525',
        N'Xerum 525',
        N'House Fornax supply only; allocated through House-to-House agreements; individual access requires House authorization',
        N'Not commercially priced; traded as a political commodity through Fornax allocation agreements',
        N'The primary Transmutation Catalyst. The name comes from the House Fornax internal classification system: Catalyst Series X (the most refined), Refinement Run 525. The classification survived into common usage because no other name exists in any record accessible outside Fornax. What Xerum 525 does is known; what it is made of is classified. The refinement process produces a liquid of deep red-amber color — the color that names the Amber Wards'' atmospheric staining — stable for approximately ninety days at standard temperature before requiring re-stabilization. First infusion mortality rate: 80%. Administered correctly by a certified Transmutation practitioner to a person who survives the first infusion, the ascendance process begins within seventy-two hours.',
        N'deep red-amber alchemical liquid in glass containment vials, precise industrial packaging, medieval-alchemical aesthetic, House Fornax amber-and-furnace markings, clinical and controlled',
        N'red-amber liquid in glass vials, alchemical packaging, clinical aesthetic',
        0, 0
    );
    PRINT 'Xerum 525 seeded.';
END
GO

-- ── Catalyst Theta — experimental second Catalyst ─────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Materials WHERE Name = N'Catalyst Theta')
BEGIN
    DECLARE @thetaId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@thetaId, N'material', N'Catalyst Theta', N'catalyst-theta', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Materials (Id, Name, Slug, Category, Tier, BrandName, ProductName, TierAvailability, Cost, Description, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @thetaId, N'Catalyst Theta', N'catalyst-theta',
        N'Catalyst (experimental)',
        N'Experimental; restricted',
        N'Catalyst Theta',
        N'Catalyst Theta',
        N'Available only through direct House Fornax authorization; currently in controlled trial with three Houses; not on standard allocation',
        N'Higher than Xerum 525 per dose; experimental premium; price under active negotiation',
        N'The second Catalyst in House Fornax''s refinement portfolio. Catalyst Theta produces Transmutation at a measurably faster rate than Xerum 525 — the first-infusion response begins within 24 hours rather than 72, and subsequent stages progress 30–40% faster in controlled trials. The first-infusion mortality rate is approximately the same (75–80%; trial data not fully resolved). The accelerated progression comes with a corresponding reduction in transformation precision. Theta subjects show more variation in their transformation profiles than Xerum 525 subjects, and the botched-infusion monster profile is more rapidly achieved and larger in scale. House Fornax characterizes this as a dosing calibration problem under active refinement. The three Houses in the controlled trial have different views.',
        N'experimental alchemical catalyst, darker coloration than Xerum 525, darker red-brown, experimental trial labeling, controlled-environment context, House Fornax markings, medieval-alchemical aesthetic',
        N'dark red-brown experimental catalyst in sealed containers, experimental labeling',
        0, 0
    );
    PRINT 'Catalyst Theta seeded.';
END
GO

-- ── Ichor Compound — House Ophiuchus first-infusion stabilizer ───────────────
IF NOT EXISTS (SELECT 1 FROM Materials WHERE Name = N'Ichor Compound')
BEGIN
    DECLARE @ichorId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@ichorId, N'material', N'Ichor Compound', N'ichor-compound', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Materials (Id, Name, Slug, Category, Tier, BrandName, ProductName, TierAvailability, Cost, Description, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @ichorId, N'Ichor Compound', N'ichor-compound',
        N'Stabilizer (classified)',
        N'House Ophiuchus internal only',
        N'Ichor Compound',
        N'Ichor Compound',
        N'House Ophiuchus internal supply only; not traded; not confirmed to exist in any official communication',
        N'No market price; does not appear on any allocation list or coalition supply manifest',
        N'A stabilizing compound developed by House Ophiuchus''s research element. Administered as a preparatory treatment 48 hours before a first Xerum 525 infusion, it reduces first-infusion mortality from approximately 80% to approximately 60%. The mechanism is not formally documented in any record that has left Ophiuchus''s possession. The evidence for its existence comes from the disproportionate survival rates in Ophiuchus''s Myrmidon infusion records over the past twelve years compared to the baseline — a statistical anomaly that House Fornax has noticed and not yet acted on. House Ophiuchus has not disclosed the compound. Its name appears in one internal field incident memo produced during a Myrmidon-009 action that has not been formally transmitted to any other House.',
        N'clear alchemical stabilizer compound in medical containment, research context, House Ophiuchus serpent markings, clinical and secretive, medieval-alchemical aesthetic',
        N'clear stabilizer compound in medical containers, clinical, secretive aesthetic',
        0, 0
    );
    PRINT 'Ichor Compound seeded.';
END
GO

-- ── Chromite Black — House Vulcanus high-strength metallurgical material ──────
IF NOT EXISTS (SELECT 1 FROM Materials WHERE Name = N'Chromite Black')
BEGIN
    DECLARE @chromId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@chromId, N'material', N'Chromite Black', N'chromite-black', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Materials (Id, Name, Slug, Category, Tier, BrandName, ProductName, TierAvailability, Cost, Description, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @chromId, N'Chromite Black', N'chromite-black',
        N'Metallurgical material',
        N'House Vulcanus proprietary',
        N'Chromite Black',
        N'Chromite Black',
        N'Produced exclusively at the Forge Hearth; sold only to Houses in current coalition with Vulcanus',
        N'Significantly above Alloy 41 pricing; priced as a strategic coalition incentive',
        N'A Scried metallurgical material derived from a Sphere in which industrialized chromium-alloyed steel production was already established (catalogue designation SCRY-1912-RU-4). House Vulcanus adapted the design for locally available mineral inputs over approximately two decades of development. The result has 2.3x the tensile strength of Alloy 41 in standardized testing, with specific application to weapon barrel and receiver linings, and structural reinforcement in installation components and shield plating. The material is a dark grey-black — distinctive enough that weapons or installations using Chromite Black components are identifiable to anyone who knows what they are looking at. This is a feature, not a flaw: it is also a coalition signal.',
        N'dark chromite steel alloy bars and components, high-sheen dark grey-black finish, Forge Hearth manufacturing marks, industrial medieval aesthetic, Buehlman dark fantasy',
        N'dark grey-black steel alloy, high-sheen finish, industrial manufacturing marks',
        0, 0
    );
    PRINT 'Chromite Black seeded.';
END
GO

-- ── Monster Meat — the ungoverned catalyst, the Houses'' suppressed secret ─────
IF NOT EXISTS (SELECT 1 FROM Materials WHERE Name = N'Monster Meat')
BEGIN
    DECLARE @meatId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@meatId, N'material', N'Monster Meat', N'monster-meat', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Materials (Id, Name, Slug, Category, Tier, BrandName, ProductName, TierAvailability, Cost, Description, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @meatId, N'Monster Meat', N'monster-meat',
        N'Catalyst (ungoverned; prohibited)',
        N'Field hazard; universal',
        N'Monster Meat',
        N'Monster Meat',
        N'Encountered in field conditions; consumed by desperate, uninformed, or profoundly hungry soldiers; never deliberately distributed',
        N'No cost. No market. The consequences are the entire price.',
        N'The flesh of the Cauld''s native creatures. Specifically: the tissue of creatures that have been part of the Cauld''s ecosystem long enough to carry whatever biological mechanism the Catalysts artificially introduce into a controlled Transmutation. When consumed in sufficient quantity under conditions that produce biological absorption rather than digestion, the Transmutation mechanism activates without a Catalyst, without a practitioner, without a sequence, and without control. The result is the same as a botched infusion: uncontrolled catastrophic mutation, beginning within days, accelerating without a ceiling, fatal to the subject and to the people nearby. The Houses know this. The knowledge is not widely distributed because distributing it would require acknowledging that Transmutation and Monster Meat are the same biological process. This is the ideological contradiction that the Houses'' entire legitimacy depends on not surfacing.',
        N'raw creature flesh in field conditions, obviously dangerous, desperate conscripts nearby, Buehlman dark fantasy horror register, uncontrolled biological threat',
        N'raw dangerous flesh, field conditions, horror aesthetic',
        0, 0
    );
    PRINT 'Monster Meat seeded.';
END
GO

-- ── Alloy 41 — the Cauld''s standard steel equivalent ─────────────────────────
IF NOT EXISTS (SELECT 1 FROM Materials WHERE Name = N'Alloy 41')
BEGIN
    DECLARE @alloyId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@alloyId, N'material', N'Alloy 41', N'alloy-41', N'canon', 1, '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Materials (Id, Name, Slug, Category, Tier, BrandName, ProductName, TierAvailability, Cost, Description, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @alloyId, N'Alloy 41', N'alloy-41',
        N'Metallurgical material',
        N'Standard production (all Houses)',
        N'Alloy 41',
        N'Alloy 41',
        N'Standard production across all Houses; no monopoly; regional access varies by mineral deposits and refining capacity; House Vulcanus produces most of it',
        N'Standard industrial pricing; Vulcanus''s production volume sets the effective market price',
        N'The Cauld''s designation for locally produced steel equivalent. The name derives from the Scrying catalogue designation SCRY-1815-UK-41, which described crucible steel production from a Sphere whose industrialized metallurgy had already produced a quality product. House Vulcanus Scried this before the Forge Hearth was what it is now; Alloy 41 is the material the Forge Hearth''s expansion was built on. It is now produced across all Houses in minor volumes and by Vulcanus at industrial scale. The material designation has become the generic term — even Houses that Scried their own metallurgical references use the Alloy 41 name because that is what the material is called. It is to the Cauld what steel is to the source Sphere: the material the war is made from.',
        N'industrial steel bars and ingots, Cauld manufacturing stamps, Forge Hearth production aesthetic, medieval-industrial setting, Buehlman dark fantasy',
        N'steel bars and ingots, industrial manufacturing marks, medieval setting',
        0, 0
    );
    PRINT 'Alloy 41 seeded.';
END
GO


PRINT '═══════════════════════════════════════════════════════════════';
PRINT 'CAULD EXPANSION SEED COMPLETE';
PRINT '6 Houses  |  7 Characters  |  6 Places  |  5 Weapons  |  6 Materials';
PRINT '═══════════════════════════════════════════════════════════════';

