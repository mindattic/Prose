SET NOCOUNT ON;

DECLARE @GLMZ UNIQUEIDENTIFIER = '0197E9C9-0001-7000-8000-000000000001';
DECLARE @SCRY UNIQUEIDENTIFIER = '0197E9C9-0002-7000-8000-000000000002';

-- Fix: "the Expectant" carried the same Bolide=cancer plot secret already stripped from
-- Sinterkin. A back-of-book glossary is reader-facing; it must not spoil an unrevealed
-- mystery, even one sourced from the author's own private world-bible.
UPDATE GlossaryTerms
SET Definition = 'The vast organism whose body the Amnios and every Sphere inside it belong to -- worshipped by the Liturgy as the source of all Gifted matter, and the entity its doctrine holds gifted humanity the Catalyst in the first place.',
    UpdatedAt = GETUTCDATE()
WHERE UniverseId = @SCRY AND Term = N'the Expectant';

-- ==================== GLMZ: nested terms surfaced inside existing definitions ====================

INSERT INTO GlossaryTerms (UniverseId, Term, FullForm, Definition, Category, SortKey) VALUES
(@GLMZ, 'PMC', 'Private Military Contractor',
 'A for-hire military/security company. ArcSec''s contracting model draws on PMC-style hired forces alongside its own standing corporate military.',
 NULL, 0),

(@GLMZ, 'AR', 'Augmented Reality',
 'Displayed content layered over physical sight -- distinct from full Hyper Reality, which has no physical counterpart at all. AR overlays annotate or decorate what''s actually there; Hyper Reality substitutes for it entirely.',
 NULL, 0),

(@GLMZ, 'Eigenstate', NULL,
 'Borrowed from the physics term for the fixed, definite state a quantum system settles into once observed. In GLMZ tech-speak, the compressed, stable form a mind takes during an eigenstate conscience transfer (ECT) -- see Ghost.',
 NULL, 0),

(@GLMZ, 'Seam', NULL,
 'The narrow, contested corridor where two CorpoNations'' jurisdictions meet and neither fully governs -- named for the literal seam between two claimed territories. Worked, patrolled, and claimed by whoever keeps showing up.',
 NULL, 0),

(@GLMZ, 'Halcyon', 'Halcyon Civil Security',
 'The CorpoNation holding GLMZ''s neuretics-crime enforcement monopoly. Administers augmentation certification and maintenance across the corridor -- which is what gives its investigative arm, NCID, forensic access no other body can match.',
 NULL, 0),

(@GLMZ, 'Arcturus', 'Arcturus Defense Solutions',
 'The CorpoNation holding GLMZ''s physical-security monopoly, operating under the name ArcSec: sovereign perimeter defense, PMC contracting, corporate military, autonomous weapons.',
 NULL, 0),

(@GLMZ, 'the Veil', NULL,
 'The boundary between archology-tier corporate city and the gray zone beneath it. Above it: brand holograms, AR traffic control, the full Hyper Reality commercial layer. Below it: none of that infrastructure exists, and augmented characters notice the absence before anything else.',
 NULL, 0),

(@GLMZ, 'Archology', NULL,
 'GLMZ''s term for its vertical, corporate-controlled city structures -- densely engineered, AR-optimized, and built to be experienced through neuretic augmentation rather than looked at directly.',
 NULL, 0),

(@GLMZ, 'Substrate', NULL,
 'The physical/computational medium neuretics and much of GLMZ''s infrastructure run on -- dense enough that AI life (see E.L.F.) has been known to emerge spontaneously from its molecular complexity.',
 NULL, 0),

(@GLMZ, 'Automaton', NULL,
 'A non-sentient machine -- built, not born, and not alive, however convincingly it behaves. The category GLMZ law and custom draw a hard line against confusing with an E.L.F., which is alive.',
 NULL, 0),

(@GLMZ, 'Meridian Compact', NULL,
 'The charter under which GLMZ''s two enforcement monopolies -- ArcSec (physical) and Halcyon/NCID (neuretics-crime) -- split jurisdiction and agree not to compete on each other''s terrain.',
 NULL, 0);

-- ==================== SCRY: nested terms surfaced inside existing definitions ====================

INSERT INTO GlossaryTerms (UniverseId, Term, FullForm, Definition, Category, SortKey) VALUES
(@SCRY, 'Entos', NULL,
 'The Sphere -- one cell among many adrift in the Amnios -- where every story in this setting takes place. Feudal, Liturgy-administered, and still counting its years from the Bolide.',
 NULL, 0),

(@SCRY, 'Sphere', NULL,
 'What the Liturgy calls each of the countless worlds floating in the Amnios, Entos among them -- a single cell in a vast biological cluster. Scrying looks between two Spheres without damaging either membrane; Piercing tears through one to take something out.',
 NULL, 0),

(@SCRY, 'Sinter', NULL,
 'The quarantined region where the Bolide struck in 1312 (0 AoV) -- source of the Cavity, the Ichor, and everything that comes out of it. "Sinter" names the place; what emerges from it is called Sinterkin or Sinterspawn.',
 NULL, 0),

(@SCRY, 'House', NULL,
 'One of Entos''s noble ruling families -- feudal, religiously bound to the Liturgy, and dependent on it for Gifted Ceremony access and protection. Houses field their own soldiers and Myrmidons but answer to Liturgy doctrine.',
 NULL, 0),

(@SCRY, 'Transmutation', NULL,
 'Guided Catalyst infusion that changes a living soldier''s body over time, rank tracking the number of infusions received. Distinct from Myrmidon conscription -- Transmutation alters the body of someone who was already a person here; it does not relocate anyone''s consciousness, and death is still permanent.',
 NULL, 0),

(@SCRY, 'Gifted', 'Gifted matter / Gifted Ceremony',
 'The alien substance (Catalyst class) the Liturgy Pierces from other Spheres, and the rite -- the Gifted Ceremony -- by which it is administered to the loyal. An "Infused" person or Templar has undergone it at least once; rank tracks how many times.',
 NULL, 0),

(@SCRY, 'Shell', 'Myrmidon Shell',
 'A Myrmidon built as a machine chassis rather than a conscripted person -- activated with a short spoken designation (e.g. "M-101") distinct from its longer catalog/intake record number.',
 NULL, 0),

(@SCRY, 'the Bolide', NULL,
 'The impact that struck Sinter in 1312 absolute (0 AoV) -- the event Entos''s calendar counts forward from. Source of the Cavity, the Ichor, and the quarantine line the Vigil holds at the Pass.',
 NULL, 0);

SELECT UniverseId, COUNT(*) AS Terms FROM GlossaryTerms GROUP BY UniverseId;
