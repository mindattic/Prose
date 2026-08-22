using System.ComponentModel.DataAnnotations;

namespace Prose.Core.Data.Entities;

/// <summary>
/// Append-only, best-effort audit trail of every command Prose.Hub ever executes —
/// every CLI command (via <see cref="Services.IUniverseContext"/>-scoped dispatch,
/// see Prose.Hub's CliDispatch.ExecuteCoreAsync) and every MCP tool call (see
/// Prose.Hub's ToolDispatch.InvokeAsync), cost-gated or not. This is the durable
/// answer to "what did the Hub actually do" — nothing depends on a conversation's
/// memory to reconstruct it. Modeled on <see cref="LlmCallHistory"/>'s posture:
/// a logging failure must never break the command it's logging.
/// </summary>
public class CommandLedgerEntry
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime At { get; set; } = DateTime.UtcNow;

    [MaxLength(16)]
    public string Source { get; set; } = ""; // "cli" | "mcp" | "cost-gate"

    [MaxLength(128)]
    public string HandlerClass { get; set; } = "";

    [MaxLength(64)]
    public string? Method { get; set; }

    /// <summary>The forwarded args/params as JSON — the CLI's string[] args array, or the
    /// MCP tool's deserialized parameter object.</summary>
    public string ArgsJson { get; set; } = "[]";

    [MaxLength(32)]
    public string? Universe { get; set; }

    public int? ExitCode { get; set; }
    public bool Success { get; set; }
    public double DurationMs { get; set; }

    /// <summary>First ~500 chars of captured output, not the full text — keeps rows small
    /// while still letting a human/LLM sanity-check what happened without re-running it.</summary>
    [MaxLength(512)]
    public string? OutputSummary { get; set; }

    [MaxLength(1024)]
    public string? ErrorMessage { get; set; }

    /// <summary>Who/what invoked this — "claude-code", "human-cli", a session id, etc.
    /// Best-effort; not every caller threads this through yet.</summary>
    [MaxLength(128)]
    public string? Actor { get; set; }
}
