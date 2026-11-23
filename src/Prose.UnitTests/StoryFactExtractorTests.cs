using Prose.Core.Services.ReadAlong;
using static Prose.Core.Services.ReadAlong.StoryFactExtractor;

namespace Prose.UnitTests;

/// <summary>RFC 0019: the read-along's deterministic extractor — names met, details stated, each
/// with the sentence it came from.</summary>
[TestFixture]
public class StoryFactExtractorTests
{
    private static Paragraph P(string text) => new(Guid.NewGuid(), text);

    [Test]
    public void Finds_names_and_skips_sentence_starters()
    {
        var r = Extract(
        [
            P("The fog came off the moor before first light. Elena crossed the last stretch of open ground with her hood up. Frost lay over everything, and the frost was fine as flour."),
            P("She had come alone, and told no one but Kael, and Kael was three days south and furious about it."),
        ], []);

        var met = r.Names.Where(n => n.IsNew).Select(n => n.Name).Distinct().ToList();
        Assert.That(met, Does.Contain("Elena"));
        Assert.That(met, Does.Contain("Kael"));
        Assert.That(met, Does.Not.Contain("Frost"));
        Assert.That(met, Does.Not.Contain("The"));
    }

    [Test]
    public void Age_by_pronoun_resolves_to_the_last_character_with_its_quote()
    {
        var r = Extract([P("Elena crossed the yard. She was twenty-eight.")], []);
        var age = r.Facts.Single(f => f.Attribute == "age");
        Assert.That(age.SubjectName, Is.EqualTo("Elena"));
        Assert.That(age.Value, Is.EqualTo("28"));
        Assert.That(age.Quote, Is.EqualTo("She was twenty-eight."));
    }

    [Test]
    public void Eyes_scar_item_and_timeline_details()
    {
        var r = Extract(
        [
            P("Kael said nothing. Kael's eyes were grey, and a scar ran across his left cheek."),
            P("Three years since the Siege, and no one had troubled to right it. Elena pulled her grey cloak tight."),
            P("The staff needs three months to refill."),
        ], []);

        Assert.That(r.Facts.Any(f => f.SubjectName == "Kael" && f.Attribute == "eyes" && f.Value == "grey"));
        Assert.That(r.Facts.Any(f => f.SubjectName == "Kael" && f.Attribute == "scar" && f.Value == "left cheek"));
        Assert.That(r.Facts.Any(f => f.SubjectKey == "siege" && f.Attribute == "years-ago" && f.Value == "3"));
        Assert.That(r.Facts.Any(f => f.SubjectKey == "staff" && f.Attribute == "time to refill" && f.Value == "3 months"));
    }

    [Test]
    public void Known_entries_match_by_name_and_first_name()
    {
        var id = Guid.NewGuid();
        var r = Extract([P("Sera walked in. Sera's hair was black.")],
            [new KnownEntry(id, "Sera Vane", CatCharacter, [])]);
        Assert.That(r.Names.All(n => !n.IsNew));
        Assert.That(r.Facts.Single(f => f.Attribute == "hair").EntityId, Is.EqualTo(id));
    }

    [Test]
    public void Tossed_subjects_stay_tossed()
    {
        var r = Extract([P("Elena crossed the yard and Kael followed.")], [], new HashSet<string> { "kael" });
        Assert.That(r.Names.Any(n => n.Key == "kael"), Is.False);
    }

    [Test]
    public void Kin_relation_is_recorded()
    {
        var r = Extract([P("Maren watched the door. Maren's brother, Tobin Hale, came in late.")], []);
        Assert.That(r.Relations.Any(x => x.FromName == "Maren" && x.Type == "brother" && x.ToName == "Tobin Hale"));
    }

    [Test]
    public void Compare_age_growing_is_an_update_shrinking_is_a_contradiction()
    {
        Assert.That(Compare("age", "28", "29", 85, true).Kind, Is.EqualTo("updated"));
        Assert.That(Compare("age", "28", "24", 85, true).Kind, Is.EqualTo("contradiction"));
        Assert.That(Compare("eyes", "grey", "grey", 90, true).Confidence, Is.EqualTo(0));
        Assert.That(Compare("eyes", "grey", "green", 90, true).Kind, Is.EqualTo("contradiction"));
    }

    [Test]
    public void Numbers_and_values_normalize()
    {
        Assert.That(ParseNumber("twenty-eight"), Is.EqualTo(28));
        Assert.That(ParseNumber("a hundred"), Is.EqualTo(100));
        Assert.That(NormalizeValue("eyes", "Gray"), Is.EqualTo("grey"));
        Assert.That(NormalizeValue("time to heal", "two weeks"), Is.EqualTo("2 weeks"));
    }
}

[TestFixture]
public class StoryFactExtractorMergeTests
{
    [Test]
    public void A_first_name_or_titled_surname_folds_into_the_full_name_as_an_alias()
    {
        var r = StoryFactExtractor.Extract(
        [
            new(Guid.NewGuid(), "She had told no one but Hollis Marsh. Hollis said the tide would turn."),
            new(Guid.NewGuid(), "Captain Aldous waited by the fire. Later, Aldous watched her. Wren's brother, Hollis Marsh, laughed."),
        ], []);
        var met = r.Names.Where(n => n.IsNew).Select(n => n.Name).Distinct().ToList();
        Assert.That(met, Does.Contain("Hollis Marsh").And.Contain("Captain Aldous"));
        Assert.That(met, Does.Not.Contain("Hollis").And.Not.Contain("Aldous"));
        Assert.That(r.Facts.Any(f => f.SubjectName == "Hollis Marsh" && f.Attribute == "alias" && f.Value == "Hollis"));
        Assert.That(r.Facts.Any(f => f.Attribute == "brother" && f.Value == "Hollis Marsh"), "names keep their case");
    }
}

[TestFixture]
public class StoryFactExtractorPronounTests
{
    [Test]
    public void A_possessive_pronoun_belongs_to_the_nearest_name_in_its_own_sentence()
    {
        var r = StoryFactExtractor.Extract(
        [
            new(Guid.NewGuid(), "Wren crossed the flats. She was nineteen."),
            new(Guid.NewGuid(), "Captain Aldous was dead by evening, found below the locks."),
            new(Guid.NewGuid(), "Wren pulled her black coat tight against the wind."),
        ], []);
        var coat = r.Facts.Single(f => f.Attribute == "coat");
        Assert.That(coat.SubjectName, Is.EqualTo("Wren"));
        Assert.That(coat.Value, Is.EqualTo("black"));
    }

    [Test]
    public void A_pronoun_skips_a_character_of_the_other_known_gender()
    {
        var r = StoryFactExtractor.Extract(
        [
            new(Guid.NewGuid(), "Wren crossed the flats alone. She was nineteen."),
            new(Guid.NewGuid(), "Lord Tobin met Wren at the gate. Lord Tobin said nothing."),
            new(Guid.NewGuid(), "Her eyes were grey in the rain."),
        ], []);
        Assert.That(r.Facts.Single(f => f.Attribute == "eyes").SubjectName, Is.EqualTo("Wren"));
    }
}

[TestFixture]
public class StoryFactExtractorSingleValueTests
{
    [TestCase("time to recharge")]
    [TestCase("coat")]
    [TestCase("eyes")]
    public void World_rules_and_item_looks_hold_one_value(string attr) =>
        Assert.That(StoryFactExtractor.IsSingleValued(attr), Is.True);

    [TestCase("carries")]
    [TestCase("brother")]
    [TestCase("alias")]
    public void Relations_and_lists_accumulate(string attr) =>
        Assert.That(StoryFactExtractor.IsSingleValued(attr), Is.False);
}
