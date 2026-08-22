using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Mcp;

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
    private readonly UserContextService userContext;
    private readonly LibertyReportService libertyReport;
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly TokenLedger ledger;
    private readonly HubInvoker hub;

    public ConfigTools(
        MarkdownFileService svc,
        DocContextService docContext,
        UserContextService userContext,
        LibertyReportService libertyReport,
        IDbContextFactory<ProseDbContext> dbFactory,
        TokenLedger ledger,
        HubInvoker hub)
    {
        this.svc          = svc;
        this.docContext   = docContext;
        this.userContext  = userContext;
        this.libertyReport = libertyReport;
        this.dbFactory    = dbFactory;
        this.ledger       = ledger;
        this.hub          = hub;
    }

    [McpServerTool, Description(
        "Show the running token cost tally for the current MCP server session. " +
        "Returns call count, input/output token estimates, and USD cost broken down by model. " +
        "Token counts are estimated from text length (chars / 4) since the Legion transport " +
        "does not expose Anthropic usage objects. Pass reset=true to clear the ledger.")]
    public Task<string> GetCostReport(
        [Description("If true, clear the ledger after reporting. Default false.")] bool reset = false) =>
        hub.InvokeAsync(nameof(ConfigTools), nameof(GetCostReportImpl), new { reset });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public string GetCostReportImpl(bool reset = false)
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
    public Task<string> ListMarkdownFiles() =>
        hub.InvokeAsync(nameof(ConfigTools), nameof(ListMarkdownFilesImpl), new { });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> ListMarkdownFilesImpl()
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
    public Task<string> GetMarkdownFile(
        [Description("Relative path key, e.g. 'CLAUDE.md' or 'docs/AMENDMENTS.md'.")] string relativePath,
        [Description("Optional ISO 8601 UTC datetime to retrieve the version current at that moment.")] string? asOf = null) =>
        hub.InvokeAsync(nameof(ConfigTools), nameof(GetMarkdownFileImpl), new { relativePath, asOf });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> GetMarkdownFileImpl(string relativePath, string? asOf = null)
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
        "'BCODA') to include that book's bible + its one register; pass text (scene/goal/conversation) to " +
        "trigger topic docs by keyword and semantic embedding. This is how you load only the few docs that " +
        "matter now instead of dumping hundreds.")]
    public Task<string> DocContextPrepare(
        [Description("Scene/goal/conversation text to trigger topic docs against.")] string text,
        [Description("Optional node CODE (e.g. 'BCODA') to also load that book's bible + register.")] string? nodeCode = null,
        [Description("Token budget for the assembled block. Default 2000.")] int budget = 2000) =>
        hub.InvokeAsync(nameof(ConfigTools), nameof(DocContextPrepareImpl), new { text, nodeCode, budget });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> DocContextPrepareImpl(string text, string? nodeCode = null, int budget = 2000)
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
    public Task<string> DocContextStatus(
        [Description("Optional node CODE whose working set to inspect.")] string? nodeCode = null) =>
        hub.InvokeAsync(nameof(ConfigTools), nameof(DocContextStatusImpl), new { nodeCode });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public string DocContextStatusImpl(string? nodeCode = null)
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
    public Task<string> RecallMarkdownFiles(
        [Description("Keyword to match against path/name/category (and body when includeContent=true).")] string keyword,
        [Description("Also search inside file bodies, not just names. Default false.")] bool includeContent = false) =>
        hub.InvokeAsync(nameof(ConfigTools), nameof(RecallMarkdownFilesImpl), new { keyword, includeContent });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> RecallMarkdownFilesImpl(string keyword, bool includeContent = false)
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
    public Task<string> SyncMarkdownFiles(
        [Description("If true, report what would be synced without writing to the database.")] bool dryRun = false) =>
        hub.InvokeAsync(nameof(ConfigTools), nameof(SyncMarkdownFilesImpl), new { dryRun });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> SyncMarkdownFilesImpl(bool dryRun = false)
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
    public Task<string> RestoreMarkdownFile(
        [Description("Relative path of the file to restore, e.g. 'CLAUDE.md'. Omit to restore all.")] string? relativePath = null,
        [Description("Optional ISO 8601 UTC datetime for point-in-time recovery.")] string? asOf = null,
        [Description("If true, report what would be written without touching the filesystem.")] bool dryRun = false) =>
        hub.InvokeAsync(nameof(ConfigTools), nameof(RestoreMarkdownFileImpl), new { relativePath, asOf, dryRun });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> RestoreMarkdownFileImpl(string? relativePath = null, string? asOf = null, bool dryRun = false)
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

    // ── User context overrides ────────────────────────────────────────────────
    // Pin or exclude specific canon docs from the DocContextStack for the
    // current session. Overrides expire after 24 h or on clear_context.
    // ─────────────────────────────────────────────────────────────────────────

    [McpServerTool, Description(
        "Pin a canon .md doc so it is always included in every beat prompt, regardless of LRU tier. " +
        "Identify the doc by relative path fragment (e.g. 'ICFI', 'BIBLE', 'wound') or its GUID. " +
        "The override lasts 24 h or until remove_context_doc / clear_context is called. " +
        "Optionally scope to a single book with nodeSlug so only that book's beats include it.")]
    public Task<string> AddContextDoc(
        [Description("Relative path fragment or GUID of the markdown doc to pin.")] string doc,
        [Description("Optional book slug to scope the pin (e.g. 'icfi'). Omit for session-global.")] string? nodeSlug = null) =>
        hub.InvokeAsync(nameof(ConfigTools), nameof(AddContextDocImpl), new { doc, nodeSlug });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> AddContextDocImpl(string doc, string? nodeSlug = null)
    {
        var (docId, docPath, err) = await ResolveDocIdAsync(doc);
        if (err != null) return JsonSerializer.Serialize(new { error = err }, JsonOpts);

        var nodeId = nodeSlug != null ? await ResolveNodeIdAsync(nodeSlug) : null;
        if (nodeSlug != null && nodeId == null)
            return JsonSerializer.Serialize(new { error = $"node_not_found: '{nodeSlug}'" }, JsonOpts);

        await userContext.PinAsync(docId, nodeId);
        return JsonSerializer.Serialize(new
        {
            action   = "pinned",
            doc      = docPath,
            nodeSlug = nodeSlug ?? "(global)",
            expiresIn = "24h",
        }, JsonOpts);
    }

    [McpServerTool, Description(
        "Exclude a canon .md doc from the DocContextStack so it is never injected even if it would " +
        "normally match. Identify the doc by relative path fragment or GUID. " +
        "The override lasts 24 h or until remove_context_doc / clear_context is called.")]
    public Task<string> ExcludeContextDoc(
        [Description("Relative path fragment or GUID of the markdown doc to exclude.")] string doc,
        [Description("Optional book slug to scope the exclusion. Omit for session-global.")] string? nodeSlug = null) =>
        hub.InvokeAsync(nameof(ConfigTools), nameof(ExcludeContextDocImpl), new { doc, nodeSlug });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> ExcludeContextDocImpl(string doc, string? nodeSlug = null)
    {
        var (docId, docPath, err) = await ResolveDocIdAsync(doc);
        if (err != null) return JsonSerializer.Serialize(new { error = err }, JsonOpts);

        var nodeId = nodeSlug != null ? await ResolveNodeIdAsync(nodeSlug) : null;
        if (nodeSlug != null && nodeId == null)
            return JsonSerializer.Serialize(new { error = $"node_not_found: '{nodeSlug}'" }, JsonOpts);

        await userContext.ExcludeAsync(docId, nodeId);
        return JsonSerializer.Serialize(new
        {
            action   = "excluded",
            doc      = docPath,
            nodeSlug = nodeSlug ?? "(global)",
            expiresIn = "24h",
        }, JsonOpts);
    }

    [McpServerTool, Description(
        "Remove a specific pin or exclude override for a canon doc. " +
        "Pass the same doc path/GUID and optional nodeSlug used when the override was created.")]
    public Task<string> RemoveContextDoc(
        [Description("Relative path fragment or GUID of the markdown doc whose override to remove.")] string doc,
        [Description("Optional book slug the override was scoped to.")] string? nodeSlug = null) =>
        hub.InvokeAsync(nameof(ConfigTools), nameof(RemoveContextDocImpl), new { doc, nodeSlug });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> RemoveContextDocImpl(string doc, string? nodeSlug = null)
    {
        var (docId, docPath, err) = await ResolveDocIdAsync(doc);
        if (err != null) return JsonSerializer.Serialize(new { error = err }, JsonOpts);

        var nodeId = nodeSlug != null ? await ResolveNodeIdAsync(nodeSlug) : null;
        await userContext.RemoveAsync(docId, nodeId);
        return JsonSerializer.Serialize(new { action = "removed", doc = docPath, nodeSlug = nodeSlug ?? "(global)" }, JsonOpts);
    }

    [McpServerTool, Description(
        "Clear ALL active context overrides for this session (both pins and excludes). " +
        "Pass nodeSlug to clear only overrides scoped to that book; omit for session-wide clear.")]
    public Task<string> ClearContext(
        [Description("Optional book slug to clear only overrides for that node. Omit for full session clear.")] string? nodeSlug = null) =>
        hub.InvokeAsync(nameof(ConfigTools), nameof(ClearContextImpl), new { nodeSlug });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> ClearContextImpl(string? nodeSlug = null)
    {
        var nodeId = nodeSlug != null ? await ResolveNodeIdAsync(nodeSlug) : null;
        if (nodeSlug != null && nodeId == null)
            return JsonSerializer.Serialize(new { error = $"node_not_found: '{nodeSlug}'" }, JsonOpts);

        await userContext.ClearAsync(nodeId);
        return JsonSerializer.Serialize(new { action = "cleared", scope = nodeSlug ?? "(global)" }, JsonOpts);
    }

    [McpServerTool, Description(
        "Show all active context overrides (pins and excludes) for this session. " +
        "Includes the doc path, action, scope (global or node), and expiry time.")]
    public Task<string> GetContextStatus() =>
        hub.InvokeAsync(nameof(ConfigTools), nameof(GetContextStatusImpl), new { });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> GetContextStatusImpl()
    {
        var report = await userContext.GetStatusAsync();
        return JsonSerializer.Serialize(new
        {
            session  = report.SessionKey,
            count    = report.Entries.Count,
            entries  = report.Entries.Select(e => new
            {
                action      = e.Action,
                doc         = e.RelativePath,
                nodeId      = e.NodeId?.ToString() ?? "(global)",
                expiresAt   = e.ExpiresAt.ToString("u"),
            }),
        }, JsonOpts);
    }

    // ── Liberty Report (Rule of Cool) ─────────────────────────────────────────

    [McpServerTool, Description(
        "Show the liberty analysis (Rule of Cool) for a single beat or all beats in a book. " +
        "A 'liberty' is any creative departure from the beat goal or entity roster: " +
        "entity_invention (name not in DB), tech_departure (GLMZ physics violated), " +
        "or creative_departure (plot beyond the beat goal). " +
        "Each liberty is scored CoolFactor 0–10: ≥8 → CANON-ADDITION-CANDIDATE finding, " +
        "5–7 → LIBERTY-CONSIDER advisory, ≤4 entity invention → LIBERTY-WARNING. " +
        "Reports are written automatically after each beat write; this tool reads them.")]
    public Task<string> GetLibertyReport(
        [Description("Beat GUID to retrieve the report for that specific beat.")] string? beatId = null,
        [Description("Book slug (e.g. 'icfi') to retrieve all reports for that book, newest first.")] string? slug = null) =>
        hub.InvokeAsync(nameof(ConfigTools), nameof(GetLibertyReportImpl), new { beatId, slug });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> GetLibertyReportImpl(string? beatId = null, string? slug = null)
    {
        if (beatId != null)
        {
            if (!Guid.TryParse(beatId, out var gid))
                return JsonSerializer.Serialize(new { error = $"invalid_beat_id: '{beatId}'" }, JsonOpts);

            var liberties = await libertyReport.GetAsync(gid);
            return JsonSerializer.Serialize(new
            {
                beatId,
                count = liberties.Count,
                liberties = liberties.Select(l => new
                {
                    l.Kind, l.Name, l.Evidence, l.Explanation, l.CoolFactor,
                    tag = l.CoolFactor >= 8 ? "CANON-ADDITION-CANDIDATE" :
                          l.CoolFactor >= 5 ? "LIBERTY-CONSIDER" :
                          l.Kind == "entity_invention" ? "LIBERTY-WARNING" : "LIBERTY-LOW",
                }),
            }, JsonOpts);
        }

        if (slug != null)
        {
            var reports = await libertyReport.GetForNodeAsync(slug);
            var flat = reports.SelectMany(r => r.Liberties.Select(l => new
            {
                r.BeatId, r.GeneratedAt, l.Kind, l.Name, l.Evidence, l.Explanation, l.CoolFactor,
                tag = l.CoolFactor >= 8 ? "CANON-ADDITION-CANDIDATE" :
                      l.CoolFactor >= 5 ? "LIBERTY-CONSIDER" :
                      l.Kind == "entity_invention" ? "LIBERTY-WARNING" : "LIBERTY-LOW",
            })).ToList();
            return JsonSerializer.Serialize(new
            {
                slug,
                totalBeats    = reports.Count,
                totalLiberties = flat.Count,
                candidates    = flat.Count(l => l.CoolFactor >= 8),
                advisories    = flat.Count(l => l.CoolFactor is >= 5 and < 8),
                warnings      = flat.Count(l => l.Kind == "entity_invention" && l.CoolFactor < 5),
                liberties     = flat,
            }, JsonOpts);
        }

        return JsonSerializer.Serialize(new { error = "provide beatId or slug" }, JsonOpts);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<(Guid Id, string Path, string? Error)> ResolveDocIdAsync(string doc)
    {
        if (Guid.TryParse(doc, out var g)) return (g, doc, null);

        await using var db = await dbFactory.CreateDbContextAsync();
        var hits = await db.MarkdownFiles.AsNoTracking()
            .Where(m => m.RelativePath.Contains(doc))
            .Select(m => new { m.Id, m.RelativePath })
            .Take(5)
            .ToListAsync();

        if (hits.Count == 0)
            return (Guid.Empty, "", $"doc_not_found: no markdown file matches '{doc}'");
        if (hits.Count > 1)
            return (Guid.Empty, "", $"doc_ambiguous: {hits.Count} files match '{doc}': {string.Join(", ", hits.Select(h => h.RelativePath))}");

        return (hits[0].Id, hits[0].RelativePath, null);
    }

    private async Task<Guid?> ResolveNodeIdAsync(string slug)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Nodes.AsNoTracking()
            .Where(n => n.Slug == slug)
            .Select(n => (Guid?)n.Id)
            .FirstOrDefaultAsync();
    }
}
