import re
from pathlib import Path


def write_text(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def safe_filename(s: str) -> str:
    s = re.sub(r"[^a-zA-Z0-9_\-\.]+", "_", s.strip())
    return s[:80] if s else "untitled"
