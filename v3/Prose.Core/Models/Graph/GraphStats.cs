namespace Prose.Core.Models.Graph;

public record GraphStats
{
    public Dictionary<string, int> NodesByType { get; init; } = new();
    public Dictionary<string, int> EdgesByType { get; init; } = new();
    public int TotalNodes => NodesByType.Values.Sum();
    public int TotalEdges => EdgesByType.Values.Sum();
}
