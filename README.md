# StreetSamurai

**A literary fiction engine for a cyberpunk century.**

StreetSamurai writes novels. Not snippets, not summaries — chapter-length prose, voice-disciplined, canon-grounded, ready for the bookshelf. It is the authoring stack for *Bushido Coda* and a hundred stories beyond it, set in the GLMZ: a 500-kilometer vertical megacity stacked along the western shore of Lake Michigan in the year 2225, where ferrocement waves rise a hundred stories above the lake and CorpoNations hold sovereignty the old nations could not.

Live at **[streetsamurai.azurewebsites.net](https://streetsamurai.azurewebsites.net/)**.

> **[docs/BIBLE.md](docs/BIBLE.md) is the architecture bible** (Codex L0) — the endpoint, laws, and architecture canon. [docs/AMENDMENTS.md](docs/AMENDMENTS.md) is the append-only change log (an amendment wins over the bible). [docs/USER_STORIES.md](docs/USER_STORIES.md) is the goal table with acceptance tests. [docs/rfc/](docs/rfc/) holds design notes. **This README is the full engineering tour** — what every subsystem is and how they fit together.

---

## Table of contents

- [The one paragraph](#the-one-paragraph)
- [How it works: seed → published](#how-it-works-seed--published)
- [Architecture: canon as a database](#architecture-canon-as-a-database)
- [The subsystems](#the-subsystems)
  - [1. Canon & data layer](#1-canon--data-layer)
  - [2. Embeddings & semantic retrieval](#2-embeddings--semantic-retrieval)
  - [3. Prose generation](#3-prose-generation)
  - [4. Review & quality](#4-review--quality)
  - [5. Storytelling science](#5-storytelling-science)
  - [6. Continuity & world-modelling](#6-continuity--world-modelling)
  - [7. Voice & persona](#7-voice--persona)
  - [8. Memory & editorial lessons](#8-memory--editorial-lessons)
  - [9. Export & publish](#9-export--publish)
  - [10. LLM providers & routing](#10-llm-providers--routing)
  - [11. Agentic authoring: Operator + MCP](#11-agentic-authoring-operator--mcp)
  - [12. Infrastructure & utilities](#12-infrastructure--utilities)
- [Cost tiering (RFC 0009)](#cost-tiering-rfc-0009)
- [The CLI surface](#the-cli-surface)
- [The MCP surface](#the-mcp-surface)
- [Stack at a glance](#stack-at-a-glance)
- [Repository layout](#repository-layout)
- [Running locally](#running-locally)
- [Database migrations](#database-migrations)
- [Deploying to Azure](#deploying-to-azure)
- [Tests](#tests)
- [Status](#status)

---

## The one paragraph

A C# / .NET 10 Blazor Server application — web-only, fully responsive — that pairs a disciplined SQL data layer with a multi-LLM generation pipeline. **Canon is a database, not a folder of files.** SQL Server holds 1,000+ named entities (characters, places, factions, CorpoNations, weapons, biotech, documents) bound by a directional graph of relationships, vector embeddings, and a two-axis time model. On top of that sits the writing surface: a Strand → Beat hierarchy with an outline-first workflow, a multi-provider review pipeline (Legion-backed, 11 LLM providers), storytelling-science analysis, a deterministic continuity suite, and an export pipeline (Word / EPUB / PDF / Markdown / audiobook). A reader-review panel of synthetic personas measures reception from data, not vibes. For agentic authoring, an MCP server exposes the entire canon to Claude — Desktop, Code, or any MCP client — so the model can call the world directly. The human approves canon and voice changes; the engine writes.

---

## How it works: seed → published

Every strand or book follows this sequence (codified in [`CLAUDE.md`](CLAUDE.md) and [docs/BIBLE.md §10](docs/BIBLE.md)). The service that owns each step is named in parentheses.

1. **Docs first.** Append world facts to `docs/AMENDMENTS.md`; add a story entry to `docs/USER_STORIES.md`; run `pwsh tools/codex.ps1 doctor` to validate. A fact lives in exactly one Codex layer, cited by stable `{#SS-…}` id.
2. **Entity seeding.** Every named character, CorpoNation, place, and weapon is seeded into SQL **before any prose is generated** (`SqlSeedService`, typed repositories, or MCP `create_*` tools).
3. **Book structure.** Create a book-level strand (`kind=book`) plus chapter sub-strands (`kind=chapter`) parented to the book; the 14-beat authorial spine becomes the book strand's `seed` text (`StrandBibleService`, `BookOutlineService`).
4. **Prose draft.** A panel of expert personas votes on the best next beat, then an LLM expands the winning blurb to prose (`BeatGeneratorService`). Each beat's prompt is grounded by an X-Ray context block — the exact entities, voice rules, world state, and behavioral rules in play (`SceneContextAssembler`).
5. **Reflow.** A bounded copy-edit pass fixes paragraph breaks, question punctuation, and dialogue attribution without rewriting prose (`ProseReflowService` / `StrandMarkdownExporter`).
6. **Review (dual, mandatory).** A structural pre-flight runs first (`StructuralDiagnosticService`), then N Legion personas cast scored reader ballots and synthesize an aggregate (`StrandReviewService`). Per the house rule: a per-strand **standalone** review must clear ≥82%, and the **cumulative** prefix (all strands in reading order) must hold ≥85%.
7. **Continuity scan.** Claims are extracted from prose and validated against live entity state (`ContinuityExtractionService`, `ContinuityValidatorService`); mentions are scanned (`EntityExtractionService`); deterministic checks catch timeline and location impossibilities (`TimelineConsistencyService`, `LocationContradictionService`). Approved fixes apply via `FindingApplyService`.
8. **Export.** A KDP-ready Word `.docx`, plus EPUB 3 + PDF + Markdown, all land in a `{Title}/V{N}/` subdirectory (`DocxExportService`, `ManuscriptExportService`); optionally a one-pass audiobook (`ElevenLabsTtsService`).

Prose is **never** written before steps 1 and 2 are complete. The engine is **not** a multi-author averaging machine: quorum is for *review* (catching what one voter misses); prose is written by one voice at a time.

---

## Architecture: canon as a database

The core invariant ([SS-LAW-1](docs/BIBLE.md)): **the live canon is the SQL database.** The generator reads SQL, never a markdown file that might not be parsed. The `engine_data/*.json` corpus is a seed/export mirror, not the read path.

- **Universe scoping.** Every story and canon row belongs to exactly one Universe (`UniverseId` FK). EF Core global query filters scope ~26 entity types per process automatically (`UniverseContext`); the flagship is GLMZ (cyberpunk), with a Fantasy/Steampunk universe standing up on the same engine.
- **The entity spine.** All world objects are `Entity` rows with typed child tables (`Character` + ~25 child tables, `Place`, `Faction`, `CorpoNation`, weapons, biotech, …). The relational migration (RFC 0007) replaced `Records.Json` blobs with real columns and bridge tables.
- **The Edges graph.** `Edge` is a labeled, directed relationship (`carries`, `wields`, `member_of`, `parent_of`, `located_at`, …) with validity windows. `WorldGraphService` materializes it into an in-memory adjacency graph for traversal and nearest-neighbor context.
- **Two-axis time.** (a) `Beat` and `Strand` are SQL Server **system-versioned (temporal)** tables, so the writer gets undo/diff across every edit. (b) `EntityStateEvent` is an append-only **in-world** story-time ledger — `(entity, predicate, value, beatId, storyTime)` — so world state can be reconstructed at any beat without mutating identity columns. Identity facts live on entity tables (vector-indexable); story-state lives in the ledger (the static-vs-dynamic split).
- **Findings inbox.** Autonomous quality checks never edit prose directly. They write `FindingRow`s (category, summary, before/after, status) that the author approves or dismisses.

---

## The subsystems

> The tables below are the complete service catalog (~214 services in `v3/StreetSamurai.Core/Services/`), grouped by subsystem. Each line is one service and what it does.

### 1. Canon & data layer

| Service | Role |
|---|---|
| `Repositories` | Aggregate injection point wiring all typed repos together |
| `BookRepository` / `ChapterRepository` / `SeriesRepository` / `UserRepository` | CRUD repositories for their entity types |
| `RepositoryDefinitionService` | Runtime-defined custom entity repositories (slug, icon, route) |
| `BookOutlineService` | Builds ordered chapter/beat outlines for a book |
| `LoreService` | Canonical lore lookup — pulls character/world facts for prompt grounding |
| `CanonGroundingService` | Injects grounded entity dossiers into LLM prompts |
| `CanonRetrievalService` | RAG-style semantic + graph lookup over canon for context assembly |
| `CrossReferenceService` / `XrefService` | Resolve and persist cross-entity reference edges |
| `WorldStateLedger` | Append-only event log powering state-at-beat queries |
| `DatabaseService` | Low-level DB access / direct SQL wrapper over the EF context |
| `DataConsistencyService` | Audits FK and relational integrity across entity tables |
| `DataRepairService` / `DataScanUtility` | Repair known corruption patterns; enumerate entities for batch ops |
| `SchemaRebuildService` | Safe column-reorder rebuild via snapshot/recreate |
| `SchemaGraphService` | Introspects DB schema into a typed graph |
| `SqlSeedService` | Applies canonical SQL seeds from C# |
| `CanonExportService` / `ExportService` | Export canonical entity data to JSON/zip |
| `JsonDirectoryRepository` / `JsonDictionaryRepository` | Legacy file-backed repos kept on the migration path |

### 2. Embeddings & semantic retrieval

| Service | Role |
|---|---|
| `EmbeddingService` | OpenAI `text-embedding-3-small` vector store; SHA-256 drift detection; cosine NN search over ~10k entities. Use `FindSimilarAsync(text, k, types?)` — never substring matching |
| `SemanticIndexService` | Low-level embedding index management (upsert, prune, re-index) |
| `GlobalSearchService` / `GlobalSearchWarmupService` | Combined full-text + semantic search; background preheater |
| `SemanticFidelityService` | Goodhart-drift check — cosine similarity of beat prose vs. strand bible/synopsis |
| `KnowledgeMapService` | Clusters entities by embedding similarity into knowledge maps |
| `ThematicIndexService` | Tags/indexes beats and strands by theme for retrieval |

### 3. Prose generation

| Service | Role |
|---|---|
| `BeatGeneratorService` | **Core engine** — panel of expert personas votes on the best next beat; an LLM expands it to prose. Tier-locked HIGH for prose the reader sees |
| `BeatPromptBuilder` | Constructs beat prompts with canon context, voice rules, world state |
| `SceneGenerationService` | Full scene pipeline: assemble context → prompt → prose |
| `CombatSceneWriter` | Canon-aware combat prose — tracks loadouts, ammo, bio-battery, terrain |
| `DialogueService` | Dialogue in per-character voice registers |
| `CoWriterService` | Interactive co-writing — completes an author's partial draft in voice |
| `StrandBibleService` | Generates/manages per-strand bibles (authorial spine, beat plan, synopsis) |
| `OutlineService` / `StoryMethodologyService` | Generate outlines from a seed; embed five-act / scene-anatomy frameworks into prompts |
| `StoryDirectorService` | Top-level autonomous loop: plan → generate → assess → continue |
| `StoryStarterService` / `StoryRefinementService` | Seed a new story; propose targeted refinements to a finished one |
| `BeatRebuildService` | LLM re-segmentation of prose into canonical beat atoms |
| `EpisodeGeneratorService` / `AutonomousStoryFormatter` | Legacy episodic generation + output formatting |
| `NpcGenerator` / `DynamicPlaceGenerator` / `RandomEncounterService` / `ContractGenerator` | Procedural NPCs, places, encounters, and in-world contract documents grounded in canon |

### 4. Review & quality

| Service | Role |
|---|---|
| `StrandReviewService` | **Primary review path** — N distinct Legion personas each cast a scored reader ballot; synthesizes an Amazon-style aggregate; round-robins across the trusted-4 providers for model + viewpoint diversity |
| `ReviewEffortProfile` | RFC 0009 cost tier (draft / standard / deep) — scales ballot count, prose upgrades, diagnosis, and per-call model selection to the task's importance |
| `ReviewClusterer` | Clusters ballots into Pareto / contested / seam groupings for the sampled report |
| `StructuralDiagnosticService` | 12 parallel structural pre-flight checks (missing antagonist cost, passive protagonist, exposition-only, …) before ballots burn |
| `BookReviewService` | Book-level multi-LLM quorum review; aggregates continuity/motif/style findings into a `BookReviewReport` |
| `ContinuousQualityService` | Background monitor — fires contradiction + cliché checks on each saved beat (gated by `ReviewAutoRunEnabled`; Draft-tier only) |
| `WritingQualityService` | **Deterministic** heuristic pass (no LLM): first-line strength, tension delta, paragraph-serves, motif reuse, voice cadence drift |
| `StoryQualityService` | LLM-panel story scoring on configurable rubrics |
| `OutlineReviewService` | Reviews a chapter outline for structural/narrative quality |
| `PostBeatValidationService` | Post-generation gate: gear-carry + behavioral-invariant + timeline checks in one call |
| `FindingsService` | Inbox for all autonomous findings — CRUD + status tracking |
| `FindingApplyService` | Applies a single approved finding (before/after) to a beat |
| `EntityRatingService` / `EntityReviewService` | Legion-persona quality voting on canon entities; aggregate summaries |
| `StoryRepairService` | Dossier-driven repair — walks chapters, augments character records, runs continuity extraction |
| `ValidationService` | Cross-cutting validation rules (entity constraints, world rules) |

### 5. Storytelling science

Operationalizes Will Storr's *The Science of Storytelling* — the engine treats craft as measurable. RFC 0009 wired this from *report* to *guidance*: results persist as findings and inject into the prose context deterministically (zero per-beat LLM cost), so the science guides generation, not just audits it.

| Service | Role |
|---|---|
| `NarrativeScienceService` | Sacred-flaw / theory-of-control analysis, dramatic-question check, 6-point scene-engagement audit, five-act structure map, antihero-empathy check |
| `ArcTrackerService` | Tracks per-strand five-act position and beat-level progression |
| `PacingService` | Flags beats that over-stay, under-stay, or repeat an emotional register without escalation |

### 6. Continuity & world-modelling

Mostly **deterministic** (DB-only, no LLM cost). The seven prose-continuity services (RFC 0002 / world-modelling pass) split cleanly into pre-generation injectors and post-generation validators.

| Service | Role | LLM? |
|---|---|---|
| `EntityRelationshipService` | BFS Edge-graph relationship trees for scene context | No |
| `ProsePatternGuard` | Regex linter for banned patterns (pseudo-profound, cliché, on-the-nose, italicized dialogue) | No |
| `AmbientDetailInjector` | Builds a sensory palette from a character's carried/worn gear for pre-gen injection | No |
| `WorldStateAtBeatService` | Point-in-time world-state snapshot (aspects + active edges) from the ledger | No |
| `GearCarryEnforcer` | Post-gen: detects gear-use verbs and checks the carry graph allows them | No |
| `BehavioralInvariantEnforcer` | Post-gen: checks prose against a character's registered behavioral rules | **Yes** (1/char) |
| `WeaponAmmoCompatibilityService` | Validates weapon+ammo pairs; canonical name+GUID constants | No |
| `TimelineConsistencyService` | RFC 0009 — deterministic dead-character-acting + wound-regression checks | No |
| `LocationContradictionService` | Detects a character in two places at once across beats | No |
| `CanonContradictionService` | Chunked semantic + LLM sweep for canon rule violations | Yes |
| `WorldConsistencyService` | Prose scan for world-rule violations (city police, Behemoth-as-alive, …) | Yes |
| `SceneContextAssembler` | X-Ray scene assembly — entity mentions → dossiers → voice + behavioral + science context block | No |
| `WorldGraphService` | In-memory adjacency graph over all entities/edges; rebuilt from SQL; staleness-probed | No |
| `ContinuityExtractionService` | LLM+Quorum extraction of `(entity, predicate, object)` claims from prose | Yes |
| `ContinuityService` / `ContinuityValidatorService` / `ContinuityApplyService` | Claim store CRUD + contradiction detection; validate vs. live state; apply resolved claims | Mixed |
| `ContinuityLongSweepService` | Cross-chapter drift sweep | Yes |
| `ConsequenceEngine` / `ConsequenceService` | Track and propagate plot consequences across beats | Mixed |
| `EntityExtractionService` | Entity-mention extraction → `BeatEntityMentions` | Yes |
| `EntityRamificationService` | Propagates entity-update side-effects to stale beats that mention it | No |
| `BehaviorPredictionService` | Predicts how a character would act in a scenario from rules + history | Yes |
| `MotifService` | Per-book motif inventories; flags chapters that drop a thread | No |
| `GraphHealthService` | Audits the graph for orphan nodes, missing edges, stale data | No |
| `DriftAuditService` | Reports columns disagreeing between static tables and latest ledger events | No |
| `WorldClockService` / `WorldTickService` / `WorldStateService` / `WorldStatePrecheckService` | In-world time advancement; current-state façade; pre-gen impossibility checks | No |
| `CharacterStateBackfillService` / `DateBackfillService` | Backfill ledger events and InWorldDates from existing data | No |
| `AmbientAnomalyService` | Detects/logs ambient anomaly events (resonance irregularities) | No |

### 7. Voice & persona

| Service | Role |
|---|---|
| `VoiceHarvestService` | Mines author edits + directives from winning strands (≥80%) into proposed voice rules; propose-then-approve |
| `ExpertPersonaService` | Manages the reusable expert-persona pool; `SelectPertinentAsync` picks top-N for a scene (with offline tag-overlap fallback) |
| `ExpertPersonaCatalog` | Curated starter personas — genre experts, craft specialists, die-hard cyberpunk readers |
| `StrandSpineService` | Versioned authorial-spine history (`StrandSpineVersion`) for diff/rollback |
| `NamePoolService` | Culturally diverse name pools (Ubiquitous Diaspora rule) |

### 8. Memory & editorial lessons

| Service | Role |
|---|---|
| `ProseLessonStore` | RFC 0009 — SQL-backed store of author rulings (score-vs-function, delight, voice, pacing) scoped global/strand/beat; injected into review prompts so reviewers stop penalizing beats already ruled to be doing their job |
| `ActionConfigService` | Per-action LLM tier registry; enforces `ChapterBeatWriter`/`Expander` locked at HIGH |
| `SettingsKvStore` / `SettingsService` | SQL-backed key-value config; app-wide settings façade (models, keys, tone targets, review knobs) |
| `SceneContextBuilder` / `ContextAnalyzerService` | Build scene context blocks; analyze current context to inform generation |
| `StoryStateService` / `NarrativeSessionContext` | Per-story / per-session generation state across server lifecycles |
| `AgendaEngine` | Director-level agenda — outstanding obligations (plant/payoff, dangling threads) |
| `NarrativeSummaryService` | Chapter/strand transition summaries |
| `LastPromptStore` | Stores the last LLM prompt for inspection/debugging |

### 9. Export & publish

| Service | Role |
|---|---|
| `ManuscriptExportService` | All-in-one strand export — EPUB 3 + PDF (QuestPDF, 6"×9" KDP trim) + Markdown into one `{Title}/V{N}/` dir |
| `DocxExportService` | KDP-ready Word `.docx` (title page, chapter headers, justified serif) via OpenXml |
| `BookExportService` | Book-level EPUB 3 (Calibre-compatible) + PDF |
| `StrandMarkdownExporter` | Ordered beats → markdown with a stable content fingerprint; mirrors `engine/data/exports/` |
| `HtmlExportService` | Interlinked HTML encyclopedia (sidebar nav, cross-refs, tag filtering) |
| `ExportDiscoveryService` | Finds existing export files for a strand/book in the publish dir |
| `ElevenLabsTtsService` | ElevenLabs TTS (v2/v3 channels) for narration; tiered fallback (one request → per-chapter → split) |
| `TtsEnhancementService` | Audio-tag injection + stability tuning for TTS |
| `SegmentAggregator` | Aggregates audio segments into combined-strand output |
| `AudioFileService` / `AudioReconciliationService` | Local audio management; reconcile bytes between disk and Azure Blob |
| `LocalDiskAudioStore` / `AzureBlobAudioStore` / `DualWriteAudioStore` | Audio backends — disk, blob, and dual-write resilience |
| `LocalTts` / `PiperTtsService` / `WindowsTtsService` | Local TTS fallbacks (SAPI / Piper) |
| `MarkdownFileService` / `MarkdownService` | Sync Codex/memory markdown to DB with version history; render markdown |
| `ChapterRecordingService` / `EpisodeAudioService` / `EpisodeExportService` | Legacy chapter/episode audio + export paths |

### 10. LLM providers & routing

| Service | Role |
|---|---|
| `MultiLlmService` | 11-provider fan-out — Claude, ChatGPT, Gemini, DeepSeek, Mistral, Grok, Groq, Together, OpenRouter, Fireworks, Cohere; wire-level via `MindAttic.Legion` |
| `LlmRouter` | Routes each request to the right provider/tier from `ActionConfig` |
| `AssignTiersService` | Assigns Haiku/Sonnet/Opus class to actions per settings |
| `InferenceService` | Lightweight single-LLM wrapper for simple callers |
| `ClaudeService` / `ClaudeCliService` / `OpenAiService` | Direct provider wrappers (pre-Legion / CLI paths) |
| `DallEService` / `ImagePromptRegenService` | Image generation; ancestry-matched prompt rewrites (SHA-256 hash-gated) |
| `AskService` | RAG "ask" mode — embed question → retrieve entities → ground the answer |

Reviews round-robin across the **trusted-4** (Claude, OpenAI, Gemini, DeepSeek) for diversity; review personas are required to be die-hard cyberpunk readers, never randos.

### 11. Agentic authoring: Operator + MCP

The **Operator** (`v3/StreetSamurai.Core/Services/Operator/`) runs a Claude tool-use loop that writes chapters end-to-end by calling the world directly.

| Service | Role |
|---|---|
| `WriterOperatorService` | Orchestrates the agentic tool-use loop (hardcoded Opus-class) |
| `AnthropicToolClient` | Anthropic-native tool-call client |
| `WriterToolRegistry` | Registers all writer tools |
| `ValidateCanonTool`, `DraftCombatSceneTool`, `QueryWorldGraphTool`, `OutlineChapterTool`, `ScoreStoryQualityTool`, `RefineStoryTool`, `ExtractEntitiesTool`, `GetVoiceContextTool`, `RecordCanonChangeTool`, `GetConsequencesTool`, `ProposeStoryEditsTool`, `PredictBehaviorTool` | Individual operator tools — each maps to a Core service |

The **MCP server** (`StreetSamurai.Mcp`) exposes the same canon to any MCP client. See [The MCP surface](#the-mcp-surface).

### 12. Infrastructure & utilities

| Service | Role |
|---|---|
| `UniverseContext` | Ambient multi-universe scope read by DbContext global filters |
| `FamilyGeneratorService` / `FamilyTieService` / `GeneticsInheritanceService` | Generate immediate family; manage parent/sibling/spouse edges; blend ancestry 50/50 ±5% |
| `RelationshipDiscoveryService` | Discover implicit relationships from prose for edge seeding |
| `ReputationTracker` / `CrewAssessmentService` / `CohortRelocationService` | Track reputation; score crews; bulk-relocate character cohorts |
| `FactInterpreterService` / `BeatFactExtractionService` / `BeatStateExtractor` | Extract structured facts and state deltas from prose |
| `BeatFormatter` / `TextAnalysisService` / `WikiLinkService` | Format beats; text statistics; resolve `[[wiki-links]]` to DB ids |
| `TagNormalizerService` / `TagWeaponLethalityService` | Normalize tags; tag weapon lethality tiers |
| `SuggestionEngineService` / `TriviaService` | Editorial suggestions; in-world trivia for UI flavor |
| `CoverageService` | Per-entity-type embedding reachability matrix |
| `FixPhiService` / `FixIdentityCorruptionService` / `MojibakeRepairService` | One-shot repairs — Φ (QUANTA) symbol normalization, identity corruption, encoding mojibake |
| `ProfileService` / `AuthUserImportService` / `EmailService` | User profiles; auth import; email |
| `EventLogService` / `LoggingService` / `NavigationService` | Audit trail; structured logging; Blazor nav |
| `HomeStatsCache` / `HomeStatsRefreshService` / `SearchTriggerService` / `ViewModeService` / `ReadOnlyState` | Dashboard stats + UI state |
| `ScriptRunnerService` / `PipelineServiceBase` / `ProjectArchitectureService` | Run embedded SQL/PS scripts; pipeline base class; codebase introspection |
| `FileSecurePreferences` / `FileSystemPathProvider` / `MediaService` | OS-encrypted prefs; path resolution; media files |

---

## Cost tiering (RFC 0009)

The engine is powerful but token-hungry, so spend scales to a task's importance rather than running flat. A review's job is to **guide prose, not mint a number** — the dial is "what signal do I need right now?" See [docs/rfc/0009-cost-tiered-storytelling-engine.md](docs/rfc/0009-cost-tiered-storytelling-engine.md).

| Tier (`--effort`) | LLM calls | Models | Used for |
|---|---|---|---|
| `draft` | ~6 (**−84%**) | cheapest per provider (Haiku / Flash-Lite / Nano) | mid-draft spot checks; per-beat iteration — **not** a gate |
| `standard` | ~15 (**−60%**) | mid-tier defaults | the per-strand standalone gate (≥82%) |
| `deep` | ~37 (baseline) | mid-tier defaults + full diagnosis + prose critique | the cumulative / publish gate (≥85%), flagship |

Two cost axes compose: **call count** (ballots / prose upgrades / diagnosis) and **per-call model selection** (cheap models at Draft only, since gate scores must stay trustworthy). The override is per-run — it never mutates persisted settings. The same philosophy gates the rest of the engine: continuity is deterministic where possible, storytelling science runs once and is then injected for free, and `ProseLessonStore` lets the author tell reviewers when a low score is *fine* because the beat is doing its job.

---

## The CLI surface

~118 headless handlers, all dispatched from `v3/StreetSamurai.Blazor/Program.cs`. Canonical invocation:

```powershell
dotnet run --project v3/StreetSamurai.Blazor -- <args>
```

**Prose & strands:** `--write-strand` (bible-first generation) · `--expand-beat` · `--edit-beat` · `--edit-strand` (review-weighted auto-editor) · `--reflow-strand` · `--rebeat-strand` · `--bible-strand` · `--duplicate-strand` · `--reparent-strand` · `--run-corpus` (autonomous loop)

**Review & quality:** `--review-strand [--effort draft|standard|deep]` · `--diagnose-strand` · `--check-fidelity` · `--check-prose` · `--check-canon` · `--check-behavior`

**Memory:** `--lesson-add` · `--lessons-list`

**Continuity & world-modelling:** `--timeline-check` · `--gear-check` · `--assemble-scene` (X-Ray) · `--world-state` · `--weapon-network` · `--ambient-palette` · `--continuity` · `--interpret` (prose → entities/edges)

**Storytelling science:** `--narrative-science [--effort …]`

**Export & publish:** `--publish-docx` · `--publish-audiobook` / `--narrate-strand` · `--export` (canon JSON) · `--sql-export` · `--sync-markdown` / `--restore-markdown`

**Entities & migration:** `--add-character` / `--add-place` / `--add-doc` / `--add-corponation` · `--seed` · `--migrate-sql` · `--migrate-strands` · `--rebuild-*-relational` (RFC 0007 backfills)

**Infrastructure:** `--rebuild-graph` · `--reembed` · `--coverage` · `--ask` (RAG) · `--audit-drift` · `--universe <id>` (global scope flag) · `--review-entity`

---

## The MCP surface

`StreetSamurai.Mcp` exposes **175 `[McpServerTool]` methods** (24 tool-type classes across 19 files), so Claude can call the world directly. **The complete, always-current per-tool reference — every tool, its description, and its parameters — is [docs/MCP_TOOLS.md](docs/MCP_TOOLS.md)**, generated by reflection over the live attributes (`ToolDocGenerator`) and auto-regenerated by the pre-commit hook whenever a tool changes, so it can never drift:

```powershell
dotnet run --project v3/StreetSamurai.Mcp -- --export-tools docs/MCP_TOOLS.md
```

The families at a glance:

| File | Tool family |
|---|---|
| `Tools.cs` | Primary canon lookup — characters, places, factions, CorpoNations, literary rules |
| `Tools.Strands.cs` | Strand + beat CRUD — insert / split / delete / join / rebeat / reflow / publish-docx / narrate / spine |
| `Tools.Encyclopedia.cs` | Read-only encyclopedia — weapons, ammo, equipment, tech, cyberware, apparel, pharma, automata, archetypes, materials, transport, consumer goods, quotes, documents, genemods, psionics, subsidiaries |
| `Tools.EntityCrud.cs` | Create/upsert all typed entity kinds |
| `Tools.WorldModelling.cs` | The 7 prose-continuity services + entity tree, world-state-at-beat, post-beat validation, timeline consistency, **prose lessons** |
| `Tools.Quality.cs` | `validate_canon_text`, `review_strand`, `diagnose_strand`, `scan_strand_violations`, `scan_chapter_quality`, `check_prose`, `check_semantic_fidelity`, `findings_stats`, review-settings CRUD |
| `Tools.NarrativeScience.cs` | `analyze_sacred_flaw`, `check_dramatic_question`, `audit_scene_engagement`, `map_five_act_structure`, `check_antihero_empathy`, `check_behavior` |
| `Tools.Voice.cs` | `harvest_voice(_all)`, voice-proposal list/apply/reject, `get_tone_bible` |
| `Tools.Findings.cs` | `list_findings`, `set_finding_status`, `apply_finding`, `findings_stats` |
| `Tools.Planning.cs` | `extract_entities`, `predict_behavior`, `get_neighbors(_by_relation)`, `get_weapon_network`, consequence context |
| `Tools.LoreTriples.cs` | Continuity-claim extraction from chapter/book/entity; list/resolve/apply contradictions; `append_strand_amendment` |
| `Tools.Continuity.cs` | `find_contradictions` (chapter) + `find_contradictions_book` (cross-chapter pairwise) |
| `Tools.Scene.cs` | `assemble_scene_context`, wound ledger (`get_character_wounds`, `log_wound`, `set_wound_status`), `get_character_loadout` |
| `Tools.Combat.cs` | `draft_combat_scene` (the sole prose-generating MCP tool), `get_director_context`, `get_world_state_at_beat` |
| `Tools.Writing.cs` | `create_book`, `add_chapter_to_book`, `get_book(_outline)` |
| `Tools.Universe.cs` | `current_universe`, `list_universes`, `switch_universe`, `get_story_bible` |
| `Tools.Repository.cs` | `list_repositories`, `create_repository`, `get_entity_tree` |
| `Tools.Config.cs` | Markdown-file sync/restore, `get_review_settings` |
| `Tools.Species.cs` | `list_species`, `get_species`, `get_archetype` |

---

## Stack at a glance

| Layer | Technology |
| --- | --- |
| Host | Blazor Server (.NET 10), cookie auth, role-gated builds |
| Canon | SQL Server with vector indexes, `FOR SYSTEM_TIME` audit, directional Edges graph |
| Writing surface | `/strand/{id}` — writer + recorder + listener on one screen |
| Embeddings | `EmbeddingService`, ~10k entities cached, drift-detected |
| Generation | Multi-provider Quorum via `MindAttic.Legion` — 11 LLM providers, scored evaluation |
| Review | `StrandReviewService` + `StructuralDiagnosticService` + `WritingQualityService` + continuity suite, cost-tiered (RFC 0009) |
| Storytelling science | `NarrativeScienceService` (Will Storr framework), injected into prose context |
| Export | Word / EPUB / PDF / Markdown / HTML via `DocxExportService` + `ManuscriptExportService` + `BookExportService` |
| Audio | ElevenLabs TTS; free local alternatives via Kokoro or Chatterbox (see `tools/`) |
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
│   │   ├── Services/            # ~214 services (cataloged above)
│   │   ├── Services/Operator/   # Agentic writer tool-use loop
│   │   └── Data/                # EF entities, DbContext, Sql/ migration scripts
│   ├── StreetSamurai.Blazor/    # Blazor Server host — the live site + ~118 CLI handlers
│   ├── StreetSamurai.Mcp/       # Model Context Protocol server (~201 tools)
│   ├── StreetSamurai.UnitTests/ # NUnit + bUnit tests
│   ├── ApplyMigrations/         # One-shot EF Core migration runner
│   └── ...                      # Apply* / Promote* / Sync* one-off consoles
├── tools/
│   ├── codex.ps1                # Codex digest + doctor (run after editing docs/*)
│   ├── check-contradictions.js  # Legion-Quorum chapter-vs-canon sweep
│   ├── chatterbox/              # Free local TTS via Resemble AI Chatterbox (MIT)
│   └── kokoro/                  # Free local TTS via Kokoro-82M (Apache-2.0)
├── v3/python/                   # SPO triple extraction + semantic consistency pipeline
├── infra/                       # Azure SQL + GitHub Actions setup
├── docs/                        # Codex docs — BIBLE.md, AMENDMENTS.md, USER_STORIES.md, rfc/
├── engine_data/                 # Canon entity seed/export mirror (SQL is the live read path)
├── cypress/                     # Cypress end-to-end tests
└── cypress.config.js
```

Each `Apply*` / `Promote*` / `Sync*` console under `v3/` is a single-purpose migration runner — built once, run once against the live database, left in place as a record.

---

## Running locally

Prerequisites: .NET 10 SDK, SQL Server (LocalDB is fine), at least one LLM provider API key wired through `MindAttic.Vault`.

```powershell
cd v3
dotnet restore
dotnet run --project ApplyMigrations   # apply schema + seed

dotnet run --project StreetSamurai.Blazor
# -> https://localhost:7103/
```

The MCP server runs as its own host:

```powershell
dotnet build StreetSamurai.Mcp/StreetSamurai.Mcp.csproj -c Release
dotnet run --project StreetSamurai.Mcp --no-build --configuration Release
```

Register it permanently in Claude Code:

```
claude mcp add streetsamurai dotnet run --project D:/Projects/MindAttic/StreetSamurai/v3/StreetSamurai.Mcp/StreetSamurai.Mcp.csproj --no-build --configuration Release
```

---

## Database migrations

Raw T-SQL files live under `v3/StreetSamurai.Core/Data/Sql/`. All scripts are idempotent.

```powershell
dotnet run --project v3/ApplyMigrations
```

---

## Deploying to Azure

The live site runs on Azure App Service at **streetsamurai.azurewebsites.net** against Azure SQL (Serverless GP). CI/CD runs `build → migrate → deploy` on every push to master via GitHub Actions. Full provisioning guide: [`infra/README.md`](infra/README.md).

---

## Tests

```powershell
dotnet test v3/StreetSamurai.UnitTests   # NUnit + bUnit

npx cypress run     # headless e2e
npx cypress open    # interactive
```

After editing any `docs/*` Codex file, run `pwsh tools/codex.ps1 digest` then `pwsh tools/codex.ps1 doctor` — doctor must pass.

---

## Status

In active development. Live site running, working bookshelf, cost-tiered review pipeline shipping findings, storytelling-science guidance wired into generation, deterministic continuity suite (timeline + location + gear + behavior), MCP server registered with ~201 tools, audiobook MVP in flight.
