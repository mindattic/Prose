"""
Phase 1: Extract Subject-Predicate-Object triples from all JSON entity files
using the Anthropic API. Each entity's description is parsed into atomic claims.

This is the SLOWEST phase because it sends every entity to Claude's API one at a time.
For 10,000+ files, this can take hours. But it's resume-safe -- if you stop and restart,
it picks up where it left off because it checks which files are already in the database.

The output goes into the "triples" table in the SQLite database. Each entity might
produce 3-20 triples depending on how much information it contains.

Usage: python extract.py [--limit 50] [--repo documents] [--dry-run]
"""

import json
import os
import sys
import time
import sqlite3
import glob       # glob lets you find files using wildcard patterns like "*.json"
from pathlib import Path  # Path provides a cleaner way to work with file paths than raw strings
from dotenv import load_dotenv

# Rich is a Python library for beautiful terminal output -- colored text, progress bars, tables
from rich.console import Console
from rich.progress import Progress

# Load environment variables from the .env file
load_dotenv()

# Console is the Rich library's main output object -- like a fancy version of print()
console = Console()

# Read configuration from environment variables, with sensible defaults.
# These can all be overridden by setting them in the .env file.
ANTHROPIC_API_KEY = os.getenv("ANTHROPIC_API_KEY", "")  # Your Claude API key for authentication
DATA_DIR = os.getenv("DATA_DIR", "../../engine/data")    # Where the JSON entity files live
DB_PATH = os.getenv("DB_PATH", "facts.db")               # Where to store extracted triples
BATCH_SIZE = int(os.getenv("BATCH_SIZE", "10"))           # How many files to process before pausing (rate limiting)

# This is the "system prompt" sent to Claude's API.
# It tells Claude exactly what role to play and what format to return.
# Think of it as detailed instructions for a very capable assistant.
# The prompt includes:
#   - Rules for what constitutes a valid triple
#   - How to normalize predicates (use simple verbs)
#   - What fields to extract from and which to skip
#   - The exact JSON format to return (no markdown, no explanation)
#   - A worked example so Claude knows what "good output" looks like
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

    # Convert the DATA_DIR string into a Path object for easier manipulation
    base = Path(DATA_DIR)

    # Build the glob pattern: either a specific repo subfolder or all subfolders
    if repo:
        # Only look in one specific subfolder (e.g., "documents/*.json")
        pattern = base / repo / "*.json"
    else:
        # The "**" wildcard means "any number of subdirectories"
        # So this matches data/weapons/gun.json, data/places/city.json, etc.
        pattern = base / "**" / "*.json"

    files = []
    # glob.glob() finds all files matching the wildcard pattern.
    # recursive=True enables the ** wildcard to descend into subdirectories.
    for fp in glob.glob(str(pattern), recursive=True):

        # Skip known non-entity files that happen to be JSON but aren't worldbuilding entities.
        # These are config/metadata files that would produce garbage triples.
        fname = os.path.basename(fp)  # Get just the filename without the directory path
        if fname in ("subsidiary_index.json", "trivia.json", "tts_rules.json"):
            continue

        # Skip the graph directory entirely -- it contains processed output, not source entities.
        # We normalize backslashes to forward slashes so this works on both Windows and Linux.
        if "/graph/" in fp.replace("\\", "/"):
            continue

        files.append(fp)

    # Return files in a consistent alphabetical order (sorted() returns a new sorted list)
    return sorted(files)


def extract_triples_via_api(entity_json, filepath):
    """Send entity to Claude API for SPO extraction. Returns list of triples."""

    # Import the Anthropic SDK inside the function (not at the top of the file).
    # This is called a "lazy import" -- it means the heavy library only loads
    # when this function is actually called, not when the file is first imported.
    import anthropic

    # Create an API client with our key. This object handles authentication,
    # HTTP requests, retries, and response parsing.
    client = anthropic.Anthropic(api_key=ANTHROPIC_API_KEY)

    # Handle both cases: entity_json might be a JSON string or already a Python dict.
    # isinstance() checks the type of a variable.
    # json.loads() converts a JSON string into a Python dictionary.
    entity = json.loads(entity_json) if isinstance(entity_json, str) else entity_json

    # Get the description field (or "body" as a fallback -- some entities use different field names)
    desc = entity.get("description", entity.get("body", ""))

    # Truncate very long descriptions to avoid hitting the API's token (word) limit.
    # The {**entity, "description": ...} syntax creates a COPY of the dict with one key overridden.
    # The ** operator "unpacks" all key-value pairs from entity, then we override "description".
    if len(desc) > 3000:
        entity = {**entity, "description": desc[:3000] + "..."}

    # Convert the entity dict back into a nicely-formatted JSON string to send to the API.
    # indent=2 makes it human-readable; ensure_ascii=False preserves special characters.
    user_content = json.dumps(entity, indent=2, ensure_ascii=False)

    # Retry up to 3 times if something goes wrong (network errors, rate limits, etc.)
    max_retries = 3
    for attempt in range(max_retries):
        try:
            # This is the actual API call to Claude. It sends our entity as a "user" message
            # with the extraction instructions as the "system" prompt.
            # model: which Claude model to use (Sonnet is fast and cheap, good for bulk work)
            # max_tokens: maximum length of Claude's response (2048 tokens is roughly 1500 words)
            # system: the instruction prompt that tells Claude what to do
            # messages: the conversation history (just one user message here)
            response = client.messages.create(
                model="claude-sonnet-4-20250514",
                max_tokens=2048,
                system=EXTRACTION_PROMPT,
                messages=[{"role": "user", "content": user_content}],
            )

            # Extract the text from Claude's response.
            # response.content is a list of content blocks; we want the first one's text.
            # .strip() removes leading/trailing whitespace.
            text = response.content[0].text.strip()

            # Sometimes Claude wraps its JSON in markdown code fences (```json ... ```)
            # even though we told it not to. This block strips those fences to get pure JSON.
            if "```" in text:
                # Use regex to find a JSON array (starts with [ and ends with ]) anywhere in the text.
                # re.search() scans through the string looking for the first match.
                # [\s\S]* matches any character including newlines (. doesn't match newlines by default).
                import re
                json_match = re.search(r'\[[\s\S]*\]', text)
                if json_match:
                    # .group(0) returns the full matched text
                    text = json_match.group(0)
                else:
                    # Fallback: just remove the code fence markers and hope for the best
                    text = text.replace("```json", "").replace("```", "").strip()

            # If the response doesn't start with "[", it might have explanatory text before the JSON.
            # Try to find and extract just the JSON array.
            if not text.startswith("["):
                import re
                json_match = re.search(r'\[[\s\S]*\]', text)
                if json_match:
                    text = json_match.group(0)

            # Parse the JSON string into a Python list of dictionaries.
            # json.loads() is the opposite of json.dumps() -- it converts JSON text to Python objects.
            triples = json.loads(text)

            # Verify we got a list (not a dict or other type) and return it.
            # If it's not a list, return an empty list as a safe fallback.
            return triples if isinstance(triples, list) else []

        except json.JSONDecodeError:
            # The response wasn't valid JSON. This happens when Claude returns malformed output.
            # Try one more time with a more lenient regex pattern to salvage partial JSON.
            try:
                import re
                # [\s\S]*? is the "non-greedy" version -- it matches as FEW characters as possible,
                # which helps when there are multiple [] groups (we want the first complete array)
                json_match = re.search(r'\[[\s\S]*?\]', text)
                if json_match:
                    triples = json.loads(json_match.group(0))
                    return triples if isinstance(triples, list) else []
            except:
                # bare except catches ALL exceptions -- normally bad practice, but here we're
                # just trying to salvage data and will retry or give up gracefully
                pass

            # If we still have retries left, try again; otherwise give up
            if attempt < max_retries - 1:
                continue
            return []  # give up silently after all retries

        except Exception as e:
            # Catch any other error (network timeout, API overload, etc.)
            # Check if it's a rate limit or overload error -- these are temporary and worth retrying
            if "overloaded" in str(e).lower() or "rate" in str(e).lower():
                # Exponential backoff: wait longer with each retry (10s, 20s, 30s)
                wait = (attempt + 1) * 10
                console.print(f"  [yellow]Rate limited, waiting {wait}s...[/yellow]")
                time.sleep(wait)
                continue
            # For other errors, print the error and give up (no point retrying)
            console.print(f"  [red]API error: {e}[/red]")
            return []

    # If we exhausted all retries without returning, return empty list
    return []


def store_triples(conn, triples, source_file, source_repo, entity_name):
    """Store extracted triples in the database."""

    # Get a cursor to execute SQL commands on this connection
    c = conn.cursor()
    count = 0

    # Iterate through each triple Claude extracted
    for t in triples:
        # Validate that the triple has all required keys before inserting.
        # all() returns True only if EVERY item in the iterable is True.
        # "k in t" checks if the key exists in the dictionary.
        # This is a "generator expression" inside all() -- it checks each key one at a time.
        if not all(k in t for k in ("subject", "predicate", "object", "sentence")):
            continue  # Skip malformed triples that are missing required fields

        # Insert the triple into the triples table.
        # The ? placeholders prevent SQL injection (a security attack where someone puts
        # malicious SQL in the data). SQLite safely escapes the values for us.
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

    # Save all the inserts to disk
    conn.commit()
    return count


def run_extraction(limit=None, repo=None, dry_run=False):
    """Main extraction loop."""

    # Initialize the database (create tables if they don't exist)
    from db_schema import init_db
    init_db()

    # Get the list of JSON files to process
    files = get_json_files(repo)

    # If the user set a limit, only process that many files (useful for testing)
    # This is Python's "slice" syntax: files[:50] means "the first 50 items"
    if limit:
        files = files[:limit]

    # Print a summary of what we're about to do using Rich's styled output
    # [bold] and [yellow] are Rich markup tags for text styling
    console.print(f"[bold]Phase 1: Extraction[/bold]")
    console.print(f"  Files to process: {len(files)}")
    console.print(f"  Repo filter: {repo or 'all'}")  # "repo or 'all'" means: use repo if truthy, else 'all'
    console.print(f"  Dry run: {dry_run}")

    # Dry run mode: just show what WOULD happen without actually calling the API
    if dry_run:
        console.print("[yellow]Dry run — showing first 5 files that would be processed:[/yellow]")
        for f in files[:5]:
            console.print(f"  {f}")
        return  # Exit early -- don't process anything

    # Open a database connection that will be shared across all file processing
    conn = sqlite3.connect(DB_PATH)
    total_triples = 0

    # RESUME SUPPORT: Check which files have already been processed in a previous run.
    # This is crucial for a pipeline that takes hours -- if it crashes at file 5000,
    # you don't want to re-process files 1-4999 when you restart.
    c = conn.cursor()
    c.execute("SELECT DISTINCT source_file FROM triples")

    # Build a set of already-processed filenames for O(1) lookup speed.
    # A set is like a list but much faster for "is X in this collection?" checks.
    # {row[0] for row in c.fetchall()} is a "set comprehension" -- it builds a set
    # from the first column of each database row.
    already_processed = {row[0] for row in c.fetchall()}

    # Filter out files we've already processed.
    # This is a "list comprehension" -- a compact way to build a new list by filtering another.
    # It checks both the raw path and the absolute path in case they were stored differently.
    remaining = [f for f in files if f not in already_processed and os.path.abspath(f) not in already_processed]

    # Tell the user how many files we're skipping
    if len(remaining) < len(files):
        skipped = len(files) - len(remaining)
        console.print(f"  [green]Resuming: {skipped} files already processed, {len(remaining)} remaining[/green]")

    # Progress() creates a Rich progress bar that shows how far along we are.
    # "with" is a Python context manager -- it ensures the progress bar is properly
    # cleaned up (removed from the screen) when we're done, even if an error occurs.
    with Progress() as progress:
        # Create a named task in the progress bar with the total number of items
        task = progress.add_task("Extracting triples...", total=len(remaining))

        # enumerate() gives us both the index (i) and the value (filepath) as we iterate.
        # This is useful for knowing "how many have we processed so far?"
        for i, filepath in enumerate(remaining):
            try:
                # Open the JSON file and parse it into a Python dictionary.
                # encoding="utf-8" ensures special characters (accents, symbols) are read correctly.
                # "with open(...) as f" is a context manager that automatically closes the file
                # when we're done, even if json.load() raises an error.
                with open(filepath, "r", encoding="utf-8") as f:
                    entity = json.load(f)

                # Try to find a human-readable name for this entity.
                # .get() returns the value if the key exists, or the default (second argument) if not.
                # This chains three attempts: name -> title -> quote -> filename as last resort.
                entity_name = entity.get("name", entity.get("title", entity.get("quote", os.path.basename(filepath))))

                # Get the repo name from the parent directory (e.g., "weapons" from "data/weapons/gun.json")
                source_repo = Path(filepath).parent.name

                # Send this entity to Claude's API and get back a list of SPO triples
                triples = extract_triples_via_api(entity, filepath)

                # Store the extracted triples in the database and count how many were valid
                count = store_triples(conn, triples, filepath, source_repo, entity_name)
                total_triples += count

            except Exception as e:
                # If anything goes wrong with one file, log the error and continue to the next.
                # We don't want one bad file to stop the entire multi-hour pipeline.
                console.print(f"  [red]Error on {filepath}: {e}[/red]")

            # Move the progress bar forward by one item
            progress.update(task, advance=1)

            # Rate limiting: pause for 1 second after every BATCH_SIZE files.
            # This prevents us from overwhelming the Claude API with too many requests.
            # The % operator is "modulo" (remainder after division).
            # (i+1) % 10 == 0 is True when i+1 is 10, 20, 30, etc.
            if (i + 1) % BATCH_SIZE == 0:
                time.sleep(1)

            # Checkpoint: save to database every 50 files.
            # This ensures that if the script crashes, we don't lose more than 50 files of work.
            # Without this, all inserts since the last commit() would be lost.
            if (i + 1) % 50 == 0:
                conn.commit()

            # Print a progress message every 100 files so the user knows things are moving
            if (i + 1) % 100 == 0:
                from datetime import datetime
                ts = datetime.now().strftime("%Y-%m-%d %I:%M:%S%p")  # e.g., "2026-04-06 02:30:45PM"
                print(f"{ts}    {i + 1}/{len(remaining)} entries processed... ({total_triples} triples extracted)")

    # Write a completion record to the processing_log table.
    # This is a journal entry that says "extraction finished at this time with these results."
    c = conn.cursor()
    c.execute(
        "INSERT INTO processing_log (phase, status, files_processed, triples_extracted, message) VALUES (?, ?, ?, ?, ?)",
        ("extraction", "complete", len(files), total_triples, f"Processed {len(files)} files, extracted {total_triples} triples"),
    )
    conn.commit()
    conn.close()

    # Print the final summary
    console.print(f"\n[bold green]Extraction complete![/bold green]")
    console.print(f"  Files processed: {len(files)}")
    console.print(f"  Triples extracted: {total_triples}")


# This block runs only when you execute "python extract.py" directly.
# It sets up command-line argument parsing so you can customize the run.
if __name__ == "__main__":
    import argparse

    # argparse is Python's built-in library for parsing command-line arguments.
    # It automatically generates --help output and validates user input.
    parser = argparse.ArgumentParser(description="Extract SPO triples from entity files")

    # Each add_argument() defines one command-line flag:
    #   --limit 50       -> only process 50 files (for testing)
    #   --repo documents -> only process the "documents" subfolder
    #   --dry-run        -> show what would happen without calling the API
    parser.add_argument("--limit", type=int, help="Limit number of files to process")
    parser.add_argument("--repo", type=str, help="Only process files from this repo (e.g. 'documents')")
    parser.add_argument("--dry-run", action="store_true", help="Show what would be processed without calling API")

    # parse_args() reads sys.argv (the command-line arguments) and returns a namespace object
    # where each argument is an attribute (e.g., args.limit, args.repo, args.dry_run)
    args = parser.parse_args()

    run_extraction(limit=args.limit, repo=args.repo, dry_run=args.dry_run)
