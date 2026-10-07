# Prose — System Architecture

> **Replaces the previous version of this file (superseded 2026-06-07, deleted content centered on
> `StoryDirectorService` and a Blazor `Write.razor` UI — both deleted 2026-08-13, commit
> `ed22bd4f6`, "Command-line only").** This describes the C# system as it actually exists as of
> 2026-10-07 (refreshed from the ~2026-08-23 version — see the Hub observability work order
> 01a1188f for what changed). For story-world canon (GLMZ facts, engine invariants), see `docs/BIBLE.md`. For
> universal prose craft rules, see `docs/ENGINE.md` (Tier 0) and `docs/CRAFT.md` (Tier 1) — this
> file is about the *codebase*, not the *canon*.

## 1. The project map

Nine C# projects live under `src/`. Four are the system; the rest are auxiliary tools or generated
contract types.

| Project | Kind | Role |
|---|---|---|
| **`Prose.Core`** | Library | Everything: `ProseDbContext`, every service (enrichment chain, WriteGate, DCM, audits, generation), EF migrations. No entry point of its own — referenced by all three below. |
| **`Prose.Hub`** | ASP.NET Core app (`WebHost.UseUrls("http://127.0.0.1:5900")`) | **The one resident, long-running process.** Owns the DI container every write and most reads actually execute inside. Hosts `CliDispatch`/`ToolDispatch` (reflection-based command execution), the WriteGate hook, the resident Trinity (§3), and a small set of HTTP endpoints (`/api/health`, `/api/dcm/status`, `/api/entities/active`, `/api/edges`). Runs with a visible console window that echoes every dispatched command in/out (`HubConsoleEcho`, added 2026-08-21/22). |
| **`Prose.Cli`** | Console exe | The `prose` / `Prose.Cli.exe` command line. **Almost entirely a forwarder**: 270 handler call sites in `Program.cs` call `HubCliClient.ForwardAsync(...)` (285 commands total per `docs/CLI_COMMANDS.md`), which POSTs to the running Hub and streams its console output back — the actual `RunAsync` handler classes under `Prose.Cli/Cli/*.cs` (302 files) execute *inside the Hub process*, not inside the `Prose.Cli.exe` process that was invoked. A couple of commands (`WorkerModeCli`, `EstimateCostCli`) run in-process instead — neither touches shared state. |
| **`Prose.Mcp`** | Library (loaded into Hub) | MCP tool definitions (50 `Tools.*.cs` files; `docs/MCP_TOOLS.md` auto-generated, self-reports 289 tools across 52 families). `Prose.Hub` loads this DLL directly — `ToolDispatch` reflects into it the same way `CliDispatch` reflects into `Prose.Cli`. |
| `Prose.Hub.Contracts` | Library | Shared DTOs (`ObservabilityDtos.cs`) between Hub and ObserverUi — kept separate so ObserverUi doesn't need to reference all of Core. |
| `Prose.ObserverUi` | Blazor (Razor) app | A live observability dashboard over Hub's own state (`HubApiClient`) — part of the 2026-08-20 "Observability plan," distinct from the deleted `Write.razor` authoring UI. Watches, does not author. |
| `Prose.KdpPublish` | WPF desktop app | Separate tool for Amazon KDP publishing (WebView2-driven), documented in project memory, not part of the Cli/Core/Mcp/Hub write path. |
| `Prose.LlmCli` | Console exe | Standalone LLM-calling utility, independent of the Hub dispatch model. |
| `Prose.UnitTests` | Test project | 3003 tests as of 2026-10-07 (2984 passed, 0 failed, 19 skipped — the 14 pre-existing failures this row used to note are gone; full `dotnet test` run clean). |

**The system that matters for "does a write get validated, does a read see fresh state" is
Cli → Hub (→ Mcp) → Core.** Everything else is peripheral tooling.

## 2. The dispatch model — reflection, not routing

Neither `CliDispatch` nor `ToolDispatch` makes any decision about *which* code to run beyond
"resolve this exact string to a type/method and call it." There is no request routing,
inference, or fallback logic in either.

- **`CliDispatch.ExecuteCoreInnerAsync`** (`src/Prose.Hub/CliDispatch.cs`): resolves a handler
  `Type` by exact name (`ResolveHandlerType`, cached in a `ConcurrentDictionary`), reflects its
  `RunAsync`/`Run` method, redirects `Console.Out`/`Error`/`In` and the working directory for the
  call's duration (serialized through `ConsoleGate`, a single global semaphore — one CLI command
  runs inside Hub at a time), invokes it, restores state, and writes a `CommandLedgerEntry` row.
- **`ToolDispatch.InvokeCoreAsync`** (`src/Prose.Hub/ToolDispatch.cs`): identical shape for MCP —
  resolve `{ToolClass}` type, resolve `{Method}Impl`, JSON-deserialize args positionally by
  parameter name, invoke, log to the same ledger table.

Both write a `CommandLedgerEntry` for every invocation — this is the closest thing to a system
audit trail, and doubles as a smoke test (confirm a ledger row + any expected findings-table row
exist with correlated timestamps after running a command).

## 3. The resident Trinity — Hub's real, but narrow, memory

Hub genuinely holds long-lived in-memory state across separate CLI/MCP invocations — this is not
aspirational. Because the DI container these three are singletons in lives inside the one
resident `Prose.Hub` process, and because ~99% of CLI/MCP calls forward into that same process
(§1), state set by one command really is still there for the next one:

| Component | What it holds | Registration |
|---|---|---|
| `DocContextStack` | The DCM working set — a `ConcurrentDictionary<Guid /*NodeId*/, ContextState>` with its own action counter and LRU eviction (`EvictAfterActions`) | `AddSingleton`, `ServiceCollectionExtensions.cs` |
| `EntityContextStack` | Entity-level LRU (the "Lyra vs Vega" rule — see CLAUDE.md's DCM section) | `AddSingleton` |
| `UniverseGraphService` | In-memory entity/edge graph, per-universe `GraphState`, `EnsureLoaded`/`EnsureFresh` | `AddSingleton` |

Program.cs's own comment calls these three "the resident Trinity." Two HTTP endpoints expose them
live: `/api/dcm/status` (reads `docStack.GetActive(...)`), `/api/entities/active` (reads
`entityStack.GetActive(nodeId)`).

**The important caveat**: this memory is real but **narrow**. Only `DocContextService` and
`ProseWriterRouter` (the beat-generation path, §4) ever read or write it. The other ~217 CLI/MCP
commands — entity CRUD, audits, sweeps, exports — neither consult it nor invalidate it. A write
from one of those commands can leave the resident Trinity holding stale state for a concurrent or
later generation call, with nothing to catch it. Widening and hardening this is tracked as its
own initiative (see the write-gate-successor plan referenced in project memory,
`project_writegate_phase0_1_shipped_2026_08_22.md` and its follow-on).

The entity stack now has a relational fact pass at beat preparation time. It joins active EntityIds to canonical/confirmed `ContinuityClaims`, scores predicate/object text against the beat goal and scene-so-far, collapses duplicate or historical rows per entity/predicate with deterministic tie-breakers, and injects only the highest-scoring facts into the DCM block. Scene-critical details belong here (for example, Kyle drawing Silence with his right hand or Pixel soldering left-handed); entity JSON blobs are not used as a fact store. The same pass is used regardless of whether the request arrived through CLI, MCP, or another LLM client because all adapters enter through the provider-neutral protocol.

`EntityContextStack` (the table row above) is just the container; **`EntityContextService`**
(`src/Prose.Core/Services/EntityContextService.cs`) is the engine that populates and reconciles
it — three passes per beat: DETECT (scan beat goal + prior scene text for proper nouns, resolve
to entity GUIDs), EXPAND (pull each detected entity's semantic neighbors up to depth 2, from
stored embedding vectors, not live re-embedding — fixed 2026-09-07, RFC 0012, after this loop was
measured at 52 OpenAI round-trips per Bushido Coda beat), and RECONCILE (post-generation,
non-blocking — scan generated prose for entity claims, detect conflicts against canon, file a
`Finding` for Legion to resolve as fix-prose/update-entity/ignore). This isn't new from this
pass — the engine predates this document — it was simply never named here before.

There is **no routing intelligence** anywhere in the system — Hub never decides which service or
command to invoke; every dispatch is a caller (human via CLI flag, or an LLM via MCP tool name)
naming the exact target.

## 3a. The Verification Context Provider (RFC 0011 Brick 1)

**`VerificationContextService`** (`src/Prose.Core/Services/VerificationContextService.cs`) closes
the gap RFC 0011 named: a Finding-producing check used to judge a beat in isolation, with no way
to ask "what do I already know about this beat/book/character that should change how I judge it?"
— the root cause behind a 2026-08-10 session fixing four separately-discovered instances of the
same bug (abstract-synopsis-vs-concrete-prose scoring, a POV character's own established voice
misread as a generic AI tic, etc.). It is a real, consulted dependency today, not just a design
document: registered as a singleton (`ServiceCollectionExtensions.cs`) and injected into
`ProseWriterRouter`, `BarksExportService`, and `Writer/BeatBriefBuilder`, with test coverage in
`BarksExportServiceTests`. RFC 0011's other two infrastructure bricks are also real, not proposals:
`prose --findings-staleness` (Brick 2 — CraftChecklist is deliberately absent from it, confirmed
still correct: its LLM checklist was deleted under BIBLE ADR-10, and the category now holds only
deterministic LINT rows hash-gated on beat text, which doesn't need a rule-version concept) and
`prose --provider-status` plus a shared
`ThrowingLlmService` test fixture (Brick 3 — re-surveyed 2026-10-07: at least 51 services take
`ILlmService` as a constructor dependency; only 3 (`SceneContextAssemblerTests`,
`BeatAuditServiceTests`, `SceneCollisionServiceTests`) have the provider-down test. This is a much
bigger gap than "a few services left" — closing it is its own bounded follow-up work order, not
something to rush as a side effect of an unrelated pass). See `docs/rfc/0011-context-aware-
verification-and-universal-engine-roadmap.md` for the full brick-by-brick status.

## 4. The prose-generation path — `ProseWriterRouter`

`ProseWriterRouter.WriteAsync(context, beatId, beatIndex, totalBeats)` is the one real entry
point for beat generation (also `CombatSceneWriter` for explicit multi-exchange combat). It
coordinates ~25 enrichment services, each gated on its own precondition — the full, corrected
table lives in **CLAUDE.md's "Context enrichment chain" section** (corrected 2026-08-23; do not
duplicate that table here, it will drift — this file points to it as the source of truth for that
specific mechanism).

**No generation bypass exists any more (corrected 2026-09-04; this section described one as live
and "scheduled for deletion" for months after it was gone).** The last one,
`SceneGenerationService`, hand-rolled its own path around `BeatGeneratorService.GenerateBeatAsync`
and skipped the entire enrichment chain; it was **deleted 2026-08-23** as confirmed dead code —
verified again here: no such file, and the sole surviving reference is a historical note in
`BeatExtractionService.cs`'s header. `StoryDirectorService` and `Write.razor` went earlier, with
the Blazor UI (`ed22bd4f6`). `ProseWriterRouter` is the only live generation entry point.

## 5. The WriteGate — the one chokepoint for writes

Shipped 2026-08-22 (commits `06959f65a`, `eac584be0`). `ProseDbContext.SaveChanges`/
`SaveChangesAsync` (`src/Prose.Core/Data/ProseDbContext.cs`) run two extra steps beyond the base
EF save, for **every** write on a `ProseDbContext` regardless of caller:

- **Sync, pre-save, can reject**: walks `ChangeTracker.Entries()` against every registered
  `IWriteGateSyncCheck` (`src/Prose.Core/Services/WriteGate/`); a failing check throws
  `WriteGateRejectedException` and aborts the save. Three checks are live:
  `SelfAliasSyncCheck` (alias can't equal its own entity's name), `CrossUniverseOriginCheck`
  (`Entity.OriginNodeId` must be in the entity's own universe), `PreviousNodeCycleCheck` (a
  book's sequel chain can't loop).
- **Async, post-save, fire-and-forget**: dispatches changed entities to `IWriteAuditService`
  (`DefaultWriteAuditService`) for slower judgment-based checks — currently near-duplicate-entity
  detection, filed as `Finding` rows.

Both lists are wired once at Hub startup by `WriteGateBootstrap`, which **must be eagerly
resolved** in `Program.cs` (a singleton nobody resolves never constructs — this is the same "a
mechanism exists but nothing activates it" failure class the WriteGate initiative itself was built
to close; see §6). `WriteGateScope` is the ambient static gateway `ProseDbContext` reads from,
mirroring the existing `UniverseScope` pattern.

**Known limitation, by design**: `ExecuteDeleteAsync`/`ExecuteUpdateAsync`/raw SQL bypass
`ChangeTracker` and are invisible to this mechanism. The write-gate initiative audited all such
call sites across the ~150 CLI/MCP surface and gave each an explicit disposition (rewired onto a
sanctioned service method, or a documented accepted exception) — see project memory for the full
per-file list.

## 6. A recurring bug class worth naming

Three times now (NodeWorkbenchService's validator hooks, `DeleteNodeCli` vs. `NodeWorkbenchService
.DeleteNodeAsync`, `CloneNodeCli` vs. `DuplicateNodeAsync`), the same shape of bug has appeared:
**a sanctioned/improved mechanism gets built, but the code it was meant to replace never gets
rewired onto it** — so the old, buggier, or unvalidated path keeps running in production while
the fix sits unused. When adding a new sanctioned method or service, the check that closes the
loop is: *grep for every existing caller of the thing being replaced, and confirm each one was
actually repointed* — not just that the new method compiles and has a test.

## 7. Canon documents are ALL database-backed — never hand-edit `docs/*.md`

Every canon `.md` file under `docs/` (`BIBLE.md`, `WORLD.md`, `FRANCHISE.md`, `CRAFT.md`,
`GLMZ.md`, `SCRY.md`, `DELIGHT.md`, `ENGINE.md`, `CHARACTER.md`, `universes/ENTOS.md`) is a
generated mirror of `CanonDocumentSections` rows, keyed by `CanonDocumentType` (`WorldBible`,
`WorldMaster`, `Franchise`, `UniverseCanon`, `CraftGuide`, `UniverseCraft`, `DelightGuide`,
`EngineGuide`, `CharacterDoctrine`). Confirmed live 2026-08-23 via `prose --generate-canon-md
--all`: every one of these files carries a `<!-- GENERATED — do not hand-edit -->` banner and a
real, non-trivial section count. **This file previously (in CLAUDE.md's Codex table) told readers
to hand-edit 5 of these files directly** — stale instructions from before they were migrated into
`CanonDocumentSections`; fixed 2026-08-23. The only sanctioned edit path for any of them is the
`set_canon_section` MCP tool, followed by `prose --generate-canon-md --type <type>` to
re-materialize the `.md` mirror.

**Previously-flagged gap, confirmed fixed**: this section used to say `docs/ENGINE.md`
§SS-ENGINE-2 wrongly claimed `ArchivedBooks` snapshots were the *only* recovery mechanism, stale
since the 2026-08-17 system-versioning re-enable, and that fixing it needed an MCP client this
file's author didn't have at the time. Re-read 2026-10-07: the live `docs/ENGINE.md` §SS-ENGINE-2
already states the corrected fact (`FOR SYSTEM_TIME AS OF` rewind is real again, `ArchivedBooks`
is not the only path) and even narrates its own prior mistake — someone fixed it via
`set_canon_section` between 2026-08-23 and now. `set_canon_section` itself still has no CLI
equivalent (only an MCP tool), which remains worth knowing for a CLI-only session, but the
specific ENGINE.md gap this section used to point at no longer exists.

## 8. Documentation map (what's real, what's stale)

| Reference for | Where |
|---|---|
| C# system architecture (this document) | `docs/ARCHITECTURE.md` |
| Story-world canon (GLMZ facts, engine invariants) | `docs/BIBLE.md` |
| Universal prose craft — Tier 0 (loads above everything, every beat) | `docs/ENGINE.md` |
| Universal prose craft — Tier 1 | `docs/CRAFT.md` |
| Per-universe craft — Tier 2 | `docs/GLMZ.md` / `docs/SCRY.md` |
| Character Doctrine (topic-tier craft law) | `docs/CHARACTER.md` |
| Logic-sweep / QA methodology | `docs/LOGIC.md`, `docs/READER-QA.md`, `docs/LEDGER.md` |
| Story Ledger — the record of what is true, and cross-predicate contradiction detection | `docs/LEDGER.md` (added 2026-09-04, third peer of LOGIC/READER-QA) |
| MCP tool reference (auto-generated, current) | `docs/MCP_TOOLS.md` (289 tools, 52 families) |
| CLI command reference (auto-generated, current) | `docs/CLI_COMMANDS.md` (285 commands) — **gap closed 2026-09-04.** `CommandDocGenerator` parses the dispatch chain in `Program.cs` rather than reflecting over attributes, because CLI handlers carry none; refresh with `dotnet run --project src/Prose.Cli -- --export-commands docs/CLI_COMMANDS.md` |
| Story/feature status | `docs/USER_STORIES.md` (stale ~1 week as of 2026-08-23 — missing this week's write-gate/architecture work) |
