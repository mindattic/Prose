---
name: reader-proxy
description: Alias for /reader-qa — Run Reader-Proxy QA (docs/READER-QA.md) on one or more books — comprehension probes, hash-gated craft/delight checklist, and optionally a findings-only gripe jury. NO scores, NO panels; findings land in the Findings table and a markdown report in audit-outlines-<date>/reader-qa/. Usage /reader-proxy <slug> [--gripe-pass] [--force].
---

# Reader-Proxy QA runbook

This is an alias of `/reader-qa` — same runbook, same behavior, just callable under the
subsystem's own name. Canonical methodology: [docs/READER-QA.md](../../../docs/READER-QA.md).
This is the reader-facing half of QA; the Logic Sweep (`/logic-sweep`) is the continuity half.
**Emits no scores** — measurements and page-anchored findings only.

## Steps

1. **Resolve the book + universe.** Every command needs `--universe glmz|scry|gspl`.
2. **Comprehension + checklist (cheap, hash-cached — run both every time):**
   ```
   ss --reader-qa --slug <slug> --universe <u>
   ss --craft-checklist --slug <slug> --universe <u>
   ```
   Unchanged chapters/beats are cache hits and cost nothing. Exit 1 = findings filed.
3. **Gripe jury (optional, ~$0.50 per full read of a big book — run when the user asks
   for a reader pass, or after a large edit campaign):**
   ```
   ss --reader-qa --gripe-pass --slug <slug> --readers 4 --universe <u>
   ```
4. **Triage the findings** (`list_findings`, categories `ComprehensionDefect` /
   `CraftChecklist` / `ReaderGripe`): fix what a finding names with a minimal splice
   via `update_beat_text`; if you can't name the failure, leave the beat alone and
   `set_finding_status` → Dismissed with reason. High/BLOCKER findings block publish;
   MODERATE/MINOR are editorial judgment.
5. **Gate contested splices** through the duel when in doubt:
   `ss --duel --beat-id <guid> --candidate <file> --allow-votes` — KEEP verdicts return
   dissent rationales as revision fuel.
6. **Re-run step 2** after fixes — findings auto-supersede; unchanged content is free.
   Report lands in `audit-outlines-<date>/reader-qa/<SLUG>.md`.

## Never

- Never run `--review-node` / persona panels / Legion votes as part of this flow —
  legacy machinery, explicit user request only (SS-A44).
- Never treat a pass-fraction or findings count as a score to optimize. Fix named
  failures; nothing else.
