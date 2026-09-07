using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// <see cref="LlmActionContext.BeginStage"/> — the ambient stage tag that lets every LLM and
/// embedding call made during one beat write be attributed to the pipeline stage that made it
/// (single-source-writer RFC, step one, 2026-09-07). Pins: the tag is visible inside the scope,
/// nested scopes compose as <c>outer/inner</c>, disposal restores the enclosing value (so a throw
/// or early return cannot leak a stage onto unrelated work), and two async flows do not see each
/// other's stage.
/// </summary>
[TestFixture]
public class LlmActionContextStageTests
{
    [Test]
    public void BeginStage_SetsCurrentStage_AndRestoresOnDispose()
    {
        Assert.That(LlmActionContext.CurrentStage, Is.Null);
        using (LlmActionContext.BeginStage("SceneCollision"))
        {
            Assert.That(LlmActionContext.CurrentStage, Is.EqualTo("SceneCollision"));
        }
        Assert.That(LlmActionContext.CurrentStage, Is.Null);
    }

    [Test]
    public void BeginStage_NestedScopes_ComposeWithSlash_AndUnwindInOrder()
    {
        using (LlmActionContext.BeginStage("Draft"))
        {
            using (LlmActionContext.BeginStage("StyleAnchors"))
            {
                Assert.That(LlmActionContext.CurrentStage, Is.EqualTo("Draft/StyleAnchors"));
            }
            Assert.That(LlmActionContext.CurrentStage, Is.EqualTo("Draft"));
        }
        Assert.That(LlmActionContext.CurrentStage, Is.Null);
    }

    [Test]
    public void BeginStage_RestoresEnclosingStage_WhenBodyThrows()
    {
        using (LlmActionContext.BeginStage("outer"))
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                using var inner = LlmActionContext.BeginStage("inner");
                throw new InvalidOperationException("boom");
            });
            Assert.That(LlmActionContext.CurrentStage, Is.EqualTo("outer"));
        }
        Assert.That(LlmActionContext.CurrentStage, Is.Null);
    }

    [Test]
    public void BeginStage_DoubleDispose_IsIdempotent()
    {
        var scope = LlmActionContext.BeginStage("x");
        scope.Dispose();
        scope.Dispose();
        Assert.That(LlmActionContext.CurrentStage, Is.Null);
    }

    [Test]
    public async Task BeginStage_DoesNotLeakAcrossConcurrentAsyncFlows()
    {
        var gate = new TaskCompletionSource();

        var a = Task.Run(async () =>
        {
            using var s = LlmActionContext.BeginStage("A");
            await gate.Task;
            return LlmActionContext.CurrentStage;
        });
        var b = Task.Run(async () =>
        {
            using var s = LlmActionContext.BeginStage("B");
            await gate.Task;
            return LlmActionContext.CurrentStage;
        });

        gate.SetResult();
        var results = await Task.WhenAll(a, b);
        Assert.That(results[0], Is.EqualTo("A"));
        Assert.That(results[1], Is.EqualTo("B"));
        Assert.That(LlmActionContext.CurrentStage, Is.Null);
    }

    [Test]
    public async Task CurrentBeatId_FlowsIntoTaskRun_StartedInsideTheScope()
    {
        // The router relies on this: the post-write cluster is a fire-and-forget Task.Run and
        // must still see the beat id (and stage) set by WriteAsync around the whole write.
        var beat = Guid.NewGuid();
        var previous = LlmActionContext.CurrentBeatId;
        LlmActionContext.CurrentBeatId = beat;
        try
        {
            Guid? seen;
            using (LlmActionContext.BeginStage("post"))
            {
                seen = await Task.Run(() => LlmActionContext.CurrentBeatId);
                var stage = await Task.Run(() => LlmActionContext.CurrentStage);
                Assert.That(stage, Is.EqualTo("post"));
            }
            Assert.That(seen, Is.EqualTo(beat));
        }
        finally
        {
            LlmActionContext.CurrentBeatId = previous;
        }
    }
}
