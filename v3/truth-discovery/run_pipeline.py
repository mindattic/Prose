"""
Master pipeline runner. Executes all phases in sequence.

Usage:
  python run_pipeline.py                    # full pipeline
  python run_pipeline.py --limit 50         # test with 50 files
  python run_pipeline.py --skip-extract     # re-run from embedding onward
  python run_pipeline.py --phase score      # run a single phase
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

    console.print("[bold red]Street Samurai — Truth Discovery Pipeline[/bold red]")
    console.print(f"  9,600+ entity files → SPO extraction → embedding → clustering → truth scoring")
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
    console.rule("[bold]Phase 4: Truth Scoring[/bold]")
    from score import run_scoring
    run_scoring(min_confidence=args.min_confidence)

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
