using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

/// <summary>
/// Regression cover for the nondeterminism in <see cref="DocContextStack"/>.
///
/// <c>ActionCounter</c> advances once per <c>BeginAction</c>, not per push, so every doc pushed
/// during a single <c>PrepareContextAsync</c> shares the same <c>LastTouchedAction</c>. Before the
/// fix, docs that also tied on tier and score fell through to <see cref="System.Collections.Concurrent.ConcurrentDictionary"/>
/// bucket enumeration order — so the emitted doc order, and the eviction victim at capacity, could
/// differ between two runs of the identical beat. That matters more now that BuildBlockAsync skips
/// oversized docs instead of truncating at the first one: order decides what fits the budget.
/// </summary>
[TestFixture]
public class DocContextStackDeterminismTests
{
    static DocContextStack.StackEntry Entry(string path, string tier, double score = 50) =>
        new(Guid.NewGuid(), path, tier, Scope: "", Triggers: "", Reason: "test",
            Score: score, PushedAtAction: 0, LastTouchedAction: 0);

    /// <summary>Pushes the same doc set in a different order each run and returns the emitted paths.</summary>
    static List<string> OrderAfterPush(IEnumerable<DocContextStack.StackEntry> entries)
    {
        var stack = new DocContextStack();
        var contextId = Guid.NewGuid();
        stack.BeginAction(contextId, "TEST");
        foreach (var e in entries) stack.Push(contextId, e);
        return stack.GetActive(contextId).Select(e => e.RelativePath).ToList();
    }

    [Test]
    public void GetActive_TiedEntries_OrderIsStableRegardlessOfPushOrder()
    {
        // All four tie on tier AND score AND (because one action) LastTouchedAction.
        string[] paths = ["docs/ENGINE.md", "docs/BIBLE.digest.md", "docs/AAA.md", "docs/ZZZ.md"];

        var forward = OrderAfterPush(paths.Select(p => Entry(p, "always", 100)));
        var reverse = OrderAfterPush(paths.Reverse().Select(p => Entry(p, "always", 100)));

        Assert.That(forward, Is.EqualTo(reverse),
            "identical doc sets must emit in the same order no matter what order they were pushed");
        Assert.That(forward, Is.EqualTo(paths.OrderBy(p => p, StringComparer.Ordinal).ToList()),
            "the tiebreak is RelativePath ordinal, so a fully-tied set sorts by path");
    }

    [Test]
    public void GetActive_RepeatedRuns_ProduceIdenticalOrder()
    {
        var paths = Enumerable.Range(0, 12).Select(i => $"docs/topic-{i:D2}.md").ToList();

        var first = OrderAfterPush(paths.Select(p => Entry(p, "always", 100)));
        for (var run = 0; run < 5; run++)
            Assert.That(OrderAfterPush(paths.Select(p => Entry(p, "always", 100))), Is.EqualTo(first),
                $"run {run} diverged — ordering is not reproducible");
    }

    [Test]
    public void GetActive_TierStillDominatesTheTiebreak()
    {
        var order = OrderAfterPush([
            Entry("docs/zzz-topic.md", "topic"),
            Entry("docs/aaa-node.md",  "node"),
            Entry("docs/mmm-always.md", "always"),
            Entry("docs/nnn-series.md", "series"),
        ]);

        Assert.That(order, Is.EqualTo(new[]
        {
            "docs/mmm-always.md", "docs/aaa-node.md", "docs/nnn-series.md", "docs/zzz-topic.md",
        }), "path is only the LAST tiebreak — tier rank still wins");
    }

    [Test]
    public void EvictLruTopic_AtCapacity_DropsTheSameDocEveryRun()
    {
        // TopicCapacity is 8 non-pinned entries; push more so eviction fires. Every entry ties on
        // LastTouchedAction (one action), which is exactly the case that used to be arbitrary.
        var paths = Enumerable.Range(0, DocContextStack.TopicCapacity + 3)
            .Select(i => $"docs/topic-{i:D2}.md").ToList();

        var survivors = new List<List<string>>();
        for (var run = 0; run < 4; run++)
        {
            // Vary push order per run to prove the outcome doesn't depend on it.
            var ordered = run % 2 == 0 ? paths : Enumerable.Reverse(paths).ToList();
            survivors.Add(OrderAfterPush(ordered.Select(p => Entry(p, "topic"))));
        }

        Assert.That(survivors[0], Has.Count.EqualTo(DocContextStack.TopicCapacity),
            "capacity must still be enforced");
        foreach (var s in survivors.Skip(1))
            Assert.That(s, Is.EqualTo(survivors[0]), "the same beat must evict the same docs every time");
    }

    [Test]
    public void AlwaysAndNodeTiers_AreExemptFromCapacity()
    {
        var entries = Enumerable.Range(0, DocContextStack.TopicCapacity + 5)
            .Select(i => Entry($"docs/topic-{i:D2}.md", "topic"))
            .Concat([Entry("docs/ENGINE.md", "always", 100), Entry("docs/nodes/BCODA.md", "node", 90)]);

        var order = OrderAfterPush(entries);

        Assert.That(order, Does.Contain("docs/ENGINE.md"));
        Assert.That(order, Does.Contain("docs/nodes/BCODA.md"));
        Assert.That(order.Count, Is.EqualTo(DocContextStack.TopicCapacity + 2),
            "pinned tiers survive on top of the topic cap, they do not consume it");
    }
}
