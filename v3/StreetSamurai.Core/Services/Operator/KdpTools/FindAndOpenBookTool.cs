using System.Text.Json;
using System.Text.RegularExpressions;

namespace StreetSamurai.Core.Services.Operator.KdpTools;

/// <summary>
/// Finds a book on the KDP bookshelf and clicks through to its "Edit eBook content" page.
/// Three strategies, tried in order of decreasing certainty:
///
/// 1. known_title_id — a direct URL, no search involved at all. Fastest, but the id can go
///    stale (title deleted/recreated) so the landing page's own heading is verified before
///    trusting it.
/// 2. known_asin — confirmed live that typing a book's ASIN into KDP's own "Search by title"
///    box resolves to that exact book with zero ambiguity (unlike title text, which commonly
///    diverges from our DB title by a subtitle/series suffix, e.g. "Bushido Coda" vs "Bushido
///    Coda: A GLMZ Novel | Street Samurai #1"). Preferred whenever available.
/// 3. title text — last resort, fuzzy substring matching against the search results.
///
/// Self-healing across runs: every success path returns the resolved titleId, so the caller
/// (the LLM) can pass it to mark_published, which already upserts it into
/// tools/kdp/title-ids.json — next run on this same book can skip straight to it via strategy 1.
/// ASIN itself never needs separate storage: it's already derivable from Node.PublishUrl (see
/// KdpManifestEntry.Asin), which every previously-published book has.
/// </summary>
public class FindAndOpenBookTool : IKdpTool
{
    public string Name => "find_and_open_book";

    public string Description =>
        "Find a book on the KDP bookshelf and click through to its 'Edit eBook content' page. " +
        "Tries, in order: known_title_id (a direct URL, if you have one), known_asin (typed " +
        "into KDP's own search box — an exact, unambiguous match, unlike title text which " +
        "often diverges from the plain title by a subtitle/series suffix), then the title " +
        "text itself as a last-resort fuzzy search. Returns {found, matchedTitle, titleId} on " +
        "success — remember titleId and pass it to mark_published's titleId parameter later so " +
        "future runs on this same book can skip straight to it. Returns {found:false, " +
        "likelyPublishing:true} if a row matching the search exists but has no edit link — the " +
        "book is already mid-publish (KDP hides the '...' options menu during that window), " +
        "not a real error; call log_note and stop, don't retry or guess another cause. Returns " +
        "{found:false} if nothing matched at all. Call this once per book, at the start.";

    public string ParametersJsonSchema => """
    {
      "type": "object",
      "properties": {
        "title": { "type": "string", "description": "The book's title as it appears on KDP (substring match, case-insensitive) — used only if known_title_id and known_asin are absent or don't pan out." },
        "known_title_id": { "type": "string", "description": "A previously-recorded KDP titleId for this exact book, if you were given one — tried first." },
        "known_asin": { "type": "string", "description": "The book's Amazon ASIN, if you were given one (e.g. from PublishUrl) — searched for directly and matched exactly; tried before title text." }
      },
      "required": ["title"]
    }
    """;

    public async Task<string> InvokeAsync(JsonElement args, KdpOperatorContext ctx, CancellationToken ct)
    {
        var title = args.GetProperty("title").GetString() ?? "";
        var titleJs = JsonSerializer.Serialize(title);
        var knownTitleId = args.TryGetProperty("known_title_id", out var kt) ? kt.GetString() : null;
        var knownAsin = args.TryGetProperty("known_asin", out var ka) ? ka.GetString() : null;

        if (!string.IsNullOrWhiteSpace(knownTitleId))
        {
            var byTitleId = await TryDirectTitleIdAsync(ctx, knownTitleId, titleJs, ct);
            if (byTitleId != null) return byTitleId;
            // Stale/wrong/404 — fall through.
        }

        if (!string.IsNullOrWhiteSpace(knownAsin))
        {
            var byAsin = await TrySearchAsync(ctx, knownAsin, JsonSerializer.Serialize(knownAsin), isAsin: true, ct);
            if (byAsin != null) return byAsin;
            // ASIN search found nothing (delisted? typo in our stored value?) — fall through.
        }

        return await TrySearchAsync(ctx, title, titleJs, isAsin: false, ct)
            ?? JsonSerializer.Serialize(new { found = false });
    }

    private static async Task<string?> TryDirectTitleIdAsync(
        KdpOperatorContext ctx, string titleId, string titleJs, CancellationToken ct)
    {
        try
        {
            await ctx.Browser.EvalAsync(
                $"window.location.href = 'https://kdp.amazon.com/en_US/title-setup/kindle/{titleId}/content'; ''", ct);
        }
        catch { /* navigation tears down the script context; a thrown eval is expected here */ }
        await Task.Delay(2500, ct);

        var result = await ctx.Browser.EvalAsync(VerifyDirectNavScript(titleJs), ct);
        using var doc = JsonDocument.Parse(result);
        if (!doc.RootElement.GetProperty("found").GetBoolean()) return null;

        return JsonSerializer.Serialize(new
        {
            found = true,
            matchedTitle = doc.RootElement.GetProperty("matchedTitle").GetString(),
            titleId
        });
    }

    /// <summary>Types <paramref name="searchText"/> into KDP's search box, hits Enter, and
    /// either exact-matches an ASIN or fuzzy-matches title text among the results. Returns null
    /// (not a {found:false} JSON string) when nothing at all matched, so the caller can
    /// distinguish "this strategy found nothing, try the next one" from a real terminal
    /// {found:false} result worth returning to the LLM.</summary>
    private static async Task<string?> TrySearchAsync(
        KdpOperatorContext ctx, string searchText, string queryJs, bool isAsin, CancellationToken ct)
    {
        try
        {
            await ctx.Browser.EvalAsync("window.location.href = 'https://kdp.amazon.com/en_US/bookshelf'; ''", ct);
        }
        catch { /* navigation tears down the script context; a thrown eval is expected here */ }
        await Task.Delay(2000, ct);

        await ctx.Browser.EvalAsync(TypeIntoSearchBoxScript(JsonSerializer.Serialize(searchText)), ct);
        await Task.Delay(2500, ct);

        var searchResult = await ctx.Browser.EvalAsync(SearchAndClickScript(queryJs, isAsin), ct);
        using var doc = JsonDocument.Parse(searchResult);
        if (!doc.RootElement.GetProperty("found").GetBoolean())
        {
            // Distinguish "not found at all" (try the next strategy) from "found the row but it
            // has no edit link" (a real, terminal, reportable state — return it as-is).
            if (doc.RootElement.TryGetProperty("likelyPublishing", out var lp) && lp.GetBoolean())
                return searchResult;
            return null;
        }

        await Task.Delay(1500, ct);
        var titleId = ExtractTitleId(ctx.Browser.CurrentUrl);
        return JsonSerializer.Serialize(new
        {
            found = true,
            matchedTitle = doc.RootElement.GetProperty("matchedTitle").GetString(),
            titleId
        });
    }

    private static string? ExtractTitleId(string url)
    {
        var m = Regex.Match(url ?? "", @"/title-setup/kindle/([^/]+)/");
        return m.Success ? m.Groups[1].Value : null;
    }

    private static string VerifyDirectNavScript(string titleJsonLiteral) => $$"""
    (function() {
        // A direct titleId URL landing on the wrong/stale book (or a 404) must not be silently
        // accepted — confirm the page's own heading actually mentions our title before trusting it.
        function normalize(s) { return (s || '').toLowerCase().replace(/[^a-z0-9]+/g, ' ').trim(); }
        var query = normalize({{titleJsonLiteral}});
        var heading = document.querySelector('h1, h2');
        var headingText = normalize(heading ? heading.textContent : '');
        var bodyText = normalize(document.body.innerText || '');
        var ok = (headingText && (headingText.indexOf(query) !== -1 || query.indexOf(headingText) !== -1))
              || bodyText.indexOf(query) !== -1;
        return JSON.stringify({ found: !!ok, matchedTitle: heading ? heading.textContent : null });
    })()
    """;

    private static string TypeIntoSearchBoxScript(string searchTextJsonLiteral) => $$"""
    (function() {
        // "Search by title" — an input near that label text, not a fixed id/class (KDP's own
        // markup, no stable selector guaranteed). Fall back to any single visible text input on
        // the bookshelf toolbar if the label-based lookup doesn't find one. Confirmed live that
        // this same box also resolves an ASIN typed into it to the exact matching book.
        var input = null;
        var labels = Array.from(document.querySelectorAll('label, span, div')).filter(function (el) {
            return (el.textContent || '').trim().toLowerCase() === 'search by title';
        });
        for (var i = 0; i < labels.length && !input; i++) {
            var container = labels[i].closest('div, form') || labels[i].parentElement;
            if (container) input = container.querySelector('input[type="text"], input[type="search"], input:not([type])');
        }
        if (!input) input = document.querySelector('input[type="search"]');
        if (!input) return JSON.stringify({ typed: false });

        var setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
        setter.call(input, {{searchTextJsonLiteral}});
        input.dispatchEvent(new Event('input', { bubbles: true }));
        input.dispatchEvent(new Event('change', { bubbles: true }));
        input.focus();

        // Confirmed live: setting .value + form submission does NOT actually execute the
        // search (the box shows the typed text but the list stays unfiltered) — this search
        // box only reacts to a real Enter keypress on the input, the same as a human typing
        // and hitting Enter. Dispatch a full keydown/keypress/keyup sequence with Enter's real
        // key code, then ALSO click a "Search" button as a harmless fallback in case the page
        // instead wires its handler there.
        ['keydown', 'keypress', 'keyup'].forEach(function (type) {
            input.dispatchEvent(new KeyboardEvent(type, {
                key: 'Enter', code: 'Enter', keyCode: 13, which: 13, bubbles: true, cancelable: true
            }));
        });
        var btn = Array.from(document.querySelectorAll('button')).find(function (b) {
            return (b.textContent || '').trim().toLowerCase() === 'search';
        });
        if (btn) btn.click();

        return JSON.stringify({ typed: true });
    })()
    """;

    private static string SearchAndClickScript(string queryJsonLiteral, bool isAsin) => $$"""
    (function() {
        function normalize(s) { return (s || '').toLowerCase().replace(/[^a-z0-9]+/g, ' ').trim(); }
        function coreMatch(a, b) {
            if (!a || !b) return false;
            return a.indexOf(b) !== -1 || b.indexOf(a) !== -1;
        }

        var isAsinSearch = {{(isAsin ? "true" : "false")}};
        var rawQuery = {{queryJsonLiteral}};
        var query = normalize(rawQuery);
        var links = Array.from(document.querySelectorAll('a[id^="digital_edit_content-"]'));

        for (var i = 0; i < links.length; i++) {
            var el = links[i];
            var raw = el.getAttribute('data-link-parameters');
            var parsed = {};
            try { parsed = JSON.parse(raw) || {}; } catch (e) {}
            var linkTitle = parsed.title || '';

            if (isAsinSearch) {
                // An ASIN search already narrowed the bookshelf to (at most) the one matching
                // book server-side — confirmed live. Prefer an exact ASIN field match if the
                // link's own data-link-parameters carries one; otherwise, since the search
                // itself already did the narrowing, the first (only) result IS the match.
                var linkAsin = (parsed.asin || parsed.ASIN || '').toString().toLowerCase();
                if (!linkAsin || linkAsin === rawQuery.toLowerCase()) {
                    el.click();
                    return JSON.stringify({ found: true, matchedTitle: linkTitle || null });
                }
                continue;
            }

            // Title-text path: our DB title ("Bushido Coda") and KDP's displayed title
            // ("Bushido Coda: A GLMZ Novel | Street Samurai #1") commonly diverge by a
            // subtitle/series suffix. Strip punctuation entirely and match bidirectionally;
            // fall back to walking ancestor levels (KDP's bookshelf is a card grid, not
            // classic <tr>/<li> rows, so a single closest() selector guess can miss the title
            // text) — nearer levels are subsets of farther ones, so this can't false-positive
            // onto an unrelated book above/below in the grid.
            var candidates = [linkTitle];
            var node = el;
            for (var depth = 0; depth < 6 && node.parentElement; depth++) {
                node = node.parentElement;
                candidates.push(node.textContent || '');
            }
            var matched = null;
            for (var c = 0; c < candidates.length; c++) {
                if (coreMatch(normalize(candidates[c]), query)) { matched = candidates[c]; break; }
            }
            if (matched) {
                el.click();
                return JSON.stringify({ found: true, matchedTitle: linkTitle || matched.slice(0, 200) });
            }
        }

        // No edit link matched — but a search returning a book CARD with no edit link at all
        // is a real, specific, recognizable state: KDP hides the "..." options/edit menu on a
        // row while that edition is already mid-publish. Distinguish this from "book doesn't
        // exist at all" by checking whether the results contain ANY text matching the query,
        // even without a clickable edit link nearby.
        var pageText = normalize(document.body.innerText || '');
        var likelyPublishing = pageText.indexOf(query) !== -1;
        return JSON.stringify({ found: false, likelyPublishing: likelyPublishing });
    })()
    """;
}
