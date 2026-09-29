using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for GetByEntity's id matching: claims store EntityId in "N" form (32 hex
/// digits), while callers holding a Guid passed <c>Guid.ToString()</c> ("D", hyphenated) and got
/// no claims back — which left RamificationService's "already established" facts permanently empty.
/// </summary>
[TestFixture]
public class ContinuityServiceEntityIdFormsTests
{
    [Test]
    public void HyphenatedGuid_AlsoMatchesNForm()
    {
        var g = Guid.NewGuid();
        var forms = ContinuityService.EntityIdForms(g.ToString());
        Assert.That(forms, Does.Contain(g.ToString("N")));
        Assert.That(forms, Does.Contain(g.ToString("D")));
    }

    [Test]
    public void NFormGuid_AlsoMatchesHyphenatedForm()
    {
        var g = Guid.NewGuid();
        var forms = ContinuityService.EntityIdForms(g.ToString("N"));
        Assert.That(forms, Does.Contain(g.ToString("D")));
        Assert.That(forms.Distinct().Count(), Is.EqualTo(forms.Count), "no duplicate forms");
    }

    [Test]
    public void NonGuidId_IsMatchedVerbatimOnly()
    {
        Assert.That(ContinuityService.EntityIdForms("e1"), Is.EqualTo(new[] { "e1" }));
    }
}
