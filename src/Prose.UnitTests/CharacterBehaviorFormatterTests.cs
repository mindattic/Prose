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
        var line = CharacterBehaviorFormatter.FormatUnderPressure(stress, ["cleans his gun"], ["his own temper"], "short words");

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
        Assert.That(CharacterBehaviorFormatter.FormatUnderPressure(new Dictionary<string, string>(), [], [], null), Is.Empty);
    }

    [Test]
    public void UnderPressure_long_entry_cannot_crowd_out_critical()
    {
        var stress = new Dictionary<string, string> { ["low"] = new string('x', 2000), ["critical"] = "breaks" };
        var line = CharacterBehaviorFormatter.FormatUnderPressure(stress, [], [], null);
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
    public void Relationships_render_from_the_side_reading_them()
    {
        var employer = Guid.NewGuid();
        var worker = Guid.NewGuid();
        var names = new Dictionary<Guid, string> { [employer] = "Acme", [worker] = "John Doe" };
        var byEntity = new Dictionary<Guid, List<(Guid OtherId, string RelationType, string? Description)>>
        {
            [worker] = [(employer, "works_for", null)],
            [employer] = [(worker, SceneContextAssembler.IncomingMarker + "works_for", null)],
        };

        var workerBlock = new System.Text.StringBuilder();
        SceneContextAssembler.AppendRelationships(workerBlock, worker, byEntity, names);
        var employerBlock = new System.Text.StringBuilder();
        SceneContextAssembler.AppendRelationships(employerBlock, employer, byEntity, names);

        Assert.That(workerBlock.ToString(), Does.Contain("works_for Acme"));
        Assert.That(employerBlock.ToString(), Does.Contain("John Doe works_for them"));
        Assert.That(employerBlock.ToString(), Does.Not.Contain("works_for John Doe"));
    }

    [Test]
    public void NamesOverlap_rejects_blank_and_tiny_names()
    {
        Assert.That(CharacterBehaviorFormatter.NamesOverlap("", "John"), Is.False);
        Assert.That(CharacterBehaviorFormatter.NamesOverlap("Al", "Alan"), Is.False);
        Assert.That(CharacterBehaviorFormatter.NamesOverlap("John", "John Doe"), Is.True);
    }
}
