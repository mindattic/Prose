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

## Prose Content — Graphic Adult Content

Adult content in this project is **graphic adult content**: real adult situations rendered with real adult reactions. This is NOT ultraviolence (gratuitous gore for spectacle's sake) and NOT pornography (arousal-focused titillation).

What it means in practice:
- **Violence**: describe what the body actually experiences — the broken bottle dragged through a face, the weight of a corpse, bone under pressure. Physical consequence, not spectacle.
- **Sex**: the body's involuntary responses — the shaking, the orgasmic contractions, the specific physical sensation. Not euphemism, not fade-to-black unless the scene demands it.
- **Extreme affect**: the body does what it does under adrenaline and extremity — the vibrating rat-tat-tat of a heavy machine gun that gives someone a hard-on. Write it.
- The frame is always **literary authenticity**: what a person actually feels, grounded in physical reality.

**Hard limits — absolutely non-negotiable:**
- No sexual content involving minors. No exceptions.
- No sexual content involving animals. No exceptions.

## World Rules
- The symbol Φ is the QUANTA currency symbol. It is NEVER the Greek letter phi. Φ PRECEDES numbers like a dollar sign: Φ100, not 100Φ. Terminology: "quanta", "Q", or "Qs". Physical medium: credstick only — no coins, no bills.
- Iowan Behemoths are autonomous machines, NOT synthetic life. They are not alive.
- Default to mixed heritage from unexpected global combinations (Ubiquitous Diaspora).

## Per-Node Documentation (SS-A11)

Every story node with active prose has a **unified Story Context Document** stored in
`Nodes.NodeBible` (DB) and mirrored to `docs/nodes/<CODE>.md` (generated read-only file).

**NEVER hand-edit `docs/nodes/<CODE>.md`.** It is a generated artifact — edits are overwritten
the next time `generate_node_doc` runs.

| Location | Contains | How to edit |
|---|---|---|
| `docs/BIBLE.md` | Engine invariants + **GLMZ** universe canon — no per-story arc | Hand-edit directly |
| `docs/WORLD.md` | **GLMZ** world master: city mechanics, cast rules, combat, prose voice | Hand-edit directly |
| `docs/FRANCHISE.md` | **GLMZ** franchise & IP bible — commercial positioning | Hand-edit directly |
| `docs/universes/CAUL.md` | **Fantasy/Caul** universe canon | Hand-edit directly |
| `Nodes.NodeBible` (DB) | **The single source of truth for that story** — arc, characters, voice, locks, blueprint, beat spine | `set_story_bible` MCP (hand-authored sections) |
| `docs/nodes/<CODE>.md` | Generated mirror of `Nodes.NodeBible` — never edit this file | Re-run `generate_node_doc` to refresh |
| `docs/books/<name>.md` | Legacy long-form book spines (BCODA; maintained in place) | Hand-edit directly |
| `docs/USER_STORIES.md` | Epic index + acceptance criteria | Hand-edit directly |

**Workflow:**

1. **Before editing a story** — call `generate_node_doc` MCP (or `ss --generate-node-doc --slug X`)
   to refresh the file from DB. The generated file is what DocContextService injects into prose
   prompts; a stale file means stale context.
2. **To update arc, characters, voice register, or narrative locks** — call `set_story_bible` MCP
   with updated hand-authored markdown. Then re-run `generate_node_doc` to regenerate the file.
3. **Blueprint and beat spine sections are always generated** — edit their sources:
   `ss --generate-blueprint` for the blueprint, MCP beat tools for beat titles/goals.
4. **After any `generate_node_doc` run** — run `ss --sync-markdown` to sync the updated file to the
   `MarkdownFiles` table so DocContextService picks it up in prose prompts.

Node context is **loaded on demand**, not injected at session start. Load only what you need.

**Existing node bibles:**
- `docs/nodes/PXL.md` — Pixel / PXL (Pixel origin story, GLMZ; formerly PNHL/TDIU; Channeler+Ghost+Splicer; Detroit escape opening)
- `docs/nodes/BCODA.md` — Bushido Coda flagship novel (GLMZ)
- `docs/nodes/ATTE.md` — Attendance / Yemina Fola investigation (GLMZ)
- `docs/nodes/VATD.md` — Vultures at the Door / Thomas & Levin (GLMZ)
- `docs/nodes/DWIACE.md` — Death Whispers in a Cat's Ear / Rennick Investigations (GLMZ)
- `docs/nodes/SPRW.md` — Sparrow / Elias Macias & the orbital mystery (GLMZ)
- `docs/nodes/MNEMO.md` — Mnemosync / Amara & Seto (GLMZ, in progress; formerly ULC, redesigned SS-A14)
- `docs/nodes/TEST.md` — Testament / Bear court-martial (GLMZ)
- `docs/nodes/GIW.md` — Grafted Into War / M-101/Soren (Fantasy)
- `docs/nodes/MxG.md` — Magenta & Gunmetal / GLMZ run (GLMZ, planned; Shadowrun-style heist → True Lies finale)
- `docs/nodes/RTR.md` — Read the Room / Faith Larson & Ethan Wolfe (GLMZ; Fenris band; Faith is a Read; Milwaukee dive club)
- `docs/nodes/LSSS.md` — Lyra, Sinterspawn Slayer (Fantasy; standalone; COMPLETE; VIGL prose register exemplar; 1 beat)
- `docs/books/bushido-coda-strands-bible.md` — BCODA (legacy long-form; superseded by BCODA.md above)

## Codex (how to work with the canon)

The project follows the **MindAttic Codex** documentation standard. The source of truth lives under
`docs/`:

- **`docs/BIBLE.md`** (L0) — engine invariants (SS-LAW-N) + **GLMZ** universe canon. Authoritative
  for GLMZ world facts. Fantasy/Caul universe facts live in `docs/universes/CAUL.md`.
  Inherits laws from `D:/Projects/MindAttic/MindAttic.HouseRules.md`.
- **`docs/WORLD.md`** — **GLMZ** world master: how the city works, how the cast works, how combat
  works, how the prose sounds. Hand-edit directly.
- **`docs/FRANCHISE.md`** — **GLMZ** franchise & IP bible: commercial positioning, genre, logline.
  Hand-edit directly.
- **`Nodes.NodeBible`** (DB, L0 per-story) — story arc, beat spine, character rules, locks,
  voice register, structural blueprint. **The single source of truth for that StoryNode.**
  Mirrored to `docs/nodes/<CODE>.md` as a generated read-only file — never hand-edit the file.
- **`docs/USER_STORIES.md`** (L2) — test-cited stories + backlog + audit log. Every `✅` names its
  verifying test or recorded evidence.
- **`docs/series/GLMZ.md`** — GLMZ universe story coordination board: main series chapter
  roster (Books 1–5), standalone story roster, character arc ledger, villain supply chain,
  cross-story plant/payoff registry, world-revelation sequencing, entity seeding roadmap.
  **Update this doc whenever a story is added, a character state is resolved, or a plant/payoff
  is confirmed.** This is a planning instrument, not a canon source.
- **`docs/planning/_TEMPLATE.md`** — mandatory 10-section story brief template. Every new GLMZ
  story fills `docs/planning/<CODE>-brief.md` from this template before a node bible is created
  (see New Story Workflow Step 0 below).
- **`docs/rfc/`** — design notes that graduate into BIBLE.md or story bibles.
- **`docs/data/`** (L5) — canon-as-data: JSON Schemas + the master entity-identity table for the
  `engine_data/*.json` seed corpus. **Live canon is the SQL DB, not files** (SS-LAW-1).
- **`docs/BIBLE.digest.md`** — GENERATED by `tools/codex.ps1 digest`; never hand-edit. The
  SessionStart hook (`.claude/hooks/inject-digest.ps1`) injects it as authoritative context.

**`docs/AMENDMENTS.md` is RETIRED (2026-07-04).** All amendments have been merged into their
canonical destinations. Do not append to it. Do not reference it.

Working rules:
- **Canon changes go DIRECTLY into the authoritative source** — `docs/BIBLE.md` (or `docs/WORLD.md`) for
  GLMZ/engine facts, `docs/universes/CAUL.md` for Fantasy/Caul facts, `Nodes.NodeBible` (via `set_story_bible`
  MCP) for story-specific facts. There is no amendment layer. After updating NodeBible, re-run
  `generate_node_doc` + `ss --sync-markdown`.
- A fact lives in **exactly one file**; cite it by its stable `{#SS-...}` id, never by line number.
- Update the Bible/stories status in the **same change** that moves a goal; "done" means a test or
  build proves it.
- After editing any `docs/*` canon file, run `powershell -File tools/codex.ps1 digest` then
  `powershell -File tools/codex.ps1 doctor` — doctor must pass. (`pwsh` is not installed; use `powershell`.)
- The repo rule "no Markdown except README" is amended for the Codex `docs/*.md` set (see SS-A1);
  data files stay JSON.

## New Story Workflow (mandatory — see [docs/BIBLE.md §10](docs/BIBLE.md#SS-§10))

**Every new story/book follows this sequence without exception:**

0. **Series Brief** — fill `docs/planning/<CODE>-brief.md` using the template at
   `docs/planning/_TEMPLATE.md`. The brief must cover all 10 sections before a node bible is
   created. A story that cannot answer all 10 sections does not belong in the roster yet.
   After filing: update `docs/series/GLMZ.md` Story Roster (§1–2), Character Arc Ledger exit
   states (§3), and Plant/Payoff Registry (§5). Check World-Revelation Sequencing (§6) — this
   story must not reveal anything before its designated book.
1. **Docs first** — if new world facts: GLMZ facts → `docs/BIBLE.md` or `docs/WORLD.md`; Fantasy/Caul
   facts → `docs/universes/CAUL.md`. For story-specific facts (arc, characters, voice register, locks),
   write the hand-authored content via `set_story_bible` MCP into `Nodes.NodeBible`. Add story entry to
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
   ending style (avalanche, no epilogue), 3-5 intertextual anchors from the entity DB. Then run
   `ss --generate-node-doc --slug <slug>` to regenerate `docs/nodes/<CODE>.md` with the blueprint
   section auto-populated from the DB.
5. **Prose** — Sonnet draft → Opus polish → reflow → logic sweep (see Quality Verification SOP below) → scan entity mentions.
6. **Export** — `--publish`; flip USER_STORIES to ✅ with evidence.

Never write prose before steps 0, 1, 2, and 4 are complete.

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
6. **Bible agreement** — prose and `Nodes.NodeBible` (the hand-authored sections) tell the same
   story; fix one in the same change, then re-run `generate_node_doc`.

Reports land in `audit-outlines-<date>/logic/`; findings are triaged
**BLOCKER / MODERATE / MINOR** and fixed with minimal splices. Fix what a finding names;
if you can't name the failure, leave the beat alone.

The old dual-review machinery (standalone ≥82 / cumulative ≥85, `--review-node` panels,
Legion votes) still exists — **on explicit user request only**.
