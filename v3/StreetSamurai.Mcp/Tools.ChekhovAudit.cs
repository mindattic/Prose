using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Mcp;

[McpServerToolType]
public class ChekhovAuditTools(
    ChekhovAuditService chekhov,
    IDbContextFactory<StreetSamuraiDbContext> dbFactory)
{
    static readonly JsonSerializerOptions JsonOpts = CanonTools.JsonOpts;

    /// <summary>
    /// Chekhov's Gun audit: every concrete prop, sensory anchor, and recurring
    /// character-specific physical trait is extracted from the prose and tested
    /// for narrative function. ORPHANED = appears with no payoff; DECORATION =
    /// appears multiple times without new function; EARNS_IT = each appearance
    /// serves a distinct narrative purpose.
    /// </summary>
    [McpServerTool, Description("Chekhov's Gun audit for a story node: extract all concrete props, environmental anchors, sensory details, and recurring character-specific physical traits, then test whether each earns its place. Verdicts: EARNS_IT (each appearance serves a distinct purpose), ORPHANED (appears once with no payoff), DECORATION (repeated without new narrative function), ATMOSPHERE (one-time environmental texture with no implied promise), FLAG (uncertain — human review). Run before trimming any prose detail; before cutting, confirm the prop has no payoff in a later beat. Accepts node id (GUID) or slug.")]
    public async Task<string> chekhov_audit(
        [Description("Node id (GUID) or slug.")] string nodeIdOrSlug)
    {
        var nodeId = await ResolveNodeAsync(nodeIdOrSlug);
        if (nodeId == null)
            return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);

        try
        {
            var report = await chekhov.AuditAsync(nodeId.Value);
            return JsonSerializer.Serialize(new
            {
                node_slug       = report.NodeSlug,
                node_title      = report.NodeTitle,
                beat_count      = report.BeatCount,
                prop_count      = report.Findings.Count,
                orphaned_count  = report.OrphanedCount,
                flag_count      = report.FlagCount,
                decoration_count = report.DecorationCount,
                earns_it_count  = report.EarnsItCount,
                findings        = report.Findings.Select(f => new
                {
                    prop        = f.PropName,
                    type        = f.PropType,
                    verdict     = f.Verdict,
                    reasoning   = f.Reasoning,
                    fix         = f.Fix,
                    appearances = f.Appearances.Select(a => new
                    {
                        beat    = a.BeatLabel,
                        sortKey = a.SortKey,
                        context = a.Context,
                    }),
                }),
            }, JsonOpts);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message, nodeIdOrSlug }, JsonOpts);
        }
    }

    async Task<Guid?> ResolveNodeAsync(string idOrSlug)
    {
        if (Guid.TryParse(idOrSlug, out var g)) return g;
        await using var db = await dbFactory.CreateDbContextAsync();
        var s = await db.Nodes.AsNoTracking()
            .Where(x => x.Slug == idOrSlug || x.NodeCode == idOrSlug)
            .Select(x => x.Id)
            .FirstOrDefaultAsync();
        return s == Guid.Empty ? null : s;
    }
}
