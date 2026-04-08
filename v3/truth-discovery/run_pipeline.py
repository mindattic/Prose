"""
Fact Discovery Pipeline — Master Runner

ONE COMMAND — does everything, auto-repairs 90%+ confidence fixes:
  python run_pipeline.py

QUERY RESULTS when done:
  python query.py --stats                        # numbers dashboard
  python query.py --flagged                      # see all inconsistencies
  python query.py "Arcturus Defense Solutions"    # search a subject

OR RUN PHASES INDIVIDUALLY:
  python extract.py                # Phase 1: Claude API extracts triples (SLOW — hours)
  python embed.py                  # Phase 2: sentence-transformers embeds as vectors
  python cluster.py                # Phase 3: HDBSCAN clusters equivalent claims
  python score.py                  # Phase 4: consensus scoring + flagging
  python repair.py --min-confidence 0.9   # Phase 5: auto-fix high-confidence issues

SKIP PHASES YOU'VE ALREADY RUN:
  python run_pipeline.py --skip-extract    # already extracted, re-run the rest
  python run_pipeline.py --phase score     # just re-score
  python run_pipeline.py --limit 50        # test with 50 files first

RESUME ANYTIME — Ctrl+C and restart:
  python run_pipeline.py    # picks up where it left off
"""
import argparse
from rich.console import Console

console = Console()


def main():
    parser = argparse.ArgumentParser(description="Run the truth discovery pipeline")
    parser.add_argument("--limit", type=int, help="Limit files for extraction")
    parser.add_argument("--repo", type=str, help="Only process one repo")
    parser.add_argument("--skip-extract", action="store_true", help="Skip extraction (use existing triples)")
    parser.add_argument("--skip-embed", action="store_true", help="Skip embedding")
    parser.add_argument("--skip-cluster", action="store_true", help="Skip clustering")
    parser.add_argument("--phase", type=str, choices=["extract", "embed", "cluster", "score", "query"], help="Run a single phase")
    parser.add_argument("--min-cluster-size", type=int, default=3)
    parser.add_argument("--min-confidence", type=float, default=0.6)
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()

    console.print("[bold red]Street Samurai -- Fact Discovery Pipeline[/bold red]")
    console.print("  10,000+ entity files -> SPO extraction -> embedding -> clustering -> fact scoring")
    console.print("  Resume-safe: restart at any time, progress is checkpointed")
    console.print()

    if args.phase:
        run_single_phase(args)
        return

    # Phase 1: Extract
    if not args.skip_extract:
        console.rule("[bold]Phase 1: Extraction[/bold]")
        from extract import run_extraction
        run_extraction(limit=args.limit, repo=args.repo, dry_run=args.dry_run)
    else:
        console.print("[yellow]Skipping extraction[/yellow]")

    # Phase 2: Embed
    if not args.skip_embed:
        console.rule("[bold]Phase 2: Embedding[/bold]")
        from embed import run_embedding
        run_embedding()
    else:
        console.print("[yellow]Skipping embedding[/yellow]")

    # Phase 3: Cluster
    if not args.skip_cluster:
        console.rule("[bold]Phase 3: Clustering[/bold]")
        from cluster import run_clustering
        run_clustering(min_cluster_size=args.min_cluster_size)
    else:
        console.print("[yellow]Skipping clustering[/yellow]")

    # Phase 4+5: Score + Flag
    console.rule("[bold]Phase 4: Fact Scoring[/bold]")
    from score import run_scoring
    run_scoring(min_confidence=args.min_confidence)

    # Phase 5: Auto-repair high-confidence fixes
    console.rule("[bold]Phase 5: Auto-Repair (90%+ confidence)[/bold]")
    from repair import run_repair
    run_repair(dry_run=False, min_confidence=0.9)

    # Summary
    console.rule("[bold]Pipeline Complete[/bold]")
    from query import show_stats
    show_stats()


def run_single_phase(args):
    if args.phase == "extract":
        from extract import run_extraction
        run_extraction(limit=args.limit, repo=args.repo, dry_run=args.dry_run)
    elif args.phase == "embed":
        from embed import run_embedding
        run_embedding()
    elif args.phase == "cluster":
        from cluster import run_clustering
        run_clustering(min_cluster_size=args.min_cluster_size)
    elif args.phase == "score":
        from score import run_scoring
        run_scoring(min_confidence=args.min_confidence)
    elif args.phase == "query":
        from query import show_stats
        show_stats()


if __name__ == "__main__":
    main()
