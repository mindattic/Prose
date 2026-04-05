using System.Text.Json;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Generic repository for reading/writing a List of T from a JSON file.
/// Each entity type gets its own file. Designed for eventual neo4j migration —
/// each file maps to a node collection.
/// </summary>
public class JsonDictionaryRepository<T> where T : class
{
    private readonly string filePath;
    private readonly Func<T, string> _nameSelector;
    private readonly JsonSerializerOptions jsonOptions;
    private List<T>? cache;

    public JsonDictionaryRepository(string filePath, Func<T, string> nameSelector)
    {
        this.filePath = filePath;
        _nameSelector = nameSelector;
        jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };
    }

    public List<T> GetAll()
    {
        if (cache != null) return cache;
        if (!File.Exists(filePath)) return cache = [];

        var json = File.ReadAllText(filePath);
        cache = JsonSerializer.Deserialize<List<T>>(json, jsonOptions) ?? [];
        return cache;
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
        cache = items;
        Persist(items);
    }

    public void Reload()
    {
        cache = null;
    }

    private void Persist(List<T> items)
    {
        cache = items;
        var dir = Path.GetDirectoryName(filePath);
        if (dir != null) Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(items, jsonOptions);
        File.WriteAllText(filePath, json);
    }
}

/// <summary>
/// Repository for a single JSON object (not a list) — used for story_bible, literary_rules, etc.
/// </summary>
public class JsonSingletonRepository<T> where T : class, new()
{
    private readonly string filePath;
    private readonly JsonSerializerOptions jsonOptions;
    private T? cache;

    public JsonSingletonRepository(string filePath)
    {
        this.filePath = filePath;
        jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };
    }

    public T Get()
    {
        if (cache != null) return cache;
        if (!File.Exists(filePath)) return cache = new T();

        var json = File.ReadAllText(filePath);
        cache = JsonSerializer.Deserialize<T>(json, jsonOptions) ?? new T();
        return cache;
    }

    public void Save(T item)
    {
        cache = item;
        var dir = Path.GetDirectoryName(filePath);
        if (dir != null) Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(item, jsonOptions);
        File.WriteAllText(filePath, json);
    }

    public void Reload()
    {
        cache = null;
    }
}
