using System.Text.Json;
using System.Text.Json.Nodes;

namespace StreetSamurai.Core.Services.Operator.KdpTools;

/// <summary>
/// One-off, read-only exploration of KDP's live Categories modal — walks every Subcategory branch
/// and Placement leaf under a given starting path and returns the whole subtree as JSON. Never
/// saves anything: each node is visited by reloading the Details page fresh (clean slate, no
/// stale modal state) rather than clicking Save or Cancel, so this can never accidentally alter
/// the real book's category assignment it happens to run against.
///
/// Deliberately NOT registered as an <see cref="IKdpTool"/> for the LLM operator — this is a
/// documentation/reference-building pass run once by a human decision (see
/// tools/kdp/category-tree-horror.json), not part of the autonomous publish flow. The JS query
/// patterns mirror <see cref="SelectCategoriesTool"/>'s confirmed-live DOM shape (option values
/// carry a "level":N marker; leaf options are native checkboxes under a closest label).
/// </summary>
public static class CategoryTreeCrawler
{
    /// <summary>
    /// Diagnostic-only: opens the modal and reports whatever it can find about how the category
    /// data actually reaches the page — the shape of the top-level &lt;option&gt; values' embedded
    /// JSON, and any candidate script-tag/global-variable blobs that look like they might hold the
    /// full taxonomy already loaded client-side. Run this BEFORE the full interactive crawl: if
    /// the whole tree (or a usable chunk of it) is already sitting in one JS object, extracting it
    /// directly is far faster and safer than clicking through hundreds of cascading selects.
    /// </summary>
    public static async Task<string> ProbeAsync(IKdpBrowser browser, string detailsUrl, CancellationToken ct)
    {
        await NavigateAsync(browser, detailsUrl, ct);
        var opened = await OpenModalAsync(browser, ct);
        if (!opened)
        {
            var diag = await browser.EvalAsync("""
            (function() {
                var btn = document.getElementById('categories-modal-button');
                return JSON.stringify({
                    url: window.location.href,
                    title: document.title,
                    hasCategoriesButton: !!btn,
                    bodySnippet: (document.body.innerText || '').slice(0, 600)
                });
            })()
            """, ct);
            return JsonSerializer.Serialize(new { error = "modal did not open", diag = JsonDocument.Parse(diag).RootElement });
        }

        return await browser.EvalAsync("""
        (function() {
            var all = Array.from(document.querySelectorAll('select'));
            var anchor = all.findIndex(function (s) { return s.options.length > 1 && s.options[0].value.indexOf('"level":0') !== -1; });
            var sels = anchor === -1 ? [] : all.slice(anchor);
            var topOptions = sels.length > 0
                ? Array.from(sels[0].options).slice(0, 60).map(function (o) { return { text: o.textContent.trim(), value: o.value }; })
                : [];

            // Any inline <script> whose text mentions a handful of real category names together —
            // a strong signal it's carrying the taxonomy, not just app boilerplate.
            var scriptHits = [];
            Array.from(document.querySelectorAll('script')).forEach(function (s, idx) {
                var t = s.textContent || '';
                if (t.length < 500) return;
                var hits = ['Horror', 'Science Fiction', 'Fantasy'].filter(function (kw) { return t.indexOf(kw) !== -1; }).length;
                if (hits >= 2) scriptHits.push({ scriptIndex: idx, length: t.length, sample: t.slice(0, 300) });
            });

            // Unusual global variables (skip well-known browser/framework builtins) whose JSON
            // stringification is suspiciously large or whose text mentions category-ish keywords.
            var globalHits = [];
            var builtins = ['window','document','location','navigator','history','screen','console','WebView2'];
            for (var key in window) {
                if (builtins.indexOf(key) !== -1) continue;
                if (!/^[a-zA-Z_$][a-zA-Z0-9_$]*$/.test(key)) continue;
                var val;
                try { val = window[key]; } catch (e) { continue; }
                if (val === null || typeof val !== 'object') continue;
                var str;
                try { str = JSON.stringify(val); } catch (e) { continue; }
                if (!str || str.length < 2000) continue;
                var hits = ['Horror', 'category', 'Category', 'Subcategory'].filter(function (kw) { return str.indexOf(kw) !== -1; }).length;
                if (hits >= 1) globalHits.push({ key: key, length: str.length, sample: str.slice(0, 300) });
            }

            return JSON.stringify({ anchorFound: anchor !== -1, topOptionCount: sels.length > 0 ? sels[0].options.length : 0, topOptionsSample: topOptions, sampleValue: sels.length > 0 && sels[0].options.length > 1 ? sels[0].options[1].value : null, scriptHits: scriptHits, globalHits: globalHits });
        })()
        """, ct);
    }

    /// <summary>Hard cap on total node visits — a safety backstop against a misbehaving branch
    /// (e.g. a cascading select that never stabilizes) turning into a runaway crawl against the
    /// live site. Well above anything a single genre's subtree should need.</summary>
    private const int MaxNodeVisits = 200;

    public static async Task<JsonObject> CrawlAsync(
        IKdpBrowser browser, string detailsUrl, string[] startPath, Action<string> log, CancellationToken ct, int? maxDepth = null)
    {
        var visits = new int[] { 0 };
        var root = await CrawlPathAsync(browser, detailsUrl, startPath, visits, log, ct, startPath.Length + (maxDepth ?? int.MaxValue));
        // Leave the page in a clean, unmodified state — one final reload, no modal left open.
        await NavigateAsync(browser, detailsUrl, ct);
        return root;
    }

    /// <summary>Absolute path-length ceiling — recursion stops once <c>path.Length</c> reaches this,
    /// regardless of what maxDepth was requested relative to. Lets a caller ask for "just the next
    /// couple of levels under this branch" without descending into an entire genre's full subtree.</summary>
    private static async Task<JsonObject> CrawlPathAsync(
        IKdpBrowser browser, string detailsUrl, string[] path, int[] visits, Action<string> log, CancellationToken ct, int maxPathLength = int.MaxValue)
    {
        visits[0]++;
        var pathLabel = string.Join(" > ", path);
        if (visits[0] > MaxNodeVisits)
        {
            log($"⚠ Node cap ({MaxNodeVisits}) reached — stopping at \"{pathLabel}\" without descending further.");
            return new JsonObject { ["name"] = path[^1], ["truncated"] = true };
        }

        await NavigateAsync(browser, detailsUrl, ct);
        var opened = await OpenModalAsync(browser, ct);
        if (!opened)
        {
            log($"⚠ Could not open the Categories modal for \"{pathLabel}\" — skipping.");
            return new JsonObject { ["name"] = path[^1], ["error"] = "modal did not open" };
        }

        for (var i = 0; i < path.Length; i++)
        {
            var (found, available) = await SetLevelAsync(browser, i, path[i], ct);
            if (!found)
            {
                log($"⚠ \"{path[i]}\" not found at level {i} (path so far: {pathLabel}). Available: {string.Join(" | ", available ?? [])}");
                return new JsonObject
                {
                    ["name"] = path[^1],
                    ["error"] = $"not found at level {i}",
                    ["availableAtThisLevel"] = new JsonArray((available ?? []).Select(a => (JsonNode)a!).ToArray())
                };
            }
            await Task.Delay(700, ct);
        }

        log($"✓ {pathLabel}");
        var state = await ReadStateAsync(browser, path.Length, ct);
        var node = new JsonObject { ["name"] = path[^1] };

        if (state.Leaves is { Count: > 0 })
            node["leaves"] = new JsonArray(state.Leaves.Select(l => (JsonNode)l).ToArray());

        if (state.HasNextSelect && state.Options is { Count: > 0 })
        {
            if (path.Length >= maxPathLength)
            {
                node["truncatedChildren"] = new JsonArray(state.Options.Select(o => (JsonNode)o).ToArray());
            }
            else
            {
                var children = new JsonArray();
                foreach (var opt in state.Options)
                {
                    if (visits[0] > MaxNodeVisits) break;
                    var child = await CrawlPathAsync(browser, detailsUrl, [.. path, opt], visits, log, ct, maxPathLength);
                    children.Add(child);
                }
                node["children"] = children;
            }
        }

        return node;
    }

    private static async Task NavigateAsync(IKdpBrowser browser, string url, CancellationToken ct)
    {
        try { await browser.EvalAsync($"window.location.href = {JsonSerializer.Serialize(url)}; ''", ct); }
        catch { /* navigation tears down the script context; a thrown eval is expected here */ }
        await Task.Delay(3500, ct);
    }

    private static async Task<bool> OpenModalAsync(IKdpBrowser browser, CancellationToken ct)
    {
        const string clickScript = """
        (function() {
            var btn = document.getElementById('categories-modal-button');
            if (!btn) return JSON.stringify({ found: false });
            btn.click();
            return JSON.stringify({ found: true });
        })()
        """;
        var clicked = false;
        for (var attempt = 0; attempt < 20 && !clicked; attempt++)
        {
            if (attempt > 0) await Task.Delay(800, ct);
            var openResult = await browser.EvalAsync(clickScript, ct);
            using var doc = JsonDocument.Parse(openResult);
            clicked = doc.RootElement.GetProperty("found").GetBoolean();
        }
        if (!clicked) return false;

        const string probe = """
        (function() {
            var top = Array.from(document.querySelectorAll('select'))
                .filter(function (s) { return s.options.length > 1 && s.options[0].value.indexOf('"level":0') !== -1; });
            return JSON.stringify({ ready: top.length > 0 });
        })()
        """;
        for (var attempt = 0; attempt < 10; attempt++)
        {
            await Task.Delay(attempt == 0 ? 800 : 500, ct);
            var result = await browser.EvalAsync(probe, ct);
            using var doc = JsonDocument.Parse(result);
            if (doc.RootElement.GetProperty("ready").GetBoolean()) return true;
        }
        return false;
    }

    private static async Task<(bool Found, List<string>? Available)> SetLevelAsync(
        IKdpBrowser browser, int levelIndex, string text, CancellationToken ct)
    {
        var textJs = JsonSerializer.Serialize(text);
        var result = await browser.EvalAsync($$"""
        (function() {
            var all = Array.from(document.querySelectorAll('select'));
            var anchor = all.findIndex(function (s) { return s.options.length > 1 && s.options[0].value.indexOf('"level":0') !== -1; });
            var sels = anchor === -1 ? [] : all.slice(anchor);
            if (sels.length <= {{levelIndex}}) return JSON.stringify({ found: false, available: [] });
            var sel = sels[{{levelIndex}}];
            var opt = Array.from(sel.options).find(function (o) { return o.textContent.trim() === {{textJs}}; });
            if (!opt) {
                var available = Array.from(sel.options).map(function (o) { return o.textContent.trim(); })
                    .filter(function (t) { return t.length > 0; });
                return JSON.stringify({ found: false, available: available });
            }
            var setter = Object.getOwnPropertyDescriptor(window.HTMLSelectElement.prototype, 'value').set;
            setter.call(sel, opt.value);
            sel.dispatchEvent(new Event('change', { bubbles: true }));
            return JSON.stringify({ found: true });
        })()
        """, ct);
        using var doc = JsonDocument.Parse(result);
        var found = doc.RootElement.GetProperty("found").GetBoolean();
        List<string>? available = null;
        if (!found && doc.RootElement.TryGetProperty("available", out var av))
            available = av.EnumerateArray().Select(x => x.GetString() ?? "").Where(s => s.Length > 0).ToList();
        return (found, available);
    }

    private static async Task<(bool HasNextSelect, List<string>? Options, List<string>? Leaves)> ReadStateAsync(
        IKdpBrowser browser, int afterLevel, CancellationToken ct)
    {
        var result = await browser.EvalAsync($$"""
        (function() {
            var all = Array.from(document.querySelectorAll('select'));
            var anchor = all.findIndex(function (s) { return s.options.length > 1 && s.options[0].value.indexOf('"level":0') !== -1; });
            var sels = anchor === -1 ? [] : all.slice(anchor);
            if (sels.length > {{afterLevel}}) {
                var next = sels[{{afterLevel}}];
                var opts = Array.from(next.options).map(function (o) { return o.textContent.trim(); })
                    .filter(function (t) { return t.length > 0 && t.toLowerCase() !== 'select one' && t.toLowerCase() !== 'select a subcategory'; });
                return JSON.stringify({ hasNextSelect: true, options: opts });
            }
            var boxes = Array.from(document.querySelectorAll('input[type=checkbox]'))
                .filter(function (cb) { return cb.offsetParent !== null; });
            var leaves = boxes.map(function (cb) {
                var lbl = cb.closest('label') || cb.parentElement;
                return (lbl ? lbl.textContent : '').trim();
            }).filter(function (t) { return t.length > 0; });
            return JSON.stringify({ hasNextSelect: false, leaves: leaves });
        })()
        """, ct);
        using var doc = JsonDocument.Parse(result);
        var hasNext = doc.RootElement.GetProperty("hasNextSelect").GetBoolean();
        List<string>? options = null, leaves = null;
        if (hasNext && doc.RootElement.TryGetProperty("options", out var op))
            options = op.EnumerateArray().Select(x => x.GetString() ?? "").Where(s => s.Length > 0).ToList();
        if (!hasNext && doc.RootElement.TryGetProperty("leaves", out var lv))
            leaves = lv.EnumerateArray().Select(x => x.GetString() ?? "").Where(s => s.Length > 0).ToList();
        return (hasNext, options, leaves);
    }
}
