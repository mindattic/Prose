SET NOCOUNT ON;
DECLARE @SCRY UNIQUEIDENTIFIER = '0197E9C9-0002-7000-8000-000000000002';
UPDATE GlossaryTerms
SET Definition = 'A living Myrmidon moving its mind from its current Anima-Core-bearing Shell or machine into another one -- another Myrmidon Shell, a Templar Station terminal, an airship turret, a war-beast left standing on a battlefield. Requires a clear line of sight AND close range -- roughly a quarter mile, the distance at which the destination''s detail is visible, not just its silhouette; past that, even with sightline, the Anima Core signature is too faint to lock onto. The vacated Shell goes inert, not dead. Never a way to survive death: if the Shell a mind occupies is destroyed before it Relays out, that mind is gone, same as any other death.',
    UpdatedAt = GETUTCDATE()
WHERE UniverseId = @SCRY AND Term = 'Relay';
SELECT Term, Definition FROM GlossaryTerms WHERE UniverseId = @SCRY AND Term = 'Relay';
