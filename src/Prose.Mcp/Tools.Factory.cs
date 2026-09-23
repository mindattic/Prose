using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;
using Prose.Core.Services.Factory;

namespace Prose.Mcp;

// ── The Novel Factory (RFC 0015) ──────────────────────────────────────────────
// State lives in the database; status is computed; every call is logged in the command ledger.
// A session asks the factory what to do next instead of working from memory.
//
//   factory_status      — the unit × station matrix for a book
//   factory_next        — the single next action and the exact calls that clear it
//   work_order_add/list/close/abandon — engine and author work, closed only by Hub-validated checks
//   session_end         — end the session; every decision must reference a ruling or work order
// CLI twin: prose --factory / --order / --session (works before an MCP restart).

[McpServerToolType]
public class FactoryTools(
    FactoryService factory,
    WorkOrderService orders,
    FactorySessionService sessions,
    IDbContextFactory<ProseDbContext> dbFactory,
    HubInvoker hub)
{
    static readonly JsonSerializerOptions JsonOpts = CanonTools.JsonOpts;

    Task<Guid?> Resolve(string? r) => string.IsNullOrWhiteSpace(r) ? Task.FromResult<Guid?>(null) : NodeRefResolver.ResolveAsync(dbFactory, r);

    [McpServerTool, Description("The Novel Factory's matrix for one book: every unit (chapter, as the exporters print it) × every station (F2 Planned, F3 Written, F4 Captured, F5 Read, F6 Clean, F1 Verified) plus the book stations (F7 Pressed, A Audio). Every verdict is computed from the prose, the world and the read receipts; a station not built yet says not-built.")]
    public Task<string> factory_status([Description("Book id, slug or NodeCode.")] string nodeIdOrSlug) =>
        hub.InvokeAsync(nameof(FactoryTools), nameof(FactoryStatusImpl), new { nodeIdOrSlug });

    public async Task<string> FactoryStatusImpl(string nodeIdOrSlug)
    {
        if (await Resolve(nodeIdOrSlug) is not { } id) return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);
        var s = await factory.StatusAsync(id);
        return JsonSerializer.Serialize(new
        {
            book = new { s.BookId, s.Slug, s.Code, s.Title, s.Beats, s.Words, units = s.Units.Count },
            stations = FactoryService.UnitStationOrder.ToDictionary(c => c, c => FactoryService.Built.Contains(c)
                ? $"{s.Units.Count(u => u.Stations[c].Pass)}/{s.Units.Count} pass" : "not built"),
            bookStations = s.BookStations.ToDictionary(kv => kv.Key, kv => new { kv.Value.State, kv.Value.Detail }),
            units = s.Units.Select(u => new
            {
                u.Unit.Ordinal, u.Unit.Heading, positions = $"{u.Unit.FirstPosition}-{u.Unit.LastPosition}", beats = u.Unit.BeatIds.Count,
                stations = u.Stations.Where(kv => kv.Value.State != "pass").ToDictionary(kv => kv.Key, kv => kv.Value.State == "not-built" ? "not built" : kv.Value.Detail),
            }),
        }, JsonOpts);
    }

    [McpServerTool, Description("The single next action, computed: the first open blocking work order (during the factory's own build), otherwise the first failing station of the earliest unit of a book an author order has put on the line — with the exact calls that clear it. Ask this instead of deciding from memory.")]
    public Task<string> factory_next([Description("Optional book id, slug or NodeCode to ask about one book only.")] string? nodeIdOrSlug = null) =>
        hub.InvokeAsync(nameof(FactoryTools), nameof(FactoryNextImpl), new { nodeIdOrSlug });

    public async Task<string> FactoryNextImpl(string? nodeIdOrSlug = null)
    {
        var next = await factory.NextAsync(await Resolve(nodeIdOrSlug));
        return JsonSerializer.Serialize(next, JsonOpts);
    }

    [McpServerTool, Description("Open a work order. kind engine = code or docs in the repo (must declare paths; must descend from an author-approved root); kind author = a request about a book (pass nodeIdOrSlug to put the book on the factory line). checksJson is an array of typed checks validated Hub-side at close: commit, tests {names[]}, ledger {handler, method?, minCalls}, factory {book, station}, deploy, author.")]
    public Task<string> work_order_add(
        [Description("engine | author")] string kind,
        [Description("Short title.")] string title,
        [Description("Parent order id (required unless this is a root the author approved).")] string? parentId = null,
        [Description("Roots only: 'author' — only when the author has approved this root.")] string? rootApprovedBy = null,
        [Description("Book for an author order.")] string? nodeIdOrSlug = null,
        [Description("Semicolon-separated repo-relative globs the order may touch (engine).")] string? paths = null,
        [Description("JSON array of checks.")] string? checksJson = null,
        [Description("True = this order is the factory's next action until it closes.")] bool blocking = false,
        [Description("Longer description.")] string? detail = null) =>
        hub.InvokeAsync(nameof(FactoryTools), nameof(WorkOrderAddImpl), new { kind, title, parentId, rootApprovedBy, nodeIdOrSlug, paths, checksJson, blocking, detail });

    public async Task<string> WorkOrderAddImpl(string kind, string title, string? parentId = null, string? rootApprovedBy = null,
        string? nodeIdOrSlug = null, string? paths = null, string? checksJson = null, bool blocking = false, string? detail = null)
    {
        try
        {
            var row = await orders.AddAsync(new WorkOrderDraft(kind, title, detail,
                Guid.TryParse(parentId, out var p) ? p : null, rootApprovedBy, await Resolve(nodeIdOrSlug),
                (paths ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                checksJson ?? "[]", blocking), await sessions.CurrentSessionIdAsync());
            return JsonSerializer.Serialize(new { ok = true, row.Id, row.Kind, row.Title, row.Blocking }, JsonOpts);
        }
        catch (ArgumentException ex) { return JsonSerializer.Serialize(new { ok = false, error = ex.Message }, JsonOpts); }
    }

    [McpServerTool, Description("List work orders as a tree (depth-first, in the order the factory works them).")]
    public Task<string> work_order_list([Description("open (default) | closed | abandoned | all")] string status = "open", [Description("engine | author")] string? kind = null) =>
        hub.InvokeAsync(nameof(FactoryTools), nameof(WorkOrderListImpl), new { status, kind });

    public async Task<string> WorkOrderListImpl(string status = "open", string? kind = null)
    {
        var rows = await orders.ListAsync(status, kind);
        return JsonSerializer.Serialize(rows.Select(r => new { r.Id, r.ParentId, r.Kind, r.Status, r.Blocking, r.Title, r.NodeId, r.ClosedAt }), JsonOpts);
    }

    [McpServerTool, Description("Close a work order. The Hub validates every check itself (commit on HEAD within declared paths, TRX tests passed, real ledger use, station passes, Hub redeployed, author confirmation relayed). Refused — and the order stays open — if any check fails.")]
    public Task<string> work_order_close(
        [Description("Order id.")] string id,
        [Description("Commit hash for a commit check.")] string? commitHash = null,
        [Description("Path to a dotnet test TRX file for a tests check.")] string? trxPath = null,
        [Description("The author's words, for an author check (trust point: relayed).")] string? authorConfirmation = null,
        [Description("Optional note.")] string? note = null) =>
        hub.InvokeAsync(nameof(FactoryTools), nameof(WorkOrderCloseImpl), new { id, commitHash, trxPath, authorConfirmation, note });

    public async Task<string> WorkOrderCloseImpl(string id, string? commitHash = null, string? trxPath = null, string? authorConfirmation = null, string? note = null)
    {
        if (!Guid.TryParse(id, out var gid)) return JsonSerializer.Serialize(new { ok = false, error = "bad_id" }, JsonOpts);
        var r = await orders.CloseAsync(gid, new CloseInputs(commitHash, trxPath, authorConfirmation, note));
        return JsonSerializer.Serialize(new { ok = r.Closed, refusal = r.Refusal, checks = r.Checks, autoClosedParents = r.AutoClosedParents }, JsonOpts);
    }

    [McpServerTool, Description("Abandon an open work order. A reason is required and recorded.")]
    public Task<string> work_order_abandon([Description("Order id.")] string id, [Description("Why.")] string reason) =>
        hub.InvokeAsync(nameof(FactoryTools), nameof(WorkOrderAbandonImpl), new { id, reason });

    public async Task<string> WorkOrderAbandonImpl(string id, string reason)
    {
        if (!Guid.TryParse(id, out var gid)) return JsonSerializer.Serialize(new { ok = false, error = "bad_id" }, JsonOpts);
        try { return JsonSerializer.Serialize(new { ok = await orders.AbandonAsync(gid, reason) }, JsonOpts); }
        catch (ArgumentException ex) { return JsonSerializer.Serialize(new { ok = false, error = ex.Message }, JsonOpts); }
    }

    [McpServerTool, Description("End the current factory session (what /quicksave does). summaryJson = {done:[...], decisions:[{text, rulingId|orderId}], next:\"...\"}. Refused while any decision is not backed by a ruling or work order recorded this session.")]
    public Task<string> session_end([Description("The summary JSON.")] string summaryJson, [Description("Optional session id (defaults to the open one).")] string? sessionId = null) =>
        hub.InvokeAsync(nameof(FactoryTools), nameof(SessionEndImpl), new { summaryJson, sessionId });

    public async Task<string> SessionEndImpl(string summaryJson, string? sessionId = null)
    {
        var (ok, problems, id) = await sessions.EndAsync(Guid.TryParse(sessionId, out var s) ? s : null, summaryJson, GitProbe.Head(GitProbe.RepoPath));
        return JsonSerializer.Serialize(new { ok, sessionId = id, problems }, JsonOpts);
    }
}
