using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;
using Prose.Core.Services.Audit;
using Prose.Core.Services.Obligations;

namespace Prose.Cli;

/// <summary>
/// prose --obligations &lt;mode&gt; --slug &lt;slug|code|id&gt; [--json] …   (RFC 0013)
///
///   list          [--state Open|Advanced|Closed|Dropped|Deferred] [--kind k] [--overdue] [--chapter N]
///   trial-balance [--chapter N]           the period close: opened − closed − dropped − deferred = carried
///   history       --id &lt;guid&gt;             the journal for one obligation
///   open          --kind k --description "…" [--beat-id g --quote "…"] [--due chapter:7|beats:12|book-end]
///   close         --id g --beat-id g --quote "…" [--note "…"]
///   drop          --id g --reason &lt;reason&gt; --note "…"
///   defer         --id g --note "…" --due chapter:12|beats:30|book-end
///   reopen        --id g [--note "…"]
///   due           --id g --due chapter:7|beats:12|book-end
///   accept        --id g                  lock an extracted row as-is
///   rescan        [--beat-id g] [--force]  re-run the extractor over one beat / the whole book
///   completeness  [--complete t|f|unset]  Does this text finish its own story? The one fact the
///                                         calibration bar needs and could not ask for (§6a). With
///                                         no flag it reports; it is NOT derived from Status, which
///                                         answers a different question. Free.
///   coverage                              READ-ONLY: which beats the extractor actually READ on the
///                                         last scan, with the length distribution of the read vs
///                                         unread groups. An unread beat contributes nothing and is
///                                         otherwise indistinguishable from a beat that owes nothing.
///   candidates    --id g [--payoff-beat g …]  READ-ONLY retrieval diagnostic: what the resurfacing
///                                         judge's candidate finder returns for one obligation, what
///                                         the judge cache recorded, and — for each --payoff-beat —
///                                         whether that beat was ever in the candidate set, plus its
///                                         lexical and embedding rank when it was not. Separates
///                                         "retrieved but not recognised" (a judge/granularity
///                                         problem) from "never retrieved" (a retrieval-budget
///                                         problem). No LLM call, no writes.
///   import-bible-ledger [--dry-run]       the bible's §14a closed plants / §14b dropped findings →
///                                         authored, locked Closed / Dropped rows (BCODA runbook step 3);
///                                         anchors resolved from "Ch<n> SK:<k>" exactly or listed as NEEDS ANCHOR
///
/// Runs inside the Hub via CliDispatch. Every write is an author write: it locks the row and
/// journals the actor. Nothing here touches prose.
/// </summary>
public static class ObligationCli
{
    static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var mode = args.SkipWhile(a => a != "--obligations").Skip(1).FirstOrDefault(a => !a.StartsWith("--")) ?? "list";
        var json = args.Contains("--json");
        string? Flag(string name) { for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1]; return null; }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var svc = services.GetRequiredService<NarrativeObligationService>();

        // Read-only retrieval diagnostic. Keyed by obligation id (the row carries its own node),
        // and it needs the finder + embeddings from DI, so it routes ahead of the author modes.
        if (mode == "candidates")
        {
            if (!Guid.TryParse(Flag("--id"), out var oid)) { Console.Error.WriteLine("[obligations] --id <guid> is required."); return 2; }
            return await RunCandidatesAsync(oid, args, json, services, dbFactory);
        }

        // Modes keyed by obligation id don't need a slug.
        if (mode is "history" or "close" or "drop" or "defer" or "reopen" or "due" or "accept")
        {
            if (!Guid.TryParse(Flag("--id"), out var id)) { Console.Error.WriteLine("[obligations] --id <guid> is required."); return 2; }
            return await RunByIdAsync(mode, id, args, Flag, json, svc);
        }

        var slug = Flag("--slug");
        if (string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("Usage: prose --obligations <list|trial-balance|open|rescan> --slug <slug> [--json] …");
            return 2;
        }

        Guid nodeId; string title;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var resolved = await NodeRefResolver.ResolveAsync(db, slug);
            if (resolved == null) { Console.Error.WriteLine($"[obligations] {NodeRefResolver.NotFoundMessage(slug)}"); return 1; }
            nodeId = await BookRootAsync(db, resolved.Value);
            title = await db.Nodes.IgnoreQueryFilters().Where(n => n.Id == nodeId).Select(n => n.Title).FirstAsync();
        }

        switch (mode)
        {
            case "list":
            {
                int? chapter = int.TryParse(Flag("--chapter"), out var c) ? c : null;
                var rows = await svc.ListAsync(nodeId, Flag("--state"), Flag("--kind"), args.Contains("--overdue"), chapter);
                if (json) { Console.WriteLine(JsonSerializer.Serialize(new { node_id = nodeId, title, count = rows.Count, obligations = rows }, Json)); return 0; }
                Console.WriteLine($"[obligations] {title} — {rows.Count} row(s)");
                foreach (var r in rows) Console.WriteLine(Line(r));
                if (rows.Count == 0) Console.WriteLine("  (none — if the book has beats, run `prose --obligations rescan --slug …` to build the ledger)");
                return 0;
            }
            case "trial-balance":
            {
                int? chapter = int.TryParse(Flag("--chapter"), out var c) ? c : null;
                var tb = await svc.TrialBalanceAsync(nodeId, chapter);
                if (json) { Console.WriteLine(JsonSerializer.Serialize(new { title, tb.NodeId, tb.ChapterOrdinal, tb.ChapterCount, tb.TotalBeats, tb.ScannedBeats, tb.Opened, tb.Advanced, tb.Closed, tb.Dropped, tb.Deferred, tb.Withdrawn, tb.CarriedForward, tb.Balanced, tb.CouldNotLook, overdue_without_decision = tb.OverdueWithoutDecision, opened_here = tb.OpenedHere, closed_here = tb.ClosedHere, stale_closures = tb.StaleClosures, dangling_origins = tb.DanglingOrigins }, Json)); return tb.Balanced ? 0 : 1; }
                PrintTrialBalance(title, tb);
                return tb.Balanced ? 0 : 1;
            }
            case "open":
            {
                var kind = Flag("--kind") ?? ObligationKind.Promise;
                var description = Flag("--description");
                if (string.IsNullOrWhiteSpace(description)) { Console.Error.WriteLine("[obligations] --description is required."); return 2; }
                Guid? beatId = Guid.TryParse(Flag("--beat-id"), out var b) ? b : null;
                var (dueKind, dueValue) = ParseDue(Flag("--due"));
                var res = await svc.OpenAsync(nodeId, kind, description, ObligationActor.AuthorCli, beatId, Flag("--quote"), null, Flag("--trigger"), dueKind, dueValue);
                return Report(res, json);
            }
            case "rescan":
            {
                var force = args.Contains("--force");
                Guid? only = Guid.TryParse(Flag("--beat-id"), out var b) ? b : null;
                var scanned = 0; var opened = 0; var advanced = 0; var closed = 0; var skipped = 0; var ungrounded = 0; var notEvaluated = 0;
                await using var db = await dbFactory.CreateDbContextAsync();
                var clock = await NarrativeObligationService.LoadClockAsync(db, nodeId, CancellationToken.None);
                var ordered = clock.Beats.OrderBy(kv => kv.Value.Position).Select(kv => kv.Key).Where(id => only == null || id == only).ToList();
                Console.WriteLine($"[obligations] rescan {title}: {ordered.Count} beat(s) in reading order{(force ? " (forced)" : "")}");
                foreach (var beatId in ordered)
                {
                    var beat = await db.Beats.AsNoTracking().FirstOrDefaultAsync(x => x.Id == beatId);
                    if (beat == null) continue;
                    if (force) await db.Beats.Where(x => x.Id == beatId).ExecuteUpdateAsync(s => s.SetProperty(x => x.ObligationScanHash, (string?)null));
                    var r = await svc.ScanBeatAsync(nodeId, beatId, BeatMarkup.StripEntityTags(beat.Text), ObligationActor.SystemRescan);
                    if (r.Skipped) { skipped++; continue; }
                    scanned++; opened += r.Opened; advanced += r.Advanced; closed += r.Closed; ungrounded += r.DiscardedUngrounded;
                    if (!r.Evaluated) notEvaluated++;
                    if (scanned % 10 == 0) Console.WriteLine($"  … {scanned} scanned, {opened} opened, {closed} closed");
                }
                Console.WriteLine($"  scanned {scanned} of {ordered.Count} (skipped {skipped} unchanged) — opened {opened}, advanced {advanced}, closed {closed}, ungrounded discarded {ungrounded}, not evaluated {notEvaluated}");
                if (scanned == 0 && skipped == 0) Console.WriteLine("  COULD NOT LOOK — no beats.");
                return notEvaluated > 0 ? 1 : 0;
            }
            case "completeness":
            {
                // Does this text finish its own story? It is the one fact the calibration bar needs
                // and could not ask for (RFC 0013 §6a). Deliberately NOT derived from Status: that
                // answers "have we reached the end of the text we hold", which is a different
                // question — GCTOC is "Complete - publication ready" AND is act one of three.
                await using var db = await dbFactory.CreateDbContextAsync();
                var node = await db.Nodes.IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Id == nodeId);
                if (node == null) { Console.Error.WriteLine("[obligations] node not found."); return 1; }

                var arg = args.SkipWhile(a => a != "--complete").Skip(1).FirstOrDefault();
                if (arg != null)
                {
                    bool? value = arg.ToLowerInvariant() switch
                    {
                        "true" or "yes" or "1"  => true,
                        "false" or "no" or "0"  => false,
                        "unset" or "null"       => null,
                        _ => throw new ArgumentException($"--complete expects true|false|unset, got '{arg}'"),
                    };
                    node.StructurallyComplete = value;
                    await db.SaveChangesAsync();
                    Console.WriteLine($"[obligations] {title}: StructurallyComplete = {Describe(value)}");
                    return 0;
                }

                Console.WriteLine($"[obligations] COMPLETENESS — {title}");
                Console.WriteLine($"  Status (is the text at its end?):      {node.Status}");
                Console.WriteLine($"  StructurallyComplete (does it finish its own story?): {Describe(node.StructurallyComplete)}");
                if (node.StructurallyComplete == null)
                {
                    Console.WriteLine("  NOT SET — calibration scores this book STRICT (every control finding counts).");
                    Console.WriteLine("  Set it: prose --obligations completeness --slug <slug> --complete true|false");
                }
                return node.StructurallyComplete == null ? 1 : 0;

                static string Describe(bool? v) => v switch
                {
                    true  => "true — finishes its own story; a debt still open at the end was abandoned and counts against the bar",
                    false => "false — a fragment of a larger work; outstanding-at-end is expected and is scored separately",
                    null  => "(not set)",
                };
            }
            case "coverage":
            {
                // READ-ONLY. Which beats the extractor actually READ, from the stamps the last scan
                // left behind. A beat whose extractor response could not be parsed is left unstamped
                // and contributes nothing, so an unread beat is indistinguishable from a beat that
                // owes nothing — except here. Lengths are reported because the leading suspect is
                // truncation at the token ceiling, which would make the UNREAD beats systematically
                // longer than the read ones.
                await using var db = await dbFactory.CreateDbContextAsync();
                var clock = await NarrativeObligationService.LoadClockAsync(db, nodeId, CancellationToken.None);
                var ids = clock.Beats.Keys.ToList();
                var beats = await db.Beats.AsNoTracking().Where(b => ids.Contains(b.Id))
                    .Select(b => new { b.Id, b.Text, b.ObligationScanHash }).ToListAsync();
                var openedBy = (await db.NarrativeObligations.AsNoTracking()
                        .Where(o => o.NodeId == nodeId && o.OriginBeatId != null)
                        .Select(o => o.OriginBeatId!.Value).ToListAsync())
                    .GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());

                var rows = beats.Select(b =>
                {
                    var collapsed = QuoteGrounding.Normalize(BeatMarkup.StripEntityTags(b.Text ?? ""));
                    return new
                    {
                        b.Id,
                        Read = !string.IsNullOrEmpty(b.ObligationScanHash),
                        Chapter = clock.ChapterOf(b.Id),
                        Position = clock.PositionOf(b.Id),
                        Chars = collapsed.Length,
                        Windows = NarrativeObligationExtractor.SplitIntoWindows(collapsed, NarrativeObligationExtractor.MaxBeatChars).Count,
                        Opened = openedBy.TryGetValue(b.Id, out var n) ? n : 0,
                    };
                }).OrderBy(r => r.Position).ToList();

                var read = rows.Where(r => r.Read).ToList();
                var unread = rows.Where(r => !r.Read).ToList();

                // Index coverage, not just read coverage. The resurfacing judge retrieves candidate
                // payoff beats from ProseEmbeddings, and nothing maintains that index when a beat is
                // created — SemanticFidelityService is the only path that refreshes it, and
                // UpdateBeatTextAsync runs it only when !deferAnalysis AND the beat has a
                // Description. Beats made by split_beat have neither. GCSH was split from 12 beats
                // to 96 and the index kept the 12, so the judge has been choosing candidates from a
                // stub for the whole calibration programme, reporting no error to anyone.
                var embeddedIds = await db.ProseEmbeddings.AsNoTracking()
                    .Where(e => e.ScopeKind == EmbeddingService.ScopeBeatNode && ids.Contains(e.ScopeId))
                    .Select(e => e.ScopeId).ToListAsync();
                var embedded = embeddedIds.ToHashSet();
                var unembedded = rows.Where(r => !embedded.Contains(r.Id)).ToList();
                static string Stats(IReadOnlyList<int> xs)
                {
                    if (xs.Count == 0) return "—";
                    var s = xs.OrderBy(x => x).ToList();
                    return $"median {s[s.Count / 2]:N0}, mean {xs.Average():N0}, max {xs.Max():N0}";
                }

                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new
                    {
                        node_id = nodeId, title, beats_total = rows.Count, beats_read = read.Count, beats_unread = unread.Count,
                        max_beat_chars = NarrativeObligationExtractor.MaxBeatChars,
                        beats_embedded = embedded.Count, beats_unembedded = unembedded.Count,
                        unread = unread.Select(r => new { beat_id = r.Id.ToString("N"), r.Chapter, r.Position, r.Chars, r.Windows }),
                        read_chars = read.Select(r => r.Chars), unread_chars = unread.Select(r => r.Chars),
                    }, Json));
                    return unread.Count == 0 && unembedded.Count == 0 ? 0 : 1;
                }

                Console.WriteLine($"[obligations] COVERAGE — {title}");
                Console.WriteLine($"  beats read by the extractor: {read.Count}/{rows.Count}" + (unread.Count == 0 ? "" : $"   UNREAD: {unread.Count}"));
                Console.WriteLine($"  read   beat length (chars): {Stats(read.Select(r => r.Chars).ToList())}");
                Console.WriteLine($"  unread beat length (chars): {Stats(unread.Select(r => r.Chars).ToList())}");
                Console.WriteLine($"  window size: {NarrativeObligationExtractor.MaxBeatChars} chars · multi-window beats: {rows.Count(r => r.Windows > 1)} of {rows.Count} (unread: {unread.Count(r => r.Windows > 1)})");
                if (unread.Count > 0)
                {
                    Console.WriteLine("\n  UNREAD BEATS (nothing in them was ever extracted):");
                    foreach (var r in unread)
                        Console.WriteLine($"    Ch{r.Chapter,-3} pos {r.Position,-4} {r.Id:N}  {r.Chars,6:N0} chars  {r.Windows} window(s)");
                    Console.WriteLine("\n  If the unread beats are systematically longer than the read ones, the extractor response is");
                    Console.WriteLine("  being cut at the token ceiling and the parse fails — a defect in the instrument, not the book.");
                }
                else Console.WriteLine("\n  Every beat was read. A finding of zero here means zero, not \"could not look\".");

                Console.WriteLine($"\n  RETRIEVAL INDEX — beats with a ProseEmbeddings row: {embedded.Count}/{rows.Count}"
                    + (unembedded.Count == 0 ? "" : $"   MISSING: {unembedded.Count}"));
                if (unembedded.Count > 0)
                {
                    Console.WriteLine("  The resurfacing judge picks candidate payoff beats from this index. A beat that is not in");
                    Console.WriteLine("  it can never be retrieved, so its payoff can never be recognised — and the judge reports");
                    Console.WriteLine("  \"not_addressed\", which reads as a recognition failure rather than a missing index.");
                    Console.WriteLine($"  Backfill (cheap, drift-skipped): prose --reembed --beats --slug <slug>");
                }
                return unread.Count == 0 && unembedded.Count == 0 ? 0 : 1;
            }
            case "import-bible-ledger":
            {
                var importer = services.GetRequiredService<BibleLedgerImporter>();
                var dryRun = args.Contains("--dry-run");
                var report = await importer.ImportAsync(nodeId, dryRun);
                if (json) { Console.WriteLine(JsonSerializer.Serialize(new { title, report.NodeId, report.Source, report.DryRun, report.CouldNotLook, closed_rows = report.ClosedRows, dropped_rows = report.DroppedRows, report.Created, report.Existing, needs_anchor_rows = report.NeedsAnchorRows, report.Outcomes, report.Warnings }, Json)); return report.CouldNotLook ? 1 : 0; }
                Console.WriteLine($"[obligations] import-bible-ledger — {title}{(dryRun ? " (DRY RUN — nothing written)" : "")}");
                if (report.CouldNotLook)
                {
                    Console.WriteLine("  COULD NOT LOOK — no §14a/§14b table in this node's bible (NodeOutlineSections or NodeOutline). Nothing imported.");
                    return 1;
                }
                Console.WriteLine($"  source: {report.Source}");
                Console.WriteLine($"  §14a closed plants: {report.ClosedRows}   §14b dropped findings: {report.DroppedRows}   created {report.Created}, already present {report.Existing}, NEED AN ANCHOR {report.NeedsAnchorRows}");
                foreach (var o in report.Outcomes)
                {
                    var anchors = $"origin {(o.OriginBeatId is Guid ob ? ob.ToString("N") : "—")} · closing {(o.ClosingBeatId is Guid cb ? cb.ToString("N") : "—")}";
                    Console.WriteLine($"  {(o.NeedsAnchor.Count > 0 ? "!" : " ")} §{o.Table} [{o.Kind}] {o.State,-7} {o.Action,-8} {(o.ObligationId is Guid id ? id.ToString("N") : new string(' ', 32))}  {Trunc(o.Description, 110)}");
                    Console.WriteLine($"        {anchors}");
                    foreach (var n in o.NeedsAnchor) Console.WriteLine($"        NEEDS ANCHOR: {n}");
                    if (o.Warning != null) Console.WriteLine($"        WARNING: {o.Warning}");
                }
                foreach (var w in report.Warnings) Console.WriteLine($"  warning: {w}");
                if (report.NeedsAnchorRows > 0) Console.WriteLine("  Rows marked ! carry a null anchor on purpose — the bible label no longer matches the tree. Anchor them by hand (`prose --obligations close --id … --beat-id … --quote …`) or leave them; they raise no finding.");
                return 0;
            }
            default:
                Console.Error.WriteLine($"[obligations] unknown mode '{mode}'.");
                return 2;
        }
    }

    private sealed record CacheRow(string PromptVersion, string Relation, string? Quote);

    private sealed record PayoffProbe(
        string BeatId, bool InBook, int Chapter, int Position, bool AfterOrigin, bool InFinder,
        IReadOnlyList<CacheRow> CacheRows, int? LexicalScore, int? LexicalRank, bool MeetsLexicalFloor,
        double? EmbeddingSimilarity, int? EmbeddingRank, string Verdict);

    /// <summary>
    /// READ-ONLY retrieval diagnostic (RFC 0013). Re-runs the resurfacing judge's candidate finder
    /// for one obligation and reports, for each named payoff beat, whether it was ever in the
    /// candidate set. A payoff the judge saw and still called not_addressed is a recognition or
    /// granularity problem; a payoff the finder never surfaced is a retrieval-budget problem. The
    /// two demand opposite fixes, so the ranks below are the evidence that picks one. Costs one
    /// embedding query and no LLM call; writes nothing.
    /// </summary>
    private static async Task<int> RunCandidatesAsync(Guid oid, string[] args, bool json, IServiceProvider services, IDbContextFactory<ProseDbContext> dbFactory)
    {
        var payoffs = new List<Guid>();
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i] == "--payoff-beat" && Guid.TryParse(args[i + 1], out var pb)) payoffs.Add(pb);

        await using var db = await dbFactory.CreateDbContextAsync();
        var o = await db.NarrativeObligations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == oid);
        if (o == null) { Console.Error.WriteLine("[obligations] not found."); return 1; }

        var clock = await NarrativeObligationService.LoadClockAsync(db, o.NodeId, CancellationToken.None);
        var finder = services.GetRequiredService<IObligationCandidateFinder>();
        var k = ObligationResurfacingJudge.CandidatesPerObligation;
        var found = await finder.FindAsync(o, clock, k, CancellationToken.None);

        var cache = await db.ObligationJudgeCache.AsNoTracking()
            .Where(c => c.ObligationId == oid).OrderBy(c => c.CreatedAt).ToListAsync();

        // The deterministic half of the finder, recomputed over every later beat so a payoff that
        // missed the cut gets a rank instead of a bare "absent".
        var originPos = o.OriginBeatId is Guid ob ? clock.PositionOf(ob) : -1;
        var laterIds = clock.Beats.Where(kv => kv.Value.Position > originPos).Select(kv => kv.Key).ToList();
        var words = LexicalCandidateFinder.ContentWords(o.Description + " " + (o.OriginQuote ?? ""));
        var texts = await db.Beats.AsNoTracking().Where(b => laterIds.Contains(b.Id))
            .Select(b => new { b.Id, b.Text }).ToListAsync();
        var lexRank = texts
            .Select(b => (b.Id, Score: words.Count(w => (b.Text ?? "").Contains(w, StringComparison.OrdinalIgnoreCase))))
            .OrderByDescending(x => x.Score)
            .Select((x, i) => (x.Id, x.Score, Rank: i + 1))
            .ToDictionary(x => x.Id, x => (x.Score, x.Rank));

        // Embedding ranks over a deep sweep: the finder only ever asks for k*4, so a payoff outside
        // that budget is invisible to it — this says how far outside.
        const int DeepK = 400;
        var embRank = new Dictionary<Guid, (double Similarity, int Rank)>();
        string embNote;
        var embeddings = services.GetService<EmbeddingService>();
        if (embeddings == null) embNote = "EmbeddingService not registered — lexical only";
        else
        {
            try
            {
                var hits = await embeddings.FindSimilarBeatNodesAsync(o.Description + " " + (o.OriginQuote ?? ""), DeepK, null, CancellationToken.None);
                var later = hits.Where(h => clock.Beats.TryGetValue(h.ScopeId, out var p) && p.Position > originPos)
                                .OrderByDescending(h => h.Similarity).ToList();
                for (var i = 0; i < later.Count; i++) embRank[later[i].ScopeId] = (later[i].Similarity, i + 1);
                embNote = $"{later.Count} of {hits.Count} top-{DeepK} corpus hits fall after the origin";
            }
            catch (Exception ex) { embNote = "embedding sweep FAILED (lexical ranks still valid): " + ex.Message; }
        }

        var probes = new List<PayoffProbe>();
        foreach (var p in payoffs)
        {
            var inBook = clock.Beats.ContainsKey(p);
            var pos = clock.PositionOf(p);
            var after = pos > originPos;
            var inFinder = found.Contains(p);
            var rows = cache.Where(c => c.CandidateBeatId == p)
                .Select(c => new CacheRow(c.PromptVersion, c.Relation, c.Quote)).ToList();
            var lex = lexRank.TryGetValue(p, out var lr) ? lr : ((int Score, int Rank)?)null;
            var emb = embRank.TryGetValue(p, out var er) ? er : ((double Similarity, int Rank)?)null;
            var verdict =
                !inBook  ? "NOT IN THIS BOOK — the beat id does not belong to this obligation's tree"
                : !after ? "BEFORE THE ORIGIN — the finder only ever looks after the opening beat, so this can never be a candidate"
                : inFinder ? (rows.Count > 0
                    ? "RETRIEVED AND JUDGED — the judge saw this text and still did not close on it: a recognition/granularity problem, not retrieval"
                    : "RETRIEVED, NOT YET JUDGED — in the candidate set but no cache row at any prompt version")
                : rows.Count > 0 ? "NOT IN THE CURRENT CANDIDATE SET, but judged earlier — retrieval moved under it"
                : "NEVER RETRIEVED — the judge was never shown this text: a retrieval-budget problem, not recognition";
            probes.Add(new PayoffProbe(p.ToString("N"), inBook, clock.ChapterOf(p), pos, after, inFinder,
                rows, lex?.Score, lex?.Rank, lex is { Score: >= 2 }, emb?.Similarity, emb?.Rank, verdict));
        }

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                obligation = new
                {
                    id = o.Id.ToString("N"), node_id = o.NodeId, kind = o.Kind, state = o.State.ToString(),
                    description = o.Description, origin_quote = o.OriginQuote,
                    origin_beat = o.OriginBeatId?.ToString("N"), origin_chapter = o.OriginBeatId is Guid g ? clock.ChapterOf(g) : 0,
                },
                retrieval = new
                {
                    finder = finder.GetType().Name, candidates_per_obligation = k,
                    later_beats = laterIds.Count, content_words = words, embedding_note = embNote,
                    candidates = found.Select(id => new
                    {
                        beat_id = id.ToString("N"), chapter = clock.ChapterOf(id), position = clock.PositionOf(id),
                        lexical_score = lexRank.TryGetValue(id, out var l) ? l.Score : (int?)null,
                        embedding_rank = embRank.TryGetValue(id, out var e) ? e.Rank : (int?)null,
                        relation = cache.FirstOrDefault(c => c.CandidateBeatId == id)?.Relation,
                    }).ToList(),
                },
                judge_cache = cache.Select(c => new { beat_id = c.CandidateBeatId.ToString("N"), chapter = clock.ChapterOf(c.CandidateBeatId), c.PromptVersion, c.Relation, c.Quote }).ToList(),
                payoffs = probes,
            }, Json));
            return 0;
        }

        Console.WriteLine($"[obligations] candidates — {o.Kind} · {o.State} · {o.Id:N}");
        Console.WriteLine($"  {Trunc(o.Description, 150)}");
        if (!string.IsNullOrEmpty(o.OriginQuote)) Console.WriteLine($"  origin quote: \"{Trunc(o.OriginQuote, 130)}\"");
        Console.WriteLine($"  origin: {(o.OriginBeatId is Guid g2 ? $"Ch{clock.ChapterOf(g2)} beat {g2:N} (position {originPos})" : "— none")}");
        Console.WriteLine($"  finder: {finder.GetType().Name}, k={k} · {laterIds.Count} later beat(s) in range · {embNote}");
        Console.WriteLine($"  content words ({words.Count}): {string.Join(" ", words.Take(24))}{(words.Count > 24 ? " …" : "")}");
        Console.WriteLine($"\n  CANDIDATE SET ({found.Count}):");
        foreach (var id in found)
        {
            var rel = cache.FirstOrDefault(c => c.CandidateBeatId == id)?.Relation ?? "—";
            var l = lexRank.TryGetValue(id, out var lv) ? lv.Score.ToString() : "—";
            var e = embRank.TryGetValue(id, out var ev) ? $"#{ev.Rank} ({ev.Similarity:F3})" : "—";
            Console.WriteLine($"    Ch{clock.ChapterOf(id),-3} {id:N}  lex {l,-3} emb {e,-16} judged {rel}");
        }
        if (found.Count == 0) Console.WriteLine("    (none — the finder returned nothing; the judge counts this obligation not addressed without a call)");

        if (cache.Count > 0)
        {
            Console.WriteLine($"\n  JUDGE CACHE ({cache.Count} row(s)):");
            foreach (var c in cache)
                Console.WriteLine($"    Ch{clock.ChapterOf(c.CandidateBeatId),-3} {c.CandidateBeatId:N}  {c.PromptVersion,-14} {c.Relation,-13} {(c.Quote == null ? "" : "\"" + Trunc(c.Quote, 90) + "\"")}");
        }
        else Console.WriteLine("\n  JUDGE CACHE: empty — this obligation has never been judged (a reset drops the cache).");

        foreach (var p in probes)
        {
            Console.WriteLine($"\n  PAYOFF {p.BeatId} — Ch{p.Chapter}, position {p.Position}");
            Console.WriteLine($"    in candidate set: {(p.InFinder ? "YES" : "no")}   lexical: {(p.LexicalScore is int s ? $"score {s}, rank #{p.LexicalRank} of {lexRank.Count}{(p.MeetsLexicalFloor ? "" : " — BELOW the score>=2 floor, the lexical finder can never return it")}" : "not scored")}");
            Console.WriteLine($"    embedding: {(p.EmbeddingRank is int r ? $"rank #{r} of the later-beat sweep (similarity {p.EmbeddingSimilarity:F3}) — the finder only takes the top {k}" : $"outside the top {DeepK} of the corpus")}");
            foreach (var row in p.CacheRows) Console.WriteLine($"    cached verdict: {row.PromptVersion} → {row.Relation}");
            Console.WriteLine($"    → {p.Verdict}");
        }
        if (probes.Count == 0) Console.WriteLine("\n  (no --payoff-beat given — pass one or more to probe whether a specific payoff was ever retrievable)");
        return 0;
    }

    private static string Trunc(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";

    private static async Task<int> RunByIdAsync(string mode, Guid id, string[] args, Func<string, string?> Flag, bool json, NarrativeObligationService svc)
    {
        const string actor = ObligationActor.AuthorCli;
        switch (mode)
        {
            case "history":
            {
                var row = await svc.GetAsync(id);
                if (row == null) { Console.Error.WriteLine("[obligations] not found."); return 1; }
                var events = await svc.HistoryAsync(id);
                if (json) { Console.WriteLine(JsonSerializer.Serialize(new { obligation = row, events }, Json)); return 0; }
                Console.WriteLine($"[obligations] {row.Kind} · {row.State} · {row.Provenance}{(row.AuthorLocked ? " · locked" : "")}\n  {row.Description}\n  quote: {row.OriginQuote}");
                foreach (var e in events) Console.WriteLine($"  {e.CreatedAt:u} {e.Action,-11} {e.Actor,-16} {(e.BeatId is Guid b ? $"beat {b} " : "")}{e.Note}{(e.Quote != null ? $" — \"{e.Quote}\"" : "")}");
                return 0;
            }
            case "close":
            {
                if (!Guid.TryParse(Flag("--beat-id"), out var beatId)) { Console.Error.WriteLine("[obligations] --beat-id is required."); return 2; }
                var quote = Flag("--quote");
                if (string.IsNullOrWhiteSpace(quote)) { Console.Error.WriteLine("[obligations] --quote (verbatim from the closing beat) is required."); return 2; }
                return Report(await svc.CloseAsync(id, beatId, quote, Flag("--note"), actor), json);
            }
            case "drop":
            {
                var reason = Flag("--reason"); var note = Flag("--note");
                if (reason == null || note == null) { Console.Error.WriteLine("[obligations] --reason and --note are both required to drop."); return 2; }
                return Report(await svc.DropAsync(id, reason, note, actor), json);
            }
            case "defer":
            {
                var note = Flag("--note");
                if (note == null) { Console.Error.WriteLine("[obligations] --note is required to defer."); return 2; }
                var (dueKind, dueValue) = ParseDue(Flag("--due"));
                return Report(await svc.DeferAsync(id, note, dueKind, dueValue, actor), json);
            }
            case "reopen": return Report(await svc.ReopenAsync(id, Flag("--note"), actor), json);
            case "due":
            {
                var (dueKind, dueValue) = ParseDue(Flag("--due"));
                return Report(await svc.SetDueAsync(id, dueKind, dueValue, actor), json);
            }
            case "accept": return Report(await svc.LockAsync(id, actor), json);
        }
        return 2;
    }

    internal static (string Kind, int? Value) ParseDue(string? due)
    {
        if (string.IsNullOrWhiteSpace(due) || due.Equals("book-end", StringComparison.OrdinalIgnoreCase)) return (ObligationDueKind.BookEnd, null);
        var parts = due.Split(':', 2);
        if (parts.Length == 2 && int.TryParse(parts[1], out var n))
        {
            if (parts[0].Equals("chapter", StringComparison.OrdinalIgnoreCase)) return (ObligationDueKind.Chapter, n);
            if (parts[0].Equals("beats",   StringComparison.OrdinalIgnoreCase)) return (ObligationDueKind.Beats, n);
        }
        return (ObligationDueKind.BookEnd, null);
    }

    private static int Report(NarrativeObligationService.AuthorResult res, bool json)
    {
        if (json) { Console.WriteLine(JsonSerializer.Serialize(new { ok = res.Ok, error = res.Error, obligation = res.Row }, Json)); return res.Ok ? 0 : 1; }
        if (!res.Ok) { Console.Error.WriteLine($"[obligations] {res.Error}"); return 1; }
        Console.WriteLine($"[obligations] ok — {res.Row!.Id} is now {res.Row.State}{(res.Row.AuthorLocked ? " (locked)" : "")}");
        return 0;
    }

    private static string Line(NarrativeObligationService.ObligationView v) =>
        $"  {(v.Overdue && !v.AuthorLocked ? "!" : " ")} {v.Id:N} [{v.Kind}] Ch{v.OriginChapter} {v.State,-9} {v.Provenance,-8} due {v.Due,-10} {v.Description}" +
        (v.OriginQuote != null ? $"\n        \"{v.OriginQuote}\"" : "");

    private static void PrintTrialBalance(string title, NarrativeObligationService.TrialBalance tb)
    {
        var scope = tb.ChapterOrdinal is int c ? $"through Ch{c} of {tb.ChapterCount}" : $"whole book ({tb.ChapterCount} chapters)";
        Console.WriteLine($"[obligations] TRIAL BALANCE — {title} — {scope}");
        Console.WriteLine($"  ledger scan coverage : {tb.ScannedBeats}/{tb.TotalBeats} beats");
        if (tb.CouldNotLook) { Console.WriteLine("  COULD NOT LOOK — the ledger is empty; run `prose --obligations rescan --slug …` first. An empty ledger fails, it does not pass."); return; }
        Console.WriteLine($"  opened {tb.Opened} − closed {tb.Closed} − dropped {tb.Dropped} − deferred {tb.Deferred} = carried forward {tb.CarriedForward}  (advanced {tb.Advanced}, withdrawn {tb.Withdrawn})");
        Console.WriteLine($"  OVERDUE WITHOUT DECISION: {tb.OverdueWithoutDecision.Count}");
        foreach (var v in tb.OverdueWithoutDecision) Console.WriteLine(Line(v));
        if (tb.StaleClosures.Count > 0) { Console.WriteLine($"  STALE CLOSURES (closing quote no longer on the page): {tb.StaleClosures.Count}"); foreach (var v in tb.StaleClosures) Console.WriteLine(Line(v)); }
        if (tb.DanglingOrigins.Count > 0) { Console.WriteLine($"  DANGLING ORIGINS (origin beat gone): {tb.DanglingOrigins.Count}"); foreach (var v in tb.DanglingOrigins) Console.WriteLine(Line(v)); }
        Console.WriteLine(tb.Balanced ? "  RESULT: BALANCED" : "  RESULT: NOT BALANCED — decide each overdue row (close / drop / defer) before this chapter closes.");
    }

    private static async Task<Guid> BookRootAsync(ProseDbContext db, Guid nodeId)
    {
        var walk = nodeId;
        for (var depth = 0; depth < 10; depth++)
        {
            var parent = await db.Nodes.IgnoreQueryFilters().AsNoTracking().Where(n => n.Id == walk).Select(n => n.ParentNodeId).FirstOrDefaultAsync();
            if (parent == null) return walk;
            walk = parent.Value;
        }
        return walk;
    }
}
