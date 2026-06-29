using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Mcp;

// ── "Behave like people" beat lenses ───────────────────────────────────────────
// Three single-LLM-call reads over a strand's numbered beats. Each files advisory
// Findings and returns a per-lens score + issue list.
//   causality_check     — events follow by therefore/but, not "and then"
//   affect_check        — emotion plausibly drives action
//   interpersonal_check — verbal + non-verbal relational work (the 90+ lever)

/// <summary>
/// MCP tools for the three story lenses (CausalityService, AffectBehaviorService,
/// InterpersonalDynamicsService). Each accepts a strand id (GUID) or slug.
/// </summary>
[McpServerToolType]
public class BeatLensTools
{
    private readonly CausalityService causality;
    private readonly AffectBehaviorService affect;
    private readonly InterpersonalDynamicsService interpersonal;
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public BeatLensTools(
        CausalityService causality,
        AffectBehaviorService affect,
        InterpersonalDynamicsService interpersonal,
        IDbContextFactory<StreetSamuraiDbContext> dbFactory)
    {
        this.causality = causality;
        this.affect = affect;
        this.interpersonal = interpersonal;
        this.dbFactory = dbFactory;
    }

    [McpServerTool, Description("Check a strand's CAUSE-AND-EFFECT: do beats follow by therefore/but rather than 'and then'? Flags episodic transitions, effects without setup, actions against established motive, implausible reactions. Files advisory Findings; returns score 0-100 + issues. Arg: strand GUID or slug.")]
    public Task<string> CausalityCheck([Description("Strand id (GUID) or slug.")] string strandIdOrSlug)
        => RunAsync(causality, strandIdOrSlug);

    [McpServerTool, Description("Check whether each character's EMOTION believably DRIVES their ACTION. Flags actions that ignore what just happened, unmotivated calm, feelings named but not enacted. Files advisory Findings; returns score 0-100 + issues. Arg: strand GUID or slug.")]
    public Task<string> AffectCheck([Description("Strand id (GUID) or slug.")] string strandIdOrSlug)
        => RunAsync(affect, strandIdOrSlug);

    [McpServerTool, Description("Check INTERPERSONAL DYNAMICS — the 90+ relational lever. Are exchanges doing real relational work on BOTH channels (verbal subtext + non-verbal body/gesture)? Flags info-only dead exchanges, missing non-verbal channel, on-the-nose emotion-naming, bonds that don't change. Files advisory Findings; returns score 0-100 + issues. Arg: strand GUID or slug.")]
    public Task<string> InterpersonalCheck([Description("Strand id (GUID) or slug.")] string strandIdOrSlug)
        => RunAsync(interpersonal, strandIdOrSlug);

    private async Task<string> RunAsync(BeatLensService svc, string strandIdOrSlug)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        Guid id;
        if (Guid.TryParse(strandIdOrSlug, out var g)) id = g;
        else
        {
            var s = await db.Strands.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == strandIdOrSlug || x.StrandCode == strandIdOrSlug);
            if (s == null) return JsonSerializer.Serialize(new { error = "strand_not_found", strandIdOrSlug }, JsonOpts);
            id = s.Id;
        }

        var r = await svc.RunAsync(id);
        return JsonSerializer.Serialize(new
        {
            strand_id = r.StrandId, slug = r.Slug, title = r.Title,
            lens = r.Lens, score = r.Score, recommendation = r.Recommendation,
            issues = r.Issues.Select(i => new
            {
                beat = i.Beat, kind = i.Kind, severity = i.Severity,
                evidence = i.Evidence, fix = i.Fix
            })
        }, JsonOpts);
    }
}
