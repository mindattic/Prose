"""
Write ML findings to the Findings table, mirroring FindingsService.Upsert semantics.

DedupKey contract (must match C# exactly):
    dedup = f"{file_path}|{category}|{summary}".lower()[:450]

The C# uses:
    var dedup = $"{filePath}|{category}|{summary}".ToLowerInvariant();
    if (dedup.Length > 450) dedup = dedup[..450];

Note: {category} is the enum value ("Other"), then ToLowerInvariant makes it "other".
"""
import sys
import os
sys.path.insert(0, os.path.dirname(os.path.dirname(__file__)))

from datetime import datetime
from rich.console import Console

console = Console()

CATEGORY = "Other"

INSERT_SQL = """
INSERT INTO Findings (DetectedAt, FilePath, ChapterId, Category, Severity, Summary, Snippet, SuggestedFix, Status, DedupKey)
VALUES (?, ?, NULL, ?, ?, ?, ?, ?, 'New', ?)
"""

UPDATE_SQL = """
UPDATE Findings
SET DetectedAt   = ?,
    Severity     = ?,
    Snippet      = ?,
    SuggestedFix = ?,
    Status       = 'New'
WHERE DedupKey = ?
"""

FIND_DEDUP_SQL = "SELECT Id FROM Findings WHERE DedupKey = ?"

DELETE_PREFIX_SQL = """
DELETE FROM Findings
WHERE FilePath LIKE ?
  AND Summary LIKE ?
"""


def _dedup_key(file_path: str, category: str, summary: str) -> str:
    raw = f"{file_path}|{category}|{summary}".lower()
    return raw[:450]


def upsert(
    conn,
    file_path: str,
    severity: str,
    summary: str,
    snippet: str | None,
    suggested_fix: str | None,
    category: str = CATEGORY,
) -> None:
    dedup = _dedup_key(file_path, category, summary)
    now   = datetime.utcnow()
    cursor = conn.cursor()
    cursor.execute(FIND_DEDUP_SQL, (dedup,))
    row = cursor.fetchone()
    if row:
        cursor.execute(UPDATE_SQL, (now, severity, snippet, suggested_fix, dedup))
    else:
        cursor.execute(INSERT_SQL, (now, file_path, category, severity, summary, snippet, suggested_fix, dedup))


def delete_stale(conn, file_path_prefix: str, summary_prefix: str) -> int:
    """Delete all findings whose FilePath starts with prefix AND Summary starts with prefix."""
    cursor = conn.cursor()
    cursor.execute(DELETE_PREFIX_SQL, (file_path_prefix + "%", summary_prefix + "%"))
    return cursor.rowcount


def write_beat_score_finding(
    conn,
    strand_slug: str,
    beat_number: int,
    predicted_score: float,
    top_negative: list[tuple[str, float]],
    beat_text_snippet: str,
) -> None:
    if predicted_score >= 3.5:
        return

    severity = (
        "High"   if predicted_score < 2.5  else
        "Medium" if predicted_score < 3.0  else
        "Low"
    )

    top_feature = top_negative[0][0] if top_negative else "unknown"
    summary = f"ML-PROSE-SCORE: Beat #{beat_number} predicted {predicted_score:.1f}/5 — weak on {top_feature}"

    if top_negative:
        drivers = ", ".join(
            f"{name} (SHAP {val:+.2f})" for name, val in top_negative[:3]
        )
        suggested_fix = (
            f"Top negative drivers: {drivers}. "
            f"Consider: vary sentence length, increase direct dialogue proportion, "
            f"add specific sensory details, or intensify the emotional undercurrent."
        )
    else:
        suggested_fix = "Review prose for variety in sentence structure, dialogue balance, and sensory grounding."

    snippet = (beat_text_snippet[:200] + "…") if len(beat_text_snippet) > 200 else beat_text_snippet

    upsert(
        conn,
        file_path=f"strand:{strand_slug}",
        severity=severity,
        summary=summary,
        snippet=snippet,
        suggested_fix=suggested_fix,
    )


def write_gripe_finding(conn, file_path: str, severity: str, summary: str, suggested_fix: str) -> None:
    upsert(conn, file_path=file_path, severity=severity, summary=summary,
           snippet=None, suggested_fix=suggested_fix)
