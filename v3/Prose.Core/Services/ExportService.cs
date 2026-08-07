using System.Text;
using System.Text.RegularExpressions;

namespace Prose.Core.Services;

/// <summary>
/// Converts story HTML to various export formats (plain text, markdown, print-ready HTML for PDF).
/// </summary>
public partial class ExportService
{
    /// <summary>Export story as plain text with title header.</summary>
    public string ToPlainText(string title, string html)
    {
        var text = StripHtml(html);
        return $"{title}\n\n{text}";
    }

    /// <summary>Export story as Markdown. Converts HTML structure to MD syntax.</summary>
    public string ToMarkdown(string title, string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return $"# {title}\n";

        var md = html;

        // Chapter breaks
        md = md.Replace("<hr class=\"chapter-break-hr\"", "<hr");

        // Headings
        md = Regex.Replace(md, @"<h1[^>]*>(.*?)</h1>", m => $"\n# {Strip(m.Groups[1].Value)}\n", RegexOptions.Singleline);
        md = Regex.Replace(md, @"<h2[^>]*>(.*?)</h2>", m => $"\n## {Strip(m.Groups[1].Value)}\n", RegexOptions.Singleline);
        md = Regex.Replace(md, @"<h3[^>]*>(.*?)</h3>", m => $"\n### {Strip(m.Groups[1].Value)}\n", RegexOptions.Singleline);

        // Bold, italic, underline, strikethrough
        md = Regex.Replace(md, @"<(b|strong)[^>]*>(.*?)</\1>", "**$2**", RegexOptions.Singleline);
        md = Regex.Replace(md, @"<(i|em)[^>]*>(.*?)</\1>", "*$2*", RegexOptions.Singleline);
        md = Regex.Replace(md, @"<(u)[^>]*>(.*?)</\1>", "<u>$2</u>", RegexOptions.Singleline);
        md = Regex.Replace(md, @"<(s|strike|del)[^>]*>(.*?)</\1>", "~~$2~~", RegexOptions.Singleline);

        // Blockquotes
        md = Regex.Replace(md, @"<blockquote[^>]*>(.*?)</blockquote>", m =>
        {
            var inner = Strip(m.Groups[1].Value);
            return string.Join("\n", inner.Split('\n').Select(l => $"> {l.Trim()}"));
        }, RegexOptions.Singleline);

        // Horizontal rules
        md = Regex.Replace(md, @"<hr[^>]*/?>", "\n---\n");

        // List items
        md = Regex.Replace(md, @"<li[^>]*>(.*?)</li>", m => $"- {Strip(m.Groups[1].Value).Trim()}", RegexOptions.Singleline);
        md = Regex.Replace(md, @"</?[ou]l[^>]*>", "");

        // Paragraphs and divs to double newlines
        md = Regex.Replace(md, @"<p[^>]*>(.*?)</p>", m => $"\n{Strip(m.Groups[1].Value).Trim()}\n", RegexOptions.Singleline);
        md = Regex.Replace(md, @"<div[^>]*>(.*?)</div>", m => $"\n{m.Groups[1].Value}\n", RegexOptions.Singleline);

        // Line breaks
        md = Regex.Replace(md, @"<br\s*/?>", "\n");

        // Images
        md = Regex.Replace(md, @"<img[^>]*src=""([^""]*)""\s*alt=""([^""]*)""[^>]*/?>", "![$2]($1)");
        md = Regex.Replace(md, @"<img[^>]*src=""([^""]*)""[^>]*/?>", "![]($1)");

        // Entity links - keep display text
        md = Regex.Replace(md, @"<span[^>]*class=""entity-link[^""]*""[^>]*>(.*?)</span>", "$1", RegexOptions.Singleline);

        // Facet tags - keep text
        md = Regex.Replace(md, @"<span[^>]*class=""facet-tag""[^>]*>(.*?)</span>", "$1", RegexOptions.Singleline);

        // ElevenLabs tags - strip
        md = Regex.Replace(md, @"<span[^>]*class=""elevenlabs-tag""[^>]*>.*?</span>", "", RegexOptions.Singleline);

        // Strip remaining HTML tags
        md = Regex.Replace(md, @"<[^>]+>", "");

        // Decode HTML entities
        md = md.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">")
               .Replace("&quot;", "\"").Replace("&#39;", "'").Replace("&nbsp;", " ")
               .Replace("&#9835;", "");

        // Clean up excessive newlines
        md = Regex.Replace(md, @"\n{3,}", "\n\n").Trim();

        return $"# {title}\n\n{md}";
    }

    /// <summary>Generate a self-contained HTML document styled for PDF printing.</summary>
    public string ToPrintHtml(string title, string html, List<string>? characters = null)
    {
        var safeTitle = System.Web.HttpUtility.HtmlEncode(title);
        var charLine = characters?.Count > 0
            ? $"<p style=\"color:#888;font-size:0.9rem;margin-bottom:2rem;\">Characters: {string.Join(", ", characters)}</p>"
            : "";

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset=\"utf-8\" />");
        sb.AppendLine($"<title>{safeTitle}</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("@page { margin: 1in; }");
        sb.AppendLine("body { font-family: Georgia, 'Times New Roman', serif; font-size: 12pt; line-height: 1.8; color: #1a1a1a; max-width: 6.5in; margin: 0 auto; padding: 1in; }");
        sb.AppendLine("h1 { font-size: 2rem; margin-bottom: 0.5rem; color: #333; }");
        sb.AppendLine("h2 { font-size: 1.4rem; margin-top: 2rem; color: #444; }");
        sb.AppendLine("h3 { font-size: 1.2rem; margin-top: 1.5rem; color: #555; }");
        sb.AppendLine("p { margin-bottom: 1rem; text-indent: 1.5em; }");
        sb.AppendLine("p:first-of-type { text-indent: 0; }");
        sb.AppendLine("blockquote { border-left: 3px solid #ccc; padding-left: 1rem; margin-left: 0; color: #555; font-style: italic; }");
        sb.AppendLine("hr { border: none; border-top: 1px solid #ccc; margin: 2rem 0; }");
        sb.AppendLine(".entity-link { font-weight: inherit; color: inherit; cursor: default; }");
        sb.AppendLine(".elevenlabs-tag, .facet-tag { display: none; }");
        sb.AppendLine("img { max-width: 100%; }");
        sb.AppendLine("@media print { body { padding: 0; } }");
        sb.AppendLine("</style></head><body>");
        sb.AppendLine($"<h1>{safeTitle}</h1>");
        sb.AppendLine(charLine);
        sb.AppendLine(html);
        sb.AppendLine("<script>window.onload = function() { window.print(); }</script>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static string StripHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return "";
        var text = Regex.Replace(html, @"<br\s*/?>", "\n");
        text = Regex.Replace(text, @"</p>|</div>|</h[1-6]>", "\n");
        text = Regex.Replace(text, @"<[^>]+>", "");
        text = text.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">")
                   .Replace("&quot;", "\"").Replace("&#39;", "'").Replace("&nbsp;", " ");
        return Regex.Replace(text, @"\n{3,}", "\n\n").Trim();
    }

    private static string Strip(string html) => Regex.Replace(html, @"<[^>]+>", "").Trim();
}
