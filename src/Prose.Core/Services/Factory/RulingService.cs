using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services.Factory;

public sealed record RulingDraft(
    string Kind,
    string Text,
    Guid? BookId = null,
    Guid? UniverseId = null,
    string? Pattern = null,
    decimal? MaxPer1kWords = null,
    string Source = "author");

/// <summary>One place a law's pattern matched in the prose.</summary>
public sealed record LawHit(Guid RulingId, string RulingText, Guid BeatId, int Number, int Position, string Match, string Context);

/// <summary>One place a law's pattern matched an entity record the book tags. Field is the JSON path.</summary>
public sealed record RecordLawHit(Guid RulingId, string RulingText, Guid EntityId, string EntityName, string EntityType, string Field, string Match, string Context);

/// <summary>
/// The author's law as data (RFC 0015 §3.6). A law is a constraint: pattern-less text the writer
/// is shown, or a zero-tolerance pattern the prose must never match. A metric is a book-level tic
/// ceiling. An incidental is a proper name that intentionally has no entity. Entity FACTS are never
/// rulings — they live on the entity record, so a ruling can never be a second copy of the world.
///
/// <para>Patterns are .NET regexes, case-insensitive by default (an inline <c>(?-i)</c> makes one
/// case-sensitive), matched against tag-stripped text with a one-second timeout.</para>
/// </summary>
public sealed class RulingService(IDbContextFactory<ProseDbContext> dbFactory, BookSpineService spine)
{
    public static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

    public static Regex Compile(string pattern) =>
        new(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline, PatternTimeout);

    public async Task<Ruling> RecordAsync(RulingDraft d, CancellationToken ct = default)
    {
        if (!RulingKinds.All.Contains(d.Kind)) throw new ArgumentException($"kind must be one of {string.Join(", ", RulingKinds.All)}.");
        if (string.IsNullOrWhiteSpace(d.Text) || d.Text.Length > 2000) throw new ArgumentException("text is required (≤2000 chars): the author's words.");
        if (d.Pattern is { Length: > 400 }) throw new ArgumentException("pattern must be ≤400 chars.");
        if (d.Kind is RulingKinds.Metric or RulingKinds.Incidental && string.IsNullOrWhiteSpace(d.Pattern))
            throw new ArgumentException($"a {d.Kind} ruling needs a pattern.");
        if (d.Kind == RulingKinds.Metric && d.MaxPer1kWords is not > 0)
            throw new ArgumentException("a metric ruling needs maxPer1kWords > 0.");
        if (!string.IsNullOrWhiteSpace(d.Pattern) && d.Kind != RulingKinds.Incidental)
        {
            try { _ = Compile(d.Pattern); }
            catch (ArgumentException ex) { throw new ArgumentException($"the pattern does not compile: {ex.Message}"); }
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        // A universe-wide ruling with no universe named takes the caller's scope — only when the
        // caller NAMED that scope. The inherited default (GLMZ) would otherwise receive a live law
        // every time a --node was forgotten.
        var universe = d.UniverseId
            ?? (UniverseScope.IsExplicitlyScoped && UniverseScope.EffectiveId != Guid.Empty ? UniverseScope.EffectiveId : null);
        var bookId = d.BookId;
        if (d.BookId is { } book)
        {
            universe = await db.Nodes.IgnoreQueryFilters().AsNoTracking().Where(n => n.Id == book).Select(n => (Guid?)n.UniverseId).FirstOrDefaultAsync(ct)
                       ?? throw new ArgumentException($"book {book} not found.");
            // Rulings are read per book (ListAsync matches BookId exactly), so a chapter id stored
            // here was recorded, answered ok, and never applied anywhere.
            bookId = await NodeWorkbenchService.ResolveBookAncestorIdAsync(db, book, ct) ?? book;
        }
        if (universe is not { } u || u == Guid.Empty) throw new ArgumentException("a universe-wide ruling needs a universe.");

        var row = new Ruling
        {
            UniverseId = u,
            BookId = bookId,
            Kind = d.Kind,
            Text = d.Text.Trim(),
            Pattern = string.IsNullOrWhiteSpace(d.Pattern) ? null : d.Pattern.Trim(),
            MaxPer1kWords = d.Kind == RulingKinds.Metric ? d.MaxPer1kWords : null,
            Source = string.IsNullOrWhiteSpace(d.Source) ? "author" : d.Source.Trim(),
        };
        db.Rulings.Add(row);
        await db.SaveChangesAsync(ct);
        return row;
    }

    /// <summary>Active rulings that apply to a book: the book's own plus its universe-wide ones.</summary>
    public async Task<List<Ruling>> ListAsync(Guid bookId, string? kind = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var universe = await db.Nodes.IgnoreQueryFilters().AsNoTracking().Where(n => n.Id == bookId).Select(n => n.UniverseId).FirstOrDefaultAsync(ct);
        var q = db.Rulings.AsNoTracking().Where(r => r.SupersededById == null
            && (r.BookId == bookId || (r.BookId == null && r.UniverseId == universe)));
        if (!string.IsNullOrWhiteSpace(kind)) q = q.Where(r => r.Kind == kind);
        return await q.OrderBy(r => r.Kind).ThenBy(r => r.At).ToListAsync(ct);
    }

    /// <summary>Replace a ruling: the new one is recorded, the old one points at it and goes inert.</summary>
    public async Task<Ruling> SupersedeAsync(Guid id, RulingDraft replacement, CancellationToken ct = default)
    {
        await using (var db0 = await dbFactory.CreateDbContextAsync(ct))
        {
            var old = await db0.Rulings.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct) ?? throw new ArgumentException($"ruling {id} not found.");
            if (old.SupersededById != null) throw new ArgumentException("that ruling is already superseded.");
            replacement = replacement with { BookId = replacement.BookId ?? old.BookId, UniverseId = replacement.UniverseId ?? old.UniverseId };
        }
        var row = await RecordAsync(replacement, ct);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        // Claim the old ruling in one conditional statement: two sessions superseding it at once
        // otherwise both succeeded and left two active replacements.
        var claimed = await db.Rulings.Where(r => r.Id == id && r.SupersededById == null)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.SupersededById, (Guid?)row.Id), ct);
        if (claimed == 0)
        {
            await db.Rulings.Where(r => r.Id == row.Id).ExecuteDeleteAsync(ct);
            throw new ArgumentException("that ruling was superseded by someone else meanwhile; nothing was recorded.");
        }
        return row;
    }

    /// <summary>Every place an active law's (or page-law's) pattern matches the book's prose, in reading order.</summary>
    public async Task<List<LawHit>> FindLawViolationsAsync(Guid bookId, CancellationToken ct = default)
    {
        var laws = (await ListAsync(bookId, null, ct))
            .Where(r => RulingKinds.BindThePage.Contains(r.Kind) && r.Pattern != null).ToList();
        if (laws.Count == 0) return [];
        var text = await BookTextAsync(bookId, ct);
        var hits = new List<LawHit>();
        foreach (var law in laws)
        {
            var rx = Compile(law.Pattern!);
            foreach (var (beatId, number, position, plain) in text)
            {
                foreach (Match m in rx.Matches(plain))
                    hits.Add(new LawHit(law.Id, law.Text, beatId, number, position, m.Value, Context(plain, m.Index, m.Length)));
            }
        }
        return hits.OrderBy(h => h.Position).ToList();
    }

    /// <summary>
    /// Every place an active law's pattern matches the canonical record of an entity the book tags
    /// (RFC 0015 §3.6): the world the writer draws on must not hold what the page may not say.
    /// Page-laws are not applied here — they name facts the record is meant to hold.
    /// <paramref name="entityId"/> narrows the scan to one record. <paramref name="searchPattern"/>
    /// replaces the laws with one ad-hoc pattern: a read-only search of the book's world (for the
    /// residue of a retired storyline, say), recorded nowhere.
    /// </summary>
    public async Task<List<RecordLawHit>> FindRecordViolationsAsync(Guid bookId, Guid? entityId = null, string? searchPattern = null,
        CancellationToken ct = default)
    {
        var laws = !string.IsNullOrWhiteSpace(searchPattern)
            ? [(Law: new Ruling { Id = Guid.Empty, Kind = "search", Text = $"search /{searchPattern}/", Pattern = searchPattern }, Rx: Compile(searchPattern))]
            : (await ListAsync(bookId, RulingKinds.Law, ct)).Where(r => r.Pattern != null)
                .Select(r => (Law: r, Rx: Compile(r.Pattern!))).ToList();
        if (laws.Count == 0) return [];
        List<Guid> ids = entityId is { } one ? [one]
            : (await BookBeatsAsync(bookId, ct)).SelectMany(b => BeatMarkup.ExtractEntityGuids(b.Raw)).Distinct().ToList();

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var entities = await db.Entities.IgnoreQueryFilters().AsNoTracking().Where(e => ids.Contains(e.Id))
            .Select(e => new { e.Id, e.Name, e.EntityType }).ToListAsync(ct);
        var hits = new List<RecordLawHit>();
        foreach (var e in entities.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (CanonRecordLoader.Load(db, e.EntityType, e.Id) is not { } record) continue;
            foreach (var (field, value) in StringLeaves(record, ""))
                foreach (var (law, rx) in laws)
                    foreach (Match m in rx.Matches(value))
                        hits.Add(new RecordLawHit(law.Id, law.Text, e.Id, e.Name, e.EntityType, field, m.Value, Context(value, m.Index, m.Length)));
        }
        return hits;
    }

    private static IEnumerable<(string Field, string Value)> StringLeaves(System.Text.Json.Nodes.JsonNode node, string path)
    {
        switch (node)
        {
            case System.Text.Json.Nodes.JsonObject o:
                foreach (var (k, v) in o)
                    if (v != null) foreach (var leaf in StringLeaves(v, path.Length == 0 ? k : $"{path}.{k}")) yield return leaf;
                break;
            case System.Text.Json.Nodes.JsonArray a:
                for (var i = 0; i < a.Count; i++)
                    if (a[i] is { } v) foreach (var leaf in StringLeaves(v, $"{path}[{i}]")) yield return leaf;
                break;
            case System.Text.Json.Nodes.JsonValue v when v.TryGetValue<string>(out var s):
                yield return (path, s);
                break;
        }
    }

    /// <summary>The book's beats in reading order as (id, number, position, tag-stripped text).</summary>
    public async Task<List<(Guid BeatId, int Number, int Position, string Plain)>> BookTextAsync(Guid bookId, CancellationToken ct = default) =>
        (await BookBeatsAsync(bookId, ct)).Select(b => (b.BeatId, b.Number, b.Position, BeatMarkup.StripEntityTags(b.Raw))).ToList();

    private async Task<List<(Guid BeatId, int Number, int Position, string Raw)>> BookBeatsAsync(Guid bookId, CancellationToken ct)
    {
        var sp = await spine.GetAsync(bookId, ct);
        var order = sp.Chapters.SelectMany(c => c.Beats).ToList();
        var ids = order.Select(b => b.BeatId).ToList();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var texts = await db.Beats.AsNoTracking().Where(b => ids.Contains(b.Id)).Select(b => new { b.Id, b.Text }).ToDictionaryAsync(b => b.Id, b => b.Text ?? "", ct);
        return order.Select(b => (b.BeatId, b.Number, b.Ordinal, texts.GetValueOrDefault(b.BeatId, ""))).ToList();
    }

    private static string Context(string text, int index, int length)
    {
        var from = Math.Max(0, index - 50);
        var to = Math.Min(text.Length, index + length + 50);
        return (from > 0 ? "…" : "") + text[from..to].Replace('\n', ' ') + (to < text.Length ? "…" : "");
    }
}

/// <summary>One metric against its ceiling, for the whole book.</summary>
public sealed record MetricResult(Guid RulingId, string Text, string Pattern, int Count, int Max, decimal MaxPer1kWords, bool Pass, bool AuthorSourced);

public sealed record MetricsResult(int Words, IReadOnlyList<MetricResult> Metrics)
{
    /// <summary>Only author-sourced metrics gate the press (RFC 0015 §3.7).</summary>
    public bool GatePasses => Metrics.Where(m => m.AuthorSourced).All(m => m.Pass);
}

/// <summary>Book-level tic counts against the author's metric rulings (RFC 0015 §3.7). Counting,
/// not judging: a pattern and a ceiling per thousand words of the whole book.</summary>
public sealed class MetricsReport(RulingService rulings)
{
    public static int WordCount(string plain) => Regex.Matches(plain, @"\S+").Count;

    public static int Count(string pattern, IEnumerable<string> texts)
    {
        var rx = RulingService.Compile(pattern);
        return texts.Sum(t => rx.Matches(t).Count);
    }

    public async Task<MetricsResult> ComputeAsync(Guid bookId, CancellationToken ct = default)
    {
        var metrics = await rulings.ListAsync(bookId, RulingKinds.Metric, ct);
        var text = (await rulings.BookTextAsync(bookId, ct)).Select(t => t.Plain).ToList();
        var words = text.Sum(WordCount);
        var results = metrics.Select(m =>
        {
            var count = Count(m.Pattern!, text);
            var max = (int)Math.Floor(m.MaxPer1kWords!.Value * words / 1000m);
            return new MetricResult(m.Id, m.Text, m.Pattern!, count, max, m.MaxPer1kWords.Value, count <= max, m.Source == "author");
        }).ToList();
        return new MetricsResult(words, results);
    }
}
