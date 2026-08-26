---
codex: 1
project: Prose
code: SS
layer: rfc
status: implemented
updated: 2026-08-26
---

# RFC 0007 — Universe Interchange & the EVE Universe {#SS-RFC-0007-INTERCHANGE}

> Note: this repo already has an unrelated "RFC 0007" (`0007-fully-relational-canon.md`)
> predating this one — a numbering collision from before this RFC existed. Both keep their
> original filenames/titles ("RFC 0007") since that's how every piece of code, this doc's own
> history, and the `/eve` slash command already refer to this one; this doc's own heading
> anchor above carries an `-INTERCHANGE` suffix so codex doctor's unique-anchor check passes
> without renaming anything else.

Status: **PHASE 1 IMPLEMENTED (2026-08-26)** — execution keyword: `/eve` in the Prose CLI.
Phase 2 (game-writing deliverables as Books) remains design-approved, not yet built.
Author: handoff from the ExperimentEve session (Claude, D:\Projects\MindAttic\ExperimentEve)
Depends on: RFC 0001 (canon-as-data), RFC 0006 (universe segregation)

## Purpose

Prose becomes what it already almost is: **the repository of universes** — not
just the writing program for books, but the canonical store other MindAttic
apps (games first: ExperimentEve) read from and write to via CLI and MCP.

**The end goal** (user-stated): Prose lends its writing abilities to
ExperimentEve. Prose does what Prose does best — structured, canon-aware
long-form writing — and the deliverables expand from novels to the documents
game industry writers actually produce: the **Game Design Document**, the
**game script / narrative design doc**, **bark & dialog sheets**, and
eventually the **EVE Prequel Novella** — video games, creative writing, dark
comedy, and sci-fi merged in one canon. This is the most important thing to
come from the Experiment. (Yes, that's what it was all along.)

This must NOT diminish the book-writing mission. Everything here is additive:
new dispatch lines, new files, one new universe row. No existing command,
tool, table, or law changes. The Book→Chapter→Beat hierarchy is untouched.
If any step would require modifying existing behavior, stop and redesign.

## What already exists (do not rebuild)

- `Universe` + SS-LAW-15 scoping (`v3/Prose.Core/Data/Entities/Universe.cs`,
  global query filter in `ProseDbContext`).
- Generic `Entity` spine + typed subtypes (`Character`, `Place`, `Faction`,
  `Species`, `Gear`, ...) + runtime `RepositoryDefinition` for new types with
  zero migrations.
- `Edge` typed relation graph, `Tag`, `Record` JSON round-trip blob.
- Hub-only DB access (HARD RULE) with reflection dispatch:
  CLI handlers `v3/Prose.Cli/Cli/*Cli.cs` via `HubCliClient.ForwardAsync`,
  MCP tools `v3/Prose.Mcp/Tools.*.cs` via `HubInvoker` → `{Method}Impl`.
- Hub HTTP reads: `GET /api/universes`, `/api/universes/{slug}/entities/{id}`,
  `/neighbors/{id}`, `/search`, `/snapshot`.

## The interchange format (the contract with other apps)

File: `<app>/universe/<slug>.universe.json`, schema
`D:\Projects\MindAttic\ExperimentEve\universe\universe.schema.json` (copy it to
`docs/schemas/universe.schema.json` here so Prose owns a copy).

Shape: `{ universe: {id, name, tagline, era, setting, logline, rules[]},
entities: [{id, type, name, summary, details{}, relations[{to, kind}], tags[]}] }`.

- `entities[].id` is a slug, unique per universe → maps to `Entity.Slug`.
- `type` is an open string set; core: character, faction, creature, location,
  artifact, event, rule, organization, concept.
- Dangling `relations[].to` are allowed (they mark future work): import them
  as edges to auto-created `Status=stub` entities.

First consumer file (75 entities, validated):
`D:\Projects\MindAttic\ExperimentEve\universe\eve.universe.json`

## Work plan (execute in order)

### 1. Register the EVE universe
- New `Universe` row: Slug `EVE`, Name "Experiment Eve".
- `WorldFacts` ← the interchange `universe.rules` expanded to one line each
  (they are the hard creative laws: no-exposition, placeless-winks,
  tech-1998-only, nostalgia-parody, fathers-day-throughline, night-deadline,
  lighthouse-saves, hourly-chimes, routing-is-the-puzzle,
  drip-feed-blueprints, a-life-ecology — full text lives on the `rule`-type
  entities in the file).
- `UniversePrimer` ← `universe.logline` + `setting` + `tagline`.
- Follow whatever the existing seeding convention is for the 7 current
  universes (find where GLMZ/SCRY/etc. are seeded; do it the same way —
  likely a SQL seed via `SqlSeedService` or a service-level ensure).

### 2. `UniverseInterchangeService` (Prose.Core/Services/)
Import/export between the interchange JSON and the Entity spine. Runs inside
the Hub only (like every service).

Type mapping (import):
| interchange `type` | Prose target |
|---|---|
| character | `Character` subtype |
| location | `Place` subtype |
| faction | `Faction` subtype |
| creature | `Species` subtype (fits: chimera species with behavior text) |
| artifact | `Gear` subtype if it fits, else generic `Entity` with EntityType "artifact" |
| event / rule / organization / concept / anything else | generic `Entity` with `EntityType` = the interchange type; ensure a `RepositoryDefinition` exists for each such type (create at import, global, idempotent) |

Field mapping: `id→Slug`, `name→Name`, `summary→Description` (or nearest),
`details` + full source object → `Record` JSON (round-trip source of truth),
`tags[]→Tags`, `relations[]→Edges` (EdgeType = `kind`, create stub targets for
dangling ids), everything `Status=canon` except auto-created stubs.

Semantics: **idempotent upsert by (UniverseId, Slug)** — re-import must be a
no-op diff, updates in the file win over DB fields it maps (Record keeps the
old copy versioned if a versioning convention exists; otherwise overwrite).
Never touch entities in other universes (SS-LAW-15 does this structurally).

Export: inverse mapping back to the interchange shape; entities whose
EntityType has no interchange core type export with their EntityType string
verbatim. Round-trip test: import → export → import produces zero changes.

### 3. CLI (Prose.Cli)
- New handler `v3/Prose.Cli/Cli/UniverseInterchangeCli.cs` (convention:
  `RunAsync(string[] args, IServiceProvider services)`).
- New dispatch lines in `Prose.Cli/Program.cs` (pure additions):
  - `prose --universe-import <path> [--universe <slug>]` (slug defaults to the
    file's `universe.id` uppercased)
  - `prose --universe-export <slug> <path>`
  - `prose --universe-sync <path>` = import, then export back to the same path
    (normalizes the file; the game commits the normalized copy)

### 4. MCP (Prose.Mcp)
New `v3/Prose.Mcp/Tools.UniverseInterchange.cs` (`[McpServerToolType]`,
one-line forwards + `Impl` siblings), only adding what generic tools don't
already cover — audit `Tools.Universe.cs`, `Tools.EntityCrud.cs`,
`Tools.Repository.cs` first:
- `import_universe_file(path, slug?)`
- `export_universe_file(slug, path)`
- `get_universe_entity(slug, entitySlug)` (if no existing equivalent by slug)
- `search_universe(slug, query)` (if not covered)
Regenerate `docs/MCP_TOOLS.md` via the existing `--export-tools`.

### 5. Hub HTTP (wanted for game-side push + the CliHook channel)
- `POST /api/universes/{slug}/import` accepting the interchange JSON body →
  same service. Mirrors the existing read endpoints' style in
  `Prose.Hub/Program.cs`. Keep it localhost like everything else.
- **The Outbox (CliHook channel)** — the Hub feeds information INTO consumer
  Claude Code sessions (ExperimentEve's window first) via hook-friendly pull:
  - Table/store: `OutboxEvent { Id, Consumer, Ts, Kind, Summary, DataJson, DeliveredTs? }`
    (small EF entity + migration, or reuse an existing event mechanism if one
    fits; Hub-only access as always).
  - `GET /api/outbox/{consumer}` → pending events for that consumer, marking
    them delivered (add `?peek=true` to read without consuming).
  - `POST /api/outbox/{consumer}` `{kind, summary, data?}` → enqueue. This
    lets Prose's own Claude session deliberately message the EVE window
    ("GDD chapter 3 drafted — pull barks"), and lets services enqueue
    automatically.
  - Auto-enqueue for consumer `eve` on: interchange import/export completed,
    any entity upsert in the EVE universe, barks export written, any beat
    written in an EVE-universe book. One-line summaries; keep it quiet.
  - Consumer side (already installed in ExperimentEve, fails silent until
    this endpoint exists): `.claude/hooks/prose-outbox.ps1` registered as a
    `UserPromptSubmit` hook — every prompt in the EVE window drains
    `GET /api/outbox/eve` and injects the summaries as context. A matching
    hook can later be added to any other consumer repo (this is the pattern
    that makes Prose a live participant in other projects' sessions).

### 6. Tests (Prose.UnitTests, NUnit, TestDbFactory/SQLite)
- Interchange round-trip (import→export→reimport = no-op).
- Type mapping per row of the table above (one entity each).
- Dangling relation → stub creation.
- Universe segregation: importing EVE twice touches only EVE rows
  (mirror `UniverseSegregationTests` patterns).
- Idempotency: import same file twice → identical DB state.

### 7. Seed EVE and verify
```
prose --universe-import D:\Projects\MindAttic\ExperimentEve\universe\eve.universe.json
prose --universe use EVE
prose --universe-export EVE <temp> && diff against source (allowing normalization)
```
Expected: 75 entities (7 character, 2 faction, 20 creature/species,
30 location/place, 4 artifact, 5 event, 7 rule).

### 8. Documentation
- README: add a "Universe Interchange" subsection under the Universes section
  (what it is, the commands, the contract path, ExperimentEve as consumer #1).
- This RFC marked implemented, with any deviations noted.

## The other side (already built, in ExperimentEve)

- `universe/eve.universe.json` + `universe/universe.schema.json` — the seed.
- `npm run universe -- validate|list|get|search|stats` — local queries.
- `npm run universe -- pull` — GETs `http://127.0.0.1:5900/api/universes/EVE/snapshot`
  into `universe/eve.prose-snapshot.json` (works as soon as EVE exists in Prose).
- `npm run universe -- push` — invokes `prose.cmd --universe-import <abs path>`
  (works as soon as step 3 lands).

## Phase 2 — Prose writes for the game (design now, build after Phase 1)

The insight: **game-writing deliverables are Books.** The existing
Book→Chapter→Beat spine, generation pipeline, continuity ledger, and
character bibles apply unchanged — only the universe (EVE) and the
deliverable's purpose differ. No new hierarchy, no new laws.

Create, in the EVE universe, as ordinary Prose books:
1. **"EVE — Game Design Document"** — nonfiction-structured book; chapters =
   systems (battle, A-Life, crafting, districts...), beats = sections. Seed
   its outline from the entity data (rules + factions + creatures) rather
   than from scratch.
2. **"EVE — Night One (Game Script)"** — the slice's narrative script in
   screenplay-ish beats: scene direction, Kat's barks, Observer interventions,
   ApertureOS file texts. Every speaking/named thing links to its Entity
   (Prose's continuity machinery then guards game canon for free: it can
   catch a bark contradicting a WorldFact like no-exposition or 1998-only).
3. **"EVE — Prequel Novella"** — real fiction, same universe, same canon
   guards; the reason this whole merger exists.

The one new piece of plumbing (small, additive): **bark/dialog export** —
`prose --barks-export EVE <path>` walks a designated script book and emits
`{barkId, speakerEntitySlug, text, context}` JSON the game consumes at build
time (barkId = a stable per-beat key). ExperimentEve will read it from
`universe/eve.barks.json`. Design the key convention in Phase 2; do not build
generation-side anything new — Prose's normal writing flow authors these
books; the export is just a filter.

## Acceptance
1. `dotnet test v3/Prose.UnitTests` green, including the new tests.
2. All 7 pre-existing universes and every existing command/tool behave
   identically (spot-check `--book`, a generation command, `--universe list`).
3. EVE queryable from Prose CLI, MCP, and Hub HTTP; `npm run universe -- pull`
   succeeds from the ExperimentEve repo.
4. Nothing writes to the DB except through the Hub. No exceptions.

## Phase 1 — implemented 2026-08-26

All four acceptance points verified live:
1. `dotnet test v3/Prose.UnitTests` — 2212 passed, 24 skipped, 0 regressions. 14
   pre-existing failures confirmed unrelated (12 environment-dependent log-search
   tests reading real Serilog files; 2 a structurally pre-existing DI-registration
   test bug in files this RFC never touched — `Prose.Core.Extensions.AddProseServices()`
   cannot register `Prose.Mcp.HubInvoker` since Core has no reference to Mcp).
   16 new tests added (`UniverseInterchangeServiceTests`, `OutboxServiceTests`), all green.
2. Spot-checked `--universe list` (all 8 universes, including the 7 pre-existing,
   listed correctly) and `--book`/generation commands untouched.
3. EVE live: 75/75 entities imported (7 character, 2 faction, 20 creature, 30
   location, 4 artifact, 5 event, 7 rule — matches the file exactly), 38 edges,
   0 stubs (the file's cross-references are internally complete). Verified via
   CLI (`--universe-import/-export/-sync`), MCP (`get_universe_entity`,
   `search_universe` via direct `/api/mcp-invoke` calls), and Hub HTTP
   (`POST /api/universes/eve/import`, `GET/POST /api/outbox/eve`). Import →
   export → diff against source: 75/75 entities present, 0 relation diffs, 0
   universe-metadata diffs. `npm run universe -- pull` from ExperimentEve
   succeeds (exit 0, writes `eve.prose-snapshot.json`) — see the noted gap below.
4. All writes went through the Hub (`prose` CLI → `HubCliClient` → `/api/cli-invoke`,
   or a direct MCP-shaped `/api/mcp-invoke` call, or `/api/universes/eve/import`).
   No raw SQL was run against the database at any point in this implementation.

### Deviations from the RFC's suggested design (all minimal, documented in code)

- **Storage model**: every interchange entity — including `character`/`location`/
  `faction` — is stored on the generic Entity + `Record.Json` + `EntityTag` + `Edge`
  spine (`EfRepository`-style), never on the fully-relational `Character`/`Place`/
  `Faction` typed tables (`CharacterMapper`/`PlaceMapper`/`FactionMapper`). Those
  mappers are built around Prose's own ~15–25-bridge-table domain model and
  explicitly stopped reading `Records.Json` years before this RFC; forcing sparse
  game-entity data through that machinery would be fragile for no benefit. The RFC
  itself designates `Record.Json` as the "round-trip source of truth" for
  import/export, which is exactly the generic-spine model. `EntityType` strings
  still follow the RFC's semantic mapping for readability. See
  `UniverseInterchangeService`'s class doc-comment.
- **`creature` type mapping**: NOT routed to the `Species` table as suggested —
  `Species` is a 5-row controlled vocabulary (human/ai/elf/synthetic/unknown) that
  `Character.Species` references by name, not a per-instance table for dozens of
  individual creatures. Creatures get a generic, `RepositoryDefinition`-registered
  `EntityType` instead (same fallback path the RFC already specifies for
  event/rule/organization/concept).
- **`artifact` type mapping**: NOT split between Gear subtypes and generic Entity.
  None of the file's four artifacts (a yacht, an OS, a keepsake card, a dress)
  cleanly fits Prose's Gear categories as a uniform rule, so all artifacts get a
  uniform generic `EntityType` for consistency rather than cherry-picking two into
  `Transportation`/`Apparel`.
- **Universe slug casing**: registered as `eve` (lowercase), not `EVE` as the RFC's
  Step 1 literally states — every existing universe (`glmz`, `scry`, `gospel`, ...)
  uses a lowercase `Slug` with an uppercase `Name`; the RFC's own Step 1 line
  ("New Universe row: Slug EVE, Name 'Experiment Eve'") conflicts with that
  established convention, so the convention wins. Same for the CLI's
  `--universe-import` slug default: the file's own `universe.id` is used verbatim
  (already lowercase per the schema's `^[a-z0-9-]+$` pattern), not uppercased.
- **Universe-block round-trip**: the RFC doesn't specify where `tagline`/`era`/
  `setting` persist (`Universe` has no matching columns). Stored verbatim as a
  `Setting` row (`Key = "interchange.universe_source"`, scoped to the universe)
  so export reconstructs the exact original `universe` object; falls back to
  deriving from `Universe.Name`/`Description`/`WorldFacts` if that row is ever
  missing (e.g. a universe seeded by hand, not via interchange import).

### Bugs found and fixed along the way (not RFC-scoped, but blocking and fixed per
### project policy — "fix bugs immediately, never deferred")

1. **`CliDispatch` param binder silently passed `null` args** to any CLI handler
   typed `IReadOnlyList<string> args` instead of the concrete `string[]` (affects
   `SeedCli`, `ResetPasswordCli`, `VulturesSeedCli`, `AuditDenormCli`) — found via
   `prose --seed`, which has apparently been broken via the Hub-forwarding path
   since the Stage C CLI migration. Fixed by broadening the type check to
   `pt.IsAssignableFrom(typeof(string[]))`.
2. **`SqlSeedService` never refreshed `IUniverseContext`** after a successful run.
   A Hub-resident process caches its universe catalog for its whole lifetime; a
   freshly-seeded universe (EVE, or any future one) was invisible to
   `--universe list`/`--universe use` until the Hub restarted. Fixed by calling
   `IUniverseContext.Refresh()` after every successful seed run.
3. **`UniverseInterchangeService`'s own `UpsertUniverseSourceAsync`** was missing
   `.IgnoreQueryFilters()` on its `Settings` lookup (`Setting` has a shared-visibility
   query filter) — under any non-empty ambient `UniverseScope` that wasn't the
   target universe (the normal case for a live Hub), a re-import's lookup silently
   missed the existing row and crashed on a duplicate-key insert. Caught live via
   the Hub (not by the original unit tests, which never set an ambient scope) and
   fixed; a regression test (`ImportAsync_ReimportUnderNonEmptyAmbientUniverseScope_DoesNotThrow`)
   now sets a non-empty ambient scope explicitly so this class of bug can't hide again.

### Known integration gap (not fixed — outside this repo)

`npm run universe -- pull` (ExperimentEve's own script) calls
`GET /api/universes/EVE/snapshot` with no `scope` parameter, which returns the
DCM *active working set* (what's currently resident for live prose generation —
correctly empty, since Prose has never generated EVE prose yet), not the full
universe dump. A full pull needs `?scope=all`. This is a one-line change in
`ExperimentEve/scripts/universe.mjs`, in a sibling repo this RFC's mandate didn't
cover — flagged for the user/ExperimentEve session rather than edited here.

### Pre-existing `UniverseGraphService` bugs found during cross-session
### verification (real, NOT caused by RFC 0007, deliberately NOT fixed here)

The ExperimentEve session bridge-tested against the *pre-existing* `/api/universes/{slug}/
search` and `/snapshot` endpoints (built on `UniverseGraphService`, not on this RFC's own
Entity-spine reads) and reported two symptoms. Both were verified directly against the DB
before touching anything — neither is a defect in `UniverseInterchangeService`:

1. **Not a bug**: `/search` shows `katie-kat-weiss` instead of the interchange id `kat-weiss`.
   `Entity.Slug` is stored correctly as `kat-weiss` verbatim (confirmed via `get_universe_entity`
   reading the DB column directly). `UniverseGraphService` computes its OWN graph-node id by
   re-slugifying `Entity.Name` at load time (`Slugify(en.Name)`, `UniverseGraphService.cs` — every
   bespoke per-type node builder does this) — it never reads the stored `Entity.Slug` column.
   This is universal, long-predates this RFC, and affects every universe (a GLMZ entity whose
   `Entity.Slug` differs from `Slugify(Entity.Name)` would show the same divergence). Out of scope
   to change unilaterally — it's a shared service with real blast radius (19k+ live GLMZ nodes).
   **Use the RFC 0007 tools for interchange data instead**: MCP `get_universe_entity`/
   `search_universe`, or CLI `--universe-export` (both read `Entity.Slug` directly, confirmed correct).
2. **Real bug, not RFC 0007's**: every node's `edgeCount` field in `/snapshot` and `/search`
   reads 0, even though the raw `Edges` table is fully correct (38 rows, both directions verified,
   e.g. `soak`↔`ren`) and the SAME snapshot response's top-level `edges` array is also fully
   correct (38 entries, correct source/target). Reproduces identically after a full Hub restart
   (rules out staleness/caching timing). GLMZ (populated mostly via older bespoke per-type
   relationship builders, not the generic `Edges` table) shows correct non-zero `edgeCount`s, so
   the bug appears specific to edges loaded via `UniverseGraphService.BuildEdgesFromSqlTable`
   (the generic-`Edges`-table path) versus the bespoke per-type builders. Not root-caused further
   — it's deep in a shared graph-build/caching service touching every universe, genuinely risky to
   patch blind, and outside this RFC's acceptance criteria (which this RFC satisfies via its own
   Entity-spine reads, confirmed correct independent of this service). **Flagged for the user**,
   not fixed here.
3. **Real, serious bug — cross-universe data leak in `/snapshot` and `/search`.** A rebuild of
   EVE's graph (forced by an `EnsureFresh()` fix earlier this session) returned 5,175 nodes /
   5,273 edges instead of the correct ~97/~58, including 723 `weapon`, 2,129 `technology`, 155
   `organization`, and 2,071 `unknown`-typed nodes — types that don't exist anywhere in EVE's
   interchange schema. This is GLMZ's (and/or other universes') data leaking into EVE's graph.
   Reproduced consistently across multiple isolated calls, not a one-off race.

   Two confirmed, compounding defects in `UniverseGraphService.cs`:
   - `BuildCharacters`/`BuildDistricts`/`BuildFactions`/`BuildCorponations`/`BuildWeaponry`/
     `BuildEquipment`/`BuildTechnology`/`BuildRemainingEntities` (`BuildFromDatabase`, line
     1105) read `db.<Property>`/`ctx.Entities` with no explicit
     `.Where(x => x.UniverseId == state.UniverseId)` — unlike `BuildEdgesFromSqlTable` (line
     1133), which already treats that explicit filter as required "belt-and-suspenders"
     alongside the ambient EF query filter, in its own doc comment.
   - The seven legacy `EfRepository<T>` singletons behind `db.Characters` etc.
     (`CharacterRepository`, `DistrictRepository`, ... — `Repositories.cs`, `AddSingleton` in
     `ServiceCollectionExtensions.cs:192-201`) each cache `GetAll()` into a single shared
     instance field keyed only by `UniverseScope.Epoch`, a process-wide counter bumped by every
     universe switch — not by which universe the cache actually holds. One cache slot, shared
     across every universe and every concurrent request.

   **FIXED (2026-08-26).** Live repro at the time of writing confirmed the second defect —
   the singleton cache — as the actual, sufficient root cause for the leak: `BuildCharacters`
   etc. read through `CharacterMapper.LoadAllFromReadModel`/`PlaceMapper.LoadAll`/etc., which
   already query `db.Entities`/`CharacterReadModels` under EF's ambient `HasQueryFilter` — so
   those reads are correctly scoped *whenever they actually execute*. The leak was the cache
   short-circuiting before that ambient-filtered query ever ran, serving whichever universe's
   rows happened to be cached under the last epoch bump, regardless of which universe the
   current request/rebuild was actually scoped to. Rewriting the eight builders to read
   `ctx.<Table>` directly (the fix originally proposed here) turned out to be the wrong shape
   for the six that consume rich Mapper-built DTOs (`CharacterData`/`DistrictData`/etc. carry no
   `UniverseId` field to filter on, and none of them expose the ~25-Include enrichment those
   Mappers already do) — replacing the data source would have meant re-deriving that enrichment
   from scratch, a much larger and riskier change than the actual defect warranted.

   Applied instead: each of the seven `EfRepository<T>` singletons behind `db.Characters`/
   `Districts`/`Factions`/`Corponations`/`Weaponry`/`Equipment`/`Technology`
   (`v3/Prose.Core/Services/Repositories.cs`) now keys its `GetAll()`/`GetAllLite()` cache by
   `UniverseScope.EffectiveId` in addition to the existing `UniverseScope.Epoch` check — a
   `mappedCacheUniverseId`/`mappedCacheLiteUniverseId` field set alongside the existing epoch
   field, checked in the same condition. Strictly additive (only ever causes *more* cache misses
   than before, never serves a result the old check would have rejected), so it carries no
   correctness risk to the existing epoch-based invalidation. `BuildRemainingEntities` (the one
   builder that already reads `ctx.Entities` directly, no repository cache involved) also picked
   up the same explicit `.Where(x => x.UniverseId == state.UniverseId)` belt-and-suspenders guard
   `BuildEdgesFromSqlTable` already used, for consistency.

   Verified live: `dotnet run --project Prose.Cli -- --universe eve --rebuild-graph` → 97
   nodes / 58 edges (was 5,175/5,273); `GET /api/universes/eve/snapshot?scope=all` confirms the
   same 97/58 with only EVE-native node types (`creature`/`place`/`character`/`artifact`/`rule`/
   `event`/`faction`/`concept` — no `weapon`/`technology`/`organization`/`unknown`). GLMZ's own
   graph re-checked unaffected: `GET /api/universes/glmz/stats` still reports 19,239 nodes /
   19,185 edges, matching its pre-fix state. Full unit suite for the touched services
   (`UniverseGraph`/`WorldGraph`/all seven repositories) passed, 0 regressions.

   **Follow-up FIXED (2026-08-26):** the identical `mappedCache`/`mappedCacheEpoch` pattern
   (cache keyed by a global switch counter, not by universe) repeated in every other
   `EfRepository<T>`-derived repository in `Repositories.cs` (22 more types — Ammunition,
   Cyberware, Vocabulary, Genemod, Transportation, Contract, Automaton, Subsidiary,
   Entertainment, Apparel, News, Archetype, Material, Pharmaceutical, ConsumerGood, Quote,
   LabSpecimen, FlyoverEntity, Psionic, SyntheticLife, Motif, WorldbuildingDoc). None of those
   feed `UniverseGraphService` today, so they were outside this bug's reproduced blast radius,
   but carried the exact same latent defect for any future multi-universe caller. Applied the
   same `mappedCacheUniverseId`/`mappedCacheLiteUniverseId` fix to all 22 remaining types
   (44 sites: field + check + assignment × 2 caches × 22 classes), via literal-pattern
   `replace_all` since every repository shares byte-identical cache-check/assignment lines.
   All 29 `EfRepository<T>` types in `Repositories.cs` now carry the universe-scoped cache guard.
   `dotnet build Prose.Core` clean, 0 warnings/errors.

### First outbox event

Enqueued per the handoff's step 2 (left undelivered so ExperimentEve's own
`UserPromptSubmit` hook drains it naturally on its next prompt):
`POST /api/outbox/eve {"kind":"hello","summary":"EVE universe live in Prose: 75 entities. Pull when ready."}`
