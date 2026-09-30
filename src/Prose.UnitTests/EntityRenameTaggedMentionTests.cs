using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Entity rename rewrites the entity's own tags and nothing else. It used to match the old name as
/// loose text, rewriting any word that spelled it; every real mention is a tag carrying the
/// entity's id, so a word outside one is not a mention. Also pins the tag-only test the read gate
/// uses to carry a receipt across a retag.
/// </summary>
[TestFixture]
public class EntityRenameTaggedMentionTests
{
    private static readonly Guid Sword = Guid.Parse("0197e9c9-0001-7000-8000-00000000a001");
    private static readonly Guid Other = Guid.Parse("0197e9c9-0001-7000-8000-00000000a002");

    private static string Tag(Guid id, string text) => $"<entity repo=\"weapon\" guid=\"{id}\">{text}</entity>";

    [Test]
    public void Only_the_entitys_own_tags_are_renamed()
    {
        var text = $"{Tag(Sword, "Silence")} cut. The silence held. Silence fell.";
        Assert.That(EntityRenameService.RenameTaggedMentions(text, Sword, "Silence", "Hush"),
            Is.EqualTo($"{Tag(Sword, "Hush")} cut. The silence held. Silence fell."));
    }

    [Test]
    public void A_tag_for_another_entity_is_left_alone()
    {
        var text = $"{Tag(Other, "Silence")} and {Tag(Sword, "Silence")}.";
        Assert.That(EntityRenameService.RenameTaggedMentions(text, Sword, "Silence", "Hush"),
            Is.EqualTo($"{Tag(Other, "Silence")} and {Tag(Sword, "Hush")}."));
    }

    [Test]
    public void A_tag_that_reads_otherwise_keeps_its_words()
    {
        var text = $"She drew {Tag(Sword, "the blade")}.";
        Assert.That(EntityRenameService.RenameTaggedMentions(text, Sword, "Silence", "Hush"), Is.EqualTo(text));
    }

    [Test]
    public void The_bare_guid_form_is_recognised()
    {
        var text = $"<entity repo=\"weapon\" guid=\"{Sword:N}\">Silence</entity>";
        Assert.That(EntityRenameService.RenameTaggedMentions(text, Sword, "Silence", "Hush"), Does.Contain(">Hush</entity>"));
    }

    [Test]
    public void A_beat_with_only_the_loose_word_is_not_a_mention()
    {
        Assert.That(EntityRenameService.MentionsByName("The silence held. Silence fell.", Sword, "Silence"), Is.False);
        Assert.That(EntityRenameService.MentionsByName($"{Tag(Sword, "Silence")} cut.", Sword, "Silence"), Is.True);
    }

    [Test]
    public void Wrapping_a_word_in_a_tag_is_a_tag_only_change()
    {
        const string before = "Kyle drew the Cacophony.";
        var after = $"Kyle drew the {Tag(Other, "Cacophony")}.";
        Assert.That(ProseDbContext.IsTagOnlyChange(before, after), Is.True);
    }

    [Test]
    public void Changing_a_word_is_not_a_tag_only_change()
    {
        Assert.That(ProseDbContext.IsTagOnlyChange("Kyle drew the gun.", "Kyle drew the knife."), Is.False);
    }
}
