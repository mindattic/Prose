using System.Text.Json;

namespace Prose.Core.Services.Operator.KdpTools;

/// <summary>
/// Finds and clicks the first button/link/input whose visible text contains any of the given
/// candidate substrings — KDP's exact button wording drifts across redesigns ("Save and
/// Continue", "Publish Your Book", modal "OK"), so matching by text is more resilient than a
/// guessed CSS selector the LLM has no way to verify in advance.
/// </summary>
public class ClickButtonTool : IKdpTool
{
    public string Name => "click_button";

    public string Description =>
        "Find and click the first clickable element (button, link, or submit input) whose " +
        "visible text contains any of the given candidate phrases (case-insensitive). Pass " +
        "several plausible variants of the label you're looking for (e.g. [\"save and " +
        "publish\", \"publish your book\"]) since exact wording can drift. Returns " +
        "{clicked:true, matchedText} or {clicked:false} if nothing matched.";

    public string ParametersJsonSchema => """
    {
      "type": "object",
      "properties": {
        "text_candidates": {
          "type": "array",
          "items": { "type": "string" },
          "description": "Candidate substrings to match against visible text, e.g. [\"save and continue\"]."
        }
      },
      "required": ["text_candidates"]
    }
    """;

    // KDP's confirm-checkbox candidates, ticked unconditionally before every click_button call
    // (see KdpFormHelpers' remarks) — confirmed live across two separate runs (MxG, NxR) that the
    // LLM agent uploaded a manuscript, then skipped straight to click_button without ever calling
    // check_checkbox, causing Save and Continue to silently no-op and the agent to abandon the
    // book. Ticking here removes the dependency on the LLM remembering a separate step.
    private static readonly string[] ConfirmCheckboxCandidates = { "confirm that my answers are accurate" };

    public async Task<string> InvokeAsync(JsonElement args, KdpOperatorContext ctx, CancellationToken ct)
    {
        var candidates = args.GetProperty("text_candidates")
            .EnumerateArray().Select(x => x.GetString() ?? "").Where(s => s.Length > 0).ToArray();
        if (candidates.Length == 0)
            return JsonSerializer.Serialize(new { error = "text_candidates was empty." });

        var tickResult = await KdpFormHelpers.TickMatchingCheckboxesAsync(ctx, ConfirmCheckboxCandidates, ct);
        if (tickResult.BlockedByProcessing)
        {
            // Refuse the click entirely, not just the auto-tick — a Save/Continue/Publish click
            // while KDP is still quality-checking/converting the upload depends on checkboxes we
            // just confirmed we can't reliably tick yet, so clicking now would race the same
            // unreliable-form state this guard exists to avoid.
            return JsonSerializer.Serialize(new
            {
                clicked = false,
                blockedByProcessing = true,
                processingIndicator = tickResult.ProcessingIndicator,
                hint = "KDP is still running its server-side quality check/processing. Call get_page_status and wait for it to clear before retrying.",
            });
        }
        var autoTicked = tickResult.Matches;

        var candidatesJson = JsonSerializer.Serialize(candidates);
        var script = $$"""
        (function() {
            var candidates = {{candidatesJson}}.map(function (c) { return c.toLowerCase(); });

            function textOf(el) {
                // Icon-only buttons (e.g. a modal's "X" close button) commonly have NO visible
                // text at all — just an <i> icon child rendered via CSS — so textContent is
                // empty and this element would never match "close"/"done" without falling back
                // to aria-label/title, which is where their accessible name actually lives.
                // Confirmed live: KDP's post-publish success modal's close button is exactly
                // this shape: <button aria-label="Close"><i class="a-icon-close"></i></button>.
                var direct = el.tagName === 'INPUT' ? (el.value || '') : (el.textContent || '');
                direct = direct.trim();
                if (direct) return direct.toLowerCase();
                var fallback = el.getAttribute('aria-label') || el.getAttribute('title') || '';
                return fallback.trim().toLowerCase();
            }
            function inFooterOrNav(el) {
                return !!el.closest('footer, nav, [class*="footer" i], [id*="footer" i], [class*="nav" i], [id*="nav" i]');
            }
            function tryMatch(elements) {
                for (var i = 0; i < elements.length; i++) {
                    var el = elements[i];
                    var text = textOf(el);
                    // A real action button's label is short — a long multi-sentence match (like a
                    // footer blurb that happens to contain the word "publish") is never the real
                    // target, no matter what tag it's on.
                    if (!text || text.length > 60) continue;
                    for (var j = 0; j < candidates.length; j++) {
                        if (text.indexOf(candidates[j]) !== -1) return { el: el, text: text };
                    }
                }
                return null;
            }

            // Pass 1: real form-action elements only (never <a> here) — this is what a KDP
            // "Save and Continue" / "Publish Your Book" control actually is. Confirmed necessary
            // live: a footer <a> reading "CreateSpace: Indie print publishing made easy" matched
            // a bare "publish" candidate before any real button was tried, navigating the whole
            // browser to an unrelated CreateSpace account-transfer page.
            var found = tryMatch(document.querySelectorAll('button, input[type=submit], input[type=button]'));
            // Pass 2: links, but never ones living inside a footer/nav region.
            if (!found) {
                var links = Array.from(document.querySelectorAll('a')).filter(function (a) { return !inFooterOrNav(a); });
                found = tryMatch(links);
            }

            if (found) {
                found.el.click();
                return JSON.stringify({ clicked: true, matchedText: found.text });
            }
            return JSON.stringify({ clicked: false });
        })()
        """;

        var result = await ctx.Browser.EvalAsync(script, ct);

        // "Save and Continue" / "Publish" trigger a client-side page transition — confirmed live
        // that a get_page_status call immediately after this returns stale/pre-transition content
        // (or an empty banner set), which the agent has then narrated into a fabricated blocker
        // explanation instead of correctly reporting "nothing conclusive yet." A short settle
        // delay after a real click gives the SPA a moment to start rendering the next state before
        // the next tool call reads it — cheap insurance against that exact failure mode.
        if (result.Contains("\"clicked\":true"))
            await Task.Delay(1500, ct);

        if (autoTicked.Count == 0)
            return result;

        // Splice autoTickedCheckboxes into the returned JSON so the agent (and the log) can see
        // this happened without needing a separate check_checkbox call to surface it.
        using var doc = JsonDocument.Parse(result);
        var merged = new Dictionary<string, object?>();
        foreach (var prop in doc.RootElement.EnumerateObject())
            merged[prop.Name] = JsonSerializer.Deserialize<object?>(prop.Value.GetRawText());
        merged["autoTickedCheckboxes"] = autoTicked;
        return JsonSerializer.Serialize(merged);
    }
}
