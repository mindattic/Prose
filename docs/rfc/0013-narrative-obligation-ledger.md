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
  instrument that closes them, and it never ran; that number is unmeasured, not failed. Measured
  beat lengths (raw chars) attribute the misses precisely: the lantern boy (6,597) and stopped
  clock (6,648) setups and the brass-whistle (6,603) and tan-coat (6,825) payoffs sat past the cut;
  so did the one-eyed-cat setup (6,347), which the score counted "ok — never opened" and was in
  fact a hidden miss. Two failures were NOT truncation: the violet-seal setup (5,525 chars, fully
  visible; that beat logged 2 ungrounded quotes — quote fidelity, e.g. "recognised" respelled) and
  the tin-soldier payoff (5,815, visible, not recognised as closing — suspect `MaxOpenListed = 40`
  saturating the listed open set on a 116-open book, or an "advanced" verdict). Run 2 decides
  whether those two need extractor work; the windowing alone cannot fix them. Cost priors
  added for `--calibrate-obligations` ($2.50) and `--reconcile-obligations-deep` ($1.50).
- 2026-09-15 (run 2, after redeploy, $2.61) — **still BELOW BAR, but the shape moved:** recall 0.75
  (3/4 abandoned setups opened; the violet seal is still the miss), yet **all four resolved payoffs
  "not recognised" with the judge running**, and control MODERATE+ rose to 205 (455 opened, 198
  closed, 257 outstanding). Two more tail-cuts, same class as run 1: (3) the judge clamped every
  candidate passage to 600 words; the payoff beats are ~1,100 words with the payoff last. v2 lists
  a long candidate as consecutive numbered parts, gates quotes against the whole beat, and merges
  one beat's parts into one cache row (closes > advances > not_addressed); `obl-judge-v2` voids the
  cut verdicts. (4) At scan time the extractor is shown the 40 most URGENT open rows; on a
  257-outstanding book no book-end plant ever made the list, so the payoff beat could not "touch"
  the debt it pays. `SelectForListing` now keeps 25 urgent slots and fills 15 with rows whose
  content words occur in the beat (falling back to urgency); `obl-extract-v3` restamps. Tests pin
  both. The 205 controls remain unmeasured until the judge sees whole beats. Run 3 (~$2.6) needs
  another redeploy.
- 2026-09-16 (runs 3 and 4 — **void; they measured nothing**) — The harness never reset ledger
  state between runs, so every run inherited the verdicts of the one before it. Run 3's judge
  CLOSED the injected stopped-clock plant on "…smoked a cigar and waited behind a tree…" — a real
  sentence from the book, and entirely unrelated to the debt. Run 4 then scored that same row as a
  miss, on run 3's close event, with no run-4 event on the row at all. **A false close is
  terminal:** `ObligationResurfacingJudge` only ever revisits `Open|Advanced` rows, so nothing could
  correct it. Do not reason from any run 1–4 number; cumulative spend to that point was $14.38 for
  no measurement. Two fixes: `ObligationCalibrationService.ResetLedgerAsync` drops machine rows,
  their journal and the judge cache and clears `ObligationScanHash` before every score — preserving
  authored and author-locked rows, and refusing any book outside the `gutenberg` universe — and the
  judge gained a relevance veto. **Grounding is not relevance:** `QuoteGrounding.Contains` proves a
  quote is real text in the candidate, never that it answers the debt. `SharesContent` now vetoes a
  closes/advances verdict whose quote shares no content word with the obligation; `obl-judge-v3`
  restamps so ungated v2 verdicts are re-judged rather than replayed. The underlying product risk is
  noted, not fixed: outside calibration, one bad auto-close still drops a real obligation forever.
- 2026-09-16 (run 5, $3.10) — **the first valid measurement. BELOW BAR:** TP 4, FN 0, resolved
  mis-flagged 2, control MODERATE+ 103 (9.85/10k), P 0.037, R 1.000, F1 0.071. Ledger 229 rows (not
  the old cumulative 563): 134 outstanding, 95 closed. The stopped clock came back `Advanced
  (flagged)` and the violet seal `Open (flagged)` — the first time in five runs — but the extractor
  prompt was byte-identical to run 4's, so the credit belongs to the reset, not to any prompt fix.
  **Diagnosis corrected: over-extraction is not the dominant term.** Close rate by chapter is ch1
  **82%** against 15–59% for every later chapter, across twelve structurally identical self-contained
  Holmes stories. Doyle did not get worse at resolving his own plots; the only variable is how many
  older debts compete for the same 40 slots, and outstanding-left-behind climbs with it (ch1 leaves
  3, ch7 15, ch12 18). Reading the starved rows confirms they are real debts the story does pay —
  the masked visitor who unmasks as the King of Bohemia, Spaulding's cellar (he is digging the
  tunnel), the typewriter wear that convicts Hosmer Angel, the locked attic room. The extractor was
  right; the model was never shown the row it was paying. `SelectForListing` now fills three tiers —
  LOCAL (≤15 debts opened in this chapter, newest origin first), then lexically relevant rows from
  anywhere, then urgency for the remainder — on the principle that proximity, not urgency, predicts
  which debt a beat pays. `obl-extract-v5` restamps. Left untouched so run 6 stays attributable: the
  judge's candidate finder, `MaxOpenListed` itself, and the off-page back-reference false-plant class
  ("the Tankerville Club scandal", "the Irene Adler photograph") — prose gesturing at stories it will
  never tell, which can never be paid and should never be opened.
- 2026-09-16 (harness defects fixed between runs 6 and 7, no LLM cost) — Two instrument problems
  that no amount of spending would have surfaced. (1) **The scorer could not fail.** Of the eight
  injections only the four ABANDONED ones were ever tallied TP/FN; of the four RESOLVED ones, only a
  row left outstanding scored (as a mis-flag), while both "never opened" and "opened and Closed"
  printed `ok` and fell through as neither. So recall was a statistic over half the manifest, and a
  planted debt the extractor missed entirely — the worst outcome available — counted as a PASS. An
  extractor that missed every resolved injection outright would have reported exactly run 5's
  "recall 1.000". `Classify` now scores all three resolved outcomes (never opened or left in a
  non-paying terminal state → FN, opened-and-Closed → TP, still outstanding → mis-flag as before),
  and is a pure static tested without the LLM pipeline — the reason the gap survived five paid runs
  is that nothing could exercise the scorer without spending $3. `Score.ScorerVersion`
  (`obl-score-v2`) is stamped on every result and printed: **run 6 and everything before it are v1,
  and their recall is not comparable to run 7's.** (2) `prose --obligations candidates --id <guid>
  [--payoff-beat <guid> …]` is a read-only retrieval diagnostic — no writes, no LLM call. "Payoff
  not recognised" hides two opposite defects: if the judge was shown the payoff text and still said
  not_addressed, the fix is recognition or granularity (a ~1,100-word beat carries one embedding, so
  a two-sentence payoff inside it is a rounding error against the whole beat's meaning); if the
  finder never surfaced the beat, the judge never had the chance, and the fix is retrieval budget.
  The verse-index proposal is only worth building under the first diagnosis. The command prints the
  candidate set, the `ObligationJudgeCache` rows behind it, and per payoff its lexical score and rank
  over every later beat (flagging when it sits below the finder's `score >= 2` floor) plus its rank
  in a deep top-400 embedding sweep, so a payoff outside the k=8 budget reports how far outside
  rather than merely absent.
