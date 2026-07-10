namespace StreetSamurai.Core.Services;

/// <summary>
/// Given a scene location, returns 0-2 subtle anomaly references appropriate to the area.
/// These are injected into scene prompts as atmospheric detail — the New Weird layer
/// bleeding into every scene without being a plot point.
/// </summary>
public class AmbientAnomalyService
{
    private readonly WorldbuildingDocRepository docRepo;
    private readonly DistrictRepository districtRepo;

    // Thread-safe lazy cache — populated once on first access from any circuit thread.
    private readonly Lazy<List<(string title, string snippet, List<string> tags)>> anomalyCache;

    public AmbientAnomalyService(WorldbuildingDocRepository docRepo, DistrictRepository districtRepo)
    {
        this.docRepo = docRepo;
        this.districtRepo = districtRepo;
        this.anomalyCache = new Lazy<List<(string, string, List<string>)>>(
            BuildCache, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>Get 0-2 ambient anomaly hints for a scene location.</summary>
    public List<string> GetAmbientHints(string? location, int maxHints = 2)
    {
        var cache = anomalyCache.Value;
        if (cache.Count == 0) return [];

        var hints = new List<string>();
        var locationLower = (location ?? "").ToLowerInvariant();

        // 60% chance of any anomaly appearing in a scene
        if (!RandomGatePasses()) return [];

        // Prefer location-relevant anomalies, fall back to universal ones
        var relevant = cache
            .Where(a => a.tags.Any(t => locationLower.Contains(t)))
            .ToList();

        var pool = relevant.Count >= 2 ? relevant : cache;

        // Pick 1-2 random anomalies
        var count = Random.Shared.Next(1, Math.Min(maxHints + 1, pool.Count + 1));
        var selected = pool.OrderBy(_ => Random.Shared.Next()).Take(count);

        foreach (var anomaly in selected)
        {
            hints.Add($"[Ambient — weave subtly, do NOT explain or make it a plot point]: {anomaly.snippet}");
        }

        return hints;
    }

    /// <summary>Format hints as a prompt injection block.</summary>
    public string FormatHints(string? location)
    {
        var hints = GetAmbientHints(location);
        if (hints.Count == 0) return "";

        return "AMBIENT ANOMALIES (subtle background detail — mention in passing, never explain):\n"
            + string.Join("\n", hints);
    }

    protected virtual bool RandomGatePasses() => Random.Shared.NextDouble() <= 0.6;

    private List<(string title, string snippet, List<string> tags)> BuildCache()
    {
        return docRepo.GetAll()
            .Where(d => d.Tags.Any(t =>
                t.Contains("anomaly") || t.Contains("inexplicable") || t.Contains("new_weird") ||
                t.Contains("ghost_building") || t.Contains("lost_block")))
            .Select(d =>
            {
                var body = d.Body.Length > 0 ? d.Body : d.Title;
                // Extract a 1-2 sentence snippet for the prompt
                var sentences = body.Split(new[] { ". ", ".\n" }, StringSplitOptions.RemoveEmptyEntries);
                var snippet = sentences.Length > 1
                    ? sentences[0] + ". " + sentences[1] + "."
                    : sentences.FirstOrDefault() ?? d.Title;
                if (snippet.Length > 200) snippet = snippet[..200] + "...";

                return (d.Title, snippet, d.Tags.Select(t => t.ToLowerInvariant()).ToList());
            })
            .ToList();
    }
}

