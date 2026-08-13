---
codex: 1
project: Prose
code: SS
layer: rfc
status: draft
updated: 2026-06-22
---

# RFC 0009 — The Cost-Tiered Storytelling Engine {#SS-RFC-0009}

> Status: **active / implementing** · Author: engine pass 2026-06-22 · Supersedes nothing; extends
> [0002 (entity X-Ray)](0002-entity-xray-scene-assembly.md) and the review subsystem.

## 0. Problem

The engine is powerful but ungoverned on cost. A single strand review fires **~37 LLM calls**
(sampled) / **51** (full) / **128+** (segment study), and there is **no way to ask for a cheap
in-flight read versus an expensive publish-gate read** — every check pays full price. Meanwhile
prose generation already runs lean (Sonnet draft), and the continuity suite is **mostly
deterministic and nearly free** (6 of 7 services are DB-only). So the money bleeds almost entirely
through **reviews** and through **serial, disconnected narrative-science passes**.

This RFC introduces a single governing idea — **Effort Tiers driven by task importance** — and four
concrete changes that make the engine scale its spend to what the task is worth, while *raising*
quality where it matters (storytelling science, character agency, timeline integrity).

## 1. Measured cost surface (baseline)

| Path | Calls / unit | Model | Notes |
|---|---|---|---|
| Review — sampled (default) | 12 diag + 20 ballot + 4 prose + 1 summary = **37** | mixed providers | `StrandReviewService` |
| Review — full | **51** | mixed | census readers |
| Review — segment study | **128** | mixed | panel |
| NarrativeScience (strand) | **N beats × analyzer**, *serial* | app LLM | disconnected from prose |
| Prose — expand beat | 1 Sonnet + 1 OOC(Haiku) + 2 embeds | Sonnet | already lean |
| Prose — suggest panel (opt-in) | 10 Opus | Opus | UI button |
| Prose — rank panel (opt-in) | 100 Haiku | Haiku | UI button |
| Continuity — 6 services | **0 LLM** (deterministic) | — | cheap |
| Continuity — BehavioralInvariant | 1 / character | app LLM | underused |
| Continuity — CanonContradiction | chunked (6k-char) | app LLM | expensive, run sparingly |

**Conclusion:** the lever is reviews. Tiering reviews is ~80 % of the win.

## 2. The governing idea: Effort Tiers

A review's job is **to guide prose, not to mint a number.** The dial is therefore *"what signal do
I need right now?"* — not merely "fewer voters."

| Tier | Used for | Signal needed | Spend philosophy |
|---|---|---|---|
| **Draft** | mid-draft spot checks; per-beat iteration loops | actionable per-beat gripes; rough direction | cheapest models, tiny panel, no diagnosis, no prose upgrades |
| **Standard** | per-strand standalone gate (≥82 %); routine iteration | trustworthy score + top fixes | balanced — the workhorse |
| **Deep** | cumulative prefix gate (≥85 %); pre-publish; flagship (Coda → 90) | defensible precision + prose-level critique | full panel, best models, all checks |

This maps cleanly onto the **mandated dual-review workflow** (CLAUDE.md): standalone = **Standard**,
cumulative = **Deep** — and adds a *new* **Draft** tier for in-flight iteration that previously did
not exist (today people fire the 37-call sampled review just to check progress — pure waste).

### 2.1 Review effort profiles (concrete mapping)

```
draft:    diagnosis=off, ballots=6,  prose=0, providers=cheap(haiku/flash/nano), judge=haiku   → ~6 calls   (−84%)
standard: diagnosis=off, ballots=12, prose=2, providers=mixed,                   judge=sonnet  → ~15 calls  (−60%)
deep:     diagnosis=on,  ballots=20, prose=4, providers=full(incl claude-sonnet),judge=claude  → ~37 calls  (baseline)
```

Profiles live in `SettingsService` as overridable defaults; selected per-invocation by
`--effort draft|standard|deep` on `ReviewStrandCli` (and the `review_strand` MCP tool). When omitted,
the default is **standard**; `--final` / publish paths request **deep**.

### 2.2 Why the score stays trustworthy at Draft

Draft's 6 ballots give a wide 95 % CI (±~6 pts) and that is *correct* — Draft is not for gate
decisions, it is for "which beats are dragging." The per-beat weakness tags survive small panels
fine (they are categorical, not averaged). Gate decisions always use Standard or Deep.

## 3. Storytelling science: from *report* to *guidance*

`NarrativeScienceService` (Storr framework: sacred flaw, dramatic question, scene engagement, five-act,
antihero empathy) currently runs **serial loops** and emits **standalone reports nobody feeds back**.

1. **Parallelize** the per-beat loops (`Task.WhenAll` + concurrency cap) — wall-clock win, same tokens.
2. **Feed findings into prose context.** At expand time inject the relevant signal: the *dramatic
   question* this beat must advance, the POV character's *sacred-flaw pressure*, the *6-point scene
   checklist*. This closes the "science perfect → story perfect" loop the goal demands.
3. **Tier it:** Draft skips science; Standard runs the two per-beat analyzers (dramatic-question +
   scene-engagement); Deep runs all five.

## 4. Prose-Lessons memory ("a score isn't always the point")

Explicit goal: *remember lessons learned during CLI prose changes — e.g. sometimes a beat doing its
job matters more than its score; "delight and surprise" matters.*

**Store:** `ProseLessons`, a SQL-backed KV collection (via `SettingsKvStore` — **no new JSON file**
per `feedback_no_json_files`, **no migration**). Each lesson:

```
scope:  global | strand:<slug> | beat:<id>
kind:   score-vs-function | delight | voice | pacing | continuity | other
text:   the ruling
addedAt
```

**CLI:** `--lesson-add --scope <s> --kind <k> "text"` · `--lessons-list [--scope <s>]`.

**Injection (the point):**
- Into **review ballot prompts** — `EDITORIAL LESSONS (the human has ruled on these; respect them)`
  so reviewers stop dinging beats the author already decided are doing their job. Kills the
  score-chasing treadmill.
- Into **prose generation context** — lessons tagged `delight`/`voice` are woven into the brief.

## 5. Continuity hardening (cheap, deterministic)

- **New `TimelineConsistencyService` (0 LLM):** fills the gap the audit found.
  - *Dead-character-acting:* `EntityStateEvents` aspect `status=dead` / `condition.*.severity=dead`
    at story-time T ⇒ flag any later `ChapterBeat` where that character participates.
  - *Wound regression:* severity dropping faster than its healing curve, or `healed` before the
    injury's `InWorldDate`.
  - Pairs with the existing deterministic `LocationContradictionService` (teleport) to cover the
    cheap continuity surface end-to-end.
- **Character agency at generation time:** `SceneContextAssembler` already injects *voice* fields but
  **not** `CharacterBehavioralRules` (decision_rules / escalation_ladder / breaking_points). Inject
  them so characters act in-character *when written*, not only get flagged after. `BehavioralInvariant`
  becomes the Standard+ post-check, not the only line of defence. Directly serves "not will-less
  vessels."

## 6. Overlap map (audit result: minimal — document, don't delete)

- `AmbientDetailInjector` (pre-gen sensory) and `GearCarryEnforcer` (post-gen validation) share
  carry-edge reads but sit at **opposite ends of the pipeline** — complementary, keep both.
- `WorldStateAtBeatService` (broad) vs `AmbientDetailInjector` (narrow, carry-only) — shallow overlap,
  different output shape, keep both.
- **Real conceptual overlap:** `ContinuousQualityService` (auto-fires 2 calls/beat-save) vs
  `StrandReviewService`. Ruling: ContinuousQuality is **Draft-tier only** and stays gated by the
  existing `ReviewAutoRunEnabled`; it must never escalate to the full panel.

## 7. Phasing

1. **P1 — Review effort tiers** (§2). Biggest saver. `SettingsService` profiles + `--effort` flag +
   `StrandReviewService` scaling.
2. **P2 — Prose-Lessons memory** (§4). KV store + CLI + prompt injection.
3. **P3 — Narrative science** (§3): parallelize + prose injection + tier gating.
4. **P4 — Timeline validator + behavioral-rule injection** (§5).

## 8. Acceptance

- `--effort draft` review issues ≤ 8 LLM calls and still returns per-beat weakness tags + a score+CI.
- `--effort deep` reproduces today's sampled behaviour exactly.
- A recorded `score-vs-function` lesson appears verbatim in the next review's ballot prompt.
- `TimelineConsistencyService` flags a synthetic dead-character-acting case with zero LLM calls.
- Core builds green; existing review/continuity tests pass.

## 9. 2026-08-13 status update — this RFC's original cost surface is obsolete; here is the current one {#SS-RFC-0009-U1}

`StrandReviewService` (§1's entire "review" cost surface — the 37/51/128-call numbers) **no longer
exists in the codebase.** The engine has since moved to the Node/Beat/BeatNode model and
`BookHealthService`'s FREE/DEEP/FULL battery. §5's `TimelineConsistencyService` shipped as
described. This section replaces §1's cost table with the current one and records a holistic
cost review's actual findings — several of which contradict a prior session's hypothesis about
where the money was going, so they're recorded here rather than silently assumed correct.

### 9.1 Effort tiering already exists in two places — it did not silently re-accrete ungoverned

- **`BookHealthService.RunAsync(nodeId, tier)`** — `BookHealthTier.Free / Deep / Full`. FREE is
  ~10 deterministic-or-near-zero checks (timeline, nouns, sanity-scan, voice-drift, plant-audit,
  etc. — genuinely $0, see §9.3). DEEP adds ~10 one-call-per-book LLM audits. FULL adds the
  heaviest: `storyscope-audit`, `swain-audit` (1 Haiku call/beat), `chekhov-audit`, `five-act-map`
  (1 call/book), `dramatic-question` (1 call/beat — deliberately, see §9.2), `sacred-flaw` (1
  call/POV character). This is the "official battery" — `--audit-book` / `book_health` MCP tool.
- **`NarrativeScienceCli`** — independently implements `--effort draft|standard|deep` (skip / cheap
  subset / everything) for its own manual subcommands.
- Every always-on per-beat generation-time service named in the original plan for this cost
  review (`LibertyReportService`, `SemanticFidelityService`, `OpenThreadsService`,
  `BookStateLedgerService`, `ReaderKnowledgeService`, `SceneCollisionService`) is real and
  unconditional inside `ProseWriterRouter.WriteAsync` — **this is where the money actually is**,
  and it remains untouched by this pass (see §9.4).

### 9.2 Corrected findings — three hypotheses from the initial diagnosis did not survive contact with the code

A 2026-08-13 cost-reduction pass approved five items (Swain triple-payment, NarrativeScience
per-beat→per-act, fold deterministic checks into DCM, consolidate craft/structure audits, this
governance update) based on an initial Explore-agent diagnosis. Reading the actual implementations
turned up three corrections worth recording so they aren't re-asserted in a future pass:

1. **"Swain paid three times" was a mischaracterization.** `BeatAuditService`'s three
   chapter-close lenses (`CausalityService`, `AffectBehaviorService`, `InterpersonalDynamicsService`
   — `BeatLensServices.cs`) check causal-chain plausibility, emotion-drives-action, and relational
   subtext — **none of them are Swain scene/sequel classification.** They're also each a single
   whole-node call (not per-beat), run twice per chapter-close (before/after repair) as a
   legitimate "did the repair work" gate — 6 cheap calls/chapter, not a cost problem. The only
   real Swain-doctrine *audit* is `SwainAuditService` (1 Haiku call/beat, FULL tier only); the
   generation prompt's Swain instruction (`BeatGeneratorService.cs` ~L221-234) is free prompt
   text, not a second paid check. There is no triple-payment to cut. **No code changed for this
   item.**
2. **NarrativeScienceService's five analyzers were already 4-of-5 correctly scoped.**
   `MapFiveActStructureAsync` (1 call/book) and `AnalyzeSacredFlawAsync` (1 call/POV character)
   were never per-beat. Only `CheckDramaticQuestionAsync` ("dramatic-question") is genuinely
   per-beat — and deliberately so: Storr's "who is this person really?" is a per-beat revelation
   question by definition; forcing it to per-act would lose the signal, not just cut cost, so it
   stays as-is (FULL tier only). The one real waste found: `AuditSceneEngagementAsync`
   ("scene-anatomy") — per-beat, overlapping signal already covered by LogicSweep/DELIGHT/
   StoryScope, and with **no automated caller anywhere** — only a manual bulk CLI/MCP surface.
   **Removed outright** (`NarrativeScienceService.cs`, `NarrativeScienceCli.cs`,
   `Tools.NarrativeScience.cs`) rather than left as a cost trap. `CheckAntiheroEmpathyAsync` has
   no bulk-per-book caller either (MCP tool only, one beat/character at a time) — not a cost
   problem, left as-is.
3. **The claimed 3-way (setup/payoff) and 4-way (structural/outline agreement) redundancies
   don't hold up on inspection either.** `PlantPayoffService` is a free DB-only ledger query over
   *explicitly author-registered* pairs; `ChekhovAuditService` is an LLM discovery pass that finds
   props nobody registered — complementary coverage, not duplicate. Of the four "structural
   agreement" services, `OutlineAdherenceService` is not a post-hoc audit at all — it fires at
   every chapter close to *recalibrate remaining beat goals*, i.e. it's load-bearing inside the
   live generation loop; folding it into a whole-book pass would remove that correction loop, not
   just save money. `AltitudeAuditService` (bible/blueprint headline vs. chapter synopses),
   `StructuralDiagnosticService` (12 category-level craft checks), and `StoryScopeAuditService`
   (AI-fiction-tell detection) each check a genuinely different property, mostly already at
   book-level, not per-beat. Merging these into one "kitchen sink" prompt would likely produce
   *worse* answers per topic, not just cheaper ones. **No consolidation made** — see §9.5 for what
   this means for the approved plan's item 5.

### 9.3 What is actually free right now (confirmed, not assumed)

`TimelineConsistencyService`, `NounConsistencyService`, and the mention-indexing half of
`EntityRamificationService` (`IndexBeatMentionsAsync`) are all zero-LLM queries over data
`WorldStateLedger`/`BeatEntityMention` already holds — none call an LLM, none duplicate each
other's lookups (checked directly against `WorldStateLedger.cs` and `EntityDocService.cs`).
Documented inline in each file's header 2026-08-13.

### 9.4 Where the money actually is (unchanged by this pass — item 1, deferred)

The corrections in §9.2 mean the pre-pass estimate of "$400 for 8-11 books" being explained by
triple-redundant post-hoc audits was **too optimistic about how much was recoverable outside the
live generation path.** The concentrated cost is:
- `ProseWriterRouter.WriteAsync`'s ~10 unconditional per-beat side-calls (extraction cluster:
  `ReaderKnowledgeService`, `NarrativeSummaryService`, `OpenThreadsService`, `BookStateLedgerService`,
  plus `EntityContextService` conflict checks, `LibertyReportService`, `SemanticFidelityService`,
  `SceneCollisionService`) — fires on every beat of every book, no settings gate.
- Running `BookHealthService`'s FULL tier (`swain-audit` + `dramatic-question`, ~2 calls/beat) across
  a whole book, repeatedly, as part of the informal "full battery" habit.
Both were explicitly out of scope for the first pass (item 1 deferred by the user's own choice
pending proof that the safer cuts helped). Given §9.2's corrections showed most of those safer cuts
didn't materialize, item 1 was revisited the same day: `BeatExtractionService.cs` (new) now fires
ONE consolidated Haiku call in place of the five separate calls in `ReaderKnowledgeService.ExtractAsync`,
`NarrativeSummaryService.SummarizeSceneAsync`, `OpenThreadsService.DetectAndRegisterAsync` +
`MarkResolvedAsync`, and `BookStateLedgerService.ExtractAndRecordAsync` — all five really were
asking the model to look at the same just-written beat and pull out a different slice of
structured fact, confirmed by reading each prompt directly (unlike items 2/3/5's claims, this one
held up). Each service keeps its original method working standalone (split into an LLM-calling
wrapper + a `Persist*`-only method `BeatExtractionService` calls directly) for other callers
(`SceneGenerationService` still uses `NarrativeSummaryService.SummarizeSceneAsync` on its own path).
`ProseWriterRouter.WriteAsync` falls back to the original five-call sequence if
`BeatExtractionService` isn't wired, so behavior never silently regresses to "nothing runs."
This cuts the ~10-call/beat generation total in §9.4 by roughly 4 calls/beat (5 calls → 1).
The `SceneCollisionService`/`LibertyReportService`/`SemanticFidelityService`/`EntityContextService`
calls in that same cluster were NOT touched — each needs a distinct enough prompt shape (collision
physics, Rule-of-Cool judgment, intent-drift comparison, conflict reconciliation) that folding them
in would risk quality loss for a smaller marginal saving; revisit only if the combined call above
proves out in practice.

### 9.5 Cost estimator

`prose --estimate-cost --beats <N> [--tier free|deep|full]` (new, `Prose.Cli/Cli/EstimateCostCli.cs`)
prints the call count implied by `BookHealthService`'s current wiring for a book of N beats, so a
future addition's cost is visible before it ships rather than discovered by totaling a bill months
later.
