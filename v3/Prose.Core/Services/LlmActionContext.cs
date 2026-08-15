namespace Prose.Core.Services;

/// <summary>
/// Ambient "which command/action is currently generating" tag. Prose.Cli's CostGateCli
/// sets this around a CLI command's lifetime; <see cref="LlmRouter"/> reads
/// it when writing <see cref="Data.Entities.LlmCallHistory"/> rows — so a query like
/// <c>SELECT * FROM LlmCallHistories WHERE Action = '--write-story'</c> shows which model handled
/// which action, per the Multi-LLM Master Switch-Over plan's audit-trail requirement.
/// AsyncLocal, not a plain static field, so concurrent hosts (Blazor, MCP) don't cross-talk.
/// Callers that never set it (MCP tools, Blazor UI actions, ad-hoc scripts) simply get an
/// "(unspecified)" action tag on their rows — a graceful degradation, not an error.
/// </summary>
public static class LlmActionContext
{
    private static readonly AsyncLocal<string?> current = new();
    public static string? Current { get => current.Value; set => current.Value = value; }
}
