using System.Text.Json;

namespace Prose.Core.Services.Operator.KdpTools;

/// <summary>
/// Sets the KDP Details page's product Description — a CKEditor rich-text box, not a plain input
/// (confirmed live: the page loads a <c>window.CKEDITOR</c> global with one instance named
/// <c>editor1</c>, hosted in a <c>cke_wysiwyg_frame</c> iframe). <c>CKEDITOR.instances.editor1.
/// setData(text)</c> writes the visible editor content and updates its own backing hidden
/// <c>&lt;input name="data[description]"&gt;</c> — but confirmed live that KDP's own Save-and-
/// Continue validation ("Enter a description.") does NOT see that update from setData()/
/// updateElement() alone; it only clears once the hidden input also receives real 'input'/
/// 'change' DOM events (the same React-notification requirement every other field here has) AND
/// the editor instance itself fires a 'change'/'blur' event. This tool does all three steps.
/// </summary>
public class SetDescriptionTool : IKdpTool
{
    public string Name => "set_description";

    public string Description =>
        "Set the KDP Details page's product Description (the rich-text box under 'Description'). " +
        "Plain text only — do not include HTML tags. Returns {found:false} if no CKEditor " +
        "instance named editor1 exists on the current page (you're probably not on the Details " +
        "step). Returns {found:true, data} with the description as KDP now sees it.";

    public string ParametersJsonSchema => """
    {
      "type": "object",
      "properties": {
        "text": { "type": "string" }
      },
      "required": ["text"]
    }
    """;

    public async Task<string> InvokeAsync(JsonElement args, KdpOperatorContext ctx, CancellationToken ct)
    {
        var text = args.GetProperty("text").GetString() ?? "";
        var textJs = JsonSerializer.Serialize(text);

        var script = $$"""
        (function() {
            if (typeof window.CKEDITOR === 'undefined' || !window.CKEDITOR.instances['editor1'])
                return JSON.stringify({ found: false });

            var ed = window.CKEDITOR.instances['editor1'];
            ed.setData({{textJs}});
            ed.updateElement();

            var hidden = document.querySelector('input[name="data[description]"]');
            if (hidden) {
                var setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
                setter.call(hidden, ed.getData());
                hidden.dispatchEvent(new Event('input', { bubbles: true }));
                hidden.dispatchEvent(new Event('change', { bubbles: true }));
            }
            ed.fire('change');
            ed.fire('blur');

            return JSON.stringify({ found: true, data: ed.getData() });
        })()
        """;

        return await ctx.Browser.EvalAsync(script, ct);
    }
}
