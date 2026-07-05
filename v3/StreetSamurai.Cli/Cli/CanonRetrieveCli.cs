using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// <c>ss --canon-retrieve "&lt;query&gt;" [--k N] [--types t1,t2]</c> — show what the
/// engine's universal canon reach pulls for a query, ACROSS ALL entity types.
/// Verifies the full-interconnect path (the embedding index covers every type, so
/// previously-"dead" inventory like cyberware/materials/pharma now surfaces).
/// </summary>
public static class CanonRetrieveCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? query = null, types = null;
        int k = 12;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--canon-retrieve": if (i + 1 < args.Length && !args[i + 1].StartsWith("--")) query = args[++i]; break;
                case "--k":     if (i + 1 < args.Length && int.TryParse(args[++i], out var n)) k = n; break;
                case "--types": if (i + 1 < args.Length) types = args[++i]; break;
            }
        }
        if (string.IsNullOrWhiteSpace(query))
        {
            Console.Error.WriteLine("[canon-retrieve] Usage: ss --canon-retrieve \"<query>\" [--k N] [--types character,pharmaceutical]");
            return 1;
        }

        var retrieval = services.GetRequiredService<CanonRetrievalService>();
        var onlyTypes = string.IsNullOrWhiteSpace(types)
            ? null
            : types.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var hits = await retrieval.RetrieveAsync(query, k, onlyTypes);
        if (hits.Count == 0) { Console.WriteLine("[canon-retrieve] No hits (is the embedding index populated?)."); return 0; }

        Console.WriteLine($"[canon-retrieve] {hits.Count} hits for \"{query}\":\n");
        foreach (var h in hits)
            Console.WriteLine($"  {h.Similarity,6:F3}  [{h.Type,-14}] {h.Name}");
        Console.WriteLine($"\n[canon-retrieve] types surfaced: {string.Join(", ", hits.Select(h => h.Type).Distinct().OrderBy(t => t))}");
        return 0;
    }
}
