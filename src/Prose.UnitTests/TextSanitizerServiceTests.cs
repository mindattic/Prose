using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Coverage for the generated-map mojibake repair + BOM stripping. Mojibake
/// inputs are CONSTRUCTED by encoding round-trip (never literal) so this test
/// file itself can't be corrupted by the failure mode it verifies.
/// </summary>
[TestFixture]
public class TextSanitizerServiceTests
{
    private static string Mangle(string clean) =>
        TextSanitizerService.DecodeAsCp1252(System.Text.Encoding.UTF8.GetBytes(clean));

    [TestCase("—")] // em dash
    [TestCase("–")] // en dash
    [TestCase("’")] // right single quote
    [TestCase("“")] // left double quote
    [TestCase("…")] // ellipsis
    [TestCase("Φ")] // QUANTA symbol
    [TestCase("é")] // NOT in the old hand-enumerated map
    [TestCase("ğ")] // Latin Extended-A — Tekirdağ-class names
    [TestCase("œ")]
    public void Sanitize_repairs_single_encoded_mojibake(string clean)
    {
        var mangled = Mangle(clean);
        Assert.That(mangled, Is.Not.EqualTo(clean));
        Assert.That(TextSanitizerService.HasMojibake(mangled), Is.True);
        Assert.That(TextSanitizerService.Sanitize(mangled), Is.EqualTo(clean));
    }

    [Test]
    public void Sanitize_repairs_double_encoded_text()
    {
        const string clean = "the long dark — and after";
        var doubled = Mangle(Mangle(clean));
        Assert.That(TextSanitizerService.Sanitize(doubled), Is.EqualTo(clean));
    }

    [Test]
    public void Sanitize_repairs_mixed_clean_and_mangled_text()
    {
        // A beat where one splice arrived mangled but the rest is healthy:
        // the legit em dash and é must survive untouched.
        var text = "café stood open — " + Mangle("señor Günter…");
        Assert.That(TextSanitizerService.Sanitize(text),
            Is.EqualTo("café stood open — señor Günter…"));
    }

    [Test]
    public void Sanitize_strips_bom_anywhere()
    {
        var text = "\uFEFFThe door\uFEFF opened.";
        Assert.That(TextSanitizerService.HasMojibake(text), Is.True);
        Assert.That(TextSanitizerService.Sanitize(text), Is.EqualTo("The door opened."));
    }

    [Test]
    public void Sanitize_leaves_clean_prose_untouched()
    {
        const string clean = "Thirty-five winters — “she said” … café, Tekirdağ, Φ100.";
        Assert.That(TextSanitizerService.HasMojibake(clean), Is.False);
        Assert.That(TextSanitizerService.Sanitize(clean), Is.SameAs(clean));
    }

    [Test]
    public void Sanitize_handles_legacy_dropped_tail_right_quote()
    {
        // Pipelines that dropped the unprintable 0x9D tail: a-circumflex+euro -> right double quote
        var legacy = "he said\u00E2\u20AC and left";
        Assert.That(TextSanitizerService.Sanitize(legacy), Is.EqualTo("he said\u201D and left"));
    }

    [Test]
    public void Sanitize_null_and_empty_are_safe()
    {
        Assert.That(TextSanitizerService.Sanitize(null), Is.EqualTo(""));
        Assert.That(TextSanitizerService.Sanitize(""), Is.EqualTo(""));
        Assert.That(TextSanitizerService.HasMojibake(null), Is.False);
    }
}
