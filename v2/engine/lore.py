"""
Lore Browser -- Search and display worldbuilding canon from the terminal.

Note: Forces UTF-8 stdout on Windows to avoid cp1252 encoding errors.

Provides fast lookups across:
  - Corponations (by name, number, or sector)
  - Characters
  - Locations
  - Technologies, weapons, drugs
  - Free-text search across all worldbuilding

Usage:
    python -m engine.lore search "Ringo"
    python -m engine.lore corp 5
    python -m engine.lore corp "Tessera"
    python -m engine.lore corps                    # list all 120
    python -m engine.lore entity "Kael"
    python -m engine.lore topic "exclusion registry"
    python -m engine.lore docs                     # list all worldbuilding docs
    python -m engine.lore read "arsenal_ecosystem"
"""

import io
import re
import sys
from pathlib import Path

# Force UTF-8 on Windows
if sys.platform == "win32":
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")

from .config import WORLDBUILDING_DIR, CHARACTERS_DIR, ESSENCES_DIR, ROOT


# -- Corponation Index ----------------------------------------------------------

def _build_corp_index() -> list[dict]:
    """Parse all corponation files and build a searchable index."""
    corps = []
    corp_files = sorted(WORLDBUILDING_DIR.glob("corponations_*.md"))

    for filepath in corp_files:
        text = filepath.read_text(encoding="utf-8")
        source = filepath.name

        # Extract entries -- handles multiple heading formats
        # Pattern 1: ### 5. Ringo CorpoNation
        # Pattern 2: **5. Name**
        # Pattern 3: ### Name
        blocks = re.split(r'\n(?=###?\s+(?:\d+\.?\s+)?[A-Z]|\*\*\d+\.\s)', text)

        for block in blocks:
            # Try to extract number and name
            m = re.match(
                r'(?:###?\s*)?(?:\*\*)?(\d+)\.?\s*(.+?)(?:\*\*)?\s*\n',
                block.strip()
            )
            if not m:
                continue

            num = int(m.group(1))
            name = m.group(2).strip().rstrip('*')

            # Extract sector
            sector_m = re.search(r'\*\*Sector:\*\*\s*(.+)', block)
            sector = sector_m.group(1).strip() if sector_m else ""

            # Extract valuation
            val_m = re.search(r'\*\*Valuation:\*\*\s*(.+)', block)
            valuation = val_m.group(1).strip() if val_m else ""

            # Extract origin (first 1-2 sentences)
            origin_m = re.search(r'\*\*Origin:\*\*\s*(.+)', block)
            origin = origin_m.group(1).strip()[:200] if origin_m else ""

            # Extract key detail
            detail_m = re.search(r'\*\*Key Detail:\*\*\s*(.+)', block)
            key_detail = detail_m.group(1).strip()[:200] if detail_m else ""

            # Extract territory
            territory_m = re.search(r'\*\*Territory:\*\*\s*(.+)', block)
            territory = territory_m.group(1).strip()[:200] if territory_m else ""

            # Extract security force
            security_m = re.search(r'\*\*Security Force:\*\*\s*(.+)', block)
            security = security_m.group(1).strip()[:200] if security_m else ""

            # For Big 20, extract from longer-form entries
            if not sector and not valuation:
                # Try alternate patterns for the detailed corp files
                sector_m2 = re.search(r'Sector[:\s]+(.+?)(?:\n|$)', block[:500])
                if sector_m2:
                    sector = sector_m2.group(1).strip()

            corps.append({
                "number": num,
                "name": name,
                "sector": sector,
                "valuation": valuation,
                "origin": origin,
                "key_detail": key_detail,
                "territory": territory,
                "security": security,
                "source": source,
                "full_text": block.strip(),
            })

    # Sort by number
    corps.sort(key=lambda c: c["number"])
    return corps


def list_corps(filter_text: str | None = None) -> None:
    """List all corponations, optionally filtered."""
    corps = _build_corp_index()

    if filter_text:
        ft = filter_text.lower()
        corps = [c for c in corps if
                 ft in c["name"].lower() or
                 ft in c["sector"].lower() or
                 ft in str(c["number"])]

    if not corps:
        print("  No corponations found matching that filter.")
        return

    # Print as table
    print(f"  {'#':>4}  {'Name':<35} {'Sector':<30} {'Valuation':<12}")
    print(f"  {'-' * 4}  {'-' * 35} {'-' * 30} {'-' * 12}")
    for c in corps:
        name = c["name"][:34]
        sector = c["sector"][:29] if c["sector"] else "--"
        val = c["valuation"][:11] if c["valuation"] else "--"
        print(f"  {c['number']:>4}  {name:<35} {sector:<30} {val:<12}")

    print(f"\n  Total: {len(corps)} corponations")


def show_corp(identifier: str) -> None:
    """Show details for a single corponation by number or name."""
    corps = _build_corp_index()

    # Try number first
    try:
        num = int(identifier)
        matches = [c for c in corps if c["number"] == num]
    except ValueError:
        # Search by name
        ft = identifier.lower()
        matches = [c for c in corps if ft in c["name"].lower()]

    if not matches:
        print(f"  No corponation found: {identifier}")
        return

    c = matches[0]
    print(f"  +{'-' * 58}+")
    print(f"  | #{c['number']:>3}  {c['name']:<51} |")
    print(f"  +{'-' * 58}+")
    print()

    if c["sector"]:
        print(f"  Sector:     {c['sector']}")
    if c["valuation"]:
        print(f"  Valuation:  {c['valuation']}")
    if c["origin"]:
        print(f"  Origin:     {c['origin']}")
    if c["territory"]:
        print(f"  Territory:  {c['territory']}")
    if c["security"]:
        print(f"  Security:   {c['security']}")
    if c["key_detail"]:
        print(f"  Key Detail: {c['key_detail']}")
    print(f"  Source:     {c['source']}")

    # Print full text if it's a shorter entry (mid-tier corps)
    full = c["full_text"]
    if len(full) < 3000:
        print(f"\n{'-' * 60}")
        print(full)


# -- Free-Text Search ----------------------------------------------------------

def search_worldbuilding(query: str, max_results: int = 10) -> None:
    """Search across all worldbuilding docs for a term."""
    query_lower = query.lower()
    results = []

    for filepath in sorted(WORLDBUILDING_DIR.glob("*.md")):
        if filepath.name.startswith("ARCHIVED_"):
            continue
        text = filepath.read_text(encoding="utf-8")
        if query_lower not in text.lower():
            continue

        # Find matching lines with context
        lines = text.split("\n")
        for i, line in enumerate(lines):
            if query_lower in line.lower():
                # Get context: 1 line before, the match, 1 line after
                start = max(0, i - 1)
                end = min(len(lines), i + 2)
                context = "\n".join(lines[start:end]).strip()

                # Find nearest heading
                heading = ""
                for j in range(i, -1, -1):
                    if lines[j].startswith("#"):
                        heading = lines[j].lstrip("#").strip()
                        break

                results.append({
                    "file": filepath.name,
                    "heading": heading,
                    "line": i + 1,
                    "context": context,
                })

                if len(results) >= max_results:
                    break
        if len(results) >= max_results:
            break

    if not results:
        print(f"  No results for: {query}")
        return

    print(f"  Results for \"{query}\" ({len(results)} matches):\n")
    for r in results:
        print(f"  [{r['file']}:{r['line']}]", end="")
        if r["heading"]:
            print(f" > {r['heading']}", end="")
        print()
        # Highlight the match in context
        ctx = r["context"]
        if len(ctx) > 300:
            ctx = ctx[:300] + "..."
        for ctx_line in ctx.split("\n"):
            print(f"    {ctx_line}")
        print()


# -- Topic Lookup (RAG) --------------------------------------------------------

def topic_lookup(query: str) -> None:
    """Use the vector store for semantic topic search."""
    try:
        from .embedder import get_collection
        collection = get_collection()
    except Exception:
        print("  Canon index not built. Run 'Build Canon Index' first.")
        return

    results = collection.query(
        query_texts=[query],
        n_results=8,
    )

    if not results or not results["documents"] or not results["documents"][0]:
        print(f"  No results for: {query}")
        return

    print(f"  Topic: \"{query}\"\n")
    seen_sources = set()
    for i, doc in enumerate(results["documents"][0]):
        meta = results["metadatas"][0][i] if results["metadatas"] else {}
        source = meta.get("source", "unknown")
        heading = meta.get("heading", "")

        source_key = f"{source}|{heading}"
        if source_key in seen_sources:
            continue
        seen_sources.add(source_key)

        print(f"  [{source}]", end="")
        if heading:
            print(f" > {heading}", end="")
        print()

        # Show first 300 chars of the chunk
        snippet = doc.strip()
        if len(snippet) > 300:
            snippet = snippet[:300] + "..."
        for line in snippet.split("\n")[:6]:
            print(f"    {line}")
        print()


# -- Entity Lookup (Knowledge Graph) -------------------------------------------

def entity_lookup(name: str) -> None:
    """Look up an entity in the knowledge graph."""
    try:
        from .graph import load_graph, query_entity
        G = load_graph()
    except FileNotFoundError:
        print("  Knowledge graph not built. Run 'Build Canon Index' first.")
        return

    info = query_entity(G, name)
    if not info:
        # Try partial match
        matches = [n for n in G.nodes if name.lower() in n.lower()]
        if matches:
            print(f"  No exact match for \"{name}\". Did you mean:")
            for m in matches[:10]:
                print(f"    - {m}")
            return
        print(f"  Entity not found: {name}")
        return

    print(f"  +{'-' * 58}+")
    print(f"  | {info['name']:<57}|")
    print(f"  +{'-' * 58}+")
    print()

    attrs = info["attributes"]
    data = attrs.get("data", {})
    if attrs.get("type"):
        print(f"  Type:        {attrs['type']}")
    if attrs.get("source"):
        print(f"  Source:      {attrs['source']}")
    for k, v in data.items():
        if k not in ("name",) and v:
            if isinstance(v, dict):
                print(f"  {k}:")
                for sk, sv in v.items():
                    print(f"    {sk}: {sv}")
            else:
                print(f"  {k:<12}  {v}")

    if info["outgoing"]:
        print(f"\n  Connections (outgoing):")
        for rel in info["outgoing"]:
            print(f"    -> {rel['relationship']:20} -> {rel['target']}")

    if info["incoming"]:
        print(f"\n  Connections (incoming):")
        for rel in info["incoming"]:
            print(f"    <- {rel['relationship']:20} <- {rel['source']}")


# -- Document Browser ----------------------------------------------------------

def list_docs() -> None:
    """List all worldbuilding documents with line counts."""
    files = sorted(WORLDBUILDING_DIR.glob("*.md"))
    files = [f for f in files if not f.name.startswith("ARCHIVED_")]

    total_lines = 0
    print(f"  {'Document':<45} {'Lines':>6}")
    print(f"  {'-' * 45} {'-' * 6}")
    for f in files:
        lines = len(f.read_text(encoding="utf-8").split("\n"))
        total_lines += lines
        print(f"  {f.stem:<45} {lines:>6}")

    print(f"  {'-' * 45} {'-' * 6}")
    print(f"  {'TOTAL':<45} {total_lines:>6}")
    print(f"\n  {len(files)} documents")


def read_doc(name: str) -> None:
    """Display a worldbuilding document."""
    # Find the file
    matches = list(WORLDBUILDING_DIR.glob(f"*{name}*.md"))
    if not matches:
        print(f"  Document not found: {name}")
        return
    if len(matches) > 1:
        print(f"  Multiple matches:")
        for m in matches:
            print(f"    - {m.stem}")
        return

    text = matches[0].read_text(encoding="utf-8")
    # Print with basic paging
    lines = text.split("\n")
    page_size = 40
    for i in range(0, len(lines), page_size):
        for line in lines[i:i + page_size]:
            print(f"  {line}")
        if i + page_size < len(lines):
            remaining = len(lines) - (i + page_size)
            print(f"\n  --- {remaining} lines remaining. Press Enter to continue, Q to quit ---")
            inp = input("  ").strip().lower()
            if inp == "q":
                break


# -- Character Browser ---------------------------------------------------------

def list_characters() -> None:
    """List all character files."""
    import yaml

    chars_files = list(CHARACTERS_DIR.rglob("*.yaml")) if CHARACTERS_DIR.exists() else []
    ess_chars = []
    if ESSENCES_DIR.exists():
        for f in ESSENCES_DIR.rglob("*.yaml"):
            try:
                data = yaml.safe_load(f.read_text(encoding="utf-8"))
                if isinstance(data, dict) and data.get("type", "").lower() in ("character", "npc", "protagonist"):
                    ess_chars.append(f)
            except Exception:
                pass

    all_files = chars_files + ess_chars
    if not all_files:
        print("  No character files found.")
        return

    print(f"  {'Name':<30} {'Type':<15} {'Source':<40}")
    print(f"  {'-' * 30} {'-' * 15} {'-' * 40}")

    for f in sorted(all_files, key=lambda x: x.stem):
        try:
            data = yaml.safe_load(f.read_text(encoding="utf-8"))
            name = data.get("name", f.stem)
            ctype = data.get("type", "character")
            rel_path = str(f.relative_to(ROOT))
            print(f"  {name:<30} {ctype:<15} {rel_path:<40}")
        except Exception:
            print(f"  {f.stem:<30} {'error':<15} {str(f):<40}")


def show_character_detail(name: str) -> None:
    """Show full character details."""
    import yaml

    # Search characters/ and essences/
    for search_dir in [CHARACTERS_DIR, ESSENCES_DIR]:
        if not search_dir.exists():
            continue
        for f in search_dir.rglob("*.yaml"):
            try:
                data = yaml.safe_load(f.read_text(encoding="utf-8"))
                if not isinstance(data, dict):
                    continue
                cname = data.get("name", f.stem)
                if name.lower() in cname.lower() or name.lower() in f.stem.lower():
                    # Found it -- display
                    print(f"  +{'-' * 58}+")
                    print(f"  | {cname:<57}|")
                    print(f"  +{'-' * 58}+")
                    print()

                    # Print key fields
                    for key in ["type", "tier", "status", "age", "occupation", "affiliation"]:
                        if key in data:
                            val = data[key]
                            if isinstance(val, str) and len(val) > 80:
                                val = val[:77] + "..."
                            print(f"  {key:<15} {val}")

                    # Facets
                    facets = data.get("facets") or data.get("facet_weights")
                    if isinstance(facets, dict):
                        print(f"\n  Facet Weights:")
                        for fname, fval in facets.items():
                            bar_len = int(float(fval) * 30)
                            bar = "#" * bar_len + "." * (30 - bar_len)
                            print(f"    {fname:<8} {bar} {fval}")

                    # Aliases
                    aliases = data.get("aliases", [])
                    if aliases:
                        print(f"\n  Aliases: {', '.join(str(a) for a in aliases)}")

                    # Relationships
                    rels = data.get("relationships", [])
                    if rels:
                        print(f"\n  Relationships:")
                        for rel in rels:
                            rname = rel.get("name", "?")
                            rstatus = rel.get("status", rel.get("facet_connection", ""))
                            print(f"    -> {rname}: {rstatus}")

                    print(f"\n  Source: {f.relative_to(ROOT)}")
                    return
            except Exception:
                continue

    print(f"  Character not found: {name}")


# -- CLI -----------------------------------------------------------------------

def main():
    import argparse

    parser = argparse.ArgumentParser(description="Street Samurai Lore Browser")
    subs = parser.add_subparsers(dest="command")

    # Corps
    p_corps = subs.add_parser("corps", help="List all corponations")
    p_corps.add_argument("filter", nargs="?", default=None, help="Filter by name/sector")

    p_corp = subs.add_parser("corp", help="Show a specific corponation")
    p_corp.add_argument("identifier", help="Corp number or name")

    # Search
    p_search = subs.add_parser("search", help="Search all worldbuilding text")
    p_search.add_argument("query", help="Search term")
    p_search.add_argument("--max", type=int, default=10, help="Max results")

    # Topic (semantic/RAG)
    p_topic = subs.add_parser("topic", help="Semantic topic lookup via vector store")
    p_topic.add_argument("query", help="Topic to look up")

    # Entity (graph)
    p_entity = subs.add_parser("entity", help="Look up entity in knowledge graph")
    p_entity.add_argument("name", help="Entity name")

    # Docs
    p_docs = subs.add_parser("docs", help="List all worldbuilding documents")

    p_read = subs.add_parser("read", help="Read a worldbuilding document")
    p_read.add_argument("name", help="Document name (partial match)")

    # Characters
    p_chars = subs.add_parser("characters", help="List all characters")

    p_char = subs.add_parser("character", help="Show character details")
    p_char.add_argument("name", help="Character name")

    args = parser.parse_args()

    if not args.command:
        parser.print_help()
        sys.exit(1)

    match args.command:
        case "corps":
            list_corps(args.filter)
        case "corp":
            show_corp(args.identifier)
        case "search":
            search_worldbuilding(args.query, args.max)
        case "topic":
            topic_lookup(args.query)
        case "entity":
            entity_lookup(args.name)
        case "docs":
            list_docs()
        case "read":
            read_doc(args.name)
        case "characters":
            list_characters()
        case "character":
            show_character_detail(args.name)


if __name__ == "__main__":
    main()
