using NUnit.Framework;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Pure unit tests for <see cref="EntityMentionScanner"/>'s span-aware matching and tag insertion
/// (corpus-trust-recovery Phase 1a). <c>BuildCandidateIndexAsync</c> needs a real DbContext and is
/// exercised separately; these tests cover the deterministic <c>Scan</c>/<c>ApplyTags</c> logic
/// directly against hand-built candidate lists — no DB required.
/// </summary>
[TestFixture]
public class EntityMentionScannerTests
{
    [Test]
    public void Scan_MultiWordName_MatchesCaseInsensitive()
    {
        var id = Guid.NewGuid();
        var candidates = new List<EntityMentionScanner.MentionCandidate>
        {
            new("Declan Doyle", id, "Declan Doyle", "character", RequiresStrictCase: false),
        };

        var matches = EntityMentionScanner.Scan("declan doyle went back down into the seam.", candidates);

        Assert.That(matches, Has.Count.EqualTo(1));
        Assert.That(matches[0].EntityId, Is.EqualTo(id));
        Assert.That(matches[0].Start, Is.EqualTo(0));
        Assert.That(matches[0].Length, Is.EqualTo(12));
    }

    [Test]
    public void Scan_SingleTokenName_RequiresExactCase_DoesNotMatchLowercaseCommonWord()
    {
        var id = Guid.NewGuid();
        var candidates = new List<EntityMentionScanner.MentionCandidate>
        {
            new("Silence", id, "Silence", "weapon", RequiresStrictCase: true),
        };

        // The exact "Silence" bug this whole feature is meant to fix: a named weapon sharing
        // spelling with an ordinary English word must not false-positive.
        var matches = EntityMentionScanner.Scan("he stood in silence for a long moment.", candidates);

        Assert.That(matches, Is.Empty);
    }

    [Test]
    public void Scan_SingleTokenName_MatchesWhenCapitalizedCorrectly()
    {
        var id = Guid.NewGuid();
        var candidates = new List<EntityMentionScanner.MentionCandidate>
        {
            new("Silence", id, "Silence", "weapon", RequiresStrictCase: true),
        };

        var matches = EntityMentionScanner.Scan("she drew Silence from its sheath.", candidates);

        Assert.That(matches, Has.Count.EqualTo(1));
        Assert.That(matches[0].EntityId, Is.EqualTo(id));
    }

    [Test]
    public void Scan_OverlappingNames_LongestMatchWins_ShorterAliasElsewhereStillTagged()
    {
        var doyleId = Guid.NewGuid();
        var candidates = new List<EntityMentionScanner.MentionCandidate>
        {
            new("Declan Doyle", doyleId, "Declan Doyle", "character", RequiresStrictCase: false),
            new("Doyle", doyleId, "Declan Doyle", "character", RequiresStrictCase: true),
        };

        var text = "Declan Doyle went back down. Doyle never looked up again.";
        var matches = EntityMentionScanner.Scan(text, candidates);

        Assert.That(matches, Has.Count.EqualTo(2), "the full name AND the separate later bare mention must both be tagged");
        Assert.That(matches[0].Length, Is.EqualTo(12), "the full name must win over the bare surname at the same position");
        Assert.That(matches[1].Length, Is.EqualTo(5), "the separate, non-overlapping later mention of the bare name is still tagged");
    }

    [Test]
    public void Scan_WordBoundary_DoesNotMatchInsideALongerWord()
    {
        var id = Guid.NewGuid();
        var candidates = new List<EntityMentionScanner.MentionCandidate>
        {
            new("Rowe", id, "Rowe", "character", RequiresStrictCase: true),
        };

        // "borrowed" contains the literal substring "rowe" -- must not match as a word.
        var matches = EntityMentionScanner.Scan("he had perhaps four seconds of borrowed motion left.", candidates);

        Assert.That(matches, Is.Empty);
    }

    [Test]
    public void ApplyTags_WrapsMatchedSpans_PreservingSurroundingText()
    {
        var id = Guid.NewGuid();
        var text = "Declan Doyle went back down into the seam.";
        var matches = new List<EntityMentionScanner.MentionMatch> { new(0, 12, id, "Declan Doyle", "character") };

        var tagged = EntityMentionScanner.ApplyTags(text, matches);

        Assert.That(tagged, Is.EqualTo($"""<entity repo="character" guid="{id}">Declan Doyle</entity> went back down into the seam."""));
    }

    [Test]
    public void ApplyTags_MultipleMatches_InsertsRightToLeft_KeepingEarlierOffsetsValid()
    {
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        var text = "Declan Doyle spoke to Lyra by the gate.";
        var matches = new List<EntityMentionScanner.MentionMatch>
        {
            new(0, 12, idA, "Declan Doyle", "character"),
            new(22, 4, idB, "Lyra", "character"),
        };

        var tagged = EntityMentionScanner.ApplyTags(text, matches);

        Assert.That(tagged, Is.EqualTo(
            $"""<entity repo="character" guid="{idA}">Declan Doyle</entity> spoke to <entity repo="character" guid="{idB}">Lyra</entity> by the gate."""));
    }

    [Test]
    public void ScanThenApplyTags_ThenStrip_RoundTripsToOriginalText()
    {
        var id = Guid.NewGuid();
        var candidates = new List<EntityMentionScanner.MentionCandidate>
        {
            new("Declan Doyle", id, "Declan Doyle", "character", RequiresStrictCase: false),
        };
        var original = "Declan Doyle went back down into the seam.";

        var matches = EntityMentionScanner.Scan(original, candidates);
        var tagged = EntityMentionScanner.ApplyTags(original, matches);
        var stripped = BeatMarkup.StripEntityTags(tagged);

        Assert.That(stripped, Is.EqualTo(original));
    }

    [Test]
    public void Scan_BareGivenNameAndSurname_BothTagWhenRegisteredAsCandidates()
    {
        // Mirrors EntityMentionScanner.BuildCandidateIndexAsync's derived-token behavior: a
        // multi-word character name also registers its first and last token as their own
        // candidates, so "Declan" and "Doyle" alone still tag correctly, not just "Declan Doyle".
        var id = Guid.NewGuid();
        var candidates = new List<EntityMentionScanner.MentionCandidate>
        {
            new("Declan Doyle", id, "Declan Doyle", "character", RequiresStrictCase: false),
            new("Declan", id, "Declan Doyle", "character", RequiresStrictCase: true),
            new("Doyle", id, "Declan Doyle", "character", RequiresStrictCase: true),
        };

        var text = "Declan Doyle stood in the churned ground. Declan said his own name. Doyle's hand shook.";
        var matches = EntityMentionScanner.Scan(text, candidates);

        Assert.That(matches, Has.Count.EqualTo(3));
        Assert.That(matches.All(m => m.EntityId == id), Is.True);
        // "Doyle's" -- the tag must wrap only "Doyle", leaving the possessive "'s" outside it.
        var last = matches[^1];
        Assert.That(text.Substring(last.Start, last.Length), Is.EqualTo("Doyle"));
    }

    [Test]
    public void Scan_HyphenatedCodeNameAlias_TagsAsWholeToken()
    {
        // "M-101" is a curated CharacterAlias (a Myrmidon designation), not derivable from the
        // character's Name by splitting -- exercises that a hyphenated single-token alias still
        // gets clean word-boundary matching around the hyphen.
        var id = Guid.NewGuid();
        var candidates = new List<EntityMentionScanner.MentionCandidate>
        {
            new("M-101", id, "Declan Doyle", "character", RequiresStrictCase: true),
        };

        var matches = EntityMentionScanner.Scan("\"Designation: M-101. Catalog entry: M-1018883.\"", candidates);

        Assert.That(matches, Has.Count.EqualTo(1), "must match the standalone designation but not the longer catalog number");
        Assert.That(matches[0].EntityId, Is.EqualTo(id));
    }
}

/// <summary>
/// Pure unit tests for <see cref="EntityMentionScanner.FindUnresolvedProperNouns"/> — the Bible→
/// Outline refactor Phase 4b residue check that files an <c>[outline-entity]</c> EntityDrift
/// finding for a capitalized name Scan() left untagged. No DB required: both the text and the
/// completed Scan() match list are hand-built.
/// </summary>
[TestFixture]
public class EntityMentionScannerFindUnresolvedProperNounsTests
{
    [Test]
    public void NameNotInCandidates_IsReported()
    {
        var result = EntityMentionScanner.FindUnresolvedProperNouns(
            "Marisol Vega walks into the room.", matches: []);

        Assert.That(result, Is.EquivalentTo(new[] { "Marisol Vega" }));
    }

    [Test]
    public void NameCoveredByAMatch_IsNotReported()
    {
        var id = Guid.NewGuid();
        var text = "Declan Doyle walks into the room.";
        var candidates = new List<EntityMentionScanner.MentionCandidate>
        {
            new("Declan Doyle", id, "Declan Doyle", "character", RequiresStrictCase: false),
        };
        var matches = EntityMentionScanner.Scan(text, candidates);

        var result = EntityMentionScanner.FindUnresolvedProperNouns(text, matches);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void MixOfTaggedAndUntagged_OnlyUntaggedReported()
    {
        var id = Guid.NewGuid();
        var text = "Declan Doyle meets Marisol Vega at the dock.";
        var candidates = new List<EntityMentionScanner.MentionCandidate>
        {
            new("Declan Doyle", id, "Declan Doyle", "character", RequiresStrictCase: false),
        };
        var matches = EntityMentionScanner.Scan(text, candidates);

        var result = EntityMentionScanner.FindUnresolvedProperNouns(text, matches);

        Assert.That(result, Is.EquivalentTo(new[] { "Marisol Vega" }));
    }

    [Test]
    public void CommonSentenceOpeners_NeverReported()
    {
        var result = EntityMentionScanner.FindUnresolvedProperNouns(
            "The dock was quiet. After the storm, She waited.", matches: []);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void DuplicateName_ReportedOnce()
    {
        var result = EntityMentionScanner.FindUnresolvedProperNouns(
            "Marisol Vega walked. Marisol Vega waited. Marisol Vega left.", matches: []);

        Assert.That(result, Is.EquivalentTo(new[] { "Marisol Vega" }));
    }

    [Test]
    public void ShortCapitalizedWord_NotFlagged()
    {
        // Length filter (>3 chars) exists so a short incidental capitalized word doesn't spam
        // findings — matches the same threshold ProseWriterRouter's beat-goal precheck uses.
        // "Sam" is 3 characters and does match the proper-noun regex shape (unlike "Ok"/"Go",
        // which are too short even to match the pattern at all), so it actually exercises the
        // length filter rather than the regex's own minimum-match-length falling short first.
        var result = EntityMentionScanner.FindUnresolvedProperNouns("Sam left early.", matches: []);

        Assert.That(result, Is.Empty);
    }
}
