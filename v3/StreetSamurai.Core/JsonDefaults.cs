using System.Text.Json;
using System.Text.Json.Serialization;

namespace StreetSamurai.Core;

/// <summary>
/// Centralized JSON serializer options. Use these instead of creating new instances.
/// </summary>
public static class JsonDefaults
{
    /// <summary>For reading LLM responses — case-insensitive property matching.</summary>
    public static JsonSerializerOptions LlmParsing { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>For writing data files — indented, readable JSON.</summary>
    public static JsonSerializerOptions Indented { get; } = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>For writing data files with snake_case naming (Claude API format).</summary>
    public static JsonSerializerOptions SnakeCase { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
