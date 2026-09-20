using NUnit.Framework;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Pinned mentions: an entity tag already in the caller's text is the caller's DISAMBIGUATION,
/// and must survive a save even when the scanner cannot re-derive it.
///
/// <para><b>Measured cause, 2026-09-04.</b> Every beat save strips incoming tags and re-derives
/// them from a name scan — deliberately, so a rename cannot leave a stale tag in prose. But
/// <see cref="EntityMentionScanner.BuildCandidateIndexAsync"/> refuses (rightly) to anchor a tag
/// to a surface name claimed by more than one entity, so a tag on an ambiguous name could not be
/// re-derived and was simply destroyed. Editing one clause of BCODA beat #3289 silently deleted
/// four valid <c>&lt;entity guid="01a0030b-…"&gt;Marisol&lt;/entity&gt;</c> tags — the universe
/// holds five Marisols — while Kyle's and Silence's tags round-tripped fine. The guid resolved to
/// a live entity the whole time: the tag was correct, merely not reconstructible from its own
/// name, which is exactly what a human's explicit markup knows and a name scan cannot.</para>
///
/// <para>The asymmetry that shapes these tests: pinning must NOT become a way for a stale tag to
/// outlive its entity, so every test below has a matching negative.</para>
/// </summary>
[TestFixture]
public class BeatMarkupPinnedMentionTests
{
    private static readonly Guid Marisol = Guid.Parse("01a0030b-602e-7a47-a1ec-61747fc9d537");
    private static readonly Guid Kyle = Guid.Parse("019d6143-a648-7876-9688-0f6d38d70075");

    private static Dictionary<Guid, (string Name, string EntityType)> Live(params Guid[] ids)
    {
        var map = new Dictionary<Guid, (string, string)>();
        foreach (var id in ids)
            map[id] = (id == Marisol ? "Marisol" : "Kyle Ellen Corbin", "character");
        return map;
    }

    [Test]
    public void ExtractTaggedMentions_ReadsSurfaceTextAndGuidTogether()
    {
        const string text = """
            <entity repo="character" guid="01a0030b-602e-7a47-a1ec-61747fc9d537">Marisol</entity> said
            nothing, and <entity repo="character" guid="019d6143-a648-7876-9688-0f6d38d70075">Kyle</entity>
            watched <entity repo="character" guid="01a0030b-602e-7a47-a1ec-61747fc9d537">Marisol</entity>.
            """;

        var pinned = BeatMarkup.ExtractTaggedMentions(text);

        // De-duplicated on (text, guid): two Marisol tags, one entry.
        Assert.That(pinned, Has.Count.EqualTo(2));
        Assert.That(pinned.Any(p => p.Text == "Marisol" && p.EntityId == Marisol), Is.True);
        Assert.That(pinned.Any(p => p.Text == "Kyle" && p.EntityId == Kyle), Is.True);
    }

    [Test]
    public void ExtractTaggedMentions_SkipsTagsWrappingNestedMarkup()
    {
        // Not a plain surface name, so it could never anchor a scan position.
        var pinned = BeatMarkup.ExtractTaggedMentions(
            """<entity repo="character" guid="01a0030b-602e-7a47-a1ec-61747fc9d537">a <b>name</b></entity>""");

        Assert.That(pinned, Is.Empty);
    }

    [Test]
    public void PinnedMention_SurvivesWhenTheNameIsAmbiguous()
    {
        // The live shape: five Marisols, so BuildCandidateIndexAsync purged "Marisol" entirely
        // and the scanner has nothing to match.
        var candidates = new List<EntityMentionScanner.MentionCandidate>();
        var pinned = BeatMarkup.ExtractTaggedMentions(
            """<entity repo="character" guid="01a0030b-602e-7a47-a1ec-61747fc9d537">Marisol</entity> said nothing.""");

        var withPins = EntityMentionScanner.WithPinnedMentions(candidates, pinned, Live(Marisol));
        var matches = EntityMentionScanner.Scan("Marisol said nothing.", withPins);
        var tagged = EntityMentionScanner.ApplyTags("Marisol said nothing.", matches);

        Assert.That(tagged, Does.Contain($"guid=\"{Marisol}\""));
        Assert.That(tagged, Does.Contain(">Marisol</entity>"));
    }

    [Test]
    public void PinnedMention_IsDroppedWhenItsEntityIsGoneOrArchived()
    {
        // The property that made re-derivation strip-first in the first place. Pinning must never
        // let a tag outlive the entity it points at.
        var pinned = BeatMarkup.ExtractTaggedMentions(
            """<entity repo="character" guid="01a0030b-602e-7a47-a1ec-61747fc9d537">Marisol</entity> said nothing.""");

        var withPins = EntityMentionScanner.WithPinnedMentions(
            [], pinned, Live() /* nothing live */);

        Assert.That(withPins, Is.Empty);
        Assert.That(EntityMentionScanner.Scan("Marisol said nothing.", withPins), Is.Empty);
    }

    [Test]
    public void PinnedMention_UsesTheEntitysCurrentName_NotTheTagsInnerText()
    {
        // A renamed entity must re-render under its current identity — the guid is the permanent
        // fact, the tag's display text is not.
        var pinned = BeatMarkup.ExtractTaggedMentions(
            """<entity repo="character" guid="01a0030b-602e-7a47-a1ec-61747fc9d537">Marisol</entity> said nothing.""");
        var live = new Dictionary<Guid, (string Name, string EntityType)>
        {
            [Marisol] = ("Marisol Strand", "character"),
        };

        var withPins = EntityMentionScanner.WithPinnedMentions([], pinned, live);

        Assert.That(withPins, Has.Count.EqualTo(1));
        Assert.That(withPins[0].Name, Is.EqualTo("Marisol Strand"), "canonical name comes from the lookup");
        Assert.That(withPins[0].Text, Is.EqualTo("Marisol"), "but it still anchors on the surface words in the prose");
    }

    [Test]
    public void PinnedMention_OverridesAnyOtherClaimOnTheSameSurfaceText()
    {
        // If the scanner would have tagged "Marisol" as somebody else, the explicit guid wins:
        // that is the entire point of the caller having written it down.
        var wrongMarisol = Guid.NewGuid();
        var candidates = new List<EntityMentionScanner.MentionCandidate>
        {
            new("Marisol", wrongMarisol, "Marisol Teng", "character", RequiresStrictCase: true),
        };
        var pinned = BeatMarkup.ExtractTaggedMentions(
            """<entity repo="character" guid="01a0030b-602e-7a47-a1ec-61747fc9d537">Marisol</entity> said nothing.""");

        var withPins = EntityMentionScanner.WithPinnedMentions(candidates, pinned, Live(Marisol));
        var matches = EntityMentionScanner.Scan("Marisol said nothing.", withPins);

        Assert.That(matches, Has.Count.EqualTo(1));
        Assert.That(matches[0].EntityId, Is.EqualTo(Marisol));
    }

    [Test]
    public void WithPinnedMentions_MutatesTheListItIsGiven_SoBulkCallersMustCopy()
    {
        // Pins the hazard that `--tag-entities` walks into. The per-beat save paths build a fresh
        // candidate index per call, so in-place mutation is harmless there. TagEntitiesCli builds
        // ONE index per book and reuses it for every beat — pinning into that shared list would
        // leak beat N's disambiguation into beat N+1 and, worse, RemoveAll would permanently
        // delete a legitimate candidate for every later beat. This test documents the contract
        // that makes the copy at the call site mandatory rather than stylistic.
        var wrongMarisol = Guid.NewGuid();
        var shared = new List<EntityMentionScanner.MentionCandidate>
        {
            new("Marisol", wrongMarisol, "Marisol Teng", "character", RequiresStrictCase: true),
        };
        var pinned = BeatMarkup.ExtractTaggedMentions(
            """<entity repo="character" guid="01a0030b-602e-7a47-a1ec-61747fc9d537">Marisol</entity> said nothing.""");

        var returned = EntityMentionScanner.WithPinnedMentions(shared, pinned, Live(Marisol));

        Assert.That(returned, Is.SameAs(shared), "returns the same instance it mutated");
        Assert.That(shared.Any(c => c.EntityId == wrongMarisol), Is.False,
            "the caller's own list lost a candidate — which is why a shared index must be copied");

        // And the copy that TagEntitiesCli actually makes leaves the shared index intact.
        var fresh = new List<EntityMentionScanner.MentionCandidate>
        {
            new("Marisol", wrongMarisol, "Marisol Teng", "character", RequiresStrictCase: true),
        };
        var perBeat = EntityMentionScanner.WithPinnedMentions([.. fresh], pinned, Live(Marisol));

        Assert.That(perBeat.Any(c => c.EntityId == Marisol), Is.True, "the copy carries the pin");
        Assert.That(fresh.Any(c => c.EntityId == wrongMarisol), Is.True, "the shared index is untouched");
    }

    [Test]
    public void PinnedMention_DoesNotDisturbANameTheScannerAlreadyResolves()
    {
        // Kyle is unambiguous, so nothing should change — pinning is a repair for what the scan
        // cannot do, not a second tagging mechanism running alongside it.
        var candidates = new List<EntityMentionScanner.MentionCandidate>
        {
            new("Kyle", Kyle, "Kyle Ellen Corbin", "character", RequiresStrictCase: true),
        };
        var pinned = BeatMarkup.ExtractTaggedMentions(
            """<entity repo="character" guid="019d6143-a648-7876-9688-0f6d38d70075">Kyle</entity> watched.""");

        var withPins = EntityMentionScanner.WithPinnedMentions(candidates, pinned, Live(Kyle));

        Assert.That(withPins, Has.Count.EqualTo(1));
        Assert.That(withPins[0].EntityId, Is.EqualTo(Kyle));
    }

    [Test]
    public void PinnedMention_NeverAnchorsOnAStopword()
    {
        var pinned = BeatMarkup.ExtractTaggedMentions(
            """The <entity repo="character" guid="01a0030b-602e-7a47-a1ec-61747fc9d537">the</entity> stood.""");

        Assert.That(EntityMentionScanner.WithPinnedMentions([], pinned, Live(Marisol)), Is.Empty);
    }
}
