using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-08-09 `related:` frontmatter fix. The frontmatter (currently
/// just `related: docs/WORLD.md`) is recomputed fresh from live UniverseId on every regenerate
/// and must NEVER be carried forward as "hand-authored" content — GenerateAsync's own comment
/// warns this exact bug: without stripping it first, a fresh frontmatter block gets prepended
/// on top of the old one (now embedded in what ExtractHandAuthored returns) every single
/// regenerate, accumulating one block per run. Verified manually via the CLI during the same
/// session (regenerated ATTE.md twice, confirmed exactly one `---\nrelated:...\n---` block
/// survived) — these tests pin that same guarantee at the unit level so a refactor can't
/// silently reintroduce the accumulation.
/// </summary>
[TestFixture]
public class NodeDocServiceFrontmatterTests
{
    [Test]
    public void StripFrontmatter_RemovesLeadingBlock()
    {
        var content = "---\nrelated: docs/WORLD.md\n---\n\n# Book Context\n\nSome hand-authored text.";

        var result = NodeDocService.StripFrontmatter(content);

        Assert.That(result, Is.EqualTo("# Book Context\n\nSome hand-authored text."));
    }

    [Test]
    public void StripFrontmatter_NoFrontmatter_ReturnsUnchanged()
    {
        var content = "# Book Context\n\nSome hand-authored text with no frontmatter.";

        var result = NodeDocService.StripFrontmatter(content);

        Assert.That(result, Is.EqualTo(content));
    }

    [Test]
    public void StripFrontmatter_MalformedUnclosedBlock_ReturnsUnchanged()
    {
        // A "---\n" opener with no closing "---" is not a valid frontmatter block — must not
        // eat the rest of the document looking for a close that never comes.
        var content = "---\nThis just starts with a horizontal rule, not real frontmatter.";

        var result = NodeDocService.StripFrontmatter(content);

        Assert.That(result, Is.EqualTo(content));
    }

    [Test]
    public void ExtractThenStrip_TwoRegenerateCycles_FrontmatterNeverAccumulates()
    {
        // Simulates GenerateAsync's exact sequence across two consecutive regenerates:
        // 1. Prepend fresh frontmatter + hand-authored content + generated marker/section.
        // 2. On the NEXT regenerate, ExtractHandAuthored pulls everything before the marker
        //    (which now includes the previous frontmatter block) — StripFrontmatter must
        //    remove that before a NEW frontmatter block is prepended again.
        const string marker = "<!-- ==== GENERATED SECTIONS — do not hand-edit below this line ==== -->";
        var handAuthored = "# Book Context: Attendance (ATTE)\n\nArc notes here.";

        string BuildDoc(string frontmatter, string hand) =>
            frontmatter + hand + "\n\n" + marker + "\n<!-- GENERATED-CHECKSUM: abc -->\n## Structural Blueprint\n";

        // Run 1: no prior NodeOutline, nothing to strip.
        var run1Extracted = NodeDocService.StripFrontmatter(NodeDocService.ExtractHandAuthored(null));
        Assert.That(run1Extracted, Is.EqualTo(""));
        var run1Doc = BuildDoc("---\nrelated: docs/WORLD.md\n---\n\n", handAuthored);

        // Run 2: NodeOutline now holds run1Doc. ExtractHandAuthored + StripFrontmatter must
        // recover the ORIGINAL hand-authored text, with the run-1 frontmatter gone.
        var run2Extracted = NodeDocService.StripFrontmatter(NodeDocService.ExtractHandAuthored(run1Doc));
        Assert.That(run2Extracted, Is.EqualTo(handAuthored),
            "run 2 must recover exactly the original hand-authored text — no leftover frontmatter, " +
            "no duplication");

        var run2Doc = BuildDoc("---\nrelated: docs/WORLD.md\n---\n\n", run2Extracted);

        // The final doc must contain exactly ONE frontmatter block, not two stacked.
        var frontmatterOccurrences = System.Text.RegularExpressions.Regex.Matches(run2Doc, "related: docs/WORLD.md").Count;
        Assert.That(frontmatterOccurrences, Is.EqualTo(1),
            "exactly one frontmatter block must survive across regenerate cycles, never accumulating");
    }
}

/// <summary>
/// Regression cover for the 2026-08-10 Beat Spine mid-word truncation bug, found via a Logic
/// Sweep of BLST (Ballast) — the compressed (&gt;60 beat) Beat Spine view rendered lines like
/// "...foreshadowing that the crisis will for. - closes" because BeatLabel's fallback sliced a
/// beat's Description at a hard character index (80) with no word-boundary awareness. Fixed by
/// routing through TruncateAtWordBoundary, which backs up to the last space before the cutoff.
/// </summary>
[TestFixture]
public class NodeDocServiceBeatLabelTests
{
    [Test]
    public void BeatLabel_PrefersTitle_WhenPresent()
    {
        var result = NodeDocService.BeatLabel("The Ledger", "A much longer description that would otherwise be truncated here.");

        Assert.That(result, Is.EqualTo("The Ledger"));
    }

    [Test]
    public void BeatLabel_ShortDescription_ReturnedUnchanged()
    {
        var result = NodeDocService.BeatLabel(null, "A short description.");

        Assert.That(result, Is.EqualTo("A short description."));
    }

    [Test]
    public void BeatLabel_NoTitleOrDescription_ReturnsDash()
    {
        var result = NodeDocService.BeatLabel(null, null);

        Assert.That(result, Is.EqualTo("—"));
    }

    [Test]
    public void BeatLabel_LongDescription_TruncatesAtWordBoundary_NotMidWord()
    {
        // The exact real-world string from BLST's Description that produced "will for…" —
        // the 80th character lands inside "force". Must back up to the space before "force".
        const string desc = "Reveals Teo's grim administrative burden, foreshadowing that the crisis will force impossible choices about whose lives matter less.";

        var result = NodeDocService.BeatLabel(null, desc);

        Assert.That(result, Does.EndWith("…"));
        Assert.That(result, Does.Not.Contain("for…"), "must not cut off mid-word inside \"force\"");
        Assert.That(result, Is.EqualTo("Reveals Teo's grim administrative burden, foreshadowing that the crisis will…"));
    }

    [Test]
    public void TruncateAtWordBoundary_ExactLength_ReturnsUnchanged()
    {
        var text = new string('a', 80);

        var result = NodeDocService.TruncateAtWordBoundary(text, 80);

        Assert.That(result, Is.EqualTo(text));
    }

    [Test]
    public void TruncateAtWordBoundary_NoSpaceBeforeLimit_FallsBackToHardCut()
    {
        // A single 100-character word with no space at all — there's no word boundary to back
        // up to, so this must still produce a bounded result rather than throwing or returning
        // the full oversized string.
        var text = new string('a', 100);

        var result = NodeDocService.TruncateAtWordBoundary(text, 80);

        Assert.That(result, Is.EqualTo(new string('a', 80) + "…"));
    }

    [Test]
    public void TruncateAtWordBoundary_SpaceExactlyAtCutoff_BacksUpCorrectly()
    {
        var text = "1234567890 " + new string('b', 100); // space at index 10, well before 20

        var result = NodeDocService.TruncateAtWordBoundary(text, 20);

        Assert.That(result, Is.EqualTo("1234567890…"));
    }
}
