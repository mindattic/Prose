"""
Nightly ML orchestration pipeline.

Phases (run order):
    extract        → pull reviews + beat texts from DB → Parquet cache
    train_quality  → train LightGBM beat quality regressor
    train_topics   → train BERTopic gripe miner
    train_persona  → train persona preference model
    audit          → score all current beats → write Findings
    train_beatmode → train SetFit beat-mode classifier

Usage:
    python nightly_run.py [--phases all|extract|train_quality|...]
                          [--strand <slug>]
                          [--skip-retrain]
"""
import sys
import os
sys.path.insert(0, os.path.dirname(os.path.dirname(__file__)))

import argparse
import traceback
from datetime import datetime
from pathlib import Path
from rich.console import Console
import mlflow

from config import (
    MLFLOW_TRACKING_URI, REVIEWS_PARQUET, BEAT_TEXTS_PARQUET,
    TRAINING_PARQUET, TOPIC_MODEL_PATH, BEAT_QUALITY_MODEL_PATH,
)

console = Console()

PHASE_ORDER = [
    "extract",
    "train_quality",
    "train_topics",
    "train_persona",
    "audit",
    "train_beatmode",
]


# ── Phase implementations ────────────────────────────────────────────────────

def phase_extract(args):
    from extract.pull_reviews import run as pull_reviews
    from extract.pull_beat_texts import run as pull_texts, build_training_dataset
    pull_reviews()
    pull_texts()
    build_training_dataset()


def phase_train_quality(args):
    import pandas as pd
    from models.beat_quality_model import BeatQualityModel

    if not Path(TRAINING_PARQUET).exists():
        console.print("[red]Training Parquet not found. Run extract phase first.[/red]")
        return
    df = pd.read_parquet(TRAINING_PARQUET)
    if len(df) < 50:
        console.print(f"[yellow]Only {len(df)} training rows — skipping model training.[/yellow]")
        return
    model = BeatQualityModel()
    model.train(df, mlflow_run=True)
    model.save()


def phase_train_topics(args):
    import pandas as pd
    from models.topic_model import GripeMiner

    gripes_path = str(REVIEWS_PARQUET).replace(".parquet", "_gripes.parquet")
    if not Path(gripes_path).exists():
        console.print("[yellow]Gripes Parquet not found. Run extract first.[/yellow]")
        return
    df = pd.read_parquet(gripes_path)
    texts = df["Improvements"].dropna().str.strip().tolist()
    texts = [t for t in texts if len(t) > 5]

    miner = GripeMiner()
    miner.train(texts)


def phase_train_persona(args):
    import pandas as pd
    from models.persona_preference_model import PersonaPreferenceModel
    from db import get_connection

    if not Path(REVIEWS_PARQUET).exists():
        console.print("[yellow]Reviews Parquet not found. Run extract first.[/yellow]")
        return
    panel_scores = pd.read_parquet(REVIEWS_PARQUET)

    with get_connection() as conn:
        ppm = PersonaPreferenceModel()
        ppm.train(conn, panel_scores)


def phase_audit(args):
    from models.beat_quality_model import BeatQualityModel
    from models.topic_model import GripeMiner
    from audit.beat_auditor import run_audit, run_gripe_audit
    from db import get_connection

    if not Path(BEAT_QUALITY_MODEL_PATH).exists():
        console.print("[yellow]No quality model found — skipping audit.[/yellow]")
        return

    model = BeatQualityModel()
    model.load()

    miner = None
    if Path(TOPIC_MODEL_PATH).exists():
        miner = GripeMiner()
        miner.load()

    with get_connection() as conn:
        slug = getattr(args, "strand", None)
        findings = run_audit(conn, model, slug=slug)

        if miner:
            if slug:
                run_gripe_audit(conn, miner, slug)
            else:
                cursor = conn.cursor()
                cursor.execute("SELECT DISTINCT Slug FROM Strands WHERE IsDraft = 0")
                for (s,) in cursor.fetchall():
                    run_gripe_audit(conn, miner, s)

    n_high = sum(1 for f in findings if f["severity"] == "High")
    n_med  = sum(1 for f in findings if f["severity"] == "Medium")
    n_low  = sum(1 for f in findings if f["severity"] == "Low")
    console.print(f"[green]Audit complete: {n_high} High, {n_med} Medium, {n_low} Low findings[/green]")

    if mlflow.active_run():
        mlflow.log_metrics({
            "findings_high":   n_high,
            "findings_medium": n_med,
            "findings_low":    n_low,
        })


def phase_train_beatmode(args):
    from models.beatmode_classifier import BeatModeClassifier, pull_training_data
    from db import get_connection

    with get_connection() as conn:
        texts, labels = pull_training_data(conn)

    if len(texts) < 30:
        console.print(f"[yellow]Only {len(texts)} labeled beat goals — skipping SetFit training.[/yellow]")
        return

    clf = BeatModeClassifier()
    clf.train(texts, labels)


PHASES = {
    "extract":        phase_extract,
    "train_quality":  phase_train_quality,
    "train_topics":   phase_train_topics,
    "train_persona":  phase_train_persona,
    "audit":          phase_audit,
    "train_beatmode": phase_train_beatmode,
}


# ── Main ─────────────────────────────────────────────────────────────────────

def main():
    parser = argparse.ArgumentParser(description="StreetSamurai ML nightly pipeline")
    parser.add_argument(
        "--phases", nargs="+",
        choices=list(PHASES.keys()) + ["all"],
        default=["all"],
        help="Which phases to run (default: all)",
    )
    parser.add_argument("--strand",       type=str, help="Audit only this strand slug")
    parser.add_argument("--skip-retrain", action="store_true",
                        help="Skip all train_* phases (use cached models)")
    args = parser.parse_args()

    phases_to_run = PHASE_ORDER if "all" in args.phases else [
        p for p in PHASE_ORDER if p in args.phases
    ]
    if args.skip_retrain:
        phases_to_run = [p for p in phases_to_run if not p.startswith("train_")]

    console.print(f"[bold cyan]StreetSamurai ML Nightly — {datetime.now():%Y-%m-%d %H:%M}[/bold cyan]")
    console.print(f"Phases: {', '.join(phases_to_run)}")

    mlflow.set_tracking_uri(MLFLOW_TRACKING_URI)
    run_name = f"nightly_{datetime.now():%Y%m%d_%H%M}"

    # Phases that must not run if extract produced stale/missing data
    _EXTRACT_DEPENDENT = {"train_quality", "train_topics", "train_persona", "audit"}
    failed_phases: set[str] = set()

    with mlflow.start_run(run_name=run_name):
        for phase in phases_to_run:
            if phase in _EXTRACT_DEPENDENT and "extract" in failed_phases:
                console.print(f"[yellow]{phase} skipped — extract phase failed (stale Parquet risk)[/yellow]")
                continue

            t0 = datetime.now()
            console.rule(f"[bold]{phase}[/bold]")
            try:
                PHASES[phase](args)
                elapsed = (datetime.now() - t0).total_seconds()
                console.print(f"[green]{phase} complete ({elapsed:.0f}s)[/green]")
            except Exception:
                console.print(f"[red]{phase} FAILED:[/red]")
                traceback.print_exc()
                failed_phases.add(phase)

    console.rule("[bold green]Nightly run complete[/bold green]")


if __name__ == "__main__":
    main()
