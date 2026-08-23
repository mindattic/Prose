# Prose — System Architecture

> **Replaces the previous version of this file (superseded 2026-06-07, deleted content centered on
> `StoryDirectorService` and a Blazor `Write.razor` UI — both deleted 2026-08-13, commit
> `ed22bd4f6`, "Command-line only").** This describes the C# system as it actually exists as of
> 2026-08-23. For story-world canon (GLMZ facts, engine invariants), see `docs/BIBLE.md`. For
> universal prose craft rules, see `docs/ENGINE.md` (Tier 0) and `docs/CRAFT.md` (Tier 1) — this
> file is about the *codebase*, not the *canon*.

## 1. The project map

Nine C# projects live under `v3/`. Four are the system; the rest are auxiliary tools or generated
contract types.

| Project | Kind | Role |
|---|---|---|
| **`Prose.Core`** | Library | Everything: `ProseDbContext`, every service (enrichment chain, WriteGate, DCM, audits, generation), EF migrations. No entry point of its own — referenced by all three below. |
| **`Prose.Hub`** | ASP.NET Core app (`WebHost.UseUrls("http://127.0.0.1:5900")`) | **The one resident, long-running process.** Owns the DI container every write and most reads actually execute inside. Hosts `CliDispatch`/`ToolDispatch` (reflection-based command execution), the WriteGate hook, the resident Trinity (§3), and a small set of HTTP endpoints (`/api/health`, `/api/dcm/status`, `/api/entities/active`, `/api/edges`). Runs with a visible console window that echoes every dispatched command in/out (`HubConsoleEcho`, added 2026-08-21/22). |
| **`Prose.Cli`** | Console exe | The `prose` / `Prose.Cli.exe` command line. **Almost entirely a forwarder**: confirmed 218 of 220 handler call sites in `Program.cs` call `HubCliClient.ForwardAsync(...)`, which POSTs to the running Hub and streams its console output back — the actual `RunAsync` handler classes under `Prose.Cli/Cli/*.cs` (257 files) execute *inside the Hub process*, not inside the `Prose.Cli.exe` process that was invoked. Two commands run in-process instead (`WorkerModeCli`, `EstimateCostCli`) — neither touches shared state. |
| **`Prose.Mcp`** | Library (loaded into Hub) | MCP tool definitions (40 `Tools.*.cs` files, 309 `[McpServerTool]`-attributed methods, `docs/MCP_TOOLS.md` auto-generated). `Prose.Hub` loads this DLL directly — `ToolDispatch` reflects into it the same way `CliDispatch` reflects into `Prose.Cli`. |
| `Prose.Hub.Contracts` | Library | Shared DTOs (`ObservabilityDtos.cs`) between Hub and ObserverUi — kept separate so ObserverUi doesn't need to reference all of Core. |
| `Prose.ObserverUi` | Blazor (Razor) app | A live observability dashboard over Hub's own state (`HubApiClient`) — part of the 2026-08-20 "Observability plan," distinct from the deleted `Write.razor` authoring UI. Watches, does not author. |
| `Prose.KdpPublish` | WPF desktop app | Separate tool for Amazon KDP publishing (WebView2-driven), documented in project memory, not part of the Cli/Core/Mcp/Hub write path. |
| `Prose.LlmCli` | Console exe | Standalone LLM-calling utility, independent of the Hub dispatch model. |
| `Prose.UnitTests` | Test project | 2176 tests as of 2026-08-23 (14 pre-existing, unrelated failures — `LoggingServiceTests` Serilog-format parsing + 2 `DI_Resolves*Tools` tests missing a `HubInvoker` test registration). |

**The system that matters for "does a write get validated, does a read see fresh state" is
Cli → Hub (→ Mcp) → Core.** Everything else is peripheral tooling.

## 2. The dispatch model — reflection, not routing

Neither `CliDispatch` nor `ToolDispatch` makes any decision about *which* code to run beyond
"resolve this exact string to a type/method and call it." There is no request routing,
inference, or fallback logic in either.

- **`CliDispatch.ExecuteCoreInnerAsync`** (`v3/Prose.Hub/CliDispatch.cs`): resolves a handler
  `Type` by exact name (`ResolveHandlerType`, cached in a `ConcurrentDictionary`), reflects its
  `RunAsync`/`Run` method, redirects `Console.Out`/`Error`/`In` and the working directory for the
  call's duration (serialized through `ConsoleGate`, a single global semaphore — one CLI command
  runs inside Hub at a time), invokes it, restores state, and writes a `CommandLedgerEntry` row.
- **`ToolDispatch.InvokeCoreAsync`** (`v3/Prose.Hub/ToolDispatch.cs`): identical shape for MCP —
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

There is **no routing intelligence** anywhere in the system — Hub never decides which service or
command to invoke; every dispatch is a caller (human via CLI flag, or an LLM via MCP tool name)
naming the exact target.

## 4. The prose-generation path — `ProseWriterRouter`

`ProseWriterRouter.WriteAsync(context, beatId, beatIndex, totalBeats)` is the one real entry
point for beat generation (also `CombatSceneWriter` for explicit multi-exchange combat). It
coordinates ~25 enrichment services, each gated on its own precondition — the full, corrected
table lives in **CLAUDE.md's "Context enrichment chain" section** (corrected 2026-08-23; do not
duplicate that table here, it will drift — this file points to it as the source of truth for that
specific mechanism).

One confirmed dead bypass exists: `SceneGenerationService` (`v3/Prose.Core/Services/
SceneGenerationService.cs`) hand-rolls its own generation path around `BeatGeneratorService.
GenerateBeatAsync` directly, skipping the entire enrichment chain. DI-registered, has its own
test, zero call sites in `Cli`/`Mcp`/`Hub` — leftover from the deleted Blazor UI. A landmine if a
future command gets wired to it instead of `ProseWriterRouter`; scheduled for deletion.

## 5. The WriteGate — the one chokepoint for writes

Shipped 2026-08-22 (commits `06959f65a`, `eac584be0`). `ProseDbContext.SaveChanges`/
`SaveChangesAsync` (`v3/Prose.Core/Data/ProseDbContext.cs`) run two extra steps beyond the base
EF save, for **every** write on a `ProseDbContext` regardless of caller:

- **Sync, pre-save, can reject**: walks `ChangeTracker.Entries()` against every registered
  `IWriteGateSyncCheck` (`v3/Prose.Core/Services/WriteGate/`); a failing check throws
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

**Gap found, not yet fixable from a CLI-only session**: `set_canon_section` has no CLI
equivalent — editing canon content requires an MCP client connected to `Prose.Mcp`. A Claude Code
CLI session with no MCP server attached cannot correct canon content at all, only read/regenerate
the existing `.md` mirrors. Concretely: `docs/ENGINE.md` §SS-ENGINE-2 currently states there is
"no soft-delete anymore anywhere in the beat/node/link tables" and that `ArchivedBooks` snapshots
are the *only* recovery mechanism — this was true in the narrow window after
`RemoveBeatTemporalVersioning` but **stale as of 2026-08-17**, when SQL Server system-versioning
was re-enabled for `Beat`/`Node`/`BeatNode` (`ProseDbContext.SystemVersionedTables`, once
`BeatNodes.IsEnabled` was dropped as the actual root cause). `FOR SYSTEM_TIME AS OF` rewind is a
real recovery path again and ENGINE.md doesn't say so. Flagged for the next session with MCP
access to fix via `set_canon_section`, not a raw SQL workaround.

## 8. Documentation map (what's real, what's stale)

| Reference for | Where |
|---|---|
| C# system architecture (this document) | `docs/ARCHITECTURE.md` |
| Story-world canon (GLMZ facts, engine invariants) | `docs/BIBLE.md` |
| Universal prose craft — Tier 0 (loads above everything, every beat) | `docs/ENGINE.md` |
| Universal prose craft — Tier 1 | `docs/CRAFT.md` |
| Per-universe craft — Tier 2 | `docs/GLMZ.md` / `docs/SCRY.md` |
| Character Doctrine (topic-tier craft law) | `docs/CHARACTER.md` |
| Logic-sweep / QA methodology | `docs/LOGIC.md`, `docs/READER-QA.md` |
| MCP tool reference (auto-generated, current) | `docs/MCP_TOOLS.md` (309 tools) |
| CLI command reference | **Does not exist yet** — 257 command files, no generated reference; tracked as a documentation gap |
| Story/feature status | `docs/USER_STORIES.md` (stale ~1 week as of 2026-08-23 — missing this week's write-gate/architecture work) |
