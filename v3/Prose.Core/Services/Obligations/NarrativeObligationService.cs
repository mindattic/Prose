using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services.Audit;

namespace Prose.Core.Services.Obligations;

/// <summary>
/// The Narrative Obligation Ledger (RFC 0013). Every promise the prose makes becomes a row when
/// it is made; the Brief shows the writer what is owed; the chapter trial balance refuses to
/// close a chapter that owes something with no author decision. Nothing here writes prose.
///
/// <para>Three entry points matter:
/// <list type="bullet">
/// <item><see cref="ScanBeatAsync"/> — called from the single beat-write door for EVERY
/// <c>BeatWriteReason</c>, hash-gated, fire-and-forget. Re-verifies quotes on the edited beat,
/// withdraws or re-anchors what the edit invalidated, then extracts what the new text opens or
/// pays.</item>
/// <item><see cref="BuildBriefBlockAsync"/> — the RFC 0012 tier-C block: what is due now, what
/// is not yet.</item>
/// <item><see cref="TrialBalanceAsync"/> — the period close: opened − closed − dropped − deferred
/// = carried forward, and the overdue rows that still need a decision.</item>
/// </list></para>
///
/// <para>Author writes (<see cref="CloseAsync"/>, <see cref="DropAsync"/>, <see cref="DeferAsync"/>
/// …) set <see cref="NarrativeObligation.AuthorLocked"/>; from then on no system path re-states,
/// withdraws or re-anchors that row.</para>
/// </summary>
public class NarrativeObligationService(
    IDbContextFactory<ProseDbContext> dbFactory,
    ILogger<NarrativeObligationService> log,
    NarrativeObligationExtractor? extractor = null)
{
    /// <summary>Default due window for an unnamed referent / unexplained presence: the origin
    /// chapter plus this many chapters. Everything else defaults to book-end.</summary>
    public const int DefaultReferentDueChapters = 3;

    /// <summary>Chapters-to-due at which a Brief entry moves into DUE NOW.</summary>
    public const int UrgencyWindowChapters = 1;

    // ── The book clock ────────────────────────────────────────────────────────────

    /// <summary>Reading-order map of a book: every beat's 1-based chapter ordinal and 0-based
    /// position. Computed per call — a 475-beat book is trivial — so the ledger never keeps a
    /// stale copy of the tree.</summary>
    public sealed class BookClock
    {
        public required Guid BookNodeId { get; init; }
        public required IReadOnlyList<Guid> ChapterIds { get; init; }
        public required IReadOnlyDictionary<Guid, (int Chapter, int Position)> Beats { get; init; }
        public required IReadOnlyDictionary<Guid, string> ChapterTitles { get; init; }
        public int ChapterCount => ChapterIds.Count;
        public int BeatCount    => Beats.Count;

        public int ChapterOf(Guid beatId)  => Beats.TryGetValue(beatId, out var p) ? p.Chapter : 0;
        public int PositionOf(Guid beatId) => Beats.TryGetValue(beatId, out var p) ? p.Position : -1;
        public string ChapterTitle(int ordinal) =>
            ordinal >= 1 && ordinal <= ChapterIds.Count && ChapterTitles.TryGetValue(ChapterIds[ordinal - 1], out var t) ? t : $"Ch{ordinal}";
    }

    public static async Task<BookClock> LoadClockAsync(ProseDbContext db, Guid bookNodeId, CancellationToken ct)
    {
        var leafIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, bookNodeId, ct);
        if (leafIds.Count == 0) leafIds = [bookNodeId];
        var order = leafIds.Select((id, i) => (id, i)).ToDictionary(x => x.id, x => x.i);

        var rows = await db.BeatNodes.AsNoTracking()
            .Where(bn => leafIds.Contains(bn.NodeId))
            .Select(bn => new { bn.BeatId, bn.NodeId, bn.SortKey })
            .ToListAsync(ct);
        var ordered = rows.OrderBy(r => order[r.NodeId]).ThenBy(r => r.SortKey).ToList();

        var beats = new Dictionary<Guid, (int, int)>(ordered.Count);
        for (var i = 0; i < ordered.Count; i++)
            beats[ordered[i].BeatId] = (order[ordered[i].NodeId] + 1, i);

        var titles = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(n => leafIds.Contains(n.Id))
            .Select(n => new { n.Id, n.Title })
            .ToDictionaryAsync(n => n.Id, n => n.Title ?? "", ct);

        return new BookClock { BookNodeId = bookNodeId, ChapterIds = leafIds, Beats = beats, ChapterTitles = titles };
    }

    /// <summary>Is <paramref name="o"/> past due at <paramref name="atPosition"/> (a 0-based beat
    /// position) / <paramref name="atChapter"/>? Book-end obligations are never overdue here —
    /// the reconciler ages them only once the book is complete.</summary>
    public static bool IsPastDue(NarrativeObligation o, BookClock clock, int atChapter, int atPosition)
    {
        if (!ObligationState.IsOutstanding(o.State)) return false;
        return o.DueByKind switch
        {
            ObligationDueKind.Chapter => o.DueByValue is int ch && atChapter > ch,
            ObligationDueKind.Beats   => o.DueByValue is int n && o.OriginBeatId is Guid ob
                                          && clock.PositionOf(ob) is var op && op >= 0 && atPosition > op + n,
            _ => false,
        };
    }

    // ── Scan (the write door) ─────────────────────────────────────────────────────

    public sealed record ScanResult(bool Skipped, int Opened, int Advanced, int Closed, int Withdrawn, int Reopened, int Reanchored, int DiscardedUngrounded, bool Evaluated);

    /// <summary>Called for every saved beat. Free when the collapsed text hash is unchanged.</summary>
    public async Task<ScanResult> ScanBeatAsync(Guid bookNodeId, Guid beatId, string strippedText, string actor, CancellationToken ct = default)
    {
        var collapsed = QuoteGrounding.Normalize(strippedText);
        // The stamp covers the text AND the extractor version: a change in how beats are read
        // (v1 → v2 windowing, 2026-09-15) must make every "unchanged" beat rescannable, or a
        // calibration run silently re-uses the flawed read and scores the fix as if it never landed.
        var scanHash  = NodeWorkbenchService.ComputeTextHash(collapsed + "\n" + NarrativeObligationExtractor.PromptVersion);

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var beat = await db.Beats.FirstOrDefaultAsync(b => b.Id == beatId, ct);
        if (beat == null) return new ScanResult(true, 0, 0, 0, 0, 0, 0, 0, false);
        if (string.Equals(beat.ObligationScanHash, scanHash, StringComparison.OrdinalIgnoreCase))
            return new ScanResult(true, 0, 0, 0, 0, 0, 0, 0, false);

        var clock = await LoadClockAsync(db, bookNodeId, ct);
        var withdrawn = 0; var reopened = 0; var reanchored = 0;

        // 1. Re-verify what this beat already anchors.
        var anchored = await db.NarrativeObligations
            .Where(o => o.NodeId == bookNodeId && (o.OriginBeatId == beatId || o.ClosingBeatId == beatId) && !o.AuthorLocked)
            .ToListAsync(ct);
        foreach (var o in anchored)
        {
            if (o.OriginBeatId == beatId && o.OriginQuote != null)
            {
                if (QuoteGrounding.Contains(collapsed, o.OriginQuote, QuoteGrounding.MinObligationQuoteLength))
                {
                    o.OriginTextHash = beat.TextHash;
                }
                else
                {
                    var sibling = await FindSiblingWithQuoteAsync(db, clock, beatId, o.OriginQuote, ct);
                    if (sibling is { } s)
                    {
                        o.OriginBeatId = s.Id; o.OriginTextHash = s.TextHash; o.UpdatedAt = DateTime.UtcNow;
                        db.NarrativeObligationEvents.Add(Event(o.Id, ObligationEventAction.Reanchor, s.Id, o.OriginQuote, s.TextHash, actor, "origin quote moved to a sibling beat"));
                        reanchored++;
                    }
                    else if (o.State == ObligationState.Open && !await HasEventAsync(db, o.Id, ObligationEventAction.Advance, ct))
                    {
                        o.State = ObligationState.Withdrawn; o.UpdatedAt = DateTime.UtcNow;
                        db.NarrativeObligationEvents.Add(Event(o.Id, ObligationEventAction.Withdraw, beatId, null, beat.TextHash, actor, "origin text no longer on the page; nothing had advanced it"));
                        withdrawn++;
                    }
                    // else: keep; origin now reads "stale" via the hash and the reconciler reports it.
                }
            }
            if (o.ClosingBeatId == beatId && o.State == ObligationState.Closed && o.ClosingQuote != null
                && !QuoteGrounding.Contains(collapsed, o.ClosingQuote, QuoteGrounding.MinObligationQuoteLength))
            {
                o.State = ObligationState.Open; o.ClosingBeatId = null; o.ClosingQuote = null; o.ClosingTextHash = null; o.UpdatedAt = DateTime.UtcNow;
                db.NarrativeObligationEvents.Add(Event(o.Id, ObligationEventAction.Reopen, beatId, null, beat.TextHash, actor, "closing quote no longer on the page"));
                reopened++;
            }
        }
        await db.SaveChangesAsync(ct);

        // 2. Extract what the new text opens / pays.
        var opened = 0; var advanced = 0; var closed = 0; var discarded = 0; var evaluated = false;
        if (extractor != null && !string.IsNullOrWhiteSpace(collapsed))
        {
            var open = await OutstandingAsync(db, bookNodeId, ct);
            var openOrdered = SelectForListing(open, clock, clock.ChapterOf(beatId), clock.PositionOf(beatId), collapsed);

            var tagged = BeatMarkup.ExtractTaggedMentions(beat.Text ?? "").Select(m => m.Text).Distinct().ToList();
            var aliases = await KnownAliasesAsync(db, bookNodeId, ct);
            var hints = UnnamedReferentScanner.Scan(collapsed, aliases);
            var previousTail = await PreviousTailAsync(db, clock, beatId, ct);
            var stopList = await StopListExamplesAsync(db, bookNodeId, ct);

            var result = await extractor.ExtractAsync(new NarrativeObligationExtractor.Input(
                collapsed, previousTail, openOrdered, tagged, hints, stopList), ct);
            evaluated = result.Evaluated;
            discarded = result.DiscardedUngrounded;

            if (result.Evaluated)
            {
                var universeId = await db.Nodes.IgnoreQueryFilters().AsNoTracking()
                    .Where(n => n.Id == bookNodeId).Select(n => n.UniverseId).FirstOrDefaultAsync(ct);
                var originChapter = clock.ChapterOf(beatId);

                foreach (var item in result.Opened)
                {
                    var dedup = DedupKey(bookNodeId, item.Kind, item.Description);
                    var existing = await db.NarrativeObligations.FirstOrDefaultAsync(o => o.NodeId == bookNodeId && o.DedupKey == dedup, ct)
                        ?? await db.NarrativeObligations.FirstOrDefaultAsync(o => o.NodeId == bookNodeId && o.OriginQuote == item.Quote, ct);
                    if (existing != null)
                    {
                        if (existing.OriginBeatId != beatId && ObligationState.IsOutstanding(existing.State))
                        {
                            db.NarrativeObligationEvents.Add(Event(existing.Id, ObligationEventAction.Advance, beatId, item.Quote, beat.TextHash, actor, "re-raised in a later beat"));
                            if (!existing.AuthorLocked && existing.State == ObligationState.Open) existing.State = ObligationState.Advanced;
                            existing.UpdatedAt = DateTime.UtcNow;
                            advanced++;
                        }
                        continue;
                    }

                    Guid? entityId = null;
                    if (item.ReferentLabel != null && item.ReferentType != null && universeId != Guid.Empty)
                    {
                        var type = item.ReferentType switch { "place" => "place", "object" => "equipment", _ => "character" };
                        entityId = await EntityStubFactory.GetOrCreateUnnamedAsync(db, universeId, bookNodeId, item.ReferentLabel, type, item.Quote, ct);
                    }

                    var (dueKind, dueValue) = DefaultDue(item.Kind, item.DueHint, originChapter, clock);
                    var row = new NarrativeObligation
                    {
                        NodeId = bookNodeId, Kind = item.Kind, Description = item.Description,
                        Provenance = ClaimProvenance.Observed,
                        OriginBeatId = beatId, OriginQuote = item.Quote, OriginTextHash = beat.TextHash,
                        EntityId = entityId, TriggerCondition = item.Trigger,
                        DueByKind = dueKind, DueByValue = dueValue,
                        State = ObligationState.Open, DedupKey = dedup,
                    };
                    db.NarrativeObligations.Add(row);
                    db.NarrativeObligationEvents.Add(Event(row.Id, ObligationEventAction.Open, beatId, item.Quote, beat.TextHash, actor, item.Trigger));
                    opened++;
                }

                foreach (var t in result.Touched)
                {
                    var target = openOrdered[t.Index - 1];
                    var row = await db.NarrativeObligations.FirstOrDefaultAsync(o => o.Id == target.Id, ct);
                    if (row == null || row.OriginBeatId == beatId) continue;

                    if (t.Verdict == "closed")
                    {
                        db.NarrativeObligationEvents.Add(Event(row.Id, ObligationEventAction.Close, beatId, t.Quote, beat.TextHash, actor, null));
                        if (!row.AuthorLocked)
                        {
                            row.State = ObligationState.Closed; row.ClosingBeatId = beatId; row.ClosingQuote = t.Quote; row.ClosingTextHash = beat.TextHash;
                        }
                        row.UpdatedAt = DateTime.UtcNow;
                        closed++;
                    }
                    else
                    {
                        db.NarrativeObligationEvents.Add(Event(row.Id, ObligationEventAction.Advance, beatId, t.Quote, beat.TextHash, actor, null));
                        if (!row.AuthorLocked && row.State == ObligationState.Open) row.State = ObligationState.Advanced;
                        row.UpdatedAt = DateTime.UtcNow;
                        advanced++;
                    }
                }
                await db.SaveChangesAsync(ct);
            }
        }

        // 3. Stamp the gate — only when the extractor actually looked (or is not wired), so an
        //    outage re-scans next time instead of silently marking the beat done.
        if (extractor == null || evaluated)
            await db.Beats.Where(b => b.Id == beatId)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.ObligationScanHash, scanHash), ct);

        var res = new ScanResult(false, opened, advanced, closed, withdrawn, reopened, reanchored, discarded, evaluated);
        log.LogInformation("Obligation scan beat {BeatId}: opened={Opened} advanced={Advanced} closed={Closed} withdrawn={Withdrawn} reopened={Reopened} ungrounded={Discarded} evaluated={Evaluated}",
            beatId, opened, advanced, closed, withdrawn, reopened, discarded, evaluated);
        return res;
    }

    /// <summary>Extension of <c>NodeWorkbenchService.ClearEdgeBeatBoundsAsync</c>: null every beat
    /// reference to <paramref name="beatIds"/>, then withdraw open, unlocked, never-advanced rows
    /// whose origin just vanished. Bulk updates; nothing loaded into the change tracker.</summary>
    public static async Task ClearBeatReferencesAsync(ProseDbContext db, IReadOnlyCollection<Guid> beatIds, CancellationToken ct)
    {
        if (beatIds.Count == 0) return;

        var orphaned = await db.NarrativeObligations
            .Where(o => o.OriginBeatId != null && beatIds.Contains(o.OriginBeatId.Value) && !o.AuthorLocked && o.State == ObligationState.Open)
            .Select(o => o.Id).ToListAsync(ct);
        var advancedIds = await db.NarrativeObligationEvents
            .Where(e => orphaned.Contains(e.ObligationId) && e.Action == ObligationEventAction.Advance)
            .Select(e => e.ObligationId).Distinct().ToListAsync(ct);
        var toWithdraw = orphaned.Except(advancedIds).ToList();

        await db.NarrativeObligations.Where(o => o.OriginBeatId != null && beatIds.Contains(o.OriginBeatId.Value))
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.OriginBeatId, (Guid?)null), ct);
        await db.NarrativeObligations.Where(o => o.ClosingBeatId != null && beatIds.Contains(o.ClosingBeatId.Value))
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.ClosingBeatId, (Guid?)null), ct);
        await db.NarrativeObligationEvents.Where(e => e.BeatId != null && beatIds.Contains(e.BeatId.Value))
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.BeatId, (Guid?)null), ct);

        if (toWithdraw.Count > 0)
        {
            await db.NarrativeObligations.Where(o => toWithdraw.Contains(o.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.State, ObligationState.Withdrawn).SetProperty(o => o.UpdatedAt, DateTime.UtcNow), ct);
            db.NarrativeObligationEvents.AddRange(toWithdraw.Select(id =>
                Event(id, ObligationEventAction.Withdraw, null, null, null, ObligationActor.SystemRescan, "origin beat deleted; nothing had advanced it")));
            await db.SaveChangesAsync(ct);
        }
    }

    // ── The Brief block (RFC 0012 tier C) ─────────────────────────────────────────

    public const int BriefBlockMaxChars = 2500;

    public async Task<string> BuildBriefBlockAsync(Guid bookNodeId, Guid? beatId, int beatIndex, int totalBeats, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var open = await OutstandingAsync(db, bookNodeId, ct);
        if (open.Count == 0) return "";

        var clock = await LoadClockAsync(db, bookNodeId, ct);
        var atChapter  = beatId is Guid b && clock.ChapterOf(b) > 0 ? clock.ChapterOf(b) : Math.Max(1, clock.ChapterCount);
        var atPosition = beatId is Guid b2 && clock.PositionOf(b2) >= 0 ? clock.PositionOf(b2) : Math.Max(0, beatIndex);
        var endgame    = totalBeats > 0 && totalBeats - beatIndex <= 5;

        var dueNow = new List<string>();
        var notYet = new List<string>();
        foreach (var o in OrderByUrgency(open, clock, atChapter, atPosition))
        {
            var line = FormatBriefLine(o, clock);
            if (IsEligibleNow(o, clock, atChapter, atPosition) || (endgame && o.DueByKind == ObligationDueKind.BookEnd))
                dueNow.Add(line);
            else
                notYet.Add(line);
        }

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("[OPEN OBLIGATIONS — what the story owes the reader]");
        if (dueNow.Count > 0)
        {
            sb.AppendLine("DUE NOW (advance or pay at least one if the scene allows):");
            var n = 1; foreach (var l in dueNow) sb.AppendLine($"  {n++}. {l}");
        }
        if (notYet.Count > 0)
        {
            sb.AppendLine("NOT YET (do not resolve; may echo):");
            var n = dueNow.Count + 1; foreach (var l in notYet) sb.AppendLine($"  {n++}. {l}");
        }
        sb.AppendLine("If you introduce a new unnamed person, object, or mystery, make it matter — it will be recorded as owed.");

        var text = sb.ToString();
        if (text.Length <= BriefBlockMaxChars) return text;
        // Trim NOT YET lines from the bottom until it fits; DUE NOW is never dropped.
        while (notYet.Count > 0 && text.Length > BriefBlockMaxChars)
        {
            notYet.RemoveAt(notYet.Count - 1);
            text = Render(dueNow, notYet);
        }
        return text;

        static string Render(List<string> dueNow, List<string> notYet)
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("[OPEN OBLIGATIONS — what the story owes the reader]");
            if (dueNow.Count > 0) { sb.AppendLine("DUE NOW (advance or pay at least one if the scene allows):"); var n = 1; foreach (var l in dueNow) sb.AppendLine($"  {n++}. {l}"); }
            if (notYet.Count > 0) { sb.AppendLine("NOT YET (do not resolve; may echo):"); var n = dueNow.Count + 1; foreach (var l in notYet) sb.AppendLine($"  {n++}. {l}"); }
            sb.AppendLine("If you introduce a new unnamed person, object, or mystery, make it matter — it will be recorded as owed.");
            return sb.ToString();
        }
    }

    private static string FormatBriefLine(NarrativeObligation o, BookClock clock)
    {
        var origin = o.OriginBeatId is Guid ob && clock.ChapterOf(ob) > 0 ? $"Ch{clock.ChapterOf(ob)}" : "outline";
        var due = o.DueByKind switch
        {
            ObligationDueKind.Chapter => $"due Ch{o.DueByValue}",
            ObligationDueKind.Beats   => $"due within {o.DueByValue} beats",
            _                         => "book-end",
        };
        var trigger = string.IsNullOrWhiteSpace(o.TriggerCondition) ? "" : $" · when: {o.TriggerCondition}";
        var quote = string.IsNullOrWhiteSpace(o.OriginQuote) ? "" : $" — \"{Truncate(o.OriginQuote, 110)}\"";
        return $"[{o.Kind.ToUpperInvariant()} · {origin} · {due}{trigger}] {o.Description}{quote}";
    }

    private static bool IsEligibleNow(NarrativeObligation o, BookClock clock, int atChapter, int atPosition)
    {
        if (IsPastDue(o, clock, atChapter, atPosition)) return true;
        return o.DueByKind switch
        {
            ObligationDueKind.Chapter => o.DueByValue is int ch && ch - atChapter <= UrgencyWindowChapters,
            ObligationDueKind.Beats   => o.DueByValue is int n && o.OriginBeatId is Guid ob && clock.PositionOf(ob) is var op && op >= 0 && atPosition >= op + n - 5,
            // A book-end promise with no trigger becomes fair game once it is a chapter old.
            _ => string.IsNullOrWhiteSpace(o.TriggerCondition) && o.OriginBeatId is Guid ob2 && atChapter > clock.ChapterOf(ob2),
        };
    }

    /// <summary>The open rows the extractor is shown for one beat, filled in three tiers when there
    /// are more outstanding rows than <see cref="NarrativeObligationExtractor.MaxOpenListed"/>:
    /// <list type="number">
    /// <item><b>Local</b> — up to <see cref="NarrativeObligationExtractor.MaxLocalListed"/> debts
    /// opened in THIS chapter, newest origin first. A story pays what it has just promised.</item>
    /// <item><b>Lexically relevant</b> — up to <see cref="NarrativeObligationExtractor.MaxLexicalListed"/>
    /// rows from anywhere whose content words occur in this beat, most overlap first.</item>
    /// <item><b>Urgent</b> — whatever slots remain, in urgency order.</item>
    /// </list>
    /// Locality leads because proximity, not urgency, predicts which debt a beat pays: on GCSH —
    /// twelve structurally identical self-contained stories — chapter 1 closed 82% of what it
    /// opened while later chapters managed 15–59%, the only variable being how many older debts
    /// were competing for the same 40 slots (run 5, 2026-09-16, the first uncontaminated run).
    /// The numbering the model sees is the index into the returned list, so callers must pass this
    /// exact list on.</summary>
    internal static List<NarrativeObligation> SelectForListing(IReadOnlyList<NarrativeObligation> open, BookClock clock, int atChapter, int atPosition, string beatText)
    {
        var max = NarrativeObligationExtractor.MaxOpenListed;
        var ordered = OrderByUrgency(open, clock, atChapter, atPosition).ToList();
        if (ordered.Count <= max) return ordered;

        var listed = new List<NarrativeObligation>(max);
        var seen = new HashSet<Guid>();
        void Take(IEnumerable<NarrativeObligation> rows, int cap)
        {
            var taken = 0;
            foreach (var o in rows)
            {
                if (taken >= cap || listed.Count >= max) return;
                if (!seen.Add(o.Id)) continue;
                listed.Add(o);
                taken++;
            }
        }

        Take(open.Where(o => o.OriginBeatId is Guid ob && clock.ChapterOf(ob) == atChapter)
                 .OrderByDescending(o => o.OriginBeatId is Guid ob2 ? clock.PositionOf(ob2) : -1),
             NarrativeObligationExtractor.MaxLocalListed);

        Take(ordered
                .Select(o => (Row: o, Score: LexicalCandidateFinder.ContentWords(o.Description + " " + (o.OriginQuote ?? ""))
                                                 .Count(w => beatText.Contains(w, StringComparison.OrdinalIgnoreCase))))
                .Where(x => x.Score >= 2)
                .OrderByDescending(x => x.Score)
                .Select(x => x.Row),
             NarrativeObligationExtractor.MaxLexicalListed);

        Take(ordered, max);
        return listed;
    }

    private static IEnumerable<NarrativeObligation> OrderByUrgency(IEnumerable<NarrativeObligation> rows, BookClock clock, int atChapter, int atPosition) =>
        rows.OrderByDescending(o => IsPastDue(o, clock, atChapter, atPosition))
            .ThenBy(o => o.DueByKind == ObligationDueKind.Chapter ? (o.DueByValue ?? int.MaxValue) - atChapter : int.MaxValue / 2)
            .ThenBy(o => o.OriginBeatId is Guid ob ? clock.PositionOf(ob) : -1);

    // ── Trial balance ─────────────────────────────────────────────────────────────

    public sealed record ObligationView(
        Guid Id, string Kind, string Description, string State, string Provenance,
        int OriginChapter, Guid? OriginBeatId, string? OriginQuote, string OriginTrust,
        int? ClosingChapter, Guid? ClosingBeatId, string? ClosingQuote,
        string Due, bool Overdue, bool AuthorLocked, string? EntityName, string? Trigger, string? AuthorNote, string? DroppedReason,
        DateTime CreatedAt);

    public sealed record TrialBalance(
        Guid NodeId, int? ChapterOrdinal, int ChapterCount, int TotalBeats, int ScannedBeats,
        int Opened, int Advanced, int Closed, int Dropped, int Deferred, int Withdrawn, int CarriedForward,
        IReadOnlyList<ObligationView> OverdueWithoutDecision,
        IReadOnlyList<ObligationView> OpenedHere,
        IReadOnlyList<ObligationView> ClosedHere,
        IReadOnlyList<ObligationView> StaleClosures,
        IReadOnlyList<ObligationView> DanglingOrigins)
    {
        public bool Balanced => OverdueWithoutDecision.Count == 0 && ScannedBeats == TotalBeats;
        public bool CouldNotLook => TotalBeats == 0 || ScannedBeats == 0;
    }

    /// <summary>The period close for a chapter (or, with <paramref name="chapterOrdinal"/> null,
    /// the whole book). "Overdue without decision" is the set the hard gate reads: outstanding
    /// rows past due at the END of the chapter, not author-locked into a decision.</summary>
    public async Task<TrialBalance> TrialBalanceAsync(Guid bookNodeId, int? chapterOrdinal, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var clock = await LoadClockAsync(db, bookNodeId, ct);
        var all = await db.NarrativeObligations.AsNoTracking()
            .Where(o => o.NodeId == bookNodeId && o.State != ObligationState.Withdrawn)
            .ToListAsync(ct);
        var withdrawn = await db.NarrativeObligations.AsNoTracking().CountAsync(o => o.NodeId == bookNodeId && o.State == ObligationState.Withdrawn, ct);

        var beatIds = clock.Beats.Keys.ToList();
        var beatMeta = await db.Beats.AsNoTracking()
            .Where(b => beatIds.Contains(b.Id))
            .Select(b => new { b.Id, b.TextHash, b.ObligationScanHash })
            .ToDictionaryAsync(b => b.Id, ct);
        var scanned = beatMeta.Values.Count(b => !string.IsNullOrEmpty(b.ObligationScanHash));

        var entityIds = all.Where(o => o.EntityId != null).Select(o => o.EntityId!.Value).Distinct().ToList();
        var entityNames = entityIds.Count == 0 ? new Dictionary<Guid, string>()
            : await db.Entities.AsNoTracking().IgnoreQueryFilters().Where(e => entityIds.Contains(e.Id)).ToDictionaryAsync(e => e.Id, e => e.Name, ct);

        var atChapter  = chapterOrdinal ?? clock.ChapterCount;
        var atPosition = chapterOrdinal is int co && co >= 1 && co <= clock.ChapterCount
            ? clock.Beats.Values.Where(p => p.Chapter <= co).Select(p => p.Position).DefaultIfEmpty(-1).Max()
            : clock.BeatCount - 1;

        ObligationView View(NarrativeObligation o)
        {
            var originCh = o.OriginBeatId is Guid ob ? clock.ChapterOf(ob) : 0;
            var trust = o.OriginBeatId is Guid ob2 && beatMeta.TryGetValue(ob2, out var bm)
                ? Beat.SummaryTrustState(o.OriginQuote, o.OriginTextHash, bm.TextHash) ?? "unverified"
                : (o.OriginBeatId == null ? "dangling" : "unverified");
            var due = o.DueByKind switch
            {
                ObligationDueKind.Chapter => $"Ch{o.DueByValue}",
                ObligationDueKind.Beats   => $"+{o.DueByValue} beats",
                _                         => "book-end",
            };
            return new ObligationView(o.Id, o.Kind, o.Description, o.State, o.Provenance,
                originCh, o.OriginBeatId, o.OriginQuote, trust,
                o.ClosingBeatId is Guid cb ? clock.ChapterOf(cb) : null, o.ClosingBeatId, o.ClosingQuote,
                due, IsPastDue(o, clock, atChapter, atPosition), o.AuthorLocked,
                o.EntityId is Guid eid && entityNames.TryGetValue(eid, out var en) ? en : null,
                o.TriggerCondition, o.AuthorNote, o.DroppedReason, o.CreatedAt);
        }

        bool InChapter(Guid? beatId) => chapterOrdinal == null || (beatId is Guid b && clock.ChapterOf(b) == chapterOrdinal);

        var openedHere = all.Where(o => InChapter(o.OriginBeatId)).OrderBy(o => o.OriginBeatId is Guid b ? clock.PositionOf(b) : -1).Select(View).ToList();
        var closedHere = all.Where(o => o.State == ObligationState.Closed && InChapter(o.ClosingBeatId)).Select(View).ToList();

        var overdue = all
            .Where(o => ObligationState.IsOutstanding(o.State) && !o.AuthorLocked && IsPastDue(o, clock, atChapter, atPosition))
            .Where(o => chapterOrdinal == null || (o.OriginBeatId is Guid b && clock.ChapterOf(b) <= chapterOrdinal))
            .Select(View).OrderBy(v => v.OriginChapter).ToList();

        var staleClosures = all
            .Where(o => o.State == ObligationState.Closed && o.ClosingBeatId is Guid cb && beatMeta.TryGetValue(cb, out var bm)
                        && Beat.SummaryTrustState(o.ClosingQuote, o.ClosingTextHash, bm.TextHash) == "stale")
            .Select(View).ToList();

        var dangling = all.Where(o => o.OriginBeatId == null && o.Provenance != ClaimProvenance.Authored && o.State != ObligationState.Dropped).Select(View).ToList();

        var inScope = chapterOrdinal == null ? all : all.Where(o => o.OriginBeatId is Guid b && clock.ChapterOf(b) <= chapterOrdinal).ToList();
        int Count(string state) => inScope.Count(o => o.State == state);
        var closedCount = Count(ObligationState.Closed);
        var dropped = Count(ObligationState.Dropped);
        var deferred = Count(ObligationState.Deferred);
        var advancedCount = Count(ObligationState.Advanced);
        var carried = inScope.Count(o => ObligationState.IsOutstanding(o.State));

        return new TrialBalance(bookNodeId, chapterOrdinal, clock.ChapterCount, clock.BeatCount, scanned,
            inScope.Count, advancedCount, closedCount, dropped, deferred, withdrawn, carried,
            overdue, openedHere, closedHere, staleClosures, dangling);
    }

    // ── Reads ─────────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<ObligationView>> ListAsync(Guid bookNodeId, string? state = null, string? kind = null, bool overdueOnly = false, int? chapter = null, CancellationToken ct = default)
    {
        var tb = await TrialBalanceAsync(bookNodeId, null, ct);
        IEnumerable<ObligationView> rows = tb.OpenedHere;
        if (state != null) rows = rows.Where(v => string.Equals(v.State, state, StringComparison.OrdinalIgnoreCase));
        if (kind != null) rows = rows.Where(v => string.Equals(v.Kind, kind, StringComparison.OrdinalIgnoreCase));
        if (overdueOnly) rows = rows.Where(v => v.Overdue && !v.AuthorLocked);
        if (chapter is int ch) rows = rows.Where(v => v.OriginChapter == ch);
        return rows.ToList();
    }

    public async Task<NarrativeObligation?> GetAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.NarrativeObligations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public async Task<IReadOnlyList<NarrativeObligationEvent>> HistoryAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.NarrativeObligationEvents.AsNoTracking().Where(e => e.ObligationId == id).OrderBy(e => e.CreatedAt).ToListAsync(ct);
    }

    // ── Author writes ─────────────────────────────────────────────────────────────

    public sealed record AuthorResult(bool Ok, string? Error, NarrativeObligation? Row);

    public async Task<AuthorResult> OpenAsync(Guid bookNodeId, string kind, string description, string actor,
        Guid? originBeatId = null, string? quote = null, Guid? entityId = null, string? trigger = null,
        string dueKind = ObligationDueKind.BookEnd, int? dueValue = null, CancellationToken ct = default)
    {
        if (!ObligationKind.IsValid(kind)) return new(false, $"unknown kind '{kind}'", null);
        if (string.IsNullOrWhiteSpace(description)) return new(false, "description required", null);
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        string? textHash = null;
        if (originBeatId is Guid ob)
        {
            var beat = await db.Beats.AsNoTracking().FirstOrDefaultAsync(b => b.Id == ob, ct);
            if (beat == null) return new(false, "origin beat not found", null);
            if (quote != null && !QuoteGrounding.Contains(BeatMarkup.StripEntityTags(beat.Text), quote, QuoteGrounding.MinObligationQuoteLength))
                return new(false, "quote_not_found: the quote is not in that beat's text", null);
            textHash = beat.TextHash;
        }

        var dedup = DedupKey(bookNodeId, kind, description);
        if (await db.NarrativeObligations.AnyAsync(o => o.NodeId == bookNodeId && o.DedupKey == dedup, ct))
            return new(false, "duplicate: an obligation with this kind and description already exists", null);

        var row = new NarrativeObligation
        {
            NodeId = bookNodeId, Kind = kind, Description = description.Trim(), Provenance = ClaimProvenance.Authored,
            OriginBeatId = originBeatId, OriginQuote = quote is null ? null : QuoteGrounding.ClampForStorage(quote), OriginTextHash = textHash,
            EntityId = entityId, TriggerCondition = trigger, DueByKind = dueKind, DueByValue = dueValue,
            State = ObligationState.Open, AuthorLocked = true, DedupKey = dedup,
        };
        db.NarrativeObligations.Add(row);
        db.NarrativeObligationEvents.Add(Event(row.Id, ObligationEventAction.Open, originBeatId, row.OriginQuote, textHash, actor, trigger));
        await db.SaveChangesAsync(ct);
        return new(true, null, row);
    }

    public async Task<AuthorResult> CloseAsync(Guid id, Guid closingBeatId, string quote, string? note, string actor, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.NarrativeObligations.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (row == null) return new(false, "not_found", null);
        var beat = await db.Beats.AsNoTracking().FirstOrDefaultAsync(b => b.Id == closingBeatId, ct);
        if (beat == null) return new(false, "closing beat not found", null);
        if (!QuoteGrounding.Contains(BeatMarkup.StripEntityTags(beat.Text), quote, QuoteGrounding.MinObligationQuoteLength))
            return new(false, "quote_not_found: the quote is not in that beat's text", null);

        row.State = ObligationState.Closed; row.ClosingBeatId = closingBeatId; row.ClosingQuote = QuoteGrounding.ClampForStorage(quote); row.ClosingTextHash = beat.TextHash;
        row.AuthorNote = note ?? row.AuthorNote; row.AuthorLocked = true; row.UpdatedAt = DateTime.UtcNow;
        db.NarrativeObligationEvents.Add(Event(row.Id, ObligationEventAction.Close, closingBeatId, row.ClosingQuote, beat.TextHash, actor, note));
        await db.SaveChangesAsync(ct);
        return new(true, null, row);
    }

    public async Task<AuthorResult> DropAsync(Guid id, string reason, string note, string actor, CancellationToken ct = default)
    {
        if (!ObligationDroppedReason.IsValid(reason)) return new(false, $"unknown dropped reason '{reason}'", null);
        if (string.IsNullOrWhiteSpace(note)) return new(false, "a note is required to drop an obligation", null);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.NarrativeObligations.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (row == null) return new(false, "not_found", null);
        row.State = ObligationState.Dropped; row.DroppedReason = reason; row.AuthorNote = note.Trim(); row.AuthorLocked = true; row.UpdatedAt = DateTime.UtcNow;
        db.NarrativeObligationEvents.Add(Event(row.Id, ObligationEventAction.Drop, null, null, null, actor, $"{reason}: {note.Trim()}"));
        await db.SaveChangesAsync(ct);
        return new(true, null, row);
    }

    public async Task<AuthorResult> DeferAsync(Guid id, string note, string dueKind, int? dueValue, string actor, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(note)) return new(false, "a note is required to defer an obligation", null);
        if (dueKind is not (ObligationDueKind.Chapter or ObligationDueKind.Beats or ObligationDueKind.BookEnd)) return new(false, $"unknown due kind '{dueKind}'", null);
        if (dueKind != ObligationDueKind.BookEnd && dueValue is null) return new(false, "dueValue required for that due kind", null);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.NarrativeObligations.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (row == null) return new(false, "not_found", null);
        row.State = ObligationState.Deferred; row.DueByKind = dueKind; row.DueByValue = dueValue; row.AuthorNote = note.Trim(); row.AuthorLocked = true; row.UpdatedAt = DateTime.UtcNow;
        db.NarrativeObligationEvents.Add(Event(row.Id, ObligationEventAction.Defer, null, null, null, actor, note.Trim()));
        await db.SaveChangesAsync(ct);
        return new(true, null, row);
    }

    public async Task<AuthorResult> ReopenAsync(Guid id, string? note, string actor, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.NarrativeObligations.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (row == null) return new(false, "not_found", null);
        row.State = ObligationState.Open; row.ClosingBeatId = null; row.ClosingQuote = null; row.ClosingTextHash = null; row.DroppedReason = null;
        row.AuthorNote = note ?? row.AuthorNote; row.AuthorLocked = true; row.UpdatedAt = DateTime.UtcNow;
        db.NarrativeObligationEvents.Add(Event(row.Id, ObligationEventAction.Reopen, null, null, null, actor, note));
        await db.SaveChangesAsync(ct);
        return new(true, null, row);
    }

    public async Task<AuthorResult> SetDueAsync(Guid id, string dueKind, int? dueValue, string actor, CancellationToken ct = default)
    {
        if (dueKind is not (ObligationDueKind.Chapter or ObligationDueKind.Beats or ObligationDueKind.BookEnd)) return new(false, $"unknown due kind '{dueKind}'", null);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.NarrativeObligations.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (row == null) return new(false, "not_found", null);
        row.DueByKind = dueKind; row.DueByValue = dueValue; row.AuthorLocked = true; row.UpdatedAt = DateTime.UtcNow;
        db.NarrativeObligationEvents.Add(Event(row.Id, ObligationEventAction.DueChanged, null, null, null, actor, $"{dueKind} {dueValue}"));
        await db.SaveChangesAsync(ct);
        return new(true, null, row);
    }

    /// <summary>"Accept": the author confirms an extracted row as-is; it becomes immutable to the system.</summary>
    public async Task<AuthorResult> LockAsync(Guid id, string actor, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.NarrativeObligations.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (row == null) return new(false, "not_found", null);
        row.AuthorLocked = true; row.UpdatedAt = DateTime.UtcNow;
        db.NarrativeObligationEvents.Add(Event(row.Id, ObligationEventAction.Lock, null, null, null, actor, null));
        await db.SaveChangesAsync(ct);
        return new(true, null, row);
    }

    public async Task<AuthorResult> LinkEntityAsync(Guid id, Guid entityId, string actor, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.NarrativeObligations.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (row == null) return new(false, "not_found", null);
        if (!await db.Entities.IgnoreQueryFilters().AnyAsync(e => e.Id == entityId, ct)) return new(false, "entity not found", null);
        row.EntityId = entityId; row.AuthorLocked = true; row.UpdatedAt = DateTime.UtcNow;
        db.NarrativeObligationEvents.Add(Event(row.Id, ObligationEventAction.Lock, null, null, null, actor, $"linked entity {entityId}"));
        await db.SaveChangesAsync(ct);
        return new(true, null, row);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    public static string DedupKey(Guid nodeId, string kind, string description)
    {
        var norm = QuoteGrounding.Normalize(description).ToLowerInvariant();
        var bytes = SHA1.HashData(Encoding.UTF8.GetBytes($"{nodeId:N}|{kind}|{norm}"));
        return Convert.ToHexString(bytes);
    }

    internal static (string Kind, int? Value) DefaultDue(string kind, string? dueHint, int originChapter, BookClock clock)
    {
        if (dueHint == "soon")    return (ObligationDueKind.Beats, 12);
        if (dueHint == "chapter") return (ObligationDueKind.Chapter, Math.Max(1, originChapter));
        if (dueHint == "book")    return (ObligationDueKind.BookEnd, null);
        if (kind is ObligationKind.IntroducedReferent or ObligationKind.UnexplainedPresence)
            return (ObligationDueKind.Chapter, Math.Min(Math.Max(clock.ChapterCount, originChapter), Math.Max(1, originChapter) + DefaultReferentDueChapters));
        return (ObligationDueKind.BookEnd, null);
    }

    private static NarrativeObligationEvent Event(Guid obligationId, string action, Guid? beatId, string? quote, string? textHash, string actor, string? note) =>
        new() { ObligationId = obligationId, Action = action, BeatId = beatId, Quote = quote is null ? null : Truncate(quote, 400), BeatTextHash = textHash, Actor = actor, Note = note is null ? null : Truncate(note, 1000) };

    private static async Task<List<NarrativeObligation>> OutstandingAsync(ProseDbContext db, Guid bookNodeId, CancellationToken ct) =>
        await db.NarrativeObligations.AsNoTracking()
            .Where(o => o.NodeId == bookNodeId && (o.State == ObligationState.Open || o.State == ObligationState.Advanced))
            .OrderBy(o => o.CreatedAt)
            .ToListAsync(ct);

    private static Task<bool> HasEventAsync(ProseDbContext db, Guid obligationId, string action, CancellationToken ct) =>
        db.NarrativeObligationEvents.AnyAsync(e => e.ObligationId == obligationId && e.Action == action, ct);

    private sealed record SiblingHit(Guid Id, string? TextHash);

    private static async Task<SiblingHit?> FindSiblingWithQuoteAsync(ProseDbContext db, BookClock clock, Guid beatId, string quote, CancellationToken ct)
    {
        var chapter = clock.ChapterOf(beatId);
        if (chapter == 0) return null;
        var siblingIds = clock.Beats.Where(kv => kv.Value.Chapter == chapter && kv.Key != beatId).Select(kv => kv.Key).ToList();
        if (siblingIds.Count == 0) return null;
        var siblings = await db.Beats.AsNoTracking().Where(b => siblingIds.Contains(b.Id)).Select(b => new { b.Id, b.Text, b.TextHash }).ToListAsync(ct);
        var hit = siblings.FirstOrDefault(s => QuoteGrounding.Contains(BeatMarkup.StripEntityTags(s.Text), quote, QuoteGrounding.MinObligationQuoteLength));
        return hit == null ? null : new SiblingHit(hit.Id, hit.TextHash);
    }

    private static async Task<string?> PreviousTailAsync(ProseDbContext db, BookClock clock, Guid beatId, CancellationToken ct)
    {
        var pos = clock.PositionOf(beatId);
        var ch  = clock.ChapterOf(beatId);
        if (pos <= 0) return null;
        var prevId = clock.Beats.Where(kv => kv.Value.Position == pos - 1 && kv.Value.Chapter == ch).Select(kv => kv.Key).FirstOrDefault();
        if (prevId == Guid.Empty) return null;
        var text = await db.Beats.AsNoTracking().Where(b => b.Id == prevId).Select(b => b.Text).FirstOrDefaultAsync(ct);
        return text == null ? null : BeatMarkup.StripEntityTags(text);
    }

    private static async Task<HashSet<string>> KnownAliasesAsync(ProseDbContext db, Guid bookNodeId, CancellationToken ct)
    {
        var universeId = await db.Nodes.IgnoreQueryFilters().AsNoTracking().Where(n => n.Id == bookNodeId).Select(n => n.UniverseId).FirstOrDefaultAsync(ct);
        var names = await db.Entities.AsNoTracking().IgnoreQueryFilters()
            .Where(e => e.UniverseId == universeId && e.Status != "archived")
            .Select(e => e.Name).ToListAsync(ct);
        return names.Select(n => n.ToLowerInvariant().Trim()).Where(n => n.Length > 0).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Negative few-shots for the extractor: descriptions the author dropped as
    /// background texture or false extraction in this book's universe (RFC 0013 learning loop).</summary>
    private static async Task<List<string>> StopListExamplesAsync(ProseDbContext db, Guid bookNodeId, CancellationToken ct)
    {
        var universeId = await db.Nodes.IgnoreQueryFilters().AsNoTracking().Where(n => n.Id == bookNodeId).Select(n => n.UniverseId).FirstOrDefaultAsync(ct);
        var nodeIds = await db.Nodes.IgnoreQueryFilters().AsNoTracking().Where(n => n.UniverseId == universeId && n.ParentNodeId == null).Select(n => n.Id).ToListAsync(ct);
        return await db.NarrativeObligations.AsNoTracking()
            .Where(o => nodeIds.Contains(o.NodeId) && o.State == ObligationState.Dropped
                        && (o.DroppedReason == ObligationDroppedReason.BackgroundTexture || o.DroppedReason == ObligationDroppedReason.FalseExtraction))
            .OrderByDescending(o => o.UpdatedAt)
            .Select(o => o.Description)
            .Take(12)
            .ToListAsync(ct);
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
}
