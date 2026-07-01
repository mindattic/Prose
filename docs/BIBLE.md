---
codex: 1
project: StreetSamurai
code: SS
layer: bible
status: living
updated: 2026-06-18
---

# StreetSamurai — Project Bible
> Single source of truth for what StreetSamurai IS, is NOT, and the rules that keep it coherent.
> README says how to build/run; this says how to think about the system.
>
> **Migrated from `ARCHITECTURE.md` (2026-06-07).** The old `ARCHITECTURE.md` is retained as a
> 1-line pointer for tooling; this file supersedes it. **StreetSamurai is a `mixed` project:** a
> *universe-agnostic* software story-generation engine AND the narrative canon of its universes.
> The engine is the product; a **Universe** is content. **GLMZ** (the *Bushido Coda*) is
> Universe #1; a **Fantasy/Steampunk** universe is being stood up on the same engine
> (see [SS-A2](AMENDMENTS.md)). §4 is the software canon; §5 carries both engine invariants and
> per-universe narrative laws; §9 glossary spans both.

## 1. The one sentence {#SS-§1}

StreetSamurai is a canon-grounded, voice-disciplined story-generation engine that takes a
one-line seed to a published, reader-reviewed, canon-consistent audiobook + manuscript **in any
registered Universe** — with the human approving, not authoring, canon and voice changes. The
flagship Universe is **GLMZ** (Greater Lake Michigan Zone, 2226, cyberpunk; the *Bushido Coda*); a
second, **Fantasy/Steampunk**, is being stood up alongside it on the same engine. Every canon and
story row belongs to exactly one Universe ([SS-LAW-15](#SS-§5)).

## 2. The product promise {#SS-§2}

A **complete StreetSamurai engine** can, with minimal human steering, do all of the following and
prove it:

1. **Reach everything.** Any of the ~28 canon entity types (characters, places, factions, gear,
   drugs, materials, orgs, synthetics, documents, …) can surface in generation when relevant — no
   "dead inventory."
2. **Decide from truth.** Every generation/decision is grounded in canon facts + relationships +
   the codified house voice, all read from the **database** (never from an `.md` that might not be
   parsed).
3. **Self-correct.** Generated prose is automatically checked against canon across all types;
   contradictions are detected and a fix is proposed without an admin diffing by hand. Unknown
   entities are captured as provisional canon, flagged for review.
4. **Judge with the whole picture.** Legion/LLM story decisions are fed the totality (retrieved
   canon + continuity state + voice rules) so they choose well instead of producing slop.
5. **Evolve its own voice.** Every strand that scores ≥80% with readers is harvested — its winning
   edits/directives distilled into the codified rules — so each winner sharpens the next.
6. **Run the loop.** Outline → generate → validate/self-correct → narrate → publish → review →
   harvest, repeatable per strand and across a corpus, with audio + manuscript outputs.
7. **Know its gaps.** A standing coverage report shows, per type, what is reachable/validated and
   what is not.

**The engine is "done" when** a fresh strand can be taken from a one-line seed to a published,
reader-reviewed, canon-consistent audiobook + manuscript with the human only approving (not
authoring) canon/voice changes — and the coverage report shows 100% of *diegetic* types reachable
and validated.

The narrative promise: in its flagship Universe (GLMZ), StreetSamurai writes the *Bushido Coda* —
chapter-length literary cyberpunk prose, voice-disciplined and canon-grounded — and a hundred
stories beyond it; **and the same engine writes any other registered Universe's stories** (e.g.
Fantasy/Steampunk) from that Universe's own canon and voice register. See
[§7 Active frontier](#SS-§7) for the corpus pointers.

## 3. What it is NOT {#SS-§3}

- **NOT a folder of canon.** Canon is a *database*, not a pile of `.md`/`.json`. The generator
  reads SQL, never an `.md` that might not be parsed. (`engine_data/*.json` is the **seed/export**
  mirror — see [§4.4](#SS-§4) and [docs/data/](data/) — not the live read path.)
- **NOT a multi-author averaging machine.** Quorum is for *review* (catching what one voter
  misses); prose is written by one voice at a time. No averaging toward mediocrity.
- **NOT substring-grounded.** Substring search is retired wherever it touched generation; semantic
  embeddings ground prompts in the real corpus.
- **NOT a MAUI / desktop app.** Web-only, Blazor Server. No MAUI host.
- **NOT single-universe.** GLMZ is one **Universe** among several, not *the* universe. The engine
  is the product; a Universe is content. The two are decoupled: the rules below marked *(GLMZ)*
  are that Universe's content, not engine truths.
- **NOT cyberpunk cliché** *(GLMZ)*. Currency is Φ (QUANTA = quantum compute-time), never the Greek
  letter phi. Iowan Behemoths are autonomous machines, *not* alive. Heritage is mixed (Ubiquitous
  Diaspora). Cliché is rejected on contact.
- **NOT energy-weapon fantasy** *(GLMZ)*. *Silence* is just a sword; *Chorus* is just a five-shot
  revolver shotgun. See the GLMZ narrative laws in [§5](#SS-§5).

## 4. Architecture canon {#SS-§4}

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

### 4.1 Projects

Solution: `v3/StreetSamurai.slnx` (.NET 10). Active engine lives under `v3/`.

| Project | Role |
|---|---|
| `v3/StreetSamurai.Shared/` | POCOs, enums, DTOs shared by every project |
| `v3/StreetSamurai.Core/` | Canon services, generation pipeline, embeddings, review |
| `v3/StreetSamurai.Blazor/` | ASP.NET Core Blazor Server host — the live site (web-only) |
| `v3/StreetSamurai.Mcp/` | Model Context Protocol server exposing canon to Claude clients |
| `v3/StreetSamurai.UnitTests/` | NUnit + bUnit tests |
| `v3/ApplyMigrations/` + `Apply*`/`Promote*`/`Sync*`/`Write*` consoles | One-shot migration/backfill tools (not runtime) |

The `ss` CLI dispatches from `Program.cs`. Stack: .NET 10, Blazor Server, SQL Server 2025
(LocalDB in dev), ElevenLabs TTS, Legion/LLMVoting (11 providers), embeddings (`VECTOR_DISTANCE`),
QuikGraph. Quick start: `README.md`.

### 4.2 Domain model (the NOUNS — SQL Server is the only canon store)

- **`Universe`** — the top-level tenant: a lookup row (`Id, Slug, Name, Description, Theme,
  IsActive`) naming one fictional world (e.g. `glmz`, `fantasy-steampunk`). **Every canon and story
  root carries a single non-null `UniverseId` FK** (1:M) — `Entities`, `Strands`, `Books`; beats
  and chapters inherit their Universe via their parent strand/book. Reads are universe-scoped
  (`WHERE UniverseId = @u`, enforced engine-wide; see [SS-LAW-15](#SS-§5)). **Crossover policy:** an
  entity needed in two universes is *duplicated*, one row per Universe — there is **no** M:M bridge
  and no shared row. *(Adding `UniverseId` to a system-versioned table requires the
  `SYSTEM_VERSIONING OFF → ALTER table + `_History` → ON` dance.)* Scoping reaches **beyond canon
  rows** to config (`Settings`/`Species`, with a SHARED sentinel for operational keys), retrieval
  (`EntityEmbeddings`/`ProseEmbeddings`), the derived-index caches, the `Edge`/`EntityStateEvent`/
  `CharacterReadModel` ledger, and the LLM prompt "cards" (the `WorldGroundingOr` seam) — see
  [RFC 0006](rfc/0006-universe-segregation.md). Universe ids are UUIDv7 like every other Id.
- **`Entities`** — universal row per entity: `Id, Name, Slug, EntityType, Description, IsActive`.
  The spine.
- **`Records`** — `Records.Json`: a per-entity JSON blob retained as a per-type rollback artifact;
  retired as the canonical store once that type's relational parity passes. See SS-A5.
- Per-type **subtype tables** + bridges — the **canonical** store per type (one repo per type,
  `IExportableRepository`). Relational tables are authoritative; the blob is a fallback until
  its type is converted.
- **`EntityEmbeddings`** — 1536-d vectors; cosine via `VECTOR_DISTANCE`. Covers all active entities.
- **`Edges`** — typed graph relations (parent_of, etc.); cousins/grandparents derived.
- **`EntityStateEvents`** — story-state ledger (location, life status, ammo) — see
  `static_vs_dynamic_split`.
- **System-versioned temporal** tables: `Beats`, `Strands`, `StrandBeats`, `ChapterBeats`
  (+`_History`) — every edit is rewindable (`FOR SYSTEM_TIME ALL`); this is what voice-harvest mines.
- **`CharacterReadModels`** — materialized full-character read-model (non-temporal projection;
  single-writer sync from `CharacterRepository.Save`); never reintroduce on-disk entity JSON.
- **`Settings`** singletons: `literary_rules`, `tone_bible`, `story_bible`, app config
  (`JsonSingletonRepository`).
- **`ContinuityClaims`**, **`Findings`**, **`VoiceChangeLog`**, **`Strand*`/`FocusGroup*`**,
  **`Books`/`Chapters`/`ChapterBeats`**, **`Episodes`/`EpisodeBeats`**.

**The story hierarchy:** **Beat → Strand → Collection → Series** (one model, no parallel formats).
A **Beat** is a discrete unit of story (not a typographic paragraph); a **Strand** is an ordered
set of beats; a **Collection** is any ordered set of strands (modeled as a parent `Strand` via the
`ParentStrandId` tree); a **Series** is a Collection of Collections. **Canon** is the author-only
`Strand.IsCanon` trust gate (see [SS-LAW-9](#SS-§5)).

### 4.3 Key services (the VERBS — one job each)

- **Interconnect:** `EmbeddingService`, `WorldGraphService`, `CanonRetrievalService`,
  `NarrativeSessionContext`, `SemanticIndexService`, `InferenceService`.
- **Generation:** `StoryDirectorService`, `OutlineService`, `BeatGeneratorService`,
  `BeatPromptBuilder`, `StoryStarterService`, `SceneGenerationService`, `DialogueService`,
  `AgendaEngine`, `PacingService`.
- **Voice/rules:** `DatabaseService.GetLiteraryRulesPrompt/GetToneBiblePrompt`,
  `VoiceHarvestService`, `VoiceChangeLog`, `SeedVoiceRulesCli`.
- **Validation/self-correction:** `CanonContradictionService`, `CanonGroundingService`,
  `ContinuityService`/`Extraction`/`Apply`, `WorldConsistencyService`,
  `ContinuityValidatorService`, `FindingsService`.
- **Review/scoring:** `StrandReviewService`, `StrandReviewSummary`, focus groups.
- **Audio/publish:** `StrandWorkbenchService`, `ElevenLabsTtsService`, `ManuscriptExportService`,
  `DocxExportService`, `EpisodeAudioService`.
- **Observe:** `CoverageService`, `FindingsService`.
- **Surfaces:** Blazor pages (`/strand`, `/strands`, `/generate`, `/continuity`, `/findings`,
  `/settings`, encyclopedia dictionaries), the MCP server (`StreetSamurai.Mcp`), and the `ss` CLI.

### 4.4 Canon-as-data (L5)

Structured narrative canon is registered as the L5 data layer in [docs/data/](data/): per-type
JSON Schemas under `docs/data/_schema/` plus a master **entity-identity table**
([docs/data/ENTITY_IDENTITY.md](data/ENTITY_IDENTITY.md)) mapping name ↔ id ↔ fields for the
on-disk seed corpus under `engine_data/`. **`engine_data/*.json` is the seed/export mirror, not the
live read path** — per [SS-LAW-1](#SS-§5) the live canon is SQL. The schemas register the existing
shapes; they do not rewrite canon values.

## 5. The Laws {#SS-§5}

> **Inherits [MindAttic.HouseRules.md](../../MindAttic.HouseRules.md)** (shared, repo-external).
> Those house rules apply in full and are not restated here. Project-specific laws follow. The
> [Amendments log](AMENDMENTS.md) wins over this section when they conflict.

**Engine invariants (do not violate):**

1. **Single source of truth = the SQL database.** {#SS-LAW-1} Canon lives in `Entities` +
   `Records.Json` (+ relational projections). QuikGraph, embeddings, and the `.md` memory rubric
   are **derived indexes, rebuildable from canon and never authoritative**. The generator reads the
   DB, never an `.md`/`.json`.
2. **Separation of responsibilities.** {#SS-LAW-2} One job each: *Retrieve* → `CanonRetrievalService`;
   *Decide* → `StoryDirectorService`/`AgendaEngine`; *Validate* → `CanonContradictionService`/
   continuity; *Codify voice* → `VoiceHarvestService`; *Observe* → `CoverageService`/
   `FindingsService`. A service that needs canon asks the retriever; it does not query
   embeddings/graph directly.
3. **Static vs dynamic split.** {#SS-LAW-3} Identity facts (name, height, ancestry) live on
   canonical entity tables; story-state (location, ammo, life status) lives in the
   `EntityStateEvents` ledger. No denorm "convenience copies."
4. **One home per kind of being (the sentience test).** {#SS-LAW-4} All sentient life — anything
   with feelings/agency — lives in the **Characters** repo, classified by `Species`. Non-sentient
   machines live in the **Automaton** repo. A being is never in both. *The sentience test is an
   engine invariant; the `Species` value set is **per-Universe** — GLMZ's set is exactly `human`,
   `ai`, `elf`, `synthetic`, `unknown`, and another universe defines its own.*
5. **Characters deepen over time.** {#SS-LAW-5} A character's metadata accretes, never resets:
   continuity claims upsert true facts; voice-harvest folds winning prose moves into
   `SpeechPatterns`/`NarrationVoice` (propose-then-approve); state events record change without
   overwriting identity.
6. **One format: everything is a Strand of Beats.** {#SS-LAW-6} Collection/Series are parent
   `Strand`s on the `ParentStrandId` tree — no parallel formats, no new table. A Beat is a unit of
   story function, not a typographic paragraph; the Beat Doctrine is codified in
   `LiteraryRulesData.BeatDoctrine` and emitted by `GetLiteraryRulesPrompt()`.
7. **No underscore-prefixed fields.** {#SS-LAW-7} Private fields are `camelCase` without the
   leading underscore.
8. **Φ is QUANTA, never phi.** {#SS-LAW-8} *(GLMZ.)* The symbol Φ is the QUANTA currency symbol
   (quantum compute-time), *never* the Greek letter phi. (Listed among the engine invariants for
   historical id stability, but it is **GLMZ-universe content**, not an engine truth.)
15. **Every row belongs to exactly one Universe.** {#SS-LAW-15} Every canon/story root
   (`Entities`, `Strands`, `Books`) carries a non-null `UniverseId`; all generation and retrieval
   is universe-scoped. An entity that must appear in two universes is **duplicated** (one row per
   Universe) — never a shared row and never an M:M bridge. (Id 15 is the next free number; the
   narrative laws 9–14 below were allocated earlier.)

**GLMZ / Bushido Coda narrative laws (Universe: GLMZ — validate any GLMZ rewrite against these;
from `v3/canon_writes/story_state.md`). Other universes (e.g. Fantasy/Steampunk) get their own
narrative-law block here when stood up:**

9. **Canon is author-only.** {#SS-LAW-9} `Strand.IsCanon` is set only manually by the author and
   means "strong enough to draw conclusions from." Only canon strands are authoritative for
   voice-harvest, continuity inference, and capability decisions.
10. **Silence is JUST a sword.** {#SS-LAW-10} No glow, no discharge, no charge state, no
    piezoelectric/triboelectric harvest. The "it shorts BCIs" street myth is a myth; neither Seo
    nor Kyle corrects it. Matte-black mono-edged katana, carbon-nanotube blade. If prose says
    otherwise, fix it.
11. **Chorus is JUST a five-shot revolver shotgun.** {#SS-LAW-11} No magazine, no semi-auto. Reload
    is moon clips (~3–6 s). Five rounds, then reload.
12. **Kyle has a motorcycle.** {#SS-LAW-12} Used for distance travel. Default: matte black,
    unbranded, ground-level parking.
13. **The Sable reveal sequence is fixed.** {#SS-LAW-13} Sable is a **mystery voice only** in all
    BCODA chapters before Ch13 (The Offer). Her first in-person appearance is at Vey's Antiquity &
    Stationary in the Faraday vault (Ch13): the AI-reveal and the confession *"Your contracts do
    not come from people."* Her appearance at the motorcycle funeral (Joy strand) is post-Ch13 and
    is correct. Do not place Sable in-person before Ch13 under any circumstance.
14. **The rogue-AI long con stays unconfirmed.** {#SS-LAW-14} The rogue AI is real and routing
    Kyle's contracts, but the full reveal (it has orchestrated his life) lands many books later.
    Bushido Coda only lands the avatar misdirect (Kyle/Sable's *wrong* hypothesis), nothing more.

**Fantasy/Steampunk narrative laws (Universe: fantasy-steampunk — validate any Fantasy/Steampunk rewrite against these):**

16. **Action beats carry thematic weight; contemplative beats have physical immediacy.** {#SS-LAW-16} An action beat that doesn't advance or complicate the strand's central tension is stage business. A contemplative beat without a grounding sensory or physical anchor is abstraction. Both fail. *(Universal beat doctrine — applies to all universes.)*

## 6. Verified state {#SS-§6}

Status legend: `✅ done (verified)` · `🟡 partial` · `⬜ planned` · `🗑️ cut` · `living`.

**Build/test evidence (recorded 2026-06-07, this session):**
- **Build:** `dotnet build v3/StreetSamurai.UnitTests/StreetSamurai.UnitTests.csproj` (Core +
  Shared + tests) — **succeeded, 0 errors, 0 warnings.** (A full-solution `slnx` build also
  compiled clean; its only "errors" were DLL-copy file locks from the live Blazor host running
  during the build — an environment condition, not a code regression.)
- **Tests:** `dotnet test v3/StreetSamurai.UnitTests` (deterministic gate filter) — **114 passed,
  0 failed, 0 skipped** (8 s). The gate suites are `CanonEngineTests`, `StrandWorkbenchServiceTests`,
  `DiRegistrationTests`/`InterfaceRegistrationTests`, `OutlineGateTests`, `BeatHandleTests`,
  `BeatFormatterTests`. **Known pre-existing failures:** ~43 data-dependent integration tests
  (`*_LoadsRealData`, `RuleScan_*`, `ZoneInference_*`) require seeded data/DB not present in a clean
  `dotnet test` — not regressions.

**Proven foundation (shipped & verified):**
- Web-only Blazor Server on SQL Server; DB is sole canon store. *(app runs; `engine_data/*.json`
  migrated to SQL)*
- ~28 canon entity types with tolerant `Records.Json` + relational projections. *(`--coverage`
  lists 28 types)*
- Embedding index over all active entities (`VECTOR_DISTANCE`). *(`--coverage`: 11,588 entities,
  100% after `--backfill`; motif 0→100%)*
- Generation pipeline (outline → beats → scene), persona reviews + 1–100 scoring.
- Audio (one-pass audiobook v2/v3 TTS) + manuscript export (docx/md/txt/pdf) + tier check.
- **Facet system 100% eradicated** (code + tests + DB). *(0 `Facet*` tables/columns;
  `drop_facet_system_20260606.sql`; `CanonEngineTests` regression guards `Beat_HasNoFacetTag`,
  `OutlineBeat_HasNoFacetHint`, `CoreAssembly_HasNoFacetTypes`)*
- Voice codification, full interconnect, canon-true decisions, self-correction, coverage
  instrumentation. *(N1–N6; `CanonEngineTests` 21 tests green)*
- Synthetics + ELFs → Characters; robots → Automaton (sentience test). *(326 → Characters; active
  synthetics 0; `SyntheticLife` retired; 116 tests pass)*

Per-story status with cited tests lives in [docs/USER_STORIES.md](USER_STORIES.md).

## 7. Active frontier {#SS-§7}

Software frontier (the road to the endpoint; ordered) — full backlog with acceptance tests in
[docs/USER_STORIES.md](USER_STORIES.md):
- **F1 Ship present work to prod** ⬜ · **F6 Coverage → action** 🟡 (appearance tracking residual)
- **F7 In-app review surfaces** ⬜ (`/voice`, `/coverage` pages) · **F8 Autonomous corpus loop** ⬜
- **Fh Hierarchy + Collection builder** ⬜ (drag-and-drop on `/strands`)
- **F9 Living world tick** ⬜ · **F10 Voice flywheel proof** ⬜
- **Fs2 Species as a first-class type** ⬜
- See design notes in [docs/rfc/](rfc/).

Narrative frontier (Bushido Coda):
- **Canon spine** — 8-chapter Book One; canon prose register is **v8** (see [§ canon note](#SS-§6)
  and [docs/USER_STORIES.md](USER_STORIES.md) audit). Working corpus:
  `v3/canon_writes/bushido_coda_100_stories_outline.md` (100-story outline, stories 1–8 = the
  spine, 9+ = sketches).
- **Style register:** `engine/bushido_coda_v3/00_style_guide.md` (v8 register, the rulebook).
- **World reference:** `v3/canon_writes/network_doc.md` (the Network in 2226).
- **Session/state notes:** `v3/canon_writes/story_state.md` (the per-session canon scratch — its
  continuity invariants are promoted to the Laws in [§5](#SS-§5)).

## 8. Quality bar {#SS-§8}

A software feature is **done** only when:
- It is **verified** by a deterministic test (named in [USER_STORIES.md](USER_STORIES.md)) or a
  reproducible build/CLI smoke (`--canon-retrieve`, `--coverage`, `--harvest-voice`,
  `--check-canon`, `--seed-voice-rules` against LocalDB). "Done" never means "written."
- The full DI graph resolves (`DiRegistrationTests`/`InterfaceRegistrationTests`).
- No engine invariant in [§5](#SS-§5) is violated; no new on-disk entity JSON read path is added.
- ARCHITECTURE/BIBLE status is flipped in the same change that moves the goal, with acceptance
  evidence.

A prose beat/strand is **done** only when:
- It obeys the Beat Doctrine and the v8 style register, and passes the forbidden-term scan for
  Silence/Chorus power references ([SS-LAW-10](#SS-§5), [SS-LAW-11](#SS-§5)).
- It does not violate any narrative continuity law ([SS-LAW-9](#SS-§5)–[SS-LAW-14](#SS-§5)).
- For canon promotion: the author sets `Strand.IsCanon` ([SS-LAW-9](#SS-§5)).

## 9. Glossary {#SS-§9}

- **Universe** — the top-level tenant: one self-contained fictional world (e.g. GLMZ,
  Fantasy/Steampunk). Every canon/story row belongs to exactly one ([SS-LAW-15](#SS-§5)). The
  **current Universe** is selected *per CLI process and per UI session* (not a single shared
  global), so two CLIs — or two browser tabs — can target different universes simultaneously
  ("SwitchUniverse"). Engine terms below are universe-neutral; terms tagged *(GLMZ)* are GLMZ
  content.
- **GLMZ** *(GLMZ)* — Greater Lake Michigan Zone (a.k.a. Meridian 88, *The Glooms*); a 500-km
  vertical megacity on the western shore of Lake Michigan, year 2226. Universe #1.
- **Φ / QUANTA** *(GLMZ)* — the currency: one Φ = one second of certified error-corrected quantum
  coherence. Never the Greek letter phi ([SS-LAW-8](#SS-§5)).
- **Beat** — a discrete unit of story function (not a paragraph); the atom of prose + audio.
- **Strand** — an ordered set of beats; the unit generated, validated, reviewed, narrated, published.
- **Collection / Series** — ordered sets of strands / of collections, on the `ParentStrandId` tree.
- **Canon** — author-only `Strand.IsCanon` trust gate ([SS-LAW-9](#SS-§5)).
- **E.L.F.** *(GLMZ)* — Emergent Life Form; a sentient `Species` living in Characters.
- **Automaton** *(GLMZ)* — non-sentient machine repo (Iowan Behemoths, robots, drones) — *not alive*.
- **Facet** — a retired psychology-weighting system, **100% eradicated** ([§6](#SS-§6)).
- **Voice harvest** — distilling a ≥80%-scoring strand's winning edits into the codified
  `literary_rules`/`tone_bible`/character voice fields (propose-then-approve).
- **Silence** *(GLMZ)* — Kyle's katana; *just a sword* ([SS-LAW-10](#SS-§5)).
- **Chorus** *(GLMZ)* — Kyle's five-shot revolver shotgun ([SS-LAW-11](#SS-§5)).
- **Bushido Coda** *(GLMZ)* — GLMZ's flagship narrative work; Book One is an 8-chapter spine.
- **The Pulse** *(GLMZ)* — Mach-6 magnetic vacuum transit network. **The Network** *(GLMZ)* — the
  ambient proprioceptive information field BCI-augmented people sense (see `network_doc.md`).

## 10. New story / book workflow {#SS-§10}

> **Invariant: docs and entities before prose.** No prose is generated until every step marked
> ★ is complete for this story. This order is enforced; skipping it produces stories that reference
> characters or CorpoNations that don't exist in the DB, breaking continuity retrieval.

### Step 1 — Canon first ★

- **If the story introduces new world facts** (new CorpoNation, new species, new world event, new
  narrative laws for a character): append a new `SS-AN` entry to `docs/AMENDMENTS.md`.
- **Always**: add a new story entry to `docs/USER_STORIES.md` under the appropriate Epic, with
  sub-items for entity seeding, chapter structure, each act, and the review target.
- Run `pwsh tools/codex.ps1 doctor` — must pass before proceeding.

### Step 2 — Entity seeding ★

Every **named entity** that appears in the story must be in the DB:
- Characters: `ss --add-character --name "..." --species human [--description "..."]`
- CorpoNations: `ss --add-corponation --name "..." [--description "..."]` (or via MCP `add_entity`)
- Places, Weapons, Documents, etc.: via MCP `add_entity` or the appropriate CLI

Run `ss --scan-entity-mentions --strand <slug>` after every chapter draft to keep coverage current.

### Step 3 — Book structure ★

1. Create a **book-level strand** (`kind=book`): `ss --write-strand --seed "..." --kind book`
   or via the UI.
2. Create **chapter sub-strands** as children of the book strand (`kind=chapter`, `--parent <slug>`).
   Target ~28 chapters for a KDP paperback (~80k words); ~12–15 for a novella.
3. The authorial spine (14-beat outline or equivalent) is saved as the book strand's `seed` text —
   it is the **outline**, not the final prose.

### Step 4 — Prose generation

For each chapter, in order:
1. **Sonnet draft** — `ss --expand-beat` or the Writer UI (Sonnet is the draft model).
2. **Opus polish** — mandatory; never ship Sonnet-only prose.
3. **`ss --reflow-strand --slug <chapter-slug>`** — fix paragraph and dialogue mechanics.
4. **`ss --review-strand --slug <chapter-slug>`** — Legion panel; target ≥82% before next chapter.
5. **`ss --scan-entity-mentions --strand <book-slug>`** — keep coverage current after each chapter.

### Step 5 — Export and review

- After all chapters reach draft standard: full-book review panel; target ≥85%.
- `ss --publish-docx --slug <book-slug>` → KDP-ready .docx.
- Voice harvest if any chapter scores ≥80%: `ss --harvest-voice --strand <chapter-slug>`.
- Flip all USER_STORIES.md sub-items to ✅ with evidence in the same commit.

> **Invariant (SS-A15):** The emotional depth score (`EmotionalDepthService`) is a side-car signal
> with a Deep-tier advisory cap. It scores prose against an 8-dimension rubric (0–4 per dimension,
> 0–100 aggregate) and files blocking Findings for `WantNeedDivergence` / `CostFeltNotAsserted`.
> A strand with open blocking emotional Findings cannot be marked publish-ready at the Deep gate.
> It **never** folds into the 82/85 headline reader-panel score; `Strand.Score` and the review gates
> are untouched by any emotional examination run. See [RFC 0010](rfc/0010-emotional-intelligence-examination.md).

---

*Maintenance rule: when you finish a goal, flip its status here and in
[USER_STORIES.md](USER_STORIES.md) and add acceptance evidence in the same change. This file is the
source of truth for "what's left and in what order." Amendments are append-only in
[AMENDMENTS.md](AMENDMENTS.md) and win over this file.*

---

## 11. The Feedback Architecture {#SS-§11}

The engine's most important property is not any single service — it is how the services form closed
feedback loops that make each story better than the last. Every loop is a check-and-balance circuit:
a generator produces output; one or more validators or reviewers evaluate it; their judgment feeds
back into the generator's inputs. If a loop is broken — findings accumulate but are never reviewed,
voice harvest is never run, coverage is never checked — the system degrades toward stochastic prose.
**The loops being wired is necessary but not sufficient; the author must use the surfaces.**

### Loop 1 — Canon Grounding {#SS-§11-L1}

```
Entity seeding (CLI / MCP / UI)
  → EntityEmbeddings (1536-d VECTOR_DISTANCE index)
  → CanonRetrievalService (semantic + graph at generation time)
  → BeatPromptBuilder (injects canon-facts block into every prompt)
  → BeatGeneratorService (prose grounded in real entities)
  → ss --scan-entity-mentions (gap report: mentioned but not seeded)
  → Author seeds gap entities → index rebuilt → loop closes
```

**What it prevents.** Prose that references characters, places, or gear that don't exist in the
DB — the most common cause of continuity breakage. If all entities are seeded before prose fires
([SS-LAW-1](#SS-§5), [§10](#SS-§10)), the scanner finds 0 gaps. Any gap found is the signal to seed
the entity; the next generation cycle closes it.

**Health signal.** `ss --scan-entity-mentions --strand <slug>` returns 0 unseeded mentions.

### Loop 2 — Post-Beat Validation {#SS-§11-L2}

```
Beat saved (StrandWorkbenchService.SaveBeatAsync)
  → PostBeatValidationService (orchestrator)
      ├─ ProsePatternGuard:             catch anti-patterns (pseudo-profound, substring)
      ├─ GearCarryEnforcer:             verify character carries only seeded gear
      ├─ BehavioralInvariantEnforcer:   check per-character behavioral rules
      └─ WeaponAmmoCompatibilityService: validate ammo↔weapon pairing
  → FindingRow written (status: pending) → /findings inbox
  → Author: approve / reject / dismiss
  → Approved: FindingApplyService patches prose; next beat generation avoids the violation
```

**What it prevents.** Prose that silently violates gear, behavior, or weapon rules established in
earlier beats. Each violation caught here avoids multiple review-panel rewrites later.

**Health signal.** `/findings` inbox is empty, or every open finding has been explicitly adjudicated,
before a strand is submitted for review.

### Loop 3 — Continuity {#SS-§11-L3}

```
Beat saved
  → ContinuityExtractionService: LLM extracts (entity, predicate, object) triples
  → ContinuityService: stores claims
  → BeatStateExtractor: extracts EntityStateEvents (location, life status, ammo)
  → WorldStateAtBeatService: reconstructs world state at any beat on demand
  → ContinuityValidatorService: pre-checks next beat against established facts
  → CanonContradictionService (--check-canon): raises CANON-CONTRADICTION findings
  → Author approves fix → prose patched → loop closes
```

**What it prevents.** A character who was shot in beat 5 appearing uninjured in beat 7 without
explanation. The continuity ledger is the engine's working memory of what has happened.

**Health signal.** `ss --check-canon --slug <slug>` returns 0 unresolved contradictions.

### Loop 4 — Review → Voice Harvest (The Flywheel) {#SS-§11-L4}

```
Strand complete
  → StrandReviewService: Legion persona panel (N readers, 1–100 score per reader)
      → StrandReview rows + StrandReviewSummary aggregate
      → Strand.Score updated; StrandScoreHistory row appended
  → Score < 80%: low-score findings raised; strand flagged for rewrite
  → Score ≥ 80%:
      → VOICE-HARVEST finding auto-raised
      → VoiceHarvestService mines temporal Beat history (FOR SYSTEM_TIME ALL)
      → Winning edits → VoiceChangeLog (status: proposed)
      → Author approves → directive folded into literary_rules / tone_bible / Character fields
  → Next strand generation uses improved rules → higher baseline score
```

**The flywheel property.** The loop is self-reinforcing: better `literary_rules` → better prose →
higher scores → more harvests → even better rules. The endpoint ([SS-US-F10](#)) is demonstrable:
batch K+1 mean score > batch K mean score across N harvests.

**What it prevents.** Voice drift. Each ≥80%-scoring strand is proof the engine produced something
that worked; harvest crystallizes WHY it worked so the next strand starts from a higher baseline.
Without this loop the engine would perpetually forget its own successes.

**Health signal.** Every ≥80%-scoring strand has a non-empty `VoiceChangeLog` set. No strand sits
at ≥80% with 0 harvest rows (unless harvest was explicitly waived by the author).

### Loop 5 — Semantic Fidelity Guard {#SS-§11-L5}

```
Strand review complete
  → SemanticFidelityService: compare prose embedding centroid to seed embedding
  → Goodhart's Law check: is prose optimising for review vocabulary
    while drifting from the seed's actual intent?
  → SEMANTIC-DRIFT finding if drift exceeds threshold
  → Author adjudicates: keep (intent legitimately changed), rewrite (drift genuine), adjust seed
```

**What it prevents.** The Goodhart's Law trap — prose that scores high by adopting reviewer
vocabulary but has quietly lost what the seed was about. This is the guard against a system that
learns to game its own metrics.

**Health signal.** `ss --check-fidelity --slug <slug>` returns no SEMANTIC-DRIFT findings, or all
findings have been adjudicated.

### Loop 6 — Coverage {#SS-§11-L6}

```
ss --coverage (CoverageService)
  → Per-type reachability matrix across all 28 registered entity types
  → Dead inventory: types with 0% appearance in any generated prose
  → Author acts: enrich entities of dead type, or include in a forthcoming seed
  → Next coverage run shows improvement
```

**What it prevents.** A canon with 28 registered types where prose only ever mentions characters
and places, leaving weapons, pharmaceuticals, automata, and the rest unreachable. The matrix is the
engine's standing self-audit of what it is actually using.

**Health signal.** `ss --coverage` shows >0% for all diegetic types. Any type stuck at 0% for
two or more consecutive strands is a retrieval bug or an empty entity roster — fix before shipping.

### Loop 7 — World-State / Temporal {#SS-§11-L7}

```
Generation pre-flight (WorldStatePrecheckService)
  → WorldStateAtBeatService: reconstruct exact world state at the last committed beat
      (who is where, who is alive, how much ammo remains)
  → WorldClockService: advance in-world clock to the beat's timestamp
  → Pre-check failures → generation prompt adjusted before prose fires (not after)

All beats in a strand (and across strands)
  → EntityStateEvents ledger: append-only world-state changes
  → Available to any future generation, validation, or continuity check
```

**What it prevents.** Generating prose that assumes the wrong world state: a character who is
supposed to be dead, a gun emptied two beats ago, a location the character left last chapter. This
is the engine's forward-looking continuity guard (vs. the backward-looking
`ContinuityExtractionService`).

**Health signal.** `WorldStatePrecheckService` returns no violations before any beat generation.

### System invariant: all loops must close {#SS-§11-invariant}

Every loop has an author-facing surface where its findings land:

| Loop | Finding category | Surface |
|---|---|---|
| Canon Grounding | Unseeded entity gaps | `ss --scan-entity-mentions` output |
| Post-Beat Validation | `PROSE-GUARD`, `GEAR-CARRY`, `BEHAVIOR`, `AMMO` | `/findings` inbox |
| Continuity | `CANON-CONTRADICTION` | `/findings` inbox |
| Voice Harvest | `VOICE-HARVEST` | `/findings` inbox + `/voice` page |
| Fidelity Guard | `SEMANTIC-DRIFT` | `/findings` inbox |
| Coverage | Per-type 0% flags | `ss --coverage` output + `/coverage` page |
| World-State | Pre-check violations | Generation pre-flight log |

A loop is considered **closed** when all its findings have been explicitly adjudicated (approved /
rejected / dismissed). A loop with unreviewed findings is a **circuit break** — the author's
attention is the closing mechanism and cannot be automated away.

---

## 12. Service Communication Laws {#SS-§12}

These laws govern HOW services are allowed to talk to each other. Violations make the feedback loops
in [§11](#SS-§11) fragile or invisible. They are enforced via interface contracts, DI registration
tests, and code review.

**SCL-1: Generators ask; they do not query.**
No generation service (`BeatGeneratorService`, `OutlineService`, `StoryDirectorService`, etc.) may
query the DB or the embedding index directly. All canon retrieval goes through
`CanonRetrievalService`. This ensures the retrieval layer — which owns universe scoping, embedding
fallback, and semantic ranking — is the only surface the generator touches.
*Audit: grep `StreetSamuraiDbContext` or `EntityEmbeddings` in generation services — 0 hits.*

**SCL-2: Validators file; they do not fix.**
No validation service (`CanonContradictionService`, `ProsePatternGuard`, `GearCarryEnforcer`,
`BehavioralInvariantEnforcer`, `ContinuityValidatorService`, `SemanticFidelityService`) may write
to prose tables. Violations are filed as `Finding` rows. Only `FindingApplyService` (triggered by
an author approval) writes prose changes. Every correction is traceable and author-gated.
*Audit: none of the validator services inject `StrandWorkbenchService` or `BeatRepository`.*

**SCL-3: Voice changes are always proposed, never applied.**
`VoiceHarvestService` writes only to `VoiceChangeLog` (status: proposed). Only
`ApproveVoiceChange` (author-triggered via `/voice` page or CLI) writes to `Settings.literary_rules`,
`Settings.tone_bible`, or Character voice fields. No service auto-commits a voice change.
*Audit: grep direct writes to `literary_rules` / `tone_bible` in service code — 0 hits outside
the explicit approve path.*

**SCL-4: World state is always at-beat, never "current."**
No service calls "get the current world state." The only temporal API is
`WorldStateAtBeatService.GetStateAtAsync(beatId)`. This prevents stale-state bugs where "current"
means different things to different services depending on when they happen to run.
*Audit: no public `GetCurrentWorldState()` method exists on any service.*

**SCL-5: Entity identity stays on entity tables; story-state stays in the ledger.**
No "convenience copy" columns. A character's location is not on `Characters`. It is in
`EntityStateEvents` with a `Source`, a `BeatId`, and a `Timestamp`. Any query for location asks
`WorldStateAtBeatService`.
*Audit: `DiRegistrationTests` + `InterfaceRegistrationTests` + no `Location` column on `Characters`.*

**SCL-6: The universe scope is ambient, not a parameter.**
Services do not accept a `UniverseId` parameter. The current universe is set on the ambient
`IUniverseContext` (per-process for CLI, per-circuit for Blazor, per-request for MCP), and the EF
global filter enforces it automatically. A service cannot accidentally cross universe boundaries
mid-request.
*Audit: `UniverseSegregationTests` (10 tests); no `UniverseId` parameter on any service method.*

**SCL-7: No generation bypasses the outline gate.**
`BeatGeneratorService` will not fire if `OutlineService` has not produced an outline that passed
`OutlineReviewService`. The gate is enforced structurally.
*Audit: `OutlineGateTests`.*

**SCL-8: Reviews never auto-apply their editorial conclusions.**
`StrandReviewService` writes scores and summaries. It does not patch beats, does not update voice
rules, and does not raise rewrites. A review result is observation; action requires the author
(or an explicitly author-approved agent).

---

## 13. The Quality Invariants {#SS-§13}

Measurable properties the system must maintain end-to-end. Their purpose is to make "the system is
working" observable rather than assumed.

**QI-1: Every generated beat has been through all validation loops before it is considered a
draft.**
*Evidence:* `PostBeatValidationService` runs on every `StrandWorkbenchService.SaveBeatAsync`. The
`/findings` inbox must be empty (or all open findings explicitly adjudicated) before a strand is
submitted for review.

**QI-2: Every review-eligible strand has a score.**
*Evidence:* `Strand.Score` is null only before a review panel has run. A null-score strand cannot
be published or harvested. `ss --review-strand` must run before `ss --publish-*`.

**QI-3: Every ≥80%-scoring strand has triggered a voice harvest attempt.**
*Evidence:* The `VOICE-HARVEST` finding is auto-raised at the `<80→≥80` crossing. A strand that
scores ≥80% with 0 `VoiceChangeLog` rows is a broken flywheel loop — diagnose and re-run
`ss --harvest-voice --slug <slug>`.

**QI-4: The coverage report shows 0 dead diegetic types.**
*Evidence:* `ss --coverage` shows >0% for all 28 registered types. A type at 0% for two or more
consecutive strands is either an empty roster (seed entities) or a retrieval bug (fix
`CanonRetrievalService`).

**QI-5: The DI graph resolves with no missing registrations.**
*Evidence:* `DiRegistrationTests` + `InterfaceRegistrationTests` green on every build. This is the
system's structural self-test — every service referenced by another must be registered.

**QI-6: All entity mentions in prose are seeded in the DB.**
*Evidence:* `ss --scan-entity-mentions --strand <slug>` produces 0 unseeded mentions. Run after
every chapter draft.

**QI-7: Voice changes are not auto-applied.**
*Evidence:* `VoiceChangeLog` is the single write path to `literary_rules` / `tone_bible`. A direct
write to those `Settings` keys from any service other than the approve handler is a SCL-3 violation.
*Audit: grep `literary_rules` in service code — only appears in `GetLiteraryRulesPrompt()` (read)
and the approve handler (write).*

**QI-8: No cross-universe data appears in any generation prompt.**
*Evidence:* `UniverseSegregationTests` (10 tests: query-filter scoping, insert-stamping, shared-key
visibility, strand scoping, epoch, uuid-v7). A GLMZ-only entity appearing in a Fantasy/Steampunk
beat's canon-facts block is a segregation failure — check `IUniverseContext` wiring.

**QI-9: All structural pre-flight checks pass before a review panel fires.**
*Evidence:* `ss --diagnose-strand --slug <slug>` (StructuralDiagnosticService, 12 parallel LLM
checks) returns no critical failures. Review panels run against structurally sound prose; a panel
scoring a fundamentally broken strand wastes voters and produces misleading score history.

**QI-10: Each strand's score trends upward across its revision history.**
*Evidence:* `StrandScoreHistory` table. A score that trends downward after voice-harvest application
is a signal the harvested directive is incorrect — revert the approval and re-examine the finding.
The flywheel must demonstrably spin forward.

---

## 14. The Architectural Decision Register {#SS-§14}

Why key choices were made — so future architects understand the reasoning and can judge whether it
still holds. Each ADR names the decision, the why, the tradeoff, and the condition under which it
should be revisited.

**ADR-1: SQL Server is the sole canon store.**
*Decision:* All canon lives in SQL. `.md`/`.json` files are documentation or export mirrors only.
*Why:* A file system has no foreign keys, no temporal history, no query scope, no transactions.
Continuity validation, world-state reconstruction, and voice harvest all require queryable,
relational, time-aware data. Files cannot deliver this.
*Revisit when:* The app moves to a client-only model with no server. Not a current direction.

**ADR-2: System-versioned (temporal) tables for Beats and Strands.**
*Decision:* `Beats`, `Strands`, `StrandBeats`, `ChapterBeats` use `SYSTEM_VERSIONING ON`.
*Why:* Voice harvest mines `FOR SYSTEM_TIME ALL` to find which edits correlated with score
improvements. Without temporal history the flywheel has no data to mine.
*Tradeoff:* Adding a column to a temporal table requires the `SYSTEM_VERSIONING OFF → ALTER →
_History → ON` dance. Accepted overhead for the harvest capability it enables.
*Revisit when:* SQL Server 2025 vector indexes become compatible with system-versioned tables
(currently incompatible; tracked as a known constraint).

**ADR-3: Legion/LLM voting rather than a single judge for review.**
*Decision:* Review panels use N voters (Legion, 11 providers, up to 100 for some actions).
*Why:* A single LLM judge is a single point of bias. A voting panel surfaces disagreement — which
is more informative than a consensus score. The minority "gripe" is where the most actionable
editorial signal lives.
*Tradeoff:* Cost and latency. Mitigated by the sampled-panel pattern (cheap score-ballots first;
upgrade only the most informative few to full prose).
*Revisit when:* A single high-capability judge demonstrably produces better editorial signal than a
panel at lower cost. Not proven; panel diversity remains the safer default.

**ADR-4: Propose-then-approve for voice harvest and canon changes.**
*Decision:* No service auto-commits a change to `literary_rules`, `tone_bible`, or any entity field.
*Why:* The LLM's "this is a winning move" verdict is a heuristic, not ground truth. Auto-applying
would let the system modify its own voice without human review — the Goodhart's Law failure mode at
the system level, not just the prose level.
*Revisit when:* F10 demonstrates the flywheel produces consistently ≥90% scores across 50+ strands
without any author veto on harvested directives. Not before.

**ADR-5: One format (Strand of Beats) — no parallel format tables.**
*Decision:* Books, chapters, episodes, and collections are all parent/child Strand trees on the
`ParentStrandId` column. No separate table per format.
*Why:* Every new format table is a new maintenance surface. The audio pipeline, review pipeline,
voice harvest, and export pipeline would each need to handle every format. Strand-of-Beats
abstracts over all of them: one pipeline, one set of loops.
*Tradeoff:* The mental model requires understanding that `kind=book` is a parent strand, not a row
in a `Books` table. The legacy `Books` table is being retired; its presence is a migration artifact.

**ADR-6: Per-universe entity duplication, not M:M bridging.**
*Decision:* An entity that must appear in two universes gets two rows (one per Universe), not a
shared row with a junction table.
*Why:* An M:M bridge means every query that touches entities must join the bridge — the most
frequent query in the system. The author prefers ~10 duplicate rows over a double-bridge refactor
that would touch every retrieval, scoping, and embedding path. The cost of duplication is bounded
(number of crossover entities is small and stable).
*Revisit when:* Crossover entities grow to hundreds per type and the duplication cost (stale-copies
drift) exceeds the bridge cost. Currently well under the threshold.

**ADR-7: EF global query filter for universe scoping, not per-query WHERE clauses.**
*Decision:* Universe scoping is enforced at the DbContext level via an EF global query filter keyed
on `IUniverseContext`, not by adding `WHERE UniverseId = @u` to every query.
*Why:* Per-query clauses are a convention. Any developer can forget one; there is no structural
guarantee. The global filter is structural: a query cannot bypass it without explicitly calling
`IgnoreQueryFilters()`, which is auditable and rare. Make the right thing easy; make the wrong
thing hard.
*Revisit when:* A use-case legitimately requires reading across all universes in a single query
without bypassing the filter. Use `IgnoreQueryFilters()` explicitly and document the reason.

**ADR-8: CharacterReadModels as a non-temporal CQRS projection.**
*Decision:* `CharacterReadModels` is a materialized, non-temporal full-character read-model. It is
single-writer (synced from `CharacterRepository.Save`) and never holds entity JSON.
*Why:* A naïve full-character read joins ~30 character sub-tables. At read time that is a 50–80 s
multi-join. The read-model materializes the result once on write; reads are a single column select.
*Tradeoff:* The read-model can be stale if a direct SQL update bypasses `CharacterRepository.Save`.
Convention: every character write goes through the repository. No exceptions.

**ADR-9: Beat Doctrine as a DB-resident rule, not a hardcoded prompt string.**
*Decision:* The Beat Doctrine and house voice rules live in `Settings` (`literary_rules`,
`tone_bible`) and are emitted by `DatabaseService.GetLiteraryRulesPrompt()`. They are not
hardcoded in any service.
*Why:* Rules that live in code can only be changed by a developer and a deployment. Rules that live
in `Settings` can be updated by the voice-harvest flywheel (with author approval) without a code
change. The flywheel only works if the rules it updates are actually read by generation.
*Revisit when:* A rule is so structural that it cannot safely change without a code change (e.g.
universe scoping logic). Those rules belong in code; everything else belongs in Settings.
