using Prose.Core.Services;

namespace Prose.UnitTests;

[TestFixture]
public class BeatModeDetectorTests
{
    // BeatModeDetector.Detect is pure — no DB needed. PersistAsync requires DB, skip it.
    private static (BeatMode Mode, float Confidence, string Method) Detect(string? goal, string? hint = null)
    {
        // Construct with null! — Detect() is pure and never touches dbFactory
        var detector = new BeatModeDetector(null!);
        return detector.Detect(goal, hint);
    }

    // ── Null / empty → default ───────────────────────────────────────────────

    [Test]
    public void Detect_NullGoal_ReturnsNarrativeDefault()
    {
        var (mode, confidence, method) = Detect(null);
        Assert.That(mode, Is.EqualTo(BeatMode.Narrative));
        Assert.That(method, Is.EqualTo("default"));
        Assert.That(confidence, Is.EqualTo(0.5f));
    }

    [Test]
    public void Detect_EmptyGoal_ReturnsNarrativeDefault()
    {
        var (mode, _, method) = Detect("   ");
        Assert.That(mode, Is.EqualTo(BeatMode.Narrative));
        Assert.That(method, Is.EqualTo("default"));
    }

    // ── Combat keywords ──────────────────────────────────────────────────────

    [TestCase("fight breaks out in the hallway")]
    [TestCase("blade to blade — Kyle kills the runner")]
    [TestCase("gun comes up before she can scream")]
    [TestCase("standoff in the parking structure")]
    [TestCase("firefight at the checkpoint")]
    [TestCase("brawl spills into the street")]
    [TestCase("ambush on the Spine walkway")]
    [TestCase("shoot and evade through the market")]
    [TestCase("stab wound slows him down")]
    [TestCase("chase through the maintenance corridors")]
    public void Detect_CombatKeyword_ReturnsCombat(string goal)
    {
        var (mode, confidence, method) = Detect(goal);
        Assert.That(mode, Is.EqualTo(BeatMode.Combat));
        Assert.That(confidence, Is.EqualTo(0.85f));
        Assert.That(method, Is.EqualTo("keyword"));
    }

    // ── EmotionalClimax keywords ─────────────────────────────────────────────

    [TestCase("grief settles in like sediment")]
    [TestCase("loss of the only person who knew")]
    [TestCase("mourning what was never said")]
    [TestCase("she breaks down in the corridor")]
    [TestCase("he confesses what he did in the Pulse station")]
    [TestCase("confession extracted at last")]
    [TestCase("face the cost of what happened")]
    [TestCase("she weeps — first time he has seen it")]
    public void Detect_EmotionalKeyword_ReturnsEmotionalClimax(string goal)
    {
        var (mode, confidence, method) = Detect(goal);
        Assert.That(mode, Is.EqualTo(BeatMode.EmotionalClimax));
        Assert.That(confidence, Is.EqualTo(0.80f));
        Assert.That(method, Is.EqualTo("keyword"));
    }

    // ── Dialogue keywords ────────────────────────────────────────────────────

    [TestCase("negotiation over the payment terms")]
    [TestCase("interrogation of the witness in the back room")]
    [TestCase("interview the contact before the drop")]
    [TestCase("debrief after the job goes sideways")]
    [TestCase("argument about whether to run")]
    [TestCase("confrontation in the elevator")]
    [TestCase("meeting at the neutral ground café")]
    [TestCase("she convinces him the trail is cold")]
    [TestCase("he persuades the council to wait")]
    public void Detect_DialogueKeyword_ReturnsDialogue(string goal)
    {
        var (mode, confidence, method) = Detect(goal);
        Assert.That(mode, Is.EqualTo(BeatMode.Dialogue));
        Assert.That(confidence, Is.EqualTo(0.75f));
        Assert.That(method, Is.EqualTo("keyword"));
    }

    // ── Transition keywords ──────────────────────────────────────────────────

    [TestCase("travels to the drop point on the Pulse")]
    [TestCase("commutes across the city before dawn")]
    [TestCase("arrives at the zone boundary checkpoint")]
    [TestCase("departs before anyone notices")]
    [TestCase("moves through the crowd towards the Spine")]
    [TestCase("en route to the extraction point")]
    public void Detect_TransitionKeyword_ReturnsTransition(string goal)
    {
        var (mode, confidence, method) = Detect(goal);
        Assert.That(mode, Is.EqualTo(BeatMode.Transition));
        Assert.That(confidence, Is.EqualTo(0.70f));
        Assert.That(method, Is.EqualTo("keyword"));
    }

    // ── Revelation keywords ──────────────────────────────────────────────────

    [TestCase("discovers the second body in the machine room")]
    [TestCase("realizes the client has been lying")]
    [TestCase("learns who authorized the hit")]
    [TestCase("uncovers the second account in the ledger")]
    [TestCase("decodes the signal — it is not random")]
    [TestCase("solves the relay sequence")]
    [TestCase("pieces together what actually happened")]
    public void Detect_RevelationKeyword_ReturnsRevelation(string goal)
    {
        var (mode, confidence, method) = Detect(goal);
        Assert.That(mode, Is.EqualTo(BeatMode.Revelation));
        Assert.That(confidence, Is.EqualTo(0.70f));
        Assert.That(method, Is.EqualTo("keyword"));
    }

    // ── Narrative default ────────────────────────────────────────────────────

    [Test]
    public void Detect_NoMatchingKeyword_ReturnsNarrative()
    {
        var (mode, confidence, method) = Detect("Kyle waits in the apartment watching the rain");
        Assert.That(mode, Is.EqualTo(BeatMode.Narrative));
        Assert.That(confidence, Is.EqualTo(0.5f));
        Assert.That(method, Is.EqualTo("default"));
    }

    // ── ProseHint incorporation ───────────────────────────────────────────────

    [Test]
    public void Detect_CombatInProseHint_UpgradesToCombat()
    {
        // Neutral goal but prose hint mentions "fight"
        var (mode, _, method) = Detect("something happens", "the fight spills into the corridor");
        Assert.That(mode, Is.EqualTo(BeatMode.Combat));
        Assert.That(method, Is.EqualTo("keyword"));
    }

    [Test]
    public void Detect_ProseHintOver500Chars_IsIgnored()
    {
        // Hint longer than 500 chars is not appended — goal alone decides
        var longHint = new string('x', 501) + " fight brawl shoot";
        var (mode, _, _) = Detect("Kyle waits quietly", longHint);
        Assert.That(mode, Is.EqualTo(BeatMode.Narrative));
    }

    [Test]
    public void Detect_ProseHint500CharsExact_IsIncluded()
    {
        // Exactly 500 chars is included (condition: length < 500 is false for 500, so NOT included)
        // Per source: proseHint.Length < 500 → exactly 500 is excluded
        var hint500 = new string('a', 500);
        var (mode, _, _) = Detect("neutral goal", hint500 + "fight");
        // The hint is 500 chars, condition is < 500 → not appended; goal has no keyword
        Assert.That(mode, Is.EqualTo(BeatMode.Narrative));
    }

    // ── Priority ordering (Combat wins over EmotionalClimax when both present) ─

    [Test]
    public void Detect_CombatAndEmotional_CombatWins()
    {
        // Both keywords in goal — Combat check runs first
        var (mode, _, _) = Detect("fight through the grief of killing him");
        Assert.That(mode, Is.EqualTo(BeatMode.Combat));
    }

    [Test]
    public void Detect_EmotionalAndDialogue_EmotionalWins()
    {
        // Emotional check runs before Dialogue check
        var (mode, _, _) = Detect("grief in the confrontation with the council");
        Assert.That(mode, Is.EqualTo(BeatMode.EmotionalClimax));
    }

    // ── Case insensitivity ───────────────────────────────────────────────────

    [Test]
    public void Detect_UpperCaseKeyword_StillMatches()
    {
        var (mode, _, _) = Detect("FIGHT in the upper corridor");
        Assert.That(mode, Is.EqualTo(BeatMode.Combat));
    }
}
