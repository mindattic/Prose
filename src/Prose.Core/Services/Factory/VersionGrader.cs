using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services.Factory;

/// <summary>Which way a grade row is won. <see cref="Neutral"/> rows are shown, never won.</summary>
public enum GradeBetter { Neutral, Higher, Lower }

/// <summary>
/// One deterministic measurement of one version. Stored as a sum and a base so that a series or
/// universe rollup is the sum of its books (never a mean of means): the shown value is
/// <c>Sum</c> for a total (<c>Base</c> null), else <c>Sum / Base * Scale</c> — or nothing when
/// the base is 0 (no plants to pay off is not 0% paid).
/// </summary>
public sealed record GradeMeasure(string Key, string Label, GradeBetter Better, double Sum, double? Base = null, double Scale = 1)
{
    public double? Value => Base is not { } b ? Sum : b == 0 ? null : Sum / b * Scale;
}

/// <summary>One A-vs-B row. <c>Winner</c> is "A", "B", "=" (tie) or "" (neutral / missing on a side).</summary>
public sealed record GradeRow(string Key, string Label, GradeBetter Better, double? A, double? B, string Winner, double Weight);

public sealed record GradeComparison(IReadOnlyList<GradeRow> Rows, double AWins, double BWins, double Ties)
{
    public string Verdict => AWins > BWins ? "A" : BWins > AWins ? "B" : "even";
}

/// <summary>The pure half of the version grader: rollup and the weighted win count. No database.</summary>
public static class GradeMath
{
    /// <summary>Sum each measure across books by key, keeping the first label/direction seen.</summary>
    public static List<GradeMeasure> Rollup(IEnumerable<IReadOnlyList<GradeMeasure>> books)
    {
        var order = new List<string>();
        var acc = new Dictionary<string, GradeMeasure>();
        foreach (var book in books)
            foreach (var m in book)
            {
                if (acc.TryGetValue(m.Key, out var have))
                    acc[m.Key] = have with { Sum = have.Sum + m.Sum, Base = have.Base is null && m.Base is null ? null : (have.Base ?? 0) + (m.Base ?? 0) };
                else { acc[m.Key] = m; order.Add(m.Key); }
            }
        return order.Select(k => acc[k]).ToList();
    }

    /// <summary>
    /// Row-by-row comparison. Values are compared as displayed (two decimals), so float noise is a
    /// tie. A row's weight comes from <paramref name="weights"/> by key (case-insensitive), else 1;
    /// a weight of 0 shows the row but counts it for nobody.
    /// </summary>
    public static GradeComparison Compare(IReadOnlyList<GradeMeasure> a, IReadOnlyList<GradeMeasure> b,
        IReadOnlyDictionary<string, double>? weights = null)
    {
        var byKeyB = b.ToDictionary(m => m.Key);
        var keys = a.Select(m => m.Key).Concat(b.Select(m => m.Key).Where(k => a.All(x => x.Key != k))).ToList();
        var byKeyA = a.ToDictionary(m => m.Key);
        double aw = 0, bw = 0, tw = 0;
        var rows = new List<GradeRow>();
        foreach (var key in keys)
        {
            var ma = byKeyA.GetValueOrDefault(key);
            var mb = byKeyB.GetValueOrDefault(key);
            var head = ma ?? mb!;
            double? va = ma?.Value is { } ra ? Math.Round(ra, 2) : null;
            double? vb = mb?.Value is { } rb ? Math.Round(rb, 2) : null;
            var weight = WeightFor(weights, key);
            var winner = "";
            if (head.Better != GradeBetter.Neutral && va is { } x && vb is { } y)
            {
                winner = x == y ? "=" : (x > y) == (head.Better == GradeBetter.Higher) ? "A" : "B";
                if (winner == "A") aw += weight; else if (winner == "B") bw += weight; else tw += weight;
            }
            rows.Add(new GradeRow(key, head.Label, head.Better, va, vb, winner, weight));
        }
        return new GradeComparison(rows, aw, bw, tw);
    }

    public static double WeightFor(IReadOnlyDictionary<string, double>? weights, string key)
    {
        if (weights is null) return 1;
        foreach (var (k, v) in weights)
            if (string.Equals(k, key, StringComparison.OrdinalIgnoreCase)) return v;
        // "metric" weighs every per-ruling metric row ("metric:xxxxxxxx") not named on its own.
        var colon = key.IndexOf(':');
        if (colon > 0)
            foreach (var (k, v) in weights)
                if (string.Equals(k, key[..colon], StringComparison.OrdinalIgnoreCase)) return v;
        return 1;
    }

    /// <summary>The A-vs-B table as Markdown.</summary>
    public static string Table(GradeComparison c, string aLabel, string bLabel)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"| Metric | Better | {aLabel} | {bLabel} | Win | Weight |");
        sb.AppendLine("|---|---|---:|---:|:---:|---:|");
        foreach (var r in c.Rows)
            sb.AppendLine($"| {r.Label.Replace("|", "/")} | {Arrow(r.Better)} | {Fmt(r.A)} | {Fmt(r.B)} | {r.Winner} | {(r.Better == GradeBetter.Neutral ? "" : r.Weight.ToString("0.##", CultureInfo.InvariantCulture))} |");
        sb.AppendLine();
        sb.AppendLine($"**Weighted wins: A {c.AWins:0.##} · B {c.BWins:0.##} · ties {c.Ties:0.##} → {(c.Verdict == "even" ? "even" : c.Verdict == "A" ? aLabel : bLabel)}**");
        return sb.ToString();
    }

    private static string Arrow(GradeBetter b) => b switch { GradeBetter.Higher => "↑", GradeBetter.Lower => "↓", _ => "" };
    private static string Fmt(double? v) => v is { } x ? x.ToString(x == Math.Floor(x) ? "#,0" : "#,0.00", CultureInfo.InvariantCulture) : "—";
}

/// <summary>A version the grader can read: <c>working</c>, <c>V12</c> (or <c>12</c>), or a date.</summary>
public sealed record GradeVersionSpec(string Raw, int? Version, DateTime? Before, bool Working)
{
    public string Label => Working ? "working" : Version is { } v ? $"V{v}" : Raw.Trim().Replace(':', '-');

    public static GradeVersionSpec Parse(string raw)
    {
        var s = raw.Trim();
        if (s.Equals("working", StringComparison.OrdinalIgnoreCase) || s.Equals("live", StringComparison.OrdinalIgnoreCase))
            return new(s, null, null, true);
        var v = s.StartsWith('V') || s.StartsWith('v') ? s[1..] : s;
        if (int.TryParse(v, NumberStyles.None, CultureInfo.InvariantCulture, out var n)) return new(s, n, null, false);
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var d))
            // A bare date means "as of the end of that day".
            return new(s, null, d.TimeOfDay == TimeSpan.Zero ? d.AddDays(1) : d, false);
        throw new ArgumentException($"not a version: '{raw}' (use working, V12, or a date such as 2026-10-01).");
    }
}

/// <summary>One version's text: beats in reading order (raw, entity tags kept) and its chapter count.</summary>
public sealed record GradeSnapshot(string Label, string Source, IReadOnlyList<(Guid? BeatId, string Raw)> Beats, int Chapters);

/// <summary>
/// Version grader (PRS-T866), deterministic battery only. Resolves a version of a book to its text
/// — the press (<see cref="ExportRecord"/>) and its <see cref="ArchivedBook"/> snapshot, or the live
/// beats for <c>working</c> — and measures it with the instruments that already exist:
/// <see cref="BeatProseMetricsService.Compute"/> (style), the book's metric rulings
/// (<see cref="MetricsReport"/>'s counting), its law/page-law patterns (<c>law_violations</c>) and
/// the plant/payoff registry. Rulings and plants are the CURRENT ones, applied to both versions'
/// text: the question is which version better meets today's rules.
/// </summary>
public sealed class VersionGrader(IDbContextFactory<ProseDbContext> dbFactory, RulingService rulings, BookSpineService spine)
{
    private static readonly Regex Marker = new(@"^<!--\s*beat:\d+:([0-9a-f]{32})\s*-->$", RegexOptions.IgnoreCase);

    public async Task<GradeSnapshot?> ResolveAsync(Guid bookId, GradeVersionSpec spec, CancellationToken ct = default)
    {
        if (spec.Working) return await LiveAsync(bookId, spec.Label, ct);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var version = spec.Version;
        string source;
        if (version is null)
        {
            // A date names the newest press on or before it.
            var press = await db.Exports.AsNoTracking()
                .Where(e => e.BookId == bookId && e.At < spec.Before && e.Format != "mp3")
                .OrderByDescending(e => e.At).FirstOrDefaultAsync(ct);
            version = press?.Version;
            source = press is null ? "archive" : $"press V{press.Version} {press.At:yyyy-MM-dd}";
        }
        else
            source = await db.Exports.AsNoTracking().AnyAsync(e => e.BookId == bookId && e.Version == version, ct)
                ? $"press V{version}" : $"archive V{version} (never pressed)";

        var archives = db.ArchivedBooks.AsNoTracking().Where(a => a.NodeId == bookId);
        var archive = version is { } v
            ? await archives.Where(a => a.Version == v && (spec.Before == null || a.CreatedAt < spec.Before))
                .OrderByDescending(a => a.Reason == "export").ThenByDescending(a => a.CreatedAt).FirstOrDefaultAsync(ct)
            : await archives.Where(a => a.CreatedAt < spec.Before).OrderByDescending(a => a.CreatedAt).FirstOrDefaultAsync(ct);
        if (archive is null) return null;
        if (version is null) source = $"archive V{archive.Version} {archive.CreatedAt:yyyy-MM-dd}";
        var (beats, chapters) = ParseArchive(archive.Markdown);
        return new GradeSnapshot(spec.Label, source, beats, chapters);
    }

    /// <summary>Beats and chapter count out of a beat-marked archive (ManuscriptExportService's Markdown).</summary>
    public static (List<(Guid? BeatId, string Raw)> Beats, int Chapters) ParseArchive(string markdown)
    {
        var beats = new List<(Guid?, string)>();
        int chapters = 0;
        Guid? id = null;
        var text = new System.Text.StringBuilder();
        bool open = false;
        void Flush() { if (open && text.Length > 0) beats.Add((id, text.ToString().Trim())); text.Clear(); open = false; }
        foreach (var line in markdown.Replace("\r\n", "\n").Split('\n'))
        {
            var m = Marker.Match(line.Trim());
            if (m.Success) { Flush(); id = Guid.ParseExact(m.Groups[1].Value, "N"); open = true; continue; }
            if (line.StartsWith("## ")) { Flush(); chapters++; continue; }
            if (line.StartsWith("### ") || !open) continue;
            text.AppendLine(line);
        }
        Flush();
        return (beats, Math.Max(1, chapters));
    }

    private async Task<GradeSnapshot> LiveAsync(Guid bookId, string label, CancellationToken ct)
    {
        var sp = await spine.GetAsync(bookId, ct);
        var ids = sp.Chapters.SelectMany(c => c.Beats).Select(b => b.BeatId).Distinct().ToList();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var texts = await db.Beats.AsNoTracking().Where(b => ids.Contains(b.Id))
            .Select(b => new { b.Id, b.Text }).ToDictionaryAsync(b => b.Id, b => b.Text ?? "", ct);
        var beats = ids.Select(i => ((Guid?)i, texts.GetValueOrDefault(i, "").Trim())).Where(b => b.Item2.Length > 0).ToList();
        return new GradeSnapshot(label, "live beats", beats, Math.Max(1, sp.ChapterCount));
    }

    /// <summary>The deterministic battery for one version of one book.</summary>
    public async Task<List<GradeMeasure>> MeasureAsync(Guid bookId, GradeSnapshot snap, CancellationToken ct = default)
    {
        var plain = snap.Beats.Select(b => BeatMarkup.StripEntityTags(b.Raw)).ToList();
        var stats = snap.Beats.Select(b => BeatProseMetricsService.Compute(b.BeatId ?? Guid.Empty, bookId, b.Raw)).ToList();
        double words = stats.Sum(s => s.WordCount);
        var list = new List<GradeMeasure>
        {
            new("words", "Words", GradeBetter.Neutral, words),
            new("beats", "Beats", GradeBetter.Neutral, snap.Beats.Count),
            new("chapters", "Chapters", GradeBetter.Neutral, snap.Chapters),
            new("words_per_beat", "Words per beat", GradeBetter.Neutral, words, snap.Beats.Count),
            new("words_per_sentence", "Words per sentence", GradeBetter.Neutral, words, stats.Sum(s => s.SentenceCount)),
            new("reading_ease", "Flesch reading ease", GradeBetter.Higher, stats.Sum(s => s.FleschReadingEase * s.WordCount), words),
            new("lexical_variety", "Lexical variety (type/token)", GradeBetter.Higher, stats.Sum(s => s.TypeTokenRatio * s.WordCount), words),
            new("dialogue_share", "Dialogue share %", GradeBetter.Neutral, stats.Sum(s => s.DialogueProportion * s.WordCount), words, 100),
        };

        // Style ceilings: each metric ruling's hits per 1k words, plus how many are over their ceiling.
        int over = 0;
        foreach (var r in await rulings.ListAsync(bookId, RulingKinds.Metric, ct))
        {
            int hits;
            try { hits = MetricsReport.Count(r.Pattern!, plain); }
            catch (RegexMatchTimeoutException) { continue; }
            if (hits > Math.Floor(r.MaxPer1kWords!.Value * (decimal)words / 1000m)) over++;
            list.Add(new($"metric:{r.Id.ToString("N")[..8]}", $"Metric: {Short(r.Text)} (per 1k)", GradeBetter.Lower, hits, words, 1000));
        }
        list.Add(new("metrics_over", "Metric rulings over ceiling", GradeBetter.Lower, over));

        // Laws and page-laws: zero tolerance, so the count itself.
        int lawHits = 0;
        foreach (var law in (await rulings.ListAsync(bookId, null, ct)).Where(r => RulingKinds.BindThePage.Contains(r.Kind) && r.Pattern != null))
        {
            try { var rx = RulingService.Compile(law.Pattern!); lawHits += plain.Sum(t => rx.Matches(t).Count); }
            catch (RegexMatchTimeoutException) { }
        }
        list.Add(new("law_hits", "Law hits", GradeBetter.Lower, lawHits));

        // Plant/payoff: of the registered plants whose plant beat this version carries, how many
        // also carry their payoff beat.
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            var present = snap.Beats.Where(b => b.BeatId is not null).Select(b => b.BeatId!.Value).ToHashSet();
            var pps = await db.PlantPayoffs.AsNoTracking().Where(p => p.NodeId == bookId && p.PlantBeatId != null)
                .Select(p => new { p.PlantBeatId, p.PayoffBeatId }).ToListAsync(ct);
            var planted = pps.Where(p => present.Contains(p.PlantBeatId!.Value)).ToList();
            var paid = planted.Count(p => p.PayoffBeatId is { } pay && present.Contains(pay));
            list.Add(new("plants", "Plants on the page", GradeBetter.Neutral, planted.Count));
            list.Add(new("plants_paid", "Plants paid off %", GradeBetter.Higher, paid, planted.Count, 100));
            list.Add(new("plants_dangling", "Plants left dangling", GradeBetter.Lower, planted.Count - paid));
        }

        // Cast: distinct tagged entities, and tags per 1k words.
        var cast = snap.Beats.SelectMany(b => BeatMarkup.ExtractEntityGuids(b.Raw)).Distinct().Count();
        var tags = snap.Beats.Sum(b => BeatMarkup.CountTagsByEntity(b.Raw).Values.Sum());
        list.Add(new("cast", "Distinct entities tagged", GradeBetter.Neutral, cast));
        list.Add(new("tag_density", "Entity tags per 1k words", GradeBetter.Neutral, tags, words, 1000));
        return list;
    }

    private static string Short(string s) { s = s.Replace('\n', ' ').Trim(); return s.Length > 60 ? s[..57] + "…" : s; }
}
