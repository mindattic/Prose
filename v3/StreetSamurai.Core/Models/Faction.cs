namespace StreetSamurai.Core.Models;

public record Faction
{
    public string Name { get; init; } = "";
    public string Type { get; init; } = "faction";
    public List<string> Aliases { get; init; } = [];
    public string Description { get; init; } = "";
    public string Ideology { get; init; } = "";
    public string Territory { get; init; } = "";
    public string Leadership { get; init; } = "";
    public List<string> Methods { get; init; } = [];
    public List<string> Resources { get; init; } = [];
    public List<string> Goals { get; init; } = [];
    public string SourceFile { get; init; } = "";
}
