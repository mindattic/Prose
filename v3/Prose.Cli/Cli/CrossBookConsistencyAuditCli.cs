using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --consistency-audit [--all | --since &lt;hours&gt;]
///
/// Surfaces factual claims that contradict across different book nodes.
/// Reads the existing ContinuityClaims table (no LLM calls). Filters to
/// groups where conflicting claims originate from different BookSlug values.
/// Exit 0 = clean, 1 = conflicts found.
/// </summary>
public static class CrossBookConsistencyAuditCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var svc = sp.GetRequiredService<CrossBookConsistencyService>();

        DateTime? since = null;
        var sinceArg = args.SkipWhile(a => a != "--since").Skip(1).FirstOrDefault();
        if (sinceArg != null && double.TryParse(sinceArg, out var hours))
            since = DateTime.UtcNow.AddHours(-hours);

        Console.WriteLine(since.HasValue
            ? $"[consistency] Checking cross-book conflicts since {since:yyyy-MM-dd HH:mm} UTC..."
            : "[consistency] Checking all cross-book conflicts...");

        var report = await svc.GetCrossBookConflictsAsync(since);

        Console.WriteLine();
        if (report.Conflicts.Count == 0)
        {
            Console.WriteLine("✓ No cross-book contradictions found.");
            return 0;
        }

        Console.WriteLine($"Cross-book contradictions ({report.Conflicts.Count}):");
        Console.WriteLine(new string('─', 80));

        foreach (var c in report.Conflicts)
        {
            Console.WriteLine($"  Entity  : {c.EntityName} ({c.EntityKind}) [{c.EntityId}]");
            Console.WriteLine($"  Claim   : {c.Predicate}");
            Console.WriteLine($"  Majority: \"{c.MajorityObject}\" — {c.MajorityCount} claim(s) from: {string.Join(", ", c.MajorityBooks)}");
            Console.WriteLine($"  Minority: \"{c.MinorityObject}\" — {c.MinorityCount} claim(s) from: {string.Join(", ", c.MinorityBooks)}");
            Console.WriteLine();
        }

        Console.WriteLine($"Resolve via: prose --continuity contradictions");
        return 1;
    }
}
