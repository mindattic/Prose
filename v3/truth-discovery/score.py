"""
Phase 4 & 5: Score ground truth by consensus and flag disagreements.

For each cluster of claims, the most common object value wins as ground truth.
Confidence = agreeing_sources / total_sources. Any triple that disagrees with
consensus is flagged for review or repair.

Usage: python score.py [--min-confidence 0.6]
"""
import sqlite3
import os
from collections import Counter
from dotenv import load_dotenv
from rich.console import Console
from rich.table import Table

load_dotenv()

DB_PATH = os.getenv("DB_PATH", "facts.db")

console = Console()


def run_scoring(min_confidence=0.6):
    """Score truth by consensus within each cluster."""
    console.print("[bold]Phase 4: Fact Scoring[/bold]")

    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()

    # Clear previous scores
    c.execute("DELETE FROM fact_scores")
    c.execute("DELETE FROM flagged_triples")

    # Get all clusters with 2+ triples
    c.execute("""
        SELECT cluster_id, COUNT(*) as cnt
        FROM triples
        WHERE cluster_id >= 0
        GROUP BY cluster_id
        HAVING cnt >= 2
    """)
    clusters = c.fetchall()

    console.print(f"  Clusters to score: {len(clusters)}")

    total_scored = 0
    total_flagged = 0

    for cluster_id, count in clusters:
        # Get all triples in this cluster
        c.execute(
            "SELECT id, source_file, entity_name, subject, predicate, object FROM triples WHERE cluster_id = ?",
            (cluster_id,),
        )
        triples = c.fetchall()

        # Find consensus: most common (subject, predicate, object) combination
        # Group by subject+predicate, then vote on object
        subject_pred_groups = {}
        for triple_id, source_file, entity_name, subject, predicate, obj in triples:
            key = (subject.lower(), predicate.lower())
            if key not in subject_pred_groups:
                subject_pred_groups[key] = []
            subject_pred_groups[key].append((triple_id, source_file, entity_name, subject, predicate, obj))

        for (subj_key, pred_key), group in subject_pred_groups.items():
            # Vote on object value
            object_votes = Counter(t[5] for t in group)
            consensus_obj, agree_count = object_votes.most_common(1)[0]
            total_sources = len(group)
            dissent_count = total_sources - agree_count
            confidence = agree_count / total_sources

            # Use first triple's original casing for subject/predicate
            repr_subject = group[0][3]
            repr_predicate = group[0][4]

            # Store truth score
            c.execute(
                """INSERT INTO fact_scores
                   (cluster_id, subject, predicate, consensus_object, confidence, agreeing_sources, dissenting_sources, total_sources)
                   VALUES (?, ?, ?, ?, ?, ?, ?, ?)""",
                (cluster_id, repr_subject, repr_predicate, consensus_obj, confidence, agree_count, dissent_count, total_sources),
            )
            total_scored += 1

            # Flag disagreements
            if dissent_count > 0:
                for triple_id, source_file, entity_name, subject, predicate, obj in group:
                    if obj != consensus_obj:
                        c.execute(
                            """INSERT INTO flagged_triples
                               (triple_id, source_file, entity_name, subject, predicate, incorrect_object, correct_object, confidence)
                               VALUES (?, ?, ?, ?, ?, ?, ?, ?)""",
                            (triple_id, source_file, entity_name, subject, predicate, obj, consensus_obj, confidence),
                        )
                        total_flagged += 1

    # Log
    c.execute(
        "INSERT INTO processing_log (phase, status, message) VALUES (?, ?, ?)",
        ("scoring", "complete", f"Scored {total_scored} claims, flagged {total_flagged} disagreements"),
    )

    conn.commit()
    conn.close()

    console.print(f"\n[bold green]Scoring complete![/bold green]")
    console.print(f"  Claims scored: {total_scored}")
    console.print(f"  Disagreements flagged: {total_flagged}")

    # Show top contested claims
    show_contested(min_confidence)


def show_contested(min_confidence=0.6):
    """Display the most contested claims."""
    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()

    c.execute("""
        SELECT subject, predicate, consensus_object, confidence, agreeing_sources, dissenting_sources
        FROM fact_scores
        WHERE confidence < ?
        ORDER BY confidence ASC
        LIMIT 20
    """, (min_confidence,))

    rows = c.fetchall()
    conn.close()

    if not rows:
        console.print("[green]No contested claims below confidence threshold.[/green]")
        return

    table = Table(title=f"Contested Claims (confidence < {min_confidence})")
    table.add_column("Subject", style="red")
    table.add_column("Predicate", style="cyan")
    table.add_column("Consensus", style="green")
    table.add_column("Confidence", justify="right")
    table.add_column("Agree/Dissent", justify="right")

    for subject, predicate, obj, confidence, agree, dissent in rows:
        table.add_row(subject[:30], predicate[:20], obj[:30], f"{confidence:.2f}", f"{agree}/{dissent}")

    console.print(table)


if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(description="Score truth by consensus")
    parser.add_argument("--min-confidence", type=float, default=0.6, help="Confidence threshold for contested claims")
    args = parser.parse_args()

    run_scoring(min_confidence=args.min_confidence)
