SET NOCOUNT ON;
SELECT bn.SortKey AS SK,
  b.Id AS Id,
  REPLACE(REPLACE(b.Title, CHAR(13), ''), CHAR(10), ' ') AS Title,
  REPLACE(REPLACE(b.Text, CHAR(13), ''), CHAR(10), ' <NL> ') AS Text,
  REPLACE(REPLACE(ISNULL(b.Description,''), CHAR(13), ''), CHAR(10), ' ') AS Description,
  REPLACE(REPLACE(ISNULL(b.EventSummary,''), CHAR(13), ''), CHAR(10), ' ') AS EventSummary
FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id
WHERE bn.NodeId='3D9E873B-5763-4C90-ABF8-BAD60CD549B3' AND bn.IsEnabled=1
ORDER BY bn.SortKey
