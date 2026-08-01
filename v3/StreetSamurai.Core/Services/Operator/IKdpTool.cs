using System.Text.Json;

namespace StreetSamurai.Core.Services.Operator;

/// <summary>
/// One callable surface the KDP-publish operator LLM can invoke. Mirrors <see cref="IWriterTool"/>'s
/// shape exactly — same registry/loop pattern, different tool surface (drives a live KDP browser
/// page instead of the prose-generation services).
/// </summary>
public interface IKdpTool
{
    string Name { get; }
    string Description { get; }
    string ParametersJsonSchema { get; }

    Task<string> InvokeAsync(JsonElement args, KdpOperatorContext context, CancellationToken ct);
}

/// <summary>Per-turn context handed to every KDP tool: the live browser surface to act on.</summary>
public sealed class KdpOperatorContext
{
    public required IKdpBrowser Browser { get; init; }
}
