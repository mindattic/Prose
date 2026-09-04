---
title: The Story Ledger — the engine's memory of what is actually true
status: canonical
adopted: 2026-09-04
---

# The Story Ledger {#SS-LEDGER}

The third methodology doc, peer of [docs/LOGIC.md](LOGIC.md) and
[docs/READER-QA.md](READER-QA.md). The Logic Sweep owns causality/continuity QA; Reader-Proxy QA
owns craft/comprehension QA; **the Story Ledger owns the engine's record of what is true, and the
detection of two facts that cannot both be.**

It is a **cold-ledger instrument only** ([LOGIC.md §10](LOGIC.md#SS-LOGIC-10)). It measures
correctness. It cannot, and must never be asked to, judge whether the book is good.

## 1. The faculty it is {#SS-LEDGER-1}

A storytelling engine needs three faculties. This one had built two:

| Faculty | Instrument |
|---|---|
| **Composition** — write a beat with the right context | DCM, `ProseWriterRouter`, ~25 enrichment services |
| **Judgment** — assess what was written | Logic Sweep, Reader-Proxy QA, StoryScope, publish-readiness |
| **Memory** — a trustworthy record of what is *actually true*, derived from the text and never from a paraphrase of it | **This doc** |

The defect that forced the third: a character's fabricated father (`Dae-jung Seo`) coexisted with
the climax establishing him as a construct with "no before" — across ~290 beats of a finished book
that had passed five clean logic sweeps. It had already spread into a weapon record and onto an
unrelated `she/her` character from a different book. **Every instrument that missed it, missed it
for a mechanically explainable reason**, and none of them were bad luck:

- `LogicSweepService.BuildClampedProse` keeps a book's head 50k and tail 50k above 100k chars and
  **elides the middle**. BCODA is ~1.9M. The sweep saw the father claim (head) and the climax
  (tail); the reveal that reconciles them sat in the elided middle.
- The fact ledger could not **represent** the conflict: `ContinuityService.Upsert` fires only on
  same predicate/different object, and `father` vs `origin` are different predicates. Not missed —
  *unrepresentable*.
- Every other instrument windows or shards (range-scoped sweep subagents, a comprehension probe
  with a three-chapter recap), and none share verified state.
- With no bounded full-fidelity read path, every reader fell back to the one-line
  `Beat.Description` spine — an *intent* field with no binding to the prose at all until Phase 1
  gave it one.

**The common cause: nothing carried verified facts across a whole book at bounded cost.** Every
instrument truncates, windows or shards, then re-derives facts by paraphrase inside its own narrow
view. Paraphrase is where "father" and "mentor" stop looking incompatible.

## 2. What the ledger is {#SS-LEDGER-2}

`ContinuityClaims` — atomic `(EntityId, Predicate, Object)` triples, **widened**, not a new store.
The load-bearing columns beyond the triple:

| Column | Why it matters |
|---|---|
| `Snippet` | The mandatory verbatim span the claim was read from. This is what makes a claim *evidence* rather than an assertion, and it is what [§5](#SS-LEDGER-5)'s anchor backfill matches against. |
| `SourceBeatId` | The exact beat. **See [§5](#SS-LEDGER-5) — this is the instrument's ceiling.** |
| `SourceChapterId` / `SourceChapterNumber` | The older, coarser anchor. A 40-beat chapter and no further. |
| `BookSlug` | Null for entity-record and bible claims, which belong to no single book. |
| `Provenance` | [§4](#SS-LEDGER-4). Did a human approve this, or did a model invent it? |
| `Status` | `NEW` / `CONFIRMED` / `CANONICAL` / `CONTRADICTED` / `REJECTED` / `SUPERSEDED`. |
| `ExclusionRuleId` | Which axiom flagged it, when the contradiction came from the ontology. |

## 3. The two detectors {#SS-LEDGER-3}

### 3.1 Same predicate, different object (`ContinuityService.Upsert`)

The original mechanism. Numeric-safe: `ObjectsMatch` parses number words, so "fifty" and "50"
collapse into one claim and only a genuine arithmetic discrepancy contradicts. Three exemptions,
each answering a different question about the predicate:

| Exemption | Question it answers | Examples |
|---|---|---|
| **Volatile** (`IsVolatilePredicate`) | Does this value change as the story moves? | `location_current`, `carrying`, `mood`, `companions` |
| **Set-valued** (`IsSetValuedPredicate`, 2026-09-04) | Can this subject have many values *at once*? | `ability`, `action*`, `knowledge*`, `possession*`, `skill`, `relationship*` |
| **Same assertion** (`IsSameAssertion`) | Is this one fact recorded twice in different words? | "rebuilds bike" vs "rebuilt the bike" |

**Cardinality is not volatility, and conflating them cost real signal.** A volatile predicate has
one value that changes over time; a set-valued predicate has many values that are all true
simultaneously. `action` is both; `ability` is only the second, which is why one list could not
cover both. Measured across the 1,316 live contradiction groups in BCODA/DWIACE/VATD before the
fix: **250 groups (950 rows) were pure cardinality** — a character with two abilities filed as a
contradiction — and ~300 more were pure paraphrase. Corpus-wide, re-assessing the 3,776
`CONTRADICTED` rows under the corrected rules cleared **964 of them (26%)**.

**A status is written once and never revisited**, so every correction to these rules leaves the
existing rows stating a verdict the engine has already repudiated.
`prose --continuity reassess [--apply]` re-runs today's test over the `CONTRADICTED` population
and moves the unsupported ones back to `NEW` — deterministic, free, reversible, and the claims
themselves are untouched.

**What survives the exemptions still is not a defect list.** Reading a sample showed it is
dominated by **complementary facets** (`augmentation_type`: "four-armed combat specialist" against
"quadrupedal with four arms") and **temporal states** (`beacon_status`: "live" against "dormant
for 3 months, recovered"). Telling those from a genuine conflict is a judgement about the story,
and no string rule loose enough to merge them is tight enough to be safe — a rule that merges
complementary values is a rule that hides the next fabricated fact. So the discrimination is
bought rather than guessed: **`prose --ledger-adjudicate --slug <s>`** puts one narrow Sonnet call
per group in front of the prose each value was read from, behind the same quote gate as everything
else here. Compatible groups are cleared back to `NEW`; real conflicts keep their status and file a
`LEDGER-CONFLICT ` finding. **~$0.03 per group**, cached on the claim uids plus every anchor beat's
current `TextHash`, so an interrupted run resumes free. Only affordable at all because
[§5](#SS-LEDGER-5)'s backfill took anchor coverage to 99.1% — an unanchored claim has no prose to
show, and the question cannot be asked.

Measured on BCODA's first 120 groups: **11 real conflicts** — including a character's handedness
recorded as both right and left, one fatal wound placed in two different parts of the body in
consecutive beats, and a contract count stated as both 43 and 140 over the same eleven years.

**The paraphrase threshold is deliberately severe** (subsumption, or ≥0.75 token overlap). The
asymmetry is the reason: a false "same assertion" hides a real contradiction, while a false
contradiction merely costs a triage decision. Complementary facets — "red hair in loose braid" vs
"dark red hair" — are left for a human on purpose; deciding those is an author's call about the
story, not a string comparison.

Its reach is narrow by construction, and [LOGIC.md §3.4](LOGIC.md#SS-LOGIC-3) describes it
accurately as a **numeric drift detector**. It is not, and never was, a general identity-consistency
check.

### 3.2 Different predicate, incompatible meaning (the exclusion ontology)

The gap the Dae-jung Seo defect fell through. `PredicateExclusions` holds disjointness axioms —
declarations that two claim *shapes* cannot both be true of one subject — and
`PredicateExclusionService.GenerateCandidates` pairs claims across predicates.

```
1. EXTRACT    ContinuityExtractionService → claims w/ mandatory exact snippet (hash-gated)
2. COLLIDE    (a) same-predicate/different-object          → candidate
              (b) predicate mutual-exclusion ontology      → candidate
3. COLLAPSE   one representative question per (entity, axiom)
4. ADJUDICATE one narrow Sonnet call per candidate — never on the whole book
5. GROUND     verdict REJECTED unless its quote appears verbatim in the prose supplied
6. FILE       FindingsService → "TUNEDREAD " findings, delete-then-recreate per book
```

**Cost scales with the number of collisions, not with book length.** That is what makes it
runnable across a 46-book corpus where the clamped sweep could not be.

**Step 3 is not an optimisation, it is the difference between one call and sixty-five.** The
ledger records the same fact under many predicate names — BCODA's fabricated father appeared under
~13 `father*` predicates and the constructed origin under ~5, a cross product of 65 pairs asking
one question. An axiom is a statement about the *families*, so learning the answer once settles
the group.

**Step 5 is the most important line in the system.** An unquotable assertion about the text is
exactly how "Dae-jung Seo" became canon; the instrument built to catch that must be incapable of
committing it.

## 4. Provenance grades {#SS-LEDGER-4}

One vocabulary across `ContinuityClaims`, `Entities` and `CharacterRelationships` — the question is
identical in each case.

| Grade | Meaning |
|---|---|
| `authored` | A human decided this. The only grade that is canon without qualification. |
| `observed` | Extracted from prose **with a snippet that mechanically verifies** against `Beat.Text`. |
| `inferred` | A model produced it without a verifying quote, or derived it. |
| `scaffolded` | Auto-created by entity scaffolding. **Never canon.** Candidate only. |
| `legacy-unknown` | Pre-existing rows (author ruling: grandfather, then flag the suspicious). |

This makes *"show me everything in canon no human ever approved"* a query
(`prose --provenance-audit`) instead of an archaeology project.

## 5. Beat anchors are the ceiling {#SS-LEDGER-5}

**Read this before trusting any result from the Tuned Read.**

An unanchored claim cannot be adjudicated. `TunedReadService.AdjudicateAsync` **refuses** a pair
where neither claim carries a `SourceBeatId`, because adjudicating without prose means ruling on
two summaries — the paraphrase-only reasoning that invented the defect in the first place. A
temporal axiom ([§6.2](#SS-LEDGER-6)) additionally cannot order claims that have no position on
the book's clock, so it never fires at all.

Measured 2026-09-04: **24 of 24,758 live claims carried a beat anchor — 0.1%.** `SourceBeatId`
arrived in Phase 2; every claim extracted before it was unanchored. So the instrument could not
produce a finding on 99.9% of the ledger no matter how good the ontology was — **and it reported
that silence as a clean corpus.**

The recovery is deterministic and free. Every claim already carries a verbatim `Snippet`, and the
engine already trusts snippet-in-beat containment as its quote-grounding gate;
`prose --continuity anchor-beats` reuses that exact test to recover the anchor each snippet
implies. Corpus-wide it took prose claims from **0.2% to 90.7%** (9,968 rows).

It **fails closed**: a snippet matching two beats in its own chapter is left unanchored rather than
assigned to the first hit. A wrong anchor is worse than none — it would cite innocent prose in a
finding, hand the adjudicator the wrong carrier band, and key the verdict cache to unrelated text.

Two by-products worth reading as signals rather than as backfill misses:

- **Stale snippets** (937 corpus-wide) — the claim's own evidence no longer appears in its chapter.
  The beat was edited, split or deleted since extraction; the ledger is asserting something the
  prose stopped saying.
- **Ambiguous snippets** (81) — short or formulaic spans matching several beats. Re-extraction is
  the fix, never a guess.

`prose --continuity stats` reports anchor coverage for exactly this reason, and says outright when
zero anchors mean a clean result is meaningless.

> **The general lesson, which is not specific to this instrument:** when an audit returns zero
> findings, establish that it was *capable* of a finding before calling the result clean. "Nothing
> wrong" and "could not look" are the same output.

## 6. The resolution gradient (the radio) {#SS-LEDGER-6}

At each adjudication the reader holds three bands. The cost argument is that band 3 is lossless in
facts and free of prose, so it spans a 500-beat book without growing with it.

| Band | Span | Content |
|---|---|---|
| **Carrier** | anchor ± 10 beats | Full verbatim `Beat.Text` |
| **Near sideband** | ± 40 beats | `Beat.EventSummary` — "what happened", hash-gated |
| **Far band** | the whole book | Accumulated ledger claims for the entity. No prose. |

**Use `Beat.EventSummary`, never `Beat.Description`.** `EventSummary` is observational and
hash-gated against `Beat.TextHash`; `Description` is authorial intent, written at ~30 words/beat,
and until Phase 1 had no binding to the prose at all.

### 6.1 Where axioms come from {#SS-LEDGER-6a}

Three sources, in increasing specificity:

1. **Built-in** — logical/biological axioms, true in every universe (`UniverseId = Guid.Empty`).
2. **Canon-declared** — a universe Bible law made machine-readable (GLMZ: *"Iowan Behemoths are
   autonomous machines, NOT synthetic life"*).
3. **Learned** — proposed by the adjudicator after it confirmed a real contradiction, landing as
   `proposed` and generating nothing until a human approves it. This is how the ontology sharpens
   per incident instead of per patch.

**Author the next axiom from evidence, not imagination.** `prose --continuity predicates` reports
the ledger's real predicate vocabulary and `--co-occur` reports which families are actually held
by the same entity — an axiom can only ever fire on a pair that appears there. This is not
pedantry: the Phase 2 axioms named `father` while extraction had written `father_name`,
`father_occupation` and a dozen more, and **a rule that silently matches nothing is
indistinguishable from no rule at all.** Predicate alternatives match by equality (or an anchored
`stem*` family); object patterns match as case-insensitive substrings.

Check a candidate axiom against a hypothetical pair for free before approving it:
`prose --exclusion-rules --test --predicate-a … --object-a … --predicate-b … --object-b …`.

### 6.2 Temporal axioms {#SS-LEDGER-6b}

Some axioms are not statements about two predicates — they are statements about two predicates **in
an order**. *A dead character does not later act* is the canonical one: expressed without the
ordering it fires on every character who dies on the page, because a life that ends mid-book is the
normal shape of a story.

`PredicateExclusion.TemporalOrder = "b_after_a"` means the pair is a question only when B's anchor
sits **strictly later** in reading order than A's. Rules:

- A temporal axiom is **directional** — `Symmetric` is ignored for it, because swapping the sides
  asserts the opposite ordering, which is the opposite axiom.
- Evaluated with no reading-order map available, a temporal rule is **skipped, never treated as
  timeless** — the timeless version is a far broader axiom than the one its author approved.
- The adjudication prompt **overrides its own** "two facts describing different moments are not a
  contradiction" guidance for these, which would otherwise clear every hit by construction.
- Equal positions fail: a death and an action recorded from the same beat is the death scene.

## 7. Runbook {#SS-LEDGER-7}

```powershell
# — populate and maintain —
prose --continuity extract --node <slug>            # opt a book in (LLM cost, per chapter, hash-gated)
prose --continuity anchor-beats [--slug <s>] [--dry] # recover beat anchors — FREE, do this first
prose --continuity stats                            # counts, sources, and ANCHOR COVERAGE

# — survey before authoring an axiom (both free) —
prose --continuity predicates [--slug <s>] [--min N]
prose --continuity predicates --co-occur [--family <f>]

# — the axioms —
prose --exclusion-rules [--all] [--json]
prose --exclusion-rules --test --predicate-a <p> --object-a "…" --predicate-b <p> --object-b "…"
prose --exclusion-rules --propose --predicate-a <p> --predicate-b <p> --why "one sentence"
prose --exclusion-rules --approve --id <n>          # a proposed axiom generates nothing until this

# — the read —
prose --tuned-read --slug <s> --dry                 # FREE: candidate counts + why an axiom was silent
prose --tuned-read --slug <s> [--max-candidates N]  # one Sonnet call per uncached candidate

# — triage —
prose --continuity reassess [--slug <s>] [--apply]   # re-judge CONTRADICTED under today's rules
prose --continuity stale-snippets [--slug <s>] [--by-book] [--supersede --confirm <n>]
prose --continuity search --text "<substring>" [--predicate-prefix <p>] [--live]
prose --continuity groups --slug <s>                # N-way same-predicate groups, with ClaimUids
prose --continuity resolve --a <uid> --b <uid> --winner A|B|custom [--object "…"]
prose --continuity reject --claim <uid> [--note "…"]
prose --provenance-audit [--slug <s>] --universe <u>
prose --fact-ledger-refresh --slug <s>              # re-run just the same-predicate check, free
```

**`--dry` is the one to reach for first.** It runs the entire deterministic half and spends
nothing. A candidate count in the hundreds means an axiom is too broad, and finding that out for
free is the difference between a useful instrument and a bill.

## 8. Where it plugs in {#SS-LEDGER-8}

| Join | Mechanism |
|---|---|
| **Publish-readiness gate 2** | [LOGIC.md §9](LOGIC.md#SS-LOGIC-9) condition 2 reads all three faces: `CONTRADICTED` claim rows (volatile predicates excluded), `FACT-LEDGER ` findings, `TUNEDREAD ` findings. **A book whose ledger was never populated FAILS** — not checked is not checked clean. |
| **The battery** | `tuned-read` is a **FULL**-tier check of `prose --audit-book --full` — cost is per uncached candidate, the same multi-call class as storyscope/swain/chekhov. |
| **Generation loop-back** | `ProseWriterRouter` injects prior `TUNEDREAD` findings as forward guidance, so the beat that would create the next contradiction is written knowing about the last one. |
| **Write gates** | `CharacterRelationshipTargetCheck` rejects a relationship row with an unresolvable or empty target; `UnscopedUniverseWriteCheck` fails closed on an ambiently-scoped write. |
| **Caching** | Adjudication verdicts key on `(claim pair, axiom, both anchor TextHashes)`; extraction is hash-gated per chapter. **An unchanged book costs nothing to re-read** — including the verdicts that came back clean, or a clean book would cost the same as a broken one forever. |

## 9. Honest limits {#SS-LEDGER-9}

- **The ontology catches only contradictions someone declared.** It will not catch everything and
  must not be sold as if it will. Its value is converting a class of currently-undetectable defects
  into detectable ones, and getting better per incident rather than per patch.
- **It is report-only, by law** ([LOGIC.md §4](LOGIC.md#SS-LOGIC-4)). Findings deliberately carry
  no `Snippet`/`SuggestedFix` pair, so no apply path can splice a machine "fix" over prose. Content
  defects are fixed one beat at a time by hand.
- **It is not a felt pass** ([LOGIC.md §10](LOGIC.md#SS-LOGIC-10)). Every claim can reconcile while
  the book reads dead.
- **It is not a vote.** A measurement, outside the SS-A44 VotingGate, same exemption as the logic
  sweep and craft checklist.
- **Entity-record and bible claims carry no beat**, so a cross-source pair (a prose claim against
  an entity-record claim) is adjudicable only from the prose side.
