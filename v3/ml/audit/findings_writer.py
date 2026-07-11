"""
Write ML findings to the Findings table, mirroring FindingsService.Upsert semantics.

DedupKey contract (must match C# exactly):
    dedup = f"{file_path}|{category}|{summary}".lower()[:450]
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

FIND_DEDUP_SQL  = "SELECT Id FROM Findings WHERE DedupKey = ?"

DELETE_PREFIX_SQL = """
DELETE FROM Findings
WHERE FilePath LIKE ? ESCAPE '!'
  AND Summary  LIKE ? ESCAPE '!'
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
    dedup  = _dedup_key(file_path, category, summary)
    now    = datetime.utcnow()
    cursor = conn.cursor()
    cursor.execute(FIND_DEDUP_SQL, (dedup,))
    row = cursor.fetchone()
    if row:
        cursor.execute(UPDATE_SQL, (now, severity, snippet, suggested_fix, dedup))
    else:
        cursor.execute(INSERT_SQL, (now, file_path, category, severity, summary,
                                   snippet, suggested_fix, dedup))


def _like_escape(s: str) -> str:
    """Escape SQL Server LIKE special chars when using ESCAPE '!'."""
    return s.replace("!", "!!").replace("%", "!%").replace("_", "!_")


def delete_stale(conn, file_path_prefix: str, summary_prefix: str) -> int:
    cursor = conn.cursor()
    cursor.execute(DELETE_PREFIX_SQL,
                   (_like_escape(file_path_prefix) + "%",
                    _like_escape(summary_prefix)   + "%"))
    return cursor.rowcount


def write_gripe_finding(conn, file_path: str, severity: str, summary: str, suggested_fix: str) -> None:
    upsert(conn, file_path=file_path, severity=severity, summary=summary,
           snippet=None, suggested_fix=suggested_fix)


def write_register_finding(
    conn,
    strand_slug: str,
    beat_number: int,
    predicted_slug: str,
    confidence: float,
    beat_text_snippet: str,
) -> None:
    summary = (
        f"ML-REGISTER-BLEED: Beat #{beat_number} reads as '{predicted_slug}' "
        f"({confidence:.0%} confidence)"
    )
    snippet = (beat_text_snippet[:200] + "…") if len(beat_text_snippet) > 200 else beat_text_snippet
    suggested_fix = (
        f"Vocabulary in this beat registers as '{predicted_slug}' rather than '{strand_slug}'. "
        "Check for: arithmetic/gap/parliament/filing (Kyle), Signal/Noise/instinct (Sasha), "
        "military/obligation/boisterous (Bear), tired/empathetic (Ekow), "
        "or other protagonist-specific cognitive register bleeding in."
    )
    upsert(
        conn,
        file_path=f"strand:{strand_slug}",
        severity="Medium",
        summary=summary,
        snippet=snippet,
        suggested_fix=suggested_fix,
    )
