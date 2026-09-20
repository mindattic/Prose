using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Mcp;

// ── Data-integrity tools ─────────────────────────────────────────────────
// audit_data_consistency — SSOT-drift sweep across the SQL schema (DataConsistencyService)
// check_graph_health     — orphaned/weakly-connected/malformed-name world-graph nodes
// sanity_scan            — deterministic per-node prose scan (code leaks, undefined
//                          acronyms, length floor, mojibake)
//
// Added 2026-08-09: all three services were exposed to the CLI earlier this session
// (--audit-consistency, --graph-health, --sanity-scan) after being found completely
// unreachable from any interface — but that CLI work never extended to MCP, leaving an
// agent working through MCP tools alone with no way to run any of these checks. This
// closes that gap so both interfaces have equal coverage.

/// <summary>
/// Deterministic, no-LLM data-integrity checks: SSOT-drift across the SQL schema, world-graph
/// node/edge health, and per-node prose sanity (internal code leaks, undefined acronyms, length
/// floor, mojibake). All three are fast enough to run as a pre-publish gate.
/// </summary>
[McpServerToolType]
public class DataIntegrityTools(
    DataConsistencyService consistency,
    UniverseGraphService graph,
    GraphHealthService graphHealth,
    SanityScanService sanityScan,
    DuplicateEntityScanService duplicateEntityScan,
    CanonDocumentService canonDocs,
    IDbContextFactory<ProseDbContext> dbFactory,
    HubInvoker hub)
{
    static readonly JsonSerializerOptions JsonOpts = CanonTools.JsonOpts;

    /// <summary>Run the SSOT-drift audit across the SQL schema.</summary>
    [McpServerTool, Description(
        "Audit SSOT drift across the SQL schema — denormalized display fields (Alias caches on " +
        "bridge tables) disagreeing with the FK they cache, orphaned subtype rows, dangling edges, " +
        "slug collisions, and EntityStateEvents bi-temporal hygiene. Global, cross-universe check " +
        "(not scoped to one universe). No LLM calls; findings are reported, never auto-corrected.")]
    public Task<string> AuditDataConsistency() =>
        hub.InvokeAsync(nameof(DataIntegrityTools), nameof(AuditDataConsistencyImpl));

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> AuditDataConsistencyImpl()
    {
        var report = await consistency.RunAsync();
        return JsonSerializer.Serialize(new
        {
            ranAtUtc   = report.RanAtUtc,
            totalDrift = report.TotalDrift,
            errorCount = report.ErrorCount,
            warnCount  = report.WarnCount,
            infoCount  = report.InfoCount,
            findings   = report.Findings.Select(f => new
            {
                code        = f.Code,
                title       = f.Title,
                description = f.Description,
                driftCount  = f.DriftCount,
                severity    = f.Severity,
                fixHint     = f.FixHint,
                samples     = f.Samples.Select(s => new { label = s.Label, detail = s.Detail }),
            }),
        }, JsonOpts);
    }

    /// <summary>Check world-graph node/edge health for the active universe.</summary>
    [McpServerTool, Description(
        "Check the active universe's world-graph health: orphaned nodes (zero edges), weakly-" +
        "connected nodes (exactly one edge), and suspicious/malformed node names (sentence " +
        "fragments, junk parses from free-text fields promoted verbatim into node identities). " +
        "Rebuilds the graph from live SQL before analyzing, so results always reflect current " +
        "data. Zero LLM calls; pure graph traversal + string heuristics.")]
    public Task<string> CheckGraphHealth() =>
        hub.InvokeAsync(nameof(DataIntegrityTools), nameof(CheckGraphHealthImpl));

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public string CheckGraphHealthImpl()
    {
        graph.Rebuild();
        var report = graphHealth.Analyze();
        return JsonSerializer.Serialize(new
        {
            totalNodes          = report.TotalNodes,
            totalOrphans        = report.TotalOrphans,
            totalWeaklyConnected = report.TotalWeaklyConnected,
            totalSuspicious     = report.TotalSuspicious,
            suspiciousNodes     = report.SuspiciousNodes.Select(o => new
            {
                id = o.Id, name = o.Name, nodeType = o.NodeType, reason = o.Reason,
            }),
            orphanedNodes       = report.OrphanedNodes.Where(o => o.IsSuspicious).Select(o => new
            {
                id = o.Id, name = o.Name, nodeType = o.NodeType, reason = o.Reason,
            }),
        }, JsonOpts);
    }

    /// <summary>Run the deterministic sanity scan against one book node's prose.</summary>
    [McpServerTool, Description(
        "Run the deterministic (no-LLM) sanity scan against one book's prose: internal dev-code " +
        "leaks (an internal node code like 'BCODA' appearing as if it were an in-world name), " +
        "undefined all-caps acronyms (excludes the book's own code, purely-numeric codes, " +
        "glossaried terms, and acronyms inside an embedded found-document/log block written in " +
        "sustained capitals), a 50-page length floor, and mojibake (encoding corruption). Fast " +
        "enough for a pre-publish gate. Accepts a book node's slug or GUID.")]
    public Task<string> SanityScanNode(
        [Description("Book node slug or GUID to scan.")] string nodeIdOrSlug) =>
        hub.InvokeAsync(nameof(DataIntegrityTools), nameof(SanityScanNodeImpl), new { nodeIdOrSlug });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> SanityScanNodeImpl(
        string nodeIdOrSlug)
    {
        Guid nodeId;
        if (!Guid.TryParse(nodeIdOrSlug, out nodeId))
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var found = await db.Nodes.AsNoTracking()
                .FirstOrDefaultAsync(n => n.Slug == nodeIdOrSlug || n.NodeCode == nodeIdOrSlug);
            if (found == null)
                return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);
            nodeId = found.Id;
        }

        var report = await sanityScan.ScanAsync(nodeId);
        return JsonSerializer.Serialize(new
        {
            nodeTitle  = report.NodeTitle,
            nodeSlug   = report.NodeSlug,
            nodeCode   = report.NodeCode,
            beatCount  = report.BeatCount,
            wordCount  = report.WordCount,
            pdfPages   = report.EstimatedPdfPages,
            blocks     = report.Findings.Count(f => f.Severity == "block"),
            warns      = report.Findings.Count(f => f.Severity == "warn"),
            findings   = report.Findings.Select(f => new
            {
                severity   = f.Severity,
                kind       = f.Kind,
                beatNumber = f.BeatNumber,
                message    = f.Message,
                snippet    = f.Snippet,
            }),
        }, JsonOpts);
    }

    /// <summary>Scan a universe's Entities of one type for duplicate/near-duplicate names.</summary>
    [McpServerTool, Description(
        "Scan a universe's Entities of one EntityType (default 'character'; also useful for " +
        "'faction', 'place', etc.) for duplicate or near-duplicate names (exact match, or exactly " +
        "1 edit apart, e.g. \"Boris Johansen\" vs \"Boris Johanssen\") that are NOT explained by " +
        "legitimate cross-book disambiguation (Entity.OriginNodeId set to different values, " +
        "meaning deliberately distinct characters in different books' continuity). Finds " +
        "candidates only — it does not merge or delete anything; resolving a duplicate requires " +
        "reading the actual prose to determine which row (if either) matches what was actually " +
        "written, exactly as the investigation that motivated this tool did (TEST's 'Bear', " +
        "2026-08-10 — two draft entity rows, neither fully correct on its own). No LLM calls.")]
    public Task<string> DuplicateEntityScan(
        [Description("Universe slug, e.g. 'glmz', 'scry', 'nonfiction'.")] string universeSlug,
        [Description("Entity type to scan. Defaults to 'character'.")] string entityType = "character") =>
        hub.InvokeAsync(nameof(DataIntegrityTools), nameof(DuplicateEntityScanImpl), new { universeSlug, entityType });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> DuplicateEntityScanImpl(
        string universeSlug,
        string entityType = "character")
    {
        var universeId = await canonDocs.ResolveUniverseIdAsync(universeSlug);
        if (universeId == null)
            return JsonSerializer.Serialize(new { error = "unknown_universe", universeSlug }, JsonOpts);

        var groups = await duplicateEntityScan.ScanAsync(universeId.Value, entityType);
        return JsonSerializer.Serialize(new
        {
            universe = universeSlug,
            entityType,
            groupCount = groups.Count,
            groups = groups.Select(g => new
            {
                matchedOn = g.MatchedOn,
                candidates = g.Candidates.Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    originNodeId = c.OriginNodeId,
                    descriptionSnippet = c.DescriptionSnippet,
                    mentionCount = c.MentionCount,
                }),
            }),
        }, JsonOpts);
    }
}
