using Prose.Core.Models.Canon;

namespace Prose.Core.Services;

/// <summary>
/// Assembles rich ambient context for scene generation — sensory profiles,
/// weather, nearby anomalies, local wildlife, ghost buildings.
/// Injected into every generation prompt so scenes feel like the world.
///
/// 2026-08-28: absorbed AmbientAnomalyService. Both services auto-fired on the same
/// Location gate and independently pulled anomaly-tagged worldbuilding docs into the same
/// prompt under two different labels ("AMBIENT STRANGENESS" here + "AMBIENT ANOMALIES"
/// there), from overlapping tag pools — one beat could get the same doc injected twice as
/// two apparently different anomalies. The anomaly layer now lives only here: one cached
/// pool over the full tag set, one probability gate, one labeled section per beat.
/// </summary>
public class SceneContextBuilder
{
    private readonly WorldbuildingDocRepository docRepo;
    private readonly DistrictRepository districtRepo;

    // Thread-safe lazy cache — populated once on first access from any circuit thread.
    private readonly Lazy<List<(string title, string snippet, List<string> tags)>> anomalyCache;

    public SceneContextBuilder(WorldbuildingDocRepository docRepo, DistrictRepository districtRepo)
    {
        this.docRepo = docRepo;
        this.districtRepo = districtRepo;
        this.anomalyCache = new Lazy<List<(string, string, List<string>)>>(
            BuildAnomalyCache, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>Build a sensory/ambient context packet for a scene location.</summary>
    public string BuildAmbientContext(string? location, string? timeOfDay = null, string? weather = null)
    {
        var parts = new List<string>();

        // District sensory profile
        var district = ResolveDistrict(location);
        if (district != null)
        {
            parts.Add($"DISTRICT: {district.Name}");
            if (!string.IsNullOrWhiteSpace(district.Description))
                parts.Add($"ATMOSPHERE: {Truncate(district.Description, 300)}");
        }

        // Sensory details from documents tagged with this district
        var sensoryDocs = FindDocsByTags(["sensory", district?.Name?.ToLowerInvariant() ?? "shelf"]);
        foreach (var doc in sensoryDocs.Take(2))
            parts.Add($"SENSORY DETAIL: {Truncate(doc.Body.Length > 0 ? doc.Body : doc.Title, 200)}");

        // Time and weather
        if (!string.IsNullOrWhiteSpace(timeOfDay))
            parts.Add($"TIME: {timeOfDay}");
        if (!string.IsNullOrWhiteSpace(weather))
            parts.Add($"WEATHER: {weather}");

        // Nearby anomalies — the New Weird layer bleeding into the scene without being a plot
        // point. Location-relevant anomalies preferred; universal pool as fallback; probability
        // gate so the weird stays background texture, not a per-beat fixture.
        foreach (var hint in GetAmbientAnomalyHints(location))
            parts.Add(hint);

        // Urban wildlife
        var wildlife = FindDocsByTags(["urban_wildlife"]);
        if (wildlife.Count > 0)
        {
            var animal = wildlife[Random.Shared.Next(wildlife.Count)];
            parts.Add($"BACKGROUND WILDLIFE (mention casually): {Truncate(animal.Title, 100)}");
        }

        return parts.Count > 0
            ? "AMBIENT WORLD CONTEXT (use to ground the scene in sensory reality):\n" + string.Join("\n", parts)
            : "";
    }

    /// <summary>Get 0-2 ambient anomaly hints for a scene location (ex-AmbientAnomalyService).</summary>
    internal List<string> GetAmbientAnomalyHints(string? location, int maxHints = 2)
    {
        var cache = anomalyCache.Value;
        if (cache.Count == 0) return [];

        if (!RandomGatePasses()) return [];

        var locationLower = (location ?? "").ToLowerInvariant();

        // Prefer location-relevant anomalies, fall back to universal ones
        var relevant = cache
            .Where(a => a.tags.Any(t => locationLower.Contains(t)))
            .ToList();

        var pool = relevant.Count >= 1 ? relevant : cache;

        // Pick 1-2 random anomalies
        var count = Random.Shared.Next(1, Math.Min(maxHints + 1, pool.Count + 1));
        var selected = pool.OrderBy(_ => Random.Shared.Next()).Take(count);

        return selected
            .Select(a => $"AMBIENT STRANGENESS (weave subtly, do NOT explain or make it a plot point): {a.snippet}")
            .ToList();
    }

    /// <summary>60% chance any anomaly texture appears in a scene. Virtual for test override.</summary>
    protected virtual bool RandomGatePasses() => Random.Shared.NextDouble() <= 0.6;

    private List<(string title, string snippet, List<string> tags)> BuildAnomalyCache()
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

    private DistrictData? ResolveDistrict(string? location)
    {
        if (string.IsNullOrWhiteSpace(location)) return null;
        var loc = location.ToLowerInvariant();

        // Try to match district name in the location string
        return districtRepo.GetAll().FirstOrDefault(d =>
            loc.Contains(d.Name.ToLowerInvariant()));
    }

    private List<WorldbuildingDocument> FindDocsByTags(string[] tags)
    {
        return docRepo.GetAll()
            .Where(d => tags.Any(t => d.Tags.Any(dt => dt.Contains(t, StringComparison.OrdinalIgnoreCase))))
            .Take(5)
            .ToList();
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "...";
}
