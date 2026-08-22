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

    /// <summary>
    /// Ambient "which beat is currently generating" — sibling to <see cref="Current"/>, same
    /// AsyncLocal mechanism. Set once by <c>ProseWriterRouter.WriteAsync</c> right before
    /// calling <c>BeatGeneratorService.GenerateBeatAsync</c>; read by <see cref="LlmRouter"/>
    /// when writing a <see cref="Data.Entities.LlmPromptCapture"/> row, so the Beat Context
    /// Archive can find the exact prompt/response a given beat actually saw without widening
    /// <c>ILlmService.GenerateWithCachedPrefixAsync</c>'s signature (which every provider
    /// implementation would then need to accept and thread through for no other reason).
    /// Null for any call not made from a beat-write context (e.g. a review panel, a canon
    /// interpretation pass) — a graceful "no beat" rather than an error.
    /// </summary>
    private static readonly AsyncLocal<Guid?> currentBeatId = new();
    public static Guid? CurrentBeatId { get => currentBeatId.Value; set => currentBeatId.Value = value; }
}
