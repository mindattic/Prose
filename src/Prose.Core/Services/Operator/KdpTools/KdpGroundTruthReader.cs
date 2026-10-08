using System.Text.Json;

namespace Prose.Core.Services.Operator.KdpTools;

/// <summary>
/// Read-only scrape of the Edit eBook Content step: the manuscript filename currently attached,
/// and — if KDP's UI actually surfaces one on a cold page load, which is unverified; see the
/// remarks on <see cref="Operator.KdpReconcileService"/> — a nearby "last modified" display.
/// Never clicks or types anything. Used by the sequential reconciliation pass to compare what's
/// really live against local expectations, instead of trusting local bookkeeping.
///
/// The only precedent for reading this filename today is <c>GetPageStatusTool</c>'s banner text,
/// which only shows right after an upload in the same session — nobody has confirmed a cold-
/// loaded Content page exposes the same info the same way. This scans the whole visible body
/// text (not just alert-styled elements) so it has the best chance of catching it regardless of
/// where/how KDP displays it; treat the selectors here as a first pass to be verified (and
/// possibly tightened) against a real `--diagnose &lt;CODE&gt; content` dump before being fully
/// trusted.
/// </summary>
internal static class KdpGroundTruthReader
{
    public sealed record Result(string? Filename, string? LastModifiedText, string RawExcerpt);

    private const string Script = """
    (function() {
        var bodyText = (document.body.innerText || '').replace(/\s+/g, ' ').trim();

        // Same filename shape as KdpManifestService.VersionFileRx ("<name> V<n>.epub/.docx"),
        // wherever it appears on the page.
        var fileMatch = bodyText.match(/([\w ,'()&-]+ V\d+\.(?:epub|docx))/i);

        // Best-effort "Last Modified"/"Last saved"/"Last updated" label, if KDP's UI shows one at
        // all near the manuscript info — take a short window of text right after the label.
        var dateMatch = bodyText.match(/(?:last modified|last saved|last updated)[:\s]*([^.]{0,40})/i);

        return JSON.stringify({
            filename: fileMatch ? fileMatch[1].trim() : null,
            lastModifiedText: dateMatch ? dateMatch[1].trim() : null,
            rawExcerpt: bodyText.slice(0, 600)
        });
    })()
    """;

    public static async Task<Result> ReadAsync(IKdpBrowser browser, CancellationToken ct)
    {
        var json = await browser.EvalAsync(Script, ct);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        return new Result(
            root.TryGetProperty("filename", out var f) ? f.GetString() : null,
            root.TryGetProperty("lastModifiedText", out var d) ? d.GetString() : null,
            root.TryGetProperty("rawExcerpt", out var r) ? r.GetString() ?? "" : "");
    }
}
