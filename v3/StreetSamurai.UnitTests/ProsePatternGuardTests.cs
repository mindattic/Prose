using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

[TestFixture]
public class ProsePatternGuardTests
{
    readonly ProsePatternGuard guard = new();

    [Test]
    public void Check_EmptyString_ReturnsEmptyList()
    {
        var result = guard.Check("");
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Check_NullString_ReturnsEmptyOrDoesNotThrow()
    {
        List<ProseViolation> result = null!;
        Assert.DoesNotThrow(() => result = guard.Check(null!));
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Check_CleanText_ReturnsEmptyList()
    {
        var clean = "Kyle crossed the bridge. The rain hit the steel rail. He kept moving.";
        var result = guard.Check(clean);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Check_ChromeGleam_ReturnsCliqueViolation()
    {
        var result = guard.Check("The chrome gleam of the tower reflected the sky.");
        Assert.That(result, Has.Count.GreaterThan(0));
        Assert.That(result[0].Category, Is.EqualTo(ProseViolationCategory.Cliche));
    }

    [Test]
    public void Check_NeonWashed_DetectedAsCliched()
    {
        var result = guard.Check("The neon-washed alley smelled of rot.");
        Assert.That(result.Any(v => v.Category == ProseViolationCategory.Cliche), Is.True);
    }

    [Test]
    public void Check_HeartHammered_DetectedAsCliched()
    {
        var result = guard.Check("His heart hammered as he ran.");
        Assert.That(result.Any(v => v.Category == ProseViolationCategory.Cliche), Is.True);
    }

    [Test]
    public void Check_InThisCity_DetectedAsCliched()
    {
        var result = guard.Check("In this city, nobody trusts anyone.");
        Assert.That(result.Any(v => v.Category == ProseViolationCategory.Cliche), Is.True);
    }

    [Test]
    public void Check_InThatMoment_DetectedAsPseudoProfound()
    {
        var result = guard.Check("In that moment, everything crystallised.");
        Assert.That(result.Any(v => v.Category == ProseViolationCategory.PseudoProfound), Is.True);
    }

    [Test]
    public void Check_TheTruthWas_DetectedAsPseudoProfound()
    {
        var result = guard.Check("The truth was that he had never belonged here.");
        Assert.That(result.Any(v => v.Category == ProseViolationCategory.PseudoProfound), Is.True);
    }

    [Test]
    public void Check_SuddenlyUnderstood_DetectedAsPseudoProfound()
    {
        var result = guard.Check("She suddenly understood what the contract meant.");
        Assert.That(result.Any(v => v.Category == ProseViolationCategory.PseudoProfound), Is.True);
    }

    [Test]
    public void Check_KyleThoughtAboutHow_DetectedAsOnTheNose()
    {
        var result = guard.Check("Kyle thought about how the job would change everything.");
        Assert.That(result.Any(v => v.Category == ProseViolationCategory.OnTheNose), Is.True);
    }

    [Test]
    public void Check_ThisWasThePartWhere_DetectedAsOnTheNose()
    {
        var result = guard.Check("This was the part where things got complicated.");
        Assert.That(result.Any(v => v.Category == ProseViolationCategory.OnTheNose), Is.True);
    }

    [Test]
    public void Check_ItalicisedDialogue_DetectedCorrectly()
    {
        var result = guard.Check("She said *\"I'll be back\"* and walked away.");
        Assert.That(result.Any(v => v.Category == ProseViolationCategory.ItalicisedDialogue), Is.True);
    }


    [Test]
    public void Check_MultipleViolations_AllReturned()
    {
        var text = "The chrome gleam blinded him. In that moment, he thought about how the neon-washed street looked.";
        var result = guard.Check(text);
        Assert.That(result.Count, Is.GreaterThanOrEqualTo(3));
    }

    [Test]
    public void Check_CharOffsetAlwaysNonNegative()
    {
        var text = "The chrome gleam lit the neon-washed alley. In that moment, everything changed.";
        var result = guard.Check(text);
        Assert.That(result.All(v => v.CharOffset >= 0), Is.True);
    }

    [Test]
    public void Check_ViolationsOrderedByCharOffset()
    {
        var text = "The chrome gleam lit the neon-washed alley.";
        var result = guard.Check(text);
        if (result.Count > 1)
        {
            for (int i = 1; i < result.Count; i++)
                Assert.That(result[i].CharOffset, Is.GreaterThanOrEqualTo(result[i - 1].CharOffset));
        }
    }

    [Test]
    public void Check_AdditionalProhibition_Detected()
    {
        var result = guard.Check("The signal was lost in the static.", ["static"]);
        Assert.That(result.Any(v => v.Match.Equals("static", StringComparison.OrdinalIgnoreCase)), Is.True);
    }

    [Test]
    public void Check_AdditionalProhibition_CaseInsensitive()
    {
        var result = guard.Check("He felt the Static crackle.", ["static"]);
        Assert.That(result.Any(v => v.Match.Equals("static", StringComparison.OrdinalIgnoreCase)), Is.True);
    }

    [Test]
    public void Check_AdditionalProhibition_MultipleOccurrences_AllFound()
    {
        var result = guard.Check("Static here and static there.", ["static"]);
        Assert.That(result.Count(v => v.Match.Equals("static", StringComparison.OrdinalIgnoreCase)), Is.EqualTo(2));
    }

    [Test]
    public void Check_AdditionalProhibition_EmptyPhrase_Ignored()
    {
        Assert.DoesNotThrow(() => guard.Check("Clean text.", [""]));
    }

    [Test]
    public void Check_AdditionalProhibition_Category_IsCliched()
    {
        var result = guard.Check("The ghost walked past.", ["ghost"]);
        var v = result.FirstOrDefault(v => v.Match.Equals("ghost", StringComparison.OrdinalIgnoreCase));
        Assert.That(v, Is.Not.Null);
        Assert.That(v!.Category, Is.EqualTo(ProseViolationCategory.Cliche));
    }

    [Test]
    public void Check_CategoryEnumValuesAreSet_NotDefault()
    {
        var text = "The chrome gleam lit the neon-washed alley. In that moment, the truth was clear.";
        var result = guard.Check(text);
        var categories = result.Select(v => v.Category).Distinct().ToList();
        Assert.That(categories, Has.Count.GreaterThan(1));
    }

    [Test]
    public void Check_RuleFieldIsNonEmpty_OnAllViolations()
    {
        var text = "The chrome gleam lit the neon-washed alley. In that moment, the truth was clear.";
        var result = guard.Check(text);
        Assert.That(result.All(v => !string.IsNullOrEmpty(v.Rule)), Is.True);
    }

    [Test]
    public void Check_MatchFieldIsNonEmpty_OnAllViolations()
    {
        var text = "The chrome gleam was visible from across the neon-washed plaza.";
        var result = guard.Check(text);
        Assert.That(result.All(v => !string.IsNullOrEmpty(v.Match)), Is.True);
    }
}
