"""
Genetic Ancestry Generator

Assigns realistic genetic ancestry percentages to all characters based on
demographic projections for a 2226 Great Lakes megacity. Each character gets
a unique mix with randomized variation around population-level weights.

Ancestry is INDEPENDENT of surname — a person named Szczypiński-Lautoa might
be genetically 40% Hispanic and 30% East Asian. Surnames follow rarity-selection
(rare names propagate), genetics follow demographic reality.

Usage:
  python gen_ancestry.py                  # Process all characters
  python gen_ancestry.py --limit 10       # Test with 10
  python gen_ancestry.py --dry-run        # Preview without writing
  python gen_ancestry.py --force          # Overwrite existing ancestry
"""

import json
import glob
import random
import os
from pathlib import Path
from dotenv import load_dotenv

load_dotenv()

DATA_DIR = os.getenv("DATA_DIR", "../../engine/data")

# ── Population-level ancestry weights for GLMZ 2226 ──────────────────────
# Based on real projections:
# - Chicago 2025: 32% White, 30% Hispanic, 27% Black, 7% Asian, 4% multi
# - US 2060: No majority, Hispanic ~30%, multiracial doubling every generation
# - Climate displacement by 2100: 2B refugees, mostly coastal Asia/Africa/Americas
# - 200 years of intermarriage in compressed megacity housing
#
# By 2226, everyone is mixed. These are the AVERAGE genetic contributions
# across the population, with wide individual variation.

ANCESTRY_WEIGHTS = {
    "Sub-Saharan African":     0.20,  # Chicago's existing Black population + African climate refugees
    "Hispanic/Latin American":  0.18,  # Chicago's 30% Hispanic base + Central/South American displacement
    "Northern European":        0.12,  # Existing Midwest populations, declining but persistent
    "East Asian":               0.12,  # Massive Chinese/Korean/Japanese coastal displacement
    "South Asian":              0.09,  # Bangladeshi/Indian coastal refugees
    "Eastern European":         0.07,  # Economic migration, existing Great Lakes populations (Polish, Russian)
    "Southeast Asian":          0.06,  # Filipino/Vietnamese/Indonesian displacement
    "Middle Eastern":           0.05,  # Climate and conflict displacement
    "Pacific Islander":         0.04,  # Entire island nations displaced
    "Indigenous American":      0.03,  # Canadian/Great Lakes First Nations + Latin American indigenous
    "Central Asian":            0.02,  # Smaller but present (Kazakh, Mongolian, etc.)
    "North African":            0.02,  # Mediterranean climate displacement
}

# Verify weights sum to 1.0
assert abs(sum(ANCESTRY_WEIGHTS.values()) - 1.0) < 0.001, f"Weights sum to {sum(ANCESTRY_WEIGHTS.values())}"


def generate_ancestry(seed_name: str) -> dict:
    """
    Generate a realistic genetic ancestry mix for one character.
    Uses the character's name as a random seed for deterministic results.

    Each person gets 3-6 ancestry components (nobody is 12-way split evenly).
    Components are drawn from the population weights but with significant
    individual variation — some people are 60% one group, others are 20/20/20/20/20.
    """
    rng = random.Random(seed_name)

    # How many ancestry components this person has (3-6)
    num_components = rng.randint(3, 6)

    # Select which groups appear — weighted by population frequency
    groups = list(ANCESTRY_WEIGHTS.keys())
    weights = list(ANCESTRY_WEIGHTS.values())

    # Pick components without replacement, weighted
    selected = []
    remaining_groups = list(zip(groups, weights))
    for _ in range(min(num_components, len(remaining_groups))):
        total = sum(w for _, w in remaining_groups)
        r = rng.uniform(0, total)
        cumulative = 0
        for i, (g, w) in enumerate(remaining_groups):
            cumulative += w
            if r <= cumulative:
                selected.append(g)
                remaining_groups.pop(i)
                break

    # Assign random percentages that sum to 100
    # Use a Dirichlet-like distribution: generate random values, normalize
    raw = [rng.gammavariate(2.0, 1.0) for _ in selected]

    # Give the first 1-2 components extra weight (people tend to have
    # 1-2 dominant ancestries, not an even spread)
    if len(raw) >= 2:
        raw[0] *= rng.uniform(1.5, 3.0)
        raw[1] *= rng.uniform(1.2, 2.0)

    total = sum(raw)
    percentages = [round(r / total * 100, 1) for r in raw]

    # Fix rounding to sum to exactly 100
    diff = round(100.0 - sum(percentages), 1)
    percentages[0] = round(percentages[0] + diff, 1)

    # Build the result dict, sorted by percentage descending
    ancestry = dict(sorted(
        zip(selected, percentages),
        key=lambda x: -x[1]
    ))

    # Drop any that rounded to 0
    ancestry = {k: v for k, v in ancestry.items() if v > 0}

    return ancestry


def main():
    import argparse
    parser = argparse.ArgumentParser(description="Generate genetic ancestry for characters")
    parser.add_argument("--limit", type=int, help="Limit number of characters")
    parser.add_argument("--dry-run", action="store_true", help="Preview without writing")
    parser.add_argument("--force", action="store_true", help="Overwrite existing ancestry")
    args = parser.parse_args()

    char_dir = Path(DATA_DIR) / "characters"
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

            # Skip if already has ancestry (unless --force)
            if "genetic_ancestry" in data and data["genetic_ancestry"] and not args.force:
                skipped += 1
                continue

            name = data.get("name", os.path.basename(fp))
            ancestry = generate_ancestry(name)

            if args.dry_run:
                print(f"{name}:")
                for group, pct in ancestry.items():
                    print(f"  {group}: {pct}%")
                print()
                updated += 1
                continue

            data["genetic_ancestry"] = ancestry

            with open(fp, "w", encoding="utf-8") as f:
                json.dump(data, f, indent=2, ensure_ascii=False)

            updated += 1

        except Exception as e:
            print(f"Error on {fp}: {e}")

    print(f"\nDone: {updated} characters {'previewed' if args.dry_run else 'updated'}, {skipped} skipped (already had ancestry)")


if __name__ == "__main__":
    main()
