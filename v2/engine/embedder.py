"""
Embedder — Chunks worldbuilding documents and embeds them into ChromaDB.

This is the foundation of the RAG layer. Every worldbuilding document is
split into overlapping chunks, embedded as vectors, and stored in a local
ChromaDB instance. The retriever queries this store to ground generation
in actual canon.

Usage:
    from engine.embedder import build_canon_index, get_collection
    build_canon_index()          # Full rebuild from worldbuilding/
    collection = get_collection() # Get the ChromaDB collection for queries
"""

import re
from pathlib import Path

import chromadb
from chromadb.config import Settings

from .config import (
    WORLDBUILDING_DIR,
    CHARACTERS_DIR,
    NARRATIVE_BIBLE,
    CHROMA_DIR,
    CANON_COLLECTION,
    CHUNK_SIZE_CHARS,
    CHUNK_OVERLAP_CHARS,
)


def _get_client() -> chromadb.ClientAPI:
    """Get or create the persistent ChromaDB client."""
    CHROMA_DIR.mkdir(parents=True, exist_ok=True)
    return chromadb.PersistentClient(
        path=str(CHROMA_DIR),
        settings=Settings(anonymized_telemetry=False),
    )


def get_collection() -> chromadb.Collection:
    """Get the canon collection. Assumes build_canon_index() has been run."""
    client = _get_client()
    return client.get_collection(name=CANON_COLLECTION)


def _chunk_text(text: str, source_path: str) -> list[dict]:
    """Split text into overlapping chunks with metadata."""
    # Clean up the text
    text = text.strip()
    if not text:
        return []

    chunks = []
    start = 0
    chunk_index = 0

    while start < len(text):
        end = start + CHUNK_SIZE_CHARS

        # Try to break at a paragraph boundary
        if end < len(text):
            # Look for paragraph break near the end
            para_break = text.rfind("\n\n", start + CHUNK_SIZE_CHARS // 2, end + 200)
            if para_break > start:
                end = para_break

        chunk_text = text[start:end].strip()
        if chunk_text:
            # Extract the nearest heading for context
            heading = _find_nearest_heading(text, start)

            chunks.append({
                "text": chunk_text,
                "source": source_path,
                "heading": heading,
                "chunk_index": chunk_index,
            })
            chunk_index += 1

        start = end - CHUNK_OVERLAP_CHARS
        if start <= 0 and chunk_index > 0:
            break

    return chunks


def _find_nearest_heading(text: str, position: int) -> str:
    """Find the nearest markdown heading before the given position."""
    # Search backwards from position for a heading line
    search_text = text[:position]
    headings = re.findall(r"^#{1,3}\s+(.+)$", search_text, re.MULTILINE)
    return headings[-1].strip() if headings else ""


def _gather_canon_files() -> list[Path]:
    """Collect all files that constitute canon."""
    files = []

    # Worldbuilding markdown files
    if WORLDBUILDING_DIR.exists():
        for p in sorted(WORLDBUILDING_DIR.glob("*.md")):
            if p.name.startswith("ARCHIVED_"):
                continue
            files.append(p)

    # Character YAML files
    if CHARACTERS_DIR.exists():
        for p in sorted(CHARACTERS_DIR.rglob("*.yaml")):
            files.append(p)

    # Narrative bible
    if NARRATIVE_BIBLE.exists():
        files.append(NARRATIVE_BIBLE)

    return files


def build_canon_index(verbose: bool = True) -> int:
    """
    Rebuild the entire canon vector index from scratch.

    Deletes the existing collection and re-embeds all worldbuilding documents.
    This is idempotent — safe to run any time canon changes.

    Returns the number of chunks indexed.
    """
    client = _get_client()

    # Delete existing collection if present
    try:
        client.delete_collection(name=CANON_COLLECTION)
    except Exception:
        pass

    # Create fresh collection with default embedding function
    # ChromaDB uses its built-in sentence-transformer by default
    collection = client.create_collection(
        name=CANON_COLLECTION,
        metadata={"description": "Street Samurai canon worldbuilding"},
    )

    files = _gather_canon_files()
    if verbose:
        print(f"Indexing {len(files)} canon files...")

    all_chunks = []
    for filepath in files:
        text = filepath.read_text(encoding="utf-8")
        rel_path = str(filepath.relative_to(filepath.parent.parent))
        chunks = _chunk_text(text, rel_path)
        all_chunks.extend(chunks)
        if verbose:
            print(f"  {rel_path}: {len(chunks)} chunks")

    if not all_chunks:
        if verbose:
            print("No chunks to index.")
        return 0

    # Batch insert into ChromaDB
    # ChromaDB handles embedding automatically via its default model
    batch_size = 100
    for i in range(0, len(all_chunks), batch_size):
        batch = all_chunks[i:i + batch_size]
        collection.add(
            ids=[f"chunk_{i + j}" for j in range(len(batch))],
            documents=[c["text"] for c in batch],
            metadatas=[
                {
                    "source": c["source"],
                    "heading": c["heading"],
                    "chunk_index": c["chunk_index"],
                }
                for c in batch
            ],
        )

    total = len(all_chunks)
    if verbose:
        print(f"\nIndexed {total} chunks from {len(files)} files.")
    return total
