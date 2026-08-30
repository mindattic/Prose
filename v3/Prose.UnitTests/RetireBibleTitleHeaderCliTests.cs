using Prose.Cli;

namespace Prose.UnitTests;

/// <summary>
/// Pure unit tests for RetireBibleTitleHeaderCli's rewrite logic — the stale
/// "# NODE BIBLE: [Title]" header baked into pre-fix generated outlines by
/// NodeOutlineService.BuildBibleSystemPrompt's old prompt template.
/// </summary>
[TestFixture]
public class RetireBibleTitleHeaderCliTests
{
    [Test]
    public void StaleHeader_Rewritten_ToBookContext()
    {
        var text = "# NODE BIBLE: Bushido Coda\n\n## LOGLINE\nA man gets a job.";

        var found = RetireBibleTitleHeaderCli.TryRewrite(text, out var rewritten, out var title);

        Assert.That(found, Is.True);
        Assert.That(title, Is.EqualTo("Bushido Coda"));
        Assert.That(rewritten, Is.EqualTo("# Book Context: Bushido Coda\n\n## LOGLINE\nA man gets a job."));
    }

    [Test]
    public void LowercaseStaleHeader_StillRewritten()
    {
        var text = "# node bible: Attendance\n\n## LOGLINE\nText.";

        var found = RetireBibleTitleHeaderCli.TryRewrite(text, out var rewritten, out var title);

        Assert.That(found, Is.True);
        Assert.That(title, Is.EqualTo("Attendance"));
        Assert.That(rewritten, Is.EqualTo("# Book Context: Attendance\n\n## LOGLINE\nText."));
    }

    [Test]
    public void AlreadyModernHeader_NotMatched()
    {
        var text = "# Book Context: Attendance (ATTE)\n\n## LOGLINE\nText.";

        var found = RetireBibleTitleHeaderCli.TryRewrite(text, out var rewritten, out _);

        Assert.That(found, Is.False);
        Assert.That(rewritten, Is.EqualTo(text));
    }

    [Test]
    public void HeaderNotOnFirstLine_NotMatched()
    {
        // The header must be the very first line — GenerateAndSaveAsync always persists
        // bibleText.Trim() with no leading frontmatter or blank lines.
        var text = "Some other content.\n# NODE BIBLE: Attendance\n";

        var found = RetireBibleTitleHeaderCli.TryRewrite(text, out var rewritten, out _);

        Assert.That(found, Is.False);
        Assert.That(rewritten, Is.EqualTo(text));
    }

    [Test]
    public void EmptyWorkingTitle_RewrittenWithEmptyTitle()
    {
        var text = "# NODE BIBLE: \n\n## LOGLINE\nText.";

        var found = RetireBibleTitleHeaderCli.TryRewrite(text, out var rewritten, out var title);

        Assert.That(found, Is.True);
        Assert.That(title, Is.EqualTo(""));
        Assert.That(rewritten, Is.EqualTo("# Book Context: \n\n## LOGLINE\nText."));
    }

    [Test]
    public void NoStaleHeader_ReturnsUnchanged()
    {
        var text = "## LOGLINE\nNo header at all.";

        var found = RetireBibleTitleHeaderCli.TryRewrite(text, out var rewritten, out _);

        Assert.That(found, Is.False);
        Assert.That(rewritten, Is.EqualTo(text));
    }
}
