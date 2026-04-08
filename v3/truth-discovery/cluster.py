"""
Phase 3: Cluster embedded triples using HDBSCAN. Triples in the same
cluster represent the same claim stated different ways. This is where
"treats headaches" and "helps with headaches" get grouped together.

Usage: python cluster.py [--min-cluster-size 3] [--min-samples 2]
"""
import sqlite3
import os
import numpy as np
from dotenv import load_dotenv
from rich.console import Console

load_dotenv()

DB_PATH = os.getenv("DB_PATH", "truth.db")
SIMILARITY_THRESHOLD = float(os.getenv("SIMILARITY_THRESHOLD", "0.87"))

console = Console()


def load_embeddings():
    """Load all embeddings from the database."""
    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()
    c.execute("SELECT id, embedding FROM triples WHERE embedding IS NOT NULL")
    rows = c.fetchall()
    conn.close()

    ids = []
    embeddings = []
    for row in rows:
        ids.append(row[0])
        emb = np.frombuffer(row[1], dtype=np.float32)
        embeddings.append(emb)

    return ids, np.array(embeddings) if embeddings else np.array([])


def run_clustering(min_cluster_size=3, min_samples=2):
    """Cluster all embedded triples using HDBSCAN."""
    import hdbscan

    console.print("[bold]Phase 3: Clustering[/bold]")

    ids, embeddings = load_embeddings()
    console.print(f"  Loaded {len(ids)} embeddings")

    if len(ids) < min_cluster_size:
        console.print("[yellow]Not enough embeddings to cluster.[/yellow]")
        return

    # HDBSCAN on cosine distance (1 - cosine_similarity)
    # Since embeddings are normalized, cosine distance = 1 - dot product
    console.print(f"  Running HDBSCAN (min_cluster_size={min_cluster_size}, min_samples={min_samples})...")

    clusterer = hdbscan.HDBSCAN(
        min_cluster_size=min_cluster_size,
        min_samples=min_samples,
        metric="euclidean",  # normalized embeddings: euclidean ~ cosine
        cluster_selection_method="eom",
    )

    labels = clusterer.fit_predict(embeddings)

    num_clusters = len(set(labels)) - (1 if -1 in labels else 0)
    noise_count = (labels == -1).sum()

    console.print(f"  Clusters found: {num_clusters}")
    console.print(f"  Noise points (unclustered): {noise_count}")

    # Store cluster assignments
    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()

    for triple_id, cluster_id in zip(ids, labels):
        c.execute("UPDATE triples SET cluster_id = ? WHERE id = ?", (int(cluster_id), triple_id))

    # Build cluster summary table
    c.execute("DELETE FROM clusters")
    c.execute("""
        INSERT INTO clusters (cluster_id, representative_sentence, triple_count, unique_sources)
        SELECT cluster_id,
               (SELECT full_sentence FROM triples t2 WHERE t2.cluster_id = t.cluster_id LIMIT 1),
               COUNT(*),
               COUNT(DISTINCT source_file)
        FROM triples t
        WHERE cluster_id >= 0
        GROUP BY cluster_id
    """)

    # Log
    c.execute(
        "INSERT INTO processing_log (phase, status, message) VALUES (?, ?, ?)",
        ("clustering", "complete", f"Found {num_clusters} clusters, {noise_count} noise points"),
    )

    conn.commit()
    conn.close()

    console.print(f"\n[bold green]Clustering complete![/bold green]")
    console.print(f"  Clusters: {num_clusters}")
    console.print(f"  Unclustered: {noise_count}")


if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(description="Cluster embedded triples")
    parser.add_argument("--min-cluster-size", type=int, default=3, help="Minimum cluster size for HDBSCAN")
    parser.add_argument("--min-samples", type=int, default=2, help="Minimum samples for HDBSCAN")
    args = parser.parse_args()

    run_clustering(min_cluster_size=args.min_cluster_size, min_samples=args.min_samples)
