#!/bin/bash
# Extracts the trailing JSON object from each --book-audit log (EF Core info: lines precede it)
# and pulls out just the acronym_after_term / gloss_in_voice / lighter_regloss checks.
set -uo pipefail
cd "D:/Projects/MindAttic/StreetSamurai/tmp/audits"

for f in *.json; do
  code="${f%.json}"
  # last line matching exactly "{" starts the real JSON payload
  startline=$(grep -n '^{$' "$f" | tail -1 | cut -d: -f1)
  if [ -z "$startline" ]; then
    echo "$code: NO_JSON_FOUND"
    continue
  fi
  tail -n +"$startline" "$f" > "clean_${code}.json"
  jq -r --arg code "$code" '
    .node_title as $t | .mode as $m |
    (.checks[] | select(.Key=="acronym_after_term" or .Key=="gloss_in_voice" or .Key=="lighter_regloss")) as $c |
    "\($code)|\($t)|\($m)|\($c.Key)|\($c.Status)|\($c.Evidence // "")|\($c.Fix // "")"
  ' "clean_${code}.json" 2>/dev/null || echo "$code: JQ_PARSE_ERROR"
done
