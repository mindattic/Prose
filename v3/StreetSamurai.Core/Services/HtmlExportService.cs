using System.Text;
using System.Text.Json;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Exports repository data as a self-contained web encyclopedia.
/// Generates interlinked .htm files with sidebar navigation, cross-references,
/// global search, tag filtering, and the StreetSamurai visual identity.
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

    public string ExportDir => paths.ExportDir;

    /// <summary>Export a single JSON entity as a standalone .htm file.</summary>
    public string ExportEntry(string repoName, string entryName, string jsonContent)
    {
        Directory.CreateDirectory(ExportDir);
        EnsureSharedAssets();
        var html = BuildPage($"{entryName} — {repoName}", BuildEntryHtml(entryName, jsonContent), false, null);
        var fileName = $"{Slugify(repoName)}_{Slugify(entryName)}.htm";
        var filePath = Path.Combine(ExportDir, fileName);
        File.WriteAllText(filePath, html);
        return filePath;
    }

    /// <summary>
    /// Export an entire repo as a single .htm file with TOC and filter.
    /// </summary>
    public string ExportRepo(string repoName, List<(string name, string json)> entries, Action<int, int>? onProgress = null)
    {
        Directory.CreateDirectory(ExportDir);
        EnsureSharedAssets();
        var sorted = entries.OrderBy(e => e.name, StringComparer.OrdinalIgnoreCase).ToList();
        var sb = new StringBuilder();

        sb.AppendLine("<div class=\"toc\" id=\"tocPanel\">");
        sb.AppendLine($"<h2>{RepoIcon(repoName)}{repoName} — {sorted.Count} Entries</h2>");
        sb.AppendLine("<ul>");
        foreach (var (name, _) in sorted)
            sb.AppendLine($"<li><a href=\"#{Slugify(name)}\">{Esc(name)}</a></li>");
        sb.AppendLine("</ul></div>");

        for (int i = 0; i < sorted.Count; i++)
        {
            var (name, json) = sorted[i];
            sb.AppendLine($"<a class=\"entry-anchor\" id=\"{Slugify(name)}\"></a>");
            sb.AppendLine($"<div class=\"entry\" data-tags=\"{ExtractTags(json)}\" data-name=\"{Esc(name)}\">");
            sb.Append(BuildEntryHtml(name, json));
            sb.AppendLine("</div>");
            onProgress?.Invoke(i + 1, sorted.Count);
        }

        var html = BuildPage($"{repoName}", sb.ToString(), true, null);
        var fileName = $"{Slugify(repoName)}.htm";
        File.WriteAllText(Path.Combine(ExportDir, fileName), html);
        return Path.Combine(ExportDir, fileName);
    }

    /// <summary>
    /// Export ALL repos into individual .htm files plus a master index.htm.
    /// Generates a cross-referenced encyclopedia with sidebar navigation.
    /// </summary>
    public string ExportAll(Dictionary<string, List<(string name, string json)>> repos, Action<int, int>? onProgress = null)
    {
        Directory.CreateDirectory(ExportDir);
        EnsureSharedAssets();
        var totalEntries = repos.Values.Sum(r => r.Count);
        int processed = 0;

        var repoFiles = new List<(string repoName, string fileName, int count)>();

        // Build cross-reference index: entity name -> (repoSlug, entitySlug)
        var xrefIndex = new Dictionary<string, (string repoSlug, string entitySlug)>(StringComparer.OrdinalIgnoreCase);
        foreach (var (repoName, entries) in repos)
        {
            var repoSlug = Slugify(repoName);
            foreach (var (name, _) in entries)
                xrefIndex.TryAdd(name, (repoSlug, Slugify(name)));
        }

        // Write cross-reference index as JSON for client-side linking
        var xrefJson = new StringBuilder("{");
        var first = true;
        foreach (var (name, (repoSlug, entitySlug)) in xrefIndex.OrderBy(x => x.Key))
        {
            if (!first) xrefJson.Append(',');
            xrefJson.Append($"\n  {JsonSerializer.Serialize(name)}: {{\"r\":\"{repoSlug}\",\"e\":\"{entitySlug}\"}}");
            first = false;
        }
        xrefJson.Append("\n}");
        File.WriteAllText(Path.Combine(ExportDir, "xref.json"), xrefJson.ToString());

        // Generate repo pages
        foreach (var (repoName, entries) in repos.OrderBy(r => r.Key))
        {
            var sorted = entries.OrderBy(e => e.name, StringComparer.OrdinalIgnoreCase).ToList();
            var sb = new StringBuilder();

            sb.AppendLine("<div class=\"toc\" id=\"tocPanel\">");
            sb.AppendLine($"<h2>{RepoIcon(repoName)}{repoName} — {sorted.Count} Entries</h2>");
            sb.AppendLine("<ul>");
            foreach (var (name, _) in sorted)
                sb.AppendLine($"<li><a href=\"#{Slugify(name)}\">{Esc(name)}</a></li>");
            sb.AppendLine("</ul></div>");

            for (int i = 0; i < sorted.Count; i++)
            {
                var (name, json) = sorted[i];
                sb.AppendLine($"<a class=\"entry-anchor\" id=\"{Slugify(name)}\"></a>");
            sb.AppendLine($"<div class=\"entry\" data-tags=\"{ExtractTags(json)}\" data-name=\"{Esc(name)}\">");
                sb.Append(BuildEntryHtml(name, json));
                sb.AppendLine("</div>");
                processed++;
                onProgress?.Invoke(processed, totalEntries);
            }

            var html = BuildPage($"{repoName}", sb.ToString(), true, repoFiles);
            var fileName = $"{Slugify(repoName)}.htm";
            File.WriteAllText(Path.Combine(ExportDir, fileName), html);
            repoFiles.Add((repoName, fileName, sorted.Count));
        }

        // Re-generate all repo pages now that we have the complete nav list
        foreach (var (repoName, entries) in repos.OrderBy(r => r.Key))
        {
            var sorted = entries.OrderBy(e => e.name, StringComparer.OrdinalIgnoreCase).ToList();
            var sb = new StringBuilder();

            sb.AppendLine("<div class=\"toc\" id=\"tocPanel\">");
            sb.AppendLine($"<h2>{RepoIcon(repoName)}{repoName} — {sorted.Count} Entries</h2>");
            sb.AppendLine("<ul>");
            foreach (var (name, _) in sorted)
                sb.AppendLine($"<li><a href=\"#{Slugify(name)}\">{Esc(name)}</a></li>");
            sb.AppendLine("</ul></div>");

            for (int i = 0; i < sorted.Count; i++)
            {
                var (name, json) = sorted[i];
                sb.AppendLine($"<a class=\"entry-anchor\" id=\"{Slugify(name)}\"></a>");
            sb.AppendLine($"<div class=\"entry\" data-tags=\"{ExtractTags(json)}\" data-name=\"{Esc(name)}\">");
                sb.Append(BuildEntryHtml(name, json));
                sb.AppendLine("</div>");
            }

            var html = BuildPage($"{repoName}", sb.ToString(), true, repoFiles);
            var fileName = $"{Slugify(repoName)}.htm";
            File.WriteAllText(Path.Combine(ExportDir, fileName), html);
        }

        // Master index — dashboard style
        var indexSb = new StringBuilder();
        indexSb.AppendLine("<div class=\"dashboard\">");
        indexSb.AppendLine($"<p class=\"dash-subtitle\">{repos.Count} repositories &middot; {totalEntries:N0} entities</p>");
        indexSb.AppendLine("<div class=\"dash-grid\">");
        foreach (var (repoName, fileName, count) in repoFiles)
        {
            indexSb.AppendLine($"<a href=\"{fileName}\" class=\"dash-card\">");
            indexSb.AppendLine($"  <div class=\"dash-icon\">{RepoIcon(repoName)}</div>");
            indexSb.AppendLine($"  <div class=\"dash-label\">{Esc(repoName)}</div>");
            indexSb.AppendLine($"  <div class=\"dash-count\">{count:N0}</div>");
            indexSb.AppendLine("</a>");
        }
        indexSb.AppendLine("</div></div>");

        var indexHtml = BuildPage("Street Samurai Encyclopedia", indexSb.ToString(), false, repoFiles);
        var indexPath = Path.Combine(ExportDir, "index.htm");
        File.WriteAllText(indexPath, indexHtml);
        return indexPath;
    }

    private static string BuildEntryHtml(string name, string jsonContent)
    {
        var slug = Slugify(name);
        var sb = new StringBuilder();
        sb.AppendLine($"<h3><span class=\"entry-name\">{Esc(name)}</span><span class=\"entry-links\"><a href=\"#{slug}\" class=\"permalink\" title=\"Permalink\">#</a><span class=\"copy-link\" onclick=\"copyLink('{slug}')\" title=\"Copy link\"><i class=\"bi bi-copy\"></i></span></span></h3>");

        try
        {
            var doc = JsonDocument.Parse(jsonContent);

            // Extract description first for prominent display
            if (doc.RootElement.TryGetProperty("description", out var descEl) && descEl.ValueKind == JsonValueKind.String)
            {
                var desc = descEl.GetString() ?? "";
                if (!string.IsNullOrWhiteSpace(desc))
                {
                    var formatted = Esc(desc).Replace("\\n", "<br>").Replace("\n", "<br>");
                    sb.AppendLine($"<div class=\"entry-desc\" data-xref>{formatted}</div>");
                }
            }

            // Parent corponation chain
            if (doc.RootElement.TryGetProperty("parent_corponation", out var parentEl) && parentEl.ValueKind == JsonValueKind.String)
            {
                var parent = parentEl.GetString() ?? "";
                if (!string.IsNullOrWhiteSpace(parent))
                {
                    string mfr = "";
                    if (doc.RootElement.TryGetProperty("manufacturer", out var mfrEl) && mfrEl.ValueKind == JsonValueKind.String)
                        mfr = mfrEl.GetString() ?? "";
                    if (!string.IsNullOrWhiteSpace(mfr) && !mfr.Equals(parent, StringComparison.OrdinalIgnoreCase))
                        sb.AppendLine($"<div class=\"corp-chain\"><i class=\"bi bi-diagram-3\"></i> <span data-xref>{Esc(mfr)}</span> <span class=\"chain-arrow\">&rarr;</span> <span data-xref>{Esc(parent)}</span></div>");
                    else
                        sb.AppendLine($"<div class=\"corp-chain\"><i class=\"bi bi-building\"></i> <span data-xref>{Esc(parent)}</span></div>");
                }
            }

            // Tags as clickable pills
            if (doc.RootElement.TryGetProperty("tags", out var tagsEl) && tagsEl.ValueKind == JsonValueKind.Array)
            {
                var tags = tagsEl.EnumerateArray().Where(t => t.ValueKind == JsonValueKind.String).Select(t => t.GetString()!).ToList();
                if (tags.Count > 0)
                {
                    sb.Append("<div class=\"entry-tags\">");
                    foreach (var tag in tags)
                        sb.Append($"<span class=\"entry-tag\" onclick=\"toggleTag('{Esc(tag.ToLowerInvariant())}', null)\">{Esc(tag)}</span>");
                    sb.AppendLine("</div>");
                }
            }

            // Remaining fields as table (skip already-rendered ones)
            var skipFields = new HashSet<string> { "id", "description", "tags", "parent_corponation", "type" };
            var hasFields = false;

            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (skipFields.Contains(prop.Name)) continue;
                var val = prop.Value;
                if (val.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(val.GetString())) continue;
                if (val.ValueKind == JsonValueKind.Array && val.GetArrayLength() == 0) continue;
                if (val.ValueKind == JsonValueKind.Null) continue;

                if (!hasFields) { sb.AppendLine("<table class=\"fields\">"); hasFields = true; }
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td class=\"key\">{Esc(PrettyKey(prop.Name))}</td>");
                sb.AppendLine($"<td class=\"val\" data-xref>{FormatValue(val)}</td>");
                sb.AppendLine("</tr>");
            }
            if (hasFields) sb.AppendLine("</table>");
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "HTML table render failed, falling back to raw JSON");
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
            sb.AppendLine($"<td class=\"val\" data-xref>{FormatValue(prop.Value)}</td>");
            sb.AppendLine("</tr>");
        }
        sb.Append("</table>");
        return sb.ToString();
    }

    private void EnsureSharedAssets()
    {
        Directory.CreateDirectory(ExportDir);
        File.WriteAllText(Path.Combine(ExportDir, "export.css"), ExportCss);
        File.WriteAllText(Path.Combine(ExportDir, "export.js"), ExportJs);
    }

    private string BuildPage(string title, string body, bool includeFilter, List<(string repoName, string fileName, int count)>? navItems)
    {
        // Build sidebar nav
        var navHtml = new StringBuilder();
        navHtml.AppendLine("<nav class=\"sidebar\" id=\"sidebar\">");
        navHtml.AppendLine("<div class=\"sidebar-header\"><a href=\"index.htm\" class=\"sidebar-brand\">Street Samurai</a></div>");
        navHtml.AppendLine("<input type=\"text\" class=\"sidebar-search\" id=\"globalSearch\" placeholder=\"Search all...\" onkeydown=\"if(event.key==='Enter')globalSearchGo()\" />");
        if (navItems != null)
        {
            navHtml.AppendLine("<ul class=\"sidebar-nav\">");
            foreach (var (repoName, fileName, count) in navItems)
                navHtml.AppendLine($"<li><a href=\"{fileName}\">{RepoIcon(repoName)}<span>{Esc(repoName)}</span><span class=\"nav-count\">{count}</span></a></li>");
            navHtml.AppendLine("</ul>");
        }
        navHtml.AppendLine("</nav>");

        var filterHtml = includeFilter ? @"
<div class=""toolbar"">
    <div class=""toolbar-row"">
        <div class=""filter-bar"" id=""filterBar"">
            <input type=""text"" id=""filterInput"" placeholder=""Filter this page..."" oninput=""filterEntries()"" />
            <span class=""filter-clear"" id=""filterClear"" onclick=""clearFilter()"" title=""Clear"">&times;</span>
            <i class=""bi bi-search filter-icon""></i>
        </div>
        <div class=""tag-bar"" id=""tagBar""></div>
    </div>
    <div class=""filter-status""><span id=""visibleCount""></span></div>
</div>" : "";

        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1"">
<title>{Esc(title)}</title>
<link rel=""stylesheet"" href=""https://fonts.googleapis.com/css2?family=Outfit:wght@300;400;500;600;700&display=swap"">
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"">
<link rel=""stylesheet"" href=""export.css"">
</head>
<body id=""top"">
<button class=""sidebar-toggle"" id=""sidebarToggle"" onclick=""toggleSidebar()""><i class=""bi bi-list""></i></button>
{navHtml}
<main class=""content"" id=""mainContent"">
<div class=""page-header"">
<h1>{Esc(title)}</h1>
</div>
{filterHtml}
{body}
<footer class=""export-footer"">Exported from StreetSamurai Canon Engine — {settings.FormatTimestamp(DateTime.Now)}</footer>
</main>
<button class=""scroll-top"" id=""scrollTop"" onclick=""document.getElementById('mainContent').scrollTo({{top:0,behavior:'smooth'}})"" title=""Scroll to top""><i class=""bi bi-arrow-up""></i></button>
<script src=""export.js""></script>
</body>
</html>";
    }

    private const string ExportCss = @"
:root { --bg: #0d1117; --surface: #161b22; --border: #30363d; --text: #e6edf3; --muted: #8b949e; --accent: #dc3545; --link: #e6edf3; --key-bg: #1c2128; --sidebar-w: 240px; }
* { margin: 0; padding: 0; box-sizing: border-box; }
body { background: var(--bg); color: var(--text); font-family: 'Outfit', 'Helvetica Neue', Helvetica, Arial, sans-serif; font-size: 14px; line-height: 1.6; display: flex; height: 100vh; overflow: hidden; }

/* Sidebar */
.sidebar { width: var(--sidebar-w); min-width: var(--sidebar-w); background: var(--surface); border-right: 1px solid var(--border); height: 100vh; overflow-y: auto; display: flex; flex-direction: column; flex-shrink: 0; transition: margin-left 0.2s; }
.sidebar-header { padding: 16px; border-bottom: 1px solid var(--border); }
.sidebar-brand { color: var(--accent); font-weight: 700; font-size: 1.1rem; text-decoration: none; }
.sidebar-brand:hover { text-decoration: none; color: var(--accent); }
.sidebar-search { margin: 8px; padding: 6px 10px; background: var(--bg); color: var(--text); border: 1px solid var(--border); border-radius: 4px; font-family: 'Outfit', sans-serif; font-size: 12px; outline: none; }
.sidebar-search:focus { border-color: var(--accent); }
.sidebar-nav { list-style: none; padding: 4px 0; flex: 1; }
.sidebar-nav li a { display: flex; align-items: center; gap: 6px; padding: 6px 16px; color: var(--text); text-decoration: none; font-size: 13px; transition: background 0.15s; border-radius: 4px; margin: 1px 8px; }
.sidebar-nav li a:hover { background: var(--border); text-decoration: none; color: #fff; }
.sidebar-nav li a i { color: var(--muted); font-size: 14px; width: 18px; text-align: center; }
.sidebar-nav li a:hover i { color: var(--text); }
.sidebar-nav li a span { flex: 1; }
.nav-count { color: var(--muted); font-size: 11px; min-width: 32px; text-align: right; font-variant-numeric: tabular-nums; }
.sidebar-toggle { display: none; position: fixed; top: 8px; left: 8px; z-index: 300; background: var(--surface); border: 1px solid var(--border); color: var(--text); width: 36px; height: 36px; border-radius: 4px; cursor: pointer; font-size: 18px; }

/* Main content */
.content { flex: 1; overflow-y: auto; padding: 24px clamp(16px, 3vw, 40px); max-width: 1200px; }
.page-header { margin-bottom: 20px; padding-bottom: 12px; border-bottom: 2px solid var(--accent); }
h1 { color: var(--accent); font-weight: 700; font-size: clamp(1.1rem, 3vw, 1.6rem); }
h2 { color: var(--accent); font-weight: 600; font-size: clamp(1rem, 2.5vw, 1.3rem); margin-bottom: 12px; }
h3 { color: var(--accent); font-weight: 500; font-size: clamp(0.95rem, 2vw, 1.1rem); margin-bottom: 8px; padding-top: 16px; border-top: 1px solid var(--border); display: flex; align-items: center; }
a { color: var(--link); text-decoration: none; } a:hover { text-decoration: underline; }

/* Dashboard */
.dashboard { text-align: center; padding: 20px 0; }
.dash-subtitle { color: var(--muted); margin-bottom: 24px; font-size: 14px; }
.dash-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(180px, 1fr)); gap: 12px; }
.dash-card { background: var(--surface); border: 1px solid var(--border); border-radius: 8px; padding: 20px 16px; text-align: center; text-decoration: none; transition: border-color 0.2s, transform 0.1s; }
.dash-card:hover { border-color: var(--accent); transform: translateY(-2px); text-decoration: none; }
.dash-icon { font-size: 24px; color: var(--accent); margin-bottom: 8px; }
.dash-icon i { margin: 0; }
.dash-label { color: var(--text); font-weight: 500; font-size: 13px; margin-bottom: 4px; }
.dash-count { color: var(--muted); font-size: 20px; font-weight: 700; font-variant-numeric: tabular-nums; }

/* Toolbar */
.toolbar { padding: 8px 0; margin-bottom: 16px; }
.toolbar-row { display: flex; flex-direction: column; gap: 8px; }
.filter-bar { flex: 1; display: flex; align-items: center; position: relative; }
.filter-bar input { width: 100%; background: var(--surface); color: var(--text); border: 1px solid var(--border); border-radius: 4px; padding: 8px 36px 8px 12px; font-size: 14px; font-family: 'Outfit', sans-serif; outline: none; }
.filter-bar input:focus { border-color: var(--accent); }
.filter-icon { position: absolute; right: 12px; color: var(--muted); pointer-events: none; font-size: 14px; }
.filter-clear { position: absolute; right: 32px; cursor: pointer; color: var(--muted); font-size: 18px; display: none; }
.filter-clear:hover { color: var(--accent); }
.tag-bar { flex: 1; display: flex; align-items: center; }
.filter-status { margin-top: 4px; }
.filter-status:empty { display: none; }

/* TOC */
.toc { background: var(--surface); border: 1px solid var(--border); border-radius: 6px; padding: 16px 20px; margin-bottom: 20px; }
.toc ul { columns: 3; column-gap: 20px; list-style: none; }
.toc li { padding: 2px 0; font-size: 13px; break-inside: avoid; }
.toc .badge { background: var(--border); color: var(--muted); border-radius: 10px; padding: 1px 8px; font-size: 11px; margin-left: 4px; }

/* Entries */
.entry { background: var(--surface); border: 1px solid var(--border); border-radius: 6px; padding: clamp(12px, 2vw, 20px); margin-bottom: 12px; overflow-x: auto; }
.entry-anchor { display: block; position: relative; top: -80px; visibility: hidden; }
.entry-anchor:target + .entry { border-color: var(--accent); box-shadow: 0 0 0 1px var(--accent); }
.entry-name { flex: 1; }
.entry-links { margin-left: auto; display: flex; gap: 4px; flex-shrink: 0; }
.permalink { display: none; }
.copy-link { cursor: pointer; padding: 0 4px; color: var(--muted); }
.copy-link:hover { color: var(--link); }
.entry-desc { margin: 10px 0; line-height: 1.7; color: var(--text); }
.corp-chain { margin: 8px 0; padding: 6px 10px; background: var(--key-bg); border-radius: 4px; font-size: 12px; color: var(--muted); display: flex; align-items: center; gap: 6px; }
.chain-arrow { color: var(--accent); }
.entry-tags { margin: 8px 0; display: flex; flex-wrap: wrap; gap: 4px; }
.entry-tag { background: var(--bg); border: 1px solid var(--border); border-radius: 10px; padding: 1px 8px; font-size: 11px; color: var(--muted); cursor: pointer; transition: all 0.15s; }
.entry-tag:hover { border-color: var(--accent); color: var(--accent); }

/* Fields table */
.fields, .sub-fields { width: 100%; border-collapse: collapse; margin-top: 8px; }
.fields td, .sub-fields td { padding: 4px 10px; vertical-align: top; border-bottom: 1px solid var(--border); font-size: 13px; word-break: break-word; }
.key { color: var(--muted); white-space: nowrap; width: 160px; background: var(--key-bg); font-weight: 600; font-size: 11px; text-transform: uppercase; letter-spacing: 0.5px; }
.sub-fields .key { width: 120px; }
.val { color: var(--text); }
.num { color: #79c0ff; }
.bool { color: #7ee787; }
.null { color: var(--muted); font-style: italic; }
ul.compact { list-style: disc; margin-left: 16px; }
ul.compact li { font-size: 13px; padding: 1px 0; }
.sub-entry { border-left: 3px solid var(--border); padding-left: 12px; margin: 6px 0; }

/* Cross-references */
.xref { color: var(--link); cursor: pointer; border-bottom: 1px dotted var(--link); }
.xref:hover { color: var(--accent); border-color: var(--accent); }

/* Tag bar */
.tag-bar { display: flex; flex-wrap: wrap; gap: 4px; align-items: center; }
.tag-btn { background: var(--surface); color: var(--muted); border: 1px solid var(--border); border-radius: 12px; padding: 2px 10px; font-size: 11px; cursor: pointer; transition: all 0.15s; font-family: 'Outfit', sans-serif; }
.tag-btn:hover { border-color: var(--muted); color: var(--text); }
.tag-btn.active { background: var(--accent); color: #fff; border-color: var(--accent); }
.tag-btn .tag-count { opacity: 0.6; margin-left: 3px; font-size: 10px; }
.tag-clear { background: none; color: var(--muted); border: 1px solid var(--border); border-radius: 12px; padding: 2px 10px; font-size: 11px; cursor: pointer; font-family: 'Outfit', sans-serif; }
.tag-clear:hover { color: var(--accent); border-color: var(--accent); }
.tag-search { background: var(--surface); color: var(--text); border: 1px solid var(--border); border-radius: 12px; padding: 2px 10px; font-size: 11px; width: 120px; outline: none; font-family: 'Outfit', sans-serif; }
.tag-search:focus { border-color: var(--accent); }
.tag-overflow { max-height: 100px; overflow-y: auto; display: flex; flex-wrap: wrap; gap: 4px; flex: 1; }

/* Tag dropdown */
.tag-dropdown-wrap { position: relative; width: 100%; }
.tag-toggle { background: var(--surface); color: var(--muted); border: 1px solid var(--border); border-radius: 4px; padding: 8px 12px; font-size: 13px; cursor: pointer; font-family: 'Outfit', sans-serif; display: flex; align-items: center; gap: 4px; white-space: nowrap; width: 100%; }
.tag-toggle:hover { border-color: var(--muted); color: var(--text); }
.tag-dropdown { position: absolute; top: 100%; left: 0; margin-top: 4px; background: var(--surface); border: 1px solid var(--border); border-radius: 6px; width: 320px; max-height: 400px; overflow: hidden; display: flex; flex-direction: column; box-shadow: 0 8px 24px rgba(0,0,0,0.4); z-index: 200; }
.tag-dd-search { margin: 8px; padding: 6px 10px; background: var(--bg); color: var(--text); border: 1px solid var(--border); border-radius: 4px; font-size: 12px; font-family: 'Outfit', sans-serif; outline: none; }
.tag-dd-search:focus { border-color: var(--accent); }
.tag-dd-actions { padding: 4px 8px; border-bottom: 1px solid var(--border); display: flex; align-items: center; justify-content: space-between; }
.tag-dd-actions button { background: none; border: none; color: var(--accent); cursor: pointer; font-size: 11px; font-family: 'Outfit', sans-serif; padding: 2px 4px; }
.tag-dd-actions button:hover { text-decoration: underline; }
#tagSelectedCount { font-size: 11px; color: var(--muted); }
.tag-dd-list { overflow-y: auto; max-height: 300px; padding: 4px 0; }
.tag-dd-item { display: flex; align-items: center; gap: 8px; padding: 4px 12px; cursor: pointer; font-size: 12px; }
.tag-dd-item:hover { background: var(--bg); }
.tag-dd-item input { accent-color: var(--accent); cursor: pointer; }
.tag-dd-name { flex: 1; color: var(--text); }
.tag-dd-count { color: var(--muted); font-size: 11px; min-width: 24px; text-align: right; }
.tag-pills { display: flex; flex-wrap: wrap; gap: 4px; margin-top: 6px; }
.tag-pill { background: var(--accent); color: #fff; border-radius: 10px; padding: 2px 10px; font-size: 11px; cursor: pointer; display: flex; align-items: center; gap: 4px; }
.tag-pill i { font-size: 10px; }
.tag-pill:hover { opacity: 0.8; }
.tag-pill-clear { background: var(--border); color: var(--muted); }
.tag-pill-clear:hover { color: var(--text); }

/* Go back */
.go-back { color: var(--muted); font-size: 12px; text-decoration: none; display: inline-flex; align-items: center; gap: 2px; margin-bottom: 4px; }
.go-back:hover { color: var(--link); text-decoration: none; }

/* Footer */
.export-footer { text-align: center; padding: 24px 0 16px; color: var(--muted); font-size: 11px; border-top: 1px solid var(--border); margin-top: 40px; }

/* Scroll to top */
.scroll-top { position: fixed; bottom: 24px; right: 24px; width: 40px; height: 40px; border-radius: 50%; background: var(--accent); color: #fff; border: none; font-size: 16px; cursor: pointer; opacity: 0; transition: opacity 0.3s; z-index: 200; display: flex; align-items: center; justify-content: center; box-shadow: 0 2px 8px rgba(0,0,0,0.4); }
.scroll-top.visible { opacity: 0.8; }
.scroll-top:hover { opacity: 1; }

/* Scrollbars */
::-webkit-scrollbar { width: 4px; height: 4px; }
::-webkit-scrollbar-track { background: transparent; }
::-webkit-scrollbar-thumb { background: #30363d; border-radius: 4px; }
::-webkit-scrollbar-thumb:hover { background: #484f58; }
* { scrollbar-width: thin; scrollbar-color: #30363d transparent; }

/* Mobile */
@media (max-width: 768px) {
    .sidebar { position: fixed; z-index: 250; margin-left: calc(-1 * var(--sidebar-w)); }
    .sidebar.open { margin-left: 0; }
    .sidebar-toggle { display: flex; align-items: center; justify-content: center; }
    .content { padding: 16px 12px; padding-top: 48px; }
    .toc ul { columns: 1; }
    .dash-grid { grid-template-columns: repeat(auto-fill, minmax(140px, 1fr)); }
    .fields td, .sub-fields td { display: block; width: 100%; padding: 3px 8px; }
    .key { white-space: normal; width: 100%; border-bottom: none; }
    .fields tr, .sub-fields tr { display: block; border-bottom: 1px solid var(--border); padding: 4px 0; }
}
@media print { .sidebar, .toolbar, .scroll-top, .sidebar-toggle { display: none; } .content { max-width: 100%; } .entry { break-inside: avoid; } }
";

    private const string ExportJs = @"
var activeTags = new Set();
var xrefData = null;

// Load cross-reference index
fetch('xref.json').then(r => r.ok ? r.json() : {}).then(data => {
    xrefData = data;
    applyXrefs();
}).catch(() => { xrefData = {}; });

function applyXrefs() {
    if (!xrefData || Object.keys(xrefData).length === 0) return;
    // Build a regex from all entity names (longest first to avoid partial matches)
    var names = Object.keys(xrefData).sort((a, b) => b.length - a.length);
    // Only xref names 4+ chars to avoid false matches
    names = names.filter(n => n.length >= 4);
    if (names.length === 0) return;

    // Process all elements with data-xref attribute
    document.querySelectorAll('[data-xref]').forEach(el => {
        var html = el.innerHTML;
        // Don't process if already has xref links
        if (html.indexOf('class=""xref""') !== -1) return;

        // Simple approach: replace first occurrence of each name
        var used = new Set();
        for (var i = 0; i < names.length && used.size < 10; i++) {
            var name = names[i];
            var idx = html.indexOf(name);
            if (idx === -1) continue;
            // Don't link if inside an HTML tag
            var before = html.substring(0, idx);
            if ((before.match(/</g) || []).length > (before.match(/>/g) || []).length) continue;
            var ref = xrefData[name];
            var link = '<a class=""xref"" href=""' + ref.r + '.htm#' + ref.e + '"">' + name + '</a>';
            html = html.substring(0, idx) + link + html.substring(idx + name.length);
            used.add(name);
        }
        if (used.size > 0) el.innerHTML = html;
    });
}

function applyFilters() {
    var q = (document.getElementById('filterInput') || {}).value || '';
    q = q.toLowerCase();
    var clearBtn = document.getElementById('filterClear');
    if (clearBtn) clearBtn.style.display = q.length > 0 ? 'block' : 'none';

    var entries = document.querySelectorAll('.entry');
    var visible = 0;
    entries.forEach(function(e) {
        var entryName = (e.getAttribute('data-name') || '').toLowerCase();
        var textMatch = q.length === 0 || entryName.indexOf(q) !== -1;
        var tagMatch = true;
        if (activeTags.size > 0) {
            var entryTags = (e.getAttribute('data-tags') || '').split(',').filter(Boolean);
            activeTags.forEach(function(t) { if (entryTags.indexOf(t) === -1) tagMatch = false; });
        }
        var show = textMatch && tagMatch;
        e.style.display = show ? '' : 'none';
        if (show) visible++;
    });

    // Sync TOC items with visible entries
    document.querySelectorAll('.toc li').forEach(function(li) {
        var link = li.querySelector('a');
        if (!link) return;
        var href = link.getAttribute('href');
        if (!href || !href.startsWith('#')) return;
        var targetId = href.substring(1);
        // Find the entry-anchor with this id, then check its sibling .entry
        var anchor = document.getElementById(targetId);
        if (!anchor) { li.style.display = 'none'; return; }
        var entry = anchor.nextElementSibling;
        if (!entry || !entry.classList.contains('entry')) { li.style.display = 'none'; return; }
        li.style.display = entry.style.display === 'none' ? 'none' : '';
    });

    var counter = document.getElementById('visibleCount');
    if (counter) counter.textContent = 'Showing ' + visible + ' of ' + entries.length;
}

function filterEntries() { applyFilters(); }
function clearFilter() { document.getElementById('filterInput').value = ''; applyFilters(); }

function toggleTagDropdown() {
    var dd = document.getElementById('tagDropdown');
    dd.style.display = dd.style.display === 'none' ? 'block' : 'none';
    if (dd.style.display === 'block') document.getElementById('tagSearch').focus();
}

function toggleTag(tag, btn) {
    if (activeTags.has(tag)) {
        activeTags.delete(tag);
    } else {
        activeTags.add(tag);
    }
    // Sync checkboxes in dropdown
    document.querySelectorAll('.tag-dd-item input[data-tag=""' + tag + '""]').forEach(cb => cb.checked = activeTags.has(tag));
    // Sync entry tag pills
    document.querySelectorAll('.entry-tag').forEach(et => {
        if (et.textContent.toLowerCase() === tag) {
            et.style.borderColor = activeTags.has(tag) ? 'var(--accent)' : '';
            et.style.color = activeTags.has(tag) ? 'var(--accent)' : '';
        }
    });
    updateTagLabel();
    updateActivePills();
    applyFilters();
}

function updateTagLabel() {
    var label = document.getElementById('tagLabel');
    var count = document.getElementById('tagSelectedCount');
    if (label) label.textContent = activeTags.size === 0 ? 'Filter by tags...' : activeTags.size + ' tag' + (activeTags.size !== 1 ? 's' : '') + ' selected';
    if (count) count.textContent = activeTags.size > 0 ? activeTags.size + ' active' : '';
}

function updateActivePills() {
    var area = document.getElementById('activePills');
    if (!area) return;
    if (activeTags.size === 0) { area.innerHTML = ''; return; }
    area.innerHTML = Array.from(activeTags).sort().map(function(t) {
        return '<span class=""tag-pill"" onclick=""toggleTag(\'' + t.replace(/'/g, ""\\'"") + '\', null)"">' + t + ' <i class=""bi bi-x""></i></span>';
    }).join('') + '<span class=""tag-pill tag-pill-clear"" onclick=""clearTags()"">Clear all</span>';
}

function clearTags() {
    activeTags.clear();
    document.querySelectorAll('.tag-dd-item input').forEach(cb => cb.checked = false);
    document.querySelectorAll('.entry-tag').forEach(et => { et.style.borderColor = ''; et.style.color = ''; });
    updateTagLabel();
    updateActivePills();
    applyFilters();
}

function filterTags() {
    var q = document.getElementById('tagSearch').value.toLowerCase();
    document.querySelectorAll('.tag-dd-item').forEach(item => {
        var tag = item.querySelector('input').getAttribute('data-tag');
        item.style.display = tag.indexOf(q) !== -1 ? '' : 'none';
    });
}

function copyLink(slug) {
    var url = location.href.split('#')[0] + '#' + slug;
    navigator.clipboard.writeText(url);
}

function toggleSidebar() {
    document.getElementById('sidebar').classList.toggle('open');
}

function globalSearchGo() {
    var q = document.getElementById('globalSearch').value.trim();
    if (!q) return;
    // If on a repo page, use the local filter
    var local = document.getElementById('filterInput');
    if (local) { local.value = q; filterEntries(); return; }
    // On index, find first repo page and search there
    var firstLink = document.querySelector('.sidebar-nav a');
    if (firstLink) window.location = firstLink.href + '?q=' + encodeURIComponent(q);
}

// Scroll-to-top visibility
var mainContent = document.getElementById('mainContent');
if (mainContent) {
    mainContent.addEventListener('scroll', function() {
        var btn = document.getElementById('scrollTop');
        if (btn) btn.classList.toggle('visible', mainContent.scrollTop > 300);
    });
}

// Build tag bar on load
document.addEventListener('DOMContentLoaded', function() {
    // Apply query string filter
    var params = new URLSearchParams(window.location.search);
    var q = params.get('q');
    if (q) {
        var fi = document.getElementById('filterInput');
        if (fi) { fi.value = q; setTimeout(filterEntries, 100); }
    }

    // Show initial count
    applyFilters();

    var tagBar = document.getElementById('tagBar');
    if (!tagBar) return;

    var tagCounts = {};
    document.querySelectorAll('.entry').forEach(function(e) {
        var tags = (e.getAttribute('data-tags') || '').split(',').filter(Boolean);
        tags.forEach(function(t) { tagCounts[t] = (tagCounts[t] || 0) + 1; });
    });

    var sorted = Object.entries(tagCounts).filter(function(t) { return t[1] >= 2; }).sort(function(a, b) { return a[0].localeCompare(b[0]); });
    if (sorted.length === 0) { tagBar.style.display = 'none'; return; }

    // Dropdown with checkboxes
    var dd = document.createElement('div');
    dd.className = 'tag-dropdown-wrap';
    dd.innerHTML = '<button class=""tag-toggle"" id=""tagToggle"" onclick=""toggleTagDropdown()"">' +
        '<i class=""bi bi-tags""></i> <span id=""tagLabel"">Filter by tags...</span> <i class=""bi bi-chevron-down"" style=""font-size:0.7em;margin-left:4px;""></i></button>' +
        '<div class=""tag-dropdown"" id=""tagDropdown"" style=""display:none;"">' +
        '<input type=""text"" id=""tagSearch"" class=""tag-dd-search"" placeholder=""Search tags..."" oninput=""filterTags()"" />' +
        '<div class=""tag-dd-actions""><button onclick=""clearTags()"">Clear all</button><span id=""tagSelectedCount""></span></div>' +
        '<div class=""tag-dd-list"">' +
        sorted.map(function(t) {
            var checked = activeTags.has(t[0]) ? ' checked' : '';
            return '<label class=""tag-dd-item""><input type=""checkbox"" data-tag=""' + t[0] + '""' + checked +
                ' onchange=""toggleTag(\'' + t[0].replace(/'/g, ""\\'"") + '\', null)"" />' +
                '<span class=""tag-dd-name"">' + t[0] + '</span><span class=""tag-dd-count"">' + t[1] + '</span></label>';
        }).join('') +
        '</div></div>';
    tagBar.appendChild(dd);

    // Show active tag pills below dropdown
    var pillArea = document.createElement('div');
    pillArea.id = 'activePills';
    pillArea.className = 'tag-pills';
    tagBar.appendChild(pillArea);

    // Close dropdown on outside click
    document.addEventListener('click', function(e) {
        var wrap = document.querySelector('.tag-dropdown-wrap');
        if (wrap && !wrap.contains(e.target)) {
            document.getElementById('tagDropdown').style.display = 'none';
        }
    });
});
";

    private static string RepoIcon(string repoName)
    {
        var icon = repoName.ToLowerInvariant().Trim() switch
        {
            "corponations" => "bi-building",
            "factions" => "bi-shield-exclamation",
            "characters" => "bi-people",
            "places" => "bi-geo-alt",
            "technology" => "bi-cpu",
            "automata" => "bi-robot",
            "weaponry" => "bi-crosshair",
            "ammunition" => "bi-bullseye",
            "equipment" => "bi-shield-shaded",
            "cyberware" => "bi-motherboard",
            "documents" => "bi-file-earmark-text",
            "vocabulary" => "bi-chat-quote",
            "quotes" => "bi-quote",
            "consumer goods" => "bi-basket",
            "pharmaceuticals" => "bi-capsule",
            "substrates" or "materials" => "bi-gem",
            "news" or "news archive" => "bi-newspaper",
            "archetypes" => "bi-person-bounding-box",
            "synthetics" => "bi-cpu",
            "geneware" or "genemods" => "bi-virus",
            "transportation" => "bi-truck",
            "apparel" => "bi-handbag",
            "contracts" => "bi-clipboard-check",
            "entertainment" => "bi-film",
            "subsidiaries" => "bi-diagram-3",
            _ => "bi-folder",
        };
        return $"<i class=\"bi {icon}\" style=\"margin-right:6px;\"></i>";
    }

    private static string ExtractTags(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("tags", out var tagsEl) && tagsEl.ValueKind == JsonValueKind.Array)
                return string.Join(",", tagsEl.EnumerateArray().Where(t => t.ValueKind == JsonValueKind.String).Select(t => t.GetString()!.ToLowerInvariant()));
        }
        catch { }
        return "";
    }

    private static string PrettyKey(string key) =>
        key.Replace("_", " ").Replace("-", " ");

    private static string Esc(string s) =>
        System.Net.WebUtility.HtmlEncode(s);

    private static string Slugify(string s) =>
        System.Text.RegularExpressions.Regex.Replace(s.ToLowerInvariant(), @"[^a-z0-9]+", "_").Trim('_');
}
