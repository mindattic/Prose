using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --scan-edge-duplicates --universe &lt;slug&gt; [--json]
///
/// Report-only scan for the drift <c>link_entities</c> can introduce: the same (Source, Target)
/// pair recorded with more than one distinct <see cref="Edge.RelationType"/> wording — e.g.
/// "Kyle —owns→ Silence" and "Kyle —has→ Silence" as two separate live rows instead of one.
/// Deliberately makes no judgment call about whether a flagged pair is a true duplicate (same
/// fact, different wording) or two genuinely true facts (e.g. Kyle both "owns" AND "wields" the
/// same weapon) — only a human/LLM with real story knowledge can tell those apart. That judgment
/// is executed via <c>prose --merge-edge</c> after review; see <see cref="MergeEdgeCli"/>.
///
/// Same report-only shape as <see cref="DuplicateEntityScanBroadCli"/> for entities.
/// </summary>
public static class ScanEdgeDuplicatesCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var universeSlug = Flag(args, "--universe");
        bool jsonMode = args.Contains("--json");

        if (string.IsNullOrWhiteSpace(universeSlug))
        {
            Console.Error.WriteLine("Usage: prose --scan-edge-duplicates --universe <slug> [--json]");
            return 2;
        }

        var canonDocs = services.GetRequiredService<CanonDocumentService>();
        var universeId = await canonDocs.ResolveUniverseIdAsync(universeSlug);
        if (universeId == null)
        {
            Console.Error.WriteLine($"[scan-edge-duplicates] Unknown universe '{universeSlug}'.");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var edges = await db.Edges.AsNoTracking()
            .Where(e => e.UniverseId == universeId.Value && e.InvalidatedAt == null)
            .ToListAsync();

        var entityNames = await db.Entities.AsNoTracking()
            .Where(e => e.UniverseId == universeId.Value)
            .Select(e => new { e.Id, e.Name, e.EntityType })
            .ToDictionaryAsync(e => e.Id);

        var groups = edges
            .GroupBy(e => (e.SourceId, e.TargetId))
            .Where(g => g.Select(e => e.RelationType.Trim().ToLowerInvariant()).Distinct().Count() > 1)
            .OrderBy(g => entityNames.TryGetValue(g.Key.SourceId, out var s) ? s.Name : g.Key.SourceId.ToString())
            .ToList();

        if (jsonMode)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                universe = universeSlug,
                group_count = groups.Count,
                groups = groups.Select(g => new
                {
                    source_id = g.Key.SourceId,
                    source_name = entityNames.TryGetValue(g.Key.SourceId, out var s) ? s.Name : null,
                    target_id = g.Key.TargetId,
                    target_name = entityNames.TryGetValue(g.Key.TargetId, out var t) ? t.Name : null,
                    edges = g.Select(e => new
                    {
                        edge_id = e.Id,
                        relation_type = e.RelationType,
                        description = e.Description,
                        weight = e.Weight,
                        sentiment = e.Sentiment,
                    }),
                }),
            }, new JsonSerializerOptions { WriteIndented = true }));
            return groups.Count > 0 ? 1 : 0;
        }

        Console.WriteLine($"Edge duplicate scan: {universeSlug}");
        Console.WriteLine();

        if (groups.Count == 0)
        {
            Console.WriteLine("✅ No (Source, Target) pairs with conflicting RelationType wording found.");
            return 0;
        }

        Console.WriteLine($"Found {groups.Count} pair(s) with more than one RelationType wording:");
        Console.WriteLine();

        foreach (var g in groups)
        {
            var sourceName = entityNames.TryGetValue(g.Key.SourceId, out var s) ? $"{s.Name} [{s.EntityType}]" : g.Key.SourceId.ToString();
            var targetName = entityNames.TryGetValue(g.Key.TargetId, out var t) ? $"{t.Name} [{t.EntityType}]" : g.Key.TargetId.ToString();
            Console.WriteLine($"{sourceName} → {targetName}");
            foreach (var e in g)
                Console.WriteLine($"    [{e.Id}] \"{e.RelationType}\"" +
                    (string.IsNullOrWhiteSpace(e.Description) ? "" : $"  — {e.Description}"));
            Console.WriteLine();
        }

        Console.WriteLine(
            "Review each pair by hand — decide whether it's the same relationship reworded, or two " +
            "genuinely distinct true facts. To collapse a true duplicate: " +
            "prose --merge-edge --keep <edgeId> --dedupe <edgeId> [--as <canonicalRelationType>] [--register-alias]");

        return 1;
    }

    static string? Flag(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }
}
