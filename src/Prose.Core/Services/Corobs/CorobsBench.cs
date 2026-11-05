using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Corobs.Converters;
using Corobs.Geometry;
using Corobs.Seeding;
using Prose.Core.Data;
using CoBench = Corobs.Bench.Bench;
using RecallResult = Corobs.Bench.RecallResult;
using ReachResult = Corobs.Bench.ReachResult;

namespace Prose.Core.Services.Corobs;

/// <summary>Knobs for one bench run. Both ways see the same entities, the same seed and the same
/// scoring; only the neighbour function differs.</summary>
/// <param name="Holdout">Share of the true pairs hidden and asked back (Bench.HoldoutRecall).</param>
/// <param name="Seed">Seeds the hold-out shuffle and the planar geometry (invariant 6).</param>
/// <param name="Dimensions">N of every ply's unit cube.</param>
/// <param name="K">Neighbour budget: each way answers with at most K neighbours per entity.</param>
/// <param name="MinCoBeats">A pair is true when the two entities are tagged together in at least this many beats.</param>
/// <param name="MaxBeatCast">Beats tagging more entities than this are crowd scenes and give no pairs.</param>
/// <param name="MaxShare">A value held by more than this share of the world carries no pull (gender, status, the city).</param>
/// <param name="MinBits">A Corobs neighbour must carry at least this many bits of proximity (13 ≈ 3σ).</param>
/// <param name="MaxHops">Reach is measured out to this many hops.</param>
/// <param name="GraphPly">Whether the new way may also use the Edge graph as one ply (RFC 0016 §3.2 <c>graph</c>).</param>
public sealed record CorobsBenchOptions(
    double Holdout = 0.2, long Seed = 1, int Dimensions = 64, int K = 10, int MinCoBeats = 2,
    int MaxBeatCast = 40, double MaxShare = 0.02, double MinBits = 13, int MaxHops = 3, bool GraphPly = true);

/// <summary>The hand-pass numbers that stand in for the old way's contradiction work in run 1
/// (RFC 0016 §1.3/§6.7): the Edge graph surfaces no contradictions by itself, the hands did.</summary>
public sealed record CorobsHandBaseline(int Surfaced, int Confirmed, int Fixes, int Cascade, string? Source);

public sealed record CorobsBenchEntity(Guid Id, string Type, string Name, string? RecordJson);
public sealed record CorobsBenchEdge(Guid Source, Guid Target, string Relation, double Weight);
public sealed record CorobsBenchInput(
    IReadOnlyList<CorobsBenchEntity> Entities, IReadOnlyList<CorobsBenchEdge> Edges,
    IReadOnlyList<(Guid Beat, Guid Entity)> Mentions);

/// <summary>One way's column. Null means COULD NOT LOOK for that metric, never zero.</summary>
public sealed record CorobsWayScore(
    string Way, RecallResult Recall, ReachResult Reach, int EntitiesWithNeighbours,
    int? Surfaced, int? Confirmed, int? Fixes, int? Cascade, double? CascadeYield,
    int Events, double? StoryPull, double? StoryPullPrecision, string? Note);

public sealed record CorobsBenchReport(
    int Entities, int TruthPairs, int BeatsUsed, int CrowdBeatsSkipped, int Plies, int TokensKept,
    CorobsBenchOptions Options, CorobsWayScore Old, CorobsWayScore New);

/// <summary>
/// The pure half of <c>prose --corobs bench</c> (RFC 0016 §6.7, PRS-T865): old way (the Edge graph)
/// against new way (Corobs proximity) on the same entities, scored by the Corobs library's
/// <c>Bench</c>. No database, no LLM.
///
/// <para><b>Truth</b> is <c>BeatEntityMention</c> co-occurrence: two entities tagged together in
/// at least <see cref="CorobsBenchOptions.MinCoBeats"/> beats. Neither way reads mentions, so the
/// truth never leaks into a predictor.</para>
///
/// <para><b>Old way</b>: an entity's neighbours are its live Edge endpoints, strongest summed weight
/// first. <b>New way</b>: every entity is a set of points on named plies — one cardinal ply per
/// record field path (short values only; free prose is excluded), a <c>refs</c> ply of the entities
/// its record names, a <c>graph</c> ply of its Edge neighbours (switchable), the entity's own type,
/// and a planar <c>geo</c> ply from record coordinates. Two entities holding the same value sit on
/// the same soft token, distance 0, worth <c>NCube.BitContent(N, 0)</c> bits; two different values
/// sit at standard distance and carry ~0 bits with probability 1 − 2^−bits (Eq. III-J), which is why
/// candidate pairs can be found exactly through an inverted index rather than an N² scan. The geo
/// ply uses real distances on a seeded planar string CO. A neighbour is anything at or above
/// <see cref="CorobsBenchOptions.MinBits"/>, ranked by bits, then by rarity of what is shared.</para>
///
/// <para><b>Contradictions and cascade</b> need <c>CorobsHits</c>, which do not exist until RFC 0016
/// §6 increments 5–6 land; until then the new column reports COULD NOT LOOK and the old column
/// carries the hand-pass baseline when one is given.</para>
/// </summary>
public static class CorobsBenchMath
{
    private static readonly HashSet<string> SkipKeys = new(StringComparer.OrdinalIgnoreCase)
    { "id", "name", "slug", "guid", "entity_id", "created_at", "modified_at", "updated_at" };

    private static readonly HashSet<string> NoRefKeys = new(StringComparer.OrdinalIgnoreCase)
    { "aliases", "name", "id", "slug" };

    private static readonly HashSet<string> NullValues = new(StringComparer.OrdinalIgnoreCase)
    { "", "unknown", "none", "n/a", "na", "null", "tbd", "-", "?" };

    public static CorobsBenchReport Run(CorobsBenchInput input, CorobsBenchOptions o, CorobsHandBaseline? baseline = null)
    {
        var ents = input.Entities.OrderBy(e => e.Id).ToList();
        var n = ents.Count;
        var index = new Dictionary<Guid, int>(n);
        for (var i = 0; i < n; i++) index[ents[i].Id] = i;
        var ids = ents.Select(e => e.Id).ToList();

        // ── Truth: co-occurrence in beats ──
        var pairCounts = new Dictionary<(int, int), int>();
        int beatsUsed = 0, crowd = 0;
        foreach (var beat in input.Mentions.GroupBy(m => m.Beat))
        {
            var cast = beat.Select(m => index.TryGetValue(m.Entity, out var i) ? i : -1).Where(i => i >= 0).Distinct().OrderBy(i => i).ToArray();
            if (cast.Length < 2) continue;
            if (cast.Length > o.MaxBeatCast) { crowd++; continue; }
            beatsUsed++;
            for (var a = 0; a < cast.Length; a++)
                for (var b = a + 1; b < cast.Length; b++)
                    pairCounts[(cast[a], cast[b])] = pairCounts.GetValueOrDefault((cast[a], cast[b])) + 1;
        }
        var truth = pairCounts.Where(p => p.Value >= o.MinCoBeats).Select(p => (ids[p.Key.Item1], ids[p.Key.Item2])).ToList();
        var everTogether = new HashSet<(int, int)>(pairCounts.Keys);

        // ── Old way: Edge adjacency ──
        var oldAdj = new Dictionary<int, double>[n];
        for (var i = 0; i < n; i++) oldAdj[i] = new Dictionary<int, double>();
        foreach (var e in input.Edges)
        {
            if (!index.TryGetValue(e.Source, out var s) || !index.TryGetValue(e.Target, out var t) || s == t) continue;
            oldAdj[s][t] = oldAdj[s].GetValueOrDefault(t) + e.Weight;
            oldAdj[t][s] = oldAdj[t].GetValueOrDefault(s) + e.Weight;
        }
        var oldTop = oldAdj.Select(d => (IReadOnlyCollection<Guid>)d
            .OrderByDescending(kv => kv.Value).ThenBy(kv => ids[kv.Key]).Take(o.K).Select(kv => ids[kv.Key]).ToList()).ToArray();

        // ── New way: Corobs plies ──
        var space = BuildSpace(ents, input.Edges, index, o);
        var newTop = new IReadOnlyCollection<Guid>[n];
        var newAllCount = new int[n];
        var newAll = new List<int>?[n];
        var isEvent = ents.Select(e => string.Equals(e.Type, "event", StringComparison.OrdinalIgnoreCase)).ToArray();
        Parallel.For(0, n, i =>
        {
            var acc = space.Score(i);
            var keep = acc.Where(kv => kv.Value.Bits >= o.MinBits).ToList();
            newAllCount[i] = keep.Count;
            if (isEvent[i]) newAll[i] = keep.Select(kv => kv.Key).ToList();
            newTop[i] = keep.OrderByDescending(kv => kv.Value.Bits).ThenByDescending(kv => kv.Value.Rarity)
                .ThenBy(kv => ids[kv.Key]).Take(o.K).Select(kv => ids[kv.Key]).ToList();
        });

        IReadOnlyCollection<Guid> OldNb(Guid id) => oldTop[index[id]];
        IReadOnlyCollection<Guid> NewNb(Guid id) => newTop[index[id]];

        RecallResult Recall(Func<Guid, IReadOnlyCollection<Guid>> nb) =>
            truth.Count == 0 ? new RecallResult(0, 0, 0, 0) : CoBench.HoldoutRecall(truth, o.Holdout, o.Seed, nb);

        // ── Story pull per event: how many entities each way pulls into an event, and how many of
        // those the books ever put in a beat with it ──
        var events = Enumerable.Range(0, n).Where(i => isEvent[i]).ToList();
        (double? Pull, double? Precision) Pull(Func<int, IEnumerable<int>> pulled)
        {
            if (events.Count == 0) return (null, null);
            long total = 0, together = 0;
            foreach (var ev in events)
                foreach (var j in pulled(ev))
                {
                    total++;
                    if (everTogether.Contains(ev < j ? (ev, j) : (j, ev))) together++;
                }
            return (total / (double)events.Count, total == 0 ? null : together / (double)total);
        }
        var oldPull = Pull(ev => oldAdj[ev].Keys);
        var newPull = Pull(ev => newAll[ev] ?? []);

        var oldScore = new CorobsWayScore("OLD (Edge graph)", Recall(OldNb), CoBench.Reach(ids, OldNb, o.MaxHops),
            oldTop.Count(t => t.Count > 0),
            baseline?.Surfaced, baseline?.Confirmed, baseline?.Fixes, baseline?.Cascade,
            baseline is null ? null : CoBench.CascadeYield(baseline.Cascade, baseline.Fixes),
            events.Count, oldPull.Pull, oldPull.Precision,
            baseline is null ? "contradictions/cascade: COULD NOT LOOK (no hand baseline given)" : $"contradictions/cascade = hand baseline ({baseline.Source ?? "unnamed"})");
        var newScore = new CorobsWayScore("NEW (Corobs proximity)", Recall(NewNb), CoBench.Reach(ids, NewNb, o.MaxHops),
            newTop.Count(t => t.Count > 0),
            null, null, null, null, null,
            events.Count, newPull.Pull, newPull.Precision,
            "contradictions/cascade: COULD NOT LOOK (no CorobsHits until RFC 0016 §6 increments 5-6)");

        return new CorobsBenchReport(n, truth.Count, beatsUsed, crowd, space.PlyCount, space.TokensKept, o, oldScore, newScore);
    }

    /// <summary>Markdown table of both columns; the CLI prints it and the detail rows summarise it.</summary>
    public static string Table(CorobsBenchReport r)
    {
        static string F(double? v, string fmt = "0.000") => v is { } x ? x.ToString(fmt, CultureInfo.InvariantCulture) : "COULD NOT LOOK";
        static string I(int? v) => v?.ToString(CultureInfo.InvariantCulture) ?? "COULD NOT LOOK";
        static string Hop(ReachResult rr, int h) => rr.MeanReachableByHop.Count > h ? rr.MeanReachableByHop[h].ToString("0.0", CultureInfo.InvariantCulture) : "-";
        var sb = new StringBuilder();
        sb.AppendLine($"| metric | {r.Old.Way} | {r.New.Way} |");
        sb.AppendLine("|---|---|---|");
        void Row(string m, string a, string b) => sb.AppendLine($"| {m} | {a} | {b} |");
        Row($"hold-out recall ({r.Old.Recall.Hidden} hidden pairs)", $"{F(r.Old.Recall.Recall)} ({r.Old.Recall.Recovered})", $"{F(r.New.Recall.Recall)} ({r.New.Recall.Recovered})");
        Row("precision of predicted neighbours", $"{F(r.Old.Recall.Precision)} ({r.Old.Recall.PredictedTrue}/{r.Old.Recall.Predicted})", $"{F(r.New.Recall.Precision)} ({r.New.Recall.PredictedTrue}/{r.New.Recall.Predicted})");
        Row("entities with any neighbour", $"{r.Old.EntitiesWithNeighbours}/{r.Entities}", $"{r.New.EntitiesWithNeighbours}/{r.Entities}");
        for (var h = 0; h < r.Options.MaxHops; h++)
            Row($"mean reach within {h + 1} hop(s)", Hop(r.Old.Reach, h), Hop(r.New.Reach, h));
        Row("isolated (reach nothing)", $"{r.Old.Reach.Isolated}", $"{r.New.Reach.Isolated}");
        Row("contradictions surfaced", I(r.Old.Surfaced), I(r.New.Surfaced));
        Row("contradictions confirmed", I(r.Old.Confirmed), I(r.New.Confirmed));
        Row("direct fixes", I(r.Old.Fixes), I(r.New.Fixes));
        Row("closed by cascade", I(r.Old.Cascade), I(r.New.Cascade));
        Row("cascade yield (per fix)", F(r.Old.CascadeYield, "0.00"), F(r.New.CascadeYield, "0.00"));
        Row($"story pull per event ({r.Old.Events} events)", F(r.Old.StoryPull, "0.0"), F(r.New.StoryPull, "0.0"));
        Row("story pull precision (pulled ever share a beat)", F(r.Old.StoryPullPrecision), F(r.New.StoryPullPrecision));
        return sb.ToString();
    }

    /// <summary>One line per way for <c>InstrumentRun.Detail</c> (≤1000 chars).</summary>
    public static string Detail(string universe, CorobsBenchReport r, CorobsWayScore w)
    {
        var o = r.Options;
        static string F(double? v) => v is { } x ? x.ToString("0.###", CultureInfo.InvariantCulture) : "n/a";
        var reach = string.Join("/", w.Reach.MeanReachableByHop.Select(x => x.ToString("0.0", CultureInfo.InvariantCulture)));
        return $"{w.Way} universe={universe} seed={o.Seed} holdout={F(o.Holdout)} k={o.K} n={o.Dimensions} min-co-beats={o.MinCoBeats} " +
               $"max-share={F(o.MaxShare)} min-bits={F(o.MinBits)} graph-ply={o.GraphPly} | entities={r.Entities} truth-pairs={r.TruthPairs} " +
               $"| recall={F(w.Recall.Recall)} ({w.Recall.Recovered}/{w.Recall.Hidden}) precision={F(w.Recall.Precision)} " +
               $"| reach={reach} isolated={w.Reach.Isolated} | surfaced={w.Surfaced?.ToString() ?? "n/a"} confirmed={w.Confirmed?.ToString() ?? "n/a"} " +
               $"fixes={w.Fixes?.ToString() ?? "n/a"} cascade={w.Cascade?.ToString() ?? "n/a"} yield={F(w.CascadeYield)} " +
               $"| events={w.Events} pull={F(w.StoryPull)} pull-precision={F(w.StoryPullPrecision)} | {w.Note}";
    }

    // ───────────────────────────── the space ─────────────────────────────

    internal sealed class Space
    {
        public required int PlyCount { get; init; }
        public required int TokensKept { get; init; }
        public required double SharedBits { get; init; }
        public required int[][] EntityTokens { get; init; }     // entity → kept token ids
        public required int[][] TokenMembers { get; init; }     // token → entities holding it
        public required double[] TokenRarity { get; init; }     // log2(n / df)
        public required Correlithm?[] Geo { get; init; }
        public required int[] GeoEntities { get; init; }
        public required int Dimensions { get; init; }

        /// <summary>Every entity carrying proximity to <paramref name="i"/>, with summed bits and the
        /// rarity of what they share.</summary>
        public Dictionary<int, (double Bits, double Rarity)> Score(int i)
        {
            var acc = new Dictionary<int, (double Bits, double Rarity)>();
            foreach (var t in EntityTokens[i])
                foreach (var j in TokenMembers[t])
                {
                    if (j == i) continue;
                    var cur = acc.GetValueOrDefault(j);
                    acc[j] = (cur.Bits + SharedBits, cur.Rarity + TokenRarity[t]);
                }
            if (Geo[i] is { } gi)
                foreach (var j in GeoEntities)
                {
                    if (j == i) continue;
                    var bits = NCube.BitContent(Dimensions, gi.DistanceTo(Geo[j]!));
                    if (bits <= 0) continue;
                    var cur = acc.GetValueOrDefault(j);
                    acc[j] = (cur.Bits + bits, cur.Rarity);
                }
            return acc;
        }
    }

    internal static Space BuildSpace(List<CorobsBenchEntity> ents, IReadOnlyList<CorobsBenchEdge> edges,
        Dictionary<Guid, int> index, CorobsBenchOptions o)
    {
        var n = ents.Count;
        // Unique normalised names → entity, for the refs ply. Ambiguous names name nobody.
        var byName = new Dictionary<string, int>(StringComparer.Ordinal);
        var ambiguous = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < n; i++)
        {
            var k = Norm(ents[i].Name);
            if (k.Length < 3) continue;
            if (!byName.TryAdd(k, i)) ambiguous.Add(k);
        }
        foreach (var k in ambiguous) byName.Remove(k);

        var tokenId = new Dictionary<string, int>(StringComparer.Ordinal);
        var plies = new HashSet<string>(StringComparer.Ordinal);
        var raw = new HashSet<int>[n];
        var coords = new (double Lat, double Lng)?[n];
        int Tok(string ply, string value) { plies.Add(ply); var key = ply + "\u001f" + value; if (!tokenId.TryGetValue(key, out var t)) tokenId[key] = t = tokenId.Count; return t; }

        for (var i = 0; i < n; i++)
        {
            var set = raw[i] = new HashSet<int>();
            var self = ents[i].Id.ToString("N");
            set.Add(Tok("entity_type", ents[i].Type.Trim().ToLowerInvariant()));
            set.Add(Tok("refs", self));
            if (o.GraphPly) set.Add(Tok("graph", self));
            if (string.IsNullOrWhiteSpace(ents[i].RecordJson)) continue;
            try
            {
                using var doc = JsonDocument.Parse(ents[i].RecordJson!);
                Walk(doc.RootElement, "", 0, i);
            }
            catch (JsonException) { /* an unreadable record contributes its type and identity only */ }
        }
        if (o.GraphPly)
            foreach (var e in edges)
            {
                if (!index.TryGetValue(e.Source, out var s) || !index.TryGetValue(e.Target, out var t) || s == t) continue;
                raw[s].Add(Tok("graph", ents[t].Id.ToString("N")));
                raw[t].Add(Tok("graph", ents[s].Id.ToString("N")));
            }

        // Keep tokens shared by at least two entities and by no more than MaxShare of the world.
        var df = new int[tokenId.Count];
        foreach (var s in raw) foreach (var t in s) df[t]++;
        var maxDf = Math.Max(2, (int)Math.Floor(o.MaxShare * n));
        var keptMap = new Dictionary<int, int>();
        for (var t = 0; t < df.Length; t++) if (df[t] >= 2 && df[t] <= maxDf) keptMap[t] = keptMap.Count;
        var members = new List<int>[keptMap.Count];
        for (var k = 0; k < members.Length; k++) members[k] = [];
        var rarity = new double[keptMap.Count];
        foreach (var (t, k) in keptMap) rarity[k] = Math.Log2(n / (double)df[t]);
        var entityTokens = new int[n][];
        for (var i = 0; i < n; i++)
        {
            entityTokens[i] = raw[i].Where(keptMap.ContainsKey).Select(t => keptMap[t]).OrderBy(x => x).ToArray();
            foreach (var k in entityTokens[i]) members[k].Add(i);
        }

        // Geo: a seeded planar string CO over the world's bounding box.
        var geo = new Correlithm?[n];
        var withGeo = Enumerable.Range(0, n).Where(i => coords[i] is not null).ToArray();
        if (withGeo.Length >= 2)
        {
            double minLat = withGeo.Min(i => coords[i]!.Value.Lat), maxLat = withGeo.Max(i => coords[i]!.Value.Lat);
            double minLng = withGeo.Min(i => coords[i]!.Value.Lng), maxLng = withGeo.Max(i => coords[i]!.Value.Lng);
            if (maxLat > minLat && maxLng > minLng)
            {
                var conv = new PlanarConverter(new SeededPrng(o.Seed).Derive("ply:geo"), o.Dimensions, minLng, maxLng, minLat, maxLat);
                foreach (var i in withGeo) geo[i] = conv.Convert((coords[i]!.Value.Lng, coords[i]!.Value.Lat));
                plies.Add("geo");
            }
        }

        return new Space
        {
            PlyCount = plies.Count, TokensKept = keptMap.Count, SharedBits = NCube.BitContent(o.Dimensions, 0),
            EntityTokens = entityTokens, TokenMembers = members.Select(m => m.ToArray()).ToArray(), TokenRarity = rarity,
            Geo = geo, GeoEntities = withGeo.Where(i => geo[i] is not null).ToArray(), Dimensions = o.Dimensions,
        };

        void Walk(JsonElement el, string path, int depth, int i)
        {
            if (depth > 4) return;
            switch (el.ValueKind)
            {
                case JsonValueKind.Object:
                    if (TryCoords(el, out var c)) { if (c is { } at) coords[i] ??= at; return; }
                    foreach (var p in el.EnumerateObject())
                    {
                        // Only the record's own identity fields are skipped; a nested "name"
                        // (known_members[].name) is exactly the reference the refs ply wants.
                        if (depth == 0 && SkipKeys.Contains(p.Name)) continue;
                        Walk(p.Value, path.Length == 0 ? p.Name : path + "." + p.Name, depth + 1, i);
                    }
                    break;
                case JsonValueKind.Array:
                    foreach (var item in el.EnumerateArray()) Walk(item, path, depth + 1, i);
                    break;
                case JsonValueKind.String:
                    var v = el.GetString() ?? "";
                    var norm = Norm(v);
                    if (NullValues.Contains(norm) || Guid.TryParse(v, out _)) return;
                    if (v.Length <= 60) raw[i].Add(Tok(path, norm));
                    if (v.Length <= 300 && !NoRefKeys.Contains(path) && !string.Equals(LastKey(path), "aliases", StringComparison.OrdinalIgnoreCase))
                        foreach (var part in v.Split([';', ',', '—', '/', '(', ')', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                            if (byName.TryGetValue(Norm(part), out var j) && j != i)
                                raw[i].Add(Tok("refs", ents[j].Id.ToString("N")));
                    break;
            }
        }
    }

    /// <summary>True when <paramref name="el"/> is a coordinate object; <paramref name="c"/> is null
    /// for a (0,0) placeholder, which is no point at all.</summary>
    private static bool TryCoords(JsonElement el, out (double Lat, double Lng)? c)
    {
        c = null;
        double? lat = null, lng = null;
        foreach (var p in el.EnumerateObject())
        {
            if (p.Value.ValueKind != JsonValueKind.Number) continue;
            var key = p.Name.ToLowerInvariant();
            if (key is "lat" or "latitude" or "geo_lat") lat = p.Value.GetDouble();
            else if (key is "lng" or "lon" or "long" or "longitude" or "geo_lng") lng = p.Value.GetDouble();
        }
        if (lat is null || lng is null) return false;
        if (lat != 0 || lng != 0) c = (lat.Value, lng.Value);
        return true;
    }

    private static string LastKey(string path) { var i = path.LastIndexOf('.'); return i < 0 ? path : path[(i + 1)..]; }

    internal static string Norm(string s)
    {
        var t = string.Join(' ', s.Trim().ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return t.StartsWith("the ", StringComparison.Ordinal) ? t[4..] : t;
    }
}

/// <summary>The database half: loads one universe's entities, records, live Edges and beat
/// mentions, read-only, and hands them to <see cref="CorobsBenchMath"/>.</summary>
public sealed class CorobsBenchService(IDbContextFactory<ProseDbContext> dbFactory)
{
    public async Task<CorobsBenchInput> LoadAsync(Guid universeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var ents = await db.Entities.IgnoreQueryFilters().AsNoTracking()
            .Where(e => e.UniverseId == universeId && e.Status != "archived")
            .Select(e => new { e.Id, e.EntityType, e.Name, Json = e.Record != null ? e.Record.Json : null })
            .ToListAsync(ct);
        var edges = await db.Edges.IgnoreQueryFilters().AsNoTracking()
            .Where(e => e.UniverseId == universeId && e.InvalidatedAt == null)
            .Select(e => new CorobsBenchEdge(e.SourceId, e.TargetId, e.RelationType, e.Weight))
            .ToListAsync(ct);
        var mentions = await (from m in db.BeatEntityMentions.IgnoreQueryFilters().AsNoTracking()
                              join e in db.Entities.IgnoreQueryFilters() on m.EntityId equals e.Id
                              where e.UniverseId == universeId
                              select new { m.BeatId, m.EntityId }).ToListAsync(ct);
        return new CorobsBenchInput(
            ents.Select(e => new CorobsBenchEntity(e.Id, e.EntityType, e.Name, e.Json)).ToList(),
            edges, mentions.Select(m => (m.BeatId, m.EntityId)).ToList());
    }
}
