using Prose.Core.Services;

namespace Prose.UnitTests;

[TestFixture]
public class PacingServiceDetailedTests
{
    // ── Position-based mode selection ────────────────────────────────────────

    [Test]
    public void GetPacing_BeatZero_ReturnsBreathe()
    {
        var result = PacingService.GetPacing(0, 12);
        Assert.That(result.Mode, Is.EqualTo(PacingService.PaceMode.Breathe));
    }

    [Test]
    public void GetPacing_EarlyBeat_ReturnsBreathe()
    {
        // Beat 1 of 12 = position 0.09 — inside < 0.15 Breathe zone
        var result = PacingService.GetPacing(1, 12);
        Assert.That(result.Mode, Is.EqualTo(PacingService.PaceMode.Breathe));
    }

    [Test]
    public void GetPacing_DevelopmentBeat_ReturnsFlow()
    {
        // Beat 3 of 12 = position 0.27 — inside 0.15..0.4 Flow zone
        var result = PacingService.GetPacing(3, 12);
        Assert.That(result.Mode, Is.EqualTo(PacingService.PaceMode.Flow));
    }

    [Test]
    public void GetPacing_RisingTensionBeat_ReturnsTighten()
    {
        // Beat 6 of 12 = position 0.54 — inside 0.4..0.7 Tighten zone
        var result = PacingService.GetPacing(6, 12);
        Assert.That(result.Mode, Is.EqualTo(PacingService.PaceMode.Tighten));
    }

    [Test]
    public void GetPacing_ClimaxBeat_ReturnsStrike()
    {
        // Beat 9 of 12 = position 0.818 — inside 0.7..0.85 Strike zone
        var result = PacingService.GetPacing(9, 12);
        Assert.That(result.Mode, Is.EqualTo(PacingService.PaceMode.Strike));
    }

    [Test]
    public void GetPacing_LastBeat_ReturnsSettle()
    {
        // Beat 11 of 12 = position 1.0 — >= 0.85 Settle zone
        var result = PacingService.GetPacing(11, 12);
        Assert.That(result.Mode, Is.EqualTo(PacingService.PaceMode.Settle));
    }

    [Test]
    public void GetPacing_SingleBeat_ReturnsBreathe()
    {
        // totalBeats=1 means position=0 → Breathe
        var result = PacingService.GetPacing(0, 1);
        Assert.That(result.Mode, Is.EqualTo(PacingService.PaceMode.Breathe));
    }

    // ── Keyword overrides ────────────────────────────────────────────────────

    [TestCase("Kyle chases the runner through the market", PacingService.PaceMode.Strike)]
    [TestCase("fight breaks out in the lobby", PacingService.PaceMode.Strike)]
    [TestCase("escape through the maintenance tunnel", PacingService.PaceMode.Strike)]
    [TestCase("attack before they can signal", PacingService.PaceMode.Strike)]
    public void GetPacing_FightChaseEscapeAttack_ReturnsStrike(string goal, PacingService.PaceMode expected)
    {
        // Use mid-node position that would default to Tighten, to prove override fires
        var result = PacingService.GetPacing(5, 12, goal);
        Assert.That(result.Mode, Is.EqualTo(expected));
    }

    [TestCase("discover the hidden lab", PacingService.PaceMode.Breathe)]
    [TestCase("arrive at the safehouse", PacingService.PaceMode.Breathe)]
    [TestCase("enter the Spine through the side corridor", PacingService.PaceMode.Breathe)]
    [TestCase("explore the derelict tower", PacingService.PaceMode.Breathe)]
    public void GetPacing_DiscoverArriveEnterExplore_ReturnsBreathe(string goal, PacingService.PaceMode expected)
    {
        var result = PacingService.GetPacing(8, 12, goal);  // position would default to Strike
        Assert.That(result.Mode, Is.EqualTo(expected));
    }

    [TestCase("confront Sable in the office", PacingService.PaceMode.Tighten)]
    [TestCase("tension crests as they both reach for the door", PacingService.PaceMode.Tighten)]
    [TestCase("threaten the contact with exposure", PacingService.PaceMode.Tighten)]
    public void GetPacing_ConfrontTensionThreaten_ReturnsTighten(string goal, PacingService.PaceMode expected)
    {
        var result = PacingService.GetPacing(1, 12, goal);  // position would default to Breathe
        Assert.That(result.Mode, Is.EqualTo(expected));
    }

    [TestCase("aftermath of the bombing", PacingService.PaceMode.Settle)]
    [TestCase("grief sits in the kitchen air", PacingService.PaceMode.Settle)]
    [TestCase("reflect on what was lost", PacingService.PaceMode.Settle)]
    [TestCase("cost of the job comes due", PacingService.PaceMode.Settle)]
    public void GetPacing_AftermathGriefReflectCost_ReturnsSettle(string goal, PacingService.PaceMode expected)
    {
        var result = PacingService.GetPacing(1, 12, goal);
        Assert.That(result.Mode, Is.EqualTo(expected));
    }

    [Test]
    public void GetPacing_NullGoal_UsesPositionalArc()
    {
        var result = PacingService.GetPacing(0, 12, null);
        Assert.That(result.Mode, Is.EqualTo(PacingService.PaceMode.Breathe));
    }

    // ── ProseGuidance content ────────────────────────────────────────────────

    [Test]
    public void ProseGuidance_Breathe_ContainsSensoryKeyword()
    {
        var inst = PacingService.GetPacing(0, 12);
        Assert.That(inst.ProseGuidance, Does.Contain("BREATHE").Or.Contain("sensory"));
    }

    [Test]
    public void ProseGuidance_Flow_ContainsDialogueKeyword()
    {
        var inst = PacingService.GetPacing(3, 12);
        Assert.That(inst.ProseGuidance, Does.Contain("FLOW").Or.Contain("dialogue").Or.Contain("Dialogue"));
    }

    [Test]
    public void ProseGuidance_Tighten_ContainsTensionKeyword()
    {
        var inst = PacingService.GetPacing(6, 12);
        Assert.That(inst.ProseGuidance, Does.Contain("TIGHTEN").Or.Contain("Tension"));
    }

    [Test]
    public void ProseGuidance_Strike_ContainsActionKeyword()
    {
        var inst = PacingService.GetPacing(9, 12);
        Assert.That(inst.ProseGuidance, Does.Contain("STRIKE").Or.Contain("Action").Or.Contain("action"));
    }

    [Test]
    public void ProseGuidance_Settle_ContainsAftermathKeyword()
    {
        var inst = PacingService.GetPacing(11, 12);
        Assert.That(inst.ProseGuidance, Does.Contain("SETTLE").Or.Contain("aftermath").Or.Contain("Aftermath"));
    }

    [Test]
    public void ProseGuidance_NeverNull()
    {
        for (int i = 0; i < 12; i++)
        {
            var inst = PacingService.GetPacing(i, 12);
            Assert.That(inst.ProseGuidance, Is.Not.Null, $"Beat {i}/12 returned null ProseGuidance");
        }
    }
}
