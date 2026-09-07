# RFC 0012 — The Single-Source Writer

**Status:** step one executed (instrumentation, 2026-09-07); no service cut, folded, or reordered ·
**Requested by author, 2026-09-06/07:** *"writing needs to be from a single source, not 20+ LLM
calls doing whatever they want"* · *"a single source writer that takes in information from other
services but is the sole funnel through which all changes to beats happens"* · **Companion to:**
RFC 0009 (no autonomous prose writes), RFC 0010 (battery value audit)

---

## 0. The end state

One writer. It **consumes** what the enrichment services produce (context, constraints, ledgers)
and is the **only** code path that changes a beat. Coherence machinery stays — the Story Ledger,
the logic sweep, DCM, `BeatWriteReason`. Opinion machinery goes. Which of the services that fan out
from today's `ProseWriterRouter.WriteAsync` earn their LLM call is decided **by measurement, not by
argument** — the same rule RFC 0010 applied to the battery.

## 1. Step one — make one beat write observable (done)

### 1.1 What was wrong with the picture before

RFC 0010 §6a recorded: *"93 `await …Async(` sites per beat write, at least nine enrichment
services make their own LLM call, and the coverage log instruments five of ~35 services."* On
reading the code, two of those three were off:

- The live write path already logged **30** `BeatServiceLog` rows per beat, not five. The "five"
  is the `--backfill-coverage` path (`LogCoverageAsync`), a different method. The MCP tool
  `workflow_status`'s description also still said five; fixed alongside this RFC.
- Coverage was never the gap. `BeatServiceLog` answers "did this stage's prompt block come out
  non-empty". It cannot answer how many LLM calls a stage made, how long it took, or what it cost.
- The real gap: `LlmActionContext.CurrentBeatId` was set only around the draft call, so every
  pre-generation enrichment call and every post-write extraction/check call was recorded in
  `LlmCallHistory` / `LlmPromptCapture` with **no beat**. No call carried a stage tag at all.
  `EmbeddingService` (direct OpenAI HTTP) was recorded **nowhere** — not in `TokenLedger`, not in
  `LlmCallHistory` — so every per-beat and per-command cost figure omitted embedding spend.

### 1.2 What was added (`32e2491d1`, `--beat-id` follow-up)

| Piece | Where | What it does |
|---|---|---|
| `LlmActionContext.BeginStage` | `Prose.Core/Services/LlmActionContext.cs` | Ambient stage tag; nests as `outer/inner`; restores on dispose |
| Stage + beat on every chat call | `LlmRouter.RecordCallHistoryAsync` / `RecordPromptCaptureAsync` | New `LlmCallHistory.Stage`, `BeatId` (indexed), `ElapsedMs`; `LlmPromptCapture.Stage` |
| Embedding calls recorded | `EmbeddingService.EmbedAsync` / `EmbedBatchAsync` (+ local variants) | One `LlmCallHistory` row per HTTP round-trip (`openai-embed` / `local-embed`), real `usage.prompt_tokens` when present, plus `TokenLedger.RecordActual`; `text-embedding-3-*` pricing rows |
| Whole-write beat scope | `ProseWriterRouter.WriteAsync` → `WriteCoreAsync` | Beat id brackets the entire write, including the fire-and-forget post-write `Task.Run` (flows via captured ExecutionContext) |
| Per-stage execution log | `TraceStage` / `TraceStageAsync` → `BeatWriteStageLog` | Every traced stage records (name, phase, ms, ok); persisted at the end of the post-write cluster with a terminal `(complete)` row so "finished" is distinguishable from "crashed / still running" |
| Draft tagged | router draft call | Stage `Draft`, exceptions still propagate (not routed through the swallowing wrapper) |
| Report | `prose --beat-write-trace (--beat-id <guid> \| --last) [--json]` | Calls by stage (chat/embed, tokens, cost, ms); stage execution order with coverage join; gate-skipped stages; **UNATTRIBUTED** calls listed, never dropped |

Migration `AddLlmCallStageAndBeat` (nullable columns + `BeatWriteStageLogs` table). Tests:
`LlmActionContextStageTests`, three new cases in `LlmCallHistoryTests`.

### 1.3 The measurement — one beat, Magenta & Gunmetal Ch3, beat #17292

No book in the corpus had an unwritten beat (all 15 draft GLMZ books scanned; every beat carries
prose), so — with the author's approval — one new beat was appended to the end of MxG Chapter 3
with an outline-consistent tag-scene goal and written once via `--expand-beat`
(`BeatWriteReason.Generation`). Keep or delete is the author's call
(`prose --beat delete --id 01a07a0a-50a4-7084-b18d-342e1e953e01 --node ef777a7f-bed9-4d4f-8560-436c3a533e06`).

**Headline:** 29 LLM calls = **15 chat + 14 embedding** · 30,135 tokens in / 2,420 out ·
**$0.0287** · Σ stage wall time **52.8 s** · 0 failed · 0 unattributed · post-write cluster
complete.

| Stage | Calls | Chat | Embed | In tok | Out tok | Cost | ms | Model(s) |
|---|---|---|---|---|---|---|---|---|
| Draft | 2 | 1 | 1 | 22,871 | 1,299 | $0.0231 | 18,854 | claude-api (provider default), text-embedding-3-small |
| EntityContextStack.ReconcileAsync (post) | 12 | 11 | 1 | 1,450 | 221 | $0.0012 | 11,012 | claude-haiku-4-5 ×11, embed ×1 |
| EntityContextService (pre) | 9 | 0 | 9 | 152 | 0 | $0.0000 | 6,249 | text-embedding-3-small |
| BeatExtractionService.ExtractAllAsync (post) | 1 | 1 | 0 | 753 | 520 | $0.0027 | 6,781 | claude-haiku-4-5 |
| LibertyReportService.AnalyseAsync (post) | 1 | 1 | 0 | 38 | 247 | $0.0010 | 3,054 | claude-haiku-4-5 |
| ContinuityEnforcer (post) | 1 | 1 | 0 | 138 | 133 | $0.0006 | 2,317 | claude-api (provider default) |
| DocContextService (pre) | 2 | 0 | 2 | 3,476 | 0 | $0.0001 | 848 | text-embedding-3-small |
| SemanticFidelityService (post) | 1 | 0 | 1 | 1,257 | 0 | $0.0000 | 486 | text-embedding-3-small |

Stage execution (38 traced stages; those making no LLM call are listed for wall time only):

| Phase | Stage | ms | LLM | Coverage row |
|---|---|---|---|---|
| pre | EntityContextService | 3,672 | 9 embed | active 5,593 ch |
| pre | CharactersInScene auto-populate | 0 | – | – |
| pre | SceneContextAssembler | 9 | – | applicable, **empty** |
| pre | DocContextService | 1,154 | 2 embed | active 21,591 ch |
| pre | BeatPlaceService prior-place · DefaultLocation fallback | 7 · 4 | – | – |
| pre | SceneContextBuilder | **3,974** | – | – |
| pre | EmotionalDepthLoopback · BeatProseMetricsService · ReaderProxyQA guidance · ContinuityEnforcer guidance | 56 · 4 · 11 · 8 | – | – |
| pre | ContinuityService | 14 | – | active 168 ch |
| pre | TensionEscalationService · ReaderKnowledgeService | 1 · 7 | – | applicable, empty |
| pre | ConsequenceService | **2,285** | – | active 182 ch |
| pre | WorldStateAtBeatService | 32 | – | – |
| pre | NarrativeSummaryService · OpenThreadsService · MotifLedgerService | 5 · 8 · 6 | – | applicable, empty |
| pre | ChapterSummaryService | 9 | – | active 9,503 ch |
| pre | BookStateLedgerService · BeatBlueprintDecision · StoryScopeGuidance | 8 · 8 · 1 | – | – |
| pre | StoryScienceService | 0 | – | active 897 ch |
| pre | StructuralBlueprintService | 38 | – | active 2,098 ch |
| pre | NarrativeChartService | 103 | – | active 1,799 ch |
| pre | EntityPreCheck | 120 | – | – |
| draft | **Draft** | **18,902** | 1 chat + 1 embed | – |
| post | ContextTelemetryService · WorkflowMonitorService · BeatModeDetector | 45 · 26 · 19 | – | – |
| post | **EntityContextStack.ReconcileAsync** | **9,471** | 11 chat + 1 embed | – |
| post | BeatExtractionService.ExtractAllAsync | 6,913 | 1 chat | – |
| post | ChapterSummaryService.ExtractAndSaveAsync | 2 | – | – |
| post | ContinuityEnforcer | 2,335 | 1 chat | active 0 ch |
| post | LibertyReportService.AnalyseAsync | 3,076 | 1 chat | – |
| post | SemanticFidelityService.CheckBeatIntentDriftAsync | 495 | 1 embed | – |

Stages whose gate evaluated false and therefore do not appear: `DialogueService`,
`SceneCollisionService`, `CanonGroundingService` (opt-in), `HarvestRevealedDetails` (opt-in).

### 1.4 Observations — facts from one write, not recommendations

- The draft is **80 % of the cost** ($0.0231 of $0.0287) and **36 % of the wall time**.
- **Everything that is not the draft is Haiku or embeddings** and totals $0.0056. The dollar cost
  of the fan-out is not the problem this RFC exists for; wall time and coherence are.
- **One stage makes 12 of the 29 calls**: `EntityContextStack.ReconcileAsync` — one claim
  extraction plus one conflict check per active entity (11 here). It is also the second-longest
  stage (9.5 s).
- Two stages with **zero LLM calls take 2–4 s each**: `SceneContextBuilder` (3,974 ms) and
  `ConsequenceService` (2,285 ms). Pure DB/static work should not cost that; worth a look before
  any LLM-side change.
- `SceneContextAssembler` ran in 9 ms and produced an **empty XRay block** on a beat whose goal
  names Rook and Boiler; consequently `SceneCollisionService` and `DialogueService` never gated on.
  `ConsequenceService` did produce a block (182 ch), so `CharactersInScene` was non-empty by then.
  Whether the XRay emptiness is a roster-resolution miss or a gate ordering effect is **unknown from
  one write** — it is the first thing to check on the next one.
- Chapter-close `ChapterSummaryService.ExtractAndSaveAsync` ran (2 ms) but made no call — the
  chapter already carried a summary; the beat was appended after a finished chapter, so this is
  expected, not a defect.
- The generated text was 5,195 chars on return and 7,609 chars stored — the difference is entity
  tag markup added on save.
- **The prose ignored its goal and nothing noticed.** The goal said *"ends on her stepping onto
  the Pulse platform"*; the writer instead invented an ArcSec sedan pulling up, a summons by her
  real name, and Rook getting in — an unplanned plot event appended after the book's deliberate
  final image — and opened with a stray `# Beat: The Spine at Morning` markdown header. Three
  post-write checks ran on this beat (`SemanticFidelityService` intent drift, `LibertyReportService`,
  `ContinuityEnforcer`) and **zero findings were filed** against it (`prose --findings list --node
  MxG`: newest row is 2026-08-25). One beat is not a verdict on those checks, but it is the exact
  shape of failure the funnel exists to catch: the goal, the outline's ending, and the output
  disagreed, and no instrument said so. The beat is throwaway and should be deleted (author's call).

### 1.5 Why this is one data point and what the next ones should vary

A single beat appended to a finished chapter is the wrong shape for two of the questions that
matter (XRay emptiness; chapter-close cost). Before any cut: run `--beat-write-trace` on several
ordinary writes — a mid-chapter beat with two named characters in the goal, a combat beat, a
dialogue beat, a first-beat-of-book — and compare the stage tables. The command is free.


## 2. Diagnosis — why the beat ignored its brief, from the code (read 2026-09-07)

Three facts, each verified by reading `BeatGeneratorService.GenerateBeatAsync` (lines 64–275),
the router's post-write cluster, and every writer of `Beats.Text`:

1. **The brief is one line at the end of a prompt that has already said everything else.**
   `BeatGoal` appears exactly once, as `BEAT GOAL: …` in the user message, after the cached
   prefix (universe primer, world facts, interiority budget, the whole book bible, commandments,
   story-science), after a dynamic system block of **30 conditional guidance sections** (world
   context, X-ray, canon claims, entity memory, doc stack, location, dialogue, style anchors,
   plants, consequences, world state, collisions, six kinds of loop-back guidance, tension,
   reader knowledge, narrative summary, chapter summary, open threads, motifs, plot state, pacing,
   structural role, offscreen activity, blueprint slice), and after **6,000 characters of scene
   tail**. It is not restated, not weighted, not checked. On beat #17292 the whole prompt was
   ~22,900 tokens and the brief was ~150 characters of it. Temperature is hard-coded 0.85.
2. **Nothing validates the draft.** No header strip, no length band, no retry, no check that the
   ending or the named entities match the brief. `ClaudeService` trims whitespace; the router
   returns the string; the CLI saves it with `BeatWriteReason.Generation`. The `# Beat: …` header
   went into the book verbatim. The three post-write "checks" run **after** the save, asynchronously,
   and can only file findings: `SemanticFidelityService` compares an embedding of the goal with an
   embedding of the prose (cosine, floor 0.50) — structurally blind to "invented a plot event";
   `LibertyReportService` files only `entity_invention` below CoolFactor 5 and its findings are
   **never looped back** into generation; `ContinuityEnforcer` checks ≤40 canon claims for the
   on-page cast and *is* looped back, but only as a hint for a *later* beat, never as a gate on this
   one.
3. **28 of the 29 calls do not touch the text, and 12 of them produce nothing durable.**
   `EntityContextStack.ReconcileAsync` makes 1 claim-extraction call + 1 Haiku conflict check **per
   active entity** (11 here); its output is an **in-process dictionary** (lost on every Hub
   restart) plus `FindingCategory.Other` rows that no loop-back reads. `BeatExtractionService`
   (1 Haiku call) is the opposite: its six slices write the five tables the *next* beat reads
   (`ReaderKnowledgeFacts`, `NarrativeSummaryEntries`, `NodeOpenThreads`, `BookMotifs`,
   `BookPlotEvents`, plus `Beats.PlaceName`) — a closed loop that is load-bearing. The Story Ledger
   (`ContinuityClaims`) is **not** written by the writer at all; only `--continuity extract` /
   `--tuned-read` write it.

And one fact about the funnel: `NodeWorkbenchService.UpdateBeatTextAsync` is the choke point for
14 named write paths, but three writers still bypass it — `StripBeatArtifactsCli` (no reason),
`NodeBeatWriter` used by `--import-book`/reimport (new rows, no reason), and
`DuplicateEntityScanService` (direct write, reason set). `BeatGeneratorService.GenerateBeatAsync`
has exactly one caller (the router), which is the right shape already.

## 3. The design — one writer, four phases, one door

```
                 ┌──────────────── BeatWriter.WriteAsync(beatId) ────────────────┐
                 │                                                               │
  Brief ───────► │  ASSEMBLE ─► DRAFT (1 call) ─► GATE ─pass─► COMMIT ─► EXTRACT  │
  (contract)     │      ▲                          │fail          │        (1 call)│
                 │      │                          ▼              │               │
  Context tiers  │  budgeted            retry once with reasons   │ UpdateBeatText │
  (Ledger, DCM,  │  by tier             then STOP, hand to author │ (Generation)   │
   summaries,    │                                                │               │
   entities)     └────────────────────────────────────────────────┴───────────────┘
```

**Law (extends RFC 0009):** the writer may *generate* and may *refuse to save*; it may never
*rewrite* what the author accepted. A retry before the first save is generation. A finding
still describes and never instructs.

### 3.1 The Brief is a contract, not a line

New record `BeatBrief` built by `BeatBriefBuilder` from data that already exists:

| Field | Source | Enforced how |
|---|---|---|
| `Goal` | `Beat.Description` (else `Title`) | Prompt: first line of the user message **and** restated as the last line ("Write exactly this beat; stop where it says to stop.") |
| `StopBefore` | the **next** beat's `Description` in reading order (or the outline's next Event Sequence line at chapter end; `null` = last beat of book) | Gate verifier question 1: "Does the draft cross into this?" — this is the ending constraint that would have caught the ArcSec sedan |
| `MustInclude` | entities the goal names (existing `UniverseGraphService` name scan on the goal) | Deterministic: each name appears in the draft |
| `NoNewPlot` | always on | Gate verifier question 2: "Name any event the draft adds that the goal does not imply" |
| `Pov` | outline POV map row for this beat (`BeatEntityPresence 'pov'`) | Deterministic: person/pronoun sanity + name; Register pinned as today |
| `Register` | POV character record (DCM tier 4) | unchanged |
| `TargetWords` | `Beat.TargetWords` or Swain default | Deterministic band: 0.5×–1.6× |
| `Subtext` | `Beat.Subtext` | unchanged, writer-only |

The brief is persisted with the write (`BeatContextTrace.ContextJson` already archives the
assembled context; add `BriefJson`) so a later reader can see what the writer was told to do.

### 3.2 Assemble: the same sources, a budget, and an order

Nothing new is invented here; the 30 blocks are **re-tiered and capped**, and the brief moves
to the front. Tier order in the prompt, each with a character ceiling logged per write:

| Tier | Blocks (today's names) | Ceiling | Why it stays |
|---|---|---|---|
| **A · Brief** | `BeatBrief` (goal, stop-before, POV, must-include, length) | — | it is the job |
| **B · Facts** | canon claims (`ContinuityService`), consequences/gear, world state, X-ray roster, relationship context, universal facts | 6,000 ch | contradictions are the defects the author actually fixes (RFC 0010 §1) |
| **C · Memory** | scene-so-far tail, narrative summary, chapter summary, open threads, plot state, reader knowledge, motifs, beat place | 8,000 ch | the closed loop that keeps a 500-page book coherent |
| **D · Voice** | register (POV record), CRAFT base, universe craft, DELIGHT (3 rules), pacing, structural role | 3,000 ch | measured in §4; kept only at the level the A/B supports |
| **E · Opinion** | six finding loop-backs, story-science, blueprint slice, offscreen chart, style anchors, tension, collision | **0 by default** | none has an applied finding (RFC 0010); each is re-admitted only by the §4 A/B showing it changed the output |

The bible goes in the cached prefix as today. Temperature becomes a brief field (default 0.7;
combat 0.6) instead of a constant.

### 3.3 Draft: one call, one contract

`GenerateBeatAsync` stays the single generation call, made private to the writer. Its response
contract: **prose only**. Deterministic post-processing before anything else sees it: strip a
leading markdown header or `Beat:`/`Title:` label line, strip trailing meta commentary after a
`---`, normalise whitespace. Empty → treated as a gate failure, not saved.

### 3.4 Gate: deterministic first, then one verifier call

Runs **before** save, synchronously. Order:

1. **Deterministic (free):** header/label stripped clean · length band · every `MustInclude`
   name present · no proper noun absent from `UniverseGraphService.AllNodes()` (today's
   `EntityPreCheck` applied to the *output*, not just the goal) · `CraftNativeRules` (the two
   validated deterministic craft rules) · `RepetitionLintService` echo/crutch checks on the draft.
2. **One Haiku call, strict JSON, reasons before verdicts** (house rule): given the brief, the
   ≤40 canon claims shown to the writer, and the draft — (a) does the draft cross `StopBefore`?
   (b) list any plot event the goal does not imply; (c) list any contradiction of a shown claim.
   This one call **replaces** `ContinuityEnforcer`, `LibertyReportService` and
   `SemanticFidelityService`.
3. **Fail → retry once**, appending the verifier's reasons to the brief as constraints
   ("Do not: …"). **Fail again → do not save.** Return the draft and the reasons to the caller;
   the CLI prints them and exits non-zero; the MCP tool returns them. The author decides.
4. Pass → `UpdateBeatTextAsync(…, BeatWriteReason.Generation)`; the gate result (pass/retry/fail,
   verifier JSON) is stored on the trace, not as a Finding.

### 3.5 Extract: one call, chapter close when applicable

`BeatExtractionService.ExtractAllAsync` (already one consolidated Haiku call feeding the five
next-beat tables) stays. `ChapterSummaryService.ExtractAndSaveAsync` stays on the last beat of a
chapter. The in-memory `EntityContextStack` keeps `Push`/`RecordMentions` (no LLM). **Deleted:**
`ReconcileAsync`'s claim-extraction call and its per-entity conflict checks (12 calls → 0; output
was volatile memory plus `Other` findings nobody reads). If the author later wants per-beat Story
Ledger extraction, it is added as **one** hash-gated `ContinuityExtractionService` call — a
separate decision with its own measurement, not smuggled back in.

### 3.6 One door for beat text

- `NodeWorkbenchService.UpdateBeatTextAsync` (exists) + new `CreateBeatAsync(…, BeatWriteReason)`
  for imports. Route `NodeBeatWriter` (Import), `StripBeatArtifactsCli` (TagMaintenance), and
  `DuplicateEntityScanService` (TagMaintenance) through them.
- `BeatGeneratorService` becomes `internal` to the writer; `ILlmService` is no longer injected into
  any class that also touches `Beats.Text` (there should be none after the routing above).
- **Architecture test** (`Prose.UnitTests/WriterFunnelTests.cs`): fails the build if any file
  outside `Prose.Core/Services/Writer/` references `GenerateBeatAsync`, or any file outside
  `NodeWorkbenchService` assigns `.Text =` on a `Beat` entity, or any `Beats.Add(` occurs outside
  the workbench. This is the compiler enforcing "sole funnel", the same way `BeatWriteReason`
  already enforces "declare yourself".

### 3.7 Observability stays and gets one thing more

`--beat-write-trace` already shows calls by stage. Add per-tier character counts and the gate
verdict to the stage log so a trace answers: what was the brief, how big was each tier, did the
gate pass, and on which question it failed.

## 4. Phase 0 — the A/B that decides how much of the pile survives (before any cut)

Five ordinary beats, chosen by the author or taken as the next five beats the author wants
written anyway (so the work is not throwaway): one mid-chapter dialogue beat with two named
characters, one combat beat, one transition, one first-beat-of-chapter, one chapter close.
Each written **twice**, saved to a scratch copy of the book (`duplicate_book`), not the live book:

| Arm | Prompt |
|---|---|
| **Full** | today's router, unchanged |
| **Brief** | tiers A + B + C + D only (no tier E), brief first and last, temperature 0.7 |

Scored, per beat, by things that do not need a panel:

1. Brief compliance (deterministic + the §3.4 verifier run as a *report* on both arms): ending
   honored? new plot events? all named entities present? unknown proper nouns?
2. `--lint-prose` finding count on each draft.
3. Calls, cost, wall time from `--beat-write-trace`.
4. The author reads both blind (arm labels hidden) and marks which they would keep.

Cost: ~10 drafts ≈ $0.30. Decision rule, fixed in advance: a tier-E block is re-admitted only if
removing it changed (1) or (4) for the worse on ≥2 of 5 beats. Everything else in tier E is deleted
under the RFC 0010 rule. If the *Brief* arm loses on (4), the design in §3.2 is wrong and this RFC
stops here and says so.

Also answered by the same five traces, for free: why `SceneContextAssembler` produced an empty
X-ray on #17292 (roster resolution vs gate order — read `AssembleForBeatAsync` on a beat where it
is empty), and where `SceneContextBuilder` (4.0 s) and `ConsequenceService` (2.3 s) spend their
time with zero LLM calls (a stopwatch inside each; both should be milliseconds).

## 5. Execution order

| Step | Work | Depends on | Size |
|---|---|---|---|
| 0 | Phase 0 A/B (§4) — scratch book, 10 drafts, scoring script, author blind read | §1 tooling (done) | 1 session, ~$0.30 |
| 1 | `BeatBrief` + `BeatBriefBuilder` (`StopBefore` from next beat / outline); brief first-and-last in the prompt; temperature from brief | — | small |
| 2 | Deterministic gate (§3.4 step 1) + response post-processing (§3.3) | 1 | small |
| 3 | Verifier call (§3.4 step 2) with retry-once/refuse semantics; CLI + MCP surface the refusal | 1, 2 | medium |
| 4 | Delete `ReconcileAsync` LLM calls (keep LRU push), `LibertyReportService`, `SemanticFidelityService`, `ContinuityEnforcer` (absorbed by 3); delete tier-E blocks the A/B did not re-admit; remove the corresponding `BeatContext` fields and coverage rows | 0, 3 | medium, mostly deletion |
| 5 | Re-tier and cap the remaining blocks (§3.2); per-tier char counts + gate verdict on the trace | 4 | medium |
| 6 | One door: `CreateBeatAsync`, route the three bypassing writers, `BeatGeneratorService` internal, architecture test | — (parallel to 1–5) | small |
| 7 | Docs: CLAUDE.md "Prose Engine Services" table rewritten to the four phases; ENGINE.md thresholds; this RFC §9 status | 4, 5, 6 | small |
| 8 | Re-run the five beats through the finished writer; publish the before/after trace pair in §9 | all | ~$0.15 |

Nothing in steps 1–6 touches a beat that has accepted prose. BCODA's publish gate is re-read
after step 5 as a regression check; it must still be 5/5 (it will — nothing here rewrites).

## 6. Acceptance — what "fixed" means, measurably

| Criterion | Today (#17292) | Target |
|---|---|---|
| Chat calls per beat write | 15 | **≤ 4** (draft, verifier, ≤1 retry, extraction; +1 at chapter close) |
| Total LLM+embedding calls | 29 | **≤ 10** |
| Brief compliance on the 5-beat set (ending honored, no invented plot, entities present) | 0/1 | **≥ 4/5 pass the gate without retry; 5/5 after ≤1 retry** |
| Drafts saved that fail the gate | 1 (saved) | **0** — a failing draft is never saved |
| Markdown/label artefacts saved | 1 | 0 |
| `Beats.Text` writes without a `BeatWriteReason` | 2 paths | 0, enforced by test |
| Callers of `GenerateBeatAsync` outside the writer | 0 | 0, enforced by test |
| UNATTRIBUTED calls in `--beat-write-trace` | 0 | 0 |
| `--edit-distribution` (BCODA 472/475, mean 4.04) | baseline | unchanged |
| BCODA `--publish-readiness` | 5/5 | 5/5 |
| Wall time per write | 52.8 s | measured; expected ≈ draft + verifier + extraction ≈ 30 s |

## 7. What is deliberately not in this RFC

- **Panels, votes, scores.** SS-A44 stands. The gate's verifier answers three yes/no questions
  with reasons; it does not grade.
- **Rewriting any accepted beat**, including #17292's predecessors or the throwaway itself. The
  throwaway is deleted by the author or left; the writer never touches it.
- **Story Ledger extraction per beat.** Today it is CLI-only and that is a conscious state; adding
  it is a separate decision (§3.5).
- **A new "orchestrator" layer.** The writer is `ProseWriterRouter` renamed and cut down, not a
  fifth thing on top of it.

## 8. The rule that keeps this from growing back

Add to CLAUDE.md, next to the RFC 0009 law: *No block is added to the writer's prompt and no call
is added to the write path without a `--beat-write-trace` pair (before/after) on the same beat
showing the output changed. A service that cannot show that is not added.* The 93 awaits were
written by sessions that each had a good reason; the trace is the only thing that would have
stopped them, so it is the thing that gates the door now.

## 9. Status

- §1 done 2026-09-07 (`32e2491d1`, `ad7a26e9a`).
- §2–§8 written 2026-09-07 on the author's instruction (*"please fix this, write the plan; total
  and complete"*).
- **Author instruction 2026-09-07: *"clone BCODA and make it a better book using this plan."*** The
  clone is **BCODA2** (`01a07a27-de94-7fd9-944b-9028bc0941f4`, slug
  `bushido-coda-writer-clone-01a07a27`, 38 chapters / 475 beats). `DuplicateNodeAsync` was fixed
  first (`d5ac4d374`) — it had silently dropped the outline, narrative mode, default location,
  blueprint and the beat brief fields, so no duplicate could have been a like-for-like testbed.
  The real BCODA is not touched by anything below.
- **Steps 1–3 shipped** (`9c81fd854`): `BeatBrief` / `BeatBriefBuilder`, brief first-and-last in
  the prompt, `DraftPostProcessor`, `DraftGate`, `BriefVerifier`, `DraftAndGateAsync` with
  retry-once / refuse, `LeanContext` + `SkipPostWrite`, `--expand-beat --lean --dry-run --out`.
- **Step 6 shipped** (`7870f5aba`): `WriterFunnelTests`. Its first run found **eight** undeclared
  beat writers beyond the three the code read had named; every one now stamps a reason (new
  `Plan` member for seed-spine's empty beats). `--strip-beat-artifacts` goes through the door.
- **Phase 0 run 1** (5 beats × full/lean, dry runs on BCODA2): 6 passed, 4 refused. The refusals
  taught three things that changed the code the same night: (a) a description is intent, not
  events — both arms wrote a good Kyle/Pixel scene and neither had the pediatric prosthetic arm
  the beat is about, so the brief now carries the beat's CURRENT event summary as "what happens";
  (b) the verifier called a fee split an "added event" — recalibrated to "something a later beat
  would have to honour"; (c) the roster heuristic's POV row said Mrs. Chen on a Kyle scene and
  the gate refused a correct draft — POV now comes only from the outline map. One refusal was a
  true catch: the lean combat draft gave War Dog a fourth arm with a belt-fed weapon.
- **Phase 0 run 2** in progress with the corrected brief; results and the author's blind read go
  in §10. **Not started:** step 4 (cuts — waits on run 2), step 5 (tier ceilings), step 7 (docs),
  step 8, and the full BCODA2 pass.

## 10. Phase 0 — run 2 (2026-09-07, brief with events, calibrated verifier, POV from map only)

Five BCODA2 beats × two arms, dry runs, nothing saved. **Full** = every stage as today plus the
brief and gate; **Lean** = tiers A–D only (no finding loop-backs, story-science, blueprint slice,
offscreen chart, style anchors, tension, collision). The gate ran on both.

| Beat | Role | Full | Lean |
|---|---|---|---|
| #17369 | dialogue (Kyle/Pixel, prosthetic arm) | **refused** ×2 — "Kyle leaves exact change on the workbench" | **refused** ×2 — "someone brings the arm in tomorrow (minimal impact)" |
| #17402 | combat (War Dog opens) | pass after 1 retry | pass first try |
| #17448 | transition (ride home) | pass | **refused** — Mira's warning, "the Antiquarian", the file moving through markets |
| #17442 | chapter opener (Sable's contract) | pass | **refused** — the Lotus will contact him within 48 h |
| #17399 | chapter close (Pixel returns) | pass | pass |

Gate outcome: **Full 4/5 saved-able, Lean 2/5.** Run 1 (description-only brief) had been 6/10 with
different casualties; the events line fixed the "wrote a fine scene about the wrong thing" failure
(no run-2 refusal is about a missing event). The remaining refusals split two ways: three are
plausibly right (an invented contact protocol; Mira/Antiquarian/markets pulled in from memory
that the brief did not call for), and two are the verifier being stricter than a reader would be
(exact change on a bench; "minimal impact" in its own words). The verifier prompt now carries a
materiality test ("would a later chapter be wrong to ignore it?") — not yet re-measured.

Per-write LLM calls (trace, `--batch`; the second arm always ran with a warm entity stack, so the
Full/Lean embed difference is order, not tier E):

| Beat / arm | Calls | Chat | Embed | of which `EntityContextService` embeds | Cost | Wall |
|---|---|---|---|---|---|---|
| #17369 full / lean | 56 / 31 | 4 / 6 | 52 / 25 | 44 / 22 | $0.049 / $0.071 | 53 s / 46 s |
| #17402 full / lean | 56 / 29 | 4 / 4 | 52 / 25 | 44 / 22 | $0.049 / $0.048 | 39 s / 19 s |
| #17448 full / lean | 54 / 32 | 2 / 6 | 52 / 26 | 45 / 23 | $0.024 / $0.072 | 22 s / 41 s |
| #17442 full / lean | 54 / 31 | 3 / 6 | 51 / 25 | 44 / 22 | $0.030 / $0.077 | 34 s / 41 s |
| #17399 full / lean | 45 / 29 | 2 / 4 | 43 / 25 | 39 / 22 | $0.024 / $0.046 | 24 s / 25 s |

What the numbers say, and no more:

- **Chat calls per write are now 2–6** (draft, verifier, at most one retry of each). On MxG they
  were 15. The post-write cluster did not run here (dry run); with it, add the one extraction call.
- **The fan-out that is left is embeddings, and it is one loop:** `EntityContextService.
  ExpandEdgesAsync` re-embedded every depth-0 and depth-1 entity's *name* to find neighbours —
  vectors the `EntityEmbeddings` table already held. 39–45 HTTP round-trips and 9–12 s per write.
  Fixed the same night (`caa382fc9`, `FindSimilarToEntityAsync`: cosine against the stored vector,
  in SQL, zero calls); measured below in §10.1.
- **A refusal doubles the cost** (two drafts, two verifier calls): the lean arm's higher cost is
  its refusal rate, not its prompt.
- **Lean did not win on brief compliance in this sample.** Two of its three refusals pulled plot
  from memory that the brief did not ask for. Five beats is not a verdict on tier E; it is a reason
  not to cut tier E on the strength of the MxG intuition alone. The author's blind read (the page
  published 2026-09-07, key in the scratchpad) is the fourth score in §4 and is pending.

**Decision rule check (§4):** no tier-E block is re-admitted or removed yet — the blind read is
missing, and the verifier moved between runs. Step 4 waits.

### 10.1 After the stored-vector fix — one dry write of #17399 on the deployed Hub

| | Before (run 2, full arm) | After (`caa382fc9`) |
|---|---|---|
| Total LLM+embedding calls | 45 | **11** |
| Chat | 2 | 4 (draft, verifier, retry, verifier) |
| Embedding | 43 | **7** (style anchors ×2, DocContext ×2, EntityContext ×2, X-ray ×1) |
| `EntityContextService` embeds | 39 | **2** |
| Cost | $0.024 | $0.048 (a retry doubled the draft) |
| Wall time | 24 s | 52 s (two drafts + two verifier calls) |

The §6 target "≤ 10 total calls" is met on a first-try pass (9) and missed by one on a retry.
The remaining wall time is the two model calls the design wants (draft, verifier) — the loop
the writer pays for now is the loop that does the writing and the checking, and nothing else.
