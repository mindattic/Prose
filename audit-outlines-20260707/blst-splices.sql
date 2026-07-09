DECLARE @nid uniqueidentifier=(SELECT Id FROM Nodes WHERE NodeCode='BLST');
UPDATE b SET b.[Text] = REPLACE(REPLACE(b.[Text],
  'first-generation ANGEL-frame eigenlift systems', 'first-generation eigenlift frames'),
  'The vacuum cells weren''t holding coherence', 'The eigenlift nodes weren''t holding coherence')
FROM Beats b JOIN BeatNodes bn ON bn.BeatId=b.Id WHERE bn.NodeId=@nid;
UPDATE b SET b.[Text] = REPLACE(b.[Text], 'The ANGEL cells operate at 8 hertz', 'The eigenlift nodes operate at 8 hertz') FROM Beats b JOIN BeatNodes bn ON bn.BeatId=b.Id WHERE bn.NodeId=@nid;
UPDATE b SET b.[Text] = REPLACE(b.[Text], 'The ANGEL cells are still coherent', 'The eigenlift nodes are still coherent') FROM Beats b JOIN BeatNodes bn ON bn.BeatId=b.Id WHERE bn.NodeId=@nid;
UPDATE b SET b.[Text] = REPLACE(b.[Text], 'a vacuum pump was cycling, maintaining the pressure differential that kept the bloc aloft', 'a coherence driver was cycling, holding the tune that kept the bloc aloft') FROM Beats b JOIN BeatNodes bn ON bn.BeatId=b.Id WHERE bn.NodeId=@nid;
UPDATE b SET b.[Text] = REPLACE(b.[Text], 'My husband installed the original vacuum cell array', 'My husband installed the original eigenlift node array') FROM Beats b JOIN BeatNodes bn ON bn.BeatId=b.Id WHERE bn.NodeId=@nid;
UPDATE b SET b.[Text] = REPLACE(b.[Text], 'held up by forty-year-old vacuum cells', 'held up by forty-year-old eigenlift nodes') FROM Beats b JOIN BeatNodes bn ON bn.BeatId=b.Id WHERE bn.NodeId=@nid;
UPDATE b SET b.[Text] = REPLACE(b.[Text], 'still held aloft by vacuum cells that were failing', 'still held aloft by eigenlift nodes that were failing') FROM Beats b JOIN BeatNodes bn ON bn.BeatId=b.Id WHERE bn.NodeId=@nid;
UPDATE b SET b.[Text] = REPLACE(b.[Text], 'The pressure regulators were managing the release of vacuum cell buoyancy in measured stages', 'The coherence drivers were stepping the eigenlift nodes down in measured stages') FROM Beats b JOIN BeatNodes bn ON bn.BeatId=b.Id WHERE bn.NodeId=@nid;
UPDATE b SET b.[Text] = REPLACE(b.[Text], '"Amara. She died in 2198. Fourteen years ago.', '"She died in 2212. Fourteen years ago.') FROM Beats b JOIN BeatNodes bn ON bn.BeatId=b.Id WHERE bn.NodeId=@nid;
SELECT COUNT(*) AS RemainingBad FROM Beats b JOIN BeatNodes bn ON bn.BeatId=b.Id
WHERE bn.NodeId=@nid AND (CHARINDEX('ANGEL' COLLATE Latin1_General_CS_AS, b.[Text])>0
   OR b.[Text] LIKE '%vacuum cell%' OR b.[Text] LIKE '%vacuum pump%' OR b.[Text] LIKE '%Amara%' OR b.[Text] LIKE '%Johanna%');
