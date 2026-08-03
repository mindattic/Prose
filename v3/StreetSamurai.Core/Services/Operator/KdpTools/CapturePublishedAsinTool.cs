using System.Text.Json;
using System.Text.RegularExpressions;

namespace StreetSamurai.Core.Services.Operator.KdpTools;

/// <summary>
/// Captures a just-published book's real Amazon ASIN from the bookshelf, right after the publish
/// click redirects there — confirmed live the redirect URL carries the KDP-internal titleId as
/// <c>?publishedId=&lt;titleId&gt;</c> (KDP's own dashboard id, NOT the customer-facing ASIN — the
/// "digital_edit_content-" link id suffix is also this titleId, not the ASIN). The real ASIN only
/// appears in that book's "View on Amazon" link href (<c>/dp/&lt;ASIN&gt;</c>). Call this
/// immediately after a confirmed publish, before doing anything else, so mark_published can
/// record the ASIN (and thus the real Amazon URL) on the very first pass instead of leaving it
/// null until a much later manual check.
/// </summary>
public class CapturePublishedAsinTool : IKdpTool
{
    public string Name => "capture_published_asin";

    public string Description =>
        "Read the just-published book's real Amazon ASIN off the current bookshelf page — call " +
        "this right after a publish click redirects to the bookshelf (URL contains " +
        "?publishedId=<titleId>). Matches the book card whose edit-content link carries that " +
        "same titleId, then extracts the ASIN from its 'View on Amazon' link. Falls back to the " +
        "first/topmost book card on the shelf if no titleId match is found (a freshly-published " +
        "book sorts to the top). Returns {found:false} if neither strategy locates an ASIN — " +
        "this is common in the first minutes after publish (Amazon hasn't finished listing it " +
        "yet); don't treat that as an error, just proceed without one and it can be filled in " +
        "later.";

    public string ParametersJsonSchema => """{ "type": "object", "properties": {} }""";

    public async Task<string> InvokeAsync(JsonElement args, KdpOperatorContext ctx, CancellationToken ct)
    {
        var titleId = ExtractPublishedId(ctx.Browser.CurrentUrl);
        var titleIdJs = JsonSerializer.Serialize(titleId ?? "");

        var script = $$"""
        (function() {
            function asinFromCard(card) {
                var link = card.querySelector('a');
                var links = Array.from(card.querySelectorAll('a'));
                for (var i = 0; i < links.length; i++) {
                    var href = links[i].getAttribute('href') || '';
                    var m = href.match(/\/dp\/([A-Z0-9]{10})/);
                    if (m) return m[1];
                }
                return null;
            }

            var titleId = {{titleIdJs}};
            if (titleId) {
                var editLink = document.querySelector('a[id$="-' + titleId + '"], a[id*="' + titleId + '"]');
                if (editLink) {
                    // Walk up to the enclosing book-card container and look for a same-card
                    // "View on Amazon" link.
                    var card = editLink.closest('[class*="card" i], [class*="row" i]') || editLink.parentElement;
                    for (var depth = 0; depth < 6 && card; depth++) {
                        var asin = asinFromCard(card);
                        if (asin) return JSON.stringify({ found: true, asin: asin, strategy: 'titleId-match' });
                        card = card.parentElement;
                    }
                }
            }

            // Fallback: the topmost "View on Amazon" link on the shelf — a freshly published
            // book sorts to the top of the list.
            var allAmazonLinks = Array.from(document.querySelectorAll('a')).filter(function (a) {
                return (a.textContent || '').trim() === 'View on Amazon';
            });
            for (var i = 0; i < allAmazonLinks.length; i++) {
                var href = allAmazonLinks[i].getAttribute('href') || '';
                var m = href.match(/\/dp\/([A-Z0-9]{10})/);
                if (m) return JSON.stringify({ found: true, asin: m[1], strategy: 'topmost-fallback' });
            }

            return JSON.stringify({ found: false });
        })()
        """;

        return await ctx.Browser.EvalAsync(script, ct);
    }

    private static string? ExtractPublishedId(string url)
    {
        var m = Regex.Match(url ?? "", @"[?&]publishedId=([^&]+)");
        return m.Success ? m.Groups[1].Value : null;
    }
}
