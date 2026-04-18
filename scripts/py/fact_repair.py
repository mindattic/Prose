"""
Phase 7: Repair flagged inconsistencies by updating source JSON files
with the consensus ground truth values.

This is the "write-back" phase -- it takes the corrections identified by score.py
and actually modifies the original JSON entity files to fix the errors.

HOW REPAIR WORKS:
If 4 files say "manufactured_by = Hearthstone Firearms" and 1 file says
"manufactured_by = Arcturus Defense", the repair phase opens that 1 file,
finds the text "Arcturus Defense", and replaces it with "Hearthstone Firearms."

SAFETY: This modifies source files, so ALWAYS use --dry-run first to preview
what would change. Only repairs claims above --min-confidence (default 0.8 = 80%).

Usage: python repair.py [--dry-run] [--min-confidence 0.8] [--limit 100]

CAUTION: This modifies source JSON files. Use --dry-run first.
"""

import json
import sqlite3
import os
from rich.console import Console
from rich.progress import Progress
from constants import DB_PATH

console = Console()


def run_repair(dry_run=True, min_confidence=0.8, limit=None):
    """Repair flagged inconsistencies in source files."""

    console.print("[bold]Phase 7: Repair[/bold]")
    console.print(f"  Dry run: {dry_run}")
    console.print(f"  Min confidence: {min_confidence}")

    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()

    # Fetch all UNREPAIRED flags that meet our confidence threshold.
    # repaired = 0 means we haven't fixed this one yet.
    # confidence >= threshold means we're confident enough in the correction.
    # ORDER BY confidence DESC processes the most confident corrections first.
    # This is intentional: if we hit our limit, we want the best corrections applied.
    c.execute("""
        SELECT f.id, f.triple_id, f.source_file, f.entity_name, f.subject, f.predicate,
               f.incorrect_object, f.correct_object, f.confidence
        FROM flagged_triples f
        WHERE f.repaired = 0 AND f.confidence >= ?
        ORDER BY f.confidence DESC
    """, (min_confidence,))

    flags = c.fetchall()

    # Optionally limit how many repairs to apply (useful for testing or cautious rollouts).
    # Slice the list to only keep the first N items.
    if limit:
        flags = flags[:limit]

    console.print(f"  Repairs to apply: {len(flags)}")

    # Nothing to do if no flags meet our criteria
    if not flags:
        console.print("[green]Nothing to repair at this confidence level.[/green]")
        return

    repaired = 0  # Count of successfully repaired files
    errors = 0    # Count of files that couldn't be repaired

    # Show a progress bar while processing repairs
    with Progress() as progress:
        task = progress.add_task("Repairing...", total=len(flags))

        # Unpack each flag into its component fields for readability.
        # Python lets you unpack a tuple directly in the for loop.
        for flag_id, triple_id, source_file, entity_name, subject, predicate, wrong, right, confidence in flags:
            try:
                # Check if the source file still exists (it might have been moved or deleted)
                if not os.path.exists(source_file):
                    errors += 1
                    progress.update(task, advance=1)
                    continue  # Skip to the next flag

                # Read the JSON file into a Python dictionary
                with open(source_file, "r", encoding="utf-8") as f:
                    data = json.load(f)

                # REPAIR STRATEGY: Convert the entire JSON to a string, find the wrong value,
                # and replace it with the right value.
                # This is a "brute force" approach -- it doesn't need to know which specific
                # field contains the error. It just does a text-level find-and-replace.
                #
                # json.dumps() converts the dict back to a JSON string so we can search it.
                modified = False
                json_str = json.dumps(data)

                # Check if the incorrect value actually exists in the file's JSON
                if wrong in json_str:
                    # Replace only the FIRST occurrence (count=1) to avoid accidentally
                    # changing other unrelated fields that happen to contain the same text.
                    json_str = json_str.replace(wrong, right, 1)

                    # Convert the modified JSON string back into a Python dictionary.
                    # This validates that the replacement didn't break the JSON structure.
                    data = json.loads(json_str)
                    modified = True

                if modified:
                    if dry_run:
                        # In dry-run mode, just show what WOULD be changed (don't touch the file)
                        console.print(f"  [yellow]WOULD REPAIR[/yellow] {entity_name}: {predicate} = {wrong} -> {right}")
                    else:
                        # Actually write the corrected data back to the file.
                        # indent=2 makes the JSON human-readable (not all on one line).
                        # ensure_ascii=False preserves special characters (accents, symbols).
                        with open(source_file, "w", encoding="utf-8") as f:
                            json.dump(data, f, indent=2, ensure_ascii=False)
                            # Add a trailing newline (convention for text files)
                            f.write("\n")

                        # Mark this flag as repaired in the database so we don't fix it again
                        c.execute("UPDATE flagged_triples SET repaired = 1 WHERE id = ?", (flag_id,))
                        repaired += 1

            except Exception as e:
                # If anything goes wrong (file permissions, corrupted JSON, etc.), log and continue.
                # One broken file shouldn't stop the entire repair process.
                console.print(f"  [red]Error repairing {source_file}: {e}[/red]")
                errors += 1

            # Move the progress bar forward
            progress.update(task, advance=1)

    # Save the "repaired = 1" updates to the database
    conn.commit()
    conn.close()

    # Print the final summary.
    # The ternary expression (X if condition else Y) adjusts the wording based on dry_run mode.
    console.print(f"\n[bold green]Repair {'preview' if dry_run else 'complete'}![/bold green]")
    console.print(f"  {'Would repair' if dry_run else 'Repaired'}: {repaired if not dry_run else len(flags)}")
    console.print(f"  Errors: {errors}")


# Only run when executed directly (python repair.py), not when imported
if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(description="Repair flagged inconsistencies in source files")

    # --dry-run: Preview what would be changed without actually modifying any files.
    # This is the SAFE default -- you should always run with --dry-run first.
    parser.add_argument("--dry-run", action="store_true", help="Preview repairs without modifying files")

    # --min-confidence: Only apply repairs where we're at least this confident.
    # 0.8 = 80% means at least 80% of sources must agree on the correct value.
    # Higher = safer (fewer but more reliable repairs).
    parser.add_argument("--min-confidence", type=float, default=0.8, help="Only repair claims above this confidence")

    # --limit: Cap the number of repairs (useful for cautious, incremental fixing)
    parser.add_argument("--limit", type=int, help="Limit number of repairs")

    parser.add_argument("--silent", action="store_true", help="Suppress all console output")
    args = parser.parse_args()
    if args.silent:
        import sys as _sys, os as _os
        _sys.stdout = open(_os.devnull, "w")
        _sys.stderr = open(_os.devnull, "w")


    run_repair(dry_run=args.dry_run, min_confidence=args.min_confidence, limit=args.limit)
