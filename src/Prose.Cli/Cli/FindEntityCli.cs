using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>prose --find-entity --query "&lt;text&gt;" [--type character] [--limit N]</summary>
public static class FindEntityCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var query = Flag(args, "--query") ?? Flag(args, "--name");
        var type = Flag(args, "--type");
        var limit = int.TryParse(Flag(args, "--limit"), out var parsed) ? parsed : 40;
        if (string.IsNullOrWhiteSpace(query))
        {
            Console.Error.WriteLine("Usage: prose --find-entity --query \"<text>\" [--type <type>] [--limit N]");
            return 2;
        }

        var matches = await services.GetRequiredService<EntityLookupService>().FindAsync(query, type, limit);
        if (matches.Count == 0)
        {
            Console.WriteLine($"[find-entity] No entity matching \"{query}\".");
            return 0;
        }

        Console.WriteLine($"{"TYPE",-14} {"NAME",-32} GUID7                                 SLUG");
        Console.WriteLine(new string('-', 110));
        foreach (var match in matches)
        {
            var alias = match.MatchedAlias == null ? "" : $" (alias: {match.MatchedAlias})";
            Console.WriteLine($"{match.EntityType,-14} {Trunc(match.Name, 32),-32} {match.Id} {match.Slug}{alias}");
        }
        Console.WriteLine($"[find-entity] {matches.Count} match(es) for \"{query}\".");
        return 0;
    }

    static string? Flag(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
    static string Trunc(string text, int max) => text.Length <= max ? text : text[..(max - 1)] + "…";
}
