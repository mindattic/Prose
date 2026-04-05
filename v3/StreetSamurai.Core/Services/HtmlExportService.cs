using System.Text;
using System.Text.Json;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Exports repository data as self-contained .htm files with inline CSS/JS.
/// Three modes: single entry, full repo, and complete export (all repos).
/// Output is uploadable HTML with linked TOC, filter search, and cyberpunk styling.
/// </summary>
public class HtmlExportService
{
    private readonly IPathProvider paths;
    private readonly SettingsService settings;

    public HtmlExportService(IPathProvider paths, SettingsService settings)
    {
        this.paths = paths;
        this.settings = settings;
    }

    public string ExportDir => Path.Combine(paths.DataRoot, "exports");

    /// <summary>Export a single JSON entity as a standalone .htm file.</summary>
    public string ExportEntry(string repoName, string entryName, string jsonContent)
    {
        Directory.CreateDirectory(ExportDir);
        var html = BuildPage($"{entryName} — {repoName}", BuildEntryHtml(entryName, jsonContent), false);
        var fileName = $"{Slugify(repoName)}_{Slugify(entryName)}.htm";
        var filePath = Path.Combine(ExportDir, fileName);
        File.WriteAllText(filePath, html);
        return filePath;
    }

    /// <summary>
    /// Export an entire repo as a single .htm file with TOC and filter.
    /// Returns the file path. Calls onProgress(current, total) for each entry.
    /// </summary>
    public string ExportRepo(string repoName, List<(string name, string json)> entries, Action<int, int>? onProgress = null)
    {
        Directory.CreateDirectory(ExportDir);
        var sorted = entries.OrderBy(e => e.name, StringComparer.OrdinalIgnoreCase).ToList();
        var sb = new StringBuilder();

        // TOC
        sb.AppendLine("<div class=\"toc\">");
        sb.AppendLine($"<h2>{repoName} — {sorted.Count} Entries</h2>");
        sb.AppendLine("<ul>");
        foreach (var (name, _) in sorted)
            sb.AppendLine($"<li><a href=\"#{Slugify(name)}\">{Esc(name)}</a></li>");
        sb.AppendLine("</ul></div>");

        // Entries
        for (int i = 0; i < sorted.Count; i++)
        {
            var (name, json) = sorted[i];
            sb.AppendLine($"<div class=\"entry\" id=\"{Slugify(name)}\">");
            sb.Append(BuildEntryHtml(name, json));
            sb.AppendLine("<a href=\"#top\" class=\"back-top\">Back to top</a>");
            sb.AppendLine("</div>");
            onProgress?.Invoke(i + 1, sorted.Count);
        }

        var html = BuildPage($"{repoName} — StreetSamurai Canon Export", sb.ToString(), true);
        var fileName = $"{Slugify(repoName)}.htm";
        var filePath = Path.Combine(ExportDir, fileName);
        File.WriteAllText(filePath, html);
        return filePath;
    }

    /// <summary>
    /// Export ALL repos into individual .htm files plus a master index.htm.
    /// Returns the index file path. Calls onProgress(current, total) across all entries.
    /// </summary>
    public string ExportAll(Dictionary<string, List<(string name, string json)>> repos, Action<int, int>? onProgress = null)
    {
        Directory.CreateDirectory(ExportDir);
        var totalEntries = repos.Values.Sum(r => r.Count);
        int processed = 0;

        var repoFiles = new List<(string repoName, string fileName, int count)>();

        foreach (var (repoName, entries) in repos.OrderBy(r => r.Key))
        {
            var sorted = entries.OrderBy(e => e.name, StringComparer.OrdinalIgnoreCase).ToList();
            var sb = new StringBuilder();

            sb.AppendLine("<div class=\"toc\">");
            sb.AppendLine($"<h2>{repoName} — {sorted.Count} Entries</h2>");
            sb.AppendLine("<p><a href=\"index.htm\">Back to Master Index</a></p>");
            sb.AppendLine("<ul>");
            foreach (var (name, _) in sorted)
                sb.AppendLine($"<li><a href=\"#{Slugify(name)}\">{Esc(name)}</a></li>");
            sb.AppendLine("</ul></div>");

            for (int i = 0; i < sorted.Count; i++)
            {
                var (name, json) = sorted[i];
                sb.AppendLine($"<div class=\"entry\" id=\"{Slugify(name)}\">");
                sb.Append(BuildEntryHtml(name, json));
                sb.AppendLine("<a href=\"#top\" class=\"back-top\">Back to top</a>");
                sb.AppendLine("</div>");
                processed++;
                onProgress?.Invoke(processed, totalEntries);
            }

            var html = BuildPage($"{repoName} — StreetSamurai Canon Export", sb.ToString(), true);
            var fileName = $"{Slugify(repoName)}.htm";
            File.WriteAllText(Path.Combine(ExportDir, fileName), html);
            repoFiles.Add((repoName, fileName, sorted.Count));
        }

        // Master index
        var indexSb = new StringBuilder();
        indexSb.AppendLine("<div class=\"toc\">");
        indexSb.AppendLine($"<h2>StreetSamurai Canon — Complete Export</h2>");
        indexSb.AppendLine($"<p>{repos.Count} repositories, {totalEntries} total entries</p>");
        indexSb.AppendLine("<ul>");
        foreach (var (repoName, fileName, count) in repoFiles)
            indexSb.AppendLine($"<li><a href=\"{fileName}\">{Esc(repoName)}</a> <span class=\"badge\">{count}</span></li>");
        indexSb.AppendLine("</ul></div>");

        var indexHtml = BuildPage("StreetSamurai Canon — Master Index", indexSb.ToString(), false);
        var indexPath = Path.Combine(ExportDir, "index.htm");
        File.WriteAllText(indexPath, indexHtml);
        return indexPath;
    }

    private static string BuildEntryHtml(string name, string jsonContent)
    {
        var slug = Slugify(name);
        var sb = new StringBuilder();
        sb.AppendLine($"<h3><span>{Esc(name)}</span><span class=\"entry-links\"><a href=\"#{slug}\" class=\"permalink\" title=\"Link to this entry\">#</a><span class=\"copy-link\" onclick=\"copyLink('{slug}')\" title=\"Copy link\">&#x1F4CB;</span></span></h3>");

        try
        {
            var doc = JsonDocument.Parse(jsonContent);
            sb.AppendLine("<table class=\"fields\">");
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var key = prop.Name;
                var val = prop.Value;

                sb.AppendLine("<tr>");
                sb.AppendLine($"<td class=\"key\">{Esc(PrettyKey(key))}</td>");
                sb.AppendLine($"<td class=\"val\">{FormatValue(val)}</td>");
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</table>");
        }
        catch
        {
            sb.AppendLine($"<pre>{Esc(jsonContent)}</pre>");
        }

        return sb.ToString();
    }

    private static string FormatValue(JsonElement val)
    {
        return val.ValueKind switch
        {
            JsonValueKind.String => FormatString(val.GetString() ?? ""),
            JsonValueKind.Number => $"<span class=\"num\">{val}</span>",
            JsonValueKind.True => "<span class=\"bool\">true</span>",
            JsonValueKind.False => "<span class=\"bool\">false</span>",
            JsonValueKind.Null => "<span class=\"null\">—</span>",
            JsonValueKind.Array => FormatArray(val),
            JsonValueKind.Object => FormatObject(val),
            _ => Esc(val.ToString()),
        };
    }

    private static string FormatString(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "<span class=\"null\">—</span>";
        var escaped = Esc(s);
        // Convert newlines to HTML breaks for readability
        escaped = escaped.Replace("\\n", "<br>").Replace("\n", "<br>");
        return escaped;
    }

    private static string FormatArray(JsonElement arr)
    {
        var items = arr.EnumerateArray().ToList();
        if (items.Count == 0) return "<span class=\"null\">—</span>";
        if (items.All(i => i.ValueKind == JsonValueKind.String))
        {
            var sb = new StringBuilder("<ul class=\"compact\">");
            foreach (var item in items)
                sb.AppendLine($"<li>{Esc(item.GetString() ?? "")}</li>");
            sb.Append("</ul>");
            return sb.ToString();
        }
        // Complex array — render as sub-tables
        var csb = new StringBuilder();
        foreach (var item in items)
            csb.AppendLine($"<div class=\"sub-entry\">{FormatValue(item)}</div>");
        return csb.ToString();
    }

    private static string FormatObject(JsonElement obj)
    {
        var sb = new StringBuilder("<table class=\"sub-fields\">");
        foreach (var prop in obj.EnumerateObject())
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td class=\"key\">{Esc(PrettyKey(prop.Name))}</td>");
            sb.AppendLine($"<td class=\"val\">{FormatValue(prop.Value)}</td>");
            sb.AppendLine("</tr>");
        }
        sb.Append("</table>");
        return sb.ToString();
    }

    private string BuildPage(string title, string body, bool includeFilter)
    {
        var filterHtml = includeFilter ? @"
<div class=""filter-bar"" id=""filterBar"">
    <input type=""text"" id=""filterInput"" placeholder=""Filter entries..."" oninput=""filterEntries()"" />
    <span class=""filter-clear"" id=""filterClear"" onclick=""clearFilter()"" title=""Clear"">&times;</span>
</div>" : "";

        var filterJs = includeFilter ? @"
function filterEntries() {
    var q = document.getElementById('filterInput').value.toLowerCase();
    var entries = document.querySelectorAll('.entry');
    var clearBtn = document.getElementById('filterClear');
    clearBtn.style.display = q.length > 0 ? 'block' : 'none';
    entries.forEach(function(e) {
        e.style.display = e.textContent.toLowerCase().indexOf(q) !== -1 ? '' : 'none';
    });
    // Also filter TOC
    var tocItems = document.querySelectorAll('.toc li');
    tocItems.forEach(function(li) {
        var link = li.querySelector('a');
        if (!link) return;
        var targetId = link.getAttribute('href').substring(1);
        var target = document.getElementById(targetId);
        li.style.display = (!target || target.style.display !== 'none') ? '' : 'none';
    });
}
function clearFilter() {
    document.getElementById('filterInput').value = '';
    filterEntries();
}
function copyLink(slug) {
    var url = location.href.split('#')[0] + '#' + slug;
    navigator.clipboard.writeText(url).then(function() {
        var el = event.target;
        el.textContent = '\u2705';
        setTimeout(function() { el.textContent = '\uD83D\uDCCB'; }, 1500);
    });
}" : "";

        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1"">
<title>{Esc(title)}</title>
<style>
:root {{ --bg: #0d1117; --surface: #161b22; --border: #30363d; --text: #e6edf3; --muted: #8b949e; --accent: #dc3545; --link: #58a6ff; --key-bg: #1c2128; }}
* {{ margin: 0; padding: 0; box-sizing: border-box; }}
body {{ background: var(--bg); color: var(--text); font-family: 'Segoe UI', system-ui, -apple-system, sans-serif; font-size: 14px; line-height: 1.6; padding: 20px clamp(12px, 4vw, 40px); max-width: 1400px; margin: 0 auto; }}
h1 {{ color: var(--accent); font-size: clamp(1.2rem, 4vw, 1.8rem); margin-bottom: 4px; }}
h2 {{ color: var(--accent); font-size: clamp(1rem, 3vw, 1.3rem); margin-bottom: 12px; }}
h3 {{ color: var(--accent); font-size: clamp(0.95rem, 2.5vw, 1.1rem); margin-bottom: 8px; padding-top: 16px; border-top: 1px solid var(--border); }}
a {{ color: var(--link); text-decoration: none; }} a:hover {{ text-decoration: underline; }}
.header {{ margin-bottom: 24px; padding-bottom: 12px; border-bottom: 2px solid var(--accent); }}
.header small {{ color: var(--muted); }}
.toc {{ background: var(--surface); border: 1px solid var(--border); border-radius: 6px; padding: 16px 20px; margin-bottom: 24px; }}
.toc ul {{ columns: 3; column-gap: 24px; list-style: none; }}
.toc li {{ padding: 2px 0; font-size: 13px; break-inside: avoid; }}
.toc .badge {{ background: var(--border); color: var(--muted); border-radius: 10px; padding: 1px 8px; font-size: 11px; margin-left: 4px; }}
.entry {{ background: var(--surface); border: 1px solid var(--border); border-radius: 6px; padding: clamp(10px, 3vw, 20px); margin-bottom: 16px; overflow-x: auto; }}
.fields, .sub-fields {{ width: 100%; border-collapse: collapse; }}
.fields td, .sub-fields td {{ padding: 4px 10px; vertical-align: top; border-bottom: 1px solid var(--border); font-size: 13px; word-break: break-word; }}
.key {{ color: var(--muted); white-space: nowrap; width: 160px; background: var(--key-bg); font-weight: 600; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; }}
.sub-fields .key {{ width: 120px; font-size: 11px; }}
.val {{ color: var(--text); }}
.num {{ color: #79c0ff; }}
.bool {{ color: #7ee787; }}
.null {{ color: var(--muted); font-style: italic; }}
ul.compact {{ list-style: disc; margin-left: 16px; }}
ul.compact li {{ font-size: 13px; padding: 1px 0; }}
.sub-entry {{ border-left: 3px solid var(--border); padding-left: 12px; margin: 6px 0; }}
.back-top {{ display: inline-block; margin-top: 8px; font-size: 12px; color: var(--muted); }}
h3 {{ display: flex; align-items: center; flex-wrap: nowrap; }}
.entry-links {{ margin-left: auto; display: flex; gap: 4px; flex-shrink: 0; }}
.permalink {{ color: var(--border); font-size: 1.1em; text-decoration: none; opacity: 0.25; transition: opacity 0.2s; padding: 0 4px; }}
.permalink:hover {{ opacity: 0.6; color: var(--muted); text-decoration: none; }}
.copy-link {{ cursor: pointer; font-size: 1.1em; opacity: 0.25; padding: 0 4px; transition: opacity 0.2s; }}
.copy-link:hover {{ opacity: 0.6; }}
.entry:target {{ border-color: var(--accent); box-shadow: 0 0 0 1px var(--accent); }}
.filter-bar {{ position: sticky; top: 0; z-index: 100; background: var(--bg); padding: 10px 0; margin-bottom: 16px; display: flex; align-items: center; }}
.filter-bar input {{ flex: 1; background: var(--surface); color: var(--text); border: 1px solid var(--border); border-radius: 4px; padding: 8px 12px; font-size: 16px; outline: none; -webkit-appearance: none; }}
.filter-bar input:focus {{ border-color: var(--accent); }}
.filter-clear {{ display: none; position: relative; right: 30px; cursor: pointer; color: var(--muted); font-size: 18px; width: 0; }}
.filter-clear:hover {{ color: var(--accent); }}
@media (max-width: 768px) {{
    .toc ul {{ columns: 1; }}
    .fields td, .sub-fields td {{ display: block; width: 100%; padding: 3px 8px; }}
    .key {{ white-space: normal; width: 100%; border-bottom: none; padding-bottom: 0; }}
    .val {{ padding-top: 0; }}
    .fields tr, .sub-fields tr {{ display: block; border-bottom: 1px solid var(--border); padding: 6px 0; }}
    .sub-fields .key {{ width: 100%; }}
}}
@media (max-width: 480px) {{
    body {{ padding: 10px 8px; font-size: 13px; }}
    .entry {{ padding: 10px; }}
    h3 {{ font-size: 0.95rem; }}
}}
@media print {{ .filter-bar, .back-top {{ display: none; }} .entry {{ break-inside: avoid; }} }}
</style>
</head>
<body id=""top"">
<div class=""header"">
<h1>{Esc(title)}</h1>
<small>Exported from StreetSamurai Canon Engine — {settings.FormatTimestamp(DateTime.Now)}</small>
</div>
{filterHtml}
{body}
<script>{filterJs}</script>
</body>
</html>";
    }

    private static string PrettyKey(string key) =>
        key.Replace("_", " ").Replace("-", " ");

    private static string Esc(string s) =>
        System.Net.WebUtility.HtmlEncode(s);

    private static string Slugify(string s) =>
        System.Text.RegularExpressions.Regex.Replace(s.ToLowerInvariant(), @"[^a-z0-9]+", "_").Trim('_');
}
