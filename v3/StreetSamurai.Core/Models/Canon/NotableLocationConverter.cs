using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace StreetSamurai.Core.Models.Canon;

/// <summary>
/// Reads <see cref="NotableLocation"/> from either shape:
/// new — object with name / description / tags;
/// legacy — bare string captured into Name (the description is a duplicate of name in legacy data).
/// Always writes the object form so the next save normalizes the row.
/// </summary>
public class NotableLocationConverter : JsonConverter<NotableLocation>
{
    public override NotableLocation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return new NotableLocation();

        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString() ?? "";
            return new NotableLocation { Name = s, Description = s };
        }

        var node = JsonNode.Parse(ref reader);
        if (node is not JsonObject obj) return new NotableLocation();

        return new NotableLocation
        {
            Name        = obj["name"]?.GetValue<string>() ?? "",
            Description = obj["description"]?.GetValue<string>() ?? "",
            Tags        = obj["tags"]?.Deserialize<List<string>>() ?? [],
        };
    }

    public override void Write(Utf8JsonWriter writer, NotableLocation value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("name", value.Name ?? "");
        writer.WriteString("description", value.Description ?? "");
        writer.WritePropertyName("tags");
        JsonSerializer.Serialize(writer, value.Tags ?? [], options);
        writer.WriteEndObject();
    }
}
