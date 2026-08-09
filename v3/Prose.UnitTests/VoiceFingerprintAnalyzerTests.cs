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

    // Real-corpus regression (2026-08-09): per-beat testing against Death Whispers in a Cat's Ear
    // produced 220 "drift" findings before this margin existed, 76% with a score gap <=0.02 and 22
    // exact ties — every character shares most of a book's common vocabulary, so tiny, meaningless
    // score differences constantly flip which fingerprint numerically "wins." These two tests pin
    // down that a real margin is required, not just "numerically ahead."

    [Test]
    public void CheckDrift_ScoreGapBelowMargin_NotDrifted()
    {
        var own = Guid.NewGuid();
        var other = Guid.NewGuid();
        var shared = WordSet("common", 30); // both characters' prose shares this common vocabulary
        var fingerprints = new Dictionary<Guid, (string, HashSet<string>)>
        {
            // own: 30 shared + 6 unique = 36 tokens -> score 30/36 = 0.833
            [own]   = ("Kessa", shared.Concat(WordSet("kessaU", 6)).ToHashSet(StringComparer.OrdinalIgnoreCase)),
            // other: 30 shared + 5 unique = 35 tokens -> score 30/35 = 0.857 (gap ~0.024, below the 0.03 margin)
            [other] = ("Dren",  shared.Concat(WordSet("drenU", 5)).ToHashSet(StringComparer.OrdinalIgnoreCase)),
        };
        var result = VoiceFingerprintAnalyzer.CheckDrift(shared, own, fingerprints);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.TopMatchScore, Is.GreaterThan(result.Value.OwnScore)); // numerically ahead...
        Assert.That(result.Value.Drifted, Is.False);                                     // ...but not by enough to count.
    }

    [Test]
    public void CheckDrift_ExactTieBetweenOwnAndOther_NotDrifted()
    {
        var own = Guid.NewGuid();
        var other = Guid.NewGuid();
        var shared = WordSet("common", 30);
        // Identically-sized fingerprints (30 shared + 5 unique each) score exactly the same
        // against the shared-only test passage. `other` is inserted FIRST deliberately: LINQ's
        // OrderByDescending is a stable sort, so on an exact tie the first-enumerated entry sorts
        // first — without the margin check, that would make "Dren" the reported top match purely
        // by dictionary insertion order, not any actual vocabulary difference (confirmed by
        // reverting the margin: this ordering is what actually exposes the bug; own-first does not).
        var fingerprints = new Dictionary<Guid, (string, HashSet<string>)>
        {
            [other] = ("Dren",  shared.Concat(WordSet("drenU", 5)).ToHashSet(StringComparer.OrdinalIgnoreCase)),
            [own]   = ("Kessa", shared.Concat(WordSet("kessaU", 5)).ToHashSet(StringComparer.OrdinalIgnoreCase)),
        };
        var result = VoiceFingerprintAnalyzer.CheckDrift(shared, own, fingerprints);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.TopMatchScore, Is.EqualTo(result.Value.OwnScore).Within(1e-9));
        Assert.That(result.Value.Drifted, Is.False);
    }
}
