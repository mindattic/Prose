using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// Moves a custom-type entity to another custom repository (PRS-T994): the entity row's
/// <c>EntityType</c>, the <c>"type"</c> key of its <c>Records.Json</c>, the <c>repo</c> attribute
/// of every <c>&lt;entity&gt;</c> tag carrying its guid in its universe's beats, and the derived rows
/// that copy the type (<c>BeatEntityMentions</c>, <c>ContinuityClaims</c>, the review tables,
/// <c>DistributedWorkQueue</c>). One transaction per entity.
///
/// <para>Beat text is changed only inside the matching opening tags, through EF, so
/// <c>ProseDbContext</c> re-stamps <c>TextHash</c> and carries read receipts across what is a
/// tag-only edit, and Beats' system versioning keeps the old text. It does not go through
/// <see cref="NodeWorkbenchService.UpdateBeatTextAsync"/>: that save re-derives every tag in the beat
/// from a fresh scan, which could move bytes other than the one attribute. Once the entity row
/// carries the new type, that re-derivation writes the new <c>repo</c> anyway, so later saves agree.</para>
/// </summary>
public sealed class EntityRetypeService(IDbContextFactory<ProseDbContext> dbFactory)
{
    private static readonly Regex OpenTagPattern = new(@"<entity\b[^>]*>", RegexOptions.CultureInvariant);
    private static readonly Regex GuidAttr = new(@"\bguid=""([^""]*)""", RegexOptions.CultureInvariant);
    private static readonly Regex RepoAttr = new(@"(\brepo="")([^""]*)("")", RegexOptions.CultureInvariant);

    /// <summary>True for a type with its own typed table (or a reserved placeholder), which this move refuses.</summary>
    public static bool IsBuiltIn(string type) =>
        string.Equals(type, "character", StringComparison.OrdinalIgnoreCase)
        || string.Equals(type, "stub", StringComparison.OrdinalIgnoreCase)
        || EntityFieldWriter.Repositories.ContainsKey(type)
        || CanonRecordLoader.MappedTypes.Contains(type);

    public async Task<EntityRetypePlan> PlanAsync(Guid universeId, Guid entityId, string toType, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var entity = await db.Entities.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(e => e.Id == entityId, ct);
        if (entity == null) return EntityRetypePlan.Fail(entityId, "entity_not_found");
        if (entity.UniverseId != universeId) return EntityRetypePlan.Fail(entityId, "entity_not_in_universe", entity.Name, entity.EntityType);
        if (!await db.RepositoryDefinitions.AsNoTracking().AnyAsync(r => r.Slug == toType, ct))
            return EntityRetypePlan.Fail(entityId, $"target_repository_not_found: {toType}", entity.Name, entity.EntityType);
        if (IsBuiltIn(toType))
            return EntityRetypePlan.Fail(entityId, $"target_is_built_in: {toType}", entity.Name, entity.EntityType);
        if (IsBuiltIn(entity.EntityType))
            return EntityRetypePlan.Fail(entityId, $"source_is_built_in: {entity.EntityType}", entity.Name, entity.EntityType);
        if (string.Equals(entity.EntityType, toType, StringComparison.Ordinal))
            return EntityRetypePlan.Fail(entityId, "already_that_type", entity.Name, entity.EntityType);
        if (await db.Entities.IgnoreQueryFilters().AnyAsync(e => e.Id != entityId && e.UniverseId == universeId
                && e.EntityType == toType && e.Slug == entity.Slug, ct))
            return EntityRetypePlan.Fail(entityId, $"slug_taken_in_target: {entity.Slug}", entity.Name, entity.EntityType);

        var beats = await TaggedBeatsAsync(db, universeId, entityId, ct);
        var changes = new List<EntityRetypeBeat>();
        foreach (var b in beats)
        {
            var n = CountTagsToRewrite(b.Text, entityId, toType);
            if (n > 0) changes.Add(new EntityRetypeBeat(b.Id, n, b.Books));
        }
        return new EntityRetypePlan(true, null, entityId, entity.Name, entity.EntityType, toType, changes);
    }

    public async Task<EntityRetypeResult> ApplyAsync(Guid universeId, Guid entityId, string toType, CancellationToken ct = default)
    {
        var plan = await PlanAsync(universeId, entityId, toType, ct);
        if (!plan.Ok) return new EntityRetypeResult(plan, false, null, null, -1, plan.Error);
        var from = plan.FromType;
        var n = entityId.ToString("N");
        var d = entityId.ToString("D");

        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);

            var entity = await db.Entities.IgnoreQueryFilters().FirstAsync(e => e.Id == entityId, ct);
            entity.EntityType = toType;
            entity.ModifiedAt = DateTime.UtcNow;

            var record = await db.Records.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.EntityId == entityId, ct);
            if (record != null && JsonNode.Parse(record.Json) is JsonObject json)
            {
                json["type"] = toType;
                record.Json = json.ToJsonString();
                record.UpdatedAt = DateTime.UtcNow;
            }

            var beatIds = plan.Beats.Select(b => b.BeatId).ToList();
            var beats = await db.Beats.Where(b => beatIds.Contains(b.Id)).ToListAsync(ct);
            foreach (var beat in beats)
            {
                beat.Text = RewriteRepo(beat.Text ?? "", entityId, toType);
                beat.LastWriteReason = nameof(BeatWriteReason.TagMaintenance);
            }

            foreach (var m in await db.BeatEntityMentions.Where(m => m.EntityId == entityId).ToListAsync(ct))
                m.EntityType = toType;
            foreach (var c in await db.ContinuityClaims.Where(c => (c.EntityId == n || c.EntityId == d) && c.EntityKind == from).ToListAsync(ct))
                c.EntityKind = toType;
            foreach (var r in await db.EntityReviews.Where(r => (r.EntityId == n || r.EntityId == d) && r.EntityType == from).ToListAsync(ct))
                r.EntityType = toType;
            foreach (var s in await db.EntityReviewSummaries.Where(s => (s.EntityId == n || s.EntityId == d) && s.EntityType == from).ToListAsync(ct))
                s.EntityType = toType;
            foreach (var q in await db.EntityReviewQueue.Where(q => (q.EntityId == n || q.EntityId == d) && q.EntityType == from).ToListAsync(ct))
                q.EntityType = toType;
            foreach (var w in await db.DistributedWorkQueue.Where(w => (w.TargetId == n || w.TargetId == d) && w.TargetType == from).ToListAsync(ct))
                w.TargetType = toType;

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }

        await using var check = await dbFactory.CreateDbContextAsync(ct);
        var typeBack = await check.Entities.IgnoreQueryFilters().AsNoTracking()
            .Where(e => e.Id == entityId).Select(e => e.EntityType).FirstOrDefaultAsync(ct);
        var recordJson = await check.Records.IgnoreQueryFilters().AsNoTracking()
            .Where(r => r.EntityId == entityId).Select(r => r.Json).FirstOrDefaultAsync(ct);
        var recordTypeBack = recordJson == null ? null : (JsonNode.Parse(recordJson) as JsonObject)?["type"]?.ToString();
        var remaining = (await TaggedBeatsAsync(check, universeId, entityId, ct)).Sum(b => CountTagsToRewrite(b.Text, entityId, toType));
        var ok = typeBack == toType && (recordJson == null || recordTypeBack == toType) && remaining == 0;
        return new EntityRetypeResult(plan, ok, typeBack, recordTypeBack, remaining, ok ? null : "read_back_mismatch");
    }

    /// <summary>Tags for <paramref name="entityId"/> whose <c>repo</c> is not <paramref name="toType"/>.</summary>
    public static int CountTagsToRewrite(string? text, Guid entityId, string toType)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        var count = 0;
        foreach (Match m in OpenTagPattern.Matches(text))
            if (IsFor(m.Value, entityId) && RepoAttr.Match(m.Value) is { Success: true } r && r.Groups[2].Value != toType)
                count++;
        return count;
    }

    /// <summary>Rewrites the <c>repo</c> attribute of <paramref name="entityId"/>'s opening tags; every other byte is kept.</summary>
    public static string RewriteRepo(string text, Guid entityId, string toType) =>
        OpenTagPattern.Replace(text, m => IsFor(m.Value, entityId)
            ? RepoAttr.Replace(m.Value, r => r.Groups[1].Value + toType + r.Groups[3].Value, 1)
            : m.Value);

    private static bool IsFor(string openTag, Guid entityId) =>
        GuidAttr.Match(openTag) is { Success: true } g && Guid.TryParse(g.Groups[1].Value, out var id) && id == entityId;

    private sealed record TaggedBeat(Guid Id, string? Text, IReadOnlyList<string> Books);

    private static async Task<List<TaggedBeat>> TaggedBeatsAsync(ProseDbContext db, Guid universeId, Guid entityId, CancellationToken ct)
    {
        var needle = entityId.ToString("D");
        var nodes = await db.Nodes.IgnoreQueryFilters().AsNoTracking().Where(x => x.UniverseId == universeId)
            .Select(x => new { x.Id, x.ParentNodeId, x.Kind, x.Title }).ToDictionaryAsync(x => x.Id, ct);
        var rows = await db.Beats.AsNoTracking()
            .Where(b => b.Text != null && b.Text.Contains(needle))
            .Join(db.BeatNodes.AsNoTracking(), b => b.Id, bn => bn.BeatId, (b, bn) => new { b.Id, b.Text, bn.NodeId })
            .ToListAsync(ct);

        string? BookOf(Guid nodeId)
        {
            for (var at = (Guid?)nodeId; at is { } id && nodes.TryGetValue(id, out var node); at = node.ParentNodeId)
                if (node.Kind == "book") return node.Title;
            return null;
        }

        return rows.Where(r => nodes.ContainsKey(r.NodeId))
            .GroupBy(r => r.Id)
            .Select(g => new TaggedBeat(g.Key, g.First().Text,
                g.Select(r => BookOf(r.NodeId) ?? "(no book)").Distinct().ToList()))
            .ToList();
    }
}

public sealed record EntityRetypeBeat(Guid BeatId, int Tags, IReadOnlyList<string> Books);

public sealed record EntityRetypePlan(bool Ok, string? Error, Guid EntityId, string Name, string FromType, string ToType,
    IReadOnlyList<EntityRetypeBeat> Beats)
{
    public int TagCount => Beats.Sum(b => b.Tags);
    public IReadOnlyList<string> Books => Beats.SelectMany(b => b.Books).Distinct().OrderBy(b => b).ToList();

    public static EntityRetypePlan Fail(Guid id, string error, string name = "", string fromType = "") =>
        new(false, error, id, name, fromType, "", []);
}

public sealed record EntityRetypeResult(EntityRetypePlan Plan, bool Ok, string? EntityTypeBack, string? RecordTypeBack,
    int TagsStillOld, string? Error);
