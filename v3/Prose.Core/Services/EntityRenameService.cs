using System.Text.RegularExpressions;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// Deterministic, book-scoped canonical-name rename. Preview is side-effect free; apply uses
/// existing canon and beat write doors so entity markup and derivative state stay synchronized.
/// </summary>
public sealed class EntityRenameService(
    IDbContextFactory<ProseDbContext> dbFactory,
    NodeWorkbenchService workbench,
    CanonDocumentService canonDocs,
    NodeDocService nodeDocs,
    MarkdownFileService markdownFiles,
    ContinuityService continuity,
    NounConsistencyService nouns)
{
    public async Task<EntityRenamePreview> PreviewAsync(string entityIdOrSlug, string nodeIdOrSlug, string newName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(newName)) return EntityRenamePreview.Failure("new_name_required");
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var entity = await ResolveEntityAsync(db, entityIdOrSlug, ct);
        if (entity == null) return EntityRenamePreview.Failure("entity_not_found");
        var nodeId = await NodeRefResolver.ResolveAsync(db, nodeIdOrSlug);
        if (nodeId == null) return EntityRenamePreview.Failure("node_not_found");
        var node = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstAsync(n => n.Id == nodeId.Value, ct);
        if (node.UniverseId != entity.UniverseId) return EntityRenamePreview.Failure("cross_universe_scope");
        if (string.Equals(entity.Name, newName.Trim(), StringComparison.Ordinal)) return EntityRenamePreview.Failure("name_unchanged");

        var matcher = NameRegex(entity.Name);
        var sections = await db.NodeOutlineSections.AsNoTracking().Where(s => s.NodeId == node.Id).ToListAsync(ct);
        var leafIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, node.Id, ct);
        var beats = await db.BeatNodes.AsNoTracking().Where(bn => leafIds.Contains(bn.NodeId))
            .Join(db.Beats.AsNoTracking(), bn => bn.BeatId, b => b.Id, (_, b) => b)
            .Where(b => b.Text != null).Select(b => new { b.Id, b.Text }).ToListAsync(ct);

        var outlineHits = sections.Where(s => matcher.IsMatch(s.Content ?? "")).Select(s => s.SectionType).ToList();
        var beatIds = beats.Where(b => matcher.IsMatch(b.Text!)).Select(b => b.Id).ToList();
        var ledgerCount = continuity.GetByEntity(entity.Id.ToString()).Count(c => !string.Equals(c.EntityName, newName.Trim(), StringComparison.Ordinal));
        return new EntityRenamePreview(true, null, entity.Id, entity.Name, entity.Slug, newName.Trim(), node.Id, node.Slug,
            outlineHits, beatIds, ledgerCount, entity.EntityType);
    }

    public async Task<EntityRenameResult> ApplyAsync(string entityIdOrSlug, string nodeIdOrSlug, string newName, string? note = null, CancellationToken ct = default)
    {
        var preview = await PreviewAsync(entityIdOrSlug, nodeIdOrSlug, newName, ct);
        if (!preview.Ok) return EntityRenameResult.Failure(preview.Error!);

        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            var entity = await db.Entities.FirstAsync(e => e.Id == preview.EntityId, ct);
            // Entity is the universal authoritative identity used by graph, tags, and all
            // non-character repositories. Character's mirrored name is handled below.
            entity.Name = preview.NewName;
            entity.Slug = await UniqueSlugAsync(db, entity, preview.NewName, ct);
            entity.ModifiedAt = DateTime.UtcNow;
            var record = await db.Records.FirstOrDefaultAsync(r => r.EntityId == entity.Id, ct);
            if (record != null && JsonNode.Parse(record.Json) is JsonObject recordObject)
            {
                recordObject["name"] = preview.NewName;
                record.Json = recordObject.ToJsonString();
                record.UpdatedAt = DateTime.UtcNow;
            }
            if (entity.EntityType == "character")
            {
                var character = await db.Characters.FirstOrDefaultAsync(c => c.Id == entity.Id, ct);
                if (character != null)
                {
                    character.Name = preview.NewName;
                    var parts = CharacterMapper.ParseName(preview.NewName);
                    character.TitlePrefix = parts.Title;
                    character.FirstName = parts.First;
                    character.MiddleName = string.IsNullOrWhiteSpace(parts.Middle) ? null : parts.Middle;
                    character.LastName = parts.Last;
                }
            }
            await db.SaveChangesAsync(ct);
        }

        var matcher = NameRegex(preview.OldName);
        foreach (var sectionType in preview.OutlineSections)
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var section = await db.NodeOutlineSections.AsNoTracking()
                .FirstAsync(s => s.NodeId == preview.NodeId && s.SectionType == sectionType, ct);
            await canonDocs.SetNodeOutlineSectionAsync(preview.NodeId, sectionType,
                matcher.Replace(section.Content ?? "", preview.NewName), ct);
        }

        foreach (var beatId in preview.BeatIds)
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var text = await db.Beats.AsNoTracking().Where(b => b.Id == beatId).Select(b => b.Text).FirstAsync(ct);
            await workbench.UpdateBeatTextAsync(beatId, matcher.Replace(text ?? "", preview.NewName), BeatWriteReason.AuthorEdit, null, ct);
        }

        // The rule prevents later prose from reintroducing the old canonical form.
        await nouns.AddRuleAsync((await NodeUniverseAsync(preview.NodeId, ct)), preview.OldName, preview.NewName,
            note ?? $"Renamed from {preview.OldName} via entity rename", preview.EntityId, ct);
        var relabeled = continuity.RelabelEntityName(preview.EntityId.ToString(), preview.NewName,
            note ?? "relabeled by entity rename");

        await nodeDocs.GenerateAsync(preview.NodeId, ct);
        await markdownFiles.SyncAllAsync(dryRun: false, ct: ct);
        return new EntityRenameResult(true, null, preview.EntityId, preview.OldName, preview.NewName,
            preview.OutlineSections.Count, preview.BeatIds.Count, relabeled);
    }

    private async Task<Guid> NodeUniverseAsync(Guid nodeId, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Nodes.IgnoreQueryFilters().Where(n => n.Id == nodeId).Select(n => n.UniverseId).FirstAsync(ct);
    }

    private static async Task<Prose.Core.Data.Entities.Entity?> ResolveEntityAsync(ProseDbContext db, string idOrSlug, CancellationToken ct) =>
        Guid.TryParse(idOrSlug, out var id)
            ? await db.Entities.FirstOrDefaultAsync(e => e.Id == id, ct)
            : await db.Entities.FirstOrDefaultAsync(e => e.Slug == idOrSlug, ct);

    private static Regex NameRegex(string name) => new($@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(name)}(?![\p{{L}}\p{{N}}])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static async Task<string> UniqueSlugAsync(ProseDbContext db, Prose.Core.Data.Entities.Entity entity, string name, CancellationToken ct)
    {
        var baseSlug = UniverseGraphService.Slugify(name);
        var exists = await db.Entities.AnyAsync(e => e.Id != entity.Id && e.UniverseId == entity.UniverseId && e.Slug == baseSlug, ct);
        return exists ? $"{baseSlug}-{entity.Id:N}" : baseSlug;
    }
}

public sealed record EntityRenamePreview(bool Ok, string? Error, Guid EntityId, string OldName, string OldSlug, string NewName,
    Guid NodeId, string NodeSlug, IReadOnlyList<string> OutlineSections, IReadOnlyList<Guid> BeatIds, int LedgerCount, string EntityType)
{
    public static EntityRenamePreview Failure(string error) => new(false, error, Guid.Empty, "", "", "", Guid.Empty, "", [], [], 0, "");
}

public sealed record EntityRenameResult(bool Ok, string? Error, Guid EntityId, string OldName, string NewName,
    int OutlineSectionsChanged, int BeatsChanged, int LedgerClaimsRelabeled)
{
    public static EntityRenameResult Failure(string error) => new(false, error, Guid.Empty, "", "", 0, 0, 0);
}
