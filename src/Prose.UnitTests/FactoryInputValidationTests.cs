using Prose.Core.Data.Entities;
using Prose.Core.Services.Factory;

namespace Prose.UnitTests;

/// <summary>DB-free input checks on the factory's write doors: declared order paths, check JSON,
/// and ruling drafts validated before anything is recorded.</summary>
[TestFixture]
public class FactoryInputValidationTests
{
    [TestCase("src/Foo/**", "src/Foo/**")]
    [TestCase("./src/Foo/**", "src/Foo/**")]
    [TestCase("/src/Foo/**", "src/Foo/**")]
    [TestCase(@"src\Foo\Bar.cs", "src/Foo/Bar.cs")]
    [TestCase("  docs/x.md ", "docs/x.md")]
    public void NormalizePath_MakesDeclaredPathsRepoRelative(string declared, string expected)
    {
        var normalized = WorkOrderService.NormalizePath(declared);
        Assert.That(normalized, Is.EqualTo(expected));
        Assert.That(PathGlob.MatchesAny(expected.Replace("**", "a/b.cs"), [normalized]), Is.True);
    }

    [Test]
    public void PathGlob_DoesNotPrefixMatchASiblingFolder()
    {
        Assert.That(PathGlob.MatchesAny("src/FooBar/x.cs", ["src/Foo/**"]), Is.False);
        Assert.That(PathGlob.MatchesAny("SRC/foo/x.cs", ["src/Foo/**"]), Is.True, "case-insensitive, like the Stop hook's -match");
    }

    [TestCase("not json")]
    [TestCase("[{\"type\": 3}]")]
    [TestCase("{\"type\":\"commit\"}")]
    public void ParseChecks_MalformedInputIsAnArgumentException(string json) =>
        Assert.Throws<ArgumentException>(() => WorkOrderService.ParseChecks(json));

    [Test]
    public void RulingValidate_RejectsBadDraftsWithoutADatabase()
    {
        Assert.Throws<ArgumentException>(() => RulingService.Validate(new RulingDraft(RulingKinds.Metric, "tic", Pattern: "x")));
        Assert.Throws<ArgumentException>(() => RulingService.Validate(new RulingDraft(RulingKinds.Law, "bad", Pattern: "(")));
        Assert.Throws<ArgumentException>(() => RulingService.Validate(new RulingDraft("", "blank kind")));
        Assert.DoesNotThrow(() => RulingService.Validate(new RulingDraft(RulingKinds.Metric, "tic", Pattern: "x", MaxPer1kWords: 1m)));
    }
}
