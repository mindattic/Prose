using System.Text.Json;

namespace Prose.Core.Services.Operator.KdpTools;

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

    // Delegates to KdpFormHelpers (the same helper ClickButtonTool's auto-tick uses) instead of
    // keeping its own copy of the locate/click script — a second copy is exactly how this tool
    // used to be able to tick checkboxes without ever checking KDP's processing/quality-check
    // banner first (fixed 2026-08-01 by moving the guard into the shared helper, see its remarks).
    public async Task<string> InvokeAsync(JsonElement args, KdpOperatorContext ctx, CancellationToken ct)
    {
        var candidates = args.GetProperty("text_candidates")
            .EnumerateArray().Select(x => x.GetString() ?? "").Where(s => s.Length > 0).ToArray();
        if (candidates.Length == 0)
            return JsonSerializer.Serialize(new { error = "text_candidates was empty." });

        var result = await KdpFormHelpers.TickMatchingCheckboxesAsync(ctx, candidates, ct);
        if (result.BlockedByProcessing)
        {
            return JsonSerializer.Serialize(new
            {
                checkedCount = 0,
                blockedByProcessing = true,
                processingIndicator = result.ProcessingIndicator,
                hint = "KDP is still running its server-side quality check/processing — checkboxes are unreliable until this clears. Call get_page_status and wait, then retry.",
            });
        }

        return JsonSerializer.Serialize(new { checkedCount = result.Matches.Count, matches = result.Matches });
    }
}
