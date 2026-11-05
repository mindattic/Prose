using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Corobs.Converters;
using Corobs.Geometry;
using Corobs.Seeding;

namespace Prose.Core.Services.Corobs;

/// <summary>
/// What COROBS knows about one entity: the facts its plies are built from. Every reference is an
/// id, never a name — hand pass 2's finding (RFC 0016 spec point 1): the 2026-10-30 renames broke
/// every name-keyed list, so a ply keyed by name would rot the same way.
/// </summary>
public sealed record CorobsEntityFacts(
    Guid Id,
    string Type,
    string Name,
    DateTime ModifiedAt,
    Guid? Faction,
    Guid? Turf,
    string? Range,
    string? LifeStatus,
    string? Species,
    (double Lat, double Lng)? Geo,
    double? EraYear,
    IReadOnlyList<(string Relation, Guid Other)> Edges,
    IReadOnlyList<DateTime> StateTimes);

/// <summary>One ply's definition: key, converter kind, and the fields that feed it.</summary>
public sealed record CorobsPlyDef(string Key, string Kind, string Fields, bool Mined);

/// <summary>A computed point before it is stored.</summary>
public sealed record CorobsComputedPoint(Guid EntityId, DateTime? AtStoryTime, Correlithm Point);

/// <summary>
/// The pure half of the Prose adapter: facts → points, deterministic from the space's seed and each
/// ply's pinned range. No database, no I/O, no LLM. The science (unit N-cube, converters, string COs,
/// miners) is the Corobs library; this class only decides which record field feeds which ply.
/// </summary>
public static class CorobsGeometry
{
    /// <summary>The seed a universe's space is created with when config <c>Corobs:Seed</c> is unset
    /// (the year Lawrence published <i>Correlithm Object Technology</i>).</summary>
    public const long DefaultSeed = 2004;

    public const int DefaultCells = 64;

    /// <summary>The plies, in build order. The time ply is a trajectory (one point per dated state
    /// change) and is not mined for patterns; every other ply is static and mined.</summary>
    public static readonly IReadOnlyList<CorobsPlyDef> Plies =
    [
        new("type",    "cardinal",   "Entities.EntityType", true),
        new("faction", "cardinal",   "CharacterAffiliations.FactionId (first resolved); a faction is its own", true),
        new("turf",    "cardinal",   "CharacterHomeTurfs.PlaceId (first resolved); a place is its own", true),
        new("range",   "ordinal",    "Characters.TerritoryRange local<regional<continental<global", true),
        new("status",  "cardinal",   "Characters.LifeStatus", true),
        new("species", "cardinal",   "Characters.Species", true),
        new("geo",     "string2d",   "Places.GeoLat/GeoLng; a character takes its home turf's", true),
        new("era",     "continuous", "Entities.InWorldCreatedDate (year)", true),
        new("graph",   "graph",      "live Edges: normalised mean of (relation, neighbour) COs", true),
        new("time",    "time",       "EntityStateEvents.AtStoryTime (trajectory)", false),
    ];

    public static readonly IReadOnlyList<string> RangeOrder = ["local", "regional", "continental", "global"];

    /// <summary>The range a ply pins at creation, from the data then present. Stored in
    /// <c>CorobsPly.SourceFieldsJson</c> so later records never shift every point.</summary>
    public static string PinnedRangeJson(CorobsPlyDef ply, IReadOnlyList<CorobsEntityFacts> facts)
    {
        var o = new Dictionary<string, object?> { ["fields"] = ply.Fields };
        switch (ply.Kind)
        {
            case "string2d":
            {
                var g = facts.Where(f => f.Geo is not null).Select(f => f.Geo!.Value).ToList();
                if (g.Count > 0)
                {
                    o["minLat"] = Math.Floor(g.Min(x => x.Lat)) - 1; o["maxLat"] = Math.Ceiling(g.Max(x => x.Lat)) + 1;
                    o["minLng"] = Math.Floor(g.Min(x => x.Lng)) - 1; o["maxLng"] = Math.Ceiling(g.Max(x => x.Lng)) + 1;
                }
                break;
            }
            case "continuous":
            {
                var y = facts.Where(f => f.EraYear is not null).Select(f => f.EraYear!.Value).ToList();
                if (y.Count > 0) { o["min"] = Math.Floor(y.Min() / 100) * 100 - 100; o["max"] = Math.Ceiling(y.Max() / 100) * 100 + 100; }
                break;
            }
            case "time":
            {
                var t = facts.SelectMany(f => f.StateTimes).Select(YearOf).ToList();
                if (t.Count > 0) { o["min"] = Math.Floor(t.Min() / 100) * 100 - 100; o["max"] = Math.Ceiling(t.Max() / 100) * 100 + 100; }
                break;
            }
        }
        return JsonSerializer.Serialize(o);
    }

    /// <summary>Every point of one ply. A missing field yields no point (zero bits, never a false match).</summary>
    public static IReadOnlyList<CorobsComputedPoint> Compute(
        CorobsPlyDef ply, string pinnedJson, long seed, int cells, IReadOnlyList<CorobsEntityFacts> facts)
    {
        var root = new SeededPrng(seed).Derive("ply:" + ply.Key);
        var pins = Pins(pinnedJson);
        var result = new List<CorobsComputedPoint>();
        switch (ply.Key)
        {
            case "type":    Cardinal(f => f.Type.ToLowerInvariant()); break;
            case "faction": Cardinal(f => f.Faction?.ToString("N") ?? (f.Type == "faction" ? f.Id.ToString("N") : null)); break;
            case "turf":    Cardinal(f => f.Turf?.ToString("N") ?? (f.Type == "place" ? f.Id.ToString("N") : null)); break;
            case "status":  Cardinal(f => Norm(f.LifeStatus)); break;
            case "species": Cardinal(f => Norm(f.Species)); break;
            case "range":
            {
                var conv = new OrdinalConverter<string>(root, cells, RangeOrder, StringComparer.Ordinal);
                foreach (var f in facts)
                    if (Norm(f.Range) is { } r && RangeOrder.Contains(r)) result.Add(new(f.Id, null, conv.Convert(r)));
                break;
            }
            case "geo":
            {
                if (!pins.TryGetValue("minLat", out var minLat)) break;
                var conv = new PlanarConverter(root, cells, pins["minLng"], pins["maxLng"], minLat, pins["maxLat"]);
                foreach (var f in facts)
                    if (f.Geo is { } g) result.Add(new(f.Id, null, conv.Convert((g.Lng, g.Lat))));
                break;
            }
            case "era":
            {
                if (!pins.TryGetValue("min", out var min)) break;
                var conv = new ContinuousConverter(root, cells, min, pins["max"]);
                foreach (var f in facts)
                    if (f.EraYear is { } y) result.Add(new(f.Id, null, conv.Convert(y)));
                break;
            }
            case "graph":
            {
                var ids = new CardinalConverter<string>(root, cells);
                foreach (var f in facts)
                {
                    if (f.Edges.Count == 0) continue;
                    var keys = f.Edges.Select(e => e.Relation + "|" + e.Other.ToString("N")).Distinct(StringComparer.Ordinal)
                        .OrderBy(k => k, StringComparer.Ordinal).ToList();
                    result.Add(new(f.Id, null, NormalisedMean(keys.Select(k => ids.Convert(k)).ToList(), cells)));
                }
                break;
            }
            case "time":
            {
                if (!pins.TryGetValue("min", out var min)) break;
                var conv = new ContinuousConverter(root, cells, min, pins["max"]);
                foreach (var f in facts)
                    foreach (var t in f.StateTimes.Distinct().Order())
                        result.Add(new(f.Id, t, conv.Convert(YearOf(t))));
                break;
            }
        }
        return result;

        void Cardinal(Func<CorobsEntityFacts, string?> key)
        {
            var conv = new CardinalConverter<string>(root, cells);
            foreach (var f in facts)
                if (key(f) is { Length: > 0 } k) result.Add(new(f.Id, null, conv.Convert(k)));
        }
    }

    /// <summary>
    /// The graph ply's "invisible strings that pull": the mean of the neighbour COs, re-spread so
    /// it keeps the cube's statistics. A plain mean of k random points collapses toward the
    /// midpoint (spread 1/√k), which would make every well-connected entity a false neighbour of
    /// every other; scaling the deviation by √k restores it, so two entities sharing j of k
    /// neighbours are correlated by about j/k and two with none sit at standard distance.
    /// </summary>
    public static Correlithm NormalisedMean(IReadOnlyList<Correlithm> points, int cells)
    {
        if (points.Count == 1) return points[0];
        var mean = Correlithm.Centroid(points.ToArray());
        var scale = Math.Sqrt(points.Count);
        var c = new double[cells];
        for (var i = 0; i < cells; i++) c[i] = Math.Clamp(0.5 + (mean[i] - 0.5) * scale, 0, 1);
        return new Correlithm(c);
    }

    public static double YearOf(DateTime t) => t.Year + (t.DayOfYear - 1) / 366.0;

    public static string? Norm(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim().ToLowerInvariant();

    /// <summary>float[N] little-endian — what CorobsPoint.Coords and CorobsPattern.Centroid hold.</summary>
    public static byte[] Pack(Correlithm p)
    {
        var bytes = new byte[p.Dimensions * 4];
        for (var i = 0; i < p.Dimensions; i++)
            BitConverter.TryWriteBytes(bytes.AsSpan(i * 4), (float)p[i]);
        if (!BitConverter.IsLittleEndian)
            for (var i = 0; i < p.Dimensions; i++) bytes.AsSpan(i * 4, 4).Reverse();
        return bytes;
    }

    public static Correlithm Unpack(byte[] bytes)
    {
        var c = new double[bytes.Length / 4];
        var buf = new byte[4];
        for (var i = 0; i < c.Length; i++)
        {
            Array.Copy(bytes, i * 4, buf, 0, 4);
            if (!BitConverter.IsLittleEndian) Array.Reverse(buf);
            c[i] = Math.Clamp(BitConverter.ToSingle(buf, 0), 0f, 1f);
        }
        return new Correlithm(c);
    }

    /// <summary>A stored point round-trips through float, so compare packed bytes, not doubles.</summary>
    public static Correlithm RoundTrip(Correlithm p) => Unpack(Pack(p));

    /// <summary>Hash of an entity's live edges — Edge writes don't bump Entities.ModifiedAt.</summary>
    public static string EdgesFingerprint(IReadOnlyList<(string Relation, Guid Other)> edges)
    {
        if (edges.Count == 0) return "";
        var s = string.Join("\n", edges.Select(e => e.Relation + "|" + e.Other.ToString("N")).Order(StringComparer.Ordinal));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s)))[..32];
    }

    private static Dictionary<string, double> Pins(string json)
    {
        var d = new Dictionary<string, double>(StringComparer.Ordinal);
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
            foreach (var p in doc.RootElement.EnumerateObject())
                if (p.Value.ValueKind == JsonValueKind.Number) d[p.Name] = p.Value.GetDouble();
        }
        catch (JsonException) { }
        return d;
    }

    internal static string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
}
