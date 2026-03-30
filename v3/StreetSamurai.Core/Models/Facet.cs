namespace StreetSamurai.Core.Models;

public record Facet
{
    public string Name { get; init; } = "";
    public string Label { get; init; } = "";
    public string Domain { get; init; } = "";
    public double Weight { get; init; }
    public string VoiceTone { get; init; } = "";
    public string VoiceStyle { get; init; } = "";
    public List<string> Triggers { get; init; } = [];
    public List<string> CoreMemories { get; init; } = [];
    public List<string> Prohibitions { get; init; } = [];
}

public record FacetState
{
    public double Wound { get; init; }
    public double Ideal { get; init; }
    public double Id { get; init; }
    public double Shadow { get; init; }
    public double Mask { get; init; }
    public double Ghost { get; init; }

    public Dictionary<string, double> ToDictionary() => new()
    {
        ["wound"] = Wound,
        ["ideal"] = Ideal,
        ["id"] = Id,
        ["shadow"] = Shadow,
        ["mask"] = Mask,
        ["ghost"] = Ghost,
    };

    public static FacetState FromDictionary(Dictionary<string, double> d) => new()
    {
        Wound = d.GetValueOrDefault("wound"),
        Ideal = d.GetValueOrDefault("ideal"),
        Id = d.GetValueOrDefault("id"),
        Shadow = d.GetValueOrDefault("shadow"),
        Mask = d.GetValueOrDefault("mask"),
        Ghost = d.GetValueOrDefault("ghost"),
    };
}
