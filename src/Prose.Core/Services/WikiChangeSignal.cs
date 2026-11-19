using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

/// <summary>
/// Tells the universe's wiki that Prose changed (PRS-T1049). After a save that touched what the wiki shows (beat text,
/// entities, their records, aliases, relationships, edges, books and chapters), the universe's stamp file
/// <c>%LOCALAPPDATA%\MindAttic\Prose\wiki\changes\&lt;slug&gt;.stamp</c> gets the time of the change. The Scheduler job
/// Prose.GLMZ.Wiki.Sync runs <c>tools\wiki\wiki-watch.ps1</c> every few minutes; it syncs the wiki only when a stamp is
/// newer than the last sync, so the wiki follows Prose within minutes and an idle Prose costs nothing (the database is
/// not touched to find out).
///
/// <para>A file, not a call: the Hub never waits on the wiki, a failed write is ignored (the next change writes it
/// again), and writing one is a few bytes, cheap next to the database save it follows.</para>
/// </summary>
public static class WikiChangeSignal
{
    /// <summary>Folder of the stamp files; overridable for tests.</summary>
    public static string Folder { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MindAttic", "Prose", "wiki", "changes");

    /// <summary>Off for tests and design-time hosts that should leave no files behind.</summary>
    public static bool Enabled { get; set; } = !string.Equals(Environment.GetEnvironmentVariable("PROSE_WIKI_SIGNAL"), "0", StringComparison.Ordinal);

    /// <summary>The universes (by id; Guid.Empty = the ambient one) whose wiki content a pending save changes.
    /// Called before the save, while the change tracker still knows what changed.</summary>
    public static HashSet<Guid> Pending(ChangeTracker tracker)
    {
        var universes = new HashSet<Guid>();
        if (!Enabled) return universes;
        foreach (var entry in tracker.Entries())
        {
            if (entry.State is EntityState.Unchanged or EntityState.Detached) continue;
            if (!Shows(entry)) continue;
            var id = entry.Metadata.FindProperty("UniverseId") is { } p && entry.Property(p.Name).CurrentValue is Guid g ? g : Guid.Empty;
            universes.Add(id);
        }
        return universes;
    }

    static bool Shows(EntityEntry entry) => entry.Entity switch
    {
        // A beat's words (and tags) are the books' text; its other columns (stale flags, audio, timings) are not.
        Beat => entry.State != EntityState.Modified || entry.Property(nameof(Beat.Text)).IsModified,
        Entity or Record or EntityProperty or Edge or Node or Book => true,
        CharacterAlias or PlaceAlias or FactionAlias or WeaponAlias or CharacterRelationshipRow => true,
        _ => false,
    };

    /// <summary>After a successful save: stamp each changed universe. Never throws.</summary>
    public static void Note(HashSet<Guid> universes)
    {
        if (universes.Count == 0) return;
        try
        {
            var context = UniverseScope.Current;
            var all = context?.ListUniverses() ?? [];
            var now = DateTime.UtcNow;
            foreach (var id in universes)
            {
                var slug = id == Guid.Empty || id == context?.CurrentId
                    ? context?.CurrentSlug
                    : all.FirstOrDefault(u => u.Id == id)?.Slug;
                if (string.IsNullOrWhiteSpace(slug)) slug = "_unknown"; // the watcher treats it as every universe
                Directory.CreateDirectory(Folder);
                File.WriteAllText(Path.Combine(Folder, slug.ToLowerInvariant() + ".stamp"), now.ToString("O"));
            }
        }
        catch
        {
            // A stamp is a hint; the save already committed, and the next change writes it again.
        }
    }
}
