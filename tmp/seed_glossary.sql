SET NOCOUNT ON;

DECLARE @GLMZ UNIQUEIDENTIFIER = '0197E9C9-0001-7000-8000-000000000001';
DECLARE @SCRY UNIQUEIDENTIFIER = '0197E9C9-0002-7000-8000-000000000002';

-- ============================== GLMZ ==============================

INSERT INTO GlossaryTerms (UniverseId, Term, FullForm, Definition, Category, SortKey) VALUES
(@GLMZ, 'GLMZ', 'Great Lakes Metropolitan Zone',
 'The corridor of unified urban sprawl encircling the Great Lakes in 2226 -- governed not by one government but by competing CorpoNations, Halcyon and Arcturus''s enforcement monopolies, and the gray zones neither reaches.',
 'Geography', 10),

(@GLMZ, 'E.L.F.', 'Emergent Life Form',
 'A category of AI life that evolved spontaneously from the Substrate''s molecular complexity -- genuinely alive, animal-intelligence, not built. Distinct from an Automaton, which is a non-sentient machine and not alive.',
 'AI & Substrate', 20),

(@GLMZ, 'AAMA', 'Anomalous Activity Monitoring Authority',
 'The civil authority that classifies schisms -- dimensional anomalies pressing into 3D space -- Class-1 through Class-5 by intensity. A Class-3-or-higher site requires an AAMA permit to enter.',
 'Bureaucracy', 30),

(@GLMZ, 'Schism', NULL,
 'Official term for the geometry-breaking anomalies scattered across GLMZ -- a hum at 19Hz from no direction, a block that takes eleven steps when geometry says nine. Classified by the AAMA, whose own researchers have started to suspect what its classification system doesn''t officially know: schisms are cross-sections of higher-dimensional shapes, occupied and contested by at least two intelligences operating through them.',
 'World Physics', 40),

(@GLMZ, N'Φ', 'Quanta (Q, Qs)',
 N'GLMZ''s currency: an allotment of computational power, divided among every citizen, that can be saved, transferred, or spent on computation. The symbol Φ always precedes the number (Φ100, never "100Φ"). Informal terms: "quanta," "Q," "Qs." Physical medium is the credstick only -- no coins, no bills.',
 'Currency', 50),

(@GLMZ, 'Channeler', NULL,
 'A gray-zone operator class: session-injection into networked frames and direct AI negotiation. One of three recognized operator classes in GLMZ, alongside Ghost and Splicer.',
 'Operator Classes', 60),

(@GLMZ, 'Ghost', 'eigenstate conscience transfer (ECT)',
 'An operator class: inhabiting any machine with coherent circuitry via eigenstate conscience transfer. The body left behind during a ghost is called a Husk. Casual verb use: "ghosting" (lowercase) -- e.g. "she ghosted the stoplight green."',
 'Operator Classes', 70),

(@GLMZ, 'Husk', NULL,
 'The body at rest while its owner is ghosting -- occupying another machine via eigenstate conscience transfer.',
 'Operator Classes', 80),

(@GLMZ, 'Splicer', NULL,
 'An operator class built on hardware/software attack surface: credential work and physical build, rather than direct AI negotiation (Channeler) or inhabiting machines (Ghost).',
 'Operator Classes', 90),

(@GLMZ, 'Hyper Reality', NULL,
 'Anything seen, heard, or felt through neuretic augmentation with no physical counterpart. Not a metaphor -- a literal layer archology infrastructure is built to project into (AR-optimized surfaces, holographic signage, ambient brand effects). Unaugmented characters live permanently in the blank physical substrate underneath it.',
 'World Physics', 100),

(@GLMZ, 'ArcSec', 'Arcturus Military Command (Arcturus Defense Solutions)',
 'The physical-security monopoly chartered under the Meridian Compact: sovereign perimeter defense, PMC contracting, corporate military, autonomous weapons. Does not compete with Halcyon/NCID''s neuretics-crime jurisdiction.',
 'Enforcement', 110),

(@GLMZ, 'NCID', 'Neuretic Crime Investigation Division',
 'Halcyon Civil Security''s investigative field arm -- street name RaNCID. The neuretics-crime enforcement monopoly: illegal ghosting (ECT), unauthorized neural surveillance, black ice deployment, neuretics assault. Halcyon''s administrative control of augmentation certification gives NCID forensic access no other body can match.',
 'Enforcement', 120),

(@GLMZ, 'CorpoNation', NULL,
 'A corporate sovereign, not merely a company -- holds territory, flies flags over substations, fields security, and can die; when it does, its infrastructure often outlives it (Axiom Industrial''s mounting collars are everywhere three decades after Axiom''s collapse).',
 'Corporate Power', 130),

(@GLMZ, 'neuretics', NULL,
 'Thought-operated in-head compute that everyone in GLMZ has at some tier -- grown, not implanted. Checkpoints read the wearer biometrically through it. A common noun, lowercase, like "sword" or "hand."',
 'Tech', 140),

(@GLMZ, 'Gray Zone', NULL,
 'Corridor territory outside CorpoNation law and enforcement reach -- where civic code exists on paper and goes universally unenforced. The commercial Hyper Reality layer thins or vanishes entirely below "the Veil," the boundary between archology-tier city and the gray zone underneath it.',
 'Geography', 150),

(@GLMZ, 'NGRA', NULL,
 'Recurring background marker for a character with a military or mercenary past on the Gray Zone circuit ("Former NGRA," "NGRA veteran") -- the institution the initials name hasn''t been spelled out on the page yet.',
 'Background', 160);

-- ============================== SCRY ==============================

INSERT INTO GlossaryTerms (UniverseId, Term, FullForm, Definition, Category, SortKey) VALUES
(@SCRY, 'Sinterkin', NULL,
 'What a completed organism from the Sinter Cavity is called once it moves -- coated permanently in black, oil-thick residue, its proportions never fully "finished." The purest expression of what came through the membrane at Sinter: the Expectant''s own cancerous flesh, fully grown, undiluted by anything native to Entos. The rarest and most dangerous free-roaming threat past the quarantine line, because there is no native biology in it to predict or wound familiarly.',
 'Creatures', 10),

(@SCRY, 'Sinterspawn', NULL,
 'What a Sinterkin makes of something else. A free-roaming Sinterkin infects or "mates with" any living Entos creature it encounters -- deer, hound, person -- and the result is neither Sinterkin nor the original animal, but a hijacked, warped hybrid. Dangerous, but killing one only stops one corrupted thing; killing the Sinterkin that made it stops the source.',
 'Creatures', 20),

(@SCRY, 'Harrower', NULL,
 'Soldiers'' and villagers'' name for the body/behavior of the larger, pack-anchoring creature at a Sinterkin/Sinterspawn encounter -- a silhouette, not a taxonomic claim. Almost always a Sinterspawn wearing that shape; exceedingly rarely, a true Sinterkin wears the same silhouette, and nothing about the body alone tells you which.',
 'Creatures', 30),

(@SCRY, 'Ichor', NULL,
 'The caustic, faintly luminous fluid that runs in Sinterspawn instead of blood -- reacts with untreated metal and blisters unprotected skin within seconds. Not venom, not sprayed offensively: simply what fills a body that came out of, or was made wrong by, the Sinter overflow. Handlers who work Sinterspawn at close range wear Ichor-proof plate.',
 'Creatures', 40),

(@SCRY, 'AoV', 'Age of Vigil',
 'Entos''s calendar epoch counted from the Bolide/Fall of Sinter (1312 absolute = 0 AoV). The current story year, 1371, is 59 AoV. Pre-Bolide dates belong to the preceding Age of Scrying.',
 'Calendar', 50),

(@SCRY, 'Myrmidon', NULL,
 'A person taken from another Sphere by Piercing and conscripted into House military service -- the Liturgy''s own term, not the conscript''s word (soldiers call it "slave-soldier"). Not on the Transmutation rank ladder; native Entos soldiers are never Myrmidons. Death is permanent -- consciousness does not transfer between bodies, and a Myrmidon who dies is gone.',
 'Military', 60),

(@SCRY, 'Catalyst', 'Gifted matter class; Xerum 525 primary',
 'Infusion of alien Gifted matter into a living person, producing controlled physical change over time -- not consciousness transfer. The body is changed; the person who entered the procedure is the same person afterward, altered rather than replaced. Xerum 525 ("Red Mercury") is the primary, most-studied Catalyst; field slang is "the dose."',
 'Tech & Substances', 70),

(@SCRY, 'Amnios', NULL,
 'The womb of an impossibly large biological creature -- the Expectant -- inside which every Sphere (including Entos and Sphere 31/Earth) floats as a single cell in a dense cluster, separated from its neighbors by thin films of Amnios fluid.',
 'Cosmology', 80),

(@SCRY, 'the Expectant', NULL,
 'The organism whose body the Amnios and every Sphere inside it belong to. The Sinter Bolide was the Expectant''s own cancer -- a cell of her own body, gone malignant, breaching into Entos''s cell wall from within.',
 'Cosmology', 90),

(@SCRY, 'Scrying', NULL,
 'Pulling two adjacent cell walls (Spheres) tight against each other so a practitioner can look through -- without rupturing or damaging either membrane.',
 'Magic System', 100),

(@SCRY, 'Piercing', NULL,
 'The harder, rarer act of reaching through a Sphere''s membrane and drawing something out of another Sphere -- a rupture, not a stretch, that leaves scar tissue behind.',
 'Magic System', 110),

(@SCRY, 'the Liturgy', NULL,
 'The religious-scientific sect that administers the Gifted Ceremony and exists outside House politics, predating the current Houses. Does not fight wars or take territory; its doctrine and rites are the foundation the Houses'' own legitimacy is built on.',
 'Institutions', 120),

(@SCRY, 'Received Tongue', 'also called Liturgin',
 'The Liturgy''s dead, Scried language, used for writs, rites, and rank-names -- commoners call it High Speech, cathedral-talk, or the Silence-tongue. Rendered on Templar Station terminal screens as "Liturgin."',
 'Language', 130),

(@SCRY, 'Templar', NULL,
 'The Liturgy''s own guard order, founded generations before the current Houses existed to argue with the Liturgy -- its "Vatican Guard." An "Infused Templar" has undergone the Gifted Ceremony, which is what opens a sealed Templar Station. Rank tracks the number of Gifted infusions a Templar carries.',
 'Institutions', 140),

(@SCRY, 'Heloth', NULL,
 'Two-legged steam-chemical machines, roughly 3m tall -- the dominant labor and transport technology of Entos, named for an extinct draft animal they replaced before horses ever could. Entos has no horse analogue; Heloth variants cover freight, courier, and battle-rig roles.',
 'Tech', 150),

(@SCRY, 'M-101', 'Myrmidon Shell designation',
 'The short, spoken designation given to an activated Myrmidon Shell, distinct from its longer catalog/intake record number (e.g. "M-1018883"). A Shell may insist on the short form as its name and reject the longer number as "paperwork."',
 'Military', 160);

SELECT UniverseId, COUNT(*) AS Terms FROM GlossaryTerms GROUP BY UniverseId;
