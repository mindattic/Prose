namespace StreetSamurai.Core.Models.Graph;

public record GraphSnapshot
{
    public List<WorldNode> Nodes { get; init; } = [];
    public List<WorldEdge> Edges { get; init; } = [];
    public DateTime LastSaved { get; init; } = DateTime.UtcNow;
}
