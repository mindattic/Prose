using Corobs.Geometry;
using Corobs.Mining;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services.Corobs;

/// <summary>Thrown by every COROBS entry point while the switch is off. Callers turn it into
/// the single answer <c>disabled</c>; nothing has been read or written when it is thrown.</summary>
public sealed class CorobsDisabledException() : InvalidOperationException(
    "disabled — COROBS is switched off (prose --corobs enable, or /corobs on). Nothing was read or written.");

/// <summary>
/// The master switch (RFC 0016 §6.1): <see cref="SettingsService.CorobsEnabled"/>, default off.
/// Every COROBS entry point asks it first. Off hides the whole new way and deletes nothing.
/// </summary>
public sealed class CorobsSwitch(SettingsService settings)
{
    public bool IsOn => settings.CorobsEnabled;

    public void Set(bool on)
    {
        settings.CorobsEnabled = on;
        settings.Flush();
    }

    /// <summary>Throws <see cref="CorobsDisabledException"/> when off.</summary>
    public void Require()
    {
        if (!IsOn) throw new CorobsDisabledException();
    }
}

public sealed record CorobsPlyCount(string Key, string Kind, int Points, int Entities);
public sealed record CorobsStatus(bool Enabled, Guid? SpaceId, long? Seed, int? N, string? SpaceStatus, DateTime? BuiltAt,
    IReadOnlyList<CorobsPlyCount> Plies, IReadOnlyDictionary<string, int> Patterns, IReadOnlyDictionary<string, int> Rules,
    IReadOnlyDictionary<string, int> Hits);
public sealed record CorobsBuildReport(Guid SpaceId, long Seed, int N, int Entities, int PointsWritten, int PointsUnchanged,
    int PointsDeleted, int StalePoints, int PatternsNew, int PatternsKept, int PatternsSuperseded, int RulesNew, int RulesKept,
    int RulesSuperseded, int HitsOpened, int HitsStillOpen, int HitsClosedByCascade, TimeSpan Elapsed);
public sealed record CorobsPatternRow(Guid Id, string Ply, int Members, double Bits, double Radius, string Status, string? Label,
    Guid? RulingId, DateTime? AtStoryTime, IReadOnlyList<string> Sample);
public sealed record CorobsRuleRow(Guid Id, string SourcePly, Guid SourcePatternId, string? SourceLabel, string TargetPly,
    Guid TargetPatternId, string? TargetLabel, double Support, double Bits, string Status, Guid? RulingId);
public sealed record CorobsHitRow(Guid Id, int Rank, Guid EntityId, string Entity, string EntityType, Guid RuleId, string Rule,
    double Bits, double SourceDist, double TargetDist, string Status, bool ClosedByCascade);
public sealed record CorobsPlyPosition(string Ply, string Kind, bool HasPoint, Guid? NearestPatternId, string? PatternLabel,
    string? PatternStatus, int? PatternMembers, double? Distance, double? Bits, bool Inside);
public sealed record CorobsExplain(Guid EntityId, string Name, string Type, IReadOnlyList<CorobsPlyPosition> Plies,
    IReadOnlyList<DateTime> Trajectory, DateTime? At, DateTime? TrajectoryPointAt, IReadOnlyList<CorobsHitRow> OpenHits);

/// <summary>
/// COROBS for one universe (RFC 0016 §3.2): builds the persistent, parallel entity geometry from
/// the records, live Edges and state events; mines proposed patterns and rules; cues ranked hits
/// for confirmed rules; and on every rebuild closes the hits a fix healed (<c>ClosedByCascade</c>).
/// <para>Boundaries: reads Entities, Characters (+ home turf / affiliation bridges), Places, Edges
/// and EntityStateEvents; writes only <c>Corobs*</c> rows. Never writes an Edge, a record, a beat or
/// a ruling. No LLM. A pattern or rule is confirmed only with a ruling id; a hit closes only with a
/// ruling or order id, or by cascade.</para>
/// </summary>
public sealed class CorobsService(
    IDbContextFactory<ProseDbContext> dbFactory,
    CorobsSwitch corobsSwitch,
    IUniverseContext universe,
    IConfiguration? configuration = null)
{
    /// <summary>Pattern threshold in standard deviations below standard distance (RFC 0016: 6σ,
    /// ≈52 bits at N = 64; 3σ would cluster tens of thousands of pairs by chance).</summary>
    public double MinDeviations => Double("Corobs:MinDeviations", 6);
    public int MinMembers => (int)Double("Corobs:MinMembers", 5);
    public double MinSupport => Double("Corobs:MinSupport", 0.8);
    /// <summary>A hit's target distance must be at least μ − this many σ (default 1).</summary>
    public double TargetDeviations => Double("Corobs:TargetDeviations", 1);

    private double Double(string key, double fallback) =>
        double.TryParse(configuration?[key], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : fallback;

    private long? ConfiguredSeed =>
        long.TryParse(configuration?["Corobs:Seed"], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var s) ? s : null;

    private Guid UniverseId => universe.CurrentId != Guid.Empty
        ? universe.CurrentId
        : throw new InvalidOperationException("COROBS needs a universe: pass --universe <slug>.");

    // ── Status ──────────────────────────────────────────────────────────────

    public async Task<CorobsStatus> StatusAsync(CancellationToken ct = default)
    {
        var empty = new Dictionary<string, int>();
        if (!corobsSwitch.IsOn) return new CorobsStatus(false, null, null, null, null, null, [], empty, empty, empty);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var space = await db.CorobsSpaces.AsNoTracking().FirstOrDefaultAsync(s => s.UniverseId == UniverseId, ct);
        if (space is null) return new CorobsStatus(true, null, null, null, "not built", null, [], empty, empty, empty);
        var plies = await db.CorobsPlies.AsNoTracking().Where(p => p.SpaceId == space.Id).ToListAsync(ct);
        var plyIds = plies.Select(p => p.Id).ToList();
        var counts = await db.CorobsPoints.AsNoTracking().Where(p => plyIds.Contains(p.PlyId))
            .GroupBy(p => p.PlyId).Select(g => new { g.Key, Points = g.Count(), Entities = g.Select(x => x.EntityId).Distinct().Count() })
            .ToListAsync(ct);
        var plyRows = CorobsGeometry.Plies.Select(d => plies.FirstOrDefault(p => p.Key == d.Key) is { } p
                ? new CorobsPlyCount(d.Key, d.Kind, counts.FirstOrDefault(c => c.Key == p.Id)?.Points ?? 0, counts.FirstOrDefault(c => c.Key == p.Id)?.Entities ?? 0)
                : new CorobsPlyCount(d.Key, d.Kind, 0, 0)).ToList();
        var patternIds = await db.CorobsPatterns.AsNoTracking().Where(p => plyIds.Contains(p.PlyId)).Select(p => new { p.Id, p.Status }).ToListAsync(ct);
        var pids = patternIds.Select(p => p.Id).ToList();
        var rules = await db.CorobsRules.AsNoTracking().Where(r => pids.Contains(r.SourcePatternId)).Select(r => new { r.Id, r.Status }).ToListAsync(ct);
        var rids = rules.Select(r => r.Id).ToList();
        var hits = await db.CorobsHits.AsNoTracking().Where(h => rids.Contains(h.RuleId)).Select(h => h.Status).ToListAsync(ct);
        return new CorobsStatus(true, space.Id, space.Seed, space.N, space.Status, space.BuiltAt, plyRows,
            patternIds.GroupBy(p => p.Status).ToDictionary(g => g.Key, g => g.Count()),
            rules.GroupBy(r => r.Status).ToDictionary(g => g.Key, g => g.Count()),
            hits.GroupBy(h => h).ToDictionary(g => g.Key, g => g.Count()));
    }

    // ── Build ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Rebuilds the geometry: computes every point from the world as it stands, writes only the
    /// points that moved or went stale (<paramref name="full"/> rewrites them all), re-mines patterns
    /// and rules (a confirmed or rejected one keeps its status when it is found again; one that
    /// vanished is superseded), and re-cues every confirmed rule across the universe — an open hit
    /// that is no longer found closes with <c>ClosedByCascade</c>.
    /// </summary>
    public async Task<CorobsBuildReport> BuildAsync(bool full = false, CancellationToken ct = default)
    {
        corobsSwitch.Require();
        var started = DateTime.UtcNow;
        var uid = UniverseId;
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var facts = await LoadFactsAsync(db, uid, ct);

        var space = await db.CorobsSpaces.FirstOrDefaultAsync(s => s.UniverseId == uid, ct);
        if (space is null)
        {
            space = new CorobsSpace { UniverseId = uid, Seed = ConfiguredSeed ?? CorobsGeometry.DefaultSeed, N = CorobsGeometry.DefaultCells };
            db.CorobsSpaces.Add(space);
            await db.SaveChangesAsync(ct);
        }
        else if (ConfiguredSeed is { } cfg && cfg != space.Seed)
            throw new InvalidOperationException($"Corobs:Seed is {cfg} but this universe's space was built with seed {space.Seed}; refusing to build (same seed → same geometry).");

        var plies = await db.CorobsPlies.Where(p => p.SpaceId == space.Id).ToListAsync(ct);
        foreach (var def in CorobsGeometry.Plies.Where(d => plies.All(p => p.Key != d.Key)))
        {
            var row = new CorobsPly { SpaceId = space.Id, Key = def.Key, ConverterKind = def.Kind, Cells = space.N, SourceFieldsJson = CorobsGeometry.PinnedRangeJson(def, facts) };
            db.CorobsPlies.Add(row);
            plies.Add(row);
        }
        await db.SaveChangesAsync(ct);

        var factById = facts.ToDictionary(f => f.Id);
        var fingerprints = facts.ToDictionary(f => f.Id, f => CorobsGeometry.EdgesFingerprint(f.Edges));
        int written = 0, unchanged = 0, deleted = 0, stale = 0;
        var mined = new Dictionary<string, IReadOnlyDictionary<Guid, Correlithm>>(StringComparer.Ordinal);
        var minedPatterns = new Dictionary<string, IReadOnlyList<Pattern<Guid>>>(StringComparer.Ordinal);
        var miner = new PatternMiner(MinDeviations, MinMembers);

        db.ChangeTracker.AutoDetectChangesEnabled = false;
        foreach (var def in CorobsGeometry.Plies)
        {
            ct.ThrowIfCancellationRequested();
            var ply = plies.Single(p => p.Key == def.Key);
            var computed = CorobsGeometry.Compute(def, ply.SourceFieldsJson, space.Seed, ply.Cells, facts);
            var existing = await db.CorobsPoints.Where(p => p.PlyId == ply.Id).ToListAsync(ct);
            var byKey = existing.GroupBy(p => (p.EntityId, p.AtStoryTime)).ToDictionary(g => g.Key, g => g.ToList());
            var seen = new HashSet<(Guid, DateTime?)>();
            foreach (var c in computed)
            {
                var f = factById[c.EntityId];
                var bytes = CorobsGeometry.Pack(c.Point);
                var key = (c.EntityId, c.AtStoryTime);
                seen.Add(key);
                if (byKey.TryGetValue(key, out var rows))
                {
                    var row = rows[0];
                    foreach (var dup in rows.Skip(1)) { db.CorobsPoints.Remove(dup); deleted++; }
                    var isStale = row.RecordModifiedAt != f.ModifiedAt || row.EdgesFingerprint != fingerprints[f.Id];
                    if (isStale) stale++;
                    if (!full && !isStale && row.Coords.AsSpan().SequenceEqual(bytes)) { unchanged++; continue; }
                    row.Coords = bytes; row.RecordModifiedAt = f.ModifiedAt; row.EdgesFingerprint = fingerprints[f.Id]; row.BuiltAt = DateTime.UtcNow;
                    db.Entry(row).State = EntityState.Modified;
                    written++;
                }
                else
                {
                    db.CorobsPoints.Add(new CorobsPoint
                    {
                        SpaceId = space.Id, PlyId = ply.Id, EntityId = c.EntityId, AtStoryTime = c.AtStoryTime, Coords = bytes,
                        RecordModifiedAt = f.ModifiedAt, EdgesFingerprint = fingerprints[f.Id],
                    });
                    written++;
                }
            }
            foreach (var row in existing.Where(r => !seen.Contains((r.EntityId, r.AtStoryTime))))
            {
                if (db.Entry(row).State == EntityState.Deleted) continue;
                db.CorobsPoints.Remove(row); deleted++;
            }
            await db.SaveChangesAsync(ct);
            db.ChangeTracker.Clear();

            if (!def.Mined) continue;
            // Mine on the stored (float) form so a later read of the table sees the same geometry.
            var points = computed.Select(c => new PlyPoint<Guid>(c.EntityId, CorobsGeometry.RoundTrip(c.Point))).ToList();
            mined[def.Key] = points.ToDictionary(p => p.Id, p => p.Point);
            minedPatterns[def.Key] = miner.Mine(points);
        }
        db.ChangeTracker.AutoDetectChangesEnabled = true;

        // Patterns: match each mined pattern to a stored one on the same ply (nearest centroid
        // within the pattern threshold); the stored row keeps its id, status, label and ruling.
        var threshold = NCube.DistanceAtDeviations(space.N, MinDeviations);
        int pNew = 0, pKept = 0, pSuper = 0;
        var patternIdOf = new Dictionary<(string Ply, int Index), Guid>();
        var storedPatterns = new Dictionary<Guid, CorobsPattern>();
        foreach (var def in CorobsGeometry.Plies.Where(d => d.Mined))
        {
            var ply = plies.Single(p => p.Key == def.Key);
            var stored = await db.CorobsPatterns.Where(p => p.PlyId == ply.Id && p.Status != "superseded").ToListAsync(ct);
            var claimed = new HashSet<Guid>();
            var list = minedPatterns.GetValueOrDefault(def.Key) ?? [];
            for (var i = 0; i < list.Count; i++)
            {
                var m = list[i];
                var match = stored.Where(s => !claimed.Contains(s.Id))
                    .Select(s => (Row: s, D: CorobsGeometry.Unpack(s.Centroid).DistanceTo(m.Centroid)))
                    .Where(x => x.D <= threshold).OrderBy(x => x.D).ThenBy(x => x.Row.Id).Select(x => x.Row).FirstOrDefault();
                if (match is null)
                {
                    match = new CorobsPattern { PlyId = ply.Id };
                    db.CorobsPatterns.Add(match);
                    pNew++;
                }
                else pKept++;
                claimed.Add(match.Id);
                match.Centroid = CorobsGeometry.Pack(m.Centroid); match.Radius = m.Radius; match.MemberCount = m.Members.Count;
                match.Bits = m.Bits; match.UpdatedAt = DateTime.UtcNow;
                patternIdOf[(def.Key, i)] = match.Id;
                storedPatterns[match.Id] = match;
            }
            foreach (var s in stored.Where(s => !claimed.Contains(s.Id) && s.Status != "rejected"))
            {
                s.Status = "superseded"; s.UpdatedAt = DateTime.UtcNow; pSuper++;
            }
        }
        await db.SaveChangesAsync(ct);

        // Rules: every ordered pair of mined plies.
        var ruleMiner = new RuleMiner(MinSupport);
        var minedRules = new List<(Rule<Guid> Rule, Guid Src, Guid Tgt)>();
        var mineable = CorobsGeometry.Plies.Where(d => d.Mined).Select(d => d.Key).ToList();
        foreach (var s in mineable)
            foreach (var t in mineable.Where(t => t != s))
            {
                var sp = minedPatterns.GetValueOrDefault(s) ?? []; var tp = minedPatterns.GetValueOrDefault(t) ?? [];
                if (sp.Count == 0 || tp.Count == 0) continue;
                foreach (var r in ruleMiner.Mine(s, sp, t, tp))
                    minedRules.Add((r, patternIdOf[(s, IndexOf(sp, r.Source))], patternIdOf[(t, IndexOf(tp, r.Target))]));
            }
        var plyIdsAll = plies.Select(p => p.Id).ToList();
        var everyPatternId = await db.CorobsPatterns.Where(p => plyIdsAll.Contains(p.PlyId)).Select(p => p.Id).ToListAsync(ct);
        var storedRules = await db.CorobsRules.Where(r => everyPatternId.Contains(r.SourcePatternId)).ToListAsync(ct);
        int rNew = 0, rKept = 0, rSuper = 0;
        var foundRuleIds = new HashSet<Guid>();
        var liveRules = new Dictionary<Guid, Rule<Guid>>();
        foreach (var (rule, src, tgt) in minedRules)
        {
            var row = storedRules.FirstOrDefault(r => r.SourcePatternId == src && r.TargetPatternId == tgt);
            if (row is null)
            {
                row = new CorobsRule { SourcePatternId = src, TargetPatternId = tgt };
                db.CorobsRules.Add(row); storedRules.Add(row); rNew++;
            }
            else
            {
                rKept++;
                if (row.Status == "superseded") row.Status = "proposed";
            }
            row.Support = rule.Support; row.Bits = rule.Bits; row.UpdatedAt = DateTime.UtcNow;
            foundRuleIds.Add(row.Id);
            liveRules[row.Id] = rule;
        }
        foreach (var r in storedRules.Where(r => !foundRuleIds.Contains(r.Id) && r.Status is "proposed" or "confirmed"))
        {
            r.Status = "superseded"; r.UpdatedAt = DateTime.UtcNow; rSuper++;
        }
        await db.SaveChangesAsync(ct);

        // Cue: confirmed rules only; reconcile with the open hits across the whole universe.
        var confirmed = storedRules.Where(r => r.Status == "confirmed" && liveRules.ContainsKey(r.Id)).ToList();
        var cue = new Cue(TargetDeviations);
        var cueRules = confirmed.Select(r => liveRules[r.Id]).ToList();
        var ruleIdOf = confirmed.ToDictionary(r => liveRules[r.Id], r => r.Id, ReferenceEqualityComparer.Instance);
        IReadOnlyList<Hit<Guid>> found = cueRules.Count == 0 ? [] : cue.Find(cueRules, mined);
        var ruleIdsAll = storedRules.Select(r => r.Id).ToList();
        var openHits = await db.CorobsHits.Where(h => ruleIdsAll.Contains(h.RuleId) && h.Status == "open").ToListAsync(ct);
        var dismissed = (await db.CorobsHits.Where(h => ruleIdsAll.Contains(h.RuleId) && h.Status == "dismissed")
            .Select(h => new { h.RuleId, h.EntityId }).ToListAsync(ct)).Select(x => (x.RuleId, x.EntityId)).ToHashSet();
        int hOpened = 0, hStill = 0, hCascade = 0;
        var foundKeys = new HashSet<(Guid, Guid)>();
        foreach (var h in found)
        {
            var rid = ruleIdOf[h.Rule];
            var key = (rid, h.Id);
            if (!foundKeys.Add(key) || dismissed.Contains(key)) continue;
            var row = openHits.FirstOrDefault(o => o.RuleId == rid && o.EntityId == h.Id);
            if (row is null)
            {
                row = new CorobsHit { RuleId = rid, EntityId = h.Id };
                db.CorobsHits.Add(row); hOpened++;
            }
            else hStill++;
            row.SourceDist = h.SourceDistance; row.TargetDist = h.TargetDistance; row.Bits = h.Bits; row.Rank = h.Rank;
        }
        foreach (var o in openHits.Where(o => !foundKeys.Contains((o.RuleId, o.EntityId))))
        {
            o.Status = "resolved"; o.ClosedByCascade = true; o.ClosedAt = DateTime.UtcNow; hCascade++;
        }
        // The point loop cleared the tracker, so the space row is re-attached to record the build.
        db.Attach(space);
        space.Status = "built"; space.BuiltAt = DateTime.UtcNow;
        db.Entry(space).State = EntityState.Modified;
        await db.SaveChangesAsync(ct);

        return new CorobsBuildReport(space.Id, space.Seed, space.N, facts.Count, written, unchanged, deleted, stale,
            pNew, pKept, pSuper, rNew, rKept, rSuper, hOpened, hStill, hCascade, DateTime.UtcNow - started);
    }

    private static int IndexOf(IReadOnlyList<Pattern<Guid>> list, Pattern<Guid> p)
    {
        for (var i = 0; i < list.Count; i++) if (ReferenceEquals(list[i], p)) return i;
        throw new InvalidOperationException("Rule names a pattern the miner did not return.");
    }

    /// <summary>Reads the world facts every ply is built from — ids only, never names, as references.</summary>
    internal static async Task<List<CorobsEntityFacts>> LoadFactsAsync(ProseDbContext db, Guid uid, CancellationToken ct)
    {
        var entities = await db.Entities.IgnoreQueryFilters().AsNoTracking().Where(e => e.UniverseId == uid)
            .Select(e => new { e.Id, e.EntityType, e.Name, e.ModifiedAt, e.InWorldCreatedDate }).ToListAsync(ct);
        var ids = entities.Select(e => e.Id).ToHashSet();

        var chars = await (from c in db.Characters.IgnoreQueryFilters().AsNoTracking()
                           join e in db.Entities.IgnoreQueryFilters() on c.Id equals e.Id
                           where e.UniverseId == uid
                           select new { c.Id, c.TerritoryRange, c.LifeStatus, c.Species }).ToListAsync(ct);
        var turfs = await (from t in db.CharacterHomeTurfs.IgnoreQueryFilters().AsNoTracking()
                           join e in db.Entities.IgnoreQueryFilters() on t.CharacterId equals e.Id
                           where e.UniverseId == uid && t.PlaceId != null
                           select new { t.CharacterId, t.PlaceId, t.Position }).ToListAsync(ct);
        var affs = await (from a in db.CharacterAffiliations.IgnoreQueryFilters().AsNoTracking()
                          join e in db.Entities.IgnoreQueryFilters() on a.CharacterId equals e.Id
                          where e.UniverseId == uid && a.FactionId != null
                          select new { a.CharacterId, a.FactionId, a.Position }).ToListAsync(ct);
        var places = await (from p in db.Places.IgnoreQueryFilters().AsNoTracking()
                            join e in db.Entities.IgnoreQueryFilters() on p.Id equals e.Id
                            where e.UniverseId == uid
                            select new { p.Id, p.GeoLat, p.GeoLng }).ToListAsync(ct);
        var edges = await db.Edges.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.UniverseId == uid && x.InvalidatedAt == null)
            .Select(x => new { x.SourceId, x.TargetId, x.RelationType }).ToListAsync(ct);
        var states = await db.EntityStateEvents.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.UniverseId == uid).Select(x => new { x.EntityId, x.AtStoryTime }).ToListAsync(ct);

        var charById = chars.ToDictionary(c => c.Id);
        var turfOf = turfs.Where(t => ids.Contains(t.PlaceId!.Value)).GroupBy(t => t.CharacterId)
            .ToDictionary(g => g.Key, g => g.OrderBy(t => t.Position).First().PlaceId!.Value);
        var factionOf = affs.Where(a => ids.Contains(a.FactionId!.Value)).GroupBy(a => a.CharacterId)
            .ToDictionary(g => g.Key, g => g.OrderBy(a => a.Position).First().FactionId!.Value);
        // (0,0) is "never placed", not a point off Africa — law 01a19aa1.
        var geoOf = places.Where(p => p.GeoLat != 0 || p.GeoLng != 0).ToDictionary(p => p.Id, p => (p.GeoLat, p.GeoLng));
        var edgesOf = new Dictionary<Guid, List<(string, Guid)>>();
        foreach (var x in edges)
        {
            if (!ids.Contains(x.SourceId) || !ids.Contains(x.TargetId) || x.SourceId == x.TargetId) continue;
            var rel = (x.RelationType ?? "").Trim().ToLowerInvariant();
            Add(x.SourceId, (rel, x.TargetId));
            Add(x.TargetId, (rel, x.SourceId));
        }
        var statesOf = states.GroupBy(s => s.EntityId).ToDictionary(g => g.Key, g => g.Select(s => s.AtStoryTime).ToList());

        return entities.OrderBy(e => e.Id).Select(e =>
        {
            charById.TryGetValue(e.Id, out var c);
            Guid? turf = turfOf.TryGetValue(e.Id, out var t) ? t : null;
            (double, double)? geo = geoOf.TryGetValue(e.Id, out var g) ? g
                : turf is { } tp && geoOf.TryGetValue(tp, out var tg) ? tg : null;
            return new CorobsEntityFacts(
                e.Id, (e.EntityType ?? "").Trim().ToLowerInvariant(), e.Name, e.ModifiedAt,
                factionOf.TryGetValue(e.Id, out var fa) ? fa : null, turf,
                c?.TerritoryRange, c?.LifeStatus, c?.Species, geo,
                e.InWorldCreatedDate is { } d ? CorobsGeometry.YearOf(d) : null,
                edgesOf.TryGetValue(e.Id, out var ed) ? ed : [],
                statesOf.TryGetValue(e.Id, out var st) ? st : []);
        }).ToList();

        void Add(Guid id, (string, Guid) edge)
        {
            if (!edgesOf.TryGetValue(id, out var l)) edgesOf[id] = l = [];
            l.Add(edge);
        }
    }

    // ── Reads ───────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<CorobsPatternRow>> PatternsAsync(string? ply = null, DateTime? at = null, string? status = null, int top = 50, CancellationToken ct = default)
    {
        corobsSwitch.Require();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var (space, plies) = await SpaceAsync(db, ct);
        if (space is null) return [];
        var plyIds = plies.Where(p => ply is null || p.Key == ply).Select(p => p.Id).ToList();
        var q = db.CorobsPatterns.AsNoTracking().Where(p => plyIds.Contains(p.PlyId));
        q = status is null ? q.Where(p => p.Status != "superseded") : q.Where(p => p.Status == status);
        if (at is { } when) q = q.Where(p => p.AtStoryTime == null || p.AtStoryTime <= when);
        var rows = await q.OrderByDescending(p => p.Bits).ThenByDescending(p => p.MemberCount).ThenBy(p => p.Id).Take(Math.Max(1, top)).ToListAsync(ct);
        var result = new List<CorobsPatternRow>();
        foreach (var p in rows)
        {
            var key = plies.Single(x => x.Id == p.PlyId).Key;
            result.Add(new CorobsPatternRow(p.Id, key, p.MemberCount, p.Bits, p.Radius, p.Status, p.Label, p.ConfirmedRulingId, p.AtStoryTime,
                await SampleMembersAsync(db, p, 5, ct)));
        }
        return result;
    }

    /// <summary>Names of a pattern's members (points within its radius), for a human to judge it.</summary>
    private static async Task<IReadOnlyList<string>> SampleMembersAsync(ProseDbContext db, CorobsPattern p, int n, CancellationToken ct)
    {
        var ids = await MembersAsync(db, p, ct);
        var take = ids.Take(n).ToList();
        var names = await db.Entities.IgnoreQueryFilters().AsNoTracking().Where(e => take.Contains(e.Id)).Select(e => new { e.Id, e.Name }).ToListAsync(ct);
        return take.Select(id => names.FirstOrDefault(x => x.Id == id)?.Name ?? id.ToString()).ToList();
    }

    private static async Task<List<Guid>> MembersAsync(ProseDbContext db, CorobsPattern p, CancellationToken ct)
    {
        var centroid = CorobsGeometry.Unpack(p.Centroid);
        var points = await db.CorobsPoints.AsNoTracking().Where(x => x.PlyId == p.PlyId && x.AtStoryTime == null)
            .Select(x => new { x.EntityId, x.Coords }).ToListAsync(ct);
        return points.Select(x => (x.EntityId, D: CorobsGeometry.Unpack(x.Coords).DistanceTo(centroid)))
            .Where(x => x.D <= p.Radius + 1e-6).OrderBy(x => x.D).ThenBy(x => x.EntityId).Select(x => x.EntityId).ToList();
    }

    public async Task<IReadOnlyList<CorobsRuleRow>> RulesAsync(string? status = null, int top = 50, CancellationToken ct = default)
    {
        corobsSwitch.Require();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var (space, plies) = await SpaceAsync(db, ct);
        if (space is null) return [];
        var plyIds = plies.Select(p => p.Id).ToList();
        var patterns = await db.CorobsPatterns.AsNoTracking().Where(p => plyIds.Contains(p.PlyId)).ToDictionaryAsync(p => p.Id, ct);
        var pids = patterns.Keys.ToList();
        var q = db.CorobsRules.AsNoTracking().Where(r => pids.Contains(r.SourcePatternId));
        q = status is null ? q.Where(r => r.Status != "superseded") : q.Where(r => r.Status == status);
        var rows = await q.OrderByDescending(r => r.Bits).ThenBy(r => r.Id).Take(Math.Max(1, top)).ToListAsync(ct);
        string PlyOf(Guid pid) => plies.Single(x => x.Id == patterns[pid].PlyId).Key;
        return rows.Select(r => new CorobsRuleRow(r.Id, PlyOf(r.SourcePatternId), r.SourcePatternId, patterns[r.SourcePatternId].Label,
            PlyOf(r.TargetPatternId), r.TargetPatternId, patterns[r.TargetPatternId].Label, r.Support, r.Bits, r.Status, r.ConfirmedRulingId)).ToList();
    }

    public async Task<IReadOnlyList<CorobsHitRow>> CueAsync(string? entity = null, int top = 25, CancellationToken ct = default)
    {
        corobsSwitch.Require();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        Guid? eid = null;
        if (entity is not null) eid = (await ResolveEntityAsync(db, entity, ct)).Id;
        return await HitRowsAsync(db, eid, "open", top, ct);
    }

    private async Task<List<CorobsHitRow>> HitRowsAsync(ProseDbContext db, Guid? entityId, string status, int top, CancellationToken ct, Guid? universeId = null)
    {
        var (space, plies) = await SpaceAsync(db, universeId ?? UniverseId, ct);
        if (space is null) return [];
        var plyIds = plies.Select(p => p.Id).ToList();
        var patterns = await db.CorobsPatterns.AsNoTracking().Where(p => plyIds.Contains(p.PlyId)).ToDictionaryAsync(p => p.Id, ct);
        var pids = patterns.Keys.ToList();
        var rules = await db.CorobsRules.AsNoTracking().Where(r => pids.Contains(r.SourcePatternId)).ToDictionaryAsync(r => r.Id, ct);
        var rids = rules.Keys.ToList();
        var q = db.CorobsHits.AsNoTracking().Where(h => rids.Contains(h.RuleId) && h.Status == status);
        if (entityId is { } id) q = q.Where(h => h.EntityId == id);
        var hits = await q.OrderBy(h => h.Rank).ThenBy(h => h.Id).Take(Math.Max(1, top)).ToListAsync(ct);
        var eids = hits.Select(h => h.EntityId).Distinct().ToList();
        var names = await db.Entities.IgnoreQueryFilters().AsNoTracking().Where(e => eids.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => new { e.Name, e.EntityType }, ct);
        string Describe(CorobsRule r)
        {
            var s = patterns[r.SourcePatternId]; var t = patterns[r.TargetPatternId];
            string P(CorobsPattern p) => $"{plies.Single(x => x.Id == p.PlyId).Key}.[{p.Label ?? p.Id.ToString()[..13]}]";
            return $"{P(s)} >: {P(t)} ({r.Support:P0})";
        }
        return hits.Select(h => new CorobsHitRow(h.Id, h.Rank, h.EntityId, names.GetValueOrDefault(h.EntityId)?.Name ?? "?",
            names.GetValueOrDefault(h.EntityId)?.EntityType ?? "?", h.RuleId, Describe(rules[h.RuleId]), h.Bits, h.SourceDist, h.TargetDist,
            h.Status, h.ClosedByCascade)).ToList();
    }

    /// <param name="universeId">The universe to look in; null = the caller's current one. The wiki
    /// passes the entity's own, since a page can be opened from any universe.</param>
    public async Task<CorobsExplain> ExplainAsync(string entity, DateTime? at = null, Guid? universeId = null, CancellationToken ct = default)
    {
        corobsSwitch.Require();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var uid = universeId ?? UniverseId;
        var e = await ResolveEntityAsync(db, entity, uid, ct);
        var (space, plies) = await SpaceAsync(db, uid, ct);
        if (space is null) return new CorobsExplain(e.Id, e.Name, e.EntityType, [], [], at, null, []);
        var points = await db.CorobsPoints.AsNoTracking().Where(p => p.EntityId == e.Id).ToListAsync(ct);
        var positions = new List<CorobsPlyPosition>();
        foreach (var def in CorobsGeometry.Plies.Where(d => d.Mined))
        {
            var ply = plies.FirstOrDefault(p => p.Key == def.Key);
            var pt = ply is null ? null : points.FirstOrDefault(p => p.PlyId == ply.Id && p.AtStoryTime == null);
            if (ply is null || pt is null) { positions.Add(new(def.Key, def.Kind, false, null, null, null, null, null, null, false)); continue; }
            var co = CorobsGeometry.Unpack(pt.Coords);
            var near = (await db.CorobsPatterns.AsNoTracking().Where(p => p.PlyId == ply.Id && p.Status != "superseded" && p.Status != "rejected").ToListAsync(ct))
                .Select(p => (P: p, D: CorobsGeometry.Unpack(p.Centroid).DistanceTo(co))).OrderBy(x => x.D).ThenBy(x => x.P.Id).FirstOrDefault();
            positions.Add(near.P is null
                ? new(def.Key, def.Kind, true, null, null, null, null, null, null, false)
                : new(def.Key, def.Kind, true, near.P.Id, near.P.Label, near.P.Status, near.P.MemberCount, near.D,
                    NCube.BitContent(space.N, near.D), near.D <= near.P.Radius + 1e-6));
        }
        var timePly = plies.FirstOrDefault(p => p.Key == "time");
        var trajectory = timePly is null ? [] : points.Where(p => p.PlyId == timePly.Id && p.AtStoryTime != null)
            .Select(p => p.AtStoryTime!.Value).Order().ToList();
        DateTime? atPoint = at is { } w ? trajectory.Where(t => t <= w).Select(t => (DateTime?)t).LastOrDefault() : null;
        var hits = await HitRowsAsync(db, e.Id, "open", 50, ct, uid);
        return new CorobsExplain(e.Id, e.Name, e.EntityType, positions, trajectory, at, atPoint, hits);
    }

    // ── Hand decisions (each carries the ruling or order that records it) ───

    public async Task<string> ConfirmPatternAsync(string patternId, string rulingId, string label, CancellationToken ct = default)
    {
        corobsSwitch.Require();
        label = (label ?? "").Trim();
        if (label.Length == 0) throw new ArgumentException("A confirmed pattern needs a hand-written --label (the truth it shows, ≤120 characters).");
        if (label.Length > CorobsPattern.LabelMaxLength) throw new ArgumentException($"--label is {label.Length} characters; the cap is {CorobsPattern.LabelMaxLength}.");
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var p = await FindPatternAsync(db, patternId, ct);
        var ruling = await FindRulingAsync(db, rulingId, ct);
        p.Status = "confirmed"; p.ConfirmedRulingId = ruling; p.Label = label; p.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return $"pattern {p.Id} confirmed by ruling {ruling}: {label}";
    }

    public async Task<string> ConfirmRuleAsync(string ruleId, string rulingId, CancellationToken ct = default)
    {
        corobsSwitch.Require();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var r = await FindRuleAsync(db, ruleId, ct);
        var ruling = await FindRulingAsync(db, rulingId, ct);
        r.Status = "confirmed"; r.ConfirmedRulingId = ruling; r.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return $"rule {r.Id} confirmed by ruling {ruling}; its hits appear at the next build.";
    }

    public async Task<string> RejectAsync(string? patternId, string? ruleId, CancellationToken ct = default)
    {
        corobsSwitch.Require();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        if (patternId is not null)
        {
            var p = await FindPatternAsync(db, patternId, ct);
            p.Status = "rejected"; p.ConfirmedRulingId = null; p.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return $"pattern {p.Id} rejected.";
        }
        if (ruleId is not null)
        {
            var r = await FindRuleAsync(db, ruleId, ct);
            r.Status = "rejected"; r.ConfirmedRulingId = null; r.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return $"rule {r.Id} rejected.";
        }
        throw new ArgumentException("Name --pattern <id> or --rule <id>.");
    }

    /// <summary>Closes an open hit: <paramref name="dismiss"/> = the hand found no contradiction;
    /// otherwise it was fixed. Either way it carries the ruling or order that records why.</summary>
    public async Task<string> ResolveHitAsync(string hitId, string? orderId, string? rulingId, bool dismiss = false, CancellationToken ct = default)
    {
        corobsSwitch.Require();
        if (orderId is null && rulingId is null) throw new ArgumentException("A hit closes only with --order <id> or --ruling <id>.");
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var h = await FindByPrefixAsync(db.CorobsHits, hitId, "hit", ct);
        if (h.Status != "open") throw new ArgumentException($"hit {h.Id} is already {h.Status}.");
        if (orderId is not null)
        {
            var orders = await db.WorkOrders.AsNoTracking().Select(o => o.Id).ToListAsync(ct);
            h.ResolvedByOrderId = Unique(orders, orderId, "work order");
        }
        if (rulingId is not null) h.ResolvedByRulingId = await FindRulingAsync(db, rulingId, ct);
        h.Status = dismiss ? "dismissed" : "resolved"; h.ClosedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return $"hit {h.Id} {h.Status}.";
    }

    // ── Lookup helpers ──────────────────────────────────────────────────────

    private Task<(CorobsSpace? Space, List<CorobsPly> Plies)> SpaceAsync(ProseDbContext db, CancellationToken ct) => SpaceAsync(db, UniverseId, ct);

    private static async Task<(CorobsSpace? Space, List<CorobsPly> Plies)> SpaceAsync(ProseDbContext db, Guid uid, CancellationToken ct)
    {
        var space = await db.CorobsSpaces.AsNoTracking().FirstOrDefaultAsync(s => s.UniverseId == uid, ct);
        if (space is null) return (null, []);
        return (space, await db.CorobsPlies.AsNoTracking().Where(p => p.SpaceId == space.Id).ToListAsync(ct));
    }

    private async Task<CorobsPattern> FindPatternAsync(ProseDbContext db, string id, CancellationToken ct)
    {
        var (space, plies) = await SpaceAsync(db, ct);
        if (space is null) throw new ArgumentException("No COROBS space for this universe yet: run --corobs build.");
        var plyIds = plies.Select(p => p.Id).ToList();
        return await FindByPrefixAsync(db.CorobsPatterns.Where(p => plyIds.Contains(p.PlyId)), id, "pattern", ct);
    }

    private async Task<CorobsRule> FindRuleAsync(ProseDbContext db, string id, CancellationToken ct)
    {
        var (space, plies) = await SpaceAsync(db, ct);
        if (space is null) throw new ArgumentException("No COROBS space for this universe yet: run --corobs build.");
        var plyIds = plies.Select(p => p.Id).ToList();
        var pids = await db.CorobsPatterns.Where(p => plyIds.Contains(p.PlyId)).Select(p => p.Id).ToListAsync(ct);
        return await FindByPrefixAsync(db.CorobsRules.Where(r => pids.Contains(r.SourcePatternId)), id, "rule", ct);
    }

    private async Task<Guid> FindRulingAsync(ProseDbContext db, string id, CancellationToken ct)
    {
        var uid = UniverseId;
        var ids = await db.Rulings.IgnoreQueryFilters().AsNoTracking().Where(r => r.UniverseId == uid).Select(r => r.Id).ToListAsync(ct);
        return Unique(ids, id, "ruling");
    }

    private static async Task<T> FindByPrefixAsync<T>(IQueryable<T> q, string id, string what, CancellationToken ct) where T : class
    {
        var rows = await q.ToListAsync(ct);
        var idProp = typeof(T).GetProperty("Id")!;
        var match = Unique(rows.Select(r => (Guid)idProp.GetValue(r)!).ToList(), id, what);
        return rows.Single(r => (Guid)idProp.GetValue(r)! == match);
    }

    /// <summary>A full id, or an unambiguous prefix of one (rulings are cited as 01a19ebf-9f09).</summary>
    internal static Guid Unique(IReadOnlyCollection<Guid> ids, string reference, string what)
    {
        var r = (reference ?? "").Trim().ToLowerInvariant();
        if (Guid.TryParse(r, out var g)) return ids.Contains(g) ? g : throw new ArgumentException($"No {what} {g} here.");
        var bare = r.Replace("-", "");
        if (bare.Length < 6) throw new ArgumentException($"A {what} id prefix needs at least 6 hex digits: '{reference}'.");
        var hits = ids.Where(i => i.ToString("N").StartsWith(bare, StringComparison.Ordinal)).ToList();
        return hits.Count switch
        {
            1 => hits[0],
            0 => throw new ArgumentException($"No {what} matches '{reference}'."),
            _ => throw new ArgumentException($"'{reference}' matches {hits.Count} {what}s; give more of the id."),
        };
    }

    private Task<Entity> ResolveEntityAsync(ProseDbContext db, string reference, CancellationToken ct) => ResolveEntityAsync(db, reference, UniverseId, ct);

    private static async Task<Entity> ResolveEntityAsync(ProseDbContext db, string reference, Guid uid, CancellationToken ct)
    {
        var r = reference.Trim();
        var q = db.Entities.IgnoreQueryFilters().AsNoTracking().Where(e => e.UniverseId == uid);
        if (Guid.TryParse(r, out var g) && await q.FirstOrDefaultAsync(e => e.Id == g, ct) is { } byId) return byId;
        var bySlug = await q.Where(e => e.Slug == r).ToListAsync(ct);
        if (bySlug.Count == 1) return bySlug[0];
        var byName = await q.Where(e => e.Name == r).ToListAsync(ct);
        if (byName.Count == 1) return byName[0];
        if (bySlug.Count + byName.Count > 1) throw new ArgumentException($"'{reference}' names more than one entity; pass its id.");
        throw new ArgumentException($"No entity '{reference}' in this universe.");
    }
}
