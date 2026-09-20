using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --entity-mentions --entity &lt;id|slug&gt; [--limit &lt;n&gt;]
///
/// CLI wrapper around the existing <see cref="EntityMentionService.GetBeatsForEntityAsync"/> —
/// the same query the get_entity_beat_mentions MCP tool uses — added 2026-08-26 because no CLI
/// equivalent existed and a rename/edit workflow needs to list exactly which beats mention an
/// entity (node, beat number, excerpt) without going around Prose.Hub.
/// </summary>
public static class EntityMentionsCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var mentions = services.GetRequiredService<EntityMentionService>();

        var idOrSlug = Flag(args, "--entity");
        if (idOrSlug == null)
        {
            Console.Error.WriteLine("[entity-mentions] Requires --entity <id|slug>.");
            return 2;
        }

        var limitStr = Flag(args, "--limit");
        var limit = int.TryParse(limitStr, out var l) ? l : 200;

        var resolved = await mentions.ResolveEntityAsync(idOrSlug);
        if (resolved == null)
        {
            Console.Error.WriteLine($"[entity-mentions] No entity found for '{idOrSlug}'.");
            return 2;
        }

        var rows = await mentions.GetBeatsForEntityAsync(resolved.Value.Id, limit);
        Console.WriteLine($"[entity-mentions] {resolved.Value.Name} ({resolved.Value.Id}) — {rows.Count} mention(s):");
        foreach (var r in rows)
            Console.WriteLine($"  [{r.NodeTitle} ({r.NodeSlug})] beat {r.BeatNumber} — {r.Handle} — \"{r.Excerpt}\"");
        return 0;
    }

    static string? Flag(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }
}
