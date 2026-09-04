---
codex: 1
project: Prose
layer: methodology
status: locked
updated: 2026-09-04
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
- after any outline edit that states story facts (sweep verifies prose agreement);
- on demand, any time a story "feels off" — the sweep names the failure or clears the story.

Scope the sweep to what changed plus its blast radius (the touched beats, their chapters, and
every beat that references their content). A full-story sweep is required when structure
changed (merges, disables, re-ordering) — those create orphans far from the edit site.

**Blast radius is now automatic, not just a scoping instruction (2026-08-14).**
`BlastRadiusService.GetBlastRadiusBeatIdsAsync` operationalizes the phrase above: same-chapter
beats within a small SortKey window, UNION every beat anywhere in the book sharing an entity
presence with the edited one. `NodeWorkbenchService.UpdateBeatTextAsync` — the one write path
under every beat edit, manual or automated — fires `LogicSweepService.RunNarrowAsync` against
that set after every save, filed under its own `beat:{id}:blast` scope so it never collides with
a full sweep's findings. This exists because a fix pass can contradict its own neighbors and the
regression survives undetected until the NEXT independent full sweep — sometimes several rounds
and days later. Checking the blast radius in the same turn as the edit closes that gap.

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
   same minute) and unexplained gaps. Respect DELIBERATE impossibilities marked in the outline
   (e.g., ATTE's 6:02→0540 texture) — the outline names them; don't "fix" them.
4. **Plant/payoff ledger.** Two-way table: every planted element → where it pays (or ORPHANED);
   every payoff → where it was planted (or UNPLANTED). Also ARITHMETIC: every count, sum,
   ledger, and roster stated in prose must reconcile — build the actual ledger and walk it.
   (Catches: a population count that never summed across six chapters; a closing line whose
   components added to 28 while it said thirty-one.)

   **Hard numeric facts (ages, tenures, career/duration lengths) have a dedicated ledger,
   separate from this per-sweep arithmetic check (2026-08-14).** This is the **Story Ledger** —
   canonical methodology: **[docs/LEDGER.md](LEDGER.md)**, the third peer of this doc and
   docs/READER-QA.md. Everything below describes only its FIRST detector; read LEDGER.md before
   relying on it for anything wider. `ContinuityService` stores each fact
   as an atomic (entity, predicate, object) claim and auto-flags a `CONTRADICTED` pair when the
   same predicate gets a genuinely different value later — but numeric-aware, not bare string
   equality: "fifty" and "50" collapse into the same claim; only an actual arithmetic
   discrepancy ("fifty" vs "fifty-nine") contradicts. This is what a repeated LLM re-derivation
   of the same fact across sweep rounds used to get wrong (VIGL: a character's career length
   drifted between "fifty" and "sixty" years across several rounds before the arithmetic was
   walked and settled) — checking against one stored ground truth instead of re-deriving it from
   prose every time is what makes the fact stop drifting. Every distinct real-world clock MUST
   use its own predicate key (`career_length_years` ≠ `zone_age_years`) — two different clocks
   must never be compared to each other even when the same entity/beat mentions both. Wired into
   the automated battery as the `fact-ledger` check (`BookHealthService`, DEEP tier), filing
   `FACT-LEDGER` findings; the extraction pass (`prose --continuity extract --node <slug>`) must
   be run at least once per book to populate it — an empty ledger reads as `[not-extracted]`, not
   silently clean.

   **This detector is a numeric drift detector, and this section used to imply it was general
   (corrected 2026-09-04).** It fires only on SAME predicate, different object. The defect that
   forced the Story Ledger programme was the other shape — *different predicate, incompatible
   meaning* ("Kyle → `father` → a swordsmith" against "Kyle → `origin` → constructed, no prior
   life") — which those two rows can never collide under, so it was undetectable here **by
   construction, not by bad luck**. The second detector, the exclusion ontology, is what makes
   that shape expressible: `prose --tuned-read` (docs/LEDGER.md §3.2), filing `TUNEDREAD `
   findings. **Its ceiling is beat-anchor coverage** — an unanchored claim cannot be adjudicated
   at all, so run `prose --continuity anchor-beats` (deterministic, free) and check the coverage
   line in `prose --continuity stats` before reading a clean tuned-read result as clean
   (docs/LEDGER.md §5).
5. **Orphan references.** Nothing enabled references removed/disabled/merged content — hunt
   with greps for distinctive phrases from every disabled beat. Highest-risk after structural
   edits. Also inverse-orphans: metadata stranded on disabled beats (chapter-start flags,
   titles) — `IsChapterStart` on a disabled beat silently deletes the chapter heading from
   every export.
6. **Outline agreement.** Prose and `docs/nodes/<CODE>.md` tell the same story. **Arbitration
   (author ruling 2026-08-29, replaces the old "the outline wins on canonical facts" fixed
   rule): no corner is automatically authoritative.** Outline ⇄ Book ⇄ Entities is a three-way
   symbiosis — each corner is verified by the other two, and every divergence is resolved
   case-by-case on evidence: state which side appears stale and why, citing both texts, then fix
   whichever the evidence actually points to (the outline, the prose, or both) in the same
   change and run `codex digest` + `doctor`. Never leave the disagreement standing, and never
   apply a blanket "X always wins" shortcut — Trinity reconciliation (`TrinityReconciliationService`)
   is the canonical arbiter for this, recording every ruling as a revertible
   `ReconciliationDecision` row rather than a silent auto-win. This dimension is
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
| **10,000 ft — the story** | Premise, arc, structure-as-designed | `Nodes.NodeOutline` (hand-authored) + structural blueprint |
| **100 ft — the chapter** | What actually happens, in order | `NodeChapterSummaries` + `story-synopsis.txt` (publish artifact) |
| **10 ft — the beat** | The prose itself; who is in the room; what is true | Beat text + `BeatEntityPresence` + `BeatVerifications` |

**The agreement principle:** the three altitudes must tell the same story — they are one
specimen at different magnifications. Defects ARE altitude disagreements: a stale outline
describing a superseded draft (10,000↔10), two chapters telling incompatible events
(100↔100), a typo (pure 10 ft). Arbitration follows dimension 6: no altitude is automatically
authoritative — the divergence is judged case-by-case on evidence (which side is stale, and
why), never by a blanket rule.

**Instruments per comparison:** 10,000↔100 ft = `prose --altitude-audit` (designed vs told;
findings filed as `OutlineDrift`); 100↔10 ft and 10↔10 ft = the logic sweep itself
(dimensions 1–5). Planning and review START at chapter altitude — read
`story-synopsis.txt` first, drop to beat altitude only where a finding points. The same
model applies to entities: book-level (which books), chapter-level (which chapters),
beat-level (which scenes, and how — acting / listening / mentioned / discussed) via
`vw_EntityBookAppearances` / `vw_EntityChapterAppearances` / `BeatEntityPresence`.

## 9. When is a book actually complete? {#SS-LOGIC-9}

Added 2026-08-14 in direct response to an observed failure mode: five independent sweep rounds
run on a book, and a sixth still found a new continuity error. "Run the sweep N times" was never
a real stopping criterion — a single-shot LLM sweep is a sample of what a fresh read notices, not
a proof nothing is wrong, and a fix pass introduces new regressions about as often as it removes
old ones. A book is publish-ready only when ALL FIVE of the following hold simultaneously —
this replaces the older, looser "logic sweep clean at BLOCKER" language everywhere it appears:

1. Zero open BLOCKER/MODERATE logic-sweep findings (§3–4, unchanged).
2. Zero open `CONTRADICTED` claims for the book in the **Story Ledger**
   ([docs/LEDGER.md](LEDGER.md); §3.4 above). Widened 2026-09-03 — the gate reads all three faces:
   the `CONTRADICTED` claim rows themselves (volatile predicates excluded, §3.4), the
   same-predicate `FACT-LEDGER ` findings, and the cross-predicate `TUNEDREAD ` findings. **A book
   whose ledger was never populated FAILS this condition rather than passing silently**: it has
   not been checked clean, it has not been checked. Before trusting a pass, confirm the
   instrument could have found something — see [docs/LEDGER.md §5](LEDGER.md#SS-LEDGER-5) on
   beat-anchor coverage.
3. **Two consecutive independent sweep rounds found zero NEW findings** — convergence, not a
   fixed round count. `prose --logic-sweep --slug <slug> --until-dry` runs one round of this
   campaign and reports whether to keep going or stop; state persists in
   `NodeConvergenceStates` across sessions via a book-content fingerprint, so a repeat call with
   nothing changed since the last dry round skips without spending another LLM call.
4. Every fix applied since the last dry round passed its own blast-radius mechanical re-check
   (§1) with no new contradiction.
5. Zero open High/BLOCKER Reader-Proxy QA findings (docs/READER-QA.md, unchanged).

**The safety valve.** If a book hits a round-count cap (default 8) without ever reaching 2
consecutive dry rounds, that is itself surfaced as a `LOGICSWEEP-CONVERGENCE [not-converging]`
finding — the section keeps surfacing new problems faster than fix passes resolve them, which
usually means it needs a structural rewrite, not another one-clause splice. This is the same
"if you can't name the failure, leave the beat alone" doctrine from §4, applied to the campaign
as a whole rather than one finding: escalate instead of looping forever.

## 10. Cold ledger, felt pass — two disciplines, never one instrument {#SS-LOGIC-10}

Author doctrine (2026-08-29). Real novelists split QA into two disciplines they deliberately
don't mix, and this engine must too: **a cold ledger for correctness, and a felt pass for
weight.** The failure mode is trying to get both from the same instrument — a spot-check dimension
that passes clean while the book reads dead, or a "feel" judgment asked to also certify a fact.

**The cold ledger (§§1–9 above): correctness is clerical, never a memory problem.**
Working novelists keep scene cards (POV, location, day/time, who's present, what changes, what
the reader learns) — this engine's Beat + `Beat.Description` **is** the scene card, embedded in
the outline itself (see the Bible→Outline refactor). Mystery/thriller writers keep day-by-day
calendars so alibis and travel times arithmetic out — dimension 3 (Timeline) is that calendar.
Series bibles are LOOKUP TABLES, not lore essays (eye colors, wound history, who-knows-what as of
which chapter) — the Story Ledger ([docs/LEDGER.md](LEDGER.md)) + the wound ledger are that table. At scale, authors stop
trusting themselves and hire a continuity editor (Sanderson) or lean on superfan encyclopedists
(Martin) — this engine's equivalent is the fact ledger + Trinity reconciliation + this whole
sweep. Every dimension in §3, the fact ledger in §3.4, and dimension 6's outline-agreement check
are cold-ledger instruments. They measure correctness. **They do not, and cannot, measure
whether the book is good to read.**

**The felt pass: weight is discovered serially, not measured per-beat.** Shape comes from the
outline layer first — Rowling's famous chapters-as-rows/subplots-as-columns spreadsheet, so a
quiet thread is visible at a glance (this engine's subplot-carrier/coordination check is that
grid — and, since 2026-08-30, positional: `subplot_gap_too_long` measures the longest run of
chapters/beats between two carrier appearances against a proportional threshold, not just whether
the thread was ever touched at all). Beat-sheet structures (Save the Cat, Swain scene/sequel) set
where the big swings should land. But no working author believes the outline *delivers* weight —
weight comes from three practices, in order:

1. **The full-order read at reader speed — the sacred one.** You cannot feel pacing while
   editing, because editing speed isn't reading speed. Authors print the manuscript, sit
   somewhere else, and read it straight through like a stranger, marking only where they got
   bored. **This is the one human-analog ritual this engine keeps sacred, and no per-beat
   instrument replaces it.** Every dimension in §3 can pass clean — every ledger balanced, every
   claim reconciled, every altitude agreeing — while the book still reads dead, because deadness
   is a property of the *sequence*, not of any single beat (the EVEN1 lesson: a clean-report book
   was later found to have silently dropped its own key beat and hid three more defects, caught
   only by a full linear read). Run it after the ledgers in §9 are clean, in full reading order,
   at speed, answering exactly one question: *where did I stop caring?* `prose --reader-qa
   --full-order-read` (docs/READER-QA.md §2, instrument 5) is an automated proxy for this
   ritual — cheap, unattended, and useful, but it is not the ritual itself. An LLM doesn't get
   bored the way a person does; it can be prompted to notice textual flatness, not to replace
   an author's own read.
2. **Distance.** Drawer time — weeks between draft and reread, so the cold read is actually cold
   (King's "six weeks in a drawer"). A same-session reread of your own fix pass is not this.
3. **Weight-by-length, not weight-by-adjective.** When a beat lands flat, the fix is almost never
   a better verb — it's structural: give the moment more page-time to accrue pressure before it,
   or cut the correct-but-inert scene sitting in front of it. Weight is mostly *time under
   tension*: a payoff feels as heavy as the number of pages the promise spent open, which is why
   plant/payoff **distance** matters as much as plant/payoff existence (§3.4 tracks the pairing;
   it does not track the gap between them — that gap is a felt-pass judgment, not a ledger fact).

Then authors outsource the measurement: beta readers and editors don't fix anything, they mark
where attention died, and the author treats those marks as ground truth because the author can no
longer feel their own book. Reader-Proxy QA (docs/READER-QA.md) is this engine's beta-reader
layer — findings-only, no scores, same as the sweep.

**How this shapes engine behavior:** never let a cold-ledger instrument (a sweep dimension, the
fact ledger, outline-agreement) stand in for a felt-pass verdict, and never ask the full-order
read to also certify a fact — that's what §§1–9 are for. A flat-beat finding from the full-order
read gets fixed by reallocating page-time around it, not by rewriting its adjectives — every
finding the full-order read files now carries that instruction as its own `suggestedFix`, so the
steer reaches whoever applies the fix without having to re-derive it from this doctrine. Beta
readers (a Legion-style panel) are a possible future addition to the felt-pass layer, not a
replacement for the full-order read itself.

**A correction, found live 2026-08-30:** an earlier draft of this session's work also tried to
give the full-order read a "drawer time" nudge — warn (never block) when the beats in scope were
edited too recently. That was wrong and was reverted before shipping. Drawer time is about a
*human author's own memory* of writing fading enough that a reread becomes genuinely cold — but
the full-order read's "reader" is a jury of stateless LLM calls (`ReviewLlmTransport.
AssignJuryAsync`) that never had any exposure to the beat at write time in the first place. There
is no memory to fade, so wall-clock time since `Beat.UpdatedAt` measures nothing the instrument
actually needs — every run is already maximally cold by construction. Do not reintroduce a
timing-based warning here; if a future distance-style safeguard is wanted, it would have to target
something that actually accumulates state between the write and the read (there is none today),
not elapsed clock time.
