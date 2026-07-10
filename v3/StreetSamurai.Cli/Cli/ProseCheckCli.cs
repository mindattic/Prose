using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// ss --prose-check (--slug &lt;nodeSlug&gt; | --id &lt;beatId&gt;) [--all] [--json]
/// Runs the deterministic ProsePatternGuard linter on a node's beats or a single beat.
/// --all includes Low-severity sentence-length checks (default: shows Cliché + PseudoProfound + OnTheNose + ItalicisedDialogue only)
/// </summary>
public static class ProseCheckCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? nodeSlug = null;
        Guid? beatId = null;
        bool all = args.Contains("--all");
        bool json = args.Contains("--json");

        for (int i = 0; i < args.Length - 1; i++)
        {
            switch (args[i])
            {
                case "--slug": nodeSlug = args[i + 1]; i++; break;
                case "--id":
                    if (Guid.TryParse(args[i + 1], out var g)) { beatId = g; i++; }
                    break;
            }
        }

        if (nodeSlug == null && beatId == null)
        {
            Console.Error.WriteLine("Usage: ss --prose-check (--slug <nodeSlug> | --id <beatId>) [--all] [--json]");
            return 1;
        }

        var guard = services.GetRequiredService<ProsePatternGuard>();
        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        using var db = dbFactory.CreateDbContext();

        var totalViolations = 0;

        if (beatId.HasValue)
        {
            var beat = await db.Beats.AsNoTracking().FirstOrDefaultAsync(b => b.Id == beatId.Value);
            if (beat == null) { Console.Error.WriteLine($"Beat {beatId} not found."); return 1; }
            var violations = guard.Check(beat.Text ?? "");
            PrintViolations(violations, $"Beat #{beat.Number}", beat.Text ?? "", all, json);
            totalViolations += violations.Count;
        }
        else
        {
            var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(s => s.Slug == nodeSlug);
            if (node == null) { Console.Error.WriteLine($"Node '{nodeSlug}' not found."); return 1; }

            var beats = await (
                from sb in db.BeatNodes
                join b in db.Beats on sb.BeatId equals b.Id
                where sb.NodeId == node.Id && sb.IsEnabled
                orderby sb.SortKey
                select new { b.Id, b.Number, b.Text }
            ).ToListAsync();

            if (beats.Count == 0) { Console.Error.WriteLine("No beats found for this node."); return 1; }

            Console.WriteLine($"Checking {beats.Count} beats in '{node.Slug}'…");
            foreach (var beat in beats)
            {
                var violations = guard.Check(beat.Text ?? "");
                if (violations.Count > 0)
                {
                    PrintViolations(violations, $"Beat #{beat.Number}", beat.Text ?? "", all, json);
                    totalViolations += violations.Count;
                }
            }
        }

        if (totalViolations == 0)
            Console.WriteLine("✔ No violations found.");
        else
            Console.WriteLine($"\n{totalViolations} violation(s) total.");

        return totalViolations > 0 ? 1 : 0;
    }

    private static void PrintViolations(
        List<ProseViolation> violations, string label, string text, bool all, bool json)
    {
        var filtered = all
            ? violations
            : violations.Where(v => v.Category != ProseViolationCategory.SentenceLength).ToList();

        if (filtered.Count == 0) return;

        if (json)
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
            {
                beat = label,
                violations = filtered.Select(v => new
                {
                    category = v.Category.ToString(),
                    match = v.Match,
                    offset = v.CharOffset,
                    rule = v.Rule,
                    suggestion = v.Suggestion,
                })
            }));
        }
        else
        {
            Console.WriteLine($"\n— {label} ({filtered.Count} violation(s)) —");
            foreach (var v in filtered)
            {
                var context = ExtractContext(text, v.CharOffset, 60);
                Console.WriteLine($"  [{v.Category}] \"{v.Match}\"");
                Console.WriteLine($"    Rule: {v.Rule}");
                if (v.Suggestion != null) Console.WriteLine($"    Fix:  {v.Suggestion}");
                Console.WriteLine($"    …{context}…");
            }
        }
    }

    private static string ExtractContext(string text, int offset, int window)
    {
        var start = Math.Max(0, offset - 20);
        var end = Math.Min(text.Length, offset + window);
        return text[start..end].Replace('\n', ' ');
    }
}
