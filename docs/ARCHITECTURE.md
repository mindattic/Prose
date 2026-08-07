# Prose — Architecture Bible

> **SUPERSEDED 2026-06-07 → see [docs/BIBLE.md](docs/BIBLE.md) (Codex L0 source of truth).** This
> file is retained as a pointer; its content was migrated into `docs/BIBLE.md` (+ `docs/USER_STORIES.md`
> for the goal tables). Edit the Bible, not this file.

> The single document to lean on. It defines the **endpoint** (what "a complete
> story-generation engine" means), the **architecture** (how the parts
> interconnect today), and **every goal — past, present, future — in build
> order**, each with an acceptance test. When in doubt, this file wins; update it
> in the same change that moves a goal.

- **Project:** Prose — a canon-grounded story-generation engine for the GLMZ (Great Lakes Metropolitan Zone, 2200) cyberpunk universe.
- **Stack:** .NET 10, Blazor Server (web-only), SQL Server (LocalDB in dev), ElevenLabs TTS, Legion/LLMVoting, embeddings (VECTOR_DISTANCE), QuikGraph.
- **Quick start:** see `README.md`. **Conventions:** see `CLAUDE.md` (whole-number versioning; camelCase no-underscore; DB is the only canon store; web-only).

---

## 1. The Endpoint — "Definition of Done"

A **complete Prose engine** can, with minimal human steering, do all of the following and prove it:

1. **Reach everything.** Any of the ~28 canon entity types (characters, places, factions, gear, drugs, materials, orgs, synthetics, documents, …) can surface in generation when relevant — no "dead inventory."
2. **Decide from truth.** Every generation/decision is grounded in canon facts + relationships + the codified house voice, all read from the **database** (never from an `.md` that might not be parsed).
3. **Self-correct.** Generated prose is automatically checked against canon across all types; contradictions are detected and a fix is proposed without an admin diffing by hand. Unknown entities are captured as provisional canon, flagged for review.
4. **Judge with the whole picture.** Legion/LLM story decisions are fed the totality (retrieved canon + continuity state + voice rules), so they choose well instead of producing slop.
5. **Evolve its own voice.** Every strand that scores ≥80% with readers is harvested — its winning edits/directives distilled into the codified rules — so each winner sharpens the next.
6. **Run the loop.** Outline → generate → validate/self-correct → narrate → publish → review → harvest, repeatable per strand and across a corpus, with audio + manuscript outputs.
7. **Know its gaps.** A standing coverage report shows, per type, what is reachable/validated and what is not.

**The engine is "done" when** a fresh strand can be taken from a one-line seed to a published, reader-reviewed, canon-consistent audiobook + manuscript with the human only approving (not authoring) canon/voice changes — and the coverage report shows 100% of *diegetic* types reachable and validated.

---

## 2. Architecture at a glance

```
 SEED / DIRECTIVE
      │
      ▼
 OUTLINE  ── OutlineService / OutlineReviewService / BookOutlineService
      │
      ▼
 GENERATE ── StoryDirectorService (orchestrator)
      │        ├─ CanonRetrievalService  ← embeddings (all types) + graph + continuity   [INTERCONNECT]
      │        ├─ BuildCanonFacts (totality) → BeatGeneratorService / BeatPromptBuilder   [CANON-TRUE DECISIONS]
      │        ├─ DialogueService (per-character voice)   ← Character.SpeechPatterns/NarrationVoice
      │        └─ literary_rules / tone_bible (Settings)  ← GetLiteraryRulesPrompt/GetToneBiblePrompt   [CODIFIED VOICE]
      │
      ▼
 VALIDATE ── CanonContradictionService (all types → Findings)   [SELF-CORRECTION, approval-gated]
      │        CanonGroundingService (provisional unknown-entity capture)
      │        ContinuityService/Extraction/Apply · WorldConsistencyService · ContinuityValidatorService
      │
      ▼
 NARRATE / PUBLISH ── StrandWorkbenchService (one-pass audiobook) · ElevenLabsTtsService
      │               ManuscriptExportService (md/txt/pdf) · DocxExportService
      ▼
 REVIEW ── StrandReviewService (persona panels, 1-100) → Strand.Score
      │        on <80→≥80 crossing → VOICE-HARVEST finding
      ▼
 HARVEST ── VoiceHarvestService → VoiceChangeLog (propose) → approve → literary_rules/tone_bible/Kyle fields
      │
      └──────────────► feeds the next generation (the flywheel)

 OBSERVE (cross-cutting): CoverageService (reachability matrix) · FindingsService (the review queue)
```

### Data layer (SQL Server — the only canon store)
- **`Entities`** — universal row per entity: `Id, Name, Slug, EntityType, Description, IsActive`. The spine.
- **`Records`** — `Records.Json`: the canonical per-entity JSON blob (tolerant converters in `Models/Canon/`).
- Per-type **subtype tables** + bridges (relational projections of the blob; one repo per type, `IExportableRepository`).
- **`EntityEmbeddings`** — 1536-d vectors; cosine via `VECTOR_DISTANCE`. Covers all active entities.
- **`Edges`** — typed graph relations (parent_of, etc.); cousins/grandparents derived.
- **`EntityStateEvents`** — story-state ledger (location, life status, ammo) — see `static_vs_dynamic_split`.
- **System-versioned temporal** tables: `Beats`, `Strands`, `StrandBeats`, `ChapterBeats` (+`_History`) — every edit is rewindable (`FOR SYSTEM_TIME ALL`); this is what voice-harvest mines.
- **`Settings`** singletons: `literary_rules`, `tone_bible`, `story_bible`, app config (`JsonSingletonRepository`).
- **`ContinuityClaims`**, **`Findings`**, **`VoiceChangeLog`**, **`Strand*`/`FocusGroup*`**, **`Books`/`Chapters`/`ChapterBeats`**, **`Episodes`/`EpisodeBeats`**.

### Key services (by role)
- **Interconnect:** `EmbeddingService`, `WorldGraphService`, `CanonRetrievalService`, `NarrativeSessionContext`, `SemanticIndexService`, `InferenceService`.
- **Generation:** `StoryDirectorService`, `OutlineService`, `BeatGeneratorService`, `BeatPromptBuilder`, `StoryStarterService`, `SceneGenerationService`, `DialogueService`, `AgendaEngine`, `PacingService`.
- **Voice/rules:** `DatabaseService.GetLiteraryRulesPrompt/GetToneBiblePrompt`, `VoiceHarvestService`, `VoiceChangeLog`, `SeedVoiceRulesCli`.
- **Validation/self-correction:** `CanonContradictionService`, `CanonGroundingService`, `ContinuityService`/`Extraction`/`Apply`, `WorldConsistencyService`, `ContinuityValidatorService`, `FindingsService`.
- **Review/scoring:** `StrandReviewService`, `StrandReviewSummary`, focus groups.
- **Audio/publish:** `StrandWorkbenchService`, `ElevenLabsTtsService`, `ManuscriptExportService`, `DocxExportService`, `EpisodeAudioService`.
- **Observe:** `CoverageService`, `FindingsService`.
- **Surfaces:** Blazor pages (`/strand`, `/strands`, `/generate`, `/continuity`, `/findings`, `/settings`, encyclopedia dictionaries), the MCP server (`Prose.Mcp`), and the `ss` CLI (dispatch in `Program.cs`).

---

## 2a. Architectural invariants (do not violate)

These keep SQL, QuikGraph, embeddings, and the memory rubric from growing conflicting responsibilities.

1. **Single source of truth = the SQL database.** Canon lives in `Entities` + `Records.Json` (+ relational projections). Everything else is a **derived index, rebuildable from canon and never authoritative**:
   - **QuikGraph** (`WorldGraphService`) — an in-memory projection for neighbor-traversal; `Rebuild()` reconstructs it from the DB. Never the store of record.
   - **Embeddings** (`EntityEmbeddings`) — a derived vector index; `ReembedCorpusAsync` rebuilds it from canon (idempotent, drift-skipped). `--coverage --backfill` closes any gap.
   - **The `.md` memory rubric** — a human-readable **mirror** of the voice rules; the live rules live in `literary_rules`/`tone_bible`/Kyle's fields. The generator reads the DB, never the `.md`.
2. **Separation of responsibilities** (one job each; don't let them overlap):
   - *Retrieve* relevance → `CanonRetrievalService` (embeddings + graph). *Decide* what to write → `StoryDirectorService`/`AgendaEngine` (fed the retrieval bundle). *Validate* against canon → `CanonContradictionService`/continuity. *Codify* voice → `VoiceHarvestService`. *Observe* → `CoverageService`/`FindingsService`. A service that needs canon **asks the retriever**; it does not query embeddings/graph directly.
   - Identity facts (name, height, ancestry) live on canonical entity tables; story-state (location, ammo, life status) lives in the `EntityStateEvents` ledger (`static_vs_dynamic_split`). Don't duplicate one into the other; no denorm "convenience copies."
3. **One home per kind of being (the sentience test).** *All intelligent, sentient life — anything with feelings/agency — lives in the **Characters** repo*, classified by a first-class `Species`: exactly `human`, `ai`, `elf` (Emergent Lifeform), `synthetic`, or `unknown`. *Non-sentient machines — robots, androids, drones, anything that has no feelings — live in the **Automaton** repo.* A being is never in both. (This is why `SyntheticLife` and the old `synthetic`/`robot` split are being retired: sentient synthetics + ELFs fold into Characters; mindless machines fold into Automata.)
4. **Characters deepen over time.** A character's metadata is meant to *accrete*, never reset:
   - **Continuity claims** extracted from prose (now all entity types, F2) upsert onto the entity — new true facts join canon.
   - **Voice harvest** folds a character's winning prose moves into their `SpeechPatterns`/`NarrationVoice` (propose-then-approve).
   - **State events** record what changed in-story without overwriting identity.
   - Net effect: each strand leaves Kyle (and the cast) richer than it found them; the engine writes *with* that growing depth via the retriever + `BuildCanonFacts`.

## 2c. The story hierarchy + Canon

**Beat → Strand → Collection → Series.** One model, no parallel formats:
- **Beat** — a **story beat**, not a typographic paragraph: *one discrete unit of story that moves it forward and hands off to the next.* Usually a single paragraph, but it can be a line of dialogue, a moment of action, an image, or a realization — judged by narrative function, not length. One beat does ONE thing; beats **chain** into continuous momentum. NOT a run-on block (a whole scene crammed in one beat) and NOT sentence-shrapnel (one moment split across many tiny beats). Inside a beat: real sentences; each speaker's dialogue on its own line; questions end with `?` and use *asks/asked*; inner monologue italic on its own line. Between beats is a **gap** of narration silence — short after dialogue, longer after narration, longest at a scene/section break. **This doctrine is codified** in `LiteraryRulesData.BeatDoctrine` (default baked in `Models/Canon/WorldData.cs`) and emitted into every generation/segmentation prompt by `DatabaseService.GetLiteraryRulesPrompt()` — the single source the generator and the re-beater both read.
- **Strand** — an ordered set of Beats (a scene/chapter's worth). The unit that's generated, validated, reviewed, narrated, published.
- **Collection** — the **generic** term for *any* ordered set of strands connected in sequence ("a strand of pearls"). "Story", "Novel", "Novella", "Saga", "Anthology" are just display **labels** (`Kind`) on a Collection — structurally it's only "a collection of strands." Modeled as a parent `Strand` whose children are the member strands, ordered by `SortKey` — the existing `ParentStrandId` tree. No new table.
- **Series** — a Collection of Collections (complete arcs → a larger work). Future; another level of the same `ParentStrandId` tree.

Because Collection/Series are just parent `Strand`s, everything (export, edit, review, narrate, canon-mark) works at any level uniformly.

**Root:** today there is one root Series — **"Prose"** — and every Collection/Strand hangs beneath it. Spin-offs add sibling root Series later, so the tree is `Prose (Series) → Collections → Strands → Beats` with room to grow horizontally at the top.

Everything legacy (chapters/episodes/books) was migrated into this so the true breadth of existing writing is visible as Strands.

**Canon** — a `Strand.IsCanon` flag set **only manually, by the author**, meaning: *"I feel this story is strong enough to be used to draw conclusions about the characters and events."* It is the trust gate. Only canon strands should be treated as authoritative for **learning character truth (voice-harvest), inferring continuity/world facts, and deciding what a character is capable of**; non-canon strands are hit-or-miss drafts the engine may read but must not treat as established truth. (So: voice-harvest, continuity extraction, and canon-grounded decisions should weight/filter to canon strands.)

## 2b. Read performance (✅ shipped: materialized read-model, never files)

A full `Character` spans ~25 relational bridge tables; the deep `.Include()` load is **50–80 s** for 1,240 characters. **Decision (Legion quorum 0.75; files rejected):** never reintroduce on-disk entity JSON. Implemented:
- **Relational tables remain the source of truth.** The materialized read-model is the **`CharacterReadModels` table** (`CharacterId` PK, `Json`, `Version`, `RefreshedAt`) — a separate, **non-system-versioned** projection (`CharacterReadModel.cs`; absent from `SystemVersionedTables` so regeneration never pollutes the canonical `Characters` temporal history). It caches the `CharacterMapper.Materialize` output as one blob; full reads become a single column read.
- **Enforced single-writer sync (can't drift):** every `CharacterRepository.Save` calls `CharacterMapper.RefreshReadModelAsync` after its commit, regenerating the blob from the just-persisted relational record. The two fields sourced from *other* write paths — `Location` (EntityStateEvents) and `Tags` (EntityTags) — are blanked in the blob and **overlaid live on read**, so the projection can never go stale on dynamic state. `ReadModelVersion` bumps invalidate the whole store.
- **Self-healing + rebuildable:** `GetAll`/`GetById` read off the projection (`LoadAllFromReadModel`/`LoadOneFromReadModel`); missing or stale-version rows are backfilled relationally on read and persisted. `ss --rebuild-readmodel` rebuilds all (the one-time slow path) and prunes orphans — run after a bulk import or a version bump. Migration: `create_character_readmodel_20260606.sql`.
- **List views** keep using `CharacterRepository.GetAllLite()` (~1 s, lightweight projection); the read-model serves the *full*-record bulk/deep reads that previously took 50–80 s.

## 3. Status legend
`✅ done` · `🟡 partial (core shipped, residual noted)` · `⬜ future`

---

## 4. Goals — Past, Present, Future (in build order)

### PAST — shipped & verified (the foundation)

| # | Goal | Status | Acceptance (how we know) |
|---|------|--------|--------------------------|
| P1 | Web-only Blazor Server app on SQL Server; DB is sole canon store | ✅ | App runs; `engine/data/*.json` migrated to SQL |
| P2 | ~28 canon entity types with tolerant `Records.Json` + relational projections | ✅ | Repos + dictionaries per type; `--coverage` lists 28 types |
| P3 | Embedding index over all active entities (`VECTOR_DISTANCE`) | ✅ | `--coverage`: 11,588 entities, 98% embedded |
| P4 | Generation pipeline (outline → beats → scene), persona reviews + 1-100 scoring | ✅ | `/generate`, `/strands` Reviews modal, `Strand.Score` |
| P5 | Audio (one-pass audiobook, v2/v3 TTS) + manuscript export (docx/md/txt/pdf) + tier check | ✅ | `--publish-audiobook`, `--publish`, Settings tier check |
| P6 | Strand list CLI | ✅ | `ss --list-strands` |
| P7 | **Facet system 100% eradicated** (code + tests + DB), fully backed up | ✅ | 0 `Facet*` tables/columns; `drop_facet_system_20260606.sql`; `CanonEngineTests` regression guards |

### PRESENT — this session's engine (the four capabilities + observability)

| # | Goal | Status | Acceptance |
|---|------|--------|-----------|
| N1 | **Voice codification** — temporal-diff + directive mining → `VoiceChangeLog` → approve into live stores | ✅ | `--harvest-voice` mined 16 edits on "Sunset Clause"; `create_voice_change_log_*.sql`; auto VOICE-HARVEST finding on ≥80% |
| N2 | **Full interconnect** — `CanonRetrievalService` pulls relevant canon across **all** types into generation | ✅ | `--canon-retrieve` surfaced apparel/weapon/document; wired into `SceneGenerationService` + `BuildCanonFacts` |
| N3 | **Canon-true decisions** — totality-augmented `BuildCanonFacts` feeds the beat decision; house voice seeded into DB | ✅ | `--seed-voice-rules` (idempotent: +9/+9/+4); rules flow via `GetLiteraryRulesPrompt` |
| N4 | **Self-correction** — `CanonContradictionService` (all types → approval-gated `CANON-CONTRADICTION` findings); provisional unknown-entity capture | ✅ | `--check-canon` runs per-chunk; `CanonGroundingService` raises PROVISIONAL-ENTITY findings |
| N5 | **Coverage instrumentation** | ✅ | `--coverage` matrix; surfaced the one gap (`motif` 0%) |
| N6 | Unit tests for the engine + facet regression guards | ✅ | `CanonEngineTests` (21 tests green) |

### FUTURE — the road to the endpoint (ordered; each builds on the last)

> Build top-to-bottom. Each item lists **what to create** and its **acceptance test**.

| # | Goal | Status | What to create | Acceptance |
|---|------|--------|----------------|-----------|
| F1 | **Ship present work to prod** | ⬜ | Run `drop_facet_system_*` + `create_voice_change_log_*` via the migrate job; run `--seed-voice-rules` + `--coverage --backfill` in prod | Prod schema has no facet remnants, has `VoiceChangeLog`; `--coverage` clean in prod |
| F2 | **Validation across all types in the continuity pipeline** | ✅ | `ContinuityExtractionService.ResolveEntity` falls back to the universal `Entities` table, so any type produces claims | Builds green; a weapon/drug fact now resolves to a claim |
| F3 | **Self-correction loop (bounded)** | ✅ | `CanonContradictionService.CheckStrandAsync(proposeFixes)` + `ProposeCorrectedSpanAsync`; `ss --check-canon --fix` attaches a canon-honoring rewrite to high-severity findings (approval-gated) | High-severity contradiction yields a REWRITE proposal in the finding; nothing auto-writes |
| F4 | **Legion totality on story decisions** | ✅ | `AgendaEngine.GenerateAgendasAsync`/`FindConflictsAsync` now inject the `CanonRetrievalService` totality block | Agenda/conflict prompts carry relevant canon across all types; DI green |
| F5 | **Full graph coverage** | ✅ | `WorldGraphService.BuildRemainingEntities()` nodes every active entity the bespoke builders missed | All ~28 types are graph nodes; rich types still win |
| F6 | **Coverage → action** | 🟡 | `ss --coverage --backfill` runs idempotent `ReembedCorpusAsync` | **100% coverage** (11,588/11,588; motif 0→100%). Residual: entity↔strand *appearance* tracking still to add |
| F7 | **In-app review surfaces** | ⬜ | `/voice` (VoiceChangeLog: approve/reject) and `/coverage` pages; a `CANON-CONTRADICTION` filter in `/findings` | Approving a proposal in `/voice` writes the rule; `/coverage` renders the matrix |
| F8 | **Autonomous corpus loop** | ⬜ | A driver that runs generate→validate(--fix)→review→harvest across N seeds, pausing only for approvals; resume-safe | `ss --run-corpus --count N` produces N reviewed strands; harvested rules tighten later strands |
| Fx | **Remove dead/orphaned/unused features** | 🟡 | Legion-arbitrated removal of unused services/CLIs/pages/models; enforce §2a invariants | ✅ Deleted `FtpPublishService`/`ConversationalWriterService`/`StoryService` (Legion-unanimous; DI+tests clean). Residual: root one-shot `*.js` generators + stray `*.txt` → archive to `scripts/archive/`; verify no other orphans |
| Fs1 | **One format: everything is a Strand of Beats** | ✅ | `StrandMigrationService.MigrateAllAsync` (`ss --migrate-strands`) folds Books/Chapters/Episodes → Strands; `StrandWorkbenchService.CreateStrandFromBeatsAsync` persists generated stories as Strands | Ran: 24 chapter-strands + 132 episode beats migrated; 42 strands / 1,436 beats total. `AutonomousStory` retired as an *artifact* (generation persists a Strand); residual: excise the `AutonomousStory` class from `StoryDirectorService`/UI internals |
| Fs2 | **Species as a first-class type** | ⬜ | New `Species` lookup entity (Id, Name, Description, traits) + table + bridge + repo + DI + CREATE TABLE sql; `Character.Species` references it; `/species` dictionary + `get_species` MCP tool. **Final species set (exactly five): `human`, `ai`, `elf` (E.L.F. = Emergent Lifeform), `synthetic`, `unknown`.** Each seeded with a canon description. **No `cyborg`** (cybernetics is universal), **no `robot`/`android`/`mutant`** as species. | `/species` lists the five with descriptions; every Character resolves to one; no cyborg/robot/android species exists |
| Fs3 | **Synthetics + ELFs → Characters; robots → Automaton** | ✅ | The sentience test (§2a) drives the split. Migrate `SyntheticLife` records into `Character` (reuse Id), mapping `type`→species: `elf`→`elf`; `supermind`/`rogue_ai`/AI-kinds→`ai`; `ceramic_man` (sentient living gas)→`synthetic`. **Non-sentient `robot`/`android` machines → Automaton repo, NOT Character.** Preserve the precise original `type` as a `synthtype:<x>` tag AND folded into the description (user-confirmed hybrid; Legion had split 2-2). Carry aliases/status/location/story_hooks/tags; deactivate migrated synthetics; then retire the `SyntheticLife` entity/repo/DbSets/dictionary/MCP surfaces (facet pattern: data first, then code) | ✅ **Data migrated** via `ss --migrate-synthetics --apply`: 326 → Characters (ai 202 / elf 120 / synthetic 4), active synthetics **0**, characters **1586**, all embedded (100%). **Code fully retired**: repo, DbSets, entity, model, EF config, DI, MCP tools, `/synthetics` page + nav + tile, and all 6 service references removed; Core/Blazor/MCP/Tests build green, 116 tests pass. Residual (optional): drop the now-orphaned `SyntheticLives*` relational tables via a migration; route any non-sentient robots to Automata if/when they appear |
| Fv | **Per-strand voice / Kyle review pass** | 🟡 | Run `--review-strand` / `--check-canon` / `--harvest-voice` across all 42 strands for house-voice + correct Kyle quips | ✅ Inline facet tags in prose verified **0** (already clean). Residual: the LLM review/harvest pass per strand |
| Fc | **Canon flag** | 🟡 | Author-only `Strand.IsCanon`/`CanonAt`; voice-harvest learns from canon | ✅ Columns + `SetCanonAsync` + `ss --mark-canon (--slug\|--id) [--off]` + `ss --harvest-voice --canon` (harvests the canon set). Residual: an in-app canon toggle on the writer/`/strands` page |
| Fh | **Hierarchy + Collection builder** | ⬜ | **Series → Collection → Strand → Beat** via the `ParentStrandId` tree (no new tables). On the strands page: a **drag-and-drop Collection builder** — create a Collection (parent strand) and drag strands into a sequential order (`SortKey`) for publication; reorder persists. Collection/Series get the same export/review/narrate as a strand. | Drag a strand into a Collection on `/strands`; order persists; publishing the Collection stitches its strands in sequence |
| F9 | **Living world tick** | ⬜ | Re-enable the parked world-sim scaffold: scheduled state-event generation that feeds future strands (off by default) | A world tick emits `EntityStateEvents` that later generation reads |
| F10 | **Voice flywheel proof** | ⬜ | Run F8 long enough to show the ≥80% → harvest → apply loop measurably raising mean strand score over a batch | Mean `Strand.Score` of batch K+1 > batch K after applied harvests |

**Endpoint reached when F1–F8 are green and F10 demonstrates the flywheel.** F9 is the "living world" stretch beyond the core engine.

---

## 5. Testing & verification

- **Unit (deterministic):** `CanonEngineTests` covers retrieval formatting, voice-harvest parsing/normalisation, contradiction parsing/chunking/severity, coverage math, and facet-removal regression guards. Pattern: expose pure helpers as `internal` (Core has `InternalsVisibleTo("Prose.UnitTests")`); LLM/DB paths are exercised by CLIs against live data.
- **DI integrity:** `DiRegistrationTests` / `InterfaceRegistrationTests` prove the whole service graph resolves (run after any constructor change).
- **Known pre-existing failures:** ~43 data-dependent integration tests (`*_LoadsRealData`, `RuleScan_*`, `ZoneInference_*`) require seeded data/DB not present in a clean `dotnet test`; they are not regressions (verified: untouched `WorldConsistencyService` tests fail identically). Treat the **filtered** suites as the gate until those are made self-seeding.
- **Live smoke (CLI):** `--canon-retrieve`, `--coverage`, `--harvest-voice`, `--check-canon`, `--seed-voice-rules` each run against LocalDB.

## 6. Command reference (the `ss` CLI)

| Command | Does |
|---------|------|
| `--list-strands [--status --kind --search --limit --json]` | List strands |
| `--canon-retrieve "<q>" [--k --types]` | Show universal canon reach for a query |
| `--coverage` | Per-type reachability matrix |
| `--harvest-voice (--slug\|--id\|--all-80\|--pending\|--apply <g>\|--reject <g>)` | Voice harvest, propose-then-approve |
| `--check-canon (--slug\|--id\|--all)` | Contradiction sweep → CANON-CONTRADICTION findings |
| `--seed-voice-rules` | Codify house-voice rules into `literary_rules`/`tone_bible` (idempotent) |
| `--publish-audiobook \| --publish` | Render outputs |
| `--migrate-sql --schema` / `--sql-export --data` | Schema migrate / full DB dump |

## 7. Deployment notes
- Dev DB: `(localdb)\MSSQLLocalDB` / `Prose`. Prod: Azure SQL `prose-sql` / `Prose` (RG `MyApps`).
- New SQL artifacts to apply in prod: `Data/Sql/drop_facet_system_20260606.sql`, `Data/Sql/create_voice_change_log_20260606.sql`.
- Backups before destructive schema changes are layered: native `.bak` (incl. `_History`), portable `--sql-export --data`, plus a targeted snapshot — all under `archives/db/`.
- App MI is datareader/writer only; DDL needs the migrate job's SP (see memory `project_deploy_infra_reality`).

---

*Maintenance rule: when you finish a Future goal, flip its status here and add its acceptance evidence in the same commit. This file is the source of truth for "what's left and in what order."*
