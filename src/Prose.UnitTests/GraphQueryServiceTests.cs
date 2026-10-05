using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

[TestFixture]
public class GraphQueryServiceTests
{
    static readonly Guid A = Guid.NewGuid(), B = Guid.NewGuid(), C = Guid.NewGuid(), D = Guid.NewGuid(), E = Guid.NewGuid();

    static Edge Link(Guid s, Guid t, string type) => new() { SourceId = s, TargetId = t, RelationType = type };

    [Test]
    public void ShortestPath_takes_fewest_hops_and_walks_edges_in_either_direction()
    {
        var edges = new List<Edge>
        {
            Link(A, B, "works_for"),
            Link(B, C, "rival_of"),
            Link(C, D, "parent_of"),
            Link(D, A, "mentor_of"),   // A—D is one hop when walked backwards
        };
        var path = GraphQueryService.ShortestPath(edges, A, D, maxHops: 4);
        Assert.That(path, Is.Not.Null);
        Assert.That(path!.Select(e => e.RelationType), Is.EqualTo(new[] { "mentor_of" }));
    }

    [Test]
    public void ShortestPath_returns_chain_in_order_from_a_to_b()
    {
        var edges = new List<Edge> { Link(A, B, "x"), Link(B, C, "y"), Link(C, D, "z") };
        var path = GraphQueryService.ShortestPath(edges, A, D, maxHops: 4);
        Assert.That(path!.Select(e => e.RelationType), Is.EqualTo(new[] { "x", "y", "z" }));
    }

    [Test]
    public void ShortestPath_is_null_beyond_max_hops_or_when_unreachable()
    {
        var edges = new List<Edge> { Link(A, B, "x"), Link(B, C, "y"), Link(C, D, "z") };
        Assert.That(GraphQueryService.ShortestPath(edges, A, D, maxHops: 2), Is.Null);
        Assert.That(GraphQueryService.ShortestPath(edges, A, E, maxHops: 10), Is.Null);
    }

    [Test]
    public void ShortestPath_to_self_is_empty()
    {
        Assert.That(GraphQueryService.ShortestPath([], A, A, 3), Is.Empty);
    }
}
