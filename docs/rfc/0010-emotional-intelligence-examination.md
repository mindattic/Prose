---
codex: 1
project: Prose
code: SS
layer: rfc
status: draft
updated: 2026-06-23
---

# RFC 0010 — Emotional Intelligence Examination {#SS-RFC-0010}

> Status: **draft / approved-design, pre-implementation** · Author: emotional-depth pass 2026-06-23 ·
> Supersedes nothing; extends the structural diagnostic and review subsystem and is grounded in
> [REGISTER: CODA](../registers/CODA.md) and the per-strand bibles.

## 0. Problem

The author's diagnosis: *"the prose are great but they are still surface level events, the nuances of
the human experience are 'lost like tears in rain' — formulate a system of emotional intelligence
examination that allows for deeper levels of subtext."*

The root cause, confirmed in code: Prose's emotional examination today is **binary**. In
`StructuralDiagnosticService.cs`, the five checks that touch emotion — `DialogueSubtext`,
`StakesEmbodiment`, `CharacterEmbodiment`, `AffectationLines`, `PacingGearChange` — each return a
single `Pass/Warn/Fail` plus one quoted line, at whole-strand granularity, with **no character
model**. They can tell you subtext is present or absent; they cannot measure *depth*, cannot localize
*which beat* goes flat, and don't know any character's want-vs-need or what a character won't say.
Meanwhile the *craft* of deep subtext is richly specified — but only in prose docs
([CODA.md](../registers/CODA.md): tenderness-as-objects, register-shift-as-instrument, the unsaid;
the strand bibles: per-character Will/Flaw/Wound/Want/Need). Nothing operationalizes that craft into a
graded, per-beat, character-aware examination.

**Outcome:** a new `EmotionalDepthService` that scores prose against an 8-dimension, 0–4 emotional
rubric — per beat, character-aware (via a Want/Need/Wound ledger read from the strand bible), and
register-adaptive — then emits beat-scoped, craft-specific fixes that feed directly into the existing
beat-rewrite path to actually deepen flat beats.

### Author decisions (confirmed)

1. **Advisory cap at Deep tier** — the emotional score sits beside `Strand.Score` and files Findings;
   at the Deep/publish gate a strand cannot be marked publish-ready while a *blocking* emotional
   dimension is open. It does NOT alter the 82/85 reader-panel math.
2. **Register-adaptive** — read each strand's declared register from its bible; apply CODA-grade
   anchors to CODA strands, soften register-specific dimensions for JOY/SORROW/Fantasy.
3. **Examination + guided revision** — build the full examiner AND wire weakest-beat fixes into the
   existing beat-rewrite path.
4. **Blocking pair** — only `WantNeedDivergence` and `CostFeltNotAsserted` raise blocking findings
   (the two most diagnostic of "surface-level events"); the other six score and advise.

This mirrors the existing `StructuralDiagnosticService` pattern throughout (parallel low-temp LLM
calls, one JSON object each, `FindingsService.Upsert`, typed result records).

## 1. The rubric — 8 dimensions, scored 0–4

Each dimension is **one parallel LLM call** (temp 0.1, ~600 tokens), grounded in a specific CODA law /
bible field so the model judges against the craft, not generic "emotion". Each returns: a 0–4 score,
the strongest on-page evidence (quote), the weakest moment (quote + beat number), and a beat-scoped,
character-aware fix.

| # | Dimension | Examines | Grounding |
|---|---|---|---|
| 1 | **WantNeedDivergence** *(blocking)* | Is the gap between on-page Want and arc-revealed Need dramatized, or collapsed/absent? | bible Will-vs-Flaw, Want/Need |
| 2 | **TheUnsaid** | Meaning carried by the withheld / silence / white space, or every feeling named? | CODA "white space does the mourning" |
| 3 | **ObjectsAndGestures** | Tenderness/cost arriving as objects & physical acts vs stated feeling? | CODA "tenderness arrives as objects, never statements" |
| 4 | **RegisterShiftAsInstrument** *(register-adaptive)* | Temperature shifts on cue (CODA: warm→cold; others: purposeful tonal modulation), or one flat tone? | CODA "the register SHIFT is the instrument" |
| 5 | **EarnedInteriority** | Rare, load-bearing interior lines vs spammed/wise/subtext-explaining interiority? | CODA "one flat interior line"; RULE ZERO "narrator is never wise" |
| 6 | **RelationalSubtext** | Power/evasion/approach-and-retreat in dialogue vs pure information exchange? | CODA Kyle↔Pixel; bible relationships |
| 7 | **CostFeltNotAsserted** *(blocking)* | Price of wins *felt* (calories, years, wound ledger) vs asserted ("it mattered")? | CODA "every win PRICED" |
| 8 | **ContradictionAndAmbivalence** | The gap between what a character feels and what they show, vs emotionally one-note? | bible Flaw; contradiction |

**Anchored 0–4 scale** (one rubric, in every dimension's system prompt): **0 Absent** (flat/stated) ·
**1 Asserted** (told, reads as a label) · **2 Mixed** (inconsistent) · **3 Embodied** (working through
behavior/object/silence) · **4 Instrument** (the dimension IS doing the emotional work, at *Full
Freight / One Shoe* exemplar grade).

Dimensions are declared in a static table, so adding a 9th/10th later is a one-line edit.

**Register-adaptivity:** the strand's register is read from its bible (`Strand.StrandBible`; the
CODA/JOY/SORROW/Fantasy marker). For non-CODA strands, dimension 4's prompt softens "warm→cold on
cue" to "purposeful tonal modulation", and the 0–4 anchors drop the CODA-exemplar reference. A
register field is threaded into the dimension prompts.

## 2. Per-beat granularity + per-character Emotional Ledger

**Two-pass examination, honoring `ReviewEffortProfile`:**
- **Pass 1 — strand-level dimension scan** (8 parallel calls): always runs (all tiers). Each
  dimension scores the assembled strand 0–4 with strongest/weakest evidence + fix.
- **Pass 2 — per-beat emotional curve** (1 study-style call, Standard/Deep only): rates every beat
  0–4 for emotional depth *in context*, so the author sees exactly which beats go flat. Reuses the
  proven `RecomputeScoresAsync` positional→`SortKey` mapping and the `maxTok = 900 + beatCount*6`
  budget formula from `StrandReviewService`. Writes a **new, separate `Beat.EmotionalScore` column** —
  never overwrites the reader-panel `Beat.Score` (keeps `RecomputeScoresAsync` uncorrupted).

**Per-character Emotional Ledger** (what makes it character-aware): a cached `(StrandId, Character)`
record of Want / Need / Wound / Flaw / VoiceRegister, **parsed from the strand bible**
(`Strand.StrandBible`; bibles encode these in a stable heading shape — verified in TVYT.md §63-93)
with one LLM extraction call, cached and cache-busted on a bible content hash (mirroring
`StrandSpineVersion.BibleHash`). Inferred-from-prose fallback (flagged `Inferred=true`) when no bible
exists. Injected into every dimension prompt as a `BuildWhoBlock`-style preamble. **No
`character.schema.json` change** — the bibles stay the living source.

## 3. Service shape & composition

New standalone **`EmotionalDepthService`** mirroring `StructuralDiagnosticService` (rejected: bloating
the 12 diagnostic checks; a new ballot mode in the ~1300-line `StrandReviewService` whose scores feed
the gate). Runs in the same pre-flight slot as the structural diagnostic, before the persona panel.

Result record types (in `EmotionalDepthService.cs`):

```csharp
enum EmotionalDimension { WantNeedDivergence, TheUnsaid, ObjectsAndGestures,
    RegisterShiftAsInstrument, EarnedInteriority, RelationalSubtext,
    CostFeltNotAsserted, ContradictionAndAmbivalence }

record DimensionResult(EmotionalDimension Dimension, string Name, string Description,
    int Score /*0-4*/, string StrongestEvidence, string WeakestEvidence,
    int? WeakestBeatNumber, string Fix, string CraftLaw, bool IsBlocking);

record BeatEmotionalScore(int BeatNumber, int Depth /*0-4*/, string? Note);
record CharacterEmotionalLedger(string Character, string Want, string Need,
    string Wound, string Flaw, string VoiceRegister, bool Inferred);

record EmotionalExaminationResult(Guid StrandId, string Slug, string Title,
    double EmotionalDepthScore /*0-100 = mean(dim/4)*100*/, string Register,
    IReadOnlyList<DimensionResult> Dimensions, IReadOnlyList<BeatEmotionalScore> BeatCurve,
    IReadOnlyList<CharacterEmotionalLedger> Ledgers, int BlockingCount, string Recommendation);
```

Entry points `ExamineStrandAsync(strandId, effort, maxChars, ct)` / `ExamineTextAsync(...)`;
per-dimension runner mirrors `RunCheckAsync` (temp 0.1, `ExtractJson`, try/catch → neutral default on
parse failure). Effort tiers: Draft = Pass 1 (cheap models); Standard = Pass 1 + beat curve; Deep =
both + ledger refresh + weakest-moment fixes.

**Advisory cap:** the publish-readiness check at the Deep gate consults open blocking emotional
Findings; a strand with an unresolved blocking dimension cannot be marked publish-ready. The 82/85
`Strand.Score` math is untouched.

## 4. Persistence & Findings

Four new plain (non-temporal, append-only) tables via the project's **raw idempotent SQL** pattern
(not EF Migrations), mirroring the `StrandReview / StrandReviewBeatScore / StrandReviewSummary` trio:

- **`EmotionalExaminations`** (parent): `Id` uuidv7, `StrandId` (FK cascade), `EffortTier`,
  `EmotionalDepthScore` float, `Register`, `ContentHash` (sha-256 of assembled beats, for staleness),
  `BeatCount`, `BlockingCount`, `Model`, `ExaminedAt`, `CreatedAt`. Index `(StrandId, ExaminedAt)`.
- **`EmotionalDimensionResults`** (cascade child): `ExaminationId`, `Dimension`, `Score`,
  `StrongestEvidence`, `WeakestEvidence`, `WeakestBeatNumber` int?, `Fix`, `CraftLaw`, `IsBlocking`.
  PK `(ExaminationId, Dimension)`.
- **`EmotionalBeatScores`** (cascade child): `ExaminationId`, `BeatNumber`, `Depth`, `Note`.
  PK `(ExaminationId, BeatNumber)`.
- **`CharacterEmotionalLedgers`** (cache): `Id`, `StrandId`, `Character`, `Want`, `Need`, `Wound`,
  `Flaw`, `VoiceRegister`, `Inferred`, `SourceBibleHash`, `UpdatedAt`. Unique `(StrandId, Character)`.
- **New column** `Beat.EmotionalScore` (float?, guarded `IF COL_LENGTH(...) IS NULL`; Beats is
  temporal, so the SYSTEM_VERSIONING off/alter/on dance is required, per the `--strand-bible`
  migration precedent).

**Findings:** each dimension with `Score <= 1` and `IsBlocking=true` is filed via existing
`FindingsService.Upsert` — `filePath: "strand:{slug}"`, `category: Other`, `severity: High (score 0) /
Medium (1)`, `summary: "EMOTIONAL-DEPTH [{Name}] beat {N}: {Fix}"`, `snippet: WeakestEvidence`,
`suggestedFix: Fix`. Surfaces at `/findings` beside structural failures — one worklist.

## 5. CLI + MCP + DI surface

- **CLI:** `prose --examine-emotion --slug <slug> [--effort draft|standard|deep] [--json]` — new
  `ExamineEmotionCli.cs` (clone `DiagnoseStrandCli.cs`); dispatch in `Program.cs` next to the
  `--diagnose-strand` block (line ~1398). Optional `prose --emotional-ledger --slug <slug>` to
  print/refresh ledgers. Schema migration flag `prose --migrate-sql --emotional-examination` in
  `MigrateSqlCli.cs`.
- **MCP:** `examine_emotional_depth(strandIdOrSlug, effort="standard", maxChars=40000)` —
  `[McpServerTool]` method in `Tools.Quality.cs` next to `DiagnoseStrand` (inject
  `EmotionalDepthService`).
- **DI:** register `EmotionalDepthService` + `EmotionalLedgerService` in
  `ServiceCollectionExtensions.cs` beside `StructuralDiagnosticService` (line ~763).

## 6. Feedback loop into prose (guided revision)

Findings are phrased as **beat-scoped craft directives**, never "add more emotion". Fix template
enforced in each dimension prompt:

> `Beat {N}: {Character} {Want} but {observable behavior}. Replace the stated "{quote}" with a {move
> from CraftLaw} — e.g. {concrete object/gesture/silence} — so the {Need/cost/subtext} lands without
> being named.`

Example: *"Beat 9: Kyle says he's worried. Cut the line; have him set the better cup down handle-first
instead (CODA law 8)."*

**v1 revision wiring:** a `prose --deepen-emotion --slug <slug> [--apply]` verb runs the examination and,
for each weakest beat with a fix, generates a rewrite via the existing single-beat generation path
(`BeatGeneratorService.GenerateBeatAsync` + `StrandWorkbenchService.UpdateBeatTextAsync`, the same
path `ExpandBeatCli` uses), feeding the dimension `Fix` + the character ledger as the rewrite
instruction. Dry-run by default (prints proposed rewrites); `--apply` commits them. After rewrite,
re-examine the beat and confirm its `EmotionalScore` rises and the Finding clears.

## 7. Docs / Codex obligations

1. `docs/AMENDMENTS.md` — append **SS-A15 — Emotional Intelligence Examination system** referencing
   this RFC. Cite CODA.md as craft authority.
2. `docs/USER_STORIES.md` — add epic + acceptance criteria; each `✅` names test evidence.
3. `docs/BIBLE.md` §10 — add the invariant "emotional depth is a side-car score with a Deep-tier
   advisory cap; it never folds into the 82/85 headline gate", reference SS-A15.
4. `pwsh tools/codex.ps1 digest` then `pwsh tools/codex.ps1 doctor` — **doctor must pass**.

## 8. Ordered implementation steps

1. **Docs first (SS-A15)** — `AMENDMENTS.md`, `USER_STORIES.md`, `BIBLE.md` §10; `codex digest && doctor`.
2. **Schema** — SQL reference file + `--emotional-examination` flag in `MigrateSqlCli.cs` (4 tables +
   `Beat.EmotionalScore`).
3. **Entities** — 4 entity classes + DbSets + `b.Entity<>` configs in `ProseDbContext.cs`
   (mirror `StrandReviewBeatScore` ~line 537); add `Beat.EmotionalScore` property.
4. **Ledger service** — `EmotionalLedgerService.cs` (parse bible → cache, infer fallback, bible-hash
   cache-bust).
5. **Examination service** — `EmotionalDepthService.cs` (8 dimension prompts + 0–4 rubric,
   register-adaptive, parallel runner, per-beat curve, Findings upsert, persistence).
6. **DI** — register both services.
7. **CLI** — `ExamineEmotionCli.cs` (+ optional `EmotionalLedgerCli.cs`); dispatch in `Program.cs`.
8. **MCP** — `examine_emotional_depth` in `Tools.Quality.cs`.
9. **Advisory cap** — wire blocking findings into the Deep publish-readiness check (do NOT touch
   `RecomputeScoresAsync`).
10. **Writer surface (optional)** — EmotionalDepthScore + per-beat heat strip in `Strand.razor`.
11. **Revision loop v1** — `--deepen-emotion` over the existing beat-rewrite path.

## 9. Verification (end-to-end)

1. `pwsh tools/codex.ps1 doctor` passes after SS-A15 docs edits.
2. `prose --migrate-sql --emotional-examination` — re-runnable, no error on 2nd run.
3. Run against a CODA chapter (e.g. `part-i-teeth-019e9fb2`) and a non-CODA strand (TVYT, or the GIW
   Fantasy strand): `prose --examine-emotion --slug <slug> --effort deep --json`. Expect 8 dimension
   scores 0–4, strongest/weakest quotes, a beat-curve covering every beat, character ledgers with
   Want≠Need, the correct `Register`, ≥1 Finding for any ≤1 blocking dimension. Confirm the non-CODA
   run softens dimension 4 rather than penalizing it.
4. `prose --findings` shows `EMOTIONAL-DEPTH` findings beside structural ones.
5. Ledger sanity: Rhea Want="keep facts correct / not be managed", Need="stop calling being-managed
   competence" (matches TVYT.md §71-73).
6. Regression: `Strand.Score` and the 82/85 gates unchanged; a Deep publish-readiness check is blocked
   while a blocking emotional dimension is open and clears when resolved.
7. MCP smoke: `examine_emotional_depth("<slug>")` returns the same envelope.
8. Take the worst-beat fix, run `--deepen-emotion --apply`, re-examine; confirm that beat's
   `EmotionalScore` rises and the Finding clears.

## 10. Residual risk

- Dimension scoring is LLM-judgment at temp 0.1; like the structural checks it is advisory signal,
  not ground truth — the advisory cap (not a hard gate) is deliberately conservative for this reason.
- Register detection depends on the bible declaring its register; strands without a clear register
  marker default to the softened (non-CODA) anchors.
- Ledger extraction quality depends on bible heading consistency; the `Inferred` flag makes
  prose-fallback ledgers visible so they can be distrusted.
