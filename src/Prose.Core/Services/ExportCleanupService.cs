using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>Archives the existing per-book export bundle before a new export writes files.</summary>
public class ExportCleanupService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly SettingsService settings;
    public ExportCleanupService(IDbContextFactory<ProseDbContext> dbFactory, SettingsService settings)
    { this.dbFactory = dbFactory; this.settings = settings; }

    public async Task<string> CleanAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.AsNoTracking().IgnoreQueryFilters().Where(s => s.Id == nodeId)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");
        var universeSlug = await db.Universes.AsNoTracking().Where(u => u.Id == node.UniverseId)
            .Select(u => u.Slug).FirstOrDefaultAsync(ct);
        var baseDir = settings.GetExportDirectory(universeSlug);
        var (nodeDir, _) = await ExportPathResolver.ResolveAsync(db, node, baseDir, ct);
        Clean(nodeDir, node.Version);
        return nodeDir;
    }

    /// <summary>Copies existing export artifacts into Archives\v&lt;version&gt;, then removes live copies
    /// (cover.jpg stays). Implemented once in MindAttic.Export
    /// (<see cref="MindAttic.Export.Paths.ExportArchive.Clean"/>).</summary>
    public void Clean(string nodeDir, int? fallbackVersion = null) =>
        MindAttic.Export.Paths.ExportArchive.Clean(nodeDir, fallbackVersion);

    /// <summary>Copies the completed live export bundle into its permanent version archive.</summary>
    public void ArchiveCurrent(string nodeDir, int version) =>
        MindAttic.Export.Paths.ExportArchive.ArchiveCurrent(nodeDir, version);
}
