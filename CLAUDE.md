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

Only use `dotnet run --project v3/StreetSamurai.Blazor -- <args>` when the CLI's business logic is actually needed (write operations, generation, publish, review). Never use it just to answer a lookup question.

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

- **`docs/BIBLE.md`** (L0) — what StreetSamurai IS / is NOT, the architecture canon, and **the Laws**
  (engine invariants + Bushido Coda narrative continuity). When in doubt, this wins. It supersedes
  the old `ARCHITECTURE.md` (now a pointer). The Laws inherit `D:/Projects/MindAttic/MindAttic.HouseRules.md`.
- **`docs/AMENDMENTS.md`** (L1) — append-only change log; **an amendment wins over the bible**.
- **`docs/USER_STORIES.md`** (L2) — test-cited stories + backlog + audit log. Every `✅` names its
  verifying test or recorded evidence.
- **`docs/rfc/`** — design notes that graduate into BIBLE + stories.
- **`docs/data/`** (L5) — canon-as-data: JSON Schemas + the master entity-identity table for the
  `engine_data/*.json` seed corpus. **Live canon is the SQL DB, not files** (SS-LAW-1).
- **`docs/BIBLE.digest.md`** — GENERATED by `tools/codex.ps1 digest`; never hand-edit. The
  SessionStart hook (`.claude/hooks/inject-digest.ps1`) injects it as authoritative context.

Working rules:
- A fact lives in **exactly one layer**; cite it by its stable `{#SS-...}` id, never by line number.
- Update the Bible/stories status in the **same change** that moves a goal; "done" means a test or
  build proves it.
- After editing any `docs/*` canon file, run `powershell -File tools/codex.ps1 digest` then
  `powershell -File tools/codex.ps1 doctor` — doctor must pass. (`pwsh` is not installed; use `powershell`.)
- The repo rule "no Markdown except README" is amended for the Codex `docs/*.md` set (see SS-A1);
  data files stay JSON.

## New Story Workflow (mandatory — see [docs/BIBLE.md §10](docs/BIBLE.md#SS-§10))

**Every new story/book follows this sequence without exception:**

1. **Docs first** — append `SS-AN` to `docs/AMENDMENTS.md` if new world facts; add story entry to
   `docs/USER_STORIES.md`; run `codex doctor`.
2. **Entities** — seed every named character, CorpoNation, place, or weapon into the DB via CLI or
   MCP **before any prose is generated**.
3. **Story structure (SS-A43)** — create a **StoryNode** (MCP `create_story` / CLI `--create-story`)
   + **ChapterNode** children (MCP `create_chapter`, parent required). Authorial spine (14-beat
   outline) = the story node's `seed` text.
4. **Prose** — Sonnet draft → Opus polish → reflow → dual review (see below) → scan entity mentions.
5. **Export** — `--publish-docx`; flip USER_STORIES to ✅ with evidence.

Never write prose before steps 1 and 2 are complete.

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

## Multi-Node Story Review (mandatory after every node — see memory: feedback_story_accretion)

When writing a multi-node story (book, series, alternating POV), each completed node triggers a **mandatory dual review** before the next node begins. No exceptions.

**A. Standalone review** — Score ≥82%. If below, fix before continuing.

**B. Cumulative prefix review** — Read all nodes Ch1–N in order. Story score must trend toward and hold ≥85%. If this node drops the cumulative, investigate before proceeding.

Use both reviews to diagnose and act:
- **Prose problems** — voice drift, flat dialogue, missing sensory texture.
- **Pacing problems** — beats that over- or under-stay; repeated emotional register without escalation.
- **Contradictions** — entity state, wound ledger, timeline, character voice.
- **Underperforming beats** — expand if underdeveloped, contract if dead weight. Never pad; never cut a beat that's pulling the story forward.

| Measure | Target |
|---|---|
| Per-node standalone | ≥82% |
| Cumulative story (all nodes in reading order) | ≥85% |
