using System.Text.Json;

namespace Prose.Core.Services.Operator.KdpTools;

/// <summary>
/// Sets the KDP Pricing page's primary (Amazon.com / US) list price via REAL keystroke dispatch
/// (<see cref="IKdpBrowser.TypeTextAsync"/>) rather than the native-property-setter technique
/// <see cref="SetFieldTool"/> uses for every other text field. Added after a specific, unresolved
/// concern: KDP derives every other marketplace's price (UK, DE, FR, JP, ...) from whatever is
/// typed into this one field, and that derivation may be wired to genuine keystroke events —
/// the same class of "synthetic events aren't enough" problem already confirmed live for the
/// custom checkbox/radio widgets elsewhere in this flow, just on the keyboard instead of the
/// mouse. A single value-setter dispatch is cheap to verify wrong only by checking the live
/// international prices after Save-and-Continue; typing it for real removes the ambiguity
/// instead of relying on that after-the-fact check.
/// </summary>
public class SetPriceTool : IKdpTool
{
    public string Name => "set_price";

    public string Description =>
        "Set the Pricing page's primary list price (the Amazon.com / US price field) by REAL " +
        "keystroke dispatch — use this instead of set_field for the price, since KDP derives " +
        "every other marketplace's price from this one field and that derivation may depend on " +
        "genuine typing rather than a value that merely appears in the input. Locates the field " +
        "the same way set_field's label fallback does (searching for a label matching " +
        "label_text, e.g. \"Amazon.com\"). Clears any existing value first, then types the given " +
        "price digit by digit. Returns {found:false} if no input near that label exists on the " +
        "current page.";

    public string ParametersJsonSchema => """
    {
      "type": "object",
      "properties": {
        "label_text": { "type": "string", "description": "Label text near the price field, e.g. \"Amazon.com\"." },
        "price": { "type": "string", "description": "The price to type, e.g. \"0.99\"." }
      },
      "required": ["label_text", "price"]
    }
    """;

    public async Task<string> InvokeAsync(JsonElement args, KdpOperatorContext ctx, CancellationToken ct)
    {
        var labelText = args.GetProperty("label_text").GetString() ?? "";
        var price = args.GetProperty("price").GetString() ?? "";
        if (labelText.Length == 0 || price.Length == 0)
            return JsonSerializer.Serialize(new { error = "label_text and price are both required." });

        var labelJs = JsonSerializer.Serialize(labelText);
        var locateResult = await ctx.Browser.EvalAsync($$"""
        (function() {
            var query = {{labelJs}}.trim().toLowerCase();
            var labelEls = Array.from(document.querySelectorAll('label, span, div, legend, h5')).filter(function (el) {
                var t = (el.textContent || '').trim().toLowerCase();
                return t === query || t.indexOf(query) === 0;
            });
            for (var i = 0; i < labelEls.length; i++) {
                var label = labelEls[i];
                var input = null;
                if (label.htmlFor) {
                    var byFor = document.getElementById(label.htmlFor);
                    if (byFor && byFor.tagName === 'INPUT') input = byFor;
                }
                if (!input) {
                    var container = label.closest('div, form') || label.parentElement;
                    if (container) input = container.querySelector('input[type="text"], input[type="number"], input:not([type])');
                }
                if (input) {
                    input.scrollIntoView({ block: 'center', inline: 'center' });
                    var rect = input.getBoundingClientRect();
                    return JSON.stringify({ found: true, centerX: rect.left + rect.width / 2, centerY: rect.top + rect.height / 2 });
                }
            }
            return JSON.stringify({ found: false });
        })()
        """, ct);

        using var doc = JsonDocument.Parse(locateResult);
        if (!doc.RootElement.GetProperty("found").GetBoolean())
            return JsonSerializer.Serialize(new { found = false });

        var centerX = doc.RootElement.GetProperty("centerX").GetDouble();
        var centerY = doc.RootElement.GetProperty("centerY").GetDouble();

        // A real click both focuses the field AND (via its native text-input behavior) puts the
        // caret in it — then a real select-all + real typing replaces the existing value with
        // exactly what a human typing over it would produce.
        await ctx.Browser.ClickAtPointAsync(centerX, centerY, ct);
        await Task.Delay(150, ct);
        await ctx.Browser.EvalAsync("(function(){ var el = document.activeElement; if (el && el.select) el.select(); return 'ok'; })()", ct);
        await Task.Delay(100, ct);
        await ctx.Browser.TypeTextAsync(price, ct);
        await Task.Delay(200, ct);

        var readBack = await ctx.Browser.EvalAsync("(function(){ var el = document.activeElement; return JSON.stringify({ value: el ? el.value : null }); })()", ct);
        using var readDoc = JsonDocument.Parse(readBack);
        return JsonSerializer.Serialize(new { found = true, typed = price, value = readDoc.RootElement.GetProperty("value").GetString() });
    }
}
