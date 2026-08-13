namespace Prose.Core.Services.Operator;

/// <summary>
/// Streaming event contract for an Anthropic tool-use loop. Shared shape between
/// <see cref="KdpOperatorService"/>'s republish/new-listing loop and, historically,
/// the now-deleted WriterOperatorService — kept here as its own file since
/// KdpOperatorService is a real, independent, live consumer.
/// </summary>
public abstract record OperatorEvent
{
    public sealed record AssistantText(string Text) : OperatorEvent;
    public sealed record ToolStarted(string Name, string ArgsJson) : OperatorEvent;
    public sealed record ToolCompleted(string Name, string ResultJson, bool IsError) : OperatorEvent;
    public sealed record Error(string Message) : OperatorEvent;

    /// <summary>A non-error status line — e.g. a hard-gate check that correctly skipped a book
    /// (nothing newer to publish) rather than something that went wrong.</summary>
    public sealed record Info(string Message) : OperatorEvent;
}
