# RFC 0007 — Universe Interchange & the EVE Universe

Status: ACCEPTED (user-directed, 2026-08-26) — execution keyword: `/eve` in the Prose CLI
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
