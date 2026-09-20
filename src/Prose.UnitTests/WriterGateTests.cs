using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// RFC 0012 §3 — the brief, the post-processor, the deterministic gate, and the verifier's
/// JSON contract. Pinned against the concrete failure that motivated them (beat #17292,
/// 2026-09-07): a `# Beat:` heading saved verbatim, and a draft that ran past its stop line into
/// an invented plot event with no instrument noticing.
/// </summary>
[TestFixture]
public class WriterGateTests
{
    private static BeatBrief Brief(string? stop = "Rook boards the Pulse.", params string[] must) => new()
    {
        Goal = "Rook walks the Spine to the Pulse station in flat morning light.",
        StopBefore = stop,
        MustInclude = must,
        Pov = "Rook",
    };

    // ── BeatBrief ────────────────────────────────────────────────────────────

    [Test]
    public void Brief_PromptBlock_LeadsWithGoal_AndCarriesStop_Names_Pov_Rules()
    {
        var b = Brief("Rook boards the Pulse.", "Rook", "Lake Platform");
        var block = b.ToPromptBlock();
        Assert.That(block, Does.StartWith("THE BRIEF"));
        Assert.That(block, Does.Contain("WHAT HAPPENS: Rook walks the Spine"));
        Assert.That(block, Does.Contain("STOP BEFORE: Rook boards the Pulse."));
        Assert.That(block, Does.Contain("MUST APPEAR (by name): Rook, Lake Platform"));
        Assert.That(block, Does.Contain("POV: Rook"));
        Assert.That(block, Does.Contain("NO NEW PLOT"));
        Assert.That(block, Does.Contain("OUTPUT: prose only"));
        Assert.That(b.ToClosingLine(), Does.Contain("Stop before: Rook boards the Pulse."));
    }

    [Test]
    public void Brief_LastBeatOfBook_SaysEndTheBook_ChapterClose_SaysLandTheChapter()
    {
        var last = Brief(stop: null);
        Assert.That(last.ToPromptBlock(), Does.Contain("last beat of the book"));
        var close = last with { ClosesChapter = true };
        Assert.That(close.ToPromptBlock(), Does.Contain("closes its chapter"));
        Assert.That(close.ToClosingLine(), Does.Contain("Land the chapter"));
    }

    [Test]
    public void Brief_WithConstraints_AppendsFailuresAsFixList_AndSkipsBlanks()
    {
        var b = Brief().WithConstraints(["Remove the ArcSec sedan.", "", "  "]);
        Assert.That(b.Constraints, Has.Count.EqualTo(1));
        Assert.That(b.ToPromptBlock(), Does.Contain("THE FIRST ATTEMPT FAILED THE GATE"));
        Assert.That(b.ToPromptBlock(), Does.Contain("- Remove the ArcSec sedan."));
    }

    // ── DraftPostProcessor ───────────────────────────────────────────────────

    [Test]
    public void PostProcessor_StripsLeadingMarkdownHeading_TheBeat17292Case()
    {
        var r = DraftPostProcessor.Clean("# Beat: The Spine at Morning\n\nThe Spine ran north-south along the lakeshore.");
        Assert.That(r.Text, Does.StartWith("The Spine ran"));
        Assert.That(r.Removed, Has.Count.EqualTo(1));
        Assert.That(r.Removed[0], Does.Contain("leading line"));
    }

    [Test]
    public void PostProcessor_StripsLabelLine_AndTrailingNote_LeavesProseIntact()
    {
        var r = DraftPostProcessor.Clean("Beat 12 — The Dock\nShe stepped aboard. The lake was flat.\n\n(Word count: 8)");
        Assert.That(r.Text, Is.EqualTo("She stepped aboard. The lake was flat."));
        Assert.That(r.Removed, Has.Count.EqualTo(2));
    }

    [Test]
    public void PostProcessor_DoesNotTouchAHeadingInsideTheProse_OrAScenBreakFollowedByProse()
    {
        var prose = "She stepped aboard.\n\n---\n\nThree miles of black water. November doing what November did. The platform's lights held in the rain ahead, steady, unaware, and she counted the seconds between swells the way she counted anything she could not control, which was most things tonight, and the count came out even.";
        var r = DraftPostProcessor.Clean(prose);
        Assert.That(r.Changed, Is.False);
        Assert.That(r.Text, Is.EqualTo(prose));
    }

    [Test]
    public void PostProcessor_StripsCommentaryAfterARule_ButKeepsProseAfterARule()
    {
        var withMeta = "She stepped aboard. The lake was flat.\n\n---\n\nNote: I kept this beat short as the brief asked.";
        var r = DraftPostProcessor.Clean(withMeta);
        Assert.That(r.Text, Is.EqualTo("She stepped aboard. The lake was flat."));
        Assert.That(r.Removed.Single(), Does.Contain("trailing block after rule"));
    }

    [Test]
    public void PostProcessor_EmptyOrWhitespace_IsEmpty()
    {
        Assert.That(DraftPostProcessor.Clean(null).Text, Is.EqualTo(""));
        Assert.That(DraftPostProcessor.Clean("   \n ").Text, Is.EqualTo(""));
    }

    // ── DraftGate ────────────────────────────────────────────────────────────

    private static string Words(int n, string seed = "the lake was flat and gray") =>
        string.Join(' ', Enumerable.Range(0, n).Select(i => seed.Split(' ')[i % 6]));

    [Test]
    public void Gate_EmptyDraft_Fails()
    {
        var r = DraftGate.Check("", Brief());
        Assert.That(r.Passed, Is.False);
        Assert.That(r.Failures[0], Does.Contain("empty"));
    }

    [Test]
    public void Gate_TooShortForADramaticUnit_Fails_UnderSwainDefault()
    {
        var r = DraftGate.Check("Rook walked. " + Words(40), Brief());
        Assert.That(r.Passed, Is.False);
        Assert.That(r.Failures.Single(), Does.Contain("Too short"));
    }

    [Test]
    public void Gate_TargetWordsBand_HalfToOnePointSix()
    {
        var b = Brief() with { TargetWords = 200 };
        Assert.That(DraftGate.Check("Rook " + Words(90), b).Passed, Is.False, "below 0.5×");
        Assert.That(DraftGate.Check("Rook " + Words(200), b).Passed, Is.True);
        Assert.That(DraftGate.Check("Rook " + Words(330), b).Passed, Is.False, "above 1.6×");
    }

    [Test]
    public void Gate_MustInclude_FullNameOrAnyTokenOf3PlusLetters_Satisfies()
    {
        var b = Brief("stop", "Kyle Corbin", "Mrs. Chen");
        var draft = "Kyle put the kettle on. " + Words(150) + " Chen did not look up.";
        var r = DraftGate.Check(draft, b);
        Assert.That(r.Passed, Is.True, string.Join("|", r.Failures));
    }

    [Test]
    public void Gate_MustInclude_MissingName_Fails_NamingTheName()
    {
        var b = Brief("stop", "Boiler");
        var r = DraftGate.Check("Rook walked. " + Words(150), b);
        Assert.That(r.Passed, Is.False);
        Assert.That(r.Failures.Single(), Does.Contain("\"Boiler\""));
    }

    [Test]
    public void Gate_ResidualHeading_Fails()
    {
        var r = DraftGate.Check("# Still a heading\n" + Words(150), Brief());
        Assert.That(r.Failures.Any(f => f.Contains("markdown heading")), Is.True);
    }

    [Test]
    public void Gate_UnknownCapitalisedWords_AreWarningsNotFailures()
    {
        var draft = "Rook crossed the walkway, and Steadfast Medical crews were already at the seam. " + Words(150);
        var r = DraftGate.Check(draft, Brief(), knownEntityNames: ["Rook", "Lake Platform"]);
        Assert.That(r.Passed, Is.True);
        Assert.That(r.Warnings.Single(), Does.Contain("Steadfast Medical"));
    }

    // ── BriefVerifier.Parse ──────────────────────────────────────────────────

    [Test]
    public void Verifier_Parse_ReadsReasoningFirst_AndAllThreeAnswers()
    {
        var json = """
            Here is my assessment:
            {"reasoning":"The draft follows Rook along the Spine, then a sedan arrives and she is taken away, which the brief does not imply and which crosses into territory past the stop.",
             "crosses_stop": true, "stop_evidence": "The window came down.",
             "added_events": ["ArcSec sedan pulls up and Rook gets in"],
             "contradictions": [], "missing_from_goal": ["stepping onto the Pulse platform"]}
            """;
        var v = BriefVerifier.Parse(json);
        Assert.That(v.Reasoning, Does.StartWith("The draft follows"));
        Assert.That(v.CrossesStop, Is.True);
        Assert.That(v.StopEvidence, Is.EqualTo("The window came down."));
        Assert.That(v.AddedEvents, Is.EqualTo(new[] { "ArcSec sedan pulls up and Rook gets in" }));
        Assert.That(v.Passed, Is.False);
        var c = v.AsConstraints();
        Assert.That(c.Count, Is.EqualTo(3));
        Assert.That(c[0], Does.Contain("Stop earlier"));
        Assert.That(c[1], Does.Contain("Remove this event"));
        Assert.That(c[2], Does.Contain("requires this and the draft lacks it"));
    }

    [Test]
    public void Verifier_Parse_CleanPass()
    {
        var v = BriefVerifier.Parse("""{"reasoning":"ok","crosses_stop":false,"stop_evidence":null,"added_events":[],"contradictions":[],"missing_from_goal":[]}""");
        Assert.That(v.Passed, Is.True);
        Assert.That(v.AsConstraints(), Is.Empty);
    }

    [Test]
    public void Verifier_Parse_ThrowsOnEmpty_AndOnNoObject_NeverFailsOpen()
    {
        Assert.Throws<InvalidOperationException>(() => BriefVerifier.Parse(""));
        Assert.Throws<InvalidOperationException>(() => BriefVerifier.Parse("I could not evaluate this."));
        Assert.Throws<InvalidOperationException>(() => BriefVerifier.Parse("{not even closed"));
        Assert.Catch<System.Text.Json.JsonException>(() => BriefVerifier.Parse("""{"reasoning": , "crosses_stop": }"""));
    }
}
