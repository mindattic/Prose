using System.Text.Json.Nodes;

namespace StreetSamurai.Core.Services.Operator;

/// <summary>
/// Collects every <see cref="IWriterTool"/> registered with DI into a single
/// addressable surface. The operator builds the Anthropic "tools" array from
/// this list every turn and routes incoming tool_use blocks back to the right
/// implementation by name.
/// </summary>
public class WriterToolRegistry
{
    private readonly Dictionary<string, IWriterTool> byName;

    public WriterToolRegistry(IEnumerable<IWriterTool> tools)
    {
        byName = tools.ToDictionary(t => t.Name, StringComparer.Ordinal);
    }

    public IReadOnlyCollection<IWriterTool> All => byName.Values;

    public IWriterTool? Get(string name) => byName.TryGetValue(name, out var t) ? t : null;

    /// <summary>
    /// Builds the tools[] array as the Anthropic Messages API expects:
    /// each entry has name, description, and an input_schema JSON object.
    /// </summary>
    public JsonArray BuildToolsArray()
    {
        var arr = new JsonArray();
        foreach (var t in byName.Values)
        {
            var schema = JsonNode.Parse(t.ParametersJsonSchema)
                ?? throw new InvalidOperationException(
                    $"Tool {t.Name}: ParametersJsonSchema is not valid JSON");
            arr.Add(new JsonObject
            {
                ["name"] = t.Name,
                ["description"] = t.Description,
                ["input_schema"] = schema,
            });
        }
        return arr;
    }
}
