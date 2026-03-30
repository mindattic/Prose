namespace StreetSamurai.Core.Models;

public record District
{
    public string Name { get; init; } = "";
    public string Type { get; init; } = "place";
    public List<string> Aliases { get; init; } = [];
    public string Description { get; init; } = "";
    public DistrictAtmosphere Atmosphere { get; init; } = new();
    public string Demographics { get; init; } = "";
    public List<string> Dangers { get; init; } = [];
    public List<string> Opportunities { get; init; } = [];
    public List<string> StoryHooks { get; init; } = [];
    public string SourceFile { get; init; } = "";
}

public record DistrictAtmosphere
{
    public List<string> Sights { get; init; } = [];
    public List<string> Sounds { get; init; } = [];
    public List<string> Smells { get; init; } = [];
    public string Feel { get; init; } = "";
}
