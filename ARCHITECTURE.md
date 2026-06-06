# StreetSamurai — Architecture Bible

> The single document to lean on. It defines the **endpoint** (what "a complete
> story-generation engine" means), the **architecture** (how the parts
> interconnect today), and **every goal — past, present, future — in build
> order**, each with an acceptance test. When in doubt, this file wins; update it
> in the same change that moves a goal.

- **Project:** StreetSamurai — a canon-grounded story-generation engine for the GLMZ (Great Lakes Metropolitan Zone, 2200) cyberpunk universe.
- **Stack:** .NET 10, Blazor Server (web-only), SQL Server (LocalDB in dev), ElevenLabs TTS, Legion/LLMVoting, embeddings (VECTOR_DISTANCE), QuikGraph.
- **Quick start:** see `README.md`. **Conventions:** see `CLAUDE.md` (whole-number versioning; camelCase no-underscore; DB is the only canon store; web-only).

---

## 1. The Endpoint — "Definition of Done"

A **complete StreetSamurai engine** can, with minimal human steering, do all of the following and prove it:

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
- **Surfaces:** Blazor pages (`/strand`, `/strands`, `/generate`, `/continuity`, `/findings`, `/settings`, encyclopedia dictionaries), the MCP server (`StreetSamurai.Mcp`), and the `ss` CLI (dispatch in `Program.cs`).

---

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
| P5 | Audio (one-pass audiobook, v2/v3 TTS) + manuscript export (docx/md/txt/pdf) + tier check | ✅ | `--publish-audiobook`, `--publish-docx/md/txt/pdf`, Settings tier check |
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
| F1 | **Ship present work to prod** | ⬜ | Run `drop_facet_system_*` + `create_voice_change_log_*` via the migrate job; run `--seed-voice-rules` in prod; commit A/B/D-E-F/C-G in logical chunks | Prod schema has no facet remnants, has `VoiceChangeLog`; `--coverage` clean in prod |
| F2 | **Validation across all types in the continuity pipeline** | ⬜ | Extend `ContinuityExtractionService` beyond character/place/faction/corponation to all types (it currently silently discards the rest) | A weapon/drug fact in prose produces a `ContinuityClaim`; contradiction on it is detected |
| F3 | **Self-correction loop (bounded)** | ⬜ | After generation, run `CanonContradictionService`; for high-severity hits, offer a one-click "regenerate span with canon as hard constraint"; keep approval-gated | A planted contradiction is detected and a corrected span is proposed; nothing auto-writes without approval |
| F4 | **Legion totality on story decisions** | ⬜ | Feed the `CanonRetrievalService` bundle + relevant `ContinuityClaims` into `AgendaEngine`/director decision prompts (not the narrow field-routing call) | A logged director decision context contains retrieved canon + continuity state |
| F5 | **Full graph coverage** | ⬜ | Add `WorldGraphService` builders for the remaining ~20 types so neighbor-traversal (not just embedding) reaches gear/drugs/orgs/etc. | `GetNeighbors` returns gear/drug/org nodes; graph node count ≈ active entity count |
| F6 | **Coverage → action** | ⬜ | Backfill `motif` embeddings; add appearance tracking (entity↔strand) from grounding resolution; `--coverage` shows "ever appeared in a strand" | `motif` at 100%; coverage reports per-type appearance counts |
| F7 | **In-app review surfaces** | ⬜ | `/voice` (VoiceChangeLog: approve/reject proposals) and `/coverage` pages; a contradiction queue view in `/findings` filtered by `CANON-CONTRADICTION` | Approving a proposal in `/voice` writes the rule; `/coverage` renders the matrix |
| F8 | **Autonomous corpus loop** | ⬜ | A driver that runs outline→generate→validate→narrate→publish→review→harvest across N seeds, pausing only for approvals; resume-safe checkpoints | `ss --run-corpus --count N` produces N reviewed strands; harvested rules visibly tighten later strands |
| F9 | **Living world tick** | ⬜ | Re-enable the parked world-sim scaffold: scheduled state-event generation that feeds future strands (off by default) | A world tick emits `EntityStateEvents` that later generation reads |
| F10 | **Voice flywheel proof** | ⬜ | Run F8 long enough to show the ≥80% → harvest → apply loop measurably raising mean strand score over a batch | Mean `Strand.Score` of batch K+1 > batch K after applied harvests |

**Endpoint reached when F1–F8 are green and F10 demonstrates the flywheel.** F9 is the "living world" stretch beyond the core engine.

---

## 5. Testing & verification

- **Unit (deterministic):** `CanonEngineTests` covers retrieval formatting, voice-harvest parsing/normalisation, contradiction parsing/chunking/severity, coverage math, and facet-removal regression guards. Pattern: expose pure helpers as `internal` (Core has `InternalsVisibleTo("StreetSamurai.UnitTests")`); LLM/DB paths are exercised by CLIs against live data.
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
| `--publish-audiobook \| --publish-docx \| --publish-md \| --publish-txt \| --publish-pdf` | Render outputs |
| `--migrate-sql --schema` / `--sql-export --data` | Schema migrate / full DB dump |

## 7. Deployment notes
- Dev DB: `(localdb)\MSSQLLocalDB` / `StreetSamurai`. Prod: Azure SQL `streetsamurai-sql` / `StreetSamurai` (RG `MyApps`).
- New SQL artifacts to apply in prod: `Data/Sql/drop_facet_system_20260606.sql`, `Data/Sql/create_voice_change_log_20260606.sql`.
- Backups before destructive schema changes are layered: native `.bak` (incl. `_History`), portable `--sql-export --data`, plus a targeted snapshot — all under `archives/db/`.
- App MI is datareader/writer only; DDL needs the migrate job's SP (see memory `project_deploy_infra_reality`).

---

*Maintenance rule: when you finish a Future goal, flip its status here and add its acceptance evidence in the same commit. This file is the source of truth for "what's left and in what order."*
