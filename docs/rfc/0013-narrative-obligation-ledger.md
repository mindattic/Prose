# RFC 0013 — The Narrative Obligation Ledger

**Status:** approved by the author 2026-09-15; Phases 0–2 implemented the same day (mojibake
convergence + ingress guards; ledger schema, write-door extraction, Brief block, trial balance,
author desk; reconciliation instrument, resurfacing judge, record grounding, calibration harness,
health snapshots, publish gate 6, `--auto-run` hard gate). Awaits the author's Hub redeploy; the
two migrations apply on its next start. Phase 3 (BCODA retrofit) runs after calibration on the
gutenberg fixtures meets the bar in §6. · **Companions:** RFC 0009 (no autonomous prose writes),
RFC 0012 (single-source writer), docs/LOGIC.md §3.4 and §9, docs/LEDGER.md, docs/READER-QA.md §6.

---

## 0. The law

> A narrative promise is logged as an **obligation** at the moment it is made, with a verbatim
> quote and the beat it came from. An obligation is closed only by a quote from the beat that pays
> it. An obligation that is open past its due point without an author decision is a finding. A
> payoff with no origin is a finding. An empty ledger fails, it does not pass. Nothing in this
> system writes prose.

## 1. Why

Reading the first five pages of *Bushido Coda* found three defects no instrument had ever flagged:

1. A girl watching from behind a curtain in Ch1 — half a page of weighted setup — never mentioned
   again in ~475 beats (verified by full-text search of the whole book).
2. A second unnamed child hidden in loading-dock ductwork through a six-on-one fight, with no
   on-page reason for a child to be at a professional ambush.
3. Mrs. Chen's character record carrying a fabricated backstory ("mercy job… sickbed") that exists
   in no beat.

The book's 140k-character node bible is a real, maintained plan and mentions none of them: they
were invented at prose-generation time and never fed back. The root cause, located in code:

- the writer sees only the **last 6,000 characters of the current chapter**
  (`BeatGeneratorService.cs`, `context.SceneSoFar[^6000..]`); earlier chapters arrive as one-line
  summaries;
- the old `NodeOpenThreads` extraction ran only after *Generation* — BCODA was hand-spliced, so its
  promises were never recorded; the rows it did make had no quote, no due point, no entity, no
  read path;
- `PlantPayoffs` for BCODA was **empty**: the bible's §14 ledger existed only as markdown;
- `EntityMentionScanner` tags names only; a lowercase referent ("the girl") has no record and no
  stub; `FactInterpreterService.CreateStubAsync` was never wired to a beat write;
- docs/READER-QA.md §6 classified an unresolved mystery as a *non-finding*.

The author's question was the right one: no model (and no editor) holds a 500-page novel in
working memory. *Lost in the Middle* (TACL 2024), *NoCha* (EMNLP 2024: GPT-4o 55.8% on whole-novel
claims), and *TLDM* (2025: storyworld tracking collapses past ~64k tokens) show recall does not
scale. *Re3*/*DOC* (+14% / +22.5% coherence from plan-controlled generation), *CFPG* (2026:
Foreshadow–Trigger–Payoff pools; payoff realization 0.569 → 0.911 with oracle timing) and *Lost in
Stories* (2026: a quote-grounded LLM checker validated on 1,000 injected errors reaches F1 0.678
against professional annotators' 0.281) show what does: **record the promise when it is made,
verify closure against the text, calibrate the instrument on injected defects.** Every mature
fact-keeping discipline — double-entry bookkeeping, DO-178C bidirectional traceability, legal
fact chronologies, script-supervisor continuity logs, copyeditor style sheets — converged on the
same shape: an obligation opened at introduction, carrying provenance, explicitly closed, listable
while open, verified against the artifact.

## 2. The ledger (Phase 1, shipped)

`NarrativeObligations` — book-scoped rows: `Kind` (promise | plant | question | wound | foreshadow |
introduced-referent | unexplained-presence), `Description`, `Provenance` (`ClaimProvenance`:
authored | observed | inferred), `OriginBeatId` + `OriginQuote` + `OriginTextHash`, optional
`EntityId` (a canon entity or a `Status="stub"` row named `(unnamed) girl behind the curtain` — the
`(` keeps `EntityMentionScanner` off it), CFPG `TriggerCondition`, `DueByKind`/`DueByValue`
(chapter | beats | book-end), `State` (Open → Advanced → Closed | Dropped | Deferred, plus
system-only Withdrawn), closing beat + quote + hash, `AuthorNote`, `DroppedReason`, `AuthorLocked`,
`DedupKey`. `NarrativeObligationEvents` is the journal (open / advance / close / reopen / drop /
defer / withdraw / reanchor / due-changed / lock, with actor). `PlantPayoffs.ObligationId` bridges
hand-curated pairs; `Beats.ObligationScanHash` gates the scan. Migration
`20260915185326_AddNarrativeObligations` moved every `NodeOpenThreads` row across as `inferred`,
mirrored every plant pair as an `authored`, locked `plant` row, then dropped the old table.

**The door.** `NarrativeObligationService.ScanBeatAsync` is hooked in
`NodeWorkbenchService.UpdateBeatTextAsync` (and the batch path) for **every** `BeatWriteReason`
except TagMaintenance/Plan, fire-and-forget, hash-gated on whitespace-collapsed text. It first
re-verifies the quotes this beat anchors — restamp, re-anchor to a sibling beat, reopen a closure
whose quote vanished, withdraw an open unlocked row nothing ever advanced — then one Haiku call
(`NarrativeObligationExtractor`, reason-before-verdict JSON) returns what the beat opened and which
open rows it advanced or closed. **Every quote is checked in code** (`Audit.QuoteGrounding`, shared
with the logic sweep); an item that cannot quote the text is discarded and counted. A provider
outage leaves the gate unstamped so the beat is re-scanned next time. Locked rows are immutable to
every automated path.

**The Brief.** `BuildBriefBlockAsync` replaces the OPEN THREADS block and the separate PLANTED
DETAILS block: `[OPEN OBLIGATIONS]` with DUE NOW / NOT YET, urgency-ordered, ≤2,500 chars.

**The desk.** `prose --obligations list|trial-balance|history|open|close|drop|defer|reopen|due|
accept|rescan` and MCP `list_obligations`, `get_obligation`, `obligation_trial_balance`,
`open_obligation`, `close_obligation` (refused with `quote_not_found` unless the quote is in that
beat), `drop_obligation` (reason + note required), `defer_obligation`, `reopen_obligation`,
`set_obligation_due`, `accept_obligation`, `link_obligation_entity`. Every author write locks the
row and journals the actor. "I'll pay it in Ch12" is a Defer to Ch12 — no state, no code path,
could write the payoff.

**The bible's own ledger.** `prose --obligations import-bible-ledger --slug S [--dry-run]`
(`BibleLedgerImporter`) reads the node bible's §14a "Closed plants" and §14b "Dropped findings"
tables from `NodeOutlineSections` (or the legacy `Nodes.NodeOutline`) and lands them as authored,
locked rows: §14a → `plant`, Closed, mirrored into `PlantPayoffs`; §14b → Dropped with
`DroppedReason = author-note` and the bible's reason verbatim. Anchors resolve only when a
`Ch<n> SK:<k>` label matches a beat's SortKey exactly in the current reading order; anything else
is imported with a null anchor and printed first as NEEDS ANCHOR — a lossy label is never
guessed. Rows carry no page quote (the bible is the author's word), so the trial balance treats
them as neither stale nor dangling. Idempotent via `DedupKey`; no §14 table at all is COULD NOT
LOOK, exit 1.

## 3. The trial balance and the hard gate (author decision: hard)

`TrialBalanceAsync(book, chapter)`: opened − closed − dropped − deferred = carried forward, plus
**overdue without decision** — outstanding rows past due at the end of the chapter, not locked into
an author decision. Wired in: `prose --auto-run --obligation-gate hard|soft|off` (default hard:
scans the chapter's beats synchronously, strikes the balance, stops the run with exit 3 when a
chapter owes an undecided debt); `publish_readiness` check 6 (LOGIC.md §9 item 6 — scan coverage
100%, zero overdue undecided, zero open book-end rows at publish; an empty ledger FAILS).

## 4. Instruments (Phase 2, shipped)

| Command | Cost | What |
|---|---|---|
| `prose --reconcile-obligations --slug S [--json]` / `--all [--status …]` | free | six deterministic rules via `AuditRunner` under `FindingCategory.NarrativeObligation`, scope `node:{slug}#obligations`: `overdue_open` (BLOCKER for an early unnamed referent/question never advanced), `open_at_end`, `stale_closure`, `dangling_beat`, `unplanted_payoff` (LOGIC §3.4 reverse direction), `deferred_expired`. Writes a `NarrativeHealthSnapshots` row when it examined anything. Prints `examined N over M beats`; COULD NOT LOOK on an empty ledger. |
| `… --deep` | cents (own cost name `--reconcile-obligations-deep`) | `ObligationResurfacingJudge`: per open row, candidates from embeddings (`FindSimilarBeatNodesAsync`) ∪ lexical overlap, one Haiku call, verdicts `closes/advances/not_addressed` with verbatim quote, ungrounded discarded, cached in `ObligationJudgeCache` by candidate text hash. |
| `prose --scan-unnamed-referents --slug S` | free | `UnnamedReferentScanner`: definite NPs with a person/role head and no tag; flags ≥2 mentions in one beat, acting, never seen again. Hint-only. |
| `prose --ground-entity-records --slug S [--entity N]` | cents | `EntityRecordGroundingService`: decompose each tagged entity's record into atomic claims (FActScore); entail against tagged + embedding-retrieved beats with a quote gate; unentailed/contradicted filed under `EntityDrift` (`node:{slug}#recordground`); matching non-authored `ContinuityClaims` downgraded to `inferred`. **Never edits the record.** |
| `prose --inject-calibration-defects / --calibrate-obligations / --revert-calibration-defects` | cents | §6. `gutenberg` universe only; throws before any write elsewhere. |
| `prose --narrative-health --slug S [--history]` | free | the snapshots. |

## 5. Deliberate vs abandoned

Decided by a ledger row, never by prose language: a `Deferred` row, or an `authored` row due at
book-end, is not flagged before the end. READER-QA §6's demotion stays; what it demotes should now
become a `question` row so the trial balance can age it instead of dropping it.

## 6. Calibration bar (Phase 2 → 3 gate)

On GCNEG / GCSH / GCTOC with N = 8–20 injected defects (half abandoned, half resolved): precision
≥ 0.85, recall ≥ 0.70, F1 ≥ 0.678 (the *Lost in Stories* bar), ≤ 1.0 MODERATE+ control finding per
10k words on the un-injected text, ≤ 10% of resolved injections mis-flagged. Any extractor or
judge prompt change re-runs the harness before it ships (RFC 0010: re-admission by evidence).

## 7. Out of scope, by law

Automatic payoff generation; rewriting accepted prose; a "fix all orphans" command; LLM ranking of
obligations by importance; per-universe rulebooks. Findings describe, never instruct.

## 8. Status log

- 2026-09-15 — Phase 0 `93c7cdb74`; Phase 1 `fae87dc55`; Phase 2 this commit. Hub redeploy and
  calibration run are the author's next steps; then the BCODA retrofit runbook (plan file
  `mellow-sauteeing-squirrel.md`, "BCODA retrofit runbook").
- 2026-09-15 (later) — Hub redeployed; BCODA bible mojibake CLEAN in one pass. The gutenberg fixtures
  were one beat per chapter (12/6/3) and the harness needs ≥20, so GCSH was split to 96 beats and
  GCTOC to 17 at sentence boundaries (deterministic `split_beat`, verbatim); both marked
  "Complete - publication ready" so book-end rules age. **Calibration run 1 on GCSH: BELOW BAR.**
  `--deep` died after $2.02 (est $0.05) on `ObligationJudgeCache.Quote` nvarchar(400) overflow; the
  free-rules score was TP 1 / FN 3 / resolved mis-flagged 2 / 116 control MODERATE+ (11.1 per 10k
  words), P 0.008 R 0.25. Two root causes found by reading, both fixed in this commit: (1) the
  extractor cut every beat at 6,000 chars before the prompt AND the quote gate, so the tail of any
  longer beat — where injection appends — could never be opened or recognised as a payoff; v2 scans
  long beats in sentence-boundary windows gated against the whole beat, and `PromptVersion` is now
  folded into `ObligationScanHash` so the rescan is not skipped; (2) every persisted quote is now
  `QuoteGrounding.ClampForStorage` (≤400, word boundary; a prefix of a substring still grounds). The
  116 control findings are open obligations the free rules cannot close — the deep judge is the
  instrument that closes them, and it never ran; that number is unmeasured, not failed. Cost priors
  added for `--calibrate-obligations` ($2.50) and `--reconcile-obligations-deep` ($1.50). Next: author
  redeploys the Hub → `--revert-calibration-defects` → `--inject` → `--calibrate-obligations --deep`.
