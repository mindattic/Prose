namespace StreetSamurai.Core.Models.Graph;

public record WorldNode
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string NodeType { get; init; } = "";
    public string CanonStatus { get; init; } = "canon";
    public Dictionary<string, string> Properties { get; init; } = new();
    public string SourceFile { get; init; } = "";
    public string ExtractedFrom { get; init; } = "";
}
