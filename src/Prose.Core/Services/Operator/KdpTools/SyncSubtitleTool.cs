using System.Text.Json;
using System.Text.RegularExpressions;

namespace Prose.Core.Services.Operator.KdpTools;

/// <summary>
/// Navigates to the book's KDP "Details" step, reads the live Subtitle field, and corrects it
/// if it doesn't match our DB value — the first field-diff (read, compare, write-only-if-
/// different) tool in the KDP automation surface. Every other tool here is a one-way action
/// (click, tick, upload); this one exists because Subtitle is metadata we now generate and
/// maintain in our own DB (Nodes.Subtitle, e.g. "A GLMZ Novella") independent of the manuscript
/// file, so republishing a book must also reconcile that field on KDP, not just replace the
/// manuscript.
///
/// find_and_open_book lands on the Content step directly (its titleId URL ends in /content), so
/// this tool has to detour backward to /details, make its check/edit, then rely on the agent's
/// own Save and Continue click (via the existing check_checkbox + click_button tools — this tool
/// deliberately does NOT click anything itself, matching the one-tool-one-job shape of
/// GetPageStatusTool/CheckCheckboxTool/ClickButtonTool) to land back on Content, exactly where
/// the rest of the existing 11-step flow already expects to be.
///
/// NOT yet confirmed live: the exact Subtitle field markup and the /details URL path. Modeled on
/// FindAndOpenBookTool's confirmed-live /content URL pattern and its confirmed-live technique for
/// setting a native input's value under React (the native property setter + dispatched
/// input/change events — the same trick already proven to work on KDP's search box). Unlike the
/// checkbox widget (a custom div that specifically ignores synthetic clicks), a Subtitle field is
/// expected to be an ordinary &lt;input&gt;, which is the class of element that trick is known to
/// work on — but this should be treated as unverified until a real run confirms it, the same way
/// every other KDP-specific detail in this codebase was pinned down by live observation.
/// </summary>
public class SyncSubtitleTool : IKdpTool
{
    public string Name => "sync_subtitle";

    public string Description =>
        "Navigate to this book's KDP 'Details' step and reconcile its Subtitle field against " +
        "the given expected value: read what's currently there, and if it doesn't match " +
        "(after trimming whitespace), set it to the expected value. Does NOT click Save and " +
        "Continue itself — after calling this, call check_checkbox then click_button with " +
        "[\"save and continue\"] to advance, same as every other step-change in this flow, " +
        "then proceed with the rest of the manuscript flow as normal (you'll land back on the " +
        "Content step). Call this once, right after find_and_open_book, before step 2. Returns " +
        "{found:false} if the Subtitle field couldn't be located on the page — do not treat " +
        "this as a fatal error, log_note it and continue with the rest of the flow (manuscript " +
        "replace is more important than the subtitle correction). Returns " +
        "{found:true, changed, before, after} otherwise — changed:false means it already matched.";

    public string ParametersJsonSchema => """
    {
      "type": "object",
      "properties": {
        "expected_subtitle": {
          "type": "string",
          "description": "The subtitle this book should have, e.g. 'A GLMZ Novella'. Pass exactly what you were given — do not paraphrase or reformat it."
        }
      },
      "required": ["expected_subtitle"]
    }
    """;

    public async Task<string> InvokeAsync(JsonElement args, KdpOperatorContext ctx, CancellationToken ct)
    {
        var expected = args.GetProperty("expected_subtitle").GetString() ?? "";
        if (expected.Length == 0)
            return JsonSerializer.Serialize(new { error = "expected_subtitle was empty." });

        var titleId = ExtractTitleId(ctx.Browser.CurrentUrl);
        if (titleId == null)
            return JsonSerializer.Serialize(new { error = "Could not extract titleId from the current URL — call this after find_and_open_book, not before." });

        try
        {
            await ctx.Browser.EvalAsync(
                $"window.location.href = 'https://kdp.amazon.com/en_US/title-setup/kindle/{titleId}/details'; ''", ct);
        }
        catch { /* navigation tears down the script context; a thrown eval is expected here */ }
        await Task.Delay(2500, ct);

        var expectedJs = JsonSerializer.Serialize(expected);
        var result = await ctx.Browser.EvalAsync(LocateAndSyncScript(expectedJs), ct);
        return result;
    }

    private static string? ExtractTitleId(string url)
    {
        var m = Regex.Match(url ?? "", @"/title-setup/kindle/([^/]+)/");
        return m.Success ? m.Groups[1].Value : null;
    }

    private static string LocateAndSyncScript(string expectedJsonLiteral) => $$"""
    (function() {
        // Same label-then-nearest-input strategy as the search-box locator in
        // FindAndOpenBookTool — KDP gives no stable id/class for form fields, so match on the
        // visible label text instead. "Subtitle" fields are commonly labelled "Subtitle" or
        // "Subtitle (optional)" — match the leading word only, not an exact string.
        var labelEls = Array.from(document.querySelectorAll('label, span, div, legend')).filter(function (el) {
            var t = (el.textContent || '').trim().toLowerCase();
            return t === 'subtitle' || t.indexOf('subtitle') === 0;
        });
        var input = null;
        for (var i = 0; i < labelEls.length && !input; i++) {
            var label = labelEls[i];
            if (label.htmlFor) {
                var byFor = document.getElementById(label.htmlFor);
                if (byFor && byFor.tagName === 'INPUT') input = byFor;
            }
            if (!input) {
                var container = label.closest('div, form') || label.parentElement;
                if (container) input = container.querySelector('input[type="text"], input:not([type])');
            }
        }
        if (!input) return JSON.stringify({ found: false });

        var before = input.value || '';
        var expected = {{expectedJsonLiteral}};

        if (before.trim() === expected.trim()) {
            return JSON.stringify({ found: true, changed: false, before: before, after: before });
        }

        // Same native-setter + dispatched-event technique already confirmed live on KDP's
        // search box (FindAndOpenBookTool) — required because a plain `input.value = x`
        // assignment doesn't notify a React-controlled input's own state.
        var setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
        setter.call(input, expected);
        input.dispatchEvent(new Event('input', { bubbles: true }));
        input.dispatchEvent(new Event('change', { bubbles: true }));
        input.blur();

        return JSON.stringify({ found: true, changed: true, before: before, after: input.value || '' });
    })()
    """;
}
