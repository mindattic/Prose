SET NOCOUNT ON;

DECLARE @GLMZ UNIQUEIDENTIFIER = '0197E9C9-0001-7000-8000-000000000001';
DECLARE @SCRY UNIQUEIDENTIFIER = '0197E9C9-0002-7000-8000-000000000002';

-- ==================== GLMZ round 3 ====================

INSERT INTO GlossaryTerms (UniverseId, Term, FullForm, Definition, Category, SortKey) VALUES
(@GLMZ, 'Black Ice', NULL,
 'A category of neuretics-crime NCID investigates: defensive countermeasures embedded in a data system, deployed against an intruding Channeler, Ghost, or Splicer. Exact mechanics aren''t spelled out on the page beyond the term itself -- but deploying it is a crime, not a legitimate defense.',
 NULL, 0),

(@GLMZ, 'Operator', NULL,
 'General GLMZ term for a gray-zone specialist working outside CorpoNation-sanctioned channels. Three recognized classes exist: Channeler, Ghost, and Splicer.',
 NULL, 0);

-- ==================== SCRY round 3 ====================

INSERT INTO GlossaryTerms (UniverseId, Term, FullForm, Definition, Category, SortKey) VALUES
(@SCRY, 'Templar Station', NULL,
 'A sealed, self-contained structure at Vigil/Templar checkpoints across Entos -- locals describe them as impenetrable geometric eggs. Opens only for an Infused Templar. Holds supplies and a permanent relay terminal carrying Logs between Stations, rendered in the Received Tongue.',
 NULL, 0),

(@SCRY, 'the Cavity', 'the Sinter Cavity',
 'The crater-like descent site within the Sinter quarantine region, where cluster-stage organisms complete and Sinterkin detach. "Sinter" names the wider quarantined region; "the Cavity" is the pit itself.',
 NULL, 0),

(@SCRY, 'the Vigil', NULL,
 'The order that holds the quarantine line at the Pass, watching the Sinter border so whatever comes out of it doesn''t reach the rest of Entos.',
 NULL, 0);

SELECT UniverseId, COUNT(*) AS Terms FROM GlossaryTerms GROUP BY UniverseId;
