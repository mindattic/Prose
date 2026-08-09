using Prose.Core.Services;

namespace Prose.UnitTests;

[TestFixture]
public class VoiceFingerprintAnalyzerTests
{
    private static HashSet<string> WordSet(string prefix, int count) =>
        Enumerable.Range(1, count).Select(i => $"{prefix}{i:D2}").ToHashSet(StringComparer.OrdinalIgnoreCase);

    [Test]
    public void DistinctiveTokens_FiltersShortWordsAndStopwords_LowercasesTheRest()
    {
        var tokens = VoiceFingerprintAnalyzer.DistinctiveTokens("The GRANITE wall would not move, and Kessa knew it.");
        // "the"/"would"/"not"/"and"/"knew"/"it" are stopwords or <4 chars — dropped.
        Assert.That(tokens, Does.Contain("granite"));
        Assert.That(tokens, Does.Contain("wall"));
        Assert.That(tokens, Does.Contain("kessa"));
        Assert.That(tokens, Does.Not.Contain("would"));
        Assert.That(tokens, Does.Not.Contain("knew"));
        Assert.That(tokens, Does.Not.Contain("the"));
    }

    [Test]
    public void CheckDrift_TestPassageTooShort_ReturnsNull()
    {
        var fingerprints = new Dictionary<Guid, (string, HashSet<string>)>
        {
            [Guid.NewGuid()] = ("Kessa", WordSet("a", 25)),
        };
        var result = VoiceFingerprintAnalyzer.CheckDrift(WordSet("x", 5), fingerprints.Keys.First(), fingerprints);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void CheckDrift_OwnFingerprintMissing_ReturnsNull()
    {
        var fingerprints = new Dictionary<Guid, (string, HashSet<string>)>
        {
            [Guid.NewGuid()] = ("Kessa", WordSet("a", 25)),
        };
        var result = VoiceFingerprintAnalyzer.CheckDrift(WordSet("a", 35), Guid.NewGuid(), fingerprints);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void CheckDrift_OwnFingerprintTooThin_ReturnsNull()
    {
        var own = Guid.NewGuid();
        var fingerprints = new Dictionary<Guid, (string, HashSet<string>)>
        {
            [own] = ("Kessa", WordSet("a", 10)), // below the 20-token minimum
        };
        var result = VoiceFingerprintAnalyzer.CheckDrift(WordSet("a", 35), own, fingerprints);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void CheckDrift_PassageMatchesOwnVoice_NotDrifted()
    {
        var own = Guid.NewGuid();
        var other = Guid.NewGuid();
        var fingerprints = new Dictionary<Guid, (string, HashSet<string>)>
        {
            [own]   = ("Kessa", WordSet("kessa", 40)),
            [other] = ("Dren",  WordSet("dren", 40)),
        };
        // Test passage overlaps heavily with Kessa's own vocabulary, not Dren's.
        var result = VoiceFingerprintAnalyzer.CheckDrift(WordSet("kessa", 35), own, fingerprints);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Drifted, Is.False);
        Assert.That(result.Value.TopMatchName, Is.EqualTo("Kessa"));
    }

    [Test]
    public void CheckDrift_PassageMatchesOtherVoiceMoreThanOwn_FlagsDrift()
    {
        var own = Guid.NewGuid();
        var other = Guid.NewGuid();
        var fingerprints = new Dictionary<Guid, (string, HashSet<string>)>
        {
            [own]   = ("Kessa", WordSet("kessa", 40)),
            [other] = ("Dren",  WordSet("dren", 40)),
        };
        // A beat attributed to Kessa (own) whose vocabulary is actually Dren's.
        var result = VoiceFingerprintAnalyzer.CheckDrift(WordSet("dren", 35), own, fingerprints);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Drifted, Is.True);
        Assert.That(result.Value.TopMatchName, Is.EqualTo("Dren"));
        Assert.That(result.Value.TopMatchScore, Is.GreaterThan(result.Value.OwnScore));
    }
}
