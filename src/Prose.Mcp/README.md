# Prose.Mcp

Model Context Protocol server exposing the Prose world canon as MCP tools so Claude — Desktop, Code, or any MCP client — can call into the data without copy-pasting JSON.

The server uses `ModelContextProtocol` (Anthropic's C# MCP SDK), targets .NET 10, and registers the Core services via `AddProseServices()`. All `[McpServerToolType]` classes are auto-discovered by `WithToolsFromAssembly()` — adding a new tool only requires building.

## It is a Hub client

The server refuses to start unless the Prose Hub (`http://127.0.0.1:5900`) is healthy (`HubGate.EnsureReachableOrExit`, fail-closed). Migrated tools are one-line forwards: the `[McpServerTool]` method calls `HubInvoker.InvokeAsync(toolClass, "{Name}Impl", args)`, which POSTs to the Hub's `/api/mcp-invoke`; the Hub's `ToolDispatch` then runs the `{Name}Impl` body (which still lives in this project) against the Hub's resident services. The caller's explicit universe (`--universe <slug>` / `PROSE_UNIVERSE` / `switch_universe`) is carried on every forward. The Hub API key from `Settings.json` is sent as `X-Prose-Key`. See [`../README.md`](../README.md) for how the Hub, CLI and Writer fit together.

## Tool surface

Tools live in `Tools*.cs`, one `[McpServerToolType]` class per area (canon, nodes/beats, findings, factory, obligations, universe, …). Many tools **write** (beat text, entity fields, rulings, work orders), not only read. Some tools are deactivated by commenting out their `McpServerTool` attribute (RFC 0014); those are not exposed.

The authoritative list is generated from the attributes — do not hand-maintain one here:

```powershell
dotnet run --project src/Prose.Mcp -- --export-tools docs/MCP_TOOLS.md
```

Result: [`docs/MCP_TOOLS.md`](../../docs/MCP_TOOLS.md).

## Registration (one-time)

### Claude Code

```bash
claude mcp add prose dotnet run --project <path-to-your-clone>/src/Prose.Mcp/Prose.Mcp.csproj --no-build --configuration Release
```

Writes to `~/.claude.json` and persists across sessions. Tools appear as `mcp__prose__*`.

To remove: `claude mcp remove prose`

### Claude Desktop

Edit `%APPDATA%\Claude\claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "prose": {
      "command": "dotnet",
      "args": [
        "run", "--project",
        "D:\\Projects\\MindAttic\\Prose\\src\\Prose.Mcp\\Prose.Mcp.csproj",
        "--no-build", "--configuration", "Release"
      ]
    }
  }
}
```

Restart Claude Desktop after editing.

## Build

```bash
dotnet build src/Prose.Mcp/Prose.Mcp.csproj --configuration Release
```

The `--no-build` flag in the registration command means the client launches the pre-built binary. Rebuild manually after code changes. Because migrated tool bodies execute inside the Hub, a change to a tool's `{Name}Impl` also needs a Hub redeploy (`src/tools/deploy-apps.ps1 -Apps Hub -Start Hub`); the MCP tool catalog (names, descriptions, parameters) only changes when an MCP client restarts the server.

## Logs

The server writes to `<engine-root>/data/logs/mcp-{date}.txt` (`FileSystemPathProvider.LogDir`; the `data` root moves to `PROSE_MUTABLE_DATA_ROOT` when that is set). **Stdout is reserved for the MCP wire protocol** — writing anything else to stdout corrupts the transport. Never use `Console.WriteLine` in any code path reached from the MCP server.

## Verify it is working

After registration, start a fresh Claude Code session and ask: *"What motifs are registered for Bushido Coda?"*

If Claude calls `mcp__prose__list_books` and `mcp__prose__get_motifs`, the server is wired.

If tools do not appear:
1. Check the Hub: `http://127.0.0.1:5900/api/health` must answer 200 — the server exits at startup otherwise.
2. Build: `dotnet build src/Prose.Mcp/Prose.Mcp.csproj -c Release`
3. Check registration: `claude mcp list` should show `prose`
4. Tail the log: `<engine-root>/data/logs/mcp-<today>.txt` should show `transport reading messages`
