using System.Text.Json;

namespace Prose.Core.Services.Operator.KdpTools;

/// <summary>
/// Fills KDP's AI-generated-content follow-up section on the Content page — confirmed live to
/// be three native &lt;select&gt; dropdowns with stable ids <c>generative-ai-questionnaire-text</c>,
/// <c>-images</c>, and <c>-translations</c> (only rendered, though present in the DOM
/// beforehand, once the top-level "Yes" radio for "Did you use AI tools..." is selected — call
/// select_form_option with ["Yes"] first). Selecting any option other than "None" for text or
/// images reveals a "Which tool(s) did you use..." plain text input directly after that select
/// in DOM order (no stable id — placeholder-only, e.g. "e.g. ChatGPT" / "e.g. DALL-E"); this tool
/// locates it by walking forward from the select to the next input[placeholder] before another
/// select is reached.
/// </summary>
public class SetAiDisclosureTool : IKdpTool
{
    public string Name => "set_ai_disclosure";

    public string Description =>
        "Fill the AI-generated-content follow-up section on the Content page (call " +
        "select_form_option with text_candidates=[\"Yes\"] first to reveal it). Pass the exact " +
        "option text for each of text_option/images_option/translations_option — confirmed live " +
        "values include \"None\", \"Some sections, with minimal or no editing\", \"Some " +
        "sections, with extensive editing\", \"Entire work, with minimal or no editing\", " +
        "\"Entire work, with extensive editing\" (text/translations), or \"One or a few " +
        "AI-generated images, with minimal or no editing\"/\"...with extensive editing\", " +
        "\"Many AI-generated images, with minimal or no editing\"/\"...with extensive editing\" " +
        "(images). If text_option or images_option isn't \"None\", also pass text_tool/" +
        "images_tool (e.g. \"Claude\", \"ChatGPT\") to fill the tool-name field that appears. " +
        "Returns which selects were found/set and whether each tool-name field was located.";

    public string ParametersJsonSchema => """
    {
      "type": "object",
      "properties": {
        "text_option": { "type": "string" },
        "text_tool": { "type": "string", "description": "Tool name, e.g. \"Claude\" — omit or leave blank if text_option is \"None\"." },
        "images_option": { "type": "string" },
        "images_tool": { "type": "string", "description": "Tool name, e.g. \"ChatGPT\" — omit or leave blank if images_option is \"None\"." },
        "translations_option": { "type": "string" }
      },
      "required": ["text_option", "images_option", "translations_option"]
    }
    """;

    public async Task<string> InvokeAsync(JsonElement args, KdpOperatorContext ctx, CancellationToken ct)
    {
        var textOption = args.GetProperty("text_option").GetString() ?? "";
        var textTool = args.TryGetProperty("text_tool", out var tt) ? tt.GetString() ?? "" : "";
        var imagesOption = args.GetProperty("images_option").GetString() ?? "";
        var imagesTool = args.TryGetProperty("images_tool", out var it) ? it.GetString() ?? "" : "";
        var translationsOption = args.GetProperty("translations_option").GetString() ?? "";

        var script = $$"""
        (function() {
            function setSelect(id, optionText) {
                var sel = document.getElementById(id);
                if (!sel) return { found: false };
                var opt = Array.from(sel.options).find(function (o) { return o.textContent.trim() === optionText; });
                if (!opt) return { found: false, availableOptions: Array.from(sel.options).map(function (o) { return o.textContent.trim(); }) };
                var setter = Object.getOwnPropertyDescriptor(window.HTMLSelectElement.prototype, 'value').set;
                setter.call(sel, opt.value);
                sel.dispatchEvent(new Event('change', { bubbles: true }));
                return { found: true, el: sel };
            }

            function setToolInput(selectEl, value) {
                if (!selectEl || !value) return { attempted: false };
                var all = Array.from(document.querySelectorAll('input[placeholder], select'));
                var idx = all.indexOf(selectEl);
                var target = null;
                for (var i = idx + 1; i < all.length; i++) {
                    if (all[i].tagName === 'SELECT') break;
                    if (all[i].hasAttribute('placeholder')) { target = all[i]; break; }
                }
                if (!target) return { attempted: true, found: false };
                var setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
                setter.call(target, value);
                target.dispatchEvent(new Event('input', { bubbles: true }));
                target.dispatchEvent(new Event('change', { bubbles: true }));
                return { attempted: true, found: true };
            }

            var textResult = setSelect('generative-ai-questionnaire-text', {{JsonSerializer.Serialize(textOption)}});
            var imagesResult = setSelect('generative-ai-questionnaire-images', {{JsonSerializer.Serialize(imagesOption)}});
            var translationsResult = setSelect('generative-ai-questionnaire-translations', {{JsonSerializer.Serialize(translationsOption)}});

            var textToolResult = textResult.found ? setToolInput(textResult.el, {{JsonSerializer.Serialize(textTool)}}) : { attempted: false };
            var imagesToolResult = imagesResult.found ? setToolInput(imagesResult.el, {{JsonSerializer.Serialize(imagesTool)}}) : { attempted: false };

            // Note: deliberately NOT including the raw `el` DOM reference in the returned
            // object below — JSON.stringify on a live element throws (circular ownerDocument/
            // parentNode references), so only the plain-data fields are reported back.
            return JSON.stringify({
                text: { found: textResult.found, availableOptions: textResult.availableOptions, toolField: textToolResult },
                images: { found: imagesResult.found, availableOptions: imagesResult.availableOptions, toolField: imagesToolResult },
                translations: { found: translationsResult.found, availableOptions: translationsResult.availableOptions }
            });
        })()
        """;

        return await ctx.Browser.EvalAsync(script, ct);
    }
}
