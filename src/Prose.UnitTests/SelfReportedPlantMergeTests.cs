using Prose.Core.Composition.Obligations;

namespace Prose.UnitTests;

/// <summary>
/// The merge step that makes a self-report count comparable to an answer key.
///
/// <para>A plant introduced once gets re-reported by every later beat that touches it, so the raw
/// per-beat total is a false-positive count plus an unknown amount of repetition. Every calibration
/// number taken before this existed (GCTOC 53, then 34 after the prompt was tightened) was a raw
/// count measured against a key of DISTINCT plants — not a comparison.</para>
///
/// <para>These tests guard BOTH directions, because the two errors are not symmetric. Failing to
/// merge leaves the count pessimistic, which is safe. Merging two distinct plants makes the
/// instrument look better than it is, on the exact number being used to judge it — so the
/// "different plants stay separate" cases matter more than the ones that merge.</para>
///
/// <para>No LLM: this is string comparison, and a fake model would only obscure that.</para>
/// </summary>
[TestFixture]
public class SelfReportedPlantMergeTests
{
    private static readonly Guid B1 = Guid.CreateVersion7();
    private static readonly Guid B2 = Guid.CreateVersion7();
    private static readonly Guid B3 = Guid.CreateVersion7();

    private static (Guid, SelfReportedPlant) R(Guid beat, string referent, string question = "why it matters") =>
        (beat, new SelfReportedPlant(referent, question));

    // ── parsing ──────────────────────────────────────────────────────────────

    [Test]
    public void Parse_SplitsOnTheEmDashThePromptAsksFor()
    {
        var p = SelfReportedPlant.Parse("the message reading RECALLED TO LIFE — nobody understands it");

        Assert.That(p.Referent, Is.EqualTo("the message reading RECALLED TO LIFE"));
        Assert.That(p.Question, Is.EqualTo("nobody understands it"));
    }

    [Test]
    public void Parse_ModelIgnoredTheFormat_StillYieldsAReferent()
    {
        // Dropping the line would silently lower the measured count, which is the one thing a
        // calibration harness must never do quietly.
        var p = SelfReportedPlant.Parse("a locked drawer nobody will open");

        Assert.That(p.Referent, Is.EqualTo("a locked drawer nobody will open"));
        Assert.That(p.Question, Is.Empty);
        Assert.That(p.Description, Is.EqualTo("a locked drawer nobody will open"));
    }

    // ── merging the same plant ───────────────────────────────────────────────

    [Test]
    public void Merge_IdenticalReferentsAcrossBeatsBecomeOneRow()
    {
        var merged = SelfReportedPlantService.Merge([
            R(B1, "the message reading RECALLED TO LIFE"),
            R(B2, "the message reading RECALLED TO LIFE"),
            R(B3, "the message reading RECALLED TO LIFE"),
        ]);

        Assert.That(merged, Has.Count.EqualTo(1));
        Assert.That(merged[0].ReportCount, Is.EqualTo(3));
        Assert.That(merged[0].ReportedInBeats, Is.EqualTo(new[] { B1, B2, B3 }));
    }

    [Test]
    public void Merge_ArticlesAndPossessivesDoNotMakeANewPlant()
    {
        var merged = SelfReportedPlantService.Merge([
            R(B1, "the locked drawer in Vance's desk"),
            R(B2, "a locked drawer in his desk"),
        ]);

        Assert.That(merged, Has.Count.EqualTo(1), "the same drawer, worded differently, is one promise");
    }

    [Test]
    public void Merge_SameBeatReportingTwiceCountsOnce()
    {
        var merged = SelfReportedPlantService.Merge([
            R(B1, "the scar on her wrist"),
            R(B1, "the scar on her wrist"),
        ]);

        Assert.That(merged, Has.Count.EqualTo(1));
        Assert.That(merged[0].ReportCount, Is.EqualTo(1), "one beat cannot owe the same promise twice");
    }

    [Test]
    public void Merge_KeepsTheFirstWordingSoTheRowReadsAsFirstStated()
    {
        var merged = SelfReportedPlantService.Merge([
            R(B1, "the sealed letter", "who sent it"),
            R(B2, "a sealed letter", "what it says"),
        ]);

        Assert.That(merged, Has.Count.EqualTo(1));
        Assert.That(merged[0].Referent, Is.EqualTo("the sealed letter"));
        Assert.That(merged[0].Question, Is.EqualTo("who sent it"));
    }

    // ── NOT merging distinct plants (the dangerous direction) ────────────────

    [Test]
    public void Merge_DifferentPlantsStaySeparate()
    {
        var merged = SelfReportedPlantService.Merge([
            R(B1, "the message reading RECALLED TO LIFE"),
            R(B2, "the scar on her wrist"),
            R(B3, "the locked drawer in Vance's desk"),
        ]);

        Assert.That(merged, Has.Count.EqualTo(3));
    }

    [Test]
    public void Merge_SharingOneOrdinaryWordIsNotEnough()
    {
        // "letter" in common, nothing else. Collapsing these would hide a real false positive.
        var merged = SelfReportedPlantService.Merge([
            R(B1, "the sealed letter from Marseille"),
            R(B2, "the letter of credit the bank refused"),
        ]);

        Assert.That(merged, Has.Count.EqualTo(2),
            "two different objects that happen to share a noun are two promises, not one");
    }

    [Test]
    public void Merge_TwoPeopleWithTheSameSurnameStaySeparate()
    {
        var merged = SelfReportedPlantService.Merge([
            R(B1, "Doctor Manette's imprisonment"),
            R(B2, "Lucie Manette's parentage"),
        ]);

        Assert.That(merged, Has.Count.EqualTo(2));
    }

    // ── shape ────────────────────────────────────────────────────────────────

    [Test]
    public void Merge_EmptyInputIsEmpty()
    {
        Assert.That(SelfReportedPlantService.Merge([]), Is.Empty);
    }

    [Test]
    public void Similarity_IdenticalIsOne_DisjointIsZero()
    {
        Assert.That(SelfReportedPlantService.Similarity("the locked drawer", "the locked drawer"), Is.EqualTo(1.0));
        Assert.That(SelfReportedPlantService.Similarity("the locked drawer", "a distant siren"), Is.EqualTo(0.0));
    }

    [Test]
    public void Similarity_StopwordOnlyReferentsFallBackToExactText()
    {
        // Nothing significant survives normalisation; identity must not collapse to "everything
        // matches everything", which would merge the whole run into one row.
        Assert.That(SelfReportedPlantService.Similarity("the it", "that this"), Is.EqualTo(0.0));
    }
}
