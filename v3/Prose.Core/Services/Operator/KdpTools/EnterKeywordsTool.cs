using System.Text.Json;

namespace Prose.Core.Services.Operator.KdpTools;

/// <summary>
/// Fills KDP's 7 keyword fields on the Details page — confirmed live these are 7 separate plain
/// text inputs with stable ids <c>data-keywords-0</c> through <c>data-keywords-6</c> (NOT a
/// single Enter-to-commit box, despite how older KDP UIs and this project's own legacy
/// kdp.prompt.new_ebook template describe it). One tool rather than 7 set_field calls because
/// the whole group is always filled together and a single result is easier to verify at a glance.
/// </summary>
public class EnterKeywordsTool : IKdpTool
{
    public string Name => "enter_keywords";

    public string Description =>
        "Fill KDP's 7 keyword fields on the Details page. Pass up to 7 keyword phrases — fewer " +
        "than 7 is fine, extra fields are simply left blank. Returns {filled:[...]} listing what " +
        "was actually set into each of the 7 fields.";

    public string ParametersJsonSchema => """
    {
      "type": "object",
      "properties": {
        "keywords": {
          "type": "array",
          "items": { "type": "string" },
          "description": "Up to 7 keyword phrases, in order."
        }
      },
      "required": ["keywords"]
    }
    """;

    public async Task<string> InvokeAsync(JsonElement args, KdpOperatorContext ctx, CancellationToken ct)
    {
        var keywords = args.GetProperty("keywords").EnumerateArray().Select(x => x.GetString() ?? "").Take(7).ToArray();
        var keywordsJs = JsonSerializer.Serialize(keywords);

        var script = $$"""
        (function() {
            var keywords = {{keywordsJs}};
            var setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
            var filled = [];
            for (var i = 0; i < keywords.length; i++) {
                var el = document.getElementById('data-keywords-' + i);
                if (!el) { filled.push({ index: i, found: false }); continue; }
                setter.call(el, keywords[i]);
                el.dispatchEvent(new Event('input', { bubbles: true }));
                el.dispatchEvent(new Event('change', { bubbles: true }));
                filled.push({ index: i, found: true, value: el.value });
            }
            return JSON.stringify({ filled: filled });
        })()
        """;

        return await ctx.Browser.EvalAsync(script, ct);
    }
}
