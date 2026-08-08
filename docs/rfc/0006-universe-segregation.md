---
codex: 1
project: Prose
code: SS
layer: rfc
status: accepted
updated: 2026-06-15
---

# RFC 0006 — Universe segregation: one deck of cards per universe {#SS-RFC-0006}

> **Goal (author, 2026-06-15):** "Segregate the *cards* so there isn't cross-over" between
> universes. The data layer is already universe-scoped ([SS-A3](../AMENDMENTS.md),
> [SS-LAW-15](../BIBLE.md#SS-§5)). This RFC investigates every *remaining* place GLMZ content
> can still bleed into another universe, and designs a single coherent way to close all of them.
> **Design only — no code in this pass.**

## 0. The core insight {#SS-RFC-0006-0}

A "card" is any **world-specific input the engine feeds an LLM or a retrieval step**. Today those
cards live in three different places, only one of which is segregated:

1. **Canon rows** (Entities/Strands/Books) — ✅ already segregated by `UniverseId` + the EF query
   filter ([SS-A3](../AMENDMENTS.md)).
2. **Prompt text baked into C# code** — ❌ ~18 service files literally type "GLMZ / Meridian 88 /
   cyberpunk" into their system prompts.
3. **World config + voice** stored in the global `Settings` table and the `Species` table — ❌ one
   shared row per key, no universe dimension.
4. **Derived indexes** (embeddings, world graph, search) — ❌ raw-SQL and single in-memory caches
   that bypass the EF filter entirely.

The fix is one idea applied everywhere: **every card belongs to exactly one universe's deck, and
the engine only ever holds cards from the current universe's deck.** No code path may contain, or
silently fall back to, another universe's card. Where a card is missing for the current universe,
the engine uses a **universe-neutral default or refuses** — never another universe's content.

## 1. Investigation — the complete cross-over surface {#SS-RFC-0006-1}

### 1a. Prompt "cards" — GLMZ hard-coded in LLM system prompts (Layer 1)

No shared helper exists; each service hand-codes its own world identity + rules. Confirmed sites
(file → what it injects into the prompt):

- `BeatGeneratorService.cs` — first system prompt **already fixed** via `WorldLine()` (the seam);
  but its `BuildExpertPanel()` fallback personas (`"World-Grounding (GLMZ)"`, "Cyberpunk Genre
  Specialist") are still hard-coded.
- `EpisodeGeneratorService.BuildSystemPrompt()` — the worst case: every line is Kyle/GLMZ canon
  (Silence, Chorus, Pixel, Mrs. Chen, Sable, Lotus, Bushido-Coda continuity). No seam at all.
- `OutlineReviewService.ReviewAsync` — full "WORLD RULES YOU MUST ENFORCE" block (CorpoNations,
  Sponsorship Program, Tiers, Behemoth, Φ).
- `StoryQualityService.BuildEvaluatorContext()` — full GLMZ world rubric sent to every quality voter.
- `StoryDirectorService` — four copy-pasted `"IMPORTANT: GLMZ is a dangerous world…"` injections
  (lines ~189/309/431/1283) + the runtime `"Violence erupts — GLMZ is a dangerous place."`.
- `EntityRatingService` — a 25-entry GLMZ persona pool + `"Would a resident of GLMZ care…"` question.
- `EntityReviewService` — `"GLMZ … cyberpunk city ceded to corporate sovereignty"` in every ballot
  + "die-hard cyberpunk reader" framing.
- `WriterOperatorService.BuildSystemPrompt()` — "WORLD RULES (HARD)" block (Φ, GLMZ, no police).
- `NpcGenerator`, `DynamicPlaceGenerator`, `ContractGenerator`, `RandomEncounterService`,
  `CanonGroundingService`, `CharacterPipelineService`, `StoryRefinementService` — each opens its
  system prompt with a "…set in GLMZ…" identity line.
- `ExpertPersonaCatalog.Starter()` — seeds a "World-Grounding (GLMZ)" persona into the DB.
- `WorldConsistencyService.WorldRules` — GLMZ rule list (used for text scan, not an LLM prompt).

**Clean chokepoints that already carry no world name** (the model to emulate): `WorldLine()`,
`CanonRetrievalService.RetrieveContextBlockAsync` (entity-facts block), `SceneContextAssembler`.

### 1b. Voice / world config — global `Settings` + `Species` (Layer 2)

All of this lives in the `Setting` table (PK = `Key` only, no `UniverseId`); `SettingsKvStore` and
`JsonSingletonRepository<T>` read `WHERE Key == key` with no universe filter. Global keys that bleed:

- `literary_rules`, `tone_bible`, `story_bible`, `character_profile` — the entire house voice + lore.
- `name_pool` — GLMZ Ubiquitous-Diaspora names.
- `expert_personas` — cyberpunk editorial panel.
- `world_consequences`, `world_reputation` — GLMZ narrative state.
- `quality_patterns` — failure patterns learned from GLMZ prose.
- (Universe-agnostic / lower risk: `action_configs`, `tts.rules`, `trivia.daily`,
  `users.accounts`.)
- Per-book keys (`book_outline:{id}`, `book_review:{id}`, `book_motifs:{id}`) inherit universe via
  the Book and are already safe.
- **Registers** (`docs/registers/JOY.md`, `SORROW.md`, `VULTURES.md`) are docs; their live effect
  is whatever `SeedVoiceRulesCli` writes into `literary_rules`/`tone_bible` — i.e. captured above.
- **`Species` table** — global, GLMZ-flavored canonical set (`human/ai/elf/synthetic/unknown`); no
  `UniverseId`, no query filter.

### 1c. Derived indexes — silent leaks that bypass the EF filter (Layer 3)

These are the dangerous ones: they do not go through `IQueryable<Entity>`, so the query filter
never runs.

- `EmbeddingService.FindSimilarAsync` — **raw SQL** over `EntityEmbeddings JOIN Entities` filtering
  only on `IsActive`. Returns top-k from **all** universes. `EntityEmbeddings` / `ProseEmbeddings`
  have **no `UniverseId` column**. `FindSimilarProseAsync` / `FindSimilarStrandBeatsAsync` have the
  same gap.
- `CanonRetrievalService` — passes straight through to `FindSimilarAsync` (no universe arg), so the
  canon block fed to generation can contain other-universe entities.
- `WorldGraphService` — a single global in-memory graph + one `world_graph.json` cache, no universe
  dimension on nodes/edges. Scoped *at rebuild* (EF DbSet), but the cache is then served to every
  universe; `IsStale()` probes unfiltered `Records`.
- `SemanticIndexService`, `InferenceService` — derive from `graph.AllNodes()` → inherit the graph's
  blindness.
- `ThematicIndexService`, `GlobalSearchService` — scoped at rebuild, but the result is a **frozen
  singleton cache** with no per-universe key and no invalidation on switch → a Fantasy session keeps
  serving the GLMZ index until restart.
- `Edge`, `CharacterReadModel`, `EntityStateEvent` — no `UniverseId`, no query filter; reachable by
  direct DbSet scans, and nothing prevents an `Edge` from linking two different universes.

## 2. The design — a per-universe World Profile + scoped retrieval {#SS-RFC-0006-2}

### 2a. The World Profile (one home for every card)

Promote the `Universe` row into a full **World Profile**: the single, structured source of every
world-specific card. `WorldPrimer` (already added) is the first field; add the rest as columns on
`Universe` (or a 1:1 `UniverseProfile` table):

- `WorldRules` — the hard-rules block (currency symbol + meaning, law/policing model, factions,
  named dangers). Replaces every hand-coded "WORLD RULES" block.
- `ConflictMandate` — optional "this world is dangerous, ensure conflict" directive (GLMZ sets it;
  a cozy universe leaves it null → no forced violence).
- `ReaderPersonaFraming` / `EditorPersonaFraming` — the "die-hard cyberpunk reader" / "resident of
  GLMZ" voter framings, generalized.
- `SettingLine` — the "You are writing … set in {world}" identity line (what `WorldLine()` returns).

Everything else per-universe lives in the existing stores, made universe-aware (2b/2c).

### 2b. Make the config stores universe-aware — one transparent change (Layer 2)

Mirror the canon-scoping pattern on the config substrate:

- Add `UniverseId` to the **`Setting`** table; change its PK from `(Key)` to `(Key, UniverseId)`.
  Teach `SettingsKvStore` and `JsonSingletonRepository<T>` to read/write the row for
  `IUniverseContext.CurrentId` automatically. **This segregates `literary_rules`, `tone_bible`,
  `story_bible`, `character_profile`, `name_pool`, `expert_personas`, `world_consequences`,
  `world_reputation`, `quality_patterns` in one move, with zero call-site changes** — exactly how
  the EF query filter segregated all 26 entity types from one point. Universe-agnostic keys
  (`action_configs`, `tts.rules`, `users.accounts`) are exempted via a small allow-list that uses a
  shared sentinel `UniverseId`.
- Add `UniverseId` + a query filter to the **`Species`** table (same pattern as Entity/Strand/Book).
- **Backfill**: every existing global row → GLMZ, same as the canon backfill. The Fantasy rows start
  empty (see 2e on the missing-card policy).

### 2c. Close the derived-index leaks (Layer 3)

- **Embeddings**: add a denormalized `UniverseId` column to `EntityEmbeddings` and `ProseEmbeddings`
  (stamped from the parent at embed time; backfilled to GLMZ), and add `AND UniverseId = @universe`
  to the raw SQL in `FindSimilarAsync` / `FindSimilarProseAsync` / `FindSimilarStrandBeatsAsync`.
  Denormalize rather than join-and-filter so the vector search stays a single indexed scan.
- **WorldGraph + Semantic/Inference/Thematic/GlobalSearch caches**: key each cache **by
  `UniverseId`** (a small map of caches), and make `world_graph.json` → `world_graph.{slug}.json`.
  Switching universe serves that universe's cache (lazy-built on first use); `IsStale()` probes
  `Records` joined to the scoped `Entities`. This avoids rebuild thrash when switching back and
  forth. (Cheaper interim: tag each cache with the universe it was built for and rebuild on
  mismatch — correct but rebuilds on every switch.)
- **`Edge`, `EntityStateEvent`, `CharacterReadModel`**: add `UniverseId` (denormalized from the
  parent entity) + query filters. Add an invariant: an `Edge`'s source and target must share a
  `UniverseId` (enforced at write time; a cross-universe edge is a bug).

### 2d. Segregate the prompt cards — one `UniversePromptFactory` (Layer 1)

Introduce a single injectable helper backed by `IUniverseContext` + the World Profile, exposing the
cards every service needs:

- `SettingLine()` — generalizes `WorldLine()` (move it here; `BeatGeneratorService` calls it).
- `WorldRulesBlock()` — from `Universe.WorldRules`.
- `ReaderPersona()` / `EditorPersona()` — from the profile framings.
- `ConflictDirective()` — from `ConflictMandate` (empty ⇒ nothing injected).
- `WorldName()` / `CurrencyNote()` — small primitives.

Then **every site in §1a deletes its hard-coded literal and calls the factory instead** — the same
move already proven by `WorldLine()`. Because the factory serves the *current* universe's card, a
service can no longer contain GLMZ text: segregation becomes structural, not vigilance. For GLMZ the
profile returns byte-identical text (zero voice drift); for Fantasy it returns Fantasy's cards.
`EpisodeGeneratorService` (pure Kyle/GLMZ) is special-cased: it is a GLMZ-only feature until a
universe supplies its own episode profile.

### 2e. The missing-card policy (the anti-bleed guarantee) {#SS-RFC-0006-2e}

The current `WorldLine()` falls back to GLMZ text when the primer is empty — acceptable only because
GLMZ is the default. The general rule must be stricter: **when the current universe lacks a card, the
factory returns a universe-neutral default or signals "not configured" — it must NEVER serve another
universe's card.** A `UniverseReadiness` check reports which profile cards (primer, rules, voice,
name pool, species, personas) a universe is missing, so the author fills them before generating
there. This is what actually prevents bleed: the fallback is neutral/empty, not GLMZ.

## 3. Rollout order (low-risk, each step independently shippable) {#SS-RFC-0006-3}

1. **Config substrate (2b)** — `UniverseId` on `Setting` + `Species`, auto-scoped KV, backfill to
   GLMZ. Highest leverage, transparent, no prompt edits. GLMZ behavior unchanged (it's the only
   populated universe).
2. **Embedding columns + raw-SQL filter (2c, embeddings)** — closes the biggest *silent* leak; the
   generation grounding stops returning cross-universe entities.
3. **`UniversePromptFactory` + migrate the ~18 prompt sites (2d)** — mechanical, GLMZ byte-identical;
   do it in small batches, diffing GLMZ output as a regression guard.
4. **Per-universe caches for graph/search/indexes (2c, caches)** — last, because it's the most
   involved and least urgent (caches are scoped-at-rebuild today, so single-universe use is correct).
5. **`Edge`/`EntityStateEvent`/`CharacterReadModel` scoping + the cross-universe-edge invariant.**

## 4. Acceptance — how we prove no cross-over {#SS-RFC-0006-4}

- **Empty-universe test**: with the current universe set to Fantasy (no data), every retrieval path
  — `FindSimilarAsync`, `CanonRetrievalService`, `WorldGraphService`, `GlobalSearchService`,
  `ThematicIndexService` — returns **zero** GLMZ results.
- **Config-isolation test**: writing `literary_rules` under Fantasy does not change GLMZ's
  `literary_rules`, and vice versa.
- **Prompt-purity test**: a scan asserts no service prompt contains a hard-coded "GLMZ / Meridian
  88" string (everything routes through `UniversePromptFactory`); `EpisodeGeneratorService` is the
  single documented exception.
- **GLMZ no-drift**: golden-prompt comparison shows the GLMZ system prompts are byte-identical
  before/after the factory migration.
- **Readiness gate**: generating in a universe missing required cards surfaces a clear "universe not
  configured" report instead of silently borrowing GLMZ.

## 5. Status of the pieces {#SS-RFC-0006-5}

**Shipped + verified 2026-06-15:**
- ✅ Canon rows scoped (SS-A3); `WorldLine()` seam; `Universe.WorldPrimer`.
- ✅ **Step 1 — config substrate.** `UniverseId` on `Settings` (composite key `Key`+`UniverseId`) +
  `Species`, an EF query filter on both, a SHARED sentinel (`Universe.SharedId`) for operational
  keys (`action_configs`/`tts.rules`/`users.accounts`/`current_universe`), insert-stamping, and
  per-universe in-memory cache invalidation via `UniverseScope.Epoch` (EfRepository,
  CharacterRepository, JsonSingletonRepository). *Verified:* GLMZ literary-rules = 23,482 chars vs
  Fantasy = 1,915 (engine scaffolding only); GLMZ voice does not bleed.
- ✅ **Step 2 — embedding leak closed.** `UniverseId` denormalized onto `EntityEmbeddings` +
  `ProseEmbeddings`, stamped at embed time, and added to the raw-SQL predicate of
  `FindSimilarAsync` / `FindSimilarProseAsync` / `FindSimilarStrandBeatsAsync`. *Verified:* the same
  `--canon-retrieve` query returns 5 GLMZ hits vs **0** for Fantasy.
- ✅ **Step 3 (partial) — prompt factory.** `IUniverseContext.WorldGroundingOr(glmzText)` + `IsGlmz`
  (the segregation seam); migrated the primary generation cards (`BeatGeneratorService.WorldLine`
  + `StoryDirectorService` combat mandate, GLMZ byte-identical, empty for other universes).

**Shipped (completing the RFC; [SS-US-U5](../USER_STORIES.md) → ✅):**
- ✅ **Step 3 (rest).** Every GLMZ-worded prompt site (`StoryQualityService`, `OutlineReviewService`,
  `EntityRatingService`, `EntityReviewService`, `NpcGenerator`, `DynamicPlaceGenerator`,
  `ContractGenerator`, `RandomEncounterService`, `CanonGroundingService`, `CharacterPipelineService`,
  `StoryRefinementService`, `WriterOperatorService`, `WorldConsistencyService`, the
  `BeatGeneratorService` fallback persona panel) now routes its literal through
  `UniverseScope.Current?.WorldGroundingOr(...) ?? <literal>` (GLMZ byte-identical).
  `EpisodeGeneratorService` stays a GLMZ-only feature by design.
- ✅ **Step 4 — derived-index caches.** `WorldGraphService`, `SemanticIndexService`,
  `ThematicIndexService`, `InferenceService`, `GlobalSearchService` record the build epoch and
  rebuild when `UniverseScope.Epoch` changes.
- ✅ **Step 5 — `Edge` / `EntityStateEvent` / `CharacterReadModel`** carry a `UniverseId` + query
  filter; the missing-card policy is realized by `WorldGroundingOr` (neutral fallback, never another
  universe's card).
- ✅ **Seed ids are UUIDv7** (`Universe.GlmzId`/`FantasyId`/`SharedId`), matching the rest of the app;
  the existing DB was re-stamped via `restamp_universe_guid7_20260615.sql`.

**Verification:** `UniverseSegregationTests` (10) + 147 gate tests green; CLI smokes (canon-retrieve
GLMZ 5 / Fantasy 0; voice rules GLMZ 23.5KB / Fantasy 1.9KB). See [SS-A4](../AMENDMENTS.md).
