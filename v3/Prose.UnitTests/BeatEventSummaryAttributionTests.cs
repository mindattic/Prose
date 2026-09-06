using System.Reflection;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-09-06 attribution guard in <see cref="BeatEventSummaryService"/>.
///
/// <para>Batched event-summary generation keys each result to a "[ref N]" label. The existing code
/// deduped a DUPLICATE ref but never verified that a returned summary describes the beat it was
/// keyed to, so a SHIFTED batch wrote every beat its neighbour's event — and stamped them
/// state=current, meaning nothing would re-derive them and every synopsis-altitude instrument read
/// a wrong chapter. Found by hand-reading BCODA ch.31 (positions 392-397); no instrument caught it.
/// The fixture below is that real batch, abbreviated to the attribution-bearing sentences.</para>
/// </summary>
[TestFixture]
public class BeatEventSummaryAttributionTests
{
    private static readonly Guid B392 = Guid.NewGuid(); // Kyle walks in rain, six watchers
    private static readonly Guid B393 = Guid.NewGuid(); // KT's Kinetix arm, medical lockout
    private static readonly Guid B394 = Guid.NewGuid(); // Rook, pommel to BCI housing
    private static readonly Guid B395 = Guid.NewGuid(); // Nines, pneumatic brace folded
    private static readonly Guid B396 = Guid.NewGuid(); // Mira negotiates, Narrows reveal

    private static List<(Guid Id, string Text, string Chapter)> Batch() =>
    [
        (B392, "The rain had been settling in since midnight, and by the time Kyle was through the " +
               "Spine crossing it had the run of the place. He found five watchers by the time he was " +
               "two blocks from The Pivot. The sixth was already behind him.", "ch31"),
        (B393, "The drone dropped at 02:16, sixteen inches short. Kyle knew what a stun drone looked " +
               "like. The corner man came first. He had an arm. KT. Kinetix TerraForm. He took the " +
               "swing on his forearm, stepped inside it, and found the lockout. The arm stopped.", "ch31"),
        (B394, "Rook came out of the doorway, the approach Kyle had expected. The BCI port sat at his " +
               "left temple — an older Meridian Conduct unit. The pommel found the housing at the " +
               "angle it needed. Rook blinked twice and sat down against the laundromat door.", "ch31"),
        (B395, "Nines was the one Kyle had marked last. Industrial wrist brace, pneumatic assist. Kyle " +
               "used Nines's own weight to fold the braced arm up behind his back. He heard Dex's " +
               "footsteps go east at a run.", "ch31"),
        (B396, "Mira came down by the fire access on the east side, and she came alone. \"The Narrows " +
               "desk bought your file last year, freelancer.\"", "ch31"),
    ];

    private static HashSet<Guid> Detect(List<(Guid, string, string)> batch, List<(Guid, string)> events) =>
        (HashSet<Guid>)typeof(BeatEventSummaryService)
            .GetMethod("DetectShiftedAttribution", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [batch, events])!;

    /// <summary>The real defect: summaries keyed one beat late.</summary>
    [Test]
    public void ShiftedBatch_FlagsExactlyTheMisattributedBeats()
    {
        var events = new List<(Guid, string)>
        {
            (B392, "Kyle walks home in rain through West Town; identifies six surveillance positions."),
            (B393, "Kyle strikes Rook's Meridian Conduct port housing, triggering five-second reset."),
            (B394, "Kyle folds Nines's pneumatic brace against its own frame, locking the joint."),
            (B395, "Dex exits east at a run, carrying news of the engagement."),
            (B396, "Mira reveals the Narrows desk bought Kyle's file last year."),
        };

        var suspect = Detect(Batch(), events);

        Assert.Multiple(() =>
        {
            Assert.That(suspect, Does.Contain(B393), "#393 is the KT beat but its summary names Rook.");
            Assert.That(suspect, Does.Contain(B394), "#394 is the Rook beat but its summary names Nines.");
            Assert.That(suspect, Does.Not.Contain(B392), "#392's summary describes #392.");
            Assert.That(suspect, Does.Not.Contain(B395), "#395 genuinely contains Dex leaving.");
            Assert.That(suspect, Does.Not.Contain(B396), "#396's summary describes #396.");
        });
    }

    /// <summary>The precision case: correct attributions must never be flagged, or the guard just
    /// converts every batch into N single-beat calls and multiplies cost.</summary>
    [Test]
    public void CorrectlyAttributedBatch_FlagsNothing()
    {
        var events = new List<(Guid, string)>
        {
            (B392, "Kyle walks home in rain; identifies six surveillance positions."),
            (B393, "Kyle locks KT's Kinetix arm using the medical override."),
            (B394, "Kyle strikes Rook's Meridian Conduct housing, triggering a five-second reset."),
            (B395, "Kyle folds Nines's pneumatic brace; Dex runs east with the news."),
            (B396, "Mira reveals the Narrows desk bought Kyle's file."),
        };

        Assert.That(Detect(Batch(), events), Is.Empty);
    }

    /// <summary>A summary may legitimately name something the prose words differently. That must not
    /// be flagged — which is why the rule requires the name to appear in a SIBLING beat.</summary>
    [Test]
    public void NameAbsentFromEntireBatch_IsNotFlagged()
    {
        var events = new List<(Guid, string)>
        {
            (B393, "Kyle locks the arm; Húlìjīng will hear about it."),
        };

        Assert.That(Detect(Batch(), events), Is.Empty,
            "A name found nowhere in the batch is a paraphrase, not a shift.");
    }

    /// <summary>A single-beat batch has no ambiguity to detect and must short-circuit.</summary>
    [Test]
    public void SingleBeatBatch_IsNeverFlagged()
    {
        var batch = Batch().Take(1).Select(b => (b.Id, b.Text, b.Chapter)).ToList();
        var events = new List<(Guid, string)> { (B392, "Kyle strikes Rook's port housing.") };

        Assert.That(Detect(batch, events), Is.Empty);
    }
}
