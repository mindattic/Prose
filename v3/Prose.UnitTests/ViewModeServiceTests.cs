using Prose.Core.Services;

namespace Prose.UnitTests;

[TestFixture]
public class ViewModeServiceTests
{
    private ViewModeService svc = null!;

    [SetUp]
    public void Setup() => svc = new ViewModeService();

    [Test]
    public void DefaultMode_IsReadOnly()
    {
        Assert.That(svc.IsEditMode("weaponry"), Is.False);
    }

    [Test]
    public void Toggle_SwitchesToEditMode()
    {
        svc.Toggle("weaponry");
        Assert.That(svc.IsEditMode("weaponry"), Is.True);
    }

    [Test]
    public void Toggle_Twice_BackToReadOnly()
    {
        svc.Toggle("weaponry");
        svc.Toggle("weaponry");
        Assert.That(svc.IsEditMode("weaponry"), Is.False);
    }

    [Test]
    public void SetEditMode_Explicit()
    {
        svc.SetEditMode("factions", true);
        Assert.That(svc.IsEditMode("factions"), Is.True);
        svc.SetEditMode("factions", false);
        Assert.That(svc.IsEditMode("factions"), Is.False);
    }

    [Test]
    public void Repos_Are_Independent()
    {
        svc.Toggle("weaponry");
        Assert.That(svc.IsEditMode("weaponry"), Is.True);
        Assert.That(svc.IsEditMode("factions"), Is.False);
    }

    [Test]
    public void CaseInsensitive()
    {
        svc.SetEditMode("Weaponry", true);
        Assert.That(svc.IsEditMode("weaponry"), Is.True);
        Assert.That(svc.IsEditMode("WEAPONRY"), Is.True);
    }
}
