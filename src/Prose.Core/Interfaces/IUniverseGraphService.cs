using Prose.Core.Models.Graph;

namespace Prose.Core.Interfaces;

public interface IUniverseGraphService
{
    int NodeCount { get; }
    int EdgeCount { get; }

    void EnsureLoaded();
    UniverseNode? GetNode(string id);
    List<UniverseNode> GetNodesByType(string nodeType);
    List<UniverseNode> AllNodes();
    List<UniverseEdge> AllEdgesRaw();
    List<UniverseEdge> GetEdgesFrom(string nodeId);
    List<UniverseEdge> GetEdgesTo(string nodeId);
    List<UniverseEdge> GetAllEdges(string nodeId);
    string GetEntityBrief(string nodeId);
    string GetContextForNode(string nodeId);
    string? ResolveId(string nameOrAlias);
    string GetSceneContext(IEnumerable<string> entityNames, int neighborDepth = 1);
    List<UniverseNode> GetNeighbors(string nodeId, int depth = 1);
    List<UniverseNode> Search(string query);
    GraphStats GetStats();

    void AddNode(UniverseNode node);
    void RemoveNode(string nameOrAlias);
    void AddEdge(UniverseEdge edge);
    void Save();
    void Load();
    void Rebuild();
    void RebuildIndexes();
    int DeduplicateEdges();
}
