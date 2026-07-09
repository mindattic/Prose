DECLARE @glmz uniqueidentifier = '0197E9C9-0001-7000-8000-000000000001';
DECLARE @src nvarchar(100) = 'manual:cep-eigenlift-framework-20260707';

DECLARE @cep uniqueidentifier   = (SELECT TOP 1 Id FROM Entities WHERE Name='Coherent Eigenstate Projection');
DECLARE @nsb uniqueidentifier   = (SELECT TOP 1 Id FROM Entities WHERE Name='Neuretic Substrate Bridging');
DECLARE @lift uniqueidentifier  = (SELECT TOP 1 Id FROM Entities WHERE Name='Eigenlift' AND EntityType='technology');
DECLARE @frame uniqueidentifier = (SELECT TOP 1 Id FROM Entities WHERE Name='Coherence Frame');
DECLARE @node uniqueidentifier  = (SELECT TOP 1 Id FROM Entities WHERE Name='Eigenlift Node');
DECLARE @drv uniqueidentifier   = (SELECT TOP 1 Id FROM Entities WHERE Name='Coherence Driver');
DECLARE @hush uniqueidentifier  = (SELECT TOP 1 Id FROM Entities WHERE Name='The Hush and the Hum');
DECLARE @hand uniqueidentifier  = (SELECT TOP 1 Id FROM Entities WHERE Name='Handshake' AND EntityType='vocabulary');
DECLARE @sync uniqueidentifier  = (SELECT TOP 1 Id FROM Entities WHERE Name='Sync Depth');
DECLARE @deep uniqueidentifier  = (SELECT TOP 1 Id FROM Entities WHERE Name='Going Deep');
DECLARE @ghost uniqueidentifier = (SELECT TOP 1 Id FROM Entities WHERE Name='Ghost Ride');
DECLARE @shell uniqueidentifier = (SELECT TOP 1 Id FROM Entities WHERE Name='Shell' AND EntityType='vocabulary');
DECLARE @husk uniqueidentifier  = (SELECT TOP 1 Id FROM Entities WHERE Name='Husk' AND EntityType='vocabulary');
DECLARE @ice uniqueidentifier   = (SELECT TOP 1 Id FROM Entities WHERE Name='Black Ice' AND EntityType='technology');

IF OBJECT_ID('tempdb..#e') IS NOT NULL DROP TABLE #e;
CREATE TABLE #e (S uniqueidentifier, T uniqueidentifier, R nvarchar(60), D nvarchar(300));
INSERT INTO #e VALUES
(@cep,  @nsb,   'theoretical_basis_of', 'CEP is the physics underlying Neuretic Substrate Bridging: consciousness projected as a tuned eigenstate, not transmitted'),
(@cep,  @lift,  'theoretical_basis_of', 'CEP extended from minds to mass: Eigenlift holds matter in tuned mass-eigenstates — one deep discovery, two industries'),
(@lift, @frame, 'implemented_by',       'Eigenlift is delivered by a coherence frame woven through the structural skeleton'),
(@frame,@node,  'composed_of',          'A coherence frame terminates in Eigenlift nodes clustered at tensegrity attachment points'),
(@frame,@drv,   'tuned_by',             'Coherence Drivers continuously re-tune the frame against ambient decoherence'),
(@lift, @hush,  'signature_of',         'The Hush (silence = healthy) and the Hum (rising = failing) are eigenlift''s binding sound design'),
(@nsb,  @hand,  'vulnerable_at',        'The return handshake is NSB''s concentrated point of failure: no handshake, no operator'),
(@nsb,  @sync,  'scaled_by',            'NSB risk and fidelity both scale with sync depth'),
(@sync, @deep,  'street_term',          'Going deep = running at high sync depth'),
(@nsb,  @ghost, 'street_term',          'Ghost-riding = operating a frame via NSB projection'),
(@nsb,  @shell, 'projects_into',        'The Shell''s neural bus is the resonant cavity sustaining the projected eigenstate'),
(@nsb,  @husk,  'leaves_behind',        'The Husk is the operator''s suspended home body during projection'),
(@shell,@husk,  'counterpart_of',       'Shell = inhabited machine; Husk = dormant home body'),
(@ice,  @hand,  'attacks',              'Black Ice targets the projection chain — dirty returns and forced interrupts at the handshake');

INSERT INTO Edges (SourceId, TargetId, RelationType, Description, Weight, Sentiment, StoryValidFrom, Source, UniverseId)
SELECT e.S, e.T, e.R, e.D, 1.0, 'neutral', '2226-01-01', @src, @glmz
FROM #e e
WHERE e.S IS NOT NULL AND e.T IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM Edges x WHERE x.SourceId=e.S AND x.TargetId=e.T AND x.RelationType=e.R);

SELECT COUNT(*) AS FrameworkEdges FROM Edges WHERE Source = @src;
