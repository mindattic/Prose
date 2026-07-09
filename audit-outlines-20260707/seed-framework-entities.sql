DECLARE @glmz uniqueidentifier = '0197E9C9-0001-7000-8000-000000000001';
DECLARE @now datetime2 = SYSUTCDATETIME();

-- New entities (skip if name already exists)
IF NOT EXISTS (SELECT 1 FROM Entities WHERE Name = 'Quantum Crystal Entanglement')
INSERT INTO Entities (Id, EntityType, Name, Slug, Status, Description, CreatedAt, ModifiedAt, IsActive, UniverseId)
VALUES (NEWID(), 'vocabulary', 'Quantum Crystal Entanglement', 'quantum-crystal-entanglement', 'active',
'QCE — the marketing-brochure explanation of remote frame operation that corpo sales reps give nervous clients. Technicians find it embarrassing: entanglement famously cannot transmit information, and the explanation collapses under one pointed question. The actual mechanism is Coherent Eigenstate Projection — consciousness is not transmitted anywhere; it is projected as a tuned eigenstate into a resonant substrate. QCE survives in brochures, onboarding videos, and lawsuits.',
@now, @now, 1, @glmz);

IF NOT EXISTS (SELECT 1 FROM Entities WHERE Name = 'Coherence Frame')
INSERT INTO Entities (Id, EntityType, Name, Slug, Status, Description, CreatedAt, ModifiedAt, IsActive, UniverseId)
VALUES (NEWID(), 'technology', 'Coherence Frame', 'coherence-frame', 'active',
'The powered lattice woven through a structure''s skeleton that holds the matter in its envelope in a partially decoherent mass-state — the hardware layer of Eigenlift (Coherent Mass-State Suspension). The frame continuously tunes the structure''s mass eigenstate; Coherence Drivers do the moment-to-moment re-tuning against ambient decoherence. Official register calls failure events "decoherence incidents"; professionals say a building is "framed" or "on frame." A frame never reaches full suppression — civic-grade frames hold a district at 3-8% effective mass with tethers and conventional structure carrying the remainder.',
@now, @now, 1, @glmz);

IF NOT EXISTS (SELECT 1 FROM Entities WHERE Name = 'Eigenlift Node')
INSERT INTO Entities (Id, EntityType, Name, Slug, Status, Description, CreatedAt, ModifiedAt, IsActive, UniverseId)
VALUES (NEWID(), 'technology', 'Eigenlift Node', 'eigenlift-node', 'active',
'The visible unit of eigenlift architecture: a small, solid-state emitter with no moving parts and no interior to fail catastrophically. Nodes cluster at a structure''s tensegrity attachment points, giving float platforms their signature constellation-of-small-glowing-components silhouette — and no other visible means of support at all. Slang: a node holding tune is "clean"; a dead, dark node is "cold"; a decommissioned or hacked node is a "gray node" — heavier, unevenly tuned, no failure warning.',
@now, @now, 1, @glmz);

IF NOT EXISTS (SELECT 1 FROM Entities WHERE Name = 'Sync Depth')
INSERT INTO Entities (Id, EntityType, Name, Slug, Status, Description, CreatedAt, ModifiedAt, IsActive, UniverseId)
VALUES (NEWID(), 'vocabulary', 'Sync Depth', 'sync-depth', 'active',
'The bandwidth fraction of a Neuretic Substrate Bridging projection — how much of the operator rides the frame. Risk scales with it: burning a simple drone at 10% barely registers when the drone dies; running a heavy mech at 90% when it takes a kill-shot means feedback cascade back down the projection and a real chance of never waking up. Everything dangerous about NSB concentrates in the return handshake, and sync depth is the multiplier.',
@now, @now, 1, @glmz);

IF NOT EXISTS (SELECT 1 FROM Entities WHERE Name = 'Going Deep')
INSERT INTO Entities (Id, EntityType, Name, Slug, Status, Description, CreatedAt, ModifiedAt, IsActive, UniverseId)
VALUES (NEWID(), 'vocabulary', 'Going Deep', 'going-deep', 'active',
'Street term for running a Neuretic Substrate Bridging projection at high sync depth — most of the operator in the frame, a sliver minding the Husk. Deep runs get the fine motor control and full sensorium that contract work pays for, and they are where the horror stories come from: a frame killed at depth propagates shock back through the projection before the operator can drop sync.',
@now, @now, 1, @glmz);

IF NOT EXISTS (SELECT 1 FROM Entities WHERE Name = 'Husk')
INSERT INTO Entities (Id, EntityType, Name, Slug, Status, Description, CreatedAt, ModifiedAt, IsActive, UniverseId)
VALUES (NEWID(), 'vocabulary', 'Husk', 'husk', 'active',
'The operator''s home body during a Neuretic Substrate Bridging projection. It does not go empty — it enters a managed low-activity suspension: metabolism continues, the body reads catatonic, and the EEG signature is unclassifiable (not sleep, not anesthesia, not vegetative). The Husk is physically defenseless, which is one of NSB''s two great vulnerabilities: an unguarded body, a power failure, or a forced neural interrupt can strand the projection or collapse it mid-return. Distinct from the Shell, which is the machine being inhabited.',
@now, @now, 1, @glmz);

-- Expansions of thin existing entries
UPDATE Entities SET Description =
'The return path of a Neuretic Substrate Bridging projection — and the technology''s dramatic engine. Coming back is not automatic: the projection must complete a handshake between the target''s neural bus and the operator''s neuretic array. No handshake, no operator. Everything dangerous about NSB concentrates here: feedback cascades (a frame destroyed at high sync depth propagates shock backward before the operator can drop sync), dirty returns, degraded arrays. Risk scales with sync depth.',
ModifiedAt = @now
WHERE Name = 'Handshake' AND EntityType = 'vocabulary';

UPDATE Entities SET Description =
'The machine a Frame operator inhabits during a Neuretic Substrate Bridging projection — drone, mech, or purpose-built chassis. The Shell''s neural bus acts as the resonant cavity sustaining the operator''s projected eigenstate in parallel with its own base firmware: nothing is copied, nothing uploaded — a standing wave maintained in a second medium. Operators personalize long-run Shells and treat damage to a worn-in Shell as something close to personal injury. Distinct from the Husk, the dormant home body left behind.',
ModifiedAt = @now
WHERE Name = 'Shell' AND EntityType = 'vocabulary';

UPDATE Entities SET Description =
'Street term for operating a frame by Neuretic Substrate Bridging projection — riding a machine as a ghost rides a body. Ghost-riding a cheap drone at shallow sync is commodity labor; ghost-riding heavy iron at depth is a specialist trade with a short life expectancy and its own funeral customs.',
ModifiedAt = @now
WHERE Name = 'Ghost Ride' AND EntityType = 'vocabulary';

SELECT Name, EntityType, LEN(Description) AS DescLen FROM Entities
WHERE Name IN ('Quantum Crystal Entanglement','Coherence Frame','Eigenlift Node','Sync Depth','Going Deep','Husk','Handshake','Shell','Ghost Ride')
  AND EntityType IN ('vocabulary','technology')
ORDER BY Name;
