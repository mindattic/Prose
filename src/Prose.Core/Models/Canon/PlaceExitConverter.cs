using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Prose.Core.Models.Canon;

/// <summary>
/// Reads <see cref="PlaceExit"/> from either shape:
/// new — object with direction / destination / type / description / restricted / danger_level / tags;
/// legacy — bare string captured into Description (with a default road type).
/// Always writes the object form so the next save normalizes the row.
/// </summary>
public class PlaceExitConverter : JsonConverter<PlaceExit>
{
    public override PlaceExit Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return new PlaceExit();

        if (reader.TokenType == JsonTokenType.String)
            return new PlaceExit { Description = reader.GetString() ?? "", Type = "road" };

        var node = JsonNode.Parse(ref reader);
        if (node is not JsonObject obj) return new PlaceExit();

        return new PlaceExit
        {
            Direction   = obj["direction"]?.GetValue<string>() ?? "",
            Destination = obj["destination"]?.GetValue<string>() ?? "",
            Type        = obj["type"]?.GetValue<string>() ?? "road",
            Description = obj["description"]?.GetValue<string>() ?? "",
            Restricted  = ReadBool(obj["restricted"]),
            DangerLevel = ReadInt(obj["danger_level"]),
            Tags        = obj["tags"]?.Deserialize<List<string>>() ?? [],
        };
    }

    // GetValue<bool>/GetValue<int> throw on "restricted": "true" or "danger_level": "3" / 2.5, and
    // one such exit failed the whole place's deserialization. Anything unreadable falls back to the
    // default instead.
    private static bool ReadBool(JsonNode? n) => n is JsonValue v
        && (v.TryGetValue<bool>(out var b) ? b
            : v.TryGetValue<string>(out var s) && bool.TryParse(s, out var parsed) && parsed);

    private static int ReadInt(JsonNode? n)
    {
        if (n is not JsonValue v) return 0;
        if (v.TryGetValue<int>(out var i)) return i;
        if (v.TryGetValue<double>(out var d)) return (int)Math.Round(d);
        return v.TryGetValue<string>(out var s)
            && int.TryParse(s, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var p)
            ? p : 0;
    }

    public override void Write(Utf8JsonWriter writer, PlaceExit value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("direction", value.Direction ?? "");
        writer.WriteString("destination", value.Destination ?? "");
        writer.WriteString("type", value.Type ?? "road");
        writer.WriteString("description", value.Description ?? "");
        writer.WriteBoolean("restricted", value.Restricted);
        writer.WriteNumber("danger_level", value.DangerLevel);
        writer.WritePropertyName("tags");
        JsonSerializer.Serialize(writer, value.Tags ?? [], options);
        writer.WriteEndObject();
    }
}
