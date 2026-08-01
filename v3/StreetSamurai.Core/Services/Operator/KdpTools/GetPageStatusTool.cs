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
        "Read the current page's title, main heading, URL, and any visible banner/alert text " +
        "(success or error messages, e.g. 'Manuscript uploaded successfully', a publish " +
        "confirmation modal). Call this after upload_manuscript or click_button to check " +
        "whether KDP finished processing before moving to the next step — call it again after " +
        "a short pause if nothing conclusive shows up yet, rather than assuming success.";

    public string ParametersJsonSchema => """{ "type": "object", "properties": {} }""";

    private const string Script = """
    (function() {
        var banners = Array.from(document.querySelectorAll(
            '[class*="alert"], [class*="success"], [class*="error"], [class*="banner"], [role="alert"], [role="dialog"]'
        )).map(function (el) { return (el.textContent || '').trim().replace(/\s+/g, ' '); })
          .filter(function (t) { return t.length > 0 && t.length < 400; });
        var uniqueBanners = Array.from(new Set(banners));
        var h1 = document.querySelector('h1, h2');
        return JSON.stringify({
            url: location.href,
            title: document.title,
            heading: h1 ? (h1.textContent || '').trim() : null,
            banners: uniqueBanners.slice(0, 8),
        });
    })()
    """;

    public async Task<string> InvokeAsync(JsonElement args, KdpOperatorContext ctx, CancellationToken ct)
        => await ctx.Browser.EvalAsync(Script, ct);
}
