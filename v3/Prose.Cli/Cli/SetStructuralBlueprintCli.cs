using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --set-structural-blueprint --slug &lt;slug&gt; --file &lt;path.json&gt;
///
/// Hand-author a structural blueprint with no LLM call — for when the generation provider is
/// unavailable but the structural decisions (resolution mode, moral polarity, escalation curve,
/// event-type palette, subplot, ending style, intertextual anchors) have already been authored
/// by a human/agent, typically already written out in the node's own brief/bible. The JSON must
/// match the STRICT contract documented in StructuralBlueprintService.BuildSystemPrompt's
/// response section. Added 2026-08-10 after the standing Anthropic credit outage blocked BTL's
/// blueprint generation despite every structural decision already being made and documented.
/// </summary>
public static class SetStructuralBlueprintCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null, filePath = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--slug": if (i + 1 < args.Length) slug = args[++i]; break;
                case "--file": if (i + 1 < args.Length) filePath = args[++i]; break;
            }
        }

        if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(filePath))
        {
            Console.Error.WriteLine("[set-structural-blueprint] --slug and --file are both required.");
            Console.Error.WriteLine("Usage: prose --set-structural-blueprint --slug <slug> --file <path.json>");
            return 2;
        }

        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine($"[set-structural-blueprint] File not found: {filePath}");
            return 1;
        }
        var json = await File.ReadAllTextAsync(filePath);

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var blueprintService = services.GetRequiredService<StructuralBlueprintService>();

        await using var db = await dbFactory.CreateDbContextAsync();
        var node = await db.Nodes.FirstOrDefaultAsync(n => n.Slug == slug || n.NodeCode == slug);
        if (node == null)
        {
            Console.Error.WriteLine($"[set-structural-blueprint] No node found with slug or code '{slug}'.");
            return 1;
        }

        try
        {
            var blueprint = await blueprintService.SetManualAsync(node.Id, json);
            Console.WriteLine($"[set-structural-blueprint] Saved for '{node.Title}' ({node.Slug}).");
            Console.WriteLine($"[set-structural-blueprint] subplot={blueprint.HasSubplot} temporal={blueprint.TemporalScheme} resolution={blueprint.ResolutionMode} moral={blueprint.MoralPolarity} ending={blueprint.EndingStyle} granularity={blueprint.Granularity}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[set-structural-blueprint] Failed: {ex.Message}");
            return 1;
        }
    }
}
