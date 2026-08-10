using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --duplicate-entity-scan --universe &lt;slug&gt; [--entity-type &lt;type&gt;] [--json]
///
/// Scans a universe's Entities (default EntityType "character"; pass --entity-type to check
/// "faction", "place", etc.) for duplicate/near-duplicate names that aren't explained by
/// legitimate cross-book disambiguation (OriginNodeId). See
/// <see cref="DuplicateEntityScanService"/> for the detection logic and the real bug
/// ("Boris Johansen" / "Boris Johanssen", TEST's Bear, 2026-08-10) that motivated it.
///
/// No LLM calls — fast deterministic checks only.
///
/// Exit codes: 0 = none found, 1 = candidates found (informational, not a hard block —
/// resolving a duplicate requires reading the actual prose to know which row is canonical,
/// exactly as this session's own investigation did; this tool finds candidates, it doesn't
/// resolve them).
/// </summary>
public static class DuplicateEntityScanCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var universeSlug = Flag(args, "--universe");
        var entityType = Flag(args, "--entity-type") ?? "character";
        bool jsonMode = args.Contains("--json");

        if (string.IsNullOrWhiteSpace(universeSlug))
        {
            Console.Error.WriteLine("Usage: prose --duplicate-entity-scan --universe <slug> [--entity-type <type>] [--json]");
            return 2;
        }

        var canonDocs = services.GetRequiredService<CanonDocumentService>();
        var universeId = await canonDocs.ResolveUniverseIdAsync(universeSlug);
        if (universeId == null)
        {
            Console.Error.WriteLine($"[duplicate-entity-scan] Unknown universe '{universeSlug}'.");
            return 2;
        }

        var scanSvc = services.GetRequiredService<DuplicateEntityScanService>();
        var groups = await scanSvc.ScanAsync(universeId.Value, entityType);

        if (jsonMode)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                universe = universeSlug,
                entity_type = entityType,
                group_count = groups.Count,
                groups = groups.Select(g => new
                {
                    matched_on = g.MatchedOn,
                    candidates = g.Candidates.Select(c => new
                    {
                        id = c.Id,
                        name = c.Name,
                        origin_node_id = c.OriginNodeId,
                        is_active = c.IsActive,
                        description_snippet = c.DescriptionSnippet,
                        mention_count = c.MentionCount,
                    }),
                }),
            }, new JsonSerializerOptions { WriteIndented = true }));
            return groups.Count > 0 ? 1 : 0;
        }

        Console.WriteLine($"Duplicate entity scan: {universeSlug} ({entityType} entities only)");
        Console.WriteLine();

        if (groups.Count == 0)
        {
            Console.WriteLine("✅ No duplicate-name candidates found.");
            return 0;
        }

        foreach (var g in groups)
        {
            Console.WriteLine($"⚠️  {g.MatchedOn}");
            foreach (var c in g.Candidates)
            {
                var active = c.IsActive ? "active" : "RETIRED";
                var scope = c.OriginNodeId is { } id ? $"scoped to {id}" : "universe-wide (unscoped)";
                Console.WriteLine($"   - {c.Name}  [{active}, {scope}, {c.MentionCount} beat mention(s)]  id={c.Id}");
                if (c.DescriptionSnippet != null)
                    Console.WriteLine($"     \"{c.DescriptionSnippet}\"");
            }
            Console.WriteLine();
        }

        Console.WriteLine(new string('─', 60));
        Console.WriteLine($"⚠️  {groups.Count} duplicate-name candidate group(s). " +
            "Mention counts come from BeatEntityMentions, which may be sparsely populated for " +
            "older content — a count of 0 does NOT prove an entity is unused; read the actual " +
            "prose before merging or retiring either row (see 2026-08-10 TEST/Bear investigation).");

        return 1;
    }

    private static string? Flag(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }
}
