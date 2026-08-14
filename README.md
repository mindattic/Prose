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
- [Quality Verification — Logic Sweeps, Not Votes](#quality-verification--logic-sweeps-not-votes)
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
entire user-facing surface. See [Deployment](#deployment) for what this means for the still-wired
Azure App Service pipeline, and [Tests](#tests) for what it means for the Cypress suite — both
predate this epoch and reference the deleted UI; neither has been reconciled yet.

---

## Architecture at a Glance

```
prose CLI  (dotnet run --project v3/Prose.Cli -- <args>)          Prose.Mcp  (MCP server)
        │  263 dispatched --flag handlers, v3/Prose.Cli/Program.cs      │  273 [McpServerTool] methods,
        │  + v3/Prose.Cli/Cli/ (226 handler files)                      │  43 tool families, v3/Prose.Mcp/Tools*.cs
        └──────────────────────────┬───────────────────────────────────┘
                                    ▼
                    Core services  (v3/Prose.Core/)
                    │  309 services directly under Services/, 351 including
                    │  Services/Audit, /CoverImage, /Local, /Operator (KDP-automation
                    │  tool-calling only), /VideoGen — see The Subsystems below
                    │  EF Core → SQL Server (LocalDB in dev, Azure SQL in prod)
                    ▼
                    Database  (SQL Server)
                    │  261 base tables + 168 system-versioned _History tables
                    │  (262 FKs, 278 PKs) — see docs/schema.md
                    │  Vector embeddings · directional Edges graph · EntityStateEvents ledger
                    │  ContinuityClaims · Findings inbox · MarkdownFiles (this doc lives here too)
```

There is **no web application** in this diagram — see [Epoch 4](#epoch-4--command-line-only-2026-08-13--present).
Two satellite projects sit beside the CLI/MCP core, each solving a narrow problem the main engine
doesn't:

| Project | Type | Purpose |
|---|---|---|
| `Prose.KdpPublish` | WPF + WebView2 desktop app | Automates the Amazon KDP publishing workflow (browser automation via an internal tool-calling "Operator" — `AnthropicToolClient`/`OpenAiToolCallingLlm` + `KdpTools/` — unrelated to the deleted prose-writing Operator from Epoch 2) |
| `Prose.LlmCli` (`prose-llm`) | Standalone console app, **no** `Prose.Core` reference | Generic multi-provider LLM CLI escape hatch — works even if `Prose.Core`/the DB/EF migrations are broken, since its only dependency is `MindAttic.Legion` + `MindAttic.Vault`. The last-resort fallback tier in `LlmRouter`'s chain, and a manual terminal tool during an outage |

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
4. `prose --book-audit --slug <slug>` — gateway/sequel commandments once the book is complete.
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

## Quality Verification — Logic Sweeps, Not Votes

**The 0–100 score gates (≥82% standalone / ≥85% cumulative) are retired** (author ruling,
2026-08-03: *"remove scores; they mean nothing"* — SS-A44). Nothing writes new `Node.Score` values
except an explicitly requested legacy panel run. Publish-readiness is now: **Logic Sweep clean at
BLOCKER + zero open High/BLOCKER Reader-Proxy QA findings.**

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

## CLI Reference

The canonical invocation `prose` expands to `dotnet run --project v3/Prose.Cli -- <args>`. **263
distinct `--flag` handlers** are dispatched from `v3/Prose.Cli/Program.cs`, backed by **226 handler
files** under `v3/Prose.Cli/Cli/`. What follows is a categorized tour, not an exhaustive
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
| `prose --logic-sweep --slug <slug>` | The default QA methodology — six-dimension continuity sweep, no votes |
| `prose --reader-qa --slug <slug> [--gripe-pass]` | Reader-Proxy QA — comprehension, craft checklist, gripe jury |
| `prose --craft-checklist --slug <slug>` | Hash-gated binary craft/delight checklist |
| `prose --duel --slug <slug>` | Cross-provider pairwise splice duels |
| `prose --diagnose-book --slug <slug>` | 12-check structural pre-flight, cheap |
| `prose --audit-book [--full] [--deep] [--model haiku]` | Repeatable full-QA orchestrator — every audit tool in one pass |
| `prose --book-audit --slug <slug>` | Gateway/sequel commandment audit |
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
| `prose --graph-health` | Entity relationship graph integrity check |
| `prose --coordinate` / `--coverage` / `--backfill-coverage` | Cross-service coverage bookkeeping (non-destructive logging for existing beats) |
| `prose --fix-cross-universe-contamination` | Repairs cross-universe roster/POV leaks (the fail-closed scoping fix from Epoch 4) |
| `prose --review-book` / `--review-node` / `--review-entity` | **Quarantined legacy vote panels** — SS-A44-gated, explicit request only |

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
tool-calling (`AnthropicToolClient`, `KdpToolRegistry`, `KdpTools/`) consumed only by
`Prose.KdpPublish`. Agentic authoring today means Claude — via this MCP server — calling the
canon directly, with the human approving canon and voice changes rather than authoring them.

---

## Database

### Connection

```
Server=(localdb)\MSSQLLocalDB;Database=Prose;Trusted_Connection=True;TrustServerCertificate=True;
```

Windows Authentication — no `-U`/`-P` needed. Same as `appsettings.json` → `ConnectionStrings:Prose`.

### Direct queries (read-only lookups)

For book lists, scores, entity counts, and other read-only lookups, query the DB directly:

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -d Prose -Q "SELECT Name, Score FROM Nodes WHERE IsCanon = 1 ORDER BY Score DESC"
```

Only use `dotnet run --project v3/Prose.Cli -- <args>` when the CLI's business logic is actually
needed (write operations, generation, publish, audit). Never use it just to answer a lookup
question.

### Schema reference

The DB schema is documented at [docs/schema.md](docs/schema.md) — **261 base tables** (262 FKs,
278 PKs) plus **168 system-versioned `_History` tables**, generated by `tools/gen-schema.ps1`.
Regenerate after any migration.

### Key tables

| Table | Role |
|---|---|
| `Entities` | Universal spine — one row per entity: `Id, Name, Slug, EntityType, Description, IsActive, UniverseId` |
| `Nodes` / `Nodes_History` | System-versioned typed content tree (series/book/chapter) + score |
| `Beats` / `Beats_History` | System-versioned prose beats — every edit rewindable |
| `BeatNodes` | Joins Beats to Nodes (`NodeId, BeatId, SortKey, IsEnabled`) — the current book↔beat relationship; supersedes the retired `Beats.NodeId` |
| `EntityEmbeddings` | 1536-d vectors (OpenAI `text-embedding-3-small`); cosine via `VECTOR_DISTANCE` |
| `Edges` | Typed directed relationships (`carries`, `wields`, `member_of`, `located_at`, …) with validity windows |
| `EntityStateEvents` | Append-only in-world story-time ledger: `(entity, predicate, value, beatId, storyTime)` |
| `ContinuityClaims` | Extracted `(entity, predicate, object)` claims from prose |
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
Seven are currently registered:

| Universe | Slug | Genre | World-facts doc | Craft-layer doc |
|---|---|---|---|---|
| **GLMZ** | `glmz` | Cyberpunk, 2226, the flagship (*Bushido Coda*) | [docs/BIBLE.md](docs/BIBLE.md) / [docs/WORLD.md](docs/WORLD.md) | [docs/GLMZ.md](docs/GLMZ.md) |
| **SCRY** | `scry` | Fantasy/steampunk (The Entos / The Caul) | [docs/universes/ENTOS.md](docs/universes/ENTOS.md) | [docs/SCRY.md](docs/SCRY.md) |
| **NONFICTION** | `nonfiction` | Citation-grounded nonfiction (formerly GSPL, then SOURCE) | — | [docs/NONFICTION.md](docs/NONFICTION.md) |
| **HORROR** | `horror` | Ambiguous/analog horror | — | [docs/HORROR.md](docs/HORROR.md) |
| **EROTICA** | `erotica` | Sixth universe added 2026-08-04 | — | — |
| **GOSPEL** | `gospel` | Split out from NONFICTION; New Testament claims-cataloguing campaign | [docs/gospel/](docs/gospel/) | — |
| **FICTION** | `fiction` | General literary fiction (formerly EPIC) | — | — |

Universe scoping is **fail-closed** as of Epoch 4: an unset or invalid universe scope blocks
generation rather than silently defaulting to "every universe" — the fix that closed a
cross-universe content-leak incident (`--fix-cross-universe-contamination` repairs any residue).
Switch scope with `prose --universe <id>` (CLI, per-process) or MCP `switch_universe`
(per-session) — two processes or sessions can target different universes simultaneously.

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
| `WorldGraphService` | In-memory adjacency graph over all entities/edges | No |
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
| `MultiLlmService` | Multi-provider fan-out via `MindAttic.Legion` (Claude, OpenAI, Gemini, DeepSeek, Mistral, Grok, Groq, Together, OpenRouter, Fireworks, Cohere) |
| `LlmRouter` | Routes each request to the right provider/tier from `ActionConfig` |
| `AssignTiersService` | Assigns Haiku/Sonnet/Opus class to actions per settings |

See [docs/PROVIDERS.md](docs/PROVIDERS.md) for the checked-in table of which services depend on
which external provider (Anthropic-only, OpenAI-only, or both).

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
| LLM providers | `PROSE_CLAUDE_API_KEY`, `PROSE_OPENAI_API_KEY`, `PROSE_GEMINI_API_KEY`, `PROSE_DEEPSEEK_API_KEY`, `PROSE_MISTRAL_API_KEY`, `PROSE_GROK_API_KEY`, `PROSE_GROQ_API_KEY`, `PROSE_TOGETHER_API_KEY`, `PROSE_OPENROUTER_API_KEY`, `PROSE_FIREWORKS_API_KEY`, `PROSE_COHERE_API_KEY` |
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

---

## Database Migrations

Raw T-SQL files live under `v3/Prose.Core/Data/Sql/`. Most are historical, pre-EF-migration schema
deltas superseded by `v3/Prose.Core/Migrations/` — run individually via `prose --seed <name>`
(see `SqlSeedService.Seeds` for registered names) only if a specific older environment needs them.
A few small data-only scripts (e.g. universe seed inserts) are not yet registered anywhere and
would need to be applied by hand with `sqlcmd` if a fresh DB is missing rows you expect.

```powershell
dotnet run --project v3/Prose.Cli -- --migrate-sql --schema
```

---

## Deployment

**This section documents the current, as-found state — including a break introduced by Epoch 4
that has not yet been reconciled.**

`.github/workflows/azure-deploy.yml` runs a four-stage pipeline on every push to `master`: build →
test → migrate → deploy, targeting an Azure App Service named `mindattic-prose` (the `prose`
hostname was already taken on `*.azurewebsites.net`) against Azure SQL Database with OIDC/managed-identity
auth — no passwords anywhere. Full provisioning guide: [`infra/README.md`](infra/README.md).

**The build/publish/deploy steps still target `v3/Prose.Codex/Prose.Codex.csproj` — a project
Epoch 4 deleted.** The next push to `master` will fail at `dotnet restore
v3/Prose.Codex/Prose.Codex.csproj`. The `migrate` job (schema bootstrap against Azure SQL via
`Prose.Cli --migrate-sql --schema`) is unaffected and would still succeed on its own. This is a
known, currently-live gap — not yet decided whether the pipeline should deploy the MCP server,
deploy nothing web-facing and become build+test+migrate only, or be retired in favor of a
different distribution model now that there's no browser surface to serve.

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
- **DB:** 261 base tables + 168 system-versioned history tables (`docs/schema.md`, 2026-08-14).
- **MCP:** 273 tools across 43 families (`docs/MCP_TOOLS.md`, 2026-08-14).
- **CLI:** 263 dispatched `--flag` handlers across 226 handler files.
- **Core services:** 309 directly under `Services/`, 351 including subfolders.
- **Tests:** 185 unit test files (NUnit + bUnit); Cypress suite currently orphaned (see [Tests](#tests)).
- **Commit history:** 1,000+ commits since 2026-03-25 (StreetSamurai) → 2026-08-14 (Prose, command-line only).
- **Status index** (`docs/USER_STORIES.md`, via `codex.ps1 digest`): done 172 · partial 8 · planned 27 · cut 4.

**Known open gaps, discovered while writing this document — not yet fixed:**

1. The Azure deploy pipeline (`.github/workflows/azure-deploy.yml`) still targets the deleted
   `Prose.Codex` project (see [Deployment](#deployment)) — the next push to `master` will fail CI.
2. The Cypress e2e suite exercises deleted Blazor UI routes (see [Tests](#tests)).
3. `docs/BIBLE.md §3` and `CLAUDE.md`'s Code Style section still assert "Web-only project (Blazor
   Server). No MAUI host." — true through Epoch 3, false since Epoch 4's command-line-only pivot.
4. `v3/README.md` and `v3/Prose.Mcp/README.md` still describe the deleted `Prose.Shared`,
   `Prose.Writer`, `Prose.Codex` projects and a stale, pre-273-tool MCP tool-group table.

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
