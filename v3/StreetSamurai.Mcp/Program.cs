using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using StreetSamurai.Core.Extensions;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Services;

// ── StreetSamurai MCP server ─────────────────────────────────────────────────
// Exposes the canon (characters, places, factions, books, outlines, motifs,
// literary rules) plus the semantic index as Model Context Protocol tools so
// Claude (Desktop / Code / API clients) can call into the world without the
// caller copy-pasting JSON blobs into the prompt.
//
// Stdio transport — the standard MCP wire format. Launched per-session by the
// client (see README.md for Claude Desktop / Code config).
//
// Toggle: this server only runs when a client launches it. Remove or comment
// out the entry in claude_desktop_config.json / ~/.claude.json to fall back to
// chat-only behaviour without the canon tools.
//
// Voice preservation: every tool here is read-mostly and *returns data* —
// nothing in this server generates prose. The caller (Claude, in conversation)
// stays the single author; tools just give it better context.
// ────────────────────────────────────────────────────────────────────────────

// ── Doc export mode ──────────────────────────────────────────────────────────
// `--export-tools [path]` reflects over this assembly's [McpServerTool] methods
// and writes the generated tool reference (default docs/MCP_TOOLS.md), then exits
// WITHOUT starting the server, the DB, or logging. Keeps the doc drift-free.
if (args.Contains("--export-tools"))
{
    var idx = Array.IndexOf(args, "--export-tools");
    var outPath = (idx + 1 < args.Length && !args[idx + 1].StartsWith("--")) ? args[idx + 1] : "docs/MCP_TOOLS.md";
    var n = StreetSamurai.Mcp.ToolDocGenerator.Generate(outPath);
    Console.Error.WriteLine($"[export-tools] Wrote {n} tools to {Path.GetFullPath(outPath)}");
    return;
}

// Logs go to a file under the canon root's logs/ directory — stdout is reserved
// for the MCP wire protocol, so anything written there breaks the transport.
var settings = new SettingsService();
var paths = new FileSystemPathProvider(settings);
var logPath = Path.Combine(paths.LogDir, "mcp-.txt");

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .WriteTo.File(
        logPath,
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        shared: true)
    .CreateLogger();

// Multi-universe: honor a `--universe <slug>` arg (and the SS_UNIVERSE env var)
// so an MCP session can target GLMZ or Fantasy independently of other processes
// (SS-LAW-15). A switch_universe tool can also change it mid-session.
UniverseBootstrap.RequestedSlug ??= UniverseBootstrap.ParseSlug(args);

var builder = Host.CreateApplicationBuilder(args);

// Route the framework log pipeline through Serilog (file-only, no console).
builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

// All Core services — repositories, BookOutlineService, SemanticIndexService,
// MotifService, WritingQualityService, etc.
builder.Services.AddStreetSamuraiServices();

// MCP server with stdio transport. WithToolsFromAssembly scans this assembly
// for [McpServerToolType] classes and registers each [McpServerTool] method.
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

var mcpHost = builder.Build();
// Construct the universe context up front so canon reads are scoped to the
// requested universe (--universe / SS_UNIVERSE) from the first tool call.
mcpHost.Services.GetRequiredService<IUniverseContext>();
await mcpHost.RunAsync();
