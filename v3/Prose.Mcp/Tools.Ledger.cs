using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Mcp;

// ── Command Ledger + Decision Ledger (observability plan, 2026-08-20) ─────────
// The durable answer to "don't depend on fading context memory": every CLI/MCP/cost-gated
// call Prose.Hub executes is already logged automatically by CliDispatch/ToolDispatch
// (CommandLedgerEntry). LogDecision is the explicit, bidirectional half — any LLM (this
// assistant included) can write a structured, permanent row for a higher-level decision
// or piece of reasoning, then any session (including a totally fresh one) can read either
// ledger back via CommandLog/DecisionLog instead of relying on chat history.

[McpServerToolType]
public class LedgerTools
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly LoggingService logging;
    private readonly HubInvoker hub;

    public LedgerTools(IDbContextFactory<ProseDbContext> dbFactory, LoggingService logging, HubInvoker hub)
    {
        this.dbFactory = dbFactory;
        this.logging   = logging;
        this.hub       = hub;
    }

    [McpServerTool, Description(
        "Write a Decision Ledger row — a durable, structured record of a higher-level decision or " +
        "piece of reasoning (not a mechanical command call; those are logged automatically). Use this " +
        "to make your own reasoning survive past this conversation's memory: a fresh session can query " +
        "it back via decision_log.")]
    public Task<string> LogDecision(
        [Description("One-line summary of the decision.")] string summary,
        [Description("The 'why' behind it.")] string? rationale = null,
        [Description("e.g. 'architecture', 'bugfix', 'canon-change'.")] string? category = null,
        [Description("This assistant's session id, if known.")] string? sessionId = null,
        [Description("Who's recording this, e.g. 'claude-code'. Defaults to 'claude-code'.")] string? actor = null,
        [Description("CommandLedgerEntry ids this decision grew out of, comma-separated.")] string? relatedCommandIds = null) =>
        hub.InvokeAsync(nameof(LedgerTools), nameof(LogDecisionImpl), new { summary, rationale, category, sessionId, actor, relatedCommandIds });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> LogDecisionImpl(string summary, string? rationale, string? category, string? sessionId, string? actor, string? relatedCommandIds)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        string? relatedJson = null;
        if (!string.IsNullOrWhiteSpace(relatedCommandIds))
        {
            var ids = relatedCommandIds.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            relatedJson = JsonSerializer.Serialize(ids);
        }
        var entry = new DecisionLedgerEntry
        {
            SessionId = sessionId,
            Summary = summary,
            Rationale = rationale,
            Category = category,
            Actor = actor ?? "claude-code",
            RelatedCommandIdsJson = relatedJson,
        };
        db.DecisionLedgerEntries.Add(entry);
        await db.SaveChangesAsync();
        return JsonSerializer.Serialize(new { id = entry.Id, at = entry.At });
    }

    [McpServerTool, Description(
        "Read back the Command Ledger — every CLI/MCP/cost-gated call Prose.Hub has executed, with " +
        "args, exit code, duration, and error (if any). The mechanical half of the durable audit trail.")]
    public Task<string> CommandLog(
        [Description("ISO-8601 datetime; only rows at/after this time.")] string? since = null,
        [Description("Filter to one handler class, e.g. 'BeatCli'.")] string? handler = null,
        [Description("Max rows to return (default 50).")] int take = 50) =>
        hub.InvokeAsync(nameof(LedgerTools), nameof(CommandLogImpl), new { since, handler, take });

    public async Task<string> CommandLogImpl(string? since, string? handler, int take)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var query = db.CommandLedgerEntries.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(since) && DateTime.TryParse(since, out var sinceDt))
            query = query.Where(e => e.At >= sinceDt.ToUniversalTime());
        if (!string.IsNullOrWhiteSpace(handler))
            query = query.Where(e => e.HandlerClass == handler);
        var rows = await query.OrderByDescending(e => e.At).Take(take).ToListAsync();
        return JsonSerializer.Serialize(rows);
    }

    [McpServerTool, Description(
        "Read back the Decision Ledger written by log_decision — structured, permanent decision/" +
        "reasoning records. Query this at the start of a fresh session instead of relying on chat " +
        "history to reconstruct what was decided and why.")]
    public Task<string> DecisionLog(
        [Description("ISO-8601 datetime; only rows at/after this time.")] string? since = null,
        [Description("Filter to one session id.")] string? sessionId = null,
        [Description("Max rows to return (default 50).")] int take = 50) =>
        hub.InvokeAsync(nameof(LedgerTools), nameof(DecisionLogImpl), new { since, sessionId, take });

    public async Task<string> DecisionLogImpl(string? since, string? sessionId, int take)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var query = db.DecisionLedgerEntries.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(since) && DateTime.TryParse(since, out var sinceDt))
            query = query.Where(e => e.At >= sinceDt.ToUniversalTime());
        if (!string.IsNullOrWhiteSpace(sessionId))
            query = query.Where(e => e.SessionId == sessionId);
        var rows = await query.OrderByDescending(e => e.At).Take(take).ToListAsync();
        return JsonSerializer.Serialize(rows);
    }

    [McpServerTool, Description(
        "Search the Hub's durable log history (Serilog daily files on disk, not the live " +
        "in-memory tail) by time range, minimum severity, and/or free text. This is the " +
        "already-existing LoggingService.Search, the same mechanism the old Codex Logging " +
        "page used, now exposed through the Hub.")]
    public Task<string> SearchLogs(
        [Description("ISO-8601 datetime; only entries at/after this time. Defaults to 1 day ago.")] string? since = null,
        [Description("Minimum severity: Verbose|Debug|Information|Warning|Error|Fatal.")] string? severity = null,
        [Description("Free-text filter over message/exception.")] string? text = null,
        [Description("Max entries to return (default 200).")] int take = 200) =>
        hub.InvokeAsync(nameof(LedgerTools), nameof(SearchLogsImpl), new { since, severity, text, take });

    public string SearchLogsImpl(string? since, string? severity, string? text, int take)
    {
        var results = logging.Search(new LogSearchRequest
        {
            Since = !string.IsNullOrWhiteSpace(since) && DateTime.TryParse(since, out var s) ? s : null,
            MinSeverity = severity,
            SearchText = text,
            MaxResults = take,
        });
        return JsonSerializer.Serialize(results);
    }
}
