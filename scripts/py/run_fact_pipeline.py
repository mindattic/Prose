"""
Fact Discovery Pipeline -- Master Runner

This is the orchestrator that runs all phases in order. Instead of running
each script individually, you can run this one file and it handles everything.

THE FULL PIPELINE (what happens when you run this):
  Phase 1: extract.py  -- Send each JSON entity to Claude API, get back SPO triples (SLOW, hours)
  Phase 2: embed.py    -- Convert each triple's sentence into a 384-number vector (fast, minutes)
  Phase 3: cluster.py  -- Group similar triples using HDBSCAN (fast, seconds)
  Phase 4: score.py    -- Vote on truth within each cluster, flag disagreements (fast, seconds)
  Phase 5: repair.py   -- Auto-fix high-confidence errors in source JSON files (fast, seconds)

ONE COMMAND -- does everything, auto-repairs 90%+ confidence fixes:
  python run_pipeline.py

QUERY RESULTS when done:
  python query.py --stats                        # numbers dashboard
  python query.py --flagged                      # see all inconsistencies
  python query.py "Arcturus Defense Solutions"    # search a subject

OR RUN PHASES INDIVIDUALLY:
  python extract.py                # Phase 1: Claude API extracts triples (SLOW -- hours)
  python embed.py                  # Phase 2: sentence-transformers embeds as vectors
  python cluster.py                # Phase 3: HDBSCAN clusters equivalent claims
  python score.py                  # Phase 4: consensus scoring + flagging
  python repair.py --min-confidence 0.9   # Phase 5: auto-fix high-confidence issues

SKIP PHASES YOU'VE ALREADY RUN:
  python run_pipeline.py --skip-extract    # already extracted, re-run the rest
  python run_pipeline.py --phase score     # just re-score
  python run_pipeline.py --limit 50        # test with 50 files first

RESUME ANYTIME -- Ctrl+C and restart:
  python run_pipeline.py    # picks up where it left off
"""

import argparse
from rich.console import Console

# Rich console for styled terminal output
console = Console()


def main():
    # Set up command-line arguments for controlling which phases run and with what settings.
    # argparse automatically generates --help output from these definitions.
    parser = argparse.ArgumentParser(description="Run the truth discovery pipeline")

    # Control WHAT gets processed
    parser.add_argument("--limit", type=int, help="Limit files for extraction")
    parser.add_argument("--repo", type=str, help="Only process one repo")

    # Control WHICH PHASES run (skip phases you've already completed)
    # "action='store_true'" means these are boolean flags -- present = True, absent = False.
    parser.add_argument("--skip-extract", action="store_true", help="Skip extraction (use existing triples)")
    parser.add_argument("--skip-embed", action="store_true", help="Skip embedding")
    parser.add_argument("--skip-cluster", action="store_true", help="Skip clustering")

    # Run ONLY a single phase (useful for debugging or re-running one step)
    # "choices" restricts the allowed values to this list
    parser.add_argument("--phase", type=str, choices=["extract", "embed", "cluster", "score", "query"], help="Run a single phase")

    # Tuning parameters for the ML phases
    parser.add_argument("--min-cluster-size", type=int, default=3)     # HDBSCAN: minimum cluster size
    parser.add_argument("--min-confidence", type=float, default=0.6)   # Scoring: confidence threshold for "contested"
    parser.add_argument("--dry-run", action="store_true")              # Extract: preview without API calls
    parser.add_argument("--concurrency", type=int)                     # Extract: parallel API calls (default 20)

    # Parse the command-line arguments into an object
    parser.add_argument("--silent", action="store_true", help="Suppress all console output")
    args = parser.parse_args()
    if args.silent:
        import sys as _sys, os as _os
        _sys.stdout = open(_os.devnull, "w")
        _sys.stderr = open(_os.devnull, "w")


    # Print a banner so the user knows the pipeline is starting.
    # Rich markup: [bold red] makes the text bold and red in the terminal.
    console.print("[bold red]Street Samurai -- Fact Discovery Pipeline[/bold red]")
    console.print("  10,000+ entity files -> SPO extraction -> embedding -> clustering -> fact scoring")
    console.print("  Resume-safe: restart at any time, progress is checkpointed")
    console.print()

    # If the user asked for a single phase, run just that one and exit
    if args.phase:
        run_single_phase(args)
        return

    # ---- FULL PIPELINE: Run all phases in sequence ----

    # Phase 1: Extract SPO triples from JSON files using Claude API.
    # This is the slowest phase (hours) because it makes one API call per file.
    # Skip it if the user already has triples in the database.
    if not args.skip_extract:
        # console.rule() prints a horizontal line with centered text -- a visual separator
        console.rule("[bold]Phase 1: Extraction[/bold]")

        # Lazy import: only load extract.py when we actually need it.
        # This keeps startup fast when skipping phases.
        from fact_extract import run_extraction
        run_extraction(limit=args.limit, repo=args.repo, dry_run=args.dry_run, concurrency=args.concurrency)
    else:
        console.print("[yellow]Skipping extraction[/yellow]")

    # Phase 2: Generate vector embeddings for all triples.
    # Converts sentences into 384-number vectors for similarity comparison.
    if not args.skip_embed:
        console.rule("[bold]Phase 2: Embedding[/bold]")
        from fact_embed import run_embedding
        run_embedding()
    else:
        console.print("[yellow]Skipping embedding[/yellow]")

    # Phase 3: Cluster similar triples using HDBSCAN.
    # Groups triples that say the same thing in different words.
    if not args.skip_cluster:
        console.rule("[bold]Phase 3: Clustering[/bold]")
        from fact_cluster import run_clustering
        run_clustering(min_cluster_size=args.min_cluster_size)
    else:
        console.print("[yellow]Skipping clustering[/yellow]")

    # Phase 4+5: Score truth by consensus and flag disagreements.
    # Always runs (even if you skip earlier phases) because it's fast and
    # might need to recalculate if clustering parameters changed.
    console.rule("[bold]Phase 4: Fact Scoring[/bold]")
    from fact_score import run_scoring
    run_scoring(min_confidence=args.min_confidence)

    # Phase 5: Auto-repair inconsistencies where 90%+ of sources agree.
    # This is aggressive (it modifies source files!) but the high threshold
    # means we only fix things we're very confident about.
    # dry_run=False means it WILL modify files (not just preview).
    console.rule("[bold]Phase 5: Auto-Repair (90%+ confidence)[/bold]")
    from fact_repair import run_repair
    run_repair(dry_run=False, min_confidence=0.9)

    # Print a summary dashboard showing the final state of the database
    console.rule("[bold]Pipeline Complete[/bold]")
    from fact_query import show_stats
    show_stats()


def run_single_phase(args):
    """Run just one phase of the pipeline, based on the --phase argument."""

    # Each branch imports and runs the appropriate module.
    # This function exists to keep main() clean and readable.
    if args.phase == "extract":
        from fact_extract import run_extraction
        run_extraction(limit=args.limit, repo=args.repo, dry_run=args.dry_run, concurrency=args.concurrency)
    elif args.phase == "embed":
        from fact_embed import run_embedding
        run_embedding()
    elif args.phase == "cluster":
        from fact_cluster import run_clustering
        run_clustering(min_cluster_size=args.min_cluster_size)
    elif args.phase == "score":
        from fact_score import run_scoring
        run_scoring(min_confidence=args.min_confidence)
    elif args.phase == "query":
        # "query" phase just shows statistics -- a quick sanity check
        from fact_query import show_stats
        show_stats()


# This block runs only when you execute "python run_pipeline.py" directly.
# It calls main() which parses arguments and orchestrates the pipeline.
if __name__ == "__main__":
    main()
