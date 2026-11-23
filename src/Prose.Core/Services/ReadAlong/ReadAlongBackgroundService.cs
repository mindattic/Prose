using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services.ReadAlong;

/// <summary>
/// The read-along job (RFC 0019 §3): reads along while the author writes. Every few minutes it
/// looks at the books the read-along is following and reads each chapter whose text changed since
/// it was last read — once the chapter has sat still for a quiet period, so nothing is read (or
/// flagged) mid-sentence.
///
/// <para><b>Opt-in per book.</b> A book is followed once it has been read at least once (imported,
/// or "Read my book" in the Story workspace, or <c>prose --read-along --slug</c>). The job never
/// starts reading a book on its own: hundreds of existing books suddenly drafting thousands of
/// entries is not help.</para>
///
/// <para>Config: <c>BackgroundServices:Enabled</c> (the Hub-wide switch), <c>ReadAlong:Enabled</c>
/// (default true), <c>ReadAlong:IntervalMinutes</c> (default 5), <c>ReadAlong:QuietMinutes</c>
/// (default 3). Deterministic and local: it costs no tokens.</para>
/// </summary>
public sealed class ReadAlongBackgroundService(
    IDbContextFactory<ProseDbContext> dbFactory,
    ReadAlongService readAlong,
    IConfiguration configuration,
    ILogger<ReadAlongBackgroundService> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue("BackgroundServices:Enabled", true) || !configuration.GetValue("ReadAlong:Enabled", true))
        {
            log.LogInformation("Read-along job disabled.");
            return;
        }
        var interval = TimeSpan.FromMinutes(Math.Max(1, configuration.GetValue("ReadAlong:IntervalMinutes", 5)));
        var quiet = TimeSpan.FromMinutes(Math.Max(0, configuration.GetValue("ReadAlong:QuietMinutes", 3)));

        // Let the Hub finish starting before the first sweep.
        try { await Task.Delay(TimeSpan.FromSeconds(45), stoppingToken); } catch (OperationCanceledException) { return; }

        using var timer = new PeriodicTimer(interval);
        do
        {
            try { await SweepAsync(quiet, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex) { log.LogWarning(ex, "Read-along sweep failed; will try again next interval."); }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    /// <summary>One pass over every followed book. Public so the CLI (<c>prose --read-along
    /// --all</c>) and an external scheduler can run the same sweep on demand.</summary>
    public async Task<int> SweepAsync(TimeSpan quiet, CancellationToken ct)
    {
        List<Guid> books;
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            var followed = await db.ChapterReadings.IgnoreQueryFilters().Select(r => r.BookNodeId).Distinct().ToListAsync(ct);
            books = await db.Nodes.IgnoreQueryFilters().Where(n => followed.Contains(n.Id) && !n.IsArchived).Select(n => n.Id).ToListAsync(ct);
        }

        var read = 0;
        foreach (var bookId in books)
        {
            var due = await readAlong.UnreadChaptersAsync(bookId, ct);
            foreach (var chapter in due.Where(c => DateTime.UtcNow - c.LastEdited >= quiet))
            {
                var r = await readAlong.ReadChapterAsync(bookId, chapter.ChapterNodeId, ct: ct);
                if (r.Reason == "allowance") return read;
                if (!r.Skipped) read++;
            }
        }
        if (read > 0) log.LogInformation("Read-along sweep: read {Count} chapter(s).", read);
        return read;
    }
}
