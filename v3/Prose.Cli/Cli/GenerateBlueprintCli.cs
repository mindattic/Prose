using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// ss --generate-blueprint --slug &lt;nodeSlug&gt; [--retrofit] [--json]
///
/// Generates the StructuralBlueprint for a book node — the pre-prose anti-tell
/// commitments (subplot, temporal scheme, resolution mode, moral polarity,
/// escalation curve, event-type palette, form device, ending style, intertextual
/// anchors). StoryScope countermeasures: these are the narrative-structure
/// decisions that distinguish human fiction from AI fiction at 93.2% accuracy.
///
/// Ordering: bible → blueprint → prose. --retrofit infers a blueprint from
/// already-written prose for stories that predate the system.
///
/// Exit codes: 0 = generated, 2 = error.
/// </summary>
public static class GenerateBlueprintCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null;
        bool retrofit = args.Contains("--retrofit");
        bool jsonMode = args.Contains("--json");

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--slug") { slug = args[i + 1]; i++; }
        }

        if (slug == null)
        {
            Console.Error.WriteLine("Usage: ss --generate-blueprint --slug <nodeSlug> [--retrofit] [--json]");
            return 2;
        }

        var blueprintSvc = services.GetRequiredService<StructuralBlueprintService>();
        var dbFactory    = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var node = await db.Nodes.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Slug == slug || s.NodeCode == slug);
        if (node == null)
        {
            Console.Error.WriteLine($"Node '{slug}' not found.");
            return 2;
        }

        if (!jsonMode)
            Console.WriteLine($"Generating structural blueprint for '{node.Title}' ({(retrofit ? "retrofit from prose" : "pre-prose")})…\n");

        Prose.Core.Data.Entities.NodeStructuralBlueprint bp;
        try
        {
            bp = retrofit
                ? await blueprintSvc.RetrofitAsync(node.Id)
                : await blueprintSvc.GenerateAndSaveAsync(node.Id);
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"Blueprint generation failed: {ex.Message}");
            return 2;
        }

        if (jsonMode)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                node_slug        = node.Slug,
                node_title       = node.Title,
                generated_by     = bp.GeneratedBy,
                has_subplot      = bp.HasSubplot,
                subplot_summary  = bp.SubplotSummary,
                subplot_theme    = bp.SubplotTheme,
                temporal_scheme  = bp.TemporalScheme,
                anachrony_plan   = bp.AnachronyPlan,
                resolution_mode  = bp.ResolutionMode,
                resolution_note  = bp.ResolutionNote,
                moral_polarity   = bp.MoralPolarity,
                escalation_curve = bp.EscalationCurveJson,
                event_palette    = bp.EventTypePaletteJson,
                form_device      = bp.FormDevice,
                ending_style     = bp.EndingStyle,
                no_epilogue      = bp.NoEpilogue,
                anchors          = bp.IntertextualAnchorsJson,
                beat_tags        = bp.BeatTags.Count,
            }, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        Console.WriteLine($"Subplot:    {(bp.HasSubplot ? bp.SubplotSummary : "(none — book too short for a forced one)")}");
        if (bp.SubplotTheme != null)
            Console.WriteLine($"  parallel: {bp.SubplotTheme}");
        Console.WriteLine($"Temporal:   {bp.TemporalScheme}{(bp.AnachronyPlan != null ? $" — {bp.AnachronyPlan}" : "")}");
        Console.WriteLine($"Resolution: {bp.ResolutionMode}{(bp.ResolutionNote != null ? $" — {bp.ResolutionNote}" : "")}");
        Console.WriteLine($"Moral:      {bp.MoralPolarity}{(bp.MoralPolarity == "clear" ? "  ⚠️ deviation from ambivalent default" : "")}");
        Console.WriteLine($"Escalation: {bp.EscalationCurveJson}");
        Console.WriteLine($"Form:       {bp.FormDevice ?? "(conventional)"}");
        Console.WriteLine($"Ending:     {bp.EndingStyle}{(bp.EndingStyle == "quiet" ? "  ⚠️ deviation from avalanche default" : "")}, epilogue: {(bp.NoEpilogue ? "no" : "yes")}");
        Console.WriteLine($"Anchors:    {bp.IntertextualAnchorsJson}");
        Console.WriteLine($"Beat tags:  {bp.BeatTags.Count}");
        Console.WriteLine();
        Console.WriteLine($"✅ Blueprint saved ({bp.GeneratedBy}). Prose generation will now receive per-beat structural guidance.");
        Console.WriteLine("   Verify after writing with: ss --storyscope-audit --slug " + node.Slug);
        return 0;
    }
}
