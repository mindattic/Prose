using System.Text;
using System.Text.RegularExpressions;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Renders wiki-style [[Entity Name]] and [[Entity Name|display]] markup into
/// clickable anchors that target /entity/{id}. The graph resolves the name to
/// an id; when it can't, the literal text inside the brackets is preserved so
/// unfinished references don't render as broken links.
///
/// Two modes:
///   • <see cref="ToHtml"/>  — wraps matches in HTML &lt;a&gt; tags.
///   • <see cref="Extract"/> — returns a structured list of segments so a Razor
///                             component can render real Blazor &lt;NavLink&gt;.
///
/// Markup grows organically: writers add [[]] only when they want a link. Future
/// passes can auto-suggest brackets for high-confidence name matches.
/// </summary>
public class WikiLinkService
{
    private readonly WorldGraphService graph;
    private static readonly Regex Pattern =
        new(@"\[\[([^\]|]+?)(?:\|([^\]]+?))?\]\]", RegexOptions.Compiled);

    public WikiLinkService(WorldGraphService graph)
    {
        this.graph = graph;
    }

    /// <summary>
    /// Replace [[Name]] markup with HTML anchors. URL priority:
    ///   1. /entity/{guid7-id}   — when the name resolves to a node in the graph
    ///   2. /entity/stub/{slug}  — when no node exists yet (links to a stub flow)
    ///   3. plain text           — only when the name slugifies to nothing
    /// Safe to inject as MarkupString.
    /// </summary>
    public string ToHtml(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        graph.EnsureLoaded();
        return Pattern.Replace(text, m =>
        {
            var name = m.Groups[1].Value.Trim();
            var display = m.Groups[2].Success ? m.Groups[2].Value.Trim() : name;
            var safeDisplay = System.Net.WebUtility.HtmlEncode(display);

            var id = graph.ResolveId(name);
            if (!string.IsNullOrEmpty(id))
                return $"<a class=\"wiki-link\" href=\"/entity/{Uri.EscapeDataString(id)}\" title=\"Open {System.Net.WebUtility.HtmlEncode(name)}\">{safeDisplay}</a>";

            var slug = WorldGraphService.Slugify(name);
            if (!string.IsNullOrEmpty(slug))
                return $"<a class=\"wiki-link wiki-link-stub\" href=\"/entity/stub/{Uri.EscapeDataString(slug)}\" title=\"Stub: {System.Net.WebUtility.HtmlEncode(name)} (not yet in canon)\">{safeDisplay}</a>";

            return safeDisplay;
        });
    }

    /// <summary>Decompose text into ordered segments — text vs. resolved link vs. dangling reference.</summary>
    public List<WikiSegment> Extract(string? text)
    {
        var segments = new List<WikiSegment>();
        if (string.IsNullOrEmpty(text)) return segments;
        graph.EnsureLoaded();

        int cursor = 0;
        foreach (Match m in Pattern.Matches(text))
        {
            if (m.Index > cursor)
                segments.Add(new WikiSegment(WikiSegmentKind.Text, text[cursor..m.Index], null, null, null));

            var name = m.Groups[1].Value.Trim();
            var display = m.Groups[2].Success ? m.Groups[2].Value.Trim() : name;
            var id = graph.ResolveId(name);
            string? slug = id == null ? WorldGraphService.Slugify(name) : null;
            var kind = id != null
                ? WikiSegmentKind.Link
                : (string.IsNullOrEmpty(slug) ? WikiSegmentKind.Dangling : WikiSegmentKind.Stub);
            segments.Add(new WikiSegment(
                Kind:       kind,
                Text:       display,
                EntityName: name,
                EntityId:   id,
                StubSlug:   slug));
            cursor = m.Index + m.Length;
        }
        if (cursor < text.Length)
            segments.Add(new WikiSegment(WikiSegmentKind.Text, text[cursor..], null, null, null));

        return segments;
    }

    /// <summary>
    /// Auto-link unbracketed mentions of known entity names in plain text. Conservative:
    /// only links exact name matches, longest-first, skipping anything inside an existing
    /// [[link]]. Useful for diagnostic surfaces (dossier viewer) — NOT for the writer's
    /// prose page, where the writer chooses what to link.
    /// </summary>
    public string AutoLink(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        graph.EnsureLoaded();

        // Tokenize already-bracketed regions so we don't double-wrap.
        var protectedRanges = new List<(int Start, int End)>();
        foreach (Match m in Pattern.Matches(text))
            protectedRanges.Add((m.Index, m.Index + m.Length));

        // Sort entity names longest-first to prefer specific matches.
        var nodes = graph.AllNodes()
            .Where(n => !string.IsNullOrWhiteSpace(n.Name) && n.Name.Length >= 3)
            .OrderByDescending(n => n.Name.Length)
            .ToList();

        // Build replacements as (index, length, replacement) tuples then apply right-to-left.
        var replacements = new List<(int Index, int Length, string With)>();
        foreach (var n in nodes)
        {
            var nameRegex = new Regex(@"\b" + Regex.Escape(n.Name) + @"\b", RegexOptions.IgnoreCase);
            foreach (Match m in nameRegex.Matches(text))
            {
                // Skip any match that OVERLAPS a protected region — not just one
                // whose start falls inside it. A match beginning before a range
                // but extending into it would otherwise yield overlapping
                // replacements and corrupt the string during the right-to-left apply.
                if (protectedRanges.Any(r => m.Index < r.End && m.Index + m.Length > r.Start)) continue;
                replacements.Add((m.Index, m.Length, $"[[{n.Name}|{m.Value}]]"));
                protectedRanges.Add((m.Index, m.Index + m.Length));
            }
        }

        replacements.Sort((a, b) => b.Index.CompareTo(a.Index));
        var sb = new StringBuilder(text);
        foreach (var (idx, len, with) in replacements)
        {
            sb.Remove(idx, len);
            sb.Insert(idx, with);
        }
        return ToHtml(sb.ToString());
    }
}

public enum WikiSegmentKind { Text, Link, Stub, Dangling }

public sealed record WikiSegment(
    WikiSegmentKind Kind,
    string Text,
    string? EntityName,
    string? EntityId,
    string? StubSlug);
