namespace StreetSamurai.Core.Services;

/// <summary>
/// The dotted "node-guid.beat-guid" handle the writer UI shows on the
/// LLM bottom sheet, surfaced over the MCP layer for chat-side authoring.
/// Plain Guid (beat-only) is also accepted — the node id is recovered
/// from the BeatNode junction at call time.
///
/// Single source of truth so the CLI / MCP tools and the unit-test layer
/// share one parser. Keep the handle format change here if it ever moves.
/// </summary>
public static class BeatHandle
{
    /// <summary>Parse a beat handle. Returns true on success.</summary>
    /// <param name="handle">Either a Beat Guid (e.g. <c>019e4d4a-…</c>) or
    /// the dotted form <c>node-guid.beat-guid</c>.</param>
    /// <param name="nodeId">Set to the node portion when the dotted
    /// form was supplied, otherwise <c>null</c>.</param>
    /// <param name="beatId">Set to the beat portion. <c>null</c> when the
    /// handle didn't parse.</param>
    public static bool TryParse(string? handle, out Guid? nodeId, out Guid? beatId)
    {
        nodeId = null;
        beatId = null;
        if (string.IsNullOrWhiteSpace(handle)) return false;
        var trimmed = handle.Trim();

        // Dotted form: node-guid.beat-guid. Use IndexOf so the lookup
        // is allocation-free (no Split). Reject inputs where the dot lands
        // at either end — "a." and ".a" are malformed.
        var dot = trimmed.IndexOf('.');
        if (dot > 0 && dot < trimmed.Length - 1)
        {
            var sPart = trimmed[..dot];
            var bPart = trimmed[(dot + 1)..];
            if (Guid.TryParse(sPart, out var sg) && Guid.TryParse(bPart, out var bg))
            {
                nodeId = sg;
                beatId = bg;
                return true;
            }
            return false; // malformed dotted form
        }

        // Beat-only form: a single Guid.
        if (Guid.TryParse(trimmed, out var only))
        {
            beatId = only;
            return true;
        }
        return false;
    }

    /// <summary>Render the dotted handle "node-guid.beat-guid". The
    /// writer UI shows this verbatim so the user can copy-paste it into
    /// a CLI call or a chat conversation with Claude.</summary>
    public static string Format(Guid nodeId, Guid beatId) => $"{nodeId}.{beatId}";
}
