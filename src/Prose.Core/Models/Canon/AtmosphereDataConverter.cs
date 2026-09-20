using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Prose.Core.Models.Canon;

/// <summary>
/// Reads <see cref="AtmosphereData"/> from either shape:
/// new — object with sights / sounds / smells / feel / tags;
/// legacy — bare string captured into Feel.
/// Always writes the object form so the next save normalizes the row.
/// </summary>
public class AtmosphereDataConverter : JsonConverter<AtmosphereData>
{
    public override AtmosphereData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return new AtmosphereData();

        if (reader.TokenType == JsonTokenType.String)
            return new AtmosphereData { Feel = reader.GetString() ?? "" };

        var node = JsonNode.Parse(ref reader);
        if (node is not JsonObject obj) return new AtmosphereData();

        return new AtmosphereData
        {
            Sights = obj["sights"]?.Deserialize<List<string>>() ?? [],
            Sounds = obj["sounds"]?.Deserialize<List<string>>() ?? [],
            Smells = obj["smells"]?.Deserialize<List<string>>() ?? [],
            Feel   = obj["feel"]?.GetValue<string>() ?? "",
            Tags   = obj["tags"]?.Deserialize<List<string>>() ?? [],
        };
    }

    public override void Write(Utf8JsonWriter writer, AtmosphereData value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("sights");
        JsonSerializer.Serialize(writer, value.Sights ?? [], options);
        writer.WritePropertyName("sounds");
        JsonSerializer.Serialize(writer, value.Sounds ?? [], options);
        writer.WritePropertyName("smells");
        JsonSerializer.Serialize(writer, value.Smells ?? [], options);
        writer.WriteString("feel", value.Feel ?? "");
        writer.WritePropertyName("tags");
        JsonSerializer.Serialize(writer, value.Tags ?? [], options);
        writer.WriteEndObject();
    }
}
