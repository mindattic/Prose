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
from constants import DATA_DIR, NON_HUMAN_SPECIES

# ── Population-level ancestry weights for GLMZ 2226 ──────────────────────
# Based on real projections:
# - Chicago 2025: 32% White, 30% Hispanic, 27% Black, 7% Asian, 4% multi
# - US 2060: No majority, Hispanic ~30%, multiracial doubling every generation
# - Climate displacement by 2100: 2B refugees, mostly coastal Asia/Africa/Americas
# - 200 years of intermarriage in compressed megacity housing
# - Mass driver + high-speed rail makes intercontinental travel trivial,
#   accelerating migration far beyond refugee patterns alone
#
# By 2226, everyone is mixed. These are the AVERAGE genetic contributions
# across the population, with wide individual variation.
# East Asian and Southeast Asian split into distinct cultural groups —
# these are separate peoples who strongly identify as such.

ANCESTRY_WEIGHTS = {
    "Sub-Saharan African":     0.20,  # Chicago's existing Black population + African climate refugees
    "Hispanic/Latin American":  0.18,  # Chicago's 30% Hispanic base + Central/South American displacement
    "South Asian":              0.12,  # Bangladeshi/Indian coastal refugees — massive displacement (raised from 9%)
    "Northern European":        0.08,  # Existing Midwest populations, declining birth rates over 200 years
    "Chinese":                  0.07,  # Largest single source — Shanghai/Guangzhou/Shenzhen coastal displacement
    "Eastern European":         0.07,  # Economic migration, existing Great Lakes populations (Polish, Russian)
    "Middle Eastern":           0.05,  # Climate and conflict displacement
    "Vietnamese":               0.03,  # Major coastal displacement + HSR Pacific Rim corridor
    "Indigenous American":      0.03,  # Canadian/Great Lakes First Nations + Latin American indigenous
    "Korean":                   0.02,  # Smaller but highly mobile, tech-connected diaspora
    "Japanese":                 0.02,  # Island nation displacement, strong cultural preservation
    "Filipino":                 0.02,  # Archipelago — massive sea-level displacement
    "Indonesian":               0.02,  # Largest SE Asian country, island chain displacement
    "Pacific Islander":         0.02,  # Entire island nations displaced (small source populations)
    "Central Asian":            0.02,  # Mass driver opens Silk Road migration corridor
    "North African":            0.02,  # Mediterranean climate displacement
    "Laotian":                  0.01,  # Mekong displacement + HSR connectivity
    "Cambodian":                0.01,  # Mekong/coastal displacement + HSR connectivity
    "Thai":                     0.01,  # Coastal displacement, Bangkok flooding
}

# Verify weights sum to 1.0
assert abs(sum(ANCESTRY_WEIGHTS.values()) - 1.0) < 0.001, f"Weights sum to {sum(ANCESTRY_WEIGHTS.values())}"

# ── District demographic skew ─────────────────────────────────────────────
# Even 200 years out, districts have settlement patterns. These multipliers
# tilt the population weights slightly — not segregation, just statistical echoes
# of who settled where first. After applying, weights are re-normalized to 1.0.
DISTRICT_MODIFIERS = {
    "The Spires": {
        # Corpo elite — overrepresents historically wealthy diaspora
        "Northern European": 1.6, "Chinese": 1.4, "Japanese": 1.4,
        "Korean": 1.3, "South Asian": 1.2,
        # Underrepresents climate refugee descendants
        "Sub-Saharan African": 0.7, "Pacific Islander": 0.6,
    },
    "Meridian Core": {
        # Business center — similar to Spires but less extreme
        "Northern European": 1.3, "Chinese": 1.2, "Japanese": 1.2,
        "Eastern European": 1.1,
    },
    "The Circuit": {
        # Tech district — overrepresents tech-connected diaspora
        "Chinese": 1.3, "Korean": 1.4, "South Asian": 1.3,
        "Japanese": 1.3, "Vietnamese": 1.2,
    },
    "The Shelf": {
        # Working class — overrepresents climate refugee descendants
        "South Asian": 1.3, "Vietnamese": 1.3, "Filipino": 1.3,
        "Indonesian": 1.2, "Sub-Saharan African": 1.2,
        "Laotian": 1.3, "Cambodian": 1.3, "Thai": 1.2,
        # Underrepresents corpo groups
        "Northern European": 0.8,
    },
    "The Laceworks": {
        # Manufacturing/craft district
        "Eastern European": 1.3, "Hispanic/Latin American": 1.2,
        "Chinese": 1.1, "Filipino": 1.2,
    },
    "Old Harbor": {
        # Historic port — reflects waves of immigration history
        "Eastern European": 1.3, "Hispanic/Latin American": 1.2,
        "Sub-Saharan African": 1.1, "Middle Eastern": 1.2,
        "North African": 1.3,
    },
    "The Underworld": {
        # Underground/marginalized communities
        "Hispanic/Latin American": 1.2, "Sub-Saharan African": 1.2,
        "Pacific Islander": 1.3, "Indigenous American": 1.3,
    },
    "Nomadic": {
        # No fixed district — maximally diverse, no skew
    },
}


def generate_ancestry(seed_name: str, district: str = None) -> dict:
    """
    Generate a realistic genetic ancestry mix for one character.
    Uses the character's name as a random seed for deterministic results.
    Optionally adjusts weights based on district demographics.

    Each person gets 3-6 ancestry components (nobody is 19-way split evenly).
    Components are drawn from the population weights but with significant
    individual variation — some people are 60% one group, others are 20/20/20/20/20.
    """
    rng = random.Random(seed_name)

    # How many ancestry components this person has (3-6)
    num_components = rng.randint(3, 6)

    # Apply district modifiers if applicable
    base_weights = dict(ANCESTRY_WEIGHTS)
    if district and district in DISTRICT_MODIFIERS:
        modifiers = DISTRICT_MODIFIERS[district]
        for group, mult in modifiers.items():
            if group in base_weights:
                base_weights[group] *= mult
        # Re-normalize to 1.0
        total_w = sum(base_weights.values())
        base_weights = {k: v / total_w for k, v in base_weights.items()}

    # Select which groups appear — weighted by population frequency
    groups = list(base_weights.keys())
    weights = list(base_weights.values())

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

            # Skip non-human characters (AIs, androids, robots)
            species = data.get("species", "human").lower().strip()
            if species in NON_HUMAN_SPECIES:
                skipped += 1
                continue

            # Skip if already has ancestry (unless --force)
            if "genetic_ancestry" in data and data["genetic_ancestry"] and not args.force:
                skipped += 1
                continue

            seed = data.get("id", data.get("name", os.path.basename(fp)))
            district = data.get("district", None)
            ancestry = generate_ancestry(seed, district=district)

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
