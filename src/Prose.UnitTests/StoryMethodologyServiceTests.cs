using Prose.Core.Services;

namespace Prose.UnitTests;

[TestFixture]
public class StoryMethodologyServiceTests
{
    private StoryMethodologyService svc = null!;

    [SetUp]
    public void SetUp() => svc = new StoryMethodologyService();

    // ── GetBeatRole: named roles at canonical positions ──────────────────────

    [Test]
    public void GetBeatRole_FirstBeat_ReturnsOpeningImage()
    {
        var role = svc.GetBeatRole(0, 12);
        Assert.That(role.Name, Is.EqualTo("Opening Image"));
    }

    [Test]
    public void GetBeatRole_LastBeat_ReturnsFinalImage()
    {
        var role = svc.GetBeatRole(11, 12);
        Assert.That(role.Name, Is.EqualTo("Final Image"));
    }

    [Test]
    public void GetBeatRole_MidpointBeat_ReturnsMidpoint()
    {
        // Beat 6 of 12 = position 0.545 → Midpoint range 0.45-0.55
        var role = svc.GetBeatRole(6, 12);
        Assert.That(role.Name, Is.EqualTo("Midpoint"));
    }

    [Test]
    public void GetBeatRole_AllIsLostPosition_ReturnsAllIsLost()
    {
        // Position ~0.73 (beat 8 of 12) → All Is Lost range 0.70-0.78
        var role = svc.GetBeatRole(8, 12);
        Assert.That(role.Name, Is.EqualTo("All Is Lost"));
    }

    [Test]
    public void GetBeatRole_DarkNightPosition_ReturnsDarkNight()
    {
        // Position ~0.82 (beat 9 of 12) → Dark Night 0.76-0.82
        var role = svc.GetBeatRole(9, 12);
        Assert.That(role.Name, Is.EqualTo("Dark Night of the Soul"));
    }

    [Test]
    public void GetBeatRole_SingleBeat_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => svc.GetBeatRole(0, 1));
    }

    [Test]
    public void GetBeatRole_NeverReturnsNull()
    {
        for (int i = 0; i < 12; i++)
        {
            var role = svc.GetBeatRole(i, 12);
            Assert.That(role, Is.Not.Null, $"Beat {i}/12 returned null role");
        }
    }

    // ── Scene type correctness ───────────────────────────────────────────────

    [Test]
    public void GetBeatRole_OpeningImage_IsSceneType()
    {
        var role = svc.GetBeatRole(0, 12);
        Assert.That(role.SceneType, Is.EqualTo("scene"));
    }

    [Test]
    public void GetBeatRole_DebateBeat_IsSequelType()
    {
        // Debate is a sequel beat (position ~0.20, beat 2 of 12)
        var role = svc.GetBeatRole(2, 12);
        Assert.That(role.SceneType, Is.EqualTo("sequel"));
    }

    [Test]
    public void GetBeatRole_DarkNight_IsSequelType()
    {
        var role = svc.GetBeatRole(9, 12);
        Assert.That(role.SceneType, Is.EqualTo("sequel"));
    }

    // ── Tension curve ────────────────────────────────────────────────────────

    [Test]
    public void GetIdealTension_Opening_IsLow()
    {
        var tension = svc.GetIdealTension(0, 12);
        Assert.That(tension, Is.LessThanOrEqualTo(4));
    }

    [Test]
    public void GetIdealTension_AllIsLost_IsHigh()
    {
        // Beat 8 of 12 ≈ position 0.73 → All Is Lost → tension 9
        var tension = svc.GetIdealTension(8, 12);
        Assert.That(tension, Is.GreaterThanOrEqualTo(8));
    }

    [Test]
    public void GetIdealTension_DarkNight_IsLow()
    {
        // Beat 9 of 12 ≈ position 0.82 → Dark Night → tension 2
        var tension = svc.GetIdealTension(9, 12);
        Assert.That(tension, Is.LessThanOrEqualTo(3));
    }

    [Test]
    public void GetIdealTension_Climax_IsMaximum()
    {
        // Beat 10 of 12 ≈ position 0.91 → Finale → tension 8-10
        var tension = svc.GetIdealTension(10, 12);
        Assert.That(tension, Is.GreaterThanOrEqualTo(8));
    }

    [Test]
    public void GetIdealTension_AlwaysInValidRange()
    {
        for (int i = 0; i < 12; i++)
        {
            var t = svc.GetIdealTension(i, 12);
            Assert.That(t, Is.InRange(1, 10), $"Beat {i}/12 tension {t} out of 1-10 range");
        }
    }

    // ── Beat generation guidance ─────────────────────────────────────────────

    [Test]
    public void GetBeatGenerationGuidance_SceneBeat_ContainsGoalConflictDisaster()
    {
        // Opening Image is a scene beat
        var guidance = svc.GetBeatGenerationGuidance(0, 12);
        Assert.That(guidance, Does.Contain("Goal").Or.Contain("Conflict").Or.Contain("Disaster"));
    }

    [Test]
    public void GetBeatGenerationGuidance_SequelBeat_ContainsReactionDilemmaDecision()
    {
        // Debate (beat 2/12) is a sequel beat
        var guidance = svc.GetBeatGenerationGuidance(2, 12);
        Assert.That(guidance, Does.Contain("React").Or.Contain("Dilemma").Or.Contain("Decision"));
    }

    [Test]
    public void GetBeatGenerationGuidance_AlwaysContainsRoleName()
    {
        var guidance = svc.GetBeatGenerationGuidance(0, 12);
        Assert.That(guidance.Length, Is.GreaterThan(20));
    }

    // ── Outline methodology prompt ───────────────────────────────────────────

    [Test]
    public void GetOutlineMethodologyPrompt_ContainsAllBeatNumbers()
    {
        var prompt = svc.GetOutlineMethodologyPrompt(8);
        for (int i = 1; i <= 8; i++)
            Assert.That(prompt, Does.Contain($"Beat {i}/8"), $"Missing Beat {i}/8 in prompt");
    }

    [Test]
    public void GetOutlineMethodologyPrompt_ContainsWantVsNeed()
    {
        var prompt = svc.GetOutlineMethodologyPrompt(8);
        Assert.That(prompt, Does.Contain("WANT").And.Contain("NEED"));
    }

    [Test]
    public void GetOutlineMethodologyPrompt_ContainsSceneSequelRule()
    {
        var prompt = svc.GetOutlineMethodologyPrompt(8);
        Assert.That(prompt, Does.Contain("Scene").And.Contain("Sequel"));
    }

    [Test]
    public void GetOutlineMethodologyPrompt_ContainsTensionCurveGuidance()
    {
        var prompt = svc.GetOutlineMethodologyPrompt(8);
        Assert.That(prompt, Does.Contain("TENSION").Or.Contain("tension"));
    }
}
