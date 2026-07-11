using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// ss --consistency-audit [--all | --since &lt;hours&gt;]
///
/// Surfaces factual claims that contradict across different story nodes.
/// Reads the existing ContinuityClaims table (no LLM calls). Filters to
/// groups where conflicting claims originate from different StorySlug values.
/// Exit 0 = clean, 1 = conflicts found.
/// </summary>
public static class CrossStoryConsistencyAuditCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var svc = sp.GetRequiredService<CrossStoryConsistencyService>();

        DateTime? since = null;
        var sinceArg = args.SkipWhile(a => a != "--since").Skip(1).FirstOrDefault();
        if (sinceArg != null && double.TryParse(sinceArg, out var hours))
            since = DateTime.UtcNow.AddHours(-hours);

        Console.WriteLine(since.HasValue
            ? $"[consistency] Checking cross-story conflicts since {since:yyyy-MM-dd HH:mm} UTC..."
            : "[consistency] Checking all cross-story conflicts...");

        var report = await svc.GetCrossStoryConflictsAsync(since);

        Console.WriteLine();
        if (report.Conflicts.Count == 0)
        {
            Console.WriteLine("✓ No cross-story contradictions found.");
            return 0;
        }

        Console.WriteLine($"Cross-story contradictions ({report.Conflicts.Count}):");
        Console.WriteLine(new string('─', 80));

        foreach (var c in report.Conflicts)
        {
            Console.WriteLine($"  Entity  : {c.EntityName} ({c.EntityKind}) [{c.EntityId}]");
            Console.WriteLine($"  Claim   : {c.Predicate}");
            Console.WriteLine($"  Majority: \"{c.MajorityObject}\" — {c.MajorityCount} claim(s) from: {string.Join(", ", c.MajorityStories)}");
            Console.WriteLine($"  Minority: \"{c.MinorityObject}\" — {c.MinorityCount} claim(s) from: {string.Join(", ", c.MinorityStories)}");
            Console.WriteLine();
        }

        Console.WriteLine($"Resolve via: ss --continuity contradictions");
        return 1;
    }
}
