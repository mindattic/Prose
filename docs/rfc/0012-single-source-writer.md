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

## 2. Step two — not started

Decide, from ≥5 traces, which fan-out calls change what the writer produces (measure: diff the
assembled `BeatContextTrace` with and without the stage's block) and which only file findings
nobody applies (RFC 0010's rule). Then design the funnel. Not before.
