using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Prose.Core.Data.Entities;

/// <summary>
/// Durable persistence of what <see cref="Services.LastPromptStore"/> already captures —
/// the full, literal system+user prompt text and the response — for every LLM call, not just
/// the most recent 20. Separate from <see cref="LlmCallHistory"/> deliberately: that table is
/// a narrow, hot, append-on-every-call metadata table (provider/model/tokens/cost); a full
/// beat prompt runs tens of KB, and bloating the hot table with that would slow down every
/// cost-tracking query that scans it. Written best-effort from
/// <see cref="Services.LlmRouter"/>'s <c>RunWithFallbackAsync</c>, right alongside the
/// existing (in-memory-only) <c>LastPromptStore.Capture</c> call — a DB write failure here
/// must never break generation.
/// </summary>
[Index(nameof(BeatId))]
public class LlmPromptCapture
{
    public int Id { get; set; }

    /// <summary>FK -&gt; LlmCallHistory.Id, not enforced (best-effort sibling write; either
    /// row can fail to save independently).</summary>
    public int? LlmCallHistoryId { get; set; }

    /// <summary>Set when this call was made from a beat-write context — see
    /// <see cref="Services.LlmActionContext.CurrentBeatId"/>.</summary>
    public Guid? BeatId { get; set; }

    public DateTime At { get; set; } = DateTime.UtcNow;

    [MaxLength(32)]
    public string ProviderId { get; set; } = "";

    [MaxLength(128)]
    public string Model { get; set; } = "";

    /// <summary>stablePrefix + dynamicSystem, verbatim — the literal system prompt the LLM saw.</summary>
    public string System { get; set; } = "";

    public string User { get; set; } = "";

    public string? Response { get; set; }

    public int? ElapsedMs { get; set; }
}
