using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Entity rename matched the old name case-insensitively, so renaming "Silence" also rewrote every
/// ordinary lowercase "silence" in the book. It now matches the name as spelled and its ALL-CAPS
/// form, and keeps that case in the replacement. Also pins the tag-only test the read gate uses to
/// carry a receipt across a retag.
/// </summary>
[TestFixture]
public class EntityRenameCaseTests
{
    private static string Rename(string text, string oldName, string newName) =>
        EntityRenameService.ReplaceName(EntityRenameService.NameRegex(oldName), text, oldName, newName);

    [Test]
    public void The_ordinary_lowercase_word_is_left_alone()
    {
        Assert.That(Rename("Silence waited. The silence held.", "Silence", "Hush"),
            Is.EqualTo("Hush waited. The silence held."));
    }

    [Test]
    public void A_shouted_name_is_renamed_in_capitals()
    {
        Assert.That(Rename("\"SILENCE!\" Silence turned.", "Silence", "Hush"),
            Is.EqualTo("\"HUSH!\" Hush turned."));
    }

    [Test]
    public void A_name_written_in_capitals_takes_the_new_name_as_spelled()
    {
        Assert.That(Rename("The ELF answered.", "ELF", "Ghost"), Is.EqualTo("The Ghost answered."));
    }

    [Test]
    public void Only_whole_words_match()
    {
        Assert.That(Rename("Silences and Silence.", "Silence", "Hush"), Is.EqualTo("Silences and Hush."));
    }

    [Test]
    public void The_name_inside_an_entity_tag_is_renamed()
    {
        var text = "<entity repo=\"character\" guid=\"0197e9c9-0001-7000-8000-000000000001\">Silence</entity> left.";
        Assert.That(Rename(text, "Silence", "Hush"), Does.Contain(">Hush</entity> left."));
    }

    [Test]
    public void Wrapping_a_word_in_a_tag_is_a_tag_only_change()
    {
        const string before = "Kyle drew the Cacophony.";
        const string after = "Kyle drew the <entity repo=\"weapon\" guid=\"0197e9c9-0001-7000-8000-000000000002\">Cacophony</entity>.";
        Assert.That(ProseDbContext.IsTagOnlyChange(before, after), Is.True);
    }

    [Test]
    public void Changing_a_word_is_not_a_tag_only_change()
    {
        Assert.That(ProseDbContext.IsTagOnlyChange("Kyle drew the gun.", "Kyle drew the knife."), Is.False);
    }
}
