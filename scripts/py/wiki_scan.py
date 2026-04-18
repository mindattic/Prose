"""
wiki_scan.py -- StreetSamurai Wiki Link Scanner

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
  --single SLUG     Scan a single entity by slug (e.g. --single kyle-ellen-corbin-vasik)
  --since DURATION  Only scan files modified within this duration (e.g. 2h, 30m, 1d)
  --limit N         Stop after converting N entities (files with links inserted)
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

  # Scan and apply links for one specific entity
  python wiki_scan.py --apply --single kyle-ellen-corbin-vasik

  # Only re-scan files changed in the last 2 hours (fast re-run)
  python wiki_scan.py --apply --since 2h

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
import time
from pathlib import Path
from typing import Optional

# Allow running from scripts/py/ or from project root
sys.path.insert(0, str(Path(__file__).parent))
try:
    from constants import DATA_DIR, REPOS, REPO_ROUTES, WIKI_SCAN_FIELDS, WIKI_MIN_NAME_LENGTH, WIKI_SCAN_SKIP_REPOS
except ImportError:
    DATA_DIR = "../../engine/data"
    REPOS = [
        "people", "synthetics", "corponations", "subsidiaries", "places", "factions", "weaponry",
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

def to_slug(name: str) -> str:
    import unicodedata
    s = name.lower().strip()
    s = ''.join(c for c in unicodedata.normalize('NFD', s)
                if unicodedata.category(c) != 'Mn')
    return re.sub(r'[^a-z0-9]+', '-', s).strip('-')


def find_entity_by_slug(data_dir: Path, slug: str) -> tuple[str, Path] | None:
    """Search all repo dirs for a JSON entity whose name slug matches. Returns (repo, filepath)."""
    all_repos = [d.name for d in data_dir.iterdir() if d.is_dir()]
    for repo in all_repos:
        for jf in (data_dir / repo).glob("*.json"):
            try:
                data = json.loads(jf.read_text(encoding="utf-8"))
            except Exception:
                continue
            name = data.get("name") or data.get("title") or data.get("term") or data.get("headline") or ""
            if name and to_slug(name) == slug:
                return repo, jf
    return None


def parse_since(value: str) -> float:
    """Parse a duration string into a Unix cutoff timestamp.
    Accepts: 30m, 2h, 1d, 7d, or a bare integer (treated as minutes).
    Returns the earliest mtime that should be included."""
    value = value.strip().lower()
    if value.endswith("d"):
        seconds = float(value[:-1]) * 86400
    elif value.endswith("h"):
        seconds = float(value[:-1]) * 3600
    elif value.endswith("m"):
        seconds = float(value[:-1]) * 60
    else:
        seconds = float(value) * 60  # bare number = minutes
    return time.time() - seconds


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

            for alias in data.get("aliases", []):
                if isinstance(alias, str) and len(alias) >= min_length:
                    key = alias.lower()
                    if key not in index:
                        index[key] = {**entry, "display_name": alias}

    return index


# Keys whose string values are never prose (IDs, enums, dates, short codes)
# "name", "aliases", "title" etc. are entity identifiers -- never modify them with wiki markup
WIKI_SCAN_SKIP_KEYS = {
    "id", "name", "aliases", "title", "term", "codename", "product_name", "brand_name",
    "full_legal_name", "common_names", "headline", "type", "species", "gender", "pronouns",
    "status", "tier", "tier_availability", "legality", "caliber", "runner", "file", "flag",
    "date", "story_id", "installed_date", "body_location", "condition",
    "replaces", "hair_color", "hair_style", "hair_length", "eye_color",
    "skin_tone", "complexion", "build", "posture_movement", "height_cm",
    "weight_kg", "emotional_core", "story_tension", "canon_status",
    "slug", "route", "image_prompt", "dalle3_prompt", "midjourneyPrompt",
    "image", "created_at", "updated_at", "version", "changelog",
}

def extract_text_fields(data: dict, min_text_length: int = 12) -> list[tuple[str, str]]:
    """Recursively walk all string values in the JSON tree, yielding (path, text)
    for any string long enough to plausibly contain entity mentions."""
    results = []

    def walk(node, path: str):
        if isinstance(node, str):
            if len(node) >= min_text_length and node.strip():
                results.append((path, node))
        elif isinstance(node, list):
            for i, item in enumerate(node):
                walk(item, f"{path}[{i}]")
        elif isinstance(node, dict):
            for key, val in node.items():
                if key in WIKI_SCAN_SKIP_KEYS:
                    continue
                walk(val, f"{path}.{key}" if path else key)

    walk(data, "")
    return results


def already_linked(text: str) -> set[str]:
    """Return lowercase set of names already inside [[...]] in this text.
    Captures both the canonical name and the display text from pipe-style links
    ([[Canon|Display]]) so alias patterns skip correctly on re-runs."""
    result = set()
    for m in WIKI_LINK_RE.finditer(text):
        result.add(m.group(1).lower())
        if m.group(2):
            result.add(m.group(2).lower())
    return result


def is_sentence_start(text: str, start: int) -> bool:
    """True if position `start` follows sentence-ending punctuation or is at string start.
    Used to reject single-word capitalizations that are grammatical, not proper-noun."""
    prefix = text[:start].rstrip(' \t')
    return not prefix or prefix[-1] in '.!?\n'


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
        # For alias entries, search for the alias text itself (not the canonical name).
        # canonical is used in the output link; search_term drives the regex.
        search_term = entry.get("display_name", canonical)
        if name_lower in linked:
            continue  # Already linked

        is_multiword = ' ' in search_term.strip()

        if partial:
            # Partial/fuzzy preview -- case-insensitive, no word boundary required
            pattern = re.compile(re.escape(search_term), re.IGNORECASE)
        else:
            # Exact match -- case-sensitive so "face" != "Face" (the archetype)
            # Word boundary: not preceded/followed by [ or word char
            pattern = re.compile(r'(?<![\[\w])' + re.escape(search_term) + r'(?![\w\]])')

        for m in pattern.finditer(text):
            start, end = m.start(), m.end()
            matched_text = m.group(0)
            # Proper nouns only -- skip if match starts with a lowercase letter
            if not partial and matched_text[0].islower():
                continue
            # Single-word names capitalized only by sentence position are not entity references.
            # Multi-word names are distinctive enough that sentence-start position is irrelevant.
            if not partial and not is_multiword and matched_text[0].isupper():
                if is_sentence_start(text, start):
                    continue
            # Don't overlap with already-matched spans
            if any(s <= start < e or s < end <= e for s, e in covered_spans):
                continue
            # Don't match inside existing [[...]]
            pre = text[:start]
            opens = pre.count('[[') - pre.count(']]')
            if opens > 0:
                continue
            covered_spans.append((start, end))
            matches.append((start, end, entry, matched_text))

    return sorted(matches, key=lambda t: t[0])


def set_nested(data: dict, path: str, value: str) -> None:
    """Navigate a dotted/indexed path (e.g. 'relationships[0].description') and set the leaf."""
    parts = re.split(r'[\.\[\]]', path)
    parts = [p for p in parts if p]
    obj = data
    for part in parts[:-1]:
        obj = obj[int(part)] if isinstance(obj, list) else obj[part]
    last = parts[-1]
    if isinstance(obj, list):
        obj[int(last)] = value
    else:
        obj[last] = value


def apply_links(text: str, matches: list[tuple]) -> str:
    """Replace matched spans with [[DisplayText|entityId]] syntax.
    The entity ID is the stable reference; display text is what appeared in the source."""
    if not matches:
        return text

    result = []
    prev = 0
    for start, end, entry, matched_text in sorted(matches, key=lambda t: t[0]):
        result.append(text[prev:start])
        result.append(f"[[{matched_text}|{entry['id']}]]")
        prev = end
    result.append(text[prev:])
    return "".join(result)


def confirm_match(entry: dict, matched_text: str, field: str, context: str) -> bool:
    """Prompt user to confirm a single link insertion. Returns True to accept."""
    canonical = entry["name"]
    route = entry["route"] + "?id=" + entry["id"]
    print(f"\n  Found: {matched_text!r} -> [[{canonical}]]  ({entry['repo']})")
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
    parser.add_argument("--single", metavar="SLUG",
                        help="Scan a single entity by slug (e.g. kyle-ellen-corbin-vasik)")
    parser.add_argument("--min-length", type=int, default=WIKI_MIN_NAME_LENGTH,
                        metavar="N", help=f"Minimum name length (default: {WIKI_MIN_NAME_LENGTH})")
    parser.add_argument("--since", metavar="DURATION",
                        help="Only scan files modified within this duration (e.g. 2h, 30m, 1d)")
    parser.add_argument("--limit", type=int, metavar="N",
                        help="Stop after converting N entities (files with links inserted)")
    parser.add_argument("--data-dir", metavar="DIR",
                        help="Override engine/data path")
    parser.add_argument("--silent", action="store_true", help="Suppress all console output")
    args = parser.parse_args()
    if args.silent:
        import sys as _sys, os as _os
        _sys.stdout = open(_os.devnull, "w")
        _sys.stderr = open(_os.devnull, "w")


    if args.apply:
        args.dry_run = False

    try:
        data_dir = resolve_data_dir(args.data_dir)
    except FileNotFoundError as e:
        print(f"ERROR: {e}")
        sys.exit(1)

    since_cutoff: Optional[float] = None
    if args.since:
        try:
            since_cutoff = parse_since(args.since)
        except ValueError:
            print(f"ERROR: invalid --since value {args.since!r}. Use e.g. 2h, 30m, 1d.")
            sys.exit(1)

    print(f"Data directory: {data_dir}")
    print(f"Mode: {'DRY RUN (no files modified)' if args.dry_run else 'APPLY (files will be modified)'}")
    if since_cutoff is not None:
        import datetime
        cutoff_dt = datetime.datetime.fromtimestamp(since_cutoff).strftime("%Y-%m-%d %H:%M:%S")
        print(f"Since: {args.since} (files modified after {cutoff_dt})")
    if args.interactive:
        print("Interactive: prompting before each insertion")
    print()

    # --single: resolve slug to a specific file before building the index
    single_file: Path | None = None
    single_repo: str | None = None
    if args.single:
        result = find_entity_by_slug(data_dir, args.single)
        if result is None:
            print(f"ERROR: no entity found with slug {args.single!r}")
            sys.exit(1)
        single_repo, single_file = result
        print(f"Single entity: {single_file.name}  (repo: {single_repo})")

    print("Building entity index...")
    index = build_index(data_dir, args.min_length)
    print(f"  Indexed {len(index):,} entity names and aliases\n")

    # Determine repos and files to scan
    if single_file is not None:
        repos_to_scan = [single_repo]
    elif args.repo:
        repos_to_scan = [args.repo]
    else:
        repos_to_scan = REPOS
    total_links_inserted = 0
    total_files_modified = 0

    for repo in repos_to_scan:
        repo_dir = data_dir / repo
        if not repo_dir.exists():
            continue

        all_files = list(repo_dir.glob("*.json"))
        if single_file is not None:
            files = [single_file]
        else:
            files = [f for f in all_files if since_cutoff is None or f.stat().st_mtime >= since_cutoff]
        repo_links = 0
        repo_files = 0
        if not files:
            continue
        label = f"{len(files)} / {len(all_files)}" if since_cutoff is not None else str(len(files))
        print(f"  [{repo}]  {label} files", end="", flush=True)

        for jf in files:
            print(".", end="", flush=True)
            try:
                raw = jf.read_text(encoding="utf-8")
                data = json.loads(raw)
            except Exception as e:
                print(f"\n    SKIP {jf.name}: {e}", end="", flush=True)
                continue

            entity_name = data.get("name") or data.get("title") or jf.stem
            text_fields = extract_text_fields(data)
            if not text_fields:
                continue

            entity_id = data.get("id", "")
            entity_name = (data.get("name") or data.get("title") or "").lower()

            file_modified = False
            for field, text in text_fields:
                matches = find_matches(text, index, args.partial)
                if not matches:
                    continue

                # Filter out self-references: skip if same ID or same name (catches name collisions)
                matches = [m for m in matches
                           if m[2].get("id") != entity_id
                           and m[2].get("name", "").lower() != entity_name]
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
                        print(f"    {entity_name!r} -> {field}: {matched_text!r} -> [[{matched_text}|{entry['id']}]]")

                if accepted and not args.dry_run:
                    set_nested(data, field, apply_links(text, accepted))
                    file_modified = True

                repo_links += len(accepted)
                total_links_inserted += len(accepted)

            if file_modified:
                jf.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding="utf-8")
                repo_files += 1
                total_files_modified += 1
                if args.limit and total_files_modified >= args.limit:
                    print(f"\n    [limit reached: {args.limit} entities converted]")
                    break

        print(f"\n    -> {repo_links} links {'would be' if args.dry_run else ''} inserted"
              f" across {repo_files} files\n")

        if args.limit and total_files_modified >= args.limit:
            break

    print("=" * 60)
    print(f"Total links {'to insert' if args.dry_run else 'inserted'}: {total_links_inserted:,}")
    print(f"Total files {'that would be modified' if args.dry_run else 'modified'}: {total_files_modified:,}")
    print()
    if args.dry_run:
        print("Run with --apply to write changes.")


if __name__ == "__main__":
    main()
