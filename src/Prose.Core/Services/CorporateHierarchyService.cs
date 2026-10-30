using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

/// <summary>
/// The CorpoNation Tier ladder (PRS-T819). 1 is ground-level work (policing, sanitation, the
/// street); 4 is royalty and dynasties. The higher the Tier, the more untouchable. Tier 5 is a
/// rumor nobody holds, so no record may claim it.
/// </summary>
public static class CorporateTier
{
    public const int Lowest = 1;
    public const int Highest = 4;
    public const int Rumor = 5;

    /// <summary>Why <paramref name="tier"/> cannot be stored, or null when it can (1-4, or none).</summary>
    public static string? Problem(int? tier) => tier switch
    {
        null => null,
        Rumor => "Tier 5 is a rumor — no CorpoNation holds it. Tiers run 1 (ground-level work) to 4 (royalty and dynasties).",
        < Lowest or > Highest => $"Tier {tier} does not exist. Tiers run 1 (ground-level work) to 4 (royalty and dynasties).",
        _ => null,
    };

    /// <summary>Throws <see cref="ArgumentException"/> with <see cref="Problem"/>'s words when the tier cannot be stored.</summary>
    public static void Validate(int? tier)
    {
        if (Problem(tier) is { } p) throw new ArgumentException(p);
    }

    /// <summary>The two entity types that carry a Tier and may own things.</summary>
    public static bool IsCorporate(string? entityType) => entityType is "corponation" or "subsidiary";
}

/// <summary>An entity as the hierarchy shows it: who it is, and its Tier when it has one.</summary>
public sealed record CorpEntityRef(Guid Id, string Name, string Slug, string EntityType, int? Tier, Guid UniverseId)
{
    public override string ToString() => Tier is int t ? $"{Name} [{EntityType}, Tier {t}]" : $"{Name} [{EntityType}]";
}

/// <summary>One ownership edge, both ends resolved.</summary>
public sealed record OwnershipLink(Guid Id, CorpEntityRef Owner, CorpEntityRef Owned, string Kind, decimal? StakePercent, int? SinceYear, string? Notes);

/// <summary>A step of <see cref="CorporateHierarchyService.ChainAsync"/> or <see cref="CorporateHierarchyService.TreeAsync"/>:
/// the edge, and how many hops it sits from the entity asked about (1 = direct).</summary>
public sealed record HierarchyStep(int Depth, OwnershipLink Link);

/// <summary>A force a CorpoNation can call up: who keeps it, and how far down the family that keeper sits (0 = its own).</summary>
public sealed record CallableForce(Guid Id, string Name, string Kind, string? Strength, string? Notes, CorpEntityRef Keeper, CorpEntityRef? ForceEntity, int Depth);

/// <summary>One thing <see cref="CorporateHierarchyService.AuditAsync"/> found wrong.
/// Kind: tier-order | cycle | untiered-corponation | untiered-parent.</summary>
public sealed record HierarchyFinding(string Kind, string Message, Guid? EntityId, Guid? OwnershipId);

/// <summary>What to record on an ownership edge. Null fields on an existing edge are left as they are.</summary>
public sealed record OwnershipDraft(string Owner, string Owned, string? Kind = null, decimal? StakePercent = null, int? SinceYear = null, string? Notes = null);

/// <summary>What to record on a force. Upserts by (keeper, name).</summary>
public sealed record ForceDraft(string Corponation, string Name, string Kind, string? ForceEntity = null, string? Strength = null, string? Notes = null);

/// <summary>
/// CorpoNation hierarchies (PRS-T819): who owns what, all the way back down to ground level, and
/// the forces each CorpoNation can call up. The author's law this enforces:
/// <list type="bullet">
/// <item>Every CorpoNation has a Tier, 1-4 (<see cref="CorporateTier"/>).</item>
/// <item>Tiers stacked on tiers are one corporate family: an owner always sits on a higher Tier
/// than what it owns. Refused on write when both ends are tiered; untiered ends are reported by
/// <see cref="AuditAsync"/> instead, so the ladder can be filled in after the edges exist.</item>
/// <item>Nothing owns itself, ownership never loops, and owner and owned share a universe.</item>
/// <item>Every CorpoNation keeps its own private military and can call up the forces of every
/// subsidiary beneath it. "Beneath" means through a wholly-owned or controlling edge: a minority
/// stake, a lease or a licence buys no troops.</item>
/// </list>
/// The ownership graph is small (one universe's corporate sector), so every walk loads the
/// universe's edges once and walks them in memory.
/// </summary>
public sealed class CorporateHierarchyService(IDbContextFactory<ProseDbContext> dbFactory, EntityFieldWriter? fieldWriter = null)
{
    /// <summary>Edge kinds that carry command: forces flow up through these and no others.</summary>
    public static readonly string[] ControllingKinds = ["wholly-owned", "controlling"];

    // ── Resolution ────────────────────────────────────────────────────────

    /// <summary>
    /// An entity by id (any universe), else slug, else exact name, in the ambient universe. When a
    /// slug or name matches several entities and <paramref name="preferCorporate"/> is set, the one
    /// corponation or subsidiary among them wins; otherwise it is ambiguous and refused.
    /// </summary>
    public async Task<CorpEntityRef> ResolveAsync(string reference, bool preferCorporate = false, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reference)) throw new ArgumentException("an entity is required (its id, slug or exact name).");
        reference = reference.Trim();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        Guid? id = null;
        if (Guid.TryParse(reference, out var g))
            id = await db.Entities.IgnoreQueryFilters().AsNoTracking().Where(e => e.Id == g).Select(e => (Guid?)e.Id).FirstOrDefaultAsync(ct);
        else
        {
            foreach (var bySlug in new[] { true, false })
            {
                var hits = await db.Entities.AsNoTracking()
                    .Where(e => bySlug ? e.Slug == reference : e.Name == reference)
                    .Select(e => new { e.Id, e.EntityType, e.Name }).Take(10).ToListAsync(ct);
                if (hits.Count == 0) continue;
                if (hits.Count > 1 && preferCorporate && hits.Count(h => CorporateTier.IsCorporate(h.EntityType)) == 1)
                    hits = hits.Where(h => CorporateTier.IsCorporate(h.EntityType)).ToList();
                if (hits.Count > 1)
                    throw new ArgumentException($"'{reference}' matches {hits.Count} entities: " +
                        string.Join("; ", hits.Select(h => $"{h.Name} [{h.EntityType}] {h.Id:N}")) + ". Give the id.");
                id = hits[0].Id;
                break;
            }
        }
        if (id == null) throw new ArgumentException($"no entity '{reference}' in this universe (give its id, slug or exact name).");
        return (await LoadRefsAsync(db, [id.Value], ct))[id.Value];
    }

    /// <summary>Name, slug, type, universe and Tier for each id that exists (any universe).</summary>
    private static async Task<Dictionary<Guid, CorpEntityRef>> LoadRefsAsync(ProseDbContext db, ICollection<Guid> ids, CancellationToken ct)
    {
        if (ids.Count == 0) return [];
        var list = ids.Distinct().ToList();
        var rows = await db.Entities.IgnoreQueryFilters().AsNoTracking().Where(e => list.Contains(e.Id))
            .Select(e => new { e.Id, e.Name, e.Slug, e.EntityType, e.UniverseId }).ToListAsync(ct);
        var tiers = await TiersAsync(db, list, ct);
        return rows.ToDictionary(r => r.Id, r => new CorpEntityRef(r.Id, r.Name, r.Slug, r.EntityType, tiers.GetValueOrDefault(r.Id), r.UniverseId));
    }

    /// <summary>Tier of every listed corponation or subsidiary that has one.</summary>
    private static async Task<Dictionary<Guid, int?>> TiersAsync(ProseDbContext db, List<Guid> ids, CancellationToken ct)
    {
        var corp = await db.Corponations.AsNoTracking().Where(c => ids.Contains(c.Id) && c.Tier != null).Select(c => new { c.Id, c.Tier }).ToListAsync(ct);
        var subs = await db.Subsidiaries.AsNoTracking().Where(s => ids.Contains(s.Id) && s.Tier != null).Select(s => new { s.Id, s.Tier }).ToListAsync(ct);
        return corp.Concat(subs).GroupBy(x => x.Id).ToDictionary(g => g.Key, g => g.First().Tier);
    }

    // ── Tier ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Every way storing <paramref name="tier"/> on <paramref name="entityId"/> would break the
    /// ladder against the ownership edges already recorded (an owner above it at the same Tier or
    /// lower, or something it owns at the same Tier or higher). Empty when it fits, or when
    /// <paramref name="tier"/> is null. Synchronous because the repositories' Save calls it.
    /// </summary>
    public static List<string> TierConflicts(ProseDbContext db, Guid entityId, int? tier)
    {
        if (tier is not int t) return [];
        var edges = db.CorporateOwnerships.IgnoreQueryFilters().AsNoTracking()
            .Where(o => o.OwnerEntityId == entityId || o.OwnedEntityId == entityId)
            .Select(o => new { o.OwnerEntityId, o.OwnedEntityId }).ToList();
        if (edges.Count == 0) return [];
        var others = edges.Select(e => e.OwnerEntityId == entityId ? e.OwnedEntityId : e.OwnerEntityId).Distinct().ToList();
        var refs = LoadRefsAsync(db, others, CancellationToken.None).GetAwaiter().GetResult();
        var self = db.Entities.IgnoreQueryFilters().AsNoTracking().Where(e => e.Id == entityId).Select(e => e.Name).FirstOrDefault() ?? entityId.ToString("N");
        var problems = new List<string>();
        foreach (var e in edges)
        {
            if (e.OwnedEntityId == entityId && refs.TryGetValue(e.OwnerEntityId, out var owner) && owner.Tier is int ot && ot <= t)
                problems.Add($"{owner.Name} (Tier {ot}) owns {self}, so {self} must sit below Tier {ot}, not at Tier {t}.");
            if (e.OwnerEntityId == entityId && refs.TryGetValue(e.OwnedEntityId, out var owned) && owned.Tier is int dt && dt >= t)
                problems.Add($"{self} owns {owned.Name} (Tier {dt}), so {self} must sit above Tier {dt}, not at Tier {t}.");
        }
        return problems;
    }

    /// <summary>
    /// Set (or with null, clear) a corponation's or subsidiary's Tier through the ordinary field
    /// writer, so the record's save rules, cost gate and read-back all apply. Refused for 5, for
    /// anything outside 1-4, for other entity types, and when it would break the ladder.
    /// </summary>
    public async Task<(CorpEntityRef Entity, FieldWriteResult Write)> SetTierAsync(string entity, int? tier, bool confirmUnread = false, CancellationToken ct = default)
    {
        var e = await ResolveAsync(entity, preferCorporate: true, ct);
        if (!CorporateTier.IsCorporate(e.EntityType))
            throw new ArgumentException($"{e.Name} is a {e.EntityType}; only a corponation or a subsidiary has a Tier.");
        CorporateTier.Validate(tier);
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            var clash = TierConflicts(db, e.Id, tier);
            if (clash.Count > 0) throw new InvalidOperationException(string.Join(" ", clash));
        }
        if (fieldWriter == null) throw new InvalidOperationException("no field writer is wired; Tier can only be set inside the Hub.");
        var fields = new JsonObject { ["tier"] = tier is int t ? JsonValue.Create(t) : null };
        var write = await fieldWriter.SetFieldsAsync(e.Id.ToString("N"), fields.ToJsonString(), confirmUnread, ct);
        return (e with { Tier = write.Ok ? tier : e.Tier }, write);
    }

    // ── Ownership ─────────────────────────────────────────────────────────

    /// <summary>
    /// Record that <see cref="OwnershipDraft.Owner"/> owns <see cref="OwnershipDraft.Owned"/>, or
    /// update the edge when it already exists. Refused: an owner that is not a corponation or
    /// subsidiary, self-ownership, owner and owned in different universes, an unknown kind, a stake
    /// outside 0-100, an owner not on a higher Tier than the owned (both tiered), and a loop.
    /// </summary>
    public async Task<(OwnershipLink Link, bool Created)> SetOwnershipAsync(OwnershipDraft draft, CancellationToken ct = default)
    {
        var owner = await ResolveAsync(draft.Owner, preferCorporate: true, ct);
        var owned = await ResolveAsync(draft.Owned, ct: ct);
        if (!CorporateTier.IsCorporate(owner.EntityType))
            throw new ArgumentException($"{owner.Name} is a {owner.EntityType}; only a corponation or a subsidiary can own things.");
        if (owner.Id == owned.Id) throw new ArgumentException($"{owner.Name} cannot own itself.");
        if (owner.UniverseId != owned.UniverseId)
            throw new ArgumentException($"{owner.Name} and {owned.Name} are in different universes; ownership never crosses universes.");
        var kind = draft.Kind?.Trim().ToLowerInvariant();
        if (kind != null && !CorporateOwnership.Kinds.Contains(kind))
            throw new ArgumentException($"unknown ownership kind '{draft.Kind}'. Kinds: {string.Join(", ", CorporateOwnership.Kinds)}.");
        if (draft.StakePercent is < 0 or > 100) throw new ArgumentException($"a stake is 0-100 percent, not {draft.StakePercent}.");
        if (owner.Tier is int ot && owned.Tier is int dt && ot <= dt)
            throw new InvalidOperationException($"{owner.Name} (Tier {ot}) cannot own {owned.Name} (Tier {dt}): an owner always sits on a higher Tier than what it owns.");

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var edges = await EdgesAsync(db, owner.UniverseId, ct);
        if (Reaches(edges, from: owned.Id, to: owner.Id, out var loop))
        {
            var names = await LoadRefsAsync(db, loop, ct);
            throw new InvalidOperationException($"{owner.Name} owning {owned.Name} would close a loop: " +
                string.Join(" → ", loop.Select(id => names.TryGetValue(id, out var r) ? r.Name : id.ToString("N"))) + $" → {owned.Name}.");
        }

        var row = await db.CorporateOwnerships.IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.OwnerEntityId == owner.Id && o.OwnedEntityId == owned.Id, ct);
        var created = row == null;
        if (row == null)
        {
            row = new CorporateOwnership { OwnerEntityId = owner.Id, OwnedEntityId = owned.Id, UniverseId = owner.UniverseId, Kind = kind ?? "wholly-owned" };
            db.CorporateOwnerships.Add(row);
        }
        else if (kind != null) row.Kind = kind;
        if (draft.StakePercent != null) row.StakePercent = draft.StakePercent;
        if (draft.SinceYear != null) row.SinceYear = draft.SinceYear;
        if (draft.Notes != null) row.Notes = string.IsNullOrWhiteSpace(draft.Notes) ? null : draft.Notes.Trim();
        row.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return (new OwnershipLink(row.Id, owner, owned, row.Kind, row.StakePercent, row.SinceYear, row.Notes), created);
    }

    /// <summary>Remove the edge <paramref name="owner"/> → <paramref name="owned"/>. False when there was none.</summary>
    public async Task<bool> RemoveOwnershipAsync(string owner, string owned, CancellationToken ct = default)
    {
        var a = await ResolveAsync(owner, preferCorporate: true, ct);
        var b = await ResolveAsync(owned, ct: ct);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.CorporateOwnerships.IgnoreQueryFilters()
            .Where(o => o.OwnerEntityId == a.Id && o.OwnedEntityId == b.Id).ExecuteDeleteAsync(ct) > 0;
    }

    /// <summary>The entity's direct owners and its direct holdings.</summary>
    public async Task<(CorpEntityRef Entity, List<OwnershipLink> OwnedBy, List<OwnershipLink> Owns)> ListOwnershipAsync(string entity, CancellationToken ct = default)
    {
        var e = await ResolveAsync(entity, preferCorporate: true, ct);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var rows = await db.CorporateOwnerships.IgnoreQueryFilters().AsNoTracking()
            .Where(o => o.OwnerEntityId == e.Id || o.OwnedEntityId == e.Id).ToListAsync(ct);
        var links = await LinksAsync(db, rows, ct);
        return (e, links.Where(l => l.Owned.Id == e.Id).OrderBy(l => l.Owner.Name).ToList(),
                   links.Where(l => l.Owner.Id == e.Id).OrderBy(l => l.Owned.Name).ToList());
    }

    /// <summary>
    /// Everyone above the entity, nearest first, up to the ultimate parent(s): breadth-first over
    /// owner edges, each owner listed once at its shortest distance. An entity with several owners
    /// (a controlling parent and a minority investor) shows both lines.
    /// </summary>
    public async Task<(CorpEntityRef Entity, List<HierarchyStep> Chain)> ChainAsync(string entity, CancellationToken ct = default)
    {
        var e = await ResolveAsync(entity, preferCorporate: true, ct);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var edges = await EdgesAsync(db, e.UniverseId, ct);
        var byOwned = edges.ToLookup(x => x.OwnedEntityId);
        var steps = new List<(int Depth, CorporateOwnership Row)>();
        var seen = new HashSet<Guid> { e.Id };
        var frontier = new List<Guid> { e.Id };
        for (var depth = 1; frontier.Count > 0; depth++)
        {
            var next = new List<Guid>();
            foreach (var id in frontier)
                foreach (var row in byOwned[id].OrderBy(r => ControllingKinds.Contains(r.Kind) ? 0 : 1))
                {
                    steps.Add((depth, row));
                    if (seen.Add(row.OwnerEntityId)) next.Add(row.OwnerEntityId);
                }
            frontier = next;
        }
        var links = (await LinksAsync(db, steps.Select(s => s.Row).ToList(), ct)).ToDictionary(l => l.Id);
        return (e, steps.Select(s => new HierarchyStep(s.Depth, links[s.Row.Id])).ToList());
    }

    /// <summary>
    /// Everything the entity owns, recursively, down to the ground: depth-first, each holding with
    /// the edge that reaches it and its depth (1 = owned directly). A holding reached twice (two
    /// owners in one family) is listed under each, but walked below only once.
    /// </summary>
    public async Task<(CorpEntityRef Entity, List<HierarchyStep> Tree)> TreeAsync(string entity, CancellationToken ct = default)
    {
        var e = await ResolveAsync(entity, preferCorporate: true, ct);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var edges = await EdgesAsync(db, e.UniverseId, ct);
        var steps = Descend(edges, e.Id, controllingOnly: false);
        var links = (await LinksAsync(db, steps.Select(s => s.Row).ToList(), ct)).ToDictionary(l => l.Id);
        return (e, steps.Select(s => new HierarchyStep(s.Depth, links[s.Row.Id])).ToList());
    }

    private static List<(int Depth, CorporateOwnership Row)> Descend(List<CorporateOwnership> edges, Guid root, bool controllingOnly)
    {
        var byOwner = edges.Where(x => !controllingOnly || ControllingKinds.Contains(x.Kind)).ToLookup(x => x.OwnerEntityId);
        var steps = new List<(int, CorporateOwnership)>();
        var walked = new HashSet<Guid> { root };
        void Walk(Guid id, int depth)
        {
            foreach (var row in byOwner[id].OrderBy(r => r.Kind == "wholly-owned" ? 0 : 1).ThenBy(r => r.CreatedAt))
            {
                steps.Add((depth, row));
                if (walked.Add(row.OwnedEntityId)) Walk(row.OwnedEntityId, depth + 1);
            }
        }
        Walk(root, 1);
        return steps;
    }

    // ── Forces ────────────────────────────────────────────────────────────

    /// <summary>Record a force a corponation or subsidiary keeps, or update the one it already has by that name.</summary>
    public async Task<(CallableForce Force, bool Created)> SetForceAsync(ForceDraft draft, CancellationToken ct = default)
    {
        var keeper = await ResolveAsync(draft.Corponation, preferCorporate: true, ct);
        if (!CorporateTier.IsCorporate(keeper.EntityType))
            throw new ArgumentException($"{keeper.Name} is a {keeper.EntityType}; only a corponation or a subsidiary keeps forces.");
        var name = draft.Name?.Trim() ?? "";
        if (name.Length == 0) throw new ArgumentException("a force needs a name.");
        var kind = draft.Kind?.Trim().ToLowerInvariant() ?? "";
        if (!CorporateForce.Kinds.Contains(kind))
            throw new ArgumentException($"unknown force kind '{draft.Kind}'. Kinds: {string.Join(", ", CorporateForce.Kinds)}.");
        CorpEntityRef? forceEntity = null;
        if (!string.IsNullOrWhiteSpace(draft.ForceEntity))
        {
            forceEntity = await ResolveAsync(draft.ForceEntity, ct: ct);
            if (forceEntity.UniverseId != keeper.UniverseId)
                throw new ArgumentException($"{forceEntity.Name} is in a different universe from {keeper.Name}.");
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.CorporateForces.IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.CorponationEntityId == keeper.Id && f.Name == name, ct);
        var created = row == null;
        if (row == null)
        {
            row = new CorporateForce { CorponationEntityId = keeper.Id, UniverseId = keeper.UniverseId, Name = name };
            db.CorporateForces.Add(row);
        }
        row.Kind = kind;
        if (forceEntity != null) row.ForceEntityId = forceEntity.Id;
        if (draft.Strength != null) row.Strength = string.IsNullOrWhiteSpace(draft.Strength) ? null : draft.Strength.Trim();
        if (draft.Notes != null) row.Notes = string.IsNullOrWhiteSpace(draft.Notes) ? null : draft.Notes.Trim();
        row.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        if (forceEntity == null && row.ForceEntityId is Guid fid) forceEntity = (await LoadRefsAsync(db, [fid], ct)).GetValueOrDefault(fid);
        return (new CallableForce(row.Id, row.Name, row.Kind, row.Strength, row.Notes, keeper, forceEntity, 0), created);
    }

    /// <summary>Remove one force by id. False when there was none.</summary>
    public async Task<bool> RemoveForceAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.CorporateForces.IgnoreQueryFilters().Where(f => f.Id == id).ExecuteDeleteAsync(ct) > 0;
    }

    /// <summary>The forces the entity keeps itself (not its subsidiaries').</summary>
    public async Task<(CorpEntityRef Keeper, List<CallableForce> Forces)> ListForcesAsync(string corponation, CancellationToken ct = default)
    {
        var keeper = await ResolveAsync(corponation, preferCorporate: true, ct);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return (keeper, await ForcesOfAsync(db, new Dictionary<Guid, int> { [keeper.Id] = 0 }, ct));
    }

    /// <summary>
    /// Every force the CorpoNation can call up: its own, then every subsidiary's beneath it through
    /// wholly-owned or controlling edges, each with the entity that keeps it and how far down that
    /// keeper sits.
    /// </summary>
    public async Task<(CorpEntityRef Corponation, List<CallableForce> Forces)> CallableForcesAsync(string corponation, CancellationToken ct = default)
    {
        var root = await ResolveAsync(corponation, preferCorporate: true, ct);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var edges = await EdgesAsync(db, root.UniverseId, ct);
        var depthOf = new Dictionary<Guid, int> { [root.Id] = 0 };
        foreach (var (depth, row) in Descend(edges, root.Id, controllingOnly: true))
            if (!depthOf.ContainsKey(row.OwnedEntityId)) depthOf[row.OwnedEntityId] = depth;
        return (root, await ForcesOfAsync(db, depthOf, ct));
    }

    private static async Task<List<CallableForce>> ForcesOfAsync(ProseDbContext db, Dictionary<Guid, int> depthOf, CancellationToken ct)
    {
        var keepers = depthOf.Keys.ToList();
        var rows = await db.CorporateForces.IgnoreQueryFilters().AsNoTracking()
            .Where(f => keepers.Contains(f.CorponationEntityId)).ToListAsync(ct);
        var refs = await LoadRefsAsync(db, rows.Select(r => r.CorponationEntityId)
            .Concat(rows.Where(r => r.ForceEntityId != null).Select(r => r.ForceEntityId!.Value)).ToList(), ct);
        return rows
            .Select(r => new CallableForce(r.Id, r.Name, r.Kind, r.Strength, r.Notes, refs[r.CorponationEntityId],
                r.ForceEntityId is Guid fid ? refs.GetValueOrDefault(fid) : null, depthOf[r.CorponationEntityId]))
            .OrderBy(f => f.Depth).ThenBy(f => f.Keeper.Name).ThenBy(f => f.Name)
            .ToList();
    }

    // ── Audit and backfill ────────────────────────────────────────────────

    /// <summary>
    /// What is wrong with the ambient universe's hierarchy: owners not above what they own, loops,
    /// corponations with no Tier, and subsidiaries whose parent has no Tier.
    /// </summary>
    public async Task<List<HierarchyFinding>> AuditAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var findings = new List<HierarchyFinding>();
        var edges = await db.CorporateOwnerships.AsNoTracking().ToListAsync(ct);
        var refs = await LoadRefsAsync(db, edges.SelectMany(e => new[] { e.OwnerEntityId, e.OwnedEntityId }).ToList(), ct);
        string N(Guid id) => refs.TryGetValue(id, out var r) ? r.Name : id.ToString("N");

        foreach (var e in edges)
            if (refs.TryGetValue(e.OwnerEntityId, out var o) && refs.TryGetValue(e.OwnedEntityId, out var d)
                && o.Tier is int ot && d.Tier is int dt && ot <= dt)
                findings.Add(new HierarchyFinding("tier-order",
                    $"{o.Name} (Tier {ot}) owns {d.Name} (Tier {dt}); an owner must sit on a higher Tier.", e.OwnerEntityId, e.Id));

        foreach (var loop in Cycles(edges))
            findings.Add(new HierarchyFinding("cycle", "ownership loops: " + string.Join(" → ", loop.Append(loop[0]).Select(N)) + ".", loop[0], null));

        var corps = await db.Entities.AsNoTracking().Where(e => e.EntityType == "corponation")
            .Select(e => new { e.Id, e.Name }).ToListAsync(ct);
        var corpTiers = await TiersAsync(db, corps.Select(c => c.Id).ToList(), ct);
        foreach (var c in corps.Where(c => !corpTiers.ContainsKey(c.Id)).OrderBy(c => c.Name))
            findings.Add(new HierarchyFinding("untiered-corponation", $"{c.Name} has no Tier.", c.Id, null));

        var subIds = await db.Entities.AsNoTracking().Where(e => e.EntityType == "subsidiary").Select(e => e.Id).ToListAsync(ct);
        var subParents = await db.Subsidiaries.AsNoTracking().Where(s => subIds.Contains(s.Id) && s.ParentCorponationId != null)
            .Select(s => new { s.Id, Parent = s.ParentCorponationId!.Value }).ToListAsync(ct);
        var parentPairs = subParents.Select(s => (Sub: s.Id, Parent: s.Parent))
            .Concat(edges.Where(e => refs.TryGetValue(e.OwnedEntityId, out var d) && d.EntityType == "subsidiary")
                .Select(e => (Sub: e.OwnedEntityId, Parent: e.OwnerEntityId)))
            .Distinct().ToList();
        var pairRefs = await LoadRefsAsync(db, parentPairs.SelectMany(p => new[] { p.Sub, p.Parent }).ToList(), ct);
        foreach (var (sub, parent) in parentPairs)
        {
            if (!pairRefs.TryGetValue(parent, out var p) || p.Tier != null) continue;
            findings.Add(new HierarchyFinding("untiered-parent",
                $"{(pairRefs.TryGetValue(sub, out var s) ? s.Name : sub.ToString("N"))}'s parent {p.Name} has no Tier.", sub, null));
        }
        return findings;
    }

    /// <summary>
    /// One wholly-owned edge for every subsidiary in the ambient universe whose
    /// <see cref="Subsidiary.ParentCorponationId"/> is set and which has no edge from that parent yet.
    /// Idempotent: a second run creates nothing. Self- and cross-universe parents are skipped.
    /// </summary>
    public async Task<(int Created, int AlreadyRecorded, int Skipped)> BackfillFromParentsAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var subIds = await db.Entities.AsNoTracking().Where(e => e.EntityType == "subsidiary").Select(e => e.Id).ToListAsync(ct);
        var pairs = await db.Subsidiaries.AsNoTracking().Where(s => subIds.Contains(s.Id) && s.ParentCorponationId != null)
            .Select(s => new { s.Id, Parent = s.ParentCorponationId!.Value }).ToListAsync(ct);
        var involved = subIds.Concat(pairs.Select(p => p.Parent)).Distinct().ToList();
        var universes = await db.Entities.IgnoreQueryFilters().AsNoTracking()
            .Where(e => involved.Contains(e.Id))
            .Select(e => new { e.Id, e.UniverseId }).ToDictionaryAsync(e => e.Id, e => e.UniverseId, ct);
        var existing = (await db.CorporateOwnerships.IgnoreQueryFilters().AsNoTracking()
            .Where(o => subIds.Contains(o.OwnedEntityId)).Select(o => new { o.OwnerEntityId, o.OwnedEntityId }).ToListAsync(ct))
            .Select(o => (o.OwnerEntityId, o.OwnedEntityId)).ToHashSet();
        int created = 0, already = 0, skipped = 0;
        foreach (var p in pairs)
        {
            if (existing.Contains((p.Parent, p.Id))) { already++; continue; }
            if (p.Parent == p.Id || !universes.TryGetValue(p.Parent, out var pu) || !universes.TryGetValue(p.Id, out var su) || pu != su) { skipped++; continue; }
            db.CorporateOwnerships.Add(new CorporateOwnership { OwnerEntityId = p.Parent, OwnedEntityId = p.Id, UniverseId = pu, Kind = "wholly-owned" });
            created++;
        }
        if (created > 0) await db.SaveChangesAsync(ct);
        return (created, already, skipped);
    }

    // ── Graph helpers ─────────────────────────────────────────────────────

    private static Task<List<CorporateOwnership>> EdgesAsync(ProseDbContext db, Guid universeId, CancellationToken ct) =>
        db.CorporateOwnerships.IgnoreQueryFilters().AsNoTracking().Where(o => o.UniverseId == universeId).ToListAsync(ct);

    /// <summary>Whether <paramref name="to"/> is reachable from <paramref name="from"/> going down owner → owned
    /// edges; <paramref name="path"/> is the route when it is.</summary>
    private static bool Reaches(List<CorporateOwnership> edges, Guid from, Guid to, out List<Guid> path)
    {
        var byOwner = edges.ToLookup(e => e.OwnerEntityId, e => e.OwnedEntityId);
        var cameFrom = new Dictionary<Guid, Guid>();
        var queue = new Queue<Guid>([from]);
        var seen = new HashSet<Guid> { from };
        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            if (id == to)
            {
                // from (the would-be owned) … owns … to (the would-be owner); the caller appends the new edge's end.
                path = [to];
                while (path[0] != from) path.Insert(0, cameFrom[path[0]]);
                return true;
            }
            foreach (var next in byOwner[id])
                if (seen.Add(next)) { cameFrom[next] = id; queue.Enqueue(next); }
        }
        path = [];
        return false;
    }

    /// <summary>Every ownership loop, each once (as the list of entities around it).</summary>
    private static List<List<Guid>> Cycles(List<CorporateOwnership> edges)
    {
        var byOwner = edges.ToLookup(e => e.OwnerEntityId, e => e.OwnedEntityId);
        var state = new Dictionary<Guid, int>();   // 1 = on the stack, 2 = done
        var stack = new List<Guid>();
        var found = new List<List<Guid>>();
        var keys = new HashSet<string>();
        void Visit(Guid id)
        {
            state[id] = 1;
            stack.Add(id);
            foreach (var next in byOwner[id])
            {
                if (state.GetValueOrDefault(next) == 1)
                {
                    var loop = stack.Skip(stack.IndexOf(next)).ToList();
                    if (keys.Add(string.Join(",", loop.OrderBy(g => g)))) found.Add(loop);
                }
                else if (state.GetValueOrDefault(next) == 0) Visit(next);
            }
            stack.RemoveAt(stack.Count - 1);
            state[id] = 2;
        }
        foreach (var id in edges.Select(e => e.OwnerEntityId).Distinct().ToList())
            if (state.GetValueOrDefault(id) == 0) Visit(id);
        return found;
    }

    private static async Task<List<OwnershipLink>> LinksAsync(ProseDbContext db, List<CorporateOwnership> rows, CancellationToken ct)
    {
        var refs = await LoadRefsAsync(db, rows.SelectMany(r => new[] { r.OwnerEntityId, r.OwnedEntityId }).ToList(), ct);
        return rows.Where(r => refs.ContainsKey(r.OwnerEntityId) && refs.ContainsKey(r.OwnedEntityId))
            .Select(r => new OwnershipLink(r.Id, refs[r.OwnerEntityId], refs[r.OwnedEntityId], r.Kind, r.StakePercent, r.SinceYear, r.Notes))
            .ToList();
    }
}
