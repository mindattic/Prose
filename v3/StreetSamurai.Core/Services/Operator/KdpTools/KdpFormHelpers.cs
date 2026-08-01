using System.Text.Json;

namespace StreetSamurai.Core.Services.Operator.KdpTools;

/// <summary>
/// Shared checkbox-ticking logic used by both <see cref="CheckCheckboxTool"/> (explicit,
/// LLM-invoked) and <see cref="ClickButtonTool"/> (automatic, invoked unconditionally before
/// every Save/Continue/Publish click). Confirmed live across two separate runs that the LLM
/// agent successfully uploaded a manuscript but then skipped straight to clicking Save and
/// Continue without ever calling check_checkbox first — a real, observed failure mode of relying
/// on an LLM to correctly sequence two separate tool calls across an 11-step flow. Ticking these
/// automatically inside click_button removes the possibility of that step being skipped: ticking
/// an already-checked or nonexistent checkbox is a harmless no-op, so this is safe to run
/// unconditionally on every click_button call, not just ones the LLM thinks need it.
/// </summary>
internal static class KdpFormHelpers
{
    private const int MaxIterations = 10;

    /// <summary>Ticks every unchecked confirm-checkbox matching <paramref name="candidates"/>
    /// (KDP's real markup for these is a custom &lt;div role=checkbox&gt; widget, not a native
    /// input — see CheckCheckboxTool's remarks for the full story). Returns the matched label
    /// texts, one per checkbox actually ticked.</summary>
    public static async Task<List<string>> TickMatchingCheckboxesAsync(
        KdpOperatorContext ctx, string[] candidates, CancellationToken ct)
    {
        var candidatesJson = JsonSerializer.Serialize(candidates);
        var matches = new List<string>();

        // Process one match at a time rather than collecting all rects up front — clicking/
        // scrolling one element can shift page layout enough to invalidate other elements'
        // coordinates. Re-locating "the next unchecked match" after each click sidesteps that
        // entirely: if the previous click actually worked, that element no longer matches
        // "unchecked" and the loop naturally advances; if it didn't work, the loop would spin on
        // the same element forever, so it's capped at MaxIterations as a safety backstop.
        for (var iter = 0; iter < MaxIterations; iter++)
        {
            var locateResult = await ctx.Browser.EvalAsync(LocateNextScript(candidatesJson), ct);
            using var doc = JsonDocument.Parse(locateResult);
            var root = doc.RootElement;
            if (!root.GetProperty("found").GetBoolean()) break;

            var text = root.GetProperty("text").GetString() ?? "";
            var centerX = root.GetProperty("centerX").GetDouble();
            var centerY = root.GetProperty("centerY").GetDouble();

            await ctx.Browser.ClickAtPointAsync(centerX, centerY, ct);
            await Task.Delay(300, ct);
            matches.Add(text);
        }

        return matches;
    }

    private static string LocateNextScript(string candidatesJson) => $$"""
    (function() {
        var candidates = {{candidatesJson}}.map(function (c) { return c.toLowerCase(); });

        // A checkbox's visible label isn't reliably in one specific place — real-world forms
        // wrap it in <label>, or use a <label for=id> sibling, or just put plain text as a
        // sibling/cousin with no label element at all. Rather than guess one structure, walk
        // up several ancestor levels AND check immediate siblings, taking the first (nearest)
        // text blob that contains the candidate — nearer levels are strict subsets of farther
        // ones for text content, so this can't accidentally match the wrong checkbox.
        function candidateTexts(cb) {
            var texts = [];
            var label = cb.closest('label');
            if (label) texts.push(label.textContent);
            if (cb.id) {
                var forLabel = document.querySelector('label[for="' + cb.id + '"]');
                if (forLabel) texts.push(forLabel.textContent);
            }
            var node = cb;
            for (var depth = 0; depth < 5 && node.parentElement; depth++) {
                node = node.parentElement;
                texts.push(node.textContent);
            }
            if (cb.nextElementSibling) texts.push(cb.nextElementSibling.textContent);
            return texts.map(function (t) { return (t || '').trim().toLowerCase(); }).filter(Boolean);
        }

        function isChecked(el) {
            return el.tagName === 'INPUT' ? el.checked : el.getAttribute('aria-checked') === 'true';
        }

        // Match BOTH native <input type=checkbox> AND custom accessible <div role=checkbox>
        // widgets — KDP's real confirm control is the latter, not the former.
        var boxes = Array.from(document.querySelectorAll('input[type=checkbox], [role=checkbox]'));
        for (var i = 0; i < boxes.length; i++) {
            var cb = boxes[i];
            if (isChecked(cb)) continue;
            var texts = cb.tagName === 'INPUT' ? candidateTexts(cb) : [(cb.textContent || '').trim().toLowerCase()];
            for (var t = 0; t < texts.length; t++) {
                for (var j = 0; j < candidates.length; j++) {
                    if (texts[t].indexOf(candidates[j]) !== -1) {
                        cb.scrollIntoView({ block: 'center', inline: 'center' });
                        var rect = cb.getBoundingClientRect();
                        return JSON.stringify({
                            found: true,
                            text: texts[t].slice(0, 200),
                            centerX: rect.left + rect.width / 2,
                            centerY: rect.top + rect.height / 2
                        });
                    }
                }
            }
        }
        return JSON.stringify({ found: false });
    })()
    """;
}
