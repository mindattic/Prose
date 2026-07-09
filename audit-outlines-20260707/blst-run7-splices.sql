DECLARE @nid uniqueidentifier=(SELECT Id FROM Nodes WHERE NodeCode='BLST');

-- Retired lift tech (beats 1, 24-ish, 25-ish)
UPDATE b SET b.[Text] = REPLACE(b.[Text],
  'filtered through the aerogel insulation of the ANGEL lift cells',
  'filtered through the aerogel cladding of the eigenlift node housings')
FROM Beats b JOIN BeatNodes bn ON bn.BeatId=b.Id WHERE bn.NodeId=@nid;

UPDATE b SET b.[Text] = REPLACE(b.[Text],
  'floating on vacuum cells generating a collective lift of 1.485 million kilos-force',
  'riding a coherence frame holding a collective suppression of 1.485 million kilos-force')
FROM Beats b JOIN BeatNodes bn ON bn.BeatId=b.Id WHERE bn.NodeId=@nid;

UPDATE b SET b.[Text] = REPLACE(b.[Text],
  'The vacuum cells are still holding, but the control architecture is losing synchronization',
  'The eigenlift nodes are still holding tune, but the driver architecture is losing synchronization')
FROM Beats b JOIN BeatNodes bn ON bn.BeatId=b.Id WHERE bn.NodeId=@nid;

-- Schism de-naming (lock 3.8): beats 5, 11, 14
UPDATE b SET b.[Text] = REPLACE(b.[Text],
  'close enough to the Schism resonance that it made her teeth ache',
  'close enough to the frame''s detuning band that it made her teeth ache')
FROM Beats b JOIN BeatNodes bn ON bn.BeatId=b.Id WHERE bn.NodeId=@nid;

-- Beat 11: also fixes the Blue Massacre chronology (2096 is 130 years before 2226)
UPDATE b SET b.[Text] = REPLACE(b.[Text],
  'I lived through the Pulse expansion in the ''70s. I lived through the Schism fence going up around the eastern zones. I lived through the Blue Massacre when ArcSec took the city.',
  'I lived through the Pulse expansion in the ''70s. I lived through the Block Wars. My grandmother lived through the Blue Massacre, when ArcSec took the city — she never once called them police, her whole life.')
FROM Beats b JOIN BeatNodes bn ON bn.BeatId=b.Id WHERE bn.NodeId=@nid;

UPDATE b SET b.[Text] = REPLACE(b.[Text],
  'that 19 Hz resonance that was supposedly the baseline frequency of the Schism itself, though Wen had never understood what that meant exactly',
  'that 19 Hz resonance the old maintenance manuals flagged and never explained, though Wen had never understood what it meant exactly')
FROM Beats b JOIN BeatNodes bn ON bn.BeatId=b.Id WHERE bn.NodeId=@nid;

SELECT
  (SELECT COUNT(*) FROM Beats b JOIN BeatNodes bn ON bn.BeatId=b.Id WHERE bn.NodeId=@nid
     AND (CHARINDEX('ANGEL' COLLATE Latin1_General_CS_AS, b.[Text])>0 OR b.[Text] LIKE '%vacuum cell%' OR b.[Text] LIKE '%vacuum-cell%')) AS RetiredTech,
  (SELECT COUNT(*) FROM Beats b JOIN BeatNodes bn ON bn.BeatId=b.Id WHERE bn.NodeId=@nid AND b.[Text] LIKE '%Schism%') AS SchismRefs;
