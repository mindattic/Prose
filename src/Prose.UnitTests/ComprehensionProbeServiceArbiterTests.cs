using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-08-09 fix to ComprehensionProbeService.ArbitrateAsync (extracted
/// as the static ParseDefects for direct testing). A malformed/unparseable arbiter response used
/// to be swallowed into an empty defects list — indistinguishable from "the arbiter confirmed this
/// chapter clean." Because ProbeChapterAsync unconditionally persisted whatever ArbitrateAsync
/// returned into the per-chapter cache, this was the most severe instance of this session's
/// most-repeated bug class: a single transient hiccup got PERMANENTLY cached as clean, never
/// re-evaluated until the chapter's own text changed. Fixed by throwing here and catching one
/// frame up (in ProbeChapterAsync) to skip the cache write entirely instead.
/// </summary>
[TestFixture]
public class ComprehensionProbeServiceArbiterTests
{
    [Test]
    public void ParseDefects_ValidJson_MapsAllFields()
    {
        var raw = """{"defects":[{"kind":"missed-fact","description":"reader never learns the letter was forged","evidence":"the letter, unsigned","severity":"blocker","readerPlausible":true}]}""";
        var result = ComprehensionProbeService.ParseDefects(raw);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Kind, Is.EqualTo("missed-fact"));
        Assert.That(result[0].Severity, Is.EqualTo("blocker"));
        Assert.That(result[0].ReaderPlausible, Is.True);
    }

    [Test]
    public void ParseDefects_MarkdownFencedJson_StripsFence()
    {
        var raw = "```json\n{\"defects\":[]}\n```";
        Assert.That(ComprehensionProbeService.ParseDefects(raw), Is.Empty);
    }

    [Test]
    public void ParseDefects_EmptyDefectsArray_ReturnsEmptyWithoutThrowing()
    {
        // A genuinely well-formed empty array (the arbiter correctly found nothing) is real
        // signal, not a parse failure — must NOT throw.
        Assert.That(ComprehensionProbeService.ParseDefects("""{"defects":[]}"""), Is.Empty);
    }

    [Test]
    public void ParseDefects_MissingDefectsKey_ReturnsEmptyWithoutThrowing()
    {
        // Valid JSON, just no "defects" array present — treated as zero defects (matches the
        // pre-existing TryGetProperty guard), not a parse failure.
        Assert.That(ComprehensionProbeService.ParseDefects("""{}"""), Is.Empty);
    }

    [Test]
    public void ParseDefects_MalformedJson_ThrowsRatherThanReturningEmpty()
    {
        Assert.That(() => ComprehensionProbeService.ParseDefects("{\"defects\": oops}"),
            Throws.Exception);
    }

    [Test]
    public void ParseDefects_NonJsonText_ThrowsRatherThanReturningEmpty()
    {
        Assert.That(() => ComprehensionProbeService.ParseDefects("I cannot arbitrate this."),
            Throws.Exception);
    }
}
