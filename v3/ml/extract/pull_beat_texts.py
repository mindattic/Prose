"""
Reconstruct historical beat texts for each scored prose snapshot.

For each distinct (StrandId, ContentHash, ReviewedAt) we use SQL Server's
temporal table feature (FOR SYSTEM_TIME AS OF) to recover beat texts as they
existed when the reviewer panel scored them.

Output: one row per (StrandSlug, BeatNumber, ContentHash) with BeatText.
Joined with pull_reviews output to form the full training dataset.
"""
import sys
import os
sys.path.insert(0, os.path.dirname(os.path.dirname(__file__)))

import pandas as pd
from rich.console import Console
from rich.progress import track
from db import get_connection, fmt_ts
from config import REVIEWS_PARQUET, BEAT_TEXTS_PARQUET, TRAINING_PARQUET

console = Console()

# Get ordered beat IDs for a strand at the current time (StrandBeats has no temporal versioning).
BEAT_POSITIONS_SQL = """
SELECT
    sb.BeatId,
    ROW_NUMBER() OVER (ORDER BY sb.SortKey) AS BeatNumber
FROM StrandBeats sb
WHERE sb.StrandId = ?
  AND sb.IsEnabled = 1
"""

# Recover historical beat text at a specific timestamp using temporal tables.
# The timestamp is embedded as a literal string (safe: comes from our own DB).
BEAT_TEXT_AT_TS_SQL = """
SELECT TOP 1 b.[Text]
FROM Beats FOR SYSTEM_TIME AS OF '{ts}' AS b
WHERE b.Id = ?
ORDER BY b.SysStart DESC
"""

# All non-draft strands with at least one review snapshot
REVIEWED_STRANDS_SQL = """
SELECT DISTINCT
    s.Id   AS StrandId,
    s.Slug AS StrandSlug
FROM StrandReviews sr
JOIN Strands s ON s.Id = sr.StrandId
WHERE s.IsDraft = 0
"""

# Distinct snapshots per strand
SNAPSHOTS_SQL = """
SELECT DISTINCT
    sr.StrandId,
    s.Slug        AS StrandSlug,
    sr.ContentHash,
    sr.ReviewedAt,
    sr.BeatCount  AS ExpectedBeatCount
FROM StrandReviews sr
JOIN Strands s ON s.Id = sr.StrandId
WHERE s.Id = ?
  AND s.IsDraft = 0
ORDER BY sr.ReviewedAt
"""


def pull_beat_positions(conn, strand_id: str) -> list[dict]:
    cursor = conn.cursor()
    cursor.execute(BEAT_POSITIONS_SQL, (strand_id,))
    return [{"beat_id": str(row[0]), "beat_number": row[1]} for row in cursor.fetchall()]


def pull_text_at_ts(conn, beat_id: str, ts_str: str) -> str | None:
    sql = BEAT_TEXT_AT_TS_SQL.format(ts=ts_str)
    cursor = conn.cursor()
    cursor.execute(sql, (beat_id,))
    row = cursor.fetchone()
    return row[0] if row else None


def pull_texts_for_snapshot(
    conn,
    strand_id: str,
    strand_slug: str,
    content_hash: str,
    reviewed_at,
    expected_beat_count: int,
) -> list[dict]:
    ts_str = fmt_ts(reviewed_at)
    beat_positions = pull_beat_positions(conn, strand_id)

    if len(beat_positions) != expected_beat_count:
        # Beat membership has drifted from the review snapshot; skip to avoid mislabeled data.
        return []

    rows = []
    for bp in beat_positions:
        text = pull_text_at_ts(conn, bp["beat_id"], ts_str)
        if text is None:
            # Beat did not exist at that time — membership drift; discard whole snapshot.
            return []
        rows.append({
            "StrandSlug":   strand_slug,
            "BeatId":       bp["beat_id"],   # stable join key; BeatNumber is position-at-extract-time
            "BeatNumber":   bp["beat_number"],
            "ContentHash":  content_hash,
            "ReviewedAt":   reviewed_at,
            "BeatText":     text,
        })
    return rows


def run() -> pd.DataFrame:
    with get_connection() as conn:
        strands = conn.cursor().execute(REVIEWED_STRANDS_SQL).fetchall()
        strands = [{"id": str(r[0]), "slug": r[1]} for r in strands]

    console.print(f"[cyan]Reconstructing beat texts for {len(strands)} strands...[/cyan]")

    all_rows = []
    drift_skipped = 0

    with get_connection() as conn:
        for strand in track(strands, description="Strands"):
            cursor = conn.cursor()
            cursor.execute(SNAPSHOTS_SQL, (strand["id"],))
            snapshots = cursor.fetchall()

            for snap in snapshots:
                sid, slug, chash, reviewed_at, expected_count = (
                    str(snap[0]), snap[1], snap[2], snap[3], snap[4],
                )
                rows = pull_texts_for_snapshot(
                    conn, sid, slug, chash, reviewed_at, expected_count
                )
                if not rows:
                    drift_skipped += 1
                    continue
                all_rows.extend(rows)

    _COLS = ["StrandSlug", "BeatId", "BeatNumber", "ContentHash", "ReviewedAt", "BeatText"]
    df = pd.DataFrame(all_rows, columns=_COLS) if all_rows else pd.DataFrame(columns=_COLS)
    df.to_parquet(BEAT_TEXTS_PARQUET, index=False)

    console.print(f"[green]{len(df):,} beat-text rows extracted[/green]")
    if drift_skipped:
        console.print(f"[yellow]{drift_skipped} snapshots skipped (membership drift)[/yellow]")

    return df


def build_training_dataset() -> pd.DataFrame:
    """Join beat texts with aggregated scores to produce the full training set."""
    beats = pd.read_parquet(BEAT_TEXTS_PARQUET)
    scores = pd.read_parquet(REVIEWS_PARQUET)

    merged = beats.merge(
        scores,
        on=["StrandSlug", "BeatNumber", "ContentHash"],
        how="inner",
    )
    console.print(f"[green]Training dataset: {len(merged):,} rows ({merged['StrandSlug'].nunique()} strands)[/green]")
    merged.to_parquet(TRAINING_PARQUET, index=False)
    return merged


if __name__ == "__main__":
    run()
    build_training_dataset()
