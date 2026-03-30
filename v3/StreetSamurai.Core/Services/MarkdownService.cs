using System.Text.RegularExpressions;
using Markdig;

namespace StreetSamurai.Core.Services;

public partial class MarkdownService
{
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public string RenderToHtml(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return "";

        var html = Markdown.ToHtml(markdown, _pipeline);
        // Render facet tags as colored badges
        html = FacetTagRegex().Replace(html, match =>
        {
            var facet = match.Groups[1].Value.ToUpperInvariant();
            var color = GetFacetColor(facet);
            return $"<span class=\"facet-tag\" style=\"color:{color};font-weight:bold;\">[{facet}]</span>";
        });
        return html;
    }

    public string StripFrontMatter(string markdown)
    {
        if (!markdown.StartsWith("---")) return markdown;
        var end = markdown.IndexOf("---", 3, StringComparison.Ordinal);
        return end < 0 ? markdown : markdown[(end + 3)..].TrimStart('\r', '\n');
    }

    private static string GetFacetColor(string facet) => facet switch
    {
        "WOUND" => "#dc3545",
        "IDEAL" => "#198754",
        "ID" => "#ffc107",
        "SHADOW" => "#6f42c1",
        "MASK" => "#0dcaf0",
        "GHOST" => "#6c757d",
        _ => "#adb5bd",
    };

    [GeneratedRegex(@"\[(WOUND|IDEAL|ID|SHADOW|MASK|GHOST)\]", RegexOptions.IgnoreCase)]
    private static partial Regex FacetTagRegex();
}
