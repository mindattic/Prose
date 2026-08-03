using System.Text.Json;

namespace StreetSamurai.Core.Services.Operator.KdpTools;

/// <summary>
/// Drives KDP's Categories modal on the Details page (opened via the "Choose categories" button,
/// id <c>categories-modal-button</c>, confirmed live) — a cascading picker: a top-level
/// "Category" &lt;select&gt;, zero or more further "Subcategory" &lt;select&gt;s that appear as
/// each choice narrows the tree, and finally a "Placement" panel of native leaf
/// &lt;input type=checkbox&gt;s for the current branch. All confirmed-live as ordinary native
/// elements (unlike the confirm-checkbox/AI-content widgets elsewhere on this flow) — a plain
/// value-setter + dispatched change event on each &lt;select&gt;, then a plain .click() on the
/// leaf checkbox, both work directly.
///
/// Up to 3 categories total (KDP's own limit). Each additional one after the first requires
/// clicking "Add another category" first, which appends a whole new sibling block (its own
/// Category/Subcategory selects + Placement panel) — this tool tags each block with a temporary
/// <c>data-ss-cat-idx</c> attribute the moment it locates that block's top-level select, so later
/// steps for the SAME path can re-locate the right block instead of accidentally touching an
/// earlier category's still-open selects.
/// </summary>
public class SelectCategoriesTool : IKdpTool
{
    public string Name => "select_categories";

    public string Description =>
        "Open KDP's Categories modal and select up to 3 category paths, each an array of level " +
        "names from the top-level Category down to the final Placement checkbox — e.g. " +
        "[[\"Biographies & Memoirs\",\"Historical\",\"General\"], [\"Religion & Spirituality\"," +
        "\"Christianity\",\"History\"]]. Every level name must match the CURRENT dropdown's/" +
        "panel's visible text (case-sensitive-ish substring match) — if a path fails partway, " +
        "the result tells you exactly which level and what options WERE actually available, so " +
        "you can retry with a corrected path rather than guessing blind. Clicks 'Save categories' " +
        "at the end only if every path fully succeeded.";

    public string ParametersJsonSchema => """
    {
      "type": "object",
      "properties": {
        "category_paths": {
          "type": "array",
          "items": { "type": "array", "items": { "type": "string" } },
          "description": "Up to 3 paths, each an ordered list of level names ending in the leaf Placement checkbox's label."
        }
      },
      "required": ["category_paths"]
    }
    """;

    /// <summary>Polls (rather than trusting one fixed delay) for an untagged top-level Category
    /// select to appear — confirmed live necessary: the modal's own React content renders on a
    /// variable delay after the triggering click, and a single fixed wait sized for interactive
    /// testing missed it in an unattended run.</summary>
    private static async Task<bool> WaitForUntaggedTopSelectAsync(KdpOperatorContext ctx, CancellationToken ct)
    {
        const string probe = """
        (function() {
            var untagged = Array.from(document.querySelectorAll('select'))
                .filter(function (s) { return !s.hasAttribute('data-ss-cat-idx') && s.options.length > 1 && s.options[0].value.indexOf('"level":0') !== -1; });
            return JSON.stringify({ ready: untagged.length > 0 });
        })()
        """;
        for (var attempt = 0; attempt < 10; attempt++)
        {
            await Task.Delay(attempt == 0 ? 800 : 500, ct);
            var result = await ctx.Browser.EvalAsync(probe, ct);
            using var doc = JsonDocument.Parse(result);
            if (doc.RootElement.GetProperty("ready").GetBoolean()) return true;
        }
        return false;
    }

    public async Task<string> InvokeAsync(JsonElement args, KdpOperatorContext ctx, CancellationToken ct)
    {
        var paths = args.GetProperty("category_paths").EnumerateArray()
            .Select(p => p.EnumerateArray().Select(s => s.GetString() ?? "").ToArray())
            .Where(p => p.Length > 0)
            .ToArray();
        if (paths.Length == 0)
            return JsonSerializer.Serialize(new { error = "category_paths was empty." });
        if (paths.Length > 3)
            return JsonSerializer.Serialize(new { error = "KDP allows at most 3 category placements." });

        // Every call is a fresh, complete attempt at the FULL given category_paths — never a
        // resume of a previous partial one. Confirmed live as a real bug: leaving a prior call's
        // data-ss-cat-idx tags in place made a retry after a partial failure think the modal was
        // already open and skip re-locating an (by then untagged-for-index-0) top-level select,
        // permanently wedging every subsequent attempt. Stripping stale tags first makes every
        // call independently reliable regardless of what a prior call left behind — deliberately
        // NOT clicking any "Cancel" button here too (confirmed live: the page has more than one
        // "Cancel" button for unrelated modals/popovers, e.g. archive-title, unlink-titles; a
        // blind textContent match risks clicking the wrong one and disturbing unrelated state).
        var openResult = await ctx.Browser.EvalAsync("""
        (function() {
            Array.from(document.querySelectorAll('[data-ss-cat-idx]')).forEach(function (el) {
                el.removeAttribute('data-ss-cat-idx');
            });
            var btn = document.getElementById('categories-modal-button');
            if (!btn) return JSON.stringify({ found: false });
            btn.click();
            return JSON.stringify({ found: true });
        })()
        """, ct);
        using (var doc = JsonDocument.Parse(openResult))
            if (!doc.RootElement.GetProperty("found").GetBoolean())
                return JsonSerializer.Serialize(new { error = "Couldn't find the 'Choose categories' button — call this only from the Details page." });
        var modalReady = await WaitForUntaggedTopSelectAsync(ctx, ct);
        if (!modalReady)
            return JsonSerializer.Serialize(new { error = "Categories modal never rendered its Category dropdown after clicking 'Choose categories'." });

        var perPathResults = new List<object>();

        for (var i = 0; i < paths.Length; i++)
        {
            var path = paths[i];

            if (i > 0)
            {
                var addResult = await ctx.Browser.EvalAsync("""
                (function() {
                    var btn = Array.from(document.querySelectorAll('button')).find(function (b) {
                        return (b.textContent || '').trim() === 'Add another category';
                    });
                    if (!btn) return JSON.stringify({ found: false });
                    btn.click();
                    return JSON.stringify({ found: true });
                })()
                """, ct);
                using var doc = JsonDocument.Parse(addResult);
                if (!doc.RootElement.GetProperty("found").GetBoolean())
                {
                    perPathResults.Add(new { index = i, ok = false, error = "'Add another category' button not found." });
                    break;
                }
                var newBlockReady = await WaitForUntaggedTopSelectAsync(ctx, ct);
                if (!newBlockReady)
                {
                    perPathResults.Add(new { index = i, ok = false, error = "New category block never rendered after 'Add another category'." });
                    break;
                }
            }

            // Tag this block's top-level Category select with data-ss-cat-idx="{i}" so every
            // later step for THIS path re-locates the same block instead of an earlier one's.
            var tagResult = await ctx.Browser.EvalAsync($$"""
            (function() {
                var untagged = Array.from(document.querySelectorAll('select'))
                    .filter(function (s) { return !s.hasAttribute('data-ss-cat-idx') && s.options.length > 1 && s.options[0].value.indexOf('"level":0') !== -1; });
                if (untagged.length === 0) return JSON.stringify({ found: false });
                untagged[0].setAttribute('data-ss-cat-idx', '{{i}}');
                return JSON.stringify({ found: true });
            })()
            """, ct);
            using (var doc = JsonDocument.Parse(tagResult))
                if (!doc.RootElement.GetProperty("found").GetBoolean())
                {
                    perPathResults.Add(new { index = i, ok = false, error = "Could not find this block's top-level Category select to tag." });
                    break;
                }

            var levelResults = new List<object>();
            var failed = false;
            var leafFoundEarlyOverall = false;

            for (var lvl = 0; lvl < path.Length - 1; lvl++)
            {
                var levelText = path[lvl];
                var isFirst = lvl == 0;
                var levelJs = JsonSerializer.Serialize(levelText);

                var setResult = await ctx.Browser.EvalAsync($$"""
                (function() {
                    var isFirst = {{(isFirst ? "true" : "false")}};
                    var container = document.querySelector('[data-ss-cat-idx="{{i}}"]');
                    var sel;
                    if (isFirst) {
                        sel = container;
                    } else {
                        // Subsequent "Subcategory" selects appear as later <select> siblings in
                        // the same block — the block itself isn't a single easily-selectable
                        // container element, so scope by DOM position: any select AFTER the
                        // tagged one, up until the next tagged (different-index) select.
                        var all = Array.from(document.querySelectorAll('select'));
                        var startIdx = all.indexOf(container);
                        var candidates = [];
                        for (var k = startIdx + 1; k < all.length; k++) {
                            if (all[k].hasAttribute('data-ss-cat-idx')) break;
                            candidates.push(all[k]);
                        }
                        sel = candidates.find(function (s) {
                            return Array.from(s.options).some(function (o) { return o.textContent.trim() === {{levelJs}}; });
                        });
                    }
                    if (!sel) {
                        // No further cascading select rendered — this branch of the tree may be
                        // shallower than the given path assumed (confirmed live: not every
                        // top-level category has the same number of Subcategory levels before
                        // reaching Placement leaves). Check whether this level's text is actually
                        // one of the CURRENT Placement checkboxes instead — if so, this level
                        // itself is the leaf; click it and report so the caller can stop walking
                        // deeper into a path that doesn't go that deep for this branch.
                        var leafNow = Array.from(document.querySelectorAll('input[type=checkbox]')).find(function (cb) {
                            var lbl = cb.closest('label') || cb.parentElement;
                            return (lbl ? lbl.textContent : '').trim() === {{levelJs}};
                        });
                        if (leafNow) {
                            if (!leafNow.checked) leafNow.click();
                            return JSON.stringify({ found: true, leafFoundEarly: true });
                        }
                        return JSON.stringify({ found: false });
                    }
                    var opt = Array.from(sel.options).find(function (o) { return o.textContent.trim() === {{levelJs}}; });
                    if (!opt) {
                        var available = Array.from(sel.options).map(function (o) { return o.textContent.trim(); });
                        return JSON.stringify({ found: false, wrongSelect: true, available: available });
                    }
                    var setter = Object.getOwnPropertyDescriptor(window.HTMLSelectElement.prototype, 'value').set;
                    setter.call(sel, opt.value);
                    sel.dispatchEvent(new Event('change', { bubbles: true }));
                    return JSON.stringify({ found: true });
                })()
                """, ct);

                using var doc = JsonDocument.Parse(setResult);
                if (!doc.RootElement.GetProperty("found").GetBoolean())
                {
                    var available = doc.RootElement.TryGetProperty("available", out var av)
                        ? av.EnumerateArray().Select(x => x.GetString()).ToArray() : null;
                    levelResults.Add(new { level = lvl, text = levelText, ok = false, availableOptions = available });
                    failed = true;
                    break;
                }
                var leafFoundEarly = doc.RootElement.TryGetProperty("leafFoundEarly", out var lfe) && lfe.GetBoolean();
                levelResults.Add(new { level = lvl, text = levelText, ok = true, leafFoundEarly });
                if (leafFoundEarly)
                {
                    // This level's text was itself a Placement checkbox — the path's remaining
                    // segments (including what would have been the final leaf) don't apply to
                    // this shallower branch. Treat the whole path as satisfied here.
                    leafFoundEarlyOverall = true;
                    break;
                }
                await Task.Delay(600, ct);
            }

            if (!failed && !leafFoundEarlyOverall)
            {
                var leafText = path[^1];
                var leafJs = JsonSerializer.Serialize(leafText);
                var leafResult = await ctx.Browser.EvalAsync($$"""
                (function() {
                    var container = document.querySelector('[data-ss-cat-idx="{{i}}"]');
                    var all = Array.from(document.querySelectorAll('select'));
                    var startIdx = all.indexOf(container);
                    // The Placement checkboxes for THIS block live after this block's selects,
                    // before the next tagged block's select (or end of modal).
                    var boundary = document.body;
                    var checkboxes = Array.from(document.querySelectorAll('input[type=checkbox]'));
                    var match = checkboxes.find(function (cb) {
                        var lbl = cb.closest('label') || cb.parentElement;
                        var text = (lbl ? lbl.textContent : '').trim();
                        return text === {{leafJs}} && !cb.checked;
                    });
                    if (!match) {
                        var already = checkboxes.find(function (cb) {
                            var lbl = cb.closest('label') || cb.parentElement;
                            return (lbl ? lbl.textContent : '').trim() === {{leafJs}} && cb.checked;
                        });
                        if (already) return JSON.stringify({ found: true, alreadyChecked: true });
                        var availableLeaves = checkboxes.map(function (cb) {
                            var lbl = cb.closest('label') || cb.parentElement;
                            return (lbl ? lbl.textContent : '').trim();
                        });
                        return JSON.stringify({ found: false, availableOptions: availableLeaves });
                    }
                    match.click();
                    return JSON.stringify({ found: true });
                })()
                """, ct);

                using var doc = JsonDocument.Parse(leafResult);
                if (!doc.RootElement.GetProperty("found").GetBoolean())
                {
                    var available = doc.RootElement.TryGetProperty("availableOptions", out var av)
                        ? av.EnumerateArray().Select(x => x.GetString()).ToArray() : null;
                    levelResults.Add(new { level = path.Length - 1, text = leafText, ok = false, availableOptions = available });
                    failed = true;
                }
                else
                {
                    levelResults.Add(new { level = path.Length - 1, text = leafText, ok = true });
                }
            }

            perPathResults.Add(new { index = i, ok = !failed, levels = levelResults });
            if (failed) break;
        }

        var allOk = perPathResults.All(r => (bool)r.GetType().GetProperty("ok")!.GetValue(r)!);
        string? saveClicked = null;
        if (allOk)
        {
            var saveResult = await ctx.Browser.EvalAsync("""
            (function() {
                var btn = Array.from(document.querySelectorAll('button')).find(function (b) {
                    return (b.textContent || '').trim() === 'Save categories';
                });
                if (!btn) return JSON.stringify({ clicked: false });
                btn.click();
                return JSON.stringify({ clicked: true });
            })()
            """, ct);
            saveClicked = saveResult;
        }

        return JsonSerializer.Serialize(new { allOk, results = perPathResults, saved = saveClicked != null });
    }
}
