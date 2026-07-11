"""
find_near_dupes.py — Cross-story near-duplicate beat detection.

Algorithm:
  1. Pull all enabled beat texts + story slugs from DB (BeatNodes → Beats → Nodes).
  2. Embed via sentence-transformers/all-MiniLM-L6-v2 (local, CPU-only, no API calls).
  3. Compute pairwise cosine similarity in numpy (efficient for ≤20k beats).
  4. Flag pairs with cosine ≥ 0.92 that belong to different story nodes.
  5. Write each pair to the Findings table (Category=NearDuplicate, deduped by DedupKey).

Usage:
    python orchestrate/find_near_dupes.py [--threshold 0.92] [--batch 256]

Called by nightly_run.py as phase "find_near_dupes".
"""
import sys
import os
sys.path.insert(0, os.path.dirname(os.path.dirname(__file__)))

import argparse
import numpy as np
from rich.console import Console
from db import get_connection, fetchdf, execute

console = Console()

SIMILARITY_THRESHOLD = 0.92
EMBED_MODEL = "sentence-transformers/all-MiniLM-L6-v2"

BEATS_QUERY = """
SELECT
    CONVERT(nvarchar(36), b.Id)   AS BeatId,
    b.Number                      AS BeatNumber,
    b.Text                        AS BeatText,
    CONVERT(nvarchar(36), n.Id)   AS NodeId,
    ISNULL(
        -- direct story node
        CASE WHEN n.ParentNodeId IS NULL THEN n.Code ELSE NULL END,
        -- chapter → story node (parent)
        (SELECT TOP 1 p.Code FROM Nodes p WHERE p.Id = n.ParentNodeId AND p.ParentNodeId IS NULL)
    )                             AS StoryCode
FROM BeatNodes bn
JOIN Beats b  ON b.Id  = bn.BeatId
JOIN Nodes n  ON n.Id  = bn.NodeId
WHERE bn.IsEnabled = 1
  AND b.Text IS NOT NULL
  AND LEN(TRIM(b.Text)) > 100
"""

UPSERT_FINDING = """
MERGE dbo.Findings AS t
USING (SELECT @dedup AS dk) AS s ON t.DedupKey = s.dk
WHEN MATCHED THEN
    UPDATE SET Status = 'New', DetectedAt = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (FilePath, ChapterId, Category, Severity, Summary, Snippet, SuggestedFix, Status, DedupKey)
    VALUES (@fp, NULL, 'NearDuplicate', 'Medium', @summary, @snippet, NULL, 'New', @dedup);
"""


def embed_texts(texts: list[str], batch_size: int = 256) -> np.ndarray:
    from sentence_transformers import SentenceTransformer
    console.print(f"[cyan]Loading embedding model: {EMBED_MODEL}[/cyan]")
    model = SentenceTransformer(EMBED_MODEL)
    console.print(f"[cyan]Embedding {len(texts)} beats (batch={batch_size})...[/cyan]")
    vecs = model.encode(texts, batch_size=batch_size, show_progress_bar=True,
                        normalize_embeddings=True)
    return np.array(vecs, dtype=np.float32)


def run(threshold: float = SIMILARITY_THRESHOLD, batch_size: int = 256):
    with get_connection() as conn:
        df = fetchdf(conn, BEATS_QUERY)

    if df.empty:
        console.print("[yellow]No beats found.[/yellow]")
        return

    console.print(f"[green]Loaded {len(df)} enabled beats.[/green]")
    texts  = df["BeatText"].fillna("").tolist()
    vecs   = embed_texts(texts, batch_size)

    # Pairwise cosine similarity via matrix multiply (vectors are L2-normalized)
    console.print("[cyan]Computing pairwise similarities...[/cyan]")
    sim_matrix = vecs @ vecs.T  # shape: (N, N)
    np.fill_diagonal(sim_matrix, 0.0)  # zero out self-similarity

    # Find pairs above threshold from different stories
    story_codes = df["StoryCode"].fillna("").tolist()
    beat_ids    = df["BeatId"].tolist()
    beat_nums   = df["BeatNumber"].tolist()

    rows_i, rows_j = np.where(sim_matrix >= threshold)
    # Only keep i < j to avoid duplicate pairs
    mask    = rows_i < rows_j
    pairs_i = rows_i[mask]
    pairs_j = rows_j[mask]

    findings = []
    for i, j in zip(pairs_i, pairs_j):
        if story_codes[i] == story_codes[j]:
            continue  # same story — skip
        sim     = float(sim_matrix[i, j])
        story_a = story_codes[i] or "unknown"
        story_b = story_codes[j] or "unknown"
        summary = (f"Beat #{beat_nums[i]} ≈ Beat #{beat_nums[j]} "
                   f"({sim:.3f}) across {story_a}/{story_b}")
        fp      = f"beat:{beat_ids[i]}"
        snippet = f"Also: beat:{beat_ids[j]}"
        dedup   = f"{fp}|NearDuplicate|{beat_ids[j]}".lower()[:450]
        findings.append((fp, summary, snippet, dedup))

    console.print(f"[green]{len(findings)} cross-story near-duplicate pair(s) found.[/green]")

    if findings:
        with get_connection() as conn:
            for fp, summary, snippet, dedup in findings:
                execute(conn, UPSERT_FINDING,
                        (dedup, fp, summary, snippet, dedup))
        console.print(f"[green]{len(findings)} finding(s) written to Findings table.[/green]")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--threshold", type=float, default=SIMILARITY_THRESHOLD)
    parser.add_argument("--batch",     type=int,   default=256)
    args = parser.parse_args()
    run(args.threshold, args.batch)
