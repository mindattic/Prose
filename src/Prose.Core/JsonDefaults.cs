using System.Text.Json;
using System.Text.Json.Serialization;

namespace Prose.Core;

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

    /// <summary>
    /// Strip Markdown code fences from an LLM response so the inner JSON can be parsed.
    /// Handles both the common <c>```json\n…\n```</c> form and the single-line
    /// <c>```{"a":1}```</c> form. The previous inline pattern
    /// (<c>s[(s.IndexOf('\n') + 1)..]</c>) silently no-op'd when the opening fence
    /// had no trailing newline — <see cref="string.IndexOf(char)"/> returns -1, so
    /// the slice started at index 0 and left the ``` in place, breaking the parse.
    /// </summary>
    public static string StripCodeFences(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var t = s.Trim();
        if (t.StartsWith("```"))
        {
            var nl = t.IndexOf('\n');
            t = nl >= 0 ? t[(nl + 1)..] : t[3..]; // drop the ```lang line, or just the ``` when single-line
        }
        if (t.EndsWith("```")) t = t[..^3];
        return t.Trim();
    }
}
