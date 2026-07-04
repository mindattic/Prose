DECLARE @NodeId UNIQUEIDENTIFIER = (SELECT Id FROM Nodes WHERE Slug = 'death-whispers-in-a-cats-ear-019ec3fe');

PRINT '===== (a) Chapter-start order by SortKey =====';
SELECT nb.SortKey, b.BeatTitle, b.Id AS BeatId
FROM NodeBeats nb
JOIN Beats b ON b.Id = nb.BeatId
WHERE nb.NodeId = @NodeId AND nb.IsEnabled = 1 AND b.IsChapterStart = 1
ORDER BY nb.SortKey;

PRINT '===== (b) Total enabled beat count (expect 564) =====';
SELECT COUNT(*) AS TotalEnabledBeats
FROM NodeBeats nb
WHERE nb.NodeId = @NodeId AND nb.IsEnabled = 1;

PRINT '===== (c) Duplicate SortKeys within node (expect 0 rows) =====';
SELECT nb.SortKey, COUNT(*) AS Cnt
FROM NodeBeats nb
WHERE nb.NodeId = @NodeId AND nb.IsEnabled = 1
GROUP BY nb.SortKey
HAVING COUNT(*) > 1;

PRINT '===== (d) Beat 4968 placement (expect SortKey 42600.0, between 3832 @ 42500.0 and 3844 @ 42700.0) =====';
SELECT b.Number, b.Id AS BeatId, nb.SortKey
FROM NodeBeats nb
JOIN Beats b ON b.Id = nb.BeatId
WHERE nb.NodeId = @NodeId AND nb.IsEnabled = 1
  AND nb.SortKey BETWEEN 42400.0 AND 42800.0
ORDER BY nb.SortKey;
