"""
Phase 2: Generate vector embeddings for all extracted triples using
sentence-transformers. Embeddings enable semantic similarity comparison
so "treats headaches" matches "helps with headaches."

WHAT IS AN EMBEDDING?
An embedding converts a sentence into a list of numbers (a "vector") that captures
its MEANING. Similar sentences get similar numbers. For example:
  "treats headaches"  -> [0.12, -0.45, 0.78, ...]  (384 numbers)
  "helps with headaches" -> [0.11, -0.44, 0.79, ...]  (very similar numbers!)
  "the sky is blue"   -> [-0.33, 0.67, 0.01, ...]  (very different numbers)

Think of it like GPS coordinates for meaning -- sentences that mean similar things
end up near each other in this 384-dimensional space.

This phase reads all triples from the database, converts their sentences into
embeddings, and stores the embeddings back in the database as binary blobs.

Usage: python embed.py
"""

import sqlite3
import os

# NumPy (Numerical Python) is THE library for working with arrays of numbers efficiently.
# It's written in C under the hood, so operations on large arrays are extremely fast.
import numpy as np

from dotenv import load_dotenv
from rich.console import Console
from rich.progress import Progress

# Load environment variables from .env file
load_dotenv()

# Path to the SQLite database (same one extract.py wrote to)
DB_PATH = os.getenv("DB_PATH", "facts.db")

# Rich console for pretty terminal output
console = Console()


def run_embedding():
    """Load all triples without embeddings and generate them."""

    # Lazy import: SentenceTransformer is a heavy library (loads ML models into memory).
    # We import it here so the file can be imported quickly by other modules
    # without waiting for the ML model to load.
    from sentence_transformers import SentenceTransformer

    console.print("[bold]Phase 2: Embedding[/bold]")

    # Load the "all-MiniLM-L6-v2" model. This is a small but effective model that:
    #   - Produces 384-dimensional embeddings (each sentence becomes 384 numbers)
    #   - Is trained on over 1 billion sentence pairs
    #   - Is good enough for clustering similar sentences together
    # The first time you run this, it downloads the model (~80MB). After that, it's cached.
    #
    # GPU acceleration: if a CUDA GPU is available (e.g., RTX 3080 Ti), the model
    # runs on the GPU automatically, which is significantly faster for large batches.
    import torch
    device = "cuda" if torch.cuda.is_available() else "cpu"
    model = SentenceTransformer("all-MiniLM-L6-v2", device=device)
    console.print(f"  Model loaded: all-MiniLM-L6-v2 on [bold]{device.upper()}[/bold]")

    # Connect to the database where extract.py stored the triples
    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()

    # Only fetch triples that DON'T have embeddings yet.
    # This makes the script resume-safe: if you embedded 5000 triples and stopped,
    # running it again will only embed the remaining ones.
    # "embedding IS NULL" means the embedding column hasn't been filled in yet.
    c.execute("SELECT id, full_sentence FROM triples WHERE embedding IS NULL")
    rows = c.fetchall()  # Fetch all matching rows into memory as a list of tuples

    console.print(f"  Triples to embed: {len(rows)}")

    # Nothing to do if all triples already have embeddings
    if not rows:
        console.print("[yellow]No triples to embed.[/yellow]")
        return

    # Process in batches of 256 sentences at a time.
    # Batching is important because:
    #   1. The ML model is faster when processing many sentences at once (parallelism on CPU/GPU)
    #   2. We can commit to the database after each batch (checkpoint for crash safety)
    #   3. It keeps memory usage reasonable (don't load all 10,000+ embeddings at once)
    batch_size = 256
    total_embedded = 0

    # Show a progress bar while we work through all the batches
    with Progress() as progress:
        task = progress.add_task("Embedding triples...", total=len(rows))

        # range(0, len(rows), batch_size) generates: 0, 256, 512, 768, ...
        # This is how we step through the list in chunks of 256.
        for i in range(0, len(rows), batch_size):

            # Slice out the current batch. Python slicing: rows[0:256], rows[256:512], etc.
            # If there aren't enough items to fill a batch, it just takes what's left.
            batch = rows[i : i + batch_size]

            # Split the batch into two separate lists: one of IDs, one of sentences.
            # These are "list comprehensions" -- compact loops that build a list.
            # r[0] is the triple ID (first column), r[1] is the sentence (second column).
            ids = [r[0] for r in batch]
            sentences = [r[1] for r in batch]

            # THIS IS THE CORE ML OPERATION: convert sentences into number vectors.
            # model.encode() takes a list of strings and returns a 2D array where
            # each row is a 384-number vector representing one sentence's meaning.
            # normalize_embeddings=True makes all vectors unit length (magnitude = 1.0).
            # This is important because it lets us use simple dot product as a similarity
            # measure later. Without normalization, longer sentences would have "bigger"
            # vectors, which would skew similarity comparisons.
            # show_progress_bar=False prevents a nested progress bar (we have our own).
            embeddings = model.encode(sentences, normalize_embeddings=True, show_progress_bar=False)

            # Store each embedding in the database as a binary blob.
            # zip() pairs up IDs and embeddings: (id1, emb1), (id2, emb2), ...
            for triple_id, embedding in zip(ids, embeddings):

                # Convert the embedding to a bytes object for storage in SQLite.
                # .astype(np.float32) ensures consistent precision (32-bit floating point = 4 bytes per number).
                # .tobytes() serializes the array into raw bytes (384 numbers * 4 bytes = 1536 bytes per embedding).
                # SQLite doesn't have an array type, so we store it as a BLOB (Binary Large Object).
                blob = embedding.astype(np.float32).tobytes()

                # Update the triple's row in the database with its new embedding
                c.execute("UPDATE triples SET embedding = ? WHERE id = ?", (blob, triple_id))

            # Count how many we've done
            total_embedded += len(batch)

            # Save this batch's embeddings to disk (checkpoint for crash safety)
            conn.commit()

            # Move the progress bar forward
            progress.update(task, advance=len(batch))

    # Write a log entry recording that embedding is complete
    c.execute(
        "INSERT INTO processing_log (phase, status, triples_extracted, message) VALUES (?, ?, ?, ?)",
        ("embedding", "complete", total_embedded, f"Embedded {total_embedded} triples"),
    )
    conn.commit()
    conn.close()

    # Print the final summary
    console.print(f"\n[bold green]Embedding complete![/bold green]")
    console.print(f"  Triples embedded: {total_embedded}")


# Only run when executed directly (python embed.py), not when imported
if __name__ == "__main__":
    run_embedding()
