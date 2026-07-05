-- tools/seed_cauld_seven_houses.sql
-- The Seven Houses of the Cauld — canonical entity seed
-- Universe: Fantasy / Steampunk (0197E9C9-0002-7000-8000-000000000002)
-- 2026-07-04
--
-- Operations performed:
--   UPDATE  House Fornax    (3AED2F41) — old design superseded, updated to new canon
--   UPDATE  House Corvin    (019ED86A) — renamed from House Corvus, updated to new canon
--   RETIRE  House Atrax / Cetus / Noctua / Vulcanus — superseded old-design Houses (IsActive=0)
--   INSERT  House Draught / Atrament / Calyx / Thresh / Pallor — five new Houses
--   INSERT  7 Scrying Installations as Places
--   INSERT  FactionGoals (2 per House)
--   INSERT  FactionMethods (2 per House)
--   INSERT  FactionRelationships (coalition structure)

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

DECLARE @now      DATETIME2        = GETUTCDATE();
DECLARE @fantasy  UNIQUEIDENTIFIER = '0197E9C9-0002-7000-8000-000000000002';

-- ─────────────────────────────────────────────────────────────────────────────
-- Existing IDs (carried over from old design)
-- ─────────────────────────────────────────────────────────────────────────────
DECLARE @fornaxId UNIQUEIDENTIFIER = '3AED2F41-089E-473A-98E6-82F301343E74'; -- House Fornax
DECLARE @corvinId UNIQUEIDENTIFIER = '019ED86A-2874-765E-99BA-BC83E4F97026'; -- House Corvus → Corvin

-- ─────────────────────────────────────────────────────────────────────────────
-- New House IDs
-- ─────────────────────────────────────────────────────────────────────────────
DECLARE @draughtId  UNIQUEIDENTIFIER = NEWID();
DECLARE @atramentId UNIQUEIDENTIFIER = NEWID();
DECLARE @calyxId    UNIQUEIDENTIFIER = NEWID();
DECLARE @threshId   UNIQUEIDENTIFIER = NEWID();
DECLARE @pallorId   UNIQUEIDENTIFIER = NEWID();

-- ─────────────────────────────────────────────────────────────────────────────
-- Installation (Place) IDs
-- ─────────────────────────────────────────────────────────────────────────────
DECLARE @forgeHearthId   UNIQUEIDENTIFIER = NEWID();
DECLARE @corvinStationId UNIQUEIDENTIFIER = NEWID();
DECLARE @musterChamId    UNIQUEIDENTIFIER = NEWID();
DECLARE @deepArchiveId   UNIQUEIDENTIFIER = NEWID();
DECLARE @calyxStationId  UNIQUEIDENTIFIER = NEWID();
DECLARE @threshChamId    UNIQUEIDENTIFIER = NEWID();
DECLARE @pallorStId      UNIQUEIDENTIFIER = NEWID();

-- ═════════════════════════════════════════════════════════════════════════════
-- 1. RETIRE superseded old Houses (no affiliations, no FK dependencies)
-- ═════════════════════════════════════════════════════════════════════════════

UPDATE Entities
SET IsActive = 0, Status = N'retired', ModifiedAt = @now
WHERE Id IN (
    'ED8CFFD0-732C-48A1-B433-E8B9B93F2687',  -- House Atrax
    '5E10AE1F-8A85-461C-B844-F461037058D7',  -- House Cetus
    '5D2AD2EE-8B93-4E8D-B92D-583CA9081438',  -- House Noctua
    'E2FA1827-AF1A-400B-A703-B67230D23CA7'   -- House Vulcanus
);
PRINT N'[1/8] Retired House Atrax, Cetus, Noctua, Vulcanus (IsActive=0)';

-- ═════════════════════════════════════════════════════════════════════════════
-- 2. UPDATE House Fornax (existing entity, new canon data)
-- ═════════════════════════════════════════════════════════════════════════════

UPDATE Entities SET
    Name       = N'House Fornax',
    Slug       = N'house_fornax',
    Status     = N'active',
    IsActive   = 1,
    Description = N'House Fornax holds the upper end of the Catalyst supply chain. They refine and distribute what the Liturgy supplies. Their institutional position depends entirely on this role. Fornax believes it understands what it is processing. It does not.',
    ModifiedAt  = @now
WHERE Id = @fornaxId;

UPDATE Factions SET
    Name              = N'House Fornax',
    Sector            = N'Catalyst refining',
    Tier              = N'House',
    Allegiance        = N'Coalition anchor (Coalition I)',
    Motto             = N'The Refinery Seat',
    Territory         = N'The Forge Hearth — oldest active apparatus in the Cauld; stretching the membrane at the same coordinates for generations; whether the membrane fully recovers between sessions has never been authorized for investigation',
    Leadership        = N'The Seat (institutional authority); The Warrior King; The Keeper (technical authority over the Scrying station)',
    Ideology          = N'Controls the upper end of the Catalyst supply chain; refines and distributes what the Liturgy supplies; material intermediary between the Liturgy''s acquisition mechanism and the Houses that consume it for the Gifted Ceremony; institutional position depends entirely on this role',
    NarrativeFunction = N'Power depends on not asking where the Catalyst comes from; the Liturgy supplies the upper end and Fornax processes and distributes — the question of acquisition mechanism has never been pressed; Fornax has made institutional peace with this gap because pressing the question would end the relationship that makes them indispensable; the Liturgy knows Fornax does not understand what it handles and has not corrected this; the silence is the arrangement',
    Description       = N'House Fornax holds the upper end of the Catalyst supply chain. They refine and distribute what the Liturgy supplies. Their institutional position depends entirely on this role. Fornax believes it understands what it is processing. It does not.',
    Slug              = N'house_fornax'
WHERE Id = @fornaxId;

PRINT N'[2/8] Updated House Fornax';

-- ═════════════════════════════════════════════════════════════════════════════
-- 3. RENAME + UPDATE House Corvus → House Corvin
-- ═════════════════════════════════════════════════════════════════════════════

UPDATE Entities SET
    Name        = N'House Corvin',
    Slug        = N'house_corvin',
    Status      = N'active',
    IsActive    = 1,
    Description = N'House Corvin holds thin-membrane access points no other House can reach. Their vigil operators are the most experienced in the Cauld — some stationed at the same post for forty years. Their most valuable operators are becoming unmanageable.',
    ModifiedAt  = @now
WHERE Id = @corvinId;

UPDATE Factions SET
    Name              = N'House Corvin',
    Sector            = N'Thin-membrane access / vigil operations',
    Tier              = N'House',
    Allegiance        = N'Coalition anchor (Coalition I)',
    Motto             = N'The Vigil Seat',
    Territory         = N'Corvin Station — oldest installation in the Cauld; named in historical record more than any other; the membrane here has been stretched at these coordinates longer than any other site; long-tenure vigil operators describe the membrane as something that breathes',
    Leadership        = N'The Seat (institutional authority); The Warrior King; The Keeper (technical authority over the Scrying station)',
    Ideology          = N'Controls Sphere access points no other House can reach; holds the most contested coordinates in the Cauld for location-specific Spheres that yield Gifted matter; vigil operators are the most experienced in the Cauld; some have been at the same post for forty years',
    NarrativeFunction = N'Most valued operators have been receiving Catalyst infusions tuned for observation work and are approaching functional ascendance — perception at thin-membrane sites beyond apparatus range; the House cannot promote them into combat structure and cannot demote them without losing the capability; the vigil operators who have been on the long watch longest are the ones the House most needs and can least manage',
    Description       = N'House Corvin holds thin-membrane access points no other House can reach. Their vigil operators are the most experienced in the Cauld — some stationed at the same post for forty years. Their most valuable operators are becoming unmanageable.',
    Slug              = N'house_corvin'
WHERE Id = @corvinId;

PRINT N'[3/8] Renamed House Corvus → House Corvin and updated data';

-- ═════════════════════════════════════════════════════════════════════════════
-- 4. INSERT five new House entities (Draught, Atrament, Calyx, Thresh, Pallor)
-- ═════════════════════════════════════════════════════════════════════════════

-- House Draught
INSERT INTO Entities (Id, EntityType, Name, Slug, Status, Description, CreatedAt, ModifiedAt, IsActive, UniverseId)
VALUES (@draughtId, N'faction', N'House Draught', N'house_draught', N'active',
    N'House Draught holds the largest standing Myrmidon force. A significant fraction of it arrived through the membrane. The Warrior King has been watching the same soldiers for thirty years and has said nothing about what she knows.',
    @now, @now, 1, @fantasy);

INSERT INTO Factions (Id, Name, Sector, Tier, Allegiance, Motto, Territory, Leadership, Ideology, NarrativeFunction, Description, Slug, Rating, VoteCount, MidjourneyPrompt, Dalle3Prompt)
VALUES (@draughtId,
    N'House Draught',
    N'Military conscription',
    N'House',
    N'Opposition bloc',
    N'The Muster Seat',
    N'The Muster Chamber — calibrated specifically for piercing; specialized apparatus for breaching the membrane and bringing soldiers through; the capability is not publicized',
    N'The Seat (institutional authority); The Warrior King (has watched the same soldiers for thirty years); The Keeper (technical authority over the Scrying station)',
    N'Holds the largest standing Myrmidon force in the current coalition map; a notable fraction arrived through the membrane; oath administered after arrival before the new conscript has enough language to understand what they are swearing; the service record opens and the name of the origin Sphere is not in it; Houses do not distinguish pierced conscripts from willing volunteers in paperwork',
    N'Warrior King has been watching for thirty years — knows which soldiers learned Cauld language faster than a person could and which ones looked at the sky the wrong way on arrival; has not filed anything about it; has made the calculation that telling costs something and staying quiet costs nothing; after thirty years she has stayed quiet long enough that telling would now mean explaining why she waited thirty years to tell',
    N'House Draught holds the largest standing Myrmidon force. A significant fraction of it arrived through the membrane. The Warrior King has been watching the same soldiers for thirty years and has said nothing about what she knows.',
    N'house_draught', 0, 0, N'', N'');

-- House Atrament
INSERT INTO Entities (Id, EntityType, Name, Slug, Status, Description, CreatedAt, ModifiedAt, IsActive, UniverseId)
VALUES (@atramentId, N'faction', N'House Atrament', N'house_atrament', N'active',
    N'House Atrament holds the broadest Sphere catalogue of any House. They know more about what exists across the membrane than any other institution — and publicly harbor the one genuine ideological challenge to the entire power structure.',
    @now, @now, 1, @fantasy);

INSERT INTO Factions (Id, Name, Sector, Tier, Allegiance, Motto, Territory, Leadership, Ideology, NarrativeFunction, Description, Slug, Rating, VoteCount, MidjourneyPrompt, Dalle3Prompt)
VALUES (@atramentId,
    N'House Atrament',
    N'Sphere cataloguing / knowledge',
    N'House',
    N'Opposition bloc',
    N'The Archive Seat',
    N'The Deep Archive — broadest Sphere catalogue of any House; more documented access points than any installation except Corvin Station; where Corvin has depth at specific coordinates, Atrament has breadth',
    N'The Seat (the Keeper of the Archive); The Warrior King; The Keeper (technical authority over the Scrying station)',
    N'Controls knowledge of what exists across the membrane — not always the ability to act on it; an installation that knows more than it can manufacture or deploy understands its own structural weakness with unusual clarity; the gap between knowing and the ability to act is where their politics live',
    N'Houses the real theological fault line of the Cauld: the position — voiced rarely and dangerously — that Scrying installations should not be privately controlled; that the knowledge of all Spheres belongs to everyone who lives in the Cauld; the Keeper has not suppressed it; whether this is principled tolerance or strategic positioning is the question the other Houses are asking; a House that controls the most knowledge and publicly harbors the belief that knowledge should be uncontrolled is either the most dangerous House in the coalition map or the most naive — no one has decided which yet',
    N'House Atrament holds the broadest Sphere catalogue of any House. They know more about what exists across the membrane than any other institution — and publicly harbor the one genuine ideological challenge to the entire power structure.',
    N'house_atrament', 0, 0, N'', N'');

-- House Calyx
INSERT INTO Entities (Id, EntityType, Name, Slug, Status, Description, CreatedAt, ModifiedAt, IsActive, UniverseId)
VALUES (@calyxId, N'faction', N'House Calyx', N'house_calyx', N'active',
    N'House Calyx controls food supply. The Granary Seat knows how many of its former people are eating Monster Meat. The House calls them monsters. It does not ask what they used to be.',
    @now, @now, 1, @fantasy);

INSERT INTO Factions (Id, Name, Sector, Tier, Allegiance, Motto, Territory, Leadership, Ideology, NarrativeFunction, Description, Slug, Rating, VoteCount, MidjourneyPrompt, Dalle3Prompt)
VALUES (@calyxId,
    N'House Calyx',
    N'Territory and supply / agriculture',
    N'House',
    N'Unaligned',
    N'The Granary Seat',
    N'Calyx Station — sits on territory rich in agricultural land and one of the highest monster-predator incursion corridors in the Cauld; the installation is not the most powerful on the map but the territory is the most contested for reasons that have nothing to do with membrane access',
    N'The Seat (holds institutional authority; knows the Monster Meat exposure numbers); The Warrior King; The Keeper (technical authority over the Scrying station)',
    N'Controls a significant portion of food supply for the current theater; when Calyx supply lines are intact the coalition they feed survives campaigns; when supply lines are cut their people eat what is available; in monster territory Monster Meat is available; the Monster Meat exposure rate among Calyx Oathless is the highest of any House',
    N'Monster Meat exposure rate among Calyx Oathless is the highest of any House — the governing failure metric made visible; the Seat knows the number and calls them monsters when they come back changed; the House hunts them without asking what they used to be or looking too closely at the faces; the parallel between guided Gifted ascendance and unguided Monster Meat transformation is most legible at Calyx and no one in the House has been willing to read it',
    N'House Calyx controls food supply. The Granary Seat knows how many of its former people are eating Monster Meat. The House calls them monsters. It does not ask what they used to be.',
    N'house_calyx', 0, 0, N'', N'');

-- House Thresh
INSERT INTO Entities (Id, EntityType, Name, Slug, Status, Description, CreatedAt, ModifiedAt, IsActive, UniverseId)
VALUES (@threshId, N'faction', N'House Thresh', N'house_thresh', N'active',
    N'House Thresh weaponizes negative Transmutation. Their sabotage operations have run long enough that their own supply chain is now compromised. Their Lectors know. The Silence has not responded yet.',
    @now, @now, 1, @fantasy);

INSERT INTO Factions (Id, Name, Sector, Tier, Allegiance, Motto, Territory, Leadership, Ideology, NarrativeFunction, Description, Slug, Rating, VoteCount, MidjourneyPrompt, Dalle3Prompt)
VALUES (@threshId,
    N'House Thresh',
    N'Covert operations / sabotage',
    N'House',
    N'Opposition bloc',
    N'The Cutting Seat',
    N'The Thresh Chamber — modest primary apparatus by House standards; their operational advantage is not what they can Scry but what they can deny others; the installation matters less than the network built around it',
    N'The Seat; The Warrior King; The Keeper; Lectors who have filed contamination reports through internal channels that the Silence has not acknowledged',
    N'Controls the sabotage economy: tainted Catalyst supplies, assassinated Transmuters; a House that loses its senior Transmuters to enemy action risks producing monsters from its own loyal soldiers when the next dose is administered by an undertrained practitioner with a compromised supply; Thresh weaponizes this deliberately',
    N'Their own Catalyst supply chain is compromised from running too many tainted operations — the same methods used to contaminate enemy supplies create the conditions for contamination to travel back; the provenance of any given Catalyst batch in Thresh''s inventory is no longer fully traceable; their Lectors know and have flagged it through internal channels; the Silence has not responded — yet',
    N'House Thresh weaponizes negative Transmutation. Their sabotage operations have run long enough that their own supply chain is now compromised. Their Lectors know. The Silence has not responded yet.',
    N'house_thresh', 0, 0, N'', N'');

-- House Pallor
INSERT INTO Entities (Id, EntityType, Name, Slug, Status, Description, CreatedAt, ModifiedAt, IsActive, UniverseId)
VALUES (@pallorId, N'faction', N'House Pallor', N'house_pallor', N'active',
    N'House Pallor holds a mid-tier installation and an affiliated Champion who predates the current Houses. Both coalition blocs are watching and waiting. The Champion has said nothing.',
    @now, @now, 1, @fantasy);

INSERT INTO Factions (Id, Name, Sector, Tier, Allegiance, Motto, Territory, Leadership, Ideology, NarrativeFunction, Description, Slug, Rating, VoteCount, MidjourneyPrompt, Dalle3Prompt)
VALUES (@pallorId,
    N'House Pallor',
    N'Champion affiliation / strategic patience',
    N'House',
    N'Unaligned — courted by both blocs',
    N'The Long Seat',
    N'Pallor Station — mid-tier apparatus; not the most powerful installation on the coalition map; strategically valuable not for what it can reach through the membrane but for who is affiliated with it',
    N'The Seat; The Warrior King (has been at the map table longer than any other living person; has watched coalition alignments cycle through three generations; remembers who the original sides were; has stayed quiet); affiliated Champion (not sworn — affiliated; no House owns a Champion; this one completed the arc from designation to name long ago)',
    N'Holds an affiliated Champion — not sworn, not commanded; the transition from designation to name is the arc that ends House structure''s claim on a person; this Champion completed that arc long ago and has not moved on; still here, still affiliated with a mid-tier unaligned House, in a position that benefits neither bloc in any obvious way',
    N'Both coalition anchors are quietly asking what the Champion is waiting for; what does someone who has been fighting this war long enough to remember its original shape know that makes staying at Pallor Station the correct move; the Champion has not volunteered; no one has asked them directly; the Warrior King who also remembers has stayed quiet for the same reason the other Houses have — they are afraid of the answer',
    N'House Pallor holds a mid-tier installation and an affiliated Champion who predates the current Houses. Both coalition blocs are watching and waiting. The Champion has said nothing.',
    N'house_pallor', 0, 0, N'', N'');

PRINT N'[4/8] Inserted 5 new Houses (Draught, Atrament, Calyx, Thresh, Pallor)';

-- ═════════════════════════════════════════════════════════════════════════════
-- 5. INSERT Scrying Installations as Places
-- ═════════════════════════════════════════════════════════════════════════════

-- The Forge Hearth (Fornax)
INSERT INTO Entities (Id, EntityType, Name, Slug, Status, Description, CreatedAt, ModifiedAt, IsActive, UniverseId)
VALUES (@forgeHearthId, N'place', N'The Forge Hearth', N'the_forge_hearth', N'active',
    N'Oldest active Scrying apparatus in the Cauld. Has been stretching the membrane at the same coordinates for generations. House Fornax installation.',
    @now, @now, 1, @fantasy);

INSERT INTO Places (Id, Name, Territory, Tier, Climate, Rating, VoteCount, Description, Demographics, Economy, PowerStructure, MidjourneyPrompt, Dalle3Prompt, AtmosphereFeel, GeoLat, GeoLng, Slug)
VALUES (@forgeHearthId,
    N'The Forge Hearth',
    N'Interior; House Fornax administrative buffer zone surrounding the installation',
    N'Scrying Installation',
    N'Stone and iron; controlled for apparatus operation; the heat from the refining process is constant',
    0, 0,
    N'Oldest active Scrying apparatus in the Cauld. Has been stretching the membrane at the same coordinates for generations. Whether the membrane fully recovers between sessions is not a question House Fornax has authorized anyone to investigate.',
    N'House Fornax administrators and technical staff; Catalyst processing and refining workers; Liturgy Lectors during supply assessments and distribution reviews',
    N'Catalyst refining and distribution — the upper end of the supply chain for every House that administers the Gifted Ceremony; the material intermediary between the Liturgy''s acquisition mechanism and the Houses that consume it',
    N'House Fornax holds institutional control; the Liturgy controls the supply chain''s upper end and neither party has formalized what that means for governance of the facility',
    N'', N'',
    N'The sound of the apparatus is a constant low register. The stone absorbs heat from the refining operation. Foremen who have been here for a decade no longer hear it. A Lector arriving for a distribution review will notice it. No one mentions it.',
    0, 0, N'the_forge_hearth');

-- Corvin Station (Corvin)
INSERT INTO Entities (Id, EntityType, Name, Slug, Status, Description, CreatedAt, ModifiedAt, IsActive, UniverseId)
VALUES (@corvinStationId, N'place', N'Corvin Station', N'corvin_station', N'active',
    N'Oldest installation in the Cauld. Named in historical record more than any other. House Corvin installation. The membrane here has been stretched at these coordinates longer than any other site.',
    @now, @now, 1, @fantasy);

INSERT INTO Places (Id, Name, Territory, Tier, Climate, Rating, VoteCount, Description, Demographics, Economy, PowerStructure, MidjourneyPrompt, Dalle3Prompt, AtmosphereFeel, GeoLat, GeoLng, Slug)
VALUES (@corvinStationId,
    N'Corvin Station',
    N'Northern reaches; elevated ridge country where the atmospheric crystal access is optimal; highest contested coordinates in the Cauld for location-specific Spheres that yield Gifted matter',
    N'Scrying Installation',
    N'High elevation; ridge wind audible through the stone; the vigil operators learn to sleep through it; cold most of the year',
    0, 0,
    N'Oldest installation in the Cauld. Named in historical record more than any other. The membrane here has been stretched at these coordinates longer than any other site. Long-tenure vigil operators stationed here for decades sometimes describe the membrane as something that breathes — something that responds to them, not to the apparatus. They say this to each other on night watches. They stop saying it when House administration is present. It is not in any formal record.',
    N'House Corvin vigil operators — some stationed for forty years or more; apparatus technicians; Liturgy Lectors during Relic assessments; administrative staff who rotate through on shorter terms and do not notice what the long-watch operators are not saying',
    N'Location-specific Sphere access — Gifted matter from Spheres reachable only from this ridge; the most valued import that no Scrying Chamber elsewhere can produce; the vigil operators are the institutional knowledge',
    N'House Corvin holds the ridge and therefore the access point; the most senior vigil operators are the actual capability — the House administers but the operators know the membrane; this asymmetry is what the House cannot resolve',
    N'', N'',
    N'Night watch at Corvin Station. The apparatus is off. The vigil operator has been at this post for thirty-seven years. They are not watching the equipment. They are watching something the equipment cannot see. They will not say what. They stop saying it when administration is present.',
    0, 0, N'corvin_station');

-- The Muster Chamber (Draught)
INSERT INTO Entities (Id, EntityType, Name, Slug, Status, Description, CreatedAt, ModifiedAt, IsActive, UniverseId)
VALUES (@musterChamId, N'place', N'The Muster Chamber', N'the_muster_chamber', N'active',
    N'House Draught''s Scrying installation, calibrated specifically for piercing. When numbers need supplementing, this is where the membrane gets breached and soldiers come through. The capability is not publicized.',
    @now, @now, 1, @fantasy);

INSERT INTO Places (Id, Name, Territory, Tier, Climate, Rating, VoteCount, Description, Demographics, Economy, PowerStructure, MidjourneyPrompt, Dalle3Prompt, AtmosphereFeel, GeoLat, GeoLng, Slug)
VALUES (@musterChamId,
    N'The Muster Chamber',
    N'House Draught administrative territory; location not publicized outside the House',
    N'Scrying Installation',
    N'Stone; functional; wide enough for a formation; designed for intake; the oath is administered here',
    0, 0,
    N'House Draught''s Scrying installation, calibrated specifically for piercing. When volunteer intake falls short — after costly campaigns, before planned offensives — this is where the membrane gets breached and soldiers come through. The apparatus here is specialized. The capability is not publicized.',
    N'House Draught apparatus operators; military intake administrators; Liturgy Lectors for oath administration; newly arrived conscripts during the intake window — before they have enough language to understand what they are swearing',
    N'Military conscription supplementation; the intake mechanism for Myrmidon numbers that volunteer enrollment cannot meet; the service record opened here does not note the origin Sphere',
    N'House Draught administers; the Liturgy oversees the oath; neither party discusses what comes through before the oath is administered',
    N'', N'',
    N'The chamber is functional. No ornament. The incoming arrive disoriented — one moment their own world, the next the stone chamber. The oath is administered before they have enough Cauld language to understand what they are swearing. The chamber clears. The service record opens. The same formation they join does not know who chose to be there.',
    0, 0, N'the_muster_chamber');

-- The Deep Archive (Atrament)
INSERT INTO Entities (Id, EntityType, Name, Slug, Status, Description, CreatedAt, ModifiedAt, IsActive, UniverseId)
VALUES (@deepArchiveId, N'place', N'The Deep Archive', N'the_deep_archive', N'active',
    N'House Atrament''s Scrying installation. Broadest Sphere catalogue of any House. More documented access points than any installation except Corvin Station.',
    @now, @now, 1, @fantasy);

INSERT INTO Places (Id, Name, Territory, Tier, Climate, Rating, VoteCount, Description, Demographics, Economy, PowerStructure, MidjourneyPrompt, Dalle3Prompt, AtmosphereFeel, GeoLat, GeoLng, Slug)
VALUES (@deepArchiveId,
    N'The Deep Archive',
    N'House Atrament territory; the archive itself is the strategic asset — what the cataloguing system contains is what makes the location valuable',
    N'Scrying Installation',
    N'Stone with extensive shelving and catalogue storage; scribes and apparatus operators share the physical space; the accumulation of documented observations is visible in the architecture',
    0, 0,
    N'House Atrament''s Scrying installation with the broadest Sphere catalogue of any House. More documented access points than any installation except Corvin Station. Where Corvin has depth at specific coordinates, Atrament has breadth: more Spheres observed, more designs transcribed, more knowledge of what exists across the membrane than any other institution in the Cauld.',
    N'House Atrament apparatus operators; Sphere catalogue scribes; military design researchers from visiting Houses; ideological faction members who believe this knowledge should not be privately controlled',
    N'Sphere knowledge — designs observed, catalogued, and available for manufacture; research access granted to other Houses as diplomatic and economic leverage; what is withheld is as important as what is shared',
    N'House Atrament holds the installation and the catalogue; the Keeper holds both the technical authority and the ideological question the House has not resolved; the other Houses send research delegations here and do not ask what the Keeper has said about the knowledge being uncontrolled',
    N'', N'',
    N'The scribes work in shifts. The catalogue extends into rooms whose contents were assessed before the current Keeper was born. Someone who has read enough of it begins to understand how much was observed and never built. The gap between the two is the scale of what was decided not to share.',
    0, 0, N'the_deep_archive');

-- Calyx Station (Calyx)
INSERT INTO Entities (Id, EntityType, Name, Slug, Status, Description, CreatedAt, ModifiedAt, IsActive, UniverseId)
VALUES (@calyxStationId, N'place', N'Calyx Station', N'calyx_station', N'active',
    N'House Calyx''s Scrying installation. Sits on agricultural territory and one of the highest monster-predator incursion corridors in the Cauld. Contested for food supply, not membrane access.',
    @now, @now, 1, @fantasy);

INSERT INTO Places (Id, Name, Territory, Tier, Climate, Rating, VoteCount, Description, Demographics, Economy, PowerStructure, MidjourneyPrompt, Dalle3Prompt, AtmosphereFeel, GeoLat, GeoLng, Slug)
VALUES (@calyxStationId,
    N'Calyx Station',
    N'Agricultural lowlands; one of the highest monster-predator incursion corridors in the Cauld; the land is contested for food production and supply routes rather than Sphere access',
    N'Scrying Installation',
    N'Open country; the installation is fortified against incursion rather than optimized for apparatus work; the walls are thicker than a Scrying station requires',
    0, 0,
    N'House Calyx''s Scrying installation. The installation itself is not the most powerful on the coalition map. The territory it sits on is the most contested for reasons that have nothing to do with membrane access: whoever controls Calyx''s land controls a significant portion of food supply for the current theater. Also sits on one of the highest monster-predator incursion corridors in the Cauld.',
    N'House Calyx apparatus operators; agricultural administrators; supply route officers; Myrmidon garrison for monster-predator incursion response; Oathless in the surrounding territory at the highest Monster Meat exposure rate of any House region',
    N'Food supply for the coalition theater; supply route control; Scrying is secondary to the agricultural and logistical value of the territory; the installation exists because the territory does, not the reverse',
    N'House Calyx holds the territory and the supply routes; the Seat holds institutional authority and knows the Monster Meat exposure numbers for former people; the knowledge does not change the hunting orders',
    N'', N'',
    N'The incursion sirens at Calyx Station have a specific register the garrison has learned to read. Both sirens — predator alert and humanoid monster alert — have the same pitch. Duration is the difference. The garrison knows which is which. When the humanoid monster alert sounds, no one looks at the faces.',
    0, 0, N'calyx_station');

-- The Thresh Chamber (Thresh)
INSERT INTO Entities (Id, EntityType, Name, Slug, Status, Description, CreatedAt, ModifiedAt, IsActive, UniverseId)
VALUES (@threshChamId, N'place', N'The Thresh Chamber', N'the_thresh_chamber', N'active',
    N'House Thresh''s Scrying installation. Modest primary apparatus. Their advantage is what they can deny others, not what they can Scry.',
    @now, @now, 1, @fantasy);

INSERT INTO Places (Id, Name, Territory, Tier, Climate, Rating, VoteCount, Description, Demographics, Economy, PowerStructure, MidjourneyPrompt, Dalle3Prompt, AtmosphereFeel, GeoLat, GeoLng, Slug)
VALUES (@threshChamId,
    N'The Thresh Chamber',
    N'House Thresh administrative territory; location deliberately underrepresented in official records',
    N'Scrying Installation',
    N'Stone; functional; the apparatus is maintained but the installation is not the operational center — the networks running through it are',
    0, 0,
    N'House Thresh''s Scrying installation. Modest primary apparatus by House standards. Their operational advantage is not what they can Scry but what they can deny others. The installation matters less than the network built around it — the courier lines, the tainted supply routes, the contact chains that reach into enemy installations.',
    N'House Thresh apparatus operators; courier network administrators; Catalyst supply chain managers; Lectors who have filed contamination reports that have not been acknowledged by the Silence',
    N'The sabotage economy; the apparatus maintains standard Scrying capability but the real output is the network; contaminated Catalyst supply chains run through this installation on their way to opposing Houses; the Catalyst batches here have uncertain provenance',
    N'House Thresh administers; the Lectors operating here have knowledge the Silence has not acted on; the provenance of Catalyst batches in this installation is no longer fully traceable even to the operators who handle them',
    N'', N'',
    N'The Thresh Chamber is the quietest installation in the Cauld. The apparatus runs. The paperwork is current. The contamination reports are filed and filed and filed and the Silence does not write back. The Lectors who have been here long enough to know what the reports contain have started keeping their own copies.',
    0, 0, N'the_thresh_chamber');

-- Pallor Station (Pallor)
INSERT INTO Entities (Id, EntityType, Name, Slug, Status, Description, CreatedAt, ModifiedAt, IsActive, UniverseId)
VALUES (@pallorStId, N'place', N'Pallor Station', N'pallor_station', N'active',
    N'House Pallor''s Scrying installation. Mid-tier apparatus. Strategically valuable for who is affiliated with it, not what it can reach through the membrane.',
    @now, @now, 1, @fantasy);

INSERT INTO Places (Id, Name, Territory, Tier, Climate, Rating, VoteCount, Description, Demographics, Economy, PowerStructure, MidjourneyPrompt, Dalle3Prompt, AtmosphereFeel, GeoLat, GeoLng, Slug)
VALUES (@pallorStId,
    N'Pallor Station',
    N'House Pallor territory; unaligned ground in the current coalition map; sits between the two coalition blocs without formally belonging to either',
    N'Scrying Installation',
    N'Stone; mid-tier maintenance; not the best-resourced installation; the long presence of its most notable affiliate has left a particular quality of stillness to the place',
    0, 0,
    N'House Pallor''s Scrying installation. Mid-tier apparatus — not the most powerful on the coalition map. The installation''s strategic value is not what it can reach through the membrane but who is affiliated with it. Both coalition blocs watch this installation. Neither has been able to determine what its most notable affiliate is waiting for.',
    N'House Pallor apparatus operators; administrative staff; Warrior King (longest-serving at map duty of any living commander); affiliated Champion (not sworn — affiliated; presence and absence unpredictable)',
    N'Mid-tier Sphere access; standard Scrying operations; the installation functions normally; the real strategic output is the living memory the Warrior King holds and the reasons the affiliated Champion has not explained',
    N'House Pallor holds the installation; the Warrior King holds the institutional and living memory of three coalition cycles; the affiliated Champion is not commanded by the House and does not explain their presence or absence; the House has not asked',
    N'', N'',
    N'The Champion is sometimes at the map table when the Warrior King arrives in the morning. Sometimes not. The Warrior King has learned not to ask where they go or when they will return. The Champion''s reasons predate the House''s existence. When the Champion is present the room is a different room.',
    0, 0, N'pallor_station');

PRINT N'[5/8] Inserted 7 Scrying Installations as Places';

-- ═════════════════════════════════════════════════════════════════════════════
-- 6. FactionGoals (2 per House)
-- ═════════════════════════════════════════════════════════════════════════════

-- Fornax
INSERT INTO FactionGoals (FactionId, Position, Goal) VALUES (@fornaxId, 1, N'Maintain exclusive control of Catalyst refining and distribution for all Houses');
INSERT INTO FactionGoals (FactionId, Position, Goal) VALUES (@fornaxId, 2, N'Preserve the supply relationship with the Liturgy — which depends on never pressing the question of acquisition mechanism');

-- Corvin
INSERT INTO FactionGoals (FactionId, Position, Goal) VALUES (@corvinId, 1, N'Retain control of the Sphere access points reachable only from Corvin Station''s coordinates');
INSERT INTO FactionGoals (FactionId, Position, Goal) VALUES (@corvinId, 2, N'Hold the vigil operators who are approaching functional ascendance without triggering the institutional crisis their capabilities will eventually force');

-- Draught
INSERT INTO FactionGoals (FactionId, Position, Goal) VALUES (@draughtId, 1, N'Maintain the largest standing Myrmidon force in the current coalition map');
INSERT INTO FactionGoals (FactionId, Position, Goal) VALUES (@draughtId, 2, N'Keep the piercing intake mechanism classified while using it to supplement volunteer enrollment shortfalls');

-- Atrament
INSERT INTO FactionGoals (FactionId, Position, Goal) VALUES (@atramentId, 1, N'Expand the Sphere catalogue — accumulate knowledge of more access points and designs than any other institution in the Cauld');
INSERT INTO FactionGoals (FactionId, Position, Goal) VALUES (@atramentId, 2, N'Navigate the ideological fault line within the House without either suppressing it or committing to it in a way that invites coalition retaliation');

-- Calyx
INSERT INTO FactionGoals (FactionId, Position, Goal) VALUES (@calyxId, 1, N'Hold the agricultural territory and supply routes that feed the coalition theater');
INSERT INTO FactionGoals (FactionId, Position, Goal) VALUES (@calyxId, 2, N'Maintain incursion suppression along the monster-predator corridor without formally acknowledging what the hunted humanoid monsters used to be');

-- Thresh
INSERT INTO FactionGoals (FactionId, Position, Goal) VALUES (@threshId, 1, N'Degrade opposing Houses'' Transmutation capability by contaminating Catalyst supplies and eliminating Transmuters');
INSERT INTO FactionGoals (FactionId, Position, Goal) VALUES (@threshId, 2, N'Restore Catalyst supply chain provenance before the contamination reaches their own infusion cycles');

-- Pallor
INSERT INTO FactionGoals (FactionId, Position, Goal) VALUES (@pallorId, 1, N'Maintain unaligned status — receive overtures from both coalition blocs without committing to either');
INSERT INTO FactionGoals (FactionId, Position, Goal) VALUES (@pallorId, 2, N'Keep the Warrior King''s living memory and the affiliated Champion''s reasons for remaining private until the moment those assets become worth using');

PRINT N'[6/8] Inserted FactionGoals';

-- ═════════════════════════════════════════════════════════════════════════════
-- 7. FactionMethods (2 per House)
-- ═════════════════════════════════════════════════════════════════════════════

-- Fornax
INSERT INTO FactionMethods (FactionId, Position, Method) VALUES (@fornaxId, 1, N'Catalyst refining: receives raw Gifted matter from the Liturgy supply chain and processes it into ceremony-grade Catalyst for distribution to consuming Houses');
INSERT INTO FactionMethods (FactionId, Position, Method) VALUES (@fornaxId, 2, N'Distribution leverage: controls which Houses receive Catalyst and at what priority — withholding or delaying distribution is the primary institutional pressure mechanism');

-- Corvin
INSERT INTO FactionMethods (FactionId, Position, Method) VALUES (@corvinId, 1, N'Vigil operations: long-tenure operators stationed at thin-membrane sites for decades accumulate perceptual capabilities that no appointment or transfer can replicate or move');
INSERT INTO FactionMethods (FactionId, Position, Method) VALUES (@corvinId, 2, N'Observation-tuned Transmutation: specific Catalyst infusions calibrated for perceptual range rather than combat capacity, administered to select vigil operators to enhance their thin-membrane sensitivity');

-- Draught
INSERT INTO FactionMethods (FactionId, Position, Method) VALUES (@draughtId, 1, N'Volunteer enrollment supplemented by membrane piercing: when intake is insufficient, the Muster Chamber is used to bring conscripts through from other Spheres; the service record does not distinguish');
INSERT INTO FactionMethods (FactionId, Position, Method) VALUES (@draughtId, 2, N'Oath administration before language acquisition: the oath is administered after arrival, before the conscript has enough Cauld language to understand exactly what they are swearing or enough context to understand what refusing would mean');

-- Atrament
INSERT INTO FactionMethods (FactionId, Position, Method) VALUES (@atramentId, 1, N'Sphere observation and transcription: the Deep Archive operates the broadest ongoing cataloguing effort of any House — more Spheres observed, more designs transcribed than any other institution in the Cauld');
INSERT INTO FactionMethods (FactionId, Position, Method) VALUES (@atramentId, 2, N'Knowledge leverage: grants calibrated access to the catalogue to other Houses as a diplomatic and economic tool; what is withheld is as important as what is shared; the terms of access are determined by the Keeper');

-- Calyx
INSERT INTO FactionMethods (FactionId, Position, Method) VALUES (@calyxId, 1, N'Supply control: manages food production and distribution for the coalition theater; the primary leverage is the threat — or reality — of supply line disruption');
INSERT INTO FactionMethods (FactionId, Position, Method) VALUES (@calyxId, 2, N'Incursion suppression: maintains Myrmidon garrison for monster-predator corridor defense; the hunting orders for humanoid monsters are administered without investigation of origin and without close examination of faces');

-- Thresh
INSERT INTO FactionMethods (FactionId, Position, Method) VALUES (@threshId, 1, N'Catalyst contamination: tainted Catalyst introduced into enemy supply chains produces negative Transmutation in opposing Houses'' soldiers weeks after administration — body horror and grief delivered as a deliberate weapon of war');
INSERT INTO FactionMethods (FactionId, Position, Method) VALUES (@threshId, 2, N'Transmuter elimination: targeted removal of senior Transmutation practitioners from opposing Houses, leaving those Houses unable to run controlled Gifted Ceremonies and at risk of producing monsters from their own loyal soldiers');

-- Pallor
INSERT INTO FactionMethods (FactionId, Position, Method) VALUES (@pallorId, 1, N'Strategic patience: remains unaligned while receiving overtures from both coalition blocs; the value of the position depends entirely on it remaining uncommitted');
INSERT INTO FactionMethods (FactionId, Position, Method) VALUES (@pallorId, 2, N'Living memory as latent asset: the Warrior King holds direct recollection of three coalition cycles; the affiliated Champion holds longer; neither has been asked to use this information and neither has offered it');

PRINT N'[7/8] Inserted FactionMethods';

-- ═════════════════════════════════════════════════════════════════════════════
-- 8. FactionRelationships — coalition structure
-- ═════════════════════════════════════════════════════════════════════════════

-- Coalition I: Fornax ↔ Corvin (mutual coalition anchors)
INSERT INTO FactionRelationships (FactionId, TargetFactionId, Alias, RelationshipType, Description, Position)
VALUES (@fornaxId, @corvinId, N'House Corvin', N'coalition-anchor',
    N'Co-anchor of Coalition I; Fornax holds the Catalyst supply chain, Corvin holds the thin-membrane access points — the two institutional pillars of the current alignment',
    1);

INSERT INTO FactionRelationships (FactionId, TargetFactionId, Alias, RelationshipType, Description, Position)
VALUES (@corvinId, @fornaxId, N'House Fornax', N'coalition-anchor',
    N'Co-anchor of Coalition I; Corvin holds the thin-membrane access points, Fornax holds the Catalyst supply chain — the two institutional pillars of the current alignment',
    1);

-- Opposition bloc: Draught ↔ Atrament ↔ Thresh
INSERT INTO FactionRelationships (FactionId, TargetFactionId, Alias, RelationshipType, Description, Position)
VALUES (@draughtId, @atramentId, N'House Atrament', N'opposition-bloc', N'Members of the same opposition bloc; Draught provides military force, Atrament provides knowledge leverage', 1);

INSERT INTO FactionRelationships (FactionId, TargetFactionId, Alias, RelationshipType, Description, Position)
VALUES (@draughtId, @threshId, N'House Thresh', N'opposition-bloc', N'Members of the same opposition bloc; Draught provides military force, Thresh handles covert degradation operations', 2);

INSERT INTO FactionRelationships (FactionId, TargetFactionId, Alias, RelationshipType, Description, Position)
VALUES (@atramentId, @draughtId, N'House Draught', N'opposition-bloc', N'Members of the same opposition bloc; Atrament provides knowledge leverage, Draught provides military force', 1);

INSERT INTO FactionRelationships (FactionId, TargetFactionId, Alias, RelationshipType, Description, Position)
VALUES (@atramentId, @threshId, N'House Thresh', N'opposition-bloc', N'Members of the same opposition bloc; Atrament provides knowledge leverage, Thresh handles covert degradation operations', 2);

INSERT INTO FactionRelationships (FactionId, TargetFactionId, Alias, RelationshipType, Description, Position)
VALUES (@threshId, @draughtId, N'House Draught', N'opposition-bloc', N'Members of the same opposition bloc; Thresh handles covert degradation, Draught provides military force', 1);

INSERT INTO FactionRelationships (FactionId, TargetFactionId, Alias, RelationshipType, Description, Position)
VALUES (@threshId, @atramentId, N'House Atrament', N'opposition-bloc', N'Members of the same opposition bloc; Thresh handles covert degradation, Atrament provides knowledge leverage', 2);

-- Both coalition anchors courting Pallor
INSERT INTO FactionRelationships (FactionId, TargetFactionId, Alias, RelationshipType, Description, Position)
VALUES (@fornaxId, @pallorId, N'House Pallor', N'courting-unaligned',
    N'Coalition I is actively courting Pallor''s alignment; the affiliated Champion is the primary strategic interest; no direct approach has been made about what the Champion is waiting for',
    2);

INSERT INTO FactionRelationships (FactionId, TargetFactionId, Alias, RelationshipType, Description, Position)
VALUES (@draughtId, @pallorId, N'House Pallor', N'courting-unaligned',
    N'Opposition bloc is actively courting Pallor''s alignment; the Warrior King''s living memory and the affiliated Champion are the primary strategic interests',
    3);

-- Pallor receiving overtures from both sides
INSERT INTO FactionRelationships (FactionId, TargetFactionId, Alias, RelationshipType, Description, Position)
VALUES (@pallorId, @fornaxId, N'House Fornax', N'overture-received',
    N'Receives coalition overtures from Coalition I''s anchor; has not committed; the overture''s value to Pallor is that it demonstrates the unaligned position is worth holding',
    1);

INSERT INTO FactionRelationships (FactionId, TargetFactionId, Alias, RelationshipType, Description, Position)
VALUES (@pallorId, @draughtId, N'House Draught', N'overture-received',
    N'Receives opposition bloc overtures from the military anchor; has not committed; both overtures are received and neither is answered',
    2);

-- Thresh → Fornax (supply dependency / structural irony)
INSERT INTO FactionRelationships (FactionId, TargetFactionId, Alias, RelationshipType, Description, Position)
VALUES (@threshId, @fornaxId, N'House Fornax', N'supply-dependency',
    N'Receives Catalyst through Fornax distribution; their own supply chain provenance is compromised from the sabotage economy; the relationship is a dependency that Thresh''s own methods are threatening to undermine',
    3);

PRINT N'[8/8] Inserted FactionRelationships (coalition structure)';

PRINT N'';
PRINT N'══════════════════════════════════════════════════════════════';
PRINT N'The Cauld — Seven Houses seed complete.';
PRINT N'';
PRINT N'  UPDATED:  House Fornax    (3AED2F41) — new canon';
PRINT N'  UPDATED:  House Corvin    (019ED86A) — renamed from Corvus, new canon';
PRINT N'  RETIRED:  House Atrax, Cetus, Noctua, Vulcanus — IsActive=0';
PRINT N'  INSERTED: House Draught, Atrament, Calyx, Thresh, Pallor';
PRINT N'  INSERTED: 7 Scrying Installations as Places';
PRINT N'  INSERTED: FactionGoals (2 per House × 7 Houses = 14)';
PRINT N'  INSERTED: FactionMethods (2 per House × 7 Houses = 14)';
PRINT N'  INSERTED: FactionRelationships (coalition structure = 12)';
PRINT N'';
PRINT N'  NOTE: House Ophiuchus (Tier not set to House) not touched — check separately.';
PRINT N'══════════════════════════════════════════════════════════════';
