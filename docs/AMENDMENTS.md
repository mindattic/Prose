---
codex: 1
project: StreetSamurai
code: SS
layer: amendments
status: living
updated: 2026-06-15
---

# StreetSamurai — Amendments (append-only; amendment wins over the bible)

> Append-only. Never rewrite an amendment; supersede it with a new one. Beyond ~25, fold into the
> bible and start a new epoch (note the git tag); history stays in git.

## SS-A1 — Adopt the Codex documentation standard (supersedes —)

**What changed.** Installed the MindAttic Codex standard. `ARCHITECTURE.md` (the prior software
source of truth) was migrated into [docs/BIBLE.md](BIBLE.md) (L0). Its goal tables became
[docs/USER_STORIES.md](USER_STORIES.md) (L2). The continuity-invariants list from
`v3/canon_writes/story_state.md` was promoted into BIBLE §5 as narrative laws
[SS-LAW-9](BIBLE.md#SS-§5)…[SS-LAW-14](BIBLE.md#SS-§5); the engine invariants from `ARCHITECTURE.md`
§2a became [SS-LAW-1](BIBLE.md#SS-§5)…[SS-LAW-6](BIBLE.md#SS-§5); CLAUDE.md code/world rules became
[SS-LAW-7](BIBLE.md#SS-§5)/[SS-LAW-8](BIBLE.md#SS-§5).

**Why.** One source of truth, stable IDs, a doctor that catches drift, and a SessionStart digest so
every Claude session loads the canon. Replaces ad-hoc, scattered docs.

**Migration / preservation (no content deleted).**
- `ARCHITECTURE.md` is retained as a 1-line pointer to `docs/BIBLE.md` (README links it; tooling may
  still read the path).
- `v3/canon_writes/story_state.md` remains the **session/state scratch notes**; its *invariants*
  now also live (authoritatively) in BIBLE §5.
- `engine_data/*.json` is registered as the **L5 data layer** via schemas under `docs/data/_schema/`
  and the master entity-identity table [docs/data/ENTITY_IDENTITY.md](data/ENTITY_IDENTITY.md). Its
  canon *values were not rewritten*; per [SS-LAW-1](BIBLE.md#SS-§5) it is the seed/export mirror,
  not the live read path.
- Prose draft sprawl recorded, not deleted: the canon prose register is **v8**
  (`engine/bushido_coda_v3/01_bearing_teeth_v8.md` + `00_style_guide.md`). Earlier drafts
  (`engine/bushido_coda_v2/*_v2..v6`, `*_v7`) are superseded historical drafts kept on disk. Prose
  HTML bodies are treated as `generatedFrom` the chapter beats.
- The project rule "no Markdown files except README" (CLAUDE.md) is amended: the Codex `docs/*.md`
  set is the documented exception (it is documentation, not app data). Data files remain JSON.

## SS-A2 — Multi-Universe engine; GLMZ is Universe #1 (supersedes —)

**What changed.** The engine is recast as **universe-agnostic**. A `Universe` lookup table is
introduced, and every canon/story root (`Entities`, `Strands`, `Books`) gains a single non-null
`UniverseId` (1:M; beats/chapters inherit via their parent). **GLMZ becomes Universe #1**;
**Fantasy/Steampunk** is stood up as Universe #2 on the same tooling. The project is **not
renamed** — "StreetSamurai" stays the engine codename across the DB, connection strings, `.NET`
namespaces, `StreetSamuraiDbContext`, and Azure infra. Amends BIBLE §1, §2, §3, §4.2, §5 (new
[SS-LAW-15](BIBLE.md#SS-§5); [SS-LAW-8](BIBLE.md#SS-§5) + [SS-LAW-10](BIBLE.md#SS-§5)…[SS-LAW-14](BIBLE.md#SS-§5)
re-scoped to the GLMZ universe), and §9. New stories: [USER_STORIES.md](USER_STORIES.md) Epic U
(SS-US-U1…U7).

**Why.** One engine, many worlds, on shared tooling — the lowest-risk path. A rename would touch
~3,754 string occurrences across ~731 files and would entangle the auth Data-Protection boundary,
the connection-string/DB name, the MCP tool prefix, and re-provisioning of Azure infra with an
already-delicate DB migration. Keeping the codename decouples the two.

**Migration / preservation (no content deleted).**
- **Single `UniverseId` FK chosen over an M:M bridge.** A crossover entity (vocabulary shared
  across universes) is **duplicated** — one row per Universe, never a shared row. The author
  explicitly prefers a handful of duplicate rows over refactoring the whole schema onto a bridge.
- **SwitchUniverse is per-process / per-session, never a single shared global.** The current
  universe resolves by precedence: explicit `--universe <slug>` flag → `SS_UNIVERSE` env var (per
  terminal) → UI circuit/session selection (per browser tab) → the global default `current_universe`
  KV (fallback). This lets two CLIs (or two tabs) write different universes at the same time.
- **Adding `UniverseId` to system-versioned tables** uses the `SYSTEM_VERSIONING OFF → ALTER table
  + `_History` → ON` dance (pattern in `v3/StreetSamurai.Blazor/Cli/MigrateSqlCli.cs`).
- **Execution staged:** this amendment and the docs land first; the DB was backed up to
  `backups/StreetSamurai_preuniverse_20260615.bak` (RESTORE VERIFYONLY passed) before any change.
  The schema migration, EF query filter, SwitchUniverse wiring (UI/CLI/MCP), per-universe config
  namespacing, GLMZ-prompt de-hardcoding, and the CyberSpace→dark-mode shell are **deferred to a
  reviewed follow-up** and are not built in this pass.

## SS-A3 — Multi-Universe engine implemented (supersedes the "deferred" stance of SS-A2)

**What changed.** The build deferred by [SS-A2](#) shipped (2026-06-15). The engine is now multi-
universe in code, not just docs:
- **Schema** (`add_universe_20260615.sql`): a non-temporal `Universe` table seeded with `glmz` +
  `fantasy-steampunk` (well-known ids `1111…` / `2222…`), a `UniverseId` column on `Entities`,
  `Strands`, `Books` (added via the `SYSTEM_VERSIONING OFF → ALTER table + `_History` → ON` dance,
  NOT NULL DEFAULT GLMZ so every existing row backfilled to Universe #1), and per-universe unique
  slug indexes (`UX_Entities_Universe_Type_Slug`, `UX_Strands_Universe_Slug`, `UX_Books_Universe_Slug`)
  so the same (type, slug) may recur across universes.
- **Scoping**: an EF global query filter on `Entity`/`Strand`/`Book` keyed off an ambient
  `IUniverseContext` (`UniverseScope.EffectiveId`); a single filter on the `Entity` spine
  transitively scopes every entity type (Records-path reads navigate `Records→Entity`; the character
  read paths derive their id-set from `Entities`). `StreetSamuraiDbContext.SaveChanges` stamps
  `UniverseId` on new rows. Empty scope (tests / pre-migration) ⇒ no-op.
- **SwitchUniverse** (per-process/per-session): `--universe <slug>` flag + `SS_UNIVERSE` env (CLI),
  a `switch_universe`/`list_universes`/`current_universe` MCP tool set, and a `NavMenu` dropdown in
  the UI; selection precedence flow-override → process-override → `current_universe` KV default. Two
  CLIs (two OS processes) target different universes simultaneously.
- **World-primer seam**: each `Universe` has a `WorldPrimer`; `BeatGeneratorService.WorldLine`
  injects it for non-GLMZ universes while leaving GLMZ's prompt byte-identical (zero voice drift).
- **Shell**: the CyberSpace animated background (console-bg / sacred-geometry / tv-static JS + the
  cyberspace DOM divs) removed for plain dark mode; the base dark `.app-shell` theme is unchanged.

**Why.** Realize the SS-A2 architecture so the same tooling writes any registered universe.

**Verification.** Full solution builds clean (0 errors); 129 gate tests pass
(`DiRegistrationTests`, `StrandWorkbenchServiceTests`, `CharacterReadModelTests`, …); CLI smoke
`--list-strands --universe glmz` → 94 strands vs `--universe fantasy-steampunk` → 0, with the
universe predicate visible in the generated SQL. DB backed up first to
`backups/StreetSamurai_preuniverse_20260615.bak` (RESTORE VERIFYONLY passed).

**Residual (tracked as SS-US-U5 🟡).** The ~27 other GLMZ-hardcoded generation prompt sites should
adopt the `WorldLine`/`WorldPrimer` seam, and the voice/tone/register KV keys should be namespaced
per universe slug. No rename was performed — "StreetSamurai" remains the engine codename.

## SS-A4 — Universe segregation complete; seed ids are UUIDv7 (supersedes the SS-A3 residual)

**What changed.** [RFC 0006](rfc/0006-universe-segregation.md) is fully implemented — every
cross-over surface beyond canon rows is now scoped to the current universe, and a "card" for the
current universe can never be another universe's:
- **Config** — `UniverseId` on `Settings` (composite key `Key`+`UniverseId`) and `Species`, with EF
  query filters. Operational keys (`action_configs`, `tts.rules`, `users.accounts`,
  `current_universe`) carry a SHARED sentinel and are visible from every universe. The KV layer auto-
  scopes; in-memory caches (repos, voice docs, derived indexes) invalidate on `UniverseScope.Epoch`.
- **Retrieval** — `UniverseId` denormalized onto `EntityEmbeddings`/`ProseEmbeddings`; the raw-SQL
  `FindSimilar*` queries (which bypass the EF filter) now carry a universe predicate.
- **Prompts** — the `IUniverseContext.WorldGroundingOr(glmzText)` seam wraps every GLMZ-worded LLM
  prompt string; GLMZ stays byte-identical, other universes get their own world primer.
  `EpisodeGeneratorService` remains a GLMZ-only feature by design.
- **Caches** — `WorldGraphService` + the Semantic/Thematic/Inference/GlobalSearch indexes rebuild
  when the universe changes. **Ledger** — `Edge`/`EntityStateEvent`/`CharacterReadModel` scoped.
- **Missing-card policy** — when a universe lacks a card the seam returns a neutral default, never
  another universe's content.

**Seed ids → UUIDv7.** The first universe migrations seeded sentinel ids (`11111111…`/`22222222…`/
`99999999…`). These are now UUIDv7 like every other Id in the app — fixed constants
(`0197e9c9-0001-…` GLMZ, `…-0002-…` Fantasy, `…-0099-…` Shared) so the bootstrap / IsGlmz / stamping
can still reference them without a DB hit. The existing dev DB was re-stamped with
`restamp_universe_guid7_20260615.sql` (a one-time, dev-only correction not added to the
ApplyMigrations list; fresh DBs seed UUIDv7 directly).

**Why.** Realize the RFC so the same tooling writes any universe with zero bleed; align the Id
convention with the rest of the codebase.

**Verification.** `UniverseSegregationTests` (10) + 147 gate tests green; full solution builds
clean; CLI smokes prove scoping (canon-retrieve GLMZ 5 / Fantasy 0; voice rules GLMZ 23.5KB /
Fantasy 1.9KB). DB backed up to `backups/StreetSamurai_preRFC0006_20260615.bak` first.

## SS-A5 — Fully relational canon: `Records.Json` retired per type (supersedes the blob-as-canonical framing)

**What changed.** Author directive: *"any JSON fields should be broken out to tables and bridge
tables for maximum relational data management — every repository must be relational, not use JSON
blobs."* Canon entities move off the `Records.Json` blob onto typed tables + bridges (the way
**Character** already was). See [RFC 0007](rfc/0007-fully-relational-canon.md) for the per-type
recipe + parity gate. This supersedes BIBLE §4.2's framing of `Records.Json` as *the* canonical
store — the relational tables become canonical; the blob is a per-type rollback artifact retired
once parity passes.

**Why.** The point of a relational DB is queryable, joinable, integrity-checked relationships —
not deserializing blobs to read them. (Note: cross-entity *relationships* already live relationally
in `Edges` + the WorldGraph, and *semantic* similarity in the `VECTOR` embedding tables; this
amendment relationalizes the remaining *attributes* + embedded lists, and projects blob relationship-
lists into real `Edges`/bridges — the edge-completeness prong that actually prevents missing-link
bugs like the cat-ear genemod.)

**Progress.** ✅ **Faction** converted end-to-end (FactionMapper + `FactionRelationshipTags` bridge +
faction tags → `EntityTags` + backfill CLI + 13 parity tests; live 163/0 parity; blob retired;
backup `backups/StreetSamurai_preFactionBlobDrop_20260615.bak`). Character was already relational.
⬜ ~24 types remain, each following the RFC 0007 recipe; the blob stays source-of-truth per type
until that type flips, so the engine is always consistent.
