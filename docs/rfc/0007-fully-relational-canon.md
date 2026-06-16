---
codex: 1
project: StreetSamurai
code: SS
layer: rfc
status: accepted
updated: 2026-06-15
---

# RFC 0007 — Fully relational canon: retire `Records.Json` into typed tables + bridges {#SS-RFC-0007}

> **Directive (author, 2026-06-15):** "Any JSON fields should be broken out to tables and bridge
> tables for maximum relational data management." Every canon entity type becomes fully relational —
> typed columns for scalars, bridge tables for lists/nested objects — the way **Character** already
> is. `Records.Json` is retired per type once its relational path is proven.

## 0. Current state {#SS-RFC-0007-0}

Two storage shapes exist today (measured on the live DB):
- **11,139** active entities are **`Records.Json` blob-backed** — `EfRepository<T>` deserializes the
  blob on read and re-serializes on write; thin subtype tables are partial indexed projections.
- **557** (Character) are **fully relational** — `Characters` + ~30 bridge tables + `CharacterReadModels`,
  **no `Records.Json`**. This is the **proven template** (`CharacterMapper` + `CharacterRepository`).

~25 types remain on the blob (faction, place, corponation, weapon, equipment, cyberware, apparel,
ammunition, pharmaceutical, genemod, material, transportation, consumer_good, automaton, archetype,
quote, news, contract, document, vocabulary, lab_specimen, psionic, technology, motif, entertainment,
flyover_entity, subsidiary, …).

## 1. The repeatable pattern (per type) {#SS-RFC-0007-1}

Modeled 1:1 on the Character relationalization:
1. **Shape analysis** — read the type's `Records.Json` across all rows → classify every field as a
   *scalar* (→ typed column on a `{Type}` table keyed by `Entity.Id`) or a *list/nested object*
   (→ a `{Type}{Collection}` bridge table with FK to the parent + a `Position` for order).
2. **Schema** — add the typed table + bridges (system-versioned, like the rest). **No per-table
   `UniverseId` needed** — universe scoping derives from the `Entities` spine (the same id-set
   pattern Character uses; see [RFC 0006](0006-universe-segregation.md)), so it carries over free.
3. **Mapper** — `{Type}Mapper` with `FillScalars` / `FillBridges` / `Materialize`, mirroring
   `CharacterMapper`.
4. **Repository** — convert `{Type}Repository` from `EfRepository<{Type}Data>` (JSON round-trip) to a
   relational repo reading columns/bridges and writing them, mirroring `CharacterRepository`.
5. **Backfill** — one-time, idempotent migration: for every entity, deserialize `Records.Json` →
   persist via the new mapper. **Gated by round-trip parity** (§3).
6. **Drop the blob** — delete `Records.Json` for that type **only after parity is proven**; keep it
   as the rollback artifact until then.
7. **Read-model** — add a `{Type}ReadModel` *only* if the type needs deep-read perf (Character did
   because of the ~30-include fan-out; most types won't).

## 1b. Edge-completeness (the real root-cause prong) {#SS-RFC-0007-1b}

Relationalizing *attributes* is only half the win. Cross-entity **relationships** are supposed to
live in `Edges` (the graph the generator actually traverses) — but some are buried as **lists inside
a blob** (a faction's `members`, a weapon's `known_users`), and some were never captured at all
(Cel's cat-ears↔genemod link existed in *neither* a field nor an edge — only in prose — which is
why generation wrote "a case for the ears"). So each type's conversion MUST also:
- **Project the blob's relationship lists into real `Edges`/bridges** (e.g. `member_of`, `known_user`,
  `manufactured_by`) so they're queryable and visible to the WorldGraph.
- Feed a standing **edge-completeness audit**: flag entities whose prose implies a relationship that
  has no edge (the Cel class), surfaced as Findings for review.

## 2. What stays / what's reused {#SS-RFC-0007-2}

- `Entities` spine, `Edges`, `EntityTags`, `EntityProperties` (the flex bag — use it as the
  *overflow* for rare/sparse fields so the typed columns stay clean), `Taxonomies` — all unchanged.
- Embeddings, world-graph, search, and **universe scoping** all derive from `Entities` ids, so they
  keep working as each type flips to relational — no rework.
- `IExportableRepository` surface (`GetAll/GetById/GetBySlug/Save/…`) is preserved, so consumers and
  the encyclopedia pages don't change.

## 3. Verification harness (the safety gate) {#SS-RFC-0007-3}

A **parity test per type**, run before dropping any blob: for every entity, assert
`canonicalize(JSON-materialized object) == canonicalize(relational-materialized object)`. Drop
`Records.Json` only at 100% parity. Plus: `DiRegistrationTests`/`InterfaceRegistrationTests` green,
full build clean, and a backup before each type's blob-drop. This mirrors the
`EmbeddingFallbackTests`/round-trip discipline already in the suite.

## 4. Ordering {#SS-RFC-0007-4}

1. **Proof-of-concept on ONE medium-complexity type first** (recommend **Faction** or **Weapon**) —
   end-to-end: schema → mapper → repo → backfill → parity → drop blob. This proves the pattern + the
   generic harness and produces a copyable template.
2. **Fan out** by value/complexity. Each type is an independent, shippable PR with its own parity
   gate. ~25 types; expect 100+ bridge tables in total.

## 5. Trade-offs / caveats {#SS-RFC-0007-5}

- **Loss of tolerant-converter flexibility.** The JSON blobs absorb messy/evolving canon via tolerant
  converters; fixed columns can't. Mitigation: nullable columns + the `EntityProperties` overflow bag
  for rare fields; treat the typed schema as the 90% and the bag as the long tail.
- **Schema churn.** Adding a field to a type now means a migration, not just a JSON key. Acceptable
  cost for queryability (the directive's goal: "maximum relational data management").
- **System-versioning** on every new table (the `OFF/ON` dance for later column adds), as elsewhere.
- This is a **multi-PR program**, not one change. The blob layer stays the source of truth for any
  type not yet converted, so the engine is always in a consistent, shippable state.

## 6. Status {#SS-RFC-0007-6}

✅ Accepted (author, 2026-06-15): all repositories must be relational — no `Records.Json` blobs.

**✅ Faction — DONE (the proven template).** `FactionMapper` (FillScalars/FillBridges/Materialize/
LoadAll/LoadOne/LoadAllLite/RebuildAllAsync) + `FactionRepository` flipped off `EfRepository<T>` onto
the relational tables + the missing `FactionRelationshipTags` bridge (`add_faction_relationship_tags_20260615.sql`)
+ faction `.tags` migrated to the universal `EntityTags` layer + `ss --rebuild-faction-relational`
backfill CLI + `FactionRelationalParityTests` (13). **Live parity: 163 factions / 0 mismatches**
across all 8 lists; **the faction `Records.Json` blob has been retired** (0 rows). Backup taken first
(`backups/StreetSamurai_preFactionBlobDrop_20260615.bak`).

**The repeatable recipe (copy for each remaining type):**
1. Build `{Type}Mapper` from the `CharacterMapper`/`FactionMapper` template.
2. Close fidelity gaps first (run the OPENJSON count check: any list field with no relational home,
   any `.tags` not in `EntityTags`) → add bridge(s) + migrate tags.
3. Flip `{Type}Repository` off `EfRepository<{Type}Data>` onto the mapper (keep the universe-epoch cache).
4. Backfill (`RebuildAllAsync`) from the blob; parity test (round-trip) + **live structural parity**
   (blob list-lengths == bridge counts, 0 mismatches).
5. Backup → drop `Records.Json` for that type.

### Progress (2026-06-15)
**✅ Fully relational + blob retired (10):** faction, quote, news, contract, vocabulary, genemod,
material, transportation, **archetype, ammunition**. **⬜ Remaining (~14):** place, corponation,
subsidiary, weapon, equipment, cyberware, apparel, pharmaceutical, consumer_good, automaton,
document, motif, technology, entertainment, flyover_entity. (lab_specimen / psionic: no active
data.) **Character:** read-path relational but its blob (1,242 rows) not yet dropped.

**archetype + ammunition closure (the canonical gate in action):** archetype's columns/bridges were
already complete (550 rows, 0 mismatches across category/description/behavioral_signature/under_stress/
at_rest + will_always/will_never/unless/similar_to/opposite_of). Ammunition had the batch-3 defect:
`AmmunitionMapper` never mapped `Tier ↔ TierAvailability` (read **or** write), so the app served
`tier_availability` empty for the 32 rows whose column was populated. Fixed the mapper both
directions, added the `TierAvailability` assertion to the parity test, re-ran `--rebuild-ammunition-
relational`, and verified live: 52 blob rows, **0 mismatches** across tier/legality/caliber + all four
bridges. Backup `backups/StreetSamurai_preArchetypeAmmoBlobDrop_20260615.bak`; both blobs then dropped
(602 rows: 550 archetype + 52 ammunition).

### ⚠️ CRITICAL LESSON (batch 3) — the recipe was insufficient
The existing subtype tables are **LOSSY**: many were built with *partial* columns (e.g. genemod was
missing `target_system`/`legality`, transportation missing `propulsion`/`speed`, material missing
`cost`). A naïve repo-flip onto them is a **read-regression** (the app serves those fields empty) and
dropping the blob would *lose canon*. Two corrections to the recipe, now mandatory:
1. **Audit ALL `{Type}Data` fields vs the entity columns/bridges first; ADD every missing column +
   bridge** (temporal `OFF/ON` dance) so nothing stays blob-only.
2. **The parity gate must be CANONICAL** — assert full-object JSON equality with *every* field
   populated, not just list-counts/tags (counts miss scalar gaps). genemod/material/transportation
   were fixed this way (canonical tests pass; live columns 100% populated; blobs then retired).
   archetype/ammunition still need their parity tests upgraded to canonical before their blobs drop.
