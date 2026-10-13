using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// Questions asked of the story itself, answered from beat contributions (WO:01a13278), never
/// from a separate record:
/// <code>
///   prose --character-history --entity &lt;id|slug&gt; [--in-book &lt;CODE&gt;]
///   prose --story-edges --entity &lt;id|slug&gt; [--relation &lt;relation&gt;]
/// </code>
/// Every line is cited (book, chapter, beat). A beat that tags the character with no line reads
/// [missing]; a line written against other text reads [stale].
/// </summary>
public static class StoryCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var entityArg = Flag(args, "--entity");
        if (string.IsNullOrWhiteSpace(entityArg))
        {
            Console.Error.WriteLine("Usage: prose --character-history --entity <id|slug> [--in-book <CODE>] | prose --story-edges --entity <id|slug> [--relation <r>]");
            return 2;
        }
        var resolved = await services.GetRequiredService<EntityMentionService>().ResolveEntityAsync(entityArg);
        if (resolved is not { } entity) { Console.Error.WriteLine($"[story] entity not found: {entityArg}"); return 1; }
        var svc = services.GetRequiredService<BeatContributionService>();

        if (args.Contains("--story-edges"))
        {
            var edges = await svc.EdgesAsync(entity.Id, Flag(args, "--relation"));
            Console.WriteLine($"[story-edges] {entity.Name} — {edges.Count} relation(s):");
            foreach (var e in edges)
                Console.WriteLine($"  {e.SourceName} {e.Relation} {e.TargetName}  ({e.BookCode} ch{e.Chapter} #{e.BeatNumber})" +
                                  (e.State == "current" ? "" : $" [{e.State}]") + $"\n      {e.Line}");
            return 0;
        }

        var history = await svc.HistoryAsync(entity.Id, Flag(args, "--in-book"));
        Console.WriteLine($"[character-history] {entity.Name} — {history.Count} beat(s), " +
                          $"{history.Count(h => h.State == "missing")} missing, {history.Count(h => h.State == "stale")} stale:");
        foreach (var h in history)
        {
            Console.WriteLine($"  {h.BookCode} ch{h.Chapter} #{h.BeatNumber}" + (h.State == "current" ? "" : $" [{h.State}]") +
                              (h.Line is null ? "" : $" — {h.Line}"));
            foreach (var e in h.Edges) Console.WriteLine($"      {e.Relation} → {e.Target}");
        }
        return history.Any(h => h.State != "current") ? 2 : 0;
    }

    private static string? Flag(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
