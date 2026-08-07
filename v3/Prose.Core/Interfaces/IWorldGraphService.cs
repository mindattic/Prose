using Prose.Core.Models.Graph;

namespace Prose.Core.Interfaces;

public interface IWorldGraphService
{
    int NodeCount { get; }
    int EdgeCount { get; }

    void EnsureLoaded();
    WorldNode? GetNode(string id);
    List<WorldNode> GetNodesByType(string nodeType);
    List<WorldNode> AllNodes();
    List<WorldEdge> AllEdgesRaw();
    List<WorldEdge> GetEdgesFrom(string nodeId);
    List<WorldEdge> GetEdgesTo(string nodeId);
    List<WorldEdge> GetAllEdges(string nodeId);
    string GetEntityBrief(string nodeId);
    string GetContextForNode(string nodeId);
    string? ResolveId(string nameOrAlias);
    string GetSceneContext(IEnumerable<string> entityNames, int neighborDepth = 1);
    List<WorldNode> GetNeighbors(string nodeId, int depth = 1);
    List<WorldNode> Search(string query);
    GraphStats GetStats();

    void AddNode(WorldNode node);
    void RemoveNode(string nameOrAlias);
    void AddEdge(WorldEdge edge);
    void Save();
    void Load();
    void Rebuild();
    void RebuildIndexes();
    int DeduplicateEdges();
}
