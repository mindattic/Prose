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
from rich.console import Console
from rich.progress import Progress
from constants import DATA_DIR, REPOS, REFERENCE_FIELDS, SKIP_CHARACTERS

console = Console()

RELATIONSHIP_NAME_FIELD = "name"


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
    "Chinese": [
        "Liang", "Zhao", "Huang", "Xu", "Zheng", "Qian", "Gao",
        "Chen", "Wang", "Zhang", "Liu", "Yang", "Wu", "Zhou",
        "Sun", "Ma", "Zhu", "Hu", "Lin", "Guo", "Luo",
        "Deng", "Xiao", "Feng", "Jiang", "Cheng", "Cai", "Wei",
    ],
    "Korean": [
        "Hwang", "Jeong", "Kwon", "Baek", "Yun", "Rhee", "Choi",
        "Kim", "Park", "Jung", "Kang", "Cho", "Yoon", "Jang",
        "Im", "Oh", "Seo", "Shin", "Song", "Hong", "Moon", "An",
    ],
    "Japanese": [
        "Nakamura", "Kimura", "Tanaka", "Watanabe", "Yamazaki", "Hayashi",
        "Morimoto", "Fujiwara", "Shimizu", "Arakawa", "Ueda", "Takahashi",
        "Ito", "Sato", "Suzuki", "Yamamoto", "Kobayashi", "Matsumoto",
        "Inoue", "Saito", "Okada", "Mori", "Ogawa", "Nishimura",
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
    "Vietnamese": [
        "Tran", "Nguyen", "Pham", "Hoang", "Dang", "Le", "Vo",
        "Do", "Bui", "Ngo", "Ly", "Truong", "Huynh", "Duong", "Dinh",
    ],
    "Filipino": [
        "Mallari", "Reyes", "Dimaculangan", "Bautista", "Pangilinan",
        "Santos", "Cruz", "Ramos", "Aquino", "Torres",
        "Gonzales", "Mendoza", "Dela Cruz", "Villanueva", "Aguilar",
    ],
    "Indonesian": [
        "Widodo", "Sukarno", "Hartono", "Susanto", "Suharto",
        "Wibowo", "Kurniawan", "Setiawan", "Hidayat", "Nugroho",
        "Prasetyo", "Santoso", "Gunawan", "Surya", "Wijaya",
    ],
    "Laotian": [
        "Saechao", "Khamvongsa", "Phommasack", "Chanthavong", "Inthavong",
        "Souvannasane", "Phongsavath", "Vongphasouk", "Keomanivong", "Sisavath",
        "Phommachanh", "Souvannavong", "Rattanavong", "Xayavong", "Douangpanya",
    ],
    "Cambodian": [
        "Sok", "Chan", "Chea", "Hem", "Heng", "Khieu", "Mao",
        "Nhem", "Ouch", "Phan", "Samang", "Seng", "Sor", "Thy",
        "Vann", "Yim", "Chhun", "Kem", "Keo", "Chhoem",
    ],
    "Thai": [
        "Somchai", "Kittisak", "Wattana", "Srisai", "Chaiyaporn",
        "Suwannapoom", "Rattanakorn", "Boonsri", "Kittichai", "Nanthawat",
        "Jitpakdee", "Thammasak", "Wongsawat", "Charoenphol", "Siripong",
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

    char_dir = Path(DATA_DIR) / "people"
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
            if old_name in SKIP_CHARACTERS:
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

            # Get ancestry groups sorted by percentage
            sorted_ancestry = sorted(ancestry.items(), key=lambda x: -x[1])
            if not sorted_ancestry:
                continue

            rng = random.Random(data.get("id", old_name))

            # Determine surname format based on 2226 GLMZ demographics:
            #   55% single surname — simplified over generations
            #   35% hyphenated (two-part) — mixed heritage pride
            #   10% triple-barrel — accumulated across generations
            roll = rng.random()
            if roll < 0.55:
                num_parts = 1
            elif roll < 0.90:
                num_parts = 2
            else:
                num_parts = 3

            # Need at least as many ancestry groups as surname parts
            num_parts = min(num_parts, len(sorted_ancestry))

            # Pick surname from top ancestry groups, weighted by percentage
            top_groups = sorted_ancestry[:min(num_parts + 1, len(sorted_ancestry))]
            group_names = [g for g, _ in top_groups]
            group_weights = [p for _, p in top_groups]

            # Select groups for each surname part (weighted, no replacement)
            selected_groups = []
            available = list(zip(group_names, group_weights))
            for _ in range(num_parts):
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

            if not selected_groups:
                continue

            # Pick a random surname from each group's pool
            surname_parts = []
            for group in selected_groups:
                pool = SURNAME_POOLS.get(group, [])
                if not pool:
                    pool = SURNAME_POOLS.get("Northern European", ["Unknown"])
                surname_parts.append(rng.choice(pool))

            new_surname = "-".join(surname_parts)
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
# Phase 4: Regenerate first names from genetic ancestry
# ══════════════════════════════════════════════════════════════════

# First name pools by ancestry group
FIRST_NAME_POOLS = {
    "Sub-Saharan African": [
        "Kofi", "Amara", "Jelani", "Adaeze", "Kwame", "Nneka", "Obinna",
        "Zuri", "Tendai", "Chidi", "Abiodun", "Folake", "Ifeanyi", "Ngozi",
        "Olumide", "Binta", "Sekou", "Imani", "Jabari", "Makena", "Chinwe",
        "Yemi", "Koffi", "Ama", "Efua", "Tumelo", "Sibusiso", "Thandiwe",
    ],
    "Hispanic/Latin American": [
        "Alejandro", "Valentina", "Santiago", "Camila", "Mateo", "Luciana",
        "Diego", "Isadora", "Rafael", "Marisol", "Emiliano", "Ximena",
        "Joaquín", "Paloma", "Esteban", "Daniela", "Odalys", "Renata",
        "Aurelio", "Esperanza", "Ignacio", "Soledad", "Cruz", "Mariposa",
    ],
    "South Asian": [
        "Arjun", "Priya", "Rohan", "Ananya", "Vikram", "Meera", "Siddharth",
        "Kavya", "Aditya", "Nisha", "Rajan", "Lakshmi", "Kiran", "Devi",
        "Nikhil", "Anjali", "Aarav", "Ishaan", "Saanvi", "Vivaan", "Zoya",
        "Rehana", "Farhan", "Taslima", "Nadira", "Imran", "Farid", "Yasmin",
    ],
    "Northern European": [
        "Erik", "Astrid", "Lars", "Ingrid", "Bjorn", "Freya", "Soren",
        "Elsa", "Magnus", "Sigrid", "Axel", "Linnea", "Leif", "Thora",
        "Viggo", "Britt", "Gunnar", "Solveig", "Ivar", "Dagny", "Torsten",
        "Liv", "Ulf", "Ragna", "Stellan", "Tuva",
    ],
    "Chinese": [
        "Wei", "Mei", "Jian", "Xiu", "Hao", "Ling", "Zhen", "Yue",
        "Kai", "Fang", "Tao", "Qin", "Lei", "Hua", "Feng", "Lan",
        "Bao", "Shu", "Rui", "Xia", "Jun", "Hong", "Ning", "Yan",
    ],
    "Korean": [
        "Jihoon", "Minji", "Seojun", "Yuna", "Hyunwoo", "Soojin",
        "Taehyung", "Eunji", "Minho", "Haeun", "Jisoo", "Doyeon",
        "Sunwoo", "Chaeyoung", "Wonjin", "Nayeon", "Seonghwa", "Dahyun",
    ],
    "Japanese": [
        "Haruto", "Sakura", "Ren", "Yuki", "Kaito", "Hana", "Soma",
        "Aoi", "Riku", "Akira", "Takeshi", "Hikaru", "Yui", "Kenji",
        "Mio", "Naomi", "Shin", "Hinata", "Daichi", "Misaki",
    ],
    "Eastern European": [
        "Aleksei", "Katarzyna", "Dmitri", "Milena", "Pavel", "Anya",
        "Szymon", "Ivana", "Marek", "Tatiana", "Andrei", "Zofia",
        "Vladislav", "Daria", "Mikhail", "Svetlana", "Leszek", "Bogdana",
    ],
    "Vietnamese": [
        "Minh", "Linh", "Duc", "Thao", "Hieu", "Mai", "Bao", "Ngoc",
        "Tuan", "Phuong", "An", "Thi", "Nam", "Hanh", "Quang", "Lan",
    ],
    "Filipino": [
        "Miguel", "Dalisay", "Andres", "Amihan", "Rafael", "Ligaya",
        "Emilio", "Bituin", "Tala", "Mayumi", "Bayani", "Diwa",
        "Makisig", "Lualhati", "Kidlat", "Hiraya", "Dakila", "Malaya",
    ],
    "Indonesian": [
        "Agung", "Dewi", "Budi", "Siti", "Rizal", "Putri", "Surya",
        "Ayu", "Wahyu", "Indah", "Eko", "Ratna", "Bayu", "Wulan",
        "Galih", "Lestari", "Arief", "Citra",
    ],
    "Laotian": [
        "Kham", "Souliya", "Bounmy", "Chanthou", "Vilay", "Keo",
        "Somphone", "Boupha", "Thongdy", "Sengchan", "Phailin", "Dokkeo",
        "Anousone", "Vilayphone", "Manoly", "Souphavanh",
    ],
    "Cambodian": [
        "Dara", "Bopha", "Rith", "Channary", "Sokha", "Maly", "Vibol",
        "Sreymom", "Pheakdey", "Kosal", "Chantrea", "Sopheap",
        "Narith", "Kunthea", "Piseth", "Sokunthea",
    ],
    "Thai": [
        "Somchai", "Ploy", "Nattapong", "Malai", "Kittisak", "Nong",
        "Chai", "Siriwan", "Worawut", "Kulap", "Anong", "Boonmee",
        "Narong", "Siriporn", "Prawit", "Duangkamol",
    ],
    "Middle Eastern": [
        "Amir", "Yasmin", "Reza", "Layla", "Dariush", "Noor", "Khalil",
        "Farah", "Tariq", "Samira", "Omid", "Leila", "Cyrus", "Soraya",
        "Ibrahim", "Hana", "Rashid", "Zahra",
    ],
    "Pacific Islander": [
        "Tane", "Moana", "Manu", "Leilani", "Sione", "Aroha", "Kalani",
        "Teuila", "Maui", "Hinano", "Anahera", "Nikau", "Tiare", "Hemi",
        "Ioane", "Fetaui", "Alofa", "Tavita",
    ],
    "Indigenous American": [
        "Ahanu", "Kaya", "Takoda", "Aiyana", "Chayton", "Elu", "Koda",
        "Sani", "Tallulah", "Onida", "Aponi", "Istas", "Kitchi", "Odina",
        "Chenoa", "Nayeli", "Kohana", "Mika",
    ],
    "Central Asian": [
        "Alisher", "Gulnara", "Timur", "Aizhan", "Ruslan", "Dinara",
        "Bakhtiyor", "Zarina", "Temur", "Malika", "Sardor", "Nigora",
        "Azamat", "Kamila", "Beknur", "Aidana",
    ],
    "North African": [
        "Youssef", "Amina", "Karim", "Fatima", "Tarik", "Nadia",
        "Mehdi", "Salma", "Rachid", "Houda", "Ilyas", "Safiya",
        "Driss", "Laila", "Mourad", "Ghita",
    ],
}

# Cosmopolitan names — cross-cultural names common in 2226 GLMZ
# Used 50% of the time regardless of ancestry
COSMOPOLITAN_NAMES = [
    "Kai", "Nova", "Soren", "Zara", "Luca", "Mika", "Rio", "Sage",
    "Arden", "Quinn", "Remi", "Idris", "Kira", "Atlas", "Cleo",
    "Orion", "Juno", "Sol", "Phoenix", "Sterling", "Onyx", "Echo",
    "Vesper", "Ash", "Lyric", "Wren", "Rowan", "Ember", "Kit",
    "Nyx", "Zen", "Aero", "Cipher", "Delta", "Flux", "Rune",
    "Sable", "Tarn", "Vega", "Zephyr", "Indigo", "Haze", "Drift",
    "Lark", "Reed", "Slate", "Briar", "Frost", "Vale", "Arc",
]


def regenerate_first_names(index, dry_run=False):
    """Regenerate character first names: 50% ancestry-derived, 50% cosmopolitan."""
    console.print("\n[bold]Phase 4: Regenerating first names from genetic ancestry[/bold]")

    char_dir = Path(DATA_DIR) / "people"
    files = sorted(glob.glob(str(char_dir / "*.json")))
    updated = 0

    for fp in files:
        try:
            with open(fp, "r", encoding="utf-8") as f:
                data = json.load(f)
            if not isinstance(data, dict):
                continue

            name = data.get("name", "")
            if name in SKIP_CHARACTERS:
                continue

            ancestry = data.get("genetic_ancestry", {})
            if not ancestry:
                continue

            # Split current name into first name(s) and surname
            words = name.split()
            if len(words) < 2:
                continue

            # Surname is the last token (possibly hyphenated)
            surname = words[-1]
            # Everything before surname is first/middle names
            old_first = " ".join(words[:-1])

            rng = random.Random(data.get("id", name) + "_first")

            # 50% cosmopolitan, 50% ancestry-derived
            if rng.random() < 0.50:
                new_first = rng.choice(COSMOPOLITAN_NAMES)
            else:
                # Pick from top ancestry group's name pool
                sorted_ancestry = sorted(ancestry.items(), key=lambda x: -x[1])
                # Try top 2 groups, pick one weighted by percentage
                top = sorted_ancestry[:min(2, len(sorted_ancestry))]
                groups = [g for g, _ in top]
                weights = [p for _, p in top]
                total = sum(weights)
                r = rng.uniform(0, total)
                cumulative = 0
                chosen_group = groups[0]
                for g, w in zip(groups, weights):
                    cumulative += w
                    if r <= cumulative:
                        chosen_group = g
                        break

                pool = FIRST_NAME_POOLS.get(chosen_group, COSMOPOLITAN_NAMES)
                new_first = rng.choice(pool)

            new_name = f"{new_first} {surname}"

            if new_name == name:
                continue

            if dry_run:
                print(f"  {name} -> {new_name}")
                updated += 1
                continue

            data["name"] = new_name

            with open(fp, "w", encoding="utf-8") as f:
                json.dump(data, f, indent=2, ensure_ascii=False)
            updated += 1

        except Exception as e:
            console.print(f"  [red]Error on {fp}: {e}[/red]")

    console.print(f"  First names regenerated: {updated}")


# ══════════════════════════════════════════════════════════════════
# Main
# ══════════════════════════════════════════════════════════════════

def main():
    import argparse
    parser = argparse.ArgumentParser(description="Migrate references to GUIDs + regenerate surnames")
    parser.add_argument("--phase", type=str, default="all", choices=["1", "2", "3", "4", "all"])
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()

    if args.phase in ("1", "all"):
        index = build_entity_index()

        if args.phase == "1":
            console.print("\n[yellow]Phase 1 complete (index built). Run --phase 2 to convert references.[/yellow]")
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

    if args.phase in ("4", "all"):
        if args.phase != "all":
            index = build_entity_index()
        regenerate_first_names(index, dry_run=args.dry_run)

    console.print("\n[bold green]Migration complete.[/bold green]")


if __name__ == "__main__":
    main()
