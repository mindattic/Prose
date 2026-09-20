using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services.Obligations;

namespace Prose.Core.Services.Audit;

/// <summary>
/// The obligation trial balance as an instrument (RFC 0013 D6a): six deterministic, free rules
/// over the ledger, filed through <see cref="AuditRunner"/> under
/// <see cref="FindingCategory.NarrativeObligation"/> and scope <c>node:{slug}#obligations</c>, so
/// its delete-then-recreate cycle never touches another instrument's rows.
///
/// <para>Deliberate versus abandoned mystery is decided by a ledger row, never by prose language:
/// a Deferred row, or a book-end row the author declared, is not flagged before the book ends.
/// An empty ledger prints COULD NOT LOOK and files nothing — it fails, it does not pass.</para>
/// </summary>
public class ObligationReconciliationService(
    IDbContextFactory<ProseDbContext> dbFactory,
    NarrativeObligationService obligations,
    AuditRunner auditRunner,
    ILogger<ObligationReconciliationService> log,
    ObligationResurfacingJudge? judge = null)
{
    public const string AuditName = "OBLIGATION";
    public const string InstrumentVersion = "recon-v1";

    public sealed record Report(
        Guid NodeId, string Slug, string Title,
        int Examined, int TotalBeats, int ScannedBeats, bool CouldNotLook, bool BookAtEnd,
        NarrativeObligationService.TrialBalance Balance,
        IReadOnlyDictionary<string, int> RuleCounts,
        IReadOnlyList<AuditVerdict> Verdicts,
        ObligationResurfacingJudge.DeepResult? Deep,
        NarrativeHealthSnapshot? Snapshot);

    public async Task<Report> RunAsync(Guid bookNodeId, bool deep = false, bool writeFindings = true, CancellationToken ct = default)
    {
        ObligationResurfacingJudge.DeepResult? deepResult = null;
        if (deep && judge != null)
        {
            // Judge first so a payoff the extractor missed is closed before the balance is struck.
            deepResult = await judge.RunAsync(bookNodeId, ct);
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.AsNoTracking().IgnoreQueryFilters().Where(n => n.Id == bookNodeId)
            .Select(n => new { n.Slug, n.Title, n.Status, n.KdpPublishedAt, n.PublicationStatus, n.UniverseId }).FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"Node {bookNodeId} not found.");
        var slug = node.Slug ?? bookNodeId.ToString("N");
        var atEnd = IsBookAtEnd(node.Status, node.PublicationStatus, node.KdpPublishedAt);

        var balance = await obligations.TrialBalanceAsync(bookNodeId, null, ct);
        var rows = await db.NarrativeObligations.AsNoTracking().Where(o => o.NodeId == bookNodeId).ToListAsync(ct);
        var clock = await NarrativeObligationService.LoadClockAsync(db, bookNodeId, ct);

        if (balance.CouldNotLook)
        {
            log.LogWarning("Obligation reconciliation {Slug}: COULD NOT LOOK (beats={Beats}, scanned={Scanned}, rows={Rows})", slug, balance.TotalBeats, balance.ScannedBeats, rows.Count);
            return new Report(bookNodeId, slug, node.Title ?? slug, 0, balance.TotalBeats, balance.ScannedBeats, true, atEnd, balance,
                new Dictionary<string, int>(), [], deepResult, null);
        }

        var payoffs = await db.PlantPayoffs.AsNoTracking().Where(p => p.PayoffBeatId != null && p.PlantBeatId == null).ToListAsync(ct);
        var bookPayoffs = payoffs.Where(p => clock.Beats.ContainsKey(p.PayoffBeatId!.Value)).ToList();

        var rules = new IDeterministicAuditRule[]
        {
            new OverdueOpenRule(balance, clock),
            new OpenAtEndRule(rows, clock, atEnd),
            new StaleClosureRule(balance),
            new DanglingBeatRule(rows, clock),
            new UnplantedPayoffRule(bookPayoffs, clock),
            new DeferredExpiredRule(rows, clock, balance.TotalBeats),
        };
        var ctx = new AuditContext(bookNodeId, node.UniverseId, "", [], new Dictionary<string, object?>());
        var verdicts = await auditRunner.RunAsync(AuditName, $"node:{slug}#obligations", FindingCategory.NarrativeObligation, rules, ctx, writeFindings, ct);

        var counts = rules.ToDictionary(r => r.Key, r => verdicts.Count(v => v.RuleKey == r.Key && v.Severity != "PASS"));
        var examined = rows.Count(r => r.State != ObligationState.Withdrawn);

        NarrativeHealthSnapshot? snapshot = null;
        if (writeFindings && examined > 0)
        {
            snapshot = await WriteSnapshotAsync(db, bookNodeId, slug, clock, balance, ct);
        }

        log.LogInformation("Obligation reconciliation {Slug}: examined {Examined} obligations over {Beats} beats — {Counts}",
            slug, examined, balance.TotalBeats, string.Join(", ", counts.Select(kv => $"{kv.Key}={kv.Value}")));

        return new Report(bookNodeId, slug, node.Title ?? slug, examined, balance.TotalBeats, balance.ScannedBeats, false, atEnd, balance, counts, verdicts, deepResult, snapshot);
    }

    public static bool IsBookAtEnd(string? status, string? publicationStatus, DateTime? kdpPublishedAt) =>
        kdpPublishedAt != null
        || (status?.Contains("complete", StringComparison.OrdinalIgnoreCase) ?? false)
        || (status?.Contains("publish", StringComparison.OrdinalIgnoreCase) ?? false)
        || (publicationStatus?.Contains("publish", StringComparison.OrdinalIgnoreCase) ?? false);

    // ── snapshot ──────────────────────────────────────────────────────────────

    private async Task<NarrativeHealthSnapshot> WriteSnapshotAsync(ProseDbContext db, Guid bookNodeId, string slug,
        NarrativeObligationService.BookClock clock, NarrativeObligationService.TrialBalance tb, CancellationToken ct)
    {
        var beatIds = clock.Beats.OrderBy(kv => kv.Value.Position).Select(kv => kv.Key).ToList();
        var beats = await db.Beats.AsNoTracking().Where(b => beatIds.Contains(b.Id)).Select(b => new { b.Id, b.Text, b.TextHash }).ToListAsync(ct);
        var ordered = beatIds.Select(id => beats.FirstOrDefault(b => b.Id == id)).Where(b => b != null).ToList();

        var words = ordered.Sum(b => CountWords(BeatMarkup.StripEntityTags(b!.Text)));
        var bookHash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(string.Join("|", ordered.Select(b => b!.TextHash ?? "")))));

        var referents = ordered.Sum(b => UnnamedReferentScanner.Scan(BeatMarkup.StripEntityTags(b!.Text)).Count(r => r.Mentions >= 2));

        var prefix = $"node:{slug}";
        var openBad = await db.Findings.AsNoTracking()
            .Where(f => f.FilePath.StartsWith(prefix) && (f.Status == "New" || f.Status == "Triaged") && (f.Severity == "High" || f.Severity == "Medium"))
            .Select(f => f.Summary).ToListAsync(ct);
        var consistency = openBad.Count(s => s.StartsWith("LOGICSWEEP ", StringComparison.Ordinal) || s.StartsWith("FACT-LEDGER ", StringComparison.Ordinal)
                                          || s.StartsWith("TUNEDREAD ", StringComparison.Ordinal) || s.StartsWith("OBLIGATION ", StringComparison.Ordinal));

        var lastGrounding = await db.NarrativeHealthSnapshots.AsNoTracking().Where(s => s.NodeId == bookNodeId && s.UnentailedRecordClaimPct != null)
            .OrderByDescending(s => s.TakenAt).Select(s => s.UnentailedRecordClaimPct).FirstOrDefaultAsync(ct);

        var per10k = words > 0 ? 10_000.0 / words : 0;
        var overdue = tb.OverdueWithoutDecision.Count;
        var snap = new NarrativeHealthSnapshot
        {
            NodeId = bookNodeId, BookTextHash = bookHash, WordCount = words, TotalBeats = tb.TotalBeats, ExaminedBeats = tb.ScannedBeats,
            Open = tb.CarriedForward, Overdue = overdue, Closed = tb.Closed, Dropped = tb.Dropped, Deferred = tb.Deferred,
            PayoffCoveragePct = tb.Closed + overdue == 0 ? 1.0 : (double)tb.Closed / (tb.Closed + overdue),
            UnnamedReferentDensity10k = referents * per10k,
            ConsistencyErrorDensity10k = consistency * per10k,
            UnentailedRecordClaimPct = lastGrounding,
            InstrumentVersion = InstrumentVersion,
        };
        db.NarrativeHealthSnapshots.Add(snap);
        await db.SaveChangesAsync(ct);
        return snap;
    }

    public static int CountWords(string? text) =>
        string.IsNullOrWhiteSpace(text) ? 0 : text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    // ── rules ─────────────────────────────────────────────────────────────────

    private static string Describe(NarrativeObligationService.ObligationView v, NarrativeObligationService.BookClock clock, int laterBeats) =>
        $"[{v.Kind}] \"{Trunc(v.Description, 120)}\" — set up Ch{v.OriginChapter}" +
        (v.OriginQuote is null ? "" : $", quote \"{Trunc(v.OriginQuote, 100)}\"") +
        (laterBeats > 0 ? $", {laterBeats} later beat(s), never addressed" : "") +
        $" (due {v.Due}, {v.Provenance})";

    private static string Trunc(string s, int max) => s.Length <= max ? s : s[..max] + "…";

    sealed class OverdueOpenRule(NarrativeObligationService.TrialBalance tb, NarrativeObligationService.BookClock clock) : IDeterministicAuditRule
    {
        public string Key => "overdue_open";
        public string Title => "Obligation past due with no author decision";
        public Task<IReadOnlyList<AuditVerdict>> EvaluateAsync(AuditContext ctx, CancellationToken ct)
        {
            var list = new List<AuditVerdict>();
            foreach (var v in tb.OverdueWithoutDecision)
            {
                var originPos = v.OriginBeatId is Guid ob ? clock.PositionOf(ob) : -1;
                var later = originPos >= 0 ? clock.BeatCount - originPos - 1 : 0;
                var earlyReferent = (v.Kind is ObligationKind.IntroducedReferent or ObligationKind.Question)
                                    && originPos >= 0 && originPos <= clock.BeatCount * 0.15 && v.State == ObligationState.Open;
                list.Add(new AuditVerdict(Key, Title, earlyReferent ? "BLOCKER" : "MODERATE", Describe(v, clock, later), v.OriginBeatId?.ToString("D")));
            }
            return Task.FromResult<IReadOnlyList<AuditVerdict>>(list);
        }
    }

    sealed class OpenAtEndRule(List<NarrativeObligation> rows, NarrativeObligationService.BookClock clock, bool atEnd) : IDeterministicAuditRule
    {
        public string Key => "open_at_end";
        public string Title => "Book-end obligation still open on a complete or published book";
        public Task<IReadOnlyList<AuditVerdict>> EvaluateAsync(AuditContext ctx, CancellationToken ct)
        {
            if (!atEnd) return Task.FromResult<IReadOnlyList<AuditVerdict>>([]);
            var list = rows
                .Where(o => ObligationState.IsOutstanding(o.State) && !o.AuthorLocked && o.DueByKind == ObligationDueKind.BookEnd)
                .Select(o => new AuditVerdict(Key, Title, "MODERATE",
                    $"[{o.Kind}] \"{Trunc(o.Description, 120)}\" — opened Ch{(o.OriginBeatId is Guid ob ? clock.ChapterOf(ob) : 0)}, still open at the end of a complete book",
                    o.OriginBeatId?.ToString("D")))
                .ToList();
            return Task.FromResult<IReadOnlyList<AuditVerdict>>(list);
        }
    }

    sealed class StaleClosureRule(NarrativeObligationService.TrialBalance tb) : IDeterministicAuditRule
    {
        public string Key => "stale_closure";
        public string Title => "Closure evidence no longer on the page";
        public Task<IReadOnlyList<AuditVerdict>> EvaluateAsync(AuditContext ctx, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<AuditVerdict>>(tb.StaleClosures.Select(v => new AuditVerdict(Key, Title, "MODERATE",
                $"[{v.Kind}] \"{Trunc(v.Description, 120)}\" — marked Closed in Ch{v.ClosingChapter} but the closing quote \"{Trunc(v.ClosingQuote ?? "", 100)}\" is no longer in that beat",
                v.ClosingBeatId?.ToString("D"))).ToList());
    }

    sealed class DanglingBeatRule(List<NarrativeObligation> rows, NarrativeObligationService.BookClock clock) : IDeterministicAuditRule
    {
        public string Key => "dangling_beat";
        public string Title => "Obligation anchored to a deleted beat";
        public Task<IReadOnlyList<AuditVerdict>> EvaluateAsync(AuditContext ctx, CancellationToken ct)
        {
            var list = new List<AuditVerdict>();
            foreach (var o in rows.Where(o => o.State != ObligationState.Withdrawn && o.State != ObligationState.Dropped))
            {
                if (o.OriginBeatId is Guid ob && !clock.Beats.ContainsKey(ob))
                    list.Add(new AuditVerdict(Key, Title, "MINOR", $"[{o.Kind}] \"{Trunc(o.Description, 120)}\" — origin beat is gone", ob.ToString("D")));
                if (o.ClosingBeatId is Guid cb && !clock.Beats.ContainsKey(cb))
                    list.Add(new AuditVerdict(Key, Title, "MODERATE", $"[{o.Kind}] \"{Trunc(o.Description, 120)}\" — closing beat is gone; the payoff no longer exists", cb.ToString("D")));
                if (o.OriginBeatId == null && o.Provenance == ClaimProvenance.Observed && ObligationState.IsOutstanding(o.State))
                    list.Add(new AuditVerdict(Key, Title, "MINOR", $"[{o.Kind}] \"{Trunc(o.Description, 120)}\" — observed row with no origin beat (cleared on delete)", null));
            }
            return Task.FromResult<IReadOnlyList<AuditVerdict>>(list);
        }
    }

    sealed class UnplantedPayoffRule(List<PlantPayoff> unplanted, NarrativeObligationService.BookClock clock) : IDeterministicAuditRule
    {
        public string Key => "unplanted_payoff";
        public string Title => "Payoff with no plant on record (LOGIC.md §3.4, reverse direction)";
        public Task<IReadOnlyList<AuditVerdict>> EvaluateAsync(AuditContext ctx, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<AuditVerdict>>(unplanted.Select(p => new AuditVerdict(Key, Title, "MODERATE",
                $"payoff \"{Trunc(p.PayoffDescription, 120)}\" is linked to a beat in Ch{clock.ChapterOf(p.PayoffBeatId!.Value)} but its plant \"{Trunc(p.PlantDescription, 80)}\" has no beat",
                p.PayoffBeatId!.Value.ToString("D"))).ToList());
    }

    sealed class DeferredExpiredRule(List<NarrativeObligation> rows, NarrativeObligationService.BookClock clock, int totalBeats) : IDeterministicAuditRule
    {
        public string Key => "deferred_expired";
        public string Title => "Deferred obligation past its new due point";
        public Task<IReadOnlyList<AuditVerdict>> EvaluateAsync(AuditContext ctx, CancellationToken ct)
        {
            var list = new List<AuditVerdict>();
            foreach (var o in rows.Where(o => o.State == ObligationState.Deferred))
            {
                var expired = o.DueByKind switch
                {
                    ObligationDueKind.Chapter => o.DueByValue is int ch && clock.ChapterCount > ch,
                    ObligationDueKind.Beats   => o.DueByValue is int n && o.OriginBeatId is Guid ob && clock.PositionOf(ob) >= 0 && totalBeats - 1 > clock.PositionOf(ob) + n,
                    _ => false,
                };
                if (expired)
                    list.Add(new AuditVerdict(Key, Title, "MINOR", $"[{o.Kind}] \"{Trunc(o.Description, 120)}\" — deferred to {o.DueByKind} {o.DueByValue}, which has passed", o.OriginBeatId?.ToString("D")));
            }
            return Task.FromResult<IReadOnlyList<AuditVerdict>>(list);
        }
    }
}
