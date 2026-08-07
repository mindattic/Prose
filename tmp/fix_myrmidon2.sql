SET NOCOUNT ON;
DECLARE @SCRY UNIQUEIDENTIFIER = '0197E9C9-0002-7000-8000-000000000002';
UPDATE GlossaryTerms
SET Definition = 'An android construct inhabited by a human mind -- built to fight in House service. Death is permanent: once a Myrmidon dies, the mind inside it is gone, no shell-cycle, no return.',
    UpdatedAt = GETUTCDATE()
WHERE UniverseId = @SCRY AND Term = 'Myrmidon';
SELECT Term, Definition FROM GlossaryTerms WHERE UniverseId = @SCRY AND Term = 'Myrmidon';
