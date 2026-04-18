"""
Fix stale names in character descriptions.

After name regeneration, descriptions still reference old names.
This script extracts the old name from the description text and
replaces all occurrences with the current character name.

Patterns handled:
  - "OldName is ..." (980 characters)
  - "OldName, ..." (72 characters)
  - "OldName has ..." (38 characters)
  - "OldName verbs/other ..." (57 characters)
  - First-name-only references within the body text

Usage:
  python fix_description_names.py          # dry run
  python fix_description_names.py --apply  # write changes
"""

import json
import glob
import os
import re
import sys

sys.stdout.reconfigure(encoding="utf-8")

PEOPLE_DIR = os.path.join(os.path.dirname(__file__), "..", "..", "engine", "data", "people")
SKIP_CHARACTERS = {"Kyle Ellen Corbin-Vasik"}

# Pattern to extract old name from description start.
# Matches: one or more capitalized words (possibly hyphenated), optionally preceded by honorific.
# Stops before a lowercase word, verb, comma, or other sentence structure.
NAME_PATTERN = re.compile(
    r"^((?:Mrs?\.|Dr\.|Prof\.)\s+)?"  # optional honorific
    r"([\w\u00C0-\u024F]+(?:[-\s][\w\u00C0-\u024F]+)*?)"  # name (first + possibly last)
    r"(?=\s+(?:is|are|has|had|was|were|stands|moves|runs|operates|wears|carries|dives|occupies|looks|sits|walks|leans|keeps|seems|appears|works|lives|drives|speaks|talks|holds|opens|steps|fills|enters|turns|goes|makes|takes|gives|comes|puts|gets|uses|plays|brings|sells|deals|trades|owns|manages|fights|trains|teaches|guards|patrols|watches|hunts|hides|lurks|prowls|hovers|floats|glides|crawls|limps|shuffles|stalks|strides|saunters|ambles|trudges|came|chose|used|doesn),?\s)"
)

# Broader fallback: first word(s) before a verb-like word
FALLBACK_PATTERN = re.compile(
    r"^([\w\u00C0-\u024F]+(?:[-\s][\w\u00C0-\u024F]+)*?)\s+"
    r"(?:is|are|has|had|was|were|stands|moves|runs|operates|wears|carries|dives|occupies|looks|sits|walks|leans|keeps|seems|appears|works|lives|drives|speaks|talks|holds|opens|steps|came|chose|used|doesn|a\s|the\s|an\s|in\s|at\s|on\s|with\s|from\s|into\s)"
)


def extract_old_name(description):
    """Try to extract the old character name from the description text."""
    if not description:
        return None, None

    m = NAME_PATTERN.match(description)
    if m:
        honorific = m.group(1) or ""
        name = m.group(2).strip()
        return honorific + name, name

    m = FALLBACK_PATTERN.match(description)
    if m:
        name = m.group(1).strip()
        # Sanity check: should start with uppercase and be a plausible name
        if name and name[0].isupper() and len(name) > 1:
            return name, name

    # Handle "Name -- " (em dash pattern)
    m = re.match(r"^([\w\u00C0-\u024F]+(?:[-\s][\w\u00C0-\u024F]+)*?)\s+\u2014\s", description)
    if m:
        name = m.group(1).strip()
        if name and name[0].isupper():
            return name, name

    # Last resort: first word if it looks like a name
    first_word = description.split()[0] if description.split() else None
    if first_word and first_word[0].isupper() and len(first_word) > 2 and first_word.isalpha():
        return first_word, first_word

    return None, None


def replace_name_in_text(text, old_full, old_first, new_full, new_first):
    """Replace old name references with new name in text."""
    if not text or not old_full:
        return text, False

    changed = False

    # Replace full old name with full new name
    if old_full in text:
        text = text.replace(old_full, new_full)
        changed = True

    # Replace old first name with new first name (word-boundary safe)
    if old_first and old_first != old_full and len(old_first) > 2:
        pattern = re.compile(r"\b" + re.escape(old_first) + r"\b")
        if pattern.search(text):
            text = pattern.sub(new_first, text)
            changed = True

    return text, changed


def main():
    apply = "--apply" in sys.argv

    files = sorted(glob.glob(os.path.join(PEOPLE_DIR, "*.json")))
    fixed = 0
    skipped = 0
    failed = []

    for fp in files:
        try:
            with open(fp, "r", encoding="utf-8") as f:
                data = json.load(f)

            name = data.get("name", "")
            desc = data.get("description", "")
            if not name or not desc:
                continue
            if name in SKIP_CHARACTERS:
                continue

            new_first = name.split()[0]

            # Check if current name already appears in description
            if new_first in desc:
                continue

            old_full, old_first = extract_old_name(desc)
            if not old_full:
                failed.append((name, desc[:80]))
                continue

            new_full = name
            new_desc, changed = replace_name_in_text(desc, old_full, old_first, new_full, new_first)

            if not changed:
                failed.append((name, desc[:80]))
                continue

            if apply:
                data["description"] = new_desc
                with open(fp, "w", encoding="utf-8") as f:
                    json.dump(data, f, indent=2, ensure_ascii=False)

            fixed += 1
            if fixed <= 10:
                print(f"  {old_full} -> {new_full}")
                print(f"    {desc[:60]}...")
                print(f"    {new_desc[:60]}...")
                print()

        except Exception as e:
            print(f"  ERROR on {fp}: {e}")

    print(f"{'Fixed' if apply else 'Would fix'}: {fixed}")
    print(f"Skipped (already correct): {skipped}")
    print(f"Failed to extract old name: {len(failed)}")
    if failed:
        print("\nFailed cases:")
        for n, d in failed[:15]:
            print(f"  {n}: {d}...")


if __name__ == "__main__":
    main()
