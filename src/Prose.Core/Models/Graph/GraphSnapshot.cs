namespace Prose.Core.Models.Graph;

public record GraphSnapshot
{
    /// <summary>
    /// The cache layout. 2 = nodes keyed per entity (PRS-T916); a cache written before that (0) merged
    /// same-named entities into one node and is ignored on load so the graph rebuilds.
    /// 3 = the name slug goes to the entity whose own slug it is, never to a game text (PRS-T925); a format-2
    /// cache may hold a glossary term on a canon entity's slug, so it is rebuilt too.
    /// </summary>
    public const int CurrentFormat = 3;

    public int Format { get; init; }
    public List<UniverseNode> Nodes { get; init; } = [];
    public List<UniverseEdge> Edges { get; init; } = [];
    public DateTime LastSaved { get; init; } = DateTime.UtcNow;
}
