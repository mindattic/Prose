using System.Text.Json;

namespace StreetSamurai.Core.Services.Operator.KdpTools;

/// <summary>
/// Shared checkbox-ticking logic used by both <see cref="CheckCheckboxTool"/> (explicit,
/// LLM-invoked) and <see cref="ClickButtonTool"/> (automatic, invoked unconditionally before
/// every Save/Continue/Publish click). Confirmed live across two separate runs that the LLM
/// agent successfully uploaded a manuscript but then skipped straight to clicking Save and
/// Continue without ever calling check_checkbox first — a real, observed failure mode of relying
/// on an LLM to correctly sequence two separate tool calls across an 11-step flow. Ticking these
/// automatically inside click_button removes the possibility of that step being skipped: ticking
/// an already-checked or nonexistent checkbox is a harmless no-op, so this is safe to run
/// unconditionally on every click_button call, not just ones the LLM thinks need it.
/// </summary>
internal static class KdpFormHelpers
{
    private const int MaxIterations = 10;

    /// <summary>
    /// Single source of truth for the words that mean "KDP is still working on this server-side
    /// (converting/scanning/quality-checking the upload) — do not trust the form yet." Shared by
    /// <see cref="GetPageStatusTool"/> (surfaced to the LLM as `isProcessing`) and the hard guard
    /// in <see cref="CheckIsProcessingAsync"/> below. Before this was unified, each tool kept its
    /// own copy of this regex — which is exactly how KDP's "Running quality check. This could
    /// take up to a minute." banner slipped through undetected (2026-08-01): it matched neither
    /// copy, because "quality check" isn't a substring of "processing"/"scanning"/etc. One
    /// pattern maintained in one place means a newly-discovered KDP status phrase only needs
    /// adding here to fix both the LLM's visibility into it AND the tick-refusal below.
    ///
    /// A bare "in progress" was removed 2026-08 after a confirmed live false-positive on the
    /// new-listing Details page: KDP's own 3-step tracker (Details/Content/Pricing, each showing
    /// "Complete" / "In Progress..." / "Not Started...") permanently labels whichever step you're
    /// currently on "In Progress..." — that's a static UI chip, not a real processing signal, and
    /// it blocked check_checkbox/click_button indefinitely on a page that was never actually busy.
    /// "upload in progress" (the real, specific phrasing) still matches; the bare standalone
    /// phrase no longer does.
    ///
    /// A trailing negative lookahead for "complete/finished/done" was added 2026-08 after a
    /// second confirmed live false-positive on the new-listing Content page: KDP's OWN success
    /// banner reads "File processing complete. Manuscript check complete." — which contains the
    /// bare words "processing" and "check" and was blocking every subsequent step indefinitely
    /// even though the banner is explicitly announcing completion, not describing ongoing work.
    /// </summary>
    public const string ProcessingWordsPattern =
        "(preparing|processing|converting|scanning|please wait|uploading|upload.{0,10}in progress|is not (yet )?ready|quality check|running.{0,20}check)(?!\\s*(is\\s+)?(complete|completed|finished|done|successfully))";

    /// <summary>Result of a checkbox-ticking attempt. <see cref="BlockedByProcessing"/> true means
    /// nothing was ticked because KDP's processing/quality-check banner was showing — the caller
    /// must not proceed to Save/Continue/Publish, and should surface <see cref="ProcessingIndicator"/>
    /// to the LLM so it knows to wait and retry instead of assuming the checkboxes are just absent.</summary>
    public sealed record TickResult(List<string> Matches, bool BlockedByProcessing, string? ProcessingIndicator);

    /// <summary>True (plus the matched snippet) if the page shows KDP's server-side processing/
    /// quality-check banner. See <see cref="ProcessingWordsPattern"/> for why this exists as a
    /// single shared check rather than being reimplemented per-tool.</summary>
    public static async Task<(bool IsProcessing, string? Indicator)> CheckIsProcessingAsync(
        KdpOperatorContext ctx, CancellationToken ct)
    {
        var result = await ctx.Browser.EvalAsync(ProcessingCheckScript, ct);
        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        var isProcessing = root.GetProperty("isProcessing").GetBoolean();
        var indicator = isProcessing && root.TryGetProperty("indicator", out var el) ? el.GetString() : null;
        return (isProcessing, indicator);
    }

    /// <summary>Ticks every unchecked confirm-checkbox matching <paramref name="candidates"/>
    /// (KDP's real markup for these is a custom &lt;div role=checkbox&gt; widget, not a native
    /// input — see CheckCheckboxTool's remarks for the full story). Refuses to click anything —
    /// returning <see cref="TickResult.BlockedByProcessing"/> instead — while KDP's processing/
    /// quality-check banner is showing; checkboxes are confirmed unreliable until it clears, and
    /// this guard applies unconditionally so it can't be skipped by an agent that forgot to call
    /// get_page_status first (a real, previously-observed failure mode — see class remarks).</summary>
    public static async Task<TickResult> TickMatchingCheckboxesAsync(
        KdpOperatorContext ctx, string[] candidates, CancellationToken ct)
    {
        var (isProcessing, indicator) = await CheckIsProcessingAsync(ctx, ct);
        if (isProcessing) return new TickResult(new List<string>(), true, indicator);

        var candidatesJson = JsonSerializer.Serialize(candidates);
        var matches = new List<string>();

        // Process one match at a time rather than collecting all rects up front — clicking/
        // scrolling one element can shift page layout enough to invalidate other elements'
        // coordinates. Re-locating "the next unchecked match" after each click sidesteps that
        // entirely: if the previous click actually worked, that element no longer matches
        // "unchecked" and the loop naturally advances; if it didn't work, the loop would spin on
        // the same element forever, so it's capped at MaxIterations as a safety backstop.
        for (var iter = 0; iter < MaxIterations; iter++)
        {
            var locateResult = await ctx.Browser.EvalAsync(LocateNextScript(candidatesJson), ct);
            using var doc = JsonDocument.Parse(locateResult);
            var root = doc.RootElement;
            if (!root.GetProperty("found").GetBoolean()) break;

            var text = root.GetProperty("text").GetString() ?? "";
            var centerX = root.GetProperty("centerX").GetDouble();
            var centerY = root.GetProperty("centerY").GetDouble();

            await ctx.Browser.ClickAtPointAsync(centerX, centerY, ct);
            await Task.Delay(300, ct);
            matches.Add(text);
        }

        return new TickResult(matches, false, null);
    }

    // Deliberately scoped-elements-only, no whole-page bodyText fallback, AND visible-only:
    // confirmed live (2026-08) as the repeated root cause of a whole family of false-positive
    // lockups on the new-listing flow — KDP's DOM keeps plenty of static help text, hidden
    // banner templates, and permanent section headings that happen to CONTAIN these words
    // without describing active work ("Kindle eBook Preview / Online Preview and Quality
    // Check...", the 3-step tracker's "In Progress...", the manuscript success banner's own
    // "File processing complete.", a hidden "A manuscript hasn't been uploaded yet" banner for a
    // scenario that didn't apply). A whole-body scan or a hidden-element match catches all of
    // that; a short, specifically-classed, actually-RENDERED status chip does not. Length-capped
    // at 100 (not the old 300) for the same reason — a real status chip is a short phrase, not a
    // paragraph of help text a wide net would also catch.
    //
    // [role="dialog"] was added 2026-08 after a reported live "Preparing your files" modal that
    // can block for up to a minute — it was already visible to the LLM via get_page_status's
    // `banners` scan (which includes role=dialog), but NOT included here in the hard-blocking
    // isProcessing check, since modal containers commonly don't carry a status/progress/spinner/
    // loading/preparing-named class. Without it, a real blocking modal could be invisible to the
    // one check that actually refuses to click/tick while something is still working.
    private static string ProcessingCheckScript => $$"""
    (function() {
        function isVisible(el) {
            var r = el.getBoundingClientRect();
            return r.width > 0 && r.height > 0;
        }
        var processingWords = /{{ProcessingWordsPattern}}/i;
        var processingEls = Array.from(document.querySelectorAll(
            '[class*="status"], [class*="progress"], [class*="spinner"], [class*="loading"], [class*="processing"], [class*="preparing"], [role="dialog"]'
        )).filter(isVisible)
          .map(function (el) { return (el.textContent || '').trim().replace(/\s+/g, ' '); })
          // 200, not the original 100 — a role=dialog modal's full text (title + description +
          // any chrome) commonly runs longer than a small status chip; the completion-lookahead
          // in ProcessingWordsPattern already guards against a long paragraph false-matching.
          .filter(function (t) { return t.length > 0 && t.length < 200; });
        var matches = Array.from(new Set(processingEls.filter(function (t) { return processingWords.test(t); })));
        var isProcessing = matches.length > 0;
        var indicator = matches[0] || null;
        return JSON.stringify({ isProcessing: isProcessing, indicator: indicator });
    })()
    """;

    private static string LocateNextScript(string candidatesJson) => $$"""
    (function() {
        var candidates = {{candidatesJson}}.map(function (c) { return c.toLowerCase(); });

        // A checkbox's visible label isn't reliably in one specific place — real-world forms
        // wrap it in <label>, or use a <label for=id> sibling, or just put plain text as a
        // sibling/cousin with no label element at all. Rather than guess one structure, walk
        // up several ancestor levels AND check immediate siblings, taking the first (nearest)
        // text blob that contains the candidate — nearer levels are strict subsets of farther
        // ones for text content, so this can't accidentally match the wrong checkbox.
        function candidateTexts(cb) {
            var texts = [];
            var label = cb.closest('label');
            if (label) texts.push(label.textContent);
            if (cb.id) {
                var forLabel = document.querySelector('label[for="' + cb.id + '"]');
                if (forLabel) texts.push(forLabel.textContent);
            }
            var node = cb;
            for (var depth = 0; depth < 5 && node.parentElement; depth++) {
                node = node.parentElement;
                texts.push(node.textContent);
            }
            if (cb.nextElementSibling) texts.push(cb.nextElementSibling.textContent);
            return texts.map(function (t) { return (t || '').trim().toLowerCase(); }).filter(Boolean);
        }

        function isChecked(el) {
            return el.tagName === 'INPUT' ? el.checked : el.getAttribute('aria-checked') === 'true';
        }

        // Match BOTH native <input type=checkbox> AND custom accessible <div role=checkbox>
        // widgets — KDP's real confirm control is the latter, not the former.
        var boxes = Array.from(document.querySelectorAll('input[type=checkbox], [role=checkbox]'));
        for (var i = 0; i < boxes.length; i++) {
            var cb = boxes[i];
            if (isChecked(cb)) continue;
            var texts = cb.tagName === 'INPUT' ? candidateTexts(cb) : [(cb.textContent || '').trim().toLowerCase()];
            for (var t = 0; t < texts.length; t++) {
                for (var j = 0; j < candidates.length; j++) {
                    if (texts[t].indexOf(candidates[j]) !== -1) {
                        cb.scrollIntoView({ block: 'center', inline: 'center' });
                        var rect = cb.getBoundingClientRect();
                        return JSON.stringify({
                            found: true,
                            text: texts[t].slice(0, 200),
                            centerX: rect.left + rect.width / 2,
                            centerY: rect.top + rect.height / 2
                        });
                    }
                }
            }
        }
        return JSON.stringify({ found: false });
    })()
    """;
}
