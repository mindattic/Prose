using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Mcp;

// ── "Behave like people" beat lenses ───────────────────────────────────────────
// Three single-LLM-call reads over a node's numbered beats. Each files advisory
// Findings and returns a per-lens score + issue list.
//   causality_check     — events follow by therefore/but, not "and then"
//   affect_check        — emotion plausibly drives action
//   interpersonal_check — verbal + non-verbal relational work (the 90+ lever)

/// <summary>
/// MCP tools for the three story lenses (CausalityService, AffectBehaviorService,
/// InterpersonalDynamicsService). Each accepts a node id (GUID) or slug.
/// </summary>
[McpServerToolType]
public class BeatLensTools
{
    private readonly CausalityService causality;
    private readonly AffectBehaviorService affect;
    private readonly InterpersonalDynamicsService interpersonal;
    private readonly IDbContextFactory<ProseDbContext> dbFactory;

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public BeatLensTools(
        CausalityService causality,
        AffectBehaviorService affect,
        InterpersonalDynamicsService interpersonal,
        IDbContextFactory<ProseDbContext> dbFactory)
    {
        this.causality = causality;
        this.affect = affect;
        this.interpersonal = interpersonal;
        this.dbFactory = dbFactory;
    }

    [McpServerTool, Description("Check a node's CAUSE-AND-EFFECT: do beats follow by therefore/but rather than 'and then'? Flags episodic transitions, effects without setup, actions against established motive, implausible reactions. Files advisory Findings; returns score 0-100 + issues. Arg: node GUID or slug.")]
    public Task<string> CausalityCheck([Description("Node id (GUID) or slug.")] string nodeIdOrSlug)
        => RunAsync(causality, nodeIdOrSlug);

    [McpServerTool, Description("Check whether each character's EMOTION believably DRIVES their ACTION. Flags actions that ignore what just happened, unmotivated calm, feelings named but not enacted. Files advisory Findings; returns score 0-100 + issues. Arg: node GUID or slug.")]
    public Task<string> AffectCheck([Description("Node id (GUID) or slug.")] string nodeIdOrSlug)
        => RunAsync(affect, nodeIdOrSlug);

    [McpServerTool, Description("Check INTERPERSONAL DYNAMICS — the 90+ relational lever. Are exchanges doing real relational work on BOTH channels (verbal subtext + non-verbal body/gesture)? Flags info-only dead exchanges, missing non-verbal channel, on-the-nose emotion-naming, bonds that don't change. Files advisory Findings; returns score 0-100 + issues. Arg: node GUID or slug.")]
    public Task<string> InterpersonalCheck([Description("Node id (GUID) or slug.")] string nodeIdOrSlug)
        => RunAsync(interpersonal, nodeIdOrSlug);

    private async Task<string> RunAsync(BeatLensService svc, string nodeIdOrSlug)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        Guid id;
        if (Guid.TryParse(nodeIdOrSlug, out var g)) id = g;
        else
        {
            var s = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == nodeIdOrSlug || x.NodeCode == nodeIdOrSlug);
            if (s == null) return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);
            id = s.Id;
        }

        var r = await svc.RunAsync(id);
        return JsonSerializer.Serialize(new
        {
            node_id = r.NodeId, slug = r.Slug, title = r.Title,
            lens = r.Lens, score = r.Score, recommendation = r.Recommendation,
            issues = r.Issues.Select(i => new
            {
                beat = i.Beat, kind = i.Kind, severity = i.Severity,
                evidence = i.Evidence, fix = i.Fix
            })
        }, JsonOpts);
    }
}
