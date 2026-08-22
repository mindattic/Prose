# /progress — Strand Progress Dashboard

Show a dashboard table of all non-archived strands with their Code, Title, Kind, Status, Score, and estimated Pages.

## Status: blocked pending a Hub-routed command (2026-08-22)

This command previously instructed running a raw `sqlcmd` query against the database directly.
That is no longer allowed under any circumstances — nothing reaches the database except through
Prose.Hub (HARD, absolute rule; see project memory `feedback_all_writes_through_hub`). No CLI
`--flag` or MCP tool currently exposes this dashboard's query (strand code/title/kind/status/score/
page-count roll-up).

**Do not fall back to raw `sqlcmd` to make this command work.** If the user invokes `/progress`,
tell them a proper Hub-routed CLI/MCP command needs to be built first, and ask whether they want
that built now or want the dashboard some other way (e.g. via the `/show` skill for a narrower
lookup, or as a new `prose --strand-progress` CLI command).

The original query this command ran, preserved here for reference when building the real
replacement — this is DOCUMENTATION of intent, not something to execute directly:

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

Render target (once a real command exists): a clean markdown table with columns **Code** | **Title**
| **Kind** | **Status** | **Score** | **Pages**. Omit stub chapters (Pages = 0 AND Score = NULL)
unless there are fewer than 10 rows total. Add a one-line summary at the end: total strands, total
pages written, mean score across scored strands.
