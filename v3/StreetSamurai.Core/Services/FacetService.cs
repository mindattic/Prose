using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

public class FacetService
{
    private readonly DatabaseService db;
    private Dictionary<string, FacetDefinition>? _cache;

    public FacetService(DatabaseService db)
    {
        this.db = db;
    }

    public Dictionary<string, FacetDefinition> LoadAllFacets()
    {
        if (_cache != null) return _cache;

        _cache = db.Facets.GroupBy(f => f.Name).ToDictionary(
            g => g.Key,
            g => { var f = g.First(); return new FacetDefinition
            {
                Name = f.Name,
                Label = f.Label.Length > 0 ? f.Label : $"[{f.Name.ToUpperInvariant()}]",
                Domain = f.Domain,
                Triggers = f.Triggers,
                SystemPrompt = f.SystemPrompt,
                Model = f.Model,
                Temperature = f.Temperature,
                CoreMemories = f.CoreMemories,
                Prohibitions = f.Voice.Prohibitions,
                VoiceTone = f.Voice.Tone,
                VoiceStyle = f.Voice.Style,
            }; });

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
}

public record FacetDefinition
{
    public string Name { get; init; } = "";
    public string Label { get; init; } = "";
    public string Domain { get; init; } = "";
    public List<string> Triggers { get; init; } = [];
    public string SystemPrompt { get; init; } = "";
    public string Model { get; init; } = Constants.Defaults.DefaultModel;
    public double Temperature { get; init; } = 0.8;
    public List<string> CoreMemories { get; init; } = [];
    public List<string> Prohibitions { get; init; } = [];
    public string VoiceTone { get; init; } = "";
    public string VoiceStyle { get; init; } = "";
}
