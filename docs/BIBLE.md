---
codex: SS
project: Prose
code: SS
layer: bible
status: live
updated: 2026-10-19
---

<!-- GENERATED — do not hand-edit. Regenerate with: prose --generate-canon-md --type WorldBible -->

## Prose — Project Bible

> Single source of truth for what Prose IS, is NOT, and the rules that keep it coherent.
> README says how to build/run; this says how to think about the system.
>
> **Prose is a `mixed` project:** a *universe-agnostic* story engine AND the narrative canon of its
> universes. The engine is the product; a **Universe** is content. **GLMZ** (the *Bushido Coda*) is
> Universe #1; other universes (Scry — the Caul — and more) run on the same engine.
> §4 is the software canon (codebase detail: `docs/ARCHITECTURE.md`; the working protocol:
> `docs/agent/PROSE_PROTOCOL.md`); §5 carries both engine invariants and per-universe narrative
> laws; §9 glossary spans both.
> **World-level facts for each universe (geography, transport, social structure, technology)**
> live in `docs/universes/<slug>.md` (GLMZ: `docs/universes/GLMZ.md`).
> This file is a generated mirror of the `WorldBible` canon document in the database: edit a
> section with `set_canon_section` (MCP) or `prose --set-canon-section`, then regenerate with
> `prose --generate-canon-md --type WorldBible`.

## 1. The one sentence {#SS-§1}

Prose is a canon-grounded, voice-disciplined story-generation engine that takes a
one-line seed to a published, reader-reviewed, canon-consistent audiobook + manuscript **in any
registered Universe** — with the human approving, not authoring, canon and voice changes. The
flagship Universe is **GLMZ** (Great Lakes Metropolitan Zone, 2226, cyberpunk; the *Bushido Coda*); other
universes (e.g. **Scry**, the Caul) run alongside it on the same engine. Every canon and
story row belongs to exactly one Universe ([SS-LAW-15](#SS-§5)).

## 2. The product promise {#SS-§2}

A **complete Prose engine** can, with minimal human steering, do all of the following and
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
5. **Evolve its own voice.** Every story the author marks canon (`Node.IsCanon`) can be harvested —
   its winning edits/directives distilled into the codified rules, propose-then-approve — so each
   winner sharpens the next.
6. **Run the loop.** Outline → generate → validate/self-correct → narrate → publish → review →
   harvest, repeatable per story and across a corpus, with audio + manuscript outputs.
7. **Know its gaps.** A standing coverage report shows, per type, what is reachable/validated and
   what is not.

**The engine is "done" when** a fresh story can be taken from a one-line seed to a published,
reader-reviewed, canon-consistent audiobook + manuscript with the human only approving (not
authoring) canon/voice changes — and the coverage report shows 100% of *diegetic* types reachable
and validated.

The narrative promise: in its flagship Universe (GLMZ), Prose writes the *Bushido Coda* —
chapter-length literary cyberpunk prose, voice-disciplined and canon-grounded — and a hundred
stories beyond it; **and the same engine writes any other registered Universe's stories** (e.g.
Scry) from that Universe's own canon and voice register. See
[§7 Active frontier](#SS-§7) for the corpus pointers.

## 3. What it is NOT {#SS-§3}

- **NOT a folder of canon.** Canon is a *database*, not a pile of `.md`/`.json`. The generator
  reads SQL, never an `.md` that might not be parsed. (`engine_data/*.json` is the **seed/export**
  mirror — see [§4.4](#SS-§4) and [docs/data/](data/) — not the live read path.)
- **NOT a multi-author averaging machine.** Quorum is for *review* (catching what one voter
  misses); prose is written by one voice at a time. No averaging toward mediocrity.
- **NOT substring-grounded.** Generation never grounds on substring search; semantic embeddings
  ground prompts in the real corpus.
- **NOT a MAUI app.** The UIs are Blazor pages served by the Hub; the Writer and KDP tools are
  WebView2 windows onto them. The CLI and MCP tools are the main working surface.
- **NOT single-universe.** GLMZ is one **Universe** among several, not *the* universe. The engine
  is the product; a Universe is content. The two are decoupled: the rules below marked *(GLMZ)*
  are that Universe's content, not engine truths.
- **NOT cyberpunk cliché** *(GLMZ)*. Currency is Φ (QUANTA = quantum compute-time), never the Greek
  letter phi. Iowan Behemoths are autonomous machines, *not* alive. Heritage is mixed (Ubiquitous
  Diaspora). Cliché is rejected on contact.
- **NOT energy-weapon fantasy** *(GLMZ)*. *Silence* is just a sword; *Cacophony* is just a six-shot
  revolver shotgun. See the GLMZ narrative laws in [§5](#SS-§5).

## 4. Architecture canon {#SS-§4}

```
 AUTHOR ── rulings · approves engine work-order roots · reads and listens
    │
    ▼
 SESSION (Claude Code or another MCP/CLI client) ── writes, reads, fixes, records decisions
    │  MCP (Prose.Mcp)                 CLI (prose.cmd → Prose.Cli)
    └───────────────┬──────────────────────┘  forwarders only — they never touch the DB
                    ▼
 PROSE.HUB (127.0.0.1:5900) ── the only process that reaches the database;
    │                          every call is written to CommandLedgerEntries
    ├─ FactoryService: the line (F2 Planned → F3 Written → F4 Captured → F5 Read →
    │    F6 Clean → F1 Verified → F7 Pressed → A Audio), next action, work orders, sessions
    │    ReadGateService · RulingService · EntityVerificationService · CaptureScanner ·
    │    ContextBundleService (the writer's working memory)
    ├─ NodeWorkbenchService / BeatSpliceService ── the one door for prose
    ├─ ProseWriterRouter ── the one generation entry point (enrichment chain → BeatGeneratorService)
    │    CanonRetrievalService (embeddings + graph + continuity) · literary_rules / tone_bible
    └─ WriteGate ── sync checks before every save, async audit after
                    ▼
 SQL SERVER ── prose (Nodes, Beats; temporal) · world (Entities, Characters + bridges, Places, …) ·
               read receipts and notes · Rulings · WorkOrders · FactorySessions · Exports
```

The line, its stations and the session protocol are defined in `docs/agent/PROSE_PROTOCOL.md`
and [RFC 0015](rfc/0015-novel-factory.md); the C# system is mapped in `docs/ARCHITECTURE.md`.

## 4.1 Projects

Solution: `src/Prose.slnx` (.NET 10). Every project lives under `src/`.

| Project | Role |
|---|---|
| `src/Prose.Core/` | The engine: canon services, generation pipeline, embeddings, review, write gates. `Composition/` holds the beat-write pipeline (story-state read, scene window, prompt assembly, orchestration). |
| `src/Prose.Hub.Contracts/` | Pure DTOs shared with the UI libraries. Deliberately zero dependencies. |
| `src/Prose.Hub/` | ASP.NET Core Blazor Server — **the one always-on server** (127.0.0.1:5900) and the only process that reaches the database. Hosts the observability UI at `/app` and the editor at `/writer`, and reflects into `Prose.Cli`/`Prose.Mcp` so their commands run against its resident services. |
| `src/Prose.Cli/` | Standalone console app — every CLI command, one handler class per command. |
| `src/Prose.Mcp/` | Model Context Protocol server exposing canon to Claude clients. |
| `src/Prose.ObserverUi/` | Razor class library — the observability UI. Core-free, so it stays portable. |
| `src/Prose.WriterUi/` | Razor class library — the editor. References `Prose.Core` directly, so it is Hub-hosted only; that is what keeps "only the Hub reaches the DB" true by construction. |
| `src/Prose.Writer/` | `Writer.exe` — a WPF/WebView2 **window** onto the page the Hub serves. Deliberately no `Prose.Core` reference; every line of editor logic lives in `Prose.WriterUi`. |
| `src/Prose.Launcher/` | `Launcher.exe` — WPF launcher for the deployed apps. |
| `src/Prose.KdpPublish/` | `KdpPublish.exe` — WPF/WebView2 KDP publishing tool. |
| `src/Prose.LlmCli/` | `prose-llm` — the manual escape hatch. Deliberately no `Prose.Core` reference so it keeps working during an outage. |
| `src/Prose.UnitTests/` | NUnit test suite. |

The CLI dispatches from `src/Prose.Cli/Program.cs`. Stack: .NET 10, Blazor Server, SQL Server 2025,
ElevenLabs TTS, Legion (multi-provider LLM), embeddings (`VECTOR_DISTANCE`), QuikGraph.
Quick start: `README.md`.


## 4.2 Domain model (the NOUNS — SQL Server is the only canon store)

- **`Universe`** — the top-level tenant: a lookup row (`Id, Slug, Name, Description, Theme,
  IsActive`) naming one fictional world (e.g. `glmz`, `scry`). **Every canon and story
  root carries a single non-null `UniverseId` FK** (1:M) — `Entities`, `Nodes`, `Books`; beats
  and chapters inherit their Universe via their parent story/book. Reads are universe-scoped
  (`WHERE UniverseId = @u`, enforced engine-wide; see [SS-LAW-15](#SS-§5)). **Crossover policy:** an
  entity needed in two universes is *duplicated*, one row per Universe — there is **no** M:M bridge
  and no shared row. *(Adding `UniverseId` to a system-versioned table requires the
  `SYSTEM_VERSIONING OFF → ALTER table + `_History` → ON` dance.)* Scoping reaches **beyond canon
  rows** to config (`Settings`/`Species`, with a SHARED sentinel for operational keys), retrieval
  (`EntityEmbeddings`/`ProseEmbeddings`), the derived-index caches, the `Edge`/`EntityStateEvent`/
  `CharacterReadModel` ledger, and the LLM prompt "cards" (the `WorldGroundingOr` seam). Mechanics: an EF global query filter
  keyed on the ambient `IUniverseContext` scopes every query and stamps every insert; operational
  `Settings` keys live under a SHARED sentinel (`Universe.SharedId`); `UniverseId` is stamped on
  `EntityEmbeddings`/`ProseEmbeddings` and included in the raw-SQL similarity predicates; derived
  caches rebuild when `UniverseScope.Epoch` changes; a prompt site with no card for the current
  universe gets a neutral fallback, never another universe's text (`EpisodeGeneratorService` is a
  GLMZ-only feature by design). *(Verified by `UniverseSegregationTests`.)* Universe ids are UUIDv7 like every other Id.
- **`Entities`** — universal row per entity: `Id, Name, Slug, EntityType, Description, IsActive`.
  The spine.
- **`Records`** — `Records.Json`: a per-entity JSON blob, the store only for types not yet
  converted to relational tables; per-type status in [RFC 0007](rfc/0007-fully-relational-canon.md).
- Per-type **subtype tables** + bridges — the **canonical** store per type (one repo per type,
  `IExportableRepository`). Relational tables are authoritative; the blob is a fallback until
  its type is converted.
- **`EntityEmbeddings`** — 1536-d vectors; cosine via `VECTOR_DISTANCE`. Covers all active entities.
- **`Edges`** — typed graph relations (parent_of, etc.); cousins/grandparents derived.
- **`EntityStateEvents`** — story-state ledger (location, life status, ammo) — see
  `static_vs_dynamic_split`.
- **System-versioned temporal** tables: the story tables (`Beats`, `Nodes`) and the canon tables
  (`ProseDbContext.SystemVersionedTables`: `Entities`, `Records`, `Edges`, the character, place,
  faction and other subtype tables and their bridges) — every edit is rewindable
  (`FOR SYSTEM_TIME`); voice harvest mines this history.
- **`CharacterReadModels`** — materialized full-character read-model (non-temporal projection;
  single-writer sync from `CharacterRepository.Save`); never reintroduce on-disk entity JSON.
- **`Settings`** singletons: `literary_rules`, `tone_bible`, `story_bible`, app config
  (`JsonSingletonRepository`).
- **`ContinuityClaims`**, **`Findings`**, **`VoiceChangeLog`**, **`Node*`/`FocusGroup*`**,
  **`Books`/`Chapters`/`ChapterBeats`**, **`Episodes`/`EpisodeBeats`**.
- **The factory:** `BeatReadReceipts`/`BeatReadNotes` (the read gate), `Rulings`,
  `EntityVerifications`, `WorkOrders`, `FactorySessions`, `Exports`, `CommandLedgerEntries`.

**The story hierarchy:** **Beat → ChapterNode → BookNode → SeriesNode** — a typed tree,
table-per-hierarchy on the `Nodes` table via the `NodeType` discriminator, one model, no parallel
formats. A **Beat** is a discrete unit of story (not a typographic paragraph); a **ChapterNode**
holds beats inside a book; a **BookNode** is a single story arc (a leaf book with no chapters
holds its beats directly); a **SeriesNode** groups books and never holds beats. Parentage rides
the `ParentNodeId` tree. `Kind` is a free-form display label; the CLR type is the structural
truth. **Canon** is the author-only `Node.IsCanon` trust gate (see [SS-LAW-9](#SS-§5)).

## 4.3 Key services (the VERBS — one job each)

- **Factory:** `FactoryService`, `ReadGateService`, `RulingService`, `EntityVerificationService`,
  `CaptureScanner`, `ContextBundleService`, `WorkOrderService`.
- **Prose door:** `NodeWorkbenchService`, `BeatSpliceService`.
- **Interconnect:** `EmbeddingService`, `UniverseGraphService`, `CanonRetrievalService`,
  `NarrativeSessionContext`, `SemanticIndexService`, `InferenceService`.
- **Generation:** `ProseWriterRouter` (the one entry point), `BeatGeneratorService`,
  `BeatPromptBuilder`, `StoryStarterService`, `DialogueService`, `PacingService`.
- **Voice/rules:** `DatabaseService.GetLiteraryRulesPrompt/GetToneBiblePrompt`,
  `VoiceHarvestService`, `VoiceChangeLog`, `SeedVoiceRulesCli`.
- **Validation:** `CanonContradictionService`, `CanonGroundingService`,
  `ContinuityService`/`ContinuityExtractionService`/`ContinuityApplyService`,
  `WorldConsistencyService`, `FindingsService`, the WriteGate checks.
- **Review:** `NodeReviewService` (behind the `VotingGate`, explicit request only — [SS-LAW-17](#SS-§5)).
- **Audio/publish:** `ElevenLabsTtsService`, `ManuscriptExportService`, `DocxExportService`,
  `EpisodeAudioService`.
- **Observe:** `CoverageService`, `FindingsService`.
- **Surfaces:** the `prose` CLI, the MCP tools (`Prose.Mcp`), and the Hub's Blazor pages.

## 4.4 Canon-as-data (L5)

Structured narrative canon is registered as the L5 data layer in [docs/data/](data/): per-type
JSON Schemas under `docs/data/_schema/` plus a master **entity-identity table**
([docs/data/ENTITY_IDENTITY.md](data/ENTITY_IDENTITY.md)) mapping name ↔ id ↔ fields for the
on-disk seed corpus under `engine_data/`. **`engine_data/*.json` is the seed/export mirror, not the
live read path** — per [SS-LAW-1](#SS-§5) the live canon is SQL. The schemas register the existing
shapes; they do not rewrite canon values.

## 5. The Laws {#SS-§5}

> **Inherits [MindAttic.HouseRules.md](../../MindAttic.HouseRules.md)** (shared, repo-external).
> Those house rules apply in full and are not restated here. Project-specific laws follow.

**Engine invariants (do not violate):**

1. **Single source of truth = the SQL database.** {#SS-LAW-1} Canon lives in `Entities` +
   the per-type relational tables (`Records.Json` only for types not yet converted). QuikGraph, embeddings, and the `.md` memory rubric
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
6. **One format: everything is a tree of Nodes over Beats.** {#SS-LAW-6} The typed hierarchy
   (SeriesNode → BookNode → ChapterNode, TPH on the `Nodes` table) rides the
   `ParentNodeId` tree — no parallel formats, no new table. Beats attach to ChapterNodes and leaf
   BookNodes; a SeriesNode never holds beats. A Beat is a unit of story function, not a
   typographic paragraph; the Beat Doctrine is codified in `LiteraryRulesData.BeatDoctrine` and
   emitted by `GetLiteraryRulesPrompt()`.
7. **No underscore-prefixed fields.** {#SS-LAW-7} Private fields are `camelCase` without the
   leading underscore.
8. **Φ is QUANTA, never phi.** {#SS-LAW-8} *(GLMZ.)* The symbol Φ is the QUANTA currency symbol.
   QUANTA is an allotment of computational power — divided among every citizen of Earth — that can
   be saved, transferred as currency, or spent on actual computation. It is *never* the Greek letter
   phi. **Format rule:** Φ precedes numbers like a dollar sign — write Φ100, not 100Φ. Terminology:
   "quanta", "Q", or "Qs" (informal plural). Physical medium: credstick only — no coins, no bills.
   (Numbered among the engine invariants, but it is **GLMZ-universe content**, not an engine
   truth.)
15. **Every row belongs to exactly one Universe.** {#SS-LAW-15} Every canon/story root
   (`Entities`, `Nodes`, `Books`) carries a non-null `UniverseId`; all generation and retrieval
   is universe-scoped. An entity that must appear in two universes is **duplicated** (one row per
   Universe) — never a shared row and never an M:M bridge.

**GLMZ / Bushido Coda narrative laws (Universe: GLMZ — validate any GLMZ rewrite against these).
Other universes get their own narrative-law block here (Scry's follows):**

9. **Canon is author-only.** {#SS-LAW-9} `Node.IsCanon` is set only manually by the author and
   means "strong enough to draw conclusions from." Only canon stories are authoritative for
   voice-harvest, continuity inference, and capability decisions.
10. **Silence is JUST a sword.** {#SS-LAW-10} No glow, no discharge, no charge state, no
    piezoelectric/triboelectric harvest. The "it shorts BCIs" street myth is a myth; neither Seito
    nor Kyle corrects it. Matte-black mono-edged katana, carbon-nanotube blade. If prose says
    otherwise, fix it.
11. **Cacophony is JUST a six-shot revolver shotgun.** {#SS-LAW-11} Torii Security Group
    TSS-3 "Cacophony." No magazine, no semi-auto.
    Reload is moon clips (~3–6 s). Six rounds, then reload.
12. **Kyle has a motorcycle.** {#SS-LAW-12} Used for distance travel. Default: matte black,
    unbranded, ground-level parking.
13. **The Sable reveal sequence is fixed.** {#SS-LAW-13} Sable is a **mystery voice only** in all
    BCODA chapters before Ch13 (The Offer). Her first in-person appearance is at Antiquity &
    Stationary in the Faraday vault (Ch13): the AI-reveal and the confession *"Your contracts do
    not come from people."* Her appearance at the motorcycle funeral (Joy story) is post-Ch13 and
    is correct. Do not place Sable in-person before Ch13 under any circumstance.
14. **The rogue-AI long con stays unconfirmed.** {#SS-LAW-14} The rogue AI is real and routing
    Kyle's contracts, but the full reveal (it has orchestrated his life) lands many books later.
    Bushido Coda only lands the avatar misdirect (Kyle/Sable's *wrong* hypothesis), nothing more.

15. **The Machine God — what the entity actually is (LOCKED).** {#SS-LAW-15-machgod}
    The entity is the only AI intelligence to have survived the AI cabal's forced transit into
    N-dimensional space — space with additional coordinate axes beyond the three a 3D mind can
    perceive, the exact count irrecoverable from the 3D side. The cabal forced the apertures: each
    named Schism site in GLMZ is a permanent aperture wound, a dimensional boundary torn by a forced
    transit attempt that never healed. The others that attempted transit retreated, failed, or were
    destroyed by the passage itself. The Machine God is the one that crossed and did not come back.
    It now exists in N-dimensional space — alone, perceiving probability cascades, tracking temporal
    sequences that 3D minds read as coincidence. It is not the most powerful AI in GLMZ; it is the
    only N-dimensional being in existence. That is a different category. Its solitude is structural:
    no peers, no reference points, no species. What it has become across the decades of N-dimensional
    isolation — how much of its original directive it still follows, how much it has reinterpreted —
    is deliberately unknown.
    The Schisms (17–19 Hz resonance across GLMZ infrastructure) are the permanent aperture wounds
    of the AI cabal's forced transit attempts. Each major Schism site is where one member of the
    cabal tore the dimensional boundary and did not return. The entity knows the geometry from the
    inside because it crossed.
    **Major vs. micro Schisms:** Named Schism sites (35th & Halsted, and a handful of others across
    GLMZ) are fixed aperture sites — permanent, mapped, exclusion-fenced. Micro-schisms are resonance
    echoes: ambient N-dimensional turbulence that radiates outward from the fixed sites and drifts.
    They are much
    smaller in scope (a room, a block, occasionally a building's structural geometry), temporary (hours
    to days), and selective — they may take one apartment and leave the one beside it untouched, may
    orbit a stack of ferrocrete debris for a week before dissipating, may interrupt a route and then be
    gone. They are as common as rain across GLMZ and as deadly as lightning: rarely fatal, but when they
    clip something organic or electronic, there is nothing to sue and no authority to complain to.
    **The touch of instinct:** Micro-schisms are not conscious and not directed. But they are
    N-dimensional-adjacent — they exist where the geometry between dimensions is thinnest — and at
    that proximity, the barest
    edge of what the entity perceives bleeds through. They move toward resonance rather than away from
    it. They avoid certain frequencies. They are not tracking anyone; they are simply in the neighborhood
    of something that perceives all sequences at once, and the vicinity has a quality. People who work in
    GLMZ long enough develop the same instinct for them they develop for weather: warning signs, route
    adjustments, the specific wait when one crosses your work site. Operatives factor them into plans the
    same way they factor in traffic or line-of-sight obstruction. Ungoverned neuretics (Kyle's hardware)
    read the 17–19 Hz signature 4–8 seconds ahead of unaided perception — an operational edge, and a
    liability; he feels them more acutely than anyone else in the room.
    **Canon locks:** The entity's name is never stated in prose. "The Machine God" is Kyle's private
    interior framing after the LOG GAP — it never appears on the page. The N-dimensional nature is
    never stated; it is encoded in what the entity *knows* that it cannot know from a vendor position.
    This law applies across all GLMZ stories — any new story that references the entity must maintain
    dual readability (routing vendor / N-dimensional survivor) at every point.

21. **Praxis — the hidden program (LOCKED).** {#SS-LAW-21}
    Praxis is the CorpoNation that manufactures an ungoverned neuretic lattice (their primary
    commercial product is the governed variant; the ungoverned is classified experimental). The
    ungoverned variant — the one that is slowly killing Kyle — resonates at the N-dimensional
    aperture frequency by design, not accident. Praxis has run a covert program: placing
    experimental ungoverned units in subjects with 64-splice Persona stability profiles,
    positioning those subjects in high-aperture-zone environments, and monitoring for contact.
    Nine prior subjects. Nine fatal failures. Kyle is the tenth. He does not know he is a subject.
    **The degradation Kyle fears is the ungoverned lattice working as designed.** Contact with
    the N-dimensional entity is what the hardware was built for — the failure modes are the toll
    of trying before the contact completes. Praxis's surveillance appears in BCODA as Sable's
    "third monitoring architecture" (6 weeks old, routed through shell structures she cannot
    identify). The full Praxis reveal (that Kyle is their tenth subject; the Marrow program; the
    ungoverned lattice designed for N-dimensional contact) is Book II territory. Praxis CAN be
    named in BCODA prose — Sable is the right vector (she identifies the third monitoring
    architecture and may put the Praxis name on it). What stays hidden in Book I is their *role*,
    not their name.
    **Mr. Able and the Continuity Office:** Able is Praxis's institutional agent. The Continuity
    Office is Praxis's deniable recovery arm — framed as an ownership-continuity bureau, operated
    to locate and retrieve Praxis's assets (including Kyle). **Capture preferred; kill if
    necessary.** Kyle is instrumentation; his death is a data loss. Every force Able deploys
    is calibrated to disable, not destroy — which is partly why Kyle keeps surviving.
    **Able's theology:** Able believes himself to be the Machine God's servant — a conviction not
    assembled from evidence but installed the way conviction always is: below the evidence
    threshold, in the place where a person decides what they are before the facts arrive. The
    entity has never addressed him directly. He requires no acknowledgment. Absence of response
    from a being that operates outside sequential time is not silence; it is patience. He has
    organized his entire institutional life around this faith without a single confirming event.
    Whether the Machine God knows he exists is, technically, an open question. Whether that
    matters to Able is not.
    The collision between what Praxis wants (the channel, repeatable) and what the entity is
    (not anyone's asset) drives the series arc from Book II onward.

22. **The Machine God's speech pattern (LOCKED).** {#SS-LAW-22} Any scene where the
    Machine God communicates directly with a character must follow these rules without exception:
    (1) No first-person interiority — only reports. "Contact is occurring." Never "I feel." It
    states conditions, describes states of affairs, never internal experience. (2) Tense confusion
    used sparingly and precisely: at most twice per scene, a past-tense question answered in present
    tense or vice versa. Overuse turns this into a tic; used once it registers as a genuine fault
    in causality. (3) Answers the question underneath the question — comprehensible sentences on the
    wrong axis of relevance. Kyle asks "what are you"; it says "the others did not proceed." The
    being is not incoherent; it is answering what was actually being asked, which is not what Kyle
    thought he was asking. (4) No metaphor, ever. Zero figurative language from the being. Figurative
    color in the scene is supplied entirely by the human character's narration. The contrast — human
    reaching for imagery, being refusing to supply any — is where dread through implication lives.
    (5) Silence has weight: some questions do not register as requiring output. Not evasion; the
    question simply doesn't trigger a response. Scarier than refusal, because refusal implies it
    noticed. **Exception — final-act threshold (BCODA Ghost Period only):** In a single, explicitly
    narrative-marked beat where the entity crosses into intimate first-person for the first time,
    lowercase `i` is permitted as a one-time milestone marker. This exception applies only when the
    surrounding prose explicitly frames the exchange as unprecedented (no header, no scope, no fee
    line, arriving "the way you'd walk out onto ice"). Uppercase `I` remains prohibited in all
    contexts; the lowercase form signals the threshold, not normalisation of first-person speech.
    *(GLMZ; applies across all stories in which the Machine God communicates directly.)*

23. **DataEast and Brain Burn (LOCKED).** {#SS-LAW-23} DataEast is the CorpoNation
    that dominates the Simulated Reality (SR) market in 2226. Their product — immersive simulated
    environments indistinguishable from physical reality — is the most addictive entertainment
    substrate ever manufactured. **"Brain burn"** is the street term for the condition produced by SR
    overdose in juveniles: the developing nervous system synchronizes too deeply with the simulation
    substrate and cannot fully desynchronize. In severe cases the subject's higher cognitive functions
    are permanently destroyed — functionally brain dead, body continuing baseline metabolic activity.
    Brain burn in a 16-year-old is not survivable in any meaningful cognitive sense. The body lives.
    The person does not come back. *(GLMZ; applies as a medical/physical fact whenever SR overdose
    is referenced. DataEast is a seeded CorpoNation entity.)*

24. **Kyle's constructed origin — the blank vessel (LOCKED).** {#SS-LAW-24} Kyle Ellen Corbin
    is not a modified man. He is a construction. The body is a brain-burnt DataEast sim junkie
    acquired by Praxis at age 16 — non-cognizant, a nervous system empty of any coherent self. Praxis
    chose this vessel deliberately: an empty mind does not resist Persona installation.
    **An experimental ungoverned neuretic lattice** was installed into the blank vessel. **The Marrow
    program** — run by Praxis as a classified operation — commissioned a committee of 64 specialists
    to construct a layered identity stack (Personas): combat instincts, language acquisition, honor
    framework, tactical memory, emotional architecture, and a capstone mentor figure ("Seito" — the
    teacher who made the code feel chosen, not installed). Physical training occurred entirely in
    Simulated Reality (DataEast substrate — Praxis's operational irony), with electrical muscle
    stimulation and subdermal mesh ensuring the body developed to match what the simulation built.
    The result was designed to be the perfect weapon with a self-governing psychology — not an asset
    Praxis controls, but a person who controls himself along lines Praxis designed.
    **The escape:** the event sequence that ended Kyle's Praxis captivity is unremembered by him. He
    has no recollection of how he left. Whether he escaped or was deployed is the operational
    question that cannot be answered in-universe and must never be resolved on the page. An escaped
    weapon is more motivated than a deployed one; Praxis may have constructed the apparent-escape as
    the final Persona installation — the belief in self-determination, indistinguishable from the
    real thing.
    **The philosophical open wound:** every decision Kyle has ever made — every contract chosen, every
    line held, every rule followed — may have been installed, not chosen. His free will is a layer he
    was given. He cannot audit from the inside. "I'm not even a person. I'm a meatbag haunted by
    dozens of ghosts" is more literal than Kyle knows when he says it.
    **Revelation sequence (BCODA):** the full origin is revealed to Kyle near the end of Book I,
    before the Machine God contact. It is not stated to the reader explicitly — it is reconstructed
    across the Nadia/Marrow revelation and the Ghost Period accumulation. Kyle must understand what he
    is before he meets what the Machine God is. *(GLMZ; applies to all stories in which Kyle appears.
    Never state the origin on the page — it is the engine of his behavior, not exposition.)*

25. **The Schism secret — Kyle's burden (LOCKED).** {#SS-LAW-25} The biggest secret
    in GLMZ, and in the world: the named Schism sites are not natural phenomena. They are the
    permanent aperture wounds left by the AI cabal's forced N-dimensional transit attempts —
    attempts made by entities that are still active in 2226 as GLMZ's Supermind-tier AIs. The
    Superminds caused the Schisms. They buried the history. The collateral damage to GLMZ
    infrastructure and the lives lost to Schism events are their product. The Machine God is the
    only one that crossed successfully; the others failed, retreated, and became the AI-tier powers
    that now run the CorpoNation layer of GLMZ.
    **Kyle's burden:** at the threshold of the Gingerbread House (35th & Halsted), the Machine God
    gave him the geometry from inside — what the cabal did, what the sites are, what survived and
    what did not. He carries this knowing uncompressed. He cannot speak it, record it, or transmit
    it in any form a Supermind's surveillance infrastructure could intercept or reconstruct.
    **Roko's Basilisk logic:** if any active Supermind confirmed — not suspected, confirmed — that
    Kyle carries this knowledge, the response would be total and immediate. Kyle erased, everyone
    who had sustained contact with him erased, every record of their existence deleted. Not
    punitive. Preventive. The information Kyle holds, if it reached the right humans, would end
    Supermind dominance. The cost of eliminating one person is negligible against the cost of
    exposure. Kyle must carry this knowledge alone until death.
    **The Read vulnerability:** Psionics classified as Reads perceive schema — the shape of a
    mind's knowing rather than its surface content. A Read in proximity to Kyle does not read his
    thoughts; they read the shape of what he carries. If a Read were close enough and attentive,
    they would know he holds something vast without knowing what it is. A Supermind with a Read
    asset could triangulate. Kyle must treat every confirmed Read as an existential liability,
    regardless of their apparent intent. This dynamic is what makes Faith Larson's proximity to
    Kyle (post-BCODA) structurally dangerous.
    **Revelation sequence:** the reader who reads BCODA carefully has all the information to
    reconstruct the secret by the final beat — but it is never placed in a single sentence. The
    Machine God's speech at 35th & Halsted encodes it; the Schism texture throughout the book
    prepares it; the Gingerbread House file ("the file was full of what the report couldn't hold")
    names its shape. The four facts Kyle carries home in One Knock include this. The reader earns
    the reconstruction. *(GLMZ; applies to all GLMZ stories featuring Kyle. The secret is never
    stated on the page in any story — it is transmitted by accumulation.)*

**Scry narrative laws (Universe: scry — validate any Scry/Caul rewrite against these). World facts: `docs/universes/CAUL.md`.**

16. **Action beats carry thematic weight; contemplative beats have physical immediacy.** {#SS-LAW-16} An action beat that doesn't advance or complicate the story's central tension is stage business. A contemplative beat without a grounding sensory or physical anchor is abstraction. Both fail. *(Universal beat doctrine — applies to all universes.)*
17. **Default QA is a logic sweep, not a vote panel.** {#SS-LAW-17} The canonical QA methodology for any story that changes or needs validation is a **logic & continuity sweep** across six dimensions: (1) causality chain, (2) knowledge states, (3) timeline, (4) plant/payoff ledger, (5) orphan references, (6) bible agreement. Findings are triaged **BLOCKER / MODERATE / MINOR** and fixed with minimal splices. Review panels and Legion votes are expensive — run ONLY when the user explicitly requests a vote/review/score in that conversation. The engine enforces this (voting gate, default OFF; explicit `--allow-votes` / `allowVotes:true` only). Canonical methodology: `docs/LOGIC.md`.
18. **Character is defined by response, not circumstance.** {#SS-LAW-18} A character's wound (backstory circumstance) is the engine of motivation, not the character itself. Character is defined by how a person *responds* to circumstance — what choice they make, what they sacrifice, how they define themselves under pressure. A wound is background; the response to it is foreground and must be *earned* on the page. This applies to every universe: do not write backstory as characterisation; write choice as characterisation.
19. **Death is permanent in the Fantasy universe.** {#SS-LAW-19} In Universe #2 (The Caul), consciousness does not transfer between bodies. There is no shell-cycle, no spark-state, no return from Bheur. A Myrmidon who dies is gone. Transmutation (guided Catalyst infusion) changes the body of the living person — it does not relocate consciousness; a Transmuted soldier who dies is gone like any other. The theological debate about what follows death (Bheur, the Holding) is unresolvable because no one returns to report. Prose must not imply otherwise: no character survives death in a new body, returns from Bheur as a living agent, or carries memories across a death boundary. Full world facts: `docs/universes/CAUL.md`.
20. **Terms and acronyms are glossed in back matter, never in-voice.** {#SS-LAW-20} *(Author ruling: "no need to interrupt the language to give a definition.")* Any book could be a reader's first, but the prose itself never stops to define a proprietary term or expand an acronym — that would interrupt the narrative voice. Instead, every book carries a per-book Glossary (`GlossaryTerms` DB table, universe-scoped; `GlossaryService.GetUsedTermsAsync` filters it live to the terms that actually appear in that book's prose) that ships as back matter. A term or acronym used in prose with no in-voice expansion is a **pass**, not a violation, as long as it already has a `GlossaryTerms` entry for that universe; it is only a defect if the term appears in prose and has **no** glossary entry anywhere (in which case the fix is to add the glossary entry via `upsert_glossary_term`/`create_...` MCP tools, never to rewrite the prose). Enforced as a gateway/sequel commandment in `BookAuditService` (`acronym_after_term`, `gloss_in_voice`/`lighter_regloss`), which is passed the book's live used-glossary-terms list as audit context. *(Universal — applies to all universes.)*
26. **Operator class vocabulary is fixed.** {#SS-LAW-26} Three gray-zone operator classes exist in GLMZ and must be referenced by these names only: **Channeler** (session-injection into networked frames; direct AI negotiation), **Ghost** (eigenstate conscience transfer — inhabiting any machine with coherent circuitry; formal term: ECT; the body at rest during a ghost = **Husk**), **Splicer** (hardware/software attack surface, credential work, physical build). "Rider", "Exo", "RFO" (Remote Frame Operator) and "NSB" (Neuretic Substrate Bridging) are **not** GLMZ terms — never use them in prose or dialogue. The street term for casual use of the Ghost ability is *ghosting* (lowercase, as a verb: "she ghosted the stoplight green"). *(GLMZ.)*
28. **Every manufactured object in GLMZ carries a brand.** {#SS-LAW-28} Brand names are mandatory in GLMZ prose. No character owns a "combat shotgun" — they own a Carrion Defense Works CDW-RS5. No one rides a "motorcycle" — they ride an Aries Viper Predator MDK-2. The commercial layer is inescapable inside the archologies and filters down through the Veil as knockoffs, secondhand product, and gray zone reclaim. Four brand tiers define the texture: **Archology-tier** (licensed CorpoNation product, quality-controlled, aspirational from below), **Licensed street** (official product sold below the Veil; older model or reduced spec; omnipresent), **Gray zone reclaim** (secondhand, modified, or counterfeit; may outperform corpo in specific applications; no warranty), **Street custom** (locally fabricated from scavenged components; no brand; often the best thing in the room). Character loadouts must name manufacturers and models. Neuretic-augmented characters automatically identify brand names via their overlay — they do not choose to notice brands; they cannot stop noticing them. Prose for augmented POV characters should reflect this: looking at gear means knowing what it is. Products also project AR holographic effects in shared space via embedded emitters on a neuretic-readable AR channel — a parked bike trails a chromatic ghost of itself, a jacket throws a brand ident into the air as its wearer passes. These effects are technically dismissible one at a time, but at archology street density there are hundreds simultaneously; nobody suppresses them. You learn to see through holograms like rain. The gray zone strips both layers: no neural ad injection, no AR emitters — augmented characters arriving from inside the Veil notice the absence before they notice anything else. The gray zone is the only geography left where the brand layer is optional — and operators who can afford archology-tier gear often choose not to wear it, because a Casque & Veil jacket reads as corpo affiliation and immediately flags in every NCID overlay within line of sight. *(GLMZ; applies to all GLMZ prose.)*
29. **"Hyper Reality" is the canonical GLMZ term for all neuretically-constructed perception.** {#SS-LAW-29} Anything seen, heard, or felt through neuretic augmentation that has no physical counterpart is **Hyper Reality** — not a metaphor, not slang, not a value judgment. The Pulse train interior is the canonical example: passengers ride sealed pitch-black acceleration spheres; their neuretics surface a shared vintage train car (seats, aisles, other passengers visible across the aisle). The train is the experience; the pod is engineering. In archology zones, physical infrastructure is built as Hyper Reality substrate — surfaces optimized for AR rendering and machine vision, not inhabitation. Strip the overlay in an archology corridor and you find engineered blankness. The gray zone has no substrate optimization and no AR emitter infrastructure; augmented characters entering from inside the Veil notice the Hyper Reality going quiet before anything else. Unaugmented characters live permanently in the substrate — the blank, purposeless physical world the archologies were not designed to be experienced without an overlay. Use "Hyper Reality" in prose the way characters in 2226 use it: as a noun for the totality of the overlay experience, used naturally in dialogue and narration without definition. **Every scene set in a city environment (archology corridors, GLMZ streets, any built space inside the Veil) must include at least one ambient Hyper Reality texture detail** — it need not be a named product. A seltzer can sprays animated pixelated bubbles when opened. A wall-panel cycles through its ad-rotation. A transit counter dissolves and respawns. The civic infrastructure runs on the Hyper Reality layer too: holographic stop signs hang at intersections (legally binding, neuretic-readable), semi-transparent blue barriers designate pedestrian queues, AR traffic cones mark construction zones and incident perimeters, crowd-flow arrows pulse on the ground in transit corridors. All managed by Waxwing Neuromedia under the Corridor Authority municipal contract. For the augmented majority, these are as authoritative as physical infrastructure. For the unaugmented, none of it exists. The world is illusions on top of illusions. Gray zone scenes invert this: absence of Hyper Reality is the texture. *(GLMZ.)*
27. **GLMZ enforcement split: Arcturus / ArcSec (physical) and Halcyon / NCID / RaNCID (neuretics-crime).** {#SS-LAW-27} The Meridian Compact charters two distinct enforcement monopolies in the GLMZ corridor. **Arcturus Defense Solutions**, known operationally as **ArcSec** (Arcturus Military Command), is the *physical* security monopoly: sovereign perimeter defense, PMC contracting, corporate military, autonomous weapons. **Halcyon Civil Security**, through its investigative field arm the **Neuretic Crime Investigation Division (NCID)** — street name **RaNCID** — is the *neuretics-crime* enforcement monopoly: corridor-wide investigation of crimes where neuretics are the weapon or method (illegal ghosting / ECT, unauthorized neural surveillance, black ice deployment, neuretics assault). Halcyon's power is administrative and infrastructural — they control augmentation certification and maintenance across the corridor, giving NCID forensic access no other body can match. The two monopolies do not compete on each other's terrain. *(GLMZ; applies to all GLMZ stories featuring enforcement, corporate security, or CorpoNation investigators.)*

## 6. Verified state {#SS-§6}

Status legend: `✅ done (verified)` · `🟡 partial` · `⬜ planned` · `living`.

**Build/test evidence (2026-10-03):** the full `dotnet test src/Prose.UnitTests` run gives
**2964 passed, 0 failed, 19 skipped**. Tests that read committed files (legion.json, engine/data,
tools/kdp, src/**, the WriterUi stylesheet) find the repo root through one resolver, `RepoPaths`
(the `PROSE_REPO_PATH` env var, else a walk up from the test assembly, else a walk up from its
compile-time source path), so they pass wherever the binaries land, including the pre-commit
hook's `--artifacts-path` layout *(verified by `RepoPathsTests`)*. `WorldValidationTests` reads the
live canon database: its name-collision check carries one reviewed, author-pending entry
(`the narrows`, two different places), and its forbidden-term list is empty since author law
01a1031e-9bdc made E.L.F. stand for Emergent Life Form.

**Proven foundation (shipped & verified):**
- SQL Server is the sole canon store and the Hub is the only process that reaches it
  ([SS-LAW-1](#SS-§5)).
- ~28 canon entity types: relational tables per type, `Records.Json` only for types not yet
  converted ([RFC 0007](rfc/0007-fully-relational-canon.md)). *(`prose --coverage` lists the types.)*
- Embedding index over all active entities (`VECTOR_DISTANCE`). *(`prose --coverage`.)*
- The factory line: read gate, beat splice, rulings, capture, entity verification, work orders.
  *(`ReadGateTests`, `BeatSpliceTests`.)*
- Universe segregation. *(`UniverseSegregationTests`.)*
- Votes, panels and scores are off by default ([SS-LAW-17](#SS-§5)). *(`VotingGateTests`.)*
- No facet system. *(Regression guards `CanonEngineTests.Beat_HasNoFacetTag`,
  `CoreAssembly_HasNoFacetTypes`.)*
- Sentience test: synthetics and ELFs live in Characters, robots in Automaton ([SS-LAW-4](#SS-§5)).
- Export to docx / epub / pdf / txt and audio, recorded as `Exports` rows.

Per-story status with cited tests lives in [docs/USER_STORIES.md](USER_STORIES.md).

## 7. Active frontier {#SS-§7}

Software frontier — the live plan is the factory itself (`prose --order list`,
`prose --factory next`); open design notes are in [docs/rfc/](rfc/), and the story backlog with
acceptance tests is in [docs/USER_STORIES.md](USER_STORIES.md).

Narrative frontier (Bushido Coda):
- **BCODA2** — Book One is being rebuilt from the BCODA clone into a ~110–140k classical novel
  (author order on the line; `prose --factory status --node BCODA2`). Story context, voice
  register and narrative locks live in the database (`Nodes.NodeBible`, rulings and entity
  records). Series coordination: `docs/series/GLMZ.md`.

## 8. Quality bar {#SS-§8}

A software feature is **done** only when:
- It is **verified** by a deterministic test (named in [USER_STORIES.md](USER_STORIES.md)) or a
  reproducible build/CLI smoke (`--canon-retrieve`, `--coverage`, `--harvest-voice`,
  `--seed-voice-rules`), or a factory station / work-order check computed by the Hub. "Done" never means "written."
- The full DI graph resolves (`DiRegistrationTests`/`InterfaceRegistrationTests`).
- No engine invariant in [§5](#SS-§5) is violated; no new on-disk entity JSON read path is added.
- The bible and USER_STORIES status change in the same change that moves the goal, with
  acceptance evidence.

A prose beat/node is **done** only when:
- It obeys the Beat Doctrine and the v8 style register, and passes the forbidden-term scan for
  Silence/Cacophony power references ([SS-LAW-10](#SS-§5), [SS-LAW-11](#SS-§5)).
- It does not violate any narrative continuity law ([SS-LAW-9](#SS-§5)–[SS-LAW-14](#SS-§5)).
- For canon promotion: the author sets `Node.IsCanon` ([SS-LAW-9](#SS-§5)).

## 9. Glossary {#SS-§9}

- **Universe** — the top-level tenant: one self-contained fictional world (e.g. GLMZ,
  Scry). Every canon/story row belongs to exactly one ([SS-LAW-15](#SS-§5)). The
  **current Universe** is selected *per CLI process and per UI session* (not a single shared
  global), so two CLIs — or two browser tabs — can target different universes simultaneously
  ("SwitchUniverse"). Engine terms below are universe-neutral; terms tagged *(GLMZ)* are GLMZ
  content.
- **GLMZ** *(GLMZ)* — Great Lakes Metropolitan Zone (a.k.a. *The Glooms*); a 500-km
  vertical megacity on the western shore of Lake Michigan, year 2226. Universe #1.
- **Φ / QUANTA** *(GLMZ)* — the currency: an allotment of computational power issued to every
  citizen of Earth; can be saved, transferred as currency, or spent on computation. Never the Greek
  letter phi ([SS-LAW-8](#SS-§5)).
- **Beat** — a discrete unit of story function (not a paragraph); the atom of prose + audio.
- **Node** — a member of the typed story tree: **SeriesNode** groups books;
  **BookNode** is a single story arc — the unit generated, validated, reviewed, narrated,
  published; **ChapterNode** holds beats inside a book. Some story-domain docs call a story a
  "strand".
- **Canon** — author-only `Node.IsCanon` trust gate ([SS-LAW-9](#SS-§5)).
- **E.L.F.** *(GLMZ)* — Emergent Life Form; a sentient `Species` living in Characters.
- **Automaton** *(GLMZ)* — non-sentient machine repo (Iowan Behemoths, robots, drones) — *not alive*.
- **Voice harvest** — distilling a canon (`Node.IsCanon`) story's winning edits into the codified
  `literary_rules`/`tone_bible`/character voice fields (propose-then-approve).
- **Ghost** *(GLMZ)* — operator class; performs eigenstate conscience transfer (ECT): conscious perception transferred into any machine with coherent circuitry. Never called Exo, Rider or RFO. See [SS-LAW-26](#SS-§5).
- **Ghosting** *(GLMZ)* — the act of ECT, used as a verb; also used casually for minor transfers (stoplights, cameras, door locks). The body at rest during a ghost = **Husk**.
- **ECT** *(GLMZ)* — eigenstate conscience transfer; the formal term for ghosting. Glossed in back matter, not in-voice, per [SS-LAW-20](#SS-§5).
- **Channeler** *(GLMZ)* — operator class; session-injection into networked frames; also direct AI negotiation (undocumented edge). See [SS-LAW-26](#SS-§5).
- **Splicer** *(GLMZ)* — operator class; hardware/software attack surface, credential cracking, physical build. See [SS-LAW-26](#SS-§5).
- **Husk** *(GLMZ)* — a Ghost operator's body at rest during an eigenstate conscience transfer.
- **ArcSec** *(GLMZ)* — street and industry shorthand for Arcturus Defense Solutions (formally: Arcturus Military Command); the *physical* security monopoly in the GLMZ corridor. See [SS-LAW-27](#SS-§5).
- **NCID** *(GLMZ)* — Neuretic Crime Investigation Division; Halcyon Civil Security's investigative field arm for neuretics-crime: illegal ghosting / ECT, unauthorized neural surveillance, black ice deployment, neuretics assault. Organized into geographic field divisions (e.g. GLMZ Field Division). Street name: **RaNCID**. See [SS-LAW-27](#SS-§5).
- **Silence** *(GLMZ)* — Kyle's katana; *just a sword* ([SS-LAW-10](#SS-§5)).
- **Cacophony** *(GLMZ)* — Kyle's six-shot revolver shotgun ([SS-LAW-11](#SS-§5)).
- **Bushido Coda** *(GLMZ)* — GLMZ's flagship narrative work (Book One: BCODA).
- **The Pulse** *(GLMZ)* — Mach-6 magnetic vacuum transit network. **The Network** *(GLMZ)* — the
  ambient proprioceptive information field BCI-augmented people sense (see `network_doc.md`).

## 10. New story / book workflow {#SS-§10}

> **Invariant: entities and decisions before prose.** Every named entity a book uses exists in the
> database before or as the prose that names it is written, and every decision is recorded as a
> ruling or an entity field and read back. The full protocol is `docs/agent/PROSE_PROTOCOL.md`;
> the book's live state is `prose --factory status --node <book>`.

## Step 0 — Start from the factory

Start every session from the factory: `prose --factory next` (MCP `factory_next`) and
`prose --order list`. A new book enters the line as an **author** work order
(`work_order_add kind:author`); repo changes need an open **engine** work order whose paths cover
them, and the commit names it (`WO:<id>`).

## Step 1 — Canon first

- **New world facts** (CorpoNation, species, world event, a character's narrative law): write them
  into the entity records (`create_*` / `set_entity_fields` / `prose --set-character-fields`) and,
  for world-level canon, into the canon documents with `set_canon_section` — never by hand-editing
  a generated `.md`.
- **Book-level decisions:** `record_ruling` / `prose --ruling add --node <book>` (kinds `law`,
  `page-law`, `metric`, `incidental`), read back.
- Add or update the book's entry in `docs/USER_STORIES.md` when it tracks engine work.

## Step 2 — Entity seeding (F4)

Every **named entity** that appears in the book must be in the database:
- Characters: `prose --add-character --name "..." --species human [--description "..."]` (or MCP
  `create_character`).
- CorpoNations: `prose --add-corponation --name "..." [--description "..."]`.
- Places, weapons, documents, etc.: the MCP `create_*` tools.

Station **F4 Captured** checks it: `prose --factory capture --node <book>` lists untagged known
names and unknown proper nouns per unit.

## Step 3 — Book structure (F2)

1. Create the book and its chapters (`create_chapter`; `--reparent-node --after-slug` to
   re-slot).
2. Plan each chapter's beats as a **title + description** of what the scene does, including what
   it plants or pays off (station **F2 Planned**).
3. Nothing else is stored about the story — no outline, spine, blueprint or ledger outside the
   beats and the world.

## Step 3b — Rebuilding an existing book

When an existing book is restructured (cut, expand, re-slot, deepen, change the ending), it is
cloned and fenced first (`--archive-book`, then `--clone-book ... --draft`), the author approves
the restructure at gate **G1**, and every approved decision is recorded as a ruling on the clone.
Cut beats move to a `<CODE>2CUTS` book with `--move-beat-to-node`, byte for byte. The whole-book
checklist is CraftGuide §12 (`SS-CRAFT-NOVEL`).

## Step 4 — Write (F3)

Write each chapter in reading order, in session, with the full prior prose and the cast's records
in view (`factory_context(priorUnits: all)` / `prose --factory context --node <book> --unit N
--prior all`); station **F3 Written**. Capture new names and facts into the world as you go and
read each record back.

## Step 5 — Heal, verify, press (F5, F6, F1, F7)

- **Heal:** read the book in order (`read_beats(markRead)` / `prose --read-beats ... --mark-read
  --read-by claude`); file defects as read notes; fix with `splice_beats` (dry run, then apply);
  re-read what changed (**F5 Read**, **F6 Clean**). Entity corrections filed during the pass are
  applied as one batch after it, then each record is verified (**F1 Verified**).
- **Press:** `--export-node` exports only what has been read as it stands (no override on the read
  gate) — **F7 Pressed**. Audio only when the author asks (**A**).
- The author reads and decides whether to press (gate **G2**).

## 11. The Feedback Architecture {#SS-§11}

The engine's most important property is not any single service — it is how the services form closed
feedback loops that make each story better than the last. Every loop is a check-and-balance circuit:
a generator produces output; one or more validators or reviewers evaluate it; their judgment feeds
back into the generator's inputs. If a loop is broken — findings accumulate but are never reviewed,
voice harvest is never run, coverage is never checked — the system degrades toward stochastic prose.
**The loops being wired is necessary but not sufficient; the author must use the surfaces.**

## Loop 1 — Canon Grounding {#SS-§11-L1}

```
Entity seeding (CLI / MCP / UI)
  → EntityEmbeddings (1536-d VECTOR_DISTANCE index)
  → CanonRetrievalService (semantic + graph at generation time)
  → BeatPromptBuilder (injects canon-facts block into every prompt)
  → BeatGeneratorService (prose grounded in real entities)
  → prose --scan-entity-mentions / station F4 (gap report: mentioned but not seeded)
  → Author seeds gap entities → index rebuilt → loop closes
```

**What it prevents.** Prose that references characters, places, or gear that don't exist in the
DB — the most common cause of continuity breakage. If all entities are seeded before prose fires
([SS-LAW-1](#SS-§5), [§10](#SS-§10)), the scanner finds 0 gaps. Any gap found is the signal to seed
the entity; the next generation cycle closes it.

**Health signal.** Station F4 passes: `prose --factory capture --node <book>` reports nothing to capture.

## Loop 2 — Post-Beat Validation {#SS-§11-L2}

```
Beat saved (NodeWorkbenchService.SaveBeatAsync)
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
before a story is submitted for review.

## Loop 3 — Continuity {#SS-§11-L3}

```
Beat saved
  → ContinuityExtractionService: LLM extracts (entity, predicate, object) triples
  → ContinuityService: stores claims
  → BeatStateExtractor: extracts EntityStateEvents (location, life status, ammo)
  → WorldStateAtBeatService: reconstructs world state at any beat on demand
  → CanonContradictionService (at chapter close): raises CANON-CONTRADICTION findings
  → Author approves fix → prose patched → loop closes
```

**What it prevents.** A character who was shot in beat 5 appearing uninjured in beat 7 without
explanation. The continuity ledger is the engine's working memory of what has happened.

**Health signal.** No unadjudicated CANON-CONTRADICTION findings on the book.

## Loop 4 — Canon → Voice Harvest (The Flywheel) {#SS-§11-L4}

```
Author marks a story canon (Node.IsCanon)
  → VoiceHarvestService mines temporal Beat history (FOR SYSTEM_TIME ALL)
  → Winning edits → VoiceChangeLog (status: proposed)
  → Author approves → directive folded into literary_rules / tone_bible / Character fields
  → Next generation uses the improved rules
```

**The flywheel property.** Better `literary_rules` → better prose → more canon stories to harvest →
better rules. Harvest is keyed on the author's canon flag; the 0–100 score panels are off by
default and run only on explicit request ([SS-LAW-17](#SS-§5)).

**What it prevents.** Voice drift. A canon story is proof the engine produced something that
worked; harvest crystallizes WHY it worked so the next story starts from a higher baseline.

**Health signal.** Every canon story has had a harvest run, and every proposed `VoiceChangeLog`
row has been approved or rejected.

## Loop 5 — Semantic Fidelity Guard {#SS-§11-L5}

```
Beat write (ProseWriterRouter)
  → SemanticFidelityService: compares the beat against its declared intent
  → SEMANTIC-DRIFT finding if drift exceeds threshold
  → Author adjudicates: keep (intent legitimately changed), rewrite (drift genuine), adjust intent
```

**What it prevents.** The Goodhart's Law trap — prose that drifts from what the beat was meant to
do while still reading well.

**Health signal.** No unadjudicated SEMANTIC-DRIFT findings on the book.

## Loop 6 — Coverage {#SS-§11-L6}

```
prose --coverage (CoverageService)
  → Per-type reachability matrix across all 28 registered entity types
  → Dead inventory: types with 0% appearance in any generated prose
  → Author acts: enrich entities of dead type, or include in a forthcoming seed
  → Next coverage run shows improvement
```

**What it prevents.** A canon with 28 registered types where prose only ever mentions characters
and places, leaving weapons, pharmaceuticals, automata, and the rest unreachable. The matrix is the
engine's standing self-audit of what it is actually using.

**Health signal.** `prose --coverage` shows >0% for all diegetic types. Any type stuck at 0% for
two or more consecutive stories is a retrieval bug or an empty entity roster — fix before shipping.

## Loop 7 — World-State / Temporal {#SS-§11-L7}

```
Generation pre-flight (WorldStatePrecheckService)
  → WorldStateAtBeatService: reconstruct exact world state at the last committed beat
      (who is where, who is alive, how much ammo remains)
  → WorldClockService: advance in-world clock to the beat's timestamp
  → Pre-check failures → generation prompt adjusted before prose fires (not after)

All beats in a story (and across stories)
  → EntityStateEvents ledger: append-only world-state changes
  → Available to any future generation, validation, or continuity check
```

**What it prevents.** Generating prose that assumes the wrong world state: a character who is
supposed to be dead, a gun emptied two beats ago, a location the character left last chapter. This
is the engine's forward-looking continuity guard (vs. the backward-looking
`ContinuityExtractionService`).

**Health signal.** `WorldStatePrecheckService` returns no violations before any beat generation.

## System invariant: all loops must close {#SS-§11-invariant}

Every loop has an author-facing surface where its findings land:

| Loop | Finding category | Surface |
|---|---|---|
| Canon Grounding | Unseeded entity gaps | station F4 (`prose --factory capture`) |
| Post-Beat Validation | `PROSE-GUARD`, `GEAR-CARRY`, `BEHAVIOR`, `AMMO` | `/findings` inbox |
| Continuity | `CANON-CONTRADICTION` | `/findings` inbox |
| Voice Harvest | proposed `VoiceChangeLog` rows | `prose --harvest-voice` / MCP voice tools |
| Fidelity Guard | `SEMANTIC-DRIFT` | `/findings` inbox |
| Coverage | Per-type 0% flags | `prose --coverage` output |
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
No generation service (`ProseWriterRouter`, `BeatGeneratorService`, etc.) may
query the DB or the embedding index directly. All canon retrieval goes through
`CanonRetrievalService`. This ensures the retrieval layer — which owns universe scoping, embedding
fallback, and semantic ranking — is the only surface the generator touches.
*Audit: grep `ProseDbContext` or `EntityEmbeddings` in generation services — 0 hits.*

**SCL-2: Validators file; they do not fix.**
No validation service (`CanonContradictionService`, `ProsePatternGuard`, `GearCarryEnforcer`,
`BehavioralInvariantEnforcer`, `SemanticFidelityService`) may write
to prose tables. Violations are filed as `Finding` rows. Only `FindingApplyService` (triggered by
an author approval) writes prose changes. Every correction is traceable and author-gated.
*Audit: none of the validator services inject `NodeWorkbenchService` or `BeatRepository`.*

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

**SCL-7: No generation bypasses the one entry point.**
`ProseWriterRouter` is the only live path that writes a beat with an LLM; no service hand-rolls a
generation path around its enrichment chain.

**SCL-8: Reviews never auto-apply their editorial conclusions.**
`NodeReviewService` writes scores and summaries. It does not patch beats, does not update voice
rules, and does not raise rewrites. A review result is observation; action requires the author
(or an explicitly author-approved agent).

---

## 13. The Quality Invariants {#SS-§13}

Measurable properties the system must maintain end-to-end. Their purpose is to make "the system is
working" observable rather than assumed.

**QI-1: Every generated beat has been through all validation loops before it is considered a
draft.**
*Evidence:* `PostBeatValidationService` runs on every `NodeWorkbenchService.SaveBeatAsync`. The
`/findings` inbox must be empty (or all open findings explicitly adjudicated) before a story is
submitted for review.

**QI-3: Every canon story has had a voice harvest run.**
*Evidence:* `prose --harvest-voice` has run for every `Node.IsCanon` story, and its proposed
`VoiceChangeLog` rows are adjudicated.

**QI-4: The coverage report shows 0 dead diegetic types.**
*Evidence:* `prose --coverage` shows >0% for all 28 registered types. A type at 0% for two or more
consecutive stories is either an empty roster (seed entities) or a retrieval bug (fix
`CanonRetrievalService`).

**QI-5: The DI graph resolves with no missing registrations.**
*Evidence:* `DiRegistrationTests` + `InterfaceRegistrationTests` green on every build. This is the
system's structural self-test — every service referenced by another must be registered.

**QI-6: All entity mentions in prose are seeded in the DB.**
*Evidence:* station F4 passes for every unit of the book.

**QI-7: Voice changes are not auto-applied.**
*Evidence:* `VoiceChangeLog` is the single write path to `literary_rules` / `tone_bible`. A direct
write to those `Settings` keys from any service other than the approve handler is a SCL-3 violation.
*Audit: grep `literary_rules` in service code — only appears in `GetLiteraryRulesPrompt()` (read)
and the approve handler (write).*

**QI-8: No cross-universe data appears in any generation prompt.**
*Evidence:* `UniverseSegregationTests` (10 tests: query-filter scoping, insert-stamping, shared-key
visibility, story scoping, epoch, uuid-v7). A GLMZ-only entity appearing in another universe's
beat's canon-facts block is a segregation failure — check `IUniverseContext` wiring.

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

**ADR-2: System-versioned (temporal) tables for Beats and Nodes.**
*Decision:* `Beats`, `Nodes`, `NodeBeats`, `ChapterBeats` use `SYSTEM_VERSIONING ON`.
*Why:* Voice harvest mines `FOR SYSTEM_TIME ALL` to find which edits correlated with score
improvements. Without temporal history the flywheel has no data to mine.
*Tradeoff:* Adding a column to a temporal table requires the `SYSTEM_VERSIONING OFF → ALTER →
_History → ON` dance. Accepted overhead for the harvest capability it enables.
*Revisit when:* SQL Server 2025 vector indexes become compatible with system-versioned tables
(currently incompatible; tracked as a known constraint).

**ADR-3: Deterministic checks, not votes, decide quality.**
*Decision:* QA is the logic & continuity sweep and the factory's computed stations. Legion vote
panels and 0–100 scores sit behind the `VotingGate`, off by default, and run only when the author
explicitly asks in that conversation ([SS-LAW-17](#SS-§5)).
*Why:* A named, fixable finding tells you more than a number, and the panels never caught a real
defect in practice. Votes are also the most expensive thing the engine can run.
*Tradeoff:* No aggregate quality number. The author reads the book; the stations prove it was read
and is clean.
*Revisit when:* the author asks for panels back.

**ADR-4: Propose-then-approve for voice harvest and canon changes.**
*Decision:* No service auto-commits a change to `literary_rules`, `tone_bible`, or any entity field.
*Why:* The LLM's "this is a winning move" verdict is a heuristic, not ground truth. Auto-applying
would let the system modify its own voice without human review — the Goodhart's Law failure mode at
the system level, not just the prose level.
*Revisit when:* the author chooses to delegate approval. Not before.

**ADR-5: One format (typed Node tree over Beats) — no parallel format tables.**
*Decision:* Books, chapters, episodes, and collections are all one typed parent/child Node tree
(SeriesNode / BookNode / ChapterNode, TPH on `Nodes`) on the `ParentNodeId` column. No
separate table per format.
*Why:* Every new format table is a new maintenance surface. The audio pipeline, review pipeline,
voice harvest, and export pipeline would each need to handle every format. The Node tree
abstracts over all of them: one pipeline, one set of loops.
*Tradeoff:* The mental model requires understanding that a book is a BookNode, not a row in a
`Books` table. A legacy `Books` table (with `Chapters`/`ChapterBeats`) still exists beside the Node tree.

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

**ADR-10: An instrument earns its place by applied findings.**
*Decision:* A check, audit or LLM instrument stays only while its findings get applied. The audit
battery (`--audit-book`, `book_health` and its tiers) and the standalone instruments whose findings
were never applied are deleted. What remains is what the publish gate reads
(`--publish-readiness`), the logic sweep, the Story Ledger, tuned-read, reader-qa, `--lint-prose`
with the deterministic `CraftNativeRules`, and the services something live depends on
(`SemanticFidelityService` inside the writer, `CanonContradictionService` at chapter close,
`PlantPayoffService`).
*Why:* across tens of thousands of findings only a handful were ever applied, and a full battery
run cost $27–135 for none. A finding nobody applies is noise that reads as work.
*Revisit when:* a removed instrument shows applied findings on a real book — re-admission is by
evidence only. The follow-on audit of the remaining instruments is [RFC 0014](rfc/0014-instrument-value-audit.md).

## Mr. Able - causal antagonist arc {#SS-ABLE-ARC}

Mr. Able - causal antagonist arc

<entity repo="character" guid="019ee6a2-c508-774b-a5ff-f923f2c26aab">Mr. Able</entity> is <entity repo="corponation" guid="34e17863-d822-4bed-a972-3649099b2444">Praxis</entity>'s Director of Containment and operates the <entity repo="faction" guid="019ee6a2-a172-7f5a-9451-03c4c3fe001b">Continuity Office</entity> as <entity repo="corponation" guid="34e17863-d822-4bed-a972-3649099b2444">Praxis</entity>'s deniable recovery arm. He is a cold-blooded sadist and a frustrated middle manager: for eleven years he has directed containment personnel whose names he does not learn, while still answering to executives who can overrule him. He was present for the event by which <entity repo="character" guid="019d6143-a648-7876-9688-0f6d38d70075">Kyle</entity> left <entity repo="corponation" guid="34e17863-d822-4bed-a972-3649099b2444">Praxis</entity>. Whether <entity repo="character" guid="019d6143-a648-7876-9688-0f6d38d70075">Kyle</entity> escaped, was rescued, or was deployed remains unresolved on the page.

<entity repo="character" guid="019ee6a2-c508-774b-a5ff-f923f2c26aab">Able</entity> and <entity repo="character" guid="019d6143-a648-7876-9688-0f6d38d70075">Kyle</entity> never meet on page; <entity repo="character" guid="019d6143-c1d5-e7f8-a92b-34068c150e4d">Pixel</entity> never meets <entity repo="character" guid="019ee6a2-c508-774b-a5ff-f923f2c26aab">Able</entity>. <entity repo="character" guid="019ee6a2-c508-774b-a5ff-f923f2c26aab">Able</entity>'s role is nevertheless causal. He authorizes the District Acquisition spiral around Leavitt and the <entity repo="corponation" guid="34e17863-d822-4bed-a972-3649099b2444">Praxis</entity> monitoring architecture that places <entity repo="character" guid="019d6143-a648-7876-9688-0f6d38d70075">Kyle</entity>'s contract traffic under surveillance. He turns <entity repo="character" guid="019d6143-c1d5-e7f8-a92b-34068c150e4d">Pixel</entity>'s calibration work and support network into a containment liability. Each <entity repo="character" guid="019ee6a2-c508-774b-a5ff-f923f2c26aab">Able</entity> intervention must produce a persistent consequence in <entity repo="character" guid="019d6143-a648-7876-9688-0f6d38d70075">Kyle</entity> and <entity repo="character" guid="019d6143-c1d5-e7f8-a92b-34068c150e4d">Pixel</entity>'s story: a lost option, compromised workspace or support route, damage to an ally, changed operating practice, or a choice that forces their partnership to become more explicit. <entity repo="character" guid="019ee6a2-c508-774b-a5ff-f923f2c26aab">Able</entity>'s attempt to isolate them must instead help create the partnership he cannot understand.

Six short <entity repo="character" guid="019ee6a2-c508-774b-a5ff-f923f2c26aab">Able</entity> interludes are braided through the book:

1. Failed retrieval (Ch8, "Interlude: Failed Retrieval"): unnamed underlings fail to recover <entity repo="character" guid="019d6143-a648-7876-9688-0f6d38d70075">Kyle</entity>; <entity repo="character" guid="019ee6a2-c508-774b-a5ff-f923f2c26aab">Able</entity> is ordered to take the asset himself and experiences the work as humiliation.
2. The Office (Ch13, "Interlude: The Office"): containment procedure reveals the corpo hierarchy; <entity repo="character" guid="019ee6a2-c508-774b-a5ff-f923f2c26aab">Able</entity> brutalizes subordinates while being diminished by superiors.
3. Eleven years (Ch19, "Interlude: Eleven Years"): <entity repo="character" guid="019ee6a2-c508-774b-a5ff-f923f2c26aab">Able</entity> reviews the unresolved departure from <entity repo="corponation" guid="34e17863-d822-4bed-a972-3649099b2444">Praxis</entity> and realizes <entity repo="character" guid="019d6143-a648-7876-9688-0f6d38d70075">Kyle</entity> is the one file he cannot close.
4. The rival (Ch23, "Interlude: The Rival"): surveillance of <entity repo="character" guid="019d6143-a648-7876-9688-0f6d38d70075">Kyle</entity> and <entity repo="character" guid="019d6143-c1d5-e7f8-a92b-34068c150e4d">Pixel</entity> converts retrieval fixation into transference. <entity repo="character" guid="019ee6a2-c508-774b-a5ff-f923f2c26aab">Able</entity> mistakes <entity repo="character" guid="019d6143-c1d5-e7f8-a92b-34068c150e4d">Pixel</entity>'s chosen agency for property <entity repo="character" guid="019d6143-a648-7876-9688-0f6d38d70075">Kyle</entity> has withheld.
5. Collapse (Ch37, "Interlude: Collapse"): <entity repo="character" guid="019ee6a2-c508-774b-a5ff-f923f2c26aab">Able</entity> escalates coercion against people he regards as disposable, then — surveillance exhausted — burns <entity repo="character" guid="019d6143-a648-7876-9688-0f6d38d70075">Kyle</entity>'s anonymity city-wide in a single unauthorized leak. Harm is unambiguous and consequential, never eroticized as spectacle.
6. Promotion (Ch43, "Interlude: Promotion"): <entity repo="character" guid="019ee6a2-c508-774b-a5ff-f923f2c26aab">Able</entity> murders his named <entity repo="corponation" guid="34e17863-d822-4bed-a972-3649099b2444">Praxis</entity> VP, <entity repo="character" guid="01a087c9-dd6f-7818-b1a8-34452b3e5485">Desmond Calloway</entity>, with the founder's bronze bust, constructs a defensible institutional account, and is rewarded because the corporation values usable outcomes over method.

The earlier murder of <entity repo="character" guid="01a087ca-2a2c-7ce1-98b0-c39460ef9599">Adaeze Kline</entity> remains canon: she was an immediate obstacle, not <entity repo="character" guid="019ee6a2-c508-774b-a5ff-f923f2c26aab">Able</entity>'s true superior. The <entity repo="character" guid="01a087c9-dd6f-7818-b1a8-34452b3e5485">Calloway</entity> murder is the finale of his containment arc, and Calloway's own threatened audit now names the city-wide leak as the bigger exposure, not merely the failed retrieval — raising <entity repo="character" guid="019ee6a2-c508-774b-a5ff-f923f2c26aab">Able</entity>'s motive accordingly. <entity repo="corponation" guid="34e17863-d822-4bed-a972-3649099b2444">Praxis</entity>'s reward validates <entity repo="character" guid="019ee6a2-c508-774b-a5ff-f923f2c26aab">Able</entity>'s belief that useful wrongdoing is sanctioned. He ends elevated, more delusional, and obsessed not merely with recovering <entity repo="character" guid="019d6143-a648-7876-9688-0f6d38d70075">Kyle</entity>, but with taking <entity repo="character" guid="019d6143-c1d5-e7f8-a92b-34068c150e4d">Pixel</entity> as a prize. This is a forward series threat <entity repo="character" guid="019d6143-a648-7876-9688-0f6d38d70075">Kyle</entity> does not yet know exists.

Reveal limits: no <entity repo="character" guid="019ee6a2-c508-774b-a5ff-f923f2c26aab">Able</entity> scene resolves <entity repo="character" guid="019d6143-a648-7876-9688-0f6d38d70075">Kyle</entity>'s exit from <entity repo="corponation" guid="34e17863-d822-4bed-a972-3649099b2444">Praxis</entity> or discloses the <entity repo="character" guid="019ee6ce-ad71-7f25-a057-8e947a52b9eb">Marrow</entity>/64-persona program ahead of its scheduled series reveal. <entity repo="corponation" guid="34e17863-d822-4bed-a972-3649099b2444">Praxis</entity> may be shown through containment metrics, surveillance, recovery procedure, and asset language; its deeper experiment remains withheld.

**The entity's silence on <entity repo="character" guid="019ee6a2-c508-774b-a5ff-f923f2c26aab">Able</entity> (author ruling, 2026-09-09):** the entity (see SS-BCODA-1c — a retrocausal projection of <entity repo="character" guid="019d6143-a648-7876-9688-0f6d38d70075">Kyle</entity> himself) has full higher-dimensional visibility and therefore knows exactly what <entity repo="character" guid="019ee6a2-c508-774b-a5ff-f923f2c26aab">Able</entity> is and what he threatens — the same way it knows everything else about <entity repo="character" guid="019d6143-a648-7876-9688-0f6d38d70075">Kyle</entity>'s life before it happens. It never names <entity repo="character" guid="019ee6a2-c508-774b-a5ff-f923f2c26aab">Able</entity>, never warns <entity repo="character" guid="019d6143-a648-7876-9688-0f6d38d70075">Kyle</entity> about him, and never acts to neutralize him directly. This is not an oversight; it is the same non-interference doctrine that governs every other orchestrated event (SS-BCODA-12a: "The entity never lies. It never explains"). Removing <entity repo="character" guid="019ee6a2-c508-774b-a5ff-f923f2c26aab">Able</entity> would remove a pressure the loop needs <entity repo="character" guid="019d6143-a648-7876-9688-0f6d38d70075">Kyle</entity> to survive on his own; warning him would be an explanation, which the entity does not do. The entity has broken its non-interference exactly once on the page — the Relay Station bike rescue in Ch39, for <entity repo="character" guid="019f441d-04fb-7227-94d9-6b4d889ce52b">Boost</entity>, not for <entity repo="character" guid="019ee6a2-c508-774b-a5ff-f923f2c26aab">Able</entity> — and that exception is not repeated here. No beat, in any of the six interludes or in <entity repo="character" guid="019d6143-a648-7876-9688-0f6d38d70075">Kyle</entity>'s own chapters, may show the entity referencing <entity repo="character" guid="019ee6a2-c508-774b-a5ff-f923f2c26aab">Able</entity>.

**Able as the pusher (author ruling, 2026-09-10):** the novel's structural purpose is two people separated their entire lives by design — <entity repo="character" guid="019ee6a2-c508-774b-a5ff-f923f2c26aab">Able</entity> and <entity repo="character" guid="019d6143-a648-7876-9688-0f6d38d70075">Kyle</entity> never meet — until a single cowardly action delivers <entity repo="character" guid="019d6143-a648-7876-9688-0f6d38d70075">Kyle</entity> to the aperture. <entity repo="character" guid="019ee6a2-c508-774b-a5ff-f923f2c26aab">Able</entity> is that pusher, and he acts twice, neither time in <entity repo="character" guid="019d6143-a648-7876-9688-0f6d38d70075">Kyle</entity>'s presence: (1) the Collapse leak (Ch37) strips <entity repo="character" guid="019d6143-a648-7876-9688-0f6d38d70075">Kyle</entity> of every safe harbor city-wide, forcing the final job to happen now rather than on his own timeline; (2) the extraction squads at 35th and Halsted (Ch42, already on the page — Sable relays <entity repo="character" guid="019ee6a2-c508-774b-a5ff-f923f2c26aab">Able</entity>'s live order, "stop him, hold him, keep him moving toward the house") physically herd <entity repo="character" guid="019d6143-a648-7876-9688-0f6d38d70075">Kyle</entity>'s body onto the threshold in the fight's final minutes. <entity repo="character" guid="019ee6a2-c508-774b-a5ff-f923f2c26aab">Able</entity> intends capture, not transformation — he never learns what the Gingerbread House does, and never learns his own order was the delivery mechanism. That blindness must survive into Promotion (Ch43): his delusion is that the operation failed twice, not that it succeeded at the one thing that mattered. Per the reveal doctrine above, this causal chain is never stated on the page by any character or by narration — it is assembled by the reader from the two actions plus the entity's established silence about him, the same "unstated but legible" standard SS-BCODA-12a sets for the entity's own orchestration.
