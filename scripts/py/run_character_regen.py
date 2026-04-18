"""
Character Regeneration Pipeline

Runs the full ancestry -> names -> description harmonization pipeline:
  Step 1: generate_ancestry.py           -- Assign genetic ancestry (district-weighted, skips non-humans)
  Step 2: migrate_guids_and_surnames.py  -- Regenerate surnames from ancestry (skips Kyle)
  Step 3: migrate_guids_and_surnames.py  -- Regenerate first names (50% cultural, 50% cosmopolitan, skips Kyle)
  Step 4: harmonize_descriptions.py      -- Adjust physical descriptions to match ancestry (skips Kyle)

Usage:
  python run_character_regen.py                    # Run everything
  python run_character_regen.py --dry-run          # Preview all steps
  python run_character_regen.py --limit 10         # Test with 10 characters
  python run_character_regen.py --step 2           # Start from step 2
  python run_character_regen.py --force            # Overwrite existing data
"""

import subprocess
import sys
import argparse
from pathlib import Path


def run_step(step_num, description, cmd):
    print(f"\n{'='*60}")
    print(f"  Step {step_num}: {description}")
    print(f"  > {' '.join(cmd)}")
    print(f"{'='*60}\n")

    result = subprocess.run(cmd, cwd=str(Path(__file__).parent))
    if result.returncode != 0:
        print(f"\n[FAILED] Step {step_num}: {description} (exit code {result.returncode})")
        sys.exit(result.returncode)


def main():
    parser = argparse.ArgumentParser(description="Full character regeneration pipeline")
    parser.add_argument("--dry-run", action="store_true", help="Preview all steps without changes")
    parser.add_argument("--limit", type=int, help="Limit number of characters per step")
    parser.add_argument("--step", type=int, default=1, choices=[1, 2, 3, 4],
                        help="Start from step N (1=ancestry, 2=surnames, 3=first names, 4=harmonize)")
    parser.add_argument("--force", action="store_true", help="Force overwrite existing data")
    parser.add_argument("--silent", action="store_true", help="Suppress all console output")
    args = parser.parse_args()
    if args.silent:
        import sys as _sys, os as _os
        _sys.stdout = open(_os.devnull, "w")
        _sys.stderr = open(_os.devnull, "w")


    extra = []
    if args.dry_run:
        extra.append("--dry-run")
    if args.limit:
        extra.extend(["--limit", str(args.limit)])
    if args.force:
        extra.append("--force")

    if args.step <= 1:
        run_step(1,
            "Assign genetic ancestry (district-weighted, skips non-humans)",
            [sys.executable, "generate_ancestry.py"] + extra
        )

    if args.step <= 2:
        run_step(2,
            "Regenerate surnames from ancestry (skips Kyle)",
            [sys.executable, "migrate_guids_and_surnames.py", "--phase", "3"] + extra
        )

    if args.step <= 3:
        run_step(3,
            "Regenerate first names -- 50% cultural, 50% cosmopolitan (skips Kyle)",
            [sys.executable, "migrate_guids_and_surnames.py", "--phase", "4"] + extra
        )

    if args.step <= 4:
        run_step(4,
            "Harmonize physical descriptions with ancestry (skips Kyle)",
            [sys.executable, "harmonize_descriptions.py"] + extra
        )

    print(f"\n{'='*60}")
    print(f"  Pipeline complete!")
    print(f"{'='*60}")


if __name__ == "__main__":
    main()
