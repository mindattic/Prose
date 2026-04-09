"""
Phase 1: Extract Subject-Predicate-Object triples from all JSON entity files
using the Anthropic API. Each entity's description is parsed into atomic claims.

This is the SLOWEST phase because it sends every entity to Claude's API.
For 10,000+ files, this can take hours. But it's resume-safe -- if you stop and restart,
it picks up where it left off because it checks which files are already in the database.

ASYNC CONCURRENCY: Makes multiple API calls simultaneously (default 20 at a time)
instead of one-by-one, cutting runtime dramatically.

The output goes into the "triples" table in the SQLite database. Each entity might
produce 3-20 triples depending on how much information it contains.

Usage: python extract.py [--limit 50] [--repo documents] [--dry-run] [--concurrency 20]
"""

import json
import os
import sys
import re
import asyncio
import sqlite3
import glob
from pathlib import Path
from dotenv import load_dotenv
from rich.console import Console
from rich.progress import Progress

load_dotenv()

console = Console()

# Configuration from environment variables
ANTHROPIC_API_KEY = os.getenv("ANTHROPIC_API_KEY", "")
DATA_DIR = os.getenv("DATA_DIR", "../../engine/data")
DB_PATH = os.getenv("DB_PATH", "facts.db")
BATCH_SIZE = int(os.getenv("BATCH_SIZE", "10"))

# NEW: Concurrency and model settings
# CONCURRENCY controls how many API calls run simultaneously.
# 20 is a safe default for most Anthropic API tiers.
CONCURRENCY = int(os.getenv("CONCURRENCY", "20"))

# Haiku is fast and cheap -- more than enough for structured triple extraction.
MODEL = os.getenv("MODEL", "claude-haiku-4-5-20251001")

# System prompt sent to Claude's API -- tells it exactly what to extract and how.
EXTRACTION_PROMPT = """You are a fact extraction engine. Given a JSON entity from a worldbuilding database, extract every factual claim as a Subject-Predicate-Object triple.

Rules:
- Each triple must be an atomic, verifiable claim
- Subject: the entity or thing being described
- Predicate: the relationship or property (normalize to simple verbs: "is", "has", "located_in", "manufactured_by", "costs", "used_by", etc.)
- Object: the value, target, or attribute
- Extract from: name, description, cultural_context, specifications, and any other text fields
- Skip: id, tags, type (metadata fields)
- If a description contains multiple facts, extract each as a separate triple
- Normalize numbers and units consistently

CRITICAL: Return ONLY a raw JSON array. No markdown, no explanation, no code fences.
Each object has keys: subject, predicate, object, sentence
The "sentence" is the natural language form: "Subject predicate object"

Example input: {"name": "Hearthstone HM-7", "type": "weapon", "category": "pistol", "manufacturer": "HEARTHSTONE FIREARMS", "tier_availability": "Tier 2+", "description": "A reliable mid-range sidearm popular among Circuit workers."}

Example output:
[
  {"subject": "Hearthstone HM-7", "predicate": "is_a", "object": "pistol", "sentence": "Hearthstone HM-7 is a pistol"},
  {"subject": "Hearthstone HM-7", "predicate": "manufactured_by", "object": "Hearthstone Firearms", "sentence": "Hearthstone HM-7 is manufactured by Hearthstone Firearms"},
  {"subject": "Hearthstone HM-7", "predicate": "available_at", "object": "Tier 2+", "sentence": "Hearthstone HM-7 is available at Tier 2+"},
  {"subject": "Hearthstone HM-7", "predicate": "popular_among", "object": "Circuit workers", "sentence": "Hearthstone HM-7 is popular among Circuit workers"}
]"""


def get_json_files(repo=None):
    """Get all JSON entity files, optionally filtered by repo."""

    base = Path(DATA_DIR)

    if repo:
        pattern = base / repo / "*.json"
    else:
        pattern = base / "**" / "*.json"

    files = []
    for fp in glob.glob(str(pattern), recursive=True):
        fname = os.path.basename(fp)
        if fname in ("subsidiary_index.json", "trivia.json", "tts_rules.json"):
            continue
        if "/graph/" in fp.replace("\\", "/"):
            continue
        files.append(fp)

    return sorted(files)


def parse_triple_json(text):
    """Parse Claude's response text into a list of triple dicts.
    Handles markdown fences, preamble text, and malformed JSON gracefully."""

    # Strip markdown code fences if present
    if "```" in text:
        json_match = re.search(r'\[[\s\S]*\]', text)
        if json_match:
            text = json_match.group(0)
        else:
            text = text.replace("```json", "").replace("```", "").strip()

    # If response doesn't start with "[", try to find the JSON array
    if not text.startswith("["):
        json_match = re.search(r'\[[\s\S]*\]', text)
        if json_match:
            text = json_match.group(0)

    try:
        triples = json.loads(text)
        return triples if isinstance(triples, list) else []
    except json.JSONDecodeError:
        # Try non-greedy match as last resort
        try:
            json_match = re.search(r'\[[\s\S]*?\]', text)
            if json_match:
                triples = json.loads(json_match.group(0))
                return triples if isinstance(triples, list) else []
        except Exception:
            pass
        return []


async def extract_triples_via_api(entity, filepath, client, semaphore):
    """Send entity to Claude API for SPO extraction (async with concurrency limit)."""

    # The semaphore limits how many API calls run at the same time.
    # async with semaphore: blocks here if CONCURRENCY calls are already in flight,
    # and proceeds as soon as one of them finishes.
    async with semaphore:
        desc = entity.get("description", entity.get("body", ""))
        if len(desc) > 3000:
            entity = {**entity, "description": desc[:3000] + "..."}

        user_content = json.dumps(entity, indent=2, ensure_ascii=False)

        max_retries = 3
        for attempt in range(max_retries):
            try:
                response = await client.messages.create(
                    model=MODEL,
                    max_tokens=2048,
                    system=EXTRACTION_PROMPT,
                    messages=[{"role": "user", "content": user_content}],
                )

                text = response.content[0].text.strip()
                return parse_triple_json(text)

            except json.JSONDecodeError:
                if attempt < max_retries - 1:
                    continue
                return []

            except Exception as e:
                if "overloaded" in str(e).lower() or "rate" in str(e).lower():
                    wait = (attempt + 1) * 10
                    console.print(f"  [yellow]Rate limited, waiting {wait}s...[/yellow]")
                    await asyncio.sleep(wait)
                    continue
                console.print(f"  [red]API error on {os.path.basename(filepath)}: {e}[/red]")
                return []

        return []


async def process_file(filepath, client, semaphore):
    """Read a JSON entity file and extract triples from it (async)."""
    try:
        with open(filepath, "r", encoding="utf-8") as f:
            entity = json.load(f)

        # Skip files that contain arrays instead of objects (e.g., motifs.json, validation.json, events.json)
        if not isinstance(entity, dict):
            return filepath, Path(filepath).parent.name, os.path.basename(filepath), []

        entity_name = entity.get("name", entity.get("title", entity.get("quote", os.path.basename(filepath))))
        source_repo = Path(filepath).parent.name

        triples = await extract_triples_via_api(entity, filepath, client, semaphore)
        return filepath, source_repo, entity_name, triples

    except Exception as e:
        console.print(f"  [red]Error on {filepath}: {e}[/red]")
        return filepath, Path(filepath).parent.name, os.path.basename(filepath), []


def store_triples(conn, triples, source_file, source_repo, entity_name):
    """Store extracted triples in the database."""

    c = conn.cursor()
    count = 0

    for t in triples:
        if not all(k in t for k in ("subject", "predicate", "object", "sentence")):
            continue

        c.execute(
            """INSERT INTO triples (source_file, source_repo, entity_name, subject, predicate, object, full_sentence)
               VALUES (?, ?, ?, ?, ?, ?, ?)""",
            (
                source_file,
                source_repo,
                entity_name,
                t["subject"],
                t["predicate"],
                t["object"],
                t["sentence"],
            ),
        )
        count += 1

    conn.commit()
    return count


def run_extraction(limit=None, repo=None, dry_run=False, concurrency=None):
    """Main extraction entry point. Runs the async implementation internally."""
    asyncio.run(_run_extraction_async(limit, repo, dry_run, concurrency))


async def _run_extraction_async(limit=None, repo=None, dry_run=False, concurrency=None):
    """Async extraction with concurrent API calls."""
    import anthropic

    from db_schema import init_db
    init_db()

    files = get_json_files(repo)
    if limit:
        files = files[:limit]

    actual_concurrency = concurrency or CONCURRENCY

    console.print(f"[bold]Phase 1: Extraction[/bold]")
    console.print(f"  Files to process: {len(files)}")
    console.print(f"  Repo filter: {repo or 'all'}")
    console.print(f"  Model: {MODEL}")
    console.print(f"  Concurrency: {actual_concurrency} parallel requests")
    console.print(f"  Dry run: {dry_run}")

    if dry_run:
        console.print("[yellow]Dry run — showing first 5 files that would be processed:[/yellow]")
        for f in files[:5]:
            console.print(f"  {f}")
        return

    conn = sqlite3.connect(DB_PATH)
    total_triples = 0

    # Resume support: skip files already in the database
    c = conn.cursor()
    c.execute("SELECT DISTINCT source_file FROM triples")
    already_processed = {row[0] for row in c.fetchall()}
    remaining = [f for f in files if f not in already_processed and os.path.abspath(f) not in already_processed]

    if len(remaining) < len(files):
        skipped = len(files) - len(remaining)
        console.print(f"  [green]Resuming: {skipped} files already processed, {len(remaining)} remaining[/green]")

    if not remaining:
        console.print("[yellow]All files already processed.[/yellow]")
        conn.close()
        return

    # Create async API client and concurrency limiter
    client = anthropic.AsyncAnthropic(api_key=ANTHROPIC_API_KEY)
    semaphore = asyncio.Semaphore(actual_concurrency)

    # Process in checkpoint batches of 50 files.
    # Within each batch, up to CONCURRENCY API calls run simultaneously.
    # After each batch completes, results are committed to the database.
    checkpoint_size = 50

    with Progress() as progress:
        task = progress.add_task("Extracting triples...", total=len(remaining))
        files_done = 0

        for i in range(0, len(remaining), checkpoint_size):
            batch_files = remaining[i : i + checkpoint_size]

            # Launch all files in this checkpoint batch concurrently.
            # The semaphore inside process_file limits actual parallelism.
            coros = [process_file(fp, client, semaphore) for fp in batch_files]

            # as_completed yields results as each file finishes (not in order).
            # This keeps the progress bar updating in real time.
            for coro in asyncio.as_completed(coros):
                filepath, source_repo, entity_name, triples = await coro
                if triples:
                    count = store_triples(conn, triples, filepath, source_repo, entity_name)
                    total_triples += count
                progress.update(task, advance=1)

            # Checkpoint: commit all inserts from this batch to disk
            conn.commit()
            files_done += len(batch_files)

            # Progress message every ~100 files
            if files_done % 100 < checkpoint_size:
                from datetime import datetime
                ts = datetime.now().strftime("%Y-%m-%d %I:%M:%S%p")
                print(f"{ts}    {files_done}/{len(remaining)} entries processed... ({total_triples} triples extracted)")

    # Log completion
    c = conn.cursor()
    c.execute(
        "INSERT INTO processing_log (phase, status, files_processed, triples_extracted, message) VALUES (?, ?, ?, ?, ?)",
        ("extraction", "complete", len(files), total_triples, f"Processed {len(files)} files, extracted {total_triples} triples"),
    )
    conn.commit()
    conn.close()

    console.print(f"\n[bold green]Extraction complete![/bold green]")
    console.print(f"  Files processed: {len(files)}")
    console.print(f"  Triples extracted: {total_triples}")


if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(description="Extract SPO triples from entity files")
    parser.add_argument("--limit", type=int, help="Limit number of files to process")
    parser.add_argument("--repo", type=str, help="Only process files from this repo (e.g. 'documents')")
    parser.add_argument("--dry-run", action="store_true", help="Show what would be processed without calling API")
    parser.add_argument("--concurrency", type=int, help=f"Number of parallel API calls (default: {CONCURRENCY})")

    args = parser.parse_args()

    run_extraction(limit=args.limit, repo=args.repo, dry_run=args.dry_run, concurrency=args.concurrency)
