---
codex: 1
project: Prose
layer: methodology
status: locked
updated: 2026-07-18
---

# THE LOGIC SWEEP — canonical QA methodology {#SS-LOGIC}

> **Status: LAW** (see [[AMENDMENTS#SS-A44]]). The logic sweep is the default and mandatory
> quality mechanism for every story. It replaced score panels and votes as of 2026-07-04 —
> voting happens **only on explicit user request**, never automatically. Origin: the
> 2026-07-03/04 corpus campaign, where logic sweeps found and fixed ~25 blocker-level defects
> that months of score panels had felt but never localized (full evidence:
> `audit-outlines-2026-07-03/logic/CORPUS-REPORT.md`).

## 1. When it runs {#SS-LOGIC-1}

A logic sweep is REQUIRED:
- after any beat is written, rewritten, merged, split, re-ordered, or disabled;
- before any export (`--export-node`) or release;
- after any bible edit that states story facts (sweep verifies prose agreement);
- on demand, any time a story "feels off" — the sweep names the failure or clears the story.

Scope the sweep to what changed plus its blast radius (the touched beats, their chapters, and
every beat that references their content). A full-story sweep is required when structure
changed (merges, disables, re-ordering) — those create orphans far from the edit site.

## 2. What it reads {#SS-LOGIC-2}

The LIVE story only: enabled beats in reading order —
`JOIN NodeBeats nb ... WHERE nb.IsEnabled = 1 ORDER BY nb.SortKey`.
Books read chapter children in `Nodes.SortKey` order, beats within each by `NodeBeats.SortKey`.
**Never `Beats.Number`** — it is a global counter, not reading order, and sorting by it
manufactures false findings (proven twice in the origin campaign).

## 3. The six dimensions {#SS-LOGIC-3}

Audit every story against all six. Findings cite SortKeys and quote the offending text.

1. **Causality chain.** Walk the plot as cause → effect. Every event has an established cause;
   every decision an established motivation; every capability an on-page origin *before* its
   use. (Origin-campaign catches: a dead-man's-send used in a confrontation but never staged;
   a wound described as healed the night before it is inflicted.)
2. **Knowledge states.** Per named character: what they know and the beat where they learned
   it. Nobody acts on knowledge they don't have; nobody is ignorant of what they were shown
   learning. Includes NAMES — a name used before its on-page coining/learning beat is a
   knowledge-state break. (Catches: Sparrow named 5× before her naming scene; a finale
   narrating a name the POV never learned.)
3. **Timeline.** Reconstruct the story clock from every stated time/date/span. Flag
   impossibilities (an effect timestamped before its cause; one character in two places at the
   same minute) and unexplained gaps. Respect DELIBERATE impossibilities marked in the bible
   (e.g., ATTE's 6:02→0540 texture) — the bible names them; don't "fix" them.
4. **Plant/payoff ledger.** Two-way table: every planted element → where it pays (or ORPHANED);
   every payoff → where it was planted (or UNPLANTED). Also ARITHMETIC: every count, sum,
   ledger, and roster stated in prose must reconcile — build the actual ledger and walk it.
   (Catches: a population count that never summed across six chapters; a closing line whose
   components added to 28 while it said thirty-one.)
5. **Orphan references.** Nothing enabled references removed/disabled/merged content — hunt
   with greps for distinctive phrases from every disabled beat. Highest-risk after structural
   edits. Also inverse-orphans: metadata stranded on disabled beats (chapter-start flags,
   titles) — `IsChapterStart` on a disabled beat silently deletes the chapter heading from
   every export.
6. **Bible agreement.** Prose and `docs/nodes/<CODE>.md` tell the same story. When they
   disagree: the bible wins on canonical FACTS — unless the finding shows the bible is stale
   (describes a superseded draft), in which case fix the bible in the same change and run
   `codex digest` + `doctor`. Never leave the disagreement standing. This dimension is
   checked ACROSS ALTITUDES (see [§8](#SS-LOGIC-8)): chapter synopses
   (`NodeChapterSummaries` / `story-synopsis.txt`) are the 100-ft instrument, and
   `prose --altitude-audit --slug <slug>` automates the 10,000↔100 ft comparison. Sweeps may
   read `story-synopsis.txt` for cheap chapter-altitude scoping before deep beat reads.

## 4. Triage and fix protocol {#SS-LOGIC-4}

- **Quote grounding, before anything else (SS-LOGIC-4a).** Audit agents occasionally
  misattribute a quote to the wrong SortKey/beat, or fabricate one under time pressure —
  proven twice in the 2026-07-24 VIGL sweep (one turned out to be a beat-ID mix-up; a second
  looked unverifiable at a glance and needed a closer read to confirm as real). Before triaging
  ANY finding that quotes beat text, mechanically confirm the quote exists in the beat it's
  attributed to:
  `prose --verify-quote --id <beatId> --quote "<claimed text>" --claimed-by "<agent/pass name>"`
  or, for a whole audit report at once, `prose --verify-quotes-batch --json-file <path>` (array of
  `{"beatId":"<guid>","quote":"<text>"}`). MCP equivalents: `VerifyQuoteGrounding` /
  `VerifyQuoteGroundingBatch`. Comparison is normalized (dash variants, curly/straight quotes,
  whitespace) so console-display drift never causes a false Fail — only a genuine
  misattribution or fabrication does. A Fail means: discard or re-verify that finding by
  reading the actual beat yourself before it enters triage. Results are persisted to
  `BeatVerifications` (`CheckType='QuoteGrounding'`, always inserted, never overwritten — one
  row per claim ever checked, so the audit trail accumulates across sweeps). This gate is
  mechanical and cheap; run it on every quoted finding, not just ones that feel suspicious —
  the ones that felt fine were exactly the ones that turned out wrong.
- Severity: **BLOCKER** (reader-visible contradiction) / **MODERATE** (weakens logic) /
  **MINOR**. Fix blockers always; moderates almost always; minors when the splice is one
  word/clause. If you cannot NAME the failure, leave the beat alone.
- **Minimal splices.** Pull live text, change only the finding's sentences. Prefer, in order:
  a data fix (SortKey re-seat, soft-disable, metadata) → a one-clause prose splice → a
  passage → a rewrite. Never pad; never restructure to satisfy a checklist (if a verification
  gate conflicts with story logic, report the deviation honestly instead of forcing it).
- **Reassign, don't invent.** A character who must vanish from a scene gets replaced by an
  established cast member, not a new one. A broken count gets reconciled to the load-bearing
  version (the one stated most, or locked), not to a new number.
- Report-only first, then fix: audits never write; fixes are a separate pass with the audit
  report as input. Auditors must be told "do not invent problems — if the logic holds, say so."

## 5. Verification gates (every fix pass) {#SS-LOGIC-5}

- Re-read every changed passage with one neighbor each side — no seams.
- Grep the enabled corpus for each fixed defect (the old phrase = 0 hits) and for distinctive
  phrases of anything disabled (= 0 hits).
- Walk repaired arithmetic end-to-end and print the ledger.
- Beat text pushed through the CLI only (`--beat update`; preserves TextHash/Version):
  OS-level `<` redirection (PowerShell pipelines inject U+FEFF), then
  `UNICODE(SUBSTRING(Text,1,1)) != 65279` per touched beat; em dashes preserved byte-exact.
- NEVER `DELETE` from Nodes/Beats/NodeBeats — soft-disable via `NodeBeats.IsEnabled = 0`;
  snapshot SortKeys before re-seating.
- If docs changed: `powershell -File tools/codex.ps1 digest` then `doctor` — must PASS.

## 6. No votes {#SS-LOGIC-6}

Score panels, Legion votes, and census reviews are DISABLED BY DEFAULT engine-wide
(SS-A44). They run only on an explicit user request ("vote", "review", "score it") in that
conversation, via the explicit override. Rationale: panels cost real money per ballot and
localize nothing — every unanimous panel complaint in the origin campaign traced to a concrete
logic defect a careful read caught directly. The sweep is cheaper, names the failure, and the
fix is verifiable without a score.

## 7. Operational runbook {#SS-LOGIC-7}

The invocable form of this process lives at `.claude/skills/logic-sweep/` (usage:
`/logic-sweep <slug>` or no argument for changed-stories-since-last-sweep). It contains the
audit prompt template, the fixer prompt template, and the apply-craft rules above.

## 8. The three altitudes {#SS-LOGIC-8}

A story is examined at three magnifications, and every lens has a dedicated instrument:

| Altitude | What you see | Instrument / artifact |
|---|---|---|
| **10,000 ft — the story** | Premise, arc, locks, structure-as-designed | `Nodes.NodeBible` (hand-authored) + structural blueprint |
| **100 ft — the chapter** | What actually happens, in order | `NodeChapterSummaries` + `story-synopsis.txt` (publish artifact) |
| **10 ft — the beat** | The prose itself; who is in the room; what is true | Beat text + `BeatEntityPresence` + `BeatVerifications` |

**The agreement principle:** the three altitudes must tell the same story — they are one
specimen at different magnifications. Defects ARE altitude disagreements: a stale bible
describing a superseded draft (10,000↔10), two chapters telling incompatible events
(100↔100), a typo (pure 10 ft). Arbitration follows dimension 6: prose wins on FACTS,
the bible wins on LOCKS.

**Instruments per comparison:** 10,000↔100 ft = `prose --altitude-audit` (designed vs told;
findings filed as `OutlineDrift`); 100↔10 ft and 10↔10 ft = the logic sweep itself
(dimensions 1–5). Planning and review START at chapter altitude — read
`story-synopsis.txt` first, drop to beat altitude only where a finding points. The same
model applies to entities: book-level (which books), chapter-level (which chapters),
beat-level (which scenes, and how — acting / listening / mentioned / discussed) via
`vw_EntityBookAppearances` / `vw_EntityChapterAppearances` / `BeatEntityPresence`.
