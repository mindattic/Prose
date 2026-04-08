"""
Phase 1: Extract Subject-Predicate-Object triples from all JSON entity files
using the Anthropic API. Each entity's description is parsed into atomic claims.

Usage: python extract.py [--limit 50] [--repo documents] [--dry-run]
"""
import json
import os
import sys
import time
import sqlite3
import glob
from pathlib import Path
from dotenv import load_dotenv
from rich.console import Console
from rich.progress import Progress

load_dotenv()

console = Console()

ANTHROPIC_API_KEY = os.getenv("ANTHROPIC_API_KEY", "")
DATA_DIR = os.getenv("DATA_DIR", "../../engine/data")
DB_PATH = os.getenv("DB_PATH", "truth.db")
BATCH_SIZE = int(os.getenv("BATCH_SIZE", "10"))

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

Return ONLY valid JSON array of objects with keys: subject, predicate, object, sentence
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
        # Skip non-entity files
        fname = os.path.basename(fp)
        if fname in ("subsidiary_index.json", "trivia.json", "tts_rules.json"):
            continue
        # Skip graph directory
        if "/graph/" in fp.replace("\\", "/"):
            continue
        files.append(fp)

    return sorted(files)


def extract_triples_via_api(entity_json, filepath):
    """Send entity to Claude API for SPO extraction. Returns list of triples."""
    import anthropic

    client = anthropic.Anthropic(api_key=ANTHROPIC_API_KEY)

    # Truncate very large descriptions to stay within token limits
    entity = json.loads(entity_json) if isinstance(entity_json, str) else entity_json
    desc = entity.get("description", entity.get("body", ""))
    if len(desc) > 3000:
        entity = {**entity, "description": desc[:3000] + "..."}

    user_content = json.dumps(entity, indent=2, ensure_ascii=False)

    max_retries = 3
    for attempt in range(max_retries):
        try:
            response = client.messages.create(
                model="claude-sonnet-4-20250514",
                max_tokens=2048,
                system=EXTRACTION_PROMPT,
                messages=[{"role": "user", "content": user_content}],
            )

            text = response.content[0].text.strip()
            # Strip markdown code fences if present
            if text.startswith("```"):
                text = text.split("\n", 1)[1]
                if text.endswith("```"):
                    text = text[:-3]
                text = text.strip()

            triples = json.loads(text)
            return triples if isinstance(triples, list) else []

        except json.JSONDecodeError:
            console.print(f"  [yellow]JSON parse error on attempt {attempt + 1}[/yellow]")
            continue
        except Exception as e:
            if "overloaded" in str(e).lower() or "rate" in str(e).lower():
                wait = (attempt + 1) * 10
                console.print(f"  [yellow]Rate limited, waiting {wait}s...[/yellow]")
                time.sleep(wait)
                continue
            console.print(f"  [red]API error: {e}[/red]")
            return []

    return []


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


def run_extraction(limit=None, repo=None, dry_run=False):
    """Main extraction loop."""
    from db_schema import init_db

    init_db()

    files = get_json_files(repo)
    if limit:
        files = files[:limit]

    console.print(f"[bold]Phase 1: Extraction[/bold]")
    console.print(f"  Files to process: {len(files)}")
    console.print(f"  Repo filter: {repo or 'all'}")
    console.print(f"  Dry run: {dry_run}")

    if dry_run:
        console.print("[yellow]Dry run — showing first 5 files that would be processed:[/yellow]")
        for f in files[:5]:
            console.print(f"  {f}")
        return

    conn = sqlite3.connect(DB_PATH)
    total_triples = 0

    with Progress() as progress:
        task = progress.add_task("Extracting triples...", total=len(files))

        for i, filepath in enumerate(files):
            try:
                with open(filepath, "r", encoding="utf-8") as f:
                    entity = json.load(f)

                entity_name = entity.get("name", entity.get("title", entity.get("quote", os.path.basename(filepath))))
                source_repo = Path(filepath).parent.name

                triples = extract_triples_via_api(entity, filepath)
                count = store_triples(conn, triples, filepath, source_repo, entity_name)
                total_triples += count

            except Exception as e:
                console.print(f"  [red]Error on {filepath}: {e}[/red]")

            progress.update(task, advance=1)

            # Rate limiting: pause between batches
            if (i + 1) % BATCH_SIZE == 0:
                time.sleep(1)

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
    args = parser.parse_args()

    run_extraction(limit=args.limit, repo=args.repo, dry_run=args.dry_run)
