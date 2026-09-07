using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Mcp;

// ── Publish readiness ───────────────────────────────────────────────────────
// The MCP mirror of `prose --publish-readiness` (docs/LOGIC.md §9). The `book_health`
// battery + SII score that used to live here was torn out 2026-09-06 (docs/rfc/0010):
// 32 checks, 3 of which had ever produced an applied finding, at up to $135 a run.

[McpServerToolType]
public class BookHealthTools(
    BookHealthService bookHealth,
    IDbContextFactory<ProseDbContext> dbFactory,
    HubInvoker hub)
{
    static readonly JsonSerializerOptions JsonOpts = CanonTools.JsonOpts;


    /// <summary>docs/LOGIC.md §9's five-point publish-readiness convergence gate as one answer
    /// (2026-08-30) — see BookHealthService.PublishReadinessAsync and its CLI mirror
    /// PublishReadinessCli.cs. Read-only, no LLM calls.</summary>
    [McpServerTool, Description(
        "docs/LOGIC.md §9's five-point publish-readiness convergence gate, computed as one answer: " +
        "(1) zero open BLOCKER/MODERATE logic-sweep findings, (2) zero open CONTRADICTED fact-ledger " +
        "claims, (3) two consecutive dry logic-sweep rounds against the book's current text, " +
        "(4) blast-radius recheck clean on every beat, (5) zero open High/BLOCKER Reader-Proxy QA " +
        "findings. Read-only — makes no LLM calls and runs no new checks, only reads what earlier " +
        "sweep/audit/ledger runs already filed or persisted.")]
    public Task<string> publish_readiness(
        [Description("Node id (GUID) or slug — a book or a lone chapter.")] string nodeIdOrSlug) =>
        hub.InvokeAsync(nameof(BookHealthTools), nameof(publish_readinessImpl), new { nodeIdOrSlug });

    public async Task<string> publish_readinessImpl(string nodeIdOrSlug)
    {
        var nodeId = await ResolveNodeAsync(nodeIdOrSlug);
        if (nodeId == null)
            return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);

        var report = await bookHealth.PublishReadinessAsync(nodeId.Value);
        return JsonSerializer.Serialize(new
        {
            node_id = report.NodeId, node_slug = report.Slug, ready = report.Ready,
            checks = report.Checks.Select(c => new { name = c.Name, pass = c.Pass, detail = c.Detail }),
        }, JsonOpts);
    }

    /// <summary>
    /// 2026-08-24 consolidation — see the note on <c>BookAuditTools.ResolveNodeAsync</c>. This
    /// copy had no <c>IgnoreQueryFilters()</c> on either branch, so the (since-deleted) <c>book_health</c> could not
    /// reach any book outside the ambient universe by slug. Delegates to
    /// <see cref="NodeRefResolver"/>.
    /// </summary>
    Task<Guid?> ResolveNodeAsync(string idOrSlug) =>
        NodeRefResolver.ResolveAsync(dbFactory, idOrSlug);
}
