using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace StreetSamurai.Core.Models.Canon;

/// <summary>
/// Reads a string property tolerantly:
/// • String token → returned as-is
/// • Number token → ToString
/// • Object token → summarised in priority order: total_installed, summary,
///   part_only, then a flat join of remaining values
/// • Array token → semicolon-joined elements
/// Always writes a plain string. Used for legacy fields like Material.cost
/// where some records hold a structured object instead of the modeled string.
/// </summary>
public class TolerantStringConverter : JsonConverter<string>
{
    private static readonly string[] preferredKeys =
        ["total_installed", "summary", "part_only", "value", "amount", "price"];

    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return "";
            case JsonTokenType.String:
                return reader.GetString() ?? "";
            case JsonTokenType.Number:
                return reader.TryGetInt64(out var l) ? l.ToString() : reader.GetDouble().ToString();
            case JsonTokenType.True:
            case JsonTokenType.False:
                return reader.GetBoolean().ToString();
            case JsonTokenType.StartArray:
            {
                var arr = JsonNode.Parse(ref reader) as JsonArray;
                if (arr == null) return "";
                return string.Join("; ", arr.Select(n => n?.ToString() ?? ""));
            }
            case JsonTokenType.StartObject:
            {
                var node = JsonNode.Parse(ref reader);
                if (node is not JsonObject obj) return "";
                foreach (var key in preferredKeys)
                    if (obj[key] is JsonNode preferred && !string.IsNullOrWhiteSpace(preferred.ToString()))
                        return preferred.ToString();
                return string.Join("; ", obj.Where(kv => kv.Value != null).Select(kv => $"{kv.Key}: {kv.Value}"));
            }
            default:
                return "";
        }
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        => writer.WriteStringValue(value ?? "");
}
