using System.Text.Json;
using System.Text.RegularExpressions;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Repository that stores one JSON file per entity in a typed directory.
/// Each entity gets its own file named by slugified key (e.g. characters/kyle.json).
///
/// Advantages over single-array JsonDictionaryRepository:
/// - Git-friendly: changing one entity only touches one file
/// - Resilient: one corrupt file doesn't break the entire type
/// - Partial loading: only deserialize what you need
/// - Scalable: works with thousands of entities without loading all into memory
/// - Human-browsable: each file is a self-contained entity
/// </summary>
public partial class JsonDirectoryRepository<T> where T : class
{
    private readonly string _directory;
    private readonly Func<T, string> _nameSelector;
    private readonly JsonSerializerOptions _jsonOptions;
    private List<T>? _cache;

    public JsonDirectoryRepository(string directory, Func<T, string> nameSelector)
    {
        _directory = directory;
        Directory.CreateDirectory(directory);
        _nameSelector = nameSelector;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };
    }

    /// <summary>Fired after an item is saved, with the entity name.</summary>
    public event Action<string>? OnItemSaved;

    public List<T> GetAll()
    {
        if (_cache != null) return _cache;
        if (!Directory.Exists(_directory)) return _cache = [];

        _cache = Directory.GetFiles(_directory, "*.json")
            .Select(LoadFromFile)
            .Where(item => item != null)
            .ToList()!;
        return _cache;
    }

    public T? GetByName(string name)
    {
        // Try direct file lookup first (fast path)
        var slug = Slugify(name);
        var path = Path.Combine(_directory, $"{slug}.json");
        if (File.Exists(path))
        {
            var item = LoadFromFile(path);
            if (item != null) return item;
        }

        // Fallback to scanning all (for case-insensitive or alias matching)
        return GetAll().FirstOrDefault(item =>
            _nameSelector(item).Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public void Save(T item)
    {
        var name = _nameSelector(item);
        var slug = Slugify(name);
        var path = Path.Combine(_directory, $"{slug}.json");

        var json = JsonSerializer.Serialize(item, _jsonOptions);
        File.WriteAllText(path, json);

        // Invalidate cache
        _cache = null;
        OnItemSaved?.Invoke(name);
    }

    public void Delete(string name)
    {
        var slug = Slugify(name);
        var path = Path.Combine(_directory, $"{slug}.json");
        if (File.Exists(path)) File.Delete(path);

        // Also scan for any file containing this entity (in case slug doesn't match)
        if (!File.Exists(path))
        {
            foreach (var file in Directory.GetFiles(_directory, "*.json"))
            {
                var item = LoadFromFile(file);
                if (item != null && _nameSelector(item).Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(file);
                    break;
                }
            }
        }

        _cache = null;
    }

    public void SaveAll(List<T> items)
    {
        foreach (var item in items)
        {
            var name = _nameSelector(item);
            var slug = Slugify(name);
            var path = Path.Combine(_directory, $"{slug}.json");
            File.WriteAllText(path, JsonSerializer.Serialize(item, _jsonOptions));
        }
        _cache = null;
    }

    public void Reload() => _cache = null;

    public int Count()
    {
        if (!Directory.Exists(_directory)) return 0;
        return Directory.GetFiles(_directory, "*.json").Length;
    }

    /// <summary>
    /// Migrate from a single JSON array file into this directory repository.
    /// Reads the array, writes each entity as an individual file, then optionally
    /// renames the source file to .migrated.
    /// </summary>
    public int MigrateFromArrayFile(string sourceFilePath)
    {
        if (!File.Exists(sourceFilePath)) return 0;

        var json = File.ReadAllText(sourceFilePath);
        var items = JsonSerializer.Deserialize<List<T>>(json, _jsonOptions);
        if (items == null || items.Count == 0) return 0;

        Directory.CreateDirectory(_directory);
        foreach (var item in items)
        {
            var name = _nameSelector(item);
            if (string.IsNullOrWhiteSpace(name)) continue;
            var slug = Slugify(name);
            var path = Path.Combine(_directory, $"{slug}.json");
            File.WriteAllText(path, JsonSerializer.Serialize(item, _jsonOptions));
        }

        // Rename source to .migrated so it's not loaded again
        File.Move(sourceFilePath, sourceFilePath + ".migrated", overwrite: true);
        _cache = null;
        return items.Count;
    }

    private T? LoadFromFile(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[JsonDirectoryRepository] Failed to load {path}: {ex.Message}");
            return null;
        }
    }

    public static string Slugify(string name) =>
        SlugRegex().Replace(name.ToLowerInvariant().Trim(), "_").Trim('_');

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex SlugRegex();
}
