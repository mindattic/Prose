using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the nondeterminism fix (2026-08-09): every entity pushed during a
/// single BeginBeat shares the same LastMentionedBeat (the counter advances once per beat, not
/// per push), so a set of entities tying on LastMentionedBeat and Depth used to fall through to
/// <see cref="System.Collections.Concurrent.ConcurrentDictionary"/> bucket enumeration order —
/// nondeterministic between runs of the identical beat. Mirrors
/// <see cref="DocContextStackDeterminismTests"/>, which fixed the identical class of bug in the
/// sibling doc stack.
/// </summary>
[TestFixture]
public class EntityContextStackTests
{
    static void PushAll(EntityContextStack stack, Guid nodeId, IEnumerable<(string Name, int Depth)> entries)
    {
        foreach (var (name, depth) in entries)
            stack.Push(nodeId, Guid.NewGuid(), name, "character", "", score: 50, depth: depth);
    }

    [Test]
    public void GetActive_TiedEntries_OrderIsStableRegardlessOfPushOrder()
    {
        (string, int)[] names = [("Zeta", 0), ("Alpha", 0), ("Mid", 0), ("Beta", 0)];

        var forwardStack = new EntityContextStack();
        var forwardNode = Guid.NewGuid();
        forwardStack.BeginBeat(forwardNode);
        PushAll(forwardStack, forwardNode, names);
        var forward = forwardStack.GetActive(forwardNode).Select(e => e.Name).ToList();

        var reverseStack = new EntityContextStack();
        var reverseNode = Guid.NewGuid();
        reverseStack.BeginBeat(reverseNode);
        PushAll(reverseStack, reverseNode, names.Reverse().ToArray());
        var reverse = reverseStack.GetActive(reverseNode).Select(e => e.Name).ToList();

        Assert.That(forward, Is.EqualTo(reverse),
            "identical entity sets must emit in the same order no matter what order they were pushed");
        Assert.That(forward, Is.EqualTo(names.Select(n => n.Item1).OrderBy(n => n, StringComparer.Ordinal).ToList()),
            "the tiebreak is Name ordinal, so a fully-tied set sorts by name");
    }

    [Test]
    public void GetActive_RepeatedRuns_ProduceIdenticalOrder()
    {
        var names = Enumerable.Range(0, 12).Select(i => $"Character{i:D2}").ToList();

        List<string> RunOnce()
        {
            var stack = new EntityContextStack();
            var nodeId = Guid.NewGuid();
            stack.BeginBeat(nodeId);
            PushAll(stack, nodeId, names.Select(n => (n, 0)));
            return stack.GetActive(nodeId).Select(e => e.Name).ToList();
        }

        var first = RunOnce();
        for (var run = 0; run < 5; run++)
            Assert.That(RunOnce(), Is.EqualTo(first), $"run {run} diverged — ordering is not reproducible");
    }

    [Test]
    public void EvictLru_TiedOnLastMentioned_EvictsSameEntryEveryTime()
    {
        // All entries pushed in the same beat tie on LastMentionedBeat; without the Name
        // tiebreak, which one EvictLru drops when over capacity depends on dictionary
        // enumeration order and can vary between runs. Guids are freshly generated per run,
        // so identity for comparison purposes is the Name, not the EntityId.
        List<string> RunOnceByName()
        {
            var stack = new EntityContextStack();
            var nodeId = Guid.NewGuid();
            stack.BeginBeat(nodeId);
            for (var i = 0; i < EntityContextStack.StackCapacity + 3; i++)
                stack.Push(nodeId, Guid.NewGuid(), $"Character{i:D2}", "character", "", score: 50, depth: 1);
            return stack.GetActive(nodeId).Select(e => e.Name).OrderBy(n => n, StringComparer.Ordinal).ToList();
        }

        var first = RunOnceByName();
        for (var run = 0; run < 5; run++)
            Assert.That(RunOnceByName(), Is.EqualTo(first), $"run {run}: eviction victim set diverged");
    }
}
