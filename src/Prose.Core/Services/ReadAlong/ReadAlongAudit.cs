using System.Globalization;
using System.Text;
using static Prose.Core.Services.ReadAlong.StoryFactExtractor;

namespace Prose.Core.Services.ReadAlong;

/// <summary>
/// Measure before adding (RFC 0019, RA-S3.1.1): replay the read-along over a book **in memory** — no draft,
/// fact or card is written — and sample what it would have produced, stratified by pattern, into a CSV the
/// author marks right or wrong. <see cref="Score"/> turns the marked CSV into a per-pattern precision table
/// (the RFC 0014 instrument-value audit applied to the extractor) and names the patterns to switch off.
/// </summary>
public static class ReadAlongAudit
{
    public sealed record Row(string Kind, string Pattern, string Subject, string Attribute, string Value, string Quote, string Chapter, string Verdict = "");

    public static readonly string[] Header = ["kind", "pattern", "subject", "attribute", "value", "quote", "chapter", "verdict"];

    /// <summary>Replay the extractor chapter by chapter, as a fresh read would (names met become known
    /// drafts for later chapters; single-valued details are compared like the real read).</summary>
    public static List<Row> Replay(IReadOnlyList<ReadAlongService.BookChapter> chapters, IReadOnlySet<string>? disabled = null)
    {
        var rows = new List<Row>();
        var known = new List<KnownEntry>();
        var knownKeys = new HashSet<string>(StringComparer.Ordinal);
        var current = new Dictionary<(string Subject, string Attribute), (string Value, int Chapter)>();

        foreach (var ch in chapters)
        {
            var r = Extract(ch.Paragraphs, known, null, disabled);
            foreach (var g in r.Names.Where(n => n.IsNew).GroupBy(n => n.Key))
            {
                var first = g.First();
                rows.Add(new Row("name", first.Pattern, first.Name, first.Category, "", first.Quote, ch.Title));
                if (knownKeys.Add(g.Key)) known.Add(new KnownEntry(null, first.Name, first.Category, []));
            }
            foreach (var f in r.Facts.Where(f => !f.Evidence))
            {
                rows.Add(new Row("fact", f.Pattern, f.SubjectName, f.Attribute, f.Value, f.Quote, ch.Title));
                if (f.Multi) continue;
                var key = (f.SubjectKey, f.Attribute);
                if (current.TryGetValue(key, out var prev))
                {
                    var (confidence, kind) = Compare(f.Attribute, prev.Value, f.Value, f.Confidence, prev.Chapter <= ch.Index);
                    if (confidence > 0)
                        rows.Add(new Row("card", f.Pattern, f.SubjectName, f.Attribute, $"{prev.Value} → {f.Value} ({kind} {confidence}%)", f.Quote, ch.Title));
                }
                current[key] = (f.Value, ch.Index);
            }
            foreach (var x in r.Relations)
                rows.Add(new Row("relation", x.Pattern, x.FromName, x.Type, x.ToName, x.Quote, ch.Title));
        }
        return rows;
    }

    /// <summary>Up to <paramref name="perKind"/> rows of each of name, fact and relation, spread evenly over
    /// their patterns (deterministic: the same book gives the same sample), plus every would-be card.</summary>
    public static List<Row> Sample(IReadOnlyList<Row> rows, int perKind)
    {
        var picked = new List<Row>();
        foreach (var kind in new[] { "name", "fact", "relation" })
        {
            var byPattern = rows.Where(r => r.Kind == kind).GroupBy(r => r.Pattern).OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => new Queue<Row>(Spread(g.ToList()))).ToList();
            var take = new List<Row>();
            while (take.Count < perKind && byPattern.Any(q => q.Count > 0))
                foreach (var q in byPattern)
                    if (q.Count > 0 && take.Count < perKind) take.Add(q.Dequeue());
            picked.AddRange(take);
        }
        picked.AddRange(rows.Where(r => r.Kind == "card"));
        return picked;
    }

    // Evenly spaced through the book rather than the first N, so one chapter does not dominate.
    private static IEnumerable<Row> Spread(List<Row> rows)
    {
        if (rows.Count <= 1) return rows;
        var order = Enumerable.Range(0, rows.Count).OrderBy(i => BitReverse(i, rows.Count)).ToList();
        return order.Select(i => rows[i]);
    }

    private static double BitReverse(int i, int n)
    {
        double x = 0, f = 0.5;
        for (var v = i; v > 0; v >>= 1, f /= 2) if ((v & 1) == 1) x += f;
        return x + i / (double)(n * 1000);
    }

    // ── CSV ───────────────────────────────────────────────────────────────────────────────────

    public static string ToCsv(IEnumerable<Row> rows)
    {
        static string Q(string s) => s.IndexOfAny([',', '"', '\n', '\r']) >= 0 ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
        var sb = new StringBuilder(string.Join(",", Header)).Append('\n');
        foreach (var r in rows)
            sb.Append(string.Join(",", new[] { r.Kind, r.Pattern, r.Subject, r.Attribute, r.Value, r.Quote, r.Chapter, r.Verdict }.Select(Q))).Append('\n');
        return sb.ToString();
    }

    public static List<Row> FromCsv(string csv)
    {
        var records = ParseCsv(csv);
        if (records.Count == 0) return [];
        var head = records[0].Select(h => h.Trim().ToLowerInvariant()).ToList();
        int Col(string name) => head.IndexOf(name);
        string Get(List<string> rec, string name) => Col(name) is var i and >= 0 && i < rec.Count ? rec[i] : "";
        return records.Skip(1).Where(r => r.Any(c => c.Length > 0))
            .Select(r => new Row(Get(r, "kind"), Get(r, "pattern"), Get(r, "subject"), Get(r, "attribute"), Get(r, "value"), Get(r, "quote"), Get(r, "chapter"), Get(r, "verdict")))
            .ToList();
    }

    private static List<List<string>> ParseCsv(string text)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var cell = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (quoted)
            {
                if (c == '"' && i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; }
                else if (c == '"') quoted = false;
                else cell.Append(c);
                continue;
            }
            switch (c)
            {
                case '"': quoted = true; break;
                case ',': row.Add(cell.ToString()); cell.Clear(); break;
                case '\r': break;
                case '\n': row.Add(cell.ToString()); cell.Clear(); rows.Add(row); row = []; break;
                default: cell.Append(c); break;
            }
        }
        if (cell.Length > 0 || row.Count > 0) { row.Add(cell.ToString()); rows.Add(row); }
        return rows;
    }

    // ── Score ─────────────────────────────────────────────────────────────────────────────────

    public sealed record PatternScore(string Pattern, int Judged, int Right, double Precision, bool SwitchOff);

    /// <summary>Verdicts: y/yes/1/right/✓ count right, n/no/0/wrong/✗ wrong, anything else is unjudged.
    /// A pattern with at least <paramref name="minJudged"/> verdicts below <paramref name="bar"/> is to be
    /// switched off.</summary>
    public static List<PatternScore> Score(IEnumerable<Row> rows, double bar = 0.8, int minJudged = 5)
    {
        static bool? Verdict(string v) => v.Trim().ToLowerInvariant() switch
        {
            "y" or "yes" or "1" or "right" or "✓" or "true" => true,
            "n" or "no" or "0" or "wrong" or "✗" or "false" => false,
            _ => null,
        };
        return rows.Select(r => (r.Pattern, V: Verdict(r.Verdict)))
            .Where(x => x.V is not null && x.Pattern.Length > 0)
            .GroupBy(x => x.Pattern)
            .Select(g =>
            {
                var judged = g.Count();
                var right = g.Count(x => x.V == true);
                var precision = judged == 0 ? 0 : right / (double)judged;
                return new PatternScore(g.Key, judged, right, precision, judged >= minJudged && precision < bar);
            })
            .OrderBy(p => p.Precision).ThenBy(p => p.Pattern, StringComparer.Ordinal)
            .ToList();
    }

    public static string ToMarkdown(IReadOnlyList<PatternScore> scores, string source, double bar = 0.8)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Read-along precision").AppendLine()
          .AppendLine($"Scored from `{source}` on {DateTime.UtcNow:yyyy-MM-dd}. Bar: {bar:P0}. Patterns under the bar with at least 5 verdicts are switched off (setting `{ReadAlongService.DisabledPatternsKey}`).")
          .AppendLine()
          .AppendLine("| Pattern | Judged | Right | Precision | Action |")
          .AppendLine("| --- | ---: | ---: | ---: | --- |");
        foreach (var s in scores)
            sb.AppendLine(CultureInfo.InvariantCulture, $"| `{s.Pattern}` | {s.Judged} | {s.Right} | {s.Precision:P0} | {(s.SwitchOff ? "**switch off**" : s.Judged < 5 ? "too few verdicts" : "keep")} |");
        var off = scores.Where(s => s.SwitchOff).Select(s => s.Pattern).ToList();
        sb.AppendLine().AppendLine(off.Count == 0 ? "Nothing to switch off." : $"Switch off: {string.Join(", ", off.Select(p => $"`{p}`"))}.");
        return sb.ToString();
    }
}
