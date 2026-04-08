"""
Phase 7: Repair flagged inconsistencies by updating source JSON files
with the consensus ground truth values.

Usage: python repair.py [--dry-run] [--min-confidence 0.8] [--limit 100]

CAUTION: This modifies source JSON files. Use --dry-run first.
"""
import json
import sqlite3
import os
from dotenv import load_dotenv
from rich.console import Console
from rich.progress import Progress

load_dotenv()

DB_PATH = os.getenv("DB_PATH", "facts.db")

console = Console()


def run_repair(dry_run=True, min_confidence=0.8, limit=None):
    """Repair flagged inconsistencies in source files."""
    console.print("[bold]Phase 7: Repair[/bold]")
    console.print(f"  Dry run: {dry_run}")
    console.print(f"  Min confidence: {min_confidence}")

    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()

    c.execute("""
        SELECT f.id, f.triple_id, f.source_file, f.entity_name, f.subject, f.predicate,
               f.incorrect_object, f.correct_object, f.confidence
        FROM flagged_triples f
        WHERE f.repaired = 0 AND f.confidence >= ?
        ORDER BY f.confidence DESC
    """, (min_confidence,))

    flags = c.fetchall()
    if limit:
        flags = flags[:limit]

    console.print(f"  Repairs to apply: {len(flags)}")

    if not flags:
        console.print("[green]Nothing to repair at this confidence level.[/green]")
        return

    repaired = 0
    errors = 0

    with Progress() as progress:
        task = progress.add_task("Repairing...", total=len(flags))

        for flag_id, triple_id, source_file, entity_name, subject, predicate, wrong, right, confidence in flags:
            try:
                if not os.path.exists(source_file):
                    errors += 1
                    progress.update(task, advance=1)
                    continue

                with open(source_file, "r", encoding="utf-8") as f:
                    data = json.load(f)

                # Find and replace the incorrect value in the JSON
                modified = False
                json_str = json.dumps(data)
                if wrong in json_str:
                    json_str = json_str.replace(wrong, right, 1)
                    data = json.loads(json_str)
                    modified = True

                if modified:
                    if dry_run:
                        console.print(f"  [yellow]WOULD REPAIR[/yellow] {entity_name}: {predicate} = {wrong} -> {right}")
                    else:
                        with open(source_file, "w", encoding="utf-8") as f:
                            json.dump(data, f, indent=2, ensure_ascii=False)
                            f.write("\n")
                        c.execute("UPDATE flagged_triples SET repaired = 1 WHERE id = ?", (flag_id,))
                        repaired += 1

            except Exception as e:
                console.print(f"  [red]Error repairing {source_file}: {e}[/red]")
                errors += 1

            progress.update(task, advance=1)

    conn.commit()
    conn.close()

    console.print(f"\n[bold green]Repair {'preview' if dry_run else 'complete'}![/bold green]")
    console.print(f"  {'Would repair' if dry_run else 'Repaired'}: {repaired if not dry_run else len(flags)}")
    console.print(f"  Errors: {errors}")


if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(description="Repair flagged inconsistencies in source files")
    parser.add_argument("--dry-run", action="store_true", help="Preview repairs without modifying files")
    parser.add_argument("--min-confidence", type=float, default=0.8, help="Only repair claims above this confidence")
    parser.add_argument("--limit", type=int, help="Limit number of repairs")
    args = parser.parse_args()

    run_repair(dry_run=args.dry_run, min_confidence=args.min_confidence, limit=args.limit)
