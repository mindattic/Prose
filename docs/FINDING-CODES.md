---
codex: 1
project: Prose
layer: methodology
status: active
updated: 2026-10-08
---

# FINDING CODES & SUPPRESSION {#SS-FINDCODE}

## 1. Why this exists

Every automated checker in this engine (LogicSweepService, BeatChecklistGateService,
ContinuousQualityService's Reader-Proxy comprehension pass, the EntityDrift/ProseHealth checks)
files rows into one shared `Findings` table, but has no way to be told "you already flagged this
exact pattern here once, it was reviewed, it's intentional — stop." RFC 0011 names the resulting
failure mode directly: `BeatChecklistGateService` once filed 445 findings on a single book, almost
all "Cognitive-architecture tics," because the check had no visibility into that book's POV
character's own hand-authored voice and flagged it as a generic AI tic. The 2026-10-08 BLST
book-report session hit the same shape again by hand: a ledger-keeper character's established
"checks, logs, decides" vocabulary — the book's own central metaphor, literalized — kept tripping
the deciding-tic/cognitive-architecture-tic/observation-tic checks.

A **finding code** is the stable name a `FindingSuppressionRow` points at. A **suppression** is an
author-declared row saying "a finding matching this code, at this book (or this one beat), files
pre-dismissed from now on." See `src/Prose.Core/Services/FindingSuppressionService.cs` and
`FindingCodeRegistry.cs` for the implementation; `prose --findings codes` always prints the live
table below.

## 2. Where suppressions live — and where they deliberately do NOT

**Never inline prose or `<entity>` tags.** Two concrete reasons, both confirmed in the engine
before this was built (not a guess):

1. `<entity>` tags are stripped and fully **regenerated from a fresh name-scan on every beat
   save** (`NodeWorkbenchService.UpdateBeatTextAsync` / `BeatMarkup`). Any extra attribute added to
   one (e.g. a hypothetical `suppress="..."`) would silently vanish the next time that beat is
   edited — the exception disappears without warning, and the finding comes back looking like a
   regression.
2. No tag type besides `<entity>` is recognized or stripped by the export pipeline. A new tag type
   invented for this would render as literal visible angle-bracket text in the published book.

Suppressions instead live in `FindingSuppressions`, a small side-table — the same category of
thing as `BeatVerifications` or `NodeConvergenceStates`: a deterministic exception list, not a
judge, a vote, or a new story instrument. It is managed entirely through
`prose --findings suppress/unsuppress/list-suppressions` — never by hand-editing a beat.

## 3. Using it

```
prose --findings codes
prose --findings suppress --node <slug-or-code> --code <code> [--beat <guid>] [--reason "..."]
prose --findings unsuppress <suppression-id>
prose --findings list-suppressions [--node <slug-or-code>]
```

Omit `--beat` for a book-wide exception (every beat in the book); pass it to scope the exception to
one beat only. A suppressed finding is still filed — never silently dropped — just pre-set to
`Dismissed` with `SuppressedBy` recording which code/scope matched, so the audit trail (what fired,
and why it was ruled fine) survives exactly the way a hand-dismissed finding's history does.

## 4. The code registry

Every code reuses a name that already existed before this registry did — not an invented parallel
taxonomy. `CRAFT-8.1`..`CRAFT-8.9` are `docs/CRAFT.md` §8's own numbered Banned Mannerisms list;
`DELIGHT-14` is the move-monotony check's own §14 citation; everything else is a bare
`FindingCategory` enum name, optionally narrowed with a sub-kind that already appears verbatim in
that category's Summary text.

| Code | Category | Matches | What it is |
|---|---|---|---|
| *(bare category, e.g. `CraftChecklist`)* | any | everything in that category | Coarse "suppress this whole category here" escape hatch. |
| `CRAFT-8.1` | CraftChecklist | "Associative chains" | "it was X, and X was Y, and Y was…" |
| `CRAFT-8.2` | CraftChecklist | "Cognitive-architecture tics" | filing/ledger/parliament/geometry framing of thought — the exact RFC 0011 row-3 false-positive class. |
| `CRAFT-8.3` | CraftChecklist | "observation tic" | "noted/logged/catalogued" as a thought-verb. |
| `CRAFT-8.4` | CraftChecklist | "Mood-soup" | atmosphere/interiority crowding out plot. |
| `CRAFT-8.5` | CraftChecklist | "Purple prose at the peak" | stacked similes where the feeling should land plainest. |
| `CRAFT-8.6` | CraftChecklist | "Italic-thought crutch" | italicized inner-monologue as a recurring beat. |
| `CRAFT-8.7` | CraftChecklist | "Over-explanation" | restating what the scene already showed. |
| `CRAFT-8.8` | CraftChecklist | "Jargon front-loading" | info-dumps (CRAFT.md §4). |
| `CRAFT-8.9` | CraftChecklist | "deciding tic" | personified/displaced agency or pre-conscious framing around "decided." |
| `DELIGHT-14` | CraftChecklist | "Move monotony" | one narrative move carries a disproportionate share of move-landing beats — a structural/felt-pass signal, not a per-sentence tic; suppress only when the repetition is the book's own genre-appropriate engine (see docs/LOGIC.md §10), not a stamp. |
| `LOGIC-TIMELINE` | Contradiction | "timeline@" | a stated time/date/span conflicts with another. |
| `LOGIC-DRIFT` | Contradiction | "inserted_beat_drift@" | a beat inserted after the fact reads out of order with its neighbors. |
| `READ-CONFUSE` | ComprehensionDefect | "[confusion]" | a cold reader can't resolve what a passage means — check first whether it's a deliberate, paid-off plant before suppressing. |
| `READ-MISSFACT` | ComprehensionDefect | "[missed-fact]" | a reader's own recap drops a fact the prose states. |
| `HEALTH-READABILITY` | ProseHealth | "READABILITY" | Flesch score below the clarity floor. |
| `HEALTH-SANITY` | ProseHealth | "SANITY" | a sanity-scan heuristic fired. |
| `ENTITY-DRIFT` | EntityDrift | *(none — category-wide)* | the Bible/outline names a character with no matching live entity — usually a retired-outline artifact (see docs/LOGIC.md §SS-LOGIC-3 dimension 6), not a reader-facing problem. |

**Before suppressing a `ComprehensionDefect` or `DELIGHT-14` finding**, re-read §4 and §10 of
`docs/LOGIC.md` — these two categories are the ones most likely to be a genuine felt-pass defect
rather than checker noise, and the BLST session found real fixes in exactly this category (a
dangling pronoun antecedent) alongside several confirmed false positives. The CRAFT-8.* and
ENTITY-DRIFT codes are the ones with a documented, repeatable false-positive pattern behind them.

## 5. `/book-report`

`prose --book-report --slug <slug-or-code> [--complete]` renders a deterministic summary of a
book's findings backlog, logic-sweep convergence, and any logged decisions that mention it, to
`Downloads\GLMZ_Book_Reports\drafts\{CODE}[_complete].md` — the same drafts/ + `make_pdfs.py` ->
`.pdf` pipeline already in use there. See `.prose/commands/book-report.md` for the full runbook
(read-by-hand, verify, fix, suppress, converge, report — in that order; this command only does the
last, deterministic step).
