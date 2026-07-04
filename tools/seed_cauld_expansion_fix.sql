SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- Fix 1: House Corvus (failed to seed in main run due to batch variable issue)
IF NOT EXISTS (SELECT 1 FROM Factions WHERE Name = N'House Corvus')
BEGIN
    DECLARE @corvusId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES (@corvusId, N'faction', N'House Corvus', N'house-corvus', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    INSERT INTO Factions (Id, Name, Slug, Sector, Tier, Allegiance, Motto, Description, Ideology, Territory, Leadership, NarrativeFunction, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @corvusId, N'House Corvus', N'house-corvus',
        N'military / intelligence',
        N'House',
        N'current coalition with House Noctua (western front)',
        N'What the eye sees, the hand commands.',
        N'The intelligence House. House Corvus built its power on a single premise: you cannot outfight information. Their Scrying installation in the northern reaches — the Corvin Station — is the oldest continuously operating station in the Cauld, cataloguing signals from Spheres other Houses have not yet identified. They Scried radio communication before any other House and have run encrypted signal operations for forty years. Their soldiers often carry equipment the enemy cannot identify on sight. The coalition table without House Corvus is operating partially blind; they have turned this into the cornerstone of their entire strategic position.',
        N'Information is the only weapon that does not run out of ammunition. A war fought with superior intelligence ends sooner than one fought with superior numbers. House Corvus controls what signals enter the Cauld from the Sphere catalogue and therefore controls what technologies their coalition can anticipate and what the enemy coalition believes it understands. They do not seek the most dramatic victories. They seek the most consistent advantage.',
        N'Northern reaches; elevated ridge country where atmospheric crystal access is optimal for Scrying; the Corvin Station and surrounding garrison settlements',
        N'Warrior King Vael Skaros (Paladin-rank; thirty years of campaign command). The Keeper position is currently contested following the death of the previous Keeper at Bheur''s Crossing three months ago.',
        N'The information broker House. Story tension through intelligence gaps: what Corvus knows and hasn''t shared, and why. The House that could change coalition dynamics at any moment — which means they''re waiting for the right moment, or the right price.',
        N'medieval intelligence installation on a northern highland ridge, signal towers in low cloud, atmospheric crystal formations, House Corvus raven heraldry, WW1-era communications aesthetic in fantasy architecture, Buehlman dark fantasy, dawn light and highland fog',
        N'signal tower complex on northern ridge, raven heraldry, WW1 signals aesthetic in fantasy, low cloud and dawn',
        0, 0
    );
    PRINT 'House Corvus seeded.';
END
ELSE PRINT 'House Corvus already exists.';
GO

-- Fix 2: The Forge Hearth Places record (Entities record exists; Climate was too long)
IF NOT EXISTS (SELECT 1 FROM Places WHERE Name = N'The Forge Hearth')
BEGIN
    DECLARE @forgeId UNIQUEIDENTIFIER = (SELECT Id FROM Entities WHERE Name = N'The Forge Hearth' AND EntityType = N'place');
    INSERT INTO Places (Id, Name, Slug, Territory, Tier, Climate, Description, Demographics, Economy, PowerStructure, AtmosphereFeel, GeoLat, GeoLng, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @forgeId, N'The Forge Hearth', N'the-forge-hearth',
        N'Eastern industrial district; House Vulcanus territory',
        N'Industrial District',
        N'Moderate; permanently obscured by forge-smoke; ambient temperature near the forges significantly elevated',
        N'House Vulcanus''s Scrying installation is the center of a working industrial district, not a separate facility. The Forge Hearth began as a standard station and expanded over two generations into the manufacturing complex surrounding it. The installation remains the technical heart, but what most visitors see is the forge works: foundries, production lines, workshops where Scried weapons move from design to manufacturable form. The forge-smoke is constant. The noise of production never stops. Inside the district, output can be measured in what arrives at every front in every coalition: Pattern-3 rifles, Hailmakers, Chromite Black components, a third of everything else in active military use.',
        N'Two thousand-plus production workers and engineers; a smaller Myrmidon contingent than most House installations — Vulcanus''s power is production, not combat strength; Keeper and technical staff; House Atrax logistics contractors',
        N'The Forge Hearth produces. It does not sell for coin; it trades output for coalition commitments, supply priority agreements, and Catalyst access from Fornax. Keeper Maret Delys controls the production schedule and therefore the trading leverage.',
        N'Keeper Maret Delys is the institutional authority. A rotating committee of senior Paladins holds the Warrior King title and does not challenge the Keeper''s decisions — unique among the Seven Houses.',
        N'The Forge Hearth smells of metal and burning coal and machine oil. Not comfortable. The forge-glow is visible at night for twelve kilometers. The eastern sky has not been clear in forty years.',
        0.0, 0.0,
        N'medieval industrial fortress district, coal-smoke haze, foundries through stone arches, Scrying station tower at center, WW1-era weapons manufacturing aesthetic, forge-glow and grey industrial sky, Buehlman dark fantasy',
        N'industrial medieval district, forge works, coal smoke, stone chimneys, WW1 manufacturing aesthetic in fantasy',
        0, 0
    );
    PRINT 'The Forge Hearth Places record inserted.';
END
ELSE PRINT 'The Forge Hearth already has a Places record.';
GO

-- Fix 3: Bheur's Crossing Places record
IF NOT EXISTS (SELECT 1 FROM Places WHERE Name = N'Bheur''s Crossing')
BEGIN
    DECLARE @bheurId UNIQUEIDENTIFIER = (SELECT Id FROM Entities WHERE Name = N'Bheur''s Crossing' AND EntityType = N'place');
    INSERT INTO Places (Id, Name, Slug, Territory, Tier, Climate, Description, Demographics, Economy, PowerStructure, AtmosphereFeel, GeoLat, GeoLng, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @bheurId, N'Bheur''s Crossing', N'bheurs-crossing',
        N'Middle-front; Caul River crossing; nominally Noctua-Corvus coalition',
        N'Contested Site',
        N'Wet; river microclimate; morning mist persistent and reliable — useful for cover, costly for visibility',
        N'The only passable bridge across the Caul River at the midpoint of the western front. Whoever holds Bheur''s Crossing controls the movement of anything larger than a supply pack from one side of the front to the other. Four different coalitions have held this crossing in the last century; it changes hands through formal assault, assassination, and treaty in proportions that vary by decade. The current Noctua-Corvus coalition has held for seven years — primarily because Noctua''s night operations make a direct assault unacceptably costly and Corvus''s signal advantage makes a flanking operation difficult to conceal. The bridge has been repaired eleven times by documented record; reconstruction materials become more elaborate with each iteration. The stones are worn smooth by generations of boot traffic.',
        N'Garrison rotates; currently Noctua-dominant with Corvus signal personnel embedded; civilian use nominally permitted and practically rare',
        N'Bheur''s Crossing is economic leverage disguised as military position. Control translates directly to tariffs on coalition supply movement and bargaining power at the coalition table.',
        N'Noctua operational command (Dame Thessaly Brennan in the field); Corvus signal advisory (embedded, not commanding); joint casualty authority never formally tested',
        N'Morning mist. The specific sound of a river crossing that both sides know and neither has fully secured. Worn stones. Industrial additions that do not match the medieval base.',
        0.0, 0.0,
        N'contested ancient bridge over a mist-covered river, medieval stone with industrial repair additions, WW1-era garrison, Noctua and Corvus military presence, Buehlman dark fantasy, morning mist and grey light',
        N'ancient contested bridge, mist-covered river, medieval stone with industrial additions, WW1 military aesthetic in fantasy',
        0, 0
    );
    PRINT 'Bheur''s Crossing Places record inserted.';
END
ELSE PRINT 'Bheur''s Crossing already has a Places record.';
GO

-- Fix 4: The Amber Wards Places record
IF NOT EXISTS (SELECT 1 FROM Places WHERE Name = N'The Amber Wards')
BEGIN
    DECLARE @amberId UNIQUEIDENTIFIER = (SELECT Id FROM Entities WHERE Name = N'The Amber Wards' AND EntityType = N'place');
    INSERT INTO Places (Id, Name, Slug, Territory, Tier, Climate, Description, Demographics, Economy, PowerStructure, AtmosphereFeel, GeoLat, GeoLng, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @amberId, N'The Amber Wards', N'the-amber-wards',
        N'Interior; buffer zone protected by collective interest',
        N'Restricted Facility',
        N'Interior; dry compared to the fronts; ventilation stacks produce amber atmospheric discoloration visible at 20km',
        N'House Fornax''s Catalyst production complex. Located in the interior at a distance from every active front that all parties have honored without a formal treaty, because no treaty was necessary — destroying the Amber Wards would be mutually assured logistical catastrophe. The complex is not large by manufacturing standards; Catalyst refinement requires precision rather than volume. What is large is the security apparatus and the buffer zone. The amber atmospheric staining is visible at twenty kilometers in clear conditions. Inside the perimeter, access is controlled by authorization layers that even senior House Fornax personnel navigate with documentation.',
        N'Technical refinement staff (small, specialized, House Fornax-only); administrative personnel; security apparatus primarily deterrent rather than combat-calibrated; the Seat''s administrative element',
        N'The Amber Wards is not where Catalyst is sold. It is where Catalyst is made. Sales occur through intermediaries and manifests. Nobody pays the Amber Wards directly; they pay House Fornax.',
        N'The Seat (Emric Haed) is the administrative and production authority. No Warrior King. The technical refinement director reports to the Seat, not the council.',
        N'Calm; deliberate; the amber staining in the air noticeable inside the perimeter; smells of something chemical that is not unpleasant but not natural; institutional precision to everything.',
        0.0, 0.0,
        N'interior alchemical production complex, amber-tinted atmospheric staining, buffer zone perimeter, institutional medieval-industrial architecture, Fornax furnace heraldry, Buehlman dark fantasy, amber light quality',
        N'alchemical production facility, amber atmospheric tinting, buffer zone, furnace heraldry, medieval-industrial',
        0, 0
    );
    PRINT 'The Amber Wards Places record inserted.';
END
ELSE PRINT 'The Amber Wards already has a Places record.';
GO

-- Fix 5: The Thin Places Places record
IF NOT EXISTS (SELECT 1 FROM Places WHERE Name = N'The Thin Places')
BEGIN
    DECLARE @thinId UNIQUEIDENTIFIER = (SELECT Id FROM Entities WHERE Name = N'The Thin Places' AND EntityType = N'place');
    INSERT INTO Places (Id, Name, Slug, Territory, Tier, Climate, Description, Demographics, Economy, PowerStructure, AtmosphereFeel, GeoLat, GeoLng, MidjourneyPrompt, Dalle3Prompt, Rating, VoteCount)
    VALUES (
        @thinId, N'The Thin Places', N'the-thin-places',
        N'Distributed; specific geographic sites; not a contiguous territory',
        N'Phenomenon Location',
        N'Varies by site; all share atmospheric crystal concentration above regional average',
        N'Collective term for geographic sites where the membrane between Spheres is demonstrably thinner — where a natural perception of cross-Sphere signals is theoretically possible without apparatus. The Mossland has a confirmed site. Ships Rock has one. The Corvin Station was built near one. There are believed to be several others. The Houses fight over confirmed Thin Places because the ability to Scry without an installation means the knowledge cannot be controlled. An unauthorized Scrying from a Thin Place cannot be intercepted, logged, or taxed. Everything the Houses'' power apparatus depends on rests on controlling who can Scry. A Thin Place someone learns to use independently is a strategic nightmare.',
        N'Thin Places are not inhabited; found, marked, and either incorporated into installations or actively suppressed',
        N'The economic value of a Thin Place is the independence it offers from House Scrying control — negative value from the Houses'' perspective, high positive value from everyone else''s',
        N'Contested; any confirmed Thin Place triggers a House response within weeks of confirmation',
        N'Varies by site: the Mossland site is wet and cold; Ships Rock is exposed and windy. What all accounts share is cognitive — something perceived from somewhere the observer cannot see, perceived correctly.',
        0.0, 0.0,
        N'mystical geographic sites with atmospheric crystal formations, membrane-between-worlds effect, medieval-fantasy landscape, liminal light quality, Buehlman dark fantasy',
        N'liminal geographic sites, crystal formations, liminal light, dark fantasy',
        0, 0
    );
    PRINT 'The Thin Places Places record inserted.';
END
ELSE PRINT 'The Thin Places already has a Places record.';
GO

PRINT 'Fix script complete.';
