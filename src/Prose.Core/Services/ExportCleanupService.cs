using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>Archives the existing per-book export bundle before a new export writes files, and
/// mirrors the newest bundle to the public web site once it's written.</summary>
public class ExportCleanupService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly SettingsService settings;
    private readonly WebPublishService webPublish;

    public ExportCleanupService(IDbContextFactory<ProseDbContext> dbFactory, SettingsService settings, WebPublishService webPublish)
    {
        this.dbFactory = dbFactory;
        this.settings = settings;
        this.webPublish = webPublish;
    }

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
        Clean(baseDir, nodeDir, node.Version);
        return nodeDir;
    }

    /// <summary>Copies existing export artifacts into the sibling Archives tree (see
    /// <see cref="ArchiveDirFor"/>), then removes live copies (cover.jpg stays). Implemented once
    /// in MindAttic.Export (<see cref="MindAttic.Export.Paths.ExportArchive.Clean"/>).</summary>
    public void Clean(string baseDir, string nodeDir, int? fallbackVersion = null) =>
        MindAttic.Export.Paths.ExportArchive.Clean(nodeDir, ArchiveDirFor(baseDir, nodeDir), fallbackVersion);

    /// <summary>Copies the completed live export bundle into its permanent version archive, then
    /// mirrors the newest bundle to the public web site (author decision 2026-11-16, PRS-T942:
    /// every export publishes immediately, across every universe, no opt-out). A publish failure
    /// (no credentials yet, host unreachable) is logged and swallowed by
    /// <see cref="WebPublishService"/> — the local archive always lands either way.</summary>
    public async Task ArchiveCurrentAsync(Guid nodeId, string nodeDir, int version, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.AsNoTracking().IgnoreQueryFilters().Where(n => n.Id == nodeId)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");
        var universeSlug = await db.Universes.AsNoTracking().Where(u => u.Id == node.UniverseId)
            .Select(u => u.Slug).FirstOrDefaultAsync(ct);
        var baseDir = settings.GetExportDirectory(universeSlug);
        MindAttic.Export.Paths.ExportArchive.ArchiveCurrent(nodeDir, ArchiveDirFor(baseDir, nodeDir), version);
        await webPublish.PublishAsync(baseDir, nodeDir, ct);
    }

    /// <summary>
    /// The sibling archive folder for a book's export folder: a tree that mirrors
    /// <c>&lt;universe&gt;\&lt;book-relative-path&gt;</c> one level up from the universe's own
    /// export root, so Archives never nests under the live bundle folder (author decision
    /// 2026-11-16, PRS-T942). E.g. <c>...\ePub\MindAttic\GLMZ\ATTE</c> archives to
    /// <c>...\ePub\MindAttic\Archives\GLMZ\ATTE</c>.
    /// </summary>
    internal static string ArchiveDirFor(string baseDir, string nodeDir)
    {
        var trimmed = baseDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var mindAtticRoot = Path.GetDirectoryName(trimmed) ?? trimmed;
        var universeFolder = Path.GetFileName(trimmed);
        var relative = Path.GetRelativePath(baseDir, nodeDir);
        return relative == "."
            ? Path.Combine(mindAtticRoot, "Archives", universeFolder)
            : Path.Combine(mindAtticRoot, "Archives", universeFolder, relative);
    }
}
