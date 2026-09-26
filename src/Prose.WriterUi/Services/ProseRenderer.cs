using System.Text;
using System.Text.RegularExpressions;
using Prose.Core.Services;

namespace Prose.WriterUi.Services;

/// <summary>
/// Turns raw tagged beat prose into the HTML the editor edits, and back again.
///
/// <para>Two kinds of markup live in a beat: <c>&lt;entity …&gt;</c> tags, which are structural and
/// carry a guid, and the inline emphasis markers parsed by <see cref="ProseInline"/>. Entities
/// render as atomic spans the author cannot type inside — you change one through the entity modal,
/// not by editing letters in the middle of a link — while emphasis renders as ordinary nested tags
/// that behave the way a writer expects when they select text and hit Ctrl+I.</para>
///
/// <para>Everything from the database is HTML-escaped before any markup of ours is added. The
/// prose is authored text: it legitimately contains <c>&lt;</c>, <c>&amp;</c> and quotes, and a
/// beat can hold a malformed tag, because nothing upstream of this editor ever checked.</para>
/// </summary>
public static class ProseRenderer
{
    private static readonly Regex TagPattern =
        new(@"<entity\b[^>]*>(.*?)</entity>", RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex GuidAttr =
        new(@"\bguid=""([^""]*)""", RegexOptions.Compiled);

    private static readonly Regex RepoAttr =
        new(@"\brepo=""([^""]*)""", RegexOptions.Compiled);

    public static string ToHtml(string? tagged)
    {
        if (string.IsNullOrWhiteSpace(tagged)) return "<p><br></p>";

        var sb = new StringBuilder();
        var paragraphs = tagged.Replace("\r\n", "\n")
                               .Split("\n\n", StringSplitOptions.RemoveEmptyEntries);

        foreach (var para in paragraphs)
            sb.Append("<p>").Append(RenderInline(para.Trim())).Append("</p>");

        return sb.Length == 0 ? "<p><br></p>" : sb.ToString();
    }

    // Entity tags stand in the paragraph as one private-use character each while the emphasis is
    // parsed, and become chips after. Emphasis is parsed over the WHOLE paragraph that way: parsed
    // piece by piece between tags, as it was, a marker pair with a tag between them —
    // "*<entity>Kyle</entity>*", an italic name that was linked — matched in neither piece, and
    // both asterisks showed as literal text in the editor while every exporter rendered italics.
    private const char FirstPlaceholder = '\uE000';
    private const char LastPlaceholder = '\uF8FF';

    private static string RenderInline(string text)
    {
        // Prose that already holds a private-use character (vanishingly rare) could not be told
        // apart from a placeholder; it keeps the piecewise rendering, which is only wrong in the
        // case described above.
        if (text.Any(c => c >= FirstPlaceholder && c <= LastPlaceholder)) return RenderPiecewise(text);

        var chips = new List<string>();
        var flat = new StringBuilder();
        var cursor = 0;

        foreach (Match m in TagPattern.Matches(text))
        {
            flat.Append(text, cursor, m.Index - cursor);
            var chip = ChipHtml(m);
            if (chip is not null && FirstPlaceholder + chips.Count <= LastPlaceholder)
            {
                flat.Append((char)(FirstPlaceholder + chips.Count));
                chips.Add(chip);
            }
            else
            {
                // A malformed tag still shows its words. The prose has to stay readable even when
                // the markup around it is wrong — it just doesn't become an interactive entity.
                flat.Append(m.Groups[1].Value);
            }
            cursor = m.Index + m.Length;
        }
        flat.Append(text, cursor, text.Length - cursor);

        var sb = new StringBuilder();
        AppendFormatted(sb, flat.ToString(), chips);
        return sb.Length == 0 ? "<br>" : sb.ToString();
    }

    /// <summary>The chip for one entity tag, or null when the tag is malformed.</summary>
    private static string? ChipHtml(Match m)
    {
        var guid = GuidAttr.Match(m.Value).Groups[1].Value;
        var repo = RepoAttr.Match(m.Value).Groups[1].Value;
        var inner = m.Groups[1].Value;
        if (!Guid.TryParse(guid, out var id) || inner.Contains('<')) return null;

        // No title attribute: hovering a chip opens its entity card (writer.js), and a native
        // tooltip on top of that would be two tooltips at once.
        return new StringBuilder()
            .Append("<span class=\"ent\" contenteditable=\"false\"")
            .Append(" data-guid=\"").Append(Escape(id.ToString()))
            .Append("\" data-repo=\"").Append(Escape(repo))
            .Append("\">").Append(Escape(inner)).Append("</span>")
            .ToString();
    }

    private static string RenderPiecewise(string text)
    {
        var sb = new StringBuilder();
        var cursor = 0;
        foreach (Match m in TagPattern.Matches(text))
        {
            AppendFormatted(sb, text[cursor..m.Index], []);
            sb.Append(ChipHtml(m) ?? FormattedOnly(m.Groups[1].Value));
            cursor = m.Index + m.Length;
        }
        AppendFormatted(sb, text[cursor..], []);
        return sb.Length == 0 ? "<br>" : sb.ToString();
    }

    private static string FormattedOnly(string text)
    {
        var sb = new StringBuilder();
        AppendFormatted(sb, text, []);
        return sb.ToString();
    }

    private static void AppendFormatted(StringBuilder sb, string text, IReadOnlyList<string> chips)
    {
        foreach (var span in ProseInline.Parse(text))
        {
            if (span.Text.Length == 0) continue;

            var open = new StringBuilder();
            var close = new StringBuilder();

            // Order matters only for symmetry; these nest cleanly either way.
            if (span.Style.HasFlag(ProseInline.Style.Bold)) { open.Append("<strong>"); close.Insert(0, "</strong>"); }
            if (span.Style.HasFlag(ProseInline.Style.Italic)) { open.Append("<em>"); close.Insert(0, "</em>"); }
            if (span.Style.HasFlag(ProseInline.Style.Underline)) { open.Append("<u>"); close.Insert(0, "</u>"); }
            if (span.Style.HasFlag(ProseInline.Style.Strikethrough)) { open.Append("<s>"); close.Insert(0, "</s>"); }

            sb.Append(open);
            foreach (var c in Escape(span.Text).Replace("\n", "<br>"))
            {
                var index = c - FirstPlaceholder;
                if (chips.Count > 0 && index >= 0 && index < chips.Count) sb.Append(chips[index]);
                else sb.Append(c);
            }
            sb.Append(close);
        }
    }

    private static string Escape(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
