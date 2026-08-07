SET NOCOUNT ON;
DECLARE @SCRY UNIQUEIDENTIFIER = '0197E9C9-0002-7000-8000-000000000002';

UPDATE GlossaryTerms
SET Definition = 'A Shell (a Liturgy-built android construct) with a human mind Relayed into it and operating it -- the mind always Pierced from another Sphere, never Entos-born, never Transmutation-eligible. Death is permanent: if a Myrmidon''s Shell is destroyed while its mind is still inside, the mind is gone -- no shell-cycle, no return.',
    UpdatedAt = GETUTCDATE()
WHERE UniverseId = @SCRY AND Term = 'Myrmidon';

UPDATE GlossaryTerms
SET FullForm = NULL,
    Definition = 'An android construct built by the Liturgy, carrying an Anima Core. Uninhabited, it is just a Shell -- inert, no different from any other piece of equipment. The moment a mind Relays into it, it is a Myrmidon.',
    UpdatedAt = GETUTCDATE()
WHERE UniverseId = @SCRY AND Term = 'Shell';

UPDATE GlossaryTerms
SET Definition = 'The short, spoken designation given to an activated Myrmidon (e.g. "M-101"), distinct from its longer catalog/intake record number (e.g. "M-1018883"). A Myrmidon may insist on the short form as its name and reject the longer number as "paperwork."',
    UpdatedAt = GETUTCDATE()
WHERE UniverseId = @SCRY AND Term = 'M-101';

INSERT INTO GlossaryTerms (UniverseId, Term, FullForm, Definition, Category, SortKey) VALUES
(@SCRY, 'Anima Core', NULL,
 'The component that makes a Shell or machine inhabitable by a Relayed mind -- without one, a Shell is just metal. Installed in Myrmidon Shells, Templar Station terminals, airship turrets and navigation systems, and certain war-beast constructs.',
 NULL, 0),

(@SCRY, 'Relay', NULL,
 'A living Myrmidon moving its mind from its current Anima-Core-bearing Shell or machine into another one -- another Myrmidon Shell, a Templar Station terminal, an airship turret, a war-beast left standing on a battlefield. The vacated Shell goes inert, not dead. Never a way to survive death: if the Shell a mind occupies is destroyed before it Relays out, that mind is gone, same as any other death.',
 NULL, 0),

(@SCRY, 'Anima Lantern', NULL,
 'A portable containment vessel -- not a Shell, grants no body or motion -- that holds a captured mind the way a Shell holds an operating one. Used as a prison, a storage device, or a means of transport for a mind with nowhere else to go.',
 NULL, 0);

SELECT Term, Definition FROM GlossaryTerms WHERE UniverseId = @SCRY AND Term IN ('Myrmidon','Shell','M-101','Anima Core','Relay','Anima Lantern');
