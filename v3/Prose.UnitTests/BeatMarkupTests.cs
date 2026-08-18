using NUnit.Framework;
using Prose.Core.Services;

namespace Prose.UnitTests;

[TestFixture]
public class BeatMarkupTests
{
    [Test]
    public void StripEntityTags_SingleTag_ReturnsInnerTextOnly()
    {
        var id = Guid.NewGuid();
        var tagged = $"""<entity guid="{id}">Declan Doyle</entity> went back down into the seam.""";

        var stripped = BeatMarkup.StripEntityTags(tagged);

        Assert.That(stripped, Is.EqualTo("Declan Doyle went back down into the seam."));
    }

    [Test]
    public void StripEntityTags_MultipleTags_StripsAll()
    {
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        var tagged = $"""<entity guid="{idA}">Declan Doyle</entity> spoke to <entity guid="{idB}">Lyra</entity> by the gate.""";

        var stripped = BeatMarkup.StripEntityTags(tagged);

        Assert.That(stripped, Is.EqualTo("Declan Doyle spoke to Lyra by the gate."));
    }

    [Test]
    public void StripEntityTags_NoTags_ReturnsTextUnchanged()
    {
        var plain = "There were no entities tagged in this beat at all.";

        Assert.That(BeatMarkup.StripEntityTags(plain), Is.EqualTo(plain));
    }

    [Test]
    public void StripEntityTags_NullOrEmpty_HandledSafely()
    {
        Assert.That(BeatMarkup.StripEntityTags(null), Is.EqualTo(""));
        Assert.That(BeatMarkup.StripEntityTags(""), Is.EqualTo(""));
    }

    [Test]
    public void ExtractEntityGuids_ReturnsDistinctGuidsInFirstOccurrenceOrder()
    {
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        var tagged = $"""<entity guid="{idA}">Declan Doyle</entity> spoke to <entity guid="{idB}">Lyra</entity>, then <entity guid="{idA}">Declan Doyle</entity> left.""";

        var guids = BeatMarkup.ExtractEntityGuids(tagged).ToList();

        Assert.That(guids, Has.Count.EqualTo(2), "must dedupe repeated mentions of the same entity");
        Assert.That(guids[0], Is.EqualTo(idA));
        Assert.That(guids[1], Is.EqualTo(idB));
    }

    [Test]
    public void ExtractEntityGuids_NoTags_ReturnsEmpty()
    {
        Assert.That(BeatMarkup.ExtractEntityGuids("plain prose, no tags").ToList(), Is.Empty);
    }
}
