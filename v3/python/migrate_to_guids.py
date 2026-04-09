"""
GUID Reference Migration + Surname Regeneration

Phase 1: Build a master entity index (name → GUID) across all repos
Phase 2: Convert all related_entities, relationships, known_users, etc. from name strings to GUIDs
Phase 3: Regenerate surnames from genetic ancestry (rarity-weighted)
Phase 4: Re-run cross-referencing (now GUID-based)

Resume-safe: each phase checks what's already done.
Run with: python migrate_to_guids.py

Usage:
  python migrate_to_guids.py --phase 1       # Build index only (dry run)
  python migrate_to_guids.py --phase 2       # Convert references to GUIDs
  python migrate_to_guids.py --phase 3       # Regenerate surnames
  python migrate_to_guids.py --phase all     # Run everything
  python migrate_to_guids.py --dry-run       # Preview all phases
"""

import json
import glob
import os
import re
import random
import asyncio
from pathlib import Path
from collections import defaultdict
from dotenv import load_dotenv
from rich.console import Console
from rich.progress import Progress

load_dotenv()

console = Console()
DATA_DIR = os.getenv("DATA_DIR", "../../engine/data")
CONCURRENCY = int(os.getenv("CONCURRENCY", "20"))
MODEL = os.getenv("MODEL", "claude-haiku-4-5-20251001")
ANTHROPIC_API_KEY = os.getenv("ANTHROPIC_API_KEY", "")

# All repo subdirectories
REPOS = [
    "characters", "corponations", "places", "factions", "weaponry",
    "equipment", "technology", "cyberware", "ammunition", "apparel",
    "archetypes", "automata", "entertainment", "genemods", "materials",
    "news", "pharmaceuticals", "consumer_goods", "quotes", "subsidiaries",
    "synthetics", "transportation", "vocabulary", "documents", "contracts",
]

# Reference fields to convert (field name → whether it's a list or single value)
# These are the fields across various entity types that hold name-based references
REFERENCE_FIELDS = {
    "related_entities": "list",
    "known_users": "list",
    "parent_corponation": "string",
    "manufacturer": "string",
    "primary_weapon": "string",
    "secondary_weapon": "string",
    "armor": "string",
    "vehicle": "string",
    "favorite_drink": "string",
    "favorite_food": "string",
    "stimulant": "string",
    "comm_device": "string",
    "signature_gear": "list",
    "pharmaceuticals": "list",
}

# Relationship sub-object fields
RELATIONSHIP_NAME_FIELD = "name"

# Characters to skip during surname regeneration (already correct)
SKIP_SURNAME_REGEN = {"Kyle Ellen Corbin-Vasik"}


# ══════════════════════════════════════════════════════════════════
# Phase 1: Build master entity index
# ══════════════════════════════════════════════════════════════════

def build_entity_index():
    """Build a mapping of entity name → (GUID, repo, filepath)."""
    console.print("[bold]Phase 1: Building entity index[/bold]")
    index = {}  # name -> {id, repo, path}
    dupes = defaultdict(list)

    for repo in REPOS:
        repo_dir = Path(DATA_DIR) / repo
        if not repo_dir.exists():
            continue
        for fp in glob.glob(str(repo_dir / "*.json")):
            try:
                with open(fp, "r", encoding="utf-8") as f:
                    data = json.load(f)
                if not isinstance(data, dict):
                    continue

                entity_id = data.get("id", "")
                name = data.get("name", data.get("title", data.get("headline", "")))
                if not entity_id or not name:
                    continue

                if name in index:
                    dupes[name].append({"id": entity_id, "repo": repo, "path": fp})
                else:
                    index[name] = {"id": entity_id, "repo": repo, "path": fp}

                # Also index aliases
                for alias in data.get("aliases", []):
                    if alias and alias not in index:
                        index[alias] = {"id": entity_id, "repo": repo, "path": fp}

            except Exception:
                pass

    console.print(f"  Indexed {len(index)} entity names across {len(REPOS)} repos")
    if dupes:
        console.print(f"  [yellow]{len(dupes)} duplicate names found (first match used)[/yellow]")

    return index


# ══════════════════════════════════════════════════════════════════
# Phase 2: Convert references from names to GUIDs
# ══════════════════════════════════════════════════════════════════

def convert_references_to_guids(index, dry_run=False):
    """Convert all name-based references to GUID-based references."""
    console.print("\n[bold]Phase 2: Converting references to GUIDs[/bold]")

    total_files = 0
    total_refs_converted = 0

    for repo in REPOS:
        repo_dir = Path(DATA_DIR) / repo
        if not repo_dir.exists():
            continue

        files = glob.glob(str(repo_dir / "*.json"))
        for fp in files:
            try:
                with open(fp, "r", encoding="utf-8") as f:
                    data = json.load(f)
                if not isinstance(data, dict):
                    continue

                changed = False

                # Convert top-level reference fields
                for field, field_type in REFERENCE_FIELDS.items():
                    if field_type == "list":
                        val = data.get(field, [])
                        if isinstance(val, list) and val:
                            new_val = []
                            for item in val:
                                if isinstance(item, str) and item in index:
                                    new_val.append(index[item]["id"])
                                    total_refs_converted += 1
                                    changed = True
                                else:
                                    new_val.append(item)
                            data[field] = new_val

                    elif field_type == "string":
                        val = data.get(field, "")
                        if isinstance(val, str) and val and val in index:
                            data[field] = index[val]["id"]
                            total_refs_converted += 1
                            changed = True

                # Convert nested belongings
                belongings = data.get("belongings", {})
                if isinstance(belongings, dict):
                    for field in ["primary_weapon", "secondary_weapon", "armor", "vehicle",
                                  "favorite_drink", "favorite_food", "stimulant", "comm_device"]:
                        val = belongings.get(field, "")
                        if isinstance(val, str) and val and val in index:
                            belongings[field] = index[val]["id"]
                            total_refs_converted += 1
                            changed = True
                    for field in ["signature_gear", "pharmaceuticals"]:
                        val = belongings.get(field, [])
                        if isinstance(val, list):
                            new_val = []
                            for item in val:
                                if isinstance(item, str) and item in index:
                                    new_val.append(index[item]["id"])
                                    total_refs_converted += 1
                                    changed = True
                                else:
                                    new_val.append(item)
                            belongings[field] = new_val

                # Convert relationships[].name
                rels = data.get("relationships", [])
                if isinstance(rels, list):
                    for rel in rels:
                        if isinstance(rel, dict):
                            name = rel.get("name", "")
                            if name and name in index:
                                rel["name"] = index[name]["id"]
                                total_refs_converted += 1
                                changed = True

                if changed:
                    total_files += 1
                    if not dry_run:
                        with open(fp, "w", encoding="utf-8") as f:
                            json.dump(data, f, indent=2, ensure_ascii=False)

            except Exception as e:
                console.print(f"  [red]Error on {fp}: {e}[/red]")

    console.print(f"  Files modified: {total_files}")
    console.print(f"  References converted: {total_refs_converted}")
    return total_refs_converted


# ══════════════════════════════════════════════════════════════════
# Phase 3: Regenerate surnames from genetic ancestry
# ══════════════════════════════════════════════════════════════════

# Surname pools by ancestry group — real surnames, weighted toward rarer ones
SURNAME_POOLS = {
    "Sub-Saharan African": [
        "Okonkwo", "Nwosu", "Dlamini", "Mbekele", "Achebe", "Ekwensi", "Asomaning",
        "Okafor", "Adeyemi", "Balogun", "Diallo", "Traoré", "Konaté", "Mensah",
        "Agyemang", "Owusu", "Boateng", "Appiah", "Nkrumah", "Tamale", "Kabwit",
        "Mwangi", "Kariuki", "Odinga", "Kamara", "Sesay", "Bangura", "Touré",
        "Keïta", "Cissé", "Sow", "Bah", "Prempeh", "Tetteh", "Quartey",
    ],
    "Hispanic/Latin American": [
        "Hernández", "Bautista", "Soriano", "Ibarra", "Espinoza", "Castañeda",
        "Cervantes", "Montalvo", "Delgado", "Valenzuela", "Guerrero", "Salazar",
        "Cardenas", "Villanueva", "Esquivel", "Sepúlveda", "Quiñones", "Jaramillo",
        "Arredondo", "Bustamante", "Villalobos", "Cisneros", "Madrigal", "Echeverría",
        "Zaragoza", "Trujillo", "Hinojosa", "Calvillo", "Rentería", "Tovar",
    ],
    "Northern European": [
        "Thorvaldsdóttir", "Bergqvist", "Lindström", "Kjellberg", "Halverson",
        "Strömqvist", "Solberg", "Haugen", "Nygaard", "Dalgaard", "Holm",
        "Erikstad", "Vestergaard", "Möller", "Johansson", "Larsdóttir",
        "Sigurdsson", "Árnason", "Magnúsdóttir", "Haraldsen", "Olofsson",
        "Fredriksen", "Ingebrigtsen", "Björnsdóttir", "Kristjánsson",
    ],
    "East Asian": [
        "Nakamura", "Kimura", "Tanaka", "Watanabe", "Yamazaki", "Hayashi",
        "Morimoto", "Fujiwara", "Shimizu", "Arakawa", "Ueda", "Takahashi",
        "Hwang", "Jeong", "Kwon", "Baek", "Yun", "Rhee", "Choi",
        "Liang", "Zhao", "Huang", "Xu", "Zheng", "Qian", "Gao",
    ],
    "South Asian": [
        "Chatterjee", "Mukherjee", "Banerjee", "Bhattacharya", "Raghavan",
        "Subramaniam", "Krishnamurthy", "Venkatesh", "Deshpande", "Kulkarni",
        "Pathak", "Thakur", "Malhotra", "Sengupta", "Chakraborty",
        "Rahman", "Hossain", "Chowdhury", "Karunaratne", "Wickramasinghe",
    ],
    "Eastern European": [
        "Szczypiński", "Kovalenko", "Petrović", "Krstić", "Čabarkapa",
        "Novotný", "Wojciechowski", "Lewandowski", "Kowalczyk", "Wójcik",
        "Zielinski", "Mazurek", "Volkov", "Kuznetsov", "Sokolov",
        "Popescu", "Ionescu", "Horváth", "Kovács", "Szabó",
    ],
    "Southeast Asian": [
        "Mallari", "Reyes", "Dimaculangan", "Bautista", "Pangilinan",
        "Tran", "Nguyen", "Pham", "Hoang", "Dang",
        "Somchai", "Kittisak", "Wattana", "Srisai",
        "Widodo", "Sukarno", "Hartono", "Susanto",
    ],
    "Middle Eastern": [
        "Vasquez-Alfarsi", "Khorasani", "Balasanyan", "Ghorbani", "Nazari",
        "Hosseini", "Mohammadi", "Karimi", "Shirazi", "Tehrani",
        "Haddad", "Khoury", "Mansour", "Sabbagh", "Najjar",
    ],
    "Pacific Islander": [
        "Tuivailala", "Taualagi", "Lautoa", "Tofauti", "Matai",
        "Tuiloma", "Savea", "Leota", "Vaai", "Faalele",
        "Moala", "Taufa", "Masoe", "Petelo", "Sefanaia",
    ],
    "Indigenous American": [
        "Tlaloc", "Huitzil", "Citlali", "Ixchel", "Ozcoidi",
        "Makwa", "Waboose", "Migizi", "Nokomis", "Anang",
    ],
    "Central Asian": [
        "Adu", "Nurzhanov", "Abdykadyrov", "Toktogulov", "Baiseitov",
        "Karimov", "Saidov", "Rakhimov", "Tursunov", "Ergashev",
    ],
    "North African": [
        "Benali", "Bouzid", "Amrani", "Zidane", "Boudiaf",
        "Hachemi", "Lahlou", "Tlemcani", "Fassi", "Kettani",
    ],
}


def regenerate_surnames(index, dry_run=False):
    """Regenerate character surnames based on genetic ancestry using rarity-weighted selection."""
    console.print("\n[bold]Phase 3: Regenerating surnames from genetic ancestry[/bold]")

    char_dir = Path(DATA_DIR) / "characters"
    files = sorted(glob.glob(str(char_dir / "*.json")))
    updated = 0

    # Build old_name → new_name mapping for later reference fixup
    name_changes = {}

    for fp in files:
        try:
            with open(fp, "r", encoding="utf-8") as f:
                data = json.load(f)
            if not isinstance(data, dict):
                continue

            old_name = data.get("name", "")
            if old_name in SKIP_SURNAME_REGEN:
                continue

            ancestry = data.get("genetic_ancestry", {})
            if not ancestry:
                continue

            # Keep first name(s) — everything before the last hyphenated surname
            # "Kyle Ellen Corbin-Vasik" → first_names="Kyle Ellen", surname="Corbin-Vasik"
            # "Abdirizak Petrov-Tuivailala" → first_names="Abdirizak", surname="Petrov-Tuivailala"
            parts = old_name.rsplit(" ", 1)
            if len(parts) < 2:
                continue

            # Find where the surname starts — it's the last space-separated token
            # But some have middle names: "Kyle Ellen Corbin-Vasik"
            # The surname is always hyphenated or the last word
            words = old_name.split()
            if "-" in words[-1]:
                first_names = " ".join(words[:-1])
            elif len(words) >= 3 and "-" in words[-1]:
                first_names = " ".join(words[:-1])
            else:
                first_names = " ".join(words[:-1])

            # Get the two largest ancestry groups
            sorted_ancestry = sorted(ancestry.items(), key=lambda x: -x[1])
            if len(sorted_ancestry) < 2:
                continue

            rng = random.Random(data.get("id", old_name))

            # Pick surname from top 2-3 ancestry groups, weighted by percentage
            top_groups = sorted_ancestry[:min(3, len(sorted_ancestry))]
            group_names = [g for g, _ in top_groups]
            group_weights = [p for _, p in top_groups]

            # Select two different groups for the two surname halves
            selected_groups = []
            available = list(zip(group_names, group_weights))
            for _ in range(2):
                if not available:
                    break
                total = sum(w for _, w in available)
                r = rng.uniform(0, total)
                cumulative = 0
                for i, (g, w) in enumerate(available):
                    cumulative += w
                    if r <= cumulative:
                        selected_groups.append(g)
                        available.pop(i)
                        break

            if len(selected_groups) < 2:
                continue

            # Pick a random surname from each group's pool
            surname_parts = []
            for group in selected_groups:
                pool = SURNAME_POOLS.get(group, [])
                if not pool:
                    # Fall back to a generic pool
                    pool = SURNAME_POOLS.get("Northern European", ["Unknown"])
                surname_parts.append(rng.choice(pool))

            new_surname = f"{surname_parts[0]}-{surname_parts[1]}"
            new_name = f"{first_names} {new_surname}"

            if new_name == old_name:
                continue

            name_changes[old_name] = new_name

            if dry_run:
                print(f"  {old_name} -> {new_name}")
                updated += 1
                continue

            data["name"] = new_name

            with open(fp, "w", encoding="utf-8") as f:
                json.dump(data, f, indent=2, ensure_ascii=False)
            updated += 1

        except Exception as e:
            console.print(f"  [red]Error on {fp}: {e}[/red]")

    console.print(f"  Characters renamed: {updated}")
    return name_changes


# ══════════════════════════════════════════════════════════════════
# Main
# ══════════════════════════════════════════════════════════════════

def main():
    import argparse
    parser = argparse.ArgumentParser(description="Migrate references to GUIDs + regenerate surnames")
    parser.add_argument("--phase", type=str, default="all", choices=["1", "2", "3", "all"])
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()

    if args.phase in ("1", "all"):
        index = build_entity_index()

        if args.phase == "1":
            console.print("\n[yellow]Phase 1 complete (index built). Run --phase 2 to convert references.[/yellow]")
            # Print sample
            from itertools import islice
            for name, info in islice(index.items(), 5):
                name_safe = name.encode('ascii', 'replace').decode()
                print(f"  {name_safe} -> {info['id'][:12]}... ({info['repo']})")
            return

    if args.phase in ("2", "all"):
        if args.phase == "2":
            index = build_entity_index()
        convert_references_to_guids(index, dry_run=args.dry_run)

    if args.phase in ("3", "all"):
        if args.phase != "all":
            index = build_entity_index()
        regenerate_surnames(index, dry_run=args.dry_run)

    console.print("\n[bold green]Migration complete.[/bold green]")


if __name__ == "__main__":
    main()
