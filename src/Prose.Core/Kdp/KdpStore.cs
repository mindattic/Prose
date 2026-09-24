using Microsoft.EntityFrameworkCore;

namespace Prose.Core.Kdp;

/// <summary>
/// The one API every KDP reader and writer goes through (KdpManifestService,
/// KdpMarkPublishedService, KdpOperatorService, KdpRunLogService, KdpPublish, the
/// <c>prose --kdp-*</c> commands). Each call makes sure the database is migrated and the
/// one-time import of the legacy JSON files has run (<see cref="EnsureReadyAsync"/>), so no
/// caller can read an empty store just because it happened to be first.
/// </summary>
public sealed class KdpStore
{
    /// <summary>A second confirmation of the same manuscript within this window updates the
    /// existing history entry instead of adding one. mark_published and the operator's
    /// post-publish hook both record the same publish seconds apart.</summary>
    public static readonly TimeSpan SamePublishWindow = TimeSpan.FromMinutes(30);

    private readonly IDbContextFactory<KdpDbContext> factory;
    private readonly KdpJsonTransfer transfer;
    private readonly TimeProvider clock;

    public KdpStore(IDbContextFactory<KdpDbContext> factory, KdpJsonTransfer transfer, TimeProvider? clock = null)
    {
        this.factory = factory;
        this.transfer = transfer;
        this.clock = clock ?? TimeProvider.System;
    }

    /// <summary>Migrates the database and runs the first-run import if it has never happened.
    /// Cheap after the first call. Called at startup by KdpPublish and every KDP CLI command.</summary>
    public async Task EnsureReadyAsync(CancellationToken ct = default)
    {
        await KdpMigrator.EnsureMigratedAsync(factory, ct);
        await transfer.AutoImportIfNeededAsync(ct);
    }

    private DateTimeOffset Now => clock.GetUtcNow();

    // ── Title-id crosswalk ────────────────────────────────────────────────

    public async Task<Dictionary<string, KdpTitle>> GetTitlesAsync(CancellationToken ct = default)
    {
        await EnsureReadyAsync(ct);
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Titles.AsNoTracking().ToDictionaryAsync(t => t.Code, StringComparer.Ordinal, ct);
    }

    public async Task<KdpTitle?> GetTitleAsync(string code, CancellationToken ct = default)
    {
        await EnsureReadyAsync(ct);
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Titles.AsNoTracking().FirstOrDefaultAsync(t => t.Code == code, ct);
    }

    /// <summary>Records a book's KDP titleId (and ASIN, when known). A null argument leaves that
    /// field as it was.</summary>
    public async Task UpsertTitleAsync(string code, string? titleId, string? asin, CancellationToken ct = default)
    {
        await EnsureReadyAsync(ct);
        await using var db = await factory.CreateDbContextAsync(ct);
        var row = await db.Titles.FirstOrDefaultAsync(t => t.Code == code, ct);
        if (row == null)
        {
            row = new KdpTitle { Code = code, SortOrder = await NextTitleSortOrderAsync(db, ct) };
            db.Titles.Add(row);
        }
        if (titleId != null) row.TitleId = titleId;
        if (asin != null) row.Asin = asin;
        row.UpdatedAt = Now;
        await db.SaveChangesAsync(ct);
    }

    internal static async Task<int> NextTitleSortOrderAsync(KdpDbContext db, CancellationToken ct)
    {
        var maxTitle = await db.Titles.Select(t => (int?)t.SortOrder).MaxAsync(ct) ?? -1;
        var maxNote = await db.TitleNotes.Select(t => (int?)t.SortOrder).MaxAsync(ct) ?? -1;
        return Math.Max(maxTitle, maxNote) + 1;
    }

    // ── Books: sign-off gate + publish history ────────────────────────────

    public async Task<Dictionary<string, KdpBook>> GetBooksAsync(CancellationToken ct = default)
    {
        await EnsureReadyAsync(ct);
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Books.AsNoTracking().ToDictionaryAsync(b => b.Code, StringComparer.Ordinal, ct);
    }

    public async Task<KdpBook?> GetBookAsync(string code, bool withHistory = false, CancellationToken ct = default)
    {
        await EnsureReadyAsync(ct);
        await using var db = await factory.CreateDbContextAsync(ct);
        IQueryable<KdpBook> q = db.Books.AsNoTracking();
        if (withHistory) q = q.Include(b => b.History.OrderBy(h => h.RecordedAt));
        return await q.FirstOrDefaultAsync(b => b.Code == code, ct);
    }

    /// <summary>Signs books off for publishing (<paramref name="ready"/> true) or holds them back.
    /// Returns the number of books changed.</summary>
    public async Task<int> SetSignOffAsync(IEnumerable<string> codes, bool ready, string changedBy, CancellationToken ct = default)
    {
        await EnsureReadyAsync(ct);
        await using var db = await factory.CreateDbContextAsync(ct);
        var changed = 0;
        foreach (var code in codes.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.Ordinal))
        {
            var book = await GetOrAddBookAsync(db, code, ct);
            if (book.SignOff.Ready == ready && db.Entry(book).State != EntityState.Added) continue;
            book.SignOff = new KdpSignOff { Ready = ready, ChangedAt = Now, ChangedBy = changedBy };
            book.UpdatedAt = Now;
            changed++;
        }
        await db.SaveChangesAsync(ct);
        return changed;
    }

    /// <summary>
    /// Records a confirmed publish: becomes the book's <see cref="KdpBook.LastPublish"/>, clears
    /// the "publishing detected" window, and is appended to its history — unless it is the same
    /// manuscript confirmed again within <see cref="SamePublishWindow"/>, which fills in the
    /// existing entry instead. A book the store has never seen is created signed off, exactly as
    /// writing its <c>.publish</c> marker used to sign it off.
    /// </summary>
    public async Task RecordPublishAsync(string code, KdpPublishSnapshot publish, KdpPublishSource source, CancellationToken ct = default)
    {
        await EnsureReadyAsync(ct);
        await using var db = await factory.CreateDbContextAsync(ct);
        var book = await GetOrAddBookAsync(db, code, ct);
        if (db.Entry(book).State == EntityState.Added)
            book.SignOff = new KdpSignOff { Ready = true, ChangedAt = Now, ChangedBy = source.ToString() };

        var last = await db.PublishRecords
            .Where(r => r.Code == code)
            .OrderByDescending(r => r.RecordedAt).ThenByDescending(r => r.Id)
            .FirstOrDefaultAsync(ct);

        if (last != null
            && string.Equals(last.Publish.File, publish.File, StringComparison.OrdinalIgnoreCase)
            && Now - last.RecordedAt < SamePublishWindow)
        {
            last.Publish = new KdpPublishSnapshot
            {
                File = last.Publish.File ?? publish.File,
                Version = publish.Version ?? last.Publish.Version,
                Asin = publish.Asin ?? last.Publish.Asin,
                PublishedAt = last.Publish.PublishedAt ?? publish.PublishedAt,
            };
            book.LastPublish = last.Publish.Clone();
        }
        else
        {
            db.PublishRecords.Add(new KdpPublishRecord { Code = code, Publish = publish.Clone(), Source = source, RecordedAt = Now });
            book.LastPublish = publish.Clone();
        }

        book.PublishingDetectedAt = null;
        book.UpdatedAt = Now;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Notes KDP's "Live - Updates publishing" state for a book, keeping its publish
    /// history as it is.</summary>
    public async Task MarkPublishingDetectedAsync(string code, CancellationToken ct = default)
    {
        await EnsureReadyAsync(ct);
        await using var db = await factory.CreateDbContextAsync(ct);
        var book = await GetOrAddBookAsync(db, code, ct);
        book.PublishingDetectedAt = Now;
        book.UpdatedAt = Now;
        await db.SaveChangesAsync(ct);
    }

    private async Task<KdpBook> GetOrAddBookAsync(KdpDbContext db, string code, CancellationToken ct)
    {
        var book = db.Books.Local.FirstOrDefault(b => b.Code == code)
                   ?? await db.Books.FirstOrDefaultAsync(b => b.Code == code, ct);
        if (book != null) return book;
        book = new KdpBook { Code = code, UpdatedAt = Now };
        db.Books.Add(book);
        return book;
    }

    // ── Category trees ────────────────────────────────────────────────────

    public async Task SaveCategoryTreeAsync(KdpCategoryTree tree, CancellationToken ct = default)
    {
        await EnsureReadyAsync(ct);
        await using var db = await factory.CreateDbContextAsync(ct);
        var existing = await db.CategoryTrees.FirstOrDefaultAsync(t => t.Slug == tree.Slug, ct);
        if (existing == null) db.CategoryTrees.Add(tree);
        else
        {
            existing.StartPath = tree.StartPath;
            existing.Crawl = tree.Crawl;
            existing.Tree = tree.Tree;
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<KdpCategoryTree>> GetCategoryTreesAsync(CancellationToken ct = default)
    {
        await EnsureReadyAsync(ct);
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.CategoryTrees.AsNoTracking().OrderBy(t => t.Slug).ToListAsync(ct);
    }

    // ── Run logs ──────────────────────────────────────────────────────────

    public async Task StartRunAsync(Guid runId, IEnumerable<string> codes, string? logFileName, CancellationToken ct = default)
    {
        await EnsureReadyAsync(ct);
        await using var db = await factory.CreateDbContextAsync(ct);
        db.Runs.Add(new KdpRun { Id = runId, StartedAt = Now, Codes = codes.ToList(), LogFileName = logFileName });
        await db.SaveChangesAsync(ct);
    }

    public async Task AppendRunLineAsync(Guid runId, DateTimeOffset at, string message, CancellationToken ct = default)
    {
        await EnsureReadyAsync(ct);
        await using var db = await factory.CreateDbContextAsync(ct);
        db.RunLines.Add(new KdpRunLine { RunId = runId, At = at, Message = message });
        await db.SaveChangesAsync(ct);
    }

    public async Task FinishRunAsync(Guid runId, CancellationToken ct = default)
    {
        await EnsureReadyAsync(ct);
        await using var db = await factory.CreateDbContextAsync(ct);
        var run = await db.Runs.FirstOrDefaultAsync(r => r.Id == runId, ct);
        if (run == null) return;
        run.FinishedAt = Now;
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<KdpRun>> GetRecentRunsAsync(int take, CancellationToken ct = default)
    {
        await EnsureReadyAsync(ct);
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Runs.AsNoTracking().OrderByDescending(r => r.StartedAt).Take(take).ToListAsync(ct);
    }

    // ── Status ────────────────────────────────────────────────────────────

    public sealed record Status(string Database, KdpTransferCounts Counts, List<KdpImport> Imports);

    public async Task<Status> GetStatusAsync(CancellationToken ct = default)
    {
        await EnsureReadyAsync(ct);
        await using var db = await factory.CreateDbContextAsync(ct);
        var counts = new KdpTransferCounts
        {
            Titles = await db.Titles.CountAsync(ct),
            Books = await db.Books.CountAsync(ct),
            PublishRecords = await db.PublishRecords.CountAsync(ct),
            CategoryTrees = await db.CategoryTrees.CountAsync(ct),
            Runs = await db.Runs.CountAsync(ct),
            RunLines = await db.RunLines.CountAsync(ct),
        };
        var imports = await db.Imports.AsNoTracking().OrderBy(i => i.At).ToListAsync(ct);
        return new Status(db.Database.GetConnectionString() ?? "", counts, imports);
    }
}
