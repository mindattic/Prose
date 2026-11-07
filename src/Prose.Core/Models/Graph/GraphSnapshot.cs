namespace Prose.Core.Models.Graph;

public record GraphSnapshot
{
    /// <summary>
    /// The cache layout. 2 = nodes keyed per entity (PRS-T916); a cache written before that (0) merged
    /// same-named entities into one node and is ignored on load so the graph rebuilds.
    /// </summary>
    public const int CurrentFormat = 2;

    public int Format { get; init; }
    public List<UniverseNode> Nodes { get; init; } = [];
    public List<UniverseEdge> Edges { get; init; } = [];
    public DateTime LastSaved { get; init; } = DateTime.UtcNow;
}
