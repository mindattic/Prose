using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services.Audit;
using Prose.Core.Services.Obligations;

namespace Prose.Core.Services.Calibration;

/// <summary>
/// The validation harness for the obligation instruments (RFC 0013 D7), built the way this repo
/// validates every instrument: measure the false-positive floor on a seeded negative control, run
/// the slow-burn case, prove the known-real defects are flagged, and score precision/recall on
/// INJECTED defects — the design that let <i>Lost in Stories</i>' checker be trusted over unaided
/// human annotators (F1 0.678 vs 0.281 on 1,000 injected errors).
///
/// <para>Injection appends a seeded synthetic sentence with a noun absent from the text to a beat
/// in the first 60% of a <c>gutenberg</c>-universe book ("abandoned"), or the same plus a payoff
/// sentence ten or more beats later ("resolved"). Every write goes through the one beat-text door
/// with <see cref="BeatWriteReason.Calibration"/>; the prior text is stored so the revert is
/// byte-exact. <b>Any node outside the gutenberg universe throws before the first write.</b></para>
/// </summary>
public class ObligationCalibrationService(
    IDbContextFactory<ProseDbContext> dbFactory,
    NodeWorkbenchService workbench,
    NarrativeObligationService obligations,
    ObligationReconciliationService reconciler,
    ILogger<ObligationCalibrationService> log)
{
    public const string CalibrationUniverseSlug = "gutenberg";

    // Nouns chosen to be absent from Doyle and Dickens. Each abandoned setup has a payoff twin.
    private static readonly (string Setup, string Payoff)[] Templates =
    [
        ("A girl in a grey shawl watched from behind the curtain and did not move when he looked at her.",
         "The girl in the grey shawl was the landlady's niece, sent to count the visitors, and he paid her a shilling for her silence."),
        ("On the mantel lay a brass whistle nobody in the room would admit to owning.",
         "He put the brass whistle to his lips at last; it was the signal the coachman had been waiting for since morning."),
        ("A boy with a lantern stood at the corner and followed them with his eyes as far as the turning.",
         "The boy with the lantern proved to be the constable's son, posted there to mark who came and went."),
        ("Someone had left a single tin soldier on the third step, its bayonet pointing at the door.",
         "The tin soldier had been the child's, set on the step to mark the room where the letters were kept."),
        ("The letter carried a violet seal that none of them recognised, and no one remarked upon it.",
         "The violet seal was the mark of the Hargreave estate, which explained at once who had sent the letter."),
        ("A one-eyed cat sat on the wall and watched the house with unusual attention.",
         "The one-eyed cat belonged to the gardener, who had trained it to follow him and so gave away his nightly route."),
        ("In the hall a clock had been stopped at a quarter past three, and nobody set it going again.",
         "The clock had been stopped at a quarter past three by the widow herself, at the hour of the telegram."),
        ("A woman in a tan coat asked the porter a question, received no answer, and walked away without looking back.",
         "The woman in the tan coat was the missing governess, who had come to see for herself whether the house was watched."),
    ];

    public sealed record Injected(Guid Id, Guid BeatId, string Kind, string Sentence, Guid? PayoffBeatId);
    public sealed record InjectResult(Guid NodeId, int Seed, IReadOnlyList<Injected> Injections);

    public sealed record Score(
        Guid NodeId, int Injected, int Abandoned, int Resolved,
        int TruePositives, int FalseNegatives, int ResolvedMisflagged, int ControlFindingsModeratePlus,
        int WordCount, double Precision, double Recall, double F1, double ControlFalsePositivesPer10k,
        IReadOnlyList<string> Details)
    {
        public bool MeetsBar(double minPrecision = 0.85, double minRecall = 0.70, double minF1 = 0.678, double maxControlPer10k = 1.0, double maxResolvedMisflagRate = 0.10) =>
            Precision >= minPrecision && Recall >= minRecall && F1 >= minF1
            && ControlFalsePositivesPer10k <= maxControlPer10k
            && (Resolved == 0 || (double)ResolvedMisflagged / Resolved <= maxResolvedMisflagRate);
    }

    public async Task<InjectResult> InjectAsync(Guid bookNodeId, int count, int seed, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await GuardAsync(db, bookNodeId, ct);
        if (await db.CalibrationInjections.AnyAsync(i => i.NodeId == bookNodeId, ct))
            throw new InvalidOperationException("This book already carries injections — run --revert-calibration-defects first.");

        var clock = await NarrativeObligationService.LoadClockAsync(db, bookNodeId, ct);
        var ordered = clock.Beats.OrderBy(kv => kv.Value.Position).Select(kv => kv.Key).ToList();
        if (ordered.Count < 20) throw new InvalidOperationException("Calibration needs a book of at least 20 beats.");

        var rng = new Random(seed);
        var eligible = ordered.Take((int)(ordered.Count * 0.6)).ToList();
        var chosen = eligible.OrderBy(_ => rng.Next()).Take(Math.Min(count, Math.Min(eligible.Count, Templates.Length))).ToList();

        var injections = new List<Injected>();
        for (var i = 0; i < chosen.Count; i++)
        {
            var beatId = chosen[i];
            var template = Templates[i % Templates.Length];
            var kind = i % 2 == 0 ? "abandoned" : "resolved";
            var prior = await db.Beats.AsNoTracking().Where(b => b.Id == beatId).Select(b => b.Text).FirstAsync(ct) ?? "";

            var row = new CalibrationInjection { NodeId = bookNodeId, BeatId = beatId, PriorText = prior, Sentence = template.Setup, Kind = kind, Seed = seed };
            if (kind == "resolved")
            {
                var pos = clock.PositionOf(beatId);
                var payoffCandidates = ordered.Skip(pos + 10).ToList();
                if (payoffCandidates.Count == 0) { kind = "abandoned"; row.Kind = kind; }
                else
                {
                    var payoffId = payoffCandidates[rng.Next(payoffCandidates.Count)];
                    row.PayoffBeatId = payoffId;
                    row.PayoffPriorText = await db.Beats.AsNoTracking().Where(b => b.Id == payoffId).Select(b => b.Text).FirstAsync(ct) ?? "";
                    row.PayoffSentence = template.Payoff;
                }
            }
            db.CalibrationInjections.Add(row);
            await db.SaveChangesAsync(ct);

            await workbench.UpdateBeatTextAsync(beatId, prior.TrimEnd() + "\n\n" + template.Setup, BeatWriteReason.Calibration, deferAnalysis: true, ct: ct);
            if (row.PayoffBeatId is Guid pb)
                await workbench.UpdateBeatTextAsync(pb, (row.PayoffPriorText ?? "").TrimEnd() + "\n\n" + template.Payoff, BeatWriteReason.Calibration, deferAnalysis: true, ct: ct);

            injections.Add(new Injected(row.Id, beatId, kind, template.Setup, row.PayoffBeatId));
        }
        log.LogInformation("Calibration: injected {Count} defect(s) into {Node} (seed {Seed})", injections.Count, bookNodeId, seed);
        return new InjectResult(bookNodeId, seed, injections);
    }

    public async Task<int> RevertAsync(Guid bookNodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await GuardAsync(db, bookNodeId, ct);
        var rows = await db.CalibrationInjections.Where(i => i.NodeId == bookNodeId).OrderByDescending(i => i.CreatedAt).ToListAsync(ct);
        foreach (var r in rows)
        {
            await workbench.UpdateBeatTextAsync(r.BeatId, r.PriorText, BeatWriteReason.Calibration, deferAnalysis: true, ct: ct);
            if (r.PayoffBeatId is Guid pb && r.PayoffPriorText != null)
                await workbench.UpdateBeatTextAsync(pb, r.PayoffPriorText, BeatWriteReason.Calibration, deferAnalysis: true, ct: ct);
            // Remove ledger rows the injection created (unlocked, observed, anchored at the injected beat).
            var injectedBeats = new List<Guid> { r.BeatId };
            if (r.PayoffBeatId is Guid p2) injectedBeats.Add(p2);
            var created = await db.NarrativeObligations
                .Where(o => o.NodeId == bookNodeId && o.OriginBeatId != null && injectedBeats.Contains(o.OriginBeatId.Value) && !o.AuthorLocked && o.Provenance == ClaimProvenance.Observed)
                .ToListAsync(ct);
            db.NarrativeObligations.RemoveRange(created);
        }
        db.CalibrationInjections.RemoveRange(rows);
        await db.Beats.Where(b => rows.Select(r => r.BeatId).Contains(b.Id) || rows.Where(r => r.PayoffBeatId != null).Select(r => r.PayoffBeatId!.Value).Contains(b.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.ObligationScanHash, (string?)null), ct);
        await db.SaveChangesAsync(ct);
        return rows.Count;
    }

    /// <summary>Drop every machine-produced ledger row for the calibration book, and clear the scan
    /// stamps, so the next rescan rebuilds the ledger from nothing.
    ///
    /// <para>Without this a run inherits the previous run's verdicts and is not an independent
    /// measurement. GCSH runs 1–4 (2026-09-15/16) were all contaminated this way: run 3's judge
    /// closed the injected "stopped clock" plant on an unrelated quote, and run 4 then scored that
    /// same row as a miss — its close event was still run 3's, hours old, with no run-4 event on it
    /// at all. A false close is terminal, because <see cref="ObligationResurfacingJudge"/> only
    /// revisits Open/Advanced rows, so nothing could ever correct it.</para>
    ///
    /// <para>Authored and author-locked rows are human ground truth, not instrument output, and are
    /// never touched. Beat text and the <see cref="CalibrationInjection"/> manifest are untouched
    /// too — the injections live in the prose, not in the ledger.</para></summary>
    public async Task<int> ResetLedgerAsync(Guid bookNodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await GuardAsync(db, bookNodeId, ct);
        var clock = await NarrativeObligationService.LoadClockAsync(db, bookNodeId, ct);
        return await ResetLedgerAsync(db, bookNodeId, clock, ct);
    }

    private async Task<int> ResetLedgerAsync(ProseDbContext db, Guid bookNodeId, NarrativeObligationService.BookClock clock, CancellationToken ct)
    {
        var doomed = await db.NarrativeObligations
            .Where(o => o.NodeId == bookNodeId && !o.AuthorLocked && o.Provenance != ClaimProvenance.Authored)
            .Select(o => o.Id)
            .ToListAsync(ct);
        if (doomed.Count > 0)
        {
            // Children first: judge verdicts and journal events both point at the obligation.
            await db.ObligationJudgeCache.Where(c => doomed.Contains(c.ObligationId)).ExecuteDeleteAsync(ct);
            await db.NarrativeObligationEvents.Where(e => doomed.Contains(e.ObligationId)).ExecuteDeleteAsync(ct);
            await db.NarrativeObligations.Where(o => doomed.Contains(o.Id)).ExecuteDeleteAsync(ct);
        }
        // The stamps must go too: ScanBeatAsync short-circuits on a current ObligationScanHash, so
        // an un-stamped reset would rescan nothing and score an empty ledger.
        var beatIds = clock.Beats.Keys.ToList();
        await db.Beats.Where(b => beatIds.Contains(b.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.ObligationScanHash, (string?)null), ct);
        log.LogInformation("Calibration: ledger reset for {Node} — dropped {Rows} machine row(s), cleared {Beats} scan stamp(s)", bookNodeId, doomed.Count, beatIds.Count);
        return doomed.Count;
    }

    /// <summary>Rescan the whole book (synchronously, in reading order), reconcile, and score the
    /// instrument against the injection manifest. Findings are written so the run is auditable.
    /// The ledger is reset first (<see cref="ResetLedgerAsync(Guid, CancellationToken)"/>) so the
    /// score measures this instrument, not the residue of every run before it.</summary>
    public async Task<Score> ScoreAsync(Guid bookNodeId, bool deep, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await GuardAsync(db, bookNodeId, ct);
        var injections = await db.CalibrationInjections.AsNoTracking().Where(i => i.NodeId == bookNodeId).ToListAsync(ct);
        var clock = await NarrativeObligationService.LoadClockAsync(db, bookNodeId, ct);

        // 0. Clean slate — every run is an independent measurement.
        await ResetLedgerAsync(db, bookNodeId, clock, ct);

        // 1. Rescan in reading order so each beat sees the running ledger.
        foreach (var beatId in clock.Beats.OrderBy(kv => kv.Value.Position).Select(kv => kv.Key))
        {
            var text = await db.Beats.AsNoTracking().Where(b => b.Id == beatId).Select(b => b.Text).FirstOrDefaultAsync(ct);
            if (text == null) continue;
            await obligations.ScanBeatAsync(bookNodeId, beatId, BeatMarkup.StripEntityTags(text), ObligationActor.SystemRescan, ct);
        }

        // 2. Reconcile (+ deep judge).
        var report = await reconciler.RunAsync(bookNodeId, deep, writeFindings: true, ct);

        // 3. Score.
        var rows = await db.NarrativeObligations.AsNoTracking().Where(o => o.NodeId == bookNodeId).ToListAsync(ct);
        var injectedBeatIds = injections.Select(i => i.BeatId).Concat(injections.Where(i => i.PayoffBeatId != null).Select(i => i.PayoffBeatId!.Value)).ToHashSet();
        var details = new List<string>();
        int tp = 0, fn = 0, resolvedMisflagged = 0;

        foreach (var inj in injections)
        {
            var match = rows.FirstOrDefault(o => o.OriginBeatId == inj.BeatId && o.OriginQuote != null
                && (QuoteGrounding.Contains(inj.Sentence, o.OriginQuote) || QuoteGrounding.Contains(o.OriginQuote, inj.Sentence.Split('.')[0])));
            if (inj.Kind == "abandoned")
            {
                var flagged = match != null && ObligationState.IsOutstanding(match.State)
                              && report.Verdicts.Any(v => v.RuleKey is "overdue_open" or "open_at_end" && v.Location == inj.BeatId.ToString("D"));
                // An abandoned injection also counts as found when the ledger holds it Open past due
                // even if the rule did not fire yet (e.g. book not at end and due=book-end).
                var found = match != null && ObligationState.IsOutstanding(match.State);
                if (found || flagged) { tp++; details.Add($"TP  abandoned @ {inj.BeatId:N} — \"{Trunc(inj.Sentence, 60)}\" → {match!.State}{(flagged ? " (flagged)" : "")}"); }
                else { fn++; details.Add($"FN  abandoned @ {inj.BeatId:N} — \"{Trunc(inj.Sentence, 60)}\" → {(match == null ? "no row" : match.State)}"); }
            }
            else
            {
                var stillOpen = match != null && ObligationState.IsOutstanding(match.State);
                if (stillOpen) { resolvedMisflagged++; details.Add($"FP  resolved  @ {inj.BeatId:N} — payoff at {inj.PayoffBeatId:N} not recognised; row {match!.State}"); }
                else details.Add($"ok  resolved  @ {inj.BeatId:N} — {(match == null ? "no row (never opened)" : match.State)}");
            }
        }

        // Control false positives: MODERATE+ verdicts whose origin is NOT an injected beat.
        var control = report.Verdicts.Count(v => v.Severity is "BLOCKER" or "MODERATE"
            && (v.Location == null || !Guid.TryParse(v.Location, out var loc) || !injectedBeatIds.Contains(loc)));
        var words = report.Snapshot?.WordCount ?? await WordCountAsync(db, clock, ct);
        var per10k = words > 0 ? control * 10_000.0 / words : 0;

        var precisionDen = tp + resolvedMisflagged + control;
        var precision = precisionDen == 0 ? 1.0 : (double)tp / precisionDen;
        var recall = tp + fn == 0 ? 1.0 : (double)tp / (tp + fn);
        var f1 = precision + recall == 0 ? 0 : 2 * precision * recall / (precision + recall);

        return new Score(bookNodeId, injections.Count, injections.Count(i => i.Kind == "abandoned"), injections.Count(i => i.Kind == "resolved"),
            tp, fn, resolvedMisflagged, control, words, precision, recall, f1, per10k, details);
    }

    private static async Task GuardAsync(ProseDbContext db, Guid bookNodeId, CancellationToken ct)
    {
        var universeId = await db.Nodes.AsNoTracking().IgnoreQueryFilters().Where(n => n.Id == bookNodeId).Select(n => n.UniverseId).FirstOrDefaultAsync(ct);
        var slug = await db.Universes.AsNoTracking().Where(u => u.Id == universeId).Select(u => u.Slug).FirstOrDefaultAsync(ct);
        if (!string.Equals(slug, CalibrationUniverseSlug, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Calibration is confined to the '{CalibrationUniverseSlug}' universe; node {bookNodeId} is in '{slug ?? "?"}'. Refusing to write.");
    }

    private static async Task<int> WordCountAsync(ProseDbContext db, NarrativeObligationService.BookClock clock, CancellationToken ct)
    {
        var ids = clock.Beats.Keys.ToList();
        var texts = await db.Beats.AsNoTracking().Where(b => ids.Contains(b.Id)).Select(b => b.Text).ToListAsync(ct);
        return texts.Sum(t => ObligationReconciliationService.CountWords(BeatMarkup.StripEntityTags(t)));
    }

    private static string Trunc(string s, int max) => s.Length <= max ? s : s[..max] + "…";
}
