using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MindAttic.Authentication;
using MindAttic.Authentication.Web;
using MindAttic.Vault.Configuration;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Extensions;
using Prose.Core.Interfaces;
using Prose.Core.Models.Graph;
using Prose.Core.Services;
using Prose.Hub;
using Prose.ObserverUi;
using Serilog;
using Serilog.Events;

// ── Prose Hub ─────────────────────────────────────────────────────────────
// The standalone always-on process holding the resident "Trinity" - the
// UniverseGraphService (in-memory entity/edge graph), DocContextStack (the
// DCM working set), and bible/beat access via the same repositories every
// other Prose.* front end uses. Prose.Cli and Prose.Mcp are meant to become
// thin clients of this over plain loopback HTTP; Claude Code can hit it
// directly via curl for the same reason - no dependency on any one client's
// connection staying up. A plain console app that happens to serve HTTP -
// not a public web site, loopback-only.
//
// Multi-universe correctness: every request that touches universe-scoped
// data resolves its {slug} to a universe id and calls
// IUniverseContext.SetFlowUniverse(id) - the AsyncLocal-backed per-flow
// override - NOT UseUniverse (which is process-wide and would bleed across
// concurrent requests for different universes). See UniverseGraphService's
// own per-universe GraphState dictionary for the matching fix on the graph
// side.

// Explicit user requirement (2026-08-27): yellow-background console so the Hub's own window is
// instantly recognizable among other open terminals. Clear() repaints the existing buffer with
// the new background — setting BackgroundColor alone only colors text written after this point.
// Must run before CaptureOriginal() below so the echoed writers inherit these colors too.
Console.BackgroundColor = ConsoleColor.Yellow;
Console.ForegroundColor = ConsoleColor.Black;
Console.Clear();

// Explicit user requirement (2026-08-21): visible window + live command echo. Must run before
// anything else touches Console — see HubConsoleEcho's own doc comment for why capturing the
// REAL writers here (rather than reading Console.Out fresh at each echo call) is required for
// the echo to stay safe once CliDispatch/ToolDispatch start redirecting Console.Out/Error.
Prose.Hub.HubConsoleEcho.CaptureOriginal();

// QuestPDF Community license — required call before the first Document.Create. The Hub is
// where PDF export actually executes (Prose.Cli/--export-node dispatches here via CliDispatch),
// so setting this only in Prose.Cli's own Program.cs (which never runs the export code itself)
// left every PDF export failing with QuestPDF's license-required exception — which aborted the
// rest of NodeFullExportService.ExportAllAsync (description.txt/synopsis/keywords/cover never
// ran) for every book in the corpus. This project is the non-commercial indie use case the
// Community tier exists for.
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// Force Prose.Cli.dll to actually load into this process. Unlike Prose.Mcp (loaded
// automatically because HubInvoker, a Prose.Mcp type, is registered in DI below),
// nothing here has a direct compile-time reference to any Prose.Cli type - CliDispatch
// only touches it via runtime reflection - so the CLR would otherwise never load the
// assembly at all, and AppDomain.CurrentDomain.GetAssemblies() in CliDispatch would
// never find the Cli/*.cs handler classes (found live: "unknown_handler_class").
_ = typeof(Prose.Cli.BookCli).Assembly;

// Observability plan Part E (2026-08-21): durable, searchable logs. Prose.Core/Services/
// LoggingService.cs already exists (still registered in AddProseServices()) and already
// implements time-range/severity/free-text search over Serilog daily log files - it's what
// the old, deleted Codex Logging.razor page called. Prose.Mcp already configures a Serilog
// file sink (mcp-.txt); the Hub - the process that now does essentially all the real work
// post-migration - never did, so it had no durable log at all, only the in-memory,
// restart-losing RingBufferLoggerProvider. Writing to "log-.txt" (not "hub-.txt") matters:
// LoggingService.Search hardcodes the glob "log-*.txt", so this filename makes every Hub log
// line searchable via that existing service with zero changes to it. Constructed manually
// (mirrors Prose.Mcp/Program.cs's own pattern) since this runs before the DI container exists.
var hubLogSettings = new SettingsService();
var hubLogPaths = new FileSystemPathProvider(hubLogSettings);
var hubLogPath = Path.Combine(hubLogPaths.LogDir, "log-.txt");
var hubSerilogLogger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
    .WriteTo.File(hubLogPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14, shared: true)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://127.0.0.1:5900");

// Added as ONE additional provider (deliberately no ClearProviders() - unlike Mcp, which
// needs pure stdio hygiene) - coexists with RingBufferLoggerProvider (registered below) on
// the same pipeline: one for live-tail, one for durable/searchable history.
builder.Logging.AddSerilog(hubSerilogLogger, dispose: true);

// Stage C completion: the 9 CLI commands that used Program.cs's BuildServicesWithVault(AndAuth)
// builders (rather than plain BuildCoreServices) need Vault-loaded config and, for
// --reset-password specifically, the full MindAttic.Authentication registration - neither was
// present in the Hub's own service provider, so forwarding them would have failed with a
// missing-service error. Registered unconditionally (superset of both builders' needs) so
// every migrated command shares the one resident service provider, matching every other
// forwarded command instead of needing a second, parallel DI container.
builder.Configuration.AddMindAtticVaultFiles(o => o.Buckets = new[]
    { "LLM", "Brokers", "Tokens", "Subtitles", "Notifications", "AudioStore", "Security" });
SettingsService.VaultConfiguration = builder.Configuration;
builder.Services.AddProseServices();
try
{
    // AddMindAtticAuthentication itself fail-closes (throws) when IsProduction=true and no
    // ConfigureDataProtection was supplied - a deliberate library safety check, not a bug.
    // Found live: this machine has neither DOTNET_ENVIRONMENT nor ASPNETCORE_ENVIRONMENT set,
    // so EnvironmentName defaults to "Production" and this throws today - confirmed the exact
    // same call in Prose.Cli's own BuildServicesWithVaultAndAuth would throw identically if
    // --reset-password were actually invoked directly on this machine right now, so this is
    // pre-existing environment behavior, not a regression from moving it here. The one thing
    // that must never happen: this one rarely-used operator command's setup taking down the
    // whole Hub (and therefore every other migrated command/tool) at startup. If this throws,
    // log it and continue without auth registered - only --reset-password becomes unavailable
    // via the Hub (it already returns hub_unreachable-style failures for anything needing a
    // service that didn't register), not the other ~430 working commands/tools.
    builder.Services.AddMindAtticAuthentication<ProseAuthDbContext>(
        builder.Configuration,
        o =>
        {
            o.AppName = "Prose";
            o.IsProduction = !string.Equals(builder.Environment.EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase);
        });
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[hub] MindAttic.Authentication registration failed, continuing without it - " +
        $"--reset-password will be unavailable via the Hub: {ex.Message}");
}

// Phase 2 migration: tool classes referenced from Prose.Mcp.dll (see ToolDispatch.cs) take
// HubInvoker as a constructor dependency for their thin forwarding methods. The Hub's own
// in-process copy only ever calls the {Name}Impl sibling (never the forwarding method itself),
// so HubInvoker is never actually invoked here - it just needs to resolve so
// ActivatorUtilities.CreateInstance can construct the class at all.
builder.Services.AddHttpClient("ProseHub", c => c.BaseAddress = new Uri("http://127.0.0.1:5900/"));
builder.Services.AddSingleton<Prose.Mcp.HubInvoker>();

// Observability plan (2026-08-20), Part C, Phase 3: makes the Hub's own console output
// observable to remote UIs too, not just the visible window itself (2026-08-21: the window is
// no longer hidden - see start-prose-hub.ps1 + HubConsoleEcho). Two-step registration is
// required for an ILoggerProvider that also needs to be resolvable later (Phase 4 wires its
// OnLine callback to push each line over SignalR) - it must be both a plain singleton AND
// registered into the logging pipeline as the same instance.
builder.Services.AddSingleton<Prose.Hub.Logging.RingBufferLoggerProvider>();
builder.Logging.Services.AddSingleton<Microsoft.Extensions.Logging.ILoggerProvider>(
    sp => sp.GetRequiredService<Prose.Hub.Logging.RingBufferLoggerProvider>());
// EF Core's default Information-level command logging dumps the full SQL text + every
// parameter on every query - found live the moment the ring buffer actually had a reader
// (/api/logs/recent): it would drown out every genuinely useful line. Warning-and-above
// still surfaces real EF problems (e.g. slow-query/exception logging), just not routine
// successful commands.
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", Microsoft.Extensions.Logging.LogLevel.Warning);

// Observability plan, Phase 4: live push transport for logs/DCM/graph deltas. SignalR over
// SSE/polling because the same client code path serves both UI hosts (the Blazor-Server-in-
// Hub web head and the MAUI Blazor Hybrid head) with automatic reconnection built in.
builder.Services.AddSignalR();
builder.Services.AddSingleton<Prose.Hub.ObservabilityBridge>();

// Observability plan, Phase 5: the Hub hosts the shared observability UI directly as an
// interactive Blazor Server web head, at /app - no separate process, since the Hub already
// holds every resident singleton the UI needs to observe. AddProseObserverUi is called with
// this same process's own loopback address; Prose.Maui (a different process, Phase 9) calls
// it with the same base URL from the outside instead.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddProseObserverUi("http://127.0.0.1:5900");

var app = builder.Build();

// Subscribes ONCE to every Phase-3 event and forwards to ObservabilityHub - see
// ObservabilityBridge's own doc comment. Must run after Build() so DI can resolve
// IHubContext<ObservabilityHub> (registered by AddSignalR/MapHub).
app.Services.GetRequiredService<Prose.Hub.ObservabilityBridge>().Wire();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAntiforgery(); // required by MapRazorComponents (Phase 5) - found live via /api/logs/recent
app.MapHub<Prose.Hub.Hubs.ObservabilityHub>("/hubs/observability");
// Observability plan, Phase 5: App.razor declares @page "/app" itself and is the only
// routable component (TabShell does client-side tab switching, not URL routing) - so this
// maps exactly one addressable route without needing a Router/Routes indirection. "/"
// keeps serving the existing static wwwroot/index.html dashboard untouched.
app.MapRazorComponents<Prose.Hub.Components.App>().AddInteractiveServerRenderMode();

var uc = app.Services.GetRequiredService<IUniverseContext>();

// Write-gate (2026-08-22): eagerly resolve so WriteGateBootstrap's constructor actually runs and
// wires the concrete checks/audit service into WriteGateScope — a singleton nobody resolves never
// constructs, and the gate would silently stay a no-op forever. See WriteGateBootstrap's own doc
// comment for why this mirrors the IUniverseContext resolution immediately above.
app.Services.GetRequiredService<Prose.Core.Services.WriteGate.WriteGateBootstrap>();

// Portable-writing-service plan, Phase 1: generate the shared Hub API key once, on first-ever
// startup, and flush it synchronously (not the debounced ScheduleSave path) so it's durably on
// disk in Settings.json before Kestrel starts accepting requests below — a Cli/Mcp process that
// starts even moments later reads the same file and gets a working key immediately, no race.
var hubApiKeySettings = app.Services.GetRequiredService<SettingsService>();
if (string.IsNullOrEmpty(hubApiKeySettings.HubApiKey))
{
    hubApiKeySettings.HubApiKey = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    try { hubApiKeySettings.Flush(); }
    catch (Exception ex) { Console.Error.WriteLine($"[hub] Could not persist the new Hub API key to Settings.json — {ex.Message}"); }
    Console.WriteLine("[hub] Generated a new Hub API key (Settings.json) — Cli/Mcp on this machine pick it up automatically.");
}

Guid? ResolveUniverseId(string slug)
{
    foreach (var u in uc.ListUniverses())
        if (string.Equals(u.Slug, slug, StringComparison.OrdinalIgnoreCase)) return u.Id;
    return null;
}

static Guid DocSessionKey(string? s) =>
    new(System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes("hub-doc:" + (s ?? ""))));

static object NodeDto(UniverseNode n, int edgeCount) => new
{
    id = n.Id,
    name = n.Name,
    nodeType = n.NodeType,
    status = n.Status,
    edgeCount,
    properties = n.Properties,
};

static object EdgeDto(UniverseEdge e) => new
{
    source = e.Source,
    target = e.Target,
    relationType = e.RelationType,
    sentiment = e.Sentiment,
    weight = e.Weight,
};

// Fail-closed contract (Phase 2): "the Hub is up" must mean "the Hub can do work", not just
// "the process didn't crash" - Prose.Cli/Prose.Mcp gate every startup on this endpoint and
// exit immediately if it isn't a clean 200, so a Hub process that's alive but can't reach SQL
// must report unhealthy rather than silently accepting requests it can't actually serve.
app.MapGet("/api/health", async (IDbContextFactory<ProseDbContext> dbFactory) =>
{
    try
    {
        await using var ctx = await dbFactory.CreateDbContextAsync();
        var dbOk = await ctx.Database.CanConnectAsync();
        if (!dbOk)
            return Results.Json(new { status = "unhealthy", reason = "db_unreachable" }, statusCode: 503);
        return Results.Ok(new { status = "ok", time = DateTime.UtcNow });
    }
    catch (Exception ex)
    {
        return Results.Json(new { status = "unhealthy", reason = "db_error", detail = ex.Message }, statusCode: 503);
    }
});

app.MapGet("/api/universes", () =>
    Results.Ok(uc.ListUniverses().Select(u => new { id = u.Id, slug = u.Slug, name = u.Name })));

app.MapGet("/api/universes/{slug}/stats", (string slug, UniverseGraphService graph) =>
{
    var id = ResolveUniverseId(slug);
    if (id == null) return Results.NotFound(new { error = "unknown_universe", slug });
    uc.SetFlowUniverse(id);
    // EnsureFresh(), not EnsureLoaded(): EnsureLoaded() only rebuilds when a universe's
    // GraphState has never been loaded in this process's lifetime (state.Loaded latches true
    // forever after the first call and is never re-checked) — a universe other than whichever
    // one was ambient at Hub startup (the only one RefreshIfStale() ever probes, once, via the
    // DI factory's background Task.Run) would show stale data for the rest of the process's
    // life after any later write. EnsureFresh() re-runs the cheap IsStale() SQL probe on every
    // call instead. Found live: a second RFC 0007 interchange import into EVE left this
    // endpoint stuck at the pre-import node/edge counts. See /snapshot below for the same fix.
    graph.EnsureFresh();
    var result = Results.Ok(new { nodeCount = graph.NodeCount, edgeCount = graph.EdgeCount });
    uc.SetFlowUniverse(null);
    return result;
});

app.MapGet("/api/universes/{slug}/entities/{id}", (string slug, string id, UniverseGraphService graph) =>
{
    var uid = ResolveUniverseId(slug);
    if (uid == null) return Results.NotFound(new { error = "unknown_universe", slug });
    uc.SetFlowUniverse(uid);
    var node = graph.GetNode(id);
    var result = node == null
        ? Results.NotFound(new { error = "not_found", id })
        : Results.Ok(NodeDto(node, graph.GetAllEdges(id).Count));
    uc.SetFlowUniverse(null);
    return result;
});

app.MapGet("/api/universes/{slug}/neighbors/{id}", (string slug, string id, int depth, UniverseGraphService graph) =>
{
    var uid = ResolveUniverseId(slug);
    if (uid == null) return Results.NotFound(new { error = "unknown_universe", slug });
    uc.SetFlowUniverse(uid);
    var d = depth <= 0 ? 1 : depth;
    var nodes = graph.GetNeighbors(id, d);
    var ids = new HashSet<string>(nodes.Select(n => n.Id)) { id };
    var edges = ids.SelectMany(graph.GetAllEdges)
        .Where(e => ids.Contains(e.Source) && ids.Contains(e.Target))
        .DistinctBy(e => (e.Source, e.Target, e.RelationType))
        .ToList();
    var result = Results.Ok(new
    {
        nodes = nodes.Select(n => NodeDto(n, graph.GetAllEdges(n.Id).Count)),
        edges = edges.Select(EdgeDto),
    });
    uc.SetFlowUniverse(null);
    return result;
});

app.MapGet("/api/universes/{slug}/search", (string slug, string q, UniverseGraphService graph) =>
{
    var uid = ResolveUniverseId(slug);
    if (uid == null) return Results.NotFound(new { error = "unknown_universe", slug });
    uc.SetFlowUniverse(uid);
    var matches = graph.AllNodes()
        .Where(n => n.Name.Contains(q ?? "", StringComparison.OrdinalIgnoreCase))
        .Take(50)
        .Select(n => NodeDto(n, graph.GetAllEdges(n.Id).Count));
    var result = Results.Ok(matches);
    uc.SetFlowUniverse(null);
    return result;
});

// scope=active resolves whatever DocContextStack currently holds resident for the given
// nodeCode into its corresponding graph nodes - "what's pertinent to ProseWriter right now",
// reusing DCM's existing tracking rather than a new relevance engine. scope=all dumps the
// whole per-universe graph. Both return the same {nodes, edges} shape as /neighbors.
app.MapGet("/api/universes/{slug}/snapshot", async (
    string slug, string? scope, string? nodeCode,
    UniverseGraphService graph, DocContextStack docStack, IDbContextFactory<ProseDbContext> dbFactory) =>
{
    var uid = ResolveUniverseId(slug);
    if (uid == null) return Results.NotFound(new { error = "unknown_universe", slug });
    uc.SetFlowUniverse(uid);

    List<UniverseNode> nodes;
    if (string.Equals(scope, "all", StringComparison.OrdinalIgnoreCase))
    {
        // EnsureFresh(), not EnsureLoaded() — see the /stats endpoint's comment above for why
        // EnsureLoaded() alone leaves a non-startup-default universe's graph stale forever
        // after the first load. Found live: a second RFC 0007 interchange import into EVE
        // left scope=all stuck at the pre-import node/edge counts.
        graph.EnsureFresh();
        nodes = graph.AllNodes();
    }
    else
    {
        var active = docStack.GetActive(DocSessionKey(nodeCode));
        var docIds = active.Select(a => a.DocId).ToList();
        await using var db = await dbFactory.CreateDbContextAsync();
        var entityIds = await db.Set<MarkdownFile>().AsNoTracking()
            .Where(m => docIds.Contains(m.Id) && m.EntityId != null)
            .Select(m => m.EntityId!.Value)
            .ToListAsync();
        var names = await db.Set<Entity>().AsNoTracking().IgnoreQueryFilters()
            .Where(e => entityIds.Contains(e.Id))
            .Select(e => e.Name)
            .ToListAsync();
        nodes = names
            .Select(n => graph.GetNode(UniverseGraphService.Slugify(n)))
            .Where(n => n != null)
            .Select(n => n!)
            .ToList();
    }

    var ids = new HashSet<string>(nodes.Select(n => n.Id));
    var edges = ids.SelectMany(graph.GetAllEdges)
        .Where(e => ids.Contains(e.Source) && ids.Contains(e.Target))
        .DistinctBy(e => (e.Source, e.Target, e.RelationType))
        .ToList();

    var result = Results.Ok(new
    {
        nodes = nodes.Select(n => NodeDto(n, graph.GetAllEdges(n.Id).Count)),
        edges = edges.Select(EdgeDto),
    });
    uc.SetFlowUniverse(null);
    return result;
});

app.MapGet("/api/dcm/status", (string? nodeCode, DocContextStack docStack) =>
{
    var active = docStack.GetActive(DocSessionKey(nodeCode));
    return Results.Ok(new
    {
        count = active.Count,
        docs = active.Select(e => new { e.Tier, e.Reason, e.RelativePath, e.Score }),
    });
});

// EntityContextStack is the entity-level analog of DocContextStack - the actual
// "DynamicGraphMemory" already in this codebase: entities enter the working set as
// they become relevant (direct mention = depth 0, semantic neighbors = depth 1/2) and
// evict automatically after 4 beats without a mention. This is a live snapshot of that
// resident state, keyed by book/chapter NodeId (not universe slug - EntityContextStack
// tracks per-node, same as the prose engine itself does).
app.MapGet("/api/entities/active", (Guid nodeId, EntityContextStack entityStack) =>
{
    var active = entityStack.GetActive(nodeId);
    return Results.Ok(new
    {
        count = active.Count,
        entities = active.Select(e => new
        {
            entityId = e.EntityId,
            name = e.Name,
            entityType = e.EntityType,
            depth = e.Depth,
            score = e.Score,
            pushedAtBeat = e.PushedAtBeat,
            lastMentionedBeat = e.LastMentionedBeat,
        }),
    });
});

// Observability plan, Phase 4: initial page load / reconnect catch-up for the live-tail
// Logs view - RingBufferLoggerProvider already captures every line the Hub logs (see
// Phase 3); this just snapshots it.
app.MapGet("/api/logs/recent", (int take, Prose.Hub.Logging.RingBufferLoggerProvider logs) =>
    Results.Ok(logs.Recent(take <= 0 ? 200 : take)));

// DCM-Viz history mode: list past runs, newest first.
app.MapGet("/api/dcm/runs", async (IDbContextFactory<ProseDbContext> dbFactory, int? take) =>
{
    await using var db = await dbFactory.CreateDbContextAsync();
    var rows = await db.DcmRuns.AsNoTracking()
        .OrderByDescending(r => r.StartedAt)
        .Take(take is > 0 ? take.Value : 50)
        .ToListAsync();
    return Results.Ok(rows);
});

// DCM-Viz history mode: every persisted beat snapshot for one past run, in beat order.
app.MapGet("/api/dcm/runs/{id:guid}/beats", async (Guid id, IDbContextFactory<ProseDbContext> dbFactory) =>
{
    await using var db = await dbFactory.CreateDbContextAsync();
    var rows = await db.DcmBeatSnapshots.AsNoTracking()
        .Where(b => b.RunId == id)
        .OrderBy(b => b.BeatIndex)
        .ToListAsync();
    return Results.Ok(rows);
});

// DCM-Viz history mode: rebuild the SAME payload shape the live SignalR push sends, from
// persisted DcmBeatSnapshots rows - one JS renderer, no separate history code path.
app.MapGet("/api/dcm/runs/{id:guid}/payload", async (Guid id, IDbContextFactory<ProseDbContext> dbFactory) =>
{
    await using var db = await dbFactory.CreateDbContextAsync();
    var run = await db.DcmRuns.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
    if (run == null) return Results.NotFound(new { error = "run_not_found", id });

    var rows = await db.DcmBeatSnapshots.AsNoTracking()
        .Where(b => b.RunId == id)
        .OrderBy(b => b.BeatIndex)
        .ToListAsync();
    var snapshots = rows.Select(r =>
    {
        var docs = JsonSerializer.Deserialize<List<DcmVisualizationService.DocEntry>>(r.FullActiveSetJson ?? r.DocsJson) ?? [];
        return new DcmVisualizationService.BeatSnapshot(r.BeatIndex, r.BeatTitle, docs);
    }).ToList();

    var json = DcmVisualizationService.BuildPayloadJson(run.NodeSlug, snapshots);
    return Results.Text(json, "application/json");
});

// Stage C: generic CLI command dispatch. Prose.Cli's dispatch blocks forward here instead of
// running their handler in-process - see CliDispatch.cs. Console.Out/Error capture is
// serialized (ConsoleGate) since they're process-wide statics, not per-request.
app.MapPost("/api/cli-invoke", async (CliDispatch.InvokeRequest req, IServiceProvider sp) =>
    await CliDispatch.InvokeAsync(req, sp))
    .AddEndpointFilter<HubApiKeyFilter>();

// Hub-side counterpart to Prose.Cli's CostGateCli (the ~15 cost-estimate-then-confirm
// commands) - see CostGateDispatch.cs for the two-round-trip protocol; the estimator/ledger
// are Hub-resident, only the actual terminal y/n prompt stays client-side.
app.MapPost("/api/cli-cost-gate", async (CostGateDispatch.CostGateRequest req, IServiceProvider sp) =>
    await CostGateDispatch.InvokeAsync(req, sp));

// Phase 2 migration: generic MCP tool dispatch. Prose.Mcp's [McpServerTool] methods forward
// here instead of running their own logic in-process - see ToolDispatch.cs for why this is a
// single generic mechanism instead of ~319 hand-written endpoints.
app.MapPost("/api/mcp-invoke", async (ToolDispatch.InvokeRequest req, IServiceProvider sp) =>
    await ToolDispatch.InvokeAsync(req, sp))
    .AddEndpointFilter<HubApiKeyFilter>();

// The missing generic edge-creation tool (RelationshipDiscoveryService's auto-link path
// doesn't cover every entity type, e.g. Transportation) - writes to SQL first, then applies
// the same edge to the resident in-memory graph immediately so it's visible without waiting
// on the next staleness probe.
app.MapPost("/api/edges", async (EdgeRequest req, UniverseGraphService graph, IDbContextFactory<ProseDbContext> dbFactory) =>
{
    if (req.Source == Guid.Empty || req.Target == Guid.Empty || string.IsNullOrWhiteSpace(req.RelationType))
        return Results.BadRequest(new { error = "source, target, and relationType are required" });

    var uid = req.Universe != null ? ResolveUniverseId(req.Universe) : (Guid?)null;
    if (req.Universe != null && uid == null) return Results.NotFound(new { error = "unknown_universe", req.Universe });
    if (uid != null) uc.SetFlowUniverse(uid);

    await using var db = await dbFactory.CreateDbContextAsync();
    db.Set<Edge>().Add(new Edge
    {
        SourceId = req.Source,
        TargetId = req.Target,
        RelationType = req.RelationType,
        Sentiment = req.Sentiment ?? "neutral",
        Weight = req.Weight ?? 1.0,
        Description = req.Description ?? "",
        Source = "hub-api",
    });
    await db.SaveChangesAsync();

    var refreshed = graph.EnsureFresh();
    if (uid != null) uc.SetFlowUniverse(null);
    return Results.Ok(new { ok = true, graphRefreshed = refreshed });
});

// RFC 0007 "Universe Interchange" §5 — game-side push: ExperimentEve's `npm run universe --
// push` posts its interchange JSON body here. Mirrors the read endpoints' {slug}-in-route
// style; UniverseInterchangeService is self-contained (explicit UniverseId everywhere, no
// ambient-scope dependency) so no uc.SetFlowUniverse dance is needed here.
app.MapPost("/api/universes/{slug}/import", async (string slug, HttpRequest http, UniverseInterchangeService interchange) =>
{
    using var reader = new StreamReader(http.Body);
    var json = await reader.ReadToEndAsync();
    var result = await interchange.ImportAsync(json, slug);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
})
    .AddEndpointFilter<HubApiKeyFilter>();

// RFC 0007 §5 "The Outbox (CliHook channel)" — the Hub's outbound message queue toward other
// MindAttic apps' Claude Code sessions. GET drains (marks delivered) unless ?peek=true;
// POST enqueues (used both by this session deliberately messaging a consumer, and by
// services auto-enqueuing on import/export/beat-write completion).
app.MapGet("/api/outbox/{consumer}", async (string consumer, bool? peek, OutboxService outbox) =>
{
    var events = await outbox.DrainAsync(consumer, peek ?? false);
    return Results.Ok(events.Select(e => new { id = e.Id, ts = e.Ts, kind = e.Kind, summary = e.Summary, data = e.DataJson }));
})
    .AddEndpointFilter<HubApiKeyFilter>();

app.MapPost("/api/outbox/{consumer}", async (string consumer, OutboxEnqueueRequest req, OutboxService outbox) =>
{
    if (string.IsNullOrWhiteSpace(req.Kind) || string.IsNullOrWhiteSpace(req.Summary))
        return Results.BadRequest(new { error = "kind and summary are required" });
    var ev = await outbox.EnqueueAsync(consumer, req.Kind, req.Summary, req.Data);
    return Results.Ok(new { id = ev.Id, ts = ev.Ts });
})
    .AddEndpointFilter<HubApiKeyFilter>();

// Portable-writing-service plan, Phase 3: write a scene/line of dialog without a pre-existing
// Book/Chapter/Beat row, over plain HTTP (same OneShotGenerationService the CLI/MCP entry points
// call — see OneShotGenerateCli/generate_scene's doc comments for the ephemeral-vs-attached
// design). Calls the service directly, in-process, rather than bouncing through the generic
// CliDispatch/ToolDispatch reflection dispatchers — this is one named, stable operation.
app.MapPost("/api/generate-scene", async (GenerateSceneRequest req, OneShotGenerationService generation) =>
{
    try
    {
        var result = await generation.GenerateAsync(new OneShotGenerationService.OneShotGenerationRequest(
            BeatGoal: req.BeatGoal,
            Characters: req.Characters,
            Location: req.Location,
            Subtext: req.Subtext,
            Node: req.Node,
            Universe: req.Universe,
            BeatIndex: req.BeatIndex ?? 0,
            TotalBeats: req.TotalBeats ?? 0));
        return Results.Ok(new
        {
            text = result.Text,
            wordCount = result.WordCount,
            universe = result.UniverseSlug,
            attachedNode = result.AttachedNodeSlug,
        });
    }
    catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
    {
        return Results.BadRequest(new { error = "generate_scene_failed", detail = ex.Message });
    }
})
    .AddEndpointFilter<HubApiKeyFilter>();

Console.Title = "Prose Hub — http://127.0.0.1:5900";
HubConsoleEcho.Out.WriteLine();
HubConsoleEcho.Out.WriteLine("========================================================");
HubConsoleEcho.Out.WriteLine(" Prose Hub is running — http://127.0.0.1:5900");
HubConsoleEcho.Out.WriteLine(" Every CLI/MCP command is echoed below as it runs.");
HubConsoleEcho.Out.WriteLine("========================================================");
HubConsoleEcho.Out.WriteLine();

app.Run();

sealed record EdgeRequest(Guid Source, Guid Target, string RelationType, string? Sentiment, double? Weight, string? Description, string? Universe);

sealed record OutboxEnqueueRequest(string Kind, string Summary, object? Data);

sealed record GenerateSceneRequest(
    string BeatGoal, string[]? Characters, string? Location, string? Subtext,
    string? Node, string? Universe, int? BeatIndex, int? TotalBeats);
