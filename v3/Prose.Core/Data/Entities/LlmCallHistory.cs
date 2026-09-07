using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Prose.Core.Data.Entities;

/// <summary>
/// Append-only, per-call audit trail of every LLM provider attempt made through
/// <see cref="Services.LlmRouter"/> — one row per provider tried, success or failure,
/// including fallback hops. Answers "which model performed which action" durably
/// (unlike <see cref="TokenLedger"/>, which is in-memory/per-process) and at finer
/// granularity than <see cref="CommandCostHistory"/> (per LLM call, not per whole CLI
/// command). Written best-effort — a failure to log must never break generation itself.
/// </summary>
[Index(nameof(BeatId))]
public class LlmCallHistory
{
    public int Id { get; set; }
    public DateTime At { get; set; } = DateTime.UtcNow;
    [MaxLength(32)]
    public string ProviderId { get; set; } = "";        // e.g. "claude-team", "codex-cli", "gemini"
    [MaxLength(128)]
    public string Model { get; set; } = "";              // resolved model id, or "(provider default)"
    [MaxLength(256)]
    public string Action { get; set; } = "(unspecified)"; // calling CLI command/action tag, see LlmActionContext
    public bool Success { get; set; }
    /// <summary>How many earlier providers in the fallback chain failed before this attempt (0 = first try).</summary>
    public int FallbackHopIndex { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public double Cost { get; set; }
    [MaxLength(512)]
    public string? ErrorMessage { get; set; }

    /// <summary>Which pipeline stage made this call — see
    /// <see cref="Services.LlmActionContext.CurrentStage"/>. Null = not made inside a traced
    /// stage (the trace report lists such calls as UNATTRIBUTED rather than dropping them).
    /// Added 2026-09-07 for the single-source-writer measurement.</summary>
    [MaxLength(128)]
    public string? Stage { get; set; }

    /// <summary>The beat this call was made on behalf of — see
    /// <see cref="Services.LlmActionContext.CurrentBeatId"/>. Previously only
    /// <see cref="LlmPromptCapture"/> carried this, which meant answering "how many calls did
    /// beat X make" required scanning the prompt-text table. Indexed.</summary>
    public Guid? BeatId { get; set; }

    /// <summary>Wall time of this provider hop, ms. Sibling of the same figure on
    /// <see cref="LlmPromptCapture"/> so the hot table can be summed without a join.</summary>
    public int? ElapsedMs { get; set; }
}
