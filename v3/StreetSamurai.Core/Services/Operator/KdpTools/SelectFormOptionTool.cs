using System.Text.Json;

namespace StreetSamurai.Core.Services.Operator.KdpTools;

/// <summary>
/// Selects ONE radio/checkbox-shaped option by its visible text — the new-listing flow's
/// equivalent of <see cref="CheckCheckboxTool"/>, generalized two ways that tool deliberately
/// isn't: (1) it selects a single match instead of ticking every match (correct for a radio
/// group, where only one option should ever be active), and (2) it handles BOTH shapes confirmed
/// live on the Details/Content pages — plain native <c>&lt;input type=radio/checkbox&gt;</c>
/// (rights, adult-content, DRM, assetless-preorder-choice, accessibility — a synthetic .click()
/// genuinely works here, unlike the republish flow's confirm-checkbox) AND custom
/// <c>[role=radio]</c>/<c>[role=checkbox]</c> widgets (confirmed live on the AI-generated-content
/// question: synthetic .click() leaves aria-checked unchanged, same as the republish flow's
/// confirm-checkbox widget) — those require a real CDP-dispatched mouse click via
/// <see cref="IKdpBrowser.ClickAtPointAsync"/>.
/// </summary>
public class SelectFormOptionTool : IKdpTool
{
    public string Name => "select_form_option";

    public string Description =>
        "Select ONE radio button or checkbox on the current page whose visible label text " +
        "contains any of the given candidate phrases (case-insensitive) — e.g. [\"I own the " +
        "copyright\"], [\"No, do not apply Digital Rights Management\"], [\"Yes\"] (for the " +
        "AI-generated-content question). Picks the first unselected match; already-selected " +
        "matches are left alone. Works for both ordinary form controls and KDP's custom " +
        "role=radio/role=checkbox widgets — you don't need to know which kind it is. Returns " +
        "{selected:true, matchedText} or {selected:false} if nothing matched (or everything " +
        "matching was already selected — check the page if that's unexpected).";

    public string ParametersJsonSchema => """
    {
      "type": "object",
      "properties": {
        "text_candidates": {
          "type": "array",
          "items": { "type": "string" },
          "description": "Candidate substrings to match against the option's label/text."
        }
      },
      "required": ["text_candidates"]
    }
    """;

    public async Task<string> InvokeAsync(JsonElement args, KdpOperatorContext ctx, CancellationToken ct)
    {
        var candidates = args.GetProperty("text_candidates")
            .EnumerateArray().Select(x => x.GetString() ?? "").Where(s => s.Length > 0).ToArray();
        if (candidates.Length == 0)
            return JsonSerializer.Serialize(new { error = "text_candidates was empty." });
        var candidatesJson = JsonSerializer.Serialize(candidates);

        // Single combined pass across BOTH native inputs and custom role=radio/checkbox widgets,
        // ranked exact-match-first (see FindBestMatchScript's own remarks for why: a short bare
        // candidate like "Yes" must prefer an exact-text widget hit over a substring hit inside an
        // unrelated long native sentence like "Yes, apply Digital Rights Management" — confirmed
        // live as a real bug when both exist on the same page). If the winning element is native,
        // the script clicks it directly (confirmed a plain .click() works for these). If it's a
        // custom widget, the script only reports its center point — the actual click must be a
        // real CDP-dispatched mouse event (synthetic clicks don't toggle aria-checked).
        var findResult = await ctx.Browser.EvalAsync(FindBestMatchScript(candidatesJson), ct);
        using var doc = JsonDocument.Parse(findResult);
        var root = doc.RootElement;
        if (!root.GetProperty("found").GetBoolean())
            return JsonSerializer.Serialize(new { selected = false });

        var kind = root.GetProperty("kind").GetString();
        var text = root.GetProperty("text").GetString();

        if (kind == "native")
            return JsonSerializer.Serialize(new { selected = root.GetProperty("clicked").GetBoolean(), matchedText = text, kind });

        var centerX = root.GetProperty("centerX").GetDouble();
        var centerY = root.GetProperty("centerY").GetDouble();
        await ctx.Browser.ClickAtPointAsync(centerX, centerY, ct);
        await Task.Delay(300, ct);
        return JsonSerializer.Serialize(new { selected = true, matchedText = text, kind = "widget" });
    }

    // Ranked, single-pass matching across native AND widget elements together:
    //   Tier 1 — EXACT text match (trimmed, case-insensitive), native then widget.
    //   Tier 2 — substring match using TIGHT text association (closest label, label[for],
    //            immediate parent, immediate next sibling — never a multi-level ancestor walk).
    //   Tier 3 — substring match using a WIDER ancestor walk (native only; last resort).
    // Exact-first is the critical fix: a short bare candidate ("Yes") must prefer an EXACT hit on
    // a bare-text widget ("Yes") over a SUBSTRING hit inside a longer, unrelated native sentence
    // ("Yes, apply Digital Rights Management") that happens to contain the word too — confirmed
    // live as a real bug when a native Yes/No pair and a widget Yes/No pair coexist on one page
    // (DRM's native radios vs. the AI-generated-content question's custom widget radios).
    // Tight-first (within a tier) is the earlier-fixed bug: a radio GROUP's siblings commonly
    // share one enclosing container, so walking several ancestor levels up merges their text
    // together and a generic word from one sibling's label can match its neighbor instead.
    private static string FindBestMatchScript(string candidatesJson) => $$"""
    (function() {
        var candidates = {{candidatesJson}}.map(function (c) { return c.toLowerCase().trim(); });

        function tightTexts(input) {
            var texts = [];
            var lbl = input.closest('label');
            if (lbl) texts.push(lbl.textContent);
            if (input.id) {
                var forLbl = document.querySelector('label[for="' + input.id + '"]');
                if (forLbl) texts.push(forLbl.textContent);
            }
            if (input.parentElement) texts.push(input.parentElement.textContent);
            if (input.nextElementSibling) texts.push(input.nextElementSibling.textContent);
            return texts.map(function (t) { return (t || '').trim().toLowerCase(); }).filter(Boolean);
        }

        function wideTexts(input) {
            var texts = tightTexts(input);
            var node = input;
            for (var d = 0; d < 5 && node.parentElement; d++) {
                node = node.parentElement;
                texts.push((node.textContent || '').trim().toLowerCase());
            }
            return texts.filter(Boolean);
        }

        var nativeInputs = Array.from(document.querySelectorAll('input[type=radio], input[type=checkbox]'))
            .filter(function (el) { return !el.checked; });
        var widgets = Array.from(document.querySelectorAll('[role=radio], [role=checkbox]'))
            .filter(function (el) { return el.getAttribute('aria-checked') !== 'true'; });

        function widgetResult(el, text) {
            el.scrollIntoView({ block: 'center', inline: 'center' });
            var rect = el.getBoundingClientRect();
            return { found: true, kind: 'widget', text: text.slice(0, 200), centerX: rect.left + rect.width / 2, centerY: rect.top + rect.height / 2 };
        }
        function nativeResult(el, text) {
            el.click();
            return { found: true, kind: 'native', clicked: true, text: text.slice(0, 200) };
        }

        // Tier 1: exact match, native first then widget.
        for (var i = 0; i < nativeInputs.length; i++) {
            var texts = tightTexts(nativeInputs[i]);
            for (var t = 0; t < texts.length; t++)
                if (candidates.indexOf(texts[t]) !== -1) return JSON.stringify(nativeResult(nativeInputs[i], texts[t]));
        }
        for (var i = 0; i < widgets.length; i++) {
            var text = (widgets[i].textContent || '').trim().toLowerCase();
            if (candidates.indexOf(text) !== -1) return JSON.stringify(widgetResult(widgets[i], text));
        }

        // Tier 2: substring match, tight association only, native first then widget.
        for (var i = 0; i < nativeInputs.length; i++) {
            var texts = tightTexts(nativeInputs[i]);
            for (var t = 0; t < texts.length; t++)
                for (var c = 0; c < candidates.length; c++)
                    if (texts[t].indexOf(candidates[c]) !== -1) return JSON.stringify(nativeResult(nativeInputs[i], texts[t]));
        }
        for (var i = 0; i < widgets.length; i++) {
            var text = (widgets[i].textContent || '').trim().toLowerCase();
            for (var c = 0; c < candidates.length; c++)
                if (text.indexOf(candidates[c]) !== -1) return JSON.stringify(widgetResult(widgets[i], text));
        }

        // Tier 3: substring match, wide ancestor walk — native only, last resort.
        for (var i = 0; i < nativeInputs.length; i++) {
            var texts = wideTexts(nativeInputs[i]);
            for (var t = 0; t < texts.length; t++)
                for (var c = 0; c < candidates.length; c++)
                    if (texts[t].indexOf(candidates[c]) !== -1) return JSON.stringify(nativeResult(nativeInputs[i], texts[t]));
        }

        return JSON.stringify({ found: false });
    })()
    """;
}
