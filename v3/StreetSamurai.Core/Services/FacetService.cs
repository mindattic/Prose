using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

public class FacetService
{
    private readonly ICanonPathProvider _paths;
    private readonly YamlService _yaml;
    private Dictionary<string, FacetDefinition>? _cache;

    public FacetService(ICanonPathProvider paths, YamlService yaml)
    {
        _paths = paths;
        _yaml = yaml;
    }

    public Dictionary<string, FacetDefinition> LoadAllFacets()
    {
        if (_cache != null) return _cache;

        _cache = new Dictionary<string, FacetDefinition>();
        var dir = _paths.FacetsDir;
        if (!Directory.Exists(dir)) return _cache;

        foreach (var file in Directory.GetFiles(dir, "*.yaml"))
        {
            try
            {
                var data = _yaml.LoadDynamic(file);
                var name = data.GetValueOrDefault("name")?.ToString() ?? Path.GetFileNameWithoutExtension(file);
                var facet = new FacetDefinition
                {
                    Name = name,
                    Label = data.GetValueOrDefault("label")?.ToString() ?? $"[{name.ToUpperInvariant()}]",
                    Domain = data.GetValueOrDefault("domain")?.ToString() ?? "",
                    Triggers = ExtractList(data, "triggers"),
                    SystemPrompt = ExtractNestedString(data, "system_prompt") ?? "",
                    Model = data.GetValueOrDefault("model")?.ToString() ?? "claude-sonnet-4-6",
                    Temperature = double.TryParse(data.GetValueOrDefault("temperature")?.ToString(), out var t) ? t : 0.8,
                    CoreMemories = ExtractList(data, "core_memories"),
                    Prohibitions = ExtractNestedList(data, "voice", "prohibitions"),
                    VoiceTone = ExtractNestedString(data, "voice", "tone") ?? "",
                    VoiceStyle = ExtractNestedString(data, "voice", "style") ?? "",
                    SourceFile = file,
                };
                _cache[name] = facet;
            }
            catch { /* skip malformed */ }
        }
        return _cache;
    }

    public double ScoreFacet(FacetDefinition facet, List<string> contextTags, double characterWeight)
    {
        var triggerOverlap = facet.Triggers
            .Count(t => contextTags.Any(ct => ct.Contains(t, StringComparison.OrdinalIgnoreCase)));
        return triggerOverlap * characterWeight;
    }

    public (FacetDefinition lead, List<FacetDefinition> supporting) SelectFacets(
        FacetState weights,
        List<string> contextTags,
        List<string> recentLeads)
    {
        var facets = LoadAllFacets();
        var weightDict = weights.ToDictionary();

        var scored = facets.Values
            .Select(f => (facet: f, score: ScoreFacet(f, contextTags, weightDict.GetValueOrDefault(f.Name, 0.5))))
            .OrderByDescending(x => x.score)
            .ToList();

        // Enforce rotation: don't let same facet lead 3+ consecutive
        var lead = scored
            .Where(x => recentLeads.Count < 2 || recentLeads.TakeLast(2).Any(r => r != x.facet.Name))
            .First().facet;

        var supporting = scored
            .Where(x => x.facet.Name != lead.Name)
            .Take(2)
            .Select(x => x.facet)
            .ToList();

        return (lead, supporting);
    }

    public void InvalidateCache() => _cache = null;

    private static List<string> ExtractList(Dictionary<string, object> data, string key)
    {
        if (!data.TryGetValue(key, out var val)) return [];
        if (val is List<object> list) return list.Select(o => o.ToString() ?? "").ToList();
        return [];
    }

    private static List<string> ExtractNestedList(Dictionary<string, object> data, string parent, string child)
    {
        if (!data.TryGetValue(parent, out var parentVal)) return [];
        if (parentVal is Dictionary<object, object> dict && dict.TryGetValue(child, out var val))
        {
            if (val is List<object> list) return list.Select(o => o.ToString() ?? "").ToList();
        }
        return [];
    }

    private static string? ExtractNestedString(Dictionary<string, object> data, string key)
    {
        return data.TryGetValue(key, out var val) ? val?.ToString() : null;
    }

    private static string? ExtractNestedString(Dictionary<string, object> data, string parent, string child)
    {
        if (!data.TryGetValue(parent, out var parentVal)) return null;
        if (parentVal is Dictionary<object, object> dict && dict.TryGetValue(child, out var val))
            return val?.ToString();
        return null;
    }
}

public record FacetDefinition
{
    public string Name { get; init; } = "";
    public string Label { get; init; } = "";
    public string Domain { get; init; } = "";
    public List<string> Triggers { get; init; } = [];
    public string SystemPrompt { get; init; } = "";
    public string Model { get; init; } = "claude-sonnet-4-6";
    public double Temperature { get; init; } = 0.8;
    public List<string> CoreMemories { get; init; } = [];
    public List<string> Prohibitions { get; init; } = [];
    public string VoiceTone { get; init; } = "";
    public string VoiceStyle { get; init; } = "";
    public string SourceFile { get; init; } = "";
}
