using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --export-entity-cluster --root &lt;entityGuid&gt; --universe &lt;slug&gt; --out &lt;path.md&gt;
///
/// Report-only half of the archive-then-delete workflow for an orphaned worldbuilding cluster —
/// entities/edges that exist in the DB but were never wired into any live book (found live
/// 2026-09-02 via prose --scan-edge-duplicates surfacing a ~60-entity pre-alpha "Eld/Yggdra"
/// afterlife-cosmology draft stranded under UniverseId=glmz). Walks the full connected component
/// from --root via <see cref="EntityClusterWalker"/>, writes every entity's full canon record
/// (Name/Type/Slug/Status/Description/Record.Json) and every internal edge to one Markdown file,
/// and prints the same entity list to console for human review — the whole point of a SEPARATE
/// export step before <see cref="DeleteEntityClusterCli"/> runs is that a human reads this output
/// and confirms nothing load-bearing is in it before anything is deleted.
/// </summary>
public static class ExportEntityClusterCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var rootArg = Flag(args, "--root");
        var universeSlug = Flag(args, "--universe");
        var outPath = Flag(args, "--out");
        var excludeArg = Flag(args, "--exclude");

        if (!Guid.TryParse(rootArg, out var rootId) || string.IsNullOrWhiteSpace(universeSlug) || string.IsNullOrWhiteSpace(outPath))
        {
            Console.Error.WriteLine("Usage: prose --export-entity-cluster --root <entityGuid> --universe <slug> --out <path.md> [--exclude <guid,guid,...>]");
            return 2;
        }

        var walls = (excludeArg ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => Guid.TryParse(s, out var g) ? g : (Guid?)null)
            .Where(g => g != null).Select(g => g!.Value).ToHashSet();

        var canonDocs = services.GetRequiredService<CanonDocumentService>();
        var universeId = await canonDocs.ResolveUniverseIdAsync(universeSlug);
        if (universeId == null)
        {
            Console.Error.WriteLine($"[export-entity-cluster] Unknown universe '{universeSlug}'.");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var rootExists = await db.Entities.IgnoreQueryFilters().AsNoTracking()
            .AnyAsync(e => e.Id == rootId && e.UniverseId == universeId.Value);
        if (!rootExists)
        {
            Console.Error.WriteLine($"[export-entity-cluster] No entity {rootId} in universe '{universeSlug}'.");
            return 1;
        }

        var cluster = await EntityClusterWalker.WalkAsync(db, rootId, universeId.Value, walls);

        var sb = new StringBuilder();
        sb.AppendLine($"# Entity cluster archive — root {rootId} ({universeSlug})");
        sb.AppendLine();
        sb.AppendLine($"Exported {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC — {cluster.Entities.Count} entities, {cluster.Edges.Count} edges.");
        sb.AppendLine();
        sb.AppendLine("## Entities");
        sb.AppendLine();
        foreach (var e in cluster.Entities)
        {
            sb.AppendLine($"### {e.Name} [{e.EntityType}]");
            sb.AppendLine();
            sb.AppendLine($"- Id: `{e.Id}`");
            sb.AppendLine($"- Slug: `{e.Slug}`");
            sb.AppendLine($"- Status: {e.Status}");
            if (!string.IsNullOrWhiteSpace(e.Description))
                sb.AppendLine($"- Description: {e.Description}");
            if (e.Record != null && !string.IsNullOrWhiteSpace(e.Record.Json))
            {
                sb.AppendLine();
                sb.AppendLine("```json");
                try
                {
                    var pretty = JsonSerializer.Serialize(
                        JsonSerializer.Deserialize<JsonElement>(e.Record.Json),
                        new JsonSerializerOptions { WriteIndented = true });
                    sb.AppendLine(pretty);
                }
                catch { sb.AppendLine(e.Record.Json); }
                sb.AppendLine("```");
            }
            sb.AppendLine();
        }

        sb.AppendLine("## Edges");
        sb.AppendLine();
        sb.AppendLine("| Source | Relation | Target | Sentiment | Description |");
        sb.AppendLine("|---|---|---|---|---|");
        var byId = cluster.Entities.ToDictionary(e => e.Id, e => e.Name);
        foreach (var edge in cluster.Edges)
        {
            var sourceName = byId.TryGetValue(edge.SourceId, out var sn) ? sn : edge.SourceId.ToString();
            var targetName = byId.TryGetValue(edge.TargetId, out var tn) ? tn : edge.TargetId.ToString();
            var desc = (edge.Description ?? "").Replace("|", "\\|").Replace("\n", " ");
            sb.AppendLine($"| {sourceName} | {edge.RelationType} | {targetName} | {edge.Sentiment} | {desc} |");
        }

        var dir = Path.GetDirectoryName(Path.GetFullPath(outPath));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(outPath, sb.ToString());

        Console.WriteLine($"[export-entity-cluster] Wrote {cluster.Entities.Count} entities, {cluster.Edges.Count} edges to {outPath}");
        Console.WriteLine();
        Console.WriteLine("Entities in cluster:");
        foreach (var e in cluster.Entities)
            Console.WriteLine($"  [{e.EntityType,-14}] {e.Name}  ({e.Id})");
        Console.WriteLine();
        var excludeSuffix = walls.Count > 0 ? $" --exclude {string.Join(',', walls)}" : "";
        Console.WriteLine($"Review the list above. If it's clean, run:");
        Console.WriteLine($"  prose --delete-entity-cluster --root {rootId} --universe {universeSlug} --confirm {cluster.Entities.Count}{excludeSuffix}");

        return 0;
    }

    static string? Flag(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }
}
