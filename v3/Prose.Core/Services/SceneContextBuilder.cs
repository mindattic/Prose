using Prose.Core.Models.Canon;

namespace Prose.Core.Services;

/// <summary>
/// Assembles rich ambient context for scene generation — sensory profiles,
/// weather, nearby anomalies, local wildlife, ghost buildings.
/// Injected into every generation prompt so scenes feel like the world.
/// </summary>
public class SceneContextBuilder
{
    private readonly DatabaseService db;
    private readonly WorldGraphService graph;
    private readonly WorldbuildingDocRepository docRepo;
    private readonly DistrictRepository districtRepo;

    public SceneContextBuilder(
        DatabaseService db, WorldGraphService graph,
        WorldbuildingDocRepository docRepo, DistrictRepository districtRepo)
    {
        this.db = db;
        this.graph = graph;
        this.docRepo = docRepo;
        this.districtRepo = districtRepo;
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

        // Nearby anomalies (from the New Weird layer)
        var anomalies = FindDocsByTags(["anomaly", "inexplicable"]);
        if (anomalies.Count > 0)
        {
            var anomaly = anomalies[Random.Shared.Next(anomalies.Count)];
            parts.Add($"AMBIENT STRANGENESS (weave subtly, don't explain): {Truncate(anomaly.Title, 100)} — {Truncate(anomaly.Body.Length > 0 ? anomaly.Body : anomaly.Title, 150)}");
        }

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
