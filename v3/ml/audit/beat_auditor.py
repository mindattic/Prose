"""
ML beat auditor: gripe topic analysis + register bleed detection.

Usage:
    python beat_auditor.py --gripes [--strand <slug>]
    python beat_auditor.py --register [--strand <slug>]
    python beat_auditor.py --all [--strand <slug>]
Exit codes: 0=clean, 1=findings present
"""
import sys
import os
sys.path.insert(0, os.path.dirname(os.path.dirname(__file__)))

import argparse
from pathlib import Path
from rich.console import Console
from rich.table import Table
from db import get_connection
from models.topic_model import GripeMiner
from models.register_classifier import RegisterClassifier
from audit.findings_writer import delete_stale, write_gripe_finding, write_register_finding
from config import TOPIC_MODEL_PATH, REGISTER_MODEL_PATH

console = Console()

ALL_BEATS_SQL = """
SELECT
    n.Slug AS StrandSlug,
    ROW_NUMBER() OVER (PARTITION BY bn.NodeId ORDER BY bn.SortKey) AS BeatNumber,
    CONVERT(nvarchar(36), b.Id) AS BeatId,
    b.Text AS BeatText
FROM BeatNodes bn
JOIN Beats b  ON b.Id  = bn.BeatId
JOIN Nodes n  ON n.Id  = bn.NodeId
WHERE bn.IsEnabled = 1
  AND b.Text IS NOT NULL
  AND LEN(TRIM(b.Text)) > 100
"""

STRAND_GRIPES_SQL = """
SELECT nr.Improvements AS GripeText
FROM NodeReviews nr
JOIN Nodes n ON n.Id = nr.NodeId
WHERE n.Slug  = ?
  AND nr.Improvements IS NOT NULL
  AND LEN(TRIM(nr.Improvements)) > 10
"""


def run_gripe_audit(conn, miner: GripeMiner, slug: str | None) -> list[dict]:
    cursor = conn.cursor()
    if slug:
        slugs_to_audit = [slug]
    else:
        cursor.execute("SELECT DISTINCT Slug FROM Nodes")
        slugs_to_audit = [r[0] for r in cursor.fetchall()]

    all_findings = []
    for s in slugs_to_audit:
        cursor.execute(STRAND_GRIPES_SQL, (s,))
        gripes = [r[0].strip() for r in cursor.fetchall() if r[0]]
        if not gripes:
            continue
        delete_stale(conn, f"strand:{s}", "ML-PROSE-GRIPE")
        findings = miner.strand_findings(s, gripes)
        for f in findings:
            write_gripe_finding(conn, file_path=f"strand:{s}",
                                severity=f["severity"], summary=f["summary"],
                                suggested_fix=f["suggested_fix"])
        all_findings.extend(findings)
    return all_findings


def run_register_audit(conn, clf: RegisterClassifier, slug: str | None) -> list[dict]:
    sql = ALL_BEATS_SQL + (" AND s.Slug = ?" if slug else "")
    cursor = conn.cursor()
    cursor.execute(sql, (slug,) if slug else ())
    rows = cursor.fetchall()

    strands: dict[str, list] = {}
    for row in rows:
        strands.setdefault(row[0], []).append({
            "beat_number": int(row[1]),
            "beat_id":     row[2],
            "text":        row[3],
        })

    all_findings = []
    for strand_slug, beats in strands.items():
        if strand_slug not in clf.trained_slugs:
            console.print(f"[yellow]  {strand_slug} not in register model - skipping[/yellow]")
            continue
        console.print(f"[cyan]Register check: {strand_slug} ({len(beats)} beats)[/cyan]")
        delete_stale(conn, f"strand:{strand_slug}", "ML-REGISTER-BLEED")
        for beat in beats:
            result = clf.check_bleed(beat["text"], strand_slug)
            if not result["bleed"]:
                continue
            write_register_finding(
                conn,
                strand_slug=strand_slug,
                beat_number=beat["beat_number"],
                predicted_slug=result["predicted_slug"],
                confidence=result["confidence"],
                beat_text_snippet=beat["text"],
            )
            all_findings.append({
                "strand":     strand_slug,
                "beat":       beat["beat_number"],
                "predicted":  result["predicted_slug"],
                "confidence": result["confidence"],
            })
    return all_findings


def main():
    parser = argparse.ArgumentParser(description="ML beat auditor")
    parser.add_argument("--gripes",   action="store_true", help="Run gripe topic audit")
    parser.add_argument("--register", action="store_true", help="Run register bleed audit")
    parser.add_argument("--all",      action="store_true", help="Run both audits")
    parser.add_argument("--strand",   type=str,            help="Limit to a single strand slug")
    args = parser.parse_args()

    run_gripes   = args.gripes   or args.all
    run_register = args.register or args.all

    miner = None
    if run_gripes:
        if not Path(TOPIC_MODEL_PATH).exists():
            console.print(f"[red]Topic model not found: {TOPIC_MODEL_PATH}[/red]")
            console.print("Run: python orchestrate/nightly_run.py --phases extract_gripes,train_topics")
            sys.exit(2)
        miner = GripeMiner()
        miner.load()

    clf = None
    if run_register:
        if not Path(REGISTER_MODEL_PATH).exists():
            console.print(f"[red]Register model not found: {REGISTER_MODEL_PATH}[/red]")
            console.print("Run: python orchestrate/nightly_run.py --phases extract_beats,train_register")
            sys.exit(2)
        clf = RegisterClassifier()
        clf.load()

    with get_connection() as conn:
        gripe_findings    = run_gripe_audit(conn, miner, args.strand)    if miner else []
        register_findings = run_register_audit(conn, clf, args.strand)   if clf   else []

    total = len(gripe_findings) + len(register_findings)
    if total == 0:
        console.print("[green]Audit clean - no findings.[/green]")
    else:
        if gripe_findings:
            t = Table(title="Gripe Topic Findings")
            t.add_column("Severity")
            t.add_column("Summary")
            for f in gripe_findings:
                t.add_row(f["severity"], f["summary"])
            console.print(t)
        if register_findings:
            t = Table(title="Register Bleed Findings")
            t.add_column("Strand")
            t.add_column("Beat #", justify="right")
            t.add_column("Predicted As")
            t.add_column("Confidence", justify="right")
            for f in register_findings:
                t.add_row(f["strand"], str(f["beat"]), f["predicted"], f"{f['confidence']:.0%}")
            console.print(t)

    sys.exit(1 if total > 0 else 0)


if __name__ == "__main__":
    main()
