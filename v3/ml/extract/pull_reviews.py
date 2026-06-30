"""
Extract per-beat panel scores from StrandReviewBeatScores into a Parquet file.

Each output row represents one (strand, beat_position, content_hash) combination
with aggregated scores across all reviewer ballots at that prose version.
"""
import sys
import os
sys.path.insert(0, os.path.dirname(os.path.dirname(__file__)))

import pandas as pd
from rich.console import Console
from rich.progress import track
from db import get_connection, fetchdf
from config import REVIEWS_PARQUET

console = Console()

BEAT_SCORES_SQL = """
SELECT
    sr.StrandId,
    sr.StrandId + '-' + CAST(srbs.BeatNumber AS nvarchar) + '-' + sr.ContentHash AS RowKey,
    s.Slug                                                  AS StrandSlug,
    srbs.BeatNumber,
    sr.ContentHash,
    sr.ReviewedAt,
    sr.BeatCount                                            AS ExpectedBeatCount,
    COUNT(*)                                                AS ReviewCount,
    AVG(CAST(srbs.Score AS FLOAT))                          AS MeanBeatScore,
    STDEV(CAST(srbs.Score AS FLOAT))                        AS StdBeatScore,
    MIN(srbs.Score)                                         AS MinBeatScore,
    MAX(srbs.Score)                                         AS MaxBeatScore,
    AVG(CASE WHEN srbs.Score >= 4 THEN 1.0 ELSE 0.0 END)   AS HighlightRate,
    AVG(CASE WHEN srbs.Score <= 2 THEN 1.0 ELSE 0.0 END)   AS LowlightRate,
    CASE WHEN STDEV(CAST(srbs.Score AS FLOAT)) > 1.2
         THEN 1 ELSE 0 END                                  AS IsContested,
    AVG(CAST(sr.Score AS FLOAT))                            AS StrandMeanScore,
    AVG(CAST(sr.FlowScore AS FLOAT))                        AS StrandFlowScore
FROM StrandReviewBeatScores srbs
JOIN StrandReviews sr ON sr.Id = srbs.ReviewId
JOIN Strands s ON s.Id = sr.StrandId
WHERE s.IsDraft = 0
GROUP BY
    sr.StrandId, s.Slug, srbs.BeatNumber, sr.ContentHash,
    sr.ReviewedAt, sr.BeatCount
"""

GRIPES_SQL = """
SELECT
    sr.StrandId,
    s.Slug AS StrandSlug,
    sr.ContentHash,
    sr.ReviewedAt,
    sr.PersonaId,
    sr.Score,
    sr.FlowScore,
    sr.Improvements,
    sr.ReviewText,
    sr.ClusterLabel
FROM StrandReviews sr
JOIN Strands s ON s.Id = sr.StrandId
WHERE s.IsDraft = 0
  AND sr.Improvements IS NOT NULL
  AND LEN(TRIM(sr.Improvements)) > 5
"""


def pull_beat_scores(conn) -> pd.DataFrame:
    console.print("[cyan]Pulling per-beat scores...[/cyan]")
    df = fetchdf(conn, BEAT_SCORES_SQL)
    console.print(f"  [green]{len(df):,} (strand, beat, hash) combinations[/green]")
    return df


def pull_gripes(conn) -> pd.DataFrame:
    console.print("[cyan]Pulling gripe/improvements texts...[/cyan]")
    df = fetchdf(conn, GRIPES_SQL)
    console.print(f"  [green]{len(df):,} gripe records[/green]")
    return df


def run():
    with get_connection() as conn:
        beat_scores = pull_beat_scores(conn)
        gripes = pull_gripes(conn)

    # Save to Parquet
    beat_scores.to_parquet(REVIEWS_PARQUET, index=False)
    gripes.to_parquet(str(REVIEWS_PARQUET).replace(".parquet", "_gripes.parquet"), index=False)

    console.print(f"[bold green]Saved to {REVIEWS_PARQUET}[/bold green]")
    console.print(f"[bold green]Unique strands: {beat_scores['StrandSlug'].nunique()}[/bold green]")
    console.print(f"[bold green]Unique content hashes: {beat_scores['ContentHash'].nunique()}[/bold green]")
    return beat_scores, gripes


if __name__ == "__main__":
    run()
