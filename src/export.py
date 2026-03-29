import re
from pathlib import Path

from .utils import write_text


def export_to_canon(session_scene_text: str, canon_path: Path) -> Path:
    write_text(canon_path, session_scene_text)
    return canon_path


def extract_outline(text: str) -> str:
    m = re.search(
        r"(?:9-beat outline|next scene outline|beat outline).*?\n(.+?)(?:\n\s*(?:\d+\)|#{1,3}|recurring motifs)|$)",
        text,
        flags=re.S | re.I,
    )
    return m.group(1).strip() if m else ""


def extract_motifs(text: str) -> str:
    m = re.search(
        r"(?:recurring motifs|motifs to reuse).*?\n(.+)$",
        text,
        flags=re.S | re.I,
    )
    return m.group(1).strip() if m else ""


def export_session(session, canon_dir: Path, scene_number: int) -> dict[str, Path]:
    scene_text = session.get_scene_text()
    paths = {}

    scene_path = canon_dir / f"scene_{scene_number:02d}.txt"
    export_to_canon(scene_text, scene_path)
    paths["scene"] = scene_path

    outline = extract_outline(scene_text)
    if outline:
        outline_path = canon_dir / f"scene_{scene_number:02d}_outline.txt"
        write_text(outline_path, outline)
        paths["outline"] = outline_path

    motifs = extract_motifs(scene_text)
    if motifs:
        motifs_path = canon_dir / f"scene_{scene_number:02d}_motifs.txt"
        write_text(motifs_path, motifs)
        paths["motifs"] = motifs_path

    return paths
