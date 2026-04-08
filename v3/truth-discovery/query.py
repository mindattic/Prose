"""
Phase 6: CLI query tool. Ask about any subject and get back ground truth
claims with confidence scores and which sources disagree.

Usage: python query.py "Arcturus Defense Solutions"
       python query.py --flagged          # show all flagged inconsistencies
       python query.py --stats            # show pipeline statistics
"""
import sqlite3
import os
import sys
from dotenv import load_dotenv
from rich.console import Console
from rich.table import Table

load_dotenv()

DB_PATH = os.getenv("DB_PATH", "truth.db")

console = Console()


def query_subject(subject):
    """Query all ground truth claims about a subject."""
    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()

    c.execute("""
        SELECT subject, predicate, ground_truth_object, confidence, agreeing_sources, dissenting_sources, total_sources
        FROM truth_scores
        WHERE LOWER(subject) LIKE ?
        ORDER BY confidence DESC
    """, (f"%{subject.lower()}%",))

    rows = c.fetchall()

    if not rows:
        console.print(f"[yellow]No claims found for '{subject}'[/yellow]")
        conn.close()
        return

    table = Table(title=f"Ground Truth: {subject}")
    table.add_column("Subject", style="bold")
    table.add_column("Predicate", style="cyan")
    table.add_column("Value", style="green")
    table.add_column("Confidence", justify="right")
    table.add_column("Sources", justify="right")

    for subj, pred, obj, conf, agree, dissent, total in rows:
        conf_style = "green" if conf >= 0.8 else "yellow" if conf >= 0.6 else "red"
        table.add_row(subj[:35], pred[:25], obj[:40], f"[{conf_style}]{conf:.0%}[/{conf_style}]", f"{agree}/{total}")

    console.print(table)

    # Show disagreements
    c.execute("""
        SELECT f.source_file, f.predicate, f.incorrect_object, f.correct_object, f.confidence
        FROM flagged_triples f
        WHERE LOWER(f.subject) LIKE ?
        ORDER BY f.confidence ASC
    """, (f"%{subject.lower()}%",))

    flags = c.fetchall()
    if flags:
        console.print(f"\n[red]Flagged inconsistencies ({len(flags)}):[/red]")
        for source, pred, wrong, right, conf in flags[:10]:
            source_name = os.path.basename(source)
            console.print(f"  {source_name}: {pred} = [red]{wrong}[/red] → should be [green]{right}[/green] ({conf:.0%})")

    conn.close()


def show_flagged(limit=50):
    """Show all flagged inconsistencies."""
    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()

    c.execute("""
        SELECT entity_name, subject, predicate, incorrect_object, correct_object, confidence, source_file
        FROM flagged_triples
        WHERE repaired = 0
        ORDER BY confidence DESC
        LIMIT ?
    """, (limit,))

    rows = c.fetchall()
    conn.close()

    if not rows:
        console.print("[green]No flagged inconsistencies.[/green]")
        return

    table = Table(title=f"Flagged Inconsistencies (top {limit})")
    table.add_column("Entity", style="bold")
    table.add_column("Claim", style="cyan")
    table.add_column("Has", style="red")
    table.add_column("Should Be", style="green")
    table.add_column("Confidence", justify="right")

    for entity, subj, pred, wrong, right, conf, source in rows:
        table.add_row(entity[:25], f"{pred}"[:20], wrong[:25], right[:25], f"{conf:.0%}")

    console.print(table)


def show_stats():
    """Show pipeline statistics."""
    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()

    stats = {}
    c.execute("SELECT COUNT(*) FROM triples")
    stats["Total triples"] = c.fetchone()[0]

    c.execute("SELECT COUNT(DISTINCT source_file) FROM triples")
    stats["Source files"] = c.fetchone()[0]

    c.execute("SELECT COUNT(*) FROM clusters")
    stats["Clusters"] = c.fetchone()[0]

    c.execute("SELECT COUNT(*) FROM truth_scores")
    stats["Ground truth claims"] = c.fetchone()[0]

    c.execute("SELECT COUNT(*) FROM flagged_triples WHERE repaired = 0")
    stats["Flagged inconsistencies"] = c.fetchone()[0]

    c.execute("SELECT COUNT(*) FROM flagged_triples WHERE repaired = 1")
    stats["Repaired"] = c.fetchone()[0]

    c.execute("SELECT AVG(confidence) FROM truth_scores")
    avg = c.fetchone()[0]
    stats["Average confidence"] = f"{avg:.2%}" if avg else "N/A"

    conn.close()

    table = Table(title="Pipeline Statistics")
    table.add_column("Metric", style="bold")
    table.add_column("Value", justify="right", style="cyan")

    for k, v in stats.items():
        table.add_row(k, str(v))

    console.print(table)


def main():
    import argparse

    parser = argparse.ArgumentParser(description="Query the truth discovery database")
    parser.add_argument("subject", nargs="?", help="Subject to query (e.g. 'Arcturus Defense Solutions')")
    parser.add_argument("--flagged", action="store_true", help="Show all flagged inconsistencies")
    parser.add_argument("--stats", action="store_true", help="Show pipeline statistics")
    parser.add_argument("--limit", type=int, default=50, help="Limit results")
    args = parser.parse_args()

    if args.stats:
        show_stats()
    elif args.flagged:
        show_flagged(args.limit)
    elif args.subject:
        query_subject(args.subject)
    else:
        parser.print_help()


if __name__ == "__main__":
    main()
