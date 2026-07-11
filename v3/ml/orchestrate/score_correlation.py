"""
score_correlation.py — Beat-level score correlation model.

Algorithm:
  1. JOIN NodeReviewBeatScores × BeatProseMetrics on BeatId.
  2. Features: WordCount, AvgWordsPerSentence, TypeTokenRatio, LexicalDiversityMtld,
               FleschKincaidGrade, FleschReadingEase, AvgSyllablesPerWord, DialogueProportion.
     Optional: EmotionalScore from Beats (when available).
  3. Target: mean NodeReviewBeatScores.Score per beat (1-5).
  4. Model: GradientBoostingRegressor with 5-fold CV.
  5. Feature importance: permutation_importance (no SHAP dep needed).
  6. Write report to %APPDATA%/MindAttic/ML/score_correlation_latest.txt.

Usage:
    python orchestrate/score_correlation.py

Called by nightly_run.py as phase "score_correlation".
"""
import sys
import os
sys.path.insert(0, os.path.dirname(os.path.dirname(__file__)))

import os
from pathlib import Path
from rich.console import Console
from db import get_connection, fetchdf
from config import ARTIFACTS

console = Console()

OUTPUT_PATH = ARTIFACTS / "score_correlation_latest.txt"

QUERY = """
SELECT
    CONVERT(nvarchar(36), m.BeatId)   AS BeatId,
    AVG(CAST(s.Score AS float))       AS MeanScore,
    m.WordCount,
    m.AvgWordsPerSentence,
    m.TypeTokenRatio,
    m.LexicalDiversityMtld,
    m.FleschKincaidGrade,
    m.FleschReadingEase,
    m.AvgSyllablesPerWord,
    m.DialogueProportion,
    b.EmotionalScore                  AS EmotionalScore
FROM BeatProseMetrics m
JOIN Beats b             ON b.Id       = m.BeatId
JOIN NodeReviews r       ON r.NodeId   = m.NodeId
JOIN NodeReviewBeatScores s ON s.ReviewId = r.Id AND s.BeatNumber = b.Number
GROUP BY
    m.BeatId, m.NodeId,
    m.WordCount, m.AvgWordsPerSentence, m.TypeTokenRatio,
    m.LexicalDiversityMtld, m.FleschKincaidGrade, m.FleschReadingEase,
    m.AvgSyllablesPerWord, m.DialogueProportion,
    b.EmotionalScore
HAVING COUNT(s.Score) >= 2
"""

FEATURES = [
    "WordCount", "AvgWordsPerSentence", "TypeTokenRatio",
    "LexicalDiversityMtld", "FleschKincaidGrade", "FleschReadingEase",
    "AvgSyllablesPerWord", "DialogueProportion", "EmotionalScore",
]


def run():
    import pandas as pd
    import numpy as np
    from sklearn.ensemble import GradientBoostingRegressor
    from sklearn.inspection import permutation_importance
    from sklearn.model_selection import cross_val_score

    with get_connection() as conn:
        df = fetchdf(conn, QUERY)

    if df.empty or len(df) < 20:
        console.print(f"[yellow]Only {len(df)} scored beats -- need >=20 to train. Run reviews first.[/yellow]")
        return

    # Deduplicate by BeatId — if a beat somehow appears more than once (edge case
    # from multi-chapter node memberships), keep the first occurrence.
    df = df.drop_duplicates(subset=["BeatId"]).reset_index(drop=True)
    console.print(f"[green]Training on {len(df)} beats with >=2 reviews.[/green]")

    # Fill missing EmotionalScore with column mean
    df["EmotionalScore"] = pd.to_numeric(df["EmotionalScore"], errors="coerce")
    df["EmotionalScore"] = df["EmotionalScore"].fillna(df["EmotionalScore"].mean())

    X = df[FEATURES].fillna(0).values
    y = df["MeanScore"].values

    model = GradientBoostingRegressor(n_estimators=200, max_depth=4, random_state=42)

    # 5-fold CV
    cv_scores = cross_val_score(model, X, y, cv=5, scoring="neg_root_mean_squared_error")
    rmse_mean = -cv_scores.mean()
    rmse_std  = cv_scores.std()

    # Fit on full data for importance
    model.fit(X, y)
    r2 = model.score(X, y)

    result = permutation_importance(model, X, y, n_repeats=20, random_state=42)
    importances = sorted(
        zip(FEATURES, result.importances_mean),
        key=lambda x: -x[1]
    )

    lines = [
        f"Score Correlation Model - {pd.Timestamp.now():%Y-%m-%d}",
        f"Training beats    : {len(df)}",
        f"CV RMSE (5-fold)  : {rmse_mean:.3f} +/- {rmse_std:.3f}",
        f"Train R^2         : {r2:.3f}",
        "",
        "Feature Importance (permutation, descending):",
    ]
    for feat, imp in importances:
        bar = "#" * max(0, int(imp * 30))
        lines.append(f"  {feat:<28} {imp:+.4f}  {bar}")

    report = "\n".join(lines)
    console.print(report)

    OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT_PATH.write_text(report, encoding="utf-8")
    console.print(f"\n[green]Report written to: {OUTPUT_PATH}[/green]")


if __name__ == "__main__":
    run()
