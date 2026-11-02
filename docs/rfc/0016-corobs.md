---
codex: 1
project: Prose
code: SS
layer: rfc
status: proposed
updated: 2026-11-02
---

# RFC 0016 — COROBS: the world as geometry (Correlithm Object technology for the Prose factory)

**Status:** PROPOSED to the author 2026-11-02 (tickets PRS-T863 store, PRS-T864 living world, PRS-T865 bench, PRS-T866 grader). Becomes a root when the author approves it; from then on the live build state is the factory (`prose --order list`), not this file, and this document changes only through an engine work order under its own root.

> COROBS — COrrelation + algoRithm OBjectS — is the author's name for a new, persistent, parallel way of storing how the entities of a universe relate. It is built on Correlithm Object (CO) technology (P. Nick Lawrence, *Correlithm Object Technology*, 2004). The resonance is deliberate: a series about SuperMinds breaking into higher dimensionality is written, balanced, measured, edited and synchronized by a science of high-dimensional organization of data.

## 0. Context

RFC 0015 made the book its beats and the world its entities, and it works: 23 GLMZ books through the line, every change a logged Hub call. What it does not do is make the world **live**. An entity is a record. A place knows who frequents it; a character knows their turf; but no instrument asks whether the two agree, and nothing pulls a shopper into the crossfire happening on the next block at the same story-hour. The author's asks (2026-11-01/02):

- *"Every person, place and thing lives in this world and each informs the other … at any moment a group of people could suddenly be a part of something: a crossfire of rival gangs, an Arcturus round-up of dissenters, a robot got mad, a Schism opening up and turning a building inside out."*
- *"By learning about the people and places some universal truths will be found, and those truths will cause contradictions to surface in entities, which when corrected will surface and heal other contradictions, until every person, place and thing is aligned with every rule that organically emerged."*
- *"Not just the book, the series, the universe — everything is tied to everything, so every change affects everything else forever and continuing through linear time."*
- *"The existing system must remain available but this new way of organizing data is to be freshly minted; don't overwrite any relationships today … so that the OLD WAY and the NEW WAY can be called on."*
- *"Make everything about this new way toggle with `/corobs on|off` so that this massive change can just be toggled off if it doesn't pay off."*
- An **empirical** comparison of the old way and the new on interconnectivity, and an empirical way to say whether one **version** of a book, a series or the universe is better than another, *"using every metric ever conceived for grading storytelling."*

The author rejected the book-by-book focus wheel as the wrong technology and asked for **multidimensional logic**.

**What CO technology gives us.** A list of N numbers in [0,1] is a point in a unit N-cube. For N ≥ 20 (we use 64–128) two points chosen at random are almost always the same distance apart — standard distance μ = √(N/6), with a standard deviation σ ≈ √(7/120) ≈ 0.2415 that does not grow with N (Table II-1). Proximity far below μ therefore almost never happens by accident: the probability that two random points lie closer than d is about 10^(−7.45(μ−d)²) (Eq. III-J), and that proximity carries about 24.75·(μ−d)² bits of information (Eq. III-H). So: give every entity a point built from its facts; entities that cluster on some subspace share a truth nobody wrote down; a rule `s.p >: t.q` says "things near pattern p on subspace s land near q on subspace t"; an entity that satisfies the source and misses the target is a **ranked, graded contradiction**; and after a fix the geometry is rebuilt and the next outlier stands out. The book calls this exact role a *cueing system* — directing an expert's attention to the most productive places — and *anomaly detection*. The expert is the agent in session; the law stays deterministic.

**What this RFC is not.** Not an outline, spine, ledger or summary (Law 1). Not an LLM judging prose (Law 7). Not a replacement for Edges, relationships, state events or rulings. Not a tool that edits anything by itself.

## 1. Law

### 1.1 Amendments to standing law
- **PROSE_PROTOCOL Law 1** reads, from approval: *"The book is its beats; the world is its entities **and the geometry among them**. Nothing else is stored about the story — no outline, spine, blueprint, ledger, summary or reconciler."* COROBS tables are the world, not the story.
- **RFC 0015 §2.1/§2.2**: the `Corobs*` tables are added as world tables, additive, one migration (`AddCorobs`). No existing table is altered.
- **RFC 0015 §8** gains a row: *LLM judging in Corobs — none; wiring test asserts no `ILlmService` reference in `MindAttic.Corobs` or `Prose.Core/Services/Corobs`; enforced.*
- **RFC 0015 §7** gains **protocol H** (§5 below).
- **SS-LAW-17** is exercised, not amended: the author's explicit request (2026-11-02) authorizes one advisory reader tier in the version grader (§7), labeled, never gating, behind `--allow-votes`.

### 1.2 Invariants (each is a unit test)
1. No `Corobs*` entity has a foreign key to `Beats`, `BeatNodes`, `Nodes`, `Findings` or `ContinuityClaims`. Geometry references entities and story-time, never pages.
2. The only free-text column in `Corobs*` is `CorobsPattern.Label`, ≤120 characters, written by hand at confirmation. No column holds beat text, synopsis or plot.
3. No `Corobs*` row moves to `confirmed` without a `RulingId`; no `CorobsHit` closes without a ruling or order id, or the `ClosedByCascade` flag set by a rebuild.
4. The library `MindAttic.Corobs` has no `ProjectReference` to any `Prose.*` project and no package reference to an LLM client.
5. With `Corobs:Enabled = false`, every COROBS entry point returns `disabled` and no `Corobs*` row is written or deleted.
6. Same seed, same inputs → identical coordinates, pattern ids and hit ranks (bit-for-bit).
7. Old-way surfaces (`Edges`, `CharacterRelationships`, `PlaceFrequentBy`, `PlaceAdjacency`, `EntityStateEvents`, `BeatEntityPresence`, `--graph-query`, `get_character`) are byte-identical in behaviour with the switch on or off.

### 1.3 Hand first (RESULTS NOT TOOLS)
Two full hand passes over every GLMZ entity (§4) precede any code under §6. The passes produce the baseline numbers the bench (§6.5) compares against and the thresholds the miners (§6.3) start from. The author closes pass 2 before the first engine order under §6 opens.

## 2. Data model (one additive migration, `AddCorobs`)

| Table | Columns | Holds |
|---|---|---|
| `CorobsSpaces` | Id, UniverseId, Seed (long), N (int, default 64), Status, CreatedAt | one geometry per universe; the seed persists here *and* in config — a mismatch refuses to build |
| `CorobsPlies` | Id, SpaceId, Key, ConverterKind (cardinal · ordinal · continuous · string2d · graph · time), SourceFieldsJson, Cells | a named subspace and which record fields / edges / state aspects feed it |
| `CorobsPoints` | SpaceId, EntityId, PlyId, AtStoryTime (nullable), Coords (varbinary float[N]), RecordModifiedAt, EdgesFingerprint, BuiltAt | an entity's position on a ply; static plies one point per entity, state-driven plies one per state change — an entity is a **trajectory through linear time** |
| `CorobsPatterns` | Id, PlyId, Centroid, Radius, MemberCount, AtStoryTime (nullable), Label ≤120, Status (proposed · confirmed · rejected · superseded), ConfirmedRulingId, EventEntityId (nullable) | an emergent truth: a cluster far tighter than chance; or an incident (an `event` entity) as a time-scoped pattern |
| `CorobsRules` | Id, SourcePatternId, TargetPatternId, Support, Bits, Status, ConfirmedRulingId | `s.p >: t.q` — the strings that pull |
| `CorobsHits` | Id, RuleId, EntityId, SourceDist, TargetDist, Bits, Rank, Status (open · resolved · dismissed), ResolvedByRulingId, ResolvedByOrderId, ClosedByCascade, OpenedAt, ClosedAt | a ranked contradiction candidate and how it ended |

Staleness: a point is stale when `Entities.ModifiedAt` is newer than `RecordModifiedAt` **or** the entity's edge fingerprint changed (Edge writes do not bump `ModifiedAt`). Nothing in these tables is ever edited by hand; they are rebuilt from the world.

## 3. Components

### 3.1 `Corobs` — the science, generic, in its own repository
The author's decision (2026-11-02): COROBS is designed right from the beginning as a standalone library. It lives in the public repository **`mindattic/Corobs`** (`D:\Projects\MindAttic\Corobs`, ticket code COR), with the full description of the technology and attribution to Dr. P. Nick Lawrence in its README; the copyrighted book is kept in the private companion `mindattic/Corobs-Book`, checked out at `Corobs/book/` and never committed to the public repo. A class library with no dependency on anything, deterministic, no I/O: `Corobs.Geometry` (`NCube` — μ, σ, Eq. III-H/III-J/III-Q, Table II-1; `Correlithm`), `Corobs.Seeding` (`SeededPrng`, xoshiro256**, `Derive(label)`), `Corobs.Strings` (`StringCo` line/ring/plane), `Corobs.Converters` (cardinal, ordinal, continuous, planar; midpoint for missing), `Corobs.Mining` (`PatternMiner`, `RuleMiner`, `Cue`), `Corobs.Bench`. NUnit tests beside it. Prose consumes it the way it consumes MindAttic.Export: packed to `C:\LocalNuGet` and `lib/local-packages`, referenced by version. Prose keeps only adapters (§3.2).

### 3.2 `src/Prose.Core/Services/Corobs/` — the world-aware adapters
- `CorobsSpaceBuilder` — records (`CanonRecordLoader`), live `Edges`, `EntityStateEvents`, `Place.GeoLat/GeoLng` → points. Cardinal fields (EntityType, faction/corp, Territory, Species) → a seeded random CO per value; ordinal (Tier, TerritoryRange, LifeStatus) → evenly spaced string CO; continuous (ages, dates) → interpolated string CO; `string2d` → geography with adjacency; `graph` → the normalised mean of neighbours' COs by relation type; `time` → one long string CO over story-time so near-in-time is near-in-space. A missing field maps to the ply's midpoint: zero bits, never a false match. Free prose fields (`DailyLife`) are excluded. Rebuilds only stale points; runs as a cancellable background job in the Hub.
- `CorobsPatternMiner` / `CorobsRuleMiner` — thin wrappers that feed the library and persist proposals.
- `CorobsCueService` — hits for confirmed rules; on every rebuild re-evaluates every open hit across the universe and closes those now inside the zone with `ClosedByCascade`.
- `CorobsSwitch` — reads `Corobs:Enabled` from the Hub settings store; every entry point checks it first.
- Boundaries: `RulingService` = regex prohibitions on record leaves; `ContinuityService` = same-predicate drift on one entity; `PredicateExclusionService` = cross-predicate on one entity; **Corobs = cross-entity proximity only** and calls none of them. A confirmed truth that is a zero-tolerance prohibition is recorded as a `law` ruling, never as a `CorobsRule`.

### 3.3 Interfaces (both ways stay callable)
- CLI `prose --universe <u> --corobs enable | disable | status | build [--incremental] | patterns [--ply k] [--at <story-time>] | rules | cue [--entity <id|slug>] [--top N] | confirm --pattern|--rule <id> --ruling <id> | resolve --hit <id> --order|--ruling <id> | explain --entity <id|slug> [--at <story-time>] | bench [--holdout f] [--seed n]`.
- MCP twins `corobs_*`, each `[FactoryTool]`, dispatched by the Hub.
- Wiki: an additive **Corobs** panel on the entity page (position per ply, nearest patterns, trajectory, open hits), hidden when off. The Edge-based **Related** panel is untouched.
- Skill `/corobs on|off|status`.

## 4. The hand passes (protocol H, phases 1–2, no code)

1. Author order "COROBS hand pass n" under the campaign root. Dump read-only to scratch (`--export global`, `--graph-health --json`).
2. Build the plies by hand in scratch (sorting and counting are arithmetic, not judgment): faction; turf; era; life status; tier; tech; Schism exposure; carries/wields/owns; adjacency; story-time trajectory.
3. A candidate truth is ≥3 entities agreeing on ≥2 plies. Confirm by reading the records and the beats that tag them. Record each: mechanical → `law` with a prohibition regex probed first with `--search`; otherwise `law` text with no pattern.
4. Surface contradictions: `--ruling violations --records`; cluster outliers; turf vs no-go; the dead who act later; relationship reciprocity; `member_of` asymmetry; location vs timeline vs mention dates.
5. Fix one at a time: `verify_entity_begin` → field write with read-back → re-read → `verify_entity_commit`; or archive → splice → re-read. Never bulk.
6. Log the baseline: examined · truths · contradictions · fixed · **cascade** (closed without its own fix) · plies that carried signal · time.
7. Pass 2 repeats on everything modified since pass 1 began **plus its 1-hop neighbours and everything in its time window**. Dry when zero new fixes. The pass-1→pass-2 delta is the specification for §3.2's thresholds.

## 5. Protocol H — build, cue, adjudicate, resolve, rebuild (with the switch on)

1. `--corobs build` (incremental). 2. `--corobs patterns` and `rules`: read the proposals; **confirm** each true one with a hand-written `Label` and the ruling id that records it (`record_ruling` first), **reject** the rest. 3. `--corobs cue`: take the ranked hits from the top; for each, read the records and beats; fix by protocol A/B; `--corobs resolve --hit <id> --order <id>`. 4. Rebuild; hits that closed by cascade are counted, not claimed. 5. New incidents: a time-scoped pattern where several trajectories intersect is a vignette waiting to be written (RFC 0015 protocol C for the beat; `event` entity by protocol A).

## 6. Build increments (one engine order each, under this root)

1. **Switch** — `Corobs:Enabled` setting (default off), `--corobs enable|disable|status`, `/corobs` skill, invariant 5 test.
2. **Event is first-class** — `create_entity(type, name, description, recordJson)` MCP+CLI; `EntityFieldWriter` falls back to a `Records.Json` merge patch for repo-less types; `create_repository("Events", World)`. Edge types `present_at`, `witnessed`, `caused` documented in `docs/WORLD.md`. (Old way; may run during the hand passes.)
3. **Dated state events by hand** — `record_state_event(entity, aspect, verb, value, atStoryTime, beatGuid?)` over `WorldStateLedger.RecordAsync`.
4. **Library** — `MindAttic.Corobs` + tests (invariant 4, 6).
5. **Tables** — `AddCorobs` migration + invariants 1–3.
6. **Builder + miners + cue** — §3.2, background job, `--corobs build|patterns|rules|cue|confirm|resolve|explain`, MCP twins, wiki panel (invariant 7).
7. **Bench** — `--corobs bench`: hold-out recall against `BeatEntityMention` co-occurrence; reach; contradictions surfaced vs confirmed; cascade yield; story pull per `event`. OLD = Edge graph; NEW = Corobs. Results appended as `InstrumentRun` rows (`Instrument = corobs-bench`). Run 1 compares the hand baselines with the first build.
8. **Version grader** — §7.

Each closes by RFC 0015 §5.2 checks: commit, tests (TRX), **ledger** (real use), deploy where Hub code changed.

## 7. The version grader (PRS-T866)

`prose --grade --slug <book> | --series <slug> | --universe <u> --a <V|date|working> --b <V|date|working> [--advisory --allow-votes]`, MCP `grade_versions`. A book version is a pressed export (`ExportRecords.Version`, `BookFingerprint`, text at `Path`; fallback `ArchivedBooks.Markdown`); `working` is the live beats; a series or universe version at a date is each member book's latest press at or before it (no press → COULD NOT LOOK, never zero). Scores append as `InstrumentRun` (`Instrument = grade`); the comparison prints and lands in the export directory as `<CODE>_Grade_<A>_vs_<B>.md`.

**Deterministic battery** (official; each metric has a direction and a source; rolls up book → series → universe): size and structure (words, beats, chapters, scene/summary/transition/interstitial ratio, length distributions) · pacing (dialogue ratio, sentence-length mean and variance, paragraph length, action-verb density, short-beat share) · style (TTR/MTLD, Flesch–Kincaid, adverb/passive/filter-word rates, repeated phrases, **metric rulings** via `MetricsReport`) · craft (`--lint-prose`, `CraftNativeRules`) · law (`law`/`page-law` hits) · logic (open sweep BLOCKER/MODERATE, Story Ledger `CONTRADICTED`, Obligation Ledger balance — read from the publish-readiness instruments) · story shape (plant/payoff registered/bound/paid, **But/Therefore connective ratio** per chapter, strict-F3 reconciliation rate) · cast and world (entities tagged, presence Gini, POV switches, cross-POV knowledge flags, **Corobs interconnectivity of the cast**; at series/universe grain shared-cast density and cross-book edge closure) · measured reader (READER-QA §2 instruments 1, 4, 5 — measurements, not votes — for that fingerprint, else COULD NOT LOOK). Verdict: a per-metric table with ▲/▼ at each grain and a weighted win count (`Grade:Weights`, default 1). No single 0–100 (READER-QA §4).

**Advisory reader tier** — only with `--advisory --allow-votes`; reuses the `--duel` machinery (cross-family jury, both presentation orders, flipped verdicts discarded, cached by text-hash pair) at chapter grain for chapters whose text differs, blind to which is newer; reports chapters preferred A / B / split with rationales as revision fuel; stored as `InstrumentRun` `grade-advisory`; **never gates, never becomes a score column.**

## 8. Enforcement

| Failure mode | Mechanism | Strength |
|---|---|---|
| COROBS becomes a story ledger | invariants 1–2 (no page FKs, Label only) | enforced |
| the instrument confirms its own truths | invariant 3 (ruling id required) | enforced |
| LLM creeps into the geometry | invariant 4 + §8 wiring test | enforced |
| the new way breaks the old | invariant 7 (byte-identical old surfaces) | enforced |
| can't turn it off | invariant 5 (`disabled`, zero writes) | enforced |
| non-reproducible geometry | invariant 6 (seed determinism) | enforced |
| tool before results | §1.3 — author closes hand pass 2 before increment 4 opens | author gate |
| thousands of useless hits | ranking by bits × support; min cluster 5; 6σ threshold until the hand passes say otherwise | calibrated |
| advisory tier drifts into a score | `--allow-votes` gate, label, `InstrumentRun.Detail` only | enforced |

## 9. Tests
Library: Table II-1 statistics at N = 64 and 100 (μ, σ within tolerance); Eq. III-J against Monte-Carlo; seed determinism; string-CO monotonicity and ring closure; planted cluster of 6 among 1,000 random points → exactly one pattern; planted contradiction ranks first; no `Prose.*` reference. Prose: a record change marks exactly its points stale and closes a planted hit by cascade; switch off → every entry `disabled`, zero rows; invariants 1–3, 7.

## 10. Verification (end to end)
Hand passes: `--ruling violations --records` = 0 for every confirmed mechanical law; every fixed entity has a current F1; pass logs hold the baselines. Switch: `/corobs off` hides everything and writes nothing; `/corobs on` restores without a rebuild. COROBS: `--corobs build` completes on GLMZ; `--corobs patterns` **recovers the hand-found truths** of passes 1–2 (the instrument earns its place by recovering what the hands found — LOGIC §9's rule); `--corobs explain` on three well-known entities at two story-times is plausible; the Related panel is unchanged. Bench: both columns printed and stored; the hypothesis is COROBS ≥ old way on hold-out recall and cascade yield, reported either way. Grader: `--grade --slug bcoda --a V80 --b working` and `--grade --universe glmz --a <date> --b working` print; idempotent on identical fingerprints; `--advisory` refuses without `--allow-votes`. Vignettes: an interstitial beat tagged with an `event` shows in the event's and both witnesses' Appearances; its `occurs` state event is visible via `--world-state`; F4/F1 green; the beat reads cold.

## 11. Risks
Law collision without this root · event entities cannot be created or field-written by hand until increment 2 · no dated state-event tool until increment 3 · 6σ is a prior, the hand passes are the evidence · sparse fields → false truths (midpoint rule, min cluster, hand confirm, bench precision) · Edge writes do not bump `ModifiedAt` (edge fingerprint) · duplicate incidents across books (`find_entities` first) · capture traps when splicing (RFC 0015 §13) · a concurrent session in the tree (commit by path) · the advisory tier becoming a score.

## 12. References
P. Nick Lawrence, *Correlithm Object Technology*, Correlithm Publications, 2004 (ISBN 0-9752761-1-5): Ch. II–III (unit N-cube statistics, probability of proximity, bit content, point capacity), Ch. IV (soft tokens, rules, strings), Ch. VII (string COs, approximate relative location), Ch. IX (converters), Ch. X (cueing, anomaly detection, superposition), Ch. XI ("Anywhere an appropriate ensemble is found, correlithm objects will emerge … sociological behavior, where the ensemble is made up of individuals and the correlithm objects describe social groupings"). Author's copies: `C:\Users\ryand\Downloads\COBook.pdf`, `C:\Users\ryand\Downloads\Correlithm Object Technology (Corabs).md`.
