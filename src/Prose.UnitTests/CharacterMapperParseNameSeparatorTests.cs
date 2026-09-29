using Prose.Core.Data;

namespace Prose.UnitTests;

/// <summary>
/// ParseName splits on tabs as well as spaces, and never caps the token count. It used to call
/// <c>Split(' ', '\t', options)</c>, which binds to <c>Split(char, int count, options)</c>: '\t' was
/// read as a count of 9, so a tab never separated two names and a tenth token fused into the ninth.
/// </summary>
[TestFixture]
public class CharacterMapperParseNameSeparatorTests
{
    [Test]
    public void A_tab_separates_name_parts()
    {
        var p = CharacterMapper.ParseName("Kyle\tCorbin");
        Assert.That((p.First, p.Middle, p.Last), Is.EqualTo(("Kyle", "", "Corbin")));
    }

    [Test]
    public void A_name_of_more_than_nine_tokens_keeps_its_last_token_separate()
    {
        var p = CharacterMapper.ParseName("A B C D E F G H I J");
        Assert.That(p.First, Is.EqualTo("A"));
        Assert.That(p.Middle, Is.EqualTo("B C D E F G H I"));
        Assert.That(p.Last, Is.EqualTo("J"));
    }
}
