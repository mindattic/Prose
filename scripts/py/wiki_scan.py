"""
wiki_scan.py — StreetSamurai Wiki Link Scanner

Builds an index of all named entities, then scans every entity's text fields
for unlinked mentions. Inserts [[Entity Name]] (or [[Entity Name|display text]])
syntax so the Blazor WikiText renderer can turn them into hyperlinks.

Uses the same word-boundary technique as Wikipedia's link detection:
  - Only matches whole words (won't link "Axiom" inside "Axiomatic")
  - Skips text already wrapped in [[...]] brackets
  - Multi-word names are matched longest-first to avoid partial overlaps

USAGE
-----
  python wiki_scan.py [options]

OPTIONS
-------
  --dry-run         Print what would be linked without modifying files (default)
  --apply           Write [[...]] syntax back to entity JSON files
  --interactive     Prompt before each insertion; 'y' accept, 'n' skip, 'q' quit
  --partial         Also report partial/fuzzy matches (name appears inside compound word)
  --repo REPO       Limit scan to one repo folder (e.g. --repo people)
  --min-length N    Minimum entity name length to match (default: 4)
  --data-dir DIR    Override engine/data path (default: ../../engine/data)
  --help, -h        Show this help text and exit

EXAMPLES
--------
  # Preview what would be linked across all entities
  python wiki_scan.py --dry-run

  # Apply links to a single repo, reviewing each one
  python wiki_scan.py --apply --interactive --repo people

  # Apply all exact matches automatically
  python wiki_scan.py --apply

  # Show fuzzy/partial matches for manual review
  python wiki_scan.py --partial

NOTES
-----
  - Existing [[...]] links are never double-linked
  - Entity names shorter than --min-length are skipped (avoids noise like "AI")
  - The script is safe to re-run; it won't re-link already-linked text
  - Aliases listed in entity JSON are also indexed for matching
"""

import argparse
import json
import os
import re
import sys
from pathlib import Path
from typing import Optional

# Allow running from scripts/py/ or from project root
sys.path.insert(0, str(Path(__file__).parent))
try:
    from constants import DATA_DIR, REPOS, REPO_ROUTES, WIKI_SCAN_FIELDS, WIKI_MIN_NAME_LENGTH, WIKI_SCAN_SKIP_REPOS
except ImportError:
    DATA_DIR = "../../engine/data"
    REPOS = [
        "people", "synthetics", "corponations", "places", "factions", "weaponry",
        "equipment", "technology", "cyberware", "apparel", "automata",
        "genemods", "materials", "transportation", "pharmaceuticals", "documents",
    ]
    REPO_ROUTES = {}
    WIKI_SCAN_FIELDS = [
        "description", "body", "background", "personality", "ideology",
        "founding_story", "key_detail", "cultural_context", "lore",
        "notes", "history", "functionality", "common_usage",
    ]
    WIKI_SCAN_SKIP_REPOS: set = {"materials", "archetypes"}
    WIKI_MIN_NAME_LENGTH = 4


WIKI_LINK_RE = re.compile(r'\[\[([^\]|]+)(?:\|([^\]]+))?\]\]')


def resolve_data_dir(override: Optional[str] = None) -> Path:
    base = override or DATA_DIR
    # Try relative to script location, then CWD
    candidates = [
        Path(__file__).parent / base,
        Path(base),
        Path.cwd() / base,
    ]
    for c in candidates:
        if c.exists():
            return c.resolve()
    raise FileNotFoundError(f"Cannot find data directory: {base!r}\n"
                            f"  Tried: {[str(c) for c in candidates]}\n"
                            f"  Set DATA_DIR env var or use --data-dir")


def build_index(data_dir: Path, min_length: int) -> dict[str, dict]:
    """Return {lowercased_name: {name, id, repo, route}} for all entities."""
    index: dict[str, dict] = {}

    for repo in REPOS:
        if repo in WIKI_SCAN_SKIP_REPOS:
            continue  # Too generic to auto-link (e.g. materials, archetypes)
        repo_dir = data_dir / repo
        if not repo_dir.exists():
            continue
        route = REPO_ROUTES.get(repo, f"/{repo}")

        for jf in repo_dir.glob("*.json"):
            try:
                data = json.loads(jf.read_text(encoding="utf-8"))
            except Exception:
                continue

            entity_id = data.get("id", "")
            name = data.get("name") or data.get("title") or ""
            if not name or len(name) < min_length:
                continue

            entry = {"name": name, "id": entity_id, "repo": repo, "route": route}
            index[name.lower()] = entry

            # Index aliases too
            for alias in data.get("aliases", []):
                if isinstance(alias, str) and len(alias) >= min_length:
                    key = alias.lower()
                    if key not in index:
                        index[key] = {**entry, "alias_of": name, "display_name": alias}

    return index


def extract_text_fields(data: dict) -> list[tuple[str, str]]:
    """Yield (field_name, text) for all string fields worth scanning."""
    results = []
    for field in WIKI_SCAN_FIELDS:
        val = data.get(field)
        if isinstance(val, str) and val.strip():
            results.append((field, val))
    return results


def already_linked(text: str) -> set[str]:
    """Return lowercase set of names already inside [[...]] in this text."""
    return {m.group(1).lower() for m in WIKI_LINK_RE.finditer(text)}


def find_matches(text: str, index: dict, partial: bool) -> list[tuple]:
    """Return list of (start, end, entity_entry, match_text) sorted by position.
    Longest names are matched first to avoid partial overlaps."""
    linked = already_linked(text)
    # Sort by name length descending so longest match wins
    candidates = sorted(index.items(), key=lambda kv: len(kv[0]), reverse=True)

    matches = []
    covered_spans: list[tuple[int, int]] = []

    for name_lower, entry in candidates:
        canonical = entry["name"]
        if name_lower in linked:
            continue  # Already linked

        if partial:
            # Partial/fuzzy preview — case-insensitive, no word boundary required
            pattern = re.compile(re.escape(canonical), re.IGNORECASE)
        else:
            # Exact match — case-sensitive so "face" ≠ "Face" (the archetype)
            # Word boundary: not preceded/followed by [ or word char
            pattern = re.compile(r'(?<![\[\w])' + re.escape(canonical) + r'(?![\w\]])')

        for m in pattern.finditer(text):
            start, end = m.start(), m.end()
            # Don't overlap with already-matched spans
            if any(s <= start < e or s < end <= e for s, e in covered_spans):
                continue
            # Don't match inside existing [[...]]
            pre = text[:start]
            opens = pre.count('[[') - pre.count(']]')
            if opens > 0:
                continue
            covered_spans.append((start, end))
            matches.append((start, end, entry, m.group(0)))

    return sorted(matches, key=lambda t: t[0])


def apply_links(text: str, matches: list[tuple]) -> str:
    """Replace matched spans with [[Name]] or [[Name|display]] syntax."""
    if not matches:
        return text

    result = []
    prev = 0
    for start, end, entry, matched_text in sorted(matches, key=lambda t: t[0]):
        result.append(text[prev:start])
        canonical = entry["name"]
        if matched_text.lower() == canonical.lower():
            result.append(f"[[{canonical}]]")
        else:
            result.append(f"[[{canonical}|{matched_text}]]")
        prev = end
    result.append(text[prev:])
    return "".join(result)


def confirm_match(entry: dict, matched_text: str, field: str, context: str) -> bool:
    """Prompt user to confirm a single link insertion. Returns True to accept."""
    canonical = entry["name"]
    route = entry["route"] + "?id=" + entry["id"]
    print(f"\n  Found: {matched_text!r} → [[{canonical}]]  ({entry['repo']})")
    print(f"  Route: {route}")
    print(f"  Field: {field}")
    ctx_start = max(0, context.find(matched_text) - 40)
    ctx_end = min(len(context), context.find(matched_text) + len(matched_text) + 40)
    snippet = "..." + context[ctx_start:ctx_end] + "..."
    print(f"  Context: {snippet!r}")
    while True:
        ch = input("  Accept? [y/n/q]: ").strip().lower()
        if ch == "y":
            return True
        if ch == "n":
            return False
        if ch == "q":
            print("Aborted by user.")
            sys.exit(0)


def main():
    parser = argparse.ArgumentParser(
        prog="wiki_scan.py",
        description="Scan entity JSON files and insert [[wiki link]] syntax.",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__,
    )
    parser.add_argument("--dry-run", action="store_true", default=True,
                        help="Preview changes without modifying files (default: on)")
    parser.add_argument("--apply", action="store_true",
                        help="Write [[...]] links back to JSON files")
    parser.add_argument("--interactive", action="store_true",
                        help="Prompt before each insertion")
    parser.add_argument("--partial", action="store_true",
                        help="Include partial/fuzzy matches for review")
    parser.add_argument("--repo", metavar="REPO",
                        help="Limit scan to one repo folder (e.g. people)")
    parser.add_argument("--min-length", type=int, default=WIKI_MIN_NAME_LENGTH,
                        metavar="N", help=f"Minimum name length (default: {WIKI_MIN_NAME_LENGTH})")
    parser.add_argument("--data-dir", metavar="DIR",
                        help="Override engine/data path")
    args = parser.parse_args()

    if args.apply:
        args.dry_run = False

    try:
        data_dir = resolve_data_dir(args.data_dir)
    except FileNotFoundError as e:
        print(f"ERROR: {e}")
        sys.exit(1)

    print(f"Data directory: {data_dir}")
    print(f"Mode: {'DRY RUN (no files modified)' if args.dry_run else 'APPLY (files will be modified)'}")
    if args.interactive:
        print("Interactive: prompting before each insertion")
    print()

    print("Building entity index...")
    index = build_index(data_dir, args.min_length)
    print(f"  Indexed {len(index):,} entity names and aliases\n")

    repos_to_scan = [args.repo] if args.repo else REPOS
    total_links_inserted = 0
    total_files_modified = 0

    for repo in repos_to_scan:
        repo_dir = data_dir / repo
        if not repo_dir.exists():
            continue

        files = list(repo_dir.glob("*.json"))
        repo_links = 0
        repo_files = 0
        print(f"  [{repo}]  {len(files)} files")

        for jf in files:
            try:
                raw = jf.read_text(encoding="utf-8")
                data = json.loads(raw)
            except Exception as e:
                print(f"    SKIP {jf.name}: {e}")
                continue

            entity_name = data.get("name") or data.get("title") or jf.stem
            text_fields = extract_text_fields(data)
            if not text_fields:
                continue

            file_modified = False
            for field, text in text_fields:
                matches = find_matches(text, index, args.partial)
                if not matches:
                    continue

                # Filter out self-references (don't link an entity to itself)
                entity_id = data.get("id", "")
                matches = [m for m in matches if m[2].get("id") != entity_id]
                if not matches:
                    continue

                accepted = []
                for m in matches:
                    start, end, entry, matched_text = m
                    if args.interactive:
                        if confirm_match(entry, matched_text, field, text):
                            accepted.append(m)
                    else:
                        accepted.append(m)
                        print(f"    {entity_name!r} → {field}: {matched_text!r} → [[{entry['name']}]]")

                if accepted and not args.dry_run:
                    data[field] = apply_links(text, accepted)
                    file_modified = True

                repo_links += len(accepted)
                total_links_inserted += len(accepted)

            if file_modified:
                jf.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding="utf-8")
                repo_files += 1
                total_files_modified += 1

        print(f"    → {repo_links} links {'would be' if args.dry_run else ''} inserted"
              f" across {repo_files} files\n")

    print("=" * 60)
    print(f"Total links {'to insert' if args.dry_run else 'inserted'}: {total_links_inserted:,}")
    print(f"Total files {'that would be' if args.dry_run else ''} modified: {total_files_modified:,}")
    if args.dry_run:
        print("\nRun with --apply to write changes.")


if __name__ == "__main__":
    main()
