using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

/// <summary>
/// Background pass that re-runs <see cref="SanityScanService"/> (internal-code-leak,
/// undefined-acronym, mojibake, below-length-floor detection — all deterministic, zero
/// LLM calls) across every top-level book node in every universe, on a timer.
///
/// Found 2026-08-09: despite its "nightly" name, <see cref="NightlyHealthService"/> (and
/// <see cref="BookHealthService"/>, which SanityScanService is wired into) were both
/// 100% manual — CLI/MCP only, nothing actually scheduled them. A code-leak or mojibake
/// defect introduced by any future beat edit would sit unnoticed indefinitely unless a
/// human remembered to run <c>--sanity-scan</c> by hand. This service is the fix: the
/// same class of always-on periodic sweep as <see cref="ContinuityLongSweepService"/>
/// (its own doc comment and cadence design were used directly as the template here),
/// scoped to zero-cost checks only so it carries no ongoing LLM/API cost.
///
/// Mechanism: <c>PeriodicTimer</c> inside a <see cref="BackgroundService"/>. In-process,
/// no extra packages. Registered only via <c>AddProseBackgroundServices</c> (the Codex
/// host), never the Writer or CLI/MCP hosts — same one-process-only rule as every other
/// background sweep in this file, to avoid duplicate corpus scans hitting the shared DB.
/// </summary>
public class SanityScanBackgroundService : BackgroundService
{
    private static readonly TimeSpan SweepInterval = TimeSpan.FromHours(24);
    // Wait briefly after process start so this doesn't compete with the home-page
    // cold-start for SQL connections — same rationale and duration as
    // ContinuityLongSweepService.StartupDelay.
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(5);

    // Set BackgroundServices:Enabled=false in App Service config to stop DB keep-alive
    // on zero-user deployments — same flag every other background sweep here honors.
    public bool Enabled { get; }

    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly SanityScanService sanityScan;
    private readonly FindingsService findingsSvc;
    private readonly ILogger<SanityScanBackgroundService> log;

    public SanityScanBackgroundService(
        IDbContextFactory<ProseDbContext> dbFactory,
        SanityScanService sanityScan,
        FindingsService findingsSvc,
        ILogger<SanityScanBackgroundService> log,
        IConfiguration configuration)
    {
        this.dbFactory   = dbFactory;
        this.sanityScan  = sanityScan;
        this.findingsSvc = findingsSvc;
        this.log         = log;
        Enabled = configuration.GetValue<bool>("BackgroundServices:Enabled", defaultValue: true);
    }

    /// <summary>Last sweep result, surfaced for diagnostic / status pages.</summary>
    public DateTime? LastSweepAt { get; private set; }
    public int LastSweepBookCount { get; private set; }
    public int LastSweepBlockCount { get; private set; }
    public int LastSweepWarnCount { get; private set; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!Enabled)
        {
            log.LogInformation("SanityScanBackgroundService disabled (BackgroundServices:Enabled=false).");
            return;
        }

        try { await Task.Delay(StartupDelay, stoppingToken); }
        catch (OperationCanceledException) { return; }

        using var timer = new PeriodicTimer(SweepInterval);
        do
        {
            try { await RunSweepAsync(stoppingToken); }
            catch (Exception ex)
            {
                // Sweep is best-effort — never let one bad pass kill the timer.
                log.LogWarning(ex, "Sanity-scan background sweep failed (will retry next interval)");
            }
        } while (await SafeWaitAsync(timer, stoppingToken));
    }

    private async Task RunSweepAsync(CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // IgnoreQueryFilters(): this sweep runs with no ambient universe scope by design
        // (it's a corpus-wide background job, not a per-request/per-command call) — the
        // same reasoning as WorkflowMonitorService.GetAllNodesWithGapsAsync's cross-universe
        // sweep. Every book across every universe, not just the default one.
        //
        // OfType<BookNode>(), not `Kind == "book"`: Kind is a free-form DISPLAY label (per
        // Node's own doc comment in ProseDbContext), NodeType is the structural TPH
        // discriminator that actually determines the row's real type — confirmed live: a
        // `Kind == "book"` filter found only 30 rows against SanityScanCli's own
        // `OfType<BookNode>()` query, which is the established, already-correct pattern this
        // sweep should match exactly so the manual `--sanity-scan --all` command and this
        // automated sweep always agree on which books exist. Deliberately NOT restricted to
        // ParentNodeId == null either, matching SanityScanCli — a Drafts-bucket sub-book with
        // real content is still worth catching leaks/mojibake in before it's ever promoted.
        var books = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .OfType<BookNode>()
            .Select(n => new { n.Id, n.Slug })
            .ToListAsync(ct);

        int scannedCount = 0, blockCount = 0, warnCount = 0;
        foreach (var book in books)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var report = await sanityScan.ScanAsync(book.Id, ct);

                // Same "> 2 beats" eligibility floor as SanityScanCli's `eligible` filter —
                // a near-empty stub/placeholder book isn't a real, in-progress manuscript
                // yet, and would otherwise generate a permanent, meaningless daily
                // below-length-floor finding. Checked post-scan (via the report's own
                // BeatCount) rather than pre-filtering the book list, since ScanAsync's own
                // leaf-descendant walk is the authoritative beat count — recomputing it
                // separately here would risk drifting out of sync with what ScanAsync
                // actually counted.
                if (report.BeatCount <= 2) continue;

                scannedCount++;
                SanityScanService.FileFindings(findingsSvc, book.Slug, report);
                foreach (var f in report.Findings)
                    if (f.Severity == "block") blockCount++; else warnCount++;
            }
            catch (Exception ex)
            {
                // One book's failure (e.g. mid-split, transient DB issue) must not abort
                // the whole corpus sweep.
                log.LogWarning(ex, "Sanity-scan sweep: book {Slug} failed, skipping", book.Slug);
            }
        }

        LastSweepAt         = DateTime.UtcNow;
        LastSweepBookCount  = scannedCount;
        LastSweepBlockCount = blockCount;
        LastSweepWarnCount  = warnCount;
        log.LogInformation(
            "Sanity-scan background sweep: {BookCount} book(s) scanned (of {TotalCount} total), {BlockCount} block(s), {WarnCount} warn(s)",
            scannedCount, books.Count, blockCount, warnCount);
    }

    private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken ct)
    {
        try { return await timer.WaitForNextTickAsync(ct); }
        catch (OperationCanceledException) { return false; }
    }
}
