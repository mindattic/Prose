"""
Genetic Ancestry Generator — Three-tier system

Assigns three levels of ancestry to all characters:
  genetic_ancestry  — broad regions (East Asian, Northern European, etc.)
  ancestry_detail   — nested: region → sub-region → nationality with percentages

Example output:
  genetic_ancestry: { "Northern European": 45.0, "East Asian": 35.0 }
  ancestry_detail: {
    "Northern European": {
      "Scandinavian": { "Norwegian": 25.0, "Swedish": 7.5 },
      "Germanic": { "German": 12.5 }
    },
    "East Asian": {
      "Chinese": { "Cantonese": 20.0, "Fujianese": 15.0 }
    }
  }

Usage:
  python generate_ancestry.py                  # Process all people
  python generate_ancestry.py --limit 10       # Test with 10
  python generate_ancestry.py --dry-run        # Preview without writing
  python generate_ancestry.py --force          # Overwrite existing ancestry
"""

import json
import glob
import random
import os
from pathlib import Path
from constants import DATA_DIR, NON_HUMAN_SPECIES

# ══════════════════════════════════════════════════════════════════
# Tier 1: Broad regional weights (what % of GLMZ genetics)
# ══════════════════════════════════════════════════════════════════

REGION_WEIGHTS = {
    "Sub-Saharan African":     0.20,
    "Hispanic/Latin American":  0.18,
    "South Asian":              0.12,
    "East Asian":               0.11,
    "Southeast Asian":          0.09,
    "Northern European":        0.08,
    "Eastern European":         0.07,
    "Middle Eastern":           0.05,
    "Indigenous American":      0.04,
    "Pacific Islander":         0.02,
    "Central Asian":            0.02,
    "North African":            0.02,
}

assert abs(sum(REGION_WEIGHTS.values()) - 1.0) < 0.01

# ══════════════════════════════════════════════════════════════════
# Tier 2 → Tier 3: Sub-regions and their nationalities
# Structure: region → { sub_region: { nationality: relative_weight } }
# Weights are relative within each level (don't need to sum to 1.0,
# they're normalized during generation)
# ══════════════════════════════════════════════════════════════════

ANCESTRY_TREE = {
    "Sub-Saharan African": {
        "West African": {
            "Nigerian": 30, "Ghanaian": 20, "Senegalese": 15,
            "Malian": 10, "Ivorian": 8, "Guinean": 7,
            "Sierra Leonean": 5, "Liberian": 5,
        },
        "East African": {
            "Ethiopian": 25, "Kenyan": 20, "Somali": 20,
            "Tanzanian": 15, "Ugandan": 10, "Rwandan": 10,
        },
        "Southern African": {
            "South African": 40, "Zimbabwean": 20, "Mozambican": 15,
            "Zambian": 15, "Botswanan": 10,
        },
        "Central African": {
            "Congolese": 40, "Cameroonian": 25, "Angolan": 20,
            "Gabonese": 15,
        },
    },
    "Hispanic/Latin American": {
        "Mexican": {
            "Mexican": 100,
        },
        "Central American": {
            "Salvadoran": 30, "Guatemalan": 25, "Honduran": 20,
            "Nicaraguan": 15, "Costa Rican": 10,
        },
        "Caribbean": {
            "Puerto Rican": 35, "Cuban": 25, "Dominican": 25,
            "Haitian": 15,
        },
        "South American": {
            "Colombian": 25, "Peruvian": 20, "Brazilian": 20,
            "Venezuelan": 15, "Ecuadorian": 10, "Chilean": 5,
            "Argentine": 5,
        },
    },
    "South Asian": {
        "Indian": {
            "Tamil": 15, "Bengali": 20, "Punjabi": 15,
            "Gujarati": 12, "Marathi": 10, "Telugu": 10,
            "Kannada": 8, "Malayali": 10,
        },
        "Bangladeshi": {
            "Bangladeshi": 100,
        },
        "Pakistani": {
            "Punjabi Pakistani": 40, "Sindhi": 25, "Pashtun": 20,
            "Baloch": 15,
        },
        "Sri Lankan": {
            "Sinhalese": 60, "Tamil Sri Lankan": 40,
        },
        "Nepali": {
            "Nepali": 100,
        },
    },
    "East Asian": {
        "Chinese": {
            "Cantonese": 25, "Fujianese": 15, "Shanghainese": 20,
            "Mandarin Northern": 20, "Sichuanese": 10, "Hakka": 10,
        },
        "Korean": {
            "Korean": 100,
        },
        "Japanese": {
            "Japanese": 100,
        },
    },
    "Southeast Asian": {
        "Vietnamese": {
            "Vietnamese": 100,
        },
        "Filipino": {
            "Tagalog": 40, "Cebuano": 25, "Ilocano": 15,
            "Bicolano": 10, "Waray": 10,
        },
        "Indonesian": {
            "Javanese": 45, "Sundanese": 20, "Balinese": 15,
            "Sumatran": 20,
        },
        "Thai": {
            "Thai": 100,
        },
        "Laotian": {
            "Laotian": 100,
        },
        "Cambodian": {
            "Khmer": 100,
        },
    },
    "Northern European": {
        "Scandinavian": {
            "Norwegian": 25, "Swedish": 30, "Danish": 20,
            "Finnish": 15, "Icelandic": 10,
        },
        "Germanic": {
            "German": 55, "Austrian": 20, "Swiss German": 15,
            "Dutch": 10,
        },
        "British Isles": {
            "English": 40, "Scottish": 20, "Irish": 25,
            "Welsh": 15,
        },
    },
    "Eastern European": {
        "Slavic West": {
            "Polish": 45, "Czech": 25, "Slovak": 15,
            "Sorbian": 15,
        },
        "Slavic East": {
            "Russian": 35, "Ukrainian": 35, "Belarusian": 30,
        },
        "Slavic South": {
            "Serbian": 25, "Croatian": 25, "Bosnian": 20,
            "Slovenian": 15, "Bulgarian": 15,
        },
        "Magyar-Romanian": {
            "Hungarian": 50, "Romanian": 50,
        },
    },
    "Middle Eastern": {
        "Persian": {
            "Iranian": 70, "Tajik Persian": 15, "Afghan Dari": 15,
        },
        "Arab Levantine": {
            "Lebanese": 25, "Syrian": 25, "Palestinian": 25,
            "Jordanian": 25,
        },
        "Arab Gulf": {
            "Iraqi": 40, "Yemeni": 30, "Kuwaiti": 15,
            "Emirati": 15,
        },
        "Kurdish": {
            "Kurdish": 100,
        },
    },
    "Indigenous American": {
        "Great Lakes Nations": {
            "Ojibwe": 35, "Potawatomi": 30, "Menominee": 20,
            "Ho-Chunk": 15,
        },
        "Plains Nations": {
            "Lakota": 35, "Cherokee": 30, "Choctaw": 20,
            "Muscogee": 15,
        },
        "Mesoamerican": {
            "Nahua": 40, "Maya": 35, "Zapotec": 15,
            "Mixtec": 10,
        },
        "Andean": {
            "Quechua": 50, "Aymara": 30, "Mapuche": 20,
        },
    },
    "Pacific Islander": {
        "Polynesian": {
            "Samoan": 30, "Tongan": 25, "Hawaiian": 20,
            "Tuvaluan": 15, "Tokelauan": 10,
        },
        "Melanesian": {
            "Fijian": 50, "Papua New Guinean": 30, "Ni-Vanuatu": 20,
        },
        "Micronesian": {
            "Marshallese": 35, "Kiribati": 35, "Palauan": 30,
        },
    },
    "Central Asian": {
        "Turkic": {
            "Kazakh": 30, "Uzbek": 25, "Kyrgyz": 20,
            "Turkmen": 15, "Uyghur": 10,
        },
        "Mongolic": {
            "Mongolian": 60, "Buryat": 25, "Kalmyk": 15,
        },
        "Iranian Central": {
            "Tajik": 60, "Hazara": 40,
        },
    },
    "North African": {
        "Maghreb": {
            "Moroccan": 30, "Algerian": 25, "Tunisian": 20,
            "Libyan": 10, "Mauritanian": 15,
        },
        "Nile Valley": {
            "Egyptian": 60, "Sudanese": 40,
        },
        "Amazigh": {
            "Berber": 50, "Tuareg": 30, "Kabyle": 20,
        },
    },
}

# ══════════════════════════════════════════════════════════════════
# District demographic skew
# ══════════════════════════════════════════════════════════════════

DISTRICT_MODIFIERS = {
    "The Spires": {
        "Northern European": 1.6, "East Asian": 1.4,
        "South Asian": 1.2,
        "Sub-Saharan African": 0.7, "Pacific Islander": 0.6,
    },
    "Meridian Core": {
        "Northern European": 1.3, "East Asian": 1.2,
        "Eastern European": 1.1,
    },
    "The Circuit": {
        "East Asian": 1.4, "South Asian": 1.3,
        "Southeast Asian": 1.2,
    },
    "The Shelf": {
        "South Asian": 1.3, "Southeast Asian": 1.3,
        "Sub-Saharan African": 1.2,
        "Northern European": 0.8,
    },
    "The Laceworks": {
        "Eastern European": 1.3, "Hispanic/Latin American": 1.2,
        "East Asian": 1.1, "Southeast Asian": 1.2,
    },
    "Old Harbor": {
        "Eastern European": 1.3, "Hispanic/Latin American": 1.2,
        "Sub-Saharan African": 1.1, "Middle Eastern": 1.2,
        "North African": 1.3,
    },
    "The Underworld": {
        "Hispanic/Latin American": 1.2, "Sub-Saharan African": 1.2,
        "Pacific Islander": 1.3, "Indigenous American": 1.3,
    },
    "Nomadic": {},
}


def weighted_pick(items, rng, count=1):
    """Pick count items from {name: weight} dict, weighted, no replacement."""
    picked = []
    avail = list(items.items())
    for _ in range(min(count, len(avail))):
        if not avail:
            break
        total = sum(w for _, w in avail)
        r = rng.uniform(0, total)
        cum = 0
        for i, (name, w) in enumerate(avail):
            cum += w
            if r <= cum:
                picked.append(name)
                avail.pop(i)
                break
    return picked


def distribute_percentage(pct, items, rng):
    """Distribute a percentage among items using Dirichlet-like randomness."""
    if len(items) == 1:
        return {items[0]: pct}

    raw = [rng.gammavariate(2.0, 1.0) for _ in items]
    raw[0] *= rng.uniform(1.3, 2.0)
    total = sum(raw)

    result = {}
    for item, rv in zip(items, raw):
        result[item] = round(rv / total * pct, 1)

    # Fix rounding
    diff = round(pct - sum(result.values()), 1)
    result[items[0]] = round(result[items[0]] + diff, 1)

    return {k: v for k, v in result.items() if v > 0}


def generate_ancestry(seed, district=None):
    """
    Generate three-tier ancestry for one character.
    Returns (genetic_ancestry, ancestry_detail) tuple.
    """
    rng = random.Random(seed)

    num_regions = rng.randint(3, 6)

    # Apply district modifiers
    base_weights = dict(REGION_WEIGHTS)
    if district and district in DISTRICT_MODIFIERS:
        for group, mult in DISTRICT_MODIFIERS[district].items():
            if group in base_weights:
                base_weights[group] *= mult
        total_w = sum(base_weights.values())
        base_weights = {k: v / total_w for k, v in base_weights.items()}

    # Tier 1: Pick broad regions
    selected_regions = weighted_pick(base_weights, rng, num_regions)

    # Assign percentages to regions
    raw = [rng.gammavariate(2.0, 1.0) for _ in selected_regions]
    if len(raw) >= 2:
        raw[0] *= rng.uniform(1.5, 3.0)
        raw[1] *= rng.uniform(1.2, 2.0)
    total = sum(raw)
    region_pcts = [round(r / total * 100, 1) for r in raw]
    diff = round(100.0 - sum(region_pcts), 1)
    region_pcts[0] = round(region_pcts[0] + diff, 1)

    broad = dict(sorted(
        zip(selected_regions, region_pcts),
        key=lambda x: -x[1]
    ))
    broad = {k: v for k, v in broad.items() if v > 0}

    # Tier 2 + 3: Break each region into sub-regions and nationalities
    detail = {}
    for region, region_pct in broad.items():
        tree = ANCESTRY_TREE.get(region, {})
        if not tree:
            continue

        # Pick 1-3 sub-regions from this region
        sub_region_weights = {sr: sum(nats.values()) for sr, nats in tree.items()}
        num_subs = min(rng.randint(1, 3), len(sub_region_weights))
        selected_subs = weighted_pick(sub_region_weights, rng, num_subs)

        # Distribute region percentage among sub-regions
        sub_pcts = distribute_percentage(region_pct, selected_subs, rng)

        region_detail = {}
        for sub_region, sub_pct in sub_pcts.items():
            nationalities = tree.get(sub_region, {})
            if not nationalities:
                continue

            # Pick 1-2 nationalities from this sub-region
            num_nats = min(rng.randint(1, 2), len(nationalities))
            selected_nats = weighted_pick(nationalities, rng, num_nats)

            # Distribute sub-region percentage among nationalities
            nat_pcts = distribute_percentage(sub_pct, selected_nats, rng)

            region_detail[sub_region] = nat_pcts

        if region_detail:
            detail[region] = region_detail

    return broad, detail


def main():
    import argparse
    parser = argparse.ArgumentParser(description="Generate genetic ancestry for people")
    parser.add_argument("--limit", type=int, help="Limit number of people")
    parser.add_argument("--dry-run", action="store_true", help="Preview without writing")
    parser.add_argument("--force", action="store_true", help="Overwrite existing ancestry")
    args = parser.parse_args()

    char_dir = Path(DATA_DIR) / "people"
    files = sorted(glob.glob(str(char_dir / "*.json")))

    if args.limit:
        files = files[:args.limit]

    updated = 0
    skipped = 0

    for fp in files:
        try:
            with open(fp, "r", encoding="utf-8") as f:
                data = json.load(f)

            if not isinstance(data, dict):
                continue

            species = data.get("species", "human").lower().strip()
            if species in NON_HUMAN_SPECIES:
                skipped += 1
                continue

            if "ancestry_detail" in data and data["ancestry_detail"] and not args.force:
                skipped += 1
                continue

            seed = data.get("id", data.get("name", os.path.basename(fp)))
            district = data.get("district", None)
            broad, detail = generate_ancestry(seed, district=district)

            if args.dry_run:
                name = data.get("name", os.path.basename(fp))
                print(f"{name}:")
                for region, pct in broad.items():
                    print(f"  {region}: {pct}%")
                    if region in detail:
                        for sub, nats in detail[region].items():
                            for nat, npct in nats.items():
                                print(f"    {sub} > {nat}: {npct}%")
                print()
                updated += 1
                continue

            data["genetic_ancestry"] = broad
            data["ancestry_detail"] = detail

            with open(fp, "w", encoding="utf-8") as f:
                json.dump(data, f, indent=2, ensure_ascii=False)

            updated += 1

        except Exception as e:
            print(f"Error on {fp}: {e}")

    print(f"\nDone: {updated} people {'previewed' if args.dry_run else 'updated'}, {skipped} skipped")


if __name__ == "__main__":
    main()
