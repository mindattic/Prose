"""
Phase 6: CLI query tool. Ask about any subject and get back ground truth
claims with confidence scores and which sources disagree.

This is the "read" side of the pipeline -- everything else writes data,
this script lets you explore what was found. It has three modes:
  1. Search by subject name ("what do we know about X?")
  2. Show all flagged inconsistencies ("what needs fixing?")
  3. Show pipeline statistics ("how much data do we have?")

Usage: python query.py "Arcturus Defense Solutions"
       python query.py --flagged          # show all flagged inconsistencies
       python query.py --stats            # show pipeline statistics
"""

import sqlite3
import os
import sys
from dotenv import load_dotenv
from rich.console import Console

# Rich Table creates nicely formatted ASCII tables in the terminal
from rich.table import Table

# Load environment variables from .env
load_dotenv()

# Path to the SQLite database (the one all previous phases wrote to)
DB_PATH = os.getenv("DB_PATH", "facts.db")

# Rich console for styled terminal output
console = Console()


def query_subject(subject):
    """Query all ground truth claims about a subject."""

    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()

    # Search for claims where the subject contains our search term (case-insensitive).
    # LIKE with % wildcards on both sides means "contains" -- so searching for "arcturus"
    # would match "Arcturus Defense Solutions" and "New Arcturus Colony."
    # LOWER() converts both sides to lowercase for case-insensitive comparison.
    # ORDER BY confidence DESC shows the most confident claims first.
    c.execute("""
        SELECT subject, predicate, consensus_object, confidence, agreeing_sources, dissenting_sources, total_sources
        FROM fact_scores
        WHERE LOWER(subject) LIKE ?
        ORDER BY confidence DESC
    """, (f"%{subject.lower()}%",))
    # The f-string wraps the search term in % wildcards: "arcturus" becomes "%arcturus%"

    rows = c.fetchall()

    # If no claims were found, tell the user and exit
    if not rows:
        console.print(f"[yellow]No claims found for '{subject}'[/yellow]")
        conn.close()
        return

    # Build a Rich table showing all consensus claims about this subject
    table = Table(title=f"Consensus: {subject}")
    table.add_column("Subject", style="bold")
    table.add_column("Predicate", style="cyan")
    table.add_column("Value", style="green")
    table.add_column("Confidence", justify="right")
    table.add_column("Sources", justify="right")

    for subj, pred, obj, conf, agree, dissent, total in rows:
        # Color-code confidence: green = high (80%+), yellow = medium (60-80%), red = low (<60%)
        # This makes it easy to spot claims that need attention at a glance.
        conf_style = "green" if conf >= 0.8 else "yellow" if conf >= 0.6 else "red"

        # [:35] etc. truncates long strings so the table stays readable.
        # f"[{conf_style}]...": Rich markup tags for inline color styling.
        # {conf:.0%} formats 0.8 as "80%" (:.0% = percentage with 0 decimal places).
        table.add_row(subj[:35], pred[:25], obj[:40], f"[{conf_style}]{conf:.0%}[/{conf_style}]", f"{agree}/{total}")

    # Print the formatted table to the terminal
    console.print(table)

    # Also show any flagged inconsistencies for this subject.
    # These are triples that DISAGREE with the consensus.
    c.execute("""
        SELECT f.source_file, f.predicate, f.incorrect_object, f.correct_object, f.confidence
        FROM flagged_triples f
        WHERE LOWER(f.subject) LIKE ?
        ORDER BY f.confidence ASC
    """, (f"%{subject.lower()}%",))

    flags = c.fetchall()

    # If there are disagreements, show them below the main table
    if flags:
        console.print(f"\n[red]Flagged inconsistencies ({len(flags)}):[/red]")

        # Show at most 10 flags to keep the output manageable
        for source, pred, wrong, right, conf in flags[:10]:
            # os.path.basename() strips the directory path, showing just the filename
            # e.g., "/data/weapons/gun.json" -> "gun.json"
            source_name = os.path.basename(source)

            # Display: which file has what wrong value, and what it should be
            console.print(f"  {source_name}: {pred} = [red]{wrong}[/red] -> should be [green]{right}[/green] ({conf:.0%})")

    conn.close()


def show_flagged(limit=50):
    """Show all flagged inconsistencies."""

    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()

    # Get all UNREPAIRED flagged triples (repaired = 0 means not yet fixed).
    # ORDER BY confidence DESC shows the most confidently-wrong items first
    # (these are the easiest to fix because we're most sure about the correct value).
    c.execute("""
        SELECT entity_name, subject, predicate, incorrect_object, correct_object, confidence, source_file
        FROM flagged_triples
        WHERE repaired = 0
        ORDER BY confidence DESC
        LIMIT ?
    """, (limit,))

    rows = c.fetchall()
    conn.close()

    # If nothing is flagged, everything is consistent
    if not rows:
        console.print("[green]No flagged inconsistencies.[/green]")
        return

    # Build a table showing all flagged issues
    table = Table(title=f"Flagged Inconsistencies (top {limit})")
    table.add_column("Entity", style="bold")
    table.add_column("Claim", style="cyan")
    table.add_column("Has", style="red")         # What the file currently says (wrong)
    table.add_column("Should Be", style="green")  # What the consensus says (right)
    table.add_column("Confidence", justify="right")

    for entity, subj, pred, wrong, right, conf, source in rows:
        # f"{pred}" is redundant here (pred is already a string) but makes the code explicit.
        # [:25] truncates to fit the table columns.
        table.add_row(entity[:25], f"{pred}"[:20], wrong[:25], right[:25], f"{conf:.0%}")

    console.print(table)


def show_stats():
    """Show pipeline statistics."""

    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()

    # Run a series of COUNT queries to build a dashboard of pipeline metrics.
    # Each query is a simple aggregate that counts rows in a table.
    stats = {}

    # Total number of SPO triples extracted across all files
    c.execute("SELECT COUNT(*) FROM triples")
    stats["Total triples"] = c.fetchone()[0]  # fetchone() returns a single row; [0] is the first column

    # How many unique source files contributed triples
    # COUNT(DISTINCT ...) counts unique values, ignoring duplicates
    c.execute("SELECT COUNT(DISTINCT source_file) FROM triples")
    stats["Source files"] = c.fetchone()[0]

    # How many clusters HDBSCAN found
    c.execute("SELECT COUNT(*) FROM clusters")
    stats["Clusters"] = c.fetchone()[0]

    # How many consensus claims were scored
    c.execute("SELECT COUNT(*) FROM fact_scores")
    stats["Consensus claims"] = c.fetchone()[0]

    # How many triples disagree with consensus and haven't been fixed yet
    c.execute("SELECT COUNT(*) FROM flagged_triples WHERE repaired = 0")
    stats["Flagged inconsistencies"] = c.fetchone()[0]

    # How many have already been repaired by repair.py
    c.execute("SELECT COUNT(*) FROM flagged_triples WHERE repaired = 1")
    stats["Repaired"] = c.fetchone()[0]

    # Average confidence across all consensus claims.
    # AVG() is a SQL aggregate function that computes the mean.
    c.execute("SELECT AVG(confidence) FROM fact_scores")
    avg = c.fetchone()[0]
    # Format as percentage if we have data, otherwise show "N/A"
    stats["Average confidence"] = f"{avg:.2%}" if avg else "N/A"

    conn.close()

    # Build and display the stats table
    table = Table(title="Pipeline Statistics")
    table.add_column("Metric", style="bold")
    table.add_column("Value", justify="right", style="cyan")

    # .items() returns (key, value) pairs from the dictionary
    for k, v in stats.items():
        table.add_row(k, str(v))  # str(v) converts numbers to strings for display

    console.print(table)


def main():
    import argparse

    # Set up command-line argument parsing with three modes of operation
    parser = argparse.ArgumentParser(description="Query the truth discovery database")

    # "nargs='?'" makes this argument optional (you can omit it entirely).
    # Without nargs='?', argparse would require it.
    parser.add_argument("subject", nargs="?", help="Subject to query (e.g. 'Arcturus Defense Solutions')")

    # "action='store_true'" means: if the flag is present, set it to True; otherwise False.
    # These are boolean switches that don't take a value.
    parser.add_argument("--flagged", action="store_true", help="Show all flagged inconsistencies")
    parser.add_argument("--stats", action="store_true", help="Show pipeline statistics")
    parser.add_argument("--limit", type=int, default=50, help="Limit results")

    args = parser.parse_args()

    # Route to the appropriate function based on which flag was used.
    # The order matters: --stats and --flagged take priority over a subject search.
    if args.stats:
        show_stats()
    elif args.flagged:
        show_flagged(args.limit)
    elif args.subject:
        query_subject(args.subject)
    else:
        # No arguments provided -- show the help message
        parser.print_help()


# Only run when executed directly (python query.py), not when imported
if __name__ == "__main__":
    main()
