using System.Text.Json;
using System.Text.RegularExpressions;

namespace Prose.Core.Services.Operator.KdpTools;

/// <summary>
/// Snapshots the current page's title, heading, and any banner-like text (success/error/alert
/// styling) so the LLM can decide whether an upload/publish step actually completed — instead of
/// blind fixed-duration waits, the LLM calls this repeatedly (with its own judgment about
/// spacing) until it sees the confirmation text it's looking for.
/// </summary>
public class GetPageStatusTool : IKdpTool
{
    public string Name => "get_page_status";

    public string Description =>
        "Read the current page's title, main heading, URL, any visible banner/alert text " +
        "(success or error messages, e.g. 'Manuscript uploaded successfully', a publish " +
        "confirmation modal), and isProcessing — true if the page shows any sign the uploaded " +
        "file is still being prepared/converted/scanned server-side (KDP shows a distinct " +
        "in-progress status separate from the eventual success banner). Call this after " +
        "upload_manuscript or click_button to check whether KDP finished processing before " +
        "moving to the next step. If isProcessing is true, do NOT proceed to check_checkbox — " +
        "the confirmation checkboxes are unreliable (or absent) until processing finishes; wait " +
        "and call get_page_status again instead. Call it again after a short pause if nothing " +
        "conclusive shows up yet, rather than assuming success. Also returns titleId, parsed " +
        "from the current URL — populated once KDP has minted one for this listing (after the " +
        "first successful Details save on a brand-new book, or immediately for one you opened " +
        "via find_and_open_book); null before that. Remember it and pass it to mark_published.";

    public string ParametersJsonSchema => """{ "type": "object", "properties": {} }""";

    // isProcessing detection shares KdpFormHelpers.ProcessingWordsPattern rather than keeping its
    // own copy of the regex — a second copy is exactly how KDP's "Running quality check. This
    // could take up to a minute." banner slipped through undetected once already (2026-08-01):
    // it matched neither the old copy here nor the old copy in the checkbox-ticking guard. One
    // pattern maintained in one place (KdpFormHelpers) fixes both call sites at once.
    private static string Script => $$"""
    (function() {
        // KDP's DOM keeps plenty of banner/status elements around HIDDEN (display:none, a
        // collapsed accordion panel, a template for a scenario that doesn't apply this time) —
        // textContent still reads them regardless of visibility. Confirmed live (2026-08) as the
        // real root cause behind a whole family of false-positive lockups on the new-listing
        // flow (a hidden "A manuscript hasn't been uploaded yet" banner reported on the Pricing
        // page for a book whose manuscript WAS accepted; the 3-step tracker's permanent "In
        // Progress..." chip; the "Kindle eBook Preview / Online Preview and Quality Check"
        // section's own static heading). Filtering by actual rendered size catches all of these
        // at once instead of chasing each specific phrase collision one at a time.
        function isVisible(el) {
            var r = el.getBoundingClientRect();
            return r.width > 0 && r.height > 0;
        }

        var banners = Array.from(document.querySelectorAll(
            '[class*="alert"], [class*="success"], [class*="error"], [class*="banner"], [role="alert"], [role="dialog"]'
        )).filter(isVisible)
          .map(function (el) { return (el.textContent || '').trim().replace(/\s+/g, ' '); })
          .filter(function (t) { return t.length > 0 && t.length < 400; });
        var uniqueBanners = Array.from(new Set(banners));

        // KDP shows a distinct "still working on it" status while the uploaded file is being
        // converted/scanned/quality-checked server-side, separate from the eventual success/error
        // banner — this is NOT reliably in a [class*=alert]/[role=alert] element, so also scan
        // short, specifically-classed status chips (visible ones only — see isVisible above).
        // See KdpFormHelpers.ProcessingCheckScript's identical logic and remarks — kept in sync by
        // hand since GetPageStatusTool surfaces this to the LLM directly rather than going through
        // KdpFormHelpers.CheckIsProcessingAsync.
        var processingWords = /{{KdpFormHelpers.ProcessingWordsPattern}}/i;
        var processingEls = Array.from(document.querySelectorAll(
            '[class*="status"], [class*="progress"], [class*="spinner"], [class*="loading"], [class*="processing"], [class*="preparing"], [role="dialog"]'
        )).filter(isVisible)
          .map(function (el) { return (el.textContent || '').trim().replace(/\s+/g, ' '); })
          .filter(function (t) { return t.length > 0 && t.length < 200; });
        var processingMatches = Array.from(new Set(processingEls.filter(function (t) { return processingWords.test(t); })));
        var isProcessing = processingMatches.length > 0;

        var h1 = document.querySelector('h1, h2');
        return JSON.stringify({
            url: location.href,
            title: document.title,
            heading: h1 ? (h1.textContent || '').trim() : null,
            banners: uniqueBanners.slice(0, 8),
            isProcessing: isProcessing,
            processingIndicators: processingMatches.slice(0, 5),
        });
    })()
    """;

    public async Task<string> InvokeAsync(JsonElement args, KdpOperatorContext ctx, CancellationToken ct)
    {
        var result = await ctx.Browser.EvalAsync(Script, ct);
        var titleId = ExtractTitleId(ctx.Browser.CurrentUrl);
        if (titleId == null) return result;

        using var doc = JsonDocument.Parse(result);
        var merged = new Dictionary<string, object?>();
        foreach (var prop in doc.RootElement.EnumerateObject())
            merged[prop.Name] = JsonSerializer.Deserialize<object?>(prop.Value.GetRawText());
        merged["titleId"] = titleId;
        return JsonSerializer.Serialize(merged);
    }

    private static string? ExtractTitleId(string url)
    {
        var m = Regex.Match(url ?? "", @"/title-setup/kindle/([^/]+)/");
        return m.Success && m.Groups[1].Value != "new" ? m.Groups[1].Value : null;
    }
}
