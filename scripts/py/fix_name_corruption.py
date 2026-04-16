"""
fix_name_corruption.py — one-shot repair for wiki markup written into entity identity fields.

Run once: py -3 scripts/py/fix_name_corruption.py
"""
import io
import json
import re
import sys
from pathlib import Path

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")

WIKI_RE = re.compile(r'\[\[([^\]|]+)\|[^\]]+\]\]')
CLEAN_KEYS = {"name", "title", "term", "codename", "product_name", "brand_name",
              "full_legal_name", "headline"}

script_dir = Path(__file__).parent
data_dir = (script_dir / "../../engine/data").resolve()

if not data_dir.exists():
    print(f"ERROR: data dir not found: {data_dir}")
    sys.exit(1)

print(f"Scanning: {data_dir}")

fixed = 0
errors = 0

for jf in sorted(data_dir.rglob("*.json")):
    try:
        raw = jf.read_text(encoding="utf-8")
        if "[[" not in raw:
            continue
        data = json.loads(raw)
        if not isinstance(data, dict):
            continue

        changed = False

        for key in CLEAN_KEYS:
            if key in data and isinstance(data[key], str) and "[[" in data[key]:
                cleaned = WIKI_RE.sub(r"\1", data[key])
                print(f"  {jf.name}  [{key}]  {data[key]!r} → {cleaned!r}")
                data[key] = cleaned
                changed = True

        if "aliases" in data and isinstance(data["aliases"], list):
            for i, a in enumerate(data["aliases"]):
                if isinstance(a, str) and "[[" in a:
                    cleaned = WIKI_RE.sub(r"\1", a)
                    print(f"  {jf.name}  [aliases[{i}]]  {a!r} → {cleaned!r}")
                    data["aliases"][i] = cleaned
                    changed = True

        if "common_names" in data and isinstance(data["common_names"], list):
            for i, cn in enumerate(data["common_names"]):
                if isinstance(cn, str) and "[[" in cn:
                    cleaned = WIKI_RE.sub(r"\1", cn)
                    print(f"  {jf.name}  [common_names[{i}]]  {cn!r} → {cleaned!r}")
                    data["common_names"][i] = cleaned
                    changed = True

        if changed:
            jf.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding="utf-8")
            fixed += 1

    except Exception as e:
        print(f"ERROR {jf}: {e}")
        errors += 1

print(f"\nFixed {fixed} files, {errors} errors.")
