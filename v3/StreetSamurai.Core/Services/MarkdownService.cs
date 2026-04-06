using System.Text.RegularExpressions;
using Markdig;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Renders story markdown to HTML with custom extensions:
///   - Facet tags: [WOUND], [IDEAL], etc → stripped (legacy format, inner thoughts are now italicized prose)
///   - Entity refs: {{Sable}} or {{entity:sable|Sable}} → clickable spans that fire JS
///   - ElevenLabs tags: {pause:500}, {emotion:whisper} → visual indicators in Rich, stripped in Print
///   - Chapter breaks: ====== → styled dividers
/// </summary>
public partial class MarkdownService
{
    private readonly MarkdownPipeline pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    /// <summary>Render markdown to rich HTML with all custom tags active (for Rich view).</summary>
    public string RenderToHtml(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return "";

        var html = Markdown.ToHtml(markdown, pipeline);

        // Facet tags → strip brackets (legacy format — inner thoughts are now italicized prose)
        html = FacetTagRegex().Replace(html, "");

        // Entity refs: {{entity:id|Display Name}} or {{Display Name}}
        html = EntityRefFullRegex().Replace(html, match =>
        {
            var id = match.Groups[1].Value;
            var display = match.Groups[2].Value;
            return $"<span class=\"entity-link entity-character\" data-entity-id=\"{id}\" style=\"cursor:pointer;\">{display}</span>";
        });
        html = EntityRefSimpleRegex().Replace(html, match =>
        {
            var name = match.Groups[1].Value;
            var id = Slugify(name);
            return $"<span class=\"entity-link entity-character\" data-entity-id=\"{id}\" style=\"cursor:pointer;\">{name}</span>";
        });

        // ElevenLabs tags: {pause:500}, {emotion:whisper}, {break:1s}
        html = ElevenLabsTagRegex().Replace(html, match =>
        {
            var tag = match.Groups[1].Value;
            var value = match.Groups[2].Value;
            return $"<span class=\"elevenlabs-tag\" style=\"color:#e83e8c;font-size:0.75rem;background:rgba(232,62,140,0.1);border-radius:3px;padding:0 3px;\" title=\"ElevenLabs: {tag}:{value}\">&#9835; {tag}:{value}</span>";
        });

        // Chapter breaks: ====== → styled dividers
        html = html.Replace("<p>======</p>", "<hr class=\"chapter-break-hr\" style=\"border-color:#dc3545;margin:2rem 0;border-width:2px;\">");

        return html;
    }

    /// <summary>Render markdown to clean HTML for printing — no entity links, no ElevenLabs, no facet colors.</summary>
    public string RenderToPrintHtml(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return "";

        // Strip custom tags before rendering
        var clean = markdown;
        clean = EntityRefFullRegex().Replace(clean, "$2"); // keep display name only
        clean = EntityRefSimpleRegex().Replace(clean, "$1"); // keep name only
        clean = ElevenLabsTagRegex().Replace(clean, ""); // strip entirely
        clean = FacetTagRegex().Replace(clean, ""); // strip facet tags for print

        var html = Markdown.ToHtml(clean, pipeline);

        // Chapter breaks → simple centered text
        html = html.Replace("<p>======</p>", "<div style=\"text-align:center;margin:3rem 0;font-size:1.5rem;\">* * *</div>");

        return html;
    }

    /// <summary>Strip all custom markup from markdown, returning plain text suitable for TTS or export.</summary>
    public string StripToPlainText(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return "";
        var clean = markdown;
        clean = EntityRefFullRegex().Replace(clean, "$2");
        clean = EntityRefSimpleRegex().Replace(clean, "$1");
        clean = ElevenLabsTagRegex().Replace(clean, "");
        clean = FacetTagRegex().Replace(clean, "");
        clean = clean.Replace("======", "---");
        return clean;
    }

    public string StripFrontMatter(string markdown)
    {
        if (!markdown.StartsWith("---")) return markdown;
        var end = markdown.IndexOf("---", 3, StringComparison.Ordinal);
        return end < 0 ? markdown : markdown[(end + 3)..].TrimStart('\r', '\n');
    }

    private static string Slugify(string name) =>
        Regex.Replace(name.ToLowerInvariant().Trim(), @"[^a-z0-9]+", "_").Trim('_');

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

    // {{entity:sable|Sable}} — explicit entity ref with ID
    [GeneratedRegex(@"\{\{entity:([a-z0-9_]+)\|([^}]+)\}\}")]
    private static partial Regex EntityRefFullRegex();

    // {{Sable}} — simple entity ref (auto-slugified)
    [GeneratedRegex(@"\{\{([^}|]+)\}\}")]
    private static partial Regex EntityRefSimpleRegex();

    // {pause:500}, {emotion:whisper}, {break:1s}
    [GeneratedRegex(@"\{(pause|emotion|break|speed|pitch|volume):([^}]+)\}")]
    private static partial Regex ElevenLabsTagRegex();
}
