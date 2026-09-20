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

    private static string RenderInline(string text)
    {
        var sb = new StringBuilder();
        var cursor = 0;

        foreach (Match m in TagPattern.Matches(text))
        {
            AppendFormatted(sb, text[cursor..m.Index]);

            var guid = GuidAttr.Match(m.Value).Groups[1].Value;
            var repo = RepoAttr.Match(m.Value).Groups[1].Value;
            var inner = m.Groups[1].Value;

            // A malformed tag still shows its words. The prose has to stay readable even when the
            // markup around it is wrong — it just doesn't become an interactive entity.
            if (Guid.TryParse(guid, out var id) && !inner.Contains('<'))
            {
                sb.Append("<span class=\"ent\" contenteditable=\"false\"")
                  .Append(" data-guid=\"").Append(Escape(id.ToString()))
                  .Append("\" data-repo=\"").Append(Escape(repo))
                  .Append("\">").Append(Escape(inner)).Append("</span>");
            }
            else
            {
                AppendFormatted(sb, inner);
            }

            cursor = m.Index + m.Length;
        }

        AppendFormatted(sb, text[cursor..]);
        return sb.Length == 0 ? "<br>" : sb.ToString();
    }

    private static void AppendFormatted(StringBuilder sb, string text)
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

            sb.Append(open).Append(Escape(span.Text).Replace("\n", "<br>")).Append(close);
        }
    }

    private static string Escape(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
