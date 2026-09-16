using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;
using Prose.Core.Services.Audit;

namespace Prose.Core.Services.Obligations;

/// <summary>Finds the later beats most likely to pay an open obligation. Behind an interface so
/// tests run a lexical finder (SQLite has no VECTOR_DISTANCE) and production adds embeddings.</summary>
public interface IObligationCandidateFinder
{
    Task<IReadOnlyList<Guid>> FindAsync(NarrativeObligation obligation, NarrativeObligationService.BookClock clock, int k, CancellationToken ct);
}

/// <summary>Content-word overlap between the obligation (description + quote) and every later
/// beat. Free, deterministic, and the floor the embedding finder is unioned with.</summary>
public class LexicalCandidateFinder(IDbContextFactory<ProseDbContext> dbFactory) : IObligationCandidateFinder
{
    private static readonly HashSet<string> Stop = new(StringComparer.OrdinalIgnoreCase)
    {
        "the","a","an","and","or","but","of","to","in","on","at","by","for","with","from","that","this","it","he","she",
        "they","his","her","their","was","were","is","are","be","been","had","has","have","not","no","as","into","who",
        "what","when","where","why","how","will","would","could","never","again","still","then","than","there","here",
    };

    public static IReadOnlyList<string> ContentWords(string text) =>
        text.ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Trim('"', '\'', '.', ',', ';', ':', '!', '?', '(', ')', '—', '-'))
            .Where(w => w.Length > 3 && !Stop.Contains(w))
            .Distinct()
            .ToList();

    public async Task<IReadOnlyList<Guid>> FindAsync(NarrativeObligation o, NarrativeObligationService.BookClock clock, int k, CancellationToken ct)
    {
        var originPos = o.OriginBeatId is Guid ob ? clock.PositionOf(ob) : -1;
        var laterIds = clock.Beats.Where(kv => kv.Value.Position > originPos).Select(kv => kv.Key).ToList();
        if (laterIds.Count == 0) return [];

        var words = ContentWords(o.Description + " " + (o.OriginQuote ?? ""));
        if (words.Count == 0) return [];

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var beats = await db.Beats.AsNoTracking().Where(b => laterIds.Contains(b.Id)).Select(b => new { b.Id, b.Text }).ToListAsync(ct);
        return beats
            .Select(b => (b.Id, Score: words.Count(w => (b.Text ?? "").Contains(w, StringComparison.OrdinalIgnoreCase))))
            .Where(x => x.Score >= 2)
            .OrderByDescending(x => x.Score)
            .Take(k)
            .Select(x => x.Id)
            .ToList();
    }
}

/// <summary>Embedding neighbours (server-side VECTOR_DISTANCE via <see cref="EmbeddingService"/>)
/// restricted to beats after the origin, unioned with the lexical finder.</summary>
public class EmbeddingCandidateFinder(EmbeddingService embeddings, LexicalCandidateFinder lexical, ILogger<EmbeddingCandidateFinder> log) : IObligationCandidateFinder
{
    public async Task<IReadOnlyList<Guid>> FindAsync(NarrativeObligation o, NarrativeObligationService.BookClock clock, int k, CancellationToken ct)
    {
        var originPos = o.OriginBeatId is Guid ob ? clock.PositionOf(ob) : -1;
        var result = new List<Guid>();
        try
        {
            var hits = await embeddings.FindSimilarBeatNodesAsync(o.Description + " " + (o.OriginQuote ?? ""), k * 4, null, ct);
            result.AddRange(hits.Where(h => clock.Beats.TryGetValue(h.ScopeId, out var p) && p.Position > originPos)
                                .OrderByDescending(h => h.Similarity).Select(h => h.ScopeId).Take(k));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.LogWarning(ex, "Embedding candidate search failed; falling back to lexical only");
        }
        foreach (var id in await lexical.FindAsync(o, clock, k, ct))
            if (!result.Contains(id)) result.Add(id);
        return result.Take(k + 2).ToList();
    }
}

/// <summary>
/// The paid half of <c>--reconcile-obligations --deep</c> (RFC 0013 D6b). For every outstanding
/// obligation: gather candidate later beats, ask one Haiku call which of them closes or advances
/// the debt — reason first, verbatim quote required — and discard any verdict whose quote the
/// candidate text does not contain. Verdicts are cached by candidate text hash so an unchanged
/// book is never re-judged. This is also what covers the ordering gap: an edit that opens a new
/// obligation at beat k does not re-scan k+1..N, but this pass finds its payoff if one exists.
/// </summary>
public class ObligationResurfacingJudge(
    IDbContextFactory<ProseDbContext> dbFactory,
    ILlmService llm,
    IObligationCandidateFinder finder,
    ILogger<ObligationResurfacingJudge> log)
{
    /// <summary>Cache key. v2 (2026-09-15): a candidate longer than <see cref="MaxCandidateWords"/>
    /// is listed as consecutive parts instead of being cut — v1's cut dropped the tail of every
    /// ~1,100-word beat, which is where GCSH calibration run 2 had all four injected payoffs
    /// (4/4 "not recognised" with the judge running). v1 verdicts on cut passages are void.
    /// v3 (2026-09-16): a "closes"/"advances" verdict is now also vetoed when the quote shares no
    /// content word with the obligation (see <see cref="SharesContent"/>) — run 3 closed "the
    /// stopped clock" with an unrelated grounded-but-irrelevant quote. Bumped so every open
    /// obligation is re-judged under the new gate rather than replaying v2's ungated verdicts.</summary>
    public const string PromptVersion = "obl-judge-v3";
    public const int CandidatesPerObligation = 8;
    /// <summary>Words per listed passage. Never a cut: see <see cref="SplitPassages"/>.</summary>
    public const int MaxCandidateWords = 600;

    public sealed record DeepResult(int Examined, int Closed, int Advanced, int NotAddressed, int DiscardedUngrounded, int CacheHits, int LlmCalls, bool Evaluated);

    private const string SystemPrompt = """
        You are the continuity ledger of a novel. You are given ONE open obligation — a promise the
        book made to the reader — and several later passages that might pay it. Decide, for each
        passage, whether it CLOSES the obligation (definitively pays it), ADVANCES it (returns to it
        without paying it), or does NOT ADDRESS it. Reason first.

        Return ONE JSON object and nothing else:
          "reasoning": 2-5 sentences.
          "verdicts": array with one object per passage, keys in order:
              "beat_number": the passage's number as listed.
              "relation": "closes" | "advances" | "not_addressed"
              "quote": for closes/advances, a VERBATIM sentence (≥12 characters) from THAT passage
                       that does the closing/advancing; null for not_addressed.
        Be conservative: "closes" only when a reader would consider the debt paid.
        """;

    public async Task<DeepResult> RunAsync(Guid bookNodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var clock = await NarrativeObligationService.LoadClockAsync(db, bookNodeId, ct);
        var open = await db.NarrativeObligations
            .Where(o => o.NodeId == bookNodeId && (o.State == ObligationState.Open || o.State == ObligationState.Advanced))
            .OrderBy(o => o.CreatedAt).ToListAsync(ct);

        int examined = 0, closed = 0, advanced = 0, notAddressed = 0, discarded = 0, cacheHits = 0, calls = 0;
        var evaluated = true;

        foreach (var o in open)
        {
            ct.ThrowIfCancellationRequested();
            var candidateIds = await finder.FindAsync(o, clock, CandidatesPerObligation, ct);
            if (candidateIds.Count == 0) { examined++; notAddressed++; continue; }

            var candidates = await db.Beats.AsNoTracking().Where(b => candidateIds.Contains(b.Id))
                .Select(b => new { b.Id, b.Text, b.TextHash }).ToListAsync(ct);
            candidates = candidates.OrderBy(c => clock.PositionOf(c.Id)).ToList();

            // Cache: skip candidates already judged at this exact text.
            var hashes = candidates.Select(c => c.TextHash ?? "").ToList();
            var cached = await db.ObligationJudgeCache
                .Where(c => c.ObligationId == o.Id && c.PromptVersion == PromptVersion && hashes.Contains(c.CandidateTextHash))
                .ToListAsync(ct);
            var toJudge = candidates.Where(c => !cached.Any(k => k.CandidateBeatId == c.Id && k.CandidateTextHash == (c.TextHash ?? ""))).ToList();
            cacheHits += candidates.Count - toJudge.Count;

            var verdicts = cached.Select(k => (BeatId: k.CandidateBeatId, k.Relation, k.Quote)).ToList();

            if (toJudge.Count > 0)
            {
                // Every candidate is listed WHOLE: a long beat becomes consecutive numbered parts.
                // Quotes are gated against the full beat text, and the verdicts for one beat's
                // parts merge into one cache row (closes > advances > not_addressed).
                var fullText = toJudge.ToDictionary(c => c.Id, c => BeatMarkup.StripEntityTags(c.Text));
                var listed = new List<(int Number, Guid BeatId, string Text, string? TextHash, int Part, int Parts)>();
                foreach (var c in toJudge)
                {
                    var parts = SplitPassages(fullText[c.Id], MaxCandidateWords);
                    for (var i = 0; i < parts.Count; i++)
                        listed.Add((listed.Count + 1, c.Id, parts[i], c.TextHash, i + 1, parts.Count));
                }
                var user = $"""
                    OBLIGATION [{o.Kind}] (opened Ch{(o.OriginBeatId is Guid ob ? clock.ChapterOf(ob) : 0)}):
                    {o.Description}
                    {(string.IsNullOrEmpty(o.OriginQuote) ? "" : $"Origin quote: \"{o.OriginQuote}\"")}

                    LATER PASSAGES (a long passage is split into consecutive parts; judge each part on its own text):
                    {string.Join("\n\n", listed.Select(l => $"[{l.Number}] (Ch{clock.ChapterOf(l.BeatId)}{(l.Parts > 1 ? $", part {l.Part} of {l.Parts}" : "")})\n{l.Text}"))}
                    """;
                string raw;
                try { raw = await llm.GenerateAsync(SystemPrompt, user, temperature: 0.1, maxTokens: 1200, model: LlmModels.Haiku, ct: ct); calls++; }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    log.LogWarning(ex, "Resurfacing judge call failed for obligation {Id}", o.Id);
                    evaluated = false;
                    continue;
                }

                var perBeat = new Dictionary<Guid, (string Relation, string? Quote)>();
                foreach (var (number, relation, quote) in Parse(raw))
                {
                    if (number < 1 || number > listed.Count) continue;
                    var cand = listed[number - 1];
                    var rel = relation;
                    string? q = quote;
                    if (rel is "closes" or "advances")
                    {
                        if (!QuoteGrounding.Contains(fullText[cand.BeatId], q, QuoteGrounding.MinObligationQuoteLength)) { rel = "ungrounded"; q = null; discarded++; }
                        else if (!SharesContent(o, q!)) { rel = "irrelevant"; q = null; discarded++; }
                        else q = QuoteGrounding.ClampForStorage(q);   // nvarchar(400) — a paragraph is not a quote
                    }
                    if (!perBeat.TryGetValue(cand.BeatId, out var best) || Rank(rel) > Rank(best.Relation))
                        perBeat[cand.BeatId] = (rel, q);
                }
                foreach (var c in toJudge)
                {
                    // A candidate the model did not mention in any part counts as not addressed.
                    var (rel, q) = perBeat.TryGetValue(c.Id, out var v) ? v : ("not_addressed", null);
                    db.ObligationJudgeCache.Add(new ObligationJudgeCache
                    {
                        ObligationId = o.Id, CandidateBeatId = c.Id, CandidateTextHash = c.TextHash ?? "",
                        PromptVersion = PromptVersion, Relation = rel, Quote = q,
                    });
                    verdicts.Add((c.Id, rel, q));
                }
                await db.SaveChangesAsync(ct);
            }

            examined++;
            var closing = verdicts.Where(v => v.Relation == "closes" && v.Quote != null).OrderBy(v => clock.PositionOf(v.BeatId)).FirstOrDefault();
            if (closing.Quote != null)
            {
                var hash = candidates.First(c => c.Id == closing.BeatId).TextHash;
                db.NarrativeObligationEvents.Add(new NarrativeObligationEvent { ObligationId = o.Id, Action = ObligationEventAction.Close, BeatId = closing.BeatId, Quote = closing.Quote, BeatTextHash = hash, Actor = ObligationActor.SystemDeep, Note = "resurfacing judge" });
                if (!o.AuthorLocked)
                {
                    o.State = ObligationState.Closed; o.ClosingBeatId = closing.BeatId; o.ClosingQuote = closing.Quote; o.ClosingTextHash = hash; o.UpdatedAt = DateTime.UtcNow;
                }
                closed++;
            }
            else
            {
                var adv = verdicts.Where(v => v.Relation == "advances" && v.Quote != null).ToList();
                if (adv.Count > 0)
                {
                    foreach (var a in adv)
                    {
                        var already = await db.NarrativeObligationEvents.AnyAsync(e => e.ObligationId == o.Id && e.BeatId == a.BeatId && e.Action == ObligationEventAction.Advance, ct);
                        if (already) continue;
                        db.NarrativeObligationEvents.Add(new NarrativeObligationEvent { ObligationId = o.Id, Action = ObligationEventAction.Advance, BeatId = a.BeatId, Quote = a.Quote, BeatTextHash = candidates.First(c => c.Id == a.BeatId).TextHash, Actor = ObligationActor.SystemDeep, Note = "resurfacing judge" });
                    }
                    if (!o.AuthorLocked && o.State == ObligationState.Open) o.State = ObligationState.Advanced;
                    o.UpdatedAt = DateTime.UtcNow;
                    advanced++;
                }
                else notAddressed++;
            }
            await db.SaveChangesAsync(ct);
        }

        return new DeepResult(examined, closed, advanced, notAddressed, discarded, cacheHits, calls, evaluated);
    }

    internal static List<(int Number, string Relation, string? Quote)> Parse(string? raw)
    {
        var list = new List<(int, string, string?)>();
        if (string.IsNullOrWhiteSpace(raw)) return list;
        var start = raw.IndexOf('{'); var end = raw.LastIndexOf('}');
        if (start < 0 || end <= start) return list;
        try
        {
            using var doc = JsonDocument.Parse(raw[start..(end + 1)]);
            if (!doc.RootElement.TryGetProperty("verdicts", out var arr) || arr.ValueKind != JsonValueKind.Array) return list;
            foreach (var v in arr.EnumerateArray())
            {
                var n = v.TryGetProperty("beat_number", out var bn) && bn.ValueKind == JsonValueKind.Number && bn.TryGetInt32(out var i) ? i : 0;
                var rel = v.TryGetProperty("relation", out var r) && r.ValueKind == JsonValueKind.String ? r.GetString()?.Trim().ToLowerInvariant() : null;
                var q = v.TryGetProperty("quote", out var qq) && qq.ValueKind == JsonValueKind.String ? qq.GetString() : null;
                if (n < 1 || rel is not ("closes" or "advances" or "not_addressed")) continue;
                list.Add((n, rel!, q));
            }
        }
        catch (JsonException) { }
        return list;
    }

    /// <summary>Consecutive word-windows of at most <paramref name="maxWords"/> words. Every word
    /// of the input appears in exactly one part; nothing is cut.</summary>
    internal static List<string> SplitPassages(string text, int maxWords)
    {
        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length <= maxWords) return [string.Join(' ', words)];
        var parts = new List<string>((words.Length + maxWords - 1) / maxWords);
        for (var i = 0; i < words.Length; i += maxWords)
            parts.Add(string.Join(' ', words.Skip(i).Take(maxWords)));
        return parts;
    }

    private static int Rank(string relation) => relation switch { "closes" => 3, "advances" => 2, "ungrounded" => 1, _ => 0 };

    /// <summary>Hard veto on a "closes"/"advances" verdict whose quote is real text (grounding
    /// passed) but shares no content word with the obligation it claims to resolve. This is the
    /// gap that let the judge close "the stopped clock at a quarter past three" using an unrelated
    /// sentence about smoking a cigar behind a tree (GCSH calibration run 3, 2026-09-15): grounded,
    /// but never actually about the debt. Relevance is deliberately checked separately from
    /// grounding — a quote can be real without being an answer.</summary>
    internal static bool SharesContent(NarrativeObligation o, string quote)
    {
        var obligationWords = LexicalCandidateFinder.ContentWords(o.Description + " " + (o.OriginQuote ?? ""));
        if (obligationWords.Count == 0) return true;
        var quoteWords = new HashSet<string>(LexicalCandidateFinder.ContentWords(quote), StringComparer.OrdinalIgnoreCase);
        return obligationWords.Any(quoteWords.Contains);
    }
}
