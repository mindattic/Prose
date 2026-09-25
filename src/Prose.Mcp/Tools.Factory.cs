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
//   factory_context     — the writer's working memory for one unit (one derived file + manifest)
//   factory_journal     — what happened in a window, from the ledger, the factory rows and temporal history
//   factory_capture     — station F4's worklist: names the world does not hold, known names left untagged
//   factory_usage       — the use-or-delete check over every [FactoryTool]
//   work_order_add/list/close/abandon — engine and author work, closed only by Hub-validated checks
//   session_end         — end the session; every decision must reference a ruling or work order
// CLI twin: prose --factory / --order / --session (works before an MCP restart).
// Every …Impl carries [FactoryTool]: the usage check files "Use or delete" for one no one calls.

[McpServerToolType]
public class FactoryTools(
    FactoryService factory,
    WorkOrderService orders,
    FactorySessionService sessions,
    ContextBundleService context,
    FactoryJournal journal,
    CaptureScanner capture,
    FactoryUsageCheck usage,
    IDbContextFactory<ProseDbContext> dbFactory,
    HubInvoker hub)
{
    static readonly JsonSerializerOptions JsonOpts = CanonTools.JsonOpts;

    Task<Guid?> Resolve(string? r) => string.IsNullOrWhiteSpace(r) ? Task.FromResult<Guid?>(null) : NodeRefResolver.ResolveAsync(dbFactory, r);

    [McpServerTool, Description("The Novel Factory's matrix for one book: every unit (chapter, as the exporters print it) × every station (F2 Planned, F3 Written, F4 Captured, F5 Read, F6 Clean, F1 Verified) plus the book stations (F7 Pressed, A Audio). Every verdict is computed from the prose, the world and the read receipts; a station not built yet says not-built.")]
    public Task<string> factory_status([Description("Book id, slug or NodeCode.")] string nodeIdOrSlug) =>
        hub.InvokeAsync(nameof(FactoryTools), nameof(FactoryStatusImpl), new { nodeIdOrSlug });

    [FactoryTool("factory_status", "2026-09-23", Cli = "FactoryCli --factory status")]
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

    [FactoryTool("factory_next", "2026-09-23", Cli = "FactoryCli --factory next")]
    public async Task<string> FactoryNextImpl(string? nodeIdOrSlug = null)
    {
        var bookId = await Resolve(nodeIdOrSlug);
        // A mistyped book used to fall through to the whole factory's next action, answered as if
        // it were that book's.
        if (!string.IsNullOrWhiteSpace(nodeIdOrSlug) && bookId == null)
            return JsonSerializer.Serialize(new { ok = false, error = "node_not_found", nodeIdOrSlug }, JsonOpts);
        var next = await factory.NextAsync(bookId);
        return JsonSerializer.Serialize(next, JsonOpts);
    }

    [McpServerTool, Description("The writer's working memory for one unit (RFC 0015 §3.9), rebuilt on every call and never edited. Writes two files: {slug}.world.md — the book's law and the universe's world and craft canon, the same for every unit (read it once per session) — and {slug}.context.md, within the budget: the law, the records of every entity the unit tags as they stand now, the prose before the unit (the previous unit, or with priorUnits 'all' every preceding unit that fits, oldest dropped first, never cut at a chapter boundary) and the unit itself (its prose, or its planned beats). Returns both paths and a manifest of exactly what they hold and what was left out.")]
    public Task<string> factory_context(
        [Description("Book id, slug or NodeCode.")] string nodeIdOrSlug,
        [Description("Unit ordinal, as factory_status prints it.")] int unit,
        [Description("'1' (default) = the previous unit; 'all' = every preceding unit within the budget.")] string priorUnits = "1",
        [Description("Character budget for the whole bundle (default 400000).")] int budgetChars = ContextBundleService.DefaultBudget) =>
        hub.InvokeAsync(nameof(FactoryTools), nameof(FactoryContextImpl), new { nodeIdOrSlug, unit, priorUnits, budgetChars });

    [FactoryTool("factory_context", "2026-09-23", Cli = "FactoryCli --factory context")]
    public async Task<string> FactoryContextImpl(string nodeIdOrSlug, int unit, string priorUnits = "1", int budgetChars = ContextBundleService.DefaultBudget)
    {
        if (await Resolve(nodeIdOrSlug) is not { } id) return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);
        try
        {
            var m = await context.BuildAsync(id, unit, string.Equals(priorUnits, "all", StringComparison.OrdinalIgnoreCase), budgetChars);
            return JsonSerializer.Serialize(m, JsonOpts);
        }
        catch (ArgumentException ex) { return JsonSerializer.Serialize(new { error = ex.Message }, JsonOpts); }
    }

    [McpServerTool, Description("What happened in a window, reconstructed from the records themselves, in time order: every Hub call in the command ledger (with the session that made it), every read receipt, read note, entity verification, press, ruling, work order and session written, and — on SQL Server — every stored version of a beat or an entity from the temporal history. With a book: only what touched it. Nothing here had to be remembered to be written.")]
    public Task<string> factory_journal(
        [Description("Start of the window, ISO 8601 (UTC unless it carries an offset).")] string since,
        [Description("End of the window (default now).")] string? until = null,
        [Description("Optional book id, slug or NodeCode.")] string? nodeIdOrSlug = null,
        [Description("Most events to return, newest kept (default 400). Counts by kind always cover the whole window.")] int limit = 400) =>
        hub.InvokeAsync(nameof(FactoryTools), nameof(FactoryJournalImpl), new { since, until, nodeIdOrSlug, limit });

    [FactoryTool("factory_journal", "2026-09-23", Cli = "FactoryCli --factory journal")]
    public async Task<string> FactoryJournalImpl(string since, string? until = null, string? nodeIdOrSlug = null, int limit = 400)
    {
        if (!FactoryJournal.TryParseInstant(since, out var from)) return JsonSerializer.Serialize(new { error = "bad_since", since }, JsonOpts);
        DateTime? to = null;
        if (!string.IsNullOrWhiteSpace(until))
        {
            if (!FactoryJournal.TryParseInstant(until, out var u)) return JsonSerializer.Serialize(new { error = "bad_until", until }, JsonOpts);
            to = u;
        }
        Guid? book = null;
        if (!string.IsNullOrWhiteSpace(nodeIdOrSlug))
        {
            if (await Resolve(nodeIdOrSlug) is not { } b) return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);
            book = b;
        }
        var j = await journal.ReadAsync(from, to, book);
        var kept = j.Events.Skip(Math.Max(0, j.Events.Count - Math.Max(1, limit))).ToList();
        return JsonSerializer.Serialize(new
        {
            j.Since, j.Until, total = j.Events.Count, truncated = kept.Count < j.Events.Count,
            byKind = j.Events.GroupBy(e => e.Kind).ToDictionary(g => g.Key, g => g.Count()),
            temporalHistoryRead = j.TemporalHistoryRead, temporalNote = j.TemporalNote,
            events = kept,
        }, JsonOpts);
    }

    [McpServerTool, Description("Station F4's worklist for a book: (a) capitalized names used in 2+ beats that no entity, alias or incidental ruling accounts for, and (b) uses of a book-tagged entity's name in a beat that does not tag it. Resolve (a) with create_* / an alias / record_ruling(kind: incidental), and (b) with tags (CLI: prose --factory capture --node X --retag | --pin | --pin-name \"<name>\" --entity <id>).")]
    public Task<string> factory_capture([Description("Book id, slug or NodeCode.")] string nodeIdOrSlug, [Description("Most items per list (default 100).")] int limit = 100) =>
        hub.InvokeAsync(nameof(FactoryTools), nameof(FactoryCaptureImpl), new { nodeIdOrSlug, limit });

    [FactoryTool("factory_capture", "2026-09-23", Cli = "FactoryCli --factory capture")]
    public async Task<string> FactoryCaptureImpl(string nodeIdOrSlug, int limit = 100)
    {
        if (await Resolve(nodeIdOrSlug) is not { } id) return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);
        var r = await capture.ScanAsync(id);
        return JsonSerializer.Serialize(new
        {
            r.BeatsScanned, r.Candidates, r.Millis,
            unresolved = r.Unresolved.Take(limit).Select(n => new { n.Name, n.Beats, beatNumbers = n.Numbers.Take(8), n.BookSays, n.BookSaysName, n.BookSaysType }),
            unresolvedTotal = r.Unresolved.Count,
            untagged = r.Untagged.GroupBy(t => (t.EntityId, t.EntityName)).OrderByDescending(g => g.Sum(t => t.Missing)).Take(limit)
                .Select(g => new { entityId = g.Key.EntityId, entity = g.Key.EntityName, missing = g.Sum(t => t.Missing), beatNumbers = g.Select(t => t.Number).Take(8) }),
            untaggedTotal = r.Untagged.Sum(t => t.Missing),
        }, JsonOpts);
    }

    [McpServerTool, Description("The use-or-delete check: every factory tool, the day it shipped, its real calls in the command ledger (either door, MCP or CLI) and its verdict. A tool with no real call seven days after it shipped gets one engine order, \"Use or delete: <tool>\", under the RFC's approved root; that order closes only by use, or is abandoned with the commit that deleted the tool. Runs at every session start.")]
    public Task<string> factory_usage([Description("False = report only, file nothing.")] bool fileOrders = true) =>
        hub.InvokeAsync(nameof(FactoryTools), nameof(FactoryUsageImpl), new { fileOrders });

    [FactoryTool("factory_usage", "2026-09-23", Cli = "FactoryCli --factory usage")]
    public async Task<string> FactoryUsageImpl(bool fileOrders = true)
    {
        var rows = await usage.RunAsync(fileOrders: fileOrders);
        return JsonSerializer.Serialize(rows, JsonOpts);
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

    [FactoryTool("work_order_add", "2026-09-23", Cli = "FactoryCli --order add")]
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

    [FactoryTool("work_order_list", "2026-09-23", Cli = "FactoryCli --order list")]
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

    [FactoryTool("work_order_close", "2026-09-23", Cli = "FactoryCli --order close")]
    public async Task<string> WorkOrderCloseImpl(string id, string? commitHash = null, string? trxPath = null, string? authorConfirmation = null, string? note = null)
    {
        if (!Guid.TryParse(id, out var gid)) return JsonSerializer.Serialize(new { ok = false, error = "bad_id" }, JsonOpts);
        var r = await orders.CloseAsync(gid, new CloseInputs(commitHash, trxPath, authorConfirmation, note));
        return JsonSerializer.Serialize(new { ok = r.Closed, refusal = r.Refusal, checks = r.Checks, autoClosedParents = r.AutoClosedParents }, JsonOpts);
    }

    [McpServerTool, Description("Abandon an open work order. A reason is required and recorded.")]
    public Task<string> work_order_abandon([Description("Order id.")] string id, [Description("Why.")] string reason) =>
        hub.InvokeAsync(nameof(FactoryTools), nameof(WorkOrderAbandonImpl), new { id, reason });

    [FactoryTool("work_order_abandon", "2026-09-23", Cli = "FactoryCli --order abandon")]
    public async Task<string> WorkOrderAbandonImpl(string id, string reason)
    {
        if (!Guid.TryParse(id, out var gid)) return JsonSerializer.Serialize(new { ok = false, error = "bad_id" }, JsonOpts);
        try { return JsonSerializer.Serialize(new { ok = await orders.AbandonAsync(gid, reason) }, JsonOpts); }
        catch (ArgumentException ex) { return JsonSerializer.Serialize(new { ok = false, error = ex.Message }, JsonOpts); }
    }

    [McpServerTool, Description("End the current factory session (what /quicksave does). summaryJson = {done:[...], decisions:[{text, rulingId|orderId}], next:\"...\"}. Refused while any decision is not backed by a ruling or work order recorded this session.")]
    public Task<string> session_end([Description("The summary JSON.")] string summaryJson, [Description("The session id the start hook printed as THIS SESSION. May be omitted only when exactly one session is open; with several open the end is refused.")] string? sessionId = null) =>
        hub.InvokeAsync(nameof(FactoryTools), nameof(SessionEndImpl), new { summaryJson, sessionId });

    [FactoryTool("session_end", "2026-09-23", Cli = "FactoryCli --session end")]
    public async Task<string> SessionEndImpl(string summaryJson, string? sessionId = null)
    {
        // A malformed id must not fall back to "the one open session": that ends someone else's.
        Guid? sid = null;
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            if (!Guid.TryParse(sessionId, out var s))
                return JsonSerializer.Serialize(new { ok = false, error = "invalid_session_id", sessionId }, JsonOpts);
            sid = s;
        }
        var (ok, problems, id) = await sessions.EndAsync(sid, summaryJson, GitProbe.Head(GitProbe.RepoPath));
        return JsonSerializer.Serialize(new { ok, sessionId = id, problems }, JsonOpts);
    }
}
