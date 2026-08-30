# Prose

**A canon-grounded, voice-disciplined story-generation engine for a cyberpunk century — and every other universe stood up alongside it.**

> **This README is the engineering tour — exhaustive, current, and stored in the database.**
> Full detail on specific subsystems lives in the generated Codex docs it links to throughout:
> [docs/BIBLE.md](docs/BIBLE.md) (engine invariants + the Architectural Decision Register),
> [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md), [docs/ENGINE.md](docs/ENGINE.md),
> [docs/CRAFT_SCIENCES.md](docs/CRAFT_SCIENCES.md) (the craft science behind the prose),
> [docs/LOGIC.md](docs/LOGIC.md) + [docs/READER-QA.md](docs/READER-QA.md) (the QA methodology),
> [docs/USER_STORIES.md](docs/USER_STORIES.md) (the goal table), [docs/MCP_TOOLS.md](docs/MCP_TOOLS.md)
> (the full tool reference), [docs/schema.md](docs/schema.md) (the full DB schema), and
> [docs/rfc/](docs/rfc/) (design notes). This document is the synthesis across all of them —
> see [About This Document](#about-this-document) for how it is stored, kept current, and regenerated.

---

## Table of contents

- [What Prose Is](#what-prose-is)
- [History — Four Epochs](#history--four-epochs)
- [Architecture at a Glance](#architecture-at-a-glance)
- [The Science — What Prose Actually Uses](#the-science--what-prose-actually-uses)
- [Prose Generation Pipeline](#prose-generation-pipeline)
- [World Graph and Interconnectivity](#world-graph-and-interconnectivity)
- [Has this book ever actually been read?](#has-this-book-ever-actually-been-read-booksequentialreads)
- [Corrupted currency/dash/diacritic characters](#corrupted-currencydashdiacritic-characters-textintegrityservice)
- [Quality Verification — Logic Sweeps, Not Votes](#quality-verification--logic-sweeps-not-votes)
- [Book Completeness — Convergence Gate & Self-Healing](#book-completeness--convergence-gate--self-healing)
- [CLI Reference](#cli-reference)
- [MCP Reference](#mcp-reference)
- [Database](#database)
- [Universes](#universes)
- [The Subsystems](#the-subsystems)
- [Cost Tiering](#cost-tiering)
- [Running Locally](#running-locally)
- [Repository Layout](#repository-layout)
- [Database Migrations](#database-migrations)
- [Deployment](#deployment)
- [Tests](#tests)
- [Patent Disclosures](#patent-disclosures)
- [Code Style Rules](#code-style-rules)
- [Status](#status)
- [About This Document](#about-this-document)

---

## What Prose Is

Prose is a **command-line-only** .NET 10 story-generation engine that takes a one-line seed to a
published, canon-consistent manuscript and audiobook — in any registered Universe, not just one.
The flagship Universe is **GLMZ** (Great Lakes Metropolitan Zone, 2226, cyberpunk; home of the
*Bushido Coda*), running alongside six others on the same engine (see [Universes](#universes)).
Canon is a **database**, not a folder of files: SQL Server holds tens of thousands of named
entities — characters, places, factions, CorpoNations, weapons, biotech, nanotech, in-world
documents — bound by a directional relationship graph, vector embeddings, and a temporal
(system-versioned) history on every canon table. On top of that sits a typed content tree (series
→ book → chapter, all one polymorphic `Nodes` table) holding beats: the atomic unit of story
function. Generation runs through one router (`ProseWriterRouter`) that assembles a wide
enrichment context per beat before handing off to the LLM. Quality is enforced by a **Logic
Sweep** (six-dimension continuity audit) and **Reader-Proxy QA** (comprehension probes + craft
checklist + gripe jury) — not by a persona-vote score gate; those were retired (SS-A44,
2026-08-03) once the team judged that a 0–100 number told you less than a named, fixable finding
does. There is no web UI. The entire surface is the `prose` CLI and an MCP server — Claude
(Desktop, Code, or any MCP client) is a first-class author of canon calls, with a human approving
canon and voice changes rather than hand-authoring them line by line.

---

## History — Four Epochs

The repository's own commit history *is* the most reliable design-history document — 1,000+
commits since 2026-03-25, none of it lost, all of it groupable into four epochs. This section is
the condensed narrative; the *reasoning* behind each major structural decision is recorded
permanently in [docs/BIBLE.md §14, the Architectural Decision Register](docs/BIBLE.md#SS-§14) —
read that for the "why," this section is the "when and what."

### Epoch 1 — StreetSamurai worldbuilding scripts (2026-03 – 2026-05)

The project began as **StreetSamurai**: a folder of Python scripts (`duo_writer.py`,
`duo_writer_lit.py`) and hand-written Markdown worldbuilding documents — a "6-facet psychology
system," an "Essence Network," a cyberpunk setting called **Meridian City**. Early commits show a
RAG canon engine built on ChromaDB + NetworkX with substring-based grounding. Nearly everything
from this epoch has since been replaced or retired: the Facet system (100% eradicated per
[docs/BIBLE.md §6](docs/BIBLE.md#SS-§6)), ChromaDB/NetworkX substring grounding (replaced by SQL +
OpenAI embeddings), and Meridian City itself (renamed to **GLMZ**). Nothing from this epoch is
load-bearing today except the seed of the idea: a canon-grounded engine that writes in one voice.

### Epoch 2 — The SQL engine and the Blazor UI (2026-05 – 2026-07)

The real architecture emerged: canon moved into SQL Server as the sole source of truth
([SS-LAW-1](docs/BIBLE.md#SS-§5)), a typed **Node** tree replaced ad-hoc story formats, and a
Blazor Server web application (**StreetSamurai.Writer** + **StreetSamurai.Codex**, later renamed)
gave the author a writing surface, canon encyclopedia, and world-health dashboards in the browser.
A standalone CLI was extracted (`StreetSamurai.Cli`), and a Model Context Protocol server
(`Prose.Mcp`) was added so Claude could call the same canon directly. `StoryDirectorService` (an
autonomous "Surprise Me" planning loop) and an `Operator`/CoWriter chat cluster were built as a
second, higher-autonomy generation path alongside the router.

### Epoch 3 — StreetSamurai → Prose, universes multiply (2026-07 – 2026-08-12)

The whole codebase, namespace, and connection string were renamed **StreetSamurai → Prose**
(`8f3fa03a0`, `71e214e51`). `StoryNode` became `BookNode` (SS-A43) as the Node hierarchy gained
formal chapter/beat typing. What began as a single GLMZ universe grew into a genuinely
multi-universe engine: **SCRY** (fantasy/steampunk) stood up alongside GLMZ, then a citation-grounded
nonfiction line (**GSPL → SOURCE → NONFICTION**, renamed twice as its scope widened beyond the
Gospels), **EPIC → FICTION**, **HORROR**, and **EROTICA** (the sixth, added `f74d26f46`) — with
**GOSPEL** later splitting out as its own registered universe. "Dynamic Prose Context" was renamed
**Dynamic Context Memory (DCM)** and formalized into the five-pass, four-layer retrieval system
documented in `CLAUDE.md` and [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md). The 0–100 score-gate
review panels (≥82%/≥85%) that had driven "is this book done" for months were retired in favor of
the Logic Sweep + Reader-Proxy QA methodology (SS-A44, 2026-08-03) — see
[Quality Verification](#quality-verification--logic-sweeps-not-votes).

### Epoch 4 — Command-line only (2026-08-13 → present)

In a single large commit (`ed22bd4f6`, 2026-08-13), the author made Prose **command-line-only**:
both Blazor UIs (`Prose.Writer`, `Prose.Codex`) and their UI-only support libraries (`Prose.Shared`,
`Prose.UiUx`) were deleted outright, along with the disconnected `StoryDirectorService`
"Surprise Me" pipeline and the `Operator`/CoWriter chat cluster (confirmed zero real callers by
dependency trace). The same commit made universe scoping **fail closed** instead of silently
defaulting to "everything" (closing the exact gap behind a cross-universe content-leak incident),
and extended self-healing repair (`BeatRepairService.SelfHealAsync`) to the expensive Full-tier
audits (SWAIN, DRAMATIC-Q, VERIFY), so a failing check now attempts a real targeted rewrite before
ever filing a Finding. **This is the current architecture**: `Prose.Cli` + `Prose.Mcp` are the
entire user-facing surface. The Azure App Service deploy pipeline this epoch had left stale has
since been retired entirely (see [Deployment](#deployment)) — Prose is local-only. See
[Tests](#tests) for what the deleted UI means for the Cypress suite, which still hasn't been
reconciled.

---

## Architecture at a Glance

```
prose CLI  (dotnet run --project v3/Prose.Cli -- <args>)          Prose.Mcp  (MCP server)
        │  265 dispatched --flag handlers, v3/Prose.Cli/Program.cs      │  322 [McpServerTool] methods,
        │  + v3/Prose.Cli/Cli/ (227 handler files)                      │  ~40 tool families, v3/Prose.Mcp/Tools*.cs
        └──────────────────────────┬───────────────────────────────────┘
                                    ▼
                    Core services  (v3/Prose.Core/)
                    │  311 services directly under Services/, 353 including
                    │  Services/Audit, /CoverImage, /Local, /Operator (KDP-automation
                    │  tool-calling only), /VideoGen — see The Subsystems below
                    │  EF Core → SQL Server (LocalDB — local-only app, no hosted deployment)
                    ▼
                    Database  (SQL Server)
                    │  263 base tables + 168 system-versioned _History tables
                    │  (264 FKs, 263 PKs) — see docs/schema.md
                    │  Vector embeddings · directional Edges graph · EntityStateEvents ledger
                    │  ContinuityClaims fact ledger · LlmCallHistories · Findings inbox
                    │  MarkdownFiles (this doc lives here too)
```

*(Counts verified live 2026-08-15 — see [Status](#status). The MCP tool-family table further
down predates the jump from 273→322 tools; regenerate via `--export-tools` before trusting the
per-family breakdown.)*

There is **no web application** in this diagram — see [Epoch 4](#epoch-4--command-line-only-2026-08-13--present).
Two satellite projects sit beside the CLI/MCP core, each solving a narrow problem the main engine
doesn't:

| Project | Type | Purpose |
|---|---|---|
| `Prose.KdpPublish` | WPF + WebView2 desktop app | Automates the Amazon KDP publishing workflow via `KdpOperatorService`'s tool-calling loop over `IToolCallingLlm` (`v3/Prose.Core/Services/Operator/`) — a provider-neutral agentic contract implemented by `AnthropicToolCallingLlm` (tried first) and `OpenAiToolCallingLlm` (in-process fallback), so the tool loop/prompts never change when the vendor does; tools live in `KdpTools/` (`FindAndOpenBookTool`, `UploadManuscriptTool`, `SetPriceTool`, `SelectCategoriesTool`, `MarkPublishedTool`, …). Unrelated to the deleted prose-writing Operator from Epoch 2 |
| `Prose.LlmCli` (`prose-llm`) | Standalone console app, **no** `Prose.Core` reference | Generic multi-provider LLM CLI escape hatch over `MindAttic.Legion`'s `LegionClient` — works even if `Prose.Core`/the DB/EF migrations are broken, since its only dependencies are `MindAttic.Legion` + `MindAttic.Vault`. Syntax: `prose-llm --provider <id> --prompt <text\|@file\|-> [--system <text>] [--temperature <n>] [--max-tokens <n>] [--model <id>] [--json]`; speaks every provider Legion knows (broader than `LlmRouter`'s set — adds cohere/xai/groq/together/openrouter/fireworks). The last-resort fallback tier in `LlmRouter`'s chain, and a manual terminal tool during an outage |

A handful of small one-off maintenance console apps also live under `v3/` (`PromoteEsperanza`,
`PurgeOldNames`, `RunRepair`, `SyncSableProse`, `WriteSableOrigin`) — narrow, single-purpose data
migration utilities, not part of the standing architecture.

### Data layer principles ([SS-LAW-1](docs/BIBLE.md#SS-§5) through [SS-LAW-15](docs/BIBLE.md#SS-§5))

1. **SQL is the only canon store.** `.md`/`.json` files are documentation or export mirrors —
   never the live read path. `engine_data/*.json` is a seed/export mirror, not canon.
2. **System-versioned (temporal) tables** on `Beats`, `Nodes`, and their history twins — every
   edit is rewindable via `FOR SYSTEM_TIME ALL`, which is what voice-harvest mines to find which
   edits correlated with quality improvements.
3. **Static vs. dynamic split** — identity facts (name, height, ancestry) live on canonical
   entity tables; story-state (location, ammo, life status) lives in the append-only
   `EntityStateEvents` ledger. No denormalized "convenience copies."
4. **One format: everything is a tree of Nodes over Beats** — `SeriesNode → BookNode →
   ChapterNode` on a single `Nodes` table (table-per-hierarchy, SS-A43), riding `ParentNodeId`.
   No parallel format tables per content type.
5. **Every row belongs to exactly one Universe** ([SS-LAW-15](docs/BIBLE.md#SS-§5)) — enforced
   structurally via an EF Core global query filter keyed on `IUniverseContext`, not by convention
   (a per-query `WHERE UniverseId = @u` clause that any developer could forget). An entity needed
   in two universes gets two rows — never a shared row, never an M:M bridge.
6. **Propose-then-approve** — no service auto-commits a canon or voice-rule change. Voice harvest,
   continuity claims, and generated docs all land as proposals the author reviews.

---

## The Science — What Prose Actually Uses

Prose's craft rules are not invented in a vacuum — they cite specific, checkable sources. The full
annotated bibliography (who, what, why, and where it's wired into the engine) lives in
[docs/CRAFT_SCIENCES.md](docs/CRAFT_SCIENCES.md); this is the condensed map:

| Source | What Prose takes from it | Where it's wired in |
|---|---|---|
| **Dwight Swain** — *Techniques of the Selling Writer* | Scene/Sequel structure (Goal→Conflict→Disaster / Reaction→Dilemma→Decision) as a beat-classification doctrine | `SwainAuditService`, `swain_audit`/`swain_repair` MCP + CLI (SS-A47) |
| **Will Storr** — *The Science of Storytelling* | Sacred-flaw theory of control, dramatic-question tracking, the antihero-empathy mechanism | `NarrativeScienceService` — `analyze_sacred_flaw`, `check_dramatic_question`, `check_antihero_empathy` |
| **Rudolf Flesch** — Flesch Reading Ease / Flesch-Kincaid Grade Level | Deterministic readability scoring, no LLM cost | `WritingQualityService` and related deterministic checks |
| **McKee / Truby / Vogler** | Controlling idea, dramatic premise discipline | Structural blueprint's thematic/controlling-idea slice |
| **Chekhov's Gun** | Setup-and-payoff as a trackable, auditable ledger (not a metaphor) | `PlantPayoffService`, `chekhov_audit`, plant/payoff orphan detection |
| **StoryScope** (empirical AI-fiction detection research) | Nine structural anti-tell countermeasures committed *before* prose is written — escalation curve, event-type variety, resolution mode, moral polarity, subplot carrier | `StructuralBlueprintService`, `storyscope_audit` — see Patent Disclosure SS-DISC-002 |
| **The six-dimension Logic Sweep** (in-house) | Causality, knowledge-states, timeline, plant/payoff, orphan references, bible agreement | [docs/LOGIC.md](docs/LOGIC.md) — the default QA methodology, SS-A44 |
| **"Signs of AI writing"** (Wikipedia's crowd-sourced tell catalogue) | The banned-mannerism list — pseudo-profundity, rule-of-three overuse, hedge-stacking | [docs/CRAFT.md §8](docs/CRAFT.md) |
| **Stephen King** — *On Writing* | Craft anti-patterns: adverb discipline, "kill your darlings," voice over exposition | Woven through [docs/CRAFT.md](docs/CRAFT.md) |

Two companion doctrine documents sit beside the science: [docs/CRAFT.md](docs/CRAFT.md) (10
sections of universal DON'Ts — camera/scene architecture, sentence/voice discipline, dialogue,
interiority, banned mannerisms, POV clarity) and [docs/DELIGHT.md](docs/DELIGHT.md) (14 DOs —
the positive doctrine, reverse-engineered from the top-decile beats and reader praise ballots:
open on a sensory fact that's already a clue, pay the cost on-page and keep it as a number, let
mundane heroism win, vary the moves rather than stamping the same one). CRAFT.md is the floor;
DELIGHT.md is what separates competent from unforgettable. Both are hand-edited directly and
DB-backed via `CanonDocumentSections` (`set_canon_section` / `generate_canon_md`).

---

## Prose Generation Pipeline

`ProseWriterRouter` is the **sole entry point** for beat prose generation (SS-A16). Call it — never
`BeatGeneratorService` directly.

```
ProseWriterRouter.WriteAsync(context, beatId, beatIndex, totalBeats)
```

The router assembles a wide enrichment context before handing off to generation:

| Service | What it injects | Activation |
|---|---|---|
| `BeatModeDetector` | Classifies the beat as Combat / Narrative / EmotionalClimax / Dialogue / Transition / Revelation | Keyword scan on `BeatGoal` |
| `PacingService` | BREATHE / FLOW / TIGHTEN / STRIKE / SETTLE prose rhythm | Position + keywords; Combat forces STRIKE |
| `StoryMethodologyService` | Save the Cat structural role (Opening Image → Final Image) + Scene/Sequel type | Position in book |
| `DelightProseGuidance` | The 2–3 reader-loved moves matching this beat's mode ([docs/DELIGHT.md](docs/DELIGHT.md)) | All beat modes |
| `CombatProseGuidance` | Verbs-first, fragment sentences, no emotion-naming, dissociated-observer examples | `BeatMode.Combat` |
| `SceneContextBuilder` | Ambient sensory grounding | Always |
| `DialogueService` | Per-character voice/subtext profile injection | Dialogue / EmotionalClimax modes |
| `SceneContextAssembler` (+ `WoundLedgerService`) | Per-entity X-Ray: voice/psychology/wound/behavior profile of everyone on-page | Always |
| `ContinuityService` | Canonical/confirmed fact constraints for on-page characters | Always |
| `TensionEscalationService` | Warns when beats have stagnated at low intensity | `beatIndex > 2` |
| `ReaderKnowledgeService` | Dramatic-irony bookkeeping — what the reader currently knows | Always |
| `ConsequenceService` / `ConsequenceEngine` | Gear/cyberware/status constraints + cross-book persistent consequences | Always |
| `AmbientAnomalyService` | Location-tagged background New Weird detail | ~60% gate |
| `WorldStateAtBeatService` | Temporal entity-state snapshot (drift from canon baseline) | Always |
| `NarrativeSummaryService` | Rolling compressed memory of prior beats | Always |
| `ChapterSummaryService` | DB-backed prior-chapter memory | Always |
| `OpenThreadsService` | Unresolved promises/plants/questions | Always |
| `BookStateLedgerService` | Arc-level named state (crises, dramatic questions, alliances) | Long books |
| `StoryScienceService` | King + Storr craft laws: sacred-flaw consistency, status dynamics, curiosity gap, causal chains, sensory specificity | Always |
| `StructuralBlueprintService` | Per-beat StoryScope anti-tell slice — subplot carrier, anachrony cut, escalation floor, event type, ending/resolution mode | Node has a blueprint (`--generate-blueprint`) |
| `NarrativeChartService` | Offscreen/parallel character activity (world continuity) | Always |
| `WorldGraphService` | Entity pre-check — flags invented proper nouns not in canon | Always |
| `PlantPayoffService` | Active plant/payoff pairs for the book | `NodeId != Guid.Empty` |
| `BookAuditService` | Gateway or Sequel commandments (7 each, auto-detected from `PreviousNodeId`) | `NodeId != Guid.Empty` |
| `LibertyReportService` / `SemanticFidelityService` / `CanonGroundingService` | Rule-of-Cool check, Goodhart intent-drift check, canon-grounding scaffold | Always (findings loop back into later beats) |

`EmotionalDepthService` (the 8-dimension Want/Need/Wound rubric) is **not** called directly by the
router — it runs via `--examine-emotion` / the DEEP tier of `book_health`, and its findings become
live guidance one beat later through the generic findings-loop-back mechanism.

#### Entry points

| Path | When to use |
|---|---|
| `ProseWriterRouter.WriteAsync(context, beatId, beatIndex, totalBeats)` | All beat writing — the only sanctioned path |
| `CombatSceneWriter.WriteCombatSceneAsync(request)` | Explicit multi-exchange combat setpiece (loadout/ammo/bio-battery tracking) |
| `BeatGeneratorService.GenerateBeatAsync(context)` | Legacy direct path — no coverage logging, not used for new code |

Two paths from Epoch 2 **no longer exist** and are not fallbacks for anything: `StoryDirectorService`
(the "Surprise Me" autonomous pipeline) and the `Write.razor` legacy UI path — both deleted with
the Blazor UIs in Epoch 4.

#### Beat writing workflow

1. Assemble `BeatContext` (X-Ray context via `SceneContextAssembler`, `NodeId` always set).
2. Call `ProseWriterRouter.WriteAsync(context, beatId, beatIndex, totalBeats)`.
3. `prose --examine-emotion --slug <slug>` — 8-dimension emotional scoring.
4. `prose --commandment-audit --slug <slug>` — gateway/sequel commandments once the book is
   complete (renamed from `--book-audit` 2026-08-30 — it collided with the unrelated
   `--audit-book` full battery).
5. `prose --plant-audit --slug <slug>` — orphaned-plant check.
6. `prose --storyscope-audit --slug <slug>` — verifies the structural anti-tells held (monotonic
   escalation, varied event types, no moral gloss, no epilogue, subplot executed). BLOCKER
   findings are fixed with minimal splices, then re-audited.

#### Coverage monitoring

```powershell
prose --workflow-status --slug <slug>   # per-book service coverage matrix + gaps
prose --workflow-status --all           # global utilization across all books
```

MCP equivalents: `workflow_status`, `workflow_status_global`, `workflow_beat_modes`.

---

## World Graph and Interconnectivity

`WorldGraphService` (`v3/Prose.Core/Services/WorldGraphService.cs`) builds an in-memory
[QuikGraph](https://github.com/KeraLua/QuikGraph) `AdjacencyGraph<string, WorldEdge>` over every
entity in the active universe. Per [SS-LAW-1](docs/BIBLE.md#SS-§5) and
[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) §2a, **it is never the store of record** — a pure
in-memory projection rebuilt from SQL on `Rebuild()`/`EnsureLoaded()`, registered as a DI
singleton that loads synchronously at first resolution and then self-refreshes in the background
on an epoch-based staleness check (`UniverseScope.Epoch`).

**How it's populated.** Bespoke builders (`BuildCharacters`, `BuildFactions`, `BuildWeaponry`, …)
each read one typed table and turn scalar fields into a `WorldNode` (`Id = Slugify(Name)` — a
string, not the entity's real `Guid`) and relationship-shaped fields (`Affiliation`, `Territory`,
`KnownUsers`, …) into `WorldEdge`s, reading each entity type's own relationship bridge tables
(`CharacterRelationships`, `WeaponKnownUsers`, `CyberwareItemKnownUsers`, …) fresh every rebuild.
A catch-all `BuildRemainingEntities()` nodes everything the bespoke builders miss (cyberware,
ammunition, materials, ~21 other types) so nothing is graph-invisible. **It does not read from
the JSON graph snapshots** (`engine/data/graph/*.json`) — those are an output cache only,
written by `--rebuild-graph`, never an input.

There is a second, separate SQL-backed relationship representation: the generic `Edge` entity
(`v3/Prose.Core/Data/Entities/Edge.cs`) — polymorphic `SourceId`/`TargetId` into the universal
`Entities` table, bi-temporal validity, a free-string `RelationType`. Its own doc comment states
the intent (and [RFC 0007](docs/rfc/0007-fully-relational-canon.md) §1b names the same goal): it
should eventually **replace** the per-type bridge tables as the graph's actual read path, with a
standing "edge-completeness audit" flagging prose-implied relationships that never got projected
into a real edge. As of today, `WorldGraphService` does not read `Edge` rows yet — the per-type
tables are still the live source. Fixing a `WeaponKnownUsers`/`CharacterRelationships` row (as
several fixes this session did) directly improves the next `--rebuild-graph`'s output; writing to
`Edge` today does not, until that migration lands.

### Diagnosing real gaps vs. flavor texture

`prose --graph-health --universe <slug>` (`GraphHealthCli.cs`) runs `GraphHealthService.Analyze()`
— pure string heuristics, zero LLM cost — and reports orphaned nodes (0 edges), weakly-connected
nodes (exactly 1 edge), and suspicious names (sentence fragments mis-parsed as if they were named
entities, e.g. "The Provost of the University of the Great Lakes Meridian" showing up as a
character node).

A raw run against GLMZ found 13,033 total nodes, 789 orphaned, 8,660 weakly connected (66% of the
entire graph), 487 suspicious names — numbers that sound alarming until you account for **most
orphaned/weakly-connected entities being intentional flavor texture**: hundreds of guns, drugs,
apparel items, and NPCs seeded so the world has depth and a deep roster to draw from if a future
scene needs one, but that never appear on the page. Force-connecting or deleting those would be
solving a problem that doesn't exist. The real gap is the much smaller subset that **does** appear
in shipped prose (per the `BeatEntityPresence` table) but isn't properly linked — that's where
continuity/richness value is actually being left on the table.

`--used-in-prose-only` filters the report to just that actionable subset (best-effort join:
`WorldNode.Id` slug → `Entities.Slug`/`(EntityType, Name)` → `Entities.Id` → any
`BeatEntityPresence` row). On the same GLMZ run: of 789 orphans, **103** are actually referenced
in prose; of 8,660 weakly-connected, **103** are. The real interconnectivity gap is ~206 entities,
not "thousands" — including named, prose-significant BCODA characters (Femi, Casper Vey) that
turned out to have zero or one edge despite heavy on-page presence, confirming the filter finds
real signal, not just noise reduction.

This is the same principle Dynamic Context Memory (DCM) applies to per-beat prompt assembly,
applied to the graph: only what's actually in play needs full connectivity now; everything else
stays in reserve, available the moment it's actually used, not before.

**Open direction (not yet built):** a live, windowed "Dynamic Entity Memory" companion to DCM —
load an entity (and what it implies, e.g. a carried weapon via an `owned_by`/`carries` edge riding
its owner's presence rather than needing its own per-beat tracking) into a small working-set graph
when it's used, evict it after N beats of non-use exactly like DCM's `EvictAfterActions` rule, and
optionally bind a working-set snapshot to a specific beat for a "what was live at this moment in
the story" visualization. Would keep the *live* graph small and fast rather than walking all
13,033 nodes on every query. Proposed 2026-08-15; not scoped or built yet.

### Dynamic Edge State — any relationship that's only true for part of the story

This is not a possession-tracking feature. `Edges.RelationType` is a free string — the live
graph already carries `owns`, `carries`, `knows`, `protected`, `manufactures`, `located_at`, and
more (see `WorldGraphService`'s bespoke builders). **Any edge between any two entities can have a
story-time validity window** — an alliance forming and later breaking, trust earned and then
burned, a faction membership held and then renounced, a location being accessible and later
sealed, a secret known and then exposed to someone else, a debt owed and then paid. Possession
("Kyle owns his motorcycle until it's destroyed, doesn't own it until a rebuilt version is
revealed later, then does again") is just the first worked example, chosen because it was
concrete and verifiable — not the boundary of what the mechanism covers. This isn't hypothetical
design work: the machinery already existed in the engine and, as of 2026-08-15, is wired up,
bug-fixed, and verified against that one real example.

**Three separate temporal-relationship systems exist in this codebase.** Don't rebuild a fourth
without checking which of these already covers the need:

1. **`Edges`** (SQL, `v3/Prose.Core/Data/Entities/Edge.cs`) — `SourceId`/`TargetId`/`RelationType`
   with real `DateTime? StoryValidFrom`/`StoryValidUntil`. System-versioned temporal table. This
   is the one that's live and wired in (below).
2. **`EntityStateEvents`** (SQL) — an append-only ledger for quantity/status facts (`AspectKey`/
   `Verb`/`OldValue`/`NewValue`, keyed to a real `BeatGuid`), e.g. ammo counts, a weapon's
   condition, a character's designation. A different mechanism from `Edges` for a different kind
   of fact — don't model "owns" as a state event or a possession count as an edge.
3. **`WorldEdge`/`EvolveRelationship`** (in-memory, `WorldGraphService.cs`) — string-keyed
   story-points (`"chapter:3"`, not `DateTime`) via `GetEdgesAt`/`IsEdgeValidAt`. Fully-working
   query methods, but the only writer is an MCP-only LLM-extraction tool, it's never read by
   `ProseWriterRouter`, and **it doesn't survive `Rebuild()`** — every rebuild wipes it back to
   timeless edges from the static per-type tables. Effectively orphaned. Left untouched by this
   pass; documented here so it isn't mistaken for dead code and deleted, or rediscovered as new.

**`WorldStateAtBeatService.SnapshotAsync(beatId, storyTime, entityIds)`** is the live one —
confirmed called from `ProseWriterRouter.cs` and `BeatGeneratorService.cs`, not diagnostic-only.
It queries both `Edges` and `EntityStateEvents` for "what's true at this point in the story" using
exactly this filter shape:

```csharp
InvalidatedAt == null
&& (StoryValidFrom == null || StoryValidFrom <= t)
&& (StoryValidUntil == null || StoryValidUntil > t)
```

**The gap found before today:** of 2,875 `Edges` rows, 912 had `StoryValidFrom` set and **zero**
had `StoryValidUntil` — nothing had ever recorded a relationship *ending*. Of 9,366
`EntityStateEvents` rows, only 67 (0.7%) had a real `BeatGuid` — most were bulk-seeded with
`AtStoryTime` set to the dev-seeding timestamp, not an in-fiction story position. A system
genuinely wired into every beat of generation was silently contributing almost nothing.

**Two real bugs found and fixed while verifying the first worked example** (Kyle's motorcycle —
see `docs/nodes/BCODA.md`; two `Edges` rows, `RelationType = "owns"`, one period ending at the
Relay Station's destruction, one starting at the birthday reveal of the rebuilt "Mk. 2," open
-ended per the book's own deliberate ambivalence about its final loss in Ch40):

- `ProseWriterRouter.cs` called `SnapshotAsync(beatId, ct: ct)` with no `entityIds` — despite the
  method's own docstring warning the unscoped path is "expensive on large DBs — scope in
  production." **Fixed**: it now resolves `context.CharactersInScene` (names) to `Entities.Id`
  and passes them, so live generation gets a small, relevant snapshot instead of a full-universe
  one, consistent with DCM's narrow-scope-by-construction principle.
- `WorldStateAtBeatService.SnapshotAsync`'s `Edges` query capped at `.Take(500)` with **no
  `ORDER BY`** before it. SQL Server's unordered `TOP` returns whatever the scan reaches first —
  for a table with more matching rows than the cap, newly-inserted rows (highest `Id`) are
  essentially always excluded. This is exactly why the motorcycle's two new edges were invisible
  to every query until fixed, despite being written correctly. **Fixed**: added
  `OrderByDescending(e => e.StoryValidFrom != null).ThenByDescending(e => e.StoryValidFrom)`
  before the `Take(500)`, so real temporal facts sort ahead of the always-on "timeless" majority
  rather than depending on undefined scan order.

Verified end-to-end with `prose --world-state --beat <id> --story-time <date> --universe glmz`
at three points: before the destruction beat (owns the original bike), between destruction and
the reveal (owns neither — the edge is correctly absent), after the reveal (condition=rebuilt,
designation="MK. 2," ownership edge reactivated on the same entity). This is the reference
example for populating `Edges`/`EntityStateEvents` correctly going forward — **no corpus-wide
backfill was attempted**; that's a separate, much larger content decision once more of the corpus
needs it.

### Has this book ever actually been read? (`BookSequentialReads`)

A structural fix and a content read are different operations, and it's easy to do the first and
assume the second happened. BCODA had 15 chapters (Ch23-37, 155 beats, ~30% of the book) nested
under a mislabeled "Chapter 22 — Ghost Period" wrapper node until 2026-08-14, when the schema was
corrected (`Nodes.ParentNodeId` reparented). But **nobody had ever read what was inside those
chapters** — every prior sweep queried only one `ParentNodeId` level deep and silently missed
them. The first genuine read (2026-08-15) found a real spoiler-duplicate beat that had sat there,
live, since before the fix (see the Dynamic Edge State section above for that story). A
corpus-wide check the same day found **zero of the 22 books in this corpus had ever had a
verified sequential read** — this wasn't a BCODA-specific gap.

`SequentialReadTrackingService` (`v3/Prose.Core/Services/SequentialReadTrackingService.cs`) +
the `BookSequentialReads` table now make "has this book actually been read front-to-back" a
tracked, queryable fact instead of an assumption. The freshness check is self-invalidating by
construction: `ComputeBeatSequenceHashAsync` walks the book's full chapter/beat sequence **fresh
every time** via a recursive descendant walk (never a flat `ParentNodeId=book` query — see
CLAUDE.md's HARD RULE on this, which exists for exactly this reason), and hashes it. Any future
reparenting, beat insertion, disable, or reorder changes the hash automatically, so a book flips
from `Current` to `Stale` on its own — no invalidation trigger, no one has to remember to mark
anything.

```
prose --sequential-read-status --slug <slug> | --all [--json]
prose --sequential-read-record --slug <slug> --read-by <name> [--stages N] [--summary "text"]
```

BCODA is, as of 2026-08-15, the first of the 22 books with a recorded `Current` status — a
genuine 7-stage sequential read (each stage carrying forward accumulated story-state for the next
to check seams against, not independent parallel chunks) that found and fixed two real BLOCKERs
(a Mrs. Chen relationship-timeline contradiction; a weapon-draw claim contradicting an earlier
firefight) plus several bible-citation corrections, and confirmed the book reads coherently start
to finish. By the end of that day's corpus sweep, 5 of 37 books across all universes were
`Current` (BCODA, Ballast, It Came From Iowa, Read the Room, Sparrow) — the other 32, including
VIGL, which shares the identical wrapper-chapter bug, had no such record yet.

### Corrupted currency/dash/diacritic characters (`TextIntegrityService`)

Found 2026-08-15 while sequential-reading Ballast: 8 instances of Φ (the QUANTA currency symbol)
silently corrupted to U+FFFD (the Unicode replacement character) in `Nodes.NodeBible`. A
corpus-wide follow-up scan found 18 more across five other books' bibles (Pixel, Sparrow, The Way
Up, The Way Down, Vultures at the Door) — and not just Φ: em-dashes and accented Latin letters
(Alarcón, Mamá También, crèche) had been silently mangled to the same U+FFFD the same way. All 26
were fixed.

**Extended the same day, second corruption class found:** while sequential-reading Between the
Lines, a DIFFERENT garbage codepoint turned up — stray low-range ASCII control characters
(codepoints 1–31, excluding tab/LF/CR) sitting where an em-dash or section symbol (§) had been
lost, invisible in most terminals so they render as blank/zero-width and easy to mistake for a
missing space rather than a missing character. `TextIntegrityService`'s detector was narrowed to
U+FFFD only, so it missed this class entirely. Broadened the scanner to flag both signatures in
one pass, then re-ran corpus-wide: **97 more instances** across 11 more books (Pixel, Read the
Room, Ballast, Bushido Coda, The Way Up, Sparrow, The Way Down, It Came From Iowa, Critical Mass,
Steppin' Razor, Vultures at the Door) — all corrupted em-dashes/section-symbols, all in
`Nodes.NodeBible`, all codepoint 26 (ASCII SUB) except Between the Lines' codepoint-21 (NAK)
section-symbol instances. 95 auto-fixed via `--fix`, 2 needed manual review (an em-dash next to a
newline instead of a space, missed by the auto-heuristic's strict both-sides-space check). A
corpus-wide re-scan confirms **0 remaining** as of this writing, across BOTH signatures.

**Root cause of why this went undetected for so long:** SQL Server's `REPLACE`/`CHARINDEX` gave
**false negatives** for U+FFFD under this DB's collation — `CHARINDEX(NCHAR(65533), text)`
returned 0 even when a direct `UNICODE(SUBSTRING(text, pos, 1))` at that exact position confirmed
65533. Any ad-hoc SQL check written the "obvious" way would silently miss real corruption.
`TextIntegrityService` (`v3/Prose.Core/Services/TextIntegrityService.cs`) never uses those SQL
functions for detection — it pulls text into memory via EF Core and does a plain C# char
comparison against BOTH known corruption signatures (U+FFFD, and any control char below 32 other
than tab/LF/CR), which has no collation involved and cannot have the same bug. It scans with
`IgnoreQueryFilters()` so one call sees every `Beats.Text` and every book's `Nodes.NodeBible`
across all universes, never scoped to whatever `--universe` the process happened to start with.
**If a third garbage-codepoint signature ever turns up, extend the detector again — don't hand-fix
the one instance and move on; that's exactly how the control-char class sat undetected this long.**

```
prose --check-text-integrity [--fix] [--json]
```

`--fix` auto-repairs three unambiguous patterns: U+FFFD immediately followed by a digit → Φ (this
project's Φ-precedes-the-number currency convention); a stray control char between two spaces →
em-dash; a stray control char immediately before a digit → section symbol (§N). Everything else —
a foreign proper noun, a definitional "`Φ` = QUANTA" line, an em-dash sitting next to a newline
instead of a space — needs the corrected character inferred from context and fixed by hand; the
tool reports these for review rather than guessing.

**Why manual SQL bypasses of `ApplyFixAsync` are now flatly forbidden, not just error-prone —
two real incidents recorded so the reasoning is never relitigated:**

**HARD, ABSOLUTE (2026-08-22): nothing reaches the database except through Prose.Hub — reads AND
writes, no exceptions.** Both incidents below happened specifically because a manual SQL script
bypassed `TextIntegrityFinding.ApplyFixAsync` (the actual service, which already handles both
problems correctly) — always use the service; never hand-write the fix.

1. **`TextIntegrityFinding.Position` is 0-indexed (C# convention); SQL `STUFF`/`SUBSTRING` are
   1-indexed.** `ApplyFixAsync` correctly adds 1 internally. A manual script that hand-copied a
   position from the CLI's printed report and forgot that +1 silently repaired the character
   **before** the real corruption, leaving the actual bad character untouched and clobbering
   whatever legitimate character used to sit one position earlier — this happened for every manual
   fix attempted before it was caught.
2. **A literal `--` (or other multi-character ASCII sequence) typed into a `sqlcmd -Q "..."`
   string argument passed through bash is not reliable.** It arrived in the database as a single
   ASCII SUB control character (0x1A / codepoint 26), not two hyphens — some layer of the bash →
   sqlcmd argument-passing chain collapsed or reinterpreted it. This exact codepoint was later
   found sitting in place of em-dashes across 11 books' bibles (97 instances corpus-wide) — this
   manual-SQL mechanism is what produced most of that corruption, not a one-off mistake.

### The "unreachable service" anti-pattern

On 2026-08-09 a sweep found five real, working, fully-tested services with **zero CLI or MCP
invocation path** — built, never wired to anything a human or agent could actually call, quietly
rotting: `DataConsistencyService` (previously reachable only from the Blazor `/integrity` admin
page — itself removed in Epoch 4), `GraphHealthService` (this section), six `DataScanUtility`
subclasses (`FixPhiService`, `FixIdentityCorruptionService`, `TagWeaponLethalityService`,
`TagNormalizerService`, `AssignTiersService`, `CrossReferenceService` — these additionally had no
dry-run capability at all before the fix), and `NodeWorkbenchService.MoveBeatAsync` (fractional
-SortKey beat reordering, previously Blazor-drag-and-drop-only). Each got a thin CLI wrapper
(`DataConsistencyCli.cs`, `GraphHealthCli.cs`, `DataScanCli.cs`, `MoveBeatCli.cs`) and nothing
else changed — the services were already correct. **If you build a new service, wire an
invocation path (CLI flag or MCP tool) before moving on**, or it's the natural sixth example.

---

## Quality Verification — Logic Sweeps, Not Votes

**The 0–100 score gates (≥82% standalone / ≥85% cumulative) are retired** (author ruling,
2026-08-03: *"remove scores; they mean nothing"* — SS-A44). Nothing writes new `Node.Score` values
except an explicitly requested legacy panel run. Publish-readiness is **not** a single "clean at
BLOCKER" snapshot — a fixed sweep-round count was never a real stopping criterion (five rounds
clean, then a sixth independent round finding something new was the observed failure this
replaced). It's a five-point convergence gate, [docs/LOGIC.md §9](docs/LOGIC.md#SS-LOGIC-9) — see
[Book Completeness](#book-completeness--convergence-gate--self-healing) for the mechanics behind
each point.

### The Logic Sweep ([docs/LOGIC.md](docs/LOGIC.md), canonical runbook `/logic-sweep`)

Agents read a book end-to-end (enabled beats only) and audit six dimensions:

1. **Causality chain** — every event has an established cause; every decision, a motivation.
2. **Knowledge states** — who knows what, and when they learned it.
3. **Timeline** — the book's internal clock, reconstructed and checked for impossibilities.
4. **Plant/payoff ledger** — two-way: every plant pays off, every payoff was planted.
5. **Orphan references** — nothing references removed/disabled/merged content.
6. **Bible agreement** — prose and the book's `NodeBible` tell the same story; whichever is wrong
   gets fixed in the same change (prose wins on facts, the bible wins on locks).

Findings triage **BLOCKER / MODERATE / MINOR** and are fixed with minimal splices — "fix what a
finding names; if you can't name the failure, leave the beat alone."

### Reader-Proxy QA ([docs/READER-QA.md](docs/READER-QA.md), `/reader-qa`)

Four findings-only instruments, hash-gated so unchanged content re-runs free:

1. **Comprehension probes** — Haiku answers reader-level questions, diffed against a Sonnet
   synopsis, Sonnet-arbitrated → `ComprehensionDefect` findings.
2. **Craft/delight checklist** (`--craft-checklist`) — hash-gated binary checklist against
   CRAFT.md/DELIGHT.md doctrine.
3. **Cross-family pairwise duels** (`--duel`) — every splice compared across LLM families,
   SS-A44-gated.
4. **Gripe jury** (`--reader-qa --gripe-pass`) — a findings-only jury pass, no scores.

None of these are votes. The old dual-review machinery (`--review-node` panels, Legion votes,
`RunSampledReviewAsync`) is **quarantined behind the SS-A44 gate** — it runs only on explicit
user request (`--allow-votes` / `allowVotes:true`); the engine's default is OFF.

---

## Book Completeness — Convergence Gate & Self-Healing

Two pieces of infrastructure added 2026-08-14/15 that answer "is this book actually done" and
"can obviously-safe fixes apply themselves" without a human re-reading every finding by hand. A
third, related piece — has a book ever genuinely been read front-to-back, as opposed to swept in
scoped chunks — lives in [its own section above](#has-this-book-ever-actually-been-read-booksequentialreads)
under World Graph; the fact-ledger/blast-radius/loop-until-dry machinery below is what makes a
*sweep* trustworthy, and the sequential-read tracker is what confirms a *human or agent* actually
looked at the result.

### The five-point convergence gate ([docs/LOGIC.md §9](docs/LOGIC.md#SS-LOGIC-9))

A book is publish-ready only when **all five** hold — not a fixed round count, not a single clean
snapshot:

1. **Zero open BLOCKER/MODERATE logic-sweep findings.**
2. **Zero open `CONTRADICTED` fact-ledger claims.** The "fact ledger" is `ContinuityService` /
   the `ContinuityClaims` table (`v3/Prose.Core/Services/ContinuityService.cs`) — `Upsert` checks
   an incoming `(EntityId, Predicate, Object)` claim against any other live claim on the same
   `(EntityId, Predicate)`; a mismatch files the *incoming* claim as `CONTRADICTED` (a `CANONICAL`
   existing claim is never demoted). **Numeric-safe** (fixed 2026-08-14 after a real VIGL false
   positive — a career length drifted "fifty"→"sixty" across sweep rounds and got flagged as a
   contradiction it wasn't): predicates on an explicit allowlist (`age`, `tenure_years`,
   `career_length_years`, `zone_age_years`, `duration_years`, `years`) are parsed as integers
   (digits, number-words, compound words like "fifty-nine", unit suffixes stripped) and compared
   arithmetically instead of by string equality; claim UIDs normalize the same way so "fifty" and
   "50" collapse into one claim rather than two that then falsely contradict each other.
   Non-numeric predicates still use plain string equality. `prose --continuity extract --slug
   <slug>` populates the ledger from prose (multi-provider `(entity, predicate, object)`
   extraction via `ContinuityExtractionService`); `prose --continuity <list|resolve|apply>`
   manages open contradictions.
3. **Two consecutive independent sweep rounds find zero new findings** — checked mechanically via
   `prose --logic-sweep --slug <slug> --until-dry [--required-dry N] [--max-rounds N]`
   (`LogicSweepCli.cs`; defaults 2 required dry rounds, 8-round safety cap), never a manually
   counted round number. Each CLI invocation runs exactly **one round**
   (`LogicSweepService.RunConvergenceRoundAsync`); state persists per-book in
   `NodeConvergenceStates` (`ConsecutiveDryRounds`, `TotalRoundsRun`, `LastBookFingerprint` — a
   hash over every enabled beat's text in `SortKey` order, `LastRoundAt`). If the book's
   fingerprint hasn't changed and the dry-round count already meets the requirement, the round is
   skipped with zero LLM cost. Any edit since the last round resets `ConsecutiveDryRounds` to 0 —
   convergence has to be *re-earned*, not just remembered. Hitting the round cap without
   converging resets both counters and files a `LOGICSWEEP-CONVERGENCE [not-converging]` finding
   instead of looping forever — an escalation, not a silent giveup. MCP: `logic_sweep_until_dry`.
4. **Every fix since the last dry round passed its own blast-radius re-check.**
   `BlastRadiusService.GetBlastRadiusBeatIdsAsync(beatId, chapterWindow: 3)` returns the edited
   beat, its same-chapter neighbors within 3 positions either side, and every other beat *anywhere
   in the book* that shares a `BeatEntityPresence` row with it (a character, place, or object
   mentioned in both). This fires automatically and asynchronously (fire-and-forget, exceptions
   logged not surfaced) from `NodeWorkbenchService.UpdateBeatTextAsync` — the single write path
   under every beat edit, manual or automated — which hands the resulting beat-ID set to
   `LogicSweepService.RunNarrowAsync(nodeId, beatIds, anchorBeatId)`: the same five sweep
   dimensions as a full run (everything but the whole-book-context `InsertedBeatDriftRule`),
   scoped to just that subset, filed under a distinct `"beat:{anchor}:blast"` finding key so it
   never collides with full-sweep findings. Every beat save gets a mini-sweep of its own
   consequences for free, without waiting for the next full pass.
5. **Zero open High/BLOCKER Reader-Proxy QA findings.**

### AutoCorrect — the nightly self-heal pass (pure ML/deterministic, zero LLM calls)

`AutoCorrectOrchestratorService` (`v3/Prose.Core/Services/AutoCorrectOrchestratorService.cs`)
runs per-universe, nightly: refresh statistical baselines
(`UniverseProfileService.RefreshDensityBaselinesAsync` — mean/stdev over Flesch reading-ease,
Flesch-Kincaid grade, type-token ratio, dialogue proportion, and words-per-sentence, from at least
5 scored beats, upserted into `UniverseProfiles`), snapshot every book's live prose as a safety net
(`BookArchiveService.ArchiveAsync(nodeId, "autocorrect-pre-run")` → `ArchivedBooks`, same service
behind manual `prose --archive-book (--id | --slug) [--reason "..."]`), then re-run the existing
detectors (`SanityScanService`, `NightlyHealthService`, `BeatDuplicateService`) to refresh Findings.

Only **three narrow categories** ever auto-apply — everything else stays a Finding for a human to
approve:

1. Duplicate character/faction/place entities with **exactly 2 candidates** →
   `DuplicateEntityScanService.MergeAsync` (capped at 20 merges/universe/run; 3+ candidates are
   too ambiguous and stay flag-only).
2. Three specific deterministic drift patterns (`ESE-DANGLING`,
   `CHAR-AFFIL-ALIAS-DRIFT`, `CHAR-HOMETURF-ALIAS-DRIFT`) →
   `DataConsistencyService.ApplyDeterministicFixesWithLedgerAsync`.
3. Cross-book continuity contradictions with a clean 2-variant majority → `ContinuityService.Resolve`.

Near-duplicate **beats** are deliberately excluded even though they looked whitelist-eligible —
`BeatDuplicateService`'s own doc comment calls it "a candidate generator, not a verdict";
auto-deleting risks destroying an intentional callback. Every applied mutation is logged before
commit via `SelfHealLedgerService.LogAsync` (`SelfHealActions` table — `Op`/`Table`/`PkColumn`/
`PkValue`/prior column values per row, enough to reverse it) so a run can be undone:

```
prose --auto-correct-nightly [--universe <slug>] [--dry-run] [--json]
prose --auto-correct-undo (--run-id <guid> | --last-n <N>)
prose --auto-correct-status [--list-runs]
```

`--dry-run` runs every detector and logs `"[dry-run] would ..."` notes without mutating anything.
Scheduling is external — `scripts/register-autocorrect-task.ps1` registers a Windows Task
Scheduler job (`ProseAutoCorrectNightly`, daily 3:00 AM local, `StartWhenAvailable` so a missed
night on a non-24/7 machine catches up) that generates and runs `scripts/run-autocorrect-nightly.ps1`.
**As of 2026-08-15 the scheduled task still runs in `--dry-run`** — the plan is to drop that flag
once a few mornings of `prose --morning-report` output look trustworthy, a config change that
doesn't require re-registering the task. No MCP tool exists yet for AutoCorrect, blast-radius, or
undo — CLI-only for now.

**A caveat worth knowing before typing a long `--reason`:** `BookArchiveService.ArchiveAsync`
inserts into `ArchivedBooks.Reason`, mapped `nvarchar(40)` — neither the CLI handler nor the
service truncates or validates the string first, so `--archive-book --reason "<41+ characters>"`
fails at `SaveChangesAsync` with a SQL truncation error rather than a clean message. Not a risk for
the nightly pass itself (it always passes the fixed, 20-character `"autocorrect-pre-run"`), only
for manual invocations with a long custom reason.

---

## CLI Reference

The canonical invocation `prose` expands to `dotnet run --project v3/Prose.Cli -- <args>`. **265
distinct `--flag` handlers** are dispatched from `v3/Prose.Cli/Program.cs`, backed by **227 handler
files** under `v3/Prose.Cli/Cli/` (2026-08-15 count). What follows is a categorized tour, not an exhaustive
line-by-line dump — `Program.cs` is the exhaustive source, and the CLI has no built-in
`--help` listing every flag; grep `Program.cs` for the authoritative, always-current list.

### Generation & editing

| Command | What it does |
|---|---|
| `prose --write-node --slug <slug>` / `--expand-beat --slug <slug>` | Expand a beat via the full `ProseWriterRouter` pipeline |
| `prose --edit-book --slug <slug>` | Review-weighted auto-editor pass |
| `prose --reflow-book --slug <slug>` | Bounded copy-edit: paragraph breaks, question marks, dialogue attribution — never a rewrite |
| `prose --rebeat-book --slug <slug>` | Regenerate a book's beat spine |
| `prose --insert-beat` / `--split-beat` / `--join-beat` / `--move-beat` | Beat-level structural surgery |
| `prose --draft-combat-scene` | Multi-exchange combat setpiece via `CombatSceneWriter` |

### Structure & bible

| Command | What it does |
|---|---|
| `prose --create-node` / `--create-chapter` | Book/chapter node creation (SS-A43 typed tree) |
| `prose --set-book-bible --slug <slug>` | Write the hand-authored `NodeBible` sections |
| `prose --set-structural-blueprint --slug <slug> --file <path.json>` | Manually author the StoryScope blueprint (no LLM call) when the provider is unavailable |
| `prose --generate-blueprint --slug <slug>` | LLM-generated structural blueprint (9 anti-tell dimensions) |
| `prose --generate-node-doc --slug <slug>` | Regenerate `docs/nodes/<CODE>.md` from `NodeBible` |
| `prose --generate-canon-md --type <type>` | Regenerate one Codex canon doc from `CanonDocumentSections` |
| `prose --sync-markdown` | Sync hand-authored `.md` files (Codex docs, this README, Claude memory) into the `MarkdownFiles` DB table |

### Quality & audit

| Command | What it does |
|---|---|
| `prose --logic-sweep --slug <slug> [--until-dry [--required-dry N] [--max-rounds N]]` | The default QA methodology — six-dimension continuity sweep, no votes; `--until-dry` runs one convergence round per invocation until 2 consecutive dry rounds or an 8-round safety cap (see [Book Completeness](#book-completeness--convergence-gate--self-healing)) |
| `prose --continuity extract --slug <slug>` / `--continuity <list\|resolve\|apply>` | Fact-ledger extraction + contradiction management (`ContinuityService`/`ContinuityClaims`, numeric-safe comparisons) |
| `prose --sequential-read-status --slug <slug>` / `--all [--json]` / `--sequential-read-record --slug <slug> --read-by <name> [--stages N] [--summary "text"]` | Has this book ever been read front-to-back, and is that record still current? (see [Has this book ever actually been read?](#has-this-book-ever-actually-been-read-booksequentialreads)) |
| `prose --archive-book (--id <guid> \| --slug <slug>) [--reason "<text, ≤40 chars>"]` | Whole-book prose snapshot to `ArchivedBooks` — the safety net behind AutoCorrect and manual "before I touch this" backups |
| `prose --auto-correct-nightly [--universe <slug>] [--dry-run] [--json]` / `--auto-correct-undo (--run-id <guid> \| --last-n <N>)` / `--auto-correct-status [--list-runs]` | Nightly self-heal pass — 3 whitelisted deterministic auto-fixes, everything else stays a Finding; every mutation undoable (see [Book Completeness](#book-completeness--convergence-gate--self-healing)) |
| `prose --reader-qa --slug <slug> [--gripe-pass]` | Reader-Proxy QA — comprehension, craft checklist, gripe jury |
| `prose --craft-checklist --slug <slug>` | Hash-gated binary craft/delight checklist |
| `prose --duel --beat-id <guid> --candidate <file> [--goal "..."] [--apply]` | Vote-gated (SS-A44) blind A/B duel between a beat's current text and one candidate revision |
| `prose --diagnose-book --slug <slug>` | 12-check structural pre-flight, cheap |
| `prose --audit-book [--full] [--deep] [--model haiku]` | Repeatable full-QA orchestrator — every audit tool in one pass |
| `prose --publish-readiness --slug <slug>` | docs/LOGIC.md §9's five-point publish-readiness gate as one readout |
| `prose --commandment-audit --slug <slug>` | Gateway/sequel commandment audit (renamed from `--book-audit` 2026-08-30) |
| `prose --plant-audit --slug <slug>` | Orphaned plant/payoff detection |
| `prose --chekhov-audit --slug <slug>` | Setup-and-payoff ledger check |
| `prose --swain-audit [--all] --slug <slug>` / `--swain-repair` | Scene/Sequel doctrine classification + auto-splice repair |
| `prose --storyscope-audit --slug <slug>` | Verifies the structural anti-tells (escalation, event variety, ending style) held |
| `prose --timeline-check --slug <slug>` / `--timeline --slug <slug>` | Deterministic dead-character-acting + wound-regression checks |
| `prose --check-canon --slug <slug>` | Chunked semantic + LLM canon-rule violation sweep |
| `prose --check-fidelity --slug <slug>` | Cosine-similarity prose-vs-bible/synopsis drift check |
| `prose --altitude-audit --slug <slug>` | 10,000↔100 ft drift check (book vs. chapter-synopsis agreement) |
| `prose --examine-emotion --slug <slug>` | 8-dimension per-beat emotional scoring |
| `prose --verify-beat --slug <slug>` / `--verify-book --slug <slug>` | Post-generation verification passes |
| `prose --verify-quote` / `--verify-quotes-batch` | Quote-grounding verification |
| `prose --graph-health --universe <slug> [--used-in-prose-only]` | Entity relationship graph integrity check — orphans, weak links, junk names; `--used-in-prose-only` filters to entities that actually appear in shipped prose (see [World Graph and Interconnectivity](#world-graph-and-interconnectivity)) |
| `prose --coordinate` / `--coverage` / `--backfill-coverage` | Cross-service coverage bookkeeping (non-destructive logging for existing beats) |
| `prose --fix-cross-universe-contamination` | Repairs cross-universe roster/POV leaks (the fail-closed scoping fix from Epoch 4) |
| `prose --review-node` / `--review-entity` | **Quarantined legacy vote panels** — SS-A44-gated, explicit request only (`--review-book`/`--run-panel` aliases retired 2026-08-30) |

### Canon & world

| Command | What it does |
|---|---|
| `prose --add-character` / `--add-place` / `--add-corponation` / … | Seed a new typed entity |
| `prose --generate-glossary` / `--generate-book-glossary` | Per-universe / per-book glossary generation |
| `prose --validate-nouns --slug <slug>` | Deterministic deprecated-noun scan (`NounConsistencyService`) |
| `prose --harvest-voice[-all]` | Mine winning-book edits into proposed voice-rule changes |
| `prose --universe <id>` | Set the active universe scope for subsequent commands (fail-closed as of Epoch 4) |
| `prose --rebuild-graph` | Rebuild the in-memory entity relationship graph from SQL |
| `prose --reembed` | Re-embed all entities via OpenAI `text-embedding-3-small` |

### Export & publish

| Command | What it does |
|---|---|
| `prose --export-node [--slug <slug>]` | docx + EPUB + PDF + audio manuscript `.txt`; prunes stale versions; lands in `{Title}/V{N}/` |
| `prose --export-audio` / `--export-mp3` / `--prepare-audible` | Audiobook export pipeline |
| `prose --export-event-list` / `--export-synopsis` | Structured event list / synopsis export |
| `prose --narrate-node` | ElevenLabs TTS narration pass |
| `prose --generate-cover --book-code <CODE> --generator <name> --prompt "<text>"` | AI cover generation (ChatGPT / Gemini / Ideogram / Flux) |

### Infrastructure

| Command | What it does |
|---|---|
| `prose --migrate-sql --schema` | Bootstrap/upgrade the DB schema — creates from nothing, applies all EF Core migrations, enables temporal versioning. Idempotent. |
| `prose --seed <name>` | Apply a registered raw-T-SQL seed script (see `SqlSeedService.Seeds`) |

### Local / rented-GPU prose generation

Route generation to any OpenAI-compatible endpoint (e.g. Ollama on a rented GPU) without mutating
`appsettings.json`:

```powershell
prose --expand-beat --slug <slug> `
   --local-url  https://<runpod-pod-id>-11434.proxy.runpod.net/v1/chat/completions `
   --local-model qwen2.5-32b-writer
```

Flags: `--local` (use stored `LocalLlmBaseUrl`), `--local-url <url>`, `--local-model <tag>`,
`--local-key <key>`. All overrides are ephemeral. The full enrichment pipeline applies regardless
of endpoint.

---

## MCP Reference

`Prose.Mcp` exposes **273 `[McpServerTool]` methods across 43 tool families** — reflection-generated
into [docs/MCP_TOOLS.md](docs/MCP_TOOLS.md) and re-exported on demand:

```powershell
dotnet run --project v3/Prose.Mcp -- --export-tools docs/MCP_TOOLS.md
```

Register in Claude Code:

```
claude mcp add prose dotnet run --project <path-to-your-clone>/v3/Prose.Mcp/Prose.Mcp.csproj --no-build --configuration Release
```

All tools are surfaced to a client as `mcp__prose__<name>`; most return a JSON string; the canon
behind every call is the SQL database, scoped to the active Universe. Full family table:

| Family | Tools | Family | Tools |
|---|---|---|---|
| Node | 38 | Encyclopedia | 35 |
| Config | 14 | Quality | 12 |
| Canon | 9 | Survey | 7 |
| Canon Doc | 7 | Gear Entity Crud | 7 |
| Lore Triple | 7 | Edit Session | 6 |
| Findings | 6 | Planning | 6 |
| Plant Payoff | 6 | Story | 6 |
| Voice | 6 | Context | 5 |
| Core Entity Crud | 5 | Universe | 5 |
| Verification | 5 | World Entity Crud | 5 |
| Beat Event List | 3 | Beat Lens | 3 |
| Bible | 3 | Chekhov Audit | 1 |
| Combat | 1 | Book Health | 1 |
| Book Audit | 2 | Book Logic | 2 |
| Continuity | 2 | Repository | 2 |
| Species | 2 | Data Integrity | 4 |
| Entity Context | 4 | Glossary | 4 |
| Narrative Science | 4 | Scene | 4 |
| Noun Consistency | 3 | Reader Qa | 3 |
| Story Scope | 3 | Swain | 3 |
| Workflow Monitor | 3 | | |

Representative tools by family (the full per-tool reference with parameter schemas and
descriptions is [docs/MCP_TOOLS.md](docs/MCP_TOOLS.md)):

| Family | Sample tools |
|---|---|
| Node | `insert_beat`, `split_beat`, `join_beat`, `delete_beat`, `rebeat_book`, `reflow_book`, `export_node`, `narrate_book`, `get_book_spine` |
| Encyclopedia | Read-only getters/listers for characters, places, factions, weapons, ammo, cyberware, equipment, apparel, pharmaceuticals, genemods, automata, archetypes, quotes, documents |
| Quality | `validate_canon_text`, `review_book`, `diagnose_book`, `scan_book_violations`, `check_prose`, `check_semantic_fidelity`, `findings_stats` |
| Config | `sync_markdown_files`, `recall_markdown_files`, `restore_markdown_file`, `doc_context_prepare`, `doc_context_status` |
| Canon Doc | `set_canon_section`, `generate_canon_md`, `get_canon_document`, `list_canon_sections` |
| Lore Triple | Continuity-claim extraction; list/resolve/apply contradictions; `append_book_amendment` |
| Swain | `swain_audit`, `swain_audit_all`, `swain_repair` |
| Story Scope | `storyscope_audit`, structural-blueprint access |
| World Entity Crud / Gear Entity Crud | `create_character`, `create_place`, `create_faction`, `create_weapon`, `create_cyberware`, … |

### Agentic authoring is the MCP surface itself

There is **no internal "Operator" service for prose authoring** — that path (`WriterOperatorService`,
the Epoch-2 `Operator`/CoWriter chat cluster) was deleted in Epoch 4. The `Services/Operator/`
folder that still exists under `Prose.Core` is unrelated: it's KDP-publish browser-automation
tool-calling (`IToolCallingLlm`, `KdpToolRegistry`, `KdpTools/`) consumed only by
`Prose.KdpPublish` — see [Architecture at a Glance](#architecture-at-a-glance) for the
Claude-first/OpenAI-fallback shape of that abstraction. Agentic authoring today means Claude — via
this MCP server — calling the canon directly, with the human approving canon and voice changes
rather than authoring them.

---

## Database

### Connection

```
Server=(localdb)\MSSQLLocalDB;Database=Prose;Trusted_Connection=True;TrustServerCertificate=True;
```

Windows Authentication — no `-U`/`-P` needed. Same as `appsettings.json` → `ConnectionStrings:Prose`.

### Database access

**HARD, ABSOLUTE (2026-08-22): nothing reaches the database except through Prose.Hub — reads AND
writes, no exceptions.** No raw `sqlcmd`, not even a read-only lookup — this section used to say
otherwise and that guidance was wrong; it caused a real incident and must not be resurrected. For
an ad hoc lookup (book lists, scores, entity counts, etc.), use the `/show` skill or an existing
CLI `--flag` / MCP tool, both of which route through the running Prose.Hub process. If no such
command exists for something you need, stop and tell the user the gap exists rather than falling
back to a direct query — the Hub is what makes every change calibrated, tested, verified, and
tracked by the rest of the system (reconciliation, self-heal, findings); a raw SQL statement,
even read-only, bypasses that entirely.

### Schema reference

The DB schema is documented at [docs/schema.md](docs/schema.md) — **263 base tables** (264 FKs,
263 PKs, live-queried 2026-08-15) plus **168 system-versioned `_History` tables**, generated by
`tools/gen-schema.ps1`. Regenerate after any migration — `docs/schema.md`'s snapshot (2026-08-14)
already covers `ArchivedBooks`, `SelfHealActions`, `UniverseProfiles`, and `LlmCallHistories`, but
predates `BookSequentialReads` and `NodeConvergenceStates` (both created 2026-08-15, see
[Key tables](#key-tables) below), so a re-run is due.

### Content census (2026-08-14, live query)

**37 books**, 485 chapters, 2 series (`Nodes.Kind`). **13,653 total beat rows** in `Beats`;
**12,032** are linked into a live book's spine via `BeatNodes` (the rest — 1,621 — currently
belong to no book). **Re-verified live 2026-08-15: 37 books, 482 chapters, 13,648 beats, 12,015
linked** — the small drops since 2026-08-14 are real content work (duplicate/orphaned beats found
and removed during that day's sequential-read passes — see
[Has this book ever actually been read?](#has-this-book-ever-actually-been-read-booksequentialreads)),
not drift or measurement error. `BeatNodes` presence *is* the enable signal now: migration
`20260813053520_DropBeatNodeIsEnabled` (same day as [Epoch 4](#epoch-4--command-line-only-2026-08-13--present))
dropped the `IsEnabled` boolean column outright — a beat is "enabled" by having a `BeatNodes` row,
disabled by not having one. **`CLAUDE.md`'s "Key schema facts for queries" section still cites
`BeatNodes(NodeId, BeatId, SortKey, IsEnabled)` — the `IsEnabled` column no longer exists**; any
query filtering on it will fail with `Invalid column name 'IsEnabled'`, not silently return zero
rows. Added to [Status](#status)'s known-gaps list.

### Key tables

| Table | Role |
|---|---|
| `Entities` | Universal spine — one row per entity: `Id, Name, Slug, EntityType, Description, IsActive, UniverseId` |
| `Nodes` / `Nodes_History` | System-versioned typed content tree (series/book/chapter) + score |
| `Beats` / `Beats_History` | System-versioned prose beats — every edit rewindable |
| `BeatNodes` | Joins Beats to Nodes (`NodeId, BeatId, SortKey`) — a row's mere presence *is* the enabled signal (`IsEnabled` column dropped 2026-08-13, see [Content census](#content-census-2026-08-14-live-query)); supersedes the retired `Beats.NodeId` |
| `EntityEmbeddings` | 1536-d vectors (OpenAI `text-embedding-3-small`); cosine via `VECTOR_DISTANCE` |
| `Edges` | Typed directed relationships (`carries`, `wields`, `member_of`, `located_at`, …) with validity windows |
| `EntityStateEvents` | Append-only in-world story-time ledger: `(entity, predicate, value, beatId, storyTime)` |
| `ContinuityClaims` | The fact ledger — extracted `(entity, predicate, object)` claims from prose; `Status` ∈ `NEW/CONFIRMED/CONTRADICTED/CANONICAL/REJECTED/SUPERSEDED`, numeric-safe comparison on an allowlisted predicate set (see [Book Completeness](#book-completeness--convergence-gate--self-healing)) |
| `NodeConvergenceStates` | Per-book logic-sweep convergence state for `--logic-sweep --until-dry` — consecutive dry-round count, total rounds run, a fingerprint hash over the enabled beat sequence |
| `BookSequentialReads` | Records a genuine front-to-back read of a book — self-invalidating via a beat-sequence hash (see [Has this book ever actually been read?](#has-this-book-ever-actually-been-read-booksequentialreads)) |
| `ArchivedBooks` | Whole-book prose snapshots from `prose --archive-book` / the AutoCorrect pre-run safety net |
| `SelfHealActions` | Per-mutation undo ledger for AutoCorrect's whitelisted auto-fixes — reversible via `prose --auto-correct-undo` |
| `LlmCallHistories` | Every `LlmRouter` fallback-chain hop (provider, model, success, hop index, token estimate, cost) |
| `Findings` | Autonomous quality findings inbox — approve, dismiss, or apply before editing a beat |
| `CanonDocumentSections` | Hand-authored canon sections that generate to `docs/BIBLE.md`, `docs/WORLD.md`, `docs/GLMZ.md`, `docs/SCRY.md`, etc. via `generate_canon_md` |
| `MarkdownFiles` | System-versioned mirror of every `.md` file the toolchain depends on (Codex docs, root `README.md`, Claude memory) — see [About This Document](#about-this-document) |
| `Universe` / `UniverseProfiles` | The 7 registered universes and their per-universe profile settings |
| `ReaderKnowledgeFacts` | Dramatic-irony bookkeeping (moved out of `Findings` in Epoch 4 — it was misusing that table as permanent working state) |
| `PlantPayoffs` | Plant/payoff pair registry with `Hidden`/`Visible`/`Meta` transparency |
| `GlossaryTerms` | Universe-scoped glossary terms, filtered live to what a book's prose actually uses |
| `Assets` / `CoverImagePrompts` | Cover image library + generation prompt history |

### Connection string resolution order

1. Environment variable `ConnectionStrings__Prose`
2. `appsettings.json` → `ConnectionStrings:Prose`
3. LocalDB fallback (dev default)

---

## Universes

Every canon and story row belongs to exactly one Universe ([SS-LAW-15](docs/BIBLE.md#SS-§5)).
Eight are currently registered:

| Universe | Slug | Genre | World-facts doc | Craft-layer doc |
|---|---|---|---|---|
| **GLMZ** | `glmz` | Cyberpunk, 2226, the flagship (*Bushido Coda*) | [docs/BIBLE.md](docs/BIBLE.md) / [docs/WORLD.md](docs/WORLD.md) | [docs/GLMZ.md](docs/GLMZ.md) |
| **SCRY** | `scry` | Fantasy/steampunk (The Entos / The Caul) | [docs/universes/ENTOS.md](docs/universes/ENTOS.md) | [docs/SCRY.md](docs/SCRY.md) |
| **NONFICTION** | `nonfiction` | Citation-grounded nonfiction (formerly GSPL, then SOURCE) | — | [docs/NONFICTION.md](docs/NONFICTION.md) |
| **HORROR** | `horror` | Ambiguous/analog horror | — | [docs/HORROR.md](docs/HORROR.md) |
| **EROTICA** | `erotica` | Sixth universe added 2026-08-04 | — | — |
| **GOSPEL** | `gospel` | Split out from NONFICTION; New Testament claims-cataloguing campaign | [docs/gospel/](docs/gospel/) | — |
| **FICTION** | `fiction` | General literary fiction (formerly EPIC) | — | — |
| **EVE** | `eve` | Experiment Eve — first game/non-literary universe (ExperimentEve, RFC 0007) | — | — |

Universe scoping is **fail-closed** as of Epoch 4: an unset or invalid universe scope blocks
generation rather than silently defaulting to "every universe" — the fix that closed a
cross-universe content-leak incident (`--fix-cross-universe-contamination` repairs any residue).
Switch scope with `prose --universe <id>` (CLI, per-process) or MCP `switch_universe`
(per-session) — two processes or sessions can target different universes simultaneously.

### Universe Interchange (RFC 0007)

Prose is the repository of universes — not just the writing program for books, but a
canonical store other MindAttic apps read from and write to. The **Universe Interchange**
format (`docs/schemas/universe.schema.json`) is the contract: a single JSON file per universe
(`<app>/universe/<slug>.universe.json`) holding `{universe: {id, name, tagline, era, setting,
logline, rules[]}, entities: [{id, type, name, summary, details{}, relations[], tags[]}]}`.

- **Import/export** — `UniverseInterchangeService` maps interchange entities onto the generic
  Entity spine (`Entity` + `Record.Json` as the round-trip source of truth + `EntityTag` +
  `Edge`), auto-registering a `RepositoryDefinition` for any novel entity `type`. Import is an
  idempotent upsert by `(UniverseId, Slug)`; a dangling `relations[].to` auto-creates a
  `Status=stub` entity, promoted to canon if that entity's own row arrives in a later import.
- **CLI**: `prose --universe-import <path> [--universe <slug>]` · `prose --universe-export
  <slug> <path>` · `prose --universe-sync <path>` (import then export back, normalizing the
  file for the consumer app to commit).
- **MCP**: `import_universe_file`, `export_universe_file`, `get_universe_entity`,
  `search_universe` (cross-universe lookups that don't switch the session's active universe).
- **Hub HTTP**: `POST /api/universes/{slug}/import` (game-side push) plus the pre-existing
  `GET /api/universes/{slug}/{entities,neighbors,search,snapshot}` reads.
- **The Outbox** — `GET/POST /api/outbox/{consumer}` is the Hub's message queue toward another
  app's Claude Code session: Prose enqueues a one-line summary ("GDD chapter 3 drafted — pull
  barks"), and a `UserPromptSubmit` hook in the consumer repo drains it on the next prompt.
  `?peek=true` reads without marking delivered.
- **First consumer**: [ExperimentEve](../ExperimentEve) — a 75-entity game universe (Kingsport,
  a real-time-night survival-horror-parody). `npm run universe -- push|pull` round-trips
  against this contract. Phase 2 (design-approved, not yet built): Prose writes the game's GDD,
  script, and bark sheets as ordinary Books in the EVE universe — same Book→Chapter→Beat
  engine, different deliverable.

### Portable Writing Service — generic for the next consumer

Everything above proved the pattern against one consumer. These pieces generalize it for *any*
sibling project, no Prose-side code changes needed per new consumer — full runbook:
[`docs/CONSUMER_ONBOARDING.md`](docs/CONSUMER_ONBOARDING.md).

- **Auth**: the Hub's sensitive endpoints (`/api/cli-invoke`, `/api/mcp-invoke`,
  `/api/universes/{slug}/import`, `/api/outbox/{consumer}`, `/api/generate-scene`) require an
  `X-Prose-Key` header — a single shared secret, generated once at Hub startup into the shared
  `Settings.json` store, read automatically by every trusted local process (Cli, Mcp). Not
  enterprise auth; just enough to stop an arbitrary local process from reaching the two generic
  reflection dispatchers (which can invoke any CLI command or MCP tool by name) unauthenticated.
- **`generate_scene` / `prose --generate-scene`** — write a scene or line of dialog **without** a
  pre-existing Book/Chapter/Beat row. Ephemeral by default (pacing, dialogue voice, canon
  grounding, consequence/gear constraints, ambient sensory detail, and entity pre-check warnings
  all still apply); pass `--node <slug>` for "attached mode" to borrow an existing book's canon
  and continuity without writing a Beat row to it. CLI, MCP, and `POST /api/generate-scene` are
  three equivalent entry points.
- **`export_barks` / `prose --barks-export <universe> <path>`** — walk a universe's (or one
  book/chapter's) beats and emit every beat with a single recorded POV speaker as `{barkId,
  speakerEntitySlug, text, context}`. A beat with no recorded speaker is skipped and counted,
  never silently dropped.
- **The Outbox is already generic** — `consumer` has always been a free-form string with no
  registration step; a new project just starts calling `/api/outbox/<its-own-name>`.

---

## The Subsystems

**309 services** live directly under `v3/Prose.Core/Services/` (351 including the `Audit/`,
`CoverImage/`, `Local/`, `Operator/`, and `VideoGen/` subfolders), grouped here by role.

### 1. Canon & data layer

| Service | Role |
|---|---|
| `BookRepository` / `ChapterRepository` / `SeriesRepository` / `UserRepository` | CRUD repositories for their entity types |
| `RepositoryDefinitionService` | Runtime-defined custom entity repositories (slug, icon, route) |
| `BookOutlineService` | Builds ordered chapter/beat outlines for a book |
| `LoreService` | Canonical lore lookup for prompt grounding |
| `CanonGroundingService` | Injects grounded entity dossiers into LLM prompts |
| `CanonRetrievalService` | RAG-style semantic + graph lookup for context assembly |
| `WorldStateLedger` | Append-only event log powering state-at-beat queries |
| `DataConsistencyService` | Audits FK and relational integrity across entity tables |
| `SqlSeedService` | Applies canonical SQL seeds from C# |
| `CanonExportService` / `ExportService` | Export canonical entity data to JSON/zip |

### 2. Embeddings & semantic retrieval

| Service | Role |
|---|---|
| `EmbeddingService` | OpenAI `text-embedding-3-small` vector store; SHA-256 drift detection; cosine NN search. Use `FindSimilarAsync` — never substring matching |
| `SemanticIndexService` | Low-level embedding index management |
| `GlobalSearchService` / `GlobalSearchWarmupService` | Combined full-text + semantic search; background preheater |
| `SemanticFidelityService` | Goodhart-drift check — cosine similarity of beat prose vs. book bible/synopsis |
| `KnowledgeMapService` | Clusters entities by embedding similarity |
| `ThematicIndexService` | Tags/indexes beats and books by theme for retrieval |

### 3. Prose generation

| Service | Role |
|---|---|
| `ProseWriterRouter` | Sole prose entry point (SS-A16) — see [Prose Generation Pipeline](#prose-generation-pipeline) |
| `DocContextService` / `DocContextStack` | Dynamic Context Memory — five-pass, four-layer retrieval; see `CLAUDE.md`'s DCM section |
| `BeatModeDetector` | Classifies beat mode |
| `CombatProseGuidance` | Combat-prose laws |
| `BeatGeneratorService` | Core generation — expert-persona panel votes on the best next beat; tier-locked HIGH |
| `BeatPromptBuilder` | Constructs beat prompts with canon context, voice rules, world state |
| `CombatSceneWriter` | Canon-aware combat prose — loadouts, ammo, bio-battery, terrain |
| `DialogueService` | Dialogue in per-character voice registers |
| `NodeBibleService` | Manages per-book bibles (authorial spine, beat plan, synopsis) |
| `OutlineService` / `StoryMethodologyService` | Seed-to-outline generation; five-act/scene-anatomy frameworks |
| `NpcGenerator` / `DynamicPlaceGenerator` / `ContractGenerator` | Procedural NPCs, places, in-world documents grounded in canon |
| `BeatRepairService` | Self-healing targeted rewrite — attempted before any Full-tier audit files a Finding (Epoch 4) |

### 4. Review & quality

| Service | Role |
|---|---|
| `NodeReviewService` | Legacy quorum-vote review path — SS-A44-quarantined, explicit request only |
| `ReviewEffortProfile` | RFC 0009 cost tier (draft / standard / deep) |
| `StructuralDiagnosticService` | Structural pre-flight checks |
| `WritingQualityService` | Deterministic heuristic pass (no LLM): first-line strength, tension delta, motif reuse, voice-cadence drift |
| `EmotionalDepthService` | 8-dimension emotional examination at beat level (SS-A15) |
| `FindingsService` / `FindingApplyService` | Findings inbox CRUD + applying an approved fix directly to `Beat.Text` (fixed in Epoch 4 — previously matched a pseudo-path that never existed on disk) |
| `BookAuditService` | Gateway/Sequel commandment auditing |
| `PlantPayoffService` | Plant/payoff registry; orphan-audit on completion |
| `SwainAuditService` | Scene/Sequel doctrine classification + auto-repair (SS-A47) |

### 5. Storytelling science

| Service | Role |
|---|---|
| `NarrativeScienceService` | Sacred-flaw/theory-of-control, dramatic-question, five-act mapping, antihero-empathy (Storr framework) |
| `ArcTrackerService` | Per-book five-act position and beat-level progression |
| `PacingService` | Flags over-stayed, under-stayed, or non-escalating emotional registers |
| `StructuralBlueprintService` | StoryScope's 9 anti-tell structural dimensions, committed pre-prose |

### 6. Continuity & world-modelling

Mostly deterministic (DB-only, no LLM cost).

| Service | Role | LLM? |
|---|---|---|
| `EntityRelationshipService` | BFS Edge-graph relationship trees for scene context | No |
| `ProsePatternGuard` | Regex linter for banned patterns | No |
| `AmbientDetailInjector` | Sensory palette from carried/worn gear | No |
| `WorldStateAtBeatService` | Point-in-time world-state snapshot from the ledger | No |
| `GearCarryEnforcer` | Detects gear-use verbs and checks the carry graph allows them | No |
| `BehavioralInvariantEnforcer` | Checks prose against a character's registered behavioral rules | Yes (1/char) |
| `WeaponAmmoCompatibilityService` | Validates weapon+ammo pairs | No |
| `TimelineConsistencyService` | Deterministic dead-character-acting + wound-regression checks | No |
| `LocationContradictionService` | Detects a character in two places at once | No |
| `CanonContradictionService` | Chunked semantic + LLM canon-rule sweep | Yes |
| `WorldConsistencyService` | Prose scan for world-rule violations | Yes |
| `SceneContextAssembler` | X-Ray scene assembly — entity mentions → dossiers → voice + behavioral context | No |
| `WorldGraphService` | In-memory adjacency graph over all entities/edges — see [World Graph and Interconnectivity](#world-graph-and-interconnectivity) | No |
| `ContinuityExtractionService` | Multi-provider extraction of `(entity, predicate, object)` claims | Yes |
| `ContinuityValidatorService` | Validates claims vs. live entity state | Mixed |
| `NounConsistencyService` | Deterministic deprecated/renamed-noun scan across enabled beats (`--validate-nouns`) | No |

### 7. Voice & persona

| Service | Role |
|---|---|
| `VoiceHarvestService` | Mines author edits + directives from winning books into proposed voice rules |
| `ExpertPersonaService` / `ExpertPersonaCatalog` | Reusable expert-persona pool; genre experts, craft specialists |
| `NamePoolService` | Culturally diverse name pools (Ubiquitous Diaspora rule) |

### 8. Memory & editorial lessons

| Service | Role |
|---|---|
| `ProseLessonStore` | SQL-backed store of author rulings, scoped global/node/beat; injected into review prompts |
| `ActionConfigService` | Per-action LLM tier registry; `ChapterBeatWriter`/`Expander` locked at HIGH |
| `SettingsKvStore` / `SettingsService` | SQL-backed key-value config; app-wide settings façade |
| `MarkdownFileService` | Syncs hand-authored `.md` files (Codex docs, root README, Claude memory) into `MarkdownFiles`, system-versioned |

### 9. Export & publish

| Service | Role |
|---|---|
| `ManuscriptExportService` | All-in-one export — EPUB 3 + PDF (QuestPDF, 6"×9" KDP trim) + Markdown |
| `DocxExportService` | KDP-ready Word `.docx` via OpenXml |
| `BookExportService` | Book-level EPUB 3 + PDF |
| `NodeMarkdownExporter` | Ordered beats → markdown with stable content fingerprint |
| `ElevenLabsTtsService` | ElevenLabs TTS narration; tiered fallback |
| `LocalTts` / `PiperTtsService` / `WindowsTtsService` | Local TTS fallbacks — see the `tools/chatterbox`/`tools/kokoro` note in [Repository Layout](#repository-layout) |

### 10. LLM providers & routing

| Service | Role |
|---|---|
| `LlmRouter` | The live fallback chain (also an `ILlmService` itself): tries the primary provider (`SettingsService.ActiveLlmProvider`, default `claude-api`), then walks `ActiveLlmProviderChain` in configured order, de-duplicated. **Any** exception on a hop (auth, quota, rate-limit, network — not typed/discriminated) triggers the next hop; no fallback on success. Every hop, success or failure, is logged to `LlmCallHistories` (provider, model, hop index, token estimate, cost) |
| `ClaudeService` / `OpenAiService` / `GeminiService` / `DeepSeekService` / `MistralService` / `KimiService` / `PerplexityService` | Metered API-key providers, each a thin `ILlmService` HTTP client over its vendor's completions endpoint |
| `CodexCliService` / `GeminiCliService` | "CLI-shelling" providers — same `ILlmService` interface, but shell out to the `codex`/`gemini` CLI binary (`Process.Start`, prompt piped via stdin, JSONL parsed from stdout) and ride an existing subscription OAuth session (`codex login`) instead of a metered API key. Always costed at $0 in `LlmCallHistories` |
| `MultiLlmService` | Multi-provider fan-out via `MindAttic.Legion` (broader provider set than `LlmRouter`'s: adds Grok, Groq, Together, OpenRouter, Fireworks, Cohere) |
| `AssignTiersService` | Assigns Haiku/Sonnet/Opus class to actions per settings |

`--set-llm-provider claude-api|claude-team` (`SetLlmProviderCli.cs`) is the only CLI surface for
switching providers, and **only accepts those two Claude variants** — it was not extended when
Gemini/DeepSeek/Mistral/Kimi/Perplexity/the CLI-shelling providers were added, so switching to any
of them today means editing `ActiveLlmProviderChain` directly. No MCP tool exposes provider
selection or queries `LlmCallHistories`. See [docs/PROVIDERS.md](docs/PROVIDERS.md) for the
checked-in table of which services depend on which external provider — **written before this
provider expansion, so it still only describes the Anthropic/OpenAI-only/both split** and needs a
pass to cover Gemini/DeepSeek/Mistral/Kimi/Perplexity/the CLI-shelling pair.

---

## Cost Tiering

RFC 0009. Spend scales to a task's importance — see
[docs/rfc/0009-cost-tiered-storytelling-engine.md](docs/rfc/0009-cost-tiered-storytelling-engine.md).

| Tier (`--effort`) | LLM calls | Models | Used for |
|---|---|---|---|
| `draft` | ~6 (−84%) | cheapest per provider (Haiku / Flash-Lite / Nano) | mid-draft spot checks; per-beat iteration |
| `standard` | ~15 (−60%) | mid-tier defaults | the per-book QA pass |
| `deep` | ~37 (baseline) | mid-tier defaults + full diagnosis + prose critique | the flagship / cumulative pass |

---

## Running Locally

Prerequisites: .NET 10 SDK, SQL Server LocalDB, at least one LLM provider API key wired through
`MindAttic.Vault`.

One-time, right after cloning — wires up the repo's git hooks (`.githooks/`), not picked up
automatically by a fresh clone:

```powershell
git config core.hooksPath .githooks
```

Without this, `pre-commit`'s live-LLM-key gate and `post-commit`'s markdown-sync simply never
fire — no error, they just silently don't run.

```powershell
cd v3
dotnet restore

# Schema (creates the DB from nothing + applies all EF Core migrations + enables
# temporal SYSTEM_VERSIONING). Idempotent; safe to re-run.
dotnet run --project Prose.Cli -- --migrate-sql --schema

# Optional: a handful of older, pre-EF-migration schema deltas and small data
# inserts still live as raw .sql under Prose.Core/Data/Sql/, run via
# `prose --seed <name>` (see SqlSeedService.Seeds for registered names).
```

There is no web host to start — the CLI and the MCP server are the entire surface:

```powershell
# CLI is invoked per-command, not a long-running process:
dotnet run --project Prose.Cli -- --workflow-status --all

# MCP server:
dotnet build Prose.Mcp/Prose.Mcp.csproj -c Release
dotnet run --project Prose.Mcp --no-build --configuration Release
```

The `Prose.KdpPublish` desktop app (WPF, requires Windows) and `Prose.LlmCli` (`prose-llm`,
cross-platform) are run independently — see [Architecture at a Glance](#architecture-at-a-glance).

Run tests:

```powershell
dotnet test v3/Prose.UnitTests   # NUnit + bUnit, 185 test files
```

> The Cypress suite (`npx cypress run`) predates Epoch 4 and exercises routes on the now-deleted
> Blazor UI — see [Tests](#tests).

### Environment variables

All optional — the app starts and most features work without any of them (credentials fall back
through `MindAttic.Vault` / DB-stored settings). Set only what a feature you're using needs.

| Group | Variables |
|---|---|
| LLM providers | `PROSE_CLAUDE_API_KEY`, `PROSE_OPENAI_API_KEY`, `PROSE_GEMINI_API_KEY`, `PROSE_DEEPSEEK_API_KEY`, `PROSE_MISTRAL_API_KEY`, `PROSE_KIMI_API_KEY`, `PROSE_PERPLEXITY_API_KEY`, `PROSE_GROK_API_KEY`, `PROSE_GROQ_API_KEY`, `PROSE_TOGETHER_API_KEY`, `PROSE_OPENROUTER_API_KEY`, `PROSE_FIREWORKS_API_KEY`, `PROSE_COHERE_API_KEY` — `CodexCliService`/`GeminiCliService` need none of these; they ride an existing `codex login`/`gemini` CLI OAuth session instead |
| Audio / media gen | `PROSE_ELEVENLABS_API_KEY`, `PROSE_IDEOGRAM_API_KEY`, `PROSE_FAL_API_KEY`, `PROSE_STABILITY_API_KEY`, `PROSE_KLING_API_KEY`, `PROSE_RUNWAY_API_KEY` |
| Maps | `PROSE_MAP_APP_ID`, `PROSE_MAP_API_KEY`, `PROSE_GOOGLE_MAPS_API_KEY` |
| SMTP | `PROSE_SMTP_HOST`, `PROSE_SMTP_PORT`, `PROSE_SMTP_USERNAME`, `PROSE_SMTP_PASSWORD`, `PROSE_SMTP_FROM` |
| Data paths | `PROSE_DATA_ROOT`, `PROSE_MUTABLE_DATA_ROOT` — override where canon/exports live |
| Universe scope | `PROSE_UNIVERSE` — default universe slug for CLI commands that need one |
| Local TTS | `PROSE_PYTHON`, `PROSE_ENG_SCRIPT`, `PROSE_ENG_VOICE`, `PROSE_CA_BUNDLE`, `PROSE_PIPER_EXE`, `PROSE_PIPER_MODEL` — see the `tools/chatterbox`/`tools/kokoro` note below; local TTS setup is currently incomplete |

`RunPod`/GPU routing uses different variables — see [The Subsystems](#the-subsystems) → LLM providers.

After editing any `docs/*` Codex file:

```powershell
powershell -File tools/codex.ps1 digest
powershell -File tools/codex.ps1 doctor   # must pass
```

---

## Repository Layout

```
Prose/
├── v3/                          # Active engine — command-line only (Epoch 4)
│   ├── Prose.slnx
│   ├── Prose.Core/      # Canon services, generation, embeddings, review, continuity
│   │   ├── Services/            # 309 services directly here, 351 incl. subfolders
│   │   ├── Services/Operator/   # KDP browser-automation tool-calling (NOT prose authoring)
│   │   └── Data/                # EF entities, DbContext, Migrations/, Sql/ raw-T-SQL scripts
│   ├── Prose.Cli/       # Standalone CLI — 263 dispatched handlers, incl. --migrate-sql
│   ├── Prose.Mcp/       # MCP server — 273 tools across 43 families
│   ├── Prose.KdpPublish/# WPF + WebView2 desktop app — KDP publish automation
│   ├── Prose.LlmCli/    # prose-llm — generic multi-provider LLM CLI escape hatch
│   ├── Prose.UnitTests/ # NUnit + bUnit tests (185 files)
│   └── PromoteEsperanza/, PurgeOldNames/, RunRepair/, SyncSableProse/, WriteSableOrigin/
│                          # one-off data-migration console utilities
├── tools/
│   ├── codex.ps1                # Codex digest + doctor (run after editing docs/*)
│   ├── build-readme.js          # Converts this README to a standalone .htm (npm run docs)
│   ├── gen-schema.ps1           # Regenerates docs/schema.md from the live DB
│   ├── check-contradictions.js  # Legion-Quorum chapter-vs-canon sweep
│   ├── chatterbox/              # Free local TTS via Resemble AI Chatterbox (MIT) — see note below
│   └── kokoro/                  # Free local TTS via Kokoro-82M (Apache-2.0) — see note below
├── docs/                        # Codex docs — BIBLE.md, USER_STORIES.md, rfc/, universes/
├── html/                        # Standalone patent-disclosures.htm
├── engine_data/                 # Canon entity seed/export mirror (SQL is the live read path)
└── cypress/                     # Cypress e2e tests — currently orphaned, see Tests
```

> **`tools/chatterbox` and `tools/kokoro`:** both directories contain only a committed Python
> `.venv/` with no `requirements.txt` and no setup script — and that `.venv` is itself broken (its
> activation scripts hardcode a pre-rename path from before the StreetSamurai → Prose rename).
> Local TTS via these tools needs a from-scratch Python environment rebuilt by hand; ElevenLabs TTS
> (cloud) works out of the box. Flagged for cleanup, not yet fixed.

> **`v3/python/`:** no Python source remains — only a `__pycache__/` leftover and two cached
> SQLite artifacts (`lore-triples.db`, `truth.db`, the larger one >500MB) from a since-deleted SPO
> (Subject-Predicate-Object) triple-extraction pipeline. The `.py` scripts and their own README are
> gone; only their output cache survives. Orphaned cruft, not a live subsystem — safe to delete,
> not yet done.

---

## Key Dependencies

Verified against the live `.csproj` files (2026-08-15). Includes the `MindAttic.*` NuGet packages
(external sibling projects, e.g. `MindAttic.Legion`, `MindAttic.Vault`) alongside third-party ones.

| Package | Project | Purpose |
|---|---|---|
| `Markdig` 1.1.2 | Prose.Core | Markdown → HTML rendering |
| `QuikGraph` / `QuikGraph.Serialization` 2.5.0 | Prose.Core | In-memory directed relationship graph (World Graph) |
| `System.Speech` 10.0.9 | Prose.Core | Windows SAPI TTS fallback |
| `Microsoft.Extensions.Http` 10.0.9 | Prose.Core | HttpClient factory |
| `Azure.Identity` 1.21.0 / `Azure.Storage.Blobs` 12.29.1 | Prose.Core | Blob storage (cover images, exports) via OIDC/managed identity — no passwords |
| `DocumentFormat.OpenXml` 3.3.0 | Prose.Core | DOCX manuscript export |
| `QuestPDF` 2026.5.0 | Prose.Core | PDF export |
| `Microsoft.EntityFrameworkCore.SqlServer` / `.Sqlite` / `.Design` 10.0.9 | Prose.Core, Prose.Cli | EF Core against SQL Server (live) and SQLite (`TestDbFactory`) |
| `Serilog` 4.3.1 + `.Extensions.Logging` / `.Sinks.File` | Prose.Core | Structured logging |
| `BCrypt.Net-Next` 4.0.3 | Prose.Core | Password hashing utility (no current call sites found — verify before relying on it) |
| `MindAttic.Legion` 23.0.0 | Prose.Core, Prose.LlmCli | Multi-provider LLM quorum/persona review engine (external MindAttic package) |
| `MindAttic.Vault` 1.0.0 | Prose.Core, Prose.LlmCli | Secrets/credential vault |
| `MindAttic.Authentication` 2.0.0 | Prose.Core | Auth |
| `MindAttic.Media` / `.Azure` 1.0.0 | Prose.Core | Media handling |
| `ModelContextProtocol` (floating `*`) | Prose.Mcp | MCP server SDK |
| `Microsoft.Extensions.Hosting` 10.0.5 (Mcp) / 10.0.0 (Cli) | Prose.Mcp, Prose.Cli | Generic host |
| `Microsoft.Extensions.Logging.Console` 10.0.0 | Prose.Cli | Console logging provider |

`Prose.KdpPublish` (WPF + WebView2 desktop app, not in `Prose.slnx`) has its own dependency set not
audited here — see the project's own `.csproj` if working on it directly.

---

## Database Migrations

Raw T-SQL files live under `v3/Prose.Core/Data/Sql/`. Most are historical, pre-EF-migration schema
deltas superseded by `v3/Prose.Core/Migrations/` — run individually via `prose --seed <name>`
(see `SqlSeedService.Seeds` for registered names) only if a specific older environment needs them.
A few small data-only scripts (e.g. universe seed inserts) are not yet registered anywhere.
**Nothing reaches the database except through Prose.Hub — reads AND writes, no exceptions (HARD,
absolute, 2026-08-22).** If a fresh DB is missing rows from one of these unregistered scripts,
register it in `SqlSeedService.Seeds` and run it via `prose --seed <name>` — do not apply it by
hand with `sqlcmd`.

```powershell
dotnet run --project v3/Prose.Cli -- --migrate-sql --schema
```

---

## Deployment

**Retired 2026-08-23 (author decision).** Prose is a local-only, single-user application — there
is no hosted/production deployment target and there will not be one. The Azure App Service
pipeline (`.github/workflows/azure-deploy.yml`), the Azure SQL provisioning guide (`infra/`), and
`docs/infra/AZURE_DEPLOY.md` have all been deleted. This resolves the open question the Epoch 4
break had left unanswered (deploy the MCP server web-facing vs. build+test+migrate only vs.
retire) — the answer is retire; there is no browser surface, and there is no second user to serve
one to.

The only "deployment" that exists is local: `v3/Prose.Hub/tools/deploy.ps1` publishes
`Prose.Hub.exe` (bundling `Prose.Cli` + `Prose.Mcp` + `Prose.Core`) to `C:\Apps\Prose\Prose.Hub\`
and launches it — see `.claude/hooks/start-prose-hub.ps1` for how this happens automatically at
the start of every Claude Code session. `dotnet test v3/Prose.UnitTests` still runs locally the
same way it always has; there is no CI runner and no `master`-push automation of any kind.

---

## Tests

```powershell
dotnet test v3/Prose.UnitTests   # NUnit + bUnit, 185 test files
```

CI is DB-independent by design: `TestDbFactory` backs most tests with in-memory SQLite; the
handful that legitimately need a live SQL connection (`InterfaceRegistrationTests`,
`WorldValidationTests`) skip gracefully when one isn't reachable.

```powershell
npx cypress run     # headless e2e
npx cypress open    # interactive
```

**The Cypress suite (`cypress/e2e/`: `navigation.cy.js`, `book-routes.cy.js`, `ai-panels.cy.js`,
`strand-smoke.cy.js`, `strand-ux.cy.js`) exercises Blazor UI routes that no longer exist** — it
predates Epoch 4 and has not been updated or removed since the UI was deleted. Running it against
the current codebase will fail or hang waiting for a server that is never started.

---

## Patent Disclosures

Ten invention disclosures documenting novel systems in the Prose engine. All are pre-filing
confidential documents; formal claim drafting to be conducted by qualified patent counsel.

Full disclosures (standalone HTML, open in any browser): **[`html/patent-disclosures.htm`](html/patent-disclosures.htm)**

| Reference | Title | Core Novelty |
|---|---|---|
| SS-DISC-001 | **Dynamic Context Memory** | Ephemeral .md files materialized from DB, evicted after N beats without access; five-pass retrieval pipeline with tiered LRU stack |
| SS-DISC-002 | **Structural Blueprint System** | Nine structural dimensions committed before prose via mandatory outlier-seeking; internal-understanding resolution excluded as forbidden value |
| SS-DISC-003 | **Multi-Provider Expert Persona Quorum Review** | Round-robin LLM providers, two-tier ballots, Big Five psychometric shaping, audience segment clustering |
| SS-DISC-004 | **Plant/Payoff Lifecycle Registry** | Three-status lifecycle (planned/seeded/paid-off) with independent transparency certification and three-category orphan audit |
| SS-DISC-005 | **Beat-Mode Classification with Prose Rhythm Assignment** | Priority-ordered keyword quorum (6 modes) followed by fractional-position rhythm assignment (5 modes) with injected prohibition-bearing directives |
| SS-DISC-006 | **Voice Rule Harvest from Editorial History** | Three-source evidence mining (edit diffs + directives + prose) into staged change log with commit-before-mutate application; score-threshold auto-trigger |
| SS-DISC-007 | **Eight-Dimension Emotional Rubric Scoring** | Parallel LLM scoring on 0–4 universal scale; two blocking dimensions; register-adaptive criteria; per-beat curve |
| SS-DISC-008 | **Multi-Provider Continuity Claim Extraction** | Snippet-validated (entity, predicate, object) triples; voter quorum gating; three-outcome upsert (NEW / CONFIRMED / CONTRADICTED) |
| SS-DISC-009 | **Gateway/Sequel Regime Detection** | Auto-detection from predecessor book presence; parallel commandment evaluation enriched with live plant/payoff data; universe-conditional injection |
| SS-DISC-010 | **Deterministic Deprecated-Noun Enforcement** | Universe-scoped rename registry; whole-word boundary scan; chapter-child traversal; zero LLM inference |

---

## Code Style Rules

From `CLAUDE.md` — enforced across the codebase:

| Rule | Detail |
|---|---|
| **Private field naming** | `camelCase` without underscore prefix. `count`, not `_count`. |
| **Data files** | JSON only. No Python scripts, no YAML, no new Markdown files (Codex `docs/*.md` and this README are the sole exceptions). |
| **Host model** | Command-line only as of Epoch 4 — `Prose.Cli` + `Prose.Mcp`. No web host, no MAUI host. |
| **EF Core null-conditional** | `?.`/`?[]` are not allowed inside EF Core expression-tree lambdas (CS8072). Project the scalar before the terminal operator: `g.OrderByDescending(h => h.CreatedAt).Select(h => h.Score).FirstOrDefault()`, not `...FirstOrDefault()?.Score ?? 0`. |
| **QUANTA symbol** | `Φ` is the QUANTA currency symbol — never "phi", never the Greek letter. Precedes the number: `Φ100`. |
| **Iowan Behemoths** | Autonomous machines, not synthetic life. Not alive. |
| **Character heritage** | Default to mixed heritage from unexpected global combinations (Ubiquitous Diaspora). |
| **Versioning** | Whole-number only: `1.0.0`, `2.0.0`, `3.0.0`. No semver-style minor/patch bumps. |
| **Prose entry point** | All beat writing goes through `ProseWriterRouter.WriteAsync`. Never call `BeatGeneratorService` directly from new code. |
| **Canon storage** | Canon facts go into SQL Server. Never write new entity data into `engine_data/*.json` files or Markdown. |
| **CorpoNation spelling** | Conjoined capitals in prose and UI copy (e.g. `MitsuDyne`, `AgroCore`). Code identifiers unchanged. |
| **E.L.F. spelling** | Always `E.L.F.` with periods. Glossed once per book via `GlossaryTerms`, never in-voice ([SS-LAW-20](docs/BIBLE.md#SS-§5)). |
| **Score gates** | Retired (SS-A44). Publish-readiness = Logic Sweep clean at BLOCKER + zero open High/BLOCKER Reader-Proxy QA findings — not a number. |

---

## Status

In active development. **Command-line only** since 2026-08-13 (Epoch 4) — no live website.

- **7 registered universes:** GLMZ, SCRY, NONFICTION, HORROR, EROTICA, GOSPEL, FICTION.
- **DB:** 263 base tables + 168 system-versioned history tables (264 FKs, 263 PKs; live-queried
  2026-08-15 — `docs/schema.md` itself is dated 2026-08-14 and needs a re-run, see
  [Schema reference](#schema-reference)).
- **MCP:** 322 `[McpServerTool]` methods across 40 files under `v3/Prose.Mcp/` (live-counted
  2026-08-15 — `docs/MCP_TOOLS.md` and the per-family breakdown table in
  [MCP Reference](#mcp-reference) predate this jump from 273/43; regenerate via `--export-tools`).
- **CLI:** 265 dispatched `--flag` handlers across 227 handler files (live-counted 2026-08-15).
- **Core services:** 311 directly under `Services/`, 353 including subfolders (live-counted 2026-08-15).
- **Content:** 37 books, 482 chapters, 2 series; 13,648 total beats, 12,015 linked into a live book
  spine via `BeatNodes` (see [Content census](#content-census-2026-08-14-live-query)).
- **Tests:** 189 unit test files (NUnit + bUnit; live-counted 2026-08-15); Cypress suite currently
  orphaned (see [Tests](#tests)).
- **Commit history:** 1,000+ commits since 2026-03-25 (StreetSamurai) → 2026-08-14 (Prose, command-line only).
- **Status index** (`docs/USER_STORIES.md`, via `codex.ps1 digest`): done 172 · partial 8 · planned 27 · cut 4.
- **New since 2026-08-14, not yet reflected anywhere except this document:** the five-point
  book-completeness convergence gate (fact ledger, blast-radius auto-check, loop-until-dry sweep),
  sequential-read tracking (`BookSequentialReads`), and the AutoCorrect nightly self-heal pass
  (currently running in `--dry-run` only) — see
  [Book Completeness](#book-completeness--convergence-gate--self-healing) and
  [Has this book ever actually been read?](#has-this-book-ever-actually-been-read-booksequentialreads).
  A multi-LLM provider expansion (Gemini/DeepSeek/Mistral/Kimi/Perplexity + CLI-shelling
  Codex/Gemini providers) and a provider-neutral Operator tool-calling abstraction
  (`IToolCallingLlm`) for the KDP publish automation also landed — see
  [10. LLM providers & routing](#10-llm-providers--routing) and
  [Architecture at a Glance](#architecture-at-a-glance).

**Known open gaps, discovered while writing this document — not yet fixed:**

1. The Azure deploy pipeline (`.github/workflows/azure-deploy.yml`) still targets the deleted
   `Prose.Codex` project (see [Deployment](#deployment)) — the next push to `master` will fail CI.
2. The Cypress e2e suite exercises deleted Blazor UI routes (see [Tests](#tests)).
3. `docs/BIBLE.md §3` and `CLAUDE.md`'s Code Style section still assert "Web-only project (Blazor
   Server). No MAUI host." — true through Epoch 3, false since Epoch 4's command-line-only pivot.
4. `v3/Prose.Mcp/README.md` still describes the deleted `Prose.Shared`/`Prose.Writer`/`Prose.Codex`
   projects and a stale, pre-273-tool MCP tool-group table. (`v3/README.md` itself was folded into
   this document and reduced to a stub in this pass — no longer a separate source of drift.)
5. `CLAUDE.md`'s "Key schema facts for queries" section still cites `BeatNodes(NodeId, BeatId,
   SortKey, IsEnabled)` — migration `20260813053520_DropBeatNodeIsEnabled` (Epoch 4, same day)
   dropped that column; see [Content census](#content-census-2026-08-14-live-query).
6. `tools/gen-schema.ps1` hardcoded its "Snapshot date" to `2026-06-28` regardless of when it
   actually ran — fixed in this pass (now `Get-Date`), so `docs/schema.md` no longer lies about
   its own freshness.
7. `docs/PROVIDERS.md` predates the multi-LLM provider expansion — still only documents the
   Anthropic/OpenAI-only/both split, with no mention of Gemini/DeepSeek/Mistral/Kimi/Perplexity,
   the CLI-shelling providers, or `LlmCallHistories` (see
   [10. LLM providers & routing](#10-llm-providers--routing)).
8. `--set-llm-provider` only accepts `claude-api`/`claude-team` — it was never extended to the
   new providers, so switching to Gemini/DeepSeek/Mistral/Kimi/Perplexity/a CLI-shelling provider
   as the active default requires editing `ActiveLlmProviderChain` directly; no CLI or MCP surface
   does it today.
9. `prose --archive-book --reason "<text>"` (and the `BookArchiveService.ArchiveAsync` call
   behind it) does not validate or truncate the reason string before insert, while
   `ArchivedBooks.Reason` is `nvarchar(40)` — a reason over 40 characters fails at
   `SaveChangesAsync` with a raw SQL truncation error instead of a clean message. Not a risk for
   the nightly AutoCorrect pass itself (it always passes the fixed 20-character
   `"autocorrect-pre-run"`), only for manual invocations with a long custom reason.
10. The AutoCorrect nightly pass (`ProseAutoCorrectNightly` scheduled task) is live but still runs
    with `--dry-run` — it detects and logs, but does not yet apply, its three whitelisted auto-fix
    categories in production. Promoting it to live writes is a config edit
    (`scripts/run-autocorrect-nightly.ps1`), not a code change, once a few mornings' `--dry-run`
    output has been reviewed via `prose --morning-report`.
11. `BookSequentialReads` was created via a raw T-SQL script
    (`v3/Prose.Core/Data/Sql/create_book_sequential_reads_20260815.sql`, applied directly to the
    live dev DB at the time) rather than an EF Core migration, and is not registered in
    `SqlSeedService.Seeds` — a fresh clone/environment is currently missing this table until it's
    registered as a proper `--seed` entry and run via `prose --seed <name>`. **Nothing reaches the
    database except through Prose.Hub — reads AND writes, no exceptions (HARD, absolute,
    2026-08-22)** — do not run this script by hand with `sqlcmd`; register it first. No MCP tool
    exists for sequential-read tracking, AutoCorrect, or blast-radius re-checks yet — all three are
    CLI-only.

---

## About This Document

This README is the one Markdown file the repo's "no Markdown except README" rule allows outside
`docs/` — and it is not just a file on disk. `MarkdownFileService.DiscoverFiles()` special-cases
the project root the same way it special-cases `CLAUDE.md`, so `README.md` is synced into the
**`MarkdownFiles`** database table (system-versioned — every past version is recoverable by
timestamp) whenever markdown sync runs:

```powershell
prose --sync-markdown
```

MCP equivalents: `sync_markdown_files` to push the current file into the DB, `recall_markdown_files`
to read it back without touching disk, `restore_markdown_file` to roll back to an earlier synced
version if this file is ever lost or corrupted on disk. This is the same mechanism already used for
`docs/CRAFT.md`, `docs/USER_STORIES.md`, and every other hand-authored Codex doc — README.md gets
no special-case code path beyond the one that already existed for it.

**A standalone, self-contained HTML build** is generated from this file by `tools/build-readme.js`
(dependency: `marked`, `npm install` once):

```powershell
npm run docs                          # writes README.htm — same directory as this file
# or directly:
node --use-system-ca tools/build-readme.js
```

The output is a single `.htm` file with all CSS/JS inlined (dark theme, sidebar table of contents
built from this file's `##`/`###` headings, scroll-spy, copy-to-clipboard on code blocks) —
opens directly in any browser, no server required, and is both human-readable and easy for an LLM
to parse back into structured sections via its heading IDs.

**To regenerate this document after the codebase changes:**

1. Re-run the counting commands used to build it (MCP: `dotnet run --project v3/Prose.Mcp --
   export-tools docs/MCP_TOOLS.md`; schema: `powershell -File tools/gen-schema.ps1`; CLI/service
   counts: grep `Program.cs` and `Services/` as shown in each section above).
2. Update the affected sections and the [Status](#status) counts.
3. `npm run docs` to rebuild `README.htm`.
4. `prose --sync-markdown` to push the new content into the `MarkdownFiles` DB table.
