namespace Prose.Core.Services.ReadAlong;

/// <summary>
/// "Follow this book" (RFC 0019, RA-S2.3.2): an explicit per-book on/off for the read-along job. With no
/// explicit choice a book is followed once it has been read along at least once (the original opt-in).
/// Stored as one dictionary under the settings key <c>readalong.follow</c> (a
/// shared setting, so the unscoped job sees every universe's choices); turning a book off deletes nothing.
/// </summary>
public sealed class ReadAlongFollow(SettingsKvStore settings)
{
    public const string SettingsKey = "readalong.follow";

    private Dictionary<string, bool> Load() => settings.Get<Dictionary<string, bool>>(SettingsKey) ?? [];

    /// <summary>The explicit choice for a book, or null when the author never chose.</summary>
    public bool? Get(Guid bookId) => Load().TryGetValue(bookId.ToString("D"), out var on) ? on : null;

    public void Set(Guid bookId, bool on)
    {
        var map = Load();
        map[bookId.ToString("D")] = on;
        settings.Set(SettingsKey, map);
    }

    /// <summary>Every explicit choice, for the job's sweep.</summary>
    public IReadOnlyDictionary<Guid, bool> All() =>
        Load().Where(kv => Guid.TryParse(kv.Key, out _)).ToDictionary(kv => Guid.Parse(kv.Key), kv => kv.Value);

    /// <summary>Whether the job reads this book: the explicit choice, else "it has been read once".</summary>
    public bool IsFollowed(Guid bookId, bool readOnce) => Get(bookId) ?? readOnce;
}
