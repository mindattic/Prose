using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Stores user accounts in the SQL <c>Settings</c> table under the key
/// <c>users.accounts</c> (one row, full account list as JSON). Replaces the
/// pre-archival <c>{EngineDataDir}/users.json</c> file. Thread-safe for
/// concurrent reads and writes.
/// </summary>
public class UserRepository
{
    private const string AccountsKey = "users.accounts";

    private readonly SettingsKvStore kv;
    private readonly Lock writeLock = new();
    // Volatile ensures the double-check in EnsureLoaded sees a fully-constructed reference.
    // Without volatile, a thread could see a non-null but partially-initialized list.
    private volatile List<UserAccount>? cache;

    public UserRepository(SettingsKvStore kv)
    {
        this.kv = kv;
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
            cache = kv.Get<List<UserAccount>>(AccountsKey) ?? [];
        }
    }

    private void Save()
    {
        kv.Set(AccountsKey, cache);
    }
}
