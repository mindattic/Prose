"""
Phase 4 & 5: Score ground truth by consensus and flag disagreements.

For each cluster of claims, the most common object value wins as ground truth.
Confidence = agreeing_sources / total_sources. Any triple that disagrees with
consensus is flagged for review or repair.

HOW CONSENSUS SCORING WORKS:
Imagine 5 different files all talk about the same weapon's manufacturer:
  - 4 files say "manufactured_by = Hearthstone Firearms"
  - 1 file says "manufactured_by = Arcturus Defense"
The consensus is "Hearthstone Firearms" with 80% confidence (4/5 agree).
The 1 dissenting file gets flagged as potentially incorrect.

This is the same principle as "wisdom of crowds" -- if most independent sources
agree on something, it's probably true.

Usage: python score.py [--min-confidence 0.6]
"""

import sqlite3
import os

# Counter is a specialized dictionary that counts how many times each value appears.
# For example: Counter(["cat", "dog", "cat"]) -> {"cat": 2, "dog": 1}
# It's perfect for voting/tallying.
from collections import Counter

from rich.console import Console
from rich.table import Table
from constants import DB_PATH

console = Console()


def run_scoring(min_confidence=0.6):
    """Score truth by consensus within each cluster."""

    console.print("[bold]Phase 4: Fact Scoring[/bold]")

    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()

    # Wipe previous scoring results so we start fresh.
    # This is safe because scores can always be recalculated from the triples and clusters.
    c.execute("DELETE FROM fact_scores")
    c.execute("DELETE FROM flagged_triples")

    # Find all clusters that have at least 2 triples.
    # A cluster with only 1 triple has no "consensus" -- there's nothing to vote on.
    # HAVING is like WHERE but for grouped results (it filters AFTER the GROUP BY).
    # cnt >= 2 means we only care about clusters where at least 2 sources make the same claim.
    c.execute("""
        SELECT cluster_id, COUNT(*) as cnt
        FROM triples
        WHERE cluster_id >= 0
        GROUP BY cluster_id
        HAVING cnt >= 2
    """)
    clusters = c.fetchall()  # List of (cluster_id, count) tuples

    console.print(f"  Clusters to score: {len(clusters)}")

    total_scored = 0   # How many consensus claims we've recorded
    total_flagged = 0  # How many dissenting triples we've flagged

    # Process each cluster one at a time
    for cluster_id, count in clusters:

        # Fetch all triples in this cluster from the database
        c.execute(
            "SELECT id, source_file, entity_name, subject, predicate, object FROM triples WHERE cluster_id = ?",
            (cluster_id,),
        )
        triples = c.fetchall()

        # Within a single cluster, there might be multiple DIFFERENT subject+predicate combinations.
        # For example, a cluster about "Hearthstone HM-7" might contain triples about both
        # "manufactured_by" and "available_at". We need to vote on each predicate separately.
        #
        # This dictionary groups triples by their (subject, predicate) pair.
        # The key is a tuple of (lowercase subject, lowercase predicate) for case-insensitive matching.
        # The value is a list of all triples with that subject+predicate.
        subject_pred_groups = {}
        for triple_id, source_file, entity_name, subject, predicate, obj in triples:
            # .lower() normalizes case so "Hearthstone" and "hearthstone" are treated the same
            key = (subject.lower(), predicate.lower())
            if key not in subject_pred_groups:
                subject_pred_groups[key] = []
            subject_pred_groups[key].append((triple_id, source_file, entity_name, subject, predicate, obj))

        # Now vote on the object value for each subject+predicate group
        for (subj_key, pred_key), group in subject_pred_groups.items():

            # Use Counter to tally votes on the object value.
            # t[5] is the "object" field from each triple tuple.
            # Example: Counter({"Hearthstone Firearms": 4, "Arcturus Defense": 1})
            object_votes = Counter(t[5] for t in group)

            # .most_common(1) returns the single most frequent value and its count.
            # Returns a list of tuples: [("Hearthstone Firearms", 4)]
            # We unpack the first (and only) tuple into consensus_obj and agree_count.
            consensus_obj, agree_count = object_votes.most_common(1)[0]

            # Count total votes and how many disagree with the consensus
            total_sources = len(group)
            dissent_count = total_sources - agree_count

            # Confidence is the fraction of sources that agree.
            # 4 out of 5 = 0.8 = 80% confidence.
            confidence = agree_count / total_sources

            # Use the ORIGINAL casing from the first triple in the group (not the lowercased version).
            # group[0][3] is the subject, group[0][4] is the predicate.
            repr_subject = group[0][3]
            repr_predicate = group[0][4]

            # Insert the consensus result into the fact_scores table.
            # This is the "ground truth" that the pipeline has determined.
            c.execute(
                """INSERT INTO fact_scores
                   (cluster_id, subject, predicate, consensus_object, confidence, agreeing_sources, dissenting_sources, total_sources)
                   VALUES (?, ?, ?, ?, ?, ?, ?, ?)""",
                (cluster_id, repr_subject, repr_predicate, consensus_obj, confidence, agree_count, dissent_count, total_sources),
            )
            total_scored += 1

            # FLAG DISAGREEMENTS: If any sources disagree, record them as potential errors.
            # These are candidates for repair in Phase 7 (repair.py).
            if dissent_count > 0:
                for triple_id, source_file, entity_name, subject, predicate, obj in group:
                    # Only flag triples that DISAGREE with the consensus
                    if obj != consensus_obj:
                        c.execute(
                            """INSERT INTO flagged_triples
                               (triple_id, source_file, entity_name, subject, predicate, incorrect_object, correct_object, confidence)
                               VALUES (?, ?, ?, ?, ?, ?, ?, ?)""",
                            (triple_id, source_file, entity_name, subject, predicate, obj, consensus_obj, confidence),
                        )
                        total_flagged += 1

    # Write a log entry recording that scoring is complete
    c.execute(
        "INSERT INTO processing_log (phase, status, message) VALUES (?, ?, ?)",
        ("scoring", "complete", f"Scored {total_scored} claims, flagged {total_flagged} disagreements"),
    )

    # Save all changes to disk and close the connection
    conn.commit()
    conn.close()

    # Print the final summary
    console.print(f"\n[bold green]Scoring complete![/bold green]")
    console.print(f"  Claims scored: {total_scored}")
    console.print(f"  Disagreements flagged: {total_flagged}")

    # Show the most contested claims (ones where sources disagree the most)
    show_contested(min_confidence)


def show_contested(min_confidence=0.6):
    """Display the most contested claims."""

    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()

    # Find claims where confidence is BELOW the threshold.
    # Low confidence = lots of disagreement = these are the most interesting/problematic claims.
    # ORDER BY confidence ASC puts the MOST contested (lowest confidence) first.
    # LIMIT 20 prevents a huge output if there are thousands of contested claims.
    c.execute("""
        SELECT subject, predicate, consensus_object, confidence, agreeing_sources, dissenting_sources
        FROM fact_scores
        WHERE confidence < ?
        ORDER BY confidence ASC
        LIMIT 20
    """, (min_confidence,))

    rows = c.fetchall()
    conn.close()

    # If all claims have high confidence, there's nothing to show
    if not rows:
        console.print("[green]No contested claims below confidence threshold.[/green]")
        return

    # Build a Rich table for nice terminal display
    table = Table(title=f"Contested Claims (confidence < {min_confidence})")
    table.add_column("Subject", style="red")       # Red = attention needed
    table.add_column("Predicate", style="cyan")     # Cyan = informational
    table.add_column("Consensus", style="green")    # Green = the "answer"
    table.add_column("Confidence", justify="right") # Right-aligned numbers
    table.add_column("Agree/Dissent", justify="right")

    # Add each contested claim as a row in the table.
    # [:30] and [:20] truncate long strings so the table doesn't overflow the terminal.
    for subject, predicate, obj, confidence, agree, dissent in rows:
        table.add_row(subject[:30], predicate[:20], obj[:30], f"{confidence:.2f}", f"{agree}/{dissent}")

    # Print the formatted table to the terminal
    console.print(table)


# Only run when executed directly (python score.py), not when imported
if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(description="Score truth by consensus")

    # --min-confidence: Claims below this threshold are shown as "contested."
    # 0.6 means any claim where fewer than 60% of sources agree is flagged for display.
    parser.add_argument("--min-confidence", type=float, default=0.6, help="Confidence threshold for contested claims")

    args = parser.parse_args()

    run_scoring(min_confidence=args.min_confidence)
