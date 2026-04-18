"""
Phase 3: Cluster embedded triples using HDBSCAN. Triples in the same
cluster represent the same claim stated different ways. This is where
"treats headaches" and "helps with headaches" get grouped together.

WHAT IS CLUSTERING?
Clustering is grouping data points that are "close" to each other. Imagine
throwing darts at a board -- the darts that land near each other form clusters.
In our case, the "darts" are sentence embeddings (384-number vectors), and
"closeness" means similarity in meaning.

WHAT IS HDBSCAN?
HDBSCAN (Hierarchical Density-Based Spatial Clustering of Applications with Noise)
is a clustering algorithm that:
  - Finds clusters of varying sizes automatically (you don't tell it how many)
  - Labels outliers as "noise" (cluster_id = -1) instead of forcing them into a group
  - Works well with high-dimensional data like embeddings
  - Uses DENSITY to find clusters: regions where points are packed together
Compare this to K-Means, where you must specify the number of clusters upfront.

The output updates two things in the database:
  1. Each triple gets a cluster_id (or -1 if it's noise/unique)
  2. The clusters table gets a summary of each cluster

Usage: python cluster.py [--min-cluster-size 3] [--min-samples 2]
"""

import sqlite3
import os
import numpy as np

from rich.console import Console
from constants import DB_PATH

# Similarity threshold -- not directly used by HDBSCAN but stored for reference.
# 0.87 means two sentences must be at least 87% similar to be considered "the same claim."
SIMILARITY_THRESHOLD = float(os.getenv("SIMILARITY_THRESHOLD", "0.87"))

# Rich console for pretty terminal output
console = Console()


def load_embeddings():
    """Load all embeddings from the database."""

    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()

    # Fetch every triple that has an embedding (Phase 2 must have run first).
    # We get the ID (to update cluster assignments later) and the raw embedding bytes.
    c.execute("SELECT id, embedding FROM triples WHERE embedding IS NOT NULL")
    rows = c.fetchall()
    conn.close()

    # Build two parallel lists: IDs and their corresponding embedding vectors.
    # "Parallel" means ids[0] corresponds to embeddings[0], ids[1] to embeddings[1], etc.
    ids = []
    embeddings = []
    for row in rows:
        ids.append(row[0])  # row[0] is the triple's database ID

        # Convert the raw bytes back into a NumPy array of 32-bit floats.
        # This is the reverse of the .tobytes() we did in embed.py.
        # np.frombuffer() interprets raw bytes as an array of numbers.
        # dtype=np.float32 tells it each number is 4 bytes (32 bits).
        emb = np.frombuffer(row[1], dtype=np.float32)
        embeddings.append(emb)

    # Convert the list of 1D arrays into a single 2D NumPy array.
    # Shape: (num_triples, 384) -- each row is one embedding.
    # HDBSCAN needs this 2D format as input.
    # If there are no embeddings, return an empty array to avoid errors.
    return ids, np.array(embeddings) if embeddings else np.array([])


def run_clustering(min_cluster_size=3, min_samples=2):
    """Cluster all embedded triples using HDBSCAN."""

    # Lazy import: hdbscan is a heavy ML library, only load it when needed
    import hdbscan

    console.print("[bold]Phase 3: Clustering[/bold]")

    # Load all embeddings from the database into memory
    ids, embeddings = load_embeddings()
    console.print(f"  Loaded {len(ids)} embeddings")

    # Can't form clusters if we have fewer points than the minimum cluster size
    if len(ids) < min_cluster_size:
        console.print("[yellow]Not enough embeddings to cluster.[/yellow]")
        return

    # HDBSCAN configuration:
    # We're using cosine distance to measure similarity between embeddings.
    # Cosine distance measures the ANGLE between two vectors (ignoring length).
    # But since our embeddings are already normalized (all length 1.0 from Phase 2),
    # Euclidean distance and cosine distance give equivalent results.
    # This is a mathematical shortcut: for unit vectors, euclidean distance
    # is a monotonic function of cosine distance, so the clustering is identical.
    console.print(f"  Running HDBSCAN (min_cluster_size={min_cluster_size}, min_samples={min_samples})...")

    # Create the HDBSCAN clusterer with our parameters:
    #   min_cluster_size: A cluster must have at least this many points.
    #     Set to 3 means we need at least 3 sources saying the same thing.
    #   min_samples: How many neighbors a point needs to be considered "core" (not edge/noise).
    #     Lower values = more points get clustered; higher = stricter, more noise.
    #   metric: The distance function to use between points.
    #     "euclidean" works because our embeddings are normalized (see above).
    #   cluster_selection_method: "eom" (Excess of Mass) is HDBSCAN's default method for
    #     choosing which clusters to keep from the hierarchy. "eom" tends to produce
    #     clusters of varying sizes, which is what we want (some claims appear in 3 files,
    #     others in 50 files).
    clusterer = hdbscan.HDBSCAN(
        min_cluster_size=min_cluster_size,
        min_samples=min_samples,
        metric="euclidean",  # normalized embeddings: euclidean ~ cosine
        cluster_selection_method="eom",
    )

    # fit_predict() does two things at once:
    #   1. "fit" -- analyze the data and discover cluster structure
    #   2. "predict" -- assign each point to a cluster (or -1 for noise)
    # Returns an array of integers: [0, 0, 1, -1, 2, 0, ...]
    # where each number is the cluster ID for that embedding.
    # -1 means "noise" -- this point doesn't clearly belong to any cluster.
    labels = clusterer.fit_predict(embeddings)

    # Count how many real clusters were found (exclude noise label -1).
    # set(labels) gives unique values like {-1, 0, 1, 2, 3}.
    # We subtract 1 if -1 is present because noise isn't a real cluster.
    num_clusters = len(set(labels)) - (1 if -1 in labels else 0)

    # Count how many triples were labeled as noise (not similar enough to anything else).
    # (labels == -1) creates a boolean array [False, False, True, ...], and .sum()
    # counts the True values (True = 1, False = 0 in Python).
    noise_count = (labels == -1).sum()

    console.print(f"  Clusters found: {num_clusters}")
    console.print(f"  Noise points (unclustered): {noise_count}")

    # Now save the cluster assignments back to the database
    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()

    # Update each triple's cluster_id in the triples table.
    # zip() pairs up the IDs and labels: (id1, label1), (id2, label2), ...
    # int(cluster_id) converts from NumPy's integer type to Python's native int
    # because SQLite doesn't understand NumPy types.
    for triple_id, cluster_id in zip(ids, labels):
        c.execute("UPDATE triples SET cluster_id = ? WHERE id = ?", (int(cluster_id), triple_id))

    # Rebuild the clusters summary table from scratch.
    # DELETE FROM removes all existing rows (we're recalculating everything).
    c.execute("DELETE FROM clusters")

    # This single SQL statement builds the entire clusters table:
    # For each cluster_id (excluding noise, which is cluster_id < 0):
    #   - Pick one representative sentence (LIMIT 1 gives us any one example)
    #   - Count how many triples are in this cluster
    #   - Count how many DISTINCT source files contributed (more = more trustworthy)
    # The subquery (SELECT full_sentence...) is a "correlated subquery" -- it runs
    # once for each cluster_id to grab a sample sentence.
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

    # Write a log entry recording that clustering is complete
    c.execute(
        "INSERT INTO processing_log (phase, status, message) VALUES (?, ?, ?)",
        ("clustering", "complete", f"Found {num_clusters} clusters, {noise_count} noise points"),
    )

    # Save everything to disk
    conn.commit()
    conn.close()

    console.print(f"\n[bold green]Clustering complete![/bold green]")
    console.print(f"  Clusters: {num_clusters}")
    console.print(f"  Unclustered: {noise_count}")


# Only run when executed directly (python cluster.py), not when imported
if __name__ == "__main__":
    import argparse

    # Set up command-line arguments for tuning HDBSCAN parameters
    parser = argparse.ArgumentParser(description="Cluster embedded triples")

    # --min-cluster-size: How many triples must say the same thing to form a cluster.
    #   Lower = more clusters (including small, possibly noisy ones).
    #   Higher = fewer, more confident clusters.
    parser.add_argument("--min-cluster-size", type=int, default=3, help="Minimum cluster size for HDBSCAN")

    # --min-samples: How dense an area must be for HDBSCAN to consider it a cluster core.
    #   Lower = more lenient clustering. Higher = stricter, more noise points.
    parser.add_argument("--min-samples", type=int, default=2, help="Minimum samples for HDBSCAN")

    parser.add_argument("--silent", action="store_true", help="Suppress all console output")
    args = parser.parse_args()
    if args.silent:
        import sys as _sys, os as _os
        _sys.stdout = open(_os.devnull, "w")
        _sys.stderr = open(_os.devnull, "w")


    run_clustering(min_cluster_size=args.min_cluster_size, min_samples=args.min_samples)
