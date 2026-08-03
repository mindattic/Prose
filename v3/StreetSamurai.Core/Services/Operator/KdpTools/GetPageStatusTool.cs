using System.Text.Json;

namespace StreetSamurai.Core.Services.Operator.KdpTools;

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
        "conclusive shows up yet, rather than assuming success.";

    public string ParametersJsonSchema => """{ "type": "object", "properties": {} }""";

    // isProcessing detection shares KdpFormHelpers.ProcessingWordsPattern rather than keeping its
    // own copy of the regex — a second copy is exactly how KDP's "Running quality check. This
    // could take up to a minute." banner slipped through undetected once already (2026-08-01):
    // it matched neither the old copy here nor the old copy in the checkbox-ticking guard. One
    // pattern maintained in one place (KdpFormHelpers) fixes both call sites at once.
    private static string Script => $$"""
    (function() {
        var banners = Array.from(document.querySelectorAll(
            '[class*="alert"], [class*="success"], [class*="error"], [class*="banner"], [role="alert"], [role="dialog"]'
        )).map(function (el) { return (el.textContent || '').trim().replace(/\s+/g, ' '); })
          .filter(function (t) { return t.length > 0 && t.length < 400; });
        var uniqueBanners = Array.from(new Set(banners));

        // KDP shows a distinct "still working on it" status while the uploaded file is being
        // converted/scanned/quality-checked server-side, separate from the eventual success/error
        // banner — this is NOT reliably in a [class*=alert]/[role=alert] element, so scan broadly:
        // both status-shaped class names AND plain visible text containing the words KDP actually
        // uses for this state. Confirmed necessary live: checking the confirmation checkboxes
        // while this is still showing does not reliably stick.
        var processingWords = /{{KdpFormHelpers.ProcessingWordsPattern}}/i;
        var processingEls = Array.from(document.querySelectorAll(
            '[class*="status"], [class*="progress"], [class*="spinner"], [class*="loading"], [class*="processing"], [class*="preparing"]'
        )).map(function (el) { return (el.textContent || '').trim().replace(/\s+/g, ' '); })
          .filter(function (t) { return t.length > 0 && t.length < 300; });
        var bodyText = (document.body.innerText || '').replace(/\s+/g, ' ');
        var processingMatches = Array.from(new Set(processingEls.filter(function (t) { return processingWords.test(t); })));
        var isProcessing = processingMatches.length > 0 || processingWords.test(bodyText);
        // If the only evidence is a raw body-text match (no specific element found), surface the
        // matched snippet so the caller can see what triggered it instead of just a bare flag.
        if (isProcessing && processingMatches.length === 0) {
            var m = bodyText.match(processingWords);
            if (m) {
                var idx = m.index;
                processingMatches.push(bodyText.slice(Math.max(0, idx - 40), idx + 60).trim());
            }
        }

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
        => await ctx.Browser.EvalAsync(Script, ct);
}
