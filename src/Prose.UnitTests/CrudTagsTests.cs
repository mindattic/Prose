using NUnit.Framework;
using Prose.Mcp;

namespace Prose.UnitTests;

/// <summary>The create_* tools' tags parameter: "[]" clears (it used to become a literal tag "[]",
/// and the tag replace after the save then detached every real tag); commas split.</summary>
public class CrudTagsTests
{
    [Test]
    public void Empty_brackets_clear()
    {
        Assert.That(CrudTags.Parse("[]"), Is.Empty);
        Assert.That(CrudTags.Parse(" [] "), Is.Empty);
    }

    [Test]
    public void Commas_split_and_blanks_drop()
    {
        Assert.That(CrudTags.Parse("vigl, bcoda ,,Tier 3"), Is.EqualTo(new[] { "vigl", "bcoda", "Tier 3" }));
    }
}
