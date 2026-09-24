using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Core.Kdp;

/// <summary>
/// Every book's export folder that exists on disk, keyed the way the manifest keys books
/// (NodeCode, else slug) and resolved the way the exporter writes them
/// (<see cref="ExportPathResolver"/> under <see cref="SettingsService.GetExportDirectory"/>).
/// Read-only against the Prose database.
/// </summary>
public sealed class KdpBookFolderLocator(IDbContextFactory<ProseDbContext> dbFactory, SettingsService settings) : IKdpBookFolderSource
{
    public async Task<IReadOnlyList<KdpBookFolder>> ListAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var universes = await db.Set<Data.Entities.Universe>().AsNoTracking().ToDictionaryAsync(u => u.Id, u => u.Slug, ct);

        // Coded nodes publish flat under <export>/<CODE>; uncoded ones only matter if they were
        // ever tracked for KDP (the same rows the manifest considers).
        var nodes = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(n => n.NodeCode != null || n.PublishUrl != null || n.PublicationStatus != null)
            .ToListAsync(ct);

        var result = new List<KdpBookFolder>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var n in nodes)
        {
            var code = n.NodeCode ?? n.Slug;
            if (!seen.Add(code)) continue;
            var baseDir = settings.GetExportDirectory(universes.TryGetValue(n.UniverseId, out var slug) ? slug : "glmz");
            string dir;
            try { (dir, _) = await ExportPathResolver.ResolveAsync(db, n, baseDir, ct); }
            catch { dir = Path.Combine(baseDir, code); }
            if (Directory.Exists(dir)) result.Add(new KdpBookFolder(code, dir));
        }
        return result;
    }
}
