"""
Score every current non-draft beat with the trained quality model,
write Findings for beats below threshold, and optionally write gripe topic Findings.

Usage:
    python beat_auditor.py [--slug <strand_slug>] [--all] [--json]
Exit codes: 0=clean, 1=advisory (>=1 Low finding), 2=blocking (>=1 High finding)
"""
import sys
import os
sys.path.insert(0, os.path.dirname(os.path.dirname(__file__)))

import json
import argparse
from rich.console import Console
from rich.table import Table
from db import get_connection
from models.beat_quality_model import BeatQualityModel
from models.topic_model import GripeMiner
from audit.findings_writer import (
    delete_stale, write_beat_score_finding, write_gripe_finding,
)
from config import BEAT_QUALITY_MODEL_PATH, TOPIC_MODEL_PATH, GRIPE_TOPIC_MIN_PERCENT

console = Console()

ALL_BEATS_SQL = """
SELECT
    s.Slug  AS StrandSlug,
    sb.SortKey,
    ROW_NUMBER() OVER (PARTITION BY sb.StrandId ORDER BY sb.SortKey) AS BeatNumber,
    COUNT(*) OVER (PARTITION BY sb.StrandId)                          AS TotalBeats,
    b.Id    AS BeatId,
    b.Text  AS BeatText
FROM StrandBeats sb
JOIN Beats b      ON b.Id       = sb.BeatId
JOIN Strands s    ON s.Id       = sb.StrandId
WHERE s.IsDraft = 0
  AND sb.IsEnabled = 1
  AND b.Text IS NOT NULL
  AND LEN(TRIM(b.Text)) > 50
"""

STRAND_FILTER_SQL = ALL_BEATS_SQL + " AND s.Slug = ?"

STRAND_GRIPES_SQL = """
SELECT sr.Improvements
FROM StrandReviews sr
JOIN Strands s ON s.Id = sr.StrandId
WHERE s.Slug = ?
  AND sr.Improvements IS NOT NULL
  AND LEN(TRIM(sr.Improvements)) > 5
"""


def run_audit(conn, model: BeatQualityModel, slug: str | None = None) -> list[dict]:
    cursor = conn.cursor()
    if slug:
        cursor.execute(STRAND_FILTER_SQL, (slug,))
    else:
        cursor.execute(ALL_BEATS_SQL)
    rows = cursor.fetchall()

    if not rows:
        console.print("[yellow]No beats found.[/yellow]")
        return []

    # Group by strand
    strands: dict[str, list] = {}
    for row in rows:
        slug_key = row[0]
        if slug_key not in strands:
            strands[slug_key] = []
        strands[slug_key].append({
            "beat_number": int(row[2]),
            "total_beats": int(row[3]),
            "beat_id":     str(row[4]),
            "beat_text":   row[5],
        })

    all_findings = []
    for strand_slug, beats in strands.items():
        console.print(f"[cyan]Auditing {strand_slug} ({len(beats)} beats)...[/cyan]")

        # Clear stale ML findings for this strand
        delete_stale(conn, f"strand:{strand_slug}", "ML-PROSE-SCORE")
        delete_stale(conn, f"strand:{strand_slug}", "ML-PROSE-GRIPE")

        # Score all beats
        texts        = [b["beat_text"] for b in beats]
        beat_numbers = [b["beat_number"] for b in beats]
        total_beats  = [b["total_beats"] for b in beats]

        scores = model.predict(texts, beat_numbers, total_beats)

        for beat, score in zip(beats, scores):
            if score >= 3.5:
                continue
            negatives = model.top_negative_features(
                beat["beat_text"], beat["beat_number"], beat["total_beats"]
            )
            write_beat_score_finding(
                conn,
                strand_slug=strand_slug,
                beat_number=beat["beat_number"],
                predicted_score=float(score),
                top_negative=negatives,
                beat_text_snippet=beat["beat_text"],
            )
            all_findings.append({
                "strand":  strand_slug,
                "beat":    beat["beat_number"],
                "score":   round(float(score), 2),
                "severity": "High" if score < 2.5 else ("Medium" if score < 3.0 else "Low"),
            })

    return all_findings


def run_gripe_audit(conn, miner: GripeMiner, strand_slug: str) -> None:
    cursor = conn.cursor()
    cursor.execute(STRAND_GRIPES_SQL, (strand_slug,))
    gripes = [r[0].strip() for r in cursor.fetchall() if r[0]]
    if not gripes:
        return

    findings = miner.strand_findings(strand_slug, gripes)
    for f in findings:
        write_gripe_finding(
            conn,
            file_path=f["file_path"],
            severity=f["severity"],
            summary=f["summary"],
            suggested_fix=f["suggested_fix"],
        )
    if findings:
        console.print(f"  [green]{len(findings)} gripe topic findings for {strand_slug}[/green]")


def main():
    parser = argparse.ArgumentParser(description="ML beat auditor")
    parser.add_argument("--slug",  type=str, help="Audit a single strand by slug")
    parser.add_argument("--all",   action="store_true", help="Audit all non-draft strands")
    parser.add_argument("--json",  action="store_true", help="Output findings as JSON")
    parser.add_argument("--skip-gripes", action="store_true")
    args = parser.parse_args()

    if not BEAT_QUALITY_MODEL_PATH.exists():
        console.print(f"[red]Model not found: {BEAT_QUALITY_MODEL_PATH}[/red]")
        console.print("Run the nightly orchestrator first: python orchestrate/nightly_run.py --phases extract train_quality")
        sys.exit(2)

    model = BeatQualityModel()
    model.load()

    miner = None
    if not args.skip_gripes and TOPIC_MODEL_PATH.exists():
        miner = GripeMiner()
        miner.load()

    with get_connection() as conn:
        findings = run_audit(conn, model, slug=args.slug if args.slug else None)

        if miner and args.slug:
            run_gripe_audit(conn, miner, args.slug)
        elif miner and args.all:
            # Gripe audit for all strands
            cursor = conn.cursor()
            cursor.execute("SELECT DISTINCT Slug FROM Strands WHERE IsDraft = 0")
            for (slug,) in cursor.fetchall():
                run_gripe_audit(conn, miner, slug)

    if args.json:
        print(json.dumps(findings, indent=2))
    else:
        if not findings:
            console.print("[green]No beats below threshold. Audit clean.[/green]")
        else:
            table = Table(title="ML Beat Audit Findings")
            table.add_column("Strand")
            table.add_column("Beat #", justify="right")
            table.add_column("Predicted Score", justify="right")
            table.add_column("Severity")
            for f in findings:
                color = "red" if f["severity"] == "High" else ("yellow" if f["severity"] == "Medium" else "white")
                table.add_row(f["strand"], str(f["beat"]), str(f["score"]),
                              f"[{color}]{f['severity']}[/{color}]")
            console.print(table)

    has_high = any(f["severity"] == "High" for f in findings)
    has_any  = len(findings) > 0
    sys.exit(2 if has_high else (1 if has_any else 0))


if __name__ == "__main__":
    main()
