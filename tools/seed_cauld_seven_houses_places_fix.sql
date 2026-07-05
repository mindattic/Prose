-- Patch: insert 5 missing Places rows (Climate was over 120 chars in original seed)
-- 2026-07-04

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

INSERT INTO Places (Id, Name, Territory, Tier, Climate, Rating, VoteCount, Description, Demographics, Economy, PowerStructure, MidjourneyPrompt, Dalle3Prompt, AtmosphereFeel, GeoLat, GeoLng, Slug)
VALUES ('246906E3-D617-4E88-B734-6569AE90B6A7',
    N'Corvin Station',
    N'Northern reaches; elevated ridge country; highest contested coordinates for location-specific Spheres yielding Gifted matter',
    N'Scrying Installation',
    N'High elevation; ridge wind audible through stone; cold most of the year',
    0, 0,
    N'Oldest installation in the Cauld. Named in historical record more than any other. The membrane here has been stretched at these coordinates longer than any other site. Long-tenure vigil operators describe the membrane as something that breathes. They say this to each other on night watches. They stop saying it when House administration is present.',
    N'House Corvin vigil operators — some stationed forty years or more; apparatus technicians; Liturgy Lectors during Relic assessments; administrative staff on shorter rotation',
    N'Location-specific Sphere access; Gifted matter yield from Spheres reachable only from this ridge; the vigil operators are the institutional knowledge',
    N'House Corvin holds the ridge and therefore the access point; the most senior vigil operators know the membrane better than any administrator',
    N'', N'',
    N'Night watch. The apparatus is off. The vigil operator has been at this post for thirty-seven years. They are not watching the equipment. They are watching something the equipment cannot see.',
    0, 0, N'corvin_station');

INSERT INTO Places (Id, Name, Territory, Tier, Climate, Rating, VoteCount, Description, Demographics, Economy, PowerStructure, MidjourneyPrompt, Dalle3Prompt, AtmosphereFeel, GeoLat, GeoLng, Slug)
VALUES ('06BD1CB8-50A6-42D9-9975-7E97FE6C2BFD',
    N'The Deep Archive',
    N'House Atrament territory; the catalogue is the strategic asset — what is inside it is what makes the location valuable',
    N'Scrying Installation',
    N'Stone; shelving and catalogue storage throughout; scribes share space with operators',
    0, 0,
    N'House Atrament''s Scrying installation with the broadest Sphere catalogue of any House. More documented access points than any installation except Corvin Station. Where Corvin has depth at specific coordinates, Atrament has breadth: more Spheres observed, more designs transcribed, more knowledge of what exists across the membrane than any other institution in the Cauld.',
    N'House Atrament apparatus operators; Sphere catalogue scribes; military design researchers from visiting Houses; faction members who believe this knowledge should not be privately controlled',
    N'Sphere knowledge catalogued and available for manufacture; calibrated access granted to other Houses as diplomatic and economic leverage',
    N'House Atrament holds the installation; the Keeper holds both the technical authority and the unresolved ideological question the other Houses are watching',
    N'', N'',
    N'The scribes work in shifts. The catalogue extends into rooms whose contents were assessed before the current Keeper was born. Someone who has read enough begins to understand the scale of what was decided not to share.',
    0, 0, N'the_deep_archive');

INSERT INTO Places (Id, Name, Territory, Tier, Climate, Rating, VoteCount, Description, Demographics, Economy, PowerStructure, MidjourneyPrompt, Dalle3Prompt, AtmosphereFeel, GeoLat, GeoLng, Slug)
VALUES ('9C3B9B38-471A-4D72-8A05-923629A0E91B',
    N'Calyx Station',
    N'Agricultural lowlands; one of the highest monster-predator incursion corridors in the Cauld',
    N'Scrying Installation',
    N'Open country; fortified against incursion rather than optimized for apparatus work',
    0, 0,
    N'House Calyx''s Scrying installation. The territory it sits on is the most contested for reasons unrelated to membrane access: whoever controls Calyx''s land controls a significant portion of food supply for the current theater. Also sits on one of the highest monster-predator incursion corridors in the Cauld.',
    N'House Calyx apparatus operators; agricultural administrators; supply route officers; Myrmidon garrison for incursion response; Oathless in surrounding territory at the highest Monster Meat exposure rate of any House region',
    N'Food supply for the coalition theater; supply route control; Scrying is secondary to agricultural and logistical value of the territory',
    N'House Calyx holds the territory and supply routes; the Seat knows the Monster Meat exposure numbers; the knowledge does not change the hunting orders',
    N'', N'',
    N'The incursion sirens have a register the garrison has learned to read. Both alerts — predator and humanoid monster — have the same pitch. Duration is the difference. When the humanoid alert sounds, no one looks at the faces.',
    0, 0, N'calyx_station');

INSERT INTO Places (Id, Name, Territory, Tier, Climate, Rating, VoteCount, Description, Demographics, Economy, PowerStructure, MidjourneyPrompt, Dalle3Prompt, AtmosphereFeel, GeoLat, GeoLng, Slug)
VALUES ('345E7015-FB49-4A1C-BA00-D577DBF6BEDA',
    N'The Thresh Chamber',
    N'House Thresh administrative territory; location underrepresented in official records',
    N'Scrying Installation',
    N'Stone; functional; the apparatus runs but the networks are the real operation',
    0, 0,
    N'House Thresh''s Scrying installation. Modest primary apparatus by House standards. Their operational advantage is not what they can Scry but what they can deny others. The installation matters less than the network built around it.',
    N'House Thresh apparatus operators; courier network administrators; Catalyst supply chain managers; Lectors who have filed contamination reports the Silence has not acknowledged',
    N'The sabotage economy; contaminated Catalyst batches route through here on their way to opposing Houses; provenance of local batches no longer fully traceable',
    N'House Thresh administers; Lectors have knowledge the Silence has not acted on; contamination provenance is no longer fully traceable even to the operators',
    N'', N'',
    N'The Thresh Chamber is the quietest installation in the Cauld. The apparatus runs. The contamination reports are filed and the Silence does not write back. The Lectors who know what the reports contain have started keeping their own copies.',
    0, 0, N'the_thresh_chamber');

INSERT INTO Places (Id, Name, Territory, Tier, Climate, Rating, VoteCount, Description, Demographics, Economy, PowerStructure, MidjourneyPrompt, Dalle3Prompt, AtmosphereFeel, GeoLat, GeoLng, Slug)
VALUES ('020B5A1F-1966-41AB-8316-6AA667BC1783',
    N'Pallor Station',
    N'House Pallor territory; unaligned ground sitting between the two coalition blocs',
    N'Scrying Installation',
    N'Stone; mid-tier maintenance; a particular stillness from long habitation',
    0, 0,
    N'House Pallor''s Scrying installation. Mid-tier apparatus. Strategically valuable not for what it can reach through the membrane but for who is affiliated with it. Both coalition blocs watch this installation. Neither has determined what the affiliated Champion is waiting for.',
    N'House Pallor apparatus operators; administrative staff; Warrior King (longest-serving at map duty of any living commander); affiliated Champion (not sworn — affiliated; presence and absence unpredictable)',
    N'Mid-tier Sphere access; standard Scrying operations; the real strategic output is the living memory the Warrior King and affiliated Champion hold and have not offered',
    N'House Pallor administers; the Warrior King holds living memory of three coalition cycles; the affiliated Champion is not commanded and does not explain their presence',
    N'', N'',
    N'The Champion is sometimes at the map table when the Warrior King arrives. Sometimes not. The Warrior King has learned not to ask. The Champion''s reasons predate the House''s existence. When the Champion is present the room is a different room.',
    0, 0, N'pallor_station');

PRINT N'Inserted 5 missing Places rows (Corvin Station, Deep Archive, Calyx Station, Thresh Chamber, Pallor Station)';
