using System.Text;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Pure-static coverage for the mojibake detector/repair after the 2026-09-15 incident: the
/// BCODA node bible had "§13", "Φ" and em dashes stored quadruple-encoded and the repair pass
/// stalled one layer short because detection only knew the "â€" family. Every fixture here is
/// built by <see cref="Corrupt"/> — UTF-8 bytes decoded as cp1252, N times — never pasted as
/// literals, so the source file's own encoding cannot alter what is being tested.
/// </summary>
[TestFixture]
public class MojibakeRepairServiceTests
{
    /// <summary>Apply the bad pipeline N times: encode as UTF-8, decode as Windows-1252.</summary>
    private static string Corrupt(string clean, int layers)
    {
        var s = clean;
        for (var i = 0; i < layers; i++)
            s = TextSanitizerService.DecodeAsCp1252(Encoding.UTF8.GetBytes(s));
        return s;
    }

    [TestCase("§", 1)]   // §
    [TestCase("§", 2)]
    [TestCase("§", 4)]   // the BCODA depth
    [TestCase("Φ", 1)]   // Φ
    [TestCase("Φ", 3)]
    [TestCase("—", 1)]   // em dash
    [TestCase("—", 2)]
    public void ContainsMojibake_DetectsEveryLayerDepth(string glyph, int layers)
    {
        var bad = $"see {Corrupt(glyph, layers)}13 for the rule";
        Assert.That(MojibakeRepairService.ContainsMojibake(bad), Is.True, bad);
    }

    [Test]
    public void ContainsMojibake_CleanText_IsFalse()
    {
        // Real accented / punctuated prose that must NOT read as corruption.
        const string clean = "Mrs. Chen — “sit, eat” — Φ180, §13, café, naïve, año, Œuvre… ¿Qué?";
        Assert.That(MojibakeRepairService.ContainsMojibake(clean), Is.False);
        Assert.That(MojibakeRepairService.FirstMojibakeExcerpt(clean), Is.Null);
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(4)]
    public void RepairToStable_PeelsEveryLayer_InOneCall(int layers)
    {
        const string clean = "The rule is §13; the fee is Φ180 — always.";
        var bad = Corrupt(clean, layers);
        Assume.That(bad, Is.Not.EqualTo(clean));

        var repaired = MojibakeRepairService.RepairToStable(bad, out var passes);

        Assert.That(repaired, Is.EqualTo(clean));
        Assert.That(passes, Is.EqualTo(layers));
    }

    [Test]
    public void RepairToStable_CleanText_ReturnsNull()
    {
        const string clean = "Kyle took the stairs down — Φ180, half his rate.";
        Assert.That(MojibakeRepairService.RepairToStable(clean, out var passes), Is.Null);
        Assert.That(passes, Is.EqualTo(0));
    }

    [Test]
    public void RepairMixed_LeavesAlreadyCorrectUnicodeNeighboursIntact()
    {
        // A double-encoded § immediately followed by a legitimately correct ellipsis and a
        // code point outside cp1252 entirely (→). The old whole-run decode threw on the
        // ellipsis byte and left the § corrupted; the prefix-trim decode must repair the §
        // and keep both neighbours byte-for-byte.
        var bad = "rule " + Corrupt("§", 2) + "… → end";

        var repaired = MojibakeRepairService.RepairToStable(bad);

        Assert.That(repaired, Is.EqualTo("rule §… → end"));
    }

    [Test]
    public void FirstMojibakeExcerpt_ReturnsWindowAroundTheHit()
    {
        var bad = new string('x', 100) + Corrupt("—", 1) + new string('y', 100);
        var excerpt = MojibakeRepairService.FirstMojibakeExcerpt(bad, radius: 10);
        Assert.That(excerpt, Is.Not.Null);
        Assert.That(excerpt!.Length, Is.LessThanOrEqualTo(25));
        Assert.That(excerpt, Does.Contain("x"));
        Assert.That(excerpt, Does.Contain("y"));
    }

    [Test]
    public void FirstMojibakeExcerpt_UsesSanitizerTableForSingleLayerCorpusGlyphs()
    {
        // A glyph the byte-signature check might miss on its own is still caught through
        // TextSanitizerService's generated pattern table.
        var bad = "price " + Corrupt("€", 1) + "5";
        Assert.That(MojibakeRepairService.FirstMojibakeExcerpt(bad), Is.Not.Null);
    }
}
