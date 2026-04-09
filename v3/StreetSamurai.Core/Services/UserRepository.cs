using System.Text.Json;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Stores user accounts in a single JSON file at {EngineDataDir}/users.json.
/// Thread-safe for concurrent reads and writes.
/// </summary>
public class UserRepository
{
    private readonly string filePath;
    private readonly Lock writeLock = new();
    // Volatile ensures the double-check in EnsureLoaded sees a fully-constructed reference.
    // Without volatile, a thread could see a non-null but partially-initialized list.
    private volatile List<UserAccount>? cache;

    public UserRepository(IPathProvider paths)
    {
        filePath = Path.Combine(paths.EngineDataDir, "users.json");
    }

    public List<UserAccount> GetAll()
    {
        EnsureLoaded();
        lock (writeLock) { return cache!.Select(Clone).ToList(); }
    }

    public UserAccount? GetByEmail(string email)
    {
        EnsureLoaded();
        lock (writeLock)
        {
            var user = cache!.FirstOrDefault(u =>
                u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            return user == null ? null : Clone(user);
        }
    }

    public UserAccount? GetById(string id)
    {
        EnsureLoaded();
        lock (writeLock)
        {
            var user = cache!.FirstOrDefault(u => u.Id == id);
            return user == null ? null : Clone(user);
        }
    }

    /// <summary>
    /// Defensive copy — callers cannot mutate the cache by modifying returned objects.
    /// Prevents race-condition privilege escalation.
    /// </summary>
    private static UserAccount Clone(UserAccount u) => new()
    {
        Id = u.Id,
        Email = u.Email,
        DisplayName = u.DisplayName,
        PasswordHash = u.PasswordHash,
        Role = u.Role,
        SecurityStamp = u.SecurityStamp,
        MustChangePassword = u.MustChangePassword,
        CreatedUtc = u.CreatedUtc,
        LastLoginUtc = u.LastLoginUtc,
    };

    public void Add(UserAccount user)
    {
        EnsureLoaded();
        lock (writeLock)
        {
            cache!.Add(user);
            Save();
        }
    }

    public void Update(UserAccount user)
    {
        EnsureLoaded();
        lock (writeLock)
        {
            var idx = cache!.FindIndex(u => u.Id == user.Id);
            if (idx >= 0)
            {
                cache[idx] = user;
                Save();
            }
        }
    }

    public void Delete(string id)
    {
        EnsureLoaded();
        lock (writeLock)
        {
            cache!.RemoveAll(u => u.Id == id);
            Save();
        }
    }

    public int Count
    {
        get { EnsureLoaded(); lock (writeLock) { return cache!.Count; } }
    }

    private void EnsureLoaded()
    {
        if (cache != null) return;
        lock (writeLock)
        {
            if (cache != null) return;
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                cache = JsonSerializer.Deserialize<List<UserAccount>>(json) ?? [];
            }
            else
            {
                cache = [];
            }
        }
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(cache, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }
}
