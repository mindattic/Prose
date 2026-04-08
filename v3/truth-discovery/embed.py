"""
Phase 2: Generate vector embeddings for all extracted triples using
sentence-transformers. Embeddings enable semantic similarity comparison
so "treats headaches" matches "helps with headaches."

Usage: python embed.py
"""
import sqlite3
import os
import numpy as np
from dotenv import load_dotenv
from rich.console import Console
from rich.progress import Progress

load_dotenv()

DB_PATH = os.getenv("DB_PATH", "facts.db")

console = Console()


def run_embedding():
    """Load all triples without embeddings and generate them."""
    from sentence_transformers import SentenceTransformer

    console.print("[bold]Phase 2: Embedding[/bold]")

    model = SentenceTransformer("all-MiniLM-L6-v2")
    console.print(f"  Model loaded: all-MiniLM-L6-v2")

    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()

    # Get triples without embeddings
    c.execute("SELECT id, full_sentence FROM triples WHERE embedding IS NULL")
    rows = c.fetchall()

    console.print(f"  Triples to embed: {len(rows)}")

    if not rows:
        console.print("[yellow]No triples to embed.[/yellow]")
        return

    batch_size = 256
    total_embedded = 0

    with Progress() as progress:
        task = progress.add_task("Embedding triples...", total=len(rows))

        for i in range(0, len(rows), batch_size):
            batch = rows[i : i + batch_size]
            ids = [r[0] for r in batch]
            sentences = [r[1] for r in batch]

            # Generate embeddings
            embeddings = model.encode(sentences, normalize_embeddings=True, show_progress_bar=False)

            # Store as binary blobs
            for triple_id, embedding in zip(ids, embeddings):
                blob = embedding.astype(np.float32).tobytes()
                c.execute("UPDATE triples SET embedding = ? WHERE id = ?", (blob, triple_id))

            total_embedded += len(batch)
            conn.commit()
            progress.update(task, advance=len(batch))

    # Log
    c.execute(
        "INSERT INTO processing_log (phase, status, triples_extracted, message) VALUES (?, ?, ?, ?)",
        ("embedding", "complete", total_embedded, f"Embedded {total_embedded} triples"),
    )
    conn.commit()
    conn.close()

    console.print(f"\n[bold green]Embedding complete![/bold green]")
    console.print(f"  Triples embedded: {total_embedded}")


if __name__ == "__main__":
    run_embedding()
