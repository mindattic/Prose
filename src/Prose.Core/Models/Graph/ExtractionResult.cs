using System.Text.Json;
using System.Text.Json.Serialization;

namespace Prose.Core.Models.Graph;

/// <summary>
/// Result of LLM entity extraction from story text.
/// Designed to map directly to UniverseNode + UniverseEdge creation.
/// </summary>
public record ExtractionResult
{
    [JsonPropertyName("entities")]
    public List<ExtractedEntity> Entities { get; init; } = [];

    [JsonPropertyName("relationships")]
    public List<ExtractedRelationship> Relationships { get; init; } = [];
}

public record ExtractedEntity
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("type")]
    public string Type { get; init; } = "";

    [JsonPropertyName("description")]
    public string Description { get; init; } = "";

    /// <summary>Read leniently: models write <c>"age": 34</c> and <c>"alive": true</c> as often as
    /// strings, and a strict string dictionary threw on the first one — the whole extraction came
    /// back empty. A null becomes an empty dictionary.</summary>
    [JsonPropertyName("properties")]
    [JsonConverter(typeof(LenientStringDictionaryConverter))]
    public Dictionary<string, string> Properties { get; init; } = new();
}

/// <summary>A string→string dictionary that accepts any JSON scalar or structure as a value
/// (stored as its raw JSON text, or the plain string) and treats null as empty.</summary>
public sealed class LenientStringDictionaryConverter : JsonConverter<Dictionary<string, string>>
{
    public override bool HandleNull => true;

    public override Dictionary<string, string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var result = new Dictionary<string, string>();
        if (reader.TokenType == JsonTokenType.Null) return result;
        using var doc = JsonDocument.ParseValue(ref reader);
        if (doc.RootElement.ValueKind != JsonValueKind.Object) return result;
        foreach (var p in doc.RootElement.EnumerateObject())
            result[p.Name] = p.Value.ValueKind switch
            {
                JsonValueKind.String => p.Value.GetString() ?? "",
                JsonValueKind.Null   => "",
                _                    => p.Value.GetRawText(),
            };
        return result;
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<string, string> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var (k, v) in value) writer.WriteString(k, v);
        writer.WriteEndObject();
    }
}

public record ExtractedRelationship
{
    [JsonPropertyName("source")]
    public string Source { get; init; } = "";

    [JsonPropertyName("target")]
    public string Target { get; init; } = "";

    [JsonPropertyName("type")]
    public string Type { get; init; } = "";

    [JsonPropertyName("description")]
    public string Description { get; init; } = "";

    [JsonPropertyName("sentiment")]
    public string Sentiment { get; init; } = "neutral";
}
