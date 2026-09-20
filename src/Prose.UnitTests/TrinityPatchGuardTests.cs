using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-08-19 Trinity Reconciliation incident: the prose-losing case's
/// default repair mechanism regenerated a whole beat and silently replaced 2,848 chars with an
/// unrelated 10,482-char invented scene. TrinityReconciliationService.IsUnsafeLinePatch is the
/// guard for the surgical single-paragraph replacement (PatchBeatAsync) that now handles this case
/// instead — exercised directly since it's a pure static predicate, mirroring
/// BeatRepairServiceTests' coverage of IsUnsafeShrink.
/// </summary>
[TestFixture]
public class TrinityPatchGuardTests
{
    [Test]
    public void IsUnsafeLinePatch_ModestCorrection_ReturnsFalse()
    {
        // A typical single-fact correction: swap one clause, paragraph stays roughly the same size.
        Assert.That(TrinityReconciliationService.IsUnsafeLinePatch(245, 260), Is.False);
    }

    [Test]
    public void IsUnsafeLinePatch_ShortDialogueLineGrownGenerously_ReturnsFalse()
    {
        // "Don't." (7 chars) growing several-fold is fine in absolute terms — ratio math is noise
        // below the 20-char floor.
        Assert.That(TrinityReconciliationService.IsUnsafeLinePatch(7, 40), Is.False);
    }

    [Test]
    public void IsUnsafeLinePatch_ShortLineShrunkToNothing_ReturnsFalse()
    {
        // Below the 20-char floor, the shrink-ratio guard doesn't apply at all.
        Assert.That(TrinityReconciliationService.IsUnsafeLinePatch(7, 2), Is.False);
    }

    [Test]
    public void IsUnsafeLinePatch_LongParagraphGrownJustUnderCap_ReturnsFalse()
    {
        // 2x cap: 500 -> 1000 is exactly at the boundary, not over it.
        Assert.That(TrinityReconciliationService.IsUnsafeLinePatch(500, 1000), Is.False);
    }

    [Test]
    public void IsUnsafeLinePatch_LongParagraphGrownOverCap_ReturnsTrue()
    {
        Assert.That(TrinityReconciliationService.IsUnsafeLinePatch(500, 1001), Is.True);
    }

    [Test]
    public void IsUnsafeLinePatch_SmallParagraphUsesAbsoluteFloorNotRatio_ReturnsFalse()
    {
        // 50 -> 220: ratio (4.4x) would fail a pure-2x rule, but the +200 absolute floor covers it.
        Assert.That(TrinityReconciliationService.IsUnsafeLinePatch(50, 220), Is.False);
    }

    [Test]
    public void IsUnsafeLinePatch_SmallParagraphOverAbsoluteFloor_ReturnsTrue()
    {
        Assert.That(TrinityReconciliationService.IsUnsafeLinePatch(50, 260), Is.True);
    }

    [Test]
    public void IsUnsafeLinePatch_LongParagraphCollapsedBelowFloor_ReturnsTrue()
    {
        // 245 (corpus median) shrunk to under 0.4x (98 chars) — a stub-style collapse.
        Assert.That(TrinityReconciliationService.IsUnsafeLinePatch(245, 90), Is.True);
    }

    [Test]
    public void IsUnsafeLinePatch_LongParagraphAtShrinkBoundary_ReturnsFalse()
    {
        Assert.That(TrinityReconciliationService.IsUnsafeLinePatch(500, 200), Is.False);
    }

    [Test]
    public void IsUnsafeLinePatch_LongParagraphJustBelowShrinkBoundary_ReturnsTrue()
    {
        Assert.That(TrinityReconciliationService.IsUnsafeLinePatch(500, 199), Is.True);
    }

    [Test]
    public void IsUnsafeLinePatch_TheActualIncidentRatioAtBeatScale_ReturnsTrue()
    {
        // The live 2026-08-19 incident's ratio (2848 -> 10482, 3.68x) applied at paragraph scale
        // must still be refused — the whole point of tightening from a naive 3x to 2x.
        Assert.That(TrinityReconciliationService.IsUnsafeLinePatch(2848, 10482), Is.True);
    }

    // ── StripMarkdownDecoration ───────────────────────────────────────────────
    // Found live 2026-08-19: an outline regeneration reformatted character bullets from
    // "Name (slug) - desc" to "**Name** (`slug`) - desc" without the underlying fact changing,
    // which broke PatchOutlineSectionAsync's exact-line match for 5 real claims (Ruslan Adeyinka,
    // Breckenridge, Ferko Nzambe, Auda Vane, Coeli Vantanen), all refused as "snippet no longer
    // present verbatim" even though the fact was unchanged.

    [Test]
    public void StripMarkdownDecoration_RemovesBoldAsterisks()
    {
        Assert.That(TrinityReconciliationService.StripMarkdownDecoration("**Ruslan Adeyinka** - Salvage broker"),
            Is.EqualTo("Ruslan Adeyinka - Salvage broker"));
    }

    [Test]
    public void StripMarkdownDecoration_RemovesBackticks()
    {
        Assert.That(TrinityReconciliationService.StripMarkdownDecoration("(`auda-vane`)"), Is.EqualTo("(auda-vane)"));
    }

    [Test]
    public void StripMarkdownDecoration_RemovesBothInOneLine_MatchesThePreDecorationOriginal()
    {
        var decorated = "**Breckenridge** (`breckenridge`) - ex-Arcturus Defense Solutions";
        var original  = "Breckenridge (breckenridge) - ex-Arcturus Defense Solutions";
        Assert.That(TrinityReconciliationService.StripMarkdownDecoration(decorated), Is.EqualTo(original));
    }

    [Test]
    public void StripMarkdownDecoration_PlainTextWithNoDecoration_IsUnchanged()
    {
        var plain = "Ferko Nzambe - Junker, mid-fifties";
        Assert.That(TrinityReconciliationService.StripMarkdownDecoration(plain), Is.EqualTo(plain));
    }
}
