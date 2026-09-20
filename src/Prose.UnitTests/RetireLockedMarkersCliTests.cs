using Prose.Cli;

namespace Prose.UnitTests;

/// <summary>
/// Pure unit tests for RetireLockedMarkersCli's SAFE/MANUAL classifier and rewrite (Bible→Outline
/// refactor Phase 6a). Every example below is a real shape observed live in BCODA's hand-authored
/// NodeOutline when this tool was first built — the classifier must never let a MANUAL shape slip
/// into SAFE, since SAFE is what --apply actually rewrites unattended.
/// </summary>
[TestFixture]
public class RetireLockedMarkersCliTests
{
    static readonly Guid NodeId = Guid.NewGuid();

    [TestCase("**The thematic spine (LOCKED 2026-07-13):** the slow, inescapable degradation.")]
    [TestCase("**Register: CODA** - `docs/registers/CODA.md` (LOCKED). Warm-propulsive frame.")]
    [TestCase("## 1. The entity - what it actually is (LOCKED 2026-07-10) {#SS-BCODA-1a}")]
    [TestCase("**The escape (LOCKED - never resolved):** The event sequence that ended Kyle's captivity.")]
    [TestCase("**Battle choreography (LOCKED):**")]
    [TestCase("**The Rogue AI motivation (LOCKED - 2026-06-23):**")]
    [TestCase("War Dog = Rogue AI asset (LOCKED). Sift = data hygiene (LOCKED).")]
    public void BareParenthetical_ClassifiedSafe(string text)
    {
        var scan = RetireLockedMarkersCli.ClassifyText(NodeId, "bcoda", "Bushido Coda", text);
        Assert.That(scan.ManualMatches, Is.Empty, "must not misclassify a safe parenthetical as manual");
        Assert.That(scan.SafeMatches, Is.Not.Empty);
    }

    [TestCase("**LOCKED SCENE - The Board (mid-book, cut-away chapter):**")]
    [TestCase("Kyle carries Sift's body to the van, the way Stash laid it. **LOCKED.**")]
    [TestCase("first non-job contact in 11 years. LOCKED. First deliberate break from relay-format structure.")]
    [TestCase("- [x] Kyle/Nadia \"meatbag\" conversation is written with the LOCKED LINE intact")]
    [TestCase("### 13g. The seven touchstones (AUTHORITATIVE - LOCKED 2026-07-13; row 7 amended 2026-07-18) {#SS-BCODA-13g}")]
    public void NonParentheticalOrCompoundParenthetical_ClassifiedManual(string text)
    {
        var scan = RetireLockedMarkersCli.ClassifyText(NodeId, "bcoda", "Bushido Coda", text);
        Assert.That(scan.SafeMatches, Is.Empty, "must never auto-rewrite a shape a human needs to rephrase");
        Assert.That(scan.ManualMatches, Is.Not.Empty);
    }

    [Test]
    public void MixedSafeAndManualInSameText_BothClassifiedIndependently()
    {
        var text = "Register (LOCKED 2026-07-10). Elsewhere: the crew's ritual. **LOCKED.**";

        var scan = RetireLockedMarkersCli.ClassifyText(NodeId, "bcoda", "Bushido Coda", text);

        Assert.That(scan.SafeMatches, Has.Count.EqualTo(1));
        Assert.That(scan.ManualMatches, Has.Count.EqualTo(1));
    }

    [Test]
    public void NoLockedOccurrence_BothListsEmpty()
    {
        var scan = RetireLockedMarkersCli.ClassifyText(NodeId, "bcoda", "Bushido Coda", "Nothing to see here.");

        Assert.That(scan.SafeMatches, Is.Empty);
        Assert.That(scan.ManualMatches, Is.Empty);
    }

    // Case-insensitivity (fixed 2026-08-30): a same-day live corpus sweep found lowercase "locked"
    // section-heading qualifiers in a book (BTL/"Between the Lines") the original ALL-CAPS-only
    // ScanPattern never surfaced in the dry-run report at all — silently invisible, not just
    // misclassified.
    [Test]
    public void LowercaseBareParenthetical_ClassifiedSafe()
    {
        var scan = RetireLockedMarkersCli.ClassifyText(NodeId, "btl", "Between the Lines",
            "**Battle choreography (locked):**");

        Assert.That(scan.ManualMatches, Is.Empty);
        Assert.That(scan.SafeMatches, Has.Count.EqualTo(1));
    }

    [Test]
    public void ApplySafeRewrite_LowercaseBareParenthetical_ConvertsToAuthorDecision()
    {
        var result = RetireLockedMarkersCli.ApplySafeRewrite("**Battle choreography (locked):**");

        Assert.That(result, Is.EqualTo("**Battle choreography (author decision):**"));
    }

    // The two REAL BTL occurrences that motivated the case-insensitivity fix are both compound
    // parentheticals (extra text alongside "locked") — MANUAL, same as their uppercase equivalents
    // elsewhere. This pins that lowercase input doesn't accidentally get treated as safe just
    // because it slipped past the old case-sensitive scan entirely before this fix.
    [TestCase("## Theme (locked - see brief §8)")]
    [TestCase("## Relationship Utilization Map (locked - every edge below must be exercised on-page)")]
    public void LowercaseCompoundParenthetical_ClassifiedManual(string text)
    {
        var scan = RetireLockedMarkersCli.ClassifyText(NodeId, "btl", "Between the Lines", text);

        Assert.That(scan.SafeMatches, Is.Empty);
        Assert.That(scan.ManualMatches, Has.Count.EqualTo(1));
    }

    [Test]
    public void ApplySafeRewrite_DatedParenthetical_ConvertsToAuthorDecisionWithDate()
    {
        var text = "## 1. The entity (LOCKED 2026-07-10) {#SS-BCODA-1a}";

        var result = RetireLockedMarkersCli.ApplySafeRewrite(text);

        Assert.That(result, Is.EqualTo("## 1. The entity (author decision, 2026-07-10) {#SS-BCODA-1a}"));
    }

    [Test]
    public void ApplySafeRewrite_BareParenthetical_ConvertsToAuthorDecisionNoDate()
    {
        var text = "**Battle choreography (LOCKED):**";

        var result = RetireLockedMarkersCli.ApplySafeRewrite(text);

        Assert.That(result, Is.EqualTo("**Battle choreography (author decision):**"));
    }

    [Test]
    public void ApplySafeRewrite_NeverResolvedParenthetical_ConvertsWithEmDash()
    {
        var text = "**The escape (LOCKED - never resolved):** details.";

        var result = RetireLockedMarkersCli.ApplySafeRewrite(text);

        Assert.That(result, Is.EqualTo("**The escape (author decision — never resolved):** details."));
    }

    [Test]
    public void ApplySafeRewrite_MultipleSafeOccurrences_AllConverted()
    {
        var text = "War Dog = Rogue AI asset (LOCKED). Sift = data hygiene (LOCKED).";

        var result = RetireLockedMarkersCli.ApplySafeRewrite(text);

        Assert.That(result, Is.EqualTo("War Dog = Rogue AI asset (author decision). Sift = data hygiene (author decision)."));
    }

    [Test]
    public void ApplySafeRewrite_CompoundParenthetical_LeftUntouchedByteForByte()
    {
        var text = "### 13g. The seven touchstones (AUTHORITATIVE - LOCKED 2026-07-13; row 7 amended 2026-07-18) {#SS-BCODA-13g}";

        var result = RetireLockedMarkersCli.ApplySafeRewrite(text);

        Assert.That(result, Is.EqualTo(text), "a compound parenthetical must never be mangled by the safe rewrite");
    }

    [Test]
    public void ApplySafeRewrite_BareMidSentenceLocked_LeftUntouched()
    {
        var text = "Kyle carries Sift's body to the van. **LOCKED.**";

        var result = RetireLockedMarkersCli.ApplySafeRewrite(text);

        Assert.That(result, Is.EqualTo(text));
    }

    [Test]
    public void ApplySafeRewrite_NoLockedText_ReturnsUnchanged()
    {
        var text = "Nothing to see here.";

        var result = RetireLockedMarkersCli.ApplySafeRewrite(text);

        Assert.That(result, Is.EqualTo(text));
    }
}
