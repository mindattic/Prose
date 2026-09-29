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

    /// <summary>KDP's description limit, counted on the HTML sent to its editor.</summary>
    internal const int MaxDescriptionLength = 4000;

    public string Description =>
        "Set the KDP Details page's product Description (the rich-text box under 'Description'). " +
        "Plain text only — do not include HTML tags. Returns {found:false} if no CKEditor " +
        "instance named editor1 exists on the current page (you're probably not on the Details " +
        "step), or {found:false, error:'description_too_long'} if it exceeds KDP's 4,000-character " +
        "limit once formatted. Returns {found:true, data} with the description as KDP now sees it.";

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
        // CKEditor's setData takes HTML, not text: raw plain text lost every paragraph break (the
        // exported description.txt's blank-line paragraphs, and its appended reading-time line,
        // collapsed into one run-on block) and mis-parsed any '&' or '<' in it.
        var html = PlainTextToHtml(text);
        // KDP caps the description at 4,000 characters and counts the markup, so the paragraph
        // tags above can push a near-limit text over it. Refuse rather than let KDP truncate.
        // Measured with WebUtility's &#39;/&quot; collapsed back: CKEditor stores ' and " in text
        // as themselves, so counting them as 5-6 characters refused descriptions KDP accepts.
        var kdpLength = html.Replace("&#39;", "'").Replace("&quot;", "\"").Length;
        if (kdpLength > MaxDescriptionLength)
            return JsonSerializer.Serialize(new { found = false, error = "description_too_long", length = kdpLength, limit = MaxDescriptionLength });
        var textJs = JsonSerializer.Serialize(html);

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

    /// <summary>Plain text → the HTML CKEditor expects: HTML-escaped, one &lt;p&gt; per
    /// blank-line-separated paragraph, single line breaks kept as &lt;br&gt;.</summary>
    internal static string PlainTextToHtml(string text)
    {
        var paragraphs = System.Text.RegularExpressions.Regex
            .Split(text.Replace("\r\n", "\n").Trim(), @"\n\s*\n")
            .Select(p => p.Trim())
            .Where(p => p.Length > 0);
        return string.Concat(paragraphs.Select(p =>
            "<p>" + System.Net.WebUtility.HtmlEncode(p).Replace("\n", "<br>") + "</p>"));
    }
}
