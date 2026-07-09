using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Mcp;

// ── Markdown file backup / restore tools ─────────────────────────────────
// Surfaces the MarkdownFileService so Claude Code can sync project-rules,
// Codex docs, and memory files to the DB and restore them by timestamp.
// ─────────────────────────────────────────────────────────────────────────

[McpServerToolType]
public class ConfigTools
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    private readonly MarkdownFileService svc;
    private readonly DocContextService docContext;
    private readonly TokenLedger ledger;

    public ConfigTools(MarkdownFileService svc, DocContextService docContext, TokenLedger ledger)
    {
        this.svc = svc;
        this.docContext = docContext;
        this.ledger = ledger;
    }

    [McpServerTool, Description(
        "Show the running token cost tally for the current MCP server session. " +
        "Returns call count, input/output token estimates, and USD cost broken down by model. " +
        "Token counts are estimated from text length (chars / 4) since the Legion transport " +
        "does not expose Anthropic usage objects. Pass reset=true to clear the ledger.")]
    public string GetCostReport(
        [Description("If true, clear the ledger after reporting. Default false.")] bool reset = false)
    {
        var summary = ledger.GetSummary();
        var result  = JsonSerializer.Serialize(new
        {
            sessionStart  = summary.SessionStart,
            callCount     = summary.CallCount,
            inputTokens   = summary.InputTokens,
            outputTokens  = summary.OutputTokens,
            totalCostUsd  = Math.Round(summary.TotalCost, 6),
            note          = "Token counts estimated via chars/4 heuristic; actual API costs may differ.",
            byModel = summary.ByModel.Values
                .OrderByDescending(m => m.TotalCost)
                .Select(m => new
                {
                    model        = m.Model,
                    label        = m.Label,
                    callCount    = m.CallCount,
                    inputTokens  = m.InputTokens,
                    outputTokens = m.OutputTokens,
                    totalCostUsd = Math.Round(m.TotalCost, 6),
                }),
        }, new JsonSerializerOptions { WriteIndented = true });

        if (reset) ledger.Clear();
        return result;
    }

    // Deterministic LRU context key per node (or a shared default) so repeated calls within
    // the MCP server process share the same rotating working set.
    private static Guid SessionKey(string? s) =>
        new(System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes("mcp-doc:" + (s ?? ""))));

    [McpServerTool, Description(
        "List all markdown files tracked in the database (project rules, Codex docs, Claude Code memory). " +
        "Returns category, relativePath, contentHash, and lastSyncedAt for each file.")]
    public async Task<string> ListMarkdownFiles()
    {
        var rows = await svc.ListAsync();
        var result = rows.Select(r => new
        {
            category     = r.Category,
            relativePath = r.RelativePath,
            fileRoot     = r.FileRoot,
            hash         = r.ContentHash.Length >= 12 ? r.ContentHash[..12] + "…" : r.ContentHash,
            lastSynced   = r.LastSyncedAt.ToString("u"),
        }).ToList();
        return JsonSerializer.Serialize(new { count = result.Count, files = result }, JsonOpts);
    }

    [McpServerTool, Description(
        "Get the content of a tracked markdown file from the database. " +
        "Pass asOf (ISO 8601 UTC) to retrieve a historical version from the temporal table. " +
        "relativePath examples: 'CLAUDE.md', 'docs/BIBLE.md', 'feedback_sequential_node_writing.md'")]
    public async Task<string> GetMarkdownFile(
        [Description("Relative path key, e.g. 'CLAUDE.md' or 'docs/AMENDMENTS.md'.")] string relativePath,
        [Description("Optional ISO 8601 UTC datetime to retrieve the version current at that moment.")] string? asOf = null)
    {
        DateTime? asOfDt = null;
        if (asOf != null)
        {
            if (!DateTime.TryParse(asOf, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
                return JsonSerializer.Serialize(new { error = "invalid_as_of", hint = "use ISO 8601, e.g. 2026-06-01T00:00:00Z" }, JsonOpts);
            asOfDt = parsed.ToUniversalTime();
        }

        var row = await svc.GetAsync(relativePath, asOfDt);
        if (row == null)
            return JsonSerializer.Serialize(new { error = "not_found", relativePath }, JsonOpts);

        return JsonSerializer.Serialize(new
        {
            relativePath = row.RelativePath,
            category     = row.Category,
            fileRoot     = row.FileRoot,
            lastSynced   = row.LastSyncedAt.ToString("u"),
            content      = row.Content,
        }, JsonOpts);
    }

    [McpServerTool, Description(
        "Prepare the Doc Context Stack — the rotating cast of pertinent canon .md docs for a topic/scene. " +
        "Returns one budgeted block plus the resident docs (tier + why each loaded). Pass nodeCode (e.g. " +
        "'BCODA') to include that story's bible + its one register; pass text (scene/goal/conversation) to " +
        "trigger topic docs by keyword and semantic embedding. This is how you load only the few docs that " +
        "matter now instead of dumping hundreds.")]
    public async Task<string> DocContextPrepare(
        [Description("Scene/goal/conversation text to trigger topic docs against.")] string text,
        [Description("Optional node CODE (e.g. 'BCODA') to also load that story's bible + register.")] string? nodeCode = null,
        [Description("Token budget for the assembled block. Default 2000.")] int budget = 2000)
    {
        var result = await docContext.PrepareContextAsync(SessionKey(nodeCode), nodeCode, text, budget);
        return JsonSerializer.Serialize(new
        {
            estimatedTokens = result.EstimatedTokens,
            loaded = result.Loaded.Select(d => new { d.Tier, d.Reason, d.RelativePath, d.Chars }),
            block = result.Block,
        }, JsonOpts);
    }

    [McpServerTool, Description(
        "Inspect the current Doc Context Stack working set (the docs resident in the rotating cast) for a " +
        "node context, without changing it. Returns each doc's tier, why it loaded, and its score.")]
    public string DocContextStatus(
        [Description("Optional node CODE whose working set to inspect.")] string? nodeCode = null)
    {
        var active = docContext.GetActive(SessionKey(nodeCode));
        return JsonSerializer.Serialize(new
        {
            count = active.Count,
            docs = active.Select(e => new { e.Tier, e.Reason, e.RelativePath, e.Score }),
        }, JsonOpts);
    }

    [McpServerTool, Description(
        "Recall (call up) the select few tracked markdown files relevant to a keyword, straight from " +
        "the database — instead of materializing hundreds of tiny .md files on disk. Substring-matches " +
        "the keyword (case-insensitive) against relativePath, fileName, and category; set includeContent=true " +
        "to also search inside file bodies. Returns each match's full content so the caller can read only " +
        "what it needs. Examples: 'steppin', 'wound ledger', 'schism'.")]
    public async Task<string> RecallMarkdownFiles(
        [Description("Keyword to match against path/name/category (and body when includeContent=true).")] string keyword,
        [Description("Also search inside file bodies, not just names. Default false.")] bool includeContent = false)
    {
        var matches = await svc.SearchAsync(keyword, includeContent);
        var files = matches.Select(r => new
        {
            category     = r.Category,
            relativePath = r.RelativePath,
            fileRoot     = r.FileRoot,
            lastSynced   = r.LastSyncedAt.ToString("u"),
            content      = r.Content,
        }).ToList();
        return JsonSerializer.Serialize(new { keyword, count = files.Count, files }, JsonOpts);
    }

    [McpServerTool, Description(
        "Sync all discovered markdown files from disk into the database. " +
        "Only files whose content hash changed produce a new history row. " +
        "Pass dryRun=true to preview without writing.")]
    public async Task<string> SyncMarkdownFiles(
        [Description("If true, report what would be synced without writing to the database.")] bool dryRun = false)
    {
        var discovered = svc.DiscoverFiles().ToList();
        var result = await svc.SyncAllAsync(dryRun);
        return JsonSerializer.Serialize(new
        {
            dryRun,
            discovered = discovered.Count,
            inserted   = result.Inserted,
            updated    = result.Updated,
            unchanged  = result.Unchanged,
            errors     = result.Errors,
        }, JsonOpts);
    }

    [McpServerTool, Description(
        "Restore markdown files from the database back to disk. " +
        "Pass relativePath to restore a single file; omit to restore all tracked files. " +
        "Pass asOf (ISO 8601 UTC) to recover a historical version from the temporal table. " +
        "Pass dryRun=true to preview without writing to disk.")]
    public async Task<string> RestoreMarkdownFile(
        [Description("Relative path of the file to restore, e.g. 'CLAUDE.md'. Omit to restore all.")] string? relativePath = null,
        [Description("Optional ISO 8601 UTC datetime for point-in-time recovery.")] string? asOf = null,
        [Description("If true, report what would be written without touching the filesystem.")] bool dryRun = false)
    {
        DateTime? asOfDt = null;
        if (asOf != null)
        {
            if (!DateTime.TryParse(asOf, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
                return JsonSerializer.Serialize(new { error = "invalid_as_of", hint = "use ISO 8601, e.g. 2026-06-01T00:00:00Z" }, JsonOpts);
            asOfDt = parsed.ToUniversalTime();
        }

        var result = await svc.RestoreAsync(relativePath, asOfDt, dryRun);
        return JsonSerializer.Serialize(new
        {
            dryRun,
            relativePath,
            asOf    = asOfDt?.ToString("u"),
            written = result.Written,
            skipped = result.Skipped,
            errors  = result.Errors,
        }, JsonOpts);
    }
}
