---
codex: 1
project: Prose
code: SS
layer: rfc
status: draft
updated: 2026-08-10
---

# RFC 0011 — Context-Aware Verification & the Path to a Universal Engine {#SS-RFC-0011}

> Status: **draft, awaiting author direction** · Author: engine pass 2026-08-10 · Extends
> [SS-A46 (Register doctrine)](../BIBLE.md) and the DCM four-layer hierarchy (CLAUDE.md) by
> generalizing them from generation-only to generation-and-verification.

## 0. Why this RFC exists

A single session (2026-08-10) fixed four separate bugs, in four separate services, that were
**the same bug wearing different clothes**:

| # | Service | Symptom | Root cause |
|---|---|---|---|
| 1 | `SemanticFidelityService` | 2,139 "SemanticDrift" findings, mostly false | Abstract synopsis vs. concrete prose scored low regardless of true fidelity — the check had no way to know a book's own register was terse-by-design |
| 2 | `BeatVerificationService` (`DeclaredPurpose`) | Same shape, ~150 findings | Identical fixed-threshold-vs-register-blind bug in a sibling check that re-implemented the same comparison independently |
| 3 | `BeatChecklistGateService` | 445 findings on one book (IxS), most "Cognitive-architecture tics" | The check had no visibility into the POV character's own hand-authored `SpeechVocabulary` — flagged her established voice as a generic AI tic |
| 4 | `BeatVerificationService` (staleness) | 41 stale findings on one book (UNDR), rediscovered **twice** in one session | No rule-version marker existed anywhere in the verification stack to answer "which books were checked under old logic" |

Each was fixed individually, well, and honestly. But each fix was **local** — a bespoke patch to
one service — when the actual defect is architectural: **the verification layer has no shared
way to ask "what do I already know about this beat/book/character that should change how I judge
it?"** Generation solved this exact problem months ago (SS-A46, the DCM Register/BookBible/
Universe/Base hierarchy). Verification never got the same treatment, so it keeps failing the same
way, in a new service, every time someone adds a new check — and every fix has looked, in the
moment, like it might be the one-off "magic bullet" instead of an instance of a known class.

This RFC names the end-state, extracts the one abstraction that would have prevented all four
incidents, and lays out the rest of the path to a genuinely universe/genre/scale-agnostic engine
as ordered, falsifiable bricks — not a wishlist.

## 1. The end-state, stated concretely

"Rich, robust, flexible, adapts to any universe/genre/scale" is a slogan until it's testable.
This RFC defines it as four properties, each with a concrete acceptance test:

1. **Context-complete verification.** No check ever flags a beat for something the engine already
   has on file as intentional (a character's established voice, a book's own register, a
   deliberate plant left for a sequel). *Test: every check service consults the same context
   provider generation already uses; zero checks read `Beats.Text`/`Description` in isolation
   without also resolving POV/Book/Universe context first.*
2. **Self-auditing freshness.** Nobody ever has to manually diff timestamps to find out which
   books were checked under stale logic. *Test: one command (`--staleness-report`, corpus-wide,
   any category) always has the answer — not a bespoke query invented per incident.*
3. **Graceful degradation.** A provider outage (Anthropic, OpenAI, either) never produces a wrong
   answer that looks like a right one — it produces a visible "not checked" state. *Test: every
   LLM-calling service has a test that asserts a provider failure surfaces as absence-of-signal,
   not false-pass or false-fail.*
4. **Genre/scale agnosticism, proven not assumed.** The engine has running proof — not
   architecture-diagram assertion — that a 1-beat standalone, a 500-beat flagship, and a
   nonfiction history all get the *same rigor*, appropriately adapted, not silently skipped rigor.
   *Test: an explicit stress-test book per shape, in CI or at minimum in a documented sweep,
   checked against the same craft/verification battery everything else runs.*

## 2. What's already good (don't rebuild this)

- **DCM (generation side) is real and working**: the four-layer Base/Universe/BookBible/Register
  hierarchy, POV-priority pinning, clue-gathering inference, relational cascade. This RFC extends
  it; it does not replace it.
- **Universe segregation is real**: GLMZ, SCRY/Fantasy, nonfiction, fiction, horror, erotica all
  coexist today with query-filter isolation. "Any genre" is already partially proven, not
  theoretical.
- **`FindingsService` is a real, working shared sink**: every category lands in one table with
  consistent `Upsert`/`DeleteBySummaryPrefix`/status lifecycle. This RFC's staleness work extends
  that table rather than inventing a parallel one.
- **The logic-sweep/Reader-Proxy QA doctrine (SS-A44) already rejected the wrong abstraction**
  (holistic vote panels) in favor of the right one (binary, reproducible, hash-cached checks).
  This RFC is downstream of that decision, not a reversal of it.

## 3. Brick 1 — The Verification Context Provider (highest leverage)

**Problem it closes:** incidents #1, #2, #3 above, and every future instance of the same class.

**What it is:** one service, `VerificationContextService` (or fold into `DocContextService` as a
sibling read path — needs a design call, not assumed here), that answers three questions any
check service needs before judging a beat:

1. *Who is narrating this beat, and what's their on-file voice?* (`BeatEntityPresence` POV lookup
   + `Characters.SpeechVocabulary`/`Role` — currently duplicated ad hoc in both
   `ProseWriterRouter` and `BeatChecklistGateService.GetPovVoiceHintAsync`; should exist once)
2. *What's this book's own register, statistically?* (the per-book outlier baseline pattern
   already correctly shared via `SemanticFidelityService.IsIntentOutlier` between two services —
   proof the pattern works when it *is* shared; extend it to every future score-vs-threshold
   check instead of re-deriving per service)
3. *Is there a standing author ruling that pre-empts this class of finding for this book/beat?*
   (new — there is currently no way to say "BLST's terse register is intentional, stop flagging
   it" once, globally; every session re-litigates it by re-reading the same beats)

**Deliverable shape:** a single interface every `*Service` that produces a Finding calls before
filing one — not a suggestion convention, an actual dependency. New check services get this for
free; existing ones (`BeatChecklistGateService`, `BeatVerificationService`,
`SemanticFidelityService`) get migrated one at a time, each migration a small, reviewable diff
that *deletes* their bespoke local version of the same lookup.

**Acceptance test:** `GetPovVoiceHintAsync`-shaped code exists in exactly one place in the
codebase, not two.

## 4. Brick 2 — Universal staleness contract, at the `FindingsService` layer

**Problem it closes:** incident #4, and the fact that `BeatChecklistResult.RuleSetVersion` and
`BeatVerification.RuleVersion` are two independent, hand-rolled implementations of the identical
idea, each requiring its own staleness query and its own CLI flag.

**What it is:** move the version marker up to `FindingsService` itself — every category, not just
the two that happened to get bitten this session. A `Findings.SourceRuleVersion` column (or
equivalent), stamped generically by `Upsert`, with **one** `--staleness-report [--category X]
[--all]` command that answers "what's stale" for *any* current or future check, not a bespoke
implementation per table. `BeatChecklistResult`/`BeatVerification`'s existing columns can either
feed this or be superseded by it — a design call for whoever picks this up, not pre-decided here.

**Acceptance test:** adding a new check category never requires writing a new staleness query.

## 5. Brick 3 — Formal degraded-mode contract for provider outages

**Problem it closes:** the standing Anthropic credit exhaustion has silently blocked prose
generation AND `BeatChecklistGateService` re-evaluation for an unknown-but-long window. The
codebase already has the *right instinct* in places (`AffectBehaviorService`'s own comment: "Must
NOT swallow this into a fake-successful LensResult... a total LLM outage read as clean, no issues
found") — but it's a comment on one service, not an enforced contract.

**What it is:**
1. An explicit table (in docs, not just tribal knowledge) of every service by provider
   dependency: Anthropic-only, OpenAI-only, either, or none.
2. One shared test pattern — a fake `ILlmService` that always throws — run against every
   LLM-dependent service, asserting the failure surfaces as "not evaluated" (visibly, in whatever
   the caller reads) and never as a false Pass or a false Clean.
3. A single, visible "degraded services" status the CLI can report on demand
   (`--provider-status`), so a future session doesn't have to rediscover "oh right, credits are
   still out" by tripping over it mid-task the way this session's predecessor did.

**Acceptance test:** every LLM-calling service has a "provider down" test; the CLI can answer
"what can't run right now" in one command.

## 6. Brick 4 — Prove genre-agnosticism, don't assume it

**Problem it closes:** an open, never-confirmed hypothesis sitting in this session's own memory:
nonfiction books (1381, the Gospels, NEPH) score meaningfully healthier (SII 21-82) than every
fiction book (near-universal 0). The working theory was "nonfiction doesn't trigger
craft/voice-specific categories as heavily" — which, if true, doesn't mean nonfiction is
*better-written*, it means **the engine isn't actually applying full rigor to it**. That's the
opposite of "any genre," even though the data currently reads as a success story.

**What it is:** deliberately audit whether nonfiction content is under-instrumented, not just
under-flagged. Concretely: pick one nonfiction beat and one fiction beat of comparable length, run
both beats through *every* CraftChecklist DON'T/DO rule, and confirm the LOW finding rate for
nonfiction is because the rules genuinely don't apply (a Gospel chapter has no "cognitive
architecture tics" to speak of) rather than because the evaluator silently treats nonfiction
differently.

**Acceptance test:** a written finding — "nonfiction's low finding rate is [confirmed genuine /
found to be under-instrumented, fix filed as issue N]" — replacing the current "not independently
confirmed" hedge.

## 7. Brick 5 — Prove scale-agnosticism at both ends

**Problem it closes:** most of this session's evidence comes from mid-size-to-large books (BLST
339 beats, TLC 757, VIGL 318). The engine has never been deliberately stress-tested at the
extremes this RFC's "any scale" promises.

**What it is:** two small, cheap proof passes, not new features:
1. **Tiny end:** run the full pipeline (blueprint → generation → verification → audit) on a
   genuinely 1-2 chapter standalone from scratch, confirm every stage that assumes "a book has
   many beats" (the outlier-gate's `IntentOutlierMinSample = 15` fallback-to-floor-only behavior
   is a documented example of a stage that already handles this — find the ones that don't).
2. **Large end:** confirm `--audit-book`/`--craft-checklist`/`--verify-book` all complete in
   reasonable time and without truncation on the largest existing book (currently TLC at 757
   beats or BCODA at 515) — this session already found and fixed one truncation-shaped bug
   (`generate_node_doc` truncation, `BeatChecklistGateService`'s `maxChars` doubling for
   multi-chapter nodes) proving this class of bug is real, not hypothetical.

**Acceptance test:** a documented pass/fail per pipeline stage at both size extremes, not an
assumption that "it probably scales."

## 8. Sequencing and what this RFC deliberately does NOT try to fix

**Order:** Brick 1 → Brick 2 → Brick 3 → Brick 4 → Brick 5. Bricks 1-2 are pure infrastructure
(no new user-visible behavior, pure de-duplication and one new report) and should ship first
precisely because they make every subsequent brick, and every future one-off fix, cheaper and
less likely to repeat this session's pattern. Bricks 3-5 are proof/audit work — they may surface
their own new bricks, which is expected and fine.

**Explicitly out of scope for this RFC** (real work, but a different axis than "architecture that
prevents repeat bugs"):
- Clearing the existing Findings backlog (IxS's ~1900 CraftChecklist findings, VIGL's empty
  Descriptions, etc.) — that's editorial/content work, gated on LLM credits for a large chunk of
  it, and doesn't need new architecture to proceed once credits return.
- Any new craft doctrine (DELIGHT.md moves, CRAFT.md DON'Ts) — this RFC is about how checks find
  and use context, not what the checks say.
- The retired review/vote panel machinery (SS-A44) — dead, staying dead.

## 9. The rule this RFC asks the project to adopt going forward

**Any new Finding-producing service must consult the Verification Context Provider (Brick 1)
before filing a Finding, and must stamp a rule version (Brick 2) on every row it writes.** This
is the concrete, enforceable version of "stop trying random fixes" — it converts "did I remember
to think about register/staleness this time" from a matter of individual diligence (which failed
four times in one session, for a genuinely careful pass) into a structural property the next
service gets for free.
