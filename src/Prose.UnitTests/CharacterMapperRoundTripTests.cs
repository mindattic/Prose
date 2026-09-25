using Prose.Core.Data;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// A character survives a save: every belongings bucket the mapper reads back is also written
/// (ranged_weapon, tool_slot and carried_loot were read but never written, so any save deleted
/// them), and name parsing does not mistake an apostrophe inside a name for a quoted alias.
/// </summary>
[TestFixture]
public class CharacterMapperRoundTripTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;
    private CharacterRepository repo = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_mapper_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);
        repo = new CharacterRepository(paths);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    [Test]
    public void Every_belongings_bucket_survives_a_save()
    {
        var c = new CharacterData { Id = Guid.NewGuid().ToString("N"), Type = "character", Name = "Kyle Test" };
        c.Belongings.RangedWeapon = "Howl FB-7";
        c.Belongings.ToolSlot = "lockpicks";
        c.Belongings.CarriedLoot = ["a brown coat", "a soda-bottle shard"];
        repo.Save(c);

        var back = repo.GetById(c.Id)!;
        back.Description = "an unrelated edit";
        repo.Save(back);

        var again = repo.GetById(c.Id)!;
        Assert.That(again.Belongings.RangedWeapon, Is.EqualTo("Howl FB-7"));
        Assert.That(again.Belongings.ToolSlot, Is.EqualTo("lockpicks"));
        Assert.That(again.Belongings.CarriedLoot, Is.EqualTo(new[] { "a brown coat", "a soda-bottle shard" }));
    }

    [Test]
    public void Clearing_a_field_on_purpose_is_what_a_by_name_read_then_serves()
    {
        var c = new CharacterData { Id = Guid.NewGuid().ToString("N"), Type = "character", Name = "Hook Holder", StoryHooks = ["an open thread"] };
        repo.Save(c);
        Assert.That(repo.GetByName("Hook Holder")!.StoryHooks, Is.Not.Empty);

        var edit = repo.GetById(c.Id)!;
        edit.StoryHooks = [];
        repo.Save(edit);

        Assert.That(repo.GetByName("Hook Holder")!.StoryHooks, Is.Empty,
            "the depth guard protects the cache from a lossy rebuild, not from the author's own edit");
    }

    [Test]
    public void An_apostrophe_inside_a_name_is_not_a_quoted_alias()
    {
        var p = CharacterMapper.ParseName("D'Angelo O'Neil");
        Assert.That((p.First, p.Last), Is.EqualTo(("D'Angelo", "O'Neil")));

        var q = CharacterMapper.ParseName("Sean O'Brien 'Ace' Smith");
        Assert.That((q.First, q.Middle, q.Last), Is.EqualTo(("Sean", "O'Brien", "Smith")));

        var r = CharacterMapper.ParseName("Sasha 'Lena Connor' Võ");
        Assert.That((r.First, r.Middle, r.Last), Is.EqualTo(("Sasha", "", "Võ")));
    }
}
