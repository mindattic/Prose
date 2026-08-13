---
codex: 1
project: Prose
layer: reference
status: live
updated: 2026-08-13
---

# The sciences Prose actually uses — who, what, why {#SS-SCIENCES}

Prose leans on a handful of named literary-craft frameworks and one piece of empirical research
on AI-fiction detection. Each one showed up in code comments as jargon with no explanation of
where it came from or why it's there. This doc is the "who and why" — one entry per framework,
which service implements it, and (where this session's readability/consistency pass touched it)
what changed.

This is a reference doc, not injected into any generation prompt — for that, see
[CRAFT.md](CRAFT.md) (sentence-level clarity doctrine), [DELIGHT.md](DELIGHT.md) (reader-loved
prose moves), and [LOGIC.md](LOGIC.md) (the six-dimension consistency sweep).

---

## Dwight Swain — Scene/Sequel structure

*Techniques of the Selling Writer* (1965). A commercial-fiction craft manual whose central idea
is that every dramatic unit is one of two shapes:

- **Scene** (active): the POV character has a GOAL → something CONFLICTS with it → the beat ends
  in DISASTER or complication (the character does not fully succeed).
- **Sequel** (reactive): emotional REACTION to the prior disaster → a DILEMMA (two bad options) →
  a DECISION that sets the next direction.

**Where it lives:** the generation instruction in `Prose.Core/Services/BeatGeneratorService.cs`
(the model is told to write each beat as a Scene or Sequel, free — no extra LLM call). Audited
after the fact by `Prose.Core/Services/SwainAuditService.cs` (one cheap Haiku classification call
per beat, FULL tier only) — classifies each beat Scene/Sequel/Ambiguous/Deficient and flags beats
that execute neither pattern.

**2026-08-13 note:** investigated as part of the cost-reduction pass under the hypothesis that
Swain compliance was being paid for three times. It wasn't — the generation instruction is free,
the audit is one cheap pass, and `BeatAuditService`'s three chapter-close lenses (often confused
with Swain because they also run at chapter-close) check unrelated properties (causality,
emotion-drives-action, interpersonal dynamics). No redundancy found; nothing changed.

## Will Storr — The Science of Storytelling

*The Science of Storytelling* (2019). Reframes story craft through psychology/neuroscience
research on what actually holds a reader's attention. Concepts in use:

- **Sacred Flaw / theory of control** — a character's core false belief about what keeps them
  safe, powerful, or loved. The lens through which they misread every event.
- **Dramatic Question** — "who is this person really?", operating at both the surface (plot) and
  subconscious (character-revealing) level simultaneously.
- **Five-Act structure** — Flaw Enthroned → Ignition Point → Flaw Tested → Midpoint Commitment →
  Worst Case Realised → God Moment — a character-change arc, not a plot outline.
- **Antihero empathy levers** — pre-deflation, vulnerability, genuine virtue, altruistic
  punishment: the four things that make a reader root for someone doing bad things.
- **6-point scene anatomy** — unexpected change, information gap, cause-effect, tribal emotion,
  specificity, show-not-tell.

**Where it lives:** `Prose.Core/Services/NarrativeScienceService.cs` implements all of the above
as callable analyzers. `MapFiveActStructureAsync` (1 call/book) and `AnalyzeSacredFlawAsync` (1
call/POV character) are wired into `BookHealthService`'s FULL tier. `CheckDramaticQuestionAsync`
is genuinely per-beat by design — Storr's framework asks the question of every beat, not the book
as a whole — and stays in FULL tier for that reason. `Prose.Core/Services/StoryScienceService.cs`
also draws on Storr for its per-beat status-dynamics, curiosity-gap, and theory-of-mind guidance
(all free — no LLM call, pure prompt text).

**2026-08-13 note:** the 6-point scene anatomy (`AuditSceneEngagementAsync` / "scene-anatomy") was
**removed** — it overlapped LogicSweep's causality dimension, DELIGHT's moves, and StoryScope
(below), and had no automated caller anywhere, only a manual bulk CLI/MCP surface that could fire
one LLM call per beat across a whole book for signal already covered elsewhere.

## Rudolf Flesch — Flesch Reading Ease / Flesch-Kincaid Grade Level

A 1940s readability formula, still an industry and plain-language-law standard (Microsoft Word,
journalism style guides, US federal plain-writing requirements all use it). Scores text 0–100 from
average sentence length and syllables per word — higher score, easier to read. Standard bands:
90–100 fifth grade, 70–80 seventh, **60–70 eighth–ninth (plain commercial fiction)**, 50–60
tenth–twelfth, 30–50 college, 0–30 college graduate.

**Where it lives:** `Prose.Core/Services/BeatProseMetricsService.cs` computes Flesch Reading Ease,
Flesch-Kincaid Grade, type-token ratio, and lexical diversity (MTLD) per beat. Pure CPU, zero LLM
cost, safe to run on every beat.

**2026-08-13 note:** this was the missing piece behind the "prose is too dense" complaint. The
score had existed for a while but only ever reached a CLI/dashboard report — nothing fed it back
into generation. The outlier floor was also miscalibrated (40, roughly college difficulty) against
the actual target ("a bright high-school freshman reads it once, no re-reading" — CRAFT.md §0,
which lands around 60–70). Fixed: floor raised to 50 for reporting, a separate 40 floor now
triggers an immediate per-beat rewrite (`AutoRunCli`'s repair pass), and recent low-score findings
feed forward into future beats' prompts (`ProseWriterRouter.BuildFindingsGuidanceAsync`, the same
pattern already used for emotional-depth and StoryScope findings).

## McKee / Truby / Vogler — controlling idea

Robert McKee (*Story*), John Truby (*The Anatomy of Story*), Christopher Vogler — the shared
craft idea that a book's theme is a testable, value-laden claim ("true strength is admitting
weakness," not just the topic "strength") that the plot's causal chain of choices proves or
disproves — not a message stated by the narrator.

**Where it lives:** `Prose.Core/Services/ThemeCoherenceService.cs`. One LLM call per book (Seed +
Bible + opening/closing beats, not a per-beat scan): infers the controlling idea, flags theme
stated as commentary instead of dramatized through consequence, flags an ending that doesn't
engage the opening's value-question. Wired into `BookHealthService`'s DEEP tier.

## Chekhov's Gun — setup and payoff

The classical principle that a concrete detail introduced in fiction must earn its place or be
cut — "if you say a rifle is on the wall in chapter one, it must be fired by the end."

**Where it lives, two complementary ways:**
- `Prose.Core/Services/PlantPayoffService.cs` — a free, zero-LLM DB ledger of pairs an author (or
  the generation process) explicitly *registered* as a plant/payoff. Wired into `BookHealthService`'s
  FREE tier.
- `Prose.Core/Services/ChekhovAuditService.cs` — an LLM discovery pass (2 calls per book: extraction,
  then verdict) that finds props/details *nobody registered* and judges whether their recurring
  appearances earn their place, are orphaned, or are mere decoration.

These aren't redundant: one tracks what was declared intentional, the other catches what wasn't
declared but reads as a Chekhov's Gun anyway.

## StoryScope — empirical AI-fiction detection

["Lost in Stories" / StoryScope](https://arxiv.org/abs/2604.03136) (University of Maryland /
Google DeepMind, 2025) — a 61,608-story empirical study. Its headline finding: narrative-*structure*
classifiers detect AI-written fiction at 93.2% accuracy **without reading a single word of prose**
— it's the shape of the decisions (flat event escalation, event-type monoculture, narratorial
moral gloss, front-loaded revelation, clean internal-resolution endings) that gives it away, not
the sentence-level style.

**Where it lives:** `Prose.Core/Services/StoryScopeAuditService.cs` — a deterministic layer (zero
LLM cost: blueprint-vs-execution drift, beat-mode run-length, emotional-depth plateaus) plus an
LLM-graded layer (parallel per-check calls: escalation curve, event-type variety, narrator
moral-gloss, embodied-vs-labeled emotion ratio, and more). Findings loop back into future beat
prompts (`ProseWriterRouter`'s STORYSCOPE guidance block) — the audit corrects subsequent writing,
not just reports on past writing. `Prose.Core/Services/StoryScienceService.cs`'s anti-pattern list
also cites specific StoryScope findings directly (e.g. "flat event escalation... the single
strongest AI fiction signal in structural classifier studies").

## The six-dimension Logic Sweep

[docs/LOGIC.md](LOGIC.md) (SS-A44) — this project's own methodology, not borrowed from outside
research, adopted as **LAW** 2026-07-04 (replaced score-panel voting as the default QA mechanism).
Six independent checks over a book's full prose: causality chain, knowledge states, timeline,
plant/payoff (two-way), orphan references, bible agreement.

**Where it lives:** `Prose.Core/Services/Audit/LogicSweepService.cs` — one LLM call per dimension
over the whole node's prose (truncated for an oversized book). Its own doc comment is honest about
scope: this single-prompt version is a coarse, automatable gate; the `/logic-sweep` Claude Code
skill (which splits a big book across range-scoped subagents, then verifies quotes, then triages,
then fixes, then re-verifies) is the thorough version for when it actually matters.

## Wikipedia — "Signs of AI writing"

[en.wikipedia.org/wiki/Wikipedia:Signs_of_AI_writing](https://en.wikipedia.org/wiki/Wikipedia:Signs_of_AI_writing)
— a large, actively-maintained catalog Wikipedia editors built from cataloging thousands of
AI-generated submissions. Documents concrete, testable prose tics: vocabulary clustering ("delve,"
"pivotal," "underscore," "tapestry" — patterns that shift by model and release date), negative
parallelism ("not only X, but Y" / "it's not X, it's Y"), the rule-of-three ("adjective, adjective,
adjective"), copula avoidance ("serves as" instead of "is"), em-dash overuse, and more.

**Where it lives:** `Prose.Core/Services/ProsePatternGuard.cs` — a deterministic, zero-LLM regex
linter that runs synchronously in the write loop on every beat. Already implements a good chunk of
this catalog directly, with its own research citations in the code (`delve` — "+1,300–6,700% since
2023"; the negative-parallelism tic — "the #2 most human-detected AI tell"; em-dash density
against a measured per-100-word threshold). This is the clearest example in the codebase of a
cheap, well-justified, evidence-grounded check — exactly the "discrete scientific part" standard
the rest of the pipeline should be held to.

## Stephen King — craft anti-patterns

*On Writing* (2000). The plain-craft half of `StoryScienceService`'s anti-pattern list, cross-
referenced against Storr: adverbs in dialogue attribution ("said" only), passive voice, physical-
trait shortcuts as character shorthand, wardrobe inventory, clichéd similes, announcing character
state instead of showing it, research/backstory as foreground, theme imposed instead of found in
revision.

**Where it lives:** `Prose.Core/Services/StoryScienceService.cs`'s `KingAntiPatterns` array — free
prompt text, no LLM cost.

**2026-08-13 note:** `KingAntiPatterns[6]` ("announcing character state — 'Annie was angry' is a
loss") used to lose a direct, in-file contradiction to a "one explicit emotion label per scene
earns authority" exception written elsewhere in the same file (`ProseCoreMechanics`,
`StorrAntiPatterns[2]`) — and to `BeatGeneratorService`'s own separate "do not name emotions"
dialogue instruction. All three fired in the same beat's prompt, disagreeing with each other. The
exception was removed from both places it lived; King's rule (matching CRAFT.md §4/§6's "nobody
names a feeling, never named, only shown") is now the one surviving instruction on this topic, and
now fires in every beat mode, not combat-only.
