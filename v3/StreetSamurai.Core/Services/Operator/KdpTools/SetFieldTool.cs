using System.Text.Json;

namespace StreetSamurai.Core.Services.Operator.KdpTools;

/// <summary>
/// Sets a plain native text/number input's value on the KDP Details/Content/Pricing pages, using
/// the same native-property-setter + dispatched input/change events trick already confirmed live
/// on the manuscript-republish flow's Subtitle field (<see cref="SyncSubtitleTool"/>) — required
/// because a plain <c>input.value = x</c> assignment doesn't notify a React-controlled input's
/// own state, so KDP's own validation never sees the change.
///
/// <paramref name="field"/> is looked up in <see cref="KnownIds"/> first (a small, confirmed-live
/// map of friendly names to KDP's actual stable element ids on the new-listing Details page —
/// title, subtitle, author, keywords). Anything not in that map is treated as a literal DOM id
/// directly, so a not-yet-catalogued field never needs new C# — the agent can pass any id it
/// discovers itself (e.g. via get_page_status or its own reasoning about the page).
/// </summary>
public class SetFieldTool : IKdpTool
{
    /// <summary>Friendly name -> KDP's real element id, confirmed live on the new-listing
    /// Details page (2026-08). Not exhaustive — anything else is tried as a literal id.</summary>
    private static readonly Dictionary<string, string> KnownIds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["title"] = "data-title",
        ["subtitle"] = "data-subtitle",
        ["author_first"] = "data-primary-author-first-name",
        ["author_last"] = "data-primary-author-last-name",
        ["edition_number"] = "data-edition-number",
        ["keyword_0"] = "data-keywords-0",
        ["keyword_1"] = "data-keywords-1",
        ["keyword_2"] = "data-keywords-2",
        ["keyword_3"] = "data-keywords-3",
        ["keyword_4"] = "data-keywords-4",
        ["keyword_5"] = "data-keywords-5",
        ["keyword_6"] = "data-keywords-6",
    };

    public string Name => "set_field";

    public string Description =>
        "Set a plain text/number input's value on the current KDP page. `field` is tried three " +
        "ways in order: (1) a known friendly name (title, subtitle, author_first, author_last, " +
        "edition_number, keyword_0..keyword_6), (2) a literal DOM element id, (3) a label-text " +
        "substring match (case-insensitive) against the nearest input on the page — use this " +
        "third form for fields with no confirmed id yet, e.g. \"List Price\" on the Pricing " +
        "step. Does NOT work for the rich-text Description box (use set_description) or for " +
        "radio/checkbox controls (use select_form_option). Returns {found:false, tried:[...]} " +
        "if nothing matched by any of the three strategies.";

    public string ParametersJsonSchema => """
    {
      "type": "object",
      "properties": {
        "field": { "type": "string", "description": "Friendly name (title, subtitle, author_first, author_last, edition_number, keyword_0..keyword_6) or a literal element id." },
        "value": { "type": "string" }
      },
      "required": ["field", "value"]
    }
    """;

    public async Task<string> InvokeAsync(JsonElement args, KdpOperatorContext ctx, CancellationToken ct)
    {
        var field = args.GetProperty("field").GetString() ?? "";
        var value = args.GetProperty("value").GetString() ?? "";
        if (field.Length == 0)
            return JsonSerializer.Serialize(new { error = "field was empty." });

        var elementId = KnownIds.TryGetValue(field, out var mapped) ? mapped : field;
        var idJs = JsonSerializer.Serialize(elementId);
        var labelJs = JsonSerializer.Serialize(field);
        var valueJs = JsonSerializer.Serialize(value);

        var script = $$"""
        (function() {
            function apply(el) {
                var proto = el.tagName === 'TEXTAREA' ? window.HTMLTextAreaElement.prototype : window.HTMLInputElement.prototype;
                var setter = Object.getOwnPropertyDescriptor(proto, 'value').set;
                setter.call(el, {{valueJs}});
                el.dispatchEvent(new Event('input', { bubbles: true }));
                el.dispatchEvent(new Event('change', { bubbles: true }));
                el.blur();
            }

            var byId = document.getElementById({{idJs}});
            if (byId) { apply(byId); return JSON.stringify({ found: true, strategy: 'id', elementId: {{idJs}}, value: byId.value }); }

            // Label-text fallback — same nearest-input-after-label technique already confirmed
            // live on the Subtitle field (SyncSubtitleTool), for fields with no confirmed id yet.
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
                    if (byFor && (byFor.tagName === 'INPUT' || byFor.tagName === 'TEXTAREA')) input = byFor;
                }
                if (!input) {
                    var container = label.closest('div, form') || label.parentElement;
                    if (container) input = container.querySelector('input[type="text"], input[type="number"], input:not([type]), textarea');
                }
                if (input) { apply(input); return JSON.stringify({ found: true, strategy: 'label', matchedLabel: label.textContent.trim(), value: input.value }); }
            }

            return JSON.stringify({ found: false, tried: [{{idJs}}, {{labelJs}} + ' (label search)'] });
        })()
        """;

        return await ctx.Browser.EvalAsync(script, ct);
    }
}
