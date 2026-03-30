"""
Canon Queue — Quarantines new facts from generated stories for human review.

When the validator identifies new entities, relationships, or facts in
generated text, they are placed in the canon queue as proposals. Nothing
in the queue is canon until a human promotes it.

Usage:
    from engine.canon_queue import submit_to_queue, list_pending, promote, reject
    submit_to_queue(validation_report, scene_path)
    pending = list_pending()
    promote("pending/new_entity_dex.yaml")
    reject("pending/new_entity_dex.yaml", reason="Hallucination")
"""

import shutil
from datetime import datetime
from pathlib import Path

import yaml

from .config import CANON_QUEUE_DIR


def _ensure_dirs() -> None:
    """Create queue directories if they don't exist."""
    (CANON_QUEUE_DIR / "pending").mkdir(parents=True, exist_ok=True)
    (CANON_QUEUE_DIR / "promoted").mkdir(parents=True, exist_ok=True)
    (CANON_QUEUE_DIR / "rejected").mkdir(parents=True, exist_ok=True)


def submit_to_queue(validation_report: dict, scene_source: str) -> list[Path]:
    """
    Extract new entities from a validation report and create queue entries.

    Args:
        validation_report: Output from validator.validate_scene()
        scene_source: Path to the scene that generated these entities

    Returns:
        List of paths to created queue entries
    """
    _ensure_dirs()

    new_entities = validation_report.get("new_entities", [])
    if not new_entities:
        return []

    created = []
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")

    for i, entity in enumerate(new_entities):
        name = entity.get("name", f"unknown_{i}")
        safe_name = "".join(c if c.isalnum() or c in "_-" else "_" for c in name)
        filename = f"{timestamp}_{safe_name}.yaml"
        filepath = CANON_QUEUE_DIR / "pending" / filename

        entry = {
            "name": name,
            "type": entity.get("type", "unknown"),
            "context": entity.get("context", ""),
            "source_scene": scene_source,
            "submitted": datetime.now().isoformat(),
            "status": "pending",
            "notes": "",
        }

        filepath.write_text(
            yaml.dump(entry, default_flow_style=False, allow_unicode=True),
            encoding="utf-8",
        )
        created.append(filepath)

    return created


def list_pending() -> list[dict]:
    """List all pending queue entries."""
    _ensure_dirs()
    entries = []
    for path in sorted((CANON_QUEUE_DIR / "pending").glob("*.yaml")):
        data = yaml.safe_load(path.read_text(encoding="utf-8"))
        data["_path"] = str(path)
        entries.append(data)
    return entries


def promote(entry_path: str, notes: str = "") -> Path:
    """
    Promote a pending entry — marks it as canon-approved.

    The entry is moved to the promoted/ directory. The human is still
    responsible for actually adding the entity to the worldbuilding
    files or entity registry. This just records the decision.
    """
    src = Path(entry_path)
    if not src.exists():
        raise FileNotFoundError(f"Queue entry not found: {entry_path}")

    data = yaml.safe_load(src.read_text(encoding="utf-8"))
    data["status"] = "promoted"
    data["promoted_at"] = datetime.now().isoformat()
    data["notes"] = notes

    dest = CANON_QUEUE_DIR / "promoted" / src.name
    dest.write_text(
        yaml.dump(data, default_flow_style=False, allow_unicode=True),
        encoding="utf-8",
    )
    src.unlink()
    return dest


def reject(entry_path: str, reason: str = "") -> Path:
    """
    Reject a pending entry — marks it as not canon.

    The entry is moved to the rejected/ directory for record-keeping.
    """
    src = Path(entry_path)
    if not src.exists():
        raise FileNotFoundError(f"Queue entry not found: {entry_path}")

    data = yaml.safe_load(src.read_text(encoding="utf-8"))
    data["status"] = "rejected"
    data["rejected_at"] = datetime.now().isoformat()
    data["rejection_reason"] = reason

    dest = CANON_QUEUE_DIR / "rejected" / src.name
    dest.write_text(
        yaml.dump(data, default_flow_style=False, allow_unicode=True),
        encoding="utf-8",
    )
    src.unlink()
    return dest
