DECLARE @nid uniqueidentifier=(SELECT Id FROM Nodes WHERE NodeCode='BLST');
UPDATE b SET b.[Text] = REPLACE(b.[Text],
  'the subtle absence of gravity-pull that meant the ANGEL cells were holding, the subtle hum at the edge of hearing that meant the vacuum-pumps were still working',
  'the subtle absence of gravity-pull that meant the eigenlift nodes were holding, the near-silence at the edge of hearing that meant the coherence drivers were still in tune')
FROM Beats b JOIN BeatNodes bn ON bn.BeatId=b.Id WHERE bn.NodeId=@nid;
UPDATE b SET b.[Text] = REPLACE(b.[Text], 'When Amara died, I got her things', 'When my sister died, I got her things')
FROM Beats b JOIN BeatNodes bn ON bn.BeatId=b.Id WHERE bn.NodeId=@nid;
SELECT COUNT(*) AS RemainingBad FROM Beats b JOIN BeatNodes bn ON bn.BeatId=b.Id
WHERE bn.NodeId=@nid AND (CHARINDEX('ANGEL' COLLATE Latin1_General_CS_AS, b.[Text])>0
   OR b.[Text] LIKE '%vacuum cell%' OR b.[Text] LIKE '%vacuum-pump%' OR b.[Text] LIKE '%vacuum pump%'
   OR b.[Text] LIKE '%Amara%' OR b.[Text] LIKE '%Johanna%');
