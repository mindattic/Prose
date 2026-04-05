using System.Text.Json;
using System.Text.RegularExpressions;
using StreetSamurai.Core.Interfaces;

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
public partial class JsonDirectoryRepository<T> : IExportableRepository where T : class
{
    private readonly string directory;
    private readonly Func<T, string> _nameSelector;
    private readonly JsonSerializerOptions jsonOptions;
    private List<T>? cache;

    public JsonDirectoryRepository(string directory, Func<T, string> nameSelector)
    {
        this.directory = directory;
        Directory.CreateDirectory(directory);
        _nameSelector = nameSelector;
        jsonOptions = new JsonSerializerOptions
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
        if (cache != null) return cache;
        if (!Directory.Exists(directory)) return cache = [];

        cache = Directory.GetFiles(directory, "*.json")
            .Where(f => !IsArchived(f))
            .Select(LoadFromFile)
            .Where(item => item != null)
            .ToList()!;
        return cache;
    }

    public T? GetByName(string name)
    {
        // Try direct file lookup first (fast path)
        var slug = Slugify(name);
        var path = Path.Combine(directory, $"{slug}.json");
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
        var filePath = Path.Combine(directory, $"{slug}.json");

        var json = JsonSerializer.Serialize(item, jsonOptions);

        // Roundtrip validation — prove the JSON deserializes back
        try
        {
            JsonSerializer.Deserialize<T>(json, jsonOptions);
        }
        catch (Exception ex)
        {
            // Validation failed but DON'T lose data — save anyway but log the error
            System.Diagnostics.Debug.WriteLine($"[JsonDirectoryRepository] VALIDATION WARNING for {name}: {ex.Message}. Saving anyway — data preserved but may need repair.");
        }

        File.WriteAllText(filePath, json);

        // Invalidate cache
        cache = null;
        OnItemSaved?.Invoke(name);
    }

    /// <summary>Soft-delete: sets is_archived flag instead of deleting the file.</summary>
    public void Delete(string name)
    {
        var slug = Slugify(name);
        var filePath = Path.Combine(directory, $"{slug}.json");

        // Find the file (may not match slug exactly)
        if (!File.Exists(filePath))
        {
            foreach (var file in Directory.GetFiles(directory, "*.json"))
            {
                var item = LoadFromFile(file);
                if (item != null && _nameSelector(item).Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    filePath = file;
                    break;
                }
            }
        }

        // Set is_archived flag instead of deleting
        if (File.Exists(filePath))
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var doc = JsonDocument.Parse(json);
                using var ms = new System.IO.MemoryStream();
                using var writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true });
                writer.WriteStartObject();

                bool wroteArchived = false;
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.Name == "is_archived")
                    {
                        writer.WriteBoolean("is_archived", true);
                        wroteArchived = true;
                    }
                    else
                    {
                        prop.WriteTo(writer);
                    }
                }
                if (!wroteArchived)
                    writer.WriteBoolean("is_archived", true);

                writer.WriteEndObject();
                writer.Flush();
                File.WriteAllText(filePath, System.Text.Encoding.UTF8.GetString(ms.ToArray()));
            }
            catch
            {
                // If JSON manipulation fails, fall back to hard delete
                File.Delete(filePath);
            }
        }

        cache = null;
    }

    public void SaveAll(List<T> items)
    {
        foreach (var item in items)
        {
            var name = _nameSelector(item);
            var slug = Slugify(name);
            var path = Path.Combine(directory, $"{slug}.json");
            File.WriteAllText(path, JsonSerializer.Serialize(item, jsonOptions));
        }
        cache = null;
    }

    public void Reload() => cache = null;

    /// <summary>Repo display name — derived from directory name.</summary>
    public string RepoName => Path.GetFileName(directory).Replace("_", " ")
        .Replace("consumer goods", "Consumer Goods")
        .Replace("story blocks", "Stories");

    /// <summary>Export all entries as (name, json) pairs. Auto-discovered by export system.</summary>
    public List<(string name, string json)> GetExportEntries()
    {
        return GetAll().Select(e => (_nameSelector(e), JsonSerializer.Serialize(e, jsonOptions))).ToList();
    }

    public int Count()
    {
        if (!Directory.Exists(directory)) return 0;
        return Directory.GetFiles(directory, "*.json").Length;
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
        var items = JsonSerializer.Deserialize<List<T>>(json, jsonOptions);
        if (items == null || items.Count == 0) return 0;

        Directory.CreateDirectory(directory);
        foreach (var item in items)
        {
            var name = _nameSelector(item);
            if (string.IsNullOrWhiteSpace(name)) continue;
            var slug = Slugify(name);
            var path = Path.Combine(directory, $"{slug}.json");
            File.WriteAllText(path, JsonSerializer.Serialize(item, jsonOptions));
        }

        // Rename source to .migrated so it's not loaded again
        File.Move(sourceFilePath, sourceFilePath + ".migrated", overwrite: true);
        cache = null;
        return items.Count;
    }

    private T? LoadFromFile(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<T>(json, jsonOptions);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[JsonDirectoryRepository] Failed to load {filePath}: {ex.Message}");
            // Queue background repair
            QueueRepair(filePath, ex.Message);
            return null;
        }
    }

    // Background repair queue — processes broken files without blocking the UI
    private static readonly List<(string path, string error)> repairQueue = [];
    private static bool repairRunning;

    private static void QueueRepair(string filePath, string error)
    {
        lock (repairQueue)
        {
            repairQueue.Add((filePath, error));
            if (repairRunning) return;
            repairRunning = true;
        }

        // Run repairs on a background thread
        Task.Run(() =>
        {
            while (true)
            {
                (string path, string err) item;
                lock (repairQueue)
                {
                    if (repairQueue.Count == 0) { repairRunning = false; return; }
                    item = repairQueue[0];
                    repairQueue.RemoveAt(0);
                }

                try
                {
                    AttemptAutoRepair(item.path, item.err);
                }
                catch (Exception ex) { Serilog.Log.Warning(ex, "Auto-repair failed for {FilePath}", item.path); }
            }
        });
    }

    private static void AttemptAutoRepair(string filePath, string error)
    {
        try
        {
            var raw = File.ReadAllText(filePath);
            var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            var repaired = false;

            // Common repairs based on error patterns
            using var ms = new System.IO.MemoryStream();
            using var writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true });
            writer.WriteStartObject();

            foreach (var prop in root.EnumerateObject())
            {
                // Fix: numeric values where strings are expected (casualties, tier, etc.)
                if (error.Contains(prop.Name) && error.Contains("System.String") && prop.Value.ValueKind == JsonValueKind.Number)
                {
                    writer.WriteString(prop.Name, prop.Value.GetRawText());
                    repaired = true;
                    continue;
                }

                // Fix: string values where arrays are expected
                if (error.Contains(prop.Name) && error.Contains("List") && prop.Value.ValueKind == JsonValueKind.String)
                {
                    writer.WritePropertyName(prop.Name);
                    writer.WriteStartArray();
                    writer.WriteStringValue(prop.Value.GetString());
                    writer.WriteEndArray();
                    repaired = true;
                    continue;
                }

                // Fix: array items that should be objects (cyberware_inventory, timeline)
                if (error.Contains(prop.Name) && prop.Value.ValueKind == JsonValueKind.Array)
                {
                    writer.WritePropertyName(prop.Name);
                    writer.WriteStartArray();
                    foreach (var item in prop.Value.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String && error.Contains("CyberwareEntry"))
                        {
                            writer.WriteStartObject();
                            writer.WriteString("name", item.GetString());
                            writer.WriteString("body_location", "");
                            writer.WriteString("manufacturer", "");
                            writer.WriteString("tier", "");
                            writer.WriteString("condition", "functional");
                            writer.WriteString("installed_date", "");
                            writer.WriteString("description", item.GetString());
                            writer.WriteString("replaces", "");
                            writer.WriteEndObject();
                            repaired = true;
                        }
                        else if (item.ValueKind == JsonValueKind.String && error.Contains("TimelineEvent"))
                        {
                            writer.WriteStartObject();
                            writer.WriteString("date", "");
                            writer.WriteString("story_id", "");
                            writer.WriteString("event", item.GetString());
                            writer.WriteString("consequences", "");
                            writer.WritePropertyName("body_changes");
                            writer.WriteStartArray();
                            writer.WriteEndArray();
                            writer.WriteString("status_change", "");
                            writer.WriteEndObject();
                            repaired = true;
                        }
                        else
                        {
                            item.WriteTo(writer);
                        }
                    }
                    writer.WriteEndArray();
                    continue;
                }

                // Pass through unmodified
                prop.Value.WriteTo(writer);
            }

            writer.WriteEndObject();
            writer.Flush();

            if (repaired)
            {
                var repairedJson = System.Text.Encoding.UTF8.GetString(ms.ToArray());
                File.WriteAllText(filePath, repairedJson);
                System.Diagnostics.Debug.WriteLine($"[JsonDirectoryRepository] AUTO-REPAIRED: {filePath}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[JsonDirectoryRepository] Auto-repair failed for {filePath}: {ex.Message}");
        }
    }

    /// <summary>Check if a JSON file has is_archived: true without full deserialization.</summary>
    private static bool IsArchived(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            // Fast check without full parse — look for "is_archived": true
            if (json.Contains("\"is_archived\""))
            {
                var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("is_archived", out var val) && val.ValueKind == JsonValueKind.True)
                    return true;
            }
            return false;
        }
        catch (Exception ex) { Serilog.Log.Warning(ex, "Failed to check archived status for {FilePath}", filePath); return false; }
    }

    public static string Slugify(string name) =>
        SlugRegex().Replace(name.ToLowerInvariant().Trim(), "_").Trim('_');

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex SlugRegex();
}
