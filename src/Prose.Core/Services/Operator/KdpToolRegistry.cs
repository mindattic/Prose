using System.Text.Json.Nodes;

namespace Prose.Core.Services.Operator;

/// <summary>
/// Collects every <see cref="IKdpTool"/> registered with DI into a single addressable surface.
/// Modelled on the now-deleted <c>WriterToolRegistry</c>: tools are looked up by their unique <see cref="IKdpTool.Name"/>.
/// </summary>
public class KdpToolRegistry
{
    private readonly Dictionary<string, IKdpTool> byName;

    public KdpToolRegistry(IEnumerable<IKdpTool> tools)
    {
        byName = tools.ToDictionary(t => t.Name, StringComparer.Ordinal);
    }

    public IReadOnlyCollection<IKdpTool> All => byName.Values;

    public IKdpTool? Get(string name) => byName.TryGetValue(name, out var t) ? t : null;

    /// <summary>Provider-neutral tool catalog for <see cref="IToolCallingLlm"/> implementations —
    /// each vendor adapter re-nests the same JSON Schema under its own wire envelope.</summary>
    public IReadOnlyList<ToolDefinition> BuildToolDefinitions() =>
        byName.Values.Select(t => new ToolDefinition(
            t.Name,
            t.Description,
            JsonNode.Parse(t.ParametersJsonSchema)
                ?? throw new InvalidOperationException($"Tool {t.Name}: ParametersJsonSchema is not valid JSON")))
            .ToList();
}
