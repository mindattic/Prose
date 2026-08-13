using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --behavior-check --slug &lt;nodeSlug&gt; --character &lt;characterId&gt;
/// LLM-checks each beat of the node against the character's behavioral rules.
/// </summary>
public static class BehaviorCheckCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? nodeSlug = null;
        Guid? characterId = null;

        for (int i = 0; i < args.Length - 1; i++)
        {
            switch (args[i])
            {
                case "--slug": nodeSlug = args[i + 1]; i++; break;
                case "--character":
                    if (Guid.TryParse(args[i + 1], out var g)) characterId = g;
                    i++;
                    break;
            }
        }

        if (nodeSlug == null || characterId == null)
        {
            Console.Error.WriteLine("Usage: prose --behavior-check --slug <nodeSlug> --character <characterId>");
            return 1;
        }

        var enforcer = services.GetRequiredService<BehavioralInvariantEnforcer>();
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(s => s.Slug == nodeSlug || s.NodeCode == nodeSlug);
        if (node == null) { Console.Error.WriteLine($"Node '{nodeSlug}' not found."); return 1; }

        var beats = await (
            from sb in db.BeatNodes
            join b in db.Beats on sb.BeatId equals b.Id
            where sb.NodeId == node.Id && true
            orderby sb.SortKey
            select new { b.Id, b.Number, b.Text }
        ).ToListAsync();

        Console.WriteLine($"Checking {beats.Count} beats against character behavioral rules (LLM)…");

        int totalViolations = 0;
        foreach (var beat in beats)
        {
            Console.Write($"  Beat #{beat.Number}… ");
            var violations = await enforcer.EnforceAsync(beat.Text ?? "", characterId.Value);
            if (violations.Count == 0)
            {
                Console.WriteLine("✔");
                continue;
            }

            Console.WriteLine($"{violations.Count} violation(s)");
            foreach (var v in violations)
            {
                Console.WriteLine($"    [{v.RuleBucket}] Rule: {v.RuleText}");
                Console.WriteLine($"    Issue: {v.Explanation}");
            }
            totalViolations += violations.Count;
        }

        if (totalViolations == 0)
            Console.WriteLine("✔ No behavioral violations found.");
        else
            Console.WriteLine($"\n{totalViolations} behavioral violation(s) found.");

        return totalViolations > 0 ? 1 : 0;
    }
}
