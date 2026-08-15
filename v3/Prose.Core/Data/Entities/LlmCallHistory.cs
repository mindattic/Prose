using System.ComponentModel.DataAnnotations;

namespace Prose.Core.Data.Entities;

/// <summary>
/// Append-only, per-call audit trail of every LLM provider attempt made through
/// <see cref="Services.LlmRouter"/> — one row per provider tried, success or failure,
/// including fallback hops. Answers "which model performed which action" durably
/// (unlike <see cref="TokenLedger"/>, which is in-memory/per-process) and at finer
/// granularity than <see cref="CommandCostHistory"/> (per LLM call, not per whole CLI
/// command). Written best-effort — a failure to log must never break generation itself.
/// </summary>
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
}
