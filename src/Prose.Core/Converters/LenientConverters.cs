using System.Text.Json;
using System.Text.Json.Serialization;
using Prose.Core.Models.Canon;

namespace Prose.Core.Converters;

/// <summary>
/// Reads a CyberwareEntry from either a plain string or a full JSON object.
/// Generators historically wrote cyberware_inventory as string arrays;
/// the lenient converter coerces those strings to objects on load.
/// The Save path always writes proper objects.
/// </summary>
public class CyberwareEntryConverter : JsonConverter<CyberwareEntry>
{
    public override CyberwareEntry? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString() ?? "";
            return new CyberwareEntry { Name = s, Description = s };
        }
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected string or object for CyberwareEntry, got {reader.TokenType}");

        var entry = new CyberwareEntry();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException();
            var prop = reader.GetString()!;
            reader.Read();
            switch (prop)
            {
                case "name":           entry.Name = reader.GetString() ?? ""; break;
                case "body_location":  entry.BodyLocation = reader.GetString() ?? ""; break;
                case "manufacturer":   entry.Manufacturer = reader.GetString() ?? ""; break;
                case "tier":           entry.Tier = reader.GetString() ?? ""; break;
                case "condition":      entry.Condition = reader.GetString() ?? "functional"; break;
                case "installed_date": entry.InstalledDate = reader.GetString() ?? ""; break;
                case "description":    entry.Description = reader.GetString() ?? ""; break;
                case "replaces":       entry.Replaces = reader.GetString() ?? ""; break;
                // Generator variants
                case "slot":           entry.BodyLocation = reader.GetString() ?? ""; break;
                case "grade":          entry.Tier = reader.GetString() ?? ""; break;
                default:               reader.Skip(); break;
            }
        }
        return entry;
    }

    public override void Write(Utf8JsonWriter writer, CyberwareEntry value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("name", value.Name);
        writer.WriteString("body_location", value.BodyLocation);
        writer.WriteString("manufacturer", value.Manufacturer);
        writer.WriteString("tier", value.Tier);
        writer.WriteString("condition", value.Condition);
        writer.WriteString("installed_date", value.InstalledDate);
        writer.WriteString("description", value.Description);
        writer.WriteString("replaces", value.Replaces);
        writer.WriteEndObject();
    }
}

/// <summary>
/// Reads a NotableLocation from either a plain string or a full JSON object.
/// Generators historically wrote notable_locations as string arrays.
/// </summary>
public class NotableLocationConverter : JsonConverter<NotableLocation>
{
    public override NotableLocation? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString() ?? "";
            // Split "Name — description" style strings if present
            var dash = s.IndexOf(" — ", StringComparison.Ordinal);
            return dash > 0
                ? new NotableLocation { Name = s[..dash].Trim(), Description = s[(dash + 3)..].Trim() }
                : new NotableLocation { Name = s };
        }
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected string or object for NotableLocation, got {reader.TokenType}");

        var loc = new NotableLocation();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException();
            var prop = reader.GetString()!;
            reader.Read();
            switch (prop)
            {
                case "name":        loc.Name = reader.GetString() ?? ""; break;
                case "description": loc.Description = reader.GetString() ?? ""; break;
                case "tags":
                    loc.Tags = [];
                    if (reader.TokenType == JsonTokenType.StartArray)
                    {
                        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                            if (reader.TokenType == JsonTokenType.String)
                                loc.Tags.Add(reader.GetString()!);
                    }
                    break;
                default: reader.Skip(); break;
            }
        }
        return loc;
    }

    public override void Write(Utf8JsonWriter writer, NotableLocation value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("name", value.Name);
        writer.WriteString("description", value.Description);
        writer.WritePropertyName("tags");
        writer.WriteStartArray();
        foreach (var t in value.Tags) writer.WriteStringValue(t);
        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}

/// <summary>
/// Reads a PlaceExit from either a plain string or a full JSON object.
/// Generators historically wrote connections.exits as string arrays.
/// </summary>
public class PlaceExitConverter : JsonConverter<PlaceExit>
{
    public override PlaceExit? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        if (reader.TokenType == JsonTokenType.String)
            return new PlaceExit { Description = reader.GetString() ?? "" };
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected string or object for PlaceExit, got {reader.TokenType}");

        var exit = new PlaceExit();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException();
            var prop = reader.GetString()!;
            reader.Read();
            switch (prop)
            {
                case "direction":    exit.Direction = reader.GetString() ?? ""; break;
                case "destination":  exit.Destination = reader.GetString() ?? ""; break;
                case "type":         exit.Type = reader.GetString() ?? "road"; break;
                case "description":  exit.Description = reader.GetString() ?? ""; break;
                case "restricted":   exit.Restricted = reader.TokenType == JsonTokenType.True; break;
                case "danger_level": exit.DangerLevel = reader.TokenType == JsonTokenType.Number ? reader.GetInt32() : 0; break;
                case "tags":
                    exit.Tags = [];
                    if (reader.TokenType == JsonTokenType.StartArray)
                    {
                        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                            if (reader.TokenType == JsonTokenType.String)
                                exit.Tags.Add(reader.GetString()!);
                    }
                    break;
                default: reader.Skip(); break;
            }
        }
        return exit;
    }

    public override void Write(Utf8JsonWriter writer, PlaceExit value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("direction", value.Direction);
        writer.WriteString("destination", value.Destination);
        writer.WriteString("type", value.Type);
        writer.WriteString("description", value.Description);
        writer.WriteBoolean("restricted", value.Restricted);
        writer.WriteNumber("danger_level", value.DangerLevel);
        writer.WritePropertyName("tags");
        writer.WriteStartArray();
        foreach (var t in value.Tags) writer.WriteStringValue(t);
        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}
