"""
StreetSamurai ML orchestration pipeline.

Phases:
    extract_gripes    → pull reviewer gripes from DB → Parquet cache
    extract_beats     → pull current beat texts → Parquet cache
    train_topics      → train BERTopic gripe miner
    train_register    → train protagonist register classifier
    audit_gripes      → apply topic model → write Findings
    audit_register    → apply register classifier → write Findings
    compute_metrics   → CPU-only per-beat prose quality metrics (BeatProseMetrics table)
    find_near_dupes   → cross-story near-duplicate detection via sentence-transformers
    score_correlation → beat score vs prose feature correlation model (GradientBoosting)

Usage:
    python nightly_run.py [--phases all|<phase1>,<phase2>,...] [--strand <slug>]

Examples:
    python nightly_run.py                                # full pipeline
    python nightly_run.py --phases train_register        # retrain register only
    python nightly_run.py --phases audit_register --strand sasha_v
    python nightly_run.py --phases compute_metrics,find_near_dupes,score_correlation
"""
import sys
import os
sys.path.insert(0, os.path.dirname(os.path.dirname(__file__)))

import argparse
import traceback
from pathlib import Path
from rich.console import Console

console = Console()

PHASES = [
    "extract_gripes",
    "extract_beats",
    "train_topics",
    "train_register",
    "audit_gripes",
    "audit_register",
    # Canon Audit Suite — CPU-only quality metrics and cross-story analysis.
    "compute_metrics",
    "find_near_dupes",
    "score_correlation",
]


def phase_extract_gripes():
    from extract.pull_gripes import run
    run()


def phase_extract_beats():
    from extract.pull_beats import run
    run()


def phase_train_topics():
    import pandas as pd
    from models.topic_model import GripeMiner
    from config import GRIPES_CACHE_PATH
    if not GRIPES_CACHE_PATH.exists():
        console.print("[yellow]No gripes cache — running extract_gripes first.[/yellow]")
        phase_extract_gripes()
    df    = pd.read_parquet(GRIPES_CACHE_PATH)
    texts = df["GripeText"].dropna().str.strip().tolist()
    texts = [t for t in texts if len(t) > 5]
    if len(texts) < 50:
        console.print(f"[yellow]Only {len(texts)} gripe texts — skipping (need >= 50).[/yellow]")
        return
    miner = GripeMiner()
    miner.train(texts)
    summary = miner.get_topic_summary()
    console.print(f"\n[bold]Top topics ({len(summary)} total):[/bold]")
    for t in summary[:10]:
        console.print(f"  {t['size']:4d}  {t['label']}  — {', '.join(t['keywords'][:5])}")


def phase_train_register():
    import pandas as pd
    from models.register_classifier import RegisterClassifier
    from config import BEATS_CACHE_PATH
    if not BEATS_CACHE_PATH.exists():
        console.print("[yellow]No beats cache — running extract_beats first.[/yellow]")
        phase_extract_beats()
    df  = pd.read_parquet(BEATS_CACHE_PATH)
    clf = RegisterClassifier()
    metrics = clf.train(df)
    if not metrics:
        return
    console.print(f"\n[bold]Top discriminating words per strand:[/bold]")
    for slug in clf.trained_slugs:
        top   = clf.top_discriminating_words(slug, top_n=5)
        words = ", ".join(w for w, _ in top)
        console.print(f"  {slug}: {words}")


def phase_audit_gripes(strand: str | None):
    from models.topic_model import GripeMiner
    from db import get_connection
    from audit.beat_auditor import run_gripe_audit
    from config import TOPIC_MODEL_PATH
    if not Path(TOPIC_MODEL_PATH).exists():
        console.print("[red]Topic model not trained — run train_topics first.[/red]")
        return
    miner = GripeMiner()
    miner.load()
    with get_connection() as conn:
        findings = run_gripe_audit(conn, miner, strand)
    console.print(f"[green]{len(findings)} gripe finding(s) written[/green]")


def phase_audit_register(strand: str | None):
    from models.register_classifier import RegisterClassifier
    from db import get_connection
    from audit.beat_auditor import run_register_audit
    from config import REGISTER_MODEL_PATH
    if not Path(REGISTER_MODEL_PATH).exists():
        console.print("[red]Register model not trained — run train_register first.[/red]")
        return
    clf = RegisterClassifier()
    clf.load()
    with get_connection() as conn:
        findings = run_register_audit(conn, clf, strand)
    console.print(f"[green]{len(findings)} register bleed finding(s) written[/green]")


def phase_compute_metrics():
    import subprocess
    import sys
    # Call the .NET CLI to compute prose metrics for all beats.
    # Assumes dotnet is on PATH and the project is built.
    root = Path(__file__).parent.parent.parent  # v3/
    cli  = root / "StreetSamurai.Cli" / "StreetSamurai.Cli.csproj"
    result = subprocess.run(
        ["dotnet", "run", "--project", str(cli), "--", "--compute-metrics", "--all"],
        capture_output=True, text=True
    )
    if result.stdout:
        console.print(result.stdout)
    if result.returncode != 0:
        console.print(f"[red]compute_metrics exited {result.returncode}[/red]")
        if result.stderr:
            console.print(result.stderr[:2000])


def phase_find_near_dupes():
    from find_near_dupes import run as find_run
    find_run()


def phase_score_correlation():
    from score_correlation import run as corr_run
    corr_run()


PHASE_FNS = {
    "extract_gripes":    lambda args: phase_extract_gripes(),
    "extract_beats":     lambda args: phase_extract_beats(),
    "train_topics":      lambda args: phase_train_topics(),
    "train_register":    lambda args: phase_train_register(),
    "audit_gripes":      lambda args: phase_audit_gripes(args.strand),
    "audit_register":    lambda args: phase_audit_register(args.strand),
    "compute_metrics":   lambda args: phase_compute_metrics(),
    "find_near_dupes":   lambda args: phase_find_near_dupes(),
    "score_correlation": lambda args: phase_score_correlation(),
}


def main():
    parser = argparse.ArgumentParser(description="StreetSamurai ML pipeline")
    parser.add_argument(
        "--phases", default="all",
        help=f"Comma-separated phases or 'all'. Choices: {', '.join(PHASES)}",
    )
    parser.add_argument("--strand", type=str, default=None,
                        help="Limit audit phases to a single strand slug")
    args = parser.parse_args()

    phases = PHASES if args.phases == "all" else [p.strip() for p in args.phases.split(",")]
    invalid = [p for p in phases if p not in PHASE_FNS]
    if invalid:
        console.print(f"[red]Unknown phases: {invalid}. Valid: {', '.join(PHASES)}[/red]")
        sys.exit(1)

    for phase in phases:
        console.rule(f"[bold cyan]{phase}[/bold cyan]")
        try:
            PHASE_FNS[phase](args)
        except Exception:
            console.print(f"[red]{phase} failed:[/red]")
            traceback.print_exc()


if __name__ == "__main__":
    main()
