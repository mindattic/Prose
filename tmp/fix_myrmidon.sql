SET NOCOUNT ON;
DECLARE @SCRY UNIQUEIDENTIFIER = '0197E9C9-0002-7000-8000-000000000002';
UPDATE GlossaryTerms
SET Definition = 'A person taken from another Sphere by Piercing and conscripted into House military service -- the Liturgy''s own term, not the conscript''s word (soldiers call it "slave-soldier"). Not on the infusion rank ladder; native Entos soldiers are never Myrmidons. Death is permanent -- consciousness does not transfer between bodies, and a Myrmidon who dies is gone.',
    UpdatedAt = GETUTCDATE()
WHERE UniverseId = @SCRY AND Term = 'Myrmidon';
SELECT Term, Definition FROM GlossaryTerms WHERE UniverseId = @SCRY AND Term = 'Myrmidon';
