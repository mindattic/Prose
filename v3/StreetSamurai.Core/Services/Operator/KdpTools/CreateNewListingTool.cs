using System.Text.Json;

namespace StreetSamurai.Core.Services.Operator.KdpTools;

/// <summary>
/// Starts a brand-new Kindle eBook listing from the bookshelf — the entry point for a book that
/// has never been published before (no ASIN, no KdpTitleId). Confirmed live: the bookshelf's
/// "+ Create new title or series" link (id <c>a-autoid-0-announce</c>) navigates to
/// <c>/en_US/create</c>, a format-choice page whose "Create eBook" button then lands on
/// <c>/en_US/title-setup/kindle/new/details</c> — an empty Details form with no titleId yet. KDP
/// only mints a real titleId once Details is saved for the first time (confirmed live: the URL
/// becomes <c>/title-setup/kindle/&lt;titleId&gt;/content</c> right after that first successful
/// Save and Continue) — call get_page_status afterward to capture it, the same way
/// find_and_open_book's titleId is captured for a republish.
/// </summary>
public class CreateNewListingTool : IKdpTool
{
    public string Name => "create_new_listing";

    public string Description =>
        "Start a brand-new Kindle eBook listing (for a book with no existing ASIN/KDP title — " +
        "never call this for a book that's already on the bookshelf, use find_and_open_book " +
        "instead). Navigates the bookshelf's '+ Create new title or series' flow and lands on " +
        "an empty 'Kindle eBook Details' page. No titleId exists yet — it's minted only once " +
        "Details is saved for the first time; call get_page_status after that save to capture " +
        "it from the URL, then pass it to mark_published like any other run.";

    public string ParametersJsonSchema => """{ "type": "object", "properties": {} }""";

    public async Task<string> InvokeAsync(JsonElement args, KdpOperatorContext ctx, CancellationToken ct)
    {
        try
        {
            await ctx.Browser.EvalAsync("window.location.href = 'https://kdp.amazon.com/en_US/bookshelf'; ''", ct);
        }
        catch { /* navigation tears down the script context; a thrown eval is expected here */ }
        await Task.Delay(2000, ct);

        var clickCreate = await ctx.Browser.EvalAsync("""
        (function() {
            var a = document.getElementById('a-autoid-0-announce');
            if (!a) return JSON.stringify({found: false, step: 'bookshelf-create-link'});
            a.click();
            return JSON.stringify({found: true});
        })()
        """, ct);
        using (var doc = JsonDocument.Parse(clickCreate))
            if (!doc.RootElement.GetProperty("found").GetBoolean())
                return clickCreate;
        await Task.Delay(2000, ct);

        var clickEbook = await ctx.Browser.EvalAsync("""
        (function() {
            var btn = Array.from(document.querySelectorAll('button')).find(function (b) {
                return (b.textContent || '').trim() === 'Create eBook';
            });
            if (!btn) return JSON.stringify({found: false, step: 'create-ebook-button'});
            btn.click();
            return JSON.stringify({found: true});
        })()
        """, ct);
        using (var doc = JsonDocument.Parse(clickEbook))
            if (!doc.RootElement.GetProperty("found").GetBoolean())
                return clickEbook;
        await Task.Delay(2000, ct);

        return JsonSerializer.Serialize(new { started = true, url = ctx.Browser.CurrentUrl });
    }
}
