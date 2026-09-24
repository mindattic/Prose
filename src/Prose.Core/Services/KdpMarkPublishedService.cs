using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Kdp;

namespace Prose.Core.Services;

/// <summary>Result of <see cref="KdpMarkPublishedService.MarkPublishedAsync"/>. <see cref="Ok"/>
/// false means <see cref="Error"/> explains why (node not found, etc.) — everything else is
/// null in that case.</summary>
public record KdpMarkPublishedResult(
    bool Ok,
    string? Error,
    string? Code,
    string? Title,
    DateTime? KdpPublishedAt,
    string? PublishUrl,
    string? RecordedTitleId,
    string? Asin
);

/// <summary>
/// Closes the loop after a human (or an agent) actually finishes republishing a book on KDP.
/// Sets <c>KdpPublishedAt = now</c> and <c>PublicationStatus = "Published"</c> so the book drops
/// off <see cref="KdpManifestService"/>'s "needs republish" list. Optionally updates
/// <see cref="Prose.Core.Data.Entities.Node.PublishUrl"/> (only needed the first time a
/// book goes live, or if the URL changed) and records a titleId in the KDP store's crosswalk
/// (<see cref="KdpStore"/>; was tools/kdp/title-ids.json) so future manifests can deep-link
/// straight to this book's KDP edit page.
///
/// Shared by <c>prose --kdp-mark-published</c> (CLI: <c>KdpMarkPublishedCli</c>, a thin wrapper
/// that prints the result) and the KdpPublish WPF app's <c>mark_published</c> tool.
/// </summary>
public class KdpMarkPublishedService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly SettingsService settings;
    private readonly KdpStore kdpStore;

    public KdpMarkPublishedService(IDbContextFactory<ProseDbContext> dbFactory, SettingsService settings, KdpStore kdpStore)
    {
        this.dbFactory = dbFactory;
        this.settings = settings;
        this.kdpStore = kdpStore;
    }

    public async Task<KdpMarkPublishedResult> MarkPublishedAsync(
        string slug, string? url, string? titleId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var node = await db.Nodes.IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Slug == slug, ct);
        if (node == null)
            return new KdpMarkPublishedResult(false, $"No node with slug '{slug}'.", null, null, null, null, null, null);

        node.KdpPublishedAt = DateTime.UtcNow;
        node.PublicationStatus = "Published";
        if (!string.IsNullOrWhiteSpace(url))
            node.PublishUrl = url.Split("/ref=")[0];
        if (!string.IsNullOrWhiteSpace(titleId))
            node.KdpTitleId = titleId;
        if (string.IsNullOrWhiteSpace(node.Asin) && !string.IsNullOrWhiteSpace(node.PublishUrl))
        {
            var asinMatch = System.Text.RegularExpressions.Regex.Match(node.PublishUrl, @"/dp/([A-Z0-9]{10})");
            if (asinMatch.Success) node.Asin = asinMatch.Groups[1].Value;
        }
        await db.SaveChangesAsync(ct);

        // Hard-coded rule: the book's publish record (the KDP store; was the .publish marker's
        // JSON body) must be refreshed with exactly what
        // just went live — Filename, Version, ASIN, PublishedAtUtc — every time mark_published
        // fires, before the caller's book loop advances to the next book. This runs synchronously
        // inside this call (never speculatively — mark_published itself only fires after a real
        // confirmed publish, per its own tool description), so "before starting the next" is
        // satisfied by ordinary sequencing: the caller's foreach can't reach book N+1 until this
        // returns.
        try
        {
            var universeSlug = await db.Set<Prose.Core.Data.Entities.Universe>().AsNoTracking()
                .Where(u => u.Id == node.UniverseId).Select(u => u.Slug).FirstOrDefaultAsync(ct) ?? "glmz";
            var baseDir = settings.GetExportDirectory(universeSlug);
            string nodeDir; string fileBaseName;
            try { (nodeDir, fileBaseName) = await ExportPathResolver.ResolveAsync(db, node, baseDir, ct); }
            catch { nodeDir = Path.Combine(baseDir, node.NodeCode ?? node.Slug); fileBaseName = node.NodeCode ?? node.Slug; }

            string? epubFile = null;
            var version = node.Version;
            if (Directory.Exists(nodeDir))
            {
                var best = Directory.GetFiles(nodeDir)
                    .Select(f => KdpManifestService.VersionFileRx.Match(Path.GetFileName(f)))
                    .Where(m => m.Success && string.Equals(m.Groups["ext"].Value, "epub", StringComparison.OrdinalIgnoreCase))
                    .Select(m => (Ver: int.Parse(m.Groups["ver"].Value), File: m.Value))
                    .OrderByDescending(x => x.Ver)
                    .FirstOrDefault();
                if (best.File != null) { version = best.Ver; epubFile = best.File; }
            }

            if (epubFile != null && Directory.Exists(nodeDir))
            {
                await kdpStore.RecordPublishAsync(node.NodeCode ?? node.Slug, new KdpPublishSnapshot
                {
                    File = epubFile,
                    Version = version,
                    Asin = node.Asin,
                    PublishedAt = node.KdpPublishedAt is DateTime at ? new DateTimeOffset(DateTime.SpecifyKind(at, DateTimeKind.Utc)) : null,
                }, KdpPublishSource.MarkPublished, ct);
            }
        }
        catch
        {
            // Publish-record refresh is best-effort bookkeeping, never a reason to fail a publish
            // that has already gone live and been recorded in the DB above.
        }

        string? recordedTitleId = null;
        if (!string.IsNullOrWhiteSpace(titleId) && !string.IsNullOrWhiteSpace(node.NodeCode))
        {
            string? asin = null;
            if (node.PublishUrl != null)
            {
                var m = System.Text.RegularExpressions.Regex.Match(node.PublishUrl, @"/dp/([A-Z0-9]{10})");
                if (m.Success) asin = m.Groups[1].Value;
            }
            await kdpStore.UpsertTitleAsync(node.NodeCode, titleId, asin, ct);
            recordedTitleId = titleId;
        }

        return new KdpMarkPublishedResult(true, null, node.NodeCode ?? node.Slug, node.Title, node.KdpPublishedAt, node.PublishUrl, recordedTitleId, node.Asin);
    }

    /// <summary>
    /// Records that <c>find_and_open_book</c> observed KDP hiding a book's edit-content link —
    /// the "Live - Updates publishing" state, up to ~72 hours after a recent republish. Sets the
    /// book's <see cref="KdpBook.PublishingDetectedAt"/> in the KDP store (keeping whatever publish
    /// history it already recorded) so <see cref="KdpManifestService"/> reports "Publishing"
    /// instead of "Outdated" while the timestamp stays fresh. False only for an unknown slug.
    /// </summary>
    public async Task<bool> MarkPublishingDetectedAsync(string slug, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.AsNoTracking().IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Slug == slug, ct);
        if (node == null) return false;

        await kdpStore.MarkPublishingDetectedAsync(node.NodeCode ?? node.Slug, ct);
        return true;
    }

    /// <summary>
    /// The "Mark Unpublished" panel action: clears <c>PublicationStatus</c> and
    /// <c>KdpPublishedAt</c> for the given NodeCodes so <see cref="KdpManifestService"/> stops
    /// treating them as already current (falls into the "Unknown (no baseline)" / stale branch
    /// since <c>PublishUrl</c> is left alone) and a subsequent Start run will actually attempt to
    /// republish them instead of short-circuiting on the version pre-check. Deliberately does NOT
    /// touch <c>PublishUrl</c>/<c>Asin</c>/<c>KdpTitleId</c> — those are how the book gets found
    /// again, forgetting them would defeat the point of this action.
    /// </summary>
    public async Task<int> UnmarkPublishedAsync(IEnumerable<string> codes, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var codeSet = codes.ToHashSet();
        var nodes = await db.Nodes.IgnoreQueryFilters()
            .Where(n => n.NodeCode != null && codeSet.Contains(n.NodeCode))
            .ToListAsync(ct);
        foreach (var node in nodes)
        {
            node.PublicationStatus = null;
            node.KdpPublishedAt = null;
        }
        await db.SaveChangesAsync(ct);
        return nodes.Count;
    }
}
