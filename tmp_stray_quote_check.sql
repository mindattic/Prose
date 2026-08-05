SELECT n.Title, bn.SortKey, b.Id, LEFT(b.Text,160) AS Snip
FROM Beats b JOIN BeatNodes bn ON bn.BeatId=b.Id JOIN Nodes n ON n.Id=bn.NodeId
WHERE (n.Id='019F3007-F3FC-7CF7-A38D-65C00E092FEB' OR n.ParentNodeId='019F3007-F3FC-7CF7-A38D-65C00E092FEB')
AND bn.IsEnabled=1
AND LEFT(b.Text,1)='"'
AND b.Text NOT LIKE '%says%'
AND b.Text NOT LIKE '%asks%'
ORDER BY n.Title, bn.SortKey;
