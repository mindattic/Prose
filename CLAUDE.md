# StreetSamurai Project Rules

## Conversation
- A bare "do" / "do it" / "yes" from the user means "continue", "keep going", "proceed". Resume the current task without asking for clarification.

## Rate Limit & Context Protection

### Rate Limit (billing — HARD STOP at 96%)
See global rules in ~/.claude/CLAUDE.md. The rate-limit-monitor skill enforces:
- Warn every 5% starting at 80%, every 1% starting at 91%
- **Hard stop at 96%** — queue pending tasks to ~/.claude/rl-queue.json, write handoff to ~/.claude/rl-handoff.md
- Every exceeded limit = ~$30 charge on the credit card

### Context Window (conversation — HARD STOP at 96%)
- When approaching 96% context usage, STOP immediately
- Create a task list of all pending/in-progress work
- Write a handoff summary to memory so the next session can resume seamlessly
- Tell the user to take a break and come back after cooldown

## Database Access

For **read-only lookups** (node lists, scores, entity counts, etc.), query the local DB directly — returns in under a second:

```
sqlcmd -S "(localdb)\MSSQLLocalDB" -d StreetSamurai -Q "<query>"
```

- Auth: Windows Authentication (no `-U`/`-P` needed)
- Same server as `appsettings.json` → `ConnectionStrings.StreetSamurai`

Only use `dotnet run --project v3/StreetSamurai.Cli -- <args>` when the CLI's business logic is actually needed (write operations, generation, publish, review). Never use it just to answer a lookup question.

**HARD RULE — no direct SQL deletes (SS-A37, tables renamed by SS-A43):** Never execute `DELETE FROM Nodes`, `DELETE FROM Beats`, or `DELETE FROM NodeBeats` as raw sqlcmd statements. These tables are system-versioned temporal tables — deleting via raw SQL bypasses all application guards and is unrecoverable without a point-in-time restore. Any story/beat removal must go through the CLI (`ss --beat delete`). If a story genuinely needs to be deleted, get explicit user confirmation naming the story by title and slug before touching the DB.

## Code Style
- Do NOT use underscore-prefixed variables (e.g., `_myField`). Use `camelCase` for private fields without the underscore prefix.
- JSON only for all data files. No Python scripts, no YAML, no Markdown files except README.
- Web-only project (Blazor Server). No MAUI host.
- The null-conditional operator `?.` (and `?[]`) is **not allowed inside an EF Core expression-tree lambda** (anything that becomes a SQL query — `Select`/`Where`/`GroupBy` projections, etc.). It fails to compile (`CS8072`). Project the scalar **before** the terminal operator instead: write `g.OrderByDescending(h => h.RecordedAt).Select(h => h.MeanScore).FirstOrDefault()`, not `g.OrderByDescending(...).FirstOrDefault()?.MeanScore ?? 0`.

## World Rules
- The symbol Φ is the QUANTA currency symbol. It is NEVER the Greek letter phi.
- Iowan Behemoths are autonomous machines, NOT synthetic life. They are not alive.
- Default to mixed heritage from unexpected global combinations (Ubiquitous Diaspora).

## Per-Node Documentation (SS-A11)

Every story node with active prose has its own standalone bible at `docs/nodes/<CODE>.md`.

**When working on a specific node:** read `docs/nodes/<CODE>.md` before generating prose.
Do not rely on BIBLE.md alone for story-specific rules — it has engine laws, not story arc.

| Location | Contains |
|---|---|
| `docs/BIBLE.md` | Universe laws, architecture, engine invariants — no per-story arc |
| `docs/nodes/<CODE>.md` | Story arc, beat spine, character rules, locks, user stories |
| `docs/books/<name>.md` | Legacy long-form book spines (BCODA; maintained in place) |
| `docs/USER_STORIES.md` | Epic index + acceptance criteria |

Node bibles are **loaded on demand**, not injected at session start. Load only what you need.

**Existing node bibles:**
- `docs/nodes/PNHL.md` — Pinhole / PNHL (Pixel origin story, GLMZ; formerly TDIU / The Door Is Unlocked)
- `docs/nodes/BCODA.md` — Bushido Coda flagship novel (GLMZ)
- `docs/nodes/ATTE.md` — Attendance / Yemina Fola investigation (GLMZ)
- `docs/nodes/VATD.md` — Vultures at the Door / Thomas & Levin (GLMZ)
- `docs/nodes/DWIACE.md` — Death Whispers in a Cat's Ear / Rennick Investigations (GLMZ)
- `docs/nodes/SPRW.md` — Sparrow / Elias Macias & the orbital mystery (GLMZ)
- `docs/nodes/MNEMO.md` — Mnemosync / Amara & Seto (GLMZ, in progress; formerly ULC, redesigned SS-A14)
- `docs/nodes/TEST.md` — Testament / Bear court-martial (GLMZ)
- `docs/nodes/GIW.md` — Grafted Into War / M-101/Soren (Fantasy)
- `docs/nodes/MxG.md` — Magenta & Gunmetal / GLMZ run (GLMZ, planned; Shadowrun-style heist → True Lies finale)
- `docs/books/bushido-coda-strands-bible.md` — BCODA (legacy long-form; superseded by BCODA.md above)

## Codex (how to work with the canon)

The project follows the **MindAttic Codex** documentation standard. The source of truth lives under
`docs/`:

- **`docs/BIBLE.md`** (L0) — what StreetSamurai IS / is NOT, the architecture canon, **the Laws**,
  and all world-building facts (engine invariants + GLMZ universe canon). **This is the single
  authoritative source.** When in doubt, this wins. It supersedes the old `ARCHITECTURE.md`
  (now a pointer). The Laws inherit `D:/Projects/MindAttic/MindAttic.HouseRules.md`.
- **`docs/nodes/<CODE>.md`** (L0, per-story) — story arc, beat spine, character rules, locks.
  **The single source of truth for that StoryNode.** All story-specific facts live here.
- **`docs/USER_STORIES.md`** (L2) — test-cited stories + backlog + audit log. Every `✅` names its
  verifying test or recorded evidence.
- **`docs/rfc/`** — design notes that graduate into BIBLE.md or story bibles.
- **`docs/data/`** (L5) — canon-as-data: JSON Schemas + the master entity-identity table for the
  `engine_data/*.json` seed corpus. **Live canon is the SQL DB, not files** (SS-LAW-1).
- **`docs/BIBLE.digest.md`** — GENERATED by `tools/codex.ps1 digest`; never hand-edit. The
  SessionStart hook (`.claude/hooks/inject-digest.ps1`) injects it as authoritative context.

**`docs/AMENDMENTS.md` is RETIRED (2026-07-04).** All amendments have been merged into their
canonical destinations. Do not append to it. Do not reference it.

Working rules:
- **Canon changes go DIRECTLY into the authoritative file** — `docs/BIBLE.md` for world/engine facts,
  `docs/nodes/<CODE>.md` for story-specific facts. There is no amendment layer. There is no "L1 wins over L0."
- A fact lives in **exactly one file**; cite it by its stable `{#SS-...}` id, never by line number.
- Update the Bible/stories status in the **same change** that moves a goal; "done" means a test or
  build proves it.
- After editing any `docs/*` canon file, run `powershell -File tools/codex.ps1 digest` then
  `powershell -File tools/codex.ps1 doctor` — doctor must pass. (`pwsh` is not installed; use `powershell`.)
- The repo rule "no Markdown except README" is amended for the Codex `docs/*.md` set (see SS-A1);
  data files stay JSON.

## New Story Workflow (mandatory — see [docs/BIBLE.md §10](docs/BIBLE.md#SS-§10))

**Every new story/book follows this sequence without exception:**

1. **Docs first** — if new world facts, write them DIRECTLY into `docs/BIBLE.md` (engine/world-level)
   or the relevant `docs/nodes/<CODE>.md` (story-specific). Add story entry to
   `docs/USER_STORIES.md`; run `codex doctor`. Do NOT use `docs/AMENDMENTS.md` — it is retired.
2. **Entities** — seed every named character, CorpoNation, place, or weapon into the DB via CLI or
   MCP **before any prose is generated**.
3. **Story structure (SS-A43)** — create a **StoryNode** (MCP `create_story` / CLI `--create-story`)
   + **ChapterNode** children (MCP `create_chapter`, parent required). Authorial spine (14-beat
   outline) = the story node's `seed` text.
4. **Structural blueprint (StoryScope countermeasures)** — after the bible/spine exists, run
   `ss --generate-blueprint --slug <slug>` (MCP `generate_structural_blueprint`). This commits the
   structural anti-tell decisions BEFORE prose: thematically-parallel subplot + carrier beats,
   temporal scheme, resolution mode (never internal-understanding), moral polarity (ambivalent
   default), per-beat escalation curve, event-type + revelation-mode palette, optional form device,
   ending style (avalanche, no epilogue), 3-5 intertextual anchors from the entity DB. Mirror the
   decisions into a `## Structural Blueprint` section of `docs/nodes/<CODE>.md`.
5. **Prose** — Sonnet draft → Opus polish → reflow → logic sweep (see Quality Verification SOP below) → scan entity mentions.
6. **Export** — `--publish-docx`; flip USER_STORIES to ✅ with evidence.

Never write prose before steps 1, 2, and 4 are complete.

## Prose Engine Services (use all of these — see SS-A16)

The beat-generation pipeline has several layers. Use **`ProseWriterRouter`** as the entry point
for all prose writing — it coordinates all the services below and logs coverage.

### Entry points
| Path | When to use |
|---|---|
| `ProseWriterRouter.WriteAsync(context, beatId, beatIndex, totalBeats)` | All beat writing from UI + CLI |
| `CombatSceneWriter.WriteCombatSceneAsync(request)` | Explicit multi-exchange combat setpiece (numExchanges > 1, full loadout tracking) |
| `BeatGeneratorService.GenerateBeatAsync(context)` | Legacy path — direct generation without coverage logging |

### Context enrichment chain (all wired inside ProseWriterRouter)
| Service | What it injects | Activation |
|---|---|---|
| `BeatModeDetector` | Classifies beat as Combat/Narrative/EmotionalClimax/Dialogue/Transition/Revelation | Keyword scan on BeatGoal |
| `PacingService` | BREATHE/FLOW/TIGHTEN/STRIKE/SETTLE prose rhythm | Position + BeatGoal keywords; Combat forces STRIKE |
| `StoryMethodologyService` | Save the Cat structural role (Opening Image → Final Image) + Scene-Sequel type | Position in story |
| `PlantPayoffService` | Active plant/payoff pairs for the story | `BeatContext.NodeId != Guid.Empty` |
| `StoryAuditService` | Gateway or Sequel commandments (7 each, auto-detected from `PreviousNodeId`) | `BeatContext.NodeId != Guid.Empty` |
| `CombatProseGuidance` | Verbs-first, fragment sentences, no emotion-naming, dissociated observer | `BeatMode.Combat` |
| `StructuralBlueprintService` | Per-beat StoryScope anti-tell slice: subplot carrier, anachrony cut, escalation floor, event type, ending/resolution mode + STORYSCOPE audit-finding loop-back | Node has a blueprint (`ss --generate-blueprint`) |

### Coverage monitoring
```
ss --workflow-status --slug <slug>    # per-story service coverage matrix + gaps
ss --workflow-status --all            # global utilization across all stories
```
MCP: `workflow_status`, `workflow_status_global`, `workflow_beat_modes`

### Beat writing workflow
1. Assemble `BeatContext` (XRayContext via SceneContextAssembler, NodeId always set)
2. Call `ProseWriterRouter.WriteAsync(context, beatId, beatIndex, totalBeats)` — NOT BeatGeneratorService directly
3. After writing, run `ss --examine-emotion --slug <slug>` to score emotional dimensions
4. After enough beats scored, run `ss --update-register-exemplars --slug <slug>` to update the voice register
5. After story complete, run `ss --story-audit --slug <slug>` to audit gateway/sequel commandments
6. After story complete, run `ss --plant-audit --slug <slug>` to check for orphaned plants
7. After story complete, run `ss --storyscope-audit --slug <slug>` to verify the structural
   anti-tells held (escalation monotonic, event types varied, no moral gloss, no epilogue,
   subplot executed). BLOCKER findings fix per logic-sweep minimal-splice rules, then re-audit.

## Quality Verification SOP — Logic Sweeps, NOT Votes (LAW: SS-A44)

**Default QA for any story that changes or needs validation is a LOGIC & CONTINUITY SWEEP,
not a review panel and not a Legion vote.** Panels and votes are too expensive — run them
ONLY when the user explicitly asks for a vote/review/score in that conversation. The engine
enforces this (voting gate, default OFF; explicit `--allow-votes` / `allowVotes:true` only).

**Canonical methodology: [docs/LOGIC.md](docs/LOGIC.md). Invocable runbook: `/logic-sweep [slug ...]`.**

**The logic sweep:** agents read the story end-to-end (enabled beats only:
`NodeBeats.IsEnabled=1 ORDER BY NodeBeats.SortKey`) and audit six dimensions:
1. **Causality chain** — every event has an established cause, every decision a motivation,
   every capability an on-page origin.
2. **Knowledge states** — who knows what, when they learned it; nobody acts on knowledge
   they don't have.
3. **Timeline** — reconstruct the story clock; no impossibilities.
4. **Plant/payoff ledger** — two-way: every plant pays, every payoff was planted.
5. **Orphan references** — nothing references removed/disabled/merged content.
6. **Bible agreement** — prose and `docs/nodes/<CODE>.md` tell the same story; fix one in
   the same change.

Reports land in `audit-outlines-<date>/logic/`; findings are triaged
**BLOCKER / MODERATE / MINOR** and fixed with minimal splices. Fix what a finding names;
if you can't name the failure, leave the beat alone.

The old dual-review machinery (standalone ≥82 / cumulative ≥85, `--review-node` panels,
Legion votes) still exists — **on explicit user request only**.
