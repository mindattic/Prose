using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;
using Prose.Core.Services.Contradiction;

namespace Prose.Core.Composition.Ledger;

/// <summary>One open/advanced promise the story owes, scoped to an on-screen entity.</summary>
public sealed record OnScreenObligation(Guid Id, string Kind, string Description, string State, string? TriggerCondition);

/// <summary>
/// Bounded snapshot of "what is true right now" for the entities on screen in one beat — v4's
/// replacement for BeatGeneratorService's cold-start/24-cap EntityContextStack and the decorative
/// QuikGraph injection (see the v4 plan, §2). Always bounded to entities actually mentioned in the
/// beat plus its place — never a full-book scan.
/// </summary>
public sealed class OnScreenSnapshot
{
    public IReadOnlyList<Guid> EntityIds { get; init; } = [];
    public IReadOnlyList<OnScreenFact> EntityStateFacts { get; init; } = [];
    public IReadOnlyList<OnScreenFact> ContinuityFacts { get; init; } = [];
    public IReadOnlyList<OnScreenObligation> OpenObligations { get; init; } = [];

    public bool IsEmpty => EntityIds.Count == 0;
}

/// <summary>
/// Phase 0 read-path service (v4 plan §2). Reads ONLY — no writes, no schema changes. Reuses
/// <see cref="Beat.StoryPosition"/> (the documented authoritative story clock) via a join through
/// <see cref="EntityStateEvent.BeatGuid"/> rather than a denormalized column, so this can be
/// measured before committing to any schema change (the plan's own "don't gold-plate Phase 0"
/// discipline — a <c>EntityStateEvents.BeatStoryPosition</c> column can be added later purely as a
/// performance optimization once real query volume justifies it, not before).
/// </summary>
public sealed class StoryStateQuery
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;

    public StoryStateQuery(IDbContextFactory<ProseDbContext> dbFactory)
    {
        this.dbFactory = dbFactory;
    }

    /// <summary>
    /// On-screen facts for <paramref name="beatId"/>: entities mentioned in that beat (via
    /// <see cref="BeatEntityMention"/>) plus its place, each entity's latest state-ledger row per
    /// aspect at or before this beat's story position, CANONICAL/CONFIRMED continuity claims for
    /// those entities, and their open/advanced narrative obligations. Typically 2-6 entities —
    /// bounded by design, never a full-book scan.
    /// </summary>
    public async Task<OnScreenSnapshot> GetOnScreenFactsAsync(
        Guid nodeId, Guid beatId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var targetBeat = await db.Beats.AsNoTracking()
            .Where(b => b.Id == beatId)
            .Select(b => new { b.StoryPosition, b.PlaceEntityId, b.Text })
            .FirstOrDefaultAsync(ct);
        if (targetBeat is null) return new OnScreenSnapshot();

        var asOf = targetBeat.StoryPosition ?? int.MaxValue;

        // Parse <entity> tags straight from the beat's own text rather than trusting
        // BeatEntityMentions (a derived index) — measured on live BCODA (Phase 0, 2026-09-17):
        // BeatEntityMentions is empty for the majority of real beats even when their text visibly
        // carries tagged entities, so it cannot be the primary source of "who's on screen." Parsing
        // the tags is also what BeatMarkup.ExtractEntityGuids's own doc comment recommends: "the
        // derivation path for BeatEntityMentions once a beat is tagged — parse tags, don't re-run a
        // name/alias scan."
        var entityIds = BeatMarkup.ExtractEntityGuids(targetBeat.Text).ToList();
        if (entityIds.Count == 0)
        {
            // Fallback for a beat with no tags at all yet: BeatEntityMentions, if it happens to
            // have been populated some other way.
            entityIds = await db.BeatEntityMentions.AsNoTracking()
                .Where(m => m.BeatId == beatId).Select(m => m.EntityId).Distinct().ToListAsync(ct);
        }
        if (targetBeat.PlaceEntityId is Guid placeId) entityIds.Add(placeId);
        entityIds = entityIds.Distinct().ToList();
        if (entityIds.Count == 0) return new OnScreenSnapshot();

        return await GetOnScreenFactsForEntitiesAsync(db, nodeId, entityIds, asOf, ct);
    }

    /// <summary>
    /// Same query as <see cref="GetOnScreenFactsAsync(Guid,Guid,CancellationToken)"/>, but for a
    /// beat that doesn't exist yet — the generation-time case, where the caller already knows (from
    /// the beat brief, mirroring v3's <c>BeatContext.CharactersInScene</c>) who's about to be on
    /// screen, rather than deriving it from an already-written beat's tags.
    /// </summary>
    public async Task<OnScreenSnapshot> GetOnScreenFactsForEntitiesAsync(
        Guid nodeId, IReadOnlyList<Guid> entityIds, int asOfStoryPosition, CancellationToken ct = default)
    {
        if (entityIds.Count == 0) return new OnScreenSnapshot();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await GetOnScreenFactsForEntitiesAsync(db, nodeId, entityIds, asOfStoryPosition, ct);
    }

    private static async Task<OnScreenSnapshot> GetOnScreenFactsForEntitiesAsync(
        ProseDbContext db, Guid nodeId, IReadOnlyList<Guid> rawEntityIds, int asOf, CancellationToken ct)
    {
        var entityIds = rawEntityIds.Distinct().ToList();

        var names = await db.Entities.AsNoTracking()
            .Where(e => entityIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.Name, ct);

        // Left join to the originating beat's StoryPosition (not the raw AtStoryTime column,
        // which the ledger's own docs note is "frequently null" — Beat.StoryPosition is the
        // documented authoritative clock). An event with no BeatGuid (a manual/system row) has no
        // position to compare and is always treated as visible.
        var rawEvents = await (
            from ev in db.EntityStateEvents.AsNoTracking()
            where entityIds.Contains(ev.EntityId)
            join b in db.Beats.AsNoTracking() on ev.BeatGuid equals b.Id into beatJoin
            from b in beatJoin.DefaultIfEmpty()
            select new { Ev = ev, BeatPos = (int?)(b == null ? null : b.StoryPosition) }
        ).ToListAsync(ct);

        var visible = rawEvents
            .Where(x => x.BeatPos == null || x.BeatPos <= asOf)
            .Select(x => x.Ev)
            .ToList();

        var latestPerAspect = visible
            .GroupBy(e => (e.EntityId, e.AspectKey))
            .Select(g => g.OrderByDescending(e => e.AtStoryTime).ThenByDescending(e => e.Id).First())
            .ToList();

        var entityFacts = latestPerAspect
            .Select(e => new OnScreenFact(
                names.GetValueOrDefault(e.EntityId, "(unknown entity)"),
                e.AspectKey,
                e.NewValue ?? "",
                $"ledger:{e.Source}",
                e.Snippet))
            .ToList();

        // ContinuityClaims.EntityId is stored as a string (see ContinuityService/ContinuityClaim).
        var entityIdStrings = entityIds.Select(id => id.ToString()).ToList();
        var claims = await db.ContinuityClaims.AsNoTracking()
            .Where(c => entityIdStrings.Contains(c.EntityId)
                     && (c.Status == "CANONICAL" || c.Status == "CONFIRMED"))
            .ToListAsync(ct);
        var continuityFacts = claims
            .Select(c => new OnScreenFact(c.EntityName, c.Predicate, c.Object, $"claim:{c.Status}", c.Snippet))
            .ToList();

        var obligations = await db.NarrativeObligations.AsNoTracking()
            .Where(o => o.NodeId == nodeId
                     && o.EntityId != null
                     && entityIds.Contains(o.EntityId.Value)
                     && (o.State == ObligationState.Open || o.State == ObligationState.Advanced))
            .ToListAsync(ct);
        var openObligations = obligations
            .Select(o => new OnScreenObligation(o.Id, o.Kind, o.Description, o.State, o.TriggerCondition))
            .ToList();

        return new OnScreenSnapshot
        {
            EntityIds = entityIds,
            EntityStateFacts = entityFacts,
            ContinuityFacts = continuityFacts,
            OpenObligations = openObligations,
        };
    }
}
