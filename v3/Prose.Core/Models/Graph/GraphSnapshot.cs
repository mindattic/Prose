namespace Prose.Core.Models.Graph;

public record GraphSnapshot
{
    public List<UniverseNode> Nodes { get; init; } = [];
    public List<UniverseEdge> Edges { get; init; } = [];
    public DateTime LastSaved { get; init; } = DateTime.UtcNow;
}
