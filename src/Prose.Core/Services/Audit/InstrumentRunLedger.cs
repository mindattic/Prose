using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services.Audit;

/// <summary>
/// Write and read <see cref="InstrumentRun"/> rows — the publish gate's answer to "did this
/// instrument ever actually look at this book?"
///
/// <para>Every gate instrument stamps one row when it completes. The gate reads the newest row per
/// (node, instrument) and refuses to report "clean" when there isn't one. See
/// <see cref="InstrumentRun"/> for why this exists; the short version is that a Findings count of
/// zero has two completely different meanings and the gate was only ever reporting one of them.</para>
/// </summary>
public class InstrumentRunLedger(IDbContextFactory<ProseDbContext> dbFactory)
{
    // Instrument keys. Strings, not an enum: an instrument that gets commented out pending review
    // (the standing verdict policy) must leave its history readable rather than break the build.
    public const string LogicSweep        = "logic-sweep";
    public const string BlastRadius       = "blast-radius";
    public const string ReaderQaProbes    = "reader-qa-comprehension";
    public const string ReaderQaGripe     = "reader-qa-gripe";
    public const string ReaderQaFullOrder = "reader-qa-full-order-read";
    public const string ContinuityExtract = "continuity-extract";
    public const string TunedRead         = "tuned-read";
    public const string ObligationScan    = "obligation-scan";

    /// <summary>
    /// Record a completed run. Call this on the way out of the instrument, whatever it found —
    /// including when it found nothing, which is the entire point.
    ///
    /// <para><paramref name="itemsExamined"/> is what the instrument actually READ, not what it
    /// was asked to read. Zero means it could not look. Fewer than
    /// <paramref name="itemsTotal"/> means a partial run, which is void rather than clean.</para>
    /// </summary>
    public async Task RecordAsync(
        Guid nodeId, string instrument, int itemsExamined, int itemsTotal,
        string? bookFingerprint = null, int findingsFiled = 0, string? detail = null,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        db.InstrumentRuns.Add(new InstrumentRun
        {
            NodeId = nodeId,
            Instrument = instrument,
            CompletedAt = DateTime.UtcNow,
            ItemsExamined = itemsExamined,
            ItemsTotal = itemsTotal,
            BookFingerprint = bookFingerprint,
            FindingsFiled = findingsFiled,
            Detail = detail?.Length > 1000 ? detail[..1000] : detail,
        });
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Newest run of one instrument on one book, or null if it has never run there.</summary>
    public async Task<InstrumentRun?> LastRunAsync(Guid nodeId, string instrument, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await LastRunAsync(db, nodeId, instrument, ct);
    }

    /// <summary>Overload for a caller that already has a context open — the gate reads several of
    /// these in a row and has no reason to spin up a context per check.</summary>
    public static Task<InstrumentRun?> LastRunAsync(
        ProseDbContext db, Guid nodeId, string instrument, CancellationToken ct = default) =>
        db.InstrumentRuns.AsNoTracking()
            .Where(r => r.NodeId == nodeId && r.Instrument == instrument)
            .OrderByDescending(r => r.CompletedAt)
            .FirstOrDefaultAsync(ct);

    /// <summary>
    /// The gate's question, as a pure decision so it is testable without a database: given the
    /// newest run (or none) and the count of open findings that run would have filed, did this
    /// instrument look, and is the book clean?
    ///
    /// <para>Order matters. "Never ran" and "ran but read nothing" both outrank the finding count,
    /// because a zero from an instrument that did not look is not evidence of anything. A stale
    /// run — one whose fingerprint no longer matches the book — is reported as such rather than
    /// trusted, since the findings it filed describe prose that has since changed.</para>
    /// </summary>
    public static (CheckOutcome Outcome, string Detail) Evaluate(
        string instrumentLabel, InstrumentRunSummary? run, int openFindings, string remediation,
        string? currentFingerprint = null)
    {
        if (run is null)
            return (CheckOutcome.CouldNotLook,
                $"COULD NOT LOOK — {instrumentLabel} has never run on this book — {remediation}");

        if (run.ItemsExamined == 0)
            return (CheckOutcome.CouldNotLook,
                $"COULD NOT LOOK — {instrumentLabel} last ran {run.CompletedAt:yyyy-MM-dd} but read " +
                $"0 of {run.ItemsTotal} — {remediation}");

        if (run.ItemsExamined < run.ItemsTotal)
            return (CheckOutcome.CouldNotLook,
                $"COULD NOT LOOK — {instrumentLabel} read only {run.ItemsExamined} of {run.ItemsTotal} " +
                $"on {run.CompletedAt:yyyy-MM-dd}; a partial run is void — {remediation}");

        if (currentFingerprint != null && run.BookFingerprint != null &&
            run.BookFingerprint != currentFingerprint)
            return (CheckOutcome.CouldNotLook,
                $"COULD NOT LOOK — {instrumentLabel} last ran {run.CompletedAt:yyyy-MM-dd} against " +
                $"different prose; the book has changed since — {remediation}");

        return openFindings == 0
            ? (CheckOutcome.Pass, $"clean — {instrumentLabel} read {run.ItemsExamined}/{run.ItemsTotal} on {run.CompletedAt:yyyy-MM-dd}")
            : (CheckOutcome.Fail, $"{openFindings} open finding(s) from {instrumentLabel}");
    }

    /// <summary>The fields <see cref="Evaluate"/> needs, so the decision can be unit-tested
    /// without constructing an EF entity or a database.</summary>
    public sealed record InstrumentRunSummary(
        DateTime CompletedAt, int ItemsExamined, int ItemsTotal, string? BookFingerprint)
    {
        public static InstrumentRunSummary? From(InstrumentRun? r) =>
            r is null ? null : new(r.CompletedAt, r.ItemsExamined, r.ItemsTotal, r.BookFingerprint);
    }
}
