using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

[TestFixture]
public class TensionEscalationServiceTests
{
    private TensionEscalationService svc = null!;
    private static readonly Guid strandA = Guid.NewGuid();

    [SetUp]
    public void SetUp() => svc = new TensionEscalationService();

    // ── RecordBeat ───────────────────────────────────────────────────────────

    [Test]
    public void RecordBeat_EmptyGuid_IsIgnored()
    {
        svc.RecordBeat(Guid.Empty, BeatMode.Narrative);
        var modes = svc.GetRecentModes(Guid.Empty);
        Assert.That(modes, Is.Empty);
    }

    [Test]
    public void RecordBeat_StoresMode()
    {
        svc.RecordBeat(strandA, BeatMode.Combat);
        var modes = svc.GetRecentModes(strandA);
        Assert.That(modes, Has.Count.EqualTo(1));
        Assert.That(modes[0], Is.EqualTo(BeatMode.Combat));
    }

    [Test]
    public void RecordBeat_WindowCapsAtFive()
    {
        for (int i = 0; i < 8; i++)
            svc.RecordBeat(strandA, BeatMode.Narrative);

        var modes = svc.GetRecentModes(strandA);
        Assert.That(modes, Has.Count.EqualTo(5));
    }

    [Test]
    public void RecordBeat_OldestDropsWhenOverFive()
    {
        svc.RecordBeat(strandA, BeatMode.Combat);      // this should fall off
        for (int i = 0; i < 5; i++)
            svc.RecordBeat(strandA, BeatMode.Narrative);

        var modes = svc.GetRecentModes(strandA);
        Assert.That(modes, Has.Count.EqualTo(5));
        Assert.That(modes, Has.None.EqualTo(BeatMode.Combat), "Combat should have scrolled out of the window");
    }

    [Test]
    public void RecordBeat_TwoStrands_AreIndependent()
    {
        var strandB = Guid.NewGuid();
        svc.RecordBeat(strandA, BeatMode.Combat);
        svc.RecordBeat(strandB, BeatMode.Dialogue);

        Assert.That(svc.GetRecentModes(strandA)[0], Is.EqualTo(BeatMode.Combat));
        Assert.That(svc.GetRecentModes(strandB)[0], Is.EqualTo(BeatMode.Dialogue));
    }

    // ── Reset ────────────────────────────────────────────────────────────────

    [Test]
    public void Reset_ClearsHistory()
    {
        svc.RecordBeat(strandA, BeatMode.Narrative);
        svc.Reset(strandA);
        Assert.That(svc.GetRecentModes(strandA), Is.Empty);
    }

    // ── BuildGuidanceBlock: early exits ─────────────────────────────────────

    [Test]
    public void BuildGuidanceBlock_EmptyGuid_ReturnsEmpty()
    {
        var result = svc.BuildGuidanceBlock(Guid.Empty, BeatMode.Narrative);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void BuildGuidanceBlock_EscalatingIncomingMode_ReturnsEmpty()
    {
        // Fill with non-escalating beats to trigger stagnation
        for (int i = 0; i < 4; i++)
            svc.RecordBeat(strandA, BeatMode.Narrative);

        // But the incoming beat is already escalating — no warning needed
        var result = svc.BuildGuidanceBlock(strandA, BeatMode.Combat);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void BuildGuidanceBlock_EmotionalClimaxIncoming_ReturnsEmpty()
    {
        for (int i = 0; i < 4; i++)
            svc.RecordBeat(strandA, BeatMode.Narrative);

        var result = svc.BuildGuidanceBlock(strandA, BeatMode.EmotionalClimax);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void BuildGuidanceBlock_RevelationIncoming_ReturnsEmpty()
    {
        for (int i = 0; i < 4; i++)
            svc.RecordBeat(strandA, BeatMode.Narrative);

        var result = svc.BuildGuidanceBlock(strandA, BeatMode.Revelation);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void BuildGuidanceBlock_UnknownStrand_ReturnsEmpty()
    {
        // No beats recorded for this strand
        var result = svc.BuildGuidanceBlock(Guid.NewGuid(), BeatMode.Narrative);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void BuildGuidanceBlock_BelowStagnationThreshold_ReturnsEmpty()
    {
        // Only 2 non-escalating beats — below stagnation threshold of 3
        svc.RecordBeat(strandA, BeatMode.Narrative);
        svc.RecordBeat(strandA, BeatMode.Dialogue);

        var result = svc.BuildGuidanceBlock(strandA, BeatMode.Narrative);
        Assert.That(result, Is.Empty);
    }

    // ── BuildGuidanceBlock: stagnation detection ─────────────────────────────

    [Test]
    public void BuildGuidanceBlock_ThreeConsecutiveNonEscalating_ReturnsGuidance()
    {
        svc.RecordBeat(strandA, BeatMode.Narrative);
        svc.RecordBeat(strandA, BeatMode.Dialogue);
        svc.RecordBeat(strandA, BeatMode.Transition);

        var result = svc.BuildGuidanceBlock(strandA, BeatMode.Narrative);
        Assert.That(result, Is.Not.Empty);
        Assert.That(result, Does.Contain("TENSION ESCALATION"));
    }

    [Test]
    public void BuildGuidanceBlock_ContainsBeatCount()
    {
        svc.RecordBeat(strandA, BeatMode.Narrative);
        svc.RecordBeat(strandA, BeatMode.Narrative);
        svc.RecordBeat(strandA, BeatMode.Narrative);
        svc.RecordBeat(strandA, BeatMode.Narrative);

        var result = svc.BuildGuidanceBlock(strandA, BeatMode.Dialogue);
        Assert.That(result, Does.Contain("4"), "Guidance should mention the stagnation count");
    }

    [Test]
    public void BuildGuidanceBlock_MajorityEscalatingInWindow_ReturnsEmpty()
    {
        // Two non-escalating beats + two escalating beats = nonEscalatingCount is 2, below threshold of 3
        svc.RecordBeat(strandA, BeatMode.Narrative);
        svc.RecordBeat(strandA, BeatMode.Narrative);
        svc.RecordBeat(strandA, BeatMode.Combat);
        svc.RecordBeat(strandA, BeatMode.Combat);

        var result = svc.BuildGuidanceBlock(strandA, BeatMode.Narrative);
        // nonEscalatingCount = 2 < stagnation threshold (3) → no guidance
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void BuildGuidanceBlock_GuidanceMentionsStakeRaising()
    {
        for (int i = 0; i < 3; i++)
            svc.RecordBeat(strandA, BeatMode.Narrative);

        var result = svc.BuildGuidanceBlock(strandA, BeatMode.Dialogue);
        Assert.That(result, Does.Contain("stakes").Or.Contain("TENSION"));
    }
}
