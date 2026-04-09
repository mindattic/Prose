"""
Character Regeneration Pipeline

Runs the full ancestry -> surname -> description harmonization pipeline:
  Step 1: gen_ancestry.py      — Assign genetic ancestry to all characters (including Kyle)
  Step 2: migrate_to_guids.py  — Regenerate surnames from ancestry (skips Kyle)
  Step 3: harmonize.py         — Adjust physical descriptions to match ancestry (skips Kyle)

Usage:
  python run_regen.py                    # Run everything
  python run_regen.py --dry-run          # Preview all steps
  python run_regen.py --limit 10         # Test with 10 characters
  python run_regen.py --step 2           # Start from step 2
  python run_regen.py --force            # Overwrite existing data
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
    parser.add_argument("--step", type=int, default=1, choices=[1, 2, 3],
                        help="Start from step N (1=ancestry, 2=surnames, 3=harmonize)")
    parser.add_argument("--force", action="store_true", help="Force overwrite existing data")
    args = parser.parse_args()

    extra = []
    if args.dry_run:
        extra.append("--dry-run")
    if args.limit:
        extra.extend(["--limit", str(args.limit)])
    if args.force:
        extra.append("--force")

    if args.step <= 1:
        run_step(1,
            "Assign genetic ancestry (all characters including Kyle)",
            [sys.executable, "gen_ancestry.py"] + extra
        )

    if args.step <= 2:
        run_step(2,
            "Regenerate surnames from ancestry (skips Kyle)",
            [sys.executable, "migrate_to_guids.py", "--phase", "3"] + extra
        )

    if args.step <= 3:
        run_step(3,
            "Harmonize physical descriptions with ancestry (skips Kyle)",
            [sys.executable, "harmonize.py"] + extra
        )

    print(f"\n{'='*60}")
    print(f"  Pipeline complete!")
    print(f"{'='*60}")


if __name__ == "__main__":
    main()
