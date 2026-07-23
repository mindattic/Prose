# StreetSamurai

**A literary fiction engine for a cyberpunk century.**

Live at **[streetsamurai.azurewebsites.net](https://streetsamurai.azurewebsites.net/)**.

> **[docs/BIBLE.md](docs/BIBLE.md) is the architecture bible** (Codex L0). [docs/AMENDMENTS.md](docs/AMENDMENTS.md) is the append-only change log (an amendment wins over the bible). [docs/USER_STORIES.md](docs/USER_STORIES.md) is the goal table with acceptance tests. [docs/rfc/](docs/rfc/) holds design notes. **This README is the engineering tour.**

---

## Table of contents

- [What StreetSamurai Is](#what-streetsamurai-is)
- [Architecture Overview](#architecture-overview)
- [Prose Engine Pipeline](#prose-engine-pipeline)
- [CLI Reference](#cli-reference)
- [Database](#database)
- [Running Locally](#running-locally)
- [GPU Entity Scoring](#gpu-entity-scoring)
- [Code Style Rules](#code-style-rules)
- [How it works: seed to published](#how-it-works-seed-to-published)
- [The subsystems](#the-subsystems)
- [Cost tiering](#cost-tiering)
- [The UI surface](#the-ui-surface)
- [The MCP surface](#the-mcp-surface)
- [Stack at a glance](#stack-at-a-glance)
- [Repository layout](#repository-layout)
- [Database migrations](#database-migrations)
- [Deploying to Azure](#deploying-to-azure)
- [Tests](#tests)
- [Patent Disclosures](#patent-disclosures)
- [Status](#status)

---

## What StreetSamurai Is

StreetSamurai is a C#/.NET 10 Blazor Server application — web-only, fully responsive — that takes a one-line seed to a published, reader-reviewed, canon-consistent audiobook and manuscript. It is the authoring stack for *Bushido Coda* and a hundred stories beyond it, set in the GLMZ: a 500-kilometer vertical megacity stacked along the western shore of Lake Michigan in 2225, where ferrocement waves rise a hundred stories above the lake and CorpoNations hold sovereignty the old nations could not. Canon is a database, not a folder of files. SQL Server holds 12,000+ named entities — characters, places, factions, CorpoNations, weapons, biotech, nano, documents — bound by a directional graph of relationships, vector embeddings, and a two-axis time model. On top sits the writing surface: a typed Node tree (series / story / chapter) over Beats spanning 36 stories and 2,443+ beats with an outline-first workflow, a multi-provider review pipeline (Legion-backed, 11 LLM providers), storytelling-science analysis, a deterministic continuity suite, and an export pipeline (Word / EPUB / PDF / Markdown / audiobook). A reader-review panel of synthetic personas measures reception from data, not vibes. For agentic authoring, an MCP server exposes the entire canon to Claude — Desktop, Code, or any MCP client — so the model can call the world directly. The human approves canon and voice changes; the engine writes.

---

## Architecture Overview

```
CLI (dotnet run --project v3/StreetSamurai.Cli -- <args>)
  │
  ▼
Blazor Server host  (v3/StreetSamurai.Blazor/)
  │  Web UI (Razor components)
  │
  ▼
Core services  (v3/StreetSamurai.Core/)
  │  ~214 services: prose generation, review, continuity, export, embeddings
  │  EF Core → SQL Server (LocalDB in dev, Azure SQL in prod)
  │
  ▼
Database  (SQL Server)
  │  12,000+ entities · 36 stories · 2,443+ beats · vector embeddings
  │  System-versioned (temporal) Beats + Nodes tables
  │  EntityStateEvents ledger · Edges graph · ContinuityClaims
```

### Prose Engine Pipeline

`ProseWriterRouter` is the sole entry point for beat prose generation (SS-A16). Call it — never `BeatGeneratorService` directly.

```
ProseWriterRouter.WriteAsync(context, beatId, beatIndex, totalBeats)
```

The router coordinates this enrichment chain before handing off to `BeatGeneratorService`:

| Service | What it injects | Activation condition |
|---|---|---|
| `BeatModeDetector` | Classifies beat as Combat / Narrative / EmotionalClimax / Dialogue / Transition / Revelation | Keyword scan on `BeatGoal` |
| `PacingService` | BREATHE / FLOW / TIGHTEN / STRIKE / SETTLE prose rhythm | Position in story + BeatGoal keywords; Combat forces STRIKE |
| `StoryMethodologyService` | Save the Cat structural role (Opening Image → Final Image) + Scene-Sequel type | Position in story |
| `PlantPayoffService` | Active plant/payoff pairs seeded into context | `BeatContext.NodeId != Guid.Empty` |
| `StoryAuditService` | Gateway or Sequel commandments (7 each, auto-detected from `PreviousNodeId`) | `BeatContext.NodeId != Guid.Empty` |
| `CombatProseGuidance` | Verbs-first, fragment sentences, no emotion-naming, Dissociated Observer examples | `BeatMode.Combat` |
| `SceneContextBuilder` | Ambient sensory palette from carried/worn gear | `BeatContext.Location` is set |
| `DialogueService` | Per-character voice registers | `BeatMode.Dialogue` or `EmotionalClimax`; `CharactersInScene` set |
| `EmotionalDepthService` | Prior examination findings injected as generation constraints | Feedback loop from prior beat scores |
| `TensionEscalationService` | Warns when consecutive non-escalating beats detected | Beat sequence analysis |
| `ReaderKnowledgeService` | Current reader knowledge state for dramatic irony management | Always |
| `ConsequenceService` | Character gear, cyberware, and status constraints — zero LLM cost | `CharactersInScene.Count > 0` |
| `ConsequenceEngine` | Cross-story persistent consequences (contract outcomes, faction burns) | `CharactersInScene.Count > 0` |
| `AmbientAnomalyService` | New Weird ambient background detail tagged to scene location (60% chance gate) | `BeatContext.Location` is set |
| `WorldStateAtBeatService` | Live temporal entity state snapshot from `EntityStateEvents` (drifted, not canon baseline) | `beatId != Guid.Empty` |
| `NarrativeSummaryService` | Rolling compressed scene memory — last 10 beats for long-story coherence | `BeatContext.NodeId != Guid.Empty` |
| `WorkflowMonitorService` | Logs per-beat service coverage to `BeatServiceLog` | Always |
| `BeatModeDetector` | Persists mode classification to `BeatModeLog` | Always |

#### Entry points

| Path | When to use |
|---|---|
| `ProseWriterRouter.WriteAsync(context, beatId, beatIndex, totalBeats)` | All beat writing from UI + CLI |
| `CombatSceneWriter.WriteCombatSceneAsync(request)` | Explicit multi-exchange combat setpiece (numExchanges > 1, full loadout tracking) |
| `BeatGeneratorService.GenerateBeatAsync(context)` | Legacy path — direct generation without coverage logging |

#### Beat writing workflow

1. Assemble `BeatContext` (X-Ray context via `SceneContextAssembler`, `NodeId` always set)
2. Call `ProseWriterRouter.WriteAsync(context, beatId, beatIndex, totalBeats)`
3. After writing, run `ss --examine-emotion --slug <slug>` (8-dimension emotional scoring)
4. After enough beats scored, run `ss --update-register-exemplars --slug <slug>` (update voice register)
5. After story complete, run `ss --story-audit --slug <slug>` (gateway/sequel commandments)
6. After story complete, run `ss --plant-audit --slug <slug>` (orphan plant check)

#### Coverage monitoring

```powershell
ss --workflow-status --slug <slug>   # per-story service coverage matrix + gaps
ss --workflow-status --all           # global utilization across all stories
```

MCP equivalents: `workflow_status`, `workflow_status_global`, `workflow_beat_modes`

### MCP server

`StreetSamurai.Mcp` exposes 195+ `[McpServerTool]` methods across 23 tool-class files. The complete per-tool reference is [docs/MCP_TOOLS.md](docs/MCP_TOOLS.md), generated by reflection and auto-regenerated by the pre-commit hook.

Register in Claude Code:

```
claude mcp add streetsamurai dotnet run --project D:/Projects/MindAttic/StreetSamurai/v3/StreetSamurai.Mcp/StreetSamurai.Mcp.csproj --no-build --configuration Release
```

---

## CLI Reference

The canonical invocation alias `ss` expands to:

```powershell
dotnet run --project v3/StreetSamurai.Cli -- <args>
```

All ~152 handlers are dispatched from `v3/StreetSamurai.Cli/Program.cs`.

### Key commands

| Command | What it does |
|---|---|
| `ss --review-story --slug <slug> [--effort draft\|standard\|deep]` | Multi-persona scored review; targets ≥82% standalone, ≥85% cumulative |
| `ss --examine-emotion --slug <slug>` | 8-dimension per-beat emotional scoring (0–4 per dimension) |
| `ss --story-audit --slug <slug>` | Audits gateway/sequel commandments for the story |
| `ss --plant-audit --slug <slug>` | Checks for orphaned plants (planted but never paid off) |
| `ss --export-node [--slug <slug>]` | Exports docx + EPUB + PDF + audio manuscript `.txt`; prunes stale versions; lands in `{Title}/V{N}/` |
| `ss --workflow-status --slug <slug>` | Per-story service coverage matrix showing which pipeline services fired and which are gaps |
| `ss --workflow-status --all` | Global utilization matrix across all stories |
| `ss --generate-cover --story-code CODE --generator NAME --prompt "TEXT"` | AI cover generation via ChatGPT / Gemini / Ideogram / Flux; stores prompt in `CoverImagePrompts`, asset in `Assets` |
| `ss --import-cover --file PATH [--story-code CODE] [--type TYPE]` | Import a local image into the cover asset library |
| `ss --audit-story [--deep] [--model haiku]` | Repeatable full-QA orchestrator — structural + prose + continuity |
| `ss --diagnose-story --slug <slug>` | 12-check structural pre-flight without full review cost |
| `ss --expand-beat --slug <slug>` | Expand a single beat using the full enriched pipeline |
| `ss --edit-story --slug <slug>` | Review-weighted auto-editor pass |
| `ss --reflow-story --slug <slug>` | Bounded copy-edit: fixes paragraph breaks, question marks, dialogue attribution without rewriting |
| `ss --check-fidelity --slug <slug>` | Cosine-similarity check of prose vs. story bible/synopsis |
| `ss --check-prose --slug <slug>` | Regex linter for banned prose patterns (pseudo-profound, cliché, on-the-nose) |
| `ss --check-canon --slug <slug>` | Chunked semantic + LLM sweep for canon rule violations |
| `ss --timeline-check --slug <slug>` | Deterministic dead-character-acting + wound-regression checks |
| `ss --assemble-scene --slug <slug>` | X-Ray scene context assembly |
| `ss --narrative-science --slug <slug>` | Sacred-flaw / five-act / scene-engagement analysis |
| `ss --continuity --slug <slug>` | Extract and validate continuity claims against live entity state |
| `ss --backfill-coverage --slug <slug>` | Non-destructive: logs router coverage for existing beats from synopsis without regenerating |
| `ss --doc-context --slug <slug> [--goal "TEXT"]` | Dry-run the Doc Context Stack — shows which docs would load for a beat |
| `ss --sync-markdown` | Sync Codex `.md` files from disk to DB (auto-classifies into always/node/topic tiers) |
| `ss --rebuild-graph` | Rebuild in-memory entity relationship graph from SQL |
| `ss --reembed` | Re-embed all entities via OpenAI `text-embedding-3-small` |
| `ss --review-entity --id <guid>` | Legion-persona quality review of a single canon entity |
| `ss --add-character` / `--add-place` / `--add-corponation` | Seed a new typed entity into the DB |
| `ss --migrate-sql` | Apply pending hand-written T-SQL schema migrations |
| `ss --universe <id>` | Set the active universe scope (GLMZ / Fantasy) for subsequent commands |

#### Local / rented-GPU prose generation

Route `--expand-beat` to any OpenAI-compatible endpoint without mutating `appsettings.json`:

```powershell
ss --expand-beat --slug <slug> `
   --local-url  https://<runpod-pod-id>-11434.proxy.runpod.net/v1/chat/completions `
   --local-model qwen2.5-32b-writer
```

Flags: `--local` (use stored `LocalLlmBaseUrl`), `--local-url <url>` (override endpoint), `--local-model <tag>` (override model), `--local-key <key>` (override bearer token). All overrides are ephemeral — they never mutate `appsettings.json`. The full enrichment pipeline (X-Ray context, BeatModeDetector, PacingService, story-bible injection) applies regardless of endpoint.

---

## Database

### Connection

```
Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;
```

Windows Authentication — no `-U`/`-P` needed. Same as `appsettings.json` → `ConnectionStrings:StreetSamurai`.

### Direct queries (read-only lookups)

For story lists, scores, entity counts, and other read-only lookups, query the DB directly — returns in under a second:

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -d StreetSamurai -Q "SELECT Name, Score FROM Nodes WHERE IsCanon = 1 ORDER BY Score DESC"
```

Only use `dotnet run --project v3/StreetSamurai.Cli -- <args>` when the CLI's business logic is actually needed (write operations, generation, publish, review). Never use it just to answer a lookup question.

### Schema reference

The DB schema is documented at `docs/schema.md` (222 tables), generated by `tools/gen-schema.ps1`. Regen after any migration.

### Key tables

| Table | Role |
|---|---|
| `Entities` | Universal spine — one row per entity: `Id, Name, Slug, EntityType, Description, IsActive, UniverseId` |
| `Beats` / `Beats_History` | System-versioned (temporal) prose beats — every edit rewindable via `FOR SYSTEM_TIME ALL` |
| `Nodes` / `Nodes_History` | System-versioned story metadata + score |
| `EntityEmbeddings` | 1536-d vectors (OpenAI `text-embedding-3-small`); cosine via `VECTOR_DISTANCE` |
| `Edges` | Typed directed relationships (`carries`, `wields`, `member_of`, `located_at`, …) with validity windows |
| `EntityStateEvents` | Append-only in-world story-time ledger: `(entity, predicate, value, beatId, storyTime)` |
| `ContinuityClaims` | Extracted `(entity, predicate, object)` claims from prose |
| `Findings` | Autonomous quality findings inbox — approve or dismiss before any prose edit |
| `VoiceChangeLog` | Proposed voice-rule changes harvested from winning stories |
| `PlantPayoffs` | Plant/payoff pair registry with `Hidden`/`Visible`/`Meta` transparency |
| `BeatServiceLog` | Per-beat service coverage log (which pipeline services fired) |
| `Assets` / `CoverImagePrompts` | Cover image library + generation prompt history |

### Connection string resolution order

1. Environment variable `ConnectionStrings__StreetSamurai`
2. `appsettings.json` → `ConnectionStrings:StreetSamurai`
3. LocalDB fallback (dev default)

---

## Running Locally

Prerequisites: .NET 10 SDK, SQL Server LocalDB, at least one LLM provider API key wired through `MindAttic.Vault`.

```powershell
cd v3
dotnet restore
dotnet run --project ApplyMigrations   # apply schema + seed data

dotnet run --project StreetSamurai.Blazor
# -> https://localhost:7103/
```

Run the MCP server as its own host:

```powershell
dotnet build StreetSamurai.Mcp/StreetSamurai.Mcp.csproj -c Release
dotnet run --project StreetSamurai.Mcp --no-build --configuration Release
```

Run tests:

```powershell
dotnet test v3/StreetSamurai.UnitTests   # NUnit + bUnit

npx cypress run     # headless e2e
npx cypress open    # interactive
```

After editing any `docs/*` Codex file, run:

```powershell
powershell -File tools/codex.ps1 digest
powershell -File tools/codex.ps1 doctor   # must pass
```

---

## GPU Entity Scoring

The engine supports routing prose generation to any OpenAI-compatible endpoint — including Ollama running on a rented GPU (RunPod, vast.ai) — for cost-effective parallel writing experiments or high-throughput beat generation.

### RunPod setup

1. Deploy an `ollama/ollama:latest` pod with port 11434 exposed and `OLLAMA_HOST=0.0.0.0` set in the environment.
2. Pull and tag your model via the web terminal (e.g. `ollama pull qwen2.5:32b-instruct-q4_k_m`).
3. Use the proxy URL in `--expand-beat`:

```powershell
ss --expand-beat --slug <slug> `
   --local-url  https://<runpod-pod-id>-11434.proxy.runpod.net/v1/chat/completions `
   --local-model qwen2.5:32b-instruct
```

An L40S 48 GB at ~$1/hr handles `qwen2.5:32b-instruct` (Q4_K_M) comfortably with 32k context.

### Entity review scoring

`EntityRatingService` and `EntityReviewService` run Legion-persona quality voting on canon entities in a distributed queue (10 ballots per entity across the ~10,532 entity corpus). Trigger via:

```powershell
ss --review-entity --id <entity-guid>
```

MCP equivalent: the `Tools.Quality.cs` family (`review_story`, `scan_chapter_quality`, `check_prose`).

### NxR parallel-writing experiment

The first documented parallel-writing experiment (NxR story) ran Claude Opus vs. Qwen2.5:32b on the same story bible with the `--local-url` / `--local-model` flags. Both writers received identical X-Ray context, BeatModeDetector classification, PacingService rhythm, and story-bible injection. See `docs/strands/NxR.md §9`.

---

## Code Style Rules

From `CLAUDE.md` — enforced across the codebase:

| Rule | Detail |
|---|---|
| **Private field naming** | `camelCase` without underscore prefix. `count`, not `_count`. |
| **Data files** | JSON only. No Python scripts, no YAML, no new Markdown files (Codex `docs/*.md` are the exception). |
| **Host model** | Blazor Server. Web-only. No MAUI host. |
| **EF Core null-conditional** | `?.` and `?[]` are **not allowed inside EF Core expression-tree lambdas** (CS8072). Project the scalar before the terminal operator: `g.OrderByDescending(h => h.RecordedAt).Select(h => h.MeanScore).FirstOrDefault()`, not `g.OrderByDescending(...).FirstOrDefault()?.MeanScore ?? 0`. |
| **QUANTA symbol** | `Φ` is the QUANTA currency symbol — never "phi", never the Greek letter. |
| **Iowan Behemoths** | Autonomous machines, not synthetic life. They are not alive. |
| **Character heritage** | Default to mixed heritage from unexpected global combinations (Ubiquitous Diaspora). |
| **Versioning** | Whole-number only: `1.0.0`, `2.0.0`, `3.0.0`. No semver-style minor/patch bumps. |
| **Prose entry point** | All beat writing goes through `ProseWriterRouter.WriteAsync`. Never call `BeatGeneratorService` directly from new code. |
| **Canon storage** | Canon facts go into SQL Server. Never write new entity data into `engine_data/*.json` files or Markdown. |
| **CorpoNation spelling** | Conjoined capitals in prose and UI (e.g. `MitsuDyne`, `AgroCore`). Code identifiers unchanged. |
| **E.L.F. spelling** | Always `E.L.F.` with periods. Gloss on first mention in prose. |

---

## How it works: seed to published

Every story or book follows this sequence (codified in `CLAUDE.md` and `docs/BIBLE.md §10`):

1. **Docs first.** Append world facts to `docs/AMENDMENTS.md`; add story entry to `docs/USER_STORIES.md`; run `powershell -File tools/codex.ps1 doctor`. A fact lives in exactly one Codex layer, cited by stable `{#SS-…}` id.
2. **Entity seeding.** Every named character, CorpoNation, place, and weapon is seeded into SQL **before any prose is generated** (`SqlSeedService`, typed repositories, or MCP `create_*` tools).
3. **Book structure.** Create a book-level story (`kind=book`) plus chapter sub-stories (`kind=chapter`) parented to the book; the 14-beat authorial spine becomes the book story's `seed` text.
4. **Prose draft.** `ProseWriterRouter` is the entry point (SS-A16). It classifies beat mode, assigns pacing rhythm, injects structural role, loads active plant/payoff pairs, audits gateway/sequel commandments, and routes to `BeatGeneratorService` while logging per-story service coverage. Each beat's prompt is grounded by an X-Ray context block (`SceneContextAssembler`).
5. **Reflow.** A bounded copy-edit pass fixes paragraph breaks, question punctuation, and dialogue attribution without rewriting prose (`ProseReflowService`).
6. **Review (dual, mandatory).** A structural pre-flight runs first (`StructuralDiagnosticService`), then N Legion personas cast scored ballots (`NodeReviewService`). Per-story standalone must clear ≥82%; cumulative prefix must hold ≥85%.
7. **Continuity scan.** Claims extracted from prose validated against live entity state (`ContinuityExtractionService`, `ContinuityValidatorService`). Deterministic checks catch timeline and location impossibilities.
8. **Export.** KDP-ready Word `.docx`, EPUB 3, PDF, and Markdown all land in a `{Title}/V{N}/` subdirectory (`DocxExportService`, `ManuscriptExportService`); optionally a one-pass audiobook (`ElevenLabsTtsService`).

Prose is **never** written before steps 1 and 2 are complete.

---

## The subsystems

> ~214 services in `v3/StreetSamurai.Core/Services/`, grouped by subsystem.

### 1. Canon & data layer

| Service | Role |
|---|---|
| `BookRepository` / `ChapterRepository` / `SeriesRepository` / `UserRepository` | CRUD repositories for their entity types |
| `RepositoryDefinitionService` | Runtime-defined custom entity repositories (slug, icon, route) |
| `BookOutlineService` | Builds ordered chapter/beat outlines for a book |
| `LoreService` | Canonical lore lookup — pulls character/world facts for prompt grounding |
| `CanonGroundingService` | Injects grounded entity dossiers into LLM prompts |
| `CanonRetrievalService` | RAG-style semantic + graph lookup over canon for context assembly |
| `WorldStateLedger` | Append-only event log powering state-at-beat queries |
| `DataConsistencyService` | Audits FK and relational integrity across entity tables |
| `SqlSeedService` | Applies canonical SQL seeds from C# |
| `CanonExportService` / `ExportService` | Export canonical entity data to JSON/zip |

### 2. Embeddings & semantic retrieval

| Service | Role |
|---|---|
| `EmbeddingService` | OpenAI `text-embedding-3-small` vector store; SHA-256 drift detection; cosine NN search over ~10k entities. Use `FindSimilarAsync(text, k, types?)` — never substring matching |
| `SemanticIndexService` | Low-level embedding index management (upsert, prune, re-index) |
| `GlobalSearchService` / `GlobalSearchWarmupService` | Combined full-text + semantic search; background preheater |
| `SemanticFidelityService` | Goodhart-drift check — cosine similarity of beat prose vs. story bible/synopsis |
| `KnowledgeMapService` | Clusters entities by embedding similarity into knowledge maps |
| `ThematicIndexService` | Tags/indexes beats and stories by theme for retrieval |

### 3. Prose generation

| Service | Role |
|---|---|
| `ProseWriterRouter` | **Sole prose entry point (SS-A16)** — coordinates all services below and logs per-story coverage |
| `DocContextService` / `DocContextStack` | Doc Context Stack — rotating cast of pertinent canon `.md` files (always core + story bible + topic docs triggered by keyword/embedding); gated by `SettingsService.DocContextEnabled` (default off) |
| `BeatModeDetector` | Classifies each beat as Combat / Narrative / EmotionalClimax / Dialogue / Transition / Revelation |
| `CombatProseGuidance` | Injects combat-prose laws: verbs-first, fragment sentences, no emotion-naming, Dissociated Observer |
| `BeatGeneratorService` | **Core generation** — panel of expert personas votes on the best next beat; LLM expands to prose. Tier-locked HIGH |
| `BeatPromptBuilder` | Constructs beat prompts with canon context, voice rules, world state |
| `CombatSceneWriter` | Canon-aware combat prose — tracks loadouts, ammo, bio-battery, terrain |
| `DialogueService` | Dialogue in per-character voice registers |
| `NodeBibleService` | Generates/manages per-story bibles (authorial spine, beat plan, synopsis) |
| `OutlineService` / `StoryMethodologyService` | Generate outlines from a seed; embed five-act / scene-anatomy frameworks into prompts |
| `StoryDirectorService` | Top-level autonomous loop: plan → generate → assess → continue |
| `NpcGenerator` / `DynamicPlaceGenerator` / `ContractGenerator` | Procedural NPCs, places, and in-world contract documents grounded in canon |

### 4. Review & quality

| Service | Role |
|---|---|
| `NodeReviewService` | **Primary review path** — N distinct Legion personas each cast a scored reader ballot; synthesizes an aggregate; round-robins across trusted-4 providers (Claude, OpenAI, Gemini, DeepSeek) |
| `ReviewEffortProfile` | RFC 0009 cost tier (draft / standard / deep) — scales ballot count, prose upgrades, diagnosis, and per-call model selection |
| `ReviewClusterer` | Clusters ballots into Pareto / contested / seam groupings |
| `StructuralDiagnosticService` | 12 parallel structural pre-flight checks before ballots burn |
| `BookReviewService` | Book-level multi-LLM quorum review |
| `WritingQualityService` | **Deterministic** heuristic pass (no LLM): first-line strength, tension delta, motif reuse, voice cadence drift |
| `EmotionalDepthService` | 8-dimension emotional examination at beat level (0–4 per dimension): stakes, interiority, vulnerability, subtext, conflict physicality, reader engagement, thematic resonance, emotional surprise (SS-A15) |
| `FindingsService` | Inbox for all autonomous findings — CRUD + status tracking |
| `FindingApplyService` | Applies a single approved finding (before/after) to a beat |
| `StoryAuditService` | Gateway or Sequel commandment auditing (7 commandments each) |
| `PlantPayoffService` | Plant/payoff pair registry; seeds active plants into prose context; orphan-audit on story completion |

### 5. Storytelling science

| Service | Role |
|---|---|
| `NarrativeScienceService` | Sacred-flaw / theory-of-control analysis, dramatic-question check, 6-point scene-engagement audit, five-act structure map, antihero-empathy check (Will Storr framework) |
| `ArcTrackerService` | Tracks per-story five-act position and beat-level progression |
| `PacingService` | Flags beats that over-stay, under-stay, or repeat an emotional register without escalation |

### 6. Continuity & world-modelling

Mostly deterministic (DB-only, no LLM cost). The seven prose-continuity services split into pre-generation injectors and post-generation validators.

| Service | Role | LLM? |
|---|---|---|
| `EntityRelationshipService` | BFS Edge-graph relationship trees for scene context | No |
| `ProsePatternGuard` | Regex linter for banned patterns (pseudo-profound, cliché, on-the-nose, italicized dialogue) | No |
| `AmbientDetailInjector` | Builds sensory palette from carried/worn gear for pre-gen injection | No |
| `WorldStateAtBeatService` | Point-in-time world-state snapshot from the ledger | No |
| `GearCarryEnforcer` | Post-gen: detects gear-use verbs and checks the carry graph allows them | No |
| `BehavioralInvariantEnforcer` | Post-gen: checks prose against a character's registered behavioral rules | Yes (1/char) |
| `WeaponAmmoCompatibilityService` | Validates weapon+ammo pairs; canonical name+GUID constants | No |
| `TimelineConsistencyService` | Deterministic dead-character-acting + wound-regression checks | No |
| `LocationContradictionService` | Detects a character in two places at once across beats | No |
| `CanonContradictionService` | Chunked semantic + LLM sweep for canon rule violations | Yes |
| `WorldConsistencyService` | Prose scan for world-rule violations (city police, Behemoth-as-alive, …) | Yes |
| `SceneContextAssembler` | X-Ray scene assembly — entity mentions → dossiers → voice + behavioral + science context block | No |
| `WorldGraphService` | In-memory adjacency graph over all entities/edges; rebuilt from SQL | No |
| `ContinuityExtractionService` | LLM+Quorum extraction of `(entity, predicate, object)` claims from prose | Yes |
| `ContinuityValidatorService` | Validate claims vs. live entity state | Mixed |

### 7. Voice & persona

| Service | Role |
|---|---|
| `VoiceHarvestService` | Mines author edits + directives from winning stories (≥80%) into proposed voice rules |
| `ExpertPersonaService` | Manages the reusable expert-persona pool; `SelectPertinentAsync` picks top-N for a scene |
| `ExpertPersonaCatalog` | Curated starter personas — genre experts, craft specialists, die-hard cyberpunk readers |
| `NamePoolService` | Culturally diverse name pools (Ubiquitous Diaspora rule) |

### 8. Memory & editorial lessons

| Service | Role |
|---|---|
| `ProseLessonStore` | SQL-backed store of author rulings (score-vs-function, delight, voice, pacing) scoped global/node/beat; injected into review prompts so reviewers stop penalizing beats already ruled to be doing their job |
| `ActionConfigService` | Per-action LLM tier registry; enforces `ChapterBeatWriter`/`Expander` locked at HIGH |
| `SettingsKvStore` / `SettingsService` | SQL-backed key-value config; app-wide settings façade (models, keys, tone targets, review knobs) |

### 9. Export & publish

| Service | Role |
|---|---|
| `ManuscriptExportService` | All-in-one story export — EPUB 3 + PDF (QuestPDF, 6"×9" KDP trim) + Markdown into one `{Title}/V{N}/` dir |
| `DocxExportService` | KDP-ready Word `.docx` (title page, chapter headers, justified serif) via OpenXml |
| `BookExportService` | Book-level EPUB 3 (Calibre-compatible) + PDF |
| `NodeMarkdownExporter` | Ordered beats → markdown with stable content fingerprint; mirrors `engine/data/exports/` |
| `ElevenLabsTtsService` | ElevenLabs TTS (v2/v3 channels) for narration; tiered fallback |
| `LocalTts` / `PiperTtsService` / `WindowsTtsService` | Local TTS fallbacks (SAPI / Piper) |

### 10. LLM providers & routing

| Service | Role |
|---|---|
| `MultiLlmService` | 11-provider fan-out — Claude, ChatGPT, Gemini, DeepSeek, Mistral, Grok, Groq, Together, OpenRouter, Fireworks, Cohere via `MindAttic.Legion` |
| `LlmRouter` | Routes each request to the right provider/tier from `ActionConfig` |
| `AssignTiersService` | Assigns Haiku/Sonnet/Opus class to actions per settings |

Reviews round-robin across the **trusted-4** (Claude, OpenAI, Gemini, DeepSeek) for diversity. Review personas are die-hard cyberpunk readers, never randos.

### 11. Agentic authoring: Operator + MCP

The **Operator** (`v3/StreetSamurai.Core/Services/Operator/`) runs a Claude tool-use loop that writes chapters end-to-end by calling the world directly.

| Service | Role |
|---|---|
| `WriterOperatorService` | Orchestrates the agentic tool-use loop (hardcoded Opus-class) |
| `AnthropicToolClient` | Anthropic-native tool-call client |
| `ValidateCanonTool`, `DraftCombatSceneTool`, `QueryWorldGraphTool`, `OutlineChapterTool`, `ScoreStoryQualityTool`, `RefineStoryTool`, `ExtractEntitiesTool`, `GetVoiceContextTool`, `RecordCanonChangeTool`, `GetConsequencesTool`, `ProposeStoryEditsTool`, `PredictBehaviorTool` | Individual operator tools — each maps to a Core service |

---

## Cost tiering

RFC 0009. Spend scales to a task's importance. See [docs/rfc/0009-cost-tiered-storytelling-engine.md](docs/rfc/0009-cost-tiered-storytelling-engine.md).

| Tier (`--effort`) | LLM calls | Models | Used for |
|---|---|---|---|
| `draft` | ~6 (−84%) | cheapest per provider (Haiku / Flash-Lite / Nano) | mid-draft spot checks; per-beat iteration — not a gate |
| `standard` | ~15 (−60%) | mid-tier defaults | the per-story standalone gate (≥82%) |
| `deep` | ~37 (baseline) | mid-tier defaults + full diagnosis + prose critique | the cumulative / publish gate (≥85%), flagship |

---

## The UI surface

Live at **[streetsamurai.azurewebsites.net](https://streetsamurai.azurewebsites.net/)** · local dev at `https://localhost:7103/`

Every page is a Blazor Server component — real-time state, no page reloads. Fully responsive at any resolution.

### Writing & story management

| Route | Page | What it does |
|---|---|---|
| `/writer` | Writer | Story picker — the launch pad for writing |
| `/write/{BookId}/{ChapterId}` | Write | Beat-level prose editor — write, expand, reflow, review beats in one screen |
| `/node/{IdOrSlug}` | Story | Single story workbench — beats list, status, review scores, export controls |
| `/nodes` | Stories | Full story index grouped by kind |
| `/books` | Books | Bookshelf — list, create, manage all books |
| `/books/{BookId}/outline` | Book Outline Editor | LLM co-writer for building and editing a book's chapter outline |
| `/series` | Series Shelf | Series listing and management |

### Quality & findings

| Route | Page | What it does |
|---|---|---|
| `/findings` | Findings | Triage inbox — every autonomous quality finding; approve, dismiss, or apply |
| `/continuity` | Continuity | Atomic entity claims; find and resolve prose contradictions |
| `/voice` | Voice Log | Voice-harvest proposals — approve or reject rule candidates |
| `/world-health` | World Health | Live consistency scanner for the world model |

### Canon encyclopedia

| Route | Entity type |
|---|---|
| `/characters` | Characters |
| `/corps` | CorpoNations |
| `/factions` | Factions |
| `/weaponry` | Weapons |
| `/ammunition` | Ammunition |
| `/cyberware` | Cyberware implants |
| `/technology` | Technologies |
| `/pharmaceuticals` | Pharmaceuticals |
| `/genemods` | Genetic modifications |
| `/documents` | World documents |
| `/equipment` | Gear and equipment |
| `/apparel` | Clothing and apparel |
| `/automata` | Automata and machines |
| `/species` | Sentient species |

### World visualization

| Route | Page | What it does |
|---|---|---|
| `/graph` | World Graph (2D) | Interactive 2D entity relationship graph with filters |
| `/graph-3d` | World Graph (3D) | WebGL 3D relationship graph |
| `/atlas` | System Atlas | Visual architecture diagram of the writing engine |
| `/map` | Map View | Interactive GLMZ geographic district map |
| `/timeline` | Timeline | Story-time event timeline with contradiction scan |

---

## The MCP surface

`StreetSamurai.Mcp` exposes **195+ `[McpServerTool]` methods** across 23 tool-class files. The complete per-tool reference is [docs/MCP_TOOLS.md](docs/MCP_TOOLS.md), generated by reflection over the live attributes and auto-regenerated by the pre-commit hook.

```powershell
dotnet run --project v3/StreetSamurai.Mcp -- --export-tools docs/MCP_TOOLS.md
```

| File | Tool family |
|---|---|
| `Tools.cs` | Primary canon lookup — characters, places, factions, CorpoNations, literary rules |
| `Tools.Nodes.cs` | Node + beat CRUD — insert / split / delete / join / rebeat / reflow / export / narrate / spine |
| `Tools.Encyclopedia.cs` | Read-only encyclopedia — weapons, ammo, equipment, tech, cyberware, apparel, pharma, automata, archetypes, quotes, documents |
| `Tools.EntityCrud.cs` | Create/upsert all typed entity kinds |
| `Tools.WorldModelling.cs` | 7 prose-continuity services + entity tree, world-state-at-beat, post-beat validation, timeline consistency, prose lessons |
| `Tools.Quality.cs` | `validate_canon_text`, `review_story`, `diagnose_story`, `scan_story_violations`, `check_prose`, `check_semantic_fidelity`, findings stats |
| `Tools.NarrativeScience.cs` | `analyze_sacred_flaw`, `check_dramatic_question`, `audit_scene_engagement`, `map_five_act_structure` |
| `Tools.Voice.cs` | `harvest_voice(_all)`, voice-proposal list/apply/reject, `get_tone_bible` |
| `Tools.Findings.cs` | `list_findings`, `set_finding_status`, `apply_finding`, `findings_stats` |
| `Tools.Scene.cs` | `assemble_scene_context`, wound ledger, `get_character_loadout` |
| `Tools.Combat.cs` | `draft_combat_scene`, `get_director_context`, `get_world_state_at_beat` |
| `Tools.PlantPayoff.cs` | `register_plant_payoff`, `link_plant_beat`, `link_payoff_beat`, `audit_plant_payoffs` |
| `Tools.StoryAudit.cs` | `audit_story_commandments`, `get_story_spine` |
| `Tools.WorkflowMonitor.cs` | `workflow_status`, `workflow_status_global`, `workflow_beat_modes` |
| `Tools.EntityContext.cs` | `get_entity_context`, `scan_entity_context`, `list_entity_stale_beats` |
| `Tools.LoreTriples.cs` | Continuity-claim extraction; list/resolve/apply contradictions; `append_story_amendment` |
| `Tools.Config.cs` | Markdown-file sync/restore, `RecallMarkdownFiles`, `DocContextPrepare` / `DocContextStatus` |

---

## Stack at a glance

| Layer | Technology |
|---|---|
| Host | Blazor Server (.NET 10), cookie auth, role-gated builds |
| Canon | SQL Server with vector indexes, `FOR SYSTEM_TIME` audit, directional Edges graph |
| Writing surface | `/node/{id}` — writer + recorder + listener on one screen |
| Embeddings | `EmbeddingService`, ~10k entities cached, drift-detected |
| Generation | Multi-provider Quorum via `MindAttic.Legion` — 11 LLM providers, scored evaluation |
| Review | `NodeReviewService` + `StructuralDiagnosticService` + `WritingQualityService` + continuity suite, cost-tiered (RFC 0009) |
| Storytelling science | `NarrativeScienceService` (Will Storr framework), injected into prose context |
| Export | Word / EPUB / PDF / Markdown / HTML via `DocxExportService` + `ManuscriptExportService` |
| Audio | ElevenLabs TTS; local alternatives via Kokoro or Chatterbox (`tools/`) |
| Agents | `StreetSamurai.Mcp` — MCP server exposing canon to Claude clients |
| Credentials | Cloud-native resolution via `MindAttic.Vault` |

---

## Repository layout

```
StreetSamurai/
├── v3/                          # Active engine
│   ├── StreetSamurai.slnx
│   ├── StreetSamurai.Shared/    # POCOs, enums, DTOs
│   ├── StreetSamurai.Core/      # Canon services, generation, embeddings, review, continuity
│   │   ├── Services/            # ~214 services
│   │   ├── Services/Operator/   # Agentic writer tool-use loop
│   │   └── Data/                # EF entities, DbContext, Sql/ migration scripts
│   ├── StreetSamurai.Cli/       # Standalone CLI console app — 152 ss handlers
│   ├── StreetSamurai.Blazor/    # Blazor Server host — the live site
│   ├── StreetSamurai.Mcp/       # Model Context Protocol server (~195+ tools)
│   ├── StreetSamurai.UnitTests/ # NUnit + bUnit tests
│   └── ApplyMigrations/         # One-shot EF Core migration runner
├── tools/
│   ├── codex.ps1                # Codex digest + doctor (run after editing docs/*)
│   ├── check-contradictions.js  # Legion-Quorum chapter-vs-canon sweep
│   ├── chatterbox/              # Free local TTS via Resemble AI Chatterbox (MIT)
│   └── kokoro/                  # Free local TTS via Kokoro-82M (Apache-2.0)
├── docs/                        # Codex docs — BIBLE.md, AMENDMENTS.md, USER_STORIES.md, rfc/
├── engine_data/                 # Canon entity seed/export mirror (SQL is the live read path)
└── cypress/                     # Cypress end-to-end tests
```

---

## Database migrations

Raw T-SQL files live under `v3/StreetSamurai.Core/Data/Sql/`. All scripts are idempotent.

```powershell
dotnet run --project v3/ApplyMigrations
```

---

## Deploying to Azure

Live site: **streetsamurai.azurewebsites.net** on Azure App Service against Azure SQL (Serverless GP). CI/CD runs `build → migrate → deploy` on every push to master via GitHub Actions. Full provisioning guide: [`infra/README.md`](infra/README.md).

---

## Tests

```powershell
dotnet test v3/StreetSamurai.UnitTests   # NUnit + bUnit

npx cypress run     # headless e2e
npx cypress open    # interactive
```

---

## Patent Disclosures

Ten invention disclosures documenting novel systems in the StreetSamurai engine. All are pre-filing confidential documents; formal claim drafting to be conducted by qualified patent counsel.

Full disclosures (standalone HTML, open in any browser): **[`html/patent-disclosures.htm`](html/patent-disclosures.htm)**

| Reference | Title | Core Novelty |
|---|---|---|
| SS-DISC-001 | **Dynamic Context Memory** | Ephemeral .md files materialized from DB, evicted after N beats without access; five-pass retrieval pipeline with tiered LRU stack |
| SS-DISC-002 | **Structural Blueprint System** | Nine structural dimensions committed before prose via mandatory outlier-seeking; internal-understanding resolution excluded as forbidden value |
| SS-DISC-003 | **Multi-Provider Expert Persona Quorum Review** | 1,000-persona library, round-robin LLM providers, two-tier ballots, Big Five psychometric shaping, audience segment clustering |
| SS-DISC-004 | **Plant/Payoff Lifecycle Registry** | Three-status lifecycle (planned/seeded/paid-off) with independent transparency certification and three-category orphan audit |
| SS-DISC-005 | **Beat-Mode Classification with Prose Rhythm Assignment** | Priority-ordered keyword quorum (6 modes) followed by fractional-position rhythm assignment (5 modes) with injected prohibition-bearing directives |
| SS-DISC-006 | **Voice Rule Harvest from Editorial History** | Three-source evidence mining (edit diffs + directives + prose) into staged change log with commit-before-mutate application; score-threshold auto-trigger |
| SS-DISC-007 | **Eight-Dimension Emotional Rubric Scoring** | Parallel LLM scoring on 0–4 universal scale; two blocking dimensions; register-adaptive criteria; per-beat curve written to narrative unit records |
| SS-DISC-008 | **Multi-Provider Continuity Claim Extraction** | Snippet-validated (entity, predicate, object) triples; voter quorum gating; three-outcome upsert (NEW / CONFIRMED / CONTRADICTED) |
| SS-DISC-009 | **Gateway/Sequel Regime Detection** | Auto-detection from predecessor story presence; parallel commandment evaluation enriched with live plant/payoff data; universe-conditional injection |
| SS-DISC-010 | **Deterministic Deprecated-Noun Enforcement** | Universe-scoped rename registry; whole-word boundary scan; chapter-child traversal; zero LLM inference |

---

## Status

In active development. Live site running, working bookshelf.

**Stories complete (≥82% standalone):** BCODA (88.8% cumulative, 16 chapters), ATTE (89.2%), VATD (88.1%), DWIACE (90.95%), SPRW (87.0%), SRZR (86.6%), TEST (86.7%), MxG (86.4%), UNDR (83.2%). MNEMO in progress (84.0%).

**DB:** 12,629 entities · 2,443+ beats · 36 stories across 2 universes (GLMZ + Fantasy/Steampunk).

**MCP:** 195+ tools across 23 tool-class files.
