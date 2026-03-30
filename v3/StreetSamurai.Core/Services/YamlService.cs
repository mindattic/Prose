using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace StreetSamurai.Core.Services;

public class YamlService
{
    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    private readonly ISerializer _serializer = new SerializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .Build();

    public T Load<T>(string filePath)
    {
        var yaml = File.ReadAllText(filePath);
        return _deserializer.Deserialize<T>(yaml);
    }

    public Dictionary<string, object> LoadDynamic(string filePath)
    {
        var yaml = File.ReadAllText(filePath);
        return _deserializer.Deserialize<Dictionary<string, object>>(yaml)
               ?? new Dictionary<string, object>();
    }

    public string GetRawYaml(string filePath) => File.ReadAllText(filePath);

    public string Serialize<T>(T obj) => _serializer.Serialize(obj);

    public T DeserializeString<T>(string yaml) => _deserializer.Deserialize<T>(yaml);
}
