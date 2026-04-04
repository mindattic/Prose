using System.Text.Json;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Generic repository for reading/writing a List of T from a JSON file.
/// Each entity type gets its own file. Designed for eventual neo4j migration —
/// each file maps to a node collection.
/// </summary>
public class JsonDictionaryRepository<T> where T : class
{
    private readonly string _filePath;
    private readonly Func<T, string> _nameSelector;
    private readonly JsonSerializerOptions _jsonOptions;
    private List<T>? _cache;

    public JsonDictionaryRepository(string filePath, Func<T, string> nameSelector)
    {
        _filePath = filePath;
        _nameSelector = nameSelector;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };
    }

    public List<T> GetAll()
    {
        if (_cache != null) return _cache;
        if (!File.Exists(_filePath)) return _cache = [];

        var json = File.ReadAllText(_filePath);
        _cache = JsonSerializer.Deserialize<List<T>>(json, _jsonOptions) ?? [];
        return _cache;
    }

    public T? GetByName(string name)
    {
        return GetAll().FirstOrDefault(item =>
            _nameSelector(item).Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Fired after an item is saved, with the entity name.</summary>
    public event Action<string>? OnItemSaved;

    public void Save(T item)
    {
        var items = GetAll();
        var name = _nameSelector(item);
        var index = items.FindIndex(x =>
            _nameSelector(x).Equals(name, StringComparison.OrdinalIgnoreCase));

        if (index >= 0)
            items[index] = item;
        else
            items.Add(item);

        Persist(items);
        OnItemSaved?.Invoke(name);
    }

    public void Delete(string name)
    {
        var items = GetAll();
        items.RemoveAll(x =>
            _nameSelector(x).Equals(name, StringComparison.OrdinalIgnoreCase));
        Persist(items);
    }

    public void SaveAll(List<T> items)
    {
        _cache = items;
        Persist(items);
    }

    public void Reload()
    {
        _cache = null;
    }

    private void Persist(List<T> items)
    {
        _cache = items;
        var dir = Path.GetDirectoryName(_filePath);
        if (dir != null) Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(items, _jsonOptions);
        File.WriteAllText(_filePath, json);
    }
}

/// <summary>
/// Repository for a single JSON object (not a list) — used for story_bible, literary_rules, etc.
/// </summary>
public class JsonSingletonRepository<T> where T : class, new()
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private T? _cache;

    public JsonSingletonRepository(string filePath)
    {
        _filePath = filePath;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };
    }

    public T Get()
    {
        if (_cache != null) return _cache;
        if (!File.Exists(_filePath)) return _cache = new T();

        var json = File.ReadAllText(_filePath);
        _cache = JsonSerializer.Deserialize<T>(json, _jsonOptions) ?? new T();
        return _cache;
    }

    public void Save(T item)
    {
        _cache = item;
        var dir = Path.GetDirectoryName(_filePath);
        if (dir != null) Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(item, _jsonOptions);
        File.WriteAllText(_filePath, json);
    }

    public void Reload()
    {
        _cache = null;
    }
}
