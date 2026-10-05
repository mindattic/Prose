using Prose.Core.Services;

namespace Prose.UnitTests;

[TestFixture]
public class CharacterBehaviorFormatterTests
{
    [Test]
    public void UnderPressure_emits_every_recorded_level_in_ladder_order()
    {
        var stress = new Dictionary<string, string>
        {
            ["critical"] = "goes silent and walks out",
            ["low"] = "jokes",
            ["high"] = "snaps at allies",
            ["medium"] = "counts exits",
        };
        var line = CharacterBehaviorFormatter.FormatUnderPressure(stress, ["cleans his gun"], ["his own temper"], "short words", 800);

        Assert.That(line, Does.StartWith("UNDER PRESSURE"));
        var low = line.IndexOf("low: jokes", StringComparison.Ordinal);
        var medium = line.IndexOf("medium: counts exits", StringComparison.Ordinal);
        var high = line.IndexOf("high: snaps at allies", StringComparison.Ordinal);
        var critical = line.IndexOf("critical: goes silent", StringComparison.Ordinal);
        Assert.That(new[] { low, medium, high, critical }, Is.All.GreaterThan(0));
        Assert.That(low < medium && medium < high && high < critical, "levels in ladder order");
        Assert.That(line, Does.Contain("copes by: cleans his gun"));
        Assert.That(line, Does.Contain("blind to: his own temper"));
        Assert.That(line, Does.Contain("speech: short words"));
    }

    [Test]
    public void UnderPressure_is_empty_when_nothing_is_recorded()
    {
        Assert.That(CharacterBehaviorFormatter.FormatUnderPressure(new Dictionary<string, string>(), [], [], null, 400), Is.Empty);
    }

    [Test]
    public void UnderPressure_long_entry_cannot_crowd_out_critical()
    {
        var stress = new Dictionary<string, string> { ["low"] = new string('x', 2000), ["critical"] = "breaks" };
        var line = CharacterBehaviorFormatter.FormatUnderPressure(stress, [], [], null, 300);
        Assert.That(line, Does.Contain("critical: breaks"));
    }

    [Test]
    public void ModeToward_matches_the_named_partner_only()
    {
        var modes = new Dictionary<string, string> { ["Jane Roe"] = "formal, guarded", ["Wren"] = "teasing" };
        Assert.That(CharacterBehaviorFormatter.ModeToward(modes, "Dr. Jane Roe"), Is.EqualTo("formal, guarded"));
        Assert.That(CharacterBehaviorFormatter.ModeToward(modes, "Wren"), Is.EqualTo("teasing"));
        Assert.That(CharacterBehaviorFormatter.ModeToward(modes, "John Doe"), Is.Null);
    }

    [Test]
    public void NamesOverlap_rejects_blank_and_tiny_names()
    {
        Assert.That(CharacterBehaviorFormatter.NamesOverlap("", "John"), Is.False);
        Assert.That(CharacterBehaviorFormatter.NamesOverlap("Al", "Alan"), Is.False);
        Assert.That(CharacterBehaviorFormatter.NamesOverlap("John", "John Doe"), Is.True);
    }
}
