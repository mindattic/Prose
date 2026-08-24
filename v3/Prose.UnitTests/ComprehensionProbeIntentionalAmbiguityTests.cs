using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Cover for <see cref="ComprehensionProbeService.DemoteSelfDeclaredIntentional"/> (2026-08-24).
///
/// <para>The arbiter's system prompt already says "Deliberately open mysteries the text itself
/// marks as unresolved … are the text working as intended — reject, do not confirm" — but nothing
/// verified it, and the BCODA run that day filed several ComprehensionDefect findings arguing in
/// their own evidence that they were not defects. Same failure mode as
/// <c>LogicSweepService.IsSelfDeclaredNonFinding</c>, and the same reason it matters: a defect
/// whose evidence reads "this is the text working as intended" discredits every other finding in
/// the report.</para>
///
/// <para>Every string below is verbatim from that run, so this fixture doubles as the record of
/// what the guard was calibrated against.</para>
/// </summary>
[TestFixture]
public class ComprehensionProbeIntentionalAmbiguityTests
{
    private static ComprehensionProbeService.ProbeDefect Defect(
        string description, string kind = "confusion", string severity = "minor", bool plausible = true)
        => new(kind, description, Evidence: "", Severity: severity, ReaderPlausible: plausible);

    private static ComprehensionProbeService.ProbeDefect Only(string description)
        => ComprehensionProbeService.DemoteSelfDeclaredIntentional([Defect(description)])[0];

    // ── real self-refuting findings from the 2026-08-24 BCODA run ─────────────

    [TestCase("Reader cannot determine Able's institutional role/authority beyond board employee vs. higher power; "
            + "the text deliberately withholds this, which is an intentional mystery the text marks as such rather "
            + "than a clear fact left out.")]
    [TestCase("Nature of Kyle and Pixel's relationship (romantic vs protective vs other) is left deliberately unstated, "
            + "causing reader uncertainty.")]
    [TestCase("Ambiguity over what Mrs. Chen means by 'whether it would change how you handled it' is explicitly left "
            + "open by the text itself.")]
    [TestCase("Reader is confused about what 'COMPENSATION IN SUBSEQUENT POSITIONS: OBSERVABLE' means — the text leaves "
            + "this deliberately cryptic corporate-speak with no clarification, so ambiguity is inherent to the text's "
            + "style rather than a comprehension failure.")]
    public void SelfDeclaredIntentionalAmbiguity_IsDemoted(string description)
    {
        var result = Only(description);

        Assert.That(result.ReaderPlausible, Is.False, "a defect whose own evidence calls the text intentional must not be filed");
        Assert.That(result.Kind, Is.EqualTo(ComprehensionProbeService.IntentionalAmbiguityKind));
    }

    // ── VIGL, same day: the forms a literal phrase list kept missing ──────────

    [TestCase("Reader is unsure of the strategic link between suspending Lyra's field authority and staging the war "
            + "construct; text deliberately leaves the operation's ultimate purpose unstated (Vega herself doesn't "
            + "know), so the reader's uncertainty mirrors the text's own withheld information.")]
    [TestCase("The final paragraph about telling the story 'in pieces, to someone he had not yet met, in a room with no "
            + "coal dust' is an intentionally cryptic foreshadowing device with no explanation given in the chapter.")]
    [TestCase("The nature of the entity in the crater's womb-chamber is deliberately left ambiguous by the text itself "
            + "(creature vs. growth vs. construction), so a reader's uncertainty here reflects the text's intentional "
            + "withholding rather than a comprehension failure.")]
    public void IntentWordNextToAWithholdingWord_IsDemoted(string description)
    {
        var result = Only(description);

        Assert.That(result.ReaderPlausible, Is.False);
        Assert.That(result.Kind, Is.EqualTo(ComprehensionProbeService.IntentionalAmbiguityKind));
    }

    [TestCase("Reader is unsure what 'doctrinal threat' means and why testimony constitutes one; the text does explain "
            + "this fairly directly, so this confusion is largely unwarranted.")]
    [TestCase("Reader is confused about why the checkpoint officer lets Lyra proceed despite the suspension; text is "
            + "actually clear that the suspension restricts field authority, not physical passage.")]
    public void ArbiterSelfRebuttal_IsDemoted(string description)
    {
        // The arbiter confirmed a defect while arguing in the same breath that the reader was
        // simply wrong. That is a hallucination verdict wearing a defect's clothes.
        Assert.That(Only(description).ReaderPlausible, Is.False);
    }

    [TestCase("The chapter interleaves a present-tense driving scene with a two-years-earlier underground mining memory "
            + "without a clear typographic or verbal transition cue, making the two scenarios easy to conflate.")]
    [TestCase("The chapter never explains how Lyra's group knows to pursue Wren's group to Sal Vento or why they want "
            + "the case, leaving the link between the two storylines unstated on the page.")]
    [TestCase("Reader is confused about whose 'sister' is referenced in the final line and what was sent ahead — this is "
            + "a genuinely oblique, unexplained closing line with no antecedent established anywhere earlier.")]
    public void VigilsEndModerates_Survive(string description)
    {
        // Note the second case: "leaving … unstated" is withholding language with NO intent word,
        // which is exactly the distinction the pairing regex draws. A chapter that fails to
        // explain something is a defect; a chapter that withholds it on purpose is craft.
        var result = Only(description);

        Assert.That(result.ReaderPlausible, Is.True);
        Assert.That(result.Kind, Is.EqualTo("confusion"));
    }

    // ── the MODERATE findings from the same run: all must survive ─────────────

    [TestCase("Reader's summary omits the unseen catwalk shooter as a distinct sixth antagonist separate from the boss, "
            + "losing the wounding-and-departure beat.")]
    [TestCase("Reader is unable to determine how/when the printer's calibration head was damaged (at Kessler's yard vs. "
            + "in transit), since the text never shows the damage occurring on-page.")]
    [TestCase("Reader claims nine configurations died 'when their safety cutoffs fired' as if the cutoff itself was the "
            + "killer, but text specifies ten configurations reached the aperture and none survived it.")]
    public void RealDefects_AreLeftAlone(string description)
    {
        var result = Only(description);

        Assert.That(result.ReaderPlausible, Is.True);
        Assert.That(result.Kind, Is.EqualTo("confusion"));
    }

    [Test]
    public void GenuineWording_IsNotTreatedAsAnIntentAdmission()
    {
        // The arbiter uses "genuine"/"genuinely" to mean "the text really does under-establish
        // this" — a CONFIRMATION. Matching it would suppress real findings, so no phrase in the
        // guard may key on it. Both of these are real confirmed defects from the BCODA run.
        var a = Only("Text never explicitly states whether the fourteen operatives are dead or unconscious, "
                   + "leaving their fate genuinely ambiguous.");
        var b = Only("The meaning of 'cross-reference: contract 0047-C-WEST' is never explained anywhere in the text, "
                   + "making it a genuinely unresolvable oblique detail for the reader.");

        Assert.That(a.ReaderPlausible, Is.True);
        Assert.That(b.ReaderPlausible, Is.True);
    }

    [Test]
    public void EvidenceField_IsScannedToo_NotJustDescription()
    {
        var defect = new ComprehensionProbeService.ProbeDefect(
            "confusion", "Reader cannot follow who the client is.",
            Evidence: "the chapter deliberately withholds the client's identity",
            Severity: "moderate", ReaderPlausible: true);

        var result = ComprehensionProbeService.DemoteSelfDeclaredIntentional([defect])[0];

        Assert.That(result.ReaderPlausible, Is.False);
    }

    [Test]
    public void AlreadyRejectedEntries_PassThroughUntouched()
    {
        // A hallucination the arbiter already threw out, and a readerPlausible=false entry, must
        // keep their own kind — the demotion is only ever applied to CONFIRMED defects, so the
        // report's hallucination count stays honest.
        var input = new[]
        {
            Defect("reader invented a character who is plainly named on the page", kind: "hallucination", plausible: false),
            Defect("the text is clear here; this is working as intended", plausible: false),
        };

        var result = ComprehensionProbeService.DemoteSelfDeclaredIntentional(input);

        Assert.That(result[0].Kind, Is.EqualTo("hallucination"));
        Assert.That(result[1].Kind, Is.EqualTo("confusion"),
            "an entry the arbiter already rejected must not be relabelled as intentional ambiguity");
    }

    [Test]
    public void OrderAndCount_ArePreserved()
    {
        var input = new[]
        {
            Defect("a real defect the text under-establishes"),
            Defect("this is left deliberately unstated"),
            Defect("another real defect"),
        };

        var result = ComprehensionProbeService.DemoteSelfDeclaredIntentional(input);

        Assert.That(result, Has.Count.EqualTo(3), "demotion must never drop entries — the cache row records all three");
        Assert.That(result[0].ReaderPlausible, Is.True);
        Assert.That(result[1].ReaderPlausible, Is.False);
        Assert.That(result[2].ReaderPlausible, Is.True);
    }
}
