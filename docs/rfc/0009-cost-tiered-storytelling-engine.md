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
