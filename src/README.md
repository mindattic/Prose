# Prose source (`src/`)

An orientation map of the .NET 10 solution (`Prose.slnx`). The wider picture — history, Dynamic
Context Memory, the database, the full CLI/MCP reference — is in the root [`README.md`](../README.md)
and [`docs/ARCHITECTURE.md`](../docs/ARCHITECTURE.md). How story work is done (the factory, work
orders, the read gate) is **not** described here: see
[`docs/agent/PROSE_PROTOCOL.md`](../docs/agent/PROSE_PROTOCOL.md).

## How the pieces fit

```
 prose.cmd / Prose.Cli ──POST /api/cli-invoke──┐
 Prose.Mcp (stdio MCP) ──POST /api/mcp-invoke──┤
 Prose.Writer.exe (WebView2) ──GET /writer─────┼──▶ Prose.Hub (Hub.exe, http://127.0.0.1:5900) ──▶ SQL Server
 Prose.Launcher.exe ──starts Hub if down───────┘        hosts Prose.Core services,
                                                         Prose.WriterUi (/writer, /read, /repo)
                                                         and Prose.ObserverUi (/app)
```

- **The Hub owns the database.** It is the one resident process holding the Core services, the
  universe graph and DCM state. Every other front door forwards into it.
- **CLI and MCP are thin clients.** Both refuse to start when the Hub is not healthy
  (`HubGate.EnsureReachableOrExit`, fail-closed — no in-process fallback). The CLI forwards each
  command as a handler-class name plus argv (`HubCliClient.ForwardAsync("BookCli", args)`); the Hub's
  `CliDispatch` reflects into `Prose.Cli`'s `Cli/*.cs` handlers. MCP tools forward through
  `HubInvoker` to `/api/mcp-invoke`; the Hub's `ToolDispatch` runs the tool's `{Name}Impl` body from
  `Prose.Mcp` in-process. The Hub references `Prose.Cli` and `Prose.Mcp` at compile time for that
  reflection; neither references the Hub back (they reach it only over HTTP).
- **Writer is a window, not an app.** `Prose.Writer` has no `Prose.Core` reference; it finds or
  starts `Hub.exe` (`HubProcess.cs`) and points WebView2 at `/writer`. All editor code is
  `Prose.WriterUi`, compiled into `Hub.exe` — so an editor change needs a **Hub** redeploy.
- The Hub binds `127.0.0.1:5900` only. `/api/health` is fail-closed (503 when SQL Server is
  unreachable). Protected endpoints take the `X-Prose-Key` header (`HubApiKeyFilter`), which the
  CLI/MCP attach from `Settings.json`. Run it with `ASPNETCORE_ENVIRONMENT=Development` or
  MindAttic authentication fails closed.
- Universe scope travels with each call: pass `--universe <slug>` (or set `PROSE_UNIVERSE`).

## Projects

| Project | Kind | Responsibility |
| --- | --- | --- |
| `Prose.Core` | class library | The engine: EF Core `ProseDbContext` + entities (`Data/`), models, repositories and services (`Services/`, incl. `Services/Factory/` — work orders, rulings, sessions), DI entry point `AddProseServices()` (`Extensions/`), KDP store (`Kdp/`, machine-local SQLite). `Migrations/` is EF-generated — never hand-edit. |
| `Prose.Hub.Contracts` | class library, zero dependencies | DTOs shared by the Hub and its UI clients (`ObservabilityDtos.cs`) and `DispatchResolution` — the rules by which a forwarded CLI command finds its handler, shared with the unit tests so they check the Hub's real rules. |
| `Prose.Hub` | ASP.NET Core exe → `Hub.exe` | The resident server. Minimal-API endpoints in `Program.cs` (`/api/health`, `/api/cli-invoke`, `/api/mcp-invoke`, `/api/factory/*`, `/api/universes/*`, …), SignalR `/hubs/observability`, and the Razor pages in `Components/` (`/app`, `/writer`, `/read`, `/repo`). `CliDispatch.cs`, `ToolDispatch.cs`, `CostGateDispatch.cs` are the forwarding targets. |
| `Prose.Cli` | console exe | The `prose` command (`prose.cmd` at the repo root runs it via `dotnet run`). `Program.cs` is the argv dispatch chain; `Cli/` holds the ~300 handler classes that actually run inside the Hub. `--export-commands` regenerates `docs/CLI_COMMANDS.md`; `agent bootstrap` works without a Hub. |
| `Prose.Mcp` | console exe (stdio MCP server) | The `mcp__prose__*` tool surface (`Tools*.cs`, one `[McpServerToolType]` class per area). See [`Prose.Mcp/README.md`](Prose.Mcp/README.md). |
| `Prose.WriterUi` | Razor class library | The hand-editing surface — `WriterShell`, `ProseEditor`, `ReadShell`, `WikiShell` and their panels, plus UI services (`Services/`). References `Prose.Core` directly, which is legal only because it is hosted **inside** the Hub. |
| `Prose.ObserverUi` | Razor class library | Observability tabs (dashboard, beats, logs, graph 2D/3D, DCM, repositories) served by the Hub at `/app`. Deliberately Core-free: talks to the Hub over HTTP/SignalR (`HubApiClient`, `ObserverHttpClient`) using `Prose.Hub.Contracts`. |
| `Prose.Writer` | WPF + WebView2 exe → `Writer.exe` | The editor window (see above). `HubProcess.cs` — "is the Hub up, if not start it" — is linked into `Prose.Launcher` too. |
| `Prose.Launcher` | WPF exe → `Launcher.exe` | A small picker that opens Writer or KdpPublish, starting the Hub first when needed. |
| `Prose.KdpPublish` | WPF + WebView2 exe | Drives Amazon KDP in an embedded browser to publish/republish books. The one desktop app that builds its own `AddProseServices()` container instead of going through the Hub; its run state lives in the SQLite `KdpStore`. Deploys to its own subfolder with its own `wwwroot\`. |
| `Prose.LlmCli` | console exe → `prose-llm` | Standalone escape hatch over MindAttic.Legion: one CLI for every provider, reading keys from the shared MindAttic Vault store. Deliberately **no** `Prose.Core` reference so it works when Core/the DB does not. Also the last-resort tier of `LlmRouter`'s fallback chain. |
| `Prose.UnitTests` | NUnit | Tests for Core, CLI dispatch wiring and MCP DI wiring. `Fixtures/` holds SQLite-backed canon fixtures. |
| `Prose.V4.Cli` | — | **Empty.** No project file or source, only stale `bin/`/`obj/` output; not in the solution. Safe to ignore. |

## Build, test, deploy

```powershell
# Build everything
dotnet build src\Prose.slnx

# Unit tests (offline; Explicit fixtures such as LiveKeys and WorldValidation are skipped)
dotnet test src\Prose.UnitTests\Prose.UnitTests.csproj
dotnet test src\Prose.UnitTests\Prose.UnitTests.csproj --filter "Category=LiveKeysTrusted"   # live provider keys (costs money)

# Publish desktop apps to C:\Apps\MindAttic\Prose\ (Hub.exe, Writer.exe, Launcher.exe, KdpPublish\)
powershell -ExecutionPolicy Bypass -File src\tools\deploy-apps.ps1                          # all four
powershell -ExecutionPolicy Bypass -File src\tools\deploy-apps.ps1 -Apps Hub -Start Hub     # Hub only; waits for /api/health
powershell -ExecutionPolicy Bypass -File src\tools\deploy-apps.ps1 -Apps Writer -Start Writer

# Start an already-published app without rebuilding (warns if the build is stale)
powershell -ExecutionPolicy Bypass -File src\tools\launch-app.ps1 -App writer   # writer | hub | kdp | launcher | wiki
```

- `deploy-apps.ps1` stops only the apps it replaces, publishes framework-dependent single-file
  through `%LOCALAPPDATA%\Prose\build-artifacts` (so it never fights a running `Prose.Mcp` for
  `bin\`), and writes self-redeploying `launch.bat` / `Writer.bat` / `Hub.bat` into the output folder.
  The repo-root `deploy-hub.bat`, `deploy-writer.bat`, `deploy-kdp.bat`, `deploy-prose.bat` call it.
- `src/tools/install-shortcuts.ps1` points desktop icons at those repo-root `.bat` files.
- `src/Prose.Hub/tools/deploy.ps1` is a legacy forwarder to `deploy-apps.ps1`;
  `src/Prose.KdpPublish/tools/` holds KdpPublish's older deploy scripts and its `kdp/logs/`.
- `Directory.Build.props` excludes `obj_*`/`bin_*` folders so isolated builds from concurrent
  sessions (`-p:BaseIntermediateOutputPath=obj_<tag>\`) do not collide.

## Generated references

- `docs/MCP_TOOLS.md` — `dotnet run --project src/Prose.Mcp -- --export-tools docs/MCP_TOOLS.md`
- `docs/CLI_COMMANDS.md` — `prose --export-commands`
