using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// The scoped beat-similarity lookup must restrict to a node <i>and its subtree</i>, never to the
/// node alone.
///
/// <para>The bug these pin, found 2026-09-21: the filter was <c>sb.NodeId = @p_node</c>, but
/// <c>BeatNodes.NodeId</c> is the node a beat actually hangs off — a chapter — while both scoped
/// callers (<c>VoiceAnchorService</c>, <c>SemanticFidelityService</c>) pass the <b>book</b>. A book
/// owns no beats directly, so the query matched zero rows for every book-scoped caller, always.
/// Neither caller treats an empty result as an error, so the voice tier shipped prompts with no
/// exemplars and the bible-alignment map came back empty — silently, for every beat ever written
/// through that path. The write side (<c>ReembedBeatNodesAsync</c>) walked descendants all along;
/// only the read side did not.</para>
///
/// <para>These assert the SQL's shape rather than its results because the query is raw T-SQL over
/// <c>VECTOR_DISTANCE</c>, which no in-memory provider can execute. The shape is the invariant that
/// broke: a flat equality compiles, runs, and returns nothing.</para>
/// </summary>
[TestFixture]
public class BeatNodeSimilarityScopeTests
{
    [Test]
    public void ScopedQuery_WalksTheSubtree_NotJustTheNodeItself()
    {
        var sql = EmbeddingService.BuildBeatNodeSimilaritySql(scoped: true);

        Assert.That(sql, Does.Contain("WITH Subtree AS"),
            "the scope must be resolved through a recursive walk, not a single node id");
        Assert.That(sql, Does.Contain("JOIN Subtree s ON c.ParentNodeId = s.Id"),
            "the recursive member must descend by parent link");
        Assert.That(sql, Does.Contain("sb.NodeId IN (SELECT Id FROM Subtree)"));
    }

    [Test]
    public void ScopedQuery_DoesNotFilterOnAFlatNodeEquality()
    {
        // The exact regression. A book node is never present in BeatNodes.NodeId.
        var sql = EmbeddingService.BuildBeatNodeSimilaritySql(scoped: true);

        Assert.That(sql, Does.Not.Contain("sb.NodeId = @p_node"));
    }

    [Test]
    public void ScopedQuery_AnchorsOnTheNodeItself_SoLeafScopesStillMatch()
    {
        // A chapter-scoped caller must keep working: the CTE's anchor member is the node itself,
        // so a leaf resolves to a one-row subtree rather than to nothing.
        var sql = EmbeddingService.BuildBeatNodeSimilaritySql(scoped: true);

        Assert.That(sql, Does.Contain("SELECT Id FROM dbo.Nodes WHERE Id = @p_node"));
    }

    [Test]
    public void ScopedQuery_BoundsRecursion_SoAParentCycleFailsFastInsteadOfSpinning()
    {
        var sql = EmbeddingService.BuildBeatNodeSimilaritySql(scoped: true);

        Assert.That(sql, Does.Contain("OPTION (MAXRECURSION 64)"));
        // The hint is only legal alongside a recursive CTE, so it must not leak into the
        // unscoped form.
        Assert.That(EmbeddingService.BuildBeatNodeSimilaritySql(scoped: false),
            Does.Not.Contain("MAXRECURSION"));
    }

    [Test]
    public void UnscopedQuery_CarriesNoSubtreeMachineryAtAll()
    {
        var sql = EmbeddingService.BuildBeatNodeSimilaritySql(scoped: false);

        Assert.That(sql, Does.Not.Contain("Subtree"));
        Assert.That(sql, Does.Not.Contain("@p_node"),
            "an unscoped call binds no @p_node parameter — referencing it would throw");
        Assert.That(sql, Does.StartWith("SELECT TOP (@p_k)"));
    }

    [Test]
    public void BothForms_KeepTheUniverseFilterOnTheNode_NotOnTheEmbeddingTag()
    {
        // SS-A46: pe.UniverseId is a drift-prone copy that defaults to GLMZ. Scoping must not
        // have quietly reintroduced it.
        foreach (var scoped in new[] { true, false })
        {
            var sql = EmbeddingService.BuildBeatNodeSimilaritySql(scoped);
            Assert.That(sql, Does.Contain("n.UniverseId = @p_universe"), $"scoped: {scoped}");
            // Not a bare "pe.UniverseId" — that name appears in the comment explaining why the
            // embedding's own tag is the wrong thing to filter on.
            Assert.That(sql, Does.Not.Contain("pe.UniverseId ="), $"scoped: {scoped}");
        }
    }
}
