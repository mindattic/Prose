using System.Text.Json;

namespace StreetSamurai.Core.Services.Operator.KdpTools;

/// <summary>
/// Finds and checks (ticks) every unchecked checkbox whose associated text contains any of the
/// given candidate phrases. KDP requires an explicit "I confirm my answers are accurate"
/// acknowledgment whenever a new manuscript/cover was just uploaded — Save and Continue silently
/// re-shows the same page with a "Please fix the highlighted error(s)" banner until every one of
/// these is ticked, which looks identical to (and was originally misdiagnosed several times over
/// as) an unrelated account-state problem or a missing form control.
///
/// Confirmed live (via a direct DOM dump, not the agent's own report) that KDP's real markup for
/// this control is NOT a native &lt;input type=checkbox&gt; at all — it's a custom accessible
/// widget: &lt;div role="checkbox" aria-checked="false" tabindex="0"&gt;By clicking this, I
/// confirm that my answers are accurate&lt;/div&gt;. Every prior version of this tool queried only
/// input[type=checkbox], which structurally cannot match that element — it wasn't finding zero
/// matches because the checkbox was already ticked or absent, it was finding zero matches because
/// it was looking for the wrong element type entirely. This version matches both shapes.
///
/// Also confirmed live: calling .click() on that div from JS does NOT flip aria-checked — the
/// widget's React handler appears to ignore synthetic/untrusted click events. Re-diagnosing the
/// live page immediately after a run that believed it had ticked the box showed aria-checked
/// still false. The fix is to locate the element and its on-screen position via JS, then dispatch
/// a REAL mouse click through <see cref="IKdpBrowser.ClickAtPointAsync"/> (CDP
/// Input.dispatchMouseEvent) at its center — a trusted input event indistinguishable from an
/// actual user click.
/// </summary>
public class CheckCheckboxTool : IKdpTool
{
    public string Name => "check_checkbox";

    public string Description =>
        "Find and check (tick) EVERY unchecked checkbox whose label text contains any of " +
        "the given candidate phrases (case-insensitive), e.g. [\"confirm that my answers are " +
        "accurate\"]. KDP repeats the same confirmation checkbox after EACH section that had a " +
        "new upload (manuscript, cover, etc.) — a single page can have more than one identical " +
        "checkbox, and Save and Continue silently re-shows the same page with a 'Please fix " +
        "the highlighted error(s)' banner until ALL of them are ticked, not just one. This tool " +
        "ticks all matches in one call. Returns {checkedCount, matches:[matchedText,...]} — " +
        "checkedCount:0 means nothing matched or everything was already checked.";

    public string ParametersJsonSchema => """
    {
      "type": "object",
      "properties": {
        "text_candidates": {
          "type": "array",
          "items": { "type": "string" },
          "description": "Candidate substrings to match against the checkbox's label text."
        }
      },
      "required": ["text_candidates"]
    }
    """;

    private const int MaxIterations = 10;

    public async Task<string> InvokeAsync(JsonElement args, KdpOperatorContext ctx, CancellationToken ct)
    {
        var candidates = args.GetProperty("text_candidates")
            .EnumerateArray().Select(x => x.GetString() ?? "").Where(s => s.Length > 0).ToArray();
        if (candidates.Length == 0)
            return JsonSerializer.Serialize(new { error = "text_candidates was empty." });

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

        return JsonSerializer.Serialize(new { checkedCount = matches.Count, matches });
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
