using System.Globalization;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Prose.UnitTests;

/// <summary>
/// Every colour pair the stylesheets actually draw, checked against its WCAG 2.2 bar.
///
/// <para><b>Why this exists.</b> An audit on 2026-09-19 found seven contrast failures in the UI,
/// and all seven were the same mistake: <i>a token used on a surface nobody had measured it
/// against</i>. <c>--color-border-strong</c> was measured on <c>bg-2</c> and used on <c>bg-4</c>
/// (2.65:1). <c>--color-danger-bg</c> was measured with white text and used as a banner fill under
/// the body colour (4.40:1). <c>--color-fg-muted</c> was measured on <c>bg-1</c>/<c>bg-2</c> and
/// used on <c>--color-mini-bg</c> (3.99:1). The palette's own numbers were honest; the *pairings*
/// had never been checked.</para>
///
/// <para>So this reads the real <c>tokens.css</c> rather than a copy. Change a token value and
/// this fails; introduce a new pairing without adding it here and it is simply unguarded — which
/// is why the list below is the deliverable, not the arithmetic.</para>
///
/// <para>It does not make the UI accessible. It makes one whole class of regression impossible
/// to land silently.</para>
/// </summary>
[TestFixture]
public class TokenContrastTests
{
    private const double TextBar = 4.5;     // 1.4.3 normal text
    private const double NonTextBar = 3.0;  // 1.4.11 UI components and boundaries

    private static Dictionary<string, string> dark = null!;
    private static Dictionary<string, string> light = null!;

    [OneTimeSetUp]
    public void LoadPalette()
    {
        var css = File.ReadAllText(LocateTokensCss());

        // The light block redefines a subset; anything it does not name keeps the dark value,
        // which is exactly how the cascade behaves at runtime.
        var lightStart = css.IndexOf("[data-theme=\"light\"]", StringComparison.Ordinal);
        Assert.That(lightStart, Is.GreaterThan(0), "tokens.css has no light theme block");

        dark = ParseDeclarations(css[..lightStart]);
        light = new Dictionary<string, string>(dark);
        foreach (var (k, v) in ParseDeclarations(css[lightStart..])) light[k] = v;
    }

    /// <summary>Text pairs: foreground token on background token, 4.5:1.</summary>
    private static readonly (string Fg, string Bg, string Where)[] TextPairs =
    [
        ("--color-fg",          "--color-bg-1",          "body text on the editor surface"),
        ("--color-fg",          "--color-bg-2",          "body text on the page"),
        ("--color-fg",          "--color-bg-3",          "body text in a modal"),
        ("--color-fg",          "--color-bg-fail",       ".banner.error fill (was --color-danger-bg, 4.40:1)"),
        ("--color-fg-strong",   "--color-bg-sel",        "the current beat row"),
        ("--color-fg-muted",    "--color-bg-1",          "muted text on the editor surface"),
        ("--color-fg-muted",    "--color-bg-2",          "muted text on the page"),
        ("--color-fg-subtle",   "--color-bg-1",          ".statusbar .sep (was --color-border-strong, 3.31:1)"),
        ("--color-fg-subtle",   "--color-bg-2",          "empty states"),
        ("--color-mini-fg",     "--color-mini-bg",       "mini button labels"),
        ("--color-mini-fg",     "--color-mini-bg-hover", "mini button labels, hovered"),
        ("--color-mini-fg",     "--color-mini-bg",       ".d-choice-detail (was --color-fg-muted, 3.99:1)"),
        ("--color-fg-strong",   "--color-bg-3",          ".d-confirm-said — the restatement a spoken request turns on"),
        ("--color-fg-muted",    "--color-bg-3",          ".d-confirm-head and .d-confirm-spent"),
        ("--color-accent",      "--color-bg-2",          "links and accents on the page"),
        ("--color-danger",      "--color-bg-2",          "danger text on the page"),
        ("--color-danger",      "--color-bg-3",          ".danger-btn on a modal (was on mini-bg, 3.46:1)"),
        ("--color-danger",      "--color-bg-fail",       ".danger-btn hovered"),
        ("--color-success",     "--color-bg-2",          "success glyphs"),
        ("--color-warning",     "--color-bg-2",          "warning glyphs"),
        ("--color-info",        "--color-bg-2",          "info glyphs"),
    ];

    /// <summary>Fills that carry WHITE text — the other half of the two-family rule in tokens.css.</summary>
    private static readonly (string Bg, string Where)[] WhiteOnFill =
    [
        ("--color-accent-bg", "primary button / .mini.on"),
        ("--color-danger-bg", "destructive button fill"),
        ("--color-info-bg",   "info button fill"),
    ];

    /// <summary>Non-text boundaries, 3:1.</summary>
    private static readonly (string Fg, string Bg, string Where)[] NonTextPairs =
    [
        ("--color-border-input",  "--color-bg-2", "form-control edge on the page"),
        ("--color-border-input",  "--color-bg-3", "form-control edge in a modal"),
        ("--color-border-input",  "--color-bg-4", "form-control edge against its own fill"),
        ("--color-border-strong", "--color-bg-2", "container edge, the surface it was measured on"),
        ("--color-accent",        "--color-bg-1", ".d-confirm edge against the Discuss panel"),
        ("--color-accent",        "--color-bg-3", ".d-confirm edge against its own fill"),
        ("--focus-ring-color",    "--color-bg-1", "focus ring on the editor surface"),
        ("--focus-ring-color",    "--color-bg-2", "focus ring on the page"),
        ("--focus-ring-color",    "--color-bg-3", "focus ring in a modal"),
    ];

    [Test]
    public void Text_pairs_meet_the_normal_text_minimum([Values("dark", "light")] string theme)
    {
        var palette = theme == "dark" ? dark : light;
        Assert.Multiple(() =>
        {
            foreach (var (fg, bg, where) in TextPairs)
            {
                var r = Ratio(Resolve(palette, fg, palette["--color-bg-2"]),
                              Resolve(palette, bg, palette["--color-bg-2"]));
                Assert.That(r, Is.GreaterThanOrEqualTo(TextBar),
                    $"[{theme}] {fg} on {bg} = {r:0.00}:1 — {where}");
            }
        });
    }

    [Test]
    public void Fills_that_carry_white_text_meet_the_minimum([Values("dark", "light")] string theme)
    {
        var palette = theme == "dark" ? dark : light;
        Assert.Multiple(() =>
        {
            foreach (var (bg, where) in WhiteOnFill)
            {
                var r = Ratio((255, 255, 255), Resolve(palette, bg, palette["--color-bg-2"]));
                Assert.That(r, Is.GreaterThanOrEqualTo(TextBar),
                    $"[{theme}] white on {bg} = {r:0.00}:1 — {where}");
            }
        });
    }

    [Test]
    public void Non_text_boundaries_meet_the_three_to_one_minimum([Values("dark", "light")] string theme)
    {
        var palette = theme == "dark" ? dark : light;
        Assert.Multiple(() =>
        {
            foreach (var (fg, bg, where) in NonTextPairs)
            {
                var r = Ratio(Resolve(palette, fg, palette["--color-bg-2"]),
                              Resolve(palette, bg, palette["--color-bg-2"]));
                Assert.That(r, Is.GreaterThanOrEqualTo(NonTextBar),
                    $"[{theme}] {fg} against {bg} = {r:0.00}:1 — {where}");
            }
        });
    }

    /// <summary>
    /// The entity chip, whose background is a translucent tint and therefore has to be composited
    /// against whatever surface it sits on before it can be measured at all.
    /// </summary>
    [Test]
    public void Entity_chips_are_legible_on_every_surface_they_appear_on(
        [Values("dark", "light")] string theme,
        [Values("--color-bg-1", "--color-bg-2")] string surface)
    {
        var palette = theme == "dark" ? dark : light;
        var under = ParseColour(palette[surface]);
        var tint = Composite(ParseColour(palette["--color-entity-bg"]), under);

        var glyph = Opaque(ParseColour(palette["--color-entity"]));

        Assert.Multiple(() =>
        {
            Assert.That(Ratio(glyph, tint),
                Is.GreaterThanOrEqualTo(TextBar), $"[{theme}] --color-entity on the tint over {surface}");

            var hover = Composite(ParseColour(palette["--color-entity-bg-hover"]), under);
            Assert.That(Ratio(glyph, hover),
                Is.GreaterThanOrEqualTo(TextBar), $"[{theme}] --color-entity on the hovered tint over {surface}");
        });
    }

    // ── plumbing ───────────────────────────────────────────────────────────

    private static (double R, double G, double B) Resolve(
        Dictionary<string, string> palette, string token, string compositeBase)
    {
        Assert.That(palette.ContainsKey(token), Is.True, $"tokens.css does not define {token}");
        var c = ParseColour(palette[token]);
        return c.A >= 1.0 ? (c.R, c.G, c.B) : Composite(c, ParseColour(compositeBase));
    }

    private static (double R, double G, double B) Opaque((double R, double G, double B, double A) c)
        => (c.R, c.G, c.B);

    private static (double R, double G, double B) Composite(
        (double R, double G, double B, double A) over, (double R, double G, double B, double A) under)
        => (over.R * over.A + under.R * (1 - over.A),
            over.G * over.A + under.G * (1 - over.A),
            over.B * over.A + under.B * (1 - over.A));

    private static (double R, double G, double B, double A) ParseColour(string raw)
    {
        raw = raw.Trim();

        if (raw.StartsWith('#'))
        {
            var h = raw[1..];
            if (h.Length == 3) h = string.Concat(h.Select(ch => $"{ch}{ch}"));
            return (Hex(h, 0), Hex(h, 2), Hex(h, 4), 1.0);
        }

        var m = Regex.Match(raw, @"rgba?\(\s*([\d.]+)\D+([\d.]+)\D+([\d.]+)(?:\D+([\d.]+))?\s*\)");
        Assert.That(m.Success, Is.True, $"cannot parse colour '{raw}'");
        double N(int g) => double.Parse(m.Groups[g].Value, CultureInfo.InvariantCulture);
        return (N(1), N(2), N(3), m.Groups[4].Success ? N(4) : 1.0);

        static double Hex(string h, int i) => Convert.ToInt32(h.Substring(i, 2), 16);
    }

    private static double Ratio((double R, double G, double B) a, (double R, double G, double B) b)
    {
        double la = Luminance(a), lb = Luminance(b);
        var (hi, lo) = la > lb ? (la, lb) : (lb, la);
        return (hi + 0.05) / (lo + 0.05);
    }

    private static double Luminance((double R, double G, double B) c)
        => 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);

    private static double Channel(double v)
    {
        v /= 255.0;
        return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
    }

    private static Dictionary<string, string> ParseDeclarations(string css)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match m in Regex.Matches(css, @"(--[a-z0-9-]+)\s*:\s*([^;]+);"))
            map[m.Groups[1].Value] = StripComment(m.Groups[2].Value).Trim();
        return map;

        static string StripComment(string v)
        {
            var i = v.IndexOf("/*", StringComparison.Ordinal);
            return i < 0 ? v : v[..i];
        }
    }

    /// <summary>Walks up from the test binary to the repo copy — the point is to read the file the
    /// app actually ships, not a fixture that can drift away from it.</summary>
    private static string LocateTokensCss()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "Prose.WriterUi", "wwwroot", "tokens.css");
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new FileNotFoundException("Could not find Prose.WriterUi/wwwroot/tokens.css above the test binary.");
    }
}
