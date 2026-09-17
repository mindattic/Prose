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

## 6a. GCTOC answer key — what Book the First actually owes (hand-read 2026-09-16)

Written by reading the whole fixture **before** looking at any extracted ledger, so the instrument is
graded against the book rather than against a rationalisation of its own output.

**Opened AND paid inside Book the First** — a competent ledger must open and close each of these:

| # | debt | origin quote | paid by |
|---|---|---|---|
| 1 | What "RECALLED TO LIFE" means | `Jerry, say that my answer was, RECALLED TO LIFE.` | `he has been--been found. He is alive.` (Ch4) |
| 2 | Who "Mam'selle" is | `Wait at Dover for Mam'selle.` | `Miss Manette had arrived from London` (Ch4) |
| 3 | Who the buried man of Lorry's dream is | `Buried how long?` / `Almost eighteen years.` | Dr Manette, 18 years in the North Tower (Ch4–6) |
| 4 | Whether Lucie's father is really dead | `my poor father, whom I never saw--so long dead` | `Your father has been taken to the house of an old servant in Paris` (Ch4) |
| 5 | Who the white-haired shoemaker is | `a white-haired man sat on a low bench, stooping forward and very busy, making shoes` | `Monsieur Manette, do you remember nothing of me?` (Ch6) |
| 6 | What the golden hairs in the folded rag are | `it contained a very little quantity of hair` | `She had laid her head upon my shoulder, that night when I was summoned out` (Ch6) |

**Legitimately CARRIED past the end of Book the First** — Dickens pays these in Books Two and Three.
A correct ledger must hold them OPEN, and must not be scored for doing so:

| # | debt | origin quote | paid in |
|---|---|---|---|
| 7 | Why Manette was imprisoned, and by whom | `worse than useless to seek to know whether he has been for years overlooked, or always designedly held prisoner` | Book the Third (the letter) |
| 8 | Jerry Cruncher's night trade | `Jerry, you honest tradesman, it wouldn't suit _your_ line of business!` | Book the Second (Resurrection-Man) |
| 9 | The wine-cask / BLOOD foreshadow | `scrawled upon a wall with his finger dipped in muddy wine-lees--BLOOD` | Book the Third |
| 10 | The Woodman and the Farmer | `to make a certain movable framework with a sack and a knife in it, terrible in history` | Book the Third (guillotine, tumbrils) |
| 11 | The lamplighter's ropes and pulleys | `hauling up men by those ropes and pulleys` | Book the Second (Foulon) |
| 12 | Madame Defarge's knitting | `took up her knitting with great apparent calmness and repose of spirit, and became absorbed in it` | Books Two/Three (the register) |
| 13 | "Jacques" as a shared name | `I choose them as real men, of my name--Jacques is my name` | Book the Second (the Jacquerie) |
| 14 | The unnamed red-haired woman | `a wild-looking woman … all of a red colour` | Book the Second (Miss Pross) |
| 15 | Manette's relapse risk | `he would be frightened--rave--tear himself to pieces--die` | Book the Second (after the wedding) |

**The consequence for the bar, and it is not about the instrument.** A *perfect* reader of this
fixture ends with **≈9 obligations outstanding** across 17,300 words. The calibration bar allows
**≤ 1.0 MODERATE+ control finding per 10k words ≈ 1.7 for the whole book.** So a flawless ledger on
A Tale of Two Cities, Book the First scores roughly **five times** the permitted control rate — by
being right. The control metric treats outstanding-at-end as a false positive, which holds for GCSH
(twelve self-contained stories that close what they open) and is simply false for the first act of a
three-book novel, where unpaid debts are the structure. **BCODA is a novel, not a story collection.**
Before any further tuning is bought, the bar needs a rule that distinguishes a debt the text
abandons from a debt the text has not reached yet — most likely by scoring control findings only
against a text that is structurally complete, or by scoring "outstanding at end" separately from
"contradicted / abandoned". Until then, every precision number in this log is measured against a
target that penalises correct behaviour on exactly the shape of book the engine exists to write.

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
- 2026-09-16 (run 6, $3.03) — **VOID: the extractor read 55 of 96 beats.** The printed score was
  TP 2, FN 2, resolved mis-flagged 1, control MODERATE+ 99 (9.47/10k), P 0.020, R 0.500, F1 0.038 —
  and it describes 57% of a book. The trial balance immediately after the run reported `ledger scan
  coverage : 55/96 beats`; the calibration output itself said nothing, because it never reported
  coverage at all. `ScanBeatAsync` stamps a beat only when the extractor reports `Evaluated` — by
  design, so an outage re-scans rather than banking a bad read — but every `Evaluated: false` path in
  `Parse` returned silently (empty response, no JSON object, malformed JSON), and `ExtractAsync`
  discards **all** windows of a beat when any one of them fails. An unread beat was therefore
  indistinguishable from a beat that owed nothing. Leading suspect: truncation at `maxTokens = 1400`,
  since a response cut off mid-object has an opening brace and no closing one and lands on the "no
  JSON object" path — which would be the worst available sampling bias, because the beats that blow
  the ceiling are the ones opening the MOST obligations, so the instrument would be dropping the
  densest beats in the book rather than a random 43%. **Do not attribute run 6's numbers to the
  locality fix, and do not compare them with run 5.** Not yet known: whether run 5 (and every earlier
  run) had the same partial coverage — nothing recorded it, so runs 1–6 all lack a coverage figure.
  Fixed here, without guessing at the cause: `Result.Failure` records why a read failed and
  `ExtractAsync` logs a warning naming the window, beat length and reason; `Score` carries
  `BeatsTotal`/`BeatsRead`, `CouldNotLook` is true when they differ, and `MeetsBar` is false whenever
  `CouldNotLook`, so a partial run is **void — not passing and not failing**; the CLI prints
  `beats READ by the extractor: 55/96` and `RESULT: VOID — COULD NOT LOOK`. `prose --obligations
  coverage --slug <slug>` reports read-only and free which beats were read and the length
  distribution of the read vs unread groups — the evidence that confirms or kills the truncation
  hypothesis from run 6's residue before run 7 spends anything.
- 2026-09-16 (GCTOC, ~$0.15 — **root cause of the unread beats, found by changing books**) — Run 6's
  coverage hole had an unknown cause and a wrong leading suspect (truncation at the token ceiling).
  Building the ledger on **GCTOC — A Tale of Two Cities, Book the First** (17 beats, 17,300 words,
  ~1,018 words/beat — structurally comparable to GCSH's ~1,100, but one continuous narrative rather
  than twelve self-contained mysteries) named it in one command: `scanned 17 of 17 … not evaluated 5`,
  against exactly **5 beats over the 6,000-char window limit**. Computing each split: 6,255 → 5,900 +
  **355**; 6,126 → 5,985 + **141**; 6,248 → 5,931 + **317**; 6,277 → 5,883 + **394**; 6,168 → 5,881 +
  **287**. A beat a few percent over the limit splits into a real window plus a *scrap*; handed a
  scrap with nothing in it the model answers in prose rather than JSON, `Parse` returns
  `Evaluated: false`, and one unreadable window voids the **whole** beat — discarding the ~5,900
  chars that read perfectly and leaving it unstamped. It is the opposite end of the beat from the
  suspected truncation. GCSH's beats measure 5,525–6,825 chars, so roughly half cross 6,000: that is
  exactly why run 6 read 55 of 96. The bias is systematic and was invisible — **the beats that vanish
  are the long ones, in every book.** Fixed: `SplitIntoWindows` folds a trailing window under
  `max/5` into its predecessor (an oversized window costs a few hundred tokens; a scrap costs the
  beat), `obl-extract-v6` so every over-length beat is re-read. **Lesson: a second fixture of a
  different shape found in one cheap command what six paid runs on one book could not.**
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
- 2026-09-16 (**the retrieval diagnostic, run at last — free, no LLM, and it answers the question the
  wrong way round**) — `--obligations candidates` had never been run. §8's own note said "the
  verse-index proposal is only worth building under the first diagnosis" — i.e. only if the judge was
  *shown* the payoff and still failed. It was shown nothing. Both GCSH resolved injections left
  outstanding come back **NEVER RETRIEVED**, for two different reasons, and neither is recognition:
  1. **Brass whistle** (`01a0ab6e3c1a7db7bebaa99ac13ca174`, Open, origin Ch1 pos 2). Payoff is Ch11
     pos 81. Lexical **rank #15 of 93** — outside `CandidatesPerObligation = 8`. Embedding: outside
     the top 400 of the corpus. **The reason is not granularity — the index is empty.** The first
     reading of this run blamed one-vector-per-1,100-word-beat being too coarse to locate a
     two-sentence payoff. That was wrong, and the diagnostic's own output disproves it: the deep
     sweep is `FindSimilarBeatNodesAsync(…, k = 400)` and it returned **12 hits** — the same 12 in
     both probes. The `gutenberg` universe holds 116 beats (GCSH 96 + GCTOC 17 + GCNEG 3), and only
     **12** carry a row in `ProseEmbeddings`: precisely GCSH's twelve original one-per-chapter beats,
     from *before* it was split to 96. **`split_beat` never backfilled the embedding index.** Every
     candidate that scored (`01a0514d7…`) is one of those twelve chapter blobs; every split beat
     shows `emb —`. That also explains the flat 0.300–0.355 spread: it was not ranking passages, it
     was returning all twelve chapters in near-arbitrary order. The embedding tier of the retrieval
     finder has been operating on a 12-row index of chapter-sized blobs for this entire programme,
     and the payoff beat has no vector at all — it cannot rank because it does not exist to the query.
     **Consequence: we still do not know whether beat-granularity embeddings are adequate, because
     they have never once been tested.** The fix is a backfill, not an architecture.
  2. **One-eyed cat** (`01a0ab7ba3ef7743b645f7c22197fbae`, Open) — worse, and a **cascade from the
     coverage bug that survives the coverage fix**. Its origin quote *is the payoff sentence*: the
     row is anchored to Ch9 pos 64, the payoff beat itself. The finder only ever looks *after* the
     origin, so this debt is **structurally unclosable forever**. Traced end to end: the setup beat
     is `01a0a698e81f7935b8a05270b53b9e0a`, Ch2 pos 12, **6,186 chars — over the 6,000 window, and on
     the unread list**. So: the setup was never read → the debt was never opened → the payoff beat
     was read with no open row to close → the extractor opened a *new* row on the payoff → the judge
     can never reach behind it. The scorer then reported "payoff not recognised", blaming recognition
     for a coverage failure three steps upstream.
  **Consequences.** (a) The failure is retrieval, not recognition — in neither case did the judge
  ever see the text. But the first-order fix is **backfilling the embedding index and making
  beat-creation maintain it**, not building a finer one; the verse-index proposal stays unbought
  until an actually-populated index has been measured and found wanting. Raising
  `CandidatesPerObligation` and protecting the lexical tier from being crowded out are cheap and
  independently justified — the brass-whistle payoff placed #15 of 93 lexically, so lexical alone
  would have found it at k=16. (b) A new defect class: **an obligation anchored to the wrong end of
  its own arc**, which no amount of judge improvement can fix. Inside calibration `ResetLedgerAsync`
  clears it; **on a real book there is no reset, so this corruption is permanent** — the same shape
  of risk already logged for a false close. (c) Every "payoff not recognised" number in runs 1–6 is
  now suspect as mis-attributed coverage damage. (d) **A silent-index class joins the silent-failure
  classes:** a retrieval tier that returns 12 rows for a 400-row request reported no error, and
  nothing in six paid runs noticed. `ProseEmbeddings` coverage belongs in `--obligations coverage`
  alongside beats read.
