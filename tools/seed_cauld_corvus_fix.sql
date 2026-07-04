SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF NOT EXISTS (SELECT 1 FROM Entities WHERE Id = '019ED86A-2874-765E-99BA-BC83E4F97026')
BEGIN
    INSERT INTO Entities (Id, EntityType, Name, Slug, Status, IsActive, UniverseId, CreatedAt, ModifiedAt)
    VALUES ('019ED86A-2874-765E-99BA-BC83E4F97026', N'faction', N'House Corvus', N'house-corvus', N'canon', 1,
            '0197E9C9-0002-7000-8000-000000000002', GETDATE(), GETDATE());
    PRINT 'House Corvus Entities record created.';
END
ELSE PRINT 'Entities record already exists.';

UPDATE Factions SET
    Slug = N'house-corvus',
    Sector = N'military / intelligence',
    Tier = N'House',
    Allegiance = N'current coalition with House Noctua (western front)',
    Motto = N'What the eye sees, the hand commands.',
    Description = N'The intelligence House. House Corvus built its power on a single premise: you cannot outfight information. Their Scrying installation — the Corvin Station — is the oldest continuously operating station in the Cauld, cataloguing signals from Spheres other Houses have not yet identified. They Scried radio communication before any other House and have run encrypted signal operations for forty years. Their soldiers often carry equipment the enemy cannot identify on sight. The coalition table without House Corvus is operating partially blind; they have turned this into the cornerstone of their entire strategic position.',
    Ideology = N'Information is the only weapon that does not run out of ammunition. A war fought with superior intelligence ends sooner than one fought with superior numbers. House Corvus controls what signals enter the Cauld from the Sphere catalogue and therefore controls what technologies their coalition can anticipate. They do not seek the most dramatic victories. They seek the most consistent advantage.',
    Territory = N'Northern reaches; elevated ridge country where atmospheric crystal access is optimal for Scrying; the Corvin Station and surrounding garrison settlements',
    Leadership = N'Warrior King Vael Skaros (Paladin-rank; thirty years of campaign command). The Keeper position is currently contested following the death of the previous Keeper at Bheur''s Crossing.',
    NarrativeFunction = N'The information broker House. Story tension through intelligence gaps: what Corvus knows and has not shared, and why. The House that could change coalition dynamics at any moment — which means they are waiting for the right moment, or the right price.',
    MidjourneyPrompt = N'medieval intelligence installation, signal towers on northern highland ridge, atmospheric crystal formations, House Corvus raven heraldry, WW1-era communications aesthetic in fantasy architecture, Buehlman dark fantasy, dawn light and highland fog',
    Dalle3Prompt = N'signal tower complex on northern ridge, raven heraldry, WW1 signals aesthetic in fantasy, low cloud and dawn'
WHERE Name = N'House Corvus';

PRINT 'House Corvus fully resolved.';
GO
