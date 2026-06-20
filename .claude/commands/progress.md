# /progress — Strand Progress Dashboard

Show a dashboard table of all non-archived strands with their Code, Title, Kind, Status, Score, and estimated Pages.

## Instructions

Run the following SQL against `(localdb)\MSSQLLocalDB` database `StreetSamurai` and render the results as a markdown table, sorted by score descending (unscored last):

```sql
WITH latest_srs AS (
    SELECT r.StrandId, r.AvgScore AS Score, r.GeneratedAt AS ScoredAt
    FROM StrandReviewSummaries r
    INNER JOIN (
        SELECT StrandId, MAX(GeneratedAt) AS MaxDate
        FROM StrandReviewSummaries GROUP BY StrandId
    ) m ON r.StrandId = m.StrandId AND r.GeneratedAt = m.MaxDate
),
word_counts AS (
    SELECT sb.StrandId,
        SUM(LEN(b.Text) - LEN(REPLACE(b.Text, ' ', '')) + 1) AS WordCount
    FROM StrandBeats sb
    JOIN Beats b ON sb.BeatId = b.Id
    WHERE sb.IsEnabled = 1 AND b.Text IS NOT NULL AND b.Text != ''
    GROUP BY sb.StrandId
)
SELECT 
    ISNULL(s.StrandCode, '') AS Code,
    s.Title,
    s.Kind,
    s.Status,
    ROUND(srs.Score, 1) AS Score,
    ISNULL(wc.WordCount, 0) AS Words,
    ISNULL(wc.WordCount / 250, 0) AS Pages,
    CONVERT(DATE, srs.ScoredAt) AS ScoredOn
FROM Strands s
LEFT JOIN latest_srs srs ON s.Id = srs.StrandId
LEFT JOIN word_counts wc ON s.Id = wc.StrandId
WHERE s.Status != 'archived'
ORDER BY CASE WHEN srs.Score IS NULL THEN 1 ELSE 0 END, srs.Score DESC
```

Use the Bash tool with `sqlcmd` to run this. Format the output as a clean markdown table with columns: **Code** | **Title** | **Kind** | **Status** | **Score** | **Pages**. Omit stub chapters (Pages = 0 AND Score = NULL) unless there are fewer than 10 rows total. Add a one-line summary at the end: total strands, total pages written, mean score across scored strands.
